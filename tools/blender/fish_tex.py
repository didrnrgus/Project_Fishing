"""
물고기 텍스처를 코드로 그린다 (numpy + Pillow). docs/09-art-plan.md 3-5.

아틀라스 1024×512 하나에 몸·지느러미·눈·수염을 모두 담는다 (머티리얼 2개가 같은 텍스처를 쓴다).
  몸   : u 0~0.75  (주둥이 → 꼬리자루),  v 0 = 배 가운데, 1 = 등 가운데 (좌우 대칭으로 같은 텍셀)
         납작한 고기는 대칭이 아니라 v 0~0.5 = 오른쪽, 0.5~1 = 왼쪽 (눈 있는 쪽만 무늬, 반대쪽은 흰 배)
  지느러미: u 0.75~0.875 = 위 지느러미(등·꼬리), 0.875~1 = 아래 지느러미(가슴·배·뒷), v 0.5(밑동) ~ 1(끝)
  눈   : u 0.75~0.875, v 0.25~0.5
  조각 : u 0.875~0.9375, v 0.25~0.375 = 수염,  u 0.9375~1, v 0.25~0.375 = 부리(학꽁치) 끝
결과: 알베도(RGBA, A = 지느러미 투명 가장자리), 노멀(비늘 굴곡), 금속성(R)·매끈함(A).
"""
import math
import zlib

import numpy as np
from PIL import Image

W, H = 1024, 512
BW = 768  # 몸 영역 폭


def hexrgb(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0 + 1e-9), 0, 1)
    return t * t * (3 - 2 * t)


def noise(w, h, cells, seed, octaves=3):
    """값 노이즈 (0~1): 작은 무작위 격자를 키워서 겹친다."""
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        c = max(2, int(cells * (2 ** o)))
        g = rng.random((max(2, int(c * h / w)) + 1, c + 1)).astype(np.float32)
        img = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += np.asarray(img, np.float32) / 255.0 * amp
        tot += amp
        amp *= 0.5
    return out / tot


def blend(img, color, alpha):
    """img (h,w,3) 위에 color를 alpha(h,w)만큼 섞는다."""
    a = np.clip(alpha, 0, 1)[..., None]
    img[:] = img * (1 - a) + color[None, None, :] * a


def paint(d, seed=0):
    rng = np.random.default_rng(zlib.crc32(d["id"].encode()) + seed)
    col = d['colors']
    flat = d.get('flat')
    alb = np.zeros((H, W, 4), np.float32)
    alb[..., 3] = 1.0
    height = np.zeros((H, W), np.float32)
    metal = np.zeros((H, W), np.float32)
    smooth_map = np.full((H, W), 0.45, np.float32)

    # ---------------- 몸 ----------------
    yy, xx = np.mgrid[0:H, 0:BW].astype(np.float32)
    u = (xx + 0.5) / BW            # 0 주둥이 ~ 1 꼬리자루
    v = 1.0 - (yy + 0.5) / H       # 0 배 ~ 1 등
    if flat:
        # v 0~0.5 오른쪽(배 → 등), 0.5~1 왼쪽(등 → 배). 눈 있는 쪽 = side
        eyed = (v >= 0.5) if flat == 'left' else (v < 0.5)
        vv = np.where(v < 0.5, v * 2, (1 - v) * 2)  # 0 가장자리(배쪽) ~ 1 가장자리(등쪽)… 납작이는 둘레가 테두리
        vside = vv
    else:
        eyed = np.ones_like(v, bool)
        vside = v
    back, side, belly = hexrgb(col['back']), hexrgb(col['side']), hexrgb(col['belly'])
    body = np.zeros((H, BW, 3), np.float32)
    if flat:
        # 눈 있는 쪽: 고르게 진한 색, 가장자리로 갈수록 살짝 밝게. 반대쪽: 흰 배
        t = smooth(0.0, 0.25, np.minimum(vside, 1 - vside) * 2)
        top = back[None, None] * t[..., None] + side[None, None] * (1 - t[..., None])
        body[:] = np.where(eyed[..., None], top, belly[None, None])
    else:
        t1 = smooth(0.18, 0.55, v)[..., None]
        t2 = smooth(0.55, 0.92, v)[..., None]
        body[:] = belly * (1 - t1) + side * t1
        body[:] = body * (1 - t2) + back * t2
    # 머리 윗부분은 조금 진하게, 꼬리 쪽은 살짝 어둡게
    blend(body, back * 0.85, 0.25 * smooth(0.25, 0.0, u) * smooth(0.5, 1.0, vside) * eyed)
    # 잔 얼룩 (단색이 플라스틱처럼 보이지 않게)
    n = noise(BW, H, 10, rng.integers(1 << 30))
    body *= (0.9 + 0.2 * n)[..., None]

    metal_b = np.full((H, BW), col.get('metallic', 0.0) * 0.5, np.float32)
    silver = col.get('silver', 0.2)
    smooth_b = np.full((H, BW), 0.42 + 0.35 * silver, np.float32)
    h_b = np.zeros((H, BW), np.float32)

    # ---- 비늘 ----
    nsc = d.get('scales', 0)
    if nsc:
        dep, wid, be = d['depth'], d['width'], d['body_end']
        half_circ = math.pi * (dep / 2 + wid / 2) / 2
        su = BW / nsc
        sv = (be / nsc) * H / half_circ * 0.8
        sv = max(sv, 5.0)
        # 겹친 비늘: 앞(머리 쪽) 비늘이 뒤 비늘을 덮는다. 픽셀마다 덮고 있는 가장 앞쪽 원을 고르고,
        # 그 원의 뒤 가장자리(꼬리 쪽 호)를 진하게 그린다
        X = xx / su
        Y = yy / sv
        R = 0.78
        best_t = np.full(X.shape, 2.0, np.float32)
        best_dx = np.zeros_like(X)
        found = np.zeros(X.shape, bool)
        j0 = np.floor(X)
        for dj in (0, 1):
            jc = j0 + dj
            off = (jc % 2) * 0.5
            yc = np.floor(Y - off) + 0.5 + off
            for dyc in (0.0, 1.0, -1.0):
                yy_c = yc + dyc
                dist = np.sqrt((X - jc) ** 2 + (Y - yy_c) ** 2) / R
                take = (~found) & (dist <= 1.0)
                best_t = np.where(take, dist, best_t)
                best_dx = np.where(take, (X - jc) / R, best_dx)
                found |= take
        best_t = np.where(found, best_t, 1.0)
        rim = smooth(0.78, 0.97, best_t) * smooth(-0.1, 0.35, best_dx)
        dome = (1 - best_t ** 2) * (0.6 + 0.4 * best_dx)
        strength = 0.35
        for p in d['colors'].get('patterns', []):
            if p[0] == 'scale_edge':
                strength = p[1]
        fade = smooth(0.02, 0.14, u) * smooth(1.02, 0.9, u)  # 머리(아가미 뚜껑 앞)에는 비늘 없음
        body *= (1 - strength * 0.45 * rim * fade)[..., None]
        body *= (1 + 0.12 * dome * fade * silver)[..., None]
        h_b += (dome * 0.8 - rim * 0.6) * fade
        smooth_b -= 0.1 * rim * fade
        if any(p[0] == 'scale_dots' for p in d['colors'].get('patterns', [])):
            dots = smooth(0.45, 0.2, best_t)
            blend(body, hexrgb(next(p for p in d['colors']['patterns'] if p[0] == 'scale_dots')[1]['color']), dots * 0.5 * fade)
    else:
        # 비늘이 없거나 아주 잔 고기: 피부 결
        h_b += (noise(BW, H, 60, rng.integers(1 << 30), 2) - 0.5) * 0.3

    # ---- 옆줄 ----
    ll_v = 0.6 + 0.08 * np.sin(np.clip(u, 0, 1) * math.pi) - 0.08 * u
    if not flat and d['type'] not in ('G',) and not d.get('ceph'):
        line = np.exp(-((v - ll_v) ** 2) / (2 * 0.006 ** 2)) * smooth(0.18, 0.24, u)
        blend(body, side * 0.6 + back * 0.4, line * 0.35)
        h_b += line * 0.5

    # ---- 아가미 뚜껑 ----
    head_end = 0.2 if d['type'] not in ('G', 'H') else 0.13
    if d['type'] == 'G':
        head_end = 0.09
    gx = head_end - 0.05 * (vside - 0.5) ** 2 * 4
    gill = np.exp(-((u - gx) ** 2) / (2 * 0.006 ** 2)) * smooth(0.05, 0.25, vside) * smooth(0.98, 0.8, vside) * eyed * (0.0 if d.get('ceph') else 1.0)
    blend(body, back * 0.55, gill * 0.6)
    h_b += gill * 0.8
    # 입 선
    if not flat and not d.get('ceph'):
        mouth_v = 0.5 + 0.35 * d.get('mouth', 0) * 0.3
        mlen = 0.035 + 0.03 * max(0, d.get('mouth', 0))
        if d['type'] == 'C':
            mlen += 0.03
        mouth = np.exp(-((v - mouth_v - (u * 0.6)) ** 2) / (2 * 0.008 ** 2)) * smooth(mlen, mlen * 0.6, u)
        blend(body, back * 0.4, mouth * 0.7)

    # ---------------- 무늬 ----------------
    def draw_spots(n, r, color, vr=(0, 1), ur=(0.05, 0.98), a=1.0, row=False, irregular=0.3):
        c = hexrgb(color)
        for k in range(n):
            if row:
                cu = ur[0] + (ur[1] - ur[0]) * (k + 0.5) / n
                cv = (vr[0] + vr[1]) / 2
            else:
                cu = rng.uniform(*ur)
                cv = rng.uniform(*vr)
            rr = rng.uniform(*r)
            px, py = cu * BW, (1 - cv) * H
            if flat:
                # 눈 있는 쪽 절반에만
                py = (1 - (0.5 + cv * 0.5 if flat == 'left' else cv * 0.5)) * H
            x0, x1 = int(max(0, px - rr * 2)), int(min(BW, px + rr * 2 + 1))
            y0, y1 = int(max(0, py - rr * 2)), int(min(H, py + rr * 2 + 1))
            if x1 <= x0 or y1 <= y0:
                continue
            sy, sx = np.mgrid[y0:y1, x0:x1]
            dd = np.sqrt((sx - px) ** 2 + ((sy - py) * 0.9) ** 2) / rr
            ang = np.arctan2(sy - py, sx - px)
            dd *= 1 + irregular * 0.3 * np.sin(ang * 3 + k)
            m = smooth(1.0, 0.7, dd) * a
            sub = body[y0:y1, x0:x1]
            blend(sub, c, m)

    for p in col.get('patterns', []):
        kind, arg = p[0], p[1]
        if kind == 'spots':
            draw_spots(arg['n'], arg['r'], arg['color'], arg.get('v', (0, 1)), arg.get('u', (0.05, 0.98)), arg.get('a', 1.0), arg.get('row', False))
        elif kind == 'rosettes':
            draw_spots(arg['n'], arg['r'], arg['color'], arg.get('v', (0, 1)), (0.04, 0.97), 0.85, irregular=1.0)
            draw_spots(arg['n'] // 2, (3, 6), arg['color'], (0.4, 1.0), (0.0, 0.2), 0.8)
        elif kind == 'bands':
            nb, (u0, u1), bw = arg['n'], arg['u'], arg['w']
            c = hexrgb(arg['color'])
            vr = arg.get('v', (0, 1))
            for k in range(nb):
                cu = u0 + (u1 - u0) * k / max(1, nb - 1)
                w = bw * (0.55 if arg.get('taper') and k >= 4 else 1.0)
                wob = 0.01 * np.sin(v * 9 + k)
                m = smooth(w * 0.5, w * 0.35, np.abs(u - cu - wob)) * smooth(vr[0], vr[0] + 0.12, vside) * smooth(vr[1] + 0.05, vr[1] - 0.05, vside)
                blend(body, c, m * arg.get('a', 0.6) * eyed)
        elif kind == 'obliquebands':
            c = hexrgb(arg['color'])
            for k in range(arg['n']):
                cu = 0.15 + 0.75 * k / (arg['n'] - 1)
                m = smooth(0.035, 0.02, np.abs(u - cu - (v - 0.5) * 0.12 + 0.02 * np.sin(v * 13 + k)))
                blend(body, c, m * arg['a'] * smooth(0.15, 0.35, v))
        elif kind == 'stripe':
            m = smooth(arg['w'], arg['w'] * 0.4, np.abs(v - arg['v'])) * smooth(0.1, 0.2, u)
            blend(body, hexrgb(arg['color']), m * arg.get('a', 0.6))
        elif kind == 'hstripes':
            c = hexrgb(arg['color'])
            for k in range(arg['n']):
                cv = arg['v'][0] + (arg['v'][1] - arg['v'][0]) * k / max(1, arg['n'] - 1)
                m = smooth(arg['w'], arg['w'] * 0.3, np.abs(v - cv - 0.03 * u)) * smooth(0.18, 0.26, u)
                blend(body, c, m * arg.get('a', 0.5))
        elif kind == 'mottle':
            sc = arg.get('scale', 0.08)
            nn = noise(BW, H, int(1.0 / sc), rng.integers(1 << 30), 3)
            m = smooth(0.52, 0.62, nn)
            blend(body, hexrgb(arg['color']), m * arg.get('a', 0.5) * eyed * smooth(0.0, 0.25, vside if not flat else np.ones_like(v)))
        elif kind == 'blotches':
            nn = noise(BW, H, 9, rng.integers(1 << 30), 2)
            saddle = 0.5 + 0.5 * np.cos((u * 4.2 + 0.3) * 2 * math.pi)
            m = smooth(0.45, 0.55, nn * 0.6 + saddle * 0.5 * smooth(0.3, 0.7, v))
            blend(body, hexrgb(arg['color']), m * arg['a'] * smooth(0.12, 0.2, u))
        elif kind == 'blotchrows':
            c = hexrgb(arg['color'])
            for row_v, cnt in ((0.72, 9), (0.42, 10)):
                for k in range(cnt):
                    cu = 0.2 + 0.75 * (k + 0.5 * (row_v < 0.5)) / cnt
                    draw_spots(1, (14, 20), arg['color'], (row_v, row_v), (cu, cu), arg['a'], irregular=1.2)
        elif kind == 'blotchline':
            for k in range(10):
                cu = 0.2 + 0.75 * k / 10
                draw_spots(1, (10, 16), arg['color'], (arg['v'], arg['v']), (cu, cu), 0.8, irregular=1.0)
        elif kind == 'eyeline':
            e = d['eye']
            ev = 0.5 + 0.45 * e['z']
            c = hexrgb(arg['color'])
            for k in range(arg.get('n', 1)):
                ang = -0.35 + k * 0.45
                dist = (v - ev) - (u - e['s']) * ang
                m = smooth(arg['w'], arg['w'] * 0.4, np.abs(dist)) * smooth(e['s'] - 0.06, e['s'] - 0.03, u) * smooth(e['s'] + 0.22, e['s'] + 0.12, u)
                blend(body, c, m * 0.85)
        elif kind == 'earflap':
            m = smooth(0.035, 0.02, np.sqrt(((u - head_end + 0.01) * 1.0) ** 2 + ((v - 0.68) * 0.6) ** 2))
            blend(body, hexrgb(arg), m)
        elif kind == 'throat':
            m = smooth(0.4, 0.2, v) * smooth(0.45, 0.15, u)
            blend(body, hexrgb(arg), m * 0.6)
        elif kind == 'waves':
            c = hexrgb(arg['color'])
            wv = np.sin(u * 90 + np.sin(v * 25) * 2.5 + v * 30) * 0.5 + 0.5
            m = smooth(0.75, 0.9, wv) * smooth(arg['v'][0], arg['v'][0] + 0.08, v) * smooth(0.12, 0.2, u)
            blend(body, c, m * arg['a'])
        elif kind == 'scutes':
            line = np.exp(-((v - ll_v + 0.06 * smooth(0.3, 0.55, u)) ** 2) / (2 * 0.02 ** 2)) * smooth(0.2, 0.25, u)
            ticks = 0.5 + 0.5 * np.cos(u * 220)
            blend(body, hexrgb(arg['color']), line * (0.5 + 0.5 * ticks) * arg['a'])
            h_b += line * ticks * 1.2
        elif kind == 'pores':
            for k in range(arg['n']):
                cu = 0.1 + 0.88 * k / arg['n']
                draw_spots(1, (2.2, 3.0), arg['color'], (arg['v'], arg['v']), (cu, cu), 0.9, irregular=0)
        elif kind == 'sheen':
            m = np.exp(-((v - 0.62) ** 2) / (2 * 0.08 ** 2))
            body[:] = body + m[..., None] * 0.15 * arg
        elif kind == 'mouthline':
            pass
        elif kind == 'rings':
            # 주꾸미 눈 아래 금색 고리 (좌우 대칭 UV라 하나만 그리면 양쪽에 나온다). u = 몸 영역 비율
            px, py, rr = arg['u'] * BW, (1 - arg['v']) * H, arg['r']
            dd = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            m = smooth(rr * 0.35, rr * 0.2, np.abs(dd - rr * 0.75))
            blend(body, hexrgb(arg['color']), m)
    body = np.clip(body, 0, 1)
    metal_b += silver * 0.45 * smooth(0.9, 0.3, vside) * eyed
    alb[:, :BW, :3] = body
    height[:, :BW] = h_b
    metal[:, :BW] = metal_b
    smooth_map[:, :BW] = np.clip(smooth_b, 0.05, 0.95)

    # ---------------- 지느러미 ----------------
    for gi, key in enumerate(('fin_up', 'fin_low')):
        x0 = BW + gi * 128
        base, tip, ta = col[key]
        base, tip = hexrgb(base), hexrgb(tip)
        fy, fx = np.mgrid[0:256, 0:128].astype(np.float32)
        fv = 1.0 - (fy + 0.5) / 256     # 0 밑동 ~ 1 끝
        fu = (fx + 0.5) / 128
        c = base[None, None] * (1 - fv[..., None]) + tip[None, None] * fv[..., None]
        rays = 0.5 + 0.5 * np.cos(fu * 2 * math.pi * 14)
        c *= (0.82 + 0.18 * rays)[..., None]
        if any(p[0] == 'finspots' for p in col.get('patterns', [])):
            fs = hexrgb(next(p for p in col['patterns'] if p[0] == 'finspots')[1])
            nn = noise(128, 256, 8, rng.integers(1 << 30), 2)
            blend(c, fs, smooth(0.55, 0.65, nn) * 0.8)
        a = 1.0 - (1.0 - ta) * smooth(0.25, 1.0, fv) * (0.65 + 0.35 * (1 - rays))
        # 끝이 해진 듯한 가장자리 (알파 테스트로 잘린다)
        fray = noise(128, 256, 20, rng.integers(1 << 30), 2)
        a = np.where(fv > 0.93 + 0.06 * fray, 0.0, a)
        alb[0:256, x0:x0 + 128, :3] = np.clip(c, 0, 1)
        alb[0:256, x0:x0 + 128, 3] = np.clip(a, 0, 1)
        height[0:256, x0:x0 + 128] = rays * 0.6 * (1 - fv * 0.5)
        smooth_map[0:256, x0:x0 + 128] = 0.55
        metal[0:256, x0:x0 + 128] = 0.0

    # ---------------- 눈 ----------------
    iris = hexrgb(d['eye'].get('iris', '#c0a040'))
    ey, ex = np.mgrid[0:128, 0:128].astype(np.float32)
    r = np.sqrt((ex - 63.5) ** 2 + (ey - 63.5) ** 2) / 64.0
    eye = np.zeros((128, 128, 3), np.float32)
    eye[:] = iris * (0.7 + 0.3 * r[..., None] / 0.9)
    eye[r > 0.88] = hexrgb(col['back']) * 0.5
    eye[r < 0.48] = np.array([0.02, 0.02, 0.025])
    hl = np.exp(-(((ex - 50) ** 2 + (ey - 48) ** 2) / 40.0))
    eye += hl[..., None] * 0.9
    alb[256:384, BW:BW + 128, :3] = np.clip(eye, 0, 1)
    smooth_map[256:384, BW:BW + 128] = 0.95
    metal[256:384, BW:BW + 128] = 0.0

    # ---------------- 조각: 수염, 부리 끝 ----------------
    alb[320:384, 896:960, :3] = side * 0.6 + belly * 0.4
    alb[320:384, 960:1024, :3] = np.array([0.8, 0.2, 0.15]) if d['id'] == 'halfbeak' else back * 0.6
    alb[384:512, BW:, :3] = side
    alb[256:320, 896:1024, :3] = side

    # ---------------- 노멀 · 금속성 ----------------
    gy, gx = np.gradient(height)
    nz = np.ones_like(height)
    k = 2.2
    nx, ny = -gx * k, gy * k  # 이미지 y는 아래로, 탄젠트 v는 위로
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / ln, ny / ln, nz / ln], -1) * 0.5 + 0.5
    mt = np.zeros((H, W, 4), np.float32)
    mt[..., 0] = np.clip(metal, 0, 1)
    mt[..., 1] = mt[..., 0]
    mt[..., 2] = mt[..., 0]
    mt[..., 3] = np.clip(smooth_map, 0, 1)
    to8 = lambda a: Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))
    return to8(alb), to8(nrm), to8(mt)
