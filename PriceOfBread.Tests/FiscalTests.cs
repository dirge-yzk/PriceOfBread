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
    //   赤字は増税でなく国家の支払い率 r の低下（全ての行に同じ r・2026-09-28）→借入→デフォルト（払えなかった大きさの累計が DefaultArrearsMonths に届く・(205)）で閉じる。
    [Fact]
    public void 低税では緊縮が発火し逆進課税の下限の門になる()   // §6.2（赤字→借入→債務→緊縮：債権者が奪うのは法を選ぶ自由であって値ではない）
    {
        var sim = new Simulation(Fisc(12345, 0.05), new NullAgent());
        // 低税で連合を賄えない国は r＜1 の月にエンジンが借りて凌ぐ（2026-09-28 借入の自動化＝借りる主体のいないこの世界でも同じ1行）→債務累積→緊縮が発火。
        // ⚠この世界は不換＝払えない分は月初に切り下げで埋まり（2026-09-28 から払える額の側）r＜1 の月が来ない＝借りない。
        //   借入→債務→緊縮の門を測るため、切り下げの余地を閉じる（兌換の国と同じ形）。
        sim.World.Coef.DevaluationCapFracPerMonth = new RatePerMonth(0);
        // ⚠緊縮の物差しは年の名目GDP（2026-09-29・史実＝年GDP の3割で始まる）。この小経済（歳入は年GDP の約1%）は借入の上限（利払い÷歳入 MaxInterestShareOfRevenue）で
        //   国債が年GDP の約5% までしか積めない（貸し手は歳入の小さい国に貸さない＝史実どおり）＝鎖（赤字→借入→債務→緊縮の門）を測るため、開始だけをこの世界の債務の桁へ下げる
        //   （物差しと史実の値の番人は 緊縮と歳入要求と銀行の評価損は年の名目GDPで測る）。
        sim.World.Coef.AusterityStartDebt = new RatioToGdpYear(0.01);
        // ★2026-09-29（支払いの一元化 段2-2・A5）：貸し手のいない対外借入は立たない＝単一国の世界は既定で外資0（国内の貸し手＝近代エリートの居ないこの世界では借りられない）。
        //   借入→債務→緊縮の門を測るため、外国の貸し手が居る盤面にする（共有層が四半期末に注入する引き受け余力の代わり＝この番人は国債の相手を測らない）。
        sim.World.ForeignBondInvestorPool = 1e30;
        // ★盤面の直し（2026-09-30 段3-2）：連合維持費は治安と行政の役務の給与（兵士の兵力・官僚の行政の④）になった＝この較正の世界には兵士も官僚もいない
        //   ＝維持費の受け手が無く国庫に残る＝低税でも赤字が立たない。赤字の源を開始の国債の利払いに置く（年GDP の5割・金利は国債の式のまま）
        //   ＝r＜1 の月にエンジンが借りて債務が積もる鎖はそのまま（主張＝緊縮は下限の門で上げる力を持たない、は変えない）。
        //   対外の債務として置く（国内の持ち手の台帳 Σ保有＝国内元本 を0のまま閉じる）・名目GDP が立つ1四半期の後に置く。
        sim.StepQuarter();
        sim.World.ForeignDebt = new MoneyStock(Calendar.PerMonthToPerYear(0.5 * sim.World.NominalGdpPerMonth).Value); sim.World.GovDebt = sim.World.ForeignDebt;
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
        Assert.True(sim.World.FiscalFundingRatio.Value > 0.9, $"連合が養えていない（funding {sim.World.FiscalFundingRatio.Value:P0}）");
        Assert.Equal(Polity.Absolutist, sim.World.Polity);   // 治安維持力が満たされ自由化を抑え込む＝絶対王政の維持
    }

    [Fact]
    public void 財政下でも基礎貨幣は保存される()
    {
        // ★2026-10-01 (148)：保存される量は**台帳を引いた基礎貨幣**（ConservationTests・Invariants と同じ＝基礎貨幣−累積貿易−鋳造−不換の発行）。
        //   旧＝生の基礎貨幣を比べた＝国庫が負になった月の与信の償却（非常発行＝台帳 CumulativeFiatIssued に同じ額）が起きない種でしか緑にならない
        //   （(147) の木でも種1・3〜7 は400四半期の中で不換の発行が約0.1%立つ・種2 だけが立たなかった＝(148) で年次の乱数が2つ減って種2 にも立った）。許しは同じ 1e-3。
        static double D(World w) => w.BaseMoney.Value - w.CumulativeTradeFlow.Value - w.CumulativeGoldMinted.Value - w.CumulativeFiatIssued.Value;
        var sim = new Simulation(Fisc(2, 0.30), new NullAgent());
        double base0 = D(sim.World);

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(D(sim.World) - base0) < 1e-3 * Math.Max(1, Math.Abs(base0)), $"基礎貨幣が保存されない（台帳を引いた量 {base0}→{D(sim.World)}・不換の発行 {sim.World.CumulativeFiatIssued}）");
    }

    [Fact]
    public void 同一シードで財政軌跡は決定論的()
    {
        var a = new Simulation(Fisc(3, 0.30), new NullAgent());
        var b = new Simulation(Fisc(3, 0.30), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Treasury.Value, b.World.Treasury.Value);
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
    //   ＝双子の片方だけ起債させるときは、借りない側を借入の冷却中（BorrowCooldownMonths＝2 か月＝月末の Step の減算の後に次の借入の門が読む）にして同じ r で踏む
    //   （冷却は Fiscal.Borrow の門と月末の減算しか読まない）。借りる額は払えない額（−spare）だけ・1件の額まで（2026-09-28）＝r＜1 の月に spare を省くと、
    //   払えない額が1件の額より大きい月（−Ample）として踏む＝満額の1件。
    //   ⚠2026-09-28 (b) から借入は月初の Fiscal.Borrow（r を決める前・切り下げより先）＝この補助は「月末の決算 Step → 次の月初の借入 Borrow」の順で踏む
    //   ＝借りた月の利払いは次の Step から（旧＝Step の中で利払いの後に借りた形と同じ量の流れ＝番人の双子の差がそのまま国債の購入になる）。
    private const double Short = 0.5, Ample = 1e18;
    private static void FiscalMonth(World w, double r, double? spare = null)
    {
        double sp = spare ?? (r < 1 ? -Ample : 0);
        w.FiscalFundingRatio = new Share(r); Fiscal.Step(w, w.Coef, sp);
        Fiscal.Borrow(w, w.Coef, sp);
    }
    private static double HeldOf(World w) { double h = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) h += p.BondHoldings.Value; return h; }

    /// <summary>★段3-1（2026-09-29）：所得税の基数＝確定した前の月の所得（LastEarnedPerMonth）。月次の財政より後に入る受け取り（輸出代金・運賃〔国際市場の清算は
    /// 全ての国の月次決算の後〕・領主の取り分）は Earned に入り、次の月の頭で LastEarnedPerMonth へ写る＝翌月の所得税の基数になる（旧＝今月の Earned＝翌月の頭で0へ戻り課税されなかった）。
    /// 盤面＝英の静かな盤面で所得税だけを立て、1つの POP だけに所得を置く。前の月の所得（LastEarnedPerMonth）なら課税され、今月の Earned だけでは課税されない。</summary>
    [Fact]
    public void 所得税は確定した前の月の所得に掛かる()
    {
        double Revenue(bool last)
        {
            var w = QuietBritain();
            w.TaxRate = new Share(0.1); w.Coef.TitheRate = new Share(0);
            foreach (var r in w.Regions) foreach (var q in r.Pops) { q.EarnedPerMonth = new MoneyPerMonth(0); q.LastEarnedPerMonth = new MoneyPerMonth(0); }
            Pop? p = null;
            foreach (var r in w.Regions) foreach (var q in r.Pops) if (p is null && !q.IsSoldier && q.Size > 1 && q.Savings.Value > 1e6) p = q;
            Assert.True(p is not null, "前提が崩れた：貯蓄の厚い POP が無い");
            if (last) p!.LastEarnedPerMonth = new MoneyPerMonth(1000); else p!.EarnedPerMonth = new MoneyPerMonth(1000);
            double t0 = w.TreasuryLedger[(int)TreasuryCat.Tax];
            FiscalMonth(w, 1.0);
            return w.TreasuryLedger[(int)TreasuryCat.Tax] - t0;
        }
        Assert.True(Revenue(last: true) > 1e-6, "前の月の所得（LastEarned）に所得税が掛からない");
        Assert.Equal(0.0, Revenue(last: false), 9);   // 今月の Earned は翌月の基数（二重に取らない）
    }

    /// <summary>英を1四半期温め、財政の国債以外の流れ（税・汚職の漏れ・福祉・年金・外債）を止めた盤面（連合維持費は 2026-09-30 (135) に撤去＝止める行が無い）。
    /// 同じ種で2つ作れば双子＝片方だけで起債すれば、2つの貯蓄の差が国債の流れ（購入・利払い・償還）そのものになる。</summary>
    private static World QuietBritain()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.TaxRate = new Share(0); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;   // 歳入0＝汚職の漏れも0
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;                    // 福祉・年金（借入スタックの出費上限に依存＝双子で食い違う）を止める
        w.Coef.ForeignBondCapacity = new Share(0);                                        // 全額を国内の投資家が引き受ける
        // 初期の国債（world.json の govDebt＝1783年の英の国債 年GDP の約1.1・2026-09-29）と保有を消す＝国債の流れは番人が起こしたものだけ（金は動かない＝請求権と記録だけ）
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0);
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.BondHoldings = new MoneyStock(0);
        // 歳入の基数だけを立てる（関税の実収の診断値＝Fiscal.Step の中では予算の基数にだけ入り金を動かさない＝双子の差は国債の流れのまま）。
        //   借入の上限＝利払い÷歳入（2026-09-28）＝歳入0の国は借りられない＝税を止めたこの盤面でも借入の門を開けるため。
        w.LastTariffRevenuePerClearing = (w.Coef.MonthsPerClearing * w.NominalGdpPerMonth).Value;
        return w;
    }

    [Fact]
    public void 国債台帳_貯蓄の多い資本家は多く買い往復で富が移らない()
    {
        var t = QuietBritain(); var b = QuietBritain();   // 双子（b だけ起債する）
        Assert.Equal(0.0, b.GovDebt.Value);
        t.Coef.BondRepayFracPerMonth = new RatePerMonth(0); b.Coef.BondRepayFracPerMonth = new RatePerMonth(0);   // 1か月目は買うだけ
        t.BorrowCooldownMonths = 2;                                   // t は借りない（同じ r で踏む・月末の減算の後に借入の門が読む＝2）
        FiscalMonth(t, Short); FiscalMonth(b, Short);
        var tp = AllPopsOf(t); var bp = AllPopsOf(b);
        double domestic = b.GovDebt.Value - b.ForeignDebt.Value;
        Assert.True(domestic > 0, "前提が崩れた：起債が立っていない");
        double tol = 1e-9 * domestic;
        double invSav = 0; foreach (var p in tp) if (CapitalMarket.IsInvestor(p)) invSav += Math.Max(0, p.Savings.Value);
        var paid = new double[bp.Count];
        double minPerHead = double.MaxValue, maxPerHead = 0;
        for (int i = 0; i < bp.Count; i++)
        {
            paid[i] = tp[i].Savings.Value - bp[i].Savings.Value;   // 双子の差＝このPOPが国債に払った額
            Assert.True(Math.Abs(bp[i].BondHoldings.Value - paid[i]) <= tol, $"{bp[i].ClassName}：保有 {bp[i].BondHoldings} ≠ 払った額 {paid[i]}");
            double expect = CapitalMarket.IsInvestor(tp[i]) ? domestic * Math.Max(0, tp[i].Savings.Value) / invSav : 0;
            Assert.True(Math.Abs(bp[i].BondHoldings.Value - expect) <= tol, $"{bp[i].ClassName}：保有 {bp[i].BondHoldings} が貯蓄比 {expect} でない");
            if (paid[i] > tol && bp[i].Size > 0) { minPerHead = Math.Min(minPerHead, paid[i] / bp[i].Size); maxPerHead = Math.Max(maxPerHead, paid[i] / bp[i].Size); }
        }
        Assert.True(maxPerHead > 2 * minPerHead, $"前提が崩れた：1人あたりの購入額がほぼ一様（{minPerHead:G4}〜{maxPerHead:G4}）＝頭数比の償還でも富が移らない盤面");
        new BondLedger().Check(b);

        // 2か月目：全額を償還（減債率1）＋その月の利払い。受け取りは保有比例＝各持ち手の純益÷払った額が全員同じ（＝利回り）。
        t.Coef.BondRepayFracPerMonth = new RatePerMonth(1.0); b.Coef.BondRepayFracPerMonth = new RatePerMonth(1.0);
        FiscalMonth(t, 1, Ample); FiscalMonth(b, 1, Ample);
        Assert.True(b.GovDebt.Value <= tol, $"前提が崩れた：全額償還できていない（残 {b.GovDebt}）");
        double gain = 0, paidSum = 0;
        for (int i = 0; i < bp.Count; i++) { gain += bp[i].Savings.Value - tp[i].Savings.Value; paidSum += paid[i]; }
        Assert.True(gain > 0, "利払いが持ち手に届いていない");
        for (int i = 0; i < bp.Count; i++)
        {
            Assert.True(Math.Abs(bp[i].BondHoldings.Value) <= tol, $"{bp[i].ClassName}：全額償還の後に保有 {bp[i].BondHoldings} が残る");   // 往復で台帳は空に戻る（浮動小数の端数まで）
            double net = bp[i].Savings.Value - tp[i].Savings.Value;   // 往復の純益＝−払った額＋利子＋元本
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
        b.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        FiscalMonth(b, Short);
        double g0 = b.GovDebt.Value - b.ForeignDebt.Value;
        Assert.True(g0 > 2e3, $"前提が崩れた：起債が小さい（{g0}）＝丸めの残り（相対1e-11）が旧の許容の床 1e-8 を越える盤面にならない");
        // 最大時の桁の丸めの残り（g0 の相対 1e-11＝旧の許容 1e-8 より3桁小さい）を最大の持ち手に置く＝全長で積もる丸めの形
        Pop? top = null;
        foreach (var p in AllPopsOf(b)) if (p.BondHoldings.Value > 0 && (top is null || p.BondHoldings > top.BondHoldings)) top = p;
        Assert.NotNull(top);
        double drift = 1e-11 * g0;
        top!.BondHoldings += new MoneyStock(drift);
        new BondLedger().Check(b);   // 最大時＝どちらの尺度でも割れない
        // 半分ずつ返す（r＝1 の余剰から・減債率 0.5）＝国債が最大時の 1e-4 を割るまで
        b.Coef.BondRepayFracPerMonth = new RatePerMonth(0.5);
        for (int m = 0; m < 60 && b.GovDebt.Value > 1e-4 * g0; m++) FiscalMonth(b, 1, Ample);
        double held = HeldOf(b), dom = b.GovDebt.Value - b.ForeignDebt.Value;
        Assert.True(b.GovDebt.Value <= 1e-4 * g0 && b.GovDebt.Value > 0, $"前提が崩れた：国債が返されていない（{b.GovDebt}・最大 {g0}）");
        Assert.True(Math.Abs(held - dom - drift) <= 1e-3 * drift, $"比例の償還がズレを絶対値のまま保っていない（{held - dom} 対 {drift}）");
        double oldScale = Math.Max(held, Math.Max(Math.Abs(b.GovDebt.Value), Math.Abs(b.ForeignDebt.Value)));
        Assert.True(Math.Abs(held - dom) > 1e-8 * Math.Max(1.0, oldScale), "前提が崩れた：旧の尺度（今の残高）でも割れない盤面");
        new BondLedger().Check(b);   // 丸めの残り＝割れない（台帳を通った流れ≒2×最大時の桁）
        // 運び忘れ＝保有の総額の1%が消える（返し終えつつある小さな国債でも、流れの桁の1e-8 より何桁も大きい）
        top.BondHoldings -= new MoneyStock(0.01 * held);
        Assert.Throws<InvariantViolationException>(() => new BondLedger().Check(b));
    }

    [Fact]
    public void 国債台帳_利払いと償還は保有比例で買い手でない持ち手も受け取る()
    {
        var t = QuietBritain(); var b = QuietBritain();
        t.Coef.BondRepayFracPerMonth = new RatePerMonth(0); b.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        t.BorrowCooldownMonths = 2;                                   // t は借りない（同じ r で踏む・月末の減算の後に借入の門が読む＝2）
        FiscalMonth(t, Short); FiscalMonth(b, Short);
        // 双子に同じ徴兵を1回＝最大の持ち手の一部が兵士POPへ（保有は貯蓄と同じ1人あたりで運ばれる＝兵士は買い手でないのに持ち手になる）
        var r0t = t.Regions[0]; var r0b = b.Regions[0];
        int k = -1; for (int i = 0; i < r0b.Pops.Count; i++) if (!r0b.Pops[i].IsSoldier && (k < 0 || r0b.Pops[i].BondHoldings > r0b.Pops[k].BondHoldings)) k = i;
        int s = r0b.Pops.FindIndex(x => x.IsSoldier);
        Assert.True(k >= 0 && s >= 0 && r0b.Pops[k].BondHoldings.Value > 0, "前提が崩れた：首都州に持ち手か兵士POPがいない");
        double h0 = r0b.Pops[k].BondHoldings.Value, sz0 = r0b.Pops[k].Size;
        Military.MoveCohort(r0t.Pops[k], r0t.Pops[s], r0t.Pops[k].Size * 0.2, t.Coef);
        Military.MoveCohort(r0b.Pops[k], r0b.Pops[s], r0b.Pops[k].Size * 0.2, b.Coef);
        double drafted = sz0 - r0b.Pops[k].Size;
        Assert.True(drafted > 0, "前提が崩れた：徴兵が起きていない");
        Assert.True(Math.Abs(r0b.Pops[s].BondHoldings.Value - h0 * drafted / sz0) <= 1e-9 * h0, "徴兵で保有が1人あたりで運ばれていない");
        Assert.False(CapitalMarket.IsInvestor(r0b.Pops[s]));
        new BondLedger().Check(b);

        // 2か月目：既定の減債率で部分償還＋利払い。受け取り（双子の差の増分）も保有の減り方も保有比例。
        double repayFrac = new Coefficients().BondRepayFracPerMonth.Value;
        t.Coef.BondRepayFracPerMonth = new RatePerMonth(repayFrac); b.Coef.BondRepayFracPerMonth = new RatePerMonth(repayFrac);
        var tp = AllPopsOf(t); var bp = AllPopsOf(b);
        var h = new double[bp.Count]; var diff0 = new double[bp.Count]; double H = 0;
        for (int i = 0; i < bp.Count; i++) { h[i] = bp[i].BondHoldings.Value; diff0[i] = bp[i].Savings.Value - tp[i].Savings.Value; H += h[i]; }
        FiscalMonth(t, 1, Ample); FiscalMonth(b, 1, Ample);
        double cash = 0, Hafter = 0;
        for (int i = 0; i < bp.Count; i++) { cash += (bp[i].Savings.Value - tp[i].Savings.Value) - diff0[i]; Hafter += bp[i].BondHoldings.Value; }
        Assert.True(cash > 0 && Hafter < H, "前提が崩れた：利払い・償還が起きていない");
        double tol = 1e-9 * H;
        for (int i = 0; i < bp.Count; i++)
        {
            double got = (bp[i].Savings.Value - tp[i].Savings.Value) - diff0[i];
            Assert.True(Math.Abs(got - cash * h[i] / H) <= tol, $"{bp[i].ClassName}：受け取り {got} が保有比 {cash * h[i] / H} でない");
            Assert.True(Math.Abs(bp[i].BondHoldings.Value - h[i] * Hafter / H) <= tol, $"{bp[i].ClassName}：保有が同じ比で減っていない（{h[i]}→{bp[i].BondHoldings}）");
        }
        double soldierGot = (r0b.Pops[s].Savings.Value - r0t.Pops[s].Savings.Value) - diff0[s];
        Assert.True(soldierGot > 0, "買い手でない持ち手（兵士POP）が利子・元本を受け取っていない");
        new BondLedger().Check(b);
    }

    [Fact]
    public void 国債台帳_デフォルトで保有が消え国庫へは戻らない()
    {
        var w = QuietBritain();
        w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        FiscalMonth(w, Short);
        double H = HeldOf(w);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        double treasury0 = w.Treasury.Value, base0 = w.BaseMoney.Value;
        w.Coef.MaxInterestShareOfRevenue = new Share(0);                 // 借入上限に張り付き（利払い÷歳入≧限界＝借りられない）
        w.InsolvencyMonths = w.Coef.DefaultArrearsMonths;   // 払えなかった大きさの累計が線に届いている＝この月にデフォルト（(205)）
        FiscalMonth(w, Short);                                // 払えない月（r＜1）＝借りられずに払えない月が続いた
        Assert.True(w.HadSovereignDefault, "前提が崩れた：デフォルトが起きていない");
        Assert.Equal(0.0, w.GovDebt.Value);
        foreach (var p in AllPopsOf(w)) Assert.Equal(0.0, p.BondHoldings.Value);   // 損は持ち手＝保有は消える
        new BondLedger().Check(w);
        // 帳消しの額は国庫へ戻らない（国庫はその月の利払いぶん減るだけ）＝台帳の消滅は金を1円も動かさない
        Assert.True(w.Treasury.Value <= treasury0, $"帳消しの分が国庫へ戻っている（{treasury0}→{w.Treasury}・保有 {H}）");
        Assert.True(Math.Abs(w.BaseMoney.Value - base0) <= 1e-9 * Math.Max(1.0, Math.Abs(base0)), $"デフォルトで基礎貨幣が動いた（{base0}→{w.BaseMoney}）");
    }

    [Fact]
    public void 国債台帳_台帳は金を動かさず誰に渡るかだけを決める()
    {
        // 3つの双子：x＝そのまま／y＝保有を買い手でない1つのPOPへ寄せる（和は同じ）／z＝台帳を空にする（誰も読まれない＝払いは国庫に残る）
        var x = QuietBritain(); var y = QuietBritain(); var z = QuietBritain();
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFracPerMonth = new RatePerMonth(0); FiscalMonth(w, Short); }
        double H = HeldOf(y);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        var ypops = AllPopsOf(y); var xpops = AllPopsOf(x);
        int j = ypops.FindIndex(p => !p.IsSoldier && !CapitalMarket.IsInvestor(p));
        foreach (var p in ypops) p.BondHoldings = new MoneyStock(0);
        ypops[j].BondHoldings = new MoneyStock(H);
        foreach (var p in AllPopsOf(z)) p.BondHoldings = new MoneyStock(0);
        double repayFrac = new Coefficients().BondRepayFracPerMonth.Value;
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFracPerMonth = new RatePerMonth(repayFrac); FiscalMonth(w, 1, Ample); }
        Assert.Equal(x.GovDebt.Value, y.GovDebt.Value);    // 元本の動きは台帳を読まない
        Assert.Equal(x.GovDebt.Value, z.GovDebt.Value);
        Assert.Equal(x.Treasury.Value, y.Treasury.Value);  // 国庫の出入りも同じ（持ち手がいる限り）
        double scale = Math.Max(1.0, Math.Abs(x.BaseMoney.Value));
        Assert.True(Math.Abs((x.BaseMoney - y.BaseMoney).Value) <= 1e-9 * scale, $"台帳の中身で貨幣の総量が変わった（{x.BaseMoney} / {y.BaseMoney}）");
        Assert.True(Math.Abs((x.BaseMoney - z.BaseMoney).Value) <= 1e-9 * scale, $"空の台帳で貨幣の総量が変わった（{x.BaseMoney} / {z.BaseMoney}）");
        Assert.True(z.Treasury.Value > x.Treasury.Value, "持ち手のいない国内分が国庫に残っていない");
        Assert.True(ypops[j].Savings.Value > xpops[j].Savings.Value, "寄せた持ち手が利子・元本を受け取っていない");
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
        FiscalMonth(sim.World, Short);   // ★(616) 国庫の出口（線を超えた国庫は国債の返済へ）で 40 四半期の間に国債が返りきりうる＝最後にもう 1 度起債して持ち手のいる世界で台帳を検める（確かめるのは台帳の運びで、出口ではない）
        var w = sim.World;
        Assert.True(HeldOf(w) > 0, "前提が崩れた：国内債の持ち手がいない");
        new BondLedger().Check(w);
        // 直接の2経路：階級間の移動（Military.MoveCohort の通常枝）と州間移動（Territory.MoveBetweenRegions）は1人あたりで運ぶ
        Pop? from = null; foreach (var p in AllPopsOf(w)) if (!p.IsSoldier && (from is null || p.BondHoldings > from.BondHoldings)) from = p;
        var reg = w.Regions.Find(r => r.Pops.Contains(from!))!;
        var to = reg.Pops.Find(p => !p.IsSoldier && !ReferenceEquals(p, from) && p.ClassId == PopClass.Worker)!;
        double hf = (from!.BondHoldings).Value, sf = from.Size, ht = to.BondHoldings.Value;
        Military.MoveCohort(from, to, sf * 0.1, w.Coef);
        double moved = sf - from.Size;
        Assert.True(Math.Abs(to.BondHoldings.Value - ht - hf * moved / sf) <= 1e-9 * hf, "階級間の移動で保有が1人あたりで運ばれていない");
        new BondLedger().Check(w);
        var other = w.Regions.Find(r => !ReferenceEquals(r, reg) && r.Pops.Exists(p => p.ClassId == to.ClassId && !p.IsSoldier))!;
        var dp = other.Pops.Find(p => p.ClassId == to.ClassId && !p.IsSoldier)!;
        double h1 = to.BondHoldings.Value, s1 = to.Size, hd = dp.BondHoldings.Value;
        Territory.MoveBetweenRegions(to, dp, s1 * 0.1, w.Coef);
        Assert.True(Math.Abs(dp.BondHoldings.Value - hd - h1 * 0.1) <= 1e-9 * Math.Max(1.0, h1), "州間移動で保有が1人あたりで運ばれていない");
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
        c.JobSearchRatePerQuarter = new RatePerQuarter(0); c.PettyEntryRatePerQuarter = new RatePerQuarter(0); c.ArtisanDeclineRatePerQuarter = new RatePerQuarter(0);
        c.MiddleFallRatePerQuarter = new RatePerQuarter(0); c.UpperFallRatePerQuarter = new RatePerQuarter(0); c.BankruptcyFallRatePerQuarter = new RatePerQuarter(0); c.CapitalRiseRatePerQuarter = new RatePerQuarter(0);
        var worker = r.Pops[r.MeritFromIndex];
        worker.Literacy = 1.0; worker.EarnedPerMonth = new MoneyPerMonth(1.0 * worker.Size);
        foreach (var p in r.Pops) if (p.IsModernElite) { p.EducationFill = 1.0; }
        foreach (var idx in new[] { r.MeritToIndex, r.TeacherPopIndex, r.BureaucratPopIndex, r.DoctorPopIndex })
            if (idx >= 0 && r.Pops[idx].Size > 1e-9) r.Pops[idx].EarnedPerMonth = new MoneyPerMonth(10.0 * r.Pops[idx].Size);
        foreach (var reg in w.Regions) foreach (var p in reg.Pops) p.BondHoldings = new MoneyStock(0);
        const double perHead = 1000.0;
        worker.BondHoldings = new MoneyStock(perHead * worker.Size);   // 移動元だけを持ち手にする（和は検査しない＝運搬の向きだけを見る盤面）
        double s0 = worker.Size, h0 = worker.BondHoldings.Value;
        Meritocracy.Step(w, c);
        double moved = s0 - worker.Size;
        Assert.True(moved > 1e-9, "前提が崩れた：昇格が起きていない");
        Assert.True(Math.Abs((worker.BondHoldings / worker.Size).Value - perHead) <= 1e-9 * perHead,
            $"移動元に残る1人あたりの保有が変わった（{perHead}→{worker.BondHoldings / worker.Size}）＝移動者の保有が運ばれていない");
        double arrived = 0; foreach (var reg in w.Regions) foreach (var p in reg.Pops) if (!ReferenceEquals(p, worker)) arrived += p.BondHoldings.Value;
        Assert.True(Math.Abs(arrived - h0 * moved / s0) <= 1e-9 * h0, $"移動先に届いた保有 {arrived} が移動者の持ち分 {h0 * moved / s0} でない");
    }

    [Fact]
    public void 国債台帳_越境移民の保有は発行国の対外債へ振り替わる()
    {
        var coef = new Coefficients();
        var sim = new MultiNationSim([OpeningFixture.Named("イギリス"), OpeningFixture.Named("フランス")], 1789, _ => new NullAgent(), coef);   // ★(332) 開幕の国庫・国債を絶対額で持つ盤面（助走なしの器）
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World;
        // 仏の初期国債（米独立戦争債務）は構築時に購入と同じ規則で起票される
        Assert.True(fr.GovDebt.Value > 0 && Math.Abs(HeldOf(fr) - (fr.GovDebt.Value - fr.ForeignDebt.Value)) <= 1e-9 * fr.GovDebt.Value, "仏の初期国債の持ち主が起票されていない");
        new BondLedger().Check(fr); new BondLedger().Check(br);
        var frR = fr.Regions[0];
        Pop cap = frR.Pops[0]; foreach (var p in frR.Pops) if (p.BondHoldings > cap.BondHoldings) cap = p;
        var worker = frR.Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        Assert.True(cap.BondHoldings.Value > 0, "前提が崩れた：首都州に持ち手がいない");
        Military.MoveCohort(cap, worker, cap.Size * 0.5, coef);   // 転落＝保有が1人あたりで労働者へ（越境する階級に持ち手を作る）
        double hw = worker.BondHoldings.Value, sw = worker.Size, fd0 = fr.ForeignDebt.Value, cl0 = br.ForeignBondClaims.Value;
        var brWorker = br.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        double hb0 = brWorker.BondHoldings.Value;
        Assert.True(hw > 0, "前提が崩れた：労働者が持ち手になっていない");
        sim.MoveMigrants(fr, br, frR, br.Regions[0], PopClass.Worker, coef.EmigrationMaxPerQuarter.Value * sw);   // ★回1-2：流量は呼び手が決める（1階級）
        double left = sw - worker.Size;
        Assert.True(left > 0, "前提が崩れた：移民が出ていない");
        double exported = hw * left / sw;
        double tol = 1e-9 * Math.Max(1.0, hw);
        Assert.True(Math.Abs(fr.ForeignDebt.Value - fd0 - exported) <= tol, $"発行国の対外債が移民の保有ぶん増えていない（{fr.ForeignDebt.Value - fd0} / {exported}）");
        Assert.True(Math.Abs(br.ForeignBondClaims.Value - cl0 - exported) <= tol, "受入国の対外債権が移民の保有ぶん増えていない");
        Assert.Equal(hb0, brWorker.BondHoldings.Value);   // 英の台帳には仏の国債を持ち込まない（英の国内元本は不変）
        new BondLedger().Check(fr); new BondLedger().Check(br);
    }

    [Fact]
    public void 国債台帳_建国と割譲は対外債へ振替_再併合は反乱国の債務ごと消える()
    {
        var coef = new Coefficients();
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: coef) { DiplomacyFrozen = true };
        var pw = sim.Nations[0].World;
        pw.Coef.ForeignBondCapacity = new Share(0);   // ⚠国の係数は国ごと別インスタンス（共有層の coef ではない）＝起債は全額国内（反乱国は親の係数を共有する）
        FiscalMonth(pw, Short);   // 起債は r＜1 の月（2026-09-28・エンジンの1行）
        sim.StepQuarter();   // 英が国内で起債＝各州の投資家が持ち手になる（月次の BondLedger 検査はエンジン内で走る）
        double hRebel = 0; foreach (var p in pw.Regions[1].Pops) hRebel += p.BondHoldings.Value;
        Assert.True(hRebel > 0, "前提が崩れた：離脱する州に持ち手がいない");
        // (1) 建国（内戦の反乱国）：出ていく州の持ち手の保有は親の対外債へ＝反乱国の対外債権
        pw.PendingRebelRegions = [pw.Regions[1]]; pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        // 反乱国は生まれたばかり＝その対外債権は建国で振り替わった保有だけ。親の対外債はそれを含む（越境移民の振替・償還は四半期の途中にもある＝下限で見る）
        Assert.True(rw.ForeignBondClaims.Value > 0, "建国で出ていった保有が反乱国の対外債権になっていない");
        Assert.True(pw.ForeignDebt.Value >= rw.ForeignBondClaims.Value * (1 - 1e-9), $"親の対外債 {pw.ForeignDebt} が反乱国の対外債権 {rw.ForeignBondClaims.Value} を含んでいない");
        Assert.Equal(0.0, HeldOf(rw));   // 反乱国の台帳は空（反乱国はまだ起債していない）
        new BondLedger().Check(pw); new BondLedger().Check(rw);
        // (2) 割譲（講和の割譲・建国・再併合が共通に通る州の物理移転 Territory.TransferRegion）：割譲州の保有も親の対外債へ
        var before = new HashSet<Region>(rw.Regions);
        double fd0 = pw.ForeignDebt.Value, cl0 = rw.ForeignBondClaims.Value;
        Territory.TransferRegion(pw, rw, pw.Regions.Find(r => r.Id != 0)!, Territory.Rehoming.Ceded);
        var ceded = rw.Regions.FindAll(r => !before.Contains(r));
        Assert.True(ceded.Count > 0, "前提が崩れた：州が割譲されていない");
        foreach (var r in ceded) foreach (var p in r.Pops) Assert.Equal(0.0, p.BondHoldings.Value);
        double exported = pw.ForeignDebt.Value - fd0;
        Assert.True(exported > 0, "前提が崩れた：割譲州に持ち手がいない");
        Assert.True(Math.Abs(rw.ForeignBondClaims.Value - cl0 - exported) <= (1e-9 * pw.ForeignDebt).Value, "割譲州の保有が勝者の対外債権になっていない");
        new BondLedger().Check(pw); new BondLedger().Check(rw);
        // (3) 再併合：反乱国の国債は誰にも継承されない（既存どおり反乱国の GovDebt ごと消える）＝持ち手の保有も消える
        FiscalMonth(rw, Short);   // 起債は r＜1 の月（2026-09-28・エンジンの1行）
        sim.StepQuarter();   // 反乱国が自国の投資家から借りる
        Assert.True(HeldOf(rw) > 0 && rw.GovDebt.Value > 0, "前提が崩れた：反乱国の国債の持ち手がいない");
        var returning = new List<Region>(rw.Regions);
        // 親の月の支払いを満たす（国庫へ POP の貯蓄から移す＝保存）＝利子の未払いの元本への繰り入れ（r＜1 の月）で親の国内元本が動かない＝継承だけを測る。
        //   2026-09-30 (135)：連合維持費を撤去した軌道では、この四半期の親は歳入が利払いを下回り r＜1 だった＝繰り入れ（正しい機構）が継承に見えた。
        {
            var payers = new List<Pop>(pw.AllPops());
            double sav = 0; foreach (var p in payers) sav += Math.Max(0, p.Savings.Value);
            double x = Math.Min(0.5 * sav, Calendar.PerMonthToPerYear(pw.LastInterestPerMonth + pw.LastRevenuePerMonth + pw.LastAdminClaimPerMonth + pw.LastMilitaryClaimPerMonth + pw.LastSoldierPayPerMonth).Value);
            World.Transfer((pw, payers), (pw, null, null), x, TreasuryCat.OtherIn);   // 貯蓄の比で国庫へ（移し方の1本・2026-09-30 段4g）
        }
        double pwDom0 = pw.GovDebt.Value - pw.ForeignDebt.Value;
        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
        sim.StepQuarter();
        Assert.DoesNotContain(sim.Nations, n => ReferenceEquals(n.World, rw));
        foreach (var r in returning) foreach (var p in r.Pops) Assert.Equal(0.0, p.BondHoldings.Value);   // 反乱国の国債は消えた＝持ち手の損
        Assert.True(pw.GovDebt.Value - pw.ForeignDebt.Value <= pwDom0 + 1e-9 * Math.Max(1.0, pwDom0), "親が反乱国の国債を継承している");
        new BondLedger().Check(pw);
        sim.StepQuarter();   // 返った州を含めた1四半期（エンジンの月次検査）
        new BondLedger().Check(pw);
    }

    [Fact]
    public void 国債台帳_台帳導入前のセーブは読込時に起票される()
    {
        var sim1 = new MultiNationSim([OpeningFixture.Named("イギリス"), OpeningFixture.Named("フランス")], 1789, _ => new NullAgent());   // ★(332) 開幕の国債を絶対額で持つ盤面
        var fr1 = sim1.Nations[1].World;
        Assert.True(fr1.GovDebt.Value > 0, "前提が崩れた：仏に初期国債がない");
        foreach (var r in fr1.Regions) foreach (var p in r.Pops) p.BondHoldings = new MoneyStock(0);   // 台帳導入前のセーブ（保有は既定値0で読まれる）を模す
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
        var coef = new Coefficients { CapitalFlightRatePerQuarter = new RatePerQuarter(0), CapitalDividendRatePerQuarter = new RatePerQuarter(0), CapitalDemandScale = 0, CapitalDomesticFlightRatePerQuarter = new RatePerQuarter(0) };
        var sim = new MultiNationSim([OpeningFixture.Named("イギリス"), OpeningFixture.Named("フランス"), OpeningFixture.Named("プロイセン")], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };   // ★(332) 国庫・国債は絶対額（助走なしの器）
        CapitalMarket.Step(sim.Nations, coef);
        return (sim, coef);
    }

    private static double MintedAndFiat(World[] ws) { double s = 0; foreach (var w in ws) s += w.CumulativeGoldMinted.Value + w.CumulativeFiatIssued.Value; return s; }
    private static double WorldBase(World[] ws) { double s = 0; foreach (var w in ws) s += w.BaseMoney.Value; return s; }

    [Fact]
    public void 外債の元本_償還は減らした債権の比で債権国へ届き世界の貨幣は変わらない()
    {
        var (sim, coef) = QuietBondWorld();
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        var all = new[] { br, fr, pr };
        Assert.True(fr.GovDebt.Value > 0, "前提が崩れた：仏に初期国債がない");
        foreach (var w in all) Assert.Equal(0.0, w.ForeignBondClaims.Value);   // 開始盤面に対外債権は無い＝下で振り替えた分だけが債権
        // 仏の最大の持ち手の保有を英へ3・普へ1の比で国外へ振り替える（越境移民・割譲と同じ経路）＝英と普が仏の外債の債権国になる
        Pop cap = fr.Regions[0].Pops[0];
        foreach (var r in fr.Regions) foreach (var p in r.Pops) if (p.BondHoldings > cap.BondHoldings) cap = p;
        double h = cap.BondHoldings.Value;
        Assert.True(h > 0, "前提が崩れた：仏の国債に持ち手がいない");
        Fiscal.BookForeignDebt(cap, new MoneyStock(0.6 * h), fr, br);
        Fiscal.BookForeignDebt(cap, new MoneyStock(0.2 * h), fr, pr);
        new BondLedger().Check(fr);
        // 仏の財政を静める（歳入・福祉が0＝国庫の出入りは減債だけ）＋利子0（元本だけを見る）＋余剰の全額で減債
        fr.TaxRate = new Share(0); fr.PollTax = 0; fr.LandTax = 0; fr.ProgressiveTax = 0;
        fr.SocialSecurity = 0; fr.Pension = ReformLevel.None;
        fr.GovBondRatePerYear = new RatePerYear(0); fr.GovAvgCouponPerYear = new RatePerYear(0); fr.Coef.BondRepayFracPerMonth = new RatePerMonth(1.0);
        var guards = Array.ConvertAll(all, w => new MoneyConservation(w));
        var base0 = Array.ConvertAll(all, w => w.BaseMoney);
        double world0 = WorldBase(all), mint0 = MintedAndFiat(all);
        double cBr0 = br.ForeignBondClaims.Value, cPr0 = pr.ForeignBondClaims.Value, fd0 = fr.ForeignDebt.Value;

        FiscalMonth(fr, 1, fr.Treasury.Value);           // 仏が減債する（r＝1・余剰＝国庫＝利子も0の盤面・国内分は保有比で国内の持ち手へ・対外分は国外へ）
        double principal = fd0 - fr.ForeignDebt.Value;
        Assert.True(principal > 0, $"前提が崩れた：対外の元本が償還されていない（国庫 {fr.Treasury}）");
        CapitalMarket.Step(sim.Nations, coef);     // 四半期末の国際金融＝債権の繰上げと元利の配当

        double tol = 1e-9 * Math.Max(1.0, principal);
        double gotBr = (br.BaseMoney - base0[0]).Value, gotPr = (pr.BaseMoney - base0[2]).Value;
        // (a) 償還した対外の元本は債権国に同額届く＝仏の減りと債権国の増えが相殺し、世界の基礎貨幣は鋳造・不換の台帳ぶんしか動かない
        Assert.True(Math.Abs(gotBr + gotPr - principal) <= tol, $"債権国に届いた額 {gotBr + gotPr} ≠ 償還した対外の元本 {principal}（届かない分が世界から消えている）");
        Assert.True(Math.Abs(fr.BaseMoney.Value - base0[1].Value + principal) <= tol, $"仏の基礎貨幣の減り {base0[1].Value - fr.BaseMoney.Value} ≠ 対外の元本 {principal}");
        double worldDrift = (WorldBase(all) - world0) - (MintedAndFiat(all) - mint0);
        Assert.True(Math.Abs(worldDrift) <= 1e-9 * Math.Max(1.0, world0), $"世界の基礎貨幣が台帳の外で {worldDrift} 動いた");
        // (b) 債権国が複数なら、減らした債権の比で配られる＝各債権国の受け取り＝その国の債権の減り（3:1）
        Assert.True(Math.Abs(gotBr - (cBr0 - br.ForeignBondClaims.Value)) <= tol, $"英の受け取り {gotBr} ≠ 英の債権の減り {cBr0 - br.ForeignBondClaims.Value}");
        Assert.True(Math.Abs(gotPr - (cPr0 - pr.ForeignBondClaims.Value)) <= tol, $"普の受け取り {gotPr} ≠ 普の債権の減り {cPr0 - pr.ForeignBondClaims.Value}");
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
        var coef = new Coefficients { CapitalFlightRatePerQuarter = new RatePerQuarter(0), CapitalDividendRatePerQuarter = new RatePerQuarter(0), CapitalDemandScale = 0 };   // 外債の元利は構築直後で空（新規の起債も利払いも無い）
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        var all = new[] { br, fr, pr };
        foreach (var w in all) Assert.True(w.NominalGdpPerMonth.Value > 0, $"前提が崩れた：{w.Region.Name} の名目GDPが立っていない");
        fr.CumulativeDevaluation = (fr.NominalGdpPerMonth * (coef.CapitalFlightDistrustThreshold + 0.5)).Value;
        var guards = Array.ConvertAll(all, w => new MoneyConservation(w));
        var base0 = Array.ConvertAll(all, w => w.BaseMoney);
        var fci0 = Array.ConvertAll(all, w => w.ForeignCapitalIn.Value);
        var ca0 = Array.ConvertAll(all, w => w.CapitalAbroad.Value);
        double world0 = WorldBase(all), mint0 = MintedAndFiat(all), abroad0 = fr.CapitalAbroad.Value;

        CapitalMarket.Step(sim.Nations, coef);

        double fled = fr.CapitalAbroad.Value - abroad0;
        Assert.True(fled > 0, "前提が崩れた：仏の資本が逃げていない");
        double tol = 1e-9 * Math.Max(1.0, fled);
        double gotBr = (br.BaseMoney - base0[0]).Value, gotPr = (pr.BaseMoney - base0[2]).Value;
        Assert.True(Math.Abs(fr.BaseMoney.Value - base0[1].Value + fled) <= tol, $"仏の基礎貨幣の減り {base0[1].Value - fr.BaseMoney.Value} ≠ 逃げた額 {fled}");
        Assert.True(Math.Abs(gotBr + gotPr - fled) <= tol, $"受け皿の国に届いた額 {gotBr + gotPr} ≠ 逃げた額 {fled}（届かない分が世界から消えている）");
        double worldDrift = (WorldBase(all) - world0) - (MintedAndFiat(all) - mint0);
        Assert.True(Math.Abs(worldDrift) <= 1e-9 * Math.Max(1.0, world0), $"世界の基礎貨幣が台帳の外で {worldDrift} 動いた");
        // 受け皿の按分＝自国以外の信用×名目GDP（CreditRating は同じ Step の信用の写し）
        double wBr = (br.CreditRating * br.NominalGdpPerMonth).Value, wPr = (pr.CreditRating * pr.NominalGdpPerMonth).Value;
        Assert.True(Math.Abs(gotBr - fled * wBr / (wBr + wPr)) <= tol, $"英の受け取り {gotBr} が信用×名目GDPの比 {fled * wBr / (wBr + wPr)} でない");
        // 請求権の両建て：受け皿の外資負債の増え＝届いた金（逃げた国の負債は動かない）
        Assert.Equal(fci0[1], fr.ForeignCapitalIn.Value);
        Assert.True(Math.Abs(br.ForeignCapitalIn.Value - fci0[0] - gotBr) <= tol, $"英の外資負債の増え {br.ForeignCapitalIn.Value - fci0[0]} ≠ 受け取り {gotBr}（相手のいない請求権が残る）");
        Assert.True(Math.Abs(pr.ForeignCapitalIn.Value - fci0[2] - gotPr) <= tol, $"普の外資負債の増え {pr.ForeignCapitalIn.Value - fci0[2]} ≠ 受け取り {gotPr}（相手のいない請求権が残る）");
        // 世界の請求権の和が釣り合う＝Σ対外資産の増え＝Σ外資負債の増え（逃げた側の資産に、受け皿の負債が同額立つ）
        double dAbroad = 0, dFci = 0;
        for (int i = 0; i < all.Length; i++) { dAbroad += all[i].CapitalAbroad.Value - ca0[i]; dFci += all[i].ForeignCapitalIn.Value - fci0[i]; }
        Assert.True(Math.Abs(dAbroad - dFci) <= tol, $"Σ対外資産の増え {dAbroad} ≠ Σ外資負債の増え {dFci}");
        for (int i = 0; i < all.Length; i++) guards[i].Check(all[i]);
    }

    // =====================================================================================================
    // ★金利生活者のポンプ（2026-09-27）の番人。世界規模の補給崩壊の根は貨幣の崩壊だった：
    //   ①切下げの記憶が壊れたまま兌換へ戻れた（改革は兌換停止中にしかできない＝記憶が1世紀凍結し国内資本の逃避が止まらない）
    //   ②配当と逃避の払いが投資家で足りないと全POPから取った（流入は投資家へだけ入る＝分子と分母の非対称）＝受け皿の大衆の貨幣が国外へ抜けた。
    // =====================================================================================================

    [Theory]
    [InlineData(0.0)]   // 旗 CapitalCouponUnpaidWriteDown 0＝2026-09-27 の規則 C のまま（払えない配当は積まない＝負債の減りは逃避の額ちょうど）
    [InlineData(1.0)]   // ★(819) c65fl：規則 C の拡張（払えない利札も不履行＝払えなかった利札の分だけ請求権を落とす）＝負債の減り＝逃避＋min(未払いの利札, 残りの外資の負債)
    public void 外資の配当と逃避は投資家までしか払わない_払えない逃避は債権ごと消える(double writeDown)
    {
        // 外資の流入・逃避の受け皿・新規の資本移動は、どれも投資家の貯蓄へだけ入る（GiveGold の toInvestors）。流出も投資家まで＝
        // 投資家が尽きても農民・労働者の貯蓄は国外へ出ない。
        // ★2026-09-27（C）：払えない逃避は不履行＝債権ごと消える（受入国の負債と持ち主の対外資産が同じ額だけ減る・貨幣は払えた額しか動かない）。
        //   旧＝負債は払えた額しか減らず、払えなかった元本が毎四半期もう一度の逃避と配当を請求され続けた。払えない配当は積まない（同じ規則）。
        // 国際金融の外資以外の流れ（新規の資本移動・国内資本の逃避・外債の元利）を止め、仏の外資の負債を投資家の貯蓄の千倍にする（持ち主は英）。
        var coef = new Coefficients { CapitalDividendFromHostRent = 0, CapitalDemandScale = 0, CapitalDomesticFlightRatePerQuarter = new RatePerQuarter(0), CapitalCouponUnpaidWriteDown = writeDown };   // ★(801) 旗 0 を明示（この試験は旧の形の払い＝率×外資の負債を確かめる）。外債の元利は構築直後で空（新規の起債も利払いも無い）
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        var all = new[] { br, fr, pr };
        var fpops = new List<Pop>(); foreach (var r in fr.Regions) fpops.AddRange(r.Pops);
        double inv0 = 0; int nOther = 0;
        foreach (var p in fpops) { if (CapitalMarket.IsInvestor(p)) inv0 += Math.Max(0, p.Savings.Value); else if (p.Savings.Value > 0) nOther++; }
        Assert.True(inv0 > 0, "前提が崩れた：仏に投資家の貯蓄が無い");
        Assert.True(nOther > 0, "前提が崩れた：仏に貯蓄を持つ投資家でない人がいない");
        double claim = 1000 * inv0;
        fr.ForeignCapitalIn += new MoneyStock(claim); br.CapitalAbroad += new MoneyStock(claim);
        Assert.True(fr.ForeignCapitalIn.Value * coef.CapitalDividendRatePerQuarter.Value > inv0, "前提が崩れた：配当が投資家の貯蓄に収まる（不足が起きない）");
        var other0 = fpops.Select(p => CapitalMarket.IsInvestor(p) ? double.NaN : p.Savings.Value).ToArray();
        var guards = Array.ConvertAll(all, w => new MoneyConservation(w));
        double world0 = WorldBase(all), mint0 = MintedAndFiat(all), base0 = fr.BaseMoney.Value, fci0 = fr.ForeignCapitalIn.Value;
        var fcis0 = Array.ConvertAll(all, w => w.ForeignCapitalIn.Value);
        var cas0 = Array.ConvertAll(all, w => w.CapitalAbroad.Value);

        CapitalMarket.Step(sim.Nations, coef);

        // (a) 投資家でない人の貯蓄は1ビットも動かない
        for (int i = 0; i < fpops.Count; i++)
            if (!double.IsNaN(other0[i]))
                Assert.True(fpops[i].Savings.Value == other0[i], $"投資家でない {fpops[i].ClassName} の貯蓄が {other0[i]}→{fpops[i].Savings} に動いた（配当・逃避の不足を大衆から取っている）");
        // (b) 仏が払った額は投資家の貯蓄まで（払い切って尽きる）
        double paid = base0 - fr.BaseMoney.Value, tol = 1e-9 * Math.Max(1.0, inv0);
        Assert.True(paid <= inv0 + tol, $"仏が払った額 {paid} が投資家の貯蓄 {inv0} を超えた");
        Assert.True(paid >= inv0 - tol, $"前提が崩れた：投資家が払い切っていない（{paid} / {inv0}）");
        // (c) 払えない逃避は債権ごと消える＝仏の負債は払えた額でなく逃避の額だけ減る（CreditRating＝同じ Step の信用の写し）。
        //     払えない配当は積まない＝負債の減りは逃避の額ちょうど。持ち主の対外資産も同じ額だけ減る（請求権の両側を同時に）。
        double flight = Math.Min(fci0, fci0 * coef.CapitalFlightRatePerQuarter.Value * (1.0 - fr.CreditRating)), tolC = 1e-9 * Math.Max(1.0, fci0);
        Assert.True(flight > 2 * paid, $"前提が崩れた：逃避 {flight} の大半が払える（払えた額 {paid}）");
        // ★(819) c65fl 旗 1（規則 C の拡張）：逃避だけで投資家が払い切る（上の前提）＝利札は 1 も払えない＝未払いの利札＝率×逃避の後の外資の負債（試験が係数から書く・エンジンの局所の値を読まない）。
        //   その分だけ請求権も落ちる（外資の負債で頭打ち）。旗 0 は落とさない＝期待は逃避の額ちょうど（旧の等式のまま）。
        double unpaidCoupon = writeDown > 0 ? Math.Min((fci0 - flight) * coef.CapitalDividendRatePerQuarter.Value, fci0 - flight) : 0.0;
        if (writeDown > 0) Assert.True(unpaidCoupon > 1e6 * tolC, $"前提が崩れた：未払いの利札 {unpaidCoupon} が許しの幅 {tolC} に埋もれる（落とさない実装が通る）");
        double expectDrop = flight + unpaidCoupon;
        Assert.True(Math.Abs(fci0 - fr.ForeignCapitalIn.Value - expectDrop) <= tolC, $"仏の負債の減り {fci0 - fr.ForeignCapitalIn.Value} ≠ 逃避の額 {flight}＋未払いの利札の落とし {unpaidCoupon}（払えなかった元本が債権のまま残る／未払いの配当が積まれた／落としの額の誤り）");
        double dFci = 0, dCa = 0;
        for (int i = 0; i < all.Length; i++) { dFci += fcis0[i] - all[i].ForeignCapitalIn.Value; dCa += cas0[i] - all[i].CapitalAbroad.Value; }
        Assert.True(Math.Abs(dCa - dFci) <= tolC, $"持ち主の対外資産の減り {dCa} ≠ 受入国の負債の減り {dFci}（請求権の片側だけが消えた）");
        Assert.True(Math.Abs(cas0[0] - br.CapitalAbroad.Value - expectDrop) <= tolC, $"英の対外資産の減り {cas0[0] - br.CapitalAbroad.Value} ≠ 仏の逃避の額 {flight}＋未払いの利札の落とし {unpaidCoupon}");
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
            w.Coef.GoldStandardMinFunding = new Share(0); w.Coef.GoldRejoinCoverMargin = 0;
            w.Coef.CurrencyReformCalmMonths = 0;
            w.MonetaryLaw = MonetaryStandardLaw.Silver;   // 金属本位の法は立てたまま（露の銀本位）＝兌換停止中
            w.OnGoldStandard = false;
            if (broken) w.CumulativeDevaluation = (w.NominalGdpPerMonth * (w.Coef.CapitalFlightDistrustThreshold + 0.5)).Value;
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
            w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(2.0 * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
            return w;
        }
        double Coverage(World w) => w.BaseMoney / w.BroadMoney;
        var keep = Board(true);
        Assert.True(keep.MonetaryEnabled && keep.NominalGdpPerMonth.Value > 0, "前提が崩れた：英に貨幣経済が無い");
        Assert.True(Coverage(keep) >= keep.Coef.GoldCoverMin.Value * keep.Coef.GoldRejoinCoverMargin, $"前提が崩れた：準備が薄い（カバー率 {Coverage(keep)}）");
        Fiscal.Step(keep, keep.Coef);
        Assert.Equal(1.0, keep.FiscalFundingRatio.Value);
        Assert.True(keep.GovDebt.Value > Calendar.PerMonthToPerYear(1.5 * keep.NominalGdpPerMonth).Value, "前提が崩れた：債務が年GDP の2倍に届いていない");
        Assert.True(keep.OnGoldStandard, "債務が年GDP の2倍の英が、取り付けも財政不足も無いのに兌換を止めた");

        var back = Board(false);
        Fiscal.Step(back, back.Coef);
        Assert.True(back.OnGoldStandard, "債務が年GDP の2倍の英が、準備と財政が足りているのに兌換へ戻れない（英1821年の再開）");

        var run = Board(true);
        run.Bank!.Loans = new MoneyStock(run.TotalMoney.Value + run.Treasury.Value);   // 準備（基礎貨幣）が与信に食われて0＝取り付けで金が流出し尽くした盤面
        Fiscal.Step(run, run.Coef);
        Assert.False(run.OnGoldStandard, "準備が割れても兌換を止めない（取り付けの門が死んでいる＝この番人の対照が崩れた）");
    }

    // ★国債の金利を史実から出し直した（2026-09-29・作者裁定＝(94) の申し送り1 の (i)）の番人の盤面：静かな英に、金利の式が読む量だけを置く
    //   （兌換・議会の予算の掌握・期待インフレ0・政策金利0・国債＝年名目GDP の debtYears 倍）。r＝1・余剰0＝借りも返しもしない月＝国債は動かない。
    private static World BondBoard(double conv, double parliament, double debtYears)
    {
        var w = QuietBritain();
        w.Convertibility = conv; w.ParliamentControl = parliament;
        w.ExpectedInflationPerYear = new RatePerYear(0); w.InterestRate = 0;
        w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(debtYears * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
        w.FiscalFundingRatio = new Share(1.0);
        return w;
    }

    /// <summary>★債務比の上乗せは史実から（BondRateDebtScale＝英のコンソル）：国債÷年名目GDP が2.5 の信認のある国（完全兌換・議会が予算を握る・切り下げなし）の金利は
    /// 約4〜5%（英1815〜20 のコンソル約4.5%）。不換・議会の弱い周縁国は国債÷年GDP 1.5 で6〜10%（西・葡・希・オスマン・ラテンアメリカ）。
    /// 突然変異：旧の係数 1.44 へ戻す→信認のある国が上限 0.30 に張り付いて赤／分母を月次の名目GDP へ戻す（旧の DebtGdpAnnualized=0 の枝）→赤。</summary>
    [Fact]
    public void 国債の金利_債務が年GDPの2倍半でも信認のある国は4から5パーセントで不換の周縁国は6から10パーセント()
    {
        var credible = BondBoard(conv: 1.0, parliament: 1.0, debtYears: 2.5);
        Assert.True(credible.NominalGdpPerMonth.Value > 0, "前提が崩れた：英に名目GDPが無い");
        Fiscal.Step(credible, credible.Coef);
        Assert.True(credible.GovDebt.Value >= (Calendar.PerMonthToPerYear(2.5 * credible.NominalGdpPerMonth) * (1 - 1e-12)).Value, "前提が崩れた：国債が動いた");
        Assert.InRange(credible.GovBondRatePerYear.Value, 0.04, 0.05);

        var periphery = BondBoard(conv: 0.0, parliament: 0.0, debtYears: 1.5);
        Fiscal.Step(periphery, periphery.Coef);
        Assert.InRange(periphery.GovBondRatePerYear.Value, 0.06, 0.10);
        Assert.True(periphery.GovBondRatePerYear > credible.GovBondRatePerYear, "債務の軽い不換の国が債務の重い信認のある国より安く借りている");
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
        Assert.Equal(ctl.GovBondRatePerYear, dev.GovBondRatePerYear);
        dev.GovAvgCouponPerYear = dev.GovBondRatePerYear; ctl.GovAvgCouponPerYear = ctl.GovBondRatePerYear;
        // ① 切り下げの月＝年GDP の5%の速さ（この月の額＝月次GDP の5%＝×12÷年GDP＝0.05）
        double devalue = (0.05 * dev.NominalGdpPerMonth).Value;
        dev.CumulativeDevaluation += devalue;
        Fiscal.Step(dev, dev.Coef, 0, devalue); Fiscal.Step(ctl, ctl.Coef);
        double premium = (dev.GovBondRatePerYear - ctl.GovBondRatePerYear).Value;
        Assert.True(Math.Abs(premium - dev.Coef.BondRateDistrustScale * 0.05) <= 1e-12, $"切り下げの月の上乗せ {premium} が 0.5×年GDP の5%＝{dev.Coef.BondRateDistrustScale * 0.05} でない（速さか年の単位が崩れた）");
        Assert.True(premium > 0.02 && premium < 0.03, $"上乗せ {premium}（GDP 比1%につき約0.5%）");
        // ② 止めた翌月から市場の金利は双子と同じ ③ 払う利子の差は毎月薄まる
        double prevGap = double.PositiveInfinity;
        for (int m = 0; m < 24; m++)
        {
            Fiscal.Step(dev, dev.Coef); Fiscal.Step(ctl, ctl.Coef);
            Assert.Equal(ctl.GovBondRatePerYear, dev.GovBondRatePerYear);
            double gap = (dev.GovAvgCouponPerYear - ctl.GovAvgCouponPerYear).Value;
            Assert.True(gap > 0, $"{m + 1} か月目：切り下げた月の金利で乗り換えた分が平均クーポンに残っていない（差 {gap}）");
            Assert.True(gap < prevGap, $"{m + 1} か月目：平均クーポンの差 {gap} が薄まらない（前の月 {prevGap}）");
            prevGap = gap;
        }
        Assert.True(prevGap < dev.Coef.BondRolloverFracPerMonth.Value * premium, "2年たっても払う利子の上乗せが乗り換え1か月分より大きい");
    }

    /// <summary>★単位の対称：同じ実体で物価水準が2倍の双子（名目の量＝国債・切り下げの額・名目GDP が全て2倍）は同じ金利（債務÷年名目GDP・切り下げの速さ÷年名目GDP＝名目÷名目）。
    /// 突然変異：信認の喪失か債務比の分母を実質GDP にする→赤。</summary>
    [Fact]
    public void 国債の金利_物価水準が2倍の双子は同じ金利()
    {
        var a = BondBoard(conv: 0.3, parliament: 0.5, debtYears: 1.0);
        var b = BondBoard(conv: 0.3, parliament: 0.5, debtYears: 1.0);
        double n0 = b.NominalGdpPerMonth.Value;
        b.PriceLevel *= 2; b.GovDebt *= 2; b.LastTariffRevenuePerClearing *= 2;
        Assert.True(Math.Abs(b.NominalGdpPerMonth.Value - 2 * n0) <= 1e-12 * n0, $"前提が崩れた：名目GDPが2倍にならない（{n0}→{b.NominalGdpPerMonth}）");
        double devalue = 0.03 * n0;   // 年GDP の3%の速さ（月次GDP の3%×12÷年GDP）＝信認の喪失 0.5×0.03（上限 0.30 の手前で比べる）
        Fiscal.Step(a, a.Coef, 0, devalue);
        Fiscal.Step(b, b.Coef, 0, 2 * devalue);
        Assert.True(a.GovBondRatePerYear < a.Coef.BondRateMaxPerYear, $"前提が崩れた：上限に張り付いた（{a.GovBondRatePerYear}）");
        Assert.True(Math.Abs((a.GovBondRatePerYear - b.GovBondRatePerYear).Value) <= 1e-12, $"物価水準2倍の双子の金利 {b.GovBondRatePerYear} が {a.GovBondRatePerYear} と違う（実質の物差しが混じった）");
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
            double n0 = br.NominalGdpPerMonth.Value;
            Assert.True(n0 > 0, "前提が崩れた：英の名目GDPが立っていない");
            if (doublePrice)
            {
                br.PriceLevel *= 2; br.LastTariffRevenuePerClearing *= 2;
                Assert.True(Math.Abs(br.NominalGdpPerMonth.Value - 2 * n0) <= 1e-12 * n0, $"前提が崩れた：名目GDPが2倍にならない（{n0}→{br.NominalGdpPerMonth}）");
            }
            br.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(years * br.NominalGdpPerMonth).Value); br.ForeignDebt = new MoneyStock(0);
            CapitalMarket.Step(sim.Nations, new Coefficients());
            return br.CreditDebtTerm;
        }
        double c = new Coefficients().DebtDistressRatio.Value;
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
            double n0 = w.NominalGdpPerMonth.Value;
            Assert.True(n0 > 0 && w.MonetaryEnabled && w.Bank is not null, "前提が崩れた：仏の名目GDP・貨幣・銀行が立っていない");
            if (doublePrice)
            {
                w.PriceLevel *= 2;
                Assert.True(Math.Abs(w.NominalGdpPerMonth.Value - 2 * n0) <= 1e-12 * n0, $"前提が崩れた：名目GDPが2倍にならない（{n0}→{w.NominalGdpPerMonth}）");
            }
            w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(years * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
            w.FiscalFundingRatio = new Share(1);
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
            w.GovBondRatePerYear = 2 * w.Coef.BondBaseRatePerYear;   // 国債の価格＝基礎金利÷国債の金利＝0.5
            Credit.Step(w, w.Coef, (w.NominalGdpPerMonth.Value, 1.0));
            return w.Bank!.Confidence;
        }
        var c = new Coefficients();
        // ① 緊縮の強度
        Assert.True(Austerity(0.3) <= 1e-9, $"年GDP の0.3倍の国債で緊縮が立った（{Austerity(0.3)}＝物差しが年の名目GDP でない）");
        double fr = Austerity(0.7);
        Assert.True(Math.Abs(fr - (0.7 - c.AusterityStartDebt.Value) / c.AusterityRange.Value) <= 1e-9, $"仏1788（0.7）の緊縮の強度 {fr} が (0.7−開始)÷幅 でない");
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
    /// 突然変異：BondRepayFracPerMonth を旧の 0.05 へ戻す→赤。</summary>
    [Fact]
    public void 減債は年に残高の約1パーセントで余剰のある英も10年で国債の1割しか返さない()
    {
        var w = QuietBritain();
        double d0 = Calendar.PerMonthToPerYear(1.1 * w.NominalGdpPerMonth).Value;
        w.GovDebt = new MoneyStock(d0); w.ForeignDebt = new MoneyStock(0);
        Fiscal.SeedBondHoldings(w);
        for (int m = 0; m < 120; m++) FiscalMonth(w, 1, Ample);
        double left = w.GovDebt.Value / d0;
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
            w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(1.0 * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
            Fiscal.SeedBondHoldings(w);
            w.LastInterestPerMonth = new MoneyPerMonth(0); w.LastRevenuePerMonth = w.NominalGdpPerMonth;   // 借入の上限の下
            w.WarmupFrozen = warm;
            return w;
        }
        // ① 借入
        var warm1 = Board(true);
        Assert.Equal(0.0, Fiscal.Borrow(warm1, warm1.Coef, -Ample));
        var ctl = Board(false);
        Assert.True(Fiscal.Borrow(ctl, ctl.Coef, -Ample) > 0, "対照が崩れた：助走の外で払えない額があっても借りない");
        // ② 減債（r＝1・余剰は十分）
        var warm = Board(true); double d0 = warm.GovDebt.Value;
        warm.FiscalFundingRatio = new Share(1); Fiscal.Step(warm, warm.Coef, Ample);
        Assert.Equal(d0, warm.GovDebt.Value);
        var ctl2 = Board(false);
        ctl2.FiscalFundingRatio = new Share(1); Fiscal.Step(ctl2, ctl2.Coef, Ample);
        Assert.True(ctl2.GovDebt.Value < d0, "対照が崩れた：助走の外で余剰があっても返さない");
        // ③ 不履行の計数（借入の上限の上＝限界0・払えない月）
        var warm3 = Board(true); warm3.Coef.MaxInterestShareOfRevenue = new Share(0);
        warm3.FiscalFundingRatio = new Share(Short); Fiscal.Step(warm3, warm3.Coef, -Ample);
        Assert.Equal(0.0, warm3.InsolvencyMonths);
        var ctl3 = Board(false); ctl3.Coef.MaxInterestShareOfRevenue = new Share(0);
        ctl3.FiscalFundingRatio = new Share(Short); Fiscal.Step(ctl3, ctl3.Coef, -Ample);
        Assert.Equal(1.0 - Short, ctl3.InsolvencyMonths);   // ★(205) 計数は払えなかった大きさ（r＝0.5 の月＝0.5）
    }

    /// <summary>★(205) 不履行は**払えなかった大きさの累計**で決まる（2026-10-04・README §1-8・推奨で進めた＝作者の承認は未確認）。
    /// 旧＝借入の門が閉じたまま r＜1 の月を大きさに関係なく1つと数え8か月で国債を全て否認＝兌換停止の後に0 へ漸近する兌換の残り・銀の改鋳の窓の残り・
    /// 浮動小数の端（r＝1−1e-10）が毎月1か月に数えられ、ほぼ全てを払えていた国が前触れなく不履行になった（(203) の10種で148件中67件）。
    /// 盤面＝静かな英に国債（年名目GDP の1倍・国内の持ち手）を置き、借入の門を閉じて（限界0）同じ r の月を Fiscal.Step で踏む（不履行そのもの＝国債0・回数を読む）。
    /// ①r＝0.5 の月は2つで不履行（累計 0.5＋0.5＝月の請求1か月分）・1つ目では不履行にならない　②r＝0.99 の月を60続けても不履行にならない（累計 0.6）
    /// ③r＝1−1e-10 の月を120続けても不履行にならない　④払えた月（r＝1）で累計は0 に戻る＝0.5 の月・1 の月・0.5 の月では不履行にならない
    /// ⑤門が開いた月（利払い÷歳入が限界の下）も0 に戻る。
    /// 突然変異（scratchpad/m204/mut204.py）：重みを1 へ（大きさを見ない＝旧の ++）→①が赤／線を旧の8 へ→①が赤／払えた月に0 へ戻さない→④が赤／門が開いた月に0 へ戻さない→⑤が赤・M0 緑。</summary>
    [Fact]
    public void 不履行は払えなかった大きさの累計が月の請求1か月分に届くと起きる()
    {
        static World Board()
        {
            var w = QuietBritain();
            w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(1.0 * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
            Fiscal.SeedBondHoldings(w);
            w.WarmupFrozen = false;
            w.Coef.MaxInterestShareOfRevenue = new Share(0);   // 借入の門が閉じている（利払い÷歳入≧限界）
            w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
            // ★(1234) 盤面は金本位のまま保つ（強制離脱の 3 条件〔Fiscal.Step の金本位の門＝funding 比の下限・正貨準備カバー率の下限・本位の法〕をこの盤面の係数だけで塞ぐ）。
            //   金本位でない月の累計の規則（兌換の遅れで開いていない切り下げの余地の分）は ArrearsOpenedRoomTests が持つ＝この番人は金本位の月の 1−r の累計だけを見る。
            w.OnGoldStandard = true; w.Coef.GoldSuspendMinFundingRatio = 0; w.Coef.GoldCoverMin = new Share(0);
            return w;
        }
        static void Month(World w, double r) { w.FiscalFundingRatio = new Share(r); Fiscal.Step(w, w.Coef, r < 1 ? -Ample : 0); }
        Assert.Equal(1.0, new Coefficients().DefaultArrearsMonths);
        // ① r＝0.5 の月が2つ
        var a = Board(); double d0 = a.GovDebt.Value;
        Assert.True(d0 > 0 && HeldOf(a) > 0, "前提が崩れた：国債と持ち手がいない");
        Assert.True(a.OnGoldStandard, "前提が崩れた：盤面が金本位でない");
        Month(a, 0.5);
        Assert.True(a.OnGoldStandard, "前提が崩れた：1 つ目の月で金本位から離れた＝金本位でない月の規則が累計を変える");
        Assert.False(a.HadSovereignDefault, "r＝0.5 の月1つ（累計 0.5）で不履行になった");
        Assert.Equal(0.5, a.InsolvencyMonths);
        Month(a, 0.5);
        Assert.True(a.HadSovereignDefault, "r＝0.5 の月2つ（累計 1.0＝月の請求1か月分）で不履行にならない");
        Assert.Equal(1, a.SovereignDefaultCount);
        Assert.Equal(0.0, a.GovDebt.Value);
        // ② r＝0.99 の月を60
        var b = Board();
        for (int m = 0; m < 60; m++) Month(b, 0.99);
        Assert.False(b.HadSovereignDefault, "ほぼ払えている月（r＝0.99）の60か月で不履行になった＝大きさを見ていない");
        Assert.InRange(b.InsolvencyMonths, 0.59, 0.61);
        Assert.True(b.GovDebt.Value >= d0 * 0.99, "前提が崩れた：国債が消えた");
        // ③ 浮動小数の端
        var c = Board();
        for (int m = 0; m < 120; m++) Month(c, 1.0 - 1e-10);
        Assert.False(c.HadSovereignDefault, "r＝1−1e-10 の月の120か月で不履行になった");
        // ④ 払えた月で0
        var e = Board();
        Month(e, 0.5); Month(e, 1.0);
        Assert.Equal(0.0, e.InsolvencyMonths);
        Month(e, 0.5);
        Assert.False(e.HadSovereignDefault, "払えた月を挟んだのに累計が残った（0.5・1・0.5 で不履行）");
        // ⑤ 門が開いた月で0
        var g = Board();
        Month(g, 0.5);
        g.Coef.MaxInterestShareOfRevenue = new Share(1e9); g.LastInterestPerMonth = new MoneyPerMonth(0); g.LastRevenuePerMonth = g.NominalGdpPerMonth;   // 利払い÷歳入が限界の下＝借りられる
        Month(g, 0.5);
        Assert.Equal(0.0, g.InsolvencyMonths);
        g.Coef.MaxInterestShareOfRevenue = new Share(0);
        Month(g, 0.5);
        Assert.False(g.HadSovereignDefault, "門が開いた月を挟んだのに累計が残った");
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
            w.Coef.BondAppetite = new Share(0);                 // 国内の買い手0＝外資のプールだけ
            w.Coef.BorrowFractionOfGdp = new Share(10);         // 1件の額＞外資のプール
            w.LastInterestPerMonth = new MoneyPerMonth(0); w.LastRevenuePerMonth = w.NominalGdpPerMonth;   // 借入の上限の下
            w.GovBondRatePerYear = new RatePerYear(rate);
            w.ForeignBondInvestorPool = 1e30;       // 外国の貸し手が居る盤面（2026-09-29 A5＝未注入は外資0・共有層の注入の代わり）
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
        foreach (var r in rw.Regions) foreach (var p in r.Pops) if (p.Savings.Value > cap.Savings.Value) cap = p;
        double scale = Math.Max(1.0, rw.TotalMoney.Value);
        double loan = 0.3 * scale, overdraft = rw.Treasury.Value + 0.2 * scale, hh = 0.05 * scale;
        World.Transfer((null, null), (rw, [cap], [1.0]), loan, outside: World.Outside.Credit);   // 信用創造（与信と預金の対）＝移し方の1本（2026-09-30 段4g）
        World.Transfer((rw, null), (rw, [cap], [1.0]), overdraft, TreasuryCat.OtherOut);        // 国庫の当座借越での支出
        Bank.Lend(rw, cap, hh);                                                                   // 家計の借入（Pop.Debt と HouseholdLoans の対）
        // 請求権：反乱国の対外債（仏と英が3:1で持つ・国内元本は不変＝保有台帳は割れない）と、外資の負債・対外資産
        double fd = 0.1 * scale;
        rw.ForeignDebt += new MoneyStock(fd); rw.GovDebt += new MoneyStock(fd); rw.PrevForeignDebt += new MoneyStock(fd);
        fw.ForeignBondClaims += new MoneyStock(0.75 * fd * rw.FxRate / fw.FxRate); pw.ForeignBondClaims += new MoneyStock(0.25 * fd * rw.FxRate / pw.FxRate);   // 債権は持ち手の国の通貨（金の値打ちで 3:1）
        rw.ForeignCapitalIn += new MoneyStock(0.07 * scale); rw.CapitalAbroad += new MoneyStock(0.04 * scale);
        new BondLedger().Check(rw);
        Assert.True(rw.Treasury.Value < 0 && rb.Loans.Value > 0 && rb.HouseholdLoans.Value > 0, $"前提が崩れた：国庫 {rw.Treasury}・与信 {rb.Loans}・家計の与信 {rb.HouseholdLoans}");
        // ★2026-09-30 段4e（人と動くお金の換算）：反乱国の為替を親の 0.8 倍に置く＝国庫・与信・貯蓄・債務・対外の残高は親の通貨へ換算して渡る（差は換算の台帳へ）
        rw.ExchangeRate = pw.FxRate * 0.8;
        double rho = rw.FxRate / pw.FxRate;
        Assert.True(Math.Abs(rho - 0.8) < 1e-12, "前提が崩れた：反乱国の為替が置けていない");

        var all0 = sim.Nations.ConvertAll(n => n.World).ToArray();
        double world0 = WorldBase(all0), rebelBase = rw.BaseMoney.Value, parentBase0 = pw.BaseMoney.Value;
        double Sum(World[] ws, Func<World, double> f) { double s = 0; foreach (var w in ws) s += f(w); return s; }
        double ca0 = Sum(all0, w => w.CapitalAbroad.Value), fci0 = Sum(all0, w => w.ForeignCapitalIn.Value);
        // ★段3-3（2026-09-30）：債権は持ち手の国の通貨＝否認は金建ての債権の比で落ちる（債権の増減の1本 CapitalMarket.AdjustClaims）。反乱国自身の債権は名目のまま親へ（貯蓄と同じ）
        double heldOthers = fw.ForeignBondClaims.Value * fw.FxRate + pw.ForeignBondClaims.Value * pw.FxRate, repudiated = (rw.PrevForeignDebt * rw.FxRate).Value;
        double fwClaims0 = fw.ForeignBondClaims.Value, pwClaims0 = pw.ForeignBondClaims.Value, rwClaims0 = rw.ForeignBondClaims.Value;
        Assert.True(heldOthers >= repudiated, $"前提が崩れた：反乱国以外の債権 {heldOthers} が否認する対外債 {repudiated} に届かない");
        double loans0 = pb.Loans.Value, hh0 = pb.HouseholdLoans.Value, tr0 = pw.Treasury.Value, rT = rw.Treasury.Value, rL = rb.Loans.Value, rHH = rb.HouseholdLoans.Value;
        double fx0 = sim.Diplomacy.CumulativeFxResidual;
        double caGold0 = Sum(all0, w => w.CapitalAbroad.Value * w.FxRate), fciGold0 = Sum(all0, w => w.ForeignCapitalIn.Value * w.FxRate);
        var guard = new MoneyConservation(pw);
        var returning = new List<Region>(rw.Regions);

        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
        sim.ReabsorbNations();

        Assert.DoesNotContain(sim.Nations, n => ReferenceEquals(n.World, rw));
        var all1 = sim.Nations.ConvertAll(n => n.World).ToArray();
        double tol = 1e-9 * Math.Max(1.0, world0);
        // (b) 世界の貨幣は湧かず消えない＝名目の和の動きは換算の台帳の動きだけ／親の基礎貨幣の増え＝反乱国の基礎貨幣（貯蓄＋国庫−与信）の金の値打ち（親の通貨）
        double fxMoved = sim.Diplomacy.CumulativeFxResidual - fx0;
        Assert.True(Math.Abs(fxMoved) > 1e-9 * Math.Max(1.0, Math.Abs(rebelBase)), "為替が違うのに換算の台帳が動かない");
        Assert.True(Math.Abs(WorldBase(all1) - world0 - fxMoved) <= tol, $"再併合で世界の基礎貨幣が換算の台帳の外で {WorldBase(all1) - world0 - fxMoved} 動いた");
        Assert.True(Math.Abs(pw.BaseMoney.Value - parentBase0 - rebelBase * rho) <= tol, $"親の基礎貨幣の増え {pw.BaseMoney.Value - parentBase0} ≠ 反乱国の基礎貨幣×換算 {rebelBase * rho}");
        guard.Check(pw);   // 親の国ごとの保存も閉じる（累積貿易に換算した額を記帳）
        // 貨幣の行は親へ（親の通貨へ換算）：与信・家計の与信・国庫（負のまま）
        Assert.True(Math.Abs(pb.Loans.Value - loans0 - rL * rho) <= tol, $"親の与信の増え {pb.Loans.Value - loans0} ≠ 反乱国の与信×換算 {rL * rho}");
        Assert.True(Math.Abs(pb.HouseholdLoans.Value - hh0 - rHH * rho) <= tol, $"親の家計の与信の増え {pb.HouseholdLoans.Value - hh0} ≠ 反乱国の家計の与信×換算 {rHH * rho}");
        Assert.True(Math.Abs(pw.Treasury.Value - tr0 - rT * rho) <= tol, $"親の国庫の動き {pw.Treasury.Value - tr0} ≠ 反乱国の国庫×換算 {rT * rho}");
        Assert.True(Math.Abs(cap.Debt.Value - hh * rho) <= 1e-12 * Math.Max(1.0, hh), $"家計の債務の記録 {cap.Debt} ≠ 換算 {hh * rho}");   // POP と一緒に親へ（HouseholdLoans の対・同じ換算）
        // 請求権：外資の負債と対外資産は州について親へ（世界の金の値打ちの和は不変）／反乱国の対外債は否認＝債権の和が同額減る（金は動かない）
        double tolC = 1e-9 * Math.Max(1.0, scale);
        Assert.True(Math.Abs(Sum(all1, w => w.CapitalAbroad.Value * w.FxRate) - caGold0) <= tolC, "再併合で世界の対外資産の金の値打ちの和が動いた");
        Assert.True(Math.Abs(Sum(all1, w => w.ForeignCapitalIn.Value * w.FxRate) - fciGold0) <= tolC, "再併合で世界の外資負債の金の値打ちの和が動いた");
        Assert.True(Math.Abs(pw.ForeignBondClaims.Value - (pwClaims0 * (1 - repudiated / heldOthers) + rwClaims0 * rho)) <= tolC, $"親の債権 {pw.ForeignBondClaims.Value} ≠ 否認で落ちた親の債権＋反乱国の債権×換算");
        Assert.True(Math.Abs(fw.ForeignBondClaims.Value - fwClaims0 * (1 - repudiated / heldOthers)) <= tolC, "否認が債権の比で落ちていない");
        // (c) 反乱国の国債の保有は消える（持ち手の損・親は継承しない）
        foreach (var r in returning) foreach (var p in r.Pops) Assert.Equal(0.0, p.BondHoldings.Value);
        new BondLedger().Check(pw);
        sim.StepQuarter();   // 返った州と与信を含めて1四半期（エンジンの月次検査）
        new BondLedger().Check(pw);
    }

    /// <summary>★対外借入の貸し手の払いは貸し手の貯蓄まで・払えなかった分の借入は立たない（支払いの一元化 段2-2・A5・2026-09-29）。
    /// 盤面＝英仏2国（外交を止めた共有層）を1四半期温め、仏の貸し手（近代エリート）の貯蓄を小さくして、英が外資を大きく借りた形（Fiscal.Borrow の入金と同じ5行）を置く。
    /// 四半期末の引き落とし（MultiNationSim）の後：仏の貸し手の貯蓄は負にならない（旧＝貯蓄の比で請求額を引き、負へ押した＝他人から取る）・
    /// 英の対外債は貸し手が払えなかった分だけ減る（旧＝貸し手不在なら控除なし＝借り手の受領だけが残る湧き口）。
    /// ⚠突然変異「貸し手の貯蓄で切らない」「払えなかった分を戻さない」で赤。</summary>
    [Fact]
    public void 対外借入は貸し手の貯蓄までで払えなかった分は立たない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.Coef.ForeignBondCapacity = new Share(0);   // この四半期に英が自分で外資を借りない（盤面の借入だけを測る）
        var lenders = new List<Pop>();
        foreach (var r in fr.Regions)
        {
            var sink = r.Pops.Find(p => !p.IsModernElite && !p.IsSoldier)!;   // 貸し手の貯蓄の余りを同じ州の大衆へ移す＝仏の基礎貨幣は動かない（保存量の検査を通る盤面）
            foreach (var p in r.Pops) if (p.IsModernElite && !p.IsSoldier) { double ex = p.Savings.Value - 10; p.SetSavings(10); sink.SetSavings(sink.Savings.Value + (ex)); lenders.Add(p); }
        }
        Assert.NotEmpty(lenders);
        const double X = 1e9;
        World.Transfer((null, null), (uk, null, null), X, TreasuryCat.Borrow); uk.GovDebt += new MoneyStock(X); uk.ForeignDebt += new MoneyStock(X); uk.PendingForeignBorrow += X;
        double fd0 = uk.ForeignDebt.Value;
        sim.StepQuarter();
        foreach (var p in lenders) Assert.True(p.Savings.Value >= -1e-9, $"貸し手の貯蓄が負になった（{p.Savings}）＝他人から取った");
        Assert.True(uk.ForeignDebt.Value < fd0 - 0.9 * X, $"貸し手が払えなかった借入が対外債に残った（{fd0} → {uk.ForeignDebt}）");
    }

    /// <summary>★新規の資本移動の受け手は、送り手から**実際に動いた額**だけ受け取る（支払いの一元化 段2-2・A6・2026-09-29）。
    /// 旧＝受け手は traded を満額受け取り、送り手だけ投資家の貯蓄で切った＝切られた分だけ世界に湧いた。盤面＝英仏米の3国を1四半期温め、
    /// 送り手の供給と受け手の需要を大きくして（CapitalExportFrac・CapitalDemandScale）送り手の貯蓄で切られる形にし、国際金融の段を1回。
    /// 対外の利子の管（ForeignIntPaidThisQuarter＝月次の段で払い手の側を記帳済みの片側）を空にすれば、段の中の流れは全て両建て＝世界の台帳の和は動かない。
    /// ⚠突然変異「受け手へ traded の比で満額を渡す」で赤。</summary>
    [Fact]
    public void 資本移動の受け手は送り手から動いた額だけ受け取る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("アメリカ")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        var c = new Coefficients { CapitalExportFrac = new Share(1e6), CapitalDemandScale = 1e6 };   // 送り手の供給も受け手の需要も投資家の貯蓄より桁違いに大きい＝送り手の貯蓄で切られる
        static double Held(World w) { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) if (CapitalMarket.IsInvestor(p)) s += Math.Max(0, p.Savings.Value); return s; }
        double tf0 = 0, bm0 = 0, inv0 = 0;
        var held0 = new double[sim.Nations.Count];
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; w.ForeignIntPaidThisQuarter = 0; tf0 += w.CumulativeTradeFlow.Value; bm0 += w.BaseMoney.Value;
            held0[i] = Held(w); inv0 += held0[i];
        }
        CapitalMarket.Step(sim.Nations, c);
        double tf1 = 0, bm1 = 0; bool drained = false;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; tf1 += w.CumulativeTradeFlow.Value; bm1 += w.BaseMoney.Value;
            if (held0[i] > 0 && Held(w) <= 1e-6 * held0[i]) drained = true;
        }
        Assert.True(drained, "前提が崩れた：どの送り手も投資家の貯蓄で切られていない");
        double tol = 1e-9 * Math.Max(1.0, inv0);
        Assert.True(Math.Abs(tf1 - tf0) <= tol, $"国際金融の段で世界の台帳の和が動いた（{tf1 - tf0}）＝受け手が送り手の払いより多く受け取った");
        Assert.True(Math.Abs(bm1 - bm0) <= tol, $"国際金融の段で世界の基礎貨幣が動いた（{bm1 - bm0}）");
    }

    // =====================================================================================================
    // ★段3-3（2026-09-30・対外の民間請求＝処理の一元化 段4c-2）の番人：国境をまたぐ払いの換算（A7 と同じ規則）・投資家の述語の1本（C13）・
    //   受け手と払い手の対称と債権の増減の対称（C14）・運河の配当と巨大建造物の外資を投資家へ（C9・C12）・国家間の移転の未払いを対外債へ（B5・Q6）・
    //   内戦の国債は勝者が引き継ぐ（作者裁定 2026-09-29）。
    // =====================================================================================================

    private static MultiNationSim FrozenWorld(params string[] names)
    {
        var sim = new MultiNationSim(names.Select(OpeningFixture.Named).ToArray(), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };   // ★(332) 国庫・国債は絶対額（助走なしの器）
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        return sim;
    }
    private static double NonInvestorSavings(World w) { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) if (!CapitalMarket.IsInvestor(p)) s += p.Savings.Value; return s; }

    /// <summary>★国境をまたぐ払いは為替で換算する（移し方の1本 World.Transfer・段3-3）：払い手は自国の通貨で X、受け手はその金の値打ちを自国の通貨で受け取る
    /// （×払い手の為替÷受け手の為替）。差は為替の換算の台帳（Diplomacy.CumulativeFxResidual）へ＝世界の名目の動き＝台帳の動き・国ごとの保存は各自の通貨で閉じる。
    /// 受け手の国庫の区分は toCat（貢納の出と入り）。⚠突然変異「換算しない（名目 1:1）」で赤。</summary>
    [Fact]
    public void 国境をまたぐ払いは為替で換算され差は換算の台帳へ入る()
    {
        var sim = FrozenWorld("イギリス", "フランス");
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 0.5;
        var gu = new MoneyConservation(uk); var gf = new MoneyConservation(fr);
        double tu = uk.Treasury.Value, tf = fr.Treasury.Value, fx0 = sim.Diplomacy.CumulativeFxResidual, bm0 = (uk.BaseMoney + fr.BaseMoney).Value;
        double outU = uk.TreasuryLedger[(int)TreasuryCat.TributeOut], inF = fr.TreasuryLedger[(int)TreasuryCat.TributeIn];
        const double X = 1000;
        Assert.Equal(X, World.Transfer((uk, null), (fr, null, null), X, TreasuryCat.TributeOut, toCat: TreasuryCat.TributeIn));   // 戻り値＝払い手の通貨の額
        Assert.Equal(tu - X, uk.Treasury.Value, 6);
        Assert.Equal(tf + 2 * X, fr.Treasury.Value, 6);   // 金の値打ち X（英の通貨・為替1）＝仏の通貨で 2X（為替0.5）
        Assert.Equal(-X, uk.TreasuryLedger[(int)TreasuryCat.TributeOut] - outU, 6);
        Assert.Equal(2 * X, fr.TreasuryLedger[(int)TreasuryCat.TributeIn] - inF, 6);
        Assert.Equal(X, sim.Diplomacy.CumulativeFxResidual - fx0, 6);
        Assert.Equal(sim.Diplomacy.CumulativeFxResidual - fx0, (uk.BaseMoney + fr.BaseMoney).Value - bm0, 6);   // 世界の名目の動き＝換算の台帳
        gu.Check(uk); gf.Check(fr);   // 国ごとの台帳（CumulativeTradeFlow）は各自の通貨の実額
        // 逆向き（仏→英・POP の払い手）＝半分
        var frPops = new List<Pop>(fr.AllPops());
        double moved = World.Transfer((fr, frPops), (uk, null, null), X, TreasuryCat.OtherIn);
        Assert.Equal(X, moved, 6);
        Assert.Equal(tu - X + 0.5 * X, uk.Treasury.Value, 6);
        gu.Check(uk); gf.Check(fr);
    }

    /// <summary>★外資の配当は金の値打ちで動く（国際金融 CapitalMarket・段3-3）：受け入れ国（仏・為替0.5）の投資家が自国の通貨で払い、持ち主の国（英・為替1）の投資家が
    /// 金の値打ちを自国の通貨で受け取る（配当＝外資の負債×率）。投資家でない POP は1ビットも動かない・世界の名目の動き＝換算の台帳。
    /// ⚠突然変異「還流を換算しない（名目 1:1）」で赤。</summary>
    [Fact]
    public void 外資の配当は金の値打ちで換算して持ち主の国の投資家へ届く()
    {
        var coef = new Coefficients { CapitalDividendFromHostRent = 0, CapitalFlightRatePerQuarter = new RatePerQuarter(0), CapitalDemandScale = 0, CapitalDomesticFlightRatePerQuarter = new RatePerQuarter(0) };   // 配当だけ（★(801) 旗 0 を明示＝旧の形の払い）
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent(), coef) { DiplomacyFrozen = true };
        CapitalMarket.Step(sim.Nations, coef);   // 開始盤面の繰上げを1回
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 0.5;
        foreach (var w in new[] { uk, fr }) { w.ForeignIntPaidThisQuarter = 0; w.ForeignCapitalIn = default; w.CapitalAbroad = default; }
        const double F = 1e7;
        fr.ForeignCapitalIn = new MoneyStock(F); uk.CapitalAbroad = new MoneyStock(0.5 * F);   // 英の投資家が仏に持つ外資（金の値打ちで同じ）
        double invF0 = CapitalMarket.InvestorSavings(fr), invU0 = CapitalMarket.InvestorSavings(uk), othF0 = NonInvestorSavings(fr), othU0 = NonInvestorSavings(uk);
        double fx0 = sim.Diplomacy.CumulativeFxResidual, bm0 = (uk.BaseMoney + fr.BaseMoney).Value;
        Assert.True(invF0 > coef.CapitalDividendRatePerQuarter.Value * F, "前提が崩れた：仏の投資家が配当を払い切れない");
        CapitalMarket.Step(sim.Nations, coef);
        double paid = invF0 - CapitalMarket.InvestorSavings(fr), got = CapitalMarket.InvestorSavings(uk) - invU0;
        double tol = 1e-9 * Math.Max(1.0, invF0 + invU0);
        Assert.True(Math.Abs(paid - coef.CapitalDividendRatePerQuarter.Value * F) <= tol, $"仏の投資家の払い {paid} ≠ 配当 {coef.CapitalDividendRatePerQuarter.Value * F}");
        Assert.True(Math.Abs(got - 0.5 * paid) <= tol, $"英の投資家の受け取り {got} ≠ 金の値打ち（払いの半分）{0.5 * paid}");
        Assert.True(Math.Abs(NonInvestorSavings(fr) - othF0) <= tol && Math.Abs(NonInvestorSavings(uk) - othU0) <= tol, "投資家でない POP の貯蓄が動いた");
        Assert.True(Math.Abs(sim.Diplomacy.CumulativeFxResidual - fx0 - (got - paid)) <= tol, "換算の差が換算の台帳に入っていない");
        Assert.True(Math.Abs((uk.BaseMoney + fr.BaseMoney).Value - bm0 - (sim.Diplomacy.CumulativeFxResidual - fx0)) <= tol, "世界の名目の動き ≠ 換算の台帳の動き");
    }

    /// <summary>★債権の増減の1本（CapitalMarket.AdjustClaims・C14・段3-3）：債務国の対外債の変化は**債務国以外**の国の債権へ、金建ての債権の比で配る＝
    /// 増えも減りも同じ集合・同じ規則。債務国が他国に持つ債権は動かない（旧＝減りは自国の債権も含む全ての債権の比・増えは自国を除く投資家の貯蓄の比）。
    /// ⚠突然変異「債務国自身の債権も含める」で赤。</summary>
    [Fact]
    public void 債権の増減は債務国自身の債権を含まず金建ての同じ比で動く()
    {
        var (sim, _) = QuietBondWorld();   // 英仏普
        var br = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        br.ExchangeRate = 1.0; fr.ExchangeRate = 0.5; pr.ExchangeRate = 0.8;
        br.ForeignBondClaims = new MoneyStock(300); pr.ForeignBondClaims = new MoneyStock(100); fr.ForeignBondClaims = new MoneyStock(200);   // 仏も他国の外債を持つ
        void Check(double delta)
        {
            double b0 = br.ForeignBondClaims.Value, p0 = pr.ForeignBondClaims.Value;
            CapitalMarket.AdjustClaims(sim.Nations, 1, delta);   // 仏の対外債が delta（仏の通貨）動いた
            Assert.Equal(200.0, fr.ForeignBondClaims.Value);   // 債務国自身の債権は動かない
            double dB = br.ForeignBondClaims.Value - b0, dP = pr.ForeignBondClaims.Value - p0;
            Assert.Equal(delta * 0.5, dB * 1.0 + dP * 0.8, 9);   // 債権国の増減の金の値打ちの和＝対外債の変化の金の値打ち
            Assert.Equal(b0 * 1.0 / (p0 * 0.8), (dB * 1.0) / (dP * 0.8), 9);   // 比は金建ての債権の比
        }
        Check(-100);   // 償還・否認
        Check(+60);    // 利子の繰り入れ
    }

    /// <summary>★対外借入の債権は貸し手が**払った額そのもの**（C13・段3-3）：借入の月に繰上げ済みの印（PrevForeignDebt）も上がり（Fiscal.Borrow）、
    /// 四半期末の貸し手の引き落とし（MultiNationSim）が払った額を貸し手の国の債権にする＝翌四半期の繰上げで数え直さない。借りた額は金の値打ちで貸し手の通貨へ換算。
    /// 盤面＝普英2国・債権国は英だけ＝英の債権の動き（英の通貨）＝普の対外債の動き（普の通貨）×普の為替÷英の為替。
    /// ⚠突然変異「借入の月に繰上げ済みの印を上げない（数え直しで二重）」「貸し手の払いを換算しない」で赤。</summary>
    [Fact]
    public void 対外借入の債権は貸し手が払った額で換算され翌四半期に数え直さない()
    {
        var sim = FrozenWorld("プロイセン", "イギリス");   // 借り手＝普（開幕の国債が小さく借入の上限に届いていない）・貸し手＝英
        sim.StepQuarter();   // 共有層が外資の引き受け余力を注入
        var bw = sim.Nations[0].World; var lw = sim.Nations[1].World;
        // (1296) ㊴ 軌道に依る前提：普がこの四半期に国債を返さないこと（返済は債権の繰上げの後に対外債だけを減らし、英の債権は翌四半期の繰上げで合う＝四半期末の比べでは月 1%/12 の返済 3 か月分＝約 0.25% ずれる）。
        //   旗 UrbanFlowReal 1 の軌道では普の歳入に余りが出て床の返済が走った（種 1789〜1793 の全てで約 0.25% の赤）。盤面で床の返済の率を 0 に置く（換算の規則の検査は変えない）。
        bw.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        Assert.Equal(0.0, bw.Coef.BondRepayFracPerMonth.Value);
        lw.Coef.ForeignBondCapacity = new Share(0);   // 英は外資で借りない（普の債権が動かない）
        Assert.True(bw.ForeignBondInvestorPool > 0, "前提が崩れた：外資の引き受け余力が注入されていない");
        double fd0 = bw.ForeignDebt.Value, prev0 = bw.PrevForeignDebt.Value, cl0 = lw.ForeignBondClaims.Value, gd0 = bw.GovDebt.Value;
        FiscalMonth(bw, Short);   // 払えない月＝借入の1本（Fiscal.Borrow）
        double f = bw.PendingForeignBorrow;
        bw.Coef.FiscalStressBufferMonths = 1e9;   // ★(616) 借りた額が国庫に入り、次の月に線を超えた分として返される（国庫の出口）のを止める＝ここで確かめるのは貸し手の換算で、出口ではない
        Assert.True(f > 0, $"前提が崩れた：普が外資で借りていない（国債 {gd0}→{bw.GovDebt}・利払い {bw.LastInterestPerMonth}・歳入 {bw.LastRevenuePerMonth}・冷却 {bw.BorrowCooldownMonths}）");
        Assert.Equal(prev0 + f, bw.PrevForeignDebt.Value, 6);   // 借入の月に繰上げ済みの印も上がる
        bw.Coef.ForeignBondCapacity = new Share(0);   // 残りの月は借りない
        sim.StepQuarter();
        // ★(420) 合わせた木で為替の差が 0.009 に縮んで前提だけが赤（軌道の移動）。換算しない突然変異は差の分（相対で約 1%）だけ期待とずれる＝許し 1e-9 に対し十分＝前提は〈為替が違う〉（1e-6 を超える）で足りる
        Assert.True(Math.Abs(bw.FxRate - lw.FxRate) > 1e-6, $"前提が崩れた：普と英の為替が同じ（{bw.FxRate}・{lw.FxRate}）");
        Assert.True(bw.ForeignDebt.Value - fd0 > 0.5 * f, "前提が崩れた：貸し手が払えなかった");
        double expect = (bw.ForeignDebt.Value - fd0) * bw.FxRate / lw.FxRate;   // 債権国は英だけ＝英の債権の動き＝普の対外債の動きの金の値打ち
        Assert.True(Math.Abs(lw.ForeignBondClaims.Value - cl0 - expect) <= 1e-9 * Math.Max(1.0, expect), $"英の債権の動き {lw.ForeignBondClaims.Value - cl0} ≠ 普の対外債の動きの金の値打ち {expect}");
        double cl1 = lw.ForeignBondClaims.Value;
        CapitalMarket.Step(sim.Nations, sim.Coef);   // 翌四半期の繰上げ＝数え直さない
        Assert.Equal(cl1, lw.ForeignBondClaims.Value, 9);
    }

    /// <summary>★外資の受け手は投資家だけ（CapitalMarket.GiveGold・C14・段3-3）＝払い手（TakeGold＝投資家だけ）と対称。投資家のいない国（残骸国家の形）は国庫が受け取る
    /// （旧＝投資家→全POP→国庫＝外資の配当や流入が農民の貯蓄へ落ちた）。⚠突然変異「投資家不在なら全POP へ」で赤。</summary>
    [Fact]
    public void 外資の受け手は投資家だけで投資家のいない国は国庫が受け取る()
    {
        var sim = FrozenWorld("イギリス", "フランス");
        var fr = sim.Nations[1].World;
        double inv0 = CapitalMarket.InvestorSavings(fr), oth0 = NonInvestorSavings(fr), t0 = fr.Treasury.Value;
        Assert.Equal(1000, CapitalMarket.GiveGold(fr, 1000), 6);
        Assert.Equal(inv0 + 1000, CapitalMarket.InvestorSavings(fr), 6);
        Assert.Equal(oth0, NonInvestorSavings(fr), 6);
        Assert.Equal(t0, fr.Treasury.Value);
        foreach (var p in fr.AllPops()) p.IsModernElite = false;   // 投資家のいない国
        Assert.Empty(CapitalMarket.Investors(fr));
        var sav = fr.AllPops().Select(p => p.Savings.Value).ToArray();
        CapitalMarket.GiveGold(fr, 1000);
        Assert.Equal(sav, fr.AllPops().Select(p => p.Savings.Value).ToArray());   // POP は1ビットも動かない
        Assert.Equal(t0 + 1000, fr.Treasury.Value, 6);
    }

    /// <summary>★運河の配当は出資した国の**投資家**へ（C9・段3-3）：使用国の国庫から持分（金建て）の比で、出資国の投資家へ直接・国境をまたげば為替で換算。
    /// 出資国の国庫は動かない（旧＝拠出国の国庫へ）。⚠突然変異「配当を出資国の国庫へ」で赤。</summary>
    [Fact]
    public void 運河の配当は出資した国の投資家へ届き国庫へは入らない()
    {
        var sim = FrozenWorld("イギリス", "フランス", "プロイセン");
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World; var pr = sim.Nations[2].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 0.5; pr.ExchangeRate = 0.8;
        var cp = new Chokepoint { Id = "番人の運河", Name = "番人の運河", IsCanal = true, IsOpen = true };
        cp.InvestorShares[uk.SelfPower!.Id] = 3; cp.InvestorShares[pr.SelfPower!.Id] = 1;   // 金建ての持分 3:1
        double tU = uk.Treasury.Value, tF = fr.Treasury.Value, tP = pr.Treasury.Value;
        double iU = CapitalMarket.InvestorSavings(uk), iP = CapitalMarket.InvestorSavings(pr), oU = NonInvestorSavings(uk), oP = NonInvestorSavings(pr);
        Assert.Equal(800, sim.DistributeCanalDividend(cp, 800, fr), 6);   // 使用国＝仏（仏の通貨で 800）
        Assert.Equal(tF - 800, fr.Treasury.Value, 6);
        Assert.Equal(tU, uk.Treasury.Value); Assert.Equal(tP, pr.Treasury.Value);   // 出資国の国庫は動かない
        Assert.Equal(iU + 600 * 0.5 / 1.0, CapitalMarket.InvestorSavings(uk), 6);   // 3/4×800（仏の通貨）＝金 300＝英の通貨 300
        Assert.Equal(iP + 200 * 0.5 / 0.8, CapitalMarket.InvestorSavings(pr), 6);   // 1/4×800＝金 100＝普の通貨 125
        Assert.Equal(oU, NonInvestorSavings(uk), 6); Assert.Equal(oP, NonInvestorSavings(pr), 6);
    }

    /// <summary>★国家間の移転の未払いは対外債（作者裁定 Q6・B5・段3-3）：貢納と賠償の払えない分（1−r）は払い手の国債（対外）になり、受け手の国の対外債権になる
    /// （為替で換算）。国内の保有の台帳は割れない（国内元本は動かない）。払った分は移し方の1本（為替で換算）。
    /// ⚠突然変異「未払いを記録しない」（貢納・賠償それぞれ）で赤。</summary>
    [Fact]
    public void 貢納と賠償の払えない分は払い手の対外債と受け手の国の債権になる()
    {
        var sim = FrozenWorld("イギリス", "フランス");
        var coef = new Coefficients { ColonyCaptureBudget = 0 };   // 講和で植民地を動かさない（割譲州の保有の振替を混ぜない）
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 0.5;
        // (1) 貢納：仏が英の属国・仏の r＝0.25
        sim.Diplomacy.SetDependency(uk.SelfPower!, fr.SelfPower!);
        fr.FiscalFundingRatio = new Share(0.25);
        double tU = uk.Treasury.Value, tF = fr.Treasury.Value, fd = fr.ForeignDebt.Value, gd = fr.GovDebt.Value, pv = fr.PrevForeignDebt.Value, cl = uk.ForeignBondClaims.Value;
        sim.Diplomacy.StepDependency(coef, 0);
        double claim = fr.LastTributeOutPerQuarter;
        Assert.True(claim > 0, "前提が崩れた：貢納の請求が立たない");
        Assert.Equal(tF - 0.25 * claim, fr.Treasury.Value, 6);
        Assert.Equal(tU + 0.25 * claim * 0.5, uk.Treasury.Value, 6);
        Assert.Equal(fd + 0.75 * claim, fr.ForeignDebt.Value, 6);
        Assert.Equal(gd + 0.75 * claim, fr.GovDebt.Value, 6);
        Assert.Equal(pv + 0.75 * claim, fr.PrevForeignDebt.Value, 6);   // 受け手へ直接付けた＝繰上げで数え直さない
        Assert.Equal(cl + 0.75 * claim * 0.5, uk.ForeignBondClaims.Value, 6);
        new BondLedger().Check(fr);
        sim.Diplomacy.RemoveDependency(uk.SelfPower!, fr.SelfPower!);
        // (2) 賠償：英が仏に勝つ（割譲の線の下）・仏の r＝0.4
        fr.FiscalFundingRatio = new Share(0.4);
        tU = uk.Treasury.Value; tF = fr.Treasury.Value; fd = fr.ForeignDebt.Value; cl = uk.ForeignBondClaims.Value;
        sim.Diplomacy.ResolvePeace(coef, new War { A = uk.SelfPower!, B = fr.SelfPower!, Score = 0.5 });
        double rep = tF - fr.Treasury.Value;
        Assert.True(rep > 0, "前提が崩れた：賠償が払われていない");
        Assert.Equal(tU + rep * 0.5, uk.Treasury.Value, 6);
        double unpaid = rep / 0.4 * 0.6;
        Assert.True(Math.Abs(fr.ForeignDebt.Value - fd - unpaid) <= 1e-9 * Math.Max(1.0, unpaid), $"仏の対外債の増え {fr.ForeignDebt.Value - fd} ≠ 賠償の未払い {unpaid}");
        Assert.True(Math.Abs(uk.ForeignBondClaims.Value - cl - unpaid * 0.5) <= 1e-9 * Math.Max(1.0, unpaid), "英の債権が賠償の未払いの金の値打ちだけ増えていない");
        new BondLedger().Check(fr);
    }

    /// <summary>★内戦の国債は勝者が引き継ぐ（作者裁定 2026-09-29・段3-3）：反乱が勝って再統一したら、反乱国の国債（国内の保有と対外債）を統一後の国が引き継ぐ。
    /// 旧政府の国債は既存の規則（急進革命だけ否認）＝急進革命の勝ちでは旧政府の国債だけが消え、反乱国の国債と持ち手は残る。政府の勝ちは既存どおり反乱国の国債が消える
    /// （国債台帳_建国と割譲は対外債へ振替_再併合は反乱国の債務ごと消える）。⚠突然変異「反乱の勝ちでも反乱国の国債を消す」で赤。</summary>
    [Theory]
    [InlineData(Polity.Republic)]
    [InlineData(Polity.ProletarianDictatorship)]
    public void 内戦の国債は勝った反乱側を統一後の国が引き継ぎ旧政府の国債は既存の規則(Polity target)
    {
        var sim = FrozenWorld("イギリス", "フランス");
        var pw = sim.Nations[0].World; var fw = sim.Nations[1].World;
        pw.Coef.ForeignBondCapacity = new Share(0);   // 起債は全額国内（反乱国は親の係数を共有する）
        FiscalMonth(pw, Short);
        sim.StepQuarter();   // 英が国内で起債＝各州の投資家が持ち手
        pw.PendingRebelRegions = [pw.Regions[1]]; pw.PendingRebelPolity = target;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        FiscalMonth(rw, Short);
        sim.StepQuarter();   // 反乱国が自国の投資家から借りる
        double fd = 0.05 * Math.Max(1.0, rw.TotalMoney.Value);   // 反乱国の対外債（仏が持つ）
        rw.ForeignDebt += new MoneyStock(fd); rw.GovDebt += new MoneyStock(fd); rw.PrevForeignDebt += new MoneyStock(fd); fw.ForeignBondClaims += new MoneyStock(fd);
        new BondLedger().Check(rw);
        rw.ExchangeRate = pw.FxRate * 0.8;   // ★2026-09-30 段4e（人と動くお金の換算）：反乱国の通貨の元本と保有は親の通貨へ換算して引き継ぐ
        double rho = rw.FxRate / pw.FxRate;
        var moving = new HashSet<Pop>(rw.AllPops());
        var held = rw.AllPops().ToDictionary(p => p, p => p.BondHoldings);
        Assert.True(HeldOf(rw) > 0, "前提が崩れた：反乱国の国債の持ち手がいない");
        double pwGov = pw.GovDebt.Value, pwFd = pw.ForeignDebt.Value, pwPrev = pw.PrevForeignDebt.Value, rwGov = rw.GovDebt.Value, rwFd = rw.ForeignDebt.Value, rwPrev = rw.PrevForeignDebt.Value, cl = fw.ForeignBondClaims.Value;
        Assert.True(pwGov - pwFd > 0, "前提が崩れた：旧政府の国内債が無い");
        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, true, target));
        sim.ReabsorbNations();
        Assert.DoesNotContain(sim.Nations, n => ReferenceEquals(n.World, rw));
        bool radical = target is Polity.ProletarianDictatorship or Polity.Anarchy;
        double tol = 1e-9 * Math.Max(1.0, pwGov + rwGov);
        // 反乱国の国債＝引き継ぐ（持ち手の保有は残る＝親の通貨へ換算・対外債の債権は動かない）
        foreach (var (p, h) in held) Assert.True(Math.Abs((p.BondHoldings - h * rho).Value) <= tol, $"反乱国の国債の持ち手 {p.ClassName} の保有が {h}×換算 → {p.BondHoldings}");
        Assert.Equal(cl, fw.ForeignBondClaims.Value);
        // 旧政府の国債＝急進革命なら否認（旧政府の州の持ち手の保有は0）・それ以外は継承
        double oldGov = radical ? 0 : pwGov, oldFd = radical ? 0 : pwFd;
        Assert.True(Math.Abs(pw.GovDebt.Value - (oldGov + rwGov * rho)) <= tol, $"統一後の国債 {pw.GovDebt} ≠ 旧政府 {oldGov}＋反乱国 {rwGov}×換算");
        Assert.True(Math.Abs(pw.ForeignDebt.Value - (oldFd + rwFd * rho)) <= tol, $"統一後の対外債 {pw.ForeignDebt} ≠ 旧政府 {oldFd}＋反乱国 {rwFd}×換算");
        Assert.True(Math.Abs(pw.PrevForeignDebt.Value - (pwPrev + rwPrev * rho)) <= tol, "繰上げ済みの印が換算して足されていない");
        if (radical) foreach (var p in pw.AllPops()) if (!moving.Contains(p)) Assert.Equal(0.0, p.BondHoldings.Value);
        new BondLedger().Check(pw);
        sim.StepQuarter();   // 返った州を含めて1四半期（エンジンの月次検査）
        new BondLedger().Check(pw);
    }

    /// <summary>★費用の側（2026-09-30 (135)）：連合維持費の人頭割り（治安維持費＝係数×治安維持力の基礎×人口・行政費＝係数×人口×距離の割増）は無い
    /// ＝治安維持力と人口は月の国庫の請求を動かさない（兵士と官僚の給与は兵力と行政の購買の1本＝同じ役務の二重・三重の支払いを撤去した）。
    /// 盤面＝英の静かな盤面（税・福祉・年金・国債が0＝財政の月の流れが無い）で、治安維持力 0 と 3000・人口そのままと2倍。国庫・貯蓄・台帳の Admin が1ビットも動かない。
    /// ⚠突然変異「治安維持費を戻す」「行政費の人頭割りを戻す」で赤。</summary>
    [Fact]
    public void 連合維持費の人頭割りは無く治安維持力と人口は国庫の請求を動かさない()
    {
        (double treasury, double admin, double[] sav) Board(double coercion, double popScale)
        {
            var w = QuietBritain();
            w.Coef.TitheRate = new Share(0);
            w.BaseCoercion = coercion;
            if (popScale != 1.0) foreach (var r in w.Regions) foreach (var p in r.Pops) p.Size *= popScale;
            double t0 = w.Treasury.Value, a0 = w.TreasuryLedger[(int)TreasuryCat.Admin];
            FiscalMonth(w, 1.0);
            return (w.Treasury.Value - t0, w.TreasuryLedger[(int)TreasuryCat.Admin] - a0, AllPopsOf(w).ConvertAll(p => p.Savings.Value).ToArray());
        }
        var zero = Board(0, 1.0);
        Assert.Equal(0.0, zero.treasury);   // 前提：静かな盤面の月の財政は国庫を動かさない
        Assert.Equal(0.0, zero.admin);
        foreach (var (coercion, scale) in new[] { (3000.0, 1.0), (0.0, 2.0), (3000.0, 2.0) })
        {
            var arm = Board(coercion, scale);
            Assert.Equal(0.0, arm.treasury);   // 治安維持費・行政費の人頭割りが戻れば国庫が減る
            Assert.Equal(0.0, arm.admin);
            for (int i = 0; i < arm.sav.Length; i++) Assert.Equal(zero.sav[i], arm.sav[i]);   // 担い手（兵士・官僚）への給与の上乗せも無い
        }
    }

    /// <summary>★行政の距離の割増（2026-09-30 (135)＝作者裁定 2026-09-03 E-05段4「維持費は本国州にも必要なはずだ。植民地は遠く、それが高額なだけ」を
    /// 維持費の人頭割りから行政の購買へ移した）：遠い州の官僚は同じ働きに割増の俸給＝国の払う単価は市場の価格×割増・予算も同じ割増
    /// ＝充足は割増の無い州と同じ・国庫の支払いだけが割増倍（Fiscal.AdminDistanceMul の1本を予算〔Simulation〕・単価〔EconomyStep〕・請求〔Fiscal〕が読む）。
    /// 盤面＝英の同じ種の双子（DistanceWarDecay 0＝割増1・10＝首都から離れた州ほど割増）＝同じ州で予算を割増倍にして EconomyStep を1回。
    /// ⚠突然変異「単価に割増を掛けない」「見積もりに割増を掛けない」で赤（支払いか充足が食い違う）。</summary>
    [Fact]
    public void 遠い州の行政は同じ充足に距離の割増の金額を払う()
    {
        (World w, Region r) Board(double general)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            sim.Run(1);
            var w = sim.World;
            w.Coef.DistanceWarDecay = general;
            Region? far = null; double km = 0;
            foreach (var rg in w.Regions)
            {
                if (rg.BureaucratPopIndex < 0) continue;
                var rc = Places.Coord(rg.Name);
                if (rc.Lat == 0 && rc.Lon == 0) continue;
                double d = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
                if (d > km) { km = d; far = rg; }
            }
            Assert.True(far is not null && km > 100, "前提が崩れた：首都から離れた官僚のいる州が無い");
            return (w, far!);
        }
        var (a, ra) = Board(0.0);
        var (b, rb) = Board(10.0);
        Assert.Equal(ra.Name, rb.Name);
        Assert.Equal(1.0, Fiscal.AdminDistanceMul(a, ra));   // 係数0＝割増なし
        double mul = Fiscal.AdminDistanceMul(b, rb);
        var rc2 = Places.Coord(rb.Name);
        double expect = 1.0 + b.Coef.DistanceWarDecay * Geo.DistanceKm(b.Latitude, b.Longitude, rc2.Lat, rc2.Lon) / Diplomacy.DistanceNormKm;
        Assert.Equal(expect, mul, 12);
        Assert.True(mul > 1.05, $"前提が崩れた：割増が小さすぎて測れない（{mul}）");
        // 同じ予算の比（予算で必要量の1割しか買えない＝供給でなく予算が効く盤面）・上下水道と電化は止める（同じ区分 Admin に記帳する）
        double est = b.Coef.AdminNeedPerPop * ra.SumPopulation() * Math.Max(ra.Markets[Catalog.AdminId].Price, 1e-12);
        foreach (var (w, r, m) in new[] { (a, ra, 1.0), (b, rb, mul) })
        {
            r.AdminBudget = 0.1 * est * m; r.InfraBudget = 0; r.ElecInfraBudget = 0;
            r.StateSpent[(int)TreasuryCat.Admin] = 0;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        }
        Assert.True(ra.AdminFill > 0.02 && ra.AdminFill < 0.3, $"前提が崩れた：予算が効いていない（充足 {ra.AdminFill}）");
        Assert.True(Math.Abs(rb.AdminFill - ra.AdminFill) <= 1e-9, $"充足が割増で変わった（{ra.AdminFill} vs {rb.AdminFill}）＝見積もりと予算の割増が食い違う");
        double pa = ra.StateSpent[(int)TreasuryCat.Admin], pb = rb.StateSpent[(int)TreasuryCat.Admin];
        Assert.True(pa > 0, "前提が崩れた：行政を買っていない");
        Assert.True(Math.Abs(pb - mul * pa) <= 1e-9 * pb, $"国庫の支払い {pb} ≠ 割増 {mul}×割増なし {pa}");
    }

    /// <summary>★行政の予算と請求は同じ規則（2026-09-30 (135)・分子と分母を同じ規則で）：州の行政の予算＝行政費×割合×歳入×人口の比×距離の割増（Simulation）・
    /// 国の行政の請求（LastAdminClaimPerMonth＝余剰の控除と財政の安全水準が読む）＝行政費×割合×歳入×距離の割増の人口加重の平均（Fiscal）。
    /// 盤面＝英（距離の割増50倍＝州ごとに割増が大きく違う）を1四半期。①予算÷（人口×割増）が全ての官僚のいる州で同じ（人口は予算を配った月初から
    /// 1か月の動きの分だけずれる＝2%）②月の財政の直後の請求＝行政費×割合×歳入×平均の割増。
    /// ⚠突然変異「予算に割増を掛けない」「請求に平均の割増を掛けない」で赤。</summary>
    [Fact]
    public void 行政の予算は人口の比と距離の割増で配り国の請求は同じ割増の平均()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.Coef.DistanceWarDecay = 50.0;
        sim.Run(1);   // この四半期の月の予算は全て割増で配る
        double k = double.NaN; int n = 0; double lo = double.MaxValue, hi = 0;
        foreach (var r in w.Regions)
        {
            if (r.BureaucratPopIndex < 0 || !(r.AdminBudget > 0)) continue;
            double m = Fiscal.AdminDistanceMul(w, r);
            lo = Math.Min(lo, m); hi = Math.Max(hi, m);
            double v = r.AdminBudget / (r.SumPopulation() * m);
            if (double.IsNaN(k)) k = v; else Assert.True(Math.Abs(v - k) <= 0.02 * k, $"州 {r.Name} の予算÷（人口×割増）{v} ≠ {k}（割増 {m}）");
            n++;
        }
        Assert.True(n >= 2 && hi > lo * 1.3, $"前提が崩れた：割増の違う州が2つ以上ない（{n}・{lo}〜{hi}）");
        w.FiscalFundingRatio = new Share(1.0); Fiscal.Step(w, w.Coef);
        double claim = (w.Coef.AdminBudgetFrac * w.AdminSpending * w.LastRevenuePerMonth * Fiscal.AdminDistanceMean(w)).Value;
        Assert.True(Fiscal.AdminDistanceMean(w) > 1.3, "前提が崩れた：平均の割増が小さい");
        Assert.Equal(claim, w.LastAdminClaimPerMonth.Value);   // 同じ式・同じ順＝ビット一致
    }
    /// <summary>★(804) 行政の距離の割増の純関数 <see cref="Fiscal.AdminDistanceMulOfKm"/>：1＋減衰の重み×距離 km÷1万km（距離が正でない・有限でないときは 1）。期待は手で書いた値。
    /// ⚠突然変異「減衰の重みを落とす」「IsFinite を外す」「km&gt;0 を外す」「÷1万を×1万にする」で赤。</summary>
    [Theory]
    [InlineData(5000.0, 0.0, 1.0)]
    [InlineData(10000.0, 1.0, 2.0)]
    [InlineData(5000.0, 2.0, 2.0)]
    [InlineData(20000.0, 0.5, 2.0)]
    [InlineData(0.0, 3.0, 1.0)]
    [InlineData(-5000.0, 1.0, 1.0)]
    [InlineData(double.PositiveInfinity, 1.0, 1.0)]
    [InlineData(double.NaN, 1.0, 1.0)]
    public void 行政の距離の割増の純関数は手で書いた値を返す(double km, double decay, double expected)
        => Assert.Equal(expected, Fiscal.AdminDistanceMulOfKm(km, decay));

    /// <summary>★段3-4（2026-09-30・B6・作者裁定 Q4＝処理の一元化 段4d）：POP の義務は1本の率＝税と十分の一税の請求を POP ごとに集め、
    /// 払える額（貯蓄）が足りなければ全ての請求に同じ率 r_p＝貯蓄÷請求の和を掛ける（国家が教会に優先しない）。足りれば全額。
    /// どの義務も払い手の総所得（Earned）を減らさず、十分の一税は聖職の所得になる（C15 の確かめ＝所得税と同じ記帳）。
    /// 盤面＝英の静かな盤面で所得税 10%・十分の一税 5%・汚職の漏れ0・1人の大衆の POP だけに前の月の所得 1000。
    /// ⚠突然変異「税を先に取り、十分の一税は残りから（旧の順序つきクランプ）」で赤（十分の一税が0になる）。</summary>
    [Fact]
    public void POPの義務は1本の率で税と十分の一税を同じ割合で払う()
    {
        (double tax, double tithe, double paid, double earnedDrop, double clergyEarned) Month(double savings)
        {
            var w = QuietBritain();
            w.TaxRate = new Share(0.1); w.Coef.TitheRate = new Share(0.05); foreach (var rg0 in w.Regions) { rg0.Rule = [1.0, 0.0, 0.0, 0.0]; rg0.AdminFill = 1.0; }   // 国家が全域を治め、官僚の役務も満額届く（支配度の区分でも行政の充足〔★(408) 旗 1〕でも目減りしない）
            w.Coef.TaxConsentWeight = 0;   // 課税への同意（発言権の無い大衆から取りきれない分）を外す＝請求は査定の全額
            w.StateReligion = StateReligionLaw.StateChurch;
            foreach (var r in w.Regions) foreach (var q in r.Pops) { q.EarnedPerMonth = new MoneyPerMonth(0); q.LastEarnedPerMonth = new MoneyPerMonth(0); }
            Pop? p = null;
            foreach (var r in w.Regions) foreach (var q in r.Pops) if (p is null && Politics.IsMassPop(q) && q.Size > 1) p = q;
            Assert.True(p is not null, "前提が崩れた：大衆の POP が無い");
            p!.LastEarnedPerMonth = new MoneyPerMonth(1000); p.SetSavings(savings); p.EarnedPerMonth = new MoneyPerMonth(7);
            var clergy = AllPopsOf(w).FindAll(q => q.ClassId == PopClass.Clergy && q.Size > 1e-9);
            Assert.NotEmpty(clergy);
            double c0 = 0, ce0 = 0; foreach (var q in clergy) { c0 += q.Savings.Value; ce0 += q.EarnedPerMonth.Value; }
            double t0 = w.TreasuryLedger[(int)TreasuryCat.Tax];
            FiscalMonth(w, 1.0);
            double c1 = 0, ce1 = 0; foreach (var q in clergy) { c1 += q.Savings.Value; ce1 += q.EarnedPerMonth.Value; }
            return (w.TreasuryLedger[(int)TreasuryCat.Tax] - t0, c1 - c0, savings - p.Savings.Value, 7 - p.EarnedPerMonth.Value, ce1 - ce0);
        }
        double taxClaim = 0.1 * 1000, titheClaim = 0.05 * 1000;
        // 足りる＝全額
        var full = Month(1e6);
        Assert.True(Math.Abs(full.tax - taxClaim) <= 1e-9 * taxClaim, $"税が全額でない（{full.tax}）");
        Assert.True(Math.Abs(full.tithe - titheClaim) <= 1e-9 * titheClaim, $"十分の一税が全額でない（{full.tithe}）");
        // 足りない＝同じ率（貯蓄 30÷請求 150＝0.2）
        const double s = 30;
        var shortM = Month(s);
        double rp = s / (taxClaim + titheClaim);
        Assert.True(Math.Abs(shortM.paid - s) <= 1e-9, $"払った額 {shortM.paid} が貯蓄 {s} でない");
        Assert.True(Math.Abs(shortM.tax - taxClaim * rp) <= 1e-9, $"税 {shortM.tax} が請求×率 {taxClaim * rp} でない＝国家が優先している");
        Assert.True(Math.Abs(shortM.tithe - titheClaim * rp) <= 1e-9, $"十分の一税 {shortM.tithe} が請求×率 {titheClaim * rp} でない");
        // 所得の記帳（C15）：払い手の総所得は減らない・聖職の所得は受け取った十分の一税
        Assert.Equal(0.0, shortM.earnedDrop);
        Assert.True(Math.Abs(shortM.clergyEarned - shortM.tithe) <= 1e-9, "聖職の所得（Earned）が受け取った十分の一税と一致しない");
    }

    /// <summary>★(786) 金建て債務の実質負担 b＝参照の銀価格÷世界の銀価格（銀本位国・対外分だけ）。下端 1.0 は残り（銀が参照より高い＝負担なし）、上端の挟み GoldDebtBurdenCap（3.0）は消えた＝
    /// b が旧の上端を越えてもそのまま利払いに掛かる（有限）。手の盤面：英の静かな盤面を銀本位・対外債 0.5 の割合にし、世界の銀価格だけを替えて 1 か月の利払い（LastInterestPerMonth）を比べる。
    /// 期待は式 1＋対外の割合×(b−1)（b＜1 は 1）から作る（コードの形ではなく定義から）。</summary>
    [Theory]
    [InlineData(2.0, 1.0)]      // 銀が参照の 2 倍＝b＝0.5＜1 → 下端で負担なし
    [InlineData(1.0, 1.0)]      // b＝1
    [InlineData(1.0 / 1.7, 1.7)]   // 旧の上端の内側：そのまま
    [InlineData(1.0 / 5.0, 5.0)]   // 旧の上端 3.0 の外側：旧は 3.0 に挟んだ（利払い 1＋0.5×2）・今はそのまま 5
    [InlineData(double.NaN, 1.0)]  // 世界の銀価格が NaN＝負担の節に入らない（> 1e-9 が偽）→ 負担なし
    [InlineData(double.PositiveInfinity, 1.0)]   // 世界の銀価格が +∞＝b＝0 → 下端で負担なし
    [InlineData(1.0 / 1e4, 1e4)]   // 比の経路の上限の桁（局所価格の帯 0.01〜100 から約 1e4）：有限のまま
    public void 金建て債務の負担は下端1で残り上端の挟みは無い(double silverVsRef, double expectedB)
    {
        double refSilver = Catalog.RefPrices[Catalog.SilverId];
        double Interest(double ws)
        {
            var w = QuietBritain();
            w.MonetaryLaw = MonetaryStandardLaw.Silver;
            w.GovDebt = new MoneyStock(1e9); w.ForeignDebt = new MoneyStock(5e8);   // 対外の割合 0.5
            w.WorldSilverPrice = ws;
            w.FiscalFundingRatio = new Share(1.0);
            Fiscal.Step(w, w.Coef, 0);
            return w.LastInterestPerMonth.Value;
        }
        double i0 = Interest(refSilver);   // b＝1（負担なし）
        Assert.True(i0 > 0, "前提が崩れた：利払いが立たない");
        double got = Interest(refSilver * silverVsRef) / i0;
        double want = 1.0 + 0.5 * (expectedB - 1.0);
        Assert.True(Math.Abs(got - want) <= 1e-9 * want, $"b＝{expectedB} の利払いの倍率 {got}（期待 {want}）");
    }

    /// <summary>★(792) 金建て債務の負担 b の否定形のガード <see cref="Fiscal.GoldDebtBurden"/>（括り出した純関数）の手の盤面。上の Theory は世界を通すので、b が NaN・+∞・1 未満・負になる入口（参照の銀価格・世界の銀価格の非有限や負）には届かず、
    /// 下端 1.0 の倒し先を壊す突然変異（(786) の M2・M3）が緑のままだった（goldDebtBurden の読み手は `&gt; 1.0` の比較だけ＝1 未満は下流に出ない）。ここは関数の返り値を直接読む＝期待は手で書く（式から作らない）。
    /// 1 以上で有限なら b をそのまま返し（上端の挟みは無い）、NaN・+∞・1 未満・負は 1.0 へ倒れる（否定形＝NaN は倒れる側）。</summary>
    [Theory]
    [InlineData(1.0, 2.0, 1.0)]                                      // b＝0.5＜1 → 1.0
    [InlineData(1.0, -1.0, 1.0)]                                     // b＝−1 → 1.0
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 1.0)]   // b＝∞÷∞＝NaN → 1.0
    [InlineData(double.PositiveInfinity, 1.0, 1.0)]                  // b＝+∞ → 1.0
    [InlineData(1.0, 0.0, 1.0)]                                      // b＝1÷0＝+∞ → 1.0
    [InlineData(1e-10, 1.0, 1.0)]                                    // 参照の銀価格が 1e-9 以下＝b を割らず 1.0
    [InlineData(1.0, 1.0, 1.0)]                                      // b＝1 ちょうど → 1.0（そのまま）
    [InlineData(1.7, 1.0, 1.7)]                                      // 有限で 1 以上 → そのまま
    [InlineData(5.0, 1.0, 5.0)]                                      // 旧の上端 3.0 の外側でもそのまま
    [InlineData(1e4, 1.0, 1e4)]                                      // 比の経路の上限の桁でもそのまま
    public void 金建て債務の負担の関数は非有限と1未満を1へ倒し有限の1以上はそのまま返す(double refSilver, double worldSilver, double expected)
    {
        Assert.Equal(expected, Fiscal.GoldDebtBurden(refSilver, worldSilver));
    }

}
