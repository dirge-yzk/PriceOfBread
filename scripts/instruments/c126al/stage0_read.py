# (1242) 段 0 の読み手：プローブの出力 f{0,1}_s{1..10}.tsv の本番 12 か月の値（種の中央〔最小〜最大〕）。
import sys, os, statistics as st
D = sys.argv[1]
def rd(f, s):
    P, C, E, S = {}, None, {}, None
    for ln in open(os.path.join(D, f"f{f}_s{s}.tsv"), encoding="utf-8"):
        c = ln.rstrip("\n").split("\t")
        if len(c) < 3 or c[1] != "m" or c[2] != "12": continue
        if c[0] == "J" and c[3] == "ジャマイカ" and c[4] in ("パン", "小麦粉"): P[c[4]] = (float(c[9]), float(c[7]), float(c[10]))
        elif c[0] == "C": C = dict(x.split("=") for x in c[3:])
        elif c[0] == "E": E[c[3]] = (float(c[4]), float(c[5]), int(c[6]), int(c[7]))
        elif c[0] == "S": S = dict(x.split("=") for x in c[3:])
    return P, C, E, S
def fm(v): v = [x for x in v if x == x]; return f"{st.median(v):.4g}〔{min(v):.4g}〜{max(v):.4g}〕" if v else "nan"
R = {f: [rd(f, s) for s in range(1, 11)] for f in (0, 1)}
for f in (0, 1):
    P = [r[0] for r in R[f]]; C = [r[1] for r in R[f]]; E = [r[2] for r in R[f]]; S = [r[3] for r in R[f]]
    print(f"== 旗 {f}")
    print(" ジャマイカのパンの産出", fm([p["パン"][0] for p in P]), "正の種", sum(1 for p in P if p["パン"][0] > 0))
    print(" ジャマイカの小麦粉の産出", fm([p["小麦粉"][0] for p in P]), "正の種", sum(1 for p in P if p["小麦粉"][0] > 0))
    print(" ジャマイカのパンの比", fm([p["パン"][1] for p in P]), "輸入", fm([p["パン"][2] for p in P]))
    print(" 植民地格の州（旧の 10 財）の加工した食品の産出 0", fm([float(c["colonialFoodZero"]) for c in C]), "・9 財", fm([float(c["colonialFood9Zero"]) for c in C]), "・州の数", C[0]["colonial"])
    print(" G-S1 植民地格の州のパンの比の中央", fm([float(c["breadRatioColMed"]) for c in C]), "・本国格", fm([float(c["breadRatioHomeMed"]) for c in C]))
    print(" 植民地格の 10 財の産出の和", fm([float(c["colonialFood"]) for c in C]))
    z = sum(1 for e in E for n, v in e.items() if not v[0] > 0); tot = sum(len(e) for e in E)
    mism = sum(1 for e in E for n, v in e.items() if abs(v[0] - v[1]) > 1e-9 * max(1, abs(v[1])))
    print(f" 甜菜糖の親の技師：0 の国×種 {z}/{tot}・独立に足した値と食い違う国×種 {mism}・国の中央の種の中央 {fm([st.median([v[0] for v in e.values()]) for e in E])}")
    print(" 同じ盤面の旧と新（技師の役務の無い州の 9 財の既定のレシピ）旧の錠", fm([float(s["oldLock"]) for s in S]), "今の錠", fm([float(s["nowLock"]) for s in S]), "技師の分の単位の費用", fm([float(s["advCostMed"]) for s in S]))
