# -*- coding: utf-8 -*-
"""(384) 形の探り（腕ではない）の読み：scalars_s<種>.tsv の追加の列の累計（国ごとの最後の行の和）と M2 の数を印字する。使い方：python smoke_read.py <scalars_s1.tsv>"""
import sys, statistics
f = sys.argv[1]
rows = [l.rstrip("\n").split("\t") for l in open(f, encoding="utf-8")]
h = rows[0]; ix = {n: i for i, n in enumerate(h)}
last = {}
for r in rows[1:]: last[r[ix["nation"]]] = r
keys = ["pbDomActive", "pbDomFlip", "pbDomDecisive", "pbDomExec", "pbDomMismatch", "pbRegime", "pbRegimeViolent", "factionRevolts"]
print(len(rows) - 1, "rows; nations", len(last), {k: sum(float(r[ix[k]]) for r in last.values()) for k in keys})
print("gdpDrawdown>0.05 rows", sum(1 for r in rows[1:] if float(r[ix["gdpDrawdown"]]) > 0.05))
print("nonlegislative rows", sum(1 for r in rows[1:] if int(r[ix["polityCode"]]) not in (3, 4)))
print("defeatShock>=0.99 rows", sum(1 for r in rows[1:] if float(r[ix["defeatShock"]]) >= 0.99))
print("gate-open rows (meanSatPol<0.43 & maxSeat>0.6 & CM/Rep)", sum(1 for r in rows[1:] if float(r[ix["meanSatPol"]]) < 0.43 and float(r[ix["maxSeat"]]) > 0.6 and int(r[ix["polityCode"]]) in (3, 4)))
print("militancyMean median", round(statistics.median(float(r[ix["militancyMean"]]) for r in rows[1:]), 4))
