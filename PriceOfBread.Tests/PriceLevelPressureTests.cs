using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(603) 線 pfix：価格水準の発散の根の番人＝機構と不変条件だけ。
/// 根＝切り下げの月の物価への圧の分母（Simulation の切り下げの塊）が、貯蓄が尽きた月に絶対値の床 1.0 で割られ、圧が通貨の額そのものになった（P が四半期で 1e6 倍・その後は請求が P に比例して P の 2 乗の暴走）。
/// 旗 <c>FiatPressureRelativeFloor</c>（1＝床は分子自身の大きさ＝月の圧は 0〜1 の割合）と <c>PriceTrendHoldAtZeroGdp</c>（実質 GDP が正でない四半期は趨勢を更新しない）。
/// 期待値は世界を手で作り替えた結果と、式の外側の性質（割合・保存・有限・上限の導出）で書く。事前登録 <c>measurements/pfix603_20261028_prereg.txt</c>。
/// ⚠T2（健全な領域で旧とビット一致）は旗のある間だけの恒等の番人＝旗を焼き込むときに消すか、割合 ≤1・保存の番人へ置き換える。</summary>
[Collection("Instruments")]   // Instruments の静的な状態（Configure）を立てる試験を含む
public class PriceLevelPressureTests(ITestOutputHelper output)
{
    // ---------------- 圧の分母（Fiscal.FiatPressureOfDevalue・純関数） ----------------

    [Fact]
    public void 旗0は旧の式そのもので_貯蓄が尽きた月は圧が通貨の額になる()
    {
        // 試験側の手書きの旧の式
        foreach (double dev in new[] { 1e-6, 0.5, 9.5086e5, 1e12 })
            foreach (double broad in new[] { 3.5e-12, 0.0, 0.7, 1.0, 55.0, 1e7, -1e-16, -5.5e11 })
            {
                double old = dev / Math.Max(1.0, broad);
                double now = Fiscal.FiatPressureOfDevalue(dev, broad, 0);
                Assert.True(BitConverter.DoubleToInt64Bits(old) == BitConverter.DoubleToInt64Bits(now), $"旗 0：devalue {dev}・貨幣 {broad}：旧 {old} ≠ 今 {now}");
            }
        Assert.True(Fiscal.FiatPressureOfDevalue(9.5086e5, 3.5e-12, 0) > 1e5);   // 病理の月（陽性対照）：圧が割合でなく額
        Assert.True(Fiscal.FiatPressureOfDevalue(9.5086e5, 3.5e-12, double.NaN) > 1e5);   // 旗が NaN でも旧の側（否定形）
    }

    [Fact]
    public void 旗1は取り上げた額が取れる額を越えない割合_どんな貨幣でも1以下()
    {
        foreach (double dev in new[] { 1e-9 * 1.0001, 1e-6, 0.5, 1.0, 9.5086e5, 1e12, 1e300 })
            foreach (double broad in new[] { 3.5e-12, 0.0, 0.7, 1.0, 55.0, 1e7, 1e300, -1e-16, -5.5e11, -3.0e110 })
            {
                double p = Fiscal.FiatPressureOfDevalue(dev, broad, 1);
                Assert.True(p > 0 && p <= 1.0, $"旗 1：devalue {dev}・貨幣 {broad}：圧 {p} が (0,1] の外");
            }
        // 貯蓄を全て取った月（貨幣が 0 付近）は圧がちょうど 1（旗 0 は 9.5e5）
        Assert.Equal(1.0, Fiscal.FiatPressureOfDevalue(9.5086e5, 3.5e-12, 1));
    }

    [Fact]
    public void 旗1は貨幣がmax_1_devalue以上の健全な月で旗0とビット一致する()
    {
        foreach (double dev in new[] { 1e-6, 0.5, 1.0, 37.0, 9.5e5, 1e9 })
            foreach (double mult in new[] { 1.0, 1.0000001, 2.0, 20.0, 1e4 })
            {
                double broad = Math.Max(1.0, dev) * mult;
                double a = Fiscal.FiatPressureOfDevalue(dev, broad, 0), b = Fiscal.FiatPressureOfDevalue(dev, broad, 1);
                Assert.True(BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b), $"健全な月で旗 1 が旧と違う（devalue {dev}・貨幣 {broad}：{a} ≠ {b}）");
            }
    }

    [Fact]
    public void 非数の貨幣は圧に非数を積み_四半期の更新の否定形が消して物価は有限のまま()
    {
        // 旗 1 でも NaN の貨幣は NaN の圧（Math.Max は NaN を消さない）＝既存と同じ性質。UpdatePriceLevel の否定形（World.cs）が消す
        Assert.True(double.IsNaN(Fiscal.FiatPressureOfDevalue(5.0, double.NaN, 1)));
        var w = Build();
        double p0 = w.PriceLevel;
        w.FiatInflationPressure = Fiscal.FiatPressureOfDevalue(5.0, double.NaN, 1);
        w.UpdatePriceLevel(w.Coef);
        Assert.True(double.IsFinite(w.PriceLevel) && w.PriceLevel > 0, $"NaN の圧で物価が壊れた（{p0} → {w.PriceLevel}）");
        Assert.Equal(0.0, w.FiatInflationPressure);
    }

    // ---------------- 実際の赤字で切り下げの道を通す（イギリス型の盤面・貯蓄を取りきる赤字） ----------------

    private static World Build()
    {
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent());
        var w = sim.World;
        Assert.True(w.MonetaryEnabled);
        w.UpdatePriceLevel(w.Coef);   // 初回は趨勢の基準を取るだけ
        return w;
    }

    /// <summary>不換のイギリスで、毎月の月初に国債の平均クーポンを、利払いの請求が広義の貨幣の fMonth 倍になる高さへ置いて赤字を作り、借入を閉じ、<paramref name="quarters"/> 四半期進める。
    /// 四半期ごとに P の更新の前後を記録する（更新の直前の P は前の四半期の末の値）。fMonth＞1 は貯蓄を 1 か月で取りきる赤字。</summary>
    private (double[] pAfter, double[] pPrev, double maxPressureMonth, bool finiteAll, int reachedQ, bool broke) RunDeficit(double flag, double fMonth, int quarters, ulong seed = 19, bool tolerateBreak = false)
    {
        var cfg = OpeningFixture.Named("イギリス");
        cfg.InitialMonetaryLaw = MonetaryStandardLaw.Fiat;
        var sim = new Simulation(cfg.ToScenario(seed), new NullAgent());
        var w = sim.World;
        w.Coef.FiatPressureRelativeFloor = flag;
        Assert.True(w.MonetaryEnabled);
        var mc = new MoneyConservation(w);
        int months = quarters * 3;
        var pAfter = new double[quarters]; var pPrev = new double[quarters];
        double maxPress = 0, prevP = w.PriceLevel; bool finite = true; double prevPress = 0; int reached = 0; bool broke = false;
        for (int m = 0; m < months; m++)
        {
            double broad = w.BroadMoney.Value;
            w.GovAvgCouponPerYear = new RatePerYear(fMonth * 12.0 * Math.Max(1.0, broad) / Math.Max(1.0, w.GovDebt.Value));
            w.BorrowCooldownMonths = 99;
            w.Convertibility = 0;
            for (int d = 0; d < Calendar.DaysPerMonth && !broke; d++)
            {
                try { sim.StepDay(); }
                catch (InvariantViolationException) when (tolerateBreak) { broke = true; break; }   // 旗 0 の病理の盤面（P が 1e300 級）は貨幣保存の浮動小数点の丸めが相対 1e-9 を越えて先で壊れる＝陽性対照はそこまでを読む
                double cur = w.FiatInflationPressure;
                if (cur > prevPress) maxPress = Math.Max(maxPress, cur - prevPress);   // 月の寄与（四半期の更新で 0 に戻った日は拾わない）
                prevPress = cur;
            }
            if (broke) break;
            if (m % 3 == 2)
            {
                int q = m / 3; pAfter[q] = w.PriceLevel; pPrev[q] = prevP; prevP = w.PriceLevel; mc.Check(w); reached = q + 1;
            }
            finite &= double.IsFinite(w.PriceLevel) && w.PriceLevel > 0 && double.IsFinite(w.ExpectedInflationPerYear.Value) && double.IsFinite(w.GovBondRatePerYear.Value)
                      && double.IsFinite(w.Treasury.Value) && double.IsFinite(w.CostPriceLevel) && double.IsFinite(w.ExchangeRate);
        }
        return (pAfter, pPrev, maxPress, finite, reached, broke);
    }

    [Fact]
    public void 貯蓄を取りきる赤字で_旗0は物価が崖を踏み_旗1は四半期の掛け率が導出の上限に収まる()
    {
        const int quarters = 16;
        var off = RunDeficit(0, 1.5, quarters, tolerateBreak: true);
        var on = RunDeficit(1, 1.5, quarters);
        var c = new Coefficients();
        // 上限の導出（式を写さない）：P_after ≤ max(P_before, 循環比) × (1 + W × 四半期の圧の和)。圧の和 ≤ 月数（月の圧 ≤ 1）。循環比 ≤ 1/追従速度（趨勢 ≥ 追従速度 × 比）。
        double cycMax = 1.0 / c.MonetaryTrendSpeed;
        double Bound(double pPrev) => Math.Max(Math.Max(pPrev, cycMax), c.PriceLevelFloor) * (1.0 + c.FiatInflationWeight * 3.0) * (1 + 1e-9);
        Assert.False(on.broke);
        Assert.Equal(quarters, on.reachedQ);
        for (int q = 0; q < quarters; q++)
            Assert.True(on.pAfter[q] <= Bound(on.pPrev[q]), $"旗 1：{q} 四半期目の P {on.pAfter[q]:E3}（前 {on.pPrev[q]:E3}）が導出の上限 {Bound(on.pPrev[q]):E3} を越えた");
        Assert.True(on.maxPressureMonth <= 1.0 * (1 + 1e-9), $"旗 1：月の圧の最大 {on.maxPressureMonth} が割合の上限 1 を越えた");
        // 陽性対照：旗 0 は同じ盤面で上限を破る（崖）
        int viol = 0; for (int q = 0; q < off.reachedQ; q++) if (!(off.pAfter[q] <= Bound(off.pPrev[q]))) viol++;
        Assert.True(viol >= 1 || !off.finiteAll || off.broke, $"旗 0 が崖を踏まない＝この盤面は根を試していない（最大圧 {off.maxPressureMonth:E3}・到達 {off.reachedQ} 四半期）");
        Assert.True(on.finiteAll, "旗 1：物価・期待インフレ・国債金利・国庫・為替のどれかが有限でない");
        output.WriteLine($"旗0：最大月圧={off.maxPressureMonth:E3}・到達={off.reachedQ}四半期・保存の破れで停止={off.broke}・上限破り={viol}／旗1：最大月圧={on.maxPressureMonth:F3}・最大P={on.pAfter.Max():E3}・P(最終)={on.pAfter[^1]:E3}");
    }

    [Fact]
    public void 旗1でもハイパーインフレの道は残る_物価は桁で伸び有限で貨幣は保存される()
    {
        const int quarters = 24;
        var on = RunDeficit(1, 1.5, quarters);   // 保存は盤面の中で四半期ごとに Check 済み
        Assert.True(on.finiteAll);
        output.WriteLine($"max P {on.pAfter.Max():E3} last {on.pAfter[^1]:E3} maxPressMonth {on.maxPressureMonth:E3}");
        Assert.True(on.pAfter.Max() > 100.0, $"旗 1 で物価が桁で伸びない（最大 {on.pAfter.Max():E3}）＝ハイパーインフレの道を塞いだ");
    }

    // ---------------- 実質 GDP が 0 の四半期の趨勢の保持 ----------------

    [Fact]
    public void 実質GDPが0の四半期は旗1で趨勢が動かず_旗0では趨勢が桁で跳ねる()
    {
        double Run(double flag, out double trendBefore, out double trendAfter, out double p0, out double p1)
        {
            var w = Build();
            w.Coef.PriceTrendHoldAtZeroGdp = flag;
            foreach (var r in w.Regions) foreach (var s in r.Producers) s.Output = 0;   // 生産者を止める＝実質 GDP 0
            Assert.True(w.GdpPerMonth.Value <= 1e-9, "実質 GDP が 0 にならない");
            trendBefore = w.MoTrendForInstruments; p0 = w.PriceLevel;
            w.UpdatePriceLevel(w.Coef);
            trendAfter = w.MoTrendForInstruments; p1 = w.PriceLevel;
            return trendAfter / trendBefore;
        }
        double r0 = Run(0, out _, out _, out _, out _);
        double r1 = Run(1, out double t1b, out double t1a, out double pb, out double pa);
        Assert.True(r0 > 1e5, $"旗 0：趨勢の跳ねが {r0}＝汚染を再現しない");
        Assert.Equal(t1b, t1a);   // 旗 1：趨勢は更新の前後で同じ
        Assert.Equal(pb, pa);     // 旗 1：圧が無ければ循環比は P そのもの＝寄せる力 0（P は動かない）
    }

    [Fact]
    public void 実質GDPが正なら旗1でも旗0とビット一致する()
    {
        var a = Build(); var b = Build();
        b.Coef.PriceTrendHoldAtZeroGdp = 1;
        for (int q = 0; q < 6; q++)
        {
            a.UpdatePriceLevel(a.Coef); b.UpdatePriceLevel(b.Coef);
            Assert.True(BitConverter.DoubleToInt64Bits(a.PriceLevel) == BitConverter.DoubleToInt64Bits(b.PriceLevel), $"{q} 四半期目：P が違う");
            Assert.True(BitConverter.DoubleToInt64Bits(a.MoTrendForInstruments) == BitConverter.DoubleToInt64Bits(b.MoTrendForInstruments), $"{q} 四半期目：趨勢が違う");
        }
    }

    // ---------------- 計器 pfix_s・pjump_s（全ての国・まばら） ----------------

    [Fact]
    public void 計器pfixとpjumpは挙げていない国からも出し_旗0でも入口の月を出し_pjumpは崖の四半期を出す()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pob_pfix_" + Guid.NewGuid().ToString("N"));
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
                // 挙げていない国（イギリスの外）の不換の1国を選び、全ての貯蓄を取りきる赤字を作る（旗は 0）
                var target = sim.Nations.First(n => NameOf(n) != "イギリス" && n.World.MonetaryEnabled && n.World.FiscalEnabled);
                var tw = target.World;
                tw.OnGoldStandard = false; tw.Convertibility = 0;
                tw.BorrowCooldownMonths = 99;
                tw.GovAvgCouponPerYear = new RatePerYear(3.0 * 12.0 * Math.Max(1.0, tw.BroadMoney.Value) / Math.Max(1.0, tw.GovDebt.Value));
                for (int q = 0; q < 3; q++)
                {
                    sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q);
                    tw.BorrowCooldownMonths = 99; tw.Convertibility = 0;
                    tw.GovAvgCouponPerYear = new RatePerYear(3.0 * 12.0 * Math.Max(1.0, tw.BroadMoney.Value) / Math.Max(1.0, tw.GovDebt.Value));
                }
                targetName = NameOf(target);
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            var rows = File.ReadAllLines(Path.Combine(dir, "pfix_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
            var h = rows[0]; int iN = Array.IndexOf(h, "nation"), iK = Array.IndexOf(h, "kind"), iOld = Array.IndexOf(h, "pressureOld"), iNew = Array.IndexOf(h, "pressureNew"), iB = Array.IndexOf(h, "priceLevelBefore"), iA = Array.IndexOf(h, "priceLevelAfter");
            double P(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            var mine = rows.Skip(1).Where(r => r[iN] == targetName && r[iK] == "1").ToList();
            Assert.True(mine.Count > 0, $"挙げていない国 {targetName} の pfix_s の kind=1 の行が無い");
            Assert.All(mine, r => Assert.True(P(r[iNew]) <= P(r[iOld]) + 1e-12 && P(r[iNew]) <= 3.0 + 1e-9, $"新の圧 {r[iNew]} が旧 {r[iOld]} を越えた／3 を越えた"));
            Assert.Contains(mine, r => P(r[iOld]) > 1.0);   // 旗 0 でも、旗が入口に入る月（旧の圧が割合を越える月）を出す
            // pjump_s：旗 0 の崖の四半期（P が 100 倍以上）を出す
            var jrows = File.ReadAllLines(Path.Combine(dir, "pjump_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
            var jh = jrows[0]; int jN = Array.IndexOf(jh, "nation"), jB = Array.IndexOf(jh, "priceLevelBefore"), jA = Array.IndexOf(jh, "priceLevelAfter");
            var jm = jrows.Skip(1).Where(r => r[jN] == targetName).ToList();
            Assert.True(jm.Count > 0, "pjump_s に崖の四半期が出ない");
            Assert.All(jm, r => Assert.True(!(P(r[jA]) < 100.0 * P(r[jB]))));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 旗の読み手は切り下げの塊の1か所_圧の分母の関数の呼び手も1か所()
    {
        int reads = 0; string where = "";
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())
        {
            if (!rel.StartsWith("PriceOfBread.SimCore/")) continue;
            if (rel.EndsWith("Coefficients.cs")) continue;   // 宣言
            string code = GoodIdGuardTests.StripCode(File.ReadAllText(f));
            int n = System.Text.RegularExpressions.Regex.Matches(code, "FiatPressureRelativeFloor").Count;
            if (n > 0) { reads += n; where += rel + "×" + n + " "; }
        }
        Assert.True(reads == 1 && where.StartsWith("PriceOfBread.SimCore/Simulation.cs"), $"FiatPressureRelativeFloor の読み手が Simulation の1か所でない（{where}）");
        int writes = 0;
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())
        {
            if (!rel.StartsWith("PriceOfBread.SimCore/")) continue;
            string code = GoodIdGuardTests.StripCode(File.ReadAllText(f));
            writes += System.Text.RegularExpressions.Regex.Matches(code, @"FiatInflationPressure\s*\+=").Count;
        }
        Assert.True(writes == 1, $"FiatInflationPressure へ積む行が {writes} 本（1 本でなければ分母の式が割れる）");
    }

    private static string NameOf(Simulation n) => n.World.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
}
