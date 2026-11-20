# (1249) 紙・読むだけ：盤面の州（生の region_s の持ち主と人口）で文化の f を 2 つの形で出す。
#  州の文化＝world.json の province の culture（city 名で引く）・植民地（world.json に無い州）は開幕の持ち主の最初の州の文化（Simulation.cs:1365 の w.Region.Culture の近似）。
#  形 A （(1230)）：M_X＝主の文化が X の国のうち X の人口が最大の国の X（無ければ 0＝f 1）。形 A′（(1249)）：M_X＝X の人口が最大の 1 国の X（主の文化を問わない）。f＝1−M_X÷W_X。
#  主の文化＝その国の州の人口の最多の文化（Diplomacy.cs:822 の近似）。引数：生 種0 種1 world.json
import sys, csv, json, collections, statistics
d, s0, s1, wj = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
W = json.load(open(wj, encoding="utf-8-sig"))
cul = {}; first = {}
for n in W["nations"]:
    for p in n["provinces"]:
        if p.get("culture"): cul[p["city"]] = p["culture"]
    first[n["name"]] = n["provinces"][0].get("culture", "") if n["provinces"] else ""
YEARS = (1783, 1850, 1900, 1914)
NATS = ("イギリス", "フランス", "ロシア", "プロイセン", "オーストリア", "オスマン", "清")
nat_f = collections.defaultdict(list); cul_f = collections.defaultdict(list)
for s in range(s0, s1 + 1):
    rows = collections.defaultdict(list); home = {}
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            q = int(r["quarter"]); y = int(r["year"])
            if q == 0: home.setdefault(r["region"], r["nation"])
            if q % 4 == 0 and y in YEARS: rows[y].append((r["nation"], r["region"], float(r["populationK"])))
    for y in YEARS:
        by = collections.defaultdict(collections.Counter); reg = []
        for nat, rg, pop in rows[y]:
            c = cul.get(rg) or first.get(home.get(rg, nat), "")
            if not c or not pop > 0: continue
            by[nat][c] += pop; reg.append((nat, c, pop))
        main = {nat: cc.most_common(1)[0][0] for nat, cc in by.items()}
        tot = collections.Counter()
        for cc in by.values(): tot.update(cc)
        fA = {}; fB = {}
        for x, w in tot.items():
            mA = max([by[n][x] for n in by if main[n] == x] or [0.0]); mB = max(by[n][x] for n in by)
            fA[x] = 1 - mA / w; fB[x] = 1 - mB / w
            cul_f[(y, x)].append((w, fA[x], fB[x]))
        for nat in NATS:
            ps = [(c, p) for n, c, p in reg if n == nat]; P = sum(p for _, p in ps)
            if P > 0: nat_f[(y, nat)].append((sum(fA[c] * p for c, p in ps) / P, sum(fB[c] * p for c, p in ps) / P))
print("== 国の州の人口で重みづけた f の平均（種の中央）：形 A → 形 A′")
for y in YEARS:
    print(str(y) + "\t" + "\t".join(f"{n} {statistics.median(a for a, _ in nat_f[(y, n)]):.3f}→{statistics.median(b for _, b in nat_f[(y, n)]):.3f}" for n in NATS if nat_f[(y, n)]))
print("== 文化ごと（形 A と A′ で f が違う物・世界の人口の大きい順 20・種の中央）")
for y in (1783, 1900):
    xs = [(statistics.median(w for w, _, _ in v), x, statistics.median(a for _, a, _ in v), statistics.median(b for _, _, b in v)) for (yy, x), v in cul_f.items() if yy == y]
    xs = [t for t in xs if abs(t[2] - t[3]) > 1e-9]; xs.sort(reverse=True)
    print(f"{y}\t" + "・".join(f"{x} {a:.2f}→{b:.2f}（{w/1000:.1f}M）" for w, x, a, b in xs[:20]))
