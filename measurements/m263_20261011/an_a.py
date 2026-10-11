#!/usr/bin/env python
# (263) 線E2 A①：投げ売りの輪・補助の輪と動詞の個体の食い違いの頻度（読むだけ）。nation_s<seed>.tsv を読む。
import sys, glob, os, collections, re
d = sys.argv[1]
files = sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda p: int(re.search(r'nation_s(\d+)', p).group(1)))
tot = collections.Counter()
per_seed = {}
by_good = collections.Counter()
by_nation = collections.Counter()
cells = 0
cell_sets = collections.defaultdict(set)
ex = []
for f in files:
    seed = int(re.search(r'nation_s(\d+)', f).group(1))
    with open(f, encoding='utf-8') as fh:
        hdr = fh.readline().rstrip('\n').split('\t')
        ix = {h: i for i, h in enumerate(hdr)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            def I(n): return int(c[ix[n]])
            nat = c[ix['nation']]; q = int(c[ix['quarter']])
            cells += 1
            for n in ['fireSaleRunCount', 'fireSaleSeenMismatchCount', 'fireSaleFiredCount', 'fireSaleFiredMismatchCount',
                      'fireSaleFiredOverTargetCount', 'subsidyWriteMismatchCount', 'stakeMismatchProducerCount',
                      'stakeMismatchToInvestedCount', 'stakeMismatchToUninvestedCount']:
                v = I(n)
                tot[n] += v
                per_seed.setdefault(seed, collections.Counter())[n] += v
                if v > 0:
                    cell_sets[n].add((seed, nat, q))
            w = c[ix['wheelMismatchGoods']]
            if w:
                for e in w.split(';'):
                    m = re.match(r'(投げ売り|補助):([^(]+)(\((.*)\))?', e)
                    if m:
                        by_good[(m.group(1), m.group(2), m.group(4) or '')] += 1
                        by_nation[(m.group(1), nat)] += 1
                        if len(ex) < 40: ex.append((seed, nat, q, e))
print('国×四半期の行数', cells, '（種', len(files), '）')
for n, v in tot.items():
    print(f'{n:36s} 合計 {v:8d}   国×四半期の数 {len(cell_sets[n]):7d}')
print('\n種ごと（国×四半期の数）')
for s in sorted(per_seed):
    print(s, {n: sum(1 for k in cell_sets[n] if k[0] == s) for n in tot if n in cell_sets or True})
print('\n輪×財×(旧棟数→掴まれる個体の棟数/掴まれる個体の門/pr の門/向き)')
for k, v in by_good.most_common(60):
    print(v, k)
print('\n輪×国')
for k, v in by_nation.most_common(30):
    print(v, k)
print('\n例')
for e in ex: print(e)
