using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PriceOfBread.SimCore;

public static class WorldData
{
    internal sealed record ColonyDto(string Name, int[] Resources, double PopK);
    private sealed class ColonyArrayConverter : JsonConverter<(string Name, int[] Resources, double PopK)[]>
    {
        public override (string Name, int[] Resources, double PopK)[] Read(ref Utf8JsonReader r, System.Type t, JsonSerializerOptions o)
        {
            var dtos = JsonSerializer.Deserialize<ColonyDto[]>(ref r, o) ?? [];
            return System.Array.ConvertAll(dtos, d => (d.Name, d.Resources, d.PopK));
        }
        public override void Write(Utf8JsonWriter w, (string Name, int[] Resources, double PopK)[] v, JsonSerializerOptions o)
            => JsonSerializer.Serialize(w, System.Array.ConvertAll(v, t => new ColonyDto(t.Name, t.Resources, t.PopK)), o);
    }

    internal sealed record TargetDto(string Name, double Military, bool Quinine, ColonyDto[] Colonies);
    internal sealed record RivalryDto(string A, string B, double Tension);
    internal sealed record DependencyDto(string Suzerain, string Dependent);
    internal sealed record ChokepointDto(string Id, string Name, string Host, bool IsOpen, bool IsCanal, double BuildCost, Basin[] ServesBasins, string? ExitOfBasin = null, string? Requires = null);
    internal sealed record WorldSetup(IReadOnlyList<TargetDto> Targets, IReadOnlyList<RivalryDto> Rivalries, IReadOnlyList<DependencyDto> Dependencies, IReadOnlyList<ChokepointDto> Chokepoints);

    public static readonly JsonSerializerOptions Options = BuildOptions();
    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        o.Converters.Add(new JsonStringEnumConverter());
        o.Converters.Add(new ColonyArrayConverter());
        return o;
    }

    public static string Serialize(IReadOnlyList<NationConfig> nations)
    {
        var setup = Setup();
        return JsonSerializer.Serialize(new WorldDoc
        {
            Nations = [.. nations],
            AbstractTargets = [.. setup.Targets],
            Rivalries = [.. setup.Rivalries],
            Dependencies = [.. setup.Dependencies],
            Chokepoints = [.. setup.Chokepoints],
        }, Options);
    }

    private sealed class WorldDoc
    {
        public List<NationConfig> Nations { get; set; } = [];
        public List<TargetDto> AbstractTargets { get; set; } = [];
        public List<RivalryDto> Rivalries { get; set; } = [];
        public List<DependencyDto> Dependencies { get; set; } = [];
        public List<ChokepointDto> Chokepoints { get; set; } = [];
    }

    private static readonly WorldDoc _doc = LoadDoc();
    private static WorldDoc LoadDoc()
    {
        var asm = typeof(WorldData).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("world.json", System.StringComparison.Ordinal));
        if (name is null) throw new System.InvalidOperationException("world.json（埋め込みリソース）が見つかりません。SimCore.csproj の <EmbeddedResource> と名前空間を確認。");
        using var s = asm.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<WorldDoc>(s, Options) ?? throw new System.InvalidOperationException("world.json のデシリアライズに失敗");
    }

    private static readonly NationConfig[] _cache = [.. _doc.Nations];
    public static NationConfig[] Nations() => _cache;

    internal static WorldSetup Setup()
    {
        if (_doc.AbstractTargets.Count == 0) return BootstrapSetup();
        return new WorldSetup(_doc.AbstractTargets, _doc.Rivalries, _doc.Dependencies, _doc.Chokepoints);
    }

    internal static WorldSetup BootstrapSetup() => new(
        [
            new TargetDto("琉球", 0.1, false, [new ColonyDto("琉球", [Catalog.SugarCaneId, Catalog.DyeId, Catalog.FishId], 250)]),
            new TargetDto("アルタ=カリフォルニア", 0.4, false, [new ColonyDto("アルタ=カリフォルニア", [Catalog.GoldOreId, Catalog.WheatId, Catalog.LivestockId, Catalog.TimberId], 750)]),
            new TargetDto("ボルネオ", 0.5, false, [new ColonyDto("ボルネオ", [Catalog.RawRubberId, Catalog.CrudeOilId, Catalog.SpiceId, Catalog.TimberId], 1000)]),
            new TargetDto("マレー", 0.4, false, [new ColonyDto("マレー", [Catalog.SpiceId, Catalog.RawRubberId], 750)]),
            new TargetDto("バルバリ", 0.6, false, [new ColonyDto("アルジェ", [Catalog.WheatId, Catalog.FruitId, Catalog.LivestockId], 1500)]),
            new TargetDto("アシャンティ", 0.6, true, [new ColonyDto("アシャンティ", [Catalog.GoldOreId, Catalog.CocoaId, Catalog.PalmOilId], 1000)]),
            new TargetDto("ソコト", 0.6, true, [new ColonyDto("ソコト", [Catalog.DyeId, Catalog.LivestockId, Catalog.IvoryId], 1500)]),
            new TargetDto("コンゴ", 0.6, true, [new ColonyDto("コンゴ", [Catalog.RawRubberId, Catalog.IvoryId, Catalog.CopperOreId, Catalog.PalmOilId], 1500)]),
            new TargetDto("南アフリカ", 0.5, false, [new ColonyDto("南アフリカ", [Catalog.GoldOreId, Catalog.CopperOreId, Catalog.LivestockId], 1000)]),
            new TargetDto("パナマ", 0.4, false, [new ColonyDto("パナマ", [Catalog.TimberId], 500)]),
        ],
        [
            new RivalryDto("フランス", "イギリス", 50),
            new RivalryDto("イギリス", "アメリカ", 40),
            new RivalryDto("フランス", "日本", 5),
            new RivalryDto("日本", "アメリカ", 15),
            new RivalryDto("ロシア", "オスマン", 60),
            new RivalryDto("イギリス", "ロシア", 45),
            new RivalryDto("プロイセン", "フランス", 45),
            new RivalryDto("プロイセン", "ロシア", 30),
            new RivalryDto("イギリス", "清", 55),
            new RivalryDto("ロシア", "清", 50),
            new RivalryDto("フランス", "清", 40),
        ],
        [
            new DependencyDto("シャム", "カンボジア"),
            new DependencyDto("シャム", "ラオス"),
            new DependencyDto("マラータ同盟", "デリー"),
        ],
        [
            new ChokepointDto("gibraltar", "ジブラルタル海峡", "ジブラルタル", true, false, 0, [Basin.Mediterranean, Basin.BlackSea], ExitOfBasin: "Mediterranean"),
            new ChokepointDto("bosporus", "ボスポラス海峡", "イスタンブール", true, false, 0, [Basin.BlackSea], ExitOfBasin: "BlackSea"),
            new ChokepointDto("malacca", "マラッカ海峡", "マレー", true, false, 0, [Basin.Pacific]),
            new ChokepointDto("suez", "スエズ運河", "カイロ", false, true, 150_000, [Basin.IndianOcean, Basin.Pacific]),
            new ChokepointDto("panama", "パナマ運河", "パナマ", false, true, 400_000, [Basin.Pacific]),
            new ChokepointDto("oresund", "ズンド海峡", "コペンハーゲン", true, false, 0, [Basin.Baltic], ExitOfBasin: "Baltic"),
            new ChokepointDto("cape", "喜望峰", "ケープ植民地", true, false, 0, [Basin.IndianOcean, Basin.Pacific]),
        ]);

    public static NationConfig ByName(string name)
        => System.Array.Find(Nations(), n => n.Name == name)
           ?? throw new System.InvalidOperationException($"world.json に国家「{name}」が見つかりません");
}
