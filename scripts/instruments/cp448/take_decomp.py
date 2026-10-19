#!/usr/bin/env python
"""(448) 資本家の消失・段0：ピーク→t50 の窓で、資本家の貯蓄の出入りを書き手の行ごとに分ける（読むだけ）。

入力：計器つき 10 種の生の class_s／flow_s<seed>.tsv（flow は計器の国だけ）。窓＝(種,国) の資本家のピークの四半期から t50（ピークの 50% を割った最初の四半期）まで。
出力：kind・sourceFile:行・相手側（counterpartySide）ごとの amountPerMonth の窓の和（全 (種,国) の和）と、出る側（負）の合計に対する割合。
      settle（所得−支払いの純）の和も出す。行番号は木によって動く。
使い方：PYTHONUTF8=1 python take_decomp.py <計器の生のフォルダ> [--seeds 10] [--big 5000] [--top 14]
"""
import argparse, csv, os, collections, sys
sys.path.insert(0, os.path.dirname(__file__))
from cap_count import base, load

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--big", type=float, default=5000); ap.add_argument("--top", type=int, default=14)
    a = ap.parse_args()
    tot = collections.Counter(); npair = 0
    for s in range(1, a.seeds + 1):
        rows = load(a.root, s)
        allsz = collections.defaultdict(float); cap = collections.defaultdict(dict)
        for q, n, c, sz, *_ in rows:
            allsz[(n, q)] += sz
            if c == 3: cap[n][q] = sz
        bigp = collections.defaultdict(float)
        for (n, q), v in allsz.items(): bigp[n] = max(bigp[n], v)
        win = {}
        for n, d in cap.items():
            if bigp[n] < a.big: continue
            qs = sorted(d); sz = [d[q] for q in qs]; pk = max(sz)
            if pk < 5: continue
            ip = sz.index(pk); t50 = next((i for i in range(ip, len(qs)) if sz[i] < 0.5 * pk), None)
            if t50 is not None: win[n] = (qs[ip], qs[t50])
        with open(os.path.join(a.root, f"flow_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            seen = set()
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["classId"] != "3": continue
                n = base(r["nation"])
                if n not in win: continue
                q = int(r["quarter"])
                if not (win[n][0] <= q <= win[n][1]): continue
                seen.add(n)
                key = (r["kind"], (r["sourceFile"] + ":" + r["sourceLine"]) if r["sourceFile"] else "-", r["counterpartySide"])
                tot[key] += float(r["amountPerMonth"])
            npair += len(seen)
    neg = sum(v for k, v in tot.items() if v < 0 and k[0] != "settle")
    pos = sum(v for k, v in tot.items() if v > 0 and k[0] != "settle")
    print(f"# 窓（ピーク→t50）の資本家の貯蓄の出入り  root={a.root}  (種,国)={npair}")
    print(f"出る側の合計 {neg:.4g}  入る側の合計 {pos:.4g}  settle（所得−支払い）の和 {sum(v for k, v in tot.items() if k[0] == 'settle'):.4g}")
    print("kind\t書き手の行\t相手側\t窓の和\t出る側に対する割合")
    for k, v in sorted(tot.items(), key=lambda kv: kv[1])[:a.top]:
        if k[0] == "settle": continue
        print(f"{k[0]}\t{k[1]}\t{k[2]}\t{v:.4g}\t{v/neg:.3f}" if neg else f"{k}\t{v}")
    print("--- 入る側の上位")
    for k, v in sorted(tot.items(), key=lambda kv: -kv[1])[:8]:
        print(f"{k[0]}\t{k[1]}\t{k[2]}\t{v:.4g}\t{v/pos:.3f}")

if __name__ == "__main__":
    main()
