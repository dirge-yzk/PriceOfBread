#!/usr/bin/env python
# (353) 消費の入口 段0 (2)：開幕の歳入の薄さの台帳分解＝旗 0（A）と旗 1（U）の国庫の台帳の区分ごとの月の差分と、大衆の支出・使える所得を並べる読み手（読むだけ）。
# 使い方：PYTHONUTF8=1 python an_ce_rev.py <A の計器の出力フォルダ> <U の計器の出力フォルダ> <国名> [種=1]
# 読む物：fiscal_s（国×月）の ledgerTax／ledgerTariff／ledgerStateMonopoly／ledgerOtherIn・directTaxNetPerMonth／indirectRevenuePerMonth・nation_s の nominalGdpPerYear（国×四半期）、
#         ce_s の C 行（国×四半期×階級）の consumptionPaidPerMonth・earnedLastPositivePerMonth・savingsDraw（貯蓄の取り崩し）・popK。窓＝Q0–Q1／Q2–Q3／Q4–Q8（月は 3 か月＝1 四半期）。
# 歳入÷年GDP＝(ledgerTax＋ledgerTariff＋ledgerStateMonopoly)×12÷nominalGdpPerYear（StartWarmupTests〈開幕の歳入は所得税なしの直接税の段で年GDPの史実の桁〉は税・関税・専売の月の和×12÷年GDP）。
# 基数の割り当て：大衆（農民・工場労働者・職人・使用人）／資本家・富裕層（貴族・資本家・商人）の支出（consumptionPaid）のうち、税が掛かる消費の分は ce_s の階級別の支払いで按分して読む（税の対象の財の表は spend_s の財×階級の行を後で足す）。
import csv, sys, collections

dA, dU, nat = sys.argv[1], sys.argv[2], sys.argv[3]
seed = sys.argv[4] if len(sys.argv) > 4 else "1"
wins = [(0, 1), (2, 3), (4, 8)]
MASS = ["農民", "工場労働者", "職人", "使用人"]
RICH = ["貴族", "資本家", "商人"]


def read(path):
    rd = csv.reader(open(path, encoding="utf-8"), delimiter="\t")
    hdr = next(rd)
    return hdr, {h: i for i, h in enumerate(hdr)}, list(rd)


def fiscal(d):
    hdr, ix, rows = read(f"{d}/fiscal_s{seed}.tsv")
    out = collections.defaultdict(lambda: collections.defaultdict(list))
    cols = ["ledgerTaxPerMonth", "ledgerTariffPerMonth", "ledgerStateMonopolyPerMonth", "ledgerOtherInPerMonth", "directTaxNetPerMonth", "indirectRevenuePerMonth"]
    for r in rows:
        if r[ix["nation"]] != nat:
            continue
        mi = int(r[ix["monthIndex"]])
        q = (mi - 2) // 3 if mi >= 2 else 0   # monthIndex 2 が 1783 年の最初の月（Q0）
        for c in cols:
            try:
                out[q][c].append(float(r[ix[c]]))
            except ValueError:
                pass
    return out


def gdp(d):
    hdr, ix, rows = read(f"{d}/nation_s{seed}.tsv")
    g = {}
    for r in rows:
        if r[ix["nation"]] == nat:
            g[int(r[ix["quarter"]])] = float(r[ix["nominalGdpPerYear"]])
    return g


def ce(d):
    hdr, ix, rows = read(f"{d}/ce_s{seed}.tsv")
    acc = collections.defaultdict(lambda: collections.defaultdict(float))
    for r in rows:
        if r[0] != "C" or r[ix["nation"]] != nat:
            continue
        q = int(r[ix["quarter"]])
        cl = r[ix["className"]]
        for lo, hi in wins:
            if lo <= q <= hi:
                a = acc[(cl, (lo, hi))]
                a["n"] += 1
                for h in ("popK", "consumptionPaidPerMonth", "earnedLastPositivePerMonth", "savingsDrawPerMonth", "transferInPerMonth", "taxPaidPerMonth", "layerCap2PerMonth", "layerBud3PerMonth"):
                    if h in ix:
                        try:
                            a[h] += float(r[ix[h]])
                        except ValueError:
                            pass
    return acc


FA, FU = fiscal(dA), fiscal(dU)
GA, GU = gdp(dA), gdp(dU)
CA, CU = ce(dA), ce(dU)
for lo, hi in wins:
    print(f"== 窓 Q{lo}–Q{hi}（{nat}）A＝旗 0／U＝旗 1")
    for lab, F, G in (("A", FA, GA), ("U", FU, GU)):
        s = collections.defaultdict(float); n = 0
        for q in range(lo, hi + 1):
            if q not in F:
                continue
            n += 1
            for c, v in F[q].items():
                s[c] += sum(v) / len(v)   # 四半期の月平均
        if n == 0:
            continue
        g = sum(G.get(q, 0.0) for q in range(lo, hi + 1)) / max(1, sum(1 for q in range(lo, hi + 1) if q in G))
        tot = (s["ledgerTaxPerMonth"] + s["ledgerTariffPerMonth"] + s["ledgerStateMonopolyPerMonth"]) / n
        print(f"  {lab}: 税 {s['ledgerTaxPerMonth']/n:9.1f} 関税 {s['ledgerTariffPerMonth']/n:9.1f} 専売 {s['ledgerStateMonopolyPerMonth']/n:9.1f} その他の受取 {s['ledgerOtherInPerMonth']/n:9.1f} "
              f"| 直接税の純額 {s['directTaxNetPerMonth']/n:9.1f} 間接 {s['indirectRevenuePerMonth']/n:9.1f} | 年GDP {g:11.1f} 歳入÷年GDP {tot*12/g if g > 0 else float('nan'):6.4f}")
    # 大衆と富裕層の支出・所得（月の平均×人口）
    for lab, C in (("A", CA), ("U", CU)):
        tm = tr = 0.0
        row = []
        for grp, names in (("大衆", MASS), ("富裕層", RICH)):
            paid = Y = draw = pop = 0.0
            for cl in names:
                a = C.get((cl, (lo, hi)))
                if not a or a["n"] == 0:
                    continue
                n = a["n"]
                paid += a["consumptionPaidPerMonth"] / n; Y += a["earnedLastPositivePerMonth"] / n; draw += a["savingsDrawPerMonth"] / n; pop += a["popK"] / n
            row.append(f"{grp}: 支払い {paid:10.1f} 所得 {Y:10.1f} 取り崩し {draw:9.1f} 支払い÷所得 {paid/Y if Y > 0 else float('nan'):5.2f}")
        print(f"  {lab}: " + " | ".join(row))
