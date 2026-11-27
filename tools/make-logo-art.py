#!/usr/bin/env python3
"""Vẽ logo Squeaky Lane và texture pháo hoa. Chạy: python tools/make-logo-art.py"""

import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageColor, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "Assets/_Project/Art/UI"
OUTPUT = UI / "Logo"
FONT = UI / "Fonts/Baloo2-ExtraBold.ttf"
PREVIEW = ROOT / "Logs/021-logo/preview.png"
WIDTH, HEIGHT = 1024, 760
SCALE = 3
PAD = 8
INDIGO = "#26214E"
CREAM = "#FFF4D2"
LANCZOS = Image.Resampling.LANCZOS


def canvas(width, height, mode="RGBA"):
    return Image.new(mode, (width * SCALE, height * SCALE))


def pixels(values):
    return tuple(round(value * SCALE) for value in values)


def ellipse(draw, x, y, rx, ry, fill, outline=None, width=1):
    draw.ellipse(pixels((x - rx, y - ry, x + rx, y + ry)),
                 fill=fill, outline=outline, width=width * SCALE)


def arc(draw, x, y, rx, ry, angles, color, width):
    draw.arc(pixels((x - rx, y - ry, x + rx, y + ry)), *angles,
             fill=color, width=width * SCALE)


def color_mix(start, end, fraction):
    return tuple(round(a + (b - a) * fraction) for a, b in zip(start, end))


def gradient(mask, top, bottom, start_y, end_y):
    """Tô chuyển màu theo chiều dọc, giữ độ phủ alpha của hình."""
    top, bottom = ImageColor.getrgb(top), ImageColor.getrgb(bottom)
    strip = Image.new("RGB", (1, mask.height))
    start, end = pixels((start_y, end_y))
    strip.putdata([color_mix(top, bottom, min(1, max(0, (y - start) / (end - start))))
                   for y in range(mask.height)])
    image = strip.resize(mask.size, Image.Resampling.NEAREST).convert("RGBA")
    image.putalpha(mask)
    return image


def rounded_mask(size, box, radius):
    mask = Image.new("L", size)
    ImageDraw.Draw(mask).rounded_rectangle(pixels(box), radius=radius * SCALE, fill=255)
    return mask


def arch_mask(size, center, top, rx, ry, bottom):
    rim = []
    for degrees in range(181):
        angle = math.radians(degrees)
        rim.append(pixels((center - rx * math.cos(angle), top + ry * (1 - math.sin(angle)))))
    rim.extend([pixels((center + rx, bottom)), pixels((center - rx, bottom))])
    mask = Image.new("L", size)
    ImageDraw.Draw(mask).polygon(rim, fill=255)
    return mask


def stroke(draw, points, color, width):
    draw.line([pixels(point) for point in points], fill=color, width=width * SCALE, joint="curve")
    for x, y in (points[0], points[-1]):
        ellipse(draw, x, y, width / 2, width / 2, color)


def wash_portal():
    art = canvas(WIDTH, HEIGHT)
    # Vòm nhiều lớp và các sọc kem chạy qua mặt vòm.
    for top, rx, ry, light, dark in [
        (87, 350, 267, "#45457B", INDIGO),
        (99, 337, 256, "#8AF0DF", "#11818D"),
        (112, 324, 244, "#35C6C0", "#1595A6"),
    ]:
        mask = arch_mask(art.size, 512, top, rx, ry, 608)
        art.alpha_composite(gradient(mask, light, dark, top, 530))

    stripes = Image.new("RGBA", art.size)
    draw = ImageDraw.Draw(stripes)
    for x in (101, 257, 413, 569, 725, 881):
        points = [(x - 56, 80), (x - 4, 80), (x + 226, 456), (x + 174, 456)]
        draw.polygon([pixels(point) for point in points], fill=CREAM)
    stripes.putalpha(ImageChops.multiply(stripes.getchannel("A"), mask))
    art.alpha_composite(stripes)

    for top, rx, ry, light, dark in [
        (166, 267, 191, "#145C75", "#252A57"),
        (177, 255, 182, "#D5FDF2", "#69D8D7"),
        (185, 246, 176, "#206B87", "#213360"),
    ]:
        mask = arch_mask(art.size, 512, top, rx, ry, 600)
        art.alpha_composite(gradient(mask, light, dark, top, 460))

    water = Image.new("RGBA", art.size)
    draw = ImageDraw.Draw(water)
    for x, width, length in [(309, 10, 150), (368, 13, 152), (442, 8, 151),
                             (507, 13, 101), (584, 9, 144), (650, 14, 143), (713, 8, 133)]:
        stroke(draw, [(x - 22, 191), (x - 16, 245), (x + 13, 191 + length)],
               (110, 231, 237, 95), width)
    for x, y, r in [(430, 304, 9), (487, 231, 5), (533, 277, 8),
                    (565, 223, 5), (611, 306, 5), (690, 289, 7)]:
        ellipse(draw, x, y, r, r, (105, 225, 234, 90), (204, 249, 255, 158), 2)
    water.putalpha(ImageChops.multiply(water.getchannel("A"), mask))
    art.alpha_composite(water)
    return art


def soap_foam(art):
    # Lớp bọt chung chống lộ đường ráp. Các bong bóng trước có ánh sáng riêng.
    clusters = [(171, 529, 84), (234, 579, 102), (341, 634, 99), (433, 657, 82),
                (512, 643, 103), (604, 650, 84), (686, 616, 102), (788, 589, 95),
                (851, 513, 79), (909, 545, 62), (125, 577, 60)]
    silhouette = Image.new("L", art.size)
    draw = ImageDraw.Draw(silhouette)
    for x, y, r in clusters:
        ellipse(draw, x, y + 4, r + 8, r + 5, 255)
    art.alpha_composite(gradient(silhouette, "#619BC8", "#465B97", 470, 750))

    for x, y, r in sorted(clusters, key=lambda bubble: bubble[1]):
        mask = Image.new("L", art.size)
        ellipse(ImageDraw.Draw(mask), x, y, r, r, 255)
        art.alpha_composite(gradient(mask, "#F7FEFF", "#8ECFEF", y - r, y + r))
        draw = ImageDraw.Draw(art)
        arc(draw, x, y, r - 4, r - 4, (204, 290), "#EAFBFF", 6)

    for x, y, r in [(114, 549, 25), (171, 586, 38), (262, 624, 35), (333, 670, 44),
                    (432, 692, 34), (503, 713, 23), (620, 696, 33), (704, 651, 38),
                    (816, 612, 35), (883, 580, 43), (928, 538, 25), (879, 493, 27)]:
        draw = ImageDraw.Draw(art)
        ellipse(draw, x, y + 3, r + 2, r + 2, "#8DC1E5")
        mask = Image.new("L", art.size)
        ellipse(ImageDraw.Draw(mask), x, y, r, r, 255)
        art.alpha_composite(gradient(mask, "#F8FFFF", "#AEDFF4", y - r, y + r))
        draw = ImageDraw.Draw(art)
        arc(draw, x - 1, y - 1, r - 5, r - 5, (206, 282), "#FFFFFF", 4)
        ellipse(draw, x + r * 0.20, y - r * 0.63, 3, 3, "#F7FEFF")
    return art


def type_art(word, font_size, outline, depth, top, bottom):
    font = ImageFont.truetype(FONT, font_size * SCALE, layout_engine=ImageFont.Layout.BASIC)
    left, upper, right, lower = font.getbbox(word)
    margin = 25 * SCALE
    size = right - left + 2 * margin, lower - upper + 2 * margin + depth * SCALE
    origin = (margin - left, margin - upper)

    def text_mask(stroke_width=0, drop=0):
        mask = Image.new("L", size)
        ImageDraw.Draw(mask).text((origin[0], origin[1] + drop * SCALE), word, font=font,
                                  fill=255, stroke_width=stroke_width * SCALE)
        return mask

    art = Image.new("RGBA", size)
    shadow = text_mask(outline, depth)
    for step in range(depth - 3, 0, -3):
        shadow = ImageChops.lighter(shadow, text_mask(outline, step))
    art.alpha_composite(gradient(shadow, "#444077", "#352D65", 25, (lower - upper) / SCALE + 45))
    art.alpha_composite(gradient(text_mask(outline), INDIGO, INDIGO, 0, 1))
    art.alpha_composite(gradient(text_mask(2), "#FFFFFF", "#D6FBFF", 25, 135))

    # Chừa viền trong trắng và giới hạn vệt bóng ở phần trên của mặt chữ.
    face = text_mask()
    art.alpha_composite(gradient(face, top, bottom, 29, 25 + (lower - upper) / SCALE * 0.84))
    glaze = Image.new("RGBA", size)
    draw = ImageDraw.Draw(glaze)
    draw.rounded_rectangle(pixels((20, 45, size[0] / SCALE - 20, 55)), radius=5 * SCALE,
                           fill=(255, 255, 255, 140))
    inset = ImageChops.multiply(text_mask(), text_mask(0, 5))
    glaze.putalpha(ImageChops.multiply(glaze.getchannel("A"), inset))
    art.alpha_composite(glaze)
    return art


def lane_sign():
    art = canvas(550, 181)
    for box, radius, top, bottom in [
        ((4, 17, 546, 173), 62, "#794276", INDIGO),
        ((4, 5, 546, 155), 62, INDIGO, INDIGO),
        ((13, 14, 537, 146), 54, "#FFB1C2", "#BF2768"),
        ((18, 20, 532, 137), 51, "#FA81A4", "#F2578A"),
    ]:
        art.alpha_composite(gradient(rounded_mask(art.size, box, radius), top, bottom, box[1], box[3]))
    draw = ImageDraw.Draw(art)
    stroke(draw, [(53, 50), (67, 41), (111, 41)], "#FFD1DE", 5)
    stroke(draw, [(439, 41), (483, 41), (497, 50)], "#FFD1DE", 5)
    for x in (41, 85, 429, 473):
        stroke(draw, [(x, 83), (x + 26, 83)], CREAM, 9)
    label = type_art("LANE", 117, 6, 7, "#FFFFFF", "#F5F9FF")
    label = label.crop(label.getchannel("A").getbbox())
    art.alpha_composite(label, ((art.width - label.width) // 2, 41 * SCALE))
    return art.rotate(3, Image.Resampling.BICUBIC, expand=True)


def bubble(radius, small=False):
    middle = radius + 6
    art = canvas(middle * 2, middle * 2)
    draw = ImageDraw.Draw(art)
    ellipse(draw, middle, middle, radius, radius, (143, 225, 247, 56), "#507EAA", 3)
    arc(draw, middle, middle, radius - 2, radius - 2, (132, 302), "#DDFBFF", 6)
    arc(draw, middle, middle, radius - 5, radius - 5, (323, 437), "#91EEF7", 5)
    arc(draw, middle, middle, radius - 10, radius - 10, (28, 92), "#E5C8FF", 4)
    arc(draw, middle, middle, radius - 11, radius - 11, (203, 262), "#FFFFFF", 6)
    ellipse(draw, middle + radius * 0.20, middle - radius * 0.69, 4, 4, "#FFFFFF")
    if not small:
        ellipse(draw, middle + radius * 0.63, middle + radius * 0.35, 4, 4, "#FFECFB")
    return art


def spark():
    art = canvas(112, 128)
    points = [(56, 4), (70, 43), (108, 64), (70, 79), (56, 124),
              (41, 79), (5, 64), (41, 47)]
    draw = ImageDraw.Draw(art)
    stroke(draw, points + [points[0]], "#665498", 6)
    draw.polygon([pixels(point) for point in points], fill="#FFE566")
    draw.polygon([pixels(point) for point in [(56, 14), (60, 56), (94, 64),
                                               (57, 69), (51, 96), (50, 69),
                                               (19, 64), (50, 55)]], fill="#FFF7BC")
    return art


def cropped_sprite(art):
    image = art.resize((round(art.width / SCALE), round(art.height / SCALE)), LANCZOS)
    image = image.crop(image.getchannel("A").getbbox())
    padded = Image.new("RGBA", (image.width + 2 * PAD, image.height + 2 * PAD))
    padded.paste(image, (PAD, PAD))
    return padded


def make_pieces():
    artwork = []

    def place(name, role, art, *, center=None, position=None):
        sprite = cropped_sprite(art)
        if center is not None:
            position = (round(center[0] - sprite.width / 2), round(center[1] - sprite.height / 2))
        artwork.append((name, role, sprite, position))

    place("logo_back", "back", soap_foam(wash_portal()), center=(512, 415))
    for index, (letter, x, y, angle) in enumerate(zip(
            "SQUEAKY", (132, 254, 389, 515, 637, 770, 897),
            (452, 445, 422, 416, 418, 428, 449), (4, -4, 4, -4, 4, -4, 4))):
        art = type_art(letter, 214, 10, 16, "#FFFFFF", "#5FD8F0")
        rotated = art.rotate(angle, Image.Resampling.BICUBIC, expand=True)
        place(f"logo_letter_{index}", "letter", rotated, center=(x, y))
    place("logo_lane", "word", lane_sign(), center=(520, 576))
    place("deco_bubble_a", "deco", bubble(46), position=(0, 238))
    small_bubble = cropped_sprite(bubble(31, small=True))
    artwork.append(("deco_bubble_b", "deco", small_bubble, (WIDTH - small_bubble.width, 311)))
    place("deco_spark", "deco", spark(), position=(775, 0))
    return artwork


def particle(width, height, opacity):
    image = Image.new("RGBA", (width, height), "white")
    alpha = Image.new("L", image.size)
    alpha.putdata([round(255 * opacity(x + 0.5, y + 0.5))
                   for y in range(height) for x in range(width)])
    image.putalpha(alpha)
    return image


def dot_opacity(x, y):
    radius = math.hypot(x - 32, y - 32) / 25
    return max(0, 1 - radius * radius) ** 2.4


def trail_opacity(x, y):
    length = min(1, max(0, (y - 15) / 169))
    width = 6.3 - 4.8 * length
    head = math.exp(-0.5 * (min(0, y - 15) / 3.2) ** 2)
    cross_section = math.exp(-0.5 * ((x - 16) / width) ** 2)
    # Hạ alpha về 0 trước mép để chất liệu additive không lộ biên texture.
    edge = min(1, max(0, (min(x, 32 - x) - PAD) / 3))
    lead_in = min(1, max(0, (y - PAD) / 4))
    return edge * lead_in * cross_section * head * (1 - length) ** 0.85


def make_preview(full, artwork, dot, trail):
    preview = Image.new("RGB", (1456, 1224), "#17243A")
    draw = ImageDraw.Draw(preview)
    label = ImageFont.truetype(FONT, 22)
    tight = full.crop(full.getchannel("A").getbbox())
    for column, (background, foreground) in enumerate((("#0B0A14", "#EAFBFF"), ("#70AAE2", INDIGO))):
        left = 12 + column * 724
        for width, top, height in ((700, 12, 563), (350, 587, 303)):
            draw.rounded_rectangle((left, top, left + 707, top + height), radius=15, fill=background)
            image = tight.resize((width, round(tight.height * width / tight.width)), LANCZOS)
            preview.paste(image, (left + (708 - width) // 2, top + 38), image)
            draw.text((left + 15, top + 5), f"{width} px   /   {background}", font=label, fill=foreground)

    # Cùng tỉ lệ cho các lớp logo. Hai texture FX phóng lớn hơn để dễ soi alpha.
    draw.text((25, 909), "SPRITE LAYERS", font=label, fill="#EAFBFF")
    captions = ["Back", *"SQUEAKY", "Lane", "Bubble A", "Bubble B", "Spark"]
    thumbnails = [(caption, sprite, 0.35) for caption, (_, _, sprite, _) in zip(captions, artwork)]
    thumbnails.extend([("FX dot", dot, 0.65), ("FX trail", trail, 0.65)])
    caption_font = ImageFont.truetype(FONT, 18)
    x = 25
    for caption, sprite, scale in thumbnails:
        image = sprite.resize((round(sprite.width * scale), round(sprite.height * scale)), LANCZOS)
        cell_width = max(image.width + 10, round(caption_font.getlength(caption)) + 14, 54)
        draw.rounded_rectangle((x, 945, x + cell_width - 3, 1210), radius=7, fill="#243954")
        preview.paste(image, (x + (cell_width - image.width) // 2, 1068 - image.height // 2), image)
        draw.text((x + cell_width // 2, 1194), caption, font=caption_font, fill="#EAFBFF", anchor="mm")
        x += cell_width
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW, optimize=True)


def verify_layout(layout_file, full_file):
    layout = json.loads(layout_file.read_text(encoding="utf-8"))
    rebuilt = Image.new("RGBA", (layout["width"], layout["height"]))
    for piece in layout["pieces"]:
        with Image.open(OUTPUT / f"{piece['sprite']}.png") as sprite:
            w, h = piece["w"], piece["h"]
            if sprite.size != (w, h) or sprite.getchannel("A").getbbox() != (PAD, PAD, w - PAD, h - PAD):
                raise ValueError(f"Kích thước hoặc đệm sai: {piece['sprite']}")
            x = layout["width"] / 2 + piece["x"] - w / 2
            y = layout["height"] / 2 - piece["y"] - h / 2
            if int(x) != x or int(y) != y or not (0 <= x <= WIDTH - w and 0 <= y <= HEIGHT - h):
                raise ValueError(f"Vị trí sai: {piece['sprite']}")
            rebuilt.alpha_composite(sprite, (int(x), int(y)))
    with Image.open(full_file) as full:
        if rebuilt.size != full.size or rebuilt.tobytes() != full.tobytes():
            raise ValueError("Ghép PNG theo JSON lệch logo_full.png")


def main():
    artwork = make_pieces()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    layout = {"width": WIDTH, "height": HEIGHT, "pieces": []}
    full = Image.new("RGBA", (WIDTH, HEIGHT))
    assets = []

    def save_sprite(name, sprite):
        expected_bounds = PAD, PAD, sprite.width - PAD, sprite.height - PAD
        if sprite.getchannel("A").getbbox() != expected_bounds:
            raise ValueError(f"Đệm alpha không đủ 8 px: {name}")
        path = OUTPUT / f"{name}.png"
        sprite.save(path, optimize=True)
        assets.append(path)
        return path

    for name, role, sprite, (left, upper) in artwork:
        save_sprite(name, sprite)
        full.alpha_composite(sprite, (left, upper))
        w, h = sprite.size
        layout["pieces"].append({"sprite": name, "role": role, "x": left + (w - WIDTH) / 2,
                                 "y": (HEIGHT - h) / 2 - upper, "w": w, "h": h})

    layout_file = OUTPUT / "logo_layout.json"
    with layout_file.open("w", encoding="utf-8", newline="\n") as file:
        file.write(json.dumps(layout, indent=2) + "\n")
    full_file = save_sprite("logo_full", full)
    dot = particle(64, 64, dot_opacity)
    trail = particle(32, 192, trail_opacity)
    save_sprite("fx_dot", dot)
    save_sprite("fx_trail", trail)
    verify_layout(layout_file, full_file)
    size = sum(path.stat().st_size for path in [*assets, layout_file])
    if size >= 1_500_000:
        raise ValueError(f"Vượt ngân sách 1,5 MB: {size:,} bytes")
    make_preview(full, artwork, dot, trail)
    print(f"{len(assets)} PNG + JSON: {size:,} bytes. Layout matches logo_full.png pixel for pixel.")


if __name__ == "__main__":
    main()
