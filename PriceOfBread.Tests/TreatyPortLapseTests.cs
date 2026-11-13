using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>条約港の自然失効（案A・作者裁定2026-08-16）＝「不平等条約は力の差が続く間だけ有効」。
/// 動詞・戦争例外なし：国力（軍事+生産）が最強列強比に近づくほど、その国の港州の外国持分が速く風化する（連続形・四半期の月だけ）。
/// ★(399) 条約港は個数の機構でなく州の外国区分（講和で積まれる持分）で表す（個数が 4 年ごとの一斉検査で 1 減る旧の器は物理削除）。</summary>
public class TreatyPortLapseTests
{
    [Fact]
    public void 力が並んだ国の外国持分は風化し係数0では何も起きない()
    {
        var cfgs = WorldData.Nations();
        var sim = new MultiNationSim(cfgs, 1789UL, _ => new NullAgent());
        // ★(449) 物差し（世界の総量に対する割合の国力・比の基準は列強の中央値）で測る。最強の列強は比が 1 以上＝風化の係数が張り付く（旧の腕の英と同じ位置）。旧の旗の盤面に固定しない。
        var diplo = sim.Diplomacy;
        var c0 = sim.Nations[0].World.Coef;
        c0.RuleFromPower = 0;   // (1117) 旗 0 の経路の番人（★(832) ① の (a)・段 C′ の旗 1 では TreatyPortLapse は何もしない）
        diplo.ComputeRanks(c0);
        var sc = NationalScore.ScoresByPower(diplo);
        var strongest = diplo.Powers.Where(p => p.Nation is { } && diplo.RankOf(p) == PowerRank.Great && p.Nation!.Regions.Any(r => r.IsPort)).OrderByDescending(p => sc[p.Id]).First();
        var port = strongest.Nation!.Regions.First(r => r.IsPort);
        Politics.AddForeignShare(port, "テスト国", 0.5);   // 最強級の列強の港州に持分（国力比 ≧ 1）

        // OFF側（Ratio=0を明示ピン）
        c0.TreatyPortLapseRatio = new Share(0);
        diplo.TreatyPortLapse(c0, 48);
        Assert.Equal(0.5, port.ForeignShares[0], 12);

        // 点火（Ratio=0.5）：四半期の月に DominanceSpeed(0.025) の 1 回分・四半期でない月は不変
        c0.TreatyPortLapseRatio = new Share(0.5);
        diplo.ComputeRanks(c0);   // 列強ランクを確定（最強列強の物差し）
        diplo.TreatyPortLapse(c0, 47);
        Assert.Equal(0.5, port.ForeignShares[0], 12);   // 非四半期の月＝不変
        diplo.TreatyPortLapse(c0, 48);
        Assert.Equal(0.5 * (1.0 - 0.025), port.ForeignShares[0], 9);   // 四半期の月＝1 回分の風化
        diplo.TreatyPortLapse(c0, 51);
        Assert.Equal(0.5 * (1.0 - 0.025) * (1.0 - 0.025), port.ForeignShares[0], 9);   // 次の四半期でさらに 1 回分
    }

    [Fact]
    public void 弱い国の外国持分の風化は強い国より遅い()
    {
        var cfgs = WorldData.Nations();
        var sim = new MultiNationSim(cfgs, 1789UL, _ => new NullAgent());
        var diplo = sim.Diplomacy;
        // 最弱級の国に持分を付与（国力比が閾値0.5に届かない代表として弱小ランク国を選ぶ）
        var c = new Coefficients { TreatyPortLapseRatio = new Share(0.5), RuleFromPower = 0 };
        diplo.ComputeRanks(c);
        var weak = diplo.Powers.First(p => p.Nation is { } && diplo.RankOf(p) == PowerRank.Minor);
        var port = weak.Nation!.Regions.First();
        Politics.AddForeignShare(port, "テスト国", 0.5);
        diplo.TreatyPortLapse(c, 48);
        Assert.InRange(port.ForeignShares[0], 0.5 * (1.0 - 0.025) + 1e-9, 0.5);   // 力が無ければ風化は 1 回分（0.025）より遅い（力に比例して遅くなる）
    }
}
