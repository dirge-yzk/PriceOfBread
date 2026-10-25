import sys
which=sys.argv[1]
p='D:/PoB_work/wt_rs2/PriceOfBread.SimCore/PolicyPreferences.cs'
b=open(p,'rb').read().decode('utf-8')
expr='(w.Coef.LordOppositionByRentLoss > 0 ? LordIncomeLoss(w, a, e.LordRentToIncome) : e.Wealth[0])'
assert b.count(expr)==2
if which=='1':   # 旗1でも W0 を読む（農奴解放）
    b=b.replace(expr,'e.Wealth[0]',1)
elif which=='2': # 慣習を常に 1.0
    old='return double.IsPositiveInfinity(c) ? 0.0 : c;'; assert old in b
    b=b.replace(old,'return 1.0;')
elif which=='3': # 土地改革の項に旗を掛けない（W0 のまま）
    i=b.index(expr); j=b.index(expr,i+1)
    b=b[:j]+'e.Wealth[0]'+b[j+len(expr):]
elif which=='4': # 次の一段＝今の一段
    old='Simulation.RentCap(seNext, c.SerfEmancipationRentCap.Value)));\n    }'; assert old in b
    b=b.replace(old,'Simulation.RentCap(seNext, c.SerfEmancipationRentCap.Value)));\n        next = now;\n    }')
elif which=='5': # 地代÷所得を常に 1
    old='income > 1e-9 ? rent / income : 0.0'; assert old in b
    b=b.replace(old,'income > 1e-9 ? 1.0 : 0.0')
elif which=='6': # 分子の取り違え（次−今）
    old='(now - next) / now * lordRentToIncome'; assert old in b
    b=b.replace(old,'(next - now) / now * lordRentToIncome')
else: raise SystemExit('bad')
open(p,'wb').write(b.encode('utf-8'))
