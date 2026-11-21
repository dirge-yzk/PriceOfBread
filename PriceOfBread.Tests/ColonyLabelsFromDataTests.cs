using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1278) c131cr 旗 ColonyLabelsFromData の番人（事前登録 measurements/m1278_20261121_prereg_cr.txt）。
/// 旗 1：昇格した植民地の州（開幕の一括昇格・途中の接収の両方＝Simulation.PromoteColonyToRegion）の文化と宗教の札は places.json の土地のデータ。
/// データに無い名は空（首都の札へ逃がさない）。国の主流の札（Diplomacy.MajorityCultureReligion）は植民地格の州を数えない。
/// 期待値の札は字で書く（エンジンの読み口 Places.CultureOf から作らない）。</summary>
public class ColonyLabelsFromDataTests
{
    // 構築の昇格は渡した係数（sim.Coef）を、国の主流の札は国ごとの w.Coef を読む＝両方に置く（既定の値では同じ）
    private static MultiNationSim Build(double flag)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent(), new Coefficients { ColonyLabelsFromData = flag });
        foreach (var n in sim.Nations) n.World.Coef.ColonyLabelsFromData = flag;
        return sim;
    }

    private static World Nation(MultiNationSim sim, string name) => sim.Nations.First(n => n.World.SelfPower!.Name == name).World;

    // 字で書いた期待値（国・州・文化・宗教）＝首都の札と違う物だけ
    private static readonly (string Nation, string Region, string Culture, string Religion)[] Expected =
    [
        ("イギリス", "中核ベンガル", "ベンガル", "ヒンドゥー教"),
        ("イギリス", "カナダ", "フランス", "カトリック"),
        ("イギリス", "オーストラリア", "アボリジニ", "土着信仰"),
        ("ネーデルラント", "バタヴィア", "ジャワ", "イスラム"),
        ("ネーデルラント", "セイロン", "シンハラ", "上座部仏教"),
        ("スペイン", "中央メキシコ", "ナワ", "カトリック"),
        ("スペイン", "フィリピン", "タガログ", "カトリック"),
        ("フランス", "ポンディシェリ", "タミル", "ヒンドゥー教"),
        ("オマーン", "ザンジバル", "スワヒリ", "イスラム"),
        ("ポルトガル", "ルアンダ", "ムブンドゥ", "土着信仰"),
    ];

    // T1：旗 1 で構築した直後、字で書いた植民地格の州の札がデータの札（首都の写しではない）
    [Fact]
    public void T1_旗1で開幕に昇格した植民地格の州の札は土地のデータ()
    {
        var sim = Build(1.0);
        foreach (var (nation, region, culture, religion) in Expected)
        {
            var w = Nation(sim, nation);
            var r = w.Regions.SingleOrDefault(x => x.Name == region);
            Assert.True(r is not null, $"前提が崩れた：{nation}/{region} が無い");
            Assert.Equal(RegionTier.Colonial, r!.Tier);
            Assert.True(r.Culture == culture && r.Religion == religion, $"{nation}/{region} の札 {r.Culture}/{r.Religion}（期待 {culture}/{religion}・首都 {w.Region.Culture}/{w.Region.Religion}）");
            Assert.True(r.Culture != w.Region.Culture || r.Religion != w.Region.Religion, $"前提が崩れた：{nation}/{region} の期待の札が首都と同じ");
        }
    }

    // T1b：旗 1 で植民地格の全ての州の札が空でない（データの欠け＝空の札を構築した世界で拾う）。首都と違う札の州の数の床（全てを首都へ寄せる誤りを拾う）
    [Fact]
    public void T1b_旗1で植民地格の全ての州に札があり多くは首都と違う()
    {
        var sim = Build(1.0);
        int seen = 0, differ = 0; var bad = new List<string>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions.Where(x => x.Tier == RegionTier.Colonial))
            {
                seen++;
                if (r.Culture.Length == 0 || r.Religion.Length == 0) bad.Add($"{n.World.SelfPower!.Name}/{r.Name}");
                if (r.Culture != n.World.Region.Culture) differ++;
            }
        Assert.True(seen >= 75, $"前提が崩れた：植民地格の州 {seen}（開幕の 81 名から）");
        Assert.True(bad.Count == 0, $"札の空の植民地格の州 {bad.Count}：{string.Join("・", bad)}");
        Assert.True(differ >= 50, $"首都と違う文化の札の植民地格の州 {differ}／{seen}（床 50）");
    }

    // T1c：旗 0 は旧の規則のまま（首都の州の札の写し）
    [Fact]
    public void T1c_旗0では植民地格の州の札は首都の写し()
    {
        var sim = Build(0.0);
        int seen = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions.Where(x => x.Tier == RegionTier.Colonial))
            {
                seen++;
                Assert.True(r.Culture == n.World.Region.Culture && r.Religion == n.World.Region.Religion, $"{r.Name} の札 {r.Culture}/{r.Religion}＝首都 {n.World.Region.Culture}/{n.World.Region.Religion} でない");
            }
        Assert.True(seen >= 75, $"前提が崩れた：植民地格の州 {seen}");
    }

    // T2：不変条件＝world.json の植民地の名の全て（initialColonies と abstractTargets の colonies）に文化と宗教のデータがある
    [Fact]
    public void T2_植民地の名の全てに文化と宗教のデータがある()
    {
        var names = new HashSet<string>();
        foreach (var cfg in WorldData.Nations()) foreach (var c in cfg.InitialColonies) names.Add(c.Name);
        foreach (var t in WorldData.Setup().Targets) foreach (var c in t.Colonies) names.Add(c.Name);
        Assert.True(names.Count >= 110, $"前提が崩れた：植民地の名 {names.Count}（110 のはず）");
        var bad = names.Where(n => Places.CultureOf(n) is null || Places.ReligionOf(n) is null).OrderBy(n => n, System.StringComparer.Ordinal).ToList();
        Assert.True(bad.Count == 0, $"文化か宗教のデータの無い植民地の名 {bad.Count}：{string.Join("・", bad)}");
    }

    // T3：途中の接収の道（同じ関数）も旗 1 でデータの札・データに無い名は空（首都へ逃がさない）
    [Fact]
    public void T3_旗1で途中の昇格もデータの札で無い名は空()
    {
        var sim = Build(1.0);
        var w = Nation(sim, "イギリス");
        var reg = Simulation.PromoteColonyToRegion(w, sim.Coef, new Colony { Name = "キサンガニ", Resources = [], PopSize = 10, Population = 1.0 }, RegionTier.Colonial);
        Assert.Equal("モンゴ", reg.Culture);
        Assert.Equal("土着信仰", reg.Religion);
        var none = Simulation.PromoteColonyToRegion(w, sim.Coef, new Colony { Name = "データの無い植民地", Resources = [], PopSize = 10, Population = 1.0 }, RegionTier.Colonial);
        Assert.Equal("", none.Culture);
        Assert.Equal("", none.Religion);
    }

    // T4：旗 1 で国の主流の札は植民地格の州を数えない（英はベンガル 2 州の人口が本国の英語の州より多い・蘭はジャワが本国より多い）
    [Fact]
    public void T4_旗1で国の主流の札は植民地の人口で入れ替わらない()
    {
        var sim = Build(1.0);
        var uk = Nation(sim, "イギリス");
        double bengal = uk.Regions.Where(r => r.Culture == "ベンガル").Sum(r => r.SumPopulation());
        double england = uk.Regions.Where(r => r.Culture == "イングランド" && r.Tier != RegionTier.Colonial).Sum(r => r.SumPopulation());
        Assert.True(bengal > england, $"前提が崩れた：ベンガルの札の人口 {bengal:F0} が本国のイングランドの札 {england:F0} 以下");
        Assert.Equal(("イングランド", "プロテスタント"), Diplomacy.MajorityCultureReligion(uk));
        var nl = Nation(sim, "ネーデルラント");
        Assert.Equal(("オランダ", "プロテスタント"), Diplomacy.MajorityCultureReligion(nl));
    }

    // T4b：数えないのは植民地格だけ（同じ州を統合格へ置くと数える＝札で選ぶ形・全てを数えない形を拾う）
    [Fact]
    public void T4b_統合格の州は国の主流の札に数える()
    {
        var sim = Build(1.0);
        var uk = Nation(sim, "イギリス");
        var core = uk.Regions.Single(r => r.Name == "中核ベンガル");
        core.Tier = RegionTier.Integrated;
        Assert.Equal(("ベンガル", "ヒンドゥー教"), Diplomacy.MajorityCultureReligion(uk));
    }
}
