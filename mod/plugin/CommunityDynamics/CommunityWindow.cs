using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.CommunityDynamics
{
    public class CommunityWindow : MonoBehaviour
    {
        public static CommunityWindow Instance { get; private set; }

        public enum ActiveTab
        {
            Feed = 0,
            Marketing = 1,
            Streamers = 2,
            Crisis = 3
        }

        private mainScript _ms;
        private GUI_Main _guiMain;
        private sfxScript _sfx;
        private Canvas _canvas;

        private GameObject _windowRoot;
        private RectTransform _windowMainRt;
        private Text _titleText;

        // Tabs
        private ActiveTab _currentTab = ActiveTab.Feed;
        private Image _tab0Bg, _tab1Bg, _tab2Bg, _tab3Bg;
        private Text _tab0Label, _tab1Label, _tab2Label, _tab3Label;
        private GameObject _tab0Panel, _tab1Panel, _tab2Panel, _tab3Panel;

        // Sentiment metrics bar
        private Text _sentimentText;
        private Text _hypeText;
        private Text _crisisText;

        // Feed Filter
        private int _feedFilter = 0; // 0=All, 1=Studio, 2=Trends, 3=Outrage

        // Content Scrollviews
        private RectTransform _feedContent;
        private RectTransform _marketingContent;
        private RectTransform _streamersContent;
        private RectTransform _crisisContent;

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
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

        public static void Toggle(mainScript ms, GUI_Main guiMain)
        {
            var win = GetOrCreate();
            win._ms = ms;
            win._guiMain = guiMain;
            if (win._windowRoot != null && win._windowRoot.activeSelf) win.CloseWindow();
            else win.ShowWindow();
        }

        public static CommunityWindow GetOrCreate()
        {
            if (Instance != null) return Instance;
            var host = new GameObject("MGT2_CommunityWindow_Host");
            DontDestroyOnLoad(host);
            return host.AddComponent<CommunityWindow>();
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

        #region Window Controls

        public void ShowWindow()
        {
            FindScripts();
            UpdateLanguage();
            CommunityManager.EnsureSeedPosts(_ms);

            if (_windowRoot == null)
            {
                BuildNativeWindow();
            }

            _windowRoot.SetActive(true);

            if (_guiMain != null) _guiMain.OpenMenu(hideChars: false);
            else if (_ms != null && !_ms.multiplayer) _ms.PauseGame(true);

            PlaySound(5);
            UpdateMetricsHeader();
            SwitchTab(_currentTab);
        }

        public void CloseWindow()
        {
            if (_windowRoot != null) _windowRoot.SetActive(false);

            if (_guiMain != null) _guiMain.CloseMenu();
            else if (_ms != null && !_ms.multiplayer) _ms.PauseGame(false);

            PlaySound(3);
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
            CommunityDynamicsLabels.Arabic = settings != null && settings.language == 13;
        }

        private void PlaySound(int id)
        {
            try { _sfx?.PlaySound(id, force: true); } catch { }
        }

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
                return _ms.fonts[0];
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        #endregion

        #region UI Construction

        private void BuildNativeWindow()
        {
            FindScripts();

            _windowRoot = new GameObject("MGT2_CommunityWindow_Root", typeof(RectTransform));
            _windowRoot.transform.SetParent(_canvas.transform, false);

            var rootRt = (RectTransform)_windowRoot.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;

            var blockerImg = _windowRoot.AddComponent<Image>();
            blockerImg.color = new Color(0f, 0f, 0f, 0.45f);

            var windowMain = new GameObject("WindowMain", typeof(RectTransform), typeof(Image));
            windowMain.transform.SetParent(_windowRoot.transform, false);

            _windowMainRt = (RectTransform)windowMain.transform;
            _windowMainRt.sizeDelta = new Vector2(860f, 680f);
            _windowMainRt.anchoredPosition = Vector2.zero;

            var borderImg = windowMain.GetComponent<Image>();
            borderImg.sprite = GetGameSprite("window1_Border");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(windowMain.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(8f, 8f);
            bgRt.offsetMax = new Vector2(-8f, -8f);

            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = GetGameSprite("window1_FillVerlauf");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.92f, 0.94f, 0.97f, 1f);

            // Title Bar
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(bg.transform, false);
            var titleRt = (RectTransform)titleBar.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0f, 40f);

            var titleImg = titleBar.GetComponent<Image>();
            titleImg.sprite = GetGameSprite("titleLeiste");
            titleImg.type = Image.Type.Sliced;
            titleImg.color = new Color(0.204f, 0.596f, 0.859f, 1.0f);

            var titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleTextObj.transform.SetParent(titleBar.transform, false);
            var titleTextRt = (RectTransform)titleTextObj.transform;
            titleTextRt.anchorMin = Vector2.zero;
            titleTextRt.anchorMax = Vector2.one;
            titleTextRt.offsetMin = new Vector2(16f, 0f);
            titleTextRt.offsetMax = new Vector2(-50f, 0f);

            _titleText = titleTextObj.GetComponent<Text>();
            _titleText.font = GetGameFont();
            _titleText.fontSize = 17;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.text = CommunityDynamicsLabels.Title;

            // Close Button
            var closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(titleBar.transform, false);
            var closeRt = (RectTransform)closeBtnObj.transform;
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-6f, 0f);
            closeRt.sizeDelta = new Vector2(30f, 30f);

            var closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.sprite = GetGameSprite("close");
            closeImg.color = Color.white;

            var closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(CloseWindow);

            // Metrics Bar
            BuildMetricsBar(bg.transform);

            // Tabs Header
            BuildTabsHeader(bg.transform);

            // Tab Panels
            _tab0Panel = BuildScrollPanel(bg.transform, "FeedPanel", out _feedContent);
            _tab1Panel = BuildScrollPanel(bg.transform, "MarketingPanel", out _marketingContent);
            _tab2Panel = BuildScrollPanel(bg.transform, "StreamersPanel", out _streamersContent);
            _tab3Panel = BuildScrollPanel(bg.transform, "CrisisPanel", out _crisisContent);

            _tab0Panel.SetActive(true);
            _tab1Panel.SetActive(false);
            _tab2Panel.SetActive(false);
            _tab3Panel.SetActive(false);
        }

        private void BuildMetricsBar(Transform parent)
        {
            var bar = new GameObject("MetricsBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, -44f);
            barRt.sizeDelta = new Vector2(-16f, 32f);

            var barImg = bar.GetComponent<Image>();
            barImg.sprite = GetGameSprite("uiSquareVerlauf");
            barImg.type = Image.Type.Sliced;
            barImg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);

            // Sentiment Text
            var sentObj = new GameObject("SentimentText", typeof(RectTransform), typeof(Text));
            sentObj.transform.SetParent(bar.transform, false);
            var sentRt = (RectTransform)sentObj.transform;
            sentRt.anchorMin = new Vector2(0.02f, 0f);
            sentRt.anchorMax = new Vector2(0.42f, 1f);
            sentRt.offsetMin = sentRt.offsetMax = Vector2.zero;

            _sentimentText = sentObj.GetComponent<Text>();
            _sentimentText.font = GetGameFont();
            _sentimentText.fontSize = 12;
            _sentimentText.fontStyle = FontStyle.Bold;
            _sentimentText.color = Color.white;
            _sentimentText.alignment = TextAnchor.MiddleLeft;

            // Hype Text
            var hypeObj = new GameObject("HypeText", typeof(RectTransform), typeof(Text));
            hypeObj.transform.SetParent(bar.transform, false);
            var hypeRt = (RectTransform)hypeObj.transform;
            hypeRt.anchorMin = new Vector2(0.44f, 0f);
            hypeRt.anchorMax = new Vector2(0.70f, 1f);
            hypeRt.offsetMin = hypeRt.offsetMax = Vector2.zero;

            _hypeText = hypeObj.GetComponent<Text>();
            _hypeText.font = GetGameFont();
            _hypeText.fontSize = 12;
            _hypeText.fontStyle = FontStyle.Bold;
            _hypeText.color = new Color(1f, 0.85f, 0.3f);
            _hypeText.alignment = TextAnchor.MiddleCenter;

            // Crisis Alert Text
            var crisisObj = new GameObject("CrisisText", typeof(RectTransform), typeof(Text));
            crisisObj.transform.SetParent(bar.transform, false);
            var crisisRt = (RectTransform)crisisObj.transform;
            crisisRt.anchorMin = new Vector2(0.72f, 0f);
            crisisRt.anchorMax = new Vector2(0.98f, 1f);
            crisisRt.offsetMin = crisisRt.offsetMax = Vector2.zero;

            _crisisText = crisisObj.GetComponent<Text>();
            _crisisText.font = GetGameFont();
            _crisisText.fontSize = 12;
            _crisisText.fontStyle = FontStyle.Bold;
            _crisisText.color = Color.white;
            _crisisText.alignment = TextAnchor.MiddleRight;
        }

        private void BuildTabsHeader(Transform parent)
        {
            var bar = new GameObject("TabsBar", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, -80f);
            barRt.sizeDelta = new Vector2(-16f, 36f);

            _tab0Panel = null;
            CreateTabButton(bar.transform, 0, new Vector2(0.000f, 0f), new Vector2(0.244f, 1f), CommunityDynamicsLabels.TabFeed, out _tab0Bg, out _tab0Label);
            CreateTabButton(bar.transform, 1, new Vector2(0.252f, 0f), new Vector2(0.496f, 1f), CommunityDynamicsLabels.TabMarketing, out _tab1Bg, out _tab1Label);
            CreateTabButton(bar.transform, 2, new Vector2(0.504f, 0f), new Vector2(0.748f, 1f), CommunityDynamicsLabels.TabStreamers, out _tab2Bg, out _tab2Label);
            CreateTabButton(bar.transform, 3, new Vector2(0.756f, 0f), new Vector2(1.000f, 1f), CommunityDynamicsLabels.TabCrisis, out _tab3Bg, out _tab3Label);
        }

        private void CreateTabButton(Transform parent, int tabIdx, Vector2 anchorMin, Vector2 anchorMax, string label, out Image bgImg, out Text labelTxt)
        {
            var btnObj = new GameObject($"Tab_{tabIdx}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var btnRt = (RectTransform)btnObj.transform;
            btnRt.anchorMin = anchorMin;
            btnRt.anchorMax = anchorMax;
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            btnRt.sizeDelta = Vector2.zero;

            bgImg = btnObj.GetComponent<Image>();
            bgImg.sprite = GetGameSprite("uiSquareVerlauf");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = (tabIdx == 0) ? new Color(0.18f, 0.45f, 0.75f, 1f) : new Color(0.6f, 0.65f, 0.7f, 0.9f);

            var txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = (RectTransform)txtObj.transform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

            labelTxt = txtObj.GetComponent<Text>();
            labelTxt.font = GetGameFont();
            labelTxt.fontSize = 12;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.color = Color.white;
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
            labelTxt.text = label;

            var btn = btnObj.GetComponent<Button>();
            int idx = tabIdx;
            btn.onClick.AddListener(() =>
            {
                PlaySound(0);
                SwitchTab((ActiveTab)idx);
            });
        }

        private GameObject BuildScrollPanel(Transform parent, string name, out RectTransform contentRt)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = new Vector2(10f, 12f);
            panelRt.offsetMax = new Vector2(-10f, -122f);

            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(panel.transform, false);
            var scrollRt = (RectTransform)scrollObj.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;

            var scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            var viewportRt = (RectTransform)viewport.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;

            var viewImg = viewport.GetComponent<Image>();
            viewImg.sprite = GetGameSprite("whiteSimple");
            viewImg.color = Color.white;

            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(6, 6, 6, 6);

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            return panel;
        }

        #endregion

        #region Tab Switching & Metrics

        public void SwitchTab(ActiveTab tab)
        {
            _currentTab = tab;

            _tab0Panel.SetActive(tab == ActiveTab.Feed);
            _tab1Panel.SetActive(tab == ActiveTab.Marketing);
            _tab2Panel.SetActive(tab == ActiveTab.Streamers);
            _tab3Panel.SetActive(tab == ActiveTab.Crisis);

            Color activeColor = new Color(0.18f, 0.45f, 0.75f, 1f);
            Color inactiveColor = new Color(0.6f, 0.65f, 0.7f, 0.9f);

            if (_tab0Bg) _tab0Bg.color = (tab == ActiveTab.Feed) ? activeColor : inactiveColor;
            if (_tab1Bg) _tab1Bg.color = (tab == ActiveTab.Marketing) ? activeColor : inactiveColor;
            if (_tab2Bg) _tab2Bg.color = (tab == ActiveTab.Streamers) ? activeColor : inactiveColor;
            if (_tab3Bg) _tab3Bg.color = (tab == ActiveTab.Crisis) ? activeColor : inactiveColor;

            UpdateMetricsHeader();
            RefreshActiveTab();
        }

        private void UpdateMetricsHeader()
        {
            if (_sentimentText == null) return;

            float sent = CommunityManager.Data.overallSentiment;
            string sentLabel;
            string colorHex;

            if (sent >= 0.70f)
            {
                sentLabel = CommunityDynamicsLabels.SentimentPositive;
                colorHex = "#55FF55";
            }
            else if (sent >= 0.40f)
            {
                sentLabel = CommunityDynamicsLabels.SentimentMixed;
                colorHex = "#FFFF55";
            }
            else
            {
                sentLabel = CommunityDynamicsLabels.SentimentNegative;
                colorHex = "#FF5555";
            }

            _sentimentText.text = $"{CommunityDynamicsLabels.OverallSentiment} <color={colorHex}>{Mathf.RoundToInt(sent * 100f)}% ({sentLabel})</color>";

            int avgHype = 65;
            if (_ms != null)
            {
                var myGames = FindObjectsOfType<gameScript>().Where(g => g != null && g.IsMyGame() && g.isOnMarket).ToList();
                if (myGames.Count > 0) avgHype = Mathf.RoundToInt(myGames.Average(g => g.hype));
            }
            _hypeText.text = $"{CommunityDynamicsLabels.GlobalHype} <color=#FFDD44>{avgHype}/100</color>";

            int bombCount = CommunityManager.Data.activeReviewBombs.Count;
            if (bombCount > 0)
            {
                _crisisText.text = $"<color=#FF4444>[! {CommunityDynamicsLabels.ReviewBombAlert} ({bombCount}) !]</color>";
            }
            else
            {
                _crisisText.text = $"<color=#88FF88>{CommunityDynamicsLabels.PulseNormal}</color>";
            }
        }

        public void RefreshActiveTab()
        {
            UpdateMetricsHeader();
            switch (_currentTab)
            {
                case ActiveTab.Feed:
                    PopulateFeedTab();
                    break;
                case ActiveTab.Marketing:
                    PopulateMarketingTab();
                    break;
                case ActiveTab.Streamers:
                    PopulateStreamersTab();
                    break;
                case ActiveTab.Crisis:
                    PopulateCrisisTab();
                    break;
            }
        }

        private void ClearSpawnedItems()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedItems.Clear();
        }

        #endregion

        #region Tab 1: Live Social Feed

        private void PopulateFeedTab()
        {
            ClearSpawnedItems();
            if (_feedContent == null) return;

            var posts = CommunityManager.Data.posts;
            var filtered = posts.AsEnumerable();

            if (_feedFilter == 1) filtered = filtered.Where(p => p.category == 1);
            else if (_feedFilter == 2) filtered = filtered.Where(p => p.category == 2);
            else if (_feedFilter == 3) filtered = filtered.Where(p => p.category == 3);

            var list = filtered.Reverse().Take(30).ToList();

            if (list.Count == 0)
            {
                var empty = CreateTextCard(_feedContent, CommunityDynamicsLabels.NoPosts, 60f);
                _spawnedItems.Add(empty);
                return;
            }

            foreach (var p in list)
            {
                var card = CreatePostCard(_feedContent, p);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreatePostCard(Transform parent, SocialPost post)
        {
            var card = new GameObject("PostCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.sizeDelta = new Vector2(0f, 95f);

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;

            if (post.category == 3) // Outrage
                img.color = new Color(0.40f, 0.15f, 0.15f, 0.95f);
            else if (post.category == 2) // Trend
                img.color = new Color(0.18f, 0.28f, 0.42f, 0.95f);
            else if (post.category == 1) // Studio
                img.color = new Color(0.18f, 0.36f, 0.25f, 0.95f);
            else
                img.color = new Color(0.22f, 0.24f, 0.28f, 0.95f);

            // Author & Date
            var topTextObj = new GameObject("TopRow", typeof(RectTransform), typeof(Text));
            topTextObj.transform.SetParent(card.transform, false);
            var topRt = (RectTransform)topTextObj.transform;
            topRt.anchorMin = new Vector2(0.02f, 0.68f);
            topRt.anchorMax = new Vector2(0.98f, 0.96f);
            topRt.offsetMin = topRt.offsetMax = Vector2.zero;

            var topTxt = topTextObj.GetComponent<Text>();
            topTxt.font = GetGameFont();
            topTxt.fontSize = 12;
            topTxt.fontStyle = FontStyle.Bold;

            string tag = post.sentiment switch
            {
                1 => $"<color=#FF9933>{CommunityDynamicsLabels.TagHype}</color>",
                2 => $"<color=#55FF55>{CommunityDynamicsLabels.TagLegendary}</color>",
                3 => $"<color=#FFFF55>{CommunityDynamicsLabels.TagSkeptical}</color>",
                4 => $"<color=#FF3333>{CommunityDynamicsLabels.TagRage}</color>",
                _ => $"<color=#AAAAAA>{CommunityDynamicsLabels.TagCommunity}</color>"
            };

            topTxt.text = $"<color=#55DDFF>{post.authorHandle}</color>  {tag}  <color=#CCCCCC>• {CommunityDynamicsLabels.YearLabel} {post.year} {CommunityDynamicsLabels.WeekLabel} {post.week}</color>";

            // Body
            var bodyObj = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyObj.transform.SetParent(card.transform, false);
            var bodyRt = (RectTransform)bodyObj.transform;
            bodyRt.anchorMin = new Vector2(0.02f, 0.30f);
            bodyRt.anchorMax = new Vector2(0.98f, 0.68f);
            bodyRt.offsetMin = bodyRt.offsetMax = Vector2.zero;

            var bodyTxt = bodyObj.GetComponent<Text>();
            bodyTxt.font = GetGameFont();
            bodyTxt.fontSize = 12;
            bodyTxt.color = Color.white;
            bodyTxt.text = CommunityDynamicsLabels.Arabic ? Ali.Mgt2.Shared.ArabicFixer.Fix(post.content) : post.content;

            // Footer
            var footObj = new GameObject("Footer", typeof(RectTransform), typeof(Text));
            footObj.transform.SetParent(card.transform, false);
            var footRt = (RectTransform)footObj.transform;
            footRt.anchorMin = new Vector2(0.02f, 0.04f);
            footRt.anchorMax = new Vector2(0.98f, 0.28f);
            footRt.offsetMin = footRt.offsetMax = Vector2.zero;

            var footTxt = footObj.GetComponent<Text>();
            footTxt.font = GetGameFont();
            footTxt.fontSize = 11;
            footTxt.color = new Color(0.7f, 0.7f, 0.7f);
            footTxt.text = $"<color=#FF5577>♥ {post.likes:N0}</color>  |  <color=#55AAFF>🔁 {post.retweets:N0}</color>  {(string.IsNullOrEmpty(post.gameName) ? "" : $"• {CommunityDynamicsLabels.GameLabel} <color=#FFEE88>{post.gameName}</color>")}";

            return card;
        }

        #endregion

        #region Tab 2: Hype & Bullshot Marketing

        private void PopulateMarketingTab()
        {
            ClearSpawnedItems();
            if (_marketingContent == null) return;

            var devGames = FindObjectsOfType<gameScript>()
                .Where(g => g != null && g.IsMyGame() && g.inDevelopment && !g.isOnMarket)
                .ToList();

            if (devGames.Count == 0)
            {
                var empty = CreateTextCard(_marketingContent, CommunityDynamicsLabels.NoGamesDev, 80f);
                _spawnedItems.Add(empty);
                return;
            }

            foreach (var g in devGames)
            {
                var card = CreateMarketingCard(_marketingContent, g);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreateMarketingCard(Transform parent, gameScript game)
        {
            var card = new GameObject("MarketingCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.sizeDelta = new Vector2(0f, 155f);

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.24f, 0.27f, 0.32f, 0.95f);

            var infoObj = new GameObject("Info", typeof(RectTransform), typeof(Text));
            infoObj.transform.SetParent(card.transform, false);
            var infoRt = (RectTransform)infoObj.transform;
            infoRt.anchorMin = new Vector2(0.02f, 0.65f);
            infoRt.anchorMax = new Vector2(0.98f, 0.95f);
            infoRt.offsetMin = infoRt.offsetMax = Vector2.zero;

            var infoTxt = infoObj.GetComponent<Text>();
            infoTxt.font = GetGameFont();
            infoTxt.fontSize = 13;
            infoTxt.fontStyle = FontStyle.Bold;
            infoTxt.color = Color.white;
            infoTxt.text = $"<color=#FFDD44>{game.GetNameSimple()}</color>  •  {CommunityDynamicsLabels.HypeBoost} <color=#55FF55>{Mathf.RoundToInt(game.hype)}</color>  •  {CommunityDynamicsLabels.PreOrderBoost} <color=#55DDFF>{game.vorbestellungen:N0}</color>";

            // Action 1: CGI Bullshot
            CreateActionButton(card.transform, new Vector2(0.02f, 0.10f), new Vector2(0.48f, 0.58f),
                CommunityDynamicsLabels.StrategyCGI,
                $"{CommunityDynamicsLabels.CostLabel} $80,000 | {CommunityDynamicsLabels.ReviewBombRisk} {CommunityDynamicsLabels.RiskHigh}",
                new Color(0.65f, 0.22f, 0.22f, 1f),
                () =>
                {
                    PlaySound(1);
                    CommunityManager.LaunchBullshotCampaign(game, 0, _ms);
                    RefreshActiveTab();
                });

            // Action 2: Honest Showcase
            CreateActionButton(card.transform, new Vector2(0.52f, 0.10f), new Vector2(0.98f, 0.58f),
                CommunityDynamicsLabels.StrategyHonest,
                $"{CommunityDynamicsLabels.CostLabel} $25,000 | {CommunityDynamicsLabels.ReviewBombRisk} {CommunityDynamicsLabels.RiskLow}",
                new Color(0.22f, 0.55f, 0.35f, 1f),
                () =>
                {
                    PlaySound(1);
                    CommunityManager.LaunchBullshotCampaign(game, 1, _ms);
                    RefreshActiveTab();
                });

            return card;
        }

        #endregion

        #region Tab 3: Streamer Outreach

        private void PopulateStreamersTab()
        {
            ClearSpawnedItems();
            if (_streamersContent == null) return;

            var games = FindObjectsOfType<gameScript>()
                .Where(g => g != null && g.IsMyGame() && (g.inDevelopment || (g.isOnMarket && g.weeksOnMarket <= 4)))
                .ToList();

            if (games.Count == 0)
            {
                var empty = CreateTextCard(_streamersContent, CommunityDynamicsLabels.NoGamesDev, 80f);
                _spawnedItems.Add(empty);
                return;
            }

            foreach (var g in games)
            {
                var card = CreateStreamerCard(_streamersContent, g);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreateStreamerCard(Transform parent, gameScript game)
        {
            var card = new GameObject("StreamerCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.sizeDelta = new Vector2(0f, 155f);

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.23f, 0.26f, 0.33f, 0.95f);

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(card.transform, false);
            var titleRt = (RectTransform)titleObj.transform;
            titleRt.anchorMin = new Vector2(0.02f, 0.70f);
            titleRt.anchorMax = new Vector2(0.98f, 0.95f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = GetGameFont();
            titleTxt.fontSize = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = Color.white;
            titleTxt.text = $"<color=#55DDFF>{game.GetNameSimple()}</color>  •  {(game.isOnMarket ? CommunityDynamicsLabels.InMarket : CommunityDynamicsLabels.InDev)}";

            // 3 Streamer Tiers
            CreateActionButton(card.transform, new Vector2(0.02f, 0.08f), new Vector2(0.32f, 0.62f),
                CommunityDynamicsLabels.StreamerMega,
                CommunityDynamicsLabels.Subs10M,
                new Color(0.6f, 0.3f, 0.7f, 1f),
                () =>
                {
                    PlaySound(1);
                    CommunityManager.SendStreamerCopies(game, 0, _ms);
                    RefreshActiveTab();
                });

            CreateActionButton(card.transform, new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.62f),
                CommunityDynamicsLabels.StreamerNiche,
                CommunityDynamicsLabels.CoreTech,
                new Color(0.25f, 0.45f, 0.65f, 1f),
                () =>
                {
                    PlaySound(1);
                    CommunityManager.SendStreamerCopies(game, 1, _ms);
                    RefreshActiveTab();
                });

            CreateActionButton(card.transform, new Vector2(0.68f, 0.08f), new Vector2(0.98f, 0.62f),
                CommunityDynamicsLabels.StreamerSpeedrun,
                CommunityDynamicsLabels.Speedrunner,
                new Color(0.3f, 0.55f, 0.4f, 1f),
                () =>
                {
                    PlaySound(1);
                    CommunityManager.SendStreamerCopies(game, 2, _ms);
                    RefreshActiveTab();
                });

            return card;
        }

        #endregion

        #region Tab 4: Crisis PR & Damage Control

        private void PopulateCrisisTab()
        {
            ClearSpawnedItems();
            if (_crisisContent == null) return;

            var bombs = CommunityManager.Data.activeReviewBombs;

            if (bombs.Count == 0)
            {
                var banner = CreateTextCard(_crisisContent, CommunityDynamicsLabels.NoReviewBomb, 90f);
                _spawnedItems.Add(banner);
                return;
            }

            // Warning header
            var warnObj = CreateTextCard(_crisisContent, CommunityDynamicsLabels.ReviewBombWarning, 45f);
            var warnImg = warnObj.GetComponent<Image>();
            if (warnImg != null) warnImg.color = new Color(0.7f, 0.15f, 0.15f, 0.95f);
            _spawnedItems.Add(warnObj);

            foreach (var b in bombs)
            {
                var card = CreateCrisisCard(_crisisContent, b);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreateCrisisCard(Transform parent, ReviewBombState bomb)
        {
            var card = new GameObject("CrisisCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.sizeDelta = new Vector2(0f, 160f);

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.35f, 0.18f, 0.18f, 0.95f);

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(card.transform, false);
            var titleRt = (RectTransform)titleObj.transform;
            titleRt.anchorMin = new Vector2(0.02f, 0.70f);
            titleRt.anchorMax = new Vector2(0.98f, 0.95f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = GetGameFont();
            titleTxt.fontSize = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = Color.white;
            titleTxt.text = $"<color=#FF5555>⚠ {bomb.gameName}</color>  •  {CommunityDynamicsLabels.WeeksRemaining} {bomb.weeksRemaining}  •  {CommunityDynamicsLabels.NegativeSurge} -{bomb.initialNegativeSurge:N0}";

            // 3 PR Measures
            CreateActionButton(card.transform, new Vector2(0.02f, 0.08f), new Vector2(0.32f, 0.62f),
                CommunityDynamicsLabels.ApologyRoadmap,
                CommunityDynamicsLabels.HaltCrash,
                bomb.apologyIssued ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.7f, 0.45f, 0.2f, 1f),
                () =>
                {
                    if (bomb.apologyIssued) return;
                    PlaySound(1);
                    CommunityManager.IssuePublicApology(bomb.gameId, _ms);
                    RefreshActiveTab();
                });

            CreateActionButton(card.transform, new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.62f),
                CommunityDynamicsLabels.EmergencyHotfix,
                CommunityDynamicsLabels.RapidFix,
                bomb.hotfixCommitted ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.3f, 0.55f, 0.5f, 1f),
                () =>
                {
                    if (bomb.hotfixCommitted) return;
                    PlaySound(1);
                    CommunityManager.CommitEmergencyHotfix(bomb.gameId, _ms);
                    RefreshActiveTab();
                });

            CreateActionButton(card.transform, new Vector2(0.68f, 0.08f), new Vector2(0.98f, 0.62f),
                CommunityDynamicsLabels.PlayerCompensation,
                CommunityDynamicsLabels.EndCrisis,
                bomb.compensationGiven ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.2f, 0.6f, 0.35f, 1f),
                () =>
                {
                    if (bomb.compensationGiven) return;
                    PlaySound(1);
                    CommunityManager.GivePlayerCompensation(bomb.gameId, _ms);
                    RefreshActiveTab();
                });

            return card;
        }

        #endregion

        #region UI Helpers

        private GameObject CreateTextCard(Transform parent, string message, float height)
        {
            var card = new GameObject("TextCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.sizeDelta = new Vector2(0f, height);

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.28f, 0.33f, 0.95f);

            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(card.transform, false);
            var txtRt = (RectTransform)txtObj.transform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(12f, 6f);
            txtRt.offsetMax = new Vector2(-12f, -6f);

            var txt = txtObj.GetComponent<Text>();
            txt.font = GetGameFont();
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = message;

            return card;
        }

        private void CreateActionButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string title, string sub, Color btnColor, Action onClick)
        {
            var btnObj = new GameObject("ActionBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var btnRt = (RectTransform)btnObj.transform;
            btnRt.anchorMin = anchorMin;
            btnRt.anchorMax = anchorMax;
            btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;

            var img = btnObj.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = btnColor;

            var txtObj = new GameObject("BtnText", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = (RectTransform)txtObj.transform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6f, 4f);
            txtRt.offsetMax = new Vector2(-6f, -4f);

            var txt = txtObj.GetComponent<Text>();
            txt.font = GetGameFont();
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = $"{title}\n<color=#DDDDDD>{sub}</color>";

            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        #endregion
    }
}
