#!/usr/bin/env python
"""(422) 腕 B の判定の読み手（事前登録 measurements/dd_20261019_prereg.txt [7]・追記 3 の定義どおり）。腕を回す前に書いてコミットする（判定の定義を腕の後に書かない）。シミュは回さない。

入力：<計器の出力フォルダ>/disc_s<seed>.tsv（E 行＝解禁）・engineer_s<seed>.tsv（T 行＝8 四半期ごとの技術力、E 行＝解禁した国の A_n と技術力）・nation_s<seed>.tsv（開幕の人口）。
定義（腕の前に固定）：
  ・対象の行＝非制度・非思想の行（合成染料は旗の正負で E の有無が違うので除く）。制度と思想の行は disc_s の E に出るが数えない（SPREAD）。
  ・最初の発見者＝(種, 行) ごとに disc_s の E 行で (year, quarter) が最小の国。同じ (year, quarter) に複数の国が開いたら全員を最初の発見者とし、1/k ずつの重みで数える（(種, 行) の重みの和は 1）。
  ・小国＝その種の開幕（nation_s の quarter 0）の人口が、その種の国々の中央値より下の国。開幕に居ない国（独立・建国）は小国の比の分子にも分母にも入れず別に数える。T4 の比の分母には入れる。
  ・T1＝(種) ごとに、最初の解禁の year が 1783 の行の数（decade 1780＝d≦0 の行も入れる＝(415) の 11.5・m412 の 1 と同じ数え方）。種の平均と最大。d≦0 の行の数も別に出す。
  ・T2＝転炉鋼・機関車・汽船・加硫ゴムの最初の解禁の year の種の中央値。10 種のどれでも開かない種は ∞ として中央値に入れる（中央値が ∞ なら〈開かない〉）。
  ・T3 前半＝最初の発見者の A_n（engineer_s の E 行の engineerPerMonth）が Am 未満の件の重みの比。A_n が読めない件（engineer_s の E 行が無い）は別に数える。
  ・T4＝最初の発見者の重みの国別の比（清・最大の 1 国）。
  ・T5＝10 種（または与えた種の数）のどれでも開かない行の数と名前。行の全体＝engineer_s の T 行に現れる非制度・非思想の行の和集合。
  ・T0＝engineer_s の T 行（year≦1785・knownFlag＝0・d>0 の行だけ）の techTerm≧55（0.786×70）の国×行の数と国別。
使い方：PYTHONUTF8=1 python judge.py <計器の出力フォルダ> [種の数=10] [--am=17.51]
"""
import collections, csv, glob, math, os, statistics, sys

SPREAD = {"特許", "株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"}
EXCLUDED = {"合成染料"}
T2ROWS = ["転炉鋼", "機関車", "汽船", "加硫ゴム"]


def rd(path):
    with open(path, encoding="utf-8") as fh:
        return list(csv.DictReader(fh, delimiter="\t"))


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    am = 17.51
    for a in sys.argv[1:]:
        if a.startswith("--am="):
            am = float(a[5:])
    root = args[0]
    nseeds = int(args[1]) if len(args) > 1 else 10
    first = {}      # (seed,row) -> list of (year,quarter,nation)
    pop0 = {}
    aN = {}         # (seed,row,nation) -> engineerPerMonth at unlock
    allrows = set()
    d0rows = set()   # decade<=1780（d≦0）の行
    t0hits = collections.Counter(); t0n = 0
    for s in range(1, nseeds + 1):
        p0 = {}
        for r in rd(os.path.join(root, f"nation_s{s}.tsv")):
            if r["quarter"] == "0":
                p0[r["nation"]] = float(r["totalPopulationK"])
        pop0[s] = p0
        best = {}
        for r in rd(os.path.join(root, f"disc_s{s}.tsv")):
            if r["kind"] != "E" or r["row"] in SPREAD or r["row"] in EXCLUDED:
                continue
            t = (int(r["year"]), int(r["quarter"]))
            best.setdefault(r["row"], []).append((t, r["nation"]))
        for row, lst in best.items():
            tmin = min(t for t, _ in lst)
            first[(s, row)] = [(tmin[0], tmin[1], n) for t, n in lst if t == tmin]
        for r in rd(os.path.join(root, f"engineer_s{s}.tsv")):
            if r["row"] in SPREAD or r["row"] in EXCLUDED:
                continue
            allrows.add(r["row"])
            if int(r["decade"]) <= 1780:
                d0rows.add(r["row"])
            if r["kind"] == "E":
                try:
                    aN[(s, r["row"], r["nation"])] = float(r["engineerPerMonth"])
                except ValueError:
                    pass
            elif r["kind"] == "T" and int(r["year"]) <= 1785 and r["knownFlag"] == "0" and int(r["decade"]) > 1780:   # d>0 の行だけ（d≦0 の行は技術力＝70 の定義どおりで時計の試験にならない）
                try:
                    tt = float(r["techTerm"])
                except ValueError:
                    continue
                t0n += 1
                if tt >= 55.0:
                    t0hits[r["nation"]] += 1
    rows_seen = {row for (_, row) in first}
    # T1
    per1783, per1783_d0 = [], []
    for s in range(1, nseeds + 1):
        c = c0 = 0
        for (ss, row), lst in first.items():
            if ss == s and lst[0][0] == 1783:
                c += 1
        per1783.append(c)
    d0open = [sum(1 for (ss, row), lst in first.items() if ss == s and lst[0][0] == 1783 and row in d0rows) for s in range(1, nseeds + 1)]
    print(f"T1 1783 年中に開く行：種ごと {per1783}・平均 {statistics.mean(per1783):.2f}・最大 {max(per1783)}（狙い＝平均≦3・最大≦6）・うち d≦0 の行（{len(d0rows)} 本：{'・'.join(sorted(d0rows))}）種ごと {d0open}")
    # T2
    print("T2 最初の解禁の year の種の中央値（開かない種は ∞）：")
    for row in T2ROWS:
        ys = [first[(s, row)][0][0] if (s, row) in first else math.inf for s in range(1, nseeds + 1)]
        m = statistics.median(ys)
        print(f"   {row}\t{'開かない' if math.isinf(m) else f'{m:.1f}'}\t（開いた種 {sum(1 for y in ys if not math.isinf(y))}/{nseeds}・年 {sorted(y for y in ys if not math.isinf(y))}）")
    # T3, T4
    wnat = collections.Counter(); wtot = 0.0
    small_w = small_tot = 0.0; unseeded = 0.0
    below_am = known_a = unread = 0.0
    for (s, row), lst in sorted(first.items()):
        k = len(lst)
        med = statistics.median(pop0[s].values()) if pop0[s] else 0.0
        for (_, _, n) in lst:
            w = 1.0 / k
            wtot += w; wnat[n] += w
            if n in pop0[s]:
                small_tot += w
                if pop0[s][n] < med:
                    small_w += w
            else:
                unseeded += w
            a = aN.get((s, row, n))
            if a is None or a != a:
                unread += w
            else:
                known_a += w
                if a < am:
                    below_am += w
    print(f"T3 前半 最初の発見者の A_n が Am({am}) 未満の比：{below_am / known_a:.3f}（{below_am:.2f}/{known_a:.2f}・A_n が読めない件 {unread:.2f}）（狙い＜0.10）" if known_a else "T3 前半 A_n が読めない")
    print(f"T3 後半 小国が最初の発見者になる比：{small_w / small_tot:.3f}（{small_w:.2f}/{small_tot:.2f}・開幕に居ない国 {unseeded:.2f} 件は別）（狙い 0.05〜0.40）" if small_tot else "T3 後半 該当なし")
    top = wnat.most_common(8)
    print(f"T4 清の比：{wnat.get('清', 0.0) / wtot:.3f}（狙い≦0.20）・最大の 1 国：{top[0][0]} {top[0][1] / wtot:.3f}（狙い≦0.35）・上位 " + "・".join(f"{n}{c / wtot:.3f}" for n, c in top) + f"（重みの合計 {wtot:.2f}）")
    # T5
    never = sorted(r for r in allrows if r not in rows_seen)
    print(f"T5 どの種でも開かない行：{len(never)} 本（狙い≦8）：{'・'.join(never)}（行の全体 {len(allrows)} 本）")
    # R: 各行の最初の解禁の年の種の中央値
    print("R 行ごと\t開いた種\t最初の解禁の年の中央値（開いた種だけ）")
    for row in sorted(allrows):
        ys = [first[(s, row)][0][0] for s in range(1, nseeds + 1) if (s, row) in first]
        print(f"   {row}\t{len(ys)}\t{statistics.median(ys):.0f}" if ys else f"   {row}\t0\t-")
    print(f"T0 1785 年以前の T 行（knownFlag＝0・{t0n} 組）で技術力≧55 の国×行：{sum(t0hits.values())} 件・国別 {t0hits.most_common(6)}（狙い≦2・1 国で 2 件以上を占めない）")


if __name__ == "__main__":
    main()
