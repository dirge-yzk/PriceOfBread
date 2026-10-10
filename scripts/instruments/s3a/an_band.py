"""S3a の読み手4：腕ごとの補給崩壊の物差し（旧＝国×四半期の数・千件級）と、人口で重み付けた物差し（充足<0.3 の国の人口÷世界の人口の四半期平均）を並べる。
  python an_band.py <出力ディレクトリ> [...]"""
import sys, os, collections
NC = ['q','yr','idx','base','name','nreg','pop','fill','food','daily']
def fl(x):
    try: return float(x)
    except: return float('nan')
def era(yr): return 0 if yr <= 1860 else 1 if yr <= 1900 else 2 if yr <= 1913 else 3 if yr <= 1929 else 4
for d in sys.argv[1:]:
    print(f'##### {d}')
    files = sorted([f for f in os.listdir(d) if f.startswith('s') and f.endswith('.txt')], key=lambda f: int(f[1:-4]))
    big1000 = 0; persist = 0; tot = 0; wsum = 0; wq = 0; ghostTot = 0; realTot = 0
    for f in files:
        sd = int(f[1:-4]); eras = [0] * 5; lowPop = collections.defaultdict(float); allPop = collections.defaultdict(float); n_low = 0; n_ghost = 0; n_real = 0; realNat = collections.Counter(); maxN = 0
        perQ = collections.Counter()
        for line in open(os.path.join(d, f), encoding='utf-8'):
            if line.startswith('W\t'):
                p = line.split('\t'); maxN = max(maxN, int(p[3])); continue
            if not line.startswith('N\t'): continue
            p = line.rstrip('\n').split('\t')[1:]
            if len(p) < 10: continue
            n = dict(zip(NC, p)); q = int(n['q']); pop = fl(n['pop'])
            allPop[q] += pop
            if not (fl(n['fill']) >= 0.3):
                n_low += 1; eras[era(int(n['yr']))] += 1; lowPop[q] += pop
                if pop < 100: n_ghost += 1
                else: n_real += 1; realNat[n['name']] += 1
        share = sum(lowPop[q] / allPop[q] for q in allPop if allPop[q] > 0) / max(1, len(allPop))
        tot += n_low; ghostTot += n_ghost; realTot += n_real; wsum += share; wq += 1
        if n_low >= 1000: big1000 += 1
        long = n_low < 1000 and sum(1 for e in eras[1:] if e > 50) >= 3
        if long: persist += 1
        print(f's{sd}: 国×四半期 {n_low}（{"／".join(map(str, eras))}）人口<100 {n_ghost}・人口≥100 {n_real}  国の最大 {maxN}  充足<0.3 の人口の割合（四半期平均）{share*100:.3f}%  ' + ('千件級' if n_low >= 1000 else '持続' if long else '') + (f'  人口≥100 の主な国 {realNat.most_common(4)}' if n_real else ''))
    print(f'合計：国×四半期 {tot}（人口<100 {ghostTot}・人口≥100 {realTot}）  千件級 {big1000}/10  持続（千件未満・1861年以後の時代に50超が3つ以上）{persist}  人口の割合の平均 {wsum/max(1,wq)*100:.3f}%')
