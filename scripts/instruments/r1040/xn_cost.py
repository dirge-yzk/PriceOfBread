# (1040) c98xn 設計の材料の台本（読むだけ・エンジンの関数を呼ばない・既にある生だけ）
# 問い3：行の門 :885（xNeed≦0）を外したとき、走査に入る行の増えと .cost の秒を、keep_1035/base_ins の
#   csc_s（走査した行・充填を種類ごと・年ごと）・cscx_s（走査した行の充填と下界で飛ばした売り手）・tradeskip_s（調べる年の
#   第4四半期の最初の清算＝行の段の内訳）から見積もる。
# 使い方: python xn_cost.py <keep の base_ins> <出力 tsv>
# ⚠限界（自己申告）
#   ・tradeskip_s は調べる年（1790〜1950 の 18 年）の第4四半期の最初の清算だけ＝他の清算の〈需要なし〉の割合は分からない。
#     調べる年の間の年は、手前の調べる年の割合を使う（段の関数）。
#   ・rowNoNeed は :885（xNeed≦0）・:886（rpT≦1e-9）・:887（需要0）の 3 つの門の和＝:886・:887 は案 B でも閉じたまま＝上界。
#   ・新しく開く行の 1 行あたりの充填は分からない＝(上) 走査した行の平均・(下) 下界で飛ばす 1 件だけ、の 2 つで挟む。
#   ・計器の和は 1 試合の .cost の件数（助走を含む）と合わない場合がある＝比を出して名乗る。
import sys, os
from collections import defaultdict

KEEP, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11)
U_FILL, U_LB = 0.08490, 0.02000   # µs（measurements/b10_20261111_m1035z_ins.cost の単価・清算：着値の充填／着値の下界で飛ばした売り手）
COST_FILLS, COST_LB, COST_ROWS = 250629182, 217149772, 68126487   # 同じ .cost の 1 試合の件数（助走を含む）

def rd(p):
    with open(p, encoding='utf-8') as f:
        H = f.readline().rstrip('\n').split('\t')
        for ln in f:
            yield H, ln.rstrip('\n').split('\t')

out = []
agg = defaultdict(list)
for s in SEEDS:
    # tradeskip：調べる年 × 世界の和（nation='ALL' の行を除く）
    ts = defaultdict(lambda: defaultdict(float))
    for H, v in rd(os.path.join(KEEP, 'tradeskip_s%d.tsv' % s)):
        if v[1] == 'ALL': continue
        y = int(v[0])
        for k in ('rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan'):
            ts[y][k] += float(v[H.index(k)])
    ty = sorted(ts)
    def kOf(y):
        c = [t for t in ty if t <= y]
        t = c[-1] if c else ty[0]
        d = ts[t]
        return d['rowNoNeed'] / d['rowScan'] if d['rowScan'] > 0 else 0.0
    # csc：買い手（roleCode 0）・種類ごと・年ごと
    first = defaultdict(float); allr = defaultdict(float); fills = 0.0; rowsScan = 0.0
    for H, v in rd(os.path.join(KEEP, 'csc_s%d.tsv' % s)):
        if v[2] != '0': continue
        y, cl = int(v[0]), v[3]
        r = float(v[H.index('rowsCount')])
        if cl in ('0', '1'): first[y] += r
        if cl != '4': allr[y] += r; rowsScan += r; fills += float(v[H.index('fillsCount')])
    # cscx：走査した行の充填と下界で飛ばした売り手
    xr = xf = xlb = 0.0
    for H, v in rd(os.path.join(KEEP, 'cscx_s%d.tsv' % s)):
        xr += float(v[H.index('rowsCount')]); xf += float(v[H.index('fillsCount')]); xlb += float(v[H.index('fillsSkippedLB')])
    fpr, lpr = xf / xr, xlb / xr
    dFirst = sum(kOf(y) * first[y] for y in first)
    dAll = sum(kOf(y) * allr[y] for y in allr)
    scale = COST_FILLS / xf   # 計器の和 → .cost の 1 試合の件数（助走ほか）
    hi = dAll * scale * (fpr * U_FILL + lpr * U_LB) / 1e6
    mid = dFirst * scale * (fpr * U_FILL + lpr * U_LB) / 1e6
    lo = dFirst * scale * (U_LB) / 1e6
    rec = dict(seed=s, scanRows=rowsScan, firstRows=sum(first.values()), cscxRows=xr, fillsPerRow=fpr, lbPerRow=lpr,
               scale=scale, scaleLB=COST_LB / xlb, k1870=kOf(1870), k1913=kOf(1913),
               dRowsFirst=dFirst, dRowsAll=dAll, secLo=lo, secMid=mid, secHi=hi)
    out.append(rec)
    for k, v in rec.items(): agg[k].append(v)

keys = list(out[0].keys())
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\t'.join(keys) + '\n')
    for r in out: f.write('\t'.join(('%d' % r[k]) if k == 'seed' else ('%.6g' % r[k]) for k in keys) + '\n')
    med = lambda L: sorted(L)[len(L) // 2] if len(L) % 2 else 0.5 * (sorted(L)[len(L) // 2 - 1] + sorted(L)[len(L) // 2])
    f.write('median\t' + '\t'.join('%.6g' % med(agg[k]) for k in keys[1:]) + '\n')
    f.write('min\t' + '\t'.join('%.6g' % min(agg[k]) for k in keys[1:]) + '\n')
    f.write('max\t' + '\t'.join('%.6g' % max(agg[k]) for k in keys[1:]) + '\n')
print('ok', OUT)
