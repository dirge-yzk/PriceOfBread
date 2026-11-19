# (1238) 読むだけ：warentry_s の英の行を期間ごとに合計（calls・evals・r_*・sh_*）と willNow の平均。引数：生 種0 種1 [国名]
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = sys.argv[4] if len(sys.argv) > 4 else "イギリス"
per = [(1783,1850),(1850,1900),(1900,1914),(1914,1919),(1919,1951)]
sums = collections.defaultdict(collections.Counter); wn = collections.defaultdict(list); nq = collections.Counter()
cols = None
for s in range(s0, s1 + 1):
    with open(f"{d}/warentry_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] != nat: continue
            y = int(r["year"]); p = next(i for i,(a,b) in enumerate(per) if a <= y < b)
            nq[p] += 1
            cols = [c for c in r if c.startswith(("r_","sh_")) or c in ("calls","evals","tgtNonMinor")]
            for c in cols:
                try: sums[p][c] += float(r[c])
                except ValueError: pass
            try:
                v = float(r["willNow"]);
                if v == v: wn[p].append(v)
            except ValueError: pass
for i,(a,b) in enumerate(per):
    nz = {c: round(v/10,1) for c, v in sums[i].items() if v}
    m = sum(wn[i])/len(wn[i]) if wn[i] else float('nan')
    print(f"{a}-{b-1}\t行={nq[i]}\twill平均={m:.3f}\t種あたり {nz}")
