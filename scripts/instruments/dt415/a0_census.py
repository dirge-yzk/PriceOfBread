#!/usr/bin/env python
"""(415) 発見の技術力の置き換え：A0（DiscoveryEngineerHalfPerMonth）の census の読み手。事前登録 measurements/dt_20261019_prereg.txt [6]・追記 3・追記 4 の規則どおり。

入力：<計器の出力>/engineer_s<seed>.tsv（列＝kind quarter year nation row decade engineerPerMonth techTerm knownFlag・旗0の走りで出す）。
規則：各行（d＝(年代−1780)/10 が正）×大国（イギリス・フランス・プロイセン〔ドイツ〕）×種について、行の年代 +5 年の A を読み（T の軌道・8四半期ごと）、
      a ＝ A ÷ (k × d)、k ＝ 55 ÷ 15 ＝ 3.667。A0 ＝ a の（行×大国×種の）中央値（A＝0 を含む）。
      ★追記 4：読んだ年が行の年代 +5 年から許容幅（既定 ±2 年）を超える観測は捨て、捨てた件と行を名乗る（Score が前提の行〔After〕の未解禁の行を飛ばすので、その年に T の行が出ない行がある）。
      記録：解禁前だけ・解禁済みだけ・自分の財が新しい行〔機関車・新兵器 6 行〕を除いた中央値（A0 の偏りの向き）。
使い方：PYTHONUTF8=1 python a0_census.py <計器の出力フォルダ> [--tol=2] [種の番号...]
"""
import csv, glob, os, statistics, sys

K = 55.0 / 15.0
POWERS = [("イギリス", "イギリス"), ("フランス", "フランス"), ("プロイセン", "プロイセン"), ("ドイツ", "プロイセン")]
OWN_GOOD_ROWS = {"機関車", "装甲艦", "機関銃", "弩級戦艦", "戦車", "軍用機", "化学兵器"}  # 行の財＝自分の新しい財（旗が正の世界では発見の前の A が構造的に 0）


def power_of(nation: str):
    for key, name in POWERS:
        if key in nation:
            return name
    return None


def med(v):
    return statistics.median(v) if v else float("nan")


def main():
    args = sys.argv[1:]
    tol = 2
    seeds = []
    root = None
    for a in args:
        if a.startswith("--tol="):
            tol = int(a[6:])
        elif root is None:
            root = a
        else:
            seeds.append(a)
    files = sorted(glob.glob(os.path.join(root, "engineer_s*.tsv")))
    if seeds:
        files = [f for f in files if os.path.basename(f)[len("engineer_s"):-4] in seeds]
    obs = {}  # (seed, power, row) -> list of (year, A, decade, known)
    for f in files:
        seed = os.path.basename(f)[len("engineer_s"):-4]
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r.get("engineerPerMonth") is None or r.get("decade") is None or r["kind"] != "T":
                    continue  # 書きかけの末尾の行・E の行（解禁した四半期だけ）は規則の読みに使わない
                p = power_of(r["nation"])
                if p is None:
                    continue
                try:
                    a = float(r["engineerPerMonth"])
                except ValueError:
                    continue
                if a != a:
                    continue
                obs.setdefault((seed, p, r["row"]), []).append((int(r["year"]), a, int(r["decade"]), r.get("knownFlag", "0") == "1"))
    per, dropped = [], []
    for (seed, p, row), lst in sorted(obs.items()):
        dec = lst[0][2]
        d = (dec - 1780) / 10.0
        if not d > 0:
            continue
        target = dec + 5
        yr, a, _, known = min(lst, key=lambda t: (abs(t[0] - target), t[0]))
        if abs(yr - target) > tol:
            dropped.append((row, dec, p, seed, yr, a))
            continue
        per.append((row, dec, p, seed, yr, a, a / (K * d) if a > 0 else 0.0, known))
    per.sort()
    print("row\tdecade\tpower\tseed\tyear\tA\ta=A/(k*d)\tknownFlag")
    for t in per:
        print(f"{t[0]}\t{t[1]}\t{t[2]}\t{t[3]}\t{t[4]}\t{t[5]:.6g}\t{t[6]:.6g}\t{int(t[7])}")
    allv = [t[6] for t in per]
    pos = [t[6] for t in per if t[6] > 0]
    zeros = sum(1 for t in per if t[6] <= 0)
    print(f"# 許容幅 ±{tol} 年・観測 {len(per)} 件（A＝0 が {zeros} 件）・捨てた {len(dropped)} 件")
    print(f"# A0（a の中央値・A＝0 を含む）＝{med(allv):.6g}／{med(pos):.6g}（A＞0 だけ）")
    pre = [t[6] for t in per if not t[7]]
    post = [t[6] for t in per if t[7]]
    own = [t[6] for t in per if t[0] not in OWN_GOOD_ROWS]
    print(f"# 記録（偏りの向き）：解禁前の観測だけ {med(pre):.6g}（n={len(pre)}）／解禁済みの観測だけ {med(post):.6g}（n={len(post)}）／自分の財が新しい行〔{len(OWN_GOOD_ROWS)} 行〕を除く {med(own):.6g}（n={len(own)}）")
    print("# 捨てた観測（行・年代・国・種・読んだ年・A）：")
    for t in dropped:
        print(f"#   {t[0]}\t{t[1]}\t{t[2]}\t{t[3]}\t{t[4]}\t{t[5]:.6g}")
    byrow = {}
    for t in per:
        byrow.setdefault(t[0], []).append(t[6])
    print("# 行ごとの a の中央値：")
    for row, v in sorted(byrow.items(), key=lambda kv: statistics.median(kv[1])):
        print(f"#   {row}\t{statistics.median(v):.6g}\t(n={len(v)})")


if __name__ == "__main__":
    main()
