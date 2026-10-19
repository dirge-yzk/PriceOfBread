#!/usr/bin/env python
"""(448) 資本家の消失・段0：没落の入口を数える（読むだけ）。

問い：資本家の人数がピークの 50% を割った四半期（t50）の前に、損失の負債化（Simulation.cs の行）が立っていたか・所得は負だったか。
入力：計器つき 10 種の生の class_s<seed>.tsv と flow_s<seed>.tsv。大国＝(種,国) の全階級の人数の和の最大が --big 千人以上。
⚠flow_s は計器の対象の国（POB_INSTRUMENTS_NATIONS＝既定 8 国）の行しか無い＝負債化の割合は〈flow に現れる国〉だけで数える（それ以外の国は〈0〉でなく〈見えない〉＝分母に入れない）。
  所得が負の割合は class_s（全国）で数えるので〈全国〉と〈計器の国〉の 2 本を出す。
出力：t50 に達した (種,国) の数・t50 の前 --win 四半期のうち 負債化が立った割合の中央値・所得が負の割合の中央値・支払い÷所得の中央値
      ・ピークから t50 までの四半期数の中央値・t50 から 5% 未満までの四半期数の中央値。
使い方：PYTHONUTF8=1 python descent.py <計器の生のフォルダ> [--seeds 10] [--line 2935] [--big 5000] [--win 8]
"""
import argparse, csv, os, collections, statistics, sys
sys.path.insert(0, os.path.dirname(__file__))
from cap_count import base, load, num

def med(x):
    x = [v for v in x if v == v]
    return statistics.median(x) if x else float('nan')

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--line", type=int, default=2935)
    ap.add_argument("--big", type=float, default=5000); ap.add_argument("--win", type=int, default=8)
    a = ap.parse_args()
    nreach = 0; fr_bs = []; fr_neg = []; fr_neg_i = []; ratio = []; t_down = []; t_to5 = []; n_big = 0; n5 = 0; n_inst = 0
    for s in range(1, a.seeds + 1):
        rows = load(a.root, s)
        allsz = collections.defaultdict(float); cap = collections.defaultdict(dict)
        for q, nat, c, sz, e, p, sv, nsv in rows:
            allsz[(nat, q)] += sz
            if c == 3: cap[nat][q] = (sz, num(e), num(p))
        bigp = collections.defaultdict(float)
        for (nat, q), v in allsz.items(): bigp[nat] = max(bigp[nat], v)
        bs = collections.defaultdict(float); inst = set()
        with open(os.path.join(a.root, f"flow_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                inst.add(base(r["nation"]))
                if r["kind"] == "give" and r["sourceFile"] == "Simulation.cs" and r["sourceLine"] == str(a.line) and r["classId"] == "3":
                    bs[(base(r["nation"]), int(r["quarter"]))] += float(r["amountPerMonth"])
        for nat, d in cap.items():
            if bigp[nat] < a.big: continue
            qs = sorted(d); sizes = [d[q][0] for q in qs]
            peak = max(sizes)
            if peak < 5: continue
            n_big += 1
            ip = sizes.index(peak)
            t50 = next((i for i in range(ip, len(qs)) if sizes[i] < 0.5 * peak), None)
            if t50 is None: continue
            nreach += 1
            t5 = next((i for i in range(t50, len(qs)) if sizes[i] < 0.05 * peak), None)
            if t5 is not None: n5 += 1; t_to5.append(t5 - t50)
            t_down.append(t50 - ip)
            lo = max(0, t50 - a.win)
            win = qs[lo:t50 + 1]
            ev = [d[q][1] for q in win if d[q][1] is not None]
            fneg = sum(1 for x in ev if x < 0) / len(ev) if ev else float('nan')
            fr_neg.append(fneg)
            if nat in inst:
                n_inst += 1; fr_neg_i.append(fneg)
                fr_bs.append(sum(1 for q in win if bs.get((nat, q), 0) > 0) / len(win))
            for q in win:
                e, p = d[q][1], d[q][2]
                if e and p and e > 0: ratio.append(p / e)
    print(f"# root={a.root} big>={a.big}K win={a.win}")
    print(f"大国の (種,国) 資本家のピーク>=5K\t{n_big}\nピークの50%を割った\t{nreach}\nさらに5%未満へ\t{n5}\n（うち計器の国で t50 に達した）\t{n_inst}")
    print(f"t50 の前 {a.win} 四半期で 負債化が立った割合（中央値・計器の国だけ）\t{med(fr_bs):.3f}")
    print(f"同・所得が負だった割合（中央値）\t全国 {med(fr_neg):.3f}\t計器の国だけ {med(fr_neg_i):.3f}")
    print(f"同・支払い÷所得（所得>0 の四半期の中央値）\t{med(ratio):.2f}")
    print(f"ピーク→t50 の四半期数（中央値）\t{med(t_down):.0f}\nt50→5%未満の四半期数（中央値）\t{med(t_to5):.0f}")

if __name__ == "__main__":
    main()
