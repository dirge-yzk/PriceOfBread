# (1238) 読むだけ：英の開幕の Core の州が年ごとに誰の手にあるか（region_s）・英の Core の人口。引数：生 種0 種1
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = "イギリス"
years = [1820,1850,1880,1900,1914]
for s in range(s0, s1 + 1):
    own = collections.defaultdict(dict); core0 = []; corepop = collections.Counter(); totpop = collections.Counter()
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8", newline="") as f:
        first = None
        for r in csv.DictReader(f, delimiter="\t"):
            y = int(r["year"]); q = int(r["quarter"])
            if first is None: first = q
            if q == first and r["nation"] == nat and r["tier"] in ("Core","Integrated"): core0.append(r["region"])
            if y in years and q % 4 == 0:
                own[y][r["region"]] = r["nation"]
                if r["nation"] == nat:
                    totpop[y] += float(r["populationK"])
                    if r["tier"] in ("Core","Integrated"): corepop[y] += float(r["populationK"])
    out = []
    for y in years:
        lost = [f"{g}->{own[y].get(g,'?')}" for g in core0 if own[y].get(g) != nat]
        out.append(f"{y}:core={corepop[y]:.0f}/tot={totpop[y]:.0f} lost={lost}")
    print(f"s{s} core0={core0}\n  " + "\n  ".join(out))
