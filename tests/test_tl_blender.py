import importlib.util
import json
import math
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def _load_module(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


MAPPING = _load_module("tl_blender_mapping", Path("tools") / "Tl.Blender" / "mapping.py")
BAKE = _load_module("tl_blender_bake", Path("tools") / "Tl.Blender" / "bake.py")
INTROSPECT = _load_module("tl_blender_introspect", Path("tools") / "Tl.Blender" / "introspect.py")

REAL_TEST_ASSEMBLY = ROOT / "tools" / "Tl.Bake.Tests" / "bin" / "Release" / "net10.0" / "Tl.Bake.Tests.dll"
REAL_CLI_DLL = ROOT / "tools" / "Tl.Bake" / "bin" / "Release" / "net10.0" / "Tl.Bake.dll"

BLENDER_VERSION = (5, 2, 1)


class Fcurve:
    def __init__(self, data_path):
        self.data_path = data_path


class Strip:
    def __init__(
        self,
        name,
        start,
        end,
        influence=0.0,
        mute=False,
        blend_type="REPLACE",
        extrapolation="HOLD",
        blend_in=0.0,
        blend_out=0.0,
        repeat=1.0,
        strip_type="CLIP",
        fcurves=(),
        use_animated_influence=False,
    ):
        self.name = name
        self.frame_start = start
        self.frame_end = end
        self.influence = influence
        self.mute = mute
        self.blend_type = blend_type
        self.extrapolation = extrapolation
        self.blend_in = blend_in
        self.blend_out = blend_out
        self.repeat = repeat
        self.type = strip_type
        self.fcurves = fcurves
        self.use_animated_influence = use_animated_influence


class Track:
    def __init__(self, name, strips=(), mute=False):
        self.name = name
        self.strips = list(strips)
        self.mute = mute


class AnimationData:
    def __init__(self, tracks=()):
        self.nla_tracks = list(tracks)
        self.drivers = ()


class FakeObject:
    def __init__(self, name, tracks=(), props=None, animated=True):
        self.name = name
        self.animation_data = AnimationData(tracks) if animated else None
        self.props = dict(props or {})

    def get(self, key, default=None):
        return self.props.get(key, default)


class FakeScene:
    def __init__(self, frame_start=1):
        self.frame_start = frame_start


DEFAULT_PREFS = MAPPING.Prefs(default_track_type="GaGlobalTrack", default_clip_type="GaGlobalClip")


def export(objects, scene=None, prefs=DEFAULT_PREFS, version=BLENDER_VERSION):
    return MAPPING.export_objects(scene or FakeScene(), objects, prefs, version)


class HappyPathTests(unittest.TestCase):
    def test_multi_object_multi_track_multi_strip_golden(self):
        cube = FakeObject(
            "Cube",
            tracks=[
                Track(
                    "hits",
                    strips=[
                        Strip("hit_a", 1, 13, influence=0.0),
                        Strip("hit_b", 20, 26, influence=1.0),
                    ],
                ),
                Track("rings", strips=[Strip("ring", 5, 9, influence=0.75)]),
            ],
            props={
                "tl_loop": True,
                "tl_nla": {
                    "tracks": {
                        "hits": {"tl_track": "GaGlobalTrack"},
                        "rings": {"tl_track": "GaGlobalTrack"},
                    },
                    "strips": {
                        "hit_a": {"tl_clip": "GaGlobalClip", "tl_amount": 2.5},
                        "hit_b": {"tl_clip": "GaGlobalClip"},
                        "ring": {"tl_clip": "GaGlobalClip", "tl_amount": 0.75},
                    },
                },
            },
        )
        cone = FakeObject(
            "Cone",
            tracks=[Track("ticks", strips=[Strip("s1", 2, 4, influence=0.5)])],
        )
        exported = export([cube, cone])
        self.assertEqual({"Cube.json", "Cone.json"}, set(exported.files))
        self.assertEqual(
            """{
  "name": "Cube",
  "duration": 25,
  "loop": true,
  "tracks": [
    {
      "name": "hits",
      "namespace": "",
      "type": "GaGlobalTrack",
      "clips": [
        {
          "name": "hit_a",
          "namespace": "",
          "type": "GaGlobalClip",
          "data": {
            "Amount": 2.5
          },
          "start": 0,
          "end": 12
        },
        {
          "name": "hit_b",
          "namespace": "",
          "type": "GaGlobalClip",
          "data": {
            "Amount": 1.0
          },
          "start": 19,
          "end": 25
        }
      ]
    },
    {
      "name": "rings",
      "namespace": "",
      "type": "GaGlobalTrack",
      "clips": [
        {
          "name": "ring",
          "namespace": "",
          "type": "GaGlobalClip",
          "data": {
            "Amount": 0.75
          },
          "start": 4,
          "end": 8
        }
      ]
    }
  ]
}
""",
            exported.files["Cube.json"],
        )
        self.assertEqual(
            """{
  "name": "Cone",
  "duration": 3,
  "loop": false,
  "tracks": [
    {
      "name": "ticks",
      "namespace": "",
      "type": "GaGlobalTrack",
      "clips": [
        {
          "name": "s1",
          "namespace": "",
          "type": "GaGlobalClip",
          "data": {
            "Amount": 0.5
          },
          "start": 1,
          "end": 3
        }
      ]
    }
  ]
}
""",
            exported.files["Cone.json"],
        )

    def test_duration_clamped_to_one(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 2, influence=1.0)])],
            props={"tl_nla": {"strips": {"hit": {"tl_clip": "GaGlobalClip"}}}},
        )
        exported = export([obj])
        self.assertIn('"duration": 1', exported.files["Cube.json"])

    def test_defaults_and_missing_loop_are_reported(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])])
        exported = export([obj])
        info = [message for severity, message in exported.report if severity == MAPPING.SEVERITY_INFO]
        self.assertIn("object 'Cube': tl_loop missing, defaulted to false", info)
        self.assertIn("track 'hits' on object 'Cube': track type defaulted to 'GaGlobalTrack'", info)
        self.assertIn(
            "strip 'hit' on track 'hits' of object 'Cube': clip type defaulted to 'GaGlobalClip'", info
        )

    def test_namespace_preference_is_emitted(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])])
        prefs = MAPPING.Prefs(default_track_type="GaGlobalTrack", default_clip_type="GaGlobalClip", namespace="MyGame")
        exported = export([obj], prefs=prefs)
        self.assertIn('"namespace": "MyGame"', exported.files["Cube.json"])

    def test_json_matches_flat_schema_shape(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5, influence=0.25)])])
        exported = export([obj])
        document = json.loads(exported.files["Cube.json"])
        self.assertEqual({"name", "duration", "loop", "tracks"}, set(document))
        track = document["tracks"][0]
        self.assertEqual({"name", "namespace", "type", "clips"}, set(track))
        clip = track["clips"][0]
        self.assertEqual({"name", "namespace", "type", "data", "start", "end"}, set(clip))
        self.assertEqual({"Amount": 0.25}, clip["data"])
        self.assertIsInstance(clip["start"], int)
        self.assertIsInstance(clip["end"], int)


class DeterminismTests(unittest.TestCase):
    def build(self):
        return FakeObject(
            "Cube",
            tracks=[
                Track(
                    "hits",
                    strips=[Strip("hit_a", 1, 13, influence=0.0), Strip("hit_b", 20, 26, influence=1.0)],
                ),
                Track("rings", strips=[Strip("ring", 5, 9, influence=0.75)]),
            ],
            props={
                "tl_loop": True,
                "tl_nla": {
                    "tracks": {"hits": {"tl_track": "GaGlobalTrack"}},
                    "strips": {"hit_a": {"tl_clip": "GaGlobalClip", "tl_amount": 2.5}},
                },
            },
        )

    def test_identical_scene_produces_identical_bytes(self):
        first = export([self.build()])
        second = export([self.build()])
        self.assertEqual(first.files, second.files)
        self.assertEqual(first.report, second.report)

    def test_object_order_does_not_change_bytes(self):
        cone = FakeObject("Cone", tracks=[Track("t", strips=[Strip("s", 1, 5, influence=1.0)])])
        forward = export([self.build(), cone])
        reverse = export([cone, self.build()])
        self.assertEqual(forward.files, reverse.files)

    def test_key_order_is_stable(self):
        exported = export([self.build()])
        text = exported.files["Cube.json"]
        self.assertLess(text.index('"name"'), text.index('"duration"'))
        self.assertLess(text.index('"duration"'), text.index('"loop"'))
        self.assertLess(text.index('"loop"'), text.index('"tracks"'))
        clip_at = text.index('"hit_a"')
        self.assertLess(text.index('"namespace"', clip_at), text.index('"type"', clip_at))
        self.assertLess(text.index('"type"', clip_at), text.index('"data"', clip_at))
        self.assertLess(text.index('"data"', clip_at), text.index('"start"', clip_at))
        self.assertLess(text.index('"start"', clip_at), text.index('"end"', clip_at))

    def test_float_formatting_is_culture_independent(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"strips": {"hit": {"tl_clip": "GaGlobalClip", "tl_amount": 1234.5678}}}},
        )
        exported = export([obj])
        self.assertIn('"Amount": 1234.5678', exported.files["Cube.json"])
        self.assertNotIn(",", exported.files["Cube.json"].replace('"', "").replace(", ", "\n").replace(",", "\n"))


class VersionTests(unittest.TestCase):
    def test_older_blender_is_rejected(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "requires Blender 5.0"):
            export([FakeObject("Cube")], version=(4, 5, 0))


class StructureRejectionTests(unittest.TestCase):
    def test_object_without_animation_data(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "object 'Cube': has no animation data"):
            export([FakeObject("Cube", animated=False)])

    def test_object_without_nla_tracks(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "object 'Cube': has no NLA tracks"):
            export([FakeObject("Cube")])

    def test_all_tracks_muted(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)]), Track("rings", strips=[Strip("ring", 1, 5)]), ], props={"tl_nla": {"tracks": {}}})
        obj.animation_data.nla_tracks[0].mute = True
        obj.animation_data.nla_tracks[1].mute = True
        with self.assertRaisesRegex(MAPPING.MappingError, r"object 'Cube': has no included \(unmuted\) NLA tracks"):
            export([obj])

    def test_muted_tracks_are_skipped_and_reported(self):
        obj = FakeObject(
            "Cube",
            tracks=[
                Track("muted", strips=[Strip("gone", 1, 5, influence=1.0)], mute=True),
                Track("kept", strips=[Strip("hit", 1, 5)]),
            ],
        )
        exported = export([obj])
        warnings = [message for severity, message in exported.report if severity == MAPPING.SEVERITY_WARNING]
        self.assertIn("track 'muted' on object 'Cube': muted, skipped", warnings)
        document = json.loads(exported.files["Cube.json"])
        self.assertEqual(["kept"], [track["name"] for track in document["tracks"]])

    def test_track_without_strips(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "track 'hits' on object 'Cube': has no strips"):
            export([FakeObject("Cube", tracks=[Track("hits")])])

    def test_duplicate_track_names(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "duplicate NLA track name 'hits'"):
            export([FakeObject("Cube", tracks=[Track("hits", strips=[Strip("a", 1, 5)]), Track("hits", strips=[Strip("b", 1, 5)])])])

    def test_duplicate_strip_names(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "duplicate strip name 'hit' across its NLA tracks"):
            export(
                [
                    FakeObject(
                        "Cube",
                        tracks=[
                            Track("t1", strips=[Strip("hit", 1, 5)]),
                            Track("t2", strips=[Strip("hit", 1, 5)]),
                        ],
                    )
                ]
            )

    def test_filename_collision(self):
        first = FakeObject("A/B", tracks=[Track("t", strips=[Strip("s", 1, 5, influence=1.0)])])
        second = FakeObject("A_B", tracks=[Track("t", strips=[Strip("s", 1, 5, influence=1.0)])])
        with self.assertRaisesRegex(MAPPING.MappingError, "file name 'A_B' collides with object 'A/B'"):
            export([first, second])

    def test_duration_over_cap(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 65540, influence=1.0)])])
        with self.assertRaisesRegex(MAPPING.MappingError, r"duration 65539 exceeds the 65535-tick cap"):
            export([obj])


class TypeRejectionTests(unittest.TestCase):
    def test_missing_track_type(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])])
        with self.assertRaisesRegex(
            MAPPING.MappingError,
            "track 'hits' on object 'Cube': needs a track type",
        ):
            export([obj], prefs=MAPPING.Prefs())

    def test_missing_clip_type(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"tracks": {"hits": {"tl_track": "GaGlobalTrack"}}}},
        )
        with self.assertRaisesRegex(
            MAPPING.MappingError,
            "strip 'hit' on track 'hits' of object 'Cube': needs a clip type",
        ):
            export([obj], prefs=MAPPING.Prefs(default_track_type="GaGlobalTrack"))

    def test_non_string_track_type(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"tracks": {"hits": {"tl_track": 7}}}},
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_track must be a string, got int"):
            export([obj], prefs=MAPPING.Prefs())

    def test_empty_track_type(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"tracks": {"hits": {"tl_track": ""}}}},
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_track is empty"):
            export([obj])

    def test_non_string_clip_type(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"strips": {"hit": {"tl_clip": 2.5}}}},
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_clip must be a string, got float"):
            export([obj], prefs=MAPPING.Prefs(default_track_type="GaGlobalTrack"))


class AmountRejectionTests(unittest.TestCase):
    def obj(self, strip=None, props=None):
        strips = [strip] if strip is not None else [Strip("hit", 1, 5)]
        return FakeObject(
            "Cube",
            tracks=[Track("hits", strips=strips)],
            props=props,
        )

    def test_animated_influence_rejected_without_tl_amount(self):
        strip = Strip("hit", 1, 5, use_animated_influence=True)
        with self.assertRaisesRegex(MAPPING.MappingError, "strip 'hit'.*influence is animated"):
            export([self.obj(strip)], prefs=MAPPING.Prefs(default_track_type="T", default_clip_type="C"))

    def test_animated_influence_fcurve_rejected(self):
        strip = Strip("hit", 1, 5, fcurves=(Fcurve("influence"),))
        with self.assertRaisesRegex(MAPPING.MappingError, "influence is animated"):
            export([self.obj(strip)], prefs=MAPPING.Prefs(default_track_type="T", default_clip_type="C"))

    def test_driver_animated_influence_rejected(self):
        strip = Strip("hit", 1, 5)
        obj = self.obj(strip)
        obj.animation_data.drivers = (Fcurve('nla_tracks["hits"].strips["hit"].influence'),)
        with self.assertRaisesRegex(MAPPING.MappingError, "influence is animated"):
            export([obj], prefs=MAPPING.Prefs(default_track_type="T", default_clip_type="C"))

    def test_tl_amount_overrides_animated_influence(self):
        strip = Strip("hit", 1, 5, use_animated_influence=True)
        obj = self.obj(
            strip,
            props={"tl_nla": {"strips": {"hit": {"tl_clip": "GaGlobalClip", "tl_amount": 3.5}}}},
        )
        exported = export([obj], prefs=MAPPING.Prefs(default_track_type="GaGlobalTrack"))
        self.assertIn('"Amount": 3.5', exported.files["Cube.json"])

    def test_non_numeric_tl_amount(self):
        obj = self.obj(props={"tl_nla": {"strips": {"hit": {"tl_amount": "big"}}}})
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_amount must be a number, got str"):
            export([obj], prefs=MAPPING.Prefs(default_track_type="T", default_clip_type="C"))

    def test_boolean_tl_amount(self):
        obj = self.obj(props={"tl_nla": {"strips": {"hit": {"tl_amount": True}}}})
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_amount must be a number, got bool"):
            export([obj], prefs=MAPPING.Prefs(default_track_type="T", default_clip_type="C"))

    def test_non_finite_tl_amount(self):
        obj = self.obj(props={"tl_nla": {"strips": {"hit": {"tl_amount": math.inf}}}})
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_amount must be finite"):
            export([obj], prefs=MAPPING.Prefs(default_track_type="T", default_clip_type="C"))

    def test_integer_tl_amount_accepted(self):
        obj = self.obj(props={"tl_nla": {"strips": {"hit": {"tl_clip": "GaGlobalClip", "tl_amount": 3}}}})
        exported = export([obj], prefs=MAPPING.Prefs(default_track_type="GaGlobalTrack"))
        self.assertIn('"Amount": 3.0', exported.files["Cube.json"])


class BoundRejectionTests(unittest.TestCase):
    def test_subframe_start_bound(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 2.5, 7, influence=1.0)])])
        with self.assertRaisesRegex(
            MAPPING.MappingError,
            "strip 'hit' on track 'hits' of object 'Cube': start bound at frame 2.5 is a subframe",
        ):
            export([obj])

    def test_subframe_end_bound(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 6.25, influence=1.0)])])
        with self.assertRaisesRegex(MAPPING.MappingError, "end bound at frame 6.25 is a subframe"):
            export([obj])

    def test_bound_before_scene_start(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 0, 7, influence=1.0)])])
        with self.assertRaisesRegex(
            MAPPING.MappingError,
            "start bound at frame 0 lies before the scene start frame 1",
        ):
            export([obj])

    def test_empty_window(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 3, 3, influence=1.0)])])
        with self.assertRaisesRegex(MAPPING.MappingError, r"window \[2, 2\) is empty"):
            export([obj])


class PropertyRejectionTests(unittest.TestCase):
    def test_non_bool_loop(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])], props={"tl_loop": "yes"})
        with self.assertRaisesRegex(MAPPING.MappingError, "object 'Cube': tl_loop must be a bool, got str"):
            export([obj])

    def test_nla_settings_wrong_type(self):
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])], props={"tl_nla": "hits=GaGlobalTrack"})
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_nla must be a property group, got str"):
            export([obj])

    def test_unknown_top_level_key(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"types": {}}},
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "tl_nla has unknown key 'types'"):
            export([obj])

    def test_unknown_track_key(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"tracks": {"hits": {"tl_type": "GaGlobalTrack"}}}},
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "unknown key 'tl_type'"):
            export([obj])

    def test_unknown_strip_key(self):
        obj = FakeObject(
            "Cube",
            tracks=[Track("hits", strips=[Strip("hit", 1, 5)])],
            props={"tl_nla": {"strips": {"hit": {"tl_kind": "GaGlobalClip"}}}},
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "unknown key 'tl_kind'"):
            export([obj])


class IgnoredSettingsTests(unittest.TestCase):
    def test_ignored_settings_are_reported_in_order(self):
        strip = Strip(
            "hit",
            1,
            5,
            influence=1.0,
            mute=True,
            blend_type="ADD",
            extrapolation="NOTHING",
            blend_in=2.0,
            blend_out=3.0,
            repeat=2.0,
            strip_type="TRANSITION",
        )
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[strip])])
        exported = export([obj])
        warnings = [message for severity, message in exported.report if severity == MAPPING.SEVERITY_WARNING]
        self.assertEqual(
            [
                "strip 'hit' on track 'hits' of object 'Cube': strip mute has no tl equivalent, exported anyway",
                "strip 'hit' on track 'hits' of object 'Cube': blend_type 'ADD' has no tl equivalent, ignored",
                "strip 'hit' on track 'hits' of object 'Cube': extrapolation 'NOTHING' has no tl equivalent, ignored",
                "strip 'hit' on track 'hits' of object 'Cube': soft trim blend_in 2.0 has no tl equivalent, ignored",
                "strip 'hit' on track 'hits' of object 'Cube': soft trim blend_out 3.0 has no tl equivalent, ignored",
                "strip 'hit' on track 'hits' of object 'Cube': repeat 2.0 has no tl equivalent, ignored",
                "strip 'hit' on track 'hits' of object 'Cube': strip type 'TRANSITION' has no tl equivalent, exported as bounds only",
            ],
            warnings,
        )

    def test_defaults_produce_no_ignored_warnings(self):
        strip = Strip("hit", 1, 5, influence=1.0)
        obj = FakeObject("Cube", tracks=[Track("hits", strips=[strip])])
        exported = export([obj])
        self.assertEqual([], [message for severity, message in exported.report if severity == MAPPING.SEVERITY_WARNING])


class CompletedFake:
    def __init__(self, returncode=0, stdout="", stderr=""):
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr


class BakeArgvTests(unittest.TestCase):
    def test_command_input_output_and_assemblies(self):
        argv = BAKE.bake_argv(
            "dotnet /tools/Tl.Bake.dll",
            "/out/Cube.json",
            "/out/Cube.tlb",
            ("/a/X.dll", "/b/Y.dll"),
        )
        self.assertEqual(
            ["dotnet", "/tools/Tl.Bake.dll", "/out/Cube.json", "/out/Cube.tlb", "--assembly", "/a/X.dll", "--assembly", "/b/Y.dll"],
            argv,
        )

    def test_empty_command_rejected(self):
        with self.assertRaisesRegex(BAKE.BakeError, "bake command preference is empty"):
            BAKE.bake_argv("   ", "a.json", "a.tlb", ())


class BakeRunTests(unittest.TestCase):
    def test_success_result_and_report(self):
        result = BAKE.run_bake(
            ["tlb", "a.json", "a.tlb"],
            runner=lambda argv: CompletedFake(0, "cache: miss abcd\n", ""),
        )
        self.assertEqual(0, result["exit_code"])
        self.assertTrue(BAKE.succeeded(result))
        lines = BAKE.report_lines(result)
        self.assertEqual(
            [
                (BAKE.SEVERITY_INFO, "bake: tlb a.json a.tlb"),
                (BAKE.SEVERITY_INFO, "bake: cache: miss abcd"),
                (BAKE.SEVERITY_INFO, "bake: exit 0"),
            ],
            lines,
        )

    def test_failure_keeps_diagnostic_with_line_and_column(self):
        result = BAKE.run_bake(
            ["tlb", "a.json", "a.tlb"],
            runner=lambda argv: CompletedFake(
                1,
                "",
                "Diagnostic error: [12:9] unknown/unresolvable type: no loaded type named (<global>, NoSuchTrack) for track 0.\n",
            ),
        )
        self.assertFalse(BAKE.succeeded(result))
        lines = BAKE.report_lines(result)
        self.assertIn((BAKE.SEVERITY_ERROR, "bake: Diagnostic error: [12:9] unknown/unresolvable type: no loaded type named (<global>, NoSuchTrack) for track 0."), lines)
        self.assertIn((BAKE.SEVERITY_ERROR, "bake: exit 1"), lines)

    def test_missing_executable_is_a_bake_error(self):
        def raise_os_error(argv):
            raise FileNotFoundError("dotnet")

        with self.assertRaisesRegex(BAKE.BakeError, "failed to start"):
            BAKE.run_bake(["missing-cli", "a.json", "a.tlb"], runner=raise_os_error)


INTROSPECTION_DOCUMENT = {
    "schemaVersion": 1,
    "assemblies": ["Game"],
    "pairs": [
        {
            "track": {"namespace": "MyGame", "name": "GaTrack", "assembly": "Game", "size": 4, "fields": []},
            "clip": {"namespace": "MyGame", "name": "GaClip", "assembly": "Game", "size": 4, "fields": []},
            "blendable": True,
            "unmanaged": True,
            "trackPairings": ["MyGame.GaClip"],
            "consumers": [],
        },
        {
            "track": {"namespace": "MyGame", "name": "FxTrack", "assembly": "Game", "size": 4, "fields": []},
            "clip": {"namespace": "MyGame", "name": "FxBurst", "assembly": "Game", "size": 8, "fields": []},
            "blendable": True,
            "unmanaged": True,
            "trackPairings": ["MyGame.FxBurst"],
            "consumers": [],
        },
    ],
}


def introspect_runner(completed):
    return lambda argv: completed


class IntrospectArgvTests(unittest.TestCase):
    def test_command_then_json_flag_then_assemblies(self):
        argv = INTROSPECT.introspect_argv("dotnet /tools/Tl.Bake.dll", ("/a/X.dll", "/b/Y.dll"))
        self.assertEqual(
            ["dotnet", "/tools/Tl.Bake.dll", "--json", "--assembly", "/a/X.dll", "--assembly", "/b/Y.dll"],
            argv,
        )

    def test_empty_command_rejected(self):
        with self.assertRaisesRegex(INTROSPECT.IntrospectionError, "bake command preference is empty"):
            INTROSPECT.introspect_argv("   ", ())


class ParsePairsTests(unittest.TestCase):
    def test_single_pair_document(self):
        document = {
            "schemaVersion": 1,
            "pairs": [INTROSPECTION_DOCUMENT["pairs"][0]],
        }
        pairs = INTROSPECT.parse_pairs(document)
        self.assertEqual(1, len(pairs))
        self.assertEqual("MyGame", pairs[0].track_namespace)
        self.assertEqual("GaTrack", pairs[0].track_name)
        self.assertEqual("MyGame", pairs[0].clip_namespace)
        self.assertEqual("GaClip", pairs[0].clip_name)
        self.assertTrue(pairs[0].blendable)
        self.assertTrue(pairs[0].unmanaged)

    def test_wrong_schema_version_rejected(self):
        with self.assertRaisesRegex(INTROSPECT.IntrospectionError, "schemaVersion"):
            INTROSPECT.parse_pairs({"schemaVersion": 2, "pairs": []})

    def test_missing_keys_rejected(self):
        pairs, error = INTROSPECT.run_introspection(
            "tlb",
            ("game.dll",),
            runner=introspect_runner(CompletedFake(0, "{\"schemaVersion\": 1, \"pairs\": [{}]}", "")),
        )
        self.assertEqual((), pairs)
        self.assertIn("malformed", error)


class RunIntrospectionTests(unittest.TestCase):
    def test_no_assemblies_is_a_no_op(self):
        pairs, error = INTROSPECT.run_introspection("tlb", ())
        self.assertEqual((), pairs)
        self.assertEqual("", error)

    def test_successful_document_parses(self):
        pairs, error = INTROSPECT.run_introspection(
            "tlb",
            ("game.dll",),
            runner=introspect_runner(CompletedFake(0, json.dumps(INTROSPECTION_DOCUMENT), "")),
        )
        self.assertEqual("", error)
        self.assertEqual(("GaTrack", "FxTrack"), tuple(pair.track_name for pair in pairs))

    def test_nonzero_exit_reports_stderr(self):
        pairs, error = INTROSPECT.run_introspection(
            "tlb",
            ("game.dll",),
            runner=introspect_runner(CompletedFake(3, "", "boom")),
        )
        self.assertEqual((), pairs)
        self.assertIn("exited 3", error)
        self.assertIn("boom", error)

    def test_invalid_json_reports_error(self):
        pairs, error = INTROSPECT.run_introspection(
            "tlb",
            ("game.dll",),
            runner=introspect_runner(CompletedFake(0, "{not json", "")),
        )
        self.assertEqual((), pairs)
        self.assertIn("invalid JSON", error)

    def test_missing_executable_reports_error(self):
        def raise_os_error(argv):
            raise FileNotFoundError("tlb")

        pairs, error = INTROSPECT.run_introspection("tlb", ("game.dll",), runner=raise_os_error)
        self.assertEqual((), pairs)
        self.assertIn("failed to start", error)


class PairDefaultExportTests(unittest.TestCase):
    def obj(self, props=None):
        return FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])], props=props)

    def test_single_pair_fills_types_and_namespace(self):
        prefs = MAPPING.Prefs(pairs=(MAPPING.TypePair("MyGame", "GaTrack", "MyGame", "GaClip"),))
        exported = export([self.obj()], prefs=prefs)
        document = json.loads(exported.files["Cube.json"])
        track = document["tracks"][0]
        self.assertEqual("MyGame", track["namespace"])
        self.assertEqual("GaTrack", track["type"])
        self.assertEqual("GaClip", track["clips"][0]["type"])
        infos = [message for severity, message in exported.report if severity == MAPPING.SEVERITY_INFO]
        self.assertIn("object 'Cube': namespace 'MyGame' from the consumer assembly introspection", infos)
        self.assertIn("track 'hits' on object 'Cube': track type 'GaTrack' from the consumer assembly introspection", infos)
        self.assertIn(
            "strip 'hit' on track 'hits' of object 'Cube': clip type 'GaClip' from the consumer assembly introspection",
            infos,
        )

    def test_multiple_pairs_still_reject_missing_types(self):
        prefs = MAPPING.Prefs(
            pairs=(
                MAPPING.TypePair("MyGame", "GaTrack", "MyGame", "GaClip"),
                MAPPING.TypePair("MyGame", "FxTrack", "MyGame", "FxBurst"),
            )
        )
        with self.assertRaisesRegex(MAPPING.MappingError, "needs a track type"):
            export([self.obj()], prefs=prefs)

    def test_unmanaged_filter_leaves_one_usable_pair(self):
        prefs = MAPPING.Prefs(
            pairs=(
                MAPPING.TypePair("MyGame", "GaTrack", "MyGame", "GaClip"),
                MAPPING.TypePair("MyGame", "Managed", "MyGame", "GaClip", unmanaged=False),
            )
        )
        exported = export([self.obj()], prefs=prefs)
        document = json.loads(exported.files["Cube.json"])
        self.assertEqual("GaTrack", document["tracks"][0]["type"])

    def test_explicit_clip_type_overrides_pair(self):
        props = {"tl_nla": {"strips": {"hit": {"tl_clip": "HandClip"}}}}
        prefs = MAPPING.Prefs(pairs=(MAPPING.TypePair("MyGame", "GaTrack", "MyGame", "GaClip"),))
        exported = export([self.obj(props)], prefs=prefs)
        document = json.loads(exported.files["Cube.json"])
        self.assertEqual("HandClip", document["tracks"][0]["clips"][0]["type"])

    def test_preference_default_overrides_pair(self):
        prefs = MAPPING.Prefs(default_track_type="PrefTrack", pairs=(MAPPING.TypePair("MyGame", "GaTrack", "MyGame", "GaClip"),))
        exported = export([self.obj()], prefs=prefs)
        document = json.loads(exported.files["Cube.json"])
        self.assertEqual("PrefTrack", document["tracks"][0]["type"])

    def test_namespace_preference_overrides_pair_namespace(self):
        prefs = MAPPING.Prefs(namespace="PrefNs", pairs=(MAPPING.TypePair("MyGame", "GaTrack", "MyGame", "GaClip"),))
        exported = export([self.obj()], prefs=prefs)
        document = json.loads(exported.files["Cube.json"])
        self.assertEqual("PrefNs", document["tracks"][0]["namespace"])

    def test_empty_pairs_keeps_previous_rejection(self):
        with self.assertRaisesRegex(MAPPING.MappingError, "needs a track type"):
            export([self.obj()], prefs=MAPPING.Prefs())


class RealCliIntrospectionTests(unittest.TestCase):
    def obj(self, props=None):
        return FakeObject("Cube", tracks=[Track("hits", strips=[Strip("hit", 1, 5)])], props=props)

    @unittest.skipUnless(
        REAL_CLI_DLL.exists() and REAL_TEST_ASSEMBLY.exists(),
        "built Tl.Bake and Tl.Bake.Tests assemblies are required",
    )
    def test_real_cli_pairs_parse_and_drive_export(self):
        command = "dotnet %s" % REAL_CLI_DLL
        pairs, error = INTROSPECT.run_introspection(command, (str(REAL_TEST_ASSEMBLY),))
        self.assertEqual("", error)
        names = {(pair.track_name, pair.clip_name) for pair in pairs}
        self.assertIn(("JobTrack", "JobClip"), names)
        job = next(pair for pair in pairs if (pair.track_name, pair.clip_name) == ("JobTrack", "JobClip"))
        props = {
            "tl_nla": {
                "tracks": {"hits": {"tl_track": job.track_name}},
                "strips": {"hit": {"tl_clip": job.clip_name}},
            }
        }
        prefs = MAPPING.Prefs(namespace=job.track_namespace, pairs=pairs)
        exported = export([self.obj(props)], prefs=prefs)
        document = json.loads(exported.files["Cube.json"])
        track = document["tracks"][0]
        self.assertEqual("Tlb", track["namespace"])
        self.assertEqual("JobTrack", track["type"])
        self.assertEqual("JobClip", track["clips"][0]["type"])


if __name__ == "__main__":
    unittest.main()
