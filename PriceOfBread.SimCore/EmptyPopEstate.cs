using System.Collections.Generic;
namespace PriceOfBread.SimCore;

internal static class EmptyPopEstate
{
    internal static void Step(World w) { Core(w); if (Instruments.On) Instruments.EstateAfter(w); } private static void Core(World w)
    {
        if (Instruments.On) Instruments.EstateDue(w);
        if (!(w.Coef.EmptyPopEstate > 0)) return;
        foreach (var r in w.Regions)
        {
            List<Pop>? payers = null; List<double>? held = null;
            double sum = 0, live = 0;
            foreach (var p in r.Pops)
            {
                if (p.Size > 1e-12) { live += p.Size; continue; }
                if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0) { double back = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (back > 0) { if (Instruments.On) Instruments.EstateRepay(true); Bank.Lend(w, p, -back); if (Instruments.On) Instruments.EstateRepay(false); } } double s = p.Savings.Value;
                if (s > 0) { (payers ??= new List<Pop>()).Add(p); (held ??= new List<double>()).Add(s); sum += s; }
            }
            if (payers is null || !(live > 1e-9)) continue;
            var heirs = new List<Pop>();
            foreach (var p in r.Pops) if (p.Size > 1e-12) heirs.Add(p);
            int big = 0;
            for (int i = 1; i < heirs.Count; i++) if (heirs[i].Size > heirs[big].Size) big = i;
            (heirs[big], heirs[^1]) = (heirs[^1], heirs[big]);
            World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held);
        }
    }
}
