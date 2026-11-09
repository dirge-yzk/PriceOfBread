using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1007) 商人・職人の空と小営業の入口の錠 A・B の計器 merch_s（読むだけ・恒等・事前登録 measurements/m1007_20261110_prereg_ms.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（置き場は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝全ての州（Tier を問わない）で商人・職人の POP が空かを数え、小営業の入口（Meritocracy.cs の ①*）の錠 B（労働者の元手）と錠 A（空の行き先を飛ばす）を、エンジンの関数を呼ばずに計器が r.Pops から再計算する。cpop_s は植民地格の州だけ（格が変わると打ち切り）。
// 二段：MerchEnter（Simulation.cs の社会移動の区間の前・国の World ごと・助走の外）が州の行を直近の 1 四半期ぶんだけ置く／MerchTable（Quarter）が q を頭に付けて書き出す。
// ⚠限界（計器が自己申告する）：①year は呼んだ時点の World.Year＝その四半期自身の年（Quarter から書く表の q%4==3 の行は翌年のラベルなので年では結ばず (quarter,nation,region) で結ぶ）
//   ②merch_s は社会移動の前・cpop_s は後＝空の商人の POP へ入る道は Meritocracy に無いので空の判定は同じだが、popK の値は同じとは限らない③錠 B の元手は労働者（MeritFromIndex）の生の貯蓄・avgPerCap は max(0,貯蓄) の和÷頭数（エンジンと同じ定義を計器が自分で書く）。
public static partial class Instruments
{
    internal const string MerchHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tmerchantK\tartisanK\tworkerK\tmerchantEmpty\tartisanEmpty\t"
        + "avgPerCap\tworkerSavPerCap\tmeritActive\tlockBOpen\tlockABlockedMerchant\tlockABlockedArtisan\t"
        + "merchEarnedPerCap\tworkerIncomePerCap\temptyMerchantEarnedPerMonth\tmerchantDebt\tpromotionPremium";

    /// <summary>空の判定の閾値（エンジンの Meritocracy.Eps と同じ値の写し・係数ではない）。</summary>
    internal const double MerchEps = 1e-9;

    sealed class MerchBuf { public readonly List<string> Rows = new(); }
    sealed class MerchSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, MerchBuf> MerchBufs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, MerchSink> MerchSinks = new();

    /// <summary>1 つの州の再計算の結果（純関数の返り値）。</summary>
    internal readonly record struct MerchLocksRow(double PopK, double MerchantK, double ArtisanK, double WorkerK, bool MerchantEmpty, bool ArtisanEmpty,
        double AvgPerCap, double WorkerSavPerCap, bool MeritActive, bool LockBOpen, double MerchEarnedPerCap, double WorkerIncomePerCap, double EmptyMerchantEarned, double MerchantDebt);

    /// <summary>州の POP から錠 A・B を再計算する（読むだけ・エンジンの関数は呼ばない）。<paramref name="pettyEntryRate"/>＝小営業の入口の四半期率（係数の値）。</summary>
    internal static MerchLocksRow MerchLocks(Region r, double pettyEntryRate)
    {
        double total = 0, totalSav = 0;
        foreach (var p in r.Pops) { total += p.Size; totalSav += System.Math.Max(0, p.Savings.Value); }
        double avgPerCap = total > 0 ? totalSav / total : 0;
        Pop? merchant = r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count ? r.Pops[r.MerchantPopIndex] : null;
        Pop? artisan = r.ArtisanPopIndex >= 0 && r.ArtisanPopIndex < r.Pops.Count ? r.Pops[r.ArtisanPopIndex] : null;
        Pop? worker = r.MeritFromIndex >= 0 && r.MeritFromIndex < r.Pops.Count ? r.Pops[r.MeritFromIndex] : null;
        double mk = merchant?.Size ?? 0, ak = artisan?.Size ?? 0, wk = worker?.Size ?? 0;
        bool mEmpty = !(mk > MerchEps), aEmpty = !(ak > MerchEps);
        double wSavPc = wk > MerchEps ? worker!.Savings.Value / wk : double.NaN;
        bool meritActive = total > MerchEps && r.MeritFromIndex >= 0 && r.MeritToIndex >= 0;
        bool lockB = meritActive && pettyEntryRate > 0 && avgPerCap > MerchEps && wk > MerchEps && wSavPc >= avgPerCap;
        double mEarn = merchant is null ? 0 : merchant.EarnedPerMonth.Value;
        return new MerchLocksRow(total, mk, ak, wk, mEmpty, aEmpty, avgPerCap, wSavPc, meritActive, lockB,
            mk > MerchEps ? mEarn / mk : double.NaN, wk > MerchEps ? worker!.EarnedPerMonth.Value / wk : double.NaN,
            mEmpty ? mEarn : double.NaN, merchant?.Debt.Value ?? 0);
    }

    /// <summary>社会移動の区間の前（<see cref="Simulation"/>）：この国の全ての州の行を置く（直近の 1 四半期ぶんだけ・次の呼びで置き換える）。助走中は何もしない。</summary>
    internal static void MerchEnter(World w)
    {
        if (!On || w.WarmupFrozen) return;
        var buf = MerchBufs.GetValue(w, _ => new MerchBuf());
        buf.Rows.Clear();
        string nation = NameOf(w); double pettyRate = w.Coef.PettyEntryRatePerQuarter.Value, premium = w.Coef.PromotionPremium;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string B(bool v) => v ? "1" : "0";
        foreach (var r in w.Regions)
        {
            var m = MerchLocks(r, pettyRate);
            buf.Rows.Add(string.Join("\t", I(w.Year), nation, r.Name, r.Tier.ToString(), F(m.PopK), F(m.MerchantK), F(m.ArtisanK), F(m.WorkerK), B(m.MerchantEmpty), B(m.ArtisanEmpty),
                F(m.AvgPerCap), F(m.WorkerSavPerCap), B(m.MeritActive), B(m.LockBOpen), B(m.LockBOpen && m.MerchantEmpty), B(m.LockBOpen && m.ArtisanEmpty),
                F(m.MerchEarnedPerCap), F(m.WorkerIncomePerCap), F(m.EmptyMerchantEarned), F(m.MerchantDebt), F(premium)));
        }
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：置いてあった行に q を付けて書き、空にする。</summary>
    static void MerchTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = MerchSinks.GetValue(sim, _ => new MerchSink());
        sk.W ??= Open("merch", seed);
        if (!sk.Header) { sk.W.WriteLine(MerchHeaderText); sk.Header = true; }
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            if (!MerchBufs.TryGetValue(n.World, out var buf)) continue;
            foreach (var row in buf.Rows) { sk.W.Write(qs); sk.W.Write('\t'); sk.W.WriteLine(row); }
            buf.Rows.Clear();
        }
    }

    static void MerchClose(MultiNationSim sim)
    {
        if (MerchSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
