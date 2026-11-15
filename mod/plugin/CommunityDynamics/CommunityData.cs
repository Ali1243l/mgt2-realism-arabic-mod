using System;
using System.Collections.Generic;

namespace MGT2Mod.CommunityDynamics
{
    [Serializable]
    public class SocialPost
    {
        public int id;
        public string authorHandle;
        public string content;
        public string gameName;
        public int category; // 0=All, 1=Studio, 2=Trend, 3=Outrage
        public int sentiment; // 0=Neutral, 1=Hyped, 2=Legendary, 3=Skeptical, 4=Outraged
        public int likes;
        public int retweets;
        public int year;
        public int week;
    }

    [Serializable]
    public class ActiveBullshotCampaign
    {
        public int gameId;
        public string gameName;
        public int strategy; // 0=CGI Bullshot, 1=Honest Showcase
        public float hypeAdded;
        public int preOrdersAdded;
        public bool isDeceptive;
    }

    [Serializable]
    public class StreamerCampaign
    {
        public int gameId;
        public string gameName;
        public int streamerTier; // 0=Mega, 1=Niche, 2=Speedrun
        public long cost;
        public int weeksActive;
        public int outcome; // 0=Pending, 1=ViralExplosion, 2=MemeMockery, 3=SolidPraise
    }

    [Serializable]
    public class ReviewBombState
    {
        public int gameId;
        public string gameName;
        public int weeksRemaining;
        public bool apologyIssued;
        public bool hotfixCommitted;
        public bool compensationGiven;
        public int initialNegativeSurge;
    }

    [Serializable]
    public class CommunityDynamicsSave
    {
        public List<SocialPost> posts = new List<SocialPost>();
        public List<ActiveBullshotCampaign> activeBullshots = new List<ActiveBullshotCampaign>();
        public List<StreamerCampaign> activeStreamers = new List<StreamerCampaign>();
        public List<ReviewBombState> activeReviewBombs = new List<ReviewBombState>();
        public float overallSentiment = 0.85f;
        public int hypePoints = 65;
        public int lastPostWeek = -1;
    }
}
