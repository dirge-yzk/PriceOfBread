# -*- coding: utf-8 -*-
# (850) c69po 突然変異の台本：使い捨ての木（git worktree・器のコミット）に 1 本ずつ入れ、番人（RuleOrderP1・CrimeTests）の赤と緑を数える。
#   python -X utf8 -I mutate850.py <使い捨ての木> <出力の表>
# 書き換えはバイトで読み書きする（CR の数と BOM を保つ）。各突然変異の後に元のバイトへ戻す。
import sys, os, subprocess
TREE, OUT = sys.argv[1], sys.argv[2]
P = os.path.join(TREE, 'PriceOfBread.SimCore', 'Politics.cs')
C = os.path.join(TREE, 'PriceOfBread.SimCore', 'Crime.cs')
GOOD = b'orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(rawOrder * mbar / mr);'
MUT = [
    ('M1 m̄÷m_r を外す（order_r＝Clamp01(R)）', P, GOOD, b'orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(rawOrder);'),
    ('M2 order×(1−m_r) の形', P, GOOD, b'orderR = PoliticalQuantities.Clamp01(rawOrder) * (1.0 - mr);'),
    ('M3 切った R を使う', P, GOOD, b'orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(PoliticalQuantities.Clamp01(rawOrder) * mbar / mr);'),
    ('M4 m_r を m̄ に替える', P, b'double mr = AvgRegionMilitancy(r4);', b'double mr = mbar;'),
    ('M5 犯罪が州の闘争度で秩序を読む（旗 1 のとき）', C, b'double crimeRate = c.CrimeBase * (1.0 - order)',
     b'double crimeRate = c.CrimeBase * (1.0 - (c.RuleOrderByRegion > 0 ? Math.Min(1.0, order * 0.1 / Math.Max(1e-9, Politics.AvgRegionMilitancy(r))) : order))'),
    ('M7 Clamp01 を外す（order_r が 1 を超える）', P, GOOD, b'orderR = !(mr > 0) ? 1.0 : (rawOrder * mbar / mr);'),
]
FILTER = 'FullyQualifiedName~RuleOrderP1|FullyQualifiedName~CrimeTests'
lines = []
for i, (name, path, old, new) in enumerate(MUT):
    orig = open(path, 'rb').read()
    assert orig.count(old) == 1, (name, orig.count(old))
    open(path, 'wb').write(orig.replace(old, new))
    try:
        art = 'D:/PoB_work/c69po_mutart%d' % i
        r = subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                            '--filter', FILTER], capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = r.stdout + r.stderr
        failed = sorted(set(l.split('失敗 ')[1].split(' [')[0].strip() for l in out.splitlines() if l.strip().startswith('失敗 ')))
        summ = [l.strip() for l in out.splitlines() if '合格:' in l or 'Passed!' in l or 'Failed!' in l or '失敗!' in l or '成功!' in l]
        lines.append('%s\trc=%d\t%s\t赤の試験：%s' % (name, r.returncode, ' / '.join(summ) or '(要約なし)', '・'.join(failed) or 'なし'))
    finally:
        open(path, 'wb').write(orig)
    print(lines[-1], flush=True)
with open(OUT, 'wb') as f:
    f.write(('\n'.join(lines) + '\n').encode('utf-8'))
