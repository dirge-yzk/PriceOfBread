using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 参政権の範囲2法（作者裁定2026-08-16・点火2026-09-06・旧の旗 SuffrageScopeGate は 2026-10-18 に焼き込み）の経路。
/// 植民地参政権＝植民地格の州（RegionTier.Colonial）のPOPを政治的国民に算入：発言権（InterestGroups.Blocs）と
/// 課税への同意（Fiscal の consent＝徴税力）へ、なし0／自国民のみ=CoreEthnicShare／全面=全量の梯子で効く。
/// 双子シム比較スタイル（MobilityTests と同型）＝法だけが違う2世界の方向を見る。
/// </summary>
public class SuffrageScopeTests
{
    /// <summary>双子を同一状態まで走らせ、共通の細工（ゲート点火・植民地格の州・非中核pop・巨大貯蓄）を施す。</summary>
    private static (Simulation Off, Simulation On) Twins()
    {
        var off = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        off.Run(20); on.Run(20);
        foreach (var sim in new[] { off, on })
        {
            var w = sim.World;
            w.Suffrage = Suffrage.AdultMale;       // 大衆に発言権がある状態＝算入がそのまま声になる
            var col = w.Regions[1];                // 非首都州を植民地格に
            col.Tier = RegionTier.Colonial;
            foreach (var p in col.Pops) p.CoreEthnicShare = new Share(0.0);   // 全員が非中核（現地民）＝「自国民のみ」では算入0
        }
        on.World.ColonialSuffrage = ColonialSuffrageLaw.Full;      // ON側だけ全面算入（OFF側は「なし」＝算入0）
        return (off, on);
    }

    // ★J-001の範囲（2026-09-04・運転規則§9）：参政権の範囲が縛るのは代表（法）と課税への同意まで。**発言権は票の前提にしない**
    //   （チャーティスト1839/反穀物法同盟1838/1848年＝票の無い層に声があった）。利益集団の重みを算入率で乗じる測定専用の腕
    //   SuffrageScopeVoice は 2026-09-26 に削除した（棚卸し dorm-49）＝その腕を立てて乗ることを確かめていたテストも一緒に削除。
    [Fact]
    public void 参政権の範囲だけでは植民地州の大衆の発言権は変わらない()
    {
        var (off, on) = Twins();
        foreach (var sim in new[] { off, on })
        {
            var col = sim.World.Regions[1];
            foreach (var p in col.Pops)
                if (!p.Closed && !p.IsModernElite && !p.IsSoldier) { p.SetSavings(1e9); break; }
        }
        var (_, _, massOff) = InterestGroups.Blocs(off.World);
        var (_, _, massOn) = InterestGroups.Blocs(on.World);
        Assert.Equal(massOff, massOn, 12);   // 算入率は利益集団の重みに触れない（ビット恒等）
    }

    [Fact]
    public void 植民地参政権の全面算入で植民地州の課税への同意が立つ()
    {
        var (off, on) = Twins();
        foreach (var sim in new[] { off, on })
        {
            var w = sim.World;
            w.Coef.TaxConsentWeight = 0.5;   // 課税への同意を明示的に有効化（双子とも）
            w.TaxRate = new Share(0.2);                 // 所得税（同意が効く税）
            foreach (var p in w.Regions[1].Pops)
                if (!p.IsSoldier) { p.EarnedPerMonth = new MoneyPerMonth(1_000); p.SetSavings(System.Math.Max(p.Savings.Value, 100_000)); }   // 課税ベースを確保（双子とも同額）
            Fiscal.Step(w, w.Coef);
        }
        // 算入された植民地pop＝声がある＝同意が立つ＝所得税が多く取れる（貯蓄がより減る）。
        // 算入されない（なし）＝「発言権のない者からは取りきれない」側＝税が薄い。
        double savOff = 0, savOn = 0;
        foreach (var p in off.World.Regions[1].Pops) if (!p.IsSoldier) savOff += p.Savings.Value;
        foreach (var p in on.World.Regions[1].Pops) if (!p.IsSoldier) savOn += p.Savings.Value;
        Assert.True(savOn < savOff,
            $"全面算入なのに植民地州の徴税力が上がらない（なし後貯蓄 {savOff:F0} / 全面後貯蓄 {savOn:F0}）");
    }
}
