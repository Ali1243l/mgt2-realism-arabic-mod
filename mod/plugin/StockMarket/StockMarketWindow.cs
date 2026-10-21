using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Main stock market window: list of companies, their valuations, your ownership %,
    /// and buttons to buy/sell shares. Opened from Management menu.
    /// UI follows the game's style: vertical scrollable list, left-aligned text.
    /// </summary>
    internal static class StockMarketWindow
    {
        private static GameObject windowObj;
        private static GameObject contentPanel;
        private static List<GameObject> companyRows = new List<GameObject>();

        internal static void Show(mainScript ms, GUI_Main guiMain)
        {
            if (windowObj != null)
            {
                windowObj.SetActive(true);
                Refresh(ms, guiMain);
                return;
            }

            CreateWindow(ms, guiMain);
        }

        private static void CreateWindow(mainScript ms, GUI_Main guiMain)
        {
            // Find the game's canvas
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas == null)
            {
                StockMarketPlugin.Log.LogError("Canvas not found!");
                return;
            }

            // Create main window (similar to game's other windows)
            windowObj = new GameObject("StockMarketWindow");
            windowObj.transform.SetParent(canvas.transform, false);
            windowObj.layer = canvas.layer; // Same layer as game UI

            var rt = windowObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900, 600);
            rt.anchoredPosition = Vector2.zero;

            var img = windowObj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Set higher sorting order by moving to end of hierarchy (rendered last = on top)
            windowObj.transform.SetAsLastSibling();

            // Title bar
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(windowObj.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(900, 50);
            titleRt.anchoredPosition = new Vector2(0, 275);

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = GetText(ms, "Stock Market & Company Ownership");
            titleText.font = ms.fonts[0];
            titleText.fontSize = 20;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // Close button
            CreateButton(windowObj, "X", new Vector2(420, 275), new Vector2(40, 40), () =>
            {
                windowObj.SetActive(false);
            });

            // Scroll view for company list
            var scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(windowObj.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(860, 480);
            scrollRt.anchoredPosition = new Vector2(0, -30);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 40f; // Same as game's lists

            // Viewport
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewportObj.AddComponent<RectTransform>();
            viewportRt.sizeDelta = new Vector2(860, 480);
            viewportRt.anchoredPosition = Vector2.zero;

            var viewportMask = viewportObj.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRt;

            // Content panel
            contentPanel = new GameObject("Content");
            contentPanel.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentPanel.AddComponent<RectTransform>();
            contentRt.sizeDelta = new Vector2(840, 1000); // Will expand dynamically
            contentRt.anchoredPosition = new Vector2(0, -500);
            contentRt.pivot = new Vector2(0.5f, 1f);

            scrollRect.content = contentRt;

            // Populate with companies
            Refresh(ms, guiMain);

            windowObj.SetActive(true);
        }

        internal static void Refresh(mainScript ms, GUI_Main guiMain)
        {
            if (contentPanel == null) return;

            // Clear existing rows
            foreach (var row in companyRows)
            {
                if (row != null) UnityEngine.Object.Destroy(row);
            }
            companyRows.Clear();

            // Get all companies
            var companies = GameObject.FindGameObjectsWithTag("Publisher");
            var validCompanies = new List<publisherScript>();

            foreach (var obj in companies)
            {
                var pub = obj.GetComponent<publisherScript>();
                // Show all unlocked AI publishers (not player, not already owned subsidiary)
                if (pub != null && pub.isUnlocked && pub.myID != ms.myID && !pub.IsMyTochterfirma())
                {
                    validCompanies.Add(pub);
                }
            }

            StockMarketPlugin.Log.LogInfo($"Found {validCompanies.Count} companies for stock market");

            // Sort by market cap (highest first)
            validCompanies = validCompanies.OrderByDescending(p => StockData.CalculateCompanyValue(p, ms)).ToList();

            // Create row for each company
            float yPos = -20f;
            foreach (var pub in validCompanies)
            {
                var row = CreateCompanyRow(pub, ms, guiMain, yPos);
                companyRows.Add(row);
                yPos -= 80f; // Row height
            }

            // Adjust content panel height
            var contentRt = contentPanel.GetComponent<RectTransform>();
            contentRt.sizeDelta = new Vector2(840, Math.Max(1000, companyRows.Count * 80 + 40));
        }

        private static GameObject CreateCompanyRow(publisherScript pub, mainScript ms, GUI_Main guiMain, float yPos)
        {
            var rowObj = new GameObject($"Row_{pub.myID}");
            rowObj.transform.SetParent(contentPanel.transform, false);

            var rowRt = rowObj.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(820, 70);
            rowRt.anchoredPosition = new Vector2(0, yPos);

            // Background
            var bg = rowObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Company logo
            var logoObj = new GameObject("Logo");
            logoObj.transform.SetParent(rowObj.transform, false);
            var logoRt = logoObj.AddComponent<RectTransform>();
            logoRt.sizeDelta = new Vector2(60, 60);
            logoRt.anchoredPosition = new Vector2(-370, 0);

            var logoImg = logoObj.AddComponent<Image>();
            logoImg.sprite = pub.GetLogo();
            logoImg.preserveAspect = true;

            // Company name
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(rowObj.transform, false);
            var nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(200, 30);
            nameRt.anchoredPosition = new Vector2(-230, 15);

            var nameText = nameObj.AddComponent<Text>();
            nameText.text = pub.GetName();
            nameText.font = ms.fonts[0];
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;

            // Market cap
            long marketCap = StockData.CalculateCompanyValue(pub, ms);
            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(rowObj.transform, false);
            var valueRt = valueObj.AddComponent<RectTransform>();
            valueRt.sizeDelta = new Vector2(200, 25);
            valueRt.anchoredPosition = new Vector2(-230, -15);

            var valueText = valueObj.AddComponent<Text>();
            valueText.text = $"Value: ${FormatMoney(marketCap)}";
            valueText.font = ms.fonts[0];
            valueText.fontSize = 14;
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.color = new Color(0.8f, 0.8f, 0.8f);

            // Your ownership %
            var stock = StockData.GetOrCreate(pub.myID);
            var ownObj = new GameObject("Ownership");
            ownObj.transform.SetParent(rowObj.transform, false);
            var ownRt = ownObj.AddComponent<RectTransform>();
            ownRt.sizeDelta = new Vector2(120, 30);
            ownRt.anchoredPosition = new Vector2(50, 0);

            var ownText = ownObj.AddComponent<Text>();
            if (stock.playerShare > 0)
            {
                ownText.text = $"{stock.playerShare:F1}%\n{StockData.GetShareTierName(stock.playerShare)}";
                ownText.color = new Color(0.3f, 0.8f, 0.3f);
            }
            else
            {
                ownText.text = "No stake";
                ownText.color = new Color(0.6f, 0.6f, 0.6f);
            }
            ownText.font = ms.fonts[0];
            ownText.fontSize = 12;
            ownText.alignment = TextAnchor.MiddleCenter;

            // Buy button
            CreateButton(rowObj, "Buy Shares", new Vector2(220, 15), new Vector2(120, 30), () =>
            {
                ShowBuyDialog(pub, ms, guiMain);
            });

            // Sell button (only if you own shares)
            if (stock.playerShare > 0)
            {
                CreateButton(rowObj, "Sell", new Vector2(220, -15), new Vector2(120, 30), () =>
                {
                    ShowSellDialog(pub, ms, guiMain);
                });
            }

            // Info button
            CreateButton(rowObj, "Info", new Vector2(360, 0), new Vector2(60, 30), () =>
            {
                ShowCompanyInfo(pub, ms, guiMain);
            });

            return rowObj;
        }

        private static void ShowBuyDialog(publisherScript pub, mainScript ms, GUI_Main guiMain)
        {
            ShareTransactionDialogs.ShowBuyDialog(pub, ms, guiMain);
        }

        private static void ShowSellDialog(publisherScript pub, mainScript ms, GUI_Main guiMain)
        {
            ShareTransactionDialogs.ShowSellDialog(pub, ms, guiMain);
        }

        private static void ShowCompanyInfo(publisherScript pub, mainScript ms, GUI_Main guiMain)
        {
            // TODO: Show detailed company info
            StockMarketPlugin.Log.LogInfo($"Info for {pub.GetName()}");
        }

        private static GameObject CreateButton(GameObject parent, string text, Vector2 position, Vector2 size, Action onClick)
        {
            var btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent.transform, false);

            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.sizeDelta = size;
            btnRt.anchoredPosition = position;

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.4f, 0.6f, 0.9f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);

            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.sizeDelta = size;
            txtRt.anchoredPosition = Vector2.zero;

            var txt = txtObj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 12;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btnObj;
        }

        private static string FormatMoney(long amount)
        {
            if (amount >= 1000000000) return $"{amount / 1000000000.0:F1}B";
            if (amount >= 1000000) return $"{amount / 1000000.0:F1}M";
            if (amount >= 1000) return $"{amount / 1000.0:F0}K";
            return amount.ToString();
        }

        private static string GetText(mainScript ms, string fallback)
        {
            // For now, return English. Later we'll add to textScript
            return fallback;
        }
    }
}
