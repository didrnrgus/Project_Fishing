"""한국 거점 추가 어종 (docs/fish-db/01_한국_어종DB.md) — 설계 시트 형식은 fish_species.py 머리말."""
from fish_species import P, _fin  # noqa: F401

SPECIES = {
    # ===== 한강 =====
    # 황쏘가리 (Siniperca scherzeri 황색 변이): 쏘가리와 같은 몸, 온몸이 샛노란 황금빛에 얼룩이 거의 없다 (흐릿한 주황 점만 남는다)
    'yellowmandarin': dict(type='C', name='황쏘가리', depth=0.28, mouth=0.25, snout=0.8, scales=90,
                           colors=P('#d89a18', '#f2c43a', '#fbe8a8', silver=0.3, metallic=0.35,
                                    fin_up=('#e0a828', '#f4d070', 0.85), fin_low=('#eab840', '#f6d880', 0.8),
                                    patterns=[('spots', dict(n=26, r=(6, 12), color='#c07a14', v=(0.35, 1.0), a=0.35)),
                                              ('eyeline', dict(color='#b87418', w=0.025)), ('sheen', 0.6)]),
                           eye=dict(iris='#d03018')),
    # 웅어 (Coilia nasus): 칼처럼 납작하고 꼬리 쪽으로 길게 가늘어지는 몸, 뒷지느러미가 꼬리까지 길게 이어지고
    # 꼬리지느러미는 작다. 둥근 주둥이 밑으로 큰 입, 가슴지느러미 위쪽 살이 실처럼 길다. 은백색에 등은 금빛 섞인 회청색
    'grenadieranchovy': dict(type='B', name='웅어', depth=0.15, width=0.055, tmax=0.22, body_end=0.95, ped=0.06, arch=0.6, snout=0.6, mouth=-0.25,
                             xsec=1.6, scales=0,
                             fins=[_fin('dorsal', (0.24, 0.31), 0.55, prof=[(0, 0.7), (0.2, 1.0), (1, 0.4)]),
                                   _fin('anal', (0.33, 1.0), 0.28, prof=[(0, 0.5), (0.1, 1.0), (0.8, 0.7), (1, 0.3)]),
                                   _fin('caudal', h=0.06, shape='point'),
                                   _fin('pectoral', s=(0.12, 0.12), h=0.2, long=True), _fin('pelvic', s=(0.25, 0.25), h=0.06)],
                             colors=P('#7a8468', '#d4d8cc', '#f6f6f0', silver=0.9, metallic=0.35, fin_up=('#c0b890', '#d8d0b0', 0.55), fin_low=('#e0d8b8', '#ece8d8', 0.5),
                                      patterns=[('sheen', 0.7)]),
                             eye=dict(s=0.06, z=0.35, r=0.014, iris='#d8c890'), anim=dict(amp=1.2, freq=1.2)),
    # 황복 (Takifugu obscurus): 둥글고 통통한 복어 몸, 비늘 없는 매끈한 피부, 작은 입, 지느러미는 작고 뒤쪽에.
    # 등은 짙은 흑갈색, 옆구리 가운데 굵은 노란 띠, 배는 흰색. 가슴지느러미 뒤와 등지느러미 밑에 흰 테 두른 검은 점
    'obscurepuffer': dict(type='C', name='황복', depth=0.3, width=0.26, tmax=0.36, body_end=0.82, ped=0.32, arch=0.55, snout=0.42, mouth=0.05,
                          xsec=2.0, scales=0,
                          fins=[_fin('dorsal', (0.68, 0.77), 0.42, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                                _fin('anal', (0.7, 0.79), 0.38, prof=[(0, 0.6), (0.3, 1.0), (1, 0.5)]),
                                _fin('caudal', h=0.22, shape='truncate'),
                                _fin('pectoral', s=(0.3, 0.3), h=0.12)],
                          colors=P('#3a3428', '#6a5e40', '#f4f2ea', silver=0.05,
                                   fin_up=('#c8a438', '#e0c050', 0.85), fin_low=('#d8b848', '#e8cc60', 0.8),
                                   patterns=[('stripe', dict(v=0.5, w=0.07, color='#e8c030', a=0.9)),
                                             ('spots', dict(n=1, r=(22, 22), u=(0.33, 0.33), v=(0.7, 0.7), color='#f0ece0', a=0.9)),
                                             ('spots', dict(n=1, r=(17, 17), u=(0.33, 0.33), v=(0.7, 0.7), color='#141210', a=0.95)),
                                             ('spots', dict(n=1, r=(12, 12), u=(0.62, 0.62), v=(0.94, 0.94), color='#141210', a=0.85))]),
                          eye=dict(s=0.16, z=0.42, r=0.024, iris='#e0b020'), anim=dict(amp=0.6, freq=1.3)),
    # ===== 동해 =====
    # 황어 (Tribolodon hakonensis): 날씬한 잉어형, 산란철 혼인색 — 짙은 등, 옆구리에 주황·붉은 세로줄 두 가닥과
    # 그 사이 검은 띠, 붉은 배·뒷지느러미
    'japanesedace': dict(type='B', name='황어', depth=0.21, width=0.11, tmax=0.4, snout=0.75, mouth=0.0, scales=60,
                         fins=[_fin('dorsal', (0.42, 0.53), 0.55, prof=[(0, 0.6), (0.2, 1.0), (1, 0.4)]),
                               _fin('anal', (0.66, 0.78), 0.42), _fin('caudal', h=0.25, shape='fork'),
                               _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.48, 0.48), h=0.09)],
                         colors=P('#2e3834', '#a8a898', '#f0e0d4', silver=0.55,
                                  fin_up=('#4a4e44', '#6a6a5a', 0.75), fin_low=('#d0502a', '#e07040', 0.85),
                                  patterns=[('stripe', dict(v=0.3, w=0.045, color='#e04a24', a=0.85)),
                                            ('stripe', dict(v=0.47, w=0.04, color='#1e2420', a=0.6)),
                                            ('stripe', dict(v=0.66, w=0.025, color='#e8783a', a=0.7))]),
                         eye=dict(iris='#d8b060'), anim=dict(amp=1.0, freq=1.2)),
    # 연어 (Oncorhynchus keta, 첨연어): 가을 산란기에 강으로 올라온 모습 — 올리브 녹색 등, 옆구리에 자줏빛·붉은 얼룩 가로띠(호랑이 무늬),
    # 기름지느러미, 수컷은 갈고리처럼 휜 턱. 뒷·배지느러미 끝이 희다
    'chumsalmon': dict(type='B', name='연어', depth=0.22, width=0.12, tmax=0.42, ped=0.32, arch=0.58, snout=0.85, mouth=0.05, scales=110,
                       fins=[_fin('dorsal', (0.42, 0.53), 0.5, prof=[(0, 0.7), (0.2, 1.0), (1, 0.45)]), _fin('adipose', (0.76, 0.8), 0.18),
                             _fin('anal', (0.66, 0.78), 0.4), _fin('caudal', h=0.24, shape='emarg'),
                             _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.48, 0.48), h=0.09)],
                       colors=P('#3a4630', '#8a8456', '#ddd8c6', silver=0.3,
                                fin_up=('#3e4434', '#2e3428', 0.85), fin_low=('#4a4a3a', '#ecece4', 0.85),
                                patterns=[('bands', dict(n=8, u=(0.25, 0.85), w=0.05, color='#7a2a44', v=(0.15, 0.75), a=0.6)),
                                          ('bands', dict(n=7, u=(0.29, 0.81), w=0.035, color='#3a4a2a', v=(0.2, 0.8), a=0.45)),
                                          ('spots', dict(n=30, r=(2, 4), color='#22281c', v=(0.7, 1.0), a=0.4))]),
                       eye=dict(iris='#c8b070'), anim=dict(amp=1.0, freq=1.2)),
    # ===== 남해·제주 =====
    # 태평양 참다랑어 (Thunnus orientalis): 대서양 참다랑어와 거의 같은 굵은 방추형, 짙은 남색 등, 은빛 배에 희미한 흰 점선,
    # 짧은 가슴지느러미, 노란 토막지느러미, 초승달 꼬리
    'pacificbluefin': dict(type='E', name='태평양 참다랑어', depth=0.26, width=0.19, tmax=0.4, ped=0.1, snout=0.75, mouth=0.05,
                           fins=[_fin('dorsal', (0.32, 0.44), 0.4, prof=[(0, 1.0), (1, 0.25)], spiny=True), _fin('dorsal', (0.52, 0.6), 0.55, prof=[(0, 1.0), (1, 0.3)]),
                                 _fin('anal', (0.56, 0.64), 0.5, prof=[(0, 1.0), (1, 0.3)]), _fin('caudal', h=0.42, shape='deepfork'),
                                 _fin('pectoral', s=(0.26, 0.26), h=0.11), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                           colors=P('#0e1834', '#76869a', '#e8ecee', silver=0.8, metallic=0.45, fin_up=('#121c38', '#0a1028', 0.9), fin_low=('#a0a8b0', '#c0c8d0', 0.7),
                                    patterns=[('finlets', True), ('sheen', 0.5),
                                              ('hstripes', dict(n=3, v=(0.18, 0.36), w=0.006, color='#f4f6f8', a=0.45))]),
                           eye=dict(iris='#202830'), anim=dict(amp=0.9, freq=1.6)),
    # 혹돔 (Semicossyphus reticulatus): 커다란 놀래기. 수컷은 이마에 큰 혹이 솟아 머리가 뭉툭하고 높다(arch·snout로 흉내),
    # 두꺼운 입술, 긴 등지느러미, 붉은 자줏빛 몸, 턱 밑은 희다
    'sheepsheadwrasse': dict(type='C', name='혹돔', depth=0.33, width=0.15, tmax=0.26, body_end=0.82, ped=0.42, arch=0.7, snout=0.28, mouth=-0.2, scales=45,
                             fins=[_fin('dorsal', (0.3, 0.84), 0.26, prof=[(0, 0.6), (0.15, 1.0), (0.75, 0.9), (1, 1.1)], spiny=True),
                                   _fin('anal', (0.62, 0.84), 0.28, prof=[(0, 0.6), (0.3, 1.0), (1, 1.0)]),
                                   _fin('caudal', h=0.22, shape='truncate'),
                                   _fin('pectoral', s=(0.27, 0.27), h=0.15), _fin('pelvic', s=(0.31, 0.31), h=0.12)],
                             colors=P('#6a2a34', '#b2586a', '#e8c4c0', silver=0.1,
                                      fin_up=('#7a3040', '#5a2030', 0.9), fin_low=('#a04858', '#7a3444', 0.85),
                                      patterns=[('scale_edge', 0.4), ('throat', '#f2e6e0'),
                                                ('mottle', dict(color='#4e1e28', a=0.3, scale=0.07))]),
                             eye=dict(s=0.11, z=0.45, r=0.017, iris='#d06030'), anim=dict(amp=0.85, freq=0.9)),
}
