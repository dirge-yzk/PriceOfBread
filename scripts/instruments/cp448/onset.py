#!/usr/bin/env python
"""(448) 資本家の消失・段0：没落の入口の物差し（読むだけ）。

資本家が増えて薄まる（人数の伸びに対して貯蓄が伸びない）のか、支払いが所得を超えて貯蓄を食うのかを、ピークと t50（ピークの 50% を割った最初の四半期）で比べる。
大国＝(種,国) の全階級の人数の和の最大が --big 千人以上。出力は (種,国) ごとの比の中央値。
  人数の伸び＝ピーク÷最初／貯蓄の伸び＝ピークの貯蓄÷最初の貯蓄／1 人あたり貯蓄の伸び＝その比
  t50 の 1 人あたり貯蓄÷国の平均（全階級の貯蓄の和÷人数の和）／ピークの同比／t50 前後の 支払い÷所得
使い方：PYTHONUTF8=1 python onset.py <計器の生のフォルダ> [--seeds 10] [--big 5000]
"""
import argparse, collections, os, statistics, sys
sys.path.insert(0, os.path.dirname(__file__))
from cap_count import load, num

def med(x):
    x = [v for v in x if v == v and v != float('inf')]
    return statistics.median(x) if x else float('nan')

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--big", type=float, default=5000)
    a = ap.parse_args()
    R = collections.defaultdict(list)
    for s in range(1, a.seeds + 1):
        rows = load(a.root, s)
        tot_sz = collections.defaultdict(float); tot_sv = collections.defaultdict(float); cap = collections.defaultdict(dict)
        for q, n, c, sz, e, p, sv, nsv in rows:
            tot_sz[(n, q)] += sz; tot_sv[(n, q)] += max(sv, 0)
            if c == 3: cap[n][q] = (sz, sv, num(e), num(p))
        bigp = collections.defaultdict(float)
        for (n, q), v in tot_sz.items(): bigp[n] = max(bigp[n], v)
        for n, d in cap.items():
            if bigp[n] < a.big: continue
            qs = sorted(d); sz = [d[q][0] for q in qs]; pk = max(sz)
            if pk < 5: continue
            ip = sz.index(pk); t50 = next((i for i in range(ip, len(qs)) if sz[i] < 0.5 * pk), None)
            if t50 is None: continue
            q0, qp, qt = qs[0], qs[ip], qs[t50]
            if d[q0][0] <= 0 or d[q0][1] <= 0: continue
            R["人数の伸び（ピーク÷最初）"].append(d[qp][0] / d[q0][0])
            R["貯蓄の伸び（ピーク÷最初）"].append(d[qp][1] / d[q0][1])
            R["1人あたり貯蓄の伸び"].append((d[qp][1] / d[qp][0]) / (d[q0][1] / d[q0][0]))
            avg_p = tot_sv[(n, qp)] / tot_sz[(n, qp)]; avg_t = tot_sv[(n, qt)] / tot_sz[(n, qt)]
            avg_0 = tot_sv[(n, q0)] / tot_sz[(n, q0)]
            R["最初：資本家の1人あたり貯蓄÷国の平均"].append((d[q0][1] / d[q0][0]) / avg_0 if avg_0 > 0 else float('nan'))
            R["ピーク：同"].append((d[qp][1] / d[qp][0]) / avg_p if avg_p > 0 else float('nan'))
            R["t50：同"].append((d[qt][1] / d[qt][0]) / avg_t if avg_t > 0 else float('nan'))
            e, p = d[qt][2], d[qt][3]
            if e and p and e > 0: R["t50：支払い÷所得"].append(p / e)
            e, p = d[qp][2], d[qp][3]
            if e and p and e > 0: R["ピーク：支払い÷所得"].append(p / e)
    print(f"# root={a.root} big>={a.big}K  (種,国) の中央値")
    for k, v in R.items(): print(f"{k}\t{med(v):.3f}\t(n={len(v)})")

if __name__ == "__main__":
    main()
