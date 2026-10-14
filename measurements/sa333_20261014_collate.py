# -*- coding: utf-8 -*-
"""(333) 腕の表（読むだけ）：python sa333_20261014_collate.py <作業フォルダ> 腕…   （帯 b10_<腕>_10.txt と read/ の読み手の出力から）
出す物：exact・near・列強・再現率・★赤 異常・兌換停止・計算量の推定・NG（新しい・外れた・正味）・空洞化・補給崩壊 と、ヌル腕 N1〜N4 の中央値・幅（最大−最小）。"""
import sys, re, os, statistics
W = sys.argv[1]; ARMS = sys.argv[2:]
def rd(p):
    try: return open(p, encoding='utf-8', errors='replace').read()
    except Exception: return ''
def first(rx, s, g=1, default=None):
    m = re.search(rx, s, re.M)
    return m.group(g) if m else default
rows = {}
for a in ARMS:
    b = rd(f'{W}/b10_{a}_10.txt'); c = rd(f'{W}/b10_{a}_10.txt.cost'); n = rd(f'{W}/read/ng_{a}.txt'); h = rd(f'{W}/read/hol_{a}.txt'); s = rd(f'{W}/read/supply_{a}.txt')
    r = {}
    r['exact'] = float(first(r'政体exact: [\d.]+ / \d+ = ([\d.]+)', b, default='nan')) * 100
    r['near'] = float(first(r'政体near: [\d.]+ / \d+ = ([\d.]+)', b, default='nan')) * 100
    r['gp_recall'] = float(first(r'列強・再現率: ([\d.]+) %', b, default='nan'))
    r['gp_match'] = float(first(r'列強: 1914の顔ぶれ一致 (\d+) %', b, default='nan'))
    r['red'] = int(first(r'★赤 異常: (\d+)件', b, default='-1'))
    r['gold'] = int(first(r'金本位の強制離脱（兌換停止）: 総(\d+)回', b, default='-1'))
    r['cost'] = float(first(r'推定の計算量（1試合）: ([\d.]+) 秒', c, default='nan'))
    ng = first(r'比べる rd:.*＝NG (\d+)', n); r['ng'] = int(ng) if ng else -1
    nw = first(r'新しい NG（基準では NG でない）: (\d+)', n); dr = first(r'外れた NG（比べる側で NG でない）: (\d+)', n)
    r['new'] = int(nw) if nw else -1; r['drop'] = int(dr) if dr else -1
    r['net'] = r['new'] - r['drop']
    r['hol10'] = first(r'ピークの1/10 未満: (\d+) 件', h, default='?')
    r['supply'] = first(r'seeds with >=1000 \(pop>=100\): (\d+) max per seed (\d+) total (\d+)', s, 0, '?')
    m = re.search(r'seeds with >=1000 \(pop>=100\): (\d+) max per seed (\d+) total (\d+)', s)
    r['supply'] = f'{m.group(1)}/10（種の最大 {m.group(2)}・合計 {m.group(3)}）' if m else '?'
    rows[a] = r
cols = [('exact', '%.1f'), ('near', '%.1f'), ('gp_recall', '%.1f'), ('gp_match', '%d'), ('red', '%d'), ('gold', '%d'), ('cost', '%.1f'), ('ng', '%d'), ('new', '%d'), ('drop', '%d'), ('net', '%+d'), ('hol10', '%s'), ('supply', '%s')]
names = {'exact': '政体exact', 'near': '政体near', 'gp_recall': '列強・再現率', 'gp_match': '列強の顔ぶれ一致', 'red': '★赤 異常', 'gold': '兌換停止(総)', 'cost': '推定の計算量(秒)', 'ng': 'NG', 'new': 'NG新', 'drop': 'NG外れ', 'net': 'NG正味', 'hol10': '空洞化1/10', 'supply': '千件級の補給崩壊'}
print('\t'.join(['量'] + ARMS + ['N中央値', 'N最小', 'N最大', 'N幅(最大−最小)']))
for k, f in cols:
    vals = [rows[a][k] for a in ARMS]
    ns = [rows[a][k] for a in ARMS if a.startswith('N') and isinstance(rows[a][k], (int, float))]
    ex = ['', '', '', '']
    if len(ns) >= 2:
        ex = [f % statistics.median(ns), f % min(ns), f % max(ns), f % (max(ns) - min(ns))]
    print('\t'.join([names[k]] + [(f % v if isinstance(v, (int, float)) else str(v)) for v in vals] + ex))
