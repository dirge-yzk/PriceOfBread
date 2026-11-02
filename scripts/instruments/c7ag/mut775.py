# (775) 突然変異の編集（使い捨ての木の MultiNationSim.cs の注入の節だけを替える）。batch.sh prep の編集の python として渡せる。
#   python scripts/instruments/c7ag/mut775.py <木の根> <M1|M2|M3>
#   M1：平価の符号（Ref÷rel）／M2：対数平均でなく算術平均／M3：生きた市場の条件を外す（死んだ州の凍った比を算入）
import sys, os
root, which = sys.argv[1], sys.argv[2]
p = os.path.join(root, 'PriceOfBread.SimCore', 'MultiNationSim.cs')
b = open(p, 'rb').read().decode('utf-8')
def rep(old, new):
    global b
    assert b.count(old) == 1, f'{which}: 置換元が 1 件でない: {old[:60]}'
    b = b.replace(old, new)
if which == 'M1':
    rep("silverWorld = rel * Catalog.RefPrices[Catalog.SilverId];", "silverWorld = Catalog.RefPrices[Catalog.SilverId] / rel;")
elif which == 'M2':
    rep("ratioSum += System.Math.Log(pAg / pAu); ratioCnt++;", "ratioSum += pAg / pAu; ratioCnt++;")
    rep("double meanRatio = ratioCnt > 0 ? System.Math.Exp(ratioSum / ratioCnt) : 0;", "double meanRatio = ratioCnt > 0 ? ratioSum / ratioCnt : 0;")
elif which == 'M3':
    rep("if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;   // 死んだ市場（MarketFlow と同じ条件）は除外", "// M3：生きた市場の条件を外した")
else:
    raise SystemExit('M1|M2|M3')
open(p, 'wb').write(b.encode('utf-8'))
print('OK', which)
