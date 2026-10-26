using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (423) 線 lt の番人（事前登録 measurements/lt_20261019_prereg.txt [8]）。旗 <c>AiLawGateFromBoard</c>＝工業化度を読む AI の法の門（金本位・反トラスト・同業組合）を盤面の実物へ置き換える。
/// 期待値は線の数値のリテラル（識字 0.75・都市 1/3 と 1/2）で書く（実装の定数から作らない＝同語反復にしない）。
/// </summary>
public class LawGateFromBoardTests
{
    private static MultiNationSim Four()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    private static IEnumerable<Pop> AllPops(World w) => w.Regions.SelectMany(r => r.Pops);

    private static void SetLiteracy(World w, double v) { foreach (var p in AllPops(w)) p.Literacy = v; }

    private static void SetIndustrialization(World w, double v)
    {
        foreach (var r in w.Regions) { r.Industrialization = v; r.OldVintageCapital = 0; r.StrandedVintage = 0; }
    }

    /// <summary>本国（植民地格を除く活きている州）の POP を全て同じ大きさにし、先頭から k 個を都市にする。返り値＝実際の都市の割合（k/N）。</summary>
    private static double SetHomeUrban(World w, double want)
    {
        var pops = w.Regions.Where(r => r.Tier != RegionTier.Colonial).SelectMany(r => r.Pops).ToList();
        int k = (int)Math.Round(want * pops.Count);
        for (int i = 0; i < pops.Count; i++) { pops[i].Size = 100; pops[i].IsUrban = i < k; }
        return (double)k / pops.Count;
    }

    // ================= L1 金の門 =================

    [Fact]
    public void 金の願いは旗1で本国の識字0_75を読む()
    {
        var w = Four().Nations[0].World;   // フランス＝銀で始まる
        w.Coef.AiLawGateFromBoard = 1;
        Assert.Equal(MonetaryStandardLaw.Silver, w.MonetaryLaw);
        SetLiteracy(w, 0.74); Assert.False(UtilityAgent.GoldWishFromBoard(w));
        SetLiteracy(w, 0.76); Assert.True(UtilityAgent.GoldWishFromBoard(w));
        // 配線：DecideQuarter が MonetaryTarget に写す（金 2・銀 1）
        SetLiteracy(w, 0.74); new UtilityAgent().DecideQuarter(w); Assert.Equal(1, w.Policy.MonetaryTarget);
        SetLiteracy(w, 0.76); new UtilityAgent().DecideQuarter(w); Assert.Equal(2, w.Policy.MonetaryTarget);
    }

    [Fact]
    public void 旗1の金の願いは工業化度を読まず_旗0は識字を読まない()
    {
        var w = Four().Nations[0].World;
        SetLiteracy(w, 0.3);
        SetIndustrialization(w, 5.0);   // 州の上限＝旧の閾 3.5 を大きく越える
        w.Coef.AiLawGateFromBoard = 1;
        new UtilityAgent().DecideQuarter(w); Assert.Equal(1, w.Policy.MonetaryTarget);   // 識字 0.3＜0.75＝工業化度が 5 でも望まない
        w.Coef.AiLawGateFromBoard = 0;
        new UtilityAgent().DecideQuarter(w); Assert.Equal(2, w.Policy.MonetaryTarget);   // 旧：工業化度 5＞3.5＝望む
        SetIndustrialization(w, 3.4);
        SetLiteracy(w, 0.9);
        new UtilityAgent().DecideQuarter(w); Assert.Equal(1, w.Policy.MonetaryTarget);   // 旧：識字が 0.9 でも工業化度 3.4＜3.5＝望まない
        SetIndustrialization(w, 3.6);
        new UtilityAgent().DecideQuarter(w); Assert.Equal(2, w.Policy.MonetaryTarget);
    }

    [Fact]
    public void 金の法の国は識字がなくても降りない()
    {
        var w = Four().Nations[0].World;
        w.Coef.AiLawGateFromBoard = 1;
        w.MonetaryLaw = MonetaryStandardLaw.Gold;
        SetLiteracy(w, 0.0);
        Assert.True(UtilityAgent.GoldWishFromBoard(w));
    }

    [Fact]
    public void 金の願いは本国の識字だけを数える_植民地の識字は数えない()
    {
        var w = Four().Nations[0].World;
        var home = w.Regions.First(r => r.Tier != RegionTier.Colonial);
        // 先頭の本国の州だけを本国に残し、他の州は植民地格にして識字 0.9 を置く
        foreach (var r in w.Regions) { r.Tier = r == home ? RegionTier.Core : RegionTier.Colonial; foreach (var p in r.Pops) p.Literacy = r == home ? 0.8 : 0.9; }
        Assert.True(UtilityAgent.GoldWishFromBoard(w));   // 本国 0.8＞0.75
        foreach (var p in home.Pops) p.Literacy = 0.2;
        Assert.False(UtilityAgent.GoldWishFromBoard(w));  // 植民地が 0.9 でも本国 0.2＜0.75（植民地を数えると 0.9 に近く真になる）
    }

    // ================= L2 反トラストの門 =================

    [Fact]
    public void 反トラストの願いは旗2で本国の都市の割合の3分の1と2分の1を読む()
    {
        var w = Four().Nations[0].World;
        w.Coef.AiLawGateFromBoard = 2;
        double u1 = SetHomeUrban(w, 0.25); Assert.InRange(u1, 0.2, 0.3);
        Assert.Null(UtilityAgent.AntitrustTargetFromBoard(w));
        double u2 = SetHomeUrban(w, 0.42); Assert.InRange(u2, 0.38, 0.46);
        Assert.Equal(AntitrustLaw.CartelBan, UtilityAgent.AntitrustTargetFromBoard(w));
        double u3 = SetHomeUrban(w, 0.7); Assert.InRange(u3, 0.65, 0.75);
        Assert.Equal(AntitrustLaw.Trustbust, UtilityAgent.AntitrustTargetFromBoard(w));
    }

    [Fact]
    public void 反トラストと同業組合の新しい目標は下げる向きに書かない()
    {
        // 帯（1/3〜1/2）：現行の法がすでに談合禁止以上なら書かない（独占の解体を談合禁止へ引き下げる目標を立てない）。1/2 超は常に上の段。
        var w = Four().Nations[0].World;
        w.Coef.AiLawGateFromBoard = 3; w.Coef.GuildLawGate = 1;
        SetHomeUrban(w, 0.42);
        w.Antitrust = AntitrustLaw.None;      Assert.Equal(AntitrustLaw.CartelBan, UtilityAgent.AntitrustTargetFromBoard(w));
        w.Antitrust = AntitrustLaw.CartelBan; Assert.Null(UtilityAgent.AntitrustTargetFromBoard(w));
        w.Antitrust = AntitrustLaw.Trustbust; Assert.Null(UtilityAgent.AntitrustTargetFromBoard(w));   // 引き下げの CartelBan を書かない
        w.Guild = GuildLaw.Full;    Assert.Equal(GuildLaw.Limited, UtilityAgent.GuildTargetFromBoard(w));
        w.Guild = GuildLaw.Limited; Assert.Null(UtilityAgent.GuildTargetFromBoard(w));
        w.Guild = GuildLaw.None;    Assert.Null(UtilityAgent.GuildTargetFromBoard(w));                  // 廃止済みの保護を戻す Limited を書かない
        SetHomeUrban(w, 0.7);
        w.Antitrust = AntitrustLaw.CartelBan; Assert.Equal(AntitrustLaw.Trustbust, UtilityAgent.AntitrustTargetFromBoard(w));   // 1/2 超は上の段へ
        w.Guild = GuildLaw.Limited;           Assert.Equal(GuildLaw.None, UtilityAgent.GuildTargetFromBoard(w));
        SetHomeUrban(w, 0.2);
        w.Antitrust = AntitrustLaw.Trustbust; Assert.Null(UtilityAgent.AntitrustTargetFromBoard(w));   // 1/3 を割っても書かない（現行を保つ）
    }

    [Fact]
    public void 反トラストの配線は旗2から_旗1では旧の工業化度のまま()
    {
        AntitrustLaw Target(double flag, double ind, double urban)
        {
            var w = Four().Nations[0].World;
            w.Coef.AiLawGateFromBoard = flag;
            SetIndustrialization(w, ind); SetHomeUrban(w, urban);
            var p = new NationPolicy();
            UtilityAgent.DecideModernLaws(w, p, UtilityAgent.Stance.Modernize);
            return p.Antitrust;
        }
        // 旧（旗 0・1）：工業化度 0.4 は書かない／0.6 は談合禁止／0.8 は独占の解体（都市の割合は読まない）
        Assert.Equal(AntitrustLaw.None, Target(1, 0.4, 0.7));
        Assert.Equal(AntitrustLaw.CartelBan, Target(1, 0.6, 0.0));
        Assert.Equal(AntitrustLaw.Trustbust, Target(0, 0.8, 0.0));
        // 新（旗 2）：都市の割合を読む（工業化度は読まない）
        Assert.Equal(AntitrustLaw.None, Target(2, 5.0, 0.2));
        Assert.Equal(AntitrustLaw.CartelBan, Target(2, 0.0, 0.42));
        Assert.Equal(AntitrustLaw.Trustbust, Target(2, 0.0, 0.7));
    }

    [Fact]
    public void 都市の割合は植民地格の州を数えない()
    {
        var w = Four().Nations[0].World;
        var home = w.Regions.First(r => r.Tier != RegionTier.Colonial);
        foreach (var r in w.Regions) { r.Tier = r == home ? RegionTier.Core : RegionTier.Colonial; foreach (var p in r.Pops) { p.Size = 100; p.IsUrban = r != home; } }
        Assert.Equal(0.0, UtilityAgent.HomeUrbanShare(w));   // 本国は全て農村・植民地は全て都市＝植民地を数えれば 1 に近い
        foreach (var p in home.Pops) p.IsUrban = true;
        Assert.Equal(1.0, UtilityAgent.HomeUrbanShare(w));
    }

    // ================= L3 同業組合の門 =================

    [Fact]
    public void 同業組合の願いは旗3かつゲート正のときだけ_線は反トラストと同じ()
    {
        GuildLaw Target(double flag, double gate, double ind, double urban)
        {
            var w = Four().Nations[0].World;
            w.Coef.AiLawGateFromBoard = flag; w.Coef.GuildLawGate = gate;
            SetIndustrialization(w, ind); SetHomeUrban(w, urban);
            var p = new NationPolicy { Guild = GuildLaw.Full };
            UtilityAgent.DecideModernLaws(w, p, UtilityAgent.Stance.Modernize);
            return p.Guild;
        }
        Assert.Equal(GuildLaw.Full, Target(3, 0, 0.0, 0.7));     // ゲート 0＝書かない
        Assert.Equal(GuildLaw.Full, Target(3, 1, 0.0, 0.2));     // 都市 0.2＜1/3＝書かない
        Assert.Equal(GuildLaw.Limited, Target(3, 1, 0.0, 0.42)); // 1/3〜1/2＝制限
        Assert.Equal(GuildLaw.None, Target(3, 1, 0.0, 0.7));     // 1/2 超＝廃止
        Assert.Equal(GuildLaw.Limited, Target(2, 1, 0.6, 0.0));  // 旗 2 までは旧（工業化度 0.6＝制限）
        Assert.Equal(GuildLaw.Full, Target(2, 1, 0.4, 0.7));     // 旧：工業化度 0.4 は書かない
    }

    // ================= 対称・旧の式の局所の恒等 =================

    [Fact]
    public void 願いの新しい式は世界前線と他国の特例を読まない()
    {
        var sim = Four();
        var w = sim.Nations[0].World;
        SetLiteracy(w, 0.5); SetHomeUrban(w, 0.42); SetIndustrialization(w, 0.1);
        w.Coef.AiLawGateFromBoard = 3;
        bool g0 = UtilityAgent.GoldWishFromBoard(w); var a0 = UtilityAgent.AntitrustTargetFromBoard(w); var h0 = UtilityAgent.GuildTargetFromBoard(w);
        w.IndustrialFrontier = 0.0;
        Assert.Equal(g0, UtilityAgent.GoldWishFromBoard(w)); Assert.Equal(a0, UtilityAgent.AntitrustTargetFromBoard(w)); Assert.Equal(h0, UtilityAgent.GuildTargetFromBoard(w));
        w.IndustrialFrontier = 50.0;
        Assert.Equal(g0, UtilityAgent.GoldWishFromBoard(w)); Assert.Equal(a0, UtilityAgent.AntitrustTargetFromBoard(w)); Assert.Equal(h0, UtilityAgent.GuildTargetFromBoard(w));
        // 他国の世界も同じ式（国別の特例なし）：同じ盤面の値を日本に置いても同じ答え
        var j = sim.Nations[1].World;
        SetLiteracy(j, 0.5); SetHomeUrban(j, 0.42);
        Assert.Equal(g0, UtilityAgent.GoldWishFromBoard(j)); Assert.Equal(a0, UtilityAgent.AntitrustTargetFromBoard(j));
    }

    [Fact]
    public void 旧の式を畳んだ関数は元の式と同じ値を返す()
    {
        // 旗 0 の恒等の局所の証明：*Old は元の式（金の閾 3.5・反トラスト 0.5／0.75）と同じ。工業化度を動かして手書きの期待値と突き合わせる。
        var w = Four().Nations[0].World;
        w.Coef.GuildLawGate = 1;
        foreach (var (ind, gold, anti, guild) in new (double, bool, AntitrustLaw?, GuildLaw?)[]
        {
            (0.4, false, null, null), (0.6, false, AntitrustLaw.CartelBan, GuildLaw.Limited), (0.8, false, AntitrustLaw.Trustbust, GuildLaw.None),
            (3.4, false, AntitrustLaw.Trustbust, GuildLaw.None), (3.6, true, AntitrustLaw.Trustbust, GuildLaw.None),
        })
        {
            SetIndustrialization(w, ind);
            Assert.Equal(gold, UtilityAgent.GoldWishOld(w));
            Assert.Equal(anti, UtilityAgent.AntitrustTargetOld(w));
            Assert.Equal(guild, UtilityAgent.GuildTargetOld(w));
        }
        w.Coef.GuildLawGate = 0;
        SetIndustrialization(w, 0.8);
        Assert.Null(UtilityAgent.GuildTargetOld(w));   // ゲート 0＝書かない
    }
}
