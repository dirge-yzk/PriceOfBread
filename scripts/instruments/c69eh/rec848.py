# (848) c69eh 記録の先頭の読み（読むだけ・事前登録 §8 の①〜③）：cearn_s を格ごとに足す（輸入の代金・外資の出・工場と採取の産出の値・州の在庫・inFromPops・outOther）と、経路の列に EmptyPopEstate.cs が出た行の数。
#   python -I scripts/instruments/c69eh/rec848.py <出力> <名:生のフォルダ> ...
import sys, os
from collections import defaultdict
out = sys.argv[1]; L = []
def P(s=''): L.append(s); print(s, flush=True)
cols = ['outImportPerQuarter', 'outCapitalPerQuarter', 'factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth', 'savings', 'inFromPopsPerQuarter', 'outOtherPerQuarter', 'inMarketPerQuarter']
P('(848) c69eh 記録：cearn_s の格ごとの和（10 種の和・全ての四半期）と種ごとの値')
P('name\ttier\t' + '\t'.join(cols) + '\troutesWithEstate')
for arg in sys.argv[2:]:
    name, d = arg.split(':', 1)
    tot = defaultdict(lambda: [0.0] * len(cols)); rt = defaultdict(int)
    for s in range(1, 11):
        fn = os.path.join(d, f'cearn_s{s}.tsv')
        if not os.path.exists(fn): continue
        with open(fn, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = [h.index(c) for c in cols]; it = h.index('tier')
            ir = [h.index(c) for c in ('outTop1Route', 'outTop2Route', 'inTop1Route')]
            for line in f:
                x = line.rstrip('\n').split('\t'); t = tot[x[it]]
                for k, i in enumerate(ix):
                    v = float(x[i])
                    if v == v: t[k] += v
                if any('EmptyPopEstate.cs' in x[i] for i in ir): rt[x[it]] += 1
    for k in sorted(tot): P(f'{name}\t{k}\t' + '\t'.join(f'{v:.6g}' for v in tot[k]) + f'\t{rt[k]}')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
