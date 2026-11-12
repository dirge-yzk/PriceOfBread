using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1082) 不変条件の番人：構築した全ての州（開幕に昇格した植民地の州を含む）に文化と宗教の札がある。
/// 読むのは world.json の欄ではなく、構築した <see cref="World.Regions"/>（昇格した植民地の州は json に行が無く、首都の州の札を写す＝Simulation.PromoteColonyToRegion）。
/// 札が空の州は、中核の割合（Simulation の構築の CoreEthnicShare）が 1 のまま主流の文化の数えから外れ、宗教の一致（Diplomacy の保護の利害）でも飛ばされる＝少数の民族と宗教が機構に入らない。
/// 例外は名指しの許しだけ＝文化だけ（今ある名で説明できない文化・新しい名を作らない）。宗教の例外は 0。</summary>
public class RegionLabelInvariantTests
{
    /// <summary>文化を空のまま置いた州（国, 州, 理由）。宗教は埋めてある。</summary>
    internal static readonly (string Nation, string Region, string Why)[] AllowCultureEmpty =
    [
        ("日本", "蝦夷地", "アイヌ"),
        ("アメリカ", "セントポール", "ダコタ・オジブワ"),
        ("ロシア", "ウラジオストク", "ナナイ・ウデヘ"),
        ("ロシア", "ヤクーツク", "ヤクート"),
        ("ロシア", "オホーツク", "エヴェン"),
        ("ロシア", "ペトロパヴロフスク", "イテリメン・コリャーク"),
        ("ロシア", "スレドネコリムスク", "ユカギール・エヴェン"),
        ("ロシア", "アナディリ", "チュクチ"),
        ("ロシア", "トゥルハンスク", "エヴェンキ・ケット・ネネツ"),
        ("ロシア", "オブドルスク", "ネネツ・ハンティ"),
        ("ロシア", "グリエフ", "カザフ"),
        ("デンマーク", "レイキャヴィク", "アイスランド人"),
    ];

    static string NationOf(Simulation n) => n.World.SelfPower?.BaseName ?? "";

    /// <summary>構築した州を走査して、札の欠けた州を「国/州:文化」「国/州:宗教」の形で返す（許しの州の文化だけは数えない）。checkedRegions＝見た州の数。</summary>
    internal static List<string> UnlabeledRegions(MultiNationSim sim, out int checkedRegions)
    {
        var allow = new HashSet<(string, string)>(AllowCultureEmpty.Select(a => (a.Nation, a.Region)));
        var bad = new List<string>();
        checkedRegions = 0;
        foreach (var n in sim.Nations)
        {
            string nation = NationOf(n);
            foreach (var r in n.World.Regions)
            {
                checkedRegions++;
                if (string.IsNullOrEmpty(r.Culture) && !allow.Contains((nation, r.Name))) bad.Add($"{nation}/{r.Name}:文化");
                if (string.IsNullOrEmpty(r.Religion)) bad.Add($"{nation}/{r.Name}:宗教");
            }
        }
        return bad;
    }

    static MultiNationSim World61() => new(WorldData.Nations(), 1789, _ => new UtilityAgent());

    [Fact]
    public void 構築した全ての州に文化と宗教の札がある_例外は文化の名指しの許しだけ()
    {
        var sim = World61();
        var bad = UnlabeledRegions(sim, out int seen);
        int jsonProvinces = WorldData.Nations().Sum(c => c.Provinces.Length);
        int promoted = sim.Nations.Sum(n => n.World.Regions.Count(r => r.Tier != RegionTier.Core));
        Assert.True(sim.Nations.Count == 61, $"前提の破れ：国の数 {sim.Nations.Count}（61 のはず）");
        Assert.True(promoted > 0, "前提の破れ：開幕に昇格した植民地の州が 0＝json に行の無い州を見ていない");
        Assert.True(seen >= jsonProvinces + promoted, $"見た州 {seen} が json の州 {jsonProvinces}＋昇格 {promoted} より少ない");
        Assert.True(bad.Count == 0, $"札の欠けた州 {bad.Count}：{string.Join("・", bad)}");
    }

    [Fact]
    public void 名指しの許しの州は今も文化だけが空で宗教はある_古い許しを残さない()
    {
        var sim = World61();
        foreach (var (nation, region, why) in AllowCultureEmpty)
        {
            var r = sim.Nations.Where(n => NationOf(n) == nation).SelectMany(n => n.World.Regions).SingleOrDefault(x => x.Name == region);
            Assert.True(r is not null, $"許しの州 {nation}/{region}（{why}）が構築した世界に無い");
            Assert.True(r!.Culture.Length == 0, $"許しの州 {nation}/{region} に文化「{r.Culture}」がある＝許しの行を消す");
            Assert.True(r.Religion.Length > 0, $"許しの州 {nation}/{region} の宗教が空＝宗教の例外は置かない");
        }
    }

    [Fact]
    public void 番人は構築した州を読む_昇格した植民地の州の札を空にすると名指しで赤()
    {
        var sim = World61();
        var (nw, col) = sim.Nations.SelectMany(n => n.World.Regions.Where(r => r.Tier != RegionTier.Core).Select(r => (n, r))).First();
        string nation = NationOf(nw);
        Assert.DoesNotContain(WorldData.Nations().SelectMany(c => c.Provinces), p => p.City == col.Name);   // json に行の無い州（昇格した植民地）を選んだ
        Assert.Empty(UnlabeledRegions(sim, out _));
        col.Culture = ""; col.Religion = "";
        var bad = UnlabeledRegions(sim, out _);
        Assert.Equal(new[] { $"{nation}/{col.Name}:文化", $"{nation}/{col.Name}:宗教" }, bad);
    }
}
