#!/usr/bin/env python
"""(448) 資本家の消失・段0：階級の人数の推移を種×国で数える（読むだけ・シミュを回さない）。

入力：計器つき 10 種の生（INS=1 の出力）の class_s<seed>.tsv（time が q<N> の行＝四半期の末・classId 3＝資本家・11＝職人）。
定義（事前登録 §3 と同じ）：
  ピーク＝(種, 国, 階級) の sizeK の最大・終わり＝その国が最後に現れた四半期の値・最小＝ピークより後の最小値。
  〈落ちた〉＝ピークより後の最小値がピークの 5% 未満（終わりの値でなく経路の最小）。〈終わりに落ちている〉＝終わりの値がピークの 5% 未満。
  大国＝(種, 国) の全階級の sizeK の和の最大が BIG_K 千人以上（既定 5000）。ピークが小さい階級（ピーク < MIN_PEAK_K 千人）は数えない（既定 5）。
  所得が負の四半期の割合＝earnedPerMonth < 0 の四半期数 ÷ その国の四半期数（階級ごと）。
使い方：PYTHONUTF8=1 python cap_count.py <計器の生のフォルダ> [--seeds 10] [--big 5000] [--minpeak 5] [--class 3] [-v]
"""
import argparse, csv, os, collections, statistics

def rd(path):
    with open(path, encoding="utf-8", newline="") as fh:
        return csv.DictReader(fh, delimiter="\t")  # 呼び手が即座に読み切る

def load(root, s):
    rows = []
    with open(os.path.join(root, f"class_s{s}.tsv"), encoding="utf-8", newline="") as fh:
        for r in csv.DictReader(fh, delimiter="\t"):
            t = r["time"]
            if not t.startswith("q"):
                continue
            rows.append((int(t[1:]), base(r["nation"]), int(r["classId"]), float(r["sizeK"]), r["earnedPerMonth"], r["paidPerMonth"], float(r["savingsStock"]), float(r["negativeSavingsStock"])))
    return rows

PREFIXES = ("共和制", "立憲王政")


def base(n):
    """政体が変わると国の名前の頭に 共和制・立憲王政 が付く＝同じ国の系列として束ねる（nationUid は class の列に無い）。"""
    for pf in PREFIXES:
        if n.startswith(pf):
            return n[len(pf):]
    return n


def num(x):
    try:
        v = float(x)
        return v if v == v else None
    except ValueError:
        return None

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10)
    ap.add_argument("--big", type=float, default=5000); ap.add_argument("--minpeak", type=float, default=5)
    ap.add_argument("--class", dest="cls", type=int, default=3); ap.add_argument("-v", action="store_true")
    a = ap.parse_args()
    tot = {}; per = collections.defaultdict(dict)
    for s in range(1, a.seeds + 1):
        rows = load(a.root, s)
        size = collections.defaultdict(float)
        for q, n, c, sz, *_ in rows: size[(n, q)] += sz
        big = collections.defaultdict(float)
        for (n, q), v in size.items(): big[n] = max(big[n], v)
        series = collections.defaultdict(list)
        for q, n, c, sz, e, p, sv, nsv in rows:
            if c == a.cls: series[n].append((q, sz, num(e), num(p), sv, nsv))
        for n, ser in series.items():
            ser.sort()
            szs = [x[1] for x in ser]
            peak = max(szs); ip = szs.index(peak)
            if peak < a.minpeak: continue
            after = szs[ip:]
            mn = min(after); end = szs[-1]
            eneg = sum(1 for x in ser if x[2] is not None and x[2] < 0); en = sum(1 for x in ser if x[2] is not None)
            per[s][n] = dict(peak=peak, q_peak=ser[ip][0], end=end, mn=mn, fell=mn < 0.05 * peak, fell_end=end < 0.05 * peak,
                             big=big[n] >= a.big, eneg=eneg, en=en, nq=len(ser))
    out = []
    for key, name in ((None, "全部"), (True, "大国")):
        cnt = n_all = fell = fell_end = 0; negq = nq = 0
        for s, d in per.items():
            for n, v in d.items():
                if key is True and not v["big"]: continue
                n_all += 1; fell += v["fell"]; fell_end += v["fell_end"]; negq += v["eneg"]; nq += v["en"]
        out.append((name, n_all, fell, fell_end, negq / nq if nq else float('nan')))
    print(f"# class {a.cls}  root={a.root}  seeds={a.seeds}  big>={a.big}K  minpeak>={a.minpeak}K")
    print("範囲\t種×国\t経路の最小がピーク5%未満\t終わりが5%未満\t所得が負の四半期の割合")
    for name, n_all, fell, fe, fr in out:
        print(f"{name}\t{n_all}\t{fell}\t{fe}\t{fr:.3f}")
    if a.v:
        print("\n国別（大国のみ）：種のうち経路の最小が5%未満の数／種の数・所得が負の四半期の割合の種中央値・ピーク（千人）の中央値")
        bynat = collections.defaultdict(list)
        for s, d in per.items():
            for n, v in d.items():
                if v["big"]: bynat[n].append(v)
        for n, vs in sorted(bynat.items(), key=lambda kv: -statistics.median(v["peak"] for v in kv[1])):
            print(f"{n}\t{sum(v['fell'] for v in vs)}/{len(vs)}\t{statistics.median([v['eneg']/v['en'] for v in vs if v['en']] or [float('nan')]):.2f}\t{statistics.median(v['peak'] for v in vs):.0f}\t終わり/ピーク中央値 {statistics.median(v['end']/v['peak'] for v in vs):.3f}")

if __name__ == "__main__":
    main()
