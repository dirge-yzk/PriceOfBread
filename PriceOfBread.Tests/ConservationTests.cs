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
        //   ⚠`ExportRevenueToProducers`（既定0.5）は `BilateralMarket` が読む生きた機構で、
        //   輸出代金の一部が生産者POPへ直接渡る＝**貨幣が国庫を経由せず動く**経路。上のD不変が壊れやすい場所そのもの。
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
        public readonly List<string> PopBreaks = [], MoneyBreaks = [];
        public double PopMoved, MoneyMoved, PopUnexplained, MoneyUnexplained;
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
        var ezo = new Power { Id = sim.Diplomacy.Powers.Max(p => p.Id) + 1, Name = "蝦夷地", BaseName = "蝦夷地", Military = 0.2, Gdp = 0, Treasury = 800, TradeWeight = 0.0 };
        var cc = Geo.ColonyCoord("蝦夷地");
        var col = new Colony { Name = "蝦夷地", Resources = [20, 11], Population = 0.05, PopSize = 0.05 * coef.ColonialPopScale, Latitude = cc.Lat, Longitude = cc.Lon };
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
}
