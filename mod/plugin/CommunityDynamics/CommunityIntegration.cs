using System.Linq;
using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod.CommunityDynamics
{
    /// <summary>
    /// Injects "Community & Social Feed" into Statistics dropdown (GUI_MainButtons.uiObjects[11]),
    /// and listens for the F5 hotkey.
    /// </summary>
    [HarmonyPatch(typeof(GUI_MainButtons), nameof(GUI_MainButtons.BUTTON_Statistics))]
    internal static class CommunityMenuPatch
    {
        private static GameObject _communityButton;

        static void Postfix(GUI_MainButtons __instance)
        {
            try
            {
                if (__instance == null || __instance.uiObjects == null || __instance.uiObjects.Length <= 11) return;
                var dropdownPanel = __instance.uiObjects[11];
                if (dropdownPanel == null) return;

                if (_communityButton == null || _communityButton.transform.parent != dropdownPanel.transform)
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
                CommunityDynamicsPlugin.Log.LogError($"Failed to inject community button into Statistics menu: {ex.Message}");
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

            _communityButton = UnityEngine.Object.Instantiate(refBtn.gameObject, panel.transform);
            _communityButton.name = "Button_CommunityDynamics";
            _communityButton.SetActive(true);

            foreach (var st in _communityButton.GetComponentsInChildren<setText>(true)) UnityEngine.Object.DestroyImmediate(st);
            foreach (var tt in _communityButton.GetComponentsInChildren<tooltip>(true)) UnityEngine.Object.DestroyImmediate(tt);

            var btn = _communityButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() =>
                {
                    mainButtons.CloseAllDropdowns();
                    var main = GameObject.Find("Main");
                    var ms = main != null ? main.GetComponent<mainScript>() : null;
                    var guiMain = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                    CommunityWindow.Show(ms, guiMain);
                });
            }

            UpdateText();
        }

        private static void UpdateText()
        {
            if (_communityButton == null) return;
            var txt = _communityButton.GetComponentInChildren<Text>();
            if (txt != null)
            {
                var main = GameObject.Find("Main");
                var settings = main != null ? main.GetComponent<settingsScript>() : null;
                CommunityDynamicsLabels.Arabic = settings != null && settings.language == 13;
                txt.text = CommunityDynamicsLabels.DropdownBtn;
            }
        }
    }

    [HarmonyPatch(typeof(GUI_Main), "Update")]
    internal static class CommunityHotkeysPatch
    {
        static void Postfix(GUI_Main __instance)
        {
            var ms = GameObject.Find("Main")?.GetComponent<mainScript>();
            if (ms == null || ms.multiplayer) return;

            // F7 Hotkey to toggle Community Dynamics Window
            if (Input.GetKeyDown(KeyCode.F7))
            {
                CommunityWindow.Toggle(ms, __instance);
            }
        }
    }

    [HarmonyPatch(typeof(Menu_Marketing_Main), "Init")]
    internal static class Patch_Menu_Marketing_Community
    {
        private const string ButtonName = "Btn_Marketing_Community";

        private static void Postfix(Menu_Marketing_Main __instance)
        {
            try
            {
                if (__instance == null) return;
                var btn = __instance.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Game") || b.name.Contains("Abbrechen") || b.name.Contains("Konsole"));
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
                        CommunityWindow.Show(ms, guiMain);
                    });
                }

                var clr = new Color(0.2f, 0.6f, 0.9f);
                foreach (var img in go.GetComponentsInChildren<Image>(true))
                    img.color = clr;

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;
                foreach (var txt in go.GetComponentsInChildren<Text>(true))
                    txt.text = ar ? CommunityDynamicsLabels.Title : "Community & Social Hub";
            }
            catch (Exception e) { CommunityDynamicsPlugin.Log.LogError(e); }
        }
    }

}