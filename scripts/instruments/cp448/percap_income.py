#!/usr/bin/env python
"""(448) 資本家の消失・段0：資本家の 1 人あたり（千人あたり）の所得の最悪値（読むだけ）。

(種, 国) ごとに 四半期の earnedPerMonth÷sizeK（資本家）の最小値を取り、国ごとに種の中央値・最小を出す。〈分母が痩せると万倍に跳ねる〉量の大きさ（清 −228 万／千人）を測る。
入力：計器つき 10 種の生の class_s<seed>.tsv。--nations で国を絞る（既定＝計器の国 8 国）。
使い方：PYTHONUTF8=1 python percap_income.py <計器の生のフォルダ> [--seeds 10] [--nations 清,日本,...]
"""
import argparse, collections, os, statistics, sys
sys.path.insert(0, os.path.dirname(__file__))
from cap_count import load, num

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("--seeds", type=int, default=10)
    ap.add_argument("--nations", default="イギリス,オスマン,オーストリア,フランス,プロイセン,ロシア,日本,清")
    a = ap.parse_args(); want = set(a.nations.split(","))
    mins = collections.defaultdict(list)
    for s in range(1, a.seeds + 1):
        per = collections.defaultdict(list)
        for q, n, c, sz, e, p, sv, nsv in load(a.root, s):
            if c == 3 and n in want and sz > 0:
                ev = num(e)
                if ev is not None: per[n].append(ev / sz)
        for n, v in per.items(): mins[n].append(min(v))
    print(f"# 資本家の earnedPerMonth÷sizeK の (種,国) ごとの最小  root={a.root}")
    print("国\t種の数\t中央値\t最小")
    for n in sorted(mins):
        v = mins[n]; print(f"{n}\t{len(v)}\t{statistics.median(v):.4g}\t{min(v):.4g}")

if __name__ == "__main__":
    main()
