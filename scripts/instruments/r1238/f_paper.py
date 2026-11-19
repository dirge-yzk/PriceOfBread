# (1238) 紙：開幕の world.json（州の share×国の人口〈階級の和〉・植民地は含まない＝近似）から文化ごとの f を 2 つの定義で出す。
#  旧（旗 A の実装）：M_X＝主の文化が X の国のうち X の人口が最大の国の X の人口（無ければ 0）。案：M_X＝X の人口が最大の 1 国の X の人口（主の文化を問わない）。f＝1−M_X÷W_X。
import json, sys, collections
d = json.load(open(sys.argv[1], encoding="utf-8-sig"))
cls = ["nobility","peasant","worker","bourgeois","professional","merchant","artisan"]
W = collections.Counter(); byNat = collections.defaultdict(collections.Counter)
for n in d["nations"]:
    pop = sum(float(n[k]) for k in cls if isinstance(n.get(k), (int, float)))
    for p in n["provinces"]:
        x = p.get("culture") or ""
        if not x: continue
        v = pop * float(p["share"]); W[x] += v; byNat[n["name"]][x] += v
main = {nm: c.most_common(1)[0][0] for nm, c in byNat.items() if c}
rows = []
for x, w in W.items():
    old = max([byNat[nm][x] for nm in byNat if main.get(nm) == x] or [0.0])
    new = max(byNat[nm][x] for nm in byNat)
    holder = max(byNat, key=lambda nm: byNat[nm][x])
    rows.append((w, x, 1 - old / w, 1 - new / w, holder))
rows.sort(reverse=True)
print("文化\t世界の人口\tf旧\tf案\t最大の国")
for w, x, fo, fn, h in rows[:40]: print(f"{x}\t{w:.0f}\t{fo:.3f}\t{fn:.3f}\t{h}")
