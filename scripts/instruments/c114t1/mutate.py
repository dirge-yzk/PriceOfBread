# (1159) c114t1 突然変異：使い捨ての木で 1 箇所をバイトで置き換える（旧がちょうど 1 回のときだけ）。使い方: python -I mutate.py <木> <名>
import sys, os
T, M = sys.argv[1], sys.argv[2]
MUT = {
 'M2': ('PriceOfBread.SimCore/BilateralMarket.cs', 'a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;', 'a1Q = m.LastProduced;'),
 'M3': ('PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs', 'st.CurUMin = um;   // (1159)', 'st.CurUMin = lp;   // (1159)'),
 'M4': ('PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs', '=> headPrice * lbMulMin < lp;', '=> headPrice * lbMulMin >= lp;'),
 'M5': ('PriceOfBread.SimCore/EconomyStep.cs', 'if (sleep[pi] == 1) { if (t1Copy) { var sz = region.Producers[pi]; sz.T1OutPrev = 0; sz.T1Base = 0; sz.T1Mc = 0; } continue; }', 'if (sleep[pi] == 1) { continue; }'),
 'M5b': ('PriceOfBread.SimCore/EconomyStep.cs', 'if (t1Copy) { s.T1OutPrev = 0; s.T1Base = 0; s.T1Mc = 0; } continue; }', 'continue; }'),
 'MF': ('PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs', 'TbPHatSum(r, g, c, w, um / fx)); ubA1', 'TbPHatSum(r, g, c, w, um)); ubA1'),
}
f, a, b = MUT[M]
p = os.path.join(T, f)
d = open(p, 'rb').read(); A = a.encode(); B = b.encode()
n = d.count(A)
if n != 1:
    print(M, '置き換えの旧が', n, '回'); sys.exit(1)
open(p, 'wb').write(d.replace(A, B)); print(M, '置き換えた', f)
