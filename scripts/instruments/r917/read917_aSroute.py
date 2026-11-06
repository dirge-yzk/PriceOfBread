# (917) c79db：a∩S の行の州×四半期で、cearn_s の州の流出の上位の経路（outTop1Route・outTop2Route）を数える（読むだけ・州の和＝商人以外の POP も混ざる＝近似）。
#   python -X utf8 -I read917_aSroute.py <ins フォルダ> <種>
import sys, os
from collections import Counter
d, s = sys.argv[1], int(sys.argv[2]); DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def tol(v): return 1e-9 * max(1.0, abs(v))
keys = {}; prev = {}
with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        if '\t商人\t' not in line: continue
        x = line.rstrip('\n').split('\t')
        if x[ix['className']] != '商人': continue
        qq = int(x[ix['quarter']]); key = (x[ix['region']], x[ix['popIndex']]); D1 = fl(x[ix['debt']]); C = fl(x[ix['inCreditPerQuarter']])
        pr = prev.get(key); has = pr is not None and pr[0] == qq - 1; empty = not (fl(x[ix['popK']]) > DEAD)
        prev[key] = (qq, D1, empty)
        if not empty or not has or not pr[2] or C > tol(C) or not (D1 - C > 1e-9 * max(1.0, D1)): continue
        R = pr[1] + C - D1; mk = fl(x[ix['inMarketPerQuarter']])
        if mk >= D1 and abs(R) <= tol(pr[1]): keys[(x[ix['region']], x[ix['quarter']])] = fl(x[ix['outOtherPerQuarter']])
c1 = Counter(); c2 = Counter(); share = []
with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        x = line.rstrip('\n').split('\t'); k = (x[ix['region']], x[ix['quarter']])
        if k not in keys: continue
        c1[x[ix['outTop1Route']]] += 1; c2[x[ix['outTop2Route']]] += 1
        t1 = fl(x[ix['outTop1PerQuarter']]); share.append(keys[k] / t1 if t1 > 0 else float('nan'))
share = sorted(v for v in share if v == v)
print(f'# {d} 種 {s}・a∩S の州×四半期 {len(keys)}')
print('outTop1Route 上位：' + '・'.join(f'{k} {v}' for k, v in c1.most_common(6)))
print('outTop2Route 上位：' + '・'.join(f'{k} {v}' for k, v in c2.most_common(6)))
if share: print(f'商人の outOther ÷ 州の outTop1 の中央 {share[len(share) // 2]:.4g}')
