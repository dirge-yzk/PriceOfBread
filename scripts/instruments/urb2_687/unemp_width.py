# (687) 線 urb2 段0：国ごとの失業（labor_s の plainUnemploymentShare・全四半期の平均）の種の中央値を、基線・N1・N2 の 3 本で並べ、ヌル腕の幅（3 本の最大−最小）を出す（読むだけ）。
# 使い方：python unemp_width.py <基線> <N1> <N2> [<腕>…]   （腕を渡すと同じ形で並べ、3 本の幅の外の国を数える）
import sys, csv, statistics as st, collections
dirs = [x.rstrip("/") for x in sys.argv[1:]]
def nat_means(d):
    per = collections.defaultdict(list)
    for s in range(1, 11):
        try: f = open(f"{d}/labor_s{s}.tsv", encoding="utf-8")
        except FileNotFoundError: continue
        with f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ina, ic = h.index("nation"), h.index("plainUnemploymentShare")
            acc = collections.defaultdict(list)
            for row in r:
                try: v = float(row[ic])
                except: continue
                if v == v: acc[row[ina]].append(v)
            for n, l in acc.items(): per[n].append(st.mean(l))
    return {n: st.median(l) for n, l in per.items()}
res = [nat_means(d) for d in dirs]
nats = sorted(res[0])
print("nation\t" + "\t".join(d.split("/")[-1] for d in dirs) + "\t(基線 N1 N2 の幅)")
out = 0
for n in nats:
    vals = [r.get(n, float("nan")) for r in res]
    lo, hi = min(vals[:3]), max(vals[:3])
    flag = ""
    if len(vals) > 3:
        for v in vals[3:]:
            if v > hi + (hi - lo): flag = " 幅の外"; out += 1
    print(n + "\t" + "\t".join(f"{v:.4f}" for v in vals) + f"\t{hi-lo:.4f}{flag}")
if len(dirs) > 3: print("## 幅の外（上側・幅の 2 倍まで）の国×腕:", out)
