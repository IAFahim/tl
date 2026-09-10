#!/usr/bin/env python3

import argparse
import hashlib
import json
import struct
import sys
import uuid
import zipfile
from pathlib import Path
from xml.etree import ElementTree


PACKAGE_FILES = {
    "Tl.CSharp": {
        "build/Tl.CSharp.targets",
        "buildTransitive/Tl.CSharp.targets",
        "lib/net10.0/_._",
        "tools/net10.0/any/Microsoft.CodeAnalysis.CSharp.dll",
        "tools/net10.0/any/Microsoft.CodeAnalysis.dll",
        "tools/net10.0/any/Tl.Gen.CSharp.deps.json",
        "tools/net10.0/any/Tl.Gen.CSharp.dll",
        "tools/net10.0/any/Tl.Gen.CSharp.pdb",
        "tools/net10.0/any/Tl.Gen.CSharp.runtimeconfig.json",
    },
    "Tl.Compiler": {"lib/net10.0/Tl.Compiler.dll"},
    "Tl.Gen.C": {"lib/net10.0/Tl.Gen.C.dll"},
    "Tl.Gen.CSharp": {
        "build/Tl.Gen.CSharp.targets",
        "buildTransitive/Tl.Gen.CSharp.targets",
        "tools/net10.0/any/Microsoft.CodeAnalysis.CSharp.dll",
        "tools/net10.0/any/Microsoft.CodeAnalysis.dll",
        "tools/net10.0/any/Tl.Gen.CSharp.deps.json",
        "tools/net10.0/any/Tl.Gen.CSharp.dll",
        "tools/net10.0/any/Tl.Gen.CSharp.pdb",
        "tools/net10.0/any/Tl.Gen.CSharp.runtimeconfig.json",
    },
    "Tl.Runtime": {"lib/net10.0/Tl.Core.dll"},
}

PACKAGE_DEPENDENCY_GROUPS = {
    "Tl.CSharp": [{"attributes": {"targetFramework": "net10.0"}, "dependencies": [{"id": "Tl.Runtime", "exclude": "Build,Analyzers"}]}],
    "Tl.Compiler": [{"attributes": {"targetFramework": "net10.0"}, "dependencies": []}],
    "Tl.Gen.C": [{"attributes": {"targetFramework": "net10.0"}, "dependencies": [{"id": "Tl.Compiler", "exclude": "Build,Analyzers"}]}],
    "Tl.Gen.CSharp": [],
    "Tl.Runtime": [{"attributes": {"targetFramework": "net10.0"}, "dependencies": []}],
}

PACKAGE_FORBIDDEN_FILES = {
    "Tl.CSharp": {"Tl.Compiler.dll", "Tl.Gen.C.dll"},
    "Tl.Compiler": {"Microsoft.CodeAnalysis.CSharp.dll", "Microsoft.CodeAnalysis.dll", "Tl.Gen.C.dll", "Tl.Gen.CSharp.dll", "Tl.Gen.dll"},
    "Tl.Gen.C": {"Microsoft.CodeAnalysis.CSharp.dll", "Microsoft.CodeAnalysis.dll", "Tl.Compiler.dll", "Tl.Gen.CSharp.dll", "Tl.Gen.dll"},
    "Tl.Gen.CSharp": {"Tl.Compiler.dll", "Tl.Gen.C.dll"},
    "Tl.Runtime": {"Microsoft.CodeAnalysis.CSharp.dll", "Microsoft.CodeAnalysis.dll", "Tl.Compiler.dll", "Tl.Gen.C.dll", "Tl.Gen.CSharp.dll", "Tl.Gen.dll"},
}

SYMBOL_FILES = {
    "Tl.Compiler": "lib/net10.0/Tl.Compiler.pdb",
    "Tl.Gen.C": "lib/net10.0/Tl.Gen.C.pdb",
    "Tl.Runtime": "lib/net10.0/Tl.Core.pdb",
}

PACKAGE_METADATA_FILES = {
    "_rels/.rels",
    "README.md",
    "[Content_Types].xml",
    "package/services/metadata/core-properties/nuget.psmdcp",
}

SYMBOL_METADATA_FILES = {
    "_rels/.rels",
    "[Content_Types].xml",
    "package/services/metadata/core-properties/nuget.psmdcp",
}

EMBEDDED_SOURCE_GUID = uuid.UUID("0e8a571b-6926-466e-b4ad-8ab04611f5fe").bytes_le


def fail(message):
    raise ValueError(message)


def local_name(element):
    return element.tag.rsplit("}", 1)[-1]


def descendants(element, name):
    return [item for item in element.iter() if local_name(item) == name]


def canonicalize(source, destination):
    payload = bytearray(source.read_bytes())
    end = payload.rfind(b"PK\x05\x06")
    if end < 0 or len(payload) < end + 22:
        fail(f"{source} has no ZIP end record")
    entries = struct.unpack_from("<H", payload, end + 10)[0]
    offset = struct.unpack_from("<I", payload, end + 16)[0]
    if entries == 0xFFFF or offset == 0xFFFFFFFF:
        fail(f"{source} uses unsupported ZIP64 metadata")
    for _ in range(entries):
        if payload[offset:offset + 4] != b"PK\x01\x02":
            fail(f"{source} has an invalid central directory")
        name_bytes, extra_bytes, comment_bytes = struct.unpack_from("<HHH", payload, offset + 28)
        local_offset = struct.unpack_from("<I", payload, offset + 42)[0]
        if payload[local_offset:local_offset + 4] != b"PK\x03\x04":
            fail(f"{source} has an invalid local entry")
        struct.pack_into("<HH", payload, local_offset + 10, 0, 33)
        struct.pack_into("<HH", payload, offset + 12, 0, 33)
        offset += 46 + name_bytes + extra_bytes + comment_bytes
    destination.write_bytes(payload)
    with zipfile.ZipFile(destination) as archive:
        invalid = archive.testzip()
        if invalid is not None:
            fail(f"{destination} contains a corrupt entry: {invalid}")


def canonicalize_directory(source, destination):
    destination.mkdir(parents=True, exist_ok=False)
    archives = sorted([*source.glob("*.nupkg"), *source.glob("*.snupkg")])
    if not archives:
        fail(f"{source} contains no packages")
    for archive in archives:
        canonicalize(archive, destination / archive.name)


def nuspec(archive):
    names = archive.namelist()
    candidates = [name for name in names if name.endswith(".nuspec")]
    if len(candidates) != 1:
        fail(f"{archive.filename} must contain exactly one nuspec")
    return ElementTree.fromstring(archive.read(candidates[0]))


def metadata_value(root, name):
    matches = descendants(root, name)
    if len(matches) != 1 or matches[0].text is None:
        fail(f"nuspec must contain exactly one {name}")
    return matches[0].text


def verify_portable_embedded_pdb(payload, name):
    if not payload.startswith(b"BSJB"):
        fail(f"{name} is not a portable PDB")
    if EMBEDDED_SOURCE_GUID not in payload:
        fail(f"{name} has no embedded-source debug records")


def dependency_groups(root):
    containers = descendants(root, "dependencies")
    if not containers:
        return []
    if len(containers) != 1:
        fail("nuspec must contain at most one dependencies element")
    groups = []
    for group in containers[0]:
        if local_name(group) != "group":
            fail("nuspec dependencies must use framework groups")
        dependencies = []
        for dependency in group:
            if local_name(dependency) != "dependency":
                fail("nuspec dependency group contains an unsupported element")
            dependencies.append(dict(dependency.attrib))
        groups.append({"attributes": dict(group.attrib), "dependencies": dependencies})
    return groups


def verify_nupkg(path, package_id, version, commit, repository_ref):
    with zipfile.ZipFile(path) as archive:
        names = set(archive.namelist())
        expected_names = PACKAGE_FILES[package_id] | PACKAGE_METADATA_FILES | {f"{package_id}.nuspec"}
        if names != expected_names:
            fail(f"{path.name} files are {sorted(names)}, expected {sorted(expected_names)}")
        basenames = {Path(name).name for name in names}
        forbidden = PACKAGE_FORBIDDEN_FILES[package_id] & basenames
        if forbidden:
            fail(f"{path.name} contains forbidden files {sorted(forbidden)}")
        root = nuspec(archive)
        if metadata_value(root, "id") != package_id:
            fail(f"{path.name} has the wrong package ID")
        if metadata_value(root, "version") != version:
            fail(f"{path.name} has the wrong version")
        repositories = descendants(root, "repository")
        if len(repositories) != 1:
            fail(f"{path.name} must contain one repository identity")
        repository = repositories[0].attrib
        expected_repository = {
            "type": "git",
            "url": "https://github.com/IAFahim/tl",
            "branch": repository_ref,
            "commit": commit,
        }
        if repository != expected_repository:
            fail(f"{path.name} repository identity is {repository}, expected {expected_repository}")
        expected_groups = [
            {
                "attributes": group["attributes"],
                "dependencies": [dependency | {"version": version} for dependency in group["dependencies"]],
            }
            for group in PACKAGE_DEPENDENCY_GROUPS[package_id]
        ]
        actual_groups = dependency_groups(root)
        if actual_groups != expected_groups:
            fail(f"{path.name} dependency groups are {actual_groups}, expected {expected_groups}")
        generator_pdb = "tools/net10.0/any/Tl.Gen.CSharp.pdb"
        if generator_pdb in names:
            verify_portable_embedded_pdb(archive.read(generator_pdb), f"{path.name}:{generator_pdb}")


def verify_snupkg(path, package_id, version, commit, repository_ref):
    with zipfile.ZipFile(path) as archive:
        symbol_file = SYMBOL_FILES[package_id]
        expected_names = SYMBOL_METADATA_FILES | {f"{package_id}.nuspec", symbol_file}
        names = set(archive.namelist())
        if names != expected_names:
            fail(f"{path.name} files are {sorted(names)}, expected {sorted(expected_names)}")
        verify_portable_embedded_pdb(archive.read(symbol_file), f"{path.name}:{symbol_file}")
        root = nuspec(archive)
        if metadata_value(root, "id") != package_id or metadata_value(root, "version") != version:
            fail(f"{path.name} symbol identity does not match its package")
        repositories = descendants(root, "repository")
        if len(repositories) != 1:
            fail(f"{path.name} must contain one repository identity")
        repository = repositories[0].attrib
        if repository.get("commit") != commit or repository.get("branch") != repository_ref:
            fail(f"{path.name} repository identity does not match the release")


def verify_packages(directory, version, commit, repository_ref):
    nupkgs = {path.name: path for path in directory.glob("*.nupkg")}
    expected_nupkgs = {f"{package_id}.{version}.nupkg" for package_id in PACKAGE_FILES}
    if set(nupkgs) != expected_nupkgs:
        fail(f"package set is {sorted(nupkgs)}, expected {sorted(expected_nupkgs)}")
    for package_id in sorted(PACKAGE_FILES):
        verify_nupkg(nupkgs[f"{package_id}.{version}.nupkg"], package_id, version, commit, repository_ref)
    snupkgs = {path.name: path for path in directory.glob("*.snupkg")}
    expected_snupkgs = {f"{package_id}.{version}.snupkg" for package_id in SYMBOL_FILES}
    if set(snupkgs) != expected_snupkgs:
        fail(f"symbol package set is {sorted(snupkgs)}, expected {sorted(expected_snupkgs)}")
    for package_id in sorted(SYMBOL_FILES):
        verify_snupkg(snupkgs[f"{package_id}.{version}.snupkg"], package_id, version, commit, repository_ref)


def digest(path):
    checksum = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            checksum.update(block)
    return checksum.hexdigest()


def write_manifest(directory, phase, tag, version, commit, sdk, runtime_identifier):
    excluded = {"RELEASE-MANIFEST.json", "SHA256SUMS"}
    files = [path for path in directory.iterdir() if path.is_file() and path.name not in excluded]
    artifacts = [
        {"path": path.name, "bytes": path.stat().st_size, "sha256": digest(path)}
        for path in sorted(files, key=lambda item: item.name)
    ]
    manifest = {
        "schemaVersion": 1,
        "phase": phase,
        "tag": tag,
        "version": version,
        "commit": commit,
        "dotnetSdk": sdk,
        "runtimeIdentifier": runtime_identifier,
        "artifacts": artifacts,
    }
    manifest_path = directory / "RELEASE-MANIFEST.json"
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8", newline="\n")
    checksum_files = [*files, manifest_path]
    checksum_lines = [f"{digest(path)}  {path.name}" for path in sorted(checksum_files, key=lambda item: item.name)]
    (directory / "SHA256SUMS").write_text("\n".join(checksum_lines) + "\n", encoding="utf-8", newline="\n")


def parse_arguments():
    parser = argparse.ArgumentParser()
    commands = parser.add_subparsers(dest="command", required=True)
    canonical = commands.add_parser("canonicalize")
    canonical.add_argument("source", type=Path)
    canonical.add_argument("destination", type=Path)
    verify = commands.add_parser("verify")
    verify.add_argument("directory", type=Path)
    verify.add_argument("version")
    verify.add_argument("commit")
    verify.add_argument("repository_ref")
    manifest = commands.add_parser("manifest")
    manifest.add_argument("directory", type=Path)
    manifest.add_argument("phase")
    manifest.add_argument("tag")
    manifest.add_argument("version")
    manifest.add_argument("commit")
    manifest.add_argument("sdk")
    manifest.add_argument("runtime_identifier")
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    if arguments.command == "canonicalize":
        canonicalize_directory(arguments.source, arguments.destination)
    elif arguments.command == "verify":
        verify_packages(arguments.directory, arguments.version, arguments.commit, arguments.repository_ref)
    else:
        write_manifest(
            arguments.directory,
            arguments.phase,
            arguments.tag,
            arguments.version,
            arguments.commit,
            arguments.sdk,
            arguments.runtime_identifier,
        )


if __name__ == "__main__":
    try:
        main()
    except ValueError as error:
        print(error, file=sys.stderr)
        raise SystemExit(2) from error
