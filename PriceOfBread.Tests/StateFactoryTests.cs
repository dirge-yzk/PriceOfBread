using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>国営工場カウンター（作者裁定2026-08-10）の番人。
/// 固定するのは4点＝
///   ① カウンターは**意志**＝目標を上げただけで工場は湧かない（既存のゲートが速度を決める）／完成すると現在＝目標になる
///   ② 払い下げは**1棟ずつ**（旧は全部売り）＝カウンターも一緒に下がる
///   ③ 新係数は**休眠既定**＝接収も買い手の不満ゲートも1命令も走らない（現行と厳密恒等）
///   ④ 点火すると「抑圧力＜抵抗なら進まない」「不満な資本家は買わない」「国有化法の対象は売れない」が効く
/// ⑤ 材別の供給率（生産タブの「供給:n%」）は Market.LastRation そのもの＝UI に第二の式を置かない、も併せて固定する。</summary>
public class StateFactoryTests
{
    // FranceTests の国営工場テストと同じ前提（国費で重工業を建てる絶対王政＝厚い国庫。重工業の建設費は素材相場依存）
    private static Scenario Fr(ulong s, double pollTax = 1.0, double treasury = 1_000_000) => new()
    {
        Seed = s,
        StartYear = 1789,
        Kind = ScenarioKind.France,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        FiscalEnabled = true,
        TaxRate = 0.10,
        InitialPollTax = pollTax,
        InitialTreasury = treasury,
        BaseCoercion = 400,
        EducationLaw = EducationLaw.Religious,
        ExternalPressure = 0.1,
    };

    /// <summary>その州で**実際に操業できる**鋼の生産者（技術ゲートが開いている方）。
    /// 鋼は「坩堝鋼（TechOnset=0＝1783年から）」と「高炉・転炉（TechOnset=0.55＝晩期）」の2本立て（Simulation :120-133）で、
    /// 素朴な <c>Find(GoodId==32)</c> は**晩期の方**を掴む。2026-08-11に TechGatedScaleZero を点火するまでは
    /// 未解禁の生産者にも幻の操業規模が立っていたため、その工場でも損益が出て**テストが幻の上に建っていた**。
    /// 国営工場の損益・補助金を測る以上、掴むべきは操業できる工場。</summary>
    /// ★エンジンの動詞（着工・補助金・売却・カウンター）と**同じ選択関数**を通す＝
    /// テストが別の生産者を掴んでいて、通っているように見えるだけ、を構造的に起こさない。
    private static Producer Steel(Region r)
        => World.FindFactory(r, 32)
           ?? throw new Xunit.Sdk.XunitException($"{r.Name}に鋼の生産者がない（技術水準 {r.TechLevel:F2}／坩堝鋼の配線が動いた？）");

    private static Simulation Warm(ulong seed)
    {
        var sim = new Simulation(Fr(seed), new NullAgent());
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;
        sim.World.Coef.InfraBudgetScale = 0;
        sim.World.Coef.ElecInfraBudgetScale = 0;
        sim.Run(40);
        return sim;
    }

    [Fact]
    public void カウンターは意志であって実現ではない()
    {
        var sim = Warm(2025);
        var w = sim.World;
        var p = Steel(w.Regions[0]);
        Assert.Equal(0, p.StateTarget);
        Assert.Equal(0, World.StateUnits(p, w.Coef));

        w.SetStateTarget(0, 32, 1);
        Assert.Equal(1, p.StateTarget);                       // 意志は必ず記録される
        Assert.Equal(0, World.StateUnits(p, w.Coef));         // ★同じ日に工場は湧かない（建設は日数で進む）
        Assert.True(p.ConstructionLeft > 0, "目標を上げても着工が始まらない＝カウンターが動詞に届いていない");

        sim.Run(60);
        Assert.Equal(1, World.StateUnits(p, w.Coef));         // 完成＝現在が目標に追いつく
        Assert.Equal(1, p.StateTarget);                       // 追いついた後は勝手に増えない
    }

    [Fact]
    public void 目標を下げると1棟ずつ払い下げられカウンターも下がる()
    {
        var sim = Warm(2025);
        var w = sim.World;
        var p = Steel(w.Regions[0]);
        // 2棟の国営持分を直に置く（建設の時間を待たずに「複数棟から1棟だけ売る」枝を単離する）
        p.BuiltScale = 2 * w.Coef.FactoryBuildScale;
        p.StateTarget = 2;
        p.LastRevenue = 10_000; p.LastCost = 1_000; p.SmoothedMargin = 0.2;   // 黒字（売却の前提）
        w.Regions[0].Pops[w.Regions[0].InvestorPopIndex].Savings = 1e9;        // 資本家は買える
        double treasury0 = w.Treasury;

        Assert.True(w.SetStateTarget(0, 32, 1), "目標を下げても払い下げが走らない");
        Assert.Equal(1, World.StateUnits(p, w.Coef));          // ★1棟だけ（旧は全部売り＝0棟になっていた）
        Assert.Equal(1, p.StateTarget);
        Assert.True(w.Treasury > treasury0, "払い下げ代金が国庫へ入っていない");
        Assert.True(p.SubsidyRate <= 0 || p.BuiltScale > 1e-9);   // 残る棟があるうちは補助金を外さない

        Assert.True(w.SetStateTarget(0, 32, 0));
        Assert.Equal(0, World.StateUnits(p, w.Coef));
        Assert.Equal(0.0, p.BuiltScale);                       // 売り切ると完全民営化
        Assert.Equal(0.0, p.SubsidyRate);
    }

    [Fact]
    public void 接収は係数0では1命令も走らず旧経路が生きている()
    {
        var sim = Warm(2026);
        var w = sim.World;
        Assert.Equal(0.05, w.Coef.NationalizationSeizureRate);  // ★2026-08-29点火＝既定0.05（R22 batch30＝帯中立・破綻0）
        w.Coef.NationalizationSeizureRate = 0;                  // 0経路（旧・stateFrac下限）の番人として明示OFF
        w.NationalizeMines = true;                              // 鉱山（石炭ほか）を国有化
        var r = w.Regions[0];
        r.NationalizedMask = w.NationalizedMask();
        var p = r.Producers.Find(x => x.GoodId == Catalog.CoalId);
        Assert.NotNull(p);
        double built0 = p!.BuiltScale;
        double chill0 = r.SeizureChill;
        sim.Run(8);
        Assert.Equal(built0, p.BuiltScale);                     // 接収は起きない（BuiltScale は動かない）
        Assert.Equal(chill0, r.SeizureChill);                   // 資本逃避も書かれない
    }

    [Fact]
    // ⚠2026-09-21：旧名「接収は強権が抵抗を上回る時だけ進む」を N1 の一括置換が「政治資本」にしていたが、
    //   この番人が見ているのは World.Seize の suppress（＝抑圧力）であって MandateSpent ではない＝名前を真に戻した。
    public void 接収は抑圧力が抵抗を上回る時だけ進む()
    {
        var sim = Warm(2027);
        var w = sim.World;
        w.Coef.NationalizationSeizureRate = 0.25;               // 点火
        w.NationalizeMines = true;
        var r = w.Regions[0];
        r.NationalizedMask = w.NationalizedMask();
        var p = r.Producers.Find(x => x.GoodId == Catalog.CoalId)!;
        p.BuiltScale = 0;

        // ① 抵抗を抑圧力より大きくする＝1棟も奪えない
        w.Coef.SeizureResistanceWeight = 1e6;
        w.StepStateTargets();
        Assert.Equal(0.0, p.BuiltScale);

        // ② 抵抗を消す＝押し切れる（無補償＝国庫も資本家の貯蓄も動かない）
        w.Coef.SeizureResistanceWeight = 0.0;
        double treasury0 = w.Treasury;
        double savings0 = r.Pops[r.InvestorPopIndex].Savings;
        double mil0 = r.Pops[r.InvestorPopIndex].Militancy;
        w.StepStateTargets();
        Assert.True(p.BuiltScale > 0, "抵抗ゼロでも接収が進まない＝接収が配線されていない");
        Assert.Equal(treasury0, w.Treasury);                    // 補償を払わない＝金は動かない
        Assert.Equal(savings0, r.Pops[r.InvestorPopIndex].Savings);
        Assert.True(r.Pops[r.InvestorPopIndex].Militancy > mil0, "押し切った代償（闘争度）が積まれない");
        Assert.True(r.SeizureChill > 0, "押し切った代償（資本逃避）が積まれない");
        Assert.Equal(World.StateUnits(p, w.Coef), p.StateTarget);   // 接収した持分をカウンターが売り戻さない
    }

    [Fact]
    public void 不満な資本家は工場を買わない()
    {
        var sim = Warm(2028);
        var w = sim.World;
        var r = w.Regions[0];
        var p = Steel(r);
        p.BuiltScale = w.Coef.FactoryBuildScale;
        p.LastRevenue = 10_000; p.LastCost = 1_000; p.SmoothedMargin = 0.2;
        r.Pops[r.InvestorPopIndex].Savings = 1e9;
        r.Pops[r.InvestorPopIndex].Militancy = 0.6;

        Assert.Equal(0.0, w.Coef.SoeSaleMilitancyCeil);         // 既定＝休眠＝現行どおり買える
        Assert.True(w.CanSellFactory(0, 32).Ok);

        w.Coef.SoeSaleMilitancyCeil = 0.3;                      // 点火
        var no = w.CanSellFactory(0, 32);
        Assert.False(no.Ok);
        Assert.Equal("資本家が政府に不満", no.Reason);          // 理由が読める（売れない月を無言にしない）
        Assert.False(w.SellFactory(0, 32));
        Assert.Equal(w.Coef.FactoryBuildScale, p.BuiltScale);   // 拒否は副作用を残さない
    }

    [Fact]
    public void 国有化法の対象部門は払い下げられない()
    {
        var sim = Warm(2029);
        var w = sim.World;
        var r = w.Regions[0];
        var p = r.Producers.Find(x => x.GoodId == Catalog.CoalId)!;
        p.BuiltScale = w.Coef.FactoryBuildScale;
        p.LastRevenue = 10_000; p.LastCost = 1_000; p.SmoothedMargin = 0.2;
        r.Pops[r.InvestorPopIndex].Savings = 1e9;
        Assert.True(w.CanSellFactory(0, Catalog.CoalId).Ok);    // 法がなければ売れる

        w.NationalizeMines = true;
        r.NationalizedMask = w.NationalizedMask();
        var no = w.CanSellFactory(0, Catalog.CoalId);
        Assert.False(no.Ok);
        Assert.Equal("国有化法の対象", no.Reason);
    }

    [Fact]
    public void 国営持分は補助金の対象外()
    {
        var sim = Warm(2031);
        var w = sim.World;
        var p = Steel(w.Regions[0]);

        // ① 民営100%＝従来どおり設定できる（厳密恒等）
        w.SetSubsidyRate(0, 32, 1.0);
        Assert.Equal(1.0, p.SubsidyRate);
        var all = w.CanSubsidize(0, 32);
        Assert.True(all.Ok);
        Assert.Equal(1.0, all.PrivateShare, 9);

        // ② 半分だけ国営＝**崖を作らない**（1棟建てただけでボタンが死なない）。効くのは民営持分だけ
        p.Scale = 2.0; p.BuiltScale = 1.0;
        var half = w.CanSubsidize(0, 32);
        Assert.True(half.Ok);
        Assert.Equal(0.5, half.PrivateShare, 9);

        // ③ 国営100%＝対象外。プレイヤーの動詞もAIの動詞（SelfPlay の SetSubsidy）も同じここを通る＝対称
        p.BuiltScale = p.Scale;
        var no = w.CanSubsidize(0, 32);
        Assert.False(no.Ok);
        Assert.Equal("国営100%", no.Reason);
        w.SetSubsidyRate(0, 32, 1.0);
        Assert.Equal(0.0, p.SubsidyRate);
        w.SetSubsidy(0, 32, true);
        Assert.Equal(0.0, p.SubsidyRate);
    }

    [Fact]
    public void 国営100パーセントになった工場の補助金は月次で外れる()
    {
        var sim = Warm(2032);
        var w = sim.World;
        var p = Steel(w.Regions[0]);
        w.SetSubsidyRate(0, 32, 1.0);
        Assert.Equal(1.0, p.SubsidyRate);
        p.BuiltScale = p.Scale;                          // 建設が完成して国営100%になった
        p.StateTarget = World.StateUnits(p, w.Coef);     // カウンターは現在値＝払い下げも着工も走らせない
        w.StepStateTargets();
        Assert.Equal(0.0, p.SubsidyRate);                // 効かない値を記録に残さない（払い下げた瞬間に復活しない）
    }

    /// <summary>全生産者の Scale・平滑採算・国庫の合計（2本の世界が同じ道を通ったかの物差し）。</summary>
    private static (double Scale, double Margin, double Treasury) Totals(Simulation s)
    {
        double sc = 0, mg = 0;
        foreach (var r in s.World.Regions)
            foreach (var p in r.Producers) { sc += p.Scale; mg += p.SmoothedMargin; }
        return (sc, mg, s.World.Treasury);
    }

    [Fact]
    public void 補助金は国営持分の採算を底上げしない()
    {
        // ★作者裁定2026-08-10「国営工場は常に補助金を出しているようなもの＝補助金の対象外」。
        //   国営持分の赤字は既に国庫が全額負っている（stateFrac 経路）ので、そこへ補助金を重ねるのは同じ支援の二重計上。
        //   会計は元から民営持分にしか乗っていなかったが、**採算の底上げ**だけが持分を無視して全量に効いていた。
        // 国営持分を **ScaleMax** に置く＝Scale の上限＝下限なので操業規模がそこへ張り付き、走行中ずっと stateFrac=1 が保たれる
        //（Scale と同値に置くだけだと、黒字の生産者が Scale を伸ばした瞬間に民営持分が生まれて条件が崩れる）。
        static Simulation Make(ulong seed, double subsidy, bool stateOwned)
        {
            var s = Warm(seed);
            foreach (var r in s.World.Regions)
                foreach (var p in r.Producers)
                {
                    if (stateOwned) { p.BuiltScale = s.World.Coef.ScaleMax; p.StateTarget = World.StateUnits(p, s.World.Coef); }
                    p.SubsidyRate = subsidy;
                }
            s.Run(4);
            return s;
        }
        // 国営100%＝補助金を満額にしても世界は1ミリも変わらない（国庫も操業も平滑採算も同じ道）
        var stOn = Totals(Make(2033, 1.0, stateOwned: true));
        var stOff = Totals(Make(2033, 0.0, stateOwned: true));
        Assert.Equal(stOff.Treasury, stOn.Treasury, 6);
        Assert.Equal(stOff.Scale, stOn.Scale, 9);
        Assert.Equal(stOff.Margin, stOn.Margin, 9);
        // 対照＝民営100%なら補助金はちゃんと効く（この番人が空洞でないことの証拠）
        var prOn = Totals(Make(2033, 1.0, stateOwned: false));
        var prOff = Totals(Make(2033, 0.0, stateOwned: false));
        Assert.True(System.Math.Abs(prOn.Treasury - prOff.Treasury) > 1e-6,
            $"民営100%でも補助金が国庫を動かさない＝対照が空（{prOn.Treasury:F3} vs {prOff.Treasury:F3}）");
    }

    /// <summary>RGO（農園・鉱山・牧場）の国営持分・能力・産出の合計。</summary>
    private static (double Built, double Scale, double Output) Rgo(Simulation s)
    {
        double b = 0, sc = 0, op = 0;
        foreach (var r in s.World.Regions)
            foreach (var p in r.Producers) if (p.IsRgo) { b += p.BuiltScale; sc += p.Scale; op += p.Output; }
        return (b, sc, op);
    }

    [Fact]
    public void 自動拡張は止まりRGOの能力は痩せない()
    {
        // ★作者裁定2026-08-10「国営工場に自動的な拡張なし」。⚠是正2026-08-30：この裁定の点火は実際には2026-08-30まで行われていなかった（帰還構造調査§1-3）＝現在は既定1.0で点火済み。
        //   止めた経路＝資本家の建設予算が最も採算の良いRGOの BuiltScale（＝Scaleの下限であり、同時に国営持分）を押し上げていたもの。
        //   ⚠副作用の番人＝止めたせいでRGOの能力（Scale）と産出が痩せないこと。能力は採算駆動（scaleTarget=1+margin）で決まり、
        //     BuiltScale はラチェットを与えていただけ、という読みをここで固定する。
        static Simulation RunSim(ulong seed, double counterOnly, int quarters)
        {
            var sim = new Simulation(Fr(seed), new NullAgent());
            sim.World.Coef.StateShareCounterOnly = counterOnly;
            sim.Run(quarters);
            return sim;
        }
        var on = Rgo(RunSim(2034, 1.0, 120));    // 点火（現行の既定）
        var off = Rgo(RunSim(2034, 0.0, 120));   // 旧経路（対照）
        Assert.Equal(0.0, on.Built);             // ★誰も建てていないのに国営持分が湧かない
        Assert.True(off.Built > 0, "対照でも国営持分が増えない＝この番人は何も測っていない（旧経路が動いていない）");
        Assert.True(on.Scale > off.Scale * 0.9, $"自動拡張を止めたらRGOの能力が痩せた（点火 {on.Scale:F1} vs 旧 {off.Scale:F1}）");
        // 産出は能力×稼働＝稼働は需要と作柄（1か月の断面）で動く。2本の軌道は120四半期で分かれるので、産出の比は
        //   国内価格の刻み h を 1/2/3/5/6/10/15 と振ると 0.93/1.31/0.74/0.86/0.99/0.75/0.81 と非単調に散る（能力の比は 0.99〜1.01 で安定）。
        //   2026-09-26 ㊽：産出の線を 0.9→0.5 へ＝「痩せる」（能力が残っても産出が半分を割る）だけを見る。能力の線（上の 0.9）は据え置き。
        Assert.True(on.Output > off.Output * 0.5, $"自動拡張を止めたらRGOの産出が痩せた（点火 {on.Output:F0} vs 旧 {off.Output:F0}）");
    }

    [Fact]
    public void 国営持分の損益は国庫が受け取り国庫が負う()
    {
        // 作者裁定2026-08-10の1行目「利益・損失は国庫に」＝EconomyStep の stateFrac 経路が現物でそうなっているかの確認。
        // 同じ世界を「国営あり／なし」で比べる＝国営持分の損益だけが国庫の差になる（支えているのは補助金ではない）。
        var withState = Warm(2035);
        {
            var p = Steel(withState.World.Regions[0]);
            p.BuiltScale = p.Scale;
            p.StateTarget = World.StateUnits(p, withState.World.Coef);
        }
        var without = Warm(2035);
        double t0w = withState.World.Treasury, t0n = without.World.Treasury;
        withState.Run(4); without.Run(4);
        Assert.NotEqual(withState.World.Treasury - t0w, without.World.Treasury - t0n);   // 損益が国庫を通っている（同額なら経路が死んでいる）
        Assert.Equal(0.0, Steel(withState.World.Regions[0]).SubsidyRate);   // 支えているのは補助金ではない
    }

    [Fact]
    public void 材別の供給率は市場の配給率そのもの()
    {
        var sim = Warm(2030);
        var w = sim.World;
        var r = w.Region;
        var c = ProductionDiag.ExplainCost(w, r, 32);            // 鋼＝素材と労働の両方を投入する工場
        Assert.NotEmpty(c.Items);
        foreach (var it in c.Items)
        {
            double expected = System.Math.Clamp(r.Markets[it.GoodId].LastRation, 0, 1);
            Assert.Equal(expected, it.Fill);                     // UI が読む値＝エンジンの記帳そのもの（第二の式を置かない）
            Assert.InRange(it.Fill, 0.0, 1.0);
        }
    }

    // ===== 全国工場目標（作者要望2026-09-07 (1)）＝財カードの分母。満たすまで最も効率の良い州へ月に1棟ずつ =====

    /// <summary>その財の全州の国営目標が全て0か（展開の前提と、他州が動いていないことの検査に使う）。</summary>
    private static void 全州の意志は0(World w, int goodId)
    {
        foreach (var r in w.Regions)
            if (World.FindFactory(r, goodId) is { } p)
                Assert.Equal(0, p.StateTarget);
    }

    [Fact]
    public void 全国工場目標は既定では1命令も走らない()
    {
        var sim = Warm(2040);
        var w = sim.World;
        Assert.Empty(w.GoodFactoryTarget);        // 既定＝配列そのものが無い
        w.SetFactoryTarget(32, 0);                // 0 を置いても確保しない＝厳密恒等
        Assert.Empty(w.GoodFactoryTarget);
        全州の意志は0(w, 32);
        w.StepStateTargets();
        全州の意志は0(w, 32);
    }

    [Fact]
    public void 全国工場目標は最も効率の良い州に意志を立てる()
    {
        var sim = Warm(2041);
        var w = sim.World;
        全州の意志は0(w, 32);
        var (st0, pv0) = w.NationFactoryUnits(32);
        w.SetFactoryTarget(32, st0 + pv0 + 1);
        // 期待する州は**エンジンと同じ物差し**（AIの着工判断が使う1本）で決める＝テストが第二の式を持たない
        int expect = -1; double best = double.NegativeInfinity;
        for (int ri = 0; ri < w.Regions.Count; ri++)
        {
            var r = w.Regions[ri];
            if (r.State != RegionState.Active) continue;
            var p = World.FindFactory(r, 32);
            if (p is null || p.ConstructionLeft > 0 || p.Bankrupt) continue;
            double sc = UtilityAgent.FacilityBuildScore(w, r, p).Score;
            if (sc > best) { best = sc; expect = ri; }
        }
        Assert.True(expect >= 0, "鋼を作れる州が1つも無い＝ハーネスが空回りしている");
        w.StepStateTargets();
        var pe = World.FindFactory(w.Regions[expect], 32)!;
        Assert.True(pe.StateTarget > World.StateUnits(pe, w.Coef), $"{w.Regions[expect].Name}に意志が立っていない");
        for (int ri = 0; ri < w.Regions.Count; ri++)   // 1財1か月1歩＝他州は動かない
            if (ri != expect && World.FindFactory(w.Regions[ri], 32) is { } q)
                Assert.Equal(0, q.StateTarget);
    }

    [Fact]
    public void 全国工場目標に達したら意志を立てない()
    {
        var sim = Warm(2042);
        var w = sim.World;
        全州の意志は0(w, 32);
        var (st, pv) = w.NationFactoryUnits(32);
        w.SetFactoryTarget(32, st + pv);   // 現在＝目標
        w.StepStateTargets();
        全州の意志は0(w, 32);
    }

    [Fact]
    public void 全国工場目標を下げても既存の棟は壊れない()
    {
        var sim = Warm(2043);
        var w = sim.World;
        var p = Steel(w.Regions[0]);
        p.BuiltScale = w.Coef.FactoryBuildScale * 2;          // 国営2棟が既に建っている
        p.StateTarget = World.StateUnits(p, w.Coef);
        w.SetFactoryTarget(32, 1);                            // 全国目標を現在より下へ
        w.StepStateTargets();
        Assert.Equal(2, World.StateUnits(p, w.Coef));         // 取り壊さない（払い下げは州別カウンターだけの動詞）
        Assert.Equal(2, p.StateTarget);
    }

    [Fact]
    public void 未達の意志が残る間は全国目標を広げない()
    {
        var sim = Warm(2044);
        var w = sim.World;
        全州の意志は0(w, 32);
        var p0 = Steel(w.Regions[0]);
        p0.StateTarget = World.StateUnits(p0, w.Coef) + 1;    // 前に立てた意志が未実現
        var (st, pv) = w.NationFactoryUnits(32);
        w.SetFactoryTarget(32, st + pv + 5);
        w.StepStateTargets();
        for (int ri = 1; ri < w.Regions.Count; ri++)          // 意志は溜めない＝他州へ広がらない
            if (World.FindFactory(w.Regions[ri], 32) is { } q)
                Assert.Equal(0, q.StateTarget);
    }

    [Fact]
    public void 全国の棟数は州ごとの棟数の和()
    {
        var sim = Warm(2045);
        var w = sim.World;
        int st = 0, pv = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (p.GoodId != 32) continue;
                if (!(ProductionDiag.TechGate(32, p.TechOnset, r.TechLevel) > ProductionDiag.TechGateClosed)) continue;
                st += World.StateUnits(p, w.Coef);
                if (!p.Bankrupt) pv += World.PrivateUnits(p, w.Coef);
            }
        }
        Assert.Equal((st, pv), w.NationFactoryUnits(32));   // カードの分子＝全国工場目標の分子（式は1本）
    }
}
