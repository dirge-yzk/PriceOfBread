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
    // ⚠**国際移民の世界プール**（WorldMigrationPoolFrac＝ゲーム外から人と持参金が湧く既知の湧き口）は、この番人の世界では係数で0にして合わせる。
    //   ★回1-2（国際移民を閉じる）でプールそのものが消えたら、下の `WorldMigrationPoolFrac = 0` の1行を外して締める。
    // ⚠植民地（Colony オブジェクト）は国の台帳の外にある＝入植・植民地交易・州昇格は世界の外との出入りに見える。
    // ★状態は1つも増やさない（出生と死亡の累積 World.DiagBirths/DiagDeaths は popprobe の計器と共用の純記帳）。
    // ⚠静的フラグ（ConservationProbe）は使わない（上と同じ理由＝並列のテストを数え込む）＝このテストが自分で合計を取る。

    private sealed class WorldLedgerRun
    {
        public readonly List<string> PopBreaks = [], MoneyBreaks = [];
        public double PopMoved, MoneyMoved, PopUnexplained, MoneyUnexplained;
        public const int ShortQuarters = 40;   // 常設の長さ（約40秒）。全長（668四半期・約7分）は下の保留の番人
        public readonly int Quarters;

        public WorldLedgerRun(int quarters)
        {
            Quarters = quarters;
            var coef = new Coefficients { WorldMigrationPoolFrac = 0 };   // ★世界プール＝既知の湧き口を番人の世界では閉じる（回1-2 でこの行を外す）
            var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent(), coef);
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

    /// <summary>★全長（668四半期）の世界の合計（保留＝2026-09-26 に**漏れを見つけた**番人）。
    /// ⚠1回約7分＝常設しない（savetest の長距離と同じ2段構え＝直したら手で外して確かめる）。人口の側は全長でも緑だった（2026-09-26 の1回）。</summary>
    [Fact(Skip = "漏れの発見（2026-09-26・回0-4）：全長668四半期で世界の基礎貨幣が2四半期だけ鋳造・不換発行で説明できない＝Q317（1862年）−1009.0・Q598（1932年）−888.4。"
               + "現場＝Diplomacy の講和の賠償（Diplomacy.cs の `if (loser.Nation is { } lw) lw.CumulativeTradeFlow -= rep; if (winner.Nation is { } ww) …`）：勝者が抽象標的（Nation の無い勢力＝1862年はカザフ草原・敗者ポーランド・賠償1009.005／1932年はアチェ・敗者マイソール・888.411）のとき、"
               + "払った国だけが流出を記帳し金は世界から消える（片側記帳）。直すのは保存則の回（1-1）。直したら Skip を外し、1回だけ手で回して緑を確かめる")]
    public void 世界の基礎貨幣は全長でも鋳造と不換発行でしか動かない()
    {
        var run = WorldRunFull.Value;
        Assert.True(run.MoneyMoved > 0, "世界の基礎貨幣が一度も動いていない＝番人が何も検査していない");
        Assert.True(run.MoneyBreaks.Count == 0,
            $"{run.MoneyBreaks.Count}/{run.Quarters} 四半期で片側記帳（金が湧いた/消えた・累計 {run.MoneyUnexplained:+0.0;-0.0}）：\n" + string.Join("\n", run.MoneyBreaks.Take(8)));
    }
}
