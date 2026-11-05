# (848) c69eh 同じ盤面の新÷旧（教訓③・読むだけ）：cearn_s の同じ（種・四半期・国・州）の行どうしで、腕と基線の列の比を格ごとに数える。
#   python -I scripts/instruments/c69eh/ratio848.py <出力> <腕の ins> <基線の ins> [<ヌル腕の ins> ...]
#   列ごとに：和の比（Σ腕÷Σ基線・両方の行がある組だけ）と、両方が正の組の比の中央。ヌル腕を渡すと同じ数を〈ヌル腕÷基線〉でも出す（幅）。
import sys, os, statistics as st
from collections import defaultdict
out, arm, base = sys.argv[1], sys.argv[2], sys.argv[3]; nulls = sys.argv[4:]
COLS = ['savings', 'outImportPerQuarter', 'outCapitalPerQuarter', 'factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth', 'inMarketPerQuarter', 'inFromPopsPerQuarter', 'outOtherPerQuarter']
def load(d, s):
    fn = os.path.join(d, f'cearn_s{s}.tsv'); m = {}
    if not os.path.exists(fn): return m
    with open(fn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = [h.index(c) for c in COLS]; it = h.index('tier')
        for line in f:
            x = line.rstrip('\n').split('\t')
            m[(x[0], x[2], x[3])] = (x[it], [float(x[i]) for i in ix])
    return m
L = []
def P(s=''): L.append(s); print(s, flush=True)
P('(848) c69eh 同じ盤面の新÷旧（cearn_s・同じ種・四半期・国・州の行どうし・10 種）')
P('which\ttier\tcol\tsumRatio\tmedianRatio\tnPairs')
for name, d in [('armB', arm)] + [(f'null{i+1}', x) for i, x in enumerate(nulls)]:
    sa = defaultdict(float); sb = defaultdict(float); rs = defaultdict(list); n = defaultdict(int)
    for s in range(1, 11):
        A = load(d, s); B = load(base, s)
        for k, (t, va) in A.items():
            b = B.get(k)
            if b is None: continue
            vb = b[1]
            for j, c in enumerate(COLS):
                a1, b1 = va[j], vb[j]
                if a1 == a1 and b1 == b1:
                    sa[(t, c)] += a1; sb[(t, c)] += b1; n[(t, c)] += 1
                    if a1 > 0 and b1 > 0: rs[(t, c)].append(a1 / b1)
    for (t, c) in sorted(sa):
        P(f'{name}\t{t}\t{c}\t{sa[(t, c)] / sb[(t, c)] if sb[(t, c)] else float("nan"):.4g}\t{st.median(rs[(t, c)]) if rs[(t, c)] else float("nan"):.4g}\t{n[(t, c)]}')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
