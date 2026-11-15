// Native Unity UI Window for GDD Point 10: Transmedia & Merchandising (F4).
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class TransmediaLabels
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
            ? "(Transmedia & Merch) \ufe95\ufe8e\ufee4\ufeb4\ufea0\ufee4\ufedf\ufe8d\ufeed \ufee2\ufefc\ufed3\ufef7\ufe8d\ufeed \ufe95\ufefc\ufeb4\ufee0\ufeb4\ufee4\ufedf\ufe8d \ufeca\ufeb3\ufeed\ufe97"
            : "Transmedia Studio: Anime, Films & Merchandising (F4)";

        internal static string BtnAnime => IsAr ? "($400k) \ufef2\ufee4\ufee7\ufe83 \ufedd\ufeb4\ufee0\ufeb4\ufee3" : "Anime Series ($400k)";
        internal static string BtnMovie => IsAr ? "($850k) \ufef2\ufe8b\ufe8e\ufee4\ufee8\ufef4\ufeb3 \ufee2\ufee0\ufef4\ufed3" : "Feature Film ($850k)";
        internal static string BtnMerch => IsAr ? "($180k) \ufe95\ufe8e\ufee4\ufeb4\ufea0\ufee3 \ufec2\ufea7" : "Upgrade Merch ($180k)";
    }

    public static class TransmediaWindow
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
            _root.name = "Window_TransmediaMerch";

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
            titleTxt.color = new Color(1f, 0.72f, 0.88f);
            titleTxt.raycastTarget = false;
            titleTxt.text = TransmediaLabels.Title;

            if (closeBtn != null)
            {
                CloneBtn(_root.transform, "Btn_Close", "X", new Vector2(832f, -10f), new Vector2(36f, 32f),
                    () => _root.SetActive(false), new Color(0.75f, 0.22f, 0.22f));
            }

            var listGo = new GameObject("TransmediaList", typeof(RectTransform), typeof(VerticalLayoutGroup));
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
                .Where(g => g != null && g.IsMyGame() && !g.inDevelopment)
                .OrderByDescending(g => g.sellsTotal)
                .Take(8)
                .ToList();

            foreach (var g in myGames)
            {
                var st = TransmediaEngine.GetOrCreate(g);
                float halo = TransmediaEngine.CalculateHaloSalesMultiplier(st);

                var row = new GameObject("TM_" + g.myID, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(_listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 46f;
                row.GetComponent<Image>().color = st.haloWeeksRemaining > 0
                    ? new Color(0.28f, 0.18f, 0.36f, 0.92f)
                    : new Color(0.17f, 0.20f, 0.26f, 0.92f);

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

                txt.text =
                    $"{g.GetNameWithTag()} | Media: {st.activeAdaptation} ({st.haloWeeksRemaining}w) | " +
                    $"Halo: {halo:F2}x | Merch T{st.merchTier}: {ms.GetMoney(st.cumulativeMerchProfit, true)}";

                gameScript capGame = g;

                CloneBtn(row.transform, "Anime", TransmediaLabels.BtnAnime, new Vector2(445f, -8f), new Vector2(122f, 30f), () =>
                {
                    TransmediaEngine.ProduceAnimeSeries(ms, capGame, 400000L);
                    Refresh();
                }, new Color(0.55f, 0.25f, 0.62f));

                CloneBtn(row.transform, "Movie", TransmediaLabels.BtnMovie, new Vector2(574f, -8f), new Vector2(122f, 30f), () =>
                {
                    TransmediaEngine.ProduceHollywoodMovie(ms, capGame, 850000L);
                    Refresh();
                }, new Color(0.72f, 0.35f, 0.18f));

                CloneBtn(row.transform, "Merch", TransmediaLabels.BtnMerch, new Vector2(703f, -8f), new Vector2(120f, 30f), () =>
                {
                    TransmediaEngine.UpgradeMerchLine(ms, capGame, 180000L);
                    Refresh();
                }, new Color(0.18f, 0.54f, 0.36f));
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
