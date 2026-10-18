#!/usr/bin/env python
"""(415) 発見の技術力の置き換え：A0（DiscoveryEngineerHalfPerMonth）の census の読み手。事前登録 measurements/dt_20261019_prereg.txt [6] の規則どおり。

入力：<計器の出力>/engineer_s<seed>.tsv（列＝kind quarter year nation row decade engineerPerMonth techTerm・旗0の走りで出す）。
規則：各行（d＝(年代−1780)/10 が正）×大国（イギリス・フランス・プロイセン〔ドイツ〕）×種について、行の年代 +5 年に最も近い年の A（T の軌道か解禁した四半期の E）を読み、
      a(行) ＝ A ÷ (k × d)、k ＝ 55 ÷ 15 ＝ 3.667。A0 ＝ a の（行×大国×種の）中央値。
使い方：PYTHONUTF8=1 python a0_census.py <計器の出力フォルダ> [種の番号...]
"""
import csv, glob, os, statistics, sys

K = 55.0 / 15.0
POWERS = [("イギリス", "イギリス"), ("フランス", "フランス"), ("プロイセン", "プロイセン"), ("ドイツ", "プロイセン")]
SKIP_ROWS = set()  # 制度と思想の行は engineer_s に出ない（求めない）


def power_of(nation: str):
    for key, name in POWERS:
        if key in nation:
            return name
    return None


def main():
    root = sys.argv[1]
    seeds = sys.argv[2:]
    files = sorted(glob.glob(os.path.join(root, "engineer_s*.tsv")))
    if seeds:
        files = [f for f in files if os.path.basename(f)[len("engineer_s"):-4] in seeds]
    obs = {}  # (seed, power, row) -> list of (year, A, decade)
    for f in files:
        seed = os.path.basename(f)[len("engineer_s"):-4]
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r.get("engineerPerMonth") is None or r.get("decade") is None:
                    continue  # 書きかけの末尾の行
                p = power_of(r["nation"])
                if p is None:
                    continue
                try:
                    a = float(r["engineerPerMonth"])
                except ValueError:
                    continue
                if a != a:
                    continue
                obs.setdefault((seed, p, r["row"]), []).append((int(r["year"]), a, int(r["decade"])))
    per = []
    for (seed, p, row), lst in sorted(obs.items()):
        dec = lst[0][2]
        d = (dec - 1780) / 10.0
        if not d > 0:
            continue
        target = dec + 5
        yr, a, _ = min(lst, key=lambda t: (abs(t[0] - target), t[0]))
        per.append((row, dec, p, seed, yr, a, a / (K * d) if a > 0 else 0.0))
    per.sort()
    print("row\tdecade\tpower\tseed\tyear\tA\ta=A/(k*d)")
    for t in per:
        print(f"{t[0]}\t{t[1]}\t{t[2]}\t{t[3]}\t{t[4]}\t{t[5]:.6g}\t{t[6]:.6g}")
    vals = [t[6] for t in per if t[6] > 0]
    zeros = sum(1 for t in per if t[6] <= 0)
    print(f"# 観測 {len(per)} 件（A＝0 が {zeros} 件）・a の中央値（A0）＝{statistics.median([t[6] for t in per]) if per else float('nan'):.6g}（A＝0 を含む）"
          f"／{statistics.median(vals) if vals else float('nan'):.6g}（A＞0 だけ）")
    # 行ごとの中央値（行の間の開き）
    byrow = {}
    for t in per:
        byrow.setdefault(t[0], []).append(t[6])
    print("# 行ごとの a の中央値：")
    for row, v in sorted(byrow.items(), key=lambda kv: statistics.median(kv[1])):
        print(f"#   {row}\t{statistics.median(v):.6g}\t(n={len(v)})")


if __name__ == "__main__":
    main()
