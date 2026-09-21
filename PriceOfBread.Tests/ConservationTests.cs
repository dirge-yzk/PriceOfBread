using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★保存量の番人（2026-09-03・キューD-36／出典 トリアージ 2-D 10-1e）。
///
/// ⚠**きっかけ**：保存量プローブ（`Simulation.ConservationProbe`＝leakhunt）は**Runner のコマンドでしか有効にならない**＝
/// フルスイートでは一度も走っていなかった。⚠貨幣の蒸発は「破綻」ではなく**静かに数字が痩せる**ので、
/// テストが緑でも起きる（実例＝汚職 Distribute の無言リターンで seed25/27 が落ちた 2026-07）。
///
/// ★D-36 の元の案は「leakhunt/savetest をバッチのゲートへ」だったが、savetest 側は既に A-06 で
/// **2段構え**（短距離を xUnit に常設・長距離300Qは受け入れで手動）に決着している。⇒ 保存量も同じ形に揃える。
///
/// ⚠**静的フラグ（ConservationProbe）は使わない**：xUnit はテストクラスを並列に走らせるので、
/// 静的フラグを立てると**他のテストの世界まで数え込んで偽陽性になる**。
/// ★代わりに**このテストが自分で不変量を計算する**＝並列安全・新しい状態を1つも増やさない。
/// ⚠プローブの細かい粒度（サブステップ単位で現場を特定する）は**診断の道具**であって検出には要らない
/// ＝四半期境界で D が動けばどこかで漏れている。現場の特定は leakhunt を手で走らせる。</summary>
public class ConservationTests
{
    /// <summary>保存量 D ＝ 基礎貨幣 − 累積台帳（貿易/鋳造/不換発行）。
    /// ★内部でいくら金が動いても、台帳に記帳されている限り D は動かない。動いたら**記帳漏れ**である。</summary>
    private static double D(World w)
        => w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;

    [Fact]
    public void 多国の走行で保存量Dが動かない()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StartWarmup();

        int n0 = sim.Nations.Count;
        var baseline = new double[n0];
        var money0 = new double[n0];
        for (int i = 0; i < n0; i++) { baseline[i] = D(sim.Nations[i].World); money0[i] = sim.Nations[i].World.BaseMoney; }
        int moved = 0;   // ★下の生存確認用（この番人が眠っていないことを同じ走行で確かめる）

        const int Q = 12;
        for (int q = 0; q < Q; q++)
        {
            sim.StepQuarter();
            // ⚠国は増減しうる（建国・再統合）ので**開幕から居る国だけ**を見る＝新しい国の初期Dを違反と誤判定しない。
            //   参照同一性でなく添字で見るのは、Nations の並びが開幕の n0 件について安定だから（分離独立は末尾に足される）。
            int upto = System.Math.Min(n0, sim.Nations.Count);
            for (int i = 0; i < upto; i++)
            {
                var w = sim.Nations[i].World;
                double d = D(w);
                // ⚠割譲・併合・分離は**国をまたいで**貨幣を動かすので、当事国の D は正当に動く。
                //   ここは短距離（12四半期）＝領土移動がまだ起きない帯で見る＝偽陽性を作らずに記帳漏れだけを捕まえる。
                Assert.True(System.Math.Abs(d - baseline[i]) <= 1e-4,
                    $"{w.SelfPower?.Name}：保存量Dが動いた（{baseline[i]:F6} → {d:F6}／Δ={d - baseline[i]:F6}）"
                    + "＝どこかで台帳に記帳せずに貨幣を作った/消した。現場の特定は `leakhunt <seed> <四半期>` を手で走らせること。");
                baseline[i] = d;   // 次の四半期は今回値と比べる（累積でなく差分で見る＝1回の跳びを最初の四半期で捕まえる）
                if (System.Math.Abs(w.BaseMoney - money0[i]) > 1e-4) moved++;
            }
        }
        // ★J-011（2026-09-03）：**輸出利潤経路が動いていること**もここで見る＝旧 `ExportRevenueTests` が
        //   単一国の世界市場（`Trade.cs`）で見ていた検査を、**実ゲームが通る二国間経路**へ移した。
        //   ⚠`ExportRevenueToProducers`（既定0.5）は `BilateralMarket` が読む生きた機構で、
        //   輸出代金の一部が生産者POPへ直接渡る＝**貨幣が国庫を経由せず動く**経路。上のD不変が壊れやすい場所そのもの。
        double exported = 0;
        foreach (var n in sim.Nations)
            if (n.World.ExportsByGood is { } eg) foreach (var v in eg) if (v > 0) exported += v;
        Assert.True(exported > 0, "この走行では輸出が1件も起きていない＝輸出利潤経路が検証できていない（テストの前提が崩れている）");

        // ★生存確認（⚠恒真な番人を作らないための一行）：上の検査は「Dが動かない」なので、
        //   **貨幣がそもそも1円も動かない世界**でも緑になる。基礎貨幣が実際に動いていることを同じ走行で確かめる
        //   ＝Dが不変なのは「何も起きていないから」ではなく「台帳が閉じているから」だ、と言えるようにする。
        Assert.True(moved > 0,
            "この走行では基礎貨幣が一度も動いていない＝保存の検査が**何も検査していない**。"
            + "経済が止まっているか、テストの前提（助走後に12四半期回す）が壊れている。");
    }
}
