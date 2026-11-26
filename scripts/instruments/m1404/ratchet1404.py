# (1404) 合流：WriterRatchetTests の宣言の表の取り直し（★(749)＝軌道の件数を写しただけの番人）。合わせた木 650bb955a（pm・rn の旗 1）の 1 種 40 四半期の実測。
p = 'PriceOfBread.Tests/WriterRatchetTests.cs'
b = open(p, 'rb').read(); L = b.split(b'\n')
rows = [('ConstructionSeen', 2182, 2413, 'pm 単独 2182・rn 単独は表を変えず'), ('ClaimProducers', 1716, 1882, 'pm 単独 1813・rn 単独 1761'),
        ('ConsumeSkipZeroPop', 4472144, 4567100, '線の単独では許しの内'), ('ConsumeZeroPopIndep', 4472144, 4567100, '線の単独では許しの内'),
        ('DeadProdBankrupt', 1339, 1481, 'pm 単独 1423・rn 単独 1007'), ('ZeroPopMonth', 117600, 120067, '線の単独では許しの内'),
        ('ZeroPopEmptyMonth', 101158, 103273, '線の単独では許しの内'), ('DiploHegemonScans', 1902892, 4121812, 'pm 単独 2689600・rn 単独 4491632＝AI の植民の試行と参戦の評価の増え〔rn〕')]
for name, old, new, note in rows:
    o = f'[InlineData(CostLedger.{name}, {old}L)]'.encode()
    idx = [i for i, l in enumerate(L) if l.lstrip().startswith(o)]
    assert len(idx) == 1, (name, idx)
    i = idx[0]; ind = L[i][:len(L[i]) - len(L[i].lstrip())]; cr = b'\r' if L[i].endswith(b'\r') else b''
    pct = (new - old) / old * 100
    L[i] = ind + f'[InlineData(CostLedger.{name}, {new}L)]   // ★(1404) 合流の表の更新（★(749)＝軌道の件数を写しただけの番人）＝合わせた木 650bb955a（pm 旗 PriceOnlyStepDays 30・rn 旗 RandomOnlyClimateEpidemic 1）の 1 種 40 四半期の実測 {old}→{new}（{pct:+.1f}%・{note}・新しい計数の処理は無い）'.encode('utf-8') + cr
open(p, 'wb').write(b'\n'.join(L)); print('ok')
