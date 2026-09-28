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
        w.Coef.DevaluationCapFrac = 0;   // 切り下げの余地を閉じる（2026-09-28 から切り下げは払える額の側＝仏は不換で、余地が払えない分を埋めて r＝1 になる・切り下げは下の番人が見る）
        for (int d = 0; d < 28; d++) sim.StepDay();
        sim.StepDayOpen(); sim.StepDayDaily();   // 日29の日次まで＝次の締め（月次決算）の直前＝r を決める時点と同じ国庫と見込み
        double payable = System.Math.Max(0, w.Treasury) + w.LastRevenue;
        double spent0 = SpentOf(w);
        double intL0 = w.TreasuryLedger[(int)TreasuryCat.Interest], admL0 = w.TreasuryLedger[(int)TreasuryCat.Admin], debt0 = w.GovDebt, bor0 = w.TreasuryLedger[(int)TreasuryCat.Borrow];
        sim.StepDayClose(); w.FlushLog();        // 月次決算（r を決める→予算→精算→財政）
        double r = w.FiscalFundingRatio;
        Assert.True(r > 0 && r < 1, $"前提：r が (0,1) に無い（{r:F4}）＝この盤面では規則を測れない");
        Assert.Equal(1.0 - r, UtilityAgent.FiscalStressOf(w), 12);   // AI の逼迫度＝1−r（R4）
        // 利子：払った額＝請求×r・残りは元本へ（国内の持ち手がいる＝繰り入れる）
        double intPaid = intL0 - w.TreasuryLedger[(int)TreasuryCat.Interest];
        Assert.True(w.LastInterest > 0, "前提：利子の請求が0");
        Assert.True(System.Math.Abs(intPaid - r * w.LastInterest) <= 1e-9 * w.LastInterest, $"利子の支払い {intPaid:F6} ≠ r×請求 {r * w.LastInterest:F6}（r={r:F4}）");
        double borrowed = w.TreasuryLedger[(int)TreasuryCat.Borrow] - bor0;   // r＜1 の月はエンジンが借りる（2026-09-28）＝元本の増分から除く
        Assert.True(System.Math.Abs((w.GovDebt - debt0 - borrowed) - (1 - r) * w.LastInterest) <= 1e-9 * System.Math.Max(1.0, w.GovDebt), $"元本の増分（借入を除く）{w.GovDebt - debt0 - borrowed:F6} ≠ 利子の未払い {(1 - r) * w.LastInterest:F6}");
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

    /// <summary>借入は r＜1 の月だけ・減債は r＝1 の月の余剰（spare＞0）からだけ（Fiscal の2つの門）。
    /// 借入は予約なしでエンジンが月に1件（2026-09-28 作者裁定「借入は自動化・借金ボタンは不要」）＝r＜1 の月が続けば毎月1件ずつ借りる。</summary>
    [Fact]
    public void 借入はrが1未満の月だけ減債はrが1の月の余剰からだけ()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(19), new NullAgent());
        sim.Run(4);
        var w = sim.World; var c = w.Coef;
        int bo = (int)TreasuryCat.Borrow, rp = (int)TreasuryCat.DebtRepay;
        // (a) r＝1：借りない
        w.FiscalFundingRatio = 1.0;
        double b0 = w.TreasuryLedger[bo], st0 = w.BorrowStacks;
        Fiscal.Step(w, c, 0);
        Assert.Equal(b0, w.TreasuryLedger[bo]);
        Assert.Equal(st0, w.BorrowStacks);
        // (b) r＜1：誰も予約しないのにエンジンが借りる（月に1件＝1スタック）
        w.FiscalFundingRatio = 0.9;
        Fiscal.Step(w, c, 0);
        Assert.True(w.TreasuryLedger[bo] > b0, "r＜1 の月にエンジンが借りない");
        Assert.Equal(st0 + 1.0, w.BorrowStacks, 12);
        // (b') 次の r＜1 の月も同じ1行でまた1件
        double b1 = w.TreasuryLedger[bo];
        w.FiscalFundingRatio = 0.9;
        Fiscal.Step(w, c, 0);
        Assert.True(w.TreasuryLedger[bo] > b1, "続く r＜1 の月に借りない");
        Assert.Equal(st0 + 2.0, w.BorrowStacks, 12);
        // (c) r＜1：余剰を渡しても返さない
        double d0 = w.TreasuryLedger[rp];
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

    /// <summary>★借入はエンジンの1行だけ（2026-09-28 作者裁定「借入は自動化・借金ボタンは不要」）＝AI とプレイヤーの経路が無い。
    /// ①借りる主体のいない世界（NullAgent）でも、r＜1 の月の月次決算の中で借りる　②借入の書き手（国庫の台帳の区分 Borrow への Pay）は SimCore の Fiscal.cs の1か所だけ
    /// ＝SelfPlay（AI）と App（画面）には無い・Fiscal.Step を呼ぶのは月次決算（Simulation.cs）だけ。
    /// 突然変異：予約の門を戻す（エンジンが自分では借りない）→①が赤／AI か画面に借りる口を足す→②が赤。</summary>
    [Fact]
    public void 借入はエンジンの1行だけでAIとプレイヤーの経路が無い()
    {
        // ① 借りる主体のいない世界の月次決算で借りる（盤面は上の「国家の支払い率は全ての行に同じrで…」と同じ＝r＜1 の月を作る）
        var sim = new Simulation(NationConfig.France().ToScenario(19), new NullAgent());
        sim.Run(4);
        sim.StepDay();
        var w = sim.World;
        double drain = w.Treasury + 3 * System.Math.Max(1.0, w.LastRevenue);
        w.Pay(TreasuryCat.OtherOut, -drain); w.Regions[0].Pops[0].Savings += drain;
        w.AdminSpending = 3.0;
        w.Coef.DevaluationCapFrac = 0;   // 切り下げの余地を閉じる（仏は不換＝余地が払えない分を埋めて r＝1 になる・切り下げは下の番人が見る）
        double b0 = w.TreasuryLedger[(int)TreasuryCat.Borrow], st0 = w.BorrowStacks;
        Assert.True(st0 < w.Coef.MaxBorrowStacks && w.BorrowCooldown <= 0, "前提：借入の上限か冷却中");
        for (int d = 0; d < 29; d++) sim.StepDay();   // 月次決算を1回（r を決める→…→財政）
        Assert.True(w.FiscalFundingRatio < 1, $"前提：r＜1 の月になっていない（{w.FiscalFundingRatio:F4}）");
        Assert.True(w.TreasuryLedger[(int)TreasuryCat.Borrow] > b0, "借りる主体のいない世界で r＜1 の月にエンジンが借りない");
        Assert.Equal(st0 + 1.0, w.BorrowStacks, 12);

        // ② 借入の書き手は Fiscal.cs の1か所だけ・Fiscal.Step を呼ぶのは Simulation.cs だけ（SimCore と App を走査・コメントは数えない）
        string root = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(ThisFile()))!;
        var borrowSites = new System.Collections.Generic.List<string>();
        var stepCallers = new System.Collections.Generic.List<string>();
        char sep = System.IO.Path.DirectorySeparatorChar;
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.App" })
            foreach (var f in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(root, dir), "*.cs", System.IO.SearchOption.AllDirectories))
            {
                if (f.Contains($"{sep}obj{sep}") || f.Contains($"{sep}bin{sep}")) continue;
                foreach (var line in System.IO.File.ReadLines(f))
                {
                    var code = line; int k = code.IndexOf("//", System.StringComparison.Ordinal); if (k >= 0) code = code[..k];
                    if (code.Contains("Pay(TreasuryCat.Borrow")) borrowSites.Add(System.IO.Path.GetFileName(f));   // 国庫へ起債の金を入れる口（読み手＝台帳の検査や表示の名前は数えない）
                    if (code.Contains("Fiscal.Step(")) stepCallers.Add(System.IO.Path.GetFileName(f));
                }
            }
        Assert.Equal(new[] { "Fiscal.cs" }, borrowSites.ToArray());
        Assert.Equal(new[] { "Simulation.cs" }, stepCallers.ToArray());
    }

    private static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string self = "") => self;

    /// <summary>★切り下げ（インフレ税）は払える額の側（2026-09-28・(87) の申し送り iii・作者裁定 F4「不足は量を減らすか未払いの記録＝他人から取らない」・思想⑨）。
    /// 双子＝同じ盤面の r＜1 の月（兌換 0.5）で、F は切り下げの余地あり・G は切り下げの上限0（DevaluationCapFrac＝0）。
    /// ①F の r ＝ (払える額＋切り下げ) ÷ 請求＝取った金はその月の支払いに使われる（請求は G の r ＝ 払える額 ÷ 請求 から戻す）
    /// ②切り下げの額＝余地の規則 (1−兌換)×（請求−払える額−min(0,国庫)）そのもの＝払えなかった請求と同じ額を別に取らない。
    /// 突然変異：切り下げを r の後へ戻す（段2-1 の形＝F の r が G と同じ）→①が赤／r に入れた上に月末でも取る（二重）→②が赤。</summary>
    [Fact]
    public void 切り下げは払える額に入り同じ月の支払いに使われ払えなかった請求と同じ額を別に取らない()
    {
        const double conv = 0.5;
        (World w, double payable, double overdraft, double dev) Month(bool capZero)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(19), new NullAgent());
            sim.Run(4);
            sim.StepDay();
            var w = sim.World;
            double t0 = w.Treasury;
            w.Pay(TreasuryCat.OtherOut, -t0); w.Regions[0].Pops[0].Savings += t0;   // 国庫を0へ（現金は1つの POP へ移す＝保存則を割らない）
            w.AdminSpending = 3.0;                                                    // 行政の予算が歳入を超える＝r＜1 の月を作る
            if (capZero) w.Coef.DevaluationCapFrac = 0;
            for (int d = 0; d < 28; d++) sim.StepDay();
            sim.StepDayOpen(); sim.StepDayDaily();   // 締めの直前＝r を決める時点と同じ国庫と見込み
            w.Convertibility = conv;                 // 部分兌換＝余地は払えない分の半分（月の上限 DevaluationCapFrac の手前）
            double payable = System.Math.Max(0, w.Treasury) + w.LastRevenue, overdraft = System.Math.Min(0, w.Treasury);
            double dv0 = w.TreasuryLedger[(int)TreasuryCat.Devalue];
            sim.StepDayClose(); w.FlushLog();        // 月次決算（切り下げ→r→予算→精算→財政）
            return (w, payable, overdraft, w.TreasuryLedger[(int)TreasuryCat.Devalue] - dv0);
        }
        var (wf, pf, of, dev) = Month(false);
        var (wg, pg, og, devG) = Month(true);
        Assert.NotSame(wf.Coef, wg.Coef);
        Assert.NotEqual(MonetaryStandardLaw.Silver, wf.MonetaryLaw);   // 前提：銀の段なら余地の式が改鋳の窓になる（仏の開幕は不換・NullAgent は銀へ上げない）
        Assert.Equal(pg, pf); Assert.Equal(og, of);   // 双子＝締めの直前まで同じ
        double rG = wg.FiscalFundingRatio, rF = wf.FiscalFundingRatio;
        Assert.True(rG > 0 && rG < 1, $"前提：G の r が (0,1) に無い（{rG:F4}）");
        Assert.Equal(0.0, devG);
        Assert.True(dev > 0, "前提：F が切り下げていない");
        double claims = pg / rG;   // G は切り下げなし＝r＝払える額÷請求
        // ② 額＝余地の規則そのもの（月の上限が効いていない盤面）
        double room = (1.0 - conv) * System.Math.Max(0, claims - pf - of);
        Assert.True(System.Math.Abs(dev - room) <= 1e-6 * room, $"切り下げ {dev:F4} ≠ 余地 {room:F4}（(1−兌換)×払えない請求）");
        // ① r は切り下げを払える額に入れて決まる
        double expect = System.Math.Min(1.0, (pf + dev) / claims);
        Assert.True(System.Math.Abs(rF - expect) <= 1e-9, $"r {rF:F6} ≠ (払える額＋切り下げ)÷請求 {expect:F6}（G の r {rG:F6}）");
        Assert.True(rF > rG, "切り下げた金がその月の支払いに使われていない");
    }
}
