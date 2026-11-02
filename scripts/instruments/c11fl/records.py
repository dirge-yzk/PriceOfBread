# (801) c11fl：記録（門にしない）。divhost_s と nation_s から、世界の 配当の払い÷年 GDP・外資の負債÷年 GDP・未払いの割合・英仏墺普の投資家の工業レントと配当の払い（10 年ごと）を出す。
#   使い方: python -I -X utf8 records.py <ins フォルダ> <種の数>
#   世界＝divhost_s の全ての国（金建て＝×fxRate）。年 GDP＝nation_s の nominalGdpPerYear（その四半期の値）×fxRate。四半期の払いは ×4 で年に直す。種の平均。
#   比は 10 年の中の国×四半期の和どうし（Σ払い×4 ÷ Σ年GDP）。英仏墺普の列は 10 年の四半期の平均（年換算・金建て・4 国の和）。
#   ⚠限界：未払いの割合は金建ての Σ(請求−払い)÷Σ請求（計器 divReq は各国の通貨→×fxRate）。(794) の 0.63・13.6・0.23 は m784 の盤面の値で分母の取り方が違う（比べない）。
import sys, csv, os, collections
d = sys.argv[1]; N = int(sys.argv[2])
dec = collections.defaultdict(lambda: collections.defaultdict(float)); qs = collections.defaultdict(set)
big4 = {'イギリス', 'フランス', 'オーストリア', 'プロイセン'}
for s in range(1, N + 1):
    gdp = {}
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for x in r:
            try: gdp[(x[ix['quarter']], x[ix['nation']])] = float(x[ix['nominalGdpPerYear']]) * float(x[ix['fxRate']])
            except ValueError: pass
    last = {}
    with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); jx = {c: i for i, c in enumerate(h)}
        for x in r: last[(x[jx['quarter']], x[jx['nation']])] = x
    for (q, nat), x in last.items():
        if int(float(x[jx['rentMonths']])) != 3: continue
        e = float(x[jx['fxRate']]); y = int(x[jx['year']]); dd = y // 10 * 10
        g = gdp.get((q, nat))
        if g is None: continue
        a = dec[dd]; qs[dd].add(q)
        a['gdp'] += g / N; a['paid'] += 4 * float(x[jx['paidIndex']]) * e / N; a['fci'] += float(x[jx['fci']]) * e / N
        a['req'] += float(x[jx['divReqIndex']]) * e / N; a['paidq'] += float(x[jx['paidIndex']]) * e / N
        if nat in big4:
            a['rent_big4'] += 3 * float(x[jx['rentMonth3']]) * 4 * e / N; a['paid_big4'] += 4 * float(x[jx['paidIndex']]) * e / N
print('dec\t世界の配当の払い(年換算)÷年GDP\t外資の負債÷年GDP\t未払いの割合\t英仏墺普の工業レント(年換算・四半期の平均)\t英仏墺普の払い(年換算・四半期の平均)')
for k in sorted(dec):
    a = dec[k]; nq = len(qs[k])
    if nq <= 0 or a['gdp'] <= 0: continue
    print(f'{k}\t{a["paid"] / a["gdp"]:.4f}\t{a["fci"] / a["gdp"]:.4f}\t{(1 - a["paidq"] / a["req"]) if a["req"] > 0 else float("nan"):.4f}\t{a["rent_big4"] / nq:.5g}\t{a["paid_big4"] / nq:.5g}')
