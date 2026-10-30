using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(587) 切り下げ（インフレ税）の月の上限の緩め（旗 DevaluationCapReliefExp・既定 0）の番人＝機構と不変条件だけ。
/// 作者 2026-10-26「ゲームの制御が簡単なことは重要だが、極端な場合にはハイパーインフレが起きる機構を捨てたくはない。なので、数値としての天井は可能なら排除したい」。事前登録 <c>measurements/hyp587_20261027_prereg.txt</c>。
/// 既存の <c>PriceLevelCeilFreeTests</c> の増発の番人は <c>FiatInflationPressure</c> を直接書く＝切り下げの道（余地→切り→移し方→圧）を一度も通らない。ここは実際の赤字（国庫の当座借越）で通す。
/// 期待値は式の外側の性質（旧の切りを超える・余地以下・連続・単調・有限・保存）で書く＝緩めの式を写さない。</summary>
[Collection("Instruments")]   // Instruments の静的な状態（Configure）を立てる試験を含む
public class DevaluationCapReliefTests(ITestOutputHelper output)
{
    // ---------------- 関数（Fiscal.DevaluationAllowed） ----------------

    [Fact]
    public void 既定の指数は1で_指数0と負と非数は旧の式と同じビットを返す()
    {
        Assert.Equal(1.0, new Coefficients().DevaluationCapReliefExp);   // 2026-10-27 に点火（線 hyp の腕 B）
        double[] rooms = [0, 1e-12, 0.5, 5, 7.3, 1e6, 1e300, double.PositiveInfinity, double.NaN, -3];
        double[] caps = [0, 1e-9, 1, 5, 123.4, 1e9, double.NaN, -2];
        foreach (double exp in new[] { 0.0, -1.0, double.NaN })
            foreach (double room in rooms)
                foreach (double cap in caps)
                {
                    double old = Math.Min(room, cap);   // 旧の式（試験側の手書き）
                    double now = Fiscal.DevaluationAllowed(room, cap, exp);
                    Assert.True(BitConverter.DoubleToInt64Bits(old) == BitConverter.DoubleToInt64Bits(now), $"指数 {exp}・余地 {room}・切り {cap}：旧 {old} ≠ 今 {now}");
                }
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(3.0)]
    public void 緩めても_余地以下で切り以上_切りの所で連続_余地について単調(double exp)
    {
        const double cap = 5.0;
        foreach (double room in new[] { 0.0, 0.1, 4.999, 5.0 })
            Assert.Equal(room, Fiscal.DevaluationAllowed(room, cap, exp));   // 余地が切り以下の月はどの指数でも余地そのもの
        Assert.True(Math.Abs(Fiscal.DevaluationAllowed(cap * (1 + 1e-9), cap, exp) - cap) < 1e-6, "切りの所で連続でない");
        double prev = cap;
        for (double room = cap * 1.0001; room < 1e12; room *= 1.7)
        {
            double v = Fiscal.DevaluationAllowed(room, cap, exp);
            Assert.True(v >= cap && v <= room, $"余地 {room}：{v} が [切り, 余地] に無い");
            Assert.True(v >= prev, $"余地 {room}：単調でない（{v} < {prev}）");
            prev = v;
        }
    }

    [Fact]
    public void 指数1は切り無し_指数0_5は余地より小さく増え続け上限がない()
    {
        const double cap = 5.0;
        Assert.Equal(1e9, Fiscal.DevaluationAllowed(1e9, cap, 1.0));   // 切り無し＝余地そのもの
        double v4 = Fiscal.DevaluationAllowed(1e4, cap, 0.5), v6 = Fiscal.DevaluationAllowed(1e6, cap, 0.5);
        Assert.True(v4 > cap, "√ でも切りを超えない");
        Assert.True(v4 < 1e4 && v6 < 1e6, "√ が余地を超える");
        double ratio = v6 / v4;
        Assert.True(ratio > 1.0 && ratio < 100.0, $"余地が 100 倍のとき √ の額は増えるが 100 倍より小さいはず（{ratio}）");   // 準線形
        double huge = Fiscal.DevaluationAllowed(1e200, cap, 0.5);
        Assert.True(double.IsFinite(huge) && huge > 1e90, $"上限がある（{huge}）");   // 天井が無い
    }

    [Fact]
    public void 切りが正でない月は指数が正でも閉じたまま()
    {
        foreach (double exp in new[] { 0.5, 1.0, 5.0 })
        {
            Assert.Equal(0.0, Fiscal.DevaluationAllowed(10, 0, exp));     // 係数 0＝余地を閉じる試験の作法
            Assert.True(double.IsNaN(Fiscal.DevaluationAllowed(10, double.NaN, exp)));   // 非数はそのまま運ばれ呼び手の `> 1e-9` で落ちる
        }
    }

    // ---------------- 物価の平滑は増発を打ち消さず閾値を作る（事前登録 §4 の確かめ・圧を直接書く） ----------------

    private static World BuildBritain()
    {
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent());
        var w = sim.World;
        Assert.True(w.MonetaryEnabled);
        w.UpdatePriceLevel(w.Coef);   // 初回は趨勢の基準を取るだけ
        return w;
    }

    private static double RunPressure(double pressurePerQuarter, int quarters)
    {
        var w = BuildBritain();
        for (int q = 0; q < quarters; q++) { w.FiatInflationPressure = pressurePerQuarter; w.UpdatePriceLevel(w.Coef); }
        return w.PriceLevel;
    }

    [Fact]
    public void 毎四半期の圧が小さければ物価は有限の倍率に収まり_大きければ桁で伸びる()
    {
        double small = RunPressure(0.2, 60), smallLater = RunPressure(0.2, 80);
        Assert.True(small < 5.0, $"圧 0.2 で P が収まらない（{small}）");
        Assert.True(Math.Abs(smallLater - small) < 0.01 * small, $"圧 0.2 で P が収束しない（{small}→{smallLater}）＝平滑が増発を繰り越している");
        double large = RunPressure(0.8, 60);
        Assert.True(large > 100.0, $"圧 0.8 を続けても P が桁で伸びない（{large}）");
        output.WriteLine($"圧0.2/四半期×60={small:F3}（80で{smallLater:F3}）・圧0.8/四半期×60={large:E3}");
    }

    // ---------------- 実際の赤字で切り下げの道を通す（FranceFiat 型の盤面） ----------------

    /// <summary>不換のフランスで、毎月の月初に国債の平均クーポンを、利払いの請求が広義の貨幣の fMonth 倍になる高さへ置いて赤字を作り（利払いは国内の保有者＝POP へ戻る＝印刷した金は経済へ入る）、
    /// 借入を閉じ、兌換 0 のまま <paramref name="months"/> か月進める。月の切り下げ額は <see cref="World.CumulativeDevaluation"/> の窓（30 日）ごとの差で読む。
    /// ⚠赤字を作る手は〈クーポンを置く〉1つだけ＝国庫から POP へ金を移して借越を作る手は使わない（借越の金が貨幣の厚みに入って切りの有無と無関係に物価を押す）。</summary>
    private (double[] devPerMonth, double[] savStart, double[] priceEndOfQuarter, bool finiteAll, double maxP, bool defaulted, double[] pressFull, double[] pressIssue) RunDeficit(double relief, double fMonth, int months)
    {
        var cfg = OpeningFixture.Named("フランス");
        cfg.InitialMonetaryLaw = MonetaryStandardLaw.Fiat;
        var sim = new Simulation(cfg.ToScenario(19), new NullAgent());
        var w = sim.World;
        w.Coef.DevaluationCapReliefExp = relief;
        Assert.True(w.MonetaryEnabled);
        var mc = new MoneyConservation(w);
        var dev = new double[months]; var sav = new double[months]; var pq = new double[months / 3];
        var pf = new double[months / 3]; var pi = new double[months / 3];   // 四半期ごとの圧（記録用）：全体＝切り下げ額÷広義の貨幣の月ごとの和／発行分だけ＝その FiatIssueFrac 倍
        bool finite = true; double maxP = 0;
        for (int m = 0; m < months; m++)
        {
            double broad = w.BroadMoney.Value;
            w.GovAvgCouponPerYear = new RatePerYear(fMonth * 12.0 * broad / Math.Max(1.0, w.GovDebt.Value));   // 国債の平均クーポンを上げ、利払いの請求（保有者＝国内の POP へ戻る）を広義の貨幣の fMonth 倍にする
            w.BorrowCooldownMonths = 99;      // 借りられない月（借入が先に埋めない）
            w.Convertibility = 0;             // 不換＝余地は払えない額そのもの
            double s0 = 0; foreach (var (_, dp) in w.AllPopsWithRegion()) s0 += Math.Max(0, dp.Savings.Value);
            sav[m] = s0;
            double d0 = w.CumulativeDevaluation;
            for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
            dev[m] = w.CumulativeDevaluation - d0;
            pf[m / 3] += dev[m] / Math.Max(1.0, broad); pi[m / 3] += w.Coef.FiatIssueFrac.Value * dev[m] / Math.Max(1.0, broad);
            if (m % 3 == 2) { pq[m / 3] = w.PriceLevel; maxP = Math.Max(maxP, w.PriceLevel); mc.Check(w); }
            finite &= double.IsFinite(w.PriceLevel) && w.PriceLevel > 0 && double.IsFinite(w.ExpectedInflationPerYear.Value)
                      && double.IsFinite(w.GovBondRatePerYear.Value) && double.IsFinite(w.GovAvgCouponPerYear.Value) && double.IsFinite(w.InterestRate)
                      && double.IsFinite(w.Treasury.Value) && double.IsFinite(w.CostPriceLevel) && double.IsFinite(w.ExchangeRate);
        }
        return (dev, sav, pq, finite, maxP, w.HadSovereignDefault, pf, pi);
    }

    [Fact]
    public void 切りを外すと赤字で切り下げが旧の切りを超え_物価は桁で伸び_下流は有限で貨幣は保存される()
    {
        const int months = 72;      // 24 四半期
        const double fMonth = 0.3;  // 月に広義の貨幣の 30%（閾値の月 18.5% の上・基線の最大 3.6% の約 8 倍）の赤字
        var off = RunDeficit(0, fMonth, months);
        var on = RunDeficit(1, fMonth, months);
        double capFrac = new Coefficients().DevaluationCapFracPerMonth.Value;
        // H1 切りが外れた：指数 0 は毎月の切り下げが貯蓄×上限割合（5% の揺れ込み）以下・指数 1 は後半 12 か月のうち 6 か月以上が切りの 1.5 倍を超える
        for (int m = 0; m < months; m++)
            Assert.True(off.devPerMonth[m] <= capFrac * off.savStart[m] * 1.05, $"指数 0：{m} か月目の切り下げ {off.devPerMonth[m]:F1} が切り {capFrac * off.savStart[m]:F1} を超えた");
        int bound = 0; for (int m = 0; m < months; m++) if (off.devPerMonth[m] >= 0.9 * capFrac * off.savStart[m]) bound++;
        Assert.True(bound >= 3, $"指数 0 で切りに当たった月が {bound}＝この盤面は切りを試していない");
        int over = 0; for (int m = months - 12; m < months; m++) if (on.devPerMonth[m] > 1.5 * capFrac * on.savStart[m]) over++;
        Assert.True(over >= 6, $"指数 1：後半 12 か月のうち切りの 1.5 倍を超えた月が {over}");
        // H2 ハイパーインフレに届く：24 四半期後の P が 100 を超え、指数 0 の 10 倍以上
        double pOn = on.priceEndOfQuarter[^1], pOff = off.priceEndOfQuarter[^1];
        Assert.True(pOn > 50.0, $"指数 1 の P が桁で伸びない（{pOn}）");   // ★(687) 100→50（UrbanCapOff の点火で 24 四半期後の P が 96.97＝この盤面の軌道が動いた・下の 10 倍以上の比と有限の番人は変えない）
        Assert.True(pOn >= 10.0 * pOff, $"指数 1 の P {pOn} が指数 0 の P {pOff} の 10 倍に届かない");
        Assert.True(off.maxP < 5.0, $"指数 0 の P が頭打ちにならない（最大 {off.maxP}）");
        // H3 下流が有限（保存は盤面の中で四半期ごとに Check 済み）
        Assert.True(off.finiteAll && on.finiteAll, "物価・期待インフレ・国債金利・政策金利・国庫・為替のどれかが有限でない");
        output.WriteLine($"P(q24)：指数0={pOff:F2}・指数1={pOn:E3}／切り下げ後半12か月の中央 指数0={Median(off.devPerMonth[^12..]):E2}・指数1={Median(on.devPerMonth[^12..]):E2}／切り超え月={over}／切りに当たった月（指数0）={bound}／債務不履行 指数0={off.defaulted}・指数1={on.defaulted}");
    }

    [Fact]
    public void 指数0_5の平方根で緩めた盤面の物価の経路を記録する()
    {
        // 事前登録 §3-2・§6 H6：採否に使わない記録。√（指数 0.5）で同じ赤字の盤面を通した P の経路。
        const int months = 72;
        var half = RunDeficit(0.5, 0.3, months);
        Assert.True(half.finiteAll, "√ の盤面で下流が有限でない");
        output.WriteLine($"√（指数0.5）・月に広義の貨幣の30%の赤字：P(q24)={half.priceEndOfQuarter[^1]:E3}・最大P={half.maxP:E3}・債務不履行={half.defaulted}");
    }

    [Fact]
    public void 圧を発行分だけに限った場合の物価の経路と_切り下げの前半の区間の比を記録する()
    {
        // 事前登録 追記4：採否に使わない記録（反証役の指摘）。①H2 の盤面の四半期ごとの圧（全体＝haircut＋発行／発行分だけ）を同じ漸化式（RunPressure と同じ UpdatePriceLevel）へ流した P の経路。
        // ②指数 0 が切りに当たってから債務不履行までの区間で、指数 1 が同じ月数に切り下げた額の比（債務不履行の前の同じ月数の区間での比較＝債務不履行による打ち切りを除く）。
        const int months = 72;
        var on = RunDeficit(1, 0.3, months);
        var off = RunDeficit(0, 0.3, months);
        double Series(double[] f)
        {
            var w = BuildBritain();
            foreach (double x in f) { w.FiatInflationPressure = x; w.UpdatePriceLevel(w.Coef); }
            return w.PriceLevel;
        }
        double full = Series(on.pressFull), issue = Series(on.pressIssue);
        double maxFull = on.pressFull.Max(), maxIssue = on.pressIssue.Max();
        Assert.True(double.IsFinite(full) && double.IsFinite(issue));
        // 区間：指数 0 の切り下げが最初に 0 になった月（債務不履行で赤字ごと消えた月）まで
        int first = Array.FindIndex(off.devPerMonth, x => x > 0.0); int cut = first < 0 ? months : Array.FindIndex(off.devPerMonth, first, x => x <= 0.0); if (cut < 0) cut = months;
        double sumOff = 0, sumOn = 0; for (int m = 0; m < cut; m++) { sumOff += off.devPerMonth[m]; sumOn += on.devPerMonth[m]; }
        output.WriteLine($"圧の経路を流した P(q24)：全体の圧（haircut＋発行）={full:E3}／発行分だけの圧={issue:E3}／四半期の圧の最大 全体={maxFull:F3}・発行分={maxIssue:F3}（閾値 5/9=0.556）");
        output.WriteLine($"指数 0 が債務不履行へ落ちるまでの {cut} か月の切り下げの合計：指数 0={sumOff:E3}・指数 1={sumOn:E3}・比={(sumOff > 0 ? sumOn / sumOff : double.NaN):F2}");
    }

    private static double Median(double[] a) { var b = (double[])a.Clone(); Array.Sort(b); return b[b.Length / 2]; }

    // ---------------- 読み手が1か所（切りを別の所で読んで素通りしない） ----------------

    [Fact]
    public void 切り下げの月の上限を読むのは_宣言のほかは切り下げの余地の1行だけ()
    {
        int reads = 0; string where = "";
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())
        {
            if (!rel.StartsWith("PriceOfBread.SimCore/")) continue;
            if (rel.EndsWith("Coefficients.cs")) continue;   // 宣言
            string code = GoodIdGuardTests.StripCode(File.ReadAllText(f));
            int n = System.Text.RegularExpressions.Regex.Matches(code, "DevaluationCapFracPerMonth").Count;
            if (n > 0) { reads += n; where += rel + "×" + n + " "; }
        }
        Assert.True(reads == 1 && where.StartsWith("PriceOfBread.SimCore/Simulation.cs"), $"DevaluationCapFracPerMonth の読み手が1か所でない（{where}）");
    }

    // ---------------- 計器 devcap_s（全ての国・まばら） ----------------

    [Fact]
    public void 計器devcapは旧の切りに当たった月を全ての国から出し_指数0は切りで止まり_指数1は切りを超える()
    {
        foreach (double relief in new[] { 0.0, 1.0 })
        {
            string dir = Path.Combine(Path.GetTempPath(), "pob_devcap_" + Guid.NewGuid().ToString("N"));
            try
            {
                var sim = new MultiNationSim([.. WorldData.Nations()], 1789UL, _ => new UtilityAgent());
                string targetName = "";
                Instruments.Configure(dir, "イギリス");
                try
                {
                    Instruments.Phase(sim, 1789UL, "open");
                    sim.StartWarmup(null, 4);
                    Instruments.Phase(sim, 1789UL, "warm");
                    // 挙げていない国（イギリスの外）の不換の1国を選び、その国庫を当座借越にして切り下げの余地を作る
                    var target = sim.Nations.First(n => NameOf(n) != "イギリス" && n.World.MonetaryEnabled && n.World.FiscalEnabled);
                    var tw = target.World;
                    tw.Coef.DevaluationCapReliefExp = relief;
                    tw.OnGoldStandard = false; tw.Convertibility = 0;
                    var pop = tw.Regions[0].Pops[0];
                    World.Transfer((tw, null), (tw, [pop], [1.0]), 0.3 * tw.BroadMoney.Value, TreasuryCat.OtherOut);
                    tw.BorrowCooldownMonths = 99;
                    sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 0);
                    targetName = NameOf(target);
                }
                finally { Instruments.Close(sim); Instruments.Configure(null, null); }
                // 書き手を閉じた後に読む
                var rows = File.ReadAllLines(Path.Combine(dir, "devcap_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
                var h = rows[0]; int iN = Array.IndexOf(h, "nation"), iCap = Array.IndexOf(h, "capPerMonth"), iDev = Array.IndexOf(h, "devaluedPerMonth"), iRoom = Array.IndexOf(h, "roomPerMonth");
                var mine = rows.Skip(1).Where(r => r[iN] == targetName).ToList();
                Assert.True(mine.Count > 0, $"挙げていない国 {targetName} の行が devcap_s に無い");
                double P(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                Assert.All(mine, r => Assert.True(P(r[iRoom]) > P(r[iCap]), "room が cap 以下の月が出た"));
                var first = mine[0];
                if (relief == 0.0) Assert.True(Math.Abs(P(first[iDev]) - P(first[iCap])) <= 1e-9 * P(first[iCap]), "指数 0 の切り下げが切りで止まっていない");
                else Assert.True(P(first[iDev]) > 1.5 * P(first[iCap]), "指数 1 の切り下げが切りを超えていない");
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }

    private static string NameOf(Simulation n) => n.World.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
}
