import sys, subprocess
T = 'D:/PoB_work/c104fb_work/mut/'
M = {
 'M1': ('PriceOfBread.SimCore/Simulation.cs', 'if (leadChamber is not null)', 'if (leadChamber is not null && false)'),
 'M2': ('PriceOfBread.SimCore/Simulation.cs', 'producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "蒸気タービン"', 'if (false) producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "蒸気タービン"'),
 'M3': ('PriceOfBread.SimCore/Simulation.cs', '[new Input(Catalog.FarmLaborId, FarmLaborPerOutput), new Input(Catalog.MachineryId, coef.OilMachineryInput)]', '[new Input(Catalog.FarmLaborId, FarmLaborPerOutput)]'),
 'M4': ('PriceOfBread.SimCore/Simulation.cs', 'producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "堆肥"', 'if (false) producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "堆肥"'),
 'M5': ('PriceOfBread.SimCore/Simulation.cs', 'else if (sharedWorld && Catalog.IsScarceMineral(pr.GoodId, coef)) pr.Productivity = 0;', 'else if (false && sharedWorld && Catalog.IsScarceMineral(pr.GoodId, coef)) pr.Productivity = 0;'),
 'M6': ('PriceOfBread.SimCore/Simulation.cs', 'foreach (var col in n.InitialColonies)', 'foreach (var col in System.Linq.Enumerable.Take(n.InitialColonies, 0))'),
 'M7': ('PriceOfBread.SimCore/Simulation.cs', 'core = r.Culture == majority ? 1.0 : 0.0;', 'core = u0;'),
 'M8': ('PriceOfBread.SimCore/Simulation.cs', 'if (World.Regions.Count > 0) World.SetCapital(World.Regions[0]);', 'if (false) World.SetCapital(World.Regions[0]);'),
 'M9': ('PriceOfBread.SimCore/Simulation.cs', 'm.LastDemand += qty;', ';'),
 'M10': ('PriceOfBread.SimCore/Coefficients.cs', '        double a = FactoryBuildAfford(cpuNow, cpuRef);\n', '        cpuRef *= System.Math.Max(priceLevel, 1e-9);\n        double a = FactoryBuildAfford(cpuNow, cpuRef);\n'),
 'M11': ('PriceOfBread.SimCore/EconomyStep.cs', ': coef.ScaleMax;\n                catSpeed', ': 2.0;\n                catSpeed'),
}
name = sys.argv[1]
subprocess.run(['git', '-C', T, 'checkout', '-q', '--', '.'], check=True)
f, old, new = M[name]
d = open(T + f, 'rb').read(); o = old.encode()
assert d.count(o) == 1, (name, d.count(o))
open(T + f, 'wb').write(d.replace(o, new.encode()))
print('applied', name)
