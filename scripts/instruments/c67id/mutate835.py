# (835) c67id：cpwdft の上限の門 U1 を赤にする突然変異（使い捨ての木だけに当てる・リポジトリの木には当てない）。(828) の反証役〈受け皿の側を動かす突然変異が表に無い〉の穴を塞ぐ。
# 使い方：python mutate835.py <F1|Mu7|Mu8> <木の根>
#   全ての突然変異は旗 CapitalClaimCancelFromLedger の既定を 1 にしてから当てる（F1＝旗 1 だけ・突然変異なし＝比べる側。(828) の mutate828.py の F1 と同じ）。
#   Mu7＝逃避の行で、受け皿の側（外資の負債）を逃避の額の半分だけ減らす（持ち主の側は旗 1 の台帳の差で書くので両側は一致のまま・受け皿の側だけ旧の額からずれる）
#   Mu8＝利札の落としの行で、受け皿の側を落とす額 divWo の半分だけ減らす（同じ）
#   バイトで読み書きする（CR の数と BOM を保つ）。
import sys, os
mu, root = sys.argv[1], sys.argv[2]


def edit(rel, pairs):
    p = os.path.join(root, rel)
    b = open(p, 'rb').read()
    for o, n in pairs:
        o = o.encode('utf-8'); n = n.encode('utf-8')
        assert b.count(o) == 1, (rel, o)
        b = b.replace(o, n)
    open(p, 'wb').write(b)


_cp = os.path.join(root, 'PriceOfBread.SimCore/Coefficients.cs')
if b'    public double CapitalClaimCancelFromLedger = 1;' not in open(_cp, 'rb').read():
    edit('PriceOfBread.SimCore/Coefficients.cs', [('    public double CapitalClaimCancelFromLedger = 0;', '    public double CapitalClaimCancelFromLedger = 1;')])
CM = 'PriceOfBread.SimCore/CapitalMarket.cs'
if mu == 'F1':
    pass
elif mu == 'Mu7':
    edit(CM, [('w[i].ForeignCapitalIn -= new MoneyStock(flight); flightTotal += flight * e[i]; }', 'w[i].ForeignCapitalIn -= new MoneyStock(flight * 0.5); flightTotal += flight * e[i]; }')])
elif mu == 'Mu8':
    edit(CM, [('w[i].ForeignCapitalIn -= new MoneyStock(divWo); flightTotal +=', 'w[i].ForeignCapitalIn -= new MoneyStock(divWo * 0.5); flightTotal +=')])
else:
    raise SystemExit('unknown ' + mu)
print('MUTATED', mu)
