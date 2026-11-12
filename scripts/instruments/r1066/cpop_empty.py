# (1066) cpop_s：四半期×POP の popK。年（q%4==3 の 1 行＝その年の終わり）ごとに、POP の数・人口 0 の数・州の数・国を数える。読むだけ。
import sys, statistics, collections
D = sys.argv[1]
YEARS = {1784, 1800, 1850, 1900, 1950}
res = collections.defaultdict(list)
nats = collections.Counter()
small = collections.Counter()
for s in range(1, 11):
    cnt = collections.Counter()
    with open(f"{D}/cpop_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t")
        iq, iy, inat, ireg, ik, icl = (h.index(k) for k in ("quarter", "year", "nation", "region", "popK", "className"))
        for line in f:
            c = line.split("\t", 10)
            q = int(c[iq]); y = int(c[iy])
            if q % 4 != 3 or y not in YEARS: continue
            k = float(c[ik])
            cnt[(y, "pops")] += 1
            if s == 1: nats[(y, c[inat])] += 1
            if k == 0: cnt[(y, "zero")] += 1; cnt[(y, "zero_" + c[icl])] += 1
            elif k < 1e-6: cnt[(y, "tiny")] += 1
            if s == 1 and 0 < k < 1e-3: small[(y, "<1e-3")] += 1
    for kk, v in cnt.items(): res[kk].append(v)
def med(k): v = res[k]; return statistics.median(v + [0] * (10 - len(v)))
for y in sorted(YEARS):
    print(y, "pops", med((y, "pops")), "zero", med((y, "zero")), "tiny(<1e-6,>0)", med((y, "tiny")), "share0", round(med((y, "zero")) / max(1, med((y, "pops"))), 3))
    zc = sorted(((med(k), k[1]) for k in res if k[0] == y and k[1].startswith("zero_")), reverse=True)[:8]
    print("   by class:", zc)
print("nations in cpop (s1):", sorted(set(n for (_, n) in nats)))
print("small s1:", dict(small))
