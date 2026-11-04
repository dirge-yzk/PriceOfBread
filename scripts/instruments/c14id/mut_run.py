# (817) c14id 突然変異の台本：使い捨ての木 1 本（<作業>/mut/tree）を複製し、変異を 1 本ずつ当てて関係する試験だけを回し、赤か緑かを記録して必ず元へ戻す。
# 使い方：python -I mut_run.py <根> <作業フォルダ> <変異の名前>...   名前は MUTS のキー。M0＝変異なしの対照。
# 木は SimCore・Tests・App・Runner と slnx など（measurements・docs・scripts は複製しない）。ビルドは 1 本の artifacts-path を使い回す（増分）。
# 出力：<作業>/mut/<名前>.txt（dotnet の全文）と標準出力の要約（measurements/m817_20261104_mutation.txt へ写す）。
import os
import shutil
import subprocess
import sys
import time

root, work = sys.argv[1], sys.argv[2]
names = sys.argv[3:]
SIM = 'PriceOfBread.SimCore/Simulation.cs'

# 試験の絞り込み
F_RECIPE = 'FullyQualifiedName~SecondRecipeContentsTests'
F_NAVAL = 'FullyQualifiedName~MilitaryTests.海軍の予算は必要額で切られ'
F_POPSCAN = 'FullyQualifiedName~PopScanTests.全ての州が植民地格の国は国平均を全ての州で取る'
F_MIG = 'FullyQualifiedName~ConservationTests.国家間移民'

# 変異＝(ファイル, 旧, 新, 絞り込み, 注)
MUTS = {
    'M0': (None, None, None, F_RECIPE + '|' + F_NAVAL + '|' + F_POPSCAN + '|' + F_MIG, '変異なしの対照'),
    # T1 冷蔵の加工肉
    'M5a': (SIM, 'new Input(Catalog.MeatId, 0.6), new Input(Catalog.RefrigeratorId, coef.ChilledMeatColdInput)',
            'new Input(Catalog.MeatId, 0.5), new Input(Catalog.RefrigeratorId, coef.ChilledMeatColdInput)', F_RECIPE, '加工肉の肉の投入量 0.6→0.5'),
    'M5b': (SIM, 'new Input(Catalog.RefrigeratorId, coef.ChilledMeatColdInput)', 'new Input(Catalog.SaltId, coef.ChilledMeatColdInput)', F_RECIPE, '冷蔵庫の投入を塩に替える'),
    'M5c': (SIM, 'Productivity = saltCured.Productivity * coef.ChilledMeatProductivity,', 'Productivity = saltCured.Productivity,', F_RECIPE, '冷蔵の加工肉の生産性の倍率を掛けない'),
    # T1 石油ケロシン
    'M6a': (SIM, 'Inputs = [new Input(Catalog.CrudeOilId, coef.KeroseneOilInput)', 'Inputs = [new Input(Catalog.CopperOreId, coef.KeroseneOilInput)', F_RECIPE, '灯油の原油の投入を銅鉱に替える'),
    'M6b': (SIM, 'Productivity = coef.KeroseneOilProductivity * FullChainProductivityScale,', 'Productivity = FullChainProductivityScale,', F_RECIPE, '灯油の生産性の倍率を掛けない'),
    'M6c': (SIM, 'new Input(Catalog.CrudeOilId, coef.KeroseneOilInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LampOilId))]', 'new Input(Catalog.CrudeOilId, coef.KeroseneOilInput), new Input(Catalog.LaborId, LaborPerOutput)]', F_RECIPE, '灯油の技師の投入を外す'),
    # T2 艦の 2 か所
    'M1b': (SIM, '                    navalCap = Math.Min(navalCap, EconomyStep.NavalProcurementNeed(r, World.Coef, World));\n', '                    // M1b: navalCap の必要額での切り捨てを外した\n', F_NAVAL, '旗 0 の経路の navalCap の Min を外す'),
    'M1c': (SIM, '                    navalNat = Math.Min(navalNat, navNeedNat);\n', '                    // M1c: navalNat の必要額での切り捨てを外した\n', F_NAVAL, '旗 1 の経路の navalNat の Min を外す'),
    # T3 植民地だけの国
    'M3a': (SIM, '        if (mainPop > 1e-9) { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }\n', '        { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }\n', F_POPSCAN, '本土の州が無いときの退避の枝を外す（国平均が 0 になる）'),
    # T4 越境移民
    'M4': ('PriceOfBread.SimCore/MultiNationSim.cs',
           '        int n = Nations.Count;\n        // 1. 行き先の州・機会・座標・受入国の重み・州の受入の上限・居る階級\n',
           '        return;\n        int n = Nations.Count;\n        // 1. 行き先の州・機会・座標・受入国の重み・州の受入の上限・居る階級\n', F_MIG, '越境移民を止める（最初の集計の後で return）'),
}

dotnet = shutil.which('dotnet') or 'dotnet'
tree = os.path.join(work, 'mut', 'tree')
art = os.path.join(work, 'mut', 'art')
if not os.path.exists(tree):
    os.makedirs(tree)
    for d in ('PriceOfBread.SimCore', 'PriceOfBread.Tests', 'PriceOfBread.App', 'PriceOfBread.Runner'):
        shutil.copytree(os.path.join(root, d), os.path.join(tree, d), ignore=shutil.ignore_patterns('obj', 'bin'), copy_function=shutil.copyfile)   # 更新時刻を今にする（増分ビルドが前の変異の dll を使い回さない）
    for f in os.listdir(root):
        if f.endswith(('.slnx', '.json', '.props', '.targets')):
            shutil.copy(os.path.join(root, f), tree)
csproj = os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')

for name in names:
    f, old, new, flt, note = MUTS[name]
    backup = None
    if f:
        p = os.path.join(tree, f)
        backup = open(p, 'rb').read()
        o, n = old.encode('utf-8'), new.encode('utf-8')
        if b'\r\n' in backup:
            o, n = o.replace(b'\n', b'\r\n'), n.replace(b'\n', b'\r\n')
        c = backup.count(o)
        if c != 1:
            print(name, 'NG 変異の文字列が', c, '回')
            continue
        open(p, 'wb').write(backup.replace(o, n))
    log = os.path.join(work, 'mut', name + '.txt')
    t0 = time.time()
    try:
        with open(log, 'wb') as lf:
            r = subprocess.run([dotnet, 'test', csproj, '-c', 'Release', '--artifacts-path', art, '--disable-build-servers', '--filter', flt],
                               stdout=lf, stderr=subprocess.STDOUT)
    finally:
        if f:
            open(p, 'wb').write(backup)
    txt = open(log, 'rb').read().decode('utf-8', 'replace')
    last = [l for l in txt.split('\n') if ('合格:' in l or '失敗:' in l or 'Passed!' in l or 'Failed!' in l)]
    print('%s %s' % (name, note))
    print('    rc=%d（0＝緑・1＝赤）%s  %.0f秒' % (r.returncode, (' ' + last[-1].strip()) if last else '', time.time() - t0))
    for l in [l.strip() for l in txt.split('\n') if l.strip().startswith(('失敗 ', 'Failed '))][:10]:
        print('       ', l[:170])
    sys.stdout.flush()
