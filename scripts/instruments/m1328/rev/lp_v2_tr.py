# (1328) 合流の記録：c138lp の反証役 v2 の台本（段の移りを軸×前の段×後の段で数える）の写し・生のフォルダと種の起点を引数にした。
#   python -X utf8 -I lp_v2_tr.py <種の起点> <名前=生のフォルダ> ...
#   MonetaryStandard の段：0 不換・1 銀・2 金（金→銀は (MonetaryStandard, 2, 1)・銀→不換は (MonetaryStandard, 1, 0)）。
import sys, os, csv, collections
def run(d, lab, s0):
    t = collections.Counter()
    for s in range(s0, s0 + 10):
        p = os.path.join(d, f"monlaw_s{s}.tsv")
        if not os.path.exists(p): t['miss'] += 1; continue
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            prev = {}
            for row in r:
                if len(row) < len(h): continue
                uid = row[ix['nationUid']]
                cur = {a: int(float(row[ix[a]])) for a in ('Suffrage', 'MonetaryStandard', 'Inheritance') if a in ix}
                if uid in prev:
                    for a, v in cur.items():
                        pv = prev[uid][a]
                        if v != pv: t[(a, pv, v)] += 1
                prev[uid] = cur
    print(lab, ' '.join(f"{k}:{v}" for k, v in sorted(t.items(), key=str)))
s0 = int(sys.argv[1])
for a in sys.argv[2:]:
    lab, d = a.split('=', 1)
    if os.path.isdir(d + '/ins'): d = d + '/ins'
    run(d, lab, s0)
