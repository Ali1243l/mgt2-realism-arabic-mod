"""Arabic names for Hardware.txt and HardwareFeatures.txt. The game reads [NAME AR]/[DESC AR]
there but the shipped files have none, so Arabic players saw English."""
import re

from arabic_game import to_game

UNITS = {"byte": "بايت", "bytes": "بايت", "kilobyte": "كيلوبايت", "megabyte": "ميغابايت",
         "gigabyte": "غيغابايت", "terabyte": "تيرابايت"}
MEDIA = {"memory card": "بطاقة ذاكرة", "hard disk": "قرص صلب", "cartridge": "خرطوشة", "CD": "قرص CD",
         "DVD": "قرص DVD", "Blu-ray": "قرص Blu-ray", "Dual-Layer Blu-ray": "قرص Blu-ray ثنائي الطبقة",
         "HD-DVD": "قرص HD-DVD", "Ultra-DVD": "قرص Ultra-DVD"}
SCREENS = {"grayscale": "شاشة رمادية", "color": "شاشة ملونة", "touch": "شاشة لمس",
           "3D touch": "شاشة لمس ثلاثية الأبعاد"}
FIXED = {
    "2x4-Core CPU": "معالج 2×4 نواة", "8-Core Extreme CPU": "معالج 8 نواة Extreme",
    "TV Interface ADP": "واجهة تلفاز ADP", "TV Interface ADP-X": "واجهة تلفاز ADP-X",
    "Micon-SX 100 Chip": "شريحة Micon-SX 100", "Micon-SX 150 Chip": "شريحة Micon-SX 150",
    "No storage option": "بدون خيار تخزين",
    "Air vents": "فتحات تهوية", "Heat Sink": "مشتت حراري", "Small case fan": "مروحة هيكل صغيرة",
    "Small fans for CPU and GPU": "مراوح صغيرة للمعالج وكرت الشاشة",
    "High performance fan for CPU and GPU": "مروحة عالية الأداء للمعالج وكرت الشاشة",
    "Tuned 3-fan system": "نظام 3 مراوح محسّن", "Modern 4-fan system": "نظام 4 مراوح حديث",
    "Water-cooling system": "نظام تبريد مائي", "Liquid-metal cooling system": "نظام تبريد بالمعدن السائل",
}


def hardware_name(en):
    en = en.strip()
    if en in FIXED:
        return FIXED[en]
    m = re.fullmatch(r"([\d.]+) Mhz CPU", en)
    if m:
        return f"معالج {m[1]} ميغاهرتز"
    m = re.fullmatch(r"(\d+)-Core CPU", en)
    if m:
        return f"معالج {m[1]} نواة"
    m = re.fullmatch(r"(\d+) (Bytes|Kilobyte|Megabyte|Gigabyte|Terabyte)", en)
    if m:
        return f"{m[1]} {UNITS[m[2].lower()]}"
    m = re.fullmatch(r"(\d+) (bytes|kilobyte|megabyte|gigabyte) on cartridge", en)
    if m:
        return f"{m[1]} {UNITS[m[2]]} على الخرطوشة"
    m = re.fullmatch(r"(\d+) (kilobyte|megabyte|gigabyte|terabyte) (.+)", en)
    if m and m[3] in MEDIA:
        return f"{MEDIA[m[3]]} {m[1]} {UNITS[m[2]]}"
    m = re.fullmatch(r"(Modern controller|Controller) ([IVX]+)", en)
    if m:
        return ("وحدة تحكم حديثة " if m[1].startswith("Modern") else "وحدة تحكم ") + m[2]
    m = re.fullmatch(r"([\d.]+) inch (grayscale|color|touch|3D touch) screen", en)
    if m:
        return f"{SCREENS[m[2]]} {m[1]} إنش"
    m = re.fullmatch(r"Case for (stationary console|handheld) \[(\d+)\]", en)
    if m:
        return ("هيكل كونسول منزلي" if m[1].startswith("stationary") else "هيكل جهاز محمول") + f" [{m[2]}]"
    return None  # brand names (NVIDIA RSX, Yamaha OXL 500...) stay as in English


# id: (name, description)
FEATURES = {
    0: ("جاهز للإنترنت", "إذا كان الكونسول متصلاً بالإنترنت<br>فالإمكانيات لا حدود لها."),
    1: ("التوافق مع الأجيال السابقة", "تتيح هذه الميزة لعب ألعاب<br>الأجيال الأقدم من أجهزة الكونسول."),
    2: ("اللعب المشترك بين المنصات", "تتيح هذه الميزة لمالكي الأجهزة<br>المختلفة اللعب مع بعضهم."),
    3: ("اللعب الجماعي بالكابل", "بتوصيل عدة أجهزة بكابل واحد<br>يصبح اللعب الجماعي ممكناً."),
    4: ("دعم Bluetooth", "يتيح Bluetooth الاتصال اللاسلكي<br>بمجموعة واسعة من الأجهزة الإضافية."),
    5: ("دعم سماعات الرأس", "سماعات الرأس محبوبة خاصة لدى عشاق اللعب الجماعي<br>وتغني عن رسائل الدردشة النصية."),
    6: ("دعم جهاز التحكم عن بُعد", "جهاز التحكم عن بُعد الإضافي محبوب لدى العملاء<br>الذين يستخدمون الكونسول كمركز ترفيه."),
    7: ("مشغّل الوسائط", "بهذه الميزة يمكن تشغيل<br>الأفلام والموسيقى على الكونسول."),
    8: ("نظام الإنجازات", "تُحفظ الإنجازات المحققة في الألعاب<br>ويمكن للأصدقاء الاطلاع عليها."),
    9: ("وضع توفير الطاقة", "وضع توفير الطاقة يحمي البيئة."),
    10: ("دعم الأقراص الصلبة الخارجية", "يمكن زيادة سعة التخزين<br>بدعم الأقراص الصلبة الخارجية."),
    11: ("دعم بطاقات الذاكرة العادية", "دعم بطاقات الذاكرة العادية يمنح المشترين<br>بديلاً رخيصاً لزيادة سعة التخزين."),
    12: ("دعم لوحة المفاتيح", "يدعم الكونسول بذلك لوحات المفاتيح<br>التجارية بجميع أنواعها."),
    13: ("دعم الماوس", "يدعم الكونسول بذلك أجهزة الماوس<br>التجارية بجميع أنواعها."),
    14: ("دعم عجلة القيادة", "دعم عجلات القيادة محبوب<br>خاصة في ألعاب السباقات."),
    15: ("دعم كاميرا الحركة", "تتيح هذه الميزة دعم كاميرا الحركة."),
    16: ("دعم المسدس الضوئي", "تتيح هذه الميزة دعم المسدسات الضوئية."),
    17: ("دعم بساط الرقص", "تتيح هذه الميزة دعم بساط الرقص."),
    18: ("زر إعادة التشغيل", "يمكن إعادة تشغيل الكونسول<br>بسهولة بزر إعادة التشغيل."),
    19: ("وضع التوربو", "وضع التوربو يرفع أداء<br>الكونسول لفترة قصيرة."),
    20: ("قائمة الأصدقاء", "إدارة الألعاب الجماعية أسهل<br>بكثير مع قائمة الأصدقاء."),
    21: ("ملفات المستخدمين", "بهذه الميزة يمكن لكل مستخدم<br>للكونسول إنشاء ملفه الخاص."),
    22: ("مشاركة العائلة", "بهذه الميزة يمكنك مشاركة<br>ألعابك مع أفراد عائلتك."),
    23: ("البث المباشر", "بهذه الميزة يمكنك بث ألعابك<br>على المواقع الشهيرة دون مشاكل في الأداء."),
    24: ("الاستئناف السريع", "بهذه الميزة يحفظ الكونسول حالة اللعبة الحالية<br>ويمكن استئنافها لاحقاً."),
    25: ("دعم سترة الألعاب", "مع سترة الألعاب مثلاً<br>تشعر بالضربات أثناء لعبة أكشن."),
    26: ("دعم بدلة الألعاب", "مع بدلة الألعاب تنتقل حركتك مباشرة<br>إلى اللعبة وتشعر بالضربات أيضاً."),
    27: ("وضعا الجودة والأداء", "بهذين الوضعين يقرر المستخدم إن كانت الألعاب<br>بأفضل رسومات أو بمعدل إطارات أعلى."),
    28: ("دعم الأجهزة المساعدة", "تُدعم أجهزة إدخال مصممة أساساً<br>للاعبين من ذوي الإعاقات الجسدية."),
    29: ("دعم الشاشات المتعددة", "تتيح هذه الميزة توصيل عدة تلفزيونات<br>أو شاشات بأجهزة الكونسول المنزلية."),
}


def add_arabic(text, kind):
    """Insert [NAME AR] (and [DESC AR] for features) after the English lines of original entries."""
    def fix(block):
        bid = int(re.match(r"\[ID\](\d+)", block)[1])
        if "[NAME AR]" in block:
            return block  # new entries from future.py already have Arabic
        en = re.search(r"^\[NAME EN\](.*)$", block, re.M)[1]
        if kind == "hardware":
            ar = hardware_name(en)
            if ar:
                block = re.sub(r"^(\[NAME EN\].*)$", lambda m: m[1] + "\n[NAME AR]" + to_game(ar, wrap=0),
                               block, count=1, flags=re.M)
        else:
            name, desc = FEATURES[bid]
            block = re.sub(r"^(\[NAME EN\].*)$", lambda m: m[1] + "\n[NAME AR]" + to_game(name, wrap=0),
                           block, count=1, flags=re.M)
            block = re.sub(r"^(\[DESC EN\].*)$", lambda m: m[1] + "\n[DESC AR]" + to_game(desc),
                           block, count=1, flags=re.M)
        return block
    head, *rest = re.split(r"\n(?=\[ID\])", text)
    return "\n".join([head] + [fix(b) for b in rest])
