"""실물 사진 대조 진행률: 설계 파일을 기준 커밋과 비교해 정의가 바뀐 어종 수와 남은 어종.

  bpyenv/Scripts/python -I tools/blender/photo_match_progress.py [기준 커밋, 기본 56c1da11 = 대조 시작 전]
"""
import re, subprocess, sys
BASE = sys.argv[1] if len(sys.argv) > 1 else '56c1da11'
files = {'A': ('fish_species', 0), 'B': ('fish_species', 1), 'C': ('species_korea2', None), 'C2': ('species_korea3', None),
         'D': ('species_sea', None), 'D2': ('species_sea2', None), 'E': ('species_sea_fresh', None), 'E2': ('species_africa', None),
         'F': ('species_med', None), 'F2': ('species_med2', None)}
skip = {'carp', 'goldkoi', 'catfish', 'octopus', 'cuttlefish', 'squid', 'giantoctopus', 'bigfinsquid', 'skate', 'anglerfish', 'bullshark',
        'blacktipreefshark', 'giantstingray', 'smoothhound', 'blueshark', 'mako', 'thresher', 'commonstingray'}
def blocks(src, half):
    if half is not None:
        k = src.index('# ===== 동해 (헌화로')
        src = src[:k] if half == 0 else src[k:]
    parts = re.split(r"\n    (?=(?:# [^\n]*\n    )*'[a-z0-9]+': dict\()", src)
    out = {}
    for p in parts:
        m = re.search(r"^(?:# [^\n]*\n    )*'([a-z0-9]+)': dict\(", p)
        if m: out[m.group(1)] = p
    return out
tot_n = tot_c = 0
for key, (f, half) in files.items():
    path = f'tools/blender/{f}.py'
    old = subprocess.run(['git', 'show', f'{BASE}:{path}'], capture_output=True).stdout.decode('utf-8')
    new = open(path, encoding='utf-8').read()
    bo, bn = blocks(old, half), blocks(new, half)
    ids = [i for i in bn if i not in skip]
    ch = [i for i in ids if bo.get(i, '').replace('\r', '') != bn[i].replace('\r', '')]
    tot_n += len(ids); tot_c += len(ch)
    print(f"{key:3} {f:18} {len(ch):2}/{len(ids):2}", "남은:", " ".join(i for i in ids if i not in ch))
print(f'합계 {tot_c}/{tot_n}')
