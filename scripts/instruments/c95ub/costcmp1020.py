# (1020) c95ub：.cost の項ごとの推定（秒）を基準と腕で並べ、差の大きい順に出す（読むだけ）。
# 使い方: python -X utf8 -I costcmp1020.py <基準の .cost> <腕の .cost> [上位の行数=12]
import sys
def read(p):
    d = {}; tot = None
    for line in open(p, encoding='utf-8'):
        if line.startswith('推定の計算量'):
            tot = float(line.split(':', 1)[1].split('秒')[0].strip())
        f = line.rstrip('\n').split('\t')
        if len(f) == 4 and f[0].startswith('  ') and f[0].strip() != '名前':
            try: d[f[0].strip()] = float(f[3])
            except ValueError: pass
    return d, tot
b, tb = read(sys.argv[1]); a, ta = read(sys.argv[2]); n = int(sys.argv[3]) if len(sys.argv) > 3 else 12
print(f'推定の合計 基準 {tb} 秒・腕 {ta} 秒・差 {ta - tb:+.1f} 秒（上限 248.5）')
rows = sorted(((a.get(k, 0.0) - b.get(k, 0.0), k) for k in set(a) | set(b)), key=lambda x: -abs(x[0]))
for dlt, k in rows[:n]: print(f'  {dlt:+7.2f}\t{b.get(k, 0.0):7.2f}→{a.get(k, 0.0):7.2f}\t{k[:60]}')
