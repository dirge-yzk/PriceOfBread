# (1130) c110lo の突然変異（事前登録 measurements/m1130_20261114_prereg_lo.txt §突然変異）。使い捨ての木の EconomyStep.cs をバイトで書き換える。
#   python -X utf8 -I muts.py <木> <変異の名>   ＝置き換えが全て 1 か所ずつ当たったら 0・当たらなければ 2
import sys
T = sys.argv[1].rstrip('/'); MU = sys.argv[2]
P = T + '/PriceOfBread.SimCore/EconomyStep.cs'
SET = b'if (coef.RailLocoNeedsDiscovery > 0 && !Discovery.RailTransport(region.Discovered)) locoGate = 0.0;'
B1049 = b'+ coef.ConsLoco * locoGate * Math.Max(region.Markets[coef.LocoGoodId].Price, Eps);'
B1095 = b'investDemand[coef.LocoGoodId] += coef.ConsLoco * locoGate * construction;'
M1101 = b'double maintCostPerUnit = cP + railP + coef.MaintLocoRate * locoGate * locoP;'
M1119 = b'investDemand[coef.LocoGoodId] += maintenance * coef.MaintLocoRate * locoGate;'
S2085 = b'double locoGot = coef.ConsLoco * locoGate * construction * ration[coef.LocoGoodId];'
S2123 = b'double locoQ = maintenance * coef.MaintLocoRate * locoGate * ration[coef.LocoGoodId];'
def ungate(x): return x.replace(b' * locoGate', b'')
R = {
    'M0': [],   # 対照＝書き換えなし
    'M1': [(SET, SET.replace(b'&& !Discovery', b'&& Discovery'))],
    'M2': [(M1101, ungate(M1101)), (M1119, ungate(M1119)), (S2123, ungate(S2123))],   # 建設の 3 か所だけに掛ける
    'M3': [(SET, SET.replace(b'locoGate = 0.0;', b'locoGate = 1.0;'))],
    'M4': [(B1049, ungate(B1049)), (B1095, ungate(B1095)), (M1119, ungate(M1119)), (S2085, ungate(S2085)), (S2123, ungate(S2123))],   # :1101 だけ
    'M5': [(b'buildCostPerUnit += coef.ConsRail * Math.Max(', b'buildCostPerUnit += coef.ConsRail * locoGate * Math.Max('),
           (b'investDemand[coef.RailGoodId] += coef.ConsRail * construction;', b'investDemand[coef.RailGoodId] += coef.ConsRail * locoGate * construction;'),
           (b'double maintCostPerUnit = cP + railP + ', b'double maintCostPerUnit = cP + railP * locoGate + '),
           (b'investDemand[coef.RailGoodId] += maintenance;', b'investDemand[coef.RailGoodId] += maintenance * locoGate;'),
           (b'double railGot = coef.ConsRail * construction * ration', b'double railGot = coef.ConsRail * locoGate * construction * ration'),
           (b'double railQ = maintenance                    * ration', b'double railQ = maintenance * locoGate * ration')],   # 案 A
    'M6': [(S2085, ungate(S2085)), (S2123, ungate(S2123))],   # 需要だけ・決済に掛けない
}
b = open(P, 'rb').read()
for old, new in R[MU]:
    if b.count(old) != 1: print(MU, '当たらない', old[:70]); sys.exit(2)
    b = b.replace(old, new)
open(P, 'wb').write(b)
print(MU, '適用', len(R[MU]))
