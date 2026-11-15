# Tasks & Progress (Ali1243l/mgt2-realism-arabic-mod)

How we use this file:

1. Before starting a task, create a branch (`feature/<short-name>`), add your name and branch to the table below,
   commit and push the branch so the other person sees it immediately.
2. When the task is finished, move it to **Done** in the same pull request as the code.

---

## Currently in progress

| GDD # | System | Owner | Branch | Notes |
|------:|--------|-------|--------|-------|
| 18 | Collector's Editions & Physical Warehouse Logistics | Ali | `feature/collectors-logistics` | Next up in Batch 8 |
| 19 | Digital Store Wars (Steam 30% vs Epic 12% vs GOG DRM-Free) | Ali | `feature/digital-store-wars` | Next up in Batch 8 |
| 20 | Studio Culture, Office Perks & Remote Work | Ali | `feature/studio-perks` | Next up in Batch 8 |

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
| UI — Native MGT2 UI Skin Patch | Auto-reskins all mod windows to match MGT2's exact Silver-White (`#EBEBED`) & Cerulean-Blue (`#1C7DC2`) native UI, injects into "طوّر لعبة" (`Menu_DevGameMain`) and adds `F1` Unified Hub | `mod/plugin/Mgt2NativeUiPatch/` |
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
| GDD 13 — Crunch Culture, Burnout & Unions | Injected inside Staff Overview (`Menu_MitarbeiterUebersicht`): Mandatory crunch, Burnout index, Union strikes & CBA, Golden Handcuffs (`Ctrl+1`) | `mod/plugin/CrunchUnions/` |
| GDD 14 — DRM (Denuvo), Piracy & Anti-Cheat | Injected inside Buy CopyProtect (`Menu_BuyCopyProtect`) & AntiCheat (`Menu_BuyAntiCheat`): Fortress Denuvo-VM, post-launch DRM removal, Kernel Anti-Cheat (`Ctrl+2`) | `mod/plugin/DrmAntiCheat/` |
| GDD 15 — Dynamic Sales Festivals & Wishlists | Injected inside Marketing Office (`Menu_Marketing_Main`): Pre-launch wishlist pool, Summer/Black Friday sales (1.65x conversion), tactical discounts (`Ctrl+3`) | `mod/plugin/SalesWishlists/` |
| GDD 16 — Dynamic Real-World Events & Crises | Injected inside Marketing Office (`Menu_Marketing_Main`): Crypto/AI GPU shortage, Global Quarantine Lockdown (+200% sales), AAA Source-Code Leak crisis (`Ctrl+4`) | `mod/plugin/WorldEvents/` |
| GDD 17 — The Game Awards (GOTY) & Deluxe Edition | Injected inside Annual Awards Ceremony (`Menu_Awards`): December ceremony, 'For Your Consideration' review kits ($85k), GOTY trophy, GOTY Deluxe Edition (+30% units) (`Ctrl+5`) | `mod/plugin/GameAwardsGoty/` |
