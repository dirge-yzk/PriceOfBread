# (980) c88hr 問い 3 の材料：型の行（E1・E2＝人の居ない払い手の払いがあり・B1 が移し・相続人が返した）で、相続人が受けた貯蓄（estateMovedPerQuarter）と受けた債務（estateDebtMovedPerQuarter）を並べる
#   ＝〈相続人の責任を相続した財産の限度に切る〉形（限定承認）が型を閉じるかの紙。読むだけ・旗を読まない。
#   python -X utf8 -I q1_estate.py --ins <ins> --seeds <a> <b> --out <出力> [--rows 種:州:四半期 ...]
import sys, os
a = sys.argv[1:]
def opt(k, n=1):
    i = a.index(k); v = a[i + 1:i + 1 + n]; del a[i:i + 1 + n]; return v
ins = opt('--ins')[0]; s0, s1 = map(int, opt('--seeds', 2)); out = opt('--out')[0]
rq = []
if '--rows' in a:
    i = a.index('--rows'); rq = a[i + 1:]; del a[i:]
rq = {(int(x.split(':')[0]), x.split(':')[1], int(x.split(':')[2])) for x in rq}
FLOOR = 0.4 + 1e-6; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def z(x): return x if x == x else 0.0
def runs(qs, k):
    o = []; st = pv = None
    for q in qs:
        if st is not None and q == pv + 1: pv = q; continue
        if st is not None and pv - st + 1 >= k: o.append((st, pv))
        st = pv = q
    if st is not None and pv - st + 1 >= k: o.append((st, pv))
    return o
SC = ['all', 'F']
T = {s: dict(rows=0, sav=0.0, debt=0.0, over=0.0, overRows=0, E=0.0, Eover=0.0) for s in SC}
L = [f'# (980) q1_estate.py・ins {ins}・種 {s0}〜{s1}']
for s in range(s0, s1 + 1):
    pc = os.path.join(ins, f'cearn_s{s}.tsv'); pn = os.path.join(ins, f'nation_s{s}.tsv')
    if not os.path.exists(pc): continue
    byn = {}
    with open(pn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if fl(x[ix['priceLevel']]) <= FLOOR: byn.setdefault(x[ix['nation']], []).append(int(x[ix['quarter']]))
    Fset = set()
    for n, qs in byn.items():
        for st, en in runs(sorted(qs), K):
            for q in range(st, en + 1): Fset.add((n, q))
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            E = z(fl(x[ix['importPaidPayerSizeLe1e12PerQuarter']])); mv = z(fl(x[ix['estateDebtMovedPerQuarter']])); hs = z(fl(x[ix['heirDebtSettledPerQuarter']]))
            n = x[ix['nation']]; q = int(x[ix['quarter']])
            if (s, x[ix['region']], q) in rq:
                L.append(f'[行] 種 {s} {n} {x[ix["region"]]} Q{q}：相続した貯蓄 estateMoved {fl(x[ix["estateMovedPerQuarter"]]):.6g}・相続した債務 {mv:.6g}・相続人の返し {hs:.6g}・人の居ない払い手の払い {E:.6g}・inMarket {fl(x[ix["inMarketPerQuarter"]]):.6g}・outMarket {fl(x[ix["outMarketPerQuarter"]]):.6g}・inExport {fl(x[ix["inExportPerQuarter"]]):.6g}・outCapital {fl(x[ix["outCapitalPerQuarter"]]):.6g}・savingsStart {fl(x[ix["savingsStart"]]):.6g}・savings {fl(x[ix["savings"]]):.6g}')
            if not (E > 0 and mv > 0 and hs > 0): continue
            sv = z(fl(x[ix['estateMovedPerQuarter']]))
            for sc in ['all'] + (['F'] if (n, q) in Fset else []):
                t = T[sc]; t['rows'] += 1; t['sav'] += sv; t['debt'] += mv; t['E'] += E
                if mv > sv: t['over'] += mv - sv; t['overRows'] += 1; t['Eover'] += E
for sc in SC:
    t = T[sc]
    L.append(f'[{sc}] 型の行 {t["rows"]}・相続した貯蓄 {t["sav"]:.6g}・相続した債務 {t["debt"]:.6g}（債務÷貯蓄 {t["debt"] / t["sav"] if t["sav"] > 0 else float("nan"):.4f}）・債務が貯蓄を超えた行 {t["overRows"]}・超えた分の和 {t["over"]:.6g}（÷債務 {t["over"] / t["debt"] if t["debt"] > 0 else float("nan"):.4f}）・その行の人の居ない払い手の払い {t["Eover"]:.6g}（÷型の払い {t["Eover"] / t["E"] if t["E"] > 0 else float("nan"):.4f}）')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print(f'書いた {out}')
