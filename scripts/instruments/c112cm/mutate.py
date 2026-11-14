# (1147) 突然変異：使い捨ての木 <tree> に 1 つずつ変更を入れ、ビルドして RuleOnsetInstrumentTests を回す。結果は <out> へ。
# 呼び方：python -X utf8 -I mutate.py <tree> <out> <artifacts>   （木は main の c112cm の HEAD の detached worktree）
import sys, subprocess, os
tree, out, art = sys.argv[1:4]
I = tree + '/PriceOfBread.SimCore/Instruments/'
M = {
 'CTL': [],
 'M-b 頭の写しを表の時点で取る': [(I+'InstrumentsRponset.cs', '        RponsetTable(sim, seed, q, dip, st, snap, firstTable);', '        if (snap is not null) snap.Head = RpHeadCopy(sim.Nations);\n        RponsetTable(sim, seed, q, dip, st, snap, firstTable);')] ,
 'M-c DominanceSpeed を落とす': [(I+'InstrumentsRule.cs', 'double v = rule[k] + (t[k] - rule[k]) * speed;', 'double v = rule[k] + (t[k] - rule[k]);')],
 'M-d 持ち主の分も足す': [(I+'InstrumentsTransport.cs', '        if (recipient.Regions.Contains(r)) return;\n        a.PaidRailForeign += gold;', '        a.PaidRailForeign += gold;\n        if (recipient.Regions.Contains(r)) return;')],
 'M-f CfForeignPort が旗を読まない': [(I+'InstrumentsCorncf.cs', 'if (w.Coef.TreatyPortHomeOnly > 0 && r.Tier == RegionTier.Colonial) continue;', '')],
}
res = []
for name, edits in M.items():
    orig = {}
    for p, a, b in edits:
        s = open(p, 'rb').read().decode('utf-8'); assert s.count(a) == 1, (name, a[:40]); orig[p] = s
        open(p, 'wb').write(s.replace(a, b).encode('utf-8'))
    r = subprocess.run(['dotnet', 'test', tree + '/PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~RuleOnsetInstrumentTests', '-nologo'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    txt = r.stdout + r.stderr
    summ = [l for l in txt.splitlines() if '失敗' in l and '合格' in l]
    failed = [l.strip() for l in txt.splitlines() if l.strip().startswith('失敗 ') or l.strip().startswith('Failed ')]
    res.append(f"{name}\trc={r.returncode}\t{summ[-1].strip() if summ else 'ビルド失敗か結果なし'}\t{'; '.join(failed)[:300]}")
    for p, s in orig.items(): open(p, 'wb').write(s.encode('utf-8'))
open(out, 'w', encoding='utf-8', newline='\n').write('\n'.join(res) + '\n')
