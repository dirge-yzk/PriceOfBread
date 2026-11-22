import sys
p, pert = sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else ''
b = open(p, 'rb').read()
if pert:
    o = b'w.Coef.AusterityStartDebt = new RatioToGdpYear(99);'
    assert b.count(o) == 1
    b = b.replace(o, b'w.Coef.SupplySlope = ' + pert.encode() + b'; ' + o)
o = b'Assert.True(dMil > 0, $"'
assert b.count(o) == 1
b = b.replace(o, b'Assert.True(false, $"DIAG dSat={dSat:F4} dSav={dSav:F4} dMil={dMil:F4} ')
o3 = b'var heavy = Run(0.6, warmup);'
assert b.count(o3) == 1
b = b.replace(o3, o3 + b' if (step == 2) for (int c = 0; c < 64; c++) if (light.SizeByClass[c] > 0 && heavy.SizeByClass[c] > 0) diagSteps += $" c{c}:{light.SizeByClass[c] / 20:F0}/{heavy.SizeByClass[c] / 20:F0}:{light.MilByClass[c] / light.SizeByClass[c]:F3}>{heavy.MilByClass[c] / heavy.SizeByClass[c]:F3}";')
o4 = b'    private static void TaxChainCheck()'
assert b.count(o4) == 1
b = b.replace(o4, b'    static string K49 = "";' + b' ' + o4)
o5 = b'            return (w.PollTax, satAvg / k, milAvg / k, paidAvg / k, savAvg / k, milC, sizeC);'
assert b.count(o5) == 1
b = b.replace(o5, b'            K49 += $" w{warmup}t{pollTax}:pol={w.Polity} rev={w.FactionRevolts} split={w.CivilWarSplitCount} cw={w.InCivilWar} suf={w.Suffrage}";' + b' ' + o5)
b = b.replace(b'Assert.True(false, $"DIAG ', b'Assert.True(false, $"DIAG {K49} ')
open(p, 'wb').write(b)
print('patched', pert)
