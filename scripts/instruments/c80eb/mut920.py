# (920) c80eb 突然変異：使い捨ての写し（git worktree --detach）で M1〜M8 を 1 本ずつ入れ、BillBlockedColumnsTests が赤になるかを測る。
#   python -X utf8 -I mut920.py <写しの木> <成果の txt> <隔離ビルドの置き場>
#   写しの木は事前に `git worktree add --detach <写しの木> <c80eb の頭>` で作る。1 本ごとに `git checkout -- .` で戻す。
import subprocess, sys, os
tree, outp, art = sys.argv[1:4]
T = chr(92) + 't'
M = {
 'M1': ('PriceOfBread.SimCore/BilateralMarket.cs', 'if (ins2) Instruments.CearnBillSettle(r2, p2, debt0 - p2.Debt.Value);', '', '決済 ① の呼び口を外す'),
 'M2': ('PriceOfBread.SimCore/InstrumentsCearn.cs', 'if (m is not null && m.Debt.Value > 0) { a.BillBlocked++;', 'if (m is not null && m.Debt.Value > 1e99) { a.BillBlocked++;', '清算の頭の数えを外す'),
 'M3': ('PriceOfBread.SimCore/InstrumentsCearn.cs', 'if (!(m.Debt.Value > 1e-12)) a.BillBlockedDust++;', 'if (!(m.Debt.Value > 1e-9)) a.BillBlockedDust++;', '塵の閾値を 1e-9 へ'),
 'M4': ('PriceOfBread.SimCore/EmptyPopEstate.cs', 'Core(w); if (Instruments.On) Instruments.EstateAfter(w); }', 'Core(w); }', 'Step の EstateAfter の呼びを外す'),
 'M5': ('PriceOfBread.SimCore/Model.cs', 'Instruments.CearnCarryDebt(to, debtIn.Value);', 'Instruments.CearnCarryDebt(to, from.Debt.Value + debtIn.Value);', 'Carry の呼び口で受けた額でなく運び元の残りも足す'),
 'M6': ('PriceOfBread.SimCore/InstrumentsCearn.cs', 'oq[ok] = ov - delta;', 'oq[ok] = ov;', 'outOther の経路別の和を足さない'),
 'M7': ('PriceOfBread.SimCore/Model.cs', ' Instruments.CearnCarryDebt(to, debtIn.Value);', '', 'Carry の呼び口を外す'),
 'M8': ('PriceOfBread.SimCore/InstrumentsCearn.cs', 'a.EstDebtMoved += (a.EstDebtBeforeSnap - after) - (a.EstDebtSettled - a.EstSettledSnap);', 'a.EstDebtMoved += (a.EstDebtBeforeSnap - after);', '返した額の引き算を外す'),
}
def sh(cmd):
    return subprocess.run(cmd, cwd=tree, capture_output=True, text=True, encoding='utf-8', errors='replace')
res = []
for k, (f, old, new, desc) in M.items():
    p = os.path.join(tree, f)
    b = open(p, 'rb').read().decode('utf-8')
    if b.count(old) != 1:
        res.append(f'{k}\t{desc}\t突然変異を入れられない（{b.count(old)} 箇所）'); continue
    open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
    r = sh(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', art, '--disable-build-servers', '--filter', 'FullyQualifiedName~BillBlockedColumnsTests'])
    out = r.stdout
    fails = [l.strip() for l in out.splitlines() if l.strip().startswith('失敗 ') or l.strip().startswith('Failed ')]
    tail = [l for l in out.splitlines() if '合格' in l or 'Passed!' in l or 'Failed!' in l or '失敗!' in l or '成功!' in l]
    res.append(f'{k}\t{desc}\t{"赤" if fails else "緑（突然変異で確かめていない／穴）"}\t' + ' | '.join(x.split(" [")[0].replace('失敗 PriceOfBread.Tests.BillBlockedColumnsTests.', '') for x in fails) + '\t' + (tail[-1].strip() if tail else out[-200:].replace('\n', ' ')))
    sh(['git', 'checkout', '--', '.'])
open(outp, 'w', encoding='utf-8').write('\n'.join(res) + '\n')
