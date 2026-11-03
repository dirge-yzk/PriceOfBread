using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(615) 線 pcw：政治資本の壁の一歩の勘定の番人（事前登録 measurements/pcw615_20261028_prereg.txt [6]・(803) で旗 MandateWallStockCap を焼き込んだ）。
/// 見るのは機構と不変条件だけ（成績表の主張は書かない）。期待値は手で置いた b・S・代価のリテラルで書く（<c>PoliticalCapitalMax</c> から作らない）。ON 経路は <c>w.Coef.X=</c> で書く（World.Coef は国ごと別インスタンス）。
/// 書き手は <c>Government.Spend</c> の 1 本（法の <c>MandateGate</c> もスライダーの <c>Move</c> もここを通る）＝private なので反射で呼ぶ。
/// </summary>
public class MandateWallStockCapTests
{
    private static readonly MethodInfo SpendMethod = typeof(Government).GetMethod("Spend", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo MoveMethod = typeof(Government).GetMethod("Move", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static World Board()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.World.Coef.MandateOverStockLegitPower = 1;
        sim.Run(8);
        return sim.World;
    }

    private static void Spend(World w, double amount) => SpendMethod.Invoke(null, [w, amount]);

    [Fact]
    public void 壁の一歩を払うとSはbで止まり_正統性の請求は代価の全額のまま()
    {
        var w = Board();
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Spend(w, 0.9);
        Assert.Equal(0.3, w.MandateSpent);          // 在庫を使い切る（b＝0.3 を手で置いた）
        Assert.Equal(0.9, w.MandateSpentNew);       // b を超える 0.6 は S に載らず、正統性だけが払う（請求は全額）
    }

    /// <summary>壁の一歩の後、正統性は代価の全額を払い（Politics.Step で減る）、S は b の下に戻る。</summary>
    [Fact]
    public void 正統性は代価の全額を払い_Sはbを超えずに戻る()
    {
        var w = Board(); w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0; w.Legitimacy = 0.95;
        Spend(w, 0.9); Politics.Step(w, w.Coef);
        Assert.True(w.Legitimacy < 0.95, "正統性が代価の分だけ減っていない＝請求が落ちた");
        Assert.True(w.MandateSpent < 0.3, "回復の後も S が b の下にあるはず");
    }

    /// <summary>普通の一歩（S＋代価≦b）は代価の全額を S へ足す。止める枝に入るのは壁の一歩だけ。</summary>
    [Fact]
    public void 普通の一歩は代価の全額をSへ足す()
    {
        foreach (var (s, amount) in new[] { (0.0, 0.1), (0.1, 0.15), (0.125, 0.125), (0.0, 0.3) })
        {
            var w = Board(); w.PoliticalCapitalMax = 0.3; w.MandateSpent = s; w.MandateSpentNew = 0;
            Spend(w, amount);
            Assert.Equal(s + amount, w.MandateSpent);
            Assert.Equal(amount, w.MandateSpentNew);
        }
    }

    /// <summary>限界：b が 0 の国は S が 0 のまま（代価は正統性だけが払う）・b が数でなければ S を動かさない（NaN で S を壊さない）。</summary>
    [Fact]
    public void bが0_またはbがNaNならSは増えない()
    {
        var w = Board();
        w.PoliticalCapitalMax = 0; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Spend(w, 0.5);
        Assert.Equal(0.0, w.MandateSpent);
        Assert.Equal(0.5, w.MandateSpentNew);
        w.PoliticalCapitalMax = double.NaN; w.MandateSpent = 0.2; w.MandateSpentNew = 0;
        Spend(w, 0.5);
        Assert.Equal(0.2, w.MandateSpent);
        Assert.False(double.IsNaN(w.MandateSpent));
    }

    /// <summary>法の経路（Government.Step の段の法）：壁の一歩の後の S が代価でなく b。MandateSpentNew は代価の全額（Politics.Step が 1 回だけ引く）。</summary>
    [Fact]
    public void 法の壁の一歩_Sは小さなbで止まり請求は代価の全額()
    {
        var w = Board(); var c = w.Coef;
        var (axis, inc, level, cost) = PolicyPoliticsTests.FindOpposedStep(w);
        c.MandateWeight = 0.001; w.Legitimacy = 0.95; w.MandateSpentNew = 0; w.MandateSpent = 0;
        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);
        Assert.True(cost > 0.01, $"テスト前提が崩れた：代価 {cost} が小さい");
        PolicyPoliticsTests.SetLawTarget(w, axis, level);
        int before = PolicyPoliticsTests.LawLevel(w, axis);
        Government.Step(w, c);
        Assert.NotEqual(before, PolicyPoliticsTests.LawLevel(w, axis));   // 壁の一歩が通った（非空虚）
        Assert.InRange(w.MandateSpent, 0.0, 0.001);                        // b＝0.001×0.5×(正統性＋支持) ≦ 0.00100 の桁（代価 cost は 0.01 を超える）
        Assert.Equal(cost, w.MandateSpentNew, 9);                          // 正統性の請求は代価の全額
        Assert.True(w.MandateSpent < cost * 0.1);
    }

    /// <summary>スライダーの経路（Move）：同じ Spend を通る＝S が b で止まる。純支持が負になる向きの税・関税を探す（探せなければ前提が崩れた）。</summary>
    [Fact]
    public void スライダーの壁の一歩もSはbで止まる()
    {
        var w = Board(); var c = w.Coef;
        c.MandateWeight = 0.001; w.Legitimacy = 0.95;
        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);
        bool found = false;
        foreach (var axis in new[] { PolicyAxis.Tariff, PolicyAxis.IncomeTax, PolicyAxis.LandTax, PolicyAxis.PollTax, PolicyAxis.Coercion })
            foreach (bool inc in new[] { true, false })
            {
                if (found) break;
                double net = PolicyPreferences.NetSupport(w, axis, inc);
                if (!(net < -0.05)) continue;
                w.MandateSpent = 0; w.MandateSpentNew = 0;
                double cur = 0.5, target = inc ? 1.0 : 0.0;
                MoveMethod.Invoke(null, [cur, target, c, w, axis, 1.0, true, false]);
                if (!(w.MandateSpentNew > w.PoliticalCapitalMax * 2)) continue;   // 壁の一歩になっていない盤面は次を探す
                found = true;
                Assert.True(w.MandateSpent <= w.PoliticalCapitalMax * (1 + 1e-12), $"S（{w.MandateSpent}）が b（{w.PoliticalCapitalMax}）を超えた");
            }
        Assert.True(found, "テスト前提が崩れた：壁の一歩になるスライダーの押しが無い（軸の候補を見直す）");
    }

    /// <summary>壁の一歩の後の回復：S が b から 0 へ 40 四半期（実行度 1）。</summary>
    [Fact]
    public void 壁の一歩の後の在庫は40四半期で戻る()
    {
        var w = Board(); var c = w.Coef;
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Spend(w, 0.9);
        int n = 0;
        while (w.MandateSpent > 0 && n < 100000) { w.ParliamentControl = 0; w.PoliticalCapitalMax = 0.3; Politics.Step(w, c); n++; }
        Assert.InRange(n, 39, 41);                        // b＝0.3 を b/40 ずつ（実行度 1）
    }
}
