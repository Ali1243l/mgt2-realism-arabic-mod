using System.Linq;
using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.StockMarket
{
    /// <summary>
    /// Adds "Stock Market" button to the Management dropdown menu (GUI_MainButtons.uiObjects[9]).
    /// Inherits native styling, fonts, and sound effects from the game.
    /// </summary>
    [HarmonyPatch(typeof(GUI_MainButtons), nameof(GUI_MainButtons.BUTTON_Management))]
    internal static class ManagementMenuPatch
    {
        private static GameObject _stockMarketButton;

        static void Postfix(GUI_MainButtons __instance)
        {
            try
            {
                if (__instance == null || __instance.uiObjects == null || __instance.uiObjects.Length <= 9) return;
                var dropdownPanel = __instance.uiObjects[9];
                if (dropdownPanel == null) return;

                if (_stockMarketButton == null || _stockMarketButton.transform.parent != dropdownPanel.transform)
                {
                    InjectDropdownButton(__instance, dropdownPanel);
                }
                else
                {
                    UpdateText();
                }
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError($"Failed to inject stock market button into Management menu: {ex.Message}");
            }
        }

        private static void InjectDropdownButton(GUI_MainButtons mainButtons, GameObject panel)
        {
            // Find a reference button in the dropdown
            Button refBtn = null;
            for (int i = 0; i < panel.transform.childCount; i++)
            {
                refBtn = panel.transform.GetChild(i).GetComponent<Button>();
                if (refBtn != null) break;
            }

            if (refBtn == null)
            {
                StockMarketPlugin.Log.LogWarning("No reference button found in Management dropdown");
                return;
            }

            _stockMarketButton = UnityEngine.Object.Instantiate(refBtn.gameObject, panel.transform);
            _stockMarketButton.name = "Button_StockMarket";
            _stockMarketButton.SetActive(true);

            // Strip any setText or tooltip that would override our custom text
            foreach (var st in _stockMarketButton.GetComponentsInChildren<setText>(true)) UnityEngine.Object.DestroyImmediate(st);
            foreach (var tt in _stockMarketButton.GetComponentsInChildren<tooltip>(true)) UnityEngine.Object.DestroyImmediate(tt);

            var btn = _stockMarketButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() =>
                {
                    mainButtons.CloseAllDropdowns();
                    var main = GameObject.Find("Main");
                    var ms = main != null ? main.GetComponent<mainScript>() : null;
                    var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                    StockMarketWindow.Show(ms, guiMain);
                });
            }

            UpdateText();

            // Handle layout
            var layout = panel.GetComponent<LayoutGroup>();
            if (layout == null)
            {
                // No LayoutGroup: compute manual positioning below the lowest sibling
                float lowestY = float.MaxValue;
                RectTransform refRt = refBtn.GetComponent<RectTransform>();
                float btnHeight = refRt.sizeDelta.y;

                for (int i = 0; i < panel.transform.childCount; i++)
                {
                    var child = panel.transform.GetChild(i) as RectTransform;
                    if (child != null && child != _stockMarketButton.transform && child.gameObject.activeSelf)
                    {
                        if (child.anchoredPosition.y < lowestY)
                        {
                            lowestY = child.anchoredPosition.y;
                        }
                    }
                }

                var myRt = _stockMarketButton.GetComponent<RectTransform>();
                if (lowestY < float.MaxValue)
                {
                    myRt.anchoredPosition = new Vector2(refRt.anchoredPosition.x, lowestY - btnHeight - 2f);
                }

                var panelRt = panel.GetComponent<RectTransform>();
                if (panelRt != null)
                {
                    panelRt.sizeDelta = new Vector2(panelRt.sizeDelta.x, panelRt.sizeDelta.y + btnHeight + 4f);
                }
            }

            StockMarketPlugin.Log.LogInfo("Stock Market button successfully integrated into Management dropdown!");
        }

        private static void UpdateText()
        {
            if (_stockMarketButton == null) return;
            var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
            var settings = ms != null ? ms.GetComponent<settingsScript>() : null;
            StockMarketLabels.Arabic = settings != null && settings.language == 13;

            var txt = _stockMarketButton.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = StockMarketLabels.DropdownButton;
                if (ms != null && ms.fonts != null && ms.fonts.Length > 0)
                {
                    txt.font = ms.fonts[0];
                }
            }
        }
    }

    /// <summary>
    /// Keyboard shortcut: F8 opens the stock market window, F7 captures timestamped in-game screenshot
    /// </summary>
    [HarmonyPatch(typeof(GUI_Main), "Update")]
    internal static class KeyboardShortcutPatch
    {
        static void Postfix(GUI_Main __instance)
        {
            // F7 takes in-game screenshot with timestamp
            if (Input.GetKeyDown(KeyCode.F7))
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = $"screenshot_mgt2_{timestamp}.png";
                    string path = System.IO.Path.Combine(Application.dataPath, "../" + filename);
                    ScreenCapture.CaptureScreenshot(path);
                    StockMarketPlugin.Log.LogInfo("Screenshot captured to " + path);
                }
                catch (Exception ex)
                {
                    StockMarketPlugin.Log.LogError("Failed to capture screenshot: " + ex.Message);
                }
            }

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

            // F4 runs the complete in-game Mod Test Suite
            if (Input.GetKeyDown(KeyCode.F4))
            {
                var main = GameObject.Find("Main");
                if (main != null)
                {
                    var ms = main.GetComponent<mainScript>();
                    if (ms != null && !ms.multiplayer)
                    {
                        RunModTestSuite(ms, __instance);
                    }
                }
            }
        }

        private static void RunModTestSuite(mainScript ms, GUI_Main guiMain)
        {
            try
            {
                // 1. Inject $25,000,000 test funds
                ms.Earn(25000000L, 12);

                // 2. Setup Post-IPO state if not already public
                if (!StockData.Data.playerIsPublic)
                {
                    StockData.Data.playerIsPublic = true;
                    PlayerIpoManager.State.totalShares = 1000000L;
                    PlayerIpoManager.State.currentSharePrice = 5.50;
                    PlayerIpoManager.State.marketValuation = 5500000L;
                    PlayerIpoManager.State.founderSharePercent = 80.0f;
                    PlayerIpoManager.State.boardDiscontent = 35f;
                    PlayerIpoManager.State.investorConfidence = 75f;
                    PlayerIpoManager.State.quarterlyRevenueTarget = Math.Max(500000L, (long)(PlayerIpoManager.State.marketValuation * 0.12));
                    PlayerIpoManager.State.currentQuarterRevenue = 180000L;
                    PlayerIpoManager.State.consecutiveTargetsMet = 1;
                    PlayerIpoManager.State.consecutiveTargetsMissed = 0;
                }

                // 3. Setup Hostile Takeover challenge (Nintendo owns 15%, public float has remaining 5%)
                // Strict invariant: Founder (80%) + Hostile (15%) + Public Float (5%) = 100%!
                PlayerIpoManager.State.founderSharePercent = 80.0f;
                PlayerIpoManager.State.hostileRivalCompanyId = 18; // Nintendo
                PlayerIpoManager.State.hostileRivalName = "Nintendo";
                PlayerIpoManager.State.hostileRivalSharePercent = 15.0f;
                StockData.Data.playerPublicShare = 0.05f; // Remaining float from market
                PlayerIpoManager.State.boardDiscontent = 40f;
                if (PlayerIpoManager.State.quarterlyRevenueTarget <= 0)
                {
                    PlayerIpoManager.State.quarterlyRevenueTarget = Math.Max(500000L, (long)(PlayerIpoManager.State.marketValuation * 0.12));
                }

                // 4. Broadcast simulated game release hit (92%) to test synergies
                Ali.Mgt2.Simulation.CrossSystemBridge.EnqueueEvent("GAME_RELEASED",
                    "Galaxy Odyssey|StarForge Studio|1|92|4000000|0");

                // 5. Broadcast corporate event
                Ali.Mgt2.Simulation.CrossSystemBridge.EnqueueEvent("CORPORATE_EVENT",
                    "@WallStreetGames: Record surge in trading activity for studio shares after consecutive commercial successes!");

                // 6. Display perfectly shaped, authentic Arabic test suite summary notice
                guiMain?.MessageBox(StockMarketLabels.TestSuiteNotice, false);
                StockMarketPlugin.Log.LogInfo("Mod Test Suite (F4) triggered successfully.");
            }
            catch (Exception ex)
            {
                StockMarketPlugin.Log.LogError("Mod Test Suite failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(Menu_Bank), "Init")]
    internal static class Patch_Menu_Bank_StockMarket
    {
        private const string ButtonName = "Button_Bank_StockMarket";

        private static void Postfix(Menu_Bank __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Kredit") || b.name.Contains("Abbrechen"));
                if (btn == null) return;
                if (btn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(btn.gameObject, btn.transform.parent);
                go.name = ButtonName;
                var rt = (RectTransform)go.transform;
                var src = (RectTransform)btn.transform;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, 48f);

                var b = go.GetComponent<Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() =>
                    {
                        var main = GameObject.Find("Main");
                        var ms = main != null ? main.GetComponent<mainScript>() : null;
                        var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                        StockMarketWindow.Show(ms, guiMain);
                    });
                }

                var clr = new Color(0.18f, 0.8f, 0.44f);
                foreach (var img in go.GetComponentsInChildren<Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                foreach (var txt in go.GetComponentsInChildren<Text>(true))
                    txt.text = ar ? StockMarketLabels.Title : "Stock Market & IPO";
            }
            catch (Exception e) { StockMarketPlugin.Log.LogError(e); }
        }
    }

}