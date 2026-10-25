using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(547) 線 rs2：農奴解放・土地改革への旧支配層の反対の強さ（<see cref="Coefficients.LordOppositionByRentLoss"/>）の番人。
/// ⚠機構と不変条件だけを見る（解放の年の分布・政体の通り道は成績表が見る）。期待値は本体の関数 <see cref="PolicyPreferences.LordIncomeLoss"/> から作らない＝
/// 慣習は world.json のロシアの農制 1.0 と農奴の市場参加床 0.15 から（0.85）・上限は 1.0／0.75／0.5／0.3 の手書き・所得の比は POP を直に数える。</summary>
public class Rs2LordOppositionTests
{
    private static World RussiaBoard()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(1789), new NullAgent());
        sim.Run(4);
        return sim.World;
    }

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
    public void 旗0なら今の式のまま旧支配層の反対は貯蓄の割合から作られる()
    {
        var w = RussiaBoard();
        Assert.Equal(0.0, w.Coef.LordOppositionByRentLoss);
        var (w0, _) = LordSums(w);
        var e = PolicyPreferences.Build(w);
        double expect = -2.0 * (0.5 + w0);
        Assert.Equal(expect, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 9);
        Assert.Equal(expect, PolicyPreferences.Pref(PolicyAxis.LandReform, 0, e, w), 9);
        Assert.Equal(expect, PolicyPreferences.Pref(PolicyAxis.EducationLaw, 0, e, w), 9);   // 分けていない 2 軸（教育法・女性の権利）は今の式のまま
        Assert.True(w0 > 0.05, $"盤面の前提が崩れた（旧支配層の貯蓄の割合 {w0}）");
    }

    [Fact]
    public void 旗1なら農奴解放の反対は次の一段で失う所得の割合になり段ごとの値が手の式と合う()
    {
        var w = RussiaBoard();
        w.Coef.LordOppositionByRentLoss = 1.0;
        var (_, ratio) = LordSums(w);
        Assert.True(ratio > 0.2 && ratio <= 1.0, $"盤面の前提が崩れた（旧支配層の地代÷所得 {ratio}）");
        var e = PolicyPreferences.Build(w);
        const double c = 0.85;   // ロシア＝農制 1.0 を農奴の市場参加床 0.15 が切る（Simulation.CatalogEconomy の min(農制, 1−床)）
        // なし（上限 1.0）→限定（0.75）：(0.85−0.75)/0.85
        w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        Assert.Equal(-2.0 * (0.5 + (c - 0.75) / c * ratio), PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 9);
        // 限定（0.75）→全面（0.5）：(0.75−0.5)/0.75
        w.SerfEmancipation = ReformLevel.Limited;
        Assert.Equal(-2.0 * (0.5 + (0.75 - 0.5) / 0.75 * ratio), PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 9);
        // 全面の次は無い＝失う所得 0＝基礎の 0.5 だけ
        w.SerfEmancipation = ReformLevel.Full;
        Assert.Equal(-1.0, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, e, w), 12);
        // 土地改革：なし→限定（上限 0.65）は (0.85−0.65)/0.85・限定→全面（0.3）は (0.65−0.3)/0.65。農奴解放がなしの間は農奴解放の上限 1.0 が土地改革の上限と小さい方を取る
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
    public void 旗1なら旧支配層の貯蓄を10倍にしても反対の強さは動かず旗0では動く()
    {
        // 〈同じ貴族の富が力にも反対の強さにも入る〉ことの反証：貯蓄（ストック）は地代と所得（フロー）の比に入らない
        double Opp(double flag, double savingsMul)
        {
            var w = RussiaBoard();
            w.Coef.LordOppositionByRentLoss = flag;
            w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
            foreach (var (_, p) in w.AllPopsWithRegion()) if (p.Closed) p.ApplyBakedSavings(p.Savings.Value * savingsMul);
            return PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w);
        }
        Assert.Equal(Opp(1.0, 1.0), Opp(1.0, 10.0), 12);
        Assert.True(Opp(0.0, 10.0) < Opp(0.0, 1.0) - 0.05, "旗 0 では貯蓄が反対の強さに乗る（読む線 rs の根）はずが動かない＝この番人は空洞");
    }

    [Fact]
    public void 慣習の取り分が上限以下の国は農奴解放で失う所得が0で反対は基礎の0_5だけ()
    {
        // 自作農の国（アメリカ＝農制 0 → 慣習 0.3）は農奴解放の上限 0.75／0.5 のどちらにも切られない
        var sim = new Simulation(NationConfig.America().ToScenario(1789), new NullAgent());
        sim.Run(4);
        var w = sim.World;
        w.Coef.LordOppositionByRentLoss = 1.0;
        w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        Assert.Equal(0.3, PolicyPreferences.CustomaryRentShare(w), 12);
        Assert.Equal(-1.0, PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w), 12);
    }

    [Fact]
    public void 計器の表は旗0と旗1のどちらでも今の式と損の式を同じ列に並べる()
    {
        // 計器の列の式は Pref と同じ形（StatusScale×(0.5＋x)）＝旗の値を入れ替えても二つの列は動かず、使われている方だけが旗で変わる
        var w = RussiaBoard();
        var (w0, ratio) = LordSums(w);
        w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        double loss = PolicyPreferences.LordIncomeLoss(w, PolicyAxis.SerfEmancipation, ratio);
        Assert.Equal((0.85 - 0.75) / 0.85 * ratio, loss, 9);
        w.Coef.LordOppositionByRentLoss = 0.0;
        double used0 = -PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w);
        w.Coef.LordOppositionByRentLoss = 1.0;
        double used1 = -PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, PolicyPreferences.Build(w), w);
        Assert.Equal(PolicyPreferences.StatusScale * (0.5 + w0), used0, 9);
        Assert.Equal(PolicyPreferences.StatusScale * (0.5 + loss), used1, 9);
    }
}
