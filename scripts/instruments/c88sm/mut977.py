# (977) c88sm 突然変異：python mut977.py <M1..M9> で作業ツリーのファイルをバイトで書き換える（戻すのは git checkout）。
import sys
M = {
 'M1': ('PriceOfBread.SimCore/World.cs', 'if (_moTrend <= 1e-9 && !(c.MoTrendInitNeedsGdp > 0 && !(gdp > 1e-9))) { _moTrend = ratio;', 'if (_moTrend <= 1e-9) { if (!(c.MoTrendInitNeedsGdp > 0 && !(gdp > 1e-9))) _moTrend = ratio;'),
 'M2': ('PriceOfBread.SimCore/Instruments.cs', 'if (early || !(w.MoTrendForInstruments > 1e-9))', 'if (early)'),
 'M3': ('PriceOfBread.SimCore/Instruments.cs', 'early ? "init" : "defer"', '"init"'),
 'M4': ('PriceOfBread.SimCore/InstrumentsBasisx.cs', 'F(sn ? a.SFund : nan), F(sn ? a.SDebt : nan)', 'F(sn ? a.SDebt : nan), F(sn ? a.SFund : nan)'),
 'M5': ('PriceOfBread.SimCore/InstrumentsBasisx.cs', 'w.GovDebt.Value / (w.NominalGdpPerMonth.Value * 12.0)', 'w.GovDebt.Value / (w.NominalGdpPerMonth.Value * 1.0)'),
 'M6': ('PriceOfBread.SimCore/InstrumentsBasisx.cs', 'a.SFund = rate;', 'a.SFund = 1.0;'),
 'M8': ('PriceOfBread.Tests/SecessionLaborLawInstrumentTests.cs', 'foreach (var n in s.Nations) foreach (var rg in n.World.Regions) if (rg.Tier == RegionTier.Colonial) rg.Dominance = 0.5;', 'foreach (var n in s.Nations.Take(1)) { var rg1 = n.World.Regions.FirstOrDefault(x => x.Tier == RegionTier.Colonial); if (rg1 is not null) rg1.Dominance = 0.5; }'),
 'M9': ('PriceOfBread.Tests/TransportInstrumentTests.cs', 'if (q != 5 && q != 13) return;', 'if (q != 5) return;'),
 'G1': ('PriceOfBread.SimCore/Coefficients.cs', 'public double MilitaryWarMult = 9.0;', 'public double MilitaryWarMult = 9.5;'),
}
f, a, n = M[sys.argv[1]]
b = open(f, 'rb').read(); a = a.encode(); n = n.encode()
assert b.count(a) == 1, (sys.argv[1], b.count(a))
open(f, 'wb').write(b.replace(a, n))
