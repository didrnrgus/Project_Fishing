"""지중해 거점 어종 (docs/fish-db/03_지중해_어종DB.md) — 설계 시트 형식은 fish_species.py 머리말."""
from fish_species import P, _fin  # noqa: F401

SPECIES = {
    # ===== 외해 회유어 =====
    # 날개다랑어 (Thunnus alalunga): 다랑어 방추형, 몸 절반을 넘는 긴 칼 모양 가슴지느러미, 짙은 남색 등, 꼬리 뒤 가장자리가 희다.
    # 원뿔처럼 곧게 모이는 머리(blunt)와 눈 밑까지 오는 입, 등의 남색이 은빛 옆구리와 또렷하게 갈린다(넓은 stripe), 큰 눈
    'albacore': dict(type='E', name='날개다랑어', depth=0.24, width=0.17, tmax=0.4, ped=0.1, snout=0.92, mouth=0.05, blunt=1.2, mouth_h=0.13, gape=0.075,
                     fins=[_fin('dorsal', (0.32, 0.44), 0.38, prof=[(0, 1.0), (1, 0.25)], spiny=True), _fin('dorsal', (0.52, 0.6), 0.45, prof=[(0, 1.0), (1, 0.3)]),
                           _fin('anal', (0.56, 0.64), 0.42, prof=[(0, 1.0), (1, 0.3)]), _fin('caudal', h=0.4, shape='deepfork'),
                           _fin('pectoral', s=(0.27, 0.27), h=0.36, long=True), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                     colors=P('#14204a', '#7c8ca0', '#eceef0', silver=0.8, metallic=0.45, fin_up=('#18244a', '#c8ccd4', 0.85), fin_low=('#a0a8b4', '#c8ccd4', 0.7),
                              patterns=[('stripe', dict(v=0.92, w=0.2, color='#101a3e', a=0.85)), ('finlets', True),
                                        ('stripe', dict(v=0.66, w=0.035, color='#5a7aa8', a=0.45)), ('sheen', 0.5)]),
                     eye=dict(r=0.027, iris='#9aa2a8'), anim=dict(amp=0.9, freq=1.6)),
    # 대서양점다랑어 (Euthynnus alletteratus): 작고 통통한 다랑어, 원뿔처럼 모이는 머리(blunt)와 눈 밑까지 오는 입.
    # 앞이 높고 뒤로 낮아지는 오목한 첫 등지느러미, 짧은 가슴지느러미. 짙은 남색 등의 몸 뒤쪽 절반에만 굵은 물결무늬,
    # 가슴지느러미 아래 검은 점 몇 개, 비늘 없는 은백색 배
    'littletunny': dict(type='E', name='대서양점다랑어', depth=0.25, width=0.17, tmax=0.4, ped=0.1, snout=0.92, mouth=0.05, blunt=1.2, mouth_h=0.13, gape=0.075,
                        fins=[_fin('dorsal', (0.3, 0.46), 0.45, prof=[(0, 1.0), (0.3, 0.55), (1, 0.12)], spiny=True), _fin('dorsal', (0.52, 0.58), 0.3, prof=[(0, 1.0), (1, 0.35)]),
                              _fin('anal', (0.56, 0.62), 0.28, prof=[(0, 1.0), (1, 0.35)]), _fin('caudal', h=0.28, shape='deepfork'),
                              _fin('pectoral', s=(0.26, 0.26), h=0.13), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                        colors=P('#142a48', '#8c9eae', '#f2f4f2', silver=0.7, metallic=0.3, fin_up=('#1e2e44', '#3a4a5c', 0.85), fin_low=('#a8b4bc', '#c8d0d4', 0.65),
                                 patterns=[('stripe', dict(v=0.9, w=0.26, color='#1a3050', a=0.85)),
                                           ('waves', dict(color='#0a1626', v=(0.62, 1.0), a=1.0)), ('finlets', True),
                                           ('spots', dict(n=5, r=(8, 11), color='#141c24', u=(0.27, 0.4), v=(0.3, 0.42), a=0.9))]),
                        eye=dict(r=0.024, iris='#9aa2a8'), anim=dict(amp=1.0, freq=1.6)),
    # 대서양보니토 (Sarda sarda): 다랑어 중 날씬하고 머리가 길고 뾰족하며 입이 눈 뒤까지 크게 찢어진다. 길고 낮은 첫 등지느러미가
    # 둘째 등지느러미 바로 앞까지 이어진다. 짙은 청색 등에 뒤로 갈수록 살짝 올라가는 검은 줄이 7~8줄, 은빛 옆구리
    'atlanticbonito': dict(type='E', name='대서양보니토', depth=0.2, width=0.13, tmax=0.42, ped=0.11, snout=0.95, mouth=0.08, blunt=1.25, mouth_h=0.12, gape=0.095,
                           fins=[_fin('dorsal', (0.28, 0.52), 0.3, prof=[(0, 1.0), (0.4, 0.65), (1, 0.15)], spiny=True), _fin('dorsal', (0.54, 0.6), 0.28, prof=[(0, 1.0), (1, 0.35)]),
                                 _fin('anal', (0.57, 0.63), 0.26, prof=[(0, 1.0), (1, 0.35)]), _fin('caudal', h=0.3, shape='deepfork'),
                                 _fin('pectoral', s=(0.25, 0.25), h=0.1), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                           colors=P('#1e3656', '#a8b6c2', '#f4f6f4', silver=0.8, metallic=0.35, fin_up=('#2a3a50', '#46566a', 0.8), fin_low=('#b4bec4', '#ccd4d8', 0.6),
                                    patterns=[('stripe', dict(v=0.92, w=0.16, color='#1a2e4a', a=0.75)),
                                              ('hstripes', dict(n=8, v=(0.6, 0.98), w=0.014, color='#0c1624', a=0.9)), ('finlets', True)]),
                           eye=dict(s=0.1, r=0.02, iris='#c8c8b0'), anim=dict(amp=1.1, freq=1.5)),
    # ===== 전갱이과 대물 =====
    # 잿방어 (Seriola dumerili): 방어류 중 체고가 가장 높고 머리가 뭉툭하다. 짧고 낮은 가시 등지느러미 뒤로 앞이 솟은 긴 등·뒷지느러미.
    # 주둥이에서 눈을 지나 목덜미로 오르는 짙은 띠(어릴수록 또렷), 갈색빛 올리브 등과 은빛 옆구리, 희미한 호박색 세로띠, 거무스름한 지느러미
    'greateramberjack': dict(type='E', name='잿방어', depth=0.27, width=0.13, tmax=0.38, ped=0.15, snout=0.62, blunt=0.95, mouth=0.0, gape=0.07,
                             fins=[_fin('dorsal', (0.3, 0.4), 0.15, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)], spiny=True),
                                   _fin('dorsal', (0.42, 0.8), 0.42, prof=[(0, 1.0), (0.15, 0.7), (0.35, 0.36), (1, 0.25)]),
                                   _fin('anal', (0.58, 0.8), 0.34, prof=[(0, 1.0), (0.2, 0.6), (0.4, 0.35), (1, 0.25)]),
                                   _fin('caudal', h=0.33, shape='deepfork'), _fin('pectoral', s=(0.26, 0.26), h=0.11), _fin('pelvic', s=(0.31, 0.31), h=0.1)],
                             colors=P('#4a4434', '#aaa494', '#eeece4', silver=0.6, metallic=0.25, fin_up=('#4e4838', '#6a5a3a', 0.85), fin_low=('#8a8470', '#a8986a', 0.75),
                                      patterns=[('stripe', dict(v=0.92, w=0.2, color='#3e3a2c', a=0.65)), ('stripe', dict(v=0.56, w=0.03, color='#d0a040', a=0.45)),
                                                ('bands', dict(n=1, u=(0.12, 0.12), w=0.045, color='#26221c', v=(0.5, 1.0), a=0.85))]),
                             eye=dict(s=0.1, r=0.02, iris='#c8b880'), anim=dict(amp=1.0, freq=1.15)),
    # 리어피시 (Lichia amia): 옆으로 납작하고 높은 은빛 몸에 뾰족한 머리, 눈 뒤까지 오는 큰 입. 따로 떨어진 짧은 가시 몇 개 뒤로
    # 낫처럼 높이 솟은 둘째 등·뒷지느러미(끝이 검다), 깊게 갈라진 검은 테 꼬리. 짙은 회청색 등이 은빛 옆구리와 또렷이 갈린다.
    # 타협: 실물의 S자로 굽이치는 옆줄은 곧은 줄로
    'leerfish': dict(type='E', name='리어피시', depth=0.3, width=0.1, tmax=0.4, ped=0.11, snout=0.85, blunt=1.1, mouth=0.05, mouth_h=0.15, gape=0.09, xsec=1.8,
                     fins=[_fin('dorsal', (0.3, 0.4), 0.1, prof=[(0, 1.0), (1, 0.8)], spiny=True),
                           _fin('dorsal', (0.45, 0.8), 0.5, prof=[(0, 1.0), (0.18, 0.5), (0.4, 0.28), (1, 0.2)]),
                           _fin('anal', (0.52, 0.8), 0.45, prof=[(0, 1.0), (0.18, 0.5), (0.4, 0.28), (1, 0.2)]),
                           _fin('caudal', h=0.33, shape='deepfork'), _fin('pectoral', s=(0.27, 0.27), h=0.12), _fin('pelvic', s=(0.33, 0.33), h=0.08)],
                     colors=P('#2e3c44', '#c4cacc', '#f6f6f4', silver=0.85, metallic=0.3, fin_up=('#56626a', '#14181c', 0.9), fin_low=('#c8ccc8', '#3a4044', 0.75),
                              patterns=[('stripe', dict(v=0.86, w=0.2, color='#26323a', a=0.9)),
                                        ('stripe', dict(v=0.6, w=0.012, color='#2a3236', a=0.8))]),
                     eye=dict(s=0.1, r=0.019, iris='#d8d0b0'), anim=dict(amp=1.0, freq=1.3)),
    # ===== 암초의 고급 어종 =====
    # 덴텍스 (Dentex dentex): 도미류 중 머리가 가장 크고 이마가 가파르게 떨어진다. 입이 크고 앞쪽에 굵은 송곳니(teeth), 아래턱이 조금 나온다.
    # 몸은 길쭉한 도미형, 등은 분홍빛 도는 갈색, 옆구리 아래와 배는 은백색, 등 쪽에 짙은 남색 잔점이 드문드문, 눈은 머리에 비해 작다
    'dentex': dict(type='D', name='덴텍스', depth=0.34, width=0.13, tmax=0.36, snout=0.52, blunt=1.05, mouth=-0.02, mouth_h=0.21, gape=0.075, jaw=0.008,
                   teeth=True, lips=1.1, scales=60,
                   fins=[_fin('dorsal', (0.28, 0.6), 0.36, prof=[(0, 0.45), (0.18, 1.0), (1, 0.6)], spiny=True),
                         _fin('dorsal', (0.6, 0.8), 0.28, prof=[(0, 0.75), (1, 0.55)]),
                         _fin('anal', (0.63, 0.8), 0.28, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                         _fin('caudal', h=0.28, shape='fork'),
                         _fin('pectoral', s=(0.3, 0.3), h=0.22, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.12)],
                   colors=P('#8a5c56', '#d0b4ae', '#f6f2f0', silver=0.65, metallic=0.2, fin_up=('#9a6c64', '#b88476', 0.8), fin_low=('#d4b4a8', '#e2cabe', 0.7),
                            patterns=[('spots', dict(n=40, r=(3, 4.5), color='#1e2a4a', v=(0.62, 1.0), u=(0.18, 0.75), a=0.8)),
                                      ('hstripes', dict(n=5, v=(0.45, 0.8), w=0.006, color='#a07a74', a=0.3))]),
                   eye=dict(s=0.12, z=0.45, r=0.019, iris='#d8b040')),
    # 더스키 그루퍼 (Epinephelus marginatus): 두툼한 그루퍼 몸에 큰 머리, 두툼한 입술과 앞으로 나온 아래턱, 작은 눈이 머리 위쪽에.
    # 짙은 회갈색·올리브 바탕에 흐린 크림색 얼룩이 흩어지고 배는 누르스름, 둥근 꼬리, 지느러미 끝에 흰 테
    'duskygrouper': dict(type='C', name='더스키 그루퍼', depth=0.3, width=0.17, tmax=0.38, head_w=1.15, mouth=0.25, mouth_h=0.22, snout=0.72, blunt=0.9,
                         gape=0.1, jaw=0.02, lips=1.6, scales=110,
                         fins=[_fin('dorsal', (0.28, 0.58), 0.28, prof=[(0, 0.5), (0.2, 1.0), (1, 0.75)], spiny=True), _fin('dorsal', (0.58, 0.8), 0.4),
                               _fin('anal', (0.62, 0.8), 0.4), _fin('caudal', h=0.22, shape='round'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.17), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                         colors=P('#2e2e26', '#5a5644', '#a8986a', silver=0.05, fin_up=('#34322a', '#c8c4b0', 0.9), fin_low=('#4a4434', '#b0a888', 0.9),
                                  patterns=[('spots', dict(n=70, r=(5, 11), color='#b4b29a', v=(0.25, 1.0), u=(0.08, 0.92), a=0.4)),
                                            ('mottle', dict(color='#1c1c16', a=0.35))]),
                         eye=dict(s=0.1, z=0.45, r=0.015, iris='#8a7a40'), anim=dict(amp=0.8, freq=0.7)),
    # 지중해민어 (Argyrosomus regius): 길쭉하고 굵은 민어형, 큰 머리에 둥근 주둥이와 눈 밑까지 오는 큰 입. 가시 등지느러미 바로 뒤로
    # 낮고 긴 등지느러미, 짧은 뒷지느러미, 가운데가 살짝 튀어나온 곧은 꼬리. 청동빛 은회색, 짙은 회갈색 등, 휘어진 밝은 옆줄,
    # 지느러미는 짙은 갈색이고 아래 지느러미는 붉은 갈색
    'meagre': dict(type='C', name='지중해민어', depth=0.25, width=0.14, tmax=0.36, arch=0.6, snout=0.68, blunt=0.95, mouth=0.1, mouth_h=0.2, gape=0.085, scales=65,
                   fins=[_fin('dorsal', (0.3, 0.44), 0.42, prof=[(0, 0.5), (0.3, 1.0), (1, 0.35)], spiny=True),
                         _fin('dorsal', (0.45, 0.8), 0.26, prof=[(0, 0.7), (0.15, 1.0), (1, 0.8)]),
                         _fin('anal', (0.66, 0.76), 0.28, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]), _fin('caudal', h=0.22, shape='truncate'),
                         _fin('pectoral', s=(0.26, 0.26), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                   colors=P('#3e3e36', '#8e8a78', '#dcd6c4', silver=0.5, metallic=0.3, fin_up=('#4a4236', '#5a4a38', 0.85), fin_low=('#5a4434', '#6e5040', 0.85),
                            patterns=[('stripe', dict(v=0.66, w=0.008, color='#c8c098', a=0.5)), ('sheen', 0.4)]),
                   eye=dict(s=0.1, z=0.4, r=0.017, iris='#b09050'), anim=dict(amp=0.8, freq=0.7)),
    # ===== 도미류 =====
    # 판도라 (Pagellus erythrinus): 도미류 중 가장 날씬하고 주둥이가 뾰족하며 눈이 크다. 분홍빛 은색 몸(배는 은백색),
    # 등 쪽에 하늘색 잔점, 아가미 뚜껑 뒤 가장자리와 가슴지느러미 밑동에 붉은 점, 분홍빛 꼬리
    'pandora': dict(type='D', name='판도라', depth=0.3, width=0.12, tmax=0.4, snout=0.8, blunt=1.12, mouth=-0.05, gape=0.045, lips=1.2, scales=50,
                    fins=[_fin('dorsal', (0.3, 0.62), 0.34, prof=[(0, 0.4), (0.18, 1.0), (1, 0.6)], spiny=True),
                          _fin('dorsal', (0.62, 0.8), 0.26, prof=[(0, 0.75), (1, 0.55)]),
                          _fin('anal', (0.63, 0.8), 0.26, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                          _fin('caudal', h=0.29, shape='fork'),
                          _fin('pectoral', s=(0.3, 0.3), h=0.22, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.11)],
                    colors=P('#c8787a', '#e8b8b4', '#f8f0ee', silver=0.6, metallic=0.2, fin_up=('#d8948e', '#e8a29a', 0.7), fin_low=('#ecc4bc', '#f4d6d0', 0.65),
                             patterns=[('spots', dict(n=60, r=(3, 4.5), color='#5ab8ec', v=(0.55, 1.0), u=(0.15, 0.8), a=0.95)),
                                       ('bands', dict(n=1, u=(0.215, 0.215), w=0.012, color='#c02a30', v=(0.4, 0.75), a=0.7)),
                                       ('spots', dict(n=1, r=(5, 5), u=(0.3, 0.3), v=(0.42, 0.42), color='#b0222a', a=0.75))]),
                    eye=dict(s=0.12, z=0.42, r=0.03, iris='#e0b040')),
    # 지중해참돔 (Pagrus pagrus): 판도라보다 높은 몸, 둥글게 가파른 이마, 눈은 작은 편. 은빛 도는 분홍빛 붉은 몸에
    # 비늘 줄마다 가는 세로 줄, 희미한 하늘색 잔점, 붉은 꼬리의 두 끝이 하얗다 (점 무늬는 판도라보다 훨씬 흐리다)
    'redporgy': dict(type='D', name='지중해참돔', depth=0.38, width=0.12, tmax=0.36, snout=0.5, blunt=0.95, mouth=-0.18, gape=0.045, lips=1.2, scales=50,
                     fins=[_fin('dorsal', (0.28, 0.6), 0.42, prof=[(0, 0.4), (0.2, 1.0), (1, 0.55)], spiny=True),
                           _fin('dorsal', (0.6, 0.8), 0.3, prof=[(0, 0.75), (1, 0.55)]),
                           _fin('anal', (0.62, 0.8), 0.3, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                           _fin('caudal', h=0.29, shape='fork'),
                           _fin('pectoral', s=(0.3, 0.3), h=0.24, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.12)],
                     colors=P('#b05050', '#dc9c94', '#f6eae4', silver=0.5, metallic=0.2, fin_up=('#c86660', '#f2e6e2', 0.8), fin_low=('#e8a8a0', '#f2c8c0', 0.7),
                              patterns=[('hstripes', dict(n=9, v=(0.3, 0.85), w=0.006, color='#a85a56', a=0.45)),
                                        ('spots', dict(n=40, r=(2, 3), color='#9ac0d8', v=(0.5, 1.0), u=(0.15, 0.8), a=0.35))]),
                     eye=dict(s=0.13, z=0.44, r=0.022, iris='#e0b040')),
    # 흰도미 (Diplodus sargus): 아주 높은 타원형 몸, 뾰족한 주둥이에 도톰한 입술. 은회색에 금빛이 돌고 가는 검은 가로 띠 9줄
    # (진한 줄과 흐린 줄이 번갈아), 꼬리자루 위에 둥근 검은 점, 꼬리 가장자리가 검고 배지느러미가 검다
    'whiteseabream': dict(type='D', name='흰도미', depth=0.43, width=0.12, tmax=0.38, snout=0.62, blunt=1.0, mouth=-0.15, gape=0.035, lips=1.35, scales=50,
                          colors=P('#5c6258', '#bcc0b4', '#f0f0ea', silver=0.7, metallic=0.2, fin_up=('#6a7068', '#2a2e2c', 0.85), fin_low=('#a0a49c', '#6a6e68', 0.75),
                                   patterns=[('bands', dict(n=5, u=(0.26, 0.78), w=0.034, color='#22282a', v=(0.3, 1.0), a=0.45)),
                                             ('bands', dict(n=4, u=(0.325, 0.715), w=0.014, color='#2a3032', v=(0.4, 1.0), a=0.3)),
                                             ('bands', dict(n=1, u=(0.875, 0.875), w=0.05, color='#0e1012', v=(0.42, 0.98), a=0.92))]),
                          eye=dict(s=0.13, z=0.42, r=0.024, iris='#c8c0a0')),
    # 두줄도미 (Diplodus vulgaris): 흰도미처럼 높은 몸, 목덜미(등지느러미 앞 ~ 가슴지느러미 밑동)와 꼬리자루를 두른 굵은 검은 띠 두 줄,
    # 그 사이 은회색에 노란 기 도는 가는 세로줄, 누르스름한 꼬리
    'twobandedbream': dict(type='D', name='두줄도미', depth=0.41, width=0.12, tmax=0.38, snout=0.62, blunt=1.0, mouth=-0.15, gape=0.035, lips=1.35, scales=50,
                           colors=P('#666a62', '#c4c4b8', '#f2f2ea', silver=0.65, metallic=0.2, fin_up=('#7a7868', '#c8b878', 0.8), fin_low=('#c0b890', '#d4cca8', 0.7),
                                    patterns=[('hstripes', dict(n=10, v=(0.25, 0.85), w=0.006, color='#9a9468', a=0.5)),
                                              ('bands', dict(n=1, u=(0.225, 0.225), w=0.075, color='#141618', v=(0.3, 1.0), a=0.92)),
                                              ('bands', dict(n=1, u=(0.88, 0.88), w=0.08, color='#141618', v=(0.05, 1.0), a=0.92))]),
                           eye=dict(s=0.13, z=0.42, r=0.025, iris='#c8c0a0')),
    # 안장도미 (Oblada melanura): 도미류 중 길쭉하고(depth 0.3) 주둥이가 짧고 둥글며 눈이 아주 크다. 은회색에 가는 세로줄,
    # 꼬리자루에 앞뒤로 흰 테를 두른 검은 안장 무늬, 깊게 갈라진 꼬리
    'saddledbream': dict(type='D', name='안장도미', depth=0.3, width=0.12, tmax=0.4, snout=0.48, blunt=0.9, mouth=0.0, gape=0.035, scales=55,
                         fins=[_fin('dorsal', (0.3, 0.62), 0.3, prof=[(0, 0.4), (0.18, 1.0), (1, 0.6)], spiny=True), _fin('dorsal', (0.62, 0.8), 0.3),
                               _fin('anal', (0.62, 0.8), 0.3, spiny=True), _fin('caudal', h=0.31, shape='deepfork'),
                               _fin('pectoral', s=(0.3, 0.3), h=0.18, long=True), _fin('pelvic', s=(0.35, 0.35), h=0.1)],
                         colors=P('#5e6a72', '#c4cacc', '#f2f4f2', silver=0.75, metallic=0.25, fin_up=('#6a747a', '#8a9498', 0.7), fin_low=('#c0c4c4', '#d4d8d8', 0.6),
                                  patterns=[('hstripes', dict(n=9, v=(0.25, 0.85), w=0.006, color='#6a767c', a=0.45)),
                                            ('bands', dict(n=1, u=(0.88, 0.88), w=0.15, color='#f8f8f4', v=(0.0, 1.0), a=0.95)),
                                            ('bands', dict(n=1, u=(0.88, 0.88), w=0.08, color='#0e1012', v=(0.08, 0.97), a=0.95))]),
                         eye=dict(s=0.11, z=0.38, r=0.034, iris='#c8c8b8')),
    # 살레마 (Sarpa salpa): 방추형에 가까운 타원 몸, 짧고 뭉툭한 주둥이에 작은 입. 은청색 바탕에 금빛 세로줄 열 줄 남짓,
    # 크고 샛노란 눈, 가슴지느러미 밑동에 검은 점, 꼬리는 누르스름하다
    'salema': dict(type='D', name='살레마', depth=0.29, width=0.13, tmax=0.4, snout=0.42, blunt=0.85, mouth=-0.05, gape=0.03, scales=50,
                   fins=[_fin('dorsal', (0.3, 0.62), 0.28, prof=[(0, 0.4), (0.18, 1.0), (1, 0.6)], spiny=True), _fin('dorsal', (0.62, 0.8), 0.28),
                         _fin('anal', (0.62, 0.8), 0.28, spiny=True), _fin('caudal', h=0.28, shape='fork'),
                         _fin('pectoral', s=(0.3, 0.3), h=0.15), _fin('pelvic', s=(0.35, 0.35), h=0.1)],
                   colors=P('#5a6a78', '#aab8c0', '#eceeec', silver=0.6, metallic=0.2, fin_up=('#6a7680', '#b8a868', 0.7), fin_low=('#b0b8bc', '#c8c8b0', 0.6),
                            patterns=[('hstripes', dict(n=12, v=(0.12, 0.94), w=0.009, color='#e8c040', a=0.85)),
                                      ('spots', dict(n=1, r=(5, 5), u=(0.3, 0.3), v=(0.42, 0.42), color='#1a1e22', a=0.85))]),
                   eye=dict(s=0.11, z=0.38, r=0.028, iris='#f0d020')),
    # ===== 이빨 있는 표층 포식자 =====
    # 블루피시 (Pomatomus saltatrix): 굵은 방추형에 큰 머리, 눈 뒤까지 오는 큰 입과 이빨, 튀어나온 아래턱. 낮은 가시 등지느러미 뒤로
    # 긴 둘째 등지느러미와 뒷지느러미가 마주 본다. 청록색 등과 은빛 옆구리, 가슴지느러미 밑동에 검은 점
    'bluefish': dict(type='E', name='블루피시', depth=0.22, width=0.12, tmax=0.4, ped=0.2, snout=0.85, blunt=1.05, mouth=0.15, mouth_h=0.17, gape=0.09, jaw=0.012, teeth=True,
                     fins=[_fin('dorsal', (0.3, 0.4), 0.22, prof=[(0, 0.7), (0.3, 1.0), (1, 0.5)], spiny=True),
                           _fin('dorsal', (0.43, 0.77), 0.3, prof=[(0, 1.0), (0.3, 0.7), (1, 0.4)]),
                           _fin('anal', (0.55, 0.77), 0.27, prof=[(0, 1.0), (0.3, 0.7), (1, 0.4)]),
                           _fin('caudal', h=0.3, shape='fork'), _fin('pectoral', s=(0.26, 0.26), h=0.12), _fin('pelvic', s=(0.3, 0.3), h=0.08)],
                     colors=P('#24545e', '#a4bcc2', '#f2f4f2', silver=0.8, metallic=0.3, fin_up=('#3a6066', '#52707a', 0.8), fin_low=('#c0c8c4', '#d4dad6', 0.6),
                              patterns=[('stripe', dict(v=0.9, w=0.2, color='#1e4a54', a=0.75)),
                                        ('spots', dict(n=1, r=(9, 9), u=(0.26, 0.26), v=(0.4, 0.4), color='#141a1e', a=0.85)), ('sheen', 0.4)]),
                     eye=dict(s=0.1, r=0.019, iris='#d0c890'), anim=dict(amp=1.1, freq=1.35)),
    # 유럽꼬치고기 (Sphyraena sphyraena): 꼬치고기형 긴 몸, 길고 뾰족한 머리에 송곳니와 앞으로 튀어나온 아래턱, 멀리 떨어진 등지느러미 둘.
    # 회청색 등에 짙은 가로 줄이 20개 남짓 등 쪽에만, 은빛 옆구리, 누르스름한 꼬리
    'europeanbarracuda': dict(type='E', name='유럽꼬치고기', depth=0.11, width=0.09, tmax=0.45, body_end=0.85, ped=0.3, snout=0.95, blunt=1.3, mouth=0.1, mouth_h=0.14,
                              gape=0.08, jaw=0.015, teeth=True,
                              fins=[_fin('dorsal', (0.38, 0.45), 0.5, prof=[(0, 1.0), (1, 0.3)], spiny=True), _fin('dorsal', (0.68, 0.74), 0.45),
                                    _fin('anal', (0.68, 0.75), 0.4), _fin('caudal', h=0.22, shape='deepfork'),
                                    _fin('pectoral', s=(0.24, 0.24), h=0.08), _fin('pelvic', s=(0.42, 0.42), h=0.07)],
                              colors=P('#34485a', '#bcc8ce', '#f2f4f4', silver=0.85, metallic=0.3, fin_up=('#56646c', '#c8b060', 0.75), fin_low=('#c8ccc4', '#dcdcc8', 0.6),
                                       patterns=[('bands', dict(n=22, u=(0.16, 0.88), w=0.012, color='#22303c', v=(0.55, 1.0), a=0.55)),
                                                 ('stripe', dict(v=0.55, w=0.015, color='#c8b870', a=0.35))]),
                              eye=dict(s=0.11, z=0.35, r=0.017, iris='#d8c880'), anim=dict(amp=1.1, freq=1.2)),
    # 유럽동갈치 (Belone belone): 바늘처럼 가는 몸, 위아래 턱이 모두 길게 뻗은 부리(아래턱이 조금 더 길다), 짙은 청록색 등이
    # 은빛 옆구리와 또렷이 갈린다, 등·뒷지느러미가 꼬리 가까이 마주 본다
    'garfish': dict(type='E', name='유럽동갈치', depth=0.075, width=0.065, tmax=0.5, body_end=0.88, ped=0.45, snout=0.95, beak=0.15, bill=0.13,
                    fins=[_fin('dorsal', (0.74, 0.86), 0.5), _fin('anal', (0.72, 0.86), 0.5),
                          _fin('caudal', h=0.12, shape='fork'), _fin('pectoral', s=(0.22, 0.22), h=0.06), _fin('pelvic', s=(0.6, 0.6), h=0.05)],
                    colors=P('#163e4a', '#a8c8c8', '#f4f6f2', silver=0.85, metallic=0.35, fin_up=('#2a5a64', '#6a9094', 0.6), fin_low=('#b8c8c8', '#d0dcdc', 0.5),
                             patterns=[('stripe', dict(v=0.86, w=0.13, color='#123a46', a=0.8)), ('stripe', dict(v=0.6, w=0.03, color='#3a8a90', a=0.5))]),
                    eye=dict(s=0.16, z=0.3, r=0.012, iris='#d0d0c0'), anim=dict(amp=1.2, freq=1.3)),
}
