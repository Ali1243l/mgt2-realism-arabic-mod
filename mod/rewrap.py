"""Re-wrap long lines of the shipped Arabic translation without changing their wording.

The game wraps long lines itself, which puts the end of a reversed Arabic sentence on the
first line. We recover the logical text from the stored (visual) text, prove the recovery by
converting it back to exactly the stored text, and write it as a patch so build.py re-wraps it.
Review/FanLetter are skipped: the game joins those sentences into one paragraph.

    python -P rewrap.py   ->  ar/Text_rewrap.tsv, ar/Tutorial_rewrap.tsv
"""
import pathlib, re, sys, unicodedata

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import build  # noqa: E402
from arabic_game import _FORMAT, _MIRROR, to_game  # noqa: E402

HERE = pathlib.Path(__file__).resolve().parent
LONG = 70
LATIN = r"[A-Za-z0-9$%+\-][A-Za-z0-9$%+\-.,'’&:/!?]*"
UNIT = re.compile(rf"<[^<>]{{1,30}}>|{LATIN}(?: {LATIN})*|.", re.S)
TOK = re.compile(r"<[^<>]{1,30}>")


def fold(s):
    return "".join(unicodedata.normalize("NFKC", c) if unicodedata.name(c, "").endswith("ISOLATED FORM") else c
                   for c in s)


def to_logical(visual):
    units = UNIT.findall(visual)[::-1]
    # visual tag pairs come out closing-first after reversal: swap their texts back
    stack = []
    for i, u in enumerate(units):
        if _FORMAT.fullmatch(u):
            if u.startswith("</"):
                stack.append(i)
            elif stack:
                j = stack.pop()
                units[i], units[j] = units[j], units[i]
    out = []
    for i, u in enumerate(units):
        if len(u) == 1 and ("ﭐ" <= u <= "﷿" or "ﹰ" <= u <= "﻿"):
            out.append(unicodedata.normalize("NFKC", u))
        elif u in _MIRROR:
            left = next((x for x in reversed(units[:i]) if x.strip()), "")
            right = next((x for x in units[i + 1:] if x.strip()), "")
            latin = lambda x: bool(re.match(r"[A-Za-z0-9]", x))
            out.append(u if latin(left[-1:]) and latin(right[:1]) else _MIRROR[u])
        else:
            out.append(u)
    return "".join(out)


def main():
    files = {}
    build.build_arabic(files)
    patched = {}
    for f in (HERE / "ar").glob("*.tsv"):
        if "rewrap" in f.stem or f.stem in ("align", "glossary"):
            continue
        for r in build.tsv(f):
            patched.setdefault(f.stem.split("_")[0], set()).add(int(r["line"]))
    for stem in ("Text", "Tutorial"):
        rows, failed = [], []
        lines = files[f"Text/AR/{stem}_AR.txt"].lstrip("﻿").split("\n")
        for n, line in enumerate(lines, 1):
            if n in patched.get(stem, ()) or not any(
                    len(TOK.sub("", s)) > LONG and re.search("[ﹰ-﻿]", s) for s in line.split("<br>")):
                continue
            segs = [to_logical(s) for s in line.split("<br>")]
            # proof: converting back (without wrapping) must give the stored text
            if fold("<br>".join(to_game(s, wrap=0) for s in segs)) == fold(line):
                rows.append(f"{n}\t" + "<br>".join(segs))
            else:
                failed.append(n)
        (HERE / f"ar/{stem}_rewrap.tsv").write_text("line\tar\n" + "\n".join(rows) + "\n", encoding="utf-8")
        print(f"{stem}: re-wrapped {len(rows)}, could not prove {len(failed)}: {failed}")


if __name__ == "__main__":
    main()
