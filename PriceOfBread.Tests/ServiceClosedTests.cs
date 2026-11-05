using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spa) A′ 役務の不動点の閉じた式（<c>MarketFlow.ServiceClosed</c>）の番人（旗 ServiceClosedFixedPoint は (843) で焼き込み。(846) で旗 0 との比べを〈日次の 1 歩を h 回〉との比べに置き換えて戻した）。
/// 物差し＝<c>MarketFlow.StepDay(r, c, 1, …)</c> を h 回まわした盤面。比べる側＝<c>StepDay(r, c, h, …)</c> を 1 回。
/// ①帯の端の不動点の近くにいる役務（品薄が続き天井の近くに張り付いた教育）は、旬の1歩を閉じた式で進め、日次の 1 歩を h 回まわした値と相対 3e-5 以内（段0 の物差し＝最大 2.9e-5・今の既定の <c>ServiceClosedMaxCurv</c> は 0.01 で誤差は小さい側）。
/// ②値段が参照より上にいて需給が下へ押す（品余り）＝この旬に参照へ向かう（またぐ）役務は閉じた式を使わず日次へ戻る＝h 日の 1 歩と日次の 1 歩を h 回が 1 ビットも違わない。
/// ⚠参照へ向かう月（値段と平行移動が逆の側）は同じ側に不動点が無い＝〈参照をまたがない〉の判定（ServiceClosed の①u0·c≧0）に重なって、次の判定も同じ月を日次へ戻す：
///   ・x＞0（ニュートン法の中の平行移動の後の距離が正）・②不動点の側（t＞0）・③不動点からの対数の距離（PinnedClosedMaxDev）・④2次の項（ServiceClosedMaxCurv）。
///   突然変異「①・x＞0・② の3つを外す」でも②は緑（③④が戻した）＝赤にできる突然変異は無い（記録・measurements/spa_20261023_prereg.txt）。②はその性質（またぐ月は日次と1ビットも違わない）を見る。
///   閉じた式を使わない突然変異・閉じた式の値を 1+1e-4 倍する突然変異は ① が赤になる（記録・measurements/m846_20261105/）。</summary>
public class ServiceClosedTests
{
    /// <summary>教育（役務）の値段を参照の priceMult 倍から、需要 demand・供給 supply のまま days 日の 1 歩を times 回。closedStep＝false なら 1 日の歩を days×times 回。戻り＝参照に対する倍率と日次の回の台帳。</summary>
    private static (double mult, long runs) Step(bool closedStep, double priceMult, double demand, double supply, int days, int times)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        foreach (var g in r.Goods) { var m0 = r.Markets[g.Id]; m0.LastDemand = 0; m0.LastProduced = 0; }   // 他の財は死んだ財（動かさない）
        var edu = r.Goods.First(g => g.Id == Catalog.EducationId);
        var m = r.Markets[edu.Id];
        m.LastDemand = demand; m.LastProduced = supply; m.LastImport = 0; m.LastExport = 0; m.Stock = 0;
        m.Price = priceMult * edu.RefPrice * r.PriceLevel;
        var cost = new long[CostLedger.Count];
        for (int i = 0; i < times; i++)
        {
            if (closedStep) MarketFlow.StepDay(r, c, days, cost);
            else for (int d = 0; d < days; d++) MarketFlow.StepDay(r, c, 1, cost);
        }
        return (m.Price / (edu.RefPrice * r.PriceLevel), cost[CostLedger.PriceDailyRuns]);
    }

    [Fact]
    public void 天井の近くに張り付いた役務は閉じた式で日次と同じ値へ進む()
    {
        // 品薄（需要 300・供給 100）が続いた役務は天井（賃金の帯の上の端）の近くの不動点に居る＝まず日次で不動点の近くまで寄せてから比べる
        var (warm, _) = Step(false, 1.0, 300, 100, 1, 3000);
        Assert.True(warm > 2.0, $"前提が崩れた：品薄の役務が天井の近くへ寄っていない（参照の {warm} 倍）");
        var (daily, dailyRuns) = Step(false, warm * 1.001, 300, 100, 15, 1);   // 不動点から少しずらす（対数の距離 0.001）・日次の 1 歩を 15 回
        var (closed, closedRuns) = Step(true, warm * 1.001, 300, 100, 15, 1);  // 15 日の 1 歩を 1 回
        Assert.True(dailyRuns > 0, "前提が崩れた：日次の写像が回っていない");
        Assert.Equal(0, closedRuns);      // 閉じた式を使った（日次の写像を回していない）
        Assert.NotEqual(daily, closed);   // 閉じた式を使った（値が日次とは丸めの桁を超えて違う）
        Assert.True(System.Math.Abs(closed - daily) <= 3e-5 * daily, $"閉じた式の誤差 {(closed - daily) / daily:E2} が 3e-5 を超えた");
    }

    [Fact]
    public void 参照をまたぐ向きの役務は日次へ戻る()
    {
        // 値段は参照より上・品余り（需要 100・供給 300）＝この旬に参照へ向かって下がる＝閉じた式を使わない
        var (daily, _) = Step(false, 2.5, 100, 300, 15, 1);   // 帯の内側の外（内側の閉じた式は使わない＝日次か A′ のどちらか）
        var (closed, closedRuns) = Step(true, 2.5, 100, 300, 15, 1);
        Assert.True(daily < 2.5, "前提が崩れた：品余りの役務が下がっていない");
        Assert.True(closedRuns > 0, "前提が崩れた：15 日の 1 歩が日次の写像を回していない（閉じた式を使っている）");
        Assert.Equal(daily, closed);   // 1ビットも違わない
    }

    [Fact]
    public void 参照の直上から品余りで内側の帯を出て参照の下へ抜ける役務は日次へ戻る()
    {
        // (854) 反証役 低 4：内側の閉じた式（|t|＜0.5）は旬の終わりが帯の内側のときだけ使う＝旬の終わりが内側を出る歩（ここでは 70 日）だけが ServiceClosed に届く。
        //   参照の 1.002 倍・品余り（需要 1・供給 1000）＝70 日の途中で参照をまたぎ、終わりは参照の約 0.4 倍（帯の内側の外）。値段と平行移動が参照の逆の側（ServiceClosed の ①）＝閉じた式を使わず日次の写像を 70 回回す。
        //   ⚠ ①は ③（不動点からの距離 0.93＞0.1）と ④ が重ねて止める＝到達できる盤面（旬の終わりが帯の内側を出る歩）では ① 1 本だけを外しても結果は変わらない（等価）。判定を全部外す（①②x＞0③④）と閉じた式が参照の下の不動点の値（約 0.394 倍）を返して赤になる。
        //   突然変異（measurements/m854_20261105/mutation_854.txt）：①だけ・①②x＞0・さらに ③・さらに ④（判定を全部外す）。最初の 3 つは緑（等価）・最後だけ赤。
        var (daily, dailyRuns) = Step(false, 1.002, 1, 1000, 70, 1);
        var (closed, closedRuns) = Step(true, 1.002, 1, 1000, 70, 1);
        Assert.True(daily < 0.5, $"前提が崩れた：参照をまたいで帯の内側の外へ抜けていない（参照の {daily} 倍）");
        Assert.Equal(70, dailyRuns);
        Assert.Equal(70, closedRuns);      // 閉じた式を使わず日次の写像を 70 回回した
        Assert.Equal(daily, closed);       // 1ビットも違わない
    }
}
