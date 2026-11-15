using System.Linq;
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.saleswishlists", "MGT2 Dynamic Sales & Wishlists", "1.0.0")]
    public class SalesWishlistsPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.saleswishlists").PatchAll(typeof(SalesWishlistsPlugin).Assembly);
            SalesWishlistWindow.Init();
            Log.LogInfo("MGT2 Dynamic Sales & Wishlists loaded (End or Ctrl+3).");
        }
    }

    internal static class SalesWishlistEngine
    {
        public static readonly Dictionary<int, int> GameWishlists = new Dictionary<int, int>();
        public static float BrandPriceIntegrity = 100f; // [40..100] drops if discounting too early
        public static int ActiveFranchiseBundles = 0;
        public static long TotalWishlistRevenue = 0L;
        private static int _lastProcessedWeek = -1;

        public static bool IsSeasonalFestivalMonth(int month)
        {
            // Month 6/7 (Summer Sale), Month 11 (Black Friday), Month 12 (Winter Holiday Sale)
            return month == 6 || month == 7 || month == 11 || month == 12;
        }

        public static string GetFestivalLabel(int month)
        {
            if (month == 6 || month == 7) return "SUMMER MEGA SALE (1.65x Wishlist Conversion)";
            if (month == 11) return "BLACK FRIDAY FESTIVAL (1.65x Wishlist Conversion)";
            if (month == 12) return "WINTER HOLIDAY SALE (1.65x Wishlist Conversion)";
            return "Standard Store Week (1.00x Conversion)";
        }

        public static int GetTotalStudioWishlists()
        {
            int sum = 0;
            foreach (var kv in GameWishlists)
                sum += kv.Value;
            return sum;
        }

        public static void TriggerTacticalDiscount(mainScript mS, gameScript g, float discountFraction)
        {
            if (mS == null || g == null) return;
            int pool = GameWishlists.TryGetValue(g.myID, out int w) ? w : 18000;
            float festMult = IsSeasonalFestivalMonth(mS.month) ? 1.65f : 1.0f;

            // Conversion curve: 20% discount triggers store email notification
            float convRate = Mathf.Clamp(0.18f * Mathf.Pow(discountFraction / 0.20f, 1.35f) * festMult, 0.05f, 0.85f);
            int convertedBuyers = Mathf.RoundToInt(pool * convRate);
            GameWishlists[g.myID] = Mathf.Max(1500, pool - convertedBuyers);

            float effectiveUnitNet = Mathf.Max(8f, 32f * (1f - discountFraction)) * (BrandPriceIntegrity / 100f);
            long revenue = (long)(convertedBuyers * effectiveUnitNet);

            mS.Earn(revenue, 1);
            g.sellsTotal += convertedBuyers;
            TotalWishlistRevenue += revenue;

            // Early Discount Penalty if game is <= 4 weeks on market and discount >= 35%
            if (g.weeksOnMarket <= 4 && discountFraction >= 0.35f)
            {
                BrandPriceIntegrity = Mathf.Max(45f, BrandPriceIntegrity - 15f);
            }
        }

        public static void CreateFranchiseBundle(mainScript mS)
        {
            if (mS == null) return;
            long marketingCost = 55000L;
            if (mS.money < marketingCost) return;
            mS.Pay(marketingCost, 1);
            ActiveFranchiseBundles++;
            BrandPriceIntegrity = Mathf.Min(100f, BrandPriceIntegrity + 4f);
        }

        public static void EvaluateWeeklyWishlists(mainScript mS)
        {
            if (mS == null) return;
            int weekToken = mS.year * 60 + mS.month * 5 + mS.week;
            if (weekToken == _lastProcessedWeek) return;
            _lastProcessedWeek = weekToken;

            // Gradual recovery of BrandPriceIntegrity
            if (BrandPriceIntegrity < 100f)
                BrandPriceIntegrity = Mathf.Min(100f, BrandPriceIntegrity + 0.8f);

            var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
            foreach (var g in games)
            {
                if (g == null) continue;
                if (!GameWishlists.ContainsKey(g.myID))
                    GameWishlists[g.myID] = 12000;

                if (g.inDevelopment || g.isOnMarket)
                {
                    int hypeGain = Mathf.RoundToInt(Mathf.Max(10f, g.hype) * 280f + 1400f);
                    GameWishlists[g.myID] += hypeGain;
                }
            }

            if (ActiveFranchiseBundles > 0)
            {
                long bundlePassive = ActiveFranchiseBundles * 16500L;
                mS.Earn(bundlePassive, 1);
                TotalWishlistRevenue += bundlePassive;
            }
        }
    }

    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class SalesWishlistsTickPatch
    {
        [HarmonyPostfix]
        private static void Postfix(mainScript __instance)
        {
            try { SalesWishlistEngine.EvaluateWeeklyWishlists(__instance); }
            catch (Exception e) { SalesWishlistsPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_Marketing_Main), "Init")]
    internal static class Patch_Menu_Marketing_SalesWishlists
    {
        private const string ButtonName = "Btn_SalesWishlists";

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
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, 96f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => SalesWishlistWindow.Toggle());
                }

                var clr = new Color(0.95f, 0.45f, 0.2f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                string labelAr = "ﺕﺎﻴﻨﻤﻷﺍﻭ ﺕﺎﻋﻴﻔﺧﺘﻟﺍ ﺕﺎﻧﺎﺟﺮﻫﻣ"; // مهرجانات التخفيضات والأمنيات
                foreach (var txt in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    txt.text = ar ? labelAr : "Sales Festivals & Wishlists";
            }
            catch (Exception e) { SalesWishlistsPlugin.Log.LogError(e); }
        }
    }

}