# (1296) c134f1 突然変異（㉚）：使い捨ての木で 1 本ずつ入れ、UrbanFlowRealTests と UrxInstrumentTests を回して赤の試験の名前を書く。
#   使い方: python -X utf8 -I mut_f1.py <使い捨ての木> <ビルドの置き場（D:/…）> <出力フォルダ>
#   木は点火（p06:116 を 1）と T1・T2 を入れた形を前提にする（M0 は点火を外す）。1 本ごとに元のバイトへ戻す。
import sys, os, subprocess
T, ART, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
P06 = 'PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs'
PD = 'PriceOfBread.SimCore/PopDecision.cs'
UX = 'PriceOfBread.SimCore/Instruments/InstrumentsUrx.cs'
FT = 'PriceOfBread.Tests/FamineTests.cs'
MUT = {
    'B_flag1_none': [],
    'M0_flag0': [(P06, b'public double UrbanFlowReal = 1;', b'public double UrbanFlowReal = 0;')],
    'M1_denom_vu': [(PD, b'double g = sum > Eps ? 2.0 * (vu - vr) / sum : 0.0;', b'double g = sum > Eps ? 2.0 * (vu - vr) / vu : 0.0;')],
    'M2_sender_swap': [(PD, b'flow = g > 0 ? coef.Mobility * g * rural.Size : coef.Mobility * g * urban.Size;', b'flow = g > 0 ? coef.Mobility * g * urban.Size : coef.Mobility * g * rural.Size;')],
    'M3_window0': [(UX, b'internal const int UrxRegainWindowMonths = 12;', b'internal const int UrxRegainWindowMonths = 0;')],
    'M4_samesign': [(UX, b'if ((drive > 0 && d.Gsym < 0) || (drive < 0 && d.Gsym > 0))', b'if ((drive > 0 && d.Gsym > 0) || (drive < 0 && d.Gsym < 0))')],
    'M5_famine_fix_off': [(FT, b'son.Coef.Mobility = 0;', b'son.Coef.Mobility += 0;'), (FT, b'Assert.Equal(0.0, son.Coef.Mobility);', b'Assert.True(son.Coef.Mobility >= 0);'),
                          (FT, b'soff.Coef.Mobility = 0;', b'soff.Coef.Mobility += 0;'), (FT, b'sa.Coef.Mobility = 0; var sb = Famine(1); sb.Coef.Mobility = 0;', b'sa.Coef.Mobility += 0; var sb = Famine(1); sb.Coef.Mobility += 0;')],
}
FILTER = 'FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~UrxInstrumentTests|FullyQualifiedName~FamineTests|FullyQualifiedName~FamineGateReturnsRuralTests|FullyQualifiedName~DormantIdentityTests'
summary = []
for name, edits in MUT.items():
    saved = {}
    for f, a, b in edits:
        p = os.path.join(T, f); src = saved.get(p) or open(p, 'rb').read(); saved[p] = src
        assert src.count(a) == 1, (name, f, a)
    for p, src in saved.items():
        cur = src
        for f, a, b in edits:
            if os.path.join(T, f) == p: cur = cur.replace(a, b)
        open(p, 'wb').write(cur)
    try:
        log = os.path.join(OUT, f'{name}.log')
        with open(log, 'wb') as lf:
            rc = subprocess.call(['dotnet', 'test', os.path.join(T, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', ART, '--filter', FILTER], stdout=lf, stderr=subprocess.STDOUT)
        txt = open(log, encoding='utf-8', errors='replace').read()
        fails = sorted(set(l.strip().split(' [')[0].replace('失敗 ', '') for l in txt.splitlines() if l.strip().startswith('失敗 PriceOfBread')))
        tot = [l.strip() for l in txt.splitlines() if '合計:' in l]
        summary.append(f'{name}\trc={rc}\t赤 {len(fails)}\t{tot[-1] if tot else "合計なし"}')
        for x in fails: summary.append(f'  {x}')
    finally:
        for p, src in saved.items(): open(p, 'wb').write(src)
with open(os.path.join(OUT, 'mut_summary.txt'), 'wb') as f: f.write(('\n'.join(summary) + '\n').encode('utf-8'))
print('\n'.join(summary))
