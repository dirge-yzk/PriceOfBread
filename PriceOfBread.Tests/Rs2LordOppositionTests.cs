using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(547) 線 rs2：農奴解放・土地改革への旧支配層の反対の強さ（次の一段で失う所得の割合）の番人（(803) で旗 LordOppositionByRentLoss を焼き込んだ）。
/// ⚠機構と不変条件だけを見る（解放の年の分布・政体の通り道は成績表が見る）。期待値は本体の関数 <see cref="PolicyPreferences.LordIncomeLoss"/> から作らない＝
/// 慣習は NationConfig の農制と農奴の市場参加床の係数から組む（盤面の数字を手書きで固定しない）・上限は 1.0／0.75／0.5／0.3 の手書き・所得の比は POP を直に数える。</summary>
public class Rs2LordOppositionTests
{
    private static World RussiaBoard()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(1789), new NullAgent());
        sim.Run(4);
        return sim.World;
    }

    /// <summary>産地の慣習の取り分の期待値＝国の農制と農奴の市場参加床から（Simulation の組み立て min(農制, 1−床) と産地の下限 max(0.3, ·)）。本体の <see cref="PolicyPreferences.CustomaryRentShare"/> は呼ばない。</summary>
    private static double ExpectedCustom(NationConfig cfg, Coefficients c) => Math.Max(0.3, Math.Min(cfg.FarmLordShare, 1.0 - c.SerfCashShareFloor.Value));

    /// <summary>旧支配層（Closed）の貯蓄の割合と、地代÷所得を POP から直に数える（本体の BuildSums を通さない）。</summary>
    private static (double W0, double RentToIncome) LordSums(World w)
    {
        double sv0 = 0, svAll = 0, rent0 = 0, inc0 = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            double sv = Math.Max(0, p.Savings.Value);
            svAll += sv;
            if (p.Closed) { sv0 += sv; rent0 += Math.Max(0, p.LandRentPerMonth.Value); inc0 += Math.Max(0, p.EarnedPerMonth.Value); }
        }
        return (svAll > 1e-9 ? sv0 / svAll : 0, inc0 > 1e-9 ? rent0 / inc0 : 0);
    }

    [Fact]
    public void 農奴解放の反対は次の一段で失う所得の割合になり段ごとの値が手の式と合う()
    {
        var w = RussiaBoard();
        var (_, ratio) = LordSums(w);
        Assert.True(ratio > 0.2 && ratio <= 1.0, $"盤面の前提が崩れた（旧支配層の地代÷所得 {ratio}）");
        var e = PolicyPreferences.Build(w);
        double c = ExpectedCustom(NationConfig.Russia(), w.Coef);   // ロシア＝農制を農奴の市場参加床が切る（world.json と係数から）
        Assert.True(c > 0.75 && c < 1.0, $"盤面の前提が崩れた（ロシアの慣習 {c}・限定の上限 0.75 より上でなければ段が働かない）");
        // なし（上限 1.0）→限定（0.75）：(慣習−0.75)/慣習
        w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        Assert.Equal(-2.0 * (0.5 + (c - 0.75) / c * ratio), PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 9);
        // 限定（0.75）→全面（0.5）：(0.75−0.5)/0.75
        w.SerfEmancipation = ReformLevel.Limited;
        Assert.Equal(-2.0 * (0.5 + (0.75 - 0.5) / 0.75 * ratio), PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 9);
        // 全面の次は無い＝失う所得 0＝基礎の 0.5 だけ
        w.SerfEmancipation = ReformLevel.Full;
        Assert.Equal(-1.0, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 12);
        // 土地改革：なし→限定（上限 0.65）は (慣習−0.65)/慣習・限定→全面（0.3）は (0.65−0.3)/0.65。農奴解放がなしの間は農奴解放の上限 1.0 が土地改革の上限と小さい方を取る
        w.SerfEmancipation = ReformLevel.None; w.LandReform = ReformLevel.None;
        Assert.Equal(-2.0 * (0.5 + (c - 0.65) / c * ratio), PolicyPreferences.Pref(PolicyAxis.LandReform, 0, e, w), 9);
        w.LandReform = ReformLevel.Limited;
        Assert.Equal(-2.0 * (0.5 + (0.65 - 0.3) / 0.65 * ratio), PolicyPreferences.Pref(PolicyAxis.LandReform, 0, e, w), 9);
        // 近代エリートと大衆の項は旗に依らない（旧支配層の項だけを替える）
        Assert.Equal(1.0, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 1, e, w));
        Assert.Equal(1.0, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 2, e, w));
        Assert.Equal(2.0, PolicyPreferences.Pref(PolicyAxis.LandReform, 2, e, w));
    }

    [Fact]
    public void 旧支配層の貯蓄を10倍にしても反対の強さは動かない()
    {
        // 〈同じ貴族の富が力にも反対の強さにも入る〉ことの反証：貯蓄（ストック）は地代と所得（フロー）の比に入らない
        (double Opp, double W0) Run(double savingsMul)
        {
            var w = RussiaBoard();
            w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
            foreach (var (_, p) in w.AllPopsWithRegion()) if (p.Closed) p.ApplyBakedSavings(p.Savings.Value * savingsMul);
            return (PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w), LordSums(w).W0);
        }
        var (o1, w1) = Run(1.0);
        var (o10, w10) = Run(10.0);
        Assert.True(w10 > w1 + 0.01, $"貯蓄を 10 倍にしても旧支配層の貯蓄の割合が上がらない（{w1}→{w10}）＝この番人は空洞");   // 貯蓄を動かした対照（POP を直に数える）
        Assert.Equal(o1, o10, 12);
    }

    [Fact]
    public void 慣習の取り分が上限以下の国は農奴解放で失う所得が0で反対は基礎の0_5だけ()
    {
        // 自作農の国（アメリカ＝農制 0 → 慣習 0.3）は農奴解放の上限 0.75／0.5 のどちらにも切られない
        var sim = new Simulation(NationConfig.America().ToScenario(1789), new NullAgent());
        sim.Run(4);
        var w = sim.World;
        w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        Assert.Equal(ExpectedCustom(NationConfig.America(), w.Coef), PolicyPreferences.CustomaryRentShare(w), 12);
        Assert.True(ExpectedCustom(NationConfig.America(), w.Coef) <= 0.5, "盤面の前提が崩れた（慣習が農奴解放の全面の上限 0.5 以下でなければ失う所得が 0 にならない）");
        Assert.Equal(-1.0, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w), 12);
    }

    [Fact]
    public void 旧支配層の反対の強さは損の式の手の値と一致する()
    {
        // 計器の列の式は Pref と同じ形（StatusScale×(0.5＋x)）
        var w = RussiaBoard();
        var (_, ratio) = LordSums(w);
        w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        double loss = PolicyPreferences.LordIncomeLoss(w, PolicyAxis.SerfEmancipation, ratio);
        double cust = ExpectedCustom(NationConfig.Russia(), w.Coef);
        Assert.Equal((cust - 0.75) / cust * ratio, loss, 9);
        double used1 = -PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w);
        Assert.Equal(PolicyPreferences.StatusScale * (0.5 + loss), used1, 9);
    }
}
