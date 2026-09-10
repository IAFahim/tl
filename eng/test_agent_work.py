import fcntl
import json
import os
import shutil
import stat
import subprocess
import tempfile
import time
import unittest
from pathlib import Path


FAKE_GH = r'''#!/usr/bin/env python3
import fcntl
import json
import os
import subprocess
import sys
from pathlib import Path

path = Path(os.environ["TL_FAKE_STATE"])
lock_path = path.with_suffix(".lock")
args = sys.argv[1:]

with lock_path.open("a+") as lock:
    fcntl.flock(lock, fcntl.LOCK_EX)
    state = json.loads(path.read_text())
    command = " ".join(args[:2])
    if state.get("fail") == command and not state.get("failed"):
        state["failed"] = True
        path.write_text(json.dumps(state))
        sys.exit(1)

    def value(name):
        return args[args.index(name) + 1]

    if command == "repo view":
        print("OWNER")
    elif command == "api user":
        print("OWNER")
    elif command == "issue view":
        field = value("--json")
        if field == "state":
            print(state["issue_state"])
        elif field == "labels":
            print("ai\narea:tooling\nkind:feature")
        elif field == "url":
            print("https://example.invalid/issues/41")
        elif field == "assignees":
            if state["assigned"]:
                print("OWNER")
        elif field == "closedByPullRequestsReferences":
            if state.get("linked_pr"):
                print(state["linked_pr"])
        elif field == "comments":
            print("\n".join(state.get("comment_bodies", [])))
        else:
            sys.exit(1)
    elif command == "issue edit":
        state["assigned"] = True
    elif command == "issue comment":
        state["comments"] += 1
        body_file = value("--body-file")
        state.setdefault("comment_bodies", []).append(Path(body_file).read_text())
    elif command == "issue close":
        state["issue_state"] = "CLOSED"
    elif command == "project item-list":
        project = state["project"]
        print("ITEM")
        print(project["status"])
        print(project["agent"])
        print(project["machine"])
        print(project["branch"])
        print(project["checkpoint"])
        print("TL_CLAIM_END")
    elif command == "project item-add":
        print("ITEM")
    elif command == "project view":
        print("PROJECT")
    elif command == "project field-list":
        print("FIELD\tStatus\tfield-status")
        print("FIELD\tAgent\tfield-agent")
        print("FIELD\tMachine\tfield-machine")
        print("FIELD\tBranch\tfield-branch")
        print("FIELD\tCheckpoint\tfield-checkpoint")
        print("STATUS\tBacklog\tstatus-backlog")
        print("STATUS\tReady\tstatus-ready")
        print("STATUS\tIn progress\tstatus-progress")
        print("STATUS\tIn review\tstatus-review")
        print("STATUS\tDone\tstatus-done")
    elif command == "project item-edit":
        field = value("--field-id")
        project = state["project"]
        if "--text" in args:
            text = value("--text")
            names = {
                "field-agent": "agent",
                "field-machine": "machine",
                "field-branch": "branch",
                "field-checkpoint": "checkpoint",
            }
            project[names[field]] = text
        else:
            option = value("--single-select-option-id")
            statuses = {
                "status-backlog": "Backlog",
                "status-ready": "Ready",
                "status-progress": "In progress",
                "status-review": "In review",
                "status-done": "Done",
            }
            project["status"] = statuses[option]
    elif command == "pr list":
        requested = value("--state")
        if state.get("pr") and state["pr"]["state"].lower() == requested:
            if requested == "open":
                print("https://example.invalid/pull/12")
            else:
                print("12")
    elif command == "pr create":
        branch = value("--head")
        head = subprocess.check_output(["git", "rev-parse", "HEAD"], text=True).strip()
        body = Path(value("--body-file")).read_text()
        state["pr"] = {"state": "OPEN", "branch": branch, "head": head, "merge": "", "body": body}
        state["linked_pr"] = 12
        print("https://example.invalid/pull/12")
    elif command == "pr view":
        query = value("--jq")
        pull = state["pr"]
        outputs = {
            ".state": pull["state"],
            ".headRefName": pull["branch"],
            ".headRefOid": pull["head"],
            ".mergeCommit.oid // \"\"": pull["merge"],
            ".body": pull["body"],
        }
        print(outputs[query])
    else:
        sys.exit(1)
    path.write_text(json.dumps(state))
'''


FAKE_GIT = r'''#!/usr/bin/env python3
import os
import subprocess
import sys
import time
from pathlib import Path

real_git = os.environ["TL_REAL_GIT"]
arguments = sys.argv[1:]
pause = os.environ.get("TL_GIT_PAUSE", "")
ready = Path(os.environ.get("TL_GIT_READY", "/dev/null"))
resume = Path(os.environ.get("TL_GIT_RESUME", "/dev/null"))
is_branch_push = bool(arguments) and arguments[0] == "push" and any(
    argument == "HEAD" or argument.startswith("HEAD:refs/heads/")
    for argument in arguments
)
is_claim_delete = bool(arguments) and arguments[0] == "push" and any(
    argument.startswith(":refs/heads/workstream-claims/") or argument.startswith(":refs/heads/claims/")
    for argument in arguments
)

def wait():
    ready.touch()
    deadline = time.monotonic() + 10
    while not resume.exists():
        if time.monotonic() >= deadline:
            sys.exit(97)
        time.sleep(0.01)

if pause == "before-branch-push" and is_branch_push:
    wait()
    os.execv(real_git, [real_git, *arguments])

if pause == "after-branch-push" and is_branch_push:
    result = subprocess.run([real_git, *arguments])
    if result.returncode == 0:
        wait()
    sys.exit(result.returncode)

if pause == "before-claim-delete" and is_claim_delete:
    wait()
    os.execv(real_git, [real_git, *arguments])

if pause == "after-claim-delete" and is_claim_delete:
    result = subprocess.run([real_git, *arguments])
    if result.returncode == 0:
        wait()
    sys.exit(result.returncode)

os.execv(real_git, [real_git, *arguments])
'''


class AgentWorkTests(unittest.TestCase):
    def setUp(self):
        self.root = Path(tempfile.mkdtemp(prefix="tl-agent-work-"))
        self.remote = self.root / "remote.git"
        self.seed = self.root / "seed"
        self.bin = self.root / "bin"
        self.bin.mkdir()
        self.run_raw(["git", "init", "--bare", str(self.remote)])
        self.run_raw(["git", "init", "-b", "main", str(self.seed)])
        self.configure(self.seed, "Seed")
        (self.seed / "eng").mkdir()
        helper = Path(__file__).with_name("agent-work")
        shutil.copy2(helper, self.seed / "eng" / "agent-work")
        self.run_raw(["git", "add", "eng/agent-work"], self.seed)
        self.run_raw(["git", "commit", "-m", "seed"], self.seed)
        self.run_raw(["git", "remote", "add", "origin", str(self.remote)], self.seed)
        self.run_raw(["git", "push", "-u", "origin", "main"], self.seed)
        self.run_raw(["git", "symbolic-ref", "HEAD", "refs/heads/main"], self.remote)
        self.state = self.root / "state.json"
        self.write_state()
        fake = self.bin / "gh"
        fake.write_text(FAKE_GH)
        fake.chmod(fake.stat().st_mode | stat.S_IXUSR)
        fake_git = self.bin / "git"
        fake_git.write_text(FAKE_GIT)
        fake_git.chmod(fake_git.stat().st_mode | stat.S_IXUSR)

    def tearDown(self):
        shutil.rmtree(self.root)

    def write_state(self, **changes):
        state = {
            "issue_state": "OPEN",
            "assigned": False,
            "comments": 0,
            "comment_bodies": [],
            "project": {
                "status": "Ready",
                "agent": "",
                "machine": "",
                "branch": "",
                "checkpoint": "",
            },
        }
        if self.state.exists():
            state = json.loads(self.state.read_text())
        state.update(changes)
        self.state.write_text(json.dumps(state))

    def read_state(self):
        return json.loads(self.state.read_text())

    def clone(self, name):
        repo = self.root / name / "repo"
        repo.parent.mkdir()
        self.run_raw(["git", "clone", str(self.remote), str(repo)])
        self.configure(repo, name)
        return repo

    def configure(self, repo, name):
        self.run_raw(["git", "-C", str(repo), "config", "user.name", name])
        self.run_raw(["git", "-C", str(repo), "config", "user.email", f"{name}@example.invalid"])

    def environment(self, agent, machine, **changes):
        environment = os.environ.copy()
        environment["PATH"] = str(self.bin) + os.pathsep + environment["PATH"]
        environment["TL_FAKE_STATE"] = str(self.state)
        environment["TL_REAL_GIT"] = shutil.which("git")
        environment["TL_AGENT"] = agent
        environment["TL_MACHINE"] = machine
        environment.update(changes)
        return environment

    def command(self, repo, agent, machine, *arguments):
        return subprocess.run(
            [str(repo / "eng" / "agent-work"), *arguments],
            cwd=repo,
            env=self.environment(agent, machine),
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
        )

    def run_raw(self, arguments, cwd=None):
        return subprocess.run(arguments, cwd=cwd, check=True, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def claim(self, kind="feat", slug="atomic"):
        result = self.run_raw(["git", "ls-remote", "--heads", str(self.remote), f"refs/heads/workstream-claims/41/{kind}/{slug}"])
        return result.stdout.split()[0] if result.stdout else ""

    def branch(self, kind="feat", slug="atomic"):
        result = self.run_raw(["git", "ls-remote", "--heads", str(self.remote), f"refs/heads/{kind}/41-{slug}"])
        return result.stdout.split()[0] if result.stdout else ""

    def transaction(self):
        result = self.run_raw(["git", "ls-remote", "--heads", str(self.remote), "refs/heads/issue-transactions/41"])
        return result.stdout.split()[0] if result.stdout else ""

    def worktree(self, repo, slug="atomic"):
        return repo.parent / f"repo-41-{slug}"

    def paused_command(self, repo, agent, machine, pause, ready, resume, *arguments):
        environment = self.environment(
            agent,
            machine,
            TL_GIT_PAUSE=pause,
            TL_GIT_READY=str(ready),
            TL_GIT_RESUME=str(resume),
        )
        return subprocess.Popen(
            [str(repo / "eng" / "agent-work"), *arguments],
            cwd=repo,
            env=environment,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
        )

    def wait_until_paused(self, process, ready):
        deadline = time.monotonic() + 10
        while not ready.exists():
            if process.poll() is not None:
                output = process.stdout.read()
                self.fail(f"command exited before pause with {process.returncode}: {output}")
            if time.monotonic() >= deadline:
                process.kill()
                self.fail("command did not reach the requested Git interleaving")
            time.sleep(0.01)

    def resume(self, process, resume):
        resume.touch()
        output, _ = process.communicate(timeout=10)
        return process.returncode, output

    def test_concurrent_start_has_one_winner(self):
        first = self.clone("first")
        second = self.clone("second")
        one_environment = self.environment("Alpha", "pc-a")
        two_environment = self.environment("Beta", "pc-b")
        one = subprocess.Popen([str(first / "eng" / "agent-work"), "start", "41", "feat", "atomic", "first"], cwd=first, env=one_environment)
        two = subprocess.Popen([str(second / "eng" / "agent-work"), "start", "41", "feat", "atomic", "second"], cwd=second, env=two_environment)
        statuses = [one.wait(), two.wait()]
        self.assertEqual(1, statuses.count(0))
        self.assertTrue(self.claim())

    def test_scoped_claim_coexists_with_legacy_issue_claim(self):
        legacy = self.run_raw(["git", "rev-parse", "HEAD"], self.seed).stdout.strip()
        self.run_raw(["git", "push", "origin", f"{legacy}:refs/heads/claims/41"], self.seed)
        repo = self.clone("owner")

        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")

        self.assertEqual(0, started.returncode, started.stdout)
        self.assertTrue(self.claim())
        self.assertEqual(
            legacy,
            self.run_raw(["git", "ls-remote", "--heads", str(self.remote), "refs/heads/claims/41"]).stdout.split()[0],
        )

    def test_start_resumes_legacy_scoped_claim(self):
        repo = self.clone("owner")
        base = self.run_raw(["git", "rev-parse", "HEAD"], repo).stdout.strip()
        tree = self.run_raw(["git", "rev-parse", "HEAD^{tree}"], repo).stdout.strip()
        message = "issue=41\nagent=Alpha\nmachine=pc-a\nbranch=feat/41-atomic\nnonce=legacy\n"
        claim = subprocess.run(
            ["git", "commit-tree", tree, "-p", base],
            cwd=repo,
            input=message,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True,
        ).stdout.strip()
        self.run_raw(["git", "push", "origin", f"{claim}:refs/heads/claims/41/feat/atomic"], repo)

        resumed = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")

        self.assertEqual(0, resumed.returncode, resumed.stdout)
        self.assertTrue(self.worktree(repo).exists())
        self.assertEqual(
            claim,
            self.run_raw(["git", "ls-remote", "--heads", str(self.remote), "refs/heads/claims/41/feat/atomic"]).stdout.split()[0],
        )

    def test_one_issue_accepts_independent_workstreams(self):
        first = self.clone("first")
        second = self.clone("second")
        alpha = self.command(first, "Alpha", "pc-a", "start", "41", "perf", "scalar-kernel", "scalar path")
        beta = self.command(second, "Beta", "pc-b", "start", "41", "docs", "unity-guide", "Unity guide")
        self.assertEqual(0, alpha.returncode, alpha.stdout)
        self.assertEqual(0, beta.returncode, beta.stdout)
        self.assertTrue(self.claim("perf", "scalar-kernel"))
        self.assertTrue(self.claim("docs", "unity-guide"))
        alpha_tree = self.worktree(first, "scalar-kernel")
        beta_tree = self.worktree(second, "unity-guide")
        checkpoint = self.command(alpha_tree, "Alpha", "pc-a", "checkpoint", "41", "green", "tests")
        self.assertEqual(0, checkpoint.returncode, checkpoint.stdout)
        alpha_handoff = self.command(alpha_tree, "Alpha", "pc-a", "handoff", "41", "none", "done")
        self.assertEqual(0, alpha_handoff.returncode, alpha_handoff.stdout)
        self.assertEqual("In progress", self.read_state()["project"]["status"])
        beta_handoff = self.command(beta_tree, "Beta", "pc-b", "handoff", "41", "none", "done")
        self.assertEqual(0, beta_handoff.returncode, beta_handoff.stdout)
        self.assertEqual("Ready", self.read_state()["project"]["status"])

    def test_start_accepts_an_explicit_stacked_base(self):
        self.run_raw(["git", "switch", "-c", "stacked"], self.seed)
        (self.seed / "stacked.txt").write_text("stacked\n")
        self.run_raw(["git", "add", "stacked.txt"], self.seed)
        self.run_raw(["git", "commit", "-m", "stacked"], self.seed)
        expected = self.run_raw(["git", "rev-parse", "HEAD"], self.seed).stdout.strip()
        self.run_raw(["git", "push", "origin", "stacked"], self.seed)
        self.run_raw(["git", "switch", "main"], self.seed)

        repo = self.clone("owner")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope", "origin/stacked")

        self.assertEqual(0, started.returncode, started.stdout)
        actual = self.run_raw(["git", "rev-parse", "HEAD"], self.worktree(repo)).stdout.strip()
        self.assertEqual(expected, actual)
        claim_parent = self.run_raw([
            "git", "--git-dir", str(self.remote), "rev-parse", "refs/heads/workstream-claims/41/feat/atomic^"
        ]).stdout.strip()
        self.assertEqual(expected, claim_parent)

    def test_start_rejects_a_missing_explicit_base_before_claiming(self):
        repo = self.clone("owner")

        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope", "origin/missing")

        self.assertNotEqual(0, started.returncode)
        self.assertIn("does not exist", started.stdout)
        self.assertFalse(self.claim())
        self.assertFalse(self.worktree(repo).exists())

    def test_interrupted_start_repairs_the_same_claim(self):
        repo = self.clone("owner")
        self.write_state(fail="project item-edit")
        first = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertNotEqual(0, first.returncode)
        claim = self.claim()
        self.assertTrue(claim)
        second = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, second.returncode, second.stdout)
        state = self.read_state()
        self.assertEqual("In progress", state["project"]["status"])
        self.assertEqual("Alpha", state["project"]["agent"])
        self.assertEqual(claim, self.claim())

    def test_takeover_uses_compare_and_set_and_is_restartable(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        previous = self.claim()
        taken = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", previous)
        self.assertEqual(0, taken.returncode, taken.stdout)
        replacement = self.claim()
        self.assertNotEqual(previous, replacement)
        repeated = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", previous)
        self.assertEqual(0, repeated.returncode, repeated.stdout)
        self.assertEqual(replacement, self.claim())
        self.assertEqual("Beta", self.read_state()["project"]["agent"])

    def test_takeover_retry_preserves_and_publishes_original_audit(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        previous = self.claim()
        self.write_state(fail="project item-edit")

        interrupted = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", previous)

        self.assertNotEqual(0, interrupted.returncode)
        self.assertEqual(previous, self.claim())
        recovered = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", previous)
        self.assertEqual(0, recovered.returncode, recovered.stdout)
        replacement = self.claim()
        self.assertNotEqual(previous, replacement)
        audits = [body for body in self.read_state()["comment_bodies"] if body.startswith("### Claim takeover")]
        self.assertEqual(1, len(audits))
        self.assertIn("- Previous agent: Alpha", audits[0])
        self.assertIn("- Previous machine: pc-a", audits[0])
        self.assertIn(f"- Previous claim: `{previous}`", audits[0])
        self.assertIn(f"- New claim: `{replacement}`", audits[0])

    def test_issue_transaction_serializes_checkpoint_and_takeover(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        previous = self.claim()
        worktree = self.worktree(repo)
        (worktree / "checkpoint.txt").write_text("stale checkpoint\n")
        self.run_raw(["git", "add", "checkpoint.txt"], worktree)
        self.run_raw(["git", "commit", "-m", "checkpoint"], worktree)
        ready = self.root / "checkpoint-ready"
        resume = self.root / "checkpoint-resume"
        checkpoint = self.paused_command(
            worktree,
            "Alpha",
            "pc-a",
            "before-branch-push",
            ready,
            resume,
            "checkpoint",
            "41",
            "green",
            "tests",
        )
        self.wait_until_paused(checkpoint, ready)
        taken = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", previous)
        self.assertNotEqual(0, taken.returncode, taken.stdout)
        self.assertIn("active transaction", taken.stdout)
        self.assertEqual(previous, self.claim())
        status, output = self.resume(checkpoint, resume)
        self.assertEqual(0, status, output)
        published = self.claim()
        self.assertNotEqual(previous, published)
        taken = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", published)
        self.assertEqual(0, taken.returncode, taken.stdout)
        replacement = self.claim()
        self.assertEqual(replacement, self.claim())

    def test_issue_transaction_serializes_handoff_and_takeover(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        ready = self.root / "handoff-ready"
        resume = self.root / "handoff-resume"
        handoff = self.paused_command(
            worktree,
            "Alpha",
            "pc-a",
            "after-branch-push",
            ready,
            resume,
            "handoff",
            "41",
            "remaining",
            "next",
        )
        self.wait_until_paused(handoff, ready)
        published_claim = self.claim()
        taken = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", published_claim)
        self.assertNotEqual(0, taken.returncode, taken.stdout)
        self.assertIn("active transaction", taken.stdout)
        status, output = self.resume(handoff, resume)
        self.assertEqual(0, status, output)
        self.assertFalse(self.claim())
        self.assertEqual(self.run_raw(["git", "rev-parse", "HEAD"], worktree).stdout.strip(), self.branch())
        bodies = self.read_state()["comment_bodies"]
        self.assertEqual(1, len([body for body in bodies if body.startswith("### Handoff")]))
        self.assertEqual("Ready", self.read_state()["project"]["status"])

    def install_one_delete_failure(self):
        hook = self.remote / "hooks" / "update"
        marker = self.root / "delete-failed"
        hook.write_text(
            "#!/usr/bin/env bash\n"
            f"if [[ \"$1\" == refs/heads/workstream-claims/41/feat/atomic && \"$3\" == 0000000000000000000000000000000000000000 && ! -e \"{marker}\" ]]; then touch \"{marker}\"; exit 1; fi\n"
            "exit 0\n"
        )
        hook.chmod(hook.stat().st_mode | stat.S_IXUSR)

    def test_handoff_recovers_after_unlock_failure(self):
        repo = self.clone("owner")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        self.install_one_delete_failure()
        first = self.command(worktree, "Alpha", "pc-a", "handoff", "41", "remaining", "next")
        self.assertNotEqual(0, first.returncode)
        self.assertEqual("In progress", self.read_state()["project"]["status"])
        self.assertTrue(self.claim())
        second = self.command(worktree, "Alpha", "pc-a", "handoff", "41", "remaining", "next")
        self.assertEqual(0, second.returncode, second.stdout)
        third = self.command(worktree, "Alpha", "pc-a", "handoff", "41", "remaining", "next")
        self.assertEqual(0, third.returncode, third.stdout)
        self.assertFalse(self.claim())
        bodies = self.read_state()["comment_bodies"]
        self.assertEqual(0, len([body for body in bodies if body.startswith("### Green checkpoint")]))
        self.assertEqual(1, len([body for body in bodies if body.startswith("### Handoff")]))

    def test_handoff_has_no_project_writes_after_claim_release(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        ready = self.root / "handoff-delete-ready"
        resume = self.root / "handoff-delete-resume"
        handoff = self.paused_command(
            worktree,
            "Alpha",
            "pc-a",
            "after-claim-delete",
            ready,
            resume,
            "handoff",
            "41",
            "remaining",
            "next",
        )
        self.wait_until_paused(handoff, ready)
        self.assertFalse(self.claim())
        self.assertTrue(self.transaction())
        successor = self.command(replacement_repo, "Beta", "pc-b", "start", "41", "feat", "atomic", "successor")
        self.assertNotEqual(0, successor.returncode)
        self.assertIn("active transaction", successor.stdout)
        successor_claim = self.claim()
        projected = self.read_state()["project"].copy()

        status, output = self.resume(handoff, resume)

        self.assertEqual(0, status, output)
        self.assertEqual(successor_claim, self.claim())
        self.assertEqual(projected, self.read_state()["project"])
        resumed = self.command(replacement_repo, "Beta", "pc-b", "start", "41", "feat", "atomic", "successor")
        self.assertEqual(0, resumed.returncode, resumed.stdout)
        self.assertEqual("Beta", self.read_state()["project"]["agent"])
        self.assertEqual("In progress", self.read_state()["project"]["status"])

    def test_done_requires_linked_merged_pr_and_recovers_unlock(self):
        repo = self.clone("owner")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        refused = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertNotEqual(0, refused.returncode)
        self.assertEqual("In progress", self.read_state()["project"]["status"])
        body = self.root / "pull.md"
        body.write_text("Closes #41\n")
        opened = self.command(worktree, "Alpha", "pc-a", "pr", "41", "title", str(body))
        self.assertEqual(0, opened.returncode, opened.stdout)
        state = self.read_state()
        state["pr"]["state"] = "MERGED"
        state["pr"]["merge"] = state["pr"]["head"]
        state["issue_state"] = "CLOSED"
        self.state.write_text(json.dumps(state))
        self.install_one_delete_failure()
        first = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertNotEqual(0, first.returncode)
        self.assertEqual("In review", self.read_state()["project"]["status"])
        second = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertEqual(0, second.returncode, second.stdout)
        third = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertEqual(0, third.returncode, third.stdout)
        self.assertFalse(self.claim())

    def test_done_rejects_merged_pr_without_exact_issue_link(self):
        repo = self.clone("owner")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        body = self.root / "pull.md"
        body.write_text("Refs #41\n")
        opened = self.command(worktree, "Alpha", "pc-a", "pr", "41", "title", str(body))
        self.assertEqual(0, opened.returncode, opened.stdout)
        state = self.read_state()
        state["pr"]["state"] = "MERGED"
        state["pr"]["merge"] = state["pr"]["head"]
        state["pr"]["body"] = "Refs #42\n"
        self.state.write_text(json.dumps(state))
        previous_claim = self.claim()
        previous_project = self.read_state()["project"].copy()

        refused = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")

        self.assertNotEqual(0, refused.returncode)
        self.assertEqual(previous_claim, self.claim())
        self.assertEqual(previous_project, self.read_state()["project"])
        self.assertFalse(any("Merged by #12." in body for body in self.read_state()["comment_bodies"]))

    def test_done_rejects_local_head_older_than_claimed_checkpoint(self):
        repo = self.clone("owner")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        merged_head = self.run_raw(["git", "rev-parse", "HEAD"], worktree).stdout.strip()
        body = self.root / "pull.md"
        body.write_text("Refs #41\n")
        opened = self.command(worktree, "Alpha", "pc-a", "pr", "41", "title", str(body))
        self.assertEqual(0, opened.returncode, opened.stdout)
        state = self.read_state()
        state["pr"]["state"] = "MERGED"
        state["pr"]["merge"] = state["pr"]["head"]
        self.state.write_text(json.dumps(state))
        (worktree / "newer.txt").write_text("newer checkpoint\n")
        self.run_raw(["git", "add", "newer.txt"], worktree)
        self.run_raw(["git", "commit", "-m", "newer checkpoint"], worktree)
        checkpoint = self.command(worktree, "Alpha", "pc-a", "checkpoint", "41", "newer", "tests")
        self.assertEqual(0, checkpoint.returncode, checkpoint.stdout)
        claimed = self.claim()
        project = self.read_state()["project"].copy()
        comments = list(self.read_state()["comment_bodies"])
        self.run_raw(["git", "reset", "--hard", merged_head], worktree)

        refused = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")

        self.assertNotEqual(0, refused.returncode)
        self.assertIn("is not the claimed checkpoint", refused.stdout)
        self.assertEqual(claimed, self.claim())
        self.assertEqual(project, self.read_state()["project"])
        self.assertEqual(comments, self.read_state()["comment_bodies"])

    def test_done_has_no_project_writes_after_claim_release(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        body = self.root / "pull.md"
        body.write_text("Refs #41\n")
        opened = self.command(worktree, "Alpha", "pc-a", "pr", "41", "title", str(body))
        self.assertEqual(0, opened.returncode, opened.stdout)
        state = self.read_state()
        state["pr"]["state"] = "MERGED"
        state["pr"]["merge"] = state["pr"]["head"]
        self.state.write_text(json.dumps(state))
        ready = self.root / "done-delete-ready"
        resume = self.root / "done-delete-resume"
        done = self.paused_command(
            worktree,
            "Alpha",
            "pc-a",
            "after-claim-delete",
            ready,
            resume,
            "done",
            "41",
            "evidence",
        )
        self.wait_until_paused(done, ready)
        self.assertFalse(self.claim())
        self.assertTrue(self.transaction())
        successor = self.command(replacement_repo, "Beta", "pc-b", "start", "41", "feat", "atomic", "successor")
        self.assertNotEqual(0, successor.returncode)
        successor_claim = self.claim()
        projected = self.read_state()["project"].copy()

        status, output = self.resume(done, resume)

        self.assertEqual(0, status, output)
        self.assertEqual(successor_claim, self.claim())
        self.assertEqual(projected, self.read_state()["project"])
        resumed = self.command(replacement_repo, "Beta", "pc-b", "start", "41", "feat", "atomic", "successor")
        self.assertEqual(0, resumed.returncode, resumed.stdout)
        self.assertEqual("Beta", self.read_state()["project"]["agent"])

    def test_stale_issue_transaction_requires_exact_recovery(self):
        repo = self.clone("owner")
        base = self.run_raw(["git", "rev-parse", "HEAD"], repo).stdout.strip()
        tree = self.run_raw(["git", "rev-parse", "HEAD^{tree}"], repo).stdout.strip()
        message = "issue=41\nagent=Lost\nmachine=pc-lost\noperation=checkpoint\nnonce=lost\n"
        transaction = subprocess.run(
            ["git", "commit-tree", tree, "-p", base],
            cwd=repo,
            input=message,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True,
        ).stdout.strip()
        self.run_raw(["git", "push", "origin", f"{transaction}:refs/heads/issue-transactions/41"], repo)

        wrong = self.command(repo, "Beta", "pc-b", "recover-lock", "41", "0" * 40, "power lost")
        self.assertNotEqual(0, wrong.returncode)
        self.assertEqual(transaction, self.transaction())
        recovered = self.command(repo, "Beta", "pc-b", "recover-lock", "41", transaction, "power lost")

        self.assertEqual(0, recovered.returncode, recovered.stdout)
        self.assertFalse(self.transaction())
        audits = [body for body in self.read_state()["comment_bodies"] if body.startswith("### Transaction recovery")]
        self.assertEqual(1, len(audits))
        self.assertIn(f"- Recovered transaction: `{transaction}`", audits[0])

    def test_issue_transaction_serializes_done_and_takeover(self):
        repo = self.clone("owner")
        replacement_repo = self.clone("replacement")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        body = self.root / "pull.md"
        body.write_text("Refs #41\n")
        opened = self.command(worktree, "Alpha", "pc-a", "pr", "41", "title", str(body))
        self.assertEqual(0, opened.returncode, opened.stdout)
        state = self.read_state()
        state["pr"]["state"] = "MERGED"
        state["pr"]["merge"] = state["pr"]["head"]
        self.state.write_text(json.dumps(state))
        ready = self.root / "done-ready"
        resume = self.root / "done-resume"
        done = self.paused_command(
            worktree,
            "Alpha",
            "pc-a",
            "before-claim-delete",
            ready,
            resume,
            "done",
            "41",
            "evidence",
        )
        self.wait_until_paused(done, ready)
        published_claim = self.claim()
        taken = self.command(replacement_repo, "Beta", "pc-b", "takeover", "41", "feat", "atomic", "power lost", published_claim)
        self.assertNotEqual(0, taken.returncode, taken.stdout)
        self.assertIn("active transaction", taken.stdout)

        status, output = self.resume(done, resume)

        self.assertEqual(0, status, output)
        self.assertFalse(self.claim())
        self.assertTrue(any("Merged by #12." in body for body in self.read_state()["comment_bodies"]))


if __name__ == "__main__":
    unittest.main()
