using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Adds "Stock Market" button to the Management menu (bottom menu bar).
    /// Positioned after the existing buttons in a consistent style.
    /// </summary>
    [HarmonyPatch(typeof(GUI_Main), "Start")]
    internal static class ManagementMenuPatch
    {
        private static GameObject stockMarketButton;

        static void Postfix(GUI_Main __instance)
        {
            try
            {
                // Find the management menu buttons container
                var canvas = GameObject.Find("CanvasInGameMenu");
                if (canvas == null) return;

                var main = GameObject.Find("Main");
                if (main == null) return;

                var ms = main.GetComponent<mainScript>();
                if (ms == null) return;

                // Create button in management bar (we'll add it via the game's button system later)
                // For now, create a floating button for testing
                CreateStockMarketButton(canvas, ms, __instance);

                StockMarketPlugin.Log.LogInfo("Stock Market button added to Management menu");
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError($"Failed to add management button: {ex.Message}");
            }
        }

        private static void CreateStockMarketButton(GameObject canvas, mainScript ms, GUI_Main guiMain)
        {
            // Create a button that opens the stock market window
            // Position it in a visible location for testing (top-right corner)
            stockMarketButton = new GameObject("StockMarketButton");
            stockMarketButton.transform.SetParent(canvas.transform, false);

            var rt = stockMarketButton.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 40);
            rt.anchoredPosition = new Vector2(-100, 350); // Top-right area

            var img = stockMarketButton.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.6f, 0.95f);

            var btn = stockMarketButton.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                StockMarketWindow.Show(ms, guiMain);
            });

            // Button text
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(stockMarketButton.transform, false);

            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.sizeDelta = new Vector2(150, 40);
            txtRt.anchoredPosition = Vector2.zero;

            var txt = txtObj.AddComponent<Text>();
            txt.text = "Stock Market";
            txt.font = ms.fonts[0];
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
    }

    /// <summary>
    /// Keyboard shortcut: F8 opens the stock market window
    /// </summary>
    [HarmonyPatch(typeof(GUI_Main), "Update")]
    internal static class KeyboardShortcutPatch
    {
        static void Postfix(GUI_Main __instance)
        {
            // F8 opens stock market
            if (Input.GetKeyDown(KeyCode.F8))
            {
                var main = GameObject.Find("Main");
                if (main != null)
                {
                    var ms = main.GetComponent<mainScript>();
                    if (ms != null && !ms.multiplayer)
                    {
                        StockMarketWindow.Show(ms, __instance);
                    }
                }
            }
        }
    }
}
