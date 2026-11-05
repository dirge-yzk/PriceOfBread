#!/usr/bin/env python3
# (874) c73tc 突然変異：InstrumentsCearn.cs を 1 か所変えて試験を回し、元に戻す。
#   python -X utf8 -I scripts/instruments/c73tc/mut874.py <M1|M2|M3> <出力>
# 使い捨ての変更はこのファイルだけ（終わりに元のバイトへ戻す＝git diff で確かめる）。
import subprocess, sys, os
M, out = sys.argv[1], sys.argv[2]
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
P = os.path.join(REPO, 'PriceOfBread.SimCore', 'InstrumentsCearn.cs')
MUT = {
    # M1 補助の絞りを外す（輸出の関税の戻しも絞った在庫に足す）
    'M1': ("if (_cearnCat == TreasuryCat.Subsidy) CearnImportSubsidySubInto(a, delta);", "CearnImportSubsidySubInto(a, delta);"),
    # M2 帯を払いの時点の人数でなく、人数 1.0（四半期の末に人が居る状態）で振る＝四半期の末の人数で振る形の近似
    'M2': ("CearnImportPaid(a, -delta, p.Size);", "CearnImportPaid(a, -delta, 1.0);"),
    # M3 旧の規則の予算の枝（商人の人数 ≦1e-12 と商人の添字なし）を新の規則の予算に置き換える
    'M3': ("if (m is not null) ob = m.Debt.Value > 0 ? System.Math.Max(0, m.Savings.Value) : double.PositiveInfinity;\n        else foreach (var p in r.Pops) ob += System.Math.Max(0, p.Savings.Value);",
           "ob = b;"),
}
old, new = MUT[M]
orig = open(P, 'rb').read()
s = orig.decode('utf-8')
assert s.count(old) == 1, M
open(P, 'wb').write(s.replace(old, new).encode('utf-8'))
try:
    env = dict(os.environ); env['PATH'] = '/usr/bin:/c/Program Files/dotnet:' + env.get('PATH', '')
    r = subprocess.run(['dotnet', 'test', os.path.join(REPO, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c73tc_art',
                        '--filter', 'FullyQualifiedName~CearnTradeColumnsTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    keep = [l for l in r.stdout.splitlines() if ('失敗' in l or '成功' in l or 'Assert' in l or 'error' in l.lower())]
    open(out, 'w', encoding='utf-8', newline='
').write(M + ' ' + old.split('\n')[0] + '\n  -> ' + new.split('\n')[0] + '\n' + '\n'.join(keep) + '\n')
finally:
    open(P, 'wb').write(orig)
