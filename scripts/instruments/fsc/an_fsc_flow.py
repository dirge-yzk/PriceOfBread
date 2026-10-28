"""(639) 線 fsc 段0：国庫が潤沢の線を超えて貯まる流れを十年ごとに分解する。読むだけ。
入力＝計器つき10種の生（fiscal_s・fsx_s・nation_s）。使い方：python an_fsc_flow.py <ins フォルダ> [国名,...]
単位＝年GDP（nation_s の nominalGdpPerYear の十年の平均）に対する年あたりの流れ（月の流れ×12÷年GDP）。種ごとに出して種の中央値。
列：tl＝国庫÷潤沢の線（四半期末の中央値）・T＝国庫÷年GDP（十年の平均）・dT＝国庫の増減（年あたり）
    収入：tax・tariff・mono（専売）・sfac（国営工場）・trib（貢納）・oth・borrow／
    支出：mil（陸＋海）・adm・edu・wel（福祉＋年金）・mod（近代化）・int（利払い）・repay（減債）・fbld（工場建設）・sub（補助）・mega・col・trout・war・othout
    pr＝支払い率（月の平均）・adminCap＝行政の請求の上限÷必要額（上限が必要額に届かない割合のしるし）"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
NAT = ['フランス', 'オーストリア', '日本', 'イギリス', 'アメリカ', 'プロイセン']
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
INC = {'tax': 'ledgerTaxPerMonth', 'tariff': 'ledgerTariffPerMonth', 'mono': 'ledgerStateMonopolyPerMonth', 'sfac': 'ledgerStateFactoryPerMonth',
       'trib': 'ledgerTributeInPerMonth', 'oth': 'ledgerOtherInPerMonth', 'borrow': 'ledgerBorrowPerMonth'}
OUT = {'army': 'ledgerArmyPerMonth', 'navy': 'ledgerNavyPerMonth', 'adm': 'ledgerAdminPerMonth', 'edu': 'ledgerEducationPerMonth', 'wel': 'ledgerWelfarePerMonth',
       'pen': 'ledgerPensionPerMonth', 'mod': 'ledgerModernizationPerMonth', 'int': 'ledgerInterestPerMonth', 'repay': 'ledgerDebtRepayPerMonth',
       'fbld': 'ledgerFactoryBuildPerMonth', 'sub': 'ledgerSubsidyPerMonth', 'mega': 'ledgerMegaprojectPerMonth', 'col': 'ledgerColonialUpkeepPerMonth',
       'trout': 'ledgerTributeOutPerMonth', 'war': 'ledgerWarCostPerMonth', 'othout': 'ledgerOtherOutPerMonth', 'anti': 'ledgerAntiCorruptionPerMonth'}
res = {}
files = sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv')), key=lambda s: int(re.search(r'_s(\d+)', s).group(1)))
for ff in files:
    sd = int(re.search(r'_s(\d+)', ff).group(1))
    gdp = {}
    for ix, r in rows(os.path.join(d, f'nation_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT: continue
        g = fl(r[ix['nominalGdpPerYear']])
        if g == g: gdp.setdefault((n, dec(int(r[ix['year']]))), []).append(g)
    gdpD = {k: sum(v) / len(v) for k, v in gdp.items()}
    tl = {}
    for ix, r in rows(os.path.join(d, f'fsx_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT or not r[ix['time']].startswith('q'): continue
        t, l = fl(r[ix['treasuryEnd']]), fl(r[ix['lineEnd']])
        if l == l and l > 0 and t == t: tl.setdefault((n, dec(int(r[ix['year']]))), []).append(t / l)
    acc = {}
    for ix, r in rows(ff):
        n = r[ix['nation']]
        if n not in NAT: continue
        k = (n, dec(int(r[ix['year']])))
        a = acc.setdefault(k, {'m': 0, 'trs': 0.0, 'pr': 0.0, 'adm_need': 0.0, 'adm_cap': 0.0})
        a['m'] += 1
        for nm, c in list(INC.items()) + list(OUT.items()) + [('delta', 'treasuryDeltaPerMonth')]:
            v = fl(r[ix[c]]); a[nm] = a.get(nm, 0.0) + (v if v == v else 0.0)
        v = fl(r[ix['treasuryEndStock']]); a['trs'] += v if v == v else 0.0
        v = fl(r[ix['payRateShare']]); a['pr'] += v if v == v else 0.0
        v = fl(r[ix['adminNeedAtBudgetPerMonth']]); a['adm_need'] += v if v == v else 0.0
        v = fl(r[ix['adminClaimCappedAtBudgetPerMonth']]); a['adm_cap'] += v if v == v else 0.0
    for k, a in acc.items():
        g = gdpD.get(k)
        if not g or not (g > 0): continue
        m = a['m']
        o = {nm: a[nm] / m * 12 / g for nm in list(INC) + list(OUT) + ['delta']}
        o['T'] = a['trs'] / m / g; o['pr'] = a['pr'] / m
        o['tl'] = st.median(tl[k]) if k in tl else float('nan')
        o['adminCap'] = a['adm_cap'] / a['adm_need'] if a['adm_need'] > 0 else float('nan')
        res.setdefault(k, []).append(o)
cols = ['tl', 'T', 'delta', 'tax', 'tariff', 'mono', 'sfac', 'trib', 'oth', 'borrow', 'army', 'navy', 'adm', 'edu', 'wel', 'pen', 'mod', 'int', 'repay', 'fbld', 'sub', 'mega', 'col', 'trout', 'war', 'othout', 'anti', 'pr', 'adminCap']
print('国\t十年\t種数\t' + '\t'.join(cols))
for n in NAT:
    for dd in range(1780, 1960, 10):
        L = res.get((n, dd))
        if not L: continue
        vals = []
        for c in cols:
            xs = [o[c] for o in L if o[c] == o[c]]
            vals.append('%.3f' % st.median(xs) if xs else 'nan')
        print(f'{n}\t{dd}\t{len(L)}\t' + '\t'.join(vals))
