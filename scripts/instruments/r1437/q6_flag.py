# (1437) 問い5 (c)：旗 RailProcureNational 1（基準 M）と 0（腕 Z）で、届いた量と線路（読むだけ・同じ木 04a162e34・種 1〜10）
# 州の区分：内陸＝M の trstat の portFlag が 10 種で 0。型 K・型 C＝M の q1c_rows.tsv の州の名（種を問わず・持ち主は軌道で変わる）。
# 量：1870〜1950 の和＝want・州の在庫からの got・国の中の got（natGot）・届いた量（got＋natGot）・1900 年と 1950 年の railTrackEnd の和。
# 出力：標準出力（<out>/q6_flag.txt へ）
import sys, csv, collections
M, Z, out = sys.argv[1], sys.argv[2], sys.argv[3]
port = {}
for s in range(1, 11):
    with open(f"{M}/trstat_s{s}.tsv", encoding='utf-8') as f:
        for ln in f:
            c = ln.split('\t')
            if c[0] == 'G': port[c[3]] = max(port.get(c[3], 0), int(c[11] or 0))
typ = {}
with open(f"{out}/q1c_rows.tsv", encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'): typ[r['region']] = 'K' if r['tier'] in ('Core', 'Integrated') else 'C'
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception: return 0.0
def read(raw):
    A = collections.defaultdict(lambda: collections.defaultdict(float))
    for s in range(1, 11):
        with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for row in rd:
                y = int(row[ix['year']]); rg = row[ix['region']]
                cls = ['全て', '内陸' if port.get(rg, 1) == 0 else '港']
                if rg in typ: cls.append('型' + typ[rg])
                for c in cls:
                    a = A[(s, c)]
                    if 1870 <= y <= 1950:
                        a['want'] += fl(row[ix['procureWantUnitsPerYear']]); a['got'] += fl(row[ix['procureGotUnitsPerYear']])
                        a['natGot'] += sum(fl(row[ix['natGotPhi' + b]]) for b in ('Low', 'Mid', 'High'))
                        a['sz'] += fl(row[ix['procureStockZeroGoodMonths']]); a['sb'] += fl(row[ix['procureSavingsBindGoodMonths']]); a['gm'] += fl(row[ix['monthsCount']]) * 2
                    if y in (1900, 1950): a['track' + str(y)] += fl(row[ix['railTrackEnd']]); a['need' + str(y)] += fl(row[ix['needEnd']])
    return A
AM, AZ = read(M), read(Z)
print("区分\t量\t旗1(M) 10種の和\t旗0(Z) 10種の和\tM÷Z\t種ごとの M÷Z の最小〜最大")
for c in ('全て', '港', '内陸', '型K', '型C'):
    for k in ('want', 'got', 'natGot', 'deliv', 'track1900', 'track1950', 'need1950', 'szShare', 'sbShare'):
        def v(A, s):
            a = A[(s, c)]
            if k == 'deliv': return a['got'] + a['natGot']
            if k == 'szShare': return a['sz'] / a['gm'] if a['gm'] else float('nan')
            if k == 'sbShare': return a['sb'] / a['gm'] if a['gm'] else float('nan')
            return a[k]
        if k in ('szShare', 'sbShare'):
            m = sum(AM[(s, c)]['sz' if k == 'szShare' else 'sb'] for s in range(1, 11)) / max(1e-9, sum(AM[(s, c)]['gm'] for s in range(1, 11)))
            z = sum(AZ[(s, c)]['sz' if k == 'szShare' else 'sb'] for s in range(1, 11)) / max(1e-9, sum(AZ[(s, c)]['gm'] for s in range(1, 11)))
            print(f"{c}\t{k}\t{m:.3f}\t{z:.3f}\t\t"); continue
        m = sum(v(AM, s) for s in range(1, 11)); z = sum(v(AZ, s) for s in range(1, 11))
        rs = [v(AM, s) / v(AZ, s) for s in range(1, 11) if v(AZ, s) > 0]
        print(f"{c}\t{k}\t{m:.4g}\t{z:.4g}\t{m / z if z > 0 else float('nan'):.3f}\t{(min(rs) if rs else float('nan')):.3f}〜{(max(rs) if rs else float('nan')):.3f}")
