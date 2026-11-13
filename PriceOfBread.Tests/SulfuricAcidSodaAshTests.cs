using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(493) 化学薬品を〈硫酸〉(33) と〈ソーダ灰〉(128) に割った（作者 2026-10-21〜23「「硫酸」と「ソーダ灰」は2つに分けましょう。」・材の粒度の最低ライン＝
/// 酸の材料からアルカリが生産されてはならない）の番人（事前登録 measurements/gd_20261023_prereg.txt の [4]）。機構と不変条件だけを見る
/// （ルブラン法とソルベー法が開く・市場が選ぶ・値段の年代は成績表と計器 chem_s が見る）。
/// ・期待値は本体の定数・新しい goods.json から作らない：①は旧の goods.json（8c8f987a）から作った参照価格の表・③は精査の docs §5-1 から手で書いた使い手の表。
/// ・費用の大小は参照価格（<c>Catalog.RefPrices</c>）での単位費用の大小の順だけを確かめる（値のコピーにしない）。
/// ⚠突然変異（それぞれ赤になることを確かめる）：石鹸のソーダ灰の量を 0.3 に／ソーダ灰の既定の木を 1.0 に（順序が反転）／ルブラン法から塩を外す／硫酸の既定へ塩を足す／
///   ガラスからソーダ灰を外す／肥料の硫酸を化学兵器へ付け替える。</summary>
public class SulfuricAcidSodaAshTests
{
    // 旧の参照価格（8c8f987a の goods.json から python で組み直した値・小数 6 桁）。名前は新しい名前（33＝硫酸）。表の出所は新しいレシピではない。
    private static readonly (string Name, double Ref)[] OldRefPrices =
    [
        ("鉄鉱石", 0.540000),
        ("石炭", 0.540000),
        ("銅鉱", 0.540000),
        ("鉛鉱", 0.540000),
        ("金", 0.540000),
        ("硫黄", 0.540000),
        ("塩", 0.540000),
        ("石灰石", 0.540000),
        ("珪砂", 0.540000),
        ("粘土", 0.540000),
        ("原油", 0.540000),
        ("木", 0.540000),
        ("綿花", 0.540000),
        ("麻", 0.540000),
        ("生糸", 0.540000),
        ("麦", 0.540000),
        ("果実", 0.540000),
        ("染料", 0.540000),
        ("葉煙草", 0.540000),
        ("畜産", 0.540000),
        ("漁獲", 0.540000),
        ("生ゴム", 0.540000),
        ("パーム油", 0.540000),
        ("象牙", 0.540000),
        ("サトウキビ", 0.540000),
        ("カカオ", 0.540000),
        ("香辛料", 0.540000),
        ("鉄", 1.469960),
        ("鋼", 2.175820),
        ("銅", 1.245860),
        ("鉛", 1.245860),
        ("セメント", 1.106000),
        ("ガラス", 1.052000),
        ("硫酸", 1.160000),
        ("火薬", 0.890000),
        ("製材", 0.866000),
        ("綿布", 0.866000),
        ("帆布", 0.866000),
        ("羊毛", 0.596000),
        ("毛織物", 0.922000),
        ("絹織物", 0.866000),
        ("革", 0.677000),
        ("小麦粉", 0.866000),
        ("肉", 0.650000),
        ("砂糖", 0.866000),
        ("ゴム", 0.974000),
        ("機械", 4.394332),
        ("蒸気機関", 3.796285),
        ("線路", 2.501820),
        ("衣料", 0.953600),
        ("高級衣料", 1.111000),
        ("家具", 1.048700),
        ("高級家具", 1.126800),
        ("紙", 1.200160),
        ("パン", 1.192000),
        ("加工肉", 0.975800),
        ("干物", 0.812000),
        ("缶詰", 1.218490),
        ("ビール", 0.596000),
        ("ワイン", 0.806400),
        ("灯油", 0.758000),
        ("煙草", 0.866000),
        ("革製品", 0.880500),
        ("陶磁器", 0.812000),
        ("石鹸", 0.944000),
        ("チョコレート", 0.942400),
        ("茶", 0.540000),
        ("コーヒー", 0.540000),
        ("防水布", 1.159400),
        ("医薬品", 1.068000),
        ("キニーネ", 0.944000),
        ("電化製品", 3.144278),
        ("火砲", 4.096962),
        ("小銃", 1.977892),
        ("弾薬", 1.482930),
        ("帆船", 1.625000),
        ("汽船", 3.187371),
        ("軍艦", 4.063481),
        ("電力", 1.570755),
        ("機関車", 3.755681),
        ("宝飾", 0.965600),
        ("時計", 1.816356),
        ("楽器", 0.921000),
        ("アヘン", 0.540000),
        ("労働力", 0.500000),
        ("教育", 0.600000),
        ("医療", 0.600000),
        ("行政", 0.600000),
        ("兵力", 0.500000),
        ("クロム", 0.540000),
        ("送電網", 2.451421),
        ("高度労働力", 0.800000),
        ("農業労働力", 0.500000),
        ("米", 0.540000),
        ("蒸留酒", 0.650000),
        ("機関銃", 4.312254),
        ("装甲艦", 7.801672),
        ("チーズ", 0.650000),
        ("野菜", 0.540000),
        ("肥料", 0.839880),
        ("飼料", 0.650000),
        ("上下水道", 0.600000),
        ("書籍", 0.926080),
        ("ガス", 0.650000),
        ("電化インフラ", 0.600000),
        ("ジャガイモ", 0.540000),
        ("トウモロコシ", 0.540000),
        ("錫鉱", 0.540000),
        ("硝石", 0.540000),
        ("毛皮", 0.540000),
        ("銀", 0.540000),
        ("グアノ", 0.540000),
        ("戦車", 7.224751),
        ("弩級戦艦", 12.727248),
        ("家事奉仕", 0.600000),
        ("内燃機関", 5.033576),
        ("住居", 0.867000),
        ("馬", 0.910000),
        ("ダイナマイト", 0.844000),
        ("自転車", 2.664074),
        ("軍用機", 4.175515),
        ("自動車", 4.411274),
        ("化学兵器", 1.130000),
        ("新聞", 1.204292),
        ("ラジオ", 2.849745),
        ("高等教育", 0.600000),
        ("冷蔵庫", 2.563563),
        ("金鶏樹皮", 0.540000),
    ];

    private static World Nation()
        => new Simulation(NationConfig.Britain().ToScenario(41, sharedWorld: true), new NullAgent()).World;

    private static string NameOf(int id) => Catalog.Goods[id].Name;

    /// <summary>レシピの札＝既定は財の名前だけ・並存は 財〈レシピ〉。</summary>
    private static string Label(Producer p) => p.RecipeName.Length == 0 ? NameOf(p.GoodId) : $"{NameOf(p.GoodId)}〈{p.RecipeName}〉";

    /// <summary>州の生産者のうち、指定した財を投入にするレシピの札の集合（家内工業・職人も含む＝既定の投入を写した行は同じ札になる）。</summary>
    private static SortedSet<string> UsersOf(Region r, params int[] goods)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var p in r.Producers)
            if (goods.All(g => p.Inputs.Any(i => i.GoodId == g && i.QtyPerOutput > 0))) set.Add(Label(p));
        return set;
    }

    private static double RefCost(Producer p) => Catalog.FactoryVa + p.Inputs.Sum(i => i.QtyPerOutput * Catalog.RefPrices[i.GoodId]);

    private static int IdOf(string name) => Catalog.Goods.First(g => g.Name == name).Id;

    [Fact]
    public void 費用中立_旧の財の参照価格は動かずソーダ灰は草木灰の価格()
    {
        Assert.Equal(128, OldRefPrices.Length);
        var bad = new List<string>();
        for (int i = 0; i < OldRefPrices.Length; i++)
        {
            var (name, old) = OldRefPrices[i];
            Assert.Equal(name, NameOf(i));   // 番号と名前の対応（旧の番号を 1 つも動かさない）
            double now = Catalog.RefPrices[i];
            if (Math.Abs(now - old) > 5e-4) bad.Add($"{i} {name}: 旧 {old:F4} 新 {now:F4}");
        }
        Assert.True(bad.Count == 0, "費用中立が崩れた参照価格：" + string.Join(" / ", bad));
        Assert.Equal(128, Catalog.SodaAshId);
        Assert.Equal(1.43, Catalog.RefPrices[Catalog.SodaAshId], 3);   // 木 2.0 の草木灰（0.06＋0.25＋0.04＋2.0×0.54）
    }

    [Fact]
    public void 順序_草木灰が最も高くルブラン法_電解_ソルベー法の順に安い()
    {
        var r = Nation().Regions[0];
        double Cost(string recipe) => RefCost(r.Producers.First(p => p.GoodId == Catalog.SodaAshId && p.RecipeName == recipe));
        double ash = Cost(""), leblanc = Cost("ルブラン法"), electro = Cost("電解"), solvay = Cost("ソルベー法");
        Assert.True(ash > leblanc, $"草木灰 {ash:F3} がルブラン法 {leblanc:F3} 以下＝市場が新しい製法を選ぶ順序にならない");
        Assert.True(leblanc > electro, $"ルブラン法 {leblanc:F3} が電解 {electro:F3} 以下");
        Assert.True(electro > solvay, $"電解 {electro:F3} がソルベー法 {solvay:F3} 以下");
        // 硫酸の黄鉄鉱法は旧と同じく既定（鉛室法）より高い（硫黄が安い間は負ける・硫黄の地理が不足したときだけ勝つ）
        double lead = RefCost(r.Producers.First(p => p.GoodId == Catalog.SulfuricAcidId && p.RecipeName.Length == 0));
        double pyrite = RefCost(r.Producers.First(p => p.GoodId == Catalog.SulfuricAcidId && p.RecipeName == "黄鉄鉱法"));
        Assert.True(pyrite > lead, $"黄鉄鉱法 {pyrite:F3} が鉛室法 {lead:F3} 以下＝硫黄の地理の不足の代替でなくなった");
    }

    [Fact]
    public void 使い手の表_硫酸とソーダ灰とその両方()
    {
        var r = Nation().Regions[0];
        // 精査 docs §5-1 の割り振りから手で書いた期待（札＝財〈レシピ〉）
        string[] acid = ["ダイナマイト", "キニーネ", "医薬品", "紙", "肥料", "肥料〈ハーバーボッシュ〉", "肥料〈グアノ肥料〉", "火薬〈無煙火薬〉", "絹織物〈人絹〉", "染料〈合成染料〉", "ソーダ灰〈ルブラン法〉"];
        string[] alkali = ["石鹸", "石鹸〈獣脂石鹸〉", "ガラス", "医薬品", "紙", "絹織物〈人絹〉", "染料〈合成染料〉"];
        string[] both = ["医薬品", "紙", "絹織物〈人絹〉", "染料〈合成染料〉"];
        Assert.Equal(acid.OrderBy(x => x, StringComparer.Ordinal), UsersOf(r, Catalog.SulfuricAcidId));
        Assert.Equal(alkali.OrderBy(x => x, StringComparer.Ordinal), UsersOf(r, Catalog.SodaAshId));
        Assert.Equal(both.OrderBy(x => x, StringComparer.Ordinal), UsersOf(r, Catalog.SulfuricAcidId, Catalog.SodaAshId));
        // 化学薬品の外へ出した行（酸でもアルカリでもない）：塩素・アンモニア・根菜の糖・カーバイドは硫酸もソーダ灰も使わない
        foreach (var label in new[] { "化学兵器", "冷蔵庫", "砂糖〈甜菜糖〉", "ゴム〈合成ゴム〉", "セメント", "内燃機関", "加工肉", "缶詰〈魚の缶詰〉" })
        {
            Assert.DoesNotContain(label, UsersOf(r, Catalog.SulfuricAcidId));
            Assert.DoesNotContain(label, UsersOf(r, Catalog.SodaAshId));
        }
    }

    [Fact]
    public void 元素の出どころ_酸の行に塩が無くアルカリの行にナトリウムか灰がある()
    {
        var r = Nation().Regions[0];
        int[] labor = [Catalog.LaborId, Catalog.AdvLaborId, Catalog.FarmLaborId];
        foreach (var p in r.Producers.Where(p => p.GoodId == Catalog.SulfuricAcidId))
        {
            Assert.DoesNotContain(p.Inputs, i => i.GoodId == Catalog.SaltId);   // 酸の行からナトリウムは出ない（旧の化学薬品の行は塩を含んだ）
            Assert.DoesNotContain(p.Inputs, i => i.GoodId == Catalog.SodaAshId);
            Assert.Contains(p.Inputs, i => i.GoodId == Catalog.SulfurId || i.GoodId == Catalog.CopperOreId);   // 硫黄の出どころ（硫黄・含銅黄鉄鉱）
        }
        foreach (var p in r.Producers.Where(p => p.GoodId == Catalog.SodaAshId))
        {
            Assert.Contains(p.Inputs, i => i.GoodId == Catalog.SaltId || i.GoodId == Catalog.TimberId);   // ナトリウムの出どころ（塩）か灰（木）
            if (p.RecipeName.Length > 0) Assert.Contains(p.Inputs, i => i.GoodId == Catalog.SaltId);   // 新しい製法は塩から（ルブラン法の硫酸は塩を分ける薬）
            // 酸の材料だけからアルカリを作らない：労働・硫酸を除いた材料が 1 つはある
            Assert.Contains(p.Inputs, i => !labor.Contains(i.GoodId) && i.GoodId != Catalog.SulfuricAcidId);
        }
        // 割った財を使う行の材料の取り違えを突き合わせる（付け替えた行の出どころ）
        Assert.Contains(r.Producers.First(p => p.GoodId == Catalog.FertilizerId && p.RecipeName.Length == 0).Inputs, i => i.GoodId == Catalog.LivestockId);   // 骨
        Assert.Contains(r.Producers.First(p => p.GoodId == Catalog.ChemicalWeaponId).Inputs, i => i.GoodId == Catalog.SaltId);   // 塩素は塩から
        Assert.Contains(r.Producers.First(p => p.GoodId == Catalog.GlassId).Inputs, i => i.GoodId == Catalog.SodaAshId);   // 融剤
        Assert.Contains(r.Producers.First(p => p.GoodId == Catalog.SoapId && p.RecipeName.Length == 0).Inputs, i => i.GoodId == Catalog.SodaAshId);   // 鹸化
        Assert.DoesNotContain(r.Producers.First(p => p.GoodId == Catalog.CombustionEngineId).Inputs, i => i.GoodId == Catalog.CrudeOilId);   // 燃料は材料でない
    }

    [Fact]
    public void 化学薬品の財と定数は残っていない()
    {
        Assert.DoesNotContain(Catalog.Goods, g => g.Name == "化学薬品");
        Assert.Equal("硫酸", NameOf(33));
        Assert.Equal("ソーダ灰", NameOf(128));
        Assert.Equal(130, Catalog.Goods.Length);   // ★(550) 129→130＝建造物を末尾に足した（線 fb）
        // ソース全域に旧の定数の読み手が 0（この文字列はここで組み立てる＝自分に当たらない）
        string token = "Chemicals" + "Id";
        string root = GoodIdGuardTests.RepoRoot();
        var hits = new List<string>();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.Display", "PriceOfBread.Runner", "PriceOfBread.App", "PriceOfBread.Tests", "PriceOfBread.Tests.Ui" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
                int line = 0;
                foreach (var text in File.ReadLines(f)) { line++; if (text.Contains(token)) hits.Add($"{Path.GetRelativePath(root, f)}:{line}"); }
            }
        Assert.True(hits.Count == 0, "旧の定数の読み手が残っている：" + string.Join(", ", hits));
    }

    [Fact]
    public void 発見の表_末尾の3行と既存の添字()
    {
        var d = Catalog.Discoveries;
        int dye = Array.FindIndex(d, x => x.Name == "合成染料");
        int leb = Array.FindIndex(d, x => x.Name == "ルブラン法"), sol = Array.FindIndex(d, x => x.Name == "ソルベー法"), ele = Array.FindIndex(d, x => x.Name == "電解");
        Assert.True(dye >= 0 && leb == dye + 1 && sol == dye + 2 && ele == dye + 3, "ルブラン法・ソルベー法・電解は合成染料の直後（添字を動かさない）");
        Assert.Equal(("ソーダ灰", 1790), (NameOf(d[leb].Good), d[leb].Decade));
        Assert.Equal(("ソーダ灰", 1860), (NameOf(d[sol].Good), d[sol].Decade));
        Assert.Equal(("ソーダ灰", 1890), (NameOf(d[ele].Good), d[ele].Decade));
        Assert.Equal("石炭火力", d[ele].After);
        int pyr = Array.FindIndex(d, x => x.Name == "黄鉄鉱法");
        Assert.Equal("硫酸", NameOf(d[pyr].Good));
        foreach (int row in new[] { leb, sol, ele }) Assert.Equal(DiscoveryField.Process, d[row].Field);
    }

    [Fact]
    public void 生産者_全ての州にソーダ灰の既定と並存3本があり発見の行に結ばれている()
    {
        var w = Nation();
        foreach (var r in w.Regions)
        {
            var soda = r.Producers.Where(p => p.GoodId == Catalog.SodaAshId).ToList();
            Assert.Equal(4, soda.Count);
            Assert.Single(soda, p => p.RecipeName.Length == 0);
            foreach (var name in new[] { "ルブラン法", "ソルベー法", "電解" })
            {
                var p = soda.Single(x => x.RecipeName == name);
                Assert.True(p.DiscoveryRow >= 0, $"{r.Name}:{name} が発見の行に結ばれていない");
                Assert.Equal(name, Catalog.Discoveries[p.DiscoveryRow].Name);
                Assert.Equal(0.0, Discovery.Gate(r, p, w.Discovered));   // 未発見の国では門が閉じている
            }
        }
    }
}
