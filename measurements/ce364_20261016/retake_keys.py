import re, subprocess, difflib
ROOT = 'D:/PoB_work/wt_ce4/'
rp = ROOT + 'scripts/instruments/flow262/route_names.py'
py = open(rp, 'rb').read().decode('utf-8')
maps = {}
for f in ['World.cs', 'Simulation.cs', 'MultiNationSim.cs', 'EconomyStep.cs', 'Instruments.cs', 'Model.cs', 'InstrumentsPop.cs', 'Coefficients.cs']:
    old = subprocess.run(['git', 'show', 'HEAD:PriceOfBread.SimCore/' + f], capture_output=True, cwd=ROOT).stdout.decode('utf-8').split('\n')
    new = open(ROOT + 'PriceOfBread.SimCore/' + f, 'rb').read().decode('utf-8').split('\n')
    sm = difflib.SequenceMatcher(None, old, new, autojunk=False)
    m = {}
    for a, b, n in sm.get_matching_blocks():
        for k in range(n):
            m[a + k + 1] = b + k + 1
    maps[f] = m
def sub(mo):
    f, ln = mo.group(1), int(mo.group(2))
    if f in maps and ln != maps[f].get(ln, ln):
        print('shift', f, ln, '->', maps[f].get(ln))
    if f in maps:
        if ln not in maps[f]:
            raise SystemExit('unmapped ' + f + str(ln))
        return "('%s', %d):" % (f, maps[f][ln])
    return mo.group(0)
py2 = re.sub(r"\('([A-Za-z0-9]+\.cs)', (\d+)\):", sub, py)
open(rp, 'wb').write(py2.encode('utf-8'))
