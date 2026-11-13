using System;
using System.Collections.Generic;

namespace MGT2Mod.PublishingDeals
{
    public enum DealTier
    {
        IndiePrototype = 0,
        MidTierProject = 1,
        MajorBlockbuster = 2
    }

    [Serializable]
    public class StudioPitch
    {
        public int pitchId;
        public int developerId;
        public string developerName;
        public string gameTitle;
        public int genre;
        public int subgenre;
        public DealTier tier;
        public long requestedBudget;
        public float proposedRoyaltySplit; // Developer's cut % (e.g. 70%)
        public int devWeeks;
        public float estimatedQuality;     // Expected base quality (40-90)
        public int expirationWeeks = 8;

        // Dynamic Negotiation state
        public int studioPatience = 4;
        public int maxPatience = 4;
        public float currentDevCutOffer = 70f;
        public float currentPubCutOffer = 30f;
        public long currentBudgetOffer;
        public bool isExclusiveDemanded = false;
        public bool hasCounterOffer = false;
        public bool agreementReached = false;
        public bool negotiationFailed = false;
        public int negotiationRound = 0;
        public string lastCounterReason = "";
    }

    [Serializable]
    public class ActiveSecondPartyDeal
    {
        public int dealId;
        public int developerId;
        public string developerName;
        public string gameTitle;
        public int genre;
        public int subgenre;
        public long fundedBudget;
        public float playerRoyaltySplit;   // Player's cut % (e.g. 50%)
        public int weeksLeft;
        public int totalDevWeeks;
        public int exclusivePlatformId = -1; // -1 = multiplatform, >=0 = player's console
        public bool isPermanentExclusive = false;
        public bool isReleased = false;
        public int reviewScore = 0;
        public long totalRevenueEarned = 0L;
        public int unitsSold = 0;
        public bool isHit = false;
        public bool isFlop = false;
        public int gameId = -1;
    }

    [Serializable]
    public class BiddingWarItem
    {
        public int biddingId;
        public int developerId;
        public string developerName;
        public string gameTitle;
        public int genre;
        public long currentHighestBid;
        public int leadingBidderId;        // -1 = Player, >= 0 = Competitor publisher ID
        public string leadingBidderName;
        public int targetPlatformId = -1;  // Targeted exclusive platform
        public int weeksLeft;
        public bool resolved = false;
    }

    [Serializable]
    public class PublishingDealsSave
    {
        public List<StudioPitch> currentPitches = new List<StudioPitch>();
        public List<ActiveSecondPartyDeal> activeDeals = new List<ActiveSecondPartyDeal>();
        public List<ActiveSecondPartyDeal> completedDeals = new List<ActiveSecondPartyDeal>();
        public List<BiddingWarItem> activeBiddingWars = new List<BiddingWarItem>();
        public int lastPitchGenerationWeek = -1;
        public int lastBiddingWarGenerationWeek = -1;
        public long lifetimeSecondPartyProfits = 0L;
    }
}
