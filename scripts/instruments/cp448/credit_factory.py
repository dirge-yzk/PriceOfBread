#!/usr/bin/env python
"""(448) 資本家の消失・段0：与信の残高（月のGDP比）と工場の倒産・休止の数を計器の国ごとに数える（読むだけ）。

入力：計器つき 10 種の生の money_s<seed>.tsv（event=update の行・outstandingLoansStock÷gdpRealPerMonth）と factory_s<seed>.tsv
      （factoryCount・factoryBankruptCount・factoryIdleCount・四半期ごと）。
出力：国ごとに 与信÷月GDP の q=--at 四半期（既定 660〜667 の平均）の種の中央値・最大／工場の倒産数÷工場数・休止数÷工場数の全四半期の平均の種の中央値。
使い方：PYTHONUTF8=1 python credit_factory.py <計器の生のフォルダ> [--seeds 10] [--from 660]
"""
import argparse, csv, collections, os, statistics, sys
sys.path.insert(0, os.path.dirname(__file__))
from cap_count import base

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--from", dest="frm", type=int, default=660)
    a = ap.parse_args()
    loans = collections.defaultdict(list); bank = collections.defaultdict(list); idle = collections.defaultdict(list)
    for s in range(1, a.seeds + 1):
        tmp = collections.defaultdict(list)
        with open(os.path.join(a.root, f"money_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                t = r["time"]
                if t.startswith("q") and r["event"] == "update" and int(t[1:]) >= a.frm:
                    try:
                        g = float(r["gdpRealPerMonth"]); l = float(r["outstandingLoansStock"])
                        if g > 0: tmp[base(r["nation"])].append(l / g)
                    except ValueError: pass
        for n, v in tmp.items(): loans[n].append(sum(v) / len(v))
        fb = collections.defaultdict(list); fi = collections.defaultdict(list)
        with open(os.path.join(a.root, f"factory_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                try:
                    n = float(r["factoryCount"])
                    if n > 0: fb[base(r["nation"])].append(float(r["factoryBankruptCount"]) / n); fi[base(r["nation"])].append(float(r["factoryIdleCount"]) / n)
                except (ValueError, KeyError): pass
        for n in fb: bank[n].append(sum(fb[n]) / len(fb[n])); idle[n].append(sum(fi[n]) / len(fi[n]))
    print(f"# root={a.root}  与信÷月GDP は q>={a.frm} の平均")
    print("国\t与信÷月GDP 種の中央値\t最大\t倒産÷工場数 平均の中央値\t休止÷工場数 平均の中央値")
    for n in sorted(loans):
        print(f"{n}\t{statistics.median(loans[n]):.2f}\t{max(loans[n]):.2f}\t{statistics.median(bank[n]):.3f}\t{statistics.median(idle[n]):.3f}")

if __name__ == "__main__":
    main()
