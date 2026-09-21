using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 国庫の台帳の番人（2026-08-13・作者指示「国庫に触るものは全て財政タブに入れる」）。
///
/// 症状＝ヘッダーの収支（国庫の実績変化）と財政タブの収支（経常予算の予想）が一致しなかった。
/// 行を1本ずつ足す方式では厳密一致が原理的に達成できない（カデンスが違う／記帳漏れが検出できない）ので、
/// **国庫を動かせるのは <see cref="World.Pay"/> だけ**にして（setter は private）台帳を通した。
///
/// ここで固定するのは1つだけ：**Σ台帳 ＝ 国庫 − 初期国庫**。
/// 新しい書き手を足したときに <c>Pay</c> を通し忘れれば、そもそもコンパイルが通らない。
/// それでも通り抜ける経路が2つある＝①<see cref="World.SetTreasuryRaw"/> の誤用 ②**World クラス自身の内部からの書き込み**
/// （private setter はクラス内からは見える）。②は実際に2件すり抜け（工場建設の日次労務費・国営工場の払い下げ代金）、
/// このテストが日本で 872,258 対 800,866 の差として捕まえた。型システムと番人の**両方**が要る理由。
/// </summary>
public class TreasuryLedgerTests
{
    private static double LedgerSum(World w)
    {
        double s = 0;
        foreach (var v in w.TreasuryLedger) s += v;
        return s;
    }

    /// <summary>単一国（イギリス・240四半期）で台帳が国庫と一致する。</summary>
    [Fact]
    public void 単一国で台帳の合計は国庫の変化に一致する()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(19), new NullAgent());
        double start = sim.World.Treasury;
        Assert.Equal(0.0, LedgerSum(sim.World));   // 生成直後＝1件も動いていない（初期国庫はフローでない）
        sim.Run(240);
        double moved = sim.World.Treasury - start;
        // 相対許容＝浮動小数の累積誤差のみ（240四半期ぶんの加算の桁落ち）。1e-6 は取引額に対して十分に厳しい。
        double scale = System.Math.Max(1.0, System.Math.Abs(moved));
        Assert.True(System.Math.Abs(LedgerSum(sim.World) - moved) / scale < 1e-6,
            $"台帳 {LedgerSum(sim.World):F3} と国庫の変化 {moved:F3} が一致しない＝Pay を通さない書き手がある");
    }

    /// <summary>多国（全53カ国・120四半期）でも全国で一致する。外交層（戦費・拿捕・貢納・賠償）は
    /// Power 側の財布を触るので、同期窓の内側で動いた分が落ちないことをここで担保する。</summary>
    [Fact]
    public void 多国でも全ての国で台帳の合計は国庫の変化に一致する()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        var start = new System.Collections.Generic.Dictionary<string, double>();
        foreach (var n in sim.Nations) start[n.World.SelfPower!.Name] = n.World.Treasury;
        for (int q = 0; q < 120; q++) sim.StepQuarter();

        int checkedCount = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } sp || !start.TryGetValue(sp.Name, out double s0)) continue;   // 途中で建国した国は基準が無い
            double moved = w.Treasury - s0;
            double scale = System.Math.Max(1.0, System.Math.Abs(moved));
            Assert.True(System.Math.Abs(LedgerSum(w) - moved) / scale < 1e-6,
                $"{sp.Name}：台帳 {LedgerSum(w):F3} と国庫の変化 {moved:F3} が一致しない");
            checkedCount++;
        }
        Assert.True(checkedCount > 40, $"検査した国が少なすぎる（{checkedCount}）＝ハーネスが空回りしている");
    }

    /// <summary>台帳が**実際に使われている**（全部ゼロのまま通っていない）。
    /// 何もしない番人は必ず緑になるので、主要な区分に値が立つことを確かめる。</summary>
    [Fact]
    public void 主要な区分に値が立つ()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 40; q++) sim.StepQuarter();
        var w = sim.Nations[0].World;
        foreach (var cat in new[] { TreasuryCat.Tax, TreasuryCat.Admin, TreasuryCat.OtherOut })
            Assert.True(System.Math.Abs(w.TreasuryLedger[(int)cat]) > 1e-9,
                $"{cat} の台帳が1度も動いていない＝記帳の配線が切れている");
    }
}
