# (1117) c108rc 突然変異（事前登録 §3）：写しの木 <木> に 1 本の突然変異を当てる。使い捨ての木だけに使う（本物の worktree に当てない）。
# 使い方: python -X utf8 -I mutate.py <木> <M1|M2|...|M10>
import sys, os
tree, m = sys.argv[1], sys.argv[2]
E = 'PriceOfBread.SimCore/RulePowerEngine.cs'
P = 'PriceOfBread.SimCore/Politics.cs'
MUT = {
    'M1': [(E, 'double wgt = on.Y[ok] * (1 - Math.Min(1.0, held)) * (1 - b);', 'double wgt = on.Y[ok] * rho * (1 - Math.Min(1.0, held)) * (1 - b);')],
    'M2': [(E, 'internal const double RulePowerTieTolerance = 1e-9;', 'internal const double RulePowerTieTolerance = 0.0;')],
    'M3': [(E, 'if (!e.seaOk && e.r.LastPortFrozen) e.seaOk = true;', 'if (false) e.seaOk = true;')],
    'M4': [(E, 'lst.Add((n.Key, alloc * rho));', 'lst.Add((n.Key, alloc * rho + (rg.IsPort ? Math.Max(0, n.P.NavalPower) * wgt / sumW : 0)));')],
    'M5': [(E, 'double fI = double.IsNaN(r.RuleTau)\n', 'double fI = true\n')],
    'M5p': [(E, 'n.I[k] = fI + fR > 0 ? fI / (fI + fR) : 1.0;', 'n.I[k] = r.RpEng is { } pe && !double.IsNaN(pe.I) ? pe.I : (fI + fR > 0 ? fI / (fI + fR) : 1.0);')],
    'M6': [(E, 'double q = sumL / sumPop;', 'if (!(dip.RpMutQ0 > 0)) dip.RpMutQ0 = sumL / sumPop; double q = dip.RpMutQ0;'),
           ('PriceOfBread.SimCore/Diplomacy.cs', '    [System.NonSerialized] internal long RpEngStamp;', '    [System.NonSerialized] internal long RpEngStamp; [System.NonSerialized] internal double RpMutQ0;')],
    'M7': [(E, 'double fU = n.N[k] * Math.Max(0, r.RuleTau) * Math.Max(0, ri3);', 'double fU = n.N[k] * Math.Max(0, r.RuleTau) * Math.Max(0, ri3) * (1.0 - Politics.RegionReach(n.W, c, r));')],
    'M8': [(E, 'if (!e.seaOk && e.r.LastPortFrozen) e.seaOk = true;', 'if (!e.seaOk && e.r.IsPort) e.seaOk = true;')],
    'M9': [(E, 'lst.Add((n.Key, alloc * rho));', 'lst.Add((n.Key, alloc));'),
           (E, 'da[(n.Key, rg)] = (wgt, alloc, alloc > 0 ? alloc * rho : 0);', 'da[(n.Key, rg)] = (wgt, alloc, alloc > 0 ? alloc : 0);')],
    'M10': [(P, 'if (c.RuleFromPower > 0) RulePowerApply(w, c);', 'if (c.RuleFromPower > 0) { }')],
}
for f, old, new in MUT[m]:
    p = os.path.join(tree, f); b = open(p, 'rb').read().decode('utf-8')
    crlf = '\r\n' in b
    o, n = (old.replace('\n', '\r\n'), new.replace('\n', '\r\n')) if crlf else (old, new)
    if b.count(o) != 1: print(f'MUTFAIL {m} {f} count={b.count(o)}'); sys.exit(1)
    open(p, 'wb').write(b.replace(o, n).encode('utf-8'))
print(f'MUT {m} ok')
