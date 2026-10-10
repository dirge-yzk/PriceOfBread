using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★作者要望2026-08-17「**金のないPOPほど買収しやすい・汚職に手を染めやすい**」のON経路。
///
/// <para>A＝買収しやすさ（<see cref="Coefficients.ElectionBribePovertyWeight"/>・既定0＝休眠）：
/// 半減点の単価を「全国一律の一人あたり名目所得」から「その有権者pop自身の前月所得」へブレンドする。</para>
/// <code>
/// half ＝ 係数 × Σ_pop [ 声_p × ( (1−w)×規模_p + w × max(0,前月所得_p) ÷ 全国の1人あたり所得 ) ] × (名目GDP÷人口)
/// </code>
///
/// <para>B＝汚職に手を染めやすさ（<see cref="Coefficients.CorruptionPoorOfficialWeight"/>・既定0＝休眠）：</para>
/// <code>
/// 実効汚職 ＝ (1−行政)×(1−監視) × ( 1 + 重み × max(0, 1 − 官僚の生活の充足) )
/// 官僚の生活の充足 ＝ Σ_官僚 前月満足度×規模 ÷ Σ_官僚 規模        （[0,1]・World.OfficialWelfareFill）
/// </code>
/// <para>⚠物差しは俸給比でなく**満足度**（作者裁定）＝貧しい国では「官僚所得÷平均所得」が1.0でも官僚は飢えている。
/// ⚠読むのは**前月**（<c>Pop.LastSatisfaction</c>）＝汚職→貯蓄→消費→満足度→汚職 の環を同一ティックで閉じない。</para>
///
/// ここで固定するもの：(a)両係数の既定0の番人 (b)貧しい選挙民ほど半減点が小さい (c)0でビット同値・1で完全pop別
/// (d)官僚の暮らしが苦しいほど実効汚職が高い（単調・規模加重） (e)前月値を読む（同一ティックで閉路を作らない）
/// (f)官僚不在・NaN・負値は恒等側へ倒れる (g)上限1を壊さない (h)両係数ONでの決定論。
/// </summary>
public class PoorPopBriberyTests
{
    // ============ 盤面と物差し ============

    /// <summary>単一国（英＝制限選挙）。NullAgent＝popも法も勝手に動かない＝式だけを見る軽い盤面（ElectionAiTests と同じ作法）。</summary>
    private static Simulation ElectiveWorld(ulong seed)
        => new(WorldData.ByName("イギリス").ToScenario(seed), new NullAgent());

    /// <summary>世論・所得が立った盤面（公示の四半期＝15 は跨がない）。</summary>
    private static World Board(ulong seed, int quarters = 6)
    {
        var sim = ElectiveWorld(seed);
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return sim.World;
    }

    /// <summary>有権者数＝Σ 規模×FranchiseVoice（<see cref="Parliament"/> の ComputeVotes が議席を数えるのと**同じ重み**）。</summary>
    private static double VotersOf(World w)
    {
        double v = 0;
        foreach (var p in w.AllPops()) v += p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
        return v;
    }

    /// <summary>全国一律の半減点（＝富加重0のときの式）をテスト側で独立に組み直す。</summary>
    private static double UniformHalf(World w)
        => (w.Coef.ElectionPushHalfPerVoter * VotersOf(w) * (w.NominalGdpPerMonth / w.TotalPopulation)).Value;

    /// <summary>富加重 w の半減点をテスト側で独立に組み直す（式が変わればこの番人が落ちる）。</summary>
    private static double BlendedHalf(World w, double bribeW)
    {
        double incSum = 0, sizeSum = 0;
        foreach (var p in w.AllPops()) { incSum += Math.Max(0, p.LastEarnedPerMonth.Value); sizeSum += p.Size; }
        double mean = incSum / sizeSum;
        double weighted = 0;
        foreach (var p in w.AllPops())
        {
            double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (!(voice > 0)) continue;
            weighted += voice * ((1.0 - bribeW) * p.Size + bribeW * (Math.Max(0, p.LastEarnedPerMonth.Value) / mean));
        }
        return (w.Coef.ElectionPushHalfPerVoter * weighted * (w.NominalGdpPerMonth / w.TotalPopulation)).Value;
    }

    /// <summary>汚職の盤面。★監視を中庸（検閲0.4＋資格任用0.6＝noisy-or 0.76）に固定して
    /// コア＝(1−行政)×(1−監視) を小さく保つ＝上乗せが**上限1のクランプに触れない**＝単調性を素で見られる。
    /// ⚠法は走行後に置く（走行中にAIや政治が法を動かしても観測時点の値が確定する）。</summary>
    private static (MultiNationSim Sim, World W) CorruptionBoard(ulong seed)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed);
        var w = sim.Nations[0].World;
        sim.StartWarmup();
        sim.StepQuarter();
        w.Censorship = CensorshipLaw.Censorship;      // 監視0.4
        w.Bureaucracy = BureaucracyLaw.MeritExam;     // 監視0.6
        w.ParliamentControl = 0;                      // 議会の予算権なし
        return (sim, w);
    }

    /// <summary>官僚pop（兵士は除く＝兵の充足は即応度が読む別の物差し）。</summary>
    private static List<Pop> BureaucratsOf(World w)
    {
        var list = new List<Pop>();
        foreach (var p in w.AllPops()) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier && p.Size > 0) list.Add(p);
        return list;
    }

    private static void SetLastSatisfaction(List<Pop> pops, double v) { foreach (var p in pops) p.LastSatisfaction = v; }

    /// <summary>上乗せの無いコア（旧式そのもの）。</summary>
    private static double CoreOf(World w) => (1.0 - w.AdminEfficiency) * (1.0 - w.OversightV2);

    /// <summary>相対許容での一致（金額は桁が大きい＝絶対桁指定の Assert.Equal は使えない）。</summary>
    private static void AssertClose(double expect, double actual, string what, double rel = 1e-9)
        => Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"{what}：期待 {expect} ／ 実際 {actual}");

    // ============ (a) 既定0の番人 ============

    [Fact]
    public void 両係数の既定は休眠()
    {
        // ⚠POB_COEF で明示上書きした測定走行では既定値を見ない（点火A/Bを同一バイナリで取るため・ElectionAiTests と同じ作法）
        string ov = Coefficients.OverrideSummary();
        var c = new Coefficients();
        if (!ov.Contains("ElectionBribePovertyWeight", StringComparison.Ordinal))
            Assert.Equal(0.0, c.ElectionBribePovertyWeight);
        if (!ov.Contains("CorruptionPoorOfficialWeight", StringComparison.Ordinal))
            Assert.Equal(0.0, c.CorruptionPoorOfficialWeight);
    }

    // ============ (b) A：貧しい選挙民ほど安く買える ============

    [Fact]
    public void A_有権者が貧しい国ほど半減点が小さく同じ投入で効きが大きい()
    {
        var w = Board(5);
        w.Coef.ElectionBribePovertyWeight = 1.0;
        // ★(205) 2026-10-04 盤面の前提（裁量余剰）を明示で置く＝この番人は買収の効きの比だけを見る（余剰の大きさは比に効かない）。
        //   旧の盤面は 1784 年の英（NullAgent）が r 0.92〜0.96 の8か月で不履行になり国債が消えて余剰が立っていた＝不履行の計数を払えなかった大きさの累計にした (205) では
        //   累計 0.70（6四半期目）で不履行にならず国債が残る（利払い 2,657 ＞ 歳入 1,854）＝余剰0。
        if (!(w.FiscalCapacityPerMonth.Value > 0)) w.FiscalCapacityPerMonth = w.LastRevenuePerMonth;

        // 有権者（声>0）と非有権者（声=0）を取り、**総所得を変えずに**所得を有権者→非有権者へ移す。
        // ＝全国の1人あたり所得（ブレンドの分母）も名目GDPも人口も1ビットも動かない＝動くのは「有権者が豊かか」だけ。
        Pop? voter = null, outsider = null;
        foreach (var p in w.AllPops())
        {
            double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (voice > 0 && p.LastEarnedPerMonth.Value > 0 && (voter is null || p.LastEarnedPerMonth > voter.LastEarnedPerMonth)) voter = p;
            if (!(voice > 0) && outsider is null) outsider = p;
        }
        Assert.True(voter is not null, "前提の破れ：所得のある有権者popが居ない");
        Assert.True(outsider is not null, "前提の破れ：非有権者popが居ない（普通選挙の盤面では所得移転の相手が作れない）");

        double half0 = w.ElectionPushHalfPoint();
        double eff0 = w.ElectionPushEffectOf(0.5);
        Assert.True(half0 > 0 && eff0 > 0, "前提の破れ：半減点も効きも立たない盤面");

        double move = (0.5 * voter!.LastEarnedPerMonth).Value;
        voter.LastEarnedPerMonth -= new MoneyPerMonth(move); outsider!.LastEarnedPerMonth += new MoneyPerMonth(move);

        double half1 = w.ElectionPushHalfPoint();
        double eff1 = w.ElectionPushEffectOf(0.5);
        Assert.True(half1 < half0, $"有権者が貧しくなったのに半減点が下がらない（{half0} → {half1}）＝票の値段が稼ぎで決まっていない");
        Assert.True(eff1 > eff0, $"同じ投入額（裁量余剰の50%）で効きが増えない（{eff0} → {eff1}）＝安く買えていない");

        // 対照：全国一律（既定0）では**同じ所得移転で半減点が1ビットも動かない**＝動かしたのは富加重だけ
        w.Coef.ElectionBribePovertyWeight = 0.0;
        double flatPoor = w.ElectionPushHalfPoint();
        voter.LastEarnedPerMonth += new MoneyPerMonth(move); outsider.LastEarnedPerMonth -= new MoneyPerMonth(move);
        Assert.Equal(flatPoor, w.ElectionPushHalfPoint());
    }

    // ============ (c) A：0でビット同値・1で完全pop別 ============

    [Fact]
    public void A_ブレンド0は現行とビット同値で1は完全pop別()
    {
        var w = Board(5);

        // ① 0＝否定形ガードで即 return＝全国一律の現行式そのもの（丸め桁を指定しない＝**ビット同値**）
        w.Coef.ElectionBribePovertyWeight = 0.0;
        double uniform = UniformHalf(w);
        Assert.True(uniform > 0, "前提の破れ：有権者も所得も無い盤面");
        Assert.Equal(uniform, w.ElectionPushHalfPoint());

        // ② 1＝完全にpop別（テスト側で独立に組み直した式とビット同値）
        w.Coef.ElectionBribePovertyWeight = 1.0;
        double popWise = BlendedHalf(w, 1.0);
        Assert.Equal(popWise, w.ElectionPushHalfPoint());
        Assert.True(Math.Abs(popWise - uniform) > 1e-6 * Math.Max(1.0, uniform),
            $"0と1で半減点が同じ（{uniform}）＝ブレンドが空虚（有権者の1人あたり所得が全国平均と偶然一致した盤面）");
        // 制限選挙の英＝有権者は富裕層＝pop別にすると票は**高くなる**（狙った向き＝金のない者ほど安い）
        Assert.True(popWise > uniform, $"制限選挙で有権者（富裕層）の票が安くなっている（{uniform} → {popWise}）");

        // ③ 中間は両端の内分（形が線形ブレンドであることの固定）
        w.Coef.ElectionBribePovertyWeight = 0.5;
        Assert.Equal(BlendedHalf(w, 0.5), w.ElectionPushHalfPoint());
        AssertClose(0.5 * (uniform + popWise), w.ElectionPushHalfPoint(), "ブレンド0.5＝両端の内分");
    }

    // ============ (d) B：官僚の暮らしが苦しいほど実効汚職が高い ============

    [Fact]
    public void B_官僚の暮らしが苦しい国ほど実効汚職が高い()
    {
        var (_, w) = CorruptionBoard(7);
        var bur = BureaucratsOf(w);
        Assert.True(bur.Count >= 2, $"前提の破れ：官僚popが{bur.Count}個（規模加重を見るには2個以上要る）");
        double core = CoreOf(w);
        Assert.True(core > 1e-9 && core * 1.5 < 1.0, $"前提の破れ：コア={core}（上限1のクランプに触れない盤面が要る）");
        w.Coef.CorruptionPoorOfficialWeight = 0.5;

        // ① 充足＝1.0（欲しい財を全部買えている）＝上乗せゼロ＝コアと**ビット同値**（否定形ガードで即 return）
        SetLastSatisfaction(bur, 1.0);
        Assert.Equal(1.0, w.OfficialWelfareFill);
        Assert.Equal(core, w.EffectiveCorruption);

        // ② 単調：暮らしが苦しくなるほど漏れが増える
        SetLastSatisfaction(bur, 0.8); double c8 = w.EffectiveCorruption;
        SetLastSatisfaction(bur, 0.5); double c5 = w.EffectiveCorruption;
        SetLastSatisfaction(bur, 0.2); double c2 = w.EffectiveCorruption;
        Assert.True(core < c8 && c8 < c5 && c5 < c2, $"単調でない（{core} {c8} {c5} {c2}）＝薄給の官僚が汚職に手を染めていない");

        // ③ 式そのもの：コア×(1+重み×(1−充足))
        SetLastSatisfaction(bur, 0.5);
        Assert.Equal(0.5, w.OfficialWelfareFill, 12);
        AssertClose(core * (1.0 + 0.5 * 0.5), w.EffectiveCorruption, "実効汚職＝コア×(1+重み×不足)");

        // ④ 規模加重：同じ「1popだけ困窮」でも、**人数の多い官僚層**が困窮するほうが実効汚職は高い
        SetLastSatisfaction(bur, 1.0);
        Pop big = bur[0], small = bur[0];
        foreach (var p in bur) { if (p.Size > big.Size) big = p; if (p.Size < small.Size) small = p; }
        Assert.True(big.Size > small.Size, "前提の破れ：官僚popの規模が全て同じ（規模加重を区別できない）");
        small.LastSatisfaction = 0.0; double cSmall = w.EffectiveCorruption; small.LastSatisfaction = 1.0;
        big.LastSatisfaction = 0.0; double cBig = w.EffectiveCorruption; big.LastSatisfaction = 1.0;
        Assert.True(cBig > cSmall, $"規模で加重していない（大pop困窮{cBig} <= 小pop困窮{cSmall}）");
    }

    // ============ (e) B：読むのは前月の満足度（同一ティックの閉路の禁止） ============

    [Fact]
    public void B_読むのは前月の満足度で同一ティックの閉路を作らない()
    {
        var (sim, w) = CorruptionBoard(11);
        var bur = BureaucratsOf(w);
        Assert.NotEmpty(bur);
        w.Coef.CorruptionPoorOfficialWeight = 0.5;
        SetLastSatisfaction(bur, 0.9);
        double before = w.EffectiveCorruption;
        Assert.True(before > CoreOf(w), "前提の破れ：上乗せが立っていない");

        // ① **当月**の満足度を最悪にしても同じティックでは1ビットも動かない
        //    ＝汚職の分け前→貯蓄→消費→満足度→汚職 の環を同一ティックで閉じていない証拠
        foreach (var p in bur) p.Satisfaction = 0.0;
        Assert.Equal(before, w.EffectiveCorruption);

        // ② 前月値を書けば動く＝読んでいるのは確かに LastSatisfaction（①が「そもそも読んでいない」ではない）
        SetLastSatisfaction(bur, 0.1);
        Assert.True(w.EffectiveCorruption > before, "前月の満足度を下げても実効汚職が動かない＝配線切れ");

        // ③ 月初の退避（EconomyStep の1行）が**毎月走っている**＝番兵を置いて1四半期回すと必ず[0,1]へ戻る
        //    ⚠走行中は休眠に戻す＝観測が挙動を変えない／⚠退避は EconomyStep.Run の中＝**Active な州だけ**が対象
        w.Coef.CorruptionPoorOfficialWeight = 0.0;
        foreach (var p in w.AllPops()) p.LastSatisfaction = -7.0;
        sim.StepQuarter();
        int seen = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                seen++;
                Assert.True(p.LastSatisfaction >= 0.0 && p.LastSatisfaction <= 1.0,
                    $"前月満足度の退避が走っていないpopがある（{p.ClassName}＝{p.LastSatisfaction}）");
            }
        }
        Assert.True(seen > 0, "前提の破れ：Activeな州が1つも無い");
    }

    // ============ (f) B：基準が立たない世界は恒等側へ倒れる ============

    [Fact]
    public void B_官僚不在とNaNと負値は恒等側へ倒れる()
    {
        var (_, w) = CorruptionBoard(13);
        var bur = BureaucratsOf(w);
        Assert.NotEmpty(bur);
        w.Coef.CorruptionPoorOfficialWeight = 0.7;
        double core = CoreOf(w);

        // ① 官僚が1人も居ない＝基準が立たない＝1.0（上乗せなし・罰さない）
        //    ⚠コアは**その場で**組み直す：官僚を消すと AdminEfficiency（人口加重の行政充足）自体が動く＝
        //      消す前のコアと比べると「上乗せの有無」でなく「行政効率の変化」を測ってしまう（実測で踏んだ）。
        var sizes = new double[bur.Count];
        for (int i = 0; i < bur.Count; i++) { sizes[i] = bur[i].Size; bur[i].Size = 0; }
        Assert.Equal(1.0, w.OfficialWelfareFill);
        Assert.Equal(CoreOf(w), w.EffectiveCorruption);
        for (int i = 0; i < bur.Count; i++) bur[i].Size = sizes[i];
        Assert.Equal(core, CoreOf(w));   // 復元でコアが戻る＝以降は固定のコアと比べてよい

        // ② NaN＝否定形ガードで恒等側（Math.Clamp はNaNを消さない＝絶対に Clamp で書かない）
        SetLastSatisfaction(bur, double.NaN);
        Assert.Equal(1.0, w.OfficialWelfareFill);
        Assert.Equal(core, w.EffectiveCorruption);

        // ③ あり得ない負値でも発散しない（同じ否定形ガード）
        SetLastSatisfaction(bur, -5.0);
        Assert.Equal(1.0, w.OfficialWelfareFill);
        Assert.Equal(core, w.EffectiveCorruption);

        // ④ 係数0＝官僚が飢えていても1ビットも動かない（休眠＝経路を作らない）
        SetLastSatisfaction(bur, 0.0);
        w.Coef.CorruptionPoorOfficialWeight = 0.0;
        Assert.Equal(core, w.EffectiveCorruption);
    }

    // ============ (g) B：上限1を壊さない ============

    [Fact]
    public void B_上乗せは実効汚職の上限1を壊さない()
    {
        var (_, w) = CorruptionBoard(17);
        var bur = BureaucratsOf(w);
        Assert.NotEmpty(bur);
        Assert.True(CoreOf(w) > 1e-9, "前提の破れ：コアが0＝上乗せの掛かる先が無い盤面");
        SetLastSatisfaction(bur, 0.0);            // 無給同然の官僚（清の胥吏）
        w.Coef.CorruptionPoorOfficialWeight = 100.0;   // 極端な重み
        Assert.Equal(0.0, w.OfficialWelfareFill);
        Assert.Equal(1.0, w.EffectiveCorruption);      // 0..1 の外へ出ない＝CorruptionMax が漏れの上限である意味が壊れない
    }

    // ============ (h) 決定論 ============

    [Fact]
    public void 両係数ONでも同シードは同一の最終状態を生む()
    {
        Assert.Equal(Digest(4242), Digest(4242));

        static string Digest(ulong seed)
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("アメリカ")], seed);
            foreach (var n in sim.Nations)
            {
                n.World.Coef.ElectionBribePovertyWeight = 1.0;
                n.World.Coef.CorruptionPoorOfficialWeight = 0.5;
            }
            sim.StartWarmup();
            for (int q = 0; q < 16; q++) sim.StepQuarter();
            var sb = new System.Text.StringBuilder();
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                sb.Append(w.Treasury.ToString("R")).Append('|')
                  .Append(w.TotalMoney.ToString("R")).Append('|')
                  .Append(w.TotalPopulation.ToString("R")).Append('|')
                  .Append(w.EffectiveCorruption.ToString("R")).Append('|')
                  .Append(w.OfficialWelfareFill.ToString("R")).Append('|')
                  .Append(w.ElectionPushHalfPoint().ToString("R")).Append('|')
                  .Append(w.VoteShares[0].ToString("R")).Append(';');
            }
            return sb.ToString();
        }
    }
}
