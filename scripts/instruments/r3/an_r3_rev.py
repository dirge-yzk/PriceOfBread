"""(305) 線 r3：英の Q1〜Q8 の歳入の内訳と利払い・債務・物価（読むだけ）。置き直しの谷が〈どの税〉と〈どの利払い〉で起きるかを数で出す。
  python an_r3_rev.py A=<ins> B=<ins> [Q=8]
  出す物（nation_s の英の行・四半期ごと・種の中央）：歳入 revenueBaseMeanPerMonth・直接税 directTaxNetMeanPerMonth・間接税等 indirectRevenueMeanPerMonth・
    その他＝歳入−直接−間接・利払い interestMeanPerMonth・国債の金利 bondRateMeanPerYear・債務 govDebtStock・物価 priceLevel・名目GDP nominalGdpPerYear。
  限界：nation_s の月平均の列は四半期の平均。『その他』は差で出すので列の定義が重なるときは負になりうる。"""
import sys, glob, re, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:] if '=' in a and not a.startswith('Q='))
QN = 8
for a in sys.argv[1:]:
    if a.startswith('Q='):
        QN = int(a[2:])
COLS = ['revenueBaseMeanPerMonth', 'directTaxNetMeanPerMonth', 'indirectRevenueMeanPerMonth', 'interestMeanPerMonth', 'bondRateMeanPerYear', 'govDebtStock', 'priceLevel', 'nominalGdpPerYear']
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')
D = {}
for a, d in ARMS.items():
    D[a] = {}
    for p in glob.glob(d + '/nation_s*.tsv'):
        sd = int(re.search(r'nation_s(\d+)\.tsv$', p).group(1))
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in fh:
                r = ln.split('\t')
                if r[ix['nation']] == 'イギリス' and int(r[ix['quarter']]) < QN:
                    D[a].setdefault(sd, {})[int(r[ix['quarter']])] = {c: fl(r[ix[c]]) for c in COLS}
seeds = sorted(set.intersection(*[set(D[a]) for a in D]))
print(f'# (305) 英の Q1〜Q{QN} の歳入の内訳（種 {seeds}・種の中央）')
for a in D:
    print(f'\n## {a}\n| 量 | ' + ' | '.join(f'Q{q + 1}' for q in range(QN)) + ' |\n|---|' + '---|' * QN)
    def row(lab, fn, nd=0):
        print(f'| {lab} | ' + ' | '.join(f'{med([fn(D[a][sd][q]) for sd in seeds]):.{nd}f}' for q in range(QN)) + ' |')
    row('歳入（月）', lambda r: r['revenueBaseMeanPerMonth'])
    row('直接税', lambda r: r['directTaxNetMeanPerMonth'])
    row('間接税等', lambda r: r['indirectRevenueMeanPerMonth'])
    row('その他（差）', lambda r: r['revenueBaseMeanPerMonth'] - r['directTaxNetMeanPerMonth'] - r['indirectRevenueMeanPerMonth'])
    row('利払い（月）', lambda r: r['interestMeanPerMonth'])
    row('利払い÷歳入', lambda r: r['interestMeanPerMonth'] / r['revenueBaseMeanPerMonth'] if r['revenueBaseMeanPerMonth'] > 0 else float('nan'), 3)
    row('国債の金利（年）', lambda r: r['bondRateMeanPerYear'], 4)
    row('債務', lambda r: r['govDebtStock'])
    row('物価水準', lambda r: r['priceLevel'], 3)
    row('名目GDP（年）', lambda r: r['nominalGdpPerYear'])
if len(D) == 2:
    a, b = list(D)
    print(f'\n## {b}÷{a}（種ごとの比の中央）\n| 量 | ' + ' | '.join(f'Q{q + 1}' for q in range(QN)) + ' |\n|---|' + '---|' * QN)
    for lab, c in (('歳入', 'revenueBaseMeanPerMonth'), ('直接税', 'directTaxNetMeanPerMonth'), ('間接税等', 'indirectRevenueMeanPerMonth'), ('利払い', 'interestMeanPerMonth'), ('物価水準', 'priceLevel'), ('名目GDP', 'nominalGdpPerYear')):
        print(f'| {lab} | ' + ' | '.join(f'{med([D[b][sd][q][c] / D[a][sd][q][c] if D[a][sd][q][c] else float("nan") for sd in seeds]):.3f}' for q in range(QN)) + ' |')
