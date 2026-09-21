using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 開始前の助走（作者裁定2026-08-10「助走をプレイヤーの世界と自己対戦バッチで統一し、延長する」）の番人。
/// <para>守るべき性質は3つ：</para>
/// <para>①**暦が1四半期も進まない**＝助走を1年にしようが10年にしようが開始は 1783/Q1・Tick0 のまま。
///   ここが崩れると 1914年の忠実度参照断面（Q524）・604四半期の全長・技術の年ゲート（TechOnset）が丸ごとずれる
///   （2026-08-10以前は実際にずれていた＝助走4四半期でエンジン暦だけ1784年になり、表示暦1783と1年の乖離があった）。</para>
/// <para>②**時間の経過そのものである量を伴わせない**＝人口・識字は助走の長さに依存しない
///   （「10年ぶんの人口成長が時間経過なしに起きる」不整合を作らない）。</para>
/// <para>③**入口が1本**＝App も自己対戦バッチも <see cref="MultiNationSim.StartWarmup"/> だけを通り、同じ盤面から始まる。</para>
/// <para>⚠これは ON 経路のテスト（助走を実際に回して結果を見る）＝「何もしない機構は必ず緑」の轍を踏まない。</para>
/// </summary>
public class StartWarmupTests
{
    private static MultiNationSim Build(ulong seed = 1789UL)
        => new(WorldData.Nations(), seed, _ => new UtilityAgent());

    [Theory]
    [InlineData(4)]    // 既定＝1年
    [InlineData(20)]   // 5年
    [InlineData(40)]   // 10年
    public void 助走は暦を一切進めない(int quarters)
    {
        var sim = Build();
        sim.StartWarmup(null, quarters);
        foreach (var n in sim.Nations)
        {
            Assert.Equal(Calendar.StartYear, n.World.Year);   // 1783年のまま
            Assert.Equal(1, n.World.Quarter);                 // Q1のまま
            Assert.Equal(0L, n.World.Tick);                   // 経過日数0のまま
        }
    }

    [Fact]
    public void 助走の長さを変えても人口と識字は動かない()
    {
        // 助走で動いてよいのは価格・在庫・生産・財政＝実勢。人口と識字は「年月が経って積み上がる量」なので
        // 助走の長さに依存してはならない（依存したら、10年の助走で1793年の人口の国が1783年として始まる）。
        static (double Pop, double Lit) Board(int quarters)
        {
            var sim = Build();
            if (quarters > 0) sim.StartWarmup(null, quarters);
            double pop = 0, lit = 0;
            foreach (var n in sim.Nations)
            {
                pop += n.World.TotalPopulation;
                foreach (var r in n.World.Regions)
                    foreach (var p in r.Pops) lit += p.Literacy * p.Size;
            }
            return (pop, lit);
        }
        var w4 = Board(4);
        var w20 = Board(20);
        var w40 = Board(40);
        Assert.Equal(w4.Pop, w20.Pop, 6);
        Assert.Equal(w4.Pop, w40.Pop, 6);
        Assert.Equal(w4.Lit, w20.Lit, 6);
        Assert.Equal(w4.Lit, w40.Lit, 6);
    }

    [Fact]
    public void 助走は資本を一切育てない()
    {
        // ★作者裁定2026-08-10 追加分「価格のみ試走することはできない？　問題は開始時の振動なので」。
        //   助走を延ばすと開始工業化が 0.31→1.5→2.3 と膨らんでいた＝価格が均衡から遠いせいで見かけの利潤が巨大化し
        //   投資が暴走した産物＝**取り除きたい振動そのものを開始盤面に焼き込んでいた**。よって助走中は資本形成を凍結する。
        //   物差しは「助走なし」の盤面と**厳密に同じ**であること（増えないだけでなく、減価で痩せもしない＝凍結は両側）。
        static (double Ind, double Built, double Grid) Capital(int quarters)
        {
            var sim = Build();
            if (quarters > 0) sim.StartWarmup(null, quarters);
            double ind = 0, built = 0, grid = 0;
            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    ind += r.TotalIndustrialization; grid += r.GridCapacity;
                    foreach (var p in r.Producers) built += p.BuiltScale;
                }
            return (ind, built, grid);
        }
        var bareCap = Capital(0);
        foreach (int q in (int[])[4, 20, 40])
        {
            var w = Capital(q);
            Assert.Equal(bareCap.Ind, w.Ind, 6);      // 工業化度（新旧ヴィンテージ合計）
            Assert.Equal(bareCap.Built, w.Built, 6);  // 建った工場・作付面積の恒久容量
            Assert.Equal(bareCap.Grid, w.Grid, 6);    // 送電容量
        }
    }

    [Fact]
    public void 助走は建材の需要を殺さない()
    {
        // 資本形成を止めるときに「建てる需要と支払い」まで止めると、鋼・機械・セメント・製材の4財だけ需要が抜けて
        // 均衡が下振れし、助走が**間違った価格**へ収束する（作者の懸念）。需要が生きている＝価格が床へ落ちないことで確かめる。
        var matCoef = new Coefficients();
        var sim = Build();
        sim.StartWarmup(null, 20);
        foreach (int gid in (int[])[matCoef.SteelGoodId, matCoef.MachineGoodId, matCoef.CementGoodId, matCoef.LumberGoodId])
        {
            int alive = 0;
            foreach (var n in sim.Nations)
            {
                var r = n.World.Region;
                if (gid >= r.Markets.Length) continue;
                double rp = 0; foreach (var g in r.Goods) if (g.Id == gid) { rp = g.RefPrice; break; }
                if (rp > 1e-9 && r.Markets[gid].Price / rp > 0.3) alive++;   // 参照価格の3割超＝需要が生きている
            }
            Assert.True(alive > sim.Nations.Count / 2,
                $"{Catalog.Goods[gid].Name}の建設需要が助走で消えている（{alive}/{sim.Nations.Count}カ国のみ生存）");
        }
    }

    [Fact]
    public void 助走ゼロは素の構築直後と厳密恒等()
    {
        // 係数を0にしたら助走は丸ごと走らない（技術の助走も基準線の取り直しもしない）＝
        // 2026-08-10以前のバッチへ戻れる逃げ道。帯が動いたときの二分探索の基線なので、恒等でなければ意味がない。
        var bare = Build();
        var warmed = Build();
        warmed.StartWarmup(null, 0);
        for (int i = 0; i < bare.Nations.Count; i++)
        {
            var a = bare.Nations[i].World; var b = warmed.Nations[i].World;
            Assert.Equal(a.Year, b.Year);
            Assert.Equal(a.Quarter, b.Quarter);
            Assert.Equal(a.Tick, b.Tick);
            Assert.Equal(a.TotalPopulation, b.TotalPopulation, 9);
            Assert.Equal(a.TechLevel, b.TechLevel, 12);
            for (int g = 0; g < a.Region.Markets.Length; g++)
                Assert.Equal(a.Region.Markets[g].Price, b.Region.Markets[g].Price, 12);
        }
        // 助走ゼロの世界を本編で走らせても、素の世界とビット単位で同じ軌道を描く（凍結フラグが状態を汚していない）。
        for (int q = 0; q < 4; q++) { bare.StepQuarter(); warmed.StepQuarter(); }
        for (int i = 0; i < bare.Nations.Count; i++)
        {
            Assert.Equal(bare.Nations[i].World.Gdp, warmed.Nations[i].World.Gdp, 12);
            Assert.Equal(bare.Nations[i].World.TotalPopulation, warmed.Nations[i].World.TotalPopulation, 9);
        }
    }

    [Fact]
    public void 助走は二度目の呼び出しで何もしない()
    {
        // 入口が1本である以上、誤って2度呼ばれても盤面が二重に助走してはならない（ロード経路・診断からの再入）。
        var once = Build();
        once.StartWarmup();
        var twice = Build();
        twice.StartWarmup();
        twice.StartWarmup();
        for (int i = 0; i < once.Nations.Count; i++)
        {
            var a = once.Nations[i].World; var b = twice.Nations[i].World;
            for (int g = 0; g < a.Region.Markets.Length; g++)
                Assert.Equal(a.Region.Markets[g].Price, b.Region.Markets[g].Price, 12);
        }
    }

    [Fact]
    public void 助走は決定論的で助走後の本編も再現する()
    {
        // 同じ種なら助走→本編まで完全に一致する（助走で凍結/解凍するフラグが状態を汚していない証拠）。
        static double[] Run(ulong seed)
        {
            var sim = Build(seed);
            sim.StartWarmup();
            for (int q = 0; q < 8; q++) sim.StepQuarter();
            return [.. sim.Nations.Select(n => n.World.Gdp)];
        }
        Assert.Equal(Run(7UL), Run(7UL));
    }

    [Fact]
    public void 助走の後も政治と外交の凍結は解けている()
    {
        // 助走は PoliticsEnabled と DiplomacyFrozen を一時的に触る＝必ず元へ戻す（戻し忘れると本編の政治が死ぬ）。
        var sim = Build();
        var before = sim.Nations.Select(n => n.World.PoliticsEnabled).ToArray();
        sim.StartWarmup(null, 20);
        Assert.False(sim.DiplomacyFrozen);
        for (int i = 0; i < sim.Nations.Count; i++)
            Assert.Equal(before[i], sim.Nations[i].World.PoliticsEnabled);
        foreach (var n in sim.Nations) Assert.False(n.World.WarmupFrozen);
    }

    [Fact]
    public void 助走は価格を実勢へ寄せる()
    {
        // 助走の目的そのもの＝開幕直後の価格の荒れが、助走なしより小さいこと。
        // 物差し＝首都市場の実勢財について、本編8四半期のあいだの |ln(価格の四半期変化)| の平均。
        static double Turbulence(int warm)
        {
            var sim = Build();
            if (warm > 0) sim.StartWarmup(null, warm);
            var r = sim.Nations[0].World.Region;
            var ids = r.Goods.Where(g => g.Tier != GoodTier.Service && r.Markets[g.Id].Price > 1e-9)
                             .Select(g => g.Id).ToArray();
            var prev = ids.Select(g => r.Markets[g].Price).ToArray();
            double sum = 0; int n = 0;
            for (int q = 0; q < 8; q++)
            {
                sim.StepQuarter();
                for (int k = 0; k < ids.Length; k++)
                {
                    double now = r.Markets[ids[k]].Price;
                    if (now > 1e-9 && prev[k] > 1e-9) { sum += System.Math.Abs(System.Math.Log(now / prev[k])); n++; }
                    prev[k] = now;
                }
            }
            return n > 0 ? sum / n : 0;
        }
        Assert.True(Turbulence(4) < Turbulence(0), "助走は開幕の価格の荒れを減らすはず");
    }
}
