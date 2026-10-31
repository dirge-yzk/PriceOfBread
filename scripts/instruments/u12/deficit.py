# (726) u12：U4（流れだけの予算）の不足の大きさと、与信の余地・貯蓄の在庫÷年所得を、基線 m702 の計器つき 10 種の生（keep_719/base_ins）から読むだけ。
# 使い方：python deficit.py <生のフォルダ> <出力> [種の数=10] [種の起点=1]
# 量（本国格 home・国×十年・種の中央値）：
#   S/Y＝投資家の貯蓄の在庫（四半期末の値の十年平均）÷投資家の前月の所得の年換算（consf_s の flowEarnedLastMoneySum×12÷判定の州×月で割らずに、十年の和×12÷月の数で年へ）
#   old/flow＝今の予算の和÷流れの影の予算の和（u4a の record ② と同じ量）
#   fcut＝(影の希望−影の計画)÷影の希望＝流れだけにしたときの不足の割合（静的な反事実）
#   D/paid＝影の不足の和÷今の実際の建設の支払いの和（不足が今の建設の何倍か）
#   D/Y＝影の不足の年換算÷投資家の前月の所得の年換算（不足が投資家の年所得の何倍か）
#   room/D＝与信の余地（min(準備の上限, 返せる額)−与信の残高・十年の平均）÷影の不足の年換算（余地が不足の何年分か・国の全ての州の不足で割る）
#   rep/D＝返せる額の上限（首都の投資家の年所得×5）÷影の不足の年換算
#   repB＝返せる額が準備の側より小さかった日の割合
#   want/Y＝影の希望の年換算÷投資家の年所得／paid/Y＝今の建設の支払いの年換算÷投資家の年所得／C/Y＝投資家の前月の消費の支払い÷前月の所得（影の判定のある州×月の和）
import csv, sys, statistics as st, collections as C, math
R, OUT = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
SB = int(sys.argv[4]) if len(sys.argv) > 4 else 1
NAT = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"]
def dec(y): return 1783 if y < 1790 else (y // 10) * 10
def div(a, b): return a / b if b > 0 else float("nan")
res = C.defaultdict(lambda: C.defaultdict(list))
for s in range(SB, SB + NS):
    a = C.defaultdict(lambda: C.defaultdict(float))
    with open(f"{R}/consf_s{s}.tsv", encoding="utf-8") as f:
        for x in csv.DictReader(f, delimiter="\t"):
            n = x["nation"]
            if n not in NAT: continue
            try: y = int(x["year"]); sc = x["scope"]
            except Exception: continue
            t = a[(n, dec(y), sc)]
            for k in ("investorSavingsStock", "flowEarnedLastMoneySum", "flowConsumptionPaidLastMoneySum", "budgetMoneySum", "flowBudgetMoneySum",
                      "flowDesiredMoneySum", "flowPlannedMoneySum", "paidMoneySum", "desiredMoneySum", "plannedMoneySum"):
                t[k] += float(x[k] or 0)
            t["quarters"] += 1
            t["flowAvail"] += float(x["flowAvailableRegionMonthsCount"] or 0)
            t["judged"] += float(x["judgedRegionMonthsCount"] or 0)
    cr = C.defaultdict(lambda: C.defaultdict(float))
    with open(f"{R}/nation_s{s}.tsv", encoding="utf-8") as f:
        for x in csv.DictReader(f, delimiter="\t"):
            n = x["nation"]
            if n not in NAT: continue
            y = int(x["year"]); t = cr[(n, dec(y))]
            rc = float(x["creditReserveCapMeanStock"] or "nan"); rp = float(x["creditRepayableCapMeanStock"] or "nan")
            if rc == rc: t["rc"] += rc; t["nrc"] += 1
            if rp == rp: t["rp"] += rp; t["nrp"] += 1
            t["days"] += float(x["creditJudgedDaysCount"] or 0); t["bdays"] += float(x["creditRepayableBoundDaysCount"] or 0)
    with open(f"{R}/money_s{s}.tsv", encoding="utf-8") as f:
        for x in csv.DictReader(f, delimiter="\t"):
            n = x["nation"]
            if n not in NAT or not x["time"].startswith("q"): continue
            y = int(x["year"]); t = cr[(n, dec(y))]
            t["loans"] += float(x["outstandingLoansStock"] or 0); t["nl"] += 1
    for (n, d, sc), t in a.items():
        if sc != "home": continue
        q = t["quarters"]
        # 所得と消費は影の判定がある州×月の和＝年換算は (和 ÷ 判定のある州×月) × 判定の州×月 ÷ 月の数 × 12 の形ではなく、
        # 州の和の月平均＝和 ÷ (月の数) × 12（州×月の和を月の数で割る＝州の和の月の値）。判定のない月の分だけ小さい（fav が 1 に近い窓で読む）。
        months = 3 * q
        Y = div(t["flowEarnedLastMoneySum"], months) * 12
        Sav = div(t["investorSavingsStock"], q)
        D = t["flowDesiredMoneySum"] - t["flowPlannedMoneySum"]
        Dy = div(D, months) * 12
        c = cr[(n, d)]
        rc = div(c["rc"], c["nrc"]); rp = div(c["rp"], c["nrp"]); L = div(c["loans"], c["nl"])
        cap = min(rc, rp) if rc == rc and rp == rp else rc
        m = {
            "S/Y": div(Sav, Y),
            "old/flow": div(t["budgetMoneySum"], t["flowBudgetMoneySum"]),
            "fcut": div(D, t["flowDesiredMoneySum"]),
            "D/paid": div(D, t["paidMoneySum"]),
            "D/Y": div(Dy, Y),
            "room/D": div(cap - L, Dy),
            "rep/D": div(rp, Dy),
            "L/D": div(L, Dy),
            "repB": div(c["bdays"], c["days"]),
            "want/Y": div(div(t["flowDesiredMoneySum"], months) * 12, Y),
            "paid/Y": div(div(t["paidMoneySum"], months) * 12, Y),
            "C/Y": div(t["flowConsumptionPaidLastMoneySum"], t["flowEarnedLastMoneySum"]),
        }
        for k, v in m.items(): res[(n, d)][k].append(v)
def med(v):
    v = [x for x in v if x == x and not math.isinf(x)]; return st.median(v) if v else float("nan")
def f3(v): return "nan" if v != v else (f"{v:.3g}")
K = ["S/Y", "old/flow", "fcut", "D/paid", "D/Y", "room/D", "rep/D", "L/D", "repB", "want/Y", "paid/Y", "C/Y"]
with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write("# (726) u12：U4（流れだけの予算）の不足の大きさ・与信の余地・貯蓄の在庫÷年所得（基線 m702 の計器つき 10 種の生・本国格・国×十年・種の中央値）。列の定義は deficit.py の頭。\n")
    o.write("nation\tdecade\t" + "\t".join(K) + "\n")
    for n in NAT:
        for d in sorted({k[1] for k in res if k[0] == n}):
            o.write(f"{n}\t{d}\t" + "\t".join(f3(med(res[(n, d)][k])) for k in K) + "\n")
print("ok", OUT)
