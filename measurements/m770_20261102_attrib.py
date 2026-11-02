import sys, os, collections, statistics as st
d = sys.argv[1]; seeds = range(1, 11) if len(sys.argv) < 3 else range(int(sys.argv[2]), int(sys.argv[3]) + 1)
num = lambda x: float('inf') if x == 'inf' else float(x)
byNat = collections.defaultdict(lambda: [0, 0, 0.0, 0.0, 0]); byReg = collections.defaultdict(lambda: [0, 0.0, 0.0])
for sd in seeds:
    p = os.path.join(d, f'portch_s{sd}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(h): continue
            lo, ln_ = num(c[ix['landKmOld']]), num(c[ix['landKmNew']])
            f = ln_ / lo if lo > 0 else 1.0
            a = byNat[c[ix['nation']]]; a[0] += 1; a[1] += (f < 0.99); a[2] += f; a[3] += (1 - f); a[4] += (c[ix['portUsed']] != c[ix['portGc']])
            if f < 0.99:
                r = byReg[(c[ix['nation']], c[ix['region']], c[ix['portGc']], c[ix['portUsed']])]; r[0] += 1; r[1] += f; r[2] += lo - ln_
print('国別（行＝州×年×種）：行数 / landKmNew÷landKmOld<0.99 の行の割合 / 平均の比 / 港が変わった行の割合')
for n, a in sorted(byNat.items(), key=lambda kv: -kv[1][3] / max(1, kv[1][0]))[:12]:
    print(f'  {n}\t{a[0]}\t{a[1] / a[0]:.3f}\t{a[2] / a[0]:.4f}\t{a[4] / a[0]:.3f}')
print('州×港の組（比<0.99 の行が多い順）：国 州 旧港→使った港  行数 / 平均の比 / 平均の短縮km')
for k, r in sorted(byReg.items(), key=lambda kv: -kv[1][0])[:14]:
    print(f'  {k[0]} {k[1]} {k[2]}→{k[3]}\t{r[0]}\t{r[1] / r[0]:.3f}\t{r[2] / r[0]:.0f}')
