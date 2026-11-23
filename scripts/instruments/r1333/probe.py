# 列の同定（読むだけ）：σraw＝ρ×U×T÷(need×N) の N がどの列か
import sys, itertools
p = sys.argv[1]
with open(p, 'r', encoding='utf-8', newline='') as f:
    h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    n = 0
    for line in itertools.islice(f, 200000, 200400):
        c = line.rstrip('\r\n').split('\t')
        if c[ix['s2BasisKind']] != '2': continue
        g = lambda k: float(c[ix[k]])
        try:
            rho, u, t, need, sr = g('s2RhoInst'), g('s2UEnd'), g('s2TechEnd'), g('stapleNeedPerCapita'), g('s3SrawInst')
            Nimp = rho * u * t / (need * sr) if sr > 0 else float('nan')
            print(c[ix['year']], c[ix['nation']], round(Nimp, 3), c[ix['s2HeadsEnd']], c[ix['subPopK']], c[ix['s2D0Inst']], c[ix['s2RhoSource']], round(sr, 4), c[ix['s3HarvestInst']], c[ix['s3KappaEInst']])
        except Exception as e: pass
        n += 1
        if n > 12: break
