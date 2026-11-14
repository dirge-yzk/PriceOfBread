# (1145) c112hy 使い捨ての木の編集（batch.sh prep の編集の python・引数に木の根と形の名）。バイトで読み書きする（CR と BOM を保つ）。
#   coef0：p06 の DefeatShockTensionRelief の既定 2.0→0.0（係数 0 の短い走り＝腕 B の形）
#   m1   ：coef0 ＋ 枝の門を係数でなく定数 2.0 で開ける（枝を残す）
#   m2   ：coef0 ＋ 遺恨の枝の率から (1−遺恨) を外す（遺恨の枝まで消す）
#   m3   ：coef0 ＋ 凍結の枝を外す（従属の組も遺恨の式へ）
import sys, os
root, form = sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else os.environ.get('HY_MUT', '')
def rep(rel, old, new):
    p = os.path.join(root, rel); b = open(p, 'rb').read()
    assert b.count(old) == 1, (rel, old, b.count(old))
    open(p, 'wb').write(b.replace(old, new))
P06 = 'PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs'
DIP = 'PriceOfBread.SimCore/Diplomacy.cs'
rep(P06, b'public double DefeatShockTensionRelief = 2.0;', b'public double DefeatShockTensionRelief = 0.0;')
if form == 'm1':
    rep(DIP, b'else if (c.DefeatShockTensionRelief > 0 && t >= c.HighTensionBand && DefeatedShock(k) is var shock && shock >= c.DefeatShockProtectMin)',
             b'else if (t >= c.HighTensionBand && DefeatedShock(k) is var shock && shock >= c.DefeatShockProtectMin)')
    rep(DIP, b'Math.Max(0, t - c.DefeatShockTensionRelief * shock)', b'Math.Max(0, t - 2.0 * shock)')
elif form == 'm2':
    rep(DIP, b'double rate = c.TensionDecay * (1.0 - PoliticalQuantities.Clamp01(_grudge.GetValueOrDefault(k)));', b'double rate = c.TensionDecay;')
elif form == 'm3':
    rep(DIP, b'else if (t >= c.HighTensionBand && CannotDeclareWar(k))', b'else if (false && t >= c.HighTensionBand && CannotDeclareWar(k))')
elif form != 'coef0':
    raise SystemExit('形の名が無い：' + form)
print('mut', form, 'ok')
