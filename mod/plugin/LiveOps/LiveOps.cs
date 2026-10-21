// GDD point 1: Live-Service & Games-as-a-Service, integrated into the game's own systems:
// - The monetization model is the in-app selection in the development wizard (the game already
//   prices and "hates" each item); this adds a seasonal Battle Pass card there.
// - Seasons, live events, collaborations and the 2.0 relaunch are chosen in the game's F2P update
//   menu and take effect when that update task completes.
// - Live stats (DAU/MAU/retention, sentiment, season...) are a section of the game's sales window.
// The Battle Pass is added inside the game's own formulas (GetInAppPurchaseMoneyPerWeek and
// GetInAppPurchaseHate), so revenue and player reaction follow the game's existing economy.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [Serializable]
    public class LiveGame
    {
        public int id;
        public bool battlePass;
        public int season;
        public int seasonTheme = -1;
        public int weeksSinceSeason = 99;
        public int eventOcc = -1;
        public int eventWeeks;
        public int collab = -1;
        public int collabWeeks;
        public float sentiment = 60f; // 0..100
        public bool relaunched;
        public int newsCooldown;
        public List<long> history = new List<long>();
    }

    [Serializable]
    public class Pending
    {
        public int taskId;
        public int type;   // ContentType
        public int choice; // theme / occasion / licence
        public long cost;  // the campaign's share of the task's devCosts
    }

    [Serializable]
    public class LiveSave
    {
        public List<LiveGame> games = new List<LiveGame>();
        public List<Pending> pendings = new List<Pending>();
    }

    internal static class ContentType
    {
        public const int None = 0, Season = 1, Event = 2, Collab = 3, Relaunch = 4;
    }

    [BepInPlugin("ali.mgt2.liveops", "Live Ops", "2.0.0")]
    public class LiveOpsPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.liveops").PatchAll(typeof(LiveOpsPlugin).Assembly);
            Log.LogInfo("Live Ops loaded");
        }
    }

    internal static class Live
    {
        internal static LiveSave Data = new LiveSave();
        internal const float PassPrice = 1.0f;       // per buyer per month, same scale as the game's items
        internal const int FreshWeeks = 16, EventWeeks = 4, CollabWeeks = 6, OccasionCount = 8;

        internal static LiveGame Find(int id) => Data.games.FirstOrDefault(x => x.id == id);

        internal static LiveGame Get(gameScript g)
        {
            var s = Find(g.myID);
            if (s == null)
            {
                s = new LiveGame { id = g.myID };
                Data.games.Add(s);
            }
            return s;
        }

        internal static bool IsLive(gameScript g, mainScript ms) =>
            g != null && ms != null && !ms.multiplayer && g.isOnMarket && g.IsMyGame() && (g.gameTyp == 1 || g.gameTyp == 2);

        /// <summary>0 = stale, 1 = brand new season. A pass without any season counts as half fresh.</summary>
        internal static float Freshness(LiveGame s)
        {
            if (s == null || !s.battlePass) return 0f;
            if (s.season == 0) return 0.5f;
            return Mathf.Clamp(1f - s.weeksSinceSeason / (float)FreshWeeks, 0.15f, 1f);
        }

        // the game's items hate 5 (cosmetics) .. 25 (special characters); a fresh pass sits just above cosmetics
        internal static float PassHate(LiveGame s) => s != null && s.battlePass ? 6f + 8f * (1f - Freshness(s)) : 0f;

        internal static float RevenueMultiplier(LiveGame s) =>
            s == null ? 1f : 1f + (s.eventWeeks > 0 ? 0.15f : 0f) + (s.collabWeeks > 0 ? 0.10f : 0f);

        internal static void AddInterest(gameScript g, float f)
        {
            if (g.gameTyp == 1) g.AddMMOInteresse(f);
            else g.AddF2PInteresse(f);
        }

        internal static float Interest(gameScript g) => g.gameTyp == 1 ? g.mmoInteresse : g.f2pInteresse;

        internal static void News(mainScript ms, gameScript g, string text) =>
            ms.guiMain_?.CreateTopNewsInfo(Txt.Join(g.GetNameWithTag(), "-", text));

        /// <summary>Once per week per live game, right after the game's own sales update.</summary>
        internal static void WeeklyTick(gameScript g, mainScript ms)
        {
            var s = Get(g);
            s.weeksSinceSeason++;
            if (s.eventWeeks > 0) s.eventWeeks--;
            if (s.collabWeeks > 0) s.collabWeeks--;
            if (s.newsCooldown > 0) s.newsCooldown--;

            float fresh = Freshness(s);
            bool serversFull = ms.games_ != null && ms.games_.freeServerPlatz <= 0;
            // where the community's mood is heading: monetization pressure, fresh content, server pain
            float target = 75f - g.GetInAppPurchaseHate() * 0.6f
                           + (s.eventWeeks > 0 ? 10f : 0f) + (s.collabWeeks > 0 ? 8f : 0f)
                           - (serversFull ? 20f : 0f) - (s.battlePass && fresh < 0.35f ? 10f : 0f);
            s.sentiment = Mathf.Clamp(s.sentiment + (target - s.sentiment) * 0.15f, 0f, 100f);
            AddInterest(g, (s.sentiment - 50f) / 50f * 0.35f + (s.collabWeeks > 0 ? 0.6f : 0f));

            if (s.newsCooldown == 0 && serversFull) { News(ms, g, LiveOpsLabels.NewsServers); s.newsCooldown = 8; }
            else if (s.newsCooldown == 0 && s.battlePass && fresh < 0.35f) { News(ms, g, LiveOpsLabels.NewsStale); s.newsCooldown = 12; }

            s.history.Add(g.abonnements);
            if (s.history.Count > 52) s.history.RemoveAt(0);
        }

        // ---------------- content chosen in the F2P update menu ----------------
        // the game's own update items cost 1.5-3.8% of the development costs for +5..+13 interest each
        internal static long ContentCost(gameScript g, mainScript ms, licences lic, int type, int choice)
        {
            long baseCost = Math.Min(g.GetGesamteEntwicklungskosten(), 100000000L);
            switch (type)
            {
                case ContentType.Season: return (long)(baseCost * 0.06f);
                case ContentType.Event: return (long)(baseCost * 0.03f);
                case ContentType.Collab: return (long)(baseCost * 0.04f) + CollabFee(ms, lic, choice);
                case ContentType.Relaunch: return (long)(baseCost * 0.25f);
                default: return 0;
            }
        }

        internal static long CollabFee(mainScript ms, licences lic, int i) =>
            lic == null || i < 0 ? 0 : (long)(lic.licence_QUALITY[i] * (3000 + Mathf.Clamp(ms.year - 1976, 0, 60) * 120));

        internal static bool CanRelaunch(gameScript g) => !Get(g).relaunched && g.weeksOnMarket >= 104;

        internal static int RamadanMonth(int year)
        {
            // Ramadan 2024 began on day 70 (11 March); the lunar year is ~10.88 days shorter
            float day = 70f - 10.88f * (year - 2024);
            day = ((day % 365.25f) + 365.25f) % 365.25f;
            return Mathf.Clamp(Mathf.FloorToInt(day / 30.44f) + 1, 1, 12);
        }

        internal static bool InSeason(int occ, mainScript ms, gameScript g)
        {
            int m = ms.month;
            switch (occ)
            {
                case 0: int r = RamadanMonth(ms.year); return m == r || m == r % 12 + 1; // Ramadan, then Eid
                case 1: return m == 10;
                case 2: return m == 12 || m == 1;
                case 3: return m == 1 || m == 2;
                case 4: return m >= 6 && m <= 8;
                case 5: return m == g.date_start_month;
                case 6: return m == 7 || m == 8;
                case 7: return m == 11;
                default: return true;
            }
        }

        internal static float GenreFit(licences lic, gameScript g, int i)
        {
            if (lic.licence_GENREGOOD[i] == g.maingenre) return 1.4f;
            if (lic.licence_GENREBAD[i] == g.maingenre) return 0.5f;
            return 1f;
        }

        internal static void ApplyContent(gameScript g, mainScript ms, Pending p)
        {
            var s = Get(g);
            var lic = ms.GetComponent<licences>();
            var ts = ms.GetComponent<textScript>();
            switch (p.type)
            {
                case ContentType.Season:
                    s.season++;
                    s.weeksSinceSeason = 0;
                    s.seasonTheme = p.choice;
                    AddInterest(g, 12f);
                    s.sentiment = Mathf.Min(100f, s.sentiment + 6f);
                    News(ms, g, Txt.Join(LiveOpsLabels.NewsSeason, s.season.ToString(), "-", ts.GetThemes(p.choice)));
                    break;
                case ContentType.Event:
                    float timing = InSeason(p.choice, ms, g) ? 1.5f : 0.6f;
                    s.eventOcc = p.choice;
                    s.eventWeeks = EventWeeks;
                    AddInterest(g, 6f * timing);
                    s.sentiment = Mathf.Min(100f, s.sentiment + 4f * timing);
                    News(ms, g, Txt.Join(LiveOpsLabels.NewsEvent, "-", Txt.Occasion(p.choice)));
                    break;
                case ContentType.Collab:
                    float fit = GenreFit(lic, g, p.choice);
                    s.collab = p.choice;
                    s.collabWeeks = CollabWeeks;
                    AddInterest(g, (4f + 10f * lic.licence_QUALITY[p.choice] / 100f) * fit);
                    s.sentiment = Mathf.Min(100f, s.sentiment + 5f * fit);
                    News(ms, g, Txt.Join(LiveOpsLabels.NewsCollab, "-", lic.GetName(p.choice)));
                    break;
                case ContentType.Relaunch:
                    s.relaunched = true;
                    g.weeksOnMarket = Math.Min(g.weeksOnMarket, 26); // the game's wear-out restarts
                    AddInterest(g, 75f - Interest(g));
                    s.sentiment = Mathf.Max(s.sentiment, 65f);
                    News(ms, g, LiveOpsLabels.NewsRelaunch);
                    break;
            }
        }

        // ---------------- persistence: a JSON file next to each save slot ----------------
        private static string SavePath(mainScript ms, int slot) =>
            Path.Combine(Application.persistentDataPath, ms.GetSavegameTitle() + slot + ".liveops.json");

        internal static void Save(mainScript ms, int slot)
        {
            try { File.WriteAllText(SavePath(ms, slot), JsonUtility.ToJson(Data)); }
            catch (Exception e) { LiveOpsPlugin.Log.LogError("save failed: " + e.Message); }
        }

        internal static void Load(mainScript ms, int slot)
        {
            try
            {
                string p = SavePath(ms, slot);
                Data = File.Exists(p) ? JsonUtility.FromJson<LiveSave>(File.ReadAllText(p)) ?? new LiveSave() : new LiveSave();
            }
            catch (Exception e)
            {
                LiveOpsPlugin.Log.LogError("load failed: " + e.Message);
                Data = new LiveSave();
            }
        }
    }

    // ---------------- hooks into the game's own simulation ----------------
    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class TickPatch
    {
        private static void Postfix(gameScript __instance)
        {
            if (Live.IsLive(__instance, __instance.mS_))
                Live.WeeklyTick(__instance, __instance.mS_);
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.GetInAppPurchaseMoneyPerWeek))]
    internal static class PassRevenuePatch
    {
        // the game: money per purchase = sum(item prices) * 0.25; the pass adds its price, scaled by freshness
        private static void Postfix(gameScript __instance, ref float __result)
        {
            var s = Live.Find(__instance.myID);
            if (s == null) return;
            if (s.battlePass) __result += Live.PassPrice * 0.25f * (0.35f + 0.65f * Live.Freshness(s));
            __result *= Live.RevenueMultiplier(s);
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.GetInAppPurchaseHate))]
    internal static class PassHatePatch
    {
        private static void Postfix(gameScript __instance, ref float __result) => __result += Live.PassHate(Live.Find(__instance.myID));
    }

    [HarmonyPatch(typeof(taskF2PUpdate), "Complete")]
    internal static class UpdateCompletePatch
    {
        // prefix: runs before the game books devCosts and, for automatic updates, pays them again
        private static void Prefix(taskF2PUpdate __instance)
        {
            var p = Live.Data.pendings.FirstOrDefault(x => x.taskId == __instance.myID);
            var g = __instance.gS_;
            if (p == null || g == null) return;
            Live.ApplyContent(g, g.mS_, p);
            bool once = p.type == ContentType.Collab || p.type == ContentType.Relaunch;
            if (once)
            {
                // book the one-off cost now so an automatic repeat does not buy the collab/relaunch again
                g.costs_updates += p.cost;
                __instance.devCosts = Math.Max(0, __instance.devCosts - (int)p.cost);
            }
            // seasons and events repeat with an automatic update
            if (once || !__instance.automatic) Live.Data.pendings.Remove(p);
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.HasInAppPurchases))]
    internal static class PassCountsAsInAppPatch
    {
        // the game only sells in-app content when an item is ticked; a pass alone is enough
        private static void Postfix(gameScript __instance, ref bool __result)
        {
            if (!__result && __instance.gameplayFeatures_DevDone[57] && Live.Find(__instance.myID)?.battlePass == true) __result = true;
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class SavePatch
    {
        private static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) Live.Save(ms, i);
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class LoadPatch
    {
        private static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null) Live.Load(ms, i);
        }
    }

    [HarmonyPatch(typeof(mainScript), nameof(mainScript.InitNewGame))]
    internal static class NewGamePatch
    {
        private static void Postfix() => Live.Data = new LiveSave();
    }
}
