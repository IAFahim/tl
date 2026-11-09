"""Renders a film with Unity: Timeline + Tl + UI Toolkit, then encodes the MP4 with the narration.

    python demos/film/make_film.py                                   # once: timelines, narration, out/unity/<film>/
    python demos/film/unity/setup_film_project.py <unity project>    # once: the Unity side
    python demos/film/unity/render_film.py <unity project> release   # or tutorial  ->  demos/film/<film>-unity.mp4

Close any Unity Editor that has the project open first. The Unity run needs a real graphics device (no -nographics).
The narration is placed at the frames where Unity's own Tl run started each voice (cues.csv), not at times from the script.
"""
import csv, json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
FILM = os.path.dirname(HERE)
DEFAULT_UNITY = r"C:\Program Files\Unity\Hub\Editor\7000.0.0a16\Editor\Unity.exe"
FPS = 30


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 2:
        sys.exit(__doc__)
    project, film = os.path.abspath(args[0]), args[1]
    package = os.path.join(FILM, "out", "unity", film)
    data = json.load(open(os.path.join(package, "film.json"), encoding="utf-8"))
    frames = os.path.join(FILM, "out", "unity", film + "-frames")
    unity = os.environ.get("UNITY", DEFAULT_UNITY)

    if "--skip-unity" not in sys.argv:
        env = dict(os.environ, FILM_UNITY_DIR=package, FILM_FOOTAGE_DIR=os.path.join(FILM, "out", "unity", "footage"),
                   FILM_FRAMES_DIR=frames, FILM_TICKS=str(data["ticks"]))
        log = os.path.join(frames + ".log")
        command = [unity, "-batchmode", "-projectPath", project, "-runTests", "-testPlatform", "EditMode", "-testFilter", "FilmRender",
                   "-testResults", frames + ".xml", "-logFile", log]
        print("$", " ".join(command))
        if subprocess.run(command, env=env).returncode != 0:
            sys.exit("Unity failed, see " + log)

    cues = [(int(r["frame"]), int(r["voice"])) for r in csv.DictReader(open(os.path.join(frames, "cues.csv")))]
    command = ["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(frames, "f%05d.jpg")]
    filters = []
    for n, (frame, voice) in enumerate(cues, 1):
        command += ["-i", os.path.join(package, "audio", f"voice-{voice}.mp3")]
        ms = round(frame * 1000 / FPS)
        filters.append(f"[{n}:a]adelay={ms}|{ms}[a{n}]")
    mix = "".join(f"[a{n}]" for n in range(1, len(cues) + 1)) + f"amix=inputs={len(cues)}:normalize=0[audio]"
    out = os.path.join(FILM, f"{film}-unity.mp4")
    command += ["-filter_complex", ";".join(filters + [mix]), "-map", "0:v", "-map", "[audio]", "-c:v", "libx264", "-crf", "24",
                "-preset", "slow", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", out]
    print("$ ffmpeg ... ->", out)
    subprocess.run(command, check=True)
    print(out)


if __name__ == "__main__":
    main()
