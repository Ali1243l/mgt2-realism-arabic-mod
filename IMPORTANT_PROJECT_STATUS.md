# IMPORTANT PROJECT STATUS — MGT2 Realism & Arabic Localization Mod

> Read this file at the start of every session. Update it after every step.

## Start Timestamp
2026-10-21 15:08 (local)

## Game Path
- **Primary (Target Installation Path):** `E:\Steam\steamapps\common\Mad Games Tycoon 2`
- Secondary copy: `D:\Steam\steamapps\common\Mad Games Tycoon 2`: same build (20855306), identical `Extern`. Mod NOT applied there.
- Moddable data: `Mad Games Tycoon 2_Data\Extern\` (`Text\AR`, `Text\DATA`, `CompanyLogos` 64x64, `Icons_Platforms` 128x128)

## Repo & team (2026-10-21)
- Private GitHub repo: https://github.com/Ali1243l/mgt2-realism-arabic-mod (owner Ali1243l; collaborator
  uksteamaccwillysa0-lab with write access). Two developers, each with Claude Code, working at the same time.
- Rules in `CLAUDE.md` (auto-loaded): feature/* or fix/* branches only, PRs into main, conventional commits,
  claim tasks in `TASKS.md` first, run checks before "done". GitHub can't lock main (free private repo), so
  `.githooks/` blocks commits/pushes to main: enable per clone with `git config core.hooksPath .githooks`.
- `.gitattributes` = `* -text` (byte-for-byte; game text is BOM/CRLF sensitive). Not in git: backup/, decomp/,
  downloads/, audit_data/ (game-derived or per machine). Game path per machine: env var `GameDir`.
- Design plan (GDD, 35 points, user's file): `docs/GDD.md`. Progress per point: `TASKS.md`.
- Prompt for the second developer's Claude: `docs/FRIEND_PROMPT.md`.

## How the mod works (don't re-derive)
- `backup/Extern/` = untouched originals (Text, CompanyLogos, Icons_Platforms). NEVER edit.
- `mod/build.py` builds everything from `backup/` + tables, runs self-checks, writes into the game.
  - `python -P mod/build.py --check` (checks only) · no flag = apply · `--restore` = put originals back.
  - After a Steam update: delete `backup/`, run `--backup`, then apply again.
- `mod/arabic_game.py` = converter: logical Arabic -> shaped + reversed (arabic_reshaper + python-bidi),
  keeps Latin/digits/tags/placeholders LTR, mirrors brackets, keeps `+3%`/`$100`/`+<NUM>%` as one unit,
  wraps lines > 65 chars and balances <color>/<b> tags per visual line. Self-test: `python -P mod/arabic_game.py`.
- Files are UTF-8 BOM + CRLF (build preserves both). Run Python with `-P` (user site-packages needed).
- Tables: `mod/data/platforms.tsv`, `publishers.tsv`, `wiki_titles.tsv`; `mod/ar/*.tsv` (line patches, logical Arabic),
  `mod/ar/align.tsv` (Quotes/Review line alignment), `mod/ar/glossary.tsv` (whole-word fixes on shipped Arabic).
- Images: `mod/fetch_images.py` downloads Wikipedia lead images to `downloads/` and fits them into `mod/images/`.

## User decisions (2026-10-21)
- Future platforms -> expected real names (PS6, Xbox Next, Switch 2...); old fictional -> real rare devices.
- Uncertain publishers -> famous real companies of same era. Platform release dates -> real dates.
- Logos: explicit permission to download official logos (64x64 / 128x128 PNG, transparent).

## Completed Tasks
- [x] Located installs; audit `MGT2_Realism_And_Localization_Audit.md` written and approved.
- [x] Backup of originals in `backup/Extern`.
- [x] Converter with self-tests (11 cases + wrap/span tests; 2808/2815 shipped lines reproduce exactly — the 7 others are shipped bugs).
- [x] 133 platforms + 100 publishers renamed (all language keys); 33 platform dates corrected; checks: manufacturer==publisher, DATE<DATE END, predecessor older, no duplicate names.
- [x] Arabic: ~1,190 lines translated (Text 1535-2450 block + scattered, Achievements, Tutorial, Objects, Country, ContractWork, 90 Themes), 15 placeholder-order fixes, reversed Latin/number fixes, 4 unshaped lines, glossary (Console/Fans/Close/Game Pass/Mad Games...), Quotes realigned (2 extra lines removed), Review 4 blocks rewritten 1:1.
- [x] 2026-10-21: TEXT APPLIED to game (12 files; BOM + CRLF + line counts verified). Game was running at the time: needs restart.
- [x] User tested text in game: "perfect" (2026-10-21).
- [x] Long shipped Arabic lines re-wrapped without changing wording (`mod/rewrap.py` -> `ar/Text_rewrap.tsv`, 77 lines proven by exact round-trip) + 6 hand fixes in `ar/Text_fix2.tsv` (894/895 said "two platforms") + Tutorial line 1 order. Review/FanLetter intentionally NOT re-wrapped (game joins their sentences into a paragraph).
- [x] Parody names in other data files: `mod/data/renames.tsv` (99 developer legends in DevLegends.txt -> real people; Hardware.txt NVIDIA/AMD/Yamaha/Creative/VIA/Blu-ray; HardwareFeatures Bluetooth). Build errors if any old name is missing. Generic made-up names left as is. Not yet installed.
- [ ] Logos/icons: companies = Wikidata P154 logo (preferred/current) on a game-style badge (`badge_logo`), overrides in `wiki_titles.tsv` `file` column (picked with `mod/pick_logo.py`), generated wordmark when no logo exists; platforms = lead photo, background removed, drop shadow, rejected if removal fails. Download running at 250px; images NOT yet applied.

- [x] 2026-10-21 GDD phase started (user file: Desktop\mad game tycoon 2 mods.txt, 35 sections). User approved toolchain + order "data first, then code".
- [x] Toolchain: .NET 8 SDK (winget), ilspycmd 8.2 (run with DOTNET_ROLL_FORWARD=Major), game decompiled to `decomp/` (837 files). Game is 32-bit (x86): BepInEx 5.4.23.5 x86 installed in game folder (file list: `mod/bepinex_installed_files.txt`). Loader not yet verified by a launch; no plugin yet.
- [x] Code facts: no year cap (year++ forever; only an achievement at 2050). Data arrays indexed by ID -> new IDs must be contiguous. Adding entries changes save layout -> NEW GAME required. Feature/hardware/genre loaders read [NAME AR]/[DESC AR]; platforms/publishers don't. Missing PNG -> missing.png.
- [x] 2030-2100 content (`mod/future.py`): 37 platforms (ids 134-170, tech 8-16, eras cloud/holo/neural/bio/full-dive), 20 engine features (67-86), 12 gameplay features (75-86), 30 hardware parts (172-201), 6 console features (30-35); predecessors get SUC + DATE END. Icons drawn in code (`mod/future_icons.py`, F_*.png).
- [x] Arabic names for 170 hardware parts + 30 console features (`mod/hardware_ar.py`); brand names stay English.
- [x] Logos/icons final: 100 logos (real where available, game-style badge, 18 generated wordmarks), real device photos (Wikidata P18 when article leads with a logo), all 64/128 px verified. INSTALLED 2026-10-21 with full build (17 text files, 236 images, 0 failures).

- [x] 2026-10-21 Point 34 Golden Auto-Tune: BepInEx plugin `mod/plugin/GoldenAutoTune` (netstandard2.1, `dotnet build -c Release`; build.py copies it to BepInEx/plugins, --restore removes it). Clones the game's auto-design button (finds the Button whose onClick calls BUTTON_AutoDesignSettings) into a gold button left of it; sets 8 focus + 3 align via the game's genres.GetFocus/GetAlign (main+sub), 4 priorities = genre GAMEPLAY/GRAPHIC/SOUND/CONTROL weights split over 20 (largest remainder, cap 10; checked for all 19 genres). Arabic label when settings language == 13. Verified: BepInEx log shows plugin loaded. NOT yet verified: button appearing/working in the dev menu (needs user test). The GDD table's percentages differ from the game's real data; the plugin uses the game data.

- [x] User confirmed Golden Auto-Tune works (2026-10-21).
- [x] 2026-10-21 Point 1 Live-Service: plugin `mod/plugin/LiveOps` (labels generated by `mod/plugin/make_labels.py LiveOps`). Built on the game's MMO/F2P model (gameScript.SellGame, called weekly via mainScript.WochenUpdates -> games.SellAllGames): per own live game (gameTyp 1 MMO / 2 F2P) monetization model (standard / cosmetics +15% +sentiment / battle pass +60% while season <=12 weeks old else stale / P2W +150%, -2% players/week, -sentiment), seasons (cost max(20k*(year-1975), 3x weekly income), +20 interest, 6-week cooldown), sentiment 0-100 drives AddMMO/F2PInteresse weekly, full servers (games_.freeServerPlatz<=0) hurt sentiment, DAU/MAU/retention 30/60/90 from weekly history, Relaunch 2.0 once (resets wear: weeksOnMarket<=26), Sunset (RemoveFromMarket). Extra income via mS_.Earn(cat 7 MMO / 8 in-app); costs via Pay(cat 12 marketing). Data saved as <savetitle><slot>.liveops.json in persistentDataPath; reset on new game; disabled in multiplayer. Window: F9 or LIVE button (shown only with live games), game font, Arabic right-aligned. Verified: loads, patches applied. NOT verified in play (needs an MMO/F2P on the market).

- [x] 2026-10-21 Point 1 Live Ops v2 (redesign after user feedback) BUILT, INSTALLED, VERIFIED IN GAME (save "Y2032 Game4X",
  not saved). Old IMGUI/F9 window deleted. Files in `mod/plugin/LiveOps/`:
  - `LiveOps.cs` core: Battle Pass inside the game's GetInAppPurchaseMoneyPerWeek / GetInAppPurchaseHate / HasInAppPurchases
    (pass alone counts as in-app content); weekly tick after gameScript.SellGame (sentiment, interest, news, 52-week history);
    content applied in a PREFIX of taskF2PUpdate.Complete (collab/relaunch cost booked once so automatic repeats don't rebuy).
    Balance vs game data (game update items: 1.5-3.8% of dev cost, +5..13 interest; in-app hate 5..25, price 0.3..1.6):
    season 6% / +12 interest; event 3% / +6 x timing (1.5 in season, 0.6 off); collab 4% + licence fee / (4+10q) x genre fit;
    relaunch 25% / interest->75 + wear reset, once, after 104 weeks; pass price 1.0, hate 6 (fresh) .. 14 (stale).
  - `InAppPage.cs`: dev wizard page 6: "Seasonal Battle Pass" button beside enable/disable-all + "Model:" line under the hate bar.
  - `UpdatePanel.cs`: F2P update menu grows 150px: "Live campaign (optional)" header, 5 type buttons (RTL order in Arabic),
    arrows + choice box (topics researched / 8 occasions incl. real Ramadan month / licences existing by now, top 40), info +
    extra cost. With a campaign chosen BUTTON_Start is replaced by our copy of the game's start (adds cost, points, pending).
    !! Do NOT Harmony-patch methods returning long (GetDevCosts): on this x86 Mono the result gets corrupted (showed $1.065.353.216).
  - `StatsPanel.cs`: sales window (Menu_Game_Umsatz) grows 200px for own MMO/F2P games: "Live service" scroll list (model, pass,
    season, event, collab, sentiment, players, DAU, MAU, retention 30/60/90, pass revenue/week, relaunched) + 26-week bar chart.
    Scroll sensitivity copied from the game's own lists (40); drag and wheel both work; list opens at the top.
  - `UiKit.cs`: GrowWindow keeps full-height Fill_Window stretching and re-centres the window.
  Verified on screen: all three UIs, cost box, season start -> news "season 1 started - Dance", stats after 4 weeks.
- Dev tool `mod/plugin/UiDump` (NOT installed now, removed from plugins; build.py never ships it): F10 dump UI, F11 open F2P
  update menu for newest own F2P game, F12 its sales window, F8 new-game wizard, F9 finish running F2P updates + 4 x
  mainScript.WochenUpdates (game clock does not run while the game window is unfocused under computer-use).
  To use: copy its bin dll into BepInEx/plugins, test, delete again. User saves backed up in the session scratchpad before tests.

## Current Stage
Repo + team workflow live (PR #1 merged into main). Point 1 (Live Ops v2) done and installed; waiting for the
user's in-game test. Point 2 (Stock Market) implemented and ready for testing on `feature/stock-market`.

## Stock Market System (Point 2) - COMPLETED
**Branch:** `feature/stock-market`
**Plugin:** `mod/plugin/StockMarket/` (7 files, builds to StockMarket.dll)

### What's Implemented:
1. **Studio IPO & Board of Directors (Player Studio):**
   - Studio Valuation Formula: Cash + 12-month net revenue + IP library value + fans + studio rating level.
   - Public Equity Offering: User selects 10%, 20%, 30%, or 40% offering size.
   - Financial Underwriting: 3% banking underwriting fee deducted, net capital instantly credited to cash reserves.
   - Board Governance: Dynamic board sentiment (Satisfied, Cautious, Demanding, Hostile), discontent meter (0-100%), investor confidence (0-100%).
   - Quarterly Revenue Targets: Evaluated every 3 months; meeting targets improves board satisfaction, missing increases discontent.
   - Dividends System: Discretionary dividends and emergency dividend mechanism (calms hostile board when discontent >= 75%).
   - Capital Restrictions: Hostile board enforces spending cap blocks on aggressive expenditures.

2. **Tiered Ownership & Majority Control (Competitor Studios):**
   - 5%-15%: Dividend rights (quarterly cash payouts from competitor revenues).
   - 25%: Secret Project Intel (inspect competitor's games in development, progress %, genre, hype).
   - 51%: Majority Control Directives (mandate game genre and/or mandate exclusive publishing through player studio).
   - 100%: Full Acquisition & Subsidiary Absorption (converts competitor to player's official subsidiary via SetAsTochterfirma).
   - 40%-49%: Hostile Creeping Takeover Defense (AI board Poison Pill activates to dilute hostile stakes).

3. **100% Native Game UI Overhaul:**
   - Window styling clones authentic MGT2 assets: `window1_Border`, `window1_FillVerlauf`, `titleLeiste`, `whiteSimple`, and `uiSquareVerlauf`.
   - Typography: 100% bound to `ms.fonts[0]`.
   - Localization: Full Arabic localization generated via `arabic_game.to_game()` shaped/reversed unicode presentation forms.
   - Sound & Lifecycle: Game pauses on window open (`guiMain.OpenMenu()`) and unpauses on close, triggering native `sfxScript` audio cues.
   - Menu Integration: Clean button in Management dropdown (`GUI_MainButtons.uiObjects[9]`), ugly floating button removed.
   - Multi-Screenshot Capture: F7 captures timestamped screenshots (`screenshot_mgt2_YYYYMMDD_HHmmss.png`).
   - Save/Load Persistence: Hooks `savegameScript.Save` and `savegameScript.Load`.

### Files Created/Updated:
- `StockMarket.cs` - Core plugin, data models, company valuation, savegame hooks
- `StockMarketWindow.cs` - Complete native UI (tabs, competitor rows, IPO dashboard, directives/intel modals)
- `PlayerIpoManager.cs` - Valuation formula, IPO execution, quarterly meetings, dividends, spending caps
- `MajorityControlManager.cs` - 25% intel, 51% genre/publishing mandates, 100% subsidiary absorption, poison pill defenses
- `ShareTransactions.cs` - Buy/sell transactions and dialogs
- `ManagementIntegration.cs` - Management dropdown button injection, F8 shortcut, F7 timestamped capture
- `Labels.cs` - Arabic & English localization dictionaries (generated by `make_labels.py StockMarket`)
- `StockMarket.csproj` - Build configuration

## Next Steps
1. User tests Stock Market & IPO system in-game (Management dropdown or F8).
2. Begin GDD Point 3: Dynamic Publishing & Exclusivity Deals.

## Completion Timestamp
(open)
