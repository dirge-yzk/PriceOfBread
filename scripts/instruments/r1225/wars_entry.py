# (1225) 読むだけ：wars_s の entryCode ごとの件数・月数の中央・同じ組の最多（種の範囲は引数）
import sys, csv, collections, statistics
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
cnt = collections.Counter(); months = collections.defaultdict(list); pairs = collections.defaultdict(collections.Counter)
for s in range(s0, s1 + 1):
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0": continue
            e = r["entryCode"]; cnt[e] += 1; months[e].append(float(r["months"]))
            pairs[e][(s, r["sideA"], r["sideB"])] += 1
for e, n in cnt.most_common():
    top = pairs[e].most_common(3)
    print(f"{e}\tn={n}\tmonths_med={statistics.median(months[e]):.1f}\ttop={[(k[0], k[1], k[2], v) for k, v in top]}")
