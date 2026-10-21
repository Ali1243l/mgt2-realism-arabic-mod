"""Content for 2030-2100 (GDD points 8, 24.1, 35): platforms, engine/gameplay features,
hardware parts and console features, appended to the game's DATA files by build.py.

Rules the game imposes (from the decompiled loaders):
- IDs are array indexes: new IDs must continue each file's sequence with no gap.
- Platforms have no Arabic keys (players see NAME EN); the four feature/hardware files
  read [NAME AR]/[DESC AR], stored in the same visual form as Text\\AR (to_game).
- Adding entries changes save layouts: a new game is required.
"""
from arabic_game import to_game

MON = "JAN FEB MAR APR MAY JUN JUL AUG SEP OCT NOV DEC".split()

# ---------------------------------------------------------------- platforms
# id, name, manufacturer, publisher id (or None), date, tech, typ, predecessor, price, dev costs,
# units, internet, gamepass, picture
# typ: 1 home console, 2 handheld, 3 phone/wearable, 4 arcade
PLATFORMS = [
    # Era 1: cloud & AI (2030-2045)
    (134, "iPhone 20", "Apple", None, "SEP 2028", 8, 3, 106, 5800, 8800, 220000000, 1, 0, "F_iPhone20.png"),
    (135, "Android 21", "Google", None, "JUN 2030", 8, 3, 111, 5600, 8600, 260000000, 1, 0, "F_Android21.png"),
    (136, "Steam Deck 3", "Valve", 85, "FEB 2033", 10, 2, 129, 560000, 190000, 30000000, 1, 10, "F_SteamDeck3.png"),
    (137, "iPhone 25", "Apple", None, "SEP 2033", 9, 3, 134, 6200, 9400, 240000000, 1, 0, "F_iPhone25.png"),
    (138, "Xbox Cloud", "Microsoft", 16, "NOV 2034", 10, 1, 96, 720000, 245000, 180000000, 1, 25, "F_XboxCloud.png"),
    (139, "Android 26", "Google", None, "JUN 2035", 9, 3, 135, 6000, 9200, 280000000, 1, 0, "F_Android26.png"),
    (140, "PlayStation 7", "Sony", 25, "NOV 2035", 10, 1, 95, 760000, 255000, 220000000, 1, 25, "F_PS7.png"),
    (141, "Nintendo Switch 4", "Nintendo", 18, "MAR 2037", 10, 1, 70, 700000, 235000, 190000000, 1, 25, "F_Switch4.png"),
    (142, "Holo Arcade", "Arcade Electrics", None, "FEB 2040", 11, 4, 133, 900000, 300000, 600000, 1, 0, "F_HoloArcade.png"),
    (143, "Apple Neural Phone", "Apple", None, "SEP 2040", 10, 3, 137, 6800, 10200, 260000000, 1, 0, "F_AppleNeural.png"),
    (144, "Android Neural", "Google", None, "JUN 2041", 10, 3, 139, 6600, 10000, 300000000, 1, 0, "F_AndroidNeural.png"),
    # Era 2: holograms & first neural input (2044-2065)
    (145, "PlayStation 8 Holo", "Sony", 25, "NOV 2044", 11, 1, 140, 840000, 280000, 230000000, 1, 30, "F_PS8.png"),
    (146, "Xbox Holo", "Microsoft", 16, "NOV 2045", 11, 1, 138, 820000, 275000, 190000000, 1, 30, "F_XboxHolo.png"),
    (147, "Nintendo Holo", "Nintendo", 18, "MAR 2046", 11, 1, 141, 780000, 262000, 200000000, 1, 30, "F_NintendoHolo.png"),
    (148, "Steam Holo Deck", "Valve", 85, "FEB 2047", 11, 2, 136, 640000, 215000, 35000000, 1, 10, "F_SteamHoloDeck.png"),
    (149, "Apple Vision Band", "Apple", None, "SEP 2050", 12, 3, 143, 7600, 11400, 280000000, 1, 0, "F_VisionBand.png"),
    (150, "Google Neural Band", "Google", None, "JUN 2051", 12, 3, 144, 7400, 11200, 320000000, 1, 0, "F_NeuralBand.png"),
    (151, "PlayStation 9 Neural", "Sony", 25, "NOV 2053", 12, 1, 145, 920000, 305000, 240000000, 1, 30, "F_PS9.png"),
    (152, "Xbox Neural", "Microsoft", 16, "NOV 2054", 12, 1, 146, 900000, 300000, 200000000, 1, 30, "F_XboxNeural.png"),
    (153, "Nintendo NeuroSwitch", "Nintendo", 18, "MAR 2055", 12, 1, 147, 860000, 288000, 210000000, 1, 30, "F_NeuroSwitch.png"),
    (154, "PlayStation 10 Holo-Room", "Sony", 25, "NOV 2062", 13, 1, 151, 1000000, 335000, 250000000, 1, 35, "F_PS10.png"),
    (155, "Xbox Infinity", "Microsoft", 16, "NOV 2063", 13, 1, 152, 980000, 330000, 210000000, 1, 35, "F_XboxInfinity.png"),
    (156, "Nintendo Dream", "Nintendo", 18, "MAR 2064", 13, 1, 153, 940000, 315000, 220000000, 1, 35, "F_NintendoDream.png"),
    # Era 3: bio-processors & quantum (2065-2085)
    (157, "Neuro Arcade", "Arcade Electrics", None, "FEB 2065", 14, 4, 142, 1150000, 380000, 500000, 1, 0, "F_NeuroArcade.png"),
    (158, "Apple Thought Link", "Apple", None, "SEP 2065", 14, 3, 149, 8600, 12800, 300000000, 1, 0, "F_ThoughtLink.png"),
    (159, "Google Mind OS", "Google", None, "JUN 2066", 14, 3, 150, 8400, 12600, 340000000, 1, 0, "F_MindOS.png"),
    (160, "PlayStation 11 Bio", "Sony", 25, "NOV 2072", 14, 1, 154, 1080000, 360000, 260000000, 1, 35, "F_PS11.png"),
    (161, "Xbox Quantum", "Microsoft", 16, "NOV 2073", 14, 1, 155, 1060000, 355000, 220000000, 1, 35, "F_XboxQuantum.png"),
    (162, "Nintendo Bio Link", "Nintendo", 18, "MAR 2074", 14, 1, 156, 1020000, 340000, 230000000, 1, 35, "F_BioLink.png"),
    # Era 4: full-dive (2083-2100)
    (163, "PlayStation Full-Dive", "Sony", 25, "NOV 2083", 15, 1, 160, 1160000, 390000, 270000000, 1, 40, "F_PSFullDive.png"),
    (164, "Xbox Full-Dive", "Microsoft", 16, "NOV 2084", 15, 1, 161, 1140000, 385000, 230000000, 1, 40, "F_XboxFullDive.png"),
    (165, "Nintendo Dream Dive", "Nintendo", 18, "MAR 2085", 15, 1, 162, 1100000, 370000, 240000000, 1, 40, "F_DreamDive.png"),
    (166, "Apple Neural Implant", "Apple", None, "SEP 2085", 16, 3, 158, 9800, 14600, 320000000, 1, 0, "F_AppleImplant.png"),
    (167, "Google Neural Implant", "Google", None, "JUN 2086", 16, 3, 159, 9600, 14400, 360000000, 1, 0, "F_GoogleImplant.png"),
    (168, "PlayStation Eternal", "Sony", 25, "NOV 2094", 16, 1, 163, 1240000, 420000, 280000000, 1, 40, "F_PSEternal.png"),
    (169, "Xbox Singularity", "Microsoft", 16, "NOV 2095", 16, 1, 164, 1220000, 415000, 240000000, 1, 40, "F_XboxSingularity.png"),
    (170, "Nintendo Infinity", "Nintendo", 18, "MAR 2096", 16, 1, 165, 1180000, 400000, 250000000, 1, 40, "F_NintendoInfinity.png"),
]
NEED = {1: ["51"], 2: ["51", "56"], 3: ["56"], 4: ["59"]}  # required gameplay features per type
COMPLEX = {1: 1, 2: 1, 3: 0, 4: 2}
LIFE = {1: 8, 2: 7, 3: 6, 4: 25}  # years a platform stays on sale after its successor arrives

# ---------------------------------------------------------------- engine features
# id, typ (0 gfx 1 sound 2 AI 3 physics), name, Arabic name, desc, Arabic desc, date, tech, points
ENGINE = [
    (67, 0, "Path Tracing", "تتبع المسار", "Every ray of light is simulated physically.", "يحاكي كل شعاع ضوئي فيزيائياً.", "MAR 2031", 10, 5000),
    (68, 2, "Generative NPC Brains", "عقول شخصيات توليدية", "NPCs talk and react with endless, unscripted dialogue.", "الشخصيات تتحدث وتتفاعل بحوارات لا نهائية غير مكتوبة مسبقاً.", "JUN 2032", 10, 2900),
    (69, 1, "Spatial Audio Synthesis", "توليد الصوت المكاني", "Sound is generated live from the 3D scene.", "يُولَّد الصوت مباشرة من المشهد ثلاثي الأبعاد.", "SEP 2032", 10, 5500),
    (70, 3, "Real-Time Fluids & Fire", "سوائل ونيران لحظية", "Water, smoke and fire behave like in reality.", "الماء والدخان والنار تتصرف كما في الواقع.", "FEB 2034", 10, 2900),
    (71, 0, "Neural Asset Generation", "توليد الأصول العصبي", "AI creates textures and models while you play.", "الذكاء الاصطناعي يصنع الخامات والمجسمات أثناء اللعب.", "OCT 2036", 10, 5600),
    (72, 0, "Holographic Rendering", "العرض الهولوغرافي", "Worlds are projected as holograms into the room.", "تُعرض العوالم كصور هولوغرامية داخل الغرفة.", "APR 2044", 11, 6200),
    (73, 1, "Holo Sound Fields", "حقول الصوت الهولوغرافي", "Sound sources float freely in the room.", "مصادر الصوت تطفو بحرية داخل الغرفة.", "AUG 2045", 11, 6100),
    (74, 2, "Emotional AI Companions", "رفاق ذكاء اصطناعي بمشاعر", "Companions remember you and develop feelings.", "الرفاق يتذكرونك وتتطور لديهم مشاعر.", "JAN 2046", 11, 3300),
    (75, 3, "Molecular Physics", "الفيزياء الجزيئية", "Materials are simulated down to the molecule.", "تُحاكى المواد حتى مستوى الجزيئات.", "JUN 2048", 11, 3300),
    (76, 0, "Neural Image Projection", "الإسقاط العصبي للصورة", "Images are sent straight to the visual cortex.", "تُرسل الصور مباشرة إلى القشرة البصرية في الدماغ.", "MAR 2054", 12, 7000),
    (77, 1, "Neural Audio Link", "الربط الصوتي العصبي", "Sound is perceived without any speakers.", "يُسمع الصوت دون أي سماعات.", "SEP 2055", 12, 6900),
    (78, 2, "Self-Evolving NPC Societies", "مجتمعات شخصيات ذاتية التطور", "Whole NPC societies grow, trade and wage wars.", "مجتمعات كاملة من الشخصيات تنمو وتتاجر وتخوض الحروب.", "FEB 2056", 12, 3700),
    (79, 3, "Living World Simulation", "محاكاة العالم الحي", "The world keeps living while nobody plays.", "العالم يستمر في الحياة حتى عندما لا يلعب أحد.", "JUL 2058", 12, 3700),
    (80, 0, "Bio-Rendering", "العرض الحيوي", "Bio-processors render organic detail in real time.", "المعالجات الحيوية ترسم التفاصيل العضوية لحظياً.", "APR 2066", 14, 7900),
    (81, 2, "Synthetic Consciousness", "الوعي الاصطناعي", "Characters have their own synthetic consciousness.", "الشخصيات تملك وعياً اصطناعياً خاصاً بها.", "OCT 2070", 14, 4200),
    (82, 3, "Quantum Physics Engine", "محرك الفيزياء الكمومية", "Quantum computers simulate whole universes.", "الحواسيب الكمومية تحاكي أكواناً كاملة.", "MAY 2074", 14, 4200),
    (83, 1, "Full-Dive Sound & Touch", "الصوت واللمس بالغمر الكامل", "Hearing and touch are fed directly into the brain.", "يُنقل السمع واللمس مباشرة إلى الدماغ.", "FEB 2083", 15, 8600),
    (84, 0, "Full-Dive Sensory Rendering", "العرض الحسي بالغمر الكامل", "All senses become part of the game world.", "جميع الحواس تصبح جزءاً من عالم اللعبة.", "SEP 2084", 15, 9000),
    (85, 2, "Digital Consciousness Engine", "محرك الوعي الرقمي", "NPCs are indistinguishable from real people.", "لا يمكن تمييز الشخصيات عن البشر الحقيقيين.", "JUN 2090", 16, 4800),
    (86, 3, "Reality Fabric Physics", "فيزياء نسيج الواقع", "The laws of nature themselves become editable.", "قوانين الطبيعة نفسها تصبح قابلة للتعديل.", "NOV 2094", 16, 4800),
]

# ---------------------------------------------------------------- gameplay features
# id, typ (0 gfx 1 sound 2 AI 3 physics 4 gameplay 5 controls 6 multiplayer), name, Arabic,
# desc, Arabic desc, date, (gameplay, graphic, sound, tech), good genres, bad genres, flags
GAMEPLAY = [
    (75, 4, "Battle Pass", "تذكرة المعركة (Battle Pass)", "Seasonal reward tracks keep players coming back.", "مسارات مكافآت موسمية تعيد اللاعبين باستمرار.", "JUN 2018", (300, 0, 0, 100), "<5><8><14><16><18>", "<1><12><15>", ["INTERNET"]),
    (76, 4, "Cross-Progression", "التقدم عبر المنصات", "Progress carries over between all devices.", "ينتقل التقدم بين جميع الأجهزة.", "MAR 2021", (250, 0, 0, 200), "<3><5><8><14><16><17>", "<12><15>", ["INTERNET"]),
    (77, 2, "Adaptive AI Director", "مخرج ذكاء اصطناعي متكيّف", "An AI tunes the challenge to each player.", "ذكاء اصطناعي يضبط التحدي حسب كل لاعب.", "AUG 2032", (450, 0, 0, 350), "<2><5><14><16><18>", "<1><9>", []),
    (78, 4, "Generative Quests", "مهام توليدية", "Endless new quests are written on the fly.", "مهام جديدة لا نهائية تُكتب أثناء اللعب.", "JAN 2035", (550, 0, 0, 300), "<2><3><18>", "<0><1><8><17>", []),
    (79, 6, "Cross-Reality Multiplayer", "لعب جماعي عبر الواقع", "VR, holo and screen players share one world.", "لاعبو الواقع الافتراضي والهولوغرام والشاشات في عالم واحد.", "OCT 2040", (400, 100, 0, 400), "<5><8><13><14><16>", "<12><15>", ["INTERNET", "NEED_GPF 23"]),
    (80, 5, "Full-Body Haptics", "الإحساس اللمسي للجسد كاملاً", "Players feel hits, wind and heat.", "يشعر اللاعب بالضربات والرياح والحرارة.", "MAY 2045", (400, 0, 150, 350), "<5><10><14><16><17>", "<1><9><11><15>", ["NO_MOBILE"]),
    (81, 4, "Holo Spectator Mode", "وضع المشاهدة الهولوغرافي", "Watch matches as holograms on your table.", "شاهد المباريات كصور هولوغرامية على طاولتك.", "SEP 2046", (300, 300, 0, 200), "<4><8><10><13><17>", "<12><15>", ["INTERNET"]),
    (82, 5, "Neural Controls", "التحكم العصبي", "The game is controlled by thoughts alone.", "يتم التحكم في اللعبة بالأفكار فقط.", "FEB 2052", (650, 0, 0, 550), "<4><5><13><14><16>", "<15>", []),
    (83, 2, "Emotion Recognition", "التعرف على المشاعر", "The game reads and reacts to your mood.", "اللعبة تقرأ مزاجك وتتفاعل معه.", "NOV 2055", (550, 0, 100, 450), "<2><3><12><15><18>", "<0><17>", []),
    (84, 4, "Persistent Living World", "عالم حي دائم", "The world changes even while you are offline.", "العالم يتغير حتى عندما تكون غير متصل.", "APR 2065", (750, 200, 0, 650), "<3><4><9><11><18>", "<0><1><10><17>", ["INTERNET"]),
    (85, 4, "Dream Sequences", "مشاهد الأحلام", "Story chapters are played inside dreams.", "تُلعب فصول القصة داخل الأحلام.", "AUG 2075", (700, 400, 200, 300), "<2><3><12><15>", "<8><9><17>", ["NO_MOBILE"]),
    (86, 5, "Full-Dive Mode", "وضع الغمر الكامل", "The player's mind enters the game completely.", "عقل اللاعب يدخل اللعبة بالكامل.", "MAR 2085", (950, 500, 300, 850), "<2><3><5><14><18>", "<0><1>", ["NO_MOBILE", "NO_ARCADE"]),
]

# ---------------------------------------------------------------- hardware parts
# id, typ (0 CPU 1 GPU 2 RAM 3 storage 4 sound 5 cooling 6 media 7 controller 9 screen),
# name, Arabic, date, tech, (res, price, dev), flags
HARDWARE = [
    (172, 0, "32-Core Neural CPU", "معالج عصبي 32 نواة", "MAR 2036", 10, (4800, 46000000, 95000000), []),
    (173, 0, "Photonic CPU", "معالج ضوئي", "JUN 2049", 12, (5600, 56000000, 115000000), []),
    (174, 0, "Bio-Processor", "معالج حيوي", "SEP 2067", 14, (6400, 66000000, 135000000), []),
    (175, 0, "Quantum Mesh CPU", "معالج الشبكة الكمومية", "JAN 2084", 16, (7200, 76000000, 155000000), []),
    (176, 1, "NVIDIA Path-X", "NVIDIA Path-X", "APR 2035", 10, (4600, 50000000, 100000000), []),
    (177, 1, "AMD Holo-RDNA", "AMD Holo-RDNA", "NOV 2043", 11, (5000, 55000000, 110000000), []),
    (178, 1, "Neural Render Core", "نواة العرض العصبي", "FEB 2053", 12, (5600, 60000000, 120000000), []),
    (179, 1, "Photonic GPU", "معالج رسومات ضوئي", "AUG 2061", 13, (6000, 65000000, 130000000), []),
    (180, 1, "Quantum Render Mesh", "شبكة العرض الكمومية", "MAY 2080", 15, (6800, 75000000, 150000000), []),
    (181, 2, "256 Gigabyte", "256 غيغابايت", "MAR 2037", 10, (4000, 28000000, 50000000), []),
    (182, 2, "1 Terabyte", "1 تيرابايت", "JUN 2045", 11, (4400, 32000000, 58000000), []),
    (183, 2, "Holographic Memory 16 TB", "ذاكرة هولوغرافية 16 تيرابايت", "OCT 2058", 13, (5000, 38000000, 68000000), []),
    (184, 2, "DNA Memory", "ذاكرة الحمض النووي", "APR 2078", 15, (6000, 46000000, 82000000), []),
    (185, 3, "128 terabyte solid-state drive", "قرص تخزين صلب 128 تيرابايت", "SEP 2038", 10, (4600, 34000000, 70000000), []),
    (186, 3, "1 petabyte crystal storage", "تخزين كريستالي 1 بيتابايت", "MAR 2055", 12, (5200, 40000000, 82000000), []),
    (187, 3, "DNA storage", "تخزين الحمض النووي", "JUL 2075", 14, (6000, 48000000, 96000000), []),
    (188, 4, "Spatial Audio X", "Spatial Audio X", "FEB 2034", 10, (4200, 28000000, 55000000), []),
    (189, 4, "Holo-Audio Core", "نواة الصوت الهولوغرافي", "AUG 2046", 11, (4600, 32000000, 62000000), []),
    (190, 4, "Neural Audio Chip", "شريحة الصوت العصبي", "MAY 2056", 12, (5200, 36000000, 70000000), []),
    (191, 4, "Full-Dive Sense Chip", "شريحة الحواس للغمر الكامل", "JAN 2084", 15, (6400, 44000000, 86000000), []),
    (192, 5, "Phase-change cooling", "تبريد بتغيّر الطور", "OCT 2034", 10, (5200, 7000000, 17000000), ["ONLY_STATIONARY"]),
    (193, 5, "Cryogenic cooling", "تبريد بالتجميد", "MAR 2052", 12, (5800, 8500000, 20000000), ["ONLY_STATIONARY"]),
    (194, 5, "Superconducting cooling", "تبريد فائق التوصيل", "JUN 2071", 14, (6400, 10000000, 24000000), ["ONLY_STATIONARY"]),
    (195, 6, "Cloud license (no disc)", "ترخيص سحابي (بدون قرص)", "NOV 2032", 10, (3200, 18000000, 36000000), []),
    (196, 6, "Neural download", "التنزيل العصبي", "SEP 2060", 13, (4200, 24000000, 48000000), []),
    (197, 7, "Neural controller", "وحدة تحكم عصبية", "FEB 2045", 11, (5400, 15000000, 29000000), ["ONLY_STATIONARY"]),
    (198, 7, "Neural band", "سوار عصبي", "JUL 2055", 12, (5800, 17000000, 33000000), []),
    (199, 7, "Full-dive interface", "واجهة الغمر الكامل", "MAR 2085", 15, (6800, 21000000, 40000000), ["ONLY_STATIONARY"]),
    (200, 9, "Holographic display", "شاشة هولوغرافية", "JUN 2046", 11, (5400, 28000000, 50000000), ["ONLY_HANDHELD"]),
    (201, 9, "Retina projection", "إسقاط على شبكية العين", "APR 2062", 13, (6000, 32000000, 58000000), ["ONLY_HANDHELD"]),
]

# ---------------------------------------------------------------- console features
# id, name, Arabic, desc, Arabic desc, date, (res, price, dev), quality, flags
HW_FEATURES = [
    (30, "Cloud Streaming", "البث السحابي", "Games run on servers and stream to the console.", "تعمل الألعاب على الخوادم وتُبث إلى الجهاز.", "MAR 2031", (2000, 400000, 180000), 2, ["NEEDINTERNET"]),
    (31, "AI Assistant", "مساعد ذكاء اصطناعي", "A built-in assistant helps players in every game.", "مساعد مدمج يساعد اللاعبين في كل لعبة.", "AUG 2033", (2100, 420000, 190000), 2, []),
    (32, "Biometric Login", "تسجيل دخول بيومتري", "Players sign in with face or fingerprint.", "يسجل اللاعبون الدخول بالوجه أو البصمة.", "JAN 2036", (1900, 380000, 170000), 1, []),
    (33, "Holo Projector", "جهاز إسقاط هولوغرافي", "Projects the game as a hologram into the room.", "يعرض اللعبة كصورة هولوغرامية في الغرفة.", "OCT 2045", (2600, 520000, 240000), 3, ["ONLY_STATIONARY"]),
    (34, "Neural Input", "الإدخال العصبي", "The console reads brain signals as input.", "يقرأ الجهاز إشارات الدماغ كمدخلات.", "MAY 2052", (2900, 580000, 260000), 3, []),
    (35, "Full-Dive Pod Support", "دعم كبسولات الغمر الكامل", "Supports pods for complete immersion.", "يدعم كبسولات الانغماس الكامل.", "FEB 2085", (3600, 700000, 320000), 3, ["ONLY_STATIONARY"]),
]


def _year_month(d):
    m, y = d.split()
    return int(y), MON.index(m)


def platform_blocks(existing_blocks):
    """Text blocks for new platforms + {old id: {field: value}} edits for their predecessors."""
    blocks, edits = [], {}
    known = {int(b["ID"]): b for b in existing_blocks}
    by_id = {p[0]: p for p in PLATFORMS}
    for (pid, name, maker, pub, date, tech, typ, pre, price, dev, units, net, gp, pic) in PLATFORMS:
        lines = [f"[ID]{pid}", f"[NAME EN]{name}", f"[MANUFACTURER EN]{maker}", f"[DATE]{date}"]
        succ = next((p for p in PLATFORMS if p[7] == pid), None)
        if succ:
            y, m = _year_month(succ[4])
            lines.append(f"[DATE END]{MON[m]} {y + LIFE[typ]}")
        lines += [f"[PRICE]{price}", f"[DEV COSTS]{dev}", f"[TECHLEVEL]{tech}", f"[UNITS]{units}",
                  f"[PIC-1]{pic}"]
        lines += [f"[NEED-{i + 1}]{n}" for i, n in enumerate(NEED[typ])]
        lines += [f"[COMPLEX]{COMPLEX[typ]}", f"[INTERNET]{net}", f"[TYP]{typ}"]
        if gp:
            lines.append(f"[GAMEPASS]{gp}")
        # backwards compatible with up to 4 predecessors of the same line
        chain, cur = [], pre
        while cur > 0 and len(chain) < 4 and typ in (1, 2):
            chain.append(cur)
            cur = by_id[cur][7] if cur in by_id else int(known[cur].get("PRE", "-1")) if cur in known else -1
        lines += [f"[BACKCOMP-{i + 1}]{c}" for i, c in enumerate(chain)]
        if pub is not None:
            lines.append(f"[PUB]{pub}")
        lines += [f"[PRE]{pre}", f"[SUC]{succ[0] if succ else -1}", "[END]", ""]
        blocks.append("\n".join(lines))
        if pre in known:  # an original platform gets its successor and an end date
            y, m = _year_month(date)
            edits[pre] = {"SUC": str(pid)}
            if "DATE END" not in known[pre]:
                edits[pre]["DATE END"] = f"{MON[m]} {y + LIFE[typ]}"
    return blocks, edits


def engine_blocks():
    out = []
    for (fid, typ, name, ar, desc, desc_ar, date, tech, pts) in ENGINE:
        y = int(date.split()[1])
        res, price, dev = 2800 + (y - 2029) * 60, 520000 + (y - 2029) * 9000, 250000 + (y - 2029) * 4000
        g = {0: (0, pts, 0, 0), 1: (0, 0, pts, 0), 2: (pts, 0, 0, pts), 3: (pts, 0, 0, pts)}[typ]
        out.append("\n".join([
            f"[ID]{fid}", f"[TYP]{typ}", f"[NAME EN]{name}", f"[NAME AR]{to_game(ar, wrap=0)}",
            f"[DESC EN]{desc}", f"[DESC AR]{to_game(desc_ar)}", f"[DATE]{date}", f"[RES POINTS]{res}",
            f"[PRICE]{price}", f"[DEV COSTS]{dev}", f"[TECHLEVEL]{tech}", "[PIC]",
            f"[GAMEPLAY]{g[0]}", f"[GRAPHIC]{g[1]}", f"[SOUND]{g[2]}", f"[TECH]{g[3]}", ""]))
    return out


def gameplay_blocks():
    out = []
    for (fid, typ, name, ar, desc, desc_ar, date, pts, good, bad, flags) in GAMEPLAY:
        y = int(date.split()[1])
        res, price, dev = 1000 + (y - 2018) * 45, 300000 + (y - 2018) * 9000, 150000 + (y - 2018) * 4500
        lines = [f"[ID]{fid}", f"[TYP]{typ}", f"[NAME EN]{name}", f"[NAME AR]{to_game(ar, wrap=0)}",
                 f"[DESC EN]{desc}", f"[DESC AR]{to_game(desc_ar)}", f"[DATE]{date}", f"[RES POINTS]{res}",
                 f"[PRICE]{price}", f"[DEV COSTS]{dev}", "[PIC]", f"[GAMEPLAY]{pts[0]}",
                 f"[GRAPHIC]{pts[1]}", f"[SOUND]{pts[2]}", f"[TECH]{pts[3]}", f"[GOOD]{good}", f"[BAD]{bad}"]
        for f in flags:
            lines.append(f"[NEED_GPF]{f.split()[1]}" if f.startswith("NEED_GPF") else f"[{f}]")
        out.append("\n".join(lines + [""]))
    return out


def hardware_blocks():
    out = []
    for (hid, typ, name, ar, date, tech, (res, price, dev), flags) in HARDWARE:
        out.append("\n".join([f"[ID]{hid}", f"[TYP]{typ}", f"[NAME EN]{name}",
                              f"[NAME AR]{to_game(ar, wrap=0)}", f"[DATE]{date}", f"[RES POINTS]{res}",
                              f"[PRICE]{price}", f"[DEV COSTS]{dev}", f"[TECHLEVEL]{tech}"]
                             + [f"[{f}]" for f in flags] + [""]))
    return out


def hw_feature_blocks():
    out = []
    for (fid, name, ar, desc, desc_ar, date, (res, price, dev), q, flags) in HW_FEATURES:
        out.append("\n".join([f"[ID]{fid}", f"[NAME EN]{name}", f"[NAME AR]{to_game(ar, wrap=0)}",
                              f"[DESC EN]{desc}", f"[DESC AR]{to_game(desc_ar)}", f"[DATE]{date}",
                              f"[RES POINTS]{res}", f"[PRICE]{price}", f"[DEV COSTS]{dev}"]
                             + [f"[{f}]" for f in flags] + [f"[QUALITY]{q}", ""]))
    return out
