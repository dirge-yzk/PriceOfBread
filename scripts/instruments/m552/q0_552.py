# (552) 合流の記録の読み手（読むだけ・採否に使わない）。使い方：python q0_552.py ラベル=<ins> [ラベル=<ins> ...]
#  ivf_s の四半期 0（Q0）の steamMcOverWageMain（蒸気の動力の限界費用÷労働の値段・本土）と laborProdOverDemandMain（労働の産出÷需要・本土）
#  ＝国ごとの 10 種の中央値（英・清・マラータ同盟ほか）
import csv, glob, os, sys, statistics as st
NAT = ["イギリス", "清", "マラータ同盟", "フランス", "プロイセン", "アメリカ", "日本", "ハイデラバード"]
COLS = ["steamMcOverWageMain", "laborProdOverDemandMain", "laborPriceOverRefMain"]
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')
for a in sys.argv[1:]:
    lab, d = a.split('=', 1)
    acc = {}
    for p in sorted(glob.glob(os.path.join(d, 'ivf_s*.tsv'))):
        for x in rows(p):
            if x['quarter'] != '0' or x['nation'] not in NAT: continue
            for c in COLS: acc.setdefault((x['nation'], c), []).append(fl(x[c]))
    print(f'## {lab}（Q0・10 種の中央値）')
    for c in COLS:
        print('  ' + c + '：' + '・'.join(f'{n} {med(acc.get((n, c), [])):.3f}' for n in NAT))
