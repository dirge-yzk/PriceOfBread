#!/usr/bin/env python
"""(448) 診断：使い捨ての木（Simulation.LossBound に DG_FILE への書き出しを足した木・コミットしない）が書いた損失の工場の行を、財ごと・選び方（平滑採算が負か）ごとに数える（読むだけ）。
列：year／nation／投資家か／財／損失／平滑採算／Scale／産出／LastRevenue／LastCost／不足／倒産か。使い方：PYTHONUTF8=1 python dump_by_good.py <loss_dump.tsv>"""
import sys, collections, statistics as S
tot = collections.defaultdict(float); by = collections.defaultdict(lambda: collections.defaultdict(float)); rows = []
for l in open(sys.argv[1], encoding="utf-8"):
    f = l.rstrip("\n").split("\t")
    if len(f) < 12 or f[2] != "1" or f[1] not in ("日本", "清", "オスマン"): continue
    loss = float(f[4]); sel = "sel" if float(f[5]) < 0 else "notsel"
    tot[sel] += loss; by[sel][f[3]] += loss
    if sel == "notsel" and f[3] in ("パン", "小麦粉"): rows.append([float(x) for x in f[4:11]])
print({k: f"{v:.4g}" for k, v in tot.items()})
for k in ("sel", "notsel"): print(k, sorted(((round(v), g) for g, v in by[k].items()), reverse=True)[:12])
for i, n in enumerate(("loss", "smoothedMargin", "scale", "output", "lastRevenue", "lastCost", "need")):
    print("パン・小麦粉（平滑採算が非負）", n, "median", S.median([r[i] for r in rows]))
