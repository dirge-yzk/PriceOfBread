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
    //   赤字は増税でなく未払い（Fiscal の利払い・維持費の Min）→FiscalFundingRatio の劣化→デフォルト（DefaultTriggerQuarters）で閉じる。
    [Fact]
    public void 低税では緊縮が発火し逆進課税の下限の門になる()   // §6.2（赤字→借金ボタン→債務→緊縮：債権者が奪うのは法を選ぶ自由であって値ではない）
    {
        var sim = new Simulation(Fisc(12345, 0.05), new NullAgent());
        // 自動国債発行は廃止（2026-06-22）＝借入はボタンのみ。低税で連合を賄えない国は借金ボタンを押して凌ぐ（AI/プレイヤーと同ルール）→債務累積→緊縮が発火。
        double maxAusterity = 0, maxPollTax = 0, maxMonopolyBreadth = 0;
        bool floorSeen = false;
        for (int q = 0; q < 400; q++)
        {
            if (sim.World.Treasury < sim.World.LastUpkeep + sim.World.LastInterest && sim.World.BorrowStacks < sim.World.Coef.MaxBorrowStacks)
                sim.World.RequestBorrow();
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
        return w;
    }

    [Fact]
    public void 国債台帳_貯蓄の多い資本家は多く買い往復で富が移らない()
    {
        var t = QuietBritain(); var b = QuietBritain();   // 双子（b だけ起債する）
        Assert.Equal(0.0, b.GovDebt);
        t.Coef.BondRepayFrac = 0; b.Coef.BondRepayFrac = 0;   // 1か月目は買うだけ
        b.RequestBorrow();
        Fiscal.Step(t, t.Coef); Fiscal.Step(b, b.Coef);
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
        Fiscal.Step(t, t.Coef); Fiscal.Step(b, b.Coef);
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
    public void 国債台帳_利払いと償還は保有比例で買い手でない持ち手も受け取る()
    {
        var t = QuietBritain(); var b = QuietBritain();
        t.Coef.BondRepayFrac = 0; b.Coef.BondRepayFrac = 0;
        b.RequestBorrow();
        Fiscal.Step(t, t.Coef); Fiscal.Step(b, b.Coef);
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
        Fiscal.Step(t, t.Coef); Fiscal.Step(b, b.Coef);
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
        w.RequestBorrow();
        Fiscal.Step(w, w.Coef);
        double H = HeldOf(w);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        double treasury0 = w.Treasury, base0 = w.BaseMoney;
        w.BorrowStacks = w.Coef.MaxBorrowStacks;             // 借入上限に張り付き
        w.InsolvencyQuarters = w.Coef.DefaultTriggerQuarters; // 返せない期間が尽きた＝この月にデフォルト
        Fiscal.Step(w, w.Coef);
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
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFrac = 0; w.RequestBorrow(); Fiscal.Step(w, w.Coef); }
        double H = HeldOf(y);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        var ypops = AllPopsOf(y); var xpops = AllPopsOf(x);
        int j = ypops.FindIndex(p => !p.IsSoldier && !CapitalMarket.IsInvestor(p, y.Coef));
        foreach (var p in ypops) p.BondHoldings = 0;
        ypops[j].BondHoldings = H;
        foreach (var p in AllPopsOf(z)) p.BondHoldings = 0;
        double repayFrac = new Coefficients().BondRepayFrac;
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFrac = repayFrac; Fiscal.Step(w, w.Coef); }
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

    [Fact]
    public void 国債台帳_階級移動と州間移動と長い走で不変条件が保たれる()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        // 4四半期ごとに起債を積んで40四半期＝転落・昇格（Meritocracy）・徴兵と復員（Military）・州内/州間の移住（PopDecision/Territory）の全経路を
        // 持ち手のいる世界で回す（エンジンの月次検査に BondLedger が入っている＝運び忘れがあれば例外で落ちる）
        for (int q = 0; q < 40; q++) { if (q % 4 == 0) sim.World.RequestBorrow(); sim.StepQuarter(); }
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
        sim.MoveMigrants(fr, br, frR, br.Regions[0], 1.0);
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
        pw.RequestBorrow();
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
        war.Score = -0.20;
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
        rw.RequestBorrow();
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
}
