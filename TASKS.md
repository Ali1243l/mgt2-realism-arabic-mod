# Tasks & Progress (Ali1243l/mgt2-realism-arabic-mod)

How we use this file:

1. Before starting a task, create a branch (`feature/<short-name>`), add your name and branch to the table below,
   commit and push the branch so the other person sees it immediately.
2. When the task is finished, move it to **Done** in the same pull request as the code.

---

## Currently in progress

| GDD # | System | Owner | Branch | Notes |
|------:|--------|-------|--------|-------|
| 13 | Crunch Culture, Burnout, Unions & Talent Poaching | Ali | `feature/crunch-unions` | Next up in Batch 6 |
| 14 | DRM (Denuvo), Piracy Cracking & Anti-Cheat | Ali | `feature/drm-anticheat` | Next up in Batch 6 |
| 15 | Dynamic Sales Festivals & Wishlist Conversion | Ali | `feature/sales-wishlists` | Next up in Batch 6 |

---

## Done

| Area / GDD # | What was done | Main files & In-Game Locations |
|--------------|---------------|--------------------------------|
| Data — Platforms | 133 platforms: real names, dates, types, prices, tech, market share, Internet/handheld/stationary flags, devkit costs | `mod/build.py`, `mod/future.py` |
| Data — Publishers | 100 publishers/developers: real names, dates, stars, fan counts, platform relations, speed, genre focus | `mod/build.py`, `mod/future.py` |
| Data — Logos | 100 publisher logos + 133 platform photos mapped and cropped | `mod/build.py`, `logos/`, `platform_pics/` |
| Data — Hardware brands | Real CPU/GPU/RAM/sound/disc/controller/case/monitor component names | `mod/build.py` |
| Data — Game & NPC names | Famous game names and industry personalities | `mod/build.py` |
| Arabic localization | ~1,190 lines shaped and reversed, Latin/number order fixed, genres/themes/menus/events/countries translated | `mod/arabic_game.py`, `mod/build.py`, `mod/future.py` |
| GDD 8 — Future up to 2100 | 18 future platforms, 9 studios, 16 engine features, 7 hardware topics, 32 news events (2030–2095) | `mod/future.py` |
| Central Mod Hub & HUD | Unified Mod Systems Hub on bottom bar (`GUI_MainButtons`) + F12 hotkey | `mod/plugin/ModHub/` |
| GDD 34 — Golden Auto-Tune | Injected inside Game Development (`Menu_DevGame`): auto-tunes optimal sliders for chosen genres | `mod/plugin/GoldenAutoTune/` |
| GDD 1 — Live-Service & GaaS | Battle Pass card in in-app page, Season / Event / Collab / 2.0 Relaunch in F2P updates & MMO add-ons | `mod/plugin/LiveOps/` |
| GDD 2 — Stock Market & IPO | Injected inside City Bank (`Menu_Bank`) & Management Dropdown: IPO, 5/25/51/100% stakes, Poison Pill (`F8`) | `mod/plugin/StockMarket/` |
| GDD 3 — Publishing Deals | Injected inside Publisher Selection (`Menu_Dev_SelectPublisher`) & Publishing Dropdown: pitches & bidding wars (`F6`) | `mod/plugin/PublishingDeals/` |
| GDD 4 — Community Dynamics | Injected inside Marketing Office (`Menu_Marketing_Main`) & Statistics Dropdown: hype traps & review-bombing (`F7`) | `mod/plugin/CommunityDynamics/` |
| GDD 5 — Smart Automation & ROI | Injected inside Staff Overview (`Menu_MitarbeiterUebersicht`) & Staff Dropdown: role auto-assign & ROI analytics (`F9`) | `mod/plugin/SmartAutomation/` |
| GDD 6 — Custom Silicon & GPUs | Injected inside Console Hardware Lab (`Menu_Dev_KonsoleMain`): 90nm-1nm R&D, discrete GPUs, cooling RRoD (`F10`) | `mod/plugin/CustomSilicon/` |
| GDD 7 — Tech Debt & Compilers | Injected inside Engine Development (`Menu_Dev_EngineMain`): ASM/C/AI compilers, spaghetti code decay, core refactor (`F11`) | `mod/plugin/TechDebt/` |
| GDD 9 — Esports Ecosystem | Injected inside In-App / Live Management (`Menu_InAppVerwalten`): Major tournaments, netcode, meta patches (`F5`) | `mod/plugin/EsportsEcosystem/` |
| GDD 10 — Transmedia Adaptations | Injected inside Merch Fanshop (`Menu_Fanshop`): Hollywood blockbusters, Anime series, collector statues (`F4`) | `mod/plugin/TransmediaMerch/` |
| GDD 11 — Early Access & Crowdfunding | Injected inside Game Development (`Menu_DevGame`): Kickstarter campaigns, roadmap updates, dev hell decay (`F3`) | `mod/plugin/EarlyAccess/` |
| GDD 12 — Modding SDK & UGC Workshop | Injected inside Game Updates & DLC (`Menu_Dev_UpdateSelectGame`): Mod SDK tiers, long-tail sales, spinoff acquisition (`F2`) | `mod/plugin/ModWorkshop/` |
