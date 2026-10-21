"""Apply the Realism & Arabic mod to the game.

Always builds from the untouched originals in ../backup, so it is safe to re-run
(e.g. after a Steam update: delete ../backup, re-run with --backup, then re-run).

    python -P build.py            # build into the game folder (with self-checks)
    python -P build.py --check    # build in memory and run the checks only
    python -P build.py --restore  # put the original files back
    python -P build.py --backup   # copy the game's current files into ../backup (only if missing)
"""
import csv, os, pathlib, re, shutil, sys, unicodedata

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from arabic_game import to_game  # noqa: E402
import future  # noqa: E402
import hardware_ar  # noqa: E402

HERE = pathlib.Path(__file__).resolve().parent
BACKUP = HERE.parent / "backup" / "Extern"
# other machines: set the GameDir environment variable (same one the plugin builds use)
GAME = pathlib.Path(os.environ.get("GameDir", r"E:\Steam\steamapps\common\Mad Games Tycoon 2")) / "Mad Games Tycoon 2_Data" / "Extern"
PARTS = ["Text", "CompanyLogos", "Icons_Platforms"]
FIELD_FILES = {"Achivements", "Objects"}  # "name;description" lines: convert each field
MONTHS = "JAN FEB MAR APR MAY JUN JUL AUG SEP OCT NOV DEC".split()


def tsv(path):
    with open(path, encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f, delimiter="\t", quoting=csv.QUOTE_NONE))


def read_text(path):
    raw = path.read_bytes()
    text = raw.decode("utf-16" if raw[:2] in (b"\xff\xfe", b"\xfe\xff") else "utf-8-sig")
    return text.replace("\r\n", "\n")  # game files are CRLF; written back as CRLF


def month_index(d):
    m, y = d.split()
    return int(y) * 12 + MONTHS.index(m)


# ---------- data files ----------
def patch_blocks(text, table, fields):
    """Rewrite [KEY] lines inside each [ID]n block. fields(row) -> {key_regex: value}."""
    rows = {r["id"]: r for r in table}
    out, cur = [], None
    for line in text.split("\n"):
        m = re.match(r"\[ID\](\d+)", line)
        if m:
            cur = rows.get(m.group(1))
        elif cur:
            for key, val in fields(cur).items():
                km = re.match(r"\[(%s)\]" % key, line)
                if km and val:
                    line = f"[{km.group(1)}]{val}"
                    break
        out.append(line)
    return "\n".join(out)


def blocks(text):
    return [dict(re.findall(r"^\[([A-Z0-9_ \-]+)\](.*)$", b, re.M)) for b in re.split(r"\n(?=\[ID\])", text)[1:]]


def build_data(files):
    pub_txt = read_text(BACKUP / "Text/DATA/Publisher.txt")
    pub_txt = patch_blocks(pub_txt, tsv(HERE / "data/publishers.tsv"),
                           lambda r: {r"NAME [A-Z]{2}": r["name"], "COUNTRY": r["country"]})
    plat_txt = read_text(BACKUP / "Text/DATA/Platforms.txt")
    plat_txt = patch_blocks(plat_txt, tsv(HERE / "data/platforms.tsv"),
                            lambda r: {r"NAME [A-Z]{2}": r["name"],
                                       r"MANUFACTURER [A-Z]{2}": r["manufacturer"],
                                       "DATE": r["date"]})
    errors = []
    texts = {"Publisher.txt": pub_txt}

    # parody names elsewhere (developer legends, hardware parts): whole-word replacements
    by_file = {}
    for r in tsv(HERE / "data/renames.tsv"):
        by_file.setdefault(r["file"], []).append(r)
    for name, rows in by_file.items():
        text = read_text(BACKUP / "Text/DATA" / name)
        for r in rows:
            rx = re.compile(r"(?<![A-Za-z])%s(?![A-Za-z])" % re.escape(r["old"]))
            text, n = rx.subn(r["new"], text)
            if not n:  # typo in the table or the game changed the name
                errors.append(f"{name}: {r['old']!r} not found")
        texts[name] = text

    # content for 2030-2100 (future.py), appended before [EOF]
    new_plats, edits = future.platform_blocks(blocks(plat_txt))
    for pid, fields in edits.items():
        for key, val in fields.items():
            block = re.search(r"\[ID\]%d\n.*?\[END\]" % pid, plat_txt, re.S)
            body = block.group(0)
            if f"[{key}]" in body:
                body = re.sub(r"^\[%s\].*$" % re.escape(key), f"[{key}]{val}", body, flags=re.M)
            else:  # only DATE END can be missing: it follows [DATE]
                body = re.sub(r"^(\[DATE\].*)$", r"\1\n[%s]%s" % (key, val), body, count=1, flags=re.M)
            plat_txt = plat_txt[:block.start()] + body + plat_txt[block.end():]
    texts["Platforms.txt"] = plat_txt
    for name, new in (("Platforms.txt", new_plats), ("EngineFeatures.txt", future.engine_blocks()),
                      ("GameplayFeatures.txt", future.gameplay_blocks()),
                      ("Hardware.txt", future.hardware_blocks()),
                      ("HardwareFeatures.txt", future.hw_feature_blocks())):
        text = texts.get(name) or read_text(BACKUP / "Text/DATA" / name)
        head, eof, tail = text.rpartition("[EOF]")
        texts[name] = head.rstrip("\n") + "\n\n" + "\n".join(new) + "\n" + eof + tail
        # the game indexes arrays by ID: IDs must run 0..n-1 (platforms 1..n) without gaps
        ids = [int(b["ID"]) for b in blocks(texts[name])]
        first = 1 if name == "Platforms.txt" else 0
        if sorted(ids) != list(range(first, first + len(ids))):
            errors.append(f"{name}: IDs not contiguous after additions")
    texts["Hardware.txt"] = hardware_ar.add_arabic(texts["Hardware.txt"], "hardware")
    texts["HardwareFeatures.txt"] = hardware_ar.add_arabic(texts["HardwareFeatures.txt"], "features")
    for name, text in texts.items():
        files[f"Text/DATA/{name}"] = "﻿" + text  # originals carry a UTF-8 BOM
    plat_txt = texts["Platforms.txt"]

    # self-checks
    pubs = {b["ID"]: b["NAME EN"] for b in blocks(pub_txt)}
    plats = {b["ID"]: b for b in blocks(plat_txt)}
    for b in plats.values():
        if b.get("PUB") and b["MANUFACTURER EN"] != pubs[b["PUB"]]:
            errors.append(f"platform {b['ID']}: manufacturer {b['MANUFACTURER EN']!r} != publisher {pubs[b['PUB']]!r}")
        if b.get("DATE END") and month_index(b["DATE"]) >= month_index(b["DATE END"]):
            errors.append(f"platform {b['ID']}: DATE {b['DATE']} not before DATE END {b['DATE END']}")
        pre = plats.get(b.get("PRE", "-1"))
        if pre and month_index(pre["DATE"]) > month_index(b["DATE"]):
            errors.append(f"platform {b['ID']}: predecessor {pre['ID']} is newer ({pre['DATE']} > {b['DATE']})")
    names = [b["NAME EN"] for b in plats.values()]
    errors += [f"duplicate platform name {n!r}" for n in set(names) if names.count(n) > 1]
    for b in plats.values():
        suc = plats.get(b.get("SUC", "-1"))
        if suc and int(suc["ID"]) >= 134 and suc.get("PRE") != b["ID"]:  # new chains only
            errors.append(f"platform {b['ID']}: successor {suc['ID']} does not point back")
    return errors


# ---------- Arabic text ----------
_AR = r"[؀-ۿﭐ-﷿ﹰ-﻿]"


def glossary_rules():
    """Whole-word replacements done directly on the shipped (visual) text."""
    rules = []
    for row in tsv(HERE / "ar/glossary.tsv"):
        parts = []
        for c in to_game(row["old"], wrap=0):
            # shipped files write isolated letters either as base letters or isolated forms
            if unicodedata.name(c, "").endswith("ISOLATED FORM"):
                parts.append(f"(?:{re.escape(c)}|{re.escape(unicodedata.normalize('NFKC', c))})")
            else:
                parts.append(re.escape(c))
        rules.append((re.compile(f"(?<!{_AR}){''.join(parts)}(?!{_AR})"), to_game(row["new"], wrap=0)))
    return rules


def build_arabic(files):
    errors = []
    align = tsv(HERE / "ar/align.tsv")
    rules = glossary_rules()
    for en_path in sorted((BACKUP / "Text/EN").glob("*_EN.txt")):
        stem = en_path.stem[:-3]
        en = read_text(en_path).replace("\r", "").split("\n")
        if stem == "Themes":
            en = en[14:]  # EN-only header; tags are read from EN
        ar_path = BACKUP / f"Text/AR/{stem}_AR.txt"
        ar = read_text(ar_path).split("\n")
        # 1) alignment (original AR numbering, applied bottom-up)
        for op in sorted((o for o in align if o["file"] == stem), key=lambda o: -int(o["line"])):
            i = int(op["line"]) - 1
            if op["op"] == "delete":
                del ar[i]
            else:
                ar.insert(i, "")
        for rx, new in rules:
            ar = [rx.sub(new, line) for line in ar]
        # 2) line patches, keyed by line number (aligned with EN)
        for patch_file in sorted((HERE / "ar").glob(f"{stem}*.tsv")):
            for row in tsv(patch_file):
                n = int(row["line"])
                while len(ar) < n:
                    ar.append("")
                ar[n - 1] = to_game(row["ar"], fields=stem in FIELD_FILES)
        # 3) checks against EN
        if len(ar) != len(en):
            errors.append(f"{stem}: AR has {len(ar)} lines, EN has {len(en)}")
        for n, (a, e) in enumerate(zip(ar, en), 1):
            ph = lambda s: sorted(re.findall(r"<(?:NAME\d?|NUM\d?|TEXT|DATE\d|TIME|GENRE|THEME)>", s))
            if e.strip() and ph(a) != ph(e):
                errors.append(f"{stem}:{n} placeholders AR {ph(a)} vs EN {ph(e)}")
            if re.search(r"\b(?:reyalpitluM|enilnO|OMM|P2F|KDS|nOddA)\b", a):
                errors.append(f"{stem}:{n} reversed Latin left")
        files[f"Text/AR/{stem}_AR.txt"] = "\ufeff" + "\n".join(ar)
    return errors


# ---------- images ----------
def image_files():
    for part in ("CompanyLogos", "Icons_Platforms"):
        for p in sorted((HERE / "images" / part).glob("*.png")):
            yield f"{part}/{p.name}", p


PLUGINS = GAME.parents[1] / "BepInEx" / "plugins"


def plugin_files():
    """Compiled BepInEx plugins (dotnet build -c Release in mod/plugin/<name>)."""
    # UiDump is a developer tool (F10 dumps menu layouts); it is never part of a release install
    return sorted(p for p in HERE.glob("plugin/*/bin/Release/netstandard2.1/*.dll") if p.stem != "UiDump")


def main(argv):
    if "--backup" in argv:
        for part in PARTS:
            if not (BACKUP / part).exists():
                shutil.copytree(GAME / part, BACKUP / part)
        print("backup ok:", BACKUP)
        return 0
    if "--restore" in argv:
        for part in PARTS:
            shutil.copytree(BACKUP / part, GAME / part, dirs_exist_ok=True)
        for dll in plugin_files():
            (PLUGINS / dll.name).unlink(missing_ok=True)
        print("original files restored, mod plugins removed (BepInEx itself stays)")
        return 0
    files = {}
    errors = build_data(files) + build_arabic(files)
    for e in errors:
        print("CHECK FAIL:", e)
    images = list(image_files())
    print(f"{len(files)} text files, {len(images)} images, {len(errors)} check failures")
    if errors or "--check" in argv:
        return 1 if errors else 0
    for rel, text in files.items():
        (GAME / rel).write_bytes(text.replace("\n", "\r\n").encode("utf-8"))
    for rel, src in images:
        shutil.copyfile(src, GAME / rel)
    PLUGINS.mkdir(parents=True, exist_ok=True)
    for dll in plugin_files():
        shutil.copyfile(dll, PLUGINS / dll.name)
    print("applied to", GAME, "+", len(plugin_files()), "plugin(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
