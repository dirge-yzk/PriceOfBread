#!/usr/bin/env python
"""(422) 腕の前の見込み：census（旗 0）の pool_s の世界の合計 W から、W0 を入れたときの時計 S の軌道を行ごとに出す（オフライン・シミュは回さない）。
最初の発見者は伝わり＝0（誰も知らない）で必要性は 70 未満なので、点数 100 に届くには 技術力 ≧ 100−必要性＝30 以上が要る。C≦1 なので S の上限（最大値）が 30/70＝0.43 未満の行は、その世界では何年たっても最初に開かない。
入力：<計器の出力フォルダ>（pool_s*.tsv）、W0。出力：行ごとの 〈d・S の最大・最大の年・S≧0.43 に初めて届く年・S≧0.5 に初めて届く年・W の最大の年〉の種ごとの中央値。
使い方：PYTHONUTF8=1 python feasibility.py <計器の出力フォルダ> <W0>
"""
import csv, glob, os, statistics, sys, collections


def main():
    root, w0 = sys.argv[1], float(sys.argv[2])
    ser = collections.defaultdict(lambda: collections.defaultdict(list))  # row -> seed -> [(year,W,dec)]
    for f in sorted(glob.glob(os.path.join(root, "pool_s*.tsv"))):
        seed = os.path.basename(f)[len("pool_s"):-4]
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r.get("chinaPerMonth") is None or r["kind"] != "T":
                    continue
                try:
                    w = float(r["poolPerMonth"])
                except ValueError:
                    continue
                ser[r["row"]][seed].append((int(r["year"]), w, int(r["decade"])))
    print("row\tdecade\tmaxS\tyearOfMaxS\tfirstS>=0.43\tfirstS>=0.5\tyearOfPeakW\tpeakW\tlastW")
    out = []
    for row, bys in ser.items():
        mx, ym, y43, y50, ypk, pk, lw, dec = [], [], [], [], [], [], [], 0
        for seed, lst in bys.items():
            lst.sort()
            dec = lst[0][2]
            d = (dec - 1780) / 10.0
            if not d > 0:
                continue
            ss = [(y, (w / (w + w0 * d)) if w > 0 else 0.0, w) for y, w, _ in lst]
            m = max(ss, key=lambda t: t[1])
            mx.append(m[1]); ym.append(m[0])
            f43 = [y for y, s, _ in ss if s >= 0.43]; f50 = [y for y, s, _ in ss if s >= 0.5]
            if f43: y43.append(f43[0])
            if f50: y50.append(f50[0])
            p = max(ss, key=lambda t: t[2]); ypk.append(p[0]); pk.append(p[2]); lw.append(ss[-1][2])
        if not mx:
            continue
        med = lambda v: statistics.median(v) if v else float("nan")
        out.append((med(mx), row, dec, med(ym), med(y43) if len(y43) == len(mx) else float("nan"), med(y50) if len(y50) == len(mx) else float("nan"), med(ypk), med(pk), med(lw)))
    for t in sorted(out):
        print(f"{t[1]}\t{t[2]}\t{t[0]:.3f}\t{t[3]:.0f}\t{t[4]:.0f}\t{t[5]:.0f}\t{t[6]:.0f}\t{t[7]:.4g}\t{t[8]:.4g}")
    n43 = sum(1 for t in out if t[0] < 30 / 70)
    print(f"# 行 {len(out)} 本のうち max S＜0.43（必要性が 70 未満の最初の発見は何年たっても届かない）：{n43} 本。0.43〜0.60：{sum(1 for t in out if 30/70 <= t[0] < 0.60)} 本")


if __name__ == "__main__":
    main()
