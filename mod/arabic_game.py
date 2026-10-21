"""Convert normal (logical) Arabic into the pre-shaped, visually reversed form MGT2 reads.

The game renders strictly left-to-right, so Text\\AR stores Arabic already shaped
(presentation forms) and reversed. Latin words, digits, rich-text tags and <NAME>-style
placeholders must stay readable left-to-right.
"""
import re
import unicodedata

import arabic_reshaper
from bidi import get_display

_reshaper = arabic_reshaper.ArabicReshaper(configuration={
    "delete_harakat": False,
    "support_ligatures": True,
})

# Formatting tags (<b>, </color>, <size=20>...) vs. placeholders (<NAME>, <NUM2>, <7>, [Research]...)
_TOKEN = re.compile(r"<[^<>]{1,30}>")
# Number expressions stay one left-to-right unit like the shipped text: +3%, $1.000.000, +<NUM>%
_NUMEXPR = r"[+\-]?\$?(?:\d[\d.,]*\d|\d|<NUM\d?>)%?"
_UNIT = re.compile(f"{_NUMEXPR}|<[^<>]{{1,30}}>")
_FORMAT = re.compile(r"</?(b|i|color|size)(=[^<>]*)?>", re.I)
_FMT_BASE = 0x2500  # box drawing: bidi class ON, not mirrored
_PH_BASE = 0xE000   # private use: bidi class L, so placeholders keep LTR order with neighbours


_MIRROR = {"(": ")", ")": "(", "[": "]", "]": "[", "{": "}", "}": "{", "«": "»", "»": "«"}
_LTR = re.compile(r"[A-Za-z0-9-]")
_STRONG = re.compile(r"[A-Za-z0-9-؀-ۿﭐ-﷿ﹰ-﻿]")


def _mirror(visual):
    """python-bidi 0.6 does not mirror brackets: flip any bracket not inside a Latin run."""
    chars = list(visual)
    for i, c in enumerate(chars):
        if c not in _MIRROR:
            continue
        left = next((x for x in reversed(chars[:i]) if _STRONG.match(x)), "")
        right = next((x for x in chars[i + 1:] if _STRONG.match(x)), "")
        if not (_LTR.match(left) and _LTR.match(right)):
            chars[i] = _MIRROR[c]
    return "".join(chars)


def _segment(seg):
    tokens, pairs, stack, out = [], {}, [], []
    pos = 0
    for m in _UNIT.finditer(seg):
        out.append(seg[pos:m.start()])
        tok = m.group(0)
        fm = _FORMAT.fullmatch(tok)
        idx = len(tokens)
        tokens.append(tok)
        if fm:
            marker = chr(_FMT_BASE + idx)
            if tok.startswith("</"):
                if stack:
                    pairs[stack.pop()] = idx
            else:
                stack.append(idx)
        else:
            marker = chr(_PH_BASE + idx)
        out.append(marker)
        pos = m.end()
    out.append(seg[pos:])
    visual = _mirror(get_display(_reshaper.reshape("".join(out)), base_dir="R"))
    text = {i: t for i, t in enumerate(tokens)}
    for o, c in pairs.items():  # an RTL run puts the closing tag first: swap them back
        mo, mc = chr(_FMT_BASE + o), chr(_FMT_BASE + c)
        if visual.index(mc) < visual.index(mo):
            text[o], text[c] = text[c], text[o]
    for i in range(len(tokens)):
        base = _FMT_BASE if _FORMAT.fullmatch(tokens[i]) else _PH_BASE
        visual = visual.replace(chr(base + i), text[i])
    return visual


WRAP = 65  # shipped Arabic keeps ~90% of lines under this; Unity's own wrap would reverse line order


def _wrap(seg, width=WRAP):
    """Split a long logical segment into lines of at most `width` visible characters."""
    if len(_TOKEN.sub("", seg)) <= width:
        return [seg]
    lines, cur = [], ""
    for word in seg.split(" "):
        if cur and len(_TOKEN.sub("", cur + " " + word)) > width:
            lines.append(cur)
            cur = word
        else:
            cur = f"{cur} {word}" if cur else word
    return lines + [cur]


def _balance(lines):
    """Close format tags at the end of each line and reopen them on the next, so every
    visual line can be reversed on its own (tags may span <br> in the source)."""
    out, open_tags = [], []
    for line in lines:
        prefix = "".join(open_tags)
        for tag in _FORMAT.finditer(line):
            if tag.group(0).startswith("</"):
                if open_tags:
                    open_tags.pop()
            else:
                open_tags.append(tag.group(0))
        suffix = "".join("</%s>" % re.match(r"<(\w+)", t).group(1) for t in reversed(open_tags))
        out.append(re.sub(r"<(b|i|color|size)(=[^<>]*)?></\1>", "", prefix + line + suffix))
    return out


def to_game(text, fields=False, wrap=WRAP):
    """Logical Arabic -> game format. Lines are split on <br> (and ';' when fields=True)."""
    if fields:
        return ";".join(to_game(f, wrap=wrap) for f in text.split(";"))
    lines = [line for s in text.split("<br>") for line in _wrap(s, wrap or 10**9)]
    return "<br>".join(_segment(line) for line in _balance(lines))


def from_game_simple(visual):
    """Inverse for pure-Arabic segments only (used by the self-test)."""
    mirror = {"(": ")", ")": "(", "[": "]", "]": "[", "«": "»", "»": "«"}
    out = []
    for c in reversed(visual):
        if "ﭐ" <= c <= "﷿" or "ﹰ" <= c <= "﻿":
            out.append(unicodedata.normalize("NFKC", c))
        else:
            out.append(mirror.get(c, c))
    return "".join(out)


if __name__ == "__main__":
    import pathlib, sys
    sys.stdout.reconfigure(encoding="utf-8")
    # 1) Hand-checked cases with tags, placeholders, Latin and digits.
    cases = {
        "اكتملت حملة تسويقية لـ <NAME1>": "<NAME1> ـﻟ ﺔﻴﻘﻳﻮﺴﺗ ﺔﻠﻤﺣ ﺖﻠﻤﺘﻛﺍ",
        "تعمل آلة القهوة 17 ساعة": "ﺔﻋﺎﺳ 17 ﺓﻮﻬﻘﻟﺍ ﺔﻟﺁ ﻞﻤﻌﺗ",
        "اضغط <color=blue>CTRL+LMB</color> للتحديد": "ﺪﻳﺪﺤﺘﻠﻟ <color=blue>CTRL+LMB</color> ﻂﻐﺿﺍ",
        "+<NUM>% من الألعاب": "ﺏﺎﻌﻟﻷﺍ ﻦﻣ +<NUM>%",
        "مكافأة <color=blue>+3%</color> على المبيعات": "ﺕﺎﻌﻴﺒﻤﻟﺍ ﻰﻠﻋ <color=blue>+3%</color> ﺓﺄﻓﺎﻜﻣ",
        "تكلفة $1.000.000 تقريباً": "ًﺎﺒﻳﺮﻘﺗ $1.000.000 ﺔﻔﻠﻜﺗ",
        "ميزة \"Online Multiplayer\" مطلوبة": "ﺔﺑﻮﻠﻄﻣ \"Online Multiplayer\" ﺓﺰﻴﻣ",
        "<b>غرفة</b>": "<b>ﺔﻓﺮﻏ</b>",
        "غرفة (أ) و (MMO)": "(MMO) ﻭ (ﺃ) ﺔﻓﺮﻏ",
        "Mad Games (Tycoon) 2": "Mad Games (Tycoon) 2",
        "تم بيع <color=blue><NUM> <NAME></color>.": ".<color=blue><NUM> <NAME></color> ﻊﻴﺑ ﻢﺗ",
    }
    bad = 0
    # Wrapping: every visual line is short and has balanced colour tags, opening tag first.
    long = ("اضغط على <color=blue>زر الحفظ الموجود في أعلى الشاشة لكي تحفظ تقدمك في اللعبة "
            "قبل أن تغلقها</color> ثم تابع اللعب كالمعتاد مع فريقك")
    for vis in to_game(long).split("<br>"):
        plain = _TOKEN.sub("", vis)
        if len(plain) > WRAP or vis.count("<color") != vis.count("</color>") or \
                ("<color" in vis and vis.index("<color") > vis.index("</color>")):
            bad += 1
            print("WRAP FAIL", vis)
    # Tags spanning <br>: each visual line gets its own balanced pair.
    span = to_game("نص:<color=green><br>▪ أخضر<br></color><color=red>▪ أحمر</color>")
    if span.split("<br>")[1:] != ["<color=green>ﺮﻀﺧﺃ ▪</color>", "<color=red>ﺮﻤﺣﺃ ▪</color>"]:
        bad += 1
        print("SPAN FAIL", span)
    for src, want in cases.items():
        got = to_game(src)
        if got != want:
            bad += 1
            print("CASE FAIL\n  src ", src, "\n  got ", got, "\n  want", want)
    # 2) Round-trip every existing pure-Arabic segment in the shipped files.
    root = pathlib.Path(__file__).resolve().parent.parent / "backup" / "Extern" / "Text" / "AR"
    # Shipped files write isolated letters either as base letters or as isolated forms;
    # both render the same, so compare after folding isolated forms to base letters.
    fold = lambda s: "".join(
        unicodedata.normalize("NFKC", c) if unicodedata.name(c, "").endswith("ISOLATED FORM") else c
        for c in s)
    total = same = 0
    shipped_chars, made_chars = set(), set()
    for p in root.glob("*.txt"):
        for line in p.read_text(encoding="utf-8-sig").splitlines():
            shipped_chars.update(line)
            for seg in re.split(r"<br>|;", line):
                if not seg.strip() or re.search(r"[A-Za-z0-9<>\[\]]", seg):
                    continue
                total += 1
                got = to_game(from_game_simple(seg), wrap=0)
                made_chars.update(got)
                same += fold(got) == fold(seg)
    # Every glyph we emit must already ship in the game's Arabic files (font coverage proxy).
    missing = sorted(c for c in made_chars - shipped_chars if ord(c) > 0x600)
    print(f"cases failed: {bad}/{len(cases)}; round-trip identical: {same}/{total}; "
          f"glyphs not in shipped files: {[hex(ord(c)) for c in missing]}")
