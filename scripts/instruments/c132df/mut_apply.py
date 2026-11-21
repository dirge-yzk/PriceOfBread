# (1284) c132df 突然変異を木に 1 本入れる（バイトで読み書き・各置き換えはちょうど 1 回）。使い方: python mut_apply.py <木> <M0|M1|M4|M6|F0>
import sys
tree, m = sys.argv[1], sys.argv[2]
F = 'PriceOfBread.SimCore/Fiscal.cs'
PRE = "        if (onePredicate) MetallicStandardGate(w, c, owes, broken);\n"
POST = "        if (!onePredicate) MetallicStandardGate(w, c, w.FiscalFundingRatio.Value < c.GoldSuspendMinFundingRatio, broken);\n"
MUT = {
    'M0': [],
    'M1': [(PRE, "        if (onePredicate) MetallicStandardGate(w, c, w.FiscalFundingRatio.Value < c.GoldSuspendMinFundingRatio, broken);\n")],
    'M4': [(PRE, ""), (POST, "        MetallicStandardGate(w, c, onePredicate ? owes : w.FiscalFundingRatio.Value < c.GoldSuspendMinFundingRatio, broken);\n")],
    'M6': [(PRE, "        if (onePredicate) MetallicStandardGate(w, c, payRate < 1 && w.GovDebt.Value > 1e-9 && !w.WarmupFrozen, broken);\n")],
    'F0': [("        bool onePredicate = c.ArrearsOnePredicate > 0;\n", "        bool onePredicate = false;\n")],
}
p = f'{tree}/{F}'; b = open(p, 'rb').read()
for o, n in MUT[m]:
    ob = o.encode('utf-8')
    if b.count(ob) != 1: print(f'{m} 置き換えの旧が {b.count(ob)} 回'); sys.exit(1)
    b = b.replace(ob, n.encode('utf-8'))
open(p, 'wb').write(b); print(f'{m} 入れた：{p}（{len(MUT[m])} 箇所）')
