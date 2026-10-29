"""동남아 거점 어종 2차 (바다 11 · 민물 5) — 설계 시트 형식은 fish_species.py 머리말.

얇은 낚시터(돈콘·탱글·텔록바텍·센토사·방콕 등)를 채우려고 더한 실제 동남아 어종. 색은 사진 속 자연스러운 색에 맞췄다.
상어는 species_med2.py와 같은 방법(대칭 꼬리지느러미 대신 꼬리자루 위 등지느러미 = 위 꼬리, 밑 뒷지느러미 = 아래 꼬리)으로 만든다.
"""
from fish_species import P, _fin  # noqa: F401


def _shark_fins(d1, d1h, d2, d2h, an, anh, lobe, lobeh, low, lowh, pec, pech, pel):
    """상어 지느러미 (species_med2._shark_fins와 같다): 첫째·둘째 등지느러미, 뒷지느러미, 위 꼬리, 아래 꼬리, 가슴·배지느러미."""
    return [_fin('dorsal', d1, d1h, prof=[(0, 1.0), (0.55, 0.6), (1, 0.12)]),
            _fin('dorsal', d2, d2h, prof=[(0, 1.0), (1, 0.25)]),
            _fin('anal', an, anh, prof=[(0, 1.0), (1, 0.25)]),
            _fin('dorsal', lobe, lobeh, prof=[(0, 0.05), (0.45, 0.4), (1, 1.0)]),
            _fin('anal', low, lowh, prof=[(0, 0.2), (1, 1.0)]),
            _fin('pectoral', s=(pec, pec), h=pech, long=True),
            _fin('pelvic', s=(pel, pel), h=0.05)]


SPECIES = {
    # ===== 산호초·암초 (탱글·티오만·푸켓·코사무이) =====
    # 흑기흉상어 (블랙팁 리프 상어, Carcharhinus melanopterus): 짧고 넓적한 둥근 주둥이, 높은 첫 등지느러미(가슴지느러미 끝 위에서 시작),
    # 낫 모양 가슴지느러미. 옅은 회갈색 등·흰 배, 뒷지느러미 위에서 앞으로 뻗는 흰 띠, 모든 지느러미 끝이 새까맣다 (첫 등·아래 꼬리가 가장 뚜렷)
    'blacktipreefshark': dict(type='E', name='흑기흉상어', depth=0.15, width=0.13, tmax=0.34, body_end=0.8, ped=0.2, arch=0.55, snout=0.45, mouth=0.0,
                              shark=dict(mouth=0.075, gills=(0.2, 0.25), rostrum=0.28),
                              xsec=2.1, scales=0, bones=7,
                              fins=_shark_fins((0.33, 0.45), 0.9, (0.69, 0.73), 0.3, (0.7, 0.74), 0.26, (0.84, 1.0), 1.7, (0.92, 1.0), 0.95, 0.26, 0.27, 0.55),
                              colors=P('#4c463c', '#766e60', '#f0eee8', silver=0.1, fin_up=('#4c463c', '#0e0e0e', 0.97), fin_low=('#6a6458', '#121212', 0.95),
                                       patterns=[]),
                              eye=dict(s=0.09, z=0.3, r=0.016, iris='#1a1a16'), anim=dict(amp=1.05, freq=1.0)),
    # 골든 트레발리 (Gnathanodon speciosus): 옆으로 납작한 긴 타원 몸, 앞으로 쑥 늘어나는 고무 같은 입술(이빨 없음), 등이 배보다 둥글다.
    # 어린 것은 샛노란 몸에 굵고 가는 검은 띠가 번갈아, 다 자라면 은빛 금색에 띠가 희미해진다. 지느러미는 노랗고 꼬리 끝이 거뭇하다
    # 게임에서는 알아보기 쉬운 어린 것의 모습: 샛노란 몸에 굵은 검은 띠와 가는 띠가 번갈아. 주둥이는 뾰족하고 입술이 도톰하다
    'goldentrevally': dict(type='D', name='골든 트레발리', depth=0.33, width=0.11, tmax=0.42, arch=0.6, snout=0.82, blunt=0.95, mouth=-0.1, lips=1.5, gape=0.04,
                           ped=0.13, scales=0,
                           fins=[_fin('dorsal', (0.3, 0.44), 0.32, prof=[(0, 0.6), (0.3, 1.0), (1, 0.4)], spiny=True),
                                 _fin('dorsal', (0.47, 0.8), 0.5, prof=[(0, 1.0), (0.2, 0.6), (1, 0.25)]),
                                 _fin('anal', (0.55, 0.8), 0.45, prof=[(0, 1.0), (0.2, 0.6), (1, 0.25)]),
                                 _fin('caudal', h=0.36, shape='deepfork'),
                                 _fin('pectoral', s=(0.28, 0.28), h=0.24, long=True), _fin('pelvic', s=(0.33, 0.33), h=0.09)],
                           colors=P('#8a7a28', '#e8c434', '#f6e48a', silver=0.5, metallic=0.35, fin_up=('#e8c030', '#5a4a18', 0.9), fin_low=('#e8c434', '#f0d050', 0.88),
                                    patterns=[('bands', dict(n=6, u=(0.12, 0.86), w=0.05, color='#22200e', v=(0.12, 1.0), a=0.8)),
                                              ('bands', dict(n=5, u=(0.19, 0.79), w=0.022, color='#2a2612', v=(0.25, 1.0), a=0.65)),
                                              ('scutes', dict(color='#a89a60', a=0.6)), ('sheen', 0.4)]),
                           eye=dict(s=0.13, z=0.35, iris='#d0c080'), anim=dict(amp=1.0, freq=1.3)),
    # 타이탄 트리거피시 (Balistoides viridescens): 마름모꼴의 높고 납작한 몸, 몸의 3분의 1인 큰 머리, 눈은 머리 뒤쪽 높이 붙었다,
    # 작은 입에 튼튼한 이빨, 세울 수 있는 굵은 첫 등가시. 올리브빛 노란 몸에 비늘마다 짙은 가운데(바둑판), 윗입술 위 검은 띠(콧수염),
    # 지느러미는 노르스름하고 가장자리가 검다
    # 눈에서 입까지 길게 곧게 내려오는 주둥이(blunt·mouth_h)가 이 고기의 얼굴이다. 뺨·턱 밑은 연하다
    'titantriggerfish': dict(type='D', name='타이탄 트리거피시', depth=0.48, width=0.11, tmax=0.45, body_end=0.82, arch=0.55, snout=0.98, blunt=1.2, mouth_h=0.15,
                             mouth=0.0, gape=0.025, lips=1.4, xsec=1.6, scales=34,
                             fins=[_fin('dorsal', (0.3, 0.4), 0.3, prof=[(0, 1.0), (0.3, 0.6), (1, 0.15)], spiny=True),
                                   _fin('dorsal', (0.53, 0.8), 0.32, prof=[(0, 1.0), (0.3, 0.75), (1, 0.45)]),
                                   _fin('anal', (0.55, 0.8), 0.3, prof=[(0, 1.0), (0.3, 0.75), (1, 0.45)]),
                                   _fin('caudal', h=0.24, shape='emarg'), _fin('pectoral', s=(0.33, 0.33), h=0.08)],
                             colors=P('#4a4a2a', '#a49a4e', '#d8d0a4', silver=0.05, fin_up=('#c8a040', '#26261c', 0.9), fin_low=('#c8a040', '#26261c', 0.9),
                                      patterns=[('scale_dots', dict(color='#2a2a1a', a=0.8)), ('mottle', dict(color='#3a3a28', a=0.25, scale=0.07)),
                                                ('throat', '#e0dcc0'),
                                                ('spots', dict(n=1, r=(22, 22), u=(0.05, 0.05), v=(0.56, 0.56), color='#1a1a14', a=0.9))]),
                             eye=dict(s=0.22, z=0.55, r=0.022, iris='#c8b048'), anim=dict(amp=0.6, freq=1.1)),
    # 타이거 그루퍼 (갈색얼룩바리, Epinephelus fuscoguttatus): 두툼하고 옆으로 납작한 몸, 날카로운 머리선, 머리 위로 붙은 큰 입,
    # 크고 둥근 지느러미. 옅은 누런 갈색 바탕에 짙은 갈색 얼룩이 불규칙하게, 그 위로 잔 갈색 점이 빽빽, 꼬리자루 위 작은 검은 안장 무늬
    # 큰 짙은 갈색 얼룩이 또렷하고 아래턱이 나온 큰 입
    'tigergrouper': dict(type='C', name='타이거 그루퍼', depth=0.3, width=0.17, head_w=1.1, snout=0.8, blunt=0.95, mouth=0.28, gape=0.1, jaw=0.012, scales=0,
                         fins=[_fin('dorsal', (0.3, 0.8), 0.32, prof=[(0, 0.6), (0.15, 1.0), (0.6, 0.8), (1, 1.15)], spiny=True),
                               _fin('anal', (0.62, 0.8), 0.36), _fin('caudal', h=0.24, shape='round'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.18), _fin('pelvic', s=(0.3, 0.3), h=0.13)],
                         colors=P('#6a5434', '#c4a470', '#e8dab0', silver=0.05, fin_up=('#7a6a46', '#4a3e28', 0.9), fin_low=('#8a7a52', '#5a4a30', 0.9),
                                  patterns=[('blotches', dict(color='#3a2a16', a=0.7)), ('mottle', dict(color='#4a3c26', a=0.45, scale=0.06)),
                                            ('spots', dict(n=420, r=(2.5, 4.5), color='#3a2c1a', v=(0.05, 1.0), a=0.75)),
                                            ('spots', dict(n=1, r=(9, 9), u=(0.97, 0.97), v=(0.96, 0.96), color='#141210', a=0.9)), ('finspots', '#3a2c1a')]),
                         eye=dict(s=0.1, z=0.45, r=0.015, iris='#a08850'), anim=dict(amp=0.8, freq=0.75)),
    # 엠퍼러 스내퍼 (레드 엠퍼러, Lutjanus sebae): 아주 높은 몸(체고가 몸길이의 절반 가까이), 가파른 이마, 곧은 주둥이선.
    # 분홍빛 붉은 몸에 짙은 붉은 띠 셋(주둥이→눈→등가시 앞 / 가시 등지느러미→배지느러미 / 등 뒤→꼬리 아래 끝), 지느러미도 붉다. 큰 것은 띠가 흐려진다
    # 흰 바탕에 짙은 붉은 띠 셋이 또렷한 젊은 개체 모습, 곧게 비탈진 이마에 뾰족한 주둥이
    'emperorsnapper': dict(type='D', name='엠퍼러 스내퍼', depth=0.42, width=0.13, tmax=0.4, arch=0.66, snout=0.85, blunt=1.0, mouth=0.0, gape=0.06, scales=48,
                           fins=[_fin('dorsal', (0.3, 0.62), 0.36, prof=[(0, 0.4), (0.18, 1.0), (1, 0.6)], spiny=True),
                                 _fin('dorsal', (0.62, 0.8), 0.36, prof=[(0, 0.75), (0.5, 0.9), (1, 0.55)]),
                                 _fin('anal', (0.62, 0.8), 0.36, prof=[(0, 0.7), (0.3, 1.0), (1, 0.55)], spiny=True),
                                 _fin('caudal', h=0.28, shape='emarg'),
                                 _fin('pectoral', s=(0.3, 0.3), h=0.2, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.14)],
                           colors=P('#c87070', '#f0dcd8', '#f8f0ee', silver=0.35, fin_up=('#b83a3a', '#8a2228', 0.85), fin_low=('#c84a44', '#a02a2e', 0.85),
                                    patterns=[('bands', dict(n=3, u=(0.13, 0.76), w=0.11, color='#a01c22', v=(0.0, 1.0), a=0.9)), ('scale_edge', 0.3)]),
                           eye=dict(s=0.14, z=0.45, iris='#d07040'), anim=dict(amp=0.8, freq=0.9)),
    # 말라바 스내퍼 (Lutjanus malabaricus, 말레이시아 '이칸 메라'): 체고가 높은 퉁돔, 가파른 이마에 살짝 오목한 주둥이선, 반듯한(잘린) 꼬리.
    # 붉은·주홍빛 등과 옆구리, 아래로 갈수록 옅고, 지느러미도 붉다. 옆구리에 가는 붉은 세로줄이 비친다
    # 엠퍼러보다 낮은 몸에 뾰족한 머리, 띠 없이 온몸이 붉다
    'malabarsnapper': dict(type='D', name='말라바 스내퍼', depth=0.35, width=0.13, tmax=0.42, arch=0.63, snout=0.88, blunt=1.0, mouth=0.1, gape=0.07, scales=50,
                           fins=[_fin('dorsal', (0.3, 0.6), 0.36, prof=[(0, 0.4), (0.2, 1.0), (1, 0.6)], spiny=True),
                                 _fin('dorsal', (0.6, 0.8), 0.32, prof=[(0, 0.75), (0.4, 0.85), (1, 0.55)]),
                                 _fin('anal', (0.62, 0.8), 0.32, prof=[(0, 0.7), (0.3, 1.0), (1, 0.5)], spiny=True),
                                 _fin('caudal', h=0.26, shape='truncate'),
                                 _fin('pectoral', s=(0.3, 0.3), h=0.2, long=True), _fin('pelvic', s=(0.35, 0.35), h=0.12)],
                           colors=P('#9c3428', '#d4684e', '#f2cebe', silver=0.35, fin_up=('#b84434', '#c85a44', 0.82), fin_low=('#d0644a', '#e07a5a', 0.8),
                                    patterns=[('hstripes', dict(n=7, v=(0.3, 0.85), w=0.007, color='#b84a3a', a=0.4)), ('scale_edge', 0.4)]),
                           eye=dict(s=0.13, z=0.42, iris='#d09040'), anim=dict(amp=0.85, freq=0.95)),
    # 오리엔탈 스위트립스 (Plectorhinchus vittatus): 등이 둥근 긴 타원 몸, 나이 들수록 부풀어 오르는 두툼한 입술, 이어진 등지느러미.
    # 흰 바탕에 검은 세로줄(위쪽 줄이 더 굵다), 주둥이·이마와 지느러미가 노랗고, 등·뒷·꼬리지느러미에 검은 점
    'orientalsweetlips': dict(type='C', name='오리엔탈 스위트립스', depth=0.31, width=0.13, tmax=0.38, arch=0.62, snout=0.6, mouth=-0.12, lips=1.9, scales=70,
                              fins=[_fin('dorsal', (0.28, 0.82), 0.3, prof=[(0, 0.5), (0.15, 1.0), (0.5, 0.75), (0.65, 0.9), (1, 0.8)], spiny=True),
                                    _fin('anal', (0.64, 0.8), 0.34, prof=[(0, 0.7), (0.3, 1.0), (1, 0.6)]),
                                    _fin('caudal', h=0.24, shape='truncate'),
                                    _fin('pectoral', s=(0.27, 0.27), h=0.15), _fin('pelvic', s=(0.3, 0.3), h=0.13)],
                              colors=P('#3a3a36', '#f2f2ec', '#f8f8f4', silver=0.2, fin_up=('#d8b838', '#e8c848', 0.85), fin_low=('#d8b838', '#e8c848', 0.85),
                                       patterns=[('hstripes', dict(n=7, v=(0.3, 0.98), w=0.03, color='#16161a', a=0.92)),
                                                 ('spots', dict(n=1, r=(22, 22), u=(0.03, 0.03), v=(0.72, 0.72), color='#d8b838', a=0.55)),
                                                 ('finspots', '#141414')]),
                              eye=dict(s=0.12, z=0.42, iris='#c8a040'), anim=dict(amp=0.85, freq=0.9)),

    # ===== 외해 회유어 =====
    # 동갈방어 (레인보우 러너, Elagatis bipinnulata): 전갱이과답지 않은 어뢰 모양 날씬한 몸, 길고 뾰족한 머리, 작은 눈, 낮고 긴 둘째 등지느러미,
    # 깊게 갈라진 꼬리. 짙은 청록 등, 흰 배, 옆구리에 하늘색 가는 줄 둘과 그 사이 굵은 노란 줄
    # 등은 올리브빛 초록, 꼬리는 노르스름하다
    'rainbowrunner': dict(type='E', name='동갈방어', depth=0.165, width=0.1, tmax=0.42, body_end=0.8, ped=0.14, snout=0.9, blunt=1.0, mouth=0.05, gape=0.05,
                          fins=[_fin('dorsal', (0.3, 0.4), 0.28, prof=[(0, 1.0), (1, 0.35)], spiny=True),
                                _fin('dorsal', (0.44, 0.78), 0.32, prof=[(0, 1.0), (0.15, 0.6), (1, 0.4)]),
                                _fin('anal', (0.6, 0.78), 0.28, prof=[(0, 1.0), (0.15, 0.6), (1, 0.4)]),
                                _fin('caudal', h=0.38, shape='deepfork'),
                                _fin('pectoral', s=(0.25, 0.25), h=0.1), _fin('pelvic', s=(0.32, 0.32), h=0.07)],
                          colors=P('#1e4a3a', '#86a2a0', '#f2f4f0', silver=0.7, metallic=0.35, fin_up=('#7a8a40', '#c0b040', 0.82), fin_low=('#b8c0b0', '#d0d4c4', 0.6),
                                   patterns=[('stripe', dict(v=0.7, w=0.016, color='#8ad0f0', a=0.9)), ('stripe', dict(v=0.62, w=0.03, color='#c8b440', a=0.8)),
                                             ('stripe', dict(v=0.54, w=0.014, color='#8ad0f0', a=0.85)), ('sheen', 0.5)]),
                          eye=dict(s=0.1, z=0.25, r=0.015, iris='#c0c0a8'), anim=dict(amp=1.05, freq=1.5)),
    # 인도고등어 (Rastrelliger kanagurta, 태국 '쁠라투'·말레이 '이칸 켐붕'): 고등어보다 체고가 높고 머리가 길다, 토막지느러미.
    # 청록 등에 가는 짙은 세로줄(금빛으로 비친다), 가슴지느러미 밑동 아래 검은 점 하나, 노르스름한 지느러미
    # 머리가 길고 입이 크다, 옆구리는 금빛이 도는 은색, 등 쪽 가는 검은 세로줄이 또렷하다
    'indianmackerel': dict(type='E', name='인도고등어', depth=0.24, width=0.12, tmax=0.4, ped=0.17, snout=0.88, blunt=1.0, mouth=0.05, gape=0.075,
                           colors=P('#24484c', '#c0bc9c', '#f2f0e6', silver=0.85, metallic=0.35, fin_up=('#a09a58', '#4a4428', 0.75), fin_low=('#c8c090', '#d8d0a8', 0.6),
                                    patterns=[('hstripes', dict(n=3, v=(0.74, 0.92), w=0.014, color='#14262c', a=0.85)),
                                              ('hstripes', dict(n=2, v=(0.66, 0.8), w=0.01, color='#c8b060', a=0.45)),
                                              ('spots', dict(n=1, r=(6, 6), u=(0.27, 0.27), v=(0.44, 0.44), color='#141414', a=0.85)),
                                              ('finlets', True), ('sheen', 0.5)]),
                           eye=dict(r=0.024, iris='#d8d0b0'), anim=dict(amp=1.0, freq=1.5)),

    # ===== 맹그로브·갯벌 (텔록바텍·센토사·랑카위) =====
    # 자이언트 머드스키퍼 (Periophthalmodon schlosseri): 갯벌을 기어 다니는 가장 큰 말뚝망둥어. 머리 꼭대기에 툭 튀어나온 두 눈,
    # 뭉툭한 머리, 다리처럼 굵은 가슴지느러미, 높고 둥근 첫 등지느러미, 긴 둘째 등지느러미. 누런 녹갈색에 하늘색 잔점,
    # 놀라면 눈에서 꼬리자루까지 검은 세로띠가 드러난다
    'giantmudskipper': dict(type='C', name='자이언트 머드스키퍼', depth=0.15, width=0.14, head_w=1.25, tmax=0.22, ped=0.55, arch=0.62, snout=0.3, mouth=-0.05,
                            xsec=2.1, scales=60,
                            fins=[_fin('dorsal', (0.3, 0.4), 0.95, prof=[(0, 0.6), (0.3, 1.0), (0.7, 0.9), (1, 0.4)]),
                                  _fin('dorsal', (0.47, 0.8), 0.45, prof=[(0, 0.6), (0.2, 1.0), (1, 0.6)]),
                                  _fin('anal', (0.55, 0.8), 0.32), _fin('caudal', h=0.2, shape='round'),
                                  _fin('pectoral', s=(0.2, 0.2), h=0.2), _fin('pelvic', s=(0.24, 0.24), h=0.07)],
                            colors=P('#4e4a30', '#857a52', '#d6cfae', silver=0.05, fin_up=('#4a4430', '#cfcfc4', 0.85), fin_low=('#7a7050', '#8a8060', 0.85),
                                     patterns=[('mottle', dict(color='#34301e', a=0.4, scale=0.06)),
                                               ('stripe', dict(v=0.55, w=0.022, color='#1a1a12', a=0.55)),
                                               ('spots', dict(n=150, r=(2, 3.5), color='#a8dcf2', v=(0.3, 0.95), a=0.9))]),
                            eye=dict(s=0.08, z=0.92, r=0.03, iris='#b8a060'), anim=dict(amp=0.9, freq=1.0)),
    # 점박이 스캣 (Scatophagus argus): 네모나게 높고 아주 납작한 몸, 머리 뒤가 가파르게 솟는다, 작은 입, 깊게 파인 가시 등지느러미(가시에 독),
    # 거의 반듯한 꼬리. 녹갈빛 은색 바탕에 갈색 둥근 점이 흩어졌다
    # 크고 고른 둥근 검은 점이 은빛 바탕에 빽빽하다, 작은 입
    'spottedscat': dict(type='D', name='점박이 스캣', depth=0.53, width=0.1, tmax=0.42, arch=0.6, snout=0.5, blunt=0.85, mouth=-0.05, gape=0.025, ped=0.32, xsec=1.5, scales=60,
                        fins=[_fin('dorsal', (0.32, 0.56), 0.4, prof=[(0, 0.4), (0.25, 1.0), (0.85, 0.3), (1, 0.2)], spiny=True),
                              _fin('dorsal', (0.57, 0.82), 0.34, prof=[(0, 1.0), (0.4, 0.85), (1, 0.5)]),
                              _fin('anal', (0.57, 0.82), 0.32, prof=[(0, 0.8), (0.35, 1.0), (1, 0.5)], spiny=True),
                              _fin('caudal', h=0.26, shape='truncate'),
                              _fin('pectoral', s=(0.3, 0.3), h=0.12), _fin('pelvic', s=(0.34, 0.34), h=0.12)],
                        colors=P('#5e6448', '#c4c6ac', '#ece8d4', silver=0.45, fin_up=('#6a6a4e', '#4a4a36', 0.8), fin_low=('#7a7a5a', '#5a5a42', 0.8),
                                 patterns=[('spots', dict(n=150, r=(9, 12), color='#221c12', v=(0.1, 1.0), u=(0.12, 0.95), a=0.9)), ('sheen', 0.3)]),
                        eye=dict(s=0.14, z=0.32, r=0.024, iris='#c89a40'), anim=dict(amp=0.8, freq=1.1)),

    # ===== 메콩·짜오프라야 민물 =====
    # 말레이 마시어 (켈라, Tor tambroides): 큰 비늘의 어뢰형 잉어, 길쭉하고 끝이 둥근 주둥이 밑에 열린 두툼한 입술(아랫입술 가운데 혀 같은 돌기),
    # 수염 두 쌍, 높은 세모 등지느러미, 깊게 갈라진 꼬리. 짙은 올리브 등, 청동·금빛 옆구리에 큰 비늘 테가 또렷, 붉은 주황빛 지느러미 (켈라 메라)
    'mahseer': dict(type='A', name='말레이 마시어', depth=0.24, width=0.13, tmax=0.4, body_end=0.8, ped=0.4, arch=0.58, snout=0.8, blunt=0.95, mouth=-0.3,
                    lips=1.8, gape=0.035, scales=24,
                    fins=[_fin('dorsal', (0.38, 0.52), 0.65, prof=[(0, 0.5), (0.15, 1.0), (1, 0.3)]),
                          _fin('anal', (0.7, 0.8), 0.48, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]),
                          _fin('caudal', h=0.28, shape='deepfork'),
                          _fin('pectoral', s=(0.22, 0.22), h=0.13), _fin('pelvic', s=(0.5, 0.5), h=0.11)],
                    barbels=[(0.025, -0.3, 0.035, 0.004, 'down'), (0.05, -0.2, 0.03, 0.0035, 'side')],
                    colors=P('#3a4430', '#b29a5a', '#ece0bc', silver=0.4, metallic=0.3, fin_up=('#7a5a3a', '#a8502e', 0.85), fin_low=('#c05a32', '#d06a3a', 0.88),
                             patterns=[('scale_edge', 0.95), ('sheen', 0.45)]),
                    eye=dict(s=0.1, z=0.3, r=0.016, iris='#d0a040'), anim=dict(amp=1.0, freq=1.1)),
    # 솔저 리버 바브 (Cyclocheilichthys enoplos): 옆으로 납작하고 등이 솟은 은빛 잉어, 기름눈꺼풀, 수염 두 쌍,
    # 톱니 가시가 박힌 높이 솟은 등지느러미. 푸른빛 은색 등, 은백색 배, 등·배지느러미에 옅은 검은 잔점, 다른 지느러미는 옅은 회색
    # 작고 뾰족한 머리 뒤로 등이 솟는다, 등·꼬리지느러미 가장자리가 거뭇하다
    'soldierriverbarb': dict(type='A', name='솔저 리버 바브', depth=0.3, width=0.11, tmax=0.4, arch=0.66, snout=0.8, blunt=0.95, mouth=-0.1, scales=34,
                             fins=[_fin('dorsal', (0.35, 0.5), 0.78, prof=[(0, 0.6), (0.12, 1.0), (0.5, 0.5), (1, 0.3)], spine=True),
                                   _fin('anal', (0.7, 0.8), 0.45, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]),
                                   _fin('caudal', h=0.28, shape='deepfork'),
                                   _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.48, 0.48), h=0.11)],
                             barbels=[(0.025, -0.3, 0.02, 0.0025, 'down'), (0.045, -0.2, 0.018, 0.002, 'side')],
                             colors=P('#46566a', '#c4cad0', '#f2f2f0', silver=0.85, metallic=0.3, fin_up=('#8a9098', '#3a3e44', 0.75), fin_low=('#b0b4b8', '#c8ccd0', 0.6),
                                      patterns=[('scale_edge', 0.6), ('sheen', 0.5)]),
                             eye=dict(s=0.1, z=0.25, r=0.02, iris='#d0d0c0'), anim=dict(amp=1.0, freq=1.15)),
    # 틴포일 바브 (Barbonymus schwanenfeldii): 체고가 높은 은금빛 잉어, 작은 머리, 높은 등지느러미. 지느러미가 모두 붉고,
    # 등지느러미 끝에 검은 점, 꼬리는 붉은 바탕에 위아래 엽마다 검은 줄 (끝 색으로 흉내)
    # 머리는 작고 뾰족한데 등은 머리 뒤에서 높이 솟는다
    'tinfoilbarb': dict(type='A', name='틴포일 바브', depth=0.38, width=0.12, tmax=0.42, arch=0.67, snout=0.85, blunt=1.0, mouth=0.0, gape=0.03, scales=30,
                        fins=[_fin('dorsal', (0.37, 0.5), 0.58, prof=[(0, 0.5), (0.15, 1.0), (1, 0.3)]),
                              _fin('anal', (0.7, 0.8), 0.3, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]), _fin('caudal', h=0.3, shape='deepfork'),
                              _fin('pectoral', s=(0.24, 0.24), h=0.13), _fin('pelvic', s=(0.5, 0.5), h=0.11)],
                        barbels=[(0.03, -0.25, 0.015, 0.0018, 'down')],
                        colors=P('#5a5a42', '#d2c89c', '#f4f0e0', silver=0.85, metallic=0.4, fin_up=('#c84a30', '#18161a', 0.9), fin_low=('#d0502e', '#e06840', 0.88),
                                 patterns=[('scale_edge', 0.5), ('sheen', 0.6)]),
                        eye=dict(iris='#d8b860'), anim=dict(amp=1.0, freq=1.25)),
    # 스네이크스킨 구라미 (Trichopodus pectoralis, 태국 '쁠라 살릿'): 옆으로 납작한 긴 타원 몸, 작은 등지느러미, 아주 긴 뒷지느러미,
    # 실 같은 배지느러미. 올리브 등, 은빛 도는 녹회색 옆구리, 주둥이에서 꼬리자루까지 끊긴 짙은 띠, 뒤쪽에 비스듬한 희미한 줄
    # 작고 뾰족한 머리에 위로 열린 입, 올리브 갈색 바탕에 비스듬한 짙은 물결 줄이 또렷하다
    'snakeskingourami': dict(type='D', name='스네이크스킨 구라미', depth=0.32, width=0.09, tmax=0.42, arch=0.52, snout=0.85, blunt=1.0, mouth=0.35, mouth_slope=-0.2,
                             gape=0.03, scales=55,
                             fins=[_fin('dorsal', (0.55, 0.72), 0.24, prof=[(0, 0.5), (0.5, 1.0), (1, 0.7)]),
                                   _fin('anal', (0.32, 0.86), 0.3, prof=[(0, 0.3), (0.3, 0.8), (1, 1.05)]),
                                   _fin('caudal', h=0.26, shape='emarg'),
                                   _fin('pectoral', s=(0.28, 0.28), h=0.12), _fin('pelvic', s=(0.3, 0.3), h=0.5, thread=True)],
                             colors=P('#4a4a30', '#a89a72', '#e4dcc4', silver=0.4, fin_up=('#7a7050', '#9a9070', 0.6), fin_low=('#8a8060', '#a49a78', 0.6),
                                      patterns=[('stripe', dict(v=0.52, w=0.03, color='#262a1c', a=0.6)), ('blotchline', dict(v=0.52, color='#262a1c')),
                                                ('obliquebands', dict(n=16, color='#3a3020', a=0.6)), ('eyeline', dict(color='#262a1c', w=0.02, n=1)),
                                                ('sheen', 0.4)]),
                             eye=dict(s=0.12, z=0.3, r=0.022, iris='#c09040'), anim=dict(amp=0.8, freq=1.0)),
    # 마블 고비 (Oxyeleotris marmorata, 싱가포르 '순혹'): 가장 큰 망둑어류. 원통형 몸, 넓적한 머리, 아래턱이 나온 큰 입, 작은 눈은 위쪽,
    # 떨어진 등지느러미 둘, 크고 둥근 가슴·꼬리지느러미. 갈색 대리석 얼룩, 지느러미에 짙은 띠 점
    # 위아래로 눌린 넓적한 머리에 아래턱이 나온 큰 입, 밝은 황갈색 바탕에 짙은 대리석 얼룩
    'marblegoby': dict(type='C', name='마블 고비', depth=0.17, width=0.15, head_w=1.25, head_flat=0.25, tmax=0.3, ped=0.55, arch=0.55, snout=0.75, mouth=0.3,
                       gape=0.1, jaw=0.014, xsec=2.1, scales=70,
                       fins=[_fin('dorsal', (0.38, 0.48), 0.45, prof=[(0, 0.6), (0.3, 1.0), (1, 0.6)]),
                             _fin('dorsal', (0.55, 0.78), 0.48, prof=[(0, 0.7), (0.3, 1.0), (1, 0.7)]),
                             _fin('anal', (0.58, 0.78), 0.4), _fin('caudal', h=0.22, shape='round'),
                             _fin('pectoral', s=(0.22, 0.22), h=0.17), _fin('pelvic', s=(0.25, 0.25), h=0.09)],
                       colors=P('#3a3020', '#9a845a', '#e0d4b8', silver=0.05, fin_up=('#5a4c34', '#8a7a5c', 0.85), fin_low=('#6a5a3e', '#9a8a68', 0.85),
                                patterns=[('mottle', dict(color='#241c10', a=0.7, scale=0.05)), ('blotches', dict(color='#2a2014', a=0.6)),
                                          ('finspots', '#2a2014')]),
                       eye=dict(s=0.09, z=0.55, r=0.014, iris='#b09050'), anim=dict(amp=0.8, freq=0.75)),
}
