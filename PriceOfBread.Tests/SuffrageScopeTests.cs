using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 参政権の範囲2法（作者裁定2026-08-16・主フラグ SuffrageScopeGate＝1.0で点火済み・2026-09-06）のON経路。
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
            w.Coef.SuffrageScopeGate = 1.0;        // 点火（双子とも＝差は法だけ）
            w.Suffrage = Suffrage.AdultMale;       // 大衆に発言権がある状態＝算入がそのまま声になる
            var col = w.Regions[1];                // 非首都州を植民地格に
            col.Tier = RegionTier.Colonial;
            foreach (var p in col.Pops) p.CoreEthnicShare = 0.0;   // 全員が非中核（現地民）＝「自国民のみ」では算入0
        }
        on.World.ColonialSuffrage = ColonialSuffrageLaw.Full;      // ON側だけ全面算入（OFF側は「なし」＝算入0）
        return (off, on);
    }

    // ★J-001の範囲（2026-09-04・運転規則§9）：参政権の範囲が縛るのは代表（法）と課税への同意まで。**発言権は票の前提にしない**
    //   （チャーティスト1839/反穀物法同盟1838/1848年＝票の無い層に声があった）。利益集団の重みを算入率で乗じる腕は別フラグ
    //   SuffrageScopeVoice（既定0）に分離した。以下の2主張＝主フラグだけでは発言権は動かない／両腕ONで従来（V6）どおり乗る。
    [Fact]
    public void 参政権の範囲だけでは植民地州の大衆の発言権は変わらない()
    {
        var (off, on) = Twins();
        foreach (var sim in new[] { off, on })
        {
            var col = sim.World.Regions[1];
            foreach (var p in col.Pops)
                if (!p.Closed && !p.IsModernElite && !p.IsSoldier) { p.Savings = 1e9; break; }
        }
        var (_, _, massOff) = InterestGroups.Blocs(off.World);
        var (_, _, massOn) = InterestGroups.Blocs(on.World);
        Assert.Equal(massOff, massOn, 12);   // 発言権の腕が0＝算入率は利益集団の重みに触れない（ビット恒等）
    }

    [Fact]
    public void 植民地参政権の全面算入で植民地州の大衆が発言権に乗る()
    {
        var (off, on) = Twins();
        foreach (var sim in new[] { off, on })
        {
            sim.World.Coef.SuffrageScopeVoice = 1.0;   // 発言権の腕を明示的にON（V6の全効果＝比較測定用の形）
            // 植民地州の大衆popへ巨大な貯蓄を注入（双子とも同額）＝算入されれば大衆の力が跳ね、されなければ1ビットも動かない
            var col = sim.World.Regions[1];
            foreach (var p in col.Pops)
                if (!p.Closed && !p.IsModernElite && !p.IsSoldier) { p.Savings = 1e9; break; }
        }
        var (_, _, massOff) = InterestGroups.Blocs(off.World);
        var (_, _, massOn) = InterestGroups.Blocs(on.World);
        Assert.True(massOn > massOff,
            $"全面算入なのに植民地州の大衆が発言権に乗らない（なし {massOff:F4} / 全面 {massOn:F4}）");
    }

    [Fact]
    public void 植民地参政権の全面算入で植民地州の課税への同意が立つ()
    {
        var (off, on) = Twins();
        foreach (var sim in new[] { off, on })
        {
            var w = sim.World;
            w.Coef.TaxConsentWeight = 0.5;   // 課税への同意を明示的に有効化（双子とも）
            w.TaxRate = 0.2;                 // 所得税（同意が効く税）
            foreach (var p in w.Regions[1].Pops)
                if (!p.IsSoldier) { p.Earned = 1_000; p.Savings = System.Math.Max(p.Savings, 100_000); }   // 課税ベースを確保（双子とも同額）
            Fiscal.Step(w, w.Coef);
        }
        // 算入された植民地pop＝声がある＝同意が立つ＝所得税が多く取れる（貯蓄がより減る）。
        // 算入されない（なし）＝「発言権のない者からは取りきれない」側＝税が薄い。
        double savOff = 0, savOn = 0;
        foreach (var p in off.World.Regions[1].Pops) if (!p.IsSoldier) savOff += p.Savings;
        foreach (var p in on.World.Regions[1].Pops) if (!p.IsSoldier) savOn += p.Savings;
        Assert.True(savOn < savOff,
            $"全面算入なのに植民地州の徴税力が上がらない（なし後貯蓄 {savOff:F0} / 全面後貯蓄 {savOn:F0}）");
    }

    [Fact]
    public void 両法を最大にするとゲート点火前と同じ集計になる()   // 連続性＝全面＋ありは全pop算入率1.0＝点火前の物差しと厳密一致
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        w.Regions[1].Tier = RegionTier.Colonial;
        foreach (var p in w.Regions[1].Pops) p.CoreEthnicShare = 0.25;   // 非中核が居ても「全面＋あり」なら全量算入
        w.Coef.SuffrageScopeGate = 0;
        var (t0, m0, s0) = InterestGroups.Blocs(w);
        w.Coef.SuffrageScopeGate = 1.0;
        w.ColonialSuffrage = ColonialSuffrageLaw.Full;
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.Granted;
        var (t1, m1, s1) = InterestGroups.Blocs(w);
        Assert.Equal(t0, t1);
        Assert.Equal(m0, m1);
        Assert.Equal(s0, s1);
    }
}
