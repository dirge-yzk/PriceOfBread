# (810) c13fl 突然変異：使い捨ての木に 1 つずつ誤りを入れる（門の台本 gate810.py と機構の試験 CapitalCouponWriteDownTests が赤になることを確かめる）。
#   使い方: python -I mutate.py <木の根> <Mu1..Mu6>（batch.sh prep の編集の台本として呼ぶときは環境変数 MU を立てる）。
#   木の旗が 0 なら本スクリプトが既定を 1 にする。ファイルはバイトで読み書きする（CR の数と BOM を保つ）。
#   Mu1 落とさない／Mu2 2 倍落とす／Mu3 利回りの項を消す（ret に足さない）／Mu4 符号を逆（ret から引く）／Mu5 旧の単位（分母から K を外す＝形 B）／Mu6 新規の流れを 0。
#   門の予言（事前登録 [6]）：Mu1・Mu2→G2／Mu3・Mu4→G5／Mu5→G4（と G3 の見込み）／Mu6→G6。
import sys, os
root = sys.argv[1]; mu = sys.argv[2] if len(sys.argv) > 2 else os.environ['MU']
def rep(path, a, b):
    s = open(path, 'rb').read().decode('utf-8')
    assert s.count(a) == 1, (path, a[:60], s.count(a))
    open(path, 'wb').write(s.replace(a, b).encode('utf-8'))
C = root + '/PriceOfBread.SimCore/Coefficients.cs'
s0 = open(C, 'rb').read().decode('utf-8')
if 'public double CapitalDividendFromHostRent = 0;' in s0: rep(C, 'public double CapitalDividendFromHostRent = 0;', 'public double CapitalDividendFromHostRent = 1;')
M = root + '/PriceOfBread.SimCore/CapitalMarket.cs'
if mu == 'Mu1': rep(M, 'if (divWo > 0) { w[i].ForeignCapitalIn -= new MoneyStock(divWo); flightTotal += divWo * e[i]; }', 'if (divWo > 0 && false) { w[i].ForeignCapitalIn -= new MoneyStock(divWo); flightTotal += divWo * e[i]; }   /* MUTANT */')
elif mu == 'Mu2': rep(M, 'double divWo = System.Math.Min(divU, w[i].ForeignCapitalIn.Value);', 'double divWo = System.Math.Min(2 * divU, w[i].ForeignCapitalIn.Value);   /* MUTANT */')
elif mu == 'Mu3': rep(M, 'double r0 = ret[i]; ret[i] += rt;', 'double r0 = ret[i]; ret[i] += 0 * rt;   /* MUTANT */')
elif mu == 'Mu4': rep(M, 'double r0 = ret[i]; ret[i] += rt;', 'double r0 = ret[i]; ret[i] -= rt;   /* MUTANT */')
elif mu == 'Mu5': rep(M, 'double den = fci + sav + k;', 'double den = fci + sav;   // MUTANT')
elif mu == 'Mu6': rep(M, 'double traded = System.Math.Min(totalDemand, totalSupply) * riskAppetite;', 'double traded = 0 * System.Math.Min(totalDemand, totalSupply) * riskAppetite;   // MUTANT')
else: raise SystemExit('unknown ' + mu)
print('mutated', mu)
