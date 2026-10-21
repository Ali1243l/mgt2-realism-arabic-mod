"""Sheet of processed logos/icons, each on a light and a dark panel, labelled with id + name."""
import csv, pathlib, sys
from PIL import Image, ImageDraw
HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import fetch_images as F
kind, out = sys.argv[1], sys.argv[2]
targets = F.pic_files()
names = {r["id"]: r["name"] for r in F.tsv(HERE / ("data/publishers.tsv" if kind == "pub" else "data/platforms.tsv"))}
rows = [r for r in F.tsv(HERE / "data/wiki_titles.tsv") if r["kind"] == kind]
size = 64 if kind == "pub" else 128
cw, ch = size * 2 + 14, size + 18
cols = 8 if kind == "pub" else 5
sheet = Image.new("RGBA", (cols * cw, ((len(rows) + cols - 1) // cols) * ch), (0, 0, 0, 255))
d = ImageDraw.Draw(sheet)
for i, r in enumerate(rows):
    x, y = i % cols * cw, i // cols * ch
    d.rectangle((x, y, x + size + 6, y + ch - 2), fill=(225, 225, 225, 255))
    d.rectangle((x + size + 7, y, x + cw - 2, y + ch - 2), fill=(60, 60, 66, 255))
    p = HERE / "images" / targets[(kind, r["id"])][0]
    if p.exists():
        im = Image.open(p).convert("RGBA")
        sheet.alpha_composite(im, (x + 3, y + 2)); sheet.alpha_composite(im, (x + size + 10, y + 2))
    d.text((x + 3, y + size + 4), f"{r['id']} {names.get(r['id'], '')[:22]}", fill=(255, 40, 40, 255))
sheet.save(out)
