# Prompt for the second developer's Claude Code

Copy everything below the line into a new Claude Code chat.

---

I'm joining my friend's project as the second developer: a mod for **Mad Games Tycoon 2** (Unity, 32-bit Mono) with three
parts: realism (real platforms, companies, logos, dates), a complete Arabic localization, and new gameplay systems as
BepInEx plugins built from a 35-point design plan. Two of us work on it at the same time, each with Claude Code, on the
private repo https://github.com/Ali1243l/mgt2-realism-arabic-mod (I'm a collaborator with write access).
Get me fully up to speed, set up my machine, and follow the team rules exactly. Change nothing on GitHub except my own
branches and pull requests.

## Part A: understand the project (read-only)
1. Make sure I've accepted the repo invitation (https://github.com/Ali1243l/mgt2-realism-arabic-mod/invitations).
   Check `gh auth status`; if I'm not logged in, give me the `gh auth login --web` command and wait for me.
2. Clone the repo into a folder I choose (ask me), then read these fully, in this order:
   - `CLAUDE.md`: the team rules (branches, commits, checks). They apply to everything you do.
   - `IMPORTANT_PROJECT_STATUS.md`: how the mod works, the user's decisions, everything finished so far, known
     pitfalls (for example: never Harmony-patch a method that returns `long` on this 32-bit game), current stage
     and next steps.
   - `TASKS.md`: who is working on what, and the progress of every GDD point.
   - `docs/GDD.md`: the design plan (in Arabic). Points 1 and 34 are done, point 8 is mostly done, point 2 is next.
   - `MGT2_Realism_And_Localization_Audit.md`: the original audit behind the realism and Arabic work.
   - Skim `mod/build.py`, `mod/arabic_game.py` and `mod/plugin/LiveOps/` to see how data, Arabic text and plugins are built.
3. Then give me, in Arabic, a short summary: what the mod does, what is finished, what is in progress (and by whom),
   what comes next, and the rules we follow. Don't change anything in this part.

## Part B: set up my machine
1. Tools (install what's missing with winget and tell me what you install): Git, GitHub CLI, Python 3.12,
   .NET 8 SDK; then `pip install arabic-reshaper python-bidi pillow` and
   `dotnet tool install -g ilspycmd --version 8.2.0.7535`. Run `gh auth setup-git`.
2. In the clone: set git `user.name` and `user.email` for this repo (ask me), then
   `git config core.hooksPath .githooks` (the hooks block commits and pushes to main).
3. Per-machine setup (CLAUDE.md section 5):
   - Find my Mad Games Tycoon 2 folder and save it as a permanent user environment variable: `setx GameDir "<folder>"`.
   - Ask me to run "Verify integrity of game files" in Steam so the game is clean, and wait for my OK.
   - `python -P mod/build.py --backup` (copies the original game text and images into `backup/`).
   - Install BepInEx 5.4.23.5 **x86** into the game folder (the game is 32-bit). The files it adds are listed in
     `mod/bepinex_installed_files.txt`.
   - Decompile `<GameDir>\Mad Games Tycoon 2_Data\Managed\Assembly-CSharp.dll` into `decomp/` with ilspycmd
     (set `DOTNET_ROLL_FORWARD=Major` for that command).
4. Checks: `python -P mod/arabic_game.py`, `python -P mod/build.py --check`, and `dotnet build -c Release` in each
   `mod/plugin/*` folder. If all pass, `python -P mod/build.py` installs the mod into my game. Report any failure
   with its output; don't work around it.
5. Note: adding platforms and other data changes the save layout, so the mod needs a new game; old saves may break.

## Part C: how we work from now on
- Start of every session: `git checkout main`, `git pull`, `git status`, `git log -n 3 --oneline`, read `TASKS.md`.
- Before a task: claim it in `TASKS.md` (my name + branch) on a new `feature/<name>` or `fix/<name>` branch from the
  latest main, commit that claim and push the branch right away so my friend sees it. Never take a task someone claimed.
- Keep to the task's own files. Shared files (`mod/build.py`, `mod/arabic_game.py`, `mod/plugin/make_labels.py`,
  `TASKS.md`, `IMPORTANT_PROJECT_STATUS.md`) get small, focused edits only.
- Conventional scoped commits (e.g. `feat(stocks): add IPO menu`), push the branch, open a PR to main with
  `gh pr create`. Never commit or push to main, never force-push someone else's branch, never merge without my OK.
- If main moved while I worked: merge main into my branch and resolve conflicts before asking for review.
- Before and after each task, tell me: the task, the files created or changed, and the current branch.
- When a task is done: update `TASKS.md` and the matching part of `IMPORTANT_PROJECT_STATUS.md` in the same PR.

When Part B is done, give me a short summary: folder, branch, what was installed, and what the checks printed.
