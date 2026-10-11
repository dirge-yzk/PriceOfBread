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
        double start = sim.World.Treasury.Value;
        Assert.Equal(0.0, LedgerSum(sim.World));   // 生成直後＝1件も動いていない（初期国庫はフローでない）
        sim.Run(240);
        double moved = sim.World.Treasury.Value - start;
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
        foreach (var n in sim.Nations) start[n.World.SelfPower!.Name] = n.World.Treasury.Value;
        for (int q = 0; q < 120; q++) sim.StepQuarter();

        int checkedCount = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } sp || !start.TryGetValue(sp.Name, out double s0)) continue;   // 途中で建国した国は基準が無い
            double moved = w.Treasury.Value - s0;
            double scale = System.Math.Max(1.0, System.Math.Abs(moved));
            Assert.True(System.Math.Abs(LedgerSum(w) - moved) / scale < 1e-6,
                $"{sp.Name}：台帳 {LedgerSum(w):F3} と国庫の変化 {moved:F3} が一致しない");
            checkedCount++;
        }
        Assert.True(checkedCount > 40, $"検査した国が少なすぎる（{checkedCount}）＝ハーネスが空回りしている");
    }

    /// <summary>★月の境ごとの帳尻（2026-10-10 (242)・計器に依らない独立の番人）：各月の決算の後で「Δ国庫＝Σ台帳の月の差分」が閉じる。
    /// 旧の2本は終わりに1度だけ（累積）比べる＝月々の食い違いが相殺するか、助走明けの置き直し（台帳を捨てる）の位置で起きたずれは見えない。
    /// (226) の F 行の「国庫の帳尻が合わない・残差が歳入の4割」は、計器の別々の累算器を足し合わせた誤りだった（(230) の計器で台帳に直して 3e-14 で閉じた）＝
    /// 本体は閉じている。ここで月ごとに固定する（計器に依らず、4国の短い走行〔助走の後〕で国庫と台帳を直接読む）。許し＝月の流れの絶対値の和に対し 1e-9。</summary>
    [Fact]
    public void 多国でも月の境ごとに全ての国で国庫の変化は台帳の変化に一致する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
        sim.StartWarmup();
        int n = sim.Nations.Count;
        var prevT = new double[n]; var prevL = new double[n][]; var lastMonth = new long[n];
        for (int i = 0; i < n; i++) { var w = sim.Nations[i].World; prevT[i] = w.Treasury.Value; prevL[i] = (double[])w.TreasuryLedger.Clone(); lastMonth[i] = w.Tick / Calendar.DaysPerMonth; }
        int checkedMonths = 0; double worst = 0;
        for (int day = 0; day < 20 * Calendar.DaysPerQuarter; day++)
        {
            sim.StepDay();
            for (int i = 0; i < n; i++)
            {
                var w = sim.Nations[i].World;
                long m = w.Tick / Calendar.DaysPerMonth;
                if (m == lastMonth[i]) continue;
                lastMonth[i] = m;
                double dT = w.Treasury.Value - prevT[i], dL = 0, flow = 0;
                for (int k = 0; k < w.TreasuryLedger.Length; k++) { double d = w.TreasuryLedger[k] - prevL[i][k]; dL += d; flow += System.Math.Abs(d); }
                double rel = System.Math.Abs(dT - dL) / System.Math.Max(1.0, flow);
                worst = System.Math.Max(worst, rel);
                Assert.True(rel < 1e-9, $"{w.SelfPower!.Name} 月{m}：Δ国庫 {dT:F6} とΣ台帳の差分 {dL:F6} が合わない（相対 {rel:E2}）＝Pay を通さない書き手か、台帳を捨てずに国庫だけ置いた箇所がある");
                prevT[i] = w.Treasury.Value; Array.Copy(w.TreasuryLedger, prevL[i], prevL[i].Length);
                checkedMonths++;
            }
        }
        Assert.True(checkedMonths >= 4 * 55, $"検査した月が少なすぎる（{checkedMonths}）＝ハーネスが空回りしている");
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

    /// <summary>国庫が空の月に、利子・兵士の給与が**同じ r** で払われる（優先順位がない）。利子の未払いは元本へ繰り入れる。区分 Admin の減りは行政の購買だけ（連合維持費は 2026-09-30 (135) に撤去）。
    /// 突然変異：1つの行だけ r を掛けない・利子を先に満額払う（旧の滝）→ 赤。</summary>
    [Fact]
    public void 国家の支払い率は全ての行に同じrで掛かり利子の未払いは元本へ()
    {
        var sim = new Simulation(FranceFiat(), new NullAgent());   // 仏＝開幕の国債あり（保有台帳も起票済み）
        sim.Run(4);
        sim.StepDay();   // 四半期首（政策の反映）を先に済ませる＝以後の29日で月次決算が1回だけ走る
        var w = sim.World;
        Assert.True(w.GovDebt.Value > 0, "前提：国債が無い");
        // 国庫を空に＝払える額は当月の歳入の見込みだけ。国庫の現金は1つの POP へ移す（保存則を割らない＝台帳の区分 OtherOut で記帳）。
        // 行政費を歳入の3倍へ＝行政の予算（行政費×AdminBudgetFrac×歳入）が歳入を超える＝r＜1 の月を作る（四半期首の政策の反映の後なので月内は保たれる）
        double drain = w.Treasury.Value + 3 * System.Math.Max(1.0, w.LastRevenuePerMonth.Value);
        World.Transfer((w, null), (w, [w.Regions[0].Pops[0]], [1.0]), drain, TreasuryCat.OtherOut);   // 移し方の1本（2026-09-30 段4g＝国庫の口は private）
        w.AdminSpending = 3.0;
        w.Coef.DevaluationCapFrac = 0;   // 切り下げの余地を閉じる（2026-09-28 から切り下げは払える額の側＝仏は不換で、余地が払えない分を埋めて r＝1 になる・切り下げは下の番人が見る）
        w.BorrowCooldownMonths = 1;            // 借りられない月（2026-09-28 (b) から借入は月初の払える額の側＝借りられる月は借りて r＝1 になる・借入と切り下げの順は下の番人が見る）
        for (int d = 0; d < 28; d++) sim.StepDay();
        sim.StepDayOpen(); sim.StepDayDaily();   // 日29の日次まで＝次の締め（月次決算）の直前＝r を決める時点と同じ国庫と見込み
        double payable = System.Math.Max(0, w.Treasury.Value) + w.LastRevenuePerMonth.Value;
        double spent0 = SpentOf(w);
        double intL0 = w.TreasuryLedger[(int)TreasuryCat.Interest], admL0 = w.TreasuryLedger[(int)TreasuryCat.Admin], debt0 = w.GovDebt.Value, bor0 = w.TreasuryLedger[(int)TreasuryCat.Borrow];
        sim.StepDayClose(); w.FlushLog();        // 月次決算（r を決める→予算→精算→財政）
        double r = w.FiscalFundingRatio.Value;
        Assert.True(r > 0 && r < 1, $"前提：r が (0,1) に無い（{r:F4}）＝この盤面では規則を測れない");
        Assert.Equal(1.0 - r, UtilityAgent.FiscalStressOf(w), 12);   // AI の逼迫度＝1−r（R4）
        // 利子：払った額＝請求×r・残りは元本へ（国内の持ち手がいる＝繰り入れる）
        double intPaid = intL0 - w.TreasuryLedger[(int)TreasuryCat.Interest];
        Assert.True(w.LastInterestPerMonth.Value > 0, "前提：利子の請求が0");
        Assert.True(System.Math.Abs(intPaid - (r * w.LastInterestPerMonth).Value) <= (1e-9 * w.LastInterestPerMonth).Value, $"利子の支払い {intPaid:F6} ≠ r×請求 {r * w.LastInterestPerMonth:F6}（r={r:F4}）");
        double borrowed = w.TreasuryLedger[(int)TreasuryCat.Borrow] - bor0;   // 借入（冷却中＝0 のはず）は元本の増分から除く
        Assert.True(System.Math.Abs((w.GovDebt.Value - debt0 - borrowed) - ((1 - r) * w.LastInterestPerMonth).Value) <= 1e-9 * System.Math.Max(1.0, w.GovDebt.Value), $"元本の増分（借入を除く）{w.GovDebt.Value - debt0 - borrowed:F6} ≠ 利子の未払い {(1 - r) * w.LastInterestPerMonth:F6}");
        Assert.True(BondLedger.Balanced(w, HeldSum(w)), "繰り入れの後に保有台帳（Σ保有＝国内元本）が割れた");
        // 行政：区分 Admin の減り＝行政の調達（StateSpent の Admin）だけ（連合維持費の人頭割りは 2026-09-30 (135) に撤去＝官僚と兵士の給与は購買の1本）
        double stateAdmin = 0; foreach (var rg in w.Regions) if (rg.State == RegionState.Active) stateAdmin += rg.StateSpent[(int)TreasuryCat.Admin];
        double adminOther = admL0 - w.TreasuryLedger[(int)TreasuryCat.Admin] - stateAdmin;
        Assert.True(stateAdmin > 0, "前提：行政の調達が0");
        Assert.True(System.Math.Abs(adminOther) <= 1e-9 * stateAdmin, $"区分 Admin に行政の購買の外の支払い {adminOther:F6} がある（連合維持費が戻った？）");
        // 兵士の給与：予算＝請求×r（旧＝州ごとの min(国庫)＝先着）
        double soldierBudget = 0; foreach (var rg in w.Regions) if (rg.State == RegionState.Active) soldierBudget += rg.SoldierPayBudget;
        Assert.True(w.LastSoldierPayPerMonth.Value > 0, "前提：兵士の給与の請求が0");
        Assert.True(System.Math.Abs(soldierBudget - (r * w.LastSoldierPayPerMonth).Value) <= (1e-9 * w.LastSoldierPayPerMonth).Value, $"兵士の給与の予算 {soldierBudget:F6} ≠ r×請求 {r * w.LastSoldierPayPerMonth:F6}");
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

    /// <summary>仏（seed 19）の盤面＝旧の開幕の不換（2026-09-30 (119) の盤面の直し）。実国家の開幕は銀になった（助走は法を動かさない＝通貨の初期値を史実の銀へ）。
    /// ここの番人は不換の切り下げの窓・払える額・借入の門を測る＝不換が前提（銀の段は余地の式が改鋳の窓になり、兌換に入った NullAgent の仏は
    /// 切り下げの余地が閉じて4四半期で不履行＝国債が消える）＝旧と同じ盤面を置く。</summary>
    private static Scenario FranceFiat()
    {
        var cfg = NationConfig.France().ShallowClone();
        cfg.InitialMonetaryLaw = MonetaryStandardLaw.Fiat;
        return cfg.ToScenario(19);
    }

    /// <summary>借入の門を測る仏の盤面（2026-09-29 (109)）。1783年の仏は所得税0・直接税だけ（NationConfig の税の段）＝NullAgent の盤面（専売なし）は
    /// 利払い÷歳入が借入の上限（MaxInterestShareOfRevenue）を開幕から越える（史実の1788年の仏＝利払い÷歳入 約0.5〜0.67＝借りられない国）。
    /// 借入の門そのものを測る番人は「上限の下にいる国」が前提＝旧の開始の所得税10% を盤面に置く（旧と同じ盤面の歳入）。</summary>
    private static Simulation FranceBelowBorrowCap()
    {
        var sim = new Simulation(FranceFiat(), new NullAgent());
        sim.World.TaxRate = new Share(0.10); sim.World.Policy.IncomeTaxTarget = 0.10;
        return sim;
    }

    private static MoneyStock HeldSum(World w)
    {
        MoneyStock s = default; foreach (var rg in w.Regions) foreach (var p in rg.Pops) s += p.BondHoldings;
        return s;
    }

    /// <summary>借入は払えない額のある月だけ・減債は r＝1 の月の余剰（spare＞0）からだけ（Fiscal の2つの門）。
    /// 借入は予約なしでエンジンが月に1件（2026-09-28 作者裁定「借入は自動化・借金ボタンは不要」）＝払えない月が続けば毎月1件ずつ借りる。
    /// 2026-09-28 (b) から借入は月初（r を決める前・切り下げより先）の <see cref="Fiscal.Borrow"/>＝門は「払えない額（−spare）がある」（旧＝月末の Fiscal.Step で r＜1）。
    /// ここの払えない月は払えない額が1件の額より大きい月（spare＝−Big）＝満額の1件（額の規則と上限は下の番人）。</summary>
    [Fact]
    public void 借入は払えない額のある月だけ減債はrが1の月の余剰からだけ()
    {
        var sim = FranceBelowBorrowCap();
        sim.Run(4);
        var w = sim.World; var c = w.Coef;
        int bo = (int)TreasuryCat.Borrow, rp = (int)TreasuryCat.DebtRepay;
        const double Big = 1e18;
        Assert.True(w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth && w.BorrowCooldownMonths <= 0, "前提：借入の上限か冷却中");
        // (a) 払えない額が無い（spare≧0・NaN）＝借りない
        double b0 = w.TreasuryLedger[bo];
        Assert.Equal(0.0, Fiscal.Borrow(w, c, 0));
        Assert.Equal(0.0, Fiscal.Borrow(w, c, 1e12));
        Assert.Equal(0.0, Fiscal.Borrow(w, c, double.NaN));
        Assert.Equal(b0, w.TreasuryLedger[bo]);
        // (b) 払えない額がある＝誰も予約しないのにエンジンが借りる（月に1件・借りた額は国庫の台帳の借入へ）
        double got = Fiscal.Borrow(w, c, -Big);
        Assert.True(got > 0, $"払えない月にエンジンが借りない（利払い÷歳入 {w.LastInterestPerMonth / System.Math.Max(1e-9, w.LastRevenuePerMonth.Value):F3}）");
        Assert.True(System.Math.Abs(got - (w.TreasuryLedger[bo] - b0)) <= 1e-9 * got, "借りた額が台帳の借入と合わない");
        // (b') 月末の決算を挟んだ次の払えない月も同じ1行でまた1件
        w.FiscalFundingRatio = new Share(1.0); Fiscal.Step(w, c, 0);
        double b1 = w.TreasuryLedger[bo];
        Assert.True(Fiscal.Borrow(w, c, -Big) > 0, "続く払えない月に借りない");
        Assert.True(w.TreasuryLedger[bo] > b1);
        // (c) r＜1：余剰を渡しても返さない
        double d0 = w.TreasuryLedger[rp];
        w.FiscalFundingRatio = new Share(0.9);
        Fiscal.Step(w, c, 1e12);
        Assert.Equal(d0, w.TreasuryLedger[rp]);
        // (d) r＝1 かつ余剰あり：返す（額は余剰まで）
        w.FiscalFundingRatio = new Share(1.0);
        double spare = 0.5 * w.GovDebt.Value * c.BondRepayFracPerMonth.Value;
        double d1 = w.TreasuryLedger[rp];
        Fiscal.Step(w, c, spare);
        double repaid = d1 - w.TreasuryLedger[rp];
        Assert.True(repaid > 0, "r＝1 の月の余剰から返済されない");
        Assert.True(repaid <= spare * (1 + 1e-9), $"返済 {repaid:F3} が余剰 {spare:F3} を超えた");
        // (e) r＝1 でも余剰が無ければ返さない（Step は借りもしない＝借入は月初の Borrow だけ）
        double d2 = w.TreasuryLedger[rp], b2 = w.TreasuryLedger[bo];
        Fiscal.Step(w, c, -1.0);
        Assert.Equal(d2, w.TreasuryLedger[rp]);
        Assert.Equal(b2, w.TreasuryLedger[bo]);
    }

    /// <summary>★借りる額は払えない額だけ（作者裁定 2026-09-28）＝借入額＝min(1件の額, 月初の払えない額＝−spare)。1件の額＝月次**名目**GDP×割合（2026-09-28・単位の対称）。
    /// 旧＝r＜1 なら不足の大きさに関係なく満額（1件の額）を借りた＝r が1のすぐ下の月（r＜1 の四半期の77%）も満額→上限→不履行を繰り返した。
    /// ①少しの不足（1件の額の1/4）＝その額だけ借りる ②大きな不足（1件の額の10倍）＝1件の額で止まる。
    /// 突然変異：満額に戻す→①が赤／1件の額で止めない→②が赤（2026-09-28 の借入スタックの削除で旧の③〔スタックは額の比・減債と同じ単位〕は消えた）。
    /// 借入は月初の <see cref="Fiscal.Borrow"/>（2026-09-28 (b)）＝spare を直接渡す。</summary>
    [Fact]
    public void 借入は払えない額だけで不足が大きいと1件の額で止まる()
    {
        var sim = FranceBelowBorrowCap();
        sim.Run(4);
        var w = sim.World; var c = w.Coef;
        int bo = (int)TreasuryCat.Borrow;
        double Unit() => System.Math.Max(0, w.NominalGdpPerMonth.Value) * c.BorrowFractionOfGdp.Value;   // 1件の額（Fiscal と同じ式・月ごとに読み直す）
        Assert.True(Unit() > 1, $"前提：1件の額が立っていない（{Unit()}）");
        Assert.True(w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth && w.BorrowCooldownMonths <= 0, "前提：借入の上限か冷却中");
        // ① 少しの不足＝その額だけ借りる
        double u1 = Unit(), small = 0.25 * u1;
        double b0 = w.TreasuryLedger[bo];
        Fiscal.Borrow(w, c, -small);
        double got1 = w.TreasuryLedger[bo] - b0;
        Assert.True(System.Math.Abs(got1 - small) <= 1e-9 * u1, $"少しの不足 {small:F3} に {got1:F3} を借りた（払えない額だけ借りていない）");
        // ② 大きな不足＝1件の額で止まる
        double u2 = Unit(), b1 = w.TreasuryLedger[bo];
        Fiscal.Borrow(w, c, -10 * u2);
        double got2 = w.TreasuryLedger[bo] - b1;
        Assert.True(System.Math.Abs(got2 - u2) <= 1e-9 * u2, $"大きな不足で {got2:F3} を借りた（1件の額 {u2:F3} で止まっていない）");
    }

    /// <summary>★借入の上限＝直近の月次決算の利払い÷歳入が限界（MaxInterestShareOfRevenue）に届く所（作者裁定 2026-09-28・棚卸し fis-01＝旧の借入スタック20を置き換えた）＋単位の対称。
    /// ①利払い÷歳入が限界の下なら借りる（借りられた月は、r＜1 でも不履行の計数は進まない）
    /// ②限界に届いたら借りない・その月が払えない（r＜1）なら不履行の計数が進み、払えている（r＝1）なら進まない
    /// ③同じ実体の国で物価水準が2倍（名目の量を全て2倍）でも、門の判定は同じで借りる額はちょうど2倍＝上限の比は物価水準に依らない（1件の額は名目GDP・門は名目÷名目）。
    /// 盤面＝同じ種の双子（ビット単位で同じ）。門が読む利払いと歳入は盤面の LastInterestPerMonth・LastRevenue（2026-09-28 (b) から借入は月初＝直近の月次決算の量）。
    /// 突然変異：1件の額を実質GDPへ戻す→①③が赤／門を外す→②が赤／不履行の計数で r を見ない→②が赤／門の歳入を実質で測る（÷物価水準）→①か③が赤。</summary>
    [Fact]
    public void 借入の上限は利払いが歳入の限界に届く所で物価水準が2倍でも比は変わらない()
    {
        static World Twin()
        {
            var sim = new Simulation(FranceFiat(), new NullAgent());
            sim.Run(4);
            var w = sim.World;
            w.PollTax = 0;   // 人頭税の率は頭数あたりの実額（物価水準で伸びない）＝双子の名目を2倍にしても2倍にならない＝止める
            // 盤面：不履行の計数は0から数える（2026-09-29 S3b の盤面の直し＝この盤面の4四半期目は払える額と請求がちょうど釣り合う刃の上
            //   〔r＝1 と r＝1−1ulp が張り付いた市場の打ち切り〔相対1e-8〕の差で入れ替わる〕＝旧は「0 のはず」を前提に置いていた）。
            w.InsolvencyQuarters = 0;
            return w;
        }
        // 名目の量を全て2倍にする（2倍は浮動小数で厳密＝和も積もちょうど2倍）＝同じ実体・物価水準2倍
        static void DoubleNominal(World w)
        {
            w.PriceLevel *= 2; w.GovDebt *= 2; w.ForeignDebt *= 2; w.LastTariffRevenuePerClearing *= 2; w.ForeignBondInvestorPool *= 2;
            w.LastInterestPerMonth *= 2; w.LastRevenuePerMonth *= 2;   // 門が読む直近の月次決算の量
            foreach (var r in w.Regions)
            {
                r.StateSpent[(int)TreasuryCat.StateMonopoly] *= 2;
                foreach (var p in r.Pops) { p.SetSavings(p.Savings.Value * (2)); p.EarnedPerMonth *= 2; p.LandRentPerMonth *= 2; p.BondHoldings *= 2; }
            }
        }
        const double Big = 1e18;
        // 1か月＝月初の借入（払えない額 Big）→月末の決算（r を渡す）。返り値＝借りた額
        static double Month(World w, double limit, double r)
        {
            w.Coef.MaxInterestShareOfRevenue = new Share(limit);
            double got = Fiscal.Borrow(w, w.Coef, r < 1 ? -Big : 0);
            w.FiscalFundingRatio = new Share(r);
            Fiscal.Step(w, w.Coef, 0);
            return got;
        }
        var a = Twin();
        double share = a.LastInterestPerMonth / a.LastRevenuePerMonth;
        Assert.True(a.LastInterestPerMonth.Value > 0 && a.LastRevenuePerMonth.Value > 0 && share > 0, $"前提：利払いと歳入が立っていない（{a.LastInterestPerMonth}／{a.LastRevenuePerMonth}）");
        double below = share * 1.001, above = share * 0.999;

        // ① 限界の下＝借りる（大きな不足＝1件の額＝名目GDP×割合でちょうど止まる）・借りられた月は r＜1 でも不履行の計数は進まない
        double unitA = (a.NominalGdpPerMonth * a.Coef.BorrowFractionOfGdp).Value;
        double gotA = Month(a, below, 0.5);
        Assert.True(gotA > 0, $"利払い÷歳入 {share:F4} が限界 {below:F4} の下なのに借りない");
        Assert.True(System.Math.Abs(gotA - unitA) <= 1e-9 * unitA, $"大きな不足で1件の額（名目GDP×割合）{unitA:F3} で止まっていない（{gotA:F3}＝1件の額の物差しが名目でないか、プールが先に尽きた）");
        Assert.Equal(0.0, a.InsolvencyQuarters);

        // ② 限界に届いた＝借りない・払えない月は不履行の計数が進む・払えている月は進まない
        var x = Twin();
        Assert.Equal(0.0, Month(x, above, 0.5));
        Assert.Equal(0.5, x.InsolvencyQuarters);   // ★(205) 計数は払えなかった大きさ（r＝0.5 の月＝1−0.5）
        var y = Twin();
        Assert.Equal(0.0, Month(y, above, 1.0));
        Assert.Equal(0.0, y.InsolvencyQuarters);

        // ③ 物価水準2倍の双子：門の判定は同じ・借りる額はちょうど2倍
        var b2 = Twin();
        double n0 = b2.NominalGdpPerMonth.Value;
        DoubleNominal(b2);
        Assert.True(System.Math.Abs(b2.NominalGdpPerMonth.Value - 2 * n0) <= 1e-12 * n0, $"前提：名目GDPが2倍にならない（{n0}→{b2.NominalGdpPerMonth}）");
        double gotB = Month(b2, below, 0.5);
        Assert.True(System.Math.Abs(gotB - 2 * gotA) <= 1e-12 * gotA, $"物価水準2倍の双子の借入 {gotB:F3} が {2 * gotA:F3}（2倍）でない＝1件の額か門が実質の物差し");
        var z2 = Twin();
        DoubleNominal(z2);
        Assert.Equal(0.0, Month(z2, above, 0.5));
        Assert.Equal(0.5, z2.InsolvencyQuarters);   // ★(205) 同じ（物価水準2倍の双子でも r は同じ）
    }

    /// <summary>★借入はエンジンの1行だけ（2026-09-28 作者裁定「借入は自動化・借金ボタンは不要」）＝AI とプレイヤーの経路が無い。
    /// ①借りる主体のいない世界（NullAgent）でも、払えない月の月次決算の中で借りる　②借入の書き手（国庫の台帳の区分 Borrow への Pay）は SimCore の Fiscal.cs の1か所だけ
    /// ＝SelfPlay（AI）と App（画面）には無い・Fiscal.Step と Fiscal.Borrow を呼ぶのは月次決算（Simulation.cs）だけ。
    /// 突然変異：予約の門を戻す（エンジンが自分では借りない）→①が赤／AI か画面に借りる口を足す→②が赤。</summary>
    [Fact]
    public void 借入はエンジンの1行だけでAIとプレイヤーの経路が無い()
    {
        // ① 借りる主体のいない世界の月次決算で借りる（盤面は上の「国家の支払い率は全ての行に同じrで…」と同じ＝借りなければ r＜1 の月）
        var sim = FranceBelowBorrowCap();
        sim.Run(4);
        sim.StepDay();
        var w = sim.World;
        double drain = w.Treasury.Value + 3 * System.Math.Max(1.0, w.LastRevenuePerMonth.Value);
        World.Transfer((w, null), (w, [w.Regions[0].Pops[0]], [1.0]), drain, TreasuryCat.OtherOut);   // 移し方の1本（2026-09-30 段4g＝国庫の口は private）
        w.AdminSpending = 3.0;
        w.Coef.DevaluationCapFrac = 0;   // 切り下げの余地を閉じる（仏は不換＝余地が払えない分を埋めて r＝1 になる・切り下げは下の番人が見る）
        double b0 = w.TreasuryLedger[(int)TreasuryCat.Borrow];
        Assert.True(w.LastInterestPerMonth < w.Coef.MaxInterestShareOfRevenue * w.LastRevenuePerMonth && w.BorrowCooldownMonths <= 0, "前提：借入の上限か冷却中");
        for (int d = 0; d < 29; d++) sim.StepDay();   // 月次決算を1回（借入→r を決める→…→財政）
        Assert.True(w.TreasuryLedger[(int)TreasuryCat.Borrow] > b0, "借りる主体のいない世界で払えない月にエンジンが借りない");

        // ② 借入の書き手は Fiscal.cs の1か所だけ・Fiscal.Step と Fiscal.Borrow を呼ぶのは Simulation.cs だけ（SimCore と App を走査・コメントは数えない）
        string root = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(ThisFile()))!;
        var borrowSites = new System.Collections.Generic.List<string>();
        var stepCallers = new System.Collections.Generic.List<string>();
        var borrowCallers = new System.Collections.Generic.List<string>();
        char sep = System.IO.Path.DirectorySeparatorChar;
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.App" })
            foreach (var f in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(root, dir), "*.cs", System.IO.SearchOption.AllDirectories))
            {
                if (f.Contains($"{sep}obj{sep}") || f.Contains($"{sep}bin{sep}")) continue;
                foreach (var line in System.IO.File.ReadLines(f))
                {
                    var code = line; int k = code.IndexOf("//", System.StringComparison.Ordinal); if (k >= 0) code = code[..k];
                    // 国庫へ起債の金を入れる口（読み手＝台帳の検査や表示の名前は数えない）。★2026-09-30 段4g：国庫の口（Pay）は private＝起債の金は移し方の1本で
                    //   国庫（受け手＝(国, null, null)）へ入る行（区分 Borrow）を数える（貸し手の引き落としの戻し＝受け手が世界の外の行は数えない）。
                    if (System.Text.RegularExpressions.Regex.IsMatch(code, @"Transfer\(.*\((?!null)\w+, null, null\).*TreasuryCat\.Borrow")) borrowSites.Add(System.IO.Path.GetFileName(f));
                    if (code.Contains("Fiscal.Step(")) stepCallers.Add(System.IO.Path.GetFileName(f));
                    if (code.Contains("Fiscal.Borrow(")) borrowCallers.Add(System.IO.Path.GetFileName(f));
                }
            }
        Assert.Equal(new[] { "Fiscal.cs", "Fiscal.cs" }, borrowSites.ToArray());   // 国内の脚と対外の脚（2行）＝同じ関数 Fiscal.Borrow（★(144) 反証の指摘＝数を2に固定・旧は Distinct で Fiscal.cs の中の行の数を見なかった）
        Assert.Equal(new[] { "Simulation.cs" }, stepCallers.ToArray());
        Assert.Equal(new[] { "Simulation.cs" }, borrowCallers.ToArray());
    }

    private static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string self = "") => self;

    /// <summary>★切り下げ（インフレ税）は払える額の側（2026-09-28・(87) の申し送り iii・作者裁定 F4「不足は量を減らすか未払いの記録＝他人から取らない」・思想⑨）。
    /// 双子＝同じ盤面の借りられない（借入の冷却中）r＜1 の月（兌換 0.5）で、F は切り下げの余地あり・G は切り下げの上限0（DevaluationCapFrac＝0）。
    /// （2026-09-28 (b) から借りられる月は借入が先に払えない額を埋める＝切り下げの規則は借りられない月で測る・順は下の番人）
    /// ①F の r ＝ (払える額＋切り下げ) ÷ 請求＝取った金はその月の支払いに使われる（請求は G の r ＝ 払える額 ÷ 請求 から戻す）
    /// ②切り下げの額＝余地の規則 (1−兌換)×（請求−払える額−min(0,国庫)）そのもの＝払えなかった請求と同じ額を別に取らない。
    /// ③（2026-09-29）その月の国債の金利に、この月の切り下げの速さ（×12÷年名目GDP）が信認の喪失として乗る＝月次決算が月末の Fiscal.Step へ切り下げの額を渡す。
    /// 突然変異：切り下げを r の後へ戻す（段2-1 の形＝F の r が G と同じ）→①が赤／r に入れた上に月末でも取る（二重）→②が赤／切り下げの額を渡さない→③が赤。</summary>
    [Fact]
    public void 切り下げは払える額に入り同じ月の支払いに使われ払えなかった請求と同じ額を別に取らない()
    {
        const double conv = 0.5;
        (World w, double payable, double overdraft, double dev) Month(bool capZero)
        {
            var sim = new Simulation(FranceFiat(), new NullAgent());
            sim.Run(4);
            sim.StepDay();
            var w = sim.World;
            double t0 = w.Treasury.Value;
            World.Transfer((w, null), (w, [w.Regions[0].Pops[0]], [1.0]), t0, TreasuryCat.OtherOut);   // 国庫を0へ（現金は1つの POP へ移す＝保存則を割らない・移し方の1本）
            w.AdminSpending = 3.0;                                                    // 行政の予算が歳入を超える＝r＜1 の月を作る
            w.BorrowCooldownMonths = 1;                                                     // 借りられない月（この月の締めの月初は冷却中）
            if (capZero) w.Coef.DevaluationCapFrac = 0;
            for (int d = 0; d < 28; d++) sim.StepDay();
            sim.StepDayOpen(); sim.StepDayDaily();   // 締めの直前＝r を決める時点と同じ国庫と見込み
            w.Convertibility = conv;                 // 部分兌換＝余地は払えない分の半分（月の上限 DevaluationCapFrac の手前）
            double payable = System.Math.Max(0, w.Treasury.Value) + w.LastRevenuePerMonth.Value, overdraft = System.Math.Min(0, w.Treasury.Value);
            double dv0 = w.TreasuryLedger[(int)TreasuryCat.Devalue];
            sim.StepDayClose(); w.FlushLog();        // 月次決算（切り下げ→r→予算→精算→財政）
            return (w, payable, overdraft, w.TreasuryLedger[(int)TreasuryCat.Devalue] - dv0);
        }
        var (wf, pf, of, dev) = Month(false);
        var (wg, pg, og, devG) = Month(true);
        Assert.NotSame(wf.Coef, wg.Coef);
        Assert.NotEqual(MonetaryStandardLaw.Silver, wf.MonetaryLaw);   // 前提：銀の段なら余地の式が改鋳の窓になる（盤面は不換＝FranceFiat・NullAgent は銀へ上げない）
        Assert.Equal(pg, pf); Assert.Equal(og, of);   // 双子＝締めの直前まで同じ
        double rG = wg.FiscalFundingRatio.Value, rF = wf.FiscalFundingRatio.Value;
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
        // ③ その月の国債の金利に、この月の切り下げの速さ（×12÷年名目GDP）が信認の喪失として乗る（2026-09-29＝月次決算が月末の Fiscal.Step へこの月の切り下げを渡す）。
        //   G との差＝BondRateDistrustScale×速さ×兌換の割引×議会の割引（G は同じ月に未払いの利子が元本へ入った分だけ債務比が大きい＝その差は桁違いに小さい）。
        //   突然変異：月次決算が切り下げを渡さない（0）→赤。
        double speed = Calendar.PerMonthToPerYear(dev) / Calendar.PerMonthToPerYear(wf.NominalGdpPerMonth).Value;
        double disc = (1.0 - conv * (1.0 - wf.Coef.GoldStandardRateDiscount)) * (1.0 - wf.Coef.BondRateRepresentationDiscount * PoliticalQuantities.Clamp01(wf.ParliamentControl));
        double want = wf.Coef.BondRateDistrustScale * speed * disc;
        double got = (wf.GovBondRatePerYear - wg.GovBondRatePerYear).Value;
        Assert.True(want > 1e-4 && wf.GovBondRatePerYear < wf.Coef.BondRateMaxPerYear, $"前提：信認の喪失の上乗せ {want:E3} が立たないか上限に張り付いた（{wf.GovBondRatePerYear:F4}）");
        Assert.True(System.Math.Abs(got - want) <= 0.02 * want, $"切り下げた月の金利の上乗せ {got:E4} ≠ 0.5×速さ×割引 {want:E4}（月次決算が切り下げの額を渡していない）");
    }

    /// <summary>★借りられる月は切り下げない＝払える額の中身の順は 税・歳入→借入→それでも足りない分だけ切り下げ（作者裁定 2026-09-28 (b)「切り下げは借りられないとき（上限・冷却中）だけ」）。
    /// 史実の紙幣の増発は信用が尽きた後（アッシニャ1790〜・墺1811・米のグリーンバック1862）。旧（(89)〜(93)）＝切り下げが先に払える額へ入り、借入は月末に切り下げの残りだけ
    /// （切り下げの97〜99% は借りられた四半期）。盤面＝同じ種の双子（締めの直前までビット単位で同じ）の払えない月（国庫0・行政の予算が歳入を超える・兌換 0.5）。
    /// G＝借りられず（冷却中）切り下げもしない（上限0）＝請求＝払える額÷r。
    /// A＝借りられる（上限の下・冷却中でない・1件の額とプールが払えない額を越える）＝払えない額をちょうど借り、切り下げは0・r＝1。
    /// B＝冷却中／C＝上限（利払い÷歳入の限界0）＝借りず、切り下げ＝(1−兌換)×払えない額（旧と同じ余地の規則）。
    /// D＝借りられるが1件の額が払えない額の半分＝1件の額を借り、切り下げ＝(1−兌換)×（払えない額−1件の額）＝それでも足りない分だけ。
    /// 突然変異：切り下げを借入の前へ戻す（旧の順）→A が赤／借りた金を払える額へ足さない（翌月へ回す＝旧の月末の借入）→A の r が赤／
    /// 冷却を見ない→B が赤／上限を見ない→C が赤／借りられる月は一切切り下げない（残りも埋めない）→D が赤。</summary>
    [Fact]
    public void 借りられる月は切り下げず借りて埋め上限か冷却中の月だけ切り下げで埋める()
    {
        const double conv = 0.5;
        (World w, double payable, double overdraft, double dev, double bor, double unit) Month(System.Action<World> setup)
        {
            var sim = FranceBelowBorrowCap();
            sim.Run(4);
            sim.StepDay();
            var w = sim.World;
            double t0 = w.Treasury.Value;
            World.Transfer((w, null), (w, [w.Regions[0].Pops[0]], [1.0]), t0, TreasuryCat.OtherOut);   // 国庫を0へ（現金は1つの POP へ移す＝保存則を割らない・移し方の1本）
            w.AdminSpending = 3.0;                                                    // 行政の予算が歳入を超える＝払えない月を作る
            for (int d = 0; d < 28; d++) sim.StepDay();
            sim.StepDayOpen(); sim.StepDayDaily();   // 締めの直前＝r を決める時点と同じ国庫と見込み
            w.Convertibility = conv;
            setup(w);
            double payable = System.Math.Max(0, w.Treasury.Value) + w.LastRevenuePerMonth.Value, overdraft = System.Math.Min(0, w.Treasury.Value);
            double unit = System.Math.Max(0, w.NominalGdpPerMonth.Value) * w.Coef.BorrowFractionOfGdp.Value;   // 1件の額（Fiscal.Borrow と同じ式・締めの月初は産出の前＝同じ値）
            double dv0 = w.TreasuryLedger[(int)TreasuryCat.Devalue], bo0 = w.TreasuryLedger[(int)TreasuryCat.Borrow];
            sim.StepDayClose(); w.FlushLog();        // 月次決算（借入→切り下げ→r→予算→精算→財政）
            return (w, payable, overdraft, w.TreasuryLedger[(int)TreasuryCat.Devalue] - dv0, w.TreasuryLedger[(int)TreasuryCat.Borrow] - bo0, unit);
        }
        var g = Month(w => { w.BorrowCooldownMonths = 2; w.Coef.DevaluationCapFrac = 0; });
        double rG = g.w.FiscalFundingRatio.Value;
        Assert.True(rG > 0 && rG < 1, $"前提：G の r が (0,1) に無い（{rG:F4}）");
        Assert.Equal(0.0, g.dev); Assert.Equal(0.0, g.bor);
        Assert.NotEqual(MonetaryStandardLaw.Silver, g.w.MonetaryLaw);   // 前提：銀の段なら余地の式が改鋳の窓になる
        double claims = g.payable / rG;
        double need = claims - g.payable - g.overdraft;   // 払えない額（−spare）
        Assert.True(need > 0, "前提：払えない額が無い");

        // A：借りられる月＝払えない額をちょうど借り、切り下げない・r＝1
        var a = Month(w => { w.Coef.BorrowFractionOfGdp *= 4; });   // 1件の額を払えない額より大きく（1件の額の規則は別の番人）
        Assert.Equal(g.payable, a.payable); Assert.Equal(g.overdraft, a.overdraft);   // 双子＝締めの直前まで同じ
        Assert.True(a.unit > need, $"前提：A の1件の額 {a.unit:F1} が払えない額 {need:F1} を越えない");
        Assert.True(System.Math.Abs(a.bor - need) <= 1e-6 * need, $"借りられる月の借入 {a.bor:F4} が払えない額 {need:F4} でない（プールが尽きたか、切り下げが先に埋めた）");
        Assert.Equal(0.0, a.dev);
        Assert.Equal(1.0, a.w.FiscalFundingRatio.Value);

        // B：冷却中＝借りず、切り下げ＝(1−兌換)×払えない額
        var b = Month(w => { w.BorrowCooldownMonths = 2; });
        Assert.Equal(0.0, b.bor);
        Assert.True(System.Math.Abs(b.dev - (1 - conv) * need) <= 1e-6 * need, $"冷却中の切り下げ {b.dev:F4} ≠ (1−兌換)×払えない額 {(1 - conv) * need:F4}");

        // C：上限（利払い÷歳入の限界0＝借りられない）＝借りず、切り下げ＝(1−兌換)×払えない額
        var cc = Month(w => { w.Coef.MaxInterestShareOfRevenue = new Share(0); });
        Assert.Equal(0.0, cc.bor);
        Assert.True(System.Math.Abs(cc.dev - (1 - conv) * need) <= 1e-6 * need, $"上限の月の切り下げ {cc.dev:F4} ≠ (1−兌換)×払えない額 {(1 - conv) * need:F4}");

        // D：1件の額が払えない額の半分＝1件の額を借り、残りだけ切り下げ
        double unitG = g.unit;
        Assert.True(unitG > 0, "前提：1件の額が立っていない");
        var d = Month(w => { w.Coef.BorrowFractionOfGdp *= 0.5 * need / unitG; });
        Assert.True(System.Math.Abs(d.unit - 0.5 * need) <= 1e-9 * need, $"前提：D の1件の額 {d.unit:F4} が払えない額の半分でない");
        Assert.True(System.Math.Abs(d.bor - d.unit) <= 1e-9 * need, $"D の借入 {d.bor:F4} が1件の額 {d.unit:F4} でない");
        Assert.True(System.Math.Abs(d.dev - (1 - conv) * (need - d.unit)) <= 1e-6 * need, $"D の切り下げ {d.dev:F4} ≠ (1−兌換)×（払えない額−借入）{(1 - conv) * (need - d.unit):F4}");
    }
}
