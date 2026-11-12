import sys
m=sys.argv[1]
p='D:/PoB_work/wt_c102em/PriceOfBread.SimCore/EconomyStep.cs'
b=open('D:/PoB_work/c102em_work/EconomyStep.skip.cs','rb').read().decode('utf-8')
def rep(old,new,n=1):
    global b
    assert b.count(old)==n,(b.count(old),old)
    b=b.replace(old,new)
if m=='M0': rep('bool skipZeroPop = !insOn','bool skipZeroPop = false && !insOn')
elif m=='M1': rep('skipZeroPop && p.Size == 0.0','skipZeroPop && p.Size <= 1e-12',2)
elif m=='M2': rep('p.GoodNeed = 0; p.GoodDemand = 0; skippedZero++;','p.GoodNeed = 0; skippedZero++;')
elif m=='M3': rep('p.GoodNeed = 0; p.GoodDemand = 0; skippedZero++;','p.GoodDemand = 0; skippedZero++;')
elif m=='M5': rep('bool skipZeroPop = !insOn && !Instruments.On && insSpend is null && insEnt is null && fdd is null && !BudgetBindDiag.On;','bool skipZeroPop = true;')
elif m=='BASE': pass
else: raise SystemExit('?')
open(p,'wb').write(b.encode('utf-8'))
