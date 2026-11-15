using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Advanced stock market mechanics:
    /// - Quarterly dividends for 5%+ shareholders
    /// - Hostile takeover detection and AI defense (poison pill)
    /// - Board seat intelligence (see competitor projects early)
    /// - Controlling interest effects (influence AI decisions)
    /// </summary>
    internal static class AdvancedMechanics
    {
        // Dividend payout every 3 months (game months)
        private static int lastDividendMonth = -1;

        /// <summary>
        /// Called weekly to check for dividend payouts and poison pill expiry
        /// </summary>
        internal static void WeeklyTick(mainScript ms)
        {
            if (ms == null || ms.multiplayer) return;

            int currentMonth = ms.month;
            int currentYear = ms.year;
            int totalMonths = currentYear * 12 + currentMonth;

            // Quarterly dividends (every 3 months)
            if (lastDividendMonth == -1)
            {
                lastDividendMonth = totalMonths;
            }

            if (totalMonths - lastDividendMonth >= 3)
            {
                PayDividends(ms);
                lastDividendMonth = totalMonths;
            }

            // Check poison pill expiry
            CheckPoisonPills(ms, totalMonths);

            // Simulate competitor stock drift
            SimulateCompetitorStocksDrift(ms);

            // Sync with cross-system bridge
            SyncBridgeData(ms);
        }

        private static void SimulateCompetitorStocksDrift(mainScript ms)
        {
            if (ms == null) return;
            int totalWeeks = ms.year * 48 + ms.month * 4 + ms.week;
            var macro = Ali.Mgt2.Simulation.StockDriftEngine.GetMacroPhaseForWeek(totalWeeks);
            var rng = new System.Random(totalWeeks * 19);

            var pubs = GameObject.FindGameObjectsWithTag("Publisher")
                .Select(go => go.GetComponent<publisherScript>())
                .Where(p => p != null && p.isUnlocked && !p.isPlayer)
                .ToList();

            foreach (var pub in pubs)
            {
                var stock = StockData.GetOrCreate(pub.myID);
                double fairValue = StockData.CalculateSharePrice(pub, ms);
                if (stock.currentSharePrice <= 0.01)
                {
                    stock.currentSharePrice = fairValue;
                    stock.previousSharePrice = fairValue;
                }

                var stockState = new Ali.Mgt2.Simulation.CompanyStockState
                {
                    CompanyId = pub.myID.ToString(),
                    SharePrice = stock.currentSharePrice,
                    TotalShares = 1000000L,
                    FundamentalFairValue = fairValue,
                    StockBeta = 1.0 + (pub.stars * 0.1),
                    BaseWeeklyVolatility = 0.025,
                    MomentumEwma = stock.priceTrendPercent / 100.0,
                    BoardHostility = stock.poisonPillActive ? 85.0 : 20.0
                };

                var synergy = new Ali.Mgt2.Simulation.CrossSystemSynergyContext
                {
                    MacroPhase = macro,
                    ActiveReviewBombSeverity = 0.0
                };

                var res = Ali.Mgt2.Simulation.StockDriftEngine.SimulateWeeklyTick(stockState, totalWeeks, 0.0, 0.0, synergy, rng);

                stock.previousSharePrice = stock.currentSharePrice;
                stock.currentSharePrice = res.NewSharePrice;
                stock.priceTrendPercent = res.PercentChange;
            }
        }

        private static void SyncBridgeData(mainScript ms)
        {
            if (ms == null || StockData.Data == null) return;

            Ali.Mgt2.Simulation.CrossSystemBridge.SetBool("PLAYER_IS_PUBLIC", StockData.Data.playerIsPublic);
            Ali.Mgt2.Simulation.CrossSystemBridge.SetLong("PLAYER_VALUATION", PlayerIpoManager.State.marketValuation);
            Ali.Mgt2.Simulation.CrossSystemBridge.SetDouble("PLAYER_SHARE_PRICE", PlayerIpoManager.State.currentSharePrice);
            Ali.Mgt2.Simulation.CrossSystemBridge.SetDouble("BOARD_DISCONTENT", (double)PlayerIpoManager.State.boardDiscontent);

            foreach (var stock in StockData.Data.ownerships)
            {
                Ali.Mgt2.Simulation.CrossSystemBridge.SetOwnership(stock.companyId, stock.playerShare);
            }
        }

        private static void PayDividends(mainScript ms)
        {
            long totalDividends = 0;
            int companiesCount = 0;

            foreach (var stock in StockData.Data.ownerships)
            {
                if (stock.playerShare < 5f) continue; // Need at least 5% for dividends

                var pub = GetPublisher(stock.companyId);
                if (pub == null || !pub.isUnlocked) continue;

                // Calculate dividend: 10% of quarterly revenue * ownership %
                long quarterlyRevenue = 0;
                if (pub.tf_umsatz != null && pub.tf_umsatz.Length > 0)
                {
                    // Last 3 months
                    for (int i = 0; i < Math.Min(3, pub.tf_umsatz.Length); i++)
                    {
                        quarterlyRevenue += pub.tf_umsatz[i];
                    }
                }

                long dividend = (long)(quarterlyRevenue * 0.1f * stock.playerShare / 100f);

                if (dividend > 0)
                {
                    ms.Earn(dividend, 12); // Category 12: financial income
                    stock.lastDividend = dividend;
                    stock.lastDividendDate = ms.year * 12 + ms.month;
                    totalDividends += dividend;
                    companiesCount++;

                    StockMarketPlugin.Log.LogInfo($"Dividend from {pub.GetName()}: ${dividend}");
                }
            }

            if (totalDividends > 0)
            {
                string message = $"<color=green>Quarterly Dividends Received!</color>\n\n" +
                    $"Total: ${FormatMoney(totalDividends)}\n" +
                    $"From {companiesCount} companies";

                var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                guiMain?.MessageBox(message, false);
            }
        }

        private static void CheckPoisonPills(mainScript ms, int currentMonth)
        {
            foreach (var stock in StockData.Data.ownerships)
            {
                if (stock.poisonPillActive && stock.poisonPillExpiry <= currentMonth)
                {
                    stock.poisonPillActive = false;
                    StockMarketPlugin.Log.LogInfo($"Poison pill expired for company {stock.companyId}");
                }
            }
        }

        /// <summary>
        /// Detect hostile takeover attempts and trigger AI defense
        /// </summary>
        internal static void CheckHostileTakeover(publisherScript pub, float newPlayerShare, float oldPlayerShare, mainScript ms)
        {
            if (pub == null || ms == null) return;

            var stock = StockData.GetOrCreate(pub.myID);

            // Hostile if: acquiring 25%+ in one purchase, company is successful, and no prior relationship
            bool isHostile = newPlayerShare >= 25f && oldPlayerShare < 25f &&
                             pub.firmenwert > 5000000L && !pub.IsMyTochterfirma();

            if (isHostile && !stock.poisonPillActive)
            {
                // AI defense: poison pill (makes further acquisition expensive)
                // 50% chance they activate it
                if (UnityEngine.Random.value > 0.5f)
                {
                    stock.poisonPillActive = true;
                    stock.poisonPillExpiry = (ms.year * 12 + ms.month) + 12; // 1 year duration

                    var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                    guiMain?.MessageBox($"<color=yellow>Poison Pill Activated!</color>\n\n" +
                        $"{pub.GetName()} has issued new shares to dilute your stake.\n" +
                        $"Further acquisitions will be more expensive for 1 year.", false);

                    StockMarketPlugin.Log.LogInfo($"Poison pill activated for {pub.GetName()}");
                }
            }
        }

        /// <summary>
        /// Board seat benefit: notification when company starts a secret project
        /// </summary>
        [HarmonyPatch(typeof(taskGame), "Start")]
        internal static class BoardSeatIntelligence
        {
            static void Postfix(taskGame __instance)
            {
                try
                {
                    if (__instance == null || __instance.gS_ == null) return;

                    var game = __instance.gS_;
                    int publisherId = game.publisherID;
                    if (publisherId <= 0) return;

                    var pub = GetPublisher(publisherId);
                    if (pub == null || pub.IsMyTochterfirma()) return;

                    // Skip if it's player's own game
                    if (game.IsMyGame()) return;

                    var stock = StockData.GetOrCreate(pub.myID);

                    // Board seat: 25%+ ownership
                    if (stock.playerShare >= 25f && stock.playerShare < 51f)
                    {
                        var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
                        var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();

                        if (ms != null && guiMain != null && game != null)
                        {
                            guiMain.MessageBox($"<color=cyan>Board Intelligence</color>\n\n" +
                                $"{pub.GetName()} is developing a new game:\n" +
                                $"{game.GetNameWithTag()}", false);

                            StockMarketPlugin.Log.LogInfo($"Board seat intel: {pub.GetName()} developing {game.GetNameWithTag()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    StockMarketPlugin.Log.LogError($"Board seat intel failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Controlling interest (51%+): influence company's genre focus
        /// </summary>
        internal static void ApplyControllingInfluence(publisherScript pub, mainScript ms)
        {
            if (pub == null || ms == null) return;

            var stock = StockData.GetOrCreate(pub.myID);

            if (stock.playerShare >= 51f && stock.playerShare < 100f)
            {
                // Influence: boost their focus on genres you're successful in
                // This is a subtle effect - they're more likely to copy your successful genres
                if (pub.tf_gameGenre != -1)
                {
                    // 30% chance to shift their focus toward a random popular genre
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        // Just log for now - full implementation needs genre analysis
                        StockMarketPlugin.Log.LogInfo($"Controlling interest: influenced {pub.GetName()}'s strategy");
                    }
                }
            }
        }

        /// <summary>
        /// Weekly tick hook to trigger all mechanics
        /// </summary>
        [HarmonyPatch(typeof(mainScript), nameof(mainScript.WochenUpdates))]
        internal static class WeeklyTickPatch
        {
            static void Postfix(mainScript __instance)
            {
                try
                {
                    if (__instance != null && !__instance.multiplayer)
                    {
                        WeeklyTick(__instance);
                    }
                }
                catch (Exception ex)
                {
                    StockMarketPlugin.Log.LogError($"Weekly tick failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Apply poison pill price increase
        /// </summary>
        internal static long ApplyPoisonPillMultiplier(publisherScript pub, long basePrice)
        {
            var stock = StockData.GetOrCreate(pub.myID);

            if (stock.poisonPillActive)
            {
                return (long)(basePrice * 1.5f); // 50% more expensive
            }

            return basePrice;
        }

        private static publisherScript GetPublisher(int id)
        {
            var obj = GameObject.FindGameObjectsWithTag("Publisher")
                .FirstOrDefault(o => o.GetComponent<publisherScript>()?.myID == id);
            return obj?.GetComponent<publisherScript>();
        }

        private static string FormatMoney(long amount)
        {
            if (amount >= 1000000000) return $"{amount / 1000000000.0:F1}B";
            if (amount >= 1000000) return $"{amount / 1000000.0:F1}M";
            if (amount >= 1000) return $"{amount / 1000.0:F0}K";
            return amount.ToString();
        }
    }
}
