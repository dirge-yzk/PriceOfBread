# (872) c72ds 紙の台本 2（読むだけ）：収穫係数を含めた余り（σ×f＞1）の母集合
# f_eff＝engineSubOutputPerMonth÷(必要量×subPopK×subRateWeighted)＝州の自給の担い手の人数と割合で重みづけた収穫係数の近似（主食の財ごとの f は 8 表に無い＝財をまたいだ平均）
# 出す物（窓ごと）：f_eff の p10/p50/p90・
#   (a) min(1,σraw)×f＞1 の行の割合（今のエンジンで必要量を超えた自給が消える行＝食べられも売られもしない量・LastSubsistenceOutput には積まれる）
#   (b) σraw×f＞1 の行の割合（段 3＋4 の E_p＞0 の母集合の近似）とその行の余り÷作った量の平均
#   (c) 旗 0 の盤面（base・n1・n2）の実の割合 d0×f＞1 の行の割合（今の基線で消えている自給）
# 使い方：python paper872b.py <出力の .txt>
import csv, math, sys, os

SETS = [
    ("base", "D:/PoB_work/keep_852/base_ins"),
    ("n1", "D:/PoB_work/keep_852/n1"),
    ("n2", "D:/PoB_work/keep_852/n2"),
    ("arm", "D:/PoB_work/keep_859_rej/c70ssB_10/ins"),
]
WINS = [(1783, 1785), (1786, 1799), (1800, 1849), (1850, 1899), (1900, 1950)]


def fnum(s):
    try:
        return float(s)
    except ValueError:
        return float("nan")


def win_of(y):
    for i, (a, b) in enumerate(WINS):
        if a <= y <= b:
            return i
    return -1


def pct(a, q):
    if not a:
        return float("nan")
    s = sorted(a); k = (len(s) - 1) * q; lo = int(k); hi = min(lo + 1, len(s) - 1)
    return s[lo] + (s[hi] - s[lo]) * (k - lo)


def fmt(v, n=4):
    return ("%." + str(n) + "f") % v if v == v else "nan"


def main():
    out = sys.argv[1]; L = []
    L.append("(872) c72ds 紙の数 2（読むだけ）・盤面 m852・種 1〜10・行＝州×四半期・担い手の行（s2RhoSource 1 か 2）")
    L.append("⚠限界：f_eff は財をまたいだ近似（財ごとの余りは 1 つの財の f が 1/σ を超えると出る＝この近似は下に偏る）・四半期の末の月の値")
    for name, d in SETS:
        F = [[] for _ in WINS]; A = [0] * len(WINS); B = [0] * len(WINS); C = [0] * len(WINS); N = [0] * len(WINS); BS = [0.0] * len(WINS)
        for sd in range(1, 11):
            p = os.path.join(d, "subst_s%d.tsv" % sd)
            if not os.path.exists(p):
                continue
            with open(p, "r", encoding="utf-8", newline="") as f:
                rd = csv.reader(f, delimiter="\t"); h = next(rd); ix = {c: i for i, c in enumerate(h)}
                for row in rd:
                    src = fnum(row[ix["s2RhoSource"]])
                    if not (src == 1 or src == 2):
                        continue
                    rho = fnum(row[ix["s2RhoInst"]]); d0 = fnum(row[ix["s2D0Inst"]]); u = fnum(row[ix["s2UHead"]]); t = fnum(row[ix["s2TechHead"]])
                    hd = fnum(row[ix["s2HeadsEnd"]]); need = fnum(row[ix["stapleNeedPerCapita"]])
                    eng = fnum(row[ix["engineSubOutputPerMonth"]]); sp = fnum(row[ix["subPopK"]]); sr = fnum(row[ix["subRateWeighted"]])
                    if not (rho > 0 and d0 > 0 and hd > 0 and t > 0 and need > 0 and u == u and sp > 0 and sr > 0 and eng == eng):
                        continue
                    wi = win_of(int(fnum(row[ix["year"]])))
                    if wi < 0:
                        continue
                    fe = eng / (need * sp * sr)
                    sraw = rho * u * t / (need * hd)
                    N[wi] += 1; F[wi].append(fe)
                    if min(1.0, sraw) * fe > 1: A[wi] += 1
                    if sraw * fe > 1: B[wi] += 1; BS[wi] += 1 - 1 / (sraw * fe)
                    if d0 * fe > 1: C[wi] += 1
        L.append(""); L.append("== %s" % name)
        L.append("窓｜行｜f_eff p10/p50/p90｜(a) min(1,σraw)×f＞1｜(b) σraw×f＞1・余り÷作った量の平均｜(c) d0×f＞1")
        for wi, (a, b) in enumerate(WINS):
            n = N[wi]
            if n == 0:
                continue
            L.append("  %d-%d｜%d｜%s/%s/%s｜%s｜%s・%s｜%s" % (a, b, n, fmt(pct(F[wi], .1)), fmt(pct(F[wi], .5)), fmt(pct(F[wi], .9)),
                     fmt(A[wi] / n, 5), fmt(B[wi] / n, 5), fmt(BS[wi] / B[wi] if B[wi] else float("nan")), fmt(C[wi] / n, 5)))
    with open(out, "wb") as f:
        f.write(("\n".join(L) + "\n").encode("utf-8"))


main()
