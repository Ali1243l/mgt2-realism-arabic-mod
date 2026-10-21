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
