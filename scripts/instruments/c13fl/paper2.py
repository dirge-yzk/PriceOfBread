# (810) c13fl 紙の台本 2（率の候補を同じ盤面で並べる・読むだけ）
# 使い方：python paper2.py <ins フォルダ> <種の一覧 例 1-10 または 14,15,16,17> [国の名 種 年0 年1]
# 盤面の量（divhost_s の rentMonths==3 の行＝四半期の最後の呼び出し）：
#   rent_q＝3×rentMonth3（今の名目の流れ）・fci（外資の負債＝預けた時点の名目の積み上げ）・sav（投資家の貯蓄＝今の名目）
#   K_ind≈meanIndustrialization（score_s・人口加重・4 四半期ごと＝同じ年の最後の値を使う）×IndCapitalOutputYears(1.0)×12×earnedTotalPerMonth（nation_s）
#   ⚠近似：エンジンの K は州ごとの Ind×州の稼ぎ×束の実質価格（cost per point の逆）の和。ここは国の人口加重の平均 Ind×国の稼ぎ＝州の分布の差と束の実質価格を無視する。
# 率の候補（四半期・元本に対する率）：
#   yB＝rent_q÷(fci＋sav)（(801) 形 B の率＝持ち分の式を元本で割った物）
#   yK＝rent_q÷K_ind（今の名目のレント÷今の名目の生産資本）
#   yH＝rent_q÷(fci＋sav＋K_ind)
#   旧＝0.02（CapitalDividendRatePerQuarter）
import sys, csv, os, collections
d = sys.argv[1]
spec = sys.argv[2]
seeds = list(range(int(spec.split('-')[0]), int(spec.split('-')[1]) + 1)) if '-' in spec else [int(x) for x in spec.split(',')]
focus = sys.argv[3:7] if len(sys.argv) >= 7 else None
OLD = 0.02
def pct(a, ps=(0.1, 0.5, 0.9, 0.99)):
    a = sorted(a); m = len(a)
    if not a: return 'n 0'
    return ' '.join(f'p{int(p*100)} {a[min(m-1, int(p*m))]:.4g}' for p in ps) + f' max {a[-1]:.4g} n {m}'
Y = {'yB': [], 'yK': [], 'yH': []}
byp = collections.defaultdict(lambda: {'yB': [], 'yK': [], 'yH': []})
kzero = 0; rows = 0; nojoin = 0
over = collections.Counter()
focus_rows = []
for s in seeds:
    ind = {}
    with open(os.path.join(d, f'score_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: ind[(int(row[ix['year']]), row[ix['nation']])] = float(row[ix['meanIndustrialization']])
    earn = {}
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: earn[(row[ix['quarter']], row[ix['nation']])] = float(row[ix['earnedTotalPerMonth']])
    last = {}
    with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: last[(row[ix['quarter']], row[ix['nation']])] = row
    for (q, nat), row in last.items():
        if int(float(row[ix['rentMonths']])) != 3: continue
        rent = 3 * float(row[ix['rentMonth3']]); fci = float(row[ix['fci']]); sav = float(row[ix['invSav']]); yr = int(row[ix['year']])
        if not (fci > 0) or not (rent > 0): continue
        rows += 1
        mi = ind.get((yr, nat)); ea = earn.get((q, nat))
        if mi is None or ea is None: nojoin += 1; continue
        K = mi * 1.0 * 12 * ea
        yb = rent / (fci + sav); yh = rent / (fci + sav + K)
        Y['yB'].append(yb); Y['yH'].append(yh)
        per = yr // 50 * 50
        byp[per]['yB'].append(yb); byp[per]['yH'].append(yh)
        if K > 0:
            yk = rent / K; Y['yK'].append(yk); byp[per]['yK'].append(yk)
            if yk > OLD: over['yK'] += 1
        else: kzero += 1
        if yb > OLD: over['yB'] += 1
        if yh > OLD: over['yH'] += 1
        if focus and nat == focus[0] and s == int(focus[1]) and int(focus[2]) <= yr <= int(focus[3]):
            focus_rows.append((int(q), yr, fci, sav, rent, K, yb, yh, rent / K if K > 0 else float('inf'), float(row[ix['paidIndex']]), float(row[ix['divReqIndex']])))
print(f'盤面 {d} 種 {seeds}：fci>0 かつ rent>0 の行 {rows}・結合できない行 {nojoin}（score_s が 4 四半期ごと＝同じ年の値・nation_s は四半期ごと）・K_ind=0 の行 {kzero}')
for k in ('yB', 'yK', 'yH'):
    n = len(Y[k])
    print(f'  {k}: {pct(Y[k])}・旧 0.02 を超える行の割合 {over[k] / n if n else 0:.4f}')
for per in sorted(byp):
    print(f'  {per}〜: ' + '／'.join(f'{k} {pct(byp[per][k], (0.5, 0.9, 0.99))}' for k in ('yB', 'yK', 'yH')))
if focus:
    print(f'■{focus[0]} 種 {focus[1]} {focus[2]}〜{focus[3]}（四半期・年・fci・sav・rent_q・K_ind・yB・yH・yK・paid・req）')
    for t in sorted(focus_rows): print('  ' + '\t'.join(f'{v:.4g}' if isinstance(v, float) else str(v) for v in t))
