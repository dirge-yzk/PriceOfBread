#!/usr/bin/env python
"""(448) 資本家の消失・段0：損失の負債化（資本家）を〈階級の大きさ÷そのピーク〉の層に分けて数える（読むだけ）。

入力：計器つき 10 種の生の class_s<seed>.tsv（資本家の sizeK）と flow_s<seed>.tsv（Simulation.cs の負債化の行・資本家）。
定義：層＝その (種, 国, 四半期) の資本家 sizeK ÷ (種, 国) の資本家のピーク。〈5% 未満〉〈5〜50%〉〈50% 以上〉。国の名前は 共和制・立憲王政 を外して束ねる。
      大国＝(種, 国) の全階級の人数の和の最大が --big 千人以上。
出力：層ごとの 負債化の和・割合・(種,国,四半期) の数・1 人あたり（千人あたり）の負債化の中央値。
使い方：PYTHONUTF8=1 python loss_by_size.py <計器の生のフォルダ> [--seeds 10] [--line 2935] [--big 5000] [--cls 3]
"""
import argparse, csv, os, collections, statistics, sys
sys.path.insert(0, os.path.dirname(__file__))
from cap_count import base, load

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--line", type=int, default=2935)
    ap.add_argument("--big", type=float, default=5000); ap.add_argument("--cls", type=int, default=3)
    a = ap.parse_args()
    bins = [("5%未満", 0.0, 0.05), ("5〜50%", 0.05, 0.5), ("50%以上", 0.5, 1e9)]
    tot = {b[0]: 0.0 for b in bins}; n = {b[0]: 0 for b in bins}; per = {b[0]: [] for b in bins}
    for s in range(1, a.seeds + 1):
        rows = load(a.root, s)
        size = collections.defaultdict(float); allsz = collections.defaultdict(float)
        for q, nat, c, sz, *_ in rows:
            allsz[(nat, q)] += sz
            if c == a.cls: size[(nat, q)] = sz
        bigp = collections.defaultdict(float)
        for (nat, q), v in allsz.items(): bigp[nat] = max(bigp[nat], v)
        peak = collections.defaultdict(float)
        for (nat, q), v in size.items(): peak[nat] = max(peak[nat], v)
        with open(os.path.join(a.root, f"flow_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["kind"] != "give" or r["sourceFile"] != "Simulation.cs" or r["sourceLine"] != str(a.line) or int(r["classId"]) != a.cls: continue
                nat = base(r["nation"]); q = int(r["quarter"])
                if bigp[nat] < a.big or (nat, q) not in size or peak[nat] <= 0: continue
                ratio = size[(nat, q)] / peak[nat]; v = float(r["amountPerMonth"])
                for name, lo, hi in bins:
                    if lo <= ratio < hi:
                        tot[name] += v; n[name] += 1; per[name].append(v / max(size[(nat, q)], 1e-12)); break
    grand = sum(tot.values())
    print(f"# class {a.cls} 損失の負債化（大国のみ・big>={a.big}K）root={a.root}")
    print("層\t負債化の和\t割合\t(種,国,四半期)の数\t千人あたりの中央値")
    for name, _, _ in bins:
        med = statistics.median(per[name]) if per[name] else float('nan')
        print(f"{name}\t{tot[name]:.4g}\t{tot[name]/grand if grand else 0:.3f}\t{n[name]}\t{med:.4g}")

if __name__ == "__main__":
    main()
