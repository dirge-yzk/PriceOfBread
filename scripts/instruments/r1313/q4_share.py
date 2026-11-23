# (1313) c136ak 問い 4 の材料：途中で生まれた国ごとに、親（同じ四半期に OtherOut が生まれの国庫以上に出た国のうち差が最小）の国庫と民間の貨幣を
# 前の四半期の末で読み、分与を〈人口比〉（実際）と〈民間の貨幣の比〉（案 A の近似）で並べる。近似＝親の値は前の四半期の末・子の民間の貨幣は生まれの四半期の末〈推〉。読むだけ。
# 使い方：python -X utf8 -I q4_share.py <出力.tsv> <種 a-b> 名前=<ins> ...
import sys, csv, collections
out = sys.argv[1]; a, b = map(int, sys.argv[2].split('-')); arms = [x.split('=', 1) for x in sys.argv[3:]]
def fl(x):
    try: return float(x)
    except: return float('nan')
w = open(out, 'w', encoding='utf-8', newline='\n')
w.write('arm\tseed\tnation\tbornQ\tbornT\tpriv0\tparent\tparentT_prev\tparentPriv_prev\tpopShareImplied\tT_money\tT_money_over_T\tpriv0_over_T_money\n')
for name, d in arms:
    for s in range(a, b + 1):
        T = {}; PM = {}; OO = collections.defaultdict(float); births = []
        seen = collections.Counter()
        with open(f'{d}/treasledger_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); next(r)
            for q, y, n, it, v in r:
                q = int(q); v = fl(v)
                if it == '_treasuryEnd': T[(q, n)] = v
                elif it == '_privateMoney': PM[(q, n)] = v
                elif it == 'OtherOut': OO[(q, n)] += v
                elif it == '_bornTreasury' and q > 0:
                    births.append((q, n, v, seen[n])); seen[n] += 1
        byq = collections.defaultdict(list)
        for (q, n), v in OO.items(): byq[q].append((n, v))
        for q0, n, bt, k in births:
            pm0 = PM.get((q0, n), float('nan'))
            cand = [(m, v) for m, v in byq.get(q0, []) if m != n and -v >= bt * 0.999]
            par = min(cand, key=lambda x: -x[1] - bt)[0] if cand and bt > 0 else ''
            pt = T.get((q0 - 1, par), float('nan')); pp = PM.get((q0 - 1, par), float('nan'))
            sh = bt / pt if pt and pt > 0 else float('nan')
            tm = pt * pm0 / pp if pp and pp > 0 else float('nan')
            w.write(f'{name}\t{s}\t{n}#{k}\t{q0}\t{bt:.6g}\t{pm0:.6g}\t{par}\t{pt:.6g}\t{pp:.6g}\t{sh:.4g}\t{tm:.6g}\t{(tm / bt if bt > 0 else float("nan")):.4g}\t{(pm0 / tm if tm > 0 else float("nan")):.4g}\n')
w.close()
