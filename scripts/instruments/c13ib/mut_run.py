# (812) c13ib 突然変異の台本：使い捨ての木（<作業>/mut/<名前>）に変異を 1 本当てて、関係する試験だけを回し、赤か緑かを記録する。
# 使い方：python mut_run.py <根> <作業フォルダ> <変異の名前>   変異の名前は MUTS のキー。M0＝変異なしの対照。
# 木の複製は SimCore・Tests・App・Runner と slnx など（measurements・docs・scripts は複製しない）。ビルドは別の artifacts-path。
import os, subprocess, sys, shutil, time

root, work, name = sys.argv[1], sys.argv[2], sys.argv[3]

# 変異＝(ファイル, 旧, 新, 試験の絞り込み)
MUTS = {
    'M0': (None, None, None, 'FullyQualifiedName~Rs2LordOppositionTests|FullyQualifiedName~MandateWallStockCapTests|FullyQualifiedName~EquipDiscoveryTests'),
    # 教育法・女性の権利の反対を貯蓄の割合 W0 から損の式（LordStake）に替える → Rs2 の新しい試験が赤のはず
    'M1': ('PriceOfBread.SimCore/PolicyPreferences.cs',
           'return trad ? -StatusScale * (0.5 + e.Wealth[0]) : 1.0;',
           'return trad ? -StatusScale * (0.5 + LordStake(w, a, e, increasing)) : 1.0;',
           'FullyQualifiedName~Rs2LordOppositionTests'),
    # Spend の MandateSpentNew への足しを S へ足した量（applied）にする → 壁の一歩の請求が b で切れる → MandateWallStockCapTests の新しい試験が赤のはず
    'M2': ('PriceOfBread.SimCore/Government.cs',
           '        w.MandateSpentNew += amount;',
           '        w.MandateSpentNew += applied;',
           'FullyQualifiedName~MandateWallStockCapTests'),
    # 港の門を外す（港の無い国も艦を買う）→ EquipDiscoveryTests が赤のはず（赤にならなければ試験を足す）
    'M3': ('PriceOfBread.SimCore/EconomyStep.cs',
           '        if (!region.NationHasPort) return null;\n',
           '',
           'FullyQualifiedName~MilitaryTests.軍艦の購入は兵の頭数に比例せず海の必要量と在庫の差で決まる|FullyQualifiedName~EquipDiscoveryTests'),
    # 越境移民を止める（焼き込んだ早い return を戻して常に return）→ 越境移民の試験が赤のはず
    'M4': ('PriceOfBread.SimCore/MultiNationSim.cs',
           '        int n = Nations.Count;\n        // 1. 行き先の州・機会・座標・受入国の重み・州の受入の上限・居る階級\n',
           '        return;\n        int n = Nations.Count;\n        // 1. 行き先の州・機会・座標・受入国の重み・州の受入の上限・居る階級\n',
           'FullyQualifiedName~Migration'),
    # Toward の本体を壊す（目標との差の符号）→ 広い範囲が赤のはず（寄せる関数の守りがあるか）
    'M5': ('PriceOfBread.SimCore/PoliticalQuantities.cs',
           'internal static double Toward(double x, double target, double k) => x + k * (target - x);',
           'internal static double Toward(double x, double target, double k) => x + k * (x - target);',
           'FullyQualifiedName~FiscalTests|FullyQualifiedName~CreditTests|FullyQualifiedName~MarketFlow'),
    # OverSteps の本体を壊す（1 − (1 − r)^n を (r)^n に）
    'M6': ('PriceOfBread.SimCore/Calendar.cs',
           'public static double OverSteps(double rate, double steps) => 1.0 - Math.Pow(1.0 - rate, steps);',
           'public static double OverSteps(double rate, double steps) => Math.Pow(rate, steps);',
           'FullyQualifiedName~CreditTests|FullyQualifiedName~MeritocracyTests|FullyQualifiedName~SoldierTests'),
    # UrbanHeadroom の〈組の人口が 0 なら 0〉の早い return を外す（焼き込みで残した 3 つの return の 1 本）→ 赤になる試験があるかを記録
    'M7': ('PriceOfBread.SimCore/Meritocracy.cs',
           '        if (total <= Eps) return 0;\n        return double.PositiveInfinity;',
           '        return double.PositiveInfinity;',
           'FullyQualifiedName~MobilityTests|FullyQualifiedName~MigrationPairTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~MeritocracyTests'),
}

old_file, old, new, flt = MUTS[name]
dst = os.path.join(work, 'mut', name)
if os.path.exists(dst):
    shutil.rmtree(dst)
os.makedirs(dst)
for d in ('PriceOfBread.SimCore', 'PriceOfBread.Tests', 'PriceOfBread.App', 'PriceOfBread.Runner'):
    shutil.copytree(os.path.join(root, d), os.path.join(dst, d), ignore=shutil.ignore_patterns('obj', 'bin'))
for f in os.listdir(root):
    if f.endswith('.slnx') or f.endswith('.json') or f.endswith('.props') or f.endswith('.targets'):
        shutil.copy(os.path.join(root, f), dst)
if old_file:
    p = os.path.join(dst, old_file)
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = b.count(o)
    if c != 1:
        print('NG 変異の文字列が', c, '回', name)
        sys.exit(2)
    open(p, 'wb').write(b.replace(o, n))
art = os.path.join(work, 'mut_art_' + name)
log = os.path.join(work, 'mut_' + name + '.txt')
t0 = time.time()
with open(log, 'wb') as lf:
    r = subprocess.run(['dotnet', 'build', os.path.join(dst, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--disable-build-servers'], stdout=lf, stderr=subprocess.STDOUT)
    if r.returncode != 0:
        print(name, 'ビルド失敗', r.returncode)
        sys.exit(3)
    r = subprocess.run(['dotnet', 'test', os.path.join(dst, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--no-build', '--artifacts-path', art, '--disable-build-servers', '--filter', flt], stdout=lf, stderr=subprocess.STDOUT)
txt = open(log, 'rb').read().decode('utf-8', 'replace')
last = [l for l in txt.split('\n') if '合格:' in l or '失敗:' in l]
print(name, '終了コード', r.returncode, '(0＝緑・1＝赤)', last[-1].strip() if last else '', '%.0f秒' % (time.time() - t0))
fails = [l.strip() for l in txt.split('\n') if l.strip().startswith('失敗 ') or l.strip().startswith('Failed ')]
for l in fails[:8]:
    print('  ', l[:160])
shutil.rmtree(dst, ignore_errors=True)
shutil.rmtree(art, ignore_errors=True)
