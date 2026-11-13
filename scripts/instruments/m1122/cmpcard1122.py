# (1122) 合流（m1103/cmpcard1103.py の写し・中身は同じ）。
# (1103) 成績表 2 枚を、名乗り行（SOURCE-HASH）と〈★係数の上書き（POB_COEF）〉の行を除いて全行バイトで比べる（読むだけ）。
# 使い方: python -X utf8 -I cmpcard1122.py <成績表 A> <成績表 B>
import sys
def lines(p):
    return [l for l in open(p, 'rb').read().replace(b'\r\n', b'\n').split(b'\n') if b'SOURCE-HASH' not in l and '★係数の上書き'.encode('utf-8') not in l]
a, b = lines(sys.argv[1]), lines(sys.argv[2])
d = [(i, x, y) for i, (x, y) in enumerate(zip(a, b)) if x != y]
print(f'{sys.argv[1]} 対 {sys.argv[2]}：行 {len(a)}／{len(b)}・違う行 {len(d) + abs(len(a) - len(b))}' + ('＝全行バイト一致（名乗り行と係数の上書きの行を除く）' if not d and len(a) == len(b) else ''))
for i, x, y in d[:8]: print('  ', i, x.decode('utf-8', 'replace')[:120], '|', y.decode('utf-8', 'replace')[:120])
