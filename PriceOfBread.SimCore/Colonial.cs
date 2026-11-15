namespace PriceOfBread.SimCore;

public static class Colonial
{
    internal const double OilViableProductionFrac = 0.25;

    public static double OutputCurve(double dev, double half) => dev <= 0 ? 0 : dev / (dev + half);

    public static void Step(World w, Coefficients c)
    {
        int colonies = w.SelfPower?.Colonies ?? 0;
        var r = w.Region;
        if (colonies <= 0 || r.ColonialResources is null) { w.LastColonialUpkeepPerMonth = new MoneyPerMonth(0); w.ColonialShipBurden = 0; return; }

        bool suezOpen = w.Diplomacy?.ChokeOpenFor(Diplomacy.SuezId, w.SelfPower) ?? false;
        bool panamaOpen = w.Diplomacy?.ChokeOpenFor(Diplomacy.PanamaId, w.SelfPower) ?? false;
        double shipBurden = 0, quinineWant = 0;
        double colUnitPop = w.Diplomacy is { } dipM && dipM.ColonyUnitPop > 1e-9 ? dipM.ColonyUnitPop : 1.0;
        void AccumUpkeep(double clat, double clon, double popWeight, bool malarial)
        {
            double km = (Math.Abs(clat) < 1e-9 && Math.Abs(clon) < 1e-9) ? 0 : Geo.SeaDistanceKm(w.Latitude, w.Longitude, clat, clon, suezOpen, panamaOpen);
            double massDist = km / Diplomacy.DistanceNormKm * Math.Max(0, popWeight);
            shipBurden += c.ColonyShipOccupyPerDist * massDist;
            if (malarial) quinineWant += c.MalariaQuininePerColony;
        }
        foreach (var cr in w.Regions)
        {
            if (cr.Tier != RegionTier.Colonial) continue;
            var ccr = Places.Coord(cr.Name);
            double pw = cr.SumPopulation() / colUnitPop;
            AccumUpkeep(ccr.Lat, ccr.Lon, pw, cr.Malarial);
        }
        const double era = 1.0;   // 時代係数は恒等 1（Coefficients.EraFactor は 1.0 を返す＝係数 EraColonyCoal は (1154) で削除）
        double steam = w.ShipSteamShare.Value;
        double steamMult = c.ShipTier2CapMult * Discovery.GoodGate(r, c.ShipTier2GoodId) / era;
        if (steamMult > 1.0) shipBurden *= (1.0 - steam) + steam / steamMult;
        w.ColonialShipBurden = shipBurden;
        w.LastColonialUpkeepPerMonth = new MoneyPerMonth(0);
        double coalWant = c.ShipFuelRate * era * steam * OccupiedShipCapacity(w);
        var (fuelG, fuelWant) = ChooseFuelGood(r, c, coalWant);
        BuyUpkeepGood(w, r, fuelG, fuelWant);
        const int quinineG = Catalog.QuinineId;
        var qnPre = Instruments.On ? Instruments.ColqnBefore(w, r) : default; double qnGot = quinineWant > 1e-9 ? BuyUpkeepGood(w, r, quinineG, quinineWant) : 0;
        if (qnPre.Ok) Instruments.ColqnAfter(w, r, qnPre, qnGot);   // (1153) 計器 colqn_s（読むだけ・行の数を変えない・Instruments.On は 1 回だけ読む）
        if (c.ColonialUnrestHomeWeight > 1e-9)
        {
            double colMilSum = 0, colPopSum = 0;
            foreach (var cr in w.Regions)
                {
                    if (cr.Tier != RegionTier.Colonial) continue;
                    var (mn, md) = Politics.RegionMilitancy(cr);
                    if (md > 1e-9) { colMilSum += mn; colPopSum += md; }
                }
            MechanismProbe.Eval(MechanismProbe.Site.AntiImperialOpinion);
            if (colPopSum > 1e-9)
            {
                double homePressure = c.ColonialUnrestHomeWeight * (colMilSum / colPopSum) * Math.Min(1.0, colonies / Math.Max(1.0, c.ColoniesForFullColonial));
                if (homePressure > 1e-9) MechanismProbe.Hit(MechanismProbe.Site.AntiImperialOpinion);
                if (homePressure > 1e-9)
                    foreach (var pop in w.AllPops()) if (Politics.IsMassPop(pop)) pop.Militancy = Math.Min(1, pop.Militancy + homePressure);
            }
        }

    }

    internal static double OccupiedShipCapacity(World w)
    {
        double b = w.ColonialShipBurden;
        return b > 0 ? w.ShipCapacity * b / (1.0 + b) : 0.0;
    }

    internal static bool AbsenteeRgo(Region r, Producer s)
        => r.Tier == RegionTier.Colonial && s.IsRgo && ExportCrop(s.GoodId) && Array.IndexOf(r.Resources, s.GoodId) >= 0;

    internal static bool ExportCrop(int goodId)
        => goodId >= 0 && goodId < Catalog.Goods.Length && !(Catalog.Goods[goodId].Tier == GoodTier.Staple || Catalog.Goods[goodId].GrainChain);

    internal static Pop? AbsenteeOwner(World w, Region r)
    {
        var cap = w.Region;
        if (ReferenceEquals(cap, r) || cap.InvestorPopIndex < 0 || cap.InvestorPopIndex >= cap.Pops.Count) return null;
        return cap.Pops[cap.InvestorPopIndex];
    }

    internal static void RemitAbsenteeRent(World w, Region r, double[] net)
    {
        var owner = AbsenteeOwner(w, r);
        for (int k = 0; k < r.Pops.Count && k < net.Length; k++)
        {
            double a = net[k]; net[k] = 0;
            if (owner is null || !(Math.Abs(a) > 0)) continue;
            var local = r.Pops[k];
            var (payer, payee) = a > 0 ? (local, owner) : (owner, local);
            double moved = World.Transfer((w, [payer]), (w, [payee], [1.0]), Math.Abs(a), income: true);
            payer.EarnedPerMonth -= new MoneyPerMonth(moved);
            if (a > 0)
            {
                double land = Math.Min(moved, Math.Max(0, local.LandRentPerMonth.Value));
                local.LandRentPerMonth -= new MoneyPerMonth(land); owner.LandRentPerMonth += new MoneyPerMonth(land);
            }
        }
    }

    internal static (int GoodId, double Want) ChooseFuelGood(Region r, Coefficients c, double coalWant)
    {
        const int oilG = Catalog.CrudeOilId, coalG = Catalog.CoalId;
        double oilNeed = c.ColonyFuelOilEnergyRatio > 1e-9 ? coalWant / c.ColonyFuelOilEnergyRatio : 0;
        bool oilViable = c.ColonyFuelOilEnergyRatio > 1e-9 && oilG < r.Markets.Length
                      && r.Markets[oilG].LastProduced > OilViableProductionFrac * oilNeed;
        if (oilViable && coalG < r.Markets.Length
            && Math.Max(r.Markets[oilG].Price, 1e-9) / c.ColonyFuelOilEnergyRatio < Math.Max(r.Markets[coalG].Price, 1e-9))
            return (oilG, oilNeed);
        return (coalG, coalWant);
    }

    internal static double BuyUpkeepGood(World w, Region r, int goodId, double want, TreasuryCat cat = TreasuryCat.ColonialUpkeep)
    {
        if (!(want > 1e-9) || goodId >= r.Markets.Length) return 0;
        want *= EconomyStep.StatePriceAfford(r, w.Coef, goodId);
        if (!(want > 1e-9)) return 0;
        if (cat == TreasuryCat.ColonialUpkeep) w.LastColonialUpkeepPerMonth += new MoneyPerMonth(want * Math.Max(r.Markets[goodId].Price, 1e-9));
        want *= w.FiscalFundingRatio.Value;
        if (!(want > 1e-9)) return 0;
        return InternationalMarket.Procure(w, r, goodId, want, null, cat);
    }
}
