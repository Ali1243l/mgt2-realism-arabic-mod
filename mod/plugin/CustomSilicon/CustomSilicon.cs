// GDD Point 6: Custom Silicon R&D Lab, Wafer Foundry Yield, Discrete PC GPUs & Thermal Cooling Engineering.
// Built for MGT2Mod namespace, netstandard2.1, 32-bit Mono x86 safe (no Harmony patches on Int64-returning methods).
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    public enum LithographyNode
    {
        Node90nm = 0,
        Node45nm = 1,
        Node14nmFinFET = 2,
        Node7nmEUV = 3,
        Node3nmGAA = 4,
        Node1nmQuantum = 5
    }

    public enum FoundryPartner
    {
        BudgetFoundry = 0,   // Lower wafer cost, higher defect density D0 (lower yield)
        Tier1Foundry = 1     // Premium wafer cost (TSMC-class), ultra-low defect density D0
    }

    public enum CoolingSolution
    {
        BudgetAir = 0,       // Cheap ($4/unit), high thermal resistance R_theta = 0.42 C/W (RRoD risk!)
        VaporChamber = 1,    // Balanced ($14/unit), R_theta = 0.24 C/W
        LiquidPhase = 2      // Enthusiast ($32/unit), R_theta = 0.13 C/W (zero thermal throttling)
    }

    public enum SupplyChainStatus
    {
        Stable = 0,
        WaferShortage = 1,
        CryptoAiBoom = 2
    }

    [Serializable]
    public class DiscreteGpuProduct
    {
        public string modelName = "Archon GTX-100";
        public LithographyNode node = LithographyNode.Node45nm;
        public float dieAreaMm2 = 240f;
        public float tdpWatts = 165f;
        public float retailPrice = 399f;
        public float perfScore = 100f;
        public int weeksOnMarket = 0;
        public long totalUnitsSold = 0L;
        public long cumulativeNetProfit = 0L;
    }

    [Serializable]
    public class SiliconSaveData
    {
        public LithographyNode currentNode = LithographyNode.Node90nm;
        public FoundryPartner foundry = FoundryPartner.BudgetFoundry;
        public CoolingSolution cooling = CoolingSolution.BudgetAir;
        public SupplyChainStatus supplyStatus = SupplyChainStatus.Stable;
        public float hardwareBrandRep = 35f; // 0 .. 100
        public float dieAreaMm2 = 220f;
        public float targetTdpWatts = 160f;
        public float fieldFailureRatePct = 1.2f;
        public bool rrodScandalActive = false;
        public int lastProcessedWeek = -1;
        public List<DiscreteGpuProduct> gpuProducts = new List<DiscreteGpuProduct>();
    }

    [BepInPlugin("ali.mgt2.customsilicon", "Custom Silicon & PC Hardware Lab", "1.0.0")]
    public class CustomSiliconPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.customsilicon").PatchAll(typeof(CustomSiliconPlugin).Assembly);
            Log.LogInfo("Custom Silicon & PC Hardware Lab loaded (F10)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                SiliconWindow.Toggle();
        }
    }

    public static class SiliconLabEngine
    {
        public static SiliconSaveData State = new SiliconSaveData();

        /// <summary>
        /// Negative-Binomial Wafer Yield Model (Murphy / Bose-Einstein):
        /// Y = (1 + (D0 * A_cm2) / alpha)^(-alpha)
        /// Returns yield percentage in [5.0, 99.0].
        /// </summary>
        public static float CalculateWaferYieldPct(LithographyNode node, FoundryPartner foundry, float dieAreaMm2)
        {
            float areaCm2 = Mathf.Max(0.25f, dieAreaMm2 / 100f);
            // Base defect density D0 (defects per cm^2) rises on bleeding-edge nodes, drops with Tier-1 Foundry
            float baseD0 = node == LithographyNode.Node90nm ? 0.045f
                : node == LithographyNode.Node45nm ? 0.060f
                : node == LithographyNode.Node14nmFinFET ? 0.080f
                : node == LithographyNode.Node7nmEUV ? 0.105f
                : node == LithographyNode.Node3nmGAA ? 0.135f : 0.165f;

            float foundryMult = foundry == FoundryPartner.Tier1Foundry ? 0.58f : 1.22f;
            float d0 = baseD0 * foundryMult;
            float alpha = 3.0f; // Cluster parameter
            float yieldFraction = Mathf.Pow(1f + (d0 * areaCm2) / alpha, -alpha);
            return Mathf.Clamp(yieldFraction * 100f, 5f, 99f);
        }

        /// <summary>
        /// Arrhenius-inspired Thermal Junction Temperature & Field Failure Rate (%).
        /// Cheap cooling on high-TDP silicon causes junction temp > 95C and triggers RRoD recalls.
        /// </summary>
        public static float CalculateThermalFailureRatePct(float tdpWatts, CoolingSolution cooling, out float junctionTempC)
        {
            float ambientC = 28f;
            float rTheta = cooling == CoolingSolution.BudgetAir ? 0.42f
                : cooling == CoolingSolution.VaporChamber ? 0.24f : 0.13f;

            junctionTempC = ambientC + Mathf.Max(35f, tdpWatts) * rTheta;
            float excessTemp = Mathf.Max(0f, junctionTempC - 75f);
            // Exponential thermal stress acceleration
            float failurePct = 0.65f * Mathf.Exp(0.085f * excessTemp);
            return Mathf.Clamp(failurePct, 0.4f, 45.0f);
        }

        public static long GetNodeUpgradeCost(LithographyNode current)
        {
            switch (current)
            {
                case LithographyNode.Node90nm: return 2500000L;
                case LithographyNode.Node45nm: return 6500000L;
                case LithographyNode.Node14nmFinFET: return 15000000L;
                case LithographyNode.Node7nmEUV: return 35000000L;
                case LithographyNode.Node3nmGAA: return 75000000L;
                default: return 0L;
            }
        }

        public static float GetNodePerformanceScore(LithographyNode node)
        {
            switch (node)
            {
                case LithographyNode.Node90nm: return 100f;
                case LithographyNode.Node45nm: return 155f;
                case LithographyNode.Node14nmFinFET: return 240f;
                case LithographyNode.Node7nmEUV: return 370f;
                case LithographyNode.Node3nmGAA: return 560f;
                case LithographyNode.Node1nmQuantum: return 880f;
                default: return 100f;
            }
        }

        public static bool UpgradeProcessNode(mainScript ms)
        {
            if (ms == null || State.currentNode >= LithographyNode.Node1nmQuantum) return false;
            long cost = GetNodeUpgradeCost(State.currentNode);
            if (ms.money < cost) return false;

            ms.Pay(cost, 1);
            State.currentNode = (LithographyNode)((int)State.currentNode + 1);
            State.hardwareBrandRep = Mathf.Clamp(State.hardwareBrandRep + 8f, 0f, 100f);
            return true;
        }

        public static bool LaunchDiscreteGpuLine(mainScript ms)
        {
            if (ms == null) return false;
            long tapeOutCost = 1800000L + (long)State.currentNode * 2200000L;
            if (ms.money < tapeOutCost) return false;

            ms.Pay(tapeOutCost, 1);
            int seriesNum = (State.gpuProducts.Count + 1) * 100 + ((int)State.currentNode + 1) * 10;
            float yieldPct = CalculateWaferYieldPct(State.currentNode, State.foundry, State.dieAreaMm2);
            float perf = GetNodePerformanceScore(State.currentNode) * (yieldPct / 75f);

            var gpu = new DiscreteGpuProduct
            {
                modelName = $"Archon RX-{seriesNum}",
                node = State.currentNode,
                dieAreaMm2 = State.dieAreaMm2,
                tdpWatts = State.targetTdpWatts,
                retailPrice = 299f + (int)State.currentNode * 120f,
                perfScore = perf,
                weeksOnMarket = 0
            };
            State.gpuProducts.Insert(0, gpu);
            if (State.gpuProducts.Count > 8)
                State.gpuProducts.RemoveAt(State.gpuProducts.Count - 1);
            return true;
        }

        public static bool ExecuteRecallRepairCampaign(mainScript ms, long cost)
        {
            if (ms == null || ms.money < cost) return false;
            ms.Pay(cost, 1);
            State.rrodScandalActive = false;
            State.cooling = CoolingSolution.VaporChamber;
            State.fieldFailureRatePct = CalculateThermalFailureRatePct(State.targetTdpWatts, State.cooling, out _);
            State.hardwareBrandRep = Mathf.Clamp(State.hardwareBrandRep + 12f, 0f, 100f);
            return true;
        }

        public static void WeeklyTick(mainScript ms)
        {
            if (ms == null) return;

            // 1. Update thermal reliability
            State.fieldFailureRatePct = CalculateThermalFailureRatePct(State.targetTdpWatts, State.cooling, out _);
            if (State.fieldFailureRatePct >= 9.5f && UnityEngine.Random.value < 0.25f)
            {
                State.rrodScandalActive = true;
            }

            if (State.rrodScandalActive)
            {
                // Weekly RMA warranty drain & brand erosion
                long rmaPenalty = 85000L + (long)(State.fieldFailureRatePct * 6000f);
                ms.Pay(rmaPenalty, 1);
                State.hardwareBrandRep = Mathf.Max(5f, State.hardwareBrandRep - 2.2f);
            }

            // 2. Dynamic Supply Chain Markov Transition
            float roll = UnityEngine.Random.value;
            if (roll < 0.06f) State.supplyStatus = SupplyChainStatus.WaferShortage;
            else if (roll < 0.12f) State.supplyStatus = SupplyChainStatus.CryptoAiBoom;
            else if (roll < 0.32f) State.supplyStatus = SupplyChainStatus.Stable;

            float yieldPct = CalculateWaferYieldPct(State.currentNode, State.foundry, State.dieAreaMm2);
            float supplyDemandMult = State.supplyStatus == SupplyChainStatus.CryptoAiBoom ? 2.15f
                : State.supplyStatus == SupplyChainStatus.WaferShortage ? 0.68f : 1.0f;

            // 3. Process Discrete PC GPU Sales
            for (int i = 0; i < State.gpuProducts.Count; i++)
            {
                var gpu = State.gpuProducts[i];
                gpu.weeksOnMarket++;
                if (gpu.weeksOnMarket > 96) continue; // End-of-life after 2 years

                float ageDecay = Mathf.Exp(-0.028f * gpu.weeksOnMarket);
                float repFactor = 0.45f + (State.hardwareBrandRep / 100f) * 1.15f;
                float scandalPenalty = State.rrodScandalActive ? 0.38f : 1.0f;

                long unitsSold = (long)(320f * (gpu.perfScore / 100f) * repFactor * ageDecay * supplyDemandMult * scandalPenalty);
                if (unitsSold <= 0) continue;

                float unitMargin = gpu.retailPrice * (0.18f + (yieldPct / 260f));
                if (State.foundry == FoundryPartner.Tier1Foundry) unitMargin *= 0.90f;
                if (State.supplyStatus == SupplyChainStatus.WaferShortage) unitMargin *= 0.72f;

                long netProfit = (long)(unitsSold * Mathf.Max(15f, unitMargin));
                gpu.totalUnitsSold += unitsSold;
                gpu.cumulativeNetProfit += netProfit;
                ms.Earn(netProfit, 1);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class SiliconWeeklyTickPatch
    {
        private static void Postfix(gameScript __instance)
        {
            try
            {
                var mainGo = GameObject.Find("Main");
                if (mainGo == null) return;
                var ms = mainGo.GetComponent<mainScript>();
                if (ms == null) return;

                int weekStamp = ms.year * 52 + ms.month * 4 + ms.week;
                if (SiliconLabEngine.State.lastProcessedWeek != weekStamp)
                {
                    SiliconLabEngine.State.lastProcessedWeek = weekStamp;
                    SiliconLabEngine.WeeklyTick(ms);
                }
            }
            catch (Exception e) { CustomSiliconPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class SiliconSavePatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "MGT2CustomSilicon");
                Directory.CreateDirectory(dir);
                string json = JsonUtility.ToJson(SiliconLabEngine.State, true);
                File.WriteAllText(Path.Combine(dir, $"slot_{i}.json"), json);
            }
            catch (Exception e) { CustomSiliconPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class SiliconLoadPatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "MGT2CustomSilicon", $"slot_{i}.json");
                if (File.Exists(path))
                {
                    SiliconLabEngine.State = JsonUtility.FromJson<SiliconSaveData>(File.ReadAllText(path)) ?? new SiliconSaveData();
                }
            }
            catch (Exception e) { CustomSiliconPlugin.Log.LogError(e); }
        }
    }
}
