#!/usr/bin/env python
"""(448) 資本家の消失・段0：損失の負債化（資本家）が実質GDPの何%かを層ごとに数える（読むだけ）。

入力：計器つき 10 種の生の class_s／flow_s／money_s<seed>.tsv。層・大国の定義は loss_by_size.py と同じ。
      実質GDP＝money の行（event=update）の gdpRealPerMonth（四半期の末の月）。flow の amountPerMonth は〈月あたりの額〉。
出力：層ごとの 負債化÷実質GDP（月あたり同士）の件数・中央値・平均・90 分位。〈全層〉は大国の (種,国,四半期) すべて（負債化の無い四半期は 0）＝層の定義に依らない比べ方。
使い方：PYTHONUTF8=1 python loss_vs_gdp.py <計器の生のフォルダ> [--seeds 10] [--line 2935] [--big 5000]
"""
import argparse, csv, os, collections, statistics, sys
sys.path.insert(0, os.path.dirname(__file__))
from guard import check_line
from cap_count import base, load

def line(name, v):
    v = sorted(v)
    if not v: return f"{name}\t0"
    return f"{name}\t{len(v)}\t{statistics.median(v):.4f}\t{sum(v)/len(v):.4f}\t{v[int(0.9*(len(v)-1))]:.4f}"

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--line", type=int, default=2935); ap.add_argument("--big", type=float, default=5000); ap.add_argument("--src", default=None)
    a = ap.parse_args()
    check_line(a.src, a.line)
    n_match = 0
    bins = [("5%未満", 0.0, 0.05), ("5〜50%", 0.05, 0.5), ("50%以上", 0.5, 1e9)]
    res = {b[0]: [] for b in bins}; allq = []; n_matchTotal = 0
    for s in range(1, a.seeds + 1):
        rows = load(a.root, s)
        allsz = collections.defaultdict(float); size = {}
        for q, n, c, sz, *_ in rows:
            allsz[(n, q)] += sz
            if c == 3: size[(n, q)] = sz
        bigp = collections.defaultdict(float); peak = collections.defaultdict(float)
        for (n, q), v in allsz.items(): bigp[n] = max(bigp[n], v)
        for (n, q), v in size.items(): peak[n] = max(peak[n], v)
        gdp = {}
        with open(os.path.join(a.root, f"money_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                t = r["time"]
                if t.startswith("q") and r["event"] == "update":
                    try: gdp[(base(r["nation"]), int(t[1:]))] = float(r["gdpRealPerMonth"])
                    except ValueError: pass
        fin = collections.defaultdict(float)
        with open(os.path.join(a.root, f"flow_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["kind"] == "give" and r["sourceFile"] == "Simulation.cs" and r["sourceLine"] == str(a.line) and r["classId"] == "3":
                    fin[(base(r["nation"]), int(r["quarter"]))] += float(r["amountPerMonth"]); n_match += 1
        n_matchTotal += n_match; n_match = 0
        for (n, q), sz in size.items():
            if bigp[n] < a.big or peak[n] <= 0 or gdp.get((n, q), 0) <= 0: continue
            x = fin.get((n, q), 0.0) / gdp[(n, q)]
            allq.append(x)
            ratio = sz / peak[n]
            if x > 0:
                for name, lo, hi in bins:
                    if lo <= ratio < hi: res[name].append(x); break
    if n_matchTotal == 0: raise SystemExit(f"一致した行が 0 件（--line {a.line} が負債化の行でない）＝読まない")
    print(f"# 一致した資本家の負債化の行 {n_matchTotal}")
    print(f"# 資本家の損失の負債化÷実質GDP（月あたり同士・大国）root={a.root}")
    print("層\t件数\t中央値\t平均\t90分位")
    for name, _, _ in bins: print(line(name, res[name]))
    print(line("全層（負債化の無い四半期は0）", allq))

if __name__ == "__main__":
    main()
