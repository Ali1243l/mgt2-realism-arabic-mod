using System;
using System.Collections.Generic;

namespace Ali.Mgt2.Simulation
{
    internal static class MathUtil
    {
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Clamp(t, 0.0, 1.0);
        }

        public static double NextGaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            if (u1 < 1e-12) u1 = 1e-12;
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }

    public enum MacroCyclePhase
    {
        Expansion = 0,
        Bubble = 1,
        Saturation = 2,
        BearMarket = 3
    }

    public sealed class CrossSystemSynergyContext
    {
        public double OwnedEquityPercent { get; set; }
        public double ActiveReviewBombSeverity { get; set; }
        public double StudioTrustMetric { get; set; } = 50.0;
        public MacroCyclePhase MacroPhase { get; set; } = MacroCyclePhase.Expansion;
        public double MacroLiquidityMultiplier { get; set; } = 1.0;
    }

    public enum StudioArchetype
    {
        StarvingIndie,
        VeteranAuteur,
        CorporateGiant
    }

    public enum NegotiationStatus
    {
        Accepted,
        CounterOffer,
        Walkout
    }

    public enum ConsoleLifecycleStage
    {
        Launch,
        Growth,
        Peak,
        Decline
    }

    public sealed class PublisherProfile
    {
        public double CashReserve { get; set; }
        public int TotalFans { get; set; }
        public int AwardCount { get; set; }
        public double HistoricalHitRatio { get; set; }
        public double ConsoleMarketSharePercent { get; set; }
        public double Reputation { get; set; }
    }

    public sealed class TargetStudioProfile
    {
        public double Stars { get; set; }
        public double CashReserve { get; set; }
        public int AgeYears { get; set; }
        public int PastAwards { get; set; }
        public double RequestedBudget { get; set; }
        public double RequestedDevRoyaltyPercent { get; set; }
        public double CurrentPatience { get; set; } = 100.0;
        public double MaxPatience { get; set; } = 100.0;
        public double ExpectedGameRoi { get; set; } = 1.5;
    }

    public sealed class DealOffer
    {
        public double OfferedBudget { get; set; }
        public double OfferedDevRoyaltyPercent { get; set; }
        public double MarketingCommitment { get; set; }
        public bool RequireConsoleExclusivity { get; set; }
    }

    public sealed class NegotiationEvaluationResult
    {
        public NegotiationStatus Status { get; set; }
        public StudioArchetype Archetype { get; set; }
        public double OfferScore { get; set; }
        public double AcceptThreshold { get; set; }
        public double PatienceDelta { get; set; }
        public double RemainingPatience { get; set; }
        public DealOffer CounterOffer { get; set; }
        public string DominantReasonCode { get; set; }
    }

    public sealed class AiCompetitorBidder
    {
        public string PublisherId { get; set; }
        public double CashReserve { get; set; }
        public int CurrentExclusiveCount { get; set; }
        public double Aggressiveness { get; set; } = 1.0;
        public bool HasFolded { get; set; }
    }

    public sealed class BiddingWarDecision
    {
        public string PublisherId { get; set; }
        public bool StayInWar { get; set; }
        public double NextBidAmount { get; set; }
        public double MaxReservationBid { get; set; }
    }

    public static class PublishingNegotiationMath
    {
        public static StudioArchetype ClassifyArchetype(TargetStudioProfile studio)
        {
            if (studio.Stars <= 2.5) return StudioArchetype.StarvingIndie;
            if (studio.Stars <= 4.0) return StudioArchetype.VeteranAuteur;
            return StudioArchetype.CorporateGiant;
        }

        public static NegotiationEvaluationResult EvaluateAndRespond(
            PublisherProfile publisher,
            TargetStudioProfile studio,
            DealOffer offer,
            int roundNumber,
            CrossSystemSynergyContext synergy)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (studio == null) throw new ArgumentNullException(nameof(studio));
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            synergy = synergy ?? new CrossSystemSynergyContext();

            StudioArchetype archetype = ClassifyArchetype(studio);
            double offeredPubCut = MathUtil.Clamp(100.0 - offer.OfferedDevRoyaltyPercent, 0.0, 95.0);
            double deltaRoyalty = offer.OfferedDevRoyaltyPercent - studio.RequestedDevRoyaltyPercent;
            double safeReqBudget = Math.Max(studio.RequestedBudget, 1000.0);
            double budgetRatio = (offer.OfferedBudget - safeReqBudget) / safeReqBudget;
            double marketingRatio = offer.MarketingCommitment / safeReqBudget;

            double wR;
            double wB;
            double wE;
            double sBase;
            double acceptThreshold;

            switch (archetype)
            {
                case StudioArchetype.StarvingIndie:
                    sBase = 52.0;
                    wR = 0.75;
                    wB = 48.0;
                    wE = 14.0;
                    acceptThreshold = 66.0;
                    break;
                case StudioArchetype.VeteranAuteur:
                    sBase = 46.0;
                    wR = 1.25;
                    wB = 26.0;
                    wE = 30.0;
                    acceptThreshold = 74.0;
                    break;
                default:
                    sBase = 42.0;
                    wR = 1.15;
                    wB = 34.0;
                    wE = 24.0;
                    acceptThreshold = 78.0;
                    break;
            }

            double effectiveBudgetMod = MathUtil.Clamp(budgetRatio, -0.60, 1.00);
            double exclPenalty = 0.0;
            double marketShareNorm = MathUtil.Clamp(publisher.ConsoleMarketSharePercent / 100.0, 0.0, 1.0);

            if (offer.RequireConsoleExclusivity)
            {
                exclPenalty = (1.0 - marketShareNorm) * wE;
                if (archetype == StudioArchetype.VeteranAuteur && budgetRatio < 0.30)
                {
                    exclPenalty += (0.30 - Math.Max(budgetRatio, -0.30)) * 45.0;
                }
                if (archetype == StudioArchetype.CorporateGiant && publisher.ConsoleMarketSharePercent < 25.0)
                {
                    exclPenalty += (25.0 - publisher.ConsoleMarketSharePercent) * 0.9;
                }
            }

            double cashFactor = Math.Log10(Math.Max(publisher.CashReserve, 1000.0)) - 3.0;
            double fanFactor = Math.Log10(Math.Max(publisher.TotalFans, 10)) - 1.0;
            double hitFactor = MathUtil.Clamp(publisher.HistoricalHitRatio, 0.0, 1.0) * 10.0;
            double awardFactor = Math.Min(publisher.AwardCount * 1.2, 12.0);
            double prestige = (cashFactor * 1.8) + (fanFactor * 2.5) + hitFactor + awardFactor;
            if (archetype == StudioArchetype.StarvingIndie)
            {
                prestige *= 1.35;
            }

            if (archetype == StudioArchetype.CorporateGiant && publisher.TotalFans < 100000)
            {
                prestige -= (100000 - publisher.TotalFans) / 5000.0;
            }

            double pride = (studio.Stars * 4.5) + Math.Min(studio.AgeYears * 0.6, 10.0) + Math.Min(studio.PastAwards * 1.4, 14.0);

            double synergyBonus = 0.0;
            if (synergy.OwnedEquityPercent >= 51.0)
            {
                synergyBonus = 50.0;
                exclPenalty = 0.0;
            }
            else if (synergy.OwnedEquityPercent >= 20.0)
            {
                synergyBonus = 12.0;
                exclPenalty *= 0.65;
            }

            if (synergy.ActiveReviewBombSeverity > 15.0)
            {
                synergyBonus -= Math.Min(synergy.ActiveReviewBombSeverity * 0.25, 18.0);
            }
            if (synergy.MacroPhase == MacroCyclePhase.BearMarket && archetype == StudioArchetype.StarvingIndie)
            {
                synergyBonus += 8.0;
            }

            double marketingBonus = Math.Min(marketingRatio * 18.0, 14.0);
            double rawScore = sBase + (deltaRoyalty * wR) + (effectiveBudgetMod * wB) - exclPenalty + prestige - pride + marketingBonus + synergyBonus;

            if (archetype == StudioArchetype.StarvingIndie && offeredPubCut <= 45.0 && budgetRatio >= 0.0)
            {
                rawScore += 6.0;
            }

            double finalScore = MathUtil.Clamp(rawScore, 0.0, 100.0);

            double patienceLoss = CalculatePatienceLoss(archetype, offeredPubCut, finalScore, acceptThreshold, offer, budgetRatio, synergy.OwnedEquityPercent);
            double remainingPatience = MathUtil.Clamp(studio.CurrentPatience - patienceLoss, 0.0, studio.MaxPatience);

            if (finalScore >= acceptThreshold)
            {
                return new NegotiationEvaluationResult
                {
                    Status = NegotiationStatus.Accepted,
                    Archetype = archetype,
                    OfferScore = finalScore,
                    AcceptThreshold = acceptThreshold,
                    PatienceDelta = -patienceLoss,
                    RemainingPatience = remainingPatience,
                    CounterOffer = null,
                    DominantReasonCode = synergy.OwnedEquityPercent >= 51.0 ? "MAJORITY_SHAREHOLDER_LEVERAGE" : "DEAL_ACCEPTED"
                };
            }

            if (remainingPatience <= 0.0)
            {
                return new NegotiationEvaluationResult
                {
                    Status = NegotiationStatus.Walkout,
                    Archetype = archetype,
                    OfferScore = finalScore,
                    AcceptThreshold = acceptThreshold,
                    PatienceDelta = -patienceLoss,
                    RemainingPatience = 0.0,
                    CounterOffer = null,
                    DominantReasonCode = "PATIENCE_EXHAUSTED"
                };
            }

            DealOffer counter = BuildCounterOffer(archetype, studio, offer, roundNumber, remainingPatience, finalScore, acceptThreshold);

            return new NegotiationEvaluationResult
            {
                Status = NegotiationStatus.CounterOffer,
                Archetype = archetype,
                OfferScore = finalScore,
                AcceptThreshold = acceptThreshold,
                PatienceDelta = -patienceLoss,
                RemainingPatience = remainingPatience,
                CounterOffer = counter,
                DominantReasonCode = DetermineReasonCode(archetype, offer, budgetRatio, offeredPubCut, publisher)
            };
        }

        private static double CalculatePatienceLoss(
            StudioArchetype archetype,
            double offeredPubCut,
            double score,
            double acceptThreshold,
            DealOffer offer,
            double budgetRatio,
            double ownedEquity)
        {
            if (ownedEquity >= 51.0) return 4.0; // Majority owner preserves patience

            double deficit = Math.Max(0.0, acceptThreshold - score);
            // Scaled so 1 round normally consumes ~20 patience (1 pip out of 5)
            double baseLoss = 16.0 + (deficit * 0.15);

            if (archetype == StudioArchetype.VeteranAuteur && offeredPubCut > 40.0)
            {
                baseLoss += 8.0;
            }
            if (archetype == StudioArchetype.StarvingIndie && budgetRatio < -0.20)
            {
                baseLoss += 6.0;
            }
            if (offer.RequireConsoleExclusivity && budgetRatio < 0.15)
            {
                baseLoss += 5.0;
            }

            // Cap at 30.0 so no studio ever walks out instantly on round 1 with full patience
            return MathUtil.Clamp(baseLoss, 10.0, 30.0);
        }

        private static DealOffer BuildCounterOffer(
            StudioArchetype archetype,
            TargetStudioProfile studio,
            DealOffer playerOffer,
            int roundNumber,
            double remainingPatience,
            double score,
            double acceptThreshold)
        {
            double patienceRatio = MathUtil.Clamp(remainingPatience / Math.Max(studio.MaxPatience, 1.0), 0.0, 1.0);
            double concessionFactor = roundNumber <= 1 ? 0.25 : MathUtil.Lerp(0.10, 0.35, 1.0 - patienceRatio);
            double scoreGapRatio = MathUtil.Clamp((acceptThreshold - score) / 40.0, 0.05, 1.0);

            double idealDevCut = studio.RequestedDevRoyaltyPercent;
            double idealBudget = studio.RequestedBudget;
            double idealMarketing = playerOffer.MarketingCommitment;

            if (archetype == StudioArchetype.StarvingIndie)
            {
                double minAcceptableDevCut = 55.0;
                idealDevCut = Math.Max(minAcceptableDevCut, studio.RequestedDevRoyaltyPercent - (10.0 * concessionFactor));
                idealBudget = studio.RequestedBudget * (1.08 - (0.05 * concessionFactor));
                if (roundNumber >= 2)
                {
                    idealMarketing = Math.Max(playerOffer.MarketingCommitment, studio.RequestedBudget * 0.20);
                }
            }
            else if (archetype == StudioArchetype.VeteranAuteur)
            {
                double floorDevCut = 65.0;
                idealDevCut = Math.Max(floorDevCut, studio.RequestedDevRoyaltyPercent * (1.0 - (concessionFactor * 0.15)));
                double exclMult = playerOffer.RequireConsoleExclusivity ? 1.30 : 1.05;
                idealBudget = studio.RequestedBudget * exclMult;
                if (roundNumber >= 2)
                {
                    idealMarketing = Math.Max(playerOffer.MarketingCommitment, studio.RequestedBudget * 0.35);
                }
            }
            else
            {
                idealDevCut = MathUtil.Lerp(studio.RequestedDevRoyaltyPercent, playerOffer.OfferedDevRoyaltyPercent, concessionFactor * 0.6);
                idealBudget = studio.RequestedBudget * (1.0 + (scoreGapRatio * 0.25));
                idealMarketing = Math.Max(playerOffer.MarketingCommitment, studio.RequestedBudget * (roundNumber >= 2 ? 0.45 : 0.25));
            }

            double counterCut = MathUtil.Lerp(idealDevCut, playerOffer.OfferedDevRoyaltyPercent, concessionFactor * 0.5);
            double counterBudget = Math.Max(playerOffer.OfferedBudget, MathUtil.Lerp(idealBudget, playerOffer.OfferedBudget, concessionFactor * 0.4));

            return new DealOffer
            {
                OfferedDevRoyaltyPercent = Math.Round(MathUtil.Clamp(counterCut, 35.0, 90.0), 1),
                OfferedBudget = Math.Round(counterBudget, 0),
                MarketingCommitment = Math.Round(idealMarketing, 0),
                RequireConsoleExclusivity = playerOffer.RequireConsoleExclusivity
            };
        }

        private static string DetermineReasonCode(StudioArchetype archetype, DealOffer offer, double budgetRatio, double offeredPubCut, PublisherProfile publisher)
        {
            if (archetype == StudioArchetype.VeteranAuteur && offeredPubCut > 35.0) return "PUBLISHER_CUT_TOO_GREEDY";
            if (offer.RequireConsoleExclusivity && budgetRatio < 0.30 && archetype == StudioArchetype.VeteranAuteur) return "EXCLUSIVITY_REQUIRES_BUDGET_PREMIUM";
            if (archetype == StudioArchetype.CorporateGiant && publisher.TotalFans < 100000) return "INSUFFICIENT_FAN_REACH";
            if (budgetRatio < 0.0) return "UPFRONT_BUDGET_TOO_LOW";
            return "ROI_TERMS_MISMATCH";
        }

        public static BiddingWarDecision SimulateBiddingWarTurn(
            AiCompetitorBidder ai,
            double currentHighestBid,
            double baseGameValuation,
            ConsoleLifecycleStage consoleStage,
            MacroCyclePhase macroPhase)
        {
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            if (ai.HasFolded || ai.CashReserve <= currentHighestBid * 1.15)
            {
                return new BiddingWarDecision { PublisherId = ai.PublisherId, StayInWar = false, NextBidAmount = 0.0, MaxReservationBid = 0.0 };
            }

            double lifecycleMult;
            switch (consoleStage)
            {
                case ConsoleLifecycleStage.Launch: lifecycleMult = 1.45; break;
                case ConsoleLifecycleStage.Growth: lifecycleMult = 1.25; break;
                case ConsoleLifecycleStage.Peak: lifecycleMult = 1.00; break;
                default: lifecycleMult = 0.65; break;
            }

            double macroMult = macroPhase == MacroCyclePhase.Bubble ? 1.30 : (macroPhase == MacroCyclePhase.BearMarket ? 0.72 : 1.0);
            double portfolioSaturationPenalty = Math.Max(0.40, 1.0 - (ai.CurrentExclusiveCount * 0.08));
            double cashCeiling = ai.CashReserve * 0.32;
            double strategicMax = baseGameValuation * lifecycleMult * macroMult * portfolioSaturationPenalty * ai.Aggressiveness;
            double maxReservation = Math.Min(cashCeiling, strategicMax);

            double nextIncrement = Math.Max(baseGameValuation * 0.06, currentHighestBid * 0.08);
            double proposedBid = Math.Round(currentHighestBid + nextIncrement, 0);

            if (proposedBid > maxReservation)
            {
                return new BiddingWarDecision
                {
                    PublisherId = ai.PublisherId,
                    StayInWar = false,
                    NextBidAmount = 0.0,
                    MaxReservationBid = Math.Round(maxReservation, 0)
                };
            }

            return new BiddingWarDecision
            {
                PublisherId = ai.PublisherId,
                StayInWar = true,
                NextBidAmount = proposedBid,
                MaxReservationBid = Math.Round(maxReservation, 0)
            };
        }
    }

    public sealed class CompanyStockState
    {
        public string CompanyId { get; set; }
        public double SharePrice { get; set; }
        public long TotalShares { get; set; }
        public double FundamentalFairValue { get; set; }
        public double StockBeta { get; set; } = 1.0;
        public double BaseWeeklyVolatility { get; set; } = 0.035;
        public double MomentumEwma { get; set; }
        public double BoardHostility { get; set; }
        public double IndustryRelations { get; set; } = 50.0;
        public bool PoisonPillTriggered { get; set; }
    }

    public sealed class WeeklyStockUpdateResult
    {
        public double NewSharePrice { get; set; }
        public double PercentChange { get; set; }
        public double ReleaseAlphaShock { get; set; }
        public double MacroBetaContribution { get; set; }
        public double ScandalDrag { get; set; }
    }

    public sealed class TakeoverDefenseResult
    {
        public bool PoisonPillActivated { get; set; }
        public double DilutionMultiplier { get; set; }
        public long NewTotalShares { get; set; }
        public double PostDilutionPlayerOwnershipPercent { get; set; }
        public double ExtraCapitalRequiredForMajority { get; set; }
        public bool WhiteKnightIntervened { get; set; }
        public double WhiteKnightBidPricePerShare { get; set; }
    }

    public sealed class BoardAuditInput
    {
        public double QuarterlyProfitGrowthPercent { get; set; }
        public double AverageRecentReviewScore { get; set; }
        public double DividendPayoutRatio { get; set; }
        public double CashRunwayMonths { get; set; }
        public int ConsecutiveLowSentimentQuarters { get; set; }
    }

    public sealed class BoardAuditResult
    {
        public double BoardSentiment { get; set; }
        public int UpdatedConsecutiveLowQuarters { get; set; }
        public bool IssueFormalUltimatum { get; set; }
        public bool TriggerHostileOuster { get; set; }
    }

    public static class StockDriftEngine
    {
        public static MacroCyclePhase GetMacroPhaseForWeek(int totalGameWeeks)
        {
            int cycleWeek = ((totalGameWeeks % 208) + 208) % 208;
            int phaseIndex = cycleWeek / 52;
            return (MacroCyclePhase)MathUtil.ClampInt(phaseIndex, 0, 3);
        }

        public static double GetMacroDriftAndBetaMultiplier(MacroCyclePhase phase, out double weeklyMarketDrift)
        {
            switch (phase)
            {
                case MacroCyclePhase.Expansion:
                    weeklyMarketDrift = 0.0025;
                    return 1.05;
                case MacroCyclePhase.Bubble:
                    weeklyMarketDrift = 0.0055;
                    return 1.45;
                case MacroCyclePhase.Saturation:
                    weeklyMarketDrift = -0.0008;
                    return 0.90;
                default:
                    weeklyMarketDrift = -0.0045;
                    return 1.35;
            }
        }

        public static double CalculateGameReleaseShock(double reviewScore, double expectedScore, double budgetWeight)
        {
            double surprise = reviewScore - expectedScore;
            double normalized = surprise / 25.0;
            double nonlinear = Math.Tanh(normalized) * 0.24;
            if (reviewScore >= 90.0 && surprise > 0.0)
            {
                nonlinear = Math.Max(nonlinear, 0.15 * MathUtil.Clamp(surprise / 15.0, 0.8, 1.35));
            }
            else if (reviewScore <= 48.0 && surprise < 0.0)
            {
                nonlinear = Math.Min(nonlinear, -0.25 * MathUtil.Clamp(-surprise / 20.0, 0.8, 1.4));
            }

            double materiality = MathUtil.Clamp(budgetWeight, 0.35, 1.50);
            return MathUtil.Clamp(nonlinear * materiality, -0.45, 0.40);
        }

        public static WeeklyStockUpdateResult SimulateWeeklyTick(
            CompanyStockState state,
            int currentWeek,
            double gameReleaseShockAlpha,
            double quarterlyEarningsSurpriseRatio,
            CrossSystemSynergyContext synergy,
            Random rng)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            synergy = synergy ?? new CrossSystemSynergyContext();

            MacroCyclePhase phase = GetMacroPhaseForWeek(currentWeek);
            synergy.MacroPhase = phase;
            double sectorBetaMult = GetMacroDriftAndBetaMultiplier(phase, out double marketDrift);

            double macroContribution = marketDrift * state.StockBeta * sectorBetaMult;
            double fairValueGap = (state.FundamentalFairValue - state.SharePrice) / Math.Max(state.SharePrice, 1.0);
            double meanReversion = MathUtil.Clamp(fairValueGap * 0.06, -0.04, 0.04);

            double earningsAlpha = MathUtil.Clamp(quarterlyEarningsSurpriseRatio * 0.12, -0.15, 0.15);
            double scandalDrag = -MathUtil.Clamp(synergy.ActiveReviewBombSeverity * 0.0018, 0.0, 0.18);

            double noise = MathUtil.NextGaussian(rng) * state.BaseWeeklyVolatility * sectorBetaMult;
            double momentumFollow = state.MomentumEwma * 0.25;

            double totalReturn = macroContribution + meanReversion + gameReleaseShockAlpha + earningsAlpha + scandalDrag + momentumFollow + noise;
            totalReturn = MathUtil.Clamp(totalReturn, -0.50, 0.60);

            double oldPrice = Math.Max(state.SharePrice, 0.01);
            double newPrice = Math.Max(0.10, Math.Round(oldPrice * (1.0 + totalReturn), 2));
            double actualPct = (newPrice - oldPrice) / oldPrice;

            state.SharePrice = newPrice;
            state.MomentumEwma = (state.MomentumEwma * 0.65) + (actualPct * 0.35);

            return new WeeklyStockUpdateResult
            {
                NewSharePrice = newPrice,
                PercentChange = actualPct * 100.0,
                ReleaseAlphaShock = gameReleaseShockAlpha,
                MacroBetaContribution = macroContribution,
                ScandalDrag = scandalDrag
            };
        }

        public static TakeoverDefenseResult EvaluateHostileTakeoverDefense(
            CompanyStockState target,
            double playerOwnershipPercent,
            double rivalWhiteKnightCash,
            double rivalWhiteKnightReputation)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            bool crossedThreshold = playerOwnershipPercent >= 20.0;
            bool activatePoisonPill = crossedThreshold && target.BoardHostility > 70.0 && !target.PoisonPillTriggered;

            double dilutionMult = 1.0;
            long newShares = target.TotalShares;
            double postOwnership = playerOwnershipPercent;

            if (activatePoisonPill)
            {
                double hostilityFactor = MathUtil.Clamp((target.BoardHostility - 70.0) / 30.0, 0.0, 1.0);
                dilutionMult = 1.30 + (0.45 * hostilityFactor);
                double playerShareCount = target.TotalShares * (playerOwnershipPercent / 100.0);
                newShares = (long)Math.Round(target.TotalShares * dilutionMult);
                postOwnership = (playerShareCount / Math.Max(newShares, 1L)) * 100.0;
                target.TotalShares = newShares;
                target.SharePrice = Math.Max(0.10, Math.Round(target.SharePrice / Math.Sqrt(dilutionMult), 2));
                target.PoisonPillTriggered = true;
            }

            double sharesNeededForMajority = Math.Max(0.0, (newShares * 0.501) - (newShares * (postOwnership / 100.0)));
            double takeoverPremium = playerOwnershipPercent >= 50.0 ? 1.0 : 1.25;
            double extraCost = sharesNeededForMajority * target.SharePrice * takeoverPremium;

            bool whiteKnight = false;
            double whiteKnightBid = 0.0;
            double remainingControlCost = newShares * 0.501 * target.SharePrice * 1.35;

            if (playerOwnershipPercent >= 20.0
                && playerOwnershipPercent < 50.0
                && target.IndustryRelations >= 60.0
                && rivalWhiteKnightReputation >= 65.0
                && rivalWhiteKnightCash >= remainingControlCost * 1.15)
            {
                whiteKnight = true;
                whiteKnightBid = Math.Round(target.SharePrice * (1.28 + (target.IndustryRelations * 0.002)), 2);
            }

            return new TakeoverDefenseResult
            {
                PoisonPillActivated = activatePoisonPill,
                DilutionMultiplier = dilutionMult,
                NewTotalShares = newShares,
                PostDilutionPlayerOwnershipPercent = Math.Round(postOwnership, 2),
                ExtraCapitalRequiredForMajority = Math.Round(extraCost, 0),
                WhiteKnightIntervened = whiteKnight,
                WhiteKnightBidPricePerShare = whiteKnightBid
            };
        }

        public static BoardAuditResult EvaluateBoardAudit(BoardAuditInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            double profitScore = MathUtil.Clamp(50.0 + (input.QuarterlyProfitGrowthPercent * 1.25), 0.0, 100.0);
            double reviewScore = MathUtil.Clamp((input.AverageRecentReviewScore - 35.0) * 1.6, 0.0, 100.0);
            double dividendScore = MathUtil.Clamp(input.DividendPayoutRatio * 200.0, 0.0, 100.0);
            double runwayScore = MathUtil.Clamp((input.CashRunwayMonths / 18.0) * 100.0, 0.0, 100.0);

            double sentiment = (profitScore * 0.40) + (reviewScore * 0.25) + (dividendScore * 0.15) + (runwayScore * 0.20);
            sentiment = MathUtil.Clamp(sentiment, 0.0, 100.0);

            int lowQuarters = sentiment < 25.0 ? input.ConsecutiveLowSentimentQuarters + 1 : 0;

            return new BoardAuditResult
            {
                BoardSentiment = Math.Round(sentiment, 1),
                UpdatedConsecutiveLowQuarters = lowQuarters,
                IssueFormalUltimatum = lowQuarters == 1,
                TriggerHostileOuster = lowQuarters >= 2
            };
        }
    }

    public enum InfluencerArchetype
    {
        MegaStreamer,
        HardcoreTechReviewer,
        SpeedrunnerChallenge
    }

    public sealed class ViralCrisisState
    {
        public double ExpectationDebt { get; set; }
        public double PromisedQualityScore { get; set; }
        public double ActiveRageSeverity { get; set; }
        public double StudioTrustMetric { get; set; } = 60.0;
        public double EvergreenDecayResistance { get; set; } = 1.0;
        public double DlcAttachRateBonus { get; set; }
    }

    public sealed class ReleaseContagionResult
    {
        public double RageSeverity { get; set; }
        public double InitialSalesMultiplier { get; set; }
        public double StockShockAlpha { get; set; }
        public double TrustDelta { get; set; }
    }

    public sealed class WeeklyCrisisDecayResult
    {
        public double RemainingRageSeverity { get; set; }
        public double SalesVelocityMultiplier { get; set; }
        public double SuppressedSalesPercent { get; set; }
    }

    public sealed class InfluencerImpactResult
    {
        public double QualityAmplifiedMultiplier { get; set; }
        public double CoreSentimentShift { get; set; }
        public double EvergreenTailFactor { get; set; }
        public double DlcAttachRateMultiplier { get; set; }
    }

    public static class ViralCrisisEngine
    {
        public static void RecordBullshotCampaign(
            ViralCrisisState state,
            double campaignDeceptionIntensity,
            double promisedQualityScore,
            double currentEstimatedQuality)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            double qualityGap = Math.Max(0.0, promisedQualityScore - currentEstimatedQuality);
            double debtAdded = MathUtil.Clamp(campaignDeceptionIntensity, 0.0, 10.0) * (1.0 + (qualityGap * 0.08));
            state.ExpectationDebt = MathUtil.Clamp(state.ExpectationDebt + debtAdded, 0.0, 100.0);
            state.PromisedQualityScore = Math.Max(state.PromisedQualityScore, promisedQualityScore);
        }

        public static ReleaseContagionResult EvaluateReleaseExpectationShock(
            ViralCrisisState state,
            double actualGameQuality,
            CrossSystemSynergyContext synergy)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            synergy = synergy ?? new CrossSystemSynergyContext();

            double gap = state.PromisedQualityScore - actualGameQuality;
            if (gap <= 2.0 || state.ExpectationDebt <= 0.1)
            {
                double trustGain = actualGameQuality >= 80.0 ? 3.5 : 1.0;
                state.StudioTrustMetric = MathUtil.Clamp(state.StudioTrustMetric + trustGain, 1.0, 100.0);
                state.ExpectationDebt *= 0.25;
                synergy.StudioTrustMetric = state.StudioTrustMetric;
                synergy.ActiveReviewBombSeverity = state.ActiveRageSeverity;
                return new ReleaseContagionResult
                {
                    RageSeverity = state.ActiveRageSeverity,
                    InitialSalesMultiplier = 1.0,
                    StockShockAlpha = 0.0,
                    TrustDelta = trustGain
                };
            }

            double gapScale = MathUtil.Clamp(gap / 15.0, 0.3, 2.5);
            double effectiveDebt = state.ExpectationDebt * gapScale;
            double rage = (effectiveDebt * effectiveDebt) / (Math.Max(state.StudioTrustMetric, 0.0) + 1.0);
            rage = MathUtil.Clamp(rage, 0.0, 100.0);

            state.ActiveRageSeverity = MathUtil.Clamp(state.ActiveRageSeverity + rage, 0.0, 100.0);
            double trustPenalty = -MathUtil.Clamp(rage * 0.32, 1.0, 30.0);
            state.StudioTrustMetric = MathUtil.Clamp(state.StudioTrustMetric + trustPenalty, 1.0, 100.0);
            state.ExpectationDebt = 0.0;

            double salesMult = ComputeSalesVelocityMultiplier(state.ActiveRageSeverity);
            double stockShock = -MathUtil.Clamp(state.ActiveRageSeverity * 0.0032, 0.0, 0.32);

            synergy.ActiveReviewBombSeverity = state.ActiveRageSeverity;
            synergy.StudioTrustMetric = state.StudioTrustMetric;

            return new ReleaseContagionResult
            {
                RageSeverity = Math.Round(state.ActiveRageSeverity, 2),
                InitialSalesMultiplier = Math.Round(salesMult, 4),
                StockShockAlpha = stockShock,
                TrustDelta = trustPenalty
            };
        }

        public static WeeklyCrisisDecayResult StepWeeklyCrisisMitigation(
            ViralCrisisState state,
            double crisisPrSpendRatio,
            bool hotfixDeployed,
            double hotfixQualityGain,
            CrossSystemSynergyContext synergy)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            synergy = synergy ?? new CrossSystemSynergyContext();

            double organicDecay = 0.06;
            double prDecay = MathUtil.Clamp(crisisPrSpendRatio, 0.0, 1.0) * 0.22;
            double patchDecay = hotfixDeployed ? MathUtil.Clamp(0.18 + (hotfixQualityGain * 0.015), 0.15, 0.55) : 0.0;

            double totalReductionFactor = MathUtil.Clamp(1.0 - (organicDecay + prDecay + patchDecay), 0.20, 0.96);
            state.ActiveRageSeverity *= totalReductionFactor;
            if (state.ActiveRageSeverity < 0.5)
            {
                state.ActiveRageSeverity = 0.0;
            }

            double salesMult = ComputeSalesVelocityMultiplier(state.ActiveRageSeverity);
            synergy.ActiveReviewBombSeverity = state.ActiveRageSeverity;

            return new WeeklyCrisisDecayResult
            {
                RemainingRageSeverity = Math.Round(state.ActiveRageSeverity, 2),
                SalesVelocityMultiplier = Math.Round(salesMult, 4),
                SuppressedSalesPercent = Math.Round((1.0 - salesMult) * 100.0, 2)
            };
        }

        public static double ComputeSalesVelocityMultiplier(double rageSeverity)
        {
            if (rageSeverity <= 0.0) return 1.0;
            double suppression = 0.85 * (1.0 - Math.Exp(-0.045 * rageSeverity));
            return MathUtil.Clamp(1.0 - suppression, 0.15, 1.0);
        }

        public static InfluencerImpactResult EvaluateInfluencerImpact(
            InfluencerArchetype archetype,
            double gameQualityScore,
            double subscriberCount,
            ViralCrisisState crisisState,
            Random rng)
        {
            if (crisisState == null) throw new ArgumentNullException(nameof(crisisState));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            double reachWeight = MathUtil.Clamp(Math.Log10(Math.Max(subscriberCount, 1000.0)) / 7.5, 0.2, 1.35);
            double qualityPivot = (gameQualityScore - 65.0) / 35.0;

            switch (archetype)
            {
                case InfluencerArchetype.MegaStreamer:
                    double varianceRoll = MathUtil.Clamp(MathUtil.NextGaussian(rng) * 0.45, -1.0, 1.0);
                    double directionalAmp = (qualityPivot * 2.1 + varianceRoll) * reachWeight;
                    double megaMult = MathUtil.Clamp(1.0 + MathUtil.Clamp(directionalAmp, -0.70, 3.0), 0.30, 3.0);
                    return new InfluencerImpactResult
                    {
                        QualityAmplifiedMultiplier = Math.Round(megaMult, 3),
                        CoreSentimentShift = Math.Round(directionalAmp * 4.5, 2),
                        EvergreenTailFactor = crisisState.EvergreenDecayResistance,
                        DlcAttachRateMultiplier = 1.0 + crisisState.DlcAttachRateBonus
                    };

                case InfluencerArchetype.HardcoreTechReviewer:
                    double sentimentShift = qualityPivot * 18.0 * reachWeight;
                    double tailBoost = gameQualityScore >= 75.0 ? 0.18 * reachWeight : -0.10 * reachWeight;
                    crisisState.EvergreenDecayResistance = MathUtil.Clamp(crisisState.EvergreenDecayResistance + tailBoost, 0.65, 2.20);
                    return new InfluencerImpactResult
                    {
                        QualityAmplifiedMultiplier = Math.Round(1.0 + (qualityPivot * 0.45 * reachWeight), 3),
                        CoreSentimentShift = Math.Round(sentimentShift, 2),
                        EvergreenTailFactor = Math.Round(crisisState.EvergreenDecayResistance, 3),
                        DlcAttachRateMultiplier = 1.0 + crisisState.DlcAttachRateBonus
                    };

                default:
                    double dlcBoost = MathUtil.Clamp((0.15 + Math.Max(0.0, qualityPivot) * 0.35) * reachWeight, 0.05, 0.65);
                    crisisState.DlcAttachRateBonus = MathUtil.Clamp(crisisState.DlcAttachRateBonus + dlcBoost, 0.0, 1.50);
                    return new InfluencerImpactResult
                    {
                        QualityAmplifiedMultiplier = Math.Round(1.0 + (0.20 * reachWeight), 3),
                        CoreSentimentShift = Math.Round(4.0 * reachWeight, 2),
                        EvergreenTailFactor = Math.Round(crisisState.EvergreenDecayResistance, 3),
                        DlcAttachRateMultiplier = Math.Round(1.0 + crisisState.DlcAttachRateBonus, 3)
                    };
            }
        }
    }

    /// <summary>
    /// Thread-safe, AppDomain-shared bridge connecting PublishingDeals, StockMarket, and CommunityDynamics.
    /// Uses AppDomain property storage to share primitives and BCL collections safely across assembly boundaries.
    /// </summary>
    public static class CrossSystemBridge
    {
        private const string Prefix = "MGT2_BRIDGE_";
        private static readonly object LockObj = new object();

        public static void SetDouble(string key, double value)
        {
            AppDomain.CurrentDomain.SetData(Prefix + key, value);
        }

        public static double GetDouble(string key, double fallback = 0.0)
        {
            object o = AppDomain.CurrentDomain.GetData(Prefix + key);
            if (o is double d) return d;
            if (o is float f) return (double)f;
            if (o is long l) return (double)l;
            if (o is int i) return (double)i;
            return fallback;
        }

        public static void SetLong(string key, long value)
        {
            AppDomain.CurrentDomain.SetData(Prefix + key, value);
        }

        public static long GetLong(string key, long fallback = 0L)
        {
            object o = AppDomain.CurrentDomain.GetData(Prefix + key);
            if (o is long l) return l;
            if (o is int i) return (long)i;
            return fallback;
        }

        public static void SetBool(string key, bool value)
        {
            AppDomain.CurrentDomain.SetData(Prefix + key, value);
        }

        public static bool GetBool(string key, bool fallback = false)
        {
            object o = AppDomain.CurrentDomain.GetData(Prefix + key);
            if (o is bool b) return b;
            return fallback;
        }

        public static void SetString(string key, string value)
        {
            AppDomain.CurrentDomain.SetData(Prefix + key, value ?? string.Empty);
        }

        public static string GetString(string key, string fallback = "")
        {
            object o = AppDomain.CurrentDomain.GetData(Prefix + key);
            if (o is string s) return s;
            return fallback;
        }

        public static void SetOwnership(int publisherId, float playerShare)
        {
            SetDouble("OWN_" + publisherId, (double)playerShare);
        }

        public static float GetOwnership(int publisherId)
        {
            return (float)GetDouble("OWN_" + publisherId, 0.0);
        }

        public static void EnqueueEvent(string channel, string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;
            lock (LockObj)
            {
                string key = Prefix + "QUEUE_" + channel;
                List<string> list = AppDomain.CurrentDomain.GetData(key) as List<string>;
                if (list == null)
                {
                    list = new List<string>();
                    AppDomain.CurrentDomain.SetData(key, list);
                }
                list.Add(payload);
            }
        }

        public static List<string> DequeueAllEvents(string channel)
        {
            lock (LockObj)
            {
                string key = Prefix + "QUEUE_" + channel;
                List<string> list = AppDomain.CurrentDomain.GetData(key) as List<string>;
                if (list == null || list.Count == 0) return new List<string>();
                List<string> copy = new List<string>(list);
                list.Clear();
                return copy;
            }
        }
    }
}
