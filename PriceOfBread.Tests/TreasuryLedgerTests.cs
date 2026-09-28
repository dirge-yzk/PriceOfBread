using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 国庫の台帳の番人（2026-08-13・作者指示「国庫に触るものは全て財政タブに入れる」）。
///
/// 症状＝ヘッダーの収支（国庫の実績変化）と財政タブの収支（経常予算の予想）が一致しなかった。
/// 行を1本ずつ足す方式では厳密一致が原理的に達成できない（カデンスが違う／記帳漏れが検出できない）ので、
/// **国庫を動かせるのは <see cref="World.Pay"/> だけ**にして（setter は private）台帳を通した。
///
/// ここで固定するのは1つだけ：**Σ台帳 ＝ 国庫 − 初期国庫**。
/// 新しい書き手を足したときに <c>Pay</c> を通し忘れれば、そもそもコンパイルが通らない。
/// それでも通り抜ける経路が2つある＝①<see cref="World.SetTreasuryRaw"/> の誤用 ②**World クラス自身の内部からの書き込み**
/// （private setter はクラス内からは見える）。②は実際に2件すり抜け（工場建設の日次労務費・国営工場の払い下げ代金）、
/// このテストが日本で 872,258 対 800,866 の差として捕まえた。型システムと番人の**両方**が要る理由。
/// </summary>
public class TreasuryLedgerTests
{
    private static double LedgerSum(World w)
    {
        double s = 0;
        foreach (var v in w.TreasuryLedger) s += v;
        return s;
    }

    /// <summary>単一国（イギリス・240四半期）で台帳が国庫と一致する。</summary>
    [Fact]
    public void 単一国で台帳の合計は国庫の変化に一致する()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(19), new NullAgent());
        double start = sim.World.Treasury;
        Assert.Equal(0.0, LedgerSum(sim.World));   // 生成直後＝1件も動いていない（初期国庫はフローでない）
        sim.Run(240);
        double moved = sim.World.Treasury - start;
        // 相対許容＝浮動小数の累積誤差のみ（240四半期ぶんの加算の桁落ち）。1e-6 は取引額に対して十分に厳しい。
        double scale = System.Math.Max(1.0, System.Math.Abs(moved));
        Assert.True(System.Math.Abs(LedgerSum(sim.World) - moved) / scale < 1e-6,
            $"台帳 {LedgerSum(sim.World):F3} と国庫の変化 {moved:F3} が一致しない＝Pay を通さない書き手がある");
    }

    /// <summary>多国（全53カ国・120四半期）でも全国で一致する。外交層（戦費・拿捕・貢納・賠償）は
    /// Power 側の財布を触るので、同期窓の内側で動いた分が落ちないことをここで担保する。</summary>
    [Fact]
    public void 多国でも全ての国で台帳の合計は国庫の変化に一致する()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        var start = new System.Collections.Generic.Dictionary<string, double>();
        foreach (var n in sim.Nations) start[n.World.SelfPower!.Name] = n.World.Treasury;
        for (int q = 0; q < 120; q++) sim.StepQuarter();

        int checkedCount = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } sp || !start.TryGetValue(sp.Name, out double s0)) continue;   // 途中で建国した国は基準が無い
            double moved = w.Treasury - s0;
            double scale = System.Math.Max(1.0, System.Math.Abs(moved));
            Assert.True(System.Math.Abs(LedgerSum(w) - moved) / scale < 1e-6,
                $"{sp.Name}：台帳 {LedgerSum(w):F3} と国庫の変化 {moved:F3} が一致しない");
            checkedCount++;
        }
        Assert.True(checkedCount > 40, $"検査した国が少なすぎる（{checkedCount}）＝ハーネスが空回りしている");
    }

    /// <summary>台帳が**実際に使われている**（全部ゼロのまま通っていない）。
    /// 何もしない番人は必ず緑になるので、主要な区分に値が立つことを確かめる。</summary>
    [Fact]
    public void 主要な区分に値が立つ()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 40; q++) sim.StepQuarter();
        var w = sim.Nations[0].World;
        foreach (var cat in new[] { TreasuryCat.Tax, TreasuryCat.Admin, TreasuryCat.Army })   // 2026-09-28 段2-1：国家の調達は区分つきで精算＝旧の OtherOut は軍需（Army）へ
            Assert.True(System.Math.Abs(w.TreasuryLedger[(int)cat]) > 1e-9,
                $"{cat} の台帳が1度も動いていない＝記帳の配線が切れている");
    }

    // ── ★国家の支払い率 r（作者裁定 F3・Q1〜Q3・2026-09-28 支払いの一元化 段2-1＝計画 §10-12-3-3・§10-12-8-2）──────────────
    //   規則＝月初に全ての国家の支払いの請求を集め、r＝min(1, 払える額÷Σ請求)（払える額＝max(0,国庫)＋LastRevenue）。
    //   全ての行に同じ r・優先順位なし・借入は r＜1 の月だけ・減債は r＝1 の月の余剰から・利子の未払いは元本へ。

    /// <summary>国庫が空の月に、利子・維持費・兵士の給与が**同じ r** で払われる（優先順位がない）。利子の未払いは元本へ繰り入れる。
    /// 突然変異：1つの行だけ r を掛けない・利子を先に満額払う（旧の滝）→ 赤。</summary>
    [Fact]
    public void 国家の支払い率は全ての行に同じrで掛かり利子の未払いは元本へ()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(19), new NullAgent());   // 仏＝開幕の国債あり（保有台帳も起票済み）
        sim.Run(4);
        sim.StepDay();   // 四半期首（政策の反映）を先に済ませる＝以後の29日で月次決算が1回だけ走る
        var w = sim.World;
        Assert.True(w.GovDebt > 0, "前提：国債が無い");
        // 国庫を空に＝払える額は当月の歳入の見込みだけ。国庫の現金は1つの POP へ移す（保存則を割らない＝台帳の区分 OtherOut で記帳）。
        // 行政費を歳入の3倍へ＝行政の予算（行政費×AdminBudgetFrac×歳入）が歳入を超える＝r＜1 の月を作る（四半期首の政策の反映の後なので月内は保たれる）
        double drain = w.Treasury + 3 * System.Math.Max(1.0, w.LastRevenue);
        w.Pay(TreasuryCat.OtherOut, -drain); w.Regions[0].Pops[0].Savings += drain;
        w.AdminSpending = 3.0;
        for (int d = 0; d < 28; d++) sim.StepDay();
        sim.StepDayOpen(); sim.StepDayDaily();   // 日29の日次まで＝次の締め（月次決算）の直前＝r を決める時点と同じ国庫と見込み
        double payable = System.Math.Max(0, w.Treasury) + w.LastRevenue;
        double spent0 = SpentOf(w);
        double intL0 = w.TreasuryLedger[(int)TreasuryCat.Interest], admL0 = w.TreasuryLedger[(int)TreasuryCat.Admin], debt0 = w.GovDebt;
        sim.StepDayClose(); w.FlushLog();        // 月次決算（r を決める→予算→精算→財政）
        double r = w.FiscalFundingRatio;
        Assert.True(r > 0 && r < 1, $"前提：r が (0,1) に無い（{r:F4}）＝この盤面では規則を測れない");
        Assert.Equal(1.0 - r, UtilityAgent.FiscalStressOf(w), 12);   // AI の逼迫度＝1−r（R4）
        // 利子：払った額＝請求×r・残りは元本へ（国内の持ち手がいる＝繰り入れる）
        double intPaid = intL0 - w.TreasuryLedger[(int)TreasuryCat.Interest];
        Assert.True(w.LastInterest > 0, "前提：利子の請求が0");
        Assert.True(System.Math.Abs(intPaid - r * w.LastInterest) <= 1e-9 * w.LastInterest, $"利子の支払い {intPaid:F6} ≠ r×請求 {r * w.LastInterest:F6}（r={r:F4}）");
        Assert.True(System.Math.Abs((w.GovDebt - debt0) - (1 - r) * w.LastInterest) <= 1e-9 * System.Math.Max(1.0, w.GovDebt), $"元本の増分 {w.GovDebt - debt0:F6} ≠ 利子の未払い {(1 - r) * w.LastInterest:F6}");
        Assert.True(BondLedger.Balanced(w, HeldSum(w)), "繰り入れの後に保有台帳（Σ保有＝国内元本）が割れた");
        // 維持費：区分 Admin の減り＝維持費×r＋行政の調達（StateSpent の Admin）
        double stateAdmin = 0; foreach (var rg in w.Regions) if (rg.State == RegionState.Active) stateAdmin += rg.StateSpent[(int)TreasuryCat.Admin];
        double upkeepPaid = admL0 - w.TreasuryLedger[(int)TreasuryCat.Admin] - stateAdmin;
        Assert.True(w.LastUpkeep > 0, "前提：維持費の請求が0");
        Assert.True(System.Math.Abs(upkeepPaid - r * w.LastUpkeep) <= 1e-6 * w.LastUpkeep, $"維持費の支払い {upkeepPaid:F6} ≠ r×請求 {r * w.LastUpkeep:F6}");
        // 兵士の給与：予算＝請求×r（旧＝州ごとの min(国庫)＝先着）
        double soldierBudget = 0; foreach (var rg in w.Regions) if (rg.State == RegionState.Active) soldierBudget += rg.SoldierPayBudget;
        Assert.True(w.LastSoldierPay > 0, "前提：兵士の給与の請求が0");
        Assert.True(System.Math.Abs(soldierBudget - r * w.LastSoldierPay) <= 1e-9 * w.LastSoldierPay, $"兵士の給与の予算 {soldierBudget:F6} ≠ r×請求 {r * w.LastSoldierPay:F6}");
        // r＜1 の月は国家の支払いの合計≦払える額（月初の請求の見込みは先月の請求＝その誤差の分だけ相対 2% まで許す）
        double spent = SpentOf(w) - spent0;
        Assert.True(spent <= payable * 1.02, $"r＜1 の月の国家の支払い {spent:F3} が払える額 {payable:F3} を超えた（{spent / payable:P2}）");
        Assert.True(spent > 0, "前提：国家の支払いが立っていない");   // ⚠予算は量の上限＝需要が予算を下回る行（行政）は予算を使い切らない＝実額は払える額より小さくてよい
    }

    /// <summary>国家の支払いの実額＝台帳の支出区分の減り（減債は r＝1 の余剰から＝請求の外・国営の損益は収入と相殺される区分なので除く）。</summary>
    private static double SpentOf(World w)
    {
        double s = 0;
        foreach (var k in new[] { TreasuryCat.Army, TreasuryCat.Navy, TreasuryCat.Admin, TreasuryCat.Education, TreasuryCat.Welfare, TreasuryCat.Pension,
            TreasuryCat.Modernization, TreasuryCat.AntiCorruption, TreasuryCat.Interest, TreasuryCat.FactoryBuild, TreasuryCat.Subsidy, TreasuryCat.Megaproject,
            TreasuryCat.ColonialUpkeep, TreasuryCat.TributeOut, TreasuryCat.WarCost, TreasuryCat.OtherOut })
            s -= w.TreasuryLedger[(int)k];
        return s;
    }

    private static double HeldSum(World w)
    {
        double s = 0; foreach (var rg in w.Regions) foreach (var p in rg.Pops) s += p.BondHoldings;
        return s;
    }

    /// <summary>借入は r＜1 の月だけ・減債は r＝1 の月の余剰（spare＞0）からだけ（Fiscal の2つの門）。</summary>
    [Fact]
    public void 借入はrが1未満の月だけ減債はrが1の月の余剰からだけ()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(19), new NullAgent());
        sim.Run(4);
        var w = sim.World; var c = w.Coef;
        int bo = (int)TreasuryCat.Borrow, rp = (int)TreasuryCat.DebtRepay;
        // (a) r＝1：予約があっても借りない
        w.RequestBorrow(); Assert.Equal(1, w.BorrowQueue);
        w.FiscalFundingRatio = 1.0;
        double b0 = w.TreasuryLedger[bo];
        Fiscal.Step(w, c, 0);
        Assert.Equal(b0, w.TreasuryLedger[bo]);
        Assert.Equal(1, w.BorrowQueue);
        // (b) r＜1：同じ予約が実行される
        w.FiscalFundingRatio = 0.9;
        Fiscal.Step(w, c, 0);
        Assert.True(w.TreasuryLedger[bo] > b0, "r＜1 の月に予約された借入が実行されない");
        Assert.Equal(0, w.BorrowQueue);
        // (c) r＜1：余剰を渡しても返さない
        double d0 = w.TreasuryLedger[rp], g0 = w.GovDebt;
        w.FiscalFundingRatio = 0.9;
        Fiscal.Step(w, c, 1e12);
        Assert.Equal(d0, w.TreasuryLedger[rp]);
        // (d) r＝1 かつ余剰あり：返す（額は余剰まで）
        w.FiscalFundingRatio = 1.0;
        double spare = 0.5 * w.GovDebt * c.BondRepayFrac;
        double d1 = w.TreasuryLedger[rp];
        Fiscal.Step(w, c, spare);
        double repaid = d1 - w.TreasuryLedger[rp];
        Assert.True(repaid > 0, "r＝1 の月の余剰から返済されない");
        Assert.True(repaid <= spare * (1 + 1e-9), $"返済 {repaid:F3} が余剰 {spare:F3} を超えた");
        // (e) r＝1 でも余剰が無ければ返さない
        double d2 = w.TreasuryLedger[rp];
        Fiscal.Step(w, c, -1.0);
        Assert.Equal(d2, w.TreasuryLedger[rp]);
    }
}
