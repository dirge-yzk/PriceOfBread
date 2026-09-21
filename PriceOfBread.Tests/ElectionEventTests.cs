using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>E2/E3（docs/202608/20260814_選挙を動詞にする_部分完了.md §3-0〜3-3・§7の梯子）の番人。
/// (1)既定0の休眠ピン (2)OFF＝公示が一切生成されない (3)ON経路＝公示の中身（支持上位3思想＋看板議題）と
/// 加担の決着（押し込みが1回で消費される）を固定する。点火（E4＝AI即決）は単独の測定ラウンドで。</summary>
public class ElectionEventTests
{
    [Fact]
    public void 係数の既定は休眠()
    {
        Assert.Equal(1.0, new Coefficients().ElectionEventEnabled);   // ★2026-08-17に点火（A/B exact38.2→40.2・硬性緑）。0に戻せば公示もポップアップも押し込みも1命令も走らない
    }

    [Fact]
    public void OFFでは公示が生成されない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 5);
        var w = sim.Nations[0].World;
        w.SelfPower!.IsPlayer = true;
        sim.StartWarmup();
        for (int q = 0; q < (int)w.Coef.ElectionIntervalQuarters + 2; q++) sim.StepQuarter();   // 定期選挙を1回跨ぐ
        Assert.Null(w.PendingElection);
        Assert.True(w.ElectionsHeld > 0, "選挙が1回も起きていない＝テスト前提の破れ（政体が非選挙へ転げた可能性）");
    }

    [Fact]
    public void ONでは公示が立ち加担は開票1回で消費される()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 5);
        var w = sim.Nations[0].World;
        w.SelfPower!.IsPlayer = true;
        var c = w.Coef;   // ⚠sim.Coef ではない（Fiscal/議会は各国 World.Coef を読む＝CorruptionCoreV2Tests の教訓）
        sim.StartWarmup();
        c.ElectionEventEnabled = 1.0;
        ElectionEvent? seen = null;
        for (int q = 0; q < (int)c.ElectionIntervalQuarters + 2 && seen is null; q++)
        {
            sim.StepQuarter();
            if (w.PendingElection is { } ev)
            {
                seen = ev;
                Assert.InRange(ev.AgendaIdeologies.Length, 1, 3);            // 支持のある思想だけが議題を掲げる
                Assert.Equal(ev.AgendaIdeologies.Length, ev.AgendaLabels.Length);
                foreach (var l in ev.AgendaLabels) Assert.False(string.IsNullOrEmpty(l));
                // 加担：議題の先頭の思想へ**裁量余剰の50%**を注ぐ（★E3''確定形＝第2引数はレバー＝金の割合。
                // 余白ゲートで拒まれても択は消える＝どちらでも公示はクリアされる）
                w.ResolveElection(ev.AgendaIdeologies[0], 0.5);
                Assert.Null(w.PendingElection);
            }
        }
        Assert.NotNull(seen);   // 公示が一度も立たない＝配線切れ
        // 開票（次の総選挙）を跨ぐと押し込みは必ず消費されている
        for (int q = 0; q < (int)c.ElectionIntervalQuarters + 2; q++) sim.StepQuarter();
        Assert.Equal(-1, w.ElectionPushIdeology);
        Assert.Equal(0.0, w.ElectionPushStrength, 12);
    }
}
