using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1003) 貿易の計器 C1 nationgood_s（国×財の断面・読むだけ・恒等・事前登録 measurements/m1003_20261110_prereg_tg.txt）
// 部分クラス（InstrumentsBasisx.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 越境の額と量は、清算の成約の位置（BilateralMarket の TradeDeal の呼び口）で計器が自分で積む（財×国）。
//   輸入＝積出し価格＋運賃（vA＋frG）・輸出＝積出し価格（vA）・量は qA。読むのは越境の成約だけ（国内の取引は積まない）。
//   積む範囲は World.ImportsByGood・ExportsByGood と同じ〈清算 1 回分〉＝清算の頭（NgHead）で 0 へ戻す＝四半期の末には最後の清算（月 3）の分が残る。
// 行＝調べる年（NgEveryYears の倍数と 1913）の四半期の末×全ての国×全ての財。州の断面（産出・需要・不足・在庫・価格）は四半期の末の値を州から読む。
// ⚠限界（計器が自己申告する）：subsistenceShare の列は作らない（Pop.SubsistenceShare は POP の食べる割合で、国×財に導けない）。
//   priceMeanGold は州の Price の単純平均×国の FxRate（取引の重みなし）。量は財の単位（重さを掛けない）。
public static partial class Instruments
{
    internal const string NationgoodHeaderText = "year\tnation\tgood\ttier\tproducedQty\tdemandQty\tshortageQty\tstockQty\tcrossImportValue\tcrossExportValue\tcrossImportQty\tcrossExportQty\tpriceMeanGold\tdutyTariffPaid\tdutyCornPaid\ttarEffAtPay\texRateAtPay";   // (1008) 末尾の 4 列：越境の成約で買い手が払った関税の分・穀物法の分（買い手の通貨）・その清算の頭の tar・exRate

    /// <summary>越境の額と量を調べる年の間隔（年の数字がこの数の倍数と 1913）。既定 10＝trroute と同じ。番人だけが 1 にする。</summary>
    internal static int NgEveryYears = 10;

    internal static bool NgYear(int year) => year % NgEveryYears == 0 || year == 1913;

    sealed class NgAcc
    {
        public double[] ImpV = [], ExpV = [], ImpQ = [], ExpQ = [], DutyT = [], DutyC = [], Tar = [], Ex = [];
        public int Gc;
        public readonly Dictionary<World, int> Idx = new();
        public World[] Worlds = [];
        public World? LastB, LastS; public int LastBi, LastSi;
        public bool Any;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, NgAcc> NgAccs = new();
    sealed class NgSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, NgSink> NgSinks = new();

    /// <summary>清算の頭：越境の蓄えを 0 へ戻す（World.ImportsByGood の 0 戻しと同じ位置）。計器が切れていれば何もしない。</summary>
    internal static void NgHead(InternationalMarket m, IReadOnlyList<Simulation> nations, int gc, double[]? tar = null, double[]? exRate = null)
    {
        if (!On) return;
        var a = NgAccs.GetValue(m, _ => new NgAcc());
        int nn = nations.Count;
        if (a.ImpV.Length != nn * gc) { a.ImpV = new double[nn * gc]; a.ExpV = new double[nn * gc]; a.ImpQ = new double[nn * gc]; a.ExpQ = new double[nn * gc]; a.DutyT = new double[nn * gc]; a.DutyC = new double[nn * gc]; }
        else { Array.Clear(a.ImpV); Array.Clear(a.ExpV); Array.Clear(a.ImpQ); Array.Clear(a.ExpQ); Array.Clear(a.DutyT); Array.Clear(a.DutyC); }
        if (a.Tar.Length != nn) { a.Tar = new double[nn]; a.Ex = new double[nn]; }
        for (int i = 0; i < nn; i++) { a.Tar[i] = tar is not null ? tar[i] : double.NaN; a.Ex[i] = exRate is not null ? exRate[i] : double.NaN; }   // (1008) 清算の頭の tar・exRate（エンジンの局所）
        a.Gc = gc; a.Idx.Clear(); a.Worlds = new World[nn]; a.LastB = a.LastS = null; a.Any = true;
        for (int i = 0; i < nn; i++) { a.Idx[nations[i].World] = i; a.Worlds[i] = nations[i].World; }
    }

    static int NgIdx(NgAcc a, World w, bool buyer)
    {
        if (buyer) { if (ReferenceEquals(w, a.LastB)) return a.LastBi; a.LastBi = a.Idx.TryGetValue(w, out var i) ? i : -1; a.LastB = w; return a.LastBi; }
        if (ReferenceEquals(w, a.LastS)) return a.LastSi; a.LastSi = a.Idx.TryGetValue(w, out var j) ? j : -1; a.LastS = w; return a.LastSi;
    }

    /// <summary>成約 1 件（TradeDeal の位置）：越境だけを積む。</summary>
    internal static void NgDeal(InternationalMarket m, World buyer, World seller, int good, double qty, double value, double freight, bool cross, double dutyT = 0.0, double dutyC = 0.0)
    {
        if (!cross || !NgAccs.TryGetValue(m, out var a) || !a.Any) return;
        int bi = NgIdx(a, buyer, true), si = NgIdx(a, seller, false);
        if (bi < 0 || si < 0 || good >= a.Gc) return;
        a.ImpV[bi * a.Gc + good] += value + freight; a.ImpQ[bi * a.Gc + good] += qty;
        a.ExpV[si * a.Gc + good] += value; a.ExpQ[si * a.Gc + good] += qty;
        a.DutyT[bi * a.Gc + good] += dutyT; a.DutyC[bi * a.Gc + good] += dutyC;   // (1008) 買い手の通貨・越境の成約だけ
    }

    /// <summary>番人用：国の添字×財の越境の蓄え（輸入額・輸出額・輸入量・輸出量）。蓄えが無ければ null。</summary>
    internal static (double[] ImpV, double[] ExpV, double[] ImpQ, double[] ExpQ, int Gc)? NgSnapshot(InternationalMarket m)
        => NgAccs.TryGetValue(m, out var a) && a.Any ? (a.ImpV, a.ExpV, a.ImpQ, a.ExpQ, a.Gc) : null;

    /// <summary>番人用（1008）：国の添字×財の払った関税の分・穀物法の分（買い手の通貨）・国の tar・exRate。</summary>
    internal static (double[] DutyT, double[] DutyC, double[] Tar, double[] Ex, int Gc)? NgDutySnapshot(InternationalMarket m)
        => NgAccs.TryGetValue(m, out var a) && a.Any ? (a.DutyT, a.DutyC, a.Tar, a.Ex, a.Gc) : null;

    static void NationgoodTable(MultiNationSim sim, ulong seed, int q)
    {
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || sim.Nations.Count == 0 || !NgYear(sim.Nations[0].World.Year)) return;
        if (!NgAccs.TryGetValue(sim.Market, out var a) || !a.Any) return;   // 国の名簿は清算のたびに World→添字で写す（NgHead）＝清算の後に増減・並べ替えがあっても World で引く・清算に居なかった国の行は越境 0
        var sk = NgSinks.GetValue(sim, _ => new NgSink());
        if (sk.W is null) { sk.W = Open("nationgood", seed); sk.W.WriteLine(NationgoodHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // 行＝清算に居た国（清算のあと Quarter までに消えた国も含む＝年×財の量の保存が崩れない）→ 清算の後に増えた国（越境 0）
        int yearLabel = sim.Nations[0].World.Year;   // 年の印は呼びの時点の名簿の先頭の国の年（C2 と同じ・消えた国の世界の年は使わない）
        var rowWorlds = new List<World>(a.Worlds);
        foreach (var n in sim.Nations) if (!a.Idx.ContainsKey(n.World)) rowWorlds.Add(n.World);
        foreach (var w in rowWorlds)
        {
            string nm = NameOf(w);
            for (int g = 0; g < Catalog.Goods.Length && g < a.Gc; g++)
            {
                double prod = 0, dem = 0, sh = 0, stock = 0, price = 0; int n = 0;
                foreach (var r in w.Regions)
                {
                    if (g >= r.Markets.Length) continue;
                    var mk = r.Markets[g];
                    prod += mk.LastProduced; dem += mk.LastDemand; sh += mk.LastShortage; stock += mk.Stock; price += mk.Price; n++;
                }
                int ci = a.Idx.TryGetValue(w, out var ix0) ? ix0 : -1; bool had = ci >= 0; int k = ci * a.Gc + g;
                sk.W.WriteLine(string.Join('\t', I(yearLabel), nm, Catalog.Goods[g].Name, Catalog.Goods[g].Tier.ToString(), F(prod), F(dem), F(sh), F(stock),
                    had ? F(a.ImpV[k]) : "0", had ? F(a.ExpV[k]) : "0", had ? F(a.ImpQ[k]) : "0", had ? F(a.ExpQ[k]) : "0", n > 0 ? F(price / n * w.FxRate) : "nan",
                    had ? FR(a.DutyT[k]) : "0", had ? FR(a.DutyC[k]) : "0", had ? FR(a.Tar[ci]) : "nan", had ? FR(a.Ex[ci]) : "nan"));
            }
        }
    }

    static void NationgoodClose(MultiNationSim sim) { if (NgSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
