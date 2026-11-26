"""Vẽ bộ PNG combo và huy hiệu kỷ lục: python tools/make-combo-art.py."""

import math
from pathlib import Path

from PIL import Image, ImageColor, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / 'Assets/_Project/Art/UI'
OUTPUT = UI / 'Combo'
PREVIEW = ROOT / 'Logs/018/combo-preview.png'
BADGE_PREVIEW = ROOT / 'Logs/018/badge-preview.png'
SCALE = 3
BLUE = ((0, '#B9FFFF'), (.22, '#61EFFF'), (.48, '#2BB8FF'),
        (.50, '#1996F2'), (1, '#174FC9'))
GOLD = ((0, '#FFF8BA'), (.22, '#FFEA64'), (.48, '#FFC533'),
        (.50, '#FFAB20'), (1, '#EE681D'))


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


def ring_alpha(x, y):
    radius = math.hypot(x - .5, y - .5) * 2
    rim = 1 - smooth(abs(radius - .78) / .115)
    inner = .25 * (1 - smooth(abs(radius - .735) / .125))
    return min(1, rim * .9 + inner)


def badge_ribbons(image):
    for direction in (-1, 1):
        def ribbon(points):
            return badge_polygon(image, [(320 + direction * x, y) for x, y in points])

        # Vết cắt gấp thành chữ V, hai mép có sọc kim tuyến.
        paint(image, ribbon([(32, 492), (174, 457), (251, 747), (151, 720), (90, 800)]), '#500E2C')
        gradient(image, ribbon([(41, 499), (166, 470), (237, 732), (147, 708), (95, 778)]),
                 ((0, '#751135'), (.38, '#C2224A'), (.64, '#F04D62'), (1, '#A81743')))
        gradient(image, ribbon([(153, 473), (166, 470), (237, 732), (224, 728)]), GOLD)
        gradient(image, ribbon([(41, 499), (54, 496), (106, 763), (95, 778)]), GOLD)
        gradient(image, ribbon([(84, 501), (111, 490), (174, 713), (149, 708), (128, 734)]),
                 ((0, '#9F1641'), (.52, '#FA526E'), (1, '#CE2852')))


def badge_polygon(image, points):
    mask = Image.new('L', image.size)
    ImageDraw.Draw(mask).polygon([(x * SCALE, y * SCALE) for x, y in points], fill=255)
    return mask


def badge_rosette(image):
    def scallop(radius, depth, center_y):
        points = []
        for step in range(960):
            angle = math.tau * step / 960
            edge = radius + depth * math.cos(angle * 20)
            points.append((320 + edge * math.sin(angle), center_y - edge * math.cos(angle)))
        return badge_polygon(image, points)

    def disc(radius, center_y):
        mask = Image.new('L', image.size)
        bounds = (320 - radius, center_y - radius, 320 + radius, center_y + radius)
        ImageDraw.Draw(mask).ellipse(tuple(value * SCALE for value in bounds), fill=255)
        return mask

    paint(image, scallop(273, 10, 316), '#4A0D2B')
    gradient(image, scallop(273, 10, 303),
             ((0, '#FF9B9C'), (.24, '#F2526A'), (.55, '#AA1B43'), (1, '#FF667C')))
    gradient(image, scallop(262, 10, 303),
             ((0, '#FF6E89'), (.26, '#E83B55'), (.53, '#AF1742'), (.8, '#D83051'), (1, '#99143C')))

    # Gờ bóng chạy theo từng nếp gấp, sáng dần về nửa trên.
    for petal in range(20):
        angle = math.tau * petal / 20
        base = [(237, -.09), (260, -.057), (271, -.018), (270, .016), (244, -.017)]
        fold = [(320 + radius * math.sin(angle + offset),
                 303 - radius * math.cos(angle + offset)) for radius, offset in base]
        gloss = Image.new('RGBA', image.size)
        paint(gloss, badge_polygon(image, fold), '#FFAC9F')
        gloss.putalpha(gloss.getchannel('A').point(
            lambda value: round(value * (.10 + .21 * (.5 + .5 * math.cos(angle))))))
        image.alpha_composite(gloss)

    paint(image, disc(234, 307), '#60112B')
    gradient(image, disc(229, 301),
             ((0, '#FFFFBC'), (.26, '#FFDA66'), (.50, '#DF781B'), (.78, '#9B3E18'), (1, '#FFB947')))
    gradient(image, disc(218, 301),
             ((0, '#FFCE53'), (.18, '#FFF39A'), (.38, '#FFD64A'), (.57, '#EE9B22'),
              (.79, '#FFBF35'), (1, '#FFF18B')))
    gradient(image, disc(204, 301), ((0, '#7D1237'), (.5, '#B83B22'), (1, '#FFF0A6')))
    gradient(image, disc(196, 301),
             ((0, '#A41C41'), (.46, '#811237'), (.80, '#A51944'), (1, '#C3294C')))


def badge_crown(image):
    crown = [(244, 167), (282, 186), (320, 142), (358, 186), (396, 167),
             (380, 224), (260, 224)]
    outline = [(236, 161), (280, 175), (320, 132), (360, 175), (404, 161),
               (386, 233), (254, 233)]
    paint(image, badge_polygon(image, outline), '#550D2A')
    gradient(image, badge_polygon(image, crown), GOLD)
    paint(image, badge_polygon(image, [(250, 171), (282, 186), (320, 142),
                                       (358, 186), (390, 171), (357, 194), (320, 153),
                                       (283, 194)]), '#FFF5AF')
    paint(image, badge_polygon(image, [(263, 212), (377, 212), (374, 220), (266, 220)]), '#FFE774')
    gem = [(320, 171), (334, 188), (320, 203), (306, 188)]
    gradient(image, badge_polygon(image, gem), ((0, '#FFF3A3'), (.52, '#FAAC2A'), (1, '#CE591C')))


def badge_text(image, label, size, top):
    face = font(size)
    left, upper, right, _ = face.getbbox(label)
    x = (image.width - (right + left)) // 2
    y = top * SCALE - upper

    def shape(stroke, down=0):
        mask = Image.new('L', image.size)
        ImageDraw.Draw(mask).text((x, y + down * SCALE), label, font=face,
                                  fill=255, stroke_width=stroke * SCALE)
        return mask

    paint(image, shape(7, 6), '#460D29')
    paint(image, shape(7), '#5B102D')
    paint(image, shape(2), '#EF8D26')
    gradient(image, shape(0),
             ((0, '#FFFCC0'), (.30, '#FFEE89'), (.51, '#FFDD58'), (.54, '#FFC63E'), (1, '#FFAA26')))


def record_badge():
    width, height = 640, 820
    image = Image.new('RGBA', (width * SCALE, height * SCALE))
    badge_ribbons(image)
    badge_rosette(image)
    badge_crown(image)
    badge_text(image, 'KỶ LỤC', 109, 248)
    badge_text(image, 'MỚI!', 128, 352)
    return image.resize((width, height), Image.Resampling.LANCZOS)


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
        burst, star, ring = (assets['fx_' + name] for name in ('burst', 'spark', 'ring'))
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

        record = at_height(assets['record_badge'], 187)
        card.alpha_composite(record, ((card.width - record.width) // 2, 266))

        draw = ImageDraw.Draw(card)
        for i, (name, image, height) in enumerate((('Burst', burst, 110), ('Spark', star, 74),
                                                   ('Ring', ring, 94))):
            fx = at_height(image, height)
            center = 165 + i * 165
            card.alpha_composite(fx, (center - fx.width // 2, 500 - fx.height // 2))
            draw.text((center, 576), name, anchor='mm', font=font(7), fill='#172244')
        preview.alpha_composite(card, (panel * card.width, 0))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.convert('RGB').save(PREVIEW, optimize=True)


def make_badge_preview(badge):
    panel_width = 1024
    preview = Image.new('RGBA', (panel_width * 2, 900))
    for panel, background in enumerate(('#6236E5', '#7A86B8')):
        x = panel * panel_width
        preview.paste(background, (x, 0, x + panel_width, preview.height))
        preview.alpha_composite(badge, (x + 24, 40))
        preview.alpha_composite(at_height(badge, 330), (x + 708, 285))
        ImageDraw.Draw(preview).text((x + 837, 646), '330 px', anchor='mm',
                                     font=font(7), fill='white')
    BADGE_PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.convert('RGB').save(BADGE_PREVIEW, optimize=True)


def main():
    navy = ('#152758', '#0D1A3A', '#2060B5')
    amber = ('#35214B', '#241536', '#BA491C')
    assets = {'combo_word': lettering('Combo', 230, 256, 197, .10, 0, 13, BLUE, navy, 9)}
    for number in range(10):
        assets[f'combo_digit_{number}'] = lettering(str(number), 316, 320, 263, .16, 0, 15, GOLD, amber, 6)
    assets['record_badge'] = record_badge()
    assets['fx_burst'] = white_sprite((512, 512), burst_alpha)
    assets['fx_spark'] = spark()
    assets['fx_ring'] = white_sprite((512, 512), ring_alpha)

    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, image in assets.items():
        image.save(OUTPUT / f'{name}.png', optimize=True)
    make_preview(assets)
    make_badge_preview(assets['record_badge'])


if __name__ == '__main__':
    main()
