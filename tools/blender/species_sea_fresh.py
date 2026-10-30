"""동남아 거점 민물 어종 (docs/fish-db/02_동남아_어종DB.md 2-A) — 설계 시트 형식은 fish_species.py 머리말."""
from fish_species import P, _fin  # noqa: F401

SPECIES = {
    # ===== 메콩의 거인들 =====
    # 메콩 대형 메기 (Pangasianodon gigas): 상어처럼 생긴 거대한 메기. 다 자라면 수염이 거의 없고, 눈이 입꼬리보다 아래에 붙는다.
    # 짙은 회색 등, 흰 배, 높은 세모 등지느러미, 작은 기름지느러미, 깊게 갈라진 꼬리. 넓은 입은 입꼬리가 살짝 처지며 눈 바로 앞까지 온다.
    # 머리는 둥근 공이 아니라 끝으로 갈수록 좁아지는 쐐기(snout·blunt), 입은 주둥이 끝에서 넓게 찢어진다(gape)
    'mekonggiantcatfish': dict(type='H', name='메콩 대형 메기', depth=0.21, width=0.15, head_w=1.08, tmax=0.38, body_end=0.8, ped=0.3, arch=0.56,
                               snout=0.72, blunt=0.86, mouth=-0.08, gape=0.078, mouth_slope=0.2, xsec=2.0,
                               fins=[_fin('dorsal', (0.3, 0.38), 0.6, prof=[(0, 1.0), (1, 0.35)], spine=True),
                                     _fin('adipose', (0.7, 0.74), 0.1),
                                     _fin('anal', (0.56, 0.76), 0.3, prof=[(0, 1.0), (0.3, 0.8), (1, 0.5)]),
                                     _fin('caudal', h=0.3, shape='deepfork'),
                                     _fin('pectoral', s=(0.2, 0.2), h=0.16, spine=True), _fin('pelvic', s=(0.43, 0.43), h=0.1)],
                               colors=P('#2c3236', '#7a8286', '#eceae4', silver=0.3,
                                        fin_up=('#3a4248', '#2a3034', 0.9), fin_low=('#7a8086', '#a0a6aa', 0.85),
                                        patterns=[('sheen', 0.3)]),
                               eye=dict(s=0.085, z=-0.2, r=0.013, iris='#a0a090'), anim=dict(amp=0.75, freq=0.6)),
    # 자이언트 바브 (Catlocarpio siamensis): 머리가 몸의 3분의 1이나 되는 거대 잉어. 수염이 없고, 큰 입이 앞을 본다.
    # 둥근 공 같은 큰 머리 앞끝에 두툼한 입술의 입, 눈은 입 높이쯤 낮게.
    # 짙은 먹색 등에 은회색 옆구리, 큰 비늘마다 검은 테가 그물처럼 또렷하다. 커다란 눈이 머리 아래쪽에, 지느러미는 짙은 회색(배·뒷지느러미 끝이 붉다)
    'giantbarb': dict(type='A', name='자이언트 바브', depth=0.31, width=0.15, head_w=1.1, tmax=0.42, arch=0.62, snout=0.55, blunt=0.85, mouth=0.12,
                      gape=0.06, lips=1.8, scales=28,
                      fins=[_fin('dorsal', (0.42, 0.6), 0.5, prof=[(0, 0.4), (0.15, 1.0), (0.5, 0.6), (1, 0.45)]),
                            _fin('anal', (0.72, 0.82), 0.45, prof=[(0, 0.5), (0.2, 1.0), (1, 0.45)]),
                            _fin('caudal', h=0.27, shape='fork'),
                            _fin('pectoral', s=(0.3, 0.3), h=0.13), _fin('pelvic', s=(0.5, 0.5), h=0.12)],
                      colors=P('#1c2022', '#5e6668', '#cfd2ca', silver=0.5, fin_up=('#2e3234', '#1e2224', 0.9), fin_low=('#3a3436', '#8a3a34', 0.85),
                               patterns=[('scale_edge', 1.0)]),
                      eye=dict(s=0.13, z=-0.08, r=0.02, iris='#d8c890'), anim=dict(amp=0.8, freq=0.7)),
    # 거대 담수 가오리 (Urogymnus polylepis): 가오리 틀이 없어 납작한 고기 틀(F)로 흉내 낸다.
    # 몸통(원반)을 아주 높게(depth) 잡아 둥근 원반을 만들고, 납작한 고기처럼 옆으로 눕혀 '위' 면이 등이 되게 한다(flat='left' → 두 눈이 위).
    # 원반은 disc에서 끝나고 그 뒤로 가늘어지는 채찍 꼬리. ray: 두 눈이 등 가운데 좌우로 나란히, 눈 뒤 숨구멍.
    # 원반 가장자리 지느러미는 낮게만 남겨 날개처럼 물결치게 한다 (F 애니메이션이 위아래로 물결친다)
    'giantstingray': dict(type='F', name='거대 담수 가오리', flat='left', depth=0.62, width=0.07, tmax=0.28, body_end=1.0, disc=0.62, ped=0.035, arch=0.5,
                          snout=0.3, mouth=0.0, xsec=1.4, tail_w=0.35, ray=dict(eye_z=0.15),
                          fins=[_fin('dorsal', (0.062, 0.53), 0.08, prof=[(0, 0.3), (0.45, 1.0), (1, 0.2)]),
                                _fin('anal', (0.062, 0.53), 0.08, prof=[(0, 0.3), (0.45, 1.0), (1, 0.2)]),
                                _fin('caudal', h=0.02, shape='point'),
                                _fin('pelvic', s=(0.5, 0.5), h=0.06)],
                          colors=P('#4a3e30', '#6a5a44', '#f2eee6', silver=0.0, fin_up=('#5a4c3a', '#3a3026', 0.9), fin_low=('#5a4c3a', '#3a3026', 0.9),
                                   patterns=[('mottle', dict(color='#2e261c', a=0.35, scale=0.07)),
                                             ('spots', dict(n=40, r=(2, 4), color='#8a7860', v=(0.1, 1.0), a=0.3))]),
                          eye=dict(s=0.1, r=0.012, iris='#605040'), anim=dict(amp=0.6, freq=0.5)),
    # 자이언트 판가시우스 (Pangasius sanitwongsei): 넓적한 머리, 은백색 몸에 회색 등. 첫 등지느러미 살이 아주 길게 솟고
    # 가슴·배지느러미 첫 살도 실처럼 길다. 지느러미는 짙은 회색, 깊게 갈라진 꼬리
    # 머리는 위아래로 눌린 납작한 삽 모양(head_flat), 주둥이 끝 아래 입, 입꼬리 높이의 큰 눈
    'giantpangasius': dict(type='H', name='자이언트 판가시우스', depth=0.19, width=0.14, head_w=1.25, head_flat=0.2, tmax=0.38, body_end=0.8, ped=0.28, arch=0.58,
                           snout=0.85, blunt=0.95, mouth=-0.18, gape=0.07, xsec=2.0,
                           fins=[_fin('dorsal', (0.3, 0.37), 0.95, prof=[(0, 1.0), (0.3, 0.55), (1, 0.3)], spine=True),
                                 _fin('adipose', (0.7, 0.74), 0.1),
                                 _fin('anal', (0.56, 0.76), 0.3, prof=[(0, 1.0), (0.3, 0.8), (1, 0.5)]),
                                 _fin('caudal', h=0.34, shape='deepfork'),
                                 _fin('pectoral', s=(0.2, 0.2), h=0.2, long=True), _fin('pelvic', s=(0.43, 0.43), h=0.14)],
                           barbels=[(0.03, -0.1, 0.03, 0.003, 'side'), (0.025, -0.4, 0.02, 0.002, 'down')],
                           colors=P('#464e54', '#b0b6ba', '#f4f4f0', silver=0.5, metallic=0.2,
                                    fin_up=('#3a4044', '#222628', 0.9), fin_low=('#4a5054', '#2a2e30', 0.9),
                                    patterns=[('sheen', 0.45)]),
                           eye=dict(s=0.08, z=-0.1, r=0.016, iris='#b8b8a8'), anim=dict(amp=0.85, freq=0.75)),
    # 아시아 아로와나 (Scleropages formosus): 주둥이 끝이 등선 높이에 있어 등선이 주둥이부터 등지느러미까지 거의 곧다. 가장 높은 곳은 뒤쪽(뒷지느러미 앞).
    # 아래턱이 길게 나와 입이 비스듬히 위로 크게 찢어지고, 턱끝에 앞으로 뻗은 짧은 수염 한 쌍. 동전만 한 큰 비늘 다섯 줄 (금룡: 비늘마다 금빛 테),
    # 등·뒷지느러미가 꼬리 가까이 몰리고 뒷지느러미가 더 길다, 둥근 부채꼴 꼬리, 길고 낮게 붙은 가슴지느러미
    'arowana': dict(type='B', name='아시아 아로와나', depth=0.21, width=0.09, tmax=0.55, body_end=0.84, ped=0.55, arch=0.4, snout=0.9, blunt=1.0, mouth=0.9,
                    mouth_slope=-0.5, gape=0.09, jaw=0.012, xsec=1.8, scales=20,
                    fins=[_fin('dorsal', (0.66, 0.98), 0.32, prof=[(0, 0.3), (0.5, 1.0), (1, 0.9)]),
                          _fin('anal', (0.56, 0.98), 0.4, prof=[(0, 0.3), (0.5, 1.0), (1, 0.9)]),
                          _fin('caudal', h=0.22, shape='round'),
                          _fin('pectoral', s=(0.22, 0.22), h=0.18, long=True), _fin('pelvic', s=(0.45, 0.45), h=0.08)],
                    barbels=[(0.006, 0.45, 0.03, 0.0035, 'fwd')],
                    colors=P('#4a4418', '#d8b038', '#f6e2a0', silver=0.6, metallic=0.65,
                             fin_up=('#8a6a20', '#c89838', 0.85), fin_low=('#c89838', '#e0b850', 0.8),
                             patterns=[('scale_edge', 1.0), ('sheen', 0.7)]),
                    eye=dict(s=0.08, z=0.12, r=0.021, iris='#d8a020'), anim=dict(amp=0.9, freq=0.9)),
    # ===== 대물 =====
    # 줄리엔 황금 잉어 (Probarbus jullieni, 일곱줄 바브): 길쭉한 잉어, 뾰족한 주둥이 밑으로 열린 입, 은회색에 금빛이 도는 몸에
    # 검은 세로줄 일곱 가닥, 새빨간 눈, 연분홍빛 회색 지느러미
    'julliencarp': dict(type='A', name='줄리엔 황금 잉어', depth=0.24, width=0.12, tmax=0.4, snout=0.8, blunt=0.9, mouth=-0.35, gape=0.03, lips=1.4, scales=32,
                        fins=[_fin('dorsal', (0.38, 0.52), 0.6, prof=[(0, 0.5), (0.15, 1.0), (1, 0.35)]),
                              _fin('anal', (0.7, 0.8), 0.45), _fin('caudal', h=0.27, shape='deepfork'),
                              _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.5, 0.5), h=0.1)],
                        colors=P('#4a4c3c', '#b8b090', '#eeebdc', silver=0.5, metallic=0.25,
                                 fin_up=('#6a6658', '#8a8070', 0.75), fin_low=('#b08a80', '#c89888', 0.75),
                                 patterns=[('hstripes', dict(n=7, v=(0.3, 0.9), w=0.011, color='#1e1c14', a=0.85)), ('scale_edge', 0.45)]),
                        eye=dict(r=0.022, iris='#c8302a'), anim=dict(amp=0.9, freq=0.9)),
    # 군치 (Bagarius yarrelli): 넓적한 머리, 큰 입, 높고 가시 있는 등지느러미, 가슴지느러미 가시, 깊게 갈라진 꼬리 끝이 실처럼 길다.
    # 누런 갈색 바탕에 짙은 안장 무늬, 수염. 머리가 위아래로 눌린 삽처럼 넓적하고(head_flat) 작은 눈이 정수리에 붙는다
    'goonch': dict(type='H', name='군치', depth=0.16, width=0.16, head_w=1.4, head_flat=0.35, tmax=0.34, body_end=0.84, ped=0.36, snout=0.7, blunt=0.9,
                   mouth=-0.1, gape=0.08,
                   fins=[_fin('dorsal', (0.3, 0.37), 0.6, prof=[(0, 1.0), (1, 0.5)], spine=True),
                         _fin('adipose', (0.66, 0.76), 0.12, prof=[(0, 0.4), (0.4, 1.0), (1, 0.5)]),
                         _fin('anal', (0.64, 0.76), 0.3), _fin('caudal', h=0.32, shape='deepfork'),
                         _fin('pectoral', s=(0.17, 0.17), h=0.16, spine=True), _fin('pelvic', s=(0.44, 0.44), h=0.09)],
                   barbels=[(0.03, 0.1, 0.1, 0.005, 'side'), (0.025, -0.35, 0.04, 0.003, 'down'), (0.035, -0.45, 0.035, 0.003, 'down')],
                   colors=P('#5a4c34', '#8a7a58', '#d8ccae', silver=0.0,
                            fin_up=('#5a4a32', '#2e2618', 0.9), fin_low=('#7a6a4a', '#4a3e2a', 0.9),
                            patterns=[('blotches', dict(color='#2a2216', a=0.75)), ('spots', dict(n=60, r=(2, 4), color='#2e2618', v=(0.3, 1.0), a=0.45))]),
                   eye=dict(s=0.08, z=0.7, r=0.008, iris='#a08a50'), anim=dict(amp=0.8, freq=0.7)),
    # 피라루쿠 (Arapaima gigas): 굵고 길쭉한 원통형 몸, 납작한 머리, 큰 비늘, 등·뒷지느러미가 꼬리 바로 앞에 몰렸다.
    # 앞쪽은 회녹색, 뒤로 갈수록 비늘 테가 붉어진다 (꼬리 쪽 붉은 반점). 머리는 납작하고 뾰족하며 아래턱이 튀어나와 입이 위를 본다
    'arapaima': dict(type='B', name='피라루쿠', depth=0.17, width=0.13, tmax=0.4, body_end=0.86, ped=0.6, arch=0.5, snout=0.75, blunt=0.85, head_flat=0.2,
                     mouth=0.3, gape=0.07, jaw=0.012, mouth_slope=0.25, xsec=2.1, scales=30,
                     fins=[_fin('dorsal', (0.7, 0.99), 0.35, prof=[(0, 0.3), (0.4, 1.0), (1, 0.9)]),
                           _fin('anal', (0.72, 0.99), 0.35, prof=[(0, 0.3), (0.4, 1.0), (1, 0.9)]),
                           _fin('caudal', h=0.2, shape='round'),
                           _fin('pectoral', s=(0.22, 0.22), h=0.1), _fin('pelvic', s=(0.5, 0.5), h=0.07)],
                     colors=P('#2a3430', '#6e786a', '#d8d0b8', silver=0.2,
                              fin_up=('#3a3428', '#8a2a1e', 0.9), fin_low=('#4a4434', '#8a3a28', 0.9),
                              patterns=[('scale_edge', 0.9), ('spots', dict(n=220, r=(4, 7), color='#a82a1c', v=(0.05, 0.85), u=(0.6, 0.98), a=0.85)),
                                        ('spots', dict(n=60, r=(3, 5), color='#a82a1c', v=(0.05, 0.6), u=(0.45, 0.6), a=0.4))]),
                     eye=dict(s=0.085, z=0.3, r=0.015, iris='#c0a040'), anim=dict(amp=0.85, freq=0.75)),
    # 앨리게이터 가 (Atractosteus spatula): 악어 같은 넓고 긴 주둥이(blunt를 키워 뾰족하게), 단단한 마름모 비늘의 원통형 몸,
    # 등·뒷지느러미는 꼬리 가까이, 둥근 꼬리. 올리브 갈색 등, 뒤쪽·지느러미에 검은 점
    # 주둥이는 위아래로 눌린 넓적한 오리부리(head_flat), 입이 눈 밑까지 길게 찢어진다, 입 가장자리에 송곳니가 줄지어 드러난다(teeth)
    'alligatorgar': dict(type='B', name='앨리게이터 가', depth=0.13, width=0.11, tmax=0.5, body_end=0.85, ped=0.55, arch=0.52, snout=0.95, blunt=0.75, head_w=1.4,
                         head_flat=0.3, mouth=0.05, gape=0.16, teeth=True, maxilla=0, xsec=2.1, scales=60,
                         fins=[_fin('dorsal', (0.8, 0.94), 0.45, prof=[(0, 0.5), (0.3, 1.0), (1, 0.6)]),
                               _fin('anal', (0.78, 0.94), 0.45, prof=[(0, 0.5), (0.3, 1.0), (1, 0.6)]),
                               _fin('caudal', h=0.18, shape='round'),
                               _fin('pectoral', s=(0.3, 0.3), h=0.11), _fin('pelvic', s=(0.6, 0.6), h=0.09)],
                         colors=P('#2a2e22', '#5a5e46', '#d8d4bc', silver=0.05,
                                  fin_up=('#5a5a40', '#3a3a28', 0.9), fin_low=('#6a6a4a', '#4a4a34', 0.9),
                                  patterns=[('scale_edge', 0.7), ('spots', dict(n=90, r=(4, 7), color='#1a1c12', v=(0.2, 1.0), u=(0.4, 0.98), a=0.75)),
                                            ('finspots', '#1e2014')]),
                         eye=dict(s=0.12, z=0.45, r=0.012, iris='#c0a040'), anim=dict(amp=0.9, freq=0.7)),
    # ===== 중간 =====
    # 자이언트 스네이크헤드 (Channa micropeltes): 가물치 몸에 더 길고 큰 머리 — 위아래로 눌린 머리에 눈 뒤까지 찢어진 큰 입, 아래턱이 살짝 나온다.
    # 다 자라면 청흑색 등에 흰 배가 또렷이 나뉘고, 등 쪽에 희끄무레한 구불구불한 무늬. 등·뒷지느러미가 꼬리까지 길게 이어지고 둥근 꼬리
    'giantsnakehead': dict(type='H', name='자이언트 스네이크헤드', depth=0.16, width=0.14, head_w=1.1, head_flat=0.15, tmax=0.4, body_end=0.84, ped=0.75,
                           snout=0.8, blunt=0.92, mouth=0.05, gape=0.1, jaw=0.008, scales=70,
                           fins=[_fin('dorsal', (0.3, 0.98), 0.32, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                 _fin('anal', (0.55, 0.98), 0.3, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                 _fin('caudal', h=0.16, shape='round'), _fin('pectoral', s=(0.24, 0.24), h=0.12), _fin('pelvic', s=(0.33, 0.33), h=0.07)],
                           colors=P('#141e28', '#344858', '#eceeec', silver=0.15,
                                    fin_up=('#1e2a34', '#121a20', 0.95), fin_low=('#2a3640', '#1e2830', 0.9),
                                    patterns=[('mottle', dict(color='#0e161e', a=0.5, scale=0.06)),
                                              ('spots', dict(n=40, r=(2, 4), color='#a8bccc', v=(0.55, 0.95), u=(0.15, 0.9), a=0.4)),
                                              ('eyeline', dict(color='#0e161e', w=0.03, n=1))]),
                           eye=dict(s=0.065, z=0.4, r=0.013, iris='#c87a38')),
    # 줄무늬 메기 (Pangasianodon hypophthalmus): 상어 닮은 은빛 메기, 옆줄 위아래로 짙은 줄, 짙은 지느러미, 짧은 수염 두 쌍
    # 판가시우스보다 날씬하고 머리가 짧다, 입꼬리 높이의 큰 눈
    'stripedcatfish': dict(type='H', name='줄무늬 메기', depth=0.18, width=0.12, head_w=1.08, tmax=0.36, body_end=0.8, ped=0.28, arch=0.56, snout=0.8, blunt=0.95,
                           mouth=-0.05, gape=0.05, xsec=2.0,
                           fins=[_fin('dorsal', (0.3, 0.37), 0.6, prof=[(0, 1.0), (1, 0.35)], spine=True),
                                 _fin('adipose', (0.7, 0.74), 0.1),
                                 _fin('anal', (0.56, 0.76), 0.3, prof=[(0, 1.0), (0.3, 0.8), (1, 0.5)]),
                                 _fin('caudal', h=0.32, shape='deepfork'),
                                 _fin('pectoral', s=(0.2, 0.2), h=0.15, spine=True), _fin('pelvic', s=(0.43, 0.43), h=0.1)],
                           barbels=[(0.03, -0.05, 0.035, 0.003, 'side'), (0.025, -0.4, 0.025, 0.002, 'down')],
                           colors=P('#222a30', '#8a96a0', '#eceeee', silver=0.6, metallic=0.3,
                                    fin_up=('#2a3036', '#14181c', 0.9), fin_low=('#3a4046', '#1e2226', 0.9),
                                    patterns=[('stripe', dict(v=0.62, w=0.03, color='#283038', a=0.5)),
                                              ('stripe', dict(v=0.42, w=0.025, color='#3a444c', a=0.35)), ('sheen', 0.4)]),
                           eye=dict(s=0.075, z=-0.1, r=0.016, iris='#c0c0b0'), anim=dict(amp=0.9, freq=0.9)),
    # 클라운 나이프피시 (Chitala ornata): 작고 뾰족한 머리 뒤로 목덜미가 오목하게 꺼졌다가 등이 높이 솟고, 꼬리로 칼처럼 가늘어지는 아주 납작한 몸.
    # 입이 눈 뒤까지 찢어진다. 등 가운데 깃털 같은 작은 등지느러미, 배를 따라 꼬리까지 이어지는 긴 뒷지느러미.
    # 은회색, 몸 뒤쪽 아래에 흰 테 두른 큰 검은 눈알 무늬 한 줄 (꼬리 쪽으로 갈수록 크다), 등에 잔 점
    'clownknifefish': dict(type='C', name='클라운 나이프피시', depth=0.25, width=0.07, tmax=0.34, body_end=0.95, ped=0.07, arch=0.72, snout=1.0, blunt=1.25,
                           mouth=0.1, gape=0.09, jaw=0.006, hump=-0.05, hump_s=0.16, xsec=1.5, scales=90,
                           fins=[_fin('dorsal', (0.42, 0.48), 0.18, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                                 _fin('anal', (0.3, 1.0), 0.34, prof=[(0, 0.3), (0.2, 1.0), (0.85, 0.7), (1, 0.4)]),
                                 _fin('caudal', h=0.1, shape='round'),
                                 _fin('pectoral', s=(0.24, 0.24), h=0.1), _fin('pelvic', s=(0.3, 0.3), h=0.03)],
                           colors=P('#5a6064', '#b8bcbc', '#ecece8', silver=0.6, metallic=0.2,
                                    fin_up=('#7a7e80', '#9a9e9e', 0.6), fin_low=('#9a9e9e', '#b8bcbc', 0.6),
                                    patterns=[('spots', dict(n=6, r=(27, 27), color='#ffffff', v=(0.38, 0.38), u=(0.52, 0.9), row=True, a=1.0)),
                                              ('spots', dict(n=6, r=(19, 19), color='#101214', v=(0.38, 0.38), u=(0.52, 0.9), row=True, a=0.95)),
                                              ('spots', dict(n=40, r=(1, 2), color='#3a3e40', v=(0.5, 1.0), a=0.4))]),
                           eye=dict(s=0.08, z=0.35, r=0.014, iris='#d0c890'), anim=dict(amp=0.8, freq=0.9)),
    # 월라고 (Wallago attu): 입꼬리가 눈 뒤까지 간다. 길고 옆으로 납작한 메기, 등선은 곧고 배가 깊다(가장 높은 곳이 머리 바로 뒤). 머리가 눌리고 눈 뒤까지 찢어진 아주 큰 입,
    # 아래턱이 나온다. 아주 작은 등지느러미, 몸 절반을 덮는 긴 뒷지느러미, 갈라진 꼬리, 뒷지느러미까지 닿는 긴 위턱 수염 한 쌍과 짧은 아래턱 수염.
    # 올리브 회색 등, 은백색 옆구리·배, 작은 눈이 입꼬리 위 앞쪽에
    'wallago': dict(type='H', name='월라고', depth=0.15, width=0.09, head_w=1.1, head_flat=0.15, tmax=0.26, body_end=0.86, ped=0.28, arch=0.42, snout=0.78,
                    blunt=0.95, mouth=0.05, gape=0.12, jaw=0.01, xsec=1.8,
                    fins=[_fin('dorsal', (0.28, 0.33), 0.3, prof=[(0, 1.0), (1, 0.5)]),
                          _fin('anal', (0.5, 0.97), 0.24, prof=[(0, 0.6), (0.2, 1.0), (1, 0.8)]),
                          _fin('caudal', h=0.2, shape='fork'),
                          _fin('pectoral', s=(0.18, 0.18), h=0.11), _fin('pelvic', s=(0.4, 0.4), h=0.06)],
                    barbels=[(0.03, 0.1, 0.26, 0.0035, 'side'), (0.02, -0.4, 0.04, 0.0022, 'down')],
                    colors=P('#444a40', '#a2a698', '#eeeee6', silver=0.45, metallic=0.15,
                             fin_up=('#545a50', '#3a3e38', 0.85), fin_low=('#9a9a8a', '#b8b8a8', 0.75),
                             patterns=[('sheen', 0.4), ('mottle', dict(color='#3a3e34', a=0.2, scale=0.07))]),
                    eye=dict(s=0.07, z=0.25, r=0.011, iris='#c0b880'), anim=dict(amp=1.0, freq=0.9)),
    # 붉은꼬리 메기 (Hemibagrus wyckioides): 위아래로 눌린 넓적한 머리, 길쭉한 몸, 가시 있는 세모 등지느러미, 길고 낮은 기름지느러미,
    # 수염 네 쌍 (위턱 수염은 배지느러미 너머까지 아주 길다), 끝이 뾰족하게 갈라진 새빨간 꼬리. 회갈색 등, 흰 배, 가슴·배지느러미는 회색.
    # 꼬리·뒷지느러미만 아랫지느러미 색 칸(low)으로 붉게, 가슴·배지느러미는 윗지느러미 색 칸(up)으로 회색
    'redtailcatfish': dict(type='H', name='붉은꼬리 메기', depth=0.17, width=0.13, head_w=1.2, head_flat=0.25, tmax=0.32, body_end=0.83, ped=0.36, snout=0.7,
                           blunt=0.9, mouth=-0.05, gape=0.07,
                           fins=[_fin('dorsal', (0.3, 0.38), 0.5, prof=[(0, 1.0), (0.4, 0.65), (1, 0.25)]),
                                 _fin('adipose', (0.56, 0.8), 0.12, prof=[(0, 0.3), (0.3, 1.0), (1, 0.6)]),
                                 _fin('anal', (0.66, 0.78), 0.3), _fin('caudal', h=0.28, shape='deepfork', group='low'),
                                 _fin('pectoral', s=(0.17, 0.17), h=0.14, spine=True, group='up'), _fin('pelvic', s=(0.42, 0.42), h=0.08, group='up')],
                           barbels=[(0.03, 0.15, 0.3, 0.0035, 'side'), (0.035, 0.35, 0.04, 0.0025, 'up'),
                                    (0.025, -0.3, 0.06, 0.0025, 'down'), (0.03, -0.42, 0.05, 0.0025, 'down')],
                           colors=P('#3e4246', '#86888a', '#e6e4dc', silver=0.2,
                                    fin_up=('#4a4e52', '#6a6e72', 0.85), fin_low=('#c8341e', '#e8442a', 0.9),
                                    patterns=[('mottle', dict(color='#2a3034', a=0.15, scale=0.07)), ('sheen', 0.3)]),
                           eye=dict(s=0.075, z=0.28, r=0.011, iris='#c0a050'), anim=dict(amp=0.85, freq=0.75)),
    # ===== 흔한 고기 =====
    # 줄무늬 스네이크헤드 (Channa striata): 원통형 가물치 몸, 위아래로 눌린 뱀 머리에 큰 비늘, 눈 뒤까지 찢어진 입(아래턱이 살짝 나온다).
    # 짙은 갈색 등, 옆구리에 비스듬한 짙은 V자 띠, 연한 배 위 경계에 짙은 얼룩 점. 긴 등·뒷지느러미, 둥근 꼬리
    'stripedsnakehead': dict(type='H', name='줄무늬 스네이크헤드', depth=0.15, width=0.13, head_w=1.05, head_flat=0.2, tmax=0.4, body_end=0.83, ped=0.75,
                             snout=0.78, blunt=0.92, mouth=0.05, gape=0.09, jaw=0.006, scales=60,
                             fins=[_fin('dorsal', (0.3, 0.98), 0.3, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                   _fin('anal', (0.55, 0.98), 0.28, prof=[(0, 0.6), (0.2, 1.0), (1, 0.9)]),
                                   _fin('caudal', h=0.15, shape='round'), _fin('pectoral', s=(0.24, 0.24), h=0.11), _fin('pelvic', s=(0.33, 0.33), h=0.07)],
                             colors=P('#2e2a1e', '#6a5e44', '#ddd6be', silver=0.0,
                                      fin_up=('#4a4232', '#3a3426', 0.9), fin_low=('#6a604a', '#5a5040', 0.9),
                                      patterns=[('obliquebands', dict(n=11, color='#241e14', a=0.45)), ('mottle', dict(color='#2a241a', a=0.3, scale=0.06)),
                                                ('spots', dict(n=40, r=(2, 4), color='#241e14', v=(0.2, 0.42), u=(0.2, 0.9), a=0.5)),
                                                ('eyeline', dict(color='#241e14', w=0.022, n=1))]),
                             eye=dict(s=0.06, z=0.4, r=0.015, iris='#c09a50')),
    # 햄팔라 바브 (Hampala macrolepidota): 뾰족한 주둥이에 눈 밑까지 오는 큰 입 (잉어과 육식어), 큰 비늘의 은빛 몸.
    # 등지느러미 앞 밑에서 옆구리 가운데로 내려오는 검은 쐐기 띠, 붉은 주황 지느러미, 꼬리 위아래 끝 가장자리가 검다
    'hampalabarb': dict(type='B', name='햄팔라 바브', depth=0.26, width=0.11, tmax=0.4, arch=0.6, snout=0.9, blunt=1.15, mouth=0.05, gape=0.07, scales=34,
                        fins=[_fin('dorsal', (0.42, 0.54), 0.65, prof=[(0, 0.6), (0.15, 1.0), (1, 0.35)]),
                              _fin('anal', (0.66, 0.78), 0.45), _fin('caudal', h=0.28, shape='deepfork'),
                              _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.48, 0.48), h=0.1)],
                        colors=P('#4a5650', '#c0c6c0', '#f2f2ec', silver=0.75, metallic=0.2,
                                 fin_up=('#c84a2a', '#1a1a18', 0.85), fin_low=('#d0502e', '#e06a40', 0.85),
                                 patterns=[('bands', dict(n=1, u=(0.42, 0.42), w=0.06, color='#141414', v=(0.38, 1.0), a=0.85)), ('scale_edge', 0.6)]),
                        eye=dict(iris='#d8b060'), anim=dict(amp=1.0, freq=1.2)),
    # 자이언트 구라미 (Osphronemus goramy, 어른): 높은 타원형 몸, 작은 머리 위로 이마가 혹처럼 솟고(hump) 아주 두툼한 입술과 앞으로 나온 아래턱.
    # 눈은 작고 입 높이 가까이 낮게 붙는다 (머리 대부분이 눈 위 이마)
    # 등지느러미는 몸 가운데 뒤에서 시작해 뒤로 길게 뻗고, 뒷지느러미는 더 길다, 실 같은 배지느러미, 둥근 꼬리. 분홍빛 도는 회갈색에 희미한 세로 띠
    'giantgourami': dict(type='D', name='자이언트 구라미', depth=0.46, width=0.14, tmax=0.45, arch=0.6, snout=0.8, blunt=1.0, mouth=0.0, hump=0.12, hump_s=0.2,
                         lips=2.6, jaw=0.012, gape=0.035, scales=32,
                         fins=[_fin('dorsal', (0.5, 0.82), 0.3, prof=[(0, 0.4), (0.4, 0.8), (1, 1.2)], spiny=True),
                               _fin('anal', (0.35, 0.84), 0.35, prof=[(0, 0.4), (0.4, 0.8), (1, 1.2)], spiny=True),
                               _fin('caudal', h=0.28, shape='round'),
                               _fin('pectoral', s=(0.3, 0.3), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.45, thread=True)],
                         colors=P('#4e4840', '#9a8c80', '#e2d8cc', silver=0.25,
                                  fin_up=('#5a524a', '#4a423a', 0.85), fin_low=('#7a6a5e', '#9a8a7a', 0.85),
                                  patterns=[('bands', dict(n=7, u=(0.25, 0.85), w=0.05, color='#5a5048', v=(0.2, 1.0), a=0.3)), ('scale_edge', 0.55)]),
                         eye=dict(s=0.12, z=0.1, r=0.017, iris='#c0a040'), anim=dict(amp=0.75, freq=0.8)),
    # 파쿠 (Piaractus brachypomus): 피라냐처럼 높고 납작한 둥근 몸, 뭉툭한 머리에 작은 입, 작은 기름지느러미. 짙은 회색 등·옆구리,
    # 큰 눈이 입 가까이 앞쪽 낮게, 입술이 도톰한 입이 살짝 위를 본다.
    # 가슴·목 아래만 주황빛 붉은색(배 전체가 아니다), 붉은 배·뒷지느러미 끝은 거뭇하고, 등·꼬리지느러미는 짙은 먹색
    'pacu': dict(type='D', name='파쿠', depth=0.5, width=0.12, tmax=0.42, body_end=0.82, ped=0.28, arch=0.55, snout=0.55, blunt=1.0, mouth=0.15, gape=0.035, lips=1.4, scales=70,
                 fins=[_fin('dorsal', (0.42, 0.56), 0.25, prof=[(0, 0.8), (0.25, 1.0), (1, 0.35)]),
                       _fin('adipose', (0.74, 0.78), 0.08, prof=[(0, 0.5), (0.5, 1.0), (1, 0.6)]),
                       _fin('anal', (0.6, 0.84), 0.22, prof=[(0, 1.0), (0.3, 0.7), (1, 0.6)]),
                       _fin('caudal', h=0.3, shape='fork'),
                       _fin('pectoral', s=(0.27, 0.27), h=0.13), _fin('pelvic', s=(0.42, 0.42), h=0.1)],
                 colors=P('#2e3438', '#80888a', '#c4c2ba', silver=0.5, metallic=0.2,
                          fin_up=('#3a4044', '#22262a', 0.9), fin_low=('#c85a30', '#2a2420', 0.85),
                          patterns=[('throat', '#d0502a'), ('spots', dict(n=50, r=(1, 3), color='#2e3438', v=(0.3, 1.0), a=0.25))]),
                 eye=dict(s=0.1, z=0.15, r=0.022, iris='#c8783a'), anim=dict(amp=0.85, freq=1.0)),
    # 실버 바브 (Barbonymus gonionotus): 작고 뾰족한 머리 뒤로 등이 가파르게 솟는 높은 은빛 잉어, 끝에 작은 입과 작은 수염.
    # 톱니 가시가 있는 높은 세모 등지느러미, 작은 뒷지느러미, 깊게 갈라진 꼬리. 큰 비늘에 거뭇한 테, 노란 배·뒷지느러미
    'silverbarb': dict(type='A', name='실버 바브', depth=0.37, width=0.12, tmax=0.42, arch=0.66, snout=0.85, blunt=1.05, mouth=0.0, gape=0.025, scales=28,
                       fins=[_fin('dorsal', (0.38, 0.5), 0.7, prof=[(0, 0.5), (0.12, 1.0), (1, 0.25)], spine=True),
                             _fin('anal', (0.7, 0.79), 0.35, prof=[(0, 0.7), (0.2, 1.0), (1, 0.4)]), _fin('caudal', h=0.28, shape='deepfork'),
                             _fin('pectoral', s=(0.24, 0.24), h=0.12), _fin('pelvic', s=(0.5, 0.5), h=0.1)],
                       barbels=[(0.03, -0.25, 0.02, 0.002, 'down')],
                       colors=P('#5a6250', '#ccccba', '#f4f2e6', silver=0.85, metallic=0.3,
                                fin_up=('#8a8a70', '#a0a080', 0.65), fin_low=('#d8b850', '#e8c868', 0.75),
                                patterns=[('scale_edge', 0.7), ('sheen', 0.5)]),
                       eye=dict(s=0.09, r=0.022, iris='#d8c070'), anim=dict(amp=1.0, freq=1.2)),
    # 문라이트 구라미 (Trichopodus microlepis): 작고 뾰족한 머리에 위로 열린 작은 입, 머리 뒤 등선이 오목하다가 뒤쪽(뒷지느러미 앞)이 가장 높은
    # 마름모꼴 납작한 몸. 아주 잔 비늘의 달빛 같은 은녹색, 몸 길이만큼 긴 실 같은 배지느러미, 뒤로 넓어지는 긴 뒷지느러미, 뒤쪽 작은 등지느러미
    'moonlightgourami': dict(type='D', name='문라이트 구라미', depth=0.38, width=0.09, tmax=0.5, arch=0.5, snout=0.95, blunt=1.15, mouth=0.55, mouth_slope=-0.8,
                             gape=0.025, scales=0,
                             fins=[_fin('dorsal', (0.58, 0.74), 0.2, prof=[(0, 0.5), (0.5, 1.0), (1, 0.8)]),
                                   _fin('anal', (0.3, 0.88), 0.3, prof=[(0, 0.3), (0.3, 0.8), (1, 1.1)]),
                                   _fin('caudal', h=0.28, shape='emarg'),
                                   _fin('pectoral', s=(0.28, 0.28), h=0.12), _fin('pelvic', s=(0.3, 0.3), h=0.6, thread=True)],
                             colors=P('#7a8c84', '#d4dcd8', '#f4f6f2', silver=0.9, metallic=0.35,
                                      fin_up=('#c0ccc8', '#e0e8e4', 0.45), fin_low=('#c8d0cc', '#e8ece8', 0.45),
                                      patterns=[('sheen', 0.8)]),
                             eye=dict(s=0.13, z=0.3, r=0.026, iris='#c84030'), anim=dict(amp=0.8, freq=1.0)),
}
