# (424) 線 sv：使用人の段 0（読むだけ）。生の計器 m420_ins の spend_s・svc_s・class_s から、使用人（階級12）の所得・支払いの層別・買い手の階級別の割合・利用率を
# 国×時期（四半期の窓）で 10 種の中央として出す。使い方：PYTHONUTF8=1 python a0_servants.py <生の計器のフォルダ> <出力のフォルダ>
# 力学の値を 1 つも書かない・乱数を引かない（読み手）。バイトで書く（改行は LF）。
import csv, sys, os, statistics, collections

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
SERVANT = 12
GROUPS = {"貴族": 0, "農民": 1, "工場労働者": 2, "資本家": 3, "職人": 11, "使用人": 12}
# spend_s の bucket（四半期の範囲）→ 窓。窓ごとに bucket を月数で重みづけて平均する。
WINDOWS = {"Q8-39": ["2"], "Q40-159": ["3"], "Q160-399": ["4", "5", "6", "7"]}
SVC_WINDOWS = {"Q8-39": (8, 39), "Q40-159": (40, 159), "Q160-399": (160, 399)}

def rows(path):
    with open(path, encoding="utf8", newline="") as f:
        return list(csv.DictReader(f, delimiter="\t"))

def fl(x):
    try:
        return float(x)
    except Exception:
        return float("nan")

res = collections.defaultdict(list)   # (nation, window, metric) -> [種ごとの値]
for seed in range(1, 11):
    sp = rows(os.path.join(src, f"spend_s{seed}.tsv"))
    sv = rows(os.path.join(src, f"svc_s{seed}.tsv"))
    cs = rows(os.path.join(src, f"class_s{seed}.tsv"))
    for wname, buckets in WINDOWS.items():
        # 国×階級ごと：月数で重みづけた paid・size・earn、層別の pay、家事奉仕の買い手の pay
        acc = collections.defaultdict(lambda: collections.defaultdict(float))
        for r in sp:
            if r["bucket"] not in buckets:
                continue
            n, c, mo = r["nation"], int(r["classId"]), fl(r["monthsCount"])
            key = (n, c)
            if r["kind"] == "class":
                acc[key]["months"] += mo
                acc[key]["paid"] += fl(r["payPerMonth"]) * mo
                acc[key]["size"] += fl(r["qtyPerMonth"]) * mo
                acc[key]["earn"] += fl(r["receiptPerMonth"]) * mo
            elif r["kind"] == "good":
                acc[key]["tier_" + r["tier"]] += fl(r["payPerMonth"]) * mo
                if r["slotName"] == "家事奉仕":
                    acc[key]["svcbuy"] += fl(r["payPerMonth"]) * mo
            elif r["kind"] == "tier":
                acc[key]["tier_" + r["tier"]] += fl(r["payPerMonth"]) * mo
        nations = sorted({k[0] for k in acc})
        for n in nations:
            for cname, c in GROUPS.items():
                a = acc.get((n, c))
                if not a or a["size"] <= 0:
                    continue
                res[(n, wname, f"{cname}_earnPerHead")].append(a["earn"] / a["size"])
                res[(n, wname, f"{cname}_paidPerHead")].append(a["paid"] / a["size"])
                if a["paid"] > 0:
                    for t in ("Staple", "Daily", "Luxury", "Service"):
                        res[(n, wname, f"{cname}_share{t}")].append(a["tier_" + t] / a["paid"])
            tot = sum(acc[(n, c)]["svcbuy"] for (nn, c) in acc if nn == n)
            if tot > 0:
                rich = sum(acc[(n, c)]["svcbuy"] for c in (0, 3) if (n, c) in acc) / tot
                merch = acc[(n, 10)]["svcbuy"] / tot if (n, 10) in acc else 0.0
                serv = acc[(n, SERVANT)]["svcbuy"] / tot if (n, SERVANT) in acc else 0.0
                res[(n, wname, "svcBuyerShareRich(貴族+資本家)")].append(rich)
                res[(n, wname, "svcBuyerShareMerchant")].append(merch)
                res[(n, wname, "svcBuyerShareServant")].append(serv)
                res[(n, wname, "svcBuyerShareMass(他)")].append(1 - rich - merch - serv)
    # svc_s：利用率・価格÷参照・不足の割合・使用人÷人口
    for wname, (q0, q1) in SVC_WINDOWS.items():
        by = collections.defaultdict(list)
        for r in sv:
            q = int(r["quarter"])
            if q0 <= q <= q1:
                by[r["nation"]].append(r)
        for n, rr in by.items():
            def med(col, f=lambda x: x):
                v = [f(fl(r[col])) for r in rr]
                v = [x for x in v if x == x]
                return statistics.median(v) if v else float("nan")
            cap = med("servantCapacityPerMonth")
            res[(n, wname, "svcUtilization(産出/能力)")].append(med("producedPerMonth") / cap if cap > 0 else float("nan"))
            res[(n, wname, "svcPriceOverRef")].append(med("priceSoldWeighted") / med("refPrice"))
            res[(n, wname, "svcShortageOverDemand")].append(med("shortagePerMonth") / med("demandPerMonth") if med("demandPerMonth") > 0 else float("nan"))
            res[(n, wname, "servantShareOfPop")].append(med("servantK") / med("totalPopulationK"))
            res[(n, wname, "servicePerServant")].append(med("servicePerServant"))
    # class_s：使用人の貯蓄÷所得（月）と人口比（開幕）
    for r in cs:
        if r["time"] == "open" and int(r["classId"]) == SERVANT:
            tot = sum(fl(x["sizeK"]) for x in cs if x["time"] == "open" and x["nation"] == r["nation"])
            res[(r["nation"], "open", "servantShareOfPop")].append(fl(r["sizeK"]) / tot)

metrics = sorted({k[2] for k in res})
nations = sorted({k[0] for k in res})
wins = ["open", "Q8-39", "Q40-159", "Q160-399"]
with open(os.path.join(out, "a0_servants.tsv"), "wb") as f:
    f.write(("nation\twindow\tmetric\tmedian\tmin\tmax\tn\n").encode("utf8"))
    for n in nations:
        for w in wins:
            for m in metrics:
                v = [x for x in res.get((n, w, m), []) if x == x]
                if v:
                    f.write(f"{n}\t{w}\t{m}\t{statistics.median(v):.5g}\t{min(v):.5g}\t{max(v):.5g}\t{len(v)}\n".encode("utf8"))
# 要約（国の中央の中央・読みやすい形）：計器つき 10 種の挙げた 8 国（svc_s にある国）だけ。他の国は年末の行しか無く階級の詳細が違う。
picked = sorted({k[0] for k in res if k[2] == "svcUtilization(産出/能力)"})
lines = [f"挙げた国（svc_s にある国）: {'・'.join(picked)}"]
for w in wins[1:]:
    lines.append(f"## 窓 {w}（国×10種の中央の、国をまたいだ中央・最小・最大）")
    for m in metrics:
        per_nation = []
        for n in picked:
            v = [x for x in res.get((n, w, m), []) if x == x]
            if v:
                per_nation.append(statistics.median(v))
        if per_nation:
            lines.append(f"- {m}: 中央 {statistics.median(per_nation):.4g}（最小 {min(per_nation):.4g}・最大 {max(per_nation):.4g}・国 {len(per_nation)}）")
with open(os.path.join(out, "a0_summary.txt"), "wb") as f:
    f.write(("\n".join(lines) + "\n").encode("utf8"))
print("\n".join(lines))
