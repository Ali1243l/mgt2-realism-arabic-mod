using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.PublishingDeals
{
    /// <summary>
    /// Injects "Publishing & Exclusives Deals" into the Publishing dropdown menu (GUI_MainButtons.uiObjects[13]).
    /// </summary>
    [HarmonyPatch(typeof(GUI_MainButtons), nameof(GUI_MainButtons.BUTTON_Publishing))]
    internal static class PublishingMenuPatch
    {
        private static GameObject _pubDealsButton;

        static void Postfix(GUI_MainButtons __instance)
        {
            try
            {
                if (__instance == null || __instance.uiObjects == null || __instance.uiObjects.Length <= 13) return;
                var dropdownPanel = __instance.uiObjects[13];
                if (dropdownPanel == null) return;

                if (_pubDealsButton == null || _pubDealsButton.transform.parent != dropdownPanel.transform)
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
                PublishingDealsPlugin.Log.LogError($"Failed to inject button into Publishing menu: {ex.Message}");
            }
        }

        private static void InjectDropdownButton(GUI_MainButtons mainButtons, GameObject panel)
        {
            Button refBtn = null;
            for (int i = 0; i < panel.transform.childCount; i++)
            {
                refBtn = panel.transform.GetChild(i).GetComponent<Button>();
                if (refBtn != null) break;
            }

            if (refBtn == null) return;

            _pubDealsButton = UnityEngine.Object.Instantiate(refBtn.gameObject, panel.transform);
            _pubDealsButton.name = "Button_PublishingDeals";
            _pubDealsButton.SetActive(true);

            foreach (var st in _pubDealsButton.GetComponentsInChildren<setText>(true)) UnityEngine.Object.DestroyImmediate(st);
            foreach (var tt in _pubDealsButton.GetComponentsInChildren<tooltip>(true)) UnityEngine.Object.DestroyImmediate(tt);

            // Destroy all non-Text child objects (such as padlock sprites / Schloss icons inherited from locked buttons)
            for (int i = _pubDealsButton.transform.childCount - 1; i >= 0; i--)
            {
                var child = _pubDealsButton.transform.GetChild(i);
                if (child.GetComponent<Text>() == null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            var btn = _pubDealsButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() =>
                {
                    mainButtons.CloseAllDropdowns();
                    var main = GameObject.Find("Main");
                    var ms = main != null ? main.GetComponent<mainScript>() : null;
                    var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                    PublishingDealsWindow.Show(ms, guiMain);
                });
            }

            UpdateText();

            // Handle manual positioning if no LayoutGroup
            var layout = panel.GetComponent<LayoutGroup>();
            if (layout == null)
            {
                float lowestY = float.MaxValue;
                RectTransform refRt = refBtn.GetComponent<RectTransform>();
                float btnHeight = refRt.sizeDelta.y;

                for (int i = 0; i < panel.transform.childCount; i++)
                {
                    var child = panel.transform.GetChild(i) as RectTransform;
                    if (child != null && child != _pubDealsButton.transform && child.gameObject.activeSelf)
                    {
                        if (child.anchoredPosition.y < lowestY)
                            lowestY = child.anchoredPosition.y;
                    }
                }

                var myRt = _pubDealsButton.GetComponent<RectTransform>();
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

            PublishingDealsPlugin.Log.LogInfo("Publishing Deals button integrated into Publishing menu!");
        }

        private static void UpdateText()
        {
            if (_pubDealsButton == null) return;
            var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
            var settings = ms != null ? ms.GetComponent<settingsScript>() : null;
            PublishingDealsLabels.Arabic = settings != null && settings.language == 13;

            var txt = _pubDealsButton.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = PublishingDealsLabels.DropdownBtn;
                if (ms != null && ms.fonts != null && ms.fonts.Length > 0)
                {
                    txt.font = ms.fonts[0];
                }
            }
        }
    }

    /// <summary>
    /// F6 shortcut opens the Publishing Deals window.
    /// </summary>
    [HarmonyPatch(typeof(GUI_Main), "Update")]
    internal static class PublishingKeyboardShortcutPatch
    {
        static void Postfix(GUI_Main __instance)
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                var main = GameObject.Find("Main");
                if (main != null)
                {
                    var ms = main.GetComponent<mainScript>();
                    if (ms != null && !ms.multiplayer)
                    {
                        PublishingDealsWindow.Show(ms, __instance);
                    }
                }
            }
        }
    }
}
