# (1081) c103dm：DM-D′ の門 I3（読み手 0）。読むだけ。python -X utf8 -I i3_readers.py <木>
#   コードの行（// より後と /// の注を除く）で数える。許可表は事前登録 §7 の I3 の物（変更の前に固定）。
import os, re, sys
root = sys.argv[1]
names = ['MegaprojectEnabled', 'MegaprojectStateFrac', 'MegaprojectStateCap', 'MegaprojectPrestige', 'MegaprojectCounterCyclical',
         'MegaprojectCrisisDrawdown', 'MegaprojectForeignShare', 'MegaprojectForeignMinRank', 'MegaprojectForeignDrawFrac',
         'MegaprojectForeignYield', 'FormationLegitimacyBoost', 'Monument', 'megaprojects.json',
         'monumentPaidPerMonth', 'megaCrisisPassCount', 'megaCrisisGateOpenCount', 'megaStartedCount', 'megaStartedUnmetCount',
         'megaStartedOldGateClosedCount', 'claimMonumentBuildPerMonth', 'claimMegaForeignYieldAtQuarterEnd', 'MegaForeignYieldQ', 'MegaCrisis']
# 〈Megaproject〉の語（型・静的クラス・列挙）は残す口の許可表だけ
ALLOW_MEGA = {
    'PriceOfBread.SimCore/Megaprojects.cs', 'PriceOfBread.SimCore/Diplomacy.cs', 'PriceOfBread.SimCore/Model.cs',
    'PriceOfBread.SimCore/Display/FiscalCatalog.cs', 'PriceOfBread.SimCore/MultiNationSim.cs', 'PriceOfBread.SimCore/EconomyStep.cs',
    'PriceOfBread.Tests/ConservationTests.cs', 'PriceOfBread.Tests/TechLevelRemovalTests.cs', 'PriceOfBread.Tests/TreasuryLedgerTests.cs',
    'PriceOfBread.Tests/DormantIdentityTests.cs',
}
ALLOW_MEGA_RX = {   # 許可表のファイルで許す行の模様（それ以外の行は読み手に数える）
    'PriceOfBread.SimCore/Megaprojects.cs': r'class Megaprojects|Megaproject mp',
    'PriceOfBread.SimCore/Diplomacy.cs': r'class Megaproject|List<Megaproject> Megaprojects',
    'PriceOfBread.SimCore/Model.cs': r'Subsidy, Megaproject, ColonialUpkeep',
    'PriceOfBread.SimCore/Display/FiscalCatalog.cs': r'TreasuryCat\.Megaproject',
    'PriceOfBread.SimCore/MultiNationSim.cs': r'Megaprojects\.RequirementMet\(w, cp\.Id\)',
    'PriceOfBread.SimCore/EconomyStep.cs': r'$^',
    'PriceOfBread.Tests/ConservationTests.cs': r'Megaprojects\.RequiresRowOf\(cp\.Id\)',
    'PriceOfBread.Tests/TechLevelRemovalTests.cs': r'Megaprojects\.RequiresRowOf\(',
    'PriceOfBread.Tests/TreasuryLedgerTests.cs': r'TreasuryCat\.Megaproject',
    'PriceOfBread.Tests/DormantIdentityTests.cs': r'"Megaproject[A-Za-z]+",',
}
HIST = ('scripts/instruments/ir505/mut.py', 'scripts/instruments/sm4_556/mut_sm4.py',   # 履歴の台本＝直さない（事前登録）
        # 〈結果の後の明確化〉紙 §1-2 の〈読む台本（履歴）〉の列名の読み手（閉じた線の台本・現役の読み手〔止める線・NG・mon・門〕ではない）
        'scripts/instruments/an_fiscal242.py', 'scripts/instruments/d289/an_d289.py', 'scripts/instruments/m617/rec617.py',
        'scripts/instruments/o0/an_o0.py', 'scripts/instruments/q286/an_q286.py', 'scripts/instruments/tl613/an_tl613.py',
        # 〈結果の後の明確化〉残る口を指す物（TreasuryCat の名 Megaproject・ledgerMegaprojectPerMonth・ファイル Megaprojects.cs は残る）
        'scripts/instruments/e7/an_e7.py', 'scripts/instruments/fsc/an_fsc_flow.py', 'scripts/instruments/ind4/step0.py', 'scripts/instruments/ir505/step0.py')
bad = []; hist = []
for top in ('PriceOfBread.SimCore', 'PriceOfBread.Tests', 'scripts/instruments'):
    for dp, dn, fn in os.walk(os.path.join(root, top)):
        dn[:] = [d for d in dn if d not in ('bin', 'obj')]
        for f in fn:
            if not f.endswith(('.cs', '.py', '.sh', '.tsv', '.csproj', '.json')): continue
            p = os.path.join(dp, f); rel = os.path.relpath(p, root).replace('\\', '/')
            for i, ln in enumerate(open(p, encoding='utf-8', errors='replace'), 1):
                code = ln
                if f.endswith('.cs'):
                    c = code.find('//'); code = code[:c] if c >= 0 else code
                elif f.endswith(('.py', '.sh')):
                    c = code.find('#'); code = code[:c] if c >= 0 else code
                hit = [n for n in names if n in code]
                if 'Megaproject' in code and not hit:
                    rx = ALLOW_MEGA_RX.get(rel)
                    if rx is None or not re.search(rx, code): hit = ['Megaproject']
                if hit:
                    if rel in HIST: hist.append((rel, i, hit))
                    elif rel == 'PriceOfBread.Tests/DormantIdentityTests.cs' and re.search(r'"(Megaproject|FormationLegitimacyBoost|Monument)', code): pass
                    else: bad.append((rel, i, hit, code.strip()[:100]))
print(f'I3 読み手（許可表の外）{len(bad)} 件・履歴の台本と残る口を指す台本 {len(hist)} 件（直さない）')
for b in bad[:40]: print('  ', b)
for h in hist: print('  履歴', h)
print('判定:', '緑' if not bad else '赤')
sys.exit(1 if bad else 0)
