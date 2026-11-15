// GDD Point 9: Esports & Competitive Ecosystem (Pro Leagues, World Cups, Broadcast Rights & Meta Balancing).
// Built for MGT2Mod namespace, netstandard2.1, 32-bit Mono x86 safe.
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [Serializable]
    public class GameEsportsState
    {
        public int gameId;
        public float metaImbalancePct = 15f; // 0% = Perfect Balance, 100% = Broken OP Meta
        public int majorsHosted = 0;
        public bool broadcastDealSigned = false;
        public long weeklyBroadcastRevenue = 0L;
        public long peakConcurrentViewers = 0L;
        public long totalPrizeMoneyAwarded = 0L;
    }

    [Serializable]
    public class EsportsSaveContainer
    {
        public List<GameEsportsState> items = new List<GameEsportsState>();
    }

    [BepInPlugin("ali.mgt2.esportsecosystem", "Esports & Competitive Ecosystem", "1.0.0")]
    public class EsportsEcosystemPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.esportsecosystem").PatchAll(typeof(EsportsEcosystemPlugin).Assembly);
            Log.LogInfo("Esports & Competitive Ecosystem loaded (F5)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
                EsportsWindow.Toggle();
        }
    }

    public static class EsportsEngine
    {
        public static readonly Dictionary<int, GameEsportsState> States = new Dictionary<int, GameEsportsState>();

        public static GameEsportsState GetOrCreate(gameScript g)
        {
            if (g == null) return new GameEsportsState();
            if (!States.TryGetValue(g.myID, out var st))
            {
                st = new GameEsportsState
                {
                    gameId = g.myID,
                    metaImbalancePct = 15f,
                    peakConcurrentViewers = Math.Max(2500L, g.sellsTotal / 12L)
                };
                States[g.myID] = st;
            }
            return st;
        }

        /// <summary>
        /// Calculates Competitive Readiness in [0, 100] from review score and meta health.
        /// </summary>
        public static float CalculateReadinessScore(gameScript g, GameEsportsState st)
        {
            if (g == null || st == null) return 0f;
            float reviewBase = Mathf.Clamp(g.reviewTotal, 30, 100);
            float metaHealth = 100f - Mathf.Clamp(st.metaImbalancePct, 0f, 100f);
            return Mathf.Clamp(reviewBase * 0.55f + metaHealth * 0.45f, 10f, 100f);
        }

        /// <summary>
        /// Hosts a Major / World Cup Championship: invests Prize Pool & Arena cost,
        /// generates ticket + sponsorship return and boosts game hype & evergreen sales.
        /// </summary>
        public static bool HostMajorTournament(mainScript ms, gameScript g, long prizeAndArenaCost)
        {
            if (ms == null || g == null || ms.money < prizeAndArenaCost) return false;
            var st = GetOrCreate(g);

            ms.Pay(prizeAndArenaCost, 1);
            st.majorsHosted++;
            st.totalPrizeMoneyAwarded += prizeAndArenaCost;

            float readiness = CalculateReadinessScore(g, st);
            float prizeFactor = Mathf.Log10(Mathf.Max(10000f, prizeAndArenaCost)) - 4f; // ~1.4 to 2.5
            long fansCount = ms.genres_ != null ? ms.genres_.GetAmountFans() : 0L;
            long viewers = (long)(Math.Max(15000L, g.sellsTotal / 5L + fansCount / 2L) * (readiness / 65f) * Mathf.Max(1f, prizeFactor));
            st.peakConcurrentViewers = Math.Max(st.peakConcurrentViewers, viewers);

            // Arena tickets + sponsor payout
            long arenaReturn = (long)(prizeAndArenaCost * (0.75f + (readiness / 110f)));
            ms.Earn(arenaReturn, 1);
            g.AddHype(18f);
            ms.AddFans((int)Mathf.Clamp(viewers / 80f, 500f, 25000f), -1);
            return true;
        }

        /// <summary>
        /// Sells Franchise Slots & Streaming Broadcast Rights (Twitch / YouTube / TV).
        /// </summary>
        public static bool SignBroadcastAndFranchiseDeal(mainScript ms, gameScript g)
        {
            if (ms == null || g == null) return false;
            var st = GetOrCreate(g);
            float readiness = CalculateReadinessScore(g, st);
            if (readiness < 50f) return false;

            st.broadcastDealSigned = true;
            long upfrontFranchiseFee = 650000L + (long)(st.peakConcurrentViewers * 4.5f);
            st.weeklyBroadcastRevenue = 35000L + (long)(st.peakConcurrentViewers * 0.35f * (readiness / 75f));
            ms.Earn(upfrontFranchiseFee, 1);
            return true;
        }

        /// <summary>
        /// Deploys a competitive Meta Balance Patch to neutralize Overpowered weapons/characters.
        /// </summary>
        public static bool DeployMetaBalancePatch(mainScript ms, gameScript g, long patchCost)
        {
            if (ms == null || g == null || ms.money < patchCost) return false;
            var st = GetOrCreate(g);
            ms.Pay(patchCost, 1);
            st.metaImbalancePct = Mathf.Max(5f, st.metaImbalancePct - 45f);
            g.AddHype(8f);
            return true;
        }

        public static void WeeklyGameTick(gameScript g, mainScript ms)
        {
            if (g == null || ms == null || !g.IsMyGame() || g.isOnMarket == false) return;
            var st = GetOrCreate(g);

            // Weekly Meta Entropy drift (+3.2% per week as players discover OP strategies)
            st.metaImbalancePct = Mathf.Clamp(st.metaImbalancePct + 3.2f, 0f, 100f);

            // If Meta is healthy & Majors hosted, boost ongoing weekly sales & collect broadcast rights
            float readiness = CalculateReadinessScore(g, st);
            if (st.broadcastDealSigned && st.metaImbalancePct < 65f)
            {
                ms.Earn(st.weeklyBroadcastRevenue, 1);
            }

            // Broken Meta (>65% imbalance) causes pro player boycott & suppresses sales
            if (st.metaImbalancePct >= 65f && g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 && g.sellsPerWeek[0] > 0)
            {
                g.sellsPerWeek[0] = Math.Max(0, (int)(g.sellsPerWeek[0] * 0.72f));
            }
            else if (st.majorsHosted > 0 && g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 && g.sellsPerWeek[0] > 0)
            {
                float esportsBoost = 1f + Mathf.Min(0.45f, st.majorsHosted * 0.12f * (readiness / 80f));
                g.sellsPerWeek[0] = (int)(g.sellsPerWeek[0] * esportsBoost);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class EsportsSellGamePatch
    {
        private static void Postfix(gameScript __instance)
        {
            try
            {
                var mainGo = GameObject.Find("Main");
                var ms = mainGo != null ? mainGo.GetComponent<mainScript>() : null;
                EsportsEngine.WeeklyGameTick(__instance, ms);
            }
            catch (Exception e) { EsportsEcosystemPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class EsportsSavePatch
    {
        private static void Postfix(int i)
        {
            try
            {
                var container = new EsportsSaveContainer { items = new List<GameEsportsState>(EsportsEngine.States.Values) };
                string dir = Path.Combine(Application.persistentDataPath, "MGT2Esports");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, $"slot_{i}.json"), JsonUtility.ToJson(container, true));
            }
            catch (Exception e) { EsportsEcosystemPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class EsportsLoadPatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "MGT2Esports", $"slot_{i}.json");
                if (!File.Exists(path)) return;
                var container = JsonUtility.FromJson<EsportsSaveContainer>(File.ReadAllText(path));
                EsportsEngine.States.Clear();
                if (container?.items != null)
                {
                    foreach (var s in container.items)
                        EsportsEngine.States[s.gameId] = s;
                }
            }
            catch (Exception e) { EsportsEcosystemPlugin.Log.LogError(e); }
        }
    }
}
