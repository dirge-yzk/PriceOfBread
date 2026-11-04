# (832) 合流：c66dw の colcheck828.py の写し（種の範囲を引数に＝種 11〜20 を読む・元の台本は触らない）。元の注：
# (828) c66dw：門 L1（旗 1 の側）と C を、事前登録 [3] の定義どおり計器が C# で書いた列 oneSidedFlight・oneSidedCoupon（ビットの比較）と、ftAfter*・ledgerAfter* の印字の一致で読む（読むだけ）。
#   gate828.py の Python の再計算は、計器の R 書式が往復しない値（例 2^-25 を 2.980232238769531E-08 と 16 桁で印字）で 1 刻みずれることがある＝定義の側（C# の列）を正とする。
#   使い方：python -X utf8 -I colcheck832.py <ins フォルダ> [種の起点 1|11]
import csv, sys, os
d = sys.argv[1]
SB = int(sys.argv[2]) if len(sys.argv) > 2 else 1
T = chr(9)
for s in range(SB, SB + 10):
    p = os.path.join(d, f'cpwdft_s{s}.tsv')
    r = csv.reader(open(p, encoding='utf-8'), delimiter=T); h = next(r); ix = {k: i for i, k in enumerate(h)}
    osF = osC = strF = strC = 0
    for row in r:
        osF += row[ix['oneSidedFlight']] == '1'; osC += row[ix['oneSidedCoupon']] == '1'
        strF += row[ix['ftAfterFlight']] != row[ix['ledgerAfterFlight']]; strC += row[ix['ftAfterCoupon']] != row[ix['ledgerAfterCoupon']]
    print(s, 'C#の列 osF', osF, 'osC', osC, '印字の比較 ftAfter≠ledgerAfter 逃避', strF, '利札', strC)
