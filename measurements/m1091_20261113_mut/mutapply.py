import sys
m = sys.argv[1]
BASE = 'D:/PoB_work/c104si_work/mut/PriceOfBread.SimCore/'
def edit(f, old, new):
    p = BASE + f
    b = open(p, 'rb').read().decode('utf-8')
    assert b.count(old) == 1, (f, b.count(old), old)
    open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
if m == 'M0': pass
elif m == 'M1': edit('InstrumentsRulePower.cs', 'RpR(alloc > 0 ? alloc * cd.rho : 0)', 'RpR(alloc > 0 ? alloc * cd.rho * cd.rho : 0)')
elif m == 'M2': edit('InstrumentsRulePower.cs', 'RpR(cls.Owner), RpR(cls.Ungov)', 'cls.Owner.ToString("G6", System.Globalization.CultureInfo.InvariantCulture), cls.Ungov.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)')
elif m == 'M3': edit('InstrumentsMineCap.cs', 'double price = r.Markets[g].Price;', 'double price = 0; int cnt = 0; foreach (var r2 in w.Regions) if (g < r2.Markets.Length) { price += r2.Markets[g].Price; cnt++; } price /= System.Math.Max(1, cnt);')
else: sys.exit(1)
