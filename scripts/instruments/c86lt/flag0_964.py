# (964) c86lt 腕 A の旗 0：batch.sh prep の書き換え（src の写しにだけ当たる・木は書き換えない）。
#   LandTaxFixedBasis の既定 1.0 を 0.0 にする 1 か所だけ（錨が 1 本でなければ止める）。POB_COEF を使わない理由＝成績表に〈係数の上書き〉の名乗り行が 1 行増え、全行一致の比べに例外が要る。
#   python flag0_964.py <src の根>
import os, sys
p = os.path.join(sys.argv[1], 'PriceOfBread.SimCore', 'Coefficients.cs')
b = open(p, 'rb').read()
old = b'public double LandTaxFixedBasis = 1.0;'
new = b'public double LandTaxFixedBasis = 0.0;'
n = b.count(old)
if n != 1:
    print('flag0_964: 錨が %d 本' % n)
    sys.exit(1)
open(p, 'wb').write(b.replace(old, new))
print('flag0_964: LandTaxFixedBasis 1.0 -> 0.0')
