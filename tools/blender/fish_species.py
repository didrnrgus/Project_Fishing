"""
어종별 설계 시트 (docs/09-art-plan.md 3-3, 부록) → 수치.

모든 길이는 몸 전체 길이(주둥이 끝 ~ 꼬리지느러미 끝) = 1 기준이다.
  depth   : 가장 높은 곳의 몸 높이 / 길이        width : 몸 폭 / 길이
  tmax    : 몸이 가장 높은 위치 (몸통 구간 0~1)   body_end : 몸통이 끝나고 꼬리지느러미가 시작하는 위치 (0~1)
  ped     : 꼬리자루 높이 / 최대 높이              arch  : 위쪽 비율 (0.5 = 위아래 대칭, 클수록 등이 솟음)
  snout   : 머리 둥근 정도 (작을수록 뭉툭)          mouth : 주둥이 높이 (-1 아래 ~ +1 위, 0 가운데)
  xsec    : 단면 지수 (2 = 타원, 작을수록 위아래가 뾰족한 납작한 몸)
  fins    : 지느러미. kind = dorsal/anal/caudal/pectoral/pelvic/adipose, s = 몸통 구간(시작, 끝), h = 높이(최대 몸높이 배율),
            prof = 모양(제어점), spiny = 가시 지느러미(막이 오목하게 파임), group = 텍스처 칸(up/low)
  eye     : s(위치), z(머리 높이 비율 -1~1), r(반지름/길이), iris(홍채 색), pupil(동공 크기 배율, 1 = 보이는 눈의 절반), bulge(겉으로 나온 정도, 0.18 · 다랑어류 E 0.1)
  barbels : (s, z, 길이, 굵기, 방향) 수염
  colors  : back/side/belly, 무늬 patterns, fin_up/fin_low (지느러미 색: 밑동, 끝, 알파)
  anim    : amp(꼬리 흔드는 폭 배율), freq(빈도 배율) — Unity에서 재생 속도로 쓴다

머리·몸 모양을 더 고르는 값 (build_fish.Body · fish_tex):
  blunt(주둥이 끝 뾰족함, 0.8 기본) · mouth_h(주둥이 끝 단면이 남는 비율 = 입 높이, 0.17) · keel(등배 능선) · hump·hump_s(이마 혹)
  head_w(머리 폭 배율) · head_flat(머리를 위아래로 누름, 아귀) · hold(몸통 굵기 유지, 장어) · tail_w(꼬리자루 폭 비율)
  lips(입술 두께 배율, 잉어 2.2) · mouth_slope(입 선이 뒤로 오르는 기울기, 0.6) · gape·gape_w·gape_d(입 길이·홈 폭·깊이 배율) · jaw(아래턱 돌출) · teeth
  maxilla(위턱뼈 판 크기 배율: 입 선 위로 입꼬리까지 뻗고 뒤 끝이 둥근 뼈판, 0 = 없음. 잉어·납작이·장어·메기 틀은 기본 0, 나머지 1)
  hump 음수 = 오목한 목덜미
  tail_lobe(꼬리지느러미 밑동을 덮는 비늘 살 길이, 0.04) · 꼬리지느러미 procurrent(위아래 바깥 줄기가 꼬리자루 가장자리를 따라 앞에서 시작하는 길이, 0.075)
  beak(학꽁치 아래턱) · bill(황새치 칼) · barbels 방향 'down'·'side'·'up'·'lure'(머리 위 낚싯대 하나)·'flap'(피부 술)·'chin'(아래턱 끝 가운데 하나, 대구)·'fwd'(앞으로 뻣뻣하게, 아로와나)
  shark=dict(mouth, gills, rostrum): 상어 — 배 쪽 초승달 입의 s, 아가미구멍 다섯 줄 s 범위, 코끝을 올리는 정도
  ray=dict(eye_z) + disc: 가오리·홍어 — 두 눈을 등 가운데 좌우로, 원반이 끝나는 s (뒤는 가늘어지는 꼬리)
"""

# ---------------- 틀 (docs/09-art-plan.md 3-2) ----------------

def _fin(kind, s=(0, 0), h=0.5, prof=None, spiny=False, group=None, **kw):
    d = dict(kind=kind, s=s, h=h, prof=prof, spiny=spiny, group=group or ('up' if kind in ('dorsal', 'caudal', 'adipose') else 'low'))
    d.update(kw)
    return d


TEMPLATES = {
    # A. 잉어형: 두툼한 몸, 둥근 등, 작은 입, 꼬리 갈라짐
    'A': dict(depth=0.27, width=0.14, tmax=0.4, body_end=0.8, ped=0.42, arch=0.6, snout=0.6, mouth=-0.1, xsec=2.0, scales=36,
              fins=[_fin('dorsal', (0.36, 0.72), 0.5, prof=[(0, 0.35), (0.12, 1.0), (0.4, 0.55), (1, 0.4)]),
                    _fin('anal', (0.72, 0.82), 0.48, prof=[(0, 0.5), (0.2, 1.0), (1, 0.45)]),
                    _fin('caudal', h=0.24, shape='fork'),
                    _fin('pectoral', s=(0.24, 0.24), h=0.13),
                    _fin('pelvic', s=(0.5, 0.5), h=0.1)],
              eye=dict(s=0.1, z=0.25, r=0.018), anim=dict(amp=0.9, freq=0.85)),
    # B. 날씬한 잉어형: 길고 납작한 몸, 큰 비늘, 은색
    'B': dict(depth=0.2, width=0.1, tmax=0.42, body_end=0.79, ped=0.38, arch=0.55, snout=0.7, mouth=0.05, xsec=1.9, scales=46,
              fins=[_fin('dorsal', (0.47, 0.58), 0.62, prof=[(0, 0.6), (0.15, 1.0), (1, 0.35)]),
                    _fin('anal', (0.66, 0.8), 0.45, prof=[(0, 0.7), (0.2, 1.0), (1, 0.4)]),
                    _fin('caudal', h=0.26, shape='deepfork'),
                    _fin('pectoral', s=(0.22, 0.22), h=0.12),
                    _fin('pelvic', s=(0.5, 0.5), h=0.09)],
              eye=dict(s=0.1, z=0.2, r=0.02), anim=dict(amp=1.0, freq=1.2)),
    # C. 농어형: 가시 있는 등지느러미, 큰 입
    'C': dict(depth=0.27, width=0.14, tmax=0.36, body_end=0.82, ped=0.4, arch=0.55, snout=0.72, mouth=0.15, xsec=2.0, scales=60,
              fins=[_fin('dorsal', (0.27, 0.55), 0.45, prof=[(0, 0.35), (0.25, 1.0), (0.85, 0.55), (1, 0.4)], spiny=True),
                    _fin('dorsal', (0.55, 0.78), 0.5, prof=[(0, 0.75), (0.3, 1.0), (1, 0.4)]),
                    _fin('anal', (0.6, 0.78), 0.45, prof=[(0, 0.5), (0.3, 1.0), (1, 0.45)]),
                    _fin('caudal', h=0.24, shape='truncate'),
                    _fin('pectoral', s=(0.26, 0.26), h=0.14),
                    _fin('pelvic', s=(0.3, 0.3), h=0.12)],
              eye=dict(s=0.12, z=0.35, r=0.02), anim=dict(amp=1.0, freq=1.0)),
    # D. 도미형: 높고 옆으로 납작한 몸
    'D': dict(depth=0.4, width=0.12, tmax=0.4, body_end=0.8, ped=0.3, arch=0.62, snout=0.5, mouth=-0.25, xsec=1.6, scales=50,
              fins=[_fin('dorsal', (0.3, 0.62), 0.38, prof=[(0, 0.4), (0.18, 1.0), (1, 0.6)], spiny=True),
                    _fin('dorsal', (0.62, 0.8), 0.32, prof=[(0, 0.75), (1, 0.55)]),
                    _fin('anal', (0.62, 0.8), 0.32, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                    _fin('caudal', h=0.26, shape='fork'),
                    _fin('pectoral', s=(0.3, 0.3), h=0.2, long=True),
                    _fin('pelvic', s=(0.35, 0.35), h=0.12)],
              eye=dict(s=0.15, z=0.4, r=0.022), anim=dict(amp=0.8, freq=1.0)),
    # E. 유선형: 방추형 몸, 작은 꼬리자루, 갈라진 꼬리
    'E': dict(depth=0.2, width=0.13, tmax=0.4, body_end=0.79, ped=0.2, arch=0.52, snout=0.75, mouth=0.05, xsec=2.2, scales=0,
              fins=[_fin('dorsal', (0.3, 0.45), 0.35, prof=[(0, 1.0), (1, 0.25)], spiny=True),
                    _fin('dorsal', (0.56, 0.64), 0.32, prof=[(0, 1.0), (1, 0.35)]),
                    _fin('anal', (0.6, 0.68), 0.28, prof=[(0, 1.0), (1, 0.35)]),
                    _fin('caudal', h=0.3, shape='deepfork'),
                    _fin('pectoral', s=(0.25, 0.25), h=0.12),
                    _fin('pelvic', s=(0.33, 0.33), h=0.07)],
              eye=dict(s=0.1, z=0.25, r=0.022), anim=dict(amp=1.1, freq=1.4)),
    # F. 납작한 고기: 두 눈이 한쪽에. 옆으로 누운 몸 (side = 'left' 광어 / 'right' 가자미)
    'F': dict(depth=0.42, width=0.07, tmax=0.45, body_end=0.85, ped=0.25, arch=0.5, snout=0.55, mouth=0.0, xsec=1.5, scales=0,
              fins=[_fin('dorsal', (0.08, 0.94), 0.2, prof=[(0, 0.4), (0.5, 1.0), (1, 0.4)]),
                    _fin('anal', (0.3, 0.94), 0.2, prof=[(0, 0.4), (0.5, 1.0), (1, 0.4)]),
                    _fin('caudal', h=0.18, shape='round'),
                    _fin('pectoral', s=(0.24, 0.24), h=0.08),
                    _fin('pelvic', s=(0.26, 0.26), h=0.05)],
              eye=dict(s=0.12, z=0.3, r=0.02), flat='left', anim=dict(amp=0.7, freq=0.9)),
    # G. 장어형: 아주 길고 지느러미가 몸을 따라 이어진다
    # 머리는 둥글고(blunt 0.55) 몸통은 거의 같은 굵기(hold)로 가다가 꼬리가 옆으로 납작하게 가늘어진다(ped·tail_w)
    'G': dict(depth=0.07, width=0.065, tmax=0.2, hold=0.35, body_end=0.97, ped=0.12, tail_w=0.14, arch=0.5, snout=0.7, mouth=0.0, xsec=2.2, scales=0,
              fins=[_fin('dorsal', (0.32, 1.0), 0.36, prof=[(0, 0.15), (0.15, 0.75), (0.55, 1.0), (1, 0.9)]),
                    _fin('anal', (0.45, 1.0), 0.36, prof=[(0, 0.15), (0.15, 0.75), (0.55, 1.0), (1, 0.9)]),
                    _fin('caudal', h=0.05, shape='point'),
                    _fin('pectoral', s=(0.1, 0.1), h=0.05)],
              eye=dict(s=0.04, z=0.35, r=0.007), bones=14, anim=dict(amp=1.3, freq=0.8)),
    # H. 메기형: 넓적한 머리, 수염, 비늘 없음
    'H': dict(depth=0.17, width=0.16, head_w=1.25, tmax=0.28, body_end=0.86, ped=0.45, arch=0.55, snout=0.45, mouth=0.0, xsec=2.2, scales=0,
              fins=[_fin('dorsal', (0.3, 0.35), 0.28),
                    _fin('anal', (0.45, 0.98), 0.35, prof=[(0, 0.6), (0.3, 1.0), (1, 0.8)]),
                    _fin('caudal', h=0.14, shape='truncate'),
                    _fin('pectoral', s=(0.18, 0.18), h=0.11),
                    _fin('pelvic', s=(0.42, 0.42), h=0.07)],
              eye=dict(s=0.06, z=0.45, r=0.008), anim=dict(amp=0.9, freq=0.7)),
    # O. 문어형 두족류 (주꾸미): 외투막 + 다리 8개. 몸통 사슬은 짧게, 다리마다 뼈 3개 (cephalopod.py)
    'O': dict(depth=0.25, width=0.24, tmax=0.5, body_end=0.98, ped=0.5, arch=0.5, snout=0.5, mouth=0.0, xsec=2.0, scales=0,
              fins=[], eye=dict(s=0.5, z=0.3, r=0.03), bones=3, ceph=True, anim=dict(amp=0.3, freq=0.7)),
    # Q. 갑오징어형: 납작한 외투막 + 지느러미 치마 + 짧은 다리 8 + 촉완 2
    'Q': dict(depth=0.14, width=0.26, tmax=0.5, body_end=0.98, ped=0.5, arch=0.5, snout=0.5, mouth=0.0, xsec=2.0, scales=0,
              fins=[], eye=dict(s=0.35, z=0.3, r=0.032), bones=3, ceph=True, anim=dict(amp=0.25, freq=0.8)),
    # S. 오징어형: 길쭉한 원통 외투막 + 끝의 마름모 지느러미 + 짧은 다리 8 + 촉완 2
    'S': dict(depth=0.12, width=0.13, tmax=0.5, body_end=0.98, ped=0.5, arch=0.5, snout=0.5, mouth=0.0, xsec=2.0, scales=0,
              fins=[], eye=dict(s=0.3, z=0.3, r=0.026), bones=3, ceph=True, anim=dict(amp=0.3, freq=1.0)),
}


def P(back, side, belly, **kw):
    d = dict(back=back, side=side, belly=belly)
    d.update(kw)
    return d


# fin_up/fin_low = (밑동 색, 끝 색, 끝 알파)
SPECIES = {
    # ===== A. 잉어형 =====
    # 붕어: 잉어보다 높은 몸에 작고 뾰족한 머리, 수염 없음, 큰 눈. 목덜미에서 등이 가파르게 솟고 등지느러미가 길다.
    # 황동빛 옆구리에 큰 비늘 테가 또렷하고, 아래 지느러미는 반투명한 호박색
    'crucian': dict(type='A', name='붕어', depth=0.35, tmax=0.4, snout=0.85, blunt=1.0, mouth_h=0.2, lips=1.3, head_w=0.95, scales=28, mouth=0.02, gape=0.03,
                    fins=[_fin('dorsal', (0.36, 0.74), 0.42, prof=[(0, 0.5), (0.1, 1.0), (0.35, 0.62), (1, 0.45)]),
                          _fin('anal', (0.73, 0.83), 0.42, prof=[(0, 0.5), (0.2, 1.0), (1, 0.45)]),
                          _fin('caudal', h=0.27, shape='fork'),
                          _fin('pectoral', s=(0.24, 0.24), h=0.13),
                          _fin('pelvic', s=(0.48, 0.48), h=0.11)],
                    colors=P('#363c24', '#a08c4c', '#e2d6a4', silver=0.3, fin_up=('#4a4630', '#2e2c20', 0.8), fin_low=('#8a7448', '#b09868', 0.6),
                             patterns=[('scale_edge', 0.6)]),
                    eye=dict(s=0.09, z=0.25, r=0.024, iris='#c8a040')),
    # 잉어: 도톰한 입술(lips) · 입꼬리의 긴 수염 한 쌍과 윗입술의 짧은 수염 한 쌍
    'carp': dict(type='A', name='잉어', depth=0.27, scales=36, mouth=-0.08, lips=2.2,
                 barbels=[(0.035, -0.3, 0.032, 0.0035, 'down'), (0.02, -0.05, 0.016, 0.0026, 'side')],
                 colors=P('#3c4228', '#a9884a', '#e2d6a2', silver=0.15, fin_up=('#4c4630', '#3a3424', 0.85), fin_low=('#a8643a', '#c4502a', 0.85),
                          patterns=[('scale_edge', 0.75)]),
                 eye=dict(iris='#d8a838')),
    'goldkoi': dict(type='A', name='황금 비단잉어', depth=0.27, scales=36, mouth=-0.08, lips=2.2,
                    barbels=[(0.035, -0.3, 0.032, 0.0035, 'down'), (0.02, -0.05, 0.016, 0.0026, 'side')],
                    colors=P('#e0901a', '#f6b832', '#ffe08a', silver=0.6, metallic=0.7, fin_up=('#f0a830', '#ffe4a0', 0.75), fin_low=('#f0a830', '#ffe8b0', 0.75),
                             patterns=[('scale_edge', 0.4), ('sheen', 0.6)]),
                    eye=dict(iris='#e0cc88')),
    # 누치: 길쭉한 몸에 길고 뾰족한 원뿔 주둥이, 입은 주둥이 밑에 있고 입꼬리에 짧은 수염 한 쌍.
    # 등지느러미는 굵은 가시로 높이 솟고, 은빛 도는 황갈색 몸에 옆줄 위로 희미한 점이 한 줄 (어릴 때 진하다)
    'nuchi': dict(type='A', name='누치', depth=0.2, width=0.11, tmax=0.4, mouth=-0.45, snout=0.95, blunt=1.05, scales=48, gape=0.03,
                  fins=[_fin('dorsal', (0.38, 0.5), 0.68, prof=[(0, 0.55), (0.12, 1.0), (1, 0.3)], spine=True),
                        _fin('anal', (0.7, 0.8), 0.45), _fin('caudal', h=0.24, shape='deepfork'),
                        _fin('pectoral', s=(0.22, 0.22), h=0.13), _fin('pelvic', s=(0.5, 0.5), h=0.1)],
                  barbels=[(0.035, -0.5, 0.028, 0.003, 'down')],
                  colors=P('#5a5a4c', '#b8b2a0', '#eeebe2', silver=0.55, fin_up=('#7a786c', '#9a988c', 0.7), fin_low=('#c0a888', '#d8c0a0', 0.65),
                           patterns=[('spots', dict(n=9, r=(5, 7), color='#4a4a40', v=(0.6, 0.6), u=(0.25, 0.85), row=True, a=0.45))]),
                  eye=dict(s=0.12, z=0.35, r=0.02, iris='#d8c8a0')),
    # ===== B. 날씬한 잉어형 =====
    # 피라미 (혼인색 수컷): 청록색 굵은 세로 띠 10줄 사이로 분홍·주황빛, 뺨이 붉고 뒷지느러미가 크게 늘어진다.
    # 은빛 몸, 작은 입, 꼬리는 깊게 갈라진다
    'zacco': dict(type='B', name='피라미', depth=0.22, snout=0.75, gape=0.035,
                  fins=[_fin('dorsal', (0.47, 0.58), 0.6), _fin('anal', (0.64, 0.8), 0.78, prof=[(0, 0.85), (0.3, 1.0), (1, 0.6)]),
                        _fin('caudal', h=0.26, shape='deepfork'), _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.5, 0.5), h=0.1)],
                  colors=P('#4a6a6c', '#b8c8c4', '#f0f0ea', silver=0.7, fin_up=('#8aa098', '#b8c8c0', 0.6), fin_low=('#e09a60', '#e87a58', 0.8),
                           patterns=[('bands', dict(n=10, u=(0.3, 0.88), w=0.04, color='#2a7a90', v=(0.15, 0.85), a=0.7)),
                                     ('bands', dict(n=10, u=(0.33, 0.91), w=0.022, color='#f0907a', v=(0.15, 0.7), a=0.45)),
                                     ('throat', '#e07860')]),
                  eye=dict(iris='#e8a060')),
    # 끄리: 잉어과인데 육식이라 입이 아주 크다 — 눈 밑까지 찢어진 입, 아래턱 끝이 위턱 홈에 맞물리며 살짝 나온다.
    # 길쭉한 은청색 몸, 큰 머리, 혼인기에 배·아래 지느러미가 주황빛
    'kkeuri': dict(type='B', name='끄리', depth=0.2, width=0.1, tmax=0.4, mouth=0.15, snout=0.85, blunt=0.95, gape=0.085, jaw=0.008, head_w=1.05,
                   fins=[_fin('dorsal', (0.46, 0.57), 0.6, prof=[(0, 0.6), (0.15, 1.0), (1, 0.35)]),
                         _fin('anal', (0.64, 0.8), 0.5, prof=[(0, 0.8), (0.25, 1.0), (1, 0.45)]),
                         _fin('caudal', h=0.26, shape='deepfork'),
                         _fin('pectoral', s=(0.22, 0.22), h=0.13),
                         _fin('pelvic', s=(0.48, 0.48), h=0.1)],
                   colors=P('#34505e', '#aab8bc', '#eef0ec', silver=0.75, fin_up=('#5a6a72', '#7a8a92', 0.7), fin_low=('#d88a5a', '#e8865a', 0.8),
                            patterns=[('stripe', dict(v=0.58, w=0.04, color='#4a6a8a', a=0.3))]),
                   eye=dict(s=0.11, z=0.35, r=0.019, iris='#d0c0a0')),
    # 강준치: 하늘을 보는 입 — 아래턱이 위턱보다 길게 나와 입이 거의 세로로 위를 향한다. 등선은 곧고 배가 불룩한
    # 옆으로 납작한 은빛 몸, 작은 머리, 길게 이어진 뒷지느러미, 꼬리·아래 지느러미는 붉은 기
    'skygager': dict(type='B', name='강준치', depth=0.23, width=0.08, tmax=0.45, mouth=0.75, snout=0.9, blunt=1.0, arch=0.4, scales=70,
                     mouth_slope=-1.2, gape=0.05, jaw=0.016,
                     fins=[_fin('dorsal', (0.5, 0.6), 0.55, prof=[(0, 0.6), (0.12, 1.0), (1, 0.3)], spine=True),
                           _fin('anal', (0.58, 0.82), 0.4, prof=[(0, 1.0), (0.25, 0.75), (1, 0.4)]),
                           _fin('caudal', h=0.27, shape='deepfork', group='low'), _fin('pectoral', s=(0.22, 0.22), h=0.14), _fin('pelvic', s=(0.46, 0.46), h=0.1)],
                     colors=P('#526470', '#c8d0d4', '#f4f4f2', silver=0.85, fin_up=('#8a9aa2', '#a8b8c0', 0.6), fin_low=('#c8a898', '#d88a70', 0.7)),
                     eye=dict(s=0.1, z=0.15, r=0.022, iris='#e0d090')),
    # 숭어: 위아래로 눌린 넓적하고 뭉툭한 머리, 작은 입, 둥근 통 같은 몸. 등지느러미 둘이 멀리 떨어져 있다
    # (앞은 가시 4개 삼각 돛, 뒤는 작은 부드러운 것). 짙은 청회색 등, 비늘 줄마다 짙은 가로 줄무늬, 꼬리가 갈라진다
    'mullet': dict(type='B', name='숭어', depth=0.19, width=0.14, tmax=0.4, snout=0.55, blunt=0.7, head_w=1.2, mouth=-0.05, gape=0.028, scales=40, xsec=2.2,
                   fins=[_fin('dorsal', (0.35, 0.42), 0.48, prof=[(0, 1.0), (1, 0.3)], spiny=True), _fin('dorsal', (0.6, 0.67), 0.42, prof=[(0, 1.0), (1, 0.35)]),
                         _fin('anal', (0.6, 0.7), 0.38, prof=[(0, 1.0), (1, 0.4)]), _fin('caudal', h=0.25, shape='fork'),
                         _fin('pectoral', s=(0.22, 0.32), h=0.11), _fin('pelvic', s=(0.38, 0.38), h=0.09)],
                   colors=P('#34444e', '#98a6ac', '#eaeeee', silver=0.7, fin_up=('#4e5e66', '#6a7a82', 0.75), fin_low=('#9aa6aa', '#b4bec0', 0.6),
                            patterns=[('hstripes', dict(n=7, v=(0.4, 0.88), w=0.014, color='#2e3e48', a=0.55))]),
                   eye=dict(s=0.085, z=0.2, r=0.021, iris='#c8a040')),
    # ===== C. 농어형 =====
    # 쏘가리: 길고 뾰족한 머리에 눈 뒤까지 찢어진 큰 입, 아래턱이 앞으로 나온다. 가시가 촘촘한 긴 등지느러미,
    # 둥근 꼬리. 누런 바탕에 표범 같은 짙은 고리 무늬, 지느러미에도 점, 주둥이에서 눈을 지나는 검은 줄
    'mandarin': dict(type='C', name='쏘가리', depth=0.27, tmax=0.4, mouth=0.25, snout=0.9, blunt=1.0, scales=90, gape=0.1, jaw=0.014,
                     fins=[_fin('dorsal', (0.24, 0.56), 0.4, prof=[(0, 0.4), (0.2, 1.0), (0.85, 0.75), (1, 0.6)], spiny=True),
                           _fin('dorsal', (0.56, 0.78), 0.5, prof=[(0, 0.75), (0.45, 1.0), (1, 0.45)]),
                           _fin('anal', (0.6, 0.77), 0.45, prof=[(0, 0.6), (0.45, 1.0), (1, 0.45)]),
                           _fin('caudal', h=0.24, shape='round'),
                           _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                     colors=P('#6a5a30', '#c8aa5a', '#ece0b0', silver=0.1, fin_up=('#8a7a40', '#6a5a30', 0.9), fin_low=('#b09a58', '#c8b070', 0.85),
                              patterns=[('rosettes', dict(n=46, r=(8, 16), color='#2e2814', v=(0.25, 1.0))), ('eyeline', dict(color='#2e2814', w=0.03)),
                                        ('finspots', '#3a3018')]),
                     eye=dict(s=0.11, z=0.45, r=0.02, iris='#d8a020')),
    # 배스 (큰입우럭): 이름대로 눈 뒤까지 닿는 아주 큰 입, 아래턱이 살짝 나온다. 가시 등지느러미와 부드러운 등지느러미
    # 사이가 깊게 파이고, 올리브색 몸 옆구리 가운데에 들쭉날쭉한 검은 띠
    'bass': dict(type='C', name='배스', depth=0.26, mouth=0.2, snout=0.85, blunt=0.9, scales=65, gape=0.12, jaw=0.01, head_w=1.08,
                 fins=[_fin('dorsal', (0.27, 0.5), 0.4, prof=[(0, 0.5), (0.3, 1.0), (1, 0.3)], spiny=True),
                       _fin('dorsal', (0.5, 0.76), 0.5, prof=[(0, 0.6), (0.3, 1.0), (1, 0.45)]),
                       _fin('anal', (0.6, 0.78), 0.42), _fin('caudal', h=0.24, shape='emarg'),
                       _fin('pectoral', s=(0.26, 0.26), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.11)],
                 colors=P('#2f4422', '#7a8a4a', '#e6e2c0', silver=0.15, fin_up=('#4a5a30', '#3a4a26', 0.85), fin_low=('#7a8050', '#8a9060', 0.8),
                          patterns=[('blotchline', dict(v=0.55, color='#1e2a14', w=0.08)), ('mottle', dict(color='#3a4a24', a=0.4))]),
                 eye=dict(s=0.11, z=0.4, r=0.022, iris='#a0582a')),
    # 블루길: 접시처럼 둥글고 높은 몸에 가파른 이마, 작은 입, 큰 눈. 아가미 뚜껑 끝의 검은 귀(귓불)가 표지.
    # 가시·부드러운 등지느러미가 이어지고 뒤쪽이 더 높고 둥글다. 올리브빛 몸에 희미한 세로 띠, 가슴은 누런 주황
    'bluegill': dict(type='C', name='블루길', depth=0.45, width=0.12, tmax=0.4, mouth=0.15, snout=0.8, blunt=1.1, xsec=1.6, arch=0.56, scales=45, gape=0.035,
                     fins=[_fin('dorsal', (0.3, 0.53), 0.3, prof=[(0, 0.45), (0.25, 1.0), (1, 0.8)], spiny=True),
                           _fin('dorsal', (0.53, 0.78), 0.42, prof=[(0, 0.8), (0.4, 1.0), (1, 0.45)]),
                           _fin('anal', (0.56, 0.78), 0.36, prof=[(0, 0.6), (0.4, 1.0), (1, 0.45)]), _fin('caudal', h=0.24, shape='fork'),
                           _fin('pectoral', s=(0.3, 0.3), h=0.22, long=True), _fin('pelvic', s=(0.33, 0.33), h=0.11)],
                     colors=P('#2a4048', '#6a8070', '#d8c078', silver=0.2, fin_up=('#3a5050', '#2a3a3a', 0.75), fin_low=('#6a7058', '#8a8a6a', 0.65),
                              patterns=[('bands', dict(n=7, u=(0.25, 0.85), w=0.06, color='#283a3c', v=(0.3, 1.0), a=0.4)),
                                        ('earflap', '#0c1418'), ('throat', '#b08a48')]),
                     eye=dict(s=0.11, z=0.4, r=0.026, iris='#44566a')),
    # 농어: 길쭉한 은빛 몸에 뾰족한 머리, 눈 뒤까지 닿는 큰 입과 앞으로 나온 아래턱. 가시 등지느러미가 높고
    # 부드러운 등지느러미와 살짝 떨어진다. 등은 청회색, 등 쪽에 잔 검은 점(어린 농어일수록 많다), 꼬리는 얕게 갈라진다
    'seabass': dict(type='C', name='농어', depth=0.22, width=0.12, tmax=0.38, mouth=0.25, snout=0.92, blunt=1.0, scales=80, gape=0.1, jaw=0.012,
                    fins=[_fin('dorsal', (0.28, 0.52), 0.5, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.45), (1, 0.3)], spiny=True),
                          _fin('dorsal', (0.55, 0.78), 0.45, prof=[(0, 0.8), (0.25, 1.0), (1, 0.4)]),
                          _fin('anal', (0.62, 0.77), 0.4, prof=[(0, 0.6), (0.3, 1.0), (1, 0.45)]), _fin('caudal', h=0.24, shape='fork'),
                          _fin('pectoral', s=(0.26, 0.26), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                    colors=P('#3e4e5c', '#b4bec4', '#f0f2f0', silver=0.8, fin_up=('#5a6a74', '#3e4a54', 0.75), fin_low=('#c0c4c4', '#d0d4d4', 0.6),
                             patterns=[('spots', dict(n=50, r=(3, 5), color='#1e262e', v=(0.62, 1.0), u=(0.18, 0.8), a=0.85))]),
                    eye=dict(s=0.11, z=0.4, r=0.022, iris='#c8a868')),
    # 볼락: 얼굴의 반은 될 만큼 아주 큰 눈, 큰 머리에 앞으로 나온 아래턱. 옆으로 납작하고 높은 몸,
    # 가시가 억센 등지느러미와 큰 부채 가슴지느러미. 갈색·회갈색 바탕에 흐린 가로 띠 5개
    'rockfish': dict(type='C', name='볼락', depth=0.32, width=0.13, tmax=0.36, mouth=0.3, snout=0.75, blunt=0.9, head_w=1.1, mouth_h=0.22,
                     gape=0.085, jaw=0.012, scales=55,
                     fins=[_fin('dorsal', (0.25, 0.56), 0.42, prof=[(0, 0.5), (0.2, 1.0), (0.85, 0.55), (1, 0.4)], spiny=True),
                           _fin('dorsal', (0.56, 0.78), 0.45, prof=[(0, 0.75), (0.35, 1.0), (1, 0.45)]),
                           _fin('anal', (0.62, 0.77), 0.45, prof=[(0, 0.6), (0.4, 1.0), (1, 0.5)]), _fin('caudal', h=0.24, shape='emarg'),
                           _fin('pectoral', s=(0.26, 0.26), h=0.2), _fin('pelvic', s=(0.3, 0.3), h=0.14)],
                     eye=dict(s=0.11, z=0.45, r=0.034, iris='#c09040'),
                     colors=P('#3a2e26', '#7e6248', '#cfae8c', silver=0.15, fin_up=('#4a3a30', '#3a2e26', 0.85), fin_low=('#6a5040', '#7a6050', 0.8),
                              patterns=[('bands', dict(n=5, u=(0.3, 0.85), w=0.07, color='#2a201a', v=(0.35, 1.0), a=0.5)), ('mottle', dict(color='#5a4232', a=0.3))])),
    # 쏨뱅이: 가시가 돋은 크고 울퉁불퉁한 머리, 큰 입, 머리 위로 솟은 큰 눈. 붉은 갈색 얼룩에 흰 반점이 흩어지고
    # 지느러미에도 짙은 점. 둥근 꼬리, 아주 큰 부채 가슴지느러미 (바닥에 앉아 버틴다)
    'scorpionfish': dict(type='C', name='쏨뱅이', depth=0.3, width=0.17, tmax=0.34, head_w=1.25, mouth=0.25, snout=0.6, blunt=0.85, mouth_h=0.24,
                         gape=0.09, jaw=0.008, lips=1.3, scales=60,
                         fins=[_fin('dorsal', (0.24, 0.56), 0.45, prof=[(0, 0.6), (0.2, 1.0), (0.85, 0.5), (1, 0.45)], spiny=True),
                               _fin('dorsal', (0.56, 0.78), 0.42, prof=[(0, 0.75), (0.4, 1.0), (1, 0.5)]),
                               _fin('anal', (0.62, 0.76), 0.45, prof=[(0, 0.6), (0.4, 1.0), (1, 0.55)]), _fin('caudal', h=0.22, shape='round'),
                               _fin('pectoral', s=(0.25, 0.25), h=0.24), _fin('pelvic', s=(0.29, 0.29), h=0.14)],
                         eye=dict(s=0.11, z=0.55, r=0.028, iris='#d06030'),
                         colors=P('#7a3020', '#b8583a', '#e8b49a', silver=0.0, fin_up=('#a04a30', '#d07050', 0.85), fin_low=('#c06040', '#d88060', 0.85),
                                  patterns=[('mottle', dict(color='#5a1e14', a=0.6, scale=0.07)), ('spots', dict(n=40, r=(3, 6), color='#f0d0b0', v=(0.2, 0.9), a=0.6)),
                                            ('finspots', '#6a2014')])),
    # 우럭 (조피볼락): 큰 머리와 크게 벌어지는 입, 아래턱이 나오고 눈이 크다. 짙은 회흑색 바탕에 검은 얼룩,
    # 눈 뒤로 비스듬한 검은 줄 2~3개. 억센 가시 등지느러미, 꼬리는 거의 곧다
    'jacopever': dict(type='C', name='우럭', depth=0.3, width=0.15, tmax=0.36, mouth=0.32, snout=0.75, blunt=0.92, head_w=1.15, mouth_h=0.22,
                      gape=0.1, jaw=0.014, scales=70,
                      fins=[_fin('dorsal', (0.25, 0.56), 0.45, prof=[(0, 0.5), (0.2, 1.0), (0.85, 0.5), (1, 0.4)], spiny=True),
                            _fin('dorsal', (0.56, 0.78), 0.45, prof=[(0, 0.75), (0.35, 1.0), (1, 0.45)]),
                            _fin('anal', (0.62, 0.77), 0.45, prof=[(0, 0.6), (0.4, 1.0), (1, 0.5)]), _fin('caudal', h=0.24, shape='truncate'),
                            _fin('pectoral', s=(0.26, 0.26), h=0.2), _fin('pelvic', s=(0.3, 0.3), h=0.14)],
                      eye=dict(s=0.11, z=0.45, r=0.028, iris='#b08040'),
                      colors=P('#2e2e2a', '#5e5c52', '#b8b4a4', silver=0.05, fin_up=('#3a3a34', '#2a2a26', 0.9), fin_low=('#4a4a42', '#3a3a34', 0.85),
                               patterns=[('mottle', dict(color='#22221e', a=0.55, scale=0.06)), ('eyeline', dict(color='#1a1a18', w=0.025, n=3))])),
    # 쥐노래미: 길쭉한 몸에 뾰족한 머리, 작은 입과 도톰한 입술. 등지느러미 하나가 가운데쯤 얕게 파이며 꼬리자루까지
    # 길게 이어지고 뒷지느러미도 길다. 큰 부채 가슴지느러미, 곧은 꼬리. 황갈색 바탕에 짙은 갈색 구름 얼룩
    'greenling': dict(type='C', name='쥐노래미', depth=0.21, width=0.12, tmax=0.4, mouth=0.05, snout=0.88, blunt=0.95, scales=95, gape=0.04, lips=1.4,
                      fins=[_fin('dorsal', (0.24, 0.53), 0.36, prof=[(0, 0.45), (0.2, 1.0), (0.8, 0.85), (1, 0.55)], spiny=True),
                            _fin('dorsal', (0.53, 0.84), 0.42, prof=[(0, 0.65), (0.2, 1.0), (0.85, 0.9), (1, 0.5)]),
                            _fin('anal', (0.5, 0.82), 0.36, prof=[(0, 0.5), (0.15, 1.0), (0.85, 0.9), (1, 0.5)]), _fin('caudal', h=0.22, shape='truncate'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.18), _fin('pelvic', s=(0.29, 0.29), h=0.1)],
                      colors=P('#5a4a30', '#8a7650', '#d0c4a0', silver=0.05, fin_up=('#6a5a3a', '#5a4a2e', 0.85), fin_low=('#7a6a48', '#8a7a58', 0.8),
                               patterns=[('mottle', dict(color='#3a2e1e', a=0.55, scale=0.05)), ('stripe', dict(v=0.8, w=0.02, color='#c8b890', a=0.4))]),
                      eye=dict(iris='#c09840')),
    # 자이언트 그루퍼: 통나무처럼 두툼하고 긴 몸, 넓적한 큰 머리에 두꺼운 입술과 앞으로 나온 아래턱, 작은 눈.
    # 낮고 긴 가시 등지느러미 뒤로 둥글고 높은 부드러운 등지느러미, 둥근 꼬리. 회갈색 바탕에 흐린 얼룩과 연한 반점
    'giantgrouper': dict(type='C', name='자이언트 그루퍼', depth=0.3, width=0.2, tmax=0.38, head_w=1.2, mouth=0.28, snout=0.7, blunt=0.85, mouth_h=0.24,
                         gape=0.1, jaw=0.014, lips=1.6, scales=110,
                         fins=[_fin('dorsal', (0.28, 0.58), 0.24, prof=[(0, 0.6), (0.2, 1.0), (1, 0.85)], spiny=True),
                               _fin('dorsal', (0.58, 0.8), 0.4, prof=[(0, 0.6), (0.45, 1.0), (1, 0.45)]),
                               _fin('anal', (0.63, 0.79), 0.4, prof=[(0, 0.55), (0.45, 1.0), (1, 0.45)]), _fin('caudal', h=0.22, shape='round'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.18), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                         colors=P('#2e2a22', '#5e5240', '#a49478', silver=0.05, fin_up=('#3e382c', '#2a261e', 0.9), fin_low=('#4e4636', '#3a3428', 0.9),
                                  patterns=[('obliquebands', dict(n=6, color='#1e1a14', a=0.35)), ('mottle', dict(color='#3a3226', a=0.4)),
                                            ('spots', dict(n=110, r=(3, 6), color='#c8b898', v=(0.15, 0.95), a=0.45)), ('finspots', '#1e1a14')]),
                         eye=dict(s=0.1, z=0.42, r=0.015, iris='#806040'), anim=dict(amp=0.8, freq=0.7)),
    # 다금바리 (Niphon spinosus): 길쭉한 농어형 몸, 뾰족한 머리에 큰 입과 앞으로 나온 아래턱, 큰 눈.
    # 가시가 길게 솟은 첫 등지느러미가 둘째와 깊게 파여 나뉜다. 등은 자갈색, 배는 은백색, 어릴 때 몸 가운데 검은 세로띠 흔적
    'dageumbari': dict(type='C', name='다금바리', depth=0.24, width=0.14, tmax=0.38, head_w=1.05, mouth=0.27, snout=0.9, blunt=1.0, scales=100,
                       gape=0.1, jaw=0.016,
                       fins=[_fin('dorsal', (0.27, 0.53), 0.55, prof=[(0, 0.5), (0.2, 1.0), (0.8, 0.35), (1, 0.15)], spiny=True),
                             _fin('dorsal', (0.56, 0.77), 0.36, prof=[(0, 0.8), (0.25, 1.0), (1, 0.45)]),
                             _fin('anal', (0.62, 0.77), 0.32, prof=[(0, 0.6), (0.3, 1.0), (1, 0.45)]), _fin('caudal', h=0.22, shape='emarg'),
                             _fin('pectoral', s=(0.27, 0.27), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                       colors=P('#4a3a34', '#8a7060', '#e0d8cc', silver=0.25, fin_up=('#5a463c', '#3a2c26', 0.9), fin_low=('#7a6658', '#5a4a40', 0.9),
                                patterns=[('stripe', dict(v=0.5, w=0.05, color='#2a201c', a=0.35)), ('mottle', dict(color='#3a2e28', a=0.25))]),
                       eye=dict(s=0.1, z=0.45, r=0.024, iris='#d0c090'), anim=dict(amp=0.8, freq=0.75)),
    # 돗돔 (Stereolepis doederleini): 높고 두툼한 거대한 몸에 큰 머리, 큰 입과 앞으로 나온 아래턱. 가시 등지느러미와
    # 부드러운 등지느러미 사이가 파이고 꼬리는 거의 곧다. 짙은 흑갈색에 흐린 세로 줄 흔적, 배는 조금 밝다
    'dotdom': dict(type='C', name='돗돔', depth=0.34, width=0.18, tmax=0.36, head_w=1.15, mouth=0.28, snout=0.75, blunt=0.9, mouth_h=0.22, scales=120,
                   gape=0.1, jaw=0.014, lips=1.3,
                   fins=[_fin('dorsal', (0.28, 0.55), 0.34, prof=[(0, 0.4), (0.2, 1.0), (0.85, 0.45), (1, 0.35)], spiny=True),
                         _fin('dorsal', (0.56, 0.78), 0.38, prof=[(0, 0.8), (0.3, 1.0), (1, 0.5)]),
                         _fin('anal', (0.62, 0.78), 0.36, prof=[(0, 0.6), (0.4, 1.0), (1, 0.5)]), _fin('caudal', h=0.24, shape='emarg'),
                         _fin('pectoral', s=(0.27, 0.27), h=0.17), _fin('pelvic', s=(0.31, 0.31), h=0.13)],
                   colors=P('#24262a', '#44484c', '#8a8c88', silver=0.1, fin_up=('#2a2c30', '#1a1c1e', 0.95), fin_low=('#3a3c40', '#26282a', 0.95),
                            patterns=[('mottle', dict(color='#1a1c20', a=0.35))]),
                   eye=dict(s=0.09, z=0.43, r=0.02, iris='#b0a890'), anim=dict(amp=0.7, freq=0.6)),
    # ===== D. 도미형 =====
    # 감성돔: 달걀꼴 높은 몸에 이마가 가파르게 내려와 주둥이가 뾰족해지고, 입은 낮고 입술이 도톰하다.
    # 억센 가시 등지느러미가 길게 이어지고 꼬리는 갈라진다. 은빛 도는 짙은 회흑색, 흐린 가로 띠와 비늘 줄마다 가는 세로 줄
    'blackseabream': dict(type='D', name='감성돔', depth=0.39, tmax=0.38, snout=0.7, blunt=1.05, mouth=-0.28, lips=1.4, gape=0.04,
                          fins=[_fin('dorsal', (0.28, 0.6), 0.4, prof=[(0, 0.35), (0.18, 1.0), (1, 0.6)], spiny=True),
                                _fin('dorsal', (0.6, 0.79), 0.3, prof=[(0, 0.75), (1, 0.55)]),
                                _fin('anal', (0.62, 0.79), 0.32, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                                _fin('caudal', h=0.27, shape='fork'),
                                _fin('pectoral', s=(0.3, 0.3), h=0.22, long=True),
                                _fin('pelvic', s=(0.34, 0.34), h=0.13)],
                          colors=P('#2e3438', '#8a9298', '#dcdfe0', silver=0.65, fin_up=('#3a4044', '#2a3034', 0.85), fin_low=('#4a5054', '#3a4044', 0.8),
                                   patterns=[('bands', dict(n=6, u=(0.25, 0.8), w=0.05, color='#3a4248', v=(0.3, 1.0), a=0.25)),
                                             ('hstripes', dict(n=8, v=(0.25, 0.8), w=0.008, color='#5a6268', a=0.22))]),
                          eye=dict(s=0.13, z=0.42, r=0.026, iris='#c0b080')),
    # 참돔: 감성돔보다 길쭉하고 이마가 둥글게 솟는다. 분홍빛 붉은 몸에 파란 점이 흩뿌려지고 배는 은백색,
    # 꼬리는 크게 갈라지고 붉다. 등지느러미 가시가 길다
    'redseabream': dict(type='D', name='참돔', depth=0.36, width=0.12, tmax=0.36, snout=0.58, blunt=0.95, mouth=-0.2, lips=1.2, gape=0.045,
                        fins=[_fin('dorsal', (0.28, 0.6), 0.42, prof=[(0, 0.4), (0.2, 1.0), (1, 0.55)], spiny=True),
                              _fin('dorsal', (0.6, 0.8), 0.3, prof=[(0, 0.75), (1, 0.55)]),
                              _fin('anal', (0.62, 0.8), 0.3, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                              _fin('caudal', h=0.3, shape='fork'),
                              _fin('pectoral', s=(0.3, 0.3), h=0.24, long=True),
                              _fin('pelvic', s=(0.34, 0.34), h=0.12)],
                        colors=P('#c0464a', '#e88a86', '#f6e4dc', silver=0.4, fin_up=('#d0605e', '#c04848', 0.75), fin_low=('#e8a0a0', '#f0c0c0', 0.7),
                                 patterns=[('spots', dict(n=90, r=(2, 4), color='#5ac8f0', v=(0.4, 1.0), u=(0.1, 0.85), a=0.85))]),
                        eye=dict(s=0.13, z=0.42, r=0.027, iris='#e0b040')),
    # 돌돔: 아주 높고 납작한 몸에 굵은 검은 가로 띠 7줄 (첫 줄이 눈을 지난다). 이빨이 붙은 새부리 같은 작은 입,
    # 낮은 가시 등지느러미 뒤로 높이 솟은 부드러운 등·뒷지느러미, 곧은 꼬리. 지느러미는 검다
    'beakfish': dict(type='D', name='돌돔', depth=0.44, width=0.13, tmax=0.38, mouth=-0.1, snout=0.55, blunt=0.9, lips=1.5, gape=0.03,
                     fins=[_fin('dorsal', (0.3, 0.56), 0.3, prof=[(0, 0.45), (0.25, 1.0), (1, 0.7)], spiny=True),
                           _fin('dorsal', (0.56, 0.8), 0.42, prof=[(0, 0.85), (0.2, 1.0), (1, 0.45)]),
                           _fin('anal', (0.6, 0.8), 0.4, prof=[(0, 0.7), (0.25, 1.0), (1, 0.45)]),
                           _fin('caudal', h=0.25, shape='emarg'),
                           _fin('pectoral', s=(0.3, 0.3), h=0.17),
                           _fin('pelvic', s=(0.34, 0.34), h=0.13)],
                     colors=P('#4a4e50', '#9ea2a0', '#d8dad4', silver=0.25, fin_up=('#2a2e30', '#1e2224', 0.9), fin_low=('#3a3e40', '#2a2e30', 0.9),
                              patterns=[('bands', dict(n=7, u=(0.11, 0.87), w=0.075, color='#16181a', v=(0.0, 1.0), a=0.9, taper=True))]),
                     eye=dict(s=0.13, z=0.42, r=0.022, iris='#c8a860')),
    # 벵에돔: 둥글고 뭉툭한 주둥이에 아주 작은 입, 달걀꼴 몸. 낮은 등지느러미가 길게 이어지고 꼬리는 살짝 파인다.
    # 짙은 청흑색에 비늘마다 검은 점, 눈은 푸른빛
    'opaleye': dict(type='D', name='벵에돔', depth=0.37, width=0.13, tmax=0.4, snout=0.52, blunt=0.85, mouth=-0.05, lips=1.3, gape=0.028,
                    fins=[_fin('dorsal', (0.32, 0.6), 0.28, prof=[(0, 0.5), (0.25, 1.0), (1, 0.85)], spiny=True), _fin('dorsal', (0.6, 0.8), 0.32, prof=[(0, 0.9), (0.3, 1.0), (1, 0.5)]),
                          _fin('anal', (0.62, 0.8), 0.3, prof=[(0, 0.8), (0.3, 1.0), (1, 0.5)]), _fin('caudal', h=0.25, shape='emarg'),
                          _fin('pectoral', s=(0.3, 0.3), h=0.15), _fin('pelvic', s=(0.36, 0.36), h=0.11)],
                    colors=P('#1e2a28', '#3a4a46', '#6a7a74', silver=0.15, fin_up=('#1e2624', '#141a18', 0.9), fin_low=('#2a3432', '#1e2624', 0.9),
                             patterns=[('scale_dots', dict(color='#101614', a=0.5))]),
                    eye=dict(s=0.13, z=0.42, r=0.023, iris='#4a90b0')),
    # 쥐치: 마름모꼴 높은 몸, 뾰족하게 내민 주둥이 끝의 아주 작은 입. 눈 위에 굵은 가시 하나가 서고,
    # 뒤쪽 등·뒷지느러미가 마주 보며 길고 낮다. 둥근 꼬리, 황갈색 바탕에 짙은 갈색 줄·얼룩.
    # 머리: 눈 앞 이마가 오목하게 패여 주둥이가 원뿔처럼 튀어나오고, 노란 홍채의 큰 눈이 가시 밑 높은 곳에
    'filefish': dict(type='D', name='쥐치', depth=0.5, width=0.1, tmax=0.45, body_end=0.82, snout=1.0, blunt=1.3, mouth=0.12, gape=0.02, hump=-0.06, hump_s=0.1, xsec=1.6, scales=0,
                     fins=[_fin('dorsal', (0.22, 0.25), 0.28, prof=[(0, 1.0), (1, 0.9)], spine=True),
                           _fin('dorsal', (0.52, 0.8), 0.25, prof=[(0, 0.7), (0.3, 1.0), (1, 0.6)]),
                           _fin('anal', (0.52, 0.8), 0.25, prof=[(0, 0.7), (0.3, 1.0), (1, 0.6)]),
                           _fin('caudal', h=0.2, shape='round'), _fin('pectoral', s=(0.28, 0.28), h=0.08)],
                     eye=dict(s=0.2, z=0.45, r=0.03, iris='#d8a830'),
                     colors=P('#6a6a4a', '#a4a07a', '#d8d4b8', silver=0.05, fin_up=('#c0b890', '#d8d0a8', 0.6), fin_low=('#c0b890', '#d8d0a8', 0.6),
                              patterns=[('mottle', dict(color='#4a4a32', a=0.5, scale=0.05)), ('hstripes', dict(n=5, v=(0.3, 0.8), w=0.015, color='#5a5a40', a=0.4))])),
    # ===== E. 유선형 =====
    # 고등어: 둥근 방추형 몸에 뾰족한 원뿔 머리, 큰 눈. 작은 첫 등지느러미와 둘째 등지느러미가 멀리 떨어지고 그 뒤로
    # 토막지느러미(작은 가시 줄)가 꼬리까지. 청록색 등에 검은 물결 무늬, 은백색 배, 깊게 갈라진 꼬리
    'mackerel': dict(type='E', name='고등어', depth=0.17, width=0.13, tmax=0.42, snout=0.9, blunt=1.05, gape=0.075,
                     fins=[_fin('dorsal', (0.3, 0.43), 0.4, prof=[(0, 1.0), (0.4, 0.7), (1, 0.15)], spiny=True),
                           _fin('dorsal', (0.56, 0.63), 0.38, prof=[(0, 1.0), (1, 0.3)]),
                           _fin('anal', (0.6, 0.67), 0.32, prof=[(0, 1.0), (1, 0.3)]),
                           _fin('caudal', h=0.3, shape='deepfork'),
                           _fin('pectoral', s=(0.24, 0.24), h=0.12),
                           _fin('pelvic', s=(0.32, 0.32), h=0.07)],
                     colors=P('#1e5a5a', '#9ab8b8', '#f2f4f2', silver=0.8, metallic=0.35, fin_up=('#3a5a5a', '#5a7a7a', 0.7), fin_low=('#9aaaaa', '#b8c4c4', 0.6),
                              patterns=[('waves', dict(color='#0e2a32', v=(0.6, 1.0), a=1.0)), ('finlets', True)]),
                     eye=dict(s=0.1, z=0.38, r=0.022, iris='#c8ccc8')),
    # 전갱이: 고등어보다 높고 납작한 몸에 아주 큰 눈. 옆줄을 따라 꼬리까지 가시 비늘(모비늘) 줄이 이어지며 몸 가운데서
    # 아래로 꺾인다. 아가미 뚜껑 위 검은 점, 높은 삼각 첫 등지느러미와 길고 낮은 둘째 등·뒷지느러미, 누런 꼬리
    'horsemackerel': dict(type='E', name='전갱이', depth=0.23, width=0.11, tmax=0.4, snout=0.8, blunt=0.95, mouth=0.1, gape=0.055, jaw=0.006,
                          fins=[_fin('dorsal', (0.3, 0.42), 0.42, prof=[(0, 0.7), (0.25, 1.0), (1, 0.2)], spiny=True),
                                _fin('dorsal', (0.43, 0.82), 0.18, prof=[(0, 1.0), (0.15, 0.7), (1, 0.45)]),
                                _fin('anal', (0.55, 0.82), 0.16, prof=[(0, 1.0), (0.15, 0.7), (1, 0.45)]),
                                _fin('caudal', h=0.3, shape='deepfork', group='low'),
                                _fin('pectoral', s=(0.25, 0.25), h=0.2, long=True),
                                _fin('pelvic', s=(0.33, 0.33), h=0.07)],
                          colors=P('#4a6a6e', '#b8c8c4', '#f2f2ee', silver=0.85, metallic=0.3, fin_up=('#6a8080', '#8aa0a0', 0.65), fin_low=('#d0c070', '#d8c060', 0.65),
                                   patterns=[('scutes', dict(color='#8a9a96', a=0.8)), ('earflap', '#1a2224'), ('stripe', dict(v=0.66, w=0.03, color='#d8c870', a=0.35))]),
                          eye=dict(s=0.1, z=0.3, r=0.03, iris='#d8d0b0')),
    # 학꽁치: 바늘처럼 가늘고 긴 은빛 몸, 아래턱만 부리처럼 길게 뻗는다. 등·뒷지느러미는 꼬리 가까이 마주 보고,
    # 큰 눈, 옆구리 가운데 은청색 세로 띠
    'halfbeak': dict(type='E', name='학꽁치', depth=0.09, width=0.07, tmax=0.5, body_end=0.86, ped=0.4, snout=0.95, beak=0.14,
                     fins=[_fin('dorsal', (0.72, 0.84), 0.45), _fin('anal', (0.7, 0.84), 0.45),
                           _fin('caudal', h=0.13, shape='fork'), _fin('pectoral', s=(0.24, 0.24), h=0.07), _fin('pelvic', s=(0.6, 0.6), h=0.05)],
                     colors=P('#4a7a7a', '#c0d8d4', '#f6f8f4', silver=0.85, metallic=0.3, fin_up=('#8aa0a0', '#a0b8b8', 0.55), fin_low=('#b8c8c8', '#d0dcdc', 0.5),
                              patterns=[('stripe', dict(v=0.55, w=0.05, color='#d0e8f0', a=0.6))]),
                     eye=dict(s=0.16, z=0.3, r=0.018, iris='#d8d8cc'), anim=dict(amp=1.2, freq=1.3)),
    # ===== F. 납작한 고기 =====
    # 광어 (넙치): 두 눈이 왼쪽, 길쭉한 타원 몸에 큰 머리와 큰 입 (아래 눈 밑까지 찢어지고 송곳니가 보인다, 아래턱이 나온다). 등·뒷지느러미가 몸 둘레를 감싸고 꼬리는 둥글다.
    # 올리브 갈색 바탕에 짙은 구름 얼룩과 흰 점이 흩어진다 (뒷면은 흰색)
    'flounder': dict(type='F', name='광어', flat='left', depth=0.4, mouth=0.1, snout=0.65, gape=0.09, jaw=0.008, teeth=True, maxilla=0.8,
                     colors=P('#4a4030', '#6e6248', '#f2f0e8', silver=0.0, fin_up=('#5a4e3a', '#4a4030', 0.9), fin_low=('#6a5e48', '#5a4e3a', 0.9),
                              patterns=[('mottle', dict(color='#2a2418', a=0.6, scale=0.05)), ('spots', dict(n=70, r=(2, 5), color='#e0d4b8', v=(0.1, 1.0), a=0.5))]),
                     eye=dict(iris='#c0a050', r=0.026)),
    # 문치가자미: 두 눈이 오른쪽, 광어보다 둥글고 높은 달걀꼴 몸에 작은 입. 갈색 바탕에 짙은 갈색 둥근 얼룩이
    # 흩어지고 사이사이 작은 흰 점
    'marbledflounder': dict(type='F', name='문치가자미', flat='right', depth=0.47, snout=0.5, mouth=0.0, ped=0.3,
                            colors=P('#54463a', '#76664e', '#f4f2ec', silver=0.0, fin_up=('#5a4c3c', '#4a3e30', 0.9), fin_low=('#5a4c3c', '#4a3e30', 0.9),
                                     patterns=[('mottle', dict(color='#3a3024', a=0.4)), ('spots', dict(n=50, r=(5, 12), color='#2a2018', v=(0.05, 1.0), a=0.75)),
                                               ('spots', dict(n=30, r=(2, 4), color='#e8dcc4', v=(0.05, 1.0), a=0.55))]),
                            eye=dict(iris='#a08040', r=0.024)),
    # ===== G. 장어형 =====
    # 뱀장어: 작은 눈, 아래턱이 살짝 나온 둥근 주둥이. 등지느러미가 몸 앞쪽 1/3 뒤에서 늦게 시작해 꼬리를 돌아
    # 뒷지느러미와 이어진다. 짙은 올리브 갈색 등, 누런 흰 배, 비늘이 안 보이는 매끈한 피부
    'eel': dict(type='G', name='뱀장어', jaw=0.004, gape=0.045,
                colors=P('#2a3020', '#4e5636', '#d8d4a8', silver=0.1, fin_up=('#3a4228', '#2a3020', 0.9), fin_low=('#c0bc98', '#a8a480', 0.85)),
                eye=dict(iris='#a0a080')),
    # 붕장어: 뱀장어보다 눈이 크고 주둥이가 위턱 앞으로 살짝 나오며, 입이 눈 밑까지 찢어진다. 등지느러미가 가슴지느러미
    # 끝 바로 위에서 일찍 시작한다. 회갈색 등에 옆줄을 따라 흰 점(감각 구멍) 한 줄 + 그 위 한 줄, 지느러미 가장자리는 검다
    'conger': dict(type='G', name='붕장어', depth=0.075, mouth=0.1, gape=0.06,
                   fins=[_fin('dorsal', (0.16, 1.0), 0.32, prof=[(0, 0.15), (0.1, 0.75), (0.5, 1.0), (1, 0.9)]),
                         _fin('anal', (0.4, 1.0), 0.34, prof=[(0, 0.15), (0.15, 0.75), (0.55, 1.0), (1, 0.9)]),
                         _fin('caudal', h=0.05, shape='point'),
                         _fin('pectoral', s=(0.1, 0.1), h=0.06)],
                   colors=P('#4a463c', '#8a8270', '#ece8dc', silver=0.1, fin_up=('#3a362e', '#1a1814', 0.9), fin_low=('#d8d4c8', '#2a2620', 0.85),
                            patterns=[('pores', dict(color='#f8f6ee', v=0.55, n=40)), ('pores', dict(color='#f8f6ee', v=0.85, n=24))]),
                   eye=dict(s=0.045, z=0.4, r=0.011, iris='#c0b090')),
    # ===== H. 메기형 =====
    # 메기 (Silurus asotus): 위턱의 아주 긴 수염 한 쌍(가슴지느러미까지) + 아래턱의 짧은 수염 한 쌍, 두툼한 입술.
    # 넓은 입은 아래턱이 위턱보다 앞으로 나오고, 작은 눈은 머리 옆 높은 곳에. 수염은 가늘게 휜다
    'catfish': dict(type='H', name='메기', lips=1.6, jaw=0.008,
                    barbels=[(0.03, 0.1, 0.17, 0.004, 'side'), (0.025, -0.35, 0.05, 0.0028, 'down')],
                    colors=P('#2a2a20', '#5a5640', '#d8d4bc', silver=0.0, fin_up=('#3a3a2c', '#2a2a20', 0.9), fin_low=('#4a4a38', '#3a3a2c', 0.9),
                             patterns=[('mottle', dict(color='#1e1e16', a=0.6, scale=0.06))]),
                    eye=dict(iris='#a09060')),
    # 동자개 (빠가사리): 위아래로 눌린 넓적한 머리에 수염 네 쌍, 굵은 가시가 선 등지느러미·가슴지느러미(빠각빠각 소리),
    # 꼬리자루 위 기름지느러미, 갈라진 꼬리. 노란 바탕에 짙은 갈색 큰 얼룩이 안장처럼 얹힌다
    'bagrid': dict(type='H', name='동자개', depth=0.19, width=0.13, head_w=1.18, head_flat=0.2, snout=0.6, blunt=0.9, tmax=0.3, body_end=0.82,
                   fins=[_fin('dorsal', (0.3, 0.38), 0.55, prof=[(0, 1.0), (1, 0.6)], spine=True),
                         _fin('adipose', (0.66, 0.8), 0.18, prof=[(0, 0.4), (0.4, 1.0), (1, 0.5)]),
                         _fin('anal', (0.62, 0.8), 0.32), _fin('caudal', h=0.22, shape='fork'),
                         _fin('pectoral', s=(0.17, 0.17), h=0.14, spine=True), _fin('pelvic', s=(0.42, 0.42), h=0.08)],
                   barbels=[(0.03, 0.15, 0.06, 0.003, 'side'), (0.035, 0.35, 0.035, 0.002, 'up'),
                            (0.025, -0.3, 0.04, 0.002, 'down'), (0.03, -0.4, 0.03, 0.002, 'down')],
                   colors=P('#5a4a1e', '#c8a040', '#f0dc8a', silver=0.0, fin_up=('#a08030', '#6a5420', 0.85), fin_low=('#c8a040', '#a08030', 0.85),
                            patterns=[('blotches', dict(color='#2e2410', a=0.75))]),
                   eye=dict(iris='#c0a040')),
    # 가물치: 뱀 머리처럼 위아래로 눌린 넓적한 머리에 큰 입(아래턱이 나온다), 통나무 같은 긴 몸.
    # 등·뒷지느러미가 꼬리자루까지 길게 이어지고 둥근 꼬리. 올리브 회색 바탕에 검은 얼룩 두 줄, 눈 뒤로 검은 줄 두 개.
    # 눈은 주둥이 가까이 높이 붙고 홍채가 주황빛
    'snakehead': dict(type='H', name='가물치', depth=0.15, width=0.14, head_w=1.12, head_flat=0.15, tmax=0.4, body_end=0.83, ped=0.75, snout=0.75, blunt=0.9,
                      mouth=0.15, gape=0.075, jaw=0.01, scales=70,
                      fins=[_fin('dorsal', (0.3, 0.98), 0.32, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                            _fin('anal', (0.55, 0.98), 0.3, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                            _fin('caudal', h=0.15, shape='round'), _fin('pectoral', s=(0.24, 0.24), h=0.11), _fin('pelvic', s=(0.33, 0.33), h=0.07)],
                      colors=P('#2e3424', '#6a6c4a', '#d8d6c0', silver=0.0, fin_up=('#3a3e2c', '#4a4e38', 0.9), fin_low=('#4a4e38', '#5a5e48', 0.9),
                               patterns=[('blotchrows', dict(color='#16180e', a=0.8)), ('eyeline', dict(color='#16180e', w=0.025, n=2))]),
                      eye=dict(s=0.07, z=0.4, r=0.015, iris='#e08a2a')),
    # 빙어: 가늘고 긴 반투명한 몸, 큰 눈, 아래턱이 살짝 나온 위로 향한 입. 등지느러미 뒤 꼬리자루 위에 작은 기름지느러미.
    # 연한 올리브빛 등, 옆구리 가운데 은빛 세로 띠
    'smelt': dict(type='B', name='빙어', depth=0.15, width=0.09, tmax=0.45, snout=0.85, mouth=0.25, scales=60, gape=0.05, jaw=0.005,
                  fins=[_fin('dorsal', (0.45, 0.53), 0.6), _fin('adipose', (0.74, 0.78), 0.2), _fin('anal', (0.62, 0.78), 0.45),
                        _fin('caudal', h=0.22, shape='fork'), _fin('pectoral', s=(0.2, 0.2), h=0.1), _fin('pelvic', s=(0.45, 0.45), h=0.07)],
                  colors=P('#8a9474', '#d8e0d4', '#f6f8f4', silver=0.85, metallic=0.25, fin_up=('#c8d0c4', '#e4ecec', 0.45), fin_low=('#d8e0e0', '#eef2f2', 0.4),
                           patterns=[('stripe', dict(v=0.55, w=0.035, color='#b8c8d8', a=0.7))]),
                  eye=dict(r=0.026, iris='#d0d8d8'), anim=dict(amp=1.0, freq=1.4)),
    # ===== 서천 앞바다 (서해) =====
    # 망둥어 (문절망둑): 크고 둥근 머리에 머리 위로 붙은 두 눈, 두툼한 입술의 큰 입. 원통형 긴 몸에 등지느러미 둘,
    # 둥근 꼬리. 모래색 바탕에 옆구리 가운데 짙은 점이 한 줄, 등은 잔 얼룩
    'goby': dict(type='C', name='망둥어', depth=0.16, width=0.14, head_w=1.25, tmax=0.28, ped=0.55, snout=0.5, mouth=0.1, xsec=2.1, scales=50,
                 gape=0.075, lips=1.5, blunt=0.85,
                 fins=[_fin('dorsal', (0.3, 0.42), 0.45, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)], spiny=True), _fin('dorsal', (0.48, 0.76), 0.5),
                       _fin('anal', (0.55, 0.76), 0.4), _fin('caudal', h=0.2, shape='round'),
                       _fin('pectoral', s=(0.2, 0.2), h=0.14), _fin('pelvic', s=(0.25, 0.25), h=0.08)],
                 colors=P('#5e5440', '#9a8a68', '#e0d8c0', silver=0.05, fin_up=('#7a6c50', '#5a5040', 0.85), fin_low=('#a09070', '#b0a080', 0.8),
                          patterns=[('mottle', dict(color='#3e3628', a=0.45, scale=0.06)),
                                    ('spots', dict(n=8, r=(7, 11), color='#3a3226', v=(0.5, 0.5), u=(0.25, 0.9), row=True, a=0.7))]),
                 eye=dict(s=0.1, z=0.6, r=0.02, iris='#c0a060'), anim=dict(amp=0.9, freq=0.9)),
    # 전어: 옆으로 납작하고 배가 불룩한 높은 은빛 몸, 뭉툭한 주둥이 밑의 작은 입. 등지느러미 마지막 살이 실처럼
    # 길게 늘어진다(실 모양 살은 틀에 없어 끝을 조금 남긴다). 아가미 뒤 큰 검은 점 하나, 등 쪽 비늘 줄마다 검은 점선, 노르스름한 깊은 갈래 꼬리
    'shad': dict(type='B', name='전어', depth=0.31, width=0.1, tmax=0.4, arch=0.45, snout=0.7, blunt=0.85, mouth=-0.1, gape=0.03, scales=50,
                 fins=[_fin('dorsal', (0.42, 0.53), 0.55, prof=[(0, 0.75), (0.2, 1.0), (0.8, 0.45), (1, 0.7)]), _fin('anal', (0.65, 0.8), 0.3),
                       _fin('caudal', h=0.3, shape='deepfork', group='low'), _fin('pectoral', s=(0.22, 0.22), h=0.1), _fin('pelvic', s=(0.48, 0.48), h=0.07)],
                 colors=P('#2e4a5a', '#b8c4c8', '#f4f4f0', silver=0.9, metallic=0.3, fin_up=('#8aa0a8', '#b0c0c8', 0.6), fin_low=('#d0c890', '#d8c070', 0.6),
                          patterns=[('spots', dict(n=1, r=(9, 11), color='#1a2228', v=(0.66, 0.66), u=(0.26, 0.26), row=True)),
                                    ('hstripes', dict(n=7, v=(0.55, 0.92), w=0.009, color='#2a3a44', a=0.7))]),
                 eye=dict(iris='#e0d8c0'), anim=dict(amp=1.0, freq=1.3)),
    # 백조기 (보구치): 은백색 몸에 큰 눈, 비스듬히 위를 보는 입. 가시 등지느러미가 높고 깊게 파인 뒤 부드러운 등지느러미가
    # 꼬리자루까지 길다. 뒷지느러미는 짧고, 꼬리는 쐐기꼴. 아가미 뚜껑 위의 검은 점, 아래 지느러미는 누런빛
    'whitecroaker': dict(type='C', name='백조기', depth=0.27, width=0.13, tmax=0.35, arch=0.58, snout=0.62, blunt=0.9, mouth=0.1, gape=0.065, jaw=0.005, scales=55,
                         fins=[_fin('dorsal', (0.3, 0.45), 0.48, prof=[(0, 0.5), (0.3, 1.0), (0.85, 0.35), (1, 0.15)], spiny=True),
                               _fin('dorsal', (0.46, 0.82), 0.3, prof=[(0, 0.8), (0.15, 1.0), (1, 0.75)]),
                               _fin('anal', (0.68, 0.8), 0.25), _fin('caudal', h=0.22, shape='round'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                         colors=P('#7a7a70', '#d4d0c0', '#f6f2e6', silver=0.6, metallic=0.2, fin_up=('#a8a090', '#c8c0a8', 0.7), fin_low=('#e0c880', '#e8d8a0', 0.7),
                                  patterns=[('sheen', 0.5), ('earflap', '#1a1a18')]),
                         eye=dict(s=0.1, z=0.4, r=0.025, iris='#d8c890'), anim=dict(amp=0.9, freq=0.9)),
    # 민어: 백조기보다 훨씬 크고 길쭉한 몸, 큰 입에 작은 눈. 가시 등지느러미 뒤로 긴 부드러운 등지느러미,
    # 쐐기꼴 꼬리. 회갈색 등에서 은회색 배로, 잔 비늘이 은은하게 빛난다
    'croaker': dict(type='C', name='민어', depth=0.24, width=0.14, tmax=0.36, arch=0.56, snout=0.75, blunt=0.95, mouth=0.05, gape=0.08, jaw=0.006, scales=70,
                    fins=[_fin('dorsal', (0.3, 0.46), 0.42, prof=[(0, 0.5), (0.3, 1.0), (0.85, 0.4), (1, 0.25)], spiny=True),
                          _fin('dorsal', (0.46, 0.82), 0.3, prof=[(0, 0.8), (0.15, 1.0), (1, 0.75)]),
                          _fin('anal', (0.68, 0.8), 0.25), _fin('caudal', h=0.22, shape='round'),
                          _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                    colors=P('#4a4a42', '#8c8a7a', '#d8d4c4', silver=0.35, fin_up=('#5a5850', '#4a4840', 0.85), fin_low=('#7a7468', '#8a8478', 0.8),
                             patterns=[('mottle', dict(color='#3a3a32', a=0.25, scale=0.08))]),
                    eye=dict(s=0.11, z=0.4, r=0.017, iris='#c0a868'), anim=dict(amp=0.8, freq=0.7)),
    'octopus': dict(type='O', name='주꾸미',
                    colors=P('#6a4a3e', '#9a6e58', '#e2c8b8', silver=0.0, fin_up=('#8a6050', '#a07060', 0.9), fin_low=('#8a6050', '#a07060', 0.9),
                             patterns=[('mottle', dict(color='#4a3028', a=0.55, scale=0.05)), ('spots', dict(n=50, r=(2, 4), color='#3a2018', v=(0.3, 1.0), a=0.6)),
                                       ('rings', dict(u=0.47, v=0.62, r=11, color='#e0b040'))]),
                    eye=dict(iris='#d8b040')),
    'cuttlefish': dict(type='Q', name='갑오징어',
                       colors=P('#6e5a48', '#a8957e', '#f0ebe0', silver=0.0, fin_up=('#d8c8b0', '#f0e8d8', 0.85), fin_low=('#d8c8b0', '#f0e8d8', 0.85),
                                patterns=[('bands', dict(n=16, u=(0.44, 0.97), w=0.012, color='#3a2c22', v=(0.6, 1.0), a=0.7)),
                                          ('mottle', dict(color='#4e3e30', a=0.35, scale=0.05))]),
                       eye=dict(iris='#c8c0a0')),
    # ===== 동해 (헌화로·삼척·등대섬·청진포구) =====
    # 임연수어 (Pleurogrammus azonus): 쥐노래미과. 뾰족한 머리에 방추형 몸, 머리 뒤부터 꼬리자루까지 이어진 긴 등지느러미(가운데가 살짝 파임),
    # 누런 갈색 몸에 번진 듯한 넓은 흑갈색 가로띠 5개 + 잔 얼룩, 크고 둥근 가슴지느러미, 갈라진 꼬리
    'atkamackerel': dict(type='C', name='임연수어', depth=0.22, width=0.11, tmax=0.38, ped=0.32, mouth=0.1, snout=0.85, scales=110, gape=0.075,
                         fins=[_fin('dorsal', (0.22, 0.84), 0.32, prof=[(0, 0.55), (0.2, 1.0), (0.42, 0.62), (0.6, 0.85), (1, 0.75)]),
                               _fin('anal', (0.52, 0.84), 0.3, prof=[(0, 0.6), (0.2, 1.0), (1, 0.8)]), _fin('caudal', h=0.24, shape='fork'),
                               _fin('pectoral', s=(0.24, 0.24), h=0.17), _fin('pelvic', s=(0.29, 0.29), h=0.08)],
                         colors=P('#3e3422', '#9c8650', '#e8e0c8', silver=0.2, fin_up=('#5a4c30', '#3a3020', 0.85), fin_low=('#a89060', '#c0a878', 0.75),
                                  patterns=[('bands', dict(n=5, u=(0.24, 0.8), w=0.095, color='#2a2216', v=(0.28, 1.0), a=0.55)),
                                            ('mottle', dict(color='#3a2e1c', a=0.35, scale=0.05))]),
                         eye=dict(iris='#d89040'), anim=dict(amp=1.0, freq=1.1)),
    # 도루묵 (Arctoscopus japonicus): 비늘 없는 몸, 거의 세로로 열린 입(아래턱이 나옴), 큰 눈, 날개 같은 큰 가슴지느러미,
    # 멀리 떨어진 등지느러미 둘, 몸 가운데부터 긴 뒷지느러미. 등 쪽 절반은 금갈색 바탕에 잔 물결 같은 흑갈색 점, 옆·배는 은백색
    'sandfish': dict(type='C', name='도루묵', depth=0.22, width=0.11, tmax=0.33, ped=0.36, mouth=0.75, snout=0.5, arch=0.6, scales=0,
                     mouth_slope=-0.5, jaw=0.012, gape=0.05,
                     fins=[_fin('dorsal', (0.27, 0.4), 0.38, prof=[(0, 0.5), (0.3, 1.0), (1, 0.4)], spiny=True), _fin('dorsal', (0.5, 0.78), 0.3, prof=[(0, 0.8), (0.3, 1.0), (1, 0.5)]),
                           _fin('anal', (0.42, 0.82), 0.3, prof=[(0, 0.6), (0.3, 1.0), (1, 0.6)]), _fin('caudal', h=0.22, shape='emarg'),
                           _fin('pectoral', s=(0.23, 0.23), h=0.25), _fin('pelvic', s=(0.27, 0.27), h=0.08)],
                     colors=P('#7a6236', '#d0ccc0', '#f6f4ee', silver=0.6, fin_up=('#b0a080', '#d0c8b0', 0.55), fin_low=('#e0d8c8', '#eee8dc', 0.5),
                              patterns=[('spots', dict(n=170, r=(4, 7), color='#4a3418', v=(0.6, 1.0), a=0.8)),
                                        ('stripe', dict(v=0.86, w=0.12, color='#8a6a34', a=0.45))]),
                     eye=dict(s=0.1, z=0.5, r=0.03, iris='#d8d0b0'), anim=dict(amp=1.0, freq=1.1)),
    # 참가자미 (Pseudopleuronectes herzensteini): 오른눈 가자미. 둥근 마름모꼴 몸에 작은 머리, 눈 있는 쪽은 짙은 갈색에 흐린 얼룩,
    # 지느러미도 몸빛 갈색. 눈 없는 흰 쪽은 등·뒷지느러미 밑동을 따라 노란 띠가 둘러진다 (참가자미 이름표)
    'flatfish': dict(type='F', name='참가자미', flat='right', depth=0.46, snout=0.6, mouth=0.0, ped=0.28,
                     colors=P('#4a3e2a', '#625238', '#f6f4ec', silver=0.0, fin_up=('#5a4a32', '#7a6440', 0.9), fin_low=('#5a4a32', '#7a6440', 0.9),
                              patterns=[('mottle', dict(color='#2e261a', a=0.45, scale=0.05)), ('spots', dict(n=40, r=(3, 6), color='#857050', v=(0.1, 1.0), a=0.3)),
                                        ('stripe', dict(v=0.54, w=0.035, color='#e8c838', a=0.85)), ('stripe', dict(v=0.965, w=0.03, color='#e8c838', a=0.85))]),
                     eye=dict(iris='#b09040', r=0.02)),
    # 살오징어 (Todarodes pacificus): 붉은 갈색 점이 촘촘한 몸, 큰 눈
    'squid': dict(type='S', name='오징어',
                  colors=P('#7a3420', '#b0644a', '#ecd4c4', silver=0.0, fin_up=('#9a5038', '#c88a70', 0.85), fin_low=('#9a5038', '#c88a70', 0.85),
                           patterns=[('spots', dict(n=320, r=(2, 4), color='#5a1e10', v=(0.3, 1.0), a=0.7)), ('stripe', dict(v=0.98, w=0.06, color='#5a2416', a=0.6))]),
                  eye=dict(iris='#202830')),
    # 대문어 (Enteroctopus dofleini): 붉은 갈색, 주름진 피부, 큰 몸
    'giantoctopus': dict(type='O', name='대문어',
                         colors=P('#7a2e1e', '#a84a32', '#e8b8a0', silver=0.0, fin_up=('#a04a34', '#c06a50', 0.9), fin_low=('#a04a34', '#c06a50', 0.9),
                                  patterns=[('mottle', dict(color='#4e1a10', a=0.6, scale=0.04)), ('spots', dict(n=70, r=(2, 5), color='#e0a080', v=(0.2, 1.0), a=0.4))]),
                         eye=dict(iris='#c8a040'), anim=dict(amp=0.25, freq=0.55)),
    # 대구 (Gadus macrocephalus): 몸의 1/4이 넘는 큰 머리, 위턱이 아래턱을 덮는 큰 입(눈 밑까지), 턱수염 하나,
    # 둥근 세모 등지느러미 셋·뒷지느러미 둘, 가슴지느러미보다 앞의 가는 배지느러미. 회갈색 등에 잔 갈색 점이 빽빽, 흰 옆줄, 흰 배
    'cod': dict(type='C', name='대구', depth=0.22, width=0.14, head_w=1.1, tmax=0.3, ped=0.3, mouth=-0.15, snout=0.8, arch=0.55, scales=0, gape=0.095,
                barbels=[(0.03, -0.6, 0.04, 0.003, 'chin')],
                fins=[_fin('dorsal', (0.3, 0.43), 0.34, prof=[(0, 0.5), (0.35, 1.0), (1, 0.35)]), _fin('dorsal', (0.47, 0.63), 0.3, prof=[(0, 0.5), (0.3, 1.0), (1, 0.35)]),
                      _fin('dorsal', (0.67, 0.82), 0.28, prof=[(0, 0.5), (0.3, 1.0), (1, 0.35)]),
                      _fin('anal', (0.45, 0.62), 0.26, prof=[(0, 0.5), (0.3, 1.0), (1, 0.4)]), _fin('anal', (0.66, 0.82), 0.25, prof=[(0, 0.5), (0.3, 1.0), (1, 0.4)]),
                      _fin('caudal', h=0.22, shape='emarg'), _fin('pectoral', s=(0.27, 0.27), h=0.13), _fin('pelvic', s=(0.21, 0.21), h=0.1)],
                colors=P('#5a4c34', '#a09070', '#f0ece0', silver=0.05, fin_up=('#6a5c42', '#8a7a5a', 0.85), fin_low=('#b0a488', '#c8bca0', 0.8),
                         patterns=[('spots', dict(n=380, r=(2, 5), color='#45351e', v=(0.36, 1.0), a=0.8)),
                                   ('stripe', dict(v=0.58, w=0.012, color='#ece4cc', a=0.7))]),
                eye=dict(s=0.1, z=0.42, r=0.019, iris='#c8a040'), anim=dict(amp=0.85, freq=0.8)),
    # 명태 (Gadus chalcogrammus): 대구보다 훨씬 날씬하고 머리가 작다, 아래턱이 위턱보다 나온 입, 큰 눈, 배지느러미 수염 없음.
    # 올리브 갈색 등에 잔 얼룩, 옆구리 위쪽을 따라 들쭉날쭉한 짙은 얼룩 띠, 은백색 배, 짙은 꼬리. 청진 명물
    'pollock': dict(type='C', name='명태', depth=0.165, width=0.1, tmax=0.34, ped=0.3, mouth=0.4, snout=0.9, arch=0.52, scales=0, jaw=0.012, gape=0.075,
                    fins=[_fin('dorsal', (0.3, 0.41), 0.34, prof=[(0, 0.5), (0.35, 1.0), (1, 0.35)]), _fin('dorsal', (0.45, 0.62), 0.3, prof=[(0, 0.5), (0.3, 1.0), (1, 0.35)]),
                          _fin('dorsal', (0.67, 0.82), 0.27, prof=[(0, 0.5), (0.3, 1.0), (1, 0.35)]),
                          _fin('anal', (0.42, 0.62), 0.26, prof=[(0, 0.5), (0.3, 1.0), (1, 0.4)]), _fin('anal', (0.66, 0.82), 0.25, prof=[(0, 0.5), (0.3, 1.0), (1, 0.4)]),
                          _fin('caudal', h=0.22, shape='emarg'), _fin('pectoral', s=(0.27, 0.27), h=0.12), _fin('pelvic', s=(0.22, 0.22), h=0.08)],
                    colors=P('#433c2c', '#a09a88', '#f4f2ec', silver=0.5, fin_up=('#4a4234', '#2e2a22', 0.9), fin_low=('#b8b2a0', '#c8c4b4', 0.7),
                             patterns=[('mottle', dict(color='#3a3226', a=0.3, scale=0.035)),
                                       ('blotchline', dict(v=0.64, color='#3a3226', w=0.05)), ('stripe', dict(v=0.54, w=0.012, color='#e6e2d4', a=0.5))]),
                    eye=dict(s=0.1, z=0.36, r=0.026, iris='#d0ccc0'), anim=dict(amp=0.9, freq=0.9)),
    # 불볼락 (열기, Sebastes thompsoni): 연분홍 주황 몸에 등 쪽 붉은 갈색 안장 얼룩 4~5개, 아주 큰 눈(노란 테),
    # 아래턱이 살짝 나온 입, 높은 가시 등지느러미, 주홍빛 가슴지느러미
    'redrockfish': dict(type='C', name='불볼락', depth=0.32, width=0.13, tmax=0.36, mouth=0.3, snout=0.7, jaw=0.008, scales=60,
                        fins=[_fin('dorsal', (0.26, 0.56), 0.52, prof=[(0, 0.45), (0.25, 1.0), (0.85, 0.6), (1, 0.4)], spiny=True),
                              _fin('dorsal', (0.56, 0.78), 0.48, prof=[(0, 0.75), (0.3, 1.0), (1, 0.45)]),
                              _fin('anal', (0.6, 0.78), 0.45, prof=[(0, 0.5), (0.3, 1.0), (1, 0.5)]), _fin('caudal', h=0.24, shape='emarg'),
                              _fin('pectoral', s=(0.26, 0.26), h=0.16), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                        eye=dict(s=0.12, z=0.42, r=0.034, iris='#e0c040'),
                        colors=P('#b05a3a', '#eaa588', '#f8e6dc', silver=0.25, fin_up=('#d07858', '#eaa888', 0.75), fin_low=('#ec6a3a', '#f4986a', 0.8),
                                 patterns=[('bands', dict(n=5, u=(0.26, 0.86), w=0.075, color='#9a3a20', v=(0.5, 1.0), a=0.6)),
                                           ('mottle', dict(color='#a0482a', a=0.3, scale=0.05))])),
    # ===== 남해 (욕지도·거문도) =====
    # 방어 (Seriola quinqueradiata): 굵고 통통한 방추형(부시리보다 높은 몸, 조금 뭉툭한 머리), 숨은 듯 낮은 가시 등지느러미 뒤로
    # 앞이 솟은 긴 등·뒷지느러미. 등은 청록·올리브, 몸 가운데 흐린 황록색 세로띠, 꼬리·배쪽 지느러미는 누런 올리브.
    # 머리: 노란 테 눈이 입꼬리 바로 위 낮은 곳에 작게, 뒤 끝이 둥근 큰 위턱뼈가 눈 밑까지
    'yellowtail': dict(type='E', name='방어', depth=0.235, width=0.13, tmax=0.4, ped=0.17, snout=0.72, mouth=0.05, gape=0.08,
                       fins=[_fin('dorsal', (0.3, 0.4), 0.16, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)], spiny=True),
                             _fin('dorsal', (0.42, 0.82), 0.4, prof=[(0, 1.0), (0.15, 0.7), (0.35, 0.36), (1, 0.25)]),
                             _fin('anal', (0.58, 0.82), 0.32, prof=[(0, 1.0), (0.2, 0.6), (0.4, 0.35), (1, 0.25)]),
                             _fin('caudal', h=0.34, shape='deepfork', group='low'),
                             _fin('pectoral', s=(0.25, 0.25), h=0.11), _fin('pelvic', s=(0.31, 0.31), h=0.09)],
                       colors=P('#2c5458', '#a8bcbc', '#f4f4f0', silver=0.75, metallic=0.3, fin_up=('#4a6050', '#8a8448', 0.8), fin_low=('#a8a060', '#c8b448', 0.75),
                                patterns=[('stripe', dict(v=0.56, w=0.03, color='#c8c050', a=0.6))]),
                       eye=dict(s=0.1, z=0.18, r=0.019, iris='#d8b040'), anim=dict(amp=1.0, freq=1.2)),
    # 부시리 (Seriola lalandi): 방어보다 길쭉하고 머리가 뾰족하다. 등은 짙은 남청색, 눈에서 꼬리까지 또렷한 샛노란 띠,
    # 샛노란 꼬리. 거문도 갯바위 대물
    'amberjack': dict(type='E', name='부시리', depth=0.195, width=0.115, tmax=0.4, ped=0.16, snout=0.9, mouth=0.08, gape=0.08,
                      fins=[_fin('dorsal', (0.31, 0.41), 0.15, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)], spiny=True),
                            _fin('dorsal', (0.43, 0.82), 0.42, prof=[(0, 1.0), (0.15, 0.7), (0.35, 0.36), (1, 0.25)]),
                            _fin('anal', (0.6, 0.82), 0.34, prof=[(0, 1.0), (0.2, 0.6), (0.4, 0.35), (1, 0.25)]),
                            _fin('caudal', h=0.34, shape='deepfork', group='low'),
                            _fin('pectoral', s=(0.25, 0.25), h=0.09), _fin('pelvic', s=(0.31, 0.31), h=0.1)],
                      colors=P('#22406a', '#9ab0c0', '#f6f6f4', silver=0.75, metallic=0.3, fin_up=('#3a4a5a', '#7a8070', 0.8), fin_low=('#d8b830', '#f0cc30', 0.85),
                               patterns=[('stripe', dict(v=0.57, w=0.04, color='#f4cc28', a=0.95))]),
                      eye=dict(s=0.1, z=0.2, r=0.018, iris='#e0a040'), anim=dict(amp=1.0, freq=1.25)),
    # 갈치 (Trichiurus lepturus): 옆으로 아주 납작한 은빛 띠 모양, 꼬리지느러미 없이 채찍처럼 가늘어진다. 뾰족한 머리에
    # 아래턱이 튀어나온 큰 입(송곳니), 머리에 비해 큰 눈, 머리 바로 뒤부터 꼬리까지 이어진 낮고 투명한 등지느러미
    'hairtail': dict(type='G', name='갈치', depth=0.075, width=0.028, tmax=0.22, body_end=0.99, ped=0.05, snout=0.9, mouth=0.4, xsec=1.3, blunt=1.0, hold=0.0, tail_w=0.32,
                     mouth_h=0.12, jaw=0.012, gape=0.055, teeth=True,
                     fins=[_fin('dorsal', (0.07, 0.97), 0.38, prof=[(0, 0.5), (0.06, 1.0), (0.8, 0.75), (1, 0.15)]),
                           _fin('caudal', h=0.01, shape='point'), _fin('pectoral', s=(0.1, 0.1), h=0.05)],
                     colors=P('#a8b8c4', '#e4eaee', '#f8fafa', silver=1.0, metallic=0.85, fin_up=('#d8dccc', '#c0c8b8', 0.45), fin_low=('#d0dce0', '#e8f0f0', 0.5),
                              patterns=[('sheen', 0.8)]),
                     eye=dict(s=0.05, z=0.4, r=0.014, iris='#e8e0b8'), bones=14, anim=dict(amp=1.2, freq=0.9)),
    # 삼치 (Scomberomorus niphonius): 아주 길쭉한 방추형에 뾰족한 머리·큰 입, 길고 낮은 가시 등지느러미 + 작은 둘째 등지느러미,
    # 토막지느러미. 청회색 등 아래 옆구리에 잔 점이 일곱여덟 줄로 늘어선다 (폭군삼치의 물결 가로줄과 다르다)
    'spanishmackerel': dict(type='E', name='삼치', depth=0.145, width=0.095, tmax=0.42, ped=0.14, snout=0.95, mouth=0.1, gape=0.085, teeth=True,
                            fins=[_fin('dorsal', (0.27, 0.5), 0.26, prof=[(0, 1.0), (0.4, 0.7), (1, 0.25)], spiny=True),
                                  _fin('dorsal', (0.55, 0.62), 0.36, prof=[(0, 1.0), (1, 0.3)]),
                                  _fin('anal', (0.59, 0.66), 0.32, prof=[(0, 1.0), (1, 0.3)]), _fin('caudal', h=0.3, shape='deepfork'),
                                  _fin('pectoral', s=(0.25, 0.25), h=0.11), _fin('pelvic', s=(0.33, 0.33), h=0.06)],
                            colors=P('#34546e', '#b4c4cc', '#f4f6f4', silver=0.8, metallic=0.35, fin_up=('#3a5068', '#283a4c', 0.85), fin_low=('#b8c0c4', '#d0d8dc', 0.6),
                                     patterns=[('spots', dict(n=320, r=(2, 4), color='#1e3044', v=(0.48, 0.92), a=0.8)), ('finlets', True)]),
                            eye=dict(iris='#c8c8b0'), anim=dict(amp=1.1, freq=1.4)),
    # ===== 제주 =====
    # 자리돔 (Chromis notata): 손바닥만 한 자리돔과. 달걀꼴 몸에 작은 머리·작은 입·큰 눈, 깊게 갈라진 꼬리(끝이 뾰족).
    # 등은 올리브 갈색, 옆구리는 은회색 비늘이 반짝이고, 가슴지느러미 밑동에 검은 점, 등지느러미 뒤 끝 밑에 흰 점
    'damselfish': dict(type='D', name='자리돔', depth=0.36, width=0.13, tmax=0.42, snout=0.8, blunt=0.9, mouth=0.1, ped=0.3, scales=36,
                       fins=[_fin('dorsal', (0.3, 0.78), 0.3, prof=[(0, 0.5), (0.2, 1.0), (0.75, 0.8), (1, 1.15)], spiny=True),
                             _fin('anal', (0.6, 0.8), 0.32, prof=[(0, 0.5), (0.4, 1.0), (1, 1.1)]), _fin('caudal', h=0.32, shape='deepfork'),
                             _fin('pectoral', s=(0.3, 0.3), h=0.17), _fin('pelvic', s=(0.34, 0.34), h=0.13)],
                       colors=P('#3a3e30', '#7a8070', '#c8ccc4', silver=0.45, fin_up=('#3a3e34', '#2a2e28', 0.8), fin_low=('#5a6058', '#7a8078', 0.7),
                                patterns=[('spots', dict(n=1, r=(8, 8), u=(0.66, 0.66), v=(0.88, 0.88), color='#f0f2f0', a=0.9)),
                                          ('spots', dict(n=1, r=(6, 6), u=(0.3, 0.3), v=(0.4, 0.4), color='#101214', a=0.9)), ('scale_edge', 0.6)]),
                       eye=dict(r=0.026, iris='#8a9a70'), anim=dict(amp=1.0, freq=1.4)),
    # 옥돔 (Branchiostegus japonicus): 말머리처럼 거의 수직으로 떨어지는 이마, 머리 꼭대기 가까이 붙은 큰 눈, 낮게 달린 작은 입,
    # 길쭉한 몸에 머리 뒤부터 꼬리까지 이어진 낮은 등지느러미. 분홍빛 붉은 몸, 눈 뒤 아래 은빛 삼각 무늬, 꼬리에 노란 줄.
    # 입 선은 머리 밑에서 거의 수평으로 눈 앞 아래까지 (위로 휘지 않는다)
    'tilefish': dict(type='C', name='옥돔', depth=0.25, width=0.12, tmax=0.28, snout=0.25, blunt=0.6, mouth=-0.45, arch=0.7, hump=0.05, hump_s=0.1, scales=70,
                     mouth_slope=0.15, gape=0.05, maxilla=0.6,
                     fins=[_fin('dorsal', (0.24, 0.84), 0.26, prof=[(0, 0.6), (0.1, 1.0), (1, 0.85)]),
                           _fin('anal', (0.5, 0.84), 0.26, prof=[(0, 0.6), (0.2, 1.0), (1, 0.85)]), _fin('caudal', h=0.22, shape='truncate'),
                           _fin('pectoral', s=(0.24, 0.24), h=0.16), _fin('pelvic', s=(0.28, 0.28), h=0.1)],
                     colors=P('#c0505a', '#e88a8a', '#f6e0dc', silver=0.3, fin_up=('#e0a0a0', '#f0c840', 0.75), fin_low=('#e8a0a0', '#f0c8b0', 0.75),
                              patterns=[('hstripes', dict(n=4, v=(0.55, 0.9), w=0.01, color='#f0d060', a=0.5)),
                                        ('spots', dict(n=1, r=(7, 7), u=(0.12, 0.12), v=(0.48, 0.48), color='#f4f0ec', a=0.85))]),
                     eye=dict(s=0.075, z=0.6, r=0.03, iris='#e0d0a0'), anim=dict(amp=0.9, freq=1.0)),
    # 무늬오징어 (Sepioteuthis lessoniana): 외투막을 두르는 넓은 지느러미, 반투명한 갈색 얼룩. 에깅의 왕
    'bigfinsquid': dict(type='Q', name='무늬오징어', mantle_w=0.82, fin_w=1.7, tentacles=True,
                        colors=P('#7a5a46', '#b8957a', '#efe2d6', silver=0.05, fin_up=('#c8b098', '#ead8c4', 0.75), fin_low=('#c8b098', '#ead8c4', 0.75),
                                 patterns=[('mottle', dict(color='#5a3e2c', a=0.45, scale=0.05)), ('spots', dict(n=120, r=(1, 3), color='#4a2e20', v=(0.4, 1.0), a=0.5))]),
                        eye=dict(iris='#40c0c8')),
    # ===== 서해 (영흥도·태안) =====
    # 점농어 (Lateolabrax maculatus): 농어처럼 길쭉한 몸에 아래턱이 나온 큰 입(눈 밑까지), 붙어 있는 가시·연한 등지느러미,
    # 거의 검은 꼬리. 등·옆구리 위쪽과 등지느러미에 검은 점이 또렷하게 흩어진다
    'spottedseabass': dict(type='C', name='점농어', depth=0.235, width=0.12, mouth=0.3, snout=0.9, jaw=0.008, gape=0.085, scales=80,
                           fins=[_fin('dorsal', (0.27, 0.52), 0.48, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.45), (1, 0.35)], spiny=True),
                                 _fin('dorsal', (0.53, 0.76), 0.42, prof=[(0, 0.7), (0.3, 1.0), (1, 0.45)]),
                                 _fin('anal', (0.62, 0.77), 0.4, prof=[(0, 0.5), (0.3, 1.0), (1, 0.45)]), _fin('caudal', h=0.24, shape='emarg'),
                                 _fin('pectoral', s=(0.26, 0.26), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                           colors=P('#4a5a5a', '#b8c0bc', '#f2f2ee', silver=0.6, fin_up=('#4a5658', '#22282a', 0.85), fin_low=('#c8ccc8', '#d8dcd8', 0.6),
                                    patterns=[('spots', dict(n=130, r=(3, 6), color='#14181a', v=(0.5, 1.0), a=0.9)), ('finspots', '#1a2022')]),
                           eye=dict(iris='#d0c8a0')),
    # ===== 한강 추가 (원작 2013 공략: 은어·떡붕어) =====
    # 은어 (Plecoglossus altivelis): 날씬한 원통형 몸에 뾰족한 머리, 눈 뒤까지 닿는 입, 아주 잔 비늘(매끈해 보임),
    # 몸 가운데의 높은 등지느러미 + 기름지느러미. 올리브 갈색 등, 금빛 도는 옆구리, 가슴지느러미 위 노란 점, 지느러미 끝이 노랗다
    'sweetfish': dict(type='B', name='은어', depth=0.17, width=0.1, tmax=0.4, ped=0.36, snout=0.9, mouth=0.05, gape=0.075, scales=0,
                      fins=[_fin('dorsal', (0.38, 0.5), 0.8, prof=[(0, 0.6), (0.2, 1.0), (1, 0.45)]), _fin('adipose', (0.72, 0.77), 0.2),
                            _fin('anal', (0.62, 0.78), 0.5, prof=[(0, 1.0), (1, 0.5)]), _fin('caudal', h=0.24, shape='fork'),
                            _fin('pectoral', s=(0.22, 0.22), h=0.11), _fin('pelvic', s=(0.45, 0.45), h=0.09)],
                      colors=P('#4a4a2c', '#b4ac84', '#f2eee0', silver=0.6, fin_up=('#6a6a48', '#c8b048', 0.7), fin_low=('#d0b850', '#e8d070', 0.75),
                               patterns=[('spots', dict(n=1, r=(10, 13), u=(0.27, 0.27), v=(0.6, 0.6), color='#f0c830', a=0.9)), ('sheen', 0.5)]),
                      eye=dict(iris='#d8d0a0'), anim=dict(amp=1.1, freq=1.3)),
    # 떡붕어 (Carassius cuvieri): 붕어보다 체고가 훨씬 높고 머리는 작다 — 머리 뒤에서 등이 가파르게 솟아 활처럼 굽는다.
    # 은빛이 강한 놋쇠빛 비늘, 회색 반투명 지느러미
    'gengorobuna': dict(type='A', name='떡붕어', depth=0.4, tmax=0.4, arch=0.68, snout=0.75, ped=0.36, scales=30, mouth=0.1,
                        fins=[_fin('dorsal', (0.38, 0.74), 0.45, prof=[(0, 0.35), (0.12, 1.0), (0.4, 0.55), (1, 0.4)]),
                              _fin('anal', (0.72, 0.82), 0.45, prof=[(0, 0.5), (0.2, 1.0), (1, 0.45)]),
                              _fin('caudal', h=0.26, shape='fork'), _fin('pectoral', s=(0.24, 0.24), h=0.13), _fin('pelvic', s=(0.5, 0.5), h=0.1)],
                        colors=P('#5a5a44', '#c0b898', '#eeeadc', silver=0.6, fin_up=('#7a7a68', '#9a9a88', 0.6), fin_low=('#9a9a88', '#b0b0a0', 0.55),
                                 patterns=[('scale_edge', 0.55), ('sheen', 0.4)]),
                        eye=dict(r=0.023, iris='#d8d0b0')),
    # ===== 괴물 어종 (원작 이벤트 몬스터: 괴물잉어 · 괴물가물치 · 폭군삼치 · 황새치) =====
    # 괴물잉어: 잉어(Cyprinus carpio) 그대로의 몸 — 도톰한 입술·입꼬리 긴 수염 + 윗입술 짧은 수염, 긴 등지느러미 — 를 더 굵게.
    # 이끼 낀 듯 검푸른 등, 큰 비늘마다 금빛 테, 붉은 배쪽 지느러미
    'monstercarp': dict(type='A', name='괴물잉어', depth=0.3, width=0.15, arch=0.64, scales=30, mouth=-0.08, lips=2.4,
                        barbels=[(0.035, -0.3, 0.06, 0.006, 'down'), (0.02, -0.05, 0.03, 0.004, 'side')],
                        colors=P('#1e2618', '#6a5a2a', '#c8b47a', silver=0.2, metallic=0.25, fin_up=('#2a2618', '#1a1810', 0.9), fin_low=('#a83a1a', '#d8401a', 0.9),
                                 patterns=[('scale_edge', 1.0), ('mottle', dict(color='#0e140a', a=0.35, scale=0.05))]),
                        eye=dict(r=0.02, iris='#e8b020')),
    # 괴물가물치: 가물치(Channa argus) 몸 — 뱀 머리처럼 납작하고 긴 머리, 눈 뒤까지 찢어진 큰 입, 원통형 몸, 긴 등·뒷지느러미,
    # 둥근 꼬리 — 에 거의 검은 바탕과 더 짙은 얼룩 두 줄, 눈 뒤로 뻗는 검은 줄 두 개, 붉은 눈
    'monstersnakehead': dict(type='H', name='괴물가물치', depth=0.155, width=0.14, head_w=1.05, head_flat=0.15, tmax=0.4, body_end=0.83, ped=0.75, snout=0.7, mouth=0.15,
                             gape=0.075, jaw=0.008, scales=70,
                             fins=[_fin('dorsal', (0.3, 0.98), 0.34, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                   _fin('anal', (0.55, 0.98), 0.32, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                   _fin('caudal', h=0.17, shape='round'), _fin('pectoral', s=(0.22, 0.22), h=0.11), _fin('pelvic', s=(0.31, 0.31), h=0.07)],
                             colors=P('#121410', '#3e3e2a', '#8a8670', silver=0.0, fin_up=('#1a1c14', '#2a2c20', 0.95), fin_low=('#2a2c20', '#3a3c2c', 0.95),
                                      patterns=[('blotches', dict(color='#050604', a=0.75)), ('mottle', dict(color='#070806', a=0.5, scale=0.045)),
                                                ('eyeline', dict(color='#050604', w=0.025, n=2))]),
                             eye=dict(s=0.07, z=0.45, r=0.014, iris='#d02018')),
    # 폭군삼치: 동갈삼치(Scomberomorus commerson)를 닮은 큰 삼치 — 삼치보다 굵고 등이 검푸르다. 점 대신 옆구리를 가로지르는
    # 가늘고 구불구불한 짙은 가로줄 여럿, 높은 낫 모양 둘째 등·뒷지느러미, 큰 초승달 꼬리, 붉은 홍채
    'tyrantmackerel': dict(type='E', name='폭군삼치', depth=0.17, width=0.11, tmax=0.42, ped=0.13, snout=0.92, mouth=0.12, gape=0.075, jaw=0.006,
                           fins=[_fin('dorsal', (0.27, 0.48), 0.3, prof=[(0, 1.0), (0.4, 0.7), (1, 0.25)], spiny=True),
                                 _fin('dorsal', (0.54, 0.62), 0.55, prof=[(0, 1.0), (1, 0.2)]),
                                 _fin('anal', (0.58, 0.66), 0.5, prof=[(0, 1.0), (1, 0.2)]), _fin('caudal', h=0.38, shape='deepfork'),
                                 _fin('pectoral', s=(0.25, 0.25), h=0.13), _fin('pelvic', s=(0.33, 0.33), h=0.06)],
                           colors=P('#14243a', '#7a8ea0', '#e8ecee', silver=0.85, metallic=0.45, fin_up=('#1e2e44', '#34465a', 0.8), fin_low=('#9aa4ac', '#c0c8cc', 0.6),
                                    patterns=[('bands', dict(n=24, u=(0.24, 0.92), w=0.014, color='#0a1420', v=(0.4, 1.0), a=0.75)), ('finlets', True)]),
                           eye=dict(r=0.026, iris='#c83020'), anim=dict(amp=1.1, freq=1.5)),
    # 황새치 (Xiphias gladius): 칼 같은 위턱, 높은 낫 모양 등지느러미, 초승달 꼬리, 보랏빛 검은 등
    'swordfish': dict(type='E', name='황새치', depth=0.15, width=0.12, tmax=0.33, body_end=0.82, ped=0.1, snout=0.9, mouth=0.0, bill=0.3,
                      fins=[_fin('dorsal', (0.16, 0.3), 1.6, prof=[(0, 1.0), (0.25, 0.85), (1, 0.08)]),
                            _fin('anal', (0.6, 0.68), 0.4, prof=[(0, 1.0), (1, 0.2)]),
                            _fin('caudal', h=0.42, shape='deepfork'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.18, long=True)],
                      colors=P('#22182e', '#5a5068', '#d0ccd4', silver=0.4, metallic=0.4, fin_up=('#241a30', '#1a1222', 0.95), fin_low=('#3a3048', '#2a2234', 0.9),
                               patterns=[('sheen', 0.4)]),
                      eye=dict(s=0.1, z=0.22, r=0.024, iris='#28385a'), anim=dict(amp=1.0, freq=1.2)),
    # ===== 해외: 하롱베이 =====
    # 바라문디 (Lates calcarifer): 눈 위가 오목하게 꺼진 이마(오목한 머리 윤곽)에 뾰족한 주둥이, 아래턱이 나온 큰 입(눈 뒤까지),
    # 등이 솟은 몸에 굵은 꼬리자루, 둥근 꼬리. 높은 가시 등지느러미와 둥근 연한 등지느러미가 붙어 있다. 은회색 몸, 짙은 꼬리, 붉게 빛나는 눈
    'barramundi': dict(type='C', name='바라문디', depth=0.28, width=0.13, tmax=0.4, ped=0.46, snout=0.9, mouth=0.4, arch=0.64, hump=-0.05, hump_s=0.11,
                       jaw=0.01, gape=0.1, scales=55,
                       fins=[_fin('dorsal', (0.31, 0.53), 0.55, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.45), (1, 0.35)], spiny=True),
                             _fin('dorsal', (0.54, 0.75), 0.5, prof=[(0, 0.8), (0.3, 1.0), (1, 0.55)]),
                             _fin('anal', (0.63, 0.77), 0.45, prof=[(0, 0.6), (0.4, 1.0), (1, 0.6)]), _fin('caudal', h=0.24, shape='round'),
                             _fin('pectoral', s=(0.26, 0.26), h=0.13), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                       colors=P('#4e5a58', '#c0c8c4', '#f0f0ea', silver=0.7, fin_up=('#5a605a', '#2e322e', 0.9), fin_low=('#a8a08a', '#6a6a5e', 0.8),
                                patterns=[('scale_edge', 0.5)]),
                       eye=dict(s=0.1, z=0.45, iris='#d04020'), anim=dict(amp=1.0, freq=1.0)),
    # 얼룩바리 (Epinephelus coioides): 두툼하고 길쭉한 몸에 큰 머리, 아래턱이 나온 큰 입, 이어진 가시·연한 등지느러미, 둥근 꼬리.
    # 올리브 갈색 바탕에 주황 갈색 잔점이 빽빽하고(지느러미까지), 희미한 짙은 빗금 띠 다섯 줄
    'orangegrouper': dict(type='C', name='얼룩바리', depth=0.27, width=0.16, tmax=0.38, snout=0.85, mouth=0.3, jaw=0.012, gape=0.1, lips=1.3, scales=0,
                          fins=[_fin('dorsal', (0.3, 0.8), 0.32, prof=[(0, 0.6), (0.15, 1.0), (0.6, 0.8), (1, 1.1)], spiny=True),
                                _fin('anal', (0.62, 0.8), 0.34, prof=[(0, 0.5), (0.4, 1.0), (1, 0.8)]), _fin('caudal', h=0.22, shape='round'),
                                _fin('pectoral', s=(0.26, 0.26), h=0.16), _fin('pelvic', s=(0.3, 0.3), h=0.13)],
                          colors=P('#4e5236', '#8e8a60', '#dcd6b8', silver=0.05, fin_up=('#5a5a40', '#3a3a28', 0.9), fin_low=('#6a6a50', '#4a4a38', 0.9),
                                   patterns=[('obliquebands', dict(n=5, color='#2e301e', a=0.35)), ('spots', dict(n=520, r=(2, 4), color='#c46a28', v=(0.05, 1.0), a=0.9)),
                                             ('finspots', '#b0602a')]),
                          eye=dict(iris='#d0c060')),
    # 맹그로브잭 (Lutjanus argentimaculatus): 퉁돔형(높지 않은 길쭉한 몸), 뾰족한 주둥이에 큰 입, 구릿빛 붉은 갈색 몸에
    # 비늘마다 가운데가 짙어 비늘 줄이 선처럼 보인다, 붉은 지느러미, 살짝 파인 꼬리
    'mangrovejack': dict(type='D', name='맹그로브잭', depth=0.32, width=0.13, tmax=0.38, snout=0.8, mouth=0.15, ped=0.38, gape=0.08, scales=45,
                         fins=[_fin('dorsal', (0.3, 0.58), 0.36, prof=[(0, 0.5), (0.2, 1.0), (1, 0.6)], spiny=True),
                               _fin('dorsal', (0.58, 0.8), 0.34, prof=[(0, 0.8), (0.4, 1.0), (1, 0.55)]),
                               _fin('anal', (0.63, 0.8), 0.34, prof=[(0, 0.6), (0.3, 1.0), (1, 0.55)], spiny=True),
                               _fin('caudal', h=0.24, shape='emarg'), _fin('pectoral', s=(0.29, 0.29), h=0.17), _fin('pelvic', s=(0.33, 0.33), h=0.12)],
                         colors=P('#5e2a20', '#b0503a', '#e0b8a0', silver=0.3, fin_up=('#8a3a28', '#b04a30', 0.85), fin_low=('#c0503a', '#d86a48', 0.8),
                                  patterns=[('scale_edge', 0.6), ('scale_dots', dict(color='#5a2418', a=0.5))]),
                         eye=dict(iris='#d8a040'), anim=dict(amp=0.9, freq=1.0)),
    # 자이언트 트레발리 (Caranx ignobilis): 가파르게 떨어지는 뭉툭한 이마, 높고 납작한 몸, 낫처럼 길게 솟은 둘째 등·뒷지느러미,
    # 낫 모양 긴 가슴지느러미, 가는 꼬리자루의 비늘판, 큰 초승달 꼬리. 짙은 은회색에 잔 검은 점
    'gianttrevally': dict(type='D', name='자이언트 트레발리', depth=0.36, width=0.13, tmax=0.38, snout=0.5, blunt=0.75, mouth=-0.05, arch=0.64, ped=0.12, gape=0.07, scales=0,
                          fins=[_fin('dorsal', (0.3, 0.44), 0.26, prof=[(0, 0.6), (0.3, 1.0), (1, 0.4)], spiny=True),
                                _fin('dorsal', (0.47, 0.8), 0.6, prof=[(0, 1.0), (0.15, 0.6), (0.35, 0.3), (1, 0.2)]),
                                _fin('anal', (0.56, 0.8), 0.52, prof=[(0, 1.0), (0.15, 0.6), (0.35, 0.3), (1, 0.2)]),
                                _fin('caudal', h=0.4, shape='deepfork'), _fin('pectoral', s=(0.28, 0.28), h=0.26, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.08)],
                          colors=P('#363c44', '#9aa2a8', '#e0e2e2', silver=0.7, metallic=0.35, fin_up=('#2a2e34', '#1a1e22', 0.9), fin_low=('#5a6066', '#3a4046', 0.9),
                                   patterns=[('scutes', dict(color='#6a7278', a=0.7)), ('spots', dict(n=70, r=(1, 2.5), color='#1a1e22', v=(0.4, 0.95), a=0.55))]),
                          eye=dict(s=0.1, z=0.42, r=0.024, iris='#a87040'), anim=dict(amp=1.0, freq=1.3)),
    # 꼬치고기 (Sphyraena jello): 창 같은 아주 길쭉한 몸, 뾰족한 머리에 아래턱이 튀어나온 큰 입(이빨), 멀리 떨어진 작은 등지느러미 둘,
    # 은빛 몸 위쪽 절반에 꺾인(<) 모양 짙은 빗금 줄 여럿, 노르스름한 꼬리
    'barracuda': dict(type='E', name='꼬치고기', depth=0.11, width=0.09, tmax=0.45, body_end=0.85, ped=0.3, snout=0.95, mouth=0.15,
                      jaw=0.015, gape=0.09, teeth=True,
                      fins=[_fin('dorsal', (0.36, 0.43), 0.55, prof=[(0, 1.0), (1, 0.3)], spiny=True), _fin('dorsal', (0.68, 0.74), 0.45, prof=[(0, 1.0), (1, 0.4)]),
                            _fin('anal', (0.69, 0.75), 0.4, prof=[(0, 1.0), (1, 0.4)]), _fin('caudal', h=0.22, shape='fork', group='low'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.08), _fin('pelvic', s=(0.4, 0.4), h=0.07)],
                      colors=P('#3e4e58', '#b8c4c8', '#f0f2f2', silver=0.85, metallic=0.3, fin_up=('#4a5a62', '#6a7a82', 0.75), fin_low=('#c0c0a0', '#d8c870', 0.7),
                               patterns=[('obliquebands', dict(n=20, color='#2a3238', a=0.6))]),
                      eye=dict(s=0.12, z=0.38, r=0.016, iris='#d8d0b0'), anim=dict(amp=1.1, freq=1.2)),
    # ===== 해외: 산토리니 =====
    # 귀족도미 (Sparus aurata): 달걀꼴 도미형 은회색 몸, 둥글게 가파른 이마에 낮게 달린 작은 입, 두 눈 사이 이마의 금빛 띠,
    # 아가미 뚜껑 위쪽 끝의 큰 검은 얼룩(그 아래 붉은 기), 옆구리의 가는 회색 세로줄, 끝이 검은 꼬리
    'giltheadbream': dict(type='D', name='귀족도미', depth=0.37, width=0.12, tmax=0.4, snout=0.55, mouth=-0.25, arch=0.64, scales=55,
                          fins=[_fin('dorsal', (0.3, 0.62), 0.34, prof=[(0, 0.45), (0.18, 1.0), (1, 0.6)], spiny=True),
                                _fin('dorsal', (0.62, 0.8), 0.3, prof=[(0, 0.75), (1, 0.55)]),
                                _fin('anal', (0.62, 0.8), 0.3, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                                _fin('caudal', h=0.26, shape='fork'), _fin('pectoral', s=(0.3, 0.3), h=0.2, long=True), _fin('pelvic', s=(0.35, 0.35), h=0.12)],
                          colors=P('#55646e', '#b8c4cc', '#eef0f0', silver=0.75, metallic=0.25, fin_up=('#6a7a84', '#2e363e', 0.85), fin_low=('#a8b0b8', '#c0c8d0', 0.7),
                                   patterns=[('hstripes', dict(n=7, v=(0.3, 0.85), w=0.007, color='#5a6a74', a=0.55)),
                                             ('spots', dict(n=1, r=(16, 16), u=(0.07, 0.07), v=(0.86, 0.86), color='#e8b830', a=0.9)),
                                             ('spots', dict(n=1, r=(11, 11), u=(0.205, 0.205), v=(0.7, 0.7), color='#14181c', a=0.9)),
                                             ('spots', dict(n=1, r=(7, 7), u=(0.2, 0.2), v=(0.56, 0.56), color='#c05050', a=0.5))]),
                          eye=dict(iris='#d8c070')),
    # 유럽농어 (Dicentrarchus labrax): 농어처럼 길쭉한 은빛 몸, 큰 입, 회청색 등, 아가미 뚜껑 위 끝의 흐린 검은 점, 떨어진 등지느러미 둘
    'europeanseabass': dict(type='C', name='유럽농어', depth=0.22, width=0.12, mouth=0.25, snout=0.88, gape=0.09, jaw=0.008, scales=80,
                            fins=[_fin('dorsal', (0.28, 0.47), 0.45, prof=[(0, 0.45), (0.3, 1.0), (1, 0.35)], spiny=True),
                                  _fin('dorsal', (0.53, 0.76), 0.42, prof=[(0, 0.7), (0.3, 1.0), (1, 0.45)]),
                                  _fin('anal', (0.62, 0.77), 0.4, prof=[(0, 0.5), (0.3, 1.0), (1, 0.45)]), _fin('caudal', h=0.24, shape='emarg'),
                                  _fin('pectoral', s=(0.26, 0.26), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                            colors=P('#4e5e68', '#c4ccd0', '#f4f6f6', silver=0.8, metallic=0.2, fin_up=('#606e74', '#44525a', 0.8), fin_low=('#c8ccc8', '#d8dcd8', 0.6),
                                     patterns=[('spots', dict(n=1, r=(9, 9), u=(0.205, 0.205), v=(0.7, 0.7), color='#2a3238', a=0.75))]),
                            eye=dict(r=0.023, iris='#d8d4c0')),
    # 유럽대구 (Merluccius merluccius): 길쭉한 몸에 큰 머리, 아래턱이 나온 큰 입(이빨), 큰 눈. 짧은 세모 첫 등지느러미와
    # 가운데가 오목한 긴 둘째 등지느러미·뒷지느러미, 각진 꼬리. 짙은 쇳빛 회색 등, 은백색 배
    'hake': dict(type='C', name='유럽대구', depth=0.165, width=0.1, tmax=0.32, ped=0.32, snout=0.92, mouth=0.3, jaw=0.012, gape=0.09, teeth=True, scales=90,
                 fins=[_fin('dorsal', (0.27, 0.36), 0.55, prof=[(0, 0.6), (0.25, 1.0), (1, 0.25)]),
                       _fin('dorsal', (0.4, 0.83), 0.4, prof=[(0, 0.7), (0.1, 0.9), (0.45, 0.55), (0.8, 1.0), (1, 0.6)]),
                       _fin('anal', (0.44, 0.83), 0.36, prof=[(0, 0.6), (0.1, 0.9), (0.45, 0.55), (0.8, 1.0), (1, 0.6)]), _fin('caudal', h=0.2, shape='truncate'),
                       _fin('pectoral', s=(0.24, 0.24), h=0.14), _fin('pelvic', s=(0.22, 0.22), h=0.1)],
                 colors=P('#3e4a56', '#a8b0b8', '#eef0f2', silver=0.6, fin_up=('#4a5664', '#2e3844', 0.85), fin_low=('#b0b8c0', '#c8d0d8', 0.6),
                          patterns=[('sheen', 0.4)]),
                 eye=dict(s=0.1, z=0.45, r=0.026, iris='#d8c070')),
    # 지중해곰치 (Muraena helena): 굵은 장어형 몸에 가슴지느러미가 없다. 머리 뒤부터 솟은 두툼한 등지느러미, 눈 뒤까지 찢어진 입.
    # 짙은 흑갈색 바탕에 크림빛 노란 점이 빽빽 (작은 점이 모여 얼룩을 이룬다). 작은 눈은 입 앞쪽 위에, 홍채는 연한 하늘빛 은색
    'moray': dict(type='G', name='지중해곰치', depth=0.09, width=0.07, snout=0.65, mouth=0.1, mouth_h=0.08, gape=0.06, jaw=0.006, teeth=True,
                  fins=[_fin('dorsal', (0.14, 1.0), 0.3, prof=[(0, 0.15), (0.1, 0.7), (0.55, 1.0), (1, 0.9)]),
                        _fin('anal', (0.45, 1.0), 0.3, prof=[(0, 0.15), (0.15, 0.75), (0.55, 1.0), (1, 0.9)]),
                        _fin('caudal', h=0.05, shape='point')],
                  colors=P('#2a2016', '#3e3022', '#5a4a36', silver=0.0, fin_up=('#3a2e20', '#2a2014', 0.95), fin_low=('#3a2e20', '#2a2014', 0.95),
                           patterns=[('spots', dict(n=480, r=(3, 6), color='#e4cc70', v=(0.0, 1.0), a=0.85)),
                                     ('mottle', dict(color='#1a140e', a=0.35, scale=0.05))]),
                  eye=dict(r=0.009, iris='#b8c8d8')),
    # 참다랑어 (Thunnus thynnus): 굵은 방추형(몸 가운데가 가장 높은 물방울 몸), 원뿔 머리, 짧은 가슴지느러미(둘째 등지느러미에 못 미침),
    # 낫 모양 둘째 등·뒷지느러미, 노란 토막지느러미, 아주 가는 꼬리자루와 초승달 꼬리. 짙은 남색 등, 은빛 배에 희미한 흰 가로줄·점 줄.
    # 머리: 은백색 홍채에 큰 동공의 눈이 입 선보다 꽤 위에, 입꼬리는 눈 앞 아래까지
    'bluefintuna': dict(type='E', name='참다랑어', depth=0.26, width=0.18, tmax=0.42, ped=0.08, snout=0.92, mouth=0.05, gape=0.07,
                        fins=[_fin('dorsal', (0.3, 0.44), 0.36, prof=[(0, 1.0), (0.3, 0.6), (1, 0.15)], spiny=True), _fin('dorsal', (0.5, 0.6), 0.6, prof=[(0, 1.0), (0.4, 0.5), (1, 0.15)]),
                              _fin('anal', (0.56, 0.65), 0.52, prof=[(0, 1.0), (0.4, 0.5), (1, 0.15)]), _fin('caudal', h=0.46, shape='deepfork'),
                              _fin('pectoral', s=(0.26, 0.26), h=0.11), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                        colors=P('#0e1838', '#6e7e90', '#e8ecee', silver=0.8, metallic=0.45, fin_up=('#141e3a', '#0a1028', 0.9), fin_low=('#8a8c88', '#b0aa80', 0.75),
                                 patterns=[('finlets', True), ('sheen', 0.5),
                                           ('bands', dict(n=16, u=(0.3, 0.86), w=0.012, color='#f4f6f6', v=(0.12, 0.42), a=0.35))]),
                        eye=dict(s=0.1, z=0.36, r=0.02, iris='#c8ccd0', pupil=1.15), anim=dict(amp=0.9, freq=1.6)),
    # ===== 해외: 나일강 =====
    # 나일농어 (Lates niloticus): 바라문디와 같은 꼴 — 눈 위가 오목한 이마, 뾰족한 주둥이에 아래턱이 나온 큰 입 — 이지만 등이 더 높이
    # 솟은 거대한 몸, 높은 가시 등지느러미, 굵은 꼬리자루에 둥근 꼬리. 은회색에 푸르스름한 등, 노란 테 두른 검은 눈
    'nileperch': dict(type='C', name='나일농어', depth=0.3, width=0.14, tmax=0.42, ped=0.46, snout=0.88, mouth=0.4, arch=0.68, hump=-0.05, hump_s=0.11,
                      jaw=0.01, gape=0.1, scales=55,
                      fins=[_fin('dorsal', (0.3, 0.5), 0.6, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.45), (1, 0.35)], spiny=True),
                            _fin('dorsal', (0.52, 0.76), 0.5, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)]),
                            _fin('anal', (0.63, 0.77), 0.45, prof=[(0, 0.6), (0.4, 1.0), (1, 0.6)]), _fin('caudal', h=0.26, shape='round'),
                            _fin('pectoral', s=(0.26, 0.26), h=0.13), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                      colors=P('#56606a', '#b4b8b0', '#ecebe4', silver=0.6, fin_up=('#5a5e5a', '#3a3e3a', 0.85), fin_low=('#9a9480', '#7a7466', 0.75),
                               patterns=[('scale_edge', 0.45)]),
                      eye=dict(s=0.1, z=0.45, iris='#e0b020')),
    # 나일틸라피아 (Oreochromis niloticus): 높고 옆으로 납작한 몸, 두툼한 입술의 작은 입, 머리 뒤부터 꼬리자루까지 이어진 긴 가시 등지느러미
    # (뒤쪽 연한 부분이 뾰족), 잘린 꼬리. 회녹색 몸에 짙은 세로 띠 8~9개, 큰 비늘 테, 지느러미 끝과 꼬리가 붉다
    'tilapia': dict(type='D', name='나일틸라피아', depth=0.36, width=0.14, tmax=0.4, snout=0.8, blunt=0.9, mouth=0.05, lips=1.6, ped=0.42, scales=32,
                    fins=[_fin('dorsal', (0.28, 0.82), 0.25, prof=[(0, 0.5), (0.15, 1.0), (0.65, 0.9), (0.85, 1.4), (1, 1.0)], spiny=True),
                          _fin('anal', (0.64, 0.82), 0.34, prof=[(0, 0.5), (0.5, 1.0), (1, 1.2)], spiny=True), _fin('caudal', h=0.26, shape='truncate'),
                          _fin('pectoral', s=(0.3, 0.3), h=0.17, long=True), _fin('pelvic', s=(0.33, 0.33), h=0.14)],
                    colors=P('#3e4a40', '#8a9488', '#dcdcd0', silver=0.35, fin_up=('#4a5450', '#b04838', 0.8), fin_low=('#5a6058', '#a05040', 0.8),
                             patterns=[('bands', dict(n=9, u=(0.22, 0.9), w=0.035, color='#2e3630', v=(0.2, 1.0), a=0.45)), ('scale_edge', 0.6)]),
                    eye=dict(iris='#c06030')),
    # 아프리카메기 (Clarias gariepinus): 위아래로 납작하고 뼈판으로 덮인 넓은 머리, 길쭉한 몸, 가시 없이 머리 뒤부터 꼬리 앞까지 이어진
    # 긴 등지느러미·뒷지느러미, 둥근 꼬리, 수염 네 쌍(위턱 수염이 가장 길다). 올리브 회갈색 바탕에 대리석 같은 짙은 얼룩, 흰 배
    'africancatfish': dict(type='H', name='아프리카메기', depth=0.12, width=0.13, head_w=1.3, head_flat=0.25, tmax=0.32, body_end=0.88, ped=0.7, snout=0.45, scales=0,
                           lips=1.4, gape=0.06,
                           fins=[_fin('dorsal', (0.24, 0.86), 0.28, prof=[(0, 0.5), (0.08, 1.0), (1, 0.9)]),
                                 _fin('anal', (0.44, 0.86), 0.3, prof=[(0, 0.5), (0.08, 1.0), (1, 0.9)]),
                                 _fin('caudal', h=0.14, shape='round'), _fin('pectoral', s=(0.15, 0.15), h=0.1), _fin('pelvic', s=(0.42, 0.42), h=0.06)],
                           barbels=[(0.02, -0.05, 0.16, 0.0038, 'side'), (0.012, 0.25, 0.07, 0.0026, 'up'), (0.03, -0.35, 0.08, 0.0026, 'down'), (0.035, -0.5, 0.06, 0.0026, 'down')],
                           colors=P('#3a3a2c', '#6e6a52', '#d8d4c4', silver=0.0, fin_up=('#3a3a2e', '#2a2a22', 0.95), fin_low=('#4a4a3c', '#3a3a2e', 0.95),
                                    patterns=[('mottle', dict(color='#1e1e16', a=0.6, scale=0.05)), ('mottle', dict(color='#8a8668', a=0.35, scale=0.035))]),
                           eye=dict(iris='#a08040')),
    # 타이거피시 (Hydrocynus forskahlii, 나일 타이거피시): 길쭉한 은빛 몸(H. vittatus보다 날씬), 뾰족한 머리에 입술 밖으로 드러난 송곳니,
    # 몸 가운데 높은 등지느러미 + 기름지느러미, 깊게 갈라진 꼬리. 등은 올리브 은회색, 옆구리를 따라 가는 검은 세로줄 여러 개,
    # 지느러미는 붉은 주황, 꼬리 끝 가장자리는 검다
    'tigerfish': dict(type='B', name='타이거피시', depth=0.2, width=0.1, tmax=0.4, ped=0.32, snout=0.85, mouth=0.15, gape=0.07, teeth=True, scales=44,
                      fins=[_fin('dorsal', (0.42, 0.52), 0.7, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]), _fin('adipose', (0.75, 0.79), 0.18),
                            _fin('anal', (0.63, 0.78), 0.42, prof=[(0, 1.0), (1, 0.4)]), _fin('caudal', h=0.32, shape='deepfork', group='low'),
                            _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.46, 0.46), h=0.1)],
                      colors=P('#4e5a50', '#c4cac4', '#f2f4f2', silver=0.85, metallic=0.25, fin_up=('#7a7060', '#c05a3a', 0.8), fin_low=('#d04a2a', '#5a2016', 0.85),
                               patterns=[('hstripes', dict(n=8, v=(0.3, 0.88), w=0.008, color='#1a1e20', a=0.8))]),
                      eye=dict(iris='#e0c040'), anim=dict(amp=1.1, freq=1.4)),
}

# 잡동사니 (Unity에서 모양을 코드로 만드는 대신 여기서 같이 모델링한다)
JUNK = ['can', 'trap', 'box', 'fishcan', 'robotfish', 'shoe']


# 2026-10-19부터 늘어난 어종: 지역별 파일 (docs/fish-db). 각 파일은 SPECIES = {...}를 내놓는다 (P·_fin은 여기서 가져간다)
for _mod in ('species_korea2', 'species_sea_fresh', 'species_sea', 'species_med', 'species_med2', 'species_korea3', 'species_sea2', 'species_africa'):
    try:
        SPECIES.update(__import__(_mod).SPECIES)
    except ImportError:
        pass


def resolve(sid):
    """틀 + 어종 덮어쓰기 → 완성된 파라미터."""
    sp = SPECIES[sid]
    t = TEMPLATES[sp['type']]
    d = {k: (list(v) if isinstance(v, list) else (dict(v) if isinstance(v, dict) else v)) for k, v in t.items()}
    for k, v in sp.items():
        if k in ('eye', 'anim') and isinstance(v, dict):
            d[k] = dict(d.get(k, {}), **v)
        else:
            d[k] = v
    d['id'] = sid
    d.setdefault('head_w', 1.0)
    d.setdefault('barbels', [])
    d.setdefault('bones', 6)
    if not d.get('ceph'):
        # 척추 마디를 촘촘히: 어종 표의 뼈 수(6 = 예전 기본)를 1.7배로, 적어도 12개 (척추 11 + 꼬리).
        # 마디가 5개뿐이면 몸이 몇 토막으로 꺾여 보였다. 장어형 14 → 24
        d['bones'] = max(12, round(d['bones'] * 1.7))
    d.setdefault('flat', None)
    d.setdefault('beak', 0.0)
    d.setdefault('bill', 0.0)
    return d
