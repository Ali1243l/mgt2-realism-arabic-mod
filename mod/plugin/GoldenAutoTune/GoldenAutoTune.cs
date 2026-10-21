// GDD point 34: "Golden Auto-Tune". Adds a gold button next to the game's own auto-design button
// in the new-game menu. One click sets all 8 focus sliders, the 3 direction sliders and the
// 4 work priorities to their optimal values for the chosen main + sub genre.
//
// The values come from the game itself (genres.GetFocus / GetAlign, the same functions its
// game reports use), so they are exact for every genre combination, including modded genres.
// The game's own auto button only fills values already discovered through game reports;
// this one fills all of them.
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.goldenautotune", "Golden Auto-Tune", "1.0.0")]
    public class GoldenAutoTunePlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.goldenautotune").PatchAll(typeof(GoldenAutoTunePlugin).Assembly);
            Log.LogInfo("Golden Auto-Tune loaded");
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "OnEnable")]
    internal static class AddGoldenButton
    {
        private const string ButtonName = "GoldenAutoTune";
        // "الضبط الذهبي" in the shaped, reversed form the game's Arabic texts use
        private const string LabelAr = "ﻲﺒﻫﺬﻟﺍ ﻂﺒﻀﻟﺍ";
        private const int Arabic = 13; // settingsScript.language index of Arabic

        private static void Postfix(Menu_DevGame __instance)
        {
            var source = __instance.GetComponentsInChildren<Button>(true).FirstOrDefault(b => CallsAutoDesign(b.onClick));
            if (source == null)
            {
                GoldenAutoTunePlugin.Log.LogWarning("auto-design button not found; golden button not added");
                return;
            }
            if (source.transform.parent.Find(ButtonName) != null)
                return; // already added (the menu is reused)

            var go = Object.Instantiate(source.gameObject, source.transform.parent);
            go.name = ButtonName;
            var rt = (RectTransform)go.transform;
            var src = (RectTransform)source.transform;
            rt.anchoredPosition = src.anchoredPosition + new Vector2(-(src.rect.width + 6f), 0f);

            var button = go.GetComponent<Button>();
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off); // drop the copied game call
            var menu = __instance;
            button.onClick.AddListener(() => Apply(menu));

            var gold = new Color(1f, 0.78f, 0.15f);
            foreach (var img in go.GetComponentsInChildren<Image>(true))
                img.color = gold;
            var settings = Object.FindObjectOfType<settingsScript>();
            bool ar = settings != null && settings.language == Arabic;
            foreach (var text in go.GetComponentsInChildren<Text>(true))
                text.text = ar ? LabelAr : "Golden Auto-Tune";
            GoldenAutoTunePlugin.Log.LogInfo("golden button added next to " + source.name);
        }

        private static bool CallsAutoDesign(UnityEvent e)
        {
            for (int i = 0; i < e.GetPersistentEventCount(); i++)
                if (e.GetPersistentMethodName(i) == "BUTTON_AutoDesignSettings")
                    return true;
            return false;
        }

        internal static void Apply(Menu_DevGame menu)
        {
            var t = Traverse.Create(menu);
            t.Field("sfx_").GetValue<sfxScript>()?.PlaySound(3, force: true);
            int main = menu.g_GameMainGenre, sub = menu.g_GameSubGenre;
            if (main < 0)
                return; // no genre chosen yet: nothing to tune
            var genres = t.Field("genres_").GetValue<genres>();

            // 8 focus sliders (the game normalises them to the required total of 40) + 3 directions
            for (int i = 0; i < menu.g_Designschwerpunkt.Length; i++)
                menu.g_Designschwerpunkt[i] = genres.GetFocus(i, main, sub);
            for (int i = 0; i < menu.g_Designausrichtung.Length; i++)
                menu.g_Designausrichtung[i] = genres.GetAlign(i, main, sub);

            // 4 work priorities: they must add up to 100% (slider total 20, 0-10 each). The rating
            // weighs the categories by the main genre's percentages, so split 20 in that ratio.
            float[] weight =
            {
                genres.genres_GAMEPLAY[main], genres.genres_GRAPHIC[main],
                genres.genres_SOUND[main], genres.genres_CONTROL[main],
            };
            int[] ap = Split(weight, 20, 10);
            menu.g_GameAP_Gameplay = ap[0];
            menu.g_GameAP_Grafik = ap[1];
            menu.g_GameAP_Sound = ap[2];
            menu.g_GameAP_Technik = ap[3];
            for (int i = 0; i < 4; i++)
                menu.uiObjects[97 + i].GetComponent<Slider>().value = ap[i];
            menu.SetAP_Gameplay();
            menu.SetAP_Grafik();
            menu.SetAP_Sound();
            menu.SetAP_Technik();

            menu.UpdateDesignSlider();
            menu.UpdateDesignSettings();
            t.Method("SetMGSR").GetValue(); // direction slider 1 drives the age rating
        }

        /// <summary>Largest-remainder split of <paramref name="total"/> by weight, each part capped.</summary>
        internal static int[] Split(float[] weight, int total, int cap)
        {
            float sum = weight.Sum();
            var exact = weight.Select(w => sum > 0 ? w / sum * total : (float)total / weight.Length).ToArray();
            var parts = exact.Select(x => Mathf.Min(cap, Mathf.FloorToInt(x))).ToArray();
            while (parts.Sum() < total)
            {
                int best = -1;
                for (int i = 0; i < parts.Length; i++)
                    if (parts[i] < cap && (best < 0 || exact[i] - parts[i] > exact[best] - parts[best]))
                        best = i;
                if (best < 0)
                    break;
                parts[best]++;
            }
            return parts;
        }
    }
}
