# (1153) c113qn 使い捨ての木の編集（batch.sh prep の編集の python・引数に木の根・形は環境変数 QN_MUT）。バイトで読み書きする（CR と BOM を保つ）。
#   coef0：p04 の MalariaQuininePerColony の既定 6.0→0.0（係数 0 の短い走り＝腕 B の形）
#   m1   ：coef0 ＋ 遠征のキニーネの門（World.cs）に係数＞0 を足す（遠征まで消す）
#   m2   ：coef0 ＋ :24 の率を定数 6.0 にする（調達を残す＝:43 は 6×州の数で呼ばれ戻り値は qnGot へ）
#   m3   ：係数の既定 3.0（半分だけ消す）
#   m4   ：coef0 ＋ :41 の燃料の調達を外す（燃料まで消す）
import sys, os
root = sys.argv[-1]; form = os.environ.get('QN_MUT', '')


def rep(rel, old, new):
    p = os.path.join(root, rel); b = open(p, 'rb').read()
    assert b.count(old) == 1, (rel, old, b.count(old))
    open(p, 'wb').write(b.replace(old, new))


P04 = 'PriceOfBread.SimCore/Coefficients.p04_trade.cs'
COL = 'PriceOfBread.SimCore/Colonial.cs'
WLD = 'PriceOfBread.SimCore/World.cs'
OLD = b'public double MalariaQuininePerColony = 6.0;'
if form == 'm3':
    rep(P04, OLD, b'public double MalariaQuininePerColony = 3.0;')
elif form in ('coef0', 'm1', 'm2', 'm4'):
    rep(P04, OLD, b'public double MalariaQuininePerColony = 0.0;')
    if form == 'm1':
        rep(WLD, b'        if (target.RequiresQuinine)   //', b'        if (Coef.MalariaQuininePerColony > 0 && target.RequiresQuinine)   //')
    elif form == 'm2':
        rep(COL, b'if (malarial) quinineWant += c.MalariaQuininePerColony;', b'if (malarial) quinineWant += 6.0;')
    elif form == 'm4':
        rep(COL, b'        BuyUpkeepGood(w, r, fuelG, fuelWant);\n', b'        _ = fuelG + fuelWant;\n')
elif form == 'm5':
    rep('PriceOfBread.SimCore/Instruments/Instruments.cs', b' ColqnInvaded(invader, target); }', b' }')
else:
    raise SystemExit('形の名が無い：' + form)
print('mut', form, 'ok')
