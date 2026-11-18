import sys
root,which=sys.argv[1],sys.argv[2]
M={'m4':('PriceOfBread.SimCore/ProductionDiag.cs','total += pb[c]; }','total += pb[c] * 1.001; }'),
 'm5':('PriceOfBread.SimCore/EconomyStep.cs','double scaleTarget = Math.Clamp(1.0 + margin +','double scaleTarget = Math.Clamp(1.0 + Math.Abs(margin) +'),
 'm6':('PriceOfBread.SimCore/EconomyStep.cs','absentee[s.OwnerPopIndex] += rent;','absentee[s.OwnerPopIndex] += 0;')}[which]
p=root+'/'+M[0]; b=open(p,'rb').read().decode('utf8'); assert b.count(M[1])==1,M
open(p,'wb').write(b.replace(M[1],M[2]).encode('utf8'))
