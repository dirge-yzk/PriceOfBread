# (801) c11fl 突然変異：使い捨ての木に 1 つずつ誤りを入れる（門の台本 divhost_read.py と機構の試験 CapitalDividendHostRentTests が赤になることを確かめる）。
#   使い方: python -I mutate.py <木の根> <Mu1..Mu5>（batch.sh prep の編集の台本として呼ぶときは環境変数 MU=Mu1 を立てる）。
#   木は旗の既定を 1 にした物（旗が 0 の木なら本スクリプトが 1 にする）。ファイルはバイトで読み書きする（CR の数と BOM を保つ）。
#   Mu1 率×外資の負債のまま（産出と結ばず固定の率 0.02 に持ち分を掛けただけ）／Mu2 レントでなく貯蓄で引く／Mu3 利札 0／Mu4 持ち分の二乗／Mu5 月の値を 3 倍しない。
#   門の予言：Mu1・Mu2 は上限の門 U で落ちる／Mu3・Mu4・Mu5 は下限の門 L で落ちる。
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
A = 'double d = Calendar.PerMonthToPerQuarter(rent) * (fci / (fci + sav));'
if mu == 'Mu1': rep(M, A, 'double d = 0.02 * fci * (fci / (fci + sav));   // MUTANT')
elif mu == 'Mu2': rep(M, A, 'double d = 0.1 * sav * (fci / (fci + sav));   // MUTANT')
elif mu == 'Mu3': rep(M, A, 'double d = 0;   // MUTANT')
elif mu == 'Mu4': rep(M, A, 'double d = Calendar.PerMonthToPerQuarter(rent) * (fci / (fci + sav)) * (fci / (fci + sav));   // MUTANT')
elif mu == 'Mu5': rep(M, A, 'double d = rent * (fci / (fci + sav));   // MUTANT')
else: raise SystemExit('unknown ' + mu)
print('mutated', mu)
