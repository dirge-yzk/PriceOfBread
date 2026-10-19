# (433) 線 sb：家事奉仕の買い手の読み手（読むだけ）。標準の計器の svcbuy_s<種>.tsv を国×窓×階級で集め、
# 上位（rank 1・2＝大衆でない者）の買えた量の割合と、配分を〈比例の配給〉に替えた場合の割合（買いたい量の割合）を並べる。
# 使い方：PYTHONUTF8=1 python an_svcbuy.py <計器のフォルダ> [<出力のファイル>]
# 力学の値を 1 つも書かない・乱数を引かない。限界：反実仮想の比例配給は〈買いたい量〔予算で絞った後〕の割合〉で近似する
#（配給の率は全ての買い手に同じ＝買いたい量の割合がそのまま買えた量の割合になる・総量は同じ）。
import csv, sys, os, glob, statistics, collections
src = sys.argv[1]
out = sys.argv[2] if len(sys.argv) > 2 else None
WIN = {"Q8-39": (8, 39), "Q40-159": (40, 159), "Q160-399": (160, 399), "Q400-667": (400, 667)}
def fl(x):
    try: return float(x)
    except Exception: return float("nan")
acc = collections.defaultdict(lambda: collections.defaultdict(float))   # (seed,nation,win) -> key -> 量
cls = collections.defaultdict(lambda: collections.defaultdict(float))   # (seed,nation,win,classId) -> key
names = {}
sat = collections.defaultdict(list)   # (窓, 階級) -> 国×種×四半期の満足度（四半期の末の断面）
for path in sorted(glob.glob(os.path.join(src, "svcbuy_s*.tsv"))):
    seed = os.path.basename(path)[len("svcbuy_s"):-4]
    with open(path, encoding="utf8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            q = int(r["quarter"])
            for w, (a, b) in WIN.items():
                if a <= q <= b:
                    k = (seed, r["nation"], w)
                    rank = int(r["rank"]); c = int(r["classId"]); names[c] = r["className"]
                    if fl(r.get("satisfactionMean", "nan")) == fl(r.get("satisfactionMean", "nan")): sat[(w, c)].append(fl(r["satisfactionMean"]))
                    for key in ("popK", "baseWantPerMonth", "wantPerMonth", "demandPerMonth", "gotPerMonth"):
                        v = fl(r[key])
                        if v == v:
                            acc[k][key] += v
                            acc[k][f"r{'0' if rank == 0 else 'U'}_{key}"] += v
                            cls[k + (c,)][key] += v
def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float("nan")
lines = []
nations = sorted({k[1] for k in acc})
lines.append("国\t窓\t種数\t上位の買えた量の割合\t上位の買いたい量の割合(比例配給の反実仮想)\t上位の欲しい量の割合\t大衆の充足(買えた÷買いたい)\t上位の充足\t上位の人数の割合")
for w in WIN:
    per = collections.defaultdict(list)
    for n in nations:
        rows = [acc[k] for k in acc if k[1] == n and k[2] == w]
        if not rows: continue
        def ratio(f): return med([f(a) for a in rows])
        got = ratio(lambda a: a["rU_gotPerMonth"] / (a["r0_gotPerMonth"] + a["rU_gotPerMonth"]) if a["r0_gotPerMonth"] + a["rU_gotPerMonth"] > 0 else float("nan"))
        dem = ratio(lambda a: a["rU_demandPerMonth"] / (a["r0_demandPerMonth"] + a["rU_demandPerMonth"]) if a["r0_demandPerMonth"] + a["rU_demandPerMonth"] > 0 else float("nan"))
        wnt = ratio(lambda a: a["rU_wantPerMonth"] / (a["r0_wantPerMonth"] + a["rU_wantPerMonth"]) if a["r0_wantPerMonth"] + a["rU_wantPerMonth"] > 0 else float("nan"))
        f0 = ratio(lambda a: a["r0_gotPerMonth"] / a["r0_demandPerMonth"] if a["r0_demandPerMonth"] > 0 else float("nan"))
        fU = ratio(lambda a: a["rU_gotPerMonth"] / a["rU_demandPerMonth"] if a["rU_demandPerMonth"] > 0 else float("nan"))
        pk = ratio(lambda a: a["rU_popK"] / (a["r0_popK"] + a["rU_popK"]) if a["r0_popK"] + a["rU_popK"] > 0 else float("nan"))
        lines.append(f"{n}\t{w}\t{len(rows)}\t{got:.3f}\t{dem:.3f}\t{wnt:.3f}\t{f0:.3f}\t{fU:.3f}\t{pk:.3f}")
        for i, v in enumerate((got, dem, wnt, f0, fU, pk)): per[i].append(v)
    lines.append(f"[中央]\t{w}\t\t" + "\t".join(f"{med(per[i]):.3f}" for i in range(6)))
lines.append("")
lines.append("階級別（国×種の中央の中央・窓ごと）：階級\t窓\tpopK\t欲しい量/人\t係数f(欲しい÷基礎)\t買いたい/欲しい\t買えた/買いたい\t買えた量の割合")
for w in WIN:
    cs = sorted({k[3] for k in cls if k[2] == w})
    tot = collections.defaultdict(float)
    for k, a in cls.items():
        if k[2] == w: tot[(k[0], k[1])] += a["gotPerMonth"]
    for c in cs:
        pk = []; wp = []; fF = []; bind = []; fill = []; share = []
        for k, a in cls.items():
            if k[2] != w or k[3] != c or not a["popK"] > 0: continue
            pk.append(a["popK"]); wp.append(a["wantPerMonth"] / a["popK"])
            if a["baseWantPerMonth"] > 0: fF.append(a["wantPerMonth"] / a["baseWantPerMonth"])
            if a["wantPerMonth"] > 0: bind.append(a["demandPerMonth"] / a["wantPerMonth"])
            if a["demandPerMonth"] > 0: fill.append(a["gotPerMonth"] / a["demandPerMonth"])
            if tot[(k[0], k[1])] > 0: share.append(a["gotPerMonth"] / tot[(k[0], k[1])])
        lines.append(f"{names.get(c, c)}({c})\t{w}\t{med(pk):.4g}\t{med(wp):.4g}\t{med(fF):.3g}\t{med(bind):.3g}\t{med(fill):.3g}\t{med(share):.3g}")
lines.append("")
lines.append("階級の満足度（人数加重・四半期の末・国×種×四半期の中央）：階級	窓	中央	最小	最大")
for w in WIN:
    for c in sorted({k[1] for k in sat if k[0] == w}):
        v = sat[(w, c)]
        lines.append(f"{names.get(c, c)}({c})	{w}	{statistics.median(v):.3f}	{min(v):.3f}	{max(v):.3f}")
txt = "\n".join(lines) + "\n"
print(txt)
if out:
    with open(out, "wb") as f: f.write(txt.encode("utf8"))
