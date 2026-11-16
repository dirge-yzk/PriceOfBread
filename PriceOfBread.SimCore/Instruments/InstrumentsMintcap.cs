namespace PriceOfBread.SimCore;

// (1180) c117b0 B-1′ の段 B′-0：造幣の買いの置き場の計器 mintcap_s（国×四半期）。事前登録 measurements/m1180_20261116_prereg_b0.txt。
//   MintGold の頭で、計器が国の全ての州を自分で走査し（エンジンの関数を呼ばない・旗 MintBuysAtCapital を見ない）、
//   旧の規則の影（全ての州で買う）と新の規則の影（首都の州 World.Regions[0] だけで買う）を同じ式で数える。実際に買えた量は MintGold の SilverMintCapture の隣で首都と首都の外に分ける（金属別）。
//   模型の限界（近似）：予算の消費は〈在庫×月次率×値段と予算の小さい方〉で数える（実際は代金を払えた分だけ）・MintParitySells の売りが在庫を動かす月は見ない（既定 0）。
//   力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（溜めは ConditionalWeakTable）。助走（WarmupFrozen）の月は数えない。
public static partial class Instruments
{
    internal const string MintcapHeaderText = "year\tq\tnation\tlaw\tonGold\tmonths\tfiatM\tcapRegion"
        + "\tcapNoGoodM\tcapNoStockM\tcapFloorM\tcapAboveM\tcapAtMarketM"
        + "\toldShadowAg\toldShadowAu\tnewShadowAg\tnewShadowAu"
        + "\tboughtCapAg\tboughtCapAu\tboughtOtherAg\tboughtOtherAu\taboveBoughtN"
        + "\tcapStock\tcapPriceOverParity\tcapImportAgSum\tcover\tloans\tloansHousehold"
        + "\toriginN\toriginPoverMed\toriginPoverMin\toriginPoverMax";

    internal sealed class MintcapAcc
    {
        public int Months, Fiat, CapNoGood, CapNoStock, CapFloor, CapAbove, CapAtMarket, AboveBought;
        public double OldAg, OldAu, NewAg, NewAu, BoughtCapAg, BoughtCapAu, BoughtOtherAg, BoughtOtherAu, CapImportAg;
        public bool CapAboveNotAt;   // 今月の首都の模型が〈平価超えで市場買いでない〉と言ったか（買えたら aboveBought）
    }
    sealed class MintcapSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, MintcapAcc> MintcapAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, MintcapSink> MintcapSinks = new();

    /// <summary>試験用：国の今の溜め（四半期の表へ流す前）。計器が切れている時は null。</summary>
    internal static MintcapAcc? MintcapPeek(World w) => MintcapAccs.TryGetValue(w, out var a) ? a : null;

    /// <summary>1 州の模型の買う量（買わないなら 0）。gapLeft は市場買いの月次の予算（呼び手が持つ）。</summary>
    static double MintcapModelQty(Region r, int metal, double parity, double rate, ref double gapLeft, out bool aboveNotAt)
    {
        aboveNotAt = false;
        if (r.GoodOf(metal) is null) return 0;
        var m = r.Markets[metal];
        if (m.Stock <= 1e-12) return 0;
        bool atM = m.Price > parity && gapLeft > 1e-9;
        if (m.Price > parity && !atM) { aboveNotAt = true; return 0; }
        double q = atM ? System.Math.Min(m.Stock * rate, gapLeft / m.Price) : m.Stock * rate;
        if (!(q > 1e-12)) return 0;
        if (atM) gapLeft -= q * m.Price;
        return q;
    }

    /// <summary>MintGold の頭：国の全ての州を計器が自分で走査して旧の影と新の影（首都だけ）を数える。</summary>
    internal static void MintcapMonth(World w)
    {
        if (w.WarmupFrozen || !Wants("mintcap_s") || w.Regions.Count == 0) return;
        var a = MintcapAccs.GetValue(w, _ => new MintcapAcc());
        a.Months++; a.CapAboveNotAt = false;
        bool fiat = w.MonetaryLaw == MonetaryStandardLaw.Fiat;
        if (fiat) { a.Fiat++; return; }
        int metal = w.MonetaryLaw == MonetaryStandardLaw.Silver ? Catalog.SilverId : Catalog.GoldOreId;
        double parity = Catalog.RefPrices[metal];
        double gap0 = 0;
        if (w.Coef.MintReserveTargetWeight > 0)
        {
            double coverage = w.BroadMoney.Value > 1e-9 ? w.BaseMoney.Value / w.BroadMoney.Value : 1.0;
            double target = w.Coef.GoldCoverMin.Value * w.Coef.GoldRejoinCoverMargin;
            double sf = target > 1e-9 ? (target - coverage) / target : 0;
            double shortfall = !(sf > 0) ? 0 : sf > 1 ? 1 : sf;
            if (shortfall > 1e-9) gap0 = System.Math.Max(0, target * w.BroadMoney.Value - w.BaseMoney.Value) * MintregRate(w.Coef.MintReserveSellRatePerMonth.Value);
        }
        double rate = MintregRate(w.Coef.MintFloorBuyRatePerMonth.Value);
        var cap = w.Regions[0];
        // 首都の月の区分（計器が自分で数える）
        if (cap.GoodOf(metal) is null) a.CapNoGood++;
        else
        {
            var cm = cap.Markets[metal];
            if (cm.Stock <= 1e-12) a.CapNoStock++;
            else if (cm.Price > parity) { if (gap0 > 1e-9) a.CapAtMarket++; else a.CapAbove++; }
            else a.CapFloor++;
        }
        var sg = cap.GoodOf(Catalog.SilverId);
        if (sg is not null) a.CapImportAg += cap.Markets[Catalog.SilverId].LastImport;
        // 旧の影＝全ての州（予算は州をまたいで 1 本）
        double gapOld = gap0, oldQ = 0;
        foreach (var r in w.Regions) oldQ += MintcapModelQty(r, metal, parity, rate, ref gapOld, out _);
        // 新の影＝首都だけ（予算は首都が全部使える）
        double gapNew = gap0;
        double newQ = MintcapModelQty(cap, metal, parity, rate, ref gapNew, out bool aboveNotAt);
        a.CapAboveNotAt = aboveNotAt;
        if (metal == Catalog.SilverId) { a.OldAg += oldQ; a.NewAg += newQ; } else { a.OldAu += oldQ; a.NewAu += newQ; }
    }

    /// <summary>SilverMintCapture の隣：実際に買えた量を首都と首都の外に分けて溜める（計器が首都を自分で選ぶ）。</summary>
    internal static void MintcapBought(World w, Region r, int metal, double qty)
    {
        if (metal != Catalog.SilverId && metal != Catalog.GoldOreId) return;
        if (!MintcapAccs.TryGetValue(w, out var a) || w.Regions.Count == 0) return;
        bool isCap = ReferenceEquals(r, w.Regions[0]);
        if (isCap && a.CapAboveNotAt) a.AboveBought++;
        if (metal == Catalog.SilverId) { if (isCap) a.BoughtCapAg += qty; else a.BoughtOtherAg += qty; }
        else { if (isCap) a.BoughtCapAu += qty; else a.BoughtOtherAu += qty; }
    }

    static void MintcapTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("mintcap_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = MintcapSinks.GetValue(sim, _ => new MintcapSink());
        if (sk.W is null) { sk.W = Open("mintcap", seed); sk.W.WriteLine(MintcapHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int ag = Catalog.SilverId;
        int yearLabel = sim.Nations[0].World.Year;
        var sb = new System.Text.StringBuilder();
        var ratios = new System.Collections.Generic.List<double>();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var a = MintcapAccs.GetValue(w, _ => new MintcapAcc());
            if (a.Months == 0) continue;
            var cap = w.Regions[0];
            bool hasCapAg = cap.GoodOf(ag) is not null;
            double capStock = hasCapAg ? cap.Markets[ag].Stock : double.NaN;
            double capPov = hasCapAg ? cap.Markets[ag].Price / Catalog.RefPrices[ag] : double.NaN;
            double cover = w.BroadMoney.Value > 1e-9 ? w.BaseMoney.Value / w.BroadMoney.Value : double.NaN;
            ratios.Clear();
            foreach (var r in w.Regions)
            {
                if (r.GoodOf(ag) is null) continue;
                bool prod = false;
                foreach (var p in r.Producers) if (p.GoodId == ag && !p.Bankrupt) { prod = true; break; }
                if (prod) ratios.Add(r.Markets[ag].Price / Catalog.RefPrices[ag]);
            }
            ratios.Sort();
            double med = ratios.Count == 0 ? double.NaN : ratios.Count % 2 == 1 ? ratios[ratios.Count / 2] : 0.5 * (ratios[ratios.Count / 2 - 1] + ratios[ratios.Count / 2]);
            sb.Clear();
            sb.Append(I(yearLabel)).Append('\t').Append(I(q)).Append('\t').Append(NameOf(w))
              .Append('\t').Append(I((int)w.MonetaryLaw)).Append('\t').Append(w.OnGoldStandard ? "1" : "0");
            foreach (int v in new[] { a.Months, a.Fiat }) sb.Append('\t').Append(I(v));
            sb.Append('\t').Append(cap.Name);
            foreach (int v in new[] { a.CapNoGood, a.CapNoStock, a.CapFloor, a.CapAbove, a.CapAtMarket }) sb.Append('\t').Append(I(v));
            foreach (double v in new[] { a.OldAg, a.OldAu, a.NewAg, a.NewAu, a.BoughtCapAg, a.BoughtCapAu, a.BoughtOtherAg, a.BoughtOtherAu }) sb.Append('\t').Append(FR(v));
            sb.Append('\t').Append(I(a.AboveBought));
            foreach (double v in new[] { capStock, capPov, a.CapImportAg, cover, w.OutstandingLoans.Value, w.Bank is { } bk ? bk.HouseholdLoans.Value : 0.0 }) sb.Append('\t').Append(FR(v));
            sb.Append('\t').Append(I(ratios.Count));
            foreach (double v in new[] { med, ratios.Count == 0 ? double.NaN : ratios[0], ratios.Count == 0 ? double.NaN : ratios[ratios.Count - 1] }) sb.Append('\t').Append(FR(v));
            sk.W.WriteLine(sb.ToString());
            MintcapAccs.AddOrUpdate(w, new MintcapAcc());
        }
    }

    static void MintcapClose(MultiNationSim sim) { if (MintcapSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
