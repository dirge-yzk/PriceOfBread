using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spa) A′ 役務の不動点の閉じた式（係数 <see cref="Coefficients.ServiceClosedFixedPoint"/>）の番人。
/// ①帯の端の不動点の近くにいる役務（品薄が続き天井の近くに張り付いた教育）は、旬の1歩を閉じた式で進め、日次の写像を h 回まわした値と相対 3e-5 以内（段0 の物差し＝最大 2.9e-5）。
/// ②値段が参照より上にいて需給が下へ押す（品余り）＝この旬に参照へ向かう（またぐ）役務は閉じた式を使わず日次へ戻る＝旗の有無で1ビットも違わない。
/// ⚠参照へ向かう月（値段と平行移動が逆の側）は同じ側に不動点が無い＝〈参照をまたがない〉の判定（u0·c≧0）だけでなく、x＞0・不動点の側（t＞0）・不動点からの距離・2次の項の判定も同じ月を日次へ戻す
/// （突然変異「u0·c≧0・x＞0・t＞0 の3つを外す」でも②は緑＝距離と2次の項が戻した・measurements/spa_20261023_prereg.txt）。②はその性質（またぐ月は旗の有無で1ビットも違わない）を見る。</summary>
public class ServiceClosedTests
{
    private static (double flagOff, double flagOn) Step(double priceMult, double demand, double supply, int days, int times)
    {
        double Run(double flag)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var r = sim.World.Region; var c = sim.World.Coef;
            c.ServiceClosedFixedPoint = flag;
            foreach (var g in r.Goods) { var m0 = r.Markets[g.Id]; m0.LastDemand = 0; m0.LastProduced = 0; }   // 他の財は死んだ財（動かさない）
            var edu = r.Goods.First(g => g.Id == Catalog.EducationId);
            var m = r.Markets[edu.Id];
            m.LastDemand = demand; m.LastProduced = supply; m.LastImport = 0; m.LastExport = 0; m.Stock = 0;
            m.Price = priceMult * edu.RefPrice * r.PriceLevel;
            for (int i = 0; i < times; i++) MarketFlow.StepDay(r, c, days);
            return m.Price / (edu.RefPrice * r.PriceLevel);
        }
        return (Run(0), Run(1));
    }

    [Fact]
    public void 天井の近くに張り付いた役務は閉じた式で日次と同じ値へ進む()
    {
        // 品薄（需要 300・供給 100）が続いた役務は天井（賃金の帯の上の端）の近くの不動点に居る＝まず日次で不動点の近くまで寄せてから比べる
        var (warm, _) = Step(1.0, 300, 100, 1, 3000);
        Assert.True(warm > 2.0, $"前提が崩れた：品薄の役務が天井の近くへ寄っていない（参照の {warm} 倍）");
        var (daily, closed) = Step(warm * 1.001, 300, 100, 15, 1);   // 不動点から少しずらす（対数の距離 0.001）
        Assert.NotEqual(daily, closed);   // 閉じた式を使った（旗で値が変わる）
        Assert.True(System.Math.Abs(closed - daily) <= 3e-5 * daily, $"閉じた式の誤差 {(closed - daily) / daily:E2} が 3e-5 を超えた");
    }

    [Fact]
    public void 参照をまたぐ向きの役務は日次へ戻る()
    {
        // 値段は参照より上・品余り（需要 100・供給 300）＝この旬に参照へ向かって下がる＝閉じた式を使わない
        var (off, on) = Step(2.5, 100, 300, 15, 1);   // 帯の内側の外（内側の閉じた式は使わない＝日次か A′ のどちらか）
        Assert.True(off < 2.5, "前提が崩れた：品余りの役務が下がっていない");
        Assert.Equal(off, on);   // 1ビットも違わない
    }
}
