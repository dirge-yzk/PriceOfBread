using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1305) c135k47 灯油の近代レシピの重なりの削除（K47・S01-H019）の機構の試験。
/// 同じ史実（ケロシンが鯨油を駆逐した）を「石油ケロシン」（原油 0.5）と「石油灯油」（原油 1.0）の 2 本で表していた＝思想⑨の重なり。
/// (1311) c136kb で焼き込んだ（旗 LampOilPetroleumDupOff と「石油灯油」のレシピを消した）＝近代の灯油のレシピは「石油ケロシン」の 1 本だけ。
/// 盤面は試験の中で置く（英の 1 国の世界）。期待は Simulation の式から作らず、手で書いた名前と量（原油 0.5）で書く。
/// 突然変異（記録＝measurements/m1305_20261122_mutation.txt・measurements/m1311_20261123/mut.txt）：ケロシンの方を消す・「石油灯油」を戻す → 赤。</summary>
public class LampOilDuplicateRecipeTests
{
    private static World Board()
        => new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent()).World;

    private static double CrudeQty(Producer p)
    {
        var l = p.Inputs.Where(i => i.GoodId == Catalog.CrudeOilId).ToList();
        Assert.True(l.Count == 1, $"レシピ {p.RecipeName} の原油の投入が {l.Count} 本（1 本のはず）");
        return l[0].QtyPerOutput;
    }

    [Fact]
    public void 灯油の近代レシピは石油ケロシンの1本だけで原油は0_5()
    {
        var w = Board();
        int regions = 0;
        foreach (var r in w.Regions)
        {
            var lamp = r.Producers.Where(p => p.GoodId == Catalog.LampOilId).ToList();
            if (!lamp.Any(p => p.RecipeName.Length == 0)) continue;   // 既定（鯨油・魚油）の行がある州だけ＝石油ケロシンが建つ州
            regions++;
            var modern = lamp.Where(p => p.RecipeName.Length > 0).Select(p => p.RecipeName).ToList();
            Assert.True(modern.Count == 1 && modern[0] == "石油ケロシン", $"{r.Name} の灯油の近代レシピ＝[{string.Join(",", modern)}]（石油ケロシンの 1 本のはず）");
            Assert.Equal(0.5, CrudeQty(lamp.Single(p => p.RecipeName == "石油ケロシン")));
        }
        Assert.True(regions >= 3, $"灯油の既定の行がある州が {regions}（盤面の前提が崩れた）");   // 対照の件数
        Assert.DoesNotContain(w.Regions.SelectMany(r => r.Producers), p => p.RecipeName == "石油灯油");   // 既定の行の無い州にも建たない
    }
}
