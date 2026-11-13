using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod.PublishingDeals
{
    [BepInPlugin("ali.mgt2.publishingdeals", "Publishing Deals & Exclusivity", "1.0.0")]
    public class PublishingDealsPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.publishingdeals").PatchAll(typeof(PublishingDealsPlugin).Assembly);
            Log.LogInfo("Publishing Deals & Exclusivity plugin loaded");
        }
    }

    public static class PublishingManager
    {
        public static PublishingDealsSave Data = new PublishingDealsSave();
        private static int _nextId = 1000;

        public static void GeneratePitchesIfNeeded(mainScript ms)
        {
            if (ms == null || ms.multiplayer) return;

            int currentWeek = ms.year * 48 + ms.month * 4 + ms.week;
            if (Data.lastPitchGenerationWeek >= 0 && currentWeek - Data.lastPitchGenerationWeek < 6) return;

            Data.lastPitchGenerationWeek = currentWeek;

            // Remove expired pitches
            Data.currentPitches.RemoveAll(p => p.expirationWeeks <= 0);

            if (Data.currentPitches.Count >= 5) return;

            var pubs = GameObject.FindGameObjectsWithTag("Publisher")
                .Select(go => go.GetComponent<publisherScript>())
                .Where(p => p != null && p.isUnlocked && !p.isPlayer && !p.IsMyTochterfirma())
                .ToList();

            if (pubs.Count == 0) return;

            int countToGen = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < countToGen; i++)
            {
                var pub = pubs[UnityEngine.Random.Range(0, pubs.Count)];
                var pitch = CreateRandomPitch(ms, pub);
                Data.currentPitches.Add(pitch);
            }

            PublishingDealsPlugin.Log.LogInfo($"Generated {countToGen} new second-party publishing pitches");
        }

        private static StudioPitch CreateRandomPitch(mainScript ms, publisherScript pub)
        {
            _nextId++;
            DealTier tier = (DealTier)UnityEngine.Random.Range(0, 3);
            long budget;
            int devWeeks;
            float quality;
            float devCut;

            switch (tier)
            {
                case DealTier.IndiePrototype:
                    budget = (long)UnityEngine.Random.Range(80000f, 250000f);
                    devWeeks = UnityEngine.Random.Range(16, 26);
                    quality = UnityEngine.Random.Range(55f, 75f);
                    devCut = 70f;
                    break;
                case DealTier.MidTierProject:
                    budget = (long)UnityEngine.Random.Range(400000f, 1200000f);
                    devWeeks = UnityEngine.Random.Range(28, 42);
                    quality = UnityEngine.Random.Range(65f, 85f);
                    devCut = 50f;
                    break;
                default:
                    budget = (long)UnityEngine.Random.Range(2000000f, 6000000f);
                    devWeeks = UnityEngine.Random.Range(48, 68);
                    quality = UnityEngine.Random.Range(75f, 92f);
                    devCut = 35f;
                    break;
            }

            int genre = UnityEngine.Random.Range(0, 15);
            int subgenre = UnityEngine.Random.Range(0, 15);
            string[] prefix = { "Project", "Operation", "Chronicles of", "The Legend of", "Cyber", "Shadow", "Galaxy", "Neon" };
            string[] noun = { "Vanguard", "Echoes", "Odyssey", "Protocol", "Horizon", "Ascent", "Legacy", "Frontier" };
            string title = $"{prefix[UnityEngine.Random.Range(0, prefix.Length)]} {noun[UnityEngine.Random.Range(0, noun.Length)]}";

            return new StudioPitch
            {
                pitchId = _nextId,
                developerId = pub.myID,
                developerName = pub.GetName(),
                gameTitle = title,
                genre = genre,
                subgenre = subgenre,
                tier = tier,
                requestedBudget = budget,
                proposedRoyaltySplit = devCut,
                devWeeks = devWeeks,
                estimatedQuality = quality,
                expirationWeeks = 8
            };
        }

        public static bool FundAndSignDeal(mainScript ms, StudioPitch pitch, float playerCut, int exclusivePlatformId, bool permanent, GUI_Main guiMain)
        {
            if (ms == null || pitch == null) return false;
            if (ms.money < pitch.requestedBudget) return false;

            // Deduct funding
            ms.Pay(pitch.requestedBudget, 25);

            var deal = new ActiveSecondPartyDeal
            {
                dealId = pitch.pitchId,
                developerId = pitch.developerId,
                developerName = pitch.developerName,
                gameTitle = pitch.gameTitle,
                genre = pitch.genre,
                subgenre = pitch.subgenre,
                fundedBudget = pitch.requestedBudget,
                playerRoyaltySplit = playerCut,
                weeksLeft = pitch.devWeeks,
                totalDevWeeks = pitch.devWeeks,
                exclusivePlatformId = exclusivePlatformId,
                isPermanentExclusive = permanent,
                isReleased = false,
                reviewScore = Mathf.Clamp(Mathf.RoundToInt(pitch.estimatedQuality + UnityEngine.Random.Range(-8f, 10f)), 30, 99)
            };

            Data.activeDeals.Add(deal);
            Data.currentPitches.Remove(pitch);

            try
            {
                var sfx = GameObject.Find("SFX")?.GetComponent<sfxScript>();
                sfx?.PlaySound(6, force: true);
            }
            catch { }

            if (guiMain != null)
            {
                guiMain.MessageBox(PublishingDealsLabels.DealSignedNotice, false);
            }

            PublishingDealsPlugin.Log.LogInfo($"Signed publishing deal for {pitch.gameTitle} with {pitch.developerName} (Budget: ${pitch.requestedBudget})");
            return true;
        }

        public static void GenerateBiddingWarIfNeeded(mainScript ms)
        {
            if (ms == null || ms.multiplayer) return;

            int currentWeek = ms.year * 48 + ms.month * 4 + ms.week;
            if (Data.lastBiddingWarGenerationWeek >= 0 && currentWeek - Data.lastBiddingWarGenerationWeek < 24) return;

            Data.lastBiddingWarGenerationWeek = currentWeek;

            // Only 1-2 active bidding wars at a time
            Data.activeBiddingWars.RemoveAll(b => b.resolved || b.weeksLeft <= 0);
            if (Data.activeBiddingWars.Count >= 2) return;

            var pubs = GameObject.FindGameObjectsWithTag("Publisher")
                .Select(go => go.GetComponent<publisherScript>())
                .Where(p => p != null && p.isUnlocked && !p.isPlayer && p.stars >= 60)
                .ToList();

            if (pubs.Count == 0) return;

            var pub = pubs[UnityEngine.Random.Range(0, pubs.Count)];
            _nextId++;

            string[] hitTitles = { "Dragon Soul Reborn", "Starfall 2099", "Titan's Wrath", "Mythos Online", "Galactic Dominion" };
            string title = hitTitles[UnityEngine.Random.Range(0, hitTitles.Length)];
            long startingBid = (long)(pub.firmenwert * 0.15f) + (long)UnityEngine.Random.Range(500000f, 2000000f);

            var bidding = new BiddingWarItem
            {
                biddingId = _nextId,
                developerId = pub.myID,
                developerName = pub.GetName(),
                gameTitle = title,
                genre = UnityEngine.Random.Range(0, 15),
                currentHighestBid = startingBid,
                leadingBidderId = pub.myID,
                leadingBidderName = pub.GetName(),
                weeksLeft = 6,
                resolved = false
            };

            Data.activeBiddingWars.Add(bidding);
            PublishingDealsPlugin.Log.LogInfo($"Bidding war started for {title} (Starting bid: ${startingBid})");
        }

        public static bool PlaceCounterBid(mainScript ms, BiddingWarItem bidding, long bidAmount, GUI_Main guiMain)
        {
            if (ms == null || bidding == null || bidding.resolved) return false;
            if (bidAmount <= bidding.currentHighestBid || ms.money < bidAmount) return false;

            bidding.currentHighestBid = bidAmount;
            bidding.leadingBidderId = -1; // Player leads
            bidding.leadingBidderName = ms.GetCompanyName();

            try
            {
                var sfx = GameObject.Find("SFX")?.GetComponent<sfxScript>();
                sfx?.PlaySound(3, force: true);
            }
            catch { }

            PublishingDealsPlugin.Log.LogInfo($"Player placed counter-bid of ${bidAmount} on {bidding.gameTitle}");
            return true;
        }

        public static void OnWeeklyTick(mainScript ms)
        {
            if (ms == null || ms.multiplayer) return;

            GeneratePitchesIfNeeded(ms);
            GenerateBiddingWarIfNeeded(ms);

            // Age unaccepted pitches
            foreach (var p in Data.currentPitches)
            {
                p.expirationWeeks--;
            }

            // Advance active second-party projects
            for (int i = Data.activeDeals.Count - 1; i >= 0; i--)
            {
                var deal = Data.activeDeals[i];
                if (!deal.isReleased)
                {
                    deal.weeksLeft--;
                    if (deal.weeksLeft <= 0)
                    {
                        // Release the game!
                        deal.isReleased = true;
                        deal.weeksLeft = 24; // Market shelf-life
                        ms.guiMain_?.CreateTopNewsInfo($"{deal.developerName}: {deal.gameTitle} released! (Published by {ms.GetCompanyName()})");
                    }
                }
                else
                {
                    // Game is in market sales phase
                    deal.weeksLeft--;

                    // Weekly sales revenue simulation based on score & budget
                    long baseSales = (long)(deal.reviewScore * 1200L * UnityEngine.Random.Range(0.85f, 1.25f));
                    if (deal.reviewScore >= 85) baseSales *= 3; // Hit multiplier
                    else if (deal.reviewScore < 60) baseSales /= 2;

                    int unitsThisWeek = (int)(baseSales / 30);
                    deal.unitsSold += unitsThisWeek;

                    long playerRevenueCut = (long)(baseSales * (deal.playerRoyaltySplit / 100f));
                    deal.totalRevenueEarned += playerRevenueCut;
                    Data.lifetimeSecondPartyProfits += playerRevenueCut;

                    ms.Earn(playerRevenueCut, 25);

                    // Platform Exclusivity System Seller Effect
                    if (deal.exclusivePlatformId >= 0 && ms.arrayPlatformsScripts != null)
                    {
                        for (int pIdx = 0; pIdx < ms.arrayPlatformsScripts.Length; pIdx++)
                        {
                            var plat = ms.arrayPlatformsScripts[pIdx];
                            if (plat != null && plat.myID == deal.exclusivePlatformId)
                            {
                                plat.marktanteil = Mathf.Min(100f, plat.marktanteil + 0.05f);
                                plat.units += Mathf.RoundToInt(unitsThisWeek * 0.20f);
                                plat.exklusivGames++;
                                break;
                            }
                        }
                    }

                    if (deal.weeksLeft <= 0)
                    {
                        // Deal sales finished
                        Data.activeDeals.RemoveAt(i);
                        Data.completedDeals.Add(deal);
                        PublishingDealsPlugin.Log.LogInfo($"Second-party deal for {deal.gameTitle} completed market life. Total earned: ${deal.totalRevenueEarned}");
                    }
                }
            }

            // Advance Bidding Wars
            for (int bIdx = Data.activeBiddingWars.Count - 1; bIdx >= 0; bIdx--)
            {
                var bidding = Data.activeBiddingWars[bIdx];
                bidding.weeksLeft--;

                // AI competitors counter-bid
                if (bidding.weeksLeft > 1 && UnityEngine.Random.Range(0, 100) < 40)
                {
                    long raise = (long)UnityEngine.Random.Range(100000f, 500000f);
                    bidding.currentHighestBid += raise;
                    bidding.leadingBidderId = bidding.developerId;
                    bidding.leadingBidderName = bidding.developerName;
                }

                if (bidding.weeksLeft <= 0 && !bidding.resolved)
                {
                    bidding.resolved = true;
                    if (bidding.leadingBidderId == -1) // Player won!
                    {
                        ms.Pay(bidding.currentHighestBid, 25);
                        ms.guiMain_?.MessageBox(PublishingDealsLabels.BiddingWonNotice, false);
                    }
                    else
                    {
                        ms.guiMain_?.CreateTopNewsInfo($"{bidding.leadingBidderName} won the bidding war for {bidding.gameTitle}!");
                    }
                }
            }
        }
    }

    // Weekly tick patch
    [HarmonyPatch(typeof(mainScript), nameof(mainScript.WochenUpdates))]
    internal static class PublishingWeeklyTickPatch
    {
        static void Postfix(mainScript __instance)
        {
            PublishingManager.OnWeeklyTick(__instance);
        }
    }

    // Save/Load persistence patches
    internal static class PublishingPersistence
    {
        private static string SavePath(mainScript ms, int slot) =>
            Path.Combine(Application.persistentDataPath, ms.GetSavegameTitle() + slot + ".publishingdeals.json");

        internal static void Save(mainScript ms, int slot)
        {
            try
            {
                if (ms == null || ms.multiplayer) return;
                string json = JsonUtility.ToJson(PublishingManager.Data, true);
                File.WriteAllText(SavePath(ms, slot), json);
            }
            catch (Exception ex)
            {
                PublishingDealsPlugin.Log.LogError("Save failed in PublishingDeals: " + ex);
            }
        }

        internal static void Load(mainScript ms, int slot)
        {
            try
            {
                if (ms == null || ms.multiplayer)
                {
                    PublishingManager.Data = new PublishingDealsSave();
                    return;
                }

                string p = SavePath(ms, slot);
                if (File.Exists(p))
                {
                    string json = File.ReadAllText(p);
                    PublishingManager.Data = JsonUtility.FromJson<PublishingDealsSave>(json) ?? new PublishingDealsSave();
                }
                else
                {
                    PublishingManager.Data = new PublishingDealsSave();
                }
            }
            catch (Exception ex)
            {
                PublishingDealsPlugin.Log.LogError("Load failed in PublishingDeals: " + ex);
                PublishingManager.Data = new PublishingDealsSave();
            }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class PublishingSavePatch
    {
        static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) PublishingPersistence.Save(ms, i);
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class PublishingLoadPatch
    {
        static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) PublishingPersistence.Load(ms, i);
        }
    }
}
