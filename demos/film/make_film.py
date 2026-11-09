"""Builds the Tl films: the video is a Tl timeline.

    python demos/film/make_film.py            # both films; narration is read from audio/ (or made with HeyGen if missing)

For each film (script/<name>.json):
  1. narration: audio/<hash of the words>.mp3 and its word timings (made once with HeyGen TTS; the key is read from the
     environment variable HEYGEN_API_KEY and is never written anywhere; the committed mp3 files make the key optional)
  2. timelines/<name>.json: Film/Scene, Film/Caption, Film/Voice and Film/Beat clips, placed from the narration timings
  3. tl bake, then FilmTl runs the timeline and records what is on screen at every tick (states)
  4. out/<name>/: a HyperFrames project (index.html + assets) whose every frame is read from those states
Then render:  cd demos/film/out/<name> && npx --yes hyperframes@0.9.26 render
"""
import hashlib, json, math, os, re, shutil, subprocess, sys, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FPS = 30
LEAD = 0.5     # seconds of quiet before a scene's voice starts
TAIL = 0.7     # seconds of quiet after it
FILMS = ["release", "tutorial"]   # timeline numbers follow name order: release = 1, tutorial = 2


def run(cmd, **kw):
    print("  $", " ".join(cmd) if isinstance(cmd, list) else cmd)
    subprocess.run(cmd, check=True, **kw)


def audio_key(scene, voice_id):
    """The narration file of a scene is named after a hash of the voice and the words, so editing or inserting scenes never mixes them up."""
    return hashlib.sha1((voice_id + "|" + scene["text"]).encode()).hexdigest()[:10]


def tts(scene, voice_id):
    """Narration for one scene: {duration, words:[(text,start,end)]}. Calls HeyGen only if the mp3 is missing."""
    key_name = audio_key(scene, voice_id)
    mp3 = os.path.join(HERE, "audio", f"{key_name}.mp3")
    meta = os.path.join(HERE, "audio", f"{key_name}.json")
    if os.path.exists(mp3) and os.path.exists(meta):
        return json.load(open(meta, encoding="utf-8"))
    key = os.environ.get("HEYGEN_API_KEY")
    if not key:
        sys.exit(f"missing {mp3} ({scene['text'][:40]}...): set HEYGEN_API_KEY to make it")
    body = json.dumps({
        "model": "heygen-voice-1-turbo-free", "voice_id": voice_id, "language": "en", "text": scene["text"],
        "with_timestamps": True, "output_format": {"encoding": "mp3", "sample_rate_hertz": 44100, "bitrate_bps": 96000},
    }).encode()
    request = urllib.request.Request("https://api.heygen.com/v3/models/audio/tts", body,
                                     {"X-Api-Key": key, "Content-Type": "application/json", "User-Agent": "tl-film/1"})
    with urllib.request.urlopen(request, timeout=120) as response:
        data = json.load(response)["data"]
    with urllib.request.urlopen(data["audio_url"], timeout=120) as response, open(mp3, "wb") as f:
        f.write(response.read())
    info = {"duration": data["duration"], "words": [(w["text"], w["start_time"], w["end_time"]) for w in data["original_words"]]}
    json.dump(info, open(meta, "w", encoding="utf-8"))
    return info


def sentences(text):
    """Sentences with the index of their first word (words split on spaces, like the TTS timing words)."""
    out, count = [], 0
    for s in re.split(r"(?<=[.!?])\s+", text.strip()):
        out.append((s, count))
        count += len(s.split())
    return out, count


def clip(kind, start, end, **data):
    return {"namespace": "Film", "type": kind, "start": start, "end": end, "data": data}


def track(kind, clips):
    return {"namespace": "Tl", "type": f"TlNoSettings<Film.{kind}>", "data": {}, "clips": clips}


def build_timeline(film, script):
    """Places every clip of one film from the narration timings. Returns (timeline, ticks, caption lines, scene table)."""
    os.makedirs(os.path.join(HERE, "audio"), exist_ok=True)
    scenes, captions, beats = [], [], []
    footage_names = []                  # footage ids are 1-based positions in this list
    footage_spans = []                  # (start, end, id): the footage plays while the scene's voice speaks
    cursor, line = 0, 0
    caption_segments = []     # (start, end, line)
    for i, scene in enumerate(script["scenes"], 1):
        info = tts(scene, script["voice_id"])
        lead, tail = round(LEAD * FPS), round(TAIL * FPS)
        voice_len = math.ceil(info["duration"] * FPS)
        length = lead + voice_len + tail
        scene_start, voice_start = cursor, cursor + lead
        sents, word_count = sentences(scene["text"])
        words = info["words"]
        first_line = line + 1
        starts = []
        for text, first_word in sents:
            if len(words) == word_count:      # timing words match the spoken words one to one
                t = words[first_word][1]
            else:                              # numbers or symbols split differently: fall back to a share of the speech
                t = info["duration"] * first_word / max(1, word_count)
            starts.append(voice_start + round(t * FPS))
            line += 1
            captions.append(text)
        scenes.append({"start": scene_start, "end": scene_start + length, "first_line": first_line, "last_line": line,
                       "voice_start": voice_start, "voice_len": voice_len, "n": i, "starts": starts, "audio": audio_key(scene, script["voice_id"])})
        caption_segments.append((scene_start, starts[0], 0))
        for j, s in enumerate(starts):
            end = starts[j + 1] if j + 1 < len(starts) else scene_start + length
            caption_segments.append((s, end, first_line + j))
        if scene.get("footage"):
            if scene["footage"] not in footage_names: footage_names.append(scene["footage"])
            footage_spans.append((voice_start, voice_start + voice_len, footage_names.index(scene["footage"]) + 1))
        beats.append(scene_start)
        beats.extend(starts)
        cursor += length
    ticks = cursor + FPS
    footage_segments, at = [], 0         # a column keeps its last value when no clip covers a tick, so the gaps get explicit "none" clips
    for a, b, footage_id in footage_spans:
        if a > at: footage_segments.append((at, a, 0))
        footage_segments.append((a, b, footage_id))
        at = b
    if ticks > at: footage_segments.append((at, ticks, 0))
    timeline = {
        "schema": 1, "name": film, "duration": ticks,
        "tracks": [
            track("SceneClip", [clip("SceneClip", s["start"], s["end"], Scene=s["n"]) for s in scenes]),
            track("CaptionClip", [clip("CaptionClip", a, b, Line=n) for a, b, n in caption_segments if b > a]),
            track("FootageClip", [clip("FootageClip", a, b, Footage=n) for a, b, n in footage_segments]),
            track("VoiceClip", [clip("VoiceClip", s["voice_start"], s["voice_start"] + s["voice_len"], Line=s["n"]) for s in scenes]),
            track("BeatClip", [clip("BeatClip", b, b + 2, Weight=1) for b in sorted(set(beats))]),
        ],
    }
    return timeline, ticks, captions, scenes, footage_names


def footage_data(names):
    """Recorded Unity footage: footage/<name>.mp4 (one lap, 30 fps) and footage/<name>.csv (frame, tick, x, y, z per video frame)."""
    out = {}
    for name in names:
        rows = [l.split(",") for l in open(os.path.join(HERE, "footage", name + ".csv"), encoding="utf-8").read().strip().splitlines()[1:]]
        out[name] = {"frames": len(rows), "tick": [int(r[1]) for r in rows],
                     "x": [float(r[2]) for r in rows], "y": [float(r[3]) for r in rows], "z": [float(r[4]) for r in rows]}
    return out


def looped_video(name, seconds, destination):
    """The one-lap footage repeated (and cut) to exactly `seconds`, so it plays for as long as the narration does."""
    run(["ffmpeg", "-y", "-loglevel", "error", "-stream_loop", "-1", "-i", os.path.join(HERE, "footage", name + ".mp4"), "-t", f"{seconds:.3f}",
         "-r", str(FPS), "-an", "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p", destination])


def collect_demo_data():
    """Real numbers from real runs: everything the film draws about a demo comes from here, not from the script."""
    def tl_run(scene, steps, every, dll):
        return subprocess.run(["dotnet", "run", "--no-build", "-c", "Release", "--project", "tools/Tl.Bake", "--", "run", scene,
                               "--steps", str(steps), "--checkpoints", str(every), "--show", "--assembly", dll],
                              cwd=ROOT, check=True, capture_output=True, text=True).stdout.splitlines()

    for project in ("packs/Stats", "packs/Inventory", "tools/Tl.Bake", "demos/hello"):
        run(["dotnet", "build", project, "-c", "Release", "-p:NuGetAudit=false", "-v:q", "-nologo"], cwd=ROOT)

    health, attack, step = {}, {}, None
    for l in tl_run("demos/stats/stats.scene.json", 600, 25, "packs/Stats/bin~/Release/net10.0/Tl.Pack.Stats.dll"):
        m = re.match(r"step (\d+) hash=", l)
        if m: step = int(m.group(1))
        m = re.match(r"\s+hero Health: Value (-?\d+), Max (\d+)", l)
        if m: health[step] = (int(m.group(1)), int(m.group(2)))
        m = re.match(r"\s+hero Attack: Base (-?\d+), Bonus (-?\d+)", l)
        if m: attack[step] = (int(m.group(1)), int(m.group(2)))
    stats = [{"step": s, "health": health[s][0], "max": health[s][1], "attack": attack[s][0] + attack[s][1]} for s in sorted(health)]

    bag, step = [], None
    for l in tl_run("demos/inventory/inventory.scene.json", 1500, 25, "packs/Inventory/bin~/Release/net10.0/Tl.Pack.Inventory.dll"):
        m = re.match(r"step (\d+) hash=", l)
        if m: step = int(m.group(1))
        m = re.match(r"\s+player Bag: (.*)", l)
        if m:
            bag.append({"step": step, **{k: int(v) for k, v in (p.split() for p in m.group(1).split(", "))}})

    out = subprocess.run(["dotnet", "run", "--no-build", "-c", "Release", "--project", "demos/hello"], cwd=ROOT,
                         check=True, capture_output=True, text=True).stdout
    changes = [(int(a), float(b)) for a, b in (l.split() for l in out.strip().splitlines()[1:])]
    code = open(os.path.join(ROOT, "demos/hello/Teleport.cs"), encoding="utf-8").read()
    return {"stats": stats, "inventory": bag, "hello": {"changes": changes, "code": code, "output": out.strip().splitlines()}}


def main():
    only = [a for a in sys.argv[1:] if not a.startswith("-")] or FILMS
    scripts = {f: json.load(open(os.path.join(HERE, "script", f + ".json"), encoding="utf-8")) for f in FILMS}
    built = {}
    for film in FILMS:
        print(f"== {film}: timeline from narration")
        timeline, ticks, captions, scenes, footage_names = build_timeline(film, scripts[film])
        json.dump(timeline, open(os.path.join(HERE, "timelines", film + ".json"), "w", encoding="utf-8"), indent=2)
        built[film] = (timeline, ticks, captions, scenes, footage_names)

    print("== build FilmTl and bake the library")
    run(["dotnet", "build", "demos/film", "-c", "Release", "-p:NuGetAudit=false", "-v:q", "-nologo"], cwd=ROOT)
    library = os.path.join(HERE, "out", "Film.tlib")
    os.makedirs(os.path.dirname(library), exist_ok=True)
    run(["dotnet", "run", "--no-build", "-c", "Release", "--project", "tools/Tl.Bake", "--", "bake", "demos/film/tl.project.json", "-o", library,
         "--assembly", "demos/film/bin/Release/net10.0/FilmTl.dll"], cwd=ROOT)
    for stray in ("Kinds.g.cs", "Timelines.g.cs"):      # bake writes these next to the project; the build already generates them
        path = os.path.join(HERE, stray)
        if os.path.exists(path): os.remove(path)

    demo = collect_demo_data()
    template = open(os.path.join(HERE, "template", "index.template.html"), encoding="utf-8").read()
    for number, film in enumerate(FILMS, 1):
        if film not in only: continue
        timeline, ticks, captions, scenes, footage_names = built[film]
        print(f"== {film}: run the timeline in Tl ({ticks} ticks) and write the HyperFrames project")
        states = os.path.join(HERE, "out", film + ".states.json")
        run(["dotnet", "run", "--no-build", "-c", "Release", "--project", "demos/film", "--", library, str(number), str(ticks), states], cwd=ROOT)
        film_states = json.load(open(states, encoding="utf-8"))
        cues, previous = [], 0        # audio cues are read back from what Tl produced: the tick where Voice changes
        for tick, voice in enumerate(film_states["voice"]):
            if voice != previous and voice: cues.append((tick, voice))
            previous = voice
        project = os.path.join(HERE, "out", film)
        assets = os.path.join(project, "assets")
        os.makedirs(assets, exist_ok=True)
        audio_tags = []
        for tick, voice in cues:
            shutil.copy(os.path.join(HERE, "audio", scenes[voice - 1]["audio"] + ".mp3"), os.path.join(assets, f"voice-{voice}.mp3"))
            length = scenes[voice - 1]["voice_len"] / FPS
            audio_tags.append(f'<audio id="voice-{voice}" data-start="{tick / FPS:.3f}" data-duration="{length:.3f}" '
                              f'data-track-index="2" data-volume="1" src="assets/voice-{voice}.mp3"></audio>')
        spans, previous, start = [], 0, 0   # footage spans are read back from what Tl produced too: where the Footage column is not 0
        for tick, footage in enumerate(film_states["footage"] + [0]):
            if footage != previous:
                if previous: spans.append((previous, start, tick))
                if footage: start = tick
            previous = footage
        footage_start = {}
        for footage, a, b in spans:
            name = footage_names[footage - 1]
            video = f"footage-{footage}.mp4"
            looped_video(name, (b - a) / FPS, os.path.join(assets, video))
            audio_tags.append(f'<video id="footage-{footage}" class="clip footage" data-start="{a / FPS:.3f}" data-duration="{(b - a) / FPS:.3f}" '
                              f'data-track-index="1" muted playsinline src="assets/{video}"></video>')
            footage_start[footage] = a
        unity_names = sorted({sc["unity"] for sc in scripts[film]["scenes"] if sc.get("unity")})
        for name in unity_names:         # a scene can follow the timeline tick by tick with a stack of recorded frames (small jpgs)
            frames = os.path.join(assets, "unity-" + name)
            shutil.rmtree(frames, ignore_errors=True)
            os.makedirs(frames)
            run(["ffmpeg", "-y", "-loglevel", "error", "-i", os.path.join(HERE, "footage", name + ".mp4"), "-vf", "scale=640:360", "-q:v", "4",
                 "-start_number", "0", os.path.join(frames, "f%04d.jpg")])
        lanes = {t["type"].split(".")[-1].rstrip(">"): [[c["start"], c["end"]] for c in t["clips"]] for t in timeline["tracks"]}
        script = scripts[film]
        payload = {"film": film, "title": script["title"], "scenes": script["scenes"], "captions": captions,
                   "sceneTable": scenes, "states": film_states, "lanes": lanes, "demo": demo, "ticks": ticks,
                   "footageNames": footage_names, "footageStart": footage_start, "footage": footage_data(footage_names + unity_names)}
        html = (template.replace("__DURATION__", f"{ticks / FPS:.3f}")
                        .replace("__AUDIO__", "\n      ".join(audio_tags))
                        .replace("__TITLE__", script["title"])
                        .replace("__DATA__", json.dumps(payload, ensure_ascii=False, separators=(",", ":"))))
        open(os.path.join(project, "index.html"), "w", encoding="utf-8").write(html)
        json.dump({"$schema": "https://hyperframes.heygen.com/schema/hyperframes.json",
                   "registry": "https://raw.githubusercontent.com/heygen-com/hyperframes/main/registry",
                   "paths": {"blocks": "compositions", "components": "compositions/components", "assets": "assets"},
                   "media": {"autoProxy": True}}, open(os.path.join(project, "hyperframes.json"), "w"), indent=2)
        json.dump({"id": film, "name": script["title"]}, open(os.path.join(project, "meta.json"), "w"), indent=2)
        json.dump({"name": f"tl-film-{film}", "private": True, "type": "module",
                   "scripts": {"check": "npx --yes hyperframes@0.9.26 check", "render": "npx --yes hyperframes@0.9.26 render"}},
                  open(os.path.join(project, "package.json"), "w"), indent=2)
        print(f"   {project}  ({ticks / FPS:.1f} s)")


if __name__ == "__main__":
    main()
