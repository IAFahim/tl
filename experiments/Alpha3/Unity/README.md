# Alpha.3 Unity execution probe

This isolated Unity project executes the handwritten alpha.3 selection and typed-operation reference through Unity Entities jobs. It is an experiment, not production generator output or a `Tl.Unity` package.

The probe covers:

- disabled operation markers that selection enables for the current stage;
- chained select, typed operation, and completion jobs;
- exact forward order and reverse order across Attack and HeavyAttack entities;
- a completed entity alongside a live entity;
- a valid damage-only entity with no Pose component;
- an invalid mixed Damage+Animation entity with no Pose and a stale damage marker;
- borrowed `Frame` values that are created and consumed inside an operation call and never stored in a scheduled job.

Run it with the installed preview Editor:

```sh
experiments/Alpha3/Unity/verify.sh /tmp/tl-alpha3-unity-results.xml
```

`verify.sh` copies the small source project to a fresh `/tmp` directory before invoking Unity, so generated `Library`, package cache, logs, and test output do not enter the repository. Set `TL_UNITY_LOG` to retain the Editor log. Set `TL_UNITY_KEEP_PROJECT=1` only when inspecting generated Entities or Burst cache output; the command prints the retained temporary path.

The project pins Unity 6000.7.0a5, Entities 6.7.0, and Test Framework 1.8.0 because those versions are installed on the probe machine. This does not establish the package's declared stable compatibility floor.

The sources intentionally reproduce the reference's hard-coded assets and dispatch. Passing this probe proves host feasibility and falsifies specific scheduling assumptions. It does not prove TL generation, general ordered occurrence scheduling, player/AOT behavior, allocation, or performance.
