"""동남아 거점 어종 (docs/fish-db/02_동남아_어종DB.md) — 설계 시트 형식은 fish_species.py 머리말."""
from fish_species import P, _fin  # noqa: F401
from species_med2 import _shark_fins  # noqa: E402

SPECIES = {
    # ===== 외해 대형 회유어 =====
    # 돛새치 (Istiophorus platypterus): 몸보다 높은 돛 모양 등지느러미(검은 점), 짧은 창 같은 위턱, 남색 등에 하늘색 세로 점줄, 긴 배지느러미
    # 주둥이가 창으로 가늘게 모이고(blunt), 옆구리는 청동빛 은색, 세로 점줄은 흐릿하다. 배지느러미는 길고 가늘다.
    # 창 밑동에서 입 선이 눈 뒤까지 길게 뻗고(가는 위턱뼈), 눈은 크다
    'sailfish': dict(type='E', name='돛새치', depth=0.13, width=0.08, tmax=0.34, body_end=0.81, ped=0.1, snout=0.95, blunt=1.1, mouth_h=0.12, mouth=-0.3, bill=0.2,
                     gape=0.115, maxilla=0.35, mouth_slope=0.05,
                     fins=[_fin('dorsal', (0.14, 0.64), 2.0, prof=[(0, 0.8), (0.1, 1.0), (0.4, 0.95), (0.75, 0.62), (1, 0.08)], spine=True),
                           _fin('dorsal', (0.73, 0.77), 0.3, prof=[(0, 1.0), (1, 0.3)]),
                           _fin('anal', (0.58, 0.66), 0.55, prof=[(0, 1.0), (1, 0.2)]),
                           _fin('caudal', h=0.42, shape='deepfork'),
                           _fin('pectoral', s=(0.23, 0.23), h=0.2, long=True),
                           _fin('pelvic', s=(0.27, 0.27), h=0.22)],
                     colors=P('#141e3e', '#7a7a80', '#e4e6ea', silver=0.7, metallic=0.45, fin_up=('#1e3070', '#24386e', 0.9), fin_low=('#2a3450', '#1e2840', 0.9),
                              patterns=[('bands', dict(n=16, u=(0.24, 0.86), w=0.016, color='#7aa8d8', v=(0.35, 0.9), a=0.4)), ('sheen', 0.5),
                                        ('stripe', dict(v=0.55, w=0.1, color='#9a7a58', a=0.25)), ('finspots', '#0a1430')]),
                     eye=dict(s=0.1, z=0.32, r=0.019, iris='#283a5a'), anim=dict(amp=1.0, freq=1.3)),
    # 블랙마린 (Istiompax indica): 황새치보다 굵고 무거운 몸, 낮은 첫 등지느러미(앞만 솟음), 접히지 않는 딱딱한 가슴지느러미, 짙은 남흑색 등
    # 갓 잡은 것은 옆구리가 은백색이고 세로 띠는 거의 안 보인다, 머리가 창으로 가늘게 모인다. 입 선은 눈 뒤까지 길다
    'blackmarlin': dict(type='E', name='블랙마린', depth=0.19, width=0.14, tmax=0.32, body_end=0.82, ped=0.09, snout=0.92, blunt=1.05, mouth_h=0.12, mouth=-0.3, bill=0.22,
                        gape=0.115, maxilla=0.35, mouth_slope=0.05,
                        fins=[_fin('dorsal', (0.16, 0.58), 0.75, prof=[(0, 1.0), (0.12, 0.7), (0.3, 0.25), (1, 0.15)], spine=True),
                              _fin('dorsal', (0.74, 0.78), 0.22, prof=[(0, 1.0), (1, 0.3)]),
                              _fin('anal', (0.6, 0.68), 0.45, prof=[(0, 1.0), (1, 0.2)]),
                              _fin('caudal', h=0.5, shape='deepfork'),
                              _fin('pectoral', s=(0.23, 0.23), h=0.32, long=True),
                              _fin('pelvic', s=(0.27, 0.27), h=0.08)],
                        colors=P('#0e1626', '#9aa6b4', '#eef0f2', silver=0.85, metallic=0.45, fin_up=('#141c30', '#0c1220', 0.95), fin_low=('#2a3446', '#1a2232', 0.9),
                                 patterns=[('bands', dict(n=12, u=(0.25, 0.8), w=0.02, color='#7aa0c8', v=(0.45, 0.85), a=0.12)), ('sheen', 0.5)]),
                        eye=dict(s=0.1, z=0.32, r=0.02, iris='#22324a'), anim=dict(amp=0.9, freq=1.1)),
    # 황다랑어 (Thunnus albacares): 참다랑어보다 날씬, 낫처럼 길게 뻗은 노란 둘째 등·뒷지느러미, 옆구리 금빛 띠, 노란 토막지느러미
    # 등은 검푸르게 또렷이 끊기고 옆구리는 은백색, 긴 가슴지느러미는 검다 (노란 것은 둘째 등·뒷지느러미와 토막지느러미뿐)
    'yellowfintuna': dict(type='E', name='황다랑어', depth=0.24, width=0.16, tmax=0.38, ped=0.1, snout=0.92, blunt=0.95, mouth=0.05, gape=0.072,
                          fins=[_fin('dorsal', (0.32, 0.43), 0.4, prof=[(0, 1.0), (1, 0.25)], spiny=True, group='low'),
                                _fin('dorsal', (0.52, 0.59), 1.15, prof=[(0, 1.0), (0.4, 0.45), (1, 0.12)], group='low'),
                                _fin('anal', (0.56, 0.63), 1.05, prof=[(0, 1.0), (0.4, 0.45), (1, 0.12)]),
                                _fin('caudal', h=0.42, shape='deepfork'),
                                _fin('pectoral', s=(0.26, 0.26), h=0.22, long=True, group='up'), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                          colors=P('#0a1434', '#b4bcc6', '#f0f2f2', silver=0.85, metallic=0.45, fin_up=('#141e3c', '#0c1430', 0.9), fin_low=('#d8b028', '#f0cc38', 0.9),
                                   patterns=[('stripe', dict(v=0.62, w=0.035, color='#e8c030', a=0.7)), ('finlets', True), ('sheen', 0.5),
                                             ('bands', dict(n=14, u=(0.45, 0.85), w=0.012, color='#d8e0e8', v=(0.1, 0.45), a=0.4))]),
                          eye=dict(iris='#9aa2a8'), anim=dict(amp=0.9, freq=1.6)),
    # 와후 (Acanthocybium solandri): 아주 길쭉한 몸, 길고 뾰족한 주둥이, 낮고 긴 첫 등지느러미, 코발트빛 세로 줄무늬
    # 꼬리는 몸에 비해 작고 빳빳한 초승달, 띠는 은빛 바탕에 짙은 남색으로 또렷하다, 긴 입이 눈 앞까지
    'wahoo': dict(type='E', name='와후', depth=0.12, width=0.085, tmax=0.42, body_end=0.82, ped=0.14, snout=0.95, blunt=1.1, mouth_h=0.13, mouth=0.05, gape=0.07, jaw=0.006,
                  fins=[_fin('dorsal', (0.22, 0.56), 0.4, prof=[(0, 1.0), (0.15, 0.9), (1, 0.35)], spiny=True),
                        _fin('dorsal', (0.62, 0.68), 0.4, prof=[(0, 1.0), (1, 0.35)]),
                        _fin('anal', (0.63, 0.69), 0.38, prof=[(0, 1.0), (1, 0.35)]),
                        _fin('caudal', h=0.26, shape='deepfork'),
                        _fin('pectoral', s=(0.23, 0.23), h=0.1), _fin('pelvic', s=(0.32, 0.32), h=0.06)],
                  colors=P('#14305a', '#b8c8d6', '#f0f2f4', silver=0.85, metallic=0.45, fin_up=('#1a3a5e', '#10283e', 0.85), fin_low=('#7a90a4', '#9aaabc', 0.7),
                           patterns=[('bands', dict(n=26, u=(0.24, 0.9), w=0.02, color='#1a2c58', v=(0.25, 0.95), a=0.85)), ('finlets', True), ('sheen', 0.5)]),
                  eye=dict(s=0.1, z=0.25, r=0.016, iris='#c8c8b0'), anim=dict(amp=1.1, freq=1.5)),
    # 이빨다랑어 (Gymnosarda unicolor): 길쭉한 다랑어, 무늬 없는 남흑색 등과 은빛 배, 둘째 등·뒷지느러미 끝이 희다, 큰 입
    # 다랑어 중 머리가 가장 뾰족하고 입이 커서 눈 가운데 밑까지 찢어졌다, 원뿔 이빨이 줄지어 드러난다. 큰 눈에 은빛 홍채, 큰 동공
    'dogtoothtuna': dict(type='E', name='이빨다랑어', depth=0.21, width=0.15, tmax=0.4, ped=0.1, snout=0.92, blunt=1.05, mouth_h=0.15, mouth=0.18, gape=0.11, jaw=0.008,
                         teeth=True,
                         fins=[_fin('dorsal', (0.3, 0.46), 0.42, prof=[(0, 1.0), (1, 0.2)], spiny=True),
                               _fin('dorsal', (0.53, 0.61), 0.62, prof=[(0, 1.0), (1, 0.25)], group='low'),
                               _fin('anal', (0.57, 0.64), 0.55, prof=[(0, 1.0), (1, 0.25)]),
                               _fin('caudal', h=0.42, shape='deepfork'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.13), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                         colors=P('#16243e', '#98a4b2', '#eef0f2', silver=0.85, metallic=0.45, fin_up=('#141c32', '#0c1224', 0.92), fin_low=('#2a3448', '#e8ecf0', 0.9),
                                  patterns=[('finlets', True), ('sheen', 0.55)]),
                         eye=dict(s=0.1, z=0.28, r=0.024, iris='#c4c6c4', pupil=1.2), anim=dict(amp=0.95, freq=1.5)),

    # ===== 연안·외해 회유어 =====
    # 만새기 (Coryphaena hippurus): 수컷은 이마가 깎아지른 듯 높다, 머리부터 꼬리까지 이어진 등지느러미, 청록 등·금빛 옆구리·파란 잔점
    # 이마가 칼로 자른 듯 평평하게 서고(mouth_h·blunt) 입은 그 밑, 꼬리는 노랗다. 등은 청록, 옆구리 위는 연두, 아래는 샛노랑.
    # 입이 커서 위턱뼈 끝이 눈 밑까지, 눈은 입꼬리 바로 위 낮은 자리에 금빛 테
    'mahimahi': dict(type='E', name='만새기', depth=0.24, width=0.09, tmax=0.2, body_end=0.81, ped=0.13, arch=0.66, snout=0.2, blunt=0.6, mouth_h=0.36, mouth=-0.35, xsec=1.7,
                     gape=0.056, mouth_slope=0.15, maxilla=0.7,
                     fins=[_fin('dorsal', (0.06, 0.97), 0.42, prof=[(0, 0.8), (0.1, 1.0), (0.5, 0.75), (1, 0.4)]),
                           _fin('anal', (0.5, 0.97), 0.32, prof=[(0, 0.8), (0.2, 1.0), (1, 0.45)]),
                           _fin('caudal', h=0.4, shape='deepfork', group='low'),
                           _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.25, 0.25), h=0.11)],
                     colors=P('#147a86', '#a8c83a', '#f4dc34', silver=0.4, metallic=0.5, fin_up=('#1e4a9a', '#2a5ac0', 0.88), fin_low=('#e0c020', '#f0d040', 0.85),
                              patterns=[('stripe', dict(v=0.62, w=0.12, color='#4aa860', a=0.45)),
                                        ('spots', dict(n=150, r=(1.8, 3.2), color='#1a3a7a', v=(0.25, 0.92), a=0.85)), ('sheen', 0.5)]),
                     eye=dict(s=0.075, z=-0.05, r=0.018, iris='#9a8030'), anim=dict(amp=1.0, freq=1.3)),
    # 날쌔기 (Rachycentron canadum): 넓적하고 납작한 머리, 짧은 가시 등지느러미, 짙은 갈색 등에 흰 세로 띠 두 줄, 상어 같은 몸놀림
    # 위아래로 눌린 넓적한 머리가 뾰족하게 모이고 아래턱이 나왔다, 작은 눈, 꼬리는 크지 않은 갈라진 꼬리.
    # 옆구리 가운데 짙은 갈색 띠를 위아래 흰 띠가 감싼다
    'cobia': dict(type='E', name='날쌔기', depth=0.14, width=0.15, head_w=1.3, head_flat=0.3, tmax=0.34, body_end=0.81, ped=0.2, snout=0.72, blunt=0.9, mouth=0.1,
                  gape=0.075, jaw=0.012, xsec=2.4,
                  fins=[_fin('dorsal', (0.26, 0.38), 0.14, prof=[(0, 1.0), (1, 0.8)], spiny=True),
                        _fin('dorsal', (0.42, 0.78), 0.55, prof=[(0, 0.5), (0.12, 1.0), (0.35, 0.5), (1, 0.35)]),
                        _fin('anal', (0.52, 0.78), 0.45, prof=[(0, 0.5), (0.12, 1.0), (0.35, 0.5), (1, 0.35)]),
                        _fin('caudal', h=0.27, shape='fork'),
                        _fin('pectoral', s=(0.25, 0.25), h=0.18, long=True), _fin('pelvic', s=(0.3, 0.3), h=0.08)],
                  colors=P('#2a221e', '#4e4038', '#eee6da', silver=0.25, fin_up=('#2a2420', '#1e1a16', 0.92), fin_low=('#3a322a', '#2a241e', 0.9),
                           patterns=[('stripe', dict(v=0.6, w=0.025, color='#ece4d8', a=0.85)), ('stripe', dict(v=0.43, w=0.022, color='#e4dccc', a=0.7)),
                                     ('stripe', dict(v=0.515, w=0.05, color='#1a1410', a=0.75))]),
                  eye=dict(s=0.08, z=0.42, r=0.014, iris='#8a6a40'), anim=dict(amp=1.0, freq=1.0)),
    # 퀸피시 (Scomberoides commersonnianus): 옆으로 납작한 길쭉한 은빛 몸, 뾰족한 머리, 옆줄 위 검은 점 한 줄, 낫 모양 둘째 등·뒷지느러미
    # 몸은 머리 뒤에서 가장 높고, 뾰족한 머리에 눈 뒤까지 찢어진 큰 입. 옆줄을 따라 큰 검은 타원 점 대여섯 개
    'queenfish': dict(type='D', name='퀸피시', depth=0.25, width=0.08, tmax=0.36, ped=0.12, arch=0.52, snout=0.9, blunt=1.0, mouth=0.2, gape=0.1, jaw=0.012, xsec=1.6, scales=0,
                      fins=[_fin('dorsal', (0.26, 0.38), 0.14, prof=[(0, 1.0), (1, 0.8)], spiny=True),
                            _fin('dorsal', (0.44, 0.84), 0.4, prof=[(0, 1.0), (0.15, 0.55), (1, 0.3)]),
                            _fin('anal', (0.5, 0.84), 0.38, prof=[(0, 1.0), (0.15, 0.55), (1, 0.3)]),
                            _fin('caudal', h=0.32, shape='deepfork'),
                            _fin('pectoral', s=(0.27, 0.27), h=0.12), _fin('pelvic', s=(0.32, 0.32), h=0.06)],
                      colors=P('#3e5e70', '#ccd6dc', '#f4f6f4', silver=0.9, metallic=0.35, fin_up=('#5a6e78', '#3a4a54', 0.8), fin_low=('#d0c8a0', '#e0d8b4', 0.65),
                               patterns=[('spots', dict(n=6, r=(12, 15), color='#262e32', v=(0.64, 0.64), u=(0.3, 0.72), row=True, a=0.85)), ('sheen', 0.5)]),
                      eye=dict(iris='#d8d0b0'), anim=dict(amp=1.0, freq=1.3)),
    # 동갈삼치 (Scomberomorus commerson): 삼치보다 크고 길다, 은회색 몸에 물결 같은 가는 세로 줄무늬
    # 길고 낮은 첫 등지느러미(몸의 4분의 1), 짧은 낫 모양 둘째 등·뒷지느러미 뒤로 토막지느러미, 뾰족한 머리에 큰 입(입꼬리가 눈 가운데 밑까지). 가늘고 촘촘한 물결 띠
    'narrowbarredmackerel': dict(type='E', name='동갈삼치', depth=0.15, width=0.1, tmax=0.42, ped=0.15, snout=0.92, blunt=1.05, mouth=0.1, gape=0.088,
                                 fins=[_fin('dorsal', (0.24, 0.48), 0.22, prof=[(0, 1.0), (0.2, 0.9), (1, 0.3)], spiny=True),
                                       _fin('dorsal', (0.53, 0.61), 0.42, prof=[(0, 1.0), (1, 0.3)]),
                                       _fin('anal', (0.57, 0.65), 0.4, prof=[(0, 1.0), (1, 0.3)]),
                                       _fin('caudal', h=0.28, shape='deepfork'),
                                       _fin('pectoral', s=(0.24, 0.24), h=0.12), _fin('pelvic', s=(0.32, 0.32), h=0.06)],
                                 colors=P('#2a4462', '#b8c6d0', '#f4f6f4', silver=0.85, metallic=0.35, fin_up=('#3a5068', '#2a3c50', 0.8), fin_low=('#b8c0c4', '#d0d8dc', 0.6),
                                          patterns=[('bands', dict(n=40, u=(0.25, 0.92), w=0.011, color='#26385a', v=(0.3, 0.95), a=0.65)), ('finlets', True)]),
                                 eye=dict(iris='#c8c8b0'), anim=dict(amp=1.1, freq=1.4)),
    # 백다랑어 (Thunnus tonggol): 작고 날씬한 다랑어, 꼬리자루가 길다, 배에 흰 무늬, 노란 토막지느러미
    # 등은 검푸르고 옆구리 아래는 은백색, 배에 흰 타원 점이 줄지어, 가슴지느러미는 검다
    'longtailtuna': dict(type='E', name='백다랑어', depth=0.2, width=0.14, tmax=0.38, body_end=0.8, ped=0.09, snout=0.92, blunt=0.95, mouth=0.05, gape=0.068,
                         fins=[_fin('dorsal', (0.3, 0.43), 0.4, prof=[(0, 1.0), (1, 0.25)], spiny=True), _fin('dorsal', (0.51, 0.58), 0.6, prof=[(0, 1.0), (1, 0.3)]),
                               _fin('anal', (0.55, 0.62), 0.55, prof=[(0, 1.0), (1, 0.3)]), _fin('caudal', h=0.4, shape='deepfork'),
                               _fin('pectoral', s=(0.26, 0.26), h=0.16, long=True, group='up'), _fin('pelvic', s=(0.3, 0.3), h=0.06)],
                         colors=P('#0c1838', '#aab4c0', '#f2f4f4', silver=0.85, metallic=0.4, fin_up=('#1a2a48', '#10203a', 0.9), fin_low=('#a8a890', '#c8c098', 0.75),
                                  patterns=[('spots', dict(n=60, r=(3, 5), color='#ffffff', v=(0.08, 0.4), u=(0.35, 0.85), a=0.6)), ('finlets', True), ('sheen', 0.5)]),
                         eye=dict(iris='#9aa2a8'), anim=dict(amp=0.95, freq=1.6)),
    # 블루핀 트레발리 (Caranx melampygus): GT보다 날씬하고 머리가 덜 가파르다, 청동빛 몸에 검푸른 잔점, 전기처럼 파란 지느러미
    # 머리선이 매끈하게 비탈지고 주둥이가 조금 뾰족하다. 등 쪽 절반에 검푸른 잔점, 은빛 바탕에 청동·연둣빛이 돈다.
    # 큰 눈에 노란 홍채, 입꼬리는 눈 앞 가장자리 밑까지
    'bluefintrevally': dict(type='D', name='블루핀 트레발리', depth=0.32, width=0.12, tmax=0.42, snout=0.72, blunt=0.95, mouth=0.05, gape=0.085, ped=0.13, scales=0,
                            fins=[_fin('dorsal', (0.3, 0.45), 0.3, prof=[(0, 0.6), (0.3, 1.0), (1, 0.4)], spiny=True),
                                  _fin('dorsal', (0.48, 0.8), 0.55, prof=[(0, 1.0), (0.2, 0.65), (1, 0.25)]),
                                  _fin('anal', (0.55, 0.8), 0.48, prof=[(0, 1.0), (0.2, 0.65), (1, 0.25)]),
                                  _fin('caudal', h=0.36, shape='deepfork'), _fin('pectoral', s=(0.28, 0.28), h=0.24, long=True), _fin('pelvic', s=(0.34, 0.34), h=0.08)],
                            colors=P('#34505a', '#a8b4a8', '#e8eae4', silver=0.7, metallic=0.35, fin_up=('#2a58b0', '#3c7ae8', 0.88), fin_low=('#3462b0', '#4a88e0', 0.85),
                                     patterns=[('scutes', dict(color='#4a6a8a', a=0.7)), ('stripe', dict(v=0.7, w=0.15, color='#8a9a6a', a=0.3)),
                                               ('spots', dict(n=160, r=(1.5, 3), color='#142030', v=(0.45, 0.95), a=0.8))]),
                            eye=dict(r=0.026, iris='#e0c040'), anim=dict(amp=1.0, freq=1.35)),
    # 스넛노즈 폼파노 (Trachinotus blochii): 둥글고 뭉툭한 주둥이, 높고 납작한 몸, 낫 모양 둘째 등·뒷지느러미, 노란 뒷지느러미
    # 낫처럼 길게 뻗은 둘째 등·뒷지느러미 앞 엽과 깊게 갈라진 꼬리가 모두 노란 바탕에 끝이 거뭇하다. 몸은 마름모꼴 은빛, 배는 금빛.
    # 큰 눈이 낮게 — 입 바로 위 뒤쪽, 그 위로 높고 둥근 이마
    'pompano': dict(type='D', name='스넛노즈 폼파노', depth=0.44, width=0.1, tmax=0.4, ped=0.12, arch=0.55, snout=0.45, blunt=0.85, mouth=-0.2, gape=0.04, scales=0,
                    fins=[_fin('dorsal', (0.3, 0.38), 0.06, prof=[(0, 1.0), (1, 0.8)], spiny=True),
                          _fin('dorsal', (0.42, 0.8), 0.62, prof=[(0, 1.0), (0.12, 0.42), (1, 0.2)], group='low'),
                          _fin('anal', (0.46, 0.8), 0.62, prof=[(0, 1.0), (0.12, 0.42), (1, 0.2)]),
                          _fin('caudal', h=0.42, shape='deepfork', group='low'),
                          _fin('pectoral', s=(0.3, 0.3), h=0.13), _fin('pelvic', s=(0.34, 0.34), h=0.06)],
                    colors=P('#5a6e78', '#c8d0d4', '#e8d898', silver=0.9, metallic=0.35, fin_up=('#4a5a66', '#2a3640', 0.88), fin_low=('#e0a838', '#4a4430', 0.88),
                             patterns=[('sheen', 0.5)]),
                    eye=dict(s=0.12, z=0.12, r=0.026, iris='#c8b070'), anim=dict(amp=0.95, freq=1.3)),

    # ===== 산호초·암초 =====
    # 대왕바리 (Epinephelus lanceolatus): 가장 큰 바리. 굵고 둥근 몸, 넓적한 머리, 회갈색 얼룩에 옅은 점, 노르스름한 지느러미에 검은 점
    # 머리가 크고 넓적하며 아래턱이 나온 큰 입(입꼬리는 눈 뒤 끝 밑), 작은 눈
    'daewangbari': dict(type='C', name='대왕바리', depth=0.3, width=0.19, head_w=1.2, mouth=0.3, snout=0.8, blunt=0.9, mouth_h=0.22, gape=0.11, jaw=0.014, lips=1.3, scales=0,
                        mouth_slope=0.15,
                        fins=[_fin('dorsal', (0.3, 0.8), 0.28, prof=[(0, 0.6), (0.15, 1.0), (0.6, 0.8), (1, 1.15)], spiny=True),
                              _fin('anal', (0.62, 0.8), 0.32), _fin('caudal', h=0.22, shape='round'),
                              _fin('pectoral', s=(0.26, 0.26), h=0.17), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                        colors=P('#33342a', '#5a5a46', '#9a9478', silver=0.05, fin_up=('#4a4632', '#c0a030', 0.9), fin_low=('#4a4632', '#b89a30', 0.9),
                                 patterns=[('mottle', dict(color='#22231c', a=0.55, scale=0.06)), ('blotches', dict(color='#1e1e18', a=0.35)),
                                           ('spots', dict(n=90, r=(3, 6), color='#b8b090', v=(0.15, 0.95), a=0.45)), ('finspots', '#1a1a14')]),
                        eye=dict(s=0.1, z=0.56, r=0.013, iris='#7a6a40'), anim=dict(amp=0.75, freq=0.65)),
    # 코랄트라우트 (Plectropomus leopardus): 붉은 주황 몸 전체에 파란 잔점, 살짝 오목한 꼬리, 바리형 큰 입
    # 길쭉한 몸에 뾰족한 머리, 아래턱이 나오고 눈 밑까지 찢어진 입, 파란 점은 잘고 빽빽하다. 눈은 붉은 주홍 홍채
    'coraltrout': dict(type='C', name='코랄트라우트', depth=0.25, width=0.14, snout=0.88, blunt=1.0, mouth=0.25, gape=0.11, jaw=0.012, mouth_slope=0.15, scales=0,
                       fins=[_fin('dorsal', (0.3, 0.8), 0.3, prof=[(0, 0.6), (0.15, 1.0), (0.6, 0.75), (1, 1.1)], spiny=True),
                             _fin('anal', (0.62, 0.8), 0.32), _fin('caudal', h=0.24, shape='emarg'),
                             _fin('pectoral', s=(0.26, 0.26), h=0.16), _fin('pelvic', s=(0.3, 0.3), h=0.13)],
                       colors=P('#a8341e', '#d8543a', '#eca088', silver=0.1, fin_up=('#c04a30', '#d86048', 0.88), fin_low=('#d05a3a', '#e07050', 0.85),
                                patterns=[('spots', dict(n=280, r=(2, 3.5), color='#3a90e0', v=(0.05, 1.0), a=0.95)), ('finspots', '#3a80d0')]),
                       eye=dict(z=0.5, iris='#d04a28'), anim=dict(amp=0.9, freq=1.0)),
    # 골든스내퍼 (Lutjanus johnii): 청동·금빛 퉁돔, 비늘마다 짙은 점, 둘째 등지느러미 밑 검은 점 하나, 노르스름한 지느러미
    # 퉁돔답게 곧게 비탈진 이마와 뾰족한 주둥이(위턱뼈 끝이 눈 앞 가장자리 밑까지), 은빛 도는 연한 금색 바탕에 비늘마다 짙은 점이 줄을 이루고, 옆줄 위 검은 반점이 뚜렷하다
    'goldensnapper': dict(type='D', name='골든스내퍼', depth=0.34, width=0.13, tmax=0.42, snout=0.85, blunt=1.0, mouth=0.1, gape=0.1, scales=45,
                          colors=P('#7a6e50', '#d4c6a6', '#f4ecdc', silver=0.55, fin_up=('#a8884a', '#c8a860', 0.85), fin_low=('#d8b050', '#e8c460', 0.82),
                                   patterns=[('scale_dots', dict(color='#5a4220', a=0.75)),
                                             ('spots', dict(n=1, r=(17, 17), u=(0.57, 0.57), v=(0.65, 0.65), color='#2a2018', a=0.9))]),
                          eye=dict(iris='#d0a040')),
    # 갈돔 (Lethrinus nebulosus): 길고 비스듬한 주둥이와 두툼한 입술, 누런 갈색 몸에 비늘마다 파란 반짝이, 뺨에 파란 줄
    # 머리가 길고 주둥이가 길게 비스듬히 뾰족하다(갈돔류의 얼굴), 노르스름한 지느러미와 꼬리
    'spangledemperor': dict(type='D', name='갈돔', depth=0.32, width=0.13, tmax=0.46, arch=0.64, snout=0.98, blunt=1.15, mouth=-0.15, mouth_h=0.13, lips=1.4, scales=45,
                            colors=P('#76704e', '#ccc29e', '#eeeadc', silver=0.45, fin_up=('#b8984a', '#d0b058', 0.82), fin_low=('#c8b070', '#d8c088', 0.8),
                                    patterns=[('spots', dict(n=150, r=(3, 4.5), color='#3a8ac8', v=(0.3, 0.95), u=(0.2, 0.92), a=0.9)),
                                              ('eyeline', dict(color='#3a7ab8', w=0.012, n=2))]),
                            eye=dict(s=0.17, z=0.45, iris='#c8a040')),
    # 범프헤드 패럿피시 (Bolbometopon muricatum): 이마가 혹처럼 솟은 뭉툭한 머리, 앵무새 부리 이빨, 칙칙한 청록 몸, 이마는 연둣빛
    # 혹이 따로 없어 snout을 아주 작게(뭉툭) · tmax를 앞으로 · arch를 높여 이마를 세웠다
    # 이마 혹(hump)을 눈 앞 위에 크게 세우고, 드러난 부리(두툼한 입술)는 허옇다. 위턱뼈 판 없이 입술 사이로 이빨판(부리)이 드러난다
    'bumpheadparrot': dict(type='D', name='범프헤드 패럿피시', depth=0.42, width=0.17, tmax=0.28, arch=0.7, snout=0.25, hump=0.13, hump_s=0.06, mouth=-0.45, lips=1.8, maxilla=0,
                           ped=0.35, xsec=1.8, scales=28,
                           fins=[_fin('dorsal', (0.3, 0.84), 0.22, prof=[(0, 0.6), (0.15, 1.0), (1, 0.9)], spiny=True),
                                 _fin('anal', (0.64, 0.84), 0.26), _fin('caudal', h=0.28, shape='emarg'),
                                 _fin('pectoral', s=(0.28, 0.28), h=0.16), _fin('pelvic', s=(0.34, 0.34), h=0.11)],
                           colors=P('#2a4a4c', '#4a7068', '#8aa094', silver=0.1, fin_up=('#2e4a42', '#24383a', 0.9), fin_low=('#3e5a50', '#2e4a42', 0.9),
                                    patterns=[('scale_edge', 0.6), ('throat', '#b8c0a0'),
                                              ('spots', dict(n=1, r=(30, 30), u=(0.05, 0.05), v=(0.82, 0.82), color='#a8c098', a=0.6)),
                                              ('spots', dict(n=1, r=(9, 9), u=(0.008, 0.008), v=(0.38, 0.38), color='#e0d8c0', a=0.85))]),
                           eye=dict(s=0.15, z=0.35, r=0.016, iris='#c0b060'), anim=dict(amp=0.8, freq=0.8)),
    # 나폴레옹피시 (Cheilinus undulatus): 이마에 큰 혹, 두툼한 입술, 청록 몸에 비늘마다 짙은 세로 줄, 눈 뒤로 검은 줄 두 가닥, 둥근 꼬리
    # 혹 밑으로 두툼한 입술이 앞으로 나온다. 입은 낮고 짧아 입꼬리가 눈보다 한참 앞 (놀래기라 위턱뼈가 입술에 가려 안 보인다)
    'napoleonwrasse': dict(type='C', name='나폴레옹피시', depth=0.34, width=0.16, head_w=1.1, tmax=0.3, arch=0.72, snout=0.35, hump=0.12, mouth=0.0, lips=2.0, scales=34,
                           gape=0.055, mouth_slope=0.2, maxilla=0,
                           fins=[_fin('dorsal', (0.3, 0.84), 0.25, prof=[(0, 0.5), (0.6, 0.7), (0.9, 1.35), (1, 1.0)], spiny=True),
                                 _fin('anal', (0.62, 0.84), 0.32, prof=[(0, 0.5), (0.6, 1.0), (1, 0.8)]),
                                 _fin('caudal', h=0.26, shape='round'),
                                 _fin('pectoral', s=(0.27, 0.27), h=0.17), _fin('pelvic', s=(0.32, 0.32), h=0.12)],
                           colors=P('#265650', '#3a8478', '#80b4a4', silver=0.15, fin_up=('#2a6058', '#c8b048', 0.85), fin_low=('#3a7a70', '#2a5a54', 0.85),
                                    patterns=[('scale_edge', 0.6), ('bands', dict(n=30, u=(0.25, 0.92), w=0.012, color='#1e403c', v=(0.1, 0.95), a=0.45)),
                                              ('eyeline', dict(color='#18302e', w=0.014, n=2)), ('mottle', dict(color='#c8b860', a=0.14, scale=0.05))]),
                           eye=dict(s=0.14, z=0.38, r=0.016, iris='#d0a030'), anim=dict(amp=0.8, freq=0.75)),

    # ===== 하구·맹그로브 =====
    # 황소상어 (Carcharhinus leucas): 아주 짧고 뭉툭한 둥근 주둥이, 그 밑 배 쪽의 초승달 입(shark), 높은 삼각 첫 등지느러미,
    # 큰 가슴지느러미, 몸 앞쪽 아가미구멍 다섯 줄, 회색 등과 흰 배. 꼬리는 위 꼬리가 큰 상어 꼬리 (species_med2._shark_fins)
    'bullshark': dict(type='E', name='황소상어', depth=0.17, width=0.15, tmax=0.34, body_end=0.8, ped=0.17, arch=0.55, snout=0.4, mouth=0.0, xsec=2.2, scales=0,
                      shark=dict(mouth=0.07, gills=(0.18, 0.235), rostrum=0.26), bones=7,
                      fins=_shark_fins((0.3, 0.42), 0.95, (0.7, 0.74), 0.28, (0.72, 0.76), 0.24, (0.84, 1.0), 1.7, (0.92, 1.0), 0.95, 0.24, 0.3, 0.58),
                      colors=P('#585e64', '#888e94', '#eceae6', silver=0.1, fin_up=('#4e5458', '#3a3e42', 0.95), fin_low=('#5a6064', '#3e4246', 0.95),
                               patterns=[]),
                      eye=dict(s=0.09, z=0.3, r=0.011, iris='#1a1c1e'), anim=dict(amp=0.9, freq=0.8)),
    # 스레드핀 (Eleutheronema tetradactylum): 툭 튀어나온 둥근 주둥이 밑에 붙은 입, 기름눈꺼풀 낀 눈, 가슴지느러미 밑 실 가닥(수염으로 표현)
    # 비늘은 잘고, 반투명한 둥근 주둥이가 입 위로 처마처럼 나온다
    'threadfin': dict(type='C', name='스레드핀', depth=0.22, width=0.12, tmax=0.38, arch=0.58, snout=0.5, mouth=-0.4, scales=75,
                      fins=[_fin('dorsal', (0.3, 0.41), 0.42, prof=[(0, 0.6), (0.2, 1.0), (1, 0.3)], spiny=True),
                            _fin('dorsal', (0.55, 0.68), 0.4, prof=[(0, 1.0), (1, 0.35)]),
                            _fin('anal', (0.58, 0.7), 0.34, prof=[(0, 1.0), (1, 0.35)]),
                            _fin('caudal', h=0.34, shape='deepfork'),
                            _fin('pectoral', s=(0.24, 0.24), h=0.15), _fin('pelvic', s=(0.32, 0.32), h=0.1)],
                      barbels=[(0.25, -0.65, 0.09, 0.0035, 'down'), (0.255, -0.75, 0.075, 0.003, 'down')],
                      colors=P('#56687a', '#c4ccd2', '#f2f2ee', silver=0.8, metallic=0.25, fin_up=('#6a7884', '#4a5864', 0.8), fin_low=('#d8c070', '#e8d088', 0.75),
                               patterns=[('sheen', 0.4)]),
                      eye=dict(s=0.07, z=0.45, r=0.024, iris='#d0d8d8'), anim=dict(amp=1.0, freq=1.2)),
    # 레이디피시 (Elops machnata): 가늘고 긴 은빛 몸, 끝에 붙은 입, 등지느러미 하나, 깊게 갈라진 꼬리, 잔 비늘
    # 원뿔처럼 뾰족한 머리에 큰 눈, 가는 위턱뼈가 눈 뒤 끝 밑까지
    'ladyfish': dict(type='B', name='레이디피시', depth=0.15, width=0.09, tmax=0.42, snout=0.9, blunt=1.0, mouth=0.15, gape=0.1, maxilla=0.7, scales=90,
                     fins=[_fin('dorsal', (0.42, 0.53), 0.6, prof=[(0, 0.7), (0.15, 1.0), (1, 0.3)]),
                           _fin('anal', (0.68, 0.78), 0.35, prof=[(0, 1.0), (1, 0.4)]),
                           _fin('caudal', h=0.3, shape='deepfork'),
                           _fin('pectoral', s=(0.22, 0.22), h=0.11), _fin('pelvic', s=(0.45, 0.45), h=0.09)],
                     colors=P('#46677e', '#c8d4dc', '#f4f6f6', silver=0.9, metallic=0.3, fin_up=('#6a8090', '#8aa0ae', 0.65), fin_low=('#d0d4c4', '#e0e2d4', 0.55),
                              patterns=[('sheen', 0.5)]),
                     eye=dict(s=0.1, z=0.25, r=0.022, iris='#e0dcc8'), anim=dict(amp=1.1, freq=1.4)),
    # 인도태평양 타폰 (Megalops cyprinoides): 거울 같은 큰 은빛 비늘, 위로 들린 입, 등지느러미 마지막 살이 실처럼 길다
    # 아래턱이 크게 앞으로 나와 위로 비스듬히 열린 입, 큰 눈, 등은 올리브빛. 등지느러미는 작은 삼각형에 마지막 살 하나만 뒤로 길게
    'tarpon': dict(type='B', name='인도태평양 타폰', depth=0.24, width=0.1, tmax=0.42, arch=0.46, snout=0.75, mouth=0.55, mouth_slope=-0.3, gape=0.07, jaw=0.014, scales=26,
                   fins=[_fin('dorsal', (0.44, 0.54), 0.6, prof=[(0, 0.8), (0.15, 1.0), (0.55, 0.42), (0.85, 0.5), (1, 1.5)]),
                         _fin('anal', (0.66, 0.8), 0.42, prof=[(0, 1.0), (1, 0.4)]),
                         _fin('caudal', h=0.32, shape='deepfork'),
                         _fin('pectoral', s=(0.22, 0.22), h=0.12), _fin('pelvic', s=(0.44, 0.44), h=0.1)],
                   colors=P('#4a5a4c', '#c8ccc4', '#f2f4f0', silver=0.95, metallic=0.45, fin_up=('#7a8070', '#5a6050', 0.7), fin_low=('#d0ccb0', '#dcd8c0', 0.6),
                            patterns=[('scale_edge', 0.7), ('sheen', 0.6)]),
                   eye=dict(s=0.1, z=0.3, r=0.03, iris='#d8c890'), anim=dict(amp=1.05, freq=1.3)),
    # 물총고기 (Toxotes jaculatrix): 곧고 평평한 등선, 뾰족한 주둥이에 위로 들린 입, 큰 눈, 뒤로 물러난 등지느러미, 은백색 몸에 검은 쐐기 띠
    # 첫 검은 띠는 눈을 지나고, 등 쪽 쐐기 띠가 뒤로 다섯, 꼬리는 연한 노란빛
    'archerfish': dict(type='D', name='물총고기', depth=0.38, width=0.12, tmax=0.52, arch=0.4, snout=0.85, blunt=1.0, mouth=0.4, mouth_slope=-0.2, jaw=0.008, scales=40,
                       fins=[_fin('dorsal', (0.55, 0.82), 0.38, prof=[(0, 0.6), (0.3, 1.0), (1, 0.7)], spiny=True),
                             _fin('anal', (0.5, 0.84), 0.38, prof=[(0, 0.5), (0.3, 1.0), (1, 0.7)], spiny=True),
                             _fin('caudal', h=0.24, shape='truncate', group='low'),
                             _fin('pectoral', s=(0.3, 0.3), h=0.14), _fin('pelvic', s=(0.36, 0.36), h=0.11)],
                       colors=P('#6a7a5a', '#d6dace', '#f4f4ee', silver=0.75, fin_up=('#3a3c30', '#1e2018', 0.88), fin_low=('#d8cc90', '#b0a060', 0.75),
                                patterns=[('bands', dict(n=6, u=(0.1, 0.86), w=0.065, color='#18181a', v=(0.45, 1.0), a=0.9, taper=True))]),
                       eye=dict(s=0.13, z=0.5, r=0.032, iris='#d8c070'), anim=dict(amp=1.0, freq=1.3)),

    # ===== 베트남 연안 =====
    # 병어 (Pampus argenteus): 마름모꼴로 아주 높고 납작한 은빛 몸, 아주 작은 입, 배지느러미 없음, 낫 모양 등·뒷지느러미
    # 머리가 작고 입은 아주 작다, 등·뒷지느러미 앞 끝만 낫처럼 높이 솟고 뒤는 낮게 이어진다
    'silverpomfret': dict(type='D', name='병어', depth=0.6, width=0.08, tmax=0.42, arch=0.5, snout=0.35, mouth=0.0, gape=0.02, ped=0.14, xsec=1.5, scales=0,
                          fins=[_fin('dorsal', (0.38, 0.82), 0.4, prof=[(0, 0.5), (0.1, 1.0), (0.3, 0.35), (1, 0.15)]),
                                _fin('anal', (0.42, 0.82), 0.4, prof=[(0, 0.5), (0.1, 1.0), (0.3, 0.35), (1, 0.15)]),
                                _fin('caudal', h=0.32, shape='deepfork'),
                                _fin('pectoral', s=(0.3, 0.3), h=0.2, long=True)],
                          colors=P('#8e98a2', '#d0d6da', '#eef0f0', silver=0.95, metallic=0.45, fin_up=('#a8b0b8', '#8a949e', 0.7), fin_low=('#b8c0c8', '#a0a8b0', 0.65),
                                   patterns=[('sheen', 0.6), ('spots', dict(n=200, r=(1, 2), color='#6a7480', v=(0.4, 1.0), a=0.35))]),
                          eye=dict(s=0.13, z=0.15, r=0.024, iris='#c8ccd0'), anim=dict(amp=0.8, freq=1.0)),
    # 옐로핀 시브림 (Acanthopagrus latus): 감성돔 닮은 은회색 몸, 노란 배·뒷지느러미, 가는 세로 줄
    # 감성돔처럼 비탈진 이마에 뾰족한 주둥이, 비늘 줄을 따라 가는 세로줄이 또렷하고 배·뒷·꼬리지느러미가 샛노랗다
    'yellowfinseabream': dict(type='D', name='옐로핀 시브림', depth=0.4, width=0.13, tmax=0.42, snout=0.72, blunt=0.95, mouth=-0.25, gape=0.04,
                              fins=[_fin('dorsal', (0.3, 0.62), 0.38, prof=[(0, 0.4), (0.18, 1.0), (1, 0.6)], spiny=True),
                                    _fin('dorsal', (0.62, 0.8), 0.32, prof=[(0, 0.75), (1, 0.55)]),
                                    _fin('anal', (0.62, 0.8), 0.32, prof=[(0, 0.7), (0.2, 1.0), (1, 0.5)], spiny=True),
                                    _fin('caudal', h=0.26, shape='fork', group='low'),
                                    _fin('pectoral', s=(0.3, 0.3), h=0.2, long=True), _fin('pelvic', s=(0.35, 0.35), h=0.12)],
                              colors=P('#4e5a62', '#b8c0c4', '#eef0ec', silver=0.7, fin_up=('#5a6268', '#3a4248', 0.82), fin_low=('#e0b828', '#f0cc40', 0.85),
                                       patterns=[('hstripes', dict(n=11, v=(0.2, 0.85), w=0.007, color='#5a666e', a=0.5))]),
                              eye=dict(iris='#c0b080')),
    # 러셀 스내퍼 (Lutjanus russellii): 분홍빛 은색 퉁돔, 노르스름한 가는 세로줄, 옆줄 위 검은 점 하나
    # 곧게 비탈진 이마에 뾰족한 주둥이, 옆줄 위 검은 점은 눈만큼 크다, 배·뒷지느러미는 노랗다
    'russellsnapper': dict(type='D', name='러셀 스내퍼', depth=0.33, width=0.12, tmax=0.42, snout=0.88, blunt=1.0, mouth=0.1, gape=0.1, scales=45,
                           colors=P('#8a6458', '#e2cec4', '#f6f0ea', silver=0.5, fin_up=('#c8a070', '#d8b080', 0.8), fin_low=('#e8c048', '#f0d060', 0.8),
                                    patterns=[('hstripes', dict(n=7, v=(0.3, 0.85), w=0.009, color='#c09048', a=0.55)),
                                              ('spots', dict(n=1, r=(17, 17), u=(0.58, 0.58), v=(0.66, 0.66), color='#1e1814', a=0.9))]),
                           eye=dict(r=0.024, iris='#d0a050')),
    # 독가시치 (Siganus fuscescens): 둥글넓적한 납작한 몸, 토끼 같은 작은 입, 등·뒷지느러미 가시, 올리브 갈색에 흰 잔점
    # 머리는 작고 주둥이가 살짝 뾰족한 끝에 토끼 입, 흰 잔점이 몸 전체에 빽빽하다. 큰 눈에 노란 홍채
    'rabbitfish': dict(type='D', name='독가시치', depth=0.38, width=0.11, tmax=0.45, snout=0.78, blunt=0.9, mouth=0.0, gape=0.025, scales=0,
                       fins=[_fin('dorsal', (0.25, 0.85), 0.3, prof=[(0, 0.5), (0.15, 1.0), (0.6, 0.8), (1, 0.9)], spiny=True),
                             _fin('anal', (0.55, 0.85), 0.28, prof=[(0, 0.6), (0.2, 1.0), (1, 0.8)], spiny=True),
                             _fin('caudal', h=0.26, shape='emarg'),
                             _fin('pectoral', s=(0.3, 0.3), h=0.14), _fin('pelvic', s=(0.34, 0.34), h=0.1)],
                       colors=P('#5e5e40', '#8e8e6a', '#d4d4bc', silver=0.25, fin_up=('#6a6a4a', '#4a4a32', 0.85), fin_low=('#8a8a68', '#6a6a4c', 0.8),
                                patterns=[('spots', dict(n=280, r=(2, 3.5), color='#e8e4cc', v=(0.15, 0.97), a=0.75)), ('mottle', dict(color='#3e3e2a', a=0.2))]),
                       eye=dict(r=0.026, iris='#e0c040'), anim=dict(amp=0.9, freq=1.1)),
}
