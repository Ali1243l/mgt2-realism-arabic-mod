// Native Unity UI Window for GDD Point 7: Programming Languages & Engine Tech Debt (F11).
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class TechLabels
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
            ? "(Tech Debt & Languages) \ufef2\ufee8\ufed8\ufe98\ufedf\ufe8d \ufee6\ufef3\ufeaa\ufedf\ufe8d\ufeed \ufe95\ufe8e\ufedb\ufeae\ufea4\ufee4\ufedf\ufe8d \ufe94\ufeb3\ufeaa\ufee8\ufeeb"
            : "Engine Tech Debt & Programming Languages (F11)";

        internal static string LangAsm => IsAr ? "(Assembly / C) \ufef2\ufedc\ufef4\ufeb3\ufefc\ufedb" : "Assembly / C (+Perf / Slow)";
        internal static string LangCpp => IsAr ? "(C++ / C#) \ufeaa\ufea3\ufeee\ufee3 \ufe9a\ufef3\ufeaa\ufea3" : "C++ / C# (Balanced)";
        internal static string LangAi => IsAr ? "(AI Neural SDK) \ufef2\ufe92\ufebc\ufecb \ufee2\ufe9f\ufeae\ufe98\ufee3" : "AI Neural SDK (Fast)";

        internal static string BtnCycleLang => IsAr ? "\ufe94\ufed0\ufee0\ufedf\ufe8d \ufeae\ufef4\ufef4\ufed0\ufe97" : "Switch Lang";
        internal static string BtnRefactor => IsAr ? "(Refactor) \ufe93\ufe8d\ufeee\ufee8\ufedf\ufe8d \ufe94\ufee0\ufedc\ufef4\ufeeb" : "Refactor Core";

        internal static string StatusClean => IsAr ? "\ufea9\ufeed\ufedc\ufedf\ufe8d \ufed2\ufef4\ufec8\ufee7" : "Clean Core";
        internal static string StatusSpaghetti => IsAr ? "!(Spaghetti) \ufeaa\ufed8\ufecc\ufee3 \ufea9\ufeed\ufedb" : "Spaghetti Debt!";
    }

    public static class TechDebtWindow
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
            var sourceMenu = gui.uiObjects?.FirstOrDefault(o => o != null && (o.name == "Menu_Dev_Game" || o.name == "Menu_DevGame" || o.GetComponent<Menu_DevGame>() != null || o.name.StartsWith("Menu_Dev")));
            if (sourceMenu == null) return;

            _styleText = sourceMenu.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.font != null);
            var allBtns = sourceMenu.GetComponentsInChildren<Button>(true);
            var closeBtn = allBtns.FirstOrDefault(b => b.name.Contains("Close") || b.name.Contains("Abbrechen")) ?? allBtns.FirstOrDefault();
            _btnTemplate = allBtns.FirstOrDefault(b => b != closeBtn)?.gameObject ?? closeBtn?.gameObject;

            _root = Object.Instantiate(sourceMenu.transform.GetChild(0).gameObject, gui.transform, false);
            _root.name = "Window_TechDebt";

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
            titleTxt.color = new Color(0.85f, 0.75f, 1f);
            titleTxt.raycastTarget = false;
            titleTxt.text = TechLabels.Title;

            if (closeBtn != null)
            {
                CloneBtn(_root.transform, "Btn_Close", "X", new Vector2(832f, -10f), new Vector2(36f, 32f),
                    () => _root.SetActive(false), new Color(0.75f, 0.22f, 0.22f));
            }

            var listGo = new GameObject("EngineList", typeof(RectTransform), typeof(VerticalLayoutGroup));
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

            var myEngines = Object.FindObjectsOfType<engineScript>()
                .Where(e => e != null && e.ownerID == ms.myID)
                .OrderByDescending(e => e.myID)
                .Take(9)
                .ToList();

            foreach (var es in myEngines)
            {
                var st = TechDebtEngine.GetOrCreate(es);
                float speedMult = TechDebtEngine.CalculateDevSpeedMultiplier(st.language, st.techDebtPct);
                float bugMult = TechDebtEngine.CalculateBugMultiplier(st.language, st.techDebtPct);
                float portFriction = TechDebtEngine.GetPortingFrictionMultiplier(st.language);
                long refCost = TechDebtEngine.GetRefactorCost(st);

                var row = new GameObject("Eng_" + es.myID, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(_listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 46f;
                row.GetComponent<Image>().color = st.techDebtPct >= 45f
                    ? new Color(0.36f, 0.16f, 0.18f, 0.92f)
                    : new Color(0.17f, 0.20f, 0.27f, 0.92f);

                var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(row.transform, false);
                var trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(10f, 2f);
                trt.offsetMax = new Vector2(-280f, -2f);
                var txt = txtGo.GetComponent<Text>();
                txt.font = _styleText.font;
                txt.fontSize = 12;
                txt.color = Color.white;
                txt.raycastTarget = false;

                string langName = st.language == ProgLanguageParadigm.ClassicAsmC ? TechLabels.LangAsm
                    : st.language == ProgLanguageParadigm.ModernManagedCppCSharp ? TechLabels.LangCpp : TechLabels.LangAi;
                string statusStr = st.techDebtPct >= 45f ? TechLabels.StatusSpaghetti : TechLabels.StatusClean;

                txt.text =
                    $"{es.GetName()} | {langName} | Debt: {st.techDebtPct:F0}% ({statusStr}) | " +
                    $"Speed: {speedMult * 100f:F0}% | Bugs: {bugMult:F2}x | Porting: {portFriction:F2}x";

                EngineTechState capState = st;

                CloneBtn(row.transform, "CycleLang", TechLabels.BtnCycleLang, new Vector2(560f, -8f), new Vector2(120f, 30f), () =>
                {
                    capState.language = (ProgLanguageParadigm)(((int)capState.language + 1) % 3);
                    Refresh();
                }, new Color(0.35f, 0.28f, 0.62f));

                CloneBtn(row.transform, "Refactor", $"{TechLabels.BtnRefactor} ({ms.GetMoney(refCost, true)})", new Vector2(688f, -8f), new Vector2(135f, 30f), () =>
                {
                    TechDebtEngine.RefactorEngineCore(ms, capState);
                    Refresh();
                }, new Color(0.18f, 0.55f, 0.36f));
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
