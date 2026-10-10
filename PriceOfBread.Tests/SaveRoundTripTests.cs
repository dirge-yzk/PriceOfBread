using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>セーブ/ロードの往復の番人（2026-09-03・キューA-06／出典 トリアージ 2-A 10-3）。
///
/// ⚠**きっかけ**：savetest は Runner のコマンドとしてしか存在せず、受け皿として書かれていた `batch_final` は
/// リポジトリに実在しない＝「常設」と書いてあるのに**誰も自動では回していない**状態だった。
/// 新しいフィールドを足したときに落ちる型（SaveGame の switch 漏れ＝sbyte 事故）は、気づくのが遅いほど高くつく。
///
/// ★ここは**短距離**（助走＋数四半期）に留める＝フルスイートの所要時間を守るため。
/// ⚠長距離（300四半期）の往復は Runner の `savetest` が受け持つ（受け入れのたび手で走らせる）＝2段構え。
/// マルコフ違反（保存されない隠れ状態・静的キャッシュ依存）は長距離ほど露見するので、短距離版は
/// 「型を足したときに即死する」ことの検出器と割り切る。</summary>
public class SaveRoundTripTests
{
    [Fact]
    public void セーブとロードの往復で世界が一致する()
    {
        const int PreQ = 8, PostQ = 4;   // 短距離＝数秒。長距離は Runner の savetest（300Q）が担当
        var sim1 = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        sim1.StartWarmup();
        for (int q = 0; q < PreQ; q++) sim1.StepQuarter();

        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);

        for (int q = 0; q < PostQ; q++) { sim1.StepQuarter(); sim2.StepQuarter(); }

        Assert.Equal(sim1.Nations.Count, sim2.Nations.Count);
        for (int i = 0; i < sim1.Nations.Count; i++)
        {
            var a = sim1.Nations[i].World; var b = sim2.Nations[i].World;
            double ra = System.Math.Abs((a.BaseMoney - b.BaseMoney).Value) / System.Math.Max(1.0, System.Math.Abs(a.BaseMoney.Value));
            double rp = System.Math.Abs(a.TotalPopulation - b.TotalPopulation) / System.Math.Max(1.0, a.TotalPopulation);
            Assert.True(System.Math.Max(ra, rp) < 1e-9,
                $"{a.SelfPower?.Name}：継続系列と復元系列が食い違う（基礎貨幣 {a.BaseMoney:F2}⇔{b.BaseMoney:F2}／人口 {a.TotalPopulation:F0}⇔{b.TotalPopulation:F0}）");
        }
    }

    /// <summary>★眠りの印は作業領域（2026-09-30 計算量の予算 第2回）：国内価格の月の残りの眠り（Market.FlowAsleep）と清算の成約しなかった行の印
    /// （Market.ClearFailLp）はセーブに載らない＝ロードした盤面は印なしから作り直す（四半期の境のセーブでは次の歩が月の最初の旬と四半期の最初の清算＝往復は一致）。
    /// ⚠突然変異「[NonSerialized] を外す」で赤。</summary>
    [Fact]
    public void 眠りの印はセーブに載らない()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var m1 = sim1.Nations[0].World.Regions[0].Markets[0];
        m1.FlowAsleep = true; m1.ClearFailLp = 5.0;
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var m2 = SaveGame.Load(ms).Nations[0].World.Regions[0].Markets[0];
        Assert.False(m2.FlowAsleep);
        Assert.Equal(0.0, m2.ClearFailLp);
    }
    /// <summary>★要求は法の経路（2026-09-11）：FactionState に足した2配列（CrisisDemands=int[]／CrisisDemandPos=double[]）と
    /// 通知の札（World.PendingFactionCrisis＝運動への参照）が往復で残る＝「新フィールド型を足したら往復で確かめる」の番人（sbyte 事故の型）。</summary>
    [Fact]
    public void 法の経路_危機の記録と通知の札は往復で残る()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        sim1.StartWarmup();
        for (int q = 0; q < 4; q++) sim1.StepQuarter();
        var w1 = sim1.Nations[0].World;
        var f1 = new FactionState { Region = w1.Regions[0], Bloc = 2, Ideo = (int)Ideology.Communism, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0, Rejections = 3.0,
                                    CrisisDemands = [(int)PolicyAxis.Suffrage + 1, -((int)PolicyAxis.ShorterHours + 1)], CrisisDemandPos = [0.0, 1.0] };
        w1.Factions.Add(f1); w1.PendingFactionCrisis = f1; w1.PendingFactionCrisisDemands = "参政権の拡大・労働時間の短縮";
        w1.PendingRevoltDemand = -((int)PolicyAxis.CornLaws + 1); w1.PendingRevoltDemandPos = 0.5; w1.PendingRevoltRegion = w1.Regions[^1].Id;   // ★F1 譲歩は法の経路（同日）：World に足した記録（int/double）も往復で残る

        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var w2 = sim2.Nations[0].World;
        var f2 = w2.PendingFactionCrisis;
        Assert.NotNull(f2);
        Assert.Contains(f2, w2.Factions);                       // 札は運動リストの同じ実体を指す（参照の同一性＝ReferenceEquals で評価する経路の前提）
        Assert.Equal(f1.CrisisDemands, f2!.CrisisDemands);
        Assert.Equal(f1.CrisisDemandPos, f2.CrisisDemandPos);
        Assert.Equal(w1.PendingFactionCrisisDemands, w2.PendingFactionCrisisDemands);
        Assert.Same(w2.Regions[0], f2.Region);
        Assert.Equal(w1.PendingRevoltDemand, w2.PendingRevoltDemand);
        Assert.Equal(w1.PendingRevoltDemandPos, w2.PendingRevoltDemandPos);
        Assert.Equal(w1.PendingRevoltRegion, w2.PendingRevoltRegion);
    }
}
