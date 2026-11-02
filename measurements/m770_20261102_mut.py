import sys
mode = sys.argv[1]; p = 'D:/PoB_work/c6pr/wt_mut/PriceOfBread.SimCore/Simulation.cs'
b = open(p,'rb').read().decode('utf-8')
def rep(old,new):
    global b
    assert b.count(old)==1, (mode, old[:50]); b=b.replace(old,new)
if mode=='finite':      # 波54 の形：有限な港だけ。なければ旧の規則へ
    rep("double e = double.IsFinite(l) ? l : d;","double e = double.IsFinite(l) ? l : double.MaxValue / 2;")
    rep("            return (pickName, bestGc, bestEff);\n        }","            if (bestEff < double.MaxValue / 2) return (pickName, bestGc, bestEff);\n        }")
elif mode=='max':
    rep("double bestEff = double.MaxValue, bestGc = 0;","double bestEff = -1, bestGc = 0;")
    rep("if (e < bestEff) { bestEff = e;","if (e > bestEff) { bestEff = e;")
elif mode=='flag':
    rep("new Coefficients().PortByLandRoute > 0;","false;")
open(p,'wb').write(b.encode('utf-8'))
