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
    public void 低税では緊縮が発火し逆進課税の下限の門になる()   // §6.2（赤字→借金ボタン→債務→緊縮：債権者が奪うのは法を選ぶ自由であって値ではない）
    {
        var sim = new Simulation(Fisc(12345, 0.05), new NullAgent());
        // 自動国債発行は廃止（2026-06-22）＝借入はボタンのみ。低税で連合を賄えない国は借金ボタンを押して凌ぐ（AI/プレイヤーと同ルール）→債務累積→緊縮が発火。
        double maxAusterity = 0, maxPollTax = 0, maxMonopolyBreadth = 0;
        bool floorSeen = false;
        for (int q = 0; q < 400; q++)
        {
            if (sim.World.FiscalFundingRatio < 1 && sim.World.BorrowStacks < sim.World.Coef.MaxBorrowStacks && sim.World.BorrowCooldown <= 0)   // AI・プレイヤーと同じ1行（2026-09-28）
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
    // ★借入は国家の支払い率 r＜1 の月だけ・減債は r＝1 の月の余剰（spare）から（作者裁定 F3・2026-09-28 段2-1）。月初に r を決める Simulation を通らない
    //   単体の Fiscal.Step で盤面を作る番人は、起債の月は r＜1（Short）、減債の月は r＝1 と余剰を渡して踏む。
    private const double Short = 0.5, Ample = 1e18;
    private static void FiscalMonth(World w, double r, double spare = 0) { w.FiscalFundingRatio = r; Fiscal.Step(w, w.Coef, spare); }
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
    public void 国債台帳_利払いと償還は保有比例で買い手でない持ち手も受け取る()
    {
        var t = QuietBritain(); var b = QuietBritain();
        t.Coef.BondRepayFrac = 0; b.Coef.BondRepayFrac = 0;
        b.RequestBorrow();
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
        w.RequestBorrow();
        FiscalMonth(w, Short);
        double H = HeldOf(w);
        Assert.True(H > 0, "前提が崩れた：持ち手がいない");
        double treasury0 = w.Treasury, base0 = w.BaseMoney;
        w.BorrowStacks = w.Coef.MaxBorrowStacks;             // 借入上限に張り付き
        w.InsolvencyQuarters = w.Coef.DefaultTriggerQuarters; // 返せない期間が尽きた＝この月にデフォルト
        FiscalMonth(w, 1);
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
        foreach (var w in new[] { x, y, z }) { w.Coef.BondRepayFrac = 0; w.RequestBorrow(); FiscalMonth(w, Short); }
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
        for (int q = 0; q < 40; q++) { if (q % 4 == 0) { sim.World.RequestBorrow(); FiscalMonth(sim.World, Short); } sim.StepQuarter(); }   // 起債は r＜1 の月（2026-09-28）
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
        pw.RequestBorrow(); FiscalMonth(pw, Short);   // 起債は r＜1 の月（2026-09-28）
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
        rw.RequestBorrow(); FiscalMonth(rw, Short);   // 起債は r＜1 の月（2026-09-28）
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
        rw.RequestBorrow(); FiscalMonth(rw, Short);   // 起債は r＜1 の月（2026-09-28）
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
