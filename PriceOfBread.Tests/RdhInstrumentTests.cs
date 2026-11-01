using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(721) 線 rdh：名簿の外の空洞化の原因の表（<c>rdh_s</c>・読むだけ）の番人。
/// ①手の算術：農村と都市の対の〈移る値打ち〉V・g・流れを、盤面に置いた所得・自給・家賃から手で書いた期待値と突き合わせる（式は PopDecision の外で書き直す）。V都市が 0 で切れる縁も。
/// ②hook：州ごとの出生と死の和が既存の台帳（<c>World.DiagBirths／DiagDeaths</c>＝別の出所）に合い、飢えの死が州に載る（飢えの局地の盤面）。
/// ③RDH が空なら何も積まない（他の表を変えない前提）。
/// </summary>
[Collection("Instruments")]
public class RdhInstrumentTests
{
    private static Scenario Famine(ulong s) => new() { Seed = s, Kind = ScenarioKind.RegionalFamine, Coef = new Coefficients() };

    private static (World w, Region r, Pop rural, Pop urban) Board(double incRural, double subRural, double incUrban, double housing)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());   // 住居の市場がある盤面（飢饉の盤面には無い）
        var w = sim.World; var r = w.Region;
        var (rural0, urban0) = PopDecision.MigrationPair(r);
        var rural = rural0!; var urban = urban0!;
        foreach (var p in new[] { rural, urban }) { p.FoodNeed = 0.8 * p.Size; p.FoodMet = 0.9 * p.FoodNeed; }
        rural.EarnedPerMonth = new MoneyPerMonth(incRural * rural.Size);
        rural.SubsistenceValuePerMonth = new MoneyPerMonth(subRural * rural.Size);
        urban.EarnedPerMonth = new MoneyPerMonth(incUrban * urban.Size);
        var c = w.Coef;
        c.HousingNeedPerUrban = 0.03; c.HousingUrbanThreshold = 0.0;   // 家賃が必ず立つ盤面（州の都市の割合＞0）
        Assert.True(Catalog.HousingId < r.Markets.Length, "前提：盤面に住居の市場が無い");
        r.Markets[Catalog.HousingId].Price = housing / 0.03;
        r.CapitalEnabled = false;
        c.UrbanFlowReal = 1;   // 流れ＝Mobility×g×送り手の規模（案B）
        c.SerfMobilityGate = 0;   // 農奴の門を外す（流れの手の算術を門に依らせない）
        return (w, r, rural, urban);
    }

    [Fact]
    public void 手の算術でVとgと流れが出る()
    {
        bool saved = Instruments.RdhOn;
        Instruments.RdhOn = true;
        try
        {
            // 農村：現金 0.5＋自給 0.3＝0.8／都市：現金 1.0−家賃 0.25＝0.75 → g＝2(0.75−0.8)÷1.55
            var (w, r, rural, urban) = Board(0.5, 0.3, 1.0, 0.25);
            double su = urban.Size;
            var a = Instruments.RdhPeek(r); a.Clear();
            Instruments.RdhPd(w, r, 1.0);
            Instruments.RdhPd(w, r, 1.0);
            double g = 2.0 * (0.75 - 0.8) / 1.55;
            Assert.Equal(2, a.Months);
            Assert.Equal(2, a.GNeg); Assert.Equal(0, a.GPos); Assert.Equal(0, a.VuClip);
            Assert.Equal(2 * g, a.GSum, 12);
            Assert.Equal(2 * 0.8, a.VrSum, 12); Assert.Equal(2 * 0.75, a.VuSum, 12);
            Assert.Equal(2 * 0.3, a.SubSum, 12); Assert.Equal(2 * 0.25, a.HousSum, 12);
            Assert.Equal(2 * 0.5, a.EarnRSum, 12); Assert.Equal(2 * 1.0, a.EarnUSum, 12);
            Assert.Equal(2 * (-0.02 * g * su), a.FlowToRural, 9);   // 都市→農村（g＜0 は都市の規模に掛ける）
            Assert.Equal(0, a.FlowToUrban);
            Assert.Equal(su, urban.Size);   // 計器は人を動かさない

            // 縁：家賃が現金を超える（現金 0.2−家賃 0.25＜0）＝V都市は 0 で切れる → g＝2(0−0.8)÷0.8＝−2
            var (w2, r2, _, _) = Board(0.5, 0.3, 0.2, 0.25);
            var a2 = Instruments.RdhPeek(r2); a2.Clear();
            Instruments.RdhPd(w2, r2, 1.0);
            Assert.Equal(1, a2.VuClip);
            Assert.Equal(-2.0, a2.GSum, 12);
            Assert.Equal(0.0, a2.VuSum, 12);

            // 農村→都市の向き（都市の V が上）＝g＞0・流れは農村の規模に掛ける
            var (w3, r3, rural3, _) = Board(0.2, 0.1, 1.0, 0.1);
            var a3 = Instruments.RdhPeek(r3); a3.Clear();
            Instruments.RdhPd(w3, r3, 1.0);
            double g3 = 2.0 * (0.9 - 0.3) / 1.2;
            Assert.Equal(1, a3.GPos);
            Assert.Equal(g3, a3.GSum, 12);
            Assert.Equal(0.02 * g3 * rural3.Size, a3.FlowToUrban, 9);
        }
        finally { Instruments.RdhOn = saved; }
    }

    [Fact]
    public void RDHが空なら積まない()
    {
        bool saved = Instruments.RdhOn;
        Instruments.RdhOn = false;
        try
        {
            var (w, r, _, _) = Board(0.5, 0.3, 1.0, 0.25);
            var a = Instruments.RdhPeek(r); a.Clear();
            Instruments.RdhPd(w, r, 1.0);
            Assert.Equal(0, a.Months);
            Assert.Equal(0, a.NoPair);
        }
        finally { Instruments.RdhOn = saved; }
    }

    [Fact]
    public void 州ごとの出生と死の和が既存の台帳に合い_飢えの死が州に載る()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rdh_" + Guid.NewGuid().ToString("N"));
        bool saved = Instruments.RdhOn;
        Instruments.Configure(dir, "*");
        Instruments.RdhOn = true;
        try
        {
            var sim = new Simulation(Famine(1), new NullAgent());
            foreach (var r in sim.World.Regions) Instruments.RdhPeek(r).Clear();
            sim.Run(120);   // 30 年（食料不足の州が飢える）
            double b = 0, dn = 0, df = 0; int months = 0, famineRegions = 0;
            foreach (var r in sim.World.Regions)
            {
                var a = Instruments.RdhPeek(r);
                b += a.Births; dn += a.DeathsNormal; df += a.DeathsFamine; months += a.Months + a.NoPair;
                if (a.DeathsFamine > 0) famineRegions++;
            }
            Assert.True(df > 0, "飢えの死が 0＝飢えの hook が州に来ていない（対照の盤面でない）");
            Assert.True(famineRegions >= 1);
            Assert.True(b > 0 && dn > 0);
            Assert.Equal(sim.World.DiagBirths, b, 6);          // 出生＝既存の台帳（別の出所）
            Assert.Equal(sim.World.DiagDeaths, dn + df, 6);    // この盤面に疫病は無い＝死の台帳に閉じる
            Assert.Equal(360 * sim.World.Regions.Count, months);   // PopDecision の駆動は毎月・全ての州
        }
        finally { Instruments.RdhOn = saved; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
