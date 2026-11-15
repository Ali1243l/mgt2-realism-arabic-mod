using System.Linq;
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.drmanticheat", "MGT2 DRM, Piracy & Anti-Cheat", "1.0.0")]
    public class DrmAntiCheatPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.drmanticheat").PatchAll(typeof(DrmAntiCheatPlugin).Assembly);
            DrmWindow.Init();
            Log.LogInfo("MGT2 DRM, Piracy & Anti-Cheat loaded (Tilde ~ or Ctrl+2).");
        }
    }

    internal enum DrmTier
    {
        DrmFreeGogStyle = 0,
        StandardWrapper = 1,
        FortressDenuvoVm = 2
    }

    internal enum AntiCheatTier
    {
        ServerHeuristics = 0,
        UserModeGuard = 1,
        KernelRingZero = 2
    }

    internal static class DrmEngine
    {
        public static DrmTier ActiveDrmPolicy = DrmTier.StandardWrapper;
        public static AntiCheatTier ActiveAntiCheat = AntiCheatTier.UserModeGuard;
        public static float CheaterInfestationRate = 8.0f; // [0..100]
        public static int TotalPiracySavedDollars = 0;
        public static int TotalCheatersBanned = 0;
        public static readonly HashSet<int> DrmRemovedAfterWindowGames = new HashSet<int>();
        private static int _lastProcessedWeek = -1;

        public static string GetDrmName(DrmTier tier)
        {
            switch (tier)
            {
                case DrmTier.DrmFreeGogStyle: return "DRM-Free (GOG Philosophy: +Goodwill, Higher Day-1 Piracy)";
                case DrmTier.FortressDenuvoVm: return "Fortress Denuvo-VM ($180k: 96% Piracy Shield, -FPS Backlash)";
                default: return "Standard Store Wrapper ($35k: Balanced Protection)";
            }
        }

        public static string GetAntiCheatName(AntiCheatTier tier)
        {
            switch (tier)
            {
                case AntiCheatTier.ServerHeuristics: return "Basic Server Heuristics (Low Protection)";
                case AntiCheatTier.KernelRingZero: return "Ring-0 Kernel Driver ($140k: 98% Cheat Block, Privacy Debate)";
                default: return "User-Mode Memory Guard ($50k: Standard Online Shield)";
            }
        }

        public static void CycleDrmPolicy(mainScript mS)
        {
            int next = ((int)ActiveDrmPolicy + 1) % 3;
            ActiveDrmPolicy = (DrmTier)next;
            if (ActiveDrmPolicy == DrmTier.FortressDenuvoVm && mS != null && mS.money >= 180000L)
                mS.Pay(180000L, 1);
            else if (ActiveDrmPolicy == DrmTier.StandardWrapper && mS != null && mS.money >= 35000L)
                mS.Pay(35000L, 1);
        }

        public static void CycleAntiCheatTier(mainScript mS)
        {
            int next = ((int)ActiveAntiCheat + 1) % 3;
            ActiveAntiCheat = (AntiCheatTier)next;
            if (ActiveAntiCheat == AntiCheatTier.KernelRingZero && mS != null && mS.money >= 140000L)
            {
                mS.Pay(140000L, 1);
                CheaterInfestationRate = Mathf.Max(2f, CheaterInfestationRate * 0.25f);
            }
        }

        public static void ExecuteGlobalBanWave(mainScript mS)
        {
            if (mS == null) return;
            long cost = 45000L;
            if (mS.money < cost) return;
            mS.Pay(cost, 1);

            int bannedNow = Mathf.RoundToInt(CheaterInfestationRate * 1850f + 1200f);
            TotalCheatersBanned += bannedNow;
            CheaterInfestationRate = Mathf.Max(1.5f, CheaterInfestationRate * 0.20f);
            mS.AddFans(1800, -1);
        }

        public static void PatchOutDrmFromPostWindowGames(mainScript mS)
        {
            if (mS == null) return;
            var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
            int patchedCount = 0;
            foreach (var g in games)
            {
                if (g == null || !g.isOnMarket || g.weeksOnMarket < 6) continue;
                if (DrmRemovedAfterWindowGames.Contains(g.myID)) continue;
                DrmRemovedAfterWindowGames.Add(g.myID);
                g.reviewTotal = Mathf.Clamp(g.reviewTotal + 2, 1, 100);
                patchedCount++;
            }
            if (patchedCount > 0)
            {
                mS.AddFans(patchedCount * 1400, -1);
            }
        }

        public static void EvaluateWeeklyProtection(mainScript mS)
        {
            if (mS == null) return;
            int weekToken = mS.year * 60 + mS.month * 5 + mS.week;
            if (weekToken == _lastProcessedWeek) return;
            _lastProcessedWeek = weekToken;

            // Update Cheater Infestation drift
            float drift = ActiveAntiCheat == AntiCheatTier.KernelRingZero
                ? -1.2f
                : (ActiveAntiCheat == AntiCheatTier.UserModeGuard ? 1.4f : 4.2f);
            CheaterInfestationRate = Mathf.Clamp(CheaterInfestationRate + drift, 1.5f, 95f);

            var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
            foreach (var g in games)
            {
                if (g == null || !g.isOnMarket) continue;

                // Launch window (first 8 weeks): DRM policy shapes sales retention vs piracy leakage
                if (g.weeksOnMarket <= 8)
                {
                    if (ActiveDrmPolicy == DrmTier.FortressDenuvoVm && !DrmRemovedAfterWindowGames.Contains(g.myID))
                    {
                        int protectedBonus = Mathf.Max(1200, (int)((g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 ? g.sellsPerWeek[0] : 100) * 4L));
                        mS.Earn(protectedBonus, 1);
                        TotalPiracySavedDollars += protectedBonus;
                    }
                    else if (ActiveDrmPolicy == DrmTier.DrmFreeGogStyle)
                    {
                        // Small piracy leak on launch, but steady fan loyalty
                        mS.AddFans(85, -1);
                    }
                }
                else if (DrmRemovedAfterWindowGames.Contains(g.myID))
                {
                    // Post-window DRM removal gives evergreen goodwill sales boost
                    int goodwillBonus = Mathf.Max(600, (int)((g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 ? g.sellsPerWeek[0] : 100) * 2L));
                    mS.Earn(goodwillBonus, 1);
                }
            }
        }
    }

    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class DrmWeeklyTickPatch
    {
        [HarmonyPostfix]
        private static void Postfix(mainScript __instance)
        {
            try { DrmEngine.EvaluateWeeklyProtection(__instance); }
            catch (Exception e) { DrmAntiCheatPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_BuyCopyProtect), "Init")]
    internal static class Patch_Menu_CopyProtect_Drm
    {
        private const string ButtonName = "Btn_DrmAdvanced";

        private static void Postfix(Menu_BuyCopyProtect __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Close") || b.name.Contains("Abbrechen"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(-(src.rect.width + 12f), 0f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => DrmWindow.Toggle());
                }

                var clr = new Color(0.2f, 0.55f, 0.85f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                string labelAr = "Denuvo ﺔﻴﺎﻤﺣﻭ DRM"; // DRM وحماية Denuvo
                foreach (var txt in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    txt.text = ar ? labelAr : "Denuvo DRM & Security";
            }
            catch (Exception e) { DrmAntiCheatPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_BuyAntiCheat), "Init")]
    internal static class Patch_Menu_AntiCheat_Drm
    {
        private const string ButtonName = "Btn_AntiCheatAdvanced";

        private static void Postfix(Menu_BuyAntiCheat __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Close") || b.name.Contains("Abbrechen"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(-(src.rect.width + 12f), 0f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => DrmWindow.Toggle());
                }

                var clr = new Color(0.75f, 0.25f, 0.35f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                string labelAr = "Kernel ﺵﺴﻟﺍ ﺔﺪﻔﺎﻜﻣ"; // مكافحة الغش Kernel
                foreach (var txt in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    txt.text = ar ? labelAr : "Kernel Anti-Cheat";
            }
            catch (Exception e) { DrmAntiCheatPlugin.Log.LogError(e); }
        }
    }

}