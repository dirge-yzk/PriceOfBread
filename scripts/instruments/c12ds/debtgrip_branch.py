# (804) I13：DebtGrip の倒し先（年の名目 GDP が正でないとき 0 を返す枝）に債務が正の国×四半期がどれだけ在るかを数える（読むだけ・記録）。
#   使い方：python -I debtgrip_branch.py 名前=計器の出力フォルダ ...   （nation_s*.tsv の nominalGdpPerYear と govDebtStock を読む）
#   PowerShares.DebtGrip：gdpYear = PerMonthToPerYear(NominalGdpPerMonth)・!(gdpYear>0) なら 0 を返す。表の nominalGdpPerYear は同じ量の年換算。
#   数えるのは ①名目 GDP が正でない（0・負・NaN・欠け）行 ②その中で債務が正の行（＝倒し先が 1 でなく 0 になる行）。
import csv, sys, glob, os, math, collections
sys.stdout.reconfigure(encoding='utf-8')
for arg in sys.argv[1:]:
    k, d = arg.split('=', 1)
    rows = 0; g0 = 0; g0d = 0; who = collections.Counter(); first = {}
    for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
        s = os.path.basename(f)[8:-4]
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iy, inn, ig, idb = h.index('year'), h.index('nation'), h.index('nominalGdpPerYear'), h.index('govDebtStock')
        for row in r:
            rows += 1
            try: g = float(row[ig])
            except ValueError: g = float('nan')
            try: db = float(row[idb])
            except ValueError: db = float('nan')
            if not (g > 0):
                g0 += 1
                if db > 0:
                    g0d += 1; who[(s, row[inn])] += 1; first.setdefault((s, row[inn]), row[iy])
    print(f'{k}: 行 {rows}・名目 GDP が正でない行 {g0}・そのうち債務が正の行 {g0d}', '・'.join(f'種{s} {nm} {c}四半期（{first[(s, nm)]}〜）' for (s, nm), c in who.most_common(8)))
