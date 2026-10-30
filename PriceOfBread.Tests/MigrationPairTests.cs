using System;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(668) 線 pdc 案 U の番人：農村↔都市の対の選び（<see cref="PopDecision.MigrationPair"/>）を 1 本にした統一は挙動を変えない。
/// 期待値は本体の関数から作らず、番人の側の独立のループ（階級の番号 14＝犯罪者のリテラル・旧の職の移しの規則〈閉鎖だけを飛ばす〉）で作る。
/// 突然変異：犯罪者 POP を Pops の先頭に置いた州で、3 か所（人口の移動・職の移しの天井・計器）が犯罪者を対に選ばないこと
/// （どれか 1 か所の犯罪者の飛ばしを外す、または対の選びを 3 か所の手書きへ戻して 1 か所だけ飛ばさないと赤）。</summary>
public class MigrationPairTests
{
    private static (Pop? Rural, Pop? Urban) Indep(Region r, bool skipCriminal)
    {
        Pop? rural = null, urban = null;
        foreach (var p in r.Pops)
        {
            if (p.Closed) continue;
            if (skipCriminal && p.ClassId == 14) continue;
            if (p.IsUrban) { if (urban is null) urban = p; }
            else { if (rural is null) rural = p; }
        }
        return (rural, urban);
    }

    private static double Headroom(Region r, Coefficients c)
        => (double)typeof(Meritocracy).GetMethod("UrbanHeadroom", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [r, c])!;

    [Fact]
    public void 全州で対は独立のループと同じPOP参照_旧の職の移しの規則とも同じ()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, null, new Coefficients { CriminalClassEnabled = 1 });
        int regions = 0, withCriminal = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                regions++;
                if (r.Pops.Any(p => p.ClassId == 14)) withCriminal++;
                var got = PopDecision.MigrationPair(r);
                var want = Indep(r, true);
                var old = Indep(r, false);   // 旧の Meritocracy.UrbanHeadroom の規則（閉鎖だけを飛ばす）
                Assert.Same(want.Rural, got.Rural);
                Assert.Same(want.Urban, got.Urban);
                Assert.Same(old.Rural, got.Rural);   // 犯罪者は末尾に足されるので旧の規則と同じ参照（恒等の根拠）
                Assert.Same(old.Urban, got.Urban);
            }
        Assert.True(regions >= 61, $"州が少ない（{regions}）");
        Assert.True(withCriminal > 0, "犯罪者の器が並ばない世界では番人にならない");
    }

    [Fact]
    public void 犯罪者を先頭に置いた州でも3か所とも犯罪者を対に選ばない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        r.CapitalEnabled = true; c.UrbanCapOff = 0; r.Industrialization = 1.0; r.UrbanIndSmoothed = -1.0;
        var crim = new Pop { ClassId = 14, ClassName = "犯罪者", IsUrban = false, Size = 0, Satisfaction = 1.0 };
        r.Pops.Insert(0, crim);
        var (rural, urban) = Indep(r, true);
        Assert.NotNull(rural); Assert.NotNull(urban);
        Assert.NotSame(crim, rural);
        var got = PopDecision.MigrationPair(r);
        Assert.Same(rural, got.Rural); Assert.Same(urban, got.Urban);

        // 計器：対の規模は独立のループの農村と都市の規模の和
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var d));
        Assert.Equal(rural!.Size + urban!.Size, d.Total, 9);
        Assert.Equal(urban.Size, d.UrbanSize, 9);

        // 職の移しの天井：犯罪者（規模 0）が農村に化けると total が都市だけになり残枠が変わる
        double total = rural.Size + urban.Size;
        double indForCap = c.UrbanCapFollowSpeed < 1.0 && r.UrbanIndSmoothed >= 0 ? r.UrbanIndSmoothed : r.TotalIndustrialization;
        double maxShare = Math.Clamp(c.BaseUrbanCap + indForCap * c.UrbanPerInd, 0, 0.85);
        Assert.Equal(Math.Max(0, maxShare * total - urban.Size), Headroom(r, c), 9);

        // 人口の移動：犯罪者の規模は動かない（0 のまま）
        PopDecision.Run(r, c);
        Assert.Equal(0.0, crim.Size);
    }
}
