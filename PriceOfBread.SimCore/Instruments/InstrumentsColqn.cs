namespace PriceOfBread.SimCore;

// (1153) c113qn：植民地の毎月のキニーネ（Colonial.cs:24・:43）の恒等の計器 colqn_s（国×四半期）。事前登録 measurements/m1153_20261115_prereg_qn.txt。
//   同じ盤面で〈旧の規則＝マラリアの植民地格の州 1 つあたり月 6.0〉の量を計器が自分で組み（州は w.Regions を計器が回して数える・率は計器が持つ旧の既定）、
//   :43 の調達の前と後に首都の市場のキニーネの需要・在庫と植民地の維持費の請求を計器が自分で読み、戻り値（実際に買えた量）と並べる。
//   力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。助走（WarmupFrozen）の月は数えない。
public static partial class Instruments
{
    internal const string ColqnHeaderText = "year\tq\tnation\tmonths\tmalMonths\tmalRegionMonths\t"
        + "oldWant\toldReq\toldClaim\toldBuyable\tobsDemand\tobsGot\tobsStockDrop\tobsCost\tobsClaim\tfuelClaim\tmaxAbsDiffGot\tmaxRelDiffReq"
        + "\texpN\texpDepletedN\texpNeed\texpReq\texpFillUpper\texpStockAfter";

    /// <summary>旧の規則の率（p04 の MalariaQuininePerColony の旧の既定・月×州）。係数が 0 の木でも旧の規則を同じ盤面で数えるため、計器が自分で持つ。</summary>
    internal const double ColqnOldPerColony = 6.0;

    /// <summary>:43 の調達の前の断面と、旧の規則の量（BuyUpkeepGood と Procure と同じ順で組む）。Ok が偽なら数えない。</summary>
    internal readonly record struct ColqnPre(bool Ok, int Mal, double OldWant, double OldReq, double OldClaim, double OldBuyable, double Demand, double Stock, double Price, double Claim);

    sealed class ColqnAcc
    {
        public int Months, MalMonths, MalRegions;
        public double OldWant, OldReq, OldClaim, OldBuyable, ObsDemand, ObsGot, ObsStockDrop, ObsCost, ObsClaim, FuelClaim, MaxGot, MaxReq;
        public int ExpN, ExpDepletedN;
        public double ExpNeed, ExpReq, ExpFillUpper, ExpStockAfter;
    }
    sealed class ColqnSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, ColqnAcc> ColqnAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ColqnSink> ColqnSinks = new();

    /// <summary>:43 の前（:41 の燃料の調達の後）：首都の市場のキニーネと請求を読み、旧の規則の量を組む。</summary>
    internal static ColqnPre ColqnBefore(World w, Region r)
    {
        if (w.WarmupFrozen || !Wants("colqn_s")) return default;
        const int g = Catalog.QuinineId;
        int mal = 0; double want = 0;
        foreach (var cr in w.Regions)
            if (cr.Tier == RegionTier.Colonial && cr.Malarial) { mal++; want += ColqnOldPerColony; }
        double demand = 0, stock = 0, price = 0, req = 0, claim = 0, buyable = 0;
        if (g < r.Markets.Length)
        {
            var m = r.Markets[g];
            demand = m.LastDemand; stock = m.Stock; price = m.Price;
            if (want > 1e-9)
            {
                double x = want * EconomyStep.StatePriceAfford(r, w.Coef, g);
                if (x > 1e-9)
                {
                    claim = x * System.Math.Max(price, 1e-9);
                    x *= w.FiscalFundingRatio.Value;
                    if (x > 1e-9)
                    {
                        req = x;
                        double b = stock <= 1e-9 || price <= 1e-9 ? 0 : System.Math.Min(stock, x);
                        buyable = b <= 1e-9 ? 0 : b;
                    }
                }
            }
        }
        return new ColqnPre(true, mal, want, req, claim, buyable, demand, stock, price, w.LastColonialUpkeepPerMonth.Value);
    }

    /// <summary>:43 の後：実際に買えた量（戻り値）と、同じ欄の後の値を読んで足す。</summary>
    internal static void ColqnAfter(World w, Region r, in ColqnPre p, double got)
    {
        if (!p.Ok) return;
        const int g = Catalog.QuinineId;
        var a = ColqnAccs.GetValue(w, _ => new ColqnAcc());
        double demand1 = p.Demand, stock1 = p.Stock;
        if (g < r.Markets.Length) { demand1 = r.Markets[g].LastDemand; stock1 = r.Markets[g].Stock; }
        double obsDemand = demand1 - p.Demand;
        a.Months++; if (p.Mal > 0) a.MalMonths++; a.MalRegions += p.Mal;
        a.OldWant += p.OldWant; a.OldReq += p.OldReq; a.OldClaim += p.OldClaim; a.OldBuyable += p.OldBuyable;
        a.ObsDemand += obsDemand; a.ObsGot += got; a.ObsStockDrop += p.Stock - stock1; a.ObsCost += got * p.Price;
        a.ObsClaim += w.LastColonialUpkeepPerMonth.Value - p.Claim; a.FuelClaim += p.Claim;
        double dg = System.Math.Abs(got - p.OldBuyable); if (dg > a.MaxGot) a.MaxGot = dg;
        double dr = System.Math.Abs(obsDemand - p.OldReq) / System.Math.Max(1.0, System.Math.Abs(demand1)); if (dr > a.MaxReq) a.MaxReq = dr;
    }

    /// <summary>(1158) 遠征の側（World.Invade の熱帯の標的のキニーネ・World.cs:1871〜:1874）。既にある口 Instruments.Invaded（World.cs:1888＝調達の後）から呼ぶ。
    /// 調達の前の在庫は読めない（World.cs は持ち主の外）＝在庫が尽きたか（後の在庫 ≦1e-9 ⇔ 前の在庫 ≦ 求め）と、充足の上界（求め÷need＝在庫が残った遠征ではちょうどの充足）を数える。
    /// need は計器が自分で組む（缶詰の基礎＋標的の軍事力×1 あたり＝遠征のキニーネは缶詰の要求量と同じ量）。求めは BuyUpkeepGood と同じ順（afford→資金の比）。</summary>
    internal static void ColqnInvaded(Power invader, Power target)
    {
        if (!target.RequiresQuinine || invader.Nation is not { } w || w.WarmupFrozen || !Wants("colqn_s")) return;
        const int g = Catalog.QuinineId;
        var r = w.Region;
        double need = w.Coef.InvadeCannedBase + w.Coef.InvadeCannedPerMilitary * target.Military;
        double req = 0, stock = 0;
        if (g < r.Markets.Length)
        {
            stock = r.Markets[g].Stock;
            if (need > 1e-9) { req = need * EconomyStep.StatePriceAfford(r, w.Coef, g); req = req > 1e-9 ? req * w.FiscalFundingRatio.Value : 0; }
        }
        var a = ColqnAccs.GetValue(w, _ => new ColqnAcc());
        a.ExpN++; if (stock <= 1e-9) a.ExpDepletedN++;
        a.ExpNeed += need; a.ExpReq += req; a.ExpStockAfter += stock;
        a.ExpFillUpper += need > 1e-9 ? System.Math.Min(1.0, System.Math.Max(0.0, req / need)) : 0;
    }

    static void ColqnTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("colqn_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = ColqnSinks.GetValue(sim, _ => new ColqnSink());
        if (sk.W is null) { sk.W = Open("colqn", seed); sk.W.WriteLine(ColqnHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int yearLabel = sim.Nations[0].World.Year;
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var a = ColqnAccs.GetValue(w, _ => new ColqnAcc());
            sb.Clear();
            sb.Append(I(yearLabel)).Append('\t').Append(I(q)).Append('\t').Append(NameOf(w))
              .Append('\t').Append(I(a.Months)).Append('\t').Append(I(a.MalMonths)).Append('\t').Append(I(a.MalRegions));
            foreach (double v in new[] { a.OldWant, a.OldReq, a.OldClaim, a.OldBuyable, a.ObsDemand, a.ObsGot, a.ObsStockDrop, a.ObsCost, a.ObsClaim, a.FuelClaim, a.MaxGot, a.MaxReq })
                sb.Append('\t').Append(FR(v));
            sb.Append('\t').Append(I(a.ExpN)).Append('\t').Append(I(a.ExpDepletedN));
            foreach (double v in new[] { a.ExpNeed, a.ExpReq, a.ExpFillUpper, a.ExpStockAfter })
                sb.Append('\t').Append(FR(v));
            sk.W.WriteLine(sb.ToString());
            ColqnAccs.AddOrUpdate(w, new ColqnAcc());
        }
    }

    static void ColqnClose(MultiNationSim sim) { if (ColqnSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
