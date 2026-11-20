namespace PriceOfBread.SimCore;

// (1206) c121ca：ハーネスの最小国（満足度が最も低い国）の必要の充足の内訳の計器 satneeds_s（国×四半期）。事前登録 measurements/m1206_20261118_prereg_ca.txt。
//   食料以外の必要（Pop.NeedMet÷NeedTotal）を、財の格（日用品・嗜好品・その他）に分けて人口で重みづけて貯める＝EconomyStep.Run の必要の台帳の行（NeedMet・NeedTotal を足す所）の直後の 1 呼び。
//   Instruments.On の時だけ呼ばれる。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（累計は計器の ConditionalWeakTable）。助走の月は数えない。
//   重み＝その月の POP の大きさ（呼びの時点）。四半期の 3 か月ぶんの分子・分母を足し、表の行で比にする（月の途中の POP の大きさの変化は重みへそのまま入る＝限界）。
//   食料の充足 foodFill は四半期の末の POP の値（直近の月）＝日用品などの 3 か月の比とは時点が違う（限界）。満足度は NationalScore.MeanSatisfaction。
public static partial class Instruments
{
    internal const string SatNeedsHeaderText = "year\tq\tnation\tpopK\tsatisfaction\tfoodFillLastMonth"
        + "\tdailyMet\tdailyTotal\tluxuryMet\tluxuryTotal\totherMet\totherTotal\tdailyFill\tluxuryFill\totherFill\tnonFoodFill\tluxuryWanted";

    internal sealed class SatNeedsAcc { public readonly double[] Met = new double[3], Total = new double[3], Want = new double[3]; }
    sealed class SatNeedsSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, SatNeedsAcc> SatNeedsAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SatNeedsSink> SatNeedsSinks = new();
    [System.ThreadStatic] static World? satNeedsLastW;
    [System.ThreadStatic] static SatNeedsAcc? satNeedsLastAcc;

    /// <summary>財の格 → 列の番号（0＝日用品・1＝嗜好品・2＝その他）。</summary>
    internal static int SatNeedsCat(GoodTier t) => t == GoodTier.Daily ? 0 : t == GoodTier.Luxury ? 1 : 2;

    /// <summary>EconomyStep.Run の必要の台帳の行の直後：重み（POP の大きさ）つきで分子（met）と分母（total）を足す。</summary>
    internal static void SatNeeds(World? w, Pop p, GoodTier tier, double met, double total, double want)
    {
        if (!Tg.SatNeeds) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (w is null || w.WarmupFrozen) return;
        if (!ReferenceEquals(w, satNeedsLastW))
        {
            satNeedsLastW = w;
            satNeedsLastAcc = Wants("satneeds_s") ? SatNeedsAccs.GetValue(w, _ => new SatNeedsAcc()) : null;
        }
        var a = satNeedsLastAcc;
        if (a is null) return;
        int k = SatNeedsCat(tier);
        a.Met[k] += p.Size * met; a.Total[k] += p.Size * total; a.Want[k] += p.Size * want;
    }

    /// <summary>試験用：欲しい量（Σ POP の大きさ×GoodNeed×needW・格ごと）の写し。嗜好品は分母が met＝充足が常に 1 なので、欲しい量だけがその格の需要を示す（(1214)）。</summary>
    internal static double[] SatNeedsWantSnapshot(World w) => SatNeedsAccs.TryGetValue(w, out var a) ? (double[])a.Want.Clone() : new double[3];

    /// <summary>試験用：貯めた分子・分母の写しと、貯めの白紙化。</summary>
    internal static (double[] Met, double[] Total) SatNeedsSnapshot(World w)
        => SatNeedsAccs.TryGetValue(w, out var a) ? ((double[])a.Met.Clone(), (double[])a.Total.Clone()) : (new double[3], new double[3]);
    internal static void SatNeedsReset(World w) { SatNeedsAccs.AddOrUpdate(w, new SatNeedsAcc()); satNeedsLastW = null; satNeedsLastAcc = null; }

    static void SatNeedsTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.SatNeeds) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!Wants("satneeds_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = SatNeedsSinks.GetValue(sim, _ => new SatNeedsSink());
        if (sk.W is null) { sk.W = Open("satneeds", seed); sk.W.WriteLine(SatNeedsHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int yearLabel = sim.Nations[0].World.Year;
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var a = SatNeedsAccs.GetValue(w, _ => new SatNeedsAcc());
            double popK = 0, foodMet = 0, foodNeed = 0;
            foreach (var r in w.Regions)
                foreach (var p in r.Pops) { popK += p.Size; foodMet += p.Size * p.FoodMet; foodNeed += p.Size * p.FoodNeed; }
            double mAll = a.Met[0] + a.Met[1] + a.Met[2], tAll = a.Total[0] + a.Total[1] + a.Total[2];
            sb.Clear();
            sb.Append(I(yearLabel)).Append('\t').Append(I(q)).Append('\t').Append(NameOf(w)).Append('\t').Append(FR(popK))
              .Append('\t').Append(FR(NationalScore.MeanSatisfaction(w))).Append('\t').Append(foodNeed > 1e-9 ? FR(foodMet / foodNeed) : "nan");
            for (int k = 0; k < 3; k++) sb.Append('\t').Append(FR(a.Met[k])).Append('\t').Append(FR(a.Total[k]));
            for (int k = 0; k < 3; k++) sb.Append('\t').Append(a.Total[k] > 1e-9 ? FR(a.Met[k] / a.Total[k]) : "nan");
            sb.Append('\t').Append(tAll > 1e-9 ? FR(mAll / tAll) : "nan");
            sb.Append('\t').Append(FR(a.Want[1]));   // (1214) 嗜好品の欲しい量（末尾の列）
            sk.W.WriteLine(sb.ToString());
            SatNeedsAccs.AddOrUpdate(w, new SatNeedsAcc());
        }
        satNeedsLastW = null; satNeedsLastAcc = null;
    }

    static void SatNeedsClose(MultiNationSim sim) { if (SatNeedsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
