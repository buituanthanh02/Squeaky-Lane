"""Tạo tiếng hiệu ứng của Squeaky Lane bằng code (tự tổng hợp, không dùng bản ghi ngoài).

Chạy ở gốc repo:  python tools/make-sfx.py
Mọi tiếng bắt đầu và kết thúc ở biên độ 0 (không bụp), đỉnh thấp, dải cao đã cắt bớt cho êm.
Số ngẫu nhiên dùng hạt giống cố định nên chạy lại ra đúng file cũ.
"""
import os
import wave

import numpy as np

SR = 44100
OUT = 'Assets/_Project/Audio/Sfx'


def clock(seconds):
    return np.arange(int(SR * seconds)) / SR


def envelope(t, attack, release):
    total = len(t) / SR
    up = np.clip(t / attack, 0, 1)
    down = np.clip((total - t) / release, 0, 1)
    return np.sin(up * np.pi / 2) * np.sin(down * np.pi / 2)


def band(x, low, high, soft=0.35):
    """Lọc dải bằng FFT, hai mép chuyển mềm."""
    spectrum = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    keep = np.ones_like(f)
    if low > 0:
        keep *= np.clip((f - low * (1 - soft)) / (low * soft * 2), 0, 1)
    keep *= np.clip((high * (1 + soft) - f) / (high * soft * 2), 0, 1)
    return np.fft.irfft(spectrum * keep, len(x))


def sweep(t, start, end, curve=1.0):
    total = len(t) / SR
    f = start * (end / start) ** ((t / total) ** curve)
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def finish(x, peak):
    edge = int(SR * 0.004)
    x[:edge] *= np.linspace(0, 1, edge)
    x[-edge:] *= np.linspace(1, 0, edge)
    return x / np.abs(x).max() * peak


def move():
    """Xe lăn bánh: 'vrum' ngắn, trầm, có nhịp nổ máy nhẹ."""
    t = clock(0.26)
    f = 95 + 95 * (t / t[-1]) ** 0.6
    phase = 2 * np.pi * np.cumsum(f) / SR
    tone = np.sin(phase) + 0.5 * np.sin(2 * phase) + 0.22 * np.sin(3 * phase) + 0.1 * np.sin(4 * phase)
    tone *= 1 + 0.28 * np.sin(2 * np.pi * 30 * t)
    # Đầu tiếng có thêm họa âm ngắn để loa điện thoại tái tạo được ngay khi xe lăn.
    onset = (0.55 * np.sin(5 * phase) + 0.35 * np.sin(7 * phase)) * np.exp(-t / 0.04)
    return finish(band((tone + onset) * envelope(t, 0.006, 0.13), 0, 1200), 0.55)


def washing(seconds, low, high, bubbles, seed):
    """Nước xối kèm bọt nổi lách tách."""
    rng = np.random.default_rng(seed)
    t = clock(seconds)
    hiss = band(rng.normal(0, 1, len(t)), low, high)
    hiss /= np.abs(hiss).max()
    hiss *= 1 + 0.22 * np.sin(2 * np.pi * 6.5 * t) + 0.12 * np.sin(2 * np.pi * 11 * t + 1)
    x = 0.55 * hiss * envelope(t, 0.12, 0.32)
    for i in range(bubbles):
        length = rng.uniform(0.035, 0.07)
        bt = clock(length)
        f0 = rng.uniform(320, 760)
        pop = sweep(bt, f0, f0 * 1.9) * np.sin(np.pi * bt / bt[-1]) ** 2
        start = int((0.06 + (seconds - 0.25) * (i + rng.uniform(0, 0.8)) / bubbles) * SR)
        end = min(len(x), start + len(pop))
        x[start:end] += pop[:end - start] * rng.uniform(0.35, 0.65)
    return finish(band(x, 150, 5200), 0.5)


def tele():
    """Qua cổng tường: 'bụp' đi lên."""
    t = clock(0.18)
    x = sweep(t, 300, 780, 0.7) + 0.18 * sweep(t, 600, 1560, 0.7)
    return finish(x * np.clip(t / 0.01, 0, 1) * np.exp(-t / 0.07), 0.5)


def note(freq, seconds, decay):
    t = clock(seconds)
    x = np.sin(2 * np.pi * freq * t) + 0.22 * np.sin(2 * np.pi * freq * 2 * t) * np.exp(-t / (decay * 0.5))
    return x * np.clip(t / 0.008, 0, 1) * np.exp(-t / decay)


def melody(notes, step, tail, peak):
    length = step * (len(notes) - 1) + tail
    x = np.zeros(int(SR * length))
    for i, f in enumerate(notes):
        n = note(f, tail, tail * 0.28)
        start = int(i * step * SR)
        end = min(len(x), start + len(n))
        x[start:end] += n[:end - start]
    return finish(x, peak)


def tick():
    """Đếm ngược: tiếng gõ gỗ mềm."""
    t = clock(0.09)
    x = np.sin(2 * np.pi * 740 * t) + 0.25 * np.sin(2 * np.pi * 1480 * t) * np.exp(-t / 0.008)
    return finish(x * np.clip(t / 0.004, 0, 1) * np.exp(-t / 0.02), 0.38)


def mud():
    """Lội bùn: 'bõm' trầm."""
    rng = np.random.default_rng(5)
    t = clock(0.22)
    squelch = band(rng.normal(0, 1, len(t)), 90, 750)
    squelch /= np.abs(squelch).max()
    x = 0.55 * squelch * envelope(t, 0.015, 0.12) + sweep(t, 230, 75) * np.clip(t / 0.01, 0, 1) * np.exp(-t / 0.07)
    return finish(band(x, 0, 1100), 0.52)


SOUNDS = {
    'sfx_move': move(),
    'sfx_water_gate': washing(1.2, 500, 3400, 24, 11),
    'sfx_wash_station': washing(0.78, 700, 4000, 15, 23),
    'sfx_teleport': tele(),
    'sfx_stuck': melody([440, 349.2], 0.15, 0.3, 0.42),
    'sfx_lost': melody([523.3, 415.3, 329.6], 0.18, 0.42, 0.45),
    'sfx_countdown': tick(),
    'sfx_mud': mud(),
}

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for name, data in SOUNDS.items():
        with wave.open('%s/%s.wav' % (OUT, name), 'wb') as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SR)
            w.writeframes((np.clip(data, -1, 1) * 32767).astype(np.int16).tobytes())
        spectrum = np.abs(np.fft.rfft(data))
        centre = (spectrum * np.fft.rfftfreq(len(data), 1 / SR)).sum() / spectrum.sum()
        print('%-17s %.2fs peak %.2f start %.3f end %.3f centre %5.0f Hz' % (name, len(data) / SR, np.abs(data).max(), abs(data[0]), abs(data[-1]), centre))
