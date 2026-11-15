using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Ali.Mgt2.Simulation;

namespace MGT2Mod.CommunityDynamics
{
    [BepInPlugin("ali.mgt2.communitydynamics", "Community Dynamics & Social Hype", "1.0.0")]
    public class CommunityDynamicsPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.communitydynamics").PatchAll(typeof(CommunityDynamicsPlugin).Assembly);
            Log.LogInfo("Community Dynamics & Social Hype plugin loaded");
        }
    }

    public static class CommunityManager
    {
        public static CommunityDynamicsSave Data = new CommunityDynamicsSave();
        private static int _nextPostId = 1;

        public static void OnWeeklyTick(mainScript ms)
        {
            if (ms == null || ms.multiplayer) return;

            int currentWeek = ms.year * 48 + ms.month * 4 + ms.week;
            if (Data.lastPostWeek == currentWeek) return;
            Data.lastPostWeek = currentWeek;

            // Update Review Bombs
            for (int i = Data.activeReviewBombs.Count - 1; i >= 0; i--)
            {
                var bomb = Data.activeReviewBombs[i];
                bomb.weeksRemaining--;
                if (bomb.weeksRemaining <= 0 || bomb.compensationGiven)
                {
                    Data.activeReviewBombs.RemoveAt(i);
                    AddPost(new SocialPost
                    {
                        authorHandle = "@GameCommunityWatch",
                        content = $"The review bomb campaign on {bomb.gameName} has subsided as community anger calms down.",
                        gameName = bomb.gameName,
                        category = 1,
                        sentiment = 0,
                        likes = UnityEngine.Random.Range(120, 600),
                        retweets = UnityEngine.Random.Range(30, 150),
                        year = ms.year,
                        week = ms.week
                    });
                }
            }

            // Update Streamer Campaigns
            for (int i = Data.activeStreamers.Count - 1; i >= 0; i--)
            {
                var s = Data.activeStreamers[i];
                s.weeksActive--;
                if (s.weeksActive <= 0)
                {
                    Data.activeStreamers.RemoveAt(i);
                }
            }

            // Recalculate Overall Sentiment
            if (Data.activeReviewBombs.Count > 0)
            {
                Data.overallSentiment = Mathf.Max(0.10f, 0.85f - (Data.activeReviewBombs.Count * 0.25f));
            }
            else
            {
                Data.overallSentiment = Mathf.Min(0.95f, Data.overallSentiment + 0.02f);
            }

            // Cross-System Bridge: Sync community status to other systems
            double totalSeverity = Data.activeReviewBombs.Count * 25.0;
            CrossSystemBridge.SetDouble("ACTIVE_REVIEW_BOMB_SEVERITY", totalSeverity);
            CrossSystemBridge.SetDouble("COMMUNITY_SENTIMENT", (double)Data.overallSentiment);
            CrossSystemBridge.SetLong("STUDIO_HYPE", (long)Data.hypePoints);

            // Cross-System Bridge: Poll and process Game Release events from Publishing Deals
            var gameReleases = CrossSystemBridge.DequeueAllEvents("GAME_RELEASED");
            foreach (var rel in gameReleases)
            {
                var parts = rel.Split('|');
                if (parts.Length >= 5)
                {
                    string title = parts[0];
                    string dev = parts[1];
                    if (float.TryParse(parts[3], out float score))
                    {
                        if (score >= 85f)
                        {
                            AddPost(new SocialPost
                            {
                                authorHandle = "@GamingPulse",
                                content = $"إطلاق أسطوري للعبة {title} من تطوير {dev} وبدعم كامل من شركتنا! اللعبة تكتسح المراجعات بنسبة {score:F0}%!",
                                gameName = title,
                                category = 1,
                                sentiment = 2,
                                likes = UnityEngine.Random.Range(8000, 25000),
                                retweets = UnityEngine.Random.Range(2000, 9000),
                                year = ms.year,
                                week = ms.week
                            });
                            Data.hypePoints = Mathf.Min(100, Data.hypePoints + 15);
                        }
                        else if (score <= 45f)
                        {
                            AddPost(new SocialPost
                            {
                                authorHandle = "@AngryGamer",
                                content = $"لعبة {title} للمطور {dev} مخيبة للآمال بشكل لا يصدق ({score:F0}%)... مراجعات سلبية عاصفة تجتاح شبكات التواصل!",
                                gameName = title,
                                category = 1,
                                sentiment = 4,
                                likes = UnityEngine.Random.Range(5000, 15000),
                                retweets = UnityEngine.Random.Range(1800, 5000),
                                year = ms.year,
                                week = ms.week
                            });
                            Data.hypePoints = Mathf.Max(0, Data.hypePoints - 10);
                        }
                        else
                        {
                            AddPost(new SocialPost
                            {
                                authorHandle = "@GameReviewDaily",
                                content = $"صدرت رسمياً لعبة {title} من المطور {dev}! تقييمات متوسطة ({score:F0}%) واستقبال هادئ من مجتمع اللاعبين.",
                                gameName = title,
                                category = 1,
                                sentiment = 0,
                                likes = UnityEngine.Random.Range(1000, 4000),
                                retweets = UnityEngine.Random.Range(200, 800),
                                year = ms.year,
                                week = ms.week
                            });
                        }
                    }
                }
            }

            // Cross-System Bridge: Poll and process Corporate events from Stock Market
            var corpEvents = CrossSystemBridge.DequeueAllEvents("CORPORATE_EVENT");
            foreach (var postContent in corpEvents)
            {
                AddPost(new SocialPost
                {
                    authorHandle = "@BusinessInsider",
                    content = postContent,
                    gameName = "",
                    category = 2,
                    sentiment = 1,
                    likes = UnityEngine.Random.Range(3000, 10000),
                    retweets = UnityEngine.Random.Range(900, 3500),
                    year = ms.year,
                    week = ms.week
                });
            }

            // Generate organic community buzz occasionally
            if (UnityEngine.Random.Range(0, 100) < 40)
            {
                GenerateOrganicPost(ms);
            }

            // Trim old posts
            if (Data.posts.Count > 40)
            {
                Data.posts.RemoveRange(0, Data.posts.Count - 40);
            }
        }

        public static void AddPost(SocialPost post)
        {
            if (post == null) return;
            post.id = _nextPostId++;
            Data.posts.Add(post);
        }

        public static void EnsureSeedPosts(mainScript ms)
        {
            if (Data.posts.Count > 0) return;
            int y = ms != null ? ms.year : 2028;
            int w = ms != null ? ms.week : 1;

            AddPost(new SocialPost { authorHandle = "@PixelGuru", content = CommunityDynamicsLabels.SeedPost1, category = 0, sentiment = 1, likes = 1450, retweets = 320, year = y, week = w });
            AddPost(new SocialPost { authorHandle = "@GameTrader", content = CommunityDynamicsLabels.SeedPost2, category = 2, sentiment = 2, likes = 2300, retweets = 680, year = y, week = w });
            AddPost(new SocialPost { authorHandle = "u/RetroGamer", content = CommunityDynamicsLabels.SeedPost3, category = 0, sentiment = 0, likes = 890, retweets = 140, year = y, week = w });
            AddPost(new SocialPost { authorHandle = "@SpeedySam", content = CommunityDynamicsLabels.SeedPost4, category = 2, sentiment = 1, likes = 3400, retweets = 910, year = y, week = w });
            AddPost(new SocialPost { authorHandle = "@TechRadar", content = CommunityDynamicsLabels.SeedPost5, category = 0, sentiment = 2, likes = 1200, retweets = 290, year = y, week = w });
            AddPost(new SocialPost { authorHandle = "@IndiePulse", content = CommunityDynamicsLabels.SeedPost6, category = 1, sentiment = 1, likes = 1800, retweets = 450, year = y, week = w });
        }

        public static void LaunchBullshotCampaign(gameScript game, int strategy, mainScript ms)
        {
            if (game == null || ms == null) return;

            long cost = (strategy == 0) ? 80000 : 25000;
            if (ms.money < cost) return;

            ms.Pay(cost, 25);

            float hypeAdd = (strategy == 0) ? UnityEngine.Random.Range(35f, 50f) : UnityEngine.Random.Range(15f, 25f);
            game.hype = Mathf.Min(100f, game.hype + hypeAdd);

            int preOrderAdd = (strategy == 0) ? UnityEngine.Random.Range(8000, 25000) : UnityEngine.Random.Range(2000, 6000);
            game.vorbestellungen += preOrderAdd;

            var existing = Data.activeBullshots.FirstOrDefault(b => b.gameId == game.myID);
            if (existing != null)
            {
                existing.strategy = strategy;
                existing.hypeAdded += hypeAdd;
                existing.preOrdersAdded += preOrderAdd;
                existing.isDeceptive = (strategy == 0);
            }
            else
            {
                Data.activeBullshots.Add(new ActiveBullshotCampaign
                {
                    gameId = game.myID,
                    gameName = game.GetNameSimple(),
                    strategy = strategy,
                    hypeAdded = hypeAdd,
                    preOrdersAdded = preOrderAdd,
                    isDeceptive = (strategy == 0)
                });
            }

            if (strategy == 0)
            {
                AddPost(new SocialPost
                {
                    authorHandle = "@HypeInsider",
                    content = $"HOLY SMOKES! Did you see that trailer for {game.GetNameSimple()}?! Mind-blowing graphics and endless features promised!",
                    gameName = game.GetNameSimple(),
                    category = 2,
                    sentiment = 1,
                    likes = UnityEngine.Random.Range(2500, 8000),
                    retweets = UnityEngine.Random.Range(800, 2400),
                    year = ms.year,
                    week = ms.week
                });
            }
            else
            {
                AddPost(new SocialPost
                {
                    authorHandle = "@AuthenticGamer",
                    content = $"Really refreshing gameplay showcase from {game.GetNameSimple()}. No fake CGI, just honest game mechanics looking crisp.",
                    gameName = game.GetNameSimple(),
                    category = 1,
                    sentiment = 2,
                    likes = UnityEngine.Random.Range(600, 1800),
                    retweets = UnityEngine.Random.Range(150, 450),
                    year = ms.year,
                    week = ms.week
                });
            }
        }

        public static void SendStreamerCopies(gameScript game, int tier, mainScript ms)
        {
            if (game == null || ms == null) return;

            long[] costs = { 120000, 40000, 15000 };
            if (tier < 0 || tier >= costs.Length) tier = 0;

            if (ms.money < costs[tier]) return;
            ms.Pay(costs[tier], 25);

            var existing = Data.activeStreamers.FirstOrDefault(s => s.gameId == game.myID);
            if (existing != null)
            {
                existing.streamerTier = tier;
                existing.weeksActive = 5;
            }
            else
            {
                Data.activeStreamers.Add(new StreamerCampaign
                {
                    gameId = game.myID,
                    gameName = game.GetNameSimple(),
                    streamerTier = tier,
                    cost = costs[tier],
                    weeksActive = 5,
                    outcome = 0
                });
            }

            string[] handles = { "@MegaStreamerKai", "@DeepTechGamer", "@SpeedySam" };
            AddPost(new SocialPost
            {
                authorHandle = handles[tier],
                content = $"Thanks to the studio for sending an early access copy of {game.GetNameSimple()}! Going live this weekend to give our honest impressions!",
                gameName = game.GetNameSimple(),
                category = 2,
                sentiment = 1,
                likes = UnityEngine.Random.Range(1500, 5000),
                retweets = UnityEngine.Random.Range(400, 1200),
                year = ms.year,
                week = ms.week
            });
        }

        public static void IssuePublicApology(int gameId, mainScript ms)
        {
            if (ms == null) return;
            var bomb = Data.activeReviewBombs.FirstOrDefault(b => b.gameId == gameId);
            if (bomb == null || bomb.apologyIssued) return;

            long cost = 50000;
            if (ms.money < cost) return;
            ms.Pay(cost, 25);

            bomb.apologyIssued = true;

            AddPost(new SocialPost
            {
                authorHandle = "@GameDevOfficial",
                content = $"Official Statement regarding {bomb.gameName}: We hear your feedback loud and clear. We fell short of expectations and are committed to fixing all issues.",
                gameName = bomb.gameName,
                category = 1,
                sentiment = 3,
                likes = UnityEngine.Random.Range(3000, 9000),
                retweets = UnityEngine.Random.Range(1200, 3500),
                year = ms.year,
                week = ms.week
            });
        }

        public static void CommitEmergencyHotfix(int gameId, mainScript ms)
        {
            if (ms == null) return;
            var bomb = Data.activeReviewBombs.FirstOrDefault(b => b.gameId == gameId);
            if (bomb == null || bomb.hotfixCommitted) return;

            long cost = 75000;
            if (ms.money < cost) return;
            ms.Pay(cost, 25);

            bomb.hotfixCommitted = true;
            bomb.weeksRemaining = Mathf.Max(1, bomb.weeksRemaining - 2);

            AddPost(new SocialPost
            {
                authorHandle = "@PatchNotesFeed",
                content = $"{bomb.gameName} Hotfix 1.01 Released! Massive bugfixes, crash resolutions, and optimization tweaks deployed worldwide.",
                gameName = bomb.gameName,
                category = 1,
                sentiment = 2,
                likes = UnityEngine.Random.Range(2000, 6000),
                retweets = UnityEngine.Random.Range(800, 2000),
                year = ms.year,
                week = ms.week
            });
        }

        public static void GivePlayerCompensation(int gameId, mainScript ms)
        {
            if (ms == null) return;
            var bomb = Data.activeReviewBombs.FirstOrDefault(b => b.gameId == gameId);
            if (bomb == null || bomb.compensationGiven) return;

            long cost = 100000;
            if (ms.money < cost) return;
            ms.Pay(cost, 25);

            bomb.compensationGiven = true;
            bomb.weeksRemaining = 0;

            var games = UnityEngine.Object.FindObjectsOfType<gameScript>();
            var game = games.FirstOrDefault(g => g != null && g.myID == gameId);
            if (game != null)
            {
                game.userPositiv += UnityEngine.Random.Range(8000, 20000);
            }

            AddPost(new SocialPost
            {
                authorHandle = "@HappyPlayerClub",
                content = $"Free apology rewards pack sent to all {bomb.gameName} players! Huge respect to the devs for taking responsibility and treating players right.",
                gameName = bomb.gameName,
                category = 1,
                sentiment = 2,
                likes = UnityEngine.Random.Range(4500, 12000),
                retweets = UnityEngine.Random.Range(1500, 4000),
                year = ms.year,
                week = ms.week
            });
        }

        public static void HandleGameRelease(gameScript game, mainScript ms)
        {
            if (game == null || ms == null) return;

            var bullshot = Data.activeBullshots.FirstOrDefault(b => b.gameId == game.myID);
            var streamer = Data.activeStreamers.FirstOrDefault(s => s.gameId == game.myID);

            // Check Review Bombing Trigger via Archon's ViralCrisisEngine
            if (bullshot != null && bullshot.isDeceptive)
            {
                var vState = new ViralCrisisState
                {
                    ExpectationDebt = 45.0,
                    PromisedQualityScore = 92.0,
                    StudioTrustMetric = (double)Data.overallSentiment * 100.0
                };
                var shock = ViralCrisisEngine.EvaluateReleaseExpectationShock(vState, (double)game.reviewTotal, null);

                if (shock.RageSeverity > 10.0 || game.points_bugs >= 10f || game.reviewTotal < 70)
                {
                    // REVIEW BOMBING!
                    int negativeSurge = Mathf.RoundToInt((float)(shock.RageSeverity * 220.0 + 8000.0));
                    game.userNegativ += negativeSurge;

                    Data.activeReviewBombs.Add(new ReviewBombState
                    {
                        gameId = game.myID,
                        gameName = game.GetNameSimple(),
                        weeksRemaining = MathUtil.ClampInt((int)Math.Ceiling(shock.RageSeverity / 14.0) + 2, 3, 10),
                        apologyIssued = false,
                        hotfixCommitted = false,
                        compensationGiven = false,
                        initialNegativeSurge = negativeSurge
                    });

                    AddPost(new SocialPost
                    {
                        authorHandle = "@AngryGamersUnited",
                        content = $"FALSE ADVERTISING SCANDAL! {game.GetNameSimple()} was completely overhyped with fake CGI demos! Bugs everywhere, game is unplayable! BOYCOTT!",
                        gameName = game.GetNameSimple(),
                        category = 3,
                        sentiment = 4,
                        likes = UnityEngine.Random.Range(5000, 18000),
                        retweets = UnityEngine.Random.Range(2000, 7000),
                        year = ms.year,
                        week = ms.week
                    });

                    if (ms.guiMain_ != null)
                    {
                        ms.guiMain_.CreateTopNewsInfo($"[Community Backlash] {game.GetNameSimple()} is suffering from severe Review Bombing!");
                    }
                }
                else if (game.points_bugs < 3f && game.reviewTotal >= 85)
                {
                    // Masterpiece Delivered!
                    AddPost(new SocialPost
                    {
                        authorHandle = "@LegendaryReviews",
                        content = $"UNBELIEVABLE! {game.GetNameSimple()} actually lived up to the monumental hype! Flawless execution and a true masterpiece!",
                        gameName = game.GetNameSimple(),
                        category = 2,
                        sentiment = 2,
                        likes = UnityEngine.Random.Range(8000, 25000),
                        retweets = UnityEngine.Random.Range(3000, 10000),
                        year = ms.year,
                        week = ms.week
                    });

                    ms.AddFans(15000, -1);
                }

                Data.activeBullshots.Remove(bullshot);
            }

            // Check Streamer Impact via Archon's Influencer Simulation
            if (streamer != null)
            {
                var arch = (InfluencerArchetype)streamer.streamerTier;
                double subs = streamer.streamerTier == 0 ? 12000000.0 : (streamer.streamerTier == 1 ? 1500000.0 : 800000.0);
                var vState = new ViralCrisisState { StudioTrustMetric = Data.overallSentiment * 100.0 };
                var impact = ViralCrisisEngine.EvaluateInfluencerImpact(arch, (double)game.reviewTotal, subs, vState, new System.Random());

                if (impact.QualityAmplifiedMultiplier >= 1.20 && game.points_bugs < 7f)
                {
                    streamer.outcome = 1; // Viral Explosion
                    AddPost(new SocialPost
                    {
                        authorHandle = "@ViralClipsTV",
                        content = $"Streamer Kai played {game.GetNameSimple()} for 14 hours straight on stream! Peak viewer count broke 800,000! Everyone is buying it!",
                        gameName = game.GetNameSimple(),
                        category = 2,
                        sentiment = 1,
                        likes = UnityEngine.Random.Range(9000, 30000),
                        retweets = UnityEngine.Random.Range(4000, 12000),
                        year = ms.year,
                        week = ms.week
                    });
                }
                else if (game.points_bugs >= 14f || impact.QualityAmplifiedMultiplier < 0.85)
                {
                    streamer.outcome = 2; // Meme Mockery
                    AddPost(new SocialPost
                    {
                        authorHandle = "@GlitchFestStream",
                        content = $"Streamer spent 2 hours laughing at ridiculous physics glitches in {game.GetNameSimple()}. Clip is trending on every social platform right now!",
                        gameName = game.GetNameSimple(),
                        category = 3,
                        sentiment = 4,
                        likes = UnityEngine.Random.Range(6000, 15000),
                        retweets = UnityEngine.Random.Range(2500, 6000),
                        year = ms.year,
                        week = ms.week
                    });
                }
                else
                {
                    streamer.outcome = 3; // Solid Praise
                }
            }

            // Standard release post
            AddPost(new SocialPost
            {
                authorHandle = "@DailyGamingFeed",
                content = $"{game.GetNameSimple()} has officially launched worldwide! Early player reception is rolling in.",
                gameName = game.GetNameSimple(),
                category = (game.IsMyGame() ? 1 : 0),
                sentiment = (game.reviewTotal >= 80 ? 2 : (game.reviewTotal >= 50 ? 0 : 3)),
                likes = UnityEngine.Random.Range(500, 2000),
                retweets = UnityEngine.Random.Range(100, 600),
                year = ms.year,
                week = ms.week
            });
        }

        private static void GenerateOrganicPost(mainScript ms)
        {
            string[] authors = { "@RetroFan99", "@NextGenSeeker", "@IndieCorner", "@ConsoleWarrior", "@PCMasterGaming" };
            string author = authors[UnityEngine.Random.Range(0, authors.Length)];

            string[] thoughts =
            {
                CommunityDynamicsLabels.SeedPost1,
                CommunityDynamicsLabels.SeedPost2,
                CommunityDynamicsLabels.SeedPost3,
                CommunityDynamicsLabels.SeedPost4,
                CommunityDynamicsLabels.SeedPost5,
                CommunityDynamicsLabels.SeedPost6
            };

            AddPost(new SocialPost
            {
                authorHandle = author,
                content = thoughts[UnityEngine.Random.Range(0, thoughts.Length)],
                gameName = "",
                category = 0,
                sentiment = 0,
                likes = UnityEngine.Random.Range(50, 400),
                retweets = UnityEngine.Random.Range(10, 80),
                year = ms.year,
                week = ms.week
            });
        }
    }

    [HarmonyPatch(typeof(mainScript), nameof(mainScript.WochenUpdates))]
    internal static class CommunityWeeklyTickPatch
    {
        static void Postfix(mainScript __instance)
        {
            try
            {
                CommunityManager.OnWeeklyTick(__instance);
            }
            catch (Exception ex)
            {
                CommunityDynamicsPlugin.Log.LogError("Error in CommunityWeeklyTickPatch: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SetOnMarket))]
    internal static class CommunityGameReleasePatch
    {
        static void Postfix(gameScript __instance)
        {
            try
            {
                if (__instance == null || __instance.mS_ == null) return;
                CommunityManager.HandleGameRelease(__instance, __instance.mS_);
            }
            catch (Exception ex)
            {
                CommunityDynamicsPlugin.Log.LogError("Error in CommunityGameReleasePatch: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class CommunityGameSellPatch
    {
        static void Postfix(gameScript __instance)
        {
            try
            {
                if (__instance == null || __instance.mS_ == null || !__instance.IsMyGame()) return;

                // Handle Review Bomb impact on sales
                var bomb = CommunityManager.Data.activeReviewBombs.FirstOrDefault(b => b.gameId == __instance.myID);
                if (bomb != null && bomb.weeksRemaining > 0)
                {
                    if (!bomb.apologyIssued)
                    {
                        // 90% sales crash
                        if (__instance.sellsPerWeek != null && __instance.sellsPerWeek.Length > 0)
                        {
                            __instance.sellsPerWeek[0] = Mathf.Max(1, Mathf.RoundToInt(__instance.sellsPerWeek[0] * 0.10f));
                        }

                        // Penalty refunds
                        long refund = (long)(Mathf.Max(100, __instance.sellsPerWeek[0]) * 5);
                        __instance.mS_.Pay(refund, 25);
                    }
                    else if (!bomb.compensationGiven)
                    {
                        // Softened 30% drop
                        if (__instance.sellsPerWeek != null && __instance.sellsPerWeek.Length > 0)
                        {
                            __instance.sellsPerWeek[0] = Mathf.Max(1, Mathf.RoundToInt(__instance.sellsPerWeek[0] * 0.70f));
                        }
                    }
                }

                // Handle Streamer Viral Trend boost
                var streamer = CommunityManager.Data.activeStreamers.FirstOrDefault(s => s.gameId == __instance.myID);
                if (streamer != null && streamer.outcome == 1 && streamer.weeksActive > 0)
                {
                    if (__instance.sellsPerWeek != null && __instance.sellsPerWeek.Length > 0)
                    {
                        __instance.sellsPerWeek[0] = Mathf.RoundToInt(__instance.sellsPerWeek[0] * 1.8f);
                    }
                }
            }
            catch (Exception ex)
            {
                CommunityDynamicsPlugin.Log.LogError("Error in CommunityGameSellPatch: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.CalcReview))]
    internal static class CommunityCalcReviewPatch
    {
        static void Postfix(gameScript __instance)
        {
            try
            {
                if (__instance == null || __instance.mS_ == null) return;

                int score = __instance.reviewTotal;
                string gameName = __instance.GetNameSimple();
                string handle = "@CriticReviewsAggregator";
                string content = "";
                int cat = __instance.IsMyGame() ? 1 : 0;
                int sent = 0;

                if (score >= 90)
                {
                    content = $"CRITICAL ACCLAIM! {gameName} scores a sensational {score}%! Reviewers call it an instant classic!";
                    sent = 2;
                }
                else if (score >= 75)
                {
                    content = $"Positive consensus for {gameName} with a solid {score}% score. Praised for engaging gameplay.";
                    sent = 1;
                }
                else if (score >= 50)
                {
                    content = $"Mixed reviews for {gameName} sitting at {score}%. Fun in bursts but held back by rough edges.";
                    sent = 3;
                }
                else
                {
                    content = $"Reviewers pan {gameName} with a brutal {score}%. Heavily criticized across the board.";
                    sent = 4;
                }

                CommunityManager.AddPost(new SocialPost
                {
                    authorHandle = handle,
                    content = content,
                    gameName = gameName,
                    category = cat,
                    sentiment = sent,
                    likes = UnityEngine.Random.Range(800, 3500),
                    retweets = UnityEngine.Random.Range(200, 900),
                    year = __instance.mS_.year,
                    week = __instance.mS_.week
                });
            }
            catch (Exception ex)
            {
                CommunityDynamicsPlugin.Log.LogError("Error in CommunityCalcReviewPatch: " + ex);
            }
        }
    }

    internal static class CommunityPersistence
    {
        private static string SavePath(mainScript ms, int slot) =>
            Path.Combine(Application.persistentDataPath, ms.GetSavegameTitle() + slot + ".communitydynamics.json");

        internal static void Save(mainScript ms, int slot)
        {
            try
            {
                if (ms == null || ms.multiplayer) return;
                string p = SavePath(ms, slot);
                string json = JsonUtility.ToJson(CommunityManager.Data, true);
                File.WriteAllText(p, json);
            }
            catch (Exception ex)
            {
                CommunityDynamicsPlugin.Log.LogError("Save failed in CommunityDynamics: " + ex);
            }
        }

        internal static void Load(mainScript ms, int slot)
        {
            try
            {
                if (ms == null || ms.multiplayer)
                {
                    CommunityManager.Data = new CommunityDynamicsSave();
                    return;
                }

                string p = SavePath(ms, slot);
                if (File.Exists(p))
                {
                    string json = File.ReadAllText(p);
                    CommunityManager.Data = JsonUtility.FromJson<CommunityDynamicsSave>(json) ?? new CommunityDynamicsSave();
                }
                else
                {
                    CommunityManager.Data = new CommunityDynamicsSave();
                }
            }
            catch (Exception ex)
            {
                CommunityDynamicsPlugin.Log.LogError("Load failed in CommunityDynamics: " + ex);
                CommunityManager.Data = new CommunityDynamicsSave();
            }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class CommunitySavePatch
    {
        static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) CommunityPersistence.Save(ms, i);
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class CommunityLoadPatch
    {
        static void Postfix(savegameScript __instance, int i)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            if (ms != null && !ms.multiplayer) CommunityPersistence.Load(ms, i);
        }
    }
}
