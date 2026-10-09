using System.Linq;
using System.Text.Json;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 識字と制度（初期値の整備 段7・実装ログ (232)）の番人。階級の識字を「国の基準1本×階級のオッズの比（全国共通）」から導く
/// （Simulation.ClassLiteracy）＝旧の国ごとの7項目（nobLit ほか）と民族統一度 ethnicUnity をデータから外した。
/// <para>⚠式（導く側）と世界（作る側）の両方を見る＝式だけ緑で BuildProvince が使っていない、を許さない。</para>
/// </summary>
public class LiteracyTests
{
    private static JsonDocument WorldJson()
    {
        var asm = typeof(WorldData).Assembly;
        string name = System.Array.Find(asm.GetManifestResourceNames(), x => x.EndsWith("world.json", System.StringComparison.Ordinal))!;
        return JsonDocument.Parse(asm.GetManifestResourceStream(name)!);
    }

    private static double[] Weights(NationConfig c)
    {
        double noble = c.Nobility * (1 - c.ClergyFrac);
        return [noble, c.Nobility * c.ClergyFrac, c.Peasant, c.Worker, c.Bourgeois,
                c.Professional * (1 - c.IntelligentsiaFrac), c.Professional * c.IntelligentsiaFrac, c.Merchant, c.Artisan, noble + c.Bourgeois];
    }

    private static double Mean(NationConfig c, double[] lit)
    {
        var w = Weights(c);
        return Enumerable.Range(0, w.Length).Sum(i => w[i] * lit[i]) / w.Sum();
    }

    [Fact]
    public void 回232_階級の識字の人口加重は全ての国で国の基準に一致する()
    {
        var coef = new Coefficients();
        var nations = WorldData.Nations();
        Assert.True(nations.Length >= 61);
        foreach (var c in nations)
        {
            var lit = Simulation.ClassLiteracy(c, coef);
            Assert.True(c.Literacy > 0 && c.Literacy < 1, $"{c.Name} の基準 {c.Literacy}");
            Assert.Equal(c.Literacy, Mean(c, lit), 9);
            Assert.All(lit, v => Assert.InRange(v, 0.0, 1.0));
        }
    }

    [Fact]
    public void 回232_尺度1はテンプレそのもの_階級の順序と隔たりは全国共通()
    {
        var coef = new Coefficients();
        var c = new NationConfig { Nobility = 500, Peasant = 18000, Worker = 3000, Bourgeois = 1000, Professional = 600 };
        double[] template = [0.85, 0.80, 0.08, 0.25, 0.60, 0.80, 0.85, 0.70, 0.45, 0.30];
        var w = Weights(c);
        c.Literacy = Enumerable.Range(0, w.Length).Sum(i => w[i] * template[i]) / w.Sum();
        var lit = Simulation.ClassLiteracy(c, coef);
        for (int i = 0; i < template.Length; i++) Assert.Equal(template[i], lit[i], 6);   // 基準がテンプレの人口加重なら z=1＝旧の値と同じ
        // 基準が上がれば全ての階級が上がる（単調）・順序（貴族＞農民など）は保たれる
        double[]? prev = null;
        foreach (double L in new[] { 0.01, 0.05, 0.2, 0.5, 0.8, 0.95 })
        {
            c.Literacy = L;
            var x = Simulation.ClassLiteracy(c, coef);
            Assert.True(x[0] > x[2] && x[4] > x[3] && x[3] > x[2], $"L={L} で階級の順序が崩れた");
            if (prev is not null) for (int i = 0; i < x.Length; i++) Assert.True(x[i] > prev[i], $"L={L} で階級 {i} が上がらない");
            prev = x;
        }
    }

    [Fact]
    public void 回232_上限の切り落としは無く_高識字の国でも1に張り付かない()
    {
        var coef = new Coefficients();
        var c = new NationConfig { Nobility = 500, Peasant = 18000, Worker = 3000, Bourgeois = 1000, Professional = 600, Literacy = 0.9 };
        var lit = Simulation.ClassLiteracy(c, coef);
        Assert.All(lit, v => Assert.True(v < 1.0));                    // 漸近する（切り落としでない）
        Assert.True(lit[2] > 0.8, $"農民 {lit[2]}");                    // 高識字の国は農民も上がる（旧の農民だけ手で置く形では起きない）
        Assert.Equal(0.9, Mean(c, lit), 9);
    }

    [Fact]
    public void 回232_基準が範囲の外やNaNなら全階級が0か1_NaNを出さない()
    {
        var coef = new Coefficients();
        var c = new NationConfig { Nobility = 500, Peasant = 18000, Worker = 3000, Bourgeois = 1000, Professional = 600 };
        foreach (double bad in new[] { double.NaN, 0.0, -1.0 })
        {
            c.Literacy = bad;
            Assert.All(Simulation.ClassLiteracy(c, coef), v => Assert.Equal(0.0, v));
        }
        c.Literacy = 1.0;
        Assert.All(Simulation.ClassLiteracy(c, coef), v => Assert.Equal(1.0, v));
    }

    [Fact]
    public void 回232_作った世界の本国の識字の人口加重は国の基準に一致する_BuildProvinceが式を使っている()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        var byName = WorldData.Nations().ToDictionary(c => c.Name);
        int checkedN = 0;
        foreach (var n in sim.Nations)
        {
            string nm = n.World.SelfPower?.BaseName ?? "";
            if (!byName.TryGetValue(nm, out var cfg)) continue;
            double avg = PoliticalQuantities.AvgLiteracy(n.World, coreOnly: true);
            Assert.Equal(cfg.Literacy, avg, 6);
            checkedN++;
        }
        Assert.True(checkedN >= 61, $"照合できた国 {checkedN}");
    }

    [Fact]
    public void 回232_world_jsonの国の欄から_識字の7項目と民族統一度を外した()
    {
        using var doc = WorldJson();
        var nations = doc.RootElement.GetProperty("nations");
        Assert.True(nations.GetArrayLength() >= 61);
        foreach (var n in nations.EnumerateArray())
        {
            foreach (var k in new[] { "nobLit", "peasLit", "workLit", "bourgLit", "profLit", "clergyLit", "intelLit", "ethnicUnity" })
                Assert.False(n.TryGetProperty(k, out _), $"{n.GetProperty("name").GetString()} に旧の欄 {k} が残っている（識字は literacy 1本・民族は州のラベル）");
            Assert.True(n.TryGetProperty("literacy", out var lv) && lv.GetDouble() > 0 && lv.GetDouble() < 1, "literacy が無い");
        }
        // NationConfig からも外れている（読み手の無い欄を残さない）
        foreach (var f in new[] { "NobLit", "PeasLit", "WorkLit", "BourgLit", "ProfLit", "ClergyLit", "IntelLit", "EthnicUnity" })
            Assert.Null(typeof(NationConfig).GetField(f));
    }

    [Fact]
    public void 回232_法の区分_スウェーデンの農奴制は無く割当兵制_墺は1781年に人格的な農奴制を廃止_露の私有農奴は5割強()
    {
        var by = WorldData.Nations().ToDictionary(c => c.Name);
        Assert.Equal(ReformLevel.Full, by["スウェーデン"].InitialSerfEmancipation);          // 農奴制は無かった
        Assert.Equal(ConscriptionLaw.Selective, by["スウェーデン"].InitialConscription);     // 割当兵制（indelningsverket）
        Assert.InRange(by["スウェーデン"].FarmLordShare, 0.30, 0.36);                        // 貴族の免税地 約33%（Karl XI の大減封の後）
        Assert.Equal(ReformLevel.Limited, by["オーストリア"].InitialSerfEmancipation);       // 1781 農奴制廃止令（賦役は残る）
        Assert.InRange(by["ロシア"].FarmLordShare, 0.5, 0.58);                               // 私有農奴 約54%（1796）・国有地農民は地主を持たない
    }
}
