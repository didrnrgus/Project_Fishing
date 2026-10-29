"""지중해 거점 어종 뒷부분 (docs/fish-db/03_지중해_어종DB.md MED-M23~M44) — 설계 시트 형식은 fish_species.py 머리말.

상어·가오리·라이언피시·청대치 틀은 따로 없어서 가까운 틀을 고쳐 쓴다 (어종마다 타협한 점을 주석에 적었다).
상어 꼬리(위 꼬리가 큰 비대칭 꼬리): 꼬리지느러미 모양은 위아래 대칭밖에 없어서 꼬리지느러미를 빼고, 꼬리자루 위에 높은
등지느러미를 하나 더 달아 위 꼬리로, 꼬리자루 밑에 뒷지느러미를 하나 더 달아 작은 아래 꼬리로 쓴다 (지느러미 살이 뒤로 38° 눕기
때문에 꼬리자루를 따라 길게 깔면 비스듬히 솟은 위 꼬리가 된다). 대신 모델 길이가 꼬리 끝까지 1이 안 되고 0.9쯤이다.
초승달 꼬리인 청상아리만 꼬리지느러미를 그대로 쓴다.
"""
from fish_species import P, _fin  # noqa: F401


def _shark_fins(d1, d1h, d2, d2h, an, anh, lobe, lobeh, low, lowh, pec, pech, pel):
    """상어 지느러미: 첫째·둘째 등지느러미, 뒷지느러미, 위 꼬리(꼬리자루 위 등지느러미), 아래 꼬리(꼬리자루 밑 뒷지느러미),
    긴 가슴지느러미, 배지느러미. 대칭 꼬리지느러미는 달지 않는다 (달면 다랑어 꼬리처럼 보였다)."""
    return [_fin('dorsal', d1, d1h, prof=[(0, 1.0), (0.55, 0.6), (1, 0.12)]),
            _fin('dorsal', d2, d2h, prof=[(0, 1.0), (1, 0.25)]),
            _fin('anal', an, anh, prof=[(0, 1.0), (1, 0.25)]),
            _fin('dorsal', lobe, lobeh, prof=[(0, 0.05), (0.45, 0.4), (1, 1.0)]),
            _fin('anal', low, lowh, prof=[(0, 0.2), (1, 1.0)]),
            _fin('pectoral', s=(pec, pec), h=pech, long=True),
            _fin('pelvic', s=(pel, pel), h=0.05)]


SPECIES = {
    # ===== 암초·난파선 바닥 =====
    # 유럽붕장어 (Conger conger): 붕장어보다 훨씬 굵고 크다. 잿빛 등, 흰 배, 등지느러미가 가슴지느러미 끝 바로 뒤에서 시작하고
    # 지느러미 가장자리가 검다. 비늘 없음
    'europeanconger': dict(type='G', name='유럽붕장어', depth=0.085, width=0.075, mouth=0.1, snout=0.65,
                           fins=[_fin('dorsal', (0.2, 1.0), 0.36, prof=[(0, 0.15), (0.12, 0.7), (0.5, 1.0), (1, 0.9)]),
                                 _fin('anal', (0.42, 1.0), 0.34, prof=[(0, 0.15), (0.15, 0.75), (0.55, 1.0), (1, 0.9)]),
                                 _fin('caudal', h=0.05, shape='point'),
                                 _fin('pectoral', s=(0.1, 0.1), h=0.055)],
                           colors=P('#34383a', '#686a66', '#dcdad0', silver=0.1, fin_up=('#6a6c6a', '#121212', 0.9), fin_low=('#c8c4b8', '#181818', 0.88),
                                    patterns=[('pores', dict(color='#e8e6dc', v=0.55, n=34)), ('mottle', dict(color='#2a2c2e', a=0.25, scale=0.1))]),
                           eye=dict(r=0.011, iris='#b8b8a8')),
    # 붉은쏨뱅이 (Scorpaena scrofa): 쏨뱅이보다 크고 붉다. 큰 머리, 흰 얼룩과 짙은 붉은 무늬, 턱 밑 살갗 술(수염으로 흉내)
    'redscorpionfish': dict(type='C', name='붉은쏨뱅이', depth=0.29, width=0.17, head_w=1.2, tmax=0.33, mouth=0.25, snout=0.72, scales=0,
                            fins=[_fin('dorsal', (0.24, 0.56), 0.5, prof=[(0, 0.4), (0.2, 1.0), (0.85, 0.5), (1, 0.4)], spiny=True),
                                  _fin('dorsal', (0.56, 0.78), 0.45, prof=[(0, 0.8), (0.3, 1.0), (1, 0.45)]),
                                  _fin('anal', (0.6, 0.78), 0.45, prof=[(0, 0.6), (0.3, 1.0), (1, 0.45)], spiny=True),
                                  _fin('caudal', h=0.22, shape='truncate'),
                                  _fin('pectoral', s=(0.27, 0.27), h=0.17), _fin('pelvic', s=(0.3, 0.3), h=0.12)],
                            barbels=[(0.05, -0.55, 0.02, 0.005, 'down'), (0.1, 0.55, 0.02, 0.004, 'up')],
                            eye=dict(s=0.12, z=0.48, r=0.026, iris='#d05028'),
                            colors=P('#8a2418', '#c8482e', '#f0b8a0', silver=0.0, fin_up=('#b04028', '#e07858', 0.85), fin_low=('#c85a3a', '#e88a68', 0.85),
                                     patterns=[('mottle', dict(color='#5a140c', a=0.6, scale=0.045)), ('spots', dict(n=45, r=(3, 7), color='#f4d8c0', v=(0.15, 0.9), a=0.55)),
                                               ('finspots', '#6a1a10')])),
    # 페인티드콤버 (Serranus scriba): 작은 바리과. 갈색 세로 띠, 배 가운데 파란 얼룩, 머리에 파란 꼬불 줄
    'paintedcomber': dict(type='C', name='페인티드콤버', depth=0.25, width=0.12, mouth=0.25, snout=0.75, scales=70,
                          fins=[_fin('dorsal', (0.27, 0.8), 0.4, prof=[(0, 0.5), (0.15, 1.0), (0.55, 0.8), (0.75, 1.05), (1, 0.6)], spiny=True),
                                _fin('anal', (0.6, 0.78), 0.42), _fin('caudal', h=0.22, shape='truncate'),
                                _fin('pectoral', s=(0.26, 0.26), h=0.14), _fin('pelvic', s=(0.3, 0.3), h=0.11)],
                          colors=P('#7a4a2a', '#c08a58', '#f0dcc0', silver=0.1, fin_up=('#b07848', '#d8b080', 0.75), fin_low=('#c8a070', '#e0c098', 0.7),
                                   patterns=[('bands', dict(n=6, u=(0.3, 0.88), w=0.055, color='#4a2414', v=(0.35, 1.0), a=0.6)),
                                             ('eyeline', dict(color='#3a70c0', w=0.012, n=4)),
                                             ('spots', dict(n=1, r=(36, 36), u=(0.42, 0.42), v=(0.25, 0.25), color='#2a5ab8', a=0.85)),
                                             ('spots', dict(n=30, r=(2, 3), color='#e04a30', v=(0.2, 0.6), u=(0.3, 0.8), a=0.5))]),
                          eye=dict(iris='#c8a040')),
    # 무지개놀래기 (Coris julis, 수컷 체색): 가늘고 긴 몸, 길고 낮은 등지느러미, 옆구리를 따라 주황 지그재그 띠, 가슴지느러미 뒤 검은 점
    'rainbowwrasse': dict(type='C', name='무지개놀래기', depth=0.18, width=0.1, tmax=0.38, mouth=0.0, snout=0.85, arch=0.52, scales=60,
                          fins=[_fin('dorsal', (0.22, 0.8), 0.32, prof=[(0, 1.6), (0.1, 0.9), (0.5, 0.85), (1, 0.7)], spiny=True),
                                _fin('anal', (0.5, 0.8), 0.3, prof=[(0, 0.7), (0.3, 1.0), (1, 0.7)]),
                                _fin('caudal', h=0.2, shape='truncate'),
                                _fin('pectoral', s=(0.24, 0.24), h=0.11), _fin('pelvic', s=(0.3, 0.3), h=0.07)],
                          colors=P('#2a6a5a', '#5aa0a0', '#f2f0ea', silver=0.3, fin_up=('#d85a30', '#3a8ac0', 0.75), fin_low=('#5aa0b0', '#8ac8d0', 0.65),
                                   patterns=[('stripe', dict(v=0.5, w=0.06, color='#f07a28', a=0.9)),
                                             ('hstripes', dict(n=1, v=(0.42, 0.42), w=0.01, color='#1a3048', a=0.7)),
                                             ('spots', dict(n=1, r=(9, 9), u=(0.32, 0.32), v=(0.55, 0.55), color='#101820', a=0.9)),
                                             ('eyeline', dict(color='#2a60b0', w=0.02, n=2))]),
                          eye=dict(r=0.018, iris='#d84a30')),

    # ===== 모래 바닥 =====
    # 지중해노랑가오리 (Dasyatis pastinaca): 납작이(F) 틀을 옆으로 눕힌 마름모꼴 원반(disc까지) + 가늘어지는 채찍 꼬리.
    # ray: 두 눈이 등 가운데 좌우로 나란히, 눈 뒤 숨구멍. 타협: 꼬리 독가시·꼬리 밑 피부 주름은 생략
    'commonstingray': dict(type='F', name='지중해노랑가오리', flat='left', depth=0.5, width=0.075, tmax=0.22, body_end=1.0, disc=0.44, ped=0.045,
                           snout=0.95, mouth=0.0, xsec=1.6, tail_w=0.4, bones=8, ray=dict(eye_z=0.16),
                           fins=[_fin('caudal', h=0.02, shape='point')],
                           colors=P('#6a5a40', '#8a7a5a', '#f2eee4', silver=0.0, fin_up=('#5a4a34', '#3a3024', 0.95), fin_low=('#5a4a34', '#3a3024', 0.95),
                                    patterns=[('mottle', dict(color='#4a3e2a', a=0.35, scale=0.08))]),
                           eye=dict(s=0.105, r=0.014, iris='#a08850'), anim=dict(amp=0.6, freq=0.7)),
    # 붉은촉수 (Mullus barbatus): 가파른 이마, 아래로 처진 입, 턱 밑 수염 한 쌍, 떨어진 등지느러미 둘, 분홍빛 붉은 몸, 큰 비늘
    'redmullet': dict(type='C', name='붉은촉수', depth=0.24, width=0.12, tmax=0.32, snout=0.3, mouth=-0.35, arch=0.62, scales=34,
                      fins=[_fin('dorsal', (0.3, 0.44), 0.5, prof=[(0, 0.7), (0.25, 1.0), (1, 0.3)], spiny=True),
                            _fin('dorsal', (0.58, 0.72), 0.38, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)]),
                            _fin('anal', (0.6, 0.73), 0.34), _fin('caudal', h=0.22, shape='fork'),
                            _fin('pectoral', s=(0.25, 0.25), h=0.12), _fin('pelvic', s=(0.3, 0.3), h=0.1)],
                      barbels=[(0.06, -0.75, 0.07, 0.004, 'down')],
                      colors=P('#b83c3c', '#e07a6a', '#f6e4dc', silver=0.35, fin_up=('#e09080', '#f0c0b0', 0.6), fin_low=('#f0b0a0', '#f8d0c0', 0.6),
                               patterns=[('scale_edge', 0.4), ('sheen', 0.3)]),
                      eye=dict(s=0.13, z=0.5, r=0.024, iris='#d0a040')),

    # ===== 상어류 =====
    # 유럽별상어 (Mustelus mustelus): 날씬한 잿빛 소형 상어. 크기가 비슷한 등지느러미 둘, 고양이 눈, 무늬 없음
    'smoothhound': dict(type='E', name='유럽별상어', depth=0.12, width=0.11, tmax=0.32, body_end=0.8, ped=0.2, arch=0.55, snout=0.7, mouth=0.0,
                        shark=dict(mouth=0.085, gills=(0.2, 0.25), rostrum=0.3),
                        xsec=2.1, scales=0, bones=7,
                        fins=_shark_fins((0.3, 0.42), 0.75, (0.6, 0.7), 0.6, (0.68, 0.76), 0.35, (0.84, 1.0), 1.7, (0.92, 1.0), 0.9, 0.22, 0.12, 0.52),
                        colors=P('#5e6266', '#8a8e90', '#eceae4', silver=0.05, fin_up=('#5a5e62', '#4a4e52', 0.97), fin_low=('#6a6e72', '#5a5e62', 0.97),
                                 patterns=[]),
                        eye=dict(s=0.1, z=0.3, r=0.016, iris='#6a8a50'), anim=dict(amp=1.0, freq=1.0)),
    # 청새리상어 (Prionace glauca): 아주 날씬한 몸, 길고 뾰족한 주둥이, 낫처럼 긴 가슴지느러미, 쪽빛 등 · 하늘색 옆 · 흰 배
    'blueshark': dict(type='E', name='청새리상어', depth=0.12, width=0.11, tmax=0.34, body_end=0.8, ped=0.17, arch=0.55, snout=0.9, mouth=0.0, blunt=0.6,
                      shark=dict(mouth=0.095, gills=(0.22, 0.27), rostrum=0.34),
                      xsec=2.1, scales=0, bones=7,
                      fins=_shark_fins((0.38, 0.48), 0.75, (0.68, 0.73), 0.3, (0.72, 0.77), 0.28, (0.84, 1.0), 1.8, (0.92, 1.0), 0.9, 0.27, 0.26, 0.56),
                      colors=P('#1e3c8a', '#3e6ec0', '#f2f4f6', silver=0.15, metallic=0.15, fin_up=('#24448e', '#1a3070', 0.97), fin_low=('#3a64b0', '#2a4c90', 0.95),
                               patterns=[('sheen', 0.3)]),
                      eye=dict(s=0.09, z=0.25, r=0.017, iris='#101418'), anim=dict(amp=1.1, freq=0.9)),
    # 청상아리 (Isurus oxyrinchus): 굵은 방추형, 원뿔 주둥이, 초승달 꼬리(위아래 거의 대칭이라 꼬리지느러미 그대로), 짧은 가슴지느러미,
    # 짙은 금속빛 남색 등. 꼬리자루가 아주 가늘다
    'mako': dict(type='E', name='청상아리', depth=0.17, width=0.15, tmax=0.38, body_end=0.78, ped=0.1, arch=0.53, snout=0.85, mouth=0.0,
                 shark=dict(mouth=0.09, gills=(0.2, 0.26), rostrum=0.3),
                 xsec=2.2, scales=0, bones=7,
                 fins=[_fin('dorsal', (0.34, 0.45), 0.85, prof=[(0, 1.0), (0.55, 0.6), (1, 0.12)]),
                       _fin('dorsal', (0.72, 0.75), 0.18), _fin('anal', (0.74, 0.77), 0.16),
                       _fin('caudal', h=0.4, shape='deepfork'),
                       _fin('pectoral', s=(0.26, 0.26), h=0.15, long=True), _fin('pelvic', s=(0.6, 0.6), h=0.05)],
                 colors=P('#1a2a58', '#4a5e92', '#f2f4f6', silver=0.3, metallic=0.35, fin_up=('#1c2c5a', '#141e44', 0.97), fin_low=('#2a3a6a', '#1e2c58', 0.95),
                          patterns=[('sheen', 0.5)]),
                 eye=dict(s=0.1, z=0.25, r=0.02, iris='#06080a'), anim=dict(amp=0.9, freq=1.5)),
    # 환도상어 (Alopias vulpinus): 몸 절반이 낫 같은 위 꼬리. 타협: 위 꼬리는 꼬리지느러미 'point' 모양으로 몸 축을 따라 길게 뻗게 하고
    # (실제로는 살짝 위로 휜다), 작은 아래 꼬리는 꼬리자루 밑의 뒷지느러미로 흉내낸다
    'thresher': dict(type='E', name='환도상어', depth=0.11, width=0.1, tmax=0.4, body_end=0.54, ped=0.2, arch=0.55, snout=0.65, mouth=0.0,
                     shark=dict(mouth=0.11, gills=(0.24, 0.3), rostrum=0.3),
                     xsec=2.1, scales=0, bones=9,
                     fins=[_fin('dorsal', (0.42, 0.56), 0.95, prof=[(0, 1.0), (0.55, 0.6), (1, 0.12)]),
                           _fin('dorsal', (0.9, 0.93), 0.15), _fin('anal', (0.92, 0.95), 0.15),
                           _fin('anal', (0.96, 1.0), 0.7, prof=[(0, 0.3), (1, 1.0)]),
                           _fin('caudal', h=0.05, shape='point'),
                           _fin('pectoral', s=(0.3, 0.3), h=0.17, long=True), _fin('pelvic', s=(0.72, 0.72), h=0.05)],
                     colors=P('#4a4658', '#7e7a8c', '#f0f0f0', silver=0.25, metallic=0.25, fin_up=('#4a4658', '#3a3648', 0.97), fin_low=('#5a5668', '#4a4658', 0.95),
                              patterns=[('sheen', 0.35)]),
                     eye=dict(s=0.12, z=0.3, r=0.02, iris='#0a0a10'), anim=dict(amp=1.2, freq=0.9)),

    # ===== 떼고기 =====
    # 대서양고등어 (Scomber colias): 고등어와 같은 물결무늬 + 배 쪽 옆구리에 잿빛 점
    'atlanticmackerel': dict(type='E', name='대서양고등어',
                             colors=P('#265a62', '#a0bcbc', '#f2f4f2', silver=0.8, metallic=0.35, fin_up=('#3a5a5a', '#5a7a7a', 0.7), fin_low=('#9aaaaa', '#b8c4c4', 0.6),
                                      patterns=[('waves', dict(color='#0e2a32', v=(0.62, 1.0), a=0.9)), ('finlets', True),
                                                ('spots', dict(n=70, r=(2.5, 4.5), color='#6a7c84', v=(0.25, 0.52), u=(0.2, 0.85), a=0.6))]),
                             eye=dict(iris='#202a2a')),
    # 유럽전갱이 (Trachurus trachurus): 전갱이와 같은 틀, 등이 조금 더 짙은 청회색. 옆줄 방패 비늘, 아가미 뚜껑 검은 점
    'atlantichorsemackerel': dict(type='E', name='유럽전갱이', depth=0.21, width=0.11,
                                  colors=P('#3a5464', '#b0c0c4', '#f2f2ee', silver=0.85, metallic=0.3, fin_up=('#5a7078', '#7a9098', 0.65), fin_low=('#c8c8b8', '#dcdccc', 0.6),
                                           patterns=[('scutes', dict(color='#7a8a90', a=0.75)), ('earflap', '#141c20')]),
                                  eye=dict(r=0.027, iris='#d0ccb8')),
    # 보그 (Boops boops): 날씬한 도미, 아주 큰 눈, 작은 입, 등은 누런 올리브빛, 옆구리에 금빛 가는 줄 서너 개, 가슴지느러미 밑동 검은 점
    'bogue': dict(type='C', name='보그', depth=0.2, width=0.11, tmax=0.4, snout=0.6, mouth=0.05, scales=70,
                  fins=[_fin('dorsal', (0.3, 0.74), 0.38, prof=[(0, 0.6), (0.2, 1.0), (0.7, 0.75), (1, 0.6)], spiny=True),
                        _fin('anal', (0.6, 0.78), 0.32), _fin('caudal', h=0.26, shape='deepfork'),
                        _fin('pectoral', s=(0.25, 0.25), h=0.12), _fin('pelvic', s=(0.3, 0.3), h=0.09)],
                  colors=P('#6a7a5a', '#c4c8b0', '#f4f4ee', silver=0.7, metallic=0.2, fin_up=('#a0a080', '#c0c0a0', 0.6), fin_low=('#d0ccb0', '#e0dcc8', 0.55),
                           patterns=[('hstripes', dict(n=4, v=(0.42, 0.78), w=0.01, color='#d8b850', a=0.6)),
                                     ('spots', dict(n=1, r=(5, 5), u=(0.26, 0.26), v=(0.42, 0.42), color='#2a2a20', a=0.8))]),
                  eye=dict(s=0.11, z=0.3, r=0.032, iris='#d8d0b0')),

    # ===== 홍해 유입 외래종 =====
    # 은빛뺨복어 (Lagocephalus sceleratus): 길쭉한 복어. 둥근 몸통, 뭉툭한 머리, 뒤쪽에 마주 보는 작은 등·뒷지느러미, 오목한 꼬리,
    # 등에 잔 검은 점, 옆구리를 따라 은빛 띠, 흰 배. 배지느러미 없음
    'toadfish': dict(type='E', name='은빛뺨복어', depth=0.19, width=0.19, tmax=0.32, body_end=0.82, ped=0.22, arch=0.52, snout=0.25, mouth=0.0,
                     xsec=2.0, scales=0,
                     fins=[_fin('dorsal', (0.66, 0.74), 0.5, prof=[(0, 1.0), (1, 0.35)]),
                           _fin('anal', (0.67, 0.75), 0.45, prof=[(0, 1.0), (1, 0.35)]),
                           _fin('caudal', h=0.24, shape='emarg'),
                           _fin('pectoral', s=(0.3, 0.3), h=0.09)],
                     colors=P('#5a6058', '#c8d0d0', '#f6f6f2', silver=0.6, metallic=0.3, fin_up=('#7a8078', '#9aa098', 0.7), fin_low=('#d0d4d0', '#e0e4e0', 0.6),
                              patterns=[('spots', dict(n=110, r=(2, 3.5), color='#22261e', v=(0.62, 1.0), a=0.8)),
                                        ('stripe', dict(v=0.48, w=0.07, color='#e8eef2', a=0.75)), ('sheen', 0.4)]),
                     eye=dict(s=0.13, z=0.35, r=0.028, iris='#c8c8b8'), anim=dict(amp=0.9, freq=1.1)),
    # 라이언피시 (Pterois miles): 농어형(C)에 아주 높고 들쭉날쭉한 가시 등지느러미, 몸길이 절반쯤인 부채 가슴지느러미,
    # 흰 바탕에 적갈색 세로 띠, 지느러미에도 점 줄무늬. 타협: 가시 사이 막이 찢어진 실 모양, 눈 위 촉수는 생략
    'lionfish': dict(type='C', name='라이언피시', depth=0.3, width=0.13, head_w=1.1, mouth=0.15, snout=0.6, scales=0,
                     fins=[_fin('dorsal', (0.2, 0.58), 1.25, prof=[(0, 0.55), (0.2, 1.0), (0.6, 0.85), (1, 0.45)], spiny=True),
                           _fin('dorsal', (0.6, 0.8), 0.6, prof=[(0, 0.8), (0.3, 1.0), (1, 0.6)]),
                           _fin('anal', (0.62, 0.8), 0.55, prof=[(0, 0.7), (0.3, 1.0), (1, 0.6)]),
                           _fin('caudal', h=0.28, shape='round'),
                           _fin('pectoral', s=(0.28, 0.28), h=0.38), _fin('pelvic', s=(0.3, 0.3), h=0.16)],
                     colors=P('#7a2a20', '#ead4c4', '#f6ece4', silver=0.0, fin_up=('#e8cfc0', '#8a3626', 0.7), fin_low=('#e0c4b4', '#8a3626', 0.7),
                              patterns=[('bands', dict(n=15, u=(0.08, 0.95), w=0.035, color='#6a1c14', v=(0.0, 1.0), a=0.85)),
                                        ('bands', dict(n=15, u=(0.11, 0.98), w=0.012, color='#b05a40', v=(0.15, 0.95), a=0.6)),
                                        ('finspots', '#7a2618')]),
                     eye=dict(s=0.12, z=0.42, r=0.024, iris='#b03a28'), anim=dict(amp=0.7, freq=0.7)),
    # 청대치 (Fistularia commersonii): 아주 길고 가는 몸, 피리 같은 관 주둥이(부리 관으로), 뒤쪽에 마주 보는 작은 등·뒷지느러미,
    # 꼬리 가운데 실 같은 긴 줄(꼬리 'point'로 흉내), 올리브빛 몸에 파란 점선 두 줄
    'cornetfish': dict(type='E', name='청대치', depth=0.04, width=0.035, tmax=0.5, body_end=0.86, ped=0.35, arch=0.5, snout=0.95, mouth=0.0,
                       xsec=2.0, scales=0, bones=12, bill=0.2,
                       fins=[_fin('dorsal', (0.8, 0.88), 1.2, prof=[(0, 1.0), (1, 0.4)]),
                             _fin('anal', (0.8, 0.88), 1.1, prof=[(0, 1.0), (1, 0.4)]),
                             _fin('caudal', h=0.06, shape='point'),
                             _fin('pectoral', s=(0.12, 0.12), h=0.035), _fin('pelvic', s=(0.5, 0.5), h=0.02)],
                       colors=P('#5a6a4e', '#a0ac96', '#eaece2', silver=0.4, metallic=0.2, fin_up=('#8a9a80', '#a8b8a0', 0.6), fin_low=('#a8b4a0', '#c0ccb8', 0.6),
                                patterns=[('spots', dict(n=34, r=(2, 3), color='#3aa0e8', v=(0.8, 0.8), u=(0.1, 0.95), row=True, a=0.9)),
                                          ('spots', dict(n=30, r=(2, 3), color='#3aa0e8', v=(0.6, 0.6), u=(0.12, 0.93), row=True, a=0.8))]),
                       eye=dict(s=0.06, z=0.3, r=0.009, iris='#c8b880'), anim=dict(amp=1.2, freq=0.8)),
    # 물결독가시치 (Siganus rivulatus): 타원형 납작한 몸, 작은 입, 길게 이어진 가시 등·뒷지느러미, 얕게 파인 꼬리,
    # 올리브 갈색 등에 물결 같은 가는 금빛 줄
    'marbledspinefoot': dict(type='D', name='물결독가시치', depth=0.36, width=0.1, tmax=0.42, snout=0.6, mouth=0.0, scales=0,
                             fins=[_fin('dorsal', (0.25, 0.84), 0.28, prof=[(0, 0.6), (0.15, 1.0), (0.7, 0.85), (1, 0.8)], spiny=True),
                                   _fin('anal', (0.5, 0.84), 0.24, prof=[(0, 0.6), (0.2, 1.0), (1, 0.8)], spiny=True),
                                   _fin('caudal', h=0.26, shape='emarg'),
                                   _fin('pectoral', s=(0.3, 0.3), h=0.13), _fin('pelvic', s=(0.34, 0.34), h=0.1)],
                             colors=P('#5e5a3c', '#a49e78', '#e4e0cc', silver=0.3, fin_up=('#8a8460', '#a8a080', 0.7), fin_low=('#b0a880', '#c8c0a0', 0.65),
                                      patterns=[('waves', dict(color='#d8c47a', v=(0.3, 1.0), a=0.45)), ('mottle', dict(color='#4a4630', a=0.3))]),
                             eye=dict(s=0.15, z=0.38, r=0.022, iris='#c0a050')),
}
