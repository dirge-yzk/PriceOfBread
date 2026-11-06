# (916) 読むだけ：州の産出（工場＋採取の参照価格の値）がちょうど 0 の続き（人口 100 千人以上・4 四半期以上）を数える。
#   使い方: python -X utf8 -I deadreg916.py <ins フォルダ> [種の起点 1|11]
#   出力: 種ごとの区間の数・最長・州名と持ち主の国と格（最長の上位 5）。region_s の列を名前で引く（列の位置に依らない）。
import sys, os
M = sys.argv[1]; SB = int(sys.argv[2]) if len(sys.argv) > 2 else 1
POP = 100.0; K = 4
tot_runs = 0; tot_q = 0; allruns = []
for s in range(SB, SB + 10):
    p = os.path.join(M, f'region_s{s}.tsv')
    if not os.path.exists(p):
        print('欠け', p); continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
    iq, ina, ire, iti, ipo = h.index('quarter'), h.index('nation'), h.index('region'), h.index('tier'), h.index('populationK')
    ifa, iex, iea = h.index('factoryOutputRefValuePerMonth'), h.index('extractionOutputRefValuePerMonth'), h.index('earnedPerMonth')
    cur = {}  # region -> [start q, len, owner set, tier, pop]
    runs = []
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) <= max(ifa, iex): continue
        try:
            q = int(r[iq]); pop = float(r[ipo]); fa = float(r[ifa]); ex = float(r[iex])
        except ValueError:
            continue
        key = r[ire]
        zero = (fa + ex == 0.0) and pop >= POP
        if zero:
            c = cur.get(key)
            if c is None or c[4] != q - 1:
                if c is not None and c[1] >= K: runs.append(c)
                c = [q, 0, set(), r[iti], q, pop]
                cur[key] = c
            c[1] += 1; c[2].add(r[ina]); c[4] = q; c[5] = pop
        else:
            c = cur.pop(key, None)
            if c is not None and c[1] >= K: runs.append(c)
    for c in cur.values():
        if c[1] >= K: runs.append(c)
    n = len(runs); qn = sum(c[1] for c in runs)
    tot_runs += n; tot_q += qn
    for c in runs: allruns.append((c[1], s, c[0], c[3], ','.join(sorted(c[2])), c[5]))
    print(f'種 {s}\t区間 {n}\t州×四半期 {qn}\t最長 {max([c[1] for c in runs], default=0)}')
print(f'計\t区間 {tot_runs}\t州×四半期 {tot_q}')
tiers = {}
for a in allruns: tiers[a[3]] = tiers.get(a[3], 0) + a[0]
print('格ごとの州×四半期', dict(sorted(tiers.items())))
allruns.sort(reverse=True)
for a in allruns[:8]:
    print('  長さ', a[0], '種', a[1], '始め q', a[2], '格', a[3], '持ち主', a[4], '人口(終)', round(a[5], 1))
