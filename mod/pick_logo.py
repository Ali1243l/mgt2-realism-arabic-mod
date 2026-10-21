"""Search Wikimedia Commons for logo candidates and render them on one sheet to choose from.

    python -P pick_logo.py out.png "Electronic Arts logo" "Nintendo logo" ...
Prints "query | n | File name" for each candidate; the sheet shows the same numbers.
"""
import io, pathlib, sys

from PIL import Image, ImageDraw

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import fetch_images as F  # noqa: E402

sys.stdout.reconfigure(encoding="utf-8")
out, queries = sys.argv[1], sys.argv[2:]
cells = []
for q in queries:
    res = F.api({"action": "query", "list": "search", "srsearch": q, "srnamespace": 6, "srlimit": 6},
                host="commons.wikimedia.org")["query"]["search"]
    for n, r in enumerate(res):
        name = r["title"][5:]
        print(f"{q} | {n} | {name}")
        try:
            url = F.commons_thumb(name, 120)
            im = Image.open(io.BytesIO(F.get(url))).convert("RGBA") if url else None
        except Exception:
            im = None
        cells.append((q, n, im))
sheet = Image.new("RGBA", (6 * 130, len(queries) * 140), (200, 200, 200, 255))
draw = ImageDraw.Draw(sheet)
for i, (q, n, im) in enumerate(cells):
    row = queries.index(q)
    x, y = n * 130, row * 140
    if im:
        im.thumbnail((120, 110))
        sheet.alpha_composite(im, (x + 5, y + 5))
    draw.text((x + 5, y + 120), f"{n} {q[:16]}", fill=(180, 0, 0, 255))
sheet.save(out)
