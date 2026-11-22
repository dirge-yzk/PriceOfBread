using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(587) 切り下げ（インフレ税）の月の上限の緩め（旗 DevaluationCapReliefExp は c132kx で消した＝切りを超える余地を全て切り下げる形だけが残る）の番人＝機構と不変条件だけ。
/// 作者 2026-10-26「ゲームの制御が簡単なことは重要だが、極端な場合にはハイパーインフレが起きる機構を捨てたくはない。なので、数値としての天井は可能なら排除したい」。事前登録 <c>measurements/hyp587_20261027_prereg.txt</c>。
/// 既存の <c>PriceLevelCeilFreeTests</c> の増発の番人は <c>FiatInflationPressure</c> を直接書く＝切り下げの道（余地→切り→移し方→圧）を一度も通らない。ここは実際の赤字（国庫の当座借越）で通す。
/// 期待値は式の外側の性質（旧の切りを超える・余地以下・連続・単調・有限・保存）で書く＝緩めの式を写さない。</summary>
[Collection("Instruments")]   // Instruments の静的な状態（Configure）を立てる試験を含む
public class DevaluationCapReliefTests(ITestOutputHelper output)
{
    // ---------------- 関数（Fiscal.DevaluationAllowed） ----------------

    [Fact]
    public void 余地が切り以下なら余地_切りを超えれば余地そのもの_それ以外は小さい方()
    {
        // 旗 DevaluationCapReliefExp（指数 0＝旧の切り・指数 1＝切り無し）は c132kx で消した。残した道＝指数 1（切りを超える余地は全て切り下げられる）。
        double[] rooms = [0, 1e-12, 0.5, 5, 7.3, 1e6, 1e300, double.PositiveInfinity, double.NaN, -3];
        double[] caps = [0, 1e-9, 1, 5, 123.4, 1e9, double.NaN, -2];
        foreach (double room in rooms)
            foreach (double cap in caps)
            {
                double now = Fiscal.DevaluationAllowed(room, cap);
                if (cap > 0 && room > cap) Assert.True(BitConverter.DoubleToInt64Bits(room) == BitConverter.DoubleToInt64Bits(now), $"余地 {room}・切り {cap}：切りを超える余地は余地そのものを返す（{now}）");
                else if (!double.IsNaN(room) && !double.IsNaN(cap)) Assert.True(BitConverter.DoubleToInt64Bits(Math.Min(room, cap)) == BitConverter.DoubleToInt64Bits(now), $"余地 {room}・切り {cap}：旧の式 Math.Min(余地, 切り) と {now} が違う");
                else Assert.True(double.IsNaN(now), $"余地 {room}・切り {cap}：非数が運ばれない（{now}）");
            }
    }

    [Fact]
    public void 余地が切り以下の月は余地そのもの_切りの所で連続_余地について単調()
    {
        const double cap = 5.0;
        foreach (double room in new[] { 0.0, 0.1, 4.999, 5.0 })
            Assert.Equal(room, Fiscal.DevaluationAllowed(room, cap));   // 余地が切り以下の月は余地そのもの
        Assert.True(Math.Abs(Fiscal.DevaluationAllowed(cap * (1 + 1e-9), cap) - cap) < 1e-6, "切りの所で連続でない");
        double prev = cap;
        for (double room = cap * 1.0001; room < 1e12; room *= 1.7)
        {
            double v = Fiscal.DevaluationAllowed(room, cap);
            Assert.True(v >= cap && v <= room, $"余地 {room}：{v} が [切り, 余地] に無い");
            Assert.True(v >= prev, $"余地 {room}：単調でない（{v} < {prev}）");
            prev = v;
        }
        Assert.Equal(1e9, Fiscal.DevaluationAllowed(1e9, cap));   // 切り無し＝余地そのもの
        double huge = Fiscal.DevaluationAllowed(1e200, cap);
        Assert.True(double.IsFinite(huge) && huge > 1e90, $"上限がある（{huge}）");   // 天井が無い
    }

    [Fact]
    public void 切りが正でない月は閉じたまま()
    {
        Assert.Equal(0.0, Fiscal.DevaluationAllowed(10, 0));     // 係数 0＝余地を閉じる試験の作法
        Assert.True(double.IsNaN(Fiscal.DevaluationAllowed(10, double.NaN)));   // 非数はそのまま運ばれ呼び手の `> 1e-9` で落ちる
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
    private (double[] devPerMonth, double[] savStart, double[] priceEndOfQuarter, bool finiteAll, double maxP, bool defaulted, double[] pressFull, double[] pressIssue) RunDeficit(double fMonth, int months)
    {
        var cfg = OpeningFixture.Named("フランス");
        cfg.InitialMonetaryLaw = MonetaryStandardLaw.Fiat;
        var sim = new Simulation(cfg.ToScenario(19), new NullAgent());
        var w = sim.World;
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
    public void 切りが無い形では赤字で切り下げが旧の切りを超え_物価は桁で伸び_下流は有限で貨幣は保存される()
    {
        // 旗 DevaluationCapReliefExp は c132kx で消した（指数 1＝切り無しの道だけ残る）。旧の切りの腕（指数 0）との比べは無い。
        const int months = 72;      // 24 四半期
        const double fMonth = 0.3;  // 月に広義の貨幣の 30%（閾値の月 18.5% の上・基線の最大 3.6% の約 8 倍）の赤字
        var on = RunDeficit(fMonth, months);
        double capFrac = new Coefficients().DevaluationCapFracPerMonth.Value;
        // H1 切りが外れた：後半 12 か月のうち 6 か月以上が貯蓄×上限割合の 1.5 倍を超える
        int over = 0; for (int m = months - 12; m < months; m++) if (on.devPerMonth[m] > 1.5 * capFrac * on.savStart[m]) over++;
        Assert.True(over >= 6, $"後半 12 か月のうち切りの 1.5 倍を超えた月が {over}");
        // H2 ハイパーインフレに届く：24 四半期後の P が 50 を超える
        double pOn = on.priceEndOfQuarter[^1];
        Assert.True(pOn > 50.0, $"P が桁で伸びない（{pOn}）");   // ★(687) 100→50（UrbanCapOff の点火で 24 四半期後の P が 96.97＝この盤面の軌道が動いた）
        // H3 下流が有限（保存は盤面の中で四半期ごとに Check 済み）
        Assert.True(on.finiteAll, "物価・期待インフレ・国債金利・政策金利・国庫・為替のどれかが有限でない");
        output.WriteLine($"P(q24)={pOn:E3}／切り下げ後半12か月の中央={Median(on.devPerMonth[^12..]):E2}");
    }

    [Fact]
    public void 圧を発行分だけに限った場合の物価の経路を記録する()
    {
        // 事前登録 追記4：採否に使わない記録。H2 の盤面の四半期ごとの圧（全体＝haircut＋発行／発行分だけ）を同じ漸化式で流す。
        const int months = 72;
        var on = RunDeficit(0.3, months);
        double Series(double[] f)
        {
            var w = BuildBritain();
            foreach (double x in f) { w.FiatInflationPressure = x; w.UpdatePriceLevel(w.Coef); }
            return w.PriceLevel;
        }
        double full = Series(on.pressFull), issue = Series(on.pressIssue);
        Assert.True(double.IsFinite(full) && double.IsFinite(issue));
        output.WriteLine($"圧の経路を流した P(q24)：全体の圧（haircut＋発行）={full:E3}／発行分だけの圧={issue:E3}");
    }

    private static double Median(double[] a) { var b = (double[])a.Clone(); Array.Sort(b); return b[b.Length / 2]; }

    // ---------------- 読み手が1か所（切りを別の所で読んで素通りしない） ----------------

    [Fact]
    public void 切り下げの月の上限を読むのは_宣言のほかは切り下げの余地の1行だけ()
    {
        int reads = 0; string where = "";
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())
        {
            if (!GoodIdGuardTests.IsEngineRel(rel)) continue;
            if (Path.GetFileName(rel).StartsWith("Coefficients.")) continue;   // 宣言
            string code = GoodIdGuardTests.StripCode(File.ReadAllText(f));
            int n = System.Text.RegularExpressions.Regex.Matches(code, "DevaluationCapFracPerMonth").Count;
            if (n > 0) { reads += n; where += rel + "×" + n + " "; }
        }
        Assert.True(reads == 1 && where.StartsWith("PriceOfBread.SimCore/Simulation.cs"), $"DevaluationCapFracPerMonth の読み手が1か所でない（{where}）");
    }

    // ---------------- 計器 devcap_s（全ての国・まばら） ----------------

    [Fact]
    public void 計器devcapは旧の切りに当たった月を全ての国から出し_切り下げは切りを超える()
    {
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
                Assert.True(P(first[iDev]) > 1.5 * P(first[iCap]), "切り下げが切りを超えていない");
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }

    private static string NameOf(Simulation n) => n.World.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
}
