# (917) c79db：diag910 の a 型（G1′ の行で inMarket ≧ 末の債務 D1）を read917_carry.py の分け（F・E・N・S・P・P!・U）で数える。読むだけ。
#   python -X utf8 -I read917_atype.py <名> <ins フォルダ>
import sys, os
from collections import defaultdict
tag, d = sys.argv[1], sys.argv[2]; DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def tol(v): return 1e-9 * max(1.0, abs(v))
cnt = defaultdict(lambda: [0, 0])
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
            if not empty or not (D1 - C > 1e-9 * max(1.0, D1)): continue
            O = fl(x[ix['outOtherPerQuarter']]); mk = fl(x[ix['inMarketPerQuarter']])
            if not has: k = 'F'
            elif not pr[2]: k = 'E'
            elif C > tol(C): k = 'N'
            else:
                D0 = pr[1]; R = D0 + C - D1
                k = ('P!' if R > O + tol(R) else 'P') if R > tol(D0) else 'U' if R < -tol(D0) else 'S'
            cnt[k][0 if mk >= D1 else 1] += 1
print(f'# 名 {tag}・{d}・a＝inMarket ≧ D1・b＝inMarket ＜ D1')
for k in ('F', 'E', 'N', 'S', 'P', 'P!', 'U'): print(f'{k}\ta {cnt[k][0]}\tb {cnt[k][1]}')
print(f'計\ta {sum(v[0] for v in cnt.values())}\tb {sum(v[1] for v in cnt.values())}')
