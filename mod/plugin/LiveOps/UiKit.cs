using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    /// <summary>Text helpers. Arabic labels are already shaped and reversed (see make_labels.py), so a
    /// mixed sentence is built by joining its pieces in reverse order.</summary>
    internal static class Txt
    {
        private const int ArabicLanguage = 13; // settingsScript.language

        internal static void Refresh(mainScript ms)
        {
            var settings = ms != null ? ms.GetComponent<settingsScript>() : null;
            LiveOpsLabels.Arabic = settings != null && settings.language == ArabicLanguage;
        }

        internal static string Join(params string[] logicalOrder) => string.Join(" ", Order(logicalOrder));

        internal static string[] Order(string[] logicalOrder) => LiveOpsLabels.Arabic ? logicalOrder.Reverse().ToArray() : logicalOrder;

        internal static string Occasion(int i)
        {
            switch (i)
            {
                case 0: return LiveOpsLabels.Occ0;
                case 1: return LiveOpsLabels.Occ1;
                case 2: return LiveOpsLabels.Occ2;
                case 3: return LiveOpsLabels.Occ3;
                case 4: return LiveOpsLabels.Occ4;
                case 5: return LiveOpsLabels.Occ5;
                case 6: return LiveOpsLabels.Occ6;
                default: return LiveOpsLabels.Occ7;
            }
        }

        internal static string LicenceType(int t)
        {
            switch (t)
            {
                case 0: return LiveOpsLabels.Lic0;
                case 1: return LiveOpsLabels.Lic1;
                case 2: return LiveOpsLabels.Lic2;
                case 3: return LiveOpsLabels.Lic3;
                case 4: return LiveOpsLabels.Lic4;
                default: return LiveOpsLabels.Lic5;
            }
        }
    }

    /// <summary>Builds new UI out of the game's own elements so it inherits sprites, fonts and colours.</summary>
    internal static class UiKit
    {
        internal static RectTransform Find(Transform root, string name) =>
            root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);

        /// <summary>Copy of a game element: keeps looks, drops the game's own behaviour.</summary>
        internal static RectTransform Clone(RectTransform source, Transform parent, string name)
        {
            var go = Object.Instantiate(source.gameObject, parent);
            go.name = name;
            go.SetActive(true);
            foreach (var t in go.GetComponentsInChildren<setText>(true)) Object.DestroyImmediate(t); // would reset our text
            foreach (var t in go.GetComponentsInChildren<EventTrigger>(true)) Object.DestroyImmediate(t);
            foreach (var b in go.GetComponentsInChildren<Button>(true))
            {
                b.onClick = new Button.ButtonClickedEvent(); // no persistent calls into the game's menu
            }
            return (RectTransform)go.transform;
        }

        internal static void SetText(Component root, string text)
        {
            var t = root.GetComponentInChildren<Text>(true);
            if (t != null) t.text = text;
        }

        internal static void SetTooltip(Component root, string text)
        {
            var tip = root.GetComponent<tooltip>();
            if (tip == null) return;
            tip.textID = -1;
            tip.textArray = "";
            tip.c = text;
        }

        /// <summary>Grow a centred window by <paramref name="dy"/> at the bottom, keeping every child
        /// where it was relative to the top; callers move the bottom row down themselves.</summary>
        internal static void GrowWindow(RectTransform window, float dy)
        {
            float height = window.rect.height;
            // full-height backgrounds (Fill_Window) keep stretching with the window; everything else stays put
            var kids = window.Cast<Transform>().Select(t => (RectTransform)t)
                .Where(k => !(k.anchorMin.y == 0f && k.anchorMax.y == 1f && Mathf.Abs(k.rect.height - height) < 1f)).ToArray();
            var saved = kids.Select(k => (pos: k.position, h: k.rect.height)).ToArray();
            window.sizeDelta += new Vector2(0f, dy);
            window.anchoredPosition -= new Vector2(0f, dy / 2f); // top edge fixed while children are restored
            for (int i = 0; i < kids.Length; i++)
            {
                kids[i].position = saved[i].pos;
                kids[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, saved[i].h);
            }
            window.anchoredPosition += new Vector2(0f, dy / 2f); // then re-centre the whole window on screen
        }

        internal static void MoveDown(RectTransform t, float dy) => t.anchoredPosition -= new Vector2(0f, dy);

        /// <summary>Vertical scroll area (mouse wheel + drag) whose content grows with its rows.</summary>
        internal static RectTransform ScrollArea(Transform parent, Vector2 topLeft, Vector2 size, out RectTransform content)
        {
            var view = new GameObject("LiveScroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            var rt = (RectTransform)view.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = size;
            var img = view.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f); // Mask needs a graphic; nearly invisible
            view.GetComponent<Mask>().showMaskGraphic = false;

            var c = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content = (RectTransform)c.transform;
            content.SetParent(rt, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var layout = c.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;
            layout.padding = new RectOffset(4, 12, 2, 2);
            c.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = view.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            // scroll like the game's own lists
            var game = Resources.FindObjectsOfTypeAll<ScrollRect>().FirstOrDefault(x => x != scroll && x.gameObject.scene.IsValid());
            scroll.scrollSensitivity = game != null ? game.scrollSensitivity : 25f;
            LiveOpsPlugin.Log.LogInfo($"scroll sensitivity {scroll.scrollSensitivity} (from {(game != null ? game.name : "default")})");
            return rt;
        }

        /// <summary>A label/value row in the style of the game's statistics columns.</summary>
        internal static Text[] Row(Transform content, Text labelStyle, Text valueStyle, float height = 22f)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(content, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            return new[] { CopyText(labelStyle, row.transform, Middle(labelStyle.alignment)), CopyText(valueStyle, row.transform, Middle(valueStyle.alignment)) };
        }

        // the game's columns are top-aligned; rows keep their side (it flips for right-to-left languages)
        private static TextAnchor Middle(TextAnchor a) => (TextAnchor)(3 + (int)a % 3);

        internal static Text CopyText(Text style, Transform parent, TextAnchor align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = style.font;
            t.fontSize = style.fontSize;
            t.color = style.color;
            t.supportRichText = true;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
