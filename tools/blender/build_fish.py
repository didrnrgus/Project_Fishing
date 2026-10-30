"""
어종별 물고기 모델 + 애니메이션 5종 + 텍스처를 만든다. docs/09-art-plan.md 3장.

  python -I tools/blender/build_fish.py                    # 32종 모두 FBX·텍스처·아이콘
  python -I tools/blender/build_fish.py --only carp        # 하나만 (쉼표로 여러 개)
  python -I tools/blender/build_fish.py --preview --only carp --out Screenshots/fish   # 미리보기 렌더 (옆·위·비스듬히)
  python -I tools/blender/build_fish.py --sheet docs/images/32_fish_models.png         # 전체 어종 한 장 (도감용 확인)
  python -I tools/blender/build_fish.py --sheet docs/images/32_fish_models.png --sheet-only   # 다시 만들지 않고 렌더해 둔 그림(Screenshots/fish/sheet)으로 시트만
  python -I tools/blender/build_fish.py --no-icons         # 아이콘 렌더 건너뛰기

결과 (Unity):
  Assets/Resources/Fish/Species/<id>.fbx    몸(Body) + 지느러미(Fins) + LOD1, 뼈, 클립 Swim_Slow/Swim_Fast/Thrash/Flop/Idle_Hover
  Assets/Resources/Fish/Textures/<id>.png, <id>_n.png(노멀), <id>_m.png(R 금속성, A 매끈함)
  Assets/Resources/Icons/Fish/<id>.png      도감 목록 아이콘 (256×128, 투명 배경, 옆모습)

모델 좌표 (Blender): 길이 1m, 주둥이 y = -0.5, 꼬리 끝 y = +0.5, 위 +Z.
FBX는 캐릭터와 같은 축(-Y 앞, Z 위)으로 내보내서 Unity에서는 머리가 +Z를 본다.
"""
import math
import os
import sys

ARGS = sys.argv[1:]
sys.argv = [sys.argv[0]]
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT_FBX = os.path.join(ROOT, 'Assets', 'Resources', 'Fish', 'Species')
OUT_TEX = os.path.join(ROOT, 'Assets', 'Resources', 'Fish', 'Textures')
OUT_ICON = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Fish')

import bpy  # noqa: E402
import bmesh  # noqa: E402
import mathutils  # noqa: E402
import numpy as np  # noqa: E402

import cephalopod as CP  # noqa: E402
import fish_species as FS  # noqa: E402
import fish_tex  # noqa: E402

NR, NA = 84, 60   # 몸 고리 수, 고리 둘레 버텍스 수 (+ 머리·아가미 뚜껑 근처에 고리를 더 촘촘히)
FPS = 30


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


# =====================================================================
#  몸 모양 (단면 로프트)
# =====================================================================

class Body:
    """어종 수치로 몸통 단면을 계산한다. s = 몸통 구간 0(주둥이) ~ 1(꼬리자루)."""

    def __init__(self, d):
        self.d = d
        self.H = d['depth']
        self.W = d['width']
        self.be = d['body_end']
        self.y0 = -0.5
        # 주둥이를 둥글고 뭉툭하게 (지수가 클수록 뾰족한 원뿔 끝이 된다).
        # 장어형(G)도 머리가 둥글다 (예전 1.0은 바늘처럼 뾰족한 주둥이였다). 갈치는 어종 값으로 뾰족하게
        self.blunt = d.get('blunt', 0.55 if d['type'] == 'G' else 0.8)
        # 입 높이 (주둥이 끝 단면이 남는 비율). 장어·두족류는 둥근 머리 그대로
        self.m0 = d.get('mouth_h', 0.0 if d['type'] == 'G' or d.get('ceph') or d.get('shark') else 0.17)
        # 등·배 능선: 몸 단면 위아래가 뾰족해진다 (꼬리 쪽으로 갈수록 더). 둥근 원통처럼 보이지 않게
        self.keel = d.get('keel', 0.0 if d.get('ceph') else 0.1 if d['type'] == 'G' else 0.3)

    def keel_k(self, s):
        return self.keel * (0.35 + 0.65 * fish_tex_smooth(0.3, 0.95, s))

    def y(self, s):
        return self.y0 + s * self.be

    def snout(self, s, en, m0):
        """주둥이 쪽 단면 비율 (0~1). 반구처럼 둥근 돔이 아니라 입 높이(m0)만큼 남은 쐐기를 끝(rt)에서 둥글게 닫는다.
        이마·턱 선이 입으로 곧게 모여 머리가 총알이 아닌 물고기 머리가 된다."""
        tm = self.d['tmax']
        base = m0 + (1 - m0) * math.sin(math.pi / 2 * s / tm) ** (en * 1.3)
        rt = 0.03
        tip = math.sqrt(max(0.0, 1 - (1 - min(1.0, s / rt)) ** 2))
        return base * tip

    def depth(self, s):
        d, H = self.d, self.H
        tm = d['tmax']
        en = (0.2 + d['snout'] * 0.7) * self.blunt
        if s < tm:
            return H * self.snout(s, en, self.m0) * self.head_flat(s)
        disc = d.get('disc')
        if disc:
            # 가오리: 원반이 disc에서 끝나고 그 뒤는 가늘어지는 꼬리 (예전에는 꼬리지느러미 판을 길게 늘여서 종이띠 같았다)
            if s >= disc:
                return H * d['ped'] * (1 - 0.75 * (s - disc) / (1 - disc))
            t = (s - tm) / (disc - tm)
            return H * (d['ped'] + (1 - d['ped']) * math.cos(math.pi / 2 * t) ** 1.5)
        t = (s - tm) / (1 - tm)
        # hold: 몸통 굵기를 그만큼 유지하다가 꼬리로 가늘어진다 (장어: 몸 대부분이 같은 굵기)
        hold = d.get('hold', 0.0)
        t = max(0.0, (t - hold) / (1 - hold))
        return H * (d['ped'] + (1 - d['ped']) * math.cos(math.pi / 2 * t) ** 1.5) * self.head_flat(s)

    def head_flat(self, s):
        """아귀처럼 위아래로 눌린 넓적한 머리: 머리 쪽 높이를 줄인다 (몸 뒤쪽은 그대로)."""
        hf = self.d.get('head_flat', 0.0)
        return 1 - hf * (1 - fish_tex_smooth(0.04, 0.5, s)) if hf else 1.0

    def width(self, s):
        d = self.d
        tm = d['tmax'] * 0.8
        en = (0.2 + d['snout'] * 0.7) * 0.85 * self.blunt
        if s < tm:
            w = self.W * self.snout(s, en, self.m0 * 0.8)
        else:
            t = (s - tm) / (1 - tm)
            hold = d.get('hold', 0.0)
            t = max(0.0, (t - hold) / (1 - hold))
            # 꼬리 끝 폭 비율: 꼬리자루는 옆으로 얇은 칼날이다 (예전 0.32는 위에서 보면 머리부터 꼬리까지 굵기가 같은 소시지였다)
            tw = d.get('tail_w', 0.17)
            disc = d.get('disc')
            if disc and s >= disc:
                w = self.W * tw * (1 - 0.75 * (s - disc) / (1 - disc))
            else:
                if disc:
                    t = (s - tm) / (disc - tm)
                w = self.W * (tw + (1 - tw) * math.cos(math.pi / 2 * t) ** 1.1)
        hw = d.get('head_w', 1.0)
        if hw != 1.0 and s < 0.35:
            w *= 1 + (hw - 1) * (1 - s / 0.35) * min(1, s / 0.05)
        return w

    def zc(self, s):
        m = self.d.get('mouth', 0.0)
        z = m * 0.32 * self.H * max(0.0, 1 - s / 0.32) ** 2
        if self.d.get('shark'):
            # 상어 주둥이: 코끝이 몸 가운데보다 높이 떠서 배 쪽 입 위로 처마처럼 나온다. 턱 밑이 코끝으로 비스듬히 올라간다
            z += self.d['shark'].get('rostrum', 0.32) * self.H * max(0.0, 1 - s / 0.3) ** 2
        return z

    def arch(self, s):
        a = self.d['arch']
        t = min(1, max(0, (s - 0.6) / 0.4))
        return a + (0.5 - a) * t * t * (3 - 2 * t)

    def top(self, s):
        top = self.depth(s) * self.arch(s)
        # hump: 이마 혹 (혹돔·나폴레옹피시) — 눈 위 등선에 솟은 둥근 혹 (높이 = 몸 높이 비율, 위치 s)
        hump = self.d.get('hump')
        if hump:
            hs = self.d.get('hump_s', 0.13)
            top += self.H * hump * math.exp(-((s - hs) / 0.075) ** 2) * min(1.0, s / 0.05)
        return top

    def bot(self, s):
        return self.depth(s) * (1 - self.arch(s))

    def ring_point(self, s, a):
        """a: 0 = 배 가운데, π = 등 가운데, (0, π) 오른쪽(+x), (π, 2π) 왼쪽."""
        xs = self.d['xsec']
        cz, cx = -math.cos(a), math.sin(a)
        x = self.width(s) / 2 * math.copysign(abs(cx) ** (2 / xs), cx)
        x *= 1 - self.keel_k(s) * abs(cz) ** 3
        h = self.top(s) if cz > 0 else self.bot(s)
        z = self.zc(s) + h * math.copysign(abs(cz) ** (2 / xs), cz)
        return (x, self.y(s), z)

    def surface_x(self, s, zrel):
        """옆면 x (zrel: -1 배 ~ +1 등)."""
        xs = self.d['xsec']
        keel = 1 - self.keel_k(s) * abs(zrel) ** (1.5 * xs)
        return self.width(s) / 2 * max(0.0, 1 - abs(zrel) ** xs) ** (1 / xs) * keel

    def z_at(self, s, zrel):
        return self.zc(s) + (self.top(s) if zrel > 0 else self.bot(s)) * zrel


def body_uv(d, s, a):
    u = s * 0.75
    if d.get('flat'):
        v = a / (2 * math.pi)
    else:
        v = a / math.pi if a <= math.pi else (2 * math.pi - a) / math.pi
    return (u, v)


def fish_tex_smooth(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0 + 1e-9)))
    return t * t * (3 - 2 * t)


def build_body_mesh(d, B):
    verts, faces, uvs = [], [], []
    ss = [0.006 + (1 - 0.006) * (0.5 - 0.5 * math.cos(math.pi * i / (NR - 1))) for i in range(NR)]
    he = fish_tex.head_end_of(d)
    flat = d.get('flat')
    if not flat:
        # 머리(입술·눈두덩)와 아가미 뚜껑 턱이 뭉개지지 않게 그 근처에 고리를 더 넣는다
        top = he + 0.03
        if d.get('shark'):
            top = max(top, d['shark']['gills'][1] + 0.02)   # 아가미구멍 홈이 뭉개지지 않게
        if d.get('gape'):
            top = max(top, d['gape'] * 1.3)
        extra = [0.004 * i for i in range(1, int(top / 0.004))]
        ss = sorted(set(round(x, 4) for x in ss + extra))
    nr = len(ss)
    relief = not flat and not d.get('ceph')
    shark = d.get('shark')
    if relief:
        es, ev, eru, erv = fish_tex.eye_uv(d)
        er = d['eye']['r']
    for s in ss:
        for k in range(NA):
            a = 2 * math.pi * k / NA
            x, y, z = B.ring_point(s, a)
            v = body_uv(d, s, a)[1]
            if shark:
                # 상어: 아가미 뚜껑 대신 옆구리에 세로로 난 아가미구멍 다섯 줄 (살짝 파인 홈 · 뒤 가장자리 피부가 덮는다)
                g0, g1 = shark['gills']
                for k in range(5):
                    gu = g0 + (g1 - g0) * k / 4 + 0.012 * (v - 0.5)
                    gd = (s - gu) / 0.0035
                    x *= 1 - 0.035 * math.exp(-gd * gd) * fish_tex_smooth(0.28, 0.36, v) * fish_tex_smooth(0.72, 0.64, v)
                # 배 쪽 초승달 입: 홈이 파이고 그 뒤 아래턱이 살짝 도톰하다
                md = (s - fish_tex.shark_mouth_s(d, v)) / 0.0045
                cres = fish_tex_smooth(0.26, 0.2, v)
                k = 1 - 0.1 * math.exp(-md * md) * cres + 0.03 * math.exp(-((md - 2.2) / 1.2) ** 2) * cres
                x *= k
                z = B.zc(s) + (z - B.zc(s)) * k
            elif not flat:
                # 아가미 뚜껑 판이 몸 위로 살짝 덮인다: 뒤 가장자리 바로 앞이 볼록하고 그 뒤로 턱이 진다.
                # 옆면 가운데만 (예전에는 등·배까지 둘러서 머리가 따로 붙은 헬멧처럼 보였다)
                ge = fish_tex.gill_edge(d, v)
                bump = fish_tex_smooth(ge - 0.07, ge - 0.006, s) * fish_tex_smooth(ge + 0.014, ge + 0.002, s)
                side = fish_tex_smooth(0.12, 0.35, v) * fish_tex_smooth(0.88, 0.62, v)
                x *= 1 + (0.018 if d['type'] != 'G' else 0.0) * bump * side   # 0.035면 머리 둘레에 목걸이 같은 턱이 졌다   # 장어는 아가미 뚜껑이 없다 (작은 구멍뿐)
                # 입: 입술 사이가 실제로 파인 홈 (텍스처 입 선과 같은 자리: v = mouth_v + slope × s, 주둥이 끝에서 mlen까지)
                if not d.get('ceph'):
                    mv = fish_tex.mouth_v(d)
                    mlen = fish_tex.mouth_len(d)
                    md = v - mv - fish_tex.mouth_slope(d) * s
                    reach = fish_tex_smooth(mlen * 1.1, mlen * 0.5, s)
                    gw = 0.03 * d.get('gape_w', 1.0)
                    groove = math.exp(-(md ** 2) / (2 * gw ** 2)) * reach
                    # 입술: 홈 위아래가 도톰하다 (입 둘레가 그냥 파인 선이 아니라 살이 있게). lips: 잉어처럼 두툼한 입술
                    lw = 0.055 * d.get('gape_w', 1.0)
                    lips = math.exp(-((abs(md) - lw) ** 2) / (2 * 0.025 ** 2)) * fish_tex_smooth(mlen * 1.3, mlen * 0.6, s)
                    x *= 1 - 0.34 * d.get('gape_d', 1.0) * groove + 0.07 * d.get('lips', 1.0) * lips
                    z = B.zc(s) + (z - B.zc(s)) * (1 - 0.12 * d.get('gape_d', 1.0) * groove + 0.04 * (d.get('lips', 1.0) - 1) * lips)
                    jaw = d.get('jaw')
                    if jaw:
                        # 아래턱이 위턱보다 앞으로 튀어나온다 (아귀): 입 선 아래쪽 앞부분을 앞으로 민다
                        y -= jaw * fish_tex_smooth(0.02, -0.06, md) * fish_tex_smooth(mlen, 0.0, s)
            if relief and abs(x) > 1e-7:
                # 눈두덩: 눈 둘레 뼈 테가 살짝 솟고 바로 붙은 자리는 꺼진다 (눈이 머리에 붙인 단추처럼 보이지 않게)
                v = body_uv(d, s, a)[1]
                ed = math.sqrt(((s - es) / eru) ** 2 + ((v - ev) / erv) ** 2)
                bump = 0.08 * math.exp(-((ed - 1.4) / 0.32) ** 2) - 0.05 * math.exp(-((ed - 1.0) / 0.12) ** 2)   # 눈두덩은 아주 살짝
                if bump:
                    x += math.copysign(er * bump, x)
            verts.append((x, y, z))
    # 주둥이 끝 점, 꼬리자루 끝 점
    nose = len(verts)
    verts.append((0.0, B.y(0.0), B.zc(0.0)))
    tail = len(verts)
    verts.append((0.0, B.y(1.0) + 0.002, B.zc(1.0)))
    loops_uv = []
    for i in range(nr - 1):
        for k in range(NA):
            k2 = (k + 1) % NA
            f = (i * NA + k, i * NA + k2, (i + 1) * NA + k2, (i + 1) * NA + k)
            faces.append(f)
            a1, a2 = 2 * math.pi * k / NA, 2 * math.pi * (k + 1) / NA
            loops_uv.append([body_uv(d, ss[i], a1), body_uv(d, ss[i], a2), body_uv(d, ss[i + 1], a2), body_uv(d, ss[i + 1], a1)])
    for k in range(NA):
        k2 = (k + 1) % NA
        a1, a2 = 2 * math.pi * k / NA, 2 * math.pi * (k + 1) / NA
        faces.append((nose, k2, k))
        loops_uv.append([(0.0, 0.5), body_uv(d, ss[0], a2), body_uv(d, ss[0], a1)])
        base = (nr - 1) * NA
        faces.append((tail, base + k, base + k2))
        loops_uv.append([(0.75, 0.5), body_uv(d, 1.0, a1), body_uv(d, 1.0, a2)])
    return verts, faces, loops_uv


# =====================================================================
#  지느러미
# =====================================================================

def interp(prof, x):
    if not prof:
        return 1.0
    if x <= prof[0][0]:
        return prof[0][1]
    for (x0, y0), (x1, y1) in zip(prof, prof[1:]):
        if x <= x1:
            t = (x - x0) / (x1 - x0 + 1e-9)
            t = t * t * (3 - 2 * t)
            return y0 + (y1 - y0) * t
    return prof[-1][1]


def fin_grid(base_fn, tip_fn, nu, nv, group, curl=None):
    """base_fn(u) → 밑동 점, tip_fn(u) → 끝 점. 격자 버텍스·면·UV."""
    x0 = 0.75 if group == 'up' else 0.875
    verts, faces, uvs = [], [], []
    for j in range(nv + 1):
        fv = j / nv
        for i in range(nu + 1):
            fu = i / nu
            b = mathutils.Vector(base_fn(fu))
            t = mathutils.Vector(tip_fn(fu))
            p = b.lerp(t, fv)
            if curl:
                p += mathutils.Vector(curl(fu, fv))
            verts.append(tuple(p))
    for j in range(nv):
        for i in range(nu):
            a = j * (nu + 1) + i
            faces.append((a, a + 1, a + nu + 2, a + nu + 1))
            uvs.append([(x0 + 0.125 * i / nu, 0.5 + 0.5 * j / nv), (x0 + 0.125 * (i + 1) / nu, 0.5 + 0.5 * j / nv),
                        (x0 + 0.125 * (i + 1) / nu, 0.5 + 0.5 * (j + 1) / nv), (x0 + 0.125 * i / nu, 0.5 + 0.5 * (j + 1) / nv)])
    return verts, faces, uvs


def make_fins(d, B):
    out = []  # (name, verts, faces, uvs, kind)
    H = B.H
    for idx, f in enumerate(d['fins']):
        kind = f['kind']
        grp = f['group']
        if kind in ('dorsal', 'anal', 'adipose'):
            s0, s1 = f['s']
            s1 = min(s1, 1.0)
            span = s1 - s0
            up = kind != 'anal'
            sweep = math.radians(38 if not f.get('spine') else 15)
            h = f['h'] * H
            nu = max(6, int(span * 70))
            spines = max(3, int(span * 42))
            def base(u, s0=s0, span=span, up=up):
                s = s0 + span * u
                z = B.zc(s) + (B.top(s) - 0.0025 if up else -(B.bot(s) - 0.0025))
                return (0.0, B.y(s), z)
            def tip(u, s0=s0, span=span, up=up, h=h, f=f, spines=spines, sweep=sweep):
                s = s0 + span * u
                L = h * interp(f.get('prof'), u)
                if f.get('spiny'):
                    L *= 1 - 0.28 * math.sin(math.pi * u * spines) ** 2
                if s > 0.82 and d['type'] == 'G':
                    # 등·뒷지느러미가 꼬리 끝에서 둥글게 만난다 (끝까지 높으면 꼬리가 칼날처럼 보였다)
                    L *= 0.12 + 0.88 * math.cos(math.pi / 2 * min(1.0, (s - 0.82) / 0.18)) ** 0.7
                bz = B.zc(s) + (B.top(s) if up else -B.bot(s))
                return (0.0, B.y(s) + L * math.sin(sweep), bz + (L if up else -L) * math.cos(sweep))
            # 가시살 사이 막이 아코디언처럼 접힌 주름 (평평한 판이 아니게): 끝으로 갈수록 크게
            pleats = max(3, nu // 3)
            def curl(fu, fv, pleats=pleats, h=h):
                return (h * 0.035 * math.sin(fu * pleats * math.pi) * fv ** 1.3, 0.0, 0.0)
            # 장어 지느러미는 가시 없는 부드러운 막 (주름을 넣으면 긴 지느러미가 톱날처럼 보였다)
            v, fc, uv = fin_grid(base, tip, nu, 4, grp, curl if kind != 'adipose' and d['type'] != 'G' else None)
            out.append((f'{kind}{idx}', v, fc, uv, kind))
        elif kind == 'caudal':
            # 밑동을 꼬리자루 안으로 묻는다: 몸이 뚝 끝나고 지느러미를 꽂은 것처럼 보이지 않게 살이 지느러미로 이어진다
            yb = B.y(1.0) - 0.022
            zb = B.zc(1.0)
            ph = B.depth(1.0) * 0.46
            C = 0.5 - yb
            hc = f['h'] / 2
            shape = f.get('shape', 'fork')
            def cl(vc, shape=shape, C=C):
                a = abs(vc)
                return C * {'fork': 0.62 + 0.38 * a ** 1.2, 'deepfork': 0.38 + 0.62 * a ** 1.05, 'emarg': 0.86 + 0.14 * a,
                            'truncate': 0.95 + 0.05 * a, 'round': 1 - 0.32 * a * a, 'point': max(0.05, 1 - a)}.get(shape, 1.0)
            def ctz(vc, shape=shape, hc=hc):
                k = {'round': 0.85, 'point': 0.5, 'truncate': 0.95}.get(shape, 1.0)
                return vc * hc * k
            def base(u, yb=yb, zb=zb, ph=ph):
                vc = u * 2 - 1
                return (0.0, yb, zb + vc * ph)
            def tip(u, yb=yb, zb=zb):
                vc = u * 2 - 1
                return (0.0, yb + cl(vc), zb + ctz(vc))
            # 꼬리지느러미: 살 사이 잔주름 + 위아래 끝이 살짝 한쪽으로 휜다
            def ccurl(fu, fv, hc=hc):
                return (hc * (0.03 * math.sin(fu * 9 * math.pi) * fv + 0.05 * (fu - 0.5) ** 2 * fv ** 2), 0.0, 0.0)
            v, fc, uv = fin_grid(base, tip, 18, 5, grp, ccurl)
            out.append(('caudal', v, fc, uv, kind))
        elif kind in ('pectoral', 'pelvic'):
            s = f['s'][0]
            h = f['h']
            for side in (1, -1):
                if kind == 'pectoral':
                    zr0, zr1 = -0.4, -0.2
                    if d.get('flat'):
                        zr0, zr1 = -0.05, 0.08
                    def base(u, s=s, side=side, zr0=zr0, zr1=zr1):
                        zr = zr0 + (zr1 - zr0) * u
                        return (side * (B.surface_x(s, zr) - 0.002), B.y(s), B.z_at(s, zr))
                    def tip(u, s=s, side=side, zr0=zr0, zr1=zr1, h=h, f=f):
                        # 부채꼴: 위쪽 살은 위로, 아래쪽 살은 아래로 벌어진다. 끝은 둥글게
                        if f.get('long'):
                            L = h * (0.3 + 0.7 * u ** 1.6)
                        else:
                            L = h * (0.55 + 0.45 * math.sin(math.pi * (0.15 + 0.7 * u)))
                        fan = (u - 0.5) * 0.9
                        dvec = mathutils.Vector((0.42 * side, 1.0, -0.3 + fan)).normalized()
                        b = mathutils.Vector(base(u))
                        return tuple(b + dvec * L)
                else:
                    # 배지느러미: 밑동이 몸을 따라 넓고(옆에서 막대 다리처럼 보이지 않게), 앞 가시가 가장 긴 삼각 부채꼴을
                    # 몸에 붙여 뒤로 눕힌다
                    # thread: 구라미처럼 실처럼 가늘고 긴 배지느러미 (밑동이 좁고 아래 뒤로 늘어진다)
                    thread = f.get('thread')
                    def base(u, s=s, side=side, thread=thread):
                        ss = s + (0.008 if thread else 0.07) * (u - 0.5)
                        return (side * B.width(ss) * (0.1 + 0.1 * u), B.y(ss), B.zc(ss) - B.bot(ss) + 0.002)
                    def tip(u, s=s, side=side, h=h, thread=thread):
                        if thread:
                            L = h * (1.0 - 0.15 * u)
                            dvec = mathutils.Vector((0.2 * side, 1.0, -0.75)).normalized()
                            b = mathutils.Vector(base(u))
                            return tuple(b + dvec * L)
                        L = h * (1.0 - 0.55 * u ** 1.2)
                        dvec = mathutils.Vector((0.3 * side, 1.0, -0.42 + 0.2 * u)).normalized()
                        b = mathutils.Vector(base(u))
                        return tuple(b + dvec * L)
                v, fc, uv = fin_grid(base, tip, 8, 4, grp)
                out.append((f'{kind}_{"L" if side < 0 else "R"}', v, fc, uv, kind + ('_L' if side < 0 else '_R')))
    return out


# =====================================================================
#  눈, 수염, 부리
# =====================================================================

def make_eye(d, B, side):
    e = d['eye']
    r = e['r']
    s, zr = e['s'], e['z']
    flat = d.get('flat')
    if flat:
        # 납작한 고기: 두 눈이 모두 눈 있는 쪽에 (위쪽 눈은 등 가장자리 가까이)
        eside = -1 if flat == 'left' else 1
        if d.get('ray'):
            # 가오리·홍어: 두 눈이 등 가운데 좌우로 나란히 (가자미처럼 한쪽 가장자리로 몰리지 않는다)
            zr = d['ray'].get('eye_z', 0.12) * side
        else:
            zr = 0.35 if side > 0 else -0.05
            s = e['s'] if side > 0 else e['s'] + 0.025
        side = eside
    # 눈 = 머리 겉면에 맞춰 붙은 볼록 렌즈 (보이는 테 반지름 rim·r에서 머리 겉면과 딱 만나고 가운데만 bulge·r 솟는다).
    # 예전에는 머리에 박은 공이라, 보이는 테가 방향마다 달라 동공이 찌그러지고 머리 위가 좁은 어종(꼬치고기)은 눈알 위쪽이
    # 머리 밖으로 솟아 눈 위에 눈이 또 있는 것처럼 보였다. 공일 때 0.3이면 탁구공처럼 튀어나와 보였다.
    # bulge: 가운데가 솟은 정도 (반지름 비율). 다랑어·고등어류(E)는 기름눈꺼풀 밑에 거의 평평하게 묻혀 있다
    bulge = e.get('bulge', 0.1 if d['type'] == 'E' else 0.18)
    if abs(zr) >= 0.55 or (flat and not d.get('ray')):
        return make_eye_ball(d, B, side, s, zr, r, bulge)
    rim = 0.7
    c = mathutils.Vector((side * B.surface_x(s, zr), B.y(s), B.z_at(s, zr)))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=r)
    uvl = bm.loops.layers.uv.new('UV')
    # 보이는 테가 눈 텍스처 반지름 0.8 (동공 0.4 = 눈의 절반, 홍채는 넓은 금빛·은빛 고리)
    for f in bm.faces:
        for l in f.loops:
            p = l.vert.co
            q = math.hypot(p.y, p.z) / r
            k = min(q * 0.8 / rim, 0.98) / q if q > 1e-6 else 0.0
            l[uvl].uv = (0.75 + 0.125 * (0.5 + 0.5 * p.y / r * k), 0.25 + 0.25 * (0.5 + 0.5 * p.z / r * k))

    def surf(y, z):
        s2 = min(1.0, max(0.0, (y - B.y0) / B.be))
        hh = B.top(s2) if z > B.zc(s2) else B.bot(s2)
        zr2 = max(-1.0, min(1.0, (z - B.zc(s2)) / hh)) if hh > 1e-6 else 0.0
        return B.surface_x(s2, zr2)
    for vv in bm.verts:
        p = vv.co.copy()
        y, z = c.y + p.y, c.z + p.z
        t = math.hypot(p.y, p.z) / r
        sx2 = surf(y, z)
        if p.x > 0:
            # 바깥 반구: 테 안은 머리 겉면 + 볼록한 각막, 테 밖은 머리 속으로 살짝 묻힌다
            x = sx2 + (bulge * r * (1 - (t / rim) ** 2) if t < rim else -0.03 * r)
        else:
            x = max(0.0, sx2 - 0.03 * r - r * 0.5 * math.sqrt(max(0.0, 1 - t * t)))   # 뒤쪽은 머리 속 (몸 가운데를 넘지 않게)
        vv.co = mathutils.Vector((side * x, y, z))
    return bm


def make_eye_ball(d, B, side, s, zr, r, bulge):
    """머리 위쪽에 붙은 눈(말뚝망둥어·양태·통구멍)과 가자미 눈: 머리 위로 솟는 공. 겉면이 거의 위를 보는 곳이라
    옆(x)으로 붙이는 렌즈로는 납작하게 찢어졌다. 안쪽 끝은 몸 가운데를 넘지 않게 옆으로 누른다."""
    sx = B.surface_x(s, zr)
    kx = min(r * 0.8, (sx + r * bulge) / 2 * 0.95)
    c = mathutils.Vector((side * (sx + r * bulge - kx), B.y(s), B.z_at(s, zr)))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=r)
    bmesh.ops.scale(bm, vec=(kx / r, 1.0, 1.0), verts=bm.verts)
    uvl = bm.loops.layers.uv.new('UV')

    def outside(th, t):
        yy, zz = c.y + t * r * math.cos(th), c.z + t * r * math.sin(th)
        s2 = min(1.0, max(0.0, (yy - B.y0) / B.be))
        hh = B.top(s2) if zz > B.zc(s2) else B.bot(s2)
        zr2 = max(-1.0, min(1.0, (zz - B.zc(s2)) / hh)) if hh > 1e-6 else 0.0
        return abs(c.x) + kx * math.sqrt(max(0.0, 1 - t * t)) > B.surface_x(s2, zr2)

    def cap_at(th):
        # 보이는 테: 가운데에서 바깥으로 훑어 처음 머리 속으로 들어가는 곳 (머리 위로 솟은 쪽은 끝까지 보인다)
        prev = 0.0
        for i in range(1, 41):
            t = i / 40
            if not outside(th, t):
                lo, hi = prev, t
                for _ in range(10):
                    m = (lo + hi) / 2
                    lo, hi = (m, hi) if outside(th, m) else (lo, m)
                return max(lo, 0.3)
            prev = t
        return 1.0
    NC = 16
    caps = [cap_at(2 * math.pi * i / NC) for i in range(NC)]
    for f in bm.faces:
        for l in f.loops:
            p = l.vert.co
            q = math.hypot(p.y, p.z) / r
            fi = (math.atan2(p.z, p.y) % (2 * math.pi)) / (2 * math.pi) * NC
            i0 = int(fi) % NC
            cap = caps[i0] + (caps[(i0 + 1) % NC] - caps[i0]) * (fi - int(fi))
            k = min(q * 0.8 / cap, 0.98) / q if q > 1e-6 else 0.0
            l[uvl].uv = (0.75 + 0.125 * (0.5 + 0.5 * p.y / r * k), 0.25 + 0.25 * (0.5 + 0.5 * p.z / r * k))
    for vv in bm.verts:
        vv.co += c
    return bm


def make_tube(points, r0, r1, uv_rect, sides=6, taper=1.0):
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    rings = []
    n = len(points)
    for i, p in enumerate(points):
        p = mathutils.Vector(p)
        t = (mathutils.Vector(points[min(i + 1, n - 1)]) - mathutils.Vector(points[max(i - 1, 0)])).normalized()
        a = t.orthogonal().normalized()
        b = t.cross(a)
        r = r1 + (r0 - r1) * (1 - i / (n - 1)) ** taper   # taper > 1: 밑동에서 빨리 가늘어지고 끝은 채찍처럼 길게
        ring = [bm.verts.new(p + (a * math.cos(2 * math.pi * k / sides) + b * math.sin(2 * math.pi * k / sides)) * r) for k in range(sides)]
        rings.append(ring)
    u0, v0, u1, v1 = uv_rect
    for i in range(n - 1):
        for k in range(sides):
            f = bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
            for l in f.loops:
                l[uvl].uv = ((u0 + u1) / 2, (v0 + v1) / 2)
    tipv = bm.verts.new(mathutils.Vector(points[-1]))
    for k in range(sides):
        f = bm.faces.new((rings[-1][k], rings[-1][(k + 1) % sides], tipv))
        for l in f.loops:
            l[uvl].uv = ((u0 + u1) / 2, (v0 + v1) / 2)
    return bm


def make_barbels(d, B):
    out = []
    for (s, zr, L, th, kind) in d['barbels']:
        if kind == 'lure':
            # 아귀 낚싯대 (첫 등지느러미 가시가 변한 것): 머리 위 가운데에 하나, 앞으로 휘고 끝에 미끼 같은 살덩이(에스카)
            start = mathutils.Vector((0.0, B.y(s), B.zc(s) + B.top(s) - 0.003))
            pts = []
            for i in range(11):
                t = i / 10
                pts.append(tuple(start + mathutils.Vector((0.0, -0.45 * L * t * t, L * (t - 0.35 * t * t)))))
            out.append(make_tube(pts, th, th * 0.55, (0.875, 0.25, 0.9375, 0.375), sides=8))
            if L < 0.08:
                continue   # 뒤의 짧은 가시 둘째·셋째는 미끼 없이
            esca = bmesh.new()
            bmesh.ops.create_uvsphere(esca, u_segments=10, v_segments=6, radius=th * 2.6)
            bmesh.ops.scale(esca, vec=(0.8, 1.3, 1.0), verts=esca.verts)
            uvl = esca.loops.layers.uv.new('UV')
            for f in esca.faces:
                for lp in f.loops:
                    lp[uvl].uv = (0.906, 0.31)
            for vv in esca.verts:
                vv.co += mathutils.Vector(pts[-1])
            out.append(esca)
            continue
        # chin: 아래턱 끝 가운데 하나 (대구·쉬드럼), fwd: 앞으로 뻗은 뻣뻣한 수염 (아로와나 턱, 코 수염)
        for side in ((0,) if kind == 'chin' else (1, -1)):
            if kind == 'chin':
                start = mathutils.Vector((0.0, B.y(s), B.zc(s) - B.bot(s) * 0.92))
            else:
                x = B.surface_x(s, zr) * 0.9
                start = mathutils.Vector((side * x, B.y(s), B.z_at(s, zr)))
            dirs = {'down': (0.35, -0.2, -1.0), 'side': (1.0, -0.35, -0.25), 'up': (0.8, -0.3, 0.45), 'flap': (1.0, 0.1, -0.6),
                    'chin': (0.0, 0.25, -1.0), 'fwd': (0.25, -1.0, -0.12)}[kind]
            d0 = mathutils.Vector((dirs[0] * side, dirs[1], dirs[2])).normalized()
            limp = 0.25 if kind == 'fwd' else 1.0
            pts = []
            n = 12 if L > 0.06 else 9
            for i in range(n):
                t = i / (n - 1)
                # 살 수염은 물에서 힘없이 뒤로 흐르고 아래로 처진다 (긴 수염일수록 많이)
                droop = mathutils.Vector((0.12 * side * math.sin(math.pi * t), 0.55, -0.15 - 0.2 * min(1.0, L / 0.1))) * limp
                p = start + d0 * L * t + droop * L * t * t
                pts.append(tuple(p))
            if kind == 'flap':
                # 아귀 머리 둘레 피부 술: 짧고 끝이 납작하게 퍼진다
                out.append(make_tube(pts, th, th * 1.3, (0.875, 0.25, 0.9375, 0.375), sides=5))
            else:
                # 밑동이 도톰하고 빨리 가늘어져 끝은 실처럼 (0.7·0.2는 안 보였고, 1.05·0.3 고른 굵기는 막대처럼 보였다)
                out.append(make_tube(pts, th * 1.1, th * 0.12, (0.875, 0.25, 0.9375, 0.375), sides=8, taper=1.8))
    if d.get('beak'):
        L = d['beak']
        z0 = B.zc(0.02) - B.bot(0.02) * 0.4
        pts = [(0.0, B.y(0.03) - L * t, z0 - 0.004 * t) for t in np.linspace(0, 1, 6)]
        out.append(make_tube(pts, B.depth(0.03) * 0.18, 0.0015, (0.9375, 0.25, 1.0, 0.375), sides=5))
    if d.get('bill'):
        # 황새치: 위턱이 칼처럼 길게 뻗는다 (납작한 칼날 대신 끝으로 가늘어지는 관)
        L = d['bill']
        z0 = B.zc(0.02) + B.top(0.02) * 0.15
        pts = [(0.0, B.y(0.04) - L * t, z0 + 0.01 * t) for t in np.linspace(0, 1, 8)]
        out.append(make_tube(pts, B.depth(0.12) * 0.16, 0.004, (0.875, 0.25, 0.9375, 0.375), sides=8))
    return out


# =====================================================================
#  bpy 오브젝트
# =====================================================================

def mesh_from(name, verts, faces, loops_uv):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    uvl = me.uv_layers.new(name='UV')
    li = 0
    for poly, fuv in zip(me.polygons, loops_uv):
        for k, loop_index in enumerate(poly.loop_indices):
            uvl.data[loop_index].uv = fuv[k]
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def bm_to_obj(name, bms):
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    for src in bms:
        me = bpy.data.meshes.new('tmp')
        src.to_mesh(me)
        bm.from_mesh(me)
        bpy.data.meshes.remove(me)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.name = name
    return o


def solidify(ob, thick):
    m = ob.modifiers.new('Solid', 'SOLIDIFY')
    m.thickness = thick
    m.offset = 0.0
    m.use_rim = True
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=m.name)
    outward_normals(ob)


def outward_normals(ob):
    """면 방향을 바깥으로 맞춘다. 몸통 고리를 만드는 순서 때문에 면이 안쪽을 보고 있었다 — Blender 렌더는 뒷면도 그려서
    티가 안 났지만 Unity는 뒷면을 버려서, 가까운 쪽 몸통이 사라지고 반대편 눈이 비쳐 '한쪽에 눈 두 개'가 됐다.
    눈·수염처럼 떨어진 조각도 조각마다 따로 맞춘다."""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()


# =====================================================================
#  뼈와 가중치
# =====================================================================

def breathing_parts(d, B=None):
    """숨 쉬는 부위. 'jaw': (경첩 s, 경첩 z) — 입꼬리 뒤에서 아래턱이 내려간다, 'gill': 아가미 뚜껑 앞 경계 s.
    납작한 고기(몸을 눕혀 축이 다르다)·두족류는 없고, 장어는 아가미 뚜껑 대신 작은 구멍뿐이라 턱만."""
    if d.get('flat') or d.get('ceph') or d.get('shark'):
        return {}   # 상어는 아가미 뚜껑·옆 입이 없다 (입은 배 쪽)
    B = B or Body(d)
    out = {}
    s_h = fish_tex.mouth_len(d) * 2.0
    vg = fish_tex.mouth_v(d) + fish_tex.mouth_slope(d) * fish_tex.mouth_len(d)
    out['jaw'] = (s_h, B.ring_point(s_h, math.pi * min(0.95, vg))[2])
    if d['type'] != 'G':
        out['gill'] = float(fish_tex.gill_edge(d, 0.55)) - 0.075
    return out


def build_rig(d, B, body, fins):
    nb = d['bones']
    be = B.be
    yR = -0.5 + 0.3 * be
    yE = B.y(1.0)
    arm_data = bpy.data.armatures.new('Rig')
    arm = bpy.data.objects.new('Rig', arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    root = eb.new('Root')
    root.head, root.tail = (0, yR, 0), (0, yR + 0.04, 0)
    head = eb.new('Head')
    head.head, head.tail = (0, yR, 0), (0, -0.5, 0)
    head.parent = root
    segs = [('Head', -0.5, yR)]
    prev = root
    n_sp = nb - 1
    for i in range(n_sp):
        a = yR + (yE - yR) * i / n_sp
        b = yR + (yE - yR) * (i + 1) / n_sp
        sp = eb.new(f'Spine{i + 1}')
        sp.head, sp.tail = (0, a, 0), (0, b, 0)
        sp.parent = prev
        sp.use_connect = i > 0
        prev = sp
        segs.append((sp.name, a, b))
    tail = eb.new('Tail')
    tail.head, tail.tail = (0, yE, 0), (0, 0.5, 0)
    tail.parent = prev
    tail.use_connect = True
    segs.append(('Tail', yE, 0.5))
    pecs = {}
    for f in d['fins']:
        if f['kind'] == 'pectoral':
            s = f['s'][0]
            for side, nm in ((1, 'Pec_R'), (-1, 'Pec_L')):
                b = eb.new(nm)
                x = B.surface_x(s, -0.3)
                b.head = (side * x, B.y(s), B.z_at(s, -0.3))
                b.tail = (side * (x + 0.03), B.y(s) + 0.06, B.z_at(s, -0.3) - 0.015)
                b.parent = root
                pecs[nm] = True
    # 턱(아래턱)과 아가미 뚜껑: 숨 쉬며 입을 뻐끔·아가미 뚜껑을 들썩, 물 밖에서는 헐떡인다.
    # 뼈는 모두 +Y(꼬리 쪽)를 보게 해서 회전 축이 월드 축과 같다 (턱 = X축, 아가미 = Z축)
    breath = breathing_parts(d, B)
    if 'jaw' in breath:
        s_h, z_h = breath['jaw']
        b = eb.new('Jaw')
        b.head, b.tail = (0, B.y(s_h), z_h), (0, B.y(s_h) + 0.02, z_h)
        b.parent = head
    if 'gill' in breath:
        s_p = breath['gill']
        for side, nm in ((1, 'Gill_R'), (-1, 'Gill_L')):
            b = eb.new(nm)
            x = B.surface_x(s_p, 0.0) * 0.85
            b.head, b.tail = (side * x, B.y(s_p), B.zc(s_p)), (side * x, B.y(s_p) + 0.02, B.zc(s_p))
            b.parent = head
    for b in eb:
        b.roll = 0.0
    bpy.ops.object.mode_set(mode='OBJECT')

    centers = [(n, (a + b) / 2) for n, a, b in segs]

    def chain_weights(y):
        if y <= centers[0][1]:
            return {centers[0][0]: 1.0}
        if y >= centers[-1][1]:
            return {centers[-1][0]: 1.0}
        for (n0, c0), (n1, c1) in zip(centers, centers[1:]):
            if c0 <= y <= c1:
                t = (y - c0) / (c1 - c0)
                t = t * t * (3 - 2 * t)
                return {n0: 1 - t, n1: t}
        return {centers[-1][0]: 1.0}

    def assign(ob, extra=None):
        groups = {}
        for v in ob.data.vertices:
            w = chain_weights(v.co.y)
            if extra:
                w = extra(v, w)
            for n, wt in w.items():
                if wt <= 0:
                    continue
                if n not in groups:
                    groups[n] = ob.vertex_groups.new(name=n)
                groups[n].add([v.index], wt, 'REPLACE')
        ob.parent = arm
        mod = ob.modifiers.new('Armature', 'ARMATURE')
        mod.object = arm

    jaw, gill = breath.get('jaw'), breath.get('gill')
    mlen, mv = fish_tex.mouth_len(d), fish_tex.mouth_v(d)

    def vert_v(s, z):
        """버텍스의 몸 둘레 위치 v (0 배 ~ 1 등). ring_point의 거꾸로."""
        sc = min(1.0, max(0.004, s))
        zc = B.zc(sc)
        h = B.top(sc) if z > zc else B.bot(sc)
        zn = max(-1.0, min(1.0, (z - zc) / max(h, 1e-5)))
        cz = math.copysign(abs(zn) ** (d['xsec'] / 2), zn)
        return math.acos(-cz) / math.pi

    def mix(w, name, t):
        w = {k: x * (1 - t) for k, x in w.items()}
        w[name] = w.get(name, 0.0) + t
        return w

    def body_extra(v, w):
        # 눈·수염·부리(주둥이 앞쪽)는 머리에 단단히
        if v.co.y < -0.5 + 0.06:
            w = {'Head': 1.0}
        s = (v.co.y + 0.5) / be
        vv = None
        if jaw and s < jaw[0]:
            # 아래턱: 입 선 아래, 경첩(입꼬리 뒤) 앞쪽
            vv = vert_v(s, v.co.z)
            wj = fish_tex_smooth(0.0, 0.06, mv + fish_tex.mouth_slope(d) * min(s, mlen) - vv) * fish_tex_smooth(jaw[0], mlen * 0.9, s) ** 1.5
            if wj > 0:
                w = mix(w, 'Jaw', wj)
        if gill and abs(v.co.x) > 1e-4 and s < 0.45:
            # 아가미 뚜껑 판: 앞 경계(전새개골)에서 뒤 가장자리로 갈수록 뚜껑을 따라 움직인다 (옆구리 가운데만)
            vv = vert_v(s, v.co.z) if vv is None else vv
            ge = fish_tex.gill_edge(d, vv)
            wg = (fish_tex_smooth(ge - 0.075, ge - 0.012, s) * fish_tex_smooth(ge + 0.003, ge - 0.003, s)
                  * fish_tex_smooth(0.1, 0.28, vv) * fish_tex_smooth(0.9, 0.74, vv))
            if wg > 0:
                w = mix(w, 'Gill_R' if v.co.x > 0 else 'Gill_L', float(wg))
        return w

    assign(body, body_extra)
    pec_info = []
    for f in d['fins']:
        if f['kind'] == 'pectoral':
            s = f['s'][0]
            pec_info.append((B.y(s), f['h']))

    def fin_extra(v, w):
        if pec_info and abs(v.co.x) > 0.003:
            py, ph = pec_info[0]
            # 가슴지느러미: 몸 옆으로 튀어나온 부분
            if py - 0.01 <= v.co.y <= py + ph + 0.02 and abs(v.co.x) > B.surface_x(min(1.0, (v.co.y + 0.5) / B.be), -0.3) * 0.9:   # 몸 끝을 넘는 긴 가슴지느러미(날치)도 몸 끝 폭으로
                t = min(1.0, max(0.0, (v.co.y - py) / max(0.01, ph * 0.5)))
                nm = 'Pec_R' if v.co.x > 0 else 'Pec_L'
                return {nm: t, 'Root': 1 - t}
        return w

    assign(fins, fin_extra)
    return arm


# =====================================================================
#  애니메이션 (몸 가운데 선의 진행파 → 마디 각도)
# =====================================================================

# 헤엄 방식: 몸 가운데 선이 그리는 물결 h(x, t) = A · a(x) · sin(2π x / λ − φ).
#   x = 주둥이 0 ~ 꼬리 끝 1, a(x) = 그 자리의 흔들림 비율 (꼬리 끝 1), λ = 파장 (몸길이 배수).
# 마디마다 사인파를 따로 돌리면(예전) 몸이 몇 토막으로 꺾이고 앞몸까지 통째로 흔들려 둔하게 보였다.
# 이제 곡선을 먼저 정하고 마디(뼈)는 그 곡선의 현을 따라가게 한다 → 뼈 수와 상관없이 같은 물결.
SWIM = {
    # 잉어·농어·도미: 머리는 반동으로 살짝, 앞 1/4은 거의 그대로, 뒤 1/3이 크게 (Videler 1993의 아지형 곡선)
    'carangi': (lambda x: 0.2 - 0.8 * x + 1.6 * x * x, 1.0),
    # 다랑어·고등어·삼치: 몸통은 단단하고 가는 꼬리자루와 초승달 꼬리만 친다
    'thunni': (lambda x: 0.1 + 0.9 * max(0.0, (x - 0.3) / 0.7) ** 2.4, 1.3),
    # 장어: 머리부터 꼬리까지 몸 전체에 물결이 한 번 반
    'anguilli': (lambda x: 0.3 + 0.7 * x ** 1.3, 0.65),
    # 메기·가물치·납작한 고기: 몸 뒤쪽 절반이 길게 물결
    'silur': (lambda x: 0.25 - 0.5 * x + 1.25 * x * x, 0.85),
}
STYLE = {'E': 'thunni', 'G': 'anguilli', 'H': 'silur', 'F': 'silur'}

CLIPS = {
    # 이름: (프레임 수, 물결 주기 수, 꼬리 끝 폭(몸길이), 몸 비틀기(rad), 가슴지느러미 폭, C자 굽힘(몸길이), 턱, 아가미 뚜껑, 숨 횟수)
    'Swim_Slow': (50, 2, 0.05, 0.0, 0.35, 0.0, 0.06, 0.09, 1),
    'Swim_Fast': (30, 3, 0.085, 0.0, 0.2, 0.0, 0.1, 0.11, 1),
    'Thrash': (36, 3, 0.15, 0.22, 0.6, 0.05, 0.18, 0.17, 2),
    'Flop': (40, 2, 0.03, 0.0, 0.5, 0.13, 0.24, 0.22, 2),
    'Idle_Hover': (60, 1, 0.012, 0.0, 0.5, 0.0, 0.07, 0.1, 1),
}


def animate(d, arm):
    nb = d['bones']
    amp_k = d['anim'].get('amp', 1.0)
    freq_k = d['anim'].get('freq', 1.0)
    axis = 0 if d.get('flat') else 2  # 납작한 고기는 위아래로 물결친다
    # 뼈 회전 → 옆으로 흔들리는 방향: Z축으로 돌면 꼬리 쪽이 -X로, X축으로 돌면 +Z로 간다
    sgn = -1.0 if axis == 2 else 1.0
    chain = ['Head'] + [f'Spine{i + 1}' for i in range(nb - 1)] + ['Tail']
    amp_fn, lam = SWIM[STYLE.get(d['type'], 'carangi')]
    # 마디 경계 x: 주둥이 · Root(= Spine1 시작) · 척추 마디 · 꼬리지느러미 시작 · 꼬리 끝
    rest = {b.name: b for b in arm.data.bones}
    xs = [0.0, rest['Spine1'].head_local.y + 0.5]
    xs += [rest[f'Spine{i + 1}'].tail_local.y + 0.5 for i in range(nb - 1)]
    xs.append(1.0)
    arm.animation_data_create()
    sc = bpy.context.scene
    sc.render.fps = FPS
    for clip, (frames, cycles, amp, roll, pec, cbend, jaw_a, gill_a, breaths) in CLIPS.items():
        lam_c, amp_c = lam, amp_fn
        if clip == 'Flop' and d['type'] == 'G':
            # 장어는 물 밖에서 C자로 펄떡이지 않고 S자로 몸을 꼰다
            amp, cbend, lam_c = 0.11, 0.03, 0.7
        if clip == 'Thrash':
            # 몸부림: 어느 체형이든 몸 전체가 크게 휜다 (파장도 짧게)
            lam_c, amp_c = min(lam, 0.9), (lambda x, f=amp_fn: 0.5 * f(x) + 0.5 * (0.3 + 0.7 * x))
        A, Cb = amp * amp_k, cbend * amp_k

        def h(x, ph):
            return (A * amp_c(x) * math.sin(2 * math.pi * x / lam_c - ph)
                    + Cb * (4 * (x - 0.45) ** 2 - 0.3) * math.sin(ph))

        # 어종 빈도 배율은 클립 길이로 (주기 수는 그대로라 반복이 이어진다)
        frames = max(12, int(round(frames / freq_k)))
        act = bpy.data.actions.new(clip)
        act.use_fake_user = True
        arm.animation_data.action = act
        for pb in arm.pose.bones:
            pb.rotation_mode = 'XYZ'
        for fr in range(frames + 1):
            ph = 2 * math.pi * cycles * fr / frames
            # 마디마다 현의 기울기. 꼬리지느러미는 물을 밀며 조금 늦게 따라온다 (끝이 휘는 느낌)
            th = []
            for j in range(len(xs) - 1):
                p = ph - (0.45 if j == len(xs) - 2 else 0.0)
                th.append(math.atan2(h(xs[j + 1], p) - h(xs[j], p), xs[j + 1] - xs[j]))
            for i, name in enumerate(chain):
                pb = arm.pose.bones.get(name)
                if pb is None:
                    continue
                e = [0.0, 0.0, 0.0]
                if name != 'Head':   # 머리 방향은 Root가 맡는다 (머리 마디의 현)
                    e[axis] = sgn * (th[i] - th[i - 1])
                pb.rotation_euler = e
                pb.keyframe_insert('rotation_euler', frame=fr)
            r = arm.pose.bones['Root']
            re = [0.0, 0.0, 0.0]
            re[axis] = sgn * th[0]
            if roll:
                re[1] = roll * math.sin(ph / cycles + 0.3)  # 몸을 비튼다 (몸길이 축, 클립에 한 번)
            if clip == 'Flop':
                re[0 if axis == 2 else 2] = 0.18 * math.sin(ph * 2)  # 펄떡: 위아래로 들썩
            r.rotation_euler = re
            r.keyframe_insert('rotation_euler', frame=fr)
            # 몸 가운데(Root 자리)도 물결만큼 옆으로 밀린다 (머리 반동)
            loc = [0.0, 0.0, 0.0]
            loc[0 if axis == 2 else 2] = h(xs[1], ph)
            r.location = loc
            r.keyframe_insert('location', frame=fr)
            # 숨: 입을 벌렸다 닫고, 조금 늦게 아가미 뚜껑이 열린다 (입 → 아가미로 물을 보내는 순서)
            br = 2 * math.pi * breaths * fr / frames
            jb = arm.pose.bones.get('Jaw')
            if jb is not None:
                jb.rotation_euler = (jaw_a * ((1 - math.cos(br)) / 2) ** 1.5, 0.0, 0.0)
                jb.keyframe_insert('rotation_euler', frame=fr)
            for nm, side in (('Gill_R', -1.0), ('Gill_L', 1.0)):
                gb = arm.pose.bones.get(nm)
                if gb is not None:
                    gb.rotation_euler = (0.0, 0.0, side * gill_a * ((1 - math.cos(br - 1.8)) / 2) ** 1.5)
                    gb.keyframe_insert('rotation_euler', frame=fr)
            # 두족류 다리: 다리마다 위상을 달리해 꿈틀 (빠르게 헤엄칠 땐 뒤로 모아 쭉)
            arm_amp = {'Swim_Slow': 0.22, 'Swim_Fast': 0.08, 'Thrash': 0.6, 'Flop': 0.45, 'Idle_Hover': 0.15}[clip]
            for pb in arm.pose.bones:
                if not pb.name.startswith('Arm'):
                    continue
                ai, aj = (int(x) for x in pb.name[3:].split('_'))
                w = ph * (4 / 3 if clip == 'Thrash' else 1.0) + ai * 0.785 + aj * 0.9
                pb.rotation_euler = (arm_amp * math.sin(w), 0.0, arm_amp * 0.5 * math.cos(w * 0.7 + ai))
                pb.keyframe_insert('rotation_euler', frame=fr)
            for nm, ps in (('Pec_R', 1), ('Pec_L', -1)):
                pb = arm.pose.bones.get(nm)
                if pb is None:
                    continue
                pb.rotation_euler = (pec * math.sin(ph * (2 if clip == 'Idle_Hover' else 1) + 0.5), 0.0, ps * pec * 0.4 * math.sin(ph))
                pb.keyframe_insert('rotation_euler', frame=fr)
        for fc in act.fcurves if hasattr(act, 'fcurves') else []:
            for kp in fc.keyframe_points:
                kp.interpolation = 'LINEAR'
    arm.animation_data.action = bpy.data.actions['Swim_Slow']


# =====================================================================
#  만들기
# =====================================================================

def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def build(sid):
    d = FS.resolve(sid)
    clear()
    B = Body(d)
    if d.get('ceph'):
        return build_cephalopod(d, B)
    v, f, uv = build_body_mesh(d, B)
    body = mesh_from('Body', v, f, uv)
    extras = [make_eye(d, B, 1), make_eye(d, B, -1)] + make_barbels(d, B)
    ex = bm_to_obj('Extras', extras)
    body = join([body, ex], 'Body')
    outward_normals(body)
    fins = []
    for name, fv, ff, fu, kind in make_fins(d, B):
        fins.append(mesh_from(name, fv, ff, fu))
    fins = join(fins, 'Fins')
    # 상어 지느러미는 살갗에 덮인 두툼한 판 (얇은 막이 아니다)
    solidify(fins, 0.007 if d.get('shark') else 0.0028 if d['type'] != 'G' else 0.0018)
    for p in fins.data.polygons:
        p.use_smooth = True
    if d.get('flat'):
        ang = math.radians(90 if d['flat'] == 'left' else -90)
        rot = mathutils.Matrix.Rotation(ang, 4, 'Y')
        for ob in (body, fins):
            ob.data.transform(rot)
            ob.data.update()
    arm = build_rig(d, B, body, fins)
    # LOD1 (수족관에 여러 마리): 몸·지느러미를 줄인 복사본
    lods = []
    for ob, ratio in ((body, 0.14), (fins, 0.35)):
        c = ob.copy()
        c.data = ob.data.copy()
        c.name = ob.name + '_LOD1'
        c.data.name = c.name
        bpy.context.scene.collection.objects.link(c)
        dm = c.modifiers.new('Dec', 'DECIMATE')
        dm.ratio = ratio
        bpy.context.view_layer.objects.active = c
        c.modifiers.move(len(c.modifiers) - 1, 0)
        bpy.ops.object.modifier_apply(modifier=dm.name)
        lods.append(c)
    animate(d, arm)
    tris = sum(len(p.vertices) - 2 for ob in (body, fins) for p in ob.data.polygons)
    return d, arm, body, fins, lods, tris


def make_lods(body, fins):
    lods = []
    for ob, ratio in ((body, 0.14), (fins, 0.35)):
        c = ob.copy()
        c.data = ob.data.copy()
        c.name = ob.name + '_LOD1'
        c.data.name = c.name
        bpy.context.scene.collection.objects.link(c)
        dm = c.modifiers.new('Dec', 'DECIMATE')
        dm.ratio = ratio
        bpy.context.view_layer.objects.active = c
        c.modifiers.move(len(c.modifiers) - 1, 0)
        bpy.ops.object.modifier_apply(modifier=dm.name)
        lods.append(c)
    return lods


def build_cephalopod(d, B):
    """주꾸미·갑오징어 (cephalopod.py): 몸통 사슬 + 다리 뼈."""
    body, fins, paths, arm_idx = CP.build(d)
    outward_normals(body)
    solidify(fins, 0.002)
    arm = build_rig(d, B, body, fins)
    CP.add_arm_bones(arm, paths)
    CP.weight_arms(body, paths, arm_idx)
    lods = make_lods(body, fins)
    animate(d, arm)
    tris = sum(len(p.vertices) - 2 for ob in (body, fins) for p in ob.data.polygons)
    return d, arm, body, fins, lods, tris


def export(d, arm, objs):
    os.makedirs(OUT_FBX, exist_ok=True)
    out = os.path.join(OUT_FBX, d['id'] + '.fbx')
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             axis_forward='-Y', axis_up='Z', apply_scale_options='FBX_SCALE_UNITS',
                             add_leaf_bones=False, use_armature_deform_only=True,
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, bake_anim_step=1.0,
                             mesh_smooth_type='FACE', use_mesh_modifiers=False)
    return out


def write_textures(d):
    os.makedirs(OUT_TEX, exist_ok=True)
    alb, nrm, mt = fish_tex.paint(d)
    base = os.path.join(OUT_TEX, d['id'])
    alb.save(base + '.png')
    nrm.save(base + '_n.png')
    mt.save(base + '_m.png')
    return base + '.png'


# =====================================================================
#  렌더 (미리보기, 아이콘, 한 장 모음)
# =====================================================================

def apply_preview_materials(d, body, fins, tex_path):
    """Unity Fishing/Fish 셰이더와 같은 입력: 알베도(A = 지느러미 잘라내기), 노멀, 금속성(R)·매끈함(A)."""
    img = bpy.data.images.load(tex_path)
    nimg = bpy.data.images.load(tex_path.replace('.png', '_n.png'))
    nimg.colorspace_settings.name = 'Non-Color'
    mimg = bpy.data.images.load(tex_path.replace('.png', '_m.png'))
    mimg.colorspace_settings.name = 'Non-Color'
    mimg.alpha_mode = 'CHANNEL_PACKED'
    for ob, cut in ((body, False), (fins, True)):
        m = bpy.data.materials.new(ob.name + 'Mat')
        m.use_nodes = True
        nt = m.node_tree
        bsdf = nt.nodes['Principled BSDF']
        t = nt.nodes.new('ShaderNodeTexImage')
        t.image = img
        nt.links.new(t.outputs['Color'], bsdf.inputs['Base Color'])
        if cut:
            # Unity Fishing/FishFin: 알파만큼 반투명한 막 (얇은 막이라 뒤에서 빛이 비친다)
            nt.links.new(t.outputs['Alpha'], bsdf.inputs['Alpha'])
            bsdf.inputs['Transmission Weight'].default_value = 0.25
        else:
            # Unity Fishing/Fish의 점액 막(_Coat): 비늘 위 매끈한 막의 좁은 하이라이트·가장자리 반사
            bsdf.inputs['Coat Weight'].default_value = 0.5
            bsdf.inputs['Coat Roughness'].default_value = 0.08
        nm = nt.nodes.new('ShaderNodeNormalMap')
        nt2 = nt.nodes.new('ShaderNodeTexImage')
        nt2.image = nimg
        nt.links.new(nt2.outputs['Color'], nm.inputs['Color'])
        nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
        mt = nt.nodes.new('ShaderNodeTexImage')
        mt.image = mimg
        sep = nt.nodes.new('ShaderNodeSeparateColor')
        nt.links.new(mt.outputs['Color'], sep.inputs['Color'])
        nt.links.new(sep.outputs['Red'], bsdf.inputs['Metallic'])
        inv = nt.nodes.new('ShaderNodeMath')
        inv.operation = 'SUBTRACT'
        inv.inputs[0].default_value = 1.0
        nt.links.new(mt.outputs['Alpha'], inv.inputs[1])
        nt.links.new(inv.outputs[0], bsdf.inputs['Roughness'])
        ob.data.materials.clear()
        ob.data.materials.append(m)


def render_setup(res, transparent, samples=24):
    sc = bpy.context.scene
    for nm in ('Key', 'Rim', 'Cam'):
        if nm in bpy.data.objects:
            bpy.data.objects.remove(bpy.data.objects[nm])
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    # CPU를 다 먹지 않게 렌더 스레드를 묶는다: 미리보기는 여럿이 함께 돌리므로 코어 1/6, 전체 빌드는 3/4 (FISH_THREADS로 바꾼다)
    cores = os.cpu_count() or 4
    sc.render.threads_mode = 'FIXED'
    sc.render.threads = int(os.environ.get('FISH_THREADS', 0)) or max(1, cores // 6 if '--preview' in ARGS else cores * 3 // 4)
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True   # 지느러미(반투명)·코팅 반사가 적은 샘플에서 모래알처럼 자글거렸다
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = transparent
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('W')
    sc.world = w
    w.use_nodes = True
    # 하늘빛 그러데이션 (위 밝고 아래 어둡다): 젖은 몸·은빛 비늘에 비치는 주변 빛. 단색이면 반사가 밋밋했다
    nt = w.node_tree
    bg = nt.nodes['Background']
    tc = nt.nodes.new('ShaderNodeTexCoord')
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    nt.links.new(tc.outputs['Generated'], sep.inputs[0])
    # 월드의 Generated 좌표는 보는 방향(-1~1)이라 z를 0~1로 옮긴다: 아래 어두운 물빛 · 수평선 · 위 밝은 하늘
    mr = nt.nodes.new('ShaderNodeMapRange')
    mr.inputs['From Min'].default_value = -1.0
    nt.links.new(sep.outputs['Z'], mr.inputs['Value'])
    nt.links.new(mr.outputs['Result'], ramp.inputs['Fac'])
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = 0.3, (0.05, 0.07, 0.08, 1)
    els[1].position, els[1].color = 0.92, (1.0, 1.0, 1.0, 1)
    mid = els.new(0.52)
    mid.color = (0.36, 0.44, 0.5, 1)
    nt.links.new(ramp.outputs['Color'], bg.inputs['Color'])
    bg.inputs['Strength'].default_value = 1.0
    key = bpy.data.objects.new('Key', bpy.data.lights.new('Key', 'SUN'))
    key.data.energy = 3.2
    key.rotation_euler = (math.radians(40), math.radians(-25), math.radians(-30))
    sc.collection.objects.link(key)
    rim = bpy.data.objects.new('Rim', bpy.data.lights.new('Rim', 'SUN'))
    rim.data.energy = 1.6
    rim.rotation_euler = (math.radians(-60), math.radians(30), math.radians(160))
    sc.collection.objects.link(rim)
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def look(cam, pos, target=(0, 0, 0), ortho=None):
    cam.location = pos
    dvec = mathutils.Vector(target) - mathutils.Vector(pos)
    cam.rotation_euler = dvec.to_track_quat('-Z', 'Y').to_euler()
    if ortho:
        cam.data.type = 'ORTHO'
        cam.data.ortho_scale = ortho
    else:
        cam.data.type = 'PERSP'
        cam.data.lens = 60


def pose_frame(arm, clip, frame):
    arm.animation_data.action = bpy.data.actions[clip]
    bpy.context.scene.frame_set(frame)


def side_dir(d):
    """카메라가 보는 옆면 (+x). 납작한 고기는 위(+z)에서 본다."""
    if d.get('flat'):
        return (0, 0, 1)
    return (1, 0, 0)


def render_icon(d, arm, body, fins, out, big=None):
    """도감 아이콘 256×128. 두 배로 렌더해 줄인다 (가장자리·지느러미 결이 깨끗하다). big: 큰 그림도 남길 경로 (모음 시트용)."""
    from PIL import Image
    cam = render_setup((512, 256), True, samples=24)
    pose_frame(arm, 'Idle_Hover', 0)
    sd = side_dir(d)
    look(cam, (sd[0] * 3, sd[1], sd[2] * 3), (0, 0, 0), ortho=1.06)
    if d.get('flat'):
        cam.rotation_euler = (0, 0, math.radians(-90))
    bpy.context.scene.render.filepath = out
    bpy.ops.render.render(write_still=True)
    im = Image.open(out)
    if big:
        im.save(big)
    im.resize((256, 128), Image.LANCZOS).save(out)


def render_preview(d, arm, out_dir):
    cam = render_setup((900, 520), False, samples=32)
    os.makedirs(out_dir, exist_ok=True)
    # front_q: 도감 미리보기가 돌다가 앞쪽에서 비스듬히 볼 때, head: 머리 근접 (눈이 반대편으로 비치지 않는지)
    views = {'side': ((2.2, 0, 0.05), (0, 0, 0)), 'top': ((0.001, 0, 2.4), (0, 0, 0)), 'three_q': ((1.5, -1.5, 0.7), (0, 0, 0)),
             'front_q': ((1.6, -1.6, 0.25), (0, 0, 0)), 'head': ((0.55, -0.75, 0.12), (0, -0.38, 0.02))}
    pose_frame(arm, 'Swim_Slow', 12)
    for name, (pos, target) in views.items():
        look(cam, pos, target)
        bpy.context.scene.render.filepath = os.path.join(out_dir, f'{d["id"]}_{name}.png')
        bpy.ops.render.render(write_still=True)


def render_anim_strip(d, arm, out_dir):
    """클립마다 위에서 본 모습 4프레임 (몸이 제대로 휘는지 확인)."""
    from PIL import Image
    cam = render_setup((300, 300), False, samples=12)
    os.makedirs(out_dir, exist_ok=True)
    tiles = []
    for clip in CLIPS:
        act = bpy.data.actions[clip]
        n = int(act.frame_range[1])
        for k in range(4):
            pose_frame(arm, clip, int(n * k / 4))
            if d.get('flat'):
                look(cam, (2.2, 0.0, 0.3), (0, 0, 0))
            else:
                look(cam, (0.001, 0.0, 2.2), (0, 0, 0))
            p = os.path.join(out_dir, f'_{clip}_{k}.png')
            bpy.context.scene.render.filepath = p
            bpy.ops.render.render(write_still=True)
            tiles.append(p)
    sheet = Image.new('RGB', (1200, 300 * len(CLIPS)))
    for i, p in enumerate(tiles):
        sheet.paste(Image.open(p), ((i % 4) * 300, (i // 4) * 300))
        os.remove(p)
    sheet.save(os.path.join(out_dir, f'{d["id"]}_anim.png'))


def contact_sheet(paths, out, cols=6, cw=320, ch=196):
    """모음 한 장 (docs/images). 칸마다 그림 밑에 한글 이름과 Id.
    예전에는 이름이 그림 위에 붙어서, 위 칸 그림 바로 밑의 다음 줄 이름을 그 그림 이름으로 읽기 쉬웠다 (무늬오징어 밑에 폭군삼치).
    칸마다 옅은 판을 깔아 경계를 보이게 한다."""
    from PIL import Image, ImageDraw, ImageFont
    rows = (len(paths) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cw, rows * ch), (26, 34, 42, 255))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 17)
        small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 12)
    except OSError:
        font = small = ImageFont.load_default()
    for i, (sid, p) in enumerate(paths):
        x, y = (i % cols) * cw, (i // cols) * ch
        draw.rounded_rectangle((x + 4, y + 4, x + cw - 4, y + ch - 4), 8, fill=(38, 50, 60, 255))
        im = Image.open(p).convert('RGBA').resize((cw - 16, (cw - 16) // 2), Image.LANCZOS)
        sheet.alpha_composite(im, (x + 8, y + 8))
        name = FS.SPECIES[sid].get('name', sid) if sid in FS.SPECIES else sid
        draw.text((x + 12, y + ch - 40), name, font=font, fill=(235, 240, 244, 255))
        draw.text((x + 12, y + ch - 19), sid, font=small, fill=(140, 160, 172, 255))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.convert('RGB').save(out, optimize=True)


def main():
    only = arg('--only')
    ids = only.split(',') if only else list(FS.SPECIES.keys())
    preview = '--preview' in ARGS
    icons = '--no-icons' not in ARGS
    sheet = arg('--sheet')
    out_dir = os.path.abspath(arg('--out', os.path.join(ROOT, 'Screenshots', 'fish')))
    done = []
    if sheet and '--sheet-only' in ARGS:
        done = [(sid, os.path.join(out_dir, 'sheet', sid + '.png')) for sid in ids]
        done = [x for x in done if os.path.exists(x[1])]
        ids = []
    for sid in ids:
        d, arm, body, fins, lods, tris = build(sid)
        tex = write_textures(d)
        if not preview:
            path = export(d, arm, [body, fins] + lods)
            print(f'[fish] {sid}: {tris} tris → {os.path.relpath(path, ROOT)}', flush=True)
        apply_preview_materials(d, body, fins, tex)
        for c in lods:
            c.hide_render = True
        if preview:
            render_preview(d, arm, out_dir)
            if '--anim' in ARGS:
                render_anim_strip(d, arm, out_dir)
            print(f'[fish] preview {sid}', flush=True)
        if icons or sheet:
            os.makedirs(OUT_ICON, exist_ok=True)
            ip = os.path.join(OUT_ICON, sid + '.png')
            big = None
            if sheet:
                os.makedirs(os.path.join(out_dir, 'sheet'), exist_ok=True)
                big = os.path.join(out_dir, 'sheet', sid + '.png')
            render_icon(d, arm, body, fins, ip, big)
            done.append((sid, big or ip))
    if sheet:
        contact_sheet(done, os.path.join(ROOT, sheet) if not os.path.isabs(sheet) else sheet)
        print('[fish] sheet', sheet, flush=True)


if __name__ == '__main__':
    main()
    sys.stdout.flush()
    os._exit(0)
