"""Download each article's lead image from Wikipedia (logos for companies, photos for
hardware) and fit it into the game's sizes: CompanyLogos 64x64, Icons_Platforms 128x128,
transparent background. Raw downloads go to ../downloads (untrusted data, never executed).

    python -P fetch_images.py          # download missing + (re)process all
"""
import csv, io, json, pathlib, re, sys, time, urllib.error, urllib.parse, urllib.request
from collections import deque

from PIL import Image, ImageDraw, ImageFilter

HERE = pathlib.Path(__file__).resolve().parent
RAW = HERE.parent / "downloads" / "wiki"
OUT = HERE / "images"
DATA = HERE.parent / "backup" / "Extern" / "Text" / "DATA"
UA = {"User-Agent": "MGT2-realism-mod/1.0 (personal game mod; python urllib)"}


def tsv(path):
    with open(path, encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f, delimiter="\t", quoting=csv.QUOTE_NONE))


def get(url, tries=6):
    for attempt in range(tries):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=30) as r:
                return r.read()
        except urllib.error.HTTPError as e:
            if e.code != 429 or attempt == tries - 1:
                raise
            time.sleep(int(e.headers.get("Retry-After") or 0) or 15 * (attempt + 1))


def api(params, host="en.wikipedia.org"):
    q = urllib.parse.urlencode({"format": "json", **params})
    return json.loads(get(f"https://{host}/w/api.php?" + q))


def lead_image_url(title, size, kind):
    """Platforms: article lead image (hardware photo). Companies: Wikidata logo (P154), else the
    lead image only when it is a logo file (company lead images are often office photos)."""
    pages = api({"action": "query", "redirects": 1, "titles": title, "prop": "pageimages|pageprops",
                 "piprop": "thumbnail|name", "pithumbsize": size, "pilicense": "any",
                 "ppprop": "wikibase_item"})["query"]["pages"]
    page = next(iter(pages.values()))
    src = page.get("thumbnail", {}).get("source")
    qid = page.get("pageprops", {}).get("wikibase_item")
    if kind == "plat":
        if src and ("logo" in src.lower() or ".svg" in src.lower()) and qid:
            # the article leads with the brand logo: use the device photo (Wikidata P18) instead
            claims = api({"action": "wbgetclaims", "entity": qid, "property": "P18"},
                         host="www.wikidata.org").get("claims", {}).get("P18", [])
            photos = [c["mainsnak"]["datavalue"]["value"] for c in claims if "datavalue" in c["mainsnak"]]
            return commons_thumb(photos[0], size) if photos else None
        return src
    fallback = src if src and "logo" in src.lower() else enwiki_logo(title, size)
    if not qid:
        return fallback
    claims = api({"action": "wbgetclaims", "entity": qid, "property": "P154"},
                 host="www.wikidata.org").get("claims", {}).get("P154", [])
    if not claims:
        return fallback
    # current logo: preferred rank, otherwise one without an end date (P582), otherwise the last
    current = [c for c in claims if c.get("rank") == "preferred"] or \
              [c for c in claims if "P582" not in c.get("qualifiers", {})] or claims
    return commons_thumb(current[-1]["mainsnak"]["datavalue"]["value"], size) or fallback


def enwiki_logo(title, size):
    """Logo file used in the English article (non-free logos live on enwiki, not Commons)."""
    pages = api({"action": "query", "redirects": 1, "titles": title, "prop": "images",
                 "imlimit": 50})["query"]["pages"]
    files = [i["title"] for i in next(iter(pages.values())).get("images", [])
             if "logo" in i["title"].lower() and "commons" not in i["title"].lower()
             and "wiki" not in i["title"].lower()]
    if not files:
        return None
    info = api({"action": "query", "titles": files[0], "prop": "imageinfo", "iiprop": "url",
                "iiurlwidth": size})["query"]["pages"]
    page = next(iter(info.values()))
    return page["imageinfo"][0].get("thumburl") if "imageinfo" in page else None


def commons_thumb(fname, size):
    info = api({"action": "query", "titles": "File:" + fname, "prop": "imageinfo",
                "iiprop": "url", "iiurlwidth": size}, host="commons.wikimedia.org")["query"]["pages"]
    page = next(iter(info.values()))
    return page["imageinfo"][0].get("thumburl") if "imageinfo" in page else None


def pic_files():
    """(kind, id) -> list of target image file names, read from the original data files."""
    out = {}
    pub = (DATA / "Publisher.txt").read_text(encoding="utf-8-sig")
    for b in re.split(r"\n(?=\[ID\])", pub)[1:]:
        d = dict(re.findall(r"^\[([A-Z0-9 \-]+)\](.*)$", b, re.M))
        out[("pub", d["ID"])] = ["CompanyLogos/%s.png" % d["PIC"]]
    plat = (DATA / "Platforms.txt").read_text(encoding="utf-8-sig")
    for b in re.split(r"\n(?=\[ID\])", plat)[1:]:
        d = dict(re.findall(r"^\[([A-Z0-9 \-]+)\](.*)$", b, re.M))
        out[("plat", d["ID"])] = ["Icons_Platforms/" + d["PIC-1"]]  # PIC-2 (modern PC/Mac) kept
    return out


def clear_background(im, tol=40):
    """Photos on white/flat backgrounds: flood-fill transparent from the borders."""
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    if any(px[x, y][3] < 250 for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1))):
        return im  # already has transparency
    ref = px[0, 0]
    close = lambda p: all(abs(p[i] - ref[i]) <= tol for i in range(3))
    seen, todo = set(), deque()
    for x in range(w):
        todo += [(x, 0), (x, h - 1)]
    for y in range(h):
        todo += [(0, y), (w - 1, y)]
    while todo:
        x, y = todo.popleft()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or not close(px[x, y]):
            continue
        seen.add((x, y))
        px[x, y] = (255, 255, 255, 0)
        todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return im


def background_ok(im):
    """Reject results where the background stayed (opaque border) or the object got eaten."""
    a = im.getchannel("A")
    w, h = im.size
    border = [a.getpixel((x, y)) for x in range(w) for y in (0, h - 1)] + \
             [a.getpixel((x, y)) for y in range(h) for x in (0, w - 1)]
    opaque_border = sum(v > 200 for v in border) / len(border)
    opaque = sum(v > 200 for v in a.tobytes()) / (w * h)
    return opaque_border < 0.15 and opaque > 0.12


def fit(im, size, margin):
    im = clear_background(im)
    if not background_ok(im):
        return None
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    inner = size - 2 * margin
    im.thumbnail((inner, inner), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    canvas.alpha_composite(im, ((size - im.width) // 2, (size - im.height) // 2))
    return canvas


def luminance(im):
    b = im.tobytes()
    px = [tuple(b[i:i + 4]) for i in range(0, len(b), 4) if b[i + 3] > 200]
    return sum(0.299 * r + 0.587 * g + 0.114 * b for r, g, b, _ in px) / max(len(px), 1)


def badge_logo(im, size=64):
    """Game style: the logo sits on a rounded badge (white for dark logos, dark for light ones)
    with an outline, like the shipped PONY/WALVE/Mintendu logos. Wordmarks fill the width."""
    im = clear_background(im)
    bbox = im.getbbox()
    if not bbox:
        return None
    im = im.crop(bbox)
    light = luminance(im) > 165
    fill, line = ((28, 28, 32, 255), (200, 200, 205, 255)) if light else ((250, 250, 250, 255), (45, 45, 50, 255))
    wide = im.width / im.height >= 1.6
    pad = 5
    im.thumbnail((size - 2 - 2 * pad, (size - 2 - 2 * pad) if not wide else 34), Image.LANCZOS)
    bw, bh = (size - 2, max(im.height + 2 * pad, 24)) if wide else (size - 2, size - 2)
    canvas = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(canvas)
    x0, y0 = 1, (size - bh) // 2
    draw.rounded_rectangle((x0, y0, x0 + bw - 1, y0 + bh - 1), radius=6 if wide else 12,
                           fill=fill, outline=line, width=2)
    canvas.alpha_composite(im, ((size - im.width) // 2, (size - im.height) // 2))
    return canvas


def text_logo(name):
    """No logo file anywhere: draw the name as a bold wordmark (the shipped logos are wordmarks)."""
    from PIL import ImageFont
    words = name.replace("Studios", "").replace("Software", "").replace("Entertainment", "").split()
    text = "\n".join(words) if len(words) > 1 and len(" ".join(words)) > 9 else " ".join(words)
    font = ImageFont.truetype(r"C:\Windows\Fonts\ariblk.ttf", 120)
    tmp = Image.new("RGBA", (1600, 600), (255, 255, 255, 0))
    ImageDraw.Draw(tmp).multiline_text((20, 20), text.upper(), font=font, fill=(25, 25, 30, 255),
                                       align="center", spacing=8)
    return tmp


def with_shadow(icon):
    """Soft drop shadow under device art, like the shipped platform icons."""
    alpha = icon.getchannel("A")
    shadow = Image.new("RGBA", icon.size, (0, 0, 0, 0))
    shadow.putalpha(alpha.point(lambda v: v * 0.55).filter(ImageFilter.GaussianBlur(2.5)))
    out = Image.new("RGBA", icon.size, (0, 0, 0, 0))
    out.alpha_composite(shadow, (2, 3))
    out.alpha_composite(icon)
    return out


def main():
    RAW.mkdir(parents=True, exist_ok=True)
    targets = pic_files()
    names = {r["id"]: r["name"] for r in tsv(HERE / "data/publishers.tsv")}
    log = []
    for row in tsv(HERE / "data/wiki_titles.tsv"):
        kind, size = row["kind"], (64 if row["kind"] == "pub" else 128)
        print("progress", kind, row["id"], flush=True)
        if row.get("file") == "KEEP":  # checked by eye: the shipped icon is better
            for rel in targets[(kind, row["id"])]:
                (OUT / rel).unlink(missing_ok=True)
            log.append((kind, row["id"], row["title"], "kept original (by choice)"))
            continue
        raw = RAW / f"{kind}_{row['id']}.img"
        if not raw.exists():
            try:
                # standard thumbnail widths are pre-rendered; odd sizes get rate-limited
                thumb = 250  # infobox size: usually already rendered, so not rate-limited
                # an explicit Commons file (picked by hand) beats the automatic choice
                if row.get("file") == "TEXT":  # logo unreadable at 64px: use the wordmark
                    url = None
                else:
                    url = (commons_thumb(row["file"], thumb) if row.get("file")
                           else lead_image_url(row["title"], thumb, kind))
                if not url:
                    if kind == "pub":  # generated wordmark in the game's badge style
                        name = names[row["id"]]
                        for rel in targets[(kind, row["id"])]:
                            (OUT / rel).parent.mkdir(parents=True, exist_ok=True)
                            badge_logo(text_logo(name)).save(OUT / rel)
                        log.append((kind, row["id"], row["title"], "ok (generated wordmark)"))
                    else:
                        log.append((kind, row["id"], row["title"], "NO IMAGE"))
                    continue
                raw.write_bytes(get(url))
                (RAW / f"{kind}_{row['id']}.url").write_text(url, encoding="utf-8")
                time.sleep(1)  # be polite to Wikimedia
            except Exception as e:  # keep the original icon for this one
                log.append((kind, row["id"], row["title"], f"ERROR {e}"))
                continue
        try:
            img = Image.open(io.BytesIO(raw.read_bytes()))
            img.load()
        except Exception as e:
            log.append((kind, row["id"], row["title"], f"BAD IMAGE {e}"))
            continue
        out_img = badge_logo(img) if kind == "pub" else fit(img, size, 6)
        if out_img is not None and kind == "plat":
            out_img = with_shadow(out_img)
        if out_img is None:
            log.append((kind, row["id"], row["title"], "REJECTED background (original kept)"))
            continue
        for rel in targets[(kind, row["id"])]:
            dest = OUT / rel
            dest.parent.mkdir(parents=True, exist_ok=True)
            out_img.save(dest)
        log.append((kind, row["id"], row["title"], "ok"))
    ok = sum(1 for r in log if r[3] == "ok")
    print(f"{ok} ok, {len(log) - ok} problems")
    for r in log:
        if r[3] != "ok":
            print("  ", *r)


if __name__ == "__main__":
    sys.exit(main())
