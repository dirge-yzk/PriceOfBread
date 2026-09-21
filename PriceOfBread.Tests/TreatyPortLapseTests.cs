using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>条約港の自然失効（案A・作者裁定2026-08-16）＝「不平等条約は力の差が続く間だけ有効」。
/// 動詞・戦争例外なし：国力（軍事+生産）が最強列強比の閾値を超えた国の条約港が、一定年ごとに1港ずつ風化する。</summary>
public class TreatyPortLapseTests
{
    [Fact]
    public void 力が並んだ国の条約港は風化し既定では何も起きない()
    {
        var cfgs = WorldData.Nations();
        var sim = new MultiNationSim(cfgs, 1789UL, _ => new NullAgent());
        var diplo = sim.Diplomacy;
        var britain = diplo.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス"));
        Assert.NotNull(britain.Nation);
        britain.Nation!.TreatyPortsGranted = 3;   // 英に条約港3港を直接付与（英＝最強級＝閾値0.5を確実に超える）

        // OFF側（Ratio=0を明示ピン＝2026-08-16の点火後は既定0.5なので対照は明示する。点火したらコメント一巡の流儀）
        var c0 = britain.Nation.Coef;
        c0.TreatyPortLapseRatio = 0;
        diplo.TreatyPortLapse(c0, 48);
        Assert.Equal(3, britain.Nation.TreatyPortsGranted);

        // 点火（Ratio=0.5・4年刻み）：検査月（48ヶ月=4年）に1港失効・非検査月は不変
        c0.TreatyPortLapseRatio = 0.5;
        diplo.ComputeRanks(c0);   // 列強ランクを確定（最強列強の物差し）
        diplo.TreatyPortLapse(c0, 47);
        Assert.Equal(3, britain.Nation.TreatyPortsGranted);   // 非検査月＝不変
        diplo.TreatyPortLapse(c0, 48);
        Assert.Equal(2, britain.Nation.TreatyPortsGranted);   // 検査月＝1港風化
        diplo.TreatyPortLapse(c0, 96);
        Assert.Equal(1, britain.Nation.TreatyPortsGranted);   // 次の刻みでさらに1港
    }

    [Fact]
    public void 弱い国の条約港は風化しない()
    {
        var cfgs = WorldData.Nations();
        var sim = new MultiNationSim(cfgs, 1789UL, _ => new NullAgent());
        var diplo = sim.Diplomacy;
        // 最弱級の国に条約港を付与（国力比が閾値0.5に届かない代表として弱小ランク国を選ぶ）
        var c = new Coefficients { TreatyPortLapseRatio = 0.5 };
        diplo.ComputeRanks(c);
        var weak = diplo.Powers.First(p => p.Nation is { } && diplo.RankOf(p) == PowerRank.Minor);
        weak.Nation!.TreatyPortsGranted = 2;
        diplo.TreatyPortLapse(c, 48);
        Assert.Equal(2, weak.Nation.TreatyPortsGranted);   // 力が無ければ条約は風化しない
    }
}
