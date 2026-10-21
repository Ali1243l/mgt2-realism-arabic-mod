"""Draw 128x128 icons for the 2030-2100 platforms in the shipped icon style: a glossy 3D device
with a soft shadow on a transparent background. Shape follows device type and era, colour the
maker. Drawn at 4x and downscaled for smooth edges.

    python -P future_icons.py   ->  images/Icons_Platforms/F_*.png
"""
import pathlib, sys

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import future  # noqa: E402

S = 512  # drawing size; output is 128
OUT = pathlib.Path(__file__).resolve().parent / "images" / "Icons_Platforms"
BRAND = {  # body, accent
    "Sony": ((38, 40, 48), (40, 120, 235)), "Microsoft": ((32, 34, 36), (60, 180, 80)),
    "Nintendo": ((236, 236, 240), (230, 20, 30)), "Valve": ((40, 52, 70), (102, 192, 244)),
    "Apple": ((205, 208, 214), (90, 165, 255)), "Google": ((245, 245, 248), (66, 133, 244)),
    "Arcade Electrics": ((50, 30, 70), (255, 140, 30)),
}


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)


def box(d, x, y, w, h, depth, body):
    """Isometric box: front, top and right faces."""
    dx, dy = depth, -depth // 2
    d.polygon([(x, y), (x + w, y), (x + w + dx, y + dy), (x + dx, y + dy)], fill=shade(body, 1.35))
    d.polygon([(x + w, y), (x + w + dx, y + dy), (x + w + dx, y + h + dy), (x + w, y + h)], fill=shade(body, 0.7))
    d.rectangle((x, y, x + w, y + h), fill=shade(body, 1.0))


def glow(img, xy, color, radius):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse(xy, fill=color[:3] + (200,))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius)))


def draw(typ, tech, maker):
    body, acc = BRAND.get(maker, ((60, 60, 70), (0, 200, 255)))
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if typ == 1 and tech <= 10:  # slim console with a light strip
        box(d, 70, 200, 330, 150, 80, body)
        d.rectangle((70, 268, 400, 280), fill=acc + (255,))
        glow(img, (90, 255, 380, 295), acc, 14)
    elif typ == 1 and tech <= 12:  # base unit projecting a hologram
        d.ellipse((90, 330, 420, 420), fill=shade(body, 0.7))
        d.ellipse((90, 310, 420, 400), fill=shade(body, 1.2))
        holo = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        hd = ImageDraw.Draw(holo)
        hd.polygon([(255, 350), (110, 90), (400, 90)], fill=acc + (90,))
        hd.ellipse((180, 120, 330, 270), outline=acc + (230,), width=10)
        hd.polygon([(255, 140), (300, 230), (210, 230)], outline=(255, 255, 255, 220), width=6)
        img.alpha_composite(holo)
        glow(img, (200, 320, 310, 370), acc, 12)
        if tech == 12:  # neural generation adds a headband
            d.arc((300, 300, 470, 470), 200, 340, fill=acc + (255,), width=18)
    elif typ == 1 and tech <= 14:  # organic bio pod
        d.ellipse((90, 160, 420, 400), fill=shade(body, 1.0))
        d.ellipse((120, 175, 390, 300), fill=shade(body, 1.35))
        glow(img, (170, 240, 340, 330), acc if tech == 13 else (60, 230, 120), 18)
        d.ellipse((215, 260, 295, 310), fill=(255, 255, 255, 230))
    elif typ == 1:  # full-dive capsule with glass dome
        d.rounded_rectangle((60, 260, 450, 380), radius=60, fill=shade(body, 0.9))
        dome = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        ImageDraw.Draw(dome).chord((80, 120, 430, 400), 180, 360, fill=acc + (110,), outline=acc + (255,), width=8)
        img.alpha_composite(dome)
        glow(img, (120, 330, 390, 370), acc, 12)
        d.rectangle((90, 330, 420, 338), fill=acc + (255,))
        if tech >= 16:  # last generation: a halo ring over the capsule
            ring = Image.new("RGBA", (S, S), (0, 0, 0, 0))
            ImageDraw.Draw(ring).ellipse((150, 60, 360, 130), outline=acc + (255,), width=12)
            img.alpha_composite(ring.filter(ImageFilter.GaussianBlur(2)))
    elif typ == 2:  # handheld with grips and glowing screen
        d.rounded_rectangle((50, 180, 462, 360), radius=80, fill=shade(body, 1.0))
        d.rounded_rectangle((60, 172, 452, 344), radius=76, fill=shade(body, 1.25))
        d.rounded_rectangle((150, 200, 362, 320), radius=12, fill=(20, 24, 32, 255))
        glow(img, (170, 215, 342, 305), acc, 16 if tech >= 11 else 6)
        d.ellipse((85, 240, 125, 280), fill=shade(body, 0.6))
        d.ellipse((392, 225, 422, 255), fill=acc + (255,))
        d.ellipse((392, 265, 422, 295), fill=shade(body, 0.6))
    elif typ == 3 and tech <= 10:  # phone
        d.rounded_rectangle((165, 60, 347, 452), radius=40, fill=shade(body, 0.8))
        d.rounded_rectangle((175, 70, 337, 442), radius=32, fill=(18, 20, 28, 255))
        glow(img, (190, 110, 322, 400), acc, 24)
        d.rounded_rectangle((225, 82, 287, 96), radius=7, fill=shade(body, 0.6))
    elif typ == 3 and tech <= 12:  # wrist band
        d.ellipse((100, 130, 412, 400), outline=shade(body, 1.0), width=48)
        d.rounded_rectangle((190, 110, 322, 210), radius=30, fill=(18, 20, 28, 255))
        glow(img, (205, 125, 307, 195), acc, 12)
    elif typ == 3 and tech <= 14:  # thought-link circlet
        d.arc((80, 140, 432, 470), 190, 350, fill=shade(body, 1.0), width=40)
        for x in (130, 255, 380):
            glow(img, (x - 30, 185 if x != 255 else 140, x + 30, 245 if x != 255 else 200), acc, 10)
    elif typ == 3:  # neural implant chip
        d.rounded_rectangle((176, 176, 336, 336), radius=26, fill=shade(body, 0.9))
        d.rectangle((216, 216, 296, 296), fill=acc + (255,))
        for i in range(5):
            o = 190 + i * 30
            d.line((o, 120, o, 176), fill=shade(body, 1.2), width=8)
            d.line((o, 336, o, 392), fill=shade(body, 1.2), width=8)
            d.line((120, o, 176, o), fill=shade(body, 1.2), width=8)
            d.line((336, o, 392, o), fill=shade(body, 1.2), width=8)
        glow(img, (200, 200, 312, 312), acc, 20)
    else:  # arcade cabinet
        box(d, 150, 90, 170, 340, 70, body)
        d.rectangle((168, 120, 302, 230), fill=(15, 15, 25, 255))
        glow(img, (175, 125, 295, 225), acc, 16)
        d.rectangle((150, 260, 320, 300), fill=shade(body, 1.4))
        d.ellipse((190, 268, 210, 288), fill=acc + (255,))
        d.ellipse((250, 268, 270, 288), fill=(240, 240, 240, 255))
    # soft drop shadow like the shipped icons
    alpha = img.getchannel("A").point(lambda v: v * 0.5).filter(ImageFilter.GaussianBlur(10))
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    shadow.putalpha(alpha)
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.alpha_composite(shadow, (8, 12))
    out.alpha_composite(img)
    return out.resize((128, 128), Image.LANCZOS)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for (pid, name, maker, pub, date, tech, typ, pre, *_rest, pic) in future.PLATFORMS:
        draw(typ, tech, maker).save(OUT / pic)
    print(len(future.PLATFORMS), "icons")
