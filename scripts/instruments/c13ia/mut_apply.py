# (811) c13ia 突然変異：焼き込み後のソースに当てる（使い捨ての木で）。使い方: python mut_apply.py <木> <M1..M7>
# 置換は 1 回だけ一致を要求する。
import sys, os
tree, m = sys.argv[1], sys.argv[2]
SIM = 'PriceOfBread.SimCore/Simulation.cs'
MUT = {
 'M1': (SIM,   # 軍需の上限の必要額での切り捨てを外す
        '                    milCap = Math.Min(milCap, EconomyStep.MilitaryProcurementNeed(r, World.Coef));\n',
        '                    // M1: 切り捨てを外した\n'),
 'M2': (SIM,   # 国平均から植民地を外さない
        'if (r.Tier != RegionTier.Colonial) { mainSav += rSav;',
        'if (true) { mainSav += rSav;'),
 'M3': (SIM,   # 鋳鉄の機械を足さない
        'if (machineDefault is not null)   // 機械を作らない世界',
        'if (false)   // 機械を作らない世界'),
 'M4': (SIM,   # Religious を国庫の財源から外す
        'World.EducationLaw == EducationLaw.Public || World.EducationLaw == EducationLaw.Religious ? World.Coef.PublicEduFundingShare.Value',
        'World.EducationLaw == EducationLaw.Public ? World.Coef.PublicEduFundingShare.Value'),
 'M5': (SIM,   # 冷蔵の加工肉を足さない
        'if (saltCured is not null)   // 加工肉を作らない世界',
        'if (false)   // 加工肉を作らない世界'),
 'M6': (SIM,   # 石油ケロシンを足さない
        'if (keroDefault is not null)   // 灯油を作らない世界',
        'if (false)   // 灯油を作らない世界'),
 'M7': (SIM,   # 家内工業の行を足さない（先頭で戻る）
        '        int peasantIdx = -1;\n',
        '        return;\n        int peasantIdx = -1;\n'),
}
rel, old, new = MUT[m]
p = os.path.join(tree, rel)
b = open(p, 'rb').read()
o, n = old.encode('utf-8'), new.encode('utf-8')
if b'\r\n' in b:
    o, n = o.replace(b'\n', b'\r\n'), n.replace(b'\n', b'\r\n')
c = b.count(o)
assert c == 1, f'{m}: {rel} で {c} 件'
open(p, 'wb').write(b.replace(o, n))
print(f'{m}: applied to {rel}')
