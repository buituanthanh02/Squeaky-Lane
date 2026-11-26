"""Vẽ lại bộ PNG 018-a bằng Pillow: python tools/make-combo-art.py."""

import math
from pathlib import Path

from PIL import Image, ImageColor, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / 'Assets/_Project/Art/UI'
OUTPUT = UI / 'Combo'
PREVIEW = ROOT / 'Logs/018/combo-preview.png'
SCALE = 3
BLUE = ((0, '#B9FFFF'), (.22, '#61EFFF'), (.48, '#2BB8FF'),
        (.50, '#1996F2'), (1, '#174FC9'))
GOLD = ((0, '#FFF8BA'), (.22, '#FFEA64'), (.48, '#FFC533'),
        (.50, '#FFAB20'), (1, '#EE681D'))
RECORD = ((0, '#FFB6CB'), (.22, '#FF658B'), (.49, '#F92A68'),
          (.51, '#FA5A46'), (1, '#FFD553'))


def font(size):
    return ImageFont.truetype(str(UI / 'Fonts/Baloo2-ExtraBold.ttf'), size * SCALE)


def paint(image, mask, color):
    layer = Image.new('RGBA', image.size, color)
    layer.putalpha(mask)
    image.alpha_composite(layer)


def gradient(image, mask, stops):
    _, top, _, bottom = mask.getbbox()
    palette = [(position, ImageColor.getrgb(color)) for position, color in stops]
    strip = Image.new('RGB', (1, bottom - top))
    for y in range(strip.height):
        position = y / (strip.height - 1)
        for (start, low), (end, high) in zip(palette, palette[1:]):
            if position <= end:
                t = (position - start) / (end - start)
                strip.putpixel((0, y), tuple(round(a + (b - a) * t) for a, b in zip(low, high)))
                break
    layer = Image.new('RGBA', image.size)
    layer.paste(strip.resize((image.width, strip.height)), (0, top))
    layer.putalpha(mask)
    image.alpha_composite(layer)


def lettering(label, size, height, baseline, slant, angle, outline, stops, palette, padding):
    face = font(size)
    width = math.ceil(face.getlength(label) / SCALE) + 100
    canvas = (width * SCALE, height * SCALE)
    image = Image.new('RGBA', canvas)
    x = 34 * SCALE
    y = baseline * SCALE
    edge, depth, inner = palette

    def shape(stroke=0, down=0):
        mask = Image.new('L', canvas)
        ImageDraw.Draw(mask).text((x, y + down * SCALE), label, anchor='ls', font=face,
                                  fill=255, stroke_width=stroke * SCALE)
        return mask.transform(canvas, Image.Transform.AFFINE, (1, slant, -slant * y, 0, 1, 0),
                              resample=Image.Resampling.BICUBIC)

    paint(image, shape(outline + 1, 13).filter(ImageFilter.GaussianBlur(3 * SCALE))
          .point(lambda a: a * 115 // 255), '#182044')
    paint(image, shape(outline, 9), depth)
    paint(image, shape(outline), edge)
    paint(image, shape(outline - 4), '#FFFFFF')
    paint(image, shape(2), inner)
    face_mask = shape()
    gradient(image, face_mask, stops)
    if angle:
        image = image.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)
        image = image.crop(image.getbbox())
        centered = Image.new('RGBA', (image.width, height * SCALE))
        assert image.height <= centered.height - 4 * SCALE, label
        centered.alpha_composite(image, (0, (centered.height - image.height) // 2))
        image = centered

    # Cắt ngang theo alpha, giữ nguyên baseline và cùng khoảng đệm cho mọi số.
    left, top, right, bottom = image.getbbox()
    assert 0 < top < bottom < canvas[1], label
    left = left // SCALE * SCALE
    right = math.ceil(right / SCALE) * SCALE
    cropped = image.crop((left, 0, right, canvas[1]))
    result = Image.new('RGBA', (cropped.width + padding * 2 * SCALE, canvas[1]))
    result.alpha_composite(cropped, (padding * SCALE, 0))
    return result.resize((result.width // SCALE, height), Image.Resampling.LANCZOS)


def smooth(value):
    t = max(0, min(1, value))
    return t * t * (3 - 2 * t)


def white_sprite(size, alpha_at):
    width, height = size
    alpha = Image.new('L', size)
    alpha.putdata(bytes(round(255 * alpha_at((x + .5) / width, (y + .5) / height))
                        for y in range(height) for x in range(width)))
    image = Image.new('RGBA', size, 'white')
    image.putalpha(alpha)
    return image


def burst_alpha(x, y):
    x, y = (x - .5) * 2, (y - .5) * 2
    radius = math.hypot(x, y)
    angle = math.atan2(y, x)
    ray = (.5 + .5 * math.cos(angle * 16 + .42)) ** 5
    length = .79 + .16 * math.cos(angle * 5 + .9)
    falloff = 1 - smooth(radius / length)
    halo = .57 * math.exp(-5 * radius * radius)
    return min(1, (halo + ray * falloff * .9) * (1 - smooth((radius - .79) / .18)))


def spark():
    size = 128 * SCALE
    mask = Image.new('L', (size, size))
    points = []
    for i in range(360):
        angle = math.tau * i / 360
        x, y = math.cos(angle), math.sin(angle)
        points.append((size * (.5 + .415 * x * abs(x) ** 2),
                       size * (.5 + .47 * y * abs(y) ** 2)))
    ImageDraw.Draw(mask).polygon(points, fill=255)
    result = Image.new('RGBA', (128, 128), 'white')
    result.putalpha(mask.resize((128, 128), Image.Resampling.LANCZOS))
    return result


def streak_alpha(x, y):
    if x >= .97:
        return 0
    tail = smooth((x - .015) / .87)
    width = (.07 + .13 * x) * math.sqrt(max(0, 1 - ((max(0, x - .89)) / .08) ** 2))
    return tail * (1 - smooth((abs(y - .5) - width * .08) / (width * .92)))


def ring_alpha(x, y):
    radius = math.hypot(x - .5, y - .5) * 2
    rim = 1 - smooth(abs(radius - .78) / .115)
    inner = .25 * (1 - smooth(abs(radius - .735) / .125))
    return min(1, rim * .9 + inner)


def at_height(image, height):
    return image.resize((round(image.width * height / image.height), height), Image.Resampling.LANCZOS)


def tinted(image, color):
    result = Image.new('RGBA', image.size, color)
    result.putalpha(image.getchannel('A'))
    return result


def make_preview(assets):
    preview = Image.new('RGBA', (1320, 620))
    for panel, background in enumerate(('#7A86B8', '#A6C860')):
        card = Image.new('RGBA', (660, 620), background)
        burst, star, streak, ring = (assets['fx_' + name] for name in ('burst', 'spark', 'streak', 'ring'))
        word = at_height(assets['combo_word'], 112)
        three = at_height(assets['combo_digit_3'], 164)
        one = at_height(assets['combo_digit_1'], 164)
        x = (card.width - word.width - three.width - one.width) // 2
        card.alpha_composite(tinted(at_height(burst, 286), '#FFF09A'), (x + word.width - 66, -48))
        card.alpha_composite(word, (x, 40))
        card.alpha_composite(three, (x + word.width, 10))
        card.alpha_composite(one, (x + word.width + three.width, 10))
        card.alpha_composite(at_height(star, 32), (x + word.width + three.width + one.width - 3, 31))
        card.alpha_composite(at_height(star, 21), (x + word.width - 7, 134))

        digits = [at_height(assets[f'combo_digit_{number}'], 90) for number in range(10)]
        x = (card.width - sum(digit.width for digit in digits)) // 2
        for digit in digits:
            card.alpha_composite(digit, (x, 180))
            x += digit.width

        record = at_height(assets['record_new'], 150)
        card.alpha_composite(record, ((card.width - record.width) // 2, 281))

        draw = ImageDraw.Draw(card)
        for i, (name, image, height) in enumerate((('Burst', burst, 110), ('Spark', star, 74),
                                                   ('Streak', streak, 36), ('Ring', ring, 94))):
            fx = at_height(image, height)
            center = 90 + i * 160
            card.alpha_composite(fx, (center - fx.width // 2, 500 - fx.height // 2))
            draw.text((center, 576), name, anchor='mm', font=font(7), fill='#172244')
        preview.alpha_composite(card, (panel * card.width, 0))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.convert('RGB').save(PREVIEW, optimize=True)


def main():
    navy = ('#152758', '#0D1A3A', '#2060B5')
    amber = ('#35214B', '#241536', '#BA491C')
    berry = ('#522247', '#361631', '#B02B5D')
    assets = {'combo_word': lettering('Combo', 230, 256, 197, .10, 0, 13, BLUE, navy, 9)}
    for number in range(10):
        assets[f'combo_digit_{number}'] = lettering(str(number), 316, 320, 263, .16, 0, 15, GOLD, amber, 6)
    assets['record_new'] = lettering('KỶ LỤC MỚI!', 143, 300, 215, .11, 5, 10, RECORD, berry, 12)
    assets['fx_burst'] = white_sprite((512, 512), burst_alpha)
    assets['fx_spark'] = spark()
    assets['fx_streak'] = white_sprite((512, 128), streak_alpha)
    assets['fx_ring'] = white_sprite((512, 512), ring_alpha)

    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, image in assets.items():
        image.save(OUTPUT / f'{name}.png', optimize=True)
    make_preview(assets)


if __name__ == '__main__':
    main()
