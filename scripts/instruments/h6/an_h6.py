"""(294) h6′ 読み手＝読むだけ・挙動は変えない。事前登録 measurements/h6_20261013_prereg.txt の [3][4][5]。
python an_h6.py 腕名=<ins> [腕名=<ins>…]   （nation_s・fiscal_s の種の中央〔最小〜最大〕）
出す物（英・仏・蘭・葡）：
 ・利払い÷歳入（窓 Q1・Q2〜Q8・Q3〜Q12・Q5〜Q40・Q1〜Q40＝窓内の利払いの和÷歳入の和を種ごとに出して中央）
 ・歳入÷年GDP・債務÷年GDP（Q8 末・Q40 末）
 ・門の開いている月の割合（fiscal_s の borrowGateOpenFlag・Q2〜Q8 と Q1〜Q40・種の中央）
 ・⑤ の判定：英の利払い÷歳入（Q2〜Q8）の中央が [0.62, 0.72] か
限界の自己申告：nation_s の月平均の列（interestMeanPerMonth・revenueBaseMeanPerMonth）は四半期ごとの平均＝窓の和は四半期の平均の和（月数は各四半期 3 で同じ）。門の割合は NATIONS に挙げた国だけ（fiscal_s がある国）。"""
import sys, os, glob, re, statistics, collections
NAT = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]; return statistics.median(xs) if xs else float('nan')
def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'
WIN = [('Q1', 0, 0), ('Q2〜Q8', 1, 7), ('Q3〜Q12', 2, 11), ('Q5〜Q40', 4, 39), ('Q1〜Q40', 0, 39)]
def load_nation(D):
    out = collections.defaultdict(lambda: collections.defaultdict(dict))  # nat -> seed -> q -> row
    for p in glob.glob(os.path.join(D, 'nation_s*.tsv')):
        sd = int(re.search(r'nation_s(\d+)', p).group(1))
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in fh:
                r = ln.rstrip('\n').split('\t')
                n = r[ix['nation']]
                if n in NAT:
                    q = int(r[ix['quarter']])
                    if q < 40: out[n][sd][q] = {c: f(r[ix[c]]) for c in ('interestMeanPerMonth', 'revenueBaseMeanPerMonth', 'nominalGdpPerYear', 'govDebtStock')}
    return out
def load_gate(D):
    out = collections.defaultdict(lambda: collections.defaultdict(dict))  # nat -> seed -> month -> flag
    for p in glob.glob(os.path.join(D, 'fiscal_s*.tsv')):
        sd = int(re.search(r'fiscal_s(\d+)', p).group(1))
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in fh:
                r = ln.rstrip('\n').split('\t')
                n = r[ix['nation']]
                if n in NAT:
                    m = int(r[ix['monthIndex']])
                    if m < 120: out[n][sd][m] = f(r[ix['borrowGateOpenFlag']])
    return out
def quarter_of_month(m): return 0 if m < 2 else 1 + (m - 2) // 3
for arm, D in ARMS.items():
    N = load_nation(D); G = load_gate(D)
    print(f'## 腕 {arm}（{D}）')
    for n in NAT:
        print(f'### {n}')
        print('| 窓 | 利払い÷歳入 | 歳入÷年GDP |'); print('|---|---|---|')
        for name, a, b in WIN:
            ir = []; rg = []
            for sd, rows in N[n].items():
                qs = [rows[q] for q in range(a, b + 1) if q in rows]
                if not qs: continue
                R = sum(r['revenueBaseMeanPerMonth'] for r in qs)
                ir.append(sum(r['interestMeanPerMonth'] for r in qs) / R if R > 0 else float('nan'))
                rg.append(sum(r['revenueBaseMeanPerMonth'] * 12 for r in qs) / sum(r['nominalGdpPerYear'] for r in qs))
            print(f'| {name} | {cell(ir)} | {cell(rg)} |')
        d8 = [rows[7]['govDebtStock'] / rows[7]['nominalGdpPerYear'] for rows in N[n].values() if 7 in rows]
        d40 = [rows[39]['govDebtStock'] / rows[39]['nominalGdpPerYear'] for rows in N[n].values() if 39 in rows]
        print(f'債務÷年GDP Q8 末 {cell(d8)}・Q40 末 {cell(d40)}')
        for name, a, b in (('Q2〜Q8', 1, 7), ('Q1〜Q40', 0, 39)):
            sh = []
            for sd, mm in G[n].items():
                v = [x for m, x in mm.items() if a <= quarter_of_month(m) <= b and x == x]
                if v: sh.append(sum(v) / len(v))
            print(f'門の開いている月の割合 {name} {cell(sh)}')
    # ⑤
    ir = []
    for sd, rows in N['イギリス'].items():
        qs = [rows[q] for q in range(1, 8) if q in rows]
        R = sum(r['revenueBaseMeanPerMonth'] for r in qs); ir.append(sum(r['interestMeanPerMonth'] for r in qs) / R)
    m_ = med(ir); print(f'⑤ 英の利払い÷歳入（Q2〜Q8）の中央 {m_:.3f} → {"内側" if 0.62 <= m_ <= 0.72 else "外側"}（[0.62, 0.72]）')
    print()
