using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PriceOfBread.SimCore;

/// <summary>
/// 世界データ（国家・州・植民地）の外部化（データ駆動・287州世界の基盤）。
/// 真実源は埋め込みリソース <c>world.json</c>。<see cref="NationConfig"/> を直接（全フィールド自動）シリアライズ。
/// 州は <see cref="ProvinceSpec"/> record（名前付き自動）、植民地タプルだけ小 DTO 経由の converter で named JSON 化＝取りこぼしのない挙動同値。
/// データロードは世界構築時のみ（決定論コアの外）＝主系列 RNG に無関係＝決定論不変。
/// </summary>
public static class WorldData
{
    // 州は ProvinceSpec record＝System.Text.Json が {city,share,resources,culture,religion} を名前付きで自動シリアライズ（専用 converter 不要）。
    // 植民地タプル (Name, Resources[], PopK) を {name,resources,popK} へ。★(227)：popK＝1780年代の住民数〔千人〕（旧 population＝ウェイト×500）。
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

    // === 国際層の初期条件（§6.1）＝データ化（旧 MultiNationSim コンストラクタのハードコード表）。world.json に本節があればそれが真実源、無ければ BootstrapSetup（コード）へフォールバック＝常に動作。===
    internal sealed record TargetDto(string Name, double Military, bool Quinine, ColonyDto[] Colonies);   // 能動侵略の抽象標的（Nation=null・Gdp=0）。★2026-09-30：財布 Treasury を削除（戦費の Fund ごと読み手0・支払いの段3-2）
    internal sealed record RivalryDto(string A, string B, double Tension);          // 初期史実ライバルの緊張（BaseName 対）
    internal sealed record DependencyDto(string Suzerain, string Dependent);        // 初期史実従属（宗主→従属）
    // 巨大建造物§4-1（2026-07-28 裁定）：CanalKey enum を廃止し BuildCost/TechGate を**データ側**へ移した。
    // 旧方針「BuildCost/TechGate は Coefficients が真実源＝JSON に値を複製しない」は**運河が2個体しかない前提**で成立していたもので、
    // 個体が増えると係数表が個体台帳になる。個体別の値は資源賦存と同じデータ扱いにするのが筋（同ファイル上の
    // 「旧 MultiNationSim コンストラクタのハードコード表をデータ化」という前例と整合）。個体に依らない**率**（CanalInvestFrac 等）は係数側に残す。
    internal sealed record ChokepointDto(string Id, string Name, string Host, bool IsOpen, bool IsCanal, double BuildCost, double TechGate, Basin[] ServesBasins, string? ExitOfBasin = null);   // ExitOfBasin＝この咽喉が唯一の出口である閉鎖海盆（Basin名・null=該当なし）。13l地政学＝RouteTension の入力・地理の定数
    internal sealed record WorldSetup(IReadOnlyList<TargetDto> Targets, IReadOnlyList<RivalryDto> Rivalries, IReadOnlyList<DependencyDto> Dependencies, IReadOnlyList<ChokepointDto> Chokepoints);

    /// <summary>シリアライズ設定：public フィールドを含め、enum は文字列、camelCase、タプル配列は専用 converter。</summary>
    public static readonly JsonSerializerOptions Options = BuildOptions();
    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // 日本語を \uXXXX でなくそのまま＝編集しやすい
        };
        o.Converters.Add(new JsonStringEnumConverter());
        o.Converters.Add(new ColonyArrayConverter());
        return o;
    }

    /// <summary>現 C# 設定群を JSON 化（一時 dump 用・自動生成＝C#とビット等価の world.json を作る）。国際層の初期条件（Setup）も同梱。</summary>
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

    /// <summary>埋め込み world.json をロード（型初期化時に1回・全セクション共有）。★static readonly＝遅延キャッシュはバッチの並列実行でデータ競合になる（CultureRealms.Rules と同じ理由）。</summary>
    private static readonly WorldDoc _doc = LoadDoc();
    private static WorldDoc LoadDoc()
    {
        var asm = typeof(WorldData).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("world.json", System.StringComparison.Ordinal));
        if (name is null) throw new System.InvalidOperationException("world.json（埋め込みリソース）が見つかりません。SimCore.csproj の <EmbeddedResource> と名前空間を確認。");
        using var s = asm.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<WorldDoc>(s, Options) ?? throw new System.InvalidOperationException("world.json のデシリアライズに失敗");
    }

    // === 巨大建造物のカタログ（megaprojects.json・埋め込み）＝個体別の値はデータが真実源 ===
    internal sealed record MegaprojectDto(string Id, string Name, string Host, double BuildUnits, double TechGate,
                                          string EffectTarget, string EffectMode, double EffectValue);
    private sealed record MegaprojectDoc(List<MegaprojectDto> Projects);
    /// <summary>埋め込み megaprojects.json から建造物カタログをロード（型初期化時に1回・goods.json と同じ二本立て構成）。</summary>
    private static readonly MegaprojectDto[] _megaCache = LoadMegaprojects();
    private static MegaprojectDto[] LoadMegaprojects()
    {
        var asm = typeof(WorldData).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("megaprojects.json", System.StringComparison.Ordinal));
        if (name is null) return [];   // 未同梱でも動く＝機構ごと不発（データが無い＝不発の原則）
        using var s = asm.GetManifestResourceStream(name)!;
        return [.. (JsonSerializer.Deserialize<MegaprojectDoc>(s, Options)?.Projects ?? [])];
    }
    internal static MegaprojectDto[] Megaprojects() => _megaCache;

    /// <summary>埋め込み world.json から全国家設定（型初期化時に1回）。</summary>
    private static readonly NationConfig[] _cache = [.. _doc.Nations];
    public static NationConfig[] Nations() => _cache;

    /// <summary>国際層の初期条件（抽象標的・ライバル・従属・咽喉）。world.json に本節があればそれを、無ければ BootstrapSetup（コード）を返す＝未移行の world.json でも厳密に同挙動。</summary>
    internal static WorldSetup Setup()
    {
        if (_doc.AbstractTargets.Count == 0) return BootstrapSetup();   // 旧 world.json（nations のみ）への後方互換フォールバック
        return new WorldSetup(_doc.AbstractTargets, _doc.Rivalries, _doc.Dependencies, _doc.Chokepoints);
    }

    /// <summary>国際層初期条件のコード定義（真実源の種＝dumpworld で world.json に書き出す。旧 MultiNationSim コンストラクタのリテラルを値据え置きで移設）。順序厳守＝Power.Id は Powers.Count で連番付与されるため配列順が挙動を決める。</summary>
    internal static WorldSetup BootstrapSetup() => new(
        [   // 抽象標的（AddTarget 順）：目玉資源は被りを避ける（史実＋やや誇張）
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
        [   // 初期史実ライバルの緊張（SeedRivalries 順）
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
        [   // 初期史実従属（SeedDependencies 順）
            new DependencyDto("シャム", "カンボジア"),
            new DependencyDto("シャム", "ラオス"),
            new DependencyDto("マラータ同盟", "デリー"),
        ],
        [   // 海峡・運河（自然海峡は開通済み、運河は host 保有国が建設して開通）
            new ChokepointDto("gibraltar", "ジブラルタル海峡", "ジブラルタル", true, false, 0, 0, [Basin.Mediterranean, Basin.BlackSea], ExitOfBasin: "Mediterranean"),
            // ★2026-08-10（作者裁定「完全に重なっている州は統合したい」）：host を植民地「ボスポラス」から**オスマンの州イスタンブール**へ。
            // 旧データはイスタンブール（41.01,28.98）と距離0.0kmの双子の植民地を置いており、州昇格機構（2026-08-09）が
            // 「ボスポラスがオスマンの州へ昇格（入植者101…）」＝帝国の中枢に入植者が入る絵を出していた。
            // スエズ（host="カイロ"＝州）と同型＝扼者の host は州でも解ける（ResolveOwner が植民地→州の順で引く）。
            new ChokepointDto("bosporus", "ボスポラス海峡", "イスタンブール", true, false, 0, 0, [Basin.BlackSea], ExitOfBasin: "BlackSea"),
            new ChokepointDto("malacca", "マラッカ海峡", "マレー", true, false, 0, 0, [Basin.Pacific]),
            new ChokepointDto("suez", "スエズ運河", "カイロ", false, true, 150_000, 2.5, [Basin.IndianOcean, Basin.Pacific]),
            new ChokepointDto("panama", "パナマ運河", "パナマ", false, true, 400_000, 3.0, [Basin.Pacific]),
            // 07_再設計 第3弾3c（2026-07-31）：passages.json に手置き済みの通路2本を**扼者**として実国家に紐づける。
            // ズンド＝デンマークのズンド通行税（〜1857）＝バルト海の出入りを握る。喜望峰＝スエズ以前の唯一の東方航路＝ケープ争奪（1795/1806）がゲームになる。
            new ChokepointDto("oresund", "ズンド海峡", "コペンハーゲン", true, false, 0, 0, [Basin.Baltic], ExitOfBasin: "Baltic"),
            new ChokepointDto("cape", "喜望峰", "ケープ植民地", true, false, 0, 0, [Basin.IndianOcean, Basin.Pacific]),
        ]);

    /// <summary>名前で国家設定を取得（薄いローダ＝既存 NationConfig.France() 等が委譲する）。</summary>
    public static NationConfig ByName(string name)
        => System.Array.Find(Nations(), n => n.Name == name)
           ?? throw new System.InvalidOperationException($"world.json に国家「{name}」が見つかりません");
}
