using System.Linq;
// GDD Point 11: Early Access Lifecycle, Crowdfunding Campaigns (Kickstarter) & Development Hell Risk.
// Built for MGT2Mod namespace, netstandard2.1, 32-bit Mono x86 safe.
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    [Serializable]
    public class GameEarlyAccessState
    {
        public int gameId;
        public bool crowdfundingLaunched = false;
        public long crowdfundingRaised = 0L;
        public bool inEarlyAccess = false;
        public bool graduatedToFullRelease = false;
        public float roadmapCompletionPct = 35f; // Starts at 35%, graduates at 100%
        public float backerTrustPct = 85f;       // 0 .. 100%
        public int weeksSinceLastUpdate = 0;
        public int totalEarlyAccessWeeks = 0;
    }

    [Serializable]
    public class EarlyAccessSaveContainer
    {
        public List<GameEarlyAccessState> items = new List<GameEarlyAccessState>();
    }

    [BepInPlugin("ali.mgt2.earlyaccess", "Early Access & Crowdfunding Engine", "1.0.0")]
    public class EarlyAccessPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.earlyaccess").PatchAll(typeof(EarlyAccessPlugin).Assembly);
            Log.LogInfo("Early Access & Crowdfunding Engine loaded (F3)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
                EarlyAccessWindow.Toggle();
        }
    }

    public static class EarlyAccessEngine
    {
        public static readonly Dictionary<int, GameEarlyAccessState> States = new Dictionary<int, GameEarlyAccessState>();

        public static GameEarlyAccessState GetOrCreate(gameScript g)
        {
            if (g == null) return new GameEarlyAccessState();
            if (!States.TryGetValue(g.myID, out var st))
            {
                st = new GameEarlyAccessState { gameId = g.myID };
                States[g.myID] = st;
            }
            return st;
        }

        /// <summary>
        /// Launches a non-dilutive Kickstarter Crowdfunding Campaign based on studio fan reach and game hype.
        /// </summary>
        public static long LaunchCrowdfundingCampaign(mainScript ms, gameScript g)
        {
            if (ms == null || g == null) return 0L;
            var st = GetOrCreate(g);
            if (st.crowdfundingLaunched) return 0L;

            st.crowdfundingLaunched = true;
            st.inEarlyAccess = true;
            st.roadmapCompletionPct = 35f;

            float fanLog = Mathf.Log10(Mathf.Max(100f, (ms.genres_ != null ? ms.genres_.GetAmountFans() : 500)));
            float hypeFactor = 0.55f + 0.012f * Mathf.Clamp(g.hype, 0f, 100f) + 0.22f * fanLog;
            long raised = (long)(220000f * hypeFactor);

            st.crowdfundingRaised = raised;
            ms.Earn(raised, 1);
            g.AddHype(15f);
            return raised;
        }

        /// <summary>
        /// Pushes a Major Early Access Roadmap Update (+25% completion, resets Development Hell timer).
        /// </summary>
        public static bool PushRoadmapUpdate(mainScript ms, gameScript g, long cost)
        {
            if (ms == null || g == null || ms.money < cost) return false;
            var st = GetOrCreate(g);
            if (st.graduatedToFullRelease) return false;

            ms.Pay(cost, 1);
            st.inEarlyAccess = true;
            st.weeksSinceLastUpdate = 0;
            st.roadmapCompletionPct = Mathf.Clamp(st.roadmapCompletionPct + 25f, 35f, 100f);
            st.backerTrustPct = Mathf.Clamp(st.backerTrustPct + 14f, 0f, 100f);
            g.AddHype(10f);
            return true;
        }

        /// <summary>
        /// Graduates an Early Access title (>= 85% roadmap completion) to Version 1.0 Full Launch!
        /// </summary>
        public static bool GraduateToVersion10(mainScript ms, gameScript g)
        {
            if (ms == null || g == null) return false;
            var st = GetOrCreate(g);
            if (st.graduatedToFullRelease || st.roadmapCompletionPct < 85f) return false;

            st.inEarlyAccess = false;
            st.graduatedToFullRelease = true;
            st.roadmapCompletionPct = 100f;
            st.backerTrustPct = Mathf.Clamp(st.backerTrustPct + 20f, 0f, 100f);

            g.AddHype(30f);
            long launchBonusRevenue = (long)(180000f * (st.backerTrustPct / 75f));
            ms.Earn(launchBonusRevenue, 1);
            return true;
        }

        public static void WeeklyGameTick(gameScript g, mainScript ms)
        {
            if (g == null || ms == null || !g.IsMyGame()) return;
            var st = GetOrCreate(g);

            if (!st.inEarlyAccess || st.graduatedToFullRelease) return;

            st.totalEarlyAccessWeeks++;
            st.weeksSinceLastUpdate++;

            // Development Hell Hazard: >8 weeks without a roadmap update erodes backer trust
            if (st.weeksSinceLastUpdate > 8)
            {
                st.backerTrustPct = Mathf.Max(5f, st.backerTrustPct - 4.5f);
                if (g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 && g.sellsPerWeek[0] > 0)
                {
                    g.sellsPerWeek[0] = (int)(g.sellsPerWeek[0] * 0.45f);
                }
            }
            else
            {
                // Healthy Early Access community word-of-mouth stream
                long eaBonus = (long)(24000f * (st.roadmapCompletionPct / 100f) * (st.backerTrustPct / 80f));
                ms.Earn(eaBonus, 1);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class EarlyAccessSellGamePatch
    {
        private static void Postfix(gameScript __instance)
        {
            try
            {
                var mainGo = GameObject.Find("Main");
                var ms = mainGo != null ? mainGo.GetComponent<mainScript>() : null;
                EarlyAccessEngine.WeeklyGameTick(__instance, ms);
            }
            catch (Exception e) { EarlyAccessPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class EarlyAccessSavePatch
    {
        private static void Postfix(int i)
        {
            try
            {
                var container = new EarlyAccessSaveContainer { items = new List<GameEarlyAccessState>(EarlyAccessEngine.States.Values) };
                string dir = Path.Combine(Application.persistentDataPath, "MGT2EarlyAccess");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, $"slot_{i}.json"), JsonUtility.ToJson(container, true));
            }
            catch (Exception e) { EarlyAccessPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class EarlyAccessLoadPatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "MGT2EarlyAccess", $"slot_{i}.json");
                if (!File.Exists(path)) return;
                var container = JsonUtility.FromJson<EarlyAccessSaveContainer>(File.ReadAllText(path));
                EarlyAccessEngine.States.Clear();
                if (container?.items != null)
                {
                    foreach (var s in container.items)
                        EarlyAccessEngine.States[s.gameId] = s;
                }
            }
            catch (Exception e) { EarlyAccessPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "OnEnable")]
    internal static class Patch_Menu_DevGame_EarlyAccess
    {
        private const string ButtonName = "Btn_EarlyAccessCrowdfund";

        private static void Postfix(Menu_DevGame __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name == "GoldenAutoTune" || (b.name != null && b.name.ToLower().Contains("auto")));
                if (btn == null)
                    btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                        .FirstOrDefault(b => b.name.Contains("Start") || b.name.Contains("Abbrechen"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, 44f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => EarlyAccessWindow.Toggle());
                }

                var clr = new Color(0.2f, 0.7f, 0.85f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                foreach (var text in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    text.text = ar ? EarlyAccessLabels.Title : "Early Access & Kickstarter";
            }
            catch (Exception e) { EarlyAccessPlugin.Log.LogError(e); }
        }
    }

}