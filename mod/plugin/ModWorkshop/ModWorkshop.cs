using System.Linq;
// GDD Point 12: Official Mod Support, UGC Workshop & Viral Spinoff Acquisition (Dota / CS Mechanic).
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
    public class GameWorkshopState
    {
        public int gameId;
        public int sdkTier = 0; // 0 = No SDK, 1 = Script Hooks, 2 = Full Editor SDK, 3 = Cloud Workshop
        public int communityModsCount = 0;
        public bool viralModDiscovered = false;
        public bool spinoffAcquired = false;
        public long cumulativeSpinoffProfit = 0L;
    }

    [Serializable]
    public class ModWorkshopSaveContainer
    {
        public List<GameWorkshopState> items = new List<GameWorkshopState>();
    }

    [BepInPlugin("ali.mgt2.modworkshop", "Official Mod Support & UGC Workshop", "1.0.0")]
    public class ModWorkshopPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.modworkshop").PatchAll(typeof(ModWorkshopPlugin).Assembly);
            Log.LogInfo("Official Mod Support & UGC Workshop loaded (F2)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
                ModWorkshopWindow.Toggle();
        }
    }

    public static class ModWorkshopEngine
    {
        public static readonly Dictionary<int, GameWorkshopState> States = new Dictionary<int, GameWorkshopState>();

        public static GameWorkshopState GetOrCreate(gameScript g)
        {
            if (g == null) return new GameWorkshopState();
            if (!States.TryGetValue(g.myID, out var st))
            {
                st = new GameWorkshopState { gameId = g.myID };
                States[g.myID] = st;
            }
            return st;
        }

        /// <summary>
        /// Logarithmic Long-Tail Sales Multiplier (Skyrim / Minecraft / GTA V Effect).
        /// </summary>
        public static float CalculateEvergreenMultiplier(GameWorkshopState st)
        {
            if (st == null || st.sdkTier <= 0 || st.communityModsCount <= 0)
                return 1.0f;

            float boost = 1.0f + 0.75f * Mathf.Log(1f + st.communityModsCount / 120f);
            return Mathf.Clamp(boost, 1.0f, 3.40f);
        }

        public static bool UpgradeModSdk(mainScript ms, gameScript g, long cost)
        {
            if (ms == null || g == null || ms.money < cost) return false;
            var st = GetOrCreate(g);
            if (st.sdkTier >= 3) return false;

            ms.Pay(cost, 1);
            st.sdkTier++;
            st.communityModsCount += 45 * st.sdkTier;
            if (st.communityModsCount >= 180)
                st.viralModDiscovered = true;
            g.AddHype(10f);
            return true;
        }

        public static bool HostModdingContest(mainScript ms, gameScript g, long prizeCost)
        {
            if (ms == null || g == null || ms.money < prizeCost) return false;
            var st = GetOrCreate(g);
            if (st.sdkTier <= 0) st.sdkTier = 1;

            ms.Pay(prizeCost, 1);
            st.communityModsCount += 110;
            st.viralModDiscovered = true;
            g.AddHype(14f);
            ms.AddFans(1800, -1);
            return true;
        }

        /// <summary>
        /// Hires the creator of a viral Total-Conversion mod and acquires the commercial Spinoff IP (Dota / Counter-Strike style).
        /// </summary>
        public static bool AcquireViralModSpinoff(mainScript ms, gameScript g, long acquisitionCost)
        {
            if (ms == null || g == null || ms.money < acquisitionCost) return false;
            var st = GetOrCreate(g);
            if (!st.viralModDiscovered || st.spinoffAcquired) return false;

            ms.Pay(acquisitionCost, 1);
            st.spinoffAcquired = true;
            long launchBurst = (long)(acquisitionCost * 1.45f);
            st.cumulativeSpinoffProfit += launchBurst;
            ms.Earn(launchBurst, 1);
            ms.AddFans(6500, -1);
            return true;
        }

        public static void WeeklyGameTick(gameScript g, mainScript ms)
        {
            if (g == null || ms == null || !g.IsMyGame() || g.inDevelopment) return;
            var st = GetOrCreate(g);
            if (st.sdkTier <= 0) return;

            // Weekly organic community mod creation
            int newMods = st.sdkTier * (3 + Mathf.Clamp(g.reviewTotal / 20, 1, 5));
            st.communityModsCount += newMods;

            if (st.communityModsCount >= 200 && !st.viralModDiscovered)
            {
                st.viralModDiscovered = true;
            }

            float evergreenMult = CalculateEvergreenMultiplier(st);
            if (g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 && g.sellsPerWeek[0] > 0)
            {
                g.sellsPerWeek[0] = (int)(g.sellsPerWeek[0] * evergreenMult);
            }

            if (st.spinoffAcquired)
            {
                long weeklySpinoffRoyalty = (long)(22000f * evergreenMult);
                st.cumulativeSpinoffProfit += weeklySpinoffRoyalty;
                ms.Earn(weeklySpinoffRoyalty, 1);
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class ModWorkshopSellGamePatch
    {
        private static void Postfix(gameScript __instance)
        {
            try
            {
                var mainGo = GameObject.Find("Main");
                var ms = mainGo != null ? mainGo.GetComponent<mainScript>() : null;
                ModWorkshopEngine.WeeklyGameTick(__instance, ms);
            }
            catch (Exception e) { ModWorkshopPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class ModWorkshopSavePatch
    {
        private static void Postfix(int i)
        {
            try
            {
                var container = new ModWorkshopSaveContainer { items = new List<GameWorkshopState>(ModWorkshopEngine.States.Values) };
                string dir = Path.Combine(Application.persistentDataPath, "MGT2ModWorkshop");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, $"slot_{i}.json"), JsonUtility.ToJson(container, true));
            }
            catch (Exception e) { ModWorkshopPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class ModWorkshopLoadPatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "MGT2ModWorkshop", $"slot_{i}.json");
                if (!File.Exists(path)) return;
                var container = JsonUtility.FromJson<ModWorkshopSaveContainer>(File.ReadAllText(path));
                ModWorkshopEngine.States.Clear();
                if (container?.items != null)
                {
                    foreach (var s in container.items)
                        ModWorkshopEngine.States[s.gameId] = s;
                }
            }
            catch (Exception e) { ModWorkshopPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_UpdateSelectGame), "Init")]
    internal static class Patch_Menu_Update_ModWorkshop
    {
        private const string ButtonName = "Btn_ModWorkshop";

        private static void Postfix(Menu_Dev_UpdateSelectGame __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Search") || b.name.Contains("Close"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(-(src.rect.width + 10f), 0f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => ModWorkshopWindow.Toggle());
                }

                var clr = new Color(0.25f, 0.75f, 0.45f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                foreach (var text in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    text.text = ar ? ModWorkshopLabels.Title : "Modding SDK & Workshop";
            }
            catch (Exception e) { ModWorkshopPlugin.Log.LogError(e); }
        }
    }

}