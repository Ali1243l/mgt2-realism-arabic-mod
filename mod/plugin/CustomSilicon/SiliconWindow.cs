// Native Unity UI Window for GDD Point 6: Custom Silicon & PC Hardware Lab (F10).
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class SiliconLabels
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
            ? "(Silicon & PC Lab) \ufead\ufef3\ufeed\ufea9\ufead\ufe8e\ufeec\ufedf\ufe8d\ufeed \ufea2\ufe8b\ufe8d\ufeae\ufeb8\ufedf\ufe8d \ufeae\ufe92\ufe98\ufea8\ufee3"
            : "Custom Silicon & PC Hardware Lab (F10)";

        internal static string BtnUpgradeNode => IsAr ? "\ufe94\ufef3\ufead\ufe8e\ufee4\ufecc\ufee4\ufedf\ufe8d \ufeae\ufef3\ufeed\ufec4\ufe97" : "Upgrade Node";
        internal static string BtnSwitchFoundry => IsAr ? "\ufeda\ufe92\ufeb4\ufee4\ufedf\ufe8d \ufedd\ufef3\ufeaa\ufe92\ufe97" : "Switch Foundry";
        internal static string BtnCycleCooling => IsAr ? "\ufeaa\ufef3\ufeae\ufe92\ufe98\ufedf\ufe8d \ufeae\ufef4\ufef4\ufed0\ufe97" : "Cycle Cooling";
        internal static string BtnLaunchGpu => IsAr ? "PC \ufe94\ufeb7\ufe8e\ufeb7 \ufe95\ufeae\ufedb \ufed5\ufefc\ufec3\ufe87" : "Launch PC GPU";
        internal static string BtnRecallFix => IsAr ? "\ufe94\ufee7\ufe8e\ufef4\ufebb\ufeed \ufe80\ufe8e\ufecb\ufeaa\ufe98\ufeb3\ufe8d \ufe94\ufee0\ufee4\ufea3" : "RRoD Recall Fix ($450k)";

        internal static string FoundryTier1 => IsAr ? "(Tier-1 TSMC) \ufeef\ufedf\ufeed\ufe83 \ufe94\ufe8c\ufed3 \ufeda\ufe92\ufeb4\ufee3" : "Tier-1 Foundry (TSMC)";
        internal static string FoundryBudget => IsAr ? "(Budget Fab) \ufef1\ufea9\ufe8e\ufebc\ufe98\ufed7\ufe8d \ufeda\ufe92\ufeb4\ufee3" : "Budget Foundry";

        internal static string CoolAir => IsAr ? "\ufeba\ufef4\ufea7\ufead \ufef2\ufe8b\ufe8d\ufeed\ufeeb \ufeaa\ufef3\ufeae\ufe92\ufe97" : "Budget Air Cooling";
        internal static string CoolVapor => IsAr ? "(Vapor Chamber) \ufead\ufe8e\ufea8\ufe91 \ufe94\ufed3\ufeae\ufecf" : "Vapor Chamber";
        internal static string CoolLiquid => IsAr ? "\ufef2\ufed3\ufe8d\ufeae\ufe98\ufea3\ufe8d \ufef2\ufe8b\ufe8e\ufee3 \ufeaa\ufef3\ufeae\ufe92\ufe97" : "Liquid Phase Cooling";

        internal static string SupplyStable => IsAr ? "\ufe93\ufeae\ufed8\ufe98\ufeb4\ufee3 \ufeaa\ufef3\ufead\ufeed\ufe98\ufedf\ufe8d \ufeb3\ufefc\ufeb3" : "Supply Chain: Stable";
        internal static string SupplyShortage => IsAr ? "!\ufe94\ufef4\ufee4\ufedf\ufe8e\ufecb \ufed5\ufe8b\ufe8e\ufed7\ufead \ufe94\ufee3\ufeb0\ufe83" : "Supply Crisis: Wafer Shortage!";
        internal static string SupplyBoom => IsAr ? "(GPU Boom!) \ufee6\ufef3\ufeaa\ufecc\ufe98\ufedf\ufe8d\ufeed AI \ufe93\ufeae\ufed4\ufec3" : "Crypto/AI GPU Boom (+115% Demand)";
    }

    public static class SiliconWindow
    {
        private static GameObject _root;
        private static Transform _listContent;
        private static Text _headerSummary;
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
            _root.name = "Window_CustomSiliconLab";

            for (int i = _root.transform.childCount - 1; i >= 0; i--)
            {
                var ch = _root.transform.GetChild(i);
                if (ch.name != "Header" && !ch.name.Contains(" Close") && !ch.name.Contains("Abbrechen"))
                    Object.DestroyImmediate(ch.gameObject);
            }

            var rt = _root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(880f, 560f);
            rt.anchoredPosition = Vector2.zero;

            // Title
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
            titleTxt.color = new Color(0.55f, 0.90f, 1f);
            titleTxt.raycastTarget = false;
            titleTxt.text = SiliconLabels.Title;

            // Close Button
            if (closeBtn != null)
            {
                CloneBtn(_root.transform, "Btn_Close", "X", new Vector2(832f, -10f), new Vector2(36f, 32f),
                    () => _root.SetActive(false), new Color(0.75f, 0.22f, 0.22f));
            }

            // Summary KPI Bar
            var sumGo = new GameObject("SummaryBar", typeof(RectTransform), typeof(Text));
            sumGo.transform.SetParent(_root.transform, false);
            var sumRt = sumGo.GetComponent<RectTransform>();
            sumRt.anchorMin = new Vector2(0f, 1f);
            sumRt.anchorMax = new Vector2(1f, 1f);
            sumRt.pivot = new Vector2(0.5f, 1f);
            sumRt.anchoredPosition = new Vector2(0f, -52f);
            sumRt.sizeDelta = new Vector2(-40f, 44f);
            _headerSummary = sumGo.GetComponent<Text>();
            _headerSummary.font = titleTxt.font;
            _headerSummary.fontSize = 13;
            _headerSummary.alignment = TextAnchor.MiddleCenter;
            _headerSummary.color = Color.white;
            _headerSummary.raycastTarget = false;

            // Top Action Toolbar
            CloneBtn(_root.transform, "BtnUpgradeNode", SiliconLabels.BtnUpgradeNode, new Vector2(24f, -102f), new Vector2(158f, 34f), () =>
            {
                var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
                SiliconLabEngine.UpgradeProcessNode(ms);
                Refresh();
            }, new Color(0.18f, 0.48f, 0.68f));

            CloneBtn(_root.transform, "BtnSwitchFoundry", SiliconLabels.BtnSwitchFoundry, new Vector2(190f, -102f), new Vector2(158f, 34f), () =>
            {
                SiliconLabEngine.State.foundry = SiliconLabEngine.State.foundry == FoundryPartner.BudgetFoundry
                    ? FoundryPartner.Tier1Foundry : FoundryPartner.BudgetFoundry;
                Refresh();
            }, new Color(0.38f, 0.30f, 0.62f));

            CloneBtn(_root.transform, "BtnCycleCooling", SiliconLabels.BtnCycleCooling, new Vector2(356f, -102f), new Vector2(158f, 34f), () =>
            {
                SiliconLabEngine.State.cooling = (CoolingSolution)(((int)SiliconLabEngine.State.cooling + 1) % 3);
                Refresh();
            }, new Color(0.20f, 0.52f, 0.45f));

            CloneBtn(_root.transform, "BtnLaunchGpu", SiliconLabels.BtnLaunchGpu, new Vector2(522f, -102f), new Vector2(165f, 34f), () =>
            {
                var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
                SiliconLabEngine.LaunchDiscreteGpuLine(ms);
                Refresh();
            }, new Color(0.22f, 0.60f, 0.30f));

            CloneBtn(_root.transform, "BtnRecallFix", SiliconLabels.BtnRecallFix, new Vector2(695f, -102f), new Vector2(160f, 34f), () =>
            {
                var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
                SiliconLabEngine.ExecuteRecallRepairCampaign(ms, 450000L);
                Refresh();
            }, new Color(0.72f, 0.32f, 0.16f));

            // Product Lines List Container
            var listGo = new GameObject("GpuList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(_root.transform, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.offsetMin = new Vector2(24f, 20f);
            listRt.offsetMax = new Vector2(-24f, -148f);
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

            var st = SiliconLabEngine.State;
            float yieldPct = SiliconLabEngine.CalculateWaferYieldPct(st.currentNode, st.foundry, st.dieAreaMm2);
            float failPct = SiliconLabEngine.CalculateThermalFailureRatePct(st.targetTdpWatts, st.cooling, out float tjC);

            string foundryStr = st.foundry == FoundryPartner.Tier1Foundry ? SiliconLabels.FoundryTier1 : SiliconLabels.FoundryBudget;
            string coolStr = st.cooling == CoolingSolution.BudgetAir ? SiliconLabels.CoolAir
                : st.cooling == CoolingSolution.VaporChamber ? SiliconLabels.CoolVapor : SiliconLabels.CoolLiquid;
            string supplyStr = st.supplyStatus == SupplyChainStatus.Stable ? SiliconLabels.SupplyStable
                : st.supplyStatus == SupplyChainStatus.WaferShortage ? SiliconLabels.SupplyShortage : SiliconLabels.SupplyBoom;

            long nextUpgradeCost = SiliconLabEngine.GetNodeUpgradeCost(st.currentNode);
            string upgStr = nextUpgradeCost > 0 ? ms.GetMoney(nextUpgradeCost, true) : "MAX";
            string rrodAlert = st.rrodScandalActive ? " | [ALERT: RRoD RECALL ACTIVE!]" : "";

            _headerSummary.text =
                $"Node: {st.currentNode} (Next: {upgStr}) | {foundryStr} | Yield: {yieldPct:F1}%\n" +
                $"{coolStr} (Tj: {tjC:F0}C, Fail Risk: {failPct:F1}%) | Brand Rep: {st.hardwareBrandRep:F0}/100 | {supplyStr}{rrodAlert}";

            foreach (var gpu in st.gpuProducts)
            {
                var row = new GameObject("GPU_" + gpu.modelName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(_listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 42f;
                row.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.26f, 0.92f);

                var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(row.transform, false);
                var trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(12f, 2f);
                trt.offsetMax = new Vector2(-12f, -2f);
                var txt = txtGo.GetComponent<Text>();
                txt.font = _styleText.font;
                txt.fontSize = 12;
                txt.color = Color.white;
                txt.raycastTarget = false;
                txt.text =
                    $"{gpu.modelName} [{gpu.node}]  |  Perf: {gpu.perfScore:F0} pts  |  Price: USD {gpu.retailPrice:F0}  |  " +
                    $"Sold: {gpu.totalUnitsSold:N0} units  |  Net Profit: {ms.GetMoney(gpu.cumulativeNetProfit, true)}  |  Week: {gpu.weeksOnMarket}/96";
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
