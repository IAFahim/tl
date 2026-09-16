import json
import os
import shutil
import subprocess
import sys
import tempfile

import bpy

RECEIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ADDON_DIR = os.path.dirname(RECEIPT_DIR)
REPO = os.path.dirname(os.path.dirname(ADDON_DIR))
FIXTURE = os.path.join(ADDON_DIR, "fixtures", "receipt_scene.json")
HAND_JSON = os.path.join(ADDON_DIR, "fixtures", "receipt_scene_hand.json")
BAKE_DLL = os.path.join(REPO, "tools", "Tl.Bake", "bin", "Release", "net10.0", "Tl.Bake.dll")
ASSEMBLY_DLL = os.path.join(REPO, "tools", "Tl.Bake.Tests", "bin", "Release", "net10.0", "Tl.Bake.Tests.dll")

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else tempfile.mkdtemp(prefix="tl111_receipt2_")


def fail(message):
    print("RECEIPT2 FAIL: " + message)
    raise SystemExit(1)


for path in (BAKE_DLL, ASSEMBLY_DLL, HAND_JSON):
    if not os.path.exists(path):
        fail("missing required build artifact: " + path)

module_parent = tempfile.mkdtemp(prefix="tl111_addon_")
module_name = "tl_blender_bridge"
shutil.copytree(ADDON_DIR, os.path.join(module_parent, module_name))
sys.path.insert(0, module_parent)

bpy.ops.preferences.addon_enable(module=module_name)
prefs = bpy.context.preferences.addons[module_name].preferences
prefs.default_track_type = ""
prefs.default_clip_type = ""
prefs.namespace = "Tlb"
prefs.bake_command = "dotnet " + BAKE_DLL
prefs.assembly_paths = ASSEMBLY_DLL

scene = bpy.context.scene
mesh = bpy.data.meshes.new("BridgeMesh")
obj = bpy.data.objects.new("BridgeCube", mesh)
scene.collection.objects.link(obj)
obj["tl_loop"] = True
obj["tl_nla"] = {
    "tracks": {
        "hits": {"tl_track": "BlendTrack"},
        "rings": {"tl_track": "BlendTrack"},
    },
    "strips": {
        "hit_a": {"tl_clip": "BlendClip", "tl_amount": 2.5},
        "hit_b": {"tl_clip": "BlendClip"},
        "ring": {"tl_clip": "BlendClip", "tl_amount": 0.75},
    },
}
obj.location = (0, 0, 0)
obj.keyframe_insert(data_path="location", index=0, frame=1)
obj.location = (1, 0, 0)
obj.keyframe_insert(data_path="location", index=0, frame=13)

action = bpy.data.actions.get("BridgeCubeAction")
animation = obj.animation_data
track_hits = animation.nla_tracks.new()
track_hits.name = "hits"
strip_a = track_hits.strips.new(name="hit_a", start=1, action=animation.action)
strip_a.frame_end = 13
strip_a.influence = 1.0
strip_b = track_hits.strips.new(name="hit_b", start=20, action=animation.action)
strip_b.frame_end = 26
strip_b.influence = 0.5

track_rings = animation.nla_tracks.new()
track_rings.name = "rings"
strip_ring = track_rings.strips.new(name="ring", start=3, action=animation.action)
strip_ring.frame_end = 7
strip_ring.influence = 1.0

track_muted = animation.nla_tracks.new()
track_muted.name = "muted_one"
strip_gone = track_muted.strips.new(name="gone", start=20, action=animation.action)
strip_gone.frame_end = 24
track_muted.mute = True

bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)

result = bpy.ops.tl.export_timelines("EXEC_DEFAULT", directory=OUT, bake=True)
if result != {"FINISHED"}:
    fail("operator returned %s" % result)

exported_path = os.path.join(OUT, "BridgeCube.json")
tlb_path = os.path.join(OUT, "BridgeCube.tlb")
hand_tlb_path = os.path.join(OUT, "receipt_scene_hand.tlb")
for path in (exported_path, tlb_path):
    if not os.path.exists(path):
        fail("operator did not produce " + path)

with open(exported_path, "rb") as handle:
    exported_bytes = handle.read()

if os.environ.get("TL_WRITE_FIXTURE") == "1":
    with open(FIXTURE, "wb") as handle:
        handle.write(exported_bytes)
    print("FIXTURE WRITTEN: %s (%d bytes)" % (FIXTURE, len(exported_bytes)))
else:
    with open(FIXTURE, "rb") as handle:
        fixture_bytes = handle.read()
    if exported_bytes != fixture_bytes:
        fail("export does not match the checked-in fixture byte-for-byte")
    print("FIXTURE MATCH: %s byte-identical (%d bytes)" % (FIXTURE, len(fixture_bytes)))

bake_result = subprocess.run(
    ["dotnet", BAKE_DLL, HAND_JSON, hand_tlb_path, "--assembly", ASSEMBLY_DLL],
    capture_output=True,
    text=True,
    check=False,
)
if bake_result.returncode != 0:
    fail("hand-authored bake failed: %s" % bake_result.stderr)

with open(tlb_path, "rb") as handle:
    exported_tlb = handle.read()
with open(hand_tlb_path, "rb") as handle:
    hand_tlb = handle.read()
if exported_tlb != hand_tlb:
    fail("baked .tlb differs from the hand-authored bake (%d vs %d bytes)" % (len(exported_tlb), len(hand_tlb)))
print("BAKE PARITY: BridgeCube.tlb == hand-authored bake (%d bytes)" % len(exported_tlb))

document = json.loads(exported_bytes.decode("utf-8"))
if document["duration"] != 25 or document["loop"] is not True or len(document["tracks"]) != 2:
    fail("unexpected exported document content")

print("RECEIPT2 OK (output dir: %s)" % OUT)
