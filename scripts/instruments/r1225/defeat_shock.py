# (1225) 読むだけ：scalars_s の年末の defeatShock と warExhaustion の 1790〜1871 の平均（国ごとの種の中央）（種の範囲と年は引数）
import sys, csv, collections, statistics
d, s0, s1, y0, y1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5])
ds = collections.defaultdict(lambda: collections.defaultdict(list)); ex = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(s0, s1 + 1):
    with open(f"{d}/scalars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            y = int(r["year"])
            if y < y0 or y > y1: continue
            try: ds[r["nation"]][s].append(float(r["defeatShock"])); ex[r["nation"]][s].append(float(r["warExhaustion"]))
            except ValueError: pass
for nm in sys.argv[6].split(","):
    if nm not in ds: print(nm, "none"); continue
    a = statistics.median([statistics.mean(v) for v in ds[nm].values()]); b = statistics.median([statistics.mean(v) for v in ex[nm].values()])
    print(f"{nm}\tdefeatShock_med={a:.3f}\twarExhaustion_med={b:.3f}")
