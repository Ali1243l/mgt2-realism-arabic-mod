using System.Linq;
// GDD Point 10: Transmedia Adaptations (Anime Series, Hollywood Films) & Merchandising Ecosystem.
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
    public enum AdaptationType
    {
        None = 0,
        AnimeSeries = 1,      // Streaming Anime (e.g. Edgerunners / Arcane): High viral fan reach
        HollywoodMovie = 2    // Theatrical Blockbuster (e.g. Mario / TLOU): Massive box-office royalty + 4x Halo
    }

    [Serializable]
    public class GameTransmediaState
    {
        public int gameId;
        public AdaptationType activeAdaptation = AdaptationType.None;
        public float adaptationQuality = 0f; // 0 .. 100
        public int haloWeeksRemaining = 0;
        public int merchTier = 0; // 0 = None, 1 = Apparel, 2 = Action Figures, 3 = Collector Statues
        public long cumulativeMerchProfit = 0L;
        public long cumulativeBoxOfficeRoyalty = 0L;
    }

    [Serializable]
    public class TransmediaSaveContainer
    {
        public List<GameTransmediaState> items = new List<GameTransmediaState>();
    }

    [BepInPlugin("ali.mgt2.transmediamerch", "Transmedia & Merchandising Ecosystem", "1.0.0")]
    public class TransmediaMerchPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.transmediamerch").PatchAll(typeof(TransmediaMerchPlugin).Assembly);
            Log.LogInfo("Transmedia & Merchandising Ecosystem loaded (F4)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
                TransmediaWindow.Toggle();
        }
    }

    public static class TransmediaEngine
    {
        public static readonly Dictionary<int, GameTransmediaState> States = new Dictionary<int, GameTransmediaState>();

        public static GameTransmediaState GetOrCreate(gameScript g)
        {
            if (g == null) return new GameTransmediaState();
            if (!States.TryGetValue(g.myID, out var st))
            {
                st = new GameTransmediaState { gameId = g.myID };
                States[g.myID] = st;
            }
            return st;
        }

        /// <summary>
        /// Calculates the Transmedia Halo Multiplier in [1.0, 4.0].
        /// Successful series/films boost game sales by up to 400% (4.0x) while haloWeeksRemaining > 0.
        /// </summary>
        public static float CalculateHaloSalesMultiplier(GameTransmediaState st)
        {
            if (st == null || st.activeAdaptation == AdaptationType.None || st.haloWeeksRemaining <= 0)
                return 1.0f;

            if (st.adaptationQuality <= 50f)
                return 1.05f;

            float norm = Mathf.Clamp01((st.adaptationQuality - 50f) / 50f);
            float maxBoost = st.activeAdaptation == AdaptationType.HollywoodMovie ? 3.0f : 2.2f;
            return 1.0f + maxBoost * Mathf.Pow(norm, 1.30f);
        }

        public static bool ProduceAnimeSeries(mainScript ms, gameScript g, long cost)
        {
            if (ms == null || g == null || ms.money < cost) return false;
            var st = GetOrCreate(g);

            ms.Pay(cost, 1);
            st.activeAdaptation = AdaptationType.AnimeSeries;
            float baseQual = Mathf.Clamp(g.reviewTotal * 0.85f + 14f, 45f, 98f);
            st.adaptationQuality = baseQual;
            st.haloWeeksRemaining = 14;

            long streamingAdvance = (long)(cost * (0.85f + (baseQual / 110f)));
            st.cumulativeBoxOfficeRoyalty += streamingAdvance;
            ms.Earn(streamingAdvance, 1);
            g.AddHype(22f);
            ms.AddFans((int)Mathf.Clamp(baseQual * 180f, 2000f, 35000f), -1);
            return true;
        }

        public static bool ProduceHollywoodMovie(mainScript ms, gameScript g, long cost)
        {
            if (ms == null || g == null || ms.money < cost) return false;
            var st = GetOrCreate(g);

            ms.Pay(cost, 1);
            st.activeAdaptation = AdaptationType.HollywoodMovie;
            float baseQual = Mathf.Clamp(g.reviewTotal * 0.88f + 12f, 48f, 99f);
            st.adaptationQuality = baseQual;
            st.haloWeeksRemaining = 18;

            long boxOfficeShare = (long)(cost * (0.95f + (baseQual / 90f)));
            st.cumulativeBoxOfficeRoyalty += boxOfficeShare;
            ms.Earn(boxOfficeShare, 1);
            g.AddHype(35f);
            ms.AddFans((int)Mathf.Clamp(baseQual * 320f, 4000f, 65000f), -1);
            return true;
        }

        public static bool UpgradeMerchLine(mainScript ms, gameScript g, long cost)
        {
            if (ms == null || g == null || ms.money < cost) return false;
            var st = GetOrCreate(g);
            if (st.merchTier >= 3) return false;

            ms.Pay(cost, 1);
            st.merchTier++;
            return true;
        }

        public static void WeeklyGameTick(gameScript g, mainScript ms)
        {
            if (g == null || ms == null || !g.IsMyGame()) return;
            var st = GetOrCreate(g);

            float haloMult = CalculateHaloSalesMultiplier(st);
            if (st.haloWeeksRemaining > 0)
            {
                st.haloWeeksRemaining--;
                if (g.sellsPerWeek != null && g.sellsPerWeek.Length > 0 && g.sellsPerWeek[0] > 0)
                {
                    g.sellsPerWeek[0] = (int)(g.sellsPerWeek[0] * haloMult);
                }
                else if (!g.inDevelopment && g.sellsPerWeek != null && g.sellsPerWeek.Length > 0)
                {
                    // Revive dormant catalog sales during premiere window!
                    long revivedUnits = (long)(180f * haloMult * (Mathf.Max(50, g.reviewTotal) / 75f));
                    g.sellsPerWeek[0] += (int)revivedUnits;
                    g.sellsTotal += revivedUnits;
                }
            }

            // Passive Merchandise & Action Figures Profit
            if (st.merchTier > 0 && !g.inDevelopment)
            {
                float tierFactor = st.merchTier == 1 ? 0.14f : st.merchTier == 2 ? 0.32f : 0.58f;
                float reviewFactor = Mathf.Clamp(g.reviewTotal / 80f, 0.4f, 1.35f);
                long merchProfit = (long)(Math.Max(1500, (ms.genres_ != null ? ms.genres_.GetAmountFans() / 8 : 1000)) * tierFactor * reviewFactor * haloMult);
                if (merchProfit > 0)
                {
                    st.cumulativeMerchProfit += merchProfit;
                    ms.Earn(merchProfit, 1);
                }
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class TransmediaSellGamePatch
    {
        private static void Postfix(gameScript __instance)
        {
            try
            {
                var mainGo = GameObject.Find("Main");
                var ms = mainGo != null ? mainGo.GetComponent<mainScript>() : null;
                TransmediaEngine.WeeklyGameTick(__instance, ms);
            }
            catch (Exception e) { TransmediaMerchPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class TransmediaSavePatch
    {
        private static void Postfix(int i)
        {
            try
            {
                var container = new TransmediaSaveContainer { items = new List<GameTransmediaState>(TransmediaEngine.States.Values) };
                string dir = Path.Combine(Application.persistentDataPath, "MGT2Transmedia");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, $"slot_{i}.json"), JsonUtility.ToJson(container, true));
            }
            catch (Exception e) { TransmediaMerchPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class TransmediaLoadPatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "MGT2Transmedia", $"slot_{i}.json");
                if (!File.Exists(path)) return;
                var container = JsonUtility.FromJson<TransmediaSaveContainer>(File.ReadAllText(path));
                TransmediaEngine.States.Clear();
                if (container?.items != null)
                {
                    foreach (var s in container.items)
                        TransmediaEngine.States[s.gameId] = s;
                }
            }
            catch (Exception e) { TransmediaMerchPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(Menu_Fanshop), "Init")]
    internal static class Patch_Menu_Fanshop_Init
    {
        private const string ButtonName = "Btn_TransmediaStudio";

        private static void Postfix(Menu_Fanshop __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Bestellen") || b.name.Contains("Abbrechen") || b.name.Contains("Ok"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, 48f);

                var b = go.GetComponent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => TransmediaWindow.Toggle());
                }

                var clr = new Color(0.65f, 0.35f, 0.85f);
                foreach (var img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                foreach (var text in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    text.text = ar ? TransmediaLabels.Title : "Transmedia Studio (Films & Anime)";
            }
            catch (Exception e) { TransmediaMerchPlugin.Log.LogError(e); }
        }
    }

}