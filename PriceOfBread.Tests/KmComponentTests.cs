using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1097) 線 c105rr の最初の恒等のコミットの番人：清算の距離の表を〈対ごとの成分〉で持ち越す形（InternationalMarket.PairKmParts と ComposeKm）が、
/// 口 PairKmCore と全ての組・両向きでビット一致（値と陸の印）すること。期待値は PairKmCore（(1071) の口＝RulePowerTests.P1 が旧の逐語の式とビット一致を確かめた物）から作る。
/// 盤面＝4 国・3 四半期の清算の州の表の複写（Instruments.RpSnapOf・(1071) の計器）。費用（terrCost・landRatio）だけを変えた盤面でも一致すること
/// （成分は費用に依らない＝費用だけが動いた州の対を組み直す形の前提）を、同じ成分と別の費用で確かめる。
/// 突然変異（使い捨ての木・measurements/m1097_20261113/mut/）：C1 組み直しの結合の順を (海＋E_b)＋E_a へ → この試験で赤。
/// </summary>
[Collection("Instruments")]
public class KmComponentTests
{
    private const ulong Seed = 1789UL;

    [Fact]
    public void 成分からの組み直しは口と全ての組でビット一致_費用だけを変えても一致()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_km_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*"); Instruments.SelectTables("rulepower_s");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); Instruments.SelectTables(null); Instruments.Configure(null, null); }
        try
        {
            var s = Instruments.RpSnapOf(sim.Diplomacy);
            Assert.NotNull(s);
            Assert.True(s!.Tc > 50, "複写の州の数 " + s.Tc);
            // 費用の 2 通り：清算の値そのもの・州ごとに別の倍（鉄道の混ぜで費用だけが動いた盤面の代わり＝成分は同じ）
            var terr2 = new double[s.Tc]; var ratio2 = new double[s.Tc];
            for (int t = 0; t < s.Tc; t++) { terr2[t] = s.TerrCost[t] * (0.37 + 0.011 * (t % 53)); ratio2[t] = s.LandRatio[t] * (t % 3 == 0 ? 0.25 : 1.0); }
            long pairs = 0, landPairs = 0, seaPairs = 0, noRoute = 0;
            // 海に出られない州（内陸で港に繋がらない）を混ぜた写し＝海の成分の無い組の枝も通す（この盤面は全ての州が海に出られる）
            var trNs = (TrEntryCopy(s.Tr, s.Tc));
            foreach (var (terr, ratio, tr) in new[] { (s.TerrCost, s.LandRatio, s.Tr), (terr2, ratio2, s.Tr), (terr2, ratio2, trNs) })
            {
                var e = new double[s.Tc]; var k = new double[s.Tc];
                for (int t = 0; t < s.Tc; t++) { e[t] = tr[t].landKm * terr[t] * ratio[t]; k[t] = terr[t] * ratio[t]; }
                for (int a = 0; a < s.Tc; a++)
                    for (int b = 0; b < s.Tc; b++)
                    {
                        double want = InternationalMarket.PairKmCore(tr, s.SeaNodeOf, s.SeaHalf, s.SuezOkN, s.PanOkN, terr, ratio, s.LandNode, s.LandGraft, s.LandRoute, a, b, out bool wl);
                        byte st = InternationalMarket.PairKmParts(tr, s.SeaNodeOf, s.SeaHalf, s.SuezOkN, s.PanOkN, s.LandNode, s.LandGraft, s.LandRoute, a, b, out double sea, out double lh);
                        double got = InternationalMarket.ComposeKm(st, sea, lh, e[a], e[b], k[a], k[b], out bool gl);
                        Assert.Equal(BitConverter.DoubleToInt64Bits(want), BitConverter.DoubleToInt64Bits(got));
                        Assert.Equal(wl, gl);
                        Assert.True((st & InternationalMarket.KmStDone) != 0, "状態の印に Done が無い（0 は〈まだ引いていない〉と読まれる）");
                        pairs++; if (wl) landPairs++;
                        if ((st & InternationalMarket.KmStSea) != 0) seaPairs++;
                        if (double.IsPositiveInfinity(want)) noRoute++;
                    }
            }
            Assert.True(pairs > 9000, "調べた組の数 " + pairs);   // 対照の下限：空の母集合で緑にしない
            Assert.True(landPairs > 0, "陸直行が勝つ組が 1 つも無い＝陸の成分を見ていない");
            Assert.True(seaPairs > 0 && seaPairs < pairs, $"海の成分を持つ組 {seaPairs}／{pairs}＝海と海でない組の両方を見ていない");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static TrEntry[] TrEntryCopy(TrEntry[] src, int n)
    {
        var a = new TrEntry[n];
        for (int t = 0; t < n; t++) { var e = src[t]; if (t % 7 == 0) e.seaOk = false; a[t] = e; }
        return a;
    }
}
