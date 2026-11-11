# (922) c80cw 同じ盤面の新÷旧の表（★(919)・読むだけ）。腕と基線とヌル腕の生を同じ定義で数え、腕÷基線と N1÷基線・N2÷基線（ヌル腕の幅）を並べる。
#   使い方: python tab922.py <基線の ins> <名=ins>...   （例 base=D:/…/keep_919/base_ins n1=… n2=… armB=…）。SB=11 で種 11〜20。
#   列（全て 10 種の和か中央）：
#     内戦の月・弾薬／火薬／鉛の内戦の月あたりの需要と産出（cwmult_s・市場の LastDemand・LastProduced の和）
#     内戦の月の warMult の平均 a_c（cwmult_s の actual）と warWear の倍の代理 1+ArmsWarWearScale×(a_c−1)（EconomyStep.cs の warWear の式・ArmsWarWearScale＝1.0 を読む＝計器の列は無い＝代理）
#     内戦の四半期の装備の在庫の平均（stock_s の capitalRifle/Artillery/AmmunitionStock・計器 9 国のうち cwmult_s で内戦の月が 1 以上の四半期・国の名で突き合わせ）
#     内戦の件数・長さの中央と平均（wars_s の civilWarFlag=1）
#     内戦の決着（cpmed_s の civilFlag=1・scoreIndex の符号＝Diplomacy.cs の `var winner = war.Score >= 0 ? war.A : war.B`・blankFlag＝白紙）＝A（親の国）の勝ち・B（反乱側）の勝ち・白紙
#     1900 年より後に始まった対外戦の本数・長さの中央と平均（wars_s の civilWarFlag=0・startYear>1900）
#   ⚠限界：cpmed_s は講和で終わった戦争だけ（国の消滅で終わった内戦は行が無い）＝件数は wars_s と合わない（両方を並べる）。stock_s は計器 9 国だけ。
import sys, os, csv, statistics
SB = int(os.environ.get('SB', '1')); SEEDS = range(SB, SB + 10)
WEAR = 1.0   # Coefficients.ArmsWarWearScale（grep で確かめた値・2026-11-07）

def rows(d, name, s):
    p = os.path.join(d, f"{name}_s{s}.tsv")
    if not os.path.exists(p): return
    with open(p, encoding="utf-8") as f:
        yield from csv.DictReader(f, delimiter="\t")

def stats(d):
    o = {}
    civ = 0; act = 0.0; q = {k: 0.0 for k in ("ammoD", "ammoP", "powD", "powP", "leadD", "leadP")}
    cols = {"ammoD": "ammunitionDemandCivilQty", "ammoP": "ammunitionProducedCivilQty", "powD": "gunpowderDemandCivilQty",
            "powP": "gunpowderProducedCivilQty", "leadD": "leadDemandCivilQty", "leadP": "leadProducedCivilQty"}
    stk = {"capitalRifleStock": [], "capitalArtilleryStock": [], "capitalAmmunitionStock": []}
    cl, fl, fl1900 = [], [], []
    res = {"A": 0, "B": 0, "blank": 0}
    for s in SEEDS:
        civq = set()
        for r in rows(d, "cwmult", s):
            c = int(r["civilWarMonthsCount"]); civ += c; act += float(r["warMultActualCivilSumIndex"])
            for k, col in cols.items(): q[k] += float(r[col])
            if c > 0: civq.add((r["quarter"], r["nation"]))
        for r in rows(d, "stock", s):
            t = r["time"]
            if t.startswith("q") and (t[1:], r["nation"]) in civq:
                for k in stk: stk[k].append(float(r[k]))
        for r in rows(d, "wars", s):
            m = int(r["months"])
            if r["civilWarFlag"] == "1": cl.append(m)
            else:
                fl.append(m)
                if int(r["startYear"]) > 1900: fl1900.append(m)
        for r in rows(d, "cpmed", s):
            if r["civilFlag"] != "1": continue
            if r["blankFlag"] == "1": res["blank"] += 1
            elif float(r["scoreIndex"]) >= 0: res["A"] += 1
            else: res["B"] += 1
    o["内戦の月"] = civ
    for k, lab in (("ammoD", "弾薬の需要"), ("ammoP", "弾薬の産出"), ("powD", "火薬の需要"), ("powP", "火薬の産出"), ("leadD", "鉛の需要"), ("leadP", "鉛の産出")):
        o[lab + "÷内戦の月"] = q[k] / civ if civ else float("nan")
    ac = act / civ if civ else float("nan")
    o["内戦の月の warMult の平均"] = ac
    o["warWear の倍の代理"] = 1 + WEAR * (ac - 1)
    for k, lab in (("capitalRifleStock", "小銃の在庫"), ("capitalArtilleryStock", "大砲の在庫"), ("capitalAmmunitionStock", "弾薬の在庫")):
        o[lab + "（内戦の四半期・9国）"] = statistics.mean(stk[k]) if stk[k] else float("nan")
    o["内戦の四半期の行（9国）"] = len(stk["capitalRifleStock"])
    o["内戦の件数（wars_s）"] = len(cl)
    o["内戦の長さの中央"] = statistics.median(cl) if cl else float("nan")
    o["内戦の長さの平均"] = statistics.mean(cl) if cl else float("nan")
    o["内戦の講和の件数（cpmed_s）"] = sum(res.values())
    o["内戦の決着 A（親の国）の勝ち"] = res["A"]
    o["内戦の決着 B（反乱側）の勝ち"] = res["B"]
    o["内戦の決着 白紙"] = res["blank"]
    o["内戦の B の勝ちの割合"] = res["B"] / sum(res.values()) if sum(res.values()) else float("nan")
    o["対外戦の件数"] = len(fl)
    o["1900年より後の対外戦の件数"] = len(fl1900)
    o["1900年より後の対外戦の長さの中央"] = statistics.median(fl1900) if fl1900 else float("nan")
    o["1900年より後の対外戦の長さの平均"] = statistics.mean(fl1900) if fl1900 else float("nan")
    return o

base = stats(sys.argv[1])
others = [(a.split("=", 1)[0], stats(a.split("=", 1)[1])) for a in sys.argv[2:]]
print(f"# (922) 同じ盤面の新÷旧の表（基線 {sys.argv[1]}・種 {SB}〜{SB+9}）")
print("列\t基線\t" + "\t".join(n for n, _ in others) + "\t" + "\t".join(n + "÷基線" for n, _ in others))
def f(x): return f"{x:.6g}" if isinstance(x, float) else str(x)
for k in base:
    vals = [o[k] for _, o in others]
    rat = [(v / base[k] if base[k] else float("nan")) for v in vals]
    print(f"{k}\t{f(base[k])}\t" + "\t".join(f(v) for v in vals) + "\t" + "\t".join(f"{r:.4f}" for r in rat))
