# r794: 使い捨ての木に読むだけの計器を足す（CapitalMarket.cs は行を増やさない＝CallerLineNumber の行番号を保つ）。引数：<木の根>
import sys, shutil, os
root = sys.argv[1]
here = os.path.dirname(os.path.abspath(__file__))
p = root + '/PriceOfBread.SimCore/CapitalMarket.cs'
s = open(p, encoding='utf-8').read()
n0 = s.count('\n')
def rep(a, b):
    global s
    assert s.count(a) == 1, (a[:70], s.count(a))
    s = s.replace(a, b)
rep('var ngdp = new double[n];', 'var ngdp = new double[n]; var r794 = Instruments.On ? new double[n, 24] : null; int r794max0 = 0;')
rep('            cred[i] = cr;', '            cred[i] = cr; if (r794 is not null) { r794[i, 0] = e[i]; r794[i, 1] = cr; r794[i, 2] = ngdp[i]; r794[i, 3] = InvestorSavings(w[i]); r794[i, 4] = w[i].ForeignCapitalIn.Value; r794[i, 5] = w[i].CapitalAbroad.Value; r794[i, 6] = Instruments.R794Shadow(w[i])[0]; }')
rep('double got = TakeGold(w[i], want);', 'double got = TakeGold(w[i], want); if (r794 is not null) { r794[i, 7] += want; r794[i, 8] += got; }')
rep('GiveGold(w[h], k);', 'GiveGold(w[h], k); if (r794 is not null) { r794[h, 9] += k; Instruments.R794Shadow(w[h])[0] += k; }')
rep('(w[i].ForeignCapitalIn * c.CapitalFlightRatePerQuarter.Value * (1.0 - cred[i])).Value);',
    '(w[i].ForeignCapitalIn * c.CapitalFlightRatePerQuarter.Value * (1.0 - cred[i])).Value); double r794t0 = taken[i], r794fci0 = w[i].ForeignCapitalIn.Value;')
rep('flightTotal += flight * e[i]; }   //',
    'flightTotal += flight * e[i]; } if (r794 is not null) { r794[i, 10] += flight; r794[i, 11] += taken[i] - r794t0; if (flight > 1e-9 && r794fci0 > 1e-12) { var sh = Instruments.R794Shadow(w[i]); sh[0] = System.Math.Max(0, sh[0] - flight * sh[0] / r794fci0); } }   //')
rep('if (div > 1e-9) taken[i] += TakeGold(w[i], div);',
    'if (div > 1e-9) taken[i] += TakeGold(w[i], div); if (r794 is not null) { r794[i, 12] += div; r794[i, 13] += taken[i] - r794t0 - r794[i, 11]; }')
rep('                w[i].CapitalAbroad = new MoneyStock(System.Math.Max(0,',
    '                if (r794 is not null && w[i].CapitalAbroad.Value - flightTotal * share / e[i] < 0) r794max0++; w[i].CapitalAbroad = new MoneyStock(System.Math.Max(0,')
rep('                double k = backTotal * share / e[i];', '                double k = backTotal * share / e[i]; if (r794 is not null) r794[i, 14] += k;')
rep('double k = TakeGold(w[i], supply[i] * sFill / e[i]);', 'double k = TakeGold(w[i], supply[i] * sFill / e[i]); if (r794 is not null) r794[i, 17] += k;')
rep('GiveGold(w[i], k); fx += k;   //', 'GiveGold(w[i], k); fx += k; if (r794 is not null) r794[i, 18] += k;   //')
rep('        if (w[0].Diplomacy is { } dip) dip.CumulativeFxResidual += fx;',
    '        if (r794 is not null) { bool refundB = !(abroadSum > 1e-9 && backTotal + flightTotal > 1e-9) && takenSum > 1e-9; double fciG = 0, caG = 0, shG = 0; for (int i = 0; i < n; i++) { if (refundB && taken[i] > 0) r794[i, 16] = taken[i]; r794[i, 19] = w[i].ForeignCapitalIn.Value; r794[i, 20] = w[i].CapitalAbroad.Value; double shv = System.Math.Min(Instruments.R794Shadow(w[i])[0], w[i].ForeignCapitalIn.Value); r794[i, 21] = shv; r794[i, 22] = InvestorSavings(w[i]); r794[i, 23] = Investors(w[i]).Count; fciG += r794[i, 19] * e[i]; caG += r794[i, 20] * e[i]; shG += shv * e[i]; } Instruments.R794Record(nations, r794, new[] { abroadSum, backTotal, flightTotal, takenSum, fx, r794max0, fciG, caG, shG, traded, riskAppetite, worldRate }); }'
    ' if (w[0].Diplomacy is { } dip) dip.CumulativeFxResidual += fx;')
assert s.count('\n') == n0, 'line count changed'
open(p, 'w', encoding='utf-8', newline='').write(s)
# Instruments.cs のフック（同じ行に足す）
q = root + '/PriceOfBread.SimCore/Instruments.cs'
t = open(q, encoding='utf-8').read()
a = 'SilverParityTable(sim, seed, q);'
assert t.count(a) == 1, t.count(a)
t = t.replace(a, 'SilverParityTable(sim, seed, q); R794Table(sim, seed, q);')
open(q, 'w', encoding='utf-8', newline='').write(t)
shutil.copy(here + '/InstrumentsR794.cs', root + '/PriceOfBread.SimCore/InstrumentsR794.cs')
print('EDITED r794')
