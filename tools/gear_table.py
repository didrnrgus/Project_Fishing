"""Assets/Scripts/Data/GearDatabase.cs에서 장비(낚싯대·릴·줄·배) 표를 뽑아 docs/18-gear-table.md를 채운다.

  python tools/gear_table.py            # 표를 화면에 출력
  python tools/gear_table.py --write    # 18 문서의 <!-- gear-table:start --> ~ <!-- gear-table:end --> 사이를 바꾼다

장비 수치를 바꾸면 --write로 다시 뽑는다. 표 밖의 글(설계 의도·검토할 점)은 손으로 고친다.
"""
import io
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
DATA = os.path.join(ROOT, 'Assets', 'Scripts', 'Data')
DOC = os.path.join(ROOT, 'docs', '18-gear-table.md')
START, END = '<!-- gear-table:start -->', '<!-- gear-table:end -->'

MAPS = {'hangang': '한강', 'honghyeon': '홍현', 'seocheon': '서천', 'yeongheung': '영흥도', 'taean': '태안', 'samcheok': '삼척',
        'deungdae': '등대섬', 'heonhwa': '헌화로', 'yokji': '욕지도', 'cheongjin': '청진포구', 'geomun': '거문도', 'jeju': '제주',
        'halong': '하롱베이', 'santorini': '산토리니', 'nile': '나일강', 'angkor': '앙코르와트', 'vinpearl': '빈펄', 'donkhone': '돈콘',
        'phuket': '푸켓', 'bangkok': '방콕', 'kohsamui': '코사무이', 'langkawi': '랑카위', 'sentosa': '센토사', 'telokbatek': '텔록 바텍',
        'tioman': '티오만', 'tenggol': '탱글', 'crete': '크레타', 'zakynthos': '자킨토스', 'duden': '듀덴'}
KOREA = {'hangang', 'honghyeon', 'seocheon', 'yeongheung', 'taean', 'samcheok', 'deungdae', 'heonhwa', 'yokji', 'cheongjin', 'geomun', 'jeju'}
# GearDatabase에 출처가 안 적힌 보상 장비 (코드 위치)
SPECIAL = {
    'rod_gege': '대회 1위 (TournamentSystem)',
    'boat_blackspear': '출석 7일 (Attendance)',
}
SLOTS = [('Rod', '낚싯대'), ('Reel', '릴'), ('Line', '원줄'), ('Boat', '배')]
UP = 15   # RodUpgrade.MaxLevel


def read(name):
    return io.open(os.path.join(DATA, name), encoding='utf-8').read()


def num(v):
    return float(v.rstrip('f'))


def parse():
    src = read('GearDatabase.cs')
    gear = []
    for m in re.finditer(r'new GearDef \{(.*?)\}(?=,\s*\n)', src, re.S):
        body = m.group(1)
        g = {'Power': 1.0, 'Flex': 0.5, 'CastRange': 18.0, 'ReelSpeed': 3.0, 'Capacity': 60.0, 'Strength': 1.0,
             'BoatSpeed': 6.0, 'FuelCapacity': 20.0, 'FuelPerKm': 10.0, 'Stability': 0.5, 'Sail': False, 'OpenSea': True,
             'NotSold': False, 'Price': 0, 'Level': 1, 'TradeMap': None, 'TradeCost': 0}
        for k, v in re.findall(r'(\w+) = ("[^"]*"|GearSlot\.\w+|true|false|[\d.]+f?)', body):
            if v.startswith('"'):
                g[k] = v.strip('"')
            elif v.startswith('GearSlot.'):
                g[k] = v.split('.')[1]
            elif v in ('true', 'false'):
                g[k] = v == 'true'
            elif k in ('Price', 'Level'):
                g[k] = int(v)
            else:
                g[k] = num(v)
        if 'Id' in g and 'Slot' in g:
            gear.append(g)
    pat = re.compile(r'Trade\("(\w+)", "([^"]*)", GearSlot\.(\w+), (\d+), "(\w+)", (\d+), (?:null|"#\w+"), (?:null|"#\w+"),\s*"([^"]*)",([^)]*)\)')
    names = {'power': 'Power', 'flex': 'Flex', 'range': 'CastRange', 'speed': 'ReelSpeed', 'capacity': 'Capacity', 'strength': 'Strength'}
    for m in pat.finditer(src):
        iid, name, slot, lv, mp, cost, desc, rest = m.groups()
        g = {'Id': iid, 'Name': name, 'Slot': slot, 'Level': int(lv), 'TradeMap': mp, 'TradeCost': int(cost), 'Desc': desc,
             'NotSold': True, 'Price': 0, 'Power': 1.0, 'Flex': 0.5, 'CastRange': 18.0, 'ReelSpeed': 3.0, 'Capacity': 60.0, 'Strength': 1.0}
        for k, v in re.findall(r'(\w+): ([\d.]+)f', rest):
            g[names[k]] = float(v)
        gear.append(g)

    # 레벨 보상 (LevelRewards.All)
    rewards = {iid: lv for lv, iid in re.findall(r'\((\d+), "(\w+)", 1,', src)}
    # 대물 코인 교환 (ExchangePanel)
    ex = io.open(os.path.join(ROOT, 'Assets', 'Scripts', 'UI', 'ExchangePanel.cs'), encoding='utf-8').read()
    coins = {iid: int(c) for iid, c in re.findall(r'Icon = "(\w+)".*?Cost = (\d+), Coin = ItemDatabase\.BigCoinId', ex)}
    for g in gear:
        g['Source'] = source(g, rewards, coins)
    return gear


def currency(mp):
    return '나팔고동' if mp in KOREA else '황금거거'


def source(g, rewards, coins):
    if g.get('TradeMap'):
        return f"{MAPS[g['TradeMap']]} 어부 · {currency(g['TradeMap'])} {g['TradeCost']}"
    if not g['NotSold']:
        return f"상점 {g['Price']:,}G" if g['Price'] > 0 else '처음부터'
    if g['Id'] in rewards:
        return f"Lv.{rewards[g['Id']]} 보상"
    if g['Id'] in coins:
        return f"대물 코인 {coins[g['Id']]}"
    return SPECIAL.get(g['Id'], '?')


def kind(g):
    if g.get('TradeMap'):
        return 'npc'
    if not g['NotSold']:
        return 'shop'
    return 'reward'


TAG = {'shop': '상점', 'npc': '**마을**', 'reward': '보상'}


def flex(f):
    return '연조' if f >= 0.6 else '경조' if f <= 0.35 else '중경조'


def tables(gear):
    out = []
    by = lambda s: sorted([g for g in gear if g['Slot'] == s], key=lambda g: (g['Level'], kind(g) != 'shop', g['Name']))

    out.append('### 낚싯대\n')
    out.append('| Lv | 이름 | 구분 | 얻는 곳 | 힘 | 탄성 | 비거리 | +15 힘 | +15 비거리 |')
    out.append('|---:|---|---|---|---:|---|---:|---:|---:|')
    for g in by('Rod'):
        out.append(f"| {g['Level']} | {g['Name']} | {TAG[kind(g)]} | {g['Source']} | {g['Power']:.2f} | {flex(g['Flex'])} {g['Flex']:.2f} | "
                   f"{g['CastRange']:.0f}m | {g['Power'] * (1 + 0.03 * UP):.2f} | {g['CastRange'] + 0.4 * UP:.0f}m |")

    out.append('\n### 릴\n')
    out.append('| Lv | 이름 | 구분 | 얻는 곳 | 감기 | 권사량 | +15 감기 | +15 권사량 |')
    out.append('|---:|---|---|---|---:|---:|---:|---:|')
    for g in by('Reel'):
        out.append(f"| {g['Level']} | {g['Name']} | {TAG[kind(g)]} | {g['Source']} | {g['ReelSpeed']:.1f}m/s | {g['Capacity']:.0f}m | "
                   f"{g['ReelSpeed'] * (1 + 0.025 * UP):.2f}m/s | {g['Capacity'] + 5 * UP:.0f}m |")

    out.append('\n### 원줄 (강화 없음)\n')
    out.append('| Lv | 이름 | 구분 | 얻는 곳 | 강도 |')
    out.append('|---:|---|---|---|---:|')
    for g in by('Line'):
        out.append(f"| {g['Level']} | {g['Name']} | {TAG[kind(g)]} | {g['Source']} | {g['Strength']:.2f} |")

    out.append('\n### 배\n')
    out.append('| Lv | 이름 | 얻는 곳 | 최고 속도 | 연료통 | 1km 연료 | 안정성 | 먼바다 |')
    out.append('|---:|---|---|---:|---:|---:|---:|---|')
    for g in by('Boat'):
        sail = g.get('Sail')
        out.append(f"| {g['Level']} | {g['Name']} | {g['Source']} | {g['BoatSpeed'] * 3.6:.0f}km/h{' (바람)' if sail else ''} | "
                   f"{'-' if sail else '%.0fL' % g['FuelCapacity']} | {'-' if sail else '%.0fL' % g['FuelPerKm']} | {g['Stability']:.2f} | "
                   f"{'O' if g.get('OpenSea', True) else 'X'} |")

    # 레벨 곡선: 그 레벨에 쓸 수 있는 가장 좋은 상점 / 마을 / 보상 장비
    out.append('\n### 레벨 곡선 (그 레벨에 장착할 수 있는 가장 좋은 값, 강화 전)\n')
    out.append('상점은 골드만 있으면 바로, 마을은 그 낚시터에 가서 재화를 모아야, 보상은 대회·빙고·코인·출석 같은 조건이 있다. '
               '"마을/상점"은 같은 레벨 상점 최고값 대비 마을 장비 배율.\n')
    out.append('| Lv | 힘 상점 | 힘 마을 | 힘 보상 | 마을/상점 | 감기 상점 | 감기 마을 | 권사량 상점 | 권사량 마을 | 강도 상점 | 강도 마을 |')
    out.append('|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|')

    def best(slot, k, lv, key):
        v = [g[key] for g in gear if g['Slot'] == slot and kind(g) == k and g['Level'] <= lv]
        return max(v) if v else None

    f = lambda v, p=2: '-' if v is None else f'{v:.{p}f}'
    prev = None
    levels = sorted({g['Level'] for g in gear if g['Slot'] != 'Boat'} | {1, 40})
    for lv in levels:
        row = [best('Rod', 'shop', lv, 'Power'), best('Rod', 'npc', lv, 'Power'), best('Rod', 'reward', lv, 'Power'),
               best('Reel', 'shop', lv, 'ReelSpeed'), best('Reel', 'npc', lv, 'ReelSpeed'),
               best('Reel', 'shop', lv, 'Capacity'), best('Reel', 'npc', lv, 'Capacity'),
               best('Line', 'shop', lv, 'Strength'), best('Line', 'npc', lv, 'Strength')]
        if row == prev:
            continue
        prev = row
        ratio = f'×{row[1] / row[0]:.2f}' if row[1] and row[0] else '-'
        out.append(f'| {lv} | {f(row[0])} | {f(row[1])} | {f(row[2])} | {ratio} | {f(row[3], 1)} | {f(row[4], 1)} | '
                   f'{f(row[5], 0)} | {f(row[6], 0)} | {f(row[7])} | {f(row[8])} |')

    # 재화 비용 합계
    out.append('\n### 마을 교환 비용 합계\n')
    out.append('| 재화 | 장비 수 | 모두 바꾸는 데 | 가장 비싼 것 |')
    out.append('|---|---:|---:|---|')
    for cur in ('나팔고동', '황금거거'):
        items = [g for g in gear if g.get('TradeMap') and currency(g['TradeMap']) == cur]
        top = max(items, key=lambda g: g['TradeCost'])
        out.append(f"| {cur} | {len(items)} | {sum(g['TradeCost'] for g in items)}개 | {top['Name']} {top['TradeCost']}개 |")
    return '\n'.join(out)


def main():
    text = tables(parse())
    if '--write' not in sys.argv:
        sys.stdout.reconfigure(encoding='utf-8')
        print(text)
        return
    doc = io.open(DOC, encoding='utf-8').read()
    a, b = doc.index(START) + len(START), doc.index(END)
    io.open(DOC, 'w', encoding='utf-8', newline='\n').write(doc[:a] + '\n' + text + '\n' + doc[b:])
    print('wrote', os.path.relpath(DOC, ROOT))


if __name__ == '__main__':
    main()
