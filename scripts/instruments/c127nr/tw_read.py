"""(1249) c127nr 記録の読み手（読むだけ・門に使わない）：tensionwhy_s の英を含む組の緊張の内訳（1900-1950）。
  種ごとに、英を含む組×四半期の緊張の終値と理由ごとの寄与を足し、四半期の数で割った〈1 四半期あたりの英の組の緊張の和〉を出し、種の中央を並べる。
  緊張 100 以上の英の組×四半期の数（開戦の門の近く）も数える。
  使い方: python tw_read.py <札>=<tensionwhy_s のあるフォルダ> ...   （例：Z0=D:/PoB_work/c127nr_work/z0_tables B=<腕の ins>）"""
import sys, os, csv, collections, statistics as st
NAT = "イギリス"
args = [a.split("=", 1) for a in sys.argv[1:]]
out = {}
for tag, d in args:
    per = collections.defaultdict(list); hi = []; pairs = []
    for s in range(1, 11):
        p = os.path.join(d, f"tensionwhy_s{s}.tsv")
        if not os.path.exists(p): continue
        acc = collections.Counter(); qs = set(); h = 0; ps = set()
        with open(p, encoding="utf-8", newline="") as f:
            rd = csv.reader(f, delimiter="\t"); head = next(rd); ix = {k: i for i, k in enumerate(head)}; r0 = ix["Initial"]
            for r in rd:
                y = int(r[ix["year"]])
                if not (1900 <= y <= 1950): continue
                qs.add(r[ix["q"]])
                if NAT not in (r[ix["nationA"]], r[ix["nationB"]]): continue
                t = float(r[ix["tension"]]); acc["tension"] += t
                if t >= 100: h += 1
                ps.add((r[ix["nationA"]], r[ix["nationB"]]))
                for j in range(r0, len(head)): acc[head[j]] += float(r[j])
        n = max(1, len(qs))
        for k, v in acc.items(): per[k].append(v / n)
        hi.append(h); pairs.append(len(ps))
    out[tag] = (per, hi, pairs)
keys = ["tension"] + [k for k in next(iter(out.values()))[0] if k != "tension"]
print("理由\t" + "\t".join(f"{t}（種の中央・1 四半期の和）" for t, _ in args))
for k in keys:
    vals = [st.median(out[t][0][k]) if out[t][0].get(k) else 0.0 for t, _ in args]
    if all(abs(v) < 0.05 for v in vals): continue
    print(k + "\t" + "\t".join(f"{v:.1f}" for v in vals))
print("緊張100以上の英の組×四半期（種ごと）\t" + "\t".join(",".join(map(str, out[t][1])) for t, _ in args))
print("英を含む組の数（種ごと）\t" + "\t".join(",".join(map(str, out[t][2])) for t, _ in args))
