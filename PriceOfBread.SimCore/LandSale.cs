using System.Collections.Generic;
namespace PriceOfBread.SimCore;

internal static class LandSale
{
    internal static void Resolve(World w, Coefficients c, List<Pop> pops, List<(int Pi, double V)> owed, double crownShare)
    {
        if (w.SerfEmancipation == ReformLevel.None || owed.Count == 0) return;
        double tau = w.LandTax, rate = w.GovBondRatePerYear.Value;
        var buyers = new List<int>(); var held = new List<Producer>();
        int oi = 0, baseIdx = 0;
        foreach (var r in w.Regions)
        {
            int end = baseIdx + r.Pops.Count;
            for (; oi < owed.Count && owed[oi].Pi < end; oi++)
            {
                int i = owed[oi].Pi - baseIdx;
                if (i < 0 || !ReferenceEquals(r.Pops[i], pops[owed[oi].Pi])) continue;
                Instruments.LandsaleSaleBegin(w, r, i);
                double moved = SellOne(w, c, r, i, owed[oi].V, tau, rate, crownShare, buyers, held);
                Instruments.LandsaleSaleEnd(w, r, i, moved);
            }
            baseIdx = end;
        }
    }

    static int SlotPop(Region r, Producer s, int k)
    {
        int j = EconomyStep.LandShareRecipient(r, s, k);   // 受け手の規則は 1 本（範囲の外の枠は持ち主へ寄せる）。それでも範囲の外なら -1
        return j >= 0 && j < r.Pops.Count ? j : -1;
    }

    static double SellOne(World w, Coefficients c, Region r, int i, double v, double tau, double rate, double crownShare, List<int> buyers, List<Producer> held)
    {
        buyers.Clear(); held.Clear();
        foreach (var s in r.Producers)
        {
            if (!s.IsRgo || Colonial.AbsenteeRgo(r, s)) continue;
            var ls = s.LandShares;
            bool holds = false;
            if (ls is null) holds = s.OwnerPopIndex == i;
            else for (int k = 0, nk = System.Math.Min(4, ls.Length); k < nk && !holds; k++) holds = ls[k] > 0 && EconomyStep.LandShareRecipient(r, s, k) == i;
            if (!holds) continue;
            held.Add(s);
            for (int k = 0; k < 4; k++) { int j = SlotPop(r, s, k); if (j >= 0 && j != i && !buyers.Contains(j)) buyers.Add(j); }
        }
        if (held.Count == 0 || buyers.Count == 0) return 0;
        double sumS = 0, sumSpb = 0;
        foreach (int j in buyers) { var pj = r.Pops[j]; double sj = System.Math.Max(0, pj.Savings.Value); sumS += sj; sumSpb += sj * Privileges.TaxAssessment(w, c, pj, crownShare); }
        if (!(sumS > 0)) return 0;
        var seller = r.Pops[i];
        double net = seller.LandRentPerMonth.Value - tau * Fiscal.LandTaxBase(c, seller).Value * (sumSpb / sumS);
        if (!(net > 0) || !(rate > 0)) return 0;
        double price = Calendar.PerMonthToPerYear(net) / rate;
        double phi = v / price;
        if (phi > 1) phi = 1;
        if (phi > sumS / price) phi = sumS / price;
        if (!(phi > 0)) return 0;
        double movedSum = 0, aSum = 0;
        var a = new double[buyers.Count];
        for (int b = 0; b < buyers.Count; b++)
        {
            var pj = r.Pops[buyers[b]];
            double sj = System.Math.Max(0, pj.Savings.Value);
            double amt = System.Math.Min(phi * (sj / sumS) * price, sj);
            if (!(amt > 0)) continue;
            double moved = World.Transfer((w, [pj]), (w, null, null), amt, TreasuryCat.OtherIn);
            movedSum += moved; a[b] = moved / price; aSum += a[b];
        }
        if (!(aSum > 0)) return movedSum;
        foreach (var s in held)
        {
            double eSum = 0;
            for (int b = 0; b < buyers.Count; b++) if (a[b] > 0 && TargetSlot(r, s, buyers[b]) >= 0) eSum += a[b];
            if (!(eSum > 0)) continue;
            if (s.LandShares is null) s.LandShares = [1.0, 0.0, 0.0, 0.0];
            var ls = s.LandShares;
            for (int k = 0, nk = System.Math.Min(4, ls.Length); k < nk; k++)
            {
                if (!(ls[k] > 0) || EconomyStep.LandShareRecipient(r, s, k) != i) continue;
                double sold = ls[k] * aSum, left = sold;
                ls[k] -= sold;
                int last = -1;
                for (int b = 0; b < buyers.Count; b++) if (a[b] > 0 && TargetSlot(r, s, buyers[b]) >= 0) last = b;
                for (int b = 0; b < buyers.Count; b++)
                {
                    if (!(a[b] > 0)) continue;
                    int kt = TargetSlot(r, s, buyers[b]);
                    if (kt < 0) continue;
                    double part = b == last ? left : sold * (a[b] / eSum);
                    ls[kt] += part; left -= part;
                }
            }
        }
        return movedSum;
    }

    static int TargetSlot(Region r, Producer s, int j)
    {
        for (int k = 0; k < 4; k++) if (SlotPop(r, s, k) == j) return k;
        return -1;
    }
}
