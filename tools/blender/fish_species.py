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
  eye     : s(위치), z(머리 높이 비율 -1~1), r(반지름/길이), iris(홍채 색)
  barbels : (s, z, 길이, 굵기, 방향) 수염
  colors  : back/side/belly, 무늬 patterns, fin_up/fin_low (지느러미 색: 밑동, 끝, 알파)
  anim    : amp(꼬리 흔드는 폭 배율), freq(빈도 배율) — Unity에서 재생 속도로 쓴다
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
    'crucian': dict(type='A', name='붕어', depth=0.33, tmax=0.42, scales=30, mouth=-0.05,
                    colors=P('#3e4a2c', '#9a8a4e', '#d8cfa0', silver=0.25, fin_up=('#4a4a34', '#2e2e22', 0.85), fin_low=('#7a6a44', '#5a4a30', 0.8),
                             patterns=[('scale_edge', 0.5)]),
                    eye=dict(iris='#c8a040')),
    'carp': dict(type='A', name='잉어', depth=0.27, scales=36,
                 barbels=[(0.03, -0.25, 0.04, 0.004, 'down'), (0.05, -0.15, 0.025, 0.003, 'side')],
                 colors=P('#3c4228', '#a9884a', '#e2d6a2', silver=0.15, fin_up=('#4c4630', '#3a3424', 0.85), fin_low=('#a8643a', '#c4502a', 0.85),
                          patterns=[('scale_edge', 0.75)]),
                 eye=dict(iris='#d8a838')),
    'goldkoi': dict(type='A', name='황금 비단잉어', depth=0.27, scales=36,
                    barbels=[(0.03, -0.25, 0.04, 0.004, 'down'), (0.05, -0.15, 0.025, 0.003, 'side')],
                    colors=P('#e0901a', '#f6b832', '#ffe08a', silver=0.6, metallic=0.7, fin_up=('#f0a830', '#ffe4a0', 0.75), fin_low=('#f0a830', '#ffe8b0', 0.75),
                             patterns=[('scale_edge', 0.4), ('sheen', 0.6)]),
                    eye=dict(iris='#202020')),
    'nuchi': dict(type='A', name='누치', depth=0.21, width=0.12, tmax=0.38, mouth=-0.4, snout=0.75, scales=48,
                  fins=[_fin('dorsal', (0.38, 0.5), 0.62, prof=[(0, 0.5), (0.15, 1.0), (1, 0.35)]),
                        _fin('anal', (0.7, 0.8), 0.45), _fin('caudal', h=0.24, shape='deepfork'),
                        _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.5, 0.5), h=0.09)],
                  barbels=[(0.04, -0.4, 0.03, 0.003, 'down')],
                  colors=P('#5e6460', '#b6b8ac', '#ecebe2', silver=0.6, fin_up=('#7a7a70', '#9a9a90', 0.7), fin_low=('#b0a890', '#d0c8b0', 0.7),
                           patterns=[('spots', dict(n=9, r=(6, 9), color='#3a3e3a', v=(0.45, 0.6), u=(0.25, 0.85), row=True))]),
                  eye=dict(iris='#c0c0b0')),
    # ===== B. 날씬한 잉어형 =====
    'zacco': dict(type='B', name='피라미', depth=0.22,
                  fins=[_fin('dorsal', (0.47, 0.58), 0.6), _fin('anal', (0.64, 0.8), 0.7, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)]),
                        _fin('caudal', h=0.26, shape='deepfork'), _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.5, 0.5), h=0.1)],
                  colors=P('#4a6a72', '#b8c8c8', '#f0f0ea', silver=0.75, fin_up=('#8aa0a0', '#c0d0d0', 0.6), fin_low=('#d8b850', '#e8a860', 0.75),
                           patterns=[('bands', dict(n=10, u=(0.3, 0.88), w=0.035, color='#4a7c8c', v=(0.15, 0.85), a=0.55)),
                                     ('bands', dict(n=10, u=(0.33, 0.91), w=0.02, color='#e88a8a', v=(0.2, 0.75), a=0.35))]),
                  eye=dict(iris='#e8c070')),
    'kkeuri': dict(type='B', name='끄리', depth=0.22, mouth=0.15, snout=0.8,
                   colors=P('#3a5864', '#b0bcc0', '#eef0ec', silver=0.75, fin_up=('#6a7a80', '#8a9aa0', 0.65), fin_low=('#d88a5a', '#e8a070', 0.75),
                            patterns=[('stripe', dict(v=0.55, w=0.05, color='#3a5a7a', a=0.35)), ('mouthline', 0.8)]),
                   eye=dict(iris='#d0c0a0')),
    'skygager': dict(type='B', name='강준치', depth=0.22, width=0.08, mouth=0.6, snout=0.85, arch=0.45, scales=70,
                     fins=[_fin('dorsal', (0.5, 0.6), 0.55), _fin('anal', (0.6, 0.8), 0.4, prof=[(0, 1.0), (1, 0.4)]),
                           _fin('caudal', h=0.26, shape='deepfork'), _fin('pectoral', s=(0.22, 0.22), h=0.13), _fin('pelvic', s=(0.48, 0.48), h=0.09)],
                     colors=P('#566a74', '#c8d0d4', '#f4f4f2', silver=0.85, fin_up=('#90a0a8', '#b0c0c8', 0.6), fin_low=('#d0b0a0', '#e0c0b0', 0.6)),
                     eye=dict(iris='#e0e0d0')),
    'mullet': dict(type='B', name='숭어', depth=0.2, width=0.13, snout=0.6, mouth=-0.05, scales=40, xsec=2.2,
                   fins=[_fin('dorsal', (0.34, 0.42), 0.35, prof=[(0, 1.0), (1, 0.3)], spiny=True), _fin('dorsal', (0.58, 0.66), 0.38),
                         _fin('anal', (0.6, 0.7), 0.35), _fin('caudal', h=0.25, shape='emarg'),
                         _fin('pectoral', s=(0.22, 0.32), h=0.1), _fin('pelvic', s=(0.36, 0.36), h=0.08)],
                   colors=P('#3e505a', '#9aa8ae', '#e8ecec', silver=0.7, fin_up=('#5a6a72', '#7a8a92', 0.7), fin_low=('#8a9aa0', '#aab4b8', 0.6),
                            patterns=[('hstripes', dict(n=6, v=(0.45, 0.85), w=0.012, color='#3a4a54', a=0.45))]),
                   eye=dict(iris='#c0b080')),
    # ===== C. 농어형 =====
    'mandarin': dict(type='C', name='쏘가리', depth=0.28, mouth=0.25, snout=0.8, scales=90,
                     colors=P('#6a5a30', '#c8aa5a', '#ece0b0', silver=0.1, fin_up=('#8a7a40', '#6a5a30', 0.9), fin_low=('#b09a58', '#c8b070', 0.85),
                              patterns=[('rosettes', dict(n=46, r=(8, 16), color='#2e2814', v=(0.25, 1.0))), ('eyeline', dict(color='#2e2814', w=0.03)),
                                        ('finspots', '#3a3018')]),
                     eye=dict(iris='#d8a020')),
    'bass': dict(type='C', name='배스', depth=0.26, mouth=0.2, snout=0.85, scales=65,
                 fins=[_fin('dorsal', (0.27, 0.5), 0.4, prof=[(0, 0.5), (0.3, 1.0), (1, 0.3)], spiny=True),
                       _fin('dorsal', (0.5, 0.76), 0.5, prof=[(0, 0.6), (0.3, 1.0), (1, 0.45)]),
                       _fin('anal', (0.6, 0.78), 0.42), _fin('caudal', h=0.24, shape='emarg'),
                       _fin('pectoral', s=(0.26, 0.26), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.11)],
                 colors=P('#2f4422', '#7a8a4a', '#e6e2c0', silver=0.15, fin_up=('#4a5a30', '#3a4a26', 0.85), fin_low=('#7a8050', '#8a9060', 0.8),
                          patterns=[('blotchline', dict(v=0.55, color='#1e2a14', w=0.08)), ('mottle', dict(color='#3a4a24', a=0.4))]),
                 eye=dict(iris='#b03020')),
    'bluegill': dict(type='C', name='블루길', depth=0.44, width=0.12, tmax=0.42, mouth=0.0, snout=0.5, xsec=1.6, arch=0.55, scales=45,
                     fins=[_fin('dorsal', (0.3, 0.55), 0.3, spiny=True), _fin('dorsal', (0.55, 0.8), 0.36),
                           _fin('anal', (0.55, 0.8), 0.32), _fin('caudal', h=0.22, shape='emarg'),
                           _fin('pectoral', s=(0.3, 0.3), h=0.18, long=True), _fin('pelvic', s=(0.33, 0.33), h=0.1)],
                     colors=P('#2e4a4e', '#6a8a7a', '#d8b860', silver=0.15, fin_up=('#3a5050', '#2a3a3a', 0.8), fin_low=('#7a7a5a', '#8a8a6a', 0.75),
                              patterns=[('bands', dict(n=7, u=(0.25, 0.85), w=0.06, color='#283a3c', v=(0.3, 1.0), a=0.5)),
                                        ('earflap', '#101820'), ('throat', '#d89040')]),
                     eye=dict(iris='#202020')),
    'seabass': dict(type='C', name='농어', depth=0.23, width=0.12, mouth=0.25, snout=0.85, scales=80,
                    colors=P('#4a5a68', '#b4bec4', '#f0f2f0', silver=0.8, fin_up=('#6a7a84', '#4a5a64', 0.75), fin_low=('#c0c4c4', '#d0d4d4', 0.6),
                             patterns=[('spots', dict(n=30, r=(2, 4), color='#2a323a', v=(0.7, 1.0), u=(0.2, 0.7)))]),
                    eye=dict(iris='#d8d0b0')),
    'rockfish': dict(type='C', name='볼락', depth=0.3, mouth=0.25, eye=dict(s=0.12, z=0.4, r=0.03, iris='#c09040'),
                     colors=P('#3a2e26', '#7a5e46', '#c8a888', silver=0.1, fin_up=('#4a3a30', '#3a2e26', 0.85), fin_low=('#6a5040', '#7a6050', 0.85),
                              patterns=[('bands', dict(n=5, u=(0.3, 0.85), w=0.07, color='#2a201a', v=(0.35, 1.0), a=0.5)), ('mottle', dict(color='#5a4232', a=0.3))])),
    'scorpionfish': dict(type='C', name='쏨뱅이', depth=0.3, width=0.16, head_w=1.15, mouth=0.15, snout=0.65,
                         eye=dict(s=0.12, z=0.45, r=0.025, iris='#d06030'),
                         colors=P('#7a3020', '#b8583a', '#e8b49a', silver=0.0, fin_up=('#a04a30', '#d07050', 0.85), fin_low=('#c06040', '#d88060', 0.85),
                                  patterns=[('mottle', dict(color='#5a1e14', a=0.6, scale=0.07)), ('spots', dict(n=40, r=(3, 6), color='#f0d0b0', v=(0.2, 0.9), a=0.6)),
                                            ('finspots', '#6a2014')])),
    'jacopever': dict(type='C', name='우럭', depth=0.29, width=0.15, mouth=0.3, snout=0.7,
                      eye=dict(s=0.12, z=0.42, r=0.026, iris='#b08040'),
                      colors=P('#2e2e2a', '#5e5c52', '#b8b4a4', silver=0.05, fin_up=('#3a3a34', '#2a2a26', 0.9), fin_low=('#4a4a42', '#3a3a34', 0.85),
                               patterns=[('mottle', dict(color='#22221e', a=0.55, scale=0.06)), ('eyeline', dict(color='#1a1a18', w=0.025, n=3))])),
    'greenling': dict(type='C', name='쥐노래미', depth=0.22, width=0.12, mouth=0.0, snout=0.75, scales=95,
                      fins=[_fin('dorsal', (0.24, 0.55), 0.35, spiny=True), _fin('dorsal', (0.55, 0.82), 0.4),
                            _fin('anal', (0.5, 0.82), 0.35), _fin('caudal', h=0.22, shape='truncate'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                      colors=P('#5a4a30', '#8a7650', '#d0c4a0', silver=0.05, fin_up=('#6a5a3a', '#5a4a2e', 0.85), fin_low=('#7a6a48', '#8a7a58', 0.8),
                               patterns=[('mottle', dict(color='#3a2e1e', a=0.55, scale=0.05)), ('stripe', dict(v=0.8, w=0.02, color='#c8b890', a=0.4))]),
                      eye=dict(iris='#a08050')),
    'giantgrouper': dict(type='C', name='자이언트 그루퍼', depth=0.3, width=0.17, head_w=1.1, mouth=0.25, snout=0.75, scales=110,
                         fins=[_fin('dorsal', (0.28, 0.58), 0.3, spiny=True), _fin('dorsal', (0.58, 0.8), 0.4),
                               _fin('anal', (0.62, 0.8), 0.4), _fin('caudal', h=0.22, shape='round'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.16), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                         colors=P('#2e2a22', '#5e5240', '#a49478', silver=0.05, fin_up=('#3e382c', '#2a261e', 0.9), fin_low=('#4e4636', '#3a3428', 0.9),
                                  patterns=[('obliquebands', dict(n=6, color='#1e1a14', a=0.55)), ('mottle', dict(color='#3a3226', a=0.4)),
                                            ('spots', dict(n=60, r=(2, 4), color='#c8b898', v=(0.2, 0.9), a=0.35))]),
                         eye=dict(s=0.1, z=0.42, r=0.015, iris='#806040'), anim=dict(amp=0.8, freq=0.7)),
    # 다금바리 (Niphon spinosus): 길쭉한 농어형 몸, 등은 자갈색, 배는 은백색, 아가미 뚜껑 가시, 어릴 때 몸 가운데 검은 세로띠 흔적
    'dageumbari': dict(type='C', name='다금바리', depth=0.26, width=0.15, head_w=1.05, mouth=0.27, snout=0.8, scales=100,
                       fins=[_fin('dorsal', (0.27, 0.56), 0.32, spiny=True), _fin('dorsal', (0.57, 0.78), 0.34),
                             _fin('anal', (0.62, 0.78), 0.32), _fin('caudal', h=0.2, shape='fork'),
                             _fin('pectoral', s=(0.27, 0.27), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                       colors=P('#4a3a34', '#8a7060', '#e0d8cc', silver=0.25, fin_up=('#5a463c', '#3a2c26', 0.9), fin_low=('#7a6658', '#5a4a40', 0.9),
                                patterns=[('stripe', dict(v=0.5, w=0.05, color='#2a201c', a=0.35)), ('mottle', dict(color='#3a2e28', a=0.25))]),
                       eye=dict(s=0.1, z=0.42, r=0.016, iris='#a08050'), anim=dict(amp=0.8, freq=0.75)),
    # 돗돔 (Stereolepis doederleini): 높고 두툼한 거대한 몸, 짙은 회흑색, 배는 조금 밝다, 둥근 꼬리
    'dotdom': dict(type='C', name='돗돔', depth=0.34, width=0.18, head_w=1.12, mouth=0.26, snout=0.75, scales=120,
                   fins=[_fin('dorsal', (0.28, 0.55), 0.28, spiny=True), _fin('dorsal', (0.56, 0.78), 0.36),
                         _fin('anal', (0.62, 0.79), 0.36), _fin('caudal', h=0.24, shape='round'),
                         _fin('pectoral', s=(0.27, 0.27), h=0.17), _fin('pelvic', s=(0.31, 0.31), h=0.13)],
                   colors=P('#24262a', '#44484c', '#8a8c88', silver=0.1, fin_up=('#2a2c30', '#1a1c1e', 0.95), fin_low=('#3a3c40', '#26282a', 0.95),
                            patterns=[('mottle', dict(color='#1a1c20', a=0.35))]),
                   eye=dict(s=0.09, z=0.43, r=0.015, iris='#707060'), anim=dict(amp=0.7, freq=0.6)),
    # ===== D. 도미형 =====
    'blackseabream': dict(type='D', name='감성돔', depth=0.4,
                          colors=P('#2e3438', '#8a9298', '#dcdfe0', silver=0.65, fin_up=('#3a4044', '#2a3034', 0.85), fin_low=('#4a5054', '#3a4044', 0.8),
                                   patterns=[('bands', dict(n=6, u=(0.25, 0.8), w=0.05, color='#3a4248', v=(0.3, 1.0), a=0.25)),
                                             ('hstripes', dict(n=8, v=(0.25, 0.8), w=0.008, color='#5a6268', a=0.22))]),
                          eye=dict(iris='#c0b080')),
    'redseabream': dict(type='D', name='참돔', depth=0.38, width=0.12, mouth=-0.15,
                        colors=P('#c0464a', '#e88a86', '#f6e4dc', silver=0.4, fin_up=('#d06a6a', '#e89090', 0.75), fin_low=('#e8a0a0', '#f0c0c0', 0.7),
                                 patterns=[('spots', dict(n=90, r=(2, 4), color='#5ac8f0', v=(0.4, 1.0), u=(0.1, 0.85), a=0.85))]),
                        eye=dict(iris='#e0b040')),
    'beakfish': dict(type='D', name='돌돔', depth=0.44, width=0.13, mouth=-0.1, snout=0.45,
                     colors=P('#4a4e50', '#9ea2a0', '#d8dad4', silver=0.25, fin_up=('#2a2e30', '#1e2224', 0.9), fin_low=('#3a3e40', '#2a2e30', 0.9),
                              patterns=[('bands', dict(n=7, u=(0.08, 0.9), w=0.075, color='#16181a', v=(0.0, 1.0), a=0.9, taper=True))]),
                     eye=dict(iris='#c0b090')),
    'opaleye': dict(type='D', name='벵에돔', depth=0.38, width=0.13, snout=0.4, mouth=-0.05,
                    fins=[_fin('dorsal', (0.32, 0.6), 0.3, spiny=True), _fin('dorsal', (0.6, 0.8), 0.32),
                          _fin('anal', (0.62, 0.8), 0.3), _fin('caudal', h=0.24, shape='emarg'),
                          _fin('pectoral', s=(0.3, 0.3), h=0.15), _fin('pelvic', s=(0.36, 0.36), h=0.11)],
                    colors=P('#1e2a28', '#3a4a46', '#6a7a74', silver=0.15, fin_up=('#1e2624', '#141a18', 0.9), fin_low=('#2a3432', '#1e2624', 0.9),
                             patterns=[('scale_dots', dict(color='#101614', a=0.5))]),
                    eye=dict(iris='#4a90b0')),
    'filefish': dict(type='D', name='쥐치', depth=0.5, width=0.1, tmax=0.45, body_end=0.82, snout=0.8, mouth=0.1, xsec=1.6, scales=0,
                     fins=[_fin('dorsal', (0.22, 0.25), 0.45, prof=[(0, 1.0), (1, 0.9)], spine=True),
                           _fin('dorsal', (0.52, 0.8), 0.25, prof=[(0, 0.7), (0.3, 1.0), (1, 0.6)]),
                           _fin('anal', (0.52, 0.8), 0.25, prof=[(0, 0.7), (0.3, 1.0), (1, 0.6)]),
                           _fin('caudal', h=0.2, shape='round'), _fin('pectoral', s=(0.28, 0.28), h=0.08)],
                     eye=dict(s=0.2, z=0.5, r=0.025, iris='#80a040'),
                     colors=P('#6a6a4a', '#a4a07a', '#d8d4b8', silver=0.05, fin_up=('#c0b890', '#d8d0a8', 0.6), fin_low=('#c0b890', '#d8d0a8', 0.6),
                              patterns=[('mottle', dict(color='#4a4a32', a=0.5, scale=0.05)), ('hstripes', dict(n=5, v=(0.3, 0.8), w=0.015, color='#5a5a40', a=0.4))])),
    # ===== E. 유선형 =====
    'mackerel': dict(type='E', name='고등어',
                     colors=P('#1e5a5a', '#9ab8b8', '#f2f4f2', silver=0.8, metallic=0.35, fin_up=('#3a5a5a', '#5a7a7a', 0.7), fin_low=('#9aaaaa', '#b8c4c4', 0.6),
                              patterns=[('waves', dict(color='#0e2a32', v=(0.62, 1.0), a=0.9)), ('finlets', True)]),
                     eye=dict(iris='#202a2a')),
    'horsemackerel': dict(type='E', name='전갱이', depth=0.22, width=0.11,
                          colors=P('#4a6a6e', '#b8c8c4', '#f2f2ee', silver=0.85, metallic=0.3, fin_up=('#6a8080', '#8aa0a0', 0.65), fin_low=('#d8d0a0', '#e0d8b0', 0.6),
                                   patterns=[('scutes', dict(color='#8a9a96', a=0.7)), ('earflap', '#1a2224')]),
                          eye=dict(r=0.026, iris='#d8d0b0')),
    'halfbeak': dict(type='E', name='학꽁치', depth=0.1, width=0.08, tmax=0.5, body_end=0.86, ped=0.4, snout=0.95, beak=0.12,
                     fins=[_fin('dorsal', (0.72, 0.84), 0.45), _fin('anal', (0.7, 0.84), 0.45),
                           _fin('caudal', h=0.13, shape='fork'), _fin('pectoral', s=(0.24, 0.24), h=0.07), _fin('pelvic', s=(0.6, 0.6), h=0.05)],
                     colors=P('#4a7a7a', '#c0d8d4', '#f6f8f4', silver=0.85, metallic=0.3, fin_up=('#8aa0a0', '#a0b8b8', 0.55), fin_low=('#b8c8c8', '#d0dcdc', 0.5),
                              patterns=[('stripe', dict(v=0.55, w=0.05, color='#d0e8f0', a=0.6))]),
                     eye=dict(s=0.16, z=0.3, r=0.012, iris='#d0d0c0'), anim=dict(amp=1.2, freq=1.3)),
    # ===== F. 납작한 고기 =====
    'flounder': dict(type='F', name='광어', flat='left', depth=0.42, mouth=0.1, snout=0.6,
                     colors=P('#4a4030', '#6e6248', '#f2f0e8', silver=0.0, fin_up=('#5a4e3a', '#4a4030', 0.9), fin_low=('#6a5e48', '#5a4e3a', 0.9),
                              patterns=[('mottle', dict(color='#2e281e', a=0.5, scale=0.05)), ('spots', dict(n=50, r=(3, 7), color='#d8ccb0', v=(0.1, 1.0), a=0.35))]),
                     eye=dict(iris='#c0a050', r=0.022)),
    'marbledflounder': dict(type='F', name='문치가자미', flat='right', depth=0.45, snout=0.5, mouth=0.0, ped=0.3,
                            colors=P('#4a4234', '#6a604c', '#f4f2ec', silver=0.0, fin_up=('#5a5040', '#4a4234', 0.9), fin_low=('#5a5040', '#4a4234', 0.9),
                                     patterns=[('spots', dict(n=40, r=(5, 11), color='#2a241c', v=(0.05, 1.0), a=0.55)), ('mottle', dict(color='#3a3428', a=0.3))]),
                            eye=dict(iris='#a08040', r=0.02)),
    # ===== G. 장어형 =====
    'eel': dict(type='G', name='뱀장어',
                colors=P('#2a3020', '#4e5636', '#d8d4a8', silver=0.1, fin_up=('#3a4228', '#2a3020', 0.9), fin_low=('#c0bc98', '#a8a480', 0.85)),
                eye=dict(iris='#a0a080')),
    'conger': dict(type='G', name='붕장어', depth=0.075, mouth=0.1,
                   colors=P('#4a463c', '#8a8270', '#ece8dc', silver=0.1, fin_up=('#3a362e', '#1a1814', 0.9), fin_low=('#d8d4c8', '#2a2620', 0.85),
                            patterns=[('pores', dict(color='#f8f6ee', v=0.55, n=40)), ('pores', dict(color='#f8f6ee', v=0.85, n=24))]),
                   eye=dict(r=0.009, iris='#c0b090')),
    # ===== H. 메기형 =====
    'catfish': dict(type='H', name='메기',
                    barbels=[(0.03, 0.1, 0.12, 0.004, 'side'), (0.025, -0.35, 0.05, 0.003, 'down')],
                    colors=P('#2a2a20', '#5a5640', '#d8d4bc', silver=0.0, fin_up=('#3a3a2c', '#2a2a20', 0.9), fin_low=('#4a4a38', '#3a3a2c', 0.9),
                             patterns=[('mottle', dict(color='#1e1e16', a=0.6, scale=0.06))]),
                    eye=dict(iris='#a09060')),
    'bagrid': dict(type='H', name='동자개', depth=0.2, width=0.13, head_w=1.15, tmax=0.3, body_end=0.82,
                   fins=[_fin('dorsal', (0.3, 0.38), 0.55, prof=[(0, 1.0), (1, 0.6)], spine=True),
                         _fin('adipose', (0.66, 0.8), 0.18, prof=[(0, 0.4), (0.4, 1.0), (1, 0.5)]),
                         _fin('anal', (0.62, 0.8), 0.32), _fin('caudal', h=0.22, shape='fork'),
                         _fin('pectoral', s=(0.17, 0.17), h=0.14, spine=True), _fin('pelvic', s=(0.42, 0.42), h=0.08)],
                   barbels=[(0.03, 0.15, 0.06, 0.003, 'side'), (0.035, 0.35, 0.035, 0.002, 'up'),
                            (0.025, -0.3, 0.04, 0.002, 'down'), (0.03, -0.4, 0.03, 0.002, 'down')],
                   colors=P('#5a4a1e', '#c8a040', '#f0dc8a', silver=0.0, fin_up=('#a08030', '#6a5420', 0.85), fin_low=('#c8a040', '#a08030', 0.85),
                            patterns=[('blotches', dict(color='#2e2410', a=0.75))]),
                   eye=dict(iris='#c0a040')),
    'snakehead': dict(type='H', name='가물치', depth=0.15, width=0.13, head_w=1.0, tmax=0.4, body_end=0.83, ped=0.75, snout=0.65, mouth=0.1, scales=70,
                      fins=[_fin('dorsal', (0.3, 0.98), 0.32, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                            _fin('anal', (0.55, 0.98), 0.3, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                            _fin('caudal', h=0.15, shape='round'), _fin('pectoral', s=(0.24, 0.24), h=0.11), _fin('pelvic', s=(0.33, 0.33), h=0.07)],
                      colors=P('#2e3424', '#6a6c4a', '#d8d6c0', silver=0.0, fin_up=('#3a3e2c', '#4a4e38', 0.9), fin_low=('#4a4e38', '#5a5e48', 0.9),
                               patterns=[('blotchrows', dict(color='#16180e', a=0.8)), ('eyeline', dict(color='#16180e', w=0.025, n=2))]),
                      eye=dict(s=0.07, z=0.4, r=0.012, iris='#c0a050')),
    'smelt': dict(type='B', name='빙어', depth=0.15, width=0.09, tmax=0.45, snout=0.85, mouth=0.25, scales=60,
                  fins=[_fin('dorsal', (0.45, 0.53), 0.6), _fin('adipose', (0.74, 0.78), 0.2), _fin('anal', (0.62, 0.78), 0.45),
                        _fin('caudal', h=0.22, shape='fork'), _fin('pectoral', s=(0.2, 0.2), h=0.1), _fin('pelvic', s=(0.45, 0.45), h=0.07)],
                  colors=P('#7a9a98', '#d8e4e0', '#f6f8f4', silver=0.85, metallic=0.25, fin_up=('#c8d4d4', '#e4ecec', 0.45), fin_low=('#d8e0e0', '#eef2f2', 0.4),
                           patterns=[('stripe', dict(v=0.55, w=0.035, color='#9ab0c8', a=0.7))]),
                  eye=dict(r=0.026, iris='#d0d8d8'), anim=dict(amp=1.0, freq=1.4)),
    # ===== 서천 앞바다 (서해) =====
    'goby': dict(type='C', name='망둥어', depth=0.16, width=0.14, head_w=1.25, tmax=0.28, ped=0.55, snout=0.5, mouth=0.1, xsec=2.1, scales=50,
                 fins=[_fin('dorsal', (0.3, 0.42), 0.45, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)], spiny=True), _fin('dorsal', (0.48, 0.76), 0.5),
                       _fin('anal', (0.55, 0.76), 0.4), _fin('caudal', h=0.2, shape='round'),
                       _fin('pectoral', s=(0.2, 0.2), h=0.14), _fin('pelvic', s=(0.25, 0.25), h=0.08)],
                 colors=P('#5e5440', '#9a8a68', '#e0d8c0', silver=0.05, fin_up=('#7a6c50', '#5a5040', 0.85), fin_low=('#a09070', '#b0a080', 0.8),
                          patterns=[('mottle', dict(color='#3e3628', a=0.45, scale=0.06)),
                                    ('spots', dict(n=8, r=(7, 11), color='#3a3226', v=(0.5, 0.5), u=(0.25, 0.9), row=True, a=0.7))]),
                 eye=dict(s=0.1, z=0.6, r=0.02, iris='#c0a060'), anim=dict(amp=0.9, freq=0.9)),
    'shad': dict(type='B', name='전어', depth=0.3, width=0.1, tmax=0.4, snout=0.7, mouth=-0.1, scales=50,
                 fins=[_fin('dorsal', (0.42, 0.52), 0.55, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)]), _fin('anal', (0.65, 0.8), 0.3),
                       _fin('caudal', h=0.3, shape='deepfork'), _fin('pectoral', s=(0.22, 0.22), h=0.1), _fin('pelvic', s=(0.48, 0.48), h=0.07)],
                 colors=P('#2e4a5a', '#b8c4c8', '#f4f4f0', silver=0.9, metallic=0.3, fin_up=('#8aa0a8', '#b0c0c8', 0.6), fin_low=('#c8d0d0', '#dde4e4', 0.55),
                          patterns=[('spots', dict(n=1, r=(9, 11), color='#1a2228', v=(0.66, 0.66), u=(0.26, 0.26), row=True)),
                                    ('hstripes', dict(n=6, v=(0.6, 0.9), w=0.008, color='#2a3a44', a=0.6))]),
                 eye=dict(iris='#e0d8c0'), anim=dict(amp=1.0, freq=1.3)),
    'whitecroaker': dict(type='C', name='백조기', depth=0.27, width=0.13, tmax=0.35, arch=0.58, snout=0.55, mouth=-0.2, scales=55,
                         fins=[_fin('dorsal', (0.3, 0.45), 0.45, spiny=True), _fin('dorsal', (0.45, 0.82), 0.32),
                               _fin('anal', (0.68, 0.8), 0.25), _fin('caudal', h=0.22, shape='round'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                         colors=P('#7a7a70', '#d4d0c0', '#f6f2e6', silver=0.6, metallic=0.2, fin_up=('#a8a090', '#c8c0a8', 0.7), fin_low=('#e0c880', '#e8d8a0', 0.7),
                                  patterns=[('sheen', 0.5)]),
                         eye=dict(iris='#d8c890'), anim=dict(amp=0.9, freq=0.9)),
    'croaker': dict(type='C', name='민어', depth=0.26, width=0.14, tmax=0.36, arch=0.58, snout=0.6, mouth=-0.1, scales=60,
                    fins=[_fin('dorsal', (0.3, 0.46), 0.42, spiny=True), _fin('dorsal', (0.46, 0.82), 0.3),
                          _fin('anal', (0.68, 0.8), 0.25), _fin('caudal', h=0.22, shape='round'),
                          _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                    colors=P('#4a4a42', '#8c8a7a', '#d8d4c4', silver=0.35, fin_up=('#5a5850', '#4a4840', 0.85), fin_low=('#7a7468', '#8a8478', 0.8),
                             patterns=[('mottle', dict(color='#3a3a32', a=0.25, scale=0.08))]),
                    eye=dict(iris='#c0a868'), anim=dict(amp=0.8, freq=0.7)),
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
    # 임연수어 (Pleurogrammus azonus): 쥐노래미과. 누런 몸에 검은 가로띠 5개, 꼬리 깊게 갈라짐
    'atkamackerel': dict(type='C', name='임연수어', depth=0.2, width=0.11, mouth=0.05, snout=0.8, scales=110,
                         fins=[_fin('dorsal', (0.22, 0.84), 0.3, prof=[(0, 0.6), (0.3, 1.0), (0.55, 0.7), (1, 0.8)]),
                               _fin('anal', (0.5, 0.84), 0.3), _fin('caudal', h=0.24, shape='deepfork'),
                               _fin('pectoral', s=(0.24, 0.24), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.08)],
                         colors=P('#4a4a2a', '#b0a050', '#ece4c0', silver=0.2, fin_up=('#5a5430', '#3a361e', 0.85), fin_low=('#b0a060', '#c8b880', 0.75),
                                  patterns=[('bands', dict(n=5, u=(0.22, 0.78), w=0.06, color='#22221a', v=(0.35, 1.0), a=0.75))]),
                         eye=dict(iris='#c0b060'), anim=dict(amp=1.0, freq=1.1)),
    # 도루묵 (Arctoscopus japonicus): 비늘 없는 몸, 위로 열린 큰 입, 등은 갈색 얼룩, 배는 은백색
    'sandfish': dict(type='C', name='도루묵', depth=0.22, width=0.11, tmax=0.35, mouth=0.65, snout=0.55, arch=0.6, scales=0,
                     fins=[_fin('dorsal', (0.25, 0.4), 0.35, spiny=True), _fin('dorsal', (0.44, 0.78), 0.3),
                           _fin('anal', (0.42, 0.8), 0.32), _fin('caudal', h=0.2, shape='truncate'),
                           _fin('pectoral', s=(0.24, 0.24), h=0.2), _fin('pelvic', s=(0.27, 0.27), h=0.08)],
                     colors=P('#7a6a4a', '#c8c0a8', '#f4f2ea', silver=0.55, fin_up=('#8a7a5a', '#a0906a', 0.7), fin_low=('#d8d0c0', '#e8e0d0', 0.6),
                              patterns=[('mottle', dict(color='#4a3a24', a=0.45, scale=0.06))]),
                     eye=dict(s=0.11, z=0.5, r=0.028, iris='#d8d0b0'), anim=dict(amp=1.0, freq=1.1)),
    # 참가자미 (Pseudopleuronectes herzensteini): 오른눈 가자미, 지느러미 가장자리 노란 띠
    'flatfish': dict(type='F', name='참가자미', flat='right', depth=0.44, snout=0.5, mouth=0.0, ped=0.3,
                     colors=P('#5a4e38', '#7a6c50', '#f6f2e6', silver=0.0, fin_up=('#7a6a40', '#d0b040', 0.9), fin_low=('#7a6a40', '#d0b040', 0.9),
                              patterns=[('mottle', dict(color='#3a3226', a=0.4, scale=0.05)), ('spots', dict(n=30, r=(2, 4), color='#2a241c', v=(0.1, 1.0), a=0.4))]),
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
    # 대구 (Gadus macrocephalus): 큰 머리, 턱수염 하나, 등지느러미 셋·뒷지느러미 둘, 갈색 점
    'cod': dict(type='C', name='대구', depth=0.22, width=0.15, head_w=1.12, tmax=0.33, mouth=0.0, snout=0.6, arch=0.55, scales=0,
                barbels=[(0.035, -0.6, 0.035, 0.003, 'down')],
                fins=[_fin('dorsal', (0.3, 0.44), 0.32, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]), _fin('dorsal', (0.48, 0.64), 0.3, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                      _fin('dorsal', (0.68, 0.82), 0.26, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                      _fin('anal', (0.45, 0.62), 0.26), _fin('anal', (0.66, 0.82), 0.24),
                      _fin('caudal', h=0.2, shape='truncate'), _fin('pectoral', s=(0.27, 0.27), h=0.13), _fin('pelvic', s=(0.24, 0.24), h=0.09)],
                colors=P('#5a5038', '#9a8c68', '#ece6d4', silver=0.05, fin_up=('#6a5e44', '#5a503a', 0.9), fin_low=('#a09478', '#b0a488', 0.85),
                         patterns=[('spots', dict(n=90, r=(2, 5), color='#3e3626', v=(0.4, 1.0), a=0.5)), ('stripe', dict(v=0.55, w=0.012, color='#e8e0c8', a=0.6))]),
                eye=dict(s=0.1, z=0.38, r=0.02, iris='#b0a070'), anim=dict(amp=0.85, freq=0.8)),
    # 명태 (Gadus chalcogrammus): 대구보다 날씬하고 아래턱이 나온다, 등은 갈색, 배는 은백색. 청진 명물
    'pollock': dict(type='C', name='명태', depth=0.17, width=0.11, tmax=0.36, mouth=0.35, snout=0.85, arch=0.52, scales=0,
                    fins=[_fin('dorsal', (0.3, 0.42), 0.3, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]), _fin('dorsal', (0.46, 0.62), 0.3, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                          _fin('dorsal', (0.67, 0.82), 0.26, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                          _fin('anal', (0.42, 0.62), 0.26), _fin('anal', (0.66, 0.82), 0.24),
                          _fin('caudal', h=0.2, shape='emarg'), _fin('pectoral', s=(0.27, 0.27), h=0.12), _fin('pelvic', s=(0.24, 0.24), h=0.08)],
                    colors=P('#4e4a3e', '#a8a290', '#f2f0e8', silver=0.45, fin_up=('#5e584a', '#4a463a', 0.85), fin_low=('#b8b4a4', '#c8c4b4', 0.7),
                             patterns=[('mottle', dict(color='#3a362c', a=0.35, scale=0.06)), ('stripe', dict(v=0.5, w=0.015, color='#e0dccc', a=0.5))]),
                    eye=dict(s=0.1, z=0.32, r=0.024, iris='#c0b890'), anim=dict(amp=0.9, freq=0.9)),
    # 불볼락 (열기, Sebastes thompsoni): 붉은 주황 볼락, 큰 눈
    'redrockfish': dict(type='C', name='불볼락', depth=0.3, mouth=0.25, eye=dict(s=0.12, z=0.42, r=0.032, iris='#d0a040'),
                        colors=P('#a8402a', '#e07a4a', '#f6d8c0', silver=0.15, fin_up=('#c05a3a', '#e08a5a', 0.85), fin_low=('#e08a5a', '#f0a878', 0.8),
                                 patterns=[('bands', dict(n=5, u=(0.3, 0.85), w=0.06, color='#8a3020', v=(0.45, 1.0), a=0.35))])),
    # ===== 남해 (욕지도·거문도) =====
    # 방어 (Seriola quinqueradiata): 큰 방추형, 등 청록, 몸 가운데 노란 세로띠
    'yellowtail': dict(type='E', name='방어', depth=0.22, width=0.13, ped=0.18,
                       colors=P('#2a5a6a', '#a8c0c4', '#f2f4f0', silver=0.75, metallic=0.3, fin_up=('#4a7070', '#c8b040', 0.75), fin_low=('#b8c0b8', '#d8d0a0', 0.6),
                                patterns=[('stripe', dict(v=0.55, w=0.035, color='#e8c840', a=0.85))]),
                       eye=dict(iris='#d0c890'), anim=dict(amp=1.0, freq=1.2)),
    # 부시리 (Seriola lalandi): 방어보다 길쭉, 노란 띠가 진하고 꼬리가 노랗다. 거문도 갯바위 대물
    'amberjack': dict(type='E', name='부시리', depth=0.19, width=0.12, ped=0.17, snout=0.8,
                      colors=P('#2a4e66', '#9ab4c0', '#f4f6f2', silver=0.7, metallic=0.3, fin_up=('#4a6a70', '#e8c040', 0.8), fin_low=('#c0c4b0', '#e8d070', 0.7),
                               patterns=[('stripe', dict(v=0.56, w=0.045, color='#f0c830', a=0.9))]),
                      eye=dict(iris='#d0c890'), anim=dict(amp=1.0, freq=1.25)),
    # 갈치 (Trichiurus lepturus): 은빛 띠 모양, 꼬리지느러미 없이 가늘어진다, 등지느러미가 몸 전체
    'hairtail': dict(type='G', name='갈치', depth=0.07, width=0.025, tmax=0.25, body_end=0.99, ped=0.06, snout=0.9, mouth=0.35, xsec=1.3, blunt=1.0, hold=0.0, tail_w=0.32,
                     fins=[_fin('dorsal', (0.08, 0.97), 0.4, prof=[(0, 0.6), (0.1, 1.0), (0.85, 0.7), (1, 0.2)]),
                           _fin('caudal', h=0.01, shape='point'), _fin('pectoral', s=(0.1, 0.1), h=0.05)],
                     colors=P('#b8c4cc', '#e4eaee', '#f8fafa', silver=1.0, metallic=0.85, fin_up=('#d0dce0', '#e8f0f0', 0.5), fin_low=('#d0dce0', '#e8f0f0', 0.5),
                              patterns=[('sheen', 0.8)]),
                     eye=dict(s=0.05, z=0.35, r=0.012, iris='#e0d8b0'), bones=14, anim=dict(amp=1.2, freq=0.9)),
    # 삼치 (Scomberomorus niphonius): 길쭉한 방추형, 등 청회색에 점 줄, 작은 토막지느러미
    'spanishmackerel': dict(type='E', name='삼치', depth=0.15, width=0.1, tmax=0.42, ped=0.16, snout=0.85, mouth=0.1,
                            colors=P('#3a5a74', '#b4c4cc', '#f4f6f4', silver=0.8, metallic=0.35, fin_up=('#4a6478', '#6a8090', 0.7), fin_low=('#b8c0c4', '#d0d8dc', 0.6),
                                     patterns=[('spots', dict(n=110, r=(4, 7), color='#1e3044', v=(0.55, 0.9), a=0.75)), ('finlets', True)]),
                            eye=dict(iris='#c8c8b0'), anim=dict(amp=1.1, freq=1.4)),
    # ===== 제주 =====
    # 자리돔 (Chromis notata): 손바닥만 한 자리돔과, 짙은 회갈색 몸에 등지느러미 밑 흰 점, 깊게 갈라진 꼬리
    'damselfish': dict(type='D', name='자리돔', depth=0.42, width=0.13, snout=0.45, mouth=0.05, scales=30,
                       fins=[_fin('dorsal', (0.3, 0.78), 0.3, prof=[(0, 0.5), (0.2, 1.0), (0.8, 0.8), (1, 1.1)], spiny=True),
                             _fin('anal', (0.6, 0.8), 0.32), _fin('caudal', h=0.3, shape='deepfork'),
                             _fin('pectoral', s=(0.3, 0.3), h=0.16), _fin('pelvic', s=(0.34, 0.34), h=0.13)],
                       colors=P('#2a2e34', '#4a5058', '#8a8c88', silver=0.15, fin_up=('#2a2e34', '#1e2226', 0.9), fin_low=('#3a3e44', '#2a2e34', 0.9),
                                patterns=[('spots', dict(n=1, r=(9, 9), u=(0.62, 0.62), v=(0.92, 0.92), color='#f0f2f0', a=0.9)), ('scale_edge', 0.5)]),
                       eye=dict(iris='#6a8aa0'), anim=dict(amp=1.0, freq=1.4)),
    # 옥돔 (Branchiostegus japonicus): 가파른 이마, 큰 눈, 분홍빛 붉은 몸, 눈 아래 은빛 삼각 무늬, 꼬리에 노란 줄
    'tilefish': dict(type='C', name='옥돔', depth=0.25, width=0.12, tmax=0.3, snout=0.3, mouth=-0.25, arch=0.62, scales=70,
                     fins=[_fin('dorsal', (0.27, 0.82), 0.25, prof=[(0, 0.6), (0.1, 1.0), (1, 0.8)]),
                           _fin('anal', (0.5, 0.82), 0.25), _fin('caudal', h=0.22, shape='truncate'),
                           _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                     colors=P('#c0505a', '#e88a8a', '#f6e0dc', silver=0.3, fin_up=('#d07070', '#f0c040', 0.8), fin_low=('#e8a0a0', '#f0c8b0', 0.75),
                              patterns=[('hstripes', dict(n=4, v=(0.55, 0.9), w=0.012, color='#f0d060', a=0.6))]),
                     eye=dict(s=0.08, z=0.55, r=0.03, iris='#e0d0a0'), anim=dict(amp=0.9, freq=1.0)),
    # 무늬오징어 (Sepioteuthis lessoniana): 외투막을 두르는 넓은 지느러미, 반투명한 갈색 얼룩. 에깅의 왕
    'bigfinsquid': dict(type='Q', name='무늬오징어',
                        colors=P('#7a5a46', '#b8957a', '#efe2d6', silver=0.05, fin_up=('#c8b098', '#ead8c4', 0.75), fin_low=('#c8b098', '#ead8c4', 0.75),
                                 patterns=[('mottle', dict(color='#5a3e2c', a=0.45, scale=0.05)), ('spots', dict(n=120, r=(1, 3), color='#4a2e20', v=(0.4, 1.0), a=0.5))]),
                        eye=dict(iris='#40c0c8')),
    # ===== 서해 (영흥도·태안) =====
    # 점농어 (Lateolabrax maculatus): 농어와 같은 몸에 등·옆구리 검은 점이 뚜렷하다
    'spottedseabass': dict(type='C', name='점농어', depth=0.24, width=0.12, mouth=0.25, snout=0.85, scales=80,
                           colors=P('#4a5a62', '#b8c4c8', '#f0f2f0', silver=0.6, fin_up=('#5a686e', '#3a484e', 0.8), fin_low=('#c8ccc8', '#d8dcd8', 0.6),
                                    patterns=[('spots', dict(n=150, r=(4, 8), color='#1a2024', v=(0.45, 1.0), a=0.9))]),
                           eye=dict(iris='#d0c8a0')),
    # ===== 한강 추가 (원작 2013 공략: 은어·떡붕어) =====
    # 은어 (Plecoglossus altivelis): 날씬한 몸, 기름지느러미, 올리브색 등에 아가미 뒤 노란 점
    'sweetfish': dict(type='B', name='은어', depth=0.18, width=0.1, tmax=0.42, snout=0.8, mouth=0.1, scales=70,
                      fins=[_fin('dorsal', (0.42, 0.54), 0.7, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]), _fin('adipose', (0.73, 0.77), 0.2),
                            _fin('anal', (0.62, 0.78), 0.5), _fin('caudal', h=0.24, shape='deepfork'),
                            _fin('pectoral', s=(0.22, 0.22), h=0.11), _fin('pelvic', s=(0.48, 0.48), h=0.08)],
                      colors=P('#5a6a3a', '#b8b890', '#f2f0e0', silver=0.6, fin_up=('#7a7a50', '#a09a60', 0.7), fin_low=('#d8c060', '#e8d080', 0.75),
                               patterns=[('spots', dict(n=1, r=(10, 10), u=(0.26, 0.26), v=(0.62, 0.62), color='#f0d040', a=0.85)), ('sheen', 0.5)]),
                      eye=dict(iris='#d8d0a0'), anim=dict(amp=1.1, freq=1.3)),
    # 떡붕어 (Carassius cuvieri): 붕어보다 체고가 높고 은빛이 강하다
    'gengorobuna': dict(type='A', name='떡붕어', depth=0.38, tmax=0.42, scales=30, mouth=0.05,
                        colors=P('#4a5448', '#b8b8a0', '#ecead8', silver=0.55, fin_up=('#5a5e50', '#3e4238', 0.8), fin_low=('#8a8a74', '#6a6a58', 0.75),
                                 patterns=[('scale_edge', 0.45), ('sheen', 0.4)]),
                        eye=dict(iris='#c8b060')),
    # ===== 괴물 어종 (원작 이벤트 몬스터: 괴물잉어 · 괴물가물치 · 폭군삼치 · 황새치) =====
    # 괴물잉어: 이끼 낀 듯 검푸른 등, 큰 비늘마다 금빛 테, 붉은 지느러미
    'monstercarp': dict(type='A', name='괴물잉어', depth=0.3, scales=30,
                        barbels=[(0.03, -0.25, 0.06, 0.006, 'down'), (0.05, -0.15, 0.04, 0.004, 'side')],
                        colors=P('#1e2618', '#6a5a2a', '#c8b47a', silver=0.2, metallic=0.25, fin_up=('#2a2618', '#1a1810', 0.9), fin_low=('#a83a1a', '#d8401a', 0.9),
                                 patterns=[('scale_edge', 1.0), ('mottle', dict(color='#0e140a', a=0.5, scale=0.08))]),
                        eye=dict(r=0.022, iris='#e8b020')),
    # 괴물가물치: 거의 검은 몸에 더 짙은 얼룩, 붉은 눈
    'monstersnakehead': dict(type='H', name='괴물가물치', depth=0.16, width=0.14, head_w=1.05, tmax=0.4, body_end=0.83, ped=0.75, snout=0.6, mouth=0.1, scales=70,
                             fins=[_fin('dorsal', (0.3, 0.98), 0.34, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                   _fin('anal', (0.55, 0.98), 0.32, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                   _fin('caudal', h=0.16, shape='round'), _fin('pectoral', s=(0.24, 0.24), h=0.11), _fin('pelvic', s=(0.33, 0.33), h=0.07)],
                             colors=P('#121410', '#3a3a28', '#8a8670', silver=0.0, fin_up=('#1a1c14', '#2a2c20', 0.95), fin_low=('#2a2c20', '#3a3c2c', 0.95),
                                      patterns=[('blotchrows', dict(color='#050604', a=0.9)), ('eyeline', dict(color='#050604', w=0.03, n=2))]),
                             eye=dict(s=0.07, z=0.4, r=0.014, iris='#d02018')),
    # 폭군삼치: 삼치보다 굵고 등이 검푸르며 점이 크다, 붉은 홍채
    'tyrantmackerel': dict(type='E', name='폭군삼치', depth=0.17, width=0.11, tmax=0.42, ped=0.16, snout=0.8, mouth=0.12,
                           colors=P('#14243a', '#7a8ea0', '#e8ecee', silver=0.85, metallic=0.45, fin_up=('#1e2e44', '#34465a', 0.8), fin_low=('#9aa4ac', '#c0c8cc', 0.6),
                                    patterns=[('spots', dict(n=70, r=(7, 11), color='#0a1420', v=(0.5, 0.9), a=0.85)), ('finlets', True)]),
                           eye=dict(r=0.026, iris='#c83020'), anim=dict(amp=1.1, freq=1.5)),
    # 황새치 (Xiphias gladius): 칼 같은 위턱, 높은 낫 모양 등지느러미, 초승달 꼬리, 보랏빛 검은 등
    'swordfish': dict(type='E', name='황새치', depth=0.15, width=0.12, tmax=0.33, body_end=0.82, ped=0.1, snout=0.9, mouth=0.0, bill=0.3,
                      fins=[_fin('dorsal', (0.16, 0.3), 1.6, prof=[(0, 1.0), (0.25, 0.85), (1, 0.08)]),
                            _fin('anal', (0.6, 0.68), 0.4, prof=[(0, 1.0), (1, 0.2)]),
                            _fin('caudal', h=0.42, shape='deepfork'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.18, long=True)],
                      colors=P('#22182e', '#5a5068', '#d0ccd4', silver=0.4, metallic=0.4, fin_up=('#241a30', '#1a1222', 0.95), fin_low=('#3a3048', '#2a2234', 0.9),
                               patterns=[('sheen', 0.4)]),
                      eye=dict(s=0.1, z=0.2, r=0.02, iris='#28385a'), anim=dict(amp=1.0, freq=1.2)),
    # ===== 해외: 하롱베이 =====
    # 바라문디 (Lates calcarifer): 오목한 이마, 큰 입, 은회색 몸, 붉게 빛나는 눈, 둥근 꼬리
    'barramundi': dict(type='C', name='바라문디', depth=0.27, width=0.13, snout=0.6, mouth=0.35, arch=0.62, scales=55,
                       fins=[_fin('dorsal', (0.3, 0.52), 0.5, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.5), (1, 0.4)], spiny=True),
                             _fin('dorsal', (0.55, 0.76), 0.5, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)]),
                             _fin('anal', (0.62, 0.77), 0.45), _fin('caudal', h=0.24, shape='round'),
                             _fin('pectoral', s=(0.26, 0.26), h=0.13), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                       colors=P('#5a6a6a', '#c0c8c4', '#f0f0ea', silver=0.7, fin_up=('#6a706a', '#4a504a', 0.85), fin_low=('#b0a890', '#c8c0a8', 0.75),
                                patterns=[('scale_edge', 0.4)]),
                       eye=dict(iris='#d04020'), anim=dict(amp=1.0, freq=1.0)),
    # 얼룩바리 (Epinephelus coioides): 두툼한 몸에 주황 갈색 점이 빽빽, 둥근 꼬리
    'orangegrouper': dict(type='C', name='얼룩바리', depth=0.28, width=0.16, snout=0.6, mouth=0.25, scales=0,
                          fins=[_fin('dorsal', (0.3, 0.8), 0.32, prof=[(0, 0.6), (0.15, 1.0), (0.6, 0.8), (1, 1.1)], spiny=True),
                                _fin('anal', (0.62, 0.8), 0.32), _fin('caudal', h=0.22, shape='round'),
                                _fin('pectoral', s=(0.26, 0.26), h=0.16), _fin('pelvic', s=(0.3, 0.3), h=0.13)],
                          colors=P('#4a5038', '#8a8a62', '#d8d4b8', silver=0.05, fin_up=('#5a5a40', '#3a3a28', 0.9), fin_low=('#6a6a50', '#4a4a38', 0.9),
                                   patterns=[('spots', dict(n=260, r=(3, 5), color='#c86a28', v=(0.1, 1.0), a=0.9)), ('blotches', dict(color='#2a2c1a', a=0.35))]),
                          eye=dict(iris='#d0c060')),
    # 맹그로브잭 (Lutjanus argentimaculatus): 구릿빛 붉은 몸, 도미형, 깊게 파이지 않은 꼬리
    'mangrovejack': dict(type='D', name='맹그로브잭', depth=0.33, width=0.13, snout=0.6, mouth=0.05, scales=45,
                         colors=P('#6a2a20', '#b84a34', '#e8b8a0', silver=0.25, fin_up=('#7a3424', '#9a4a30', 0.85), fin_low=('#c05a3a', '#d87a50', 0.8),
                                  patterns=[('scale_edge', 0.5)]),
                         eye=dict(iris='#d8a040')),
    # 자이언트 트레발리 (Caranx ignobilis): 가파른 이마, 높고 납작한 몸, 꼬리자루 비늘판, 낫 모양 꼬리
    'gianttrevally': dict(type='D', name='자이언트 트레발리', depth=0.36, width=0.13, snout=0.4, mouth=0.0, ped=0.14, scales=0,
                          fins=[_fin('dorsal', (0.3, 0.45), 0.3, prof=[(0, 0.6), (0.3, 1.0), (1, 0.4)], spiny=True),
                                _fin('dorsal', (0.48, 0.78), 0.55, prof=[(0, 1.0), (0.2, 0.7), (1, 0.25)]),
                                _fin('anal', (0.55, 0.78), 0.5, prof=[(0, 1.0), (0.2, 0.7), (1, 0.25)]),
                                _fin('caudal', h=0.36, shape='deepfork'), _fin('pectoral', s=(0.28, 0.28), h=0.24, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.08)],
                          colors=P('#3a4048', '#9aa2a8', '#e0e2e2', silver=0.7, metallic=0.3, fin_up=('#2a2e34', '#1a1e22', 0.9), fin_low=('#5a6066', '#3a4046', 0.9),
                                   patterns=[('scutes', dict(color='#6a7278', a=0.7)), ('spots', dict(n=60, r=(1, 2), color='#1a1e22', v=(0.5, 0.95), a=0.5))]),
                          eye=dict(iris='#c8c0a0'), anim=dict(amp=1.0, freq=1.3)),
    # 꼬치고기 (Sphyraena jello): 아주 길쭉한 몸, 아래턱이 튀어나온 큰 입, 비스듬한 검은 줄무늬
    'barracuda': dict(type='E', name='꼬치고기', depth=0.11, width=0.09, tmax=0.45, body_end=0.85, ped=0.3, snout=0.95, mouth=0.15,
                      fins=[_fin('dorsal', (0.38, 0.45), 0.5, prof=[(0, 1.0), (1, 0.3)], spiny=True), _fin('dorsal', (0.68, 0.74), 0.45),
                            _fin('anal', (0.68, 0.75), 0.4), _fin('caudal', h=0.2, shape='fork'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.08), _fin('pelvic', s=(0.42, 0.42), h=0.07)],
                      colors=P('#4a5a62', '#b8c4c8', '#f0f2f2', silver=0.85, metallic=0.3, fin_up=('#5a6a70', '#7a8a90', 0.7), fin_low=('#c0c8c8', '#d8e0e0', 0.6),
                               patterns=[('obliquebands', dict(n=18, color='#2a3238', a=0.55))]),
                      eye=dict(s=0.11, z=0.35, r=0.016, iris='#d8d0b0'), anim=dict(amp=1.1, freq=1.2)),
    # ===== 해외: 산토리니 =====
    # 귀족도미 (Sparus aurata): 도미형 은회색 몸, 눈 사이 금빛 띠, 아가미 뚜껑 끝 검은 점
    'giltheadbream': dict(type='D', name='귀족도미', depth=0.38, width=0.12, snout=0.45, mouth=-0.2, scales=55,
                          colors=P('#5a6a74', '#b8c4cc', '#eef0f0', silver=0.75, metallic=0.25, fin_up=('#6a7a84', '#4a5a64', 0.8), fin_low=('#a8b0b8', '#c0c8d0', 0.7),
                                   patterns=[('hstripes', dict(n=6, v=(0.3, 0.8), w=0.008, color='#7a8a94', a=0.5)),
                                             ('spots', dict(n=1, r=(11, 11), u=(0.12, 0.12), v=(0.8, 0.8), color='#e8b830', a=0.9)),
                                             ('spots', dict(n=1, r=(7, 7), u=(0.24, 0.24), v=(0.68, 0.68), color='#1a1e22', a=0.8))]),
                          eye=dict(iris='#d8c070')),
    # 유럽농어 (Dicentrarchus labrax): 농어와 비슷한 은빛 몸
    'europeanseabass': dict(type='C', name='유럽농어', depth=0.23, width=0.12, mouth=0.25, snout=0.85, scales=80,
                            colors=P('#56646c', '#c4ccd0', '#f4f6f6', silver=0.8, metallic=0.2, fin_up=('#606e74', '#44525a', 0.8), fin_low=('#c8ccc8', '#d8dcd8', 0.6),
                                     patterns=[('spots', dict(n=1, r=(8, 8), u=(0.24, 0.24), v=(0.7, 0.7), color='#2a3238', a=0.7))]),
                            eye=dict(iris='#d0c8a0')),
    # 유럽대구 (Merluccius merluccius): 길쭉한 몸, 큰 입, 등지느러미 둘, 회청색 등
    'hake': dict(type='C', name='유럽대구', depth=0.17, width=0.1, tmax=0.35, snout=0.85, mouth=0.25, scales=90,
                 fins=[_fin('dorsal', (0.28, 0.4), 0.5, prof=[(0, 0.6), (0.3, 1.0), (1, 0.3)]), _fin('dorsal', (0.44, 0.82), 0.35, prof=[(0, 0.6), (0.5, 1.0), (1, 0.5)]),
                       _fin('anal', (0.44, 0.82), 0.3), _fin('caudal', h=0.18, shape='truncate'),
                       _fin('pectoral', s=(0.24, 0.24), h=0.13), _fin('pelvic', s=(0.22, 0.22), h=0.1)],
                 colors=P('#4a5868', '#a8b4c0', '#eef0f2', silver=0.6, fin_up=('#5a6878', '#3a4858', 0.8), fin_low=('#b0b8c0', '#c8d0d8', 0.6),
                          patterns=[('sheen', 0.4)]),
                 eye=dict(r=0.026, iris='#c8c8b0')),
    # 지중해곰치 (Muraena helena): 장어형, 짙은 갈색 바탕에 노란 얼룩 점
    'moray': dict(type='G', name='지중해곰치', depth=0.09, width=0.07, snout=0.6, mouth=0.1,
                  colors=P('#2e2418', '#4a3a26', '#6a5a40', silver=0.0, fin_up=('#3a2e20', '#2a2014', 0.95), fin_low=('#3a2e20', '#2a2014', 0.95),
                           patterns=[('spots', dict(n=220, r=(4, 8), color='#e0c050', v=(0.0, 1.0), a=0.85))]),
                  eye=dict(iris='#c0a040')),
    # 참다랑어 (Thunnus thynnus): 굵은 방추형, 짙은 남색 등, 은빛 배, 노란 토막지느러미, 초승달 꼬리
    'bluefintuna': dict(type='E', name='참다랑어', depth=0.25, width=0.18, tmax=0.4, ped=0.1, snout=0.75, mouth=0.05,
                        fins=[_fin('dorsal', (0.32, 0.44), 0.4, prof=[(0, 1.0), (1, 0.25)], spiny=True), _fin('dorsal', (0.52, 0.6), 0.55, prof=[(0, 1.0), (1, 0.3)]),
                              _fin('anal', (0.56, 0.64), 0.5, prof=[(0, 1.0), (1, 0.3)]), _fin('caudal', h=0.42, shape='deepfork'),
                              _fin('pectoral', s=(0.26, 0.26), h=0.12), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                        colors=P('#101a3a', '#7a8a9a', '#e8ecee', silver=0.8, metallic=0.45, fin_up=('#141e3a', '#0a1028', 0.9), fin_low=('#a0a8b0', '#c0c8d0', 0.7),
                                 patterns=[('finlets', True), ('sheen', 0.5)]),
                        eye=dict(iris='#202830'), anim=dict(amp=0.9, freq=1.6)),
    # ===== 해외: 나일강 =====
    # 나일농어 (Lates niloticus): 바라문디 닮은 큰 몸, 은회색, 오목한 이마
    'nileperch': dict(type='C', name='나일농어', depth=0.28, width=0.14, snout=0.55, mouth=0.35, arch=0.65, scales=55,
                      fins=[_fin('dorsal', (0.3, 0.5), 0.55, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.5), (1, 0.4)], spiny=True),
                            _fin('dorsal', (0.53, 0.76), 0.5, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)]),
                            _fin('anal', (0.62, 0.77), 0.45), _fin('caudal', h=0.24, shape='round'),
                            _fin('pectoral', s=(0.26, 0.26), h=0.13), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                      colors=P('#5a5e50', '#b0b0a0', '#e8e6dc', silver=0.55, fin_up=('#5a5a4a', '#3a3a30', 0.85), fin_low=('#9a9480', '#b0aa98', 0.75),
                               patterns=[('scale_edge', 0.4)]),
                      eye=dict(iris='#e0b020')),
    # 나일틸라피아 (Oreochromis niloticus): 붕어 닮은 높은 몸, 회색 몸에 희미한 세로 띠, 꼬리에 가는 줄무늬
    'tilapia': dict(type='D', name='나일틸라피아', depth=0.38, width=0.14, snout=0.5, mouth=0.05, scales=32,
                    colors=P('#4a5248', '#8a9488', '#d8dcd0', silver=0.3, fin_up=('#5a6058', '#c05040', 0.8), fin_low=('#6a7068', '#8a5a4a', 0.8),
                             patterns=[('bands', dict(n=7, u=(0.25, 0.85), w=0.04, color='#3a4038', v=(0.2, 0.95), a=0.35)), ('scale_edge', 0.5)]),
                    eye=dict(iris='#c08040')),
    # 아프리카메기 (Clarias gariepinus): 넓적하고 뼈판 같은 머리, 긴 등·뒷지느러미, 수염 네 쌍, 얼룩 회갈색
    'africancatfish': dict(type='H', name='아프리카메기', depth=0.13, width=0.13, head_w=1.3, tmax=0.3, body_end=0.88, ped=0.7, snout=0.4, scales=0,
                           fins=[_fin('dorsal', (0.25, 0.86), 0.25, prof=[(0, 0.6), (0.1, 1.0), (1, 0.9)]),
                                 _fin('anal', (0.45, 0.86), 0.28, prof=[(0, 0.6), (0.1, 1.0), (1, 0.9)]),
                                 _fin('caudal', h=0.14, shape='round'), _fin('pectoral', s=(0.16, 0.16), h=0.1), _fin('pelvic', s=(0.42, 0.42), h=0.06)],
                           barbels=[(0.02, -0.1, 0.09, 0.006, 'side'), (0.03, -0.35, 0.06, 0.004, 'down'), (0.05, 0.3, 0.05, 0.004, 'up')],
                           colors=P('#3a3a30', '#6a6a58', '#c8c4b0', silver=0.0, fin_up=('#3a3a30', '#2a2a22', 0.95), fin_low=('#4a4a3c', '#3a3a2e', 0.95),
                                    patterns=[('mottle', dict(color='#1e1e18', a=0.55, scale=0.06))]),
                           eye=dict(iris='#a08040')),
    # 타이거피시 (Hydrocynus vittatus): 은빛 몸에 검은 가로줄, 붉은 지느러미, 드러난 송곳니(입 테두리)
    'tigerfish': dict(type='B', name='타이거피시', depth=0.23, width=0.11, tmax=0.4, snout=0.75, mouth=0.15, scales=40,
                      fins=[_fin('dorsal', (0.42, 0.53), 0.65, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]), _fin('adipose', (0.74, 0.78), 0.18),
                            _fin('anal', (0.62, 0.78), 0.45), _fin('caudal', h=0.3, shape='deepfork'),
                            _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.48, 0.48), h=0.1)],
                      colors=P('#4a5658', '#c0c8c8', '#f2f4f2', silver=0.85, metallic=0.25, fin_up=('#c03a2a', '#e05a3a', 0.85), fin_low=('#c03a2a', '#e05a3a', 0.85),
                               patterns=[('hstripes', dict(n=6, v=(0.3, 0.9), w=0.012, color='#1a1e20', a=0.75)), ('mouthline', 0.8)]),
                      eye=dict(iris='#e0c040'), anim=dict(amp=1.1, freq=1.4)),
}

# 잡동사니 (Unity에서 모양을 코드로 만드는 대신 여기서 같이 모델링한다)
JUNK = ['can', 'trap', 'box', 'fishcan', 'robotfish', 'shoe']


# 2026-10-19부터 늘어난 어종: 지역별 파일 (docs/fish-db). 각 파일은 SPECIES = {...}를 내놓는다 (P·_fin은 여기서 가져간다)
for _mod in ('species_korea2', 'species_sea_fresh', 'species_sea', 'species_med', 'species_med2'):
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
