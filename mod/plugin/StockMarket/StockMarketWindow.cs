using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Native Mad Games Tycoon 2 UI Window for Stock Market and Player IPO.
    /// Clones game styling, sprites, fonts, sound effects, and menu lifecycle.
    /// Supports two tabs:
    ///   Tab 1: "Market & Competitors" (AI competitor trading)
    ///   Tab 2: "My Studio & Board (IPO)" (Player studio valuation, IPO launch, board governance, dividends)
    /// </summary>
    public class StockMarketWindow : MonoBehaviour
    {
        public static StockMarketWindow Instance { get; private set; }

        public enum ActiveTab
        {
            MarketAndCompetitors = 0,
            MyStudioAndBoard = 1
        }

        // Core references
        private mainScript _ms;
        private GUI_Main _guiMain;
        private sfxScript _sfx;
        private Canvas _canvas;

        // Window UI Root & Frames
        private GameObject _windowRoot;
        private RectTransform _windowMainRt;
        private Text _titleText;

        // Tab Navigation
        private ActiveTab _currentTab = ActiveTab.MarketAndCompetitors;
        private Button _tab1Btn;
        private Button _tab2Btn;
        private Image _tab1Bg;
        private Image _tab2Bg;
        private Text _tab1Label;
        private Text _tab2Label;

        // Tab 1 Panels & Content
        private GameObject _tab1Panel;
        private Text _marketIndexText;
        private Text _portfolioValText;
        private Text _cashText;
        private RectTransform _competitorListContent;
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        // Tab 2 Panels & Content
        private GameObject _tab2Panel;
        private float _selectedOfferingPct = 0.20f; // 20% default
        private readonly float[] _offeringSizes = new float[] { 0.10f, 0.20f, 0.30f, 0.40f };
        private readonly Button[] _offeringButtons = new Button[4];
        private readonly Image[] _offeringButtonBgs = new Image[4];
        private readonly Text[] _offeringButtonTexts = new Text[4];

        // Dynamic Elements Tab 2
        private GameObject _preIpoContainer;
        private GameObject _postIpoContainer;
        private Text _studioValText;
        private Text _ipoBreakdownText;
        private Text _netProceedsText;
        private Button _launchIpoBtn;

        // Post-IPO elements
        private Text _sharePriceText;
        private Text _marketCapText;
        private Text _sharesDistributionText;
        private Text _boardSentimentBadge;
        private Image _boardSentimentBadgeBg;
        private Image _discontentFill;
        private Text _discontentLabel;
        private Image _confidenceFill;
        private Text _confidenceLabel;
        private GameObject _spendingCapWarning;
        private Text _quarterlyTargetText;
        private Text _quarterlyProgressText;
        private Image _targetProgressBarFill;
        private Text _streakText;
        private Text _dividendRecommendedText;
        private Button _payDividendBtn;
        private Button _emergencyDividendBtn;
        private Button _buybackBtn;
        private Button _poisonPillBtn;

        private float _nextAutoRefreshTime;
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        #region Static API

        public static void Show(mainScript ms, GUI_Main guiMain)
        {
            var win = GetOrCreate();
            win._ms = ms;
            win._guiMain = guiMain;
            win.ShowWindow();
        }

        public static void Refresh(mainScript ms, GUI_Main guiMain)
        {
            if (Instance != null)
            {
                Instance._ms = ms;
                Instance._guiMain = guiMain;
                Instance.RefreshActiveTab();
            }
        }

        public static StockMarketWindow GetOrCreate()
        {
            if (Instance != null) return Instance;
            var host = new GameObject("MGT2_StockMarketWindow_Host");
            DontDestroyOnLoad(host);
            return host.AddComponent<StockMarketWindow>();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_windowRoot != null && _windowRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseWindow();
                    return;
                }

                if (Time.unscaledTime >= _nextAutoRefreshTime)
                {
                    _nextAutoRefreshTime = Time.unscaledTime + 2.0f;
                    RefreshActiveTab();
                }
            }
        }

        #endregion

        #region Window Show / Close

        public void ShowWindow()
        {
            FindScripts();
            UpdateLanguage();

            if (_windowRoot == null)
            {
                BuildNativeWindow();
            }

            _windowRoot.SetActive(true);

            if (_guiMain != null)
            {
                _guiMain.OpenMenu(hideChars: false);
            }
            else if (_ms != null && !_ms.multiplayer)
            {
                _ms.PauseGame(true);
            }

            PlaySound(5); // game menu open sound
            SwitchTab(_currentTab);
        }

        public void CloseWindow()
        {
            if (_windowRoot != null)
            {
                _windowRoot.SetActive(false);
            }

            if (_guiMain != null)
            {
                _guiMain.CloseMenu();
            }
            else if (_ms != null && !_ms.multiplayer)
            {
                _ms.PauseGame(false);
            }

            PlaySound(3); // game click/close sound
        }

        private void FindScripts()
        {
            if (!_ms)
            {
                var main = GameObject.Find("Main");
                if (main) _ms = main.GetComponent<mainScript>();
            }
            if (!_guiMain)
            {
                var menu = GameObject.Find("CanvasInGameMenu");
                if (menu) _guiMain = menu.GetComponent<GUI_Main>();
            }
            if (!_sfx)
            {
                var sfxObj = GameObject.Find("SFX");
                if (sfxObj) _sfx = sfxObj.GetComponent<sfxScript>();
            }
            if (!_canvas)
            {
                var menu = GameObject.Find("CanvasInGameMenu");
                if (menu) _canvas = menu.GetComponent<Canvas>();
                if (!_canvas) _canvas = FindObjectOfType<Canvas>();
            }
        }

        private void UpdateLanguage()
        {
            var settings = _ms != null ? _ms.GetComponent<settingsScript>() : null;
            StockMarketLabels.Arabic = settings != null && settings.language == 13;
        }

        private void PlaySound(int id)
        {
            try
            {
                if (_sfx != null)
                {
                    _sfx.PlaySound(id, force: true);
                }
            }
            catch { }
        }

        #endregion

        #region Native Asset Helpers

        public static Sprite GetGameSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_spriteCache.TryGetValue(name, out var s) && s != null) return s;

            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var spr in allSprites)
            {
                if (spr != null && spr.name == name)
                {
                    _spriteCache[name] = spr;
                    return spr;
                }
            }
            return null;
        }

        private Font GetGameFont()
        {
            if (_ms != null && _ms.fonts != null && _ms.fonts.Length > 0 && _ms.fonts[0] != null)
            {
                return _ms.fonts[0];
            }
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        #endregion

        #region Native Window Hierarchy Builder

        private void BuildNativeWindow()
        {
            FindScripts();

            // Root container inside CanvasInGameMenu
            _windowRoot = new GameObject("MGT2_StockMarket_WindowRoot", typeof(RectTransform));
            _windowRoot.transform.SetParent(_canvas.transform, false);

            var rootRt = (RectTransform)_windowRoot.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;

            // Faint raycast blocker to prevent clicking underlying 3D world
            var blockerImg = _windowRoot.AddComponent<Image>();
            blockerImg.color = new Color(0f, 0f, 0f, 0.45f);

            // WindowMain (Centered authentic dialog frame)
            var windowMain = new GameObject("WindowMain", typeof(RectTransform), typeof(Image));
            windowMain.transform.SetParent(_windowRoot.transform, false);

            _windowMainRt = (RectTransform)windowMain.transform;
            _windowMainRt.sizeDelta = new Vector2(850f, 680f);
            _windowMainRt.anchoredPosition = Vector2.zero;

            var borderImg = windowMain.GetComponent<Image>();
            borderImg.sprite = GetGameSprite("window1_Border");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;

            // Fill_Window (Warm parchment background gradient matching the game)
            var fillWindow = new GameObject("Fill_Window", typeof(RectTransform), typeof(Image));
            fillWindow.transform.SetParent(windowMain.transform, false);
            var fillRt = (RectTransform)fillWindow.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;

            var fillImg = fillWindow.GetComponent<Image>();
            fillImg.sprite = GetGameSprite("window1_FillVerlauf");
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.92f, 0.925f, 0.935f, 1f);

            // Titelleiste (Signature MGT2 Blue Title Bar)
            var titelleiste = new GameObject("Titelleiste", typeof(RectTransform), typeof(Image));
            titelleiste.transform.SetParent(windowMain.transform, false);
            var titleRt = (RectTransform)titelleiste.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            titleRt.sizeDelta = new Vector2(-16f, 52f);

            var titleImg = titelleiste.GetComponent<Image>();
            titleImg.sprite = GetGameSprite("titleLeiste");
            titleImg.type = Image.Type.Sliced;
            titleImg.color = new Color(0.204f, 0.596f, 0.859f, 1.0f);

            // Title Icon
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(titelleiste.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(30f, 0f);
            iconRt.sizeDelta = new Vector2(36f, 36f);

            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = GetGameSprite("iconDollar") ?? GetGameSprite("iconDevelopment");
            iconImg.color = Color.white;

            // Title Text
            var titleTextGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleTextGo.transform.SetParent(titelleiste.transform, false);
            var ttRt = (RectTransform)titleTextGo.transform;
            ttRt.anchorMin = new Vector2(0f, 0f);
            ttRt.anchorMax = new Vector2(1f, 1f);
            ttRt.offsetMin = new Vector2(60f, 0f);
            ttRt.offsetMax = new Vector2(-55f, 0f);

            _titleText = titleTextGo.GetComponent<Text>();
            _titleText.font = GetGameFont();
            _titleText.fontSize = 22;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.text = StockMarketLabels.Title;

            // Close Button
            var closeGo = new GameObject("Button_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(titelleiste.transform, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-26f, 0f);
            closeRt.sizeDelta = new Vector2(34f, 34f);

            var closeImg = closeGo.GetComponent<Image>();
            closeImg.sprite = GetGameSprite("close");
            closeImg.color = Color.white;

            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(CloseWindow);

            // Tab Navigation Bar
            BuildTabsHeader(windowMain.transform);

            // Content Panels
            BuildTab1Panel(windowMain.transform);
            BuildTab2Panel(windowMain.transform);
        }

        private void BuildTabsHeader(Transform parent)
        {
            var tabsContainer = new GameObject("TabsHeader", typeof(RectTransform));
            tabsContainer.transform.SetParent(parent, false);
            var tcRt = (RectTransform)tabsContainer.transform;
            tcRt.anchorMin = new Vector2(0f, 1f);
            tcRt.anchorMax = new Vector2(1f, 1f);
            tcRt.pivot = new Vector2(0.5f, 1f);
            tcRt.anchoredPosition = new Vector2(0f, -66f);
            tcRt.sizeDelta = new Vector2(-28f, 36f);

            // Tab 1 Button
            var tab1Go = new GameObject("Tab1_Market", typeof(RectTransform), typeof(Image), typeof(Button));
            tab1Go.transform.SetParent(tabsContainer.transform, false);
            var t1Rt = (RectTransform)tab1Go.transform;
            t1Rt.anchorMin = new Vector2(0f, 0f);
            t1Rt.anchorMax = new Vector2(0.5f, 1f);
            t1Rt.offsetMin = new Vector2(0f, 0f);
            t1Rt.offsetMax = new Vector2(-4f, 0f);

            _tab1Bg = tab1Go.GetComponent<Image>();
            _tab1Bg.sprite = GetGameSprite("uiSquareVerlauf") ?? GetGameSprite("whiteSimple");
            _tab1Bg.type = Image.Type.Sliced;
            _tab1Btn = tab1Go.GetComponent<Button>();
            _tab1Btn.onClick.AddListener(() => SwitchTab(ActiveTab.MarketAndCompetitors));

            var t1TextGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            t1TextGo.transform.SetParent(tab1Go.transform, false);
            var t1tRt = (RectTransform)t1TextGo.transform;
            t1tRt.anchorMin = Vector2.zero;
            t1tRt.anchorMax = Vector2.one;
            t1tRt.sizeDelta = Vector2.zero;
            _tab1Label = t1TextGo.GetComponent<Text>();
            _tab1Label.font = GetGameFont();
            _tab1Label.fontSize = 15;
            _tab1Label.fontStyle = FontStyle.Bold;
            _tab1Label.alignment = TextAnchor.MiddleCenter;

            // Tab 2 Button
            var tab2Go = new GameObject("Tab2_Studio", typeof(RectTransform), typeof(Image), typeof(Button));
            tab2Go.transform.SetParent(tabsContainer.transform, false);
            var t2Rt = (RectTransform)tab2Go.transform;
            t2Rt.anchorMin = new Vector2(0.5f, 0f);
            t2Rt.anchorMax = new Vector2(1f, 1f);
            t2Rt.offsetMin = new Vector2(4f, 0f);
            t2Rt.offsetMax = new Vector2(0f, 0f);

            _tab2Bg = tab2Go.GetComponent<Image>();
            _tab2Bg.sprite = GetGameSprite("uiSquareVerlauf") ?? GetGameSprite("whiteSimple");
            _tab2Bg.type = Image.Type.Sliced;
            _tab2Btn = tab2Go.GetComponent<Button>();
            _tab2Btn.onClick.AddListener(() => SwitchTab(ActiveTab.MyStudioAndBoard));

            var t2TextGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            t2TextGo.transform.SetParent(tab2Go.transform, false);
            var t2tRt = (RectTransform)t2TextGo.transform;
            t2tRt.anchorMin = Vector2.zero;
            t2tRt.anchorMax = Vector2.one;
            t2tRt.sizeDelta = Vector2.zero;
            _tab2Label = t2TextGo.GetComponent<Text>();
            _tab2Label.font = GetGameFont();
            _tab2Label.fontSize = 15;
            _tab2Label.fontStyle = FontStyle.Bold;
            _tab2Label.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        #region Tab 1: Market & Competitors UI

        private void BuildTab1Panel(Transform parent)
        {
            _tab1Panel = new GameObject("Tab1Panel", typeof(RectTransform));
            _tab1Panel.transform.SetParent(parent, false);
            var pRt = (RectTransform)_tab1Panel.transform;
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(16f, 16f);
            pRt.offsetMax = new Vector2(-16f, -108f);

            // Summary Bar Card
            var summaryBar = CreateNativeCard(_tab1Panel.transform, "SummaryBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 44f));
            var sbRt = (RectTransform)summaryBar.transform;

            _marketIndexText = CreateText(summaryBar.transform, "MarketIndex", new Vector2(0f, 0f), new Vector2(0.33f, 1f), new Vector2(14f, 0f), new Vector2(0f, 0f), TextAnchor.MiddleLeft, 14, true);
            _portfolioValText = CreateText(summaryBar.transform, "PortfolioVal", new Vector2(0.33f, 0f), new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14, true);
            _cashText = CreateText(summaryBar.transform, "CashVal", new Vector2(0.66f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-14f, 0f), TextAnchor.MiddleRight, 14, true);

            // Table Header Bar
            var colHeader = CreateNativeCard(_tab1Panel.transform, "ColHeader", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(0f, 28f));
            CreateText(colHeader.transform, "ColCompany", new Vector2(0.02f, 0f), new Vector2(0.30f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, true, StockMarketLabels.Competitors);
            CreateText(colHeader.transform, "ColValuation", new Vector2(0.30f, 0f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 13, true, StockMarketLabels.Valuation);
            CreateText(colHeader.transform, "ColSharePrice", new Vector2(0.48f, 0f), new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 13, true, StockMarketLabels.SharePriceTrend);
            CreateText(colHeader.transform, "ColStake", new Vector2(0.66f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 13, true, StockMarketLabels.Owned);
            CreateText(colHeader.transform, "ColActions", new Vector2(0.78f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 13, true, StockMarketLabels.TradeActions);

            // Scroll View for Competitors
            var scrollGo = new GameObject("CompetitorsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_tab1Panel.transform, false);
            var sRt = (RectTransform)scrollGo.transform;
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(0f, 0f);
            sRt.offsetMax = new Vector2(0f, -80f);

            var sBg = scrollGo.GetComponent<Image>();
            sBg.sprite = GetGameSprite("uiBorder");
            sBg.type = Image.Type.Sliced;
            sBg.color = new Color(0.85f, 0.86f, 0.88f, 0.6f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vRt = (RectTransform)viewport.transform;
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.offsetMin = new Vector2(4f, 4f);
            vRt.offsetMax = new Vector2(-4f, -4f);

            var vImg = viewport.GetComponent<Image>();
            vImg.sprite = GetGameSprite("whiteSimple");
            vImg.color = new Color(1f, 1f, 1f, 0.05f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _competitorListContent = (RectTransform)content.transform;
            _competitorListContent.anchorMin = new Vector2(0f, 1f);
            _competitorListContent.anchorMax = new Vector2(1f, 1f);
            _competitorListContent.pivot = new Vector2(0.5f, 1f);
            _competitorListContent.sizeDelta = new Vector2(0f, 0f);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            layout.padding = new RectOffset(4, 4, 4, 4);

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = _competitorListContent;
            scroll.viewport = vRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;
        }

        #endregion

        #region Tab 2: My Studio & Board (IPO) UI

        private void BuildTab2Panel(Transform parent)
        {
            _tab2Panel = new GameObject("Tab2Panel", typeof(RectTransform));
            _tab2Panel.transform.SetParent(parent, false);
            var pRt = (RectTransform)_tab2Panel.transform;
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(16f, 16f);
            pRt.offsetMax = new Vector2(-16f, -108f);

            // Container A: Pre-IPO
            BuildPreIpoView(_tab2Panel.transform);

            // Container B: Post-IPO
            BuildPostIpoView(_tab2Panel.transform);
        }

        private void BuildPreIpoView(Transform parent)
        {
            _preIpoContainer = new GameObject("PreIpoContainer", typeof(RectTransform));
            _preIpoContainer.transform.SetParent(parent, false);
            var cRt = (RectTransform)_preIpoContainer.transform;
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.sizeDelta = Vector2.zero;

            // Card 1: Studio Valuation Breakdown
            var cardVal = CreateNativeCard(_preIpoContainer.transform, "CardValuation", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 140f));
            CreateText(cardVal.transform, "Header", new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 16, true, StockMarketLabels.IpoTitle);
            _studioValText = CreateText(cardVal.transform, "ValuationDetails", new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.70f), Vector2.zero, Vector2.zero, TextAnchor.UpperLeft, 14, false);

            // Card 2: Offering Size & Proceeds Configuration
            var cardConfig = CreateNativeCard(_preIpoContainer.transform, "CardConfig", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(0f, 210f));
            CreateText(cardConfig.transform, "Header", new Vector2(0.02f, 0.84f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 16, true, StockMarketLabels.OfferingSize);

            // 4 Offering Size Buttons (10%, 20%, 30%, 40%) - Distributed across full width
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                float pct = _offeringSizes[i];
                float xMin = 0.02f + i * 0.245f;
                float xMax = xMin + 0.225f;

                var btn = CreateNativeButton(cardConfig.transform, $"SizeBtn_{pct}", Vector2.zero, Vector2.zero, () =>
                {
                    _selectedOfferingPct = pct;
                    UpdateOfferingButtonsState();
                    RefreshPreIpoCalculations();
                }, new Vector2(xMin, 0.62f), new Vector2(xMax, 0.82f), new Vector2(0.5f, 0.5f));

                _offeringButtons[i] = btn.GetComponent<Button>();
                _offeringButtonBgs[i] = btn.GetComponent<Image>();
                _offeringButtonTexts[i] = btn.GetComponentInChildren<Text>();
                _offeringButtonTexts[i].text = $"{Mathf.RoundToInt(pct * 100f)}%";
                _offeringButtonTexts[i].fontSize = 14;
                _offeringButtonTexts[i].fontStyle = FontStyle.Bold;
            }

            // Inner Financial Ledger Box
            var ledgerBox = CreateNativeCard(cardConfig.transform, "LedgerBox", new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var ledgerImg = ledgerBox.GetComponent<Image>();
            ledgerImg.color = new Color(0.96f, 0.97f, 0.99f, 0.95f);

            _ipoBreakdownText = CreateText(ledgerBox.transform, "Breakdown", new Vector2(0.03f, 0.08f), new Vector2(0.58f, 0.92f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 14, false);
            _netProceedsText = CreateText(ledgerBox.transform, "NetProceeds", new Vector2(0.60f, 0.08f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero, TextAnchor.MiddleRight, 16, true);

            // Card 3: Launch Action Banner
            var cardAction = CreateNativeCard(_preIpoContainer.transform, "CardAction", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -366f), new Vector2(0f, 90f));
            CreateText(cardAction.transform, "Desc", new Vector2(0.02f, 0.15f), new Vector2(0.68f, 0.85f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, false, StockMarketLabels.IpoDesc);

            _launchIpoBtn = CreateNativeButton(cardAction.transform, "LaunchIpoBtn", new Vector2(-20f, 0f), new Vector2(280f, 48f), OnLaunchIpoClicked, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).GetComponent<Button>();
            _launchIpoBtn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f); // Vibrant native green
            var launchTxt = _launchIpoBtn.GetComponentInChildren<Text>();
            launchTxt.text = StockMarketLabels.LaunchIpo;
            launchTxt.fontSize = 15;
            launchTxt.fontStyle = FontStyle.Bold;
        }

        private void BuildPostIpoView(Transform parent)
        {
            _postIpoContainer = new GameObject("PostIpoContainer", typeof(RectTransform));
            _postIpoContainer.transform.SetParent(parent, false);
            var cRt = (RectTransform)_postIpoContainer.transform;
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.sizeDelta = Vector2.zero;

            // Card 1: Public Stock & Valuation (Top Left)
            var cardStock = CreateNativeCard(_postIpoContainer.transform, "CardStock", new Vector2(0f, 0.52f), new Vector2(0.49f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            CreateText(cardStock.transform, "Header", new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 16, true, StockMarketLabels.Title);
            _sharePriceText = CreateText(cardStock.transform, "SharePrice", new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, false);
            _marketCapText = CreateText(cardStock.transform, "MarketCap", new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.52f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, false);
            _sharesDistributionText = CreateText(cardStock.transform, "SharesDist", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.26f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 14, false);

            // Card 2: Board of Directors & Sentiment (Top Right)
            var cardBoard = CreateNativeCard(_postIpoContainer.transform, "CardBoard", new Vector2(0.51f, 0.52f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            CreateText(cardBoard.transform, "Header", new Vector2(0.04f, 0.80f), new Vector2(0.60f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 16, true, StockMarketLabels.BoardTitle);

            // Sentiment Badge
            var badgeGo = new GameObject("SentimentBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(cardBoard.transform, false);
            var bRt = (RectTransform)badgeGo.transform;
            bRt.anchorMin = new Vector2(0.62f, 0.80f);
            bRt.anchorMax = new Vector2(0.96f, 0.96f);
            bRt.sizeDelta = Vector2.zero;
            _boardSentimentBadgeBg = badgeGo.GetComponent<Image>();
            _boardSentimentBadgeBg.sprite = GetGameSprite("uiSquareVerlauf") ?? GetGameSprite("whiteSimple");
            _boardSentimentBadgeBg.type = Image.Type.Sliced;

            var badgeTxtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            badgeTxtGo.transform.SetParent(badgeGo.transform, false);
            var btRt = (RectTransform)badgeTxtGo.transform;
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;
            _boardSentimentBadge = badgeTxtGo.GetComponent<Text>();
            _boardSentimentBadge.font = GetGameFont();
            _boardSentimentBadge.fontSize = 13;
            _boardSentimentBadge.fontStyle = FontStyle.Bold;
            _boardSentimentBadge.color = Color.white;
            _boardSentimentBadge.alignment = TextAnchor.MiddleCenter;

            // Discontent bar
            _discontentLabel = CreateText(cardBoard.transform, "DiscontentLbl", new Vector2(0.04f, 0.54f), new Vector2(0.96f, 0.74f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 14, false);
            CreateProgressBar(cardBoard.transform, "DiscontentBar", new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.52f), out _discontentFill, new Color(0.85f, 0.25f, 0.25f, 1f));

            // Confidence bar
            _confidenceLabel = CreateText(cardBoard.transform, "ConfidenceLbl", new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.42f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 14, false);
            CreateProgressBar(cardBoard.transform, "ConfidenceBar", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.20f), out _confidenceFill, new Color(0.18f, 0.65f, 0.35f, 1f));

            // Spending cap warning
            _spendingCapWarning = CreateText(cardBoard.transform, "SpendingCapWarning", new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 12, true, StockMarketLabels.SpendingRestricted).gameObject;
            _spendingCapWarning.GetComponent<Text>().color = new Color(0.85f, 0.15f, 0.15f, 1f);

            // Card 3: Quarterly Performance & Target (Bottom Left)
            var cardTarget = CreateNativeCard(_postIpoContainer.transform, "CardTarget", new Vector2(0f, 0f), new Vector2(0.49f, 0.50f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
            CreateText(cardTarget.transform, "Header", new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 16, true, StockMarketLabels.QuarterlyTarget);
            _quarterlyTargetText = CreateText(cardTarget.transform, "TargetVal", new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, false);
            _quarterlyProgressText = CreateText(cardTarget.transform, "ProgressVal", new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.54f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 14, false);
            CreateProgressBar(cardTarget.transform, "TargetBar", new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.30f), out _targetProgressBarFill, new Color(0.161f, 0.502f, 0.725f, 1f));
            _streakText = CreateText(cardTarget.transform, "Streak", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.20f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, false);

            // Card 4: Dividends & Shareholder Relations (Bottom Right)
            var cardDividends = CreateNativeCard(_postIpoContainer.transform, "CardDividends", new Vector2(0.51f, 0f), new Vector2(1f, 0.50f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            CreateText(cardDividends.transform, "Header", new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, true, StockMarketLabels.DividendsTitle);
            _dividendRecommendedText = CreateText(cardDividends.transform, "RecDiv", new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, false);

            float btnW = 180f;
            float btnH = 36f;

            // Row 1: Dividends
            _payDividendBtn = CreateNativeButton(cardDividends.transform, "PayDivBtn", new Vector2(16f, 0f), new Vector2(btnW, btnH), OnPayDividendClicked, new Vector2(0f, 0.44f), new Vector2(0f, 0.44f), new Vector2(0f, 0.5f)).GetComponent<Button>();
            _payDividendBtn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f);
            var payTxt = _payDividendBtn.GetComponentInChildren<Text>();
            payTxt.text = StockMarketLabels.PayDividend;
            payTxt.fontSize = 12;
            payTxt.fontStyle = FontStyle.Bold;

            _emergencyDividendBtn = CreateNativeButton(cardDividends.transform, "EmergDivBtn", new Vector2(-16f, 0f), new Vector2(btnW, btnH), OnPayEmergencyDividendClicked, new Vector2(1f, 0.44f), new Vector2(1f, 0.44f), new Vector2(1f, 0.5f)).GetComponent<Button>();
            _emergencyDividendBtn.GetComponent<Image>().color = new Color(0.85f, 0.45f, 0.15f, 1f);
            var emergTxt = _emergencyDividendBtn.GetComponentInChildren<Text>();
            emergTxt.text = StockMarketLabels.EmergencyDividend;
            emergTxt.fontSize = 11;
            emergTxt.fontStyle = FontStyle.Bold;

            // Row 2: Defense (Buyback + Poison Pill)
            _buybackBtn = CreateNativeButton(cardDividends.transform, "BuybackBtn", new Vector2(16f, 0f), new Vector2(btnW, btnH), OnShareBuybackClicked, new Vector2(0f, 0.16f), new Vector2(0f, 0.16f), new Vector2(0f, 0.5f)).GetComponent<Button>();
            _buybackBtn.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.80f, 1f);
            var buybTxt = _buybackBtn.GetComponentInChildren<Text>();
            buybTxt.text = StockMarketLabels.ShareBuyback;
            buybTxt.fontSize = 11;
            buybTxt.fontStyle = FontStyle.Bold;

            _poisonPillBtn = CreateNativeButton(cardDividends.transform, "PoisonPillBtn", new Vector2(-16f, 0f), new Vector2(btnW, btnH), OnPoisonPillClicked, new Vector2(1f, 0.16f), new Vector2(1f, 0.16f), new Vector2(1f, 0.5f)).GetComponent<Button>();
            _poisonPillBtn.GetComponent<Image>().color = new Color(0.75f, 0.20f, 0.20f, 1f);
            var poisTxt = _poisonPillBtn.GetComponentInChildren<Text>();
            poisTxt.text = StockMarketLabels.PoisonPillBtn;
            poisTxt.fontSize = 11;
            poisTxt.fontStyle = FontStyle.Bold;
        }

        #endregion

        #region UI Component Factories

        private GameObject CreateNativeCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = go.GetComponent<Image>();
            img.sprite = GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.965f, 0.97f, 0.98f, 1f);

            // Subtle border outline
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(go.transform, false);
            var bRt = (RectTransform)border.transform;
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = Vector2.zero;
            var bImg = border.GetComponent<Image>();
            bImg.sprite = GetGameSprite("uiBorder");
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.70f, 0.74f, 0.80f, 0.5f);

            return go;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAnchor align, int fontSize, bool bold, string text = "")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var t = go.GetComponent<Text>();
            t.font = GetGameFont();
            t.fontSize = fontSize;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = new Color(0.151f, 0.151f, 0.151f, 1.0f);
            t.alignment = align;
            t.supportRichText = true;
            t.text = text;
            return t;
        }

        private GameObject CreateNativeButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var aMin = anchorMin ?? new Vector2(0f, 1f);
            var aMax = anchorMax ?? new Vector2(0f, 1f);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot ?? ((aMin == aMax) ? aMin : new Vector2(0.5f, 0.5f));
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf") ?? GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.161f, 0.502f, 0.725f, 1f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    PlaySound(3);
                    onClick();
                });
            }

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var tRt = (RectTransform)txtGo.transform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            var txt = txtGo.GetComponent<Text>();
            txt.font = GetGameFont();
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return go;
        }

        private void CreateProgressBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out Image fillImg, Color fillColor)
        {
            var bgGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(parent, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = anchorMin;
            bgRt.anchorMax = anchorMax;
            bgRt.sizeDelta = Vector2.zero;

            var bg = bgGo.GetComponent<Image>();
            bg.sprite = GetGameSprite("whiteSimple");
            bg.color = new Color(0.80f, 0.82f, 0.85f, 1f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = Vector2.zero;

            fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = GetGameSprite("whiteSimple");
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.5f;
            fillImg.color = fillColor;
        }

        #endregion

        #region Tab Switching & UI Refresh

        public void SwitchTab(ActiveTab tab)
        {
            _currentTab = tab;
            PlaySound(3);

            bool isTab1 = tab == ActiveTab.MarketAndCompetitors;
            _tab1Panel.SetActive(isTab1);
            _tab2Panel.SetActive(!isTab1);

            // Tab button styles
            Color colActive = new Color(0.161f, 0.502f, 0.725f, 1f);
            Color colInactive = new Color(0.80f, 0.82f, 0.85f, 1f);

            _tab1Bg.color = isTab1 ? colActive : colInactive;
            _tab2Bg.color = !isTab1 ? colActive : colInactive;

            _tab1Label.text = StockMarketLabels.TabMarket;
            _tab1Label.color = isTab1 ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);

            _tab2Label.text = StockMarketLabels.TabStudio;
            _tab2Label.color = !isTab1 ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);

            RefreshActiveTab();
        }

        public void RefreshActiveTab()
        {
            if (_ms == null) return;
            UpdateLanguage();

            if (_titleText != null) _titleText.text = StockMarketLabels.Title;
            if (_tab1Label != null) _tab1Label.text = StockMarketLabels.TabMarket;
            if (_tab2Label != null) _tab2Label.text = StockMarketLabels.TabStudio;

            if (_currentTab == ActiveTab.MarketAndCompetitors)
            {
                RefreshTab1Market();
            }
            else
            {
                RefreshTab2Studio();
            }
        }

        #endregion

        #region Tab 1 Refresh: Competitors

        private void RefreshTab1Market()
        {
            if (_ms == null) return;

            long totalPortfolioVal = 0L;
            var pubs = GameObject.FindGameObjectsWithTag("Publisher")
                .Select(go => go.GetComponent<publisherScript>())
                .Where(p => p != null && p.isUnlocked && !p.isPlayer)
                .OrderByDescending(p => StockData.CalculateCompanyValue(p, _ms))
                .ToList();

            foreach (var pub in pubs)
            {
                var stock = StockData.GetOrCreate(pub.myID);
                if (stock.playerShare > 0f)
                {
                    double pPrice = stock.currentSharePrice > 0.01 ? stock.currentSharePrice : StockData.CalculateSharePrice(pub, _ms);
                    long val = (long)Math.Round(pPrice * 1000000.0);
                    totalPortfolioVal += (long)(val * (stock.playerShare / 100f));
                }
            }

            int totalWeeks = _ms.year * 48 + _ms.month * 4 + _ms.week;
            var macro = Ali.Mgt2.Simulation.StockDriftEngine.GetMacroPhaseForWeek(totalWeeks);
            string macroStr = GetMacroLabel(macro);

            // Summary Texts
            if (_marketIndexText != null)
                _marketIndexText.text = $"{StockMarketLabels.IndexVal} <b><color=#1E6FB8>10,482</color></b>  |  <color=#8E44AD><b>{macroStr}</b></color>";
            if (_portfolioValText != null)
                _portfolioValText.text = $"{StockMarketLabels.PortfolioVal} <b><color=#1E8C3A>{_ms.GetMoney(totalPortfolioVal, true)}</color></b>";
            if (_cashText != null)
                _cashText.text = $"{StockMarketLabels.Cash} <b><color=#1E8C3A>{_ms.GetMoney(_ms.money, true)}</color></b>";

            // Populate rows
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null) Destroy(_spawnedRows[i]);
            }
            _spawnedRows.Clear();

            foreach (var pub in pubs)
            {
                var row = CreateCompetitorRow(pub);
                _spawnedRows.Add(row);
            }
        }

        private string GetMacroLabel(Ali.Mgt2.Simulation.MacroCyclePhase phase)
        {
            switch (phase)
            {
                case Ali.Mgt2.Simulation.MacroCyclePhase.Expansion: return StockMarketLabels.MacroExpansion;
                case Ali.Mgt2.Simulation.MacroCyclePhase.Bubble: return StockMarketLabels.MacroBubble;
                case Ali.Mgt2.Simulation.MacroCyclePhase.Saturation: return StockMarketLabels.MacroSaturation;
                default: return StockMarketLabels.MacroBear;
            }
        }

        private GameObject CreateCompetitorRow(publisherScript pub)
        {
            var row = new GameObject("Row_" + pub.myID, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(_competitorListContent, false);

            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 62f;
            le.minHeight = 62f;

            var img = row.GetComponent<Image>();
            img.sprite = GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.97f, 0.975f, 0.985f, 1f);

            var stock = StockData.GetOrCreate(pub.myID);
            double effectivePrice = stock.currentSharePrice > 0.01 ? stock.currentSharePrice : StockData.CalculateSharePrice(pub, _ms);
            long marketCap = (long)Math.Round(effectivePrice * 1000000.0);
            double pctChange = stock.priceTrendPercent;
            string trendStr = pctChange > 0.05 ? $"+{pctChange:F1}% ▲" : (pctChange < -0.05 ? $"{pctChange:F1}% ▼" : "0.0%");
            string trendCol = pctChange > 0.05 ? "#1E8C3A" : (pctChange < -0.05 ? "#C0392B" : "#7F8C8D");

            // Logo
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(row.transform, false);
            var lRt = (RectTransform)logoGo.transform;
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = new Vector2(10f, 0f);
            lRt.sizeDelta = new Vector2(46f, 46f);
            logoGo.GetComponent<Image>().sprite = pub.GetLogo();

            // Name & Developer/Publisher
            var nameGo = CreateText(row.transform, "Name", new Vector2(0.07f, 0.50f), new Vector2(0.30f, 0.92f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 14, true, pub.GetName());
            var typeGo = CreateText(row.transform, "Type", new Vector2(0.07f, 0.08f), new Vector2(0.30f, 0.48f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 12, false, pub.GetDeveloperPublisherString());
            typeGo.color = new Color(0.45f, 0.48f, 0.52f, 1f);

            // Col 2: Valuation (Market Cap)
            CreateText(row.transform, "Valuation", new Vector2(0.30f, 0.10f), new Vector2(0.48f, 0.90f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14, true, $"<b><color=#1E8C3A>{_ms.GetMoney(marketCap, true)}</color></b>");

            // Col 3: Share Price & Trend
            CreateText(row.transform, "SharePrice", new Vector2(0.48f, 0.10f), new Vector2(0.66f, 0.90f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 13, false, $"<b>${effectivePrice:F2}</b>   <color={trendCol}><b>{trendStr}</b></color>");

            // Col 4: Player Stake
            string stakeColor = stock.playerShare >= 51f ? "#1E8C3A" : stock.playerShare > 0f ? "#1E6FB8" : "#666666";
            CreateText(row.transform, "Stake", new Vector2(0.66f, 0.10f), new Vector2(0.78f, 0.90f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 15, true, $"<b><color={stakeColor}>{stock.playerShare:F1}%</color></b>");

            // Col 5: Action Buttons (78% - 98%)
            if (stock.absorbedAsSubsidiary || pub.IsMyTochterfirma())
            {
                var absBadge = CreateText(row.transform, "AbsorbedBadge", new Vector2(0.78f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14, true, $"<b><color=#1E8C3A>{StockMarketLabels.Subsidiary}</color></b>");
            }
            else if (stock.playerShare >= 100f)
            {
                var absBtn = CreateNativeButton(row.transform, "AbsorbBtn", new Vector2(-16f, 0f), new Vector2(175f, 36f), () =>
                {
                    MajorityControlManager.AbsorbAsSubsidiary(_ms, pub, _guiMain);
                    RefreshActiveTab();
                }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                absBtn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f);
                var absTxt = absBtn.GetComponentInChildren<Text>();
                absTxt.text = StockMarketLabels.Subsidiary;
                absTxt.fontSize = 12;
                absTxt.fontStyle = FontStyle.Bold;
            }
            else
            {
                float btnW = 82f;
                float btnSpacing = 8f;
                float rightOffset = -14f;

                // Sell 5% Button (on the RIGHT)
                var sellBtnGo = CreateNativeButton(row.transform, "Sell5Btn", new Vector2(rightOffset, 0f), new Vector2(btnW, 34f), () =>
                {
                    ShareTransactionDialogs.ShowSellDialog(pub, _ms, _guiMain);
                }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                sellBtnGo.GetComponent<Image>().color = new Color(0.85f, 0.35f, 0.30f, 1f);
                var sellTxt = sellBtnGo.GetComponentInChildren<Text>();
                sellTxt.text = StockMarketLabels.Sell5;
                sellTxt.fontSize = 12;
                sellTxt.fontStyle = FontStyle.Bold;
                sellBtnGo.GetComponent<Button>().interactable = stock.playerShare > 0f;

                // Buy 5% Button (on the LEFT of Sell)
                float buyOffset = rightOffset - (btnW + btnSpacing);
                var buyBtnGo = CreateNativeButton(row.transform, "Buy5Btn", new Vector2(buyOffset, 0f), new Vector2(btnW, 34f), () =>
                {
                    ShareTransactionDialogs.ShowBuyDialog(pub, _ms, _guiMain);
                }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                buyBtnGo.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f);
                var buyTxt = buyBtnGo.GetComponentInChildren<Text>();
                buyTxt.text = StockMarketLabels.Buy5;
                buyTxt.fontSize = 12;
                buyTxt.fontStyle = FontStyle.Bold;

                // Majority Directives Button (51%+)
                if (stock.playerShare >= 51f)
                {
                    float dirOffset = buyOffset - (84f + btnSpacing);
                    var dirBtnGo = CreateNativeButton(row.transform, "DirectivesBtn", new Vector2(dirOffset, 0f), new Vector2(84f, 34f), () =>
                    {
                        ShowDirectivesDialog(pub);
                    }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                    dirBtnGo.GetComponent<Image>().color = new Color(0.161f, 0.502f, 0.725f, 1f);
                    var dirTxt = dirBtnGo.GetComponentInChildren<Text>();
                    dirTxt.text = StockMarketLabels.MajorityControl;
                    dirTxt.fontSize = 11;
                    dirTxt.fontStyle = FontStyle.Bold;
                }
                // Secret Intel Button (25%+)
                else if (stock.playerShare >= 25f)
                {
                    float intelOffset = buyOffset - (84f + btnSpacing);
                    var intelBtnGo = CreateNativeButton(row.transform, "IntelBtn", new Vector2(intelOffset, 0f), new Vector2(84f, 34f), () =>
                    {
                        ShowIntelDialog(pub);
                    }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                    intelBtnGo.GetComponent<Image>().color = new Color(0.55f, 0.35f, 0.65f, 1f);
                    var intelTxt = intelBtnGo.GetComponentInChildren<Text>();
                    intelTxt.text = StockMarketLabels.SecretIntel;
                    intelTxt.fontSize = 11;
                    intelTxt.fontStyle = FontStyle.Bold;
                }
            }

            return row;
        }

        #endregion

        #region Directives & Intel Dialogs

        private GameObject _activeModalDialog;

        private void CloseModalDialog()
        {
            if (_activeModalDialog != null)
            {
                Destroy(_activeModalDialog);
                _activeModalDialog = null;
            }
        }

        private void ShowIntelDialog(publisherScript pub)
        {
            CloseModalDialog();
            PlaySound(5);

            _activeModalDialog = new GameObject("IntelModal", typeof(RectTransform), typeof(Image));
            _activeModalDialog.transform.SetParent(_windowMainRt, false);
            var mRt = (RectTransform)_activeModalDialog.transform;
            mRt.sizeDelta = new Vector2(520f, 300f);
            mRt.anchoredPosition = Vector2.zero;

            var bg = _activeModalDialog.GetComponent<Image>();
            bg.sprite = GetGameSprite("window1_Border");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(_activeModalDialog.transform, false);
            var fRt = (RectTransform)fill.transform;
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;
            var fImg = fill.GetComponent<Image>();
            fImg.sprite = GetGameSprite("window1_FillVerlauf");
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color(0.94f, 0.94f, 0.95f, 1f);

            CreateText(_activeModalDialog.transform, "Title", new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 18, true, $"{pub.GetName()} - {StockMarketLabels.SecretIntel}");
            string intel = MajorityControlManager.GetSecretProjectIntel(pub, _ms);
            CreateText(_activeModalDialog.transform, "Content", new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 15, false, $"{StockMarketLabels.SecretProjectTitle}\n\n{intel}");

            var closeBtn = CreateNativeButton(_activeModalDialog.transform, "CloseBtn", new Vector2(0f, -110f), new Vector2(140f, 38f), CloseModalDialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            closeBtn.GetComponentInChildren<Text>().text = StockMarketLabels.Close;
        }

        private void ShowDirectivesDialog(publisherScript pub)
        {
            CloseModalDialog();
            PlaySound(5);

            var stock = StockData.GetOrCreate(pub.myID);
            bool mandatePub = stock.mandateExclusivePublishing;
            int selectedGenre = stock.mandateGenre;

            _activeModalDialog = new GameObject("DirectivesModal", typeof(RectTransform), typeof(Image));
            _activeModalDialog.transform.SetParent(_windowMainRt, false);
            var mRt = (RectTransform)_activeModalDialog.transform;
            mRt.sizeDelta = new Vector2(560f, 440f);
            mRt.anchoredPosition = Vector2.zero;

            var bg = _activeModalDialog.GetComponent<Image>();
            bg.sprite = GetGameSprite("window1_Border");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(_activeModalDialog.transform, false);
            var fRt = (RectTransform)fill.transform;
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;
            var fImg = fill.GetComponent<Image>();
            fImg.sprite = GetGameSprite("window1_FillVerlauf");
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color(0.94f, 0.94f, 0.95f, 1f);

            CreateText(_activeModalDialog.transform, "Title", new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 18, true, $"{pub.GetName()} - {StockMarketLabels.MajorityControl}");

            // Exclusive Publishing Toggle Button
            var pubToggleBtn = CreateNativeButton(_activeModalDialog.transform, "PubToggle", new Vector2(0f, 100f), new Vector2(460f, 42f), null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var pubToggleTxt = pubToggleBtn.GetComponentInChildren<Text>();

            Action updatePubToggleVisual = () =>
            {
                pubToggleBtn.GetComponent<Image>().color = mandatePub ? new Color(0.18f, 0.65f, 0.35f, 1f) : new Color(0.70f, 0.72f, 0.76f, 1f);
                pubToggleTxt.text = $"{(mandatePub ? "[X]" : "[ ]")} {StockMarketLabels.MandatePublishing}";
            };
            updatePubToggleVisual();

            pubToggleBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                mandatePub = !mandatePub;
                updatePubToggleVisual();
            });

            // Mandate Genre Label
            CreateText(_activeModalDialog.transform, "GenreLabel", new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, true, StockMarketLabels.MandateGenre);

            // Genre Selector Buttons: Free Choice, RPG, Strategy, Action, Sim, Adventure
            int[] testGenres = new int[] { -1, 0, 1, 2, 3, 4, 5 };
            string[] genreNames = new string[] { StockMarketLabels.AnyGenre, "RPG", "Strategy", "Action", "Simulation", "Adventure", "Sports" };
            Button[] gBtns = new Button[testGenres.Length];

            Action updateGenreBtns = () =>
            {
                for (int i = 0; i < testGenres.Length; i++)
                {
                    bool isCur = selectedGenre == testGenres[i];
                    gBtns[i].GetComponent<Image>().color = isCur ? new Color(0.161f, 0.502f, 0.725f, 1f) : new Color(0.82f, 0.84f, 0.88f, 1f);
                    gBtns[i].GetComponentInChildren<Text>().color = isCur ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            };

            for (int i = 0; i < testGenres.Length; i++)
            {
                int gId = testGenres[i];
                float gx = (i % 3) * 160f - 160f;
                float gy = (i / 3) * -38f + 8f;

                var gBtn = CreateNativeButton(_activeModalDialog.transform, $"GBtn_{gId}", new Vector2(gx, gy), new Vector2(150f, 32f), () =>
                {
                    selectedGenre = gId;
                    updateGenreBtns();
                }, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

                gBtns[i] = gBtn.GetComponent<Button>();
                gBtn.GetComponentInChildren<Text>().text = (_ms?.genres_ != null && gId >= 0) ? _ms.genres_.GetName(gId) : genreNames[i];
                gBtn.GetComponentInChildren<Text>().fontSize = 12;
            }
            updateGenreBtns();

            // Apply Button
            var applyBtn = CreateNativeButton(_activeModalDialog.transform, "ApplyBtn", new Vector2(-100f, -165f), new Vector2(170f, 40f), () =>
            {
                MajorityControlManager.SetExclusivePublishingMandate(pub.myID, mandatePub);
                MajorityControlManager.SetGenreMandate(pub.myID, selectedGenre);
                PlaySound(6);
                if (_guiMain != null) _guiMain.MessageBox(StockMarketLabels.DirectivesApplied, false);
                CloseModalDialog();
                RefreshActiveTab();
            }, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            applyBtn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f);
            applyBtn.GetComponentInChildren<Text>().text = StockMarketLabels.ApplyDirectives;

            // Cancel Button
            var cancelBtn = CreateNativeButton(_activeModalDialog.transform, "CancelBtn", new Vector2(100f, -165f), new Vector2(140f, 40f), CloseModalDialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cancelBtn.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f, 1f);
            cancelBtn.GetComponentInChildren<Text>().text = StockMarketLabels.Close;
        }

        #endregion

        #region Tab 2 Refresh: My Studio & Board (IPO)

        private void RefreshTab2Studio()
        {
            if (_ms == null) return;

            bool isPublic = StockData.Data.playerIsPublic || PlayerIpoManager.State.ipoYear > 0;
            _preIpoContainer.SetActive(!isPublic);
            _postIpoContainer.SetActive(isPublic);

            if (!isPublic)
            {
                RefreshPreIpoView();
            }
            else
            {
                RefreshPostIpoView();
            }
        }

        private void RefreshPreIpoView()
        {
            long val = PlayerIpoManager.CalculatePlayerValuation(_ms);
            string companyName = _ms.GetCompanyName();

            if (_studioValText != null)
            {
                _studioValText.text = $"<b>{companyName}</b>\n" +
                    $"{StockMarketLabels.Cash} <b><color=#1E8C3A>{_ms.GetMoney(_ms.money, true)}</color></b>   |   " +
                    $"{StockMarketLabels.StudioVal} <b><color=#1E8C3A>{_ms.GetMoney(val, true)}</color></b>";
            }

            UpdateOfferingButtonsState();
            RefreshPreIpoCalculations();
        }

        private void UpdateOfferingButtonsState()
        {
            Color activeCol = new Color(0.161f, 0.502f, 0.725f, 1f);
            Color inactiveCol = new Color(0.82f, 0.84f, 0.88f, 1f);

            for (int i = 0; i < 4; i++)
            {
                bool isSelected = Mathf.Approximately(_offeringSizes[i], _selectedOfferingPct);
                if (_offeringButtonBgs[i] != null) _offeringButtonBgs[i].color = isSelected ? activeCol : inactiveCol;
                if (_offeringButtonTexts[i] != null) _offeringButtonTexts[i].color = isSelected ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }

        private void RefreshPreIpoCalculations()
        {
            long val = PlayerIpoManager.CalculatePlayerValuation(_ms);
            long grossProceeds = (long)(val * _selectedOfferingPct);
            long fee = (long)(grossProceeds * PlayerIpoManager.IpoUnderwritingFeeRate);
            long netProceeds = grossProceeds - fee;
            double sharePrice = Math.Round((double)val / PlayerIpoManager.DefaultTotalShares, 2);

            if (_ipoBreakdownText != null)
            {
                _ipoBreakdownText.text = $"{StockMarketLabels.GrossCapital}  <b><color=#1E8C3A>{_ms.GetMoney(grossProceeds, true)}</color></b>\n" +
                                         $"{StockMarketLabels.UnderwritingFee}  <color=#C0392B>-{_ms.GetMoney(fee, true)}</color>\n" +
                                         $"{StockMarketLabels.InitialSharePrice}  <b>${sharePrice:F2}</b>";
            }

            if (_netProceedsText != null)
            {
                _netProceedsText.text = $"{StockMarketLabels.NetProceedsReceived}\n" +
                                        $"<b><size=22><color=#1E8C3A>{_ms.GetMoney(netProceeds, true)}</color></size></b>\n" +
                                        $"<size=11><color=#666666>{StockMarketLabels.DepositNotice}</color></size>";
            }
        }

        private void OnLaunchIpoClicked()
        {
            if (_ms == null) return;

            string statusMsg;
            bool success = PlayerIpoManager.LaunchPlayerIPO(_ms, _selectedOfferingPct, out statusMsg);

            if (success)
            {
                PlaySound(6);
                if (_guiMain != null)
                {
                    _guiMain.MessageBox(StockMarketLabels.IpoSuccessNotice, false);
                }
                RefreshActiveTab();
            }
            else
            {
                PlaySound(4);
                if (_guiMain != null)
                {
                    _guiMain.MessageBox(statusMsg, false);
                }
            }
        }

        private void RefreshPostIpoView()
        {
            var state = PlayerIpoManager.State;

            // Card 1: Stock Summary & Ownership
            double pctChange = state.priceTrendPercent;
            string trendStr = pctChange > 0.05 ? $"+{pctChange:F1}% ▲" : (pctChange < -0.05 ? $"{pctChange:F1}% ▼" : "0.0%");
            string trendCol = pctChange > 0.05 ? "#1E8C3A" : (pctChange < -0.05 ? "#C0392B" : "#7F8C8D");

            if (_sharePriceText != null)
                _sharePriceText.text = $"{StockMarketLabels.SharePrice} <b>${state.currentSharePrice:F2}</b>   <color={trendCol}><b>{trendStr}</b></color>";
            if (_marketCapText != null)
                _marketCapText.text = $"{StockMarketLabels.StudioVal} <b><color=#1E8C3A>{_ms.GetMoney(state.marketValuation, true)}</color></b>";
            if (_sharesDistributionText != null)
            {
                float playerStake = state.founderSharePercent > 0f ? state.founderSharePercent : 80f;
                float publicFloat = Mathf.Clamp(100f - playerStake - state.hostileRivalSharePercent, 0f, 100f);
                string rivalInfo = "";
                if (state.hostileRivalSharePercent > 0.5f)
                {
                    string rCol = state.hostileRivalSharePercent >= 20f ? "#C0392B" : "#D35400";
                    rivalInfo = $"  |  <color={rCol}><b>{state.hostileRivalName}: {state.hostileRivalSharePercent:F1}%</b></color>";
                }
                _sharesDistributionText.text = $"{StockMarketLabels.FounderStake} <b><color=#1E8C3A>{playerStake:F1}%</color></b>  |  {StockMarketLabels.Float} <b>{publicFloat:F1}%</b>{rivalInfo}";
            }

            // Card 2: Board Sentiment
            var sentiment = PlayerIpoManager.GetBoardSentiment();
            string sentText;
            Color sentColor;

            switch (sentiment)
            {
                case BoardSentiment.Satisfied:
                    sentText = StockMarketLabels.SentimentSatisfied;
                    sentColor = new Color(0.18f, 0.65f, 0.35f, 1f);
                    break;
                case BoardSentiment.Cautious:
                    sentText = StockMarketLabels.SentimentCautious;
                    sentColor = new Color(0.85f, 0.65f, 0.15f, 1f);
                    break;
                case BoardSentiment.Demanding:
                    sentText = StockMarketLabels.SentimentDemanding;
                    sentColor = new Color(0.90f, 0.45f, 0.15f, 1f);
                    break;
                default:
                    sentText = StockMarketLabels.SentimentHostile;
                    sentColor = new Color(0.85f, 0.20f, 0.20f, 1f);
                    break;
            }

            if (_boardSentimentBadge != null)
            {
                _boardSentimentBadge.text = sentText;
                _boardSentimentBadgeBg.color = sentColor;
            }

            if (_discontentLabel != null)
                _discontentLabel.text = $"{StockMarketLabels.Discontent} <b>{Mathf.RoundToInt(state.boardDiscontent)}%</b>";
            if (_discontentFill != null)
                _discontentFill.fillAmount = Mathf.Clamp01(state.boardDiscontent / 100f);

            if (_confidenceLabel != null)
                _confidenceLabel.text = $"{StockMarketLabels.Confidence} <b>{Mathf.RoundToInt(state.investorConfidence)}%</b>";
            if (_confidenceFill != null)
                _confidenceFill.fillAmount = Mathf.Clamp01(state.investorConfidence / 100f);

            if (_spendingCapWarning != null)
                _spendingCapWarning.SetActive(state.spendingRestrictionActive);

            // Card 3: Quarterly Performance
            if (_quarterlyTargetText != null)
                _quarterlyTargetText.text = $"{StockMarketLabels.QuarterlyTarget} <b><color=#1E6FB8>{_ms.GetMoney(state.quarterlyRevenueTarget, true)}</color></b>";
            if (_quarterlyProgressText != null)
                _quarterlyProgressText.text = $"{StockMarketLabels.CurrentRevenue} <b><color=#1E8C3A>{_ms.GetMoney(state.currentQuarterRevenue, true)}</color></b>";

            if (_targetProgressBarFill != null)
            {
                float progress = state.quarterlyRevenueTarget > 0 ? (float)state.currentQuarterRevenue / state.quarterlyRevenueTarget : 0f;
                _targetProgressBarFill.fillAmount = Mathf.Clamp01(progress);
                _targetProgressBarFill.color = progress >= 1f ? new Color(0.18f, 0.65f, 0.35f, 1f) : new Color(0.161f, 0.502f, 0.725f, 1f);
            }

            if (_streakText != null)
                _streakText.text = $"{StockMarketLabels.TargetProgress} Streak Met: <color=green>{state.consecutiveTargetsMet}</color> | Missed: <color=red>{state.consecutiveTargetsMissed}</color>";

            // Card 4: Dividends & Defense
            long recommendedDiv = Math.Max(10000L, (long)(state.marketValuation * 0.015));
            if (_dividendRecommendedText != null)
                _dividendRecommendedText.text = $"{StockMarketLabels.RecommendedDiv}\n<b><color=#1E8C3A>{_ms.GetMoney(recommendedDiv, true)}</color></b>";

            if (_payDividendBtn != null)
                _payDividendBtn.interactable = _ms.money >= recommendedDiv;

            if (_emergencyDividendBtn != null)
                _emergencyDividendBtn.interactable = state.boardDiscontent >= PlayerIpoManager.HighDiscontentThreshold && _ms.money >= recommendedDiv * 2;

            double buybackCost = (state.totalShares * 0.05) * state.currentSharePrice * 1.03;
            long costLong = (long)Math.Round(buybackCost);

            if (_buybackBtn != null)
            {
                _buybackBtn.interactable = _ms.money >= costLong && state.founderSharePercent < 95f;
                var bTxt = _buybackBtn.GetComponentInChildren<Text>();
                if (bTxt != null) bTxt.text = $"{StockMarketLabels.ShareBuyback}\n({_ms.GetMoney(costLong, true)})";
            }

            if (_poisonPillBtn != null)
            {
                _poisonPillBtn.interactable = state.hostileRivalSharePercent >= 20f && !state.poisonPillActive;
                _poisonPillBtn.gameObject.SetActive(state.hostileRivalSharePercent >= 15f || state.poisonPillActive);
            }
        }

        private void OnShareBuybackClicked()
        {
            if (_ms == null) return;
            PlaySound(2);
            PlayerIpoManager.ExecuteShareBuyback(_ms, _guiMain, out _);
            RefreshActiveTab();
        }

        private void OnPoisonPillClicked()
        {
            if (_ms == null) return;
            PlaySound(1);
            PlayerIpoManager.ExecutePoisonPillDefense(_ms, _guiMain, out _);
            RefreshActiveTab();
        }

        private void OnPayDividendClicked()
        {
            if (_ms == null) return;
            string msg;
            long amount = Math.Max(10000L, (long)(PlayerIpoManager.State.marketValuation * 0.015));

            bool success = PlayerIpoManager.PayQuarterlyDividend(_ms, amount, out msg);
            if (success)
            {
                PlaySound(6);
                if (_guiMain != null) _guiMain.MessageBox(StockMarketLabels.DividendPaidNotice, false);
                RefreshPostIpoView();
            }
            else
            {
                PlaySound(4);
                if (_guiMain != null) _guiMain.MessageBox(msg, false);
            }
        }

        private void OnPayEmergencyDividendClicked()
        {
            if (_ms == null) return;
            string msg;
            bool success = PlayerIpoManager.PayEmergencyDividend(_ms, out msg);

            if (success)
            {
                PlaySound(6);
                if (_guiMain != null) _guiMain.MessageBox(StockMarketLabels.DividendPaidNotice, false);
                RefreshPostIpoView();
            }
            else
            {
                PlaySound(4);
                if (_guiMain != null) _guiMain.MessageBox(msg, false);
            }
        }

        #endregion
    }
}
