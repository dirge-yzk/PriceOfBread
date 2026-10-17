import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/MultiNationSim.cs'
t = open(p, 'rb').read().decode('utf-8')
def rep(old, new):
    global t
    assert t.count(old) == 1, old[:50]
    t = t.replace(old, new)
rep('        foreach (var sim in Nations) sim.StepDayOpen();\n', '        PT.Start(2); foreach (var sim in Nations) sim.StepDayOpen(); PT.Seg(2, 30);\n')
rep('        if (_coef.LuxuryIncomeElasticity > 0 && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0) DistributeWorldRealIncome();', '        PT.Seg(2, 31);\n        if (_coef.LuxuryIncomeElasticity > 0 && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0) DistributeWorldRealIncome();')
rep('        foreach (var sim in Nations) sim.World.FlushLog();', '        PT.Seg(2, 32);\n        foreach (var sim in Nations) sim.World.FlushLog(); PT.Seg(2, 33);')
rep('        return quarterDone;', '        PT.Seg(2, 34);\n        return quarterDone;')
open(p, 'wb').write(t.encode('utf-8'))
