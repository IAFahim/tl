import fcntl
import json
import os
import shutil
import stat
import subprocess
import tempfile
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
        else:
            sys.exit(1)
    elif command == "issue edit":
        state["assigned"] = True
    elif command == "issue comment":
        state["comments"] += 1
    elif command == "issue close":
        state["issue_state"] = "CLOSED"
    elif command == "project item-list":
        project = state["project"]
        print("ITEM")
        print(project["stage"])
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
        print("FIELD\tStage\tfield-stage")
        print("FIELD\tAgent\tfield-agent")
        print("FIELD\tMachine\tfield-machine")
        print("FIELD\tBranch\tfield-branch")
        print("FIELD\tCheckpoint\tfield-checkpoint")
        print("STAGE\tBacklog\tstage-backlog")
        print("STAGE\tReady\tstage-ready")
        print("STAGE\tIn progress\tstage-progress")
        print("STAGE\tIn review\tstage-review")
        print("STAGE\tDone\tstage-done")
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
            stages = {
                "stage-backlog": "Backlog",
                "stage-ready": "Ready",
                "stage-progress": "In progress",
                "stage-review": "In review",
                "stage-done": "Done",
            }
            project["stage"] = stages[option]
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
        state["pr"] = {"state": "OPEN", "branch": branch, "head": head, "merge": ""}
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
        }
        print(outputs[query])
    else:
        sys.exit(1)
    path.write_text(json.dumps(state))
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

    def tearDown(self):
        shutil.rmtree(self.root)

    def write_state(self, **changes):
        state = {
            "issue_state": "OPEN",
            "assigned": False,
            "comments": 0,
            "project": {
                "stage": "Ready",
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

    def command(self, repo, agent, machine, *arguments):
        environment = os.environ.copy()
        environment["PATH"] = str(self.bin) + os.pathsep + environment["PATH"]
        environment["TL_FAKE_STATE"] = str(self.state)
        environment["TL_AGENT"] = agent
        environment["TL_MACHINE"] = machine
        return subprocess.run(
            [str(repo / "eng" / "agent-work"), *arguments],
            cwd=repo,
            env=environment,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
        )

    def run_raw(self, arguments, cwd=None):
        return subprocess.run(arguments, cwd=cwd, check=True, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def claim(self, kind="feat", slug="atomic"):
        result = self.run_raw(["git", "ls-remote", "--heads", str(self.remote), f"refs/heads/claims/41/{kind}/{slug}"])
        return result.stdout.split()[0] if result.stdout else ""

    def worktree(self, repo, slug="atomic"):
        return repo.parent / f"repo-41-{slug}"

    def test_concurrent_start_has_one_winner(self):
        first = self.clone("first")
        second = self.clone("second")
        environment = os.environ.copy()
        environment["PATH"] = str(self.bin) + os.pathsep + environment["PATH"]
        environment["TL_FAKE_STATE"] = str(self.state)
        one_environment = environment | {"TL_AGENT": "Alpha", "TL_MACHINE": "pc-a"}
        two_environment = environment | {"TL_AGENT": "Beta", "TL_MACHINE": "pc-b"}
        one = subprocess.Popen([str(first / "eng" / "agent-work"), "start", "41", "feat", "atomic", "first"], cwd=first, env=one_environment)
        two = subprocess.Popen([str(second / "eng" / "agent-work"), "start", "41", "feat", "atomic", "second"], cwd=second, env=two_environment)
        statuses = [one.wait(), two.wait()]
        self.assertEqual(1, statuses.count(0))
        self.assertTrue(self.claim())

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
        self.assertEqual("In progress", self.read_state()["project"]["stage"])
        beta_handoff = self.command(beta_tree, "Beta", "pc-b", "handoff", "41", "none", "done")
        self.assertEqual(0, beta_handoff.returncode, beta_handoff.stdout)
        self.assertEqual("Ready", self.read_state()["project"]["stage"])

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
        self.assertEqual("In progress", state["project"]["stage"])
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

    def install_one_delete_failure(self):
        hook = self.remote / "hooks" / "update"
        marker = self.root / "delete-failed"
        hook.write_text(
            "#!/usr/bin/env bash\n"
            f"if [[ \"$1\" == refs/heads/claims/41/feat/atomic && \"$3\" == 0000000000000000000000000000000000000000 && ! -e \"{marker}\" ]]; then touch \"{marker}\"; exit 1; fi\n"
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
        self.assertEqual("In progress", self.read_state()["project"]["stage"])
        self.assertTrue(self.claim())
        second = self.command(worktree, "Alpha", "pc-a", "handoff", "41", "remaining", "next")
        self.assertEqual(0, second.returncode, second.stdout)
        third = self.command(worktree, "Alpha", "pc-a", "handoff", "41", "remaining", "next")
        self.assertEqual(0, third.returncode, third.stdout)
        self.assertFalse(self.claim())

    def test_done_requires_linked_merged_pr_and_recovers_unlock(self):
        repo = self.clone("owner")
        started = self.command(repo, "Alpha", "pc-a", "start", "41", "feat", "atomic", "scope")
        self.assertEqual(0, started.returncode, started.stdout)
        worktree = self.worktree(repo)
        refused = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertNotEqual(0, refused.returncode)
        self.assertEqual("In progress", self.read_state()["project"]["stage"])
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
        self.assertEqual("In review", self.read_state()["project"]["stage"])
        second = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertEqual(0, second.returncode, second.stdout)
        third = self.command(worktree, "Alpha", "pc-a", "done", "41", "evidence")
        self.assertEqual(0, third.returncode, third.stdout)
        self.assertFalse(self.claim())


if __name__ == "__main__":
    unittest.main()
