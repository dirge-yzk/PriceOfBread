#!/usr/bin/env python
"""(679) 線 navf4：番人 NavalPortProcurementTests の突然変異（隔離の写しで 1 本ずつ入れて Release で回し、赤になることを確かめる）。
使い方：python -X utf8 -I mutate.py <作業フォルダ(D:/…/mut)> [M1 M2 …]   既定は全部。出力は標準出力（M 名・赤の本数・生き残りは SURVIVED）。
写しは bin/obj/.git/docs/measurements を除いた木。変えるのは SimCore の 1 ファイルの 1 か所（バイトの置換・anchor が 1 回だけ出ること）。"""
import os, re, shutil, subprocess, sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
W = sys.argv[1]
want = sys.argv[2:]
MUT = {
    'M1': ('EconomyStep.cs', 'foreach (var r in world.Regions) if (NavalHomePort(r)) tot += r.LastPortThroughput;', 'foreach (var r in world.Regions) if (r.IsPort && r.LastPortThroughput > 0) tot += r.LastPortThroughput;'),
    'M2': ('EconomyStep.cs', 'share[i] = NavalHomePort(r) ? r.LastPortThroughput / tot : 0.0;', 'share[i] = (r.IsPort && r.LastPortThroughput > 0) ? r.LastPortThroughput / tot : 0.0;'),
    'M3': ('EconomyStep.cs', 'share[i] = NavalHomePort(r) ? r.LastPortThroughput / tot : 0.0;', 'share[i] = NavalHomePort(r) ? r.LastPortThroughput / (2 * tot) : 0.0;'),
    'M4': ('EconomyStep.cs', 'region.StateSpent[(int)TreasuryCat.Navy] += pay;\n            (region.FleetUnits', 'region.StateSpent[(int)TreasuryCat.Army] += pay;\n            (region.FleetUnits'),
    'M5': ('EconomyStep.cs', 'if (!(tot > 1e-9)) { share[cap] = 1.0;', 'if (false) { share[cap] = 1.0;'),
    'M6': ('EconomyStep.cs', '        needCost = need;\n', '        needCost = capCost;\n'),
    'M7': ('EconomyStep.cs', 'budgetShare[i] = share[i] * FleetOrderCost(r, coef, q);', 'budgetShare[i] = share[i];'),
    'M8': ('EconomyStep.cs', 'orderShare > 0 ? navalNat * budgetShare : 0;', 'orderShare > 0 ? navalNat : 0;'),
    'M9': ('Simulation.cs', 'r.NavalBudget = EconomyStep.NavalPortBudget(navalNat, sh, navBudShare![navIdx]);', 'r.NavalBudget = r.Id == 0 ? navalNat : 0;'),
}


def copy_tree(dst):
    if os.path.exists(dst):
        shutil.rmtree(dst)
    shutil.copytree(REPO, dst, ignore=shutil.ignore_patterns('bin', 'obj', '.git', 'docs', 'measurements', 'uishot', '.vs', 'scripts'))


for name in (want or list(MUT)):
    f, old, new = MUT[name]
    dst = os.path.join(W, 'src_' + name)
    copy_tree(dst)
    p = os.path.join(dst, 'PriceOfBread.SimCore', f)
    b = open(p, 'rb').read().decode('utf-8')
    crlf = '\r\n' in b
    o = old.replace('\n', '\r\n') if crlf else old
    n = new.replace('\n', '\r\n') if crlf else new
    assert b.count(o) == 1, f'{name}: anchor が {b.count(o)} 回'
    open(p, 'wb').write(b.replace(o, n).encode('utf-8'))
    r = subprocess.run(['dotnet', 'test', os.path.join(dst, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', os.path.join(W, 'art_' + name), '-nologo',
                        '--filter', 'FullyQualifiedName~NavalPortProcurement'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    out = r.stdout + r.stderr
    m = re.search(r'(失敗|Failed):\s*(\d+)', out)
    failed = [l.strip() for l in out.splitlines() if re.match(r'\s*(失敗|Failed) PriceOfBread', l)]
    print(f'{name}: rc={r.returncode} 赤={len(failed)} {"SURVIVED" if r.returncode == 0 else "KILLED"}')
    for l in failed[:4]:
        print('   ', l[:200])
    shutil.rmtree(dst, ignore_errors=True); shutil.rmtree(os.path.join(W, 'art_' + name), ignore_errors=True)
