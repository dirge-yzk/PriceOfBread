# (815) c14fl 突然変異：使い捨ての木に 1 つずつ誤りを入れる（門の台本 gate815.py と機構の試験 CapitalCouponWriteDownTests が赤になることを確かめる）。
#   使い方: python -I mutate.py <木の根> <Mu1..Mu4>（batch.sh prep の編集の台本として呼ぶときは環境変数 MU を立てる）。
#   木の旗が 0 なら本スクリプトが既定を 1 にする。ファイルはバイトで読み書きする（CR の数と BOM を保つ）。
#   Mu1 資産だけ（外資の負債を落とさず flightTotal にだけ足す）／Mu2 2 倍落とす／Mu3 負債だけ（flightTotal に足さない）／Mu4 ÷10（桁の取り違え）。
#   門の予言（事前登録 [5]）：Mu1→G1・G2／Mu2→G1（上限）／Mu3→G2／Mu4→G1（下限）。
import sys, os
root = sys.argv[1]; mu = sys.argv[2] if len(sys.argv) > 2 else os.environ['MU']
def rep(path, a, b):
    s = open(path, 'rb').read().decode('utf-8')
    assert s.count(a) == 1, (path, a[:60], s.count(a))
    open(path, 'wb').write(s.replace(a, b).encode('utf-8'))
C = root + '/PriceOfBread.SimCore/Coefficients.cs'
s0 = open(C, 'rb').read().decode('utf-8')
if 'public double CapitalCouponUnpaidWriteDown = 0;' in s0: rep(C, 'public double CapitalCouponUnpaidWriteDown = 0;', 'public double CapitalCouponUnpaidWriteDown = 1;')
M = root + '/PriceOfBread.SimCore/CapitalMarket.cs'
W = '{ w[i].ForeignCapitalIn -= new MoneyStock(divWo); flightTotal += divWo * e[i]; }'
if mu == 'Mu1': rep(M, W, '{ flightTotal += divWo * e[i]; }   /* MUTANT */')
elif mu == 'Mu2': rep(M, 'double divWo = System.Math.Min(div - divPaid0,', 'double divWo = System.Math.Min(2 * (div - divPaid0),   /* MUTANT */')
elif mu == 'Mu3': rep(M, W, '{ w[i].ForeignCapitalIn -= new MoneyStock(divWo); }   /* MUTANT */')
elif mu == 'Mu4': rep(M, 'double divWo = System.Math.Min(div - divPaid0,', 'double divWo = System.Math.Min((div - divPaid0) / 10,   /* MUTANT */')
elif mu == 'Mu0': pass   # 旗 1 のまま誤りなし（対照）
else: raise SystemExit('unknown ' + mu)
print('mutated', mu)
