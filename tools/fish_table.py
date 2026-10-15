"""Assets/Scripts/Data/FishDatabase.cs에서 docs/02-game-design.md 11장 어종 표를 뽑는다.

  python tools/fish_table.py            # 표를 화면에 출력
  python tools/fish_table.py --write    # 02 문서 11장의 표(| 맵 | 어종 ... 다음 줄부터 빈 줄까지)를 바꾼다

맵 칸은 낚시터 짧은 이름을 · 로 잇는다. 한 바다를 모두 차지하면 '동해'·'남해'·'서해', 모든 바다면 '바다 전체'.
"""
import io
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SRC = os.path.join(ROOT, 'Assets', 'Scripts', 'Data', 'FishDatabase.cs')
DOC = os.path.join(ROOT, 'docs', '02-game-design.md')

SHORT = {'hangang': '한강', 'honghyeon': '홍현', 'seocheon': '서천', 'yeongheung': '영흥도', 'taean': '태안', 'samcheok': '삼척',
         'deungdae': '등대섬', 'heonhwa': '헌화로', 'yokji': '욕지도', 'cheongjin': '청진', 'geomun': '거문도', 'jeju': '제주',
         'halong': '하롱베이', 'santorini': '산토리니', 'nile': '나일강'}
ORDER = ['hangang', 'honghyeon', 'yokji', 'geomun', 'jeju', 'seocheon', 'yeongheung', 'taean', 'heonhwa', 'samcheok', 'deungdae', 'cheongjin',
         'halong', 'santorini', 'nile']
SOUTH = ['honghyeon', 'yokji', 'geomun', 'jeju']
WEST = ['seocheon', 'yeongheung', 'taean']
EAST = ['heonhwa', 'samcheok', 'deungdae', 'cheongjin']
GROUPS = {
    'Sea': ['honghyeon'], 'FishSpecies.SeaOnly': ['honghyeon'], 'West': ['seocheon'], 'FishSpecies.WestOnly': ['seocheon'],
    'Seas': SOUTH + WEST, 'WestSea': WEST, 'East': EAST, 'South': SOUTH, 'AllSeas': SOUTH + WEST + EAST,
    'FishSpecies.HanOnly': ['hangang'], 'FishSpecies.Both': ['hangang', 'honghyeon'], 'null': None,
}
HAB = {'Shallow': ('얕은 여울', '모래 여울'), 'Deep': ('깊은 물', '깊은 바다'), 'Rocky': ('석축·교각', '갯바위·암초'), 'Weed': ('수초', '해초밭')}
TIME = {'Dawn': '새벽', 'Morning': '아침', 'Day': '낮', 'Evening': '저녁', 'Night': '밤'}
SEASON = {'Spring': '봄', 'Summer': '여름', 'Autumn': '가을', 'Winter': '겨울'}
BAIT = {'Worm': '지렁이', 'Paste': '떡밥', 'Corn': '옥수수', 'Shrimp': '새우', 'Minnow': '생미끼', 'Lure': '루어', 'Krill': '크릴',
        'SeaWorm': '청갯지렁이', 'Squid': '오징어살', 'CutBait': '해체 미끼', 'Egi': '에기', 'Fly': '플라이'}


def field(block, name):
    m = re.search(r'\b' + name + r'\s*=\s*', block)
    if not m:
        return None
    i = m.end()
    depth = 0
    j = i
    while j < len(block):
        c = block[j]
        if c in '({[':
            depth += 1
        elif c in ')}]':
            if depth == 0:
                break
            depth -= 1
        elif c == ',' and depth == 0:
            break
        j += 1
    return block[i:j].strip()


def enum_list(expr, table, any_label='전부'):
    if expr is None:
        return None
    if expr.startswith('Any'):
        return [any_label]
    return [table[x] for x in re.findall(r'\.(\w+)', expr) if x in table]


def maps_of(expr):
    if expr is None:
        return ['hangang']
    if expr.startswith('M('):
        return re.findall(r'"(\w+)"', expr)
    return GROUPS[expr]


def map_label(ids):
    ids = [i for i in ORDER if i in ids]
    if set(ids) == set(SOUTH + WEST + EAST):
        return '바다 전체'
    out = []
    for region, name in ((SOUTH, '남해'), (WEST, '서해'), (EAST, '동해')):
        if all(r in ids for r in region):
            out.append(name)
            ids = [i for i in ids if i not in region]
    return '·'.join([SHORT[i] for i in ids] + out)


def species():
    src = io.open(SRC, encoding='utf-8').read()
    body = src[src.index('List<FishSpecies> All'):]
    body = body[:body.index('\n        };')]
    for b in body.split('new FishSpecies {')[1:]:
        rarity = int(field(b, 'Rarity'))
        if rarity == 0:
            continue
        maps = maps_of(field(b, 'Maps'))
        sea = maps is not None and maps[0] not in ('hangang', 'nile')
        num = lambda n: float(field(b, n).rstrip('f'))
        habs = enum_list(field(b, 'Habitats'), {k: v[1 if sea else 0] for k, v in HAB.items()})
        seasons = enum_list(field(b, 'Seasons'), SEASON)
        cond = ', '.join(seasons) if seasons else '사계절'
        if 'SchoolOnly = true' in b:
            cond += ' · 어군 전용'
        if 'FarSeaOnly = true' in b:
            cond += ' · 먼바다 전용'
        if 'BossCoins' in b:
            cond += f" · 괴물 어종(대물 코인 {field(b, 'BossCoins')})"
        yield dict(maps=maps, map=map_label(maps), name=field(b, 'Name').strip('"'), rarity=rarity,
                   len=f"{num('MinLen'):g}~{num('MaxLen'):g}", trophy=f"{num('TrophyLen'):g} {field(b, 'TrophyName').strip(chr(34))}",
                   hab=', '.join(habs), bait=', '.join(enum_list(field(b, 'Baits'), BAIT)), time=', '.join(enum_list(field(b, 'Times'), TIME)), cond=cond)


def table():
    rows = list(species())
    rows.sort(key=lambda r: (min(ORDER.index(m) for m in r['maps']), len(r['maps']), r['rarity']))
    lines = ['| 맵 | 어종 | ★ | 길이(cm) | 대물 | 서식지 | 미끼 | 시간 | 제철·조건 |', '|---|---|---|---|---|---|---|---|---|']
    for r in rows:
        lines.append(f"| {r['map']} | {r['name']} | {r['rarity']} | {r['len']} | {r['trophy']} | {r['hab']} | {r['bait']} | {r['time']} | {r['cond']} |")
    counts = {m: sum(1 for r in rows if m in r['maps']) for m in ORDER}
    return lines, len(rows), counts


if __name__ == '__main__':
    lines, total, counts = table()
    summary = ' · '.join(f'{SHORT[m]} {counts[m]}' for m in ORDER)
    if '--write' not in sys.argv:
        print('\n'.join(lines))
        print(f'\n{total}종 / {summary}')
        sys.exit(0)
    doc = io.open(DOC, encoding='utf-8').read()
    start = doc.index('| 맵 | 어종 | ★ |')
    end = doc.index('\n\n', start)
    doc = doc[:start] + '\n'.join(lines) + doc[end:]
    doc = re.sub(r'## 11\. 어종 \(\d+종', f'## 11. 어종 ({total}종', doc)
    doc = re.sub(r'맵: 한강 \d+[^\n]*?\(겹치는 어종 포함\)', f'맵: {summary} (겹치는 어종 포함)', doc)
    io.open(DOC, 'w', encoding='utf-8', newline='').write(doc)
    print(f'02 문서 갱신: {total}종 / {summary}')
