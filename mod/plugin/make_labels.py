"""Generate Labels.cs for a plugin: English + Arabic (already shaped and reversed for the game's
left-to-right text renderer, via arabic_game.to_game). Re-run after editing LABELS.

    python -P make_labels.py LiveOps
"""
import pathlib, sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
from arabic_game import to_game  # noqa: E402

LABELS = {
    "LiveOps": {
        # in-app page of the development wizard
        "BattlePass": ("Seasonal Battle Pass", "تذكرة المعركة الموسمية"),
        "BattlePassTip": ("A paid season pass. Earns while seasons stay fresh (new season via F2P updates); a stale pass annoys players.",
                          "تذكرة موسمية مدفوعة. تربح ما دام الموسم جديداً (موسم جديد عبر تحديثات F2P)، والتذكرة القديمة تزعج اللاعبين."),
        "ModelLabel": ("Model:", "النموذج:"),
        "ModelNone": ("no purchases", "بدون مشتريات"),
        "ModelFair": ("fair cosmetics", "تجميلي عادل"),
        "ModelMixed": ("mixed", "مختلط"),
        "ModelP2W": ("pay-to-win", "الدفع للفوز"),
        "ModelPlusPass": ("+ pass", "+ تذكرة"),
        # F2P update menu
        "Campaign": ("Live campaign (optional)", "الحملة الحية (اختياري)"),
        "TypeNone": ("None", "بدون"),
        "TypeSeason": ("Season", "موسم"),
        "TypeEvent": ("Event", "حدث"),
        "TypeCollab": ("Collab", "تعاون"),
        "TypeRelaunch": ("Relaunch", "إعادة إطلاق"),
        "InfoNone": ("A regular content update.", "تحديث محتوى عادي."),
        "InfoSeason": ("New season themed on the topic. Renews the Battle Pass.", "موسم جديد بطابع الموضوع المختار، ويجدد تذكرة المعركة."),
        "InfoEvent": ("Limited-time event for 4 weeks. Strongest when it matches the real calendar.", "حدث محدود لمدة 4 أسابيع، ويكون أقوى عندما يتزامن مع موعده الحقيقي."),
        "InfoCollab": ("Crossover with a licensed brand for 6 weeks. Popular brands that fit the genre bring the most players.", "تعاون مع علامة مرخصة لمدة 6 أسابيع. العلامات الشهيرة المناسبة للنوع تجلب لاعبين أكثر."),
        "InfoRelaunch": ("Version 2.0: a full overhaul that gives the game a fresh start. Once per game, after 2 years.", "الإصدار 2.0: إعادة بناء شاملة تمنح اللعبة بداية جديدة. مرة واحدة بعد سنتين في السوق."),
        "RelaunchUsed": ("Relaunch already used or game too young.", "إعادة الإطلاق مستخدمة أو اللعبة حديثة."),
        "InSeason": ("in season now", "في موعده الآن"),
        "OffSeason": ("off-season", "خارج موعده"),
        "Fits": ("fits the genre", "مناسب للنوع"),
        "Misfit": ("does not fit the genre", "غير مناسب للنوع"),
        "Extra": ("Extra cost", "تكلفة إضافية"),
        "Occ0": ("Ramadan & Eid", "رمضان والعيد"),
        "Occ1": ("Halloween", "الهالوين"),
        "Occ2": ("Winter holidays", "عطلة الشتاء"),
        "Occ3": ("Lunar New Year", "رأس السنة القمرية"),
        "Occ4": ("Summer festival", "مهرجان الصيف"),
        "Occ5": ("Game anniversary", "ذكرى إطلاق اللعبة"),
        "Occ6": ("Esports world cup", "كأس العالم للرياضات الإلكترونية"),
        "Occ7": ("Black Friday", "الجمعة السوداء"),
        "Lic0": ("Movie", "فيلم"),
        "Lic1": ("Book", "كتاب"),
        "Lic2": ("Sport", "رياضة"),
        "Lic3": ("Toy", "ألعاب أطفال"),
        "Lic4": ("Board game", "لعبة لوحية"),
        "Lic5": ("Comic", "قصص مصورة"),
        # statistics window
        "Section": ("Live service", "الخدمة الحية"),
        "Players": ("Players", "اللاعبون"),
        "Dau": ("Daily active (DAU)", "النشطون يومياً"),
        "Mau": ("Monthly active (MAU)", "النشطون شهرياً"),
        "Retention": ("Retention 30 / 60 / 90 days", "الاحتفاظ بعد 30 / 60 / 90 يوماً"),
        "Sentiment": ("Community sentiment", "رضا المجتمع"),
        "Monetization": ("Monetization", "نموذج الربح"),
        "Pass": ("Battle Pass", "تذكرة المعركة"),
        "PassFresh": ("fresh", "جديدة"),
        "PassStale": ("stale", "قديمة"),
        "PassNone": ("none", "لا يوجد"),
        "Season": ("Season", "الموسم"),
        "WeeksAgo": ("weeks ago", "أسابيع مضت"),
        "Event": ("Event", "الحدث"),
        "Collab": ("Collab", "التعاون"),
        "WeeksLeft": ("weeks left", "أسابيع متبقية"),
        "NoneShort": ("-", "-"),
        "PassRevenue": ("Battle Pass revenue / week", "إيرادات التذكرة أسبوعياً"),
        "History": ("Players, last 26 weeks", "اللاعبون خلال آخر 26 أسبوعاً"),
        "Relaunched": ("Relaunched (2.0)", "أعيد إطلاقها (2.0)"),
        # news
        "NewsSeason": ("Season started", "بدأ موسم جديد"),
        "NewsEvent": ("Live event started", "انطلق حدث محدود"),
        "NewsCollab": ("Collab launched", "انطلق التعاون"),
        "NewsRelaunch": ("Version 2.0 is live", "انطلق الإصدار 2.0"),
        "NewsServers": ("Servers are full: players are angry", "الخوادم ممتلئة: اللاعبون غاضبون"),
        "NewsStale": ("The Battle Pass is stale", "تذكرة المعركة قديمة"),
    },
    "StockMarket": {
        "DropdownButton": ("Stock Market", "سوق الأسهم"),
        "Title": ("Stock Market & Corporate Control", "سوق الأسهم والسيطرة المالية"),
        "TabMarket": ("Market & Competitors", "السوق والمنافسون"),
        "TabStudio": ("My Studio & Board (IPO)", "استوديو ومجلس الإدارة (الاكتتاب)"),
        "PortfolioVal": ("Portfolio Value:", "قيمة المحفظة:"),
        "IndexVal": ("Market Index:", "مؤشر السوق:"),
        "StudioVal": ("Studio Valuation:", "قيمة الاستوديو:"),
        "Cash": ("Cash:", "السيولة:"),
        "SharePrice": ("Share Price:", "سعر السهم:"),
        "Shares": ("Shares:", "الأسهم:"),
        "Ownership": ("Ownership:", "نسبة الملكية:"),
        "Float": ("Public Float:", "الأسهم الحرة:"),
        "IpoTitle": ("Initial Public Offering (IPO)", "الطرح العام الأولي (الاكتتاب)"),
        "IpoDesc": ("Sell a minority stake of your studio to public investors to raise expansion capital. Note: The Board of Directors will set quarterly revenue targets.", "اطرح حصة أقلية من أسهم الاستوديو للمستثمرين لجمع سيولة توسعية. تنبيه: سيفرض مجلس الإدارة أهدافاً ربع سنوية."),
        "OfferingSize": ("Offering Size:", "حجم الطرح:"),
        "GrossCapital": ("Gross Capital Raised:", "إجمالي رأس المال المجموع:"),
        "UnderwritingFee": ("Underwriting Fee (3%):", "رسوم البنك ومدير الاكتتاب (3%):"),
        "NetProceeds": ("Net Cash Inflow:", "صافي السيولة النقدية:"),
        "LaunchIpo": ("Launch IPO & Issue Shares", "بدء الاكتتاب وإدراج الشركة"),
        "BoardTitle": ("Board of Directors & Governance", "مجلس الإدارة وحوكمة الشركة"),
        "BoardSentiment": ("Board Sentiment:", "مزاج مجلس الإدارة:"),
        "SentimentSatisfied": ("Satisfied", "راضٍ ومطمئن"),
        "SentimentCautious": ("Cautious", "حذر ومترقب"),
        "SentimentDemanding": ("Demanding", "متطلب وقلق"),
        "SentimentHostile": ("Hostile", "عدائي ومتمرد"),
        "Discontent": ("Discontent:", "مستوى الاستياء:"),
        "Confidence": ("Investor Confidence:", "ثقة المستثمرين:"),
        "QuarterlyTarget": ("Quarterly Revenue Target:", "هدف إيرادات الربع:"),
        "CurrentRevenue": ("Current Quarter Revenue:", "إيرادات الربع الحالي:"),
        "TargetProgress": ("Target Progress:", "التقدم نحو الهدف:"),
        "DividendsTitle": ("Dividends & Capital Return", "توزيعات الأرباح على المساهمين"),
        "RecommendedDiv": ("Recommended Dividend:", "التوزيع المقترح:"),
        "PayDividend": ("Pay Dividend", "صرف أرباح المساهمين"),
        "EmergencyDividend": ("Emergency Dividend (Calm Board)", "توزيع طارئ (لتهدئة المجلس)"),
        "SpendingRestricted": ("WARNING: Spending cap active! Hostile board restricts capital.", "تحذير: فرض قيود على الإنفاق! مجلس الإدارة العدائي يمنع الصرف."),
        "Competitors": ("Listed Competitor Studios", "شركات الألعاب المدرجة"),
        "Buy5": ("Buy 5%", "شراء 5%"),
        "Sell5": ("Sell 5%", "بيع 5%"),
        "HostileTakeover": ("Hostile Takeover", "استحواذ عدائي"),
        "Subsidiary": ("Absorb Subsidiary", "ضم كتابعة بالكامل"),
        "NoCompetitors": ("No public competitors found on the market.", "لا توجد استوديوهات منافسة مدرجة حالياً."),
        "Valuation": ("Valuation:", "القيمة:"),
        "Owned": ("Owned:", "المملوك:"),
        "Close": ("Close", "إغلاق"),
        "Refresh": ("Refresh", "تحديث"),
        "TargetMetNotice": ("Quarterly Target Met! The Board of Directors is pleased with studio performance.", "تم تحقيق هدف الربع السنوي بنجاح! مجلس الإدارة راضٍ عن أداء الاستوديو."),
        "TargetMissedNotice": ("Quarterly Target Missed! The Board of Directors expresses deep dissatisfaction.", "فشل تحقيق هدف الربع السنوي! مجلس الإدارة يعرب عن قلقه واستيائه الشديد."),
        "DividendPaidNotice": ("Dividend payment completed. Investor sentiment and confidence boosted.", "تم صرف توزيعات الأرباح بنجاح. تحسنت معنويات وثقة المستثمرين."),
        "IpoSuccessNotice": ("IPO completed! Company shares are now actively traded on the stock exchange.", "تم الاكتتاب بنجاح! أسهم الاستوديو الآن متداولة في البورصة."),
        "MajorityControl": ("Majority Control (51%)", "السيطرة الإدارية للأغلبية (51%)"),
        "SecretIntel": ("Secret Project Intel (25%)", "معلومات المشروع السري (25%)"),
        "SecretProjectTitle": ("Project in Development:", "المشروع قيد التطوير:"),
        "NoActiveProject": ("No project currently in development.", "لا يوجد مشروع قيد التطوير حالياً."),
        "MandatePublishing": ("Mandate Exclusive Publishing", "فرض النشر الحصري عبر شركتك"),
        "MandateGenre": ("Mandate Game Genre", "تحديد نوع الألعاب الإجباري"),
        "AnyGenre": ("Free Choice (Studio Choice)", "اختيار حر (قرار الاستوديو)"),
        "PoisonPillAlert": ("Takeover Defense! Board activated Poison Pill to dilute shares!", "تنبيه دفاعي! مجلس الإدارة فعل حبة السم لعرقلة الاستحواذ!"),
        "AbsorbSuccess": ("Full Acquisition! Studio absorbed as official subsidiary.", "استحواذ كامل! تم ضم الاستوديو كشركة تابعة رسمية."),
        "ApplyDirectives": ("Apply Directives", "تطبيق التوجيهات الإدارية"),
        "DirectivesApplied": ("Directives successfully issued to studio management.", "تم إصدار التوجيهات الإدارية لإدارة الاستوديو بنجاح."),
    },
}


def cs_string(s):
    return '"' + "".join(c if 32 <= ord(c) < 127 and c not in '"\\' else "\\u%04x" % ord(c) for c in s) + '"'


def main(name):
    lines = [
        "// Generated by mod/plugin/make_labels.py: edit LABELS there, not here.",
        "namespace MGT2Mod",
        "{",
        f"    internal static class {name}Labels",
        "    {",
        "        internal static bool Arabic;",
    ]
    for key, (en, ar) in LABELS[name].items():
        lines.append(f"        internal static string {key} => Arabic ? {cs_string(to_game(ar, wrap=0))} : {cs_string(en)};")
    lines += ["    }", "}", ""]
    out = pathlib.Path(__file__).resolve().parent / name / "Labels.cs"
    out.write_text("\n".join(lines), encoding="utf-8")
    print("wrote", out)


if __name__ == "__main__":
    main(sys.argv[1])
