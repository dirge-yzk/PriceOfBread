# (1246) c126ci 設計の材料：資本と投資の作り直しの数を基線 m1233 の生（keep_1233/base_ins）から読む。読むだけ。
# 使い方: python -X utf8 -I read_ci.py <base_ins> <出力.txt>
import sys, os, csv, statistics as st
from collections import defaultdict

INS = sys.argv[1]
OUT = sys.argv[2]
SEEDS = range(1, 11)
NAT9 = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "アメリカ", "清", "日本"]
CAP = 3  # 資本家


def rows(name, s):
    p = os.path.join(INS, f"{name}_s{s}.tsv")
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row


def fl(x):
    try:
        v = float(x)
    except ValueError:
        return float("nan")
    return v


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float("nan")


lines = []
def out(s=""):
    lines.append(s)

# ── GDP（nation_s：四半期ごとの年の名目 GDP）
gdp = {}  # (s, nation, q) -> 年の名目 GDP
for s in SEEDS:
    for ix, r in rows("nation", s):
        gdp[(s, r[ix["nation"]], int(r[ix["quarter"]]))] = fl(r[ix["nominalGdpPerYear"]])

# ── 階級（class_s：open・warm・q<N>）
cls = defaultdict(lambda: defaultdict(dict))  # (s,nation)[time][classId] = (sav, earned, paid, size)
for s in SEEDS:
    for ix, r in rows("class", s):
        nat = r[ix["nation"]]
        cls[(s, nat)][r[ix["time"]]][int(r[ix["classId"]])] = (fl(r[ix["savingsStock"]]), fl(r[ix["earnedPerMonth"]]), fl(r[ix["paidPerMonth"]]), fl(r[ix["sizeK"]]), int(r[ix["year"]]))


def qnum(t):
    return int(t[1:]) if t.startswith("q") else None

out("# (1246) 資本と投資の材料の数（基線 m1233・keep_1233/base_ins・種 1〜10・読むだけ）")
out("## 1. 開幕の貯蓄÷年の名目 GDP（GDP は nation_s の quarter 0 の nominalGdpPerYear・q1 は quarter 1）")
out("国\t時\t資本家の貯蓄÷GDP（種1）\t同 10 種の中央\t全階級の貯蓄÷GDP（種1）\t同 中央\t全階級の稼ぎ×12÷GDP（種1）\t同 中央\t資本家の稼ぎ×12÷GDP 中央\t資本家の支払い÷稼ぎ 中央")
for nat in NAT9:
    for t, qg in (("open", 0), ("warm", 0), ("q0", 0), ("q1", 1), ("q4", 4), ("q8", 8)):
        a = []; b = []; c = []; d = []; e = []
        for s in SEEDS:
            g = gdp.get((s, nat, qg), float("nan"))
            row = cls[(s, nat)].get(t)
            if not row or not (g > 0):
                a.append(float("nan")); b.append(float("nan")); c.append(float("nan")); d.append(float("nan")); e.append(float("nan")); continue
            sc = row.get(CAP, (float("nan"),) * 5)
            a.append(sc[0] / g)
            b.append(sum(v[0] for v in row.values() if v[0] == v[0]) / g)
            ce = sum(v[1] for v in row.values() if v[1] == v[1])
            c.append(ce * 12 / g if ce > 0 else float("nan"))
            d.append(sc[1] * 12 / g if sc[1] == sc[1] else float("nan"))
            e.append(sc[2] / sc[1] if sc[1] == sc[1] and sc[1] > 0 else float("nan"))
        out(f"{nat}\t{t}\t{a[0]:.2f}\t{med(a):.2f}\t{b[0]:.2f}\t{med(b):.2f}\t{c[0]:.2f}\t{med(c):.2f}\t{med(d):.2f}\t{med(e):.2f}")

# ── 十年ごとの資本家の貯蓄÷GDP と稼ぎ÷GDP
out("")
out("## 2. 十年ごとの中央（種 1〜10・各十年の class_s の行の中央）：資本家の貯蓄÷GDP／全階級の稼ぎ×12÷GDP／資本家の稼ぎ×12÷GDP／資本家の (稼ぎ−支払い)÷稼ぎ")
dec = defaultdict(lambda: defaultdict(list))
for (s, nat), tm in cls.items():
    if nat not in NAT9:
        continue
    for t, row in tm.items():
        q = qnum(t)
        if q is None:
            continue
        g = gdp.get((s, nat, q), float("nan"))
        if not (g > 0):
            continue
        yr = next(iter(row.values()))[4]
        dk = (yr // 10) * 10
        sc = row.get(CAP)
        if sc is None:
            continue
        ce = sum(v[1] for v in row.values() if v[1] == v[1])
        dec[(nat, dk)]["S"].append(sc[0] / g)
        dec[(nat, dk)]["E"].append(ce * 12 / g)
        if sc[1] == sc[1]:
            dec[(nat, dk)]["Ec"].append(sc[1] * 12 / g)
            if sc[1] > 0:
                dec[(nat, dk)]["sv"].append((sc[1] - sc[2]) / sc[1])
for nat in NAT9:
    cells = []
    for dk in range(1780, 1951, 10):
        v = dec.get((nat, dk))
        if not v:
            continue
        cells.append(f"{dk}:{med(v['S']):.2f}/{med(v['E']):.2f}/{med(v['Ec']):.2f}/{med(v['sv']):.2f}")
    out(f"{nat}\t" + "  ".join(cells))

# ── 建設（consf_s の本国格 home）
out("")
out("## 3. 建設の支払い÷GDP・予算が縛った州×月の割合・予算÷希望（consf_s の scope=home・四半期の和×4÷年 GDP・十年の中央・種 1〜10）")
cd = defaultdict(lambda: defaultdict(list))
for s in SEEDS:
    for ix, r in rows("consf", s):
        if r[ix["scope"]] != "home":
            continue
        nat = r[ix["nation"]]; q = int(r[ix["quarter"]]); yr = int(r[ix["year"]])
        g = gdp.get((s, nat, q), float("nan"))
        if not (g > 0):
            continue
        dk = (yr // 10) * 10
        paid = fl(r[ix["paidMoneySum"]]); jm = fl(r[ix["judgedRegionMonthsCount"]]); bb = fl(r[ix["budgetBoundRegionMonthsCount"]])
        des = fl(r[ix["desiredMoneySum"]]); bud = fl(r[ix["budgetMoneySum"]]); ear = fl(r[ix["flowEarnedLastMoneySum"]]); cpd = fl(r[ix["flowConsumptionPaidLastMoneySum"]])
        cd[(nat, dk)]["C"].append(paid * 4 / g)
        if jm > 0:
            cd[(nat, dk)]["B"].append(bb / jm)
        if des > 0:
            cd[(nat, dk)]["bd"].append(bud / des)
        if ear > 0:
            cd[(nat, dk)]["pe"].append(paid / ear)
            cd[(nat, dk)]["fe"].append((ear - cpd) / ear)
out("国\t十年:建設÷GDP/縛った割合/予算÷希望/建設÷投資家の前月の稼ぎ/(稼ぎ−消費)÷稼ぎ")
for nat in NAT9:
    cells = []
    for dk in range(1780, 1951, 10):
        v = cd.get((nat, dk))
        if not v:
            continue
        cells.append(f"{dk}:{med(v['C']):.3f}/{med(v['B']):.2f}/{med(v['bd']):.1f}/{med(v['pe']):.2f}/{med(v['fe']):.2f}")
    out(f"{nat}\t" + "  ".join(cells))

# ── 送電網（spend_s の extra 送電網・バケツの月あたりの支払いと量）と工場の電力の充足（factory_s）
out("")
out("## 4. 送電網：spend_s の extra〈送電網〉の月の支払い÷月の GDP と月の量（バケツ 7＝q268〜399・8＝q400〜末・種ごと）・保守と建設の支払い÷月の GDP")
for s in SEEDS:
    acc = defaultdict(dict)
    for ix, r in rows("spend", s):
        if r[ix["kind"]] != "extra":
            continue
        nat = r[ix["nation"]]
        if nat not in NAT9:
            continue
        b = int(r[ix["bucket"]]); q0 = int(r[ix["firstQuarter"]]); q1 = int(r[ix["lastQuarter"]])
        acc[(nat, b)][r[ix["slotName"]]] = (fl(r[ix["payPerMonth"]]), fl(r[ix["qtyPerMonth"]]), q0, q1)
    if s > 3:
        continue
    for (nat, b), m in sorted(acc.items(), key=lambda kv: (NAT9.index(kv[0][0]), kv[0][1])):
        if b < 7:
            continue
        q0 = next(iter(m.values()))[2]; q1 = next(iter(m.values()))[3]
        q1e = q1 if q1 >= 0 else 667
        gs = [gdp.get((s, nat, q), float("nan")) / 12 for q in range(q0, q1e + 1)]
        gm = med([x for x in gs if x > 0])
        gr = m.get("送電網"); mt = m.get("インフラ保守"); cn = m.get("建設の材料と工賃")
        f = lambda v: f"{v[0] / gm:.4f}" if v and gm > 0 else "-"
        out(f"種{s}\t{nat}\tバケツ{b}(q{q0}〜{q1})\t送電網÷GDP {f(gr)}\t送電網の量/月 {gr[1] if gr else 0:.2f}\t保守÷GDP {f(mt)}\t建設÷GDP {f(cn)}")

out("")
out("## 5. 工場の電力の充足（factory_s の elecFillMeanShare・十年の中央・種 1〜10）")
ef = defaultdict(list)
for s in SEEDS:
    for ix, r in rows("factory", s):
        nat = r[ix["nation"]]
        if nat not in NAT9:
            continue
        yr = int(r[ix["year"]])
        if yr < 1880:
            continue
        ef[(nat, (yr // 10) * 10)].append(fl(r[ix["elecFillMeanShare"]]))
for nat in NAT9:
    out(f"{nat}\t" + "  ".join(f"{dk}:{med(ef[(nat, dk)]):.3f}" for dk in range(1880, 1951, 10) if ef.get((nat, dk))))

with open(OUT, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(lines) + "\n")
print(OUT, len(lines))
