# (1024) c96kf 突然変異の適用（バイトで読み書き）。使い方：python mut.py <木> <M1|M2|M3|M4|M5>
import sys
tree, m = sys.argv[1], sys.argv[2]
MUT = {
 'M1': ('PriceOfBread.SimCore/BilateralMarket.cs', 'if (del2 >= lp) nInv++;', 'if (del2 < lp) nInv++;'),
 'M2': ('PriceOfBread.SimCore/MarketFlow.cs', 'kGoodsD++; kRunG += run;', 'kRunG += run;'),
 'M3': ('PriceOfBread.SimCore/Meritocracy.cs', 'if (ins && pi == 0) Instruments.MerchRoute(r, 2, szC0 - t.Size);', ''),
 'M4': ('PriceOfBread.SimCore/InstrumentsPxstep.cs', 'var a = PxAccs.GetValue(r, _ => new PxAcc());', 'var a = PxShared;'),
 'M5': ('PriceOfBread.SimCore/InstrumentsScope.cs', 'var key = (name, role, cls);', 'var key = (name + "|" + ((int)(w.Regions.Count > 0 ? w.Regions[0].Tier : 0)), role, cls);'),
}
f, a, b = MUT[m]
p = tree + '/' + f
s = open(p, 'rb').read().decode('utf-8')
assert s.count(a) == 1, (m, s.count(a))
s = s.replace(a, b)
if m == 'M4':
    s = s.replace('sealed class PxAcc {', 'static readonly PxAcc PxShared = new();\n    sealed class PxAcc {', 1)
open(p, 'wb').write(s.encode('utf-8'))
print('applied', m)
