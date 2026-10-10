"""(211) 番人の突然変異（潤沢線＝戦費予備−実際の借入と同じ規則で借りられる額）。python mut211.py <木の根> <M0..M9>"""
import sys
root, m = sys.argv[1], sys.argv[2]
SP = 'PriceOfBread.SimCore/SelfPlay.cs'
MUT = {
    'M0': [],
    # 貸し手のプールを読まない
    'M1': [(SP, 'double pool = Fiscal.LenderPool(w, c, gdp, CapitalMarket.InvestorSavings(w), out _);', 'double pool = double.PositiveInfinity;')],
    # 月の取り崩しで止めない
    'M2': [(SP, '        if (per > draw) per = draw;\n', '')],
    # 冷却を0か1 にする（(203) の形）
    'M3': [(SP, 'double open = months - w.BorrowCooldown;', 'double open = w.BorrowCooldown > 0 ? 0 : months;')],
    # 助走を0にする（(203) の形）
    'M4': [(SP, 'if (!(open > 0) || !(chest > 0)) return 0;', 'if (!(open > 0) || !(chest > 0) || w.WarmupFrozen) return 0;')],
    # 金利を読まない
    'M5': [(SP, 'roomI * Calendar.MonthsPerYear / rate', 'roomI * Calendar.MonthsPerYear / 0.04')],
    # 1件の額を読まない
    'M6': [(SP, 'double per = Fiscal.BorrowTicket(gdp, c);', 'double per = double.PositiveInfinity;')],
    # 呼び手（支出反射）が旧の線（戦費予備）を読む
    'M7': [('PriceOfBread.SimCore/World.cs', 'double line = UtilityAgent.OpulenceLineOf(this);',
            'double line = (UtilityAgent.FiscalBufferOf(this) + Coef.FiscalBufferSoldierPayWeight * Coef.FiscalStressBufferMonths * LastSoldierPay) * UtilityAgent.OpulenceBufferQuarters;')],
    # 呼び手（減税反射）が旧の線（戦費予備）を読む
    'M8': [(SP, 'double buffer = OpulenceLineOf(w);   // R3安定形',
            'double buffer = (FiscalBufferOf(w) + w.Coef.FiscalBufferSoldierPayWeight * w.Coef.FiscalStressBufferMonths * w.LastSoldierPay) * OpulenceBufferQuarters;   // R3安定形')],
    # 月初の予算が兵士給与の請求書を0にしてから潤沢線を読む（旧の順）
    'M9': [('PriceOfBread.SimCore/Simulation.cs',
            '        double spendRevNat = World.SpendableRevenue();\n        double milEnvelope = Military.EnvelopeOf(World, World.Coef);\n        World.LastSoldierPay = 0;',
            '        World.LastSoldierPay = 0;\n        double spendRevNat = World.SpendableRevenue();\n        double milEnvelope = Military.EnvelopeOf(World, World.Coef);')],
    # 減税の傾きを旧の式（潤沢線÷国庫）へ戻す
    'M10': [(SP, 'double mul = Math.Max(OpulenceTaxFloor, chest / (chest + (w.Treasury - buffer)));', 'double mul = Math.Max(OpulenceTaxFloor, buffer / w.Treasury);')],
}
for rel, old, new in MUT[m]:
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    assert s.count(old) == 1, (m, old, s.count(old))
    s = s.replace(old, new)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
print('mut', m)
