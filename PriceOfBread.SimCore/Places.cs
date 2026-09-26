using System;
using System.Collections.Generic;
using System.IO;

namespace PriceOfBread.SimCore;

/// <summary>★場所の一元表（2026-08-26・作者指示(1)「州情報が複数箇所にある問題を一元化する」）。
///
/// ── なぜ要るか（同じ場所の座標が3箇所にあった）──────────────────────────────
/// 旧は同じ町の緯度経度が **Climate.CityCoord（255件・switch）／Geo.ColonyCoord（43件・switch）／
/// App.MapData.CityCoords（258件・Dictionary）** の3表にあり、それぞれ別に手で維持されていた。
/// 実害の記録：
///   ・MapData.cs:30 の注記「陸グラフには居るのに**この表だけに無かった**15町（作者報告『ボルドーからの線が途切れている』）」
///   ・旧 Climate.CityCoord の注記「Geo.ColonyCoord と同値・**両表を対で維持すること**」＝手で守るしかない不変条件だった
///   ・実測（2026-08-26）：Climate と Geo で **蝦夷地だけ値が違っていた**（43.06,141.35 と 43.1,141.3）
/// 登録漏れは例外も警告も出さず既定 (0,0)＝ギニア湾の海上に州が立つ（旧 Climate.CityCoord の既定と同じ）ので、
/// **表が増えるほど静かに壊れる**構造だった。
///
/// ── 何を真実源にしたか ────────────────────────────────────────────────────
/// <c>places.json</c> ただ1つ。name / lat / lon / kind（state・colony・frontier・target）。
/// ⚠ここは**座標の**唯一の出所である。陸塊（landmass.json）・沿岸（world.json の provinces.coastal）は
///   それぞれ別の一次データが既にあるので**写さない**（写した瞬間に同じ病が再発する）。
/// ★(3) の地形（森林・山岳・運河・不毛）はこの表に列を足す＝「1枚の表」（作者指示）。</summary>
public static class Places
{
    /// <summary>Terrain＝地形タグ（設計doc 20260826_地形と輸送コスト.DONE.md の7タグ）。空＝平地（既定）。
    /// ★(3)「1枚の表」＝地形はこの表の1列（作者指示）。清算は常に読む（旗 TerrainCostEnabled は 2026-09-26 に焼き込み）。</summary>
    public sealed record Place(string Name, double Lat, double Lon, string Kind, string Terrain);

    /// <summary>州を敷いたときの**最長の間隔**（km・作者指定 2026-08-27「州数1.5倍相当＝約850kmまで」）。
    /// ⚠これは座標表の生成規約そのもの＝「この距離より遠くに州は無い」。同じ規約を読む側（地図の政治塗り分けの
    /// 到達上限＝<c>MapFill</c>）はこの1本を参照する＝塗りが**未保有の州の領分**へはみ出さないことが規約で保証される
    /// （作者裁定 2026-09-02）。値を変えるときは places.json の生成規約と一緒に動かすこと。</summary>
    public const double MaxSpacingKm = 850.0;

    private static readonly Dictionary<string, Place> ByName = Load();
    public static IReadOnlyCollection<Place> All => ByName.Values;

    private static Dictionary<string, Place> Load()
    {
        var asm = typeof(Places).Assembly;
        string? res = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("places.json", StringComparison.Ordinal));
        if (res is null) throw new InvalidOperationException("places.json が埋め込まれていない。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(res)!;
        using var rd = new StreamReader(st);
        var rows = System.Text.Json.JsonSerializer.Deserialize<Row[]>(rd.ReadToEnd(), WorldData.Options)
                   ?? throw new InvalidOperationException("places.json の読込に失敗");
        var d = new Dictionary<string, Place>(rows.Length);
        // ★fail-open是正（監査G10#1）：同名行が2つあると後勝ちで黙って上書き＝座標の唯一の真実源で重複が検出されない。
        //   TryAdd＋例外で「後勝ち」を許さない＝現データに重複が無ければ挙動不変（実測0件・places.json 338件）。
        foreach (var r in rows)
            if (!d.TryAdd(r.Name, new Place(r.Name, r.Lat, r.Lon, r.Kind, r.Terrain)))
                throw new InvalidOperationException($"places.json の重複: {r.Name}");
        return d;
    }

    private sealed class Row
    {
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Kind { get; set; } = "";
        public string Terrain { get; set; } = "";   // 空＝平地
    }

    /// <summary>名前→地形タグ（enum）。未登録・空欄は平地。</summary>
    public static Terrain TerrainOf(string name)
        => ByName.TryGetValue(name, out var p) ? p.Terrain switch
        {
            "water_major" => SimCore.Terrain.WaterMajor,
            "water_minor" => SimCore.Terrain.WaterMinor,
            "hill" => SimCore.Terrain.Hill,
            "forest" => SimCore.Terrain.Forest,
            "mountain" => SimCore.Terrain.Mountain,
            "barren" => SimCore.Terrain.Barren,
            _ => SimCore.Terrain.Plain,
        } : SimCore.Terrain.Plain;

    /// <summary>名前→座標。⚠未登録は (0,0)＝**旧 Climate.CityCoord の既定と同じ**（例外を投げない）。
    /// 0,0 はギニア湾の海上なので、登録漏れは「海の上の州」として現れる＝挙動は旧と厳密に同じ。</summary>
    public static (double Lat, double Lon) Coord(string name)
        => ByName.TryGetValue(name, out var p) ? (p.Lat, p.Lon) : (0, 0);
}
