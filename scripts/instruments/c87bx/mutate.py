import subprocess, re, os, sys
R = 'D:/PoB_work/wt_c87bx'
BX = R + '/PriceOfBread.SimCore/InstrumentsBasisx.cs'
LB = R + '/PriceOfBread.SimCore/InstrumentsLandbase.cs'
M = [
 ('M1', BX, [("-tau * pi * (a.SRentPb - a.SRentFrozenPb)", "tau * pi * (a.SRentPb - a.SRentFrozenPb)"), ("(a.SRentFrozenPb * (1 + pi) - a.SFrozenPb)", "(a.SRentFrozenPb * (1 - pi) - a.SFrozenPb)")], 'T4,T5'),
 ('M2', BX, [("System.Math.Abs(dFix) / B)", "System.Math.Abs(dFix) / (B + dFix))"), ("System.Math.Abs(dProp) / B)", "System.Math.Abs(dProp) / (B + dProp))")], 'T4,T5'),
 ('M3', BX, [("(p.Closed && !p.IsSoldier ? 1.0 - deg : 1.0)", "1.0")], 'T4,T5'),
 ('M4', BX, [("        bool back = ruleB ? R > f : R * (1 + pi) > f;", "        f = R;\n        bool back = ruleB ? R > f : R * (1 + pi) > f;")], 'T2'),
 ('M5', BX, [("Step(2, false, crisis1);", "Step(2, false, true);")], 'T6'),
 ('M6', BX, [("dP += cP; gP += System.Math.Abs(cP);", "dP += cP; gP += cP;")], 'T5'),
 ('M7', BX, [("if (stn >= 0) a.Stance = stn;", "")], 'T7'),
 ('M8', BX, [("a.BasePb += pb * fv; a.FrozenPb += pb * fv;", "a.BasePb += pb * fv; a.FrozenPb += pb * rr;")], 'T5'),
 ('M9', BX, [("bool back = ruleB ? R > f : R * (1 + pi) > f;", "bool back = R * (1 + pi) > f;")], 'T2'),
 ('M10', LB, [("        BasisxRgoRent(w, r, s, rent);   // (970)", "        // BasisxRgoRent(w, r, s, rent);   // (970)")], 'T4,T5,T8'),
]
only = sys.argv[1:] 
summary = []
for name, path, reps, expect in M:
    if only and name not in only: continue
    src = open(path, 'rb').read().decode('utf-8')
    new = src
    for a, b in reps:
        assert a in new, (name, a)
        new = new.replace(a, b, 1)
    open(path, 'wb').write(new.encode('utf-8'))
    try:
        p = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c87bx_art', '--filter', 'FullyQualifiedName~BasisxInstrumentTests'], cwd=R, capture_output=True)
        out = p.stdout.decode('utf-8', 'replace')
    finally:
        open(path, 'wb').write(src.encode('utf-8'))
    open('D:/PoB_work/c87bx_work/mut/%s.log' % name, 'w', encoding='utf-8').write(out)
    red = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.BasisxInstrumentTests\.(T\d+)', out)))
    build_err = 'error CS' in out
    line = '%s\t期待の赤=%s\t実際の赤=%s\t%s%s' % (name, expect, ','.join(red) or '(なし＝緑のまま)', '建てられず' if build_err else '', '')
    summary.append(line); print(line, flush=True)
open('D:/PoB_work/c87bx_work/mut/summary.txt', 'a', encoding='utf-8').write('\n'.join(summary) + '\n')
