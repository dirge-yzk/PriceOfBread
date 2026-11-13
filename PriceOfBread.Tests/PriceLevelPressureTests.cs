using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(603) 線 pfix：価格水準の発散の根の番人＝機構と不変条件だけ。
/// 根＝切り下げの月の物価への圧の分母（Simulation の切り下げの塊）が、貯蓄が尽きた月に絶対値の床 1.0 で割られ、圧が通貨の額そのものになった（P が四半期で 1e6 倍・その後は請求が P に比例して P の 2 乗の暴走）。
/// 直し＝分母の床は分子自身の大きさ（月の圧は 0〜1 の割合）・実質 GDP が正でない四半期は趨勢を更新しない（腕 A・B1 で基線と同じ軌道と確かめて焼き込み、旗 <c>FiatPressureRelativeFloor</c>・<c>PriceTrendHoldAtZeroGdp</c> は消した）。
/// 期待値は世界を手で作り替えた結果と、式の外側の性質（割合・保存・有限・上限の導出）で書く。事前登録 <c>measurements/pfix603_20261028_prereg.txt</c>。
/// 旧の式（絶対値の床 1.0）は試験側の手書きで持つ＝陽性対照（旧の式は貯蓄が尽きた月に圧が通貨の額になる）。旗の復活は禁じる（最後の試験）。</summary>
[Collection("Instruments")]   // Instruments の静的な状態（Configure）を立てる試験を含む
public class PriceLevelPressureTests(ITestOutputHelper output)
{
    // ---------------- 圧の分母（Fiscal.FiatPressureOfLedger・純関数・★(616) で M 案から置き換えた） ----------------

    [Fact]
    public void 旧の式は貯蓄が尽きた月に圧が通貨の額になり_今の式は割合に収まる()
    {
        // 試験側の手書きの旧の式（陽性対照）：貨幣が 1 未満だと絶対値の床 1.0 で割られる
        double Old(double dev, double broad) => dev / Math.Max(1.0, broad);
        Assert.True(Old(9.5086e5, 3.5e-12) > 1e5);
        Assert.True(Old(9.5086e5, -5.5e11) > 1e5);
        // 今の式（分母＝切り下げ前の貯蓄＋発行）：取った額は（貯蓄＋発行）を越えない台帳の範囲で、圧は (0,1]
        foreach (double sav in new[] { 0.0, 3.5e-12, 0.7, 55.0, 1e7, 1e300 })
            foreach (double issue in new[] { 0.0, 1e-6, 5.0, 9.5086e5 })
            {
                double den = sav + issue;
                if (!(den > 0)) continue;
                foreach (double f in new[] { 1e-9, 0.5, 1.0 })
                {
                    double p = Fiscal.FiatPressureOfLedger(den * f, sav, issue);
                    Assert.True(p > 0 && p <= 1.0, $"貯蓄 {sav}・発行 {issue}・割合 {f}：圧 {p} が (0,1] の外");
                }
            }
        // 貯蓄を全て取った月（貯蓄が 0 付近・発行だけ）は圧がちょうど 1（旧は 9.5e5）
        Assert.Equal(1.0, Fiscal.FiatPressureOfLedger(9.5086e5, 0.0, 9.5086e5));
    }

    [Fact]
    public void 圧は貯蓄について単調で_取り上げた額が分母の全部なら1()
    {
        foreach (double dev in new[] { 1e-6, 0.5, 37.0, 9.5e5, 1e9 })
        {
            double prev = double.PositiveInfinity;
            foreach (double sav in new[] { dev * 0.25, dev * 0.5, dev, dev * 2, dev * 20, dev * 1e4 })
            {
                double p = Fiscal.FiatPressureOfLedger(dev, sav, 0.0);
                Assert.True(p <= prev, $"貯蓄が増えたのに圧が増えた（devalue {dev}・貯蓄 {sav}）");
                prev = p;
                Assert.Equal(dev / sav, p);   // 取った割合（定義）
            }
        }
    }

    [Fact]
    public void 非数の分母は圧0を積み_四半期の更新が壊れず物価は有限のまま()
    {
        // 否定形：分母が非数（`den > 0` が偽）の月は圧 0＝NaN を積まない
        Assert.Equal(0.0, Fiscal.FiatPressureOfLedger(5.0, double.NaN, 0.0));
        var w = Build();
        double p0 = w.PriceLevel;
        w.FiatInflationPressure = Fiscal.FiatPressureOfLedger(5.0, double.NaN, 0.0);
        w.UpdatePriceLevel(w.Coef);
        Assert.True(double.IsFinite(w.PriceLevel) && w.PriceLevel > 0, $"非数の分母で物価が壊れた（{p0} → {w.PriceLevel}）");
        Assert.Equal(0.0, w.FiatInflationPressure);
        // 更新の否定形そのもの（World.cs）：非数の圧が積まれても消える
        w.FiatInflationPressure = double.NaN;
        w.UpdatePriceLevel(w.Coef);
        Assert.True(double.IsFinite(w.PriceLevel) && w.PriceLevel > 0);
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
    internal (double[] pAfter, double[] pPrev, double maxPressureMonth, bool finiteAll, int reachedQ, bool broke) RunDeficit(double fMonth, int quarters, ulong seed = 19)   // FsxTests が同じ盤面を踏む
    {
        var cfg = OpeningFixture.Named("イギリス");
        cfg.InitialMonetaryLaw = MonetaryStandardLaw.Fiat;
        var sim = new Simulation(cfg.ToScenario(seed), new NullAgent());
        var w = sim.World;
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
                catch (InvariantViolationException) { broke = true; break; }   // 保存の破れは止めずに記録して、下で赤にする
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
    public void 貯蓄を取りきる赤字でも_四半期の掛け率が導出の上限に収まり_貨幣は保存される()
    {
        const int quarters = 16;
        var on = RunDeficit(1.5, quarters);
        var c = new Coefficients();
        // 上限の導出（式を写さない）：P_after ≤ max(P_before, 循環比) × (1 + W × 四半期の圧の和)。圧の和 ≤ 月数（月の圧 ≤ 1）。循環比 ≤ 1/追従速度（趨勢 ≥ 追従速度 × 比）。
        double cycMax = 1.0 / c.MonetaryTrendSpeed;
        double Bound(double pPrev) => Math.Max(Math.Max(pPrev, cycMax), c.PriceLevelFloor) * (1.0 + c.FiatInflationWeight * 3.0) * (1 + 1e-9);
        Assert.False(on.broke, "貨幣保存が破れた");
        Assert.Equal(quarters, on.reachedQ);
        for (int q = 0; q < quarters; q++)
            Assert.True(on.pAfter[q] <= Bound(on.pPrev[q]), $"{q} 四半期目の P {on.pAfter[q]:E3}（前 {on.pPrev[q]:E3}）が導出の上限 {Bound(on.pPrev[q]):E3} を越えた");
        Assert.True(on.maxPressureMonth <= 1.0 * (1 + 1e-9), $"月の圧の最大 {on.maxPressureMonth} が割合の上限 1 を越えた");
        Assert.True(on.finiteAll, "物価・期待インフレ・国債金利・国庫・為替のどれかが有限でない");
        Assert.True(on.maxPressureMonth > 0.5, $"この盤面は貯蓄を取りきる月を作っていない（月の圧の最大 {on.maxPressureMonth}）");
        output.WriteLine($"最大月圧={on.maxPressureMonth:F3}・最大P={on.pAfter.Max():E3}・P(最終)={on.pAfter[^1]:E3}");
    }

    [Fact]
    public void ハイパーインフレの道は残る_物価は桁で伸び有限で貨幣は保存される()
    {
        const int quarters = 24;
        var on = RunDeficit(1.5, quarters);   // 保存は盤面の中で四半期ごとに Check 済み
        Assert.False(on.broke);
        Assert.True(on.finiteAll);
        Assert.True(on.pAfter.Max() > 100.0, $"物価が桁で伸びない（最大 {on.pAfter.Max():E3}）＝ハイパーインフレの道を塞いだ");
    }

    // ---------------- 実質 GDP が 0 の四半期の趨勢の保持 ----------------

    [Fact]
    public void 実質GDPが0の四半期は趨勢が動かず_物価も動かない()
    {
        var w = Build();
        foreach (var r in w.Regions) foreach (var s in r.Producers) s.Output = 0;   // 生産者を止める＝実質 GDP 0
        Assert.True(w.GdpPerMonth.Value <= 1e-9, "実質 GDP が 0 にならない");
        double t0 = w.MoTrendForInstruments, p0 = w.PriceLevel;
        Assert.True(t0 > 0);
        w.UpdatePriceLevel(w.Coef);
        Assert.Equal(t0, w.MoTrendForInstruments);   // 趨勢は更新の前後で同じ（旧は貨幣÷1e-9 を取り込み、趨勢が 1e5 倍以上に跳ねて戻らなかった）
        Assert.Equal(p0, w.PriceLevel);              // 圧が無ければ循環比は P そのもの＝寄せる力 0
    }

    // ---------------- 計器 pfix_s・pjump_s（全ての国・まばら） ----------------

    [Fact]
    public void 計器pfixとpjumpは挙げていない国からも出し_旧の式なら割合を越えた月を出し_pjumpは跳ねた四半期を出す()
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
                // 挙げていない国（イギリスの外）の不換の1国を選び、全ての貯蓄を取りきる赤字を作る
                var target = sim.Nations.First(n => NameOf(n) != "イギリス" && n.World.MonetaryEnabled && n.World.FiscalEnabled);
                var tw = target.World;
                tw.OnGoldStandard = false; tw.Convertibility = 0;
                tw.BorrowCooldownMonths = 99;
                tw.GovAvgCouponPerYear = new RatePerYear(3.0 * 12.0 * Math.Max(1.0, tw.BroadMoney.Value) / Math.Max(1.0, tw.GovDebt.Value));
                for (int q = 0; q < 3; q++)
                {
                    if (q == 2) tw.FiatInflationPressure = 1000;   // 計器の確かめ用：圧を直接積んで 1 四半期で 100 倍以上の跳ねを作る（pjump_s の経路）
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
            Assert.Contains(mine, r => P(r[iOld]) > 1.0);   // 旧の式なら圧が割合を越えた月（今の分母が効いた月）を出す
            // pjump_s：圧を直接積んだ四半期（P が 100 倍以上）を出す
            var jrows = File.ReadAllLines(Path.Combine(dir, "pjump_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
            var jh = jrows[0]; int jN = Array.IndexOf(jh, "nation"), jB = Array.IndexOf(jh, "priceLevelBefore"), jA = Array.IndexOf(jh, "priceLevelAfter");
            var jm = jrows.Skip(1).Where(r => r[jN] == targetName).ToList();
            Assert.True(jm.Count > 0, "pjump_s に跳ねた四半期が出ない");
            Assert.All(jm, r => Assert.True(!(P(r[jA]) < 100.0 * P(r[jB]))));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 旗は焼き込まれて係数に無く_圧を積む行は1本()
    {
        Assert.Null(typeof(Coefficients).GetField("FiatPressureRelativeFloor"));   // 旗の復活を禁じる（常に有効）
        Assert.Null(typeof(Coefficients).GetField("PriceTrendHoldAtZeroGdp"));
        Assert.Null(typeof(Coefficients).GetField("FiatPressureDenomLedger"));   // ★(616) 採って焼き込んだ旗の復活も禁じる
        Assert.Null(typeof(Coefficients).GetField("TreasuryExcessRepays"));
        int writes = 0;
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())
        {
            if (!GoodIdGuardTests.IsEngineRel(rel)) continue;
            string code = GoodIdGuardTests.StripCode(File.ReadAllText(f));
            writes += System.Text.RegularExpressions.Regex.Matches(code, @"FiatInflationPressure\s*\+=").Count;
        }
        Assert.True(writes == 1, $"FiatInflationPressure へ積む行が {writes} 本（1 本でなければ分母の式が割れる）");
    }

    private static string NameOf(Simulation n) => n.World.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
}
