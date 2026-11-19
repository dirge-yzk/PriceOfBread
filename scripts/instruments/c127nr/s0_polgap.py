# (1249) 段 0・読むだけ：1900-1950 の英と欧州の列強の政体の隔たり（Diplomacy.cs:1063〜1076 IdeologyTension の月の増分を四半期×3 で足す近似）。
#  同じ政体＝−IdeologyAffinityRelief 0.30／違う政体＝IdeologyTensionPerMonth 0.22×|Ideology(a)−Ideology(b)|（Diplomacy.cs:1053 の表）。交戦中の組の除外は無視（近似）。
#  引数：生 種0 種1 札。出力：種ごとの和と組ごとの和の種の中央。
import sys, csv, collections, statistics
d, s0, s1, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
IDEO = {0: 0.0, 1: 0.15, 2: 0.0, 3: 0.45, 4: 0.60, 5: 0.35, 6: 1.0, 7: 0.2, 8: 0.50}
EU = ["フランス","ロシア","プロイセン","ドイツ帝国","北ドイツ連邦","オーストリア","スペイン","イタリア王国","サルデーニャ","オスマン"]
per_seed = []; per_pair = collections.defaultdict(list); same_q = []; gap_q = []
for s in range(s0, s1 + 1):
    pol = collections.defaultdict(dict)
    with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if not (1900 <= int(r["year"]) <= 1950) or float(r["totalPopulationK"]) < 10: continue
            pol[int(r["quarter"])][r["nation"]] = int(r["polityCode"])
    tot = 0.0; pp = collections.Counter(); ns = 0; ng = 0
    for q, m in pol.items():
        if "イギリス" not in m: continue
        u = m["イギリス"]
        for e in EU:
            if e not in m: continue
            v = 3 * (-0.30 if m[e] == u else 0.22 * abs(IDEO[u] - IDEO[m[e]]))
            tot += v; pp[e] += v
            if m[e] == u: ns += 1
            else: ng += 1
    per_seed.append(tot); same_q.append(ns); gap_q.append(ng)
    for e in EU: per_pair[e].append(pp[e])
print(f"{tag}\t種ごとの和(1900-50・英×欧州の列強)\t" + ",".join(f"{x:.1f}" for x in per_seed) + f"\t中央 {statistics.median(per_seed):.1f}")
print(f"{tag}\t同じ政体の組×四半期 中央 {statistics.median(same_q)}\t違う政体 中央 {statistics.median(gap_q)}")
for e in EU:
    v = per_pair[e]
    if any(v): print(f"{tag}\t{e}\t中央 {statistics.median(v):.1f}")
