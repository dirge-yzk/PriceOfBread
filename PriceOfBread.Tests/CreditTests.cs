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
        ShockQuarterIndex = shockQ,
        ShockMagnitude = shockMag,
    };

    [Fact]
    public void 信用下でも基礎貨幣は保存され広義マネーは増える()
    {
        var sim = new Simulation(Credit(1), new NullAgent());
        double base0 = sim.World.BaseMoney.Value;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);   // §11.6（基礎貨幣保存）が全ティックで保持

        Assert.True(Math.Abs(sim.World.BaseMoney.Value - base0) < 1e-3 * base0, "基礎貨幣が保存されない");
        Assert.True(sim.World.BroadMoney.Value > base0 * 1.05, "信用で広義マネーが増えていない（ブームなし）");
    }

    [Fact]
    public void 好況で信用が拡大する()
    {
        var sim = new Simulation(Credit(1), new NullAgent());
        sim.Run(400);
        Assert.True(sim.World.OutstandingLoans.Value > 0, "与信が出ていない");
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
        inv.LastEarnedPerMonth = new MoneyPerMonth(monthlyIncome);
        double d = loans - (w.Bank!.Loans).Value; w.Bank.Loans += new MoneyStock(d); inv.SetSavings(inv.Savings.Value + (d));   // 信用創造の対（与信と預金）＝基礎貨幣は動かない
        w.Explain = new ExplainLedger();
        return w;
    }
    /// <summary>Credit.Step を1日。返り値＝説明API の内訳の [0]（与信の上限）・[0]+[1]（取り付けと最後の貸し手の後の実効の上限）・過熱度・残高。</summary>
    private static (double Cap, double EffCap, double Heat, double Loans) Day(World w)
    {
        PriceOfBread.SimCore.Credit.Step(w, w.Coef);   // ⚠このクラスの盤面の関数 Credit(…) と同名＝名前空間で指す
        var t = w.Explain!.Get("CreditTarget");
        return (t[0].Delta, t[0].Delta + t[1].Delta, w.Bank!.Heat, w.Bank.Loans.Value);
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
        for (int d = 0; d < 2000; d++) { Day(w); peak = Math.Max(peak, bank.Loans.Value); }
        Assert.True(peak <= repay * (1 + 1e-12), $"好況の与信が返せる額を越えた（最大 {peak} ＞ {repay}）");
        Assert.True(bank.Loans.Value > 0.2 * repay, $"前提が崩れた：好況で与信が伸びていない（{bank.Loans}・返せる額 {repay}）");
        // (2) 国内の金が流出＝国庫を0へ・全POPの正の貯蓄を同じ割合で減らし、基礎貨幣＝−与信/2 に置く（与信の残高は動かさない）
        if (w.Treasury.Value > 0) World.Transfer((w, null), (w, new List<Pop>(w.AllPops()), null), w.Treasury.Value, TreasuryCat.OtherOut);   // 国庫の現金は POP へ（移し方の1本・2026-09-30 段4g＝国庫の口は private）
        double loans0 = bank.Loans.Value, pos = 0;
        foreach (var p in w.AllPops()) pos += Math.Max(0, p.Savings.Value);
        double keep = 1.0 - (w.TotalMoney.Value - 0.5 * loans0) / pos;
        Assert.True(keep > 0 && keep < 1, $"前提が崩れた：流出の割合が作れない（{keep}）");
        foreach (var p in w.AllPops()) if (p.Savings.Value > 0) p.SetSavings(p.Savings.Value * (keep));
        double base0 = w.BaseMoney.Value;
        Assert.True(base0 < 0 && Math.Abs(base0 + 0.5 * loans0) <= 1e-6 * loans0, $"前提が崩れた：基礎貨幣が与信の半分の負になっていない（{base0}・与信 {loans0}）");
        double prev = loans0;
        for (int d = 0; d < 3000; d++)
        {
            foreach (var p in w.AllPops())   // 国内の売り上げ＝他のPOPの貯蓄の1%が投資家へ（国内の移転＝基礎貨幣は動かない）
                if (!ReferenceEquals(p, inv) && p.Savings.Value > 0) { double x = 0.01 * p.Savings.Value; p.SetSavings(p.Savings.Value - (x)); inv.SetSavings(inv.Savings.Value + (x)); }
            Day(w);
            Assert.True(bank.Loans.Value <= prev * (1 + 1e-12), $"基礎貨幣が負の国で与信が増えた（{prev} → {bank.Loans}・{d}日目）");
            prev = bank.Loans.Value;
        }
        double recalled = loans0 - bank.Loans.Value;
        Assert.True(recalled > 0, "前提が崩れた：負の国で回収が起きていない");
        Assert.True(recalled <= repay * (1 + 1e-9), $"負の国の回収が返せる額を越えて続いた（回収の総額 {recalled} ＞ 返せる額 {repay}）");
        Assert.True(Math.Abs(w.BaseMoney.Value - base0) <= 1e-9 * loans0, $"回収で基礎貨幣が動いた（{base0} → {w.BaseMoney}）＝回収は負の基礎貨幣を戻さない、の前提が崩れた");
    }

    /// <summary>★基礎貨幣が負の国の返せない与信の償却（作者裁定 E6 の2歩目・2026-09-28）。月末の損失の負債化（<c>Simulation.BackstopNegativeSavings</c>）の後、
    /// 基礎貨幣が負の国は負の分だけ銀行の与信を償却し、同じ額を非常発行として台帳に積む（貨幣を作る・2026-09-29 段2-2 から非常発行の行はこの償却だけ）。
    /// 盤面＝英を1四半期温めた世界。与信 L を信用創造の対（与信と預金）で置き、国庫と国内の貨幣（貯蓄の和）を置き、1つの POP に今月の損失を置く。
    /// (1) 負の国（基礎貨幣＝0.5L−0.1L−L＝−0.6L）：償却の後、基礎貨幣は0・貯蓄は全て非負・与信＝L＋損失−償却・非常発行の増分＝償却＝負の分・保存量の検査（MoneyConservation）が通る
    /// (2) 対照＝正の国（2L−0.1L−L＝0.9L）：損失は与信になるだけ（非常発行は動かない・基礎貨幣も動かない）
    /// (3) 与信より深い負（国庫 −0.5L）：償却は与信の残高まで（与信0・非常発行＝与信）・国庫の負の分は残る（与信でない）・保存量の検査が通る。</summary>
    [Fact]
    public void 基礎貨幣が負の国は負の分だけ与信を償却し非常発行として台帳に積む()
    {
        static (Simulation Sim, double L, double Loss) Setup(double moneyShare, double treasuryShare)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            sim.Run(1);
            var w = sim.World; var bank = w.Bank!; var inv = w.Region.Pops[w.Region.InvestorPopIndex];
            double L = (0.5 * w.TotalMoney).Value;
            double d = L - bank.Loans.Value; bank.Loans += new MoneyStock(d); inv.SetSavings(inv.Savings.Value + (d));   // 信用創造の対（与信と預金）
            w.SetTreasuryRaw(treasuryShare * L);                               // 盤面づくり（保存量の検査はこの後で取る）
            var victim = w.Region.Pops[0]; victim.SetSavings(0);
            double pos = 0; foreach (var p in w.AllPops()) pos += Math.Max(0, p.Savings.Value);
            foreach (var p in w.AllPops()) p.SetSavings(p.Savings.Value > 0 ? p.Savings.Value * (moneyShare * L / pos) : 0);   // 国内の貨幣＝moneyShare×L
            double loss = 0.1 * L; victim.SetSavings(-loss);                   // 今月の損失
            Assert.True(w.Bank!.HouseholdLoans.Value == 0, "前提が崩れた：家計の与信が0でない（休眠の器が点いている）");
            return (sim, L, loss);
        }

        // (1) 負の国
        {
            var (sim, L, loss) = Setup(moneyShare: 0.5, treasuryShare: 0);
            var w = sim.World; var bank = w.Bank!;
            double b0 = w.BaseMoney.Value, loans0 = bank.Loans.Value, fiat0 = w.CumulativeFiatIssued;
            Assert.True(Math.Abs(b0 + 0.6 * L) <= 1e-9 * L, $"前提が崩れた：基礎貨幣が −0.6L でない（{b0}・L {L}）");
            var mc = new MoneyConservation(w);
            sim.BackstopNegativeSavings();
            foreach (var p in w.AllPops()) Assert.True(p.Savings.Value >= 0, $"償却の後に負の貯蓄が残った（{p.Savings}）");
            Assert.True(Math.Abs(w.BaseMoney.Value) <= 1e-9 * L, $"負の国の基礎貨幣が償却で0へ戻らない（{b0} → {w.BaseMoney}）");
            Assert.True(Math.Abs((w.CumulativeFiatIssued - fiat0) + b0) <= 1e-9 * L, $"非常発行の増分が負の分と一致しない（{w.CumulativeFiatIssued - fiat0} 対 {-b0}）");
            Assert.True(Math.Abs(bank.Loans.Value - (loans0 + loss + b0)) <= 1e-9 * L, $"与信＝L＋損失−償却 にならない（{bank.Loans} 対 {loans0 + loss + b0}）");
            mc.Check(w);   // 世界の台帳は非常発行で説明できる（保存量 D＝基礎貨幣−交易−鋳造−不換 が動かない）
        }
        // (2) 対照＝正の国
        {
            var (sim, L, loss) = Setup(moneyShare: 2.0, treasuryShare: 0);
            var w = sim.World; var bank = w.Bank!;
            double b0 = w.BaseMoney.Value, loans0 = bank.Loans.Value, fiat0 = w.CumulativeFiatIssued;
            Assert.True(b0 > 0.8 * L, $"前提が崩れた：対照の基礎貨幣が正でない（{b0}）");
            sim.BackstopNegativeSavings();
            Assert.Equal(fiat0, w.CumulativeFiatIssued);
            Assert.True(Math.Abs(bank.Loans.Value - (loans0 + loss)) <= 1e-12 * L, $"正の国で損失が与信にならない（{bank.Loans} 対 {loans0 + loss}）");
            Assert.True(Math.Abs(w.BaseMoney.Value - b0) <= 1e-9 * L, $"正の国で基礎貨幣が動いた（{b0} → {w.BaseMoney}）");
        }
        // (3) 与信より深い負：基礎貨幣＝0.2L−0.1L−0.5L−L＝−1.4L・損失の後の与信 1.1L
        {
            var (sim, L, loss) = Setup(moneyShare: 0.2, treasuryShare: -0.5);
            var w = sim.World; var bank = w.Bank!;
            double b0 = w.BaseMoney.Value, loans0 = bank.Loans.Value, fiat0 = w.CumulativeFiatIssued;
            Assert.True(-b0 > loans0 + loss, $"前提が崩れた：負の分が与信より浅い（{b0}・与信 {loans0 + loss}）");
            var mc = new MoneyConservation(w);
            sim.BackstopNegativeSavings();
            Assert.Equal(0.0, bank.Loans.Value);
            Assert.True(Math.Abs((w.CumulativeFiatIssued - fiat0) - (loans0 + loss)) <= 1e-9 * L, $"償却が与信の残高と一致しない（非常発行 {w.CumulativeFiatIssued - fiat0}・与信 {loans0 + loss}）");
            Assert.True(Math.Abs(w.BaseMoney.Value - (-0.3 * L)) <= 1e-9 * L, $"国庫の負の分が残らない（{w.BaseMoney} 対 {-0.3 * L}）");
            mc.Check(w);
        }
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

    /// <summary>★投資家のいない首都の与信（支払いの一元化 段2-2・A1＋E1・2026-09-29）：全POPへ預金の比で注入・回収する枝（旧 InjectMoney＝按分の本体 World.Distribute へ畳んだ）。
    /// (1) 預金者不在（全POPの貯蓄≦0）なら、与信を広げたい（目標＞残高）ときでも与信は立たない＝基礎貨幣は動かない
    ///     （旧＝与信の残高を先に動かし、InjectMoney が早期 return しても戻さなかった＝基礎貨幣が delta だけ動いた湧き口）。
    /// (2) 回収は預金の合計まで（投資家の枝と同じ）＝負の貯蓄を作らない・与信は回収した額だけ減る・基礎貨幣は動かない。
    /// 盤面＝英を1四半期温めた世界の首都から投資家の添字を外す。⚠突然変異「配れなかった額も与信に積む」「回収を預金で切らない」で赤。</summary>
    [Fact]
    public void 投資家のいない首都では受け手がいるときだけ与信し回収は預金まで()
    {
        static World Board()
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            sim.Run(1);
            var w = sim.World;
            w.Region.InvestorPopIndex = -1;   // 投資家のいない首都（全POPへ注入・回収する枝）
            return w;
        }
        // (1) 預金者不在
        {
            var w = Board(); var bank = w.Bank!;
            foreach (var p in w.AllPops()) p.SetSavings(0);
            bank.Loans = new MoneyStock(0); bank.Confidence = 1.0;
            w.SetTreasuryRaw(1e6);   // 準備（基礎貨幣＝国庫）＞0＝与信の目標＞0
            double base0 = w.BaseMoney.Value;
            PriceOfBread.SimCore.Credit.Step(w, w.Coef);
            Assert.Equal(0.0, bank.Loans.Value);
            Assert.Equal(base0, w.BaseMoney.Value);
        }
        // (2) 回収は預金の合計まで
        {
            var w = Board(); var bank = w.Bank!;
            double held = 0; foreach (var p in w.AllPops()) { p.SetSavings(1.0); held += 1.0; }
            bank.Loans = new MoneyStock(1e9); bank.Confidence = 0;   // 目標≪残高＝大きく回収したい
            double base0 = w.BaseMoney.Value;
            PriceOfBread.SimCore.Credit.Step(w, w.Coef);
            foreach (var p in w.AllPops()) Assert.True(p.Savings.Value >= -1e-12, $"回収で負の貯蓄を作った（{p.Savings}）");
            Assert.True(Math.Abs(1e9 - bank.Loans.Value - held) <= 1e-6, $"与信の減り {1e9 - bank.Loans.Value} ≠ 預金の合計 {held}");
            Assert.True(Math.Abs(w.BaseMoney.Value - base0) <= 1e-6, $"回収で基礎貨幣が動いた（{base0} → {w.BaseMoney}）");
        }
    }

    /// <summary>★全ての世界に銀行がある（支払いの一元化 段2-2・B7＋A8・作者裁定 Q11・2026-09-29）＝損失の受け皿は与信の1本。
    /// 旧＝銀行の無い試験世界では州の最富裕POP→正の貯蓄の全員から取って埋め（他人から取る）、残りを非常発行の名目で台帳に積んだ（漏れを保存の検査から隠した）。
    /// 盤面＝最小の試験世界（旧＝銀行なし）で1つの POP に損失を置き、月末の底打ちを1回。⚠突然変異「銀行の無い世界の底打ちへ戻す」で赤。</summary>
    [Fact]
    public void 全ての世界に銀行があり損失は与信だけが受け皿()
    {
        var sim = new Simulation(new Scenario { Seed = 7, Kind = ScenarioKind.AgrarianToUrban }, new NullAgent());
        var w = sim.World;
        Assert.NotNull(w.Bank);
        var pops = w.Region.Pops;
        Assert.True(pops.Count >= 2, "前提が崩れた：POP が1つしかない");
        pops[0].SetSavings(-100);
        var others = new double[pops.Count]; for (int i = 1; i < pops.Count; i++) others[i] = pops[i].Savings.Value;
        double loans0 = (w.Bank!.Loans).Value, fiat0 = w.CumulativeFiatIssued, base0 = w.BaseMoney.Value + 100;   // 損失を置く前の基礎貨幣
        var mc = new MoneyConservation(w);
        sim.BackstopNegativeSavings();
        Assert.Equal(0.0, pops[0].Savings.Value);
        for (int i = 1; i < pops.Count; i++) Assert.Equal(others[i], pops[i].Savings.Value);   // 他人から取らない
        Assert.Equal(loans0 + 100, w.Bank.Loans.Value);
        Assert.Equal(fiat0, w.CumulativeFiatIssued);                                     // 基礎貨幣が正の国は非常発行しない
        Assert.True(base0 > 0, "前提が崩れた：基礎貨幣が正でない");
        mc.Check(w);
    }

    /// <summary>★信用の旬次（2026-09-30 高速化の段 S3c・近似）：Credit.Step(…, days: h) は信頼を h 日ぶんの緩和の閉じた形 1−(1−速さ)^h で目標へ寄せる
    /// ＝信頼の目標が旬の中で動かない盤面（過熱の脆さ・退蔵のパニック・負債デフレを切る＝目標が与信の残高と信頼そのものに依らない）では、日次を h 回まわした信頼と
    /// 丸めの差だけで一致する（目標より上＝崩れる速さ・下＝育つ速さの両方）。与信の残高は目標（信頼×上限）が日ごとに動くので一致しない＝近いことだけを見る。
    /// ⚠突然変異「h 日ぶんを1歩の外挿 h×速さで寄せる」で赤（差は h(h−1)/2×速さ²×隔たり）。</summary>
    [Fact]
    public void 信用の旬次は目標が動かない盤面で日次をh回まわした信頼と一致する()
    {
        const int h = 10;
        int down = 0, up = 0;
        foreach (double conf0 in new[] { 1.0, 0.05 })
        {
            World Mk()
            {
                var w = Board(Rich, 0);
                w.Explain = null;
                w.Coef.OverheatFragility = 0; w.Coef.HoardPanicWeight = 0; w.Coef.DebtDeflationWeight = 0;
                w.Coef.ConfidencePivot = 2.0; w.Coef.PanicSensitivity = 0.25;   // 目標＝1−0.25×(2−充足)∈[0.5, 0.75]＝1.0 は崩れる側・0.05 は育つ側
                w.Bank!.Confidence = conf0;
                return w;
            }
            var a = Mk(); var b = Mk();
            PriceOfBread.SimCore.Credit.Step(a, a.Coef, null, h);
            for (int i = 0; i < h; i++) PriceOfBread.SimCore.Credit.Step(b, b.Coef, null, 1);
            double ca = a.Bank!.Confidence, cb = b.Bank!.Confidence;
            if (cb < conf0) down++; else if (cb > conf0) up++;
            Assert.True(Math.Abs(ca - cb) <= 1e-12, $"信頼が日次を h 回まわした値と違う（始め {conf0}：旬次 {ca} 対 日次 {cb}）");
            Assert.True(Math.Abs((a.Bank.Loans - b.Bank.Loans).Value) <= 1e-2 * Math.Max(1.0, Math.Abs(b.Bank.Loans.Value)), $"与信の残高が日次から離れすぎた（{a.Bank.Loans} 対 {b.Bank.Loans}）");   // 目標の信頼は旬の台形（終わりの値だけなら 1.7%・台形で 0.2%）
        }
        Assert.True(down == 1 && up == 1, $"前提が崩れた：崩れる側と育つ側の両方を通っていない（下 {down}・上 {up}）");
    }
}
