# (1166) c115sv 記録の読み手：清・英・マラータ・日本・米の兵の数（nation_s soldierK）と軍事費の取り分（militarySpendingEndShare）の種の中央値を年代ごとに。
#   指示の上の門の量（admrule_s soldierK÷seatsK・志願の国）の種ごとの最大と、その国と四半期も並べる。
# 引数：名=生のフォルダ ...（例 M=D:/PoB_work/keep_1164/base_ins B=D:/…/ins）  種 1〜10
import sys, os, csv, statistics
sys.stdout.reconfigure(newline="\n")
NATS = ["清", "イギリス", "マラータ同盟", "日本", "アメリカ"]
YEARS = [1800, 1825, 1850, 1875, 1900, 1925, 1949]


def rd(p):
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            yield ix, x


def fnum(s):
    try:
        return float(s)
    except ValueError:
        return float("nan")


for arg in sys.argv[1:]:
    name, raw = arg.split("=", 1)
    sold = {}; mil = {}
    for s in range(1, 11):
        p = os.path.join(raw, f"nation_s{s}.tsv")
        if not os.path.exists(p):
            continue
        for ix, x in rd(p):
            y = int(x[ix["year"]]); q = int(x[ix["quarter"]])
            if q % 4 != 0 or y not in YEARS or x[ix["nation"]] not in NATS:
                continue
            k = (x[ix["nation"]], y)
            sold.setdefault(k, []).append(fnum(x[ix["soldierK"]]))
            if "militarySpendingEndShare" in ix:
                mil.setdefault(k, []).append(fnum(x[ix["militarySpendingEndShare"]]))
    print(f"# {name} 生={raw}（種の中央値・年の頭の四半期）")
    print("国\t" + "\t".join(f"兵{y}" for y in YEARS) + "\t" + "\t".join(f"軍{y}" for y in YEARS))
    for n in NATS:
        a = [f"{statistics.median(sold[(n, y)]):.1f}" if (n, y) in sold else "-" for y in YEARS]
        b = [f"{statistics.median(mil[(n, y)]):.3f}" if (n, y) in mil else "-" for y in YEARS]
        print(n + "\t" + "\t".join(a) + "\t" + "\t".join(b))
