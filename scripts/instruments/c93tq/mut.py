# (1008) 突然変異 M1〜M6：使い捨ての木（引数 1＝木）で 1 本ずつ書き換え→ TradeInstrumentTests を回す→ git checkout で戻す。出力は標準出力（measurements へ写す）。
import subprocess, sys, os
tree = sys.argv[1]; art = sys.argv[2]
M = {
 'M1 dutyCornMean に duty を積む (G5)': ('PriceOfBread.SimCore/InstrumentsTradeskip.cs', 'D[di + 2] += cornPen;', 'D[di + 2] += duty;'),
 'M2 dutyCornPaid を積まない (G6)': ('PriceOfBread.SimCore/BilateralMarket.cs', 'crossA ? vA * cornPen / exRate[ni] : 0.0)', '0.0)'),
 'M3 度数を門の前で全て xPassBin へ (G7)': ('PriceOfBread.SimCore/InstrumentsTradeskip.cs', ['GBin0 + b)); continue; }', 'GBin0 + 10 + b)); continue; }', 'GBin0 + 20 + b);'], ['GBin0 + 30 + b)); continue; }', 'GBin0 + 30 + b)); continue; }', 'GBin0 + 30 + b);']),
 'M4 tarEff を w.Tariff で写す (G8 条約港)': ('PriceOfBread.SimCore/InstrumentsTradeskip.cs', 'a.TarEff[i] = tar is not null ? tar[i] : double.NaN;', 'a.TarEff[i] = nations[i].World.Tariff;'),
 'M5 門の順を 楔→容量 に替える (G9)': ('PriceOfBread.SimCore/InstrumentsTradeskip.cs', 'if (capLeft[sn] <= 1e-12 || capLeft[t] <= 1e-12) {', 'if ((capLeft[sn] <= 1e-12 || capLeft[t] <= 1e-12) && !(selP * (baseMul + wedge - (sellCheap is not null ? sellCheap[sNat] : 0.0)) >= lp)) {'),
}
def edit(path, a, b):
    p = os.path.join(tree, path); s = open(p, 'rb').read().decode('utf-8')
    assert s.count(a) == 1, (path, a, s.count(a)); open(p, 'wb').write(s.replace(a, b).encode('utf-8'))
def run(name, edits):
    for path, a, b in edits: edit(path, a, b)
    r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~TradeInstrumentTests', '--logger', 'console;verbosity=normal'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    out = r.stdout.splitlines()
    print('=== ' + name)
    for l in out:
        if '[FAIL]' in l or l.strip().startswith('失敗 ') or l.strip().startswith('Failed ') or l.startswith('失敗!') or l.startswith('成功!') or 'error CS' in l: print(l[:220])
    subprocess.run(['git', '-C', tree, 'checkout', '--', '.'])
    sys.stdout.flush()
for name, spec in M.items():
    if name.startswith('M3'):
        run(name, [(spec[0], spec[1][0], spec[1][0])] if False else [(spec[0], 'if (!nan) Add(curBn, curG, TsCol.GBin0 + b); continue; }', 'if (!nan) Add(curBn, curG, TsCol.GBin0 + 30 + b); continue; }'),
                       (spec[0], 'Add(curBn, curG, TsCol.GBin0 + 10 + b); continue; }', 'Add(curBn, curG, TsCol.GBin0 + 30 + b); continue; }'),
                       (spec[0], 'Add(curBn, curG, TsCol.XWedge); if (!nan) Add(curBn, curG, TsCol.GBin0 + 20 + b);', 'Add(curBn, curG, TsCol.XWedge); if (!nan) Add(curBn, curG, TsCol.GBin0 + 30 + b);')])
    else: run(name, [spec])
run('M6 払いに国内の成約を混ぜる (G6)', [('PriceOfBread.SimCore/BilateralMarket.cs', 'crossA ? vA * protect / exRate[ni] : 0.0', 'vA * protect / exRate[ni]'), ('PriceOfBread.SimCore/InstrumentsNationgood.cs', 'if (!cross || !NgAccs.TryGetValue', 'if (!NgAccs.TryGetValue')])
