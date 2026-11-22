// (1306) c135c3 の使い捨ての診断（コミットしない・D:/PoB_work/r1306_work/tree の写しの試験の中だけ）
// TaxChainCheck と同じ盤面（仏 1789・NullAgent・緊縮の床を隔離・開幕の国債を消す・助走の後に人頭税を段差で入れる）を
// 勢いの重み 0.3（main の既定）と 0（K49 の既定 0 の木）で並べ、助走 100 の後の 20 四半期を四半期ごとに書き出す。
// 力学に何も書かない（読むだけ）。出力は D:/PoB_work/r1306_work/out/。
using System.Text;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

static class R1306
{
    static readonly string Out = Environment.GetEnvironmentVariable("R1306_OUT") ?? "D:/PoB_work/r1306_work/out";
    static string F(double x) => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    public static void Go(double iew, int warmup, string tag)
    {
        var pop = new StringBuilder("iew\twarmup\ttax\tq\tregion\tclass\tname\tsize\tsat\tmil\tmilVoice\tmilFamine\tmilWar\tmilNation\tfoodMet\tfoodNeed\tfoodRef\tlivingRef\tconsc\tliteracy\tsavings\tearned\tpaid\tneedMet\tneedTotal\tunemp\tideo\tdisc\tlivingWant\n");
        var prod = new StringBuilder("iew\twarmup\ttax\tq\tregion\tidx\trecipe\tgood\tscale\tbuilt\tsmMargin\trev\tcost\tbuysLabor\tinFill\tbankrupt\tmothball\n");
        var reg = new StringBuilder("iew\twarmup\ttax\tq\tregion\tinfraBudget\telecInfraBudget\tadminBudget\tinfraFill\telecInfraFill\tadminFill\thousingFill\tpriceLevel\tpInfra\tpElecInfra\tpGas\tpCement\tpSteel\tpLabor\ttreasury\tpollPaid\ttaxRate\tpollTax\tpolity\n");
        var mkt = new StringBuilder("iew\twarmup\ttax\tq\tregion\tgood\tname\ttier\tprice\tstock\tproduced\tsold\tshortage\timport\texport\tdemand\tration\tbought\n");
        foreach (double tax in new[] { 0.0, 0.6 })
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            var w = sim.World;
            w.Coef.InvestExtrapolationWeight = iew;
            w.Coef.AusterityStartDebt = new RatioToGdpYear(99);
            w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0); foreach (var rg in w.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = new MoneyStock(0);
            void Dump(int q)
            {
                string h = $"{F(iew)}\t{warmup}\t{F(tax)}\t{q}";
                var dc = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, w.Coef); var lg = w.Coef.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : new double[0];
                foreach (var r in w.Regions)
                {
                    reg.Append($"{h}\t{r.Id}\t{F(r.InfraBudget)}\t{F(r.ElecInfraBudget)}\t{F(r.AdminBudget)}\t{F(r.InfraFill)}\t{F(r.ElecInfraFill)}\t{F(r.AdminFill)}\t{F(r.HousingFill)}\t{F(r.PriceLevel)}\t{F(r.Markets[Catalog.InfrastructureId].Price)}\t{F(r.Markets[Catalog.ElecInfraId].Price)}\t{F(r.Markets[Catalog.GasId].Price)}\t{F(r.Markets[Catalog.CementId].Price)}\t{F(r.Markets[Catalog.SteelId].Price)}\t{F(r.Markets[Catalog.LaborId].Price)}\t{F(w.Treasury.Value)}\t{F(w.LastTaxPollPerMonth.Value)}\t{F(w.TaxRate.Value)}\t{F(w.PollTax)}\t{w.Polity}\n");
                    foreach (var p in r.Pops)
                        pop.Append($"{h}\t{r.Id}\t{p.ClassId}\t{p.ClassName}\t{F(p.Size)}\t{F(p.Satisfaction)}\t{F(p.Militancy)}\t{F(p.MilVoice)}\t{F(p.MilFamine)}\t{F(p.MilWar)}\t{F(p.MilNation)}\t{F(p.FoodMet)}\t{F(p.FoodNeed)}\t{F(p.FoodRef)}\t{F(p.LivingRef)}\t{F(p.Consciousness)}\t{F(p.Literacy)}\t{F(p.Savings.Value)}\t{F(p.EarnedPerMonth.Value)}\t{F(p.PaidPerMonth.Value)}\t{F(p.NeedMet)}\t{F(p.NeedTotal)}\t{F(p.UnemploymentRate.Value)}\t{string.Join(",", p.IdeologyShares.Select(F))}\t{F(FactionSnapshot.DiscontentOf(w, p, dc, lg))}\t{F(Politics.LivingWant(p, w.Coef))}{(p.Closed || p.IsSoldier ? "\tX" : "")}\n");
                    for (int gi = 0; gi < r.Markets.Length && gi < Catalog.Goods.Length; gi++)
                    {
                        var g = Catalog.Goods[gi];
                        if (g.Tier != GoodTier.Staple && !(g.Nutrition > 0)) continue;
                        var m = r.Markets[gi];
                        string bought = "";
                        if (m.PopBought is not null) for (int pi = 0; pi < r.Pops.Count && pi < m.PopBought.Length; pi++) bought += $"{r.Pops[pi].ClassId}:{F(m.PopBought[pi])},";
                        mkt.Append($"{h}\t{r.Id}\t{gi}\t{g.Name}\t{g.Tier}\t{F(m.Price)}\t{F(m.Stock)}\t{F(m.LastProduced)}\t{F(m.LastSold)}\t{F(m.LastShortage)}\t{F(m.LastImport)}\t{F(m.LastExport)}\t{F(m.LastDemand)}\t{F(m.LastRation.Value)}\t{bought}\n");
                    }
                    for (int i = 0; i < r.Producers.Count; i++)
                    {
                        var s = r.Producers[i];
                        prod.Append($"{h}\t{r.Id}\t{i}\t{s.RecipeName}\t{s.GoodId}\t{F(s.Scale)}\t{F(s.BuiltScale)}\t{F(s.SmoothedMargin)}\t{F(s.LastRevenue)}\t{F(s.LastCost)}\t{(s.BuysLabor ? 1 : 0)}\t{F(s.LastInputFill)}\t{(s.Bankrupt ? 1 : 0)}\t{F(s.MothballFrac.Value)}\n");
                    }
                }
            }
            // 助走は税なし（2 つの世界はここまで同じ軌道）＝軽税の世界だけ助走の中も 4 四半期ごとに書く（q は負＝助走の残り）
            for (int q = 0; q < warmup; q++)
            {
                sim.Run(1);
                if (tax == 0.0 && (q + 1) % 4 == 0) Dump(q + 1 - warmup);
            }
            Dump(0);
            w.PollTax = tax; w.Policy.PollTaxTarget = tax;
            for (int q = 1; q <= 20; q++) { sim.Run(1); Dump(q); }
        }
        Directory.CreateDirectory(Out);
        File.WriteAllText($"{Out}/pop_{tag}.tsv", pop.ToString().Replace("\r", ""));
        File.WriteAllText($"{Out}/prod_{tag}.tsv", prod.ToString().Replace("\r", ""));
        File.WriteAllText($"{Out}/reg_{tag}.tsv", reg.ToString().Replace("\r", ""));
        File.WriteAllText($"{Out}/mkt_{tag}.tsv", mkt.ToString().Replace("\r", ""));
    }
}

public class R1306Main20 { [Fact] public void Go() => R1306.Go(0.3, 20, "main_w20"); }
public class R1306K0w20 { [Fact] public void Go() => R1306.Go(0.0, 20, "k0_w20"); }
public class R1306Main40 { [Fact] public void Go() => R1306.Go(0.3, 40, "main_w40"); }
public class R1306K0w40 { [Fact] public void Go() => R1306.Go(0.0, 40, "k0_w40"); }
public class R1306Main60 { [Fact] public void Go() => R1306.Go(0.3, 60, "main_w60"); }
public class R1306K0w60 { [Fact] public void Go() => R1306.Go(0.0, 60, "k0_w60"); }
public class R1306Main80 { [Fact] public void Go() => R1306.Go(0.3, 80, "main_w80"); }
public class R1306K0w80 { [Fact] public void Go() => R1306.Go(0.0, 80, "k0_w80"); }
public class R1306Main100 { [Fact] public void Go() => R1306.Go(0.3, 100, "main_w100"); }
public class R1306K0w100 { [Fact] public void Go() => R1306.Go(0.0, 100, "k0_w100"); }
public class R1306Main120 { [Fact] public void Go() => R1306.Go(0.3, 120, "main_w120"); }
public class R1306K0w120 { [Fact] public void Go() => R1306.Go(0.0, 120, "k0_w120"); }
