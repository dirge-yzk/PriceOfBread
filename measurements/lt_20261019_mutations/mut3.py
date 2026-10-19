p='D:/PoB_work/lt/mut/PriceOfBread.SimCore/SelfPlay.cs'
b=open(p,'rb').read().decode('utf-8')
def rep(a,c):
    global b
    assert b.count(a)==1,a
    b=b.replace(a,c)
# M5 反トラスト（旗2）が工業化度を読む
rep('        double u = HomeUrbanShare(w);\n        if (u > AntitrustUrbanLine) return u > TrustbustUrbanLine ? AntitrustLaw.Trustbust : AntitrustLaw.CartelBan;','        double u = w.MeanIndustrialization / 5.0;\n        if (u > AntitrustUrbanLine) return u > TrustbustUrbanLine ? AntitrustLaw.Trustbust : AntitrustLaw.CartelBan;')
# M9 金の願い（旗1）が工業化度も読む
rep('PoliticalQuantities.AvgLiteracy(w, coreOnly: true) > GoldLiteracyLine;','PoliticalQuantities.AvgLiteracy(w, coreOnly: true) > GoldLiteracyLine || w.MeanIndustrialization > w.Coef.GoldStandardIndThreshold;')
# M10 反トラストの段を 2 → 3
rep('w.Coef.AiLawGateFromBoard >= 2.0 ? AntitrustTargetFromBoard(w)','w.Coef.AiLawGateFromBoard >= 3.0 ? AntitrustTargetFromBoard(w)')
open(p,'wb').write(b.encode('utf-8'))
