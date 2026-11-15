# (1152) c113lc の突然変異（事前登録 measurements/m1152_20261115_prereg_lc.txt §突然変異）。使い捨ての木の EconomyStep.cs・Transport.cs をバイトで書き換える。
#   python -X utf8 -I muts.py <木> <変異の名>   ＝置き換えが全て 1 か所ずつ当たったら 0・当たらなければ 2
import sys
T = sys.argv[1].rstrip('/'); MU = sys.argv[2]
E = T + '/PriceOfBread.SimCore/EconomyStep.cs'; TR = T + '/PriceOfBread.SimCore/Transport.cs'
OWN = b'(coef.RailLocoNeedsDiscovery > 0 && !Discovery.RailTransport(region.Discovered) ? 0.0 : locoGate)'   # 旗 RailLocoNeedsDiscovery＝0 の盤面には掛けない（初版は掛けた＝試験の対照の盤面まで変わり T1 が緑＝変異の誤り）
SET_NEW = b'!Discovery.RailTransport(coef.RailLocoGateWorld > 0 ? world?.WorldDiscovered ?? region.Discovered : region.Discovered)) locoGate = 0.0;'
SET_OLD = b'!Discovery.RailTransport(region.Discovered)) locoGate = 0.0;'
M1101 = b'double maintCostPerUnit = cP + railP + coef.MaintLocoRate * locoGate * locoP;'
M1119 = b'investDemand[coef.LocoGoodId] += maintenance * coef.MaintLocoRate * locoGate;'
S2085 = b'double locoGot = coef.ConsLoco * locoGate * construction * ration[coef.LocoGoodId];'
S2123 = b'double locoQ = maintenance * coef.MaintLocoRate * locoGate * ration[coef.LocoGoodId];'
def own(x): return x.replace(b'locoGate', OWN, 1)
TR16 = b'double railGate = Discovery.RailTransport(w.Discovered) ? 1.0 : 0.0;'
R = {
    'M1': [(E, SET_NEW, SET_OLD)],                                   # 自国の発見のまま（xUnit 用・成績表では F0 と同じ）
    'M3': [(E, S2085, own(S2085)), (E, S2123, own(S2123))],          # 需要は世界・決済は自国
    'M4': [(E, M1101, own(M1101)), (E, M1119, own(M1119)), (E, S2123, own(S2123))],   # 建設だけ世界・保守は自国
    'M5': [(TR, TR16, TR16.replace(b'(w.Discovered)', b'(w.WorldDiscovered ?? w.Discovered)'))],   # 政府の鉄道の買いも世界で読む
}
for p, old, new in R[MU]:
    b = open(p, 'rb').read()
    if b.count(old) != 1: print(MU, '当たらない', p, old[:70]); sys.exit(2)
    open(p, 'wb').write(b.replace(old, new))
print(MU, '適用', len(R[MU]))
