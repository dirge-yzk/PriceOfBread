# (1144) t1buy_s の 10 種・全期間の和（読むだけ）。使い方: python -X utf8 -I t1sum.py 名前=<ins のフォルダ> ...
import sys, glob, os, collections
COLS = ['rows','oldSkip','instSkip','unclassified','popA','popAPx','popASmallD','popADealX','popAQtyX','popAValX','smallD','gateEval','gateMismatch',
        'u1Deals','u1Unok','u1Viol','u1ViolAbs','u1SmallD','u1SmallDViol','u1Excess','u1Qty','selfN','selfDiff','retreatDeals','retreatQty','dealsX','qtyX']
for a in sys.argv[1:]:
    name, d = a.split('=', 1)
    tot = collections.Counter(); mx = {'u1MaxRel': float('-inf'), 'selfMaxRel': 0.0}; seeds = 0
    for f in sorted(glob.glob(os.path.join(d, 't1buy_s*.tsv'))):
        seeds += 1
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                v = l.rstrip('\n').split('\t')
                if len(v) < len(h): continue
                for k in COLS:
                    try: tot[k] += float(v[ix[k]])
                    except Exception: pass
                for k in mx:
                    try:
                        x = float(v[ix[k]])
                        if x == x and x > mx[k]: mx[k] = x
                    except Exception: pass
    print(f'{name} 種 {seeds} ' + ' '.join(f'{k}={tot[k]:.6g}' for k in COLS) + ' ' + ' '.join(f'{k}={mx[k]:.3g}' for k in mx))
