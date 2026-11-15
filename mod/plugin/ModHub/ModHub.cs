using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.modhub", "Advanced Mod Systems Hub", "1.0.0")]
    public class ModHubPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.modhub").PatchAll(typeof(ModHubPlugin).Assembly);
            Log.LogInfo("Mod Hub loaded - Integrates Mod Systems into Game HUD & Menus");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                ModHubWindow.Toggle();
            }
        }
    }

    [HarmonyPatch(typeof(GUI_MainButtons), "Start")]
    internal static class Patch_GUI_MainButtons_ModHub
    {
        private const string ButtonName = "BUTTON_ModHub_Main";

        private static void Postfix(GUI_MainButtons __instance)
        {
            try
            {
                if (__instance == null) return;
                var statsBtn = __instance.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name.Contains("Statistics") || b.name.Contains("Publishing") || b.name.Contains("Management"));
                if (statsBtn == null) return;
                if (statsBtn.transform.parent.Find(ButtonName) != null) return;

                var go = UnityEngine.Object.Instantiate(statsBtn.gameObject, statsBtn.transform.parent);
                go.name = ButtonName;
                go.SetActive(true);

                // Strip native tooltips/setText that would overwrite our text
                foreach (var st in go.GetComponentsInChildren<setText>(true)) UnityEngine.Object.DestroyImmediate(st);
                foreach (var tt in go.GetComponentsInChildren<tooltip>(true)) UnityEngine.Object.DestroyImmediate(tt);

                var rt = (RectTransform)go.transform;
                var srcRt = (RectTransform)statsBtn.transform;
                rt.anchoredPosition = srcRt.anchoredPosition + new Vector2(srcRt.rect.width + 6f, 0f);

                var b = go.GetComponent<Button>();
                if (b != null)
                {
                    for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() =>
                    {
                        try { UnityEngine.Object.FindObjectOfType<sfxScript>()?.PlaySound(3, true); } catch { }
                        ModHubWindow.Toggle();
                    });
                }

                // Golden star theme
                var gold = new Color(1f, 0.8f, 0.2f);
                foreach (var img in go.GetComponentsInChildren<Image>(true))
                {
                    img.color = gold;
                }

                var settings = UnityEngine.Object.FindObjectOfType<settingsScript>();
                bool ar = settings != null && settings.language == 13;

                // "⭐ الأنظمة المتطورة"
                string labelAr = "\ufe94\ufee3\ufeaa\ufed8\ufe98\ufee4\ufedf\ufe8d \ufe94\ufee4\ufecc\ufee7\ufef7\ufe8d \u2b50";
                foreach (var txt in go.GetComponentsInChildren<Text>(true))
                {
                    txt.text = ar ? labelAr : "⭐ Mod Hub";
                    txt.color = Color.black;
                    txt.fontStyle = FontStyle.Bold;
                }

                ModHubPlugin.Log.LogInfo("Mod Hub button successfully injected into main HUD bottom bar!");
            }
            catch (Exception e)
            {
                ModHubPlugin.Log.LogError("Failed to inject Mod Hub button into main HUD: " + e);
            }
        }
    }
}
