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

    /// <summary>★眠りの印はセーブに載る（(1125)・作者 2026-11-13 のマルコフ性の洗い出し・作者「全て推奨で」で推奨を採った）：国内価格の月の残りの眠り（Market.FlowAsleep）・清算の成約しなかった行の印
    /// （Market.ClearFailLp）・成約の相手の州（Market.ClearPartners）は作業領域でなく状態＝四半期の途中のセーブでも往復で残る（旧は〈載らない〉を固定していた＝守る向きの反転）。
    /// ⚠突然変異「[NonSerialized] を戻す」で赤。</summary>
    [Fact]
    public void 眠りの印はセーブに載る()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var m1 = sim1.Nations[0].World.Regions[0].Markets[0];
        m1.FlowAsleep = true; m1.ClearFailLp = 5.0;
        m1.ClearPartners = [sim1.Nations[1].World.Regions[0], sim1.Nations[0].World.Regions[0]];
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var m2 = sim2.Nations[0].World.Regions[0].Markets[0];
        Assert.True(m2.FlowAsleep);
        Assert.Equal(5.0, m2.ClearFailLp);
        Assert.NotNull(m2.ClearPartners);
        Assert.Equal(2, m2.ClearPartners!.Length);
        Assert.Same(sim2.Nations[1].World.Regions[0], m2.ClearPartners[0]);
        Assert.Same(sim2.Nations[0].World.Regions[0], m2.ClearPartners[1]);
    }

    /// <summary>★四半期の途中（45 日目）のセーブ（(1125)）：保存→読み→両方を同じ日数続けて、基礎貨幣・総人口・全市場の値段と在庫の和が一致する（バイト列は印の外の 2 件の差があり比べない）
    /// （印が載らないと、読んだ側は月の途中の眠り・清算の記憶を失って軌道が分かれる）。</summary>
    [Fact]
    public void 四半期の途中のセーブで続きが一致する()
    {
        var sim1 = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        sim1.StartWarmup();
        for (int q = 0; q < 6; q++) sim1.StepQuarter();
        for (int d = 0; d < 45; d++) sim1.StepDay();

        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);

        for (int d = 0; d < 135; d++) { sim1.StepDay(); sim2.StepDay(); }

        static double MarketSum(World w) { double s = 0; foreach (var r in w.Regions) foreach (var m in r.Markets) s += m.Price + m.Stock; return s; }
        for (int i = 0; i < sim1.Nations.Count; i++)
        {
            var a = sim1.Nations[i].World; var b = sim2.Nations[i].World;
            Assert.True(a.BaseMoney.Value == b.BaseMoney.Value && a.TotalPopulation == b.TotalPopulation && MarketSum(a) == MarketSum(b),
                $"{a.SelfPower?.Name}：継続系列と復元系列が食い違う（基礎貨幣 {a.BaseMoney:F4}⇔{b.BaseMoney:F4}／人口 {a.TotalPopulation:F2}⇔{b.TotalPopulation:F2}／値段と在庫の和 {MarketSum(a):R}⇔{MarketSum(b):R}）");
        }
        // バイト列の一致は比べない：印の外の差が 2 件ある（辞書 Diplomacy._tensionRamp の列挙順と InternationalMarket.LastPairWasLand の走り書き・measurements/m1125_20261114/byte_diff.txt）＝量だけ比べる。
    }
    /// <summary>(1165) E16 旗 RuleFromPower＝1 の四半期の途中（45 日目・75 日目）のセーブ：保存→読み→同じ日まで続けて、全ての国の全ての州の 4 区分・外国の名・外国の取り分がビットで一致する
    /// （支配の力の表〈距離の表を含む〉はセーブに載らない＝M1 で四半期の末の清算から作ってすぐ使う。表を第 1 月末に作って最終日まで持ち越すと、途中で読んだ側は大圏へ落ちて分かれる）。</summary>
    [Fact]
    public void E16_旗1の四半期の途中のセーブで支配の取り分が一致する()
    {
        static MultiNationSim New()
        {
            var coef = new Coefficients { RuleFromPower = 1.0 };
            var s = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent(), coef);
            foreach (var n in s.Nations) n.World.Coef.RuleFromPower = 1.0;
            return s;
        }
        static MultiNationSim Copy(MultiNationSim s)
        {
            using var ms = new System.IO.MemoryStream();
            SaveGame.Save(s, ms); ms.Position = 0;
            return SaveGame.Load(ms);
        }
        var sim1 = New();
        sim1.StartWarmup();
        for (int q = 0; q < 6; q++) sim1.StepQuarter();
        for (int d = 0; d < 45; d++) sim1.StepDay();
        var sim2 = Copy(sim1);
        for (int d = 0; d < 30; d++) { sim1.StepDay(); sim2.StepDay(); }
        var sim3 = Copy(sim1);
        for (int d = 0; d < 105; d++) { sim1.StepDay(); sim2.StepDay(); sim3.StepDay(); }
        int regions = 0, foreignRows = 0;
        foreach (var other in new[] { sim2, sim3 })
        {
            Assert.Equal(sim1.Nations.Count, other.Nations.Count);
            for (int i = 0; i < sim1.Nations.Count; i++)
            {
                var a = sim1.Nations[i].World; var b = other.Nations[i].World;
                Assert.Equal(a.Regions.Count, b.Regions.Count);
                for (int k = 0; k < a.Regions.Count; k++)
                {
                    var ra = a.Regions[k]; var rb = b.Regions[k];
                    regions++;
                    Assert.True(ra.Name == rb.Name && ra.Rule.Length == rb.Rule.Length, $"{ra.Name}：州の並びか 4 区分の長さが食い違う");
                    for (int j = 0; j < ra.Rule.Length; j++)
                        Assert.True(System.BitConverter.DoubleToInt64Bits(ra.Rule[j]) == System.BitConverter.DoubleToInt64Bits(rb.Rule[j]), $"{ra.Name}：Rule[{j}] {ra.Rule[j]:R}⇔{rb.Rule[j]:R}");
                    Assert.Equal(ra.ForeignRulers, rb.ForeignRulers);
                    Assert.Equal(ra.ForeignShares.Length, rb.ForeignShares.Length);
                    for (int j = 0; j < ra.ForeignShares.Length; j++)
                    {
                        if (ra.ForeignShares[j] > 0) foreignRows++;
                        Assert.True(System.BitConverter.DoubleToInt64Bits(ra.ForeignShares[j]) == System.BitConverter.DoubleToInt64Bits(rb.ForeignShares[j]), $"{ra.Name}：ForeignShares[{j}] {ra.ForeignShares[j]:R}⇔{rb.ForeignShares[j]:R}");
                    }
                }
            }
        }
        Assert.True(regions >= 600, $"比べた州 {regions}（≧600＝全ての国の州〈約 300〉を 2 回）");   // 対照の件数の下限
        Assert.True(foreignRows >= 10, $"外国の取り分＞0 の行 {foreignRows}（≧10＝外国の取り分を持つ州を比べた）");
    }
    /// <summary>★年齢の層の数はセーブに書く（(1125)）：読む時に今の層の数と食い違えば例外（POB_AGE_BANDS が保存時と違うプロセスで読むと配列の長さが合わず静かに壊れる）。</summary>
    [Fact]
    public void 層の数が食い違うセーブは読めない()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        var bytes = ms.ToArray();
        int at = 1 + System.Text.Encoding.UTF8.GetByteCount("POBSIM2");   // BinaryWriter.Write(string)＝長さ 1 バイト＋UTF-8。直後の int32 が層の数
        Assert.Equal(Pop.Layout.Bands, System.BitConverter.ToInt32(bytes, at));
        System.BitConverter.GetBytes(Pop.Layout.Bands == 8 ? 11 : 8).CopyTo(bytes, at);
        Assert.Throws<System.IO.InvalidDataException>(() => SaveGame.Load(new System.IO.MemoryStream(bytes)));
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
