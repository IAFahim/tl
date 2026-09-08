import argparse
import ctypes
import fcntl
import json
import os
import struct
import subprocess


def open_counter(process, event, group):
    attributes = bytearray(128)
    struct.pack_into("IIQ", attributes, 0, 0, len(attributes), event)
    struct.pack_into("Q", attributes, 32, 11)
    struct.pack_into("Q", attributes, 40, 1 | (1 << 5) | (1 << 6))
    buffer = (ctypes.c_char * len(attributes)).from_buffer(attributes)
    libc = ctypes.CDLL(None, use_errno=True)
    libc.syscall.restype = ctypes.c_long
    descriptor = libc.syscall(298, ctypes.byref(buffer), process, -1, group, 0)
    if descriptor < 0:
        error = ctypes.get_errno()
        raise OSError(error, os.strerror(error))
    return descriptor


def measure(assembly, method, pattern):
    process = subprocess.Popen(
        ["taskset", "-c", "4", "dotnet", assembly, "--counters", method, pattern],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    descriptors = []
    try:
        while True:
            line = process.stdout.readline()
            if not line:
                raise RuntimeError(process.stderr.read())
            if line.startswith("ready "):
                ticks = int(line.split()[1])
                break
        events = {"cycles": 0, "instructions": 1, "branches": 4, "branch_misses": 5}
        for event in events.values():
            descriptors.append(open_counter(process.pid, event, descriptors[0] if descriptors else -1))
        leader = descriptors[0]
        fcntl.ioctl(leader, 0x2403, 1)
        fcntl.ioctl(leader, 0x2400, 1)
        process.stdin.write("go\n")
        process.stdin.flush()
        line = process.stdout.readline()
        if not line.startswith("done "):
            raise RuntimeError(f"Unexpected measured output: {line}; {process.stderr.read()}")
        fcntl.ioctl(leader, 0x2401, 1)
        values = struct.unpack("7Q", os.read(leader, 56))
        count, enabled, running, *counts = values
        if count != 4 or running == 0:
            raise RuntimeError(f"Invalid hardware-counter read: {values}")
        process.stdin.write("stop\n")
        process.stdin.flush()
        process.wait(timeout=30)
        if process.returncode:
            raise RuntimeError(process.stderr.read())
        return {
            "method": method,
            "pattern": pattern,
            "ticks": ticks,
            "scope": "main thread user-space, warmed, includes loop and receipt overhead",
            "time_enabled_ns": enabled,
            "time_running_ns": running,
            "running_fraction": running / enabled,
            "raw_counts": dict(zip(events, counts)),
            "per_tick_scaled": {name: value * enabled / running / ticks for name, value in zip(events, counts)},
            "checksum": line.strip(),
        }
    finally:
        for descriptor in descriptors:
            os.close(descriptor)
        if process.poll() is None:
            process.kill()
            process.wait()


parser = argparse.ArgumentParser()
parser.add_argument("assembly")
parser.add_argument("--output", required=True)
parser.add_argument("--patterns", nargs="+", default=["Sequential", "Random", "Repeated"])
parser.add_argument("--methods", nargs="+", default=["InterpreterSingle", "CompiledSingle", "FusedSingle", "InterpreterBatch8", "CompiledBatch8", "FusedBatch8"])
arguments = parser.parse_args()
results = []
for pattern in arguments.patterns:
    for method in arguments.methods:
        result = measure(arguments.assembly, method, pattern)
        results.append(result)
        print(json.dumps(result), flush=True)
with open(arguments.output, "w") as output:
    json.dump(results, output, indent=2)
    output.write("\n")
