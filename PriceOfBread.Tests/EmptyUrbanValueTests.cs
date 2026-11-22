using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1312) 線 c136ur：空の都市の階級の値打ち（urbd 段 3 の 3 回目）。
/// ①計器 U0（urxpd_s の末尾の列）：都市の POP が 0 の州×月・前の月に案の流れ＞Eps と読んだのに次の月も 0 の州×月を、盤面を試験の中で置いて手で数える。
/// 期待値は盤面の量（P_L・P_F・Capacity・失業・人数）から試験が自分で組む（計器もエンジンの関数も呼ばない）。
/// 計器の静的な旗（<see cref="Instruments.On"/>）を触るので `Instruments` 収集で直列に回す。
/// </summary>
[Collection("Instruments")]
public class EmptyUrbanValueTests
{
    /// <summary>英の開幕の州の対（最初の農村・最初の都市）。労働 2 財の生産者の Capacity・値段・失業を試験が置く。住居の項は係数 0 で外す。</summary>
    internal static (World w, Region r, Pop rural, Pop urban, Coefficients c) Board(double ruralK, double urbanK, double pl, double pf, double capPerHead, double uU = 0, double uR = 0)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var r = w.Region; var c = w.Coef;
        var (rural0, urban0) = PopDecision.MigrationPair(r);
        var rural = rural0!; var urban = urban0!;
        rural.Size = ruralK; urban.Size = urbanK;
        c.HousingNeedPerUrban = 0;
        rural.SubsistenceValuePerMonth = new MoneyPerMonth(0);
        rural.EarnedPerMonth = new MoneyPerMonth(0); urban.EarnedPerMonth = new MoneyPerMonth(0);
        rural.UnemploymentRate = new Share(uR); urban.UnemploymentRate = new Share(uU);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = 0.9 * rural.FoodNeed;
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = 0.9 * urban.FoodNeed;
        r.Markets[Catalog.LaborId].Price = pl; r.Markets[Catalog.FarmLaborId].Price = pf;
        int nl = 0;
        foreach (var s in r.Producers)
        {
            if (s.LaborPopIndex < 0 || s.LaborPopIndex >= r.Pops.Count) continue;
            var lp = r.Pops[s.LaborPopIndex];
            if (s.GoodId == Catalog.LaborId && ReferenceEquals(lp, urban)) { s.Capacity = capPerHead * urban.Size; nl++; }
            else if (s.GoodId == Catalog.FarmLaborId && ReferenceEquals(lp, rural)) { s.Capacity = capPerHead * rural.Size; nl++; }
            else if (s.GoodId == Catalog.LaborId || s.GoodId == Catalog.FarmLaborId) s.Capacity = 0;
            if (s.IsRgo) s.LaborWeight = 0;   // RGO の残余（案 A′ の項）を外す＝案 A と A′ が同じ値
        }
        Assert.True(nl >= 2, $"前提：対の労働 2 財の生産者が {nl} 本");
        r.CapitalEnabled = false;
        return (w, r, rural, urban, c);
    }

    static double G(double vr, double vu) => vr + vu > 1e-9 ? 2.0 * (vu - vr) / (vr + vu) : 0.0;

    [Fact]
    public void 計器U0_都市0の州を数え_次の月も0なら案の流れを読んだ月だけ数える()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c136ur_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            // 農村 1,000・都市 0・P_L 3・P_F 1・一人あたりの労働 0.5 → ℓ＝0.5×1000÷1000＝0.5・V_u＝1.5・V_r＝0.5 → g＞0
            var (w, r, rural, urban, c) = Board(1000, 0, pl: 3.0, pf: 1.0, capPerHead: 0.5);
            Assert.Equal(RegionTier.Core, r.Tier);
            Instruments.UrxPd(w, r, 1.0);
            var p = Instruments.UrxMktPeek(w);
            Assert.Equal(1, p.RegionMonths); Assert.Equal(1, p.EmptyHome); Assert.Equal(0, p.EmptyCol);
            Assert.Equal(1, p.EmptyGPos); Assert.Equal(1, p.EmptyGPosA2); Assert.Equal(0, p.Stuck);
            Assert.Equal(1000 * 1.5, p.WVu, 9); Assert.Equal(1000 * 0.5, p.WVr, 9); Assert.Equal(p.WVr, p.WVrA2, 12);
            // 次の月（Tick＋1 か月）も都市 0 のまま＝数える
            w.Tick += Calendar.DaysPerMonth;
            Instruments.UrxPd(w, r, 1.0);
            p = Instruments.UrxMktPeek(w);
            Assert.Equal(1, p.Stuck); Assert.Equal(1, p.StuckA2); Assert.Equal(2, p.EmptyHome);
            // 人が入った（都市 5）＝数えない
            urban.Size = 5; w.Tick += Calendar.DaysPerMonth;
            Instruments.UrxPd(w, r, 1.0);
            p = Instruments.UrxMktPeek(w);
            Assert.Equal(1, p.Stuck); Assert.Equal(2, p.EmptyHome);
            // 値打ちが農村の側（P_L＜P_F）なら前の月に流れを読まない＝次の月も 0 でも数えない
            urban.Size = 0; r.Markets[Catalog.LaborId].Price = 0.2; w.Tick += Calendar.DaysPerMonth;
            Instruments.UrxPd(w, r, 1.0);
            w.Tick += Calendar.DaysPerMonth;
            Instruments.UrxPd(w, r, 1.0);
            p = Instruments.UrxMktPeek(w);
            Assert.Equal(1, p.Stuck); Assert.Equal(4, p.EmptyHome); Assert.Equal(2, p.EmptyGPos);
            Assert.True(G(0.5, 0.1) < 0);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
