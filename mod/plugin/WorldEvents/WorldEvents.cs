using System.Linq;
using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.worldevents", "MGT2 Dynamic Real-World Events", "1.0.0")]
    public class WorldEventsPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.worldevents").PatchAll(typeof(WorldEventsPlugin).Assembly);
            WorldEventsWindow.Init();
            Log.LogInfo("MGT2 Dynamic Real-World Events loaded (Home or Ctrl+4).");
        }
    }

    internal enum GlobalEventType
    {
        StableMarket = 0,
        CryptoAiGpuBoom = 1,
        GlobalQuarantineLockdown = 2,
        CyberLeakCrisis = 3,
        ViralMemeCatalogSurge = 4
    }

    internal static class WorldEventsEngine
    {
        public static GlobalEventType CurrentEvent = GlobalEventType.StableMarket;
        public static int EventWeeksRemaining = 0;
        public static bool CyberFirewallUpgraded = false;
        public static bool RemoteWorkInfrastructure = false;
        public static long TotalCrisisBonusRevenue = 0L;
        private static int _lastProcessedWeek = -1;

        public static string GetCurrentEventTitle()
        {
            switch (CurrentEvent)
            {
                case GlobalEventType.CryptoAiGpuBoom:
                    return "AI & Crypto GPU Shortage (PC Hardware 3x Price, Handheld/Cloud +45% Demand)";
                case GlobalEventType.GlobalQuarantineLockdown:
                    return "Global Quarantine Lockdown (Game Sales +300% Surge, Office Speed -25%)";
                case GlobalEventType.CyberLeakCrisis:
                    return "Massive AAA Source-Code Leak Crisis (Cybersecurity Alert)";
                case GlobalEventType.ViralMemeCatalogSurge:
                    return "Viral Internet Meme Surge (Back-Catalog Game Exploding in Sales!)";
                default:
                    return "Global Industry Stable (Normal Hardware Supply & Consumer Demand)";
            }
        }

        public static float GetGlobalSalesMultiplier()
        {
            switch (CurrentEvent)
            {
                case GlobalEventType.GlobalQuarantineLockdown:
                    return 3.00f; // +200% surge (300% of baseline)
                case GlobalEventType.ViralMemeCatalogSurge:
                    return 2.25f;
                case GlobalEventType.CryptoAiGpuBoom:
                    return 1.35f;
                default:
                    return 1.00f;
            }
        }

        public static void UpgradeCyberFirewall(mainScript mS)
        {
            if (mS == null || CyberFirewallUpgraded) return;
            long cost = 135000L;
            if (mS.money < cost) return;
            mS.Pay(cost, 1);
            CyberFirewallUpgraded = true;
            if (CurrentEvent == GlobalEventType.CyberLeakCrisis)
            {
                CurrentEvent = GlobalEventType.StableMarket;
                EventWeeksRemaining = 0;
            }
        }

        public static void UpgradeRemoteWorkInfra(mainScript mS)
        {
            if (mS == null || RemoteWorkInfrastructure) return;
            long cost = 90000L;
            if (mS.money < cost) return;
            mS.Pay(cost, 1);
            RemoteWorkInfrastructure = true;
        }

        public static void TriggerViralMemeCampaign(mainScript mS)
        {
            if (mS == null) return;
            CurrentEvent = GlobalEventType.ViralMemeCatalogSurge;
            EventWeeksRemaining = 8;
            mS.AddFans(4200, -1);
        }

        public static void CycleSimulatedWorldEvent()
        {
            int next = ((int)CurrentEvent + 1) % 5;
            CurrentEvent = (GlobalEventType)next;
            EventWeeksRemaining = CurrentEvent == GlobalEventType.StableMarket ? 0 : 10;
        }

        public static void EvaluateWeeklyWorldEvent(mainScript mS)
        {
            if (mS == null) return;
            int weekToken = mS.year * 60 + mS.month * 5 + mS.week;
            if (weekToken == _lastProcessedWeek) return;
            _lastProcessedWeek = weekToken;

            if (EventWeeksRemaining > 0)
            {
                EventWeeksRemaining--;
                if (EventWeeksRemaining == 0)
                    CurrentEvent = GlobalEventType.StableMarket;
            }
            else if (UnityEngine.Random.Range(0, 100) < 6)
            {
                int pick = UnityEngine.Random.Range(1, 5);
                if (pick == (int)GlobalEventType.CyberLeakCrisis && CyberFirewallUpgraded)
                    pick = (int)GlobalEventType.ViralMemeCatalogSurge;
                CurrentEvent = (GlobalEventType)pick;
                EventWeeksRemaining = 10;
            }

            float mult = GetGlobalSalesMultiplier();
            if (mult > 1.01f)
            {
                var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
                foreach (var g in games)
                {
                    if (g == null || !g.isOnMarket) continue;
                    long weeklyBonus = (long)(Mathf.Max(50, (g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 ? g.sellsPerWeek[0] : 50)) * (mult - 1.0f) * 14f);
                    if (weeklyBonus > 0)
                    {
                        mS.Earn(weeklyBonus, 1);
                        TotalCrisisBonusRevenue += weeklyBonus;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class WorldEventsWeeklyTickPatch
    {
        [HarmonyPostfix]
        private static void Postfix(mainScript __instance)
        {
            try { WorldEventsEngine.EvaluateWeeklyWorldEvent(__instance); }
            catch (Exception e) { WorldEventsPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_Marketing_Main), "Init")]
    internal static class Patch_Menu_Marketing_WorldEvents
    {
        private const string ButtonName = "Btn_WorldEvents_Marketing";

        private static void Postfix(Menu_Marketing_Main __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Game") || b.name.Contains("Abbrechen") || b.name.Contains("Konsole"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, 144f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => WorldEventsWindow.Toggle());
                }

                var clr = new Color(0.35f, 0.45f, 0.9f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                string labelAr = "ﺔﻴﻤﻼﻌﻟﺍ ﺕﺎﻣﺯﺗﻼﺍﻭ ﺏﺍﺪﺣﻷﺍ"; // الأحداث والأزمات العالمية
                foreach (var txt in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    txt.text = ar ? labelAr : "Global Industry Events";
            }
            catch (Exception e) { WorldEventsPlugin.Log.LogError(e); }
        }
    }

}