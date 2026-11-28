# (1437) 問い3 の国の層：tradeskip_s（買い手の国×財×年）の 線路・機関車 の行の止まり方の和（読むだけ・州の層は数えられない＝tsreg_s は主食の財だけ）
# 出力：標準出力（<out>/<tag>_q5_tskip.txt へ）
import sys, csv, collections
raw, tag = sys.argv[1], sys.argv[2]
K = ['rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan', 'rowDealtCross', 'rowDealtDomOnly', 'rowNoDeal', 'xPairs', 'xSoldOut', 'xCap', 'xWedge', 'xPass', 'domPairs', 'sellerNoStock', 'goodNoSellers']
A = collections.defaultdict(lambda: collections.defaultdict(float))
for s in range(1, 11):
    with open(f"{raw}/tradeskip_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            g = row[ix['good']]
            if g not in ('線路', '機関車', '小麦粉'): continue
            y = int(row[ix['year']]); band = '1850-1899' if 1850 <= y < 1900 else ('1900-1950' if y >= 1900 else None)
            if band is None: continue
            a = A[(g, band)]
            for k in K: a[k] += float(row[ix[k]] or 0)
print("財\t期\t" + "\t".join(K))
for (g, b), a in sorted(A.items()): print(f"{g}\t{b}\t" + "\t".join(f"{a[k]:.0f}" for k in K))
print("割合：行の門（rowIdle・rowNoNeed・rowBudget0・rowScan）÷rows と、組の門（xSoldOut・xCap・xWedge・xPass）÷xPairs")
for (g, b), a in sorted(A.items()):
    r = a['rows'] or 1; p = a['xPairs'] or 1
    print(f"{g}\t{b}\tidle {a['rowIdle'] / r:.2f}・noNeed {a['rowNoNeed'] / r:.2f}・budget0 {a['rowBudget0'] / r:.2f}・scan {a['rowScan'] / r:.2f}（成約 越境 {a['rowDealtCross'] / r:.2f}・国内だけ {a['rowDealtDomOnly'] / r:.2f}・成約なし {a['rowNoDeal'] / r:.2f}）｜組 soldOut {a['xSoldOut'] / p:.2f}・cap {a['xCap'] / p:.2f}・wedge {a['xWedge'] / p:.2f}・pass {a['xPass'] / p:.2f}・国内の組 {a['domPairs'] / p:.2f}")
