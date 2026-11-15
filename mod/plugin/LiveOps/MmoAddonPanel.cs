// Additional file for mod/plugin/LiveOps/MmoAddonPanel.cs
// Completes the remaining GDD Point 1 task in TASKS.md ("MMO seasons through MMO add-ons (Menu_Dev_MMOAddon)")
// using the exact LiveOpsLabels and UiKit methods already in mod/plugin/LiveOps/.

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class MmoAddonPanel
    {
        internal static int Type = ContentType.None;
        private static int _index;
        private static readonly List<int> Choices = new List<int>();
        private static Menu_Dev_MMOAddon _built;
        private static Image[] _typeButtons;
        private static Text _header, _choiceText, _infoText;
        private static Color _offColor;

        private static gameScript Game(Menu_Dev_MMOAddon m) => Traverse.Create(m).Field("gS_").GetValue<gameScript>();
        private static mainScript Main(Menu_Dev_MMOAddon m) => Traverse.Create(m).Field("mS_").GetValue<mainScript>();
        internal static int Choice => _index < Choices.Count ? Choices[_index] : -1;

        internal static void Build(Menu_Dev_MMOAddon menu)
        {
            if (_built == menu || menu == null) return;
            var window = UiKit.Find(menu.transform, "WindowMain");
            if (window == null) return;

            var allBtn = window.GetComponentsInChildren<Button>(true).FirstOrDefault();
            var style = window.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t != null && t.font != null);
            if (allBtn == null || style == null) return;

            _built = menu;
            var btnRt = (RectTransform)allBtn.transform;

            _header = UiKit.CopyText(style, window, TextAnchor.MiddleCenter);
            _header.rectTransform.anchorMin = _header.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _header.rectTransform.sizeDelta = new Vector2(460f, 22f);
            _header.rectTransform.anchoredPosition = new Vector2(0f, 92f);

            _typeButtons = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                int tIdx = i;
                var b = UiKit.Clone(btnRt, window, "Button_MmoLiveType" + i);
                b.gameObject.SetActive(true);
                b.anchorMin = b.anchorMax = new Vector2(0.5f, 0f);
                b.sizeDelta = new Vector2(96f, 26f);
                b.anchoredPosition = new Vector2(-200f + i * 100f, 62f);
                var btn = b.GetComponent<Button>();
                btn.interactable = true;
                btn.onClick.AddListener(() =>
                {
                    Traverse.Create(menu).Field("sfx_").GetValue<sfxScript>()?.PlaySound(3, force: true);
                    Type = tIdx;
                    _index = 0;
                    Refresh(menu);
                });
                _typeButtons[i] = b.GetComponent<Image>();
            }
            _offColor = _typeButtons[0].color;

            _choiceText = UiKit.CopyText(style, window, TextAnchor.MiddleCenter);
            _choiceText.rectTransform.anchorMin = _choiceText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _choiceText.rectTransform.sizeDelta = new Vector2(460f, 22f);
            _choiceText.rectTransform.anchoredPosition = new Vector2(0f, 36f);

            _infoText = UiKit.CopyText(style, window, TextAnchor.MiddleCenter);
            _infoText.rectTransform.anchorMin = _infoText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _infoText.rectTransform.sizeDelta = new Vector2(460f, 20f);
            _infoText.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        }

        internal static void Refresh(Menu_Dev_MMOAddon menu)
        {
            if (_built != menu) return;
            var g = Game(menu);
            var ms = Main(menu);
            if (g == null || ms == null) return;
            Txt.Refresh(ms);

            _header.text = LiveOpsLabels.Campaign;
            string[] names = {
                LiveOpsLabels.TypeNone,
                LiveOpsLabels.TypeSeason,
                LiveOpsLabels.TypeEvent,
                LiveOpsLabels.TypeCollab,
                LiveOpsLabels.TypeRelaunch
            };

            for (int i = 0; i < 5; i++)
            {
                var txt = _typeButtons[i].GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = names[i];
                _typeButtons[i].color = (i == Type) ? new Color(0.25f, 0.58f, 0.85f, 1f) : _offColor;
            }

            Choices.Clear();
            if (Type != ContentType.None) Choices.Add(0);
            long extra = Type == ContentType.None ? 0 : Live.ContentCost(g, ms, ms.GetComponent<licences>(), Type, Choice);
            _choiceText.text = Type == ContentType.None ? LiveOpsLabels.InfoNone : $"{LiveOpsLabels.Extra}: {ms.GetMoney(extra, true)}";
            _infoText.text = Type == ContentType.Season ? LiveOpsLabels.InfoSeason
                : Type == ContentType.Event ? LiveOpsLabels.InfoEvent
                : Type == ContentType.Collab ? LiveOpsLabels.InfoCollab
                : Type == ContentType.Relaunch ? LiveOpsLabels.InfoRelaunch : "";
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_MMOAddon), "Init")]
    internal static class Patch_MenuDevMMOAddon_Init
    {
        private static void Prefix() => MmoAddonPanel.Type = ContentType.None;
        private static void Postfix(Menu_Dev_MMOAddon __instance)
        {
            MmoAddonPanel.Build(__instance);
            MmoAddonPanel.Refresh(__instance);
        }
    }
}
