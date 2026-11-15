// GDD Point 5: Smart Staff Auto-Assign, Exhaustion Relief, Bulk Operations & Executive ROI Dashboard (F10).
// Built for MGT2Mod namespace, netstandard2.1, 32-bit Mono safe.

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class AutoLabels
    {
        private static settingsScript _settings;
        internal static bool Arabic
        {
            get
            {
                if (_settings == null)
                {
                    var go = GameObject.Find("Main");
                    if (go != null) _settings = go.GetComponent<settingsScript>();
                }
                return _settings != null && _settings.language == 13;
            }
        }
        private static string Pick(string en, string ar) => Arabic ? ar : en;

        internal static string WindowTitle => Pick("Smart Automation & Executive Analytics (F10)", "(F10) \ufe94\ufef3\ufeac\ufef4\ufed4\ufee8\ufe98\ufedf\ufe8d \ufe95\ufefc\ufef4\ufee0\ufea4\ufe98\ufedf\ufe8d \ufe94\ufea3\ufeee\ufedf\ufeed \ufe94\ufef4\ufedb\ufeac\ufedf\ufe8d \ufe94\ufe98\ufee4\ufe97\ufef7\ufe8d");
        internal static string BtnAutoAssign => Pick("Smart Auto-Assign All Staff", "\ufe93\ufead\ufe8e\ufeec\ufee4\ufedf\ufe8d \ufe90\ufeb4\ufea3 \ufee6\ufef4\ufed4\ufec7\ufeee\ufee4\ufee0\ufedf \ufede\ufee3\ufe8e\ufeb7 \ufef2\ufedb\ufeab \ufeca\ufef3\ufeaf\ufeee\ufe97");
        internal static string BtnAutoRest => Pick("Swap & Rest Exhausted Staff", "\ufe8e\ufef4\ufe8b\ufe8e\ufed8\ufee0\ufe97 \ufee6\ufef4\ufedc\ufeec\ufee8\ufee4\ufedf\ufe8d \ufee6\ufef4\ufed4\ufec7\ufeee\ufee4\ufedf\ufe8d \ufe94\ufea3\ufe8d\ufead\ufe87");
        internal static string BtnBulkTrain => Pick("Boost Staff Morale & Training", "\ufee6\ufef4\ufea3\ufe8e\ufe98\ufee4\ufedf\ufe8d \ufee6\ufef4\ufed4\ufec7\ufeee\ufee4\ufee0\ufedf \ufef2\ufecb\ufe8e\ufee4\ufe9f \ufe90\ufef3\ufead\ufeaa\ufe97");
        internal static string HeaderEngines => Pick("Engine ROI & Genre Profitability Analytics", "\ufe8f\ufe8e\ufecc\ufedf\ufef7\ufe8d \ufe95\ufe8e\ufedb\ufeae\ufea4\ufee3 \ufea1\ufe8e\ufe91\ufead\ufe83\ufeed \ufead\ufe8e\ufee4\ufe9c\ufe98\ufeb3\ufefb\ufe8d \ufeaa\ufe8b\ufe8e\ufecb");
    }

    [BepInPlugin("ali.mgt2.smartautomation", "Smart Automation & Executive Analytics", "1.0.0")]
    public class SmartAutomationPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.smartautomation").PatchAll(typeof(SmartAutomationPlugin).Assembly);
            Log.LogInfo("Smart Automation & Executive Analytics loaded");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                SmartDashboardWindow.Toggle();
        }
    }

    public static class SmartAutomationEngine
    {
        // Room Type mapping in MGT2:
        // 1 = Development (Dev), 2 = Research, 3 = QA / Bugtesting, 4 = Graphics Studio,
        // 5 = Sound Studio, 6 = Marketing / PR, 10 = Hardware R&D / Workshop
        public static int GetIdealRoomTypeForStaff(characterScript c)
        {
            if (c == null) return 1;
            float[] scores = {
                Mathf.Max(c.s_gamedesign, c.s_programmieren), // index 0 -> Room 1 (Development)
                c.s_forschen,                                 // index 1 -> Room 2 (Research)
                c.s_gametests,                                // index 2 -> Room 3 (QA)
                c.s_grafik,                                   // index 3 -> Room 4 (Graphics)
                c.s_sound,                                    // index 4 -> Room 5 (Sound)
                c.s_pr,                                       // index 5 -> Room 6 (Marketing)
                c.s_technik                                   // index 6 -> Room 10 (Hardware/Tech)
            };
            int[] roomTypes = { 1, 2, 3, 4, 5, 6, 10 };
            int bestIdx = 0;
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > scores[bestIdx]) bestIdx = i;
            }
            return roomTypes[bestIdx];
        }

        /// <summary>
        /// Aligns each employee's profession/role index with their strongest skill and boosts department efficiency.
        /// </summary>
        public static int AutoOptimizeStaffRoles()
        {
            var chars = UnityEngine.Object.FindObjectsOfType<characterScript>();
            if (chars == null) return 0;

            int optimized = 0;
            foreach (var c in chars)
            {
                if (c == null) continue;
                float[] skills = {
                    c.s_gamedesign, c.s_programmieren, c.s_grafik, c.s_sound,
                    c.s_pr, c.s_gametests, c.s_technik, c.s_forschen
                };
                int bestRole = 0;
                for (int i = 1; i < skills.Length; i++)
                    if (skills[i] > skills[bestRole]) bestRole = i;

                if (c.beruf != bestRole)
                {
                    c.beruf = bestRole;
                    optimized++;
                }
            }
            return optimized;
        }

        /// <summary>
        /// Restores morale/motivation for exhausted employees so projects don't stall during crunch.
        /// </summary>
        public static int RelieveExhaustedStaff(mainScript ms)
        {
            var chars = UnityEngine.Object.FindObjectsOfType<characterScript>();
            if (chars == null || ms == null) return 0;

            int rested = 0;
            foreach (var c in chars)
            {
                if (c != null && c.s_motivation < 65f)
                {
                    c.AddMotivation(35f);
                    rested++;
                }
            }
            return rested;
        }
    }

    internal static class SmartDashboardWindow
    {
        private static GameObject _modalRoot;
        private static Transform _listContent;
        private static Text _styleText;
        private static GameObject _btnTemplate;
        private static Text _statusBanner;

        internal static void Toggle()
        {
            var go = GameObject.Find("CanvasInGameMenu");
            var gui = go != null ? go.GetComponent<GUI_Main>() : null;
            if (gui == null) return;

            if (_modalRoot != null && _modalRoot.activeSelf)
            {
                _modalRoot.SetActive(false);
                return;
            }
            if (_modalRoot == null) BuildModal(gui);
            if (_modalRoot != null)
            {
                _modalRoot.SetActive(true);
                _modalRoot.transform.SetAsLastSibling();
                RefreshAnalytics();
            }
        }

        private static void BuildModal(GUI_Main gui)
        {
            var refMenu = gui.uiObjects?.FirstOrDefault(o => o != null && o.transform.Find("WindowMain") != null);
            if (refMenu == null) return;
            var refWin = refMenu.transform.Find("WindowMain");
            _styleText = refWin.GetComponentInChildren<Text>(true);
            var refBtn = refWin.GetComponentInChildren<Button>(true);
            _btnTemplate = refBtn != null ? refBtn.gameObject : null;
            if (_styleText == null || _btnTemplate == null) return;

            _modalRoot = new GameObject("MGT2_SmartAutoModal", typeof(RectTransform), typeof(Image));
            _modalRoot.transform.SetParent(gui.transform, false);
            var rt = _modalRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            var win = new GameObject("WindowMain", typeof(RectTransform), typeof(Image));
            win.transform.SetParent(_modalRoot.transform, false);
            var wrt = win.GetComponent<RectTransform>();
            wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(820f, 520f);
            win.GetComponent<Image>().color = new Color(0.13f, 0.15f, 0.20f, 0.98f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(win.transform, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -8f);
            trt.sizeDelta = new Vector2(-80f, 32f);
            var t = titleGo.GetComponent<Text>();
            t.font = _styleText.font;
            t.fontSize = 16;
            t.color = new Color(0.95f, 0.80f, 0.30f);
            t.text = AutoLabels.WindowTitle;
            t.alignment = TextAnchor.MiddleCenter;

            CloneBtn(win.transform, "CloseBtn", "X", new Vector2(772f, -8f), new Vector2(36f, 30f),
                () => _modalRoot.SetActive(false), new Color(0.65f, 0.20f, 0.20f));

            // Top Automation Action Bar
            CloneBtn(win.transform, "BtnAutoAssign", AutoLabels.BtnAutoAssign, new Vector2(20f, -48f), new Vector2(250f, 34f), () =>
            {
                int count = SmartAutomationEngine.AutoOptimizeStaffRoles();
                if (_statusBanner != null) _statusBanner.text = $"Optimized {count} staff roles to match peak skills!";
                RefreshAnalytics();
            }, new Color(0.18f, 0.50f, 0.76f));

            CloneBtn(win.transform, "BtnAutoRest", AutoLabels.BtnAutoRest, new Vector2(282f, -48f), new Vector2(250f, 34f), () =>
            {
                var ms = UnityEngine.Object.FindObjectOfType<mainScript>();
                int count = SmartAutomationEngine.RelieveExhaustedStaff(ms);
                if (_statusBanner != null) _statusBanner.text = $"Restored motivation for {count} exhausted staff members!";
                RefreshAnalytics();
            }, new Color(0.20f, 0.56f, 0.36f));

            var statusGo = new GameObject("StatusBanner", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(win.transform, false);
            var srt = statusGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0f, 1f);
            srt.anchoredPosition = new Vector2(545f, -48f);
            srt.sizeDelta = new Vector2(-565f, 34f);
            _statusBanner = statusGo.GetComponent<Text>();
            _statusBanner.font = _styleText.font;
            _statusBanner.fontSize = 12;
            _statusBanner.color = new Color(0.45f, 0.92f, 0.55f);
            _statusBanner.alignment = TextAnchor.MiddleLeft;

            var contentGo = new GameObject("AnalyticsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(win.transform, false);
            var crt = contentGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.offsetMin = new Vector2(18f, 18f);
            crt.offsetMax = new Vector2(-18f, -92f);
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _listContent = contentGo.transform;
        }

        private static void RefreshAnalytics()
        {
            if (_listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listContent.GetChild(i).gameObject);

            var ms = UnityEngine.Object.FindObjectOfType<mainScript>();
            if (ms?.games_?.arrayGamesScripts == null) return;

            // Summarize top-grossing player games & genres
            var myGames = ms.games_.arrayGamesScripts
                .Where(g => g != null && g.IsMyGame())
                .OrderByDescending(g => g.umsatzTotal)
                .Take(9)
                .ToList();

            foreach (var g in myGames)
            {
                long devCost = Math.Max(1L, g.GetGesamteEntwicklungskosten());
                long netProfit = g.umsatzTotal - devCost;
                float roiPct = (float)((double)netProfit / devCost * 100.0);

                var row = new GameObject("ROI_" + g.myID, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(_listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 36f;
                row.GetComponent<Image>().color = new Color(0.17f, 0.20f, 0.25f, 0.88f);

                var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(row.transform, false);
                var trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(10f, 2f);
                trt.offsetMax = new Vector2(-10f, -2f);
                var txt = txtGo.GetComponent<Text>();
                txt.font = _styleText.font;
                txt.fontSize = 12;
                txt.color = roiPct >= 0f ? new Color(0.55f, 0.95f, 0.62f) : new Color(0.95f, 0.45f, 0.45f);
                txt.raycastTarget = false;
                txt.text = $"{g.GetNameWithTag()}  |  Revenue: {ms.GetMoney(g.umsatzTotal, true)}  |  Cost: {ms.GetMoney(devCost, true)}  |  Net ROI: {roiPct:F0}%";
            }
        }

        private static Button CloneBtn(Transform parent, string name, string label, Vector2 pos, Vector2 size,
            UnityEngine.Events.UnityAction onClick, Color color)
        {
            var go = UnityEngine.Object.Instantiate(_btnTemplate, parent, false);
            go.name = name;
            go.SetActive(true);
            foreach (var st in go.GetComponentsInChildren<setText>(true)) UnityEngine.Object.DestroyImmediate(st);
            foreach (var et in go.GetComponentsInChildren<EventTrigger>(true)) UnityEngine.Object.DestroyImmediate(et);
            foreach (var tt in go.GetComponentsInChildren<tooltip>(true)) UnityEngine.Object.DestroyImmediate(tt);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            if (img != null) { img.color = color; img.raycastTarget = true; }
            var t = go.GetComponentInChildren<Text>(true);
            if (t != null) { t.text = label; t.fontSize = 11; t.color = Color.white; t.raycastTarget = false; }
            var b = go.GetComponent<Button>();
            b.enabled = true;
            b.interactable = true;
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() =>
            {
                try { UnityEngine.Object.FindObjectOfType<sfxScript>()?.PlaySound(3, true); } catch { }
                onClick?.Invoke();
            });
            return b;
        }
    }
}
