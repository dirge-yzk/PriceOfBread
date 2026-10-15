#!/usr/bin/env python
# (359) 消費の入口 初期値の線 段0：旗 0（A）・旗 1（U）・旗 1＋置き直し（UR1／UR2）の短い走行（種 1・16 四半期・英だけの計器）を並べる読み手（読むだけ）。
# 使い方：PYTHONUTF8=1 python an_ce_s0359.py <A の計器の出力フォルダ> <腕の名前=フォルダ> ... （例：U=/d/.../U UR1=/d/.../UR1）
# 出す表：①英の大衆（農民・工場労働者・職人・使用人）の四半期ごとの 貯蓄÷年所得・所得・支払い（class_s）
#         ②英の大衆の層の数（ce_s の C 行の和：食料・日用品・奢侈の支払い・層2 の上限・層3 の予算・取り崩し）
#         ③英の本国の州の数の変化と civilWarMonths（region_s／nation_s）
#         ④英の大衆へ所得を払った書き手の上位（flow_s の giveIncome／settle・ソースの位置つき）
import csv, sys, collections

MASS = ["農民", "工場労働者", "職人", "使用人"]
NAT = "イギリス"


def rows(path):
    return csv.DictReader(open(path, encoding="utf-8"), delimiter="\t")


def f(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float("nan")


def class_table(d):
    agg = collections.OrderedDict()
    for r in rows(f"{d}/class_s1.tsv"):
        if r["nation"] != NAT or r["className"] not in MASS:
            continue
        a = agg.setdefault(r["time"], [0.0, 0.0, 0.0, 0.0])
        a[0] += f(r["sizeK"]); a[1] += f(r["savingsStock"]); a[2] += f(r["earnedPerMonth"]) if f(r["earnedPerMonth"]) == f(r["earnedPerMonth"]) else 0.0; a[3] += f(r["paidPerMonth"]) if f(r["paidPerMonth"]) == f(r["paidPerMonth"]) else 0.0
    out = []
    for t, a in agg.items():
        out.append(f"  {t:5s} sizeK {a[0]:8.0f} 貯蓄 {a[1]:10.0f} 貯蓄÷年所得 {a[1] / (12 * a[2]) if a[2] > 0 else float('nan'):6.2f} 所得/月 {a[2]:8.0f} 支払い/月 {a[3]:8.0f}")
    return out


def layer_table(d):
    cols = ["popK", "earnedLastPerMonth", "foodPaidPerMonth", "dailyPaidPerMonth", "luxuryPaidPerMonth", "consumptionPaidPerMonth", "savingsDrawPerMonth", "layerCap2PerMonth", "layerBud3PerMonth"]
    agg = collections.OrderedDict()
    for r in rows(f"{d}/ce_s1.tsv"):
        if r["scope"] != "C" or r["nation"] != NAT or r["className"] not in MASS:
            continue
        q = int(r["quarter"])
        a = agg.setdefault(q, collections.defaultdict(float))
        for c in cols:
            v = f(r[c])
            if v == v:
                a[c] += v
    out = ["  q  earnedLast  food  daily  lux  paid  draw  cap2  bud3  bud3/cap2  daily/lux"]
    for q, a in agg.items():
        if q > 12:
            break
        out.append(f"  {q:2d} {a['earnedLastPerMonth']:8.0f} {a['foodPaidPerMonth']:7.0f} {a['dailyPaidPerMonth']:7.0f} {a['luxuryPaidPerMonth']:7.0f} {a['consumptionPaidPerMonth']:7.0f} {a['savingsDrawPerMonth']:7.0f} {a['layerCap2PerMonth']:8.0f} {a['layerBud3PerMonth']:8.0f} {a['layerBud3PerMonth'] / a['layerCap2PerMonth'] if a['layerCap2PerMonth'] > 0 else float('nan'):8.2f} {a['dailyPaidPerMonth'] / a['luxuryPaidPerMonth'] if a['luxuryPaidPerMonth'] > 0 else float('nan'):8.2f}")
    return out


def region_changes(d):
    by = {}
    for r in rows(f"{d}/region_s1.tsv"):
        if r["nation"] == NAT and r["tier"] == "Core":
            by.setdefault(int(r["quarter"]), set()).add(r["region"])
    out, prev = [], None
    for q in sorted(by):
        if prev is not None and by[q] != prev:
            out.append(f"  Q{q}: 減った {sorted(prev - by[q])} 増えた {sorted(by[q] - prev)}")
        prev = by[q]
    civ = [(r["quarter"], r["civilWarMonths"]) for r in rows(f"{d}/nation_s1.tsv") if r["nation"] == NAT and f(r["civilWarMonths"]) > 0]
    out.append(f"  英の civilWarMonths＞0 の四半期: {civ[:8]}")
    return out


def writers(d):
    mass_ids = {"1", "2", "11", "12"}
    acc = collections.defaultdict(float)
    for r in rows(f"{d}/flow_s1.tsv"):
        if r["classId"] in mass_ids and r["kind"] in ("giveIncome", "settle"):
            acc[(int(r["quarter"]), r["kind"], r["sourceFile"], r["sourceLine"])] += f(r["amountPerMonth"])
    keys = sorted({k[1:] for k in acc})
    qs = range(0, 8)
    tot = {k: sum(acc.get((q,) + k, 0) for q in qs) for k in keys}
    out = ["  書き手（種類 ファイル 行）: Q0〜Q7 の月平均"]
    for k in sorted(keys, key=lambda k: -abs(tot[k]))[:6]:
        out.append(f"  {k}: " + " ".join(f"{acc.get((q,) + k, 0):8.0f}" for q in qs))
    return out


for arm in sys.argv[1:]:
    name, d = arm.split("=", 1) if "=" in arm else (arm, arm)
    print(f"##### {name}")
    print("① 英の大衆（農民・工場労働者・職人・使用人）"); print("\n".join(class_table(d)))
    print("② 層の数（英の大衆の和・月平均）"); print("\n".join(layer_table(d)))
    print("③ 英の本国の州"); print("\n".join(region_changes(d)))
    print("④ 英の大衆への所得の書き手"); print("\n".join(writers(d)))
