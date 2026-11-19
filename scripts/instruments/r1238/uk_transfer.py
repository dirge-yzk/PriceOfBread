# (1238) 読むだけ：英の州が他へ移った件（region_s の年の頭の四半期どうし）を期間×行き先の型（開幕にいた国／新しい国）で数え、移った人口も足す。引数：生 種0 種1
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = "イギリス"
per = [(1783,1820),(1820,1850),(1850,1900),(1900,1951)]
P = lambda y: next(i for i,(a,b) in enumerate(per) if a <= y < b)
cnt = collections.Counter(); popk = collections.Counter(); gain = collections.Counter(); dest = collections.defaultdict(collections.Counter)
for s in range(s0, s1 + 1):
    prev = {}; start = None
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8", newline="") as f:
        cur = {}; cy = None
        rows = csv.DictReader(f, delimiter="\t")
        snaps = []
        for r in rows:
            q = int(r["quarter"])
            if q % 4: continue
            if start is None: start = q
            snaps.append((int(r["year"]), r["region"], r["nation"], float(r["populationK"])))
    by = collections.defaultdict(dict)
    for y, g, n_, p in snaps: by[y][g] = (n_, p)
    nations0 = {v[0] for v in by[min(by)].values()}
    ys = sorted(by)
    for a, b in zip(ys, ys[1:]):
        for g, (n0, p0) in by[a].items():
            n1 = by[b].get(g, (None, 0))[0]
            if n0 == nat and n1 not in (None, nat):
                k = "既存" if n1 in nations0 else "新"
                cnt[(P(b), k)] += 1; popk[(P(b), k)] += p0; dest[P(b)][n1] += 1
            if n1 == nat and n0 not in (None, nat): gain[P(b)] += p0
n = s1 - s0 + 1
for i,(a,b) in enumerate(per):
    print(f"{a}-{b-1}\t失った州 既存へ={cnt[(i,'既存')]/n:.1f}({popk[(i,'既存')]/n/1000:.1f}M) 新しい国へ={cnt[(i,'新')]/n:.1f}({popk[(i,'新')]/n/1000:.1f}M)\t得た人口={gain[i]/n/1000:.1f}M\t行き先上位={dest[i].most_common(6)}")
