# 読むだけ (1169)：S02 の読み直す行（10・11・13・14・16・17）の頻度を、エンジンの閾値の式で数える。
#  S02-10：Model.StockpileFactor の満点＝WarScareTension ≧ RivalTensionThreshold 40 ＋ StockpileTensionSpan 60 ＝100。
#          warpair_s の monthsGt100（組の緊張が 100 を超えた月）を国ごとに組の最大で取る（年末の四半期・labor_s の生きた国）＝下限（warpair は 100 以上か出来事のある組だけを書く）。
#  S02-13/14：pxgoods_s（1783〜1794 だけ）の産出 0・在庫 0・需要＞0 の行と、参照×物価水準に対する比の最多。
#  S02-16：fiscal_s の植民地の維持の請求（claimColonialUpkeepPerMonth）＞0 の国×月と、請求の合計に対する割合の中央。
#  S02-17：labor_s 年末の行の工場労働の失業・実質の値段・農業労働の実質の値段・最低賃金法の段。
# 使い方: python -X utf8 -I s02_freq.py <生のフォルダ> <種の数>  （計器が書かない表は〈表なし〉と印字する＝0 と数えない）
import sys, os, csv, collections, statistics
B, NS = sys.argv[1], int(sys.argv[2])
def rd(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8', newline='') as f: return list(csv.DictReader(f, delimiter='\t'))
def era(y): return '1783-1849' if y < 1850 else '1850-1913' if y < 1914 else '1914-1950'
# ---- S02-10 ----
tot = collections.Counter(); hit = collections.Counter(); full3 = 0; nq = 0; miss = 0
for s in range(1, NS + 1):
    lab, wp = rd('labor', s), rd('warpair', s)
    if lab is None or wp is None: miss += 1; continue
    qy = {}
    for r in lab: qy[int(r['quarter'])] = int(r['year'])
    lastq = {}
    for q, y in qy.items(): lastq[y] = max(lastq.get(y, -1), q)
    endq = set(lastq.values())
    mx = collections.defaultdict(int)
    for r in wp:
        q = int(r['quarter'])
        if q not in endq: continue
        m = int(float(r['monthsGt100']))
        for n in (r['nationA'], r['nationB']):
            if m > mx[(q, n)]: mx[(q, n)] = m
    for r in lab:
        q = int(r['quarter'])
        if q not in endq: continue
        e = era(int(r['year'])); m = mx.get((q, r['nation']), 0)
        tot[e] += 3; hit[e] += m; nq += 1; full3 += (m == 3)
T, H = sum(tot.values()), sum(hit.values())
print(f'S02-10 種 {NS - miss}/{NS}・年末の国×四半期 {nq}・満点（緊張≧100）の国×月 {H}/{T} = {H / T:.3%}（下限）・3 か月とも {full3 / nq:.1%}・時代別 ' + ' '.join(f'{e}:{hit[e] / tot[e]:.1%}' for e in sorted(tot)) + f'・S02-11 の一致（満点でない）は {1 - H / T:.1%} 以下')
# ---- S02-13/14 ----
cnt = 0; modes = collections.Counter(); goods = collections.Counter(); rows = 0; miss = 0
for s in range(1, min(NS, 3) + 1):
    px = rd('pxgoods', s)
    if px is None: miss += 1; continue
    for r in px:
        rows += 1
        if float(r['lastProducedQty']) == 0.0 and float(r['stockQty']) == 0.0 and float(r['lastDemandQty']) > 0:
            cnt += 1; goods[r['goodName']] += 1; modes[round(float(r['priceOverRefTimesLevelMedianRatio']), 2)] += 1
if rows:
    print(f'S02-14 種 1〜{min(NS, 3)}（表なし {miss}）・pxgoods の行 {rows}・産出 0 在庫 0 需要＞0 の行 {cnt}・比の最多 ' + ' '.join(f'{k}:{v}' for k, v in modes.most_common(4)) + '・財 ' + ' '.join(f'{k}:{v}' for k, v in goods.most_common(8)))
else: print('S02-14 表なし')
# ---- S02-16 ----
nm = 0; pos = 0; shares = []; miss = 0; nat = collections.Counter()
for s in range(1, NS + 1):
    fi = rd('fiscal', s)
    if fi is None: miss += 1; continue
    for r in fi:
        nm += 1; u = float(r['claimColonialUpkeepPerMonth']); t = float(r['claimTotalPerMonth'])
        if u > 0:
            pos += 1; nat[r['nation']] += 1
            if t > 0: shares.append(u / t)
print(f'S02-16 種 {NS - miss}/{NS}・fiscal の国×月 {nm}・植民地の維持の請求＞0 {pos} = {pos / max(nm, 1):.1%}・請求の合計に対する割合の中央 {statistics.median(shares) if shares else float("nan"):.2%}・国 ' + ' '.join(f'{k}:{v}' for k, v in nat.most_common(9)))
# ---- S02-17 ----
n = 0; u5 = u20 = 0; rp = []; farm_lo = 0; nf = 0; law = collections.Counter(); over2 = 0; miss = 0
for s in range(1, NS + 1):
    lab = rd('labor', s)
    if lab is None: miss += 1; continue
    qy = {}
    for r in lab: qy[int(r['quarter'])] = int(r['year'])
    lastq = {}
    for q, y in qy.items(): lastq[y] = max(lastq.get(y, -1), q)
    endq = set(lastq.values())
    for r in lab:
        if int(r['quarter']) not in endq: continue
        n += 1; law[r['lawCode']] += 1
        u = float(r['plainUnemploymentShare']); u5 += u > 0.05; u20 += u > 0.20
        x = float(r['plainRealPriceIndex']); rp.append(x); over2 += x > 2.0
        fx = float(r['farmRealPriceIndex'])
        if fx == fx: nf += 1; farm_lo += fx < 0.5
print(f'S02-17 種 {NS - miss}/{NS}・年末の国×四半期 {n}・工場労働の失業 5% 超 {u5 / n:.1%}・20% 超 {u20 / n:.1%}・工場労働の実質の値段÷参照の中央 {statistics.median(rp):.2f}・2 倍超 {over2 / n:.2%}・農業労働 0.5 倍未満 {farm_lo / max(nf, 1):.1%}・最低賃金法の段 ' + ' '.join(f'{k}:{v / n:.1%}' for k, v in sorted(law.items())))
