using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>観戦の読みからの表示の直し（2026-11-12 U3・U7・U8・U9・U15）の番人。機構と不変条件だけ（見た目は --uishot が見る）。</summary>
public class ObservationFixTests
{
    private static (MultiNationSim Sim, World W) Board(int quarters)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return (sim, sim.Nations[0].World);
    }

    [Fact]
    public void U3_終戦の窓は州の得失と属国化を書き_字種は全角()
    {
        string won = GameViewModel.WarEndPopupText("プロイセン", "勝利", "+50%", 12, "1.0K", "2.0K", "0.1K", "0.2K", "£1.0M", null, true,
            ["Alsace"], [], madeVassal: true, becameVassal: false);
        Assert.Contains("得たもの：賠償金、州　Alsace、プロイセンを属国に", won);
        Assert.DoesNotContain("失ったもの", won);
        string lost = GameViewModel.WarEndPopupText("フランス", "敗北", "-50%", 6, "1.0K", null, "0.1K", null, "£1.0M", null, false,
            [], ["Lorraine", "Alsace"], madeVassal: false, becameVassal: true);
        Assert.Contains("失ったもの：賠償金、州　Lorraine、Alsace、フランスの属国に", lost);
        string blank = GameViewModel.WarEndPopupText("フランス", "白紙講和", "+0%", 3, "1.0K", null, "0.1K", null, "£1.0M", null, null, [], [], false, false);
        Assert.Contains("得たもの：なし（白紙講和）", blank);
        Assert.All(new[] { won, lost, blank }, t => Assert.True(!t.Contains(':') && !t.Contains('(') && !t.Contains(')'), $"半角の括弧かコロンが残る: {t}"));
    }

    [Fact]
    public void U7_貢納は月あたりに均して出し_側ごとの実績の合計は国庫の台帳のまま()
    {
        var (_, w) = Board(6);
        double r = w.FiscalFundingRatio.Value;
        w.LastTributeOutPerQuarter = 3000;
        foreach (bool payMonth in new[] { true, false })
        {
            var led = new double[(int)TreasuryCat.Count];
            led[(int)TreasuryCat.Tax] = 500_000;
            led[(int)TreasuryCat.Army] = -200_000;
            if (payMonth) led[(int)TreasuryCat.TributeOut] = -3000 * r;   // 払った月だけ 3 か月分
            var b = FiscalTables.Budget(w, new FiscalFlows(0, 0, 0, 0, led));
            var trib = b.Spend.First(l => l.Key == "tributeOut");
            Assert.Equal(Calendar.PerQuarterToPerMonth(3000 * r), trib.Actual, 6);   // 払った月でも他の月でも同じ月あたり
            Assert.Equal(FiscalTables.LedgerBalance(new FiscalFlows(0, 0, 0, 0, led)), b.BalanceActual, 4);   // 収支は台帳のまま
            Assert.Equal(b.Spend.Where(l => !l.NoAmounts && !l.Child).Sum(l => l.Actual), b.SpendActual, 6);
        }
    }

    [Fact]
    public void U8_失った州の勢力は表に出さない()
    {
        var (sim, w) = Board(4);
        var foreign = sim.Nations[1].World.Regions[0];
        w.Factions.Clear();
        var own = new FactionState { Region = w.Regions[0], Bloc = 2, Ideo = 0, Size = 10, Cohesion = 0.5 };
        var ghost = new FactionState { Region = foreign, Bloc = 2, Ideo = 0, Size = 99, Cohesion = 0.5 };
        w.Factions.Add(ghost); w.Factions.Add(own);
        var live = ProblemScan.LiveFactions(w);
        Assert.Single(live);
        Assert.Same(own, live[0]);
        Assert.Equal(2, w.Factions.Count);   // シミュの側は掃除しない
    }

    [Fact]
    public void U9_金利の錠の間は段の列の目標が今の金利()
    {
        var (sim, w) = Board(2);
        w.Policy.InterestRateTarget = 0.5;
        w.InterestRate = 0.0;
        w.MonetaryLaw = MonetaryStandardLaw.Gold; w.OnGoldStandard = true; w.CapitalControlLaw = false;
        var st = (StageCell)FiscalTables.Finance.Read(sim, w).Rows.First(r => r.Key == "policyRate").Cells[2]!;
        Assert.Equal(st.Current, st.Target);
        Assert.False(st.CanSelect(2));
        w.OnGoldStandard = false;   // 錠が外れれば目標をそのまま出す
        var st2 = (StageCell)FiscalTables.Finance.Read(sim, w).Rows.First(r => r.Key == "policyRate").Cells[2]!;
        Assert.NotEqual(st2.Current, st2.Target);
    }

    [Fact]
    public void U15_台帳の区分は側が固定_戻りで差分が正になった戦費も支出の側にある()
    {
        var (_, w) = Board(4);
        var led = new double[(int)TreasuryCat.Count];
        led[(int)TreasuryCat.Tax] = 500_000;
        led[(int)TreasuryCat.WarCost] = +5000;   // 戻り・相殺で差分が正
        var b = FiscalTables.Budget(w, new FiscalFlows(0, 0, 0, 0, led));
        Assert.DoesNotContain(b.Income, l => l.Key == "cat." + TreasuryCat.WarCost);
        var war = b.Spend.First(l => l.Key == "cat." + TreasuryCat.WarCost);
        Assert.Equal(-5000, war.Actual, 6);
        Assert.Equal(FiscalTables.LedgerBalance(new FiscalFlows(0, 0, 0, 0, led)), b.BalanceActual, 4);
    }
}
