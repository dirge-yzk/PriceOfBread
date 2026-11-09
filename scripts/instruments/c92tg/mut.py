# (1003) 突然変異 M1〜M5 を使い捨ての木に当てる。使い方：python mut.py <木> <M1..M5>
import sys, io
tree, m = sys.argv[1], sys.argv[2]
def edit(rel, old, new):
    p = tree + '/' + rel
    b = open(p, 'rb').read().decode('utf-8')
    assert b.count(old) == 1, (rel, old, b.count(old))
    open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
NG = 'PriceOfBread.SimCore/InstrumentsNationgood.cs'
TS = 'PriceOfBread.SimCore/InstrumentsTradeskip.cs'
if m == 'M1':   # 国内を混ぜる（越境の印を見ない）
    edit(NG, 'if (!cross || !NgAccs.TryGetValue', 'if (!NgAccs.TryGetValue')
elif m == 'M2':   # 結果の 1 つ（需要なし）を数えない
    edit(TS, 'Add(curBn, curG, which == TsOutIdle ? TsCol.Idle : which == TsOutNoNeed ? TsCol.NoNeed : TsCol.Budget0);', 'if (which != TsOutNoNeed) Add(curBn, curG, which == TsOutIdle ? TsCol.Idle : TsCol.Budget0);')
elif m == 'M3':   # 関税を抜いた判定を運賃を抜いた判定と同じにする
    edit(TS, 'if (selP * (baseMul + wS2 - duty) < lp) Add(curBn, curG, TsCol.WA);', 'if (selP * (1.0 + wS2) < lp) Add(curBn, curG, TsCol.WA);')
elif m == 'M4':   # 量の代わりに額を積む
    edit(NG, 'a.ImpQ[bi * a.Gc + good] += qty;', 'a.ImpQ[bi * a.Gc + good] += value;')
elif m == 'M5':   # 輸入から運賃を落とす
    edit(NG, 'a.ImpV[bi * a.Gc + good] += value + freight;', 'a.ImpV[bi * a.Gc + good] += value;')
elif m == 'M6':   # C2 の標本の四半期を C1 とずらす（ident 1 回目の形：World.Year の読みが +1 年ずれる）
    edit(TS, '!NgYear(w0.Year + 1)', '!NgYear(w0.Year)')
else:
    raise SystemExit('unknown ' + m)
