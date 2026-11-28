# (1437) c154rl 問い1：建設が止まる内陸の州の名簿（読むだけ）
# 入力：<raw>/trrail_s{1..10}.tsv（年×州）。出力：<out>/q1_roster.tsv・q1_pick.txt
# 選び方：waterFlag 0・年 1850〜1950・needEnd>0 の年が 20 年以上・
#   その年の railTrackEnd÷needEnd の中央値が低い・procureStockZeroGoodMonths の和が多い。
#   portFlag 0（trstat の G の行・10 種のどこかで 1 なら港）。
#   州×種ごとに 1 行。名簿＝同じ州が 3 種以上で条件（比の中央値 <0.25・在庫 0 の月が年あたり 12 以上〔財 2 つ×12 か月＝24 が満〕）を満たす州。
import sys, csv, statistics as st, collections
raw, out = sys.argv[1], sys.argv[2]
Y0, Y1 = 1850, 1950
rows = []
port = {}
for s in range(1, 11):
    with open(f"{raw}/trstat_s{s}.tsv", encoding='utf-8') as f:
        for ln in f:
            c = ln.split('	')
            if c[0] == 'G': port[c[3]] = max(port.get(c[3], 0), int(c[11] or 0))
for s in range(1, 11):
    with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        acc = collections.defaultdict(lambda: {'r': [], 'sz': 0.0, 'n': 0, 'want': 0.0, 'got': 0.0, 'natW': 0.0, 'natG': 0.0, 'sav': 0.0, 'nos': 0.0, 'nat': '', 'water': None, 'terr': ''})
        for row in rd:
            y = int(row[ix['year']])
            if y < Y0 or y > Y1: continue
            key = row[ix['region']]
            a = acc[key]; a['nat'] = row[ix['nation']]; a['terr'] = row[ix['terrainCode']]
            wf = row[ix['waterFlag']]; a['water'] = wf
            def g(k):
                try:
                    v = float(row[ix[k]]); return v if v == v else 0.0
                except Exception: return 0.0
            need = g('needEnd'); tr = g('railTrackEnd')
            if need > 0:
                a['r'].append(tr / need); a['n'] += 1
                a['sz'] += g('procureStockZeroGoodMonths')
            a['want'] += g('procureWantUnitsPerYear'); a['got'] += g('procureGotUnitsPerYear')
            a['natW'] += g('natWantPhiLow') + g('natWantPhiMid') + g('natWantPhiHigh')
            a['natG'] += g('natGotPhiLow') + g('natGotPhiMid') + g('natGotPhiHigh')
            a['sav'] += g('procureSavingsBindGoodMonths'); a['nos'] += g('natNoSellerStockGoodMonths')
        for k, a in acc.items():
            if a['n'] < 20: continue
            rows.append((s, a['nat'], k, a['water'], a['terr'], a['n'], st.median(a['r']), a['sz'] / a['n'], a['want'], a['got'], a['natW'], a['natG'], a['sav'] / a['n'], a['nos'] / a['n']))
with open(f"{out}/q1_roster.tsv", 'w', encoding='utf-8', newline='') as f:
    f.write("seed\tnation\tregion\twaterFlag\tterrain\tyearsNeedPos\tmedTrackOverNeed\tstockZeroGoodMonthsPerYr\twantSum\tgotSum\tnatWantSum\tnatGotSum\tsavBindPerYr\tnatNoSellerPerYr\n")
    for r in rows: f.write("\t".join(str(round(x, 4)) if isinstance(x, float) else str(x) for x in r) + "\n")
hit = collections.defaultdict(list)
for r in rows:
    if r[3] == '0' and port.get(r[2], 1) == 0 and r[6] < 0.25 and r[7] >= 12: hit[(r[1], r[2])].append(r)
pick = sorted(hit.items(), key=lambda kv: (-len(kv[1]), st.median([x[6] for x in kv[1]])))
with open(f"{out}/q1_pick.txt", 'w', encoding='utf-8') as f:
    f.write(f"州×種の行 {len(rows)}・条件に当たる州 {len(hit)}\n")
    f.write("nation\tregion\tseedsHit\tseeds\tmedRatio\tmedStockZeroPerYr\tgot/want\tnatGot/natWant\tsavBindPerYr\tnatNoSellerPerYr\n")
    for (n, rg), lst in pick[:40]:
        W = sum(x[8] for x in lst); G = sum(x[9] for x in lst); NW = sum(x[10] for x in lst); NG = sum(x[11] for x in lst)
        f.write(f"{n}\t{rg}\t{len(lst)}\t{','.join(str(x[0]) for x in lst)}\t{st.median([x[6] for x in lst]):.3f}\t{st.median([x[7] for x in lst]):.1f}\t{G/W if W>0 else float('nan'):.3f}\t{NG/NW if NW>0 else float('nan'):.3f}\t{st.median([x[12] for x in lst]):.1f}\t{st.median([x[13] for x in lst]):.1f}\n")
print("ok", len(rows), len(hit))
