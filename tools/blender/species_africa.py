"""나일강 하류 · 지중해 추가 어종 — 설계 시트 형식은 fish_species.py 머리말.

나일강(나일강 하류, 민물)에는 나일농어·틸라피아·아프리카메기·타이거피시뿐이라 이집트 나일에 실제로 사는 어종을 더한다
(고대 이집트 벽화·신화에 나오는 고기가 많다: 코끼리주둥이고기 · 전기메기 · 빈니 바브 · 파하카 복어).
지중해 낚시터(산토리니·크레타·자킨토스·듀덴 폭포)에는 아직 없던 바닥 고기와 암초 고기를 더한다.
"""
from fish_species import P, _fin  # noqa: F401


def _finlets(s0, s1, n, h):
    """비키르의 깃발 같은 작은 등지느러미 여럿 (가시 하나 + 살 몇 개가 한 장씩 따로 선다)."""
    step = (s1 - s0) / n
    return [_fin('dorsal', (s0 + step * k, s0 + step * k + step * 0.55), h, prof=[(0, 1.0), (1, 0.55)], spine=True) for k in range(n)]


SPECIES = {
    # ===================== 나일강 (민물) =====================
    # 바야드 메기 (Bagrus bajad): 길쭉한 몸, 넓적한 머리, 가슴지느러미 끝까지 닿는 긴 위턱 수염 + 수염 넷,
    # 짧은 가시 등지느러미 뒤로 아주 긴 기름지느러미, 깊게 갈라진 꼬리. 등은 누런 올리브 회색, 배는 희고 지느러미는 짙은 자줏빛
    'bagrusbajad': dict(type='H', name='바야드 메기', depth=0.16, width=0.13, head_w=1.2, tmax=0.3, body_end=0.83, ped=0.4, arch=0.56, snout=0.5, mouth=0.0,
                        fins=[_fin('dorsal', (0.29, 0.36), 0.62, prof=[(0, 1.0), (0.3, 0.8), (1, 0.45)], spine=True),
                              _fin('adipose', (0.44, 0.79), 0.14, prof=[(0, 0.3), (0.15, 0.9), (0.85, 1.0), (1, 0.5)]),
                              _fin('anal', (0.64, 0.77), 0.34), _fin('caudal', h=0.3, shape='deepfork'),
                              _fin('pectoral', s=(0.17, 0.17), h=0.15, spine=True), _fin('pelvic', s=(0.45, 0.45), h=0.08)],
                        barbels=[(0.03, 0.05, 0.3, 0.005, 'side'), (0.045, 0.4, 0.05, 0.0025, 'up'),
                                 (0.025, -0.35, 0.07, 0.003, 'down'), (0.03, -0.5, 0.05, 0.0025, 'down')],
                        colors=P('#4c4a3c', '#948a6c', '#ece8da', silver=0.15, fin_up=('#4e4642', '#5a3e4a', 0.85), fin_low=('#7a6a64', '#6a4a52', 0.8),
                                 patterns=[('mottle', dict(color='#3a382c', a=0.12, scale=0.1))]),
                        eye=dict(s=0.08, z=0.5, r=0.012, iris='#b0a070'), anim=dict(amp=0.95, freq=0.85)),
    # 나일 시노돈티스 (Synodontis schall): 뼈판을 쓴 단단한 머리, 굵고 높은 가시 등지느러미, 큰 기름지느러미, 깊게 갈라진 꼬리,
    # 아래로 열린 입에 수염 세 쌍(아래턱 수염은 깃털처럼 갈라진다). 회갈색 몸에 잔 검은 점
    'synodontis': dict(type='H', name='나일 시노돈티스', depth=0.25, width=0.15, head_w=1.1, tmax=0.32, body_end=0.8, ped=0.32, arch=0.62, snout=0.92, blunt=1.25, mouth=-0.45,
                       fins=[_fin('dorsal', (0.3, 0.4), 0.72, prof=[(0, 1.0), (0.3, 0.85), (1, 0.4)], spine=True),
                             _fin('adipose', (0.52, 0.79), 0.3, prof=[(0, 0.3), (0.3, 1.0), (0.8, 0.9), (1, 0.4)]),
                             _fin('anal', (0.66, 0.76), 0.42), _fin('caudal', h=0.36, shape='deepfork'),
                             _fin('pectoral', s=(0.2, 0.2), h=0.2, spine=True), _fin('pelvic', s=(0.46, 0.46), h=0.1)],
                       barbels=[(0.03, -0.3, 0.17, 0.004, 'side'), (0.03, -0.6, 0.06, 0.003, 'down'), (0.035, -0.7, 0.04, 0.003, 'down')],
                       colors=P('#5a4e3e', '#968668', '#ddd4c0', silver=0.05, fin_up=('#6a6252', '#5a5244', 0.85), fin_low=('#8a826e', '#7a7260', 0.8),
                                patterns=[('spots', dict(n=45, r=(3, 5), color='#2e2a20', v=(0.3, 1.0), u=(0.2, 0.95), a=0.45)), ('finspots', '#3a3428')]),
                       eye=dict(s=0.1, z=0.45, r=0.018, iris='#b0a070'), anim=dict(amp=0.8, freq=0.9)),
    # 코끼리주둥이고기 (Mormyrus kannume): 코끼리 코처럼 아래로 길게 뻗은 주둥이, 작은 눈, 몸 뒤쪽 절반을 덮는 아주 긴 등지느러미,
    # 짧은 뒷지느러미, 가는 꼬리자루와 갈라진 작은 꼬리. 짙은 청동빛 갈색. 약한 전기로 길을 찾는다
    'elephantsnout': dict(type='B', name='코끼리주둥이고기', depth=0.15, width=0.085, tmax=0.45, body_end=0.86, ped=0.2, arch=0.52, snout=0.98, mouth=-1.0, scales=90,
                          fins=[_fin('dorsal', (0.42, 0.85), 0.32, prof=[(0, 0.5), (0.12, 1.0), (0.8, 0.85), (1, 0.7)]),
                                _fin('anal', (0.7, 0.85), 0.38, prof=[(0, 0.7), (0.25, 1.0), (1, 0.6)]),
                                _fin('caudal', h=0.22, shape='deepfork'),
                                _fin('pectoral', s=(0.21, 0.21), h=0.09), _fin('pelvic', s=(0.42, 0.42), h=0.07)],
                          colors=P('#33291f', '#6a5a46', '#c8bca6', silver=0.25, metallic=0.15, fin_up=('#3a3024', '#2a221a', 0.9), fin_low=('#5a4c3c', '#4a3e30', 0.85),
                                   patterns=[('mottle', dict(color='#241c14', a=0.3, scale=0.08)), ('sheen', 0.25)]),
                          eye=dict(s=0.12, z=0.35, r=0.011, iris='#a09060'), anim=dict(amp=1.0, freq=0.9)),
    # 나일 비키르 (Polypterus bichir): 뱀처럼 긴 몸, 단단한 마름모 굳비늘(ganoid), 등을 따라 깃발처럼 따로 선 작은 등지느러미 열넷쯤,
    # 살덩이 자루가 달린 가슴지느러미, 둥근 꼬리. 회갈색 등에 흐린 가로 얼룩, 누르스름한 배. 공기를 들이마셔야 산다
    # 타협: 가슴지느러미 살덩이 자루 대신 넓은 부채 가슴지느러미, 꼬리·뒷지느러미가 이어진 모양은 둥근 꼬리 + 꼬리 바로 앞 뒷지느러미로
    'bichir': dict(type='H', name='나일 비키르', depth=0.11, width=0.1, head_w=1.1, tmax=0.22, hold=0.35, body_end=0.9, ped=0.55, arch=0.52, snout=0.55, mouth=0.05,
                   xsec=2.1, scales=48, head_scales=True, bones=9,
                   fins=_finlets(0.32, 0.86, 15, 0.6) + [
                       _fin('anal', (0.84, 0.93), 0.4, prof=[(0, 0.6), (0.4, 1.0), (1, 0.7)]),
                       _fin('caudal', h=0.2, shape='round'),
                       _fin('pectoral', s=(0.16, 0.16), h=0.13), _fin('pelvic', s=(0.74, 0.74), h=0.07)],
                   colors=P('#3e4032', '#706c54', '#ddd6b8', silver=0.1, fin_up=('#4a4a3a', '#5a5844', 0.85), fin_low=('#6a6650', '#7a7660', 0.8),
                            patterns=[('scale_edge', 0.85), ('bands', dict(n=8, u=(0.25, 0.92), w=0.05, color='#24261c', v=(0.3, 1.0), a=0.55)),
                                      ('mottle', dict(color='#2a2c20', a=0.3, scale=0.06)), ('finspots', '#2e2e22')]),
                   eye=dict(s=0.07, z=0.5, r=0.011, iris='#c0a050'), anim=dict(amp=1.2, freq=0.8)),
    # 나일 라베오 (Labeo niloticus): 길쭉한 잉어, 주둥이 밑에 두툼한 빨판 같은 입술(돌의 이끼를 긁어 먹는다), 앞이 높고 뒤가 오목한
    # 등지느러미, 깊게 갈라진 꼬리. 짙은 올리브 회색 몸, 비늘마다 짙은 밑동. 나일 계곡 전역에 산다
    'labeo': dict(type='A', name='나일 라베오', depth=0.24, width=0.13, tmax=0.38, snout=0.5, mouth=-0.45, scales=40,
                  fins=[_fin('dorsal', (0.3, 0.55), 0.7, prof=[(0, 0.6), (0.12, 1.0), (0.45, 0.45), (1, 0.35)]),
                        _fin('anal', (0.7, 0.8), 0.45), _fin('caudal', h=0.28, shape='deepfork'),
                        _fin('pectoral', s=(0.23, 0.23), h=0.13), _fin('pelvic', s=(0.48, 0.48), h=0.1)],
                  barbels=[(0.035, -0.45, 0.015, 0.0025, 'down')],
                  colors=P('#3a3e30', '#7a7c66', '#d4d0bc', silver=0.3, fin_up=('#3e4236', '#2e3228', 0.85), fin_low=('#5e6052', '#4e5044', 0.8),
                           patterns=[('scale_edge', 0.5)]),
                  eye=dict(iris='#c0a868')),
    # 빈니 바브 (Labeobarbus bynni): 은빛 금색의 큰 잉어, 커다란 비늘, 굵은 가시가 있는 높은 등지느러미, 수염 두 쌍, 두툼한 입술.
    # 고대 이집트의 '레피도토스' — 오시리스 신화에 나오는 신성한 고기
    'bynni': dict(type='A', name='빈니 바브', depth=0.27, width=0.13, tmax=0.4, snout=0.62, mouth=-0.15, scales=30,
                  fins=[_fin('dorsal', (0.36, 0.5), 0.78, prof=[(0, 0.55), (0.18, 1.0), (1, 0.32)]),
                        _fin('anal', (0.7, 0.8), 0.45), _fin('caudal', h=0.28, shape='deepfork'),
                        _fin('pectoral', s=(0.23, 0.23), h=0.13), _fin('pelvic', s=(0.48, 0.48), h=0.1)],
                  barbels=[(0.03, -0.3, 0.03, 0.003, 'down'), (0.05, -0.15, 0.022, 0.0025, 'side')],
                  colors=P('#55553a', '#bcab78', '#ece4c8', silver=0.6, metallic=0.35, fin_up=('#6a6448', '#5a5038', 0.8), fin_low=('#b88a5a', '#c87a4a', 0.75),
                           patterns=[('scale_edge', 0.65), ('sheen', 0.4)]),
                  eye=dict(iris='#d8b050'), anim=dict(amp=0.9, freq=0.95)),
    # 파하카 복어 (Tetraodon lineatus): 둥근 복어 몸, 몸 뒤쪽에 마주 보는 등·뒷지느러미, 둥근 꼬리, 배지느러미 없음.
    # 올리브 갈색 등에 짙은 얼룩, 옆구리 아래쪽에 금빛과 짙은 세로줄이 번갈아, 흰 배. 위협하면 공처럼 부푼다
    'fahaka': dict(type='E', name='파하카 복어', depth=0.25, width=0.23, tmax=0.33, body_end=0.83, ped=0.3, arch=0.55, snout=0.3, mouth=0.0,
                   xsec=2.0, scales=0,
                   fins=[_fin('dorsal', (0.63, 0.72), 0.5, prof=[(0, 0.8), (0.3, 1.0), (1, 0.4)]),
                         _fin('anal', (0.65, 0.73), 0.45, prof=[(0, 0.8), (0.3, 1.0), (1, 0.4)]),
                         _fin('caudal', h=0.24, shape='round'),
                         _fin('pectoral', s=(0.3, 0.3), h=0.1)],
                   colors=P('#5e5a3c', '#a49a68', '#f2eee0', silver=0.1, fin_up=('#8a7a48', '#5a4a30', 0.8), fin_low=('#b0a070', '#c8b888', 0.65),
                            patterns=[('mottle', dict(color='#3a3624', a=0.28, scale=0.05)),
                                      ('hstripes', dict(n=5, v=(0.2, 0.52), w=0.016, color='#e0b838', a=0.8)),
                                      ('hstripes', dict(n=4, v=(0.25, 0.48), w=0.012, color='#3a3424', a=0.6))]),
                   eye=dict(s=0.15, z=0.45, r=0.026, iris='#e0a020'), anim=dict(amp=0.8, freq=1.1)),
    # 전기메기 (Malapterurus electricus): 소시지처럼 통통한 몸, 둥글고 뭉툭한 머리, 등지느러미 없이 꼬리 앞의 기름지느러미 하나,
    # 둥근 꼬리, 수염 세 쌍, 아주 작은 눈. 회갈색 바탕에 불규칙한 검은 점, 꼬리자루에 흰 테를 두른 검은 띠. 수백 볼트 전기를 낸다
    'electriccatfish': dict(type='H', name='전기메기', depth=0.19, width=0.17, head_w=1.0, tmax=0.42, body_end=0.86, ped=0.5, arch=0.5, snout=0.35, mouth=0.0,
                            xsec=2.2, keel=0.1,
                            fins=[_fin('adipose', (0.7, 0.8), 0.28, prof=[(0, 0.4), (0.4, 1.0), (1, 0.6)]),
                                  _fin('anal', (0.72, 0.83), 0.3, prof=[(0, 0.6), (0.4, 1.0), (1, 0.6)]),
                                  _fin('caudal', h=0.18, shape='round'),
                                  _fin('pectoral', s=(0.22, 0.22), h=0.08), _fin('pelvic', s=(0.55, 0.55), h=0.06)],
                            barbels=[(0.02, -0.1, 0.07, 0.004, 'side'), (0.025, -0.5, 0.05, 0.0035, 'down'), (0.03, -0.35, 0.04, 0.003, 'down')],
                            colors=P('#5a4836', '#8e785c', '#e0d2b8', silver=0.0, fin_up=('#6a5a48', '#4a3e32', 0.9), fin_low=('#8a7a62', '#6a5a48', 0.85),
                                     patterns=[('spots', dict(n=55, r=(4, 9), color='#1c1812', v=(0.25, 1.0), u=(0.15, 0.85), a=0.8)),
                                               ('bands', dict(n=1, u=(0.86, 0.86), w=0.07, color='#d8ccb0', v=(0.0, 1.0), a=0.45)),
                                               ('bands', dict(n=1, u=(0.93, 0.93), w=0.08, color='#16120e', v=(0.0, 1.0), a=0.85))]),
                            eye=dict(s=0.07, z=0.4, r=0.008, iris='#806040'), anim=dict(amp=0.7, freq=0.7)),
    # 아프리카 아로와나 (Heterotis niloticus): 원통형 긴 몸, 아주 큰 비늘, 작은 머리, 몸 뒤쪽에 마주 보는 긴 등·뒷지느러미, 둥근 꼬리.
    # 다 크면 무늬 없는 회갈색~청동색. 아가미 위 기관으로 플랑크톤을 걸러 먹고 공기도 마신다
    'heterotis': dict(type='B', name='아프리카 아로와나', depth=0.19, width=0.13, tmax=0.36, body_end=0.84, ped=0.5, arch=0.55, snout=0.55, mouth=0.1,
                      xsec=2.1, scales=22,
                      fins=[_fin('dorsal', (0.58, 0.86), 0.32, prof=[(0, 0.5), (0.2, 1.0), (1, 0.95)]),
                            _fin('anal', (0.6, 0.86), 0.32, prof=[(0, 0.5), (0.2, 1.0), (1, 0.95)]),
                            _fin('caudal', h=0.2, shape='round'),
                            _fin('pectoral', s=(0.22, 0.22), h=0.1), _fin('pelvic', s=(0.46, 0.46), h=0.08)],
                      colors=P('#3a3628', '#776a4e', '#c8bea0', silver=0.3, metallic=0.2, fin_up=('#3e3828', '#2e2a20', 0.9), fin_low=('#5a5040', '#4a4234', 0.85),
                               patterns=[('scale_edge', 0.75), ('sheen', 0.25)]),
                      eye=dict(s=0.08, z=0.3, r=0.016, iris='#c0a050'), anim=dict(amp=0.9, freq=0.85)),
    # 짐나르쿠스 (Gymnarchus niloticus, 아바): 뱀장어처럼 긴 몸, 뾰족한 머리, 등 거의 끝까지 이어진 긴 등지느러미 하나로만 헤엄친다
    # (뒷지느러미·배지느러미·꼬리지느러미가 없고 꼬리는 쥐꼬리처럼 가늘다). 등은 회갈색, 배는 밝다. 약한 전기를 내고 뒤로도 헤엄친다
    'gymnarchus': dict(type='G', name='짐나르쿠스', depth=0.095, width=0.075, tmax=0.25, hold=0.3, body_end=0.98, ped=0.06, tail_w=0.3,
                       snout=0.85, blunt=0.75, mouth=0.0, xsec=2.1,
                       fins=[_fin('dorsal', (0.18, 0.92), 0.34, prof=[(0, 0.25), (0.08, 0.85), (0.55, 1.0), (1, 0.45)]),
                             _fin('caudal', h=0.02, shape='point'),
                             _fin('pectoral', s=(0.13, 0.13), h=0.05)],
                       colors=P('#3a3326', '#6e6250', '#d2c8b0', silver=0.05, fin_up=('#4a4234', '#5a5040', 0.85), fin_low=('#6a6150', '#5a5244', 0.85),
                                patterns=[('mottle', dict(color='#2a261e', a=0.15, scale=0.1))]),
                       eye=dict(s=0.05, z=0.35, r=0.006, iris='#807050'), anim=dict(amp=0.7, freq=0.7)),

    # ===================== 지중해 (바다) =====================
    # 달고기 (Zeus faber): 접시처럼 높고 납작한 몸, 위로 뻗어 나오는 큰 입, 실처럼 긴 가시가 달린 등지느러미, 긴 배지느러미,
    # 올리브 금빛 몸 한가운데 노란 테를 두른 검은 '성 베드로의 엄지 자국'
    'johndory': dict(type='D', name='달고기', depth=0.52, width=0.08, tmax=0.42, body_end=0.8, ped=0.22, arch=0.55, snout=0.6, mouth=0.35, xsec=1.5, scales=0,
                     fins=[_fin('dorsal', (0.3, 0.52), 0.62, prof=[(0, 0.75), (0.2, 1.0), (0.55, 0.85), (1, 0.55)], spiny=True),
                           _fin('dorsal', (0.55, 0.8), 0.2, prof=[(0, 0.7), (0.3, 1.0), (1, 0.7)]),
                           _fin('anal', (0.56, 0.8), 0.2, prof=[(0, 0.7), (0.3, 1.0), (1, 0.7)], spiny=True),
                           _fin('caudal', h=0.2, shape='round'),
                           _fin('pectoral', s=(0.34, 0.34), h=0.09), _fin('pelvic', s=(0.3, 0.3), h=0.34, prof=None)],
                     colors=P('#5e5a4a', '#aaa082', '#e0dccc', silver=0.45, metallic=0.2, fin_up=('#6a6450', '#4a4638', 0.75), fin_low=('#9a9070', '#b0a888', 0.65),
                              patterns=[('waves', dict(color='#7a705a', v=(0.1, 1.0), a=0.25)),
                                        ('spots', dict(n=1, r=(66, 66), u=(0.5, 0.5), v=(0.5, 0.5), color='#e0c870', a=0.85)),
                                        ('spots', dict(n=1, r=(52, 52), u=(0.5, 0.5), v=(0.5, 0.5), color='#121210', a=0.97))]),
                     eye=dict(s=0.13, z=0.55, r=0.026, iris='#d8c060'), anim=dict(amp=0.6, freq=0.7)),
    # 유럽성대 (Chelidonichthys lucerna): 뼈판으로 덮인 각진 머리, 원뿔처럼 가늘어지는 몸, 날개처럼 큰 가슴지느러미(안쪽은 공작 깃처럼
    # 파랗고 가장자리가 밝다), 가시 등지느러미와 긴 둘째 등지느러미. 붉은 갈색 등, 분홍빛 옆구리, 흰 배
    'tubgurnard': dict(type='C', name='유럽성대', depth=0.21, width=0.16, head_w=1.3, tmax=0.3, body_end=0.83, ped=0.25, arch=0.62, snout=0.88, blunt=1.15, mouth=-0.3, scales=0,
                       fins=[_fin('dorsal', (0.27, 0.4), 0.55, prof=[(0, 0.7), (0.25, 1.0), (1, 0.3)], spiny=True),
                             _fin('dorsal', (0.42, 0.8), 0.28, prof=[(0, 0.8), (0.3, 1.0), (1, 0.7)]),
                             _fin('anal', (0.44, 0.8), 0.24, group='up'), _fin('caudal', h=0.2, shape='emarg'),
                             _fin('pectoral', s=(0.25, 0.25), h=0.36, long=True), _fin('pelvic', s=(0.27, 0.27), h=0.12, group='up')],
                       colors=P('#843628', '#c06a52', '#f4e6de', silver=0.15, fin_up=('#b85a44', '#d88870', 0.75), fin_low=('#2e6a8e', '#58a8c8', 0.85),
                                patterns=[('spots', dict(n=50, r=(2, 3.5), color='#6a2a1e', v=(0.5, 1.0), a=0.35)), ('sheen', 0.2)]),
                       eye=dict(s=0.12, z=0.62, r=0.022, iris='#d8a050'), anim=dict(amp=0.8, freq=0.8)),
    # 유럽통구멍 (Uranoscopus scaber): 상자 같은 큰 머리, 정수리에 붙어 하늘을 보는 눈, 곧추선 큰 입, 짧고 검은 가시 등지느러미,
    # 큰 가슴지느러미. 회갈색 등에 흰 잔점. 모래에 묻혀 눈과 입만 내놓고 기다린다
    'stargazer': dict(type='C', name='유럽통구멍', depth=0.22, width=0.2, head_w=1.35, tmax=0.24, body_end=0.83, ped=0.35, arch=0.72, snout=0.3, mouth=1.0,
                      scales=0,
                      fins=[_fin('dorsal', (0.3, 0.37), 0.32, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)], spiny=True),
                            _fin('dorsal', (0.4, 0.8), 0.3, prof=[(0, 0.8), (0.3, 1.0), (1, 0.8)]),
                            _fin('anal', (0.42, 0.8), 0.28), _fin('caudal', h=0.2, shape='truncate'),
                            _fin('pectoral', s=(0.26, 0.26), h=0.2), _fin('pelvic', s=(0.2, 0.2), h=0.1)],
                      colors=P('#56503f', '#8a8270', '#ece8dc', silver=0.05, fin_up=('#3a362e', '#5a564a', 0.85), fin_low=('#a09884', '#c0b8a4', 0.8),
                               patterns=[('mottle', dict(color='#3a3428', a=0.22, scale=0.05)),
                                         ('spots', dict(n=110, r=(2, 4), color='#ece6d6', v=(0.45, 1.0), a=0.6))]),
                      eye=dict(s=0.1, z=0.85, r=0.018, iris='#c8b070'), anim=dict(amp=0.7, freq=0.7)),
    # 그레이터 위버 (Trachinus draco): 옆으로 납작한 긴 몸, 정수리 가까이 붙은 눈, 비스듬히 위로 열린 입, 독가시가 있는 짧고 검은
    # 첫 등지느러미 + 몸 끝까지 이어지는 둘째 등·뒷지느러미. 누런 갈색 옆구리에 비스듬한 검은 줄과 푸른·노란 줄
    'greaterweever': dict(type='C', name='그레이터 위버', depth=0.17, width=0.1, tmax=0.3, body_end=0.85, ped=0.33, arch=0.55, snout=0.45, mouth=0.55, scales=0,
                          fins=[_fin('dorsal', (0.25, 0.32), 0.45, prof=[(0, 1.0), (1, 0.4)], spiny=True),
                                _fin('dorsal', (0.34, 0.86), 0.24, prof=[(0, 0.8), (0.15, 1.0), (1, 0.9)]),
                                _fin('anal', (0.36, 0.86), 0.24, prof=[(0, 0.8), (0.15, 1.0), (1, 0.9)]),
                                _fin('caudal', h=0.18, shape='emarg'),
                                _fin('pectoral', s=(0.24, 0.24), h=0.13), _fin('pelvic', s=(0.21, 0.21), h=0.08)],
                          colors=P('#6a5e3c', '#bea868', '#f2ecdc', silver=0.05, fin_up=('#4a4636', '#2a2824', 0.85), fin_low=('#c8bc98', '#d8d0b0', 0.7),
                                   patterns=[('obliquebands', dict(n=15, color='#3a3222', a=0.4)), ('obliquebands', dict(n=14, color='#5a8ac0', a=0.18)),
                                             ('mottle', dict(color='#3a3626', a=0.3, scale=0.06))]),
                          eye=dict(s=0.11, z=0.72, r=0.02, iris='#d0b060'), anim=dict(amp=0.9, freq=1.0)),
    # 움브리나 (Umbrina cirrosa, 쉬드럼): 등이 굽은 민어형, 아래에 붙은 작은 입, 턱 밑 짧고 뻣뻣한 수염 하나, 은회색에 청동빛,
    # 금빛과 푸른빛 물결 줄이 비스듬히 번갈아 흐른다, 아가미 뚜껑 끝 막이 새까맣다
    'shidrum': dict(type='C', name='움브리나', depth=0.28, width=0.13, tmax=0.36, arch=0.65, snout=0.45, mouth=-0.35, scales=55,
                    fins=[_fin('dorsal', (0.3, 0.45), 0.5, spiny=True), _fin('dorsal', (0.46, 0.82), 0.3, prof=[(0, 0.8), (0.3, 1.0), (1, 0.8)]),
                          _fin('anal', (0.66, 0.78), 0.32, prof=[(0, 1.0), (0.3, 0.9), (1, 0.6)]), _fin('caudal', h=0.22, shape='emarg'),
                          _fin('pectoral', s=(0.27, 0.27), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                    barbels=[(0.03, -0.78, 0.018, 0.003, 'down')],
                    colors=P('#565448', '#aaa084', '#ece6d6', silver=0.5, metallic=0.3, fin_up=('#4e4a40', '#2e2c28', 0.85), fin_low=('#7a7262', '#5a5448', 0.8),
                             patterns=[('obliquebands', dict(n=18, color='#d8b860', a=0.55)), ('obliquebands', dict(n=17, color='#34404e', a=0.45)),
                                       ('earflap', '#121212'), ('sheen', 0.3)]),
                    eye=dict(iris='#c8b070'), anim=dict(amp=0.85, freq=0.8)),
    # 지중해비늘돔 (Sparisoma cretense, 암컷 체색): 큰 비늘의 타원형 몸, 앵무새 부리처럼 붙은 이빨, 붉은 몸에 등 쪽 회색 안장 무늬
    # (가장자리 노랑), 꼬리자루 위 노란 점. 바위의 해조를 갉아 먹는다. 크레타의 이름이 붙은 고기
    'medparrotfish': dict(type='D', name='지중해비늘돔', depth=0.33, width=0.14, tmax=0.4, arch=0.58, snout=0.45, mouth=0.0, scales=26,
                          fins=[_fin('dorsal', (0.3, 0.8), 0.22, prof=[(0, 0.6), (0.15, 1.0), (0.7, 0.9), (1, 1.0)]),
                                _fin('anal', (0.62, 0.8), 0.24, prof=[(0, 0.7), (0.3, 1.0), (1, 0.9)]),
                                _fin('caudal', h=0.24, shape='truncate'),
                                _fin('pectoral', s=(0.3, 0.3), h=0.15), _fin('pelvic', s=(0.35, 0.35), h=0.1)],
                          colors=P('#7a6a68', '#c0503e', '#e8a08c', silver=0.15, fin_up=('#b86050', '#d88878', 0.8), fin_low=('#c86a58', '#e09a88', 0.75),
                                   patterns=[('bands', dict(n=1, u=(0.42, 0.42), w=0.32, color='#d8b840', v=(0.62, 1.0), a=0.7)),
                                             ('bands', dict(n=1, u=(0.42, 0.42), w=0.26, color='#76706a', v=(0.68, 1.0), a=0.85)),
                                             ('spots', dict(n=1, r=(24, 24), u=(0.9, 0.9), v=(0.74, 0.74), color='#f0d040', a=0.95)),
                                             ('scale_edge', 0.6)]),
                          eye=dict(iris='#d8a040'), anim=dict(amp=0.9, freq=1.0)),
}
