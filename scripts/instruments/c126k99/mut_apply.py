# (1243) c126k99 突然変異を使い捨ての木に入れる（バイトで読み書き）。使い方: python -I mut_apply.py <木> <μ0|μ1|μ2>
#   μ0＝係数の既定を 0.006 へ戻す／μ1＝塊の重みを 0.006 の定数にして加算を戻す／μ2＝μ1 に加えて植民地格の判定を本国の州へ向ける。
import sys, os
T, M = sys.argv[1], sys.argv[2]
def rep(rel, old, new):
    p = os.path.join(T, rel); b = open(p, 'rb').read(); o = old.encode('utf-8'); n = new.encode('utf-8')
    c = b.count(o)
    if c != 1: print('NOMATCH', rel, c); sys.exit(1)
    open(p, 'wb').write(b.replace(o, n)); print('applied', M, rel)
C = 'PriceOfBread.SimCore/Colonial.cs'
if M == 'μ0':
    rep('PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs', 'public double ColonialUnrestHomeWeight = 0.0;', 'public double ColonialUnrestHomeWeight = 0.006;')
else:
    rep(C, 'if (c.ColonialUnrestHomeWeight > 1e-9)', 'if (0.006 > 1e-9)')
    rep(C, 'double homePressure = c.ColonialUnrestHomeWeight *', 'double homePressure = 0.006 *')
    if M == 'μ2':
        rep(C, '''                    if (cr.Tier != RegionTier.Colonial) continue;
                    var (mn, md) = Politics.RegionMilitancy(cr);''', '''                    if (cr.Tier == RegionTier.Colonial) continue;
                    var (mn, md) = Politics.RegionMilitancy(cr);''')
