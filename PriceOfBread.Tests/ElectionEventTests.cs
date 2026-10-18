using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>E2/E3（docs/202608/20260814_選挙を動詞にする_部分完了.md §3-0〜3-3・§7の梯子）の番人。
/// ON経路＝公示の中身（支持上位3思想＋看板議題）と
/// 加担の決着（押し込みが1回で消費される）を固定する。点火（E4＝AI即決）は単独の測定ラウンドで。</summary>
public class ElectionEventTests
{
    [Fact]
    public void ONでは公示が立ち加担は開票1回で消費される()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 5);
        var w = sim.Nations[0].World;
        w.SelfPower!.IsPlayer = true;
        var c = w.Coef;   // ⚠sim.Coef ではない（Fiscal/議会は各国 World.Coef を読む＝CorruptionCoreV2Tests の教訓）
        // ★計器の隔離（2026-09-22）：飢餓の駆動（既定で点火＝階層3分割と対）を外す。英1国だけの世界は食料を輸入できず（主食充足≈0.75）、
        //   飢餓の駆動で1786年に内戦へ入り選挙が止まる＝この番人の前提（選挙のある英が4年続く）が世界の偶然になる。測るのは選挙の公示の機構だけ。
        c.FamineDriveGain = 0;
        sim.StartWarmup();
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
