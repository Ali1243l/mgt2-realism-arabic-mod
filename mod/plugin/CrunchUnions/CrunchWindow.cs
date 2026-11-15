using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal class CrunchWindow : MonoBehaviour
    {
        private static CrunchWindow _instance;
        private GameObject _rootPanel;
        private Text _titleText;
        private Text _statsText;
        private Transform _rowsContainer;
        private GameObject _buttonProto;

        public static void Init()
        {
            if (_instance != null) return;
            var go = new GameObject("MGT2_CrunchWindow_Host");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CrunchWindow>();
        }

        public static void Toggle()
        {
            if (_instance == null) Init();
            if (_instance != null) _instance.ToggleWindow();
        }

        private void Update()
        {
            bool combo = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Alpha1);
            if (Input.GetKeyDown(KeyCode.F12) || combo)
            {
                ToggleWindow();
            }
        }

        private static bool IsArabic()
        {
            var mainGo = GameObject.Find("Main");
            if (mainGo == null) return false;
            var settings = mainGo.GetComponent<settingsScript>();
            return settings != null && settings.language == 13;
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

            _rootPanel = new GameObject("Panel_CrunchUnions");
            _rootPanel.transform.SetParent(canvas.transform, false);
            var rt = _rootPanel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(780f, 520f);
            rt.anchoredPosition = Vector2.zero;

            var bg = _rootPanel.AddComponent<Image>();
            bg.color = new Color(0.11f, 0.09f, 0.13f, 0.97f);

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
            _titleText.color = new Color(1f, 0.68f, 0.32f, 1f);
            _titleText.raycastTarget = false;

            var statsGo = new GameObject("StatsSummary");
            statsGo.transform.SetParent(_rootPanel.transform, false);
            var statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 1f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.pivot = new Vector2(0.5f, 1f);
            statsRt.sizeDelta = new Vector2(-40f, 92f);
            statsRt.anchoredPosition = new Vector2(0f, -62f);

            _statsText = statsGo.AddComponent<Text>();
            _statsText.font = font;
            _statsText.fontSize = 14;
            _statsText.alignment = TextAnchor.MiddleCenter;
            _statsText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            _statsText.raycastTarget = false;

            var rowsGo = new GameObject("RowsContainer");
            rowsGo.transform.SetParent(_rootPanel.transform, false);
            var rowsRt = rowsGo.AddComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 1f);
            rowsRt.offsetMin = new Vector2(24f, 64f);
            rowsRt.offsetMax = new Vector2(-24f, -162f);
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

            bool ar = IsArabic();
            _titleText.text = ar
                ? "Crunch & Unions Studio Climate (F12 / Ctrl+1)"
                : "Studio Culture: Mandatory Crunch, Burnout & Labor Unions (F12)";

            var mainGo = GameObject.Find("Main");
            var mS = mainGo != null ? mainGo.GetComponent<mainScript>() : null;

            string strikeState = CrunchEngine.StrikeWeeksRemaining > 0
                ? $"STRIKE ACTIVE ({CrunchEngine.StrikeWeeksRemaining}w left)"
                : (CrunchEngine.MandatoryCrunchActive ? "MANDATORY CRUNCH (+50% Speed)" : "NORMAL SCHEDULE");

            _statsText.text =
                string.Format("Schedule: {0}  |  Dev Speed Mult: {1:F2}x  |  Burnout Index: {2:F1}%\n" +
                              "Union Drive: {3:F1}%  |  CBA Signed: {4}  |  Leak Risk: {5:F1}%\n" +
                              "Wellness Tier: {6}/2  |  Golden Handcuffs: {7} (Blocked Poaches: {8})",
                              strikeState,
                              CrunchEngine.GetDevSpeedMultiplier(),
                              CrunchEngine.BurnoutIndex,
                              CrunchEngine.UnionDriveMeter,
                              CrunchEngine.CbaSigned ? "YES" : "NO",
                              CrunchEngine.GetLeakRiskPercent(),
                              CrunchEngine.WellnessTier,
                              CrunchEngine.GoldenHandcuffsActive ? "ACTIVE" : "OFF",
                              CrunchEngine.TotalPoachAttemptsBlocked);

            float y = -10f;
            CreateActionRow(
                y,
                $"1. Mandatory Crunch Mode: {(CrunchEngine.MandatoryCrunchActive ? "ENABLED (+50% Dev Speed, +Burnout)" : "DISABLED (Normal Hours)")}",
                CrunchEngine.MandatoryCrunchActive ? "Stop Crunch" : "Enable Crunch",
                CrunchEngine.MandatoryCrunchActive ? new Color(0.80f, 0.36f, 0.18f, 1f) : new Color(0.68f, 0.22f, 0.24f, 1f),
                () => { CrunchEngine.ToggleCrunchMode(mS); RefreshContent(); }
            );
            y -= 58f;

            CreateActionRow(
                y,
                "2. Emergency Paid Sabbatical ($95k) -> -35% Burnout, Ends Active Strike Immediately",
                "Grant Sabbatical ($95k)",
                new Color(0.18f, 0.56f, 0.42f, 1f),
                () => { CrunchEngine.GrantPaidSabbatical(mS); RefreshContent(); }
            );
            y -= 58f;

            CreateActionRow(
                y,
                $"3. Collective Bargaining Agreement (CBA): {(CrunchEngine.CbaSigned ? "SIGNED (+12% Steady Speed, No Strikes)" : "Sign Union Charter ($150k)")}",
                CrunchEngine.CbaSigned ? "CBA Active" : "Sign CBA ($150k)",
                new Color(0.24f, 0.48f, 0.78f, 1f),
                () => { CrunchEngine.SignCollectiveBargainingAgreement(mS); RefreshContent(); }
            );
            y -= 58f;

            CreateActionRow(
                y,
                $"4. Studio Wellness & Clinic Tier ({CrunchEngine.WellnessTier}/2) -> Reduces Weekly Burnout",
                CrunchEngine.WellnessTier >= 2 ? "Max Tier" : "Upgrade Wellness",
                new Color(0.45f, 0.34f, 0.74f, 1f),
                () => { CrunchEngine.UpgradeWellness(mS); RefreshContent(); }
            );
            y -= 58f;

            CreateActionRow(
                y,
                $"5. Golden Handcuffs Retention Stock Pool ($110k) -> Blocks Rival Studio Talent Poaching",
                CrunchEngine.GoldenHandcuffsActive ? "Disable Pool" : "Activate ($110k)",
                new Color(0.72f, 0.54f, 0.16f, 1f),
                () => { CrunchEngine.ToggleGoldenHandcuffs(mS); RefreshContent(); }
            );
        }

        private void CreateActionRow(float y, string label, string btnLabel, Color btnColor, Action onClick)
        {
            var row = new GameObject("CrunchRow");
            row.transform.SetParent(_rowsContainer, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 50f);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.17f, 0.15f, 0.21f, 0.95f);

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
