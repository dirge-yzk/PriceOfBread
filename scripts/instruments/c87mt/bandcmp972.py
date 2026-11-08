# (972) 成績表の 2 本を全行で突き合わせる（ident_check.py と同じ除き方＝★係数の上書き・SOURCE-HASH の名乗り行を除く）。python -I bandcmp972.py <腕の成績表> <基準の成績表>
import sys
SKIP = ('★係数の上書き'.encode('utf-8'), b'SOURCE-HASH')
def L(p): return [l for l in open(p, 'rb').read().split(b'\n') if not l.startswith(SKIP)]
a, b = L(sys.argv[1]), L(sys.argv[2])
d = [i for i, (x, y) in enumerate(zip(a, b)) if x != y]
print('成績表', sys.argv[1], '対', sys.argv[2], '行', len(a), len(b), '違う行', len(d) + abs(len(a) - len(b)), '判定', '全行一致' if not d and len(a) == len(b) else '不一致')
for i in d[:10]: print('  行', i + 1, a[i][:160].decode('utf-8', 'replace'), '|', b[i][:160].decode('utf-8', 'replace'))
