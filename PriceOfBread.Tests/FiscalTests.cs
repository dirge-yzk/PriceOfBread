using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class FiscalTests
{
    // 近世の絶対王政はこのシナリオで所得税を置かない（Scenario.TaxRate 既定0）＝人頭税が国庫の主財源（近世財政）。
    // 宗教教育（寺子屋・教会学校）＝近世絶対王政の標準形。連合（治安維持力）を養えれば自由化を抑え絶対王政を保つ。
    private static Scenario Fisc(ulong s, double tax) => new()
    {
        Seed = s,
        Kind = ScenarioKind.AgrarianToUrban,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        EducationLaw = EducationLaw.Religious,
        EducationSpending = 1.0,
        ExternalPressure = 0.0,
        BaseCoercion = 150,   // この小経済（人口1000）が人頭税で養える規模の連合（旧1500は所得税時代の水準）
        FiscalEnabled = true,
        InitialPollTax = tax,
    };

    // ★主張の書き直し（作者裁定B・2026-09-12「今までの『上限/下限を削る』挙動は全て廃止する」＝2026-09-20 の段の点火で実装）：
    //   旧主張「低税では緊縮が逆進課税を**上げて**連合を養う」＝緊縮が人頭税・専売の実効値を Max で持ち上げる挙動（選んだ値を黙って書き換える＝P8違反）。
    //   新主張＝緊縮は**選ばせない**（Government.LawPrereq の「緊縮の下限」＝その段より下は選べない）だけで、上げる力は持たない。
    //   赤字は増税でなく国家の支払い率 r の低下（全ての行に同じ r・2026-09-28）→借入→デフォルト（DefaultTriggerQuarters）で閉じる。
    [Fact]
    public void 低税では緊縮が発火し逆進課税の下限の門になる()   // §6.2（赤字→借入→債務→緊縮：債権者が奪うのは法を選ぶ自由であって値ではない）
    {
        var sim = new Simulation(Fisc(12345, 0.05), new NullAgent());
        // 低税で連合を賄えない国は r＜1 の月にエンジンが借りて凌ぐ（2026-09-28 借入の自動化＝借りる主体のいないこの世界でも同じ1行）→債務累積→緊縮が発火。
        // ⚠この世界は不換＝払えない分は月初に切り下げで埋まり（2026-09-28 から払える額の側）r＜1 の月が来ない＝借りない。
        //   借入→債務→緊縮の門を測るため、切り下げの余地を閉じる（兌換の国と同じ形）。
        sim.World.Coef.DevaluationCapFrac = 0;
        // ⚠緊縮の物差しは年の名目GDP（2026-09-29・史実＝年GDP の3割で始まる）。この小経済（歳入は年GDP の約1%）は借入の上限（利払い÷歳入 MaxInterestShareOfRevenue）で
        //   国債が年GDP の約5% までしか積めない（貸し手は歳入の小さい国に貸さない＝史実どおり）＝鎖（赤字→借入→債務→緊縮の門）を測るため、開始だけをこの世界の債務の桁へ下げる
        //   （物差しと史実の値の番人は 緊縮と歳入要求と銀行の評価損は年の名目GDPで測る）。
        sim.World.Coef.AusterityStartDebt = 0.01;
        double maxAusterity = 0, maxPollTax = 0, maxMonopolyBreadth = 0;
        bool floorSeen = false;
        for (int q = 0; q < 400; q++)
        {
            sim.StepQuarter();
            maxAusterity = Math.Max(maxAusterity, sim.World.AusterityIntensity);
            maxPollTax = Math.Max(maxPollTax, sim.World.PollTax);
            maxMonopolyBreadth = Math.Max(maxMonopolyBreadth, sim.World.StateMonopolyBreadth);
            if (Government.LawPrereq(sim.World, PolicyAxis.PollTax, 0, sim.World.Coef) == (false, "緊縮の下限")) floorSeen = true;
        }
        Assert.True(maxAusterity > 0, "低税で借金を重ねても緊縮が1度も発火しない＝この世界では門を測れない");
        Assert.True(floorSeen, $"緊縮が立っても逆進課税（人頭税）の下限の門が1度も立たない（緊縮の最大 {maxAusterity:F2}）");
        Assert.True(maxPollTax <= 0.05 + 1e-9, $"誰も選んでいないのに人頭税が上がった（最大 {maxPollTax:F3}）＝削る/持ち上げる挙動が残っている");
        Assert.Equal(0.0, maxMonopolyBreadth);   // ★主張の書き直し（2026-09-20）：専売は政策軸ごと撤去＝緊縮は**材を勝手に選ばない**（専売の広さは0のまま）
    }

    [Fact]
    public void 十分な税は連合を養い絶対王政を保つ()   // 徳川型の抑圧：連合を養えれば自由化を抑える
    {
        var sim = new Simulation(Fisc(12345, 0.60), new NullAgent());
        // ★計器の隔離（回B 2026-09-22）：この番人が測るのは**財政→連合（治安維持力）→自由化の抑え込み**だけ。飢餓の駆動は回Bで平常からの急な落ち込みを測るようになり、
        //   この盤面（充足1.0から始まり、不作0.75〜0.82が2〜3年続いて充足が0.6台へ落ちる）では数年ぶんの飢饉が駆動を飽和させ
        //   （回復期も平常が四半期に3%しか寄らないので落差が残る）、闘争度0.97で絶対王政が倒れる（44四半期目に立憲・52四半期目に共和）＝別の経路。外して財政の経路だけを見る。
        sim.World.Coef.FamineDriveGain = 0;
        sim.Run(400);
        Assert.True(sim.World.FiscalFundingRatio > 0.9, $"連合が養えていない（funding {sim.World.FiscalFundingRatio:P0}）");
        Assert.Equal(Polity.Absolutist, sim.World.Polity);   // 治安維持力が満たされ自由化を抑え込む＝絶対王政の維持
    }

    [Fact]
    public void 財政下でも基礎貨幣は保存される()
    {
        var sim = new Simulation(Fisc(2, 0.30), new NullAgent());
        double base0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.BaseMoney - base0) < 1e-3 * Math.Max(1, base0), "基礎貨幣が保存されない");
    }

    [Fact]
    public void 同一シードで財政軌跡は決定論的()
    {
        var a = new Simulation(Fisc(3, 0.30), new NullAgent());
        var b = new Simulation(Fisc(3, 0.30), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Treasury, b.World.Treasury);
        Assert.Equal(a.World.Coercion, b.World.Coercion);
    }

    // =====================================================================================================
    // ★国債の保有台帳（2026-09-21裁定）の番人。Pop.BondHoldings＝「誰がどれだけ持つか」の記録（金は動かさない）。
    //   旧＝貯蓄比で買い・頭数比で償還＝往復ごとに富裕な資本家から貧しい資本家へ富が移っていた（分子と分母の非対称）。
    //   不変条件（Σ保有＝国内元本）はエンジンの BondLedger が毎月検査する＝ここは機構の向きと、運び忘れの出やすい経路を叩く。
    // =====================================================================================================

    private static List<Pop> AllPopsOf(World w) { var l = new List<Pop>(); foreach (var r in w.Regions) l.AddRange(r.Pops); return l; }
    // ★借入は払えない額（−spare）のある月だけ・減債は r＝1 の月の余剰（spare）から（作者裁定 F3・2026-09-28 段2-1）。月初に r を決める Simulation を通らない
    //   単体の Fiscal.Step で盤面を作る番人は、起債の月は r＜1（Short）と払えない額、減債の月は r＝1 と余剰を渡して踏む。借入は予約なしでエンジンが払えない月に1件（2026-09-28）
    //   ＝双子の片方だけ起債させるときは、借りない側を借入の冷却中（BorrowCooldown＝2 か月＝月末の Step の減算の後に次の借入の門が読む）にして同じ r で踏む
    //   （冷却は Fiscal.Borrow の門と月末の減算しか読まない）。借りる額は払えない額（−spare）だけ・1件の額まで（2026-09-28）＝r＜1 の月に spare を省くと、
    //   払えない額が1件の額より大きい月（−Ample）として踏む＝満額の1件。
    //   ⚠2026-09-28 (b) から借入は月初の Fiscal.Borrow（r を決める前・切り下げより先）＝この補助は「月末の決算 Step → 次の月初の借入 Borrow」の順で踏む
    //   ＝借りた月の利払いは次の Step から（旧＝Step の中で利払いの後に借りた形と同じ量の流れ＝番人の双子の差がそのまま国債の購入になる）。
    private const double Short = 0.5, Ample = 1e18;
    private static void FiscalMonth(World w, double r, double? spare = null)
    {
        double sp = spare ?? (r < 1 ? -Ample : 0);
        w.FiscalFundingRatio = r; Fiscal.Step(w, w.Coef, sp);
        Fiscal.Borrow(w, w.Coef, sp);
    }
    private static double HeldOf(World w) { double h = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) h += p.BondHoldings; return h; }

    /// <summary>英を1四半期温め、財政の国債以外の流れ（税・汚職の漏れ・維持費の頭数分配・福祉・年金・外債）を止めた盤面。
    /// 同じ種で2つ作れば双子＝片方だけで起債すれば、2つの貯蓄の差が国債の流れ（購入・利払い・償還）そのものになる。</summary>
    private static World QuietBritain()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.TaxRate = 0; w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;   // 歳入0＝汚職の漏れも0
        w.BaseCoercion = 0; w.Coef.AdminCostPerPop = 0;                        // 維持費0＝頭数比の俸給分配が0
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;                    // 福祉・年金（借入スタックの出費上限に依存＝双子で食い違う）を止める
        w.Coef.ForeignBondCapacity = 0;                                        // 全額を国内の投資家が引き受ける
        // 初期の国債（world.json の govDebt＝1783年の英の国債 年GDP の約1.1・2026-09-29）と保有を消す＝国債の流れは番人が起こしたものだけ（金は動かない＝請求権と記録だけ）
        w.GovDebt = 0; w.ForeignDebt = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.BondHoldings = 0;
        // 歳入の基数だけを立てる（関税の実収の診断値＝Fiscal.Step の中では予算の基数にだけ入り金を動かさない＝双子の差は国債の流れのまま）。
        //   借入の上限＝利払い÷歳入（2026-09-28）＝歳入0の国は借りられない＝税を止めたこの盤面でも借入の門を開けるため。
        w.LastTariffRevenue = w.Coef.MonthsPerClearing * w.NominalGdp;
        return w;
    }

    [Fact]
    public void 国債台帳_貯蓄の多い資本家は多く買い往復で富が移らない()
    {
        var t = QuietBritain(); var b = QuietBritain();   // 双子（b だけ起債する）
        Assert.Equal(0.0, b.GovDebt);
        t.Coef.BondRepayFrac = 0; b.Coef.BondRepayFrac = 0;   // 1か月目は買うだけ
        t.BorrowCooldown = 2;                                   // t は借りない（同じ r で踏む・月末の減算の後に借入の門が読む＝2）
        FiscalMonth(t, Short); FiscalMonth(b, Short);
        var tp = AllPopsOf(t); var bp = AllPopsOf(b);
        double domestic = b.GovDebt - b.ForeignDebt;
        Assert.True(domestic > 0, "前提が崩れた：起債が立っていない");
        double tol = 1e-9 * domestic;
        double invSav = 0; foreach (var p in tp) if (CapitalMarket.IsInvestor(p, t.Coef)) invSav += Math.Max(0, p.Savings);
        var paid = new double[bp.Count];
        double minPerHead = double.MaxValue, maxPerHead = 0;
        for (int i = 0; i < bp.Count; i++)
        {
            paid[i] = tp[i].Savings - bp[i].Savings;   // 双子の差＝このPOPが国債に払った額
            Assert.True(Math.Abs(bp[i].BondHoldings - paid[i]) <= tol, $"{bp[i].ClassName}：保有 {bp[i].BondHoldings} ≠ 払った額 {paid[i]}");
            double expect = CapitalMarket.IsInvestor(tp[i], t.Coef) ? domestic * Math.Max(0, tp[i].Savings) / invSav : 0;
            Assert.True(Math.Abs(bp[i].BondHoldings - expect) <= tol, $"{bp[i].ClassName}：保有 {bp[i].BondHoldings} が貯蓄比 {expect} でない");
            if (paid[i] > tol && bp[i].Size > 0) { minPerHead = Math.Min(minPerHead, paid[i] / bp[i].Size); maxPerHead = Math.Max(maxPerHead, paid[i] / bp[i].Size); }
        }
        Assert.True(maxPerHead > 2 * minPerHead, $"前提が崩れた：1人あたりの購入額がほぼ一様（{minPerHead:G4}〜{maxPerHead:G4}）＝頭数比の償還でも富が移らない盤面");
        new BondLedger().Check(b);

        // 2か月目：全額を償還（減債率1）＋その月の利払い。受け取りは保有比例＝各持ち手の純益÷払った額が全員同じ（＝利回り）。
        t.Coef.BondRepayFrac = 1.0; b.Coef.BondRepayFrac = 1.0;
        FiscalMonth(t, 1, Ample); FiscalMonth(b, 1, Ample);
        Assert.True(b.GovDebt <= tol, $"前提が崩れた：全額償還できていない（残 {b.GovDebt}）");
        double gain = 0, paidSum = 0;
        for (int i = 0; i < bp.Count; i++) { gain += bp[i].Savings - tp[i].Savings; paidSum += paid[i]; }
        Assert.True(gain > 0, "利払いが持ち手に届いていない");
        for (int i = 0; i < bp.Count; i++)
        {
            Assert.True(Math.Abs(bp[i].BondHoldings) <= tol, $"{bp[i].ClassName}：全額償還の後に保有 {bp[i].BondHoldings} が残る");   // 往復で台帳は空に戻る（浮動小数の端数まで）
            double net = bp[i].Savings - tp[i].Savings;   // 往復の純益＝−払った額＋利子＋元本
            Assert.True(Math.Abs(net - paid[i] * gain / paidSum) <= tol,
                $"{bp[i].ClassName}：往復の純益 {net} が払った額に比例しない（期待 {paid[i] * gain / paidSum}）＝往復で富が他の持ち手へ移っている");
        }
        new BondLedger().Check(b);
    }

    [Fact]
    public void 国債台帳_返し終えつつある国債に残る丸めのズレでは割れず運び忘れでは割れる()
    {
        // ★2026-09-28（支払いの一元化 段2-1 の合流）：全長の番人（Category=FullLength）が seed1789 の清で止まった偽陽性の再現。
        //   ズレは国債が最大のときの桁の丸め（相対1e-15級）で、比例の償還（Σ保有×keep と 国債−元本）は絶対値のまま保つ
        //   ＝国債を返して小さくなるほどズレの相対が膨らむ。許容の尺度は台帳を通った元本の流れ（起債と償還の累計）＝貨幣保存が累積貿易を入れるのと同じ形。
        var b = QuietBritain();
        b.Coef.BondRepayFrac = 0;
        FiscalMonth(b, Short);
        double g0 = b.GovDebt - b.ForeignDebt;
        Assert.True(g0 > 2e3, $"前提が崩れた：起債が小さい（{g0}）＝丸めの残り（相対1e-11）が旧の許容の床 1e-8 を越える盤面にならない");
        // 最大時の桁の丸めの残り（g0 の相対 1e-11＝旧の許容 1e-8 より3桁小さい）を最大の持ち手に置く＝全長で積もる丸めの形
        Pop? top = null;
        foreach (var p in AllPopsOf(b)) if (p.BondHoldings > 0 && (top is null || p.BondHoldings > top.BondHoldings)) top = p;
        Assert.NotNull(top);
        double drift = 1e-11 * g0;
        top!.BondHoldings += drift;
        new BondLedger().Check(b);   // 最大時＝どちらの尺度でも割れない
        // 半分ずつ返す（r＝1 の余剰から・減債率 0.5）＝国債が最大時の 1e-4 を割るまで
        b.Coef.BondRepayFrac = 0.5;
        for (int m = 0; m < 60 && b.GovDebt > 1e-4 * g0; m++) FiscalMonth(b, 1, Ample);
        double held = HeldOf(b), dom = b.GovDebt - b.ForeignDebt;
        Assert.True(b.GovDebt <= 1e-4 * g0 && b.GovDebt > 0, $"前提が崩れた：国債が返されていない（{b.GovDebt}・最大 {g0}）");
        Assert.True(Math.Abs(held - dom - drift) <= 1e-3 * drift, $"比例の償還がズレを絶対値のまま保っていない（{held - dom} 対 {drift}）");
        double oldScale = Math.Max(held, Math.Max(Math.Abs(b.GovDebt), Math.Abs(b.ForeignDebt)));
        Assert.True(Math.Abs(held - dom) > 1e-8 * Math.Max(1.0, oldScale), "前提が崩れた：旧の尺度（今の残高）でも割れない盤面");
        new BondLedger().Check(b);   // 丸めの残り＝割れない（台帳を通った流れ≒2×最大時の桁）
        // 運び忘れ＝保有の総額の1%が消える（返し終えつつある小さな国債でも、流れの桁の1e-8 より何桁も大きい）
        top.BondHoldings -= 0.01 * held;
        Assert.Throws<InvariantViolationException>(() => new BondLedger().Check(b));
    }

    [Fact]
    public void 国債台帳_利払いと償還は保有比例で買い手でない持ち手も受け取る()
    {
        var t = QuietBritain(); var b = QuietBritain();
        t.Coef.BondRepayFrac = 0; b.Coef.BondRepayFrac = 0;
        t.BorrowCooldown = 2;                                   // t は借りない（同じ r で踏む・月末の減算の後に借入の門が読む＝2）
        FiscalMonth(t, Short); FiscalMonth(b, Short);
        // 双子に同じ徴兵を1回＝最大の持ち手の一部が兵士POPへ（保有は貯蓄と同じ1人あたりで運ばれる＝兵士は買い手でないのに持ち手になる）
        var r0t = t.Regions[0]; var r0b = b.Regions[0];
        int k = -1; for (int i = 0; i < r0b.Pops.Count; i++) if (!r0b.Pops[i].IsSoldier && (k < 0 || r0b.Pops[i].BondHoldings > r0b.Pops[k].BondHoldings)) k = i;
        int s = r0b.Pops.FindIndex(x => x.IsSoldier);
        Assert.True(k >= 0 && s >= 0 && r0b.Pops[k].BondHoldings > 0, "前提が崩れた：首都州に持ち手か兵士POPがいない");
        double h0 = r0b.Pops[k].BondHoldings, sz0 = r0b.Pops[k].Size;
        Military.MoveCohort(r0t.Pops[k], r0t.Pops[s], r0t.Pops[k].Size * 0.2, t.Coef);
        Military.MoveCohort(r0b.Pops[k], r0b.Pops[s], r0b.Pops[k].Size * 0.2, b.Coef);
        double drafted = sz0 - r0b.Pops[k].Size;
        Assert.True(drafted > 0, "前提が崩れた：徴兵が起きていない");
        Assert.True(Math.Abs(r0b.Pops[s].BondHoldings - h0 * drafted / sz0) <= 1e-9 * h0, "徴兵で保有が1人あたりで運ばれていない");
        Assert.False(CapitalMarket.IsInvestor(r0b.Pops[s], b.Coef));
        new BondLedger().Check(b);

        // 2か月目：既定の減債率で部分償還＋利払い。受け取り（双子の差の増分）も保有の減り方も保有比例。
        double repayFrac = new Coefficients().BondRepayFrac;
        t.Coef.BondRepayFrac = repayFrac; b.Coef.BondRepayFrac = repayFrac;
        var tp = AllPopsOf(t); var bp = AllPopsOf(b);
        var h = new double[bp.Count]; var diff0 = new double[bp.Count]; double H = 0;
        for (int i = 0; i < bp.Count; i++) { h[i] = bp[i].BondHoldings; diff0[i] = bp[i].Savings - tp[i].Savings; H += h[i]; }
        FiscalMonth(t, 1, Ample); FiscalMonth(b, 1, Ample);
        double cash = 0, Hafter = 0;
        for (int i = 0; i < bp.Count; i++) { cash += (bp[i].Savings - tp[i].Savings) - diff0[i]; Hafter += bp[i].BondHoldings; }
        Assert.True(cash > 0 && Hafter < H, "前提が崩れた：利払い・償還が起きていない");
        double tol = 1e-9 * H;
        for (int i = 0; i < bp.Count; i++)
        {
            double got = (bp[i].Savings - tp[i].Savings) - diff0[i];
            Assert.True(Math.Abs(got - cash * h[i] / H) <= tol, $"{bp[i].ClassName}：受け取り {got} が保有比 {cash * h[i] / H} でない");
            Assert.True(Math.Abs(bp[i].BondHoldings - h[i] * Hafter / H) <= tol, $"{bp[i].ClassName}：保有が同じ比で減っていない（{h[i]}→{bp[i].BondHoldings}）");
        }
        double soldierGot = (r0b.Pops[s].Savings - r0t.Pops[s].Savings) - diff0[s];
        Assert.True(soldierGot > 0, "買い手でない持ち手（兵士POP）が利子・元本を受け取っていない");
        new BondLedger().Check(b);
    }

    [Fact]
    public void 国債台帳_デフォルトで保有が消え国庫へは戻らない()
    {
        var w = QuietBritain();
        w.Coef.BondRepayFrac = 0;
        FiscalMonth(w, Short);
        double H = HeldOf(w);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        double treasury0 = w.Treasury, base0 = w.BaseMoney;
        w.Coef.MaxInterestShareOfRevenue = 0;                 // 借入上限に張り付き（利払い÷歳入≧限界＝借りられない）
        w.InsolvencyQuarters = w.Coef.DefaultTriggerQuarters; // 返せない期間が尽きた＝この月にデフォルト
        FiscalMonth(w, Short);                                // 払えない月（r＜1）＝借りられずに払えない月が続いた
        Assert.True(w.HadSovereignDefault, "前提が崩れた：デフォルトが起きていない");
        Assert.Equal(0.0, w.GovDebt);
        foreach (var p in AllPopsOf(w)) Assert.Equal(0.0, p.BondHoldings);   // 損は持ち手＝保有は消える
        new BondLedger().Check(w);
        // 帳消しの額は国庫へ戻らない（国庫はその月の利払いぶん減るだけ）＝台帳の消滅は金を1円も動かさない
        Assert.True(w.Treasury <= treasury0, $"帳消しの分が国庫へ戻っている（{treasury0}→{w.Treasury}・保有 {H}）");
        Assert.True(Math.Abs(w.BaseMoney - base0) <= 1e-9 * Math.Max(1.0, Math.Abs(base0)), $"デフォルトで基礎貨幣が動いた（{base0}→{w.BaseMoney}）");
    }

    [Fact]
    public void 国債台帳_台帳は金を動かさず誰に渡るかだけを決める()
    {
        // 3つの双子：x＝そのまま／y＝保有を買い手でない1つのPOPへ寄せる（和は同じ）／z＝台帳を空にする（誰も読まれない＝払いは国庫に残る）
        var x = QuietBritain(); var y = QuietBritain(); var z = QuietBritain();
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFrac = 0; FiscalMonth(w, Short); }
        double H = HeldOf(y);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        var ypops = AllPopsOf(y); var xpops = AllPopsOf(x);
        int j = ypops.FindIndex(p => !p.IsSoldier && !CapitalMarket.IsInvestor(p, y.Coef));
        foreach (var p in ypops) p.BondHoldings = 0;
        ypops[j].BondHoldings = H;
        foreach (var p in AllPopsOf(z)) p.BondHoldings = 0;
        double repayFrac = new Coefficients().BondRepayFrac;
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFrac = repayFrac; FiscalMonth(w, 1, Ample); }
        Assert.Equal(x.GovDebt, y.GovDebt);    // 元本の動きは台帳を読まない
        Assert.Equal(x.GovDebt, z.GovDebt);
        Assert.Equal(x.Treasury, y.Treasury);  // 国庫の出入りも同じ（持ち手がいる限り）
        double scale = Math.Max(1.0, Math.Abs(x.BaseMoney));
        Assert.True(Math.Abs(x.BaseMoney - y.BaseMoney) <= 1e-9 * scale, $"台帳の中身で貨幣の総量が変わった（{x.BaseMoney} / {y.BaseMoney}）");
        Assert.True(Math.Abs(x.BaseMoney - z.BaseMoney) <= 1e-9 * scale, $"空の台帳で貨幣の総量が変わった（{x.BaseMoney} / {z.BaseMoney}）");
        Assert.True(z.Treasury > x.Treasury, "持ち手のいない国内分が国庫に残っていない");
        Assert.True(ypops[j].Savings > xpops[j].Savings, "寄せた持ち手が利子・元本を受け取っていない");
        new BondLedger().Check(x); new BondLedger().Check(y);
    }

    // 【墓標2026-09-28・支払いの一元化 段2-1】「減債の線は借入の線と同じ」（2026-09-27）＝旧の線（国庫＜維持費＋利払い）が消えた。
    //   借入は国家の支払い率 r＜1 の月だけ・減債は r＝1 の月の余剰（spare）から＝同じ月に借りながら返すことは構造的に起きない
    //   ＝番人は TreasuryLedgerTests.借入はrが1未満の月だけ減債はrが1の月の余剰からだけ。

    [Fact]
    public void 国債台帳_階級移動と州間移動と長い走で不変条件が保たれる()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        // 4四半期ごとに起債を積んで40四半期＝転落・昇格（Meritocracy）・徴兵と復員（Military）・州内/州間の移住（PopDecision/Territory）の全経路を
        // 持ち手のいる世界で回す（エンジンの月次検査に BondLedger が入っている＝運び忘れがあれば例外で落ちる）
        for (int q = 0; q < 40; q++) { if (q % 4 == 0) FiscalMonth(sim.World, Short); sim.StepQuarter(); }   // 起債は r＜1 の月（2026-09-28・エンジンの1行）
        var w = sim.World;
        Assert.True(HeldOf(w) > 0, "前提が崩れた：国内債の持ち手がいない");
        new BondLedger().Check(w);
        // 直接の2経路：階級間の移動（Military.MoveCohort の通常枝）と州間移動（Territory.MoveBetweenRegions）は1人あたりで運ぶ
        Pop? from = null; foreach (var p in AllPopsOf(w)) if (!p.IsSoldier && (from is null || p.BondHoldings > from.BondHoldings)) from = p;
        var reg = w.Regions.Find(r => r.Pops.Contains(from!))!;
        var to = reg.Pops.Find(p => !p.IsSoldier && !ReferenceEquals(p, from) && p.ClassId == PopClass.Worker)!;
        double hf = from!.BondHoldings, sf = from.Size, ht = to.BondHoldings;
        Military.MoveCohort(from, to, sf * 0.1, w.Coef);
        double moved = sf - from.Size;
        Assert.True(Math.Abs(to.BondHoldings - ht - hf * moved / sf) <= 1e-9 * hf, "階級間の移動で保有が1人あたりで運ばれていない");
        new BondLedger().Check(w);
        var other = w.Regions.Find(r => !ReferenceEquals(r, reg) && r.Pops.Exists(p => p.ClassId == to.ClassId && !p.IsSoldier))!;
        var dp = other.Pops.Find(p => p.ClassId == to.ClassId && !p.IsSoldier)!;
        double h1 = to.BondHoldings, s1 = to.Size, hd = dp.BondHoldings;
        Territory.MoveBetweenRegions(to, dp, s1 * 0.1, w.Coef);
        Assert.True(Math.Abs(dp.BondHoldings - hd - h1 * 0.1) <= 1e-9 * Math.Max(1.0, h1), "州間移動で保有が1人あたりで運ばれていない");
        new BondLedger().Check(w);
    }

    [Fact]
    public void 国債台帳_転落と昇格の経路は保有を1人あたりで運ぶ()
    {
        // ★検証担当の追加（2026-09-22）：Meritocracy.Move（転落・昇格・聖職者・職人など18箇所の呼び出しの1本）の運搬を直接叩く。
        //   ここで運ばないと保有は移動元に残るが、同じ国の中なので Σ保有＝国内元本は割れない＝エンジンの BondLedger では捕まらない
        //   （運搬を外す突然変異が、この番人を足す前は国債台帳の8本すべてを緑のまま通った）。
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World; var c = w.Coef; var r = w.Region;
        // 昇格だけを残して隔離（MobilityTests の世代ゲートの番人と同じ盤面＝他の移動機構は率0）
        c.JobSearchRate = 0; c.PettyEntryRate = 0; c.ArtisanDeclineRate = 0;
        c.MiddleFallRate = 0; c.UpperFallRate = 0; c.BankruptcyFallRate = 0; c.CapitalRiseRate = 0;
        var worker = r.Pops[r.MeritFromIndex];
        worker.Literacy = 1.0; worker.Earned = 1.0 * worker.Size;
        foreach (var p in r.Pops) if (p.IsModernElite) { p.EducationFill = 1.0; p.EducationEma = 1.0; }
        foreach (var idx in new[] { r.MeritToIndex, r.TeacherPopIndex, r.BureaucratPopIndex, r.DoctorPopIndex })
            if (idx >= 0 && r.Pops[idx].Size > 1e-9) r.Pops[idx].Earned = 10.0 * r.Pops[idx].Size;
        foreach (var reg in w.Regions) foreach (var p in reg.Pops) p.BondHoldings = 0;
        const double perHead = 1000.0;
        worker.BondHoldings = perHead * worker.Size;   // 移動元だけを持ち手にする（和は検査しない＝運搬の向きだけを見る盤面）
        double s0 = worker.Size, h0 = worker.BondHoldings;
        Meritocracy.Step(w, c);
        double moved = s0 - worker.Size;
        Assert.True(moved > 1e-9, "前提が崩れた：昇格が起きていない");
        Assert.True(Math.Abs(worker.BondHoldings / worker.Size - perHead) <= 1e-9 * perHead,
            $"移動元に残る1人あたりの保有が変わった（{perHead}→{worker.BondHoldings / worker.Size}）＝移動者の保有が運ばれていない");
        double arrived = 0; foreach (var reg in w.Regions) foreach (var p in reg.Pops) if (!ReferenceEquals(p, worker)) arrived += p.BondHoldings;
        Assert.True(Math.Abs(arrived - h0 * moved / s0) <= 1e-9 * h0, $"移動先に届いた保有 {arrived} が移動者の持ち分 {h0 * moved / s0} でない");
    }

    [Fact]
    public void 国債台帳_越境移民の保有は発行国の対外債へ振り替わる()
    {
        var coef = new Coefficients();
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent(), coef);
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World;
        // 仏の初期国債（米独立戦争債務）は構築時に購入と同じ規則で起票される
        Assert.True(fr.GovDebt > 0 && Math.Abs(HeldOf(fr) - (fr.GovDebt - fr.ForeignDebt)) <= 1e-9 * fr.GovDebt, "仏の初期国債の持ち主が起票されていない");
        new BondLedger().Check(fr); new BondLedger().Check(br);
        var frR = fr.Regions[0];
        Pop cap = frR.Pops[0]; foreach (var p in frR.Pops) if (p.BondHoldings > cap.BondHoldings) cap = p;
        var worker = frR.Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        Assert.True(cap.BondHoldings > 0, "前提が崩れた：首都州に持ち手がいない");
        Military.MoveCohort(cap, worker, cap.Size * 0.5, coef);   // 転落＝保有が1人あたりで労働者へ（越境する階級に持ち手を作る）
        double hw = worker.BondHoldings, sw = worker.Size, fd0 = fr.ForeignDebt, cl0 = br.ForeignBondClaims;
        var brWorker = br.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        double hb0 = brWorker.BondHoldings;
        Assert.True(hw > 0, "前提が崩れた：労働者が持ち手になっていない");
        sim.MoveMigrants(fr, br, frR, br.Regions[0], PopClass.Worker, coef.EmigrationMaxFrac * sw);   // ★回1-2：流量は呼び手が決める（1階級）
        double left = sw - worker.Size;
        Assert.True(left > 0, "前提が崩れた：移民が出ていない");
        double exported = hw * left / sw;
        double tol = 1e-9 * Math.Max(1.0, hw);
        Assert.True(Math.Abs(fr.ForeignDebt - fd0 - exported) <= tol, $"発行国の対外債が移民の保有ぶん増えていない（{fr.ForeignDebt - fd0} / {exported}）");
        Assert.True(Math.Abs(br.ForeignBondClaims - cl0 - exported) <= tol, "受入国の対外債権が移民の保有ぶん増えていない");
        Assert.Equal(hb0, brWorker.BondHoldings);   // 英の台帳には仏の国債を持ち込まない（英の国内元本は不変）
        new BondLedger().Check(fr); new BondLedger().Check(br);
    }

    [Fact]
    public void 国債台帳_建国と割譲は対外債へ振替_再併合は反乱国の債務ごと消える()
    {
        var coef = new Coefficients { RevoltExpansionScore = 0.10 };
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: coef) { DiplomacyFrozen = true };
        var pw = sim.Nations[0].World;
        pw.Coef.ForeignBondCapacity = 0;   // ⚠国の係数は国ごと別インスタンス（共有層の coef ではない）＝起債は全額国内（反乱国は親の係数を共有する）
        FiscalMonth(pw, Short);   // 起債は r＜1 の月（2026-09-28・エンジンの1行）
        sim.StepQuarter();   // 英が国内で起債＝各州の投資家が持ち手になる（月次の BondLedger 検査はエンジン内で走る）
        double hRebel = 0; foreach (var p in pw.Regions[1].Pops) hRebel += p.BondHoldings;
        Assert.True(hRebel > 0, "前提が崩れた：離脱する州に持ち手がいない");
        // (1) 建国（内戦の反乱国）：出ていく州の持ち手の保有は親の対外債へ＝反乱国の対外債権
        pw.PendingRebelRegions = [pw.Regions[1]]; pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        // 反乱国は生まれたばかり＝その対外債権は建国で振り替わった保有だけ。親の対外債はそれを含む（越境移民の振替・償還は四半期の途中にもある＝下限で見る）
        Assert.True(rw.ForeignBondClaims > 0, "建国で出ていった保有が反乱国の対外債権になっていない");
        Assert.True(pw.ForeignDebt >= rw.ForeignBondClaims * (1 - 1e-9), $"親の対外債 {pw.ForeignDebt} が反乱国の対外債権 {rw.ForeignBondClaims} を含んでいない");
        Assert.Equal(0.0, HeldOf(rw));   // 反乱国の台帳は空（反乱国はまだ起債していない）
        new BondLedger().Check(pw); new BondLedger().Check(rw);
        // (2) 割譲（内戦の膨張の腕＝Diplomacy の州の物理移転）：割譲州の保有も親の対外債へ
        var war = sim.Diplomacy.Wars.Find(x => x.CivilWarParent == pw.SelfPower)!;
        war.Score = -0.20; war.CivilWarSecession = true;   // ★2026-09-29 線2：膠着の枝（膨張の器）は分離独立の白紙だけ（政体の内戦の膠着は勝者で終わる）
        var before = new HashSet<Region>(rw.Regions);
        double fd0 = pw.ForeignDebt, cl0 = rw.ForeignBondClaims;
        sim.Diplomacy.ResolvePeace(coef, war);
        var ceded = rw.Regions.FindAll(r => !before.Contains(r));
        Assert.True(ceded.Count > 0, "前提が崩れた：州が割譲されていない");
        foreach (var r in ceded) foreach (var p in r.Pops) Assert.Equal(0.0, p.BondHoldings);
        double exported = pw.ForeignDebt - fd0;
        Assert.True(exported > 0, "前提が崩れた：割譲州に持ち手がいない");
        Assert.True(Math.Abs(rw.ForeignBondClaims - cl0 - exported) <= 1e-9 * pw.ForeignDebt, "割譲州の保有が勝者の対外債権になっていない");
        new BondLedger().Check(pw); new BondLedger().Check(rw);
        // (3) 再併合：反乱国の国債は誰にも継承されない（既存どおり反乱国の GovDebt ごと消える）＝持ち手の保有も消える
        FiscalMonth(rw, Short);   // 起債は r＜1 の月（2026-09-28・エンジンの1行）
        sim.StepQuarter();   // 反乱国が自国の投資家から借りる
        Assert.True(HeldOf(rw) > 0 && rw.GovDebt > 0, "前提が崩れた：反乱国の国債の持ち手がいない");
        var returning = new List<Region>(rw.Regions);
        double pwDom0 = pw.GovDebt - pw.ForeignDebt;
        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
        sim.StepQuarter();
        Assert.DoesNotContain(sim.Nations, n => ReferenceEquals(n.World, rw));
        foreach (var r in returning) foreach (var p in r.Pops) Assert.Equal(0.0, p.BondHoldings);   // 反乱国の国債は消えた＝持ち手の損
        Assert.True(pw.GovDebt - pw.ForeignDebt <= pwDom0 + 1e-9 * Math.Max(1.0, pwDom0), "親が反乱国の国債を継承している");
        new BondLedger().Check(pw);
        sim.StepQuarter();   // 返った州を含めた1四半期（エンジンの月次検査）
        new BondLedger().Check(pw);
    }

    [Fact]
    public void 国債台帳_台帳導入前のセーブは読込時に起票される()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var fr1 = sim1.Nations[1].World;
        Assert.True(fr1.GovDebt > 0, "前提が崩れた：仏に初期国債がない");
        foreach (var r in fr1.Regions) foreach (var p in r.Pops) p.BondHoldings = 0;   // 台帳導入前のセーブ（保有は既定値0で読まれる）を模す
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var fr2 = sim2.Nations[1].World;
        Assert.True(HeldOf(fr2) > 0, "旧セーブの国内債に持ち主が起票されていない");
        new BondLedger().Check(fr2);
        new BondLedger().Check(sim2.Nations[0].World);
        sim2.StepQuarter();   // 読み込んだ世界が月次の検査を通って進む（セーブ破れでない）
    }

    // =====================================================================================================
    // ★国境をまたぐ金の両建て（2026-09-22）の番人。国ごとの保存（MoneyConservation）は「流出を記帳した国」だけを見る＝
    //   受け取る国が無くても割れない。ここは**世界の和**と**受け手ごとの額**を直接叩く。
    // =====================================================================================================

    /// <summary>英・仏・普で、国際金融の外債以外の流れ（外資の逃避・配当・新規の資本移動・国内資本の逃避）を止めた盤面。
    /// 開始盤面の繰上げを1回済ませる（PrevForeignDebt＝ForeignDebt・元利の管は空）。</summary>
    private static (MultiNationSim sim, Coefficients coef) QuietBondWorld()
    {
        var coef = new Coefficients { CapitalFlightRate = 0, CapitalDividendRate = 0, CapitalDemandScale = 0, CapitalDomesticFlightRate = 0 };
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        CapitalMarket.Step(sim.Nations, coef);
        return (sim, coef);
    }

    private static double MintedAndFiat(World[] ws) { double s = 0; foreach (var w in ws) s += w.CumulativeGoldMinted + w.CumulativeFiatIssued; return s; }
    private static double WorldBase(World[] ws) { double s = 0; foreach (var w in ws) s += w.BaseMoney; return s; }

    [Fact]
    public void 外債の元本_償還は減らした債権の比で債権国へ届き世界の貨幣は変わらない()
    {
        var (sim, coef) = QuietBondWorld();
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        var all = new[] { br, fr, pr };
        Assert.True(fr.GovDebt > 0, "前提が崩れた：仏に初期国債がない");
        foreach (var w in all) Assert.Equal(0.0, w.ForeignBondClaims);   // 開始盤面に対外債権は無い＝下で振り替えた分だけが債権
        // 仏の最大の持ち手の保有を英へ3・普へ1の比で国外へ振り替える（越境移民・割譲と同じ経路）＝英と普が仏の外債の債権国になる
        Pop cap = fr.Regions[0].Pops[0];
        foreach (var r in fr.Regions) foreach (var p in r.Pops) if (p.BondHoldings > cap.BondHoldings) cap = p;
        double h = cap.BondHoldings;
        Assert.True(h > 0, "前提が崩れた：仏の国債に持ち手がいない");
        Fiscal.ExportBondHoldings(cap, 0.6 * h, fr, br);
        Fiscal.ExportBondHoldings(cap, 0.2 * h, fr, pr);
        new BondLedger().Check(fr);
        // 仏の財政を静める（歳入・維持費・福祉が0＝国庫の出入りは減債だけ）＋利子0（元本だけを見る）＋余剰の全額で減債
        fr.TaxRate = 0; fr.PollTax = 0; fr.LandTax = 0; fr.ProgressiveTax = 0;
        fr.BaseCoercion = 0; fr.Coef.AdminCostPerPop = 0; fr.SocialSecurity = 0; fr.Pension = ReformLevel.None;
        fr.GovBondRate = 0; fr.GovAvgCoupon = 0; fr.Coef.BondRepayFrac = 1.0;
        var guards = Array.ConvertAll(all, w => new MoneyConservation(w));
        var base0 = Array.ConvertAll(all, w => w.BaseMoney);
        double world0 = WorldBase(all), mint0 = MintedAndFiat(all);
        double cBr0 = br.ForeignBondClaims, cPr0 = pr.ForeignBondClaims, fd0 = fr.ForeignDebt;

        FiscalMonth(fr, 1, fr.Treasury);           // 仏が減債する（r＝1・余剰＝国庫＝維持費も利子も0の盤面・国内分は保有比で国内の持ち手へ・対外分は国外へ）
        double principal = fd0 - fr.ForeignDebt;
        Assert.True(principal > 0, $"前提が崩れた：対外の元本が償還されていない（国庫 {fr.Treasury}）");
        CapitalMarket.Step(sim.Nations, coef);     // 四半期末の国際金融＝債権の繰上げと元利の配当

        double tol = 1e-9 * Math.Max(1.0, principal);
        double gotBr = br.BaseMoney - base0[0], gotPr = pr.BaseMoney - base0[2];
        // (a) 償還した対外の元本は債権国に同額届く＝仏の減りと債権国の増えが相殺し、世界の基礎貨幣は鋳造・不換の台帳ぶんしか動かない
        Assert.True(Math.Abs(gotBr + gotPr - principal) <= tol, $"債権国に届いた額 {gotBr + gotPr} ≠ 償還した対外の元本 {principal}（届かない分が世界から消えている）");
        Assert.True(Math.Abs(fr.BaseMoney - base0[1] + principal) <= tol, $"仏の基礎貨幣の減り {base0[1] - fr.BaseMoney} ≠ 対外の元本 {principal}");
        double worldDrift = (WorldBase(all) - world0) - (MintedAndFiat(all) - mint0);
        Assert.True(Math.Abs(worldDrift) <= 1e-9 * Math.Max(1.0, world0), $"世界の基礎貨幣が台帳の外で {worldDrift} 動いた");
        // (b) 債権国が複数なら、減らした債権の比で配られる＝各債権国の受け取り＝その国の債権の減り（3:1）
        Assert.True(Math.Abs(gotBr - (cBr0 - br.ForeignBondClaims)) <= tol, $"英の受け取り {gotBr} ≠ 英の債権の減り {cBr0 - br.ForeignBondClaims}");
        Assert.True(Math.Abs(gotPr - (cPr0 - pr.ForeignBondClaims)) <= tol, $"普の受け取り {gotPr} ≠ 普の債権の減り {cPr0 - pr.ForeignBondClaims}");
        Assert.True(Math.Abs(gotBr - 3 * gotPr) <= tol, $"受け取りが債権の比 3:1 でない（英 {gotBr} / 普 {gotPr}）");
        for (int i = 0; i < all.Length; i++) guards[i].Check(all[i]);   // 各国の保存も閉じたまま（債権国は流入を記帳している）
        new BondLedger().Check(fr);
    }

    [Fact]
    public void 国内資本の逃避_逃げた金は受け皿の国へ届き受け皿に同額の負債が立つ()
    {
        // 対外元本と同じ形の片側記帳だった（CapitalMarket の国内資本の逃避＝流出を記帳するだけで受け手が無い）。
        // 請求権も片側だった（逃げた側の CapitalAbroad だけが増え、受け皿の負債が立たない＝旗 DomesticFlightHasHost の既定0・2026-09-22 に旗ごと点火）。
        // 国際金融の逃避以外の流れを止め、仏だけを切り下げ常習（不信0.5）にする。
        var coef = new Coefficients { CapitalFlightRate = 0, CapitalDividendRate = 0, CapitalDemandScale = 0 };   // 外債の元利は構築直後で空（新規の起債も利払いも無い）
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        var all = new[] { br, fr, pr };
        foreach (var w in all) Assert.True(w.NominalGdp > 0, $"前提が崩れた：{w.Region.Name} の名目GDPが立っていない");
        fr.CumulativeDevaluation = fr.NominalGdp * (coef.CapitalFlightDistrustThreshold + 0.5);
        var guards = Array.ConvertAll(all, w => new MoneyConservation(w));
        var base0 = Array.ConvertAll(all, w => w.BaseMoney);
        var fci0 = Array.ConvertAll(all, w => w.ForeignCapitalIn);
        var ca0 = Array.ConvertAll(all, w => w.CapitalAbroad);
        double world0 = WorldBase(all), mint0 = MintedAndFiat(all), abroad0 = fr.CapitalAbroad;

        CapitalMarket.Step(sim.Nations, coef);

        double fled = fr.CapitalAbroad - abroad0;
        Assert.True(fled > 0, "前提が崩れた：仏の資本が逃げていない");
        double tol = 1e-9 * Math.Max(1.0, fled);
        double gotBr = br.BaseMoney - base0[0], gotPr = pr.BaseMoney - base0[2];
        Assert.True(Math.Abs(fr.BaseMoney - base0[1] + fled) <= tol, $"仏の基礎貨幣の減り {base0[1] - fr.BaseMoney} ≠ 逃げた額 {fled}");
        Assert.True(Math.Abs(gotBr + gotPr - fled) <= tol, $"受け皿の国に届いた額 {gotBr + gotPr} ≠ 逃げた額 {fled}（届かない分が世界から消えている）");
        double worldDrift = (WorldBase(all) - world0) - (MintedAndFiat(all) - mint0);
        Assert.True(Math.Abs(worldDrift) <= 1e-9 * Math.Max(1.0, world0), $"世界の基礎貨幣が台帳の外で {worldDrift} 動いた");
        // 受け皿の按分＝自国以外の信用×名目GDP（CreditRating は同じ Step の信用の写し）
        double wBr = br.CreditRating * br.NominalGdp, wPr = pr.CreditRating * pr.NominalGdp;
        Assert.True(Math.Abs(gotBr - fled * wBr / (wBr + wPr)) <= tol, $"英の受け取り {gotBr} が信用×名目GDPの比 {fled * wBr / (wBr + wPr)} でない");
        // 請求権の両建て：受け皿の外資負債の増え＝届いた金（逃げた国の負債は動かない）
        Assert.Equal(fci0[1], fr.ForeignCapitalIn);
        Assert.True(Math.Abs(br.ForeignCapitalIn - fci0[0] - gotBr) <= tol, $"英の外資負債の増え {br.ForeignCapitalIn - fci0[0]} ≠ 受け取り {gotBr}（相手のいない請求権が残る）");
        Assert.True(Math.Abs(pr.ForeignCapitalIn - fci0[2] - gotPr) <= tol, $"普の外資負債の増え {pr.ForeignCapitalIn - fci0[2]} ≠ 受け取り {gotPr}（相手のいない請求権が残る）");
        // 世界の請求権の和が釣り合う＝Σ対外資産の増え＝Σ外資負債の増え（逃げた側の資産に、受け皿の負債が同額立つ）
        double dAbroad = 0, dFci = 0;
        for (int i = 0; i < all.Length; i++) { dAbroad += all[i].CapitalAbroad - ca0[i]; dFci += all[i].ForeignCapitalIn - fci0[i]; }
        Assert.True(Math.Abs(dAbroad - dFci) <= tol, $"Σ対外資産の増え {dAbroad} ≠ Σ外資負債の増え {dFci}");
        for (int i = 0; i < all.Length; i++) guards[i].Check(all[i]);
    }

    // =====================================================================================================
    // ★金利生活者のポンプ（2026-09-27）の番人。世界規模の補給崩壊の根は貨幣の崩壊だった：
    //   ①切下げの記憶が壊れたまま兌換へ戻れた（改革は兌換停止中にしかできない＝記憶が1世紀凍結し国内資本の逃避が止まらない）
    //   ②配当と逃避の払いが投資家で足りないと全POPから取った（流入は投資家へだけ入る＝分子と分母の非対称）＝受け皿の大衆の貨幣が国外へ抜けた。
    // =====================================================================================================

    [Fact]
    public void 外資の配当と逃避は投資家までしか払わない_払えない逃避は債権ごと消える()
    {
        // 外資の流入・逃避の受け皿・新規の資本移動は、どれも投資家の貯蓄へだけ入る（GiveGold の toInvestors）。流出も投資家まで＝
        // 投資家が尽きても農民・労働者の貯蓄は国外へ出ない。
        // ★2026-09-27（C）：払えない逃避は不履行＝債権ごと消える（受入国の負債と持ち主の対外資産が同じ額だけ減る・貨幣は払えた額しか動かない）。
        //   旧＝負債は払えた額しか減らず、払えなかった元本が毎四半期もう一度の逃避と配当を請求され続けた。払えない配当は積まない（同じ規則）。
        // 国際金融の外資以外の流れ（新規の資本移動・国内資本の逃避・外債の元利）を止め、仏の外資の負債を投資家の貯蓄の千倍にする（持ち主は英）。
        var coef = new Coefficients { CapitalDemandScale = 0, CapitalDomesticFlightRate = 0 };   // 外債の元利は構築直後で空（新規の起債も利払いも無い）
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        var all = new[] { br, fr, pr };
        var fpops = new List<Pop>(); foreach (var r in fr.Regions) fpops.AddRange(r.Pops);
        double inv0 = 0; int nOther = 0;
        foreach (var p in fpops) { if (CapitalMarket.IsInvestor(p, fr.Coef)) inv0 += Math.Max(0, p.Savings); else if (p.Savings > 0) nOther++; }
        Assert.True(inv0 > 0, "前提が崩れた：仏に投資家の貯蓄が無い");
        Assert.True(nOther > 0, "前提が崩れた：仏に貯蓄を持つ投資家でない人がいない");
        double claim = 1000 * inv0;
        fr.ForeignCapitalIn += claim; br.CapitalAbroad += claim;
        Assert.True(fr.ForeignCapitalIn * coef.CapitalDividendRate > inv0, "前提が崩れた：配当が投資家の貯蓄に収まる（不足が起きない）");
        var other0 = fpops.Select(p => CapitalMarket.IsInvestor(p, fr.Coef) ? double.NaN : p.Savings).ToArray();
        var guards = Array.ConvertAll(all, w => new MoneyConservation(w));
        double world0 = WorldBase(all), mint0 = MintedAndFiat(all), base0 = fr.BaseMoney, fci0 = fr.ForeignCapitalIn;
        var fcis0 = Array.ConvertAll(all, w => w.ForeignCapitalIn);
        var cas0 = Array.ConvertAll(all, w => w.CapitalAbroad);

        CapitalMarket.Step(sim.Nations, coef);

        // (a) 投資家でない人の貯蓄は1ビットも動かない
        for (int i = 0; i < fpops.Count; i++)
            if (!double.IsNaN(other0[i]))
                Assert.True(fpops[i].Savings == other0[i], $"投資家でない {fpops[i].ClassName} の貯蓄が {other0[i]}→{fpops[i].Savings} に動いた（配当・逃避の不足を大衆から取っている）");
        // (b) 仏が払った額は投資家の貯蓄まで（払い切って尽きる）
        double paid = base0 - fr.BaseMoney, tol = 1e-9 * Math.Max(1.0, inv0);
        Assert.True(paid <= inv0 + tol, $"仏が払った額 {paid} が投資家の貯蓄 {inv0} を超えた");
        Assert.True(paid >= inv0 - tol, $"前提が崩れた：投資家が払い切っていない（{paid} / {inv0}）");
        // (c) 払えない逃避は債権ごと消える＝仏の負債は払えた額でなく逃避の額だけ減る（CreditRating＝同じ Step の信用の写し）。
        //     払えない配当は積まない＝負債の減りは逃避の額ちょうど。持ち主の対外資産も同じ額だけ減る（請求権の両側を同時に）。
        double flight = Math.Min(fci0, fci0 * coef.CapitalFlightRate * (1.0 - fr.CreditRating)), tolC = 1e-9 * Math.Max(1.0, fci0);
        Assert.True(flight > 2 * paid, $"前提が崩れた：逃避 {flight} の大半が払える（払えた額 {paid}）");
        Assert.True(Math.Abs(fci0 - fr.ForeignCapitalIn - flight) <= tolC, $"仏の負債の減り {fci0 - fr.ForeignCapitalIn} ≠ 逃避の額 {flight}（払えなかった元本が債権のまま残る／未払いの配当が積まれた）");
        double dFci = 0, dCa = 0;
        for (int i = 0; i < all.Length; i++) { dFci += fcis0[i] - all[i].ForeignCapitalIn; dCa += cas0[i] - all[i].CapitalAbroad; }
        Assert.True(Math.Abs(dCa - dFci) <= tolC, $"持ち主の対外資産の減り {dCa} ≠ 受入国の負債の減り {dFci}（請求権の片側だけが消えた）");
        Assert.True(Math.Abs(cas0[0] - br.CapitalAbroad - flight) <= tolC, $"英の対外資産の減り {cas0[0] - br.CapitalAbroad} ≠ 仏の逃避の額 {flight}");
        // (d) 貨幣は払えた額しか動かない＝世界の基礎貨幣は台帳の外で動かない・各国の保存も閉じる
        double worldDrift = (WorldBase(all) - world0) - (MintedAndFiat(all) - mint0);
        Assert.True(Math.Abs(worldDrift) <= 1e-9 * Math.Max(1.0, world0), $"世界の基礎貨幣が台帳の外で {worldDrift} 動いた");
        for (int i = 0; i < all.Length; i++) guards[i].Check(all[i]);
    }

    [Fact]
    public void 兌換への復帰は通貨改革を通る_切下げの記憶が壊れたままでは戻れない()
    {
        // 改革は兌換停止中にしかできない（Fiscal の改革の条件 !OnGoldStandard）。再加入が記憶（累積切下げ÷名目GDP）を見ないと、
        // 改革より先に戻った国は記憶が1世紀凍結し、CapitalMarket の国内資本の逃避が止まらなかった（露1804〜11年の先着争い）。
        // 同じ盤面を2つ作り、記憶だけを違える。再加入の他の条件（財政・カバー率）と改革の静穏の月数はこの盤面の係数で外す
        // ＝対照が同じ月に戻れることで確かめる。
        World Board(bool broken)
        {
            var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 1789, _ => new NullAgent()) { DiplomacyFrozen = true };
            var w = sim.Nations[0].World;
            w.Coef.GoldStandardMinFunding = 0; w.Coef.GoldRejoinCoverMargin = 0;
            w.Coef.CurrencyReformCalmMonths = 0;
            w.MonetaryLaw = MonetaryStandardLaw.Silver;   // 金属本位の法は立てたまま（露の銀本位）＝兌換停止中
            w.OnGoldStandard = false;
            if (broken) w.CumulativeDevaluation = w.NominalGdp * (w.Coef.CapitalFlightDistrustThreshold + 0.5);
            return w;
        }
        var ctl = Board(false); var brk = Board(true);
        Assert.True(ctl.MonetaryEnabled, "前提が崩れた：英に貨幣経済が無い");
        Fiscal.Step(ctl, ctl.Coef);
        Assert.True(ctl.OnGoldStandard, "前提が崩れた：記憶の無い対照が再加入しない（盤面が再加入の他の条件を満たしていない）");
        Fiscal.Step(brk, brk.Coef);
        Assert.False(brk.OnGoldStandard, "切下げの記憶が壊れたまま兌換へ戻った（通貨改革を通らない回復）");
        // 通貨改革（プレイヤーのボタンとAIが押すのと同じ口）→ 記憶が消える。改革した月は改革の前の記憶で判定＝戻るのは翌月
        brk.RequestCurrencyReform(brk.ExchangeRate);
        Fiscal.Step(brk, brk.Coef);
        Assert.Equal(0.0, brk.CumulativeDevaluation);
        Assert.False(brk.OnGoldStandard, "改革と同じ月に兌換へ戻った（改革の前の記憶で判定していない）");
        Fiscal.Step(brk, brk.Coef);
        Assert.True(brk.OnGoldStandard, "通貨改革の後も兌換へ戻れない");
    }

    /// <summary>★兌換の門は債務の水準を読まない（作者裁定 2026-09-28 F6 の1歩目）。旧＝債務/年GDP の固定の閾値（停止 7.5%・再加入 4.2%）。
    /// 史実＝英は1815年に国債が GDP の約2.5倍でも1821年に兌換を再開し1914年まで保った。停止は取り付け（金の流出が準備を超える）と戦費で起きた。
    /// 盤面＝維持費0の英（払えた比1＝財政の門は開く）に年GDP の2倍の国債。(1) 兌換中なら止めない (2) 停止中なら戻れる
    /// (3) 対照＝同じ盤面で準備が割れる（取り付け）と止める＝門そのものは生きている。</summary>
    [Fact]
    public void 債務が年GDPの2倍の英も取り付けが無ければ兌換を止めず_停止中なら戻れる()
    {
        World Board(bool on)
        {
            var w = QuietBritain();
            w.MonetaryLaw = MonetaryStandardLaw.Gold; w.Policy.MonetaryTarget = 2;
            w.OnGoldStandard = on;
            w.GovDebt = 2.0 * w.NominalGdp * Calendar.MonthsPerYear; w.ForeignDebt = 0;
            return w;
        }
        double Coverage(World w) => w.BaseMoney / w.BroadMoney;
        var keep = Board(true);
        Assert.True(keep.MonetaryEnabled && keep.NominalGdp > 0, "前提が崩れた：英に貨幣経済が無い");
        Assert.True(Coverage(keep) >= keep.Coef.GoldCoverMin * keep.Coef.GoldRejoinCoverMargin, $"前提が崩れた：準備が薄い（カバー率 {Coverage(keep)}）");
        Fiscal.Step(keep, keep.Coef);
        Assert.Equal(1.0, keep.FiscalFundingRatio);
        Assert.True(keep.GovDebt > 1.5 * keep.NominalGdp * Calendar.MonthsPerYear, "前提が崩れた：債務が年GDP の2倍に届いていない");
        Assert.True(keep.OnGoldStandard, "債務が年GDP の2倍の英が、取り付けも財政不足も無いのに兌換を止めた");

        var back = Board(false);
        Fiscal.Step(back, back.Coef);
        Assert.True(back.OnGoldStandard, "債務が年GDP の2倍の英が、準備と財政が足りているのに兌換へ戻れない（英1821年の再開）");

        var run = Board(true);
        run.Bank!.Loans = run.TotalMoney + run.Treasury;   // 準備（基礎貨幣）が与信に食われて0＝取り付けで金が流出し尽くした盤面
        Fiscal.Step(run, run.Coef);
        Assert.False(run.OnGoldStandard, "準備が割れても兌換を止めない（取り付けの門が死んでいる＝この番人の対照が崩れた）");
    }

    // ★国債の金利を史実から出し直した（2026-09-29・作者裁定＝(94) の申し送り1 の (i)）の番人の盤面：静かな英に、金利の式が読む量だけを置く
    //   （兌換・議会の予算の掌握・期待インフレ0・政策金利0・国債＝年名目GDP の debtYears 倍）。r＝1・余剰0＝借りも返しもしない月＝国債は動かない。
    private static World BondBoard(double conv, double parliament, double debtYears)
    {
        var w = QuietBritain();
        w.Convertibility = conv; w.ParliamentControl = parliament;
        w.ExpectedInflation = 0; w.InterestRate = 0;
        w.GovDebt = debtYears * w.NominalGdp * Calendar.MonthsPerYear; w.ForeignDebt = 0;
        w.FiscalFundingRatio = 1.0;
        return w;
    }

    /// <summary>★債務比の上乗せは史実から（BondRateDebtScale＝英のコンソル）：国債÷年名目GDP が2.5 の信認のある国（完全兌換・議会が予算を握る・切り下げなし）の金利は
    /// 約4〜5%（英1815〜20 のコンソル約4.5%）。不換・議会の弱い周縁国は国債÷年GDP 1.5 で6〜10%（西・葡・希・オスマン・ラテンアメリカ）。
    /// 突然変異：旧の係数 1.44 へ戻す→信認のある国が上限 0.30 に張り付いて赤／分母を月次の名目GDP へ戻す（旧の DebtGdpAnnualized=0 の枝）→赤。</summary>
    [Fact]
    public void 国債の金利_債務が年GDPの2倍半でも信認のある国は4から5パーセントで不換の周縁国は6から10パーセント()
    {
        var credible = BondBoard(conv: 1.0, parliament: 1.0, debtYears: 2.5);
        Assert.True(credible.NominalGdp > 0, "前提が崩れた：英に名目GDPが無い");
        Fiscal.Step(credible, credible.Coef);
        Assert.True(credible.GovDebt >= 2.5 * credible.NominalGdp * Calendar.MonthsPerYear * (1 - 1e-12), "前提が崩れた：国債が動いた");
        Assert.InRange(credible.GovBondRate, 0.04, 0.05);

        var periphery = BondBoard(conv: 0.0, parliament: 0.0, debtYears: 1.5);
        Fiscal.Step(periphery, periphery.Coef);
        Assert.InRange(periphery.GovBondRate, 0.06, 0.10);
        Assert.True(periphery.GovBondRate > credible.GovBondRate, "債務の軽い不換の国が債務の重い信認のある国より安く借りている");
    }

    /// <summary>★信認の喪失＝**直近の切り下げの速さ**（この月の切り下げ×12÷年名目GDP×BondRateDistrustScale）＝累積でない（旧＝累積の切り下げ÷月次名目GDP×0.06＝
    /// 通貨改革まで減らず単独で上限 0.30 を越えた）。双子（同じ盤面）の片方だけ1か月、年GDP の5%の速さで切り下げる（Simulation と同じく累積にも積む）：
    /// ①その月の市場の金利の上乗せ＝0.5×5%＝2.5%（名目・年÷名目・年）②止めた翌月から市場の金利は双子と同じ（累積を読まない）
    /// ③払う利子（既往債の平均クーポン）の差は乗り換え（R9 の満期構造）とともに毎月薄まって0へ向かう＝記憶は払う利子の側が持つ（新しい状態なし）。
    /// 突然変異：累積の切り下げを読む→②が赤／分母を月次の名目GDP（×12 を落とす）→①が赤／速さを年に直さない→①が赤。</summary>
    [Fact]
    public void 国債の金利_信認の喪失は直近の切り下げの速さで止めた国の上乗せは乗り換えとともに薄まる()
    {
        var dev = BondBoard(conv: 0.0, parliament: 0.0, debtYears: 0.5);
        var ctl = BondBoard(conv: 0.0, parliament: 0.0, debtYears: 0.5);
        // 釣り合いの月（どちらも切り下げない）＝平均クーポンを市場の金利にそろえる
        Fiscal.Step(dev, dev.Coef); Fiscal.Step(ctl, ctl.Coef);
        Assert.Equal(ctl.GovBondRate, dev.GovBondRate);
        dev.GovAvgCoupon = dev.GovBondRate; ctl.GovAvgCoupon = ctl.GovBondRate;
        // ① 切り下げの月＝年GDP の5%の速さ（この月の額＝月次GDP の5%＝×12÷年GDP＝0.05）
        double devalue = 0.05 * dev.NominalGdp;
        dev.CumulativeDevaluation += devalue;
        Fiscal.Step(dev, dev.Coef, 0, devalue); Fiscal.Step(ctl, ctl.Coef);
        double premium = dev.GovBondRate - ctl.GovBondRate;
        Assert.True(Math.Abs(premium - dev.Coef.BondRateDistrustScale * 0.05) <= 1e-12, $"切り下げの月の上乗せ {premium} が 0.5×年GDP の5%＝{dev.Coef.BondRateDistrustScale * 0.05} でない（速さか年の単位が崩れた）");
        Assert.True(premium > 0.02 && premium < 0.03, $"上乗せ {premium}（GDP 比1%につき約0.5%）");
        // ② 止めた翌月から市場の金利は双子と同じ ③ 払う利子の差は毎月薄まる
        double prevGap = double.PositiveInfinity;
        for (int m = 0; m < 24; m++)
        {
            Fiscal.Step(dev, dev.Coef); Fiscal.Step(ctl, ctl.Coef);
            Assert.Equal(ctl.GovBondRate, dev.GovBondRate);
            double gap = dev.GovAvgCoupon - ctl.GovAvgCoupon;
            Assert.True(gap > 0, $"{m + 1} か月目：切り下げた月の金利で乗り換えた分が平均クーポンに残っていない（差 {gap}）");
            Assert.True(gap < prevGap, $"{m + 1} か月目：平均クーポンの差 {gap} が薄まらない（前の月 {prevGap}）");
            prevGap = gap;
        }
        Assert.True(prevGap < dev.Coef.BondRolloverFrac * premium, "2年たっても払う利子の上乗せが乗り換え1か月分より大きい");
    }

    /// <summary>★単位の対称：同じ実体で物価水準が2倍の双子（名目の量＝国債・切り下げの額・名目GDP が全て2倍）は同じ金利（債務÷年名目GDP・切り下げの速さ÷年名目GDP＝名目÷名目）。
    /// 突然変異：信認の喪失か債務比の分母を実質GDP にする→赤。</summary>
    [Fact]
    public void 国債の金利_物価水準が2倍の双子は同じ金利()
    {
        var a = BondBoard(conv: 0.3, parliament: 0.5, debtYears: 1.0);
        var b = BondBoard(conv: 0.3, parliament: 0.5, debtYears: 1.0);
        double n0 = b.NominalGdp;
        b.PriceLevel *= 2; b.GovDebt *= 2; b.LastTariffRevenue *= 2;
        Assert.True(Math.Abs(b.NominalGdp - 2 * n0) <= 1e-12 * n0, $"前提が崩れた：名目GDPが2倍にならない（{n0}→{b.NominalGdp}）");
        double devalue = 0.03 * n0;   // 年GDP の3%の速さ（月次GDP の3%×12÷年GDP）＝信認の喪失 0.5×0.03（上限 0.30 の手前で比べる）
        Fiscal.Step(a, a.Coef, 0, devalue);
        Fiscal.Step(b, b.Coef, 0, 2 * devalue);
        Assert.True(a.GovBondRate < a.Coef.BondRateMax, $"前提が崩れた：上限に張り付いた（{a.GovBondRate}）");
        Assert.True(Math.Abs(a.GovBondRate - b.GovBondRate) <= 1e-12, $"物価水準2倍の双子の金利 {b.GovBondRate} が {a.GovBondRate} と違う（実質の物差しが混じった）");
    }

    /// <summary>★資本市場の債務の物差しは**年の**名目GDP（2026-09-29・棚卸し fis-02）＝国債の金利の債務比と同じ単位。旧＝月次名目GDP 比（DebtDistressRatio 2.0＝年に直すと約0.17）
    /// ＝国債÷年GDP が史実の水準の国は対外信用の債務の項が0＝外資が逃げた。盤面＝外交を止めた英仏の構築直後に、英の国債＝年名目GDP の n 倍を置いて四半期末の国際金融を1回。
    /// ①債務の項＝1−n÷DebtDistressRatio（年GDP の1倍で 0.6〜0.7）②仏1789（0.7）で 0.7〜0.85・英1815（2.5）でも0より大きい ③物価水準が2倍の双子（名目を全て2倍）は同じ項。
    /// 突然変異：分母を月次に戻す（×12 を落とす）→①②が赤／DebtDistressRatio を旧の 2.0 へ→①②が赤／分母を実質GDP にする→③が赤。</summary>
    [Fact]
    public void 対外信用の債務の項は年の名目GDPで測り英1815の債務でも尽きない()
    {
        static double Term(double years, bool doublePrice)
        {
            var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent()) { DiplomacyFrozen = true };
            var br = sim.Nations[0].World;
            double n0 = br.NominalGdp;
            Assert.True(n0 > 0, "前提が崩れた：英の名目GDPが立っていない");
            if (doublePrice)
            {
                br.PriceLevel *= 2; br.LastTariffRevenue *= 2;
                Assert.True(Math.Abs(br.NominalGdp - 2 * n0) <= 1e-12 * n0, $"前提が崩れた：名目GDPが2倍にならない（{n0}→{br.NominalGdp}）");
            }
            br.GovDebt = years * br.NominalGdp * Calendar.MonthsPerYear; br.ForeignDebt = 0;
            CapitalMarket.Step(sim.Nations, new Coefficients());
            return br.CreditDebtTerm;
        }
        double c = new Coefficients().DebtDistressRatio;
        double one = Term(1.0, false);
        Assert.True(Math.Abs(one - (1.0 - 1.0 / c)) <= 1e-12, $"年GDP の1倍の国債の債務の項 {one} が 1−1÷{c} でない（物差しが年の名目GDP でない）");
        Assert.InRange(one, 0.6, 0.7);
        Assert.InRange(Term(0.7, false), 0.7, 0.85);
        Assert.True(Term(2.5, false) > 0, "英1815（国債÷年GDP 約2.5）で対外信用の債務の項が尽きた");
        Assert.True(Math.Abs(Term(1.0, true) - one) <= 1e-12, "物価水準2倍の双子の債務の項が違う（実質の物差しが混じった）");
    }

    /// <summary>★債務÷GDP の物差しは1本＝**年の名目GDP**（2026-09-29・棚卸し fis-02 の残り＝B13）：Government の緊縮の強度・PolicyPreferences の国家の歳入要求・
    /// Credit の銀行の評価損を、国債の金利（Fiscal の dGdp）と資本市場の対外信用と同じ単位で測る。旧＝緊縮と歳入要求は**月次実質**・評価損は**月次名目**
    /// ＝緊縮は年GDP の約1.7% で始まり約8% で満額・歳入要求と評価損は年GDP の約8% で飽和（1783年の英仏蘭米は開幕から緊縮1＝三部会の召集の信号が立った）。
    /// 盤面＝外交を止めた仏英の構築直後に、仏の国債＝年名目GDP の n 倍を置く。
    /// ①緊縮の強度＝(n−AusterityStartDebt)÷AusterityRange＝n=0.3 で0・仏1788 の0.7 で 4/7（三部会の召集の信号 0.5 を越える）・1.0 で1
    /// ②国家の歳入要求（法の支持の内訳の「国庫」・支払い率1）＝0.5＋min(1, n)＝n=0.25 で0.75・0.5 で1.0・2.0 で1.5
    /// ③銀行の評価損（国債の価格0.5）＝n=0.5 の国は n=1 の国より信頼を削らない・n=1 と 2 は同じ（年GDP の1倍で頭打ち）
    /// ④物価水準が2倍の双子（名目の国債も2倍）は①②が同じ。
    /// 突然変異：Government の×12 を落とす→①が赤／PolicyPreferences の×12 を落とす→②が赤／Credit の×12 を落とす→③が赤／
    /// Government・PolicyPreferences の分母を実質GDP にする→④が赤。</summary>
    [Fact]
    public void 緊縮と歳入要求と銀行の評価損は年の名目GDPで測る()
    {
        static World Board(double years, bool doublePrice)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789, _ => new NullAgent()) { DiplomacyFrozen = true };
            var w = sim.Nations[0].World;
            w.FiscalEnabled = true; w.PoliticalLockEnabled = false;
            double n0 = w.NominalGdp;
            Assert.True(n0 > 0 && w.MonetaryEnabled && w.Bank is not null, "前提が崩れた：仏の名目GDP・貨幣・銀行が立っていない");
            if (doublePrice)
            {
                w.PriceLevel *= 2;
                Assert.True(Math.Abs(w.NominalGdp - 2 * n0) <= 1e-12 * n0, $"前提が崩れた：名目GDPが2倍にならない（{n0}→{w.NominalGdp}）");
            }
            w.GovDebt = years * w.NominalGdp * Calendar.MonthsPerYear; w.ForeignDebt = 0;
            w.FiscalFundingRatio = 1;
            return w;
        }
        static double Austerity(double years, bool doublePrice = false) { var w = Board(years, doublePrice); Government.Step(w, w.Coef); return w.AusterityIntensity; }
        static double StateDemand(double years, bool doublePrice = false)
        {
            var w = Board(years, doublePrice);
            foreach (var (who, v) in PolicyPreferences.SupportBreakdown(w, PolicyAxis.IncomeTax, true)) if (who == "国庫") return v;
            Assert.Fail("前提が崩れた：所得税の支持の内訳に国庫が無い");
            return double.NaN;
        }
        static double Confidence(double years)
        {
            var w = Board(years, false);
            w.GovBondRate = 2 * w.Coef.BondBaseRate;   // 国債の価格＝基礎金利÷国債の金利＝0.5
            Credit.Step(w, w.Coef, (w.NominalGdp, 1.0, w.MeanIndustrialization));
            return w.Bank!.Confidence;
        }
        var c = new Coefficients();
        // ① 緊縮の強度
        Assert.True(Austerity(0.3) <= 1e-9, $"年GDP の0.3倍の国債で緊縮が立った（{Austerity(0.3)}＝物差しが年の名目GDP でない）");
        double fr = Austerity(0.7);
        Assert.True(Math.Abs(fr - (0.7 - c.AusterityStartDebt) / c.AusterityRange) <= 1e-9, $"仏1788（0.7）の緊縮の強度 {fr} が (0.7−開始)÷幅 でない");
        Assert.True(fr > 0.5, $"仏1788（0.7）で三部会の召集の信号（緊縮 0.5 超）が立たない（{fr}）");
        Assert.Equal(1.0, Austerity(1.0), 9);
        // ② 国家の歳入要求
        Assert.Equal(0.75, StateDemand(0.25), 9);
        Assert.Equal(1.0, StateDemand(0.5), 9);
        Assert.Equal(1.5, StateDemand(2.0), 9);
        // ③ 銀行の評価損
        double half = Confidence(0.5), one = Confidence(1.0), two = Confidence(2.0);
        Assert.True(half > one + 1e-6, $"国債が年GDP の0.5倍の銀行の信頼 {half} が1倍 {one} より高くない＝国債の多さが評価損に効かない（物差しが飽和している）");
        Assert.True(Math.Abs(one - two) <= 1e-12, $"年GDP の1倍と2倍で評価損が違う（{one}／{two}）＝頭打ちが年GDP の1倍でない");
        // ④ 物価水準2倍の双子
        Assert.Equal(Austerity(0.5), Austerity(0.5, true), 9);
        Assert.Equal(StateDemand(0.25), StateDemand(0.25, true), 9);
    }

    /// <summary>★減債は年に残高の約1%（減債基金・2026-09-29）＝余剰のある英でも10年で国債の1割しか返さない（史実＝英の国債は1783〜92 の平時に約2.4億ポンドのまま・
    /// 1816〜1914 も名目で年約0.3%しか減らない）。旧＝月に5%（年60%）＝余剰のある国は数年で返し切り、初期の国債（英 年GDP の約1.1）が開幕から3〜6年で0になった。
    /// 盤面＝静かな英に国債＝年名目GDP の1.1倍（全て国内の持ち手）・毎月 r＝1 で余剰は十分（Ample）を120か月。
    /// 突然変異：BondRepayFrac を旧の 0.05 へ戻す→赤。</summary>
    [Fact]
    public void 減債は年に残高の約1パーセントで余剰のある英も10年で国債の1割しか返さない()
    {
        var w = QuietBritain();
        double d0 = 1.1 * w.NominalGdp * Calendar.MonthsPerYear;
        w.GovDebt = d0; w.ForeignDebt = 0;
        Fiscal.SeedBondHoldings(w);
        for (int m = 0; m < 120; m++) FiscalMonth(w, 1, Ample);
        double left = w.GovDebt / d0;
        Assert.InRange(left, 0.85, 0.95);   // 年1% の10年＝(1−0.01/12)^120≒0.905
        new BondLedger().Check(w);
    }

    /// <summary>★開始前の助走（World.WarmupFrozen）は国債の残高を動かさない（2026-09-29）＝借りない・返さない・不履行へ数えない（人口・資本と同じ「年月が経って積み上がる量」）。
    /// 盤面＝静かな英に国債（年名目GDP の1倍・国内の持ち手）を置き、助走の印を立てた国と立てない双子で同じ月を踏む。
    /// ①払えない額があっても借りない ②余剰があっても返さない ③借入の上限の上で払えない月でも不履行の計数は進まない。対照（印なし）では①②③が起きる。
    /// 突然変異：借入の門を外す→①が赤／減債の門を外す→②が赤／不履行の計数の門を外す→③が赤。</summary>
    [Fact]
    public void 助走中は国債を借りも返しもせず不履行へ数えない()
    {
        static World Board(bool warm)
        {
            var w = QuietBritain();
            w.GovDebt = 1.0 * w.NominalGdp * Calendar.MonthsPerYear; w.ForeignDebt = 0;
            Fiscal.SeedBondHoldings(w);
            w.LastInterest = 0; w.LastRevenue = w.NominalGdp;   // 借入の上限の下
            w.WarmupFrozen = warm;
            return w;
        }
        // ① 借入
        var warm1 = Board(true);
        Assert.Equal(0.0, Fiscal.Borrow(warm1, warm1.Coef, -Ample));
        var ctl = Board(false);
        Assert.True(Fiscal.Borrow(ctl, ctl.Coef, -Ample) > 0, "対照が崩れた：助走の外で払えない額があっても借りない");
        // ② 減債（r＝1・余剰は十分）
        var warm = Board(true); double d0 = warm.GovDebt;
        warm.FiscalFundingRatio = 1; Fiscal.Step(warm, warm.Coef, Ample);
        Assert.Equal(d0, warm.GovDebt);
        var ctl2 = Board(false);
        ctl2.FiscalFundingRatio = 1; Fiscal.Step(ctl2, ctl2.Coef, Ample);
        Assert.True(ctl2.GovDebt < d0, "対照が崩れた：助走の外で余剰があっても返さない");
        // ③ 不履行の計数（借入の上限の上＝限界0・払えない月）
        var warm3 = Board(true); warm3.Coef.MaxInterestShareOfRevenue = 0;
        warm3.FiscalFundingRatio = Short; Fiscal.Step(warm3, warm3.Coef, -Ample);
        Assert.Equal(0, warm3.InsolvencyQuarters);
        var ctl3 = Board(false); ctl3.Coef.MaxInterestShareOfRevenue = 0;
        ctl3.FiscalFundingRatio = Short; Fiscal.Step(ctl3, ctl3.Coef, -Ample);
        Assert.Equal(1, ctl3.InsolvencyQuarters);
    }

    /// <summary>★外資の貸し手は**低金利の国ほど深い**（2026-09-29・向きの是正）：外資のプール＝開放度×ForeignBondCapacity×月次名目GDP×（基礎金利÷国債の金利・上限3）。
    /// 旧＝金利÷基礎金利＝金利が下がると外資が縮んだ（英のコンソルの国が最も浅い市場になる逆向き）。盤面＝静かな英の双子（国内の買い手0＝BondAppetite 0・
    /// 1件の額を大きく＝外資のプールで止まる）で、国債の金利だけを変えて同じ払えない額を借りる。
    /// ①金利 2% の国は 8% の国より多く借りる（比 4＝(0.04÷0.02)÷(0.04÷0.08)）②4% の国の半分が 8%・1% の国は上限3＝4% の国の3倍。
    /// 突然変異：旧の向き（金利÷基礎金利）へ戻す→①②が赤。</summary>
    [Fact]
    public void 外資の貸し手は低金利の国ほど深い()
    {
        static double Lend(double rate)
        {
            var w = QuietBritain();
            w.Coef.ForeignBondCapacity = new Coefficients().ForeignBondCapacity;
            w.Coef.BondAppetite = 0;                 // 国内の買い手0＝外資のプールだけ
            w.Coef.BorrowFractionOfGdp = 10;         // 1件の額＞外資のプール
            w.LastInterest = 0; w.LastRevenue = w.NominalGdp;   // 借入の上限の下
            w.GovBondRate = rate;
            return Fiscal.Borrow(w, w.Coef, -1e18);
        }
        double r1 = Lend(0.01), r2 = Lend(0.02), r4 = Lend(0.04), r8 = Lend(0.08);
        Assert.True(r4 > 0, "前提が崩れた：外資から借りられない盤面");
        Assert.True(r2 > r8, $"金利 2% の国（{r2}）が 8% の国（{r8}）より外資から借りられない");
        Assert.True(Math.Abs(r2 / r8 - 4.0) <= 1e-9, $"金利 2% と 8% の借入の比 {r2 / r8} が 4 でない");
        Assert.True(Math.Abs(r8 / r4 - 0.5) <= 1e-9, $"金利 8% の借入 {r8} が 4% の国 {r4} の半分でない");
        Assert.True(Math.Abs(r1 / r4 - 3.0) <= 1e-9, $"金利 1% の借入 {r1} が上限（4% の国の3倍）で止まっていない");
    }

    [Fact]
    public void 再併合_反乱国の与信と負の国庫は親へ渡り世界の貨幣は変わらない()
    {
        // 旧＝親へ渡すのは貯蓄と正の国庫だけ＝反乱国の与信と負の国庫が国ごと消え、世界の基礎貨幣がその額だけ湧いた（国ごとの保存は閉じる＝計器に掛からない）。
        // 再併合の1段（MultiNationSim.ReabsorbNations）だけを踏み、四半期の他の流れ（国外移民の持参金など）を挟まない。
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var pw = sim.Nations[0].World; var fw = sim.Nations[1].World;
        pw.PendingRebelRegions = [pw.Regions[1]]; pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        FiscalMonth(rw, Short);   // 起債は r＜1 の月（2026-09-28・エンジンの1行）
        sim.StepQuarter();   // 反乱国が自国の投資家から借りる＝国債の持ち手が立つ
        Assert.True(HeldOf(rw) > 0, "前提が崩れた：反乱国の国債の持ち手がいない");
        // 反乱国の貸借を、基礎貨幣を動かさない操作で立てる：信用創造（与信と預金の対）・国庫の当座借越での支出・家計の借入（Pop.Debt と HouseholdLoans の対）
        var rb = rw.Bank!; var pb = pw.Bank!;
        Pop cap = rw.Regions[0].Pops[0];
        foreach (var r in rw.Regions) foreach (var p in r.Pops) if (p.Savings > cap.Savings) cap = p;
        double scale = Math.Max(1.0, rw.TotalMoney);
        double loan = 0.3 * scale, overdraft = rw.Treasury + 0.2 * scale, hh = 0.05 * scale;
        rb.Loans += loan; cap.Savings += loan;
        rw.Pay(TreasuryCat.OtherOut, -overdraft); cap.Savings += overdraft;
        rb.HouseholdLoans += hh; cap.Debt += hh; cap.Savings += hh;
        // 請求権：反乱国の対外債（仏と英が3:1で持つ・国内元本は不変＝保有台帳は割れない）と、外資の負債・対外資産
        double fd = 0.1 * scale;
        rw.ForeignDebt += fd; rw.GovDebt += fd; rw.PrevForeignDebt += fd;
        fw.ForeignBondClaims += 0.75 * fd; pw.ForeignBondClaims += 0.25 * fd;
        rw.ForeignCapitalIn += 0.07 * scale; rw.CapitalAbroad += 0.04 * scale;
        new BondLedger().Check(rw);
        Assert.True(rw.Treasury < 0 && rb.Loans > 0 && rb.HouseholdLoans > 0, $"前提が崩れた：国庫 {rw.Treasury}・与信 {rb.Loans}・家計の与信 {rb.HouseholdLoans}");

        var all0 = sim.Nations.ConvertAll(n => n.World).ToArray();
        double world0 = WorldBase(all0), rebelBase = rw.BaseMoney, parentBase0 = pw.BaseMoney;
        double Sum(World[] ws, Func<World, double> f) { double s = 0; foreach (var w in ws) s += f(w); return s; }
        double ca0 = Sum(all0, w => w.CapitalAbroad), fci0 = Sum(all0, w => w.ForeignCapitalIn), claims0 = Sum(all0, w => w.ForeignBondClaims);
        double heldOthers = fw.ForeignBondClaims + pw.ForeignBondClaims, repudiated = rw.PrevForeignDebt, fwClaims0 = fw.ForeignBondClaims;
        Assert.True(heldOthers >= repudiated, $"前提が崩れた：反乱国以外の債権 {heldOthers} が否認する対外債 {repudiated} に届かない");
        double loans0 = pb.Loans, hh0 = pb.HouseholdLoans, tr0 = pw.Treasury, rT = rw.Treasury, rL = rb.Loans, rHH = rb.HouseholdLoans;
        var guard = new MoneyConservation(pw);
        var returning = new List<Region>(rw.Regions);

        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
        sim.ReabsorbNations();

        Assert.DoesNotContain(sim.Nations, n => ReferenceEquals(n.World, rw));
        var all1 = sim.Nations.ConvertAll(n => n.World).ToArray();
        double tol = 1e-9 * Math.Max(1.0, world0);
        // (b) 世界の貨幣は変わらない＝親の基礎貨幣の増え＝反乱国の基礎貨幣（貯蓄＋国庫−与信）
        Assert.True(Math.Abs(WorldBase(all1) - world0) <= tol, $"再併合で世界の基礎貨幣が {WorldBase(all1) - world0} 動いた");
        Assert.True(Math.Abs(pw.BaseMoney - parentBase0 - rebelBase) <= tol, $"親の基礎貨幣の増え {pw.BaseMoney - parentBase0} ≠ 反乱国の基礎貨幣 {rebelBase}");
        guard.Check(pw);   // 親の国ごとの保存も閉じる（累積貿易に同額を記帳）
        // 貨幣の行は親へ：与信・家計の与信・国庫（負のまま）
        Assert.True(Math.Abs(pb.Loans - loans0 - rL) <= tol, $"親の与信の増え {pb.Loans - loans0} ≠ 反乱国の与信 {rL}");
        Assert.True(Math.Abs(pb.HouseholdLoans - hh0 - rHH) <= tol, $"親の家計の与信の増え {pb.HouseholdLoans - hh0} ≠ 反乱国の家計の与信 {rHH}");
        Assert.True(Math.Abs(pw.Treasury - tr0 - rT) <= tol, $"親の国庫の動き {pw.Treasury - tr0} ≠ 反乱国の国庫 {rT}");
        Assert.Equal(hh, cap.Debt);   // 家計の債務の記録は POP と一緒に親へ（HouseholdLoans の対）
        // 請求権：外資の負債と対外資産は州について親へ（世界の和は不変）／反乱国の対外債は否認＝債権の和が同額減る（金は動かない）
        double tolC = 1e-9 * Math.Max(1.0, scale);
        Assert.True(Math.Abs(Sum(all1, w => w.CapitalAbroad) - ca0) <= tolC, "再併合で世界の対外資産の和が動いた");
        Assert.True(Math.Abs(Sum(all1, w => w.ForeignCapitalIn) - fci0) <= tolC, "再併合で世界の外資負債の和が動いた");
        Assert.True(Math.Abs(Sum(all1, w => w.ForeignBondClaims) - (claims0 - repudiated)) <= tolC, $"債権の和の減り {claims0 - Sum(all1, w => w.ForeignBondClaims)} ≠ 否認した対外債 {repudiated}");
        Assert.True(Math.Abs(fw.ForeignBondClaims - fwClaims0 * (1 - repudiated / heldOthers)) <= tolC, "否認が債権の比で落ちていない");
        // (c) 反乱国の国債の保有は消える（持ち手の損・親は継承しない）
        foreach (var r in returning) foreach (var p in r.Pops) Assert.Equal(0.0, p.BondHoldings);
        new BondLedger().Check(pw);
        sim.StepQuarter();   // 返った州と与信を含めて1四半期（エンジンの月次検査）
        new BondLedger().Check(pw);
    }
}
