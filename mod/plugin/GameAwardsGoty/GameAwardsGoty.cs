using System.Linq;
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.gameawardsgoty", "MGT2 The Game Awards & GOTY Edition", "1.0.0")]
    public class GameAwardsGotyPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.gameawardsgoty").PatchAll(typeof(GameAwardsGotyPlugin).Assembly);
            GameAwardsWindow.Init();
            Log.LogInfo("MGT2 The Game Awards & GOTY Edition loaded (Insert or Ctrl+5).");
        }
    }

    internal static class GameAwardsEngine
    {
        public static int TotalGotyTrophies = 0;
        public static int TotalCategoryAwards = 0;
        public static bool JuryReviewCodesSent = false;
        public static readonly HashSet<int> GotyEditionReleasedGames = new HashSet<int>();
        public static string LastCeremonySummary = "No December Ceremony held yet this year.";
        private static int _lastCeremonyYear = -1;

        public static void SendForYourConsiderationCampaign(mainScript mS)
        {
            if (mS == null || JuryReviewCodesSent) return;
            long cost = 85000L;
            if (mS.money < cost) return;
            mS.Pay(cost, 1);
            JuryReviewCodesSent = true;
            mS.AddFans(2200, -1);
        }

        public static void HostDecemberAwardsCeremony(mainScript mS)
        {
            if (mS == null) return;
            var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
            gameScript bestGame = null;
            float bestScore = 0f;

            foreach (var g in games)
            {
                if (g == null || !g.isOnMarket) continue;
                float score = g.reviewTotal * 0.65f + Mathf.Min(100f, g.hype) * 0.20f + (JuryReviewCodesSent ? 12f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGame = g;
                }
            }

            if (bestGame == null)
            {
                LastCeremonySummary = "No eligible released games found for this ceremony.";
                return;
            }

            if (bestScore >= 72f)
            {
                TotalGotyTrophies++;
                TotalCategoryAwards += 3;
                long prizeBump = 320000L + (long)(bestGame.sellsTotal * 4L);
                mS.Earn(prizeBump, 1);
                mS.AddFans(8500, -1);
                LastCeremonySummary = $"WINNER! '{bestGame.GetNameSimple()}' won GAME OF THE YEAR (Score: {bestScore:F1}) + USD {prizeBump:N0} Sales Bump!";
            }
            else if (bestScore >= 58f)
            {
                TotalCategoryAwards += 1;
                long catBump = 110000L;
                mS.Earn(catBump, 1);
                mS.AddFans(2800, -1);
                LastCeremonySummary = $"NOMINEE WINNER: '{bestGame.GetNameSimple()}' won Best Audio/Visual Direction (+USD {catBump:N0})!";
            }
            else
            {
                LastCeremonySummary = $"'{bestGame.GetNameSimple()}' (Score: {bestScore:F1}) did not reach the 72.0 GOTY threshold.";
            }

            JuryReviewCodesSent = false;
        }

        public static void ReleaseGotyDeluxeEdition(mainScript mS, gameScript g)
        {
            if (mS == null || g == null) return;
            if (GotyEditionReleasedGames.Contains(g.myID)) return;

            long masteringCost = 65000L;
            if (mS.money < masteringCost) return;
            mS.Pay(masteringCost, 1);

            GotyEditionReleasedGames.Add(g.myID);
            int extraUnits = Mathf.Max(25000, Mathf.RoundToInt(g.sellsTotal * 0.30f));
            long deluxeRevenue = extraUnits * 28L;

            g.sellsTotal += extraUnits;
            g.hype = Mathf.Clamp(g.hype + 25f, 0f, 100f);
            mS.Earn(deluxeRevenue, 1);
            mS.AddFans(5400, -1);
        }

        public static void CheckAnnualDecemberCeremony(mainScript mS)
        {
            if (mS == null) return;
            if (mS.month == 12 && mS.week == 3 && _lastCeremonyYear != mS.year)
            {
                _lastCeremonyYear = mS.year;
                HostDecemberAwardsCeremony(mS);
            }
        }
    }

    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class GameAwardsWeeklyTickPatch
    {
        [HarmonyPostfix]
        private static void Postfix(mainScript __instance)
        {
            try { GameAwardsEngine.CheckAnnualDecemberCeremony(__instance); }
            catch (Exception e) { GameAwardsGotyPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_Awards), "Init")]
    internal static class Patch_Menu_Awards_Goty
    {
        private const string ButtonName = "Btn_GameAwardsGoty";

        private static void Postfix(Menu_Awards __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Abbrechen") || b.name.Contains("Yes"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, 48f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => GameAwardsWindow.Toggle());
                }

                var clr = new Color(0.95f, 0.75f, 0.2f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                string labelAr = "GOTY ﺔﻨﺴﻟﺍ ﺔﺒﻌﻟ ﺕﺍﺟﺍﻮﺟ"; // جوائز لعبة السنة GOTY
                foreach (var txt in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    txt.text = ar ? labelAr : "The Game Awards (GOTY)";
            }
            catch (Exception e) { GameAwardsGotyPlugin.Log.LogError(e); }
        }
    }

}