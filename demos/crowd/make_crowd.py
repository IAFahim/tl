import json
from pathlib import Path

here = Path(__file__).parent
samples = 16
ramp = [i / (samples - 1) for i in range(samples)]
triangle = [1.0 - abs(2.0 * i / (samples - 1) - 1.0) for i in range(samples)]


def clip(kind, start, end, data):
    return {"namespace": "Tl.Pack.Motion", "type": kind, "start": start, "end": end, "data": data}


def track(kind, clips):
    return {"namespace": "Tl", "type": "TlNoSettings<Tl.Pack.Motion.%s>" % kind, "data": {}, "clips": clips}


def v(x, y, z):
    return {"x": x, "y": y, "z": z}


def rgba(r, g, b, a):
    return {"R": r, "G": g, "B": b, "A": a}


hop = {"From": v(0, 0, 0), "To": v(0, 0.6, 0), "Ease": triangle, "Priority": 0}
dance = {
    "schema": 1,
    "name": "dance",
    "duration": 120,
    "loop": True,
    "tracks": [
        track("MoveClip", [clip("MoveClip", 0, 30, hop), clip("MoveClip", 60, 90, hop)]),
        track("SpinClip", [clip("SpinClip", 30, 60, {"FromYaw": 0, "ToYaw": 360, "Ease": ramp})]),
        track("TintClip", [clip("TintClip", 0, 120, {"From": rgba(1, 0, 0, 1), "To": rgba(1, 1, 0, 1), "Ease": triangle})]),
    ],
}
(here / "dance.json").write_text(json.dumps(dance, indent=2) + "\n", encoding="utf-8")
(here / "tl.project.json").write_text(json.dumps({"schema": 1, "tickRate": 60, "channels": [], "timelines": ["dance.json"]}, indent=2) + "\n", encoding="utf-8")

side = 25
entities = []
for i in range(side * side):
    x, z = i % side, i // side
    start = (x * 7 + z * 11) % 120
    entities.append({
        "name": "dancer%03d" % i,
        "states": [
            {"namespace": "Tl.Pack.Motion", "type": "Pose", "data": {"Position": v(x, 0, z), "Scale": v(1, 1, 1), "Yaw": 0, "Has": 0}},
            {"namespace": "Tl.Pack.Motion", "type": "Tint", "data": {"Color": rgba(1, 1, 1, 1), "Has": 0}},
        ],
        "play": [{"timeline": "dance", "tick": start}] if start else [{"timeline": "dance"}],
    })
scene = {"schema": 1, "project": "tl.project.json", "entities": entities, "direction": [{"step": 300, "forward": False}, {"step": 420, "forward": True}]}
(here / "crowd.scene.json").write_text(json.dumps(scene, indent=2) + "\n", encoding="utf-8")
