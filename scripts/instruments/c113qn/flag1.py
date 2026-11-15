# (1153) c113qn 旗 1 の編集（係数の既定 6.0→0.0・DormantIdentityTests の ZeroDefaultLedger に 1 行）。バイトで読み書きする。使い方: python flag1.py <木の根>
import sys, os
root = sys.argv[-1]
def rep(rel, old, new):
    p = os.path.join(root, rel); b = open(p, 'rb').read()
    assert b.count(old) == 1, (rel, old, b.count(old))
    open(p, 'wb').write(b.replace(old, new))
rep('PriceOfBread.SimCore/Coefficients.p04_trade.cs', b'public double MalariaQuininePerColony = 6.0;', b'public double MalariaQuininePerColony = 0.0;')
rep('PriceOfBread.Tests/DormantIdentityTests.cs', b'        "DefeatShockTensionRelief",\n',
    b'        "DefeatShockTensionRelief",\n'
    + '        // (1153) 植民地の毎月のキニーネを閉じた（既定 0＝門 quinineWant > 1e-9 が閉じる）。quinineWant と係数の削除は次の恒等の線＝そこでこの行も消す\n'.encode('utf-8')
    + b'        "MalariaQuininePerColony",\n')
print('flag1 ok')
