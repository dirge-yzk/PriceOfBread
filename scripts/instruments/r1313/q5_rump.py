# (1313) 入口 E2（領土を失った残りの国が国庫を丸ごと持つ）の頻度：人口が 1 四半期で 1/10 未満へ落ちた国×四半期（人口 100 千人以上の後）と、
# その前後の国庫・民間の貨幣、その後 40 四半期のうち民間÷国庫 < 1/1000 の四半期の数。読むだけ。
# 使い方：python -X utf8 -I q5_rump.py <出力.tsv> <種 a-b> 名前=<ins> ...
import sys, csv, collections
out = sys.argv[1]; a, b = map(int, sys.argv[2].split('-')); arms = [x.split('=', 1) for x in sys.argv[3:]]
def fl(x):
    try: return float(x)
    except: return float('nan')
w = open(out, 'w', encoding='utf-8', newline='\n')
w.write('arm\tseed\tnation\tq\tyear\tpopBefore\tpopAfter\tT_before\tT_after\tpriv_before\tpriv_after\tT_over_priv_after\tstuck40\n')
for name, d in arms:
    for s in range(a, b + 1):
        pop = collections.defaultdict(dict)
        with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for row in r: pop[row[ix['nation']]][int(row[ix['quarter']])] = fl(row[ix['totalPopulationK']])
        T = collections.defaultdict(dict); PM = collections.defaultdict(dict)
        with open(f'{d}/treasledger_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); next(r)
            for q, y, n, it, v in r:
                if it == '_treasuryEnd': T[n][int(q)] = fl(v)
                elif it == '_privateMoney': PM[n][int(q)] = fl(v)
        for n, P in pop.items():
            for q in sorted(P):
                p0 = P.get(q - 1); p1 = P[q]
                if p0 is None or not (p0 >= 100) or not (p1 > 0) or p1 / p0 >= 0.1: continue
                t0 = T[n].get(q - 1, float('nan')); t1 = T[n].get(q, float('nan')); m0 = PM[n].get(q - 1, float('nan')); m1 = PM[n].get(q, float('nan'))
                st = sum(1 for k in range(q, q + 40) if k in T[n] and T[n][k] > 0 and PM[n].get(k, float('inf')) < T[n][k] / 1000)
                w.write(f'{name}\t{s}\t{n}\t{q}\t\t{p0:.6g}\t{p1:.6g}\t{t0:.6g}\t{t1:.6g}\t{m0:.6g}\t{m1:.6g}\t{(t1 / m1 if m1 > 0 else float("inf")):.4g}\t{st}\n')
w.close()
