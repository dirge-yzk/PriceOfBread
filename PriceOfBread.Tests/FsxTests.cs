using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(616) 線 fsx：国庫の出口と切り下げの圧の分母（S 案）の番人＝機構と不変条件だけ。事前登録 <c>measurements/fsx616_20261028_prereg.txt</c>。
/// 段1（2026-10-28 に採って焼き込み・旗は消えた）：圧の分母＝切り下げ前の貯蓄＋新規発行（分子と同じ台帳）＝圧 ≤ 1 が式の外から挟まずに成り立つ。
/// 段2（同上）：線（<see cref="UtilityAgent.OpulenceLineOf"/>）を超えた国庫をまず国債の返済へ。年 1% は下限。
/// 期待値は式の写しでなく、式の外側の性質（割合・返済後の国庫が線に着く・国庫が線の下ならビット一致・台帳）で書く。AI とプレイヤー（NullAgent）の両方の盤面で踏む。</summary>
[Collection("Instruments")]   // Instruments の静的な状態（Configure）を立てる試験を含む
public class FsxTests(ITestOutputHelper output)
{
    // ---------------- 段1：圧の分母（Fiscal.FiatPressureOfLedger・純関数） ----------------

    [Fact]
    public void S案の圧は台帳の恒等式で1以下で_式の外からは挟まない()
    {
        foreach (double sav in new[] { 0.0, 1e-12, 0.7, 55.0, 1e7, 1e300 })
            foreach (double issue in new[] { 0.0, 1e-9, 3.0, 1e5 })
            {
                double den = sav + issue;
                if (!(den > 0)) { Assert.Equal(0.0, Fiscal.FiatPressureOfLedger(0.0, sav, issue)); continue; }
                foreach (double f in new[] { 1e-9, 0.25, 0.5, 1.0 })
                {
                    double dev = den * f;   // 台帳の恒等式の範囲（取った額 ≤ 取れる額＋発行）
                    double p = Fiscal.FiatPressureOfLedger(dev, sav, issue);
                    Assert.True(p > 0 && p <= 1.0, $"貯蓄 {sav}・発行 {issue}・割合 {f}：圧 {p} が (0,1] の外");
                    Assert.True(Math.Abs(p - f) <= 1e-12 * Math.Max(1.0, f), $"圧 {p} が取った割合 {f} でない");
                }
                // 陽性対照：台帳の範囲を越える入力なら 1 を越える＝式の外から 1 に挟んでいない（M 案の Math.Max(…, devalue) との違い）
                Assert.True(Fiscal.FiatPressureOfLedger(2.0 * den, sav, issue) > 1.5);
                // 貯蓄を取りきる月（haircut＝貯蓄の全部）は圧がちょうど 1（分母から発行を落とす突然変異では 1 を越える）
                Assert.Equal(1.0, Fiscal.FiatPressureOfLedger(sav + issue, sav, issue));
            }
        Assert.Equal(0.0, Fiscal.FiatPressureOfLedger(5.0, double.NaN, 0.0));   // 否定形＝NaN の分母は圧 0（NaN を積まない）
        Assert.Equal(0.0, Fiscal.FiatPressureOfLedger(0.0, 0.0, 0.0));
    }

    [Fact]
    public void S案の圧はM案を越えない_分母が切り下げ前の貯蓄と発行だから()
    {
        // 導出：haircut を h・発行を i・切り下げ前の貯蓄を T とすると M の分母＝max(T−h−負の貯蓄, h+i)、S の分母＝T+i。T−h ≤ T+i かつ h+i ≤ T+i なので S の分母 ≥ M の分母。
        foreach (double T in new[] { 10.0, 1e3, 7.5e5 })
            foreach (double i in new[] { 0.0, 0.1 * T })
                foreach (double h in new[] { 0.01 * T, 0.3 * T, 0.9 * T, T })
                {
                    double dev = h + i, broadAfter = T - h;
                    double m = Instruments.FiatPressureM(dev, broadAfter), s = Fiscal.FiatPressureOfLedger(dev, T, i);
                    Assert.True(s <= m + 1e-12, $"T={T} h={h} i={i}：S の圧 {s} が M の圧 {m} を越えた");
                    Assert.True(s <= 1.0 + 1e-12);
                }
    }

    // ---------------- 段1：実際の赤字で切り下げの道を通す（pfix の盤面を旗 1 で踏む） ----------------

    [Fact]
    public void 貯蓄を取りきる赤字の月の圧は1以下で_掛け率は導出の上限に収まり_貨幣は保存される()
    {
        const int quarters = 16;
        var on = new PriceLevelPressureTests(output).RunDeficit(1.5, quarters);
        var c = new Coefficients();
        double cycMax = 1.0 / c.MonetaryTrendSpeed;
        double Bound(double pPrev) => Math.Max(Math.Max(pPrev, cycMax), c.PriceLevelFloor) * (1.0 + c.FiatInflationWeight * 3.0) * (1 + 1e-9);
        Assert.False(on.broke, "貨幣保存が破れた");
        Assert.Equal(quarters, on.reachedQ);
        for (int q = 0; q < quarters; q++)
            Assert.True(on.pAfter[q] <= Bound(on.pPrev[q]), $"{q} 四半期目の P {on.pAfter[q]:E3}（前 {on.pPrev[q]:E3}）が導出の上限 {Bound(on.pPrev[q]):E3} を越えた");
        Assert.True(on.maxPressureMonth <= 1.0 * (1 + 1e-9), $"月の圧の最大 {on.maxPressureMonth} が台帳の上限 1 を越えた");
        Assert.True(on.finiteAll, "物価・期待インフレ・国債金利・国庫・為替のどれかが有限でない");
        Assert.True(on.maxPressureMonth > 0.5, $"この盤面は貯蓄を取りきる月を作っていない（月の圧の最大 {on.maxPressureMonth}）");
    }

    [Fact]
    public void ハイパーインフレの道は残る_物価は桁で伸び有限で貨幣は保存される()
    {
        var on = new PriceLevelPressureTests(output).RunDeficit(1.5, 24);
        Assert.False(on.broke);
        Assert.True(on.finiteAll);
        Assert.True(on.pAfter.Max() > 100.0, $"物価が桁で伸びない（最大 {on.pAfter.Max():E3}）＝ハイパーインフレの道を塞いだ");
    }

    // ---------------- 段2：国庫の出口（Fiscal.Step の減債） ----------------

    private const double Ample = 1e18;

    private static void Month(World w, double r, double spare)
    {
        w.FiscalFundingRatio = new Share(r); Fiscal.Step(w, w.Coef, spare);
        Fiscal.Borrow(w, w.Coef, spare);
    }

    /// <summary>英を 1 四半期温め、国債以外の流れ（税・福祉・年金・外債）を止め、1 回起債した盤面（FiscalTests の QuietBritain と同じ作り）。国債は全額国内の持ち手。</summary>
    private static World Board(bool ai)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), ai ? new UtilityAgent() : new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.TaxRate = new Share(0); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;
        w.Coef.ForeignBondCapacity = new Share(0);
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0);
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.BondHoldings = new MoneyStock(0);
        w.LastTariffRevenuePerClearing = (w.Coef.MonthsPerClearing * w.NominalGdpPerMonth).Value;
        Month(w, 0.5, -Ample);   // r＜1 の月に起債（払えない額が 1 件の額より大きい＝満額の 1 件）
        Assert.True(w.GovDebt.Value > 2e3, $"前提が崩れた：起債が立っていない（{w.GovDebt}）");
        return w;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 国庫が線の下の月は年1パーセントの減債のまま(bool ai)
    {
        var b = Board(ai);
        double line = UtilityAgent.OpulenceLineOf(b);
        Assert.True(line > 1.0);
        b.SetTreasuryRaw(0.1 * line);
        double d0 = b.GovDebt.Value, t0 = b.Treasury.Value;
        Month(b, 1.0, Ample);
        Assert.True(b.Treasury.Value < UtilityAgent.OpulenceLineOf(b), "前提が崩れた：国庫が線の下のまま");
        double repaid = d0 - b.GovDebt.Value;
        double yearOnePercent = d0 * b.Coef.BondRepayFracPerMonth.Value;   // 年 1% の額（定義）
        Assert.True(Math.Abs(repaid - yearOnePercent) <= 1e-9 * d0, $"返済 {repaid} が年 1% の額 {yearOnePercent} でない（国庫 {t0}・線 {line}）");
        new BondLedger().Check(b);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 線を超えた国庫はまず国債の返済に回り_返済の後の国庫は線に着き_台帳は閉じる(bool ai)
    {
        var b = Board(ai);
        double line = UtilityAgent.OpulenceLineOf(b), d0 = b.GovDebt.Value;
        b.SetTreasuryRaw(line + 0.4 * d0);
        Month(b, 1.0, Ample);
        double lineB = UtilityAgent.OpulenceLineOf(b);
        double tol = 1e-9 * (lineB + d0);
        Assert.True(Math.Abs(b.Treasury.Value - lineB) <= tol, $"返済の後の国庫 {b.Treasury.Value} が線 {lineB} に着いていない");
        // 年 1% だけの返済は月に d0 の 0.1% 未満。線を超えた分まで返しているなら 10 倍以上（その月の他の支払いで超過は 0.4 d0 より小さくなる）
        Assert.True(d0 - b.GovDebt.Value > 0.1 * d0, $"国債の残高が年 1% を大きく越えて減っていない（{b.GovDebt.Value}・起債 {d0}）");
        new BondLedger().Check(b);
    }

    [Fact]
    public void 国庫が債務を大きく越えるなら国債は全て返り_残高は負にならない()
    {
        var b = Board(false);
        double line = UtilityAgent.OpulenceLineOf(b), d0 = b.GovDebt.Value;
        b.SetTreasuryRaw(line + 5.0 * d0);
        Month(b, 1.0, Ample);
        Assert.True(b.GovDebt.Value >= 0 && b.GovDebt.Value <= 1e-6 * d0, $"国債が返りきっていない（{b.GovDebt.Value}・起債 {d0}）");
        new BondLedger().Check(b);
    }

    [Fact]
    public void 余剰が国庫を下回る月は余剰で頭打ち_線の読みが非数でも年1パーセントに倒れる()
    {
        // 余剰（spare）が小さい月：返済は余剰を越えない
        var b = Board(false);
        double line = UtilityAgent.OpulenceLineOf(b), d0 = b.GovDebt.Value, spare = 0.05 * d0;
        b.SetTreasuryRaw(line + 5.0 * d0);
        Month(b, 1.0, spare);
        Assert.True(d0 - b.GovDebt.Value <= spare * (1 + 1e-9) + 1e-9, $"返済 {d0 - b.GovDebt.Value} が余剰 {spare} を越えた");
        Assert.True(d0 - b.GovDebt.Value > 0.5 * spare, "前提が崩れた：余剰まで返していない");
        // 否定形：線が非数（兵士給与が非数）の月は超過 0 に倒れ、年 1% になる
        var b2 = Board(false);
        double d2 = b2.GovDebt.Value;
        b2.SetTreasuryRaw(line + 5.0 * d2);
        b2.LastSoldierPayPerMonth = new MoneyPerMonth(double.NaN);
        Month(b2, 1.0, Ample);
        Assert.True(double.IsFinite(b2.GovDebt.Value));
        Assert.True(Math.Abs((d2 - b2.GovDebt.Value) - d2 * b2.Coef.BondRepayFracPerMonth.Value) <= 1e-9 * d2, "線が非数の月が年 1% に倒れていない");
    }

    [Fact]
    public void 旗は焼き込まれて係数に無く_減債の行と圧の行は1本()
    {
        Assert.Null(typeof(Coefficients).GetField("FiatPressureDenomLedger"));   // 旗の復活を禁じる（常に有効）
        Assert.Null(typeof(Coefficients).GetField("TreasuryExcessRepays"));
        int calls = 0;   // 本体（Simulation）が圧の分母を S 案の関数から取る行は 1 本（M 案へ戻る別の式を足さない）
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())
        {
            if (rel != "PriceOfBread.SimCore/Simulation.cs") continue;
            string code = GoodIdGuardTests.StripCode(File.ReadAllText(f));
            calls += System.Text.RegularExpressions.Regex.Matches(code, @"FiatPressureOfLedger\s*\(").Count;
        }
        Assert.True(calls == 1, $"Simulation.cs の FiatPressureOfLedger の呼びが {calls} 本（1 本のはず）");
    }

    // ---------------- 計器 fsx_s（全ての国・国×四半期） ----------------

    [Fact]
    public void 計器fsxは挙げていない国からも出し_超過とMとSの両方を出す()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pob_fsx_" + Guid.NewGuid().ToString("N"));
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
                var target = sim.Nations.First(n => NameOf(n) != "イギリス" && n.World.MonetaryEnabled && n.World.FiscalEnabled && n.World.GovDebt.Value > 0);
                var tw = target.World;
                targetName = NameOf(target);
                tw.OnGoldStandard = false; tw.Convertibility = 0;
                tw.Coef.FiscalStressBufferMonths = 1e-6;   // 潤沢線を床（1×8）まで下げる＝国庫は線を超える（国庫そのものは動かさない＝貨幣保存を壊さない）
                for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            var rows = File.ReadAllLines(Path.Combine(dir, "fsx_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
            var h = rows[0];
            int iN = Array.IndexOf(h, "nation"), iEx = Array.IndexOf(h, "excessWouldSum"), iM = Array.IndexOf(h, "pressureMSum"), iS = Array.IndexOf(h, "pressureSSum"), iF = Array.IndexOf(h, "pressureForceSum"),
                iDev = Array.IndexOf(h, "devalueMonths"), iAbove = Array.IndexOf(h, "repayAboveFloorSum"), iLine = Array.IndexOf(h, "lineEnd");
            Assert.True(iN >= 0 && iEx >= 0 && iM >= 0 && iS >= 0 && iF >= 0 && iDev >= 0 && iAbove >= 0 && iLine >= 0, "fsx_s の列が足りない");
            double P(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            var mine = rows.Skip(1).Where(r => r[iN] == targetName).ToList();
            Assert.True(mine.Count > 0, $"挙げていない国 {targetName} の fsx_s の行が無い");
            Assert.Contains(mine, r => P(r[iEx]) > 0);                       // 線を超えた国庫の額を出す
            int iDone = Array.IndexOf(h, "repayDoneSum"), iFloor = Array.IndexOf(h, "repayFloorSum");
            Assert.All(rows.Skip(1), r => Assert.True(P(r[iDone]) >= P(r[iFloor]) - 1e-9, $"{r[iN]}：返した額 {r[iDone]} が下限 {r[iFloor]} を下回った"));   // 年 1% は下限
            Assert.All(rows.Skip(1), r => { if (int.Parse(r[iDev]) > 0) Assert.True(P(r[iS]) <= P(r[iM]) + 1e-9, $"{r[iN]}：S の和 {r[iS]} が M の和 {r[iM]} を越えた"); });
            Assert.All(rows.Skip(1), r => Assert.Equal(P(r[iS]), P(r[iF]), 12));   // 効いている式は S
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 効いている圧はS案で_穏やかな切り下げの月はSがMより小さい_計器の和が本体の圧と一致する()
    {
        // 貯蓄の約 2 割を毎月取る赤字（利払いの請求が広義の貨幣の 0.2 倍）＝S（分母＝切り下げ前の貯蓄＋発行）は約 0.2・M（分母＝切り下げの後の貯蓄）は約 0.25 で、両者が違う盤面。
        // 本体が M 案に戻ると、積んだ圧 pressureForceSum が S の和でなく M の和になる＝この試験が赤になる（式の出所の文字の数えだけに頼らない番人）
        string dir = Path.Combine(Path.GetTempPath(), "pob_fsx2_" + Guid.NewGuid().ToString("N"));
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
                var target = sim.Nations.First(n => NameOf(n) != "イギリス" && n.World.MonetaryEnabled && n.World.FiscalEnabled);
                var tw = target.World;
                targetName = NameOf(target);
                void Press()
                {
                    tw.OnGoldStandard = false; tw.Convertibility = 0; tw.BorrowCooldownMonths = 99;
                    tw.GovAvgCouponPerYear = new RatePerYear(0.2 * 12.0 * Math.Max(1.0, tw.BroadMoney.Value) / Math.Max(1.0, tw.GovDebt.Value));
                }
                Press();
                for (int q = 0; q < 4; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); Press(); }
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            var rows = File.ReadAllLines(Path.Combine(dir, "fsx_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
            var h = rows[0];
            int iN = Array.IndexOf(h, "nation"), iM = Array.IndexOf(h, "pressureMSum"), iS = Array.IndexOf(h, "pressureSSum"), iF = Array.IndexOf(h, "pressureForceSum"), iDev = Array.IndexOf(h, "devalueMonths");
            Assert.True(iN >= 0 && iM >= 0 && iS >= 0 && iF >= 0 && iDev >= 0, "fsx_s の列が足りない");
            double P(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            var dev = rows.Skip(1).Where(r => r[iN] == targetName && int.Parse(r[iDev]) > 0).ToList();
            Assert.True(dev.Count > 0, $"{targetName} に切り下げの月が無い（この盤面は赤字を作っていない）");
            Assert.Contains(dev, r => P(r[iS]) < P(r[iM]) - 1e-9);   // S と M が違う盤面（S のほうが小さい）
            Assert.All(dev, r => Assert.Equal(P(r[iS]), P(r[iF]), 12));   // 積んだ圧は S の和（M の和ではない）
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    private static string NameOf(Simulation n) => n.World.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
}
