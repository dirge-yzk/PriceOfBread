# (1133) 問い1の集計：onsets_<ラベル>.tsv と series_<ラベル>.tsv から、出どころ別の初出の数・値・時期と、四半期の平均の州の数と取り分の和（種の中央）。
# 使い方: python -X utf8 -I q1_sum.py <out/q1> <ラベル...>
import sys, os, statistics as st
from collections import defaultdict
d = sys.argv[1]; labels = sys.argv[2:]
CLS = ['開幕データ', '開幕の表', '講和の付与', 'エンジン平時', 'エンジン戦時', '?']
out = []
def med(xs): return st.median(xs) if xs else float('nan')
for lb in labels:
    ons = defaultdict(lambda: defaultdict(int)); vals = defaultdict(list); eng = defaultdict(lambda: [0, 0]); per = defaultdict(lambda: defaultdict(int))
    tiers = defaultdict(lambda: defaultdict(int)); ports = defaultdict(lambda: [0, 0])
    with open(os.path.join(d, f'onsets_{lb}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t'); cl = c[ix['class']]; s = int(c[ix['seed']]); q = int(c[ix['quarter']])
            ons[cl][s] += 1; vals[cl].append(float(c[ix['value']]))
            if c[ix['engForceNow']] != '': eng[cl][0] += 1; eng[cl][1] += int(c[ix['engForceNow']])
            y = int(c[ix['year']]); per[cl]['q0' if q == 0 else ('1783-1830' if y < 1831 else '1831-1880' if y < 1881 else '1881-1950')] += 1
            tiers[cl][c[ix['tier']]] += 1; ports[cl][0] += 1; ports[cl][1] += int(c[ix['port']])
    ser = defaultdict(lambda: defaultdict(lambda: [0.0, 0.0, 0.0, 0.0]))   # cls -> seed -> sums over quarters
    nq = defaultdict(set)
    with open(os.path.join(d, f'series_{lb}.tsv'), encoding='utf-8') as f:
        f.readline()
        for ln in f:
            s, q, cl, n, sm, pn, ps = ln.rstrip('\n').split('\t'); s = int(s)
            a = ser[cl][s]; a[0] += int(n); a[1] += float(sm); a[2] += int(pn); a[3] += float(ps); nq[s].add(int(q))
    out.append(f'== {lb}')
    out.append('出どころ\t初出の数(種の中央)\t種ごと\t初出の値の中央\t値の最大\t初出の四半期にエンジンの力あり\t時期(q0/1783-1830/1831-1880/1881-1950・10種の和)\t格(10種の和)\t港の割合\t四半期の平均の州の数(種の中央)\t取り分の和の四半期の平均(種の中央)\t港の州の数の平均')
    tot_n = defaultdict(float); tot_s = defaultdict(float)
    for cl in CLS:
        if cl not in ons and cl not in ser: continue
        cs = [ons[cl].get(s, 0) for s in range(1, 11)]
        v = sorted(vals[cl])
        nqs = {s: max(1, len(nq[s])) for s in range(1, 11)}
        avgn = [ser[cl][s][0] / nqs[s] for s in range(1, 11)]; avgs = [ser[cl][s][1] / nqs[s] for s in range(1, 11)]; avgp = [ser[cl][s][2] / nqs[s] for s in range(1, 11)]
        e = eng[cl]
        out.append('\t'.join([cl, f'{med(cs):g}', ','.join(map(str, cs)), f'{med(v):.4g}' if v else '-', f'{v[-1]:.4g}' if v else '-',
            f'{e[1]}/{e[0]}' if e[0] else '-', '/'.join(str(per[cl][k]) for k in ['q0', '1783-1830', '1831-1880', '1881-1950']),
            ','.join(f'{k}:{n}' for k, n in sorted(tiers[cl].items())), f'{ports[cl][1]}/{ports[cl][0]}' if ports[cl][0] else '-',
            f'{med(avgn):.4g}', f'{med(avgs):.4g}', f'{med(avgp):.4g}']))
    alln = [sum(ser[cl][s][0] for cl in ser) / max(1, len(nq[s])) for s in range(1, 11)]
    alls = [sum(ser[cl][s][1] for cl in ser) / max(1, len(nq[s])) for s in range(1, 11)]
    out.append(f'計\t\t\t\t\t\t\t\t\t{med(alln):.4g}\t{med(alls):.4g}')
open(os.path.join(d, 'q1_summary.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
print('\n'.join(out))
