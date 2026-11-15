"""Генерує оригінальну іконку Crown Rush (корона на тлі веж) — власна графіка, без сторонніх ресурсів."""
from PIL import Image, ImageDraw, ImageFilter
import math, sys

S = 4  # supersampling


def crown(d, cx, cy, w, h, shade=True):
    """Корона: основа + 5 зубців + 3 самоцвіти. Координати в пікселях великого полотна."""
    x0, x1 = cx - w / 2, cx + w / 2
    base_t = cy + h * 0.10
    base_b = cy + h * 0.5
    pts = [(x0, base_b), (x0, base_t)]
    n = 5
    for i in range(n):
        tx = x0 + w * (i + 0.5) / n
        pts += [(tx - w / (n * 2) * 0.55, base_t - h * 0.05), (tx, cy - h * 0.5 + (h * 0.12 if i % 2 else 0)), (tx + w / (n * 2) * 0.55, base_t - h * 0.05)]
    pts += [(x1, base_t), (x1, base_b)]
    gold, dark, light = (255, 200, 40), (214, 140, 20), (255, 236, 150)
    d.polygon(pts, fill=gold)
    d.rectangle([x0, base_t + h * 0.22, x1, base_b], fill=dark)       # нижня смуга
    d.rectangle([x0, base_t + h * 0.04, x1, base_t + h * 0.12], fill=light)
    for i in range(n):
        tx = x0 + w * (i + 0.5) / n
        ty = cy - h * 0.5 + (h * 0.12 if i % 2 else 0)
        r = w * 0.032
        d.ellipse([tx - r, ty - r, tx + r, ty + r], fill=(255, 90, 90) if i % 2 == 0 else (110, 200, 255))
    for k, col in enumerate([(230, 50, 70), (60, 140, 255), (230, 50, 70)]):
        gx = cx + (k - 1) * w * 0.26
        gy = base_t + h * 0.30
        r = w * 0.045
        d.ellipse([gx - r, gy - r, gx + r, gy + r], fill=col)
    d.line(pts + [pts[0]], fill=(150, 90, 10), width=int(w * 0.012), joint="curve")


def sky(size):
    img = Image.new("RGB", (size, size))
    px = img.load()
    for y in range(size):
        t = y / size
        for x in range(size):
            r = int(40 + 70 * t + 25 * math.sin(x / size * 3))
            g = int(60 + 60 * t)
            b = int(150 + 70 * (1 - t))
            px[x, y] = (min(r, 255), min(g, 255), min(b, 255))
    return img


def foreground(size):
    """Корона по центру в безпечній зоні (адаптивна іконка ~66%)."""
    big = Image.new("RGBA", (size * S, size * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    c = size * S / 2
    w = size * S * 0.52
    h = size * S * 0.42
    # тінь
    sh = Image.new("RGBA", big.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([c - w * 0.55, c + h * 0.45, c + w * 0.55, c + h * 0.7], fill=(0, 0, 0, 110))
    big.alpha_composite(sh.filter(ImageFilter.GaussianBlur(size * S * 0.02)))
    crown(d, c, c - h * 0.05, w, h)
    return big.resize((size, size), Image.LANCZOS)


def background(size):
    img = sky(size)
    big = img.resize((size * S, size * S)).convert("RGBA")
    d = ImageDraw.Draw(big)
    W = size * S
    # дві вежі-силуети на тлі і пагорб
    for bx, bw, bh in [(0.12, 0.18, 0.32), (0.70, 0.2, 0.40)]:
        x0, x1 = W * bx, W * (bx + bw)
        y1 = W * 0.9
        y0 = y1 - W * bh
        d.rectangle([x0, y0, x1, y1], fill=(50, 52, 96, 255))
        m = (x1 - x0) / 5
        for k in range(3):
            d.rectangle([x0 + m * (2 * k), y0 - W * 0.03, x0 + m * (2 * k + 1), y0], fill=(50, 52, 96, 255))
    d.ellipse([-W * 0.2, W * 0.82, W * 1.2, W * 1.5], fill=(60, 150, 80, 255))
    return big.resize((size, size), Image.LANCZOS).convert("RGB")


def mono(size):
    fg = foreground(size)
    a = fg.split()[3]
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste((255, 255, 255, 255), mask=a)
    return out


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "godot/icon"
    bg, fg = background(432), foreground(432)
    bg.save(f"{out}/adaptive_background_432.png")
    fg.save(f"{out}/adaptive_foreground_432.png")
    mono(432).save(f"{out}/adaptive_monochrome_432.png")
    full = background(512).convert("RGBA")
    full.alpha_composite(foreground(512))
    full.save(f"{out}/icon_512.png")
    full.resize((192, 192), Image.LANCZOS).save(f"{out}/icon_192.png")
    full.resize((128, 128), Image.LANCZOS).save(f"{out}/icon.png")


main()
