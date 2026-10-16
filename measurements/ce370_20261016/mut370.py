#!/usr/bin/env python
"""(370) 突然変異：EconomyStep.ArmLuxuryBudget の形（割合を a×X にも掛ける・実効の a）を壊して ConsumptionLayersTests が赤になることを確かめる。
  python mut370.py <worktree> <artifacts-path> <出力>
  作業ツリーの EconomyStep.cs をバイトで書き換え、試験の後に必ず元へ戻す。"""
import sys, subprocess, os
wt, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
path = os.path.join(wt, 'PriceOfBread.SimCore', 'EconomyStep.cs')
orig = open(path, 'rb').read()
MUTS = [
    ('M1 割合を a×X に掛けない（旧 (364) の形）', '(a * x + r * sp) * bf', 'a * x + r * sp * bf'),
    ('M2 実効の a を a のまま（割合を掛けない）', 'aEff[i] = a * bf;', 'aEff[i] = a;'),
    ('M3 実効の a を 0 にする', 'aEff[i] = a * bf;', 'aEff[i] = 0;'),
    ('M4 需要の側の減りだけ a のまま（支払いの側は実効の a）', 'layBud3[pIdx] - layW * layAEff[pIdx] * dNec * buyerPrice', 'layBud3[pIdx] - layW * coef.ConsumeLuxuryIncomeShare.Value * dNec * buyerPrice'),
]
res = []
try:
    with open(out, 'w', encoding='utf-8') as f:
        for name, old, new in MUTS:
            s = orig.decode('utf-8')
            assert s.count(old) == 1, (name, s.count(old))
            open(path, 'wb').write(s.replace(old, new).encode('utf-8'))
            p = subprocess.run(['dotnet', 'test', os.path.join(wt, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~ConsumptionLayers'],
                               capture_output=True, text=True, encoding='utf-8', errors='replace')
            tail = [l for l in p.stdout.splitlines() if '失敗:' in l or '成功!' in l or '失敗!' in l or 'error' in l.lower()][-4:]
            red = p.returncode != 0
            f.write(f'{name}: {"赤" if red else "★緑（生き残り）"} rc={p.returncode}\n  ' + '\n  '.join(tail) + '\n')
            f.flush()
finally:
    open(path, 'wb').write(orig)
