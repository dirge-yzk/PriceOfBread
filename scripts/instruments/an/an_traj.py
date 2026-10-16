# (372) 合流：西・普・サルデーニャ＝ピエモンテ・墺の本国起源の州の数（anchorRegionsN）の軌道を種ごとに腕 A と B で並べる（州を失う→扶養力が落ちる→さらに失う、の正帰還の確かめ）。読むだけ。
import csv, sys
W = sys.argv[1]
NN = ['スペイン', 'プロイセン', 'サルデーニャ＝ピエモンテ', 'オーストリア']
QS = [0, 80, 160, 240, 320, 400, 480, 524, 600, 667]
def rows(a, s):
    d = {}
    with open(f"{W}/out_{a}_10/ins/anchor_s{s}.tsv", encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for ln in r:
            n = ln[ix['nation']]
            if n in NN: d[(n, int(ln[ix['quarter']]))] = (int(ln[ix['anchorRegionsN']]), float(ln[ix['popK']]))
    return d
R = {(a, s): rows(a, s) for a in ('A', 'B', 'N1', 'N2') for s in range(1, 11)}
print('本国起源の州の数（四半期 ' + ' '.join(map(str, QS)) + '）・- は国が無い')
for n in NN:
    print(f'== {n}')
    for s in range(1, 11):
        line = []
        for a in ('A', 'B', 'N1', 'N2'):
            line.append(a + ' ' + ','.join(str(R[(a, s)][(n, q)][0]) if (n, q) in R[(a, s)] else '-' for q in QS))
        print(f'  種{s:2d}  ' + ' | '.join(line))
    for a in ('A', 'B', 'N1', 'N2'):
        lost = sum(1 for s in range(1, 11) if (n, 524) not in R[(a, s)] or R[(a, s)][(n, 524)][0] < R[(a, s)][(n, 0)][0])
        print(f'  {a}: 1914 年までに本国起源の州を 1 つ以上失った（または国が消えた）種 {lost}/10')
