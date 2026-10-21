"""Render all processed images (labelled by file name) on a grey sheet for a visual check."""
import pathlib, sys

from PIL import Image, ImageDraw

HERE = pathlib.Path(__file__).resolve().parent
part, cell = sys.argv[1], int(sys.argv[2])  # e.g. CompanyLogos 64 / Icons_Platforms 128
files = sorted((HERE / "images" / part).glob("*.png"), key=lambda p: (len(p.stem), p.stem))
cols = 10
rows = (len(files) + cols - 1) // cols
sheet = Image.new("RGBA", (cols * (cell + 10), rows * (cell + 22)), (80, 80, 80, 255))
draw = ImageDraw.Draw(sheet)
for i, p in enumerate(files):
    x, y = i % cols * (cell + 10), i // cols * (cell + 22)
    sheet.alpha_composite(Image.open(p).convert("RGBA"), (x + 5, y + 2))
    draw.text((x + 3, y + cell + 6), p.stem[:18], fill=(255, 255, 255, 255))
sheet.save(sys.argv[3])
print(len(files), "images ->", sys.argv[3])
