# MGT2 Realism & Arabic Mod: rules for Claude Code

Two developers work on this project at the same time, on separate machines, each with Claude Code.
These rules keep their work from colliding.

## 1. Start of every session or task
1. `git status` and `git log -n 3 --oneline`: see what changed recently.
2. Read `TASKS.md` (who is doing what) and `IMPORTANT_PROJECT_STATUS.md` (how the mod works, decisions, gotchas).
3. State the plan before touching code: the task, the files you will create or modify, and the current branch.

## 2. Visibility
Before and after each task, say briefly:
1. Which task or feature you are working on.
2. Which files you are creating or modifying.
3. Which git branch you are on (`git branch --show-current`).

You may create files, refactor, run commands and tests, and do local git work without asking.

## 3. Git and branches
- Never commit or push to `main`. Check `git branch --show-current` before changing anything.
- One branch per feature or fix: `feature/<name>` or `fix/<name>`, created from an up-to-date `main`.
- Conventional, scoped commit messages, for example `feat(liveops): add season choice to F2P updates`
  or `fix(arabic): correct placeholder order in Text line 912`.
- Claim a task in `TASKS.md` (owner and branch) before starting it, so the other developer does not pick it up.
- Stay inside your task's files. Shared files (`mod/build.py`, `mod/arabic_game.py`, `mod/plugin/make_labels.py`,
  `TASKS.md`, `IMPORTANT_PROJECT_STATUS.md`) get small, focused edits only, to keep merges easy.

## 4. Done means checked
Before calling a step done, run the checks that cover it:
- `python -P mod/arabic_game.py`: converter self-test.
- `python -P mod/build.py --check`: data and text checks, without writing to the game.
- `dotnet build -c Release` in each changed `mod/plugin/<Name>/` folder.
- For gameplay or UI changes: install (`python -P mod/build.py`) and look at it in the game.
Then update `TASKS.md` and the relevant part of `IMPORTANT_PROJECT_STATUS.md`.

## 5. Per-machine setup
- The game path defaults to `E:\Steam\steamapps\common\Mad Games Tycoon 2`. On another machine, set the `GameDir`
  environment variable to the game folder; `mod/build.py` and every plugin `.csproj` read it.
- Not in git, create locally: `backup/` (run `python -P mod/build.py --backup` on an unmodded game), `decomp/`
  (ilspycmd output of Assembly-CSharp.dll), `downloads/` (image cache, `python -P mod/fetch_images.py`).
- Turn on the repo's git hooks once per clone: `git config core.hooksPath .githooks` (they block commits and
  pushes to `main`, since GitHub cannot lock `main` on this free private repo).
- Run Python with `-P` (needs user site-packages: arabic_reshaper, python-bidi, Pillow).
