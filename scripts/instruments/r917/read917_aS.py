# (917) c79db：a∩S（G1′・inMarket ≧ D1・四半期の返し R≈0）の行で、お金がどの列から出たかを数える（読むだけ）。比べに a∩P と b∩S も。
#   python -X utf8 -I read917_aS.py <名> <ins フォルダ>
import sys, os
from collections import defaultdict
tag, d = sys.argv[1], sys.argv[2]; DEAD = 1e-12
COLS = ['inMarketPerQuarter', 'outMarketPerQuarter', 'inExportPerQuarter', 'inStatePerQuarter', 'inFromPopsPerQuarter', 'inOtherPerQuarter', 'outTaxPerQuarter', 'outBondPerQuarter', 'outDuesPerQuarter', 'outColonialRentPerQuarter', 'outCapitalPerQuarter', 'outImportPerQuarter', 'outMigrationPerQuarter', 'outOtherPerQuarter', 'estateOutPerQuarter', 'estateInPerQuarter', 'residualPerQuarter', 'savingsStart', 'savings', 'monthsClosed', 'lateRegistered']
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def tol(v): return 1e-9 * max(1.0, abs(v))
acc = defaultdict(lambda: defaultdict(float)); n = defaultdict(int)
for s in range(1, 11):
    p = os.path.join(d, f'cpop_s{s}.tsv')
    if not os.path.exists(p): continue
    prev = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            if '\t商人\t' not in line: continue
            x = line.rstrip('\n').split('\t')
            if x[ix['className']] != '商人': continue
            qq = int(x[ix['quarter']]); key = (x[ix['region']], x[ix['popIndex']]); D1 = fl(x[ix['debt']]); C = fl(x[ix['inCreditPerQuarter']])
            pr = prev.get(key); has = pr is not None and pr[0] == qq - 1; empty = not (fl(x[ix['popK']]) > DEAD)
            prev[key] = (qq, D1, empty)
            if not empty or not has or not pr[2] or C > tol(C) or not (D1 - C > 1e-9 * max(1.0, D1)): continue
            D0 = pr[1]; R = D0 + C - D1; mk = fl(x[ix['inMarketPerQuarter']])
            k = ('a' if mk >= D1 else 'b') + ('S' if abs(R) <= tol(D0) else 'P' if R > 0 else 'U')
            n[k] += 1; a = acc[k]; a['D1'] += D1
            for c in COLS: a[c] += fl(x[ix[c]])
print(f'# 名 {tag}・{d}・G1′ の空の商人の行（前の四半期も空・C≦ε）の列の和')
for k in ('aS', 'aP', 'bS', 'bP'):
    a = acc[k]; print(f'{k}\t行 {n[k]}\tD1 {a["D1"]:.6g}\t' + '\t'.join(f'{c.replace("PerQuarter", "")} {a[c]:.6g}' for c in COLS))
