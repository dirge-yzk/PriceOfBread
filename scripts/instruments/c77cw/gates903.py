# (903) c77cw 門の集計（事前登録 measurements/m903_20261106_prereg_cw.txt §3 の写し・結果を見て替えない）
#   使い方: python gates903.py <腕の ins（cwmult_s・wars_s）> <旗 0 の ins（cwmult_s＝d_0）> [M=9.0] [D=0.5]
import sys, os, csv, statistics
ARM, BASE = sys.argv[1], sys.argv[2]
M = float(sys.argv[3]) if len(sys.argv) > 3 else 9.0
D = float(sys.argv[4]) if len(sys.argv) > 4 else 0.5
SEEDS = range(int(os.environ.get('SB', '1')), int(os.environ.get('SB', '1')) + 10)   # SB=11 で S11（種 11〜20）

def cw(d):
    t = dict(civil=0, foreign=0, actC=0.0, oldC=0.0, newC=0.0, exhC=0.0, actF=0.0, newF=0.0, oldMis=0, newMis=0, milC=0.0,
             ammoD=0.0, ammoP=0.0, powD=0.0, powP=0.0, leadD=0.0, leadP=0.0, minAct=float("inf"), maxAct=0.0, rows=0, seeds=0)
    for s in SEEDS:
        p = os.path.join(d, f"cwmult_s{s}.tsv")
        if not os.path.exists(p): continue
        t["seeds"] += 1
        with open(p, encoding="utf-8") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                t["rows"] += 1
                c = int(r["civilWarMonthsCount"]); fo = int(r["foreignWarMonthsCount"])
                t["civil"] += c; t["foreign"] += fo
                for k, col in (("actC", "warMultActualCivilSumIndex"), ("oldC", "warMultOldRuleCivilSumIndex"), ("newC", "warMultNewRuleCivilSumIndex"),
                               ("exhC", "warExhaustionCivilSumShare"), ("actF", "warMultActualForeignSumIndex"), ("newF", "warMultNewRuleForeignSumIndex"),
                               ("milC", "militaryScaleCivilSumIndex"), ("ammoD", "ammunitionDemandCivilQty"), ("ammoP", "ammunitionProducedCivilQty"),
                               ("powD", "gunpowderDemandCivilQty"), ("powP", "gunpowderProducedCivilQty"), ("leadD", "leadDemandCivilQty"), ("leadP", "leadProducedCivilQty")):
                    t[k] += float(r[col])
                t["oldMis"] += int(r["oldRuleMismatchMonthsCount"]); t["newMis"] += int(r["newRuleMismatchMonthsCount"])
                if c > 0:
                    a = float(r["warMultActualCivilSumIndex"]) / c   # 四半期の平均（月ごとの最小は四半期の平均の最小で下から抑える）
                    t["minAct"] = min(t["minAct"], a); t["maxAct"] = max(t["maxAct"], a)
    return t

def civil_wars(d):
    L = []
    for s in SEEDS:
        p = os.path.join(d, f"wars_s{s}.tsv")
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                if r["civilWarFlag"] == "1": L.append(int(r["months"]))
    return L

a = cw(ARM); b = cw(BASE)
def ok(x): return "緑" if x else "赤"
print(f"# (903) 門（腕 {ARM}・旗 0 {BASE}・M={M} D={D}）")
print(f"腕：種 {a['seeds']}・行 {a['rows']}・内戦の月 {a['civil']}・対外戦だけの月 {a['foreign']}／旗 0：種 {b['seeds']}・内戦の月 {b['civil']}")
if a["civil"] > 0:
    ac = a["actC"] / a["civil"]
    g1lo = ac >= M - 1e-9 and a["minAct"] >= M - 1e-9
    g1hi = ac <= M * (1 + D) + 1e-9
    mis_lim = 0.001 * (a["civil"] + a["foreign"])
    g1shape = a["newMis"] <= mis_lim
    g1mag = ac / 18.0 <= 0.75
    print(f"G1 下限：a_c={ac:.6f}・四半期の平均の最小 {a['minAct']:.6f} ≧ {M}\t{ok(g1lo)}")
    print(f"G1 上限：a_c={ac:.6f} ≦ {M*(1+D)}（四半期の平均の最大 {a['maxAct']:.6f}）\t{ok(g1hi)}")
    print(f"G1 形：newRuleMismatch {a['newMis']} ≦ {mis_lim:.1f}（oldRuleMismatch {a['oldMis']}）\t{ok(g1shape)}")
    print(f"G1 大きさ：a_c÷18={ac/18:.4f} ≦ 0.75\t{ok(g1mag)}")
    print(f"記録：内戦の月の厭戦の平均 {a['exhC']/a['civil']:.4f}・新の規則の平均 {a['newC']/a['civil']:.4f}・旧の規則の平均 {a['oldC']/a['civil']:.4f}")
else:
    print("G1：内戦の月が 0＝門を読めない\t赤")
if a["foreign"] > 0:
    af = a["actF"] / a["foreign"]
    print(f"G1f：対外戦の a_f={af:.6f} ∈ [{M}, {M*(1+D)}]\t{ok(M - 1e-9 <= af <= M*(1+D) + 1e-9)}")
if b["civil"] > 0:
    print(f"旗 0（同じ表）：a_c={b['actC']/b['civil']:.6f}・新の規則の平均 {b['newC']/b['civil']:.6f}（同じ盤面の新の分布）・oldRuleMismatch {b['oldMis']}・newRuleMismatch {b['newMis']}")
La = civil_wars(ARM)
NULL = {"base": 12.0, "n1": 10.0, "n2": 11.0}   # 事前登録 §2 (4) の 10 種の長さの中央
lo, hi = min(NULL.values()), max(NULL.values()); w = hi - lo
med = statistics.median(La) if La else float("nan")
print(f"G2：内戦の長さの中央（10 種）{med} ∈ [{lo-w}, {hi+w}]\t{ok(lo - w <= med <= hi + w)}")
print(f"記録：内戦の件数（10 種）{len(La)}（base 177・n1 145・n2 178＝一つ抜きで空振り 1/3＝記録）")
for g, kd, kp in (("弾薬", "ammoD", "ammoP"), ("火薬", "powD", "powP"), ("鉛", "leadD", "leadP")):
    print(f"G3 {g}：内戦の月の需要の和 {a[kd]:.6g}・産出の和 {a[kp]:.6g}＞0\t{ok(a[kd] > 0 and a[kp] > 0)}")
if a["civil"] > 0 and b["civil"] > 0 and b["ammoD"] > 0:
    dB = a["ammoD"] / a["civil"]; d0 = b["ammoD"] / b["civil"]
    print(f"G3 比：弾薬の内戦の月あたりの需要 d_B={dB:.6g}÷d_0={d0:.6g}＝{dB/d0:.4f} ≧ 0.2（幅なし）\t{ok(dB/d0 >= 0.2)}")
    print(f"記録：装備の規模の内戦の月あたり 腕 {a['milC']/a['civil']:.6g}・旗 0 {b['milC']/b['civil']:.6g}・比 {(a['milC']/a['civil'])/(b['milC']/b['civil']):.4f}")
    for g, kd, kp in (("火薬", "powD", "powP"), ("鉛", "leadD", "leadP"), ("弾薬", "ammoD", "ammoP")):
        print(f"記録：{g} 内戦の月あたり 需要 腕÷旗0 {(a[kd]/a['civil'])/(b[kd]/b['civil']) if b[kd] > 0 else float('nan'):.4f}・産出 腕÷旗0 {(a[kp]/a['civil'])/(b[kp]/b['civil']) if b[kp] > 0 else float('nan'):.4f}")
