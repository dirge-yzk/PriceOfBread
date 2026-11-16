# (1175) c116cl 使い捨ての突然変異：python -X utf8 -I mutate.py <apply|revert> <M2|M3|M4|M5> 
# バイトで読み書きする。revert は git checkout で戻す（呼び手が行う）。
import sys
M={
 'M2':('PriceOfBread.SimCore/MarketFlow.cs','double demandRaw = totalDemand + m.LastExport;','double demandRaw = totalDemand;'),
 'M3a':('PriceOfBread.SimCore/MarketFlow.cs','double p1 = want < wrp\n','double p1 = false\n'),
 'M5':('PriceOfBread.SimCore/Simulation.cs','if (insOn) Instruments.MintregMonth(World);','Instruments.MintregMonth(World);'),
}
k=sys.argv[1]; p,o,n=M[k]
b=open(p,'rb').read().decode()
assert b.count(o)==1,(k,b.count(o))
open(p,'wb').write(b.replace(o,n).encode())
print('mutated',k,p)
