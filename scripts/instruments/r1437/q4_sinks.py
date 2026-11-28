# (1437) 線路と機関車の 2 つの使い道の比べ（読むだけ）
# 使い道 1＝EconomyStep の〈鉄道インフラ〉（工業化度 TotalIndustrialization の建設 ConsRail・ConsLoco と保守 InfraMaintRate）＝線路の在庫（RailTrack）を増やさない消費。railspend_s（国×四半期・実際に移った量）。
# 使い道 2＝Transport.BuyToward の敷き＝RailTrack を増やす買い。trrail_s（州×年）の procureGot（州の在庫から）＋natGot（国の中の買い）。
# 同じ州の同じ投資家の貯蓄と同じ州の在庫から、使い道 1 が月の先（EconomyStep.Run → Transport.Step の順・Simulation.cs:1975・:2001）。
# 出力：<out>/<tag>_q4_sinks.txt（10 年の帯×国の和・10 種の和）
import sys, csv, collections
raw, out, tag = sys.argv[1], sys.argv[2], sys.argv[3]
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception: return 0.0
E = collections.defaultdict(float); T = collections.defaultdict(float); TL = collections.defaultdict(float)
for s in range(1, 11):
    with open(f"{raw}/railspend_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            y = int(row[ix['year']]); d = (y // 10) * 10; n = row[ix['nation']]
            q = sum(fl(row[ix[p + k + 'Qty']]) for p in ('pre', 'post') for k in ('BuildRail', 'BuildLoco', 'MaintRail', 'MaintLoco'))
            E[(d, n)] += q; E[(d, '*')] += q
    with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            y = int(row[ix['year']]); d = (y // 10) * 10; n = row[ix['nation']]
            g = fl(row[ix['procureGotUnitsPerYear']]); ng = sum(fl(row[ix['natGotPhi' + b]]) for b in ('Low', 'Mid', 'High'))
            T[(d, n)] += g + ng; T[(d, '*')] += g + ng; TL[(d, '*')] += g
with open(f"{out}/{tag}_q4_sinks.txt", 'w', encoding='utf-8') as f:
    f.write("10年の帯\t国\t使い道1（EconomyStep の鉄道インフラ・線路＋機関車の量）\t使い道2（敷き・州の在庫＋国の中）\t使い道1÷(1＋2)\n")
    for d in range(1780, 1951, 10):
        e, t = E[(d, '*')], T[(d, '*')]
        if e + t > 0: f.write(f"{d}\t全て\t{e:.4g}\t{t:.4g}\t{e / (e + t):.3f}\n")
    f.write("\n国別（1870〜1949 の和・使い道の和の多い順の上位 20）\n")
    tot = collections.defaultdict(lambda: [0.0, 0.0])
    for (d, n), v in E.items():
        if n != '*' and 1870 <= d < 1950: tot[n][0] += v
    for (d, n), v in T.items():
        if n != '*' and 1870 <= d < 1950: tot[n][1] += v
    for n, (e, t) in sorted(tot.items(), key=lambda kv: -(kv[1][0] + kv[1][1]))[:20]:
        f.write(f"1870-1949\t{n}\t{e:.4g}\t{t:.4g}\t{e / (e + t) if e + t > 0 else float('nan'):.3f}\n")
print('ok')
