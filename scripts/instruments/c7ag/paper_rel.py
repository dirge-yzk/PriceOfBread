# (775) 紙の検算 2：m773 の生の metal_s の lnAgAuMean（＝旗 1 が注入する rel の対数・四半期ごと）から、旗 1 の一次近似を読む。読むだけ。
#   python -I scripts/instruments/c7ag/paper_rel.py <生のフォルダ> [種の数]
import sys, math, statistics
sys.stdout.reconfigure(encoding='utf-8', newline=chr(10))
raw = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def ols(x, y):
    mx = sum(x)/len(x); my = sum(y)/len(y)
    sxx = sum((a-mx)**2 for a in x)
    return sum((a-mx)*(b-my) for a, b in zip(x, y))/sxx if sxx > 0 else float('nan')
allx = []; ally = {}; ks = [0.2, 0.2/3]
print('seed\tquarters\trel<=0.31\trel>=2.99\trelMin\trelMax\tsd(ln rel)\tsd(dln rel)\tslopeSmoothed(k=0.2/q)\tslopeSmoothed(k=0.2/m)')
tot_out = 0
for s in range(1, n+1):
    ln = []; nr = []
    for l in open(f'{raw}/metal_s{s}.tsv', encoding='utf-8').read().split('\n')[1:]:
        p = l.split('\t')
        if len(p) < 13: continue
        try: v = float(p[11])
        except: continue
        if v != v: continue
        ln.append(v); nr.append(int(p[12]))
    rel = [math.exp(v) for v in ln]
    lo = sum(1 for r in rel if r <= 0.31); hi = sum(1 for r in rel if r >= 2.99)
    d = [ln[i]-ln[i-1] for i in range(1, len(ln))]
    sl = []
    for k, per in ((0.2, 1), (0.2, 3)):
        e = 1.0; es = []
        for r in rel:
            for _ in range(per):
                e += k*(r-e)
            es.append(math.log(e))
        sl.append(ols(ln, es))
    tot_out += lo+hi
    print(f'{s}\t{len(ln)}\t{lo}\t{hi}\t{min(rel):.3f}\t{max(rel):.3f}\t{statistics.pstdev(ln):.4f}\t{statistics.pstdev(d):.4f}\t{sl[0]:.3f}\t{sl[1]:.3f}')
    allx += ln
print('rel の対数の全期間・10 種の範囲：', f'{min(allx):.3f} .. {max(allx):.3f}', ' 挟みの外の四半期の合計:', tot_out)
