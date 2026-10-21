// The game's per-game sales window gets a "Live service" section for the player's MMO/F2P games:
// a scrollable list in the style of the window's own columns, ending with a 26-week player chart.
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class StatsPanel
    {
        private const float Grow = 200f;
        private const int Bars = 26;
        private static Menu_Game_Umsatz built;
        private static bool grown;
        private static RectTransform window, header, scroll;
        private static readonly List<Text[]> rows = new List<Text[]>();
        private static Image[] bars;

        private static void Build(Menu_Game_Umsatz menu)
        {
            if (built == menu) return;
            window = UiKit.Find(menu.transform, "WindowMain");
            var title = window != null ? UiKit.Find(window, "TextGewinn") : null;
            if (title == null)
            {
                LiveOpsPlugin.Log.LogWarning("sales window layout not found; live section not added");
                return;
            }
            built = menu;
            grown = false;
            rows.Clear();
            var labelStyle = menu.uiObjects[3].GetComponent<Text>();
            var valueStyle = menu.uiObjects[4].GetComponent<Text>();

            header = UiKit.Clone(title, window, "TextLive");
            header.anchoredPosition = new Vector2(title.anchoredPosition.x, -452f);
            header.sizeDelta = new Vector2(title.sizeDelta.x, 24f);
            header.GetComponent<Text>().fontSize = 16;

            scroll = UiKit.ScrollArea(window, new Vector2(74f, -466f), new Vector2(466f, 160f), out var content);
            for (int i = 0; i < 12; i++) rows.Add(UiKit.Row(content, labelStyle, valueStyle));
            rows.Add(UiKit.Row(content, labelStyle, valueStyle)); // chart title

            var chart = new GameObject("Chart", typeof(RectTransform), typeof(LayoutElement));
            chart.transform.SetParent(content, false);
            chart.GetComponent<LayoutElement>().preferredHeight = 60f;
            bars = new Image[Bars];
            for (int i = 0; i < Bars; i++)
            {
                var b = new GameObject("Bar", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)b.transform;
                rt.SetParent(chart.transform, false);
                rt.anchorMin = new Vector2((i + 0.15f) / Bars, 0f);
                rt.anchorMax = new Vector2((i + 0.85f) / Bars, 0f);
                rt.sizeDelta = Vector2.zero;
                bars[i] = b.GetComponent<Image>();
                bars[i].color = new Color(0.165f, 0.555f, 1f); // the game's interest-bar blue
            }
        }

        private static void SetGrown(Menu_Game_Umsatz menu, bool on)
        {
            header.gameObject.SetActive(on);
            scroll.gameObject.SetActive(on);
            if (on == grown) return;
            grown = on;
            float dy = on ? Grow : -Grow;
            UiKit.GrowWindow(window, dy);
            UiKit.MoveDown(UiKit.Find(window, "Button_Yes"), dy);
            UiKit.MoveDown((RectTransform)menu.uiObjects[6].transform, dy); // exclusive/system-seller note
        }

        internal static void Show(Menu_Game_Umsatz menu, gameScript g)
        {
            Build(menu);
            if (built != menu) return;
            var ms = g != null ? g.mS_ : null;
            bool live = ms != null && g.IsMyGame() && (g.gameTyp == 1 || g.gameTyp == 2) && (Live.IsLive(g, ms) || Live.Find(g.myID) != null);
            SetGrown(menu, live);
            if (!live) return;
            Txt.Refresh(ms);
            Fill(g, ms);
            var sr = scroll.GetComponent<ScrollRect>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content); // size the rows first, then jump to the top
            sr.verticalNormalizedPosition = 1f;
        }

        private static string Weeks(int n, string unit) => Txt.Join(n.ToString(), unit);

        private static void Fill(gameScript g, mainScript ms)
        {
            var s = Live.Get(g);
            var lic = ms.GetComponent<licences>();
            var ts = ms.GetComponent<textScript>();
            float fresh = Live.Freshness(s), mood = s.sentiment / 100f;
            long players = g.abonnements;
            float d30 = 4f + 14f * mood + (s.season > 0 && s.weeksSinceSeason < Live.FreshWeeks ? 3f : 0f);
            string none = LiveOpsLabels.NoneShort;
            string moodColor = s.sentiment >= 60f ? "#1E8C3A" : s.sentiment >= 40f ? "#C77700" : "#A40000";
            float passWeek = s.battlePass
                ? g.inAppPurchaseWeek * Live.PassPrice * 0.25f * (0.35f + 0.65f * fresh) * Live.RevenueMultiplier(s) : 0f;

            header.GetComponent<Text>().text = LiveOpsLabels.Section;
            Set(0, LiveOpsLabels.Monetization, InAppPage.Model(g.inAppPurchase, s.battlePass));
            Set(1, LiveOpsLabels.Pass, !s.battlePass ? LiveOpsLabels.PassNone
                : Txt.Join(fresh >= 0.35f ? LiveOpsLabels.PassFresh : "<color=#A40000>" + LiveOpsLabels.PassStale + "</color>", Mathf.RoundToInt(fresh * 100f) + "%"));
            Set(2, LiveOpsLabels.Season, s.season == 0 ? none
                : Txt.Join("#" + s.season, "-", s.seasonTheme >= 0 ? ts.GetThemes(s.seasonTheme) : "", "-", Weeks(s.weeksSinceSeason, LiveOpsLabels.WeeksAgo)));
            Set(3, LiveOpsLabels.Event, s.eventWeeks <= 0 || s.eventOcc < 0 ? none
                : Txt.Join(Txt.Occasion(s.eventOcc), "-", Weeks(s.eventWeeks, LiveOpsLabels.WeeksLeft)));
            Set(4, LiveOpsLabels.Collab, s.collabWeeks <= 0 || s.collab < 0 ? none
                : Txt.Join(lic.GetName(s.collab), "-", Weeks(s.collabWeeks, LiveOpsLabels.WeeksLeft)));
            Set(5, LiveOpsLabels.Sentiment, $"<color={moodColor}>{Mathf.RoundToInt(s.sentiment)}%</color>");
            Set(6, LiveOpsLabels.Players, ms.GetMoney(players, showDollar: false));
            Set(7, LiveOpsLabels.Dau, ms.GetMoney((long)(players * (0.12f + 0.18f * mood)), showDollar: false));
            Set(8, LiveOpsLabels.Mau, ms.GetMoney((long)(players * (0.45f + 0.35f * mood)), showDollar: false));
            // the label lists 30 / 60 / 90; Arabic reads both right to left, so the values follow the same order
            Set(9, LiveOpsLabels.Retention, string.Join(" / ", Txt.Order(new[] { Mathf.RoundToInt(d30) + "%", Mathf.RoundToInt(d30 * 0.65f) + "%", Mathf.RoundToInt(d30 * 0.45f) + "%" })));
            Set(10, LiveOpsLabels.PassRevenue, s.battlePass ? ms.GetMoney((long)passWeek, showDollar: true) : none);
            Set(11, LiveOpsLabels.Relaunched, s.relaunched ? "2.0" : none);
            Set(12, LiveOpsLabels.History, "");

            var h = s.history;
            long max = 1;
            foreach (var v in h) if (v > max) max = v;
            for (int i = 0; i < Bars; i++)
            {
                int k = h.Count - Bars + i; // right-most bar is this week
                float f = k >= 0 ? h[k] / (float)max : 0f;
                bars[i].rectTransform.anchorMax = new Vector2(bars[i].rectTransform.anchorMax.x, Mathf.Max(0.02f, f));
            }
        }

        private static void Set(int row, string label, string value)
        {
            rows[row][0].text = label;
            rows[row][1].text = value;
        }
    }

    [HarmonyPatch(typeof(Menu_Game_Umsatz), nameof(Menu_Game_Umsatz.Init))]
    internal static class SalesWindowPatch
    {
        private static void Postfix(Menu_Game_Umsatz __instance, gameScript game_) => StatsPanel.Show(__instance, game_);
    }
}
