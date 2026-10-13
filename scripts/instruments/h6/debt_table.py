"""(294) 読むだけ：英・仏・蘭・葡の Q1〜Q8 の債務・利率・利払い÷歳入・債務÷年GDP・門の閉じた月の表（nation_s<seed>.tsv の種の中央〔最小〜最大〕）
python debt_table.py <ins> [qmax=8]"""
import sys, os, glob, statistics, collections
D = sys.argv[1]; QMAX = int(sys.argv[2]) if len(sys.argv) > 2 else 8
NAT = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
def f(x):
    try: return float(x)
    except: return float('nan')
data = collections.defaultdict(lambda: collections.defaultdict(dict))  # nat -> q -> seed -> row
for p in sorted(glob.glob(os.path.join(D, 'nation_s*.tsv'))):
    sd = int(os.path.basename(p)[len('nation_s'):-4])
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(head)}
        for ln in fh:
            r = ln.rstrip('\n').split('\t')
            if r[ix['nation']] in NAT:
                q = int(r[ix['quarter']])
                if q < QMAX: data[r[ix['nation']]][q][sd] = {h: r[i] for h, i in ix.items() if h in ('govDebtStock','interestMeanPerMonth','revenueBaseMeanPerMonth','nominalGdpPerYear','borrowGateClosedMonths','borrowGateObservedMonths')}
def cell(xs):
    xs = [x for x in xs if x == x]
    if not xs: return '—'
    return f'{statistics.median(xs):.3f} ({min(xs):.3f}〜{max(xs):.3f})'
for n in NAT:
    print(f'## {n}（種の中央〔最小〜最大〕・nation_s の四半期の行）')
    print('| Q | 債務÷年GDP | 平均クーポン(年) | 利払い÷年GDP | 利払い÷歳入 | 歳入÷年GDP | 門の閉じた月/観測月 |')
    print('|---|---|---|---|---|---|---|')
    for q in range(QMAX):
        rows = data[n].get(q, {})
        dg = [f(r['govDebtStock'])/f(r['nominalGdpPerYear']) for r in rows.values()]
        cp = [f(r['interestMeanPerMonth'])*12/f(r['govDebtStock']) if f(r['govDebtStock'])>0 else float('nan') for r in rows.values()]
        ig = [f(r['interestMeanPerMonth'])*12/f(r['nominalGdpPerYear']) for r in rows.values()]
        ir = [f(r['interestMeanPerMonth'])/f(r['revenueBaseMeanPerMonth']) if f(r['revenueBaseMeanPerMonth'])>0 else float('nan') for r in rows.values()]
        rg = [f(r['revenueBaseMeanPerMonth'])*12/f(r['nominalGdpPerYear']) for r in rows.values()]
        gc = [f(r['borrowGateClosedMonths']) for r in rows.values()]; go = [f(r['borrowGateObservedMonths']) for r in rows.values()]
        print(f'| Q{q+1} | {cell(dg)} | {cell(cp)} | {cell(ig)} | {cell(ir)} | {cell(rg)} | {statistics.median(gc) if gc else "—"}/{statistics.median(go) if go else "—"} |')
    print()
