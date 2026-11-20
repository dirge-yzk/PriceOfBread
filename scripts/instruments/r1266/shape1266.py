# (1266) c129dh 売る割合の形（世界の和・10 年ごと・本国格と植民地格・keep_1260/base_ins 種 1〜10・読むだけ）
# 使い方: python -X utf8 -I shape1266.py <base_ins> <出力 txt>
import sys, os, math
from collections import defaultdict
base, out = sys.argv[1], sys.argv[2]
def fnum(x):
    try:
        v = float(x); return v if math.isfinite(v) else None
    except ValueError:
        return None
K = ['cap', 'sold', 'U', 'minUL', 'heads', 'Tcap', 'subPop', 'subSig', 'rows', 'rawGt1', 'need']
acc = defaultdict(lambda: dict.fromkeys(K, 0.0))
for seed in range(1, 11):
    with open(os.path.join(base, f'subst_s{seed}.tsv'), 'r', encoding='utf-8', newline='') as f:
        hdr = f.readline().rstrip('\r\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            y = int(c[ix['year']]); d = (y // 10) * 10
            for key in ((d, 'all'), (d, c[ix['tier']])):
                a = acc[key]
                sp, sr = fnum(c[ix['subPopK']]), fnum(c[ix['subRateWeighted']])
                if sp and sr is not None: a['subPop'] += sp; a['subSig'] += sp * sr
                if c[ix['s2BasisKind']] != '2': continue
                v = [fnum(c[ix[k]]) for k in ('s2CapEnd', 's2SoldEnd', 's2UEnd', 's2LneedEnd', 's2HeadsEnd', 's2TechEnd')]
                if None in v or not v[0] > 0: continue
                cap, sold, u, ln, h, t = v
                a['cap'] += cap; a['sold'] += sold; a['U'] += u; a['minUL'] += min(u, ln) if ln > 0 else u
                a['heads'] += h; a['Tcap'] += t * cap; a['rows'] += 1
                if u > ln: a['rawGt1'] += 1
L = ['# 10 年ごと（四半期×州の和・種 1〜10）: 窓 tier sig soc kH cap/heads T(cap重み) 余りの行の割合 行']
for (d, tier) in sorted(acc):
    a = acc[(d, tier)]
    if not a['cap'] > 0: continue
    L.append(f"{d}\t{tier}\tsig={a['subSig']/a['subPop'] if a['subPop'] else float('nan'):.4f}\tsoc={a['sold']/a['cap']:.4f}\tkH={1-a['minUL']/a['cap']:.4f}"
             f"\tcap/heads={a['cap']/a['heads'] if a['heads'] else float('nan'):.4f}\tT={a['Tcap']/a['cap']:.4f}\tsur_rows={a['rawGt1']/a['rows']:.5f}\trows={int(a['rows'])}")
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print('\n'.join(L))
