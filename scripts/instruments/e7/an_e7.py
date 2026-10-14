"""(332) 線 e7：事前登録 measurements/e7_20261014_prereg.txt の読み手（読むだけ・挙動は変えない）。
  PYTHONUTF8=1 python an_e7.py <出力.md> A=<ins> B=<ins> [B1=<ins> N1=<ins> ...]
入力：INS=1 で回した <出力>/ins の nation_s<種>.tsv（全61国が入る）と stock_s<種>.tsv（あれば・gdpRealPerYear）。
出す物（事前登録の [4][6]）：
 ・H1 英の利払い÷歳入（窓 Q5〜Q40＝q4..39・種ごとに 窓内の interestMeanPerMonth の和÷revenueBaseMeanPerMonth の和 を出して種の中央）
   ★事前登録 追記 2（腕を回す前）で合否から外した＝記録だけ（置いた比の変化で自明に動く量）。判定の代わりに〈A の H1 × （腕の Q1 債務比 ÷ A の Q1 債務比）〉の予測と残差を出す（A が引数にあるとき）
 ・H2 英の Q40 末の債務÷年GDP（種の中央・分母＝nominalGdpPerYear〔名目〕）＝追記 2 で記録だけ（Q1→Q40 の変化も出す）
 ・追記 2 で足した記録：置いた国債の開幕のクーポン・市場金利・利払い÷国債（fiscal_s の最初の3か月）／助走 w1〜q0 の treasuryResetSinceLastRowStock の和と開幕の貯蓄（money_s・年名目GDP で割る）／
   英の利払い÷歳出（近似：fiscal_s の台帳の支出の和・元本の返しを除く・窓 Q5〜Q40）／英の稼ぎ（region_s の earnedPerMonth）に占める GB の 4 州の割合
 ・L1 歳入÷年GDP の順位 英＞仏（種の中央・種ごとに ≧8/10）
 ・記録：英・仏・蘭・葡・普の Q1／Q40 末の債務÷年GDP・英の Q1 の債務÷実質年GDP（stock_s があれば・置いた比との差＝物価水準）・開幕に国債のある18国の不履行のある種（Q1〜Q40／Q1〜Q160）
 ・英の兌換停止・不履行の窓の表は measurements/rev1_20261012_readers/l4w.py（fiscal_s を読む）・払えない月は scripts/instruments/h6/binder_split.py・
   空洞化は hol/an_h2.py -v4・NG は ng.sh を別に回す（この読み手は重ねない）。
限界の自己申告：
 ・nation_s の月平均の列は四半期ごとの平均（窓の和は四半期の平均の和）。nominalGdpPerYear は名目の年GDP（総産出で付加価値ではない）。
 ・国が窓の途中で消える種は残る四半期だけで出す。全く無い種は除く（NaN は判定不能で合格にしない）。種は腕の間で共通の種だけ。"""
import sys, os, re, glob, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
OUT = sys.argv[1]
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[2:])
DEBTORS = ['イギリス', 'フランス', 'ネーデルラント', 'アメリカ', 'スペイン', 'オーストリア', 'ロシア', '教皇領', 'ジェノヴァ', 'デンマーク', 'スウェーデン', 'ポルトガル',
           '両シチリア', 'ヴェネツィア', 'バイエルン', 'ザクセン', 'ヴュルテンベルク', 'オスマン']
FOCUS = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', 'プロイセン']
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]; return statistics.median(xs) if xs else float('nan')
def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'
def load(D):
    R = {}
    for p in glob.glob(os.path.join(D, 'nation_s*.tsv')):
        sd = int(re.search(r'nation_s(\d+)\.tsv$', p).group(1))
        d = collections.defaultdict(dict)
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in fh:
                r = ln.rstrip('\n').split('\t'); q = int(r[ix['quarter']])
                if q > 159: continue
                d[r[ix['nation']]][q] = dict(rev=fl(r[ix['revenueBaseMeanPerMonth']]), int=fl(r[ix['interestMeanPerMonth']]), gdp=fl(r[ix['nominalGdpPerYear']]),
                                             debt=fl(r[ix['govDebtStock']]), dflt=fl(r[ix['sovereignDefaultCumulativeCount']]))
        R[sd] = d
    return R
def real_gdp(D):
    R = {}
    for p in glob.glob(os.path.join(D, 'stock_s*.tsv')):
        sd = int(re.search(r'stock_s(\d+)\.tsv$', p).group(1))
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            if 'gdpRealPerYear' not in ix: return {}
            for ln in fh:
                r = ln.rstrip('\n').split('\t')
                if r[ix['nation']] == 'イギリス' and r[ix['time']] == 'warm': R[sd] = fl(r[ix['govDebtStock']]) / fl(r[ix['gdpRealPerYear']])   # 'warm'＝助走の後の開幕の盤面の行（open＝構築の直後・q0＝最初の四半期の後）
    return R
OUTS = ['Army', 'Navy', 'Admin', 'Education', 'Welfare', 'Pension', 'Modernization', 'AntiCorruption', 'Interest', 'FactoryBuild', 'Subsidy', 'Megaproject', 'ColonialUpkeep', 'TributeOut', 'WarCost', 'OtherOut']
def tsv_rows(p):
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in fh: yield ix, ln.rstrip('\n').split('\t')
def load_fiscal(D):
    # fiscal_s<種>.tsv（POB_INSTRUMENTS_NATIONS の国だけ・月ごと）。q＝monthIndex//3。支出＝台帳の支出（負）の和の符号を反転（元本の返し・借入は含めない）
    R = {}
    for p in glob.glob(os.path.join(D, 'fiscal_s*.tsv')):
        sd = int(re.search(r'fiscal_s(\d+)\.tsv$', p).group(1)); d = collections.defaultdict(dict)
        for ix, r in tsv_rows(p):
            d[r[ix['nation']]][int(r[ix['monthIndex']])] = dict(debt=fl(r[ix['govDebtStock']]), coupon=fl(r[ix['govAvgCouponPerYear']]), rate=fl(r[ix['govBondRatePerYear']]),
                intr=-fl(r[ix['ledgerInterestPerMonth']]), out=-sum(fl(r[ix['ledger' + k + 'PerMonth']]) for k in OUTS))
        R[sd] = d
    return R
def load_money(D):
    # money_s<種>.tsv：time は w0〜w19（助走）・q0〜（本編）。助走の reset の和と開幕の貯蓄の組
    R = {}
    for p in glob.glob(os.path.join(D, 'money_s*.tsv')):
        sd = int(re.search(r'money_s(\d+)\.tsv$', p).group(1)); d = collections.defaultdict(dict)
        for ix, r in tsv_rows(p):
            t = r[ix['time']]
            if t.startswith('w') or t == 'q0':
                d[r[ix['nation']]][t] = dict(reset=fl(r[ix['treasuryResetSinceLastRowStock']]), sav=[fl(r[ix['savingsStockClass%d' % c]]) for c in range(14)])
        R[sd] = d
    return R
def load_region(D):
    # region_s<種>.tsv の q0：英の稼ぎ（earnedPerMonth）に占める ロンドン・マンチェスター・バーミンガム・グラスゴー（GB の 4 州）の割合
    R = {}
    for p in glob.glob(os.path.join(D, 'region_s*.tsv')):
        sd = int(re.search(r'region_s(\d+)\.tsv$', p).group(1)); tot = gb = 0.0
        for ix, r in tsv_rows(p):
            if r[ix['quarter']] != '0': break
            if r[ix['nation']] != 'イギリス': continue
            e = fl(r[ix['earnedPerMonth']]); tot += e
            if r[ix['region']] in ('ロンドン', 'マンチェスター', 'バーミンガム', 'グラスゴー'): gb += e
        if tot > 0: R[sd] = gb / tot
    return R
def win(rows, a, b): return [rows[q] for q in range(a, b + 1) if q in rows]
def rev_ratio(rows, a, b):
    qs = win(rows, a, b); g = sum(x['gdp'] for x in qs)
    return sum(x['rev'] * 12 for x in qs) / g if qs and g > 0 else float('nan')
def int_ratio(rows, a, b):
    qs = win(rows, a, b); R = sum(x['rev'] for x in qs)
    return sum(x['int'] for x in qs) / R if qs and R > 0 else float('nan')
def debt_ratio(rows, q):
    x = rows.get(q); return x['debt'] / x['gdp'] if x and x['gdp'] > 0 else float('nan')
DATA = {a: load(d) for a, d in ARMS.items()}
RG = {a: real_gdp(d) for a, d in ARMS.items()}
FS = {a: load_fiscal(d) for a, d in ARMS.items()}
MS = {a: load_money(d) for a, d in ARMS.items()}
GBS = {a: load_region(d) for a, d in ARMS.items()}
common = sorted(set.intersection(*[set(v) for v in DATA.values()]))
lines = []; P = lines.append
P(f'# (332) 段6 の採り直し 読み手の出力（種 {len(common)}：{common}）\n')
def uk_h1(S): return [int_ratio(S[sd]['イギリス'], 4, 39) for sd in common]
def uk_q1(S): return [debt_ratio(S[sd]['イギリス'], 0) for sd in common]
A_H1 = med(uk_h1(DATA['A'])) if 'A' in DATA else float('nan'); A_Q1 = med(uk_q1(DATA['A'])) if 'A' in DATA else float('nan')
def ratio_expend(F, sd):
    # 英の利払い÷歳出（近似・窓 Q5〜Q40＝月 12..119）
    m = F.get(sd, {}).get('イギリス', {}); ms = [x for k, x in m.items() if 12 <= k <= 119]
    o = sum(x['out'] for x in ms); return sum(x['intr'] for x in ms) / o if ms and o > 0 else float('nan')
for a, S in DATA.items():
    P(f'## 腕 {a}（{ARMS[a]}）'+chr(10))
    h1 = uk_h1(S)
    h2 = [debt_ratio(S[sd]['イギリス'], 39) for sd in common]
    uk = [rev_ratio(S[sd]['イギリス'], 4, 39) for sd in common]; fr = [rev_ratio(S[sd]['フランス'], 4, 39) for sd in common]
    n1 = sum(1 for x, y in zip(uk, fr) if x == x and y == y and x > y)
    q1 = uk_q1(S); pred = A_H1 * med(q1) / A_Q1 if A_Q1 == A_Q1 and A_Q1 > 0 else float('nan')
    P(f'- H1（記録だけ・追記 2）英の利払い÷歳入（Q5-40）{cell(h1)}／A の H1 × （この腕の Q1 債務比 {med(q1):.3f} ÷ A の {A_Q1:.3f}）の予測 {pred:.3f}・残差（実−予測）{med(h1) - pred:+.3f}（A の H1 {A_H1:.3f}）')
    P(f'- H1b（記録だけ・近似）英の利払い÷歳出（Q5-40・fiscal_s の台帳の支出の和）{cell([ratio_expend(FS[a], sd) for sd in common])}（史実の目安 OBrien 1988 平時 0.56）')
    P(f'- H2（記録だけ・追記 2）英の Q40 末の債務÷年名目GDP {cell(h2)}・Q1 {cell(q1)}・Q1→Q40 の変化 {cell([y - x for x, y in zip(q1, h2)])}')
    P(f'- L1 英の歳入÷年GDP（Q5-40）{cell(uk)} ＞ 仏 {cell(fr)} → {"合格" if med(uk) > med(fr) and n1 >= 8 else "外れ"}（種ごとに英＞仏 {n1}/{len(common)}）')
    gb = GBS[a]
    if gb: P(f'- 英の稼ぎ（q0）に占める GB の 4 州の割合 {cell(list(gb.values()))}（残りはダブリンと植民地・稼ぎは GDP でなく桁の目安）')
    P('| 国 | 債務÷年GDP Q1 | Q40 末 | 利払い÷歳入 Q5-40 | 歳入÷年GDP Q5-40 |'); P('|---|---|---|---|---|')
    for n in FOCUS:
        P(f'| {n} | {cell([debt_ratio(S[sd].get(n, {}), 0) for sd in common])} | {cell([debt_ratio(S[sd].get(n, {}), 39) for sd in common])} | '
          f'{cell([int_ratio(S[sd].get(n, {}), 4, 39) for sd in common])} | {cell([rev_ratio(S[sd].get(n, {}), 4, 39) for sd in common])} |')
    rg = RG[a]
    if rg:
        P(f'- 英の開幕（助走の後・stock_s の warm 行）の債務÷実質年GDP {cell(list(rg.values()))}'
          f'（名目の分母 nation_s の Q1 では {cell([debt_ratio(S[sd]["イギリス"], 0) for sd in common])}・差＝物価水準と最初の四半期の動き）')
    F = FS[a]; M = MS[a]
    if F:
        P('- 置いた国債の開幕（fiscal_s の最初の3か月の中央〔月 2〜4〕）：クーポン／市場金利／利払い×12÷国債（国ごと・種の中央）')
        for n in FOCUS:
            f3 = lambda key: med([x[key] for sd in common for k, x in F.get(sd, {}).get(n, {}).items() if k <= 4])
            ir = med([x['intr'] * 12 / x['debt'] for sd in common for k, x in F.get(sd, {}).get(n, {}).items() if k <= 4 and x['debt'] > 0])
            if F.get(common[0], {}).get(n): P(f'  - {n}：クーポン {f3("coupon"):.4f}／市場金利 {f3("rate"):.4f}／利払い×12÷国債 {ir:.4f}')
    if M:
        P('- 助走の国庫の戻しと開幕の貯蓄（money_s・助走 w1〜w19 と q0 の treasuryResetSinceLastRowStock の和＝基礎貨幣への出入り／貯蓄＝14 階級の和・年名目GDP〔nation_s の Q1〕で割る）')
        for n in FOCUS:
            if not M.get(common[0], {}).get(n): continue
            rs = [sum(v['reset'] for t, v in M[sd][n].items() if t != 'w0') for sd in common]
            g = [S[sd][n][0]['gdp'] for sd in common]
            sv0 = [sum(M[sd][n]['w0']['sav']) / g_ for sd, g_ in zip(common, g)]; sv1 = [sum(M[sd][n]['q0']['sav']) / g_ for sd, g_ in zip(common, g)]
            P(f'  - {n}：戻しの和÷年名目GDP {cell([r / g_ for r, g_ in zip(rs, g)], 4)}／貯蓄÷年名目GDP w0 {cell(sv0)} → q0 {cell(sv1)}')
        n = 'イギリス'
        if M.get(common[0], {}).get(n):
            P('  - 英の階級ごとの貯蓄÷年名目GDP（q0・種の中央・階級 0〜13）：' + '、'.join(f'{c}:{med([M[sd][n]["q0"]["sav"][c] / S[sd][n][0]["gdp"] for sd in common]):.3f}' for c in range(14)))
    P('- 不履行（sovereignDefaultCumulativeCount>0）のある種の数（開幕に国債のある18国・Q1〜Q40〔q=39〕／Q1〜Q160〔q=159〕）：' + '、'.join(
        f'{n} {sum(1 for sd in common if (S[sd].get(n, {}).get(39, {}).get("dflt", 0) or 0) > 0)}/{sum(1 for sd in common if (S[sd].get(n, {}).get(159, {}).get("dflt", 0) or 0) > 0)}' for n in DEBTORS))
    P('')
open(OUT, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))
print('\n'.join(lines))
