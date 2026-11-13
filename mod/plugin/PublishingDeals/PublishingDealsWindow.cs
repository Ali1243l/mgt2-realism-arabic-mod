using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.PublishingDeals
{
    public class PublishingDealsWindow : MonoBehaviour
    {
        public static PublishingDealsWindow Instance { get; private set; }

        public enum ActiveTab
        {
            Pitches = 0,
            ActiveProjects = 1,
            BiddingWars = 2
        }

        private mainScript _ms;
        private GUI_Main _guiMain;
        private sfxScript _sfx;
        private Canvas _canvas;

        private GameObject _windowRoot;
        private RectTransform _windowMainRt;
        private Text _titleText;

        // Tabs
        private ActiveTab _currentTab = ActiveTab.Pitches;
        private Image _tab1Bg, _tab2Bg, _tab3Bg;
        private Text _tab1Label, _tab2Label, _tab3Label;
        private GameObject _tab1Panel, _tab2Panel, _tab3Panel;

        // Content Scrollviews
        private RectTransform _pitchesContent;
        private RectTransform _activeProjectsContent;
        private RectTransform _biddingWarsContent;

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

        public static PublishingDealsWindow GetOrCreate()
        {
            if (Instance != null) return Instance;
            var host = new GameObject("MGT2_PublishingDealsWindow_Host");
            DontDestroyOnLoad(host);
            return host.AddComponent<PublishingDealsWindow>();
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
            PublishingManager.GeneratePitchesIfNeeded(_ms);

            if (_windowRoot == null)
            {
                BuildNativeWindow();
            }

            _windowRoot.SetActive(true);

            if (_guiMain != null) _guiMain.OpenMenu(hideChars: false);
            else if (_ms != null && !_ms.multiplayer) _ms.PauseGame(true);

            PlaySound(5);
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
            PublishingDealsLabels.Arabic = settings != null && settings.language == 13;
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

            _windowRoot = new GameObject("MGT2_PubDeals_Root", typeof(RectTransform));
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

            var fillWindow = new GameObject("Fill_Window", typeof(RectTransform), typeof(Image));
            fillWindow.transform.SetParent(windowMain.transform, false);
            var fillRt = (RectTransform)fillWindow.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            var fillImg = fillWindow.GetComponent<Image>();
            fillImg.sprite = GetGameSprite("window1_FillVerlauf");
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.92f, 0.925f, 0.935f, 1f);

            // Title Bar
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

            var titleTextGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleTextGo.transform.SetParent(titelleiste.transform, false);
            var ttRt = (RectTransform)titleTextGo.transform;
            ttRt.anchorMin = new Vector2(0f, 0f);
            ttRt.anchorMax = new Vector2(1f, 1f);
            ttRt.offsetMin = new Vector2(40f, 0f);
            ttRt.offsetMax = new Vector2(-55f, 0f);

            _titleText = titleTextGo.GetComponent<Text>();
            _titleText.font = GetGameFont();
            _titleText.fontSize = 22;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.text = PublishingDealsLabels.Title;

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

            closeGo.GetComponent<Button>().onClick.AddListener(CloseWindow);

            BuildTabsHeader(windowMain.transform);
            BuildTabPanels(windowMain.transform);
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

            // Tab 1: Pitches
            _tab1Bg = CreateTabButton(tabsContainer.transform, "Tab1", 0f, 0.33f, () => SwitchTab(ActiveTab.Pitches), out _tab1Label);
            // Tab 2: Active Projects
            _tab2Bg = CreateTabButton(tabsContainer.transform, "Tab2", 0.335f, 0.665f, () => SwitchTab(ActiveTab.ActiveProjects), out _tab2Label);
            // Tab 3: Bidding Wars
            _tab3Bg = CreateTabButton(tabsContainer.transform, "Tab3", 0.67f, 1f, () => SwitchTab(ActiveTab.BiddingWars), out _tab3Label);
        }

        private Image CreateTabButton(Transform parent, string name, float minX, float maxX, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(minX, 0f);
            rt.anchorMax = new Vector2(maxX, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf") ?? GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;

            go.GetComponent<Button>().onClick.AddListener(onClick);

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var tRt = (RectTransform)txtGo.transform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            label = txtGo.GetComponent<Text>();
            label.font = GetGameFont();
            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;

            return img;
        }

        private void BuildTabPanels(Transform parent)
        {
            _tab1Panel = CreatePanel(parent, "Tab1Panel", out _pitchesContent);
            _tab2Panel = CreatePanel(parent, "Tab2Panel", out _activeProjectsContent);
            _tab3Panel = CreatePanel(parent, "Tab3Panel", out _biddingWarsContent);
        }

        private GameObject CreatePanel(Transform parent, string name, out RectTransform contentRt)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var pRt = (RectTransform)panel.transform;
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(16f, 16f);
            pRt.offsetMax = new Vector2(-16f, -108f);

            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(panel.transform, false);
            var sRt = (RectTransform)scrollGo.transform;
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.sizeDelta = Vector2.zero;

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
            contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = vRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 25f;

            return panel;
        }

        #endregion

        #region Tab Switching

        public void SwitchTab(ActiveTab tab)
        {
            _currentTab = tab;
            PlaySound(3);

            _tab1Panel.SetActive(tab == ActiveTab.Pitches);
            _tab2Panel.SetActive(tab == ActiveTab.ActiveProjects);
            _tab3Panel.SetActive(tab == ActiveTab.BiddingWars);

            Color colActive = new Color(0.161f, 0.502f, 0.725f, 1f);
            Color colInactive = new Color(0.80f, 0.82f, 0.85f, 1f);

            _tab1Bg.color = tab == ActiveTab.Pitches ? colActive : colInactive;
            _tab2Bg.color = tab == ActiveTab.ActiveProjects ? colActive : colInactive;
            _tab3Bg.color = tab == ActiveTab.BiddingWars ? colActive : colInactive;

            _tab1Label.text = PublishingDealsLabels.TabPitches;
            _tab2Label.text = PublishingDealsLabels.TabActive;
            _tab3Label.text = PublishingDealsLabels.TabBidding;

            _tab1Label.color = tab == ActiveTab.Pitches ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);
            _tab2Label.color = tab == ActiveTab.ActiveProjects ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);
            _tab3Label.color = tab == ActiveTab.BiddingWars ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);

            RefreshActiveTab();
        }

        public void RefreshActiveTab()
        {
            if (_ms == null) return;
            UpdateLanguage();

            if (_titleText != null) _titleText.text = PublishingDealsLabels.Title;

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null) Destroy(_spawnedItems[i]);
            }
            _spawnedItems.Clear();

            switch (_currentTab)
            {
                case ActiveTab.Pitches:
                    RefreshPitches();
                    break;
                case ActiveTab.ActiveProjects:
                    RefreshActiveProjects();
                    break;
                case ActiveTab.BiddingWars:
                    RefreshBiddingWars();
                    break;
            }
        }

        #endregion

        #region Tab 1: Pitches

        private void RefreshPitches()
        {
            var pitches = PublishingManager.Data.currentPitches;
            if (pitches.Count == 0)
            {
                var empty = CreateEmptyNotice(_pitchesContent, PublishingDealsLabels.NoPitches);
                _spawnedItems.Add(empty);
                return;
            }

            foreach (var pitch in pitches)
            {
                var card = CreatePitchCard(pitch);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreatePitchCard(StudioPitch pitch)
        {
            var card = new GameObject("PitchCard_" + pitch.pitchId, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(_pitchesContent, false);

            var le = card.GetComponent<LayoutElement>();
            le.preferredHeight = 76f;
            le.minHeight = 76f;

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.97f, 0.975f, 0.985f, 1f);

            string genreName = _ms?.genres_ != null ? _ms.genres_.GetName(pitch.genre) : $"Genre {pitch.genre}";

            // Title & Developer
            CreateText(card.transform, "Title", new Vector2(0.03f, 0.52f), new Vector2(0.40f, 0.94f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, true, $"<b>{pitch.gameTitle}</b>");
            CreateText(card.transform, "Dev", new Vector2(0.03f, 0.08f), new Vector2(0.40f, 0.50f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 12, false, $"{pitch.developerName}  ({genreName} - {pitch.tier})");

            // Budget & Timeline
            CreateText(card.transform, "Budget", new Vector2(0.40f, 0.52f), new Vector2(0.68f, 0.94f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, false, $"{PublishingDealsLabels.PitchBudget} <b><color=#1E8C3A>{_ms.GetMoney(pitch.requestedBudget, true)}</color></b>");
            CreateText(card.transform, "Time", new Vector2(0.40f, 0.08f), new Vector2(0.68f, 0.50f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 12, false, $"{PublishingDealsLabels.DevTime} {pitch.devWeeks} {PublishingDealsLabels.Weeks}  |  Split: {Mathf.RoundToInt(pitch.proposedRoyaltySplit)}% dev");

            // Fund & Accept Button
            var fundBtn = CreateNativeButton(card.transform, "FundBtn", new Vector2(-15f, 0f), new Vector2(170f, 38f), () =>
            {
                ShowDealNegotiationModal(pitch);
            }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            fundBtn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f);
            var fTxt = fundBtn.GetComponentInChildren<Text>();
            fTxt.text = PublishingDealsLabels.AcceptDeal;
            fTxt.fontSize = 12;

            return card;
        }

        #endregion

        #region Tab 2: Active Projects

        private void RefreshActiveProjects()
        {
            var deals = PublishingManager.Data.activeDeals;
            if (deals.Count == 0)
            {
                var empty = CreateEmptyNotice(_activeProjectsContent, PublishingDealsLabels.NoActiveProjects);
                _spawnedItems.Add(empty);
                return;
            }

            foreach (var deal in deals)
            {
                var card = CreateActiveProjectCard(deal);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreateActiveProjectCard(ActiveSecondPartyDeal deal)
        {
            var card = new GameObject("ProjectCard_" + deal.dealId, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(_activeProjectsContent, false);

            var le = card.GetComponent<LayoutElement>();
            le.preferredHeight = 74f;
            le.minHeight = 74f;

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.97f, 0.975f, 0.985f, 1f);

            string status = !deal.isReleased ? $"Dev: {deal.weeksLeft} wks left" : $"Market Sales: {deal.weeksLeft} wks left";
            CreateText(card.transform, "Title", new Vector2(0.03f, 0.52f), new Vector2(0.40f, 0.94f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, true, $"<b>{deal.gameTitle}</b>");
            CreateText(card.transform, "Dev", new Vector2(0.03f, 0.08f), new Vector2(0.40f, 0.50f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 12, false, $"{deal.developerName}  ({status})");

            CreateText(card.transform, "Financials", new Vector2(0.40f, 0.08f), new Vector2(0.70f, 0.92f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, false,
                $"Funded: ${_ms.GetMoney(deal.fundedBudget, true)}\nProfits: <b><color=#1E8C3A>{_ms.GetMoney(deal.totalRevenueEarned, true)}</color></b>");

            // Status Badge
            string badgeText = !deal.isReleased ? "IN DEVELOPMENT" : $"RELEASED (Score: {deal.reviewScore}%)";
            Color badgeColor = !deal.isReleased ? new Color(0.161f, 0.502f, 0.725f, 1f) : new Color(0.18f, 0.65f, 0.35f, 1f);
            var badge = CreateNativeButton(card.transform, "StatusBadge", new Vector2(-15f, 0f), new Vector2(170f, 36f), null, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            badge.GetComponent<Image>().color = badgeColor;
            var bTxt = badge.GetComponentInChildren<Text>();
            bTxt.text = badgeText;
            bTxt.fontSize = 11;

            return card;
        }

        #endregion

        #region Tab 3: Bidding Wars

        private void RefreshBiddingWars()
        {
            var wars = PublishingManager.Data.activeBiddingWars;
            if (wars.Count == 0)
            {
                var empty = CreateEmptyNotice(_biddingWarsContent, PublishingDealsLabels.NoBiddingWars);
                _spawnedItems.Add(empty);
                return;
            }

            foreach (var war in wars)
            {
                var card = CreateBiddingWarCard(war);
                _spawnedItems.Add(card);
            }
        }

        private GameObject CreateBiddingWarCard(BiddingWarItem war)
        {
            var card = new GameObject("BiddingCard_" + war.biddingId, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(_biddingWarsContent, false);

            var le = card.GetComponent<LayoutElement>();
            le.preferredHeight = 74f;
            le.minHeight = 74f;

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("whiteSimple");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.97f, 0.975f, 0.985f, 1f);

            CreateText(card.transform, "Title", new Vector2(0.03f, 0.52f), new Vector2(0.40f, 0.94f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, true, $"<b>{war.gameTitle}</b>");
            CreateText(card.transform, "Dev", new Vector2(0.03f, 0.08f), new Vector2(0.40f, 0.50f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 12, false, $"{war.developerName}  ({war.weeksLeft} wks left to bid)");

            string leader = war.leadingBidderId == -1 ? "<color=#1E8C3A>You (Leading!)</color>" : war.leadingBidderName;
            CreateText(card.transform, "BidInfo", new Vector2(0.40f, 0.08f), new Vector2(0.70f, 0.92f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 13, false,
                $"{PublishingDealsLabels.CurrentHighestBid} <b>{leader}</b>\nBid: <b><color=#1E8C3A>{_ms.GetMoney(war.currentHighestBid, true)}</color></b>");

            long counterBidAmount = war.currentHighestBid + 250000L;
            var bidBtn = CreateNativeButton(card.transform, "CounterBidBtn", new Vector2(-15f, 0f), new Vector2(170f, 38f), () =>
            {
                bool success = PublishingManager.PlaceCounterBid(_ms, war, counterBidAmount, _guiMain);
                if (success) RefreshActiveTab();
            }, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            bidBtn.GetComponent<Image>().color = new Color(0.85f, 0.55f, 0.15f, 1f);
            var bTxt = bidBtn.GetComponentInChildren<Text>();
            bTxt.text = $"{PublishingDealsLabels.CounterBid} (+${_ms.GetMoney(250000L, false)})";
            bTxt.fontSize = 11;

            return card;
        }

        #endregion

        #region Negotiation Modal Dialog

        private GameObject _activeModalDialog;

        private void CloseModalDialog()
        {
            if (_activeModalDialog != null)
            {
                Destroy(_activeModalDialog);
                _activeModalDialog = null;
            }
        }

        private void ShowDealNegotiationModal(StudioPitch pitch)
        {
            CloseModalDialog();
            PlaySound(5);

            float playerRoyaltyCut = 100f - pitch.proposedRoyaltySplit;
            int exclusivePlatformId = -1;
            bool permanent = false;

            _activeModalDialog = new GameObject("NegotiationModal", typeof(RectTransform), typeof(Image));
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

            CreateText(_activeModalDialog.transform, "Title", new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 18, true, $"{pitch.gameTitle} - Publishing Deal Terms");

            // Royalty Split Selector (Player cut: 30%, 50%, 70%)
            CreateText(_activeModalDialog.transform, "SplitLabel", new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, 15, true, $"{PublishingDealsLabels.RoyaltySplit} (Your Share)");

            float[] splits = { 30f, 50f, 70f };
            Button[] sBtns = new Button[3];

            Action updateSplitBtns = () =>
            {
                for (int i = 0; i < splits.Length; i++)
                {
                    bool isCur = Mathf.Approximately(playerRoyaltyCut, splits[i]);
                    sBtns[i].GetComponent<Image>().color = isCur ? new Color(0.161f, 0.502f, 0.725f, 1f) : new Color(0.82f, 0.84f, 0.88f, 1f);
                    sBtns[i].GetComponentInChildren<Text>().color = isCur ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            };

            for (int i = 0; i < 3; i++)
            {
                float cut = splits[i];
                var sBtn = CreateNativeButton(_activeModalDialog.transform, $"SBtn_{cut}", new Vector2((i - 1) * 160f, 40f), new Vector2(150f, 36f), () =>
                {
                    playerRoyaltyCut = cut;
                    updateSplitBtns();
                }, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

                sBtns[i] = sBtn.GetComponent<Button>();
                sBtn.GetComponentInChildren<Text>().text = $"{PublishingDealsLabels.PubShare} {cut:F0}%";
            }
            updateSplitBtns();

            // Console Exclusivity Option (Check if player owns a console)
            var playerConsole = _ms?.arrayPlatformsScripts?.FirstOrDefault(p => p != null && p.ownerID == _ms.myID && p.isUnlocked && !p.vomMarktGenommen);
            string exclText = playerConsole != null ? $"Make Exclusive to {playerConsole.GetName()} (System Seller)" : "Multiplatform Release";

            var exclToggleBtn = CreateNativeButton(_activeModalDialog.transform, "ExclToggle", new Vector2(0f, -40f), new Vector2(460f, 40f), null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var exclToggleTxt = exclToggleBtn.GetComponentInChildren<Text>();

            Action updateExclToggle = () =>
            {
                bool isExcl = exclusivePlatformId >= 0;
                exclToggleBtn.GetComponent<Image>().color = isExcl ? new Color(0.18f, 0.65f, 0.35f, 1f) : new Color(0.70f, 0.72f, 0.76f, 1f);
                exclToggleTxt.text = $"{(isExcl ? "[X]" : "[ ]")} {exclText}";
            };
            updateExclToggle();

            if (playerConsole != null)
            {
                exclToggleBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    exclusivePlatformId = (exclusivePlatformId == -1) ? playerConsole.myID : -1;
                    permanent = exclusivePlatformId >= 0;
                    updateExclToggle();
                });
            }
            else
            {
                exclToggleBtn.GetComponent<Button>().interactable = false;
            }

            // Confirm Fund & Sign Button
            var signBtn = CreateNativeButton(_activeModalDialog.transform, "SignBtn", new Vector2(-110f, -165f), new Vector2(180f, 40f), () =>
            {
                bool success = PublishingManager.FundAndSignDeal(_ms, pitch, playerRoyaltyCut, exclusivePlatformId, permanent, _guiMain);
                if (success)
                {
                    CloseModalDialog();
                    RefreshActiveTab();
                }
            }, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            signBtn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f, 1f);
            signBtn.GetComponentInChildren<Text>().text = PublishingDealsLabels.AcceptDeal;

            // Cancel Button
            var cancelBtn = CreateNativeButton(_activeModalDialog.transform, "CancelBtn", new Vector2(110f, -165f), new Vector2(140f, 40f), CloseModalDialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cancelBtn.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f, 1f);
            cancelBtn.GetComponentInChildren<Text>().text = PublishingDealsLabels.Close;
        }

        #endregion

        #region Factory Helpers

        private GameObject CreateEmptyNotice(Transform parent, string text)
        {
            var go = new GameObject("EmptyNotice", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 60f;

            var t = go.GetComponent<Text>();
            t.font = GetGameFont();
            t.fontSize = 15;
            t.color = new Color(0.5f, 0.55f, 0.6f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
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

        private GameObject CreateNativeButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick, Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin ?? new Vector2(0f, 1f);
            rt.anchorMax = anchorMax ?? new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
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
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        #endregion
    }
}
