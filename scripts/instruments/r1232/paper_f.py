# (1232) 紙：段 F の c_W＝A_W ρ_W÷(A_W ρ_W＋A_L ρ_L) を講和ごとに当て、段 E の最大の区分で移る州を数える。読むだけ。
# 使い方: python -X utf8 -I paper_f.py <INSRAW> <places.json> <world.json> <種の下> <種の上> <出力>
# A＝rulepeace_s の winnerLandIndex・loserLandIndex（四半期の末の陸軍力）。ρ＝大圏の 1÷(1＋km÷1万)（Politics.cs:1056 と同じ式）。
# 勝者の首都＝rule_s で capitalDistanceKm が 0 の州（無ければ world.json の開幕の座標）。敗者の ρ_L＝rule_s の capitalDistanceKm から。
# 変種：〈流せる力〉A_W'＝max(0, L−H)（rulepower_s の lNationIndex・hNationIndex＝国内を治める分を引いた勝者の軍）。
import sys, json, math
from collections import defaultdict
ins, placesp, worldp, lo, hi, outp = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), sys.argv[6]
out = []
def P(s): out.append(s)
def fl(x):
    try:
        v = float(x); return v if v == v and abs(v) != float('inf') else None
    except ValueError:
        return None
pl = {p["name"]: (p["lat"], p["lon"]) for p in json.load(open(placesp, encoding="utf-8"))}
wd = {n["name"]: (n.get("latitude"), n.get("longitude")) for n in json.load(open(worldp, encoding="utf-8"))["nations"]}
def gc(a, b):
    la1, lo1 = map(math.radians, a); la2, lo2 = map(math.radians, b)
    h = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 2 * 6371.0 * math.asin(min(1.0, math.sqrt(h)))
def rho(km): return 1.0 / (1.0 + km / 10000.0)

tot = defaultdict(int); hist = defaultdict(lambda: defaultdict(int)); shadow = 0; peaces = 0; skipped = 0
dmatch = [0]; dlist = []; byrat = defaultdict(lambda: [0, 0]); ceded = 0; lone = 0; lone_all = 0; small = 0; small_taken = 0; vanish = defaultdict(int)
for s in range(lo, hi + 1):
    peace = []
    with open(f"{ins}/rulepeace_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            peace.append((int(c[ix["quarter"]]), c[ix["winner"]], c[ix["loser"]], fl(c[ix["winnerLandIndex"]]), fl(c[ix["loserLandIndex"]]), int(float(c[ix["takenCount"]])), int(float(c[ix["loserRegionCount"]]))))
    need = defaultdict(set)
    for q, W, L, *_ in peace:
        for d in range(-3, 4): need[q + d].add(W); need[q + d].add(L)   # rule_s は国ごとに年 1 回の行＝近い四半期の行を使う
    regs = defaultdict(list); cap = {}
    with open(f"{ins}/rule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            q = int(c[ix["quarter"]]); n = c[ix["nation"]]
            if n not in need.get(q, ()): continue
            km = fl(c[ix["capitalDistanceKm"]])
            r = c[ix["region"]]
            regs[(q, n)].append((r, km, fl(c[ix["ruleStateShare"]]) or 0, fl(c[ix["ruleLocalShare"]]) or 0, fl(c[ix["ruleForeignShare"]]) or 0, fl(c[ix["ruleUngovShare"]]) or 0))
            if km is not None and km < 1e-6 and r in pl: cap[(q, n)] = pl[r]
    lh = {}
    with open(f"{ins}/rulepower_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            q = int(c[ix["quarter"]]); n = c[ix["nation"]]
            if n in need.get(q, ()) and (q, n) not in lh:
                lh[(q, n)] = (fl(c[ix["lNationIndex"]]), fl(c[ix["hNationIndex"]]))
    with open(f"{ins}/cederule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            if line.split("\t")[ix["how"]] == "Ceded": ceded += 1
    # 壊された分 δ_L＝死者÷(死者＋講和の時の敗者の兵)（wars_s の deadK は両側の和＝上の見積もり ub と、半分を敗者とする half）
    wend = {}
    with open(f"{ins}/wars_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            qe = int(c[ix["q"]]) + int(float(c[ix["months"]])) // 3
            wend.setdefault(frozenset((c[ix["sideA"]], c[ix["sideB"]])), []).append((qe, fl(c[ix["deadK"]]) or 0.0))
    sold = {}
    with open(f"{ins}/nation_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            q = int(c[ix["quarter"]]); n = c[ix["nation"]]
            if n in need.get(q, ()): sold[(q, n)] = fl(c[ix["soldierK"]])
    for q, W, L, AW, AL, taken, lrc in peace:
        peaces += 1; shadow += taken
        def near(dct, n):
            for d in (0, -1, -2, -3, 1, 2, 3):
                if (q + d, n) in dct: return dct[(q + d, n)]
            return None
        rs = near(regs, L)
        wc = near(cap, W) or (wd[W] if W in wd and wd[W][0] is not None else None)
        if not rs or wc is None or AW is None or AL is None or AW + AL <= 0:
            skipped += 1; continue
        if len(rs) == 1: lone += 1
        lw = near(lh, W); AWf = max(0.0, lw[0] - lw[1]) if lw and lw[0] is not None and lw[1] is not None else None
        if AL < 0.01 * AW: small += 1
        dk = None
        for qe, d in wend.get(frozenset((W, L)), []):
            if abs(qe - q) <= 2: dk = d; break
        sl = near(sold, L)
        dub = dk / (dk + sl) if dk is not None and sl is not None and dk + sl > 0 else None
        dhf = (dk / 2) / (dk / 2 + sl) if dub is not None else None
        if dub is not None: dmatch[0] += 1; dlist.append(dub)
        rat = AW / AL if AL > 0 else float("inf")
        rb = "≦1" if rat <= 1 else ("1-3" if rat <= 3 else ("3-10" if rat <= 10 else ">10"))
        for var, aw, sc in (("A", AW, 1.0), ("流せる力", AWf, 1.0), ("A×δ_L ub", AW, dub), ("A×δ_L half", AW, dhf), ("min(A_W,A_L)", min(AW, AL) if AL > 0 else None, 1.0), ("割り振り A", AW, 1.0), ("割り振り 流せる力", AWf, 1.0)):
            if aw is None or sc is None: continue
            nmin = nmax = 0
            alloc = var.startswith("割り振り")
            if alloc:
                sw = sum(rho(gc(wc, pl[r])) for (r, km, *_ ) in rs if r in pl and km is not None)
                sl_ = sum(rho(km) for (r, km, *_ ) in rs if r in pl and km is not None)
            for (r, km, st, lc, fo, un) in rs:
                if r not in pl or km is None: continue
                rw = rho(gc(wc, pl[r])); rl = rho(km)
                if alloc and sw > 0 and sl_ > 0: rw /= sw; rl /= sl_
                den = aw * rw + AL * rl
                cw = aw * rw / den if den > 0 else 0.0
                xw = st * cw * sc; st2 = st - xw
                if xw > max(st2, lc, un, fo): nmin += 1
                if xw > max(st2 + lc, un, fo): nmax += 1
            tot[(var, 'min')] += nmin; tot[(var, 'max')] += nmax
            if var == "A": byrat[rb][0] += 1; byrat[rb][1] += nmax
            hist[(var, 'max')][min(nmax, 5)] += 1
            if var == "A" and AL < 0.01 * AW: small_taken += nmax
            if nmax == len(rs): vanish[var] += 1
            if len(rs) == 1 and var == "A" and nmax == 1: lone_all += 1
P(f"講和 {peaces}（種 {lo}-{hi} の和・rulepeace_s）・読めなかった講和 {skipped}・今の木の実際の割譲（cederule_s の Ceded）{ceded}・段 B の影 {shadow}")
P(f"  δ_L が取れた講和 {dmatch[0]}・δ_L ub の中央 {sorted(dlist)[len(dlist)//2] if dlist else None}")
P("  A の形で A_W÷A_L の区分ごと（講和の数・移る州 max）：" + " ".join(f"{k}:{v[0]}・{v[1]}" for k, v in sorted(byrat.items())))
for var in ("A", "流せる力", "A×δ_L ub", "A×δ_L half", "min(A_W,A_L)", "割り振り A", "割り振り 流せる力"):
    P(f"  {var}: 移る州 min（持ち主の側＝国家だけ）{tot[(var,'min')]}・max（国家＋在地）{tot[(var,'max')]}・敗者の全ての州が移る講和（max）{vanish[var]}")
    P("    1 講和で移る州（max）の度数：" + " ".join(f"{k if k<5 else '5+'}:{hist[(var,'max')][k]}" for k in range(6)))
P(f"  A_L＜A_W÷100 の講和 {small}（その講和で移る州 max の和 {small_taken}）")
P(f"  敗者が 1 州の国の講和 {lone}・その州が移る（＝国が消える）講和 {lone_all}")
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
