using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MGT2Mod
{
    internal static class ModHubLabels
    {
        private static bool IsAr
        {
            get
            {
                var main = GameObject.Find("Main");
                var s = main != null ? main.GetComponent<settingsScript>() : null;
                return s != null && s.language == 13;
            }
        }

        internal static string Title => IsAr
            ? "(Mod Systems Hub) \ufe94\ufee3\ufeaa\ufed8\ufe98\ufee4\ufedf\ufe8d \ufe94\ufee4\ufecc\ufee7\ufef7\ufe8d \ufea3\ufee4\ufef4\ufea0\ufedf \ufef2\ufebc\ufef4\ufe8e\ufeae\ufedf\ufe8d \ufeab\ufead\ufee3"
            : "Advanced Mod Systems Hub";

        internal static string Subtitle => IsAr
            ? "\ufe94\ufedb\ufeae\ufeb8\ufedf\ufe8d \ufe93\ufead\ufe8d\ufea9\ufe87\ufeed \ufea3\ufe8e\ufe92\ufeae\ufef7\ufe8d \ufe94\ufeb8\ufea4\ufee4\ufedf \ufe94\ufef4\ufe98\ufea4\ufe98\ufedf\ufe8d \ufe94\ufef4\ufee8\ufef2\ufedf\ufe8d \ufe94\ufee4\ufecc\ufee7\ufe83 \ufea1\ufef4\ufee4\ufea0 \ufee2\ufee3\ufeed \ufee2\ufedc\ufea4\ufe98\ufedf\ufe8d \ufe94\ufea3\ufeee\ufedf"
            : "Central Control Dashboard for All Systems & Game Menus";

        internal static string OpenBtn => IsAr ? "\ufed3\ufe98\ufea4" : "Launch";
        internal static string KeyHint => IsAr ? "\ufeae\ufe8e\ufebc\ufe98\ufea7\ufefc\ufe8d" : "Hotkey";
        internal static string AlsoInMenu => IsAr ? ":\ufe8e\ufeac\ufef4\ufef3\ufe87 \ufe8e\ufeeb\ufeaa\ufea0\ufe97 \ufeed" : "In Menu:";
    }

    public static class ModHubWindow
    {
        private static GameObject _root;
        private static Transform _listContent;
        private static Text _styleText;
        private static GameObject _btnTemplate;

        public static void Toggle()
        {
            if (_root != null && _root.activeSelf)
            {
                _root.SetActive(false);
                return;
            }
            Open();
        }

        public static void Open()
        {
            var mainGo = GameObject.Find("Main");
            if (mainGo == null) return;
            var gui = mainGo.GetComponent<GUI_Main>();
            if (gui == null) return;

            if (_root == null)
                BuildWindow(gui);

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private static void BuildWindow(GUI_Main gui)
        {
            var canvas = GameObject.Find("CanvasInGameMenu")?.GetComponent<Canvas>()
                      ?? UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _root = new GameObject("ModHub_Window", typeof(RectTransform));
            _root.transform.SetParent(canvas.transform, false);

            var rt = (RectTransform)_root.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(880f, 620f);

            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);

            // Find style template
            var existingText = gui.GetComponentsInChildren<Text>(true);
            if (existingText.Length > 0) _styleText = existingText[0];
            var existingBtn = gui.GetComponentsInChildren<Button>(true);
            if (existingBtn.Length > 0) _btnTemplate = existingBtn[0].gameObject;

            // Title Bar
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(_root.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-40f, 55f);
            titleRt.anchoredPosition = new Vector2(0f, -10f);

            var titleTxt = titleGo.GetComponent<Text>();
            if (_styleText != null) { titleTxt.font = _styleText.font; }
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.82f, 0.25f);
            titleTxt.text = ModHubLabels.Title;

            // Close button [X]
            var closeGo = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(_root.transform, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(36f, 36f);
            closeRt.anchoredPosition = new Vector2(-12f, -12f);
            closeGo.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f);

            var closeTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            closeTxtGo.transform.SetParent(closeGo.transform, false);
            var closeTxt = closeTxtGo.GetComponent<Text>();
            if (_styleText != null) closeTxt.font = _styleText.font;
            closeTxt.fontSize = 18;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
            closeTxt.text = "X";
            ((RectTransform)closeTxtGo.transform).sizeDelta = closeRt.sizeDelta;

            closeGo.GetComponent<Button>().onClick.AddListener(() => _root.SetActive(false));

            // Scroll container
            var scrollGo = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_root.transform, false);
            var sRt = (RectTransform)scrollGo.transform;
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.sizeDelta = new Vector2(-28f, -80f);
            sRt.anchoredPosition = new Vector2(0f, -32f);

            scrollGo.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.85f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _listContent = contentGo.transform;

            var cRt = (RectTransform)contentGo.transform;
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 8f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = cRt;

            PopulateSystems();
        }

        private struct ModSystemInfo
        {
            public string Title;
            public string Description;
            public string InGameLocation;
            public string Hotkey;
            public Color HeaderColor;
            public string AssemblyName;
            public string WindowClassName;
            public string ToggleMethodName;
        }

        private static void PopulateSystems()
        {
            var systems = new List<ModSystemInfo>
            {
                new ModSystemInfo {
                    Title = "1. سوق الأسهم، السيطرة والاستحواذ (Stock Market & IPO)",
                    Description = "اكتتاب عام، تداول أسهم الاستوديوهات، حبة السم للدفاع ضد الاستحواذ المعادي، وتوزيعات الأرباح.",
                    InGameLocation = "قائمة بنك المدينة (Menu_Bank) وقائمة الإدارة (Management)",
                    Hotkey = "F8",
                    HeaderColor = new Color(0.18f, 0.8f, 0.44f),
                    AssemblyName = "StockMarket",
                    WindowClassName = "MGT2Mod.StockMarket.StockMarketWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "2. صفقات النشر وحروب المزايدة (Publishing Deals & Bidding Wars)",
                    Description = "تمويل استوديوهات الطرف الثالث، نسب الأرباح، حصرية المنصات، والتنافس مع الناشرين في المزادات.",
                    InGameLocation = "قائمة اختيار الناشر (Select Publisher) وقائمة النشر (Publishing)",
                    Hotkey = "F6",
                    HeaderColor = new Color(0.2f, 0.8f, 0.5f),
                    AssemblyName = "PublishingDeals",
                    WindowClassName = "MGT2Mod.PublishingDealsWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "3. مجتمع اللاعبين وتريندات السوشيال (Community Dynamics & Hype)",
                    Description = "تأثير الهايب، فخ التوقعات، مراجعات القصف، صناع المحتوى والستريمرز، وإدارة الأزمات والاعتذارات.",
                    InGameLocation = "مكتب التسويق (Marketing Main) وقائمة الإحصائيات (Statistics)",
                    Hotkey = "F7",
                    HeaderColor = new Color(0.2f, 0.6f, 0.9f),
                    AssemblyName = "CommunityDynamics",
                    WindowClassName = "MGT2Mod.CommunityWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "4. الأتمتة الذكية وتوزيع الموظفين (Smart Automation & ROI)",
                    Description = "توزيع ذكي للموظفين حسب المهارة، استراحة تلقائية عند الإرهاق، وتحليل العائد الاستثماري للمحركات.",
                    InGameLocation = "نظرة عامة على الموظفين (Staff Overview) وقائمة الـ Personal",
                    Hotkey = "F9",
                    HeaderColor = new Color(0.95f, 0.7f, 0.15f),
                    AssemblyName = "SmartAutomation",
                    WindowClassName = "MGT2Mod.SmartDashboardWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "5. مختبر السيليكون وهندسة التبريد (Custom Silicon, Fabs & GPUs)",
                    Description = "تطوير دقة التصنيع (90nm إلى 1nm)، التعاقد مع المسابك العالمية، كروت شاشة منفصلة، وفضيحة RRoD.",
                    InGameLocation = "مختبر الهاردوير وتطوير الكونسول (Menu_Dev_KonsoleMain)",
                    Hotkey = "F10",
                    HeaderColor = new Color(0.15f, 0.65f, 0.8f),
                    AssemblyName = "CustomSilicon",
                    WindowClassName = "MGT2Mod.SiliconWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "6. لغات البرمجة والدين التقني (Compilers & Tech Debt)",
                    Description = "اختيار لغات المحرك (Assembly/C, C++/C#, AI)، تراكم الكود المشوه (Spaghetti)، وإعادة الهيكلة الشاملة.",
                    InGameLocation = "غرفة تطوير المحركات (Menu_Dev_EngineMain)",
                    Hotkey = "F11",
                    HeaderColor = new Color(0.9f, 0.35f, 0.25f),
                    AssemblyName = "TechDebt",
                    WindowClassName = "MGT2Mod.TechDebtWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "7. بطولات الرياضات الإلكترونية (Esports Ecosystem & Majors)",
                    Description = "استضافة البطولات العالمية والكؤوس الكبرى، صفقات البث الرقمي والتلفزيوني، وموازنة الميتا.",
                    InGameLocation = "إدارة الألعاب الخدمية و الـ MMO (Menu_InAppVerwalten)",
                    Hotkey = "F5",
                    HeaderColor = new Color(0.85f, 0.35f, 0.85f),
                    AssemblyName = "EsportsEcosystem",
                    WindowClassName = "MGT2Mod.EsportsWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "8. استوديو ترانزميديا: أنمي وأفلام وبضائع (Transmedia & Merch)",
                    Description = "إنتاج مسلسلات أنمي وأفلام هوليوود، تأثير الهالو لتنشيط مبيعات الألعاب القديمة، وتماثيل المجموعات.",
                    InGameLocation = "متجر المعجبين والبضائع (Menu_Fanshop)",
                    Hotkey = "F4",
                    HeaderColor = new Color(0.65f, 0.35f, 0.85f),
                    AssemblyName = "TransmediaMerch",
                    WindowClassName = "MGT2Mod.TransmediaWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "9. الوصول المبكر والتمويل الجماعي (Early Access & Crowdfunding)",
                    Description = "حملات كيك ستارتر لجمع التمويل بدون بيع أسهم، خارطة طريق الوصول المبكر، وخطر جحيم التطوير.",
                    InGameLocation = "واجهة تطوير لعبة جديدة (Menu_DevGame) بجانب الضبط الذهبي",
                    Hotkey = "F3",
                    HeaderColor = new Color(0.2f, 0.7f, 0.85f),
                    AssemblyName = "EarlyAccess",
                    WindowClassName = "MGT2Mod.EarlyAccessWindow",
                    ToggleMethodName = "Toggle"
                },
                new ModSystemInfo {
                    Title = "10. ورشة التعديلات والمودات (Official Modding SDK & UGC)",
                    Description = "إطلاق أدوات التعديل للاعبين، إطالة عمر الألعاب بمبيعات مستمرة لسنوات، والاستحواذ على المودات الناجحة.",
                    InGameLocation = "قائمة تحديثات الألعاب والـ DLC (Menu_Dev_UpdateSelectGame)",
                    Hotkey = "F2",
                    HeaderColor = new Color(0.25f, 0.75f, 0.45f),
                    AssemblyName = "ModWorkshop",
                    WindowClassName = "MGT2Mod.ModWorkshopWindow",
                    ToggleMethodName = "Toggle"
                }
            };

            foreach (var sys in systems)
            {
                CreateCard(sys);
            }
        }

        private static void CreateCard(ModSystemInfo sys)
        {
            var cardGo = new GameObject("Card_" + sys.AssemblyName, typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(_listContent, false);
            var crt = (RectTransform)cardGo.transform;
            crt.sizeDelta = new Vector2(830f, 85f);
            cardGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            // Left colored accent strip
            var stripGo = new GameObject("AccentStrip", typeof(RectTransform), typeof(Image));
            stripGo.transform.SetParent(cardGo.transform, false);
            var srt = (RectTransform)stripGo.transform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0f, 1f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.sizeDelta = new Vector2(8f, 0f);
            srt.anchoredPosition = Vector2.zero;
            stripGo.GetComponent<Image>().color = sys.HeaderColor;

            // Title & Hotkey Text
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(cardGo.transform, false);
            var trt = (RectTransform)titleGo.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.82f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 26f);
            trt.anchoredPosition = new Vector2(18f, -6f);

            var tTxt = titleGo.GetComponent<Text>();
            if (_styleText != null) tTxt.font = _styleText.font;
            tTxt.fontSize = 14;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.color = sys.HeaderColor;
            tTxt.alignment = TextAnchor.MiddleLeft;
            tTxt.text = $"{sys.Title}  [<color=#F1C40F>{sys.Hotkey}</color>]";

            // Description Text
            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            descGo.transform.SetParent(cardGo.transform, false);
            var drt = (RectTransform)descGo.transform;
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(0.82f, 1f);
            drt.pivot = new Vector2(0f, 0.5f);
            drt.sizeDelta = new Vector2(0f, -32f);
            drt.anchoredPosition = new Vector2(18f, -2f);

            var dTxt = descGo.GetComponent<Text>();
            if (_styleText != null) dTxt.font = _styleText.font;
            dTxt.fontSize = 11;
            dTxt.color = new Color(0.85f, 0.88f, 0.92f);
            dTxt.alignment = TextAnchor.MiddleLeft;
            dTxt.text = $"{sys.Description}\n<color=#F39C12><b>{ModHubLabels.AlsoInMenu}</b> {sys.InGameLocation}</color>";

            // Launch Button
            var btnGo = new GameObject("Btn_Launch", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardGo.transform, false);
            var brt = (RectTransform)btnGo.transform;
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(110f, 44f);
            brt.anchoredPosition = new Vector2(-16f, 0f);

            btnGo.GetComponent<Image>().color = sys.HeaderColor;

            var bTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            bTxtGo.transform.SetParent(btnGo.transform, false);
            var bTxt = bTxtGo.GetComponent<Text>();
            if (_styleText != null) bTxt.font = _styleText.font;
            bTxt.fontSize = 13;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.color = Color.black;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.text = $"{ModHubLabels.OpenBtn} ({sys.Hotkey})";
            ((RectTransform)bTxtGo.transform).sizeDelta = brt.sizeDelta;

            var info = sys;
            btnGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                LaunchSystem(info);
            });
        }

        private static void LaunchSystem(ModSystemInfo sys)
        {
            try
            {
                _root.SetActive(false); // Close hub
                var t = Type.GetType(sys.WindowClassName + ", " + sys.AssemblyName);
                if (t == null)
                {
                    // Fallback scan across all loaded assemblies
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = a.GetType(sys.WindowClassName);
                        if (t != null) break;
                    }
                }

                if (t != null)
                {
                    var m = t.GetMethod(sys.ToggleMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (m != null)
                    {
                        if (m.GetParameters().Length == 0)
                        {
                            m.Invoke(null, null);
                            return;
                        }
                    }

                    // Try Show(ms, guiMain)
                    var mShow = t.GetMethod("Show", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (mShow != null)
                    {
                        var main = GameObject.Find("Main");
                        var ms = main != null ? main.GetComponent<mainScript>() : null;
                        var gui = GameObject.Find("CanvasInGameMenu")?.GetComponent<GUI_Main>();
                        mShow.Invoke(null, new object[] { ms, gui });
                        return;
                    }
                }

                Debug.LogWarning($"[ModHub] Could not find toggle method for {sys.WindowClassName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModHub] Launch failed: {ex}");
            }
        }
    }
}
