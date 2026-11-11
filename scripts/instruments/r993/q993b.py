# (993) c90bc 補い：billBlocked と E を tier×importPayerKind で分け、E の国×種の集中を出す（読むだけ・旗を読まない）
#   python -X utf8 -I q993b.py --ins <ins> --seeds <a> <b> --out <出力>
import sys, os
a = sys.argv[1:]
def opt(k, n=1):
    i = a.index(k); v = a[i + 1:i + 1 + n]; del a[i:i + 1 + n]; return v
ins = opt('--ins')[0]; s0, s1 = map(int, opt('--seeds', 2)); out = opt('--out')[0]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def z(x): return x if x == x else 0.0
G = {}; ser = {}; rows = {}
for s in range(s0, s1 + 1):
    pc = os.path.join(ins, f'cearn_s{s}.tsv')
    if not os.path.exists(pc): continue
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        it, ik, ibb, iE, ip, inat, imd = ix['tier'], ix['importPayerKind'], ix['billBlockedClearings'], ix['importPaidPayerSizeLe1e12PerQuarter'], ix['importPaidPerQuarter'], ix['nation'], ix['merchantDebt']
        for line in f:
            x = line.rstrip('\n').split('\t')
            k = (x[it], x[ik]); v = G.setdefault(k, [0, 0, 0.0, 0.0, 0])
            v[0] += 1; v[1] += int(x[ibb]); v[2] += z(fl(x[iE])); v[3] += z(fl(x[ip]))
            if z(fl(x[imd])) > 0: v[4] += 1
            e = z(fl(x[iE]))
            if e > 0: ser[(s, x[inat])] = ser.get((s, x[inat]), 0.0) + e
L = [f'# (993) q993b.py・ins {ins}・種 {s0}〜{s1}', 'tier\tkind\t行\tΣbillBlocked\tΣE\tΣpaid\tmerchantDebt＞0 の行']
tb = sum(v[1] for v in G.values())
for k in sorted(G): v = G[k]; L.append(f'{k[0]}\t{k[1]}\t{v[0]}\t{v[1]}\t{v[2]:.6g}\t{v[3]:.6g}\t{v[4]}')
L.append(f'[billBlocked の和] {tb}')
sE = sum(ser.values()); top = sorted(ser.items(), key=lambda kv: -kv[1])
if sE > 0: L.append('[E の集中] ' + '・'.join(f'上位{k} {sum(t[1] for t in top[:k]) / sE:.4f}' for k in (1, 2, 4)) + '｜' + '・'.join(f'種{t[0][0]} {t[0][1]} {t[1]:.6g}' for t in top[:6]) + f'｜系列 {len(ser)}')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print(f'書いた {out}')
