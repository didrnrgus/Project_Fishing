"""
인트로 아트 생성기: 사업자 로고(제쓰로네 게임공방)와 타이틀(청풍명월) 화면의 그림, RESULT 로고·도감 물속 배경을 만든다.
결과는 Assets/Resources/Intro/*.png (IntroSequence.cs가 불러 움직인다).

    bpyenv/Scripts/python.exe -I tools/intro/build_intro_art.py [--preview 폴더]

타이틀 배경은 달밤의 수묵 산수화를 겹(레이어)으로 나눠 그린다. 겹마다 따로 움직여서(시차·물안개·물빛)
한 장짜리 그림보다 살아 있어 보인다. 글꼴은 모두 SIL OFL (tools/intro/fonts/OFL-*.txt).
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Intro")
FONTS = os.path.join(HERE, "fonts")

# 배경 겹은 1920×1080 화면보다 조금 크게(시차로 움직일 여유) 그린다
W, H = 2048, 1152
HORIZON = 0.635          # 수평선 높이 (위에서부터 비율)
MOON = (0.70, 0.235)     # 달 중심 (비율)
MOON_R = 92              # 달 반지름 (px, 2048 폭 기준)

rng = np.random.default_rng(7)


def font(name, size, variation=None):
    f = ImageFont.truetype(os.path.join(FONTS, name), size)
    if variation:
        try:
            f.set_variation_by_name(variation)
        except Exception:
            pass
    return f


# ---------------------------------------------------------------- 노이즈

def value_noise_2d(h, w, cell, seed, wrap_x=False):
    """부드러운 값 노이즈 (격자 cell px). 0~1. wrap_x면 가로로 이어 붙는다 (w가 cell의 배수일 때)."""
    r = np.random.default_rng(seed)
    gh, gw = h // cell + 3, w // cell + 3
    g = r.random((gh, gw)).astype(np.float32)
    if wrap_x:
        period = max(1, w // cell)
        g = g[:, np.arange(gw) % period]
    ys = np.arange(h, dtype=np.float32) / cell
    xs = np.arange(w, dtype=np.float32) / cell
    y0 = ys.astype(int); x0 = xs.astype(int)
    fy = ys - y0; fx = xs - x0
    fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
    a = g[y0][:, x0]; b = g[y0][:, x0 + 1]
    c = g[y0 + 1][:, x0]; d = g[y0 + 1][:, x0 + 1]
    top = a + (b - a) * fx[None, :]
    bot = c + (d - c) * fx[None, :]
    return top + (bot - top) * fy[:, None]


def fbm_2d(h, w, cell, octaves, seed, gain=0.5, wrap_x=False):
    total = np.zeros((h, w), np.float32); amp = 1.0; norm = 0.0
    for o in range(octaves):
        c = max(2, int(cell / (2 ** o)))
        if wrap_x:
            c = w // max(1, round(w / c))   # w를 나누어떨어지게
        total += value_noise_2d(h, w, c, seed + o * 17, wrap_x) * amp
        norm += amp; amp *= gain
    return total / norm


def noise_1d(n, cell, seed):
    r = np.random.default_rng(seed)
    g = r.random(n // cell + 3)
    xs = np.arange(n) / cell
    x0 = xs.astype(int); f = xs - x0
    f = f * f * (3 - 2 * f)
    return g[x0] + (g[x0 + 1] - g[x0]) * f


def fbm_1d(n, cell, octaves, seed, gain=0.5):
    t = np.zeros(n); amp = 1.0; norm = 0.0
    for o in range(octaves):
        t += noise_1d(n, max(2, int(cell / 2 ** o)), seed + o * 31) * amp
        norm += amp; amp *= gain
    return t / norm


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def to_img(rgb, alpha=None):
    rgb = np.clip(rgb, 0, 1)
    if alpha is None:
        return Image.fromarray((rgb * 255 + 0.5).astype(np.uint8), "RGB")
    a = np.clip(alpha, 0, 1)
    arr = np.dstack([rgb, a[..., None]])
    return Image.fromarray((arr * 255 + 0.5).astype(np.uint8), "RGBA")


def dither(h, w, amount=1.0 / 255):
    return (rng.random((h, w, 1)).astype(np.float32) - 0.5) * amount * 2


def hexc(s):
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)


# 달빛 수묵 팔레트
SKY_TOP = hexc("#04070f")
SKY_MID = hexc("#0d1a2e")
SKY_LOW = hexc("#2c4157")
MOON_GLOW = hexc("#9fb8c9")
INK_FAR = hexc("#3d5266")
INK_MID = hexc("#22323f")
INK_NEAR = hexc("#0d151d")
IVORY = hexc("#f1e9d6")
SEAL_RED = hexc("#b3242a")

# ---------------------------------------------------------------- 하늘

def sky():
    ys = np.linspace(0, 1, H, dtype=np.float32)[:, None, None]
    hz = HORIZON
    t = np.clip(ys / hz, 0, 1)
    col = np.where(t < 0.55, SKY_TOP + (SKY_MID - SKY_TOP) * (t / 0.55),
                   SKY_MID + (SKY_LOW - SKY_MID) * np.clip((t - 0.55) / 0.45, 0, 1) ** 1.4)
    col = np.broadcast_to(col, (H, W, 3)).copy()
    # 달 둘레 달무리
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    mx, my = MOON[0] * W, MOON[1] * H
    d = np.sqrt((xx - mx) ** 2 + (yy - my) ** 2) / W
    glow = np.exp(-d * 5.5) * 0.55 + np.exp(-d * 18) * 0.35
    col += MOON_GLOW[None, None, :] * glow[..., None] * 0.55
    # 엷은 구름결 (하늘에 붓 자국처럼)
    n = fbm_2d(H, W, 260, 5, 11)
    streak = fbm_2d(H, W // 4, 60, 3, 12)
    streak = np.repeat(streak, 4, axis=1)[:, :W]
    cloud = smoothstep(0.52, 0.8, n * 0.7 + streak * 0.3) * (1 - smoothstep(hz - 0.1, hz, ys[..., 0]))
    col += (MOON_GLOW * 0.25)[None, None, :] * cloud[..., None] * (0.4 + glow[..., None])
    # 별 (달 근처와 아래쪽은 적게)
    stars = np.zeros((H, W), np.float32)
    count = 900
    sx = rng.integers(0, W, count); sy = (rng.random(count) ** 1.6 * H * hz * 0.9).astype(int)
    for x, y in zip(sx, sy):
        if math.hypot(x - mx, y - my) < MOON_R * 3.2:
            continue
        b = rng.random() ** 3 * 0.9 + 0.08
        stars[y, x] = max(stars[y, x], b)
    star_img = Image.fromarray((stars * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))
    st = np.asarray(star_img, np.float32) / 255 * 2.2
    big = np.zeros((H, W), np.float32)
    for _ in range(26):
        x, y = rng.integers(40, W - 40), int(rng.random() ** 1.3 * H * hz * 0.75)
        if math.hypot(x - mx, y - my) < MOON_R * 4:
            continue
        big[y, x] = 1
    big = np.asarray(Image.fromarray((big * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6)), np.float32) / 255 * 9
    col += (st + np.clip(big, 0, 1))[..., None] * hexc("#dfe8f0")[None, None, :] * (1 - cloud[..., None] * 0.7)
    col += dither(H, W, 1.5 / 255)
    return to_img(col)


def moon():
    """달 (따로 그려서 은은하게 숨쉬듯 밝아지게 한다). 512 정사각, 가운데 달 + 무리."""
    S = 512
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    c = S / 2
    r = MOON_R * 1.0
    d = np.sqrt((xx - c) ** 2 + (yy - c) ** 2)
    disk = 1 - smoothstep(r - 1.5, r + 1.0, d)
    maria = fbm_2d(S, S, 70, 4, 21)
    craters = fbm_2d(S, S, 18, 3, 22)
    shade = 1.0 - smoothstep(0.5, 0.78, maria) * 0.10 - smoothstep(0.64, 0.82, craters) * 0.03
    # 가장자리로 갈수록 살짝 어둡게 (구 느낌)
    limb = np.sqrt(np.clip(1 - (d / r) ** 2, 0, 1))
    shade = shade * (0.9 + 0.1 * limb)
    moon_col = hexc("#fff8e6")[None, None, :] * shade[..., None]
    halo = np.exp(-np.maximum(d - r, 0) / (r * 0.3)) * 0.6 + np.exp(-np.maximum(d - r, 0) / (r * 1.1)) * 0.22
    halo *= (1 - disk) * (1 - smoothstep(S * 0.32, S * 0.49, d))
    rgb = moon_col * disk[..., None] + hexc("#e4eef2")[None, None, :] * (1 - disk[..., None])
    a = np.clip(disk + halo, 0, 1)
    return to_img(rgb, a)


def wisps():
    """달 앞을 지나는 엷은 구름 띠 (옆으로 천천히 흐른다). 가로로 이어 붙는다."""
    w, h = 2048, 512
    # 가로로 길게 늘인 구름결 (좁게 만들어 가로로 늘린다)
    stretch = fbm_2d(h, w // 4, 64, 5, 31, wrap_x=True)
    stretch = np.asarray(Image.fromarray((stretch * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), np.float32) / 255
    ys = np.linspace(-1, 1, h)[:, None]
    band = np.exp(-(ys / 0.42) ** 2)
    a = smoothstep(0.5, 0.78, stretch) * band * 0.55
    rgb = np.broadcast_to(hexc("#a9bccb"), (h, w, 3))
    return to_img(rgb, a)


# ---------------------------------------------------------------- 산

def ridge(n, base, amp, seed, peaks, sharp=1.0):
    """수묵 산수화 봉우리 능선 (y px, 위가 0). base: 산 밑 높이, amp: 높이."""
    x = np.arange(n, dtype=np.float32)
    h = fbm_1d(n, 340, 6, seed) * 0.45
    r = np.random.default_rng(seed)
    for _ in range(peaks):
        px = r.uniform(-0.05, 1.05) * n
        pw = r.uniform(0.04, 0.12) * n
        ph = r.uniform(0.35, 1.0)
        # 뾰족하고 어깨가 둥근 봉우리 (금강산 같은)
        t = np.abs(x - px) / pw
        h = np.maximum(h, ph * np.exp(-(t ** (1.2 * sharp))) )
    h += (fbm_1d(n, 24, 3, seed + 5) - 0.5) * 0.05   # 잔 굴곡
    return base - h * amp


def mountain_layer(ridges, colors, mist_top, mist_alpha, seed, tex=0.5):
    """ridges: 뒤→앞 능선 배열. 능선 바로 아래가 가장 짙고(먹이 몰림) 아래로 갈수록 안개에 녹는다."""
    yy = np.arange(H, dtype=np.float32)[:, None]
    rgb = np.zeros((H, W, 3), np.float32)
    alpha = np.zeros((H, W), np.float32)
    brush = fbm_2d(H, W, 90, 5, seed)
    # 세로 붓결 (준법 비슷하게)
    vert = fbm_2d(H // 6, W, 14, 3, seed + 1)
    vert = np.asarray(Image.fromarray((vert * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), np.float32) / 255
    for rl, col in zip(ridges, colors):
        top = rl[None, :]
        inside = smoothstep(top - 1.2, top + 1.2, yy)
        depth = np.clip((yy - top) / 260.0, 0, 1)
        # 아래로 갈수록 엷어짐 + 안개
        fade = 1 - smoothstep(mist_top - 120, mist_top + 40, yy) * mist_alpha
        ink = (1 - depth * 0.55) * (0.78 + 0.22 * brush) * (0.85 + 0.15 * vert * tex * 2)
        c = col[None, None, :] * (0.75 + 0.25 * (1 - ink[..., None])) + (SKY_LOW * 0.25)[None, None, :] * depth[..., None]
        a = inside * fade * (1 - smoothstep(HORIZON * H - 6, HORIZON * H + 1, yy))
        rgb = rgb * (1 - a[..., None]) + c * a[..., None]
        alpha = alpha + a * (1 - alpha)
    return rgb, alpha


def mountains_far():
    rs, cs = [], []
    hz = HORIZON * H
    specs = [
        (hz - 40, 330, 101, 9, INK_FAR * 1.18),
        (hz - 10, 260, 102, 8, INK_FAR * 1.0),
        (hz + 8, 170, 103, 10, INK_FAR * 0.86),
    ]
    for base, amp, seed, peaks, col in specs:
        rl = ridge(W, base, amp, seed, peaks)
        # 달 바로 아래는 낮게 (달과 겹치지 않게)
        mx = MOON[0] * W
        x = np.arange(W)
        dip = np.exp(-((x - mx) / (W * 0.08)) ** 2) * amp * 0.35
        rs.append(rl + dip); cs.append(col)
    rgb, a = mountain_layer(rs, cs, hz - 30, 0.85, 201)
    return to_img(rgb, a)


def pine(draw, x, y, s, col, r):
    """수묵 소나무 실루엣: 휜 줄기 + 납작한 솔잎 덩이."""
    lean = r.uniform(-0.5, 0.5)
    pts = []
    for i in range(12):
        t = i / 11
        pts.append((x + math.sin(t * 2.2 + lean) * s * 0.18 * lean * 3 + lean * t * s * 0.4, y - t * s))
    for i in range(len(pts) - 1):
        wd = max(1, int(s * 0.05 * (1 - i / 14)))
        draw.line([pts[i], pts[i + 1]], fill=col, width=wd)
    tx, ty = pts[-1]
    for k in range(r.integers(4, 7)):
        t = 0.45 + k * 0.11
        if t > 1:
            break
        i = min(len(pts) - 1, int(t * 11))
        bx, by = pts[i]
        side = 1 if k % 2 == 0 else -1
        L = s * r.uniform(0.22, 0.38) * (1.1 - t * 0.5)
        cx = bx + side * L * 0.6
        draw.line([(bx, by), (cx, by - s * 0.03)], fill=col, width=max(1, int(s * 0.02)))
        for j in range(5):
            ex = cx + r.uniform(-L * 0.6, L * 0.6)
            ey = by - s * 0.03 + r.uniform(-s * 0.04, s * 0.02)
            draw.ellipse([ex - L * 0.32, ey - s * 0.035, ex + L * 0.32, ey + s * 0.03], fill=col)
    draw.ellipse([tx - s * 0.16, ty - s * 0.05, tx + s * 0.16, ty + s * 0.04], fill=col)


def mountains_near():
    """양옆에서 화면을 감싸는 가까운 바위 산 + 소나무."""
    hz = HORIZON * H
    x = np.arange(W, dtype=np.float32)
    # 왼쪽은 높은 절벽, 오른쪽은 낮은 언덕
    left = hz - 360 * np.exp(-((x - W * 0.02) / (W * 0.20)) ** 2) - 110 * np.exp(-((x - W * 0.22) / (W * 0.08)) ** 2)
    right = hz - 210 * np.exp(-((x - W * 1.0) / (W * 0.17)) ** 2) - 60 * np.exp(-((x - W * 0.86) / (W * 0.05)) ** 2)
    rl = np.minimum(left, right) + (fbm_1d(W, 60, 4, 301) - 0.5) * 26
    rl = np.where(np.minimum(left, right) > hz - 12, hz + 40, rl)     # 산이 없는 가운데는 비운다
    rgb, a = mountain_layer([rl], [INK_NEAR * 1.25], hz + 30, 0.6, 302, tex=0.8)
    img = to_img(rgb, a)
    # 능선 위 소나무
    d = ImageDraw.Draw(img)
    r = np.random.default_rng(303)
    col = tuple(int(v * 255) for v in INK_NEAR) + (255,)
    for _ in range(90):
        px = int(r.uniform(0, W))
        if rl[px] > hz - 20:
            continue
        s = r.uniform(26, 70) * (1.0 + (hz - rl[px]) / 500)
        pine(d, px, rl[px] + 6, s, col, r)
    # 왼쪽 절벽 끝의 큰 소나무 (그림의 주인공)
    pine(d, W * 0.205, left[int(W * 0.205)] + 10, 170, col, np.random.default_rng(9))
    return img


# ---------------------------------------------------------------- 물

def water(sky_img, far_img, near_img):
    """수면: 하늘·산 그림자가 비친 물 + 가로 물결."""
    hz = int(HORIZON * H)
    above = Image.alpha_composite(Image.alpha_composite(sky_img.convert("RGBA"), far_img), near_img)
    arr = np.asarray(above, np.float32)[..., :3] / 255
    refl = arr[:hz][::-1]
    hh = H - hz
    refl = refl[:hh] if refl.shape[0] >= hh else np.pad(refl, ((0, hh - refl.shape[0]), (0, 0), (0, 0)), mode="edge")
    # 물결로 흔든 반사 (가로 흔들림 + 세로 늘임)
    yy, xx = np.mgrid[0:hh, 0:W].astype(np.float32)
    wob = (fbm_2d(hh, W // 8, 6, 3, 401) - 0.5)
    wob = np.asarray(Image.fromarray(((wob + 0.5) * 255).astype(np.uint8)).resize((W, hh), Image.BICUBIC), np.float32) / 255 - 0.5
    depth = yy / hh
    sx = np.clip(xx + wob * (6 + depth * 40), 0, W - 1).astype(int)
    sy = np.clip(yy * (1 + wob * 0.05), 0, hh - 1).astype(int)
    r = refl[sy, sx]
    r = np.asarray(Image.fromarray((np.clip(r, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2.5)), np.float32) / 255
    base = hexc("#06101a")
    col = base * 0.45 + r * 0.55 * (1 - depth[..., None] * 0.55)
    # 가로 물결 무늬
    lines = fbm_2d(hh, W // 16, 3, 2, 402)
    lines = np.asarray(Image.fromarray((lines * 255).astype(np.uint8)).resize((W, hh), Image.BICUBIC), np.float32) / 255
    col += (smoothstep(0.62, 0.9, lines) * 0.05 * (0.4 + depth))[..., None] * MOON_GLOW
    col *= (1 - depth[..., None] * 0.35)
    full = np.zeros((H, W, 3), np.float32)
    full[hz:] = col
    full[:hz] = col[0]
    full += dither(H, W, 1.5 / 255)
    ys = np.arange(H, dtype=np.float32)[:, None]
    a = np.broadcast_to(smoothstep(hz - 6, hz + 4, ys), (H, W))
    return to_img(full, a)


def moon_path():
    """달이 물에 비친 길 (반짝이는 가로 조각들). 더해 그리기(additive)용, 검은 바탕."""
    w, h = 512, 512
    img = np.zeros((h, w), np.float32)
    r = np.random.default_rng(501)
    for _ in range(1400):
        t = r.random() ** 0.8           # 0 = 수평선, 1 = 화면 아래
        y = int(t * (h - 4))
        spread = 14 + t * 120
        x = int(w / 2 + r.normal(0, spread * 0.55))
        L = int(4 + t * 34 * r.uniform(0.3, 1.2))
        b = (1 - t * 0.55) * r.uniform(0.25, 1.0)
        x0, x1 = max(0, x - L // 2), min(w, x + L // 2)
        th = 1 + int(t * 2.5)
        img[y:y + th, x0:x1] = np.maximum(img[y:y + th, x0:x1], b)
    img = np.asarray(Image.fromarray((img * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.1)), np.float32) / 255 * 1.6
    xs = np.linspace(-1, 1, w)[None, :]
    ys = np.linspace(0, 1, h)[:, None]
    glow = np.exp(-(xs / (0.08 + ys * 0.25)) ** 2) * (1 - ys * 0.6) * 0.35
    v = np.clip(img + glow, 0, 1)
    rgb = hexc("#fff3d6")[None, None, :] * v[..., None]
    return to_img(rgb, v)


def mist():
    """수평선 물안개 띠 (가로로 이어 붙음, 천천히 흐른다)."""
    w, h = 2048, 384
    s = fbm_2d(h, w // 4, 64, 5, 601, wrap_x=True)
    s = np.asarray(Image.fromarray((s * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), np.float32) / 255
    ys = np.linspace(-1, 1, h)[:, None]
    band = np.exp(-(ys / 0.45) ** 2)
    a = np.clip((s - 0.3) * 1.6, 0, 1) * band * 0.75
    rgb = np.broadcast_to(hexc("#8ea4b6"), (h, w, 3))
    return to_img(rgb, a)


# ---------------------------------------------------------------- 배, 갈대

def boat():
    """작은 쪽배에 앉은 낚시꾼 실루엣 + 휜 낚싯대·낚싯줄. 아래 가운데가 수면."""
    w, h = 640, 400
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    ink = (10, 16, 24, 255)
    wl = 330  # 수면선
    # 배: 양 끝이 들린 초승달 모양
    hull = []
    for i in range(41):
        t = i / 40
        x = 150 + t * 300
        y = wl - 4 - (abs(t - 0.5) * 2) ** 2.4 * 34
        hull.append((x, y))
    for i in range(41):
        t = 1 - i / 40
        x = 165 + t * 270
        y = wl + 8 - (abs(t - 0.5) * 2) ** 3 * 10
        hull.append((x, y))
    d.polygon(hull, fill=ink)
    # 뱃전 위 덮개(뜸)
    d.pieslice([328, wl - 62, 436, wl + 40], 180, 360, fill=ink)     # 둥근 거적 덮개
    d.rectangle([328, wl - 12, 436, wl - 4], fill=ink)
    # 앉은 사람 (삿갓)
    cx = 262
    d.ellipse([cx - 13, wl - 70, cx + 13, wl - 44], fill=ink)          # 머리
    d.polygon([(cx - 40, wl - 60), (cx, wl - 84), (cx + 40, wl - 60), (cx + 30, wl - 56), (cx - 30, wl - 56)], fill=ink)  # 삿갓
    d.polygon([(cx - 20, wl - 48), (cx + 18, wl - 48), (cx + 28, wl - 10), (cx - 26, wl - 10)], fill=ink)                 # 몸
    d.polygon([(cx + 6, wl - 40), (cx + 42, wl - 34), (cx + 40, wl - 26), (cx + 4, wl - 30)], fill=ink)                    # 팔
    # 낚싯대: 손에서 앞으로 길게, 끝이 휜다
    rod = []
    hx, hy = cx + 40, wl - 32
    for i in range(30):
        t = i / 29
        rod.append((hx - t * 230, hy - t * 120 + t * t * 70))
    for i in range(len(rod) - 1):
        d.line([rod[i], rod[i + 1]], fill=ink, width=max(1, int(4 * (1 - i / 30)) + 1))
    tx, ty = rod[-1]
    # 낚싯줄: 초릿대 끝에서 물로
    line_col = (200, 210, 220, 150)
    d.line([(tx, ty), (tx - 6, wl + 2)], fill=line_col, width=1)
    # 물에 닿은 자리 잔물결
    for k in range(3):
        rw = 10 + k * 12
        d.ellipse([tx - 6 - rw, wl - 1 - k, tx - 6 + rw, wl + 3 + k], outline=(180, 195, 210, 90 - k * 25), width=1)
    return img.filter(ImageFilter.GaussianBlur(0.5))


def boat_reflection(b):
    """배 그림자 (물에 비친 것, 흐리고 일렁이게)."""
    w, h = b.size
    wl = 330
    top = b.crop((0, 0, w, wl + 6)).transpose(Image.FLIP_TOP_BOTTOM)
    arr = np.asarray(top, np.float32) / 255
    rh = arr.shape[0]
    yy = np.linspace(0, 1, rh)[:, None]
    shift = (np.sin(np.arange(rh) * 0.9) * 3 * (0.3 + yy[:, 0])).astype(int)
    out = np.zeros_like(arr)
    for y in range(rh):
        out[y] = np.roll(arr[y], shift[y], axis=0)
    out[..., 3] *= (1 - yy) ** 1.5 * 0.55
    img = Image.fromarray((out * 255).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(1.5))
    canvas = Image.new("RGBA", (w, rh), (0, 0, 0, 0))
    canvas.paste(img, (0, 0))
    return canvas


def reeds():
    """앞쪽 갈대 실루엣 (오른쪽 아래). 아래 가운데가 뿌리."""
    w, h = 900, 520
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    r = np.random.default_rng(701)
    ink = (5, 9, 14, 255)
    for i in range(70):
        bx = r.uniform(80, w - 40)
        height = r.uniform(140, 470) * (0.6 + 0.4 * math.sin((bx / w) * math.pi))
        bend = r.uniform(-0.25, 0.05) * height
        pts = [(bx + bend * (t / 10) ** 2, h - t / 10 * height) for t in range(11)]
        for k in range(10):
            d.line([pts[k], pts[k + 1]], fill=ink, width=max(1, int(4 - k * 0.3)))
        if r.random() < 0.55:
            # 갈대 이삭
            ex, ey = pts[-1]
            d.ellipse([ex - 5, ey - 26, ex + 5, ey + 4], fill=ink)
        # 잎
        for _ in range(r.integers(1, 3)):
            k = r.integers(2, 7)
            sx, sy = pts[k]
            L = r.uniform(50, 130)
            ang = r.uniform(-2.4, -0.7) if r.random() < 0.5 else r.uniform(-2.4, -1.6)
            leaf = [(sx, sy)]
            for j in range(1, 9):
                t = j / 8
                leaf.append((sx + math.cos(ang) * L * t - L * 0.15 * t * t * 3, sy + math.sin(ang) * L * t + L * 0.35 * t * t))
            d.line(leaf, fill=ink, width=3)
    # 아래쪽 물가 덩어리
    d.ellipse([20, h - 40, w + 120, h + 60], fill=ink)
    return img.filter(ImageFilter.GaussianBlur(0.6))


# ---------------------------------------------------------------- 글자

def ink_texture(mask, seed, rough=0.22):
    """글자 마스크에 붓 자국 질감: 가장자리가 조금 갈라지고 속은 먹이 고르지 않다."""
    h, w = mask.shape
    n = fbm_2d(h, w, 40, 4, seed)
    fine = fbm_2d(h, w, 6, 2, seed + 3)
    a = mask * np.clip(0.80 + (n - 0.5) * 0.5, 0, 1)
    edge = smoothstep(0.0, 0.6, mask) * (1 - smoothstep(0.6, 1.0, mask))
    a = a * (1 - edge * smoothstep(0.55, 0.75, fine) * rough * 3)
    return np.clip(a, 0, 1)


def render_text(text, fnt, pad=40, spacing=0, stroke=0):
    """텍스트를 회색조 마스크(0~1)로. spacing: 글자 사이 추가 간격(px)."""
    if spacing == 0:
        box = fnt.getbbox(text, stroke_width=stroke)
        w, h = box[2] - box[0] + pad * 2, box[3] - box[1] + pad * 2
        img = Image.new("L", (w, h), 0)
        ImageDraw.Draw(img).text((pad - box[0], pad - box[1]), text, font=fnt, fill=255, stroke_width=stroke, stroke_fill=255)
        return np.asarray(img, np.float32) / 255
    widths = [fnt.getlength(c) for c in text]
    asc, desc = fnt.getmetrics()
    w = int(sum(widths) + spacing * (len(text) - 1) + pad * 2)
    h = asc + desc + pad * 2
    img = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(img)
    x = pad
    for c, cw in zip(text, widths):
        d.text((x, pad), c, font=fnt, fill=255)
        x += cw + spacing
    arr = np.asarray(img, np.float32) / 255
    ys, xs = np.nonzero(arr > 0.01)
    return arr[max(0, ys.min() - pad):ys.max() + pad, max(0, xs.min() - pad):xs.max() + pad]


def gradient_text(mask, top, bottom, glow=None):
    h, w = mask.shape
    t = np.linspace(0, 1, h)[:, None, None]
    rgb = top[None, None, :] * (1 - t) + bottom[None, None, :] * t
    return to_img(np.broadcast_to(rgb, (h, w, 3)), mask)


def seal(chars, size, seed, font_name="SongMyung-Regular.ttf", grid=None):
    """붉은 낙관(도장). 글자를 하얗게 파낸 음각. chars를 grid(행 목록)로 배치."""
    S = size
    r = np.random.default_rng(seed)
    pad = int(S * 0.07)
    # 거친 테두리의 사각 도장 판
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    n = fbm_2d(S, S, max(4, S // 24), 3, seed)
    inset = pad + (n - 0.5) * S * 0.035
    body = ((xx > inset) & (xx < S - inset) & (yy > inset) & (yy < S - inset)).astype(np.float32)
    # 글자
    rows = grid or [chars]
    txt = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(txt)
    inner = S - pad * 2 - S * 0.08
    cell = inner / max(len(rows), max(len(rw) for rw in rows))
    f = font(font_name, int(cell * 1.02))
    for ri, row in enumerate(rows):
        for ci, ch in enumerate(row):
            bb = f.getbbox(ch)
            cw, chh = bb[2] - bb[0], bb[3] - bb[1]
            cx = pad + S * 0.04 + cell * (ci + 0.5) + (inner - cell * len(row)) / 2
            cy = pad + S * 0.04 + cell * (ri + 0.5) + (inner - cell * len(rows)) / 2
            d.text((cx - cw / 2 - bb[0], cy - chh / 2 - bb[1]), ch, font=f, fill=255)
    # 획을 조금 굵게: PIL stroke_width는 송명 '쓰'처럼 윤곽이 겹친 글자에서 획 속이 지워져서(ㅡ가 사라짐) 팽창 필터를 쓴다
    grow = max(1, int(cell * 0.025))
    txt = txt.filter(ImageFilter.MaxFilter(grow * 2 + 1))
    t = np.asarray(txt, np.float32) / 255
    # 안쪽 테두리선 (흰 선)
    frame = ((np.abs(xx - S / 2) < S / 2 - pad - S * 0.025) & (np.abs(yy - S / 2) < S / 2 - pad - S * 0.025)).astype(np.float32)
    frame_line = body - frame * body
    a = np.clip(body - t - frame_line * 0.0, 0, 1)
    # 인주가 덜 묻은 얼룩
    blot = fbm_2d(S, S, max(3, S // 40), 3, seed + 9)
    a *= np.clip(0.55 + blot * 0.7, 0, 1) ** 0.6
    speck = r.random((S, S)) < 0.012
    a[speck] *= 0.25
    a = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(S / 900)), np.float32) / 255
    shade = 0.88 + (blot[..., None] - 0.5) * 0.3
    rgb = SEAL_RED[None, None, :] * shade
    return to_img(np.broadcast_to(rgb, (S, S, 3)), a)


def blur_glow(img, radius, strength=1.0, color=None):
    a = np.asarray(img.split()[-1], np.float32) / 255
    g = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius)), np.float32) / 255
    g = np.clip(g * strength, 0, 1)
    rgb = np.broadcast_to(color if color is not None else IVORY, g.shape + (3,))
    return to_img(rgb, g)


def pad_img(img, px):
    w, h = img.size
    out = Image.new("RGBA", (w + px * 2, h + px * 2), (0, 0, 0, 0))
    out.paste(img, (px, px))
    return out


# 타이틀 글자 배치 (원작 로고 느낌의 힘 있는 붓글씨, 2026-10-27 사용자): 글자마다 (크기 배율, 세로 어긋남, 기울기°, 앞 글자와 겹침)
TITLE_LAYOUT = [("청", 1.14, -0.07, -5.0, 0.0), ("풍", 0.9, 0.12, 4.0, 0.02), ("명", 1.0, -0.03, -3.0, 0.0), ("월", 1.1, 0.07, 5.0, 0.03)]


def calligraphy(text_layout, font_name, size, pad=60):
    """글자를 하나씩 그려 크기·높이·기울기를 달리해 이어 붙인다 (손으로 쓴 붓글씨의 리듬). 회색조 마스크(0~1)."""
    parts = []
    for ch, sc, dy, rot, overlap in text_layout:
        m = render_text(ch, font(font_name, int(size * sc)), pad=pad)
        im = Image.fromarray((m * 255).astype(np.uint8), "L").rotate(rot, resample=Image.BICUBIC, expand=True)
        a = np.asarray(im, np.float32) / 255
        ys, xs = np.nonzero(a > 0.02)
        a = a[:, xs.min():xs.max() + 1]   # 좌우 여백을 잘라 글자끼리 붙게 (위아래는 그대로 두어 기준선이 맞는다)
        parts.append((a, dy, overlap))
    hmax = max(p.shape[0] for p, _, _ in parts)
    H2 = int(hmax + size * 0.5)
    W2 = int(sum(p.shape[1] for p, _, _ in parts) + pad * 2)
    out = np.zeros((H2, W2), np.float32)
    x = pad
    for m, dy, overlap in parts:
        x -= int(overlap * size)
        y = int((H2 - m.shape[0]) / 2 + dy * size)
        out[y:y + m.shape[0], x:x + m.shape[1]] = np.maximum(out[y:y + m.shape[0], x:x + m.shape[1]], m)
        x += m.shape[1]
    ys, xs = np.nonzero(out > 0.01)
    return out[max(0, ys.min() - pad):ys.max() + pad, max(0, xs.min() - pad):xs.max() + pad]


def title_assets():
    # 제주한라산체(OFL): 굵고 힘 있는 붓글씨. 예전 East Sea Dokdo(가는 마커 느낌)에서 바꿈
    m = calligraphy(TITLE_LAYOUT, "JejuHallasan-Regular.ttf", 380)
    m = ink_texture(m, 801, rough=0.32)
    # 위는 상아색, 아래로 달빛 은빛
    title = gradient_text(m, hexc("#fffaf0"), hexc("#d7dfe6"))
    title = pad_img(title, 60)
    glow = blur_glow(title, 28, 1.6, hexc("#bcd2e2"))
    sealimg = seal("짭", 300, 811, "SongMyung-Regular.ttf", grid=["짭"])
    en = render_text("CHEONGPUNG  MYEONGWOL", font("Cinzel.ttf", 64, "Bold"), pad=20, spacing=14)
    en_img = gradient_text(en, hexc("#e9d9a8"), hexc("#b99a5a"))
    tag = render_text("바람 맑고 달 밝은 밤, 낚싯대 하나", font("SongMyung-Regular.ttf", 64), pad=20, spacing=4)
    tag_img = gradient_text(tag, IVORY, IVORY * 0.85)
    copy = render_text("© 2026 JETHRO'S GAME COMPANY", font("Cinzel.ttf", 34, "Regular"), pad=12, spacing=5)
    copy_img = gradient_text(copy, hexc("#9aa7b3"), hexc("#7b8894"))
    return {"title": title, "title_glow": glow, "title_seal": sealimg, "title_en": en_img, "title_tag": tag_img, "copyright": copy_img}


def result_logo():
    """RESULT 로고 (원작 결과 화면 왼쪽 위): 비스듬한 굵은 대문자, 닳은 가장자리, 위는 흰빛 아래는 은회색,
    검은 테두리, 글자 뒤를 가로지르는 먹 붓질. 결과 화면(ResultPanel)이 Resources/Intro/result_logo로 불러 쓴다."""
    f = font("Cinzel.ttf", 230, "Black")
    m = render_text("RESULT", f, pad=80, spacing=-8)
    h, w = m.shape
    # 오른쪽으로 기울이기 (붓으로 빠르게 쓴 느낌)
    im = Image.fromarray((m * 255).astype(np.uint8), "L")
    sh = 0.2
    im = im.transform((int(w + h * sh), h), Image.AFFINE, (1, sh, -h * sh, 0, 1, 0), resample=Image.BICUBIC)
    m = np.asarray(im, np.float32) / 255
    m = ink_texture(m, 1301, rough=0.45)
    h, w = m.shape
    # 먹 붓질: 글자 아래쪽 2/3를 가로지르는 굵은 획, 오른쪽 끝은 갈라지며 가늘어진다 (비백)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    u = xx / w
    cy = h * (0.62 + 0.06 * (u - 0.5))
    half = h * 0.2 * (1 - smoothstep(0.7, 1.02, u)) * smoothstep(-0.02, 0.06, u)
    streak = fbm_2d(h, w, 3, 2, 1311)
    streak = np.repeat(streak.mean(axis=1, keepdims=True), w, axis=1)   # 가로로 길게 늘인 붓 털 자국
    brush = (np.abs(yy - cy) < half).astype(np.float32)
    brush *= np.clip(1.4 - smoothstep(0.45, 1.0, u) * (streak > 0.5) * 1.6, 0, 1)
    brush = np.asarray(Image.fromarray((brush * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2)), np.float32) / 255
    # 검은 테두리 (글자를 7px 부풀림)
    outline = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(15)), np.float32) / 255
    dark = np.maximum(brush * 0.88, outline)
    # 글자 색: 위 흰빛 → 아래 은회색, 가운데 살짝 밝은 금속 띠
    t = (yy / h)
    body = np.dstack([np.clip(1.0 - 0.24 * t + 0.1 * np.exp(-((t - 0.42) / 0.05) ** 2), 0, 1)] * 3)
    ink = smoothstep(0.05, 0.5, m)[..., None]   # 글자 속은 흰빛 그대로, 닳은 가장자리만 검은 바탕이 비친다
    rgb = body * ink
    alpha = np.maximum(dark, ink[..., 0])
    return {"result_logo": to_img(rgb, alpha)}


def underwater(seed, top, bottom, floor):
    """도감 상세의 물속 배경 (원작 도감: 물고기 사진 뒤 물속 풍경). 위에서 내리는 빛 줄기, 바닥 자갈·바위, 물풀 그림자, 떠다니는 알갱이."""
    w, h = 1024, 640
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    t = yy / h
    rgb = top[None, None, :] * (1 - t[..., None]) + bottom[None, None, :] * t[..., None]
    # 빛 줄기: 위에서 비스듬히 내려오는 밝은 띠 여러 개 (아래로 갈수록 흐려진다)
    rays = np.zeros((h, w), np.float32)
    r = np.random.default_rng(seed)
    for _ in range(9):
        x0, ang, wd = r.uniform(0, w), r.uniform(-0.35, 0.05), r.uniform(18, 60)
        d = np.abs(xx - (x0 + yy * ang))
        rays += np.exp(-(d / wd) ** 2) * r.uniform(0.25, 0.6)
    rays *= (1 - t) ** 1.6
    rgb += rays[..., None] * np.array([0.55, 0.75, 0.7])[None, None, :] * 0.35
    # 수면 일렁임 (맨 위 얇은 밝은 그물무늬)
    caus = fbm_2d(h, w, 22, 3, seed + 1)
    rgb += (smoothstep(0.55, 0.85, caus) * (1 - smoothstep(0.0, 0.25, t)))[..., None] * 0.08
    # 바닥: 굴곡진 모래·자갈 띠 + 바위 덩어리
    fl = fbm_2d(1, w, 90, 3, seed + 2)[0]
    ground = h * (0.87 + (fl - 0.5) * 0.1)
    rocks = fbm_2d(h, w, 60, 4, seed + 3)
    under = smoothstep(-6, 6, yy - ground[None, :])
    peb = fbm_2d(h, w, 5, 2, seed + 4)
    floor_col = floor[None, None, :] * (0.65 + peb[..., None] * 0.7)
    rgb = rgb * (1 - under[..., None]) + floor_col * under[..., None]
    rock = smoothstep(0.6, 0.72, rocks) * under
    rgb = rgb * (1 - rock[..., None] * 0.45) + (floor * 0.5)[None, None, :] * rock[..., None] * 0.45
    # 물풀: 바닥에서 위로 흔들리며 자라는 어두운 줄기들
    weed = np.zeros((h, w), np.float32)
    for _ in range(26):
        x0, ht, wd = r.uniform(0, w), r.uniform(0.15, 0.5) * h, r.uniform(3, 8)
        base = ground[int(np.clip(x0, 0, w - 1))]
        k = np.clip((base - yy) / ht, 0, 1)
        cx = x0 + np.sin(k * 3.5 + x0) * 22 * k
        weed += np.exp(-((xx - cx) / (wd * (1.2 - k))) ** 2) * (yy < base) * (yy > base - ht)
    rgb = rgb * (1 - np.clip(weed, 0, 1)[..., None] * 0.55) + (bottom * 0.5)[None, None, :] * np.clip(weed, 0, 1)[..., None] * 0.55
    # 떠다니는 알갱이
    dots = (r.random((h, w)) > 0.9993).astype(np.float32)
    dots = np.asarray(Image.fromarray((dots * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)), np.float32) / 255 * 3
    rgb += np.clip(dots, 0, 1)[..., None] * 0.35
    # 가장자리 어둡게
    vig = 1 - 0.45 * (((xx / w - 0.5) * 1.6) ** 2 + ((t - 0.45) * 1.4) ** 2)
    rgb *= np.clip(vig, 0.35, 1)[..., None]
    # 사진 배경처럼 초점이 물고기에 있어 뒤는 살짝 흐리다
    return to_img(rgb + dither(h, w)).filter(ImageFilter.GaussianBlur(2.2))


def dex_assets():
    return {
        "dex_fresh": underwater(41, hexc("#5f8a5a"), hexc("#1f3a26"), hexc("#7a6a4c")),   # 민물: 초록빛 탁한 물
        "dex_sea": underwater(43, hexc("#3f8fae"), hexc("#0f2f4a"), hexc("#a89878")),     # 바다: 푸른 물, 밝은 모래
    }


def company_assets():
    seal_img = seal("제쓰로네", 640, 901, "SongMyung-Regular.ttf", grid=["제쓰", "로네"])
    ko = render_text("제쓰로네 게임공방", font("SongMyung-Regular.ttf", 150), pad=30, spacing=10)
    ko_img = gradient_text(ko, hexc("#fbf5e6"), hexc("#e2d6b8"))
    en = render_text("JETHRO'S GAME COMPANY", font("Cinzel.ttf", 70, "SemiBold"), pad=20, spacing=22)
    en_img = gradient_text(en, hexc("#ecd9a0"), hexc("#b38e4c"))
    return {"company_seal": seal_img, "company_ko": ko_img, "company_en": en_img,
            "company_seal_glow": blur_glow(pad_img(seal_img, 60), 30, 1.2, hexc("#ff5a3c"))}


def paper():
    """사업자 화면 바탕: 은은한 한지 결 + 가운데가 밝은 비네트 (검정에 가까운 먹색)."""
    w, h = 1024, 576
    n = fbm_2d(h, w, 120, 5, 951)
    fiber = fbm_2d(h, w // 8, 3, 2, 952)
    fiber = np.asarray(Image.fromarray((fiber * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), np.float32) / 255
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    vig = np.clip(1 - d * 0.75, 0, 1) ** 1.6
    base = hexc("#0b0a09") + hexc("#2a2420") * vig[..., None] * 0.9
    col = base * (0.9 + 0.2 * n[..., None]) * (0.96 + 0.08 * fiber[..., None])
    col += dither(h, w, 1.5 / 255)
    return to_img(col)


def petal():
    """바람에 날리는 잎/꽃잎 (흰색, 유니티에서 색을 입힌다)."""
    S = 64
    img = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(img)
    d.ellipse([12, 22, 52, 42], fill=255)
    d.polygon([(12, 32), (4, 30), (12, 28)], fill=255)
    img = img.filter(ImageFilter.GaussianBlur(1.2))
    a = np.asarray(img, np.float32) / 255
    return to_img(np.ones((S, S, 3), np.float32), a)


def main():
    preview = None
    if "--preview" in sys.argv:
        preview = sys.argv[sys.argv.index("--preview") + 1]
    os.makedirs(OUT, exist_ok=True)
    out = {}
    s = sky(); out["bg_sky"] = s
    out["bg_moon"] = moon()
    out["bg_wisps"] = wisps()
    far = mountains_far(); out["bg_far"] = far
    near = mountains_near(); out["bg_near"] = near
    out["bg_water"] = water(s, far, near)
    out["bg_moonpath"] = moon_path()
    out["bg_mist"] = mist()
    b = boat(); out["bg_boat"] = b
    out["bg_boat_refl"] = boat_reflection(b)
    out["bg_reeds"] = reeds()
    out["petal"] = petal()
    out["company_paper"] = paper()
    out.update(title_assets())
    out.update(company_assets())
    out.update(result_logo())
    out.update(dex_assets())
    for k, img in out.items():
        img.save(os.path.join(OUT, k + ".png"), optimize=True)
        print(f"{k}.png {img.size[0]}x{img.size[1]}")
    if preview:
        os.makedirs(preview, exist_ok=True)
        compose_preview(out, preview)


def compose_preview(o, folder):
    """IntroSequence의 배치를 흉내 낸 미리보기 (1920×1080)."""
    cw, ch = 1920, 1080
    sc = cw / W
    def fit(img):
        return img.resize((int(img.size[0] * sc), int(img.size[1] * sc)), Image.LANCZOS)
    canvas = fit(o["bg_sky"]).convert("RGBA").crop((0, 0, cw, ch))
    m = fit(o["bg_moon"]); canvas.alpha_composite(m, (int(MOON[0] * cw - m.size[0] / 2), int(MOON[1] * ch * (H * sc / ch) - m.size[1] / 2)))
    wi = fit(o["bg_wisps"]); canvas.alpha_composite(wi.crop((0, 0, cw, wi.size[1])), (0, int(MOON[1] * H * sc - wi.size[1] * 0.45)))
    for k in ("bg_far", "bg_water", "bg_near"):
        canvas.alpha_composite(fit(o[k]).crop((0, 0, cw, ch)))
    mp = o["bg_moonpath"].resize((int(420 * sc * 1.6), int((H - HORIZON * H) * sc)))
    add = np.asarray(canvas, np.float32)
    mpa = np.asarray(mp, np.float32)
    x0 = int(MOON[0] * W * sc - mp.size[0] / 2); y0 = int(HORIZON * H * sc)
    hh = min(mp.size[1], ch - y0)
    add[y0:y0 + hh, x0:x0 + mp.size[0], :3] += mpa[:hh, :, :3] * (mpa[:hh, :, 3:4] / 255) * 0.8
    canvas = Image.fromarray(np.clip(add, 0, 255).astype(np.uint8), "RGBA")
    mi = fit(o["bg_mist"]); canvas.alpha_composite(mi.crop((0, 0, cw, mi.size[1])), (0, int(HORIZON * H * sc - mi.size[1] * 0.55)))
    bt = o["bg_boat"]; br = o["bg_boat_refl"]
    bx, by = int(MOON[0] * cw - bt.size[0] * 0.62), int(ch * 0.80 - 330)
    canvas.alpha_composite(br, (bx, by + 336)); canvas.alpha_composite(bt, (bx, by))
    rd = o["bg_reeds"].resize((630, 364)); canvas.alpha_composite(rd, (cw - rd.size[0] + 90, ch - rd.size[1] + 20))
    t = o["title"]; tw = 900; t2 = t.resize((tw, int(t.size[1] * tw / t.size[0])), Image.LANCZOS)
    g = o["title_glow"].resize(t2.size, Image.LANCZOS)
    tx, ty = int(cw * 0.30 - tw / 2), int(ch * 0.36 - t2.size[1] / 2)
    canvas.alpha_composite(g, (tx, ty)); canvas.alpha_composite(t2, (tx, ty))
    se = o["title_seal"].resize((110, 110)); canvas.alpha_composite(se, (tx + tw - 70, ty + t2.size[1] - 150))
    en = o["title_en"]; en2 = en.resize((int(en.size[0] * 0.62), int(en.size[1] * 0.62)))
    canvas.alpha_composite(en2, (int(cw * 0.30 - en2.size[0] / 2), ty + t2.size[1] - 40))
    tg = o["title_tag"]; tg2 = tg.resize((int(tg.size[0] * 0.5), int(tg.size[1] * 0.5)))
    canvas.alpha_composite(tg2, (int(cw * 0.30 - tg2.size[0] / 2), ty + t2.size[1] + 20))
    canvas.convert("RGB").save(os.path.join(folder, "intro_title_preview.png"))
    # 사업자 화면
    c2 = o["company_paper"].resize((cw, ch)).convert("RGBA")
    sl = o["company_seal"].resize((230, 230)); c2.alpha_composite(sl, (cw // 2 - 115, 300))
    ko = o["company_ko"]; ko2 = ko.resize((int(ko.size[0] * 0.55), int(ko.size[1] * 0.55)))
    c2.alpha_composite(ko2, (cw // 2 - ko2.size[0] // 2, 570))
    en = o["company_en"]; en3 = en.resize((int(en.size[0] * 0.42), int(en.size[1] * 0.42)))
    c2.alpha_composite(en3, (cw // 2 - en3.size[0] // 2, 690))
    c2.convert("RGB").save(os.path.join(folder, "intro_company_preview.png"))


if __name__ == "__main__":
    main()
