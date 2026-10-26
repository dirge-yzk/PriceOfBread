# (573) 線 sm5 の突然変異の台本：木の写し（<copy>）の上で 1 か所ずつ戻し、関係する試験が赤になることを確かめる。元の木は触らない。
# 使い方：python mut_sm5.py <copy> <artifacts-path> [番号...]   （copy は PriceOfBread.SimCore／Tests／Runner／App と goods.json を持つ木の写し）
import subprocess, sys, os
copy, art = sys.argv[1], sys.argv[2]
pick = set(int(x) for x in sys.argv[3:])
M = [
 ('M1 配りの第 2 段を希望（買い直し）に比例', 'PriceOfBread.SimCore/SlotCapacity.cs', 'double share = byExcess ? ExcessOf(s) / sumExcess', 'double share = byExcess ? s.SlotRepl / sumExcess'),
 ('M2 配りの第 1 段（買い直し）を飛ばす', 'PriceOfBread.SimCore/SlotCapacity.cs', 'double replTotal = sumRepl > 1e-12 ? Math.Min(dN, sumRepl) : 0.0;', 'double replTotal = 0.0;'),
 ('M3 買い直しの合計で頭打ちにしない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'double replTotal = sumRepl > 1e-12 ? Math.Min(dN, sumRepl) : 0.0;', 'double replTotal = sumRepl > 1e-12 ? sumRepl : 0.0;'),
 ('M4 超過の値打ちから 1 を引かない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'double ex = n * (rate - 1.0);', 'double ex = n * rate;'),
 ('M7 機械の rateK の手置き 2.0 を戻す', 'PriceOfBread.SimCore/SlotMachines.cs', 'double rateK = grossK > 0 ? grossK / capEff : 0.0;', 'double rateK = capKy > 0 ? grossK / capKy : (grossK > 0 ? 2.0 : 0.0);'),
 ('M8 機械の買い直しの床を外す', 'PriceOfBread.SimCore/SlotMachines.cs', 'double capEff = Math.Max(capKy, SlotCapacity.CapCostFloor);', 'double capEff = capKy;'),
 ('M9 置き直しの発動を u でなく 1 に', 'PriceOfBread.SimCore/SlotCapacity.cs', '!(a.A0 > u * a.Cap0)', '!(a.A0 > a.Cap0)'),
 ('M10 置き直しの倍率を自己消費なしの式に', 'PriceOfBread.SimCore/SlotCapacity.cs', 'double f = (a.A0 - a.BSelf + u * a.AMarg - u * a.Cap0) / (u * a.AMarg - a.BSelf);', 'double f = a.A0 / (u * a.Cap0);'),
 ('M17 解が無い財も置き直す（退路の式）', 'PriceOfBread.SimCore/SlotCapacity.cs', 'if (!(u * a.AMarg > a.BSelf)) continue;', 'if (!(u * a.AMarg > a.BSelf)) { fOf[g] = a.A0 / (u * a.Cap0); continue; }'),
 ('M18 f を州ごとに別の値で掛ける', 'PriceOfBread.SimCore/SlotCapacity.cs', 's.SlotsPerScale *= f;', 's.SlotsPerScale *= f * (1.0 + 0.001 * (r.Producers.IndexOf(s) % 7));'),
 ('M11 蒸気の割合の分母から機械の枠を引かない', 'PriceOfBread.SimCore/SlotMachines.cs', 'double tot = Math.Max(0.0, n - sumM) + em;', 'double tot = n + em;'),
 ('M12 採算の率をセーブに載せない', 'PriceOfBread.SimCore/Model.cs', '    public double SlotRate;', '    [System.NonSerialized] public double SlotRate;'),
 ('M13 供給の段が採算の率を書かない', 'PriceOfBread.SimCore/EconomyStep.cs', 's.SlotRepl = rj; s.SlotRate = rate;', 's.SlotRepl = rj;'),
 ('M14 供給の段が買い直しの希望を書かない', 'PriceOfBread.SimCore/EconomyStep.cs', 's.SlotRepl = rj; s.SlotRate = rate;', 's.SlotRate = rate;'),
 ('M16 倒産・眠りの月に採算の率を 0 に戻さない', 'PriceOfBread.SimCore/EconomyStep.cs', 'if (slotsOn) { var s0 = region.Producers[pi]; if (FactoryEquipment.Eligible(s0)) { s0.SlotRepl = 0; s0.SlotRate = 0; } }', ''),
 ('M15 株式会社の倍率が slotGrowMul で戻る', 'PriceOfBread.SimCore/EconomyStep.cs', 'desiredBundles = (slotSumRepl + slotSumGrow) * coef.SlotBundlesPerSlot;', 'double slotGrowMul = world!.JointStock ? coef.JointStockInvestBonus : 1.0; desiredBundles = (slotSumRepl + slotGrowMul * slotSumGrow) * coef.SlotBundlesPerSlot;'),
]
filt = 'FullyQualifiedName~SlotCapacityTests|FullyQualifiedName~SlotMachinesTests|FullyQualifiedName~SlotFiveTests|FullyQualifiedName~StateIndConstructionScaleTests'
res = []
for i, (name, rel, old, new) in enumerate(M, 1):
    if pick and i not in pick: continue
    path = os.path.join(copy, rel.replace('/', os.sep))
    orig = open(path, 'rb').read()
    text = orig.decode('utf-8')
    if text.count(old) != 1:
        res.append((name, 'ANCHOR?', text.count(old))); print(res[-1], flush=True); continue
    try:
        open(path, 'wb').write(text.replace(old, new).encode('utf-8'))
        p = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--filter', filt], cwd=copy, capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = p.stdout
        if 'エラー' in out and ' 0 エラー' not in out and '失敗!' not in out and '成功!' not in out: tag = 'BUILDFAIL'
        elif p.returncode != 0 and '失敗!' in out: tag = 'RED'
        else: tag = 'GREEN(生き残り)'
        res.append((name, tag)); print(res[-1], flush=True)
    finally:
        open(path, 'wb').write(orig)
print('DONE', len(res))
