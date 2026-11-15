using System.Linq;
using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.crunchunions", "MGT2 Crunch, Burnout & Unions", "1.0.0")]
    public class CrunchUnionsPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.crunchunions").PatchAll(typeof(CrunchUnionsPlugin).Assembly);
            CrunchWindow.Init();
            Log.LogInfo("MGT2 Crunch, Burnout & Unions loaded (F12 or Ctrl+1).");
        }
    }

    internal static class CrunchEngine
    {
        public static bool MandatoryCrunchActive = false;
        public static float BurnoutIndex = 12f;        // [0..100]
        public static float UnionDriveMeter = 15f;     // [0..100]
        public static bool CbaSigned = false;          // Collective Bargaining Agreement
        public static bool GoldenHandcuffsActive = false;
        public static int WellnessTier = 0;            // 0: None, 1: Clinic & Catering, 2: 4-Day Flexible + Sabbatical
        public static int StrikeWeeksRemaining = 0;
        public static int TotalLeaksPrevented = 0;
        public static int TotalPoachAttemptsBlocked = 0;
        private static int _lastProcessedWeek = -1;

        public static float GetDevSpeedMultiplier()
        {
            if (StrikeWeeksRemaining > 0)
                return 0.30f; // 70% studio paralysis during active strike
            if (MandatoryCrunchActive)
                return 1.50f; // +50% crunch output boost
            if (CbaSigned)
                return 1.12f; // High morale steady productivity under CBA
            return 1.0f + WellnessTier * 0.05f;
        }

        public static float GetLeakRiskPercent()
        {
            if (BurnoutIndex <= 55f) return 0f;
            float norm = (BurnoutIndex - 55f) / 45f;
            return Mathf.Clamp(norm * norm * 18f, 0f, 25f);
        }

        public static void ToggleCrunchMode(mainScript mS)
        {
            if (CbaSigned && !MandatoryCrunchActive)
            {
                // Under CBA, crunch requires immediate overtime payout ($65,000)
                if (mS != null && mS.money >= 65000L)
                    mS.Pay(65000L, 1);
                else
                    return;
            }
            MandatoryCrunchActive = !MandatoryCrunchActive;
        }

        public static void UpgradeWellness(mainScript mS)
        {
            if (mS == null || WellnessTier >= 2) return;
            long cost = WellnessTier == 0 ? 120000L : 280000L;
            if (mS.money < cost) return;
            mS.Pay(cost, 1);
            WellnessTier++;
            BurnoutIndex = Mathf.Max(0f, BurnoutIndex - 18f);
            UnionDriveMeter = Mathf.Max(0f, UnionDriveMeter - 12f);
        }

        public static void GrantPaidSabbatical(mainScript mS)
        {
            if (mS == null) return;
            long cost = 95000L;
            if (mS.money < cost) return;
            mS.Pay(cost, 1);
            MandatoryCrunchActive = false;
            StrikeWeeksRemaining = 0;
            BurnoutIndex = Mathf.Max(5f, BurnoutIndex - 35f);
            UnionDriveMeter = Mathf.Max(10f, UnionDriveMeter - 25f);
        }

        public static void SignCollectiveBargainingAgreement(mainScript mS)
        {
            if (mS == null || CbaSigned) return;
            long signBonus = 150000L;
            if (mS.money < signBonus) return;
            mS.Pay(signBonus, 1);
            CbaSigned = true;
            MandatoryCrunchActive = false;
            StrikeWeeksRemaining = 0;
            BurnoutIndex = Mathf.Max(10f, BurnoutIndex - 28f);
            UnionDriveMeter = 0f;
            mS.AddFans(3500, -1);
        }

        public static void ToggleGoldenHandcuffs(mainScript mS)
        {
            if (!GoldenHandcuffsActive)
            {
                if (mS == null || mS.money < 110000L) return;
                mS.Pay(110000L, 1);
                GoldenHandcuffsActive = true;
            }
            else
            {
                GoldenHandcuffsActive = false;
            }
        }

        public static void EvaluateWeeklyStudioClimate(mainScript mS)
        {
            if (mS == null) return;
            int weekToken = mS.year * 60 + mS.month * 5 + mS.week;
            if (weekToken == _lastProcessedWeek) return;
            _lastProcessedWeek = weekToken;

            if (StrikeWeeksRemaining > 0)
            {
                StrikeWeeksRemaining--;
                BurnoutIndex = Mathf.Max(15f, BurnoutIndex - 8f);
            }

            if (MandatoryCrunchActive)
            {
                float deltaB = 6.5f - 1.5f * WellnessTier;
                BurnoutIndex = Mathf.Clamp(BurnoutIndex + deltaB, 0f, 100f);
                if (!CbaSigned)
                    UnionDriveMeter = Mathf.Clamp(UnionDriveMeter + 4.8f, 0f, 100f);
            }
            else
            {
                float recovery = 4.0f + 2.2f * WellnessTier + (CbaSigned ? 2.5f : 0f);
                BurnoutIndex = Mathf.Clamp(BurnoutIndex - recovery, 0f, 100f);
                if (BurnoutIndex < 35f && !CbaSigned)
                    UnionDriveMeter = Mathf.Clamp(UnionDriveMeter - 1.5f, 0f, 100f);
            }

            // Trigger union strike if union drive hits threshold and CBA not signed
            if (!CbaSigned && UnionDriveMeter >= 85f && StrikeWeeksRemaining == 0)
            {
                StrikeWeeksRemaining = 3;
                MandatoryCrunchActive = false;
                UnionDriveMeter = 55f;
            }

            // Rival Poaching check when Burnout > 50
            if (BurnoutIndex > 50f)
            {
                if (GoldenHandcuffsActive)
                {
                    TotalPoachAttemptsBlocked++;
                }
                else if (UnityEngine.Random.Range(0f, 100f) < (BurnoutIndex - 45f) * 0.35f)
                {
                    // Emergency retention counter-offer cost
                    mS.Pay(25000L, 1);
                }
            }

            // Apply active development speed multiplier & bug injection on in-development games
            var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
            float speedMult = GetDevSpeedMultiplier();
            foreach (var g in games)
            {
                if (g == null || !g.inDevelopment || g.isOnMarket) continue;
                if (Math.Abs(speedMult - 1.0f) > 0.01f)
                {
                    float deltaFactor = (speedMult - 1.0f) * 0.045f;
                    g.points_gameplay = Mathf.Max(0f, g.points_gameplay * (1f + deltaFactor));
                    g.points_grafik = Mathf.Max(0f, g.points_grafik * (1f + deltaFactor));
                    g.points_sound = Mathf.Max(0f, g.points_sound * (1f + deltaFactor));
                    g.points_technik = Mathf.Max(0f, g.points_technik * (1f + deltaFactor));
                }

                // High burnout injects extra bugs or triggers leak scandal
                if (BurnoutIndex > 60f)
                {
                    float extraBugs = ((BurnoutIndex - 60f) / 10f) * 1.8f;
                    g.points_bugs += extraBugs;
                }
            }
        }
    }

    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class CrunchTickPatch
    {
        [HarmonyPostfix]
        private static void Postfix(mainScript __instance)
        {
            try { CrunchEngine.EvaluateWeeklyStudioClimate(__instance); }
            catch (Exception e) { CrunchUnionsPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_MitarbeiterUebersicht), "Init")]
    internal static class Patch_Menu_Staff_CrunchUnions
    {
        private const string ButtonName = "Btn_CrunchUnions";

        private static void Postfix(Menu_MitarbeiterUebersicht __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Search") || b.name.Contains("Close"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(-(src.rect.width * 2f + 20f), 0f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => CrunchWindow.Toggle());
                }

                var clr = new Color(0.85f, 0.35f, 0.25f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                string labelAr = "ﺕﺎﺑﺎﻘﻨﻟﺍﻭ ﺼﻧﺮﻜﻟﺍ ﺓﺮﺎﺼﻭ"; // ضغط العمل والنقابات
                foreach (var text in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    text.text = ar ? labelAr : "Crunch & Unions";
            }
            catch (Exception e) { CrunchUnionsPlugin.Log.LogError(e); }
        }
    }

}