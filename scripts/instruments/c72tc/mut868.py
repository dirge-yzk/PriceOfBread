#!/usr/bin/env python3
# (868) c72tc 突然変異と旗 1 の使い捨ての木の書き換え（バイトで読み書き）。python -I mut868.py <型> <木>
#   F1 旗 ImportPayerLive の既定を 1 にする（Coefficients.cs）
#   M1 盤面の行（人の居る POP に在庫）を外す（TradeClearingTests.cs）
#   M2 旧の規則の予算の手形の枝を外す（InstrumentsCearn.cs）
#   M3 清算の頭で清算内の払いを 0 に戻さない（＝清算ごとの超過が四半期の和になる）
import sys, os
mode, root = sys.argv[1], sys.argv[2]

def sub(rel, old, new):
    p = os.path.join(root, rel)
    b = open(p, 'rb').read()
    o = old.encode('utf-8'); n = new.encode('utf-8')
    assert b.count(o) == 1, (rel, old, b.count(o))
    open(p, 'wb').write(b.replace(o, n))

if mode == 'F1':
    import re
    p = os.path.join(root, 'PriceOfBread.SimCore/Coefficients.cs')
    b = open(p, 'rb').read().decode('utf-8')
    m = re.search(r'public double ImportPayerLive\s*=\s*0(\.0)?;', b)
    assert m, 'ImportPayerLive の既定が見つからない'
    b = b[:m.start()] + m.group(0).replace('0.0;', '1.0;').replace('= 0;', '= 1;') + b[m.end():]
    open(p, 'wb').write(b.encode('utf-8'))
elif mode == 'M1':
    sub('PriceOfBread.Tests/TradeClearingTests.cs',
        'foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Size > 1e-12) p.SetSavings(1e9);   // (868)',
        '   // (868)')
elif mode == 'M2':
    sub('PriceOfBread.SimCore/InstrumentsCearn.cs',
        'if (m is not null) ob = m.Debt.Value > 0 ? System.Math.Max(0, m.Savings.Value) : double.PositiveInfinity;',
        'if (m is not null) ob = System.Math.Max(0, m.Savings.Value);')
elif mode == 'M3':
    sub('PriceOfBread.SimCore/InstrumentsCearn.cs',
        'a.ImpHeadStock = double.IsPositiveInfinity(b) ? double.NaN : b; a.ImpPaidClr = 0;',
        'a.ImpHeadStock = double.IsPositiveInfinity(b) ? double.NaN : b;')
else:
    raise SystemExit('型が分からない ' + mode)
print('edited', mode)
