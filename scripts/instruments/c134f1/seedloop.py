# (1296) ㊴ 種の選び直しの試し（前提が成り立つ種へ・規則の検査は変えない）。SEEDF・SEEDM を種で置き換え、旗 1 と旗 0 で 2 本を回す。終わったら SEEDF・SEEDM の形へ戻す。
import sys, os, subprocess
T = sys.argv[1]; ART = sys.argv[2]; OUT = sys.argv[3]; seeds = [int(x) for x in sys.argv[4].split(',')]
F = os.path.join(T, 'PriceOfBread.Tests/FiscalTests.cs'); M = os.path.join(T, 'PriceOfBread.Tests/MilitaryTests.cs'); P6 = os.path.join(T, 'PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs')
f0 = open(F, 'rb').read(); m0 = open(M, 'rb').read(); p0 = open(P6, 'rb').read()
FILTER = 'FullyQualifiedName~FiscalTests.対外借入の債権|FullyQualifiedName~MilitaryTests.交戦中も装備の上限|FullyQualifiedName~RevoltIgnitionTests.武器購入'
res = []
try:
    for flag in (1, 0):
        p = p0 if flag == 1 else p0.replace(b'public double UrbanFlowReal = 1;', b'public double UrbanFlowReal = 0;')
        open(P6, 'wb').write(p)
        for s in seeds:
            open(F, 'wb').write(f0.replace(b'SEEDF', f'{s}UL'.encode())); open(M, 'wb').write(m0.replace(b'SEEDM', f'{s}UL'.encode()))
            log = os.path.join(OUT, f'seed_{flag}_{s}.log')
            with open(log, 'wb') as lf:
                subprocess.call(['dotnet', 'test', os.path.join(T, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', ART, '--filter', FILTER], stdout=lf, stderr=subprocess.STDOUT)
            txt = open(log, encoding='utf-8', errors='replace').read()
            fails = [l.strip().split(' [')[0].replace('失敗 PriceOfBread.Tests.', '') for l in txt.splitlines() if l.strip().startswith('失敗 PriceOfBread')]
            res.append(f'flag={flag}\tseed={s}\t赤={len(fails)}\t' + '・'.join(x[:30] for x in fails))
            print(res[-1], flush=True)
finally:
    open(F, 'wb').write(f0); open(M, 'wb').write(m0); open(P6, 'wb').write(p0)
open(os.path.join(OUT, 'seedloop.txt'), 'wb').write(('\n'.join(res) + '\n').encode('utf-8'))
