using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Buy/Sell shares dialogs with tiered purchase options (5%, 25%, 51%, 100%)
    /// and real-time price calculation based on company valuation.
    /// </summary>
    internal static class ShareTransactionDialogs
    {
        private static GameObject buyDialog;
        private static GameObject sellDialog;
        private static publisherScript currentCompany;
        private static mainScript mainS;
        private static GUI_Main guiMainS;

        internal static void ShowBuyDialog(publisherScript pub, mainScript ms, GUI_Main guiMain)
        {
            currentCompany = pub;
            mainS = ms;
            guiMainS = guiMain;

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

            buyDialog = new GameObject("BuySharesDialog");
            buyDialog.transform.SetParent(canvas.transform, false);

            var rt = buyDialog.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 450);
            rt.anchoredPosition = Vector2.zero;

            var bg = buyDialog.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);

            var stock = StockData.GetOrCreate(currentCompany.myID);
            long marketCap = StockData.CalculateCompanyValue(currentCompany, mainS);

            // Title
            CreateText(buyDialog, $"Buy Shares: {currentCompany.GetName()}",
                new Vector2(0, 200), new Vector2(480, 40), 18, TextAnchor.MiddleCenter, Color.white);

            // Company info
            CreateText(buyDialog, $"Market Value: ${FormatMoney(marketCap)}",
                new Vector2(0, 160), new Vector2(480, 30), 14, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f));

            CreateText(buyDialog, $"Your Current Stake: {stock.playerShare:F1}%",
                new Vector2(0, 135), new Vector2(480, 25), 13, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

            // Purchase options (tiered)
            float yPos = 80;

            // 5% - Dividend Rights
            if (stock.playerShare < 5f)
            {
                CreatePurchaseOption(buyDialog, "5% - Dividend Rights",
                    "Earn quarterly dividends from company revenue",
                    5f - stock.playerShare, marketCap, yPos);
                yPos -= 75;
            }

            // 25% - Board Seat
            if (stock.playerShare < 25f)
            {
                CreatePurchaseOption(buyDialog, "25% - Board Seat",
                    "See their secret projects before announcement",
                    25f - stock.playerShare, marketCap, yPos);
                yPos -= 75;
            }

            // 51% - Controlling Interest
            if (stock.playerShare < 51f)
            {
                CreatePurchaseOption(buyDialog, "51% - Controlling Interest",
                    "Direct their game development and publishing",
                    51f - stock.playerShare, marketCap, yPos);
                yPos -= 75;
            }

            // 100% - Full Acquisition
            if (stock.playerShare < 100f)
            {
                CreatePurchaseOption(buyDialog, "100% - Full Acquisition",
                    "Complete merger - absorb all assets and IPs",
                    100f - stock.playerShare, marketCap, yPos);
            }

            // Close button
            CreateButton(buyDialog, "Cancel", new Vector2(0, -195), new Vector2(120, 35), () =>
            {
                UnityEngine.Object.Destroy(buyDialog);
            }, new Color(0.4f, 0.2f, 0.2f));

            buyDialog.SetActive(true);
        }

        private static void CreatePurchaseOption(GameObject parent, string title, string description,
            float sharePercent, long marketCap, float yPos)
        {
            // Container
            var container = new GameObject($"Option_{title}");
            container.transform.SetParent(parent.transform, false);

            var rt = container.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460, 70);
            rt.anchoredPosition = new Vector2(0, yPos);

            var bg = container.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // Title
            CreateText(container, title, new Vector2(-120, 20), new Vector2(200, 25), 14, TextAnchor.MiddleLeft, Color.white);

            // Description
            CreateText(container, description, new Vector2(-120, -5), new Vector2(200, 20), 11, TextAnchor.MiddleLeft,
                new Color(0.7f, 0.7f, 0.7f));

            // Price
            long cost = (long)(marketCap * sharePercent / 100f);
            CreateText(container, $"${FormatMoney(cost)}", new Vector2(-120, -22), new Vector2(200, 20), 12,
                TextAnchor.MiddleLeft, new Color(1f, 0.8f, 0.3f));

            // Buy button
            CreateButton(container, "Buy", new Vector2(150, 0), new Vector2(100, 50), () =>
            {
                ExecutePurchase(sharePercent, cost);
            }, new Color(0.2f, 0.5f, 0.3f));
        }

        private static void ExecutePurchase(float sharePercent, long cost)
        {
            if (mainS.money < cost)
            {
                guiMainS?.ShowNoMoney();
                return;
            }

            var stock = StockData.GetOrCreate(currentCompany.myID);

            // Check if this is hostile takeover (company not willing to sell)
            bool isHostile = stock.playerShare < 25f && !currentCompany.notForSale;

            mainS.Pay(cost, 28); // Category 28: company acquisitions
            stock.playerShare += sharePercent;

            // Handle tier-specific effects
            if (stock.playerShare >= 100f)
            {
                // Full acquisition - convert to subsidiary
                stock.playerShare = 100f;
                currentCompany.SetAsTochterfirma(mainS.myID);
                currentCompany.ownerID = mainS.myID;

                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>Full acquisition complete!</color>\n\n" +
                        $"{currentCompany.GetName()} is now your subsidiary.", false);
                }
            }
            else if (stock.playerShare >= 51f)
            {
                // Controlling interest - can direct their development
                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>Controlling interest acquired!</color>\n\n" +
                        $"You now control {currentCompany.GetName()}'s strategic direction.", false);
                }
            }
            else if (stock.playerShare >= 25f)
            {
                // Board seat - see their projects early
                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>Board seat secured!</color>\n\n" +
                        $"You'll be notified of {currentCompany.GetName()}'s secret projects.", false);
                }
            }
            else if (stock.playerShare >= 5f)
            {
                // Dividend rights
                if (guiMainS != null)
                {
                    guiMainS.MessageBox($"<color=green>Dividend rights acquired!</color>\n\n" +
                        $"You'll receive quarterly payments from {currentCompany.GetName()}'s revenue.", false);
                }
            }

            StockMarketPlugin.Log.LogInfo($"Purchased {sharePercent:F1}% of {currentCompany.GetName()} for ${cost}");

            UnityEngine.Object.Destroy(buyDialog);
            StockMarketWindow.Refresh(mainS, guiMainS);
        }

        private static void CreateSellDialog()
        {
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas == null || currentCompany == null || mainS == null) return;

            sellDialog = new GameObject("SellSharesDialog");
            sellDialog.transform.SetParent(canvas.transform, false);

            var rt = sellDialog.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(450, 350);
            rt.anchoredPosition = Vector2.zero;

            var bg = sellDialog.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);

            var stock = StockData.GetOrCreate(currentCompany.myID);
            long marketCap = StockData.CalculateCompanyValue(currentCompany, mainS);

            // Title
            CreateText(sellDialog, $"Sell Shares: {currentCompany.GetName()}",
                new Vector2(0, 150), new Vector2(430, 40), 18, TextAnchor.MiddleCenter, Color.white);

            CreateText(sellDialog, $"Your Stake: {stock.playerShare:F1}%",
                new Vector2(0, 110), new Vector2(430, 30), 14, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f));

            // Sell options
            float yPos = 50;

            // Sell 25%
            if (stock.playerShare >= 25f)
            {
                long value = (long)(marketCap * 25f / 100f);
                CreateSellOption(sellDialog, "Sell 25%", value, 25f, yPos);
                yPos -= 60;
            }

            // Sell 50%
            if (stock.playerShare >= 50f)
            {
                long value = (long)(marketCap * 50f / 100f);
                CreateSellOption(sellDialog, "Sell 50%", value, 50f, yPos);
                yPos -= 60;
            }

            // Sell All
            long totalValue = (long)(marketCap * stock.playerShare / 100f);
            CreateSellOption(sellDialog, "Sell All", totalValue, stock.playerShare, yPos);

            // Close button
            CreateButton(sellDialog, "Cancel", new Vector2(0, -145), new Vector2(120, 35), () =>
            {
                UnityEngine.Object.Destroy(sellDialog);
            }, new Color(0.4f, 0.2f, 0.2f));

            sellDialog.SetActive(true);
        }

        private static void CreateSellOption(GameObject parent, string label, long value, float sharePercent, float yPos)
        {
            var container = new GameObject($"SellOption_{label}");
            container.transform.SetParent(parent.transform, false);

            var rt = container.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 50);
            rt.anchoredPosition = new Vector2(0, yPos);

            var bg = container.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            CreateText(container, label, new Vector2(-100, 0), new Vector2(150, 30), 14, TextAnchor.MiddleLeft, Color.white);
            CreateText(container, $"+${FormatMoney(value)}", new Vector2(-100, -15), new Vector2(150, 20), 12,
                TextAnchor.MiddleLeft, new Color(0.3f, 0.9f, 0.3f));

            CreateButton(container, "Sell", new Vector2(120, 0), new Vector2(100, 40), () =>
            {
                ExecuteSale(sharePercent, value);
            }, new Color(0.5f, 0.3f, 0.2f));
        }

        private static void ExecuteSale(float sharePercent, long value)
        {
            var stock = StockData.GetOrCreate(currentCompany.myID);

            if (stock.playerShare < sharePercent)
            {
                guiMainS?.MessageBox("Not enough shares to sell!", false);
                return;
            }

            stock.playerShare -= sharePercent;
            mainS.Earn(value, 12); // Category 12: financial income

            // Remove subsidiary status if selling below 100%
            if (stock.playerShare < 100f && currentCompany.IsMyTochterfirma())
            {
                currentCompany.RemoveTochterfirma();
                currentCompany.ownerID = -1;
            }

            StockMarketPlugin.Log.LogInfo($"Sold {sharePercent:F1}% of {currentCompany.GetName()} for ${value}");

            UnityEngine.Object.Destroy(sellDialog);
            StockMarketWindow.Refresh(mainS, guiMainS);
        }

        private static GameObject CreateText(GameObject parent, string text, Vector2 pos, Vector2 size,
            int fontSize, TextAnchor alignment, Color color)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent.transform, false);

            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;

            return obj;
        }

        private static GameObject CreateButton(GameObject parent, string text, Vector2 pos, Vector2 size,
            Action onClick, Color? color = null)
        {
            var btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent.transform, false);

            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var img = btnObj.AddComponent<Image>();
            img.color = color ?? new Color(0.2f, 0.4f, 0.6f, 0.9f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            CreateText(btnObj, text, Vector2.zero, size, 12, TextAnchor.MiddleCenter, Color.white);

            return btnObj;
        }

        private static string FormatMoney(long amount)
        {
            if (amount >= 1000000000) return $"{amount / 1000000000.0:F1}B";
            if (amount >= 1000000) return $"{amount / 1000000.0:F1}M";
            if (amount >= 1000) return $"{amount / 1000.0:F0}K";
            return amount.ToString();
        }
    }
}
