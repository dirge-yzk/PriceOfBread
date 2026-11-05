using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(854) 線 c70id：<c>Discovery.IdeaBoardOf</c> の自給の割合（工場労働者の依存度＝人数×(1−自給の割合)）は、読み込みの直後でも保存の前と同じ値を読む。
/// <c>Pop.SubsistenceShare</c> は [NonSerialized] で EconomyStep.Run の頭が毎月書く＝Run の外か読み込みの直後は 0 になる。以前の読み方（欄 SubsistenceShare を直に読む）は、読み込みの直後の月だけ
/// 自給の基を持つ POP の依存度を 1 に見ていた。今の読み方＝InstrumentsCearn・InstrumentsSubst と同じ三項式（Productivity でなければ欄 SubsistenceBasis・Productivity だけ SubsistenceShare）。
/// 今のデータの工場労働者は自給の基が 0（MkPop の既定）なので通常の走りでは値は変わらない。試験は工場労働者に基を置いて形を確かめる。突然変異の表は measurements/m854_20261105/mutation_854.txt。</summary>
public class IdeaBoardLoadTests
{
    private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

    private static MultiNationSim Two() => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1861UL) { DiplomacyFrozen = true };

    /// <summary>自給の基 <paramref name="basis"/> を国 0 の工場労働者に置いて 1 四半期回した世界（旗 SubsistenceStage2＝0 を明示）。</summary>
    private static MultiNationSim Run1Q(double basis)
    {
        var sim = Two();
        foreach (var n in sim.Nations) n.World.Coef.SubsistenceStage2 = 0;    // 前提を既定に頼らず明示（旗 0＝食べる割合は欄そのもの）
        foreach (var r in sim.Nations[0].World.Regions) if (r.Tier != RegionTier.Colonial) foreach (var p in r.Pops) if (p.ClassId == PopClass.Worker && p.Size > 0) p.SubsistenceBasis = basis;   // 植民地の POP は月ごとに作り直される州がある＝本国の州だけ
        sim.StepQuarter();                                                      // EconomyStep.Run の頭が欄 SubsistenceShare を書く
        return sim;
    }

    private static List<Pop> Home(World w) => w.Regions.Where(r => r.Tier != RegionTier.Colonial).SelectMany(r => r.Pops).Where(p => p.ClassId == PopClass.Worker && p.Size > 0).ToList();

    [Fact]
    public void 読み込みの直後でも自給の基を持つ工場労働者の依存度は保存の前と同じ()
    {
        var sim = Run1Q(0.3);
        var w = sim.Nations[0].World;
        var workers = Home(w);
        Assert.NotEmpty(workers);
        Assert.All(workers, p => Assert.Equal(0.3, p.SubsistenceShare));
        var b0 = Discovery.IdeaBoardOf(w, 30.0);
        var none = Discovery.IdeaBoardOf(Run1Q(0.0).Nations[0].World, 30.0);    // 自給の基が 0 の盤面の依存度（比べる側）
        Assert.True(b0.WageShare < none.WageShare, "前提が崩れた：自給の基が依存度に効いていない");
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var w2 = sim2.Nations[0].World;
        var workers2 = Home(w2);
        Assert.NotEmpty(workers2);
        Assert.All(workers2, p => Assert.Equal(0.3, p.SubsistenceBasis));       // 基は保存される
        Assert.All(workers2, p => Assert.Equal(0.0, p.SubsistenceShare));       // 食べる割合の欄は [NonSerialized]＝読み込みの直後は 0（この月の Run がまだ書いていない）
        var b1 = Discovery.IdeaBoardOf(w2, 30.0);
        Assert.Equal(Bits(b0.WageShare), Bits(b1.WageShare));
        Assert.Equal(Bits(b0.WageUnmet), Bits(b1.WageUnmet));
    }

    [Fact]
    public void 焼いた自給の生産性の担い手は欄の食べる割合を読む()
    {
        // Productivity の POP は欄 SubsistenceBasis が割合でなく生産性 ρ＝割合は SubsistenceShare から読む（三項式の Productivity の側）
        var sim = Two();
        var w = sim.Nations[0].World;
        var workers = w.AllPops().Where(p => p.ClassId == PopClass.Worker && p.Size > 0).ToList();
        Assert.NotEmpty(workers);
        foreach (var p in workers) { p.SubsistenceBasisKind = SubsistenceBasisKind.Productivity; p.SubsistenceBasis = 7.0; p.SubsistenceShare = 0.25; }   // ρ＝7（割合として読めば Clamp01 で 1）・割合の欄＝0.25
        var withRho = Discovery.IdeaBoardOf(w, 30.0);
        foreach (var p in workers) { p.SubsistenceBasisKind = SubsistenceBasisKind.OpeningShare; p.SubsistenceBasis = 0.25; p.SubsistenceShare = 0.0; }
        var asShare = Discovery.IdeaBoardOf(w, 30.0);
        Assert.Equal(Bits(asShare.WageShare), Bits(withRho.WageShare));         // どちらも割合 0.25
    }
}
