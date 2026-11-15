"""Feature graphic 1024x500 з РЕАЛЬНОГО знімка меню гри + назва. Назву змінити в TITLE за потреби (див. docs/play/NAME_CHECK.md)."""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
TITLE = "CROWN RUSH"
TAG = "Turn gold into towers. Hold the keep."
menu = Image.open("store/screenshots/00_menu.png").convert("RGB")   # 1080x2160
g1 = Image.open("store/screenshots/01_meadow.png").convert("RGB")
W, H = 1024, 500
canvas = Image.new("RGB", (W, H))
# правий блок: діорама замку (реальний знімок), ліворуч — геймплей, розмитий як тло
bg = g1.crop((0, 700, 1080, 700 + int(1080 * H / W))).resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(6))
canvas.paste(bg, (0, 0))
dio = menu.crop((60, 560, 1020, 1520))          # замок + герой + орда
dio = dio.resize((560, 560), Image.LANCZOS)
mask = Image.new("L", dio.size, 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, 559, 559], 40, fill=255)
canvas.paste(dio, (500, -30), mask)
ov = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(ov)
for x in range(0, 520):
    d.line([(x, 0), (x, H)], fill=(10, 22, 48, int(200 * (1 - x / 520))))
canvas = Image.alpha_composite(canvas.convert("RGBA"), ov)
d = ImageDraw.Draw(canvas)
f1 = ImageFont.truetype("/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf", 70)
f2 = ImageFont.truetype("/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf", 25)
for dx, dy in [(-3, 0), (3, 0), (0, -3), (0, 3), (3, 3)]:
    d.text((40 + dx, 160 + dy), TITLE, font=f1, fill=(20, 20, 50))
d.text((40, 160), TITLE, font=f1, fill=(255, 217, 90))
d.text((44, 262), TAG, font=f2, fill=(255, 244, 214))
canvas.convert("RGB").save("store/feature_graphic_1024x500.png")
Image.open("godot/icon/icon_512.png").convert("RGBA").save("store/icon_512.png")
