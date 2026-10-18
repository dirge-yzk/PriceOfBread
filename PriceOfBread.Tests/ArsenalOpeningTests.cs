using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 軍需の開幕の在庫（実装ログ (239)）の番人。首都に置く軍需最終財の備蓄は
/// 「人口の側の定員（MaxSoldierFraction×Σ RegionManpower）×MilitaryUpkeepScale×レート×InitialArsenalMonths」で、
/// 市場の在庫 200（CatalogEconomy）は床のまま（Max）。国名を読まず、人口（母数）に比例する。
/// <para>⚠ON 経路のテスト（構築した世界の首都の在庫を式と突き合わせる）。</para>
/// <para>⚠(241) 限界（反証役 va1#7）：1本目は本体の式の写し（式どおりかだけを見る）・2本目は旧の底（構築時の兵＝定員の 0.30 倍）でも緑＝底の選び方そのものは守らない。
/// 開幕（助走の直後）の在庫が史実の桁かは帯と probe が見る（実装ログ (241)）。</para>
/// </summary>
public class ArsenalOpeningTests
{
    private static readonly int[] ArsenalGoods = [Catalog.RifleId, Catalog.AmmunitionId, Catalog.ArtilleryId];

    private static double Expected(World w, Coefficients c, int good)
    {
        double man = 0;
        // ★(408) 備蓄の底は構築の中で読む＝その時点の Region.AdminFill は既定の 1.0（構築の後に 0 へ倒す）。(408) の行政費の返り〔旗は (419) で焼き込み〕でも底は動かない（ソース読みの確定・事前登録 追記2 ②）＝同じ時点（充足 1）の母数で突き合わせる
        foreach (var r in w.Regions) { double keep = r.AdminFill; r.AdminFill = 1.0; man += Military.RegionManpower(r, c); r.AdminFill = keep; }
        int b = System.Array.IndexOf(c.MilitaryGoods, good);
        double monthly = c.MilitaryUpkeepScale * c.MaxSoldierFraction.Value * man * c.MilitaryRates[b];
        return System.Math.Max(200.0, monthly * c.InitialArsenalMonths);
    }

    [Fact]
    public void 回239_首都の軍需の備蓄は人口の側の定員と月数の式どおりで200は床()
    {
        var c = new Coefficients();
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent(), c);
        int above = 0, floor = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            // 植民地の州は構築のあとで足される（備蓄の式が見た州の集合は本国の州だけ）＝植民地を持たない国だけ等式で突き合わせる
            if (w.Regions.Any(r => r.Tier == RegionTier.Colonial)) continue;
            foreach (int g in ArsenalGoods)
            {
                double want = Expected(w, c, g);
                Assert.Equal(want, w.Regions[0].Markets[g].Stock, 6);
                if (want > 200.0 + 1e-9) above++; else floor++;
            }
        }
        Assert.True(above > 20, $"式が床を超える国×財が少なすぎる（{above}）＝式の経路が試されていない");
        Assert.True(floor > 20, $"床のままの国×財が少なすぎる（{floor}）＝床の経路が試されていない");
    }

    [Fact]
    public void 回239_備蓄は人口に比例し国名を読まない()
    {
        var c = new Coefficients();
        var big = WorldData.Nations().OrderByDescending(x => x.Peasant + x.Worker).First();
        var renamed = big.ShallowClone(); renamed.Name = "別の名前の同じ国";
        var doubled = big.ShallowClone(); doubled.Peasant *= 2; doubled.Worker *= 2;
        double Stock(NationConfig cfg)
        {
            var sim = new MultiNationSim([cfg], 1789UL, _ => new UtilityAgent(), c);
            return sim.Nations[0].World.Regions[0].Markets[Catalog.RifleId].Stock;
        }
        double s0 = Stock(big), s1 = Stock(renamed), s2 = Stock(doubled);
        Assert.True(s0 > 400.0, $"試験の国の備蓄が床に近すぎる（{s0}）");
        Assert.Equal(s0, s1, 6);   // 国名で変わらない
        Assert.True(s2 > s0 * 1.5, $"人口を2倍にしても備蓄が増えない（{s0}→{s2}）");
    }
}
