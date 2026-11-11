# (820) c65op 突然変異：使い捨ての木に 1 つずつ誤りを入れる（門の台本 gate_op.py と機構の試験 HabitLogisticTests が赤になることを確かめる）。
#   使い方: python -I mutate.py <木の根> <Mu1..Mu3>（batch.sh prep の編集の台本として呼ぶときは環境変数 MU を立てる）。
#   木の旗が 0 なら本スクリプトが既定を 1 にする。ファイルはバイトで読み書きする（CR の数と BOM を保つ）。
#   Mu1 (1 − s/d) を外す（挟みは d のまま）／Mu2 減衰を 0（新の式の中だけ）／Mu3 d を 1（新の式の中と挟み）。Mu0（旗を読まない＝旗 0 の経路）は ident の旗 0 の計器つき 10 種で読む。
import sys, os
root = sys.argv[1]; mu = sys.argv[2] if len(sys.argv) > 2 else os.environ['MU']
def rep(path, a, b):
    s = open(path, 'rb').read().decode('utf-8')
    assert s.count(a) == 1, (path, a[:60], s.count(a))
    open(path, 'wb').write(s.replace(a, b).encode('utf-8'))
C = root + '/PriceOfBread.SimCore/Coefficients.cs'
s0 = open(C, 'rb').read().decode('utf-8')
if 'public double OpiumAddictionLogistic = 0;' in s0: rep(C, 'public double OpiumAddictionLogistic = 0;', 'public double OpiumAddictionLogistic = 1;')
H = root + '/PriceOfBread.SimCore/Habit.cs'
NEW = 'h.Logistic ? stock + h.Growth * perCapita * (1 - stock / h.Cap) - h.Decay * stock'
if mu == 'Mu1': rep(H, NEW, 'h.Logistic ? stock + h.Growth * perCapita - h.Decay * stock /* MUTANT */')
elif mu == 'Mu2': rep(H, NEW, 'h.Logistic ? stock + h.Growth * perCapita * (1 - stock / h.Cap) - 0 * stock /* MUTANT */')
elif mu == 'Mu3':
    rep(H, NEW, 'h.Logistic ? stock + h.Growth * perCapita * (1 - stock / 1.0) - h.Decay * stock /* MUTANT */')
    rep(H, ': stock + h.Growth * perCapita - h.Decay * stock, 0, h.Cap, 0);', ': stock + h.Growth * perCapita - h.Decay * stock, 0, h.Logistic ? 1.0 : h.Cap, 0);   /* MUTANT */')
elif mu == 'Mu0': pass   # 旗 1 のまま誤りなし（対照）
else: raise SystemExit('unknown ' + mu)
print('mutated', mu)
