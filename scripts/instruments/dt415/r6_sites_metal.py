#!/usr/bin/env python
"""(415) 記録の線 R6：産地の発見（site_s の O 行＝開いた産地）と世界の金銀（metal_s）を、基準（m412_ins）と腕で並べる。採否に使わない（発散・保存則の止める線は README のまま）。

産地は技術水準の時計を失う（旗が正では N÷(N＋N0) だけ）ので、後期の鉱床（金・銀など）が早く開いて通貨の側を動かさないかを見る。
使い方：PYTHONUTF8=1 python r6_sites_metal.py <基準の計器フォルダ> <腕の計器フォルダ> [種の数=10]
出力：産地ごとの〈開いた種の数・最初に開いた年の中央値〉の基準と腕の並び、世界の金銀の（年ごと）種の中央値（在庫・銀金比の対数）。
"""
import csv, glob, os, statistics, sys


def sites(root, n):
    out = {}
    for s in range(1, n + 1):
        f = os.path.join(root, f"site_s{s}.tsv")
        if not os.path.exists(f):
            continue
        first = {}
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["kind"] != "O":
                    continue
                y = int(r["year"])
                k = r["site"]
                if k not in first or y < first[k][0]:
                    first[k] = (y, r["nation"])
        for k, v in first.items():
            out.setdefault(k, []).append(v)
    return out


def metal(root, n):
    out = {}
    for s in range(1, n + 1):
        f = os.path.join(root, f"metal_s{s}.tsv")
        if not os.path.exists(f):
            continue
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                y = int(r["year"])
                if int(r["quarter"]) % 4 != 0:
                    continue
                out.setdefault(y, []).append((float(r["goldStock"]), float(r["silverStock"]), float(r["lnAgAuMean"]) if r["lnAgAuMean"] != "nan" else float("nan")))
    return out


def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float("nan")


def main():
    base, arm = sys.argv[1], sys.argv[2]
    n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
    sb, sa = sites(base, n), sites(arm, n)
    print("産地\t基準:開いた種\t基準:最初の年(中央値)\t腕:開いた種\t腕:最初の年(中央値)\t年の差(腕-基準)")
    for k in sorted(set(sb) | set(sa), key=lambda k: med([v[0] for v in sb.get(k, sa.get(k, []))])):
        b = [v[0] for v in sb.get(k, [])]
        a = [v[0] for v in sa.get(k, [])]
        print(f"{k}\t{len(b)}\t{med(b):.1f}\t{len(a)}\t{med(a):.1f}\t{med(a) - med(b):+.1f}")
    mb, ma = metal(base, n), metal(arm, n)
    print("\n年\t基準:金在庫\t基準:銀在庫\t基準:ln(銀/金)\t腕:金在庫\t腕:銀在庫\t腕:ln(銀/金)")
    for y in sorted(set(mb) & set(ma)):
        if y % 25 != 0 and y not in (1783, 1950):
            continue
        b, a = mb[y], ma[y]
        print(f"{y}\t{med([x[0] for x in b]):.0f}\t{med([x[1] for x in b]):.0f}\t{med([x[2] for x in b]):.3f}\t{med([x[0] for x in a]):.0f}\t{med([x[1] for x in a]):.0f}\t{med([x[2] for x in a]):.3f}")


if __name__ == "__main__":
    main()
