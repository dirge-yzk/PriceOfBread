# (942) 使い捨て：門と突然変異の変種を木へ書く（バイトで読み書き）。変種＝<G1|G2|M1|M2|M3>_<flag1|flag0>
import sys
t, v = sys.argv[1], sys.argv[2]
kind, flag = v.split('_')

def edit(rel, old, new):
    p = t + '/' + rel
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    assert b.count(o) == 1, (rel, old[:60], b.count(o))
    open(p, 'wb').write(b.replace(o, n))

if flag == 'flag0':
    edit('PriceOfBread.SimCore/Coefficients.cs', 'public double LandTaxFixedBasis = 1.0;', 'public double LandTaxFixedBasis = 0.0;')
if kind == 'M1':   # 計器：州の表の行を書いた後に払った額を戻さない（積み残しが次の四半期へ漏れる）
    edit('PriceOfBread.SimCore/Instruments.cs',
         'if (a is not null) { a.Judged = 0; a.Desired = a.Budget = a.Planned = a.PlannedUnits = a.BuiltUnits = a.Paid = 0; ClearTerms(a); }',
         'if (a is not null) { a.Judged = 0; a.Desired = a.Budget = a.Planned = a.PlannedUnits = a.BuiltUnits = 0; ClearTerms(a); }')
elif kind == 'M2':   # 計器の呼び：州の表へ材料だけを渡す（工賃を落とす）
    edit('PriceOfBread.SimCore/EconomyStep.cs',
         'Instruments.ConsRegionPaid(world!, region, built, cost);',
         'Instruments.ConsRegionPaid(world!, region, built, sGot * clearPrice[sg] + mGot * clearPrice[mg] + cGot * clearPrice[cg] + lGot * clearPrice[lg]);')
elif kind == 'M3':   # 試験：全ての区切りを比べない（除く述語が常に真）
    edit('PriceOfBread.Tests/InstrumentsTests.cs', 'if (movedBuckets.Contains(b)) continue;', 'if (movedBuckets.Contains(b) || b >= 0) continue;')
