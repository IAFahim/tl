"""Records the Unity demos as footage for the films.

    python demos/film/unity/record_footage.py <unity project> [--unity <Unity.exe>] [demo ...]

The project is one the installer made (install/unity.ps1: the package plus every demo sample imported). This copies FilmCapture.cs
into it, runs it as an EditMode test with a real graphics device (no -nographics), and encodes what it saved:

    footage/<demo>.mp4   1280x720, 30 fps, one lap of the demo (every second 60 Hz tick of Unity)
    footage/<demo>.csv   per video frame: frame, tick of the first playing timeline, x, y, z of the first entity that has a position

Close any Unity Editor that has the project open first.
"""
import glob, os, shutil, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
FOOTAGE = os.path.join(os.path.dirname(HERE), "footage")
DEFAULT_UNITY = r"C:\Program Files\Unity\Hub\Editor\7000.0.0a16\Editor\Unity.exe"


def main():
    args = sys.argv[1:]
    unity = DEFAULT_UNITY
    if "--unity" in args:
        i = args.index("--unity")
        unity = args[i + 1]
        del args[i:i + 2]
    if not args:
        sys.exit(__doc__)
    project, wanted = os.path.abspath(args[0]), args[1:]

    target = os.path.join(project, "Assets", "FilmCapture", "Editor")
    os.makedirs(target, exist_ok=True)
    for name in ("FilmCapture.cs", "Tl.FilmCapture.asmdef"):
        shutil.copy(os.path.join(HERE, name), target)
    shutil.rmtree(os.path.join(project, "Logs", "film"), ignore_errors=True)

    results = os.path.join(project, "Logs", "film-results.xml")
    log = os.path.join(project, "Logs", "film-capture.log")
    command = [unity, "-batchmode", "-projectPath", project, "-runTests", "-testPlatform", "EditMode",
               "-testFilter", ";".join(wanted) if wanted else "FilmCapture", "-testResults", results, "-logFile", log]
    print("$", " ".join(command))
    subprocess.run(command, check=False)

    os.makedirs(FOOTAGE, exist_ok=True)
    made = []
    for folder in sorted(glob.glob(os.path.join(project, "Logs", "film", "*"))):
        name = os.path.basename(folder)
        if not glob.glob(os.path.join(folder, "f*.png")):
            continue
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "30", "-i", os.path.join(folder, "f%04d.png"),
                        "-c:v", "libx264", "-crf", "23", "-pix_fmt", "yuv420p", os.path.join(FOOTAGE, name + ".mp4")], check=True)
        shutil.copy(os.path.join(folder, "ticks.csv"), os.path.join(FOOTAGE, name + ".csv"))
        made.append(name)
    print("footage:", ", ".join(made) or "none (see " + log + ")")


if __name__ == "__main__":
    main()
