// F2P update menu: a "Live campaign" section under the game's update content. The player picks a
// type (season / event / collab / relaunch) and, with the arrows, what exactly to release: a season
// themed on a researched topic, a calendar event, a crossover with a licensed brand. The cost joins
// the game's own update cost; the effect lands when the update task completes (LiveOps.cs).
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class UpdatePanel
    {
        internal static int Type;
        private static int index;
        private static readonly List<int> choices = new List<int>();
        private static Menu_Dev_F2PUpdate built;
        private static Image[] typeButtons;
        private static Text header, choiceText, infoText;
        private static Color offColor;
        private const float Grow = 150f;

        private static gameScript Game(Menu_Dev_F2PUpdate m) => Traverse.Create(m).Field("gS_").GetValue<gameScript>();
        private static mainScript Main(Menu_Dev_F2PUpdate m) => Traverse.Create(m).Field("mS_").GetValue<mainScript>();
        internal static int Choice => index < choices.Count ? choices[index] : -1;

        internal static long BaseCost(Menu_Dev_F2PUpdate m) => Traverse.Create(m).Method("GetDevCosts").GetValue<long>();

        internal static long ExtraCost(Menu_Dev_F2PUpdate m)
        {
            if (Type == ContentType.None || Choice < 0) return 0;
            var ms = Main(m);
            return Live.ContentCost(Game(m), ms, ms.GetComponent<licences>(), Type, Choice);
        }

        internal static void Build(Menu_Dev_F2PUpdate menu)
        {
            if (built == menu) return;
            var window = UiKit.Find(menu.transform, "WindowMain");
            var top = UiKit.Find(window, "TxtTopAdds");
            var all = UiKit.Find(window, "Button_AlleAdds");
            var costs = UiKit.Find(window, "BGDevCosts");
            var minus = UiKit.Find(window, "Button_M");
            var plus = UiKit.Find(window, "Button_P");
            var style = window.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.fontSize == 14 && t.alignment == TextAnchor.UpperCenter);
            if (top == null || all == null || costs == null || minus == null || plus == null || style == null)
            {
                LiveOpsPlugin.Log.LogWarning("F2P update layout not found; live campaign section not added");
                return;
            }
            built = menu;
            UiKit.GrowWindow(window, Grow);
            foreach (var name in new[] { "Button_Yes", "ToggleAuto", "NumAutoNum", "Button_P", "Button_M", "BGDevCosts" })
                UiKit.MoveDown(UiKit.Find(window, name), Grow);

            var h = UiKit.Clone(top, window, "TxtLive");
            h.anchoredPosition = new Vector2(top.anchoredPosition.x, -686f);
            header = h.GetComponentInChildren<Text>();

            typeButtons = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                int type = i;
                var b = UiKit.Clone(all, window, "Button_LiveType" + i);
                b.sizeDelta = new Vector2(108f, 26f);
                b.anchoredPosition = new Vector2(73f + 117f * i, -718f);
                b.GetComponent<Button>().onClick.AddListener(() => { Click(menu); Type = type; index = 0; Refresh(menu); });
                typeButtons[i] = b.GetComponent<Image>();
            }
            offColor = typeButtons[0].color;

            var box = UiKit.Clone(costs, window, "BGLiveChoice");
            box.anchorMin = box.anchorMax = new Vector2(0f, 1f);
            box.sizeDelta = new Vector2(480f, 30f);
            box.anchoredPosition = new Vector2(307f, -756f);
            choiceText = box.GetComponentInChildren<Text>();
            choiceText.color = style.color;
            choiceText.fontSize = 15;
            choiceText.alignment = TextAnchor.MiddleCenter;
            Arrow(menu, minus, window, 46f, -1);
            Arrow(menu, plus, window, 568f, +1);

            infoText = UiKit.CopyText(style, window, TextAnchor.UpperCenter);
            var it = infoText.rectTransform;
            it.anchorMin = it.anchorMax = new Vector2(0f, 1f);
            it.sizeDelta = new Vector2(570f, 48f);
            it.anchoredPosition = new Vector2(307f, -800f);
            infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static void Arrow(Menu_Dev_F2PUpdate menu, RectTransform source, Transform parent, float x, int step)
        {
            var a = UiKit.Clone(source, parent, "Button_Live" + (step < 0 ? "Prev" : "Next"));
            Object.DestroyImmediate(a.GetComponent<tooltip>());
            a.anchoredPosition = new Vector2(x, -756f);
            a.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (choices.Count == 0) return;
                Click(menu);
                index = (index + step + choices.Count) % choices.Count;
                Refresh(menu);
            });
        }

        private static void Click(Menu_Dev_F2PUpdate m) => Traverse.Create(m).Field("sfx_").GetValue<sfxScript>()?.PlaySound(3, force: true);

        private static void FillChoices(gameScript g, mainScript ms)
        {
            choices.Clear();
            var th = ms.GetComponent<themes>();
            var lic = ms.GetComponent<licences>();
            switch (Type)
            {
                case ContentType.Season: // researched topics, the ones that suit the genre first
                    choices.AddRange(Enumerable.Range(0, th.themes_RES_POINTS_LEFT.Length).Where(th.IsErforscht)
                        .OrderByDescending(i => th.IsThemesFitWithGenre(i, g.maingenre)));
                    break;
                case ContentType.Event: // whatever is on the calendar right now first
                    choices.AddRange(Enumerable.Range(0, Live.OccasionCount).OrderByDescending(i => Live.InSeason(i, ms, g)));
                    break;
                case ContentType.Collab: // brands that exist by now: best fit, then most famous
                    choices.AddRange(Enumerable.Range(0, lic.licence_QUALITY.Length).Where(i => lic.licence_YEAR[i] <= ms.year)
                        .OrderByDescending(i => Live.GenreFit(lic, g, i)).ThenByDescending(i => lic.licence_QUALITY[i]).Take(40));
                    break;
                case ContentType.Relaunch:
                    if (Live.CanRelaunch(g)) choices.Add(0);
                    break;
            }
            if (index >= choices.Count) index = 0;
        }

        internal static void Refresh(Menu_Dev_F2PUpdate menu)
        {
            if (built != menu) return;
            var g = Game(menu);
            var ms = Main(menu);
            if (g == null || ms == null) return;
            Txt.Refresh(ms);
            var gui = Traverse.Create(menu).Field("guiMain_").GetValue<GUI_Main>();
            var th = ms.GetComponent<themes>();
            var lic = ms.GetComponent<licences>();
            var ts = ms.GetComponent<textScript>();
            FillChoices(g, ms);

            UiKit.SetText(header, LiveOpsLabels.Campaign);
            string[] names = { LiveOpsLabels.TypeNone, LiveOpsLabels.TypeSeason, LiveOpsLabels.TypeEvent, LiveOpsLabels.TypeCollab, LiveOpsLabels.TypeRelaunch };
            for (int i = 0; i < typeButtons.Length; i++)
            {
                UiKit.SetText(typeButtons[i], names[i]);
                typeButtons[i].color = i == Type ? gui.colors[7] : offColor; // same highlight as the game's selected content
                var rt = typeButtons[i].rectTransform;
                rt.anchoredPosition = new Vector2(73f + 117f * (LiveOpsLabels.Arabic ? 4 - i : i), rt.anchoredPosition.y);
            }

            int c = Choice;
            string choice = LiveOpsLabels.NoneShort, info;
            switch (Type)
            {
                case ContentType.Season:
                    info = LiveOpsLabels.InfoSeason;
                    if (c >= 0) choice = Txt.Join(ts.GetThemes(c), "-", th.IsThemesFitWithGenre(c, g.maingenre) ? LiveOpsLabels.Fits : LiveOpsLabels.Misfit);
                    break;
                case ContentType.Event:
                    info = LiveOpsLabels.InfoEvent;
                    if (c >= 0) choice = Txt.Join(Txt.Occasion(c), "-", Live.InSeason(c, ms, g) ? LiveOpsLabels.InSeason : LiveOpsLabels.OffSeason);
                    break;
                case ContentType.Collab:
                    info = LiveOpsLabels.InfoCollab;
                    if (c >= 0)
                    {
                        float fit = Live.GenreFit(lic, g, c);
                        choice = Txt.Join(lic.GetName(c), "-", Txt.LicenceType(lic.licence_TYP[c]), "-", Mathf.RoundToInt(lic.licence_QUALITY[c]) + "%",
                            fit > 1f ? "- " + LiveOpsLabels.Fits : fit < 1f ? "- " + LiveOpsLabels.Misfit : "");
                    }
                    break;
                case ContentType.Relaunch:
                    info = LiveOpsLabels.InfoRelaunch;
                    choice = c >= 0 ? "2.0" : LiveOpsLabels.RelaunchUsed;
                    break;
                default:
                    info = LiveOpsLabels.InfoNone;
                    break;
            }
            if (choices.Count > 1) choice = Txt.Join(choice, $"({index + 1}/{choices.Count})");
            choiceText.text = choice;
            long extra = ExtraCost(menu);
            infoText.text = extra > 0 ? info + "\n" + Txt.Join(LiveOpsLabels.Extra, ":", "<color=#76110A>" + ms.GetMoney(extra, showDollar: true) + "</color>") : info;
            Traverse.Create(menu).Method("UpdateGUI").GetValue(); // total cost now includes the campaign
        }

        /// <summary>The game's BUTTON_Start, plus the campaign's cost, points and pending effect.</summary>
        internal static bool Start(Menu_Dev_F2PUpdate menu)
        {
            var t = Traverse.Create(menu);
            var g = Game(menu);
            var ms = Main(menu);
            var room = t.Field("rS_").GetValue<roomScript>();
            var gui = t.Field("guiMain_").GetValue<GUI_Main>();
            if (g == null || room == null || ms == null || gui == null) return false;
            long extra = ExtraCost(menu);
            long cost = BaseCost(menu) + extra;
            Click(menu);
            ms.Pay(cost, 15);
            var task = gui.AddTask_F2PUpdate();
            task.Init(fromSavegame: false);
            task.targetID = g.myID;
            task.devCosts = (int)cost;
            task.automatic = menu.uiObjects[4].GetComponent<Toggle>().isOn;
            int amount = t.Field("amount").GetValue<int>();
            if (amount > 0) task.autoAmount = amount + 1;
            int devPoints = g.GetGesamtDevPoints();
            task.points = Mathf.RoundToInt(devPoints * 0.1f);
            var adds = t.Field("buttonAdds").GetValue<bool[]>();
            for (int j = 0; j < adds.Length; j++)
            {
                if (!adds[j]) continue;
                task.quality += menu.interestBoost[j];
                task.points += Mathf.RoundToInt(devPoints * menu.interestBoost[j] * 0.02f);
            }
            task.points += Mathf.RoundToInt(devPoints * PointsShare(Type));
            task.pointsLeft = task.points;
            var roomObject = GameObject.Find("Room_" + room.myID);
            if (roomObject != null) roomObject.GetComponent<roomScript>().taskID = task.myID;
            Live.Data.pendings.Add(new Pending { taskId = task.myID, type = Type, choice = Choice, cost = extra });
            Type = ContentType.None;
            gui.uiObjects[299].SetActive(false);
            gui.CloseMenu();
            menu.gameObject.SetActive(false);
            return true;
        }

        /// <summary>Development points the content adds to the update task (share of the game's own points).</summary>
        internal static float PointsShare(int type)
        {
            switch (type)
            {
                case ContentType.Season: return 0.2f;
                case ContentType.Event: return 0.08f;
                case ContentType.Collab: return 0.12f;
                case ContentType.Relaunch: return 0.6f;
                default: return 0f;
            }
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_F2PUpdate), nameof(Menu_Dev_F2PUpdate.Init))]
    internal static class UpdateInitPatch
    {
        private static void Prefix() => UpdatePanel.Type = ContentType.None;

        private static void Postfix(Menu_Dev_F2PUpdate __instance)
        {
            UpdatePanel.Build(__instance);
            UpdatePanel.Refresh(__instance);
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_F2PUpdate), "UpdateGUI")]
    internal static class UpdateCostPatch
    {
        // GetDevCosts itself is not patched: Harmony mangles 64-bit return values on this 32-bit game
        private static void Postfix(Menu_Dev_F2PUpdate __instance)
        {
            long extra = UpdatePanel.ExtraCost(__instance);
            if (extra <= 0) return;
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>();
            __instance.uiObjects[3].GetComponent<Text>().text = ms.GetMoney(UpdatePanel.BaseCost(__instance) + extra, showDollar: true);
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_F2PUpdate), nameof(Menu_Dev_F2PUpdate.BUTTON_Start))]
    internal static class UpdateStartPatch
    {
        // with a campaign chosen the update is started here (the game's start refuses an update without its own items)
        private static bool Prefix(Menu_Dev_F2PUpdate __instance) =>
            UpdatePanel.Type == ContentType.None || UpdatePanel.Choice < 0 || !UpdatePanel.Start(__instance);
    }
}
