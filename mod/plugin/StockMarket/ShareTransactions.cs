using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Native Buy/Sell shares dialogs matching 100% Mad Games Tycoon 2 UI styling,
    /// with tiered purchase options (5%, 25%, 51%, 100%), real-time pricing,
    /// and pre-shaped Arabic labels.
    /// </summary>
    internal static class ShareTransactionDialogs
    {
        private static GameObject buyDialog;
        private static GameObject sellDialog;
        private static publisherScript currentCompany;
        private static mainScript mainS;
        private static GUI_Main guiMainS;

        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private static Sprite GetGameSprite(string name)
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

        private static Font GetGameFont()
        {
            if (mainS != null && mainS.fonts != null && mainS.fonts.Length > 0 && mainS.fonts[0] != null)
                return mainS.fonts[0];
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void UpdateLanguage()
        {
            var settings = mainS != null ? mainS.GetComponent<settingsScript>() : null;
            StockMarketLabels.Arabic = settings != null && settings.language == 13;
        }

        internal static void ShowBuyDialog(publisherScript pub, mainScript ms, GUI_Main guiMain)
        {
            currentCompany = pub;
            mainS = ms;
            guiMainS = guiMain;
            UpdateLanguage();

            if (buyDialog != null)
            {
                UnityEngine.Object.Destroy(buyDialog);
            }

            CreateBuyDialog();
        }

        internal static void ShowSellDialog(publisherScript pub, mainScript ms, GUI_Main guiMain)
        {
            currentCompany = pub;
            mainS = ms;
            guiMainS = guiMain;
            UpdateLanguage();

            if (sellDialog != null)
            {
                UnityEngine.Object.Destroy(sellDialog);
            }

            CreateSellDialog();
        }

        private static void CreateBuyDialog()
        {
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas == null || currentCompany == null || mainS == null) return;

            buyDialog = new GameObject("BuySharesDialog_Root", typeof(RectTransform));
            buyDialog.transform.SetParent(canvas.transform, false);

            var rootRt = (RectTransform)buyDialog.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

            var blocker = buyDialog.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);

            // Window Frame
            var window = new GameObject("Window", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(buyDialog.transform, false);
            var winRt = (RectTransform)window.transform;
            winRt.sizeDelta = new Vector2(560f, 520f);
            winRt.anchoredPosition = Vector2.zero;

            var borderImg = window.GetComponent<Image>();
            borderImg.sprite = GetGameSprite("window1_Border");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(window.transform, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(8f, 8f);
            fillRt.offsetMax = new Vector2(-8f, -8f);

            var fillImg = fill.GetComponent<Image>();
            fillImg.sprite = GetGameSprite("window1_FillVerlauf");
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.92f, 0.94f, 0.97f, 1f);

            // Title Bar
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(fill.transform, false);
            var titleRt = (RectTransform)titleBar.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0f, 40f);

            var titleImg = titleBar.GetComponent<Image>();
            titleImg.sprite = GetGameSprite("titleLeiste");
            titleImg.type = Image.Type.Sliced;
            titleImg.color = Color.white;

            CreateNativeText(titleBar.transform, $"{StockMarketLabels.BuySharesTitle} {currentCompany.GetName()}",
                new Vector2(16f, 0f), new Vector2(-50f, 0f), Vector2.zero, Vector2.one, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

            // Close button
            var closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
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

            closeBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                PlaySound(3);
                UnityEngine.Object.Destroy(buyDialog);
            });

            var stock = StockData.GetOrCreate(currentCompany.myID);
            long marketCap = StockData.CalculateCompanyValue(currentCompany, mainS);

            // Overview Info Card
            var infoCard = CreateNativeCard(fill.transform, "InfoCard", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(-16f, 50f));
            CreateNativeText(infoCard.transform, $"{StockMarketLabels.MarketCap} <color=#228833>${marketCap:N0}</color>   |   {StockMarketLabels.CurrentStake} <color=#1166AA>{stock.playerShare:F1}%</color>",
                new Vector2(10f, 0f), new Vector2(-10f, 0f), Vector2.zero, Vector2.one, 13, FontStyle.Bold, new Color(0.15f, 0.20f, 0.25f), TextAnchor.MiddleCenter);

            // 4 Options Container
            float yPos = -106f;

            // 5% - Dividend Rights
            if (stock.playerShare < 5f)
            {
                CreateNativePurchaseOption(fill.transform, StockMarketLabels.TierDividend, StockMarketLabels.TierDividendDesc, 5f - stock.playerShare, marketCap, yPos);
                yPos -= 78f;
            }

            // 25% - Board Seat
            if (stock.playerShare < 25f)
            {
                CreateNativePurchaseOption(fill.transform, StockMarketLabels.TierBoard, StockMarketLabels.TierBoardDesc, 25f - stock.playerShare, marketCap, yPos);
                yPos -= 78f;
            }

            // 51% - Controlling Interest
            if (stock.playerShare < 51f)
            {
                CreateNativePurchaseOption(fill.transform, StockMarketLabels.TierControl, StockMarketLabels.TierControlDesc, 51f - stock.playerShare, marketCap, yPos);
                yPos -= 78f;
            }

            // 100% - Full Acquisition
            if (stock.playerShare < 100f)
            {
                CreateNativePurchaseOption(fill.transform, StockMarketLabels.TierAcquisition, StockMarketLabels.TierAcquisitionDesc, 100f - stock.playerShare, marketCap, yPos);
                yPos -= 78f;
            }

            // Cancel Button at bottom
            var cancelBtn = CreateNativeButton(fill.transform, "CancelBtn", StockMarketLabels.Cancel, new Color(0.7f, 0.25f, 0.25f), 13, () =>
            {
                PlaySound(3);
                UnityEngine.Object.Destroy(buyDialog);
            });
            var cancelRt = (RectTransform)cancelBtn.transform;
            cancelRt.anchorMin = new Vector2(0.5f, 0f);
            cancelRt.anchorMax = new Vector2(0.5f, 0f);
            cancelRt.pivot = new Vector2(0.5f, 0f);
            cancelRt.anchoredPosition = new Vector2(0f, 10f);
            cancelRt.sizeDelta = new Vector2(130f, 36f);

            buyDialog.SetActive(true);
            PlaySound(0);
        }

        private static void CreateNativePurchaseOption(Transform parent, string title, string description,
            float sharePercent, long marketCap, float yPos)
        {
            var card = CreateNativeCard(parent, $"Option_{sharePercent}", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(-16f, 72f));

            // Title & Description
            CreateNativeText(card.transform, $"<color=#004488>{title}</color>",
                new Vector2(14f, 40f), new Vector2(-130f, 66f), Vector2.zero, Vector2.zero, 13, FontStyle.Bold, Color.black, TextAnchor.MiddleLeft);

            CreateNativeText(card.transform, description,
                new Vector2(14f, 18f), new Vector2(-130f, 40f), Vector2.zero, Vector2.zero, 11, FontStyle.Normal, new Color(0.35f, 0.38f, 0.42f), TextAnchor.MiddleLeft);

            long cost = (long)(marketCap * sharePercent / 100f);
            CreateNativeText(card.transform, $"{StockMarketLabels.Cash} <color=#228833>${cost:N0}</color> ({sharePercent:F1}%)",
                new Vector2(14f, 2f), new Vector2(-130f, 20f), Vector2.zero, Vector2.zero, 11, FontStyle.Bold, new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);

            // Buy Button
            var buyBtn = CreateNativeButton(card.transform, "BuyBtn", StockMarketLabels.BuyBtn, new Color(0.18f, 0.65f, 0.35f), 13, () =>
            {
                ExecutePurchase(sharePercent, cost);
            });
            var bRt = (RectTransform)buyBtn.transform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-12f, 0f);
            bRt.sizeDelta = new Vector2(100f, 44f);
        }

        private static void CreateSellDialog()
        {
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas == null || currentCompany == null || mainS == null) return;

            sellDialog = new GameObject("SellSharesDialog_Root", typeof(RectTransform));
            sellDialog.transform.SetParent(canvas.transform, false);

            var rootRt = (RectTransform)sellDialog.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

            var blocker = sellDialog.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);

            var window = new GameObject("Window", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(sellDialog.transform, false);
            var winRt = (RectTransform)window.transform;
            winRt.sizeDelta = new Vector2(500f, 400f);
            winRt.anchoredPosition = Vector2.zero;

            var borderImg = window.GetComponent<Image>();
            borderImg.sprite = GetGameSprite("window1_Border");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(window.transform, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(8f, 8f);
            fillRt.offsetMax = new Vector2(-8f, -8f);

            var fillImg = fill.GetComponent<Image>();
            fillImg.sprite = GetGameSprite("window1_FillVerlauf");
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.92f, 0.94f, 0.97f, 1f);

            // Title Bar
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(fill.transform, false);
            var titleRt = (RectTransform)titleBar.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0f, 40f);

            var titleImg = titleBar.GetComponent<Image>();
            titleImg.sprite = GetGameSprite("titleLeiste");
            titleImg.type = Image.Type.Sliced;
            titleImg.color = Color.white;

            CreateNativeText(titleBar.transform, $"{StockMarketLabels.SellSharesTitle} {currentCompany.GetName()}",
                new Vector2(16f, 0f), new Vector2(-50f, 0f), Vector2.zero, Vector2.one, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

            var stock = StockData.GetOrCreate(currentCompany.myID);
            long marketCap = StockData.CalculateCompanyValue(currentCompany, mainS);

            // Info Card
            var infoCard = CreateNativeCard(fill.transform, "InfoCard", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(-16f, 44f));
            CreateNativeText(infoCard.transform, $"{StockMarketLabels.CurrentStake} <color=#1166AA>{stock.playerShare:F1}%</color>",
                new Vector2(10f, 0f), new Vector2(-10f, 0f), Vector2.zero, Vector2.one, 13, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);

            float yPos = -100f;

            if (stock.playerShare >= 5f)
            {
                long val5 = (long)(marketCap * 5f / 100f);
                CreateNativeSellOption(fill.transform, $"{StockMarketLabels.Sell5} (+${val5:N0})", val5, 5f, yPos);
                yPos -= 56f;
            }

            if (stock.playerShare >= 25f)
            {
                long val25 = (long)(marketCap * 25f / 100f);
                CreateNativeSellOption(fill.transform, $"Sell 25% (+${val25:N0})", val25, 25f, yPos);
                yPos -= 56f;
            }

            long totalVal = (long)(marketCap * stock.playerShare / 100f);
            CreateNativeSellOption(fill.transform, $"Sell All 100% (+${totalVal:N0})", totalVal, stock.playerShare, yPos);

            // Cancel Button
            var cancelBtn = CreateNativeButton(fill.transform, "CancelBtn", StockMarketLabels.Cancel, new Color(0.7f, 0.25f, 0.25f), 13, () =>
            {
                PlaySound(3);
                UnityEngine.Object.Destroy(sellDialog);
            });
            var cancelRt = (RectTransform)cancelBtn.transform;
            cancelRt.anchorMin = new Vector2(0.5f, 0f);
            cancelRt.anchorMax = new Vector2(0.5f, 0f);
            cancelRt.pivot = new Vector2(0.5f, 0f);
            cancelRt.anchoredPosition = new Vector2(0f, 10f);
            cancelRt.sizeDelta = new Vector2(120f, 36f);

            sellDialog.SetActive(true);
            PlaySound(0);
        }

        private static void CreateNativeSellOption(Transform parent, string label, long value, float sharePercent, float yPos)
        {
            var card = CreateNativeCard(parent, "SellOpt", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(-16f, 48f));

            CreateNativeText(card.transform, label, new Vector2(14f, 0f), new Vector2(-120f, 0f), Vector2.zero, Vector2.one, 12, FontStyle.Bold, new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);

            var sellBtn = CreateNativeButton(card.transform, "SellBtn", StockMarketLabels.SellBtn, new Color(0.75f, 0.35f, 0.2f), 12, () =>
            {
                ExecuteSale(sharePercent, value);
            });
            var sRt = (RectTransform)sellBtn.transform;
            sRt.anchorMin = new Vector2(1f, 0.5f);
            sRt.anchorMax = new Vector2(1f, 0.5f);
            sRt.pivot = new Vector2(1f, 0.5f);
            sRt.anchoredPosition = new Vector2(-10f, 0f);
            sRt.sizeDelta = new Vector2(85f, 34f);
        }

        private static void ExecutePurchase(float sharePercent, long cost)
        {
            if (mainS.money < cost)
            {
                guiMainS?.ShowNoMoney();
                return;
            }

            PlaySound(1);
            var stock = StockData.GetOrCreate(currentCompany.myID);
            mainS.Pay(cost, 28);
            stock.playerShare += sharePercent;

            if (stock.playerShare >= 100f)
            {
                stock.playerShare = 100f;
                currentCompany.SetAsTochterfirma(mainS.myID);
                currentCompany.ownerID = mainS.myID;

                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>{StockMarketLabels.AbsorbSuccess}</color>\n\n{currentCompany.GetName()}", false);
                }
            }
            else if (stock.playerShare >= 51f)
            {
                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>{StockMarketLabels.MajorityControl}</color>\n\n{currentCompany.GetName()}", false);
                }
            }
            else if (stock.playerShare >= 25f)
            {
                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>{StockMarketLabels.SecretIntel}</color>\n\n{currentCompany.GetName()}", false);
                }
            }

            if (buyDialog != null) UnityEngine.Object.Destroy(buyDialog);
            StockMarketWindow.Refresh(mainS, guiMainS);
        }

        private static void ExecuteSale(float sharePercent, long value)
        {
            var stock = StockData.GetOrCreate(currentCompany.myID);
            if (stock.playerShare < sharePercent) return;

            PlaySound(1);
            stock.playerShare -= sharePercent;
            mainS.Earn(value, 12);

            if (stock.playerShare < 100f && currentCompany.IsMyTochterfirma())
            {
                currentCompany.RemoveTochterfirma();
                currentCompany.ownerID = -1;
            }

            if (sellDialog != null) UnityEngine.Object.Destroy(sellDialog);
            StockMarketWindow.Refresh(mainS, guiMainS);
        }

        private static GameObject CreateNativeCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = card.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.88f, 0.93f, 0.95f);

            return card;
        }

        private static GameObject CreateNativeText(Transform parent, string text, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(parent, false);
            var rt = (RectTransform)txtObj.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var txt = txtObj.GetComponent<Text>();
            txt.font = GetGameFont();
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.text = text;

            return txtObj;
        }

        private static GameObject CreateNativeButton(Transform parent, string name, string label, Color color, int fontSize, Action onClick)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            var img = btnObj.GetComponent<Image>();
            img.sprite = GetGameSprite("uiSquareVerlauf");
            img.type = Image.Type.Sliced;
            img.color = color;

            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = (RectTransform)txtObj.transform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

            var txt = txtObj.GetComponent<Text>();
            txt.font = GetGameFont();
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = label;

            return btnObj;
        }

        private static void PlaySound(int id)
        {
            try
            {
                var sfxObj = GameObject.Find("SFX");
                sfxObj?.GetComponent<sfxScript>()?.PlaySound(id, force: true);
            }
            catch { }
        }
    }
}
