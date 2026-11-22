# (1296) WriterRatchetTests の計算量の宣言の表（★(749) 軌道の件数を写しただけの番人）を旗 1 の木の 1 種 40 四半期の実測へ。使い方: python cost749.py <木>
import sys, os
T = sys.argv[1]; P = os.path.join(T, 'PriceOfBread.Tests/WriterRatchetTests.cs')
NEW = [('PriceDailyRuns', 17420888, 15654759), ('ZeroPopMonth', 117600, 146161), ('ZeroPopEmptyMonth', 101158, 121133), ('ConsumeSkipZeroPop', 4472144, 5554118),
       ('DeadProdService', 191513, 147846), ('ConsumeZeroPopIndep', 4472144, 5554118), ('DeadMarketMonth', 754773, 1048308), ('ClearingSkipsLB', 36613591, 28490244),
       ('ClaimProducers', 1700, 1467), ('DeadProdOther', 63990, 47201), ('DeadProdBankrupt', 1519, 9740), ('ClearingFills', 27304042, 21977491),
       ('DeadProdRgo', 47291, 41784), ('DiploHegemonScans', 2844252, 1956684), ('DeadProdZeroPop', 175483, 1167222)]
s = open(P, 'rb').read().decode('utf-8')
for name, old, new in NEW:
    a = f'[InlineData(CostLedger.{name}, {old}L)]'
    assert s.count(a) == 1, (name, s.count(a))
    b = f'[InlineData(CostLedger.{name}, {new}L)]   // ★(1296) c134f1 旗 UrbanFlowReal 1 の木 1440cd45b の 1 種 40 四半期の実測 {old}→{new}（★(749) 軌道の件数を写しただけの番人）。'
    s = s.replace(a, b)
open(P, 'wb').write(s.encode('utf-8'))
print('ok', len(NEW))
