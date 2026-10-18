using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★財の番号のずれの予防（2026-10-10 (249)・作者「材名がNo指定で削減でズレる不具合は他にもありそうだ。これを予防するための仕組みを組み込んでおいてほしい。」・00一覧 00-R2）。
///
/// 財の削減（(123)(129)）で番号が振り直されると、番号を数字で書いた読み手は黙って別の財を読む（(244)＝初期値の書き出しの「軍／首都の在庫」が時計・楽器・アヘン・医療を読んでいた）。
/// この5本（①②③⑥＋別ファイルの④）と、検出器の自己検査が「番号でなく名前で引く」の番人：
///   ① <see cref="財の定数はカタログの同じ名前の財を指す"/>＝Catalog の財の定数の全てが、**このファイルに手で書いた表**の名前の財を指す（表は Catalog や goods.json から作らない＝同語反復になり、ずれても赤にならない）。
///   ② <see cref="数値の財idは書かない"/>＝SimCore・Runner・App・Tests の .cs が財の番号を数字で書いていない。**許可リスト＝（ファイル, 一致した文字列, 出現の数）**（(251) で数を足した＝同じ文字列の2つ目は赤）。
///   ③ <see cref="道具のPythonは財の番号を数字で書かない"/>＝scripts の .py。財の番号は <c>scripts/instruments/instr.py</c> の <c>good_id(名前)</c> で引く。
///   ④ <c>埋め込みgoodsJsonはBuiltinGoodsと内容一致する</c>は <c>BreakDetectionTests</c>（goods.json が古いと名前引きが黙って間違う）。
///   ⑥ <see cref="世界データの財の番号は期待する名前の財を指す"/>＝world.json の財の番号（州・植民地・能動侵略の標的の賦存と国の象徴的主食）を手書きの表と突き合わせる。
/// </summary>
public class GoodIdGuardTests
{
    internal static string RepoRoot([CallerFilePath] string self = "")
        => Path.GetDirectoryName(Path.GetDirectoryName(self))!;

    // ─────────────────────────────────────────────────────────────────────────────
    // ① 定数と名前
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>振り直しの番人（作者裁定 Q7・(249) で反射の全数を足した）：Catalog の全ての財の定数が、**この表に手で書いた名前**の財を指す＝定数の値の書き違い・財の削減での振り直しの漏れを捕まえる。
    /// 表は今の BuiltinGoods から一度だけ書き起こして行ごとに確かめた物（Catalog や goods.json から作らない＝同語反復）。財を足す・畳むときは定数と表を同じ回に直す。</summary>
    [Fact]
    public void 財の定数はカタログの同じ名前の財を指す()
    {
        (int Id, string Name)[] pins =
        [
            (Catalog.IronOreId, "鉄鉱石"),
            (Catalog.CoalId, "石炭"),
            (Catalog.CopperOreId, "銅鉱"),
            (Catalog.LeadOreId, "鉛鉱"),
            (Catalog.GoldOreId, "金"),
            (Catalog.SulfurId, "硫黄"),
            (Catalog.SaltId, "塩"),
            (Catalog.LimestoneId, "石灰石"),
            (Catalog.SilicaSandId, "珪砂"),
            (Catalog.ClayId, "粘土"),
            (Catalog.CrudeOilId, "原油"),
            (Catalog.TimberId, "木"),
            (Catalog.CottonId, "綿花"),
            (Catalog.HempId, "麻"),
            (Catalog.SilkId, "生糸"),
            (Catalog.WheatId, "麦"),
            (Catalog.FruitId, "果実"),
            (Catalog.DyeId, "染料"),
            (Catalog.TobaccoLeafId, "葉煙草"),
            (Catalog.LivestockId, "畜産"),
            (Catalog.FishId, "漁獲"),
            (Catalog.RawRubberId, "生ゴム"),
            (Catalog.PalmOilId, "パーム油"),
            (Catalog.IvoryId, "象牙"),
            (Catalog.SugarCaneId, "サトウキビ"),
            (Catalog.CocoaId, "カカオ"),
            (Catalog.SpiceId, "香辛料"),
            (Catalog.IronId, "鉄"),
            (Catalog.SteelId, "鋼"),
            (Catalog.CopperId, "銅"),
            (Catalog.LeadId, "鉛"),
            (Catalog.CementId, "セメント"),
            (Catalog.GlassId, "ガラス"),
            (Catalog.ChemicalsId, "化学薬品"),
            (Catalog.GunpowderId, "火薬"),
            (Catalog.LumberId, "製材"),
            (Catalog.CottonClothId, "綿布"),
            (Catalog.CanvasId, "帆布"),
            (Catalog.WoolId, "羊毛"),
            (Catalog.WoolenClothId, "毛織物"),
            (Catalog.SilkClothId, "絹織物"),
            (Catalog.LeatherId, "革"),
            (Catalog.FlourId, "小麦粉"),
            (Catalog.MeatId, "肉"),
            (Catalog.SugarId, "砂糖"),
            (Catalog.RubberId, "ゴム"),
            (Catalog.MachineryId, "機械"),
            (Catalog.SteamEngineId, "蒸気機関"),
            (Catalog.RailId, "線路"),
            (Catalog.ClothingId, "衣料"),
            (Catalog.LuxuryClothingId, "高級衣料"),
            (Catalog.FurnitureId, "家具"),
            (Catalog.LuxuryFurnitureId, "高級家具"),
            (Catalog.PaperId, "紙"),
            (Catalog.BreadId, "パン"),
            (Catalog.PreservedMeatId, "加工肉"),
            (Catalog.DriedFishId, "干物"),
            (Catalog.CannedFoodId, "缶詰"),
            (Catalog.BeerId, "ビール"),
            (Catalog.WineId, "ワイン"),
            (Catalog.LampOilId, "灯油"),
            (Catalog.TobaccoId, "煙草"),
            (Catalog.LeatherGoodsId, "革製品"),
            (Catalog.PorcelainId, "陶磁器"),
            (Catalog.SoapId, "石鹸"),
            (Catalog.ChocolateId, "チョコレート"),
            (Catalog.TeaId, "茶"),
            (Catalog.CoffeeId, "コーヒー"),
            (Catalog.WaterproofClothId, "防水布"),
            (Catalog.MedicineId, "医薬品"),
            (Catalog.QuinineId, "キニーネ"),
            (Catalog.ApplianceId, "電化製品"),
            (Catalog.ArtilleryId, "火砲"),
            (Catalog.RifleId, "小銃"),
            (Catalog.AmmunitionId, "弾薬"),
            (Catalog.SailingShipId, "帆船"),
            (Catalog.SteamshipId, "汽船"),
            (Catalog.WarshipId, "軍艦"),
            (Catalog.ElectricityId, "電力"),
            (Catalog.LocomotiveId, "機関車"),
            (Catalog.JewelryId, "宝飾"),
            (Catalog.WatchId, "時計"),
            (Catalog.InstrumentId, "楽器"),
            (Catalog.OpiumId, "アヘン"),
            (Catalog.LaborId, "労働力"),
            (Catalog.EducationId, "教育"),
            (Catalog.HealthcareId, "医療"),
            (Catalog.AdminId, "行政"),
            (Catalog.MilitaryPowerId, "兵力"),
            (Catalog.ChromeId, "クロム"),
            (Catalog.GridId, "送電網"),
            (Catalog.AdvLaborId, "高度労働力"),
            (Catalog.FarmLaborId, "農業労働力"),
            (Catalog.RiceId, "米"),
            (Catalog.SpiritsId, "蒸留酒"),
            (Catalog.MachineGunId, "機関銃"),
            (Catalog.IroncladId, "装甲艦"),
            (Catalog.CheeseId, "チーズ"),
            (Catalog.VegetableId, "野菜"),
            (Catalog.FertilizerId, "肥料"),
            (Catalog.FodderId, "飼料"),
            (Catalog.InfrastructureId, "上下水道"),
            (Catalog.BookId, "書籍"),
            (Catalog.GasId, "ガス"),
            (Catalog.ElecInfraId, "電化インフラ"),
            (Catalog.PotatoId, "ジャガイモ"),
            (Catalog.MaizeId, "トウモロコシ"),
            (Catalog.TinOreId, "錫鉱"),
            (Catalog.SaltpeterId, "硝石"),
            (Catalog.FurId, "毛皮"),
            (Catalog.SilverId, "銀"),
            (Catalog.GuanoId, "グアノ"),
            (Catalog.TankId, "戦車"),
            (Catalog.DreadnoughtId, "弩級戦艦"),
            (Catalog.DomesticServiceId, "家事奉仕"),
            (Catalog.CombustionEngineId, "内燃機関"),
            (Catalog.HousingId, "住居"),
            (Catalog.HorseId, "馬"),
            (Catalog.DynamiteId, "ダイナマイト"),
            (Catalog.BicycleId, "自転車"),
            (Catalog.MilitaryAircraftId, "軍用機"),
            (Catalog.AutomobileId, "自動車"),
            (Catalog.ChemicalWeaponId, "化学兵器"),
            (Catalog.NewspaperId, "新聞"),
            (Catalog.RadioId, "ラジオ"),
            (Catalog.HigherEducationId, "高等教育"),
            (Catalog.RefrigeratorId, "冷蔵庫"),
            (Catalog.CinchonaBarkId, "金鶏樹皮"),   // ★2026-09-30 (129)：戻した財は末尾（既存の番号は変えない）
        ];
        // 表の1行ずつ：定数の値の財の名前が、手で書いた名前と同じ（ずれたら、どの定数かを名指しする）
        var constName = ConstantsByValue();
        foreach (var (id, n) in pins)
        {
            Assert.True(id >= 0 && id < Catalog.Goods.Length, $"定数 {constName.GetValueOrDefault(id, "?")}＝{id} は財の範囲の外");
            Assert.True(n == Catalog.Goods[id].Name, $"定数 {constName.GetValueOrDefault(id, "?")}＝{id} は「{Catalog.Goods[id].Name}」を指す（期待＝「{n}」）");
        }
        // 全ての定数が表にある：財の定数（public const int …Id）を反射で全て列挙し、表の件数・値の集合と等しい（表に無い定数・同じ値の定数が2本ある・財に定数が無いは赤）
        var all = GoodIdConstants();
        Assert.True(all.Count == pins.Length, $"財の定数 {all.Count} 本に対し表は {pins.Length} 行＝表に無い定数がある：" + string.Join("、", all.Where(c => !pins.Any(p => p.Id == c.Value)).Select(c => c.Name)));
        Assert.Equal(all.Select(c => c.Value).OrderBy(v => v), pins.Select(p => p.Id).OrderBy(v => v));
        Assert.Equal(Catalog.Goods.Length, pins.Length);
        Assert.Equal(Enumerable.Range(0, Catalog.Goods.Length), pins.Select(p => p.Id).OrderBy(v => v));   // 番号は 0 から欠けなく
        // 財の番号でない整数の定数は名前を固定する（財の定数が …Id で終わらない名前で生えたら、ここで赤）
        Assert.Equal(new[] { "DiscoveryBaseDecade", "LandFreight", "NoGood", "ReplacesOwn" },
            typeof(Catalog).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral && f.FieldType == typeof(int) && !f.Name.EndsWith("Id")).Select(f => f.Name).OrderBy(s => s, StringComparer.Ordinal));
    }

    private static List<(string Name, int Value)> GoodIdConstants()
        => typeof(Catalog).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(int) && f.Name.EndsWith("Id"))
            .Select(f => (f.Name, (int)f.GetRawConstantValue()!)).ToList();

    private static Dictionary<int, string> ConstantsByValue()
    {
        var d = new Dictionary<int, string>();
        foreach (var (name, v) in GoodIdConstants()) d[v] = d.TryGetValue(v, out var o) ? o + "／" + name : name;
        return d;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ② C# の数字の直書き
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>許可リスト＝（リポジトリからの相対パス, 一致した文字列, 出現の数）。**中身と数で固定**する＝1つ消えて1つ増えた入れ替えも、許可済みと同じ文字列の2つ目も赤（リストにあるのに当たらない行・数が減った行も赤＝古い許可は直す）。
    /// ⚠ここに足すのは「財の番号ではない」ものだけ（理由を1行ずつ）。財の番号は Catalog の定数（…Id）で書く。誤検出は正規表現の側で外す（外した型は「検出器は財でない形に当たらない」に並べる）。
    /// ★(251) 反証役 vg#4：旧は（ファイル, 文字列）の集合で照合していた＝同じファイルの同じ文字列の2つ目（Simulation.cs のもう1つの <c>home.Markets[1]</c> など）が通った＝出現の数を足した。</summary>
    private static readonly (string File, string Text, int Count, string Why)[] Allowed =
    [
        ("PriceOfBread.SimCore/Simulation.cs", "home.Markets[0]", 1, "Snapshot の既定（玩具の世界の食料＝0）。実世界では直後の主食探しが上書きする（監査 G03#4 の是正済み）"),
        ("PriceOfBread.SimCore/Simulation.cs", "home.Markets[1]", 1, "TurnSnapshot.ManufPrice＝読み手の無い欄（玩具の世界の工業品＝1・実世界では石炭を読む）＝死んだ欄（00一覧 00-R2 の報告に記録）"),
        ("PriceOfBread.Runner/Program.cs", "(8, \"\"", 1, "助走の長さ（四半期）の見出しの組（財ではない）"),
        ("PriceOfBread.Runner/Program.cs", "(16, \"\"", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "(24, \"\"", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "(44, \"\"", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "(84, \"\"", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "(204, \"\"", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "new[] { 0, 8, 40, 80, 200, 320, 400 }", 1, "診断の掃引に使う四半期の並び（財ではない）"),
        ("PriceOfBread.Runner/Program.cs", "new[] { 4, 100, 200, 320, 440 }", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "new[] { 0, 4, 8, 12, 16, 20, 24, 30, 40 }", 1, "同上"),
        ("PriceOfBread.Runner/Program.cs", "new[] { 8, 40, 120, 240, 400 }", 2, "同上"),
        ("PriceOfBread.Runner/Program.cs", "watch = [5, 2, 6, 3, 4, 1]", 1, "chaintrace＝連鎖の玩具の世界（ScenarioKind.ProductionChain）の財の番号（木5・木材2・鉄鉱石6・鉄3・鉄鋼4・家具1）＝Catalog の財の番号ではない"),
    ];

    /// <summary>玩具の世界（Simulation.cs の「◆玩具の世界ここから／ここまで」の間＝単一州の最小シナリオ）の中だけで許す物。財の番号がシナリオの中だけの番号で、Catalog の財の番号ではない。
    /// 範囲の外に同じ文字列が現れても許さない（範囲の外は <see cref="Allowed"/> だけ）。</summary>
    private static readonly (string File, string Text, int Count, string Why)[] ToyAllowed =
    [
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 0", 6, "玩具の世界の食料"),
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 1", 3, "玩具の世界の工業品・家具"),
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 2", 1, "玩具の世界の製材"),
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 3", 1, "玩具の世界の鉄"),
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 4", 1, "玩具の世界の鋼"),
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 5", 1, "玩具の世界の木材"),
        ("PriceOfBread.SimCore/Simulation.cs", "GoodId = 6", 1, "玩具の世界の鉱石"),
        ("PriceOfBread.SimCore/Simulation.cs", "new Input(2,", 1, "玩具の世界の投入（製材）"),
        ("PriceOfBread.SimCore/Simulation.cs", "new Input(3,", 1, "玩具の世界の投入（鉄）"),
        ("PriceOfBread.SimCore/Simulation.cs", "new Input(4,", 1, "玩具の世界の投入（鋼）"),
        ("PriceOfBread.SimCore/Simulation.cs", "new Input(5,", 1, "玩具の世界の投入（木材）"),
        ("PriceOfBread.SimCore/Simulation.cs", "new Input(6,", 1, "玩具の世界の投入（鉱石）"),
    ];

    /// <summary>許可の照合（数つき）：当たりを（ファイル, 文字列）で数え、許可の数と違えば名指しする。extra＝許可に無いか許可の数を超えた当たり・stale＝許可にあるのに当たりが許可の数より少ない行。</summary>
    private static (List<string> Extra, List<string> Stale) CompareCounted(IReadOnlyList<Hit> hits, IEnumerable<(string File, string Text, int Count, string Why)> allowed)
    {
        var allow = allowed.ToDictionary(a => (a.File, a.Text), a => a.Count);
        var extra = new List<string>(); var stale = new List<string>();
        foreach (var grp in hits.GroupBy(h => (h.File, h.Text)))
        {
            int n = grp.Count(), ok = allow.GetValueOrDefault(grp.Key);
            if (n > ok) extra.Add($"{grp.First().File}:{string.Join(",", grp.Select(h => h.Line))}［{grp.First().Rule}］{(grp.Key.Text.Length > 160 ? grp.Key.Text[..160] + "…" : grp.Key.Text)}（{n} 件・許可 {ok}）");
        }
        foreach (var (key, cnt) in allow)
        {
            int n = hits.Count(h => (h.File, h.Text) == key);
            if (n < cnt) stale.Add($"{key.File}［{key.Text}］（当たり {n} 件・許可 {cnt}）");
        }
        return (extra, stale);
    }

    /// <summary>この規則はこのファイルでだけ許す（定義の表そのもの）。</summary>
    private static readonly (string Rule, string File)[] RuleFiles =
    [
        ("const int …Id = n", "PriceOfBread.SimCore/Catalog.cs"),   // 財の番号の定義（128 本・①の表と goods.json が検査する）
    ];

    /// <summary>当たり1件。Pos＝走査した文字列の中の位置（同じ位置の同じ文字列に2つの規則が当たったら1件に数える）。</summary>
    private sealed record Hit(string File, int Line, string Rule, string Text, int Pos);

    /// <summary>コメントと文字列・文字を消す（改行は残す＝行番号を保つ）。補間文字列の穴の中身も消す（穴の中の直書きは見ない＝限界）。</summary>
    internal static string StripCode(string src)
    {
        var sb = new StringBuilder(src.Length);
        int i = 0, n = src.Length;
        while (i < n)
        {
            char c = src[i];
            if (c == '/' && i + 1 < n && src[i + 1] == '/') { while (i < n && src[i] != '\n') i++; continue; }
            if (c == '/' && i + 1 < n && src[i + 1] == '*')
            {
                int j = src.IndexOf("*/", i + 2, StringComparison.Ordinal); j = j < 0 ? n : j + 2;
                for (int k = i; k < j; k++) if (src[k] == '\n') sb.Append('\n');
                i = j; continue;
            }
            if (c == '"' || ((c == '@' || c == '$') && i + 1 < n && (src[i + 1] == '"' || ((src[i + 1] == '@' || src[i + 1] == '$') && i + 2 < n && src[i + 2] == '"'))))
            {
                int j = SkipString(src, i);
                sb.Append("\"\"");
                for (int k = i; k < j; k++) if (src[k] == '\n') sb.Append('\n');
                i = j; continue;
            }
            if (c == '\'')
            {
                var m = CharLiteral.Match(src, i);
                if (m.Success && m.Index == i) { sb.Append("''"); i += m.Length; continue; }
            }
            sb.Append(c); i++;
        }
        return sb.ToString();
    }

    private static readonly Regex CharLiteral = new(@"\G'(?:\\.[^']*|[^\\'])'");

    /// <summary>i から始まる文字列（"…"・@"…"・$"…"・$@"…"・"""…"""）の次の位置。</summary>
    private static int SkipString(string s, int i)
    {
        int n = s.Length; bool verbatim = false, interp = false;
        while (i < n && s[i] != '"') { if (s[i] == '@') verbatim = true; if (s[i] == '$') interp = true; i++; }
        if (i + 2 < n && s[i + 1] == '"' && s[i + 2] == '"')   // 生文字列
        {
            int j = s.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
            return j < 0 ? n : j + 3;
        }
        i++;
        while (i < n)
        {
            char c = s[i];
            if (verbatim) { if (c == '"') { if (i + 1 < n && s[i + 1] == '"') { i += 2; continue; } return i + 1; } }
            else { if (c == '\\') { i += 2; continue; } if (c == '"') return i + 1; }
            if (interp && c == '{')
            {
                if (i + 1 < n && s[i + 1] == '{') { i += 2; continue; }
                int depth = 1; i++;
                while (i < n && depth > 0)
                {
                    char d = s[i];
                    if (d == '"' || ((d == '@' || d == '$') && i + 1 < n && (s[i + 1] == '"' || s[i + 1] == '@' || s[i + 1] == '$'))) { i = SkipString(s, i); continue; }
                    if (d == '{') depth++; else if (d == '}') depth--;
                    i++;
                }
                continue;
            }
            i++;
        }
        return n;
    }

    private const string GoodIsh = @"(?:id|g|gi|gid|good|goodId|g2|\w*GoodId|\w*Good|\w*Gid)";
    private static readonly Regex GoodIshOnly = new("^(?:[\\w]+\\.)*" + GoodIsh + "$");

    private static readonly (string Rule, Regex Rx)[] Rules =
    [
        ("I(n, …)", new Regex(@"\bI\(\s*\d+\s*,")),
        ("new Input(n, …)", new Regex(@"\bnew Input\(\s*\d+\s*,")),
        ("GoodId の比較・代入", new Regex(@"\b\w*(?:GoodId|Good|Gid|gid|goodId)\s*(?:==|!=|=(?![=>]))\s*\d+\b")),
        // ★(251) 反証役 vg#2：財の定数で添字を引いている財ごとの配列の名前を足した（今の木で `名前[Catalog.…Id]` を集めて2回以上の名前＝ration 19・clearPrice 17・revenue 14・investDemand 7・militaryDemand 5・Price 5・inputNeed 3・produced 2・StateMonopolyMargins）。
        //   ⚠限界＝名前の一覧に無い配列（1回しか使わない px・wp0 などの局所の名前・新しい配列）の数字の添字は見えない（README §1-9）。
        ("財ごとの表の添字", new Regex(@"(?<!\w)(?:[\w.]+\.)?(?:Markets|markets|RefPrices|Complexity|Goods|Prices|Stocks?|Imports|Exports|ration|clearPrice|revenue|investDemand|militaryDemand|Price|inputNeed|produced|StateMonopolyMargins)\s*\[\s*\d+\s*\]")),
        ("財の変数 == n", new Regex(@"\b" + GoodIsh + @"\s*(?:==|!=)\s*\d+\b")),
        ("n == 財の変数", new Regex(@"\b\d+\s*(?:==|!=)\s*" + GoodIsh + @"\b")),
        ("財の変数 is n", new Regex(@"\b" + GoodIsh + @"\s+is\s+(?:not\s+)?\d+(?:\s+or\s+\d+)*")),
        ("辞書・組の初期化子 { n, \"名前\" }", new Regex(@"(?:\{|(?<![\w)\]>])\()\s*\d+\s*,\s*""""")),   // 直前が識別子の ( は呼び出し（MkPop(0, "貴族", …) など＝階級の番号）であって組ではない
        ("GoodDef の直書き new(n, \"名前\"", new Regex(@"\bnew\s*\(\s*\d+\s*,\s*""""")),
        ("財を引数に取る呼び出し", new Regex(@"\b(?:BuildFactory|CanBuildFactory|CanSellFactory|SellFactory|FindFactory|SetStateTarget|SetSubsidyRate|SetSubsidy|CanSubsidize|OutputOf|PaySales|GoodOf)\([^,()]*,\s*\d+\s*[,)]")),
        ("財を引数に取る呼び出し（先頭）", new Regex(@"\b(?:SelectProdGood|SetFactoryTarget|NationFactoryUnits|WeatherExposure|IsMineral|IsColonial|RgoBoosterGoods|RgoFacilityName|WearRatePerMonthOf|DurabilityOf|UseOf|TransportWeight|FactorySpec|ServiceRefPrice|AdvLaborPerOutput|IsRefrigerable|IsScarceMineral|TradeBiasOf)\(\s*\d+\s*[,)]")),
        ("名前つき引数 good: n", new Regex(@"\b(?:goodId|good|gid|goodIdx)\s*:\s*\d+\b")),
        ("無名の整数の配列", new Regex(@"\bnew\s*(?:int)?\s*\[\s*\]\s*\{\s*\d+\s*(?:,\s*\d+\s*)*\}")),
        ("foreach (… in [整数の並び])", new Regex(@"\bin\s+\[\s*\d+\s*(?:,\s*\d+\s*)*\]")),
        ("const int …Id = n", new Regex(@"\bconst\s+int\s+\w+Id\s*=\s*\d+")),
    ];

    // ★(251) 反証役 vg#3：今の木で財の定数だけを並べている一覧の名前（Runner の reps・watch・milTrace／PolicyPoliticsTests の excise。DiploCore の StapleExtra は 2026-10-18 に DiploCore ごと削除）を足した。
    //   名前に依らない検出は下の「初期化子に財の定数と整数が混ざる」（{ WheatId, 47 } の形）。⚠限界＝名前が一覧に無く、要素が全て整数の新しい配列は見えない（README §1-9）。
    private static readonly Regex GoodArray = new(@"\b\w*(?i:goods?|goodids?|ids|gids?|resources|fuels?|metals?|weapons?|staples?|crops?|reps|watch|miltrace|stapleextra|excise)\s*=\s*(?:new\s*(?:\w+(?:<\w+>)?)?\s*(?:\[\s*\]\s*)?)?[\[{]([^\]}]*)[\]}]|ColonyDto\([^,()]*,\s*\[([^\]]*)\]");
    /// <summary>入れ子の無い初期化子 [ … ]・{ … } の中身（要素をカンマで分けて、財の定数と裸の整数が混ざるかを見る）。</summary>
    private static readonly Regex FlatInit = new(@"[\[{]([^\[\]{}]*)[\]}]");
    private static readonly Regex IntToken = new(@"(?<![\w.])\d+(?![\w.])");
    private static readonly Regex IsChain = new(@"\bis\s+(?:not\s+)?[\w.]+(?:\s+or\s+[\w.]+)*");
    private static readonly Regex GoodConstToken = new(@"^(?:Catalog\.)?[A-Z]\w*Id$");
    private static readonly Regex Switch = new(@"([\w.]+)\s+switch\s*\{|\bswitch\s*\(\s*([\w.]+)\s*\)\s*\{");
    private static readonly Regex NumToken = new(@"(?<![\w.])(\d+)(?![\w.])\s*(=>|\bor\b|:(?!:))");

    private static string Norm(string s) => Regex.Replace(s.Trim(), @"\s+", " ");

    /// <summary>括弧（丸・角・波）の外のカンマで分ける（初期化子の要素）。</summary>
    private static string[] TopLevelSplit(string s)
    {
        var parts = new List<string>(); int depth = 0, start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}') depth--;
            else if (c == ',' && depth == 0) { parts.Add(s[start..i].Trim()); start = i + 1; }
        }
        parts.Add(s[start..].Trim());
        return parts.ToArray();
    }

    private static IEnumerable<Hit> ScanCode(string file, string stripped)
    {
        int Ln(int idx) { int l = 1; for (int k = 0; k < idx; k++) if (stripped[k] == '\n') l++; return l; }
        foreach (var (rule, rx) in Rules)
            foreach (Match m in rx.Matches(stripped))
                yield return new Hit(file, Ln(m.Index), rule, Norm(m.Value), m.Index);
        // 財の一覧・賦存の配列：要素に整数がある（0.5 のような小数の列は財の番号ではない）
        foreach (Match m in GoodArray.Matches(stripped))
            if (IntToken.IsMatch(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value))
                yield return new Hit(file, Ln(m.Index), "財の一覧・賦存の配列", Norm(m.Value), m.Index);
        // 初期化子に「…Id の定数」と整数が混ざる（財の一覧に番号が1つ紛れた形・名前に依らない＝(251) vg#3）
        foreach (Match m in FlatInit.Matches(stripped))
        {
            var toks = TopLevelSplit(m.Groups[1].Value);   // 要素の区切りは括弧の外のカンマだけ（new(…, LaborId, …, 1780) の中の引数は要素ではない）
            if (toks.Any(t => GoodConstToken.IsMatch(t)) && toks.Any(t => Regex.IsMatch(t, @"^\d+$")))
                yield return new Hit(file, Ln(m.Index), "初期化子に財の定数と整数", Norm(m.Value), m.Index);
        }
        // is の連なりに「…Id の定数」と整数が混ざる（財の集合の判定に番号が1つ紛れた形）
        foreach (Match m in IsChain.Matches(stripped))
        {
            var toks = m.Value.Substring(2).Split(new[] { "or" }, StringSplitOptions.None).Select(t => t.Replace("not", "").Trim()).ToArray();
            if (toks.Any(t => GoodConstToken.IsMatch(t)) && toks.Any(t => Regex.IsMatch(t, @"^\d+$")))
                yield return new Hit(file, Ln(m.Index), "is の連なりに定数と整数", Norm(m.Value), m.Index);
        }
        // 財で分岐する switch（式・文）の腕の先頭の整数
        foreach (Match m in Switch.Matches(stripped))
        {
            string gov = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (!GoodIshOnly.IsMatch(gov)) continue;
            int open = m.Index + m.Length - 1, depth = 0, end = open;
            for (; end < stripped.Length; end++) { if (stripped[end] == '{') depth++; else if (stripped[end] == '}' && --depth == 0) break; }
            string body = stripped.Substring(open, Math.Min(end, stripped.Length - 1) - open + 1);
            foreach (Match a in NumToken.Matches(body))
                yield return new Hit(file, Ln(open + a.Index), "財の switch の整数の腕", Norm($"{gov} switch … {a.Value}"), open + a.Index);
        }
    }

    internal static IEnumerable<(string Full, string Rel)> CsFiles()
    {
        string root = RepoRoot();
        foreach (var proj in new[] { "PriceOfBread.SimCore", "PriceOfBread.Runner", "PriceOfBread.App", "PriceOfBread.Tests" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, proj), "*.cs", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
                if (rel.Contains("/obj/") || rel.Contains("/bin/")) continue;
                yield return (f, rel);
            }
    }

    /// <summary>単一州の最小シナリオ（財の番号がシナリオの中だけ＝Catalog の財の番号ではない）の印。SimCore では範囲の中の当たりを <see cref="ToyAllowed"/> で固定する。
    /// テストは <see cref="ToyTestFiles"/> の名前のファイル（シナリオの中だけを使う）を対象外にする。</summary>
    private const string ToyStart = "◆玩具の世界ここから", ToyEnd = "◆玩具の世界ここまで";

    /// <summary>丸ごと対象外にするテストのファイル（単一州の最小シナリオだけを使う）。★(251) 反証役 vg#5：旧は「印の文字列を含むファイル」を丸ごと外していた＝印を定数で持つこのファイル自身や、
    /// 印にコメントで触れただけのファイルが黙って全行対象外になった＝名前で明示する（印を含むのにこの一覧に無いテストのファイルは赤）。</summary>
    private static readonly string[] ToyTestFiles = ["PriceOfBread.Tests/ChainTests.cs"];

    private static readonly Regex AnyInt = new(@"(?<![\w.])\d+(?![\w.])");

    [Fact]
    public void 数値の財idは書かない()
    {
        var hits = new List<Hit>(); var toyHits = new List<Hit>(); var toyMarked = new List<string>();
        foreach (var (f, rel) in CsFiles())
        {
            string raw = File.ReadAllText(f);
            bool isTest = rel.StartsWith("PriceOfBread.Tests/", StringComparison.Ordinal);
            if (isTest && raw.Contains(ToyStart) && rel != "PriceOfBread.Tests/GoodIdGuardTests.cs") toyMarked.Add(rel);   // このファイルは印を定数で持つだけ
            if (isTest && ToyTestFiles.Contains(rel)) continue;
            // 玩具の範囲（行番号）：印は文字列の外のコメントにある
            int toyFrom = -1, toyTo = -1, ln = 0;
            foreach (var line in raw.Split('\n')) { ln++; if (toyFrom < 0 && line.Contains(ToyStart)) toyFrom = ln; if (line.Contains(ToyEnd)) toyTo = ln; }
            foreach (var h in ScanCode(rel, StripCode(raw)))
            {
                if (RuleFiles.Any(r => r.Rule == h.Rule && r.File == h.File)) continue;
                // テストの 0 と 1 は単一州の最小シナリオの添字（食料＝0・工業品＝1）と見分けられない＝数えない（旧の番人と同じ線）
                if (isTest && AnyInt.Matches(h.Text).All(m => int.Parse(m.Value) <= 1)) continue;
                if (toyFrom > 0 && toyTo > toyFrom && h.Line > toyFrom && h.Line < toyTo) toyHits.Add(h); else hits.Add(h);
            }
        }
        // 同じ箇所に2つの規則が当たる（例：無名の配列と初期化子の混ざり）ことがある＝（ファイル, 行, 文字列）で1つに数える
        static List<Hit> Dedup(List<Hit> l) => l.GroupBy(h => (h.File, h.Pos, h.Text)).Select(g => g.First()).ToList();
        var (e1, s1) = CompareCounted(Dedup(hits), Allowed);
        var (e2, s2) = CompareCounted(Dedup(toyHits), ToyAllowed);
        var extra = e1.Concat(e2).ToList();
        var stale = s1.Concat(s2).ToList();
        Assert.True(toyMarked.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(ToyTestFiles.OrderBy(s => s, StringComparer.Ordinal)),
            "玩具の世界の印を持つテストのファイルと、丸ごと対象外にする一覧 ToyTestFiles が違う：印あり＝" + string.Join("、", toyMarked));
        Assert.True(extra.Count == 0,
            $"財の番号を数字で書いた箇所が {extra.Count} 件＝Catalog の定数（…Id）で書くこと（財を畳む・振り直すと数値は黙って別の財を読む）：" + string.Join(" ／ ", extra.Take(40)));
        Assert.True(stale.Count == 0, $"許可リストにあるのに当たらない行 {stale.Count} 件＝消すこと：" + string.Join(" ／ ", stale.Take(40)));
    }

    /// <summary>番人②の検出器の自己検査＝(244) の形（foreach の無名配列）ほか、書いてはいけない形が実際に当たる（検出器が空回りしていない）。</summary>
    [Theory]
    [InlineData("foreach (int g in new[] { 81, 82, 83, 86 }) { }")]
    [InlineData("var x = r.Markets[54].Price;")]
    [InlineData("if (pr.GoodId == 54) { }")]
    [InlineData("var p = new Producer { GoodId = 7 };")]
    [InlineData("var r = I(54, 1.0);")]
    [InlineData("bool b = id is WheatId or 20;")]
    [InlineData("double w = id switch { CottonId => 1.0, 20 => 0.5, _ => 0 };")]
    [InlineData("const int FooId = 128;")]
    [InlineData("var ids = new int[] { 3, 4 };")]
    [InlineData("int[] fuels = [81, 82];")]
    [InlineData("var goodIds = new List<int> { 5 };")]
    [InlineData("foreach (var (gid, gl) in new[] { (15, \"小麦\") }) { }")]
    [InlineData("double p = clearPrice[54];")]                       // (251) vg#2
    [InlineData("x += investDemand[47];")]
    [InlineData("int[] watch = [81, 82];")]                          // (251) vg#3
    [InlineData("int[] list = { Catalog.WheatId, 47 };")]
    [InlineData("var l = new[] { 66, CoalId };")]
    public void 検出器は書いてはいけない形に当たる(string code)
        => Assert.NotEmpty(ScanCode("x.cs", StripCode(code)).ToList());

    /// <summary>誤検出の型（財でない整数）には当たらない＝検出器が広すぎない。外した型：コメントと文字列・Rate などの double 配列・Tuple の第2要素・n 次元の添字・ループの境界。</summary>
    [Theory]
    [InlineData("// r.Markets[54] と GoodId == 54")]
    [InlineData("var s = \"Markets[54] GoodId == 54\";")]
    [InlineData("double[] w = [200, 600, 1500];")]
    [InlineData("for (int g = 0; g < n; g++) { }")]
    [InlineData("var q = Catalog.Goods[Catalog.WheatId];")]
    [InlineData("bool b = quarterOfYear is 0 or 3;")]
    [InlineData("var a = new[] { \"a\", \"b\" };")]
    [InlineData("int[] marks = [1800, 1850];")]
    [InlineData("int[] years = { 1784, 1800 };")]
    [InlineData("var resources = new[] { 0.5, 1.5 };")]
    [InlineData("int[] none = [];")]
    public void 検出器は財でない形に当たらない(string code)
        => Assert.Empty(ScanCode("x.cs", StripCode(code)).ToList());

    // ─────────────────────────────────────────────────────────────────────────────
    // ③ 道具（Python）の数字の直書き
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>許可リスト（Python）＝（scripts からの相対パス, 一致した文字列）。⚠足すのは「財の番号ではない」ものだけ（理由を1行）。</summary>
    private static readonly (string File, string Text, int Count, string Why)[] PyAllowed =
    [
        ("instruments/m208/inj_s208.py", "g == 0", 2, "注入する C# の本位の印（金本位＝0・銀本位＝1・不換＝その他）＝財の番号ではない"),
        ("instruments/m208/inj_s208.py", "g == 1", 2, "同上"),
    ];

    private static readonly (string Rule, Regex Rx)[] PyRules =
    [
        ("GoodId／good／gid の比較", new Regex(@"\b\w*(?:GoodId|[Gg]ood|[Gg]id)\w*\s*(?:==|!=)\s*['""]?\d+")),
        // ★(251) 反証役 vg#6：C# の GoodIsh と同じ短い名前（g・id・gi・g2）の比較と、CSV を文字列で読んだときの引用符つきの比較（'54'）。⚠限界＝t[3] == '54' のように名前の無い比較は見えない
        ("g／id／gi の比較", new Regex(@"\b(?:g|id|gi|g2)\s*(?:==|!=)\s*['""]?\d+")),
        ("財ごとの表の添字", new Regex(@"\b(?:Goods|Markets|RefPrices|Complexity|goods|markets|prices|stocks|ref_prices)\s*\[\s*\d+\s*\]")),
        ("I(n, …)", new Regex(@"\bI\(\s*\d+\s*,")),
        ("new Input(n, …)", new Regex(@"\bnew Input\(\s*\d+\s*,")),
        ("good／gid への代入", new Regex(@"\b(?:good|gid|good_id)\w*\s*=\s*\d+")),
        ("good／gid in (n, …)", new Regex(@"\b(?:good|gid)\w*\s+in\s+[\(\[\{]\s*\d+")),
        ("C# の無名の整数の配列", new Regex(@"\bnew\s*(?:int)?\s*\[\s*\]\s*\{\s*\d+")),
        ("財の表の直書き（GOODS = …）", new Regex(@"\bGOODS?\w*\s*=\s*[\{\[\(]")),
    ];

    private static IEnumerable<Hit> ScanPython(string file, string text)
    {
        foreach (var (rule, rx) in PyRules)
            foreach (Match m in rx.Matches(text))
            {
                int ln = 1; for (int k = 0; k < m.Index; k++) if (text[k] == '\n') ln++;
                yield return new Hit(file, ln, rule, Norm(m.Value), m.Index);
            }
    }

    /// <summary>scripts の .py（計器の読み手・注入の道具）が財の番号を数字で書いていない。財は <c>scripts/instruments/instr.py</c> の <c>good_id("財の名前")</c> で引く（goods.json を読む＝新しさは
    /// <c>BreakDetectionTests.埋め込みgoodsJsonはBuiltinGoodsと内容一致する</c> が守る）。注入の道具が C# の本文を文字列で持つ場合は、その中の財も Catalog の定数で書く（生の本文に当てる＝文字列は消さない）。
    /// 対象外＝measurements の過去の回の写し（凍結した記録・scripts の道具として引かれない）。</summary>
    [Fact]
    public void 道具のPythonは財の番号を数字で書かない()
    {
        string scripts = Path.Combine(RepoRoot(), "scripts");
        var hits = new List<Hit>();
        int files = 0;
        foreach (var f in Directory.EnumerateFiles(scripts, "*.py", SearchOption.AllDirectories))
        {
            files++;
            string rel = Path.GetRelativePath(scripts, f).Replace('\\', '/');
            hits.AddRange(ScanPython(rel, File.ReadAllText(f)));
        }
        Assert.True(files > 100, $"scripts の .py が {files} 本しか見つからない＝検査が空回りしている");
        var (extra, stale) = CompareCounted(hits.GroupBy(h => (h.File, h.Pos, h.Text)).Select(g => g.First()).ToList(), PyAllowed);
        Assert.True(extra.Count == 0, $"Python の道具が財の番号を数字で書いている {extra.Count} 件＝instr.good_id(\"名前\") か Catalog の定数で書くこと：" + string.Join(" ／ ", extra.Take(40)));
        Assert.True(stale.Count == 0, $"許可リスト（Python）にあるのに当たらない行 {stale.Count} 件＝消すこと：" + string.Join(" ／ ", stale.Take(40)));
    }

    [Theory]
    [InlineData("if gid == 54: pass")]
    [InlineData("x = Markets[54]")]
    [InlineData("s = 'else if (pr.GoodId == 15) { }'")]
    [InlineData("good_id = 81")]
    [InlineData("for gid in (81, 82, 83, 86): pass")]
    [InlineData("if g == 54: pass")]
    [InlineData("if t.gid == '54': pass")]
    [InlineData("if id != 81: pass")]
    public void Pythonの検出器は書いてはいけない形に当たる(string code)
        => Assert.NotEmpty(ScanPython("x.py", code).ToList());

    [Theory]
    [InlineData("gid = good_id('小麦')")]
    [InlineData("D = {'pollTaxStage': 1}")]
    [InlineData("print(prices[ix])")]
    public void Pythonの検出器は名前引きと財でない数字に当たらない(string code)
        => Assert.Empty(ScanPython("x.py", code).ToList());

    // ─────────────────────────────────────────────────────────────────────────────
    // ⑥ データ（world.json）の財の番号
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>州・植民地の賦存（world.json の resources は財の**番号**で書いてある）の、財の名前ごとの個数。財の削減で番号が振り直されたのに world.json を直し忘れると、賦存が黙って別の財になる
    /// （茶の産地が象牙になる）＝名前ごとの個数がずれて赤。**表は手で書いた物**（今の world.json から一度だけ書き起こして、産地が史実に合うか目で確かめた）。州や植民地を足す・賦存を直す回は表も直す。</summary>
    private static readonly (string Good, int Count)[] ExpectedResourceCounts =
    [
        ("アヘン", 4),
        ("カカオ", 4),
        ("クロム", 3),
        ("グアノ", 1),
        ("コーヒー", 10),
        ("サトウキビ", 19),
        ("ジャガイモ", 9),
        ("トウモロコシ", 14),
        ("パーム油", 4),
        ("原油", 6),
        ("塩", 4),
        ("帆船", 1),
        ("木", 33),
        ("果実", 40),
        ("染料", 11),
        ("機械", 2),
        ("毛皮", 5),
        ("漁獲", 27),
        ("珪砂", 1),
        ("生ゴム", 3),
        ("生糸", 20),
        ("畜産", 52),
        ("石灰石", 3),
        ("石炭", 12),
        ("硝石", 6),
        ("硫黄", 1),
        ("米", 22),
        ("粘土", 3),
        ("絹織物", 4),
        ("綿布", 6),
        ("綿花", 23),
        ("茶", 8),
        ("葉煙草", 6),
        ("象牙", 6),
        ("野菜", 3),
        ("金", 5),
        ("金鶏樹皮", 5),
        ("鉄", 1),
        ("鉄鉱石", 8),
        ("鉛鉱", 2),
        ("銀", 6),
        ("銅鉱", 6),
        ("鋼", 4),
        ("錫鉱", 4),
        ("陶磁器", 3),
        ("香辛料", 12),
        ("麦", 57),
        ("麻", 5),
    ];

    /// <summary>各国の「象徴的主食」（culturalGoodId・world.json は番号）の、財の名前ごとの国の数。</summary>
    private static readonly (string Good, int Nations)[] ExpectedCulturalGoodNations =
    [
        ("コーヒー", 2),
        ("チーズ", 1),
        ("トウモロコシ", 6),
        ("パン", 24),
        ("ビール", 7),
        ("米", 18),
        ("茶", 1),
        ("蒸留酒", 2),
    ];

    /// <summary>能動侵略の標的の植民地（賦存のある物だけ・world.json の順）の財の名前の並び。**手で書いた表**（(251) に今の world.json から書き起こし、産地が史実に合うか目で確かめた：
    /// 琉球の砂糖・カリフォルニアの金・ボルネオの石油・マレーの錫とゴム・南アフリカの金と銅）。賦存の無い標的（大盆地・スーダンほか 15）は並べない。</summary>
    private static readonly (string Colony, string Goods)[] ExpectedTargetResources =
    [
        ("琉球", "サトウキビ・染料・漁獲"),
        ("アルタ=カリフォルニア", "金・麦・畜産・木"),
        ("ボルネオ", "生ゴム・原油・香辛料・木"),
        ("マレー", "香辛料・生ゴム・錫鉱"),
        ("南アフリカ", "金・銅鉱・畜産"),
        ("パナマ", "木"),
    ];

    [Fact]
    public void 世界データの財の番号は期待する名前の財を指す()
    {
        var nations = WorldData.Nations();
        var res = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var cul = new SortedDictionary<string, int>(StringComparer.Ordinal);
        void Add(SortedDictionary<string, int> d, int id)
        {
            Assert.True(id >= 0 && id < Catalog.Goods.Length, $"財の番号 {id} が範囲の外");
            string n = Catalog.Goods[id].Name; d[n] = d.GetValueOrDefault(n) + 1;
        }
        foreach (var c in nations)
        {
            foreach (var p in c.Provinces) foreach (var g in p.Resources) Add(res, g);
            foreach (var col in c.InitialColonies) foreach (var g in col.Resources) Add(res, g);
            if (c.CulturalGoodId >= 0) Add(cul, c.CulturalGoodId);
        }
        // ★(251) 反証役 vg#1：能動侵略の標的（world.json の abstractTargets[].colonies[].resources・MultiNationSim が標的の賦存に使う）も財の番号＝植民地ごとに名前の並びで固定する
        var targets = WorldData.Setup().Targets.SelectMany(t => t.Colonies.Where(c => c.Resources.Length > 0)
            .Select(c => (Colony: c.Name, Goods: string.Join("・", c.Resources.Select(id => { Assert.True(id >= 0 && id < Catalog.Goods.Length, $"標的 {c.Name} の財の番号 {id} が範囲の外"); return Catalog.Goods[id].Name; }))))).ToList();
        Assert.True(targets.SequenceEqual(ExpectedTargetResources),
            "能動侵略の標的の賦存（植民地ごとの財の名前の並び）が表と違う：" + string.Join("、", targets.Select(t => $"(\"{t.Colony}\", \"{t.Goods}\")")));
        string Show(SortedDictionary<string, int> d) => string.Join("、", d.Select(kv => $"(\"{kv.Key}\", {kv.Value})"));
        Assert.True(res.OrderBy(kv => kv.Key, StringComparer.Ordinal).SequenceEqual(ExpectedResourceCounts.Select(e => new KeyValuePair<string, int>(e.Good, e.Count)).OrderBy(kv => kv.Key, StringComparer.Ordinal)),
            "賦存の個数（財の名前ごと）が表と違う：" + Show(res));
        Assert.True(cul.OrderBy(kv => kv.Key, StringComparer.Ordinal).SequenceEqual(ExpectedCulturalGoodNations.Select(e => new KeyValuePair<string, int>(e.Good, e.Nations)).OrderBy(kv => kv.Key, StringComparer.Ordinal)),
            "象徴的主食の国の数（財の名前ごと）が表と違う：" + Show(cul));
    }
}
