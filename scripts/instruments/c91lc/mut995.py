# (995) c91lc 突然変異：InstrumentsLandsale.cs の払いの予言を 4 通りに壊し、LandsaleInstrumentTests を回す（使い捨て・元のバイトへ戻す）。出力は標準出力。
import subprocess, sys
P = 'PriceOfBread.SimCore/InstrumentsLandsale.cs'
orig = open(P, 'rb').read()
S = orig.decode('utf-8')
muts = {
 'M1_塵の述語を外す': ('if (sj > 1e-9 && amt > floor) copy += amt;', 'if (amt > floor) copy += amt;'),
 'M2_床を1e-12に固定': ('double floor = c.TakeFloorPositive > 0 ? 0 : 1e-12;', 'double floor = 1e-12;'),
 'M3_ΣSから塵を外す': ('double sj = System.Math.Max(0, q.Savings.Value); sumS += sj;', 'double sj = System.Math.Max(0, q.Savings.Value); if (sj > 1e-9) sumS += sj;'),
 'M4_minを外す': ('double amt = System.Math.Min(phi * (sj / sumS) * price, sj);', 'double amt = phi * (sj / sumS) * price;'),
}
try:
    for k, (a, b) in muts.items():
        assert S.count(a) == 1, k
        open(P, 'wb').write(S.replace(a, b).encode('utf-8'))
        r = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c91lc_art', '--filter', 'FullyQualifiedName~LandsaleInstrumentTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = r.stdout.splitlines()
        failed = [l.strip() for l in out if l.strip().startswith('失敗 ') or l.strip().startswith('Failed ')]
        tail = [l for l in out if '成功!' in l or '失敗!' in l or 'error' in l.lower()][:3]
        print(k, 'rc', r.returncode, '|', '; '.join(failed)[:300], '|', ' '.join(tail)[:200], flush=True)
finally:
    open(P, 'wb').write(orig)
