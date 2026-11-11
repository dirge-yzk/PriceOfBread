# (1038) 紙の台本 1（基線 keep_1035/base_ins・旗 0 の盤面）：段1 の新しい門 S >= D×(1−τ²)・D＝AISubmitMilRatio(3.0)×max(床0.05, 標的の陣営の陸の大きさ) の近似の予言。
# 近似（名乗る）：標的の陸＝抽象標的は world.json の military（海軍の割合を引かない＝D を大きく見る側）・実在の国は標的自身の直近の warentry の sumLand/calls（その四半期以前で calls>0 の最後）。
#   陣営（同盟）は数えない（D を小さく見る側）。τ＝0（D を大きく見る側）。S＝侵略した国のその四半期の sumSC/calls（S_C′）。
import sys, csv, os, json, bisect
root, world = sys.argv[1], sys.argv[2]
absmil = {a["name"]: float(a["military"]) for a in json.load(open(world, encoding="utf-8"))["abstractTargets"]}
R, FL = 3.0, 0.05
T = dict(inv=0, matched=0, Spos=0, passD=0, tgtUnknown=0, calls=0, callsSpos=0)
bins = [0.15, 0.3, 0.75, 1.5, 3.0]; cb = [0] * len(bins)
for s in range(1, 11):
    we, land = {}, {}
    with open(os.path.join(root, f"warentry_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            c = int(row[ix["calls"]])
            if c <= 0: continue
            q, n = int(row[ix["quarter"]]), row[ix["nation"]]
            S = float(row[ix["sumSC"]]) / c; we[(q, n)] = S
            land.setdefault(n, []).append((q, float(row[ix["sumLand"]]) / c))
            T["calls"] += c
            if S > 0:
                T["callsSpos"] += c
                for i, b in enumerate(bins):
                    if S >= b: cb[i] += c
    p = dict(inv=0, matched=0, Spos=0, passD=0)
    with open(os.path.join(root, f"invade_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            q, a, t = int(row[ix["q"]]), row[ix["invader"]], row[ix["target"]]
            p["inv"] += 1
            S = we.get((q, a))
            if S is None: continue
            p["matched"] += 1
            if S > 0: p["Spos"] += 1
            if row[ix["targetIsNation"]] == "0": tl = absmil.get(t)
            else:
                lst = land.get(t, []); qs = [x[0] for x in lst]; i = bisect.bisect_right(qs, q) - 1
                tl = lst[i][1] if i >= 0 else None
            if tl is None: T["tgtUnknown"] += 1; continue
            if S >= R * max(FL, tl): p["passD"] += 1
    print(f"seed {s}: " + " ".join(f"{k}={v}" for k, v in p.items()))
    for k, v in p.items(): T[k] += v
print("total " + " ".join(f"{k}={v}" for k, v in T.items()))
print("calls with S>=" + " ".join(f"{b}:{c}" for b, c in zip(bins, cb)))
