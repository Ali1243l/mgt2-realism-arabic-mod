using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal class SalesWishlistWindow : MonoBehaviour
    {
        private static SalesWishlistWindow _instance;
        private GameObject _rootPanel;
        private Text _titleText;
        private Text _statsText;
        private Transform _rowsContainer;
        private GameObject _buttonProto;

        public static void Init()
        {
            if (_instance != null) return;
            var go = new GameObject("MGT2_SalesWishlistWindow_Host");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SalesWishlistWindow>();
        }

        public static void Toggle()
        {
            if (_instance == null) Init();
            if (_instance != null) _instance.ToggleWindow();
        }

        private void Update()
        {
            bool combo = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Alpha3);
            if (Input.GetKeyDown(KeyCode.End) || combo)
            {
                ToggleWindow();
            }
        }

        private void ToggleWindow()
        {
            if (_rootPanel == null)
                BuildUi();
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

            _rootPanel = new GameObject("Panel_SalesWishlists");
            _rootPanel.transform.SetParent(canvas.transform, false);
            var rt = _rootPanel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800f, 520f);
            rt.anchoredPosition = Vector2.zero;

            var bg = _rootPanel.AddComponent<Image>();
            bg.color = new Color(0.09f, 0.14f, 0.11f, 0.97f);

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_rootPanel.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-32f, 44f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);

            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 18;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(0.42f, 0.95f, 0.62f, 1f);
            _titleText.raycastTarget = false;

            var statsGo = new GameObject("StatsSummary");
            statsGo.transform.SetParent(_rootPanel.transform, false);
            var statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 1f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.pivot = new Vector2(0.5f, 1f);
            statsRt.sizeDelta = new Vector2(-40f, 88f);
            statsRt.anchoredPosition = new Vector2(0f, -62f);

            _statsText = statsGo.AddComponent<Text>();
            _statsText.font = font;
            _statsText.fontSize = 14;
            _statsText.alignment = TextAnchor.MiddleCenter;
            _statsText.color = new Color(0.92f, 0.98f, 0.94f, 1f);
            _statsText.raycastTarget = false;

            var rowsGo = new GameObject("RowsContainer");
            rowsGo.transform.SetParent(_rootPanel.transform, false);
            var rowsRt = rowsGo.AddComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 1f);
            rowsRt.offsetMin = new Vector2(24f, 64f);
            rowsRt.offsetMax = new Vector2(-24f, -158f);
            _rowsContainer = rowsGo.transform;

            var closeBtnGo = SpawnButton(_rootPanel.transform, "X", new Color(0.75f, 0.22f, 0.22f, 1f), () => _rootPanel.SetActive(false));
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(40f, 34f);
            closeRt.anchoredPosition = new Vector2(-14f, -12f);

            _rootPanel.SetActive(false);
        }

        private void RefreshContent()
        {
            if (_rowsContainer == null) return;
            for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
                Destroy(_rowsContainer.GetChild(i).gameObject);

            _titleText.text = "Dynamic Sales Festivals, Wishlists & Franchise Bundles (End / Ctrl+3)";

            var mainGo = GameObject.Find("Main");
            var mS = mainGo != null ? mainGo.GetComponent<mainScript>() : null;
            int month = mS != null ? mS.month : 1;

            _statsText.text =
                $"Store Calendar: {SalesWishlistEngine.GetFestivalLabel(month)}\n" +
                $"Total Unconverted Wishlists: {SalesWishlistEngine.GetTotalStudioWishlists():N0}  |  Brand Price Integrity: {SalesWishlistEngine.BrandPriceIntegrity:F0}%\n" +
                $"Active Franchise Bundles: {SalesWishlistEngine.ActiveFranchiseBundles}  |  Cumulative Wishlist Sales Revenue: USD {SalesWishlistEngine.TotalWishlistRevenue:N0}";

            float y = -10f;
            CreateActionRow(
                y,
                "Create Complete-the-Set Franchise Bundle ($55k) -> Generates Weekly Catalog Revenue +4% Price Integrity",
                "Create Bundle ($55k)",
                new Color(0.20f, 0.56f, 0.78f, 1f),
                () => { SalesWishlistEngine.CreateFranchiseBundle(mS); RefreshContent(); }
            );
            y -= 58f;

            var games = FindObjectsOfType<gameScript>();
            var activeGames = new List<gameScript>();
            foreach (var g in games)
            {
                if (g != null && g.isOnMarket)
                    activeGames.Add(g);
            }

            int shown = 0;
            for (int i = activeGames.Count - 1; i >= 0 && shown < 4; i--)
            {
                var g = activeGames[i];
                int wish = SalesWishlistEngine.GameWishlists.TryGetValue(g.myID, out int w) ? w : 15000;
                string earlyWarn = g.weeksOnMarket <= 4 ? " [EARLY <=4w]" : "";
                CreateDualDiscountRow(
                    y,
                    $"{g.GetNameSimple()} | Wishlists: {wish:N0} | Weeks: {g.weeksOnMarket}{earlyWarn}",
                    () => { SalesWishlistEngine.TriggerTacticalDiscount(mS, g, 0.20f); RefreshContent(); },
                    () => { SalesWishlistEngine.TriggerTacticalDiscount(mS, g, 0.50f); RefreshContent(); }
                );
                y -= 58f;
                shown++;
            }
        }

        private void CreateActionRow(float y, string label, string btnLabel, Color btnColor, Action onClick)
        {
            var row = new GameObject("WishlistBundleRow");
            row.transform.SetParent(_rowsContainer, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 50f);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.21f, 0.17f, 0.95f);

            var txtGo = new GameObject("RowText");
            txtGo.transform.SetParent(row.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.offsetMin = new Vector2(14f, 4f);
            txtRt.offsetMax = new Vector2(-215f, -4f);

            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.text = label;

            var btnGo = SpawnButton(row.transform, btnLabel, btnColor, onClick);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.sizeDelta = new Vector2(190f, 36f);
            btnRt.anchoredPosition = new Vector2(-12f, 0f);
        }

        private void CreateDualDiscountRow(float y, string label, Action on20, Action on50)
        {
            var row = new GameObject("GameSaleRow");
            row.transform.SetParent(_rowsContainer, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 50f);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.20f, 0.16f, 0.95f);

            var txtGo = new GameObject("RowText");
            txtGo.transform.SetParent(row.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.offsetMin = new Vector2(14f, 4f);
            txtRt.offsetMax = new Vector2(-300f, -4f);

            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.text = label;

            var btn20 = SpawnButton(row.transform, "-20% Alert", new Color(0.20f, 0.62f, 0.36f, 1f), on20);
            var rt20 = btn20.GetComponent<RectTransform>();
            rt20.anchorMin = new Vector2(1f, 0.5f);
            rt20.anchorMax = new Vector2(1f, 0.5f);
            rt20.pivot = new Vector2(1f, 0.5f);
            rt20.sizeDelta = new Vector2(132f, 36f);
            rt20.anchoredPosition = new Vector2(-152f, 0f);

            var btn50 = SpawnButton(row.transform, "-50% Flash", new Color(0.80f, 0.44f, 0.16f, 1f), on50);
            var rt50 = btn50.GetComponent<RectTransform>();
            rt50.anchorMin = new Vector2(1f, 0.5f);
            rt50.anchorMax = new Vector2(1f, 0.5f);
            rt50.pivot = new Vector2(1f, 0.5f);
            rt50.sizeDelta = new Vector2(132f, 36f);
            rt50.anchoredPosition = new Vector2(-12f, 0f);
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
                t.color = Color.white;
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
                label.color = Color.white;
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
