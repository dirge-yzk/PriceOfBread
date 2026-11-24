namespace PriceOfBread.SimCore;

public sealed partial class InternationalMarket
{
    private readonly double[] _price;
    private readonly double[] _volume;

    public IReadOnlyList<double> Price => _price;
    public IReadOnlyList<double> Volume => _volume;

    private const double PriceBandFloorMul = 0.1;
    private const double PriceBandCeilMul = 10.0;

    public InternationalMarket(int goodCount)
    {
        _price = new double[goodCount];
        _volume = new double[goodCount];
        for (int i = 0; i < goodCount; i++)
            _price[i] = i < Catalog.RefPrices.Length ? Catalog.RefPrices[i] : 1.0;
    }


    public void Step(IReadOnlyList<Simulation> nations, Coefficients c, bool firstClearingOfQuarter = true)
    {
        StepBilateral(nations, c, firstClearingOfQuarter);
    }


    internal static double PaySales((World? W, List<Pop>? Pops) from, Region? r, int goodId, double amount, World w, TreasuryCat cat = TreasuryCat.OtherIn, bool minted = false, [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)
    {
        if (!(amount > 0)) return 0;
        var coef = w.Coef;
        int nr = r is null ? w.Regions.Count : 1;
        static bool Sells(Producer pr, Region rr, int g) => pr.GoodId == g && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < rr.Pops.Count && pr.LaborPopIndex >= 0 && pr.LaborPopIndex < rr.Pops.Count;
        double totalOut = 0;
        for (int ri = 0; ri < nr; ri++)
        {
            var rr = r ?? w.Regions[ri];
            foreach (var pr in rr.Producers)
                if (Sells(pr, rr, goodId)) totalOut += Math.Max(0, pr.Output);
        }
        var to = new List<Pop>(); List<double>? wt = null;
        double stateCut = 0;
        if (totalOut > 1e-9)
        {
            wt = [];
            for (int ri = 0; ri < nr; ri++)
            {
                var rr = r ?? w.Regions[ri];
                    foreach (var pr in rr.Producers)
                {
                    if (!Sells(pr, rr, goodId)) continue;
                    double amt = amount * (Math.Max(0, pr.Output) / totalOut);
                    if (!(amt > 0)) continue;
                    bool insOn = Instruments.On;   // (1228) 計器の旗の読みを 1 本に（OnDoubleReadScanTests の表の 4 のまま）
                    if (coef.UnionBargaining > 0 && insOn) Instruments.SbExport(w, amt, pr.BuysLabor && !pr.IsRgo);
                    double rent = EconomyStep.RentShareOf(pr, rr, coef) * amt;
                    double wages = amt - rent;
                    // (1228) 記帳：代金を生産者の帳へ（売上に amt・費用に賃金の分＝損益の増えは地代〔国の取り分を含む〕）。お金の移しは下のまま。
                    double revBefore = pr.LastRevenue, costBefore = pr.LastCost;
                    if (coef.PaySalesToProducerBook > 0 && wages >= 0) { pr.LastRevenue += amt; pr.LastCost += wages; }
                    if (insOn) Instruments.PaysaleAlloc(w, pr, amt, wages, revBefore, costBefore, minted, from.W is null && from.Pops is null);
                    double sc =rent * World.StateProfitShare(pr, rr, coef);
                    if (sc > 0) { stateCut += sc; rent -= sc; }
                    if (rent > 0)
                    {
                        if (Instruments.On) Instruments.LandshareExp(w, rr, pr, rent, sc);
                        EconomyStep.AddRentRecipients(to, wt, w, rr, pr, rent);
                    }
                    if (wages > 0) { to.Add(rr.Pops[pr.LaborPopIndex]); wt.Add(wages); }
                }
            }
        }
        if (to.Count == 0)
        {
            if (r is null) return 0;
            stateCut = 0; wt = null;
            if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count)
            {
                bool merchantEmpty = !(r.Pops[r.MerchantPopIndex].Size > 1e-9);
                bool clearing = from.W is null && from.Pops is null;
                if (Instruments.On && merchantEmpty) PayDiag.CountEmptyMerchant(w, clearing, from.Pops is not null, minted, amount);
                if (!merchantEmpty || !clearing || minted) to.Add(r.Pops[r.MerchantPopIndex]);
                else to.AddRange(r.Pops);
            }
            else to.AddRange(r.Pops);
        }
        double moved = 0;
        if (stateCut > 0)
            moved += World.Transfer(from, (w, null, null), stateCut, from.Pops is null && ReferenceEquals(from.W, w) ? cat : TreasuryCat.StateFactory, toCat: TreasuryCat.StateFactory, outside: minted ? World.Outside.Mint : World.Outside.Border, routeFile: routeFile, routeLine: routeLine);
        moved += World.Transfer(from, (w, to, wt), amount - stateCut, cat, income: true, outside: minted ? World.Outside.Mint : World.Outside.Border, routeFile: routeFile, routeLine: routeLine);
        if (Instruments.On && from.W is null && from.Pops is null && amount - moved > 1e-9) PayDiag.CountUnmoved(w, minted, amount - moved);
        return moved;
    }

    internal static double Procure(World w, Region r, int g, double want, Pop? buyer, TreasuryCat cat = TreasuryCat.OtherIn, [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)
    {
        var m = r.Markets[g];
        m.LastDemand += want;
        if (m.Stock <= 1e-9 || m.Price <= 1e-9) return 0;
        double buy = Math.Min(m.Stock, want);
        double pay = buy * m.Price;
        if (buyer is not null && pay > buyer.Savings.Value) { pay = Math.Max(0, buyer.Savings.Value); buy = pay / m.Price; }
        if (buy <= 1e-9) return 0;
        m.Stock -= buy;
        if (buyer is null) m.LastExport += buy;
        else m.LastSold += buy;
        PaySales(buyer is null ? (w, null) : (w, [buyer]), r, g, pay, w, cat, routeFile: routeFile, routeLine: routeLine);
        return buy;
    }

    internal static double TreatyPortTariffScale(World w)
        => 1.0 - Politics.ForeignPortShare(w);

    internal static double CornFactor(ReformLevel lv) => lv switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };

    internal static double TariffRate(World w, Coefficients c) => Math.Clamp(w.Tariff * TreatyPortTariffScale(w), c.TariffFloor, 1);

}
