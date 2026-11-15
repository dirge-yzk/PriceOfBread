using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1168) 線 c115sb 項2：退蔵率の上限 HoardRatioMax を外した後の守り（Credit.Step の退蔵率）の機構の番人。
/// 退蔵率＝CurrencyHoldingRatio×(1＋HoardPanicWeight×(1−信頼))・銀行の金＝基礎貨幣×(1−退蔵率)。
/// 負の値は 0（銀行の金が基礎貨幣を超えない）・非有限は既定の退蔵率（CurrencyHoldingRatio）へ倒す。期待は試験が手で組む。
/// 盤面＝英を 1 か月温めた世界（Credit.Step が銀行を持つ）。信頼は Step の頭の値で退蔵率に入る。
/// </summary>
public class CreditHoardGuardTests
{
    private static World Board(double weight, double confidence)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.Coef.HoardPanicWeight = weight;
        w.Bank!.Confidence = confidence;
        Assert.True(w.BaseMoney.Value > 1.0, "前提＝基礎貨幣が正");
        return w;
    }

    [Fact]
    public void 退蔵率が負になる入力は0へ倒れ銀行の金は基礎貨幣に等しい()
    {
        var w = Board(-20.0, 0.0);   // 0.1×(1−20×1)＜0
        Assert.True(w.Coef.CurrencyHoldingRatio.Value * (1.0 + w.Coef.HoardPanicWeight * (1.0 - w.Bank!.Confidence)) < 0, "前提＝素の退蔵率が負");
        double baseMoney = w.BaseMoney.Value;
        PriceOfBread.SimCore.Credit.Step(w, w.Coef);
        Assert.Equal(baseMoney, w.Bank!.Gold.Value, 6);
    }

    [Fact]
    public void 退蔵率が非有限なら既定の退蔵率へ倒れる()
    {
        var w = Board(double.NaN, 0.5);
        double baseMoney = w.BaseMoney.Value;
        PriceOfBread.SimCore.Credit.Step(w, w.Coef);
        Assert.Equal(baseMoney * (1.0 - w.Coef.CurrencyHoldingRatio.Value), w.Bank!.Gold.Value, 6);
    }

    [Fact]
    public void 退蔵率は信頼の低下で増えるが上限の細工は無い_既定の範囲の手計算と一致()
    {
        var w = Board(1.0, 0.25);   // 0.1×(1＋1×0.75)＝0.175
        double baseMoney = w.BaseMoney.Value;
        PriceOfBread.SimCore.Credit.Step(w, w.Coef);
        Assert.Equal(baseMoney * (1.0 - 0.175), w.Bank!.Gold.Value, 6);
    }
}
