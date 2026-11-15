// Native Unity UI Window for GDD Point 11: Early Access & Crowdfunding (F3).
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class EarlyAccessLabels
    {
        private static bool IsAr
        {
            get
            {
                var main = GameObject.Find("Main");
                var s = main != null ? main.GetComponent<settingsScript>() : null;
                return s != null && s.language == 13;
            }
        }

        internal static string Title => IsAr
            ? "(Early Access & Kickstarter) \ufef2\ufecb\ufe8e\ufee4\ufea0\ufedf\ufe8d \ufedd\ufef3\ufeee\ufee4\ufe98\ufedf\ufe8d\ufeed \ufeae\ufedc\ufe92\ufee4\ufedf\ufe8d \ufedd\ufeee\ufebb\ufeee\ufedf\ufe8d"
            : "Early Access & Crowdfunding Campaigns (F3)";

        internal static string BtnKickstarter => IsAr ? "Kickstarter \ufe94\ufee0\ufee4\ufea3" : "Kickstarter";
        internal static string BtnRoadmap => IsAr ? "($120k) \ufed5\ufef3\ufeae\ufec4\ufedf\ufe8d \ufe94\ufec4\ufead\ufe8e\ufea7" : "Roadmap (+25%)";
        internal static string BtnRelease10 => IsAr ? "(1.0) \ufedd\ufee3\ufe8e\ufedc\ufedf\ufe8d \ufed5\ufefc\ufec3\ufef9\ufe8d" : "Launch 1.0!";
    }

    public static class EarlyAccessWindow
    {
        private static GameObject _root;
        private static Transform _listContent;
        private static Text _styleText;
        private static GameObject _btnTemplate;

        public static void Toggle()
        {
            if (_root != null && _root.activeSelf)
            {
                _root.SetActive(false);
                return;
            }
            Open();
        }

        public static void Open()
        {
            var mainGo = GameObject.Find("Main");
            if (mainGo == null) return;
            var gui = mainGo.GetComponent<GUI_Main>();
            if (gui == null) return;

            if (_root == null)
                BuildWindow(gui);

            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private static void BuildWindow(GUI_Main gui)
        {
            var sourceMenu = gui.uiObjects.FirstOrDefault(o => o != null && o.name == "Menu_Dev_Game");
            if (sourceMenu == null) return;

            _styleText = sourceMenu.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.font != null);
            var allBtns = sourceMenu.GetComponentsInChildren<Button>(true);
            var closeBtn = allBtns.FirstOrDefault(b => b.name.Contains("Close") || b.name.Contains("Abbrechen")) ?? allBtns.FirstOrDefault();
            _btnTemplate = allBtns.FirstOrDefault(b => b != closeBtn)?.gameObject ?? closeBtn?.gameObject;

            _root = Object.Instantiate(sourceMenu.transform.GetChild(0).gameObject, gui.transform, false);
            _root.name = "Window_EarlyAccess";

            for (int i = _root.transform.childCount - 1; i >= 0; i--)
            {
                var ch = _root.transform.GetChild(i);
                if (ch.name != "Header" && !ch.name.Contains("Close") && !ch.name.Contains("Abbrechen"))
                    Object.DestroyImmediate(ch.gameObject);
            }

            var rt = _root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(880f, 540f);
            rt.anchoredPosition = Vector2.zero;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(_root.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(-80f, 34f);
            var titleTxt = titleGo.GetComponent<Text>();
            titleTxt.font = _styleText != null ? _styleText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleTxt.fontSize = 18;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.55f, 0.95f, 0.72f);
            titleTxt.raycastTarget = false;
            titleTxt.text = EarlyAccessLabels.Title;

            if (closeBtn != null)
            {
                CloneBtn(_root.transform, "Btn_Close", "X", new Vector2(832f, -10f), new Vector2(36f, 32f),
                    () => _root.SetActive(false), new Color(0.75f, 0.22f, 0.22f));
            }

            var listGo = new GameObject("EAList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(_root.transform, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.offsetMin = new Vector2(24f, 20f);
            listRt.offsetMax = new Vector2(-24f, -62f);
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _listContent = listGo.transform;
        }

        public static void Refresh()
        {
            if (_listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_listContent.GetChild(i).gameObject);

            var mainGo = GameObject.Find("Main");
            if (mainGo == null) return;
            var ms = mainGo.GetComponent<mainScript>();
            if (ms == null) return;

            var myGames = Object.FindObjectsOfType<gameScript>()
                .Where(g => g != null && g.IsMyGame())
                .OrderByDescending(g => g.myID)
                .Take(8)
                .ToList();

            foreach (var g in myGames)
            {
                var st = EarlyAccessEngine.GetOrCreate(g);

                var row = new GameObject("EA_" + g.myID, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(_listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 46f;
                row.GetComponent<Image>().color = st.weeksSinceLastUpdate > 8
                    ? new Color(0.38f, 0.16f, 0.16f, 0.92f)
                    : new Color(0.17f, 0.21f, 0.26f, 0.92f);

                var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(row.transform, false);
                var trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(10f, 2f);
                trt.offsetMax = new Vector2(-385f, -2f);
                var txt = txtGo.GetComponent<Text>();
                txt.font = _styleText.font;
                txt.fontSize = 12;
                txt.color = Color.white;
                txt.raycastTarget = false;

                string status = st.graduatedToFullRelease ? "v1.0 Released"
                    : st.inEarlyAccess ? $"EA ({st.roadmapCompletionPct:F0}%)" : "Standard";
                txt.text =
                    $"{g.GetNameWithTag()} | {status} | Pledged: {ms.GetMoney(st.crowdfundingRaised, true)} | " +
                    $"Trust: {st.backerTrustPct:F0}% | Stale: {st.weeksSinceLastUpdate}w";

                gameScript capGame = g;

                CloneBtn(row.transform, "Kickstarter", EarlyAccessLabels.BtnKickstarter, new Vector2(445f, -8f), new Vector2(120f, 30f), () =>
                {
                    EarlyAccessEngine.LaunchCrowdfundingCampaign(ms, capGame);
                    Refresh();
                }, new Color(0.20f, 0.56f, 0.38f));

                CloneBtn(row.transform, "Roadmap", EarlyAccessLabels.BtnRoadmap, new Vector2(572f, -8f), new Vector2(124f, 30f), () =>
                {
                    EarlyAccessEngine.PushRoadmapUpdate(ms, capGame, 120000L);
                    Refresh();
                }, new Color(0.24f, 0.46f, 0.68f));

                CloneBtn(row.transform, "Release10", EarlyAccessLabels.BtnRelease10, new Vector2(703f, -8f), new Vector2(120f, 30f), () =>
                {
                    EarlyAccessEngine.GraduateToVersion10(ms, capGame);
                    Refresh();
                }, new Color(0.72f, 0.48f, 0.14f));
            }
        }

        private static Button CloneBtn(Transform parent, string name, string label, Vector2 pos, Vector2 size,
            UnityEngine.Events.UnityAction onClick, Color color)
        {
            var go = Object.Instantiate(_btnTemplate, parent, false);
            go.name = name;
            go.SetActive(true);
            foreach (var st in go.GetComponentsInChildren<setText>(true)) Object.DestroyImmediate(st);
            foreach (var et in go.GetComponentsInChildren<EventTrigger>(true)) Object.DestroyImmediate(et);
            foreach (var tt in go.GetComponentsInChildren<tooltip>(true)) Object.DestroyImmediate(tt);
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
                try { Object.FindObjectOfType<sfxScript>()?.PlaySound(3, true); } catch { }
                onClick?.Invoke();
            });
            return b;
        }
    }
}
