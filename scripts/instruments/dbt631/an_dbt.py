"""(631) 線 dbt 段0：国債の流れを十年ごとに〈発行・返済（床の分と線を超えた分）・不履行・残り（未払い利子の元本繰り入れ・否認・継承など）〉へ分解する。読むだけ。
入力＝計器つき10種の生（fiscal_s・fsx_s・nation_s）。使い方：python an_dbt.py <ins フォルダ> [国名,...]
単位＝年GDP（nation_s の nominalGdpPerYear の十年の平均）に対する比。種ごとに出して種の中央値を出す。"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1<<30)
NAT = ['フランス','オーストリア','日本','イギリス','アメリカ','プロイセン']
d = sys.argv[1]
if len(sys.argv) > 2: NAT = sys.argv[2].split(',')
def rows(f):
    with open(f, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r
def fl(x):
    try: return float(x)
    except: return float('nan')
dec = lambda y: (int(y) // 10) * 10
# nation×seed×decade の集計
res = {}   # (nation, decade) -> list of dicts per seed
files = sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv')), key=lambda s: int(re.search(r'_s(\d+)', s).group(1)))
for ff in files:
    sd = int(re.search(r'_s(\d+)', ff).group(1))
    # GDP 年平均
    gdp = {}; war = {}
    for ix, r in rows(os.path.join(d, f'nation_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT: continue
        g = fl(r[ix['nominalGdpPerYear']])
        if g == g: gdp.setdefault((n, int(r[ix['year']])), []).append(g)
        w_ = fl(r[ix['atWarMonths']])
        if w_ == w_:
            kk = (n, dec(int(r[ix['year']]))); war.setdefault(kk, [0.0, 0.0]); war[kk][0] += w_; war[kk][1] += 3.0
    gdpY = {k: sum(v) / len(v) for k, v in gdp.items()}
    # fsx 四半期
    fs = {}
    for ix, r in rows(os.path.join(d, f'fsx_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT or not r[ix['time']].startswith('q'): continue   # 助走の行 w* は数えない
        y = int(r[ix['year']]); k = (n, dec(y))
        a = fs.setdefault(k, dict(gate=0, floor=0, done=0, above=0, over=0, tre=0, line=0, nq=0))
        a['gate'] += fl(r[ix['repayGateMonths']]); a['floor'] += fl(r[ix['repayFloorSum']]); a['done'] += fl(r[ix['repayDoneSum']]); a['above'] += fl(r[ix['repayAboveFloorSum']])
        a['over'] += fl(r[ix['overLineMonths']])
        t = fl(r[ix['treasuryEnd']]); l = fl(r[ix['lineEnd']])
        if t == t and l == l and l > 0: a['tre'] += t; a['line'] += l; a['nq'] += 1
    # fiscal 月
    mo = {}
    for ix, r in rows(ff):
        n = r[ix['nation']]
        if n not in NAT: continue
        y = int(r[ix['year']]); k = (n, dec(y))
        a = mo.setdefault(k, dict(m=0, borrow=0, repay=0, short=0, gateclosed=0, none=0, pool=0, full=0, ticket=0, d0=None, d1=0, dflt0=None, dflt1=0, intr=0, rev=0, nogov=0, clpb=[]))
        a['m'] += 1
        b = fl(r[ix['ledgerBorrowPerMonth']]); a['borrow'] += b if b == b else 0
        rp = fl(r[ix['ledgerDebtRepayPerMonth']]); a['repay'] += -rp if rp == rp else 0   # 台帳は支出が負
        bd = r[ix['borrowBinder']]
        if bd != 'noShortfall': a['short'] += 1
        if bd == 'gateClosed': a['gateclosed'] += 1
        elif bd == 'none': a['none'] += 1
        elif bd == 'pool': a['pool'] += 1
        elif bd == 'ticket': a['ticket'] += 1
        elif bd == 'full': a['full'] += 1
        gd = fl(r[ix['govDebtStock']])
        if a['d0'] is None: a['d0'] = gd
        a['d1'] = gd
        df = fl(r[ix['defaultCount']])
        if a['dflt0'] is None: a['dflt0'] = df
        a['dflt1'] = df
        i_ = fl(r[ix['ledgerInterestPerMonth']]); a['intr'] += -i_ if i_ == i_ else 0
        rv = fl(r[ix['revenueBasePerMonth']]); a['rev'] += rv if rv == rv else 0
        cl = fl(r[ix['claimTotalPerMonth']]); pb = fl(r[ix['payableBeforeBorrowStock']])
        if cl == cl and pb == pb and pb > 0: a['clpb'].append(cl / pb)
    for k, a in mo.items():
        n, D = k
        g = [gdpY[(n, y)] for y in range(D, D + 10) if (n, y) in gdpY]
        if not g or a['d0'] is None: continue
        G = sum(g) / len(g)
        f = fs.get(k, {})
        # 年GDP比（十年の累計÷十年平均の年GDP＝〈年GDPの何倍が十年に動いたか〉）
        o = dict(d0=a['d0'] / G, d1=a['d1'] / G, issue=a['borrow'] / G, repay=a['repay'] / G,
                 floor=f.get('floor', 0) / G, above=f.get('above', 0) / G, done=f.get('done', 0) / G,
                 resid=(a['d1'] - a['d0'] - a['borrow'] + a['repay']) / G,
                 short=a['short'] / a['m'], gateclosed=a['gateclosed'] / a['m'], none=a['none'] / a['m'], pool=a['pool'] / a['m'], ticket=a['ticket'] / a['m'], full=a['full'] / a['m'],
                 gate=f.get('gate', 0) / a['m'], over=f.get('over', 0) / a['m'], dflt=a['dflt1'] - a['dflt0'],
                 war=(war[k][0] / war[k][1] if k in war and war[k][1] > 0 else float('nan')), clpb=(st.median(a['clpb']) if a['clpb'] else float('nan')), tl=(f['tre'] / f['line']) if f.get('nq') else float('nan'), ir=a['intr'] / a['rev'] if a['rev'] > 0 else float('nan'))
        res.setdefault(k, []).append(o)
cols = ['d0', 'd1', 'issue', 'repay', 'floor', 'above', 'resid', 'short', 'gateclosed', 'none', 'pool', 'gate', 'over', 'tl', 'ir', 'dflt', 'war', 'clpb']
print('国\t十年\t種数\t' + '\t'.join(cols))
for n in NAT:
    for D in range(1780, 1960, 10):
        L = res.get((n, D))
        if not L: continue
        out = []
        for c in cols:
            v = [x[c] for x in L if x[c] == x[c]]
            out.append(f'{st.median(v):.3f}' if v else 'nan')
        print(f'{n}\t{D}\t{len(L)}\t' + '\t'.join(out))
