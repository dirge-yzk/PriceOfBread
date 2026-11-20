# (1262) c129sb 突然変異の台本：使い捨ての木（D:/PoB_work/c129sb_mut）で 1 本ずつ当て、絞った試験を回して赤の名前を記録し、元へ戻す。
# 使い方: python -X utf8 -I mut_sb.py <木> <出力ファイル>
import subprocess, sys, os
tree, out = sys.argv[1], sys.argv[2]
ART = 'D:/PoB_work/c129sb_mutart'
def rd(p): return open(os.path.join(tree, p), 'rb').read().decode('utf-8')
def wr(p, t): open(os.path.join(tree, p), 'wb').write(t.encode('utf-8'))
M = [
 ('M2 PairFlowQ の詰めを Take(n-1) へ', 'PriceOfBread.SimCore/MultiNationSim.cs',
  'pfq.Where((_, a) => a != ri).Select(row => row.Where((_, b) => b != ri).ToArray()).ToArray()',
  'pfq.Take(pfq.Length - 1).Select(row => row.Take(row.Length - 1).ToArray()).ToArray()', 'DissolveNationTests'),
 ('M3 HorseCostOf の値を +1', 'PriceOfBread.SimCore/Instruments/InstrumentsRk.cs',
  '        return bh;\n    }', '        return bh + 1;\n    }', 'RkInstrumentTests'),
 ('M4 PaysaleMonth の su と pu の順を逆', 'PriceOfBread.SimCore/Instruments/InstrumentsPaysale.cs',
  'PaysaleStakeDiff(su, pu, diff)', 'PaysaleStakeDiff(pu, su, diff)', 'RkInstrumentTests'),
 ('M6a 送電網の劣化を 1−2×率', 'PriceOfBread.SimCore/EconomyStep.cs',
  'region.GridCapacity *= 1.0 - coef.GridReplaceRate;', 'region.GridCapacity *= 1.0 - 2.0 * coef.GridReplaceRate;', 'GridDegradeEngineTests'),
 ('M6b 送電網の劣化を消す', 'PriceOfBread.SimCore/EconomyStep.cs',
  'region.GridCapacity *= 1.0 - coef.GridReplaceRate;', 'region.GridCapacity *= 1.0;', 'GridDegradeEngineTests'),
 ('M5 FormedElsewhere の段の条件を消す', 'PriceOfBread.SimCore/Formations.cs',
  ' || q.FormationTier < r.Tier) continue;', ') continue;', 'UnificationSkipFormedTests'),
 ('M7 退役した係数 GridKeepPerMonth を戻す', 'PriceOfBread.SimCore/Coefficients.p08_audit_soldiers.cs',
  '    public double GridReplaceRate = 0.002;', '    public double GridKeepPerMonth = 0.998;\n    public double GridReplaceRate = 0.002;', 'DormantIdentityTests.削除した休眠の係数は戻っていない'),
]
res = []
for name, f, old, new, flt in M:
    t = rd(f); assert old in t, (name, 'no match')
    wr(f, t.replace(old, new, 1))
    r = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', ART, '--filter', 'FullyQualifiedName~' + flt],
                       cwd=tree, capture_output=True, text=True, encoding='utf-8', errors='replace')
    wr(f, t)
    fails = [l.strip()[:150] for l in r.stdout.splitlines() if l.strip().startswith('失敗 ')]
    tail = [l for l in r.stdout.splitlines() if '合計:' in l]
    res.append(f'{name}: rc={r.returncode} 赤={len(fails)} {fails} {tail[-1] if tail else ""}')
    open(out, 'w', encoding='utf-8').write('\n'.join(res) + '\n')
# 元の木（変異なし）の対照
r = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', ART, '--filter',
    'FullyQualifiedName~DissolveNationTests|FullyQualifiedName~RkInstrumentTests|FullyQualifiedName~GridDegradeEngineTests|FullyQualifiedName~UnificationSkipFormedTests'],
    cwd=tree, capture_output=True, text=True, encoding='utf-8', errors='replace')
tail = [l for l in r.stdout.splitlines() if '合計:' in l]
res.append(f'対照（変異なし）: rc={r.returncode} {tail[-1] if tail else ""}')
open(out, 'w', encoding='utf-8').write('\n'.join(res) + '\n')
