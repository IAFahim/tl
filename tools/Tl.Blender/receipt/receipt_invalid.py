import os
import re
import shutil
import subprocess
import sys
import tempfile

import bpy

RECEIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ADDON_DIR = os.path.dirname(RECEIPT_DIR)
REPO = os.path.dirname(os.path.dirname(ADDON_DIR))
BAKE_DLL = os.path.join(REPO, "tools", "Tl.Bake", "bin", "Release", "net10.0", "Tl.Bake.dll")
ASSEMBLY_DLL = os.path.join(REPO, "tools", "Tl.Bake.Tests", "bin", "Release", "net10.0", "Tl.Bake.Tests.dll")

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else tempfile.mkdtemp(prefix="tl111_receipt3_")


def fail(message):
    print("RECEIPT3 FAIL: " + message)
    raise SystemExit(1)


module_parent = tempfile.mkdtemp(prefix="tl111_addon_invalid_")
module_name = "tl_blender_bridge"
shutil.copytree(ADDON_DIR, os.path.join(module_parent, module_name))
sys.path.insert(0, module_parent)

bpy.ops.preferences.addon_enable(module=module_name)
prefs = bpy.context.preferences.addons[module_name].preferences
prefs.bake_command = "dotnet " + BAKE_DLL
prefs.namespace = "Tlb"
prefs.assembly_paths = ASSEMBLY_DLL

scene = bpy.context.scene
mesh = bpy.data.meshes.new("InvalidMesh")
obj = bpy.data.objects.new("InvalidCube", mesh)
scene.collection.objects.link(obj)
obj["tl_nla"] = {
    "tracks": {"hits": {"tl_track": "NoSuchTrack"}},
    "strips": {"hit": {"tl_clip": "NoSuchClip"}},
}
obj.location = (0, 0, 0)
obj.keyframe_insert(data_path="location", index=0, frame=1)
obj.location = (1, 0, 0)
obj.keyframe_insert(data_path="location", index=0, frame=13)
animation = obj.animation_data
track = animation.nla_tracks.new()
track.name = "hits"
strip = track.strips.new(name="hit", start=1, action=animation.action)
strip.frame_end = 13
strip.influence = 1.0

bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)

try:
    result = bpy.ops.tl.export_timelines("EXEC_DEFAULT", directory=OUT, bake=True)
    if result != {"CANCELLED"}:
        fail("operator returned %s, expected CANCELLED from the failed bake" % result)
except RuntimeError as error:
    print("OPERATOR FAILED AS EXPECTED: %s" % error)

json_path = os.path.join(OUT, "InvalidCube.json")
if not os.path.exists(json_path):
    fail("exported JSON missing")
with open(json_path, "r", encoding="utf-8") as handle:
    if "NoSuchTrack" not in handle.read():
        fail("invalid type name did not flow into the JSON")

tlb_path = os.path.join(OUT, "InvalidCube.tlb")
if os.path.exists(tlb_path):
    fail("a failed bake must not leave a .tlb")

cli = subprocess.run(
    ["dotnet", BAKE_DLL, json_path, tlb_path, "--assembly", ASSEMBLY_DLL],
    capture_output=True,
    text=True,
    check=False,
)
diagnostic = (cli.stderr or "").strip()
if cli.returncode == 0 or "unknown/unresolvable type" not in diagnostic:
    fail("CLI did not produce the type diagnostic: %r" % diagnostic)
print("ROUNDTRIP DIAGNOSTIC: %s" % diagnostic)

duplicate_json = os.path.join(OUT, "duplicate_field.json")
with open(duplicate_json, "w", encoding="utf-8") as handle:
    handle.write('{"duration": 1, "duration": 1, "tracks": []}')
duplicate_tlb = os.path.join(OUT, "duplicate_field.tlb")
located = subprocess.run(
    ["dotnet", BAKE_DLL, duplicate_json, duplicate_tlb, "--assembly", ASSEMBLY_DLL],
    capture_output=True,
    text=True,
    check=False,
)
located_diagnostic = (located.stderr or "").strip()
if not re.search(r"\[\d+:\d+\]", located_diagnostic):
    fail("CLI did not produce a located diagnostic: %r" % located_diagnostic)
print("LOCATED DIAGNOSTIC: %s" % located_diagnostic)
print("RECEIPT3 OK (output dir: %s)" % OUT)
