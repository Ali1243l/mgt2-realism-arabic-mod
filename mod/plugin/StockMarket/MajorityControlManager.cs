using System;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Implements GDD Point 2 mechanics:
    /// - 25% Stake: Secret Project Intel (view games currently in development)
    /// - 51% Stake: Majority Control Directives (mandate game genre, mandate exclusive publishing)
    /// - 100% Stake: Full Acquisition (absorb as official game subsidiary via SetAsTochterfirma)
    /// - Hostile Takeover Defense: Poison Pill dilution when player approaches 51%
    /// </summary>
    public static class MajorityControlManager
    {
        public static string GetSecretProjectIntel(publisherScript pub, mainScript ms)
        {
            if (pub == null) return StockMarketLabels.NoActiveProject;

            var inDev = pub.FindGameInDevelopment();
            if (inDev != null)
            {
                string genreName = ms != null && ms.genres_ != null ? ms.genres_.GetName(inDev.maingenre) : inDev.maingenre.ToString();
                int progress = Mathf.RoundToInt(pub.GetEntwicklungsFortschritt() * 100f);
                return $"<b>{inDev.GetNameWithTag()}</b> ({genreName}) - {progress}% in dev, Hype: {Mathf.RoundToInt(inDev.hype)}";
            }

            return StockMarketLabels.NoActiveProject;
        }

        public static void SetExclusivePublishingMandate(int companyId, bool mandate)
        {
            var stock = StockData.GetOrCreate(companyId);
            if (stock.playerShare >= 51f)
            {
                stock.mandateExclusivePublishing = mandate;
            }
        }

        public static void SetGenreMandate(int companyId, int genreId)
        {
            var stock = StockData.GetOrCreate(companyId);
            if (stock.playerShare >= 51f)
            {
                stock.mandateGenre = genreId;
            }
        }

        public static bool AbsorbAsSubsidiary(mainScript ms, publisherScript pub, GUI_Main guiMain)
        {
            if (pub == null || ms == null) return false;

            var stock = StockData.GetOrCreate(pub.myID);
            if (stock.playerShare < 100f) return false;

            // Convert to official game subsidiary
            pub.SetAsTochterfirma(ms.myID);
            stock.absorbedAsSubsidiary = true;

            try
            {
                var sfx = GameObject.Find("SFX")?.GetComponent<sfxScript>();
                sfx?.PlaySound(6, force: true);
            }
            catch { }

            if (guiMain != null)
            {
                string msg = string.Format(StockMarketLabels.AbsorbSuccess, pub.GetName());
                guiMain.MessageBox(msg, false);
            }

            StockMarketPlugin.Log.LogInfo($"Successfully absorbed {pub.GetName()} as player subsidiary!");
            return true;
        }
    }

    /// <summary>
    /// Hooks game creation by AI publishers to enforce majority control directives.
    /// </summary>
    [HarmonyPatch(typeof(publisherScript), nameof(publisherScript.CreateNewGame2))]
    internal static class MajorityControlGameCreationPatch
    {
        static void Postfix(publisherScript __instance, ref gameScript __result)
        {
            try
            {
                if (__instance == null || __result == null || __instance.isPlayer) return;

                var stock = StockData.GetOrCreate(__instance.myID);
                if (stock.playerShare < 51f) return;

                // Enforce mandated genre
                if (stock.mandateGenre >= 0)
                {
                    __result.maingenre = stock.mandateGenre;
                }

                // Enforce exclusive publishing through player
                if (stock.mandateExclusivePublishing)
                {
                    var main = GameObject.Find("Main");
                    var ms = main != null ? main.GetComponent<mainScript>() : null;
                    if (ms != null)
                    {
                        __result.publisherID = ms.myID;
                        ms.guiMain_?.CreateTopNewsInfo($"{__instance.GetName()}: {__result.GetNameWithTag()} (Player Publishing Mandate)");
                    }
                }
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError("Failed in MajorityControlGameCreationPatch: " + ex);
            }
        }
    }

    /// <summary>
    /// Hooks weekly tick to handle Poison Pill defenses when creeping takeover is attempted.
    /// </summary>
    [HarmonyPatch(typeof(mainScript), nameof(mainScript.WochenUpdates))]
    internal static class HostileTakeoverWeeklyPatch
    {
        static void Postfix(mainScript __instance)
        {
            try
            {
                if (__instance == null || __instance.multiplayer) return;

                int currentWeekNumber = __instance.year * 48 + __instance.month * 4 + __instance.week;

                var pubs = GameObject.FindGameObjectsWithTag("Publisher");
                for (int i = 0; i < pubs.Length; i++)
                {
                    if (!pubs[i]) continue;
                    var pub = pubs[i].GetComponent<publisherScript>();
                    if (pub == null || !pub.isUnlocked || pub.isPlayer) continue;

                    var stock = StockData.GetOrCreate(pub.myID);

                    // Check Poison Pill Expiry
                    if (stock.poisonPillActive && currentWeekNumber >= stock.poisonPillExpiry)
                    {
                        stock.poisonPillActive = false;
                        StockMarketPlugin.Log.LogInfo($"Poison pill for {pub.GetName()} expired.");
                    }

                    // Creeping hostile takeover triggers Poison Pill defense (40% - 50.9%)
                    if (stock.playerShare >= 40f && stock.playerShare < 51f && !stock.poisonPillActive)
                    {
                        // 15% weekly chance AI board activates defense
                        if (UnityEngine.Random.Range(0, 100) < 15)
                        {
                            stock.poisonPillActive = true;
                            stock.poisonPillExpiry = currentWeekNumber + 16; // Active for 16 weeks
                            float dilution = UnityEngine.Random.Range(5f, 8f);
                            stock.playerShare = Mathf.Max(30f, stock.playerShare - dilution);

                            string alert = string.Format(StockMarketLabels.PoisonPillAlert, pub.GetName(), dilution.ToString("F1"));
                            __instance.guiMain_?.MessageBox(alert, false);

                            StockMarketPlugin.Log.LogInfo($"Hostile takeover defense triggered by {pub.GetName()}: diluted player share by {dilution}%");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError("Failed in HostileTakeoverWeeklyPatch: " + ex);
            }
        }
    }
}
