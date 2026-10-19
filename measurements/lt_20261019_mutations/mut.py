import re
p='D:/PoB_work/lt/mut/PriceOfBread.SimCore/SelfPlay.cs'
b=open(p,'rb').read().decode('utf-8')
def rep(a,c):
    global b
    assert b.count(a)==1,a
    b=b.replace(a,c)
# M1 識字の線 0.6 -> 0.4
rep('internal const double GoldLiteracyLine = 0.6;','internal const double GoldLiteracyLine = 0.4;')
# M2 coreOnly を外す
rep('PoliticalQuantities.AvgLiteracy(w, coreOnly: true) > GoldLiteracyLine','PoliticalQuantities.AvgLiteracy(w, coreOnly: false) > GoldLiteracyLine')
# M4 都市の線の入れ替え
rep('internal const double AntitrustUrbanLine = 1.0 / 3.0;','internal const double AntitrustUrbanLine = 0.5;')
rep('internal const double TrustbustUrbanLine = 0.5;','internal const double TrustbustUrbanLine = 1.0 / 3.0;')
# M6 植民地を数える
rep('if (r.State != RegionState.Active || r.Tier == RegionTier.Colonial) continue;\n            total += r.SumPopulation(); urban += r.SumUrbanPopulation();','if (r.State != RegionState.Active) continue;\n            total += r.SumPopulation(); urban += r.SumUrbanPopulation();')
# M7 ゲートを無視
rep('if (!(w.Coef.GuildLawGate > 0)) return null;\n        double u = HomeUrbanShare(w);','double u = HomeUrbanShare(w);')
open(p,'wb').write(b.encode('utf-8'))
