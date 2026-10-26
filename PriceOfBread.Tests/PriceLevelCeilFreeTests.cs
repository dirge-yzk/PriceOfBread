using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(582) 物価水準の天井の素通り（旗 <see cref="Coefficients.PriceLevelCeilFree"/>・作者 2026-10-26「極端な場合にはハイパーインフレが起きる機構を捨てたくはない。なので、数値としての天井は可能なら排除したい」）の番人＝機構と不変条件。
/// 旗 0 は今の Clamp(P, 床, 天井) のまま・旗 1 は天井を掛けず（床は残す）、P が正の有限でない四半期だけ更新前の P を保つ。紙幣増発の掛け算の道（ハイパーインフレ）は旗に依らず残る。
/// 期待値は World の式を写さない＝旗 0 と旗 1 の同じ盤面の差と、更新の式の外側の性質（天井を越える・床は残る・増発で伸び続ける・有限）だけで書く。
/// 事前登録 <c>measurements/svf2_582_20261026_prereg.txt</c>。</summary>
[Collection("Instruments")]   // 計器の静的な状態（Instruments.Configure）を立てる試験を含む＝他の試験と並列に走らせない（DiscoveryClockTests の世界の合計の欄が計器の On で埋まる干渉を起こした）
public class PriceLevelCeilFreeTests
{
    /// <summary>イギリスの盤面を組み、旗を立て（または立てず）、初回の更新（趨勢の基準を取るだけ）を済ませて返す。</summary>
    private static World Build(int flag)
    {
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent());
        var w = sim.World;
        Assert.True(w.MonetaryEnabled);
        w.Coef.PriceLevelCeilFree = flag;
        w.UpdatePriceLevel(w.Coef);   // 初回は趨勢の基準を取るだけ（P は動かない）
        return w;
    }

    [Fact]
    public void 既定は旗0で天井の欄は4_0のまま()
    {
        var c = new Coefficients();
        Assert.Equal(0, c.PriceLevelCeilFree);
        Assert.Equal(4.0, c.PriceLevelCeil);   // 旗 0 が読む天井（試験側の手書き）
        Assert.Equal(0.4, c.PriceLevelFloor);
    }

    [Fact]
    public void 旗0は物価水準の天井で止まる_旗1は天井を越える()
    {
        var w0 = Build(0); var w1 = Build(1);
        w0.PriceLevel = 50; w1.PriceLevel = 50;                       // 物価を天井よりずっと高く置く（趨勢比は 1 付近）
        w0.UpdatePriceLevel(w0.Coef); w1.UpdatePriceLevel(w1.Coef);
        Assert.Equal(4.0, w0.PriceLevel);                             // 旗 0＝今の天井（手書きの 4.0）
        double sens = w1.Coef.MonetaryPriceSensitivity;
        Assert.True(w1.PriceLevel > 4.0, $"旗1でも天井で止まった（{w1.PriceLevel}）");
        Assert.True(w1.PriceLevel >= 50 * (1.0 - sens) && w1.PriceLevel < 50.0, $"旗1の物価が更新の式の外側の範囲にない（{w1.PriceLevel}）");   // P＋sens×(循環比−P)＝循環比が 0〜(P) の間なら P×(1−sens) 以上で P 未満
    }

    [Fact]
    public void 旗1でも床は残る()
    {
        var w1 = Build(1);
        w1.PriceLevel = 0.01;                                         // 床よりずっと低く置く
        w1.UpdatePriceLevel(w1.Coef);
        Assert.True(w1.PriceLevel >= 0.4, $"旗1で床が消えた（{w1.PriceLevel}）");
    }

    /// <summary>紙幣増発の掛け算の道（FiatInflationPressure→FiatInflationWeight）＝ハイパーインフレの機構が旗1で天井なしに伸び続け、旗0は 4.0 で止まる（機構を捨てていないことの確かめ）。</summary>
    [Fact]
    public void 紙幣増発を続けると旗1は天井なしに伸び続け_旗0は天井で止まる()
    {
        var w0 = Build(0); var w1 = Build(1);
        double prev = w1.PriceLevel; int risingAboveCeil = 0;
        for (int q = 0; q < 40; q++)
        {
            w0.FiatInflationPressure = 1.0; w1.FiatInflationPressure = 1.0;   // 毎四半期、広義マネーと同じ額を刷る（インフレ圧 1）
            w0.UpdatePriceLevel(w0.Coef); w1.UpdatePriceLevel(w1.Coef);
            Assert.True(w0.PriceLevel <= 4.0, $"旗0が天井を越えた（q{q}: {w0.PriceLevel}）");
            Assert.True(double.IsFinite(w1.PriceLevel) && w1.PriceLevel > 0, $"旗1の物価が有限でない（q{q}: {w1.PriceLevel}）");
            if (w1.PriceLevel > 4.0 && w1.PriceLevel > prev) risingAboveCeil++;
            prev = w1.PriceLevel;
        }
        Assert.Equal(4.0, w0.PriceLevel);
        Assert.True(w1.PriceLevel > 100.0, $"紙幣増発を40四半期続けても旗1の物価が桁で伸びない（{w1.PriceLevel}）＝ハイパーインフレの道が閉じている");
        Assert.True(risingAboveCeil >= 20, $"天井の上で伸び続けた四半期が少ない（{risingAboveCeil}）");
        Assert.Equal(0.0, w1.FiatInflationPressure);                  // 圧は毎回消費される（旧と同じ）
    }

    /// <summary>計算が壊れた四半期（増発の掛け算が桁あふれして無限・入力が NaN）では、旗1は更新前の物価を保つ＝NaN も無限も盤面に入らない。旗0は旧のまま天井へ。</summary>
    [Fact]
    public void 無限と非数は旗1で更新前の物価を保つ()
    {
        foreach (double bad in new[] { double.PositiveInfinity, double.NaN, double.MaxValue })
        {
            var w0 = Build(0); var w1 = Build(1);
            double before1 = w1.PriceLevel;
            w1.FiatInflationPressure = bad; w0.FiatInflationPressure = bad;
            w0.UpdatePriceLevel(w0.Coef); w1.UpdatePriceLevel(w1.Coef);
            Assert.True(double.IsFinite(w1.PriceLevel) && w1.PriceLevel > 0, $"圧 {bad} で旗1の物価が有限でない（{w1.PriceLevel}）");
            if (double.IsPositiveInfinity(bad)) Assert.Equal(before1, w1.PriceLevel, 9);   // 無限の圧は更新前の P を保つ（NaN の圧は圧そのものが消える＝通常の更新・MaxValue の圧は有限の巨大な P になる＝天井なしの結果）
            Assert.True(w0.PriceLevel <= 4.0);                        // 旗0は旧どおり天井で止まる
        }
    }

    /// <summary>★(582) 反証役の点検（中#2）：ハイパーインフレの領域（旗1で毎四半期の紙幣増発が P を桁で押し上げる）を実際に通しても、下流（期待インフレ→国債金利・政策金利・財政・貨幣の保存）が NaN・無限を出さず、貨幣の保存則が破れない。
    /// 基線の種1〜10ではこの領域を通らない（増発による P＞4 が 0 行）＝腕Bの緑では下流の安全が示されない。ここで直接通す。期待値は性質（有限・保存）だけで、P の値を式で写さない。</summary>
    [Fact]
    public void 旗1で紙幣増発を毎四半期続けても下流が有限で貨幣の保存が破れない()
    {
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent());
        var w = sim.World;
        Assert.True(w.MonetaryEnabled);
        w.Coef.PriceLevelCeilFree = 1;
        var mc = new MoneyConservation(w);
        double maxP = 0;
        for (int q = 0; q < 24; q++)
        {
            w.FiatInflationPressure = 1.0;   // 毎四半期、広義マネーと同じ額を刷った扱い（次の月次の更新で P に乗る）
            sim.Run(1);
            maxP = Math.Max(maxP, w.PriceLevel);
            Assert.True(double.IsFinite(w.PriceLevel) && w.PriceLevel > 0, $"q{q}: P が有限でない（{w.PriceLevel}）");
            Assert.True(double.IsFinite(w.ExpectedInflationPerYear.Value), $"q{q}: 期待インフレが有限でない（{w.ExpectedInflationPerYear.Value}）");
            Assert.True(double.IsFinite(w.GovBondRatePerYear.Value) && double.IsFinite(w.GovAvgCouponPerYear.Value), $"q{q}: 国債金利が有限でない");
            Assert.True(double.IsFinite(w.InterestRate), $"q{q}: 政策金利が有限でない");
            Assert.True(double.IsFinite(w.Treasury.Value), $"q{q}: 国庫が有限でない");
            Assert.True(double.IsFinite(w.CostPriceLevel), $"q{q}: 自己費用の物価が有限でない");
            mc.Check(w);
        }
        Assert.True(maxP > 100.0, $"紙幣増発を24四半期続けても P が桁で伸びない（最大 {maxP}）＝この番人がハイパーインフレの領域を通っていない");
    }

    /// <summary>★(582) 物価の天井・床に触れた行の表 <c>pclamp_s</c>（全ての国・まばら）は、挙げていない国からも出て、挙げた国の物価の表 <c>money_s</c> と食い違わない。
    /// 天井を低く（0.6）置いて助走の物価を天井に触れさせる。期待値は別の表（money_s の増発の後の P）から作る＝同じ出所にしない。</summary>
    [Fact]
    public void 天井と床に触れた行は全ての国から出て_挙げた国の物価の表と一致する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pclamp_" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = new MultiNationSim([.. WorldData.Nations()], 1789UL, _ => new UtilityAgent());
            foreach (var n in sim.Nations) n.World.Coef.PriceLevelCeil = 0.6;
            Instruments.Configure(dir, "イギリス");
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup(null, 4);
                Instruments.Phase(sim, 1789UL, "warm");
                for (int q = 0; q < 2; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }

            string[][] Read(string path) => System.IO.File.ReadAllLines(path).Select(l => l.Split('\t')).ToArray();
            var pc = Read(System.IO.Path.Combine(dir, "pclamp_s1789.tsv"));
            var ph = pc[0]; var prow = pc.Skip(1).ToList();
            Assert.True(prow.Count > 0, "天井を 0.6 にしても pclamp_s に行が無い");
            int iNat = Array.IndexOf(ph, "nation"), iKind = Array.IndexOf(ph, "kind"), iFiat = Array.IndexOf(ph, "priceLevelAfterFiatIndex"), iT = Array.IndexOf(ph, "time");
            Assert.True(prow.Select(r => r[iNat]).Distinct().Count() > 1, "挙げた国（イギリス）の外の国の行が無い");
            Assert.All(prow, r =>
            {
                double v = double.Parse(r[iFiat], System.Globalization.CultureInfo.InvariantCulture);
                if (r[iKind] == "ceil") Assert.True(v > 0.6, $"ceil の行の AfterFiat {v} が天井 0.6 以下");
                else { Assert.Equal("floor", r[iKind]); Assert.True(v < 0.4, $"floor の行の AfterFiat {v} が床 0.4 以上"); }
            });

            // 挙げた国の物価の表（別の書き手）と突き合わせる：イギリスの更新の行のうち AfterFiat が天井を越えた行の時刻の集合が一致する。
            var mo = Read(System.IO.Path.Combine(dir, "money_s1789.tsv"));
            var mh = mo[0]; int mT = Array.IndexOf(mh, "time"), mEv = Array.IndexOf(mh, "event"), mF = Array.IndexOf(mh, "priceLevelAfterFiatIndex");
            var fromMoney = mo.Skip(1).Where(r => r[mEv] == "update" && double.Parse(r[mF], System.Globalization.CultureInfo.InvariantCulture) > 0.6).Select(r => r[mT]).OrderBy(x => x).ToList();
            var fromClamp = prow.Where(r => r[iNat] == "イギリス" && r[iKind] == "ceil").Select(r => r[iT]).OrderBy(x => x).ToList();
            Assert.Equal(fromMoney, fromClamp);
            Assert.True(fromMoney.Count > 0, "イギリスが天井 0.6 に一度も触れない＝突き合わせが空振り");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
