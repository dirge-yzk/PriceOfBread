# (980) c88hr 問い 3 の材料：F の区間（gate971.py と同じ定義）の底の四半期で、州の貯蓄の和を減らした流れを並べる（読むだけ・旗を読まない）
#   python -X utf8 -I q3_bottom.py --ins <ins> --seeds <a> <b> --out <出力>
#   区間ごと：底の四半期（州の貯蓄の和が最小・＞0）の outImport・inExport・純・outCapital・人の居ない払い手の払い（Le1e12）を、底の貯蓄と区間の頭の四半期の savingsStart で割る。
#   分母 2 本＝底の貯蓄（gate971 の U5 の分母）と区間の頭の savingsStart（分母の痩せた 1 四半期に頼らない案）。区間の和÷区間の頭の貯蓄も出す。
import sys, os
a = sys.argv[1:]
def opt(k, n=1):
    i = a.index(k); v = a[i + 1:i + 1 + n]; del a[i:i + 1 + n]; return v
ins = opt('--ins')[0]; s0, s1 = map(int, opt('--seeds', 2)); out = opt('--out')[0]
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
COLS = ['savingsStart', 'savings', 'outImportPerQuarter', 'inExportPerQuarter', 'outCapitalPerQuarter', 'importPaidPayerSizeLe1e12PerQuarter', 'importPaidPerQuarter']
L = [f'# (980) q3_bottom.py・ins {ins}・種 {s0}〜{s1}', '種\t国\t区間\t底Q\t底の貯蓄\t頭のsavingsStart\t底 imp÷底貯蓄\t底 純÷底貯蓄\t底 outCapital÷底貯蓄\t底 E÷底貯蓄\t底 imp÷頭\t区間Σimp÷頭\t区間Σ純÷頭\t区間ΣoutCapital÷頭\t区間ΣE÷頭']
mx = {}
for s in range(s0, s1 + 1):
    pc = os.path.join(ins, f'cearn_s{s}.tsv'); pn = os.path.join(ins, f'nation_s{s}.tsv')
    if not os.path.exists(pc): continue
    byn = {}
    with open(pn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if fl(x[ix['priceLevel']]) <= FLOOR: byn.setdefault(x[ix['nation']], []).append(int(x[ix['quarter']]))
    ivs = [(n, st, en) for n, qs in byn.items() for st, en in runs(sorted(qs), K)]
    want = {}
    for n, st, en in ivs:
        for q in range(st, en + 1): want[(n, q)] = None
    agg = {}
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        ci = [ix[c] for c in COLS]
        for line in f:
            x = line.rstrip('\n').split('\t')
            k = (x[ix['nation']], int(x[ix['quarter']]))
            if k not in want: continue
            v = agg.setdefault(k, [0.0] * len(COLS))
            for j, i in enumerate(ci): v[j] += z(fl(x[i]))
    for n, st, en in ivs:
        qs = [q for q in range(st, en + 1) if (n, q) in agg]
        if not qs: continue
        bq = min((q for q in qs if agg[(n, q)][1] > 0), key=lambda q: agg[(n, q)][1], default=None)
        if bq is None: continue
        b = agg[(n, bq)]; hd = agg[(n, qs[0])][0]
        S = [sum(agg[(n, q)][j] for q in qs) for j in range(len(COLS))]
        r = lambda u, d: u / d if d > 0 else float('nan')
        row = [r(b[2], b[1]), r(b[2] - b[3], b[1]), r(b[4], b[1]), r(b[5], b[1]), r(b[2], hd), r(S[2], hd), r(S[2] - S[3], hd), r(S[4], hd), r(S[5], hd)]
        L.append(f'{s}\t{n}\t{st}〜{en}\t{bq}\t{b[1]:.6g}\t{hd:.6g}\t' + '\t'.join(f'{v:.4g}' for v in row))
        for j, v in enumerate(row):
            if v == v and (j not in mx or v > mx[j][0]): mx[j] = (v, s, n, st, en)
names = ['底 imp÷底貯蓄（U5 の形）', '底 純÷底貯蓄', '底 outCapital÷底貯蓄', '底 E÷底貯蓄', '底 imp÷頭', '区間Σimp÷頭', '区間Σ純÷頭', '区間ΣoutCapital÷頭', '区間ΣE÷頭']
L.append('[最大] ' + '・'.join(f'{names[j]} {mx[j][0]:.4g}（種 {mx[j][1]} {mx[j][2]} {mx[j][3]}〜{mx[j][4]}）' for j in sorted(mx)))
L.append(f'[区間の数] {len([l for l in L if l and l[0].isdigit()])}')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print(f'書いた {out}')
