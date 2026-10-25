using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(558) 線 rs3：農奴解放・土地改革への旧支配層の減らす向きの強さ（<see cref="Coefficients.LordOppositionReverseGain"/>）の番人。
/// ⚠機構と不変条件だけを見る（再農奴化が起きる頻度・解放の年は成績表が見る）。期待値は本体の関数 <see cref="PolicyPreferences.LordIncomeGain"/> から作らない＝
/// 慣習は NationConfig の農制と農奴の市場参加床の係数から組み、上限は 1.0／0.75／0.5（農奴解放）・1.0／0.65／0.3（土地改革）の手書き、所得の比は POP を直に数える。</summary>
public class Rs3LordReverseGainTests
{
    private static World Board(NationConfig cfg)
    {
        var sim = new Simulation(cfg.ToScenario(1789), new NullAgent());
        sim.Run(4);
        return sim.World;
    }

    private static double ExpectedCustom(NationConfig cfg, Coefficients c) => Math.Max(0.3, Math.Min(cfg.FarmLordShare, 1.0 - c.SerfCashShareFloor.Value));

    /// <summary>旧支配層（Closed）の地代÷所得を POP から直に数える（本体の BuildSums を通さない）。</summary>
    private static double LordRentToIncome(World w)
    {
        double rent0 = 0, inc0 = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
            if (p.Closed) { rent0 += Math.Max(0, p.LandRentPerMonth.Value); inc0 += Math.Max(0, p.EarnedPerMonth.Value); }
        return inc0 > 1e-9 ? rent0 / inc0 : 0;
    }

    private static double SerfCap(ReformLevel l) => l switch { ReformLevel.None => 1.0, ReformLevel.Limited => 0.75, _ => 0.5 };
    private static double LandCap(ReformLevel l) => l switch { ReformLevel.None => 1.0, ReformLevel.Limited => 0.65, _ => 0.3 };
    private static double Eff(double custom, ReformLevel serf, ReformLevel land) => Math.Min(custom, Math.Min(SerfCap(serf), LandCap(land)));

    /// <summary>減らす向きの強さ（Pref の返り値の絶対値＝×sign(−1) で読む向きの旧支配層の支持の強さ）。</summary>
    private static double Dec(World w, PolicyAxis a) => -PolicyPreferences.Pref(a, 0, PolicyPreferences.Build(w), w, false);
    private static double Inc(World w, PolicyAxis a) => -PolicyPreferences.Pref(a, 0, PolicyPreferences.Build(w), w, true);

    [Fact]
    public void 旗0なら全軸全ブロックで減らす向きは進める向きとビット一致する()
    {
        var w = Board(NationConfig.Russia());
        Assert.Equal(0.0, w.Coef.LordOppositionReverseGain);   // 既定は 0（事前登録・腕 B が 1 に倒す）
        foreach (var serf in new[] { ReformLevel.None, ReformLevel.Limited, ReformLevel.Full })
            foreach (var land in new[] { ReformLevel.None, ReformLevel.Limited, ReformLevel.Full })
            {
                w.SerfEmancipation = serf; w.LandReform = land;
                var e = PolicyPreferences.Build(w);
                foreach (PolicyAxis a in Enum.GetValues<PolicyAxis>())
                    for (int b = 0; b < PolicyPreferences.BlocCount; b++)
                        Assert.Equal(BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, b, e, w, true)), BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, b, e, w, false)));
            }
    }

    [Fact]
    public void rs2の旗が0なら新しい旗が1でも無効で今の式のまま()
    {
        var w = Board(NationConfig.Russia());
        w.Coef.LordOppositionByRentLoss = 0.0; w.Coef.LordOppositionReverseGain = 1.0;
        w.SerfEmancipation = ReformLevel.Full; w.LandReform = ReformLevel.Full;
        var e = PolicyPreferences.Build(w);
        Assert.False(PolicyPreferences.DirectionAware(w, PolicyAxis.LandReform, 0));
        foreach (PolicyAxis a in Enum.GetValues<PolicyAxis>())
            for (int b = 0; b < PolicyPreferences.BlocCount; b++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, b, e, w, true)), BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, b, e, w, false)));
    }

    [Fact]
    public void 旗1なら減らす向きは1段戻したときの得になり段ごとの値が手の式と合う()
    {
        var w = Board(NationConfig.Russia());
        w.Coef.LordOppositionReverseGain = 1.0;
        double ratio = LordRentToIncome(w);
        Assert.True(ratio > 0.2 && ratio <= 1.0, $"盤面の前提が崩れた（旧支配層の地代÷所得 {ratio}）");
        double c = ExpectedCustom(NationConfig.Russia(), w.Coef);
        Assert.True(c > 0.75 && c < 1.0, $"盤面の前提が崩れた（ロシアの慣習 {c}・限定の上限 0.75 より上でなければ段が働かない）");
        double S(double gain) => 2.0 * (0.5 + gain);
        // 農奴解放の軸（土地改革はなし＝上限 1.0）
        w.LandReform = ReformLevel.None;
        w.SerfEmancipation = ReformLevel.Full;      // 全面→限定：今 0.5・戻した後 0.75
        Assert.Equal(S((Eff(c, ReformLevel.Limited, ReformLevel.None) - Eff(c, ReformLevel.Full, ReformLevel.None)) / Eff(c, ReformLevel.Full, ReformLevel.None) * ratio), Dec(w, PolicyAxis.SerfEmancipation), 9);
        w.SerfEmancipation = ReformLevel.Limited;   // 限定→なし：今 0.75・戻した後 c
        Assert.Equal(S((c - 0.75) / 0.75 * ratio), Dec(w, PolicyAxis.SerfEmancipation), 9);
        w.SerfEmancipation = ReformLevel.None;      // なしの前は無い＝得 0＝基礎の 0.5 だけ
        Assert.Equal(1.0, Dec(w, PolicyAxis.SerfEmancipation), 12);
        // 土地改革の軸
        w.SerfEmancipation = ReformLevel.None;
        w.LandReform = ReformLevel.Full;            // 全面→限定：今 0.3・戻した後 0.65
        Assert.Equal(S((0.65 - 0.3) / 0.3 * ratio), Dec(w, PolicyAxis.LandReform), 9);
        w.LandReform = ReformLevel.Limited;         // 限定→なし：今 0.65・戻した後 c
        Assert.Equal(S((c - 0.65) / 0.65 * ratio), Dec(w, PolicyAxis.LandReform), 9);
        w.LandReform = ReformLevel.None;
        Assert.Equal(1.0, Dec(w, PolicyAxis.LandReform), 12);
        // 農奴解放が全面の国の土地改革の全面→限定：戻した後の上限は農奴解放の 0.5 が切る（0.65 ではない）
        w.SerfEmancipation = ReformLevel.Full; w.LandReform = ReformLevel.Full;
        Assert.Equal(S((0.5 - 0.3) / 0.3 * ratio), Dec(w, PolicyAxis.LandReform), 9);
    }

    [Fact]
    public void 旗1でも進める向きと近代エリートと大衆の項は変わらない()
    {
        var w = Board(NationConfig.Russia());
        double ratio = LordRentToIncome(w);
        double c = ExpectedCustom(NationConfig.Russia(), w.Coef);
        w.SerfEmancipation = ReformLevel.Limited; w.LandReform = ReformLevel.None;
        w.Coef.LordOppositionReverseGain = 0.0;
        var off = PolicyPreferences.Build(w);
        double[] before = new double[6];
        int i = 0;
        foreach (var a in new[] { PolicyAxis.SerfEmancipation, PolicyAxis.LandReform })
            for (int b = 0; b < 3; b++) before[i++] = PolicyPreferences.Pref(a, b, off, w, true);
        w.Coef.LordOppositionReverseGain = 1.0;
        var on = PolicyPreferences.Build(w);
        i = 0;
        foreach (var a in new[] { PolicyAxis.SerfEmancipation, PolicyAxis.LandReform })
            for (int b = 0; b < 3; b++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(before[i++]), BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, b, on, w, true)));
                // 近代エリート（1）と大衆（2）は減らす向きも進める向きと同じ値
                if (b > 0) Assert.Equal(PolicyPreferences.Pref(a, b, on, w, true), PolicyPreferences.Pref(a, b, on, w, false));
            }
        // 進める向きは rs2 の損の式（限定→全面）のまま
        Assert.Equal(2.0 * (0.5 + (0.75 - 0.5) / 0.75 * ratio), Inc(w, PolicyAxis.SerfEmancipation), 9);
        Assert.True(c > 0.75);
    }

    [Fact]
    public void 慣習が上限以下の国は旗1でも減らす向きの得が0で床の1_0のまま()
    {
        var w = Board(NationConfig.America());
        w.Coef.LordOppositionReverseGain = 1.0;
        double c = ExpectedCustom(NationConfig.America(), w.Coef);
        Assert.True(c <= 0.3 + 1e-12, $"盤面の前提が崩れた（アメリカの慣習 {c}・土地改革の全面の上限 0.3 以下でなければ得が 0 にならない）");
        Assert.Equal(c, PolicyPreferences.CustomaryRentShare(w), 12);
        foreach (var serf in new[] { ReformLevel.Limited, ReformLevel.Full })
            foreach (var land in new[] { ReformLevel.Limited, ReformLevel.Full })
            {
                w.SerfEmancipation = serf; w.LandReform = land;
                Assert.Equal(1.0, Dec(w, PolicyAxis.SerfEmancipation), 12);
                Assert.Equal(1.0, Dec(w, PolicyAxis.LandReform), 12);
            }
    }

    [Fact]
    public void 旗1なら旧支配層の貯蓄を10倍にしても減らす向きの強さは動かない()
    {
        // 貯蓄（ストック）は地代と所得（フロー）の比に入らない＝rs2 が進める向きで外した型を、減らす向きでも外したことの反証
        double Opp(double savingsMul)
        {
            var w = Board(NationConfig.Russia());
            w.Coef.LordOppositionReverseGain = 1.0;
            w.LandReform = ReformLevel.Full; w.SerfEmancipation = ReformLevel.None;
            foreach (var (_, p) in w.AllPopsWithRegion()) if (p.Closed) p.ApplyBakedSavings(p.Savings.Value * savingsMul);
            return Dec(w, PolicyAxis.LandReform);
        }
        double s1 = Opp(1.0);
        Assert.True(s1 > 1.1, $"盤面の前提が崩れた（全面の土地改革の得が 0・強さ {s1}）");
        Assert.Equal(s1, Opp(10.0), 12);
    }

    [Fact]
    public void 要求の順位は旧支配層の土地改革を減らす要求の強さを得から取り直す()
    {
        // 旗 0 では減らす向きの強さ＝進める向きの強さ（全面の次は無い＝損 0＝1.0）。旗 1 では 2×(0.5＋得)。要求の点＝強さ×可動域÷(1＋FactionTargetsWeakAxis×反対の力) なので、
        // 反対の力と可動域が旗に依らない（旧支配層の項は反対側に入らない）ことを使って、点の比が強さの比に一致することを見る。
        double Score(double flag, out double ratio, out double c)
        {
            var w = Board(NationConfig.Russia());
            w.Coef.LordOppositionReverseGain = flag;
            w.SerfEmancipation = ReformLevel.Full; w.LandReform = ReformLevel.Full;
            ratio = LordRentToIncome(w); c = ExpectedCustom(NationConfig.Russia(), w.Coef);
            var d = FactionSnapshot.ComputeDemands(w, 0, false).Find(x => x.Axis == PolicyAxis.LandReform);
            Assert.True(d.Score > 0, "盤面の前提が崩れた（旧支配層の要求の上位 3 に土地改革が入らない）");
            Assert.False(d.Increase);
            return d.Score;
        }
        double off = Score(0.0, out double ratio, out _);
        double on = Score(1.0, out _, out _);
        double gain = (0.5 - 0.3) / 0.3 * ratio;   // 農奴解放が全面＝戻した後の上限 0.5
        Assert.Equal(2.0 * (0.5 + gain) / 1.0, on / off, 6);
    }

    [Fact]
    public void 純支持は旗1で旧支配層の減らす向きの支持が強まり進める向きは変わらない()
    {
        var w = Board(NationConfig.Russia());
        w.SerfEmancipation = ReformLevel.Full; w.LandReform = ReformLevel.Full;
        w.Coef.LordOppositionReverseGain = 0.0;
        var e0 = PolicyPreferences.Build(w);
        double decOff = PolicyPreferences.NetSupport(w, PolicyAxis.LandReform, false, e0), incOff = PolicyPreferences.NetSupport(w, PolicyAxis.LandReform, true, e0);
        w.Coef.LordOppositionReverseGain = 1.0;
        var e1 = PolicyPreferences.Build(w);
        double decOn = PolicyPreferences.NetSupport(w, PolicyAxis.LandReform, false, e1), incOn = PolicyPreferences.NetSupport(w, PolicyAxis.LandReform, true, e1);
        Assert.Equal(incOff, incOn, 12);
        Assert.True(decOn > decOff + 1e-6, $"減らす向きの純支持が強まらない（旗 0 {decOff}・旗 1 {decOn}）");
    }
    [Fact]
    public void 旗1の減らす向きで内訳の合計は純支持と一致し議会の保守党の項が得に替わる()
    {
        // 呼び手が向きを渡す経路（ParliamentNet・SupportBreakdown）の番人。議会のある盤面（立憲君主制・議席を置く）。
        var w = Board(NationConfig.Russia());
        w.Polity = Polity.ConstitutionalMonarchy; w.ParliamentEnabled = true;
        w.PartySeats[0] = 0.5; w.PartySeats[1] = 0.3; w.PartySeats[2] = 0.2;
        Assert.True(PolicyPreferences.HasLegislature(w.Polity));
        w.SerfEmancipation = ReformLevel.Full; w.LandReform = ReformLevel.Full;
        double ratio = LordRentToIncome(w);
        Assert.True(ratio > 0.2, $"盤面の前提が崩れた（旧支配層の地代÷所得 {ratio}）");
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            w.Coef.LordOppositionReverseGain = flag;
            foreach (bool inc in new[] { false, true })
            {
                var parts = PolicyPreferences.SupportBreakdown(w, PolicyAxis.LandReform, inc);
                Assert.Equal(PolicyPreferences.NetSupport(w, PolicyAxis.LandReform, inc), parts.Sum(x => x.Weight), 9);
            }
        }
        // 保守党の項（減らす向き）：旗 1 では議席×議会の重み×旧支配層の減らす向きの強さ（得の手の式）。旗 0 では全面の損 0＝床 1.0
        double gain = (0.5 - 0.3) / 0.3 * ratio;
        double W = w.Coef.ParliamentLawWeight * w.PartySeats[0];
        double Cons(double flag)
        {
            w.Coef.LordOppositionReverseGain = flag;
            return PolicyPreferences.SupportBreakdown(w, PolicyAxis.LandReform, false).First(x => x.Who == "議会・保守党").Weight;
        }
        Assert.Equal(W * 1.0, Cons(0.0), 9);
        Assert.Equal(W * 2.0 * (0.5 + gain), Cons(1.0), 9);
    }
}
