using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(706) 線 urb3：都市化の目標の式（設計 20261031_都市化の目標の式_設計.md 案B）の段1 計器と段2 の器の番人。
/// ①自給の値の欄（<see cref="Pop.SubsistenceValuePerMonth"/>）＝1 か月の主食の充足の自給の量×その州のその主食の市場価格（手の算術・5 財の必要量は goods.json の literal）
/// ②<see cref="PopDecision.MigrationValues"/>＝農村は現金＋自給・都市は現金−住居・0 で切る・g の手の算術（盤面の量から作る・式を呼ばない）
/// ③旗 <c>UrbanFlowReal</c> の流れ：旗 0 は新しい入力（自給の値・住居の価格）を読まない（ビット一致）／旗 1 は手の算術どおり・対称・食料の値段で細る・送り手の最大 4%
/// ④計器 urxpd_s の新しい列（wSubV・wHousU・wGsym・flowFlipRegionMonths）。
/// 計器の静的な旗（<see cref="Instruments.On"/>）を触る試験があるので `Instruments` 収集で直列に回す。
/// </summary>
[Collection("Instruments")]
public class UrbanFlowRealTests
{
    // ---------------------------------------------------------------- ① 自給の値の欄 ----------------------------------------------------------------

    static readonly int[] Staples = [Catalog.BreadId, Catalog.RiceId, Catalog.VegetableId, Catalog.PotatoId, Catalog.MaizeId];
    static readonly double[] Need = [0.55, 0.20, 0.25, 0.06, 0.04];   // goods.json の主食 5 財の基礎の必要量（literal・FoodLedgerTests と同じ）

    /// <summary>英国の開幕の州に 1 か月だけ消費を回す（収穫係数 1・在庫は十分・値段＝ratio×参照価格）。</summary>
    static (World W, Region R) Month(double[] ratio, int months = 1, double harvest = 1.0)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
        var r = w.Region;
        r.ArableLand = 1e9; r.HarvestFactor = harvest; r.WeatherBandFactor = 1.0; r.CropHealth = null;
        foreach (var p in r.Pops) p.SetSavings(1e12);
        for (int i = 0; i < Staples.Length; i++)
        {
            int g = Staples[i];
            r.Markets[g].Price = ratio[i] * Catalog.RefPrices[g];
            r.Markets[g].Stock = 1e9;
        }
        for (int m = 0; m < months; m++) EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        return (w, r);
    }

    [Fact]
    public void 自給の値は自給の量と主食の市場価格の積の和_手の算術()
    {
        double[] ratio = [2.0, 0.5, 1.0, 3.0, 1.5];
        var (_, r) = Month(ratio);
        int farmers = 0;
        foreach (var p in r.Pops)
        {
            // 手の算術：自給で満たす量＝必要量(財ごと)×規模×自給率（収穫係数 1）・値段＝ratio×参照価格。★(713) urb4：食べられる量＝必要量で切る（自給率≦1・収穫係数 1 の盤面では切りは効かない＝値は (706) と同じ）
            double expect = 0;
            for (int i = 0; i < Staples.Length; i++) expect += Math.Min(Need[i] * p.Size * p.Subsistence, Need[i] * p.Size) * ratio[i] * Catalog.RefPrices[Staples[i]];
            Assert.Equal(expect, p.SubsistenceValuePerMonth.Value, 9);
            if (p.Subsistence > 0) { farmers++; Assert.True(p.SubsistenceValuePerMonth.Value > 0, "前提：自給のある POP の欄が 0"); }
            else Assert.Equal(0.0, p.SubsistenceValuePerMonth.Value);
        }
        Assert.True(farmers >= 1, "前提：自給率の正の POP が無い");
    }

    [Fact]
    public void 自給の値は必要量で切る_豊作で自給の量が必要量を超えても食べられる分だけ()
    {
        // ★(713) urb4：収穫係数 4（豊作）＝自給で満たす量 必要量×自給率×4 が必要量を超える POP がある。値打ちは食べられる量（必要量）までで切る（met と同じ上限）。
        double[] ratio = [2.0, 0.5, 1.0, 3.0, 1.5];
        const double harvest = 4.0;
        var (_, r) = Month(ratio, 1, harvest);
        int capped = 0;
        foreach (var p in r.Pops)
        {
            double expect = 0, uncapped = 0;
            for (int i = 0; i < Staples.Length; i++)
            {
                double px = ratio[i] * Catalog.RefPrices[Staples[i]];
                double raw = Need[i] * p.Size * p.Subsistence * harvest;   // 手の算術（goods.json の必要量の literal）
                double need = Need[i] * p.Size;
                expect += (raw < need ? raw : need) * px;
                uncapped += raw * px;
            }
            Assert.Equal(expect, p.SubsistenceValuePerMonth.Value, 9);
            if (expect < uncapped - 1e-9 * (1 + uncapped)) capped++;
        }
        Assert.True(capped >= 1, "前提：自給の量が必要量を超える POP が無い（収穫係数 4 で自給率 0.25 超の POP が要る）");
    }

    [Fact]
    public void 自給の値は月の頭に0へ戻る_2か月回しても1か月ぶん()
    {
        double[] ratio = [1.0, 1.0, 1.0, 1.0, 1.0];
        var (_, r1) = Month(ratio, 1);
        var (_, r2) = Month(ratio, 2);
        int n = 0;
        for (int i = 0; i < r1.Pops.Count; i++)
        {
            if (!(r1.Pops[i].Subsistence > 0)) continue;
            // 2 か月目も同じ盤面の値段＝1 か月ぶんと同じ値（積み上がっていない）
            Assert.Equal(r1.Pops[i].SubsistenceValuePerMonth.Value, r2.Pops[i].SubsistenceValuePerMonth.Value, 9);
            n++;
        }
        Assert.True(n >= 1);
    }

    // ---------------------------------------------------------------- ② 移る値打ち V ----------------------------------------------------------------

    /// <summary>対（最初の農村・最初の都市）の盤面。income は 1 人・月。housingOn＝州の都市の割合を住居の門（<c>HousingUrbanThreshold</c>）の上／下に置く。</summary>
    static (World w, Region r, Pop rural, Pop urban, Coefficients c) Board(double earnedR, double subR, double earnedU, double housingPrice, bool housingOn)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());   // 住居の市場がある盤面（飢饉の盤面には無い）
        var w = sim.World; var r = w.Region; var c = w.Coef;
        var (rural0, urban0) = PopDecision.MigrationPair(r);
        var rural = rural0!; var urban = urban0!;
        // 州の都市の割合を 0.6 に（対の都市 POP の規模で調整・他の POP は動かさない）。門の下は、門の閾値を 0.9 へ上げて作る（英の開幕は他の都市 POP が多く、規模だけでは 0.25 の下へ行けない）
        double tOther = 0, uOther = 0;
        foreach (var p in r.Pops) { if (ReferenceEquals(p, urban)) continue; tOther += p.Size; if (p.IsUrban) uOther += p.Size; }
        double need = (0.6 * tOther - uOther) / (1.0 - 0.6);
        urban.Size = Math.Max(need, 1.0);
        if (!housingOn) c.HousingUrbanThreshold = 0.9;
        double share = r.SumUrbanPopulation() / r.SumPopulation();
        Assert.True(housingOn ? share > c.HousingUrbanThreshold : share <= c.HousingUrbanThreshold, $"前提：州の都市の割合 {share} が住居の門 {c.HousingUrbanThreshold} の{(housingOn ? "上" : "下")}でない");
        rural.EarnedPerMonth = new MoneyPerMonth(earnedR * rural.Size); rural.SubsistenceValuePerMonth = new MoneyPerMonth(subR * rural.Size);
        urban.EarnedPerMonth = new MoneyPerMonth(earnedU * urban.Size);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = 0.9 * rural.FoodNeed;
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = 0.9 * urban.FoodNeed;
        r.Markets[Catalog.HousingId].Price = housingPrice;
        r.CapitalEnabled = false;
        return (w, r, rural, urban, c);
    }

    static double G(double vr, double vu) => vr + vu > 1e-9 ? 2.0 * (vu - vr) / (vr + vu) : 0.0;

    [Fact]
    public void V_農村は現金と自給_都市は現金から住居_手の算術()
    {
        var (_, r, rural, urban, c) = Board(earnedR: 0.30, subR: 0.20, earnedU: 0.60, housingPrice: 2.0, housingOn: true);
        Assert.Equal(0.03, c.HousingNeedPerUrban);   // 前提：住居の必要量は都市 1 人あたり 0.03
        var v = PopDecision.MigrationValues(r, c, rural, urban);
        double vr = 0.30 + 0.20, vu = 0.60 - 0.03 * 2.0;
        Assert.Equal(vr, v.VRural, 12); Assert.Equal(vu, v.VUrban, 12);
        Assert.Equal(0.20, v.SubsistenceValue, 12); Assert.Equal(0.06, v.HousingCost, 12);
        Assert.Equal(G(vr, vu), v.Gsym, 12);
    }

    [Fact]
    public void V_住居の門の下では住居の費用が立たない()
    {
        var (_, r, rural, urban, c) = Board(0.30, 0.20, 0.60, housingPrice: 2.0, housingOn: false);
        var v = PopDecision.MigrationValues(r, c, rural, urban);
        Assert.Equal(0.0, v.HousingCost, 12);
        Assert.Equal(0.60, v.VUrban, 12);
    }

    [Fact]
    public void V_家賃が現金を超えるときは0で切る_gは符号が反転せず発散しない()
    {
        // 都市の現金 0.01・家賃 0.03×1.0＝0.03 → V都市＝0（切る）・農村は 0.5 → g＝−2（都市の方が貧しい＝農村へ）。切らなければ vu＝−0.02・g＝2(−0.02−0.5)/0.48＝−2.17
        var (_, r, rural, urban, c) = Board(0.30, 0.20, 0.01, housingPrice: 1.0, housingOn: true);
        var v = PopDecision.MigrationValues(r, c, rural, urban);
        Assert.Equal(0.0, v.VUrban, 12); Assert.Equal(-2.0, v.Gsym, 12);
        // 縁：農村の現金・自給とも 0、都市は家賃で 0 → 両方 0 → g＝0
        var (_, r2, rural2, urban2, c2) = Board(0.0, 0.0, 0.0, housingPrice: 1.0, housingOn: true);
        Assert.Equal(0.0, PopDecision.MigrationValues(r2, c2, rural2, urban2).Gsym);
        // 負の現金（資本家の粗利潤のように負になりうる量）は 0 に切る
        var (_, r3, rural3, urban3, c3) = Board(-5.0, 0.20, 0.6, housingPrice: 0.0, housingOn: true);
        var v3 = PopDecision.MigrationValues(r3, c3, rural3, urban3);
        Assert.Equal(0.20, v3.VRural, 12);
        Assert.InRange(v3.Gsym, -2.0, 2.0);
    }

    // ---------------------------------------------------------------- ③ 旗 UrbanFlowReal ----------------------------------------------------------------

    /// <summary>本物の Run の前後の都市 POP の規模の差（農村→都市が正）。</summary>
    static double Moved(World w, Region r, Pop rural, Pop urban, Coefficients c, double serfFree = 1.0)
    {
        double u0 = urban.Size, r0 = rural.Size;
        PopDecision.Run(r, c, serfFree);
        double d = urban.Size - u0;
        Assert.True(Math.Abs((r0 - rural.Size) - d) <= 1e-9 * Math.Max(1, Math.Abs(d)), "前提：農村の減りと都市の増えが合わない");
        return d;
    }

    [Fact]
    public void 旗0は新しい入力_自給の値と住居の価格_を読まない_ビット一致()
    {
        // 同じ所得の 2 つの盤面で、自給の値と住居の価格だけを大きく違えても、旗 0 の Run の結果は 1 ビットも違わない
        var (wa, ra, rua, ura, ca) = Board(0.10, 0.0, 0.30, housingPrice: 1.0, housingOn: true);
        var (wb, rb, rub, urb, cb) = Board(0.10, 5.0, 0.30, housingPrice: 100.0, housingOn: true);
        Assert.Equal(0.0, ca.UrbanFlowReal); Assert.Equal(0.0, cb.UrbanFlowReal);
        double da = Moved(wa, ra, rua, ura, ca), db = Moved(wb, rb, rub, urb, cb);
        Assert.True(Math.Abs(da) > 1e-9, "前提：この盤面で人が動いていない");
        Assert.Equal(BitConverter.DoubleToInt64Bits(da), BitConverter.DoubleToInt64Bits(db));
    }

    [Fact]
    public void 旗1の流れは手の算術どおり_農村から都市と都市から農村()
    {
        // 農村→都市：vr＝0.10＋0.05＝0.15・vu＝0.60−0.03×2＝0.54 → g＞0 → 流れ＝0.02×g×農村の規模
        var (w, r, rural, urban, c) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
        c.UrbanFlowReal = 1; Assert.Equal(0.02, c.Mobility);
        double g = G(0.15, 0.54), rs = rural.Size;
        Assert.True(g > 0);
        Assert.Equal(0.02 * g * rs, Moved(w, r, rural, urban, c), 9);
        // 都市→農村：vr＝0.50＋0.30＝0.80・vu＝0.40−0.06＝0.34 → g＜0 → 流れ＝0.02×g×都市の規模（負）
        var (w2, r2, rural2, urban2, c2) = Board(0.50, 0.30, 0.40, housingPrice: 2.0, housingOn: true);
        c2.UrbanFlowReal = 1;
        double g2 = G(0.80, 0.34), us = urban2.Size;
        Assert.True(g2 < 0);
        Assert.Equal(0.02 * g2 * us, Moved(w2, r2, rural2, urban2, c2), 9);
    }

    [Fact]
    public void 旗1_釣り合い_V農村とV都市が等しければ流れは0()
    {
        // vr＝0.20＋0.10＝0.30・vu＝0.36−0.03×2＝0.30
        var (w, r, rural, urban, c) = Board(0.20, 0.10, 0.36, housingPrice: 2.0, housingOn: true);
        c.UrbanFlowReal = 1;
        Assert.Equal(0.0, Moved(w, r, rural, urban, c), 9);
    }

    [Fact]
    public void 旗1_自給を0にすると名目の現金の比べと同じ向き_住居を0にしたとき()
    {
        // 自給 0・住居の門の下＝V は現金そのもの＝都市の現金が高ければ農村→都市・低ければ都市→農村（旧の名目の比べと同じ向き）
        var (w, r, rural, urban, c) = Board(0.10, 0.0, 0.30, housingPrice: 5.0, housingOn: false);
        c.UrbanFlowReal = 1;
        Assert.True(Moved(w, r, rural, urban, c) > 0);
        var (w2, r2, rural2, urban2, c2) = Board(0.30, 0.0, 0.10, housingPrice: 5.0, housingOn: false);
        c2.UrbanFlowReal = 1;
        Assert.True(Moved(w2, r2, rural2, urban2, c2) < 0);
    }

    [Fact]
    public void 旗1_食料の値段が上がると_自給の値が増え_農村から都市への流れが比例で細る()
    {
        // 自給の値＝量×値段。値段が 2 倍なら自給の値が 2 倍（欄を倍にして盤面で確かめる）→ vr が増え g が減る
        var (w, r, rural, urban, c) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
        c.UrbanFlowReal = 1;
        double d1 = Moved(w, r, rural, urban, c);
        var (w2, r2, rural2, urban2, c2) = Board(0.10, 0.10, 0.60, housingPrice: 2.0, housingOn: true);
        c2.UrbanFlowReal = 1;
        double d2 = Moved(w2, r2, rural2, urban2, c2);
        Assert.True(d1 > 0 && d2 > 0 && d2 < d1, $"自給の値が 2 倍で流れが細らない（{d1}→{d2}）");
        // 手の算術：g(0.15, 0.54) と g(0.20, 0.54) の比
        Assert.Equal(G(0.20, 0.54) / G(0.15, 0.54), d2 / d1, 6);
    }

    [Fact]
    public void 旗1_1か月に動けるのは送り手の最大4パーセント_農村が空でも都市の割合が極端でも()
    {
        // 農村の V＝0・都市の V＝1 → g＝2 → 流れ＝0.02×2×農村の規模＝農村の 4%（ちょうど）
        var (w, r, rural, urban, c) = Board(0.0, 0.0, 1.03, housingPrice: 1.0, housingOn: true);   // vu＝1.03−0.03＝1.0
        c.UrbanFlowReal = 1;
        double rs = rural.Size;
        Assert.Equal(0.04 * rs, Moved(w, r, rural, urban, c), 9);
        // 逆：都市の V＝0（家賃で切る）・農村の V＞0 → g＝−2 → 都市の 4%
        var (w2, r2, rural2, urban2, c2) = Board(0.5, 0.0, 0.0, housingPrice: 1.0, housingOn: true);
        c2.UrbanFlowReal = 1;
        double us = urban2.Size;
        Assert.Equal(-0.04 * us, Moved(w2, r2, rural2, urban2, c2), 9);
    }

    [Fact]
    public void 旗1でも農奴の門と飢えの門は残る()
    {
        var (w, r, rural, urban, c) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
        c.UrbanFlowReal = 1; c.SerfMobilityGate = 1.0;
        double open = Moved(w, r, rural, urban, c, 1.0);
        var (w2, r2, rural2, urban2, c2) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
        c2.UrbanFlowReal = 1; c2.SerfMobilityGate = 1.0;
        Assert.Equal(0.25 * open, Moved(w2, r2, rural2, urban2, c2, 0.25), 9);   // 農奴の門＝(1−g)+g×serfFree＝0.25
        var (w3, r3, rural3, urban3, c3) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
        c3.UrbanFlowReal = 1; urban3.FoodMet = 0.2 * urban3.FoodNeed;   // 都市が飢えの線の下で農村より食べられない
        Assert.Equal(0.0, Moved(w3, r3, rural3, urban3, c3));
    }

    // ---------------------------------------------------------------- ④ 計器 urxpd_s の新しい列 ----------------------------------------------------------------

    [Fact]
    public void 計器の駆動の行は本物のMigrationValuesと手の算術に合い_旗1の流れは本物のRunと一致する()
    {
        var (w, r, rural, urban, c) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
        // 旗 0：列は出る（式は常に出す）・流れは旧の式
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var d0));
        Assert.Equal(0.05, d0.SubsistenceValue, 12); Assert.Equal(0.06, d0.HousingCost, 12); Assert.Equal(G(0.15, 0.54), d0.Gsym, 12);
        // 旗 1：計器の流れ＝本物の Run の都市の増分
        c.UrbanFlowReal = 1;
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var d1));
        double moved = Moved(w, r, rural, urban, c);
        Assert.True(Math.Abs(moved) > 1e-9);
        Assert.True(Math.Abs(moved - d1.Flow) <= 1e-9 * Math.Max(1, Math.Abs(moved)), $"計器の flow {d1.Flow} と本物の Run {moved}");
    }

    static void PdMonth(World w, Region r, Pop rural, Pop urban, double earnedR, double earnedU, double urbanFill)
    {
        rural.EarnedPerMonth = new MoneyPerMonth(earnedR * rural.Size);
        urban.EarnedPerMonth = new MoneyPerMonth(earnedU * urban.Size);
        urban.FoodMet = urbanFill * urban.FoodNeed;
        Instruments.UrxPd(w, r, 1.0);
    }

    [Fact]
    public void 流れの向きの反転の数え方_前の月も今の月も0でなく符号が逆のときだけ()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_urb3_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (w, r, rural, urban, c) = Board(0.10, 0.0, 0.30, housingPrice: 1.0, housingOn: false);
            Assert.Equal(Tier(r), RegionTier.Core);
            urban.Size = rural.Size;   // 対の都市の割合 0.5＝所得の差の向きがそのまま流れの向き（目標 0.79／0.21）
            long Flip() => Instruments.UrxPdPeek(w).FlipRegionMonths;
            PdMonth(w, r, rural, urban, 0.10, 0.30, 0.9);   // 農村→都市（+）
            Assert.Equal(0, Flip());
            PdMonth(w, r, rural, urban, 0.10, 0.30, 0.9);   // +（同じ向き）
            Assert.Equal(0, Flip());
            PdMonth(w, r, rural, urban, 0.30, 0.10, 0.9);   // −（反転）
            Assert.Equal(1, Flip());
            PdMonth(w, r, rural, urban, 0.30, 0.10, 0.9);   // −（同じ向き）
            Assert.Equal(1, Flip());
            PdMonth(w, r, rural, urban, 0.10, 0.30, 0.2);   // + だが飢えの門で 0
            Assert.Equal(1, Flip());
            PdMonth(w, r, rural, urban, 0.30, 0.10, 0.9);   // − ：前の月が 0 なので反転と数えない
            Assert.Equal(1, Flip());
            PdMonth(w, r, rural, urban, 0.10, 0.30, 0.9);   // + ：前の月が − → 反転
            Assert.Equal(2, Flip());
            Assert.Equal(7, Instruments.UrxPdPeek(w).RegionMonths);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    static RegionTier Tier(Region r) => r.Tier;

    [Fact]
    public void 計器の積みの列は_total_で重みを付けた和()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_urb3b_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (w, r, rural, urban, c) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
            double total = rural.Size + urban.Size;
            Instruments.UrxPd(w, r, 1.0);
            Instruments.UrxPd(w, r, 1.0);
            var p = Instruments.UrxPdPeek(w);
            Assert.Equal(2, p.RegionMonths);
            Assert.Equal(2 * total, p.SumTotal, 9);
            Assert.Equal(2 * total * 0.05, p.WSubV, 9);
            Assert.Equal(2 * total * 0.06, p.WHousU, 9);
            Assert.Equal(2 * total * G(0.15, 0.54), p.WGsym, 9);
            Assert.Equal(2 * total * Math.Abs(G(0.15, 0.54)), p.WAbsGsym, 9);   // (713) urb4
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 計器の列_wAbsGsym_は符号が混ざっても打ち消し合わない()
    {
        // ★(713) urb4：1 か月目は都市の値打ちが高い（g＞0）・2 か月目は低い（g＜0）。wGsym は符号つきの和＝打ち消し合う・wAbsGsym は |g| の和。期待値は盤面の量から手で（G＝対称の相対差の手の算術）。
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_urb4_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (w, r, rural, urban, c) = Board(0.10, 0.05, 0.60, housingPrice: 2.0, housingOn: true);
            double total = rural.Size + urban.Size;
            double g1 = G(0.10 + 0.05, 0.60 - 0.03 * 2.0);   // 0.15 対 0.54 ＝ 正
            Instruments.UrxPd(w, r, 1.0);
            urban.EarnedPerMonth = new MoneyPerMonth(0.10 * urban.Size);
            double g2 = G(0.10 + 0.05, 0.10 - 0.03 * 2.0);   // 0.15 対 0.04 ＝ 負
            Instruments.UrxPd(w, r, 1.0);
            Assert.True(g1 > 0 && g2 < 0, "前提：2 か月の g の符号が逆でない");
            var p = Instruments.UrxPdPeek(w);
            Assert.Equal(total * (g1 + g2), p.WGsym, 9);
            Assert.Equal(total * (g1 - g2), p.WAbsGsym, 9);
            Assert.True(p.WAbsGsym > Math.Abs(p.WGsym) + 0.1 * total, "wAbsGsym が符号つきの和の絶対値と区別できない");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
