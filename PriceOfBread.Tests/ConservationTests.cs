using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★保存量の番人（2026-09-03・キューD-36／出典 トリアージ 2-D 10-1e）。
///
/// ⚠**きっかけ**：保存量プローブ（`Simulation.ConservationProbe`＝leakhunt）は**Runner のコマンドでしか有効にならない**＝
/// フルスイートでは一度も走っていなかった。⚠貨幣の蒸発は「破綻」ではなく**静かに数字が痩せる**ので、
/// テストが緑でも起きる（実例＝汚職 Distribute の無言リターンで seed25/27 が落ちた 2026-07）。
///
/// ★D-36 の元の案は「leakhunt/savetest をバッチのゲートへ」だったが、savetest 側は既に A-06 で
/// **2段構え**（短距離を xUnit に常設・長距離300Qは受け入れで手動）に決着している。⇒ 保存量も同じ形に揃える。
///
/// ⚠**静的フラグ（ConservationProbe）は使わない**：xUnit はテストクラスを並列に走らせるので、
/// 静的フラグを立てると**他のテストの世界まで数え込んで偽陽性になる**。
/// ★代わりに**このテストが自分で不変量を計算する**＝並列安全・新しい状態を1つも増やさない。
/// ⚠プローブの細かい粒度（サブステップ単位で現場を特定する）は**診断の道具**であって検出には要らない
/// ＝四半期境界で D が動けばどこかで漏れている。現場の特定は leakhunt を手で走らせる。</summary>
public class ConservationTests
{
    /// <summary>保存量 D ＝ 基礎貨幣 − 累積台帳（貿易/鋳造/不換発行）。
    /// ★内部でいくら金が動いても、台帳に記帳されている限り D は動かない。動いたら**記帳漏れ**である。</summary>
    private static double D(World w)
        => w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;

    [Fact]
    public void 多国の走行で保存量Dが動かない()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StartWarmup();

        int n0 = sim.Nations.Count;
        var baseline = new double[n0];
        var money0 = new double[n0];
        for (int i = 0; i < n0; i++) { baseline[i] = D(sim.Nations[i].World); money0[i] = sim.Nations[i].World.BaseMoney; }
        int moved = 0;   // ★下の生存確認用（この番人が眠っていないことを同じ走行で確かめる）

        const int Q = 12;
        for (int q = 0; q < Q; q++)
        {
            sim.StepQuarter();
            // ⚠国は増減しうる（建国・再統合）ので**開幕から居る国だけ**を見る＝新しい国の初期Dを違反と誤判定しない。
            //   参照同一性でなく添字で見るのは、Nations の並びが開幕の n0 件について安定だから（分離独立は末尾に足される）。
            int upto = System.Math.Min(n0, sim.Nations.Count);
            for (int i = 0; i < upto; i++)
            {
                var w = sim.Nations[i].World;
                double d = D(w);
                // ⚠割譲・併合・分離は**国をまたいで**貨幣を動かすので、当事国の D は正当に動く。
                //   ここは短距離（12四半期）＝領土移動がまだ起きない帯で見る＝偽陽性を作らずに記帳漏れだけを捕まえる。
                Assert.True(System.Math.Abs(d - baseline[i]) <= 1e-4,
                    $"{w.SelfPower?.Name}：保存量Dが動いた（{baseline[i]:F6} → {d:F6}／Δ={d - baseline[i]:F6}）"
                    + "＝どこかで台帳に記帳せずに貨幣を作った/消した。現場の特定は `leakhunt <seed> <四半期>` を手で走らせること。");
                baseline[i] = d;   // 次の四半期は今回値と比べる（累積でなく差分で見る＝1回の跳びを最初の四半期で捕まえる）
                if (System.Math.Abs(w.BaseMoney - money0[i]) > 1e-4) moved++;
            }
        }
        // ★J-011（2026-09-03）：**輸出利潤経路が動いていること**もここで見る＝旧 `ExportRevenueTests` が
        //   単一国の世界市場（`Trade.cs`）で見ていた検査を、**実ゲームが通る二国間経路**へ移した。
        //   ⚠2026-09-29（段3-1）から輸出代金は全額が清算所（世界の外）から④の持分（持ち主・国庫・労働）へ（InternationalMarket.PaySales・旧＝係数 ExportRevenueToProducers の半分だけ持ち主へ直接）
        //   ＝**貨幣が国境をまたいで POP へ直接動く**経路。上のD不変が壊れやすい場所そのもの。
        double exported = 0;
        foreach (var n in sim.Nations)
            if (n.World.ExportsByGood is { } eg) foreach (var v in eg) if (v > 0) exported += v;
        Assert.True(exported > 0, "この走行では輸出が1件も起きていない＝輸出利潤経路が検証できていない（テストの前提が崩れている）");

        // ★生存確認（⚠恒真な番人を作らないための一行）：上の検査は「Dが動かない」なので、
        //   **貨幣がそもそも1円も動かない世界**でも緑になる。基礎貨幣が実際に動いていることを同じ走行で確かめる
        //   ＝Dが不変なのは「何も起きていないから」ではなく「台帳が閉じているから」だ、と言えるようにする。
        Assert.True(moved > 0,
            "この走行では基礎貨幣が一度も動いていない＝保存の検査が**何も検査していない**。"
            + "経済が止まっているか、テストの前提（助走後に12四半期回す）が壊れている。");
    }

    // ================================================================================================
    // ★世界の合計の番人（fis-21・2026-09-26・回0-4）。
    // ⚠国ごとの番人（上の D 不変・Invariants の貨幣保存/人口保存）は**片側記帳**を拾えない：
    //   A国が「外へ払った」と記帳して払った金が誰にも届かない（B国も記帳しない）と、A も B も自分の台帳は閉じている。
    //   世界全体の合計だけがそれを見つける。⇒ 世界の総人口と世界の基礎貨幣を、**正当な湧き口と消え口だけ**で説明できるかを見る。
    //   人口：Σ現存国の全州（分離した州を含む）の人口 −（出生 − 戦争以外の死亡 − 戦死）の累積（一度でも居た国の全部）＝一定。
    //   貨幣：Σ現存国の基礎貨幣 −（鋳造 + 不換発行）の累積（一度でも居た国の全部）＝一定。
    // ★回1-2（2026-09-27・国際移民を閉じる）：旧＝国際移民の世界プール（WorldMigrationPoolFrac＝ゲーム外から人と持参金が湧く既知の湧き口）を
    //   この番人の世界では係数で0にして合わせていた＝プールそのものを削除したので、既定の世界のまま締める（差し引きなし）。
    // ★人口には国を持たない勢力（抽象標的）の植民地の住民も数える（回1-2・作者裁定 2026-09-26「住民が消えるのは好ましくない」）
    //   ＝奪われて州になる（PromoteColonyToRegion）のも、J-006 の吸収で同名の州へ運ばれるのも、世界の中の移転。
    //   ⚠植民地（Colony オブジェクト）の蓄えは貨幣の台帳の外のまま（抽象標的の植民地は蓄えを積まない＝Colonial.Step は国の植民地だけ）。
    // ★状態は1つも増やさない（出生と死亡の累積 World.DiagBirths/DiagDeaths は popprobe の計器と共用の純記帳）。
    // ⚠静的フラグ（ConservationProbe）は使わない（上と同じ理由＝並列のテストを数え込む）＝このテストが自分で合計を取る。

    private sealed class WorldLedgerRun
    {
        public readonly List<string> PopBreaks = [], MoneyBreaks = [], DebtBreaks = [];
        public double PopMoved, MoneyMoved, PopUnexplained, MoneyUnexplained, DebtMax;
        public const int ShortQuarters = 40;   // 常設の長さ（約40秒）。全長（668四半期・約7分）は下の番人＝保存則に触る回でだけ回す（Category=FullLength）
        public readonly int Quarters;

        public WorldLedgerRun(int quarters)
        {
            Quarters = quarters;
            var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());   // ★既定の世界（回1-2 で世界プールの差し引きを外した）
            foreach (var n in sim.Nations) n.TelemetryEnabled = false;
            sim.Diplomacy.Log.Enabled = false;
            sim.StartWarmup();
            var seen = new List<Simulation>(sim.Nations);   // 一度でも居た国（再統合で消えた国の出生・死亡・鋳造の累積も数え続ける）
            var seenSet = new HashSet<Simulation>(sim.Nations);
            var t0 = Totals();
            double gPop = t0.Pop - t0.Created, gMoney = t0.Money - t0.Minted;
            for (int q = 0; q < Quarters; q++)
            {
                var before = new Dictionary<Simulation, (double Pop, double Created, double Money, double Minted)>();
                foreach (var n in seen) before[n] = NationTotals(n);
                sim.StepQuarter();
                foreach (var n in sim.Nations) if (seenSet.Add(n)) seen.Add(n);
                var t = Totals();
                double nPop = t.Pop - t.Created, nMoney = t.Money - t.Minted;
                if (System.Math.Abs(nPop - gPop) > 1e-6 * System.Math.Max(1.0, t.Pop))
                {
                    PopBreaks.Add($"Q{q + 1}（{sim.Year}年）：出生・死亡・戦死で説明できない世界の総人口の増減 {nPop - gPop:+0.0;-0.0}（総人口 {t.Pop:F0}）"
                                  + Culprits(before, x => x.Pop - x.Created));
                    PopUnexplained += nPop - gPop;
                }
                if (System.Math.Abs(nMoney - gMoney) > 1e-6 * System.Math.Max(1.0, System.Math.Abs(t.Money)))
                {
                    MoneyBreaks.Add($"Q{q + 1}（{sim.Year}年）：鋳造・不換発行で説明できない世界の基礎貨幣の増減 {nMoney - gMoney:+0.0;-0.0}（世界の基礎貨幣 {t.Money:F0}）"
                                    + Culprits(before, x => x.Money - x.Minted));
                    MoneyUnexplained += nMoney - gMoney;
                }
                PopMoved += System.Math.Abs(t.Pop - t0.Pop); MoneyMoved += System.Math.Abs(t.Money - t0.Money);
                gPop = nPop; gMoney = nMoney;   // 差分で見る＝1回の跳びを最初の四半期で捕まえる（累積させない）
                // ★債務は債務者について行く（2026-09-29 S3b＝輸入の為替手形）：現存国ごとに 家計の与信＝その国の POP の債務の和（割譲・建国・再併合・越境移民の後も）
                foreach (var n in sim.Nations)
                {
                    var w = n.World; double debt = 0;
                    foreach (var r in w.Regions) foreach (var p in r.Pops) debt += p.Debt;
                    double hh = w.Bank!.HouseholdLoans;
                    if (hh > DebtMax) DebtMax = hh;
                    if (System.Math.Abs(hh - debt) > 1e-6 * System.Math.Max(1.0, System.Math.Abs(hh) + debt))
                        DebtBreaks.Add($"Q{q + 1}（{sim.Year}年）：{w.SelfPower?.Name}：家計の与信 {hh:F3} ≠ POP の債務の和 {debt:F3}");
                }
            }

            (double Pop, double Created, double Money, double Minted) Totals()
            {
                double pop = 0, money = 0, created = 0, minted = 0;
                foreach (var n in seen)
                {
                    var (p, c, m, mi) = NationTotals(n);
                    pop += p; created += c; money += m; minted += mi;
                }
                foreach (var ap in sim.Diplomacy.Powers)   // 抽象標的（Nation の無い勢力）の植民地の住民
                    if (ap.Nation is null) foreach (var col in ap.ColonyList) pop += System.Math.Max(0, col.PopSize) + col.SettlerSize;
                minted += sim.Diplomacy.CumulativeFxResidual;   // ★段3-1（A7）：為替の換算で世界の名目の基礎貨幣が動く分（清算所の台帳）＝鋳造・不換発行と並ぶ正当な湧き口と消え口
                return (pop, created, money, minted);
            }
            // 国ごと：人口と基礎貨幣は現存国だけ（消えた国の州と金は親へ移っている）／出生・死亡・戦死・鋳造・不換発行の累積は一度でも居た国の全部
            (double Pop, double Created, double Money, double Minted) NationTotals(Simulation n)
            {
                var w = n.World;
                bool alive = sim.Nations.Contains(n);
                double p = 0;
                if (alive) foreach (var r in w.Regions) p += r.SumPopulation();   // ⚠分離した州（Seceded）も数える＝分離で人は消えない
                return (p, w.DiagBirths - w.DiagDeaths - w.WarDeadCumulative, alive ? w.BaseMoney : 0, w.CumulativeGoldMinted + w.CumulativeFiatIssued);
            }
            // 現場の手掛かり＝国ごとの「説明できない増減」の上位3（国をまたぐ移転は両側に出る＝片側だけ大きい国が現場）
            string Culprits(Dictionary<Simulation, (double Pop, double Created, double Money, double Minted)> b,
                            System.Func<(double Pop, double Created, double Money, double Minted), double> g)
            {
                var d = new List<(string Name, double Delta)>();
                foreach (var n in seen)
                {
                    double v = g(NationTotals(n)) - (b.TryGetValue(n, out var bb) ? g(bb) : 0);
                    if (System.Math.Abs(v) > 1e-9) d.Add((n.World.SelfPower?.Name ?? n.World.Region.Name, v));
                }
                d.Sort((x, y) => System.Math.Abs(y.Delta).CompareTo(System.Math.Abs(x.Delta)));
                var top = new System.Text.StringBuilder("　国ごとの内訳の上位：");
                for (int i = 0; i < System.Math.Min(3, d.Count); i++) top.Append($"{d[i].Name} {d[i].Delta:+0.0;-0.0}　");
                return top.ToString();
            }
        }
    }
    private static readonly System.Lazy<WorldLedgerRun> WorldRun = new(() => new WorldLedgerRun(WorldLedgerRun.ShortQuarters));
    private static readonly System.Lazy<WorldLedgerRun> WorldRunFull = new(() => new WorldLedgerRun(Coefficients.GameQuarters));

    /// <summary>★家計の与信は国ごとに POP の債務の和（2026-09-29 S3b）＝輸入の為替手形の債務の記録（Pop.Debt）と貨幣（Bank.HouseholdLoans）は
    /// 同じ額で書き（Bank.Lend の1本）、POP が国をまたぐと与信も受け入れ国の銀行へ移る（Bank.Carry）。⚠突然変異「割譲で与信を移さない」で赤（全長）。</summary>
    [Fact]
    public void 家計の与信は国ごとにPOPの債務の和に等しい()
    {
        var run = WorldRun.Value;
        Assert.True(run.DebtMax > 0, "手形が一度も振り出されていない＝番人が何も検査していない");
        Assert.True(run.DebtBreaks.Count == 0, $"{run.DebtBreaks.Count} 件：\n" + string.Join("\n", run.DebtBreaks.Take(8)));
    }

    [Fact]
    [Trait("Category", "FullLength")]
    public void 家計の与信は全長でも国ごとにPOPの債務の和に等しい()
    {
        var run = WorldRunFull.Value;
        Assert.True(run.DebtMax > 0, "手形が一度も振り出されていない＝番人が何も検査していない");
        Assert.True(run.DebtBreaks.Count == 0, $"{run.DebtBreaks.Count} 件：\n" + string.Join("\n", run.DebtBreaks.Take(8)));
    }

    /// <summary>★輸送の容量は州と一緒に移る（2026-09-30 (118)）：割譲（Territory.TransferRegion）で、出ていく州の取り分（清算と同じ規則 Transport.RegionShare）の
    /// 鉄道と船が受け手へ移る＝両国の和は変わらない・送り手の残る州の容量は前と同じ（潜在量比例）・汽船の割合は容量で加重して混ざる。
    /// 建国（SpawnSecededNations）の新しい国は移った州の容量を持って生まれる（旧＝船腹0・鉄道0）。⚠突然変異「移さない」「建国で受け取らない」「汽船の割合を混ぜない」で赤。</summary>
    [Fact]
    public void 輸送の容量は割譲と建国で州と一緒に移る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ShipCapacity = 1000; uk.RailCapacity = 400; uk.ShipSteamShareEma = 0.5;
        fr.ShipCapacity = 200; fr.RailCapacity = 0; fr.ShipSteamShareEma = 0.0;
        int k = 0; foreach (var q in uk.Regions) q.LastTradePotential = ++k;   // 州ごとに違う潜在量（清算の配分の入力）
        var r = uk.Regions.Find(x => x.Id != 0 && x.State == RegionState.Active)!;
        var b = Transport.ShareBasis(uk, uk.Coef);
        double sh = Transport.RegionShare(b, r);
        Assert.True(sh > 0 && sh < 1, $"前提が崩れた：取り分が 0..1 の内側でない（{sh}）");
        var rest = new System.Collections.Generic.Dictionary<Region, double>();
        foreach (var q in uk.Regions) if (q != r && q.State == RegionState.Active) rest[q] = uk.ShipCapacity * Transport.RegionShare(b, q);
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        Assert.Equal(1200.0, uk.ShipCapacity + fr.ShipCapacity, 9);
        Assert.Equal(400.0, uk.RailCapacity + fr.RailCapacity, 9);
        Assert.Equal(1000.0 * (1 - sh), uk.ShipCapacity, 9);
        Assert.Equal(400.0 * sh, fr.RailCapacity, 9);
        Assert.Equal(0.5 * 1000.0 * sh / (200.0 + 1000.0 * sh), fr.ShipSteamShareEma, 12);
        var b2 = Transport.ShareBasis(uk, uk.Coef);
        foreach (var (q, before) in rest) Assert.Equal(before, uk.ShipCapacity * Transport.RegionShare(b2, q), 9);   // 残る州の容量は前と同じ

        // 建国：新しい国は出ていく州の取り分の容量を持って生まれる
        double ship0 = uk.ShipCapacity, rail0 = uk.RailCapacity;
        var sec = uk.Regions.Find(x => x.Id != 0 && x.State == RegionState.Active && x != r)!;
        double sh2 = Transport.RegionShare(Transport.ShareBasis(uk, uk.Coef), sec);
        int n0 = sim.Nations.Count;
        uk.PendingSecessionWars = [sec];
        sim.SpawnSecededNations();
        Assert.Equal(n0 + 1, sim.Nations.Count);
        var nw = sim.Nations[^1].World;
        Assert.Contains(sec, nw.Regions);
        Assert.Equal(ship0 * sh2, nw.ShipCapacity, 9);
        Assert.Equal(rail0 * sh2, nw.RailCapacity, 9);
        Assert.Equal(ship0, uk.ShipCapacity + nw.ShipCapacity, 9);
        Assert.Equal(0.5, nw.ShipSteamShareEma, 12);
    }

    /// <summary>★債務は債務者について行く（2026-09-29 S3b）：州の割譲（Territory.TransferRegion）と越境移民（MoveMigrants）で、POP の債務の分の
    /// 家計の与信が受け入れ国の銀行へ移り、運ぶ基礎貨幣＝貯蓄−債務＝両国の D が閉じる。⚠突然変異「与信を移さない」で赤。</summary>
    [Fact]
    public void 債務は割譲と越境移民で債務者について行く()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        var r = uk.Regions[1];
        Assert.True(r.MerchantPopIndex >= 0, "前提が崩れた：割譲する州に商人が居ない");
        var mer = r.Pops[r.MerchantPopIndex];
        uk.Bank!.Lend(mer, 50);   // 手形の債務（貸しの1本）
        double dUk = D(uk), dFr = D(fr), hUk = uk.Bank.HouseholdLoans, hFr = fr.Bank!.HouseholdLoans;
        double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(uk.BaseMoney) + System.Math.Abs(fr.BaseMoney));
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        Assert.Equal(hUk - 50, uk.Bank.HouseholdLoans, 9); Assert.Equal(hFr + 50, fr.Bank.HouseholdLoans, 9);
        Assert.True(System.Math.Abs(D(uk) - dUk) <= tol && System.Math.Abs(D(fr) - dFr) <= tol, "割譲で国ごとの D が閉じない");
        var sp = uk.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        var dp = fr.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        uk.Bank.Lend(sp, 30);
        double flow = 0.1 * sp.Size, carried = sp.Debt * (flow / sp.Size);
        dUk = D(uk); dFr = D(fr); hUk = uk.Bank.HouseholdLoans; hFr = fr.Bank.HouseholdLoans;
        sim.MoveMigrants(uk, fr, uk.Regions[0], fr.Regions[0], PopClass.Worker, flow);
        Assert.True(carried > 0 && System.Math.Abs(uk.Bank.HouseholdLoans - (hUk - carried)) <= 1e-9 && System.Math.Abs(fr.Bank.HouseholdLoans - (hFr + carried)) <= 1e-9, "越境移民で家計の与信が債務について行かない");
        Assert.True(System.Math.Abs(D(uk) - dUk) <= tol && System.Math.Abs(D(fr) - dFr) <= tol, "越境移民で国ごとの D が閉じない");
    }

    [Fact]
    public void 世界の総人口は出生と死亡と戦死でしか動かない()
    {
        var run = WorldRun.Value;
        Assert.True(run.PopMoved > 0, "世界の総人口が一度も動いていない＝番人が何も検査していない");
        Assert.True(run.PopBreaks.Count == 0,
            $"{run.PopBreaks.Count}/{run.Quarters} 四半期で片側記帳（人が湧いた/消えた・累計 {run.PopUnexplained:+0.0;-0.0}）：\n" + string.Join("\n", run.PopBreaks.Take(8)));
    }

    [Fact]
    public void 世界の基礎貨幣は鋳造と不換発行でしか動かない()
    {
        var run = WorldRun.Value;
        Assert.True(run.MoneyMoved > 0, "世界の基礎貨幣が一度も動いていない＝番人が何も検査していない");
        Assert.True(run.MoneyBreaks.Count == 0,
            $"{run.MoneyBreaks.Count}/{run.Quarters} 四半期で片側記帳（金が湧いた/消えた・累計 {run.MoneyUnexplained:+0.0;-0.0}）：\n" + string.Join("\n", run.MoneyBreaks.Take(8)));
    }

    /// <summary>★全長（668四半期）の世界の総人口（回1-2・2026-09-27）。旧＝全長では人口を見ていなかった（抽象標的の植民地を奪うと住民が世界の外から湧き、
    /// J-006 の吸収では住民が消えた＝seed1789 で21四半期・計 +6,850）。抽象標的の植民地の住民を世界の合計に含め、吸収で住民を同名の州へ運んで閉じた。
    /// 走行は下の貨幣の番人と共有（同じ1本の全長）。印 Category=FullLength（保存則に触る回でだけ回す）。</summary>
    [Fact]
    [Trait("Category", "FullLength")]
    public void 世界の総人口は全長でも出生と死亡と戦死でしか動かない()
    {
        var run = WorldRunFull.Value;
        Assert.True(run.PopMoved > 0, "世界の総人口が一度も動いていない＝番人が何も検査していない");
        Assert.True(run.PopBreaks.Count == 0,
            $"{run.PopBreaks.Count}/{run.Quarters} 四半期で片側記帳（人が湧いた/消えた・累計 {run.PopUnexplained:+0.0;-0.0}）：\n" + string.Join("\n", run.PopBreaks.Take(8)));
    }

    /// <summary>★全長（668四半期）の世界の合計（2026-09-26 回0-4 で**漏れを見つけた**番人・約7分）。
    /// 見つけた漏れ＝講和の賠償の片側記帳（勝者が抽象標的＝Nation の無い勢力のとき、払った国だけが流出を記帳し金が世界から消えた＝
    /// seed1789 の Q317（1862年 カザフ草原←ポーランド −1009.0）・Q598（1932年 アチェ←マイソール −888.4）。逆向きの AbstractTargetReparations も同じ形）。
    /// ★回1-1（2026-09-26）で閉じた＝賠償は貨幣経済を持つ2国の間だけで動く（Diplomacy.ResolvePeace）→ Skip を外した。
    /// ★作者裁定（2026-09-26）：普段のフルスイートからは外し、**保存則に触る回でだけ回す**＝印 Category=FullLength
    /// （普段＝`--filter "Category!=FullLength"`・保存則の回＝`--filter "Category=FullLength"`・回し方は README の計測の節）。</summary>
    [Fact]
    [Trait("Category", "FullLength")]
    public void 世界の基礎貨幣は全長でも鋳造と不換発行でしか動かない()
    {
        var run = WorldRunFull.Value;
        Assert.True(run.MoneyMoved > 0, "世界の基礎貨幣が一度も動いていない＝番人が何も検査していない");
        Assert.True(run.MoneyBreaks.Count == 0,
            $"{run.MoneyBreaks.Count}/{run.Quarters} 四半期で片側記帳（金が湧いた/消えた・累計 {run.MoneyUnexplained:+0.0;-0.0}）：\n" + string.Join("\n", run.MoneyBreaks.Take(8)));
    }

    // ================================================================================================
    // ★国家間移民の番人（回1-2・2026-09-27＝国際移民を閉じる・外部レビューの検討 §10-3 Q3／項目3 の4）。
    //   段を単離して1回だけ呼ぶ（MultiNationSim.MigrateBetweenNations は internal）。6か国（英・仏・米・普・露・西）＝行き先が複数ある盤面。
    //   外交は止める（戦争で州が動くと移民の前後が比べられない）。1四半期だけ回して州の食料充足と機会を実体化してから測る。

    private static MultiNationSim MigrationBoard(Coefficients? coef = null)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("アメリカ"),
                                      WorldData.ByName("プロイセン"), WorldData.ByName("ロシア"), WorldData.ByName("スペイン")], seed: 1789, coef: coef) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        return sim;
    }

    private static (double Pop, double Money) MigrationWorldTotals(MultiNationSim sim)
    {
        double pop = 0, money = 0;
        foreach (var n in sim.Nations) { foreach (var r in n.World.Regions) pop += r.SumPopulation(); money += n.World.BaseMoney; }
        return (pop, money);
    }

    [Fact]
    public void 国家間移民の段の前後で世界の総人口と基礎貨幣は変わらない()
    {
        var sim = MigrationBoard();
        var (p0, m0) = MigrationWorldTotals(sim);
        sim.MigrateBetweenNations();
        var (p1, m1) = MigrationWorldTotals(sim);
        double moved = 0; foreach (var n in sim.Nations) moved += n.World.LastEmigration;
        Assert.True(moved > 0, "この盤面で1人も移っていない＝番人が何も検査していない");
        Assert.True(System.Math.Abs(p1 - p0) <= 1e-9 * p0, $"移民の段で世界の総人口が動いた（{p1 - p0:+0.000000;-0.000000}・移動 {moved:F1}）＝人が湧いた/消えた");
        Assert.True(System.Math.Abs(m1 - m0) <= 1e-9 * System.Math.Abs(m0), $"移民の段で世界の基礎貨幣が動いた（{m1 - m0:+0.000000;-0.000000}）＝持参金が湧いた/消えた");
    }

    /// <summary>流出の上限は送出州・階級ごとに1回だけ＝行き先がいくつあっても EmigrationMaxFrac×(1＋飢え)×人数 を超えない。
    /// 受入の上限は外して（上限を巨大・受入能力の指数0）送出側の上限だけを見る。流出率を大きくして、正の差のある州は全て上限に張り付く盤面にする
    /// （1783年の既定では差が小さく上限に届く州が無い＝上限の掛け方を検査できない）。⚠突然変異「上限を行き先ごとに掛ける」で赤になる。</summary>
    [Fact]
    public void 国家間移民の流出は行き先の数によらず上限を1回だけ()
    {
        var sim = MigrationBoard(new Coefficients { ImmigrationReceiveCapFrac = 1e9, ImmigrationAbsorbExponent = 0, EmigrationRate = 1.0 });
        var c = sim.Coef;
        var bound = new List<(Pop Pop, double Size0, double Cap)>();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double effFill = w.FoodFill + c.FaminePushRegionBlend * (r.FoodFill - w.FoodFill);
                double maxFrac = c.EmigrationMaxFrac * (1.0 + c.FaminePushMaxFracBoost * System.Math.Max(0.0, 1.0 - effFill));
                foreach (var p in r.Pops)
                    if (!p.IsSoldier && (p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker) && p.Size > 1e-9)
                        bound.Add((p, p.Size, maxFrac * p.Size));
            }
        }
        sim.MigrateBetweenNations();
        double worst = 0;
        foreach (var (p, s0, cap) in bound)
        {
            double outflow = s0 - p.Size;   // 受入州の POP は流入で相殺される＝差し引きの流出は上限以下のまま
            Assert.True(outflow <= cap * (1 + 1e-9) + 1e-12, $"1つの POP から上限を超えて流出した（{outflow:F4} > 上限 {cap:F4}・人数 {s0:F1}）＝上限が行き先ごとに掛かっている");
            if (cap > 0) worst = System.Math.Max(worst, outflow / cap);
        }
        Assert.True(worst > 0.99, $"上限に届いた POP が1つも無い（最大 {worst:P1}）＝上限の番人が何も検査していない");
    }

    /// <summary>受入国の流入の合計は上限 C_d（受入の上限率×人口×移民制限法×受入能力≦1）以下・閉鎖の国は同じ条件の開放の国の2割以下。
    /// 上限率を小さくして両方で上限が効く盤面にする（効いていない盤面では「2割以下」は言えない）。
    /// 受入国＝盤面の6か国のうち開放で最も多く受け入れる国（どの国が引き寄せるかは1四半期の軌道で決まる＝創発。番人は規則だけを見る・2026-09-27 (73)
    /// 旧＝米に固定。自己費用の単位の是正で1四半期目の州の費用が動き、米の機会が他国を下回って番人が空振りした）。</summary>
    [Fact]
    public void 国家間移民の受入は上限以下_閉鎖は開放の2割以下()
    {
        double Inflow(string name, ImmigrationLaw law, out double capNoAbsorb)
        {
            var sim = MigrationBoard(new Coefficients { ImmigrationReceiveCapFrac = 1e-5 });
            var dst = sim.Nations.First(n => n.World.SelfPower?.BaseName == name).World;
            dst.ImmigrationRestriction = law;
            capNoAbsorb = sim.Coef.ImmigrationReceiveCapFrac * dst.TotalPopulation * (law == ImmigrationLaw.Closed ? sim.Coef.ImmigrationClosedFrac : 1.0);
            sim.MigrateBetweenNations();
            return dst.LastImmigration;
        }
        string[] board = ["イギリス", "フランス", "アメリカ", "プロイセン", "ロシア", "スペイン"];   // MigrationBoard と同じ6か国
        string name = board.OrderByDescending(n => Inflow(n, ImmigrationLaw.Open, out _)).First();
        double open = Inflow(name, ImmigrationLaw.Open, out double capOpen);
        double closed = Inflow(name, ImmigrationLaw.Closed, out double capClosed);
        Assert.True(open > 0, $"開放の{name}（最も受け入れる国）に1人も来ていない＝番人が何も検査していない");
        Assert.True(open <= capOpen * (1 + 1e-9), $"受入の上限を超えた（{open:F4} > {capOpen:F4}）");
        Assert.True(closed <= capClosed * (1 + 1e-9), $"閉鎖の受入の上限を超えた（{closed:F4} > {capClosed:F4}）");
        Assert.True(closed <= 0.2 * open * (1 + 1e-9), $"閉鎖（{closed:F4}）が開放（{open:F4}）の2割を超えた");
    }

    [Fact]
    public void 国家間移民は差が0以下の国へは1人も行かない()
    {
        var sim = MigrationBoard(new Coefficients { CrossBorderOppGapThreshold = 1e9 });   // どの組も差＝機会の差−閾値≦0
        var (p0, _) = MigrationWorldTotals(sim);
        var sizes = new List<(Pop Pop, double Size)>();
        foreach (var n in sim.Nations) foreach (var p in n.World.AllPops()) sizes.Add((p, p.Size));
        sim.MigrateBetweenNations();
        foreach (var n in sim.Nations) { Assert.Equal(0.0, n.World.LastImmigration); Assert.Equal(0.0, n.World.LastEmigration); }
        foreach (var (p, s) in sizes) Assert.Equal(s, p.Size);
        Assert.Equal(p0, MigrationWorldTotals(sim).Pop);
    }

    /// <summary>持参金＝送出 POP の1人あたり貯蓄×動いた人数（外から湧かない）。受入 POP の貯蓄の増分・送出 POP の減分・両国の台帳の記帳が同じ額。</summary>
    [Fact]
    public void 国家間移民の持参金は送出POPの1人あたり貯蓄と人数の積()
    {
        var sim = MigrationBoard();
        var src = sim.Nations[0].World; var dst = sim.Nations[2].World;   // 英→米
        var sp = src.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        var dp = dst.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
        double flow = 0.01 * sp.Size, perHead = sp.Savings / sp.Size;
        double spSav0 = sp.Savings, dpSav0 = dp.Savings, srcFlow0 = src.CumulativeTradeFlow, dstFlow0 = dst.CumulativeTradeFlow;
        Assert.True(perHead > 0, "前提が崩れた：送出 POP に貯蓄が無い");
        sim.MoveMigrants(src, dst, src.Regions[0], dst.Regions[0], PopClass.Worker, flow);
        double dowry = perHead * flow, tol = 1e-9 * System.Math.Max(1.0, dowry);
        Assert.True(System.Math.Abs(dp.Savings - dpSav0 - dowry) <= tol, $"受入 POP の貯蓄の増分 {dp.Savings - dpSav0} ≠ 持参金 {dowry}");
        Assert.True(System.Math.Abs(spSav0 - sp.Savings - dowry) <= tol, $"送出 POP の貯蓄の減分 {spSav0 - sp.Savings} ≠ 持参金 {dowry}");
        Assert.True(System.Math.Abs(dst.CumulativeTradeFlow - dstFlow0 - dowry) <= tol && System.Math.Abs(srcFlow0 - src.CumulativeTradeFlow - dowry) <= tol, "両国の台帳の記帳が持参金と一致しない");
    }

    /// <summary>★J-006 の吸収（回1-2・作者裁定 2026-09-26「住民が消えるのは好ましくない」）：同名の州を既に持つ国へ渡った植民地は、
    /// Colony オブジェクトは消えるが住民（と蓄え）はその州へ入る。統制盤面＝日本（蝦夷地の州を持つ）が、盤面の中で作った同名の抽象標的を無血で侵略（屈服線0）。
    /// ★2026-09-28（作者裁定 E9）：既定の世界から抽象標的「蝦夷地」を消した＝同名の二重登録はデータに無い。規則（同名の州へ住民を運ぶ）は残るので、
    ///   抽象標的は既定の世界に依存せず盤面の中で作る（MultiNationSim の構築と同じ形＝住民は人口の重み×ColonialPopScale）。
    /// ⚠突然変異「吸収で住民を捨てる」で赤になる。</summary>
    [Fact]
    public void J006の吸収で住民は同名の州へ入る()
    {
        var coef = new Coefficients { ColonizeSubmitRatio = 0 };   // 屈服線0＝必ず屈服（戦争にしない）
        var sim = new MultiNationSim([WorldData.ByName("日本")], seed: 1789, coef: coef);
        var jp = sim.Nations[0].World;
        var ezo = new Power { Id = sim.Diplomacy.Powers.Max(p => p.Id) + 1, Name = "蝦夷地", BaseName = "蝦夷地", Military = 0.2, Gdp = 0, TradeWeight = 0.0 };
        var cc = Geo.ColonyCoord("蝦夷地");
        var col = new Colony { Name = "蝦夷地", Resources = [Catalog.FishId, Catalog.TimberId], Population = 0.05, PopSize = 0.05 * coef.ColonialPopScale, Latitude = cc.Lat, Longitude = cc.Lon };
        ezo.ColonyList.Add(col);
        sim.Diplomacy.Powers.Add(ezo);
        var region = jp.Regions.Find(r => r.Name == "蝦夷地")!;
        double people = System.Math.Max(0, col.PopSize) + col.SettlerSize;
        Assert.True(people > 0, "前提が崩れた：抽象標的の蝦夷地に住民がいない");
        double pop0 = region.SumPopulation(), delta0 = jp.PopulationDelta;
        int regions0 = jp.Regions.Count, absorbed0 = sim.Diplomacy.AbsorbedColonies;
        Assert.True(sim.Diplomacy.Subjugate(jp.SelfPower!, ezo, coef));
        Assert.Equal(absorbed0 + 1, sim.Diplomacy.AbsorbedColonies);   // 保存検査の第4の出口（Colony オブジェクトは消える）
        Assert.Empty(ezo.ColonyList);
        Assert.Equal(regions0, jp.Regions.Count);   // 同名の州が在る＝新しい州は作らない
        double tol = 1e-9 * System.Math.Max(1.0, pop0 + people);
        Assert.True(System.Math.Abs(region.SumPopulation() - pop0 - people) <= tol, $"蝦夷地の州の人口 {pop0:F1}→{region.SumPopulation():F1}（住民 {people:F1} が入っていない）");
        Assert.True(System.Math.Abs(jp.PopulationDelta - delta0 - people) <= tol, "人口の台帳（PopulationDelta）に住民が記帳されていない");
    }

    // ── 支払いの一元化 段2-2・2-3 の番人（2026-09-29・計画 docs/202609/20260926/20260926_外部レビューの検討.md §10-12）──

    /// <summary>★按分の本体（World.Distribute）は**符号つき**で**微小な額も配り切る**（段2-3 E1）。旧＝1e-12 以下の額は配らずに0を返した（呼び手は配ったものとして扱い、その分が消えた）・
    /// 負の額（与信の回収・貿易の赤字）は同じ式の手書きの複製が配っていた。額が0か NaN なら何もしない。
    /// ⚠突然変異「旧の門 !(amount > 1e-12)」で赤（微小な額が消え、負の額が配られない）。</summary>
    [Fact]
    public void 按分の本体は符号つきで微小な額も配り切る()
    {
        var a = new Pop { Size = 1, Savings = 0 }; var b = new Pop { Size = 3, Savings = 0 };
        Assert.Equal(0.0, World.Distribute([a, b], 4e-13));
        Assert.True(System.Math.Abs(a.Savings + b.Savings - 4e-13) <= 1e-27 && a.Savings > 0, $"微小な額が配られていない（{a.Savings}＋{b.Savings}）");
        var c = new Pop { Size = 1, Savings = 10 }; var d = new Pop { Size = 1, Savings = 30 };
        Assert.Equal(0.0, World.Distribute([c, d], -8.0, [10.0, 30.0]));   // 負の額＝重みの比で取る（与信の回収・貿易の赤字）
        Assert.Equal(8.0, c.Savings); Assert.Equal(24.0, d.Savings);
        Assert.Equal(0.0, World.Distribute([c, d], double.NaN)); Assert.Equal(8.0, c.Savings);   // NaN は配らない
        Assert.Equal(-5.0, World.Distribute([], -5.0));                                        // 受け手不在＝配れなかった額を返す（符号つき）
    }

    /// <summary>★お金の移し方の1本（World.Transfer・段2-3 E2・A4＝旧 MultiNationSim.TransferToPops を移して一般化）：払い手（POP＝貯蓄の保有の比・貯蓄まで・負の貯蓄からは取らない）から
    /// 受け手へ**動いた額**を両側に書く。両側の国が違えば台帳（CumulativeTradeFlow）は払い手の国で減り受け手の国で同じ額だけ増える。受け手不在（空・重みの合計≈0）なら動かさない。
    /// ⚠突然変異「受け手不在でも取る」「台帳の片側だけ」「払い手の貯蓄で切らない」で赤。</summary>
    [Fact]
    public void 移し方の1本は動いた額を両側に書き受け手不在なら動かさない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        var payers = new List<Pop> { new() { Size = 1, Savings = 70 }, new() { Size = 1, Savings = 30 }, new() { Size = 1, Savings = -5 } };
        double t0 = fr.Treasury, uf0 = uk.CumulativeTradeFlow, ff0 = fr.CumulativeTradeFlow;
        double moved = World.Transfer((uk, payers), (fr, null, null), 250, TreasuryCat.OtherIn);   // 請求（250）は払い手の保有（100）を超える
        Assert.Equal(100.0, moved);
        Assert.Equal(0.0, payers[0].Savings); Assert.Equal(0.0, payers[1].Savings); Assert.Equal(-5.0, payers[2].Savings);
        double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(t0) + System.Math.Abs(uf0) + System.Math.Abs(ff0));
        Assert.True(System.Math.Abs(fr.Treasury - t0 - moved) <= tol, $"受け手の国庫の増分 {fr.Treasury - t0} ≠ 動いた額 {moved}");
        Assert.True(System.Math.Abs(uf0 - uk.CumulativeTradeFlow - moved) <= tol && System.Math.Abs(fr.CumulativeTradeFlow - ff0 - moved) <= tol, "台帳が両側の国で動いた額と一致しない");
        var rich = new List<Pop> { new() { Size = 1, Savings = 50 } };
        Assert.Equal(0.0, World.Transfer((uk, rich), (uk, [], null), 10));                                  // 受け手の集合が空
        Assert.Equal(0.0, World.Transfer((uk, rich), (uk, [new Pop { Size = 0 }], null), 10));              // 受け手の頭数≈0
        Assert.Equal(50.0, rich[0].Savings);                                                               // 払い手に残る
        var poor = new Pop { Size = 2, Savings = 0 };
        double uf1 = uk.CumulativeTradeFlow;
        Assert.Equal(10.0, World.Transfer((uk, rich), (uk, [poor], null), 10));
        Assert.Equal(40.0, rich[0].Savings); Assert.Equal(10.0, poor.Savings);
        Assert.Equal(uf1, uk.CumulativeTradeFlow);                                                         // 同じ国の中＝台帳は動かない
    }

    /// <summary>★移民の運賃（段2-3 C17）：運賃は**持っている正の持参金から**だけ払う＝負の貯蓄（借越）は運賃の有無に関わらず移民に付いて行く。
    /// 旧＝運賃があると max(0, 持参金−運賃) で負の持参金を送出 POP に残し、運賃0では負の持参金も運んだ。⚠突然変異「旧の max(0,…)」で赤。</summary>
    [Fact]
    public void 移民の負の持参金は運賃の有無に関わらず移民に付いて行く()
    {
        foreach (double fare in new[] { 0.0, 0.1 })
        {
            var sim = MigrationBoard(new Coefficients { MigrationFarePerkKm = fare });
            var src = sim.Nations[0].World; var dst = sim.Nations[2].World;   // 英→米（遠い＝運賃が立つ）
            var sp = src.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
            var dp = dst.Regions[0].Pops.Find(p => p.ClassId == PopClass.Worker && !p.IsSoldier)!;
            sp.Savings = -0.5 * sp.Size;   // 借越
            double flow = 0.01 * sp.Size, carried = sp.Savings * (flow / sp.Size), dp0 = dp.Savings, sp0 = sp.Savings;
            sim.MoveMigrants(src, dst, src.Regions[0], dst.Regions[0], PopClass.Worker, flow);
            double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(dp0) + System.Math.Abs(sp0));
            Assert.True(System.Math.Abs(dp.Savings - dp0 - carried) <= tol, $"運賃 {fare}：受入 POP へ負の持参金 {carried} が運ばれていない（増分 {dp.Savings - dp0}）");
            Assert.True(System.Math.Abs(sp.Savings - sp0 + carried) <= tol, $"運賃 {fare}：送出 POP に負の持参金が残った");
        }
    }

    /// <summary>★招聘の支度金（段2-3 C16）：受け手＝呼ばれた本人が入った受入州の POP へ人数の比（研究者の分は研究者へ）。
    /// 旧＝研究者の分も含めて受入州の技師 POP へ全額。盤面＝6か国を1四半期温め、両国の首都州に研究者の POP を置いて（研究者の居る世界＝ResearcherFrac＞0 の形・貯蓄0＝貨幣は動かない）
    /// MoveSpecialists を直接呼ぶ。⚠突然変異「技師へ全額」で赤。</summary>
    [Fact]
    public void 招聘の支度金は呼ばれた本人へ人数の比で渡る()
    {
        var sim = MigrationBoard();
        var src = sim.Nations[0].World; var dst = sim.Nations[1].World;   // 英→仏
        Region sR = src.Regions[0], dR = dst.Regions[0];
        Pop? Of(Region r, int cls) => r.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
        foreach (var r in new[] { sR, dR })
            if (Of(r, PopClass.Researcher) is null) r.Pops.Add(new Pop { ClassId = PopClass.Researcher, ClassName = "研究者", IsUrban = true, Size = 20, Savings = 0, Satisfaction = 1.0, IsModernElite = true });
        Pop? se = Of(sR, PopClass.Engineer), sr = Of(sR, PopClass.Researcher), de = Of(dR, PopClass.Engineer), dr = Of(dR, PopClass.Researcher);
        Assert.True(se is { Size: > 0 } && sr is { Size: > 0 } && de is not null && dr is not null, "前提が崩れた：技師と研究者の POP が両方の州に居ない");
        double pool = se!.Size + sr!.Size, heads = 0.1 * pool, perHead = 7.0;
        dst.RecruitBudget = 1e12; dst.FiscalFundingRatio = 1.0;
        double fE = System.Math.Min(heads * (se.Size / pool), se.Size), fR = System.Math.Min(heads * (sr.Size / pool), sr.Size);
        double dowE = se.Savings * (fE / se.Size), dowR = sr.Savings * (fR / sr.Size), cost = perHead * (fE + fR);
        double de0 = de!.Savings, dr0 = dr!.Savings, t0 = dst.Treasury;
        typeof(MultiNationSim).GetMethod("MoveSpecialists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sim, [src, dst, sR, dR, heads, perHead]);
        double tol = 1e-9 * System.Math.Max(1.0, cost + System.Math.Abs(de0) + System.Math.Abs(dr0) + System.Math.Abs(dowE) + System.Math.Abs(dowR));
        Assert.True(System.Math.Abs(dr.Savings - dr0 - dowR - cost * fR / (fE + fR)) <= tol, $"研究者の支度金 {dr.Savings - dr0 - dowR} ≠ {cost * fR / (fE + fR)}");
        Assert.True(System.Math.Abs(de.Savings - de0 - dowE - cost * fE / (fE + fR)) <= tol, $"技師の支度金 {de.Savings - de0 - dowE} ≠ {cost * fE / (fE + fR)}");
        Assert.True(System.Math.Abs(t0 - dst.Treasury - cost) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(t0) + cost), "国庫の減りが支度金と一致しない");
    }

    /// <summary>★お金の運搬の式は1つ（段2-3 E3）＝ストック×(運ぶ人数÷規模)（社会移動・徴兵・越境移民・招聘と同じ結合順）。
    /// 州間の移住（Territory.MoveBetweenRegions）で、貯蓄・債務・国債の保有が同じ式で運ばれる（旧＝(ストック÷規模)×人数＝浮動小数の端数が他の運搬と違った）。
    /// 値＝100÷7×3 と 100×(3÷7) が倍精度で違う組。⚠突然変異「旧の結合順」で赤。</summary>
    [Fact]
    public void 州間の移住のお金の運搬はストック掛ける人数の比()
    {
        var sp = new Pop { Size = 7, Savings = 100, Debt = 100, BondHoldings = 100 };
        var dp = new Pop { Size = 1, Savings = 0, Debt = 0, BondHoldings = 0 };
        Territory.MoveBetweenRegions(sp, dp, 3, new Coefficients());
        double want = 100.0 * (3.0 / 7.0);
        Assert.NotEqual(100.0 / 7.0 * 3.0, want);   // 盤面の前提＝2つの結合順が倍精度で違う
        Assert.Equal(want, dp.Savings); Assert.Equal(want, dp.Debt); Assert.Equal(want, dp.BondHoldings);
    }

    /// <summary>★人と動くお金の運搬の1本（2026-09-30 段4f・作者裁定 Q13＝書き手を Pop の中へ）＝<c>Pop.Carry</c> は送り手と受け手を同じ額で書く
    /// （貯蓄・個人債務・国債の保有のどれも、送り手が減った額と受け手が増えた額が同じ＝湧かず消えない）。州の間の移住（Territory.MoveBetweenRegions）を通して見る＋
    /// 越境（国債の保有は発行国の対外債へ振り替える＝運ぶ額0）の形。⚠突然変異「送り手の貯蓄を減らさない」「受け手の債務を足さない」で赤。</summary>
    [Fact]
    public void 人と動くお金は送り手と受け手を同じ額で書く()
    {
        var sp = new Pop { Size = 7, Savings = 100, Debt = 50, BondHoldings = 30 };
        var dp = new Pop { Size = 1, Savings = 5, Debt = 2, BondHoldings = 1 };
        Territory.MoveBetweenRegions(sp, dp, 3, new Coefficients());
        double f = 3.0 / 7.0;
        Assert.Equal(100 - 100 * f, sp.Savings); Assert.Equal(5 + 100 * f, dp.Savings);
        Assert.Equal(50 - 50 * f, sp.Debt); Assert.Equal(2 + 50 * f, dp.Debt);
        Assert.Equal(30 - 30 * f, sp.BondHoldings); Assert.Equal(1 + 30 * f, dp.BondHoldings);
        var a = new Pop { Savings = 10, Debt = 4, BondHoldings = 3 };
        var b = new Pop { Savings = 1, Debt = 0, BondHoldings = 2 };
        Pop.Carry(a, b, 6, 4, 0);   // 越境の形＝運賃を引いた持参金と債務を運び、保有は運ばない
        Assert.Equal(4.0, a.Savings); Assert.Equal(7.0, b.Savings);
        Assert.Equal(0.0, a.Debt); Assert.Equal(4.0, b.Debt);
        Assert.Equal(3.0, a.BondHoldings); Assert.Equal(2.0, b.BondHoldings);
    }

    // ── 支払いの一元化 段3-2 の番人（2026-09-30・C9）──

    /// <summary>★港と運河の建設の工賃は労働の④の持分（労働力の生産者の産出の比・賃金は労働者）へ＝旧は自国の全POP へ頭数（財を買わない）。
    /// 港＝港の州の労働・払い手は地元の投資家（資本家ルート）と国庫（国家ルート×r）・進捗は動いた額。運河＝自国の全ての活きた州の労働・払い手は投資家（貯蓄の比）・進捗は動いた額。
    /// 盤面＝英の1国の双子＝投資の割合だけが違う（港 PortInvestFrac・運河 CanalInvestFrac）＝双子の貯蓄の差が建設の流れそのもの（非公開の段を反射で1回だけ回す）。
    /// ⚠突然変異「全POP へ頭数で配る」で赤（労働者でない POP の貯蓄が動く）。</summary>
    [Fact]
    public void 港と運河の建設の工賃は労働の持分へ届く()
    {
        static MultiNationSim Uk()
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            foreach (var n in sim.Nations) n.TelemetryEnabled = false;
            sim.Diplomacy.Log.Enabled = false;
            sim.StepQuarter();
            return sim;
        }
        static (List<Pop> pops, double[] sav) Sav(World w) { var l = new List<Pop>(); foreach (var r in w.Regions) l.AddRange(r.Pops); return (l, l.ConvertAll(p => p.Savings).ToArray()); }
        static void Invoke(MultiNationSim sim, string name) => typeof(MultiNationSim).GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim, null);
        static HashSet<Pop> Labor(IEnumerable<Region> regs) { var s = new HashSet<Pop>(); foreach (var r in regs) foreach (var pr in r.Producers) if (pr.GoodId == Catalog.LaborId && pr.Output > 0) s.Add(r.Pops[pr.LaborPopIndex]); return s; }

        // 港：最初の港の州を混雑させる（律速率0）・技術の門を開く
        (World w, Region port, List<Pop> pops, double[] sav, double treas, double cap) Port(double frac)
        {
            var sim = Uk(); var w = sim.Nations[0].World;
            sim.Coef.PortInvestFrac = frac; sim.Coef.PortTechOnset = 0;
            var port = w.Regions.Find(r => r.State == RegionState.Active && r.IsPort && r.InvestorPopIndex >= 0)!;
            foreach (var r in w.Regions) r.LastTradeThrottle = 1.0;   // 他の港は混雑していない＝建設は1つの港だけ
            port.LastTradeThrottle = 0; w.FiscalFundingRatio = 1.0;
            Invoke(sim, "StepPorts");
            var (pops, sav) = Sav(w);
            return (w, port, pops, sav, w.Treasury, port.PortCapacity);
        }
        var p0 = Port(0); var p1 = Port(new Coefficients().PortInvestFrac);
        var portLabor = Labor([p1.port]);
        var investor = p1.port.Pops[p1.port.InvestorPopIndex];
        Assert.NotEmpty(portLabor);
        double gotP = 0, paidP = p0.treas - p1.treas;
        for (int i = 0; i < p1.pops.Count; i++)
        {
            double d = p1.sav[i] - p0.sav[i];
            if (ReferenceEquals(p1.pops[i], investor)) paidP -= d;
            else if (portLabor.Contains(p1.pops[i])) gotP += d;
            else Assert.True(d == 0, $"港：労働者でも投資家でもない POP（階級 {p1.pops[i].ClassId}）の貯蓄が {d} 動いた");
        }
        Assert.True(gotP > 0, "前提が崩れた：港の建設が立っていない");
        Assert.True(Math.Abs(gotP - paidP) <= 1e-9 * Math.Max(1.0, Math.Abs(p1.treas)), $"港：労働者の受け取り {gotP} ≠ 投資家と国庫の払い {paidP}");
        Assert.True(Math.Abs((p1.cap - p0.cap) * new Coefficients().PortBuildCostPerUnit - gotP) <= 1e-9 * Math.Max(1.0, gotP), "港：容量の伸びが動いた額でない");

        // 運河：スエズの技術の門を開き・開通しない建設費にする
        (World w, List<Pop> pops, double[] sav, double progress) Canal(double frac)
        {
            var sim = Uk(); var w = sim.Nations[0].World;
            sim.Coef.CanalInvestFrac = frac;
            var cp = sim.Diplomacy.Chokepoints.Find(k => k.IsCanal && !k.IsOpen)!;
            cp.TechGate = 0; cp.BuildCost = 1e18;
            Invoke(sim, "StepCanals");
            var (pops, sav) = Sav(w);
            return (w, pops, sav, cp.BuildProgress);
        }
        var c0 = Canal(0); var c1 = Canal(new Coefficients().CanalInvestFrac);
        var labor = Labor(c1.w.Regions.Where(r => r.State == RegionState.Active));
        var investors = new HashSet<Pop>(CapitalMarket.Investors(c1.w));   // ★段3-3（C13・2026-09-30）：運河の出資者＝投資家の述語の1本（配当の受け手と同じ集合・旧＝各州の資本家1人）
        Assert.NotEmpty(labor);
        double gotC = 0, paidC = 0;
        for (int i = 0; i < c1.pops.Count; i++)
        {
            double d = c1.sav[i] - c0.sav[i];
            bool inv = investors.Contains(c1.pops[i]), lab = labor.Contains(c1.pops[i]);
            if (inv && !lab) paidC -= d;
            else if (lab && !inv) gotC += d;
            else if (!inv && !lab) Assert.True(d == 0, $"運河：労働者でも投資家でもない POP（階級 {c1.pops[i].ClassId}）の貯蓄が {d} 動いた");
        }
        double moved = c1.progress - c0.progress;
        Assert.True(moved > 0 && gotC > 0, "前提が崩れた：運河の建設が立っていない");
        Assert.True(Math.Abs(gotC - moved) <= 1e-9 * moved && Math.Abs(paidC - moved) <= 1e-9 * moved, $"運河：労働者の受け取り {gotC}・投資家の払い {paidC}・進捗 {moved} が一致しない");
    }

    /// <summary>★人と動くお金の換算（2026-09-30 処理の一元化 段4e＝(120) の残り）：越境移民の持参金と債務は受入国の通貨へ換算して運ぶ（移し方の1本と同じ規則
    /// ＝払い手の為替÷受け手の為替）。差は換算の台帳へ＝世界の名目の基礎貨幣の動き＝換算の台帳の動き・国ごとの D は各自の通貨で閉じる。
    /// 盤面＝英（為替1）→仏（為替0.5）＝仏は2倍の名目で受け取る（運賃0）。⚠突然変異「持参金を換算しない」「債務を換算しない」で赤。</summary>
    [Fact]
    public void 越境の持参金と債務は受入国の通貨へ換算され差は換算の台帳へ入る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.Coef.MigrationFarePerkKm = 0;
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 0.5;
        var sp = uk.Regions[0].PopOf(PopClass.Worker)!; var dp = fr.Regions[0].PopOf(PopClass.Worker)!;
        uk.Bank!.Lend(sp, 30);
        double flow = 0.1 * sp.Size, sav = sp.Savings * (flow / sp.Size), debt = sp.Debt * (flow / sp.Size);
        Assert.True(sav > 0 && debt > 0, "前提が崩れた：持参金か債務が無い");
        double dUk = D(uk), dFr = D(fr), fx0 = sim.Diplomacy.CumulativeFxResidual, bm0 = uk.BaseMoney + fr.BaseMoney;
        double dpSav0 = dp.Savings, dpDebt0 = dp.Debt, hFr = fr.Bank!.HouseholdLoans, hUk = uk.Bank.HouseholdLoans;
        sim.MoveMigrants(uk, fr, uk.Regions[0], fr.Regions[0], PopClass.Worker, flow);
        double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(bm0));
        Assert.Equal(dpSav0 + 2 * sav, dp.Savings, 6);
        Assert.Equal(dpDebt0 + 2 * debt, dp.Debt, 6);
        Assert.Equal(hFr + 2 * debt, fr.Bank.HouseholdLoans, 6);
        Assert.Equal(hUk - debt, uk.Bank.HouseholdLoans, 6);
        double fxMoved = sim.Diplomacy.CumulativeFxResidual - fx0;
        Assert.Equal(sav - debt, fxMoved, 6);   // 換算の差＝運んだ基礎貨幣（持参金−債務）×(0.5 の国の名目 2倍 − 1)
        Assert.True(System.Math.Abs(uk.BaseMoney + fr.BaseMoney - bm0 - fxMoved) <= tol, "世界の名目の動き ≠ 換算の台帳の動き");
        Assert.True(System.Math.Abs(D(uk) - dUk) <= tol && System.Math.Abs(D(fr) - dFr) <= tol, "国ごとの D が閉じない");
    }

    /// <summary>★人と動くお金の換算（段4e）：割譲で州ごと別の国へ移る POP の貯蓄と債務は受け手の通貨へ（換算の1本・差は換算の台帳へ）。
    /// 旧＝名目のまま＝為替 1 の国の州が為替 0.5 の国へ移ると貯蓄の金の値打ちが半分になった。⚠突然変異「割譲で換算しない」で赤。</summary>
    [Fact]
    public void 割譲の州の貯蓄と債務は受け手の通貨へ換算され差は換算の台帳へ入る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.ExchangeRate = 1.0; fr.ExchangeRate = 0.5;
        var r = uk.Regions[1];
        var mer = r.Pops[r.MerchantPopIndex];
        uk.Bank!.Lend(mer, 50);
        double S = r.SumSavings(), Dr = 0; foreach (var p in r.Pops) Dr += p.Debt;
        Assert.True(S > 0 && Dr > 0, "前提が崩れた");
        double dUk = D(uk), dFr = D(fr), fx0 = sim.Diplomacy.CumulativeFxResidual, bm0 = uk.BaseMoney + fr.BaseMoney, hFr = fr.Bank!.HouseholdLoans;
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        double S1 = r.SumSavings(), D1 = 0; foreach (var p in r.Pops) D1 += p.Debt;
        Assert.Equal(2 * S, S1, 6); Assert.Equal(2 * Dr, D1, 6);
        Assert.Equal(hFr + 2 * Dr, fr.Bank.HouseholdLoans, 6);
        double fxMoved = sim.Diplomacy.CumulativeFxResidual - fx0;
        double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(bm0));
        Assert.True(System.Math.Abs(fxMoved - (S - Dr)) <= tol, $"換算の台帳の動き {fxMoved} ≠ 州の基礎貨幣 {S - Dr}");
        Assert.True(System.Math.Abs(uk.BaseMoney + fr.BaseMoney - bm0 - fxMoved) <= tol, "世界の名目の動き ≠ 換算の台帳の動き");
        Assert.True(System.Math.Abs(D(uk) - dUk) <= tol && System.Math.Abs(D(fr) - dFr) <= tol, "割譲で国ごとの D が閉じない");
    }
}
