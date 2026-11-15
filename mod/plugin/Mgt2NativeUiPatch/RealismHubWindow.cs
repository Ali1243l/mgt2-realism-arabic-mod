using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal class RealismHubWindow : MonoBehaviour
    {
        private static RealismHubWindow _instance;
        private GameObject _rootPanel;
        private Transform _rowsContainer;
        private GameObject _buttonProto;

        private static readonly string[] KnownModPanelNames = new string[]
        {
            "Panel_RealismHub",
            "Panel_TransmediaMerch",
            "Panel_EarlyAccess",
            "Panel_ModWorkshop",
            "Panel_CustomSilicon",
            "Panel_TechDebt",
            "Panel_EsportsEcosystem",
            "Panel_PublishingDeals",
            "Panel_CommunityDynamics",
            "Panel_SmartAutomation",
            "Panel_CrunchUnions",
            "Panel_DrmAntiCheat",
            "Panel_SalesWishlists",
            "Panel_WorldEvents",
            "Panel_GameAwardsGoty"
        };

        public static void Init()
        {
            if (_instance != null) return;
            var go = new GameObject("MGT2_RealismHubWindow_Host");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RealismHubWindow>();
        }

        public static void OpenHub()
        {
            if (_instance == null) Init();
            if (_instance != null) _instance.ShowWindow();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleWindow();
            }

            // Automatically skin any active mod Panel_* to MGT2's native Silver/Blue UI theme
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas != null)
            {
                foreach (var panelName in KnownModPanelNames)
                {
                    var tr = canvas.transform.Find(panelName);
                    if (tr != null && tr.gameObject.activeSelf)
                    {
                        Mgt2NativePalette.ApplyNativeMgt2SkinToPanel(tr.gameObject);
                    }
                }
            }
        }

        private void ShowWindow()
        {
            if (_rootPanel == null) BuildUi();
            if (_rootPanel == null) return;
            _rootPanel.SetActive(true);
            RefreshContent();
            Mgt2NativePalette.ApplyNativeMgt2SkinToPanel(_rootPanel);
        }

        private void ToggleWindow()
        {
            if (_rootPanel == null) BuildUi();
            if (_rootPanel == null) return;

            bool next = !_rootPanel.activeSelf;
            _rootPanel.SetActive(next);
            if (next)
            {
                RefreshContent();
                Mgt2NativePalette.ApplyNativeMgt2SkinToPanel(_rootPanel);
            }
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

            _rootPanel = new GameObject("Panel_RealismHub");
            _rootPanel.transform.SetParent(canvas.transform, false);
            var rt = _rootPanel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(760f, 560f);
            rt.anchoredPosition = Vector2.zero;

            var bg = _rootPanel.AddComponent<Image>();
            bg.color = Mgt2NativePalette.WindowSilverBody;

            var font = Mgt2NativePalette.GetNativeGameFont();

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_rootPanel.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-32f, 44f);
            titleRt.anchoredPosition = new Vector2(0f, -10f);

            var titleText = titleGo.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "MGT2 Realism Mod — All Systems Hub (F1)";
            titleText.raycastTarget = false;

            var statsGo = new GameObject("StatsSummary");
            statsGo.transform.SetParent(_rootPanel.transform, false);
            var statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 1f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.pivot = new Vector2(0.5f, 1f);
            statsRt.sizeDelta = new Vector2(-40f, 52f);
            statsRt.anchoredPosition = new Vector2(0f, -60f);

            var statsText = statsGo.AddComponent<Text>();
            statsText.font = font;
            statsText.fontSize = 13;
            statsText.alignment = TextAnchor.MiddleCenter;
            statsText.color = Mgt2NativePalette.TextDarkCharcoal;
            statsText.text = "Select any Realism Expansion System below (Native MGT2 Silver & Blue UI Theme Active)";
            statsText.raycastTarget = false;

            var rowsGo = new GameObject("RowsContainer");
            rowsGo.transform.SetParent(_rootPanel.transform, false);
            var rowsRt = rowsGo.AddComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 1f);
            rowsRt.offsetMin = new Vector2(24f, 24f);
            rowsRt.offsetMax = new Vector2(-24f, -122f);
            _rowsContainer = rowsGo.transform;

            var closeBtnGo = SpawnButton(_rootPanel.transform, "X", Mgt2NativePalette.CloseCircleWhite, () => _rootPanel.SetActive(false));
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

            var entries = new (string label, string panelName, string hotkey)[]
            {
                ("GDD #16 & #17: Global Crises & The Game Awards (GOTY)", "Panel_WorldEvents", "Home / Ins"),
                ("GDD #13 & #14: Studio Crunch, Unions & Denuvo DRM", "Panel_CrunchUnions", "F12 / ~"),
                ("GDD #15: Seasonal Sales Festivals & Wishlists", "Panel_SalesWishlists", "End / Ctrl+3"),
                ("GDD #10: Hollywood Movies, Anime & Merch Lines", "Panel_TransmediaMerch", "F4"),
                ("GDD #11 & #12: Early Access, Kickstarter & Mod SDK", "Panel_EarlyAccess", "F3 / F2"),
                ("GDD #6 & #7: Custom Silicon Foundries & Engine Tech Debt", "Panel_CustomSilicon", "F10 / F11"),
                ("GDD #9 & #5: Esports World Cups & Smart Auto-Assign ROI", "Panel_EsportsEcosystem", "F5 / F9"),
            };

            float y = -6f;
            for (int i = 0; i < entries.Length; i++)
            {
                var item = entries[i];
                CreateHubRow(y, item.label, $"Open ({item.hotkey})", i == 0, () =>
                {
                    OpenExistingPanelByName(item.panelName);
                });
                y -= 56f;
            }
        }

        private void OpenExistingPanelByName(string panelName)
        {
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas == null) return;
            var tr = canvas.transform.Find(panelName);
            if (tr != null)
            {
                _rootPanel.SetActive(false);
                tr.gameObject.SetActive(true);
                Mgt2NativePalette.ApplyNativeMgt2SkinToPanel(tr.gameObject);
            }
        }

        private void CreateHubRow(float y, string title, string btnLabel, bool primaryBlue, Action onClick)
        {
            var row = new GameObject("HubRow");
            row.transform.SetParent(_rowsContainer, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 48f);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = row.AddComponent<Image>();
            bg.color = Mgt2NativePalette.RecessedBoxGrey;

            var txtGo = new GameObject("RowText");
            txtGo.transform.SetParent(row.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.offsetMin = new Vector2(16f, 4f);
            txtRt.offsetMax = new Vector2(-210f, -4f);

            var txt = txtGo.AddComponent<Text>();
            txt.font = Mgt2NativePalette.GetNativeGameFont();
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Mgt2NativePalette.TextDarkCharcoal;
            txt.raycastTarget = false;
            txt.text = title;

            Color c = primaryBlue ? Mgt2NativePalette.ButtonSteelBlue : Mgt2NativePalette.ButtonSilverMetallic;
            var btnGo = SpawnButton(row.transform, btnLabel, c, onClick);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.sizeDelta = new Vector2(185f, 34f);
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
                t.font = Mgt2NativePalette.GetNativeGameFont();
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Mgt2NativePalette.TextDarkCharcoal;
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
                img.raycastTarget = true;
            }

            var label = go.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
                label.fontSize = 12;
                label.fontStyle = FontStyle.Bold;
                label.color = Mgt2NativePalette.TextDarkCharcoal;
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
