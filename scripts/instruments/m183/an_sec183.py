"""(183) 安全保障の不安（徴兵法の政治と軍事費の段が読む）のうち海を越えて来る分＝海軍が運ぶ分の読み手（読むだけ）。python an_sec183.py <out/sec183>
10年の窓・種ごとの窓の平均→種の中央値：不安・海を越えて来る分・その割合・自国の養える軍に対する不安の割合（SecurityShare＝不安÷(不安＋自国)）。"""
import sys, glob, os, statistics as st
D = sys.argv[1]
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '日本', '清']
WIN = [(1784, 1793), (1815, 1824), (1855, 1864), (1880, 1889), (1904, 1913), (1940, 1949)]
S = {}; seeds = []; COLS = None
for f in sorted(glob.glob(os.path.join(D, 's*.txt'))):
    sd = int(os.path.basename(f)[1:-4]); seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] == '#S': COLS = t[1:]
        elif t[0] == 'S':
            d = dict(zip(COLS, t[1:])); S[(sd, int(d['yr']), d['nation'])] = {k: float(v) for k, v in d.items() if k not in ('nation',)}
def wm(s, n, k, a, b):
    xs = [S[(s, y, n)][k] for y in range(a, b + 1) if (s, y, n) in S]
    return sum(xs) / len(xs) if xs else float('nan')
def med(xs):
    xs = [x for x in xs if x == x]; return st.median(xs) if xs else float('nan')
print(f'######## {D}（種 {len(seeds)}）安全保障の不安のうち海を越えて来る分（海軍が運ぶ分）＝海越え÷不安［％］／不安の割合（不安÷(不安＋自国の養える軍)）')
print('国'.ljust(8) + ''.join(f'{a}-{b % 100:02d}'.rjust(16) for a, b in WIN))
for n in NAT:
    cells = []
    for a, b in WIN:
        sh = med([wm(s, n, 'seaCross', a, b) / wm(s, n, 'threat', a, b) if wm(s, n, 'threat', a, b) > 0 else float('nan') for s in seeds])
        ss = med([wm(s, n, 'threat', a, b) / (wm(s, n, 'threat', a, b) + wm(s, n, 'own', a, b)) if wm(s, n, 'threat', a, b) + wm(s, n, 'own', a, b) > 0 else float('nan') for s in seeds])
        cells.append(f'{100 * sh:6.1f}%/{ss:6.3f}'.rjust(16))
    print(n.ljust(8) + ''.join(cells))
