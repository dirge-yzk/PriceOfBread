# (830) c66id 突然変異の台本：使い捨ての木 1 本（<作業>/mut_<巡>/tree）を複製し、変異を 1 本ずつ当てて関係する試験だけを回し、赤か緑かを予言と並べる。(824) c65id/mut_run.py の型。
# 使い方：python -I mut_run.py <根> <作業フォルダ> <巡の名前> <組> <変異の名前>...   組は POST だけ（完成した木）。
# 木は SimCore・Tests・App・Runner と slnx など（measurements・docs・scripts は複製しない）。出力：<作業>/mut_<巡>/<名前>.txt と標準出力の要約（measurements/m830_20261104_mutation.txt へ写す）。
import os
import shutil
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')
root, work, tag, group = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
names = sys.argv[5:]
COEF = 'PriceOfBread.SimCore/Coefficients.cs'
HABIT = 'PriceOfBread.SimCore/Habit.cs'
INS = 'PriceOfBread.SimCore/Instruments.cs'
HLG = 'PriceOfBread.SimCore/InstrumentsHabitlg.cs'
DISC = 'PriceOfBread.SimCore/Discovery.cs'
DET = 'PriceOfBread.Tests/DiscoveryEngineerTests.cs'

F_DORM = 'FullyQualifiedName~DormantIdentityTests'
F_HABIT = 'FullyQualifiedName~HabitTests'
F_HLG = 'FullyQualifiedName~HabitLogisticTests'
F_REL = 'FullyQualifiedName~CoefficientRelationTests|FullyQualifiedName~WarVerdictTests'
F_TECH = 'FullyQualifiedName~DiscoveryEngineerTests|FullyQualifiedName~InstitutionAnchorTests|FullyQualifiedName~TechLevelRemovalTests'
T3 = '旗0は製法の行が技術水準の式のまま'
REL = '戦勝点の月の最大の動きは白紙の線を跨がない'
ROWCAP = 'アヘンの行は今の係数の値'
ABS = 'アヘンの計器は旧の蓋と釣り合いの表を出さない'
HLGTBL = 'ロジスティックの表は旗0で実際が旧'
BACK = '削除した休眠の係数は戻っていない'

# 変異＝(編集の一覧 [(ファイル, 旧, 新), ...]（None＝変異なし）, 絞り込み, 注, 予言＝赤になるはずの試験名の断片の一覧〔空＝緑のはず〕)
POST = {
    'M0': (None, F_DORM + '|' + F_HABIT + '|' + F_HLG + '|' + F_REL + '|' + F_TECH, '変異なしの対照（完成した木）', []),
    'Mu1': ([(COEF, '    // 【墓標2026-11-04 (830)・旗の焼き込み】OpiumAddictionCapOff', '    public double OpiumAddictionCapOff = 0;\n    // 【墓標2026-11-04 (830)・旗の焼き込み】OpiumAddictionCapOff'),
             (HABIT, 'c.OpiumAddictionDecay, c.OpiumAddictionCap, c.OpiumLaborPenalty', 'c.OpiumAddictionDecay, c.OpiumAddictionCapOff > 0 ? double.PositiveInfinity : c.OpiumAddictionCap, c.OpiumLaborPenalty')],
            F_DORM, 'CapOff の欄と正の枝を残したまま（注だけ直した形）', [BACK]),
    'Mu2': ([(INS, '        HabitlgTable(sim, seed, q);', '        System.IO.File.WriteAllText(System.IO.Path.Combine(Dir, $"habitcap_s{seed}.tsv"), "x");\n        HabitlgTable(sim, seed, q);')],
            F_HABIT, 'habitcap_s の登録だけ残す（表を書く 1 行を戻す）', [ABS]),
    'Mu3': ([(DISC, 'ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;', 'ratio = 0.0;')],
            F_TECH, '製法の行の技術の比を 0 に固定（〈70×比 以下〉は通り hi＞lo の陽性の対照だけが拾う形）', [T3]),
    'Mu3b': ([(DISC, 'ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;', 'ratio = 0.0;'),
              (DET, '            Assert.True(hi > lo, "旗0で技術水準が製法の行の技術力に効いていない");\n', '')],
             F_TECH, 'Mu3 と同じ変異で T3 の hi＞lo の Assert を外した使い捨ての木（T3 が緑なら陽性の対照が T3 の唯一の拾い手）', ['TechLevelRemoval']),
    'Mu4': ([(COEF, 'public double WhitePeaceScore = 0.25;', 'public double WhitePeaceScore = 0.2;')],
            F_REL, 'WhitePeaceScore の既定を 0.2 にする', [REL, '0.25']),
    'Mu5': ([(HABIT, 'c.OpiumAddictionDecay, c.OpiumAddictionCap, c.OpiumLaborPenalty', 'c.OpiumAddictionDecay, double.PositiveInfinity, c.OpiumLaborPenalty')],
            F_HABIT + '|' + F_HLG, 'Habit.Of の Cap を +∞ に固定（蓋の読みを壊す）', [ROWCAP]),
    'Mu6': ([(HLG, 'd = c.OpiumAddictionCap;', 'd = double.PositiveInfinity;')],
            F_HLG, 'habitlg_s の計器の d を +∞ に固定', [HLGTBL]),
}
MUTS = POST
dotnet = shutil.which('dotnet') or 'dotnet'
base = os.path.join(work, 'mut_' + tag)
tree = os.path.join(base, 'tree')
art = os.path.join(base, 'art')
if not os.path.exists(tree):
    os.makedirs(tree)
    for d in ('PriceOfBread.SimCore', 'PriceOfBread.Tests', 'PriceOfBread.App', 'PriceOfBread.Runner'):
        shutil.copytree(os.path.join(root, d), os.path.join(tree, d), ignore=shutil.ignore_patterns('obj', 'bin'), copy_function=shutil.copyfile)   # 更新時刻を今にする（増分ビルドが前の変異の dll を拾わない）
    for f in os.listdir(root):
        if f.endswith(('.slnx', '.json', '.props', '.targets')):
            shutil.copy(os.path.join(root, f), tree)
csproj = os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')

for name in names:
    edits, flt, note, expect = MUTS[name]
    backups = {}
    ok = True
    if edits:
        cur = {}
        for f, old, new in edits:
            p = os.path.join(tree, f)
            data = cur.get(p) if p in cur else open(p, 'rb').read()
            backups.setdefault(p, open(p, 'rb').read())
            o, n = old.encode('utf-8'), new.encode('utf-8')
            if b'\r\n' in data:
                o, n = o.replace(b'\n', b'\r\n'), n.replace(b'\n', b'\r\n')
            c = data.count(o)
            if c != 1:
                print(name, 'NG 変異の文字列が', c, '回:', old[:60])
                ok = False
                break
            cur[p] = data.replace(o, n)
        if ok:
            for p, data in cur.items():
                open(p, 'wb').write(data)
    if not ok:
        for p, b in backups.items():
            open(p, 'wb').write(b)
        continue
    log = os.path.join(base, name + '.txt')
    t0 = time.time()
    try:
        with open(log, 'wb') as lf:
            r = subprocess.run([dotnet, 'test', csproj, '-c', 'Release', '--artifacts-path', art, '--disable-build-servers', '--filter', flt],
                               stdout=lf, stderr=subprocess.STDOUT)
    finally:
        for p, b in backups.items():
            open(p, 'wb').write(b)
    txt = open(log, 'rb').read().decode('utf-8', 'replace')
    last = [l for l in txt.split('\n') if ('合格:' in l or '失敗:' in l or 'Passed!' in l or 'Failed!' in l)]
    failed = [l.strip() for l in txt.split('\n') if l.strip().startswith(('失敗 ', 'Failed '))]
    print('%s %s' % (name, note))
    print('    rc=%d（0＝緑・1＝赤）%s  %.0f秒' % (r.returncode, (' ' + last[-1].strip()) if last else '', time.time() - t0))
    for l in failed[:12]:
        print('       ', l[:170])
    if not expect:
        verdict = '予言どおり（緑）' if r.returncode == 0 else '結果：赤（予言なし・記録するだけ）'
    else:
        miss = [e for e in expect if not any(e in l for l in failed)]
        extra = [l for l in failed if not any(e in l for e in expect)]
        if r.returncode == 0:
            verdict = '予言と違う（緑のまま＝穴）'
        elif miss:
            verdict = '予言と違う（赤になっていない試験：%s）' % ' / '.join(miss)
        else:
            verdict = '予言どおり（赤にしたい試験が全て赤）'
        if extra:
            verdict += '・予言の外の赤 %d 本' % len(extra)
    print('    →', verdict)
    sys.stdout.flush()
