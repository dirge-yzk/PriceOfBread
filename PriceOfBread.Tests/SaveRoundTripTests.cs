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

    /// <summary>★四半期の途中（45・75 日目）のセーブ（(1125)・(1309)）：保存→読み→両方を同じ日数続けて、基礎貨幣・総人口・全市場の値段と在庫の和が一致し、保存される欄の値の違いが 0 件（順だけの違いは数えない）
    /// （印が載らないと、読んだ側は月の途中の眠り・清算の記憶を失って軌道が分かれる）。</summary>
    [Fact]
    public void 四半期の途中のセーブで続きが一致する()
    {
        var sim1 = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        sim1.StartWarmup();
        for (int q = 0; q < 6; q++) sim1.StepQuarter();
        // (1309) 同じ A から 45 日と 75 日の 2 点で保存する（読んだ側 B45・B75 を同じ四半期の終わり＝180 日目まで続けて A と比べる）
        for (int d = 0; d < 45; d++) sim1.StepDay();
        var (asl45, fail45, par45) = SaveGame.MarkCounts(sim1);
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        for (int d = 45; d < 75; d++) sim1.StepDay();
        var (asl75, fail75, par75) = SaveGame.MarkCounts(sim1);
        using var ms75 = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms75);
        ms75.Position = 0;
        var sim3 = SaveGame.Load(ms75);
        // 対照（空振りの緑を防ぐ）：保存の点で、眠り・清算の失敗の印・清算の覚えが立っている市場がある（印が次の読み手より前に立っている）
        Assert.True(asl45 > 0 && fail45 > 0 && par45 > 0, $"45 日の点で印が立っていない（眠り {asl45}・失敗 {fail45}・覚え {par45}）");
        Assert.True(asl75 > 0 && fail75 > 0 && par75 > 0, $"75 日の点で印が立っていない（眠り {asl75}・失敗 {fail75}・覚え {par75}）");

        for (int d = 0; d < 105; d++) { sim1.StepDay(); sim3.StepDay(); }
        for (int d = 0; d < 30; d++) sim2.StepDay();
        for (int d = 0; d < 105; d++) sim2.StepDay();   // B45 は 45 日目から 180 日目まで（A は 75 日目から 105 日続けた）

        static double MarketSum(World w) { double s = 0; foreach (var r in w.Regions) foreach (var m in r.Markets) s += m.Price + m.Stock; return s; }
        foreach (var (sim, label) in new[] { (sim2, "45日"), (sim3, "75日") })
            for (int i = 0; i < sim1.Nations.Count; i++)
            {
                var a = sim1.Nations[i].World; var b = sim.Nations[i].World;
                Assert.True(a.BaseMoney.Value == b.BaseMoney.Value && a.TotalPopulation == b.TotalPopulation && MarketSum(a) == MarketSum(b),
                    $"{label}の点・{a.SelfPower?.Name}：継続系列と復元系列が食い違う（基礎貨幣 {a.BaseMoney:F4}⇔{b.BaseMoney:F4}／人口 {a.TotalPopulation:F2}⇔{b.TotalPopulation:F2}／値段と在庫の和 {MarketSum(a):R}⇔{MarketSum(b):R}）");
            }
        // (1309) 保存される欄を全て比べる（値の違い 0 件）。辞書・集合の列挙の順だけの違い（Diplomacy の Remove を受ける欄）は値の違いに数えない。
        foreach (var (sim, label) in new[] { (sim2, "45日"), (sim3, "75日") })
        {
            var sd = SaveGame.StateDiff(sim1, sim, 8);
            Assert.True(sd.Compared > 100000, $"{label}の点：比べた対象が少なすぎる（{sd.Compared}）");
            Assert.True(sd.Values == 0, $"{label}の点：値の違い {sd.Values} 件（順だけ {sd.OrderOnly}）：{string.Join(" ／ ", sd.Samples)}");
        }
    }

    /// <summary>(1309) 欄の比べ StateDiff の自己試験：同じ中身は値の違い 0・1 つの欄を変えれば値の違いに 1 件出る（比べの道具が空振りしない）。</summary>
    [Fact]
    public void 欄の比べは違いを拾う()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        Assert.Equal(0, SaveGame.StateDiff(sim1, sim2, 4).Values);
        var mk = sim2.Nations[0].World.Regions[0].Markets[0];
        mk.ReservationWage += 1.0;
        var sd = SaveGame.StateDiff(sim1, sim2, 4);
        Assert.Equal(1, sd.Values);
        Assert.Contains("ReservationWage", sd.Samples[0]);
        mk.ReservationWage -= 1.0;
        Assert.Equal(0, SaveGame.StateDiff(sim1, sim2, 4).Values);

        // 順だけの違い：同じ鍵と値で列挙の順が違う辞書（Remove の後の追加が空いた枠を使う）は、値の違いに数えず順だけに数える
        var fi = typeof(Diplomacy).GetField("_tensionRamp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var dA = (Dictionary<long, double>)fi.GetValue(sim1.Diplomacy)!; var dB = (Dictionary<long, double>)fi.GetValue(sim2.Diplomacy)!;
        dA.Clear(); dB.Clear();
        foreach (long k in new long[] { 1, 2, 3 }) dA[k] = k; dA.Remove(1); dA[4] = 4;   // 列挙の順 4,2,3
        foreach (long k in new long[] { 2, 3, 4 }) dB[k] = k;                            // 列挙の順 2,3,4
        Assert.False(dA.Keys.SequenceEqual(dB.Keys));
        var so = SaveGame.StateDiff(sim1, sim2, 4);
        Assert.Equal(0, so.Values);
        Assert.Equal(1, so.OrderOnly);
        dB[2] = 5.0;   // 値が違えば値の違い
        Assert.Equal(1, SaveGame.StateDiff(sim1, sim2, 4).Values);
    }

    /// <summary>(1309) 初期化子つきの [NonSerialized] の欄は、読み込みで初期化子が走らない（SaveGame の GetUninitializedObject 系）ので初期値が 0 に化ける。
    /// 許可表は理由つき（読み込み後に引き直される物だけ）。新しい物が増えたら赤＝理由を書くか、状態ならセーブに載せる。</summary>
    [Fact]
    public void 初期化子つきのNonSerializedは許可表の物だけ()
    {
        var found = new SortedDictionary<string, int>();
        string root = System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        foreach (var f in System.IO.Directory.EnumerateFiles(root, "*.cs", System.IO.SearchOption.AllDirectories))
        {
            if (f.Contains(System.IO.Path.DirectorySeparatorChar + "obj" + System.IO.Path.DirectorySeparatorChar) || f.Contains(System.IO.Path.DirectorySeparatorChar + "bin" + System.IO.Path.DirectorySeparatorChar)) continue;
            int n = 0;
            foreach (var line in System.IO.File.ReadAllLines(f))
            {
                if (line.TrimStart().StartsWith("//")) continue;
                int cut = line.IndexOf("//", System.StringComparison.Ordinal);
                string code = cut >= 0 ? line[..cut] : line;
                int at = code.IndexOf("NonSerialized", System.StringComparison.Ordinal);
                if (at < 0) continue;
                string rest = code[(at + "NonSerialized".Length)..];
                if (rest.Contains('=') && !rest.Contains("=>")) n++;
            }
            if (n > 0) found[System.IO.Path.GetFileName(f)] = n;
        }
        // 許可表：Model.cs＝Producer.DiscoveryRow の添字の覚え（_discRow・_retRow・_discGood）。読み込み後は _discRecipe が null＝RecipeName と別の参照になり、getter が必ず引き直す。
        Assert.Equal("Model.cs=1", string.Join(";", found.Select(kv => kv.Key + "=" + kv.Value)));
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
