namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (433) 家事奉仕の買い手の表 svcbuy_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝InstrumentsHouse.cs と同じ作法。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（州ごとの月の和は計器の側の RegionFlow に積み、四半期の末に書いて 0 へ戻す）。
// 目的＝事前登録 measurements/sb433_20261019_prereg.txt の段 0：家事奉仕の買い手が階級ごとにどの量を〈欲しがり・予算で絞られ・買えたか〉を割る（役務の優先配分で大衆が先に埋まる根の確認）。
// 行＝挙げた国×四半期×階級（その四半期の 3 か月の和・助走の月は数えない）。列（単位は接尾辞：K＝千人・PerMonth＝その月の平均の量・Share＝0〜1）：
//   quarter／year／nation／classId／className／rank（役務の優先順位 0 大衆・1 近代エリート・2 その他＝EconomyStep.ServiceRank の値）／
//   popK（階級の人数の月平均）／baseWantPerMonth（必要量×価格弾力＝所得の弾力を掛ける前の欲しい量）／wantPerMonth（所得の弾力を掛けた欲しい量〔desired〕）／
//   demandPerMonth（予算で絞った後の買いたい量＝GoodDemand）／gotPerMonth（配分で実際に買えた量）／incomeFactor（want÷baseWant＝所得の弾力の係数の量加重）／
//   fillShare（got÷demand）／budgetBindShare（1−demand÷want）／satisfactionMean（階級の満足度の人数加重平均・四半期の末の断面・全州）
// ⚠限界（計器が自己申告する）：欲しい量が 0 の階級（必要量 0 の POP）は行に出ない／月数は 3 固定で割る（四半期の途中で始まる走りは薄く出る）／州の単純和＝量の和なので州の人数の重みは量に入っている。
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
