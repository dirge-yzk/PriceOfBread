using PriceOfBread.SimCore;
using System.Linq;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 説明API（#63）：各数値の「なぜこの値か」を因果分解して保持する説明台帳の検証。
/// 台帳は本計算を横で観察して溜めるだけ＝**装着しても数値は1ビットも動かない**（null 条件で短絡）。
/// また記帳した実寄与量の合計は、その四半期の最終値に一致する（浮動小数の加算順の差だけ許容）。
/// 正統性は既定でストック（LegitimacyIsStock=1.0・履歴が累積）かつ複数現場で書き換わるため、
/// 「前期からの累積＋今期の回復−侵食−マンデート＋後続イベント（戦勝/デフォルト/内戦/一揆/建国）」に分解される。
/// </summary>
public class ExplainTests
{
    [Fact]
    public void 説明モードは正統性を1ビットも動かさない()
    {
        // 装着（ON）と非装着（OFF・既定）を並走させ、正統性がビット単位で一致することを確かめる。
        var off = new Simulation(NationConfig.France().ToScenario(1789), new ModernizerAgent());
        var on = new Simulation(NationConfig.France().ToScenario(1789), new ModernizerAgent());
        for (int q = 0; q < 200; q++) { off.StepQuarter(); on.StepQuarter(); }

        on.World.Explain = new ExplainLedger();   // 片方だけ最後の1四半期に装着
        off.StepQuarter();
        on.StepQuarter();

        Assert.True(off.World.Legitimacy == on.World.Legitimacy,   // == ＝厳密比較（1ビットも動かない）
            $"説明モードが正統性を動かした：OFF {off.World.Legitimacy} ≠ ON {on.World.Legitimacy}");
        Assert.NotEmpty(on.World.Explain!.Get("Legitimacy"));
    }

    [Fact]
    public void 説明APIは全国家で正統性の内訳が最終値に一致する()
    {
        // 53カ国を進めて各国の正統性書き換え現場を通し、内訳の実寄与量の合計が最終値に一致することを確かめる
        // ＝記帳していない書き込み現場があれば合計がずれて検出される（イベント現場を含む網羅性の証明）。
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 80; q++) sim.StepQuarter();
        foreach (var n in sim.Nations) n.World.Explain = new ExplainLedger();

        for (int q = 0; q < 3; q++)
        {
            sim.StepQuarter();
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                string name = w.SelfPower?.Name ?? "?";
                // ★2026-09-03（キューA-04／出典 トリアージ 2-A 10-1d）：番人を**台帳にある全量**へ広げる。
                //   旧＝正統性1本だけを検査しており、他の量（天命・行政力・国民意識・制度疲労）に
                //   記帳漏れの書き手が入っても緑のままだった＝二重計上の再発を検知できない。
                //   ★2026-09-08（トリアージ 2-A 10-1d の残り1量）：旧コメントは「FoodFill は州ごとの量」と書いていたが**現物と食い違う**。
                //   World.FoodFill は国のスカラー（World.cs）で、書き手は Simulation.DemographicStep の1本だけ＝在庫（EMA）であり検査できる。
                foreach (var (key, label, value) in new (string, string, double)[]
                {
                    ("Legitimacy", "正統性", w.Legitimacy),
                    ("DynasticMandate", "天命", w.DynasticMandate),
                    ("AdminEfficiency", "行政力", w.AdminEfficiency),
                    // 国民意識（POP の加重平均＝統計量で在庫ではなかった）は (1193) c119nd で国民意識ごと消した。
                    // 食料充足は平滑つきの在庫＝台帳の2行（(1−s)×前期の備蓄 ＋ s×今期の需給）の和が新値と代数的に一致する。
                    // 助走凍結や書いていない四半期は terms.Count==0 の早期 continue で素通りする。
                    ("FoodFill", "食料充足", w.FoodFill),
                })
                {
                    var terms = w.Explain!.Get(key);
                    if (terms.Count == 0) continue;   // その量を今期書いていない国＝検査対象外（書いたのに合わないことだけを見る）
                    double sum = 0;
                    foreach (var t in terms) sum += t.Delta;
                    // ★2026-09-03：赤のときに**内訳をそのまま出す**＝「どの行が余っているか」を人が読める
                    //   （旧＝合計と実値の2数だけで、犯人探しに毎回コードを読み直していた）。
                    Assert.True(System.Math.Abs(sum - value) < 1e-9,
                        $"{name}：{label}の内訳合計 {sum} ≠ 実値 {value}（差 {sum - value:E3}／記帳していない書き込み現場が残っている）｜内訳＝"
                        + string.Join(" ／ ", terms.Select(t => $"{t.Reason}:{t.Delta:F12}")));
                }
                Assert.NotEmpty(w.Explain!.Get("Legitimacy"));
            }
        }
    }
}
