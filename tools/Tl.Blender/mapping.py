import dataclasses
import json
import math
from typing import Any, Callable

TICK_CAP = 65535
AMOUNT_DATA_KEY = "Amount"
MIN_BLENDER_VERSION = (5, 0, 0)

SEVERITY_INFO = "INFO"
SEVERITY_WARNING = "WARNING"
SEVERITY_ERROR = "ERROR"


class MappingError(Exception):
    pass


@dataclasses.dataclass(frozen=True)
class Prefs:
    default_track_type: str = ""
    default_clip_type: str = ""
    namespace: str = ""
    bake_command: str = "dotnet"
    assembly_paths: tuple[str, ...] = ()


@dataclasses.dataclass(frozen=True)
class Export:
    files: dict[str, str]
    report: tuple[tuple[str, str], ...]


@dataclasses.dataclass(frozen=True)
class _Clip:
    name: str
    type_name: str
    amount: float
    start: int
    end: int


@dataclasses.dataclass(frozen=True)
class _Track:
    name: str
    type_name: str
    clips: tuple[_Clip, ...]


@dataclasses.dataclass(frozen=True)
class _Timeline:
    name: str
    namespace: str
    duration: int
    loop: bool
    tracks: tuple[_Track, ...]


def export_objects(scene: Any, objects: Any, prefs: Prefs, blender_version: tuple[int, int, int]) -> Export:
    _require_blender_version(blender_version)
    report = Report()
    files: dict[str, str] = {}
    taken: dict[str, str] = {}
    for obj in sorted(objects, key=lambda item: item.name):
        timeline = _build_timeline(scene, obj, prefs, report)
        filename = _safe_filename(obj.name, taken)
        taken[filename] = obj.name
        files[filename + ".json"] = render_timeline(timeline)
    return Export(files, tuple(report.lines))


def render_timeline(timeline: _Timeline) -> str:
    root = _Obj(
        [
            ("name", _quote(timeline.name)),
            ("duration", "%d" % timeline.duration),
            ("loop", "true" if timeline.loop else "false"),
            ("tracks", _Arr(_track_value(track, timeline.namespace) for track in timeline.tracks)),
        ]
    )
    return _render(root, 0) + "\n"


class _Obj(list):
    pass


class _Arr(list):
    pass


def _track_value(track: _Track, namespace: str) -> _Obj:
    return _Obj(
        [
            ("name", _quote(track.name)),
            ("namespace", _quote(namespace)),
            ("type", _quote(track.type_name)),
            ("clips", _Arr(_clip_value(clip, namespace) for clip in track.clips)),
        ]
    )


def _clip_value(clip: _Clip, namespace: str) -> _Obj:
    return _Obj(
        [
            ("name", _quote(clip.name)),
            ("namespace", _quote(namespace)),
            ("type", _quote(clip.type_name)),
            ("data", _Obj([(AMOUNT_DATA_KEY, repr(clip.amount))])),
            ("start", "%d" % clip.start),
            ("end", "%d" % clip.end),
        ]
    )


def _render(value: Any, indent: int) -> str:
    pad = "  " * indent
    inner = "  " * (indent + 1)
    if isinstance(value, _Obj):
        if not value:
            return "{}"
        body = ",\n".join(inner + _quote(key) + ": " + _render(item, indent + 1) for key, item in value)
        return "{\n" + body + "\n" + pad + "}"
    if isinstance(value, _Arr):
        if not value:
            return "[]"
        body = ",\n".join(inner + _render(item, indent + 1) for item in value)
        return "[\n" + body + "\n" + pad + "]"
    return value


def _quote(text: str) -> str:
    return json.dumps(text)


def _require_blender_version(blender_version: tuple[int, int, int]) -> None:
    if blender_version < MIN_BLENDER_VERSION:
        raise MappingError(
            "tl export requires Blender %d.%d or newer (found %d.%d.%d); older releases do not expose the "
            "5.x strip bounds this bridge maps"
            % (
                MIN_BLENDER_VERSION[0],
                MIN_BLENDER_VERSION[1],
                blender_version[0],
                blender_version[1],
                blender_version[2],
            )
        )


def _safe_filename(name: str, taken: dict[str, str]) -> str:
    safe = name.replace("/", "_").replace("\\", "_")
    if safe in taken:
        raise MappingError(
            "object '%s': file name '%s' collides with object '%s'" % (name, safe, taken[safe])
        )
    return safe


def _object_error(obj: Any, reason: str) -> MappingError:
    return MappingError("object '%s': %s" % (obj.name, reason))


def _track_error(obj: Any, track: Any, reason: str) -> MappingError:
    return MappingError("track '%s' on object '%s': %s" % (track.name, obj.name, reason))


def _strip_error(obj: Any, track: Any, strip: Any, reason: str) -> MappingError:
    return MappingError(
        "strip '%s' on track '%s' of object '%s': %s" % (strip.name, track.name, obj.name, reason)
    )


class Report:
    def __init__(self) -> None:
        self.lines: list[tuple[str, str]] = []

    def info(self, message: str) -> None:
        self.lines.append((SEVERITY_INFO, message))

    def warning(self, message: str) -> None:
        self.lines.append((SEVERITY_WARNING, message))


def _build_timeline(scene: Any, obj: Any, prefs: Prefs, report: Report) -> _Timeline:
    animation = getattr(obj, "animation_data", None)
    if animation is None:
        raise _object_error(obj, "has no animation data; add NLA tracks before exporting")
    tracks = list(animation.nla_tracks)
    if not tracks:
        raise _object_error(obj, "has no NLA tracks")
    settings = _read_nla_settings(obj)
    settings_tracks = _settings_section(obj, settings, "tracks")
    settings_strips = _settings_section(obj, settings, "strips")
    loop = _read_loop(obj, report)
    track_names = [track.name for track in tracks]
    _reject_duplicates(track_names, lambda name: _object_error(obj, "has duplicate NLA track name '%s'" % name))
    strip_names = [strip.name for track in tracks for strip in track.strips]
    _reject_duplicates(
        strip_names,
        lambda name: _object_error(obj, "has duplicate strip name '%s' across its NLA tracks" % name),
    )
    mapped: list[_Track] = []
    for track in tracks:
        if track.mute:
            report.warning("track '%s' on object '%s': muted, skipped" % (track.name, obj.name))
            continue
        mapped.append(_build_track(scene, obj, track, prefs, settings_tracks, settings_strips, report))
    if not mapped:
        raise _object_error(obj, "has no included (unmuted) NLA tracks")
    duration = max(max(clip.end for clip in track.clips) for track in mapped)
    duration = max(duration, 1)
    if duration > TICK_CAP:
        raise _object_error(
            obj, "duration %d exceeds the %d-tick cap; shorten the strips" % (duration, TICK_CAP)
        )
    return _Timeline(obj.name, prefs.namespace, duration, loop, tuple(mapped))


def _read_loop(obj: Any, report: Report) -> bool:
    value = obj.get("tl_loop")
    if value is None:
        report.info("object '%s': tl_loop missing, defaulted to false" % obj.name)
        return False
    if not isinstance(value, bool):
        raise _object_error(obj, "tl_loop must be a bool, got %s" % type(value).__name__)
    return value


def _read_nla_settings(obj: Any) -> dict[str, Any]:
    raw = obj.get("tl_nla")
    if raw is None:
        return {}
    mapping = _as_mapping(obj, raw, "tl_nla")
    for key in mapping:
        if key not in ("tracks", "strips"):
            raise _object_error(
                obj, "tl_nla has unknown key '%s' (expected 'tracks' and 'strips')" % key
            )
    return mapping


def _settings_section(obj: Any, settings: dict[str, Any], section: str) -> dict[str, dict[str, Any]]:
    if section not in settings:
        return {}
    mapping = _as_mapping(obj, settings[section], "tl_nla.%s" % section)
    return {key: dict(_as_mapping(obj, value, "tl_nla.%s['%s']" % (section, key))) for key, value in mapping.items()}


def _as_mapping(obj: Any, value: Any, label: str) -> dict[str, Any]:
    if isinstance(value, dict):
        return value
    if hasattr(value, "items"):
        return dict(value.items())
    raise _object_error(obj, "%s must be a property group, got %s" % (label, type(value).__name__))


def _build_track(
    scene: Any,
    obj: Any,
    track: Any,
    prefs: Prefs,
    settings_tracks: dict[str, dict[str, Any]],
    settings_strips: dict[str, dict[str, Any]],
    report: Report,
) -> _Track:
    strips = list(track.strips)
    if not strips:
        raise _track_error(obj, track, "has no strips")
    track_settings = settings_tracks.get(track.name, {})
    for key in track_settings:
        if key != "tl_track":
            raise _track_error(
                obj, track, "tl_nla.tracks['%s'] has unknown key '%s' (expected 'tl_track')" % (track.name, key)
            )
    track_type, defaulted = _read_type(
        obj, track, None, track_settings, "tl_track", prefs.default_track_type,
        "needs a track type: set tl_nla.tracks['%s'].tl_track or the default track type preference" % track.name,
    )
    if defaulted:
        report.info("track '%s' on object '%s': track type defaulted to '%s'" % (track.name, obj.name, track_type))
    clips = tuple(
        _build_clip(scene, obj, track, strip, prefs, settings_strips, report) for strip in strips
    )
    return _Track(track.name, track_type, clips)


def _build_clip(
    scene: Any,
    obj: Any,
    track: Any,
    strip: Any,
    prefs: Prefs,
    settings_strips: dict[str, dict[str, Any]],
    report: Report,
) -> _Clip:
    _report_ignored_strip_settings(obj, track, strip, report)
    start = _read_bound(obj, track, strip, strip.frame_start, scene.frame_start, "start")
    end = _read_bound(obj, track, strip, strip.frame_end, scene.frame_start, "end")
    if end <= start:
        raise _strip_error(obj, track, strip, "window [%d, %d) is empty" % (start, end))
    strip_settings = settings_strips.get(strip.name, {})
    for key in strip_settings:
        if key not in ("tl_clip", "tl_amount"):
            raise _strip_error(
                obj, track, strip,
                "tl_nla.strips['%s'] has unknown key '%s' (expected 'tl_clip' and 'tl_amount')"
                % (strip.name, key),
            )
    clip_type, defaulted = _read_type(
        obj, track, strip, strip_settings, "tl_clip", prefs.default_clip_type,
        "needs a clip type: set tl_nla.strips['%s'].tl_clip or the default clip type preference" % strip.name,
    )
    if defaulted:
        report.info(
            "strip '%s' on track '%s' of object '%s': clip type defaulted to '%s'"
            % (strip.name, track.name, obj.name, clip_type)
        )
    amount = _read_amount(obj, track, strip, strip_settings)
    return _Clip(strip.name, clip_type, amount, start, end)


def _read_type(
    obj: Any,
    track: Any,
    strip: Any,
    settings: dict[str, Any],
    key: str,
    default: str,
    rejection_reason: str,
) -> tuple[str, bool]:
    if key in settings:
        value = settings[key]
        if not isinstance(value, str):
            raise _setting_error(obj, track, strip, "%s must be a string, got %s" % (key, type(value).__name__))
        if not value:
            raise _setting_error(obj, track, strip, "%s is empty; set a bare type name or remove the entry" % key)
        return value, False
    if default:
        return default, True
    raise _setting_error(obj, track, strip, rejection_reason)


def _setting_error(obj: Any, track: Any, strip: Any, reason: str) -> MappingError:
    if strip is None:
        return _track_error(obj, track, reason)
    return _strip_error(obj, track, strip, reason)


def _read_bound(obj: Any, track: Any, strip: Any, frame: Any, scene_start: Any, label: str) -> int:
    tick = float(frame) - float(scene_start)
    if not tick.is_integer():
        raise _strip_error(
            obj, track, strip,
            "%s bound at frame %s is a subframe; NLA strip bounds must land on whole frames" % (label, frame),
        )
    if tick < 0:
        raise _strip_error(
            obj, track, strip,
            "%s bound at frame %s lies before the scene start frame %s" % (label, frame, scene_start),
        )
    return int(tick)


def _read_amount(obj: Any, track: Any, strip: Any, strip_settings: dict[str, Any]) -> float:
    if "tl_amount" in strip_settings:
        value = strip_settings["tl_amount"]
        if isinstance(value, bool) or not isinstance(value, (int, float)):
            raise _strip_error(obj, track, strip, "tl_amount must be a number, got %s" % type(value).__name__)
        amount = float(value)
        if not math.isfinite(amount):
            raise _strip_error(obj, track, strip, "tl_amount must be finite, got %r" % value)
        return amount
    if _influence_is_animated(obj, strip):
        raise _strip_error(
            obj, track, strip,
            "influence is animated; set tl_nla.strips['%s'].tl_amount to a constant instead" % strip.name,
        )
    amount = float(strip.influence)
    if not math.isfinite(amount):
        raise _strip_error(obj, track, strip, "influence must be finite, got %r" % strip.influence)
    return amount


def _influence_is_animated(obj: Any, strip: Any) -> bool:
    if getattr(strip, "use_animated_influence", False):
        return True
    for fcurve in getattr(strip, "fcurves", ()):
        if getattr(fcurve, "data_path", "") == "influence":
            return True
    animation = getattr(obj, "animation_data", None)
    token = 'strips["%s"].influence' % strip.name
    for fcurve in getattr(animation, "drivers", ()) or ():
        if token in getattr(fcurve, "data_path", ""):
            return True
    return False


def _report_ignored_strip_settings(obj: Any, track: Any, strip: Any, report: Report) -> None:
    where = "strip '%s' on track '%s' of object '%s'" % (strip.name, track.name, obj.name)
    if getattr(strip, "mute", False):
        report.warning("%s: strip mute has no tl equivalent, exported anyway" % where)
    blend_type = getattr(strip, "blend_type", "REPLACE")
    if blend_type != "REPLACE":
        report.warning("%s: blend_type '%s' has no tl equivalent, ignored" % (where, blend_type))
    extrapolation = getattr(strip, "extrapolation", "HOLD")
    if extrapolation != "HOLD":
        report.warning("%s: extrapolation '%s' has no tl equivalent, ignored" % (where, extrapolation))
    for label, key, default in (
        ("soft trim blend_in", "blend_in", 0.0),
        ("soft trim blend_out", "blend_out", 0.0),
        ("repeat", "repeat", 1.0),
    ):
        value = float(getattr(strip, key, default))
        if value != default:
            report.warning("%s: %s %s has no tl equivalent, ignored" % (where, label, value))
    strip_type = getattr(strip, "type", "CLIP")
    if strip_type != "CLIP":
        report.warning("%s: strip type '%s' has no tl equivalent, exported as bounds only" % (where, strip_type))


def _reject_duplicates(names: list[str], rejection: Callable[[str], MappingError]) -> None:
    seen: set[str] = set()
    for name in names:
        if name in seen:
            raise rejection(name)
        seen.add(name)
