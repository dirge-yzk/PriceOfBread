namespace PriceOfBread.SimCore;

// (1168) c115sb：造幣の床の効き（Simulation.MintGold）を州ごとに読む計器 mintreg_s（州×四半期）。事前登録 measurements/m1168_20261116_prereg_sb.txt。
//   MintGold の頭で、計器が World.Regions を自分で走査し（エンジンの関数を呼ばない）、買わなかった月を理由ごとに数える：不換・金属の市場が無い・在庫なし・平価超え・予算が先の州で尽きた。
//   買う月は計器の模型の月（wouldBuy）と、SilverMintCapture の隣で見た実際に買えた月（bought）を並べる＝差は notPaid（模型は買う・実際は買えない）と modelMiss（模型は買わない・実際は買った）。
//   模型の限界（近似）：予算の消費は〈在庫×月次率×値段と予算の小さい方〉で数える（実際は代金を払えた分だけ）・MintParitySells の売りが在庫を動かす月は見ない（既定 0）。
//   ③ 銀の生産者の労働の重み・同じ担い手の生産者の数・雇用・品位・鉱脈の残りの割合を同じ行に書く（速い崩れ＝買い手の消滅と遅い崩れ＝品位の床を分ける）。
//   力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（溜めは ConditionalWeakTable）。助走（WarmupFrozen）の月は数えない。旧と新の sellR は sellr_s（InstrumentsSellr.cs）を使う。
public static partial class Instruments
{
    internal const string MintregHeaderText = "year\tq\tnation\tregion\tlaw\tonGold\tmonths\tfiatM\tnoGoodM\tnoStockM\taboveM\tgapM\twouldBuyM\tboughtM\tnotPaidM\tatMarketM\tcoverShortM\tmodelMissM"
        + "\tqtyAg\tqtyAu\tstockAg\tstockAu\tpriceAg\tpriceAu\tparityAg\tparityAu\tprodAg\tprodAu\tdemandAg\tdemandAu"
        + "\twLaborAg\twLpAg\tfracAg\tnProdLp\temployed\tgrade\tdepositFrac";

    internal sealed class MintregAcc
    {
        public int Months, Fiat, NoGood, NoStock, Above, Gap, WouldBuy, Bought, AtMarket, CoverShort, MissBought;
        public double QtyAg, QtyAu;
        public bool ModelBuy;   // 今月の模型が〈買う〉と言ったか（capture の側が見て modelMiss を数える）
    }
    sealed class MintregSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, MintregAcc> MintregAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, MintregSink> MintregSinks = new();

    /// <summary>試験用：州の今の溜め（四半期の表へ流す前）。計器が切れている時は null。</summary>
    internal static MintregAcc? MintregPeek(Region r) => MintregAccs.TryGetValue(r, out var a) ? a : null;

    static double MintregRate(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;

    /// <summary>MintGold の頭：全ての州を計器が自分で走査して理由ごとの月を数える。</summary>
    internal static void MintregMonth(World w)
    {
        if (w.WarmupFrozen || !Wants("mintreg_s")) return;
        bool fiat = w.MonetaryLaw == MonetaryStandardLaw.Fiat;
        int metal = fiat ? Catalog.GoldOreId : w.MonetaryLaw == MonetaryStandardLaw.Silver ? Catalog.SilverId : Catalog.GoldOreId;
        double parity = Catalog.RefPrices[metal];
        double shortfall = 0, gapLeft = 0;
        if (!fiat && w.Coef.MintReserveTargetWeight > 0)
        {
            double coverage = w.BroadMoney.Value > 1e-9 ? w.BaseMoney.Value / w.BroadMoney.Value : 1.0;
            double target = w.Coef.GoldCoverMin.Value * w.Coef.GoldRejoinCoverMargin;
            double sf = target > 1e-9 ? (target - coverage) / target : 0;
            shortfall = !(sf > 0) ? 0 : sf > 1 ? 1 : sf;
            if (shortfall > 1e-9) gapLeft = System.Math.Max(0, target * w.BroadMoney.Value - w.BaseMoney.Value) * MintregRate(w.Coef.MintReserveSellRatePerMonth.Value);
        }
        double gapStart = gapLeft, rate = MintregRate(w.Coef.MintFloorBuyRatePerMonth.Value);
        foreach (var r in w.Regions)
        {
            var a = MintregAccs.GetValue(r, _ => new MintregAcc());
            a.Months++; a.ModelBuy = false;
            if (fiat) { a.Fiat++; continue; }
            if (r.GoodOf(metal) is null) { a.NoGood++; continue; }
            var m = r.Markets[metal];
            if (shortfall > 1e-9) a.CoverShort++;
            if (m.Stock <= 1e-12) { a.NoStock++; continue; }
            bool atM = m.Price > parity && gapLeft > 1e-9;
            if (m.Price > parity && !atM) { if (gapStart > 1e-9) a.Gap++; else a.Above++; continue; }
            a.WouldBuy++; a.ModelBuy = true;
            if (atM)
            {
                a.AtMarket++;
                double q = System.Math.Min(m.Stock * rate, gapLeft / m.Price);
                if (q > 1e-12) gapLeft -= q * m.Price;
            }
        }
    }

    /// <summary>SilverMintCapture の隣：実際に買えた量を全ての州で溜める（模型が買わないと言った月に買えたら modelMiss）。</summary>
    internal static void MintregBought(Region r, int metal, double qty)
    {
        if (metal != Catalog.SilverId && metal != Catalog.GoldOreId) return;
        if (!MintregAccs.TryGetValue(r, out var a)) return;
        a.Bought++; if (!a.ModelBuy) a.MissBought++;
        if (metal == Catalog.SilverId) a.QtyAg += qty; else a.QtyAu += qty;
    }

    static void MintregTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("mintreg_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = MintregSinks.GetValue(sim, _ => new MintregSink());
        if (sk.W is null) { sk.W = Open("mintreg", seed); sk.W.WriteLine(MintregHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int ag = Catalog.SilverId, au = Catalog.GoldOreId;
        int yearLabel = sim.Nations[0].World.Year;
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                var ga = r.GoodOf(ag); var gu = r.GoodOf(au);
                if (ga is null && gu is null) continue;
                var a = MintregAccs.GetValue(r, _ => new MintregAcc());
                double stockAg = ga is null ? 0 : r.Markets[ag].Stock, stockAu = gu is null ? 0 : r.Markets[au].Stock;
                double prodAg = 0, prodAu = 0, wAg = 0, wLp = 0; int nLp = 0;
                bool hasProd = false;
                foreach (var p in r.Producers)
                {
                    if (p.GoodId == ag) { prodAg += p.Output; hasProd = true; }
                    else if (p.GoodId == au) { prodAu += p.Output; hasProd = true; }
                }
                if (!hasProd && a.WouldBuy == 0 && a.Bought == 0 && !(stockAg > 1e-12) && !(stockAu > 1e-12)) { MintregAccs.AddOrUpdate(r, new MintregAcc()); continue; }
                double emp = 0; int empN = 0;
                foreach (var p in r.Producers)
                {
                    if (p.GoodId != ag || p.Bankrupt) continue;
                    wAg += p.LaborWeight * p.Scale;
                    int lp = p.LaborPopIndex;
                    if (lp >= 0 && lp < r.Pops.Count) { emp += 1.0 - r.Pops[lp].UnemploymentRate.Value; empN++; }
                }
                if (wAg > 0)
                    foreach (var p in r.Producers)
                    {
                        if (p.Bankrupt) continue;
                        bool sameLp = false;
                        foreach (var s in r.Producers) if (s.GoodId == ag && !s.Bankrupt && s.LaborPopIndex == p.LaborPopIndex) { sameLp = true; break; }
                        if (!sameLp) continue;
                        wLp += p.LaborWeight * p.Scale; nLp++;
                    }
                int notPaid = a.WouldBuy - (a.Bought - a.MissBought);
                sb.Clear();
                sb.Append(I(yearLabel)).Append('\t').Append(I(q)).Append('\t').Append(NameOf(w)).Append('\t').Append(r.Name)
                  .Append('\t').Append(I((int)w.MonetaryLaw)).Append('\t').Append(w.OnGoldStandard ? "1" : "0");
                foreach (int v in new[] { a.Months, a.Fiat, a.NoGood, a.NoStock, a.Above, a.Gap, a.WouldBuy, a.Bought, notPaid, a.AtMarket, a.CoverShort, a.MissBought })
                    sb.Append('\t').Append(I(v));
                foreach (double v in new[] {
                    a.QtyAg, a.QtyAu, stockAg, stockAu,
                    ga is null ? double.NaN : r.Markets[ag].Price, gu is null ? double.NaN : r.Markets[au].Price,
                    Catalog.RefPrices[ag], Catalog.RefPrices[au], prodAg, prodAu,
                    ga is null ? 0 : r.Markets[ag].LastDemand, gu is null ? 0 : r.Markets[au].LastDemand,
                    wAg, wLp, wLp > 0 ? wAg / wLp : double.NaN })
                    sb.Append('\t').Append(FR(v));
                sb.Append('\t').Append(I(nLp)).Append('\t').Append(empN > 0 ? FR(emp / empN) : "nan")
                  .Append('\t').Append(FR(r.MineralGradeFactor))
                  .Append('\t').Append(r.MineralDepositInitial > 1e-9 ? FR(r.MineralDeposit / r.MineralDepositInitial) : "nan");
                sk.W.WriteLine(sb.ToString());
                MintregAccs.AddOrUpdate(r, new MintregAcc());
            }
        }
    }

    static void MintregClose(MultiNationSim sim) { if (MintregSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
