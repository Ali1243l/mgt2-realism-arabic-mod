using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal class WorldEventsWindow : MonoBehaviour
    {
        private static WorldEventsWindow _instance;
        private GameObject _rootPanel;
        private Text _titleText;
        private Text _statsText;
        private Transform _rowsContainer;
        private GameObject _buttonProto;

        public static void Init()
        {
            if (_instance != null) return;
            var go = new GameObject("MGT2_WorldEventsWindow_Host");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<WorldEventsWindow>();
        }

        public static void Toggle()
        {
            if (_instance == null) Init();
            if (_instance != null) _instance.ToggleWindow();
        }

        private void Update()
        {
            bool combo = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Alpha4);
            if (Input.GetKeyDown(KeyCode.Home) || combo)
            {
                ToggleWindow();
            }
        }

        private void ToggleWindow()
        {
            if (_rootPanel == null) BuildUi();
            if (_rootPanel == null) return;

            bool next = !_rootPanel.activeSelf;
            _rootPanel.SetActive(next);
            if (next)
                RefreshContent();
        }

        private void BuildUi()
        {
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas == null) return;

            var devMenu = FindDeep(canvas.transform, "Menu_DevGame");
            if (devMenu != null)
            {
                var closeBtn = FindDeep(devMenu, "BUTTON_Close");
                if (closeBtn != null) _buttonProto = closeBtn.gameObject;
            }

            // Native MGT2 Silver-White Window (#EBEBED) + Cerulean Blue Header (#1C7DC2)
            _rootPanel = new GameObject("Panel_WorldEvents");
            _rootPanel.transform.SetParent(canvas.transform, false);
            var rt = _rootPanel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(780f, 510f);
            rt.anchoredPosition = Vector2.zero;

            var bg = _rootPanel.AddComponent<Image>();
            bg.color = new Color(0.92f, 0.92f, 0.93f, 1f);
            var outline = _rootPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.24f, 0.27f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            var headerBar = new GameObject("MGT2_HeaderBar");
            headerBar.transform.SetParent(_rootPanel.transform, false);
            var hbRt = headerBar.AddComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0f, 1f);
            hbRt.anchorMax = new Vector2(1f, 1f);
            hbRt.pivot = new Vector2(0.5f, 1f);
            hbRt.sizeDelta = new Vector2(-8f, 48f);
            hbRt.anchoredPosition = new Vector2(0f, -4f);
            var hbImg = headerBar.AddComponent<Image>();
            hbImg.color = new Color(0.11f, 0.49f, 0.76f, 1f);
            hbImg.raycastTarget = false;

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_rootPanel.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-32f, 44f);
            titleRt.anchoredPosition = new Vector2(0f, -6f);

            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 18;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = Color.white;
            _titleText.raycastTarget = false;
            var titleShadow = titleGo.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            titleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            var statsBox = new GameObject("MGT2_StatsRecessedBox");
            statsBox.transform.SetParent(_rootPanel.transform, false);
            var sbRt = statsBox.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0f, 1f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(0.5f, 1f);
            sbRt.sizeDelta = new Vector2(-40f, 86f);
            sbRt.anchoredPosition = new Vector2(0f, -60f);
            var sbImg = statsBox.AddComponent<Image>();
            sbImg.color = new Color(0.81f, 0.82f, 0.84f, 1f);
            sbImg.raycastTarget = false;
            var sbOut = statsBox.AddComponent<Outline>();
            sbOut.effectColor = new Color(0.42f, 0.44f, 0.48f, 1f);
            sbOut.effectDistance = new Vector2(1f, -1f);

            var statsGo = new GameObject("StatsSummary");
            statsGo.transform.SetParent(_rootPanel.transform, false);
            var statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 1f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.pivot = new Vector2(0.5f, 1f);
            statsRt.sizeDelta = new Vector2(-48f, 86f);
            statsRt.anchoredPosition = new Vector2(0f, -60f);

            _statsText = statsGo.AddComponent<Text>();
            _statsText.font = font;
            _statsText.fontSize = 13;
            _statsText.fontStyle = FontStyle.Bold;
            _statsText.alignment = TextAnchor.MiddleCenter;
            _statsText.color = new Color(0.09f, 0.10f, 0.12f, 1f);
            _statsText.raycastTarget = false;

            var rowsGo = new GameObject("RowsContainer");
            rowsGo.transform.SetParent(_rootPanel.transform, false);
            var rowsRt = rowsGo.AddComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 1f);
            rowsRt.offsetMin = new Vector2(24f, 36f);
            rowsRt.offsetMax = new Vector2(-24f, -158f);
            _rowsContainer = rowsGo.transform;

            var closeBtnGo = SpawnButton(_rootPanel.transform, "X", new Color(0.95f, 0.95f, 0.96f, 1f), () => _rootPanel.SetActive(false));
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(38f, 34f);
            closeRt.anchoredPosition = new Vector2(-14f, -10f);

            _rootPanel.SetActive(false);
        }

        private void RefreshContent()
        {
            if (_rowsContainer == null) return;
            for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
                Destroy(_rowsContainer.GetChild(i).gameObject);

            _titleText.text = "Global Industry Events, Quarantine Surge & Viral Memes (Home / Ctrl+4)";

            var mainGo = GameObject.Find("Main");
            var mS = mainGo != null ? mainGo.GetComponent<mainScript>() : null;

            _statsText.text =
                $"Current Macro Event: {WorldEventsEngine.GetCurrentEventTitle()}\n" +
                $"Sales Multiplier: {WorldEventsEngine.GetGlobalSalesMultiplier():F2}x  |  Weeks Remaining: {WorldEventsEngine.EventWeeksRemaining}\n" +
                $"Zero-Trust Firewall: {(WorldEventsEngine.CyberFirewallUpgraded ? "ACTIVE" : "NONE")}  |  Remote Work Cloud: {(WorldEventsEngine.RemoteWorkInfrastructure ? "READY" : "NONE")}  |  Bonus Earned: USD {WorldEventsEngine.TotalCrisisBonusRevenue:N0}";

            float y = -8f;
            CreateActionRow(
                y,
                "1. Trigger / Cycle Global Macro Event (Quarantine +300%, GPU Boom, Viral Meme Revival)",
                "Cycle Global Event",
                true,
                () => { WorldEventsEngine.CycleSimulatedWorldEvent(); RefreshContent(); }
            );
            y -= 60f;

            CreateActionRow(
                y,
                $"2. Zero-Trust Cybersecurity Firewall ($135k) -> Blocks AAA Source-Code Leaks ({(WorldEventsEngine.CyberFirewallUpgraded ? "INSTALLED" : "NOT INSTALLED")})",
                WorldEventsEngine.CyberFirewallUpgraded ? "Firewall Active" : "Install ($135k)",
                false,
                () => { WorldEventsEngine.UpgradeCyberFirewall(mS); RefreshContent(); }
            );
            y -= 60f;

            CreateActionRow(
                y,
                $"3. Cloud Work-From-Home Infrastructure ($90k) -> Cancels Quarantine Dev Speed Penalty",
                WorldEventsEngine.RemoteWorkInfrastructure ? "Cloud Ready" : "Upgrade ($90k)",
                false,
                () => { WorldEventsEngine.UpgradeRemoteWorkInfra(mS); RefreshContent(); }
            );
            y -= 60f;

            CreateActionRow(
                y,
                "4. Amplify Community Meme Trend -> Ignites 2.25x Back-Catalog Sales Surge + 4,200 Fans",
                "Ignite Meme Surge",
                false,
                () => { WorldEventsEngine.TriggerViralMemeCampaign(mS); RefreshContent(); }
            );
        }

        private void CreateActionRow(float y, string label, string btnLabel, bool primarySteelBlue, Action onClick)
        {
            var row = new GameObject("EventRow");
            row.transform.SetParent(_rowsContainer, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 52f);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.86f, 0.88f, 1f);
            var rowOutline = row.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0.32f, 0.34f, 0.38f, 1f);
            rowOutline.effectDistance = new Vector2(1f, -1f);

            var txtGo = new GameObject("RowText");
            txtGo.transform.SetParent(row.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.offsetMin = new Vector2(16f, 4f);
            txtRt.offsetMax = new Vector2(-215f, -4f);

            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = new Color(0.09f, 0.10f, 0.12f, 1f);
            txt.raycastTarget = false;
            txt.text = label;

            Color btnColor = primarySteelBlue
                ? new Color(0.32f, 0.60f, 0.76f, 1f)
                : new Color(0.84f, 0.85f, 0.86f, 1f);

            var btnGo = SpawnButton(row.transform, btnLabel, btnColor, onClick);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.sizeDelta = new Vector2(190f, 36f);
            btnRt.anchoredPosition = new Vector2(-12f, 0f);
        }

        private GameObject SpawnButton(Transform parent, string text, Color color, Action onClick)
        {
            GameObject go;
            if (_buttonProto != null)
            {
                go = Instantiate(_buttonProto, parent, false);
                go.SetActive(true);
                foreach (var st in go.GetComponentsInChildren<setText>(true)) DestroyImmediate(st);
                foreach (var et in go.GetComponentsInChildren<EventTrigger>(true)) DestroyImmediate(et);
                foreach (var tt in go.GetComponentsInChildren<tooltip>(true)) DestroyImmediate(tt);
            }
            else
            {
                go = new GameObject("Btn");
                go.transform.SetParent(parent, false);
                go.AddComponent<RectTransform>();
                go.AddComponent<Image>();
                go.AddComponent<Button>();
                var tGo = new GameObject("Text");
                tGo.transform.SetParent(go.transform, false);
                var tRt = tGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;
                var t = tGo.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.09f, 0.10f, 0.12f, 1f);
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
                img.raycastTarget = true;
            }
            var btnOut = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            btnOut.effectColor = new Color(0.14f, 0.16f, 0.18f, 1f);
            btnOut.effectDistance = new Vector2(1.2f, -1.2f);

            var label = go.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
                label.fontSize = 12;
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(0.09f, 0.10f, 0.12f, 1f);
                label.raycastTarget = false;
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.enabled = true;
                btn.interactable = true;
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() =>
                {
                    var mainGo = GameObject.Find("Main");
                    var sfx = mainGo != null ? mainGo.GetComponent<sfxScript>() : null;
                    if (sfx != null) sfx.PlaySound(3, true);
                    onClick?.Invoke();
                });
            }
            return go;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
