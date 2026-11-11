# (973) c87mw 同じ盤面の新÷旧（腕の cwmult_s で、実際の値＝接触の割合の規則と、計器が同じ盤面で数えた旧の規則〔MilitaryWarMult×(1+WarMobilizationDepth×厭戦)〕を比べる・読むだけ）
#   使い方: python sameboard973.py <腕の ins> [SB]
import sys, os, csv, collections
D = sys.argv[1]; SB = int(sys.argv[2]) if len(sys.argv) > 2 else 1
o = collections.Counter()
for s in range(SB, SB + 10):
    p = os.path.join(D, f"cwmult_s{s}.tsv")
    if not os.path.exists(p): continue
    o["seeds"] += 1
    sold = {}
    pn = os.path.join(D, f"nation_s{s}.tsv")
    with open(pn, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"): sold[(r["quarter"], r["nation"])] = float(r["soldierK"])
    with open(p, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            c = int(r["civilWarMonthsCount"]); fo = int(r["foreignWarMonthsCount"]); m = c + fo
            act = float(r["warMultActualCivilSumIndex"]) + float(r["warMultActualForeignSumIndex"])
            old = float(r["warMultNewRuleCivilSumIndex"]) + float(r["warMultNewRuleForeignSumIndex"])
            sk = sold.get((r["quarter"], r["nation"]), 0.0)
            o["m"] += m; o["c"] += c; o["act"] += act; o["old"] += old
            o["exA"] += sk * (act - m); o["exO"] += sk * (old - m)
            if c: o["actC"] += float(r["warMultActualCivilSumIndex"]); o["oldC"] += float(r["warMultNewRuleCivilSumIndex"]); o["old2C"] += float(r["warMultOldRuleCivilSumIndex"])
print(f"# 同じ盤面の新÷旧（{D}・種 {SB}〜{SB+9}・読めた種 {o['seeds']}）")
print(f"交戦の月 {o['m']}・内戦の月 {o['c']}")
print(f"warMult の月の平均：新（実際＝接触）{o['act']/max(1,o['m']):.4f}・旧（同じ盤面の 9×(1+0.5×厭戦)）{o['old']/max(1,o['m']):.4f}・新÷旧 {o['act']/o['old']:.4f}")
print(f"Σ兵K×(warMult−1)：新 {o['exA']:.6g}・旧 {o['exO']:.6g}・新÷旧 {o['exA']/o['exO']:.4f}")
if o["c"]: print(f"内戦の月の warMult の平均：新 {o['actC']/o['c']:.4f}・旧（統一の式）{o['oldC']/o['c']:.4f}・旧の 2.0 の枝 {o['old2C']/o['c']:.4f}")
