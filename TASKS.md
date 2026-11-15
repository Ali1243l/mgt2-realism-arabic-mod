# Tasks & Progress (Ali1243l/mgt2-realism-arabic-mod)

How we use this file:

1. Before starting a task, create a branch (`feature/<short-name>`), add your name and branch to the table below,
   commit and push the branch so the other person sees it immediately.
2. When the task is finished, move it to **Done** in the same pull request as the code.

---

## Currently in progress

| GDD # | System | Owner | Branch | Notes |
|------:|--------|-------|--------|-------|
| 10 | Transmedia (Hollywood Movies, Anime Series) & Merchandising | Ali | `feature/transmedia-merch` | Next up in Batch 5 |
| 11 | Early Access & Crowdfunding Campaigns | Ali | `feature/early-access` | Next up in Batch 5 |
| 12 | Official Modding SDK & UGC Workshop | Ali | `feature/mod-ugc` | Next up in Batch 5 |

---

## Done

| Area / GDD # | What was done | Main files |
|--------------|---------------|------------|
| Data — Platforms | 133 platforms: real names, dates, types, prices, tech, market share, Internet/handheld/stationary flags, devkit costs | `mod/build.py`, `mod/future.py` |
| Data — Publishers | 100 publishers/developers: real names, dates, stars, fan counts, platform relations, speed, genre focus | `mod/build.py`, `mod/future.py` |
| Data — Logos | 100 publisher logos + 133 platform photos mapped and cropped | `mod/build.py`, `logos/`, `platform_pics/` |
| Data — Hardware brands | Real CPU/GPU/RAM/sound/disc/controller/case/monitor component names | `mod/build.py` |
| Data — Game & NPC names | Famous game names and industry personalities | `mod/build.py` |
| Arabic localization | ~1,190 lines shaped and reversed, Latin/number order fixed, genres/themes/menus/events/countries translated | `mod/arabic_game.py`, `mod/build.py`, `mod/future.py` |
| GDD 8 — Future up to 2100 | 18 future platforms, 9 studios, 16 engine features, 7 hardware topics, 32 news events (2030–2095) | `mod/future.py` |
| GDD 34 — Golden Auto-Tune | Button in `Menu_DevGame` sets optimal focus, alignment, AP split and age rating for main + sub genre | `mod/plugin/GoldenAutoTune/` |
| GDD 1 — Live-Service & GaaS | Battle Pass card in in-app page, Season / Event / Collab / 2.0 Relaunch in F2P updates & MMO add-ons, live stats (DAU, MAU, retention, sentiment, season) in sales window, save/load | `mod/plugin/LiveOps/` |
| GDD 2 — Stock Market & IPO | Weekly GBM share drift, release shockwave, 4-year macro cycle, 5%/25%/51%/100% tiered stakes, Poison Pill & White Knight defense, Player IPO (10%-40% float) & Board meetings, native `UiKit` tabbed window (`F8`) | `mod/plugin/StockMarket/` |
| GDD 3 — Publishing Deals & Bidding Wars | Studio behavioral archetypes, offer evaluation score S in [0,100], patience concession counter-offers, AI rival bidding war escalation, native UI window (`F6`) | `mod/plugin/PublishingDeals/` |
| GDD 4 — Community, Hype Trap & Review Bombing | Expectation debt, logistic review-bomb severity, Pareto streamer viral multiplier, apology & roadmap recovery, native UI window (`F7`) | `mod/plugin/CommunityDynamics/` |
| GDD 5 — Smart Automation & Executive ROI | One-click smart role assignment, exhausted staff break rotation, Executive Game & Engine ROI dashboard (`F9`) | `mod/plugin/SmartAutomation/` |
| GDD 6 — Custom Silicon, Foundries & PC GPUs | Lithography node R&D (90nm to 1nm), Murphy wafer yield model, Tier-1 vs Budget foundries, discrete PC GPU product lines, Arrhenius thermal failure & RRoD recall campaign (`F10`) | `mod/plugin/CustomSilicon/` |
| GDD 7 — Programming Languages & Tech Debt | Assembly/C, C++/C#, and AI Neural compilers, engine Spaghetti Code tech debt accumulation, dev speed decay & quadratic bug multiplier, full Engine Core Refactoring (`F11`) | `mod/plugin/TechDebt/` |
| GDD 9 — Esports & Competitive Ecosystem | Competitive readiness & netcode scoring, Major & World Cup tournament hosting, broadcast & franchise slot deals, Meta balance entropy & patches (`F5`) | `mod/plugin/EsportsEcosystem/` |
