# (824) c65id 突然変異の台本：使い捨ての木 1 本（<作業>/mut_<巡>/tree）を複製し、変異を 1 本ずつ当てて関係する試験だけを回し、赤か緑かを予言と並べて記録して必ず元へ戻す。
# 使い方：python -I mut_run.py <根> <作業フォルダ> <巡の名前> <組> <変異の名前>...   組は PRE（焼き込みの前の木）か POST（最後の木）。名前は組の MUTS のキー。M0＝変異なしの対照。
# 標準出力は UTF-8 に固定する（Windows の既定の文字コードで化けた：r1）。
# 巡の名前は出力の名前に入れる（上書きしない＝(817) の教訓）。木は SimCore・Tests・App・Runner と slnx など（measurements・docs・scripts は複製しない）。ビルドは巡ごとに 1 本の artifacts-path を使い回す（増分）。
# 出力：<作業>/mut_<巡>/<名前>.txt（dotnet の全文）と標準出力の要約（measurements/m824_20261104_mutation.txt へ写す）。
import os
import shutil
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')
root, work, tag, group = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
names = sys.argv[5:]
DISC = 'PriceOfBread.SimCore/Discovery.cs'
SELF = 'PriceOfBread.SimCore/SelfPlay.cs'
AGENTS = 'PriceOfBread.SimCore/Agents.cs'
MNS = 'PriceOfBread.SimCore/MultiNationSim.cs'
SIM = 'PriceOfBread.SimCore/Simulation.cs'
TERR = 'PriceOfBread.SimCore/Territory.cs'

F_DE = 'FullyQualifiedName~DiscoveryEngineerTests'
F_IA = 'FullyQualifiedName~InstitutionAnchorTests'
F_TL = 'FullyQualifiedName~TechLevelRemovalTests'
F_TECH = F_DE + '|' + F_IA + '|' + F_TL
F_NAVAL = 'FullyQualifiedName~MilitaryTests.海軍の予算は必要額で切られ'
F_NPP = 'FullyQualifiedName~NavalPortProcurementTests'
F_POPSCAN = 'FullyQualifiedName~PopScanTests.全ての州が植民地格の国は国平均を全ての州で取る'

T3 = '旗0は製法の行が技術水準の式のまま'            # DiscoveryEngineerTests
SPR = '旗が正なら製法と産地と制度と思想の行は技術水準を読まない'   # DiscoveryEngineerTests
IA0 = '係数0は識字に依らず技術の比は1'                # InstitutionAnchorTests
IA1 = '係数が正なら技術の項は本国の識字に比例し'         # InstitutionAnchorTests
TL2A1 = '段2a_技術の遅れは姿勢の効用に入らない'
TL2A2 = '段2a_外圧は毎四半期0が書かれる'
TL2B1 = '段2b_Modernizeの法の目標は技術の閾値で出し惜しまない'
TL2B2 = '段2b_制度と思想の行の技術の比は1'

# 変異＝(編集の一覧 [(ファイル, 旧, 新), ...]（None＝変異なし）, 絞り込み, 注, 予言＝赤になるはずの試験名の断片の一覧〔空＝緑のはず〕)
PRE = {
    'M0': (None, F_TECH, '変異なしの対照（焼き込みの前の木・移した試験が緑）', []),
    'Mu1': ([(DISC, '(spread && !(c.TechThresholdOff > 0)) || (!spread && !engOn)', '(spread && true) || (!spread && !engOn)')], F_TECH,
            '制度と思想の行が技術水準を読む（TechRatioOf の旧の枝を既定に戻す）', ['段2b_制度と思想の行の技術の比は', IA0, IA1]),
    'Mu2': ([(DISC, 'if (spread && c.TechThresholdOff > 0 && c.SpreadLiteracyFull.Value > 0', 'if (spread && false && c.SpreadLiteracyFull.Value > 0')], F_DE + '|' + F_IA,
            '制度と思想の行の識字の上書きを飛ばす', [T3, IA1]),
    'Mu12': ([(DISC, '(spread && !(c.TechThresholdOff > 0)) || (!spread && !engOn)', '(spread && true) || (!spread && !engOn)'),
              (DISC, 'if (spread && c.TechThresholdOff > 0 && c.SpreadLiteracyFull.Value > 0', 'if (spread && false && c.SpreadLiteracyFull.Value > 0')], F_TECH,
             '制度と思想の行が技術水準を読む（TechRatioOf の旧の枝と識字の上書きの両方を既定から外す）', [T3, SPR, IA0, IA1, '段2b_制度と思想の行の技術の比は']),
}
POST = {
    'M0': (None, F_TECH + '|' + F_NAVAL + '|' + F_NPP + '|' + F_POPSCAN, '変異なしの対照（最後の木）', []),
    'Mu3': ([(SELF, 'if (w.EducationLaw >= EducationLaw.Public && w.Treasury.Value >', 'if (w.EducationLaw >= EducationLaw.Public && w.TechLevel > 1.0 && w.Treasury.Value >')], F_TL,
            '義務教育の目標の技術の閾値を戻す', [TL2B1]),
    'Mu5': ([(SELF, '                RaiseWomenRights(p, WomenRightsLaw.Suffrage);   // ★統合法', '                if (w.TechLevel > 1.5) RaiseWomenRights(p, WomenRightsLaw.Suffrage);   // ★統合法')], F_TL,
            '女性の権利の技術の閾値を戻す（Property の行は Suffrage の行が無条件だと常に no-op＝(824) で消した）', [TL2B1]),
    'Mu6': ([(SELF, '                if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;   // ★(613)', '                if (w.TechLevel > 0.25 && p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;   // ★(613)')], F_TL,
            'Modernize の制限選挙の技術の閾値を戻す（参政権は他の書き手も Census を置く＝緑なら穴）', [TL2B1]),
    'Mu7': ([(AGENTS, '        if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;', '        if (w.TechLevel > 0.25 && p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;')], F_TL,
            'ModernizerAgent の制限選挙の技術の閾値を戻す', [TL2B1]),
    'Mu8': ([(SELF, 'double techGap = 0.0;', 'double techGap = Math.Max(0, w.WorldTechLevel - w.TechLevel);')], F_TL,
            '技術の遅れを姿勢の効用に入れる', [TL2A1]),
    'Mu9': ([(MNS, 'double gap = 0.0;', 'double gap = PoliticalQuantities.Clamp01((frontier - w.TechLevel) / Math.Max(1e-9, 1.5));')], F_TL,
            '技術の格差を外圧に入れる', [TL2A2]),
    'Mu10': ([(DISC, 'if (!spread && !engOn)', 'if (spread || !engOn)')], F_TECH,
             'TechRatioOf で制度と思想の行に技術水準の比を戻す', [TL2B2, IA0, IA1]),
    'Mu11': ([(DISC, 'if (spread && c.SpreadLiteracyFull.Value > 0', 'if (spread && false && c.SpreadLiteracyFull.Value > 0')], F_DE + '|' + F_IA,
             '制度と思想の行の識字の上書きを飛ばす', [T3, IA1]),
    'Mu12': ([(DISC, 'if (!spread && !engOn)', 'if (spread || !engOn)'),
              (DISC, 'if (spread && c.SpreadLiteracyFull.Value > 0', 'if (spread && false && c.SpreadLiteracyFull.Value > 0')], F_TECH,
             '制度と思想の行が技術水準を読む（旧の枝と識字の上書きの両方）', [T3, SPR, IA0, IA1, TL2B2]),
    'M1c': ([(SIM, '                    navalNat = Math.Min(navalNat, navNeedNat);\n', '                    // M1c: navalNat の必要額での切り捨てを外した\n')], F_NAVAL,
            '国の海軍の予算の必要額での切り捨てを外す', ['海軍の予算は必要額で切られ']),
    'M3a': ([(SIM, '        if (mainPop > 1e-9) { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }\n', '        { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }\n')], F_POPSCAN,
            '本土の州が無い国でも本土の和（0）を国平均に使う', ['全ての州が植民地格の国は国平均を全ての州で取る']),
    'M3b': ([(SIM, '        if (mainPop > 1e-9) { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }\n', '        if (mainPop > 1e-9) { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; } else { natPop = 0; }\n')], F_POPSCAN,
            '本土の州が無い国の退避の枝で国平均の分母を 0 にする', ['全ての州が植民地格の国は国平均を全ての州で取る']),
    'M7': ([(TERR, '        r.NavalBudget = 0; r.NavalOrder = null;', '        // M7: 州が持ち主を替えるときの海軍の予算と発注の戻しを外した')], F_NPP + '|FullyQualifiedName~TerritoryTests|FullyQualifiedName~NavalCedeFleetReturnTests',
           '州が持ち主を替えるときの海軍の予算と発注の戻しを外す（r2post では緑＝穴→試験を足した・足した後の r4 は赤のはず）', ['州が持ち主を替えるとき海軍の予算と発注は戻る']),
}

MUTS = PRE if group == 'PRE' else POST
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
