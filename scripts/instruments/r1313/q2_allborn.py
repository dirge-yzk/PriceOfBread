# (1313) c136ak 問い 2 の母集合：途中で生まれた全ての国（生まれごと）の最初の 20 四半期の科目ごとの合計と、生まれの時の国庫・民間の貨幣・
# 官僚と資本家の大きさ（class_s の生まれの年の行）。読むだけ。
# 使い方：python -X utf8 -I q2_allborn.py <出力.tsv> <種 a-b> 名前=<ins> ...
import sys, csv, collections
out = sys.argv[1]; a, b = map(int, sys.argv[2].split('-')); arms = [x.split('=', 1) for x in sys.argv[3:]]
CATS = ['Tax', 'Tariff', 'StateMonopoly', 'Borrow', 'Devalue', 'StateFactory', 'TributeIn', 'OtherIn', 'Army', 'Navy', 'Admin', 'Education', 'Welfare', 'Pension', 'Modernization', 'AntiCorruption', 'Interest', 'DebtRepay', 'Subsidy', 'ColonialUpkeep', 'TributeOut', 'OtherOut']
def fl(x):
    try: return float(x)
    except: return float('nan')
w = open(out, 'w', encoding='utf-8', newline='\n')
w.write('arm\tseed\tnation\tbornQ\tbornYear\tnq\tbornT\tpriv0\tT20\tpriv20\tbur0K\tcap0K\tpop0K\t' + '\t'.join(CATS) + '\tother\n')
for name, d in arms:
    for s in range(a, b + 1):
        births = collections.defaultdict(list); rows = collections.defaultdict(list)
        with open(f'{d}/treasledger_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); next(r)
            for q, y, n, it, v in r:
                q = int(q)
                if it == '_bornTreasury': births[n].append((q, y, fl(v)))
                rows[n].append((q, it, fl(v)))
        cls = {}
        with open(f'{d}/class_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); next(r)
            for row in r:
                if row[0] == 'open': continue
                cls[(row[1], row[2], row[4])] = fl(row[5])
        popk = {}
        with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for row in r: popk[(int(row[ix['quarter']]), row[ix['nation']])] = fl(row[ix['totalPopulationK']])
        for n, L in births.items():
            for i, (q0, y0, bt) in enumerate(L):
                if q0 <= 0: continue
                q1 = min(L[i + 1][0] if i + 1 < len(L) else 10**9, q0 + 20)
                sm = collections.defaultdict(float); te = pm = pm0 = float('nan'); nq = set(); other = 0.0
                for q, it, v in rows[n]:
                    if not (q0 <= q < q1): continue
                    nq.add(q)
                    if it in CATS: sm[it] += v
                    elif it == '_treasuryEnd': te = v
                    elif it == '_privateMoney':
                        pm = v
                        if q == q0: pm0 = v
                    elif not it.startswith('_'): other += v
                yy = y0.split('.')[0]
                bur = cls.get((n, yy, '官僚'), float('nan')); cap = cls.get((n, yy, '資本家'), float('nan'))
                w.write(f'{name}\t{s}\t{n}#{i}\t{q0}\t{yy}\t{len(nq)}\t{bt:.6g}\t{pm0:.6g}\t{te:.6g}\t{pm:.6g}\t{bur:.6g}\t{cap:.6g}\t{popk.get((q0, n), float("nan")):.6g}\t' + '\t'.join(f'{sm[c]:.6g}' for c in CATS) + f'\t{other:.6g}\n')
w.close()
