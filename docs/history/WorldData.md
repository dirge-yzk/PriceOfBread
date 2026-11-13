# PriceOfBread.SimCore/WorldData.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L7-12 W 直後 `public static class WorldData`
````
/// <summary>
/// 世界データ（国家・州・植民地）の外部化（データ駆動・287州世界の基盤）。
/// 真実源は埋め込みリソース <c>world.json</c>。<see cref="NationConfig"/> を直接（全フィールド自動）シリアライズ。
/// 州は <see cref="ProvinceSpec"/> record（名前付き自動）、植民地タプルだけ小 DTO 経由の converter で named JSON 化＝取りこぼしのない挙動同値。
/// データロードは世界構築時のみ（決定論コアの外）＝主系列 RNG に無関係＝決定論不変。
/// </summary>
````

## L15-16 W 直後 `internal sealed record ColonyDto(string Name, int[] Resources, double PopK);`
````
// 州は ProvinceSpec record＝System.Text.Json が {city,share,resources,culture,religion} を名前付きで自動シリアライズ（専用 converter 不要）。
// 植民地タプル (Name, Resources[], PopK) を {name,resources,popK} へ。★(227)：popK＝1780年代の住民数〔千人〕（旧 population＝ウェイト×500）。
````

## L29-29 W 直後 `internal sealed record TargetDto(string Name, double Military, bool Quinine, ColonyDto[] C`
````
// === 国際層の初期条件（§6.1）＝データ化（旧 MultiNationSim コンストラクタのハードコード表）。world.json に本節があればそれが真実源、無ければ BootstrapSetup（コード）へフォールバック＝常に動作。===
````

## L30 T `internal sealed record TargetDto(string Name, double Military, bool Quinine, ColonyDto[] C`
````
// 能動侵略の抽象標的（Nation=null・Gdp=0）。★2026-09-30：財布 Treasury を削除（戦費の Fund ごと読み手0・支払いの段3-2）
````

## L31 T `internal sealed record RivalryDto(string A, string B, double Tension);`
````
// 初期史実ライバルの緊張（BaseName 対）
````

## L32 T `internal sealed record DependencyDto(string Suzerain, string Dependent);`
````
// 初期史実従属（宗主→従属）
````

## L33-36 W 直後 `internal sealed record ChokepointDto(string Id, string Name, string Host, bool IsOpen, boo`
````
// 巨大建造物§4-1（2026-07-28 裁定）：CanalKey enum を廃止し BuildCost/TechGate を**データ側**へ移した。
// 旧方針「BuildCost/TechGate は Coefficients が真実源＝JSON に値を複製しない」は**運河が2個体しかない前提**で成立していたもので、
// 個体が増えると係数表が個体台帳になる。個体別の値は資源賦存と同じデータ扱いにするのが筋（同ファイル上の
// 「旧 MultiNationSim コンストラクタのハードコード表をデータ化」という前例と整合）。個体に依らない**率**（CanalInvestFrac 等）は係数側に残す。
````

## L37 T `internal sealed record ChokepointDto(string Id, string Name, string Host, bool IsOpen, boo`
````
// ExitOfBasin＝この咽喉が唯一の出口である閉鎖海盆（Basin名・null=該当なし）。13l地政学＝RouteTension の入力・地理の定数
````

## L40-40 W 直後 `public static readonly JsonSerializerOptions Options = BuildOptions();`
````
/// <summary>シリアライズ設定：public フィールドを含め、enum は文字列、camelCase、タプル配列は専用 converter。</summary>
````

## L50 T `Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,`
````
// 日本語を \uXXXX でなくそのまま＝編集しやすい
````

## L57-57 W 直後 `public static string Serialize(IReadOnlyList<NationConfig> nations)`
````
/// <summary>現 C# 設定群を JSON 化（一時 dump 用・自動生成＝C#とビット等価の world.json を作る）。国際層の初期条件（Setup）も同梱。</summary>
````

## L80-80 W 直後 `private static readonly WorldDoc _doc = LoadDoc();`
````
/// <summary>埋め込み world.json をロード（型初期化時に1回・全セクション共有）。★static readonly＝遅延キャッシュはバッチの並列実行でデータ競合になる（CultureRealms.Rules と同じ理由）。</summary>
````

## L91-91 W 直後 `private static readonly NationConfig[] _cache = [.. _doc.Nations];`
````
/// <summary>埋め込み world.json から全国家設定（型初期化時に1回）。</summary>
````

## L95-95 W 直後 `internal static WorldSetup Setup()`
````
/// <summary>国際層の初期条件（抽象標的・ライバル・従属・咽喉）。world.json に本節があればそれを、無ければ BootstrapSetup（コード）を返す＝未移行の world.json でも厳密に同挙動。</summary>
````

## L98 T `if (_doc.AbstractTargets.Count == 0) return BootstrapSetup();`
````
// 旧 world.json（nations のみ）への後方互換フォールバック
````

## L102-102 W 直後 `internal static WorldSetup BootstrapSetup() => new(`
````
/// <summary>国際層初期条件のコード定義（真実源の種＝dumpworld で world.json に書き出す。旧 MultiNationSim コンストラクタのリテラルを値据え置きで移設）。順序厳守＝Power.Id は Powers.Count で連番付与されるため配列順が挙動を決める。</summary>
````

## L104 T `[`
````
// 抽象標的（AddTarget 順）：目玉資源は被りを避ける（史実＋やや誇張）
````

## L116 T `[`
````
// 初期史実ライバルの緊張（SeedRivalries 順）
````

## L129 T `[`
````
// 初期史実従属（SeedDependencies 順）
````

## L134 T `[`
````
// 海峡・運河（自然海峡は開通済み、運河は host 保有国が建設して開通）
````

## L136-139 W 直後 `new ChokepointDto("bosporus", "ボスポラス海峡", "イスタンブール", true, false, 0, 0, [Basin.BlackSea], E`
````
// ★2026-08-10（作者裁定「完全に重なっている州は統合したい」）：host を植民地「ボスポラス」から**オスマンの州イスタンブール**へ。
// 旧データはイスタンブール（41.01,28.98）と距離0.0kmの双子の植民地を置いており、州昇格機構（2026-08-09）が
// 「ボスポラスがオスマンの州へ昇格（入植者101…）」＝帝国の中枢に入植者が入る絵を出していた。
// スエズ（host="カイロ"＝州）と同型＝扼者の host は州でも解ける（ResolveOwner が植民地→州の順で引く）。
````

## L144-145 W 直後 `new ChokepointDto("oresund", "ズンド海峡", "コペンハーゲン", true, false, 0, 0, [Basin.Baltic], ExitOf`
````
// 07_再設計 第3弾3c（2026-07-31）：passages.json に手置き済みの通路2本を**扼者**として実国家に紐づける。
// ズンド＝デンマークのズンド通行税（〜1857）＝バルト海の出入りを握る。喜望峰＝スエズ以前の唯一の東方航路＝ケープ争奪（1795/1806）がゲームになる。
````

## L150-150 W 直後 `public static NationConfig ByName(string name)`
````
/// <summary>名前で国家設定を取得（薄いローダ＝既存 NationConfig.France() 等が委譲する）。</summary>
````

