#!/usr/bin/env python
# (353) 識字と教育・医療の充足の年表（読むだけ）：ce_s の C 行の literacyWeightedSum／eduFillWeightedSum／healthFillWeightedSum を popK（規模の月平均）で割った人口加重の平均。
# 使い方：PYTHONUTF8=1 python an_ce_lit.py <計器の出力フォルダ> <国名> [種=1]　　大衆＝農民・工場労働者・職人・使用人（階級をまとめた人口加重）。窓＝四半期の番号（-1 は最後まで）
import csv, sys, collections
d, nat = sys.argv[1], sys.argv[2]; seed = sys.argv[3] if len(sys.argv) > 3 else "1"
rd = csv.reader(open(f"{d}/ce_s{seed}.tsv", encoding="utf-8"), delimiter="\t"); hdr = next(rd); ix = {h: i for i, h in enumerate(hdr)}
MASS = {"農民", "工場労働者", "職人", "使用人"}
acc = collections.defaultdict(lambda: collections.defaultdict(float))
for r in rd:
    if r[0] != "C" or r[ix["nation"]] != nat: continue
    q = int(r[ix["quarter"]]); cl = r[ix["className"]]
    for key in ((q, "大衆") if cl in MASS else (q, "他"), (q, cl)):
        a = acc[key]
        for h in ("popK", "literacyWeightedSum", "eduFillWeightedSum", "healthFillWeightedSum", "eduPaidPerMonth", "healthPaidPerMonth"):
            a[h] += float(r[ix[h]])
for (q, cl) in sorted(acc, key=lambda k: (k[0], k[1])):
    a = acc[(q, cl)]; p = a["popK"]
    if p <= 0: continue
    print(f"Q{q:<3} {cl:6} 識字 {a['literacyWeightedSum']/p:5.3f} 教育の充足 {a['eduFillWeightedSum']/p:5.3f} 医療の充足 {a['healthFillWeightedSum']/p:5.3f} 教育の自費 {a['eduPaidPerMonth']:9.1f} 医療の自費 {a['healthPaidPerMonth']:9.1f}")
