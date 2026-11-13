namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（州ごとの月の和は計器の側の RegionFlow に積み、四半期の末に書いて 0 へ戻す）。〔履歴 InstrumentsSvcBuy.cs#1〕
public static partial class Instruments
{
    const int SvcBuySlots = 6;   // 階級ごとの積算の欄：人数・基礎の欲しい量・欲しい量・買いたい量・買えた量・優先順位
    const int SvcBuyClasses = 16;

    internal static string SvcBuyHeaderText() => "quarter\tyear\tnation\tclassId\tclassName\trank\tpopK\tbaseWantPerMonth\twantPerMonth\tdemandPerMonth\tgotPerMonth\tincomeFactor\tfillShare\tbudgetBindShare\tsatisfactionMean";

    static double[] SvcBuyAcc(Region r) => FlowOf(r).SvcBuyAcc ??= new double[SvcBuyClasses * SvcBuySlots];

    /// <summary>家事奉仕の POP ごとの欲しい量（所得の弾力の前後）と予算で絞った買いたい量（EconomyStep の消費のループの中・読むだけ）。</summary>
    internal static void SvcBuyWant(Region r, Pop p, double baseWant, double want, double demand)
    {
        if ((uint)p.ClassId >= SvcBuyClasses) return;
        var a = SvcBuyAcc(r); int o = p.ClassId * SvcBuySlots;
        a[o] += p.Size; a[o + 1] += baseWant; a[o + 2] += want; a[o + 3] += demand; a[o + 5] = EconomyStep.ServiceRank(p);
    }

    /// <summary>家事奉仕の POP ごとの買えた量（配分の直後・読むだけ）。</summary>
    internal static void SvcBuyGot(Region r, Pop p, double qty)
    {
        if ((uint)p.ClassId >= SvcBuyClasses) return;
        SvcBuyAcc(r)[p.ClassId * SvcBuySlots + 4] += qty;
    }

    static void SvcBuyFold(Sink sk, ulong seed, World w, int q, bool picked)
    {
        var tot = new double[SvcBuyClasses * SvcBuySlots];
        foreach (var r in w.Regions)
        {
            if (!Flows.TryGetValue(r, out var f) || f.SvcBuyAcc is not { } a) continue;
            for (int i = 0; i < tot.Length; i++) { if (i % SvcBuySlots == 5) { if (a[i] != 0) tot[i] = a[i]; } else tot[i] += a[i]; }
            System.Array.Clear(a);
        }
        if (!picked) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (int c = 0; c < SvcBuyClasses; c++)
        {
            int o = c * SvcBuySlots;
            if (!(tot[o] > 0)) continue;
            string cname = ""; double satNum = 0, satDen = 0;   // 階級の満足度（Pop.Satisfaction）の人数加重平均＝四半期の末の断面
            foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == c) { if (cname.Length == 0) cname = p.ClassName; satNum += p.Satisfaction * p.Size; satDen += p.Size; }
            sk.SvcBuy ??= Open("svcbuy", seed);
            if (!sk.SvcBuyHeader) { sk.SvcBuy.WriteLine(SvcBuyHeaderText()); sk.SvcBuyHeader = true; }
            sk.SvcBuy.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I(c), cname, I((int)tot[o + 5]),
                F(tot[o] / 3.0), F(tot[o + 1] / 3.0), F(tot[o + 2] / 3.0), F(tot[o + 3] / 3.0), F(tot[o + 4] / 3.0),
                F(tot[o + 1] > 0 ? tot[o + 2] / tot[o + 1] : double.NaN), F(tot[o + 3] > 0 ? tot[o + 4] / tot[o + 3] : double.NaN), F(tot[o + 2] > 0 ? 1.0 - tot[o + 3] / tot[o + 2] : double.NaN), F(satDen > 0 ? satNum / satDen : double.NaN)));
        }
    }
}
