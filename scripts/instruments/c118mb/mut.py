# (1187) c118mb 突然変異の当て方（使い捨ての木に 1 本ずつ・バイトで読み書き）。python -I mut.py <木> <名>
import sys, os
T, N = sys.argv[1], sys.argv[2]
SIM = 'PriceOfBread.SimCore/Simulation.cs'; P10 = 'PriceOfBread.SimCore/Coefficients.p10_ai_silver_overrides.cs'
INS = 'PriceOfBread.SimCore/Instruments/InstrumentsMintcap.cs'
MAM = 'PriceOfBread.Tests/MintAtMarketTests.cs'; MRG = 'PriceOfBread.Tests/MintregInstrumentTests.cs'
CONT = 'if (capOnly && !ReferenceEquals(r, World.Region)) continue;'
M = {
    'M1': [(P10, 'public double MintBuysAtCapital = 1.0;', 'public double MintBuysAtCapital = 0.0;')],
    'M2': [(SIM, CONT, 'if (false && capOnly && !ReferenceEquals(r, World.Region)) continue;')],
    'M3': [(SIM, 'if (m.Price > parity && !atMarket) continue;', 'if (!ReferenceEquals(r, World.Region) && m.Price > parity && !atMarket) continue;')],
    'M4': [(SIM, 'm.LastDemand += qty;   // qty', '_ = qty;   // qty')],
    'M5': [(SIM, 'bool capOnly = World.Coef.MintBuysAtCapital > 0;', 'bool capOnly = World.Coef.MintBuysAtCapital > 0; Region? mxR = null; foreach (var mxq in World.Regions) if (mxq.GoodOf(metal) is not null && (mxR is null || mxq.Markets[metal].Stock > mxR.Markets[metal].Stock)) mxR = mxq;'),
           (SIM, CONT, 'if (capOnly && !ReferenceEquals(r, mxR)) continue;')],
    'M6': [(SIM, 'double price = atMarket ? m.Price : parity;', 'double price = m.Price;')],
    'M7': [(INS, 'a.CapAboveNotAt = aboveNotAt;', 'a.CapAboveNotAt = false;')],
    'M8': [(INS, 'if (p.GoodId == ag && !p.Bankrupt)', 'if (p.GoodId == ag)')],
    'M9a': [(MAM, '        w.Coef.MintBuysAtCapital = 0.0;               // 旗 0 の経路の番人', '        // (M9a) w.Coef.MintBuysAtCapital = 0.0;               // 旗 0 の経路の番人')],
    'M9b': [(MRG, '        w.Coef.MintBuysAtCapital = 0.0;   // 旗 0 の経路の番人', '        // (M9b) w.Coef.MintBuysAtCapital = 0.0;   // 旗 0 の経路の番人')],
}
for f, a, b in M[N]:
    p = os.path.join(T, f)
    s = open(p, 'rb').read(); A = a.encode('utf-8'); B = b.encode('utf-8')
    if s.count(A) != 1:
        print(f'ERR {N} {f} 当たり {s.count(A)}'); sys.exit(1)
    open(p, 'wb').write(s.replace(A, B))
print(f'OK {N}')
