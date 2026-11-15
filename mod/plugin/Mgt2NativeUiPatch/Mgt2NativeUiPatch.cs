using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.nativeuipatch", "MGT2 Native Silver-Blue UI Patch & Hub", "1.0.0")]
    public class Mgt2NativeUiPatchPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.nativeuipatch").PatchAll(typeof(Mgt2NativeUiPatchPlugin).Assembly);
            RealismHubWindow.Init();
            Log.LogInfo("MGT2 Native Silver-Blue UI Patch & Realism Hub loaded (F1 or via Develop Game Menu).");
        }
    }

    /// <summary>
    /// Exact color & component palette extracted from MGT2's native Menu_DevGameMain ("طوّر لعبة")
    /// and Menu_DevGame ("لعبة جديدة") windows.
    /// </summary>
    public static class Mgt2NativePalette
    {
        // Outer metallic border & light silver-grey window body (#EBEBED)
        public static readonly Color WindowOuterBorder = new Color(0.22f, 0.24f, 0.27f, 1f);
        public static readonly Color WindowSilverBody = new Color(0.92f, 0.92f, 0.93f, 1f);

        // Top Glossy Cerulean Blue Header Bar (#1C7DC2)
        public static readonly Color HeaderCeruleanBlue = new Color(0.11f, 0.49f, 0.76f, 1f);

        // Section Ribbon Blue Label Strip (#1C7AB9 - like "المصمم الرئيسي" / "نوع اللعبة")
        public static readonly Color SectionRibbonBlue = new Color(0.11f, 0.48f, 0.73f, 1f);

        // Recessed Info Box Grey (#CFD1D4 - like the top 5-star box in Menu_DevGame)
        public static readonly Color RecessedBoxGrey = new Color(0.81f, 0.82f, 0.84f, 1f);

        // Primary Steel-Blue Bevelled Button (#5299C2 - like "لعبة جديدة" in Menu_DevGameMain)
        public static readonly Color ButtonSteelBlue = new Color(0.32f, 0.60f, 0.76f, 1f);

        // Standard Silver-Metallic Bevelled Button (#D6D9DC - like "لعبة موبايل جديدة" / "ريماستر")
        public static readonly Color ButtonSilverMetallic = new Color(0.84f, 0.85f, 0.86f, 1f);

        // Crisp dark charcoal/black text used on MGT2 silver & steel-blue buttons
        public static readonly Color TextDarkCharcoal = new Color(0.09f, 0.10f, 0.12f, 1f);

        // Close Circle Button silver-white fill
        public static readonly Color CloseCircleWhite = new Color(0.95f, 0.95f, 0.96f, 1f);

        private static Font _cachedGameFont;

        public static Font GetNativeGameFont()
        {
            if (_cachedGameFont != null) return _cachedGameFont;
            var canvas = GameObject.Find("CanvasInGameMenu");
            if (canvas != null)
            {
                var anyText = canvas.GetComponentInChildren<Text>(true);
                if (anyText != null && anyText.font != null)
                {
                    _cachedGameFont = anyText.font;
                    return _cachedGameFont;
                }
            }
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// Converts any mod Panel_* hierarchy in-place to match MGT2's native Silver-White & Cerulean-Blue UI.
        /// </summary>
        public static void ApplyNativeMgt2SkinToPanel(GameObject rootPanel)
        {
            if (rootPanel == null) return;
            var font = GetNativeGameFont();

            // 1. Style main window background as Light Silver-White (#EBEBED) with 2px dark frame Outline
            var rootImg = rootPanel.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = WindowSilverBody;
                EnsureOutline(rootPanel, WindowOuterBorder, new Vector2(2f, -2f));
                EnsureShadow(rootPanel, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -6f));
            }

            // 2. Ensure Glossy Cerulean Blue Header Bar behind Title
            var titleTr = rootPanel.transform.Find("Title");
            if (titleTr != null)
            {
                var headerBar = rootPanel.transform.Find("MGT2_HeaderBar");
                if (headerBar == null)
                {
                    var hbGo = new GameObject("MGT2_HeaderBar");
                    hbGo.transform.SetParent(rootPanel.transform, false);
                    hbGo.transform.SetSiblingIndex(0);
                    var hbRt = hbGo.AddComponent<RectTransform>();
                    hbRt.anchorMin = new Vector2(0f, 1f);
                    hbRt.anchorMax = new Vector2(1f, 1f);
                    hbRt.pivot = new Vector2(0.5f, 1f);
                    hbRt.sizeDelta = new Vector2(-8f, 48f);
                    hbRt.anchoredPosition = new Vector2(0f, -4f);

                    var hbImg = hbGo.AddComponent<Image>();
                    hbImg.color = HeaderCeruleanBlue;
                    hbImg.raycastTarget = false;
                    EnsureOutline(hbGo, new Color(0.06f, 0.28f, 0.48f, 1f), new Vector2(1f, -1f));

                    // Add [</>] Dev Badge on the left of the Blue Header (matching Screenshot 2 & 3)
                    var iconGo = new GameObject("HeaderCodeBadge");
                    iconGo.transform.SetParent(hbGo.transform, false);
                    var iconRt = iconGo.AddComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0f, 0.5f);
                    iconRt.anchorMax = new Vector2(0f, 0.5f);
                    iconRt.pivot = new Vector2(0f, 0.5f);
                    iconRt.sizeDelta = new Vector2(42f, 32f);
                    iconRt.anchoredPosition = new Vector2(12f, 0f);
                    var iconBg = iconGo.AddComponent<Image>();
                    iconBg.color = new Color(0.86f, 0.88f, 0.90f, 1f);
                    iconBg.raycastTarget = false;
                    EnsureOutline(iconGo, WindowOuterBorder, new Vector2(1f, -1f));

                    var badgeTxtGo = new GameObject("BadgeTxt");
                    badgeTxtGo.transform.SetParent(iconGo.transform, false);
                    var btRt = badgeTxtGo.AddComponent<RectTransform>();
                    btRt.anchorMin = Vector2.zero;
                    btRt.anchorMax = Vector2.one;
                    btRt.offsetMin = Vector2.zero;
                    btRt.offsetMax = Vector2.zero;
                    var bt = badgeTxtGo.AddComponent<Text>();
                    bt.font = font;
                    bt.fontSize = 13;
                    bt.fontStyle = FontStyle.Bold;
                    bt.alignment = TextAnchor.MiddleCenter;
                    bt.color = TextDarkCharcoal;
                    bt.text = "</>";
                    bt.raycastTarget = false;
                }

                var titleTxt = titleTr.GetComponent<Text>();
                if (titleTxt != null)
                {
                    titleTxt.font = font;
                    titleTxt.color = Color.white;
                    titleTxt.fontStyle = FontStyle.Bold;
                    titleTxt.raycastTarget = false;
                    EnsureShadow(titleTr.gameObject, new Color(0f, 0f, 0f, 0.75f), new Vector2(1.5f, -1.5f));
                }
            }

            // 3. Style StatsSummary as MGT2's Recessed Silver-Grey Box (#CFD1D4) with 1px dark border & dark text
            var statsTr = rootPanel.transform.Find("StatsSummary");
            if (statsTr != null)
            {
                var statsBg = statsTr.parent.Find("MGT2_StatsRecessedBox");
                if (statsBg == null)
                {
                    var sbGo = new GameObject("MGT2_StatsRecessedBox");
                    sbGo.transform.SetParent(rootPanel.transform, false);
                    sbGo.transform.SetSiblingIndex(1);
                    var sRt = statsTr.GetComponent<RectTransform>();
                    var boxRt = sbGo.AddComponent<RectTransform>();
                    boxRt.anchorMin = sRt.anchorMin;
                    boxRt.anchorMax = sRt.anchorMax;
                    boxRt.pivot = sRt.pivot;
                    boxRt.sizeDelta = sRt.sizeDelta;
                    boxRt.anchoredPosition = sRt.anchoredPosition;

                    var boxImg = sbGo.AddComponent<Image>();
                    boxImg.color = RecessedBoxGrey;
                    boxImg.raycastTarget = false;
                    EnsureOutline(sbGo, new Color(0.42f, 0.44f, 0.48f, 1f), new Vector2(1f, -1f));
                }

                var statsTxt = statsTr.GetComponent<Text>();
                if (statsTxt != null)
                {
                    statsTxt.font = font;
                    statsTxt.color = TextDarkCharcoal;
                    statsTxt.fontStyle = FontStyle.Bold;
                    statsTxt.raycastTarget = false;
                }
            }

            // 4. Style all Rows inside RowsContainer as Silver-Grey Cards with Blue Ribbon Accent & Steel-Blue/Silver Buttons
            var rowsContainer = rootPanel.transform.Find("RowsContainer");
            if (rowsContainer != null)
            {
                for (int i = 0; i < rowsContainer.childCount; i++)
                {
                    var row = rowsContainer.GetChild(i);
                    var rowImg = row.GetComponent<Image>();
                    if (rowImg != null)
                    {
                        rowImg.color = new Color(0.85f, 0.86f, 0.88f, 1f);
                        EnsureOutline(row.gameObject, new Color(0.32f, 0.34f, 0.38f, 1f), new Vector2(1f, -1f));
                    }

                    // Ensure top thin Blue Ribbon strip on each row (matching Screenshot 3 blue section headers)
                    if (row.Find("MGT2_BlueRibbon") == null)
                    {
                        var rib = new GameObject("MGT2_BlueRibbon");
                        rib.transform.SetParent(row, false);
                        rib.transform.SetAsFirstSibling();
                        var ribRt = rib.AddComponent<RectTransform>();
                        ribRt.anchorMin = new Vector2(0f, 0f);
                        ribRt.anchorMax = new Vector2(0f, 1f);
                        ribRt.pivot = new Vector2(0f, 0.5f);
                        ribRt.sizeDelta = new Vector2(6f, 0f);
                        ribRt.anchoredPosition = Vector2.zero;
                        var ribImg = rib.AddComponent<Image>();
                        ribImg.color = SectionRibbonBlue;
                        ribImg.raycastTarget = false;
                    }

                    var rowTxtTr = row.Find("RowText");
                    if (rowTxtTr != null)
                    {
                        var rtTxt = rowTxtTr.GetComponent<Text>();
                        if (rtTxt != null)
                        {
                            rtTxt.font = font;
                            rtTxt.color = TextDarkCharcoal;
                            rtTxt.fontStyle = FontStyle.Bold;
                            rtTxt.raycastTarget = false;
                        }
                    }

                    // Style buttons inside row as MGT2 Steel-Blue or Silver-Metallic bevelled buttons with Black Bold text
                    var buttons = row.GetComponentsInChildren<Button>(true);
                    for (int bIdx = 0; bIdx < buttons.Length; bIdx++)
                    {
                        var btn = buttons[bIdx];
                        var bImg = btn.GetComponent<Image>();
                        if (bImg != null)
                        {
                            bImg.color = (i == 0 && bIdx == 0) ? ButtonSteelBlue : ButtonSilverMetallic;
                            bImg.raycastTarget = true;
                            EnsureOutline(btn.gameObject, new Color(0.12f, 0.14f, 0.16f, 1f), new Vector2(1.2f, -1.2f));
                            EnsureShadow(btn.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -2f));
                        }
                        var bTxt = btn.GetComponentInChildren<Text>(true);
                        if (bTxt != null)
                        {
                            bTxt.font = font;
                            bTxt.color = TextDarkCharcoal;
                            bTxt.fontStyle = FontStyle.Bold;
                            bTxt.raycastTarget = false;
                        }
                    }
                }
            }

            // 5. Style top-right Close 'X' button as MGT2's circular white/silver button with dark 'X'
            var allBtns = rootPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns)
            {
                if (b.transform.parent == rootPanel.transform)
                {
                    var bImg = b.GetComponent<Image>();
                    var bTxt = b.GetComponentInChildren<Text>(true);
                    if (bTxt != null && bTxt.text == "X")
                    {
                        if (bImg != null) bImg.color = CloseCircleWhite;
                        bTxt.color = new Color(0.20f, 0.22f, 0.25f, 1f);
                        bTxt.fontStyle = FontStyle.Bold;
                        bTxt.fontSize = 15;
                        EnsureOutline(b.gameObject, WindowOuterBorder, new Vector2(1.2f, -1.2f));
                    }
                }
            }
        }

        public static void EnsureOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        public static void EnsureShadow(GameObject go, Color color, Vector2 distance)
        {
            var shadow = go.GetComponent<Shadow>();
            if (shadow == null) shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }

    /// <summary>
    /// Injects a native Steel-Blue button directly inside Menu_DevGameMain ("طوّر لعبة" — Screenshot 2)
    /// so the player can open the Realism Systems Hub right from the Develop Game popup!
    /// </summary>
    [HarmonyPatch(typeof(Menu_DevGameMain), "OnEnable")]
    internal static class DevGameSelectMenuHubPatch
    {
        private const string HubBtnName = "BUTTON_MGT2_RealismHub";

        [HarmonyPostfix]
        private static void Postfix(Menu_DevGameMain __instance)
        {
            try
            {
                if (__instance == null) return;
                var existing = FindDeep(__instance.transform, HubBtnName);
                if (existing != null) return;

                // Clone one of the native silver/blue buttons inside Menu_DevGameMain
                var anyBtn = __instance.GetComponentInChildren<Button>(true);
                if (anyBtn == null) return;

                var clone = UnityEngine.Object.Instantiate(anyBtn.gameObject, anyBtn.transform.parent, false);
                clone.name = HubBtnName;
                clone.SetActive(true);

                foreach (var st in clone.GetComponentsInChildren<setText>(true)) UnityEngine.Object.DestroyImmediate(st);
                foreach (var et in clone.GetComponentsInChildren<EventTrigger>(true)) UnityEngine.Object.DestroyImmediate(et);
                foreach (var tt in clone.GetComponentsInChildren<tooltip>(true)) UnityEngine.Object.DestroyImmediate(tt);

                var rt = clone.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(220f, 34f);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 10f);

                var img = clone.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Mgt2NativePalette.ButtonSteelBlue;
                    img.raycastTarget = true;
                }

                var txt = clone.GetComponentInChildren<Text>(true);
                if (txt != null)
                {
                    txt.text = "Realism Systems Hub (F1)";
                    txt.color = Mgt2NativePalette.TextDarkCharcoal;
                    txt.fontStyle = FontStyle.Bold;
                    txt.fontSize = 13;
                    txt.raycastTarget = false;
                }

                var btn = clone.GetComponent<Button>();
                btn.enabled = true;
                btn.interactable = true;
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() =>
                {
                    var mainGo = GameObject.Find("Main");
                    var sfx = mainGo != null ? mainGo.GetComponent<sfxScript>() : null;
                    if (sfx != null) sfx.PlaySound(3, true);
                    RealismHubWindow.OpenHub();
                });
            }
            catch (Exception e)
            {
                Mgt2NativeUiPatchPlugin.Log.LogError(e);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
