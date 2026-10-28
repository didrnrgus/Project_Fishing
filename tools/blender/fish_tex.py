"""
물고기 텍스처를 코드로 그린다 (numpy + Pillow). docs/09-art-plan.md 3-5.

아틀라스 2048×1024 하나에 몸·지느러미·눈·수염을 모두 담는다 (머티리얼 2개가 같은 텍스처를 쓴다).
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
from PIL import Image, ImageFilter

S = 2                  # 해상도 배율 (1 = 예전 1024×512). 픽셀 단위 값은 모두 S를 곱한다
W, H = 1024 * S, 512 * S
BW = 768 * S  # 몸 영역 폭


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


def head_end_of(d):
    """머리 끝(아가미 뚜껑 뒤 가장자리)의 몸 구간 위치 s. 텍스처(비늘·아가미)와 모델(아가미 뚜껑 턱)이 같이 쓴다."""
    if d['type'] == 'G':
        return 0.09
    return 0.13 if d['type'] == 'H' else 0.2


def gill_edge(d, v):
    """v(0 배 ~ 1 등)에서 아가미 뚜껑 뒤 가장자리의 s.
    실제 아가미 뚜껑처럼 위쪽(목덜미)은 거의 곧게 내려오고, 옆구리 위쪽이 뒤로 가장 볼록하며, 아래는 목(협부) 쪽으로
    길게 쓸려 나간다. 예전에는 위아래 대칭인 타원이라 머리에 둥근 헬멧을 씌운 것 같았다."""
    he = head_end_of(d)
    if d['type'] == 'G':
        return he - 0.08 * (v - 0.5) ** 2 * 4
    up = np.clip((v - 0.64) / 0.36, 0.0, 1.0)
    lo = np.clip((0.64 - v) / 0.64, 0.0, 1.0)
    return he + 0.012 - 0.045 * up ** 2 - 0.1 * lo ** 1.7


def mouth_len(d):
    """입 선(입술 사이 홈)이 주둥이 끝에서 뻗는 길이 (몸 구간 s). 텍스처·모델 홈·턱뼈가 같이 쓴다."""
    return 0.035 + 0.03 * max(0, d.get('mouth', 0)) + (0.03 if d['type'] == 'C' else 0.0)


def mouth_v(d):
    """입 선의 높이 (v: 0 배 ~ 1 등). 주둥이 끝에서 뒤로 갈수록 0.6 s만큼 올라간다."""
    return 0.5 + 0.35 * d.get('mouth', 0) * 0.3


def eye_uv(d):
    """눈 가운데 (s, v)와 눈 반지름을 s·v 단위로. 텍스처 눈 둘레와 모델 눈두덩이 같이 쓴다."""
    e = d['eye']
    ez = e['z']
    ecz = math.copysign(abs(ez) ** (d.get('xsec', 2.0) / 2.0), ez)
    ev = math.acos(max(-1.0, min(1.0, -ecz))) / math.pi
    eru = e['r'] / d['body_end']
    erv = e['r'] / (math.pi * (d['depth'] / 2 + d['width'] / 2) / 2)
    return e['s'], ev, eru, erv


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
    # 잔 얼룩 (단색이 플라스틱처럼 보이지 않게): 큰 얼룩 · 중간 결 · 등쪽 색소 점(멜라닌 세포)
    n = noise(BW, H, 10, rng.integers(1 << 30))
    body *= (0.9 + 0.2 * n)[..., None]
    big = noise(BW, H, 5, rng.integers(1 << 30), 2)
    blend(body, back * 0.8, smooth(0.5, 0.75, big) * 0.22 * smooth(0.35, 0.8, vside) * eyed)
    mid = noise(BW, H, 40, rng.integers(1 << 30), 2)
    body *= (0.95 + 0.1 * mid)[..., None]
    # 색소 점: 픽셀 단위 무작위 점을 살짝 번지게 (1~2픽셀, 등쪽에 많이)
    dens = 0.006 * smooth(0.35, 0.95, vside) * eyed
    pts = (rng.random((H, BW)) < dens).astype(np.float32)
    pts = np.asarray(Image.fromarray((pts * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6 * S)), np.float32) / 255.0
    blend(body, back * 0.3, np.clip(pts * 2.5, 0, 1) * 0.5)
    # 배는 살짝 따뜻하고 덜 얼룩지게
    blend(body, belly * np.array([1.02, 0.98, 0.94], np.float32), smooth(0.3, 0.05, vside) * 0.4 * (1 if not flat else 0))

    metal_b = np.full((H, BW), col.get('metallic', 0.0) * 0.5, np.float32)
    silver = col.get('silver', 0.2)
    smooth_b = np.full((H, BW), 0.55 + 0.3 * silver, np.float32)   # 점액으로 번들거린다
    h_b = np.zeros((H, BW), np.float32)

    head_end = head_end_of(d)
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
        # 비늘 크기는 고르지 않다: 옆구리 가운데가 가장 크고 등·배·꼬리자루·머리 쪽으로 작아진다.
        # 픽셀마다 크기 비율 f로 나눈 값을 누적해(적분) 비늘 좌표를 만든다 → 원은 원대로, 줄 간격만 자연스럽게 바뀐다
        ucol = (np.arange(BW, dtype=np.float32) + 0.5) / BW
        fu = (1 - 0.3 * smooth(0.7, 1.0, ucol)) * (1 - 0.2 * smooth(0.42, 0.22, ucol))
        vrow = 1.0 - (np.arange(H, dtype=np.float32) + 0.5) / H
        if flat:
            vrow = np.where(vrow < 0.5, vrow * 2, (1 - vrow) * 2)
        fv = np.clip(1 - 0.42 * (np.abs(vrow - 0.55) * 2) ** 2, 0.5, 1.0)
        X = np.broadcast_to((np.cumsum(1.0 / fu) / su)[None, :], xx.shape)
        Y = np.broadcast_to((np.cumsum(1.0 / fv) / sv)[:, None], yy.shape)
        R = 0.78
        best_t = np.full(X.shape, 2.0, np.float32)
        best_dx = np.zeros_like(X)
        best_dy = np.zeros_like(X)
        best_id = np.zeros_like(X)
        found = np.zeros(X.shape, bool)
        j0 = np.floor(X)

        def h01(a, b, k):
            return np.mod(np.sin(a * 12.9898 + b * 78.233 + k * 37.719) * 43758.5453, 1.0)
        for dj in (0, 1):
            jc = j0 + dj
            off = (jc % 2) * 0.5
            yc = np.floor(Y - off) + 0.5 + off
            for dyc in (0.0, 1.0, -1.0):
                yy_c = yc + dyc
                # 비늘마다 자리를 조금씩 흐트러뜨린다 (도장 찍은 듯한 격자가 안 보이게)
                cx = jc + (h01(jc, yy_c, 1) - 0.5) * 0.16
                cy = yy_c + (h01(jc, yy_c, 2) - 0.5) * 0.16
                dist = np.sqrt((X - cx) ** 2 + (Y - cy) ** 2) / R
                take = (~found) & (dist <= 1.0)
                best_t = np.where(take, dist, best_t)
                best_dx = np.where(take, (X - cx) / R, best_dx)
                best_dy = np.where(take, (Y - cy) / R, best_dy)
                best_id = np.where(take, h01(jc, yy_c, 3), best_id)
                found |= take
        best_t = np.where(found, best_t, 1.0)
        strength = 0.35
        for p in d['colors'].get('patterns', []):
            if p[0] == 'scale_edge':
                strength = p[1]
        # 비늘이 잘수록(수가 많을수록) 테두리가 덜 보인다: 잔 비늘을 굵은 선으로 그리면 그물·자처럼 보인다
        strength *= min(1.0, max(0.5, (40.0 / nsc) ** 0.8))
        # 비늘이 없는 머리: 잉어류(A·B)는 아가미 뚜껑까지 맨살이다. 농어·도미류(C·D)는 실제로 볼과 아가미 뚜껑까지
        # 비늘이 덮고 주둥이·눈 위·턱만 맨살이다 (머리 전체를 맨살 판으로 칠하면 헬멧을 쓴 것 같았다).
        # 목덜미(등 쪽)는 경계를 넓게 풀어 머리와 등이 이어지게
        gx0 = gill_edge(d, vside)
        if d['type'] in ('C', 'D') or d.get('head_scales'):
            es_, ev_, eru_, erv_ = eye_uv(d)
            cheek = es_ + eru_ * 1.5
            below = smooth(ev_ + erv_ * 0.9, ev_ - erv_ * 0.2, vside)
            gx0 = gx0 + (np.minimum(gx0, cheek) - gx0) * below
        fade = smooth(gx0 + 0.004, gx0 + 0.03 + 0.05 * smooth(0.7, 1.0, vside), u) * smooth(1.02, 0.9, u)
        # 비늘 테가 몸을 평균적으로 어둡게 하므로 맨살 머리도 그만큼 낮춘다 (머리만 밝게 떠서 따로 붙은 판처럼 보였다)
        body *= (1 - strength * 0.12 * (1 - smooth(gx0 - 0.02, gx0 + 0.03, u)))[..., None]
        # 드러난 쪽(꼬리 쪽 호) 가장자리에 짙은 색소 띠, 덮인 밑동은 살짝 어둡게, 가운데는 은빛으로 볼록
        post = smooth(-0.15, 0.4, best_dx)
        rim = smooth(0.72, 0.96, best_t) * post
        pocket = smooth(0.55, 0.9, best_t) * smooth(0.2, -0.4, best_dx)
        dome = (1 - best_t ** 2) * (0.6 + 0.4 * best_dx)
        # 비늘 나이테(성장선)와 방사선: 아주 가는 결
        circ = 0.5 + 0.5 * np.sin(best_t * 34.0)
        ang = np.arctan2(best_dy, best_dx)
        radii = smooth(0.85, 1.0, 0.5 + 0.5 * np.cos(ang * 9.0)) * smooth(0.0, 0.3, -best_dx)
        var = (best_id - 0.5) * 2.0   # 비늘마다 -1 ~ 1
        # 테두리 대비는 낮추고(자로 그은 격자처럼 보였다) 비늘마다 밝기 차이(빛을 받는 각도가 조금씩 다름)는 키운다
        body *= (1 - strength * 0.42 * rim * fade)[..., None]
        body *= (1 - strength * 0.15 * pocket * fade)[..., None]
        body *= (1 + (0.11 * var + 0.1 * dome * silver) * fade)[..., None]
        # 비늘 가운데는 옆구리 색이 밝게 비친다 (잉어·붕어의 황동빛, 은빛 고기는 은색)
        blend(body, np.minimum(side * 1.25, 1.0), dome * fade * smooth(0.15, 0.6, vside) * (0.12 + 0.2 * strength))
        h_b += (dome * 0.7 - rim * 0.7 + circ * 0.06 + radii * 0.08) * fade
        smooth_b += (-0.12 * rim + 0.06 * var + 0.05 * dome) * fade
        metal_b += (silver * 0.25 * dome + 0.08 * var * silver) * fade
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
        blend(body, side * 0.6 + back * 0.4, line * 0.3)
        h_b += line * 0.4
        if nsc:
            # 옆줄 비늘마다 감각 구멍(짧은 짙은 홈)
            pore = np.exp(-((v - ll_v) ** 2) / (2 * 0.004 ** 2)) * smooth(0.4, 0.15, best_t) * smooth(0.2, 0.26, u)
            blend(body, back * 0.35, pore * 0.6)
            h_b -= pore * 0.6

    # ---- 아가미 뚜껑 ----
    gx = gill_edge(d, vside)
    not_ceph = 0.0 if d.get('ceph') else 1.0
    # 아가미 틈은 목덜미 위로 넘어가지 않는다 (머리 꼭대기까지 둘러진 선이 헬멧 테두리처럼 보였다)
    gband = smooth(0.05, 0.25, vside) * smooth(0.86, 0.66, vside) * eyed * not_ceph
    if d['type'] == 'G':
        # 장어는 아가미 뚜껑 대신 가슴지느러미 앞의 작은 구멍뿐 (머리 둘레에 선을 두르면 헬멧처럼 보였다)
        gband = smooth(0.2, 0.3, vside) * smooth(0.5, 0.4, vside) * eyed * 0.6
    # 아가미 뚜껑 뒤 가장자리: 뼈판 끝은 밝은 턱, 바로 뒤 아가미 틈은 짙은 그늘
    edge = np.exp(-((u - gx + 0.004) ** 2) / (2 * 0.0035 ** 2)) * gband
    slit = smooth(gx - 0.002, gx + 0.004, u) * smooth(gx + 0.022, gx + 0.006, u) * gband
    blend(body, back * 0.45, slit * 0.35)
    body *= (1 + 0.05 * edge)[..., None]
    h_b += edge * 1.0 - slit * 0.8
    if not d.get('ceph'):
        head = smooth(gx + 0.002, gx - 0.01, u) * eyed
        # 머리 피부: 비늘 없이 가는 주름·모공 결, 더 매끈하고 살짝 진함
        wr = noise(BW, H, 120, rng.integers(1 << 30), 2)
        h_b += (wr - 0.5) * 0.25 * head
        smooth_b += 0.05 * head
        # 머리도 몸처럼 얼룩·색소가 이어진다 (비늘 없는 매끈한 판이 따로 붙은 헬멧처럼 보이지 않게)
        hm = noise(BW, H, 24, rng.integers(1 << 30), 3)
        body *= (1 + (hm - 0.5) * 0.16 * head)[..., None]
        blend(body, back * 0.9, head * 0.15 * smooth(0.4, 0.9, vside))
        # 아가미 뚜껑 판(전새개골 곡선 ~ 뒤 가장자리): 은빛이 돌고, 뒤쪽 위 관절에서 퍼지는 방사 줄
        pre = gx - 0.055 - 0.02 * (vside - 0.5)
        plate = smooth(pre - 0.004, pre + 0.006, u) * smooth(gx + 0.002, gx - 0.004, u) * gband
        hinge_u, hinge_v = gx - 0.01, 0.72
        pang = np.arctan2((v - hinge_v) * 0.5, (u - hinge_u))
        stri = 0.5 + 0.5 * np.cos(pang * 70.0)
        body *= (1 + 0.035 * plate)[..., None]
        metal_b += plate * (0.15 + 0.35 * silver)
        h_b += plate * stri * 0.12
        groove = np.exp(-((u - pre) ** 2) / (2 * 0.002 ** 2)) * gband * smooth(0.25, 0.45, vside)
        blend(body, back * 0.6, groove * 0.15)
        h_b -= groove * 0.35
        # 눈 둘레: 눈에 바로 붙은 가는 그늘만 (넓고 짙은 테는 눈을 왕방울처럼 보이게 했다).
        # 그 바깥 눈두덩(뼈 테)은 모델이 도드라지게 하고, 텍스처는 위쪽을 살짝 진하게(눈썹처럼 그늘)
        es, ev, eru, erv = eye_uv(d)
        ed = np.sqrt(((u - es) / eru) ** 2 + ((vside - ev) / erv) ** 2)
        noflat = 0 if flat else 1
        orbit = smooth(1.3, 1.02, ed) * smooth(0.85, 0.98, ed) * eyed * noflat
        blend(body, back * 0.55, orbit * 0.3)
        h_b -= orbit * 0.25
        brow = np.exp(-((ed - 1.35) / 0.3) ** 2) * smooth(ev - erv * 0.2, ev + erv * 0.8, vside) * eyed * noflat
        blend(body, back * 0.8, brow * 0.25)
        h_b += np.exp(-((ed - 1.35) / 0.3) ** 2) * 0.35 * eyed * noflat
        # 콧구멍: 눈과 주둥이 사이, 눈보다 조금 위에 작은 구멍 두 개 (앞·뒤)
        for k in (0.0, 1.0):
            nu_, nv_ = es * (0.42 + 0.16 * k), ev + erv * 0.25
            nd = np.sqrt(((u - nu_) / (eru * 0.15)) ** 2 + ((vside - nv_) / (erv * 0.13)) ** 2)
            pit = smooth(1.0, 0.4, nd) * eyed * noflat
            blend(body, back * 0.45, pit * 0.35)    # 진하면 돼지코처럼 보인다
            h_b -= pit * 0.45
            h_b += np.exp(-((nd - 1.1) / 0.3) ** 2) * 0.2 * eyed * noflat
        # 주둥이 위는 진하게
        blend(body, back * 0.8, smooth(0.08, 0.0, u) * smooth(0.45, 0.8, vside) * 0.4 * eyed)
    # 입 선
    if not flat and not d.get('ceph'):
        mlen = mouth_len(d)
        md = v - mouth_v(d) - (u * 0.6)
        reach = smooth(mlen, mlen * 0.6, u)
        mouth = np.exp(-(md ** 2) / (2 * 0.006 ** 2)) * reach
        # 입술: 갈라진 선 위아래로 도톰하고 밝은 살
        lips = np.exp(-(md ** 2) / (2 * 0.022 ** 2)) * smooth(mlen * 1.25, mlen * 0.7, u)
        blend(body, belly * 0.85 + side * 0.15, lips * 0.45)
        h_b += lips * 0.6 - mouth * 0.9
        blend(body, back * 0.25, mouth * 0.8)

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
            rr = rng.uniform(*r) * S
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
            bandn = noise(BW, H, 18, rng.integers(1 << 30), 2)   # 띠 가장자리가 고르지 않게
            nb, (u0, u1), bw = arg['n'], arg['u'], arg['w']
            c = hexrgb(arg['color'])
            vr = arg.get('v', (0, 1))
            for k in range(nb):
                cu = u0 + (u1 - u0) * k / max(1, nb - 1)
                w = bw * (0.55 if arg.get('taper') and k >= 4 else 1.0)
                wob = 0.01 * np.sin(v * 9 + k) + 0.012 * (bandn - 0.5)
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
            # 고등어 물결 무늬: 자로 그린 사인파가 아니라 노이즈로 휘고, 굵기가 들쭉날쭉하고, 군데군데 끊긴다
            warp = noise(BW, H, 14, rng.integers(1 << 30), 2)
            thick = noise(BW, H, 22, rng.integers(1 << 30), 2)
            wv = np.sin(u * 90 + np.sin(v * 25) * 2.5 + v * 30 + (warp - 0.5) * 7.0) * 0.5 + 0.5
            th = 0.74 + 0.12 * (thick - 0.5) * 2
            m = smooth(th, th + 0.14, wv) * smooth(arg['v'][0], arg['v'][0] + 0.08, v) * smooth(0.12, 0.2, u)
            m *= smooth(0.25, 0.45, thick * 0.6 + warp * 0.4)
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
            px, py, rr = arg['u'] * BW, (1 - arg['v']) * H, arg['r'] * S
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
    # 막 사이로 뼈 줄기(기조)가 뻗고, 끝으로 가며 둘로 갈라지고 마디가 진다. 막은 줄기보다 밝고 얇아 끝쪽이 비친다
    FWb, FHb = 128 * S, 256 * S
    for gi, key in enumerate(('fin_up', 'fin_low')):
        x0 = BW + gi * FWb
        base, tip, ta = col[key]
        base, tip = hexrgb(base), hexrgb(tip)
        fy, fx = np.mgrid[0:FHb, 0:FWb].astype(np.float32)
        fv = 1.0 - (fy + 0.5) / FHb     # 0 밑동 ~ 1 끝
        fu = (fx + 0.5) / FWb
        c = base[None, None] * (1 - fv[..., None]) + tip[None, None] * fv[..., None]
        nray = 14
        xr = fu * nray + 0.03 * np.sin(fv * 9.0)          # 줄기가 살짝 휜다
        ray1 = smooth(0.62, 0.95, 0.5 + 0.5 * np.cos(xr * 2 * math.pi))
        ray2 = smooth(0.7, 0.97, 0.5 + 0.5 * np.cos(xr * 4 * math.pi)) * smooth(0.45, 0.6, fv)   # 갈라진 가지
        rays = np.maximum(ray1 * (1 - 0.5 * smooth(0.45, 0.6, fv)), ray2)
        if d['type'] == 'G':
            # 장어 지느러미: 가시 없는 부드러운 막. 줄기가 진하면 몸 길이만큼 늘어난 텍스처에서 톱니처럼 보였다
            rays = rays * 0.25
        seg = smooth(0.8, 0.95, 0.5 + 0.5 * np.cos((fv * 26 + np.floor(xr) * 0.37) * 2 * math.pi)) * smooth(0.25, 0.4, fv)
        membrane = 1 - rays
        c *= (0.78 + 0.3 * membrane)[..., None]           # 줄기는 짙고 막은 밝다
        c *= (1 - 0.25 * rays * seg)[..., None]           # 마디
        c *= (0.92 + 0.12 * noise(FWb, FHb, 10, rng.integers(1 << 30), 2))[..., None]
        blend(c, tip * 0.6, smooth(0.8, 0.97, fv) * 0.35)  # 끝 가장자리가 짙다
        if any(p[0] == 'finspots' for p in col.get('patterns', [])):
            fs = hexrgb(next(p for p in col['patterns'] if p[0] == 'finspots')[1])
            nn = noise(FWb, FHb, 8, rng.integers(1 << 30), 2)
            blend(c, fs, smooth(0.55, 0.65, nn) * 0.8)
        # 알파 (Fishing/FishFin이 반투명으로 섞는다): 밑동·줄기는 거의 불투명, 막은 끝으로 갈수록 얇아져 비친다.
        # 가장자리는 줄기 끝이 조금 더 나오고 줄기 사이 막이 살짝 들어간 물결 모양 (예전: 노이즈 구멍으로 해진 종이 같았다)
        mem_a = 0.82 - (0.82 - ta * 0.75) * smooth(0.05, 1.0, fv)
        a = np.maximum(mem_a, rays * (0.95 - 0.25 * fv))
        edge_v = 0.975 - 0.045 * membrane if d['type'] != 'G' else np.full_like(membrane, 0.96)
        a *= smooth(edge_v, edge_v - 0.035, fv)
        alb[0:FHb, x0:x0 + FWb, :3] = np.clip(c, 0, 1)
        alb[0:FHb, x0:x0 + FWb, 3] = np.clip(a, 0, 1)
        height[0:FHb, x0:x0 + FWb] = rays * 0.7 * (1 - fv * 0.4) + rays * seg * 0.2
        smooth_map[0:FHb, x0:x0 + FWb] = 0.5 + 0.15 * membrane
        metal[0:FHb, x0:x0 + FWb] = 0.0

    # ---------------- 눈 ----------------
    # 홍채: 동공 쪽으로 퍼지는 섬유 결, 금빛 반점, 바깥 테는 짙게. 하이라이트는 셰이더의 반사가 만든다
    iris = hexrgb(d['eye'].get('iris', '#c0a040'))
    ES = 128 * S
    ey, ex = np.mgrid[0:ES, 0:ES].astype(np.float32)
    cxe = (ES - 1) / 2
    r = np.sqrt((ex - cxe) ** 2 + (ey - cxe) ** 2) / (ES / 2)
    th = np.arctan2(ey - cxe, ex - cxe)
    fib = noise(ES, ES, 6, rng.integers(1 << 30), 1)
    fibers = 0.5 + 0.5 * np.sin(th * 60 + fib * 6.0)
    # 물고기 눈은 동공이 크고(머리 밖으로 보이는 눈의 60%쯤) 홍채는 가는 금속빛 고리다. 예전에는 동공이 작고 홍채 바깥에 짙은 테,
    # 그 바깥에 또 짙은 피부 테가 있어 만화 속 왕방울 눈처럼 보였다
    ty = (ey - cxe) / (ES / 2)                                              # -1 위 ~ +1 아래 (이미지 y는 아래로)
    eye = iris[None, None] * (0.7 + 0.3 * fibers[..., None]) * (0.55 + 0.5 * smooth(0.76, 0.5, r))[..., None]
    fleck = smooth(0.7, 0.85, noise(ES, ES, 30, rng.integers(1 << 30), 1)) * smooth(0.5, 0.56, r) * smooth(0.76, 0.66, r)
    blend(eye, np.minimum(iris * 1.35, 1.0), fleck * 0.5)
    blend(eye, iris * 0.4, smooth(-0.1, -0.75, ty) * 0.55)                  # 홍채 위쪽은 색소로 어둡다 (등 쪽 그늘)
    blend(eye, np.minimum(iris * 1.5 + 0.1, 1.0), smooth(0.54, 0.5, r) * smooth(0.46, 0.5, r) * 0.6)   # 동공 둘레 밝은 고리
    blend(eye, iris * 0.6, smooth(0.68, 0.76, r) * 0.6)                     # 홍채 바깥은 살짝 어둡게 (검은 테는 아님)
    skin = hexrgb(col['side']) * 0.75 + hexrgb(col['back']) * 0.25
    blend(eye, skin, smooth(0.77, 0.83, r))                                  # 눈 가장자리는 머리 피부색으로 이어진다
    blend(eye, np.array([0.015, 0.02, 0.025], np.float32), smooth(0.5, 0.46, r))   # 동공
    blend(eye, np.array([0.06, 0.1, 0.13], np.float32), smooth(0.4, 0.05, r) * 0.25)
    hl = np.exp(-(((ex - cxe * 0.82) ** 2 + (ey - cxe * 0.74) ** 2) / (30.0 * S * S)))
    eye += hl[..., None] * 0.35
    ey0 = 256 * S
    alb[ey0:ey0 + ES, BW:BW + ES, :3] = np.clip(eye, 0, 1)
    smooth_map[ey0:ey0 + ES, BW:BW + ES] = 0.96
    metal[ey0:ey0 + ES, BW:BW + ES] = 0.0
    height[ey0:ey0 + ES, BW:BW + ES] = smooth(0.95, 0.85, r) * 0.3

    # ---------------- 조각: 수염, 부리 끝 ----------------
    alb[320 * S:384 * S, 896 * S:960 * S, :3] = side * 0.55 + back * 0.3   # 수염: 살빛 (상아색이면 엄니처럼 보인다)
    alb[320 * S:384 * S, 960 * S:1024 * S, :3] = np.array([0.8, 0.2, 0.15]) if d['id'] == 'halfbeak' else back * 0.6
    alb[384 * S:512 * S, BW:, :3] = side
    alb[256 * S:320 * S, 896 * S:1024 * S, :3] = side

    # ---------------- 노멀 · 금속성 ----------------
    gy, gx = np.gradient(height)
    nz = np.ones_like(height)
    k = 2.2 * S
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
