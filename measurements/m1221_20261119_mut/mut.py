import sys
root=sys.argv[1]
p=root+'/PriceOfBread.SimCore/ProductionDiag.cs'
b=open(p,'rb').read().decode('utf8')
for a,c in [('RefPrice * PriceLevel * SupplyDemand;','RefPrice * PriceLevel * SupplyDemand * 1.001;'),('MaterialCost + LaborCost + OwnCost;','MaterialCost + LaborCost + OwnCost + 0.5;'),('ProfitFactor * InputFill * InventoryAdjust * Inertia;','ProfitFactor * InputFill * InventoryAdjust * Inertia * 1.001;')]:
    assert b.count(a)==1,a
    b=b.replace(a,c)
open(p,'wb').write(b.encode('utf8'))
