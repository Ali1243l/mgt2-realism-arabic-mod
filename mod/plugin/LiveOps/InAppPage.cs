// Development wizard, in-app purchases page: a "Seasonal Battle Pass" toggle beside the game's
// "enable/disable all" button, and a small line under the unpopularity bar naming the business
// model the chosen items add up to. The pass is decided here, before development, and locked after.
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class InAppPage
    {
        internal static bool DevPass; // the wizard's current choice
        private static RectTransform passButton;
        private static Text modelText;
        private static Color offColor;

        // items in the game's order: 0 cosmetics, 1 pets, 2 booster packs, 3 mighty items, 4 special characters, 5 instances
        internal static string Model(bool[] items, bool pass)
        {
            string m;
            if (items == null || !items.Any(x => x)) m = LiveOpsLabels.ModelNone;
            else if (items[3] || items[4]) m = LiveOpsLabels.ModelP2W;
            else if (items[2] || items[5]) m = LiveOpsLabels.ModelMixed;
            else m = LiveOpsLabels.ModelFair;
            return pass ? Txt.Join(m, LiveOpsLabels.ModelPlusPass) : m;
        }

        internal static void Build(Menu_DevGame menu)
        {
            if (passButton != null) return;
            var bg = UiKit.Find(menu.transform, "BG_InAppPurchase");
            var all = bg != null ? UiKit.Find(bg, "Button_Alles") : null;
            var hateTitle = bg != null ? UiKit.Find(bg, "TextUnbeliebtheit") : null;
            if (all == null || hateTitle == null)
            {
                LiveOpsPlugin.Log.LogWarning("in-app page layout not found; Battle Pass card not added");
                return;
            }
            // two buttons share the row the "all" button had to itself
            all.anchoredPosition = new Vector2(149.5f, all.anchoredPosition.y);
            all.sizeDelta = new Vector2(260f, all.sizeDelta.y);
            passButton = UiKit.Clone(all, all.parent, "Button_BattlePass");
            passButton.SetSiblingIndex(all.GetSiblingIndex() + 1); // stays under the lock overlays
            passButton.anchoredPosition = new Vector2(429.5f, all.anchoredPosition.y);
            offColor = passButton.GetComponent<Image>().color;
            passButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                DevPass = !DevPass;
                Traverse.Create(menu).Field("sfx_").GetValue<sfxScript>()?.PlaySound(3, force: true);
                Traverse.Create(menu).Method("SetMaxVerdienstInApp").GetValue();
            });
            var tip = passButton.gameObject.AddComponent<tooltip>();
            tip.textID = -1;

            var model = UiKit.Clone(hateTitle, hateTitle.parent, "TextModel");
            model.SetSiblingIndex(hateTitle.GetSiblingIndex() + 1);
            model.anchoredPosition = new Vector2(hateTitle.anchoredPosition.x, -64f);
            model.sizeDelta = new Vector2(hateTitle.sizeDelta.x, 18f);
            modelText = model.GetComponent<Text>();
            modelText.fontSize = 12;
        }

        /// <summary>After the game redraws the page: add the pass to the totals and refresh our texts.</summary>
        internal static void Refresh(Menu_DevGame menu, ref float total)
        {
            if (passButton == null) return;
            var t = Traverse.Create(menu);
            var ms = t.Field("mS_").GetValue<mainScript>();
            var gui = t.Field("guiMain_").GetValue<GUI_Main>();
            Txt.Refresh(ms);
            UiKit.SetText(passButton, LiveOpsLabels.BattlePass);
            UiKit.SetTooltip(passButton, LiveOpsLabels.BattlePassTip);
            passButton.GetComponent<Image>().color = DevPass ? gui.colors[20] : offColor;
            modelText.text = Txt.Join(LiveOpsLabels.ModelLabel, Model(menu.g_InAppPurchase, DevPass));
            if (!DevPass) return;
            total += Live.PassPrice;
            menu.uiObjects[192].GetComponent<Text>().text = "$" + ms.Round(total, 2);
            var fill = menu.uiObjects[199].GetComponent<Image>();
            fill.fillAmount = Mathf.Min(1f, fill.fillAmount + Live.PassHate(new LiveGame { battlePass = true, season = 1, weeksSinceSeason = 0 }) * 0.01f); // the game: fill = hate %
            fill.color = t.Method("GetValColor", 100f - fill.fillAmount * 100f).GetValue<Color>();
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), nameof(Menu_DevGame.Init))]
    internal static class DevInitPatch
    {
        private static void Postfix(Menu_DevGame __instance)
        {
            InAppPage.DevPass = false;
            InAppPage.Build(__instance);
            Traverse.Create(__instance).Method("SetMaxVerdienstInApp").GetValue(); // Init drew the page before the card existed
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "SetMaxVerdienstInApp")]
    internal static class DevInAppPatch
    {
        private static void Postfix(Menu_DevGame __instance, ref float __result) => InAppPage.Refresh(__instance, ref __result);
    }

    [HarmonyPatch(typeof(Menu_DevGame), nameof(Menu_DevGame.BUTTON_Start))]
    internal static class DevStartPatch
    {
        private static void Prefix(out int __state) => __state = NewestOwnGameId();

        private static void Postfix(Menu_DevGame __instance, int __state)
        {
            if (!InAppPage.DevPass || __instance.g_GameTyp != 2) return;
            int id = NewestOwnGameId();
            if (id <= __state) return; // development did not start (missing money, settings...)
            var ms = Object.FindObjectOfType<mainScript>();
            var g = ms.games_.arrayGamesScripts.FirstOrDefault(x => x != null && x.myID == id);
            if (g != null) Live.Get(g).battlePass = true;
            InAppPage.DevPass = false;
        }

        private static int NewestOwnGameId()
        {
            var ms = Object.FindObjectOfType<mainScript>();
            var games = ms?.games_?.arrayGamesScripts;
            return games == null ? -1 : games.Where(g => g != null && g.IsMyGame()).Select(g => g.myID).DefaultIfEmpty(-1).Max();
        }
    }
}
