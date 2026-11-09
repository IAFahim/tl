"""Installs the Unity side of the film into a Unity project that has the Tl package and the Hello Tl sample imported
(an installer-made project is fine):

    python demos/film/unity/setup_film_project.py <unity project>

Assets/Film          Film.cs (the same Film.cs as demos/film, with a block namespace because Unity's Assembly-CSharp is C# 9),
                     FilmData.cs, FilmUi.cs, UI/Film.uxml + Film.uss, Editor/FilmBuilder.cs
Assets/FilmCapture   the EditMode tests that record frames (FilmRender.cs for the film, FilmCapture.cs for the demos)
"""
import os, re, shutil, sys

HERE = os.path.dirname(os.path.abspath(__file__))
FILM = os.path.dirname(HERE)


def block_namespace(source):
    """`namespace Film;` (C# 10) -> `namespace Film { ... }` (C# 9)."""
    head, body = source.replace("\r\n", "\n").split("namespace Film;", 1)
    body = "".join(("    " + line if line.strip() else line) for line in body.splitlines(True))
    return head + "namespace Film\n{" + body.rstrip() + "\n}\n"


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    project = os.path.abspath(sys.argv[1])
    assets = os.path.join(project, "Assets")
    film = os.path.join(assets, "Film")
    capture = os.path.join(assets, "FilmCapture", "Editor")
    shutil.rmtree(os.path.join(assets, "FilmSpike"), ignore_errors=True)
    shutil.rmtree(film, ignore_errors=True)
    for folder in (film, capture):
        os.makedirs(folder, exist_ok=True)
    shutil.copytree(os.path.join(HERE, "FilmProject"), film, dirs_exist_ok=True)
    open(os.path.join(film, "Film.cs"), "w", encoding="utf-8", newline="\n").write(block_namespace(open(os.path.join(FILM, "Film.cs"), encoding="utf-8").read()))
    for name in ("FilmRender.cs", "FilmCapture.cs", "Tl.FilmCapture.asmdef"):
        shutil.copy(os.path.join(HERE, name), capture)
    for stale in ("FilmSpikeRecord.cs", "FilmSpikeRecord.cs.meta"):
        path = os.path.join(capture, stale)
        if os.path.exists(path):
            os.remove(path)
    print("installed into", project)


if __name__ == "__main__":
    main()
