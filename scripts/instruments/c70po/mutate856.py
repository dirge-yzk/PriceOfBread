# -*- coding: utf-8 -*-
# (856) c70po 突然変異の台本（(850) の mutate850.py の形・置き換えを組にできるように直した）：使い捨ての木（git worktree・点火のコミット）に 1 本ずつ入れ、
# 番人（RuleOrderP1・CrimeTests・RuleSharesFormTests）の赤と緑を数える。
#   python -X utf8 -I mutate856.py <使い捨ての木> <出力の表>
# 書き換えはバイトで読み書きする（CR の数と BOM を保つ）。各突然変異の後に元のバイトへ戻す。
import sys, os, subprocess
TREE, OUT = sys.argv[1], sys.argv[2]
P = os.path.join(TREE, 'PriceOfBread.SimCore', 'Politics.cs')
C = os.path.join(TREE, 'PriceOfBread.SimCore', 'Crime.cs')
RS = os.path.join(TREE, 'PriceOfBread.Tests', 'RuleSharesFormTests.cs')
RP = os.path.join(TREE, 'PriceOfBread.Tests', 'RuleOrderP1Tests.cs')
GOOD = b'orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(rawOrder * mbar / mr);'
D2A = 'France(c => c.RuleOrderByRegion = 0);   // ★(856) 旗 0 の経路の番人（★(832) ① の (a)・旗 1 の同じ主張は RuleOrderP1Tests の P1_6）'.encode()
D2B = 'France(c => c.RuleOrderByRegion = 0);   // ★(856) 旗 0 の経路の番人（★(832) ① の (a)・旗 1 の同じ主張は RuleOrderP1Tests の P1_7）'.encode()
HALF = b'        SetRaw(w, 0.5);\n        SetMilitancy(w, _ => 0.1);\n    }'
MUT = [
    ('M1 m̄÷m_r を外す（order_r＝Clamp01(R)）', [(P, GOOD, b'orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(rawOrder);')]),
    ('M2 order×(1−m_r) の形', [(P, GOOD, b'orderR = PoliticalQuantities.Clamp01(rawOrder) * (1.0 - mr);')]),
    ('M3 切った R を使う', [(P, GOOD, b'orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(PoliticalQuantities.Clamp01(rawOrder) * mbar / mr);')]),
    ('M4 m_r を m̄ に替える', [(P, b'double mr = AvgRegionMilitancy(r4);', b'double mr = mbar;')]),
    ('M5 犯罪が州の闘争度で秩序を読む（旗 1 のとき）', [(C, b'double crimeRate = c.CrimeBase * (1.0 - order)',
     b'double crimeRate = c.CrimeBase * (1.0 - (c.RuleOrderByRegion > 0 ? Math.Min(1.0, order * 0.1 / Math.Max(1e-9, Politics.AvgRegionMilitancy(r))) : order))')]),
    ('M7 Clamp01 を外す（order_r が 1 を超える）', [(P, GOOD, b'orderR = !(mr > 0) ? 1.0 : (rawOrder * mbar / mr);')]),
    ('D0 D2 の 2 本の初期化子の旗 0 を外す（旗 1 の既定の木で）', [(RS, D2A, b'France(_ => { });'), (RS, D2B, b'France(_ => { });')]),
    ('D1 P1_6・P1_7 の盤面で Coercion を置かず w.PublicOrder だけ 0.5', [(RP, HALF, b'        w.PublicOrder = 0.5;\n        SetMilitancy(w, _ => 0.1);\n    }')]),
]
FILTER = 'FullyQualifiedName~RuleOrderP1|FullyQualifiedName~CrimeTests|FullyQualifiedName~RuleSharesFormTests'
lines = []
for i, (name, reps) in enumerate(MUT):
    origs = {}
    for path, old, new in reps:
        if path not in origs: origs[path] = open(path, 'rb').read()
    cur = dict(origs)
    for path, old, new in reps:
        assert cur[path].count(old) == 1, (name, cur[path].count(old))
        cur[path] = cur[path].replace(old, new)
    for path, b in cur.items(): open(path, 'wb').write(b)
    try:
        art = 'D:/PoB_work/c70po_mutart'
        r = subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                            '--disable-build-servers', '--filter', FILTER], capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = r.stdout + r.stderr
        failed = sorted(set(l.split('失敗 ')[1].split(' [')[0].strip() for l in out.splitlines() if l.strip().startswith('失敗 ')))
        summ = [l.strip() for l in out.splitlines() if '合格:' in l or 'Passed!' in l or 'Failed!' in l or '失敗!' in l or '成功!' in l]
        lines.append('%s\trc=%d\t%s\t赤の試験：%s' % (name, r.returncode, ' / '.join(summ) or '(要約なし)', '・'.join(failed) or 'なし'))
    finally:
        for path, b in origs.items(): open(path, 'wb').write(b)
    print(lines[-1], flush=True)
with open(OUT, 'wb') as f:
    f.write(('\n'.join(lines) + '\n').encode('utf-8'))
