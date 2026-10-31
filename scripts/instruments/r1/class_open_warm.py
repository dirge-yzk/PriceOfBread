"""(704) r1 段0 ②：class_s の open（構築直後＝world.json）と warm（助走の直後）の工場労働者・農民の割合の差（読むだけ）。
使い方：python -X utf8 -I class_open_warm.py（生のフォルダは本文の base と arms に直書き＝D:/PoB_work/keep_702。消えたら再走が要る）
"""
import sys, csv, statistics as st, collections
def load(path):
    d = {}
    with open(path, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); next(r)
        for row in r:
            t = row[0]
            if t not in ('open', 'warm'): continue
            d.setdefault(t, {}).setdefault(row[1], {})[row[4]] = float(row[5])
    return d
def share(d, t, n, c):
    tot = sum(d[t][n].values()); return d[t][n][c] / tot
arms = {
 'm702': ('m702_ins', range(1, 11)), 'N1': ('n1', range(1, 11)), 'N2': ('n2', range(1, 11)), 'a11': ('a11', range(11, 21))}
base = 'D:/PoB_work/keep_702/'
focus = ['フランス','日本','イギリス','アメリカ','ロシア','プロイセン','オスマン','清','オーストリア']
res = {}
for arm, (dr, seeds) in arms.items():
    for s in seeds:
        try: d = load(f'{base}{dr}/class_s{s}.tsv')
        except Exception as e: print('ERR', arm, s, e); continue
        res[(arm, s)] = d
# check open identical across seeds
o = [res[k]['open'] for k in res]
print('open identical across all seeds:', all(x == o[0] for x in o))
nations = list(o[0].keys())
print('n nations', len(nations))
cls = ['貴族','農民','工場労働者','資本家','技師','兵士','聖職者','教師','官僚','医者','商人','職人','使用人']
# per-nation open vs warm for factory worker share: mean across arm seeds
out = collections.defaultdict(dict)
for arm in arms:
    ks = [k for k in res if k[0] == arm]
    for n in nations:
        try:
            ow = share(res[ks[0]], 'open', n, '工場労働者')
            ws = [share(res[k], 'warm', n, '工場労働者') for k in ks]
            op = share(res[ks[0]], 'open', n, '農民'); wp = [share(res[k], 'warm', n, '農民') for k in ks]
        except KeyError: continue
        out[arm][n] = (ow, st.mean(ws), min(ws), max(ws), op, st.mean(wp))
print('\n工場労働者の割合 open→warm(平均 [最小,最大]) / 農民の割合 open→warm')
for n in focus:
    line = n
    for arm in arms:
        if n in out[arm]:
            ow, wm, lo, hi, op, wp = out[arm][n]
            line += f'\t{arm}: {ow:.4f}→{wm:.4f}[{lo:.4f},{hi:.4f}] 農{op:.4f}→{wp:.4f}'
    print(line)
for arm in arms:
    diffs = [abs(v[1]-v[0]) for v in out[arm].values()]
    fdiffs = [abs(out[arm][n][1]-out[arm][n][0]) for n in focus if n in out[arm]]
    print(arm, '全61国の|差|の中央値(平均種)', round(st.median(diffs),5), '9国', round(st.median(fdiffs),5))
# per-seed medians over 61 and focus9 (the gate quantity per seed)
print('\n種ごとの 9国 |差| 中央値 / 61国 |差| 中央値')
for arm in arms:
    ks = sorted(k for k in res if k[0]==arm)
    v9=[];v61=[]
    for k in ks:
        d=res[k]
        dd=[abs(share(d,'warm',n,'工場労働者')-share(d,'open',n,'工場労働者')) for n in nations]
        d9=[abs(share(d,'warm',n,'工場労働者')-share(d,'open',n,'工場労働者')) for n in focus if n in d['open']]
        v61.append(st.median(dd)); v9.append(st.median(d9))
    print(arm, '9国', [round(x,4) for x in v9], 'min',round(min(v9),4),'max',round(max(v9),4), '| 61国 min',round(min(v61),4),'max',round(max(v61),4))
