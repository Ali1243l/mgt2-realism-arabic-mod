using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Ali.Mgt2.Simulation;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Represents the current attitude of the Board of Directors toward the player studio.
    /// </summary>
    public enum BoardSentiment
    {
        Satisfied,
        Cautious,
        Demanding,
        Hostile
    }

    /// <summary>
    /// Serializable state container for the Player IPO and Board of Directors system.
    /// Embed or serialize this instance inside StockMarketSave for seamless save/load persistence.
    /// </summary>
    [Serializable]
    public class PlayerIpoState
    {
        public long totalShares = 1000000L;
        public double currentSharePrice = 1.0;
        public long marketValuation = 1000000L;
        public long ipoLaunchValuation = 0L;
        public int ipoYear = 0;
        public int ipoMonth = 0;

        public float boardDiscontent = 0f;
        public float investorConfidence = 60f;

        public long quarterlyRevenueTarget = 0L;
        public long currentQuarterRevenue = 0L;
        public long lastQuarterRevenue = 0L;

        public long lastRecordedMoney = 0L;
        public long financialAdjustmentThisTick = 0L;
        public bool hasMoneyBaseline = false;

        public int lastProcessedYear = -1;
        public int lastProcessedMonth = -1;
        public int lastProcessedWeek = -1;

        public int lastEvaluatedYear = -1;
        public int lastEvaluatedMonth = -1;
        public int monthsSinceLastBoardMeeting = 0;

        public int consecutiveTargetsMet = 0;
        public int consecutiveTargetsMissed = 0;

        public long totalDividendsPaid = 0L;
        public long lastRecommendedDividend = 0L;
        public long pendingEmergencyDividend = 0L;
        public bool spendingRestrictionActive = false;
        public bool dividendOfferedThisQuarter = false;

        public float founderSharePercent = 100f;
        public int hostileRivalCompanyId = -1;
        public string hostileRivalName = "";
        public float hostileRivalSharePercent = 0f;
        public bool poisonPillActive = false;
        public int poisonPillExpiryMonth = 0;
        public double previousSharePrice = 1.0;
        public double priceTrendPercent = 0.0;

        public List<long> weeklyNetRevenueHistory = new List<long>();
    }

    /// <summary>
    /// Manages studio valuation, IPO execution, quarterly Board meetings, dividends, and Harmony tick hooks.
    /// </summary>
    public static class PlayerIpoManager
    {
        public const float MinIpoSharePercentage = 0.10f;
        public const float MaxIpoSharePercentage = 0.40f;
        public const float IpoUnderwritingFeeRate = 0.03f;
        public const float HighDiscontentThreshold = 75f;
        public const long DefaultTotalShares = 1000000L;
        public const long MinimumValuationFloor = 100000L;
        public const int WeeksPerYear = 48;
        public const int FinanceCategory = 12;

        private static PlayerIpoState _state = new PlayerIpoState();
        private static bool _reflectionInitialized = false;

        private static MethodInfo _miGetCompanyValuation;
        private static FieldInfo _fiFans;
        private static FieldInfo _fiStudioRating;
        private static FieldInfo _fiGamesScript;
        private static FieldInfo _fiArrayGamesScripts;
        private static FieldInfo _fiGuiMain;
        private static MethodInfo _miMessageBox;

        /// <summary>
        /// Event fired whenever the Board issues an important notice or quarterly meeting summary.
        /// </summary>
        public static event Action<string, bool> OnBoardNotification;

        /// <summary>
        /// Direct reference to the active serializable state for saving and loading with StockMarketSave.
        /// </summary>
        public static PlayerIpoState State
        {
            get
            {
                if (_state == null)
                {
                    _state = new PlayerIpoState();
                }
                return _state;
            }
            set
            {
                _state = value ?? new PlayerIpoState();
                if (_state.weeklyNetRevenueHistory == null)
                {
                    _state.weeklyNetRevenueHistory = new List<long>();
                }
                if (_state.totalShares <= 0L)
                {
                    _state.totalShares = DefaultTotalShares;
                }
            }
        }

        /// <summary>
        /// Resets all IPO and Board state (for new game sessions).
        /// </summary>
        public static void ResetState()
        {
            _state = new PlayerIpoState();
        }

        /// <summary>
        /// Serializes the current IPO state to JSON for integration with StockMarketSave.
        /// </summary>
        public static string ExportStateToJson()
        {
            return JsonUtility.ToJson(State);
        }

        /// <summary>
        /// Restores IPO state from a JSON string stored in StockMarketSave.
        /// </summary>
        public static void ImportStateFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                ResetState();
                return;
            }

            try
            {
                PlayerIpoState loaded = JsonUtility.FromJson<PlayerIpoState>(json);
                State = loaded;
            }
            catch (Exception ex)
            {
                LogWarning("Failed to deserialize PlayerIpoState JSON, resetting state: " + ex.Message);
                ResetState();
            }
        }

        /// <summary>
        /// Calculates the player's studio market capitalization using:
        /// 1. Liquid cash / debt from ms.money
        /// 2. Trailing 12-month net revenue multiplied by a confidence-adjusted P/E multiple
        /// 3. Value of owned IPs / active games on the market
        /// 4. Studio reputation and fanbase equity
        /// </summary>
        public static long CalculatePlayerValuation(mainScript ms)
        {
            if (ms == null)
            {
                return MinimumValuationFloor;
            }

            EnsureReflectionInitialized();

            // 1. Cash Component:
            // Positive cash contributes 100% of book value; negative cash applies a 1.25x debt penalty.
            double cashValue = ms.money >= 0L
                ? (double)ms.money
                : (double)ms.money * 1.25;

            // 2. Trailing 12-Month Net Revenue Component:
            // Formula: Positive12MRevenue * EarningsMultiple, where EarningsMultiple scales from 3.5x to 6.5x based on InvestorConfidence.
            long trailing12MRevenue = GetTrailing12MonthNetRevenue();
            double confidenceFactor = Mathf.Clamp(State.investorConfidence, 0f, 100f) / 100.0;
            double earningsMultiple = 3.5 + (confidenceFactor * 3.0);

            // If revenue history is still empty (early mod load), blend in engine valuation if available.
            double earningsValue;
            if (State.weeklyNetRevenueHistory.Count < 4)
            {
                long engineBaseValuation = TryGetEngineCompanyValuation(ms);
                earningsValue = Math.Max((double)trailing12MRevenue * earningsMultiple, (double)engineBaseValuation * 0.65);
            }
            else
            {
                earningsValue = trailing12MRevenue > 0L
                    ? (double)trailing12MRevenue * earningsMultiple
                    : (double)trailing12MRevenue * 1.5;
            }

            // 3. Owned IPs & Active Games Portfolio Component:
            // Each released/active title adds baseline IP value scaled by review score and lifetime sales.
            double ipPortfolioValue = EstimateOwnedIpAndGamesValue(ms);

            // 4. Studio Reputation & Fanbase Equity:
            // Fans and studio reputation provide intangible brand value ($150 per fan + reputation bonus).
            double brandEquityValue = EstimateReputationAndFansValue(ms);

            double rawValuation = cashValue + earningsValue + ipPortfolioValue + brandEquityValue;

            // Apply board discontent discount when public (-0% up to -20% at 100% discontent).
            if (StockData.Data != null && StockData.Data.playerIsPublic)
            {
                double discontentPenalty = 1.0 - (Mathf.Clamp(State.boardDiscontent, 0f, 100f) / 100.0) * 0.20;
                rawValuation *= discontentPenalty;
            }

            // Sanitize to positive long within safe bounds.
            if (double.IsNaN(rawValuation) || double.IsInfinity(rawValuation))
            {
                return MinimumValuationFloor;
            }

            double clamped = Math.Max((double)MinimumValuationFloor, Math.Min(rawValuation, (double)(long.MaxValue / 4L)));
            return (long)Math.Round(clamped);
        }

        /// <summary>
        /// Takes the player's studio public by selling between 10% and 40% equity.
        /// Deposits net proceeds via ms.Earn(amount, 12) and initializes Board targets.
        /// </summary>
        public static bool LaunchPlayerIPO(mainScript ms, float sharePercentageToSell, out string statusMessage)
        {
            if (ms == null)
            {
                statusMessage = "Game instance is unavailable.";
                return false;
            }

            if (StockData.Data == null)
            {
                statusMessage = "StockMarket save data is not initialized.";
                return false;
            }

            if (StockData.Data.playerIsPublic)
            {
                statusMessage = "Studio is already publicly traded.";
                return false;
            }

            // Support both fractional (0.10 - 0.40) and percentage (10 - 40) input formats.
            float normalizedShare = sharePercentageToSell > 1.0f && sharePercentageToSell <= 100.0f
                ? sharePercentageToSell / 100.0f
                : sharePercentageToSell;

            if (normalizedShare < MinIpoSharePercentage - 0.0001f || normalizedShare > MaxIpoSharePercentage + 0.0001f)
            {
                statusMessage = "IPO share offering must be between 10% and 40%.";
                return false;
            }

            normalizedShare = Mathf.Clamp(normalizedShare, MinIpoSharePercentage, MaxIpoSharePercentage);

            // Calculate pre-IPO valuation before injecting capital.
            long preIpoValuation = CalculatePlayerValuation(ms);
            double grossProceeds = (double)preIpoValuation * normalizedShare;
            long netProceeds = Math.Max(1000L, (long)Math.Round(grossProceeds * (1.0 - IpoUnderwritingFeeRate)));

            // Record financial adjustment so weekly revenue tracking does not count IPO capital as operating revenue.
            State.financialAdjustmentThisTick += netProceeds;
            ms.Earn(netProceeds, FinanceCategory);

            // Update StockData public status.
            StockData.Data.playerIsPublic = true;
            StockData.Data.playerPublicShare = normalizedShare;

            // Initialize IPO & Board metrics.
            State.totalShares = DefaultTotalShares;
            State.ipoLaunchValuation = preIpoValuation;
            State.marketValuation = preIpoValuation + netProceeds;
            State.currentSharePrice = Math.Max(0.01, (double)State.marketValuation / (double)State.totalShares);
            State.ipoYear = ms.year;
            State.ipoMonth = ms.month;

            State.boardDiscontent = 10f;
            State.investorConfidence = 68f;
            State.consecutiveTargetsMet = 0;
            State.consecutiveTargetsMissed = 0;
            State.monthsSinceLastBoardMeeting = 0;
            State.lastEvaluatedYear = ms.year;
            State.lastEvaluatedMonth = ms.month;
            State.currentQuarterRevenue = 0L;
            State.lastQuarterRevenue = 0L;
            State.spendingRestrictionActive = false;
            State.pendingEmergencyDividend = 0L;
            State.dividendOfferedThisQuarter = false;

            State.founderSharePercent = (1.0f - normalizedShare) * 100f;
            State.hostileRivalCompanyId = -1;
            State.hostileRivalName = "";
            State.hostileRivalSharePercent = 0f;
            State.poisonPillActive = false;
            State.previousSharePrice = State.currentSharePrice;
            State.priceTrendPercent = 0.0;

            CrossSystemBridge.EnqueueEvent("CORPORATE_EVENT",
                $"@TechVentureDaily: عاجل: {ms.GetCompanyName()} تطلق رسمياً اكتتابها العام في البورصة وتجمع {ms.GetMoney(netProceeds, true)} لتمويل مشاريعها القادمة! #IPO");

            // Set baseline money after IPO cash injection.
            State.lastRecordedMoney = ms.money;
            State.hasMoneyBaseline = true;

            // Calculate initial quarterly revenue expectation.
            State.quarterlyRevenueTarget = ComputeNextQuarterlyTarget(State.marketValuation, 0L, 0);

            statusMessage = string.Format(
                "IPO Successful! Sold {0:P0} equity for {1:N0}. Initial Share Price: {2:C2}. Q1 Target: {3:N0}.",
                normalizedShare,
                netProceeds,
                State.currentSharePrice,
                State.quarterlyRevenueTarget);

            LogInfo(statusMessage);
            DispatchNotification(ms, "IPO Launched!", statusMessage, false);
            return true;
        }

        /// <summary>
        /// Evaluates quarterly performance against Board expectations every 3 game months.
        /// Adjusts share price, investor confidence, board discontent, dividends, and spending controls.
        /// </summary>
        public static void EvaluateQuarterlyBoardMeeting(mainScript ms)
        {
            if (ms == null || StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                return;
            }

            // Prevent duplicate evaluation in the same month/year.
            if (State.lastEvaluatedYear == ms.year && State.lastEvaluatedMonth == ms.month)
            {
                return;
            }

            State.lastEvaluatedYear = ms.year;
            State.lastEvaluatedMonth = ms.month;
            State.monthsSinceLastBoardMeeting = 0;

            long actualRevenue = State.currentQuarterRevenue;
            long targetRevenue = Math.Max(25000L, State.quarterlyRevenueTarget);
            double attainmentRatio = (double)actualRevenue / (double)targetRevenue;

            string meetingSummary;
            bool isWarning = false;

            if (actualRevenue >= targetRevenue)
            {
                // Target Met / Exceeded: Share price increases +5% to +10%.
                double beatExcess = Math.Min(0.50, Math.Max(0.0, attainmentRatio - 1.0));
                double shareBoostPct = 0.05 + (beatExcess / 0.50) * 0.05;
                State.currentSharePrice = Math.Max(0.01, State.currentSharePrice * (1.0 + shareBoostPct));
                State.marketValuation = (long)Math.Round(State.currentSharePrice * State.totalShares);

                // Boost investor confidence & studio hype; reduce board discontent.
                float confidenceGain = 8f + (float)(beatExcess * 14.0);
                State.investorConfidence = Mathf.Clamp(State.investorConfidence + confidenceGain, 0f, 100f);
                State.boardDiscontent = Mathf.Clamp(State.boardDiscontent - (10f + (float)(beatExcess * 15.0)), 0f, 100f);

                State.consecutiveTargetsMet++;
                State.consecutiveTargetsMissed = 0;
                State.spendingRestrictionActive = false;
                State.pendingEmergencyDividend = 0L;

                // Offer optional dividend to public shareholders (15% of quarterly net revenue * public share).
                long recommendedDividend = CalculateRecommendedDividend(actualRevenue);
                State.lastRecommendedDividend = recommendedDividend;
                State.dividendOfferedThisQuarter = recommendedDividend > 0L;

                meetingSummary = string.Format(
                    "Quarterly Board Meeting: Target MET ({0:N0} / {1:N0}). Share Price +{2:P1} ({3:C2}). Investor Confidence: {4:F0}%. Recommended Dividend: {5:N0}.",
                    actualRevenue,
                    targetRevenue,
                    shareBoostPct,
                    State.currentSharePrice,
                    State.investorConfidence,
                    recommendedDividend);
            }
            else
            {
                isWarning = true;
                double missSeverity = Math.Min(1.0, Math.Max(0.05, 1.0 - attainmentRatio));

                // Target Missed: Share price drops -5% to -18% based on miss severity.
                double shareDropPct = 0.05 + (missSeverity * 0.13);
                State.currentSharePrice = Math.Max(0.01, State.currentSharePrice * (1.0 - shareDropPct));
                State.marketValuation = (long)Math.Round(State.currentSharePrice * State.totalShares);

                // Discontent rises +12% to +28%; confidence drops.
                float discontentGain = 12f + (float)(missSeverity * 16f) + (State.consecutiveTargetsMissed * 3f);
                State.boardDiscontent = Mathf.Clamp(State.boardDiscontent + discontentGain, 0f, 100f);
                State.investorConfidence = Mathf.Clamp(State.investorConfidence - (8f + (float)(missSeverity * 12f)), 0f, 100f);

                State.consecutiveTargetsMissed++;
                State.consecutiveTargetsMet = 0;
                State.lastRecommendedDividend = 0L;
                State.dividendOfferedThisQuarter = false;

                // High discontent (>75%): block aggressive spending & demand emergency dividend.
                if (State.boardDiscontent > HighDiscontentThreshold)
                {
                    State.spendingRestrictionActive = true;
                    long emergencyDividend = CalculateEmergencyDividend(ms);
                    State.pendingEmergencyDividend = emergencyDividend;

                    meetingSummary = string.Format(
                        "BOARD CRISIS! Target MISSED ({0:N0} / {1:N0}). Share Price -{2:P1}. Discontent CRITICAL ({3:F0}%). Aggressive spending BLOCKED and emergency dividend of {4:N0} demanded!",
                        actualRevenue,
                        targetRevenue,
                        shareDropPct,
                        State.boardDiscontent,
                        emergencyDividend);
                }
                else
                {
                    State.spendingRestrictionActive = false;
                    meetingSummary = string.Format(
                        "Board Warning: Quarterly target MISSED ({0:N0} / {1:N0}). Share Price -{2:P1} ({3:C2}). Board Discontent: {4:F0}%.",
                        actualRevenue,
                        targetRevenue,
                        shareDropPct,
                        State.currentSharePrice,
                        State.boardDiscontent);
                }
            }

            // Roll forward quarter values and calculate next dynamic target (+5% to +15% growth expectation).
            State.lastQuarterRevenue = actualRevenue;
            State.currentQuarterRevenue = 0L;
            State.quarterlyRevenueTarget = ComputeNextQuarterlyTarget(State.marketValuation, actualRevenue, State.consecutiveTargetsMet);

            LogInfo(meetingSummary);
            DispatchNotification(ms, isWarning ? "Board of Directors Warning" : "Quarterly Board Report", meetingSummary, isWarning);
        }

        /// <summary>
        /// Pays an optional quarterly dividend to public shareholders using ms.Pay(amount, 12).
        /// Boosts investor confidence and reduces board discontent.
        /// </summary>
        public static bool PayQuarterlyDividend(mainScript ms, long customAmount, out string resultMessage)
        {
            if (ms == null || StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                resultMessage = "Studio is not publicly traded.";
                return false;
            }

            long payout = customAmount > 0L ? customAmount : State.lastRecommendedDividend;
            if (payout <= 0L)
            {
                resultMessage = "No dividend payout amount available.";
                return false;
            }

            if (ms.money < payout)
            {
                resultMessage = "Insufficient studio funds to pay dividend.";
                return false;
            }

            State.financialAdjustmentThisTick -= payout;
            ms.Pay(payout, FinanceCategory);
            State.totalDividendsPaid += payout;
            State.dividendOfferedThisQuarter = false;
            State.lastRecommendedDividend = 0L;

            // Reward shareholder goodwill.
            State.investorConfidence = Mathf.Clamp(State.investorConfidence + 7f, 0f, 100f);
            State.boardDiscontent = Mathf.Clamp(State.boardDiscontent - 10f, 0f, 100f);
            State.currentSharePrice = Math.Max(0.01, State.currentSharePrice * 1.025);
            State.marketValuation = (long)Math.Round(State.currentSharePrice * State.totalShares);
            State.lastRecordedMoney = ms.money;

            resultMessage = string.Format("Paid {0:N0} in shareholder dividends. Investor Confidence increased to {1:F0}%.", payout, State.investorConfidence);
            LogInfo(resultMessage);
            return true;
        }

        /// <summary>
        /// Resolves a Board crisis by paying the demanded emergency dividend via ms.Pay(amount, 12).
        /// </summary>
        public static bool PayEmergencyDividend(mainScript ms, out string resultMessage)
        {
            if (ms == null || StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                resultMessage = "Studio is not publicly traded.";
                return false;
            }

            long required = State.pendingEmergencyDividend;
            if (required <= 0L)
            {
                resultMessage = "No emergency dividend is currently demanded.";
                return false;
            }

            if (ms.money < required)
            {
                resultMessage = string.Format("Insufficient funds. Need {0:N0} to satisfy Board emergency demand.", required);
                return false;
            }

            State.financialAdjustmentThisTick -= required;
            ms.Pay(required, FinanceCategory);
            State.totalDividendsPaid += required;
            State.pendingEmergencyDividend = 0L;
            State.boardDiscontent = Mathf.Clamp(State.boardDiscontent - 22f, 0f, 100f);
            State.investorConfidence = Mathf.Clamp(State.investorConfidence + 5f, 0f, 100f);
            State.spendingRestrictionActive = State.boardDiscontent > HighDiscontentThreshold;
            State.lastRecordedMoney = ms.money;

            resultMessage = string.Format("Emergency dividend of {0:N0} paid. Board Discontent reduced to {1:F0}%.", required, State.boardDiscontent);
            LogInfo(resultMessage);
            return true;
        }

        /// <summary>
        /// Checks whether the Board currently permits an aggressive capital expenditure (e.g., studio buyouts or large investments).
        /// </summary>
        public static bool CanApproveAggressiveSpending(mainScript ms, long proposedCost, out string blockReason)
        {
            if (StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                blockReason = string.Empty;
                return true;
            }

            if (State.spendingRestrictionActive || State.boardDiscontent > HighDiscontentThreshold)
            {
                long safeLimit = ms != null ? Math.Max(50000L, ms.money / 10L) : 50000L;
                if (proposedCost > safeLimit)
                {
                    blockReason = string.Format(
                        "Board Blocked Spending: Discontent is {0:F0}% (>75%). Expenditures above {1:N0} are frozen until performance improves or emergency dividends are settled.",
                        State.boardDiscontent,
                        safeLimit);
                    return false;
                }
            }

            blockReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Weekly tick handler invoked by the Harmony Postfix on mainScript.WochenUpdates.
        /// Tracks organic weekly net cashflow and triggers quarterly evaluations every 3 months.
        /// </summary>
        public static void OnWeeklyTick(mainScript ms)
        {
            if (ms == null)
            {
                return;
            }

            // Deduplicate if WochenUpdates is invoked multiple times within the same game week.
            if (State.lastProcessedYear == ms.year &&
                State.lastProcessedMonth == ms.month &&
                State.lastProcessedWeek == ms.week)
            {
                return;
            }

            bool monthChanged = State.lastProcessedMonth != -1 &&
                                (State.lastProcessedMonth != ms.month || State.lastProcessedYear != ms.year);

            State.lastProcessedYear = ms.year;
            State.lastProcessedMonth = ms.month;
            State.lastProcessedWeek = ms.week;

            // Initialize baseline on first tick.
            if (!State.hasMoneyBaseline)
            {
                State.lastRecordedMoney = ms.money;
                State.hasMoneyBaseline = true;
                return;
            }

            // Compute organic weekly net cashflow by subtracting non-operating IPO/dividend transfers.
            long rawDelta = ms.money - State.lastRecordedMoney;
            long organicWeeklyNet = rawDelta - State.financialAdjustmentThisTick;
            State.financialAdjustmentThisTick = 0L;
            State.lastRecordedMoney = ms.money;

            // Store rolling 48-week history (12 game months).
            State.weeklyNetRevenueHistory.Add(organicWeeklyNet);
            while (State.weeklyNetRevenueHistory.Count > WeeksPerYear)
            {
                State.weeklyNetRevenueHistory.RemoveAt(0);
            }

            if (StockData.Data != null && StockData.Data.playerIsPublic)
            {
                State.currentQuarterRevenue += organicWeeklyNet;

                // Deep Simulation: Stock Drift Engine with Macro Cycles, Alpha Shocks, and Review Bomb Drag
                long intrinsicValuation = CalculatePlayerValuation(ms);
                double fairValue = Math.Max(0.01, (double)intrinsicValuation / (double)State.totalShares);

                var stockState = new Ali.Mgt2.Simulation.CompanyStockState
                {
                    CompanyId = "PLAYER",
                    SharePrice = State.currentSharePrice,
                    TotalShares = State.totalShares,
                    FundamentalFairValue = fairValue,
                    StockBeta = 1.15,
                    BaseWeeklyVolatility = 0.028,
                    MomentumEwma = State.priceTrendPercent / 100.0,
                    BoardHostility = State.boardDiscontent
                };

                int totalWeeks = ms.year * 48 + ms.month * 4 + ms.week;
                double activeBombSeverity = Ali.Mgt2.Simulation.CrossSystemBridge.GetDouble("ACTIVE_REVIEW_BOMB_SEVERITY", 0.0);
                if (activeBombSeverity > 0.0)
                {
                    // Review bomb angers the board
                    State.boardDiscontent = Mathf.Clamp(State.boardDiscontent + (float)(activeBombSeverity * 0.04), 0f, 100f);
                }

                // Check game releases shock
                double shockAlpha = 0.0;
                var releaseEvents = Ali.Mgt2.Simulation.CrossSystemBridge.DequeueAllEvents("GAME_RELEASED");
                foreach (var ev in releaseEvents)
                {
                    var parts = ev.Split('|');
                    if (parts.Length >= 4 && float.TryParse(parts[3], out float rScore))
                    {
                        shockAlpha += Ali.Mgt2.Simulation.StockDriftEngine.CalculateGameReleaseShock(rScore, 75.0, 1.1);
                    }
                }

                var synergy = new Ali.Mgt2.Simulation.CrossSystemSynergyContext
                {
                    ActiveReviewBombSeverity = activeBombSeverity,
                    OwnedEquityPercent = 0.0,
                    MacroPhase = Ali.Mgt2.Simulation.StockDriftEngine.GetMacroPhaseForWeek(totalWeeks)
                };

                var driftResult = Ali.Mgt2.Simulation.StockDriftEngine.SimulateWeeklyTick(stockState, totalWeeks, shockAlpha, 0.0, synergy, new System.Random(totalWeeks * 31));

                State.previousSharePrice = State.currentSharePrice;
                State.currentSharePrice = driftResult.NewSharePrice;
                State.priceTrendPercent = driftResult.PercentChange;
                State.marketValuation = (long)Math.Round(State.currentSharePrice * State.totalShares);

                // Poison pill duration check
                if (State.poisonPillActive && (ms.year * 12 + ms.month) >= State.poisonPillExpiryMonth)
                {
                    State.poisonPillActive = false;
                }

                // AI Competitor creeping takeover simulation
                SimulateHostileTakeoverTurn(ms);

                if (monthChanged)
                {
                    State.monthsSinceLastBoardMeeting++;
                    if (State.monthsSinceLastBoardMeeting >= 3)
                    {
                        EvaluateQuarterlyBoardMeeting(ms);
                    }
                }
            }
            else
            {
                // Keep private valuation updated for UI previews prior to IPO.
                State.marketValuation = CalculatePlayerValuation(ms);
                State.currentSharePrice = Math.Max(0.01, (double)State.marketValuation / (double)State.totalShares);
            }
        }

        #region Hostile Takeover & Shareholder Defense System

        public static void SimulateHostileTakeoverTurn(mainScript ms)
        {
            if (ms == null || StockData.Data == null || !StockData.Data.playerIsPublic) return;

            // Pick or verify hostile rival
            if (State.hostileRivalCompanyId <= 0)
            {
                // 35% chance every few ticks if there is available public float
                if (UnityEngine.Random.value < 0.25f && State.founderSharePercent < 90f)
                {
                    var pubs = GameObject.FindGameObjectsWithTag("Publisher")
                        .Select(go => go.GetComponent<publisherScript>())
                        .Where(p => p != null && p.isUnlocked && !p.isPlayer && !p.IsMyTochterfirma() && p.firmenwert >= 5000000L)
                        .OrderByDescending(p => p.firmenwert)
                        .ToList();

                    if (pubs.Count > 0)
                    {
                        var rival = pubs[UnityEngine.Random.Range(0, Math.Min(3, pubs.Count))];
                        State.hostileRivalCompanyId = rival.myID;
                        State.hostileRivalName = rival.GetName();
                        State.hostileRivalSharePercent = UnityEngine.Random.Range(2.0f, 5.0f);
                    }
                }
            }
            else
            {
                // Active rival attempts creeping accumulation if public float has room
                float maxRivalCap = 100f - State.founderSharePercent;
                if (State.hostileRivalSharePercent < maxRivalCap - 1f && !State.poisonPillActive)
                {
                    // Rival buys 1.0% to 2.5% every few weeks
                    if (UnityEngine.Random.value < 0.35f)
                    {
                        float increment = UnityEngine.Random.Range(1.0f, 2.5f);
                        float oldShare = State.hostileRivalSharePercent;
                        State.hostileRivalSharePercent = Mathf.Clamp(State.hostileRivalSharePercent + increment, 0f, maxRivalCap);

                        // If rival crossed 20% threshold, trigger emergency alert
                        if (oldShare < 20f && State.hostileRivalSharePercent >= 20f)
                        {
                            State.boardDiscontent = Mathf.Clamp(State.boardDiscontent + 20f, 0f, 100f);
                            string alert = $"<color=red><b>{StockMarketLabels.TakeoverThreatTitle}</b></color>\n\n" +
                                $"{State.hostileRivalName} has acquired {State.hostileRivalSharePercent:F1}% of studio shares!\n" +
                                "The Board of Directors strongly urges activating the Poison Pill Defense immediately.";

                            var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                            guiMain?.MessageBox(alert, false);

                            Ali.Mgt2.Simulation.CrossSystemBridge.EnqueueEvent("CORPORATE_EVENT",
                                $"@MarketInsider: عاجل: شركة {State.hostileRivalName} تستحوذ على أكثر من 20% من أسهم استوديوهاتنا في خطوة عدائية مقلقة! #TakeoverDefense");
                        }
                    }
                }
            }
        }

        public static bool ExecuteShareBuyback(mainScript ms, GUI_Main guiMain, out string message)
        {
            if (ms == null || StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                message = "Not public.";
                return false;
            }

            if (State.founderSharePercent >= 95f)
            {
                message = "Founder already owns max shares (95%).";
                guiMain?.MessageBox(message, false);
                return false;
            }

            double cost = (State.totalShares * 0.05) * State.currentSharePrice * 1.03;
            long buybackCost = (long)Math.Round(cost);

            if (ms.money < buybackCost)
            {
                message = $"Insufficient funds. Required: {ms.GetMoney(buybackCost, true)}";
                guiMain?.MessageBox(message, false);
                return false;
            }

            ms.Pay(buybackCost, FinanceCategory);
            State.financialAdjustmentThisTick -= buybackCost;

            State.founderSharePercent = Mathf.Clamp(State.founderSharePercent + 5f, 0f, 100f);
            StockData.Data.playerPublicShare = Mathf.Clamp(StockData.Data.playerPublicShare - 0.05f, 0f, 1f);

            if (State.hostileRivalSharePercent > 0f)
            {
                State.hostileRivalSharePercent = Mathf.Max(0f, State.hostileRivalSharePercent - 2.5f);
            }

            State.investorConfidence = Mathf.Clamp(State.investorConfidence + 6f, 0f, 100f);
            State.boardDiscontent = Mathf.Clamp(State.boardDiscontent - 8f, 0f, 100f);
            State.currentSharePrice = Math.Round(State.currentSharePrice * 1.04, 2);
            State.marketValuation = (long)Math.Round(State.currentSharePrice * State.totalShares);

            message = StockMarketLabels.BuybackSuccess;
            guiMain?.MessageBox($"<color=#1E8C3A><b>{StockMarketLabels.BuybackSuccess}</b></color>\n\n" +
                $"{StockMarketLabels.FounderStake} {State.founderSharePercent:F1}%\n" +
                $"{StockMarketLabels.BuybackCost} {ms.GetMoney(buybackCost, true)}", false);

            Ali.Mgt2.Simulation.CrossSystemBridge.EnqueueEvent("CORPORATE_EVENT",
                $"@WallStreetHerald: الاستوديو ينفذ بنجاح برنامج إعادة شراء أسهم بقيمة {ms.GetMoney(buybackCost, true)} لتعزيز سيطرة المؤسس وطمأنة المستثمرين!");

            return true;
        }

        public static bool ExecutePoisonPillDefense(mainScript ms, GUI_Main guiMain, out string message)
        {
            if (ms == null || StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                message = "Not public.";
                return false;
            }

            if (State.hostileRivalSharePercent < 20f)
            {
                message = "Poison Pill requires a hostile acquirer owning at least 20%.";
                guiMain?.MessageBox(message, false);
                return false;
            }

            if (State.poisonPillActive)
            {
                message = "Poison Pill is already active.";
                guiMain?.MessageBox(message, false);
                return false;
            }

            var targetStock = new Ali.Mgt2.Simulation.CompanyStockState
            {
                CompanyId = "PLAYER",
                SharePrice = State.currentSharePrice,
                TotalShares = State.totalShares,
                BoardHostility = 80.0,
                IndustryRelations = 60.0
            };

            var defense = Ali.Mgt2.Simulation.StockDriftEngine.EvaluateHostileTakeoverDefense(targetStock, (double)State.hostileRivalSharePercent, 0.0, 0.0);

            State.poisonPillActive = true;
            State.poisonPillExpiryMonth = (ms.year * 12 + ms.month) + 12;

            float oldRivalShare = State.hostileRivalSharePercent;
            State.hostileRivalSharePercent = (float)defense.PostDilutionPlayerOwnershipPercent;
            State.totalShares = defense.NewTotalShares;
            State.currentSharePrice = targetStock.SharePrice;
            State.marketValuation = (long)Math.Round(State.currentSharePrice * State.totalShares);

            State.founderSharePercent = Mathf.Clamp(State.founderSharePercent * 1.08f, 51f, 95f);
            StockData.Data.playerPublicShare = Mathf.Clamp((100f - State.founderSharePercent) / 100f, 0.05f, 0.49f);
            State.boardDiscontent = Mathf.Clamp(State.boardDiscontent - 20f, 0f, 100f);

            message = StockMarketLabels.PoisonPillSuccess;
            guiMain?.MessageBox($"<color=#1E8C3A><b>{StockMarketLabels.PoisonPillSuccess}</b></color>\n\n" +
                $"{State.hostileRivalName} stake diluted from {oldRivalShare:F1}% down to {State.hostileRivalSharePercent:F1}%!\n" +
                $"Hostile takeover averted for 12 months.", false);

            Ali.Mgt2.Simulation.CrossSystemBridge.EnqueueEvent("CORPORATE_EVENT",
                $"@FinancialTimes: مجلس إدارة الاستوديو يفعل بنجاح خطة حقوق المساهمين (حبة السم) لإحباط محاولة استحواذ عدائية من {State.hostileRivalName}!");

            return true;
        }

        #endregion

        /// <summary>
        /// Returns a clean status string indicating whether the studio is Private or Public.
        /// </summary>
        public static string GetIpoStatusText()
        {
            if (StockData.Data != null && StockData.Data.playerIsPublic)
            {
                return string.Format("Public ({0:P0} Free Float)", StockData.Data.playerPublicShare);
            }
            return "Private Studio";
        }

        /// <summary>
        /// Returns formatted current Share Price and Market Cap text for UI display.
        /// </summary>
        public static string GetSharePriceAndMarketCapText(mainScript ms)
        {
            long marketCap = (StockData.Data != null && StockData.Data.playerIsPublic)
                ? State.marketValuation
                : CalculatePlayerValuation(ms);
            double sharePrice = Math.Max(0.01, (double)marketCap / (double)State.totalShares);
            return string.Format("Share Price: {0:C2} | Market Cap: {1:N0}", sharePrice, marketCap);
        }

        /// <summary>
        /// Returns formatted Current Quarter Target vs Actual Quarterly Earnings text for UI display.
        /// </summary>
        public static string GetQuarterlyProgressText()
        {
            if (StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                return "Quarterly Target: N/A (Studio is Private)";
            }

            long target = Math.Max(1L, State.quarterlyRevenueTarget);
            double pct = ((double)State.currentQuarterRevenue / (double)target) * 100.0;
            return string.Format(
                "Quarter Earnings: {0:N0} / {1:N0} ({2:F1}%) | Months to Meeting: {3}",
                State.currentQuarterRevenue,
                State.quarterlyRevenueTarget,
                pct,
                Math.Max(0, 3 - State.monthsSinceLastBoardMeeting));
        }

        /// <summary>
        /// Computes the discrete BoardSentiment enum from discontent and confidence levels.
        /// </summary>
        public static BoardSentiment GetBoardSentiment()
        {
            if (State.boardDiscontent > HighDiscontentThreshold)
            {
                return BoardSentiment.Hostile;
            }
            if (State.boardDiscontent >= 45f)
            {
                return BoardSentiment.Demanding;
            }
            if (State.boardDiscontent >= 20f || State.investorConfidence < 50f)
            {
                return BoardSentiment.Cautious;
            }
            return BoardSentiment.Satisfied;
        }

        /// <summary>
        /// Returns a human-readable Board Sentiment status line for UI panels.
        /// </summary>
        public static string GetBoardSentimentText()
        {
            if (StockData.Data == null || !StockData.Data.playerIsPublic)
            {
                return "Board Sentiment: No Board (Private)";
            }

            BoardSentiment sentiment = GetBoardSentiment();
            string restrictionNote = State.spendingRestrictionActive ? " [SPENDING RESTRICTED]" : string.Empty;
            return string.Format(
                "Board Sentiment: {0} (Discontent: {1:F0}% | Confidence: {2:F0}%){3}",
                sentiment,
                State.boardDiscontent,
                State.investorConfidence,
                restrictionNote);
        }

        /// <summary>
        /// Builds a complete multi-line summary for UI tooltips or the Stock Market overview screen.
        /// </summary>
        public static string GetBoardReportSummary(mainScript ms)
        {
            return string.Concat(
                GetIpoStatusText(), "\n",
                GetSharePriceAndMarketCapText(ms), "\n",
                GetQuarterlyProgressText(), "\n",
                GetBoardSentimentText());
        }

        /// <summary>
        /// Sums the tracked 48-week (12-month) net revenue history.
        /// </summary>
        public static long GetTrailing12MonthNetRevenue()
        {
            long sum = 0L;
            List<long> history = State.weeklyNetRevenueHistory;
            for (int i = 0; i < history.Count; i++)
            {
                sum += history[i];
            }
            return sum;
        }

        /// <summary>
        /// Calculates dynamic quarterly revenue target: +5% to +15% growth expectation
        /// anchored by recent performance and market valuation.
        /// </summary>
        private static long ComputeNextQuarterlyTarget(long valuation, long lastQuarterActual, int consecutiveMet)
        {
            // Base expectation: 5% of market cap per quarter as minimum baseline, blended with last quarter.
            long valuationAnchor = Math.Max(50000L, (long)Math.Round((double)valuation * 0.05));
            long performanceAnchor = lastQuarterActual > 0L ? lastQuarterActual : valuationAnchor;
            long baseTarget = (long)Math.Round((valuationAnchor * 0.45) + (performanceAnchor * 0.55));

            // Growth expectation ranges from +5% to +15% depending on consecutive successes and confidence.
            float confidenceNormalized = Mathf.Clamp(State.investorConfidence, 0f, 100f) / 100f;
            float streakFactor = Mathf.Clamp(consecutiveMet / 4f, 0f, 1f);
            double expectedGrowthRate = 0.05 + (0.06 * confidenceNormalized) + (0.04 * streakFactor);
            expectedGrowthRate = Math.Max(0.05, Math.Min(0.15, expectedGrowthRate));

            long target = (long)Math.Round((double)baseTarget * (1.0 + expectedGrowthRate));
            return Math.Max(50000L, target);
        }

        private static long CalculateRecommendedDividend(long quarterNetRevenue)
        {
            if (quarterNetRevenue <= 0L || StockData.Data == null)
            {
                return 0L;
            }
            float publicShare = Mathf.Clamp(StockData.Data.playerPublicShare, MinIpoSharePercentage, MaxIpoSharePercentage);
            // 20% payout ratio scaled by public ownership share.
            long dividend = (long)Math.Round((double)quarterNetRevenue * 0.20 * (double)publicShare);
            return Math.Max(5000L, dividend);
        }

        private static long CalculateEmergencyDividend(mainScript ms)
        {
            float publicShare = StockData.Data != null
                ? Mathf.Clamp(StockData.Data.playerPublicShare, MinIpoSharePercentage, MaxIpoSharePercentage)
                : MinIpoSharePercentage;
            long cashBased = ms != null && ms.money > 0L
                ? (long)Math.Round((double)ms.money * 0.12 * (double)publicShare)
                : 25000L;
            long capBased = (long)Math.Round((double)State.marketValuation * 0.015 * (double)publicShare);
            return Math.Max(25000L, Math.Max(cashBased, capBased));
        }

        private static void EnsureReflectionInitialized()
        {
            if (_reflectionInitialized)
            {
                return;
            }
            _reflectionInitialized = true;

            Type msType = typeof(mainScript);
            _miGetCompanyValuation = AccessTools.Method(msType, "GetCompanyValuation");
            _fiFans = AccessTools.Field(msType, "fans") ?? AccessTools.Field(msType, "fans_b");
            _fiStudioRating = AccessTools.Field(msType, "studio_bewertung") ?? AccessTools.Field(msType, "reputation");
            _fiGamesScript = AccessTools.Field(msType, "games_");
            _fiGuiMain = AccessTools.Field(msType, "guiMain_");
            _miMessageBox = AccessTools.Method(typeof(GUI_Main), "MessageBox", new[] { typeof(string), typeof(bool) });
        }

        private static long TryGetEngineCompanyValuation(mainScript ms)
        {
            if (_miGetCompanyValuation == null || ms == null)
            {
                return 0L;
            }

            try
            {
                object result = _miGetCompanyValuation.Invoke(ms, null);
                if (result is long lVal)
                {
                    return Math.Max(0L, lVal);
                }
                if (result is int iVal)
                {
                    return Math.Max(0L, (long)iVal);
                }
            }
            catch
            {
                // Ignore if engine method signature differs.
            }
            return 0L;
        }

        private static double EstimateOwnedIpAndGamesValue(mainScript ms)
        {
            if (_fiGamesScript == null || ms == null)
            {
                return 0.0;
            }

            try
            {
                object gamesObj = _fiGamesScript.GetValue(ms);
                if (gamesObj == null)
                {
                    return 0.0;
                }

                if (_fiArrayGamesScripts == null)
                {
                    _fiArrayGamesScripts = AccessTools.Field(gamesObj.GetType(), "arrayGamesScripts");
                }

                if (_fiArrayGamesScripts == null)
                {
                    return 0.0;
                }

                IEnumerable list = _fiArrayGamesScripts.GetValue(gamesObj) as IEnumerable;
                if (list == null)
                {
                    return 0.0;
                }

                double totalValue = 0.0;
                int counted = 0;

                foreach (object gameScript in list)
                {
                    if (gameScript == null)
                    {
                        continue;
                    }

                    Type gType = gameScript.GetType();
                    bool isPlayerOwner = ReadIntField(gType, gameScript, "ownerID", -1) == 0 ||
                                         ReadBoolField(gType, gameScript, "inBesitz", false);
                    if (!isPlayerOwner)
                    {
                        continue;
                    }

                    bool onMarket = ReadBoolField(gType, gameScript, "isOnMarket", false);
                    int reviewTotal = ReadIntField(gType, gameScript, "reviewTotal", 50);
                    long sellsTotal = ReadLongField(gType, gameScript, "sellsTotal", 0L);

                    // Each owned IP carries base value scaled by review quality and active market status.
                    double qualityMultiplier = Math.Max(0.25, (double)reviewTotal / 70.0);
                    double activeBonus = onMarket ? 1.5 : 0.6;
                    double salesFactor = Math.Min(2500000.0, (double)sellsTotal * 0.75);
                    totalValue += (75000.0 * qualityMultiplier * activeBonus) + (salesFactor * 0.25);
                    counted++;
                }

                return totalValue;
            }
            catch
            {
                return 0.0;
            }
        }

        private static double EstimateReputationAndFansValue(mainScript ms)
        {
            if (ms == null)
            {
                return 0.0;
            }

            double value = 0.0;
            try
            {
                if (ms.genres_ != null)
                {
                    int fans = ms.genres_.GetAmountFans();
                    if (fans > 0)
                    {
                        value += (double)fans * 150.0;
                    }
                }

                if (_fiStudioRating != null)
                {
                    object repObj = _fiStudioRating.GetValue(ms);
                    if (repObj is float repFloat && repFloat > 0f)
                    {
                        value += (double)repFloat * 5000.0;
                    }
                    else if (repObj is int repInt && repInt > 0)
                    {
                        value += (double)repInt * 5000.0;
                    }
                }
            }
            catch
            {
                // Fallback safely if fields differ across game builds.
            }
            return value;
        }

        private static int ReadIntField(Type t, object instance, string fieldName, int fallback)
        {
            FieldInfo fi = AccessTools.Field(t, fieldName);
            if (fi != null && fi.GetValue(instance) is int val)
            {
                return val;
            }
            return fallback;
        }

        private static long ReadLongField(Type t, object instance, string fieldName, long fallback)
        {
            FieldInfo fi = AccessTools.Field(t, fieldName);
            if (fi != null)
            {
                object raw = fi.GetValue(instance);
                if (raw is long lVal) return lVal;
                if (raw is int iVal) return iVal;
            }
            return fallback;
        }

        private static bool ReadBoolField(Type t, object instance, string fieldName, bool fallback)
        {
            FieldInfo fi = AccessTools.Field(t, fieldName);
            if (fi != null && fi.GetValue(instance) is bool val)
            {
                return val;
            }
            return fallback;
        }

        private static void DispatchNotification(mainScript ms, string title, string message, bool isWarning)
        {
            try
            {
                OnBoardNotification?.Invoke(title + ": " + message, isWarning);
            }
            catch (Exception ex)
            {
                LogWarning("Error in OnBoardNotification listener: " + ex.Message);
            }

            try
            {
                if (ms != null && _fiGuiMain != null && _miMessageBox != null)
                {
                    object guiMain = _fiGuiMain.GetValue(ms);
                    if (guiMain != null)
                    {
                        _miMessageBox.Invoke(guiMain, new object[] { "[" + title + "]\n" + message, false });
                    }
                }
            }
            catch
            {
                // Avoid interrupting simulation if UI popup fails.
            }
        }

        private static void LogInfo(string message)
        {
            if (StockMarketPlugin.Log != null)
            {
                StockMarketPlugin.Log.LogInfo("[IPO] " + message);
            }
        }

        private static void LogWarning(string message)
        {
            if (StockMarketPlugin.Log != null)
            {
                StockMarketPlugin.Log.LogWarning("[IPO] " + message);
            }
        }
    }

    /// <summary>
    /// Harmony hook on mainScript.WochenUpdates (void return, 0 arguments) to avoid 32-bit Mono long-return corruption.
    /// </summary>
    [HarmonyPatch(typeof(mainScript), nameof(mainScript.WochenUpdates))]
    internal static class MainScriptWeeklyIpoPatch
    {
        [HarmonyPostfix]
        private static void Postfix(mainScript __instance)
        {
            PlayerIpoManager.OnWeeklyTick(__instance);
        }
    }
}
