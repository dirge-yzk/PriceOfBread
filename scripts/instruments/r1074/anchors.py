# (1206) 注：k3 の係数名（(1199) で消えた）を字面で読む＝(1199) 以後の木では動かない・旧の生でだけ動く（中身は変えていない）
"""(1074) 台帳の S23 の錨（file:line:名）が 7bb40c772 の木で行に名を含むかを確かめ、外れた物は同じファイルの宣言の行を探す。"""
import subprocess, re, sys
REPO = 'C:/Users/dirge/Documents/PriceOfBread'; C = '7bb40c772'
cache = {}
def lines(path):
    if path not in cache:
        p = subprocess.run(['git', '-C', REPO, 'show', f'{C}:PriceOfBread.SimCore/{path}'], capture_output=True)
        cache[path] = p.stdout.decode('utf-8', 'replace').split('\n') if p.returncode == 0 else None
    return cache[path]
def find(path, name):
    L = lines(path)
    if L is None: return None
    pat = re.compile(r'\b' + re.escape(name) + r'\b\s*(=|\{|;|\(|=>)')
    for i, l in enumerate(L):
        if pat.search(l) and not l.strip().startswith('//'): return i + 1
    for i, l in enumerate(L):
        if re.search(r'\b' + re.escape(name) + r'\b', l) and not l.strip().startswith('//'): return i + 1
    return None
rows = [l for l in open('D:/PoB_work/r1074_work/s23_rows.txt', encoding='utf-8') if l.strip().startswith('S23-H')]
out = []
for l in rows:
    cols = [c.strip() for c in l.split('|')]
    rid, anc = cols[0], cols[3]
    res = []
    for a in anc.split(';'):
        if a in ('—', ''): res.append('—'); continue
        f, ln, nm = a.split(':', 2)
        L = lines(f)
        ok = L is not None and int(ln) <= len(L) and re.search(r'\b' + re.escape(nm) + r'\b', L[int(ln) - 1])
        if ok: res.append(f'{f}:{ln}:{nm}')
        else:
            nl = find(f, nm)
            res.append(f'{f}:{nl}:{nm}' if nl else f'MISSING({f}:{ln}:{nm})')
    out.append(rid + '\t' + ';'.join(res))
# 追加の錨の候補
extra = [('Military.cs', 'EstablishmentOf'), ('Military.cs', 'EnvelopeAt'), ('Military.cs', 'StepSkill'), ('Military.cs', 'DrawFromPool'), ('Military.cs', 'MoveCohort'),
         ('Military.cs', 'NavalFuelStep'), ('Military.cs', 'RegionManpower'), ('Coefficients.cs', 'MilitaryWarMultFromContact'), ('Coefficients.cs', 'WarArmsDemandPerContact'),
         ('Coefficients.cs', 'GarrisonProcurement'), ('Coefficients.cs', 'NavalFuelPerGauge'), ('Coefficients.cs', 'ArmsWearRatePerMonth'), ('Coefficients.cs', 'MilitaryWearRatesPerMonth'),
         ('Coefficients.cs', 'NavalAttritionRatePerMonth'), ('Coefficients.cs', 'SkillWarGain'), ('Coefficients.cs', 'ConscriptionRatePerMonth'), ('Coefficients.cs', 'SoldierMigrationRatePerMonth'),
         ('Coefficients.cs', 'MilitaryQualityGoods'), ('Coefficients.cs', 'MilitaryTier3Goods'), ('Coefficients.cs', 'NavalCrewPerGauge'), ('Government.cs', 'UniversalConscriptionOpen'),
         ('Government.cs', 'MilitaryTotalTarget'), ('Government.cs', 'WarStageOpen'), ('SelfPlay.cs', 'SecurityStageTarget'), ('SelfPlay.cs', 'ConscriptionLowerTarget'), ('SelfPlay.cs', 'ArmyShareOrNaN'),
         ('ArmyShape.cs', 'Quality'), ('Diplomacy.cs', 'SyncPowers'), ('Diplomacy.cs', 'LandPower'), ('Diplomacy.cs', 'NavalPower'), ('PowerShares.cs', 'SoldierWelfare'), ('PowerShares.cs', 'LockedSpending'),
         ('Meritocracy.cs', 'JobIncome'), ('World.cs', 'ConscriptionPreWar'), ('World.cs', 'SecurityThreatArmy'), ('World.cs', 'NavySkill'), ('MultiNationSim.cs', 'StepSecurityThreat'),
         ('Simulation.cs', 'InitialArsenalMonths'), ('EconomyStep.cs', 'FleetGaugeDenominator'), ('EconomyStep.cs', 'MotorFuelRate'), ('EconomyStep.cs', 'AirFuelRate'), ('Discovery.cs', 'GoodGate'),
         ('Catalog.cs', 'Retirements'), ('Diplomacy.cs', 'AirControlPushWeight'), ('Diplomacy.cs', 'AirCasualtyWeight'), ('Diplomacy.cs', 'WarExhaustionAirRaid'), ('Diplomacy.cs', 'ChemExhaustionWeight'),
         ('EconomyStep.cs', 'MilitaryProcurePriceGateScale'), ('Simulation.cs', 'MilitaryWarMultFromContact'), ('Military.cs', 'PeacetimeConscriptGrievance'), ('Military.cs', 'GarrisonProcurement'),
         ('Military.cs', 'NationBuildMobilizationWeight'), ('Military.cs', 'ConscriptGrievanceRate'), ('Military.cs', 'ConscriptionFrontStep'), ('Government.cs', 'WartimeUpkeepFloor'), ('SelfPlay.cs', 'MobilizeOutmatchRatio'),
         ('Diplomacy.cs', 'NavalAttritionRatePerMonth'), ('EconomyStep.cs', 'ArmsWarWearScale'), ('ArmyShape.cs', 'MilitarismStrengthBonus'), ('ArmyShape.cs', 'UnityMilitaryQuality')]
out.append('--- 追加の錨')
for f, nm in extra:
    nl = find(f, nm); out.append(f'{f}:{nl}:{nm}' if nl else f'MISSING {f}:{nm}')
open('D:/PoB_work/r1074_work/out_anchors.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
