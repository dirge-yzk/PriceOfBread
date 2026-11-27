# (1413) c151rb 突然変異：使い捨ての木（wt_c151rb_mut）に 1 本ずつ当てて戻す。使い方：python mut.py <μ名> <木>
import sys
sys.path.insert(0, 'D:/PoB_work/c151rb_work')
from ed import edit
M = {
 'mu1': ('PriceOfBread.SimCore/Diplomacy.cs',
   [("                if (p.Nation is not null && Suzerain(p) is null)   // (1413)",
     "                if (p.Nation is not null && Suzerain(p) is null && new DeterministicRng((ulong)(p.Military * 1e6) ^ 0x5DEECE66DUL).NextDouble() < 0.12)   // (1413)")]),
 'mu2': ('PriceOfBread.SimCore/Factions.cs',
   [("        var observed = FactionSnapshot.ComputeClusters(w, countCost: true);",
     "        w.FactionRng.NextULong();\n        var observed = FactionSnapshot.ComputeClusters(w, countCost: true);")]),
 'mu3': ('PriceOfBread.SimCore/EconomyStep.cs',
   [("        return region.HarvestFactor * region.WeatherBandFactor * regionLandFactor;",
     "        return region.HarvestFactor * region.WeatherBandFactor * 0.9 * regionLandFactor;")]),
 'mu4': ('PriceOfBread.SimCore/SelfPlay.cs',
   [("            if (fiscalStress > 0) stateMonopTier = Math.Max(stateMonopTier, curTier + 1);\n",
     "            if (fiscalStress > 0 || w.LastInterestPerMonth.Value > 0) stateMonopTier = Math.Max(stateMonopTier, curTier + 1);\n")]),
}
f, pairs = M[sys.argv[1]]
edit(sys.argv[2] + '/' + f, pairs)
print('applied', sys.argv[1])
