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
    public void U15_賠償の受け取りで戦費の差分が正になった月は収入の側の行に出し_戦費の行は払った額だけ()
    {
        var (_, w) = Board(4);
        var led = new double[(int)TreasuryCat.Count];
        led[(int)TreasuryCat.Tax] = 500_000;
        led[(int)TreasuryCat.WarCost] = +5000;   // 賠償を受け取った月（戻り・相殺）で差分が正
        var b = FiscalTables.Budget(w, new FiscalFlows(0, 0, 0, 0, led));
        Assert.DoesNotContain(b.Spend, l => l.Key == "cat." + TreasuryCat.WarCost);   // 戦費の行に負の払った額を出さない
        var rep = b.Income.First(l => l.Key == "reparationsIn");
        Assert.Equal("賠償の受け取り", rep.Name);
        Assert.Equal(5000, rep.Actual, 6);
        Assert.Equal(FiscalTables.LedgerBalance(new FiscalFlows(0, 0, 0, 0, led)), b.BalanceActual, 4);
        led[(int)TreasuryCat.WarCost] = -7000;   // 払った月は従来どおり支出の側の戦費
        var b2 = FiscalTables.Budget(w, new FiscalFlows(0, 0, 0, 0, led));
        Assert.Equal(7000, b2.Spend.First(l => l.Key == "cat." + TreasuryCat.WarCost).Actual, 6);
        Assert.DoesNotContain(b2.Income, l => l.Key == "reparationsIn");
    }

    [Fact]
    public void Z2_貢納を均した差は_同じ側のその他の収入の行が受け_その他の行を並べない()
    {
        var (_, w) = Board(4);
        w.LastTributeInPerQuarter = 3000;
        var led = new double[(int)TreasuryCat.Count];
        led[(int)TreasuryCat.OtherIn] = 80_000;
        led[(int)TreasuryCat.TributeIn] = 3000;   // 受け取った月だけ 3 か月分
        var f = new FiscalFlows(0, 0, 0, 0, led);
        var b = FiscalTables.Budget(w, f);
        Assert.DoesNotContain(b.Income, l => l.Key == "restIn");   // 同じ側に「その他」と「その他の収入」が並ばない
        Assert.Equal(82_000, b.Income.Single(l => l.Key == "cat.OtherIn").Actual, 6);   // 80000＋均した差（3000−1000）
        Assert.Equal(FiscalTables.LedgerBalance(f), b.BalanceActual, 4);   // 側ごとの合計は台帳のまま
    }
}
