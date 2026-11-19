# (1246) 稼ぎの和が GDP の何倍か：cearn_s（州の現金の入りの内訳）を本国格で国ごとに足し、年の名目 GDP（nation_s）で割る。読むだけ。
# 使い方: python -X utf8 -I read_earn.py <base_ins> <出力.txt>
import sys, os, csv, statistics as st
from collections import defaultdict
INS, OUT = sys.argv[1], sys.argv[2]
NAT = ["イギリス", "フランス", "プロイセン", "ロシア", "清", "アメリカ"]
QS = [1, 2, 4, 8, 268, 468, 520]  # 1783 q1・q2・1784・1785・1850・1900・1913
COLS = ["inMarketPerQuarter", "inExportPerQuarter", "inStatePerQuarter", "inCreditPerQuarter", "inFromPopsPerQuarter", "inMigrationPerQuarter", "inOtherPerQuarter"]


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float("nan")

acc = defaultdict(lambda: defaultdict(list))
for s in range(1, 4):
    gdp = {}
    with open(os.path.join(INS, f"nation_s{s}.tsv"), encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            gdp[(row[ix["nation"]], int(row[ix["quarter"]]))] = fl(row[ix["nominalGdpPerYear"]])
    sums = defaultdict(lambda: defaultdict(float))
    with open(os.path.join(INS, f"cearn_s{s}.tsv"), encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            q = int(row[ix["quarter"]]); nat = row[ix["nation"]]
            if q not in QS or nat not in NAT or row[ix["tier"]] == "Colonial":
                continue
            d = sums[(nat, q)]
            for c in COLS:
                v = fl(row[ix[c]])
                if v == v:
                    d[c] += v
            for c in ("factoryOutputRefValuePerMonth", "extractionOutputRefValuePerMonth", "earnedGrossPerMonth"):
                v = fl(row[ix[c]])
                if v == v:
                    d[c] += v
    for (nat, q), d in sums.items():
        g = gdp.get((nat, q), float("nan"))
        if not (g > 0):
            continue
        for c in COLS:
            acc[(nat, q)][c].append(d[c] * 4 / g)
        acc[(nat, q)]["inAll"].append(sum(d[c] for c in COLS) * 4 / g)
        acc[(nat, q)]["earnedGross"].append(d["earnedGrossPerMonth"] * 12 / g)
        acc[(nat, q)]["outputRef"].append((d["factoryOutputRefValuePerMonth"] + d["extractionOutputRefValuePerMonth"]) * 12 / g)

lines = ["# (1246) 本国格の州の現金の入り（cearn_s・四半期×4）÷年の名目 GDP（種 1〜3 の中央）",
         "国\t四半期\t入りの和\t市場\t輸出\t国から\t与信\tPOP から\t移民\tその他\t稼ぎの総額(earnedGross×12)\t工場と採取の産出(参照価格×12)"]
for nat in NAT:
    for q in QS:
        a = acc.get((nat, q))
        if not a:
            continue
        m = lambda k: st.median(a[k]) if a[k] else float("nan")
        lines.append(f"{nat}\tq{q}\t{m('inAll'):.2f}\t" + "\t".join(f"{m(c):.3f}" for c in COLS) + f"\t{m('earnedGross'):.2f}\t{m('outputRef'):.2f}")
with open(OUT, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(lines) + "\n")
print(OUT, len(lines))
