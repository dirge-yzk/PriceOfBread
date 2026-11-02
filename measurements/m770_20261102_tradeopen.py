import sys, os, statistics as st, collections
nats = ['イギリス','フランス','プロイセン','ロシア','ペルシャ','スペイン','清']
def load(d, seeds):
    res = {n: [] for n in nats}
    for sd in seeds:
        p = os.path.join(d, f'nation_s{sd}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            acc = collections.defaultdict(list)
            for ln in fh:
                c = ln.rstrip('\n').split('\t')
                if len(c) < len(h): continue
                try: y = int(c[ix['year']])
                except: continue
                if not (1904 <= y <= 1913): continue
                n = c[ix['nation']]
                if n not in res: continue
                try:
                    g = float(c[ix['nominalGdpPerYear']]); e = float(c[ix['exportsPerClearing']]); i = float(c[ix['importsPerClearing']]); k = float(c[ix['clearingsPerYear']])
                except ValueError: continue
                if g > 0: acc[n].append((e + i) * k / g)
            for n, v in acc.items():
                if v: res[n].append(st.mean(v))
    return res
arms = [(a.split('=')[0], a.split('=')[1]) for a in sys.argv[1:]]
print('貿易/GDP（(輸出＋輸入)×清算/年÷名目GDP）1904-13 の種の中央（括弧＝種の範囲）')
print('arm\t' + '\t'.join(nats))
for name, spec in arms:
    d, a, b = spec.rsplit(':', 2)
    r = load(d, range(int(a), int(b) + 1))
    print(name + '\t' + '\t'.join(f'{st.median(r[n]):.4f}({min(r[n]):.3f}-{max(r[n]):.3f})' if r[n] else '-' for n in nats))
