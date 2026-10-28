"""(627) 合流の記録（読むだけ）：①発見の行が世界で最初に開いた年（disc_s の E 行・種ごとの最小の年→種の中央値〔開いた種の数〕）②補給崩壊（nation_s の populationK≥100 かつ supplyFillShare<0.3 の国×四半期の件数・種ごと・記録だけ）。
使い方：python -X utf8 -I rec627.py <腕名>=<計器の出力フォルダ（…/ins）> ...（種は 1〜10）
⚠ ① は disc_s の E 行（解禁の出来事）だけ＝開幕の既知は数えない。② の物差しは (617) の sup.txt と同じ（m617 で 29・N1 12・N2 3 を再現することを確かめて使う）。"""
import csv, os, sys, statistics as st, collections
csv.field_size_limit(10**9)
def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(v):
    try: return float(v)
    except: return float('nan')
arms = [a.split('=', 1) for a in sys.argv[1:]]
first = {}; sup = {}
for name, d in arms:
    fy = collections.defaultdict(list); per = []
    for s in range(1, 11):
        m = {}
        for r in rows(os.path.join(d, f'disc_s{s}.tsv')):
            if r['kind'] != 'E': continue
            y = int(fl(r['year'])); k = r['row']
            if k not in m or y < m[k]: m[k] = y
        for k, y in m.items(): fy[k].append(y)
        c = 0
        for r in rows(os.path.join(d, f'nation_s{s}.tsv')):
            if fl(r['populationK']) >= 100 and fl(r['supplyFillShare']) < 0.3: c += 1
        per.append(c)
    first[name] = fy; sup[name] = per
names = [a[0] for a in arms]
print('## 世界で最初に開いた年（種の中央値〔開いた種の数〕）')
print('行\t' + '\t'.join(names))
rows_all = sorted(set().union(*[set(first[n]) for n in names]))
for k in rows_all:
    out = []
    for n in names:
        v = first[n].get(k, [])
        out.append(f'{st.median(v):.0f}〔{len(v)}〕' if v else '—〔0〕')
    print(k + '\t' + '\t'.join(out))
print('## 補給崩壊（populationK≥100・supplyFillShare<0.3 の国×四半期・種ごと）[記録]')
for n in names:
    p = sup[n]; print(f'  {n}：合計 {sum(p)}・千件以上の種 {sum(1 for x in p if x >= 1000)}/10・種ごと {p}')
