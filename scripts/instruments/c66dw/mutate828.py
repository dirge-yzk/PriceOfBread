# (828) c66dw 突然変異（使い捨ての木だけに当てる・リポジトリの木には当てない）。事前登録 measurements/m828_20261104_prereg_dw.txt の [5]。
# 使い方：python mutate828.py <Mu1|Mu2|Mu3|Mu4|Mu5|Mu6|F1> <木の根>
#   全ての突然変異は旗 CapitalClaimCancelFromLedger の既定を 1 にしてから当てる（F1＝旗 1 だけ・突然変異なし＝比べる側）。
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
edit('PriceOfBread.SimCore/Coefficients.cs', [('    public double CapitalClaimCancelFromLedger = 0;', '    public double CapitalClaimCancelFromLedger = 1;')])
FL = 'if (c.CapitalClaimCancelFromLedger > 0 && flight > 1e-9) flightTotal = '
CP = 'flightTotal += (c.CapitalClaimCancelFromLedger > 0 ? fciPreWo - w[i].ForeignCapitalIn.Value : divWo) * e[i];'
CM = 'PriceOfBread.SimCore/CapitalMarket.cs'
if mu == 'F1':
    pass
elif mu == 'Mu1':   # 両行とも旧の額（直さない）
    edit(CM, [(FL, 'if (false && flight > 1e-9) flightTotal = '), (CP, 'flightTotal += (false ? fciPreWo - w[i].ForeignCapitalIn.Value : divWo) * e[i];')])
elif mu in ('Mu2', 'Mu4'):   # 利札の行を旧へ（持ち主の側にだけ divWo を足す）＝逃避の行だけ直す
    edit(CM, [(CP, 'flightTotal += (false ? fciPreWo - w[i].ForeignCapitalIn.Value : divWo) * e[i];')])
elif mu == 'Mu3':   # 負債の側だけ落とす（利札の行で累計に足さない）
    edit(CM, [(CP, 'flightTotal += (c.CapitalClaimCancelFromLedger > 0 ? 0.0 : divWo) * e[i];')])
elif mu == 'Mu5':   # 利札の行だけ直す（逃避の行は旧）
    edit(CM, [(FL, 'if (false && flight > 1e-9) flightTotal = ')])
elif mu == 'Mu6':   # 落とす前の門を divWo>0 へ（指示の字どおり）
    edit(CM, [('if (divWo > 1e-9) { double fciPreWo', 'if (divWo > 0) { double fciPreWo')])
else:
    raise SystemExit('unknown ' + mu)
print('MUTATED', mu)
