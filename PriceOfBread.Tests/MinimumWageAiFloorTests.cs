using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(480) 線 ma：AI が最低賃金法に置く目標を、盤面の実物（本国の労働の失業と、床が縛る広がり）から選ぶ形（旗 <see cref="Coefficients.MinimumWageAiFromFloor"/>・★(486) から既定 1）の番人。
/// 事前登録 measurements/ma480_20261021_prereg.txt [2] F2 と [8] G6。
/// 期待値は実装と同じ出所から作らない：床・籠・能力・失業の期待値はテストの中で（財の層の序数・生産者の列挙・POP の人数）から手で組む。
/// 線の値（失業 1/4・1/2・縛りの広がり 1/3・1/2・縛りの読み 1.05）は AI の選び（確証なし）＝ここではリテラルで書く。</summary>
public class MinimumWageAiFloorTests
{
    private static Simulation Build()
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);
        s.World.Coef.MinimumWageLawGate = 1;
        return s;
    }

    private static System.Collections.Generic.List<Region> Home(World w)
        => w.Regions.Where(r => r.State == RegionState.Active && r.Tier != RegionTier.Colonial).ToList();

    /// <summary>籠（1 人・月・名目）の手の参照：層の序数（主食 1・日用品 2・嗜好品 3）が法の序数以下の財の BaseNeed×今の値段の和（需要の立たない財は入れない）。</summary>
    private static double HandBasket(Region r, int lawOrdinal)
    {
        double sum = 0;
        foreach (var g in r.Goods)
        {
            int rank = g.Tier == GoodTier.Staple ? 1 : g.Tier == GoodTier.Daily ? 2 : g.Tier == GoodTier.Luxury ? 3 : 99;
            if (rank > lawOrdinal || !(g.BaseNeed > 0) || !(r.Markets[g.Id].LastDemand > 0) || !(r.Markets[g.Id].Price > 0)) continue;
            sum += g.BaseNeed * r.Markets[g.Id].Price;
        }
        return sum;
    }

    private static (double cap, int lp) HandCap(Region r)
    {
        var human = r.Producers.Where(s => s.GoodId == Catalog.LaborId && s.RecipeName.Length == 0 && !s.Bankrupt).ToList();
        int lp = human[0].LaborPopIndex;
        return (human.Where(s => s.LaborPopIndex == lp).Sum(s => s.Capacity), lp);
    }

    private static double HandFloor(Region r, int lawOrdinal)
    {
        var (cap, lp) = HandCap(r);
        return HandBasket(r, lawOrdinal) * r.Pops[lp].Size / cap;
    }

    /// <summary>本国の州ごとに 労働の値段＝床（法の序数 lawOrdinal）×ratio[i]・担い手 POP の失業率＝unemployment に置く。籠に労働は入らないので床は動かない。</summary>
    private static void SetBoard(World w, int lawOrdinal, double[] ratio, double unemployment)
    {
        var home = Home(w);
        for (int i = 0; i < home.Count; i++)
        {
            var r = home[i];
            r.Markets[Catalog.LaborId].Price = HandFloor(r, lawOrdinal) * ratio[i % ratio.Length];
            var (_, lp) = HandCap(r);
            r.Pops[lp].UnemploymentRate = new Share(unemployment);
        }
    }

    // ---------------------------------------------------------------- 純関数（表）

    [Fact]
    public void 下げる線と上げる線と間の現状維持の表()
    {
        var N = MinimumWageLaw.None; var S = MinimumWageLaw.Subsistence; var L = MinimumWageLaw.Living; var F = MinimumWageLaw.Sufficiency;
        double nan = double.NaN;
        // 下げる：施行中・失業 ≥ 1/2・今の段の縛り ≥ 1/2（境界を含む）
        Assert.Equal(S, UtilityAgent.MinimumWageFloorWant(L, 0.5, 0.5, nan));
        Assert.Equal(L, UtilityAgent.MinimumWageFloorWant(F, 0.9, 1.0, nan));
        Assert.Equal(N, UtilityAgent.MinimumWageFloorWant(S, 0.9, 0.5, nan));
        // 線のすぐ手前は下げない（現状維持＝今の段）
        Assert.Equal(L, UtilityAgent.MinimumWageFloorWant(L, 0.4999, 1.0, nan));
        Assert.Equal(L, UtilityAgent.MinimumWageFloorWant(L, 0.9, 0.4999, nan));
        // なしからは下げない・縛りが読めない（NaN）なら下げない
        Assert.Equal(N, UtilityAgent.MinimumWageFloorWant(N, 0.9, 0.9, nan));
        Assert.Equal(L, UtilityAgent.MinimumWageFloorWant(L, 0.9, nan, nan));
        // 上げる：充足の手前・失業 < 1/4・次の段の縛り 0 以上〜1/3 以下（境界を含む）
        Assert.Equal(S, UtilityAgent.MinimumWageFloorWant(N, 0.2499, nan, 0.0001));
        Assert.Equal(S, UtilityAgent.MinimumWageFloorWant(N, 0.0, nan, 1.0 / 3.0));
        Assert.Equal(L, UtilityAgent.MinimumWageFloorWant(S, 0.1, nan, 0.2));
        Assert.Equal(F, UtilityAgent.MinimumWageFloorWant(L, 0.1, nan, 0.2));
        // 線の外は上げない
        Assert.Equal(N, UtilityAgent.MinimumWageFloorWant(N, 0.25, nan, 0.2));        // 失業が上げる線ちょうど
        Assert.Equal(S, UtilityAgent.MinimumWageFloorWant(N, 0.1, nan, 0.0));          // 籠を割る賃金が無い＝床は誰も縛らない＝書き留めるだけで害が無い（(480) 追記 1：0 でも上げる）
        Assert.Equal(N, UtilityAgent.MinimumWageFloorWant(N, 0.1, nan, 0.3334));       // 縛りが広い
        Assert.Equal(N, UtilityAgent.MinimumWageFloorWant(N, 0.1, nan, nan));          // 読めない
        Assert.Equal(F, UtilityAgent.MinimumWageFloorWant(F, 0.1, nan, 0.2));          // 充足から上は無い
        // 間の帯（失業 1/4〜1/2）は何があっても現状維持
        foreach (var cur in new[] { N, S, L, F })
            foreach (double u in new[] { 0.25, 0.3, 0.4999 })
                Assert.Equal(cur, UtilityAgent.MinimumWageFloorWant(cur, u, 1.0, 0.2));
        // 失業が読めなければ何も書かない
        Assert.Null(UtilityAgent.MinimumWageFloorWant(L, nan, 1.0, 0.2));
    }

    // ---------------------------------------------------------------- 旗 0 は今の規則・旗 1 は盤面から

    [Fact]
    public void 旗0の入口は今の規則そのもので計算量の台帳も増えない_旗1は台帳に数える()
    {
        var sim = Build();
        var w = sim.World;
        long Led() => w.Cost[CostLedger.MinimumWageBasketScans];
        foreach (var cur in new[] { MinimumWageLaw.None, MinimumWageLaw.Subsistence, MinimumWageLaw.Living, MinimumWageLaw.Sufficiency })
        {
            w.MinimumWage = cur;
            w.Coef.MinimumWageAiFromFloor = 0;
            long before = Led();
            Assert.Equal(UtilityAgent.MinimumWageTargetFromBoard(w), UtilityAgent.MinimumWageTargetChosen(w));
            Assert.Equal(before, Led());
        }
        // 旗 1 は新しい規則（読み手を呼ぶ＝台帳が増える）。MinimumWageLawGate＝0 なら旗に依らず書かない・台帳も増えない
        w.MinimumWage = MinimumWageLaw.None;
        w.Coef.MinimumWageAiFromFloor = 1;
        long b1 = Led();
        UtilityAgent.MinimumWageTargetChosen(w);
        Assert.True(Led() > b1, "旗 1 で盤面を読んでいない");
        w.Coef.MinimumWageLawGate = 0;
        long b2 = Led();
        Assert.Null(UtilityAgent.MinimumWageTargetChosen(w));
        Assert.Equal(b2, Led());
    }

    // ---------------------------------------------------------------- 盤面の読み（手の参照との一致）

    [Fact]
    public void 失業と縛る広がりは手で組んだ期待値と一致する()
    {
        var w = Build().World;
        var home = Home(w);
        Assert.True(home.Count >= 3, "前提が崩れた：本国の州が 3 つ未満");
        // 州 0 は値段＝生活の床の 0.9 倍（縛る）・州 1 は 1.05 倍ちょうど（縛る・境界を含む）・他は 3 倍（縛らない）。失業は 0.37
        var ratio = new double[home.Count]; for (int i = 0; i < ratio.Length; i++) ratio[i] = 3.0;
        ratio[0] = 0.9; ratio[1] = 1.05;
        SetBoard(w, 2, ratio, 0.37);
        var caps = home.Select(r => HandCap(r).cap).ToArray();
        double totalCap = caps.Sum();
        var h = MinimumWageFloor.ReadHomeLabor(w);
        Assert.Equal(home.Count, h.N);
        // 失業＝担い手 POP の人数重み付け平均（全州が 0.37 なので 0.37）
        Assert.Equal(0.37, h.Unemployment, 12);
        // 生活（序数 2）が今の段：縛るのは州 0 と州 1。次の段（充足）の床は生活より高い＝州 2 以降も 3 倍で縛るとは限らないので、手で床を組んで数える
        double expectCur = 0, expectNext = 0;
        for (int i = 0; i < home.Count; i++)
        {
            double price = home[i].Markets[Catalog.LaborId].Price;
            if (price <= 1.05 * HandFloor(home[i], 2) * (1 + 1e-12) && HandFloor(home[i], 2) > 0) expectCur += caps[i];
            if (price <= 1.05 * HandFloor(home[i], 3) * (1 + 1e-12) && HandFloor(home[i], 3) > 0) expectNext += caps[i];
        }
        Assert.Equal(expectCur / totalCap, MinimumWageFloor.BindShare(h, MinimumWageLaw.Living), 9);
        Assert.Equal(expectNext / totalCap, MinimumWageFloor.BindShare(h, MinimumWageLaw.Sufficiency), 9);
        Assert.True(expectCur > 0 && expectCur < totalCap, "前提が崩れた：縛る州が 0 か全部");
        Assert.True(expectNext >= expectCur, "次の段の床は今の段以上＝縛る広がりは減らない");
        Assert.True(double.IsNaN(MinimumWageFloor.BindShare(h, MinimumWageLaw.None)));
        // 失業を州ごとに変えると人数重み付けになる
        var (_, lp0) = HandCap(home[0]); var (_, lp1) = HandCap(home[1]);
        home[0].Pops[lp0].UnemploymentRate = new Share(0.9);
        home[1].Pops[lp1].UnemploymentRate = new Share(0.1);
        double sz0 = home[0].Pops[lp0].Size, sz1 = home[1].Pops[lp1].Size;
        double den = 0, num = 0;
        for (int i = 0; i < home.Count; i++)
        {
            var (_, lp) = HandCap(home[i]);
            den += home[i].Pops[lp].Size; num += home[i].Pops[lp].Size * home[i].Pops[lp].UnemploymentRate.Value;
        }
        Assert.Equal(num / den, MinimumWageFloor.ReadHomeLabor(w).Unemployment, 12);
    }

    [Fact]
    public void 値段が立たない州と能力の無い州は数えない_読めなければNaN()
    {
        var w = Build().World;
        var home = Home(w);
        SetBoard(w, 2, [0.9], 0.6);
        home[0].Markets[Catalog.LaborId].Price = 0;                    // 値段が立たない州は数えない
        var h = MinimumWageFloor.ReadHomeLabor(w);
        Assert.Equal(home.Count - 1, h.N);
        home[1].Tier = RegionTier.Colonial;                            // 植民地格の州は本国の盤面に入れない（HomeUrbanShare と同じ集合）
        Assert.Equal(home.Count - 2, MinimumWageFloor.ReadHomeLabor(w).N);
        foreach (var r in home) r.Markets[Catalog.LaborId].Price = double.NaN;   // 全て読めない
        var h2 = MinimumWageFloor.ReadHomeLabor(w);
        Assert.Equal(0, h2.N);
        Assert.True(double.IsNaN(h2.Unemployment));
        Assert.True(double.IsNaN(MinimumWageFloor.BindShare(h2, MinimumWageLaw.Living)));
        w.MinimumWage = MinimumWageLaw.Living;
        w.Coef.MinimumWageAiFromFloor = 1;
        Assert.Null(UtilityAgent.MinimumWageTargetFromFloor(w));       // 失業が読めない＝何も書かない
    }

    // ---------------------------------------------------------------- 目標（盤面から）

    [Fact]
    public void 床が縛って失業が高ければ下げ_縛らなければ下げず_失業が低ければ縛っていても下げない()
    {
        var w = Build().World;
        w.Coef.MinimumWageAiFromFloor = 1;
        w.MinimumWage = MinimumWageLaw.Living;
        var all = new[] { 0.95 };                                    // 全ての州で値段＝生活の床の 0.95 倍（縛る）
        SetBoard(w, 2, all, 0.8);
        Assert.Equal(MinimumWageLaw.Subsistence, UtilityAgent.MinimumWageTargetFromFloor(w));
        SetBoard(w, 2, all, 0.3);                                    // 失業が下げる線の手前＝縛っていても下げない（間の帯）
        Assert.Equal(MinimumWageLaw.Living, UtilityAgent.MinimumWageTargetFromFloor(w));
        SetBoard(w, 2, new[] { 3.0 }, 0.8);                          // 床が縛っていない（値段が床の 3 倍）＝失業が高くても床のせいではない
        Assert.Equal(MinimumWageLaw.Living, UtilityAgent.MinimumWageTargetFromFloor(w));
        w.MinimumWage = MinimumWageLaw.Subsistence;                  // 生存まで施行している国が縛って失業が高い＝なしへ下げる目標
        SetBoard(w, 1, all, 0.8);
        Assert.Equal(MinimumWageLaw.None, UtilityAgent.MinimumWageTargetFromFloor(w));
    }

    [Fact]
    public void 失業が低く次の段が広くは縛らなければ上げ_広ければ上げない()
    {
        var w = Build().World;
        w.Coef.MinimumWageAiFromFloor = 1;
        w.MinimumWage = MinimumWageLaw.None;
        var home = Home(w);
        Assert.True(home.Count >= 3);
        var caps = home.Select(r => HandCap(r).cap).ToArray();
        // 州 0 だけ値段＝生存の床の 0.9 倍（籠を割る）・他は 3 倍（割らない）。失業 0.1。州 0 の能力の割合 ≤ 1/3 なら上げる
        var one = new double[home.Count]; for (int i = 0; i < one.Length; i++) one[i] = 3.0; one[0] = 0.9;
        SetBoard(w, 1, one, 0.1);
        double share0 = caps[0] / caps.Sum();
        Assert.True(share0 > 0 && share0 <= 1.0 / 3.0, $"前提が崩れた：州 0 の能力の割合 {share0:F3}");
        Assert.Equal(MinimumWageLaw.Subsistence, UtilityAgent.MinimumWageTargetFromFloor(w));
        // 籠を割る賃金が無い（縛る広がり 0）＝床は誰も縛らない＝書き留めるだけで害が無い→ 上げる（(480) 追記 1）
        SetBoard(w, 1, new[] { 3.0 }, 0.1);
        Assert.Equal(MinimumWageLaw.Subsistence, UtilityAgent.MinimumWageTargetFromFloor(w));
        // 全ての州で割る＝広い（縛り 1）→ 上げない
        SetBoard(w, 1, new[] { 0.9 }, 0.1);
        Assert.Equal(MinimumWageLaw.None, UtilityAgent.MinimumWageTargetFromFloor(w));
        // 失業が上げる線以上なら一部でも上げない
        SetBoard(w, 1, one, 0.3);
        Assert.Equal(MinimumWageLaw.None, UtilityAgent.MinimumWageTargetFromFloor(w));
        // 充足まで来たら上は無い
        w.MinimumWage = MinimumWageLaw.Sufficiency;
        SetBoard(w, 3, one, 0.1);
        Assert.Equal(MinimumWageLaw.Sufficiency, UtilityAgent.MinimumWageTargetFromFloor(w));
    }

    // ---------------------------------------------------------------- 法の門（プレイヤーと同じ）

    [Fact]
    public void 下げる目標は法の門で現行へ畳まれず残る_片方向の錠ではない()
    {
        var w = Build().World;
        w.MinimumWage = MinimumWageLaw.Living;
        w.Policy.MinimumWage = MinimumWageLaw.Subsistence;           // 下げの目標（最低賃金法は不可逆でも前提つきでもない）
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(MinimumWageLaw.Subsistence, w.Policy.MinimumWage);
        // 軸は不可逆の保護の対象でない（保護されていれば下げは規則で塞がれ現行へ畳まれる）
        Assert.False(Government.ProtectedLaw(w, PolicyAxis.MinimumWage));
        Assert.False(Government.BlockedByIrreversibility(w, PolicyAxis.MinimumWage, regressive: true));
    }

    // ---------------------------------------------------------------- ★(497) 線 mw：旗 MinimumWageAiHoldTarget（間の帯で現行法を書き戻さない）

    [Fact]
    public void 旗Holdは現行法と同じ結果のときだけ書かない_上げと下げと読めないは旗に依らない()
    {
        var w = Build().World;
        w.Coef.MinimumWageAiFromFloor = 1;
        var all3 = new[] { 3.0 };                                    // 値段＝床の 3 倍（縛らない）
        void Both(MinimumWageLaw? flag0, MinimumWageLaw? flag1, string what)
        {
            w.Coef.MinimumWageAiHoldTarget = 0; Assert.True(flag0 == UtilityAgent.MinimumWageTargetFromFloor(w), $"旗 0 の {what}");
            w.Coef.MinimumWageAiHoldTarget = 1; Assert.True(flag1 == UtilityAgent.MinimumWageTargetFromFloor(w), $"旗 1 の {what}");
        }
        w.MinimumWage = MinimumWageLaw.Living;
        SetBoard(w, 2, all3, 0.3);                                   // 失業が上げる線 1/4 と下げる線 1/2 の間＝現状維持
        Both(MinimumWageLaw.Living, null, "間の帯");
        w.MinimumWage = MinimumWageLaw.None;
        Both(MinimumWageLaw.None, null, "間の帯（なし）");
        SetBoard(w, 1, all3, 0.1);                                   // 失業が低く縛りが無い＝上げる
        Both(MinimumWageLaw.Subsistence, MinimumWageLaw.Subsistence, "上げ");
        w.MinimumWage = MinimumWageLaw.Sufficiency;                  // 充足から上は無い＝現状維持
        SetBoard(w, 3, all3, 0.1);
        Both(MinimumWageLaw.Sufficiency, null, "充足の現状維持");
        w.MinimumWage = MinimumWageLaw.Living;                       // 縛って失業が高い＝下げる
        SetBoard(w, 2, new[] { 0.95 }, 0.8);
        Both(MinimumWageLaw.Subsistence, MinimumWageLaw.Subsistence, "下げ");
        foreach (var r in Home(w)) r.Markets[Catalog.LaborId].Price = double.NaN;   // 失業が読めない＝元から書かない
        Both(null, null, "読めない");
        w.Coef.MinimumWageLawGate = 0;                               // 法のゲート 0 は旗に依らず書かない
        Both(null, null, "ゲート 0");
    }

    /// <summary>ブロック（DecideModernLaws の最低賃金法）を含む Apply を、手で組んだ盤面（失業 0.3＝間の帯）で通す。立っていた目標 standing を p.MinimumWage に置いてから。</summary>
    private static World ApplyWithStanding(double holdFlag, MinimumWageLaw cur, MinimumWageLaw standing)
    {
        var w = Build().World;
        w.Coef.MinimumWageAiFromFloor = 1;
        w.Coef.MinimumWageAiHoldTarget = holdFlag;
        w.MinimumWage = cur;
        SetBoard(w, (int)cur == 0 ? 1 : (int)cur, new[] { 3.0 }, 0.3);
        w.Policy.MinimumWage = standing;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        return w;
    }

    [Fact]
    public void 立っていた目標は旗Holdの間の帯でApplyを通っても残り_旗0では現行法へ戻る()
    {
        // 現行法＝なし・立っていた目標＝生存（要求・規則が前の四半期に置いた 1 段）・失業 0.3（間の帯）。期待値は規則から作らない（手で立てた目標が残るか）。
        var w0 = ApplyWithStanding(0, MinimumWageLaw.None, MinimumWageLaw.Subsistence);
        Assert.Equal(MinimumWageLaw.None, w0.Policy.MinimumWage);          // 旧＝書き戻す
        var w1 = ApplyWithStanding(1, MinimumWageLaw.None, MinimumWageLaw.Subsistence);
        Assert.Equal(MinimumWageLaw.Subsistence, w1.Policy.MinimumWage);   // 新＝残る（Apply 末尾の門の畳み〔GateAllLawTargets〕も壊さない）
        // 下げの目標が立っていても同じ（現行法＝生活・立っていた目標＝生存）
        Assert.Equal(MinimumWageLaw.Living, ApplyWithStanding(0, MinimumWageLaw.Living, MinimumWageLaw.Subsistence).Policy.MinimumWage);
        Assert.Equal(MinimumWageLaw.Subsistence, ApplyWithStanding(1, MinimumWageLaw.Living, MinimumWageLaw.Subsistence).Policy.MinimumWage);
        // 立っていた目標が無ければ（目標＝現行法）旗に依らず現行法のまま
        Assert.Equal(MinimumWageLaw.None, ApplyWithStanding(0, MinimumWageLaw.None, MinimumWageLaw.None).Policy.MinimumWage);
        Assert.Equal(MinimumWageLaw.None, ApplyWithStanding(1, MinimumWageLaw.None, MinimumWageLaw.None).Policy.MinimumWage);
    }

    [Fact]
    public void 要求が置き直している間は旗に依らず目標が残り_旗が効くのは要求が下りた後()
    {
        // 州一揆の譲歩（PendingRevoltDemand）が最低賃金法の引き上げを記録している間は、Apply の末尾の TargetCrisisDemands が毎期置き直す＝規則が書き戻しても旗 0 でも残る。
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var w = Build().World;
            w.Coef.MinimumWageAiFromFloor = 1; w.Coef.MinimumWageAiHoldTarget = flag;
            w.PoliticalLockEnabled = false;                                // 門が通る世界（置く/置かないは GateInfo）
            w.MinimumWage = MinimumWageLaw.None;
            SetBoard(w, 1, new[] { 3.0 }, 0.3);                            // 間の帯＝規則は現行法を書く（旗 0）か書かない（旗 1）
            var (ok, _, why) = Government.GateInfo(w, PolicyAxis.MinimumWage, increasing: true, 1);
            Assert.True(ok, $"前提が崩れた：最低賃金法の 1 段目が門を通らない（{why}）");
            w.PendingRevoltDemand = (int)PolicyAxis.MinimumWage + 1;
            UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
            Assert.Equal(MinimumWageLaw.Subsistence, w.Policy.MinimumWage);   // 旗に依らず要求の目標が最後の言葉
            w.PendingRevoltDemand = 0;                                         // 要求が下りた（危機の終わり・法は動いていない）
            UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
            Assert.Equal(flag > 0 ? MinimumWageLaw.Subsistence : MinimumWageLaw.None, w.Policy.MinimumWage);   // 旗 1 だけ目標が残る
        }
    }

    [Fact]
    public void 計器の数えは立っていた目標を書き直し_書き戻し_向き変え_書かず残るに分ける()
    {
        var w = Build().World;
        var N = MinimumWageLaw.None; var S = MinimumWageLaw.Subsistence; var L = MinimumWageLaw.Living; var F = MinimumWageLaw.Sufficiency;
        w.MinimumWage = L;
        Instruments.MaCount(w, L, L);                                  // 立っていない（prior==cur）＝決定だけ数える
        Instruments.MaCount(w, F, F);                                  // 立っていて同じ値を書き直した
        Instruments.MaCount(w, F, L);                                  // 立っていて現行法へ書き戻した
        Instruments.MaCount(w, F, S);                                  // 立っていて別の値（現行法でも立っていた目標でもない）
        Instruments.MaCount(w, S, null);                               // 立っていて何も書かない
        Instruments.MaCount(w, N, null);
        Assert.Equal((6, 5, 1, 1, 1, 2), (w.DiagMaDecisions, w.DiagMaStanding, w.DiagMaKept, w.DiagMaErased, w.DiagMaFlipped, w.DiagMaCarried));
    }
}
