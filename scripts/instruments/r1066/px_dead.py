# (1066) pxstep_s（年×国×格×枝の件数＝財×州×歩）から、価格の歩が見た財の数を州×歩あたりに直す（読むだけ）。
# 歩＝旬（月 3 回）＝年 36 歩。州の数は region_s の年の最後の四半期。生きた財の一覧の外（需要も産出も無い財）は pxstep にも台帳にも載らない＝G−（生きた財）で出す。
import sys, statistics, collections
D = sys.argv[1]; G = int(sys.argv[2])
YQ = {1800: 67, 1850: 267, 1900: 467, 1950: 667}
res = collections.defaultdict(list)
for s in range(1, 11):
    br = collections.Counter()
    with open(f"{D}/pxstep_s{s}.tsv", encoding="utf-8") as f:
        f.readline()
        for line in f:
            y, n, t, b, cnt, runs = line.rstrip("\n").split("\t")
            if int(y) in YQ: br[(int(y), b)] += int(cnt)
    nreg = collections.Counter()
    with open(f"{D}/region_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); iq = h.index("quarter")
        qy = {q: y for y, q in YQ.items()}
        for line in f:
            q = int(line.split("\t", 1)[0]) if iq == 0 else int(line.split("\t")[iq])
            if q in qy: nreg[qy[q]] += 1
    for y in YQ:
        steps = sum(v for (yy, b), v in br.items() if yy == y and b not in ("dormant", "dead"))
        dorm = br[(y, "dormant")]; dead_in = br[(y, "dead")]
        denom = nreg[y] * 36
        live = (steps + dorm + dead_in) / denom
        res[(y, "regions")].append(nreg[y]); res[(y, "stepped_per_region_step")].append(steps / denom)
        res[(y, "dormant_per_region_step")].append(dorm / denom); res[(y, "live_per_region_step")].append(live)
        res[(y, "outside_live_share")].append(1 - live / G)
keys = ["regions", "stepped_per_region_step", "dormant_per_region_step", "live_per_region_step", "outside_live_share"]
print("year\t" + "\t".join(keys) + f"\t(G={G}・10種の中央値)")
for y in YQ:
    print(str(y) + "\t" + "\t".join(f"{statistics.median(res[(y, k)]):.3f}" for k in keys))
