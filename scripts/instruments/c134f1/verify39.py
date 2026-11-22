# (1296) ㊴ の直しの確かめ：旗 1 と旗 0 で緑・直しを外す突然変異で赤。使い方: python verify39.py <木> <ビルドの置き場> <出力>
import sys, os, subprocess
T, ART, OUT = sys.argv[1], sys.argv[2], sys.argv[3]; os.makedirs(OUT, exist_ok=True)
P6 = 'PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs'; FT = 'PriceOfBread.Tests/FiscalTests.cs'; RT = 'PriceOfBread.Tests/RevoltIgnitionTests.cs'; MT = 'PriceOfBread.Tests/MilitaryTests.cs'
TR = 'FullyQualifiedName~FiscalTests.対外借入の債権|FullyQualifiedName~MilitaryTests.交戦中も装備の上限|FullyQualifiedName~RevoltIgnitionTests.武器購入|FullyQualifiedName~FamineTests'
ALL = TR + '|FullyQualifiedName~CalendarTests|FullyQualifiedName~UrxInstrumentTests|FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~FamineGateReturnsRuralTests'
RUNS = [
    ('flag1_all', [], ALL),
    ('flag0_traj', [(P6, b'public double UrbanFlowReal = 1;', b'public double UrbanFlowReal = 0;')], TR),
    ('mut_fiscal_repay_on', [(FT, b'bw.Coef.BondRepayFracPerMonth = new RatePerMonth(0);', b'bw.Coef.BondRepayFracPerMonth = bw.Coef.BondRepayFracPerMonth;'),
                             (FT, b'Assert.Equal(0.0, bw.Coef.BondRepayFracPerMonth.Value);', b'')], TR),
    ('mut_fiscal_noconvert_engine', [('PriceOfBread.SimCore/MultiNationSim.cs', b'paid += k; lent += k * lw.FxRate / bw.FxRate;', b'paid += k; lent += k;')], 'FullyQualifiedName~FiscalTests.対外借入の債権'),
    ('mut_revolt_mob_off', [(RT, b'pre: c => c.Mobility = 0);', b'pre: c => c.Mobility += 0);'), (RT, b'Assert.Equal(0.0, sim2.World.Coef.Mobility);', b'')], TR),
    ('mut_military_seed1789', [(MT, b'seed: 1793);   // (1296)', b'seed: 1789);   // (1296)')], TR),
]
res = []
for name, edits, flt in RUNS:
    saved = {}
    for f, a, b in edits:
        p = os.path.join(T, f); src = saved.get(p) or open(p, 'rb').read(); saved[p] = src; assert src.count(a) == 1, (name, a)
    for p, src in saved.items():
        cur = src
        for f, a, b in edits:
            if os.path.join(T, f) == p: cur = cur.replace(a, b)
        open(p, 'wb').write(cur)
    try:
        log = os.path.join(OUT, f'{name}.log')
        with open(log, 'wb') as lf:
            subprocess.call(['dotnet', 'test', os.path.join(T, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', ART, '--filter', flt], stdout=lf, stderr=subprocess.STDOUT)
        txt = open(log, encoding='utf-8', errors='replace').read()
        fails = [l.strip().split(' [')[0].replace('失敗 PriceOfBread.Tests.', '') for l in txt.splitlines() if l.strip().startswith('失敗 PriceOfBread')]
        tot = [l.strip() for l in txt.splitlines() if '合計:' in l]
        res.append(f'{name}\t赤 {len(fails)}\t{tot[-1] if tot else "合計なし"}')
        res += [f'  {x}' for x in fails]
        print('\n'.join(res[-1 - len(fails):]), flush=True)
    finally:
        for p, src in saved.items(): open(p, 'wb').write(src)
open(os.path.join(OUT, 'verify39.txt'), 'wb').write(('\n'.join(res) + '\n').encode('utf-8'))
