using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class CreditTests
{
    private static Scenario Credit(ulong s, int shockQ = -1, double shockMag = 0) => new()
    {
        Seed = s,
        Kind = ScenarioKind.AgrarianToUrban,
        EducationLaw = EducationLaw.Public,
        Freedom = 0.9,
        EducationSpending = 1.0,
        CreditEnabled = true,
        ShockQuarterIndex = shockQ,
        ShockMagnitude = shockMag,
    };

    [Fact]
    public void 信用下でも基礎貨幣は保存され広義マネーは増える()
    {
        var sim = new Simulation(Credit(1), new NullAgent());
        double base0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);   // §11.6（基礎貨幣保存）が全ティックで保持

        Assert.True(Math.Abs(sim.World.BaseMoney - base0) < 1e-3 * base0, "基礎貨幣が保存されない");
        Assert.True(sim.World.BroadMoney > base0 * 1.05, "信用で広義マネーが増えていない（ブームなし）");
    }

    [Fact]
    public void 好況で信用が拡大する()
    {
        var sim = new Simulation(Credit(1), new NullAgent());
        sim.Run(400);
        Assert.True(sim.World.OutstandingLoans > 0, "与信が出ていない");
        Assert.True(sim.World.Bank!.Confidence > 0.5, "信用意欲が低い");
    }

    [Fact]
    public void 金融ショックで信用が収縮し回復する()   // 恐慌→回復（§2.7）
    {
        var sim = new Simulation(Credit(1, shockQ: 200, shockMag: 0.9), new NullAgent());
        sim.Run(400);
        var t = sim.Telemetry;

        double preShock = t[195].Loans;
        double trough = double.MaxValue;
        for (int i = 200; i <= 220; i++) trough = Math.Min(trough, t[i].Loans);
        double recovered = t[260].Loans;

        Assert.True(trough < preShock * 0.85, $"ショックで信用収縮しない（{preShock:F0}→{trough:F0}）");
        Assert.True(recovered > trough * 1.1, $"信用が回復しない（{trough:F0}→{recovered:F0}）");
    }

    // ── 与信の上限の盤面（A16 の番人2本）：英を1四半期温めた世界（銀行と首都の投資家がいる）に、投資家の前月の所得と与信の残高だけを置く ──
    private static World Board(double monthlyIncome, double loans)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        var inv = w.Region.Pops[w.Region.InvestorPopIndex];
        inv.LastEarned = monthlyIncome;
        double d = loans - w.Bank!.Loans; w.Bank.Loans += d; inv.Savings += d;   // 信用創造の対（与信と預金）＝基礎貨幣は動かない
        w.Explain = new ExplainLedger();
        return w;
    }
    /// <summary>Credit.Step を1日。返り値＝説明API の内訳の [0]（与信の上限）・[0]+[1]（取り付けと最後の貸し手の後の実効の上限）・過熱度・残高。</summary>
    private static (double Cap, double EffCap, double Heat, double Loans) Day(World w)
    {
        PriceOfBread.SimCore.Credit.Step(w, w.Coef);   // ⚠このクラスの盤面の関数 Credit(…) と同名＝名前空間で指す
        var t = w.Explain!.Get("CreditTarget");
        return (t[0].Delta, t[0].Delta + t[1].Delta, w.Bank!.Heat, w.Bank.Loans);
    }
    private static readonly double years = Calendar.MonthsPerYear * new Coefficients().SoeSaleYears;   // 返せる額＝前月の所得×これ
    private const double Rich = 1e30;                                                                   // 返せる額が効かない所得

    /// <summary>★与信の上限に借り手の返せる額（作者裁定 A16・E6 の1歩目・2026-09-28）。上限＝「準備×信用乗数」と「首都の投資家の前月の所得×12×SoeSaleYears」の小さい方。
    /// 盤面＝英を1四半期温めた世界（銀行と首都の投資家がいる）。投資家の前月の所得と与信の残高だけを置き、Credit.Step を1日回して説明API の内訳（1行目＝与信の上限）と過熱度を読む。
    /// (1) 所得が十分なら上限は準備の上限のまま (2) 返せる額が準備の上限より小さければ上限は返せる額 (3) 所得が半分なら上限も半分・同じ残高で過熱度は倍（債務÷所得）
    /// (4) 最後の貸し手の準備拡張でも実効の上限は返せる額を越えない（対照＝所得が十分なら拡張が効く） (5) 返せる額を越えた残高は回収で下がる（対照＝所得が十分なら下がらない）
    /// (6) 投資家のいない盤面（全POPへ注入する枝）は準備の上限だけ（従来と同じ）。</summary>
    [Fact]
    public void 返せない借り手には与信の上限が縮み_越えた残高は回収される()
    {
        // (1) 所得が十分＝準備の上限
        double reserveCap = Day(Board(Rich, 0)).Cap;
        Assert.True(reserveCap > 0, $"前提が崩れた：英の準備の上限が0（{reserveCap}）");
        // (2) 返せる額＝準備の上限の1/10
        double repay = reserveCap / 10, income = repay / years;
        var poor = Day(Board(income, 0));
        Assert.True(Math.Abs(poor.Cap - repay) <= 1e-12 * repay, $"返せる額が準備の上限より小さいのに上限が返せる額にならない（上限 {poor.Cap}・返せる額 {repay}・準備の上限 {reserveCap}）");
        // (3) 所得が半分＝上限も半分・同じ残高で過熱度は倍
        double l0 = 0.25 * repay;
        var full = Day(Board(income, l0)); var half = Day(Board(income / 2, l0));
        Assert.True(Math.Abs(half.Cap - repay / 2) <= 1e-12 * repay, $"所得が半分でも上限が半分にならない（{half.Cap} 対 {repay / 2}）");
        Assert.True(full.Heat > 0 && Math.Abs(half.Heat - 2 * full.Heat) <= 1e-9 * full.Heat, $"所得が半分なのに同じ残高の過熱度が倍にならない（{full.Heat} → {half.Heat}）");
        // (4) 危機（前日の過熱度＞発動線）×財政健全＝最後の貸し手が準備を広げる。返せる額は越えない
        static World Crisis(double inc) { var w = Board(inc, 0); w.Bank!.Heat = w.Coef.LolrHeatTrigger + 0.5; return w; }
        var crisisRich = Day(Crisis(Rich));
        Assert.True(crisisRich.EffCap > crisisRich.Cap, $"前提が崩れた：最後の貸し手の準備拡張が働いていない（{crisisRich.Cap} → {crisisRich.EffCap}）");
        var crisisPoor = Day(Crisis(income));
        Assert.True(crisisPoor.EffCap <= repay * (1 + 1e-12), $"最後の貸し手の準備拡張で与信の上限が返せる額を越えた（{crisisPoor.EffCap} ＞ {repay}）");
        // (5) 返せる額の3倍の残高（投資家の貯蓄は十分）→回収で下がる。対照＝所得が十分なら同じ残高は下がらない
        double over = 3 * repay / 100;
        var recall = Day(Board(income / 100, over));
        Assert.True(recall.Loans < over, $"返せる額を越えた与信が回収されない（{over} → {recall.Loans}）");
        Assert.True(Day(Board(Rich, over)).Loans >= over, "対照が崩れた：所得が十分でも同じ残高が回収された（準備の上限が低すぎる盤面）");
        // (6) 投資家のいない盤面＝準備の上限だけ
        var nw = Board(income, 0); nw.Region.InvestorPopIndex = -1;
        Assert.Equal(reserveCap, Day(nw).Cap);
    }

    /// <summary>★基礎貨幣が負の国でも与信の回収は返せる額を越えて続かない（作者裁定 A16・E6 の1歩目・2026-09-28）。
    /// 回収は基礎貨幣を動かさない（与信と投資家の預金が同じ額だけ減る）＝負の基礎貨幣は回収では戻らない（戻すのは交易・鋳造・発行と、E6 の2歩目の償却）。
    /// だから負の国で回収が消す貨幣の総額は、負へ落ちた時点の与信の残高で決まる。旧＝準備×乗数まで貸した残高（国の広義の貨幣の大半）を消すまで回収が続いた
    /// （(79) の英＝停止の後にカバー率 −2.8→−300→−36,000）。盤面＝英（返せる額＝準備の上限の1/10）。
    /// (1) 信頼1の好況で2000日貸しても、残高は毎日返せる額の内側（過熱で最後の貸し手が働いても）
    /// (2) 国内の金が流出して基礎貨幣が負（与信の半分）になった後、国内の売り上げが毎日投資家へ入り銀行が3000日回収を続けても、
    ///     回収の総額は返せる額を越えない・残高は増えない・基礎貨幣は動かない（＝戻すのは回収でない）。</summary>
    [Fact]
    public void 基礎貨幣が負の国でも与信の回収は返せる額を越えて続かない()
    {
        double reserveCap = Day(Board(Rich, 0)).Cap;
        double repay = reserveCap / 10;
        var w = Board(repay / years, 0);
        var bank = w.Bank!; var inv = w.Region.Pops[w.Region.InvestorPopIndex];
        // (1) 好況
        bank.Confidence = 1.0;
        double peak = 0;
        for (int d = 0; d < 2000; d++) { Day(w); peak = Math.Max(peak, bank.Loans); }
        Assert.True(peak <= repay * (1 + 1e-12), $"好況の与信が返せる額を越えた（最大 {peak} ＞ {repay}）");
        Assert.True(bank.Loans > 0.2 * repay, $"前提が崩れた：好況で与信が伸びていない（{bank.Loans}・返せる額 {repay}）");
        // (2) 国内の金が流出＝国庫を0へ・全POPの正の貯蓄を同じ割合で減らし、基礎貨幣＝−与信/2 に置く（与信の残高は動かさない）
        if (w.Treasury > 0) w.Pay(TreasuryCat.OtherOut, -w.Treasury);
        double loans0 = bank.Loans, pos = 0;
        foreach (var p in w.AllPops()) pos += Math.Max(0, p.Savings);
        double keep = 1.0 - (w.TotalMoney - 0.5 * loans0) / pos;
        Assert.True(keep > 0 && keep < 1, $"前提が崩れた：流出の割合が作れない（{keep}）");
        foreach (var p in w.AllPops()) if (p.Savings > 0) p.Savings *= keep;
        double base0 = w.BaseMoney;
        Assert.True(base0 < 0 && Math.Abs(base0 + 0.5 * loans0) <= 1e-6 * loans0, $"前提が崩れた：基礎貨幣が与信の半分の負になっていない（{base0}・与信 {loans0}）");
        double prev = loans0;
        for (int d = 0; d < 3000; d++)
        {
            foreach (var p in w.AllPops())   // 国内の売り上げ＝他のPOPの貯蓄の1%が投資家へ（国内の移転＝基礎貨幣は動かない）
                if (!ReferenceEquals(p, inv) && p.Savings > 0) { double x = 0.01 * p.Savings; p.Savings -= x; inv.Savings += x; }
            Day(w);
            Assert.True(bank.Loans <= prev * (1 + 1e-12), $"基礎貨幣が負の国で与信が増えた（{prev} → {bank.Loans}・{d}日目）");
            prev = bank.Loans;
        }
        double recalled = loans0 - bank.Loans;
        Assert.True(recalled > 0, "前提が崩れた：負の国で回収が起きていない");
        Assert.True(recalled <= repay * (1 + 1e-9), $"負の国の回収が返せる額を越えて続いた（回収の総額 {recalled} ＞ 返せる額 {repay}）");
        Assert.True(Math.Abs(w.BaseMoney - base0) <= 1e-9 * loans0, $"回収で基礎貨幣が動いた（{base0} → {w.BaseMoney}）＝回収は負の基礎貨幣を戻さない、の前提が崩れた");
    }

    [Fact]
    public void 信用下でも決定論的()
    {
        var a = new Simulation(Credit(3, 200, 0.9), new NullAgent());
        var b = new Simulation(Credit(3, 200, 0.9), new NullAgent());
        a.Run(300);
        b.Run(300);
        Assert.Equal(a.World.BroadMoney, b.World.BroadMoney);
        Assert.Equal(a.World.OutstandingLoans, b.World.OutstandingLoans);
    }
}
