#!/usr/bin/env python
"""(448) 資本家の消失・段0：損失の負債化（BackstopNegativeSavings＝Simulation.cs の `World.Transfer(... outside: Credit)`）を階級ごとに数える（読むだけ）。

入力：計器つき 10 種の生の flow_s<seed>.tsv（kind=give・sourceFile=Simulation.cs・sourceLine=損失の負債化の行）。行番号は --line で渡す（既定 2935＝m443 の木）。
出力：階級ごとの 負債化の総額（全種・全四半期の amountPerMonth の和＝月あたりの額を四半期で足した相対量）・その階級の割合・負債化が立った (種,国,四半期) の数。
使い方：PYTHONUTF8=1 python backstop_by_class.py <計器の生のフォルダ> [--seeds 10] [--line 2935] [--src <その木の Simulation.cs>]
--src を渡すと、その行が `outside: World.Outside.Credit` の呼びか確かめる（行番号のずれの検査・B の木では必須）。
"""
import argparse, csv, os, collections, sys
sys.path.insert(0, os.path.dirname(__file__))
from guard import check_line

NAMES = {0:"貴族",1:"農民",2:"工場労働者",3:"資本家",4:"技師",5:"兵士",6:"聖職者",7:"教師",8:"官僚",9:"医者",10:"商人",11:"職人",12:"使用人"}

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--line", type=int, default=2935); ap.add_argument("--src", default=None)
    a = ap.parse_args()
    check_line(a.src, a.line)
    tot = collections.Counter(); cnt = collections.Counter()
    for s in range(1, a.seeds + 1):
        with open(os.path.join(a.root, f"flow_s{s}.tsv"), encoding="utf-8", newline="") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["kind"] == "give" and r["sourceFile"] == "Simulation.cs" and r["sourceLine"] == str(a.line):
                    c = int(r["classId"]); v = float(r["amountPerMonth"])
                    tot[c] += v; cnt[c] += 1
    grand = sum(tot.values())
    n_match = sum(cnt.values())
    if n_match == 0: raise SystemExit(f"一致した行が 0 件（--line {a.line} が負債化の行でない）＝読まない")
    print(f"# 一致した (種,国,四半期,階級) の組 {n_match}（基準 m443 の資本家は 17640 組）")
    print(f"# backstop line {a.line}  root={a.root}")
    print("階級\t負債化の和\t割合\t立った(種,国,四半期)の数")
    for c, v in sorted(tot.items(), key=lambda kv: -kv[1]):
        print(f"{NAMES.get(c, c)}\t{v:.4g}\t{v/grand:.3f}\t{cnt[c]}")

if __name__ == "__main__":
    main()
