// GDD point 2: Stock Market, Tiered Ownership & Hostile Takeover
// Replaces the all-or-nothing "buy company" with a realistic stock market:
// - IPO: convert your studio to public, sell shares for massive capital
// - Tiered ownership: 5% (dividends), 25% (board seat + intel), 51% (control), 100% (full merge)
// - Hostile takeover: buy shares when competitors struggle, AI defends with poison pill
// - Stock trading window in Management menu, company valuation based on revenue and game success
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod.StockMarket
{
    [Serializable]
    public class StockOwnership
    {
        public int companyId;           // publisherScript.myID
        public float playerShare;       // 0-100%
        public float publicShare;       // shares on open market
        public bool isPublic;           // IPO completed
        public long marketCap;          // current valuation
        public long lastDividend;       // last payout to shareholders
        public int lastDividendDate;    // year * 12 + month
        public Dictionary<int, float> otherOwners = new Dictionary<int, float>(); // AI shareholders
        public bool poisonPillActive;   // defense against hostile takeover
        public int poisonPillExpiry;    // date when it expires
        public bool mandateExclusivePublishing; // 51% majority directive: publish through player
        public int mandateGenre = -1;           // 51% majority directive: -1 = free choice, >= 0 = forced genre
        public bool absorbedAsSubsidiary;       // 100% full acquisition: converted to official subsidiary
        public double currentSharePrice;        // Active live market share price
        public double previousSharePrice;       // Track weekly price trend
        public double priceTrendPercent;        // +% / -% drift
    }

    [Serializable]
    public class StockMarketSave
    {
        public List<StockOwnership> ownerships = new List<StockOwnership>();
        public bool playerIsPublic;     // player went IPO
        public float playerPublicShare; // % of player company on market
        public PlayerIpoState ipoState = new PlayerIpoState();
    }

    [BepInPlugin("ali.mgt2.stockmarket", "Stock Market & Hostile Takeover", "1.0.0")]
    public class StockMarketPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.stockmarket").PatchAll(typeof(StockMarketPlugin).Assembly);
            Log.LogInfo("Stock Market loaded");
        }
    }

    internal static class StockData
    {
        internal static StockMarketSave Data = new StockMarketSave();

        internal static StockOwnership GetOrCreate(int companyId)
        {
            var stock = Data.ownerships.FirstOrDefault(x => x.companyId == companyId);
            if (stock == null)
            {
                stock = new StockOwnership
                {
                    companyId = companyId,
                    playerShare = 0f,
                    publicShare = 0f,
                    isPublic = false
                };
                Data.ownerships.Add(stock);
            }
            return stock;
        }

        internal static long CalculateCompanyValue(publisherScript pub, mainScript ms)
        {
            if (pub == null || ms == null) return 0;

            // Base value from game's firmenwert
            long baseValue = pub.firmenwert;

            // Adjust for recent performance (last 12 months revenue)
            long recentRevenue = 0;
            if (pub.tf_umsatz != null && pub.tf_umsatz.Length > 0)
            {
                for (int i = 0; i < Mathf.Min(12, pub.tf_umsatz.Length); i++)
                {
                    recentRevenue += pub.tf_umsatz[i];
                }
            }

            // Market cap = base value + 3x annual revenue
            long marketCap = baseValue + (recentRevenue / 4);

            // Bonus for successful games and awards
            if (pub.awards != null)
            {
                int totalAwards = pub.awards.Sum();
                marketCap += totalAwards * 1000000L;
            }

            // Penalty if company is struggling (low firmenwert relative to revenue)
            if (baseValue < recentRevenue / 10 && recentRevenue > 0)
            {
                marketCap = (long)(marketCap * 0.6f); // 40% discount for distressed company
            }

            return Math.Max(marketCap, 100000L);
        }

        internal static double CalculateSharePrice(publisherScript pub, mainScript ms)
        {
            long cap = CalculateCompanyValue(pub, ms);
            return Math.Round((double)cap / 1000000.0, 2);
        }

        internal static string GetShareTierName(float share)
        {
            if (share >= 100f) return "Full Ownership";
            if (share >= 51f) return "Controlling Interest";
            if (share >= 25f) return "Board Seat";
            if (share >= 5f) return "Dividend Rights";
            return "No Stake";
        }

        internal static string GetShareTierDescription(float share)
        {
            if (share >= 100f) return "Complete control - merge into your company";
            if (share >= 51f) return "Management control - direct their game development";
            if (share >= 25f) return "Board seat - see their secret projects early";
            if (share >= 5f) return "Dividends - earn from their revenue";
            return "No ownership";
        }
    }

    // Save/Load system: hooks into the game's save/load and stores stock data as JSON
    internal static class Persistence
    {
        private static string SavePath(mainScript ms, int slot) =>
            Path.Combine(Application.persistentDataPath, ms.GetSavegameTitle() + slot + ".stockmarket.json");

        internal static void Save(mainScript ms, int slot)
        {
            try
            {
                if (ms.multiplayer) return;
                StockData.Data.ipoState = PlayerIpoManager.State;
                string json = JsonUtility.ToJson(StockData.Data, true);
                File.WriteAllText(SavePath(ms, slot), json);
                StockMarketPlugin.Log.LogInfo($"Stock market saved: {StockData.Data.ownerships.Count} companies, IPO: {StockData.Data.playerIsPublic}");
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError($"Save failed: {ex.Message}");
            }
        }

        internal static void Load(mainScript ms, int slot)
        {
            try
            {
                if (ms.multiplayer)
                {
                    StockData.Data = new StockMarketSave();
                    PlayerIpoManager.ResetState();
                    return;
                }

                string path = SavePath(ms, slot);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    StockData.Data = JsonUtility.FromJson<StockMarketSave>(json) ?? new StockMarketSave();
                    PlayerIpoManager.State = StockData.Data.ipoState;
                    StockMarketPlugin.Log.LogInfo($"Stock market loaded: {StockData.Data.ownerships.Count} companies, IPO: {StockData.Data.playerIsPublic}");
                }
                else
                {
                    StockData.Data = new StockMarketSave();
                    PlayerIpoManager.ResetState();
                    StockMarketPlugin.Log.LogInfo("New stock market initialized");
                }
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError($"Load failed: {ex.Message}");
                StockData.Data = new StockMarketSave();
                PlayerIpoManager.ResetState();
            }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class SavePatch
    {
        private static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) Persistence.Save(ms, i);
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class LoadPatch
    {
        private static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) Persistence.Load(ms, i);
        }
    }

    [HarmonyPatch(typeof(mainScript), "Start_NewGame")]
    internal static class NewGamePatch
    {
        static void Postfix()
        {
            StockData.Data = new StockMarketSave();
            PlayerIpoManager.ResetState();
            StockMarketPlugin.Log.LogInfo("Stock market reset for new game");
        }
    }
}
