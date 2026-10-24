# (524) 実質賃金と利潤の割合の年代表（記録だけ・採否に使わない）：6 国（英・仏・普・米・日・清）の 10 種の中央値（年の最後の行）。
#   実質賃金＝labor_s の plainRealPriceIndex（工場の労働 1 単位の実質の値段の指数＝名目の値段÷物価水準）。
#   利潤の割合＝class_s の earnedPerMonth で 資本家÷（資本家＋工場労働者）（所得の割合の代理＝付加価値の分け方そのものではない・地代と国営の持分は入らない）。
# 使い方: PYTHONUTF8=1 python wage_profit_decades.py <名前>=<計器の ins フォルダ> [...]（1 番目を分母に比）
import os, sys, csv, statistics as st
NATIONS = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "清"]
YEARS = [1800, 1820, 1850, 1880, 1900, 1913, 1930, 1950]
def rows(p):
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        for row in r:
            if len(row) == len(h): yield dict(zip(h, row))
def read(d):
    wage, prof = {}, {}
    for sd in range(1, 21):
        p = os.path.join(d, f"labor_s{sd}.tsv")
        if os.path.exists(p):
            for x in rows(p):
                try: wage[(sd, x["nation"], int(x["year"]))] = float(x["plainRealPriceIndex"])
                except ValueError: pass
        p = os.path.join(d, f"class_s{sd}.tsv")
        if os.path.exists(p):
            tmp = {}
            for x in rows(p):
                try:
                    k = (sd, x["nation"], int(x["year"])); c = int(x["classId"]); e = float(x["earnedPerMonth"])
                except ValueError: continue
                tmp.setdefault(k, {})[c] = e
            for k, m in tmp.items():
                cap, wor = m.get(3), m.get(2)
                if cap is not None and wor is not None and cap + wor > 0: prof[k] = cap / (cap + wor)
    return wage, prof
def med(tab, n, y):
    vs = [v for (sd, nn, yy), v in tab.items() if nn == n and yy == y and v == v]
    return (st.median(vs), len(vs)) if vs else (float("nan"), 0)
arms = [(a.split("=", 1)[0], read(a.split("=", 1)[1])) for a in sys.argv[1:]]
for qi, qn in [(0, "実質賃金 plainRealPriceIndex"), (1, "利潤の割合 資本家÷（資本家＋工場労働者）の所得")]:
    print(f"# {qn}（10 種の中央値・年の最後の行）。比は {arms[0][0]} を分母")
    for n in NATIONS:
        print(f"## {n}")
        print("年\t" + "\t".join(k for k, _ in arms) + "\t" + "\t".join(f"{k}/{arms[0][0]}" for k, _ in arms[1:]))
        for y in YEARS:
            vals = [med(t[qi], n, y) for _, t in arms]
            b = vals[0][0]
            print(f"{y}\t" + "\t".join(f"{v:.4g}(n{c})" for v, c in vals) + "\t" + "\t".join((f"{v / b:.3f}" if b == b and b != 0 and v == v else "nan") for v, _ in vals[1:]))
