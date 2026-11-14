# (1137) c111rb 同じ盤面の新÷旧（★(1129)・教訓③）：腕 B の生の trrail_s で、エンジンが使った need の取り分（新＝needUsedShareEnd）と
#   同じ年・同じ州の潜在量の取り分（旧＝needEnd÷railTargetIndex）、その φ（同じ T）の比の 10／50／90 点。あわせて国×年代の敷いた容量（boughtCapPerYear の和）の腕÷基線の中央。
# 呼び方：python -X utf8 -I newold_rb.py <腕 B の ins> <基線の ins> <出力の txt>
import sys, os, glob
from collections import defaultdict

arm, base, outp = sys.argv[1], sys.argv[2], sys.argv[3]

def q(xs, p):
    if not xs: return float('nan')
    xs = sorted(xs); return xs[min(len(xs) - 1, int(p * len(xs)))]

def phi_of(t, need):
    if not (need > 0): return 1.0 if t > 0 else 0.0
    f = t / need
    return 1.0 if f >= 1.0 else (f if f > 0 else 0.0)

def fl(s):
    try: return float(s)
    except ValueError: return float('nan')

def targets(d, sd):
    tg = {}
    with open(os.path.join(d, f'trnation_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: tg[(int(c[ix['year']]), c[ix['nation']])] = float(c[ix['railTargetIndex']])
            except (ValueError, IndexError, KeyError): pass
    return tg

def bought(d, sd):
    b = defaultdict(float)
    with open(os.path.join(d, f'trrail_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t'); y = int(c[ix['year']])
            v = fl(c[ix['boughtCapPerYear']])
            if v == v: b[(c[ix['nation']], y // 10 * 10)] += v
    return b

rs, rphi = [], []; nat = []
for path in sorted(glob.glob(os.path.join(arm, 'trrail_s*.tsv'))):
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    tg = targets(arm, sd)
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            need = fl(c[ix['needEnd']]); u = fl(c[ix['needUsedShareEnd']])
            if not (need > 0) or c[ix['waterFlag']] == '1' or not (u == u): continue
            t = tg.get((int(c[ix['year']]), c[ix['nation']]))
            if not (t and t > 0): continue
            sp = need / t; T = fl(c[ix['railTrackEnd']])
            if sp > 0: rs.append(u / sp)
            po = phi_of(T, need)
            if po > 0: rphi.append(phi_of(T, u * t) / po)
    if os.path.exists(os.path.join(base, f'trrail_s{sd}.tsv')):
        ba, bb = bought(arm, sd), bought(base, sd)
        for k, v in ba.items():
            if bb.get(k, 0) > 0 and v > 0: nat.append(v / bb[k])
out = [f'== 同じ盤面の新÷旧 {arm}',
       f'need の取り分 新（使った）÷旧（潜在量）10／50／90 点 {q(rs, .1):.3f}／{q(rs, .5):.3f}／{q(rs, .9):.3f}（行 {len(rs)}）',
       f'φ 新÷旧（同じ T）10／50／90 点 {q(rphi, .1):.3f}／{q(rphi, .5):.3f}／{q(rphi, .9):.3f}（行 {len(rphi)}）',
       f'国×年代の敷いた容量 腕÷基線（別の軌道）10／50／90 点 {q(nat, .1):.3f}／{q(nat, .5):.3f}／{q(nat, .9):.3f}（組 {len(nat)}）']
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
