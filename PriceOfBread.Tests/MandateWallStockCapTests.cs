using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(615) 線 pcw：政治資本の壁の一歩の勘定の番人（事前登録 measurements/pcw615_20261028_prereg.txt [6]・旗 <see cref="Coefficients.MandateWallStockCap"/>）。
/// 見るのは機構と不変条件だけ（成績表の主張は書かない）。期待値は手で置いた b・S・代価のリテラルで書く（<c>PoliticalCapitalMax</c> から作らない）。ON 経路は <c>w.Coef.X=</c> で書く（World.Coef は国ごと別インスタンス）。
/// 書き手は <c>Government.Spend</c> の 1 本（法の <c>MandateGate</c> もスライダーの <c>Move</c> もここを通る）＝private なので反射で呼ぶ。
/// </summary>
public class MandateWallStockCapTests
{
    private static readonly MethodInfo SpendMethod = typeof(Government).GetMethod("Spend", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo MoveMethod = typeof(Government).GetMethod("Move", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static World Board(double cap)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.World.Coef.MandateOverStockLegitPower = 1;
        sim.World.Coef.MandateWallStockCap = cap;
        sim.Run(8);
        return sim.World;
    }

    private static void Spend(World w, double amount) => SpendMethod.Invoke(null, [w, amount]);

    [Fact]
    public void 既定は1_係数の既定は在庫で止める勘定()
    {
        Assert.Equal(1.0, new Coefficients().MandateWallStockCap);
    }

    [Fact]
    public void 旗0_壁の一歩を払うとSは代価の全額でbを超える_今のまま()
    {
        var w = Board(0);
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Spend(w, 0.9);
        Assert.Equal(0.9, w.MandateSpent);
        Assert.Equal(0.9, w.MandateSpentNew);
    }

    [Fact]
    public void 旗1_壁の一歩を払うとSはbで止まり_正統性の請求は代価の全額のまま()
    {
        var w = Board(1);
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Spend(w, 0.9);
        Assert.Equal(0.3, w.MandateSpent);          // 在庫を使い切る（b＝0.3 を手で置いた）
        Assert.Equal(0.9, w.MandateSpentNew);       // b を超える 0.6 は S に載らず、正統性だけが払う（請求は全額）
    }

    /// <summary>正統性の払いは旗に依らない（同じ盤面で旗 0 と旗 1 が Politics.Step の後に同じ正統性）。違うのは S の戻りだけ。</summary>
    [Fact]
    public void 旗1でも正統性は代価の全額を払い_Sの戻りだけが違う()
    {
        double legit0, legit1, s0, s1;
        {
            var w = Board(0); w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0; w.Legitimacy = 0.95;
            Spend(w, 0.9); Politics.Step(w, w.Coef); legit0 = w.Legitimacy; s0 = w.MandateSpent;
        }
        {
            var w = Board(1); w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0; w.Legitimacy = 0.95;
            Spend(w, 0.9); Politics.Step(w, w.Coef); legit1 = w.Legitimacy; s1 = w.MandateSpent;
        }
        Assert.Equal(legit0, legit1);               // 正統性の払いは同じ（請求は全額・二重を直したのは S の側だけ）
        Assert.True(legit1 < 0.95, "正統性が代価の分だけ減っていない＝請求が落ちた");
        Assert.True(s1 < s0, $"旗 1 の S（{s1}）が旗 0 の S（{s0}）より小さくない");
        Assert.True(s1 < 0.3 && s0 > 0.3, "回復の後も旗 0 は b を超え、旗 1 は b の下にあるはず");
    }

    /// <summary>普通の一歩（S＋代価≦b）は旗 0 と旗 1 で 1 ビットも同じ。旗に入る枝は壁の一歩だけ。</summary>
    [Fact]
    public void 旗1でも普通の一歩は旗0と1ビットも同じ()
    {
        foreach (var (s, amount) in new[] { (0.0, 0.1), (0.1, 0.15), (0.125, 0.125), (0.0, 0.3) })
        {
            var w0 = Board(0); w0.PoliticalCapitalMax = 0.3; w0.MandateSpent = s; w0.MandateSpentNew = 0;
            var w1 = Board(1); w1.PoliticalCapitalMax = 0.3; w1.MandateSpent = s; w1.MandateSpentNew = 0;
            Spend(w0, amount); Spend(w1, amount);
            Assert.Equal(s + amount, w0.MandateSpent);
            Assert.Equal(w0.MandateSpent, w1.MandateSpent);
            Assert.Equal(w0.MandateSpentNew, w1.MandateSpentNew);
        }
    }

    /// <summary>限界：b が 0 の国は S が 0 のまま（代価は正統性だけが払う）・b が数でなければ S を動かさない（NaN で S を壊さない）。</summary>
    [Fact]
    public void 旗1_bが0_またはbがNaNならSは増えない()
    {
        var w = Board(1);
        w.PoliticalCapitalMax = 0; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Spend(w, 0.5);
        Assert.Equal(0.0, w.MandateSpent);
        Assert.Equal(0.5, w.MandateSpentNew);
        w.PoliticalCapitalMax = double.NaN; w.MandateSpent = 0.2; w.MandateSpentNew = 0;
        Spend(w, 0.5);
        Assert.Equal(0.2, w.MandateSpent);
        Assert.False(double.IsNaN(w.MandateSpent));
    }

    /// <summary>法の経路（Government.Step の段の法）：旗 1 では壁の一歩の後の S が代価でなく b。MandateSpentNew は代価の全額（Politics.Step が 1 回だけ引く）。</summary>
    [Fact]
    public void 旗1_法の壁の一歩_Sは小さなbで止まり請求は代価の全額()
    {
        var w = Board(1); var c = w.Coef;
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

    /// <summary>スライダーの経路（Move）：同じ Spend を通る＝旗 1 で S が b で止まる。純支持が負になる向きの税・関税を探す（探せなければ前提が崩れた）。</summary>
    [Fact]
    public void 旗1_スライダーの壁の一歩もSはbで止まる()
    {
        foreach (var cap in new[] { 0.0, 1.0 })
        {
            var w = Board(cap); var c = w.Coef;
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
                    if (cap > 0) Assert.True(w.MandateSpent <= w.PoliticalCapitalMax * (1 + 1e-12), $"旗 1 で S（{w.MandateSpent}）が b（{w.PoliticalCapitalMax}）を超えた");
                    else Assert.Equal(w.MandateSpentNew, w.MandateSpent, 12);
                }
            Assert.True(found, "テスト前提が崩れた：壁の一歩になるスライダーの押しが無い（軸の候補を見直す）");
        }
    }

    /// <summary>壁の一歩の後の回復：旗 1 は S が b から 0 へ 40 四半期（実行度 1）。旗 0 は S÷b 倍かかる。</summary>
    [Fact]
    public void 旗1_壁の一歩の後の在庫は40四半期で戻り_旗0は代価に比例して長い()
    {
        int Quarters(double cap)
        {
            var w = Board(cap); var c = w.Coef;
            w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0;
            Spend(w, 0.9);
            int n = 0;
            while (w.MandateSpent > 0 && n < 100000) { w.ParliamentControl = 0; w.PoliticalCapitalMax = 0.3; Politics.Step(w, c); n++; }
            return n;
        }
        int n1 = Quarters(1), n0 = Quarters(0);
        Assert.InRange(n1, 39, 41);                       // b＝0.3 を b/40 ずつ（実行度 1）
        Assert.InRange(n0, 119, 121);                     // 0.9÷(0.3/40)＝120
    }
}
