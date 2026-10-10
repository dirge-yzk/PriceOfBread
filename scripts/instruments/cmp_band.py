"""(241) 帯の報告どうしを、名乗りの行（★係数の上書き）を除いて全行バイトで突き合わせる。python cmp241.py <新> <基準>"""
import sys
def lines(p):
    b = open(p, 'rb').read().split(b'\n')
    return [l for l in b if not l.startswith('★係数の上書き'.encode('utf-8'))]
a, b = lines(sys.argv[1]), lines(sys.argv[2])
diff = [(i, x, y) for i, (x, y) in enumerate(zip(a, b)) if x != y]
print(f'行数 {len(a)} 対 {len(b)}・違う行 {len(diff)}' + ('' if len(a) == len(b) else '・行数が違う'))
for i, x, y in diff[:15]:
    print(f'  {i+1}: 新 {x.decode("utf-8","replace")[:160]}\n      基 {y.decode("utf-8","replace")[:160]}')
print('全行一致' if not diff and len(a) == len(b) else '不一致')
