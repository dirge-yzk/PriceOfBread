# (858) c70ds 読むだけの台本：地位と権力の軸で〈保有から導ける量〉の候補が今の盤面でどれだけの大きさを持つかを数える。
# ①官僚制度：仲介者の権利の残高 IntermediaryDegree＝max(官僚制度の身分度, 土地改革の度)（Privileges.cs:28〜29）。
#   官僚制度を 1 段進めて在地の目標が動くのは、官僚制度の身分度が土地改革の度より大きい州×四半期だけ（rule_s の 2 つの法の列で数える）。
#   仲介者の取り分 ruleCutPerMonth（Fiscal.cs:106〜109）の大きさを国の歳入と比べる。
# ②国教：聖職者（classId 6）の所得がブロック 0（貴族 0＋聖職者 6）の所得に占める割合（class_s）＝十分の一税の利害を旧支配層のブロックで平均すると薄まる大きさ。
# 使い方：python -I -X utf8 status858.py <基線の dir>
import sys, os, math

SEEDS = range(1, 11)
EST = {0: 1.0, 1: 0.5, 2: 0.0}   # EstateDegree：猟官 1・資格任用 0.5・専門 0（Privileges.cs:21）
LRD = {0: 1.0, 1: 0.5, 2: 0.0}   # 土地改革：なし 1・限定 0.5・全面 0（Privileges.cs:29）
PERIODS = [(1783, 1814), (1815, 1846), (1847, 1879), (1880, 1913), (1914, 1950)]

def f(x):
    try:
        return float(x)
    except ValueError:
        return float("nan")

def rd(p):
    with open(p, "rb") as fh:
        lines = fh.read().decode("utf-8").splitlines()
    h = lines[0].split("\t"); ix = {n: i for i, n in enumerate(h)}
    return ix, [ln.split("\t") for ln in lines[1:]]

def med(v):
    v = sorted(x for x in v if not math.isnan(x))
    return v[len(v) // 2] if v else float("nan")

def bureau(d):
    print("## 官僚制度：官僚制度を 1 段進めて在地の目標（Rule[1] の目標＝RuleInit[1]×IntermediaryDegree）が動く州×四半期の割合")
    print("| 時期 | 州×四半期 | 土地改革＝なし | 官僚制度が専門の前で、進めると max が下がる | 在地の開幕の割合 RuleInit[1]＞0 の行 | そのうち進めると動く | 仲介者の取り分÷国の歳入 中央（取り分＞0 の国×四半期） |")
    print("|---|---|---|---|---|---|---|")
    agg = {p: [0, 0, 0, 0, 0] for p in PERIODS}
    natcut = {p: {} for p in PERIODS}
    for s in SEEDS:
        p = os.path.join(d, f"rule_s{s}.tsv")
        if not os.path.exists(p):
            continue
        ix, rows = rd(p)
        for c in rows:
            y = int(f(c[ix["year"]]))
            per = next((pp for pp in PERIODS if pp[0] <= y <= pp[1]), None)
            if per is None:
                continue
            b = int(f(c[ix["bureaucracyLawCode"]])); l = int(f(c[ix["landReformLawCode"]]))
            init = f(c[ix["ruleInitLocalShare"]])
            a = agg[per]; a[0] += 1
            if l == 0: a[1] += 1
            moves = b < 2 and EST[b] > LRD[l] and EST[min(2, b + 1)] < EST[b]
            if moves: a[2] += 1
            if init > 0:
                a[3] += 1
                if moves: a[4] += 1
            k = (s, c[ix["quarter"]], c[ix["nation"]])
            cut = f(c[ix["ruleCutPerMonth"]]); rev = f(c[ix["revenuePerMonth"]])
            natcut[per].setdefault(k, [0.0, 0.0])
            # 国の値（InstrumentsRule.cs:82〜84 の nat＝州の行に同じ値が繰り返す）
            if not math.isnan(cut): natcut[per][k][0] = cut
            if not math.isnan(rev): natcut[per][k][1] = rev
    for per in PERIODS:
        a = agg[per]
        if a[0] == 0:
            continue
        ratios = [v[0] / v[1] for v in natcut[per].values() if v[1] > 0 and v[0] > 0]
        print(f"| {per[0]}〜{per[1]} | {a[0]} | {a[1]/a[0]:.3f} | {a[2]/a[0]:.3f} | {a[3]} | {(a[4]/a[3] if a[3] else float('nan')):.3f} | {med(ratios):.4f}（{len(ratios)}） |")
    print()

def clergy(d):
    print("## 国教：聖職者の所得÷ブロック 0（貴族＋聖職者）の所得（class_s・earnedPerMonth・国×時刻・両方が正の行）")
    print("| 時期 | 行 | 中央 | p10 | p90 |")
    print("|---|---|---|---|---|")
    vals = {p: [] for p in PERIODS}
    for s in SEEDS:
        p = os.path.join(d, f"class_s{s}.tsv")
        if not os.path.exists(p):
            continue
        ix, rows = rd(p)
        acc = {}
        for c in rows:
            cid = c[ix["classId"]]
            if cid not in ("0", "6"):
                continue
            k = (c[ix["time"]], c[ix["nation"]], c[ix["year"]])
            e = f(c[ix["earnedPerMonth"]])
            acc.setdefault(k, [0.0, 0.0])
            if not math.isnan(e):
                acc[k][0 if cid == "0" else 1] += e
        for (t, n, y), (nob, cl) in acc.items():
            yy = int(f(y))
            per = next((pp for pp in PERIODS if pp[0] <= yy <= pp[1]), None)
            if per and nob > 0 and cl > 0:
                vals[per].append(cl / (nob + cl))
    for per in PERIODS:
        v = sorted(vals[per])
        if not v:
            continue
        q = lambda pp: v[int((len(v) - 1) * pp)]
        print(f"| {per[0]}〜{per[1]} | {len(v)} | {q(.5):.3f} | {q(.1):.3f} | {q(.9):.3f} |")
    print()

if __name__ == "__main__":
    d = sys.argv[1]
    bureau(d)
    clergy(d)
