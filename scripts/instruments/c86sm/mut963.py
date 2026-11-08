# (963) 突然変異を使い捨ての木に当てる：python mut963.py <木> <名前>   名前＝M1,M2,M3,M5,M6,M7（M4 は ident_check の写し＝mut963.sh が別に回す）
import sys
tree, name = sys.argv[1], sys.argv[2]
M = {
    'M1': ('PriceOfBread.SimCore/BilateralMarket.cs',
           [('            foreach (var p2 in r2.Pops)\n                if (p2.Debt.Value > 0) { double back', '            if (ins2) Instruments.CearnImportHead(r2);\n            foreach (var p2 in r2.Pops)\n                if (p2.Debt.Value > 0) { double back'),
            ('if (ins2) Instruments.CearnImportHead(r2);   //', '   //')]),
    'M2': ('PriceOfBread.SimCore/InstrumentsCearn.cs', [('&& heirDebt > 1e-9)', '&& heirDebt > 0)')]),
    'M3': ('PriceOfBread.SimCore/InstrumentsCearn.cs', [('        if (!CearnOn) return;\n        CearnImportHeadInto(CearnAccs.GetValue', '        CearnImportHeadInto(CearnAccs.GetValue')]),
    'M5': ('PriceOfBread.SimCore/InstrumentsU4.cs', [('bool colonial = s == 1;', 'bool colonial = s == 0;')]),
    'M6': ('PriceOfBread.SimCore/InstrumentsU4.cs', [('                paid += a.Paid; built += a.Built; oP += a.OscPaid; oD1 += a.OscAbsD1; oD2 += a.OscAbsD2; oLag += a.OscLag; oSq += a.OscSq; oSqLag += a.OscSqLag;\n                a.ClearQuarter();', '                paid += a.Paid; built += a.Built; oP += a.OscPaid; oD1 += a.OscAbsD1; oD2 += a.OscAbsD2; oLag += a.OscLag; oSq += a.OscSq; oSqLag += a.OscSqLag;')]),
    'M7': ('PriceOfBread.SimCore/InstrumentsU4.cs', [('.Add((r.Tier, snap));', '.Add((r.Tier, snap)); U4Departed.GetValue(from, _ => new List<(RegionTier Tier, U4Acc Acc)>()).Add((r.Tier, snap));')]),
}
f, pairs = M[name]
p = tree + '/' + f
b = open(p, 'rb').read().decode('utf-8')
for old, new in pairs:
    assert b.count(old) == 1, (name, old[:60], b.count(old))
    b = b.replace(old, new)
open(p, 'wb').write(b.encode('utf-8'))
print('applied', name)
