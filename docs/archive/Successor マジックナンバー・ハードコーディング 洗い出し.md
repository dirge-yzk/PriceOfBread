# Successor マジックナンバー・ハードコーディング 洗い出し（2026-07-20）

多エージェント静的スキャン（17並列）で SimCore 全体＋App のロジックを走査し、まだ `Coefficients`/`Catalog`/データファイルに外部化されていない裸の数値リテラル・ハードコードを列挙した。

> **【実施済み・2026-07-20】この一覧をもとに約394箇所を挙動不変（値据え置き・命名/集約のみ）で命名置換した。**
> 新設＝`Calendar.cs`（90/89/30/360/3/4/12/StartYear/EndYear）・`PopClass.cs`（Noble0…Servant12 の ClassId 定数）・`Catalog` good-id 定数18個（Salt/Tea/Rice/汽船/線路 等）＋各ファイル局所 `private const` 約200個。
> 検証＝169テスト緑・calib 33/33/33・leakhunt 清潔＋全 const 抽出値が本doc原本リテラルと一致（挙動不変を確認）。
> **見送り（skip）約108件**＝イプシロン(1e-9等)・番兵(-1/1e8)・自明値(0/0.5/1)・国名/座標のデータ表・enum序数の構造変更・中央定数の無い good-id レシピ配列（「過小置換は可・破壊は不可」で保守的に据え置き）。データ配列/名前の enum化・データ外部化はより大きな別タスク。

**スコープ**：シミュレーションロジックの調整値・同定（ID/名前/索引）・暦定数・重複リテラル。除外＝0/1/0.5・イプシロン(1e-9等)・自明な単位・**UIの純presentational値（色hex・余白・フォント・Grid列定義）**（別カテゴリのため今回は対象外。必要なら別途洗い出し可）。

---

## 1. 概要

Successor は既に Coefficients.cs（約800係数）・goods.json・world.json で大半の定数を外部化し、倍率排除キャンペーンで条件付き補正の実数化を進めてきた。今回の横断監査で残存する裸のマジックナンバーは388件（high 87・medium 204・low 97）。その分布が示すのは、外部化が「値の一元管理」までは到達した一方で、「意味の命名」と「参照の単一化」には未達だという構造である。最大塊の tunable-coefficient 190件は、Coefficients に器はあるのに世界価格クランプ（InternationalMarket 0.85/1.18/0.1/10.0）や生産性スケール（Simulation 7.0/5.0/4.0/3.0）、限界費用（0.06/0.34）など式の内側に取り残された調整値で、国内価格側は coef 化済みなのに世界価格側だけ裸、といった非対称が較正の見通しを曇らせている。

第二の問題群は「識別子の生リテラル化」である。hardcoded-id 48件は good-id（塩6・茶74・米102・電力87・化学薬品37…）や ClassId が式や配列に直書きされ、しかも塩6/茶74は独占レント判定で3箇所、電力87・化学37は多数のレシピに散在する。hardcoded-name 25件は国名・チョークポイント名・運河名・都市名・文化/宗教ラベルへの文字列一致でロジックが動いており（"スエズ"/"パナマ" の Contains 判定、列強名や初期緊張値の直書き、Diplomacy の文化→民族/宗教→ファミリー分類テーブル）、リネーム機構やデータ駆動化と正面衝突する correctness リスクを抱える。hardcoded-index 25件は首都=Regions[0]/Id==0 規約や Ideology enum 序数への直接添字（s[0]+s[1]+s[4]…）で、enum やレイアウト変更に暗黙結合している。

残りは重複と暦。duplicated-literal 53件（10_000.0 の基準距離、0.06/0.34 の限界費用、Output=0.5/Scale=0.05 の初期値群、穀物法 0.5/1.0 の二重定義、成人女性比テーブルの複製など）と calendar-constant 44件（90=四半期日数、89=四半期末索引、30=月、360=年、3=四半期の月数、12.0=年→月、1783/1914 年号）は、一箇所の名前付き定数に集約すれば意味が自明化し、変更時の取りこぼしが消える。一掃の意義は較正値の削減そのものより、(1) good-id/名前/enum序数の散在を単一の Catalog 定数・enum・命名 const に束ねて correctness の地雷を除去し、(2) 較正対象の tunable を Coefficients に集めて「どの値を動かせば何が変わるか」の探索面を明示化することにある。コードの挙動は不変（値は据え置いて命名・集約のみ）で進められる、リスクの低い機械的リファクタが中心である。

## 2. 集計（総 388件）

### カテゴリ別

| カテゴリ | 件数 |
|---|--:|
| ハードコードID（財 good-id・ClassId の裸リテラル） | 48 |
| ハードコード名（国名/州名/財名/文明などの文字列比較） | 25 |
| 固定インデックス（Nations[2]=英 等の魔法索引） | 25 |
| 重複リテラル（同値が複数箇所に散在＝一箇所に集約すべき） | 53 |
| 暦定数（3=月/四半期・4・90・360・年号） | 44 |
| 調整係数（Coefficients へ移すべき裸の率/閾値/倍率/重み/上限） | 190 |
| その他 | 3 |

### 優先度別

| 優先度 | 件数 |
|---|--:|
| high | 87 |
| medium | 204 |
| low | 97 |

## 3. 掃除方針（カテゴリ別）

### ハードコードID（財 good-id・ClassId の裸リテラル）

- **なぜ問題か**：good-id/ClassId の生リテラル（塩6・茶74・米102・電力87・化学37・職人工房57/59/70/71 等）が式・分岐・レシピ配列に直書きされ、しかも同一IDが複数ファイル・複数行に散在する。goods.json の並び替えや財の追加で全参照が静かに壊れ、追跡不能なバグ源になる。独占レント（6/74）は3箇所で同じ判定を繰り返す。
- **掃除の手順**：Catalog の名前付き good-id 定数（CoalId 形式）を欠けている財ぶん拡充し、SaltId/TeaId/RiceId/ElectricityId/ChemicalsId 等を新設。EconomyStep の独占判定 gi==6||gi==74 は Catalog.IsLegalMonopolyGood(id) のような述語に集約して3箇所を単一化。レシピ投入の裸ID配列は Catalog 定数参照に置換。ClassId リテラル（1=農民/2=労働者/8/12）は ClassId enum もしくは Catalog 定数へ。値は不変、参照名の差し替えのみ。

### ハードコード名（国名/州名/財名/文明などの文字列比較）

- **なぜ問題か**：国名・運河名・チョークポイント名・都市名・文化/宗教ラベルへの文字列一致でロジックが分岐する（"スエズ"/"パナマ" の Contains、列強名の直書き、初期従属や初期緊張の名前依存、文化→民族/宗教→ファミリー分類テーブル約40件）。リネーム機構やデータ駆動の建国/変名と正面衝突し、日本語表記の揺れ一つで無言破綻する correctness 級リスク。同一チョークポイント文字列は6ファイルに散在。
- **掃除の手順**：チョークポイント/運河は種別 enum（CanalKind.Suez/Panama 等）か world.json のフラグ属性を持たせ、Name.Contains 判定を型/ID判定へ置換。列強・初期従属・初期緊張はデータファイル（world.json のシード節）へ外部化し、コードは名前でなく識別子で引く。Diplomacy の文化→民族/宗教→ファミリー分類テーブルはコードから静的辞書もしくはデータへ切り出し、参照点を一本化。Factions の変異語幹テーブルはコンテンツとしてデータ側に置く。

### 調整係数（Coefficients へ移すべき裸の率/閾値/倍率/重み/上限）

- **なぜ問題か**：最大塊190件。式や分岐に直書きされた閾値/率/倍率/スケール/上限（世界価格クランプ0.85/1.18・床天井0.1/10.0、工業化スケール7.0、教師5.0/医療4.0/行政3.0、限界費用0.06/0.34、労働価格クランプ0.25/4.0、技術逓減2.0/0.3 等）。Coefficients という器があるのに式の内側に残り、国内価格は coef 化済みで世界価格だけ裸、という非対称が較正の探索面を隠す。どれを動かせば挙動が変わるかが不可視。
- **掃除の手順**：Coefficients.cs に意味のわかるフィールド名で移設（WorldPriceFloorFactor/WorldPriceCeilFactor/WorldPriceMaxDrop/MaxRise、IndustrializationScale、EducatorProductivity 等）。国内側 coef と対になる世界価格側を優先的に対称化。既存の calibration ハーネス（fidelity/batch）を回して値据え置きで挙動不変を確認しつつ移設。medium 中心なので一括ではなくファイル単位で束ね、レビュー可能な粒度で。

### 重複リテラル（同値が複数箇所に散在＝一箇所に集約すべき）

- **なぜ問題か**：同一意味の値が複数箇所に分散（10_000.0 基準距離が2箇所、限界費用 0.06/0.34 が3箇所、初期 Output=0.5/Scale=0.05 が多数レシピ、穀物法 0.5/1.0 が Trade と InternationalMarket に二重定義、成人女性比テーブルが BuildFrontier に複製、移民階級[1,2]が2箇所）。片方だけ直す事故が起き、意味の一致が保証されない。
- **掃除の手順**：意味単位で単一の名前付き const/Coefficients フィールドに集約し全参照を差し替え。穀物法保護強度マップは共有ヘルパへ。初期 Output/Scale はプロデューサ生成の共通ファクトリかデフォルト定数に。成人女性比テーブルは1本の配列定数を Build 系で共用。移民階級は MigrantClasses 定数を両所で参照。RNG seed ソルトは決定論に効くため値は据え置き、命名 const 化のみで散在を可視化。

### 暦定数（3=月/四半期・4・90・360・年号）

- **なぜ問題か**：暦の魔法数が全域に反復（90=四半期日数、89=四半期末索引、30=月、360=年、4=年の四半期数、3=四半期の月数、12.0=年→月換算、年号1783/1914）。Simulation/MultiNationSim/Diplomacy/EconomyStep に同じ値が独立して散在し、暦モデルを変えると取りこぼす。month%3==0 の四半期判定が多数。
- **掃除の手順**：Calendar 静的クラス（あるいは Coefficients の暦節）に DaysPerQuarter=90/QuarterEndDayIndex=89/DaysPerMonth=30/DaysPerYear=360/QuartersPerYear=4/MonthsPerQuarter=3/MonthsPerYear=12/StartYear=1783/EndYear=1914 を定義し全参照を置換。派生値（89=DaysPerQuarter-1、*3.0 の四半期→月）は式で表現。年号は世界初期化データと整合させる。

### 固定インデックス（Nations[2]=英 等の魔法索引）

- **なぜ問題か**：固定レイアウト前提の索引が correctness に直結。首都=Regions[0] かつ Id==0 の規約が Diplomacy/Politics/MultiNationSim に散在、Ideology enum 序数への直接添字（s[0]+s[1]+s[4]+s[8]+s[9] の権威群など）が CrownParliament/Parliament/Ideology で重複、Bloc 0/1/2 や服務ランク<3、党コード1/2 などマジック索引が多い。enum やレイアウトを変えると無言で誤分類。
- **掃除の手順**：首都判定は Nation.CapitalRegion プロパティ/ヘルパに集約し Regions[0]/Id==0 の直参照を廃す。Ideology 序数グルーピングは Ideology enum に分類メソッド（IsAuthoritarian/PartyFamily(id)）を持たせ、添字群を意味づけて一本化。Bloc/ServiceRank/党コードは enum 化。stackalloc[5] 等の固定容量は候補列挙元と結合した名前付き const に。

### その他

- **なぜ問題か**：分類外だが命名すべきプロトコル/センチネル定数。SaveGame の -1『新型マーカー』が writer/reader に散在（id>=0 判定と暗黙結合）、Territory の 1e8 耕地未設定センチネル、Meritocracy の stackalloc[5] 昇格候補上限。意味が命名されず、直列化フォーマットや合成較正の前提が読み取れない。
- **掃除の手順**：SaveGame は NewTypeMarker=-1 を共有 const 化し writer/reader 双方で参照。Territory の 1e8 は UnconstrainedArableSentinel として命名し用途をコメント化。Meritocracy の 5 は PromotionTargetCount として昇格先列挙と紐付け。いずれも値据え置きで意味の明示のみ。

## 4. 着手順の推奨（高ROI順）

1. hardcoded-name の correctness 級（チョークポイント/運河の Name.Contains 判定・列強名・初期従属/緊張の名前依存）を最優先。リネーム/建国機構と衝突する無言破綻を止める効果が最大で、high 集中。
2. hardcoded-id の散在ID（塩6/茶74 独占判定3箇所・電力87・化学37 の多数レシピ）を Catalog 定数と述語に集約。goods.json 並び替え耐性を得る高ROI。
3. calendar-constant を Calendar クラスに一括集約。90/89/30/360/3/12 が全域反復で置換範囲が広く、機械的で低リスク・効果が全ファイルに波及。
4. duplicated-literal のうち二重定義（穀物法 0.5/1.0・限界費用 0.06/0.34・10_000.0）を単一化。片側だけ直す事故の芽を摘む。
5. hardcoded-index の首都規約と Ideology 序数グルーピングを enum メソッド/CapitalRegion に集約。enum 変更耐性を確保。
6. tunable-coefficient を Coefficients へ移設。件数最多だが挙動不変で急がない。世界価格クランプなど国内 coef と非対称なものから対称化し、fidelity/batch で確認しつつファイル単位で。
7. other（SaveGame -1・Territory 1e8・Meritocracy 5）と low 優先の境界的リテラルを最後に命名整理。

## 5. 一掃してはいけない/慎重にすべき値

- RNG seed ソルト（0xC0FFEE_D15EA5E 等の XOR ソルト、疫病/作物/F3変異の系列分離）は決定論の再現性に直結する。値は絶対に変えず、命名 const 化で散在を可視化するのみ。統合や値変更は既存シード互換を壊す。
- イプシロン/許容誤差（1e-9, 1e-6, 1e-12, 1e-3, 1e8 の境界ガード）は原則対象外。明らかに不整合に散在する場合のみ低優先で命名し、統一しての『まるめ』は数値挙動を変えうるので避ける。
- 自明値 0/1/-1/2（索引・増分・符号）、0.5（中点）、/2・*2、100.0/1000.0 の単位換算、書式指定（P0/F2）は flag 対象外。命名するとかえって読みにくくなるので触らない。
- tunable-coefficient の Coefficients 移設は『値据え置き』が鉄則。移設ついでの再較正はしない。挙動を変える較正は #17 の全係数有効化・創発再較正の枠で別途行い、掃除とは分離する。
- Territory の 1e8 センチネルや SaveGame の -1 マーカーは『魔法数に見えるが意味を持つ番兵』。数値を別値へ置換すると比較ロジック（id>=0 判定・未設定判定）が壊れるため、値は不変で命名のみ。
- 首都=Regions[0]/Id==0 は文書化済みの不変条件。CapitalRegion ヘルパへ集約する際、先頭挿入・Id 割当を行う全経路（Diplomacy/Politics/MultiNationSim の昇格・分離独立）を同時に通さないと不変条件を破る。段階的置換の途中状態に注意。
- hardcoded-name のデータ外部化で文字列を捨てる際、忠実度ハーネス（HistoricalReference）や BaseName/Name 分離が名前に依存していないか確認。変名しても忠実度が壊れない設計を維持する。

## 6. 詳細一覧

各行＝場所（file:line）／値／意味／移設先案。優先度→カテゴリ→ファイル順。

### 6.1 優先度 HIGH（優先・ハードコードID/名前/索引・散在重複・correctness系） ― 87件

#### ハードコードID（財 good-id・ClassId の裸リテラル）（47件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.Production.cs:209 | `2, 1, 4` | 労働pop→人数換算のClassId直書き（2=労働者/1=農民/4=上級労働者）。good-idはCatalog.LaborId等で命名済みなのにClassIdだけ裸 | Catalog.LaborClassId / FarmLaborClassId / AdvLaborClassId 定数 or ClassId enum |
| Successor.App/ViewModels/GameViewModel.Production.cs:434 | `6, 74` | 塩(6)/茶(74)の法的独占バッジ判定のgood-idリテラル（コメントにも「塩(6)/茶(74)」と明記） | Catalog.SaltId / Catalog.TeaId 名前付き定数 |
| Successor.SimCore/Climate.cs:21 | `15, 108, 102, 115, 116` | 主食の good-id（麦・野菜・米・ジャガイモ・トウモロコシ）を StapleSelfSufficient の switch に直書き。同じ主食idクラスタが WeatherExposure(275)・RgoBoosterGoods(264)・IsDiseaseProneCrop(293) 等に散在＝ファイル横断の重複ハードコードID | Catalog.WheatId/VegetableId/RiceId/PotatoId/MaizeId 定数（既存の CoalId 等と同型）＋ Catalog.StapleFoodIds[] 集合 |
| Successor.SimCore/Colonial.cs:24 | `const int coalG = 1, foodG = 65` | 石炭good-id(=Catalog.CoalId=1・既に外部化済み)と缶詰糧食good-id(=World.InvCannedId=65) | Catalog.CoalId を使用／Catalog.CannedFoodId(65) を新設 |
| Successor.SimCore/Colonial.cs:53 | `const int oilG = 10` | 原油good-id（B6重油二択の燃料代替） | Catalog.CrudeOilId |
| Successor.SimCore/Colonial.cs:65 | `const int quinineG = 78` | キニーネgood-id（=World.InvQuinineId=78・別所で同値が命名済み） | Catalog.QuinineId |
| Successor.SimCore/Colonial.cs:70 | `const int ship = 85, rail = 56` | 汽船good-id(=World.InvSteamId=85)と線路good-id(=56) | Catalog.SteamshipId(85) / Catalog.RailroadId(56) |
| Successor.SimCore/EconomyStep.cs:622 | `gi == 6 \|\| gi == 74` | 独占レント対象財＝塩(6・王室専売gabelle)と茶(74・東インド会社独占)のgood-idリテラル | Catalog.SaltId / Catalog.TeaId（新設） |
| Successor.SimCore/EconomyStep.cs:814 | `gi == 74` | 独占レントの帰属先分岐＝茶(74)は資本家(東インド会社)へ・その他(塩)は国庫へ。茶のgood-idリテラル（3度目の散在） | Catalog.TeaId |
| Successor.SimCore/EconomyStep.cs:1265 | `gi2 == 6 \|\| gi2 == 74` | 法的独占財（塩6/茶74）のEffFirms=1判定＝622行と同一の裸good-id（散在＝重複） | Catalog.SaltId / Catalog.TeaId（622行と共通化） |
| Successor.SimCore/Fiscal.cs:79 | `8` | 官僚(bureaucrat)のClassId。汚職leakの受け手判定 | Catalog.BureaucratClassId（const int）。Fiscal内で79/225/243/428・Credit他にも散在＝4箇所以上の重複 |
| Successor.SimCore/Fiscal.cs:79 | `8` | 官僚(bureaucrat)の ClassId。行政力/俸給/汚職分配の受け手判定に直書き | ClassId enum もしくは Catalog.BureaucratClassId 定数。同じ 8 が Fiscal.cs:225/243/428 にも散在（重複） |
| Successor.SimCore/Fiscal.cs:99 | `6` | 聖職者(clergy)のClassId。十分の一税の受け手判定 | Catalog.ClergyClassId（const int）。Simulation.cs のpop生成でもClassId=6を使用＝重複 |
| Successor.SimCore/Fiscal.cs:99 | `6` | 聖職者(clergy)の ClassId。教会からの徴収対象の抽出に直書き | ClassId enum / Catalog.ClergyClassId 定数 |
| Successor.SimCore/Fiscal.cs:225 | `8` | 官僚のClassId（維持費俸給の抑圧装置＝兵士＋官僚） | Catalog.BureaucratClassId |
| Successor.SimCore/Fiscal.cs:243 | `8` | 官僚のClassId（汚職根絶費＝視学官への俸給分配） | Catalog.BureaucratClassId |
| Successor.SimCore/Fiscal.cs:428 | `8` | 官僚のClassId（弾圧の受け手＝官僚＋兵士） | Catalog.BureaucratClassId |
| Successor.SimCore/Meritocracy.cs:181 | `p.ClassId == 1` | ClassId 1＝農民（訓練所で労働者へ転換する母体）。裸のClassIdリテラルで階級を判定 | Catalog.PeasantClassId 定数 or ClassId enum（Fiscal.cs の ClassId==8/6, World.cs の ClassId==0 と同じ問題群） |
| Successor.SimCore/Meritocracy.cs:181 | `1` | 農民(peasant)の ClassId。登用元プールの抽出に直書き | ClassId enum / Catalog.PeasantClassId（Military では c.SoldierRecruitClassA として既に外部化済み＝同じ概念が二系統に） |
| Successor.SimCore/MultiNationSim.cs:72 | `[25, 17, 20]` | 琉球の固有資源 good-id 配列（裸の財ID） | world.json 等のデータファイルへ抽象標的定義を外出し／good-idは Catalog 名前付き定数 |
| Successor.SimCore/MultiNationSim.cs:73 | `[20, 1, 11, 5]` | 蝦夷地の固有資源 good-id 配列（石炭=蝦夷等） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:75 | `[4, 15, 19, 11]` | アルタ=カリフォルニアの資源 good-id 配列（金=加州） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:77 | `[21, 10, 29, 11]` | ボルネオの資源 good-id 配列（石油=ボルネオ） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:79 | `[29, 21]` | マレーの資源 good-id 配列（胡椒/ゴム） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:84 | `[15, 16, 19]` | バルバリ(アルジェ)の資源 good-id 配列 | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:85 | `[4, 26, 23]` | アシャンティ(金海岸)の資源 good-id 配列（金/カカオ） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:86 | `[17, 19, 24]` | ソコト(サヘル)の資源 good-id 配列 | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:88 | `[21, 24, 2, 23]` | コンゴの資源 good-id 配列（ゴム） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:89 | `[4, 2, 19]` | 南アフリカの資源 good-id 配列（金/銅） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:91 | `[11]` | パナマの資源 good-id 配列（運河 host） | データファイル／Catalog 名前付き good-id |
| Successor.SimCore/MultiNationSim.cs:868 | `[1, 2]` | 越境移民する階級=農民(ClassId1)・労働者(ClassId2)のClassIdリテラル | Catalog.PeasantClassId / Catalog.WorkerClassId か ClassId enum |
| Successor.SimCore/Policy.cs:56 | `88` | 機関車(鉄道産業)の good-id | Catalog.LocomotiveId |
| Successor.SimCore/Policy.cs:57 | `0 or 1 or 2 or 3 or 4 or 5 or 7 or 8 or 9 or 120` | 鉱山部門の good-id 群（鉄鉱石/石炭/銅鉱/鉛鉱/金/硫黄/石灰石/珪砂/粘土/銀） | Catalog の鉱物 good-id 定数群（例 Catalog.MineGoodIds 配列） |
| Successor.SimCore/Policy.cs:58 | `81 or 82 or 83 or 86 or 122 or 123 or 131 or 133` | 軍事産業の good-id 群（火砲/小銃/弾薬/軍艦/戦車/弩級戦艦/戦闘機/化学兵器） | Catalog.MilitaryGoodIds 定数群 |
| Successor.SimCore/Policy.cs:59 | `134 or 135` | 通信・報道の good-id（新聞/ラジオ） | Catalog.NewspaperId / Catalog.RadioId |
| Successor.SimCore/Policy.cs:60 | `10` | 原油(石油部門)の good-id | Catalog.CrudeOilId |
| Successor.SimCore/Policy.cs:61 | `87` | 電力の good-id | Catalog.ElectricityId |
| Successor.SimCore/Simulation.cs:529 | `102` | 米の good-id を制御フロー（生産者除去＝米の気候ゲート）に直書き。799行にも同一 `pr.GoodId == 102` | Catalog.RiceId（新設・命名定数） |
| Successor.SimCore/Simulation.cs:529 | `102` | 米(rice)の good-id。RiceSuitable でない州から米生産者を除外する分岐で裸の good-id を直書き | Catalog.RiceId (const int = 102) を新設。Catalog には既に CoalId/GoldOreId 等の named good-id 定数がある |
| Successor.SimCore/Simulation.cs:651 | `[57, 59, 70, 71]` | 職人工房の対象 good-id（衣料/家具/革製品/陶磁器）を配列リテラルで直書き。57は727/762行にも | Catalog の命名定数群 or Catalog.ArtisanGoods 配列 |
| Successor.SimCore/Simulation.cs:695 | `55, 10, 87` | 石油火力レシピの投入財 good-id（55=発電機・10=原油）と産物 good-id 87（電力）を直書き。87は372/373/694/721/722行にも散在 | Catalog.ElectricityId/GeneratorId/CrudeOilId（命名定数）＋レシピをgoods.json/Catalogへ外部化 |
| Successor.SimCore/Simulation.cs:714 | `49, 16` | 甜菜糖レシピ：産物 good-id 49（砂糖）・投入16（果実）を直書き。72/109/17/57/103/110/45/50/126/68 など全代替レシピが同型の裸ID | Catalog 命名定数＋AddAdvancedProducers のレシピ表をデータファイルへ |
| Successor.SimCore/Simulation.cs:722 | `37` | 化学薬品の good-id を多数のレシピ投入に直書き（714/717/721/725/741/760/771/780行にも散在＝高頻度の重複ID） | Catalog.ChemicalsId（命名定数） |
| Successor.SimCore/Simulation.cs:750 | `0, 11, 7` | 木炭製鉄レシピの投入 good-id（0=木炭?/11=木/7=?）を直書き。木炭製銅/鉛（752/754行）でも2,3,11を直書き | Catalog 命名定数（CharcoalId/WoodId 等）＋レシピのデータ外部化 |
| Successor.SimCore/Simulation.cs:799 | `102` | 米(rice)の good-id。529 と同一の裸リテラルが別関数にも散在（重複） | Catalog.RiceId（529 と共有） |
| Successor.SimCore/World.cs:420 | `0` | 貴族(noble)の ClassId。近世国家の行政の担い手（貴族行政力）の集計に直書き | ClassId enum / Catalog.NobleClassId 定数 |
| Successor.SimCore/World.cs:692 | `84, 85, 78, 65` | 侵略に要る財の good-id を直書き（帆船=84・蒸気船=85・キニーネ=78・缶詰=65）。市場在庫参照(InvShipStock/InvQuinineStock/InvCannedStock)で使う | Catalog.SailId / Catalog.SteamShipId / Catalog.QuinineId / Catalog.CannedFoodId（既存 CoalId 等と同じ named good-id 定数へ） |

#### ハードコード名（国名/州名/財名/文明などの文字列比較）（11件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:1100 | `"資本家" / "官僚" or "職人" or "商人"` | POP階級を上流/中流/下流の3層へ分類するClassName文字列比較。同一式が1100/1115/1129/2659-2660の4箇所に散在＝ロジックが訳語文字列に依存 | Pop.SocialLayer プロパティ or 共通ヘルパ LayerOf(Pop) へ集約（ClassId/enum基準） |
| Successor.App/ViewModels/NationSelectViewModel.cs:15 | `{ "フランス", "日本", "イギリス", "アメリカ", "ロシア", "プロイセン", "オスマン" }` | 国選択画面の国名リスト。indexがplayerIndex＝MultiNationSimのconfig順に暗黙依存。WorldData.Nations()先頭7の複製で二重管理＝並び不一致で別国を選ぶ危険 | WorldData.Nations() から先頭N件を導出（単一の真実源） |
| Successor.SimCore/Diplomacy.cs:1339 | `"スエズ運河" / "パナマ運河"` | チョークポイント名の文字列直書き。ChokeOpenFor(name)へ生の運河名を渡してスエズ/パナマ通行可否を判定（講和の植民地奪取距離・投射で参照） | Catalog定数 SuezCanalName / PanamaCanalName（または Chokepoint.ServesBasins 経由に一本化・行1352,1594にも重複） |
| Successor.SimCore/InternationalMarket.cs:109 | `"ジブラルタル海峡" / "ボスポラス海峡" / "スエズ運河" / "パナマ運河" / "マラッカ海峡"` | チョークポイントを名前で引くロジック依存の文字列リテラル。Diplomacy等6ファイルに同一文字列が散在 | Chokepoint名の const/enum（例 ChokepointNames.Gibraltar）を一元定義 |
| Successor.SimCore/InternationalMarket.cs:109 | `"ジブラルタル海峡","ボスポラス海峡","スエズ運河","パナマ運河","マラッカ海峡"` | チョークポイントを名前文字列で同定。輸送距離ロジックが州/運河の表示名に依存 | Chokepoint に安定 id/enum（ChokepointId.Suez 等）を持たせ名前照合を廃止。Diplomacy.Chokepoints の定義側に定数化 |
| Successor.SimCore/MultiNationSim.cs:485 | `cp.Name.Contains("スエズ") / cp.Name.Contains("パナマ")` | 運河の種別を名前文字列一致で判定（スエズ/パナマの個別分岐）。トール経路判定の中核ロジックが日本語名に依存 | Chokepoint に enum CanalKind か bool IsSuez/IsPanama 相当の識別子を持たせる（名前依存を排除） |
| Successor.SimCore/MultiNationSim.cs:485 | `"スエズ","パナマ"` | 通航税ルートの運河を部分一致文字列で同定（Contains）。名前依存のロジック | ChokepointId enum。InternationalMarket.cs:109 と同じチョークポイントを別表記(部分一致)で参照＝二重の名前結合 |
| Successor.SimCore/MultiNationSim.cs:501 | `cp.Name.Contains("スエズ"), cp.Name.Contains("パナマ")` | トール課金の海路短縮計算で運河種別を再び名前一致で判定（485行と重複） | Chokepoint.CanalKind enum（485行と同一の識別子を共有） |
| Successor.SimCore/MultiNationSim.cs:501 | `"スエズ","パナマ"` | 運河別の海路短絡判定を部分一致文字列で同定（485 と重複） | ChokepointId enum（485 と共有） |
| Successor.SimCore/SelfPlay.cs:387 | `"軍需"` | 工場カテゴリ名の文字列比較（軍需補助金の対象判定がカテゴリ名文字列に依存） | Catalog のカテゴリ定数 or enum（FactoryCategory.Military 等） |
| Successor.SimCore/Simulation.cs:505 | `["マルセイユ","ボルドー","江戸","大阪","ロンドン",...]` | 港湾都市を都市名の文字列集合でハードコード（IsPort 判定の backstop）。ロジックが都市名に依存 | world.json のプロビンス属性 Coastal（データ側へ・p.Coastal に一本化） |

#### 固定インデックス（Nations[2]=英 等の魔法索引）（2件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Formations.cs:438 | `Ideologies.National(w)[4]` | イデオロギー配列のナショナリズム支持率への固定索引（4番目） | Ideologies.NationalismIndex 定数 or enum |
| Successor.SimCore/SelfPlay.cs:407 | `16` | 階級（ClassId）の最大数＝stackalloc長・境界判定に散在（408,411,426,434行にも重複） | Catalog.ClassCount 定数（or Coefficients.MaxClasses） |

#### 重複リテラル（同値が複数箇所に散在＝一箇所に集約すべき）（7件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:1835 | `1783 / 360 / 30` | EventDate：1723と同一の暦定数の重複（事件ログの日付） | 共通 DateAt/Calendar ヘルパへ一本化 |
| Successor.App/ViewModels/GameViewModel.cs:2659 | `"資本家" / "官僚" or "職人" or "商人"` | POPタブの上流/中流振り分け。1100/1115/1129と同一の階級分類ロジックの4個目の重複 | 上記 LayerOf ヘルパへ統合 |
| Successor.SimCore/Diplomacy.cs:1218 | `1.5 / -1.5` | 戦勝点の上限＝厭戦崩壊の上書き値（決定的1.0を超える崩壊域）。行1227(±1.5上書き)・1396(Min(absScore,1.5))・1534(Min(...,1.5))に散在する同一意味の値 | Coefficients.WarCollapseScore（または const CollapseScore=1.5） |
| Successor.SimCore/FactionSnapshot.cs:51 | `p.Closed ? 0 : p.IsModernElite ? 1 : 2` | ブロック符号化（0/1/2）。Factions.cs:101 と重複した束ね方の定義サイト | Bloc enum ＋共有ヘルパーに一本化（上記と同じ移設先） |
| Successor.SimCore/Factions.cs:101 | `p.Closed ? 0 : p.IsModernElite ? 1 : 2` | ブロック符号化（0=旧支配層 1=近代エリート 2=大衆）。5ファイルに散在する同一表現（FactionSnapshot.cs:51, PolicyPreferences.cs:49, Politics.cs:791/829） | Bloc enum（OldRuling/ModernElite/Mass）＋共有ヘルパー Bloc.Of(Pop) に一本化 |
| Successor.SimCore/MultiNationSim.cs:878 | `new[] { 1, 2 }` | 国際移民の着地階級=農民(1)・労働者(2)。868行 MigrantClasses と同一の意味を持つ重複リテラル | MigrantClasses を再利用（またはCatalogの名前付きClassId） |
| Successor.SimCore/Simulation.cs:347 | `0.06 / 0.34` | 生産者の限界費用（工場/自家農家）。415行・629行にも同一 `isFactory ? 0.06 : 0.34` が重複 | Coefficients.FactoryMarginalCost / RgoMarginalCost |

#### 暦定数（3=月/四半期・4・90・360・年号）（13件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:617 | `1783 ... / 360 >= 1935` | 終局判定の起点年1783・年360日・終局翌年1935（表示暦1934年12月終了）。1700にも同式 | const EndYear/EndSentinelYear=1935（+ StartDisplayYear/DaysPerYear） |
| Successor.App/ViewModels/GameViewModel.cs:1723 | `1783 / 360 / 30` | 表示暦の起点年(1783)・年360日・月30日。UpdateDateの日付整形。EventDate(1835)/DateAt(1836)/ShowEnding(617,1700)に同じ式が散在 | const StartDisplayYear=1783 / DaysPerYear=360 / DaysPerMonth=30（Calendar静的 or Coefficients） |
| Successor.SimCore/Ideology.cs:22 | `1830 / 40.0` | 社会主義イデオロギーが立ち上がる暦年オンセット(1830)と開放スパン(40年) | Coefficients.SocialismOnsetYear / IdeologyEraSpanYears（40.0は23-24行で3重出現） |
| Successor.SimCore/Ideology.cs:23 | `1848` | 共産主義イデオロギーの暦年オンセット（共産党宣言） | Coefficients.CommunismOnsetYear |
| Successor.SimCore/Ideology.cs:24 | `1860` | 帝国主義イデオロギーの暦年オンセット | Coefficients.ImperialismOnsetYear |
| Successor.SimCore/Ideology.cs:25 | `1919 / 15.0` | ファシズムの暦年オンセット(1919・両大戦間)と結晶スパン(15年) | Coefficients.FascismOnsetYear / FascismEraSpanYears |
| Successor.SimCore/NationConfig.cs:59 | `1783` | シナリオ開始年をコード直書き（world.json 経由でなく ToScenario がハードコード） | const int CampaignStartYear = 1783（暦定数・散在する年号 1783/1914 等を集約） |
| Successor.SimCore/Simulation.cs:865 | `90` | 四半期の日数（StepQuarter の日次ループ回数）。894行 `% 90`・863行コメントとも重複 | const int DaysPerQuarter（またはCoefficients/Calendar定数） |
| Successor.SimCore/Simulation.cs:890 | `30` | 1か月の日数（月次決算の発火境界）。1230行 `World.Tick / 30`（外交の月換算）とも重複 | const int DaysPerMonth |
| Successor.SimCore/Simulation.cs:892 | `89` | 四半期最終日の索引（quarterDone 判定）。976行 `_dayInQuarter == 89`（IsQuarterEndMonth）とも重複 | const int LastDayOfQuarter = DaysPerQuarter-1 |
| Successor.SimCore/Simulation.cs:966 | `4` | 1年の四半期数（年送り判定） | const int QuartersPerYear |
| Successor.SimCore/Simulation.cs:978 | `360` | 1年の日数（Tick→年の換算・豊凶ロールの年境界） | const int DaysPerYear (= DaysPerQuarter*QuartersPerYear) |
| Successor.SimCore/Simulation.cs:1423 | `12.0` | 年→月の換算（コホート加齢の月次化）。1465行 WorkingYears*12.0・1396/1437行 CrudeBirthRate/12.0 とも重複 | const int MonthsPerYear |

#### 調整係数（Coefficients へ移すべき裸の率/閾値/倍率/重み/上限）（7件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Diplomacy.cs:227 | `12` | 国家ランク算出で小さな世界（比率式）と大きな世界（定員式）を分ける実国数の閾値『実国12以下』 | Coefficients.SmallWorldNationThreshold |
| Successor.SimCore/Diplomacy.cs:253 | `200` | AddTension の緊張上限クランプ（0..200）。行930の緊張ランプ適用でも同じ0..200。SeedTension(行189)は0..100と別上限＝二種の上限が裸で散在 | Coefficients.TensionMaxAccum（行930と共有） |
| Successor.SimCore/Diplomacy.cs:424 | `0.45 / 0.35 / 0.20` | 脅威超過スコアの重み（GDP=0.45・植民地=0.35・軍事=0.20、和1.0）。覇権判定と ThreatTension の中核係数。行830のコメントは古い『GDP×0.6＋植民地×0.4』で実コードと不一致 | Coefficients.ThreatGdpWeight / ThreatColonyWeight / ThreatMilitaryWeight |
| Successor.SimCore/Diplomacy.cs:1607 | `100` | 緊張の自然冷却の天井＝100未満のみ薄れる／100以上は戦争まで下がらない。行1611でも同値。開戦閾値(c.WarDeclareTension)とは別概念だが裸のまま散在＝両者ズレのリスク | Coefficients.TensionCoolCeiling（行1611と共有） |
| Successor.SimCore/Fiscal.cs:301 | `0.7` | 金本位からの強制離脱(兌換停止)の財政funding閾値。この値未満で強制停止 | Coefficients.GoldStandardSuspendFunding。加入側は c.GoldStandardMinFunding（line313）を使うのに離脱側だけ裸の0.7＝非対称/未外部化 |
| Successor.SimCore/InternationalMarket.cs:324 | `0.1 / 10.0` | 世界価格の床/天井＝RefPrice×0.1〜×10.0。MarketFlowの国内価格は coef.PriceFloorFactor/PriceCeilFactor で外部化済みなのに世界価格側だけ裸 | Coefficients.WorldPriceFloorFactor / WorldPriceCeilFactor |
| Successor.SimCore/TechEducation.cs:127 | `0.0001 / 0.004 / 0.005 / 0.007` | 教育法（None/Religious/Public/Compulsory）ごとの月次識字成長率。教育の中核律速値がswitch直書き | Coefficients.LiteracyRateNone/Religious/Public/Compulsory（または法→率の配列/データファイル） |

### 6.2 優先度 MEDIUM（Coefficients へ移すべき単発の調整値） ― 204件

#### ハードコードID（財 good-id・ClassId の裸リテラル）（1件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Catalog.cs:262 | `[109, 127], [84], [110], [52, 38, 128]` | RGO効率化材の good-id（肥料・馬／帆船／飼料／工具・火薬・ダイナマイト）と対象RGOの id リストを直書き。ブースター物品idが裸リテラル | Catalog.FertilizerId/HorseId/SailingShipId/FeedId/ToolId/GunpowderId/DynamiteId 定数 |

#### ハードコード名（国名/州名/財名/文明などの文字列比較）（11件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Climate.cs:31 | `"パリ" => (48.85, 2.35) … "ラクナウ" => (26.85, 80.95)` | 代表都市名→座標の巨大ルックアップ表（約160件）。コメントに「世界地図(App/MapData)と同源」とあり座標が App 側と二重管理＝名前依存＋重複データ | world.json/データファイル（州エンティティに座標を持たせ App/MapData と単一ソース化） |
| Successor.SimCore/Diplomacy.cs:508 | `"ロシア"/"ドイツ"/"漢"… → "スラヴ"等` | 文化ラベル→民族グループの分類テーブル（汎ナショナリズムの親近/反目）。約40の国名/民族名がロジック内に直書き | データファイル（culture→group マップ・world.json 併設） |
| Successor.SimCore/Diplomacy.cs:523 | `"カトリック"/"イスラム"… → "西方キリスト"等` | 宗教ラベル→ファミリーの分類テーブル。宗教名がロジック内に直書き | データファイル（religion→family マップ） |
| Successor.SimCore/Formations.cs:28 | `Rules = [ new FormationRule("ローマ", ... "清" ...) ]（国名/州名/文化/宗教の巨大文字列表＋Tier整数）` | 変名/formable規則の全データ＝国名・都市名・文化・宗教・段位が全てソース直書き（world.jsonのcityと一字一致を手管理） | データファイル化（formations.json 等）＝州名の照合を world.json とデータ側で一元管理 |
| Successor.SimCore/Formations.cs:422 | `["北ドイツ連邦", "ドイツ", "イタリア"]` | 統一戦争v1対象の変体ホワイトリスト（国名文字列でロジックが分岐） | FormationRule にフラグ列を追加 or データファイル化 |
| Successor.SimCore/Geo.cs:56 | `"サン=ドマング" => (18.6, -72.3) … "マラッカ" => (1.4, 103.8)` | 植民地/侵略標的名→座標のルックアップ表（約90件）。Colony 名文字列に依存＋座標がハードコード | データファイル（Colony 定義に座標フィールドを持たせる） |
| Successor.SimCore/MultiNationSim.cs:99 | `"ジブラルタル" / "ボスポラス" / "マレー" / "カイロ" / "パナマ"` | チョークポイントの Host 名（扼者解決が名前一致で動く＝ResolveOwner/StepCanalsが依存） | チョークポイント定義をデータファイルへ（host 参照は州IDで） |
| Successor.SimCore/MultiNationSim.cs:120 | `50 / 40 / 5 / 15 / 60 / 45 / 45 / 30 / 55 / 50` | 初期史実ライバルの緊張値（仏英50・英米40…）と国名。全ロジックが国名文字列＋裸の初期緊張値に依存 | scenario/world データファイルへ初期ライバル表を外出し（国名IDと初期緊張の表） |
| Successor.SimCore/MultiNationSim.cs:138 | `"シャム"/"カンボジア"/"ラオス"/"マラータ同盟"/"デリー"` | 初期従属関係を国名文字列で設定（宗主-属国の初期条件が名前依存） | scenario データファイルへ初期従属表を外出し |
| Successor.SimCore/SelfPlay.cs:387 | `"軍需"` | 工場カテゴリを文字列で同定。軍需工場のみ集計する分岐が FactorySpec のカテゴリ表示名に依存 | FactoryCategory enum（Catalog.FactorySpec の Category を string→enum 化）。Catalog.cs:414 の "軍需" 定義と対応 |
| Successor.SimCore/Simulation.cs:842 | `"フランス","イギリス","プロイセン","オーストリア","ロシア"` | SetupDiplomacy の列強名を文字列直書き（軍事力・GDP・国庫・貿易比重も同行にハードコード） | world.json のシナリオ列強テーブル（データファイル） |

#### 固定インデックス（Nations[2]=英 等の魔法索引）（18件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.Production.cs:371 | `Regions[0]` | 首都＝州0の前提（世界価格差の基準州）。434の cap0.Industrialization も同索引。首都参照が索引直書き | World.CapitalRegion プロパティ |
| Successor.App/ViewModels/GameViewModel.cs:527 | `[0] / [1] / [2]` | 議席配列のインデックス→政党(0=保守/1=自由/2=社会)の暗黙対応。PartyName(530)・POP党派(2607)にも同じ0/1/2⇔党名の対応が散在 | enum Party { Conservative, Liberal, Social } で索引を命名 |
| Successor.App/ViewModels/GameViewModel.cs:2681 | `{ "君主制", "議会制", "強権", "急進" } / 4` | 政体別実績ストックの系統名とインデックス上限4。RegimeTrackRecordの各indexの意味が名前配列に暗黙依存 | enum RegimeTrack + 命名済みラベル表（SimCore側と共有） |
| Successor.SimCore/CrownParliament.cs:149 | `3` | 徴兵供給源＝貴族士官団を表す索引値 | ConscriptionSource enum（均等/大衆/志願/貴族士官団 の名前付き列挙） |
| Successor.SimCore/CrownParliament.cs:327 | `s[0] + s[1] + s[4] + s[8] + s[9]（対 s[2]+s[3]+s[5]+s[6]+s[7]）` | 王権/権威寄りイデオロギー索引群 対 反権威索引群（Ideology enum 序数への直接添字） | Ideology enum ＋名前付きグルーピング定数（CrownAlignedIdeologies / AntiAuthoritarianIdeologies）。Parliament.ComputeVotes と重複 |
| Successor.SimCore/FactionSnapshot.cs:123 | `bloc == 0 ? trad : bloc == 1 ? modern : mass` | ブロック索引リテラル 0/1/2 でStance成分を選択。ブロック符号化への依存 | Bloc enum 化に伴い (Bloc)bloc switch へ |
| Successor.SimCore/FactionSnapshot.cs:148 | `/ 3.0, / 2.0（enum序数の正規化分母）` | 各法enumの最大序数でHeadroomを[0,1]正規化する分母（EducationLaw/Suffrage=3, Censorship/LaborUnion/CornLaws/LandReform/SerfEmancipation=2, WorkHours=3）。enum定義に暗黙依存 | 各enumの (MemberCount-1) を返すヘルパー or 名前付き const（EnumMax.EducationLaw 等） |
| Successor.SimCore/Government.cs:45 | `4` | 社会保障法の最上段序数（5段→最大index4）で福祉意図0..1を段階へ写像 | Enum.GetValues<SocialSecurityLaw>().Length-1 か const SocialSecurityMaxLevel（line47 の /4.0 と重複） |
| Successor.SimCore/MultiNationSim.cs:155 | `reg.Id == 0` | 首都州のId=0規約（首都不変条件の判定）。179行 reg.Id = 0 と対で散在 | const int CapitalRegionId = 0（179行と共有） |
| Successor.SimCore/Parliament.cs:100 | `s[0]+s[1]+s[4]+s[8]+s[9] / s[2]+s[3] / s[5]+s[6]+s[7]` | 党族（保守/自由/社会）へのイデオロギー索引グルーピング（Ideology enum 序数直書き） | Ideology→Family の単一写像表（FamilyOf と統合）。line135・FamilyOf(201) と三重重複 |
| Successor.SimCore/Parliament.cs:189 | `f.Bloc == 0 / == 1` | 利益ブロック索引（0=旧支配層/1=近代エリート/2=大衆） | Bloc enum（InterestGroups.Blocs の3区分を名前付き列挙に） |
| Successor.SimCore/Parliament.cs:201 | `2 or 3 → 1 / 5 or 6 or 7 → 2 / else 0` | イデオロギー序数→党族の分類（保守0/自由1/社会2） | Ideology enum ＋ Ideologies.FamilyOf 単一定義（ComputeVotes の同一分類と共有） |
| Successor.SimCore/Parliament.cs:222 | `3 / 3 / 2 / 2 / 3 / 2 / 2 / 2` | 各法enumの最大序数（StepToのクランプ上限＝enum長-1の直書き） | Enum.GetValues<T>().Length-1 で各enum境界を導出（マジックな上限3/2を排除） |
| Successor.SimCore/Politics.cs:457 | `2 => ..., 1 => ...` | GoverningParty の党コード(1=自由→ブルジョア独裁/2=社会→プロ独裁)＝マジック党索引 | enum Party { Conservative, Liberal, Social }（PartySeats[0/1/2]も同スキーム・308/453/158行等に散在） |
| Successor.SimCore/Politics.cs:966 | `Regions[0] / r.Id == 0 / Region.Id != 0` | 首都(中核)州を索引0で識別＝固定レイアウト前提。744/966/970行に散在 | World.CapitalRegion プロパティ or const CapitalRegionId |
| Successor.SimCore/SelfPlay.cs:422 | `1 / 2 / 0` | ClassStance の状態コード（1=優遇・2=弾圧・0=なし）を裸整数で扱う（418,435,436行にも散在） | enum ClassStanceKind { None=0, Favor=1, Suppress=2 } |
| Successor.SimCore/SelfPlay.cs:444 | `3 / 0` | 徴兵源コード（3=貴族士官団・0=均等）を裸整数で設定 | enum ConscriptionSource { Equal=0, NobleOfficerCorps=3 } |
| Successor.SimCore/World.cs:232 | `16` | 階級数（ClassId で引く固定次元）の直書き。ClassStance/ClassStancePaid/ClassGrievance の3配列に散在 | Catalog.MaxClasses（または ClassCount 定数）を3箇所で共有 |

#### 重複リテラル（同値が複数箇所に散在＝一箇所に集約すべき）（36件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.Production.cs:231 | `0.95` | 原料充足の律速判定閾値（ボトルネック赤字化）。231/271/469/477＋地図の過密/主食/住居(2120/2123/2126/2127)に同じ0.95が散在 | Coefficients.ShortageDisplayThreshold（充足<閾値で不足表示） |
| Successor.App/ViewModels/GameViewModel.cs:1103 | `{ "上流", "中流", "下流" } / new double[3]` | 3層ラベル配列と層数3。LiteracyClassTerms/MilitancyClassTerms/SatisfactionClassTerms(1103/1118/1132)で同一配列が3回重複 | static readonly LayerNames 定数 + const LayerCount=3（分類ヘルパと同居） |
| Successor.App/ViewModels/GameViewModel.cs:1700 | `1783 ... / 360 >= 1935` | OnTick内の終局判定。617と同一の暦定数の重複 | 上記 EndYear 定数と Calendar ヘルパ |
| Successor.App/ViewModels/GameViewModel.cs:1836 | `1783 / 360 / 30` | DateAt：暦定数の3個目の重複 | Calendar 静的クラスへ集約 |
| Successor.App/ViewModels/GameViewModel.cs:2457 | `30.0` | 戦争経過月→日数換算(月=30日)。開戦日算出。monthStamp(2431)・終戦(2485,2491,2500)・遅延月(1879)にも散在 | const DaysPerMonth=30 |
| Successor.SimCore/Colonial.cs:33 | `/ 10_000.0` | 海路距離kmを正規化する距離スケール（distF＝km/1万）。line197でも同値を反復 | Coefficients.ColonyDistanceScaleKm（line33/197で共有） |
| Successor.SimCore/Colonial.cs:197 | `km / 10_000.0` | 独立判定の距離減衰でも同じkm正規化スケール（line33と重複） | Coefficients.ColonyDistanceScaleKm |
| Successor.SimCore/Diplomacy.cs:860 | `-5` | 不可侵協定締結による緊張減。行1023(ProposeNonAggression)でも同じ -5 が重複 | Coefficients.NonAggressionTensionRelief（行1023と共有） |
| Successor.SimCore/Diplomacy.cs:1236 | `256` | EndedWars 終戦記録リングバッファの上限件数。行116・行1234コメントにも同値が散在 | const EndedWarRingSize=256 |
| Successor.SimCore/Diplomacy.cs:1595 | `10_000.0` | 距離減衰の正規化スケール（万km基準）。行1353の植民地奪取距離コストでも同じ /10_000.0 で重複 | const DistanceNormKm=10000（行1353と共有） |
| Successor.SimCore/Ideology.cs:69 | `(wealthRel - 1) / 3.0) * 3.0` | 平均超の富(0..3＝失う物)への正規化スケール。98行のBaseWeightsと同一式が重複 | Coefficients.ExcessWealthScale（69行と98行で同一の裸の3.0） |
| Successor.SimCore/InternationalMarket.cs:146 | `10_000.0` | 距離マージンの正規化基準距離（10000km）。185行でも同値で再登場＝重複 | Coefficients.DistanceRefKm（146/185行で共有） |
| Successor.SimCore/InternationalMarket.cs:185 | `10_000.0` | 内陸鉄道の港距離減衰の正規化基準距離（10000km）＝146行と同一意味の散在 | Coefficients.DistanceRefKm（146行と共通化） |
| Successor.SimCore/InternationalMarket.cs:209 | `Math.Min(0.9, gMargin)` | throttle事前パスの輸出マージン上限0.9＝253行の本パスと同一の裸上限 | Coefficients.MaxExportMargin（253行と共通化） |
| Successor.SimCore/MultiNationSim.cs:246 | `4.0` | 社会保障法の段数（なし/救貧/労災/失業/年金＝5段の分母4）で法段を0..1の目標へ正規化 | const int SocialSecurityMaxTier（enum段数から導出） |
| Successor.SimCore/MultiNationSim.cs:255 | `0.15` | 分離独立で生まれる新国家の初期 TradeWeight 固定値（50行の基準0.15と重複概念） | Coefficients.TradeWeightInitBase（50行と共有） |
| Successor.SimCore/Parliament.cs:331 | `(0.3,0.1,0.0) / (0.1,0.15,0.0) / (_,0.5,0.5)` | 連立相手の綱領（関税/福祉/累進）＝ApplyPlatform の各党定数の重複 | 上記の党綱領テーブルを単一ソース化し line279-313 と共有 |
| Successor.SimCore/PolicyPreferences.cs:102 | `1.5` | 関税/穀物法で保護される生産者(地代)の賛成強度。104行のCornLawsでも1.5が重複 | Coefficients.TariffRentBenefit（102行と104行の裸の1.5） |
| Successor.SimCore/PolicyPreferences.cs:121 | `0.5 + e.Wealth[0]` | 地位/権力軸の自由化に対する旧支配層の抵抗の基底(0.5)。121/125/127行に重複 | Coefficients.StatusResistanceBase（EducationLaw/WomenSuffrage/LandReform で同一の裸0.5） |
| Successor.SimCore/Politics.cs:169 | `== 1 / != 2 / == 2` | ClassStance の状態コード(0中立/1優遇/2弾圧)＝マジック状態値。169/217/242/549行に散在 | enum ClassStanceKind { Neutral, Favor, Repress } |
| Successor.SimCore/SelfPlay.cs:77 | `0.5` | 近代化法ゲートの大衆闘争閾値（コメントで SuffrageReformThreshold と同水準と明記＝重複値） | Coefficients.SuffrageReformThreshold を参照（重複の一本化） |
| Successor.SimCore/SelfPlay.cs:170 | `1.0 ... 3.0` | 義務教育への移行条件：TechLevel>1.0 かつ Treasury>財政バッファ×3.0（248行にも同一・重複） | Coefficients.CompulsoryEduTechThreshold / CompulsoryEduBufferMult |
| Successor.SimCore/SelfPlay.cs:178 | `1.2 / 1.5 / 0.25` | 法解禁の技術閾値：女性財産権(1.2)・女性参政権(1.5)・制限選挙(0.25)。Apply側(259-261)にも同値が重複 | Coefficients.WomenPropertyTech / WomenSuffrageTech / CensusSuffrageTech |
| Successor.SimCore/SelfPlay.cs:366 | `2.5` | 国家主導工業化の設計包絡上限（375行の輸出攻勢ゲート<2.5にも重複＝後発判定の同一意味値） | Coefficients.StateLedIndustrializationCap |
| Successor.SimCore/SelfPlay.cs:407 | `16` | 階級スロット数（stackalloc double[16]/bool[16]×4）。World.cs の [16] と同じ意味の重複 | const int ClassCount = 16（World.cs と共有） |
| Successor.SimCore/SelfPlay.cs:606 | `10_000_000` | 損失financing過大の異常閾値（637行の集計判定にも同値が重複） | Coefficients.LossFinancedAnomalyThreshold |
| Successor.SimCore/Simulation.cs:372 | `0.5 (Scale=0.05, Productivity=1.4)` | BuildFullChain 内の石油火力/魚缶詰プロデューサ定義が AddAdvancedProducers(694/697行) とほぼ同一内容で重複（GoodId 87/65・投入・係数一式） | 共通レシピ生成ヘルパへ集約（AddAdvancedProducers と一本化） |
| Successor.SimCore/Simulation.cs:555 | `0.10,0.20,0.02,0.25,0.03,0.05,0.25,0.30` | 職業POPの成人女性比を直書き。814-816行（BuildFrontier）に同一値が重複。553/812行の使用人0.75も重複 | Coefficients/データの職業別 FemaleShare テーブル（1箇所で共有） |
| Successor.SimCore/Simulation.cs:627 | `0.5 / 50 / 0.05` | 工場/RGOの初期Output（0.5対50）と初期Scale（0.05）。BuildFullChain(346-347)・BuildFrance・各代替レシピにも Output=0.5/Scale=0.05 が多数重複 | Coefficients.FactoryInitialOutput / RgoInitialOutput / FactoryInitialScale |
| Successor.SimCore/Simulation.cs:701 | `0.2` | 下位/上位インフラ各レシピの高度労働力投入量 AdvLaborPerOutput を直書き（705/708行にも0.2）。他レシピは Catalog.AdvLaborPerOutput(id) を使用するのに不整合 | Catalog.AdvLaborPerOutput(id) の使用に統一 or Coefficients.InfraAdvLaborPerOutput |
| Successor.SimCore/Simulation.cs:1366 | `0.3` | 扶養力の崩壊防止下限（baseline×0.3）。1345行 foodRatio下限0.3・1509系の飢饉係数と概念重複する「0.3」 | Coefficients.CapacityCollapseFloor |
| Successor.SimCore/TechEducation.cs:92 | `1.0 / 0.75 / 0.5` | 検閲法（FreePress/Censorship/StateControl）ごとの報道自由による技術吸収係数 pressAbs | Coefficients.PressAbsorptionCensorship(0.75)/PressAbsorptionStateControl(0.5)。line117のpressと共有 |
| Successor.SimCore/TechEducation.cs:117 | `1.0 / 0.75 / 0.5` | 検閲法ごとの革新（フロンティア押上げ）速度乗数 press。line92のpressAbsと同一マッピングの重複 | line92と同じ名前付き定数へ集約（Coefficients.PressAbsorption*） |
| Successor.SimCore/Validation.cs:57 | `0.051 / 19.9` | 価格の床(≤0.051×参照価格)・天井(≥19.9×参照価格)の判定倍率（NationMetrics docコメント15-16行にも同値） | Coefficients.PriceFloorFrac / PriceCeilFrac |
| Successor.SimCore/World.cs:232 | `16` | 階級スロットの最大数（ClassStance/Paid/Grievance 配列の固定長）。クラス総数の魔法定数 | const int ClassCount = 16 を単一定義。World.cs:233,234 と SelfPlay.cs:407,408（stackalloc [16]×4）にも散在・Politics.cs:169 のコメントも「固定長16」に依存 |
| Successor.SimCore/World.cs:261 | `0.04` | 国債金利/平均クーポンの初期値（4%）。GovBondRate と GovAvgCoupon の両方に同値を直書き | Coefficients.InitialGovBondRate（両フィールド初期値を単一定数から） |

#### 暦定数（3=月/四半期・4・90・360・年号）（22件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:577 | `1789UL` | MultiNationSim のRNGシード兼開始暦年（表示暦1783とずれる＝助走で+1年先行の根拠年） | const SimStartYear=1789 / DefaultSeed（world.json or Coefficients） |
| Successor.App/ViewModels/GameViewModel.cs:1689 | `30 / 90` | 月次決算境界＝30日ごと・四半期＝90日。UI時計の月/四半期日数の直書き | const DaysPerMonth=30 / DaysPerQuarter=90 |
| Successor.App/ViewModels/GameViewModel.cs:2510 | `360.0` | 終わった戦争の残置期間判定＝年360日×PastWarKeepYears | const DaysPerYear=360 |
| Successor.SimCore/Diplomacy.cs:369 | `3` | 四半期＝3ヶ月。month % 3 == 0 の四半期判定が多数（変名・DiplomacyAI・統一戦争・貢納リセット・貢納支払）。行383では *3.0 で四半期→月換算 | const MonthsPerQuarter=3（行383,391,392,613,626と共有） |
| Successor.SimCore/Diplomacy.cs:369 | `3` | 1四半期＝3ヶ月（四半期タイミングの発火判定）。暦定数が同一ファイル内に反復 | const int MonthsPerQuarter = 3。Diplomacy.cs 内で5箇所反復（重複） |
| Successor.SimCore/EconomyStep.cs:363 | `3.0` | 四半期→月換算（1四半期＝3ヶ月）。暦定数 | const int MonthsPerQuarter = 3 |
| Successor.SimCore/Factions.cs:87 | `f.AbsentQuarters >= 8` | 除去に要する連続不在四半期数（8Q＝2年）。暦由来の反復閾値 | Coefficients.FactionForgetQuarters |
| Successor.SimCore/Ideology.cs:22 | `1830 / 40.0` | 社会主義イデオロギー創発の暦年オンセット(1830s〜)と立ち上がり幅40年。分岐の調整値 | Coefficients.SocialismOnsetYear / SocialismRampYears（load-bearing とコメントあり＝較正対象として明示化すべき） |
| Successor.SimCore/Ideology.cs:23 | `1848 / 40.0` | 共産主義創発の暦年オンセット(1848共産党宣言)と幅40年 | Coefficients.CommunismOnsetYear / RampYears |
| Successor.SimCore/Ideology.cs:24 | `1860 / 40.0` | 帝国主義創発の暦年オンセット(19c後半)と幅40年 | Coefficients.ImperialismOnsetYear / RampYears |
| Successor.SimCore/Ideology.cs:25 | `1919 / 15.0` | ファシズム創発の暦年オンセット(1919両大戦間)と幅15年。E7の時代ゲート | Coefficients.FascismOnsetYear / RampYears（FascismGate 係数は既にあるがオンセット年は裸） |
| Successor.SimCore/MultiNationSim.cs:304 | `90` | 四半期＝90日（日次刻みループ回数） | const int DaysPerQuarter（334/379行と共有） |
| Successor.SimCore/MultiNationSim.cs:334 | `89` | 四半期末判定（DaysPerQuarter-1） | const int DaysPerQuarter |
| Successor.SimCore/MultiNationSim.cs:340 | `3` | 外交を月次3回（四半期＝3ヶ月）回すループ回数＋月インデックス _quarter*3+m | const int MonthsPerQuarter |
| Successor.SimCore/MultiNationSim.cs:379 | `90` | 日カウンタの四半期モジュロ（DaysPerQuarter・304/334と重複） | const int DaysPerQuarter |
| Successor.SimCore/NationConfig.cs:59 | `1783` | ゲーム開始年。Scenario.StartYear の既定 1783 と同一値がここにも直書き（重複） | Scenario の既定(Scenario.cs:32=1783)を単一の真実源にして参照。Coefficients に StartYear 定数化も可 |
| Successor.SimCore/Simulation.cs:890 | `30` | 1月＝30日（月次決算の境界）。暦の魔法数 | const int DaysPerMonth = 30。MultiNationSim.cs でも月/四半期時計に散在 |
| Successor.SimCore/Simulation.cs:894 | `90 / 89` | 1四半期＝90日（89＝締め日）。三層時計の暦定数 | const int DaysPerQuarter = 90。MultiNationSim.cs:334(==89)/:379(%90) にも同値が散在（重複） |
| Successor.SimCore/Simulation.cs:966 | `4` | 1年＝4四半期（年繰り上げ境界）。暦定数 | const int QuartersPerYear = 4。World.cs:517 の *4.0(四半期→年率)・Government.cs:47 の /4.0 とも概念的に関連 |
| Successor.SimCore/World.cs:517 | `4.0` | 1年の四半期数（四半期物価変化→年率換算）。暦の反復定数 | const int QuartersPerYear = 4 |
| Successor.SimCore/World.cs:590 | `90.0` | 1四半期の日数（建設日数＝四半期数×90）。暦の反復定数 | const int DaysPerQuarter = 90（Coefficients か Catalog の暦定数） |
| Successor.SimCore/World.cs:882 | `12.0` | 1年の月数（買取価格＝月次利益×12×年数）。暦の反復定数 | const int MonthsPerYear = 12 |

#### 調整係数（Coefficients へ移すべき裸の率/閾値/倍率/重み/上限）（116件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:1753 | `0.85 / 0.80 / 0.20 / 0.25 / 0.01` | 自国経済イベントの発火閾値＝信用過熱>0.85・凶作<0.80・恐慌>0.20/深刻>0.25・緊縮>0.01。基準化(1750-1754)と検出(1783-1789)で二重に直書き | Coefficients.HeatAlertThreshold / FamineAlertThreshold / DepressionThreshold 等（検出と基準化で共有） |
| Successor.App/ViewModels/GameViewModel.cs:2286 | `0.8` | 緊張要因『国力への警戒』の発火閾値＝首位スコアの80%以上 | Coefficients.ThreatWatchScoreFrac |
| Successor.App/ViewModels/GameViewModel.cs:2287 | `0.10` | 緊張要因ホバーの『禁輸』月次寄与率。他要因はmw.Coef.*Rateを読むのにここだけ裸の率 | Coefficients.EmbargoTensionRate（SimCoreの実レートと同源に） |
| Successor.App/ViewModels/GameViewModel.cs:2589 | `0.5 / 0.30` | POP不満原因『重税』の判定＝人頭税を0.5加重した税負担が0.30超。生活水準ヒントの閾値 | Coefficients.PollTaxBurdenWeight / HeavyTaxThreshold |
| Successor.App/ViewModels/GameViewModel.cs:2627 | `12` | POP不満原因『長時間労働』の労働時間閾値（都市下層のみ） | Coefficients.LongWorkHoursThreshold |
| Successor.SimCore/Calibration.cs:23 | `[200, 600, 1500, 3000]` | 較正格子の圧政値スイープ | Coefficients/データファイルの較正グリッド定義 |
| Successor.SimCore/CapitalMarket.cs:44 | `9` | NominalGdp≈0時の債務/GDP比の代替センチネル（対外信用の distress 判定に流用） | Coefficients.DebtGdpDistressSentinel。Fiscal.cs:291 の 1e9 と同概念で値が食い違う |
| Successor.SimCore/CapitalMarket.cs:47 | `0.2` | 国債利回りスプレッドによる対外信用bondConfの下限（暴落しても0.2までしか落ちない床） | Coefficients.BondConfidenceFloor |
| Successor.SimCore/CapitalMarket.cs:50 | `0.3` | 為替減価による対外信用の下限クランプ（ExchangeRateの信用効果の床） | Coefficients.ExchangeRateCreditFloor |
| Successor.SimCore/Catalog.cs:275 | `1.0, 0.8, 0.6, 0.45, 0.35` | 各財の天候依存度（露地作物1.0/熱帯露地0.8/樹木プランテーション0.6/畜産0.45/漁獲・毛皮0.35）。不作の振れ幅を決める調整値 | Coefficients.WeatherExposure* もしくは goods.json の財プロパティ化 |
| Successor.SimCore/Catalog.cs:322 | `0.5, 0.8, 0.6` | 役務の参照価格アンカー（労働/兵力/農業労働=0.5、技師=0.8、その他役務=0.6）。役務popの収入水準を決める係数 | Coefficients.LaborRefPrice / AdvLaborRefPrice / ServiceRefPriceDefault |
| Successor.SimCore/Catalog.cs:333 | `0.15, 0.10, 0.05, 0.02` | 産出1単位あたりの高度労働力（技師）投入。発電0.15/機械類0.10/重工業0.05/軽工業0.02＝エネルギー集約度の勾配係数 | Coefficients.AdvLaborPerOutput*（4段の階梯値） |
| Successor.SimCore/Catalog.cs:408 | `320_000/28, 200_000/16, 240_000/22, 150_000/12, 180_000/20, 110_000/12, 90_000/8, 70_000/8, 30_000/4` | 工場カテゴリ別の建設費と建設四半期数。高度な工場ほど高額・長期を表す調整値（暦の四半期数含む） | Coefficients.FactoryCost*/FactoryQuarters* もしくはデータファイルのカテゴリ表 |
| Successor.SimCore/Catalog.cs:448 | `0.6, 0.5, 0.9, 0.5, 1.3, 0.8, 0.85, 0.5, 0.6, 0.5, 0.7, 0.8, 1.0, 1.0, 0.95, 1.0, 1.05` | デフォルトレシピの技術 onset（各財の史実発明年を内生tech水準へ写像）。原油0.6・紙0.5・ゴム0.9・発電機1.3・戦車0.8・内燃機関0.85 等の解禁閾値が good-id ごとに直書き | Coefficients.RecipeOnset*（財別 onset テーブル）／goods.json の財プロパティ TechOnset |
| Successor.SimCore/Catalog.cs:482 | `0.8, 1.0, 1.05` | 需要側の発明 onset（自転車0.8・自動車1.0・ラジオ1.05）＝発明前の幻需要を防ぐ閾値。good-id別に直書き | Coefficients.DemandOnset*（財別 onset）／goods.json 財プロパティ |
| Successor.SimCore/Climate.cs:12 | `32.0, 60.0, 41.0` | 米の適地判定の気候帯境界＝熱帯/亜熱帯緯度上限32、アジア夏雨帯の経度下限60・緯度上限41。栽培可否＝供給の偏在に直結する閾値 | Coefficients.RiceTropicalLatMax / RiceMonsoonLonMin / RiceMonsoonLatMax |
| Successor.SimCore/Climate.cs:15 | `45.0` | ジャガイモ自給帯の緯度下限（北緯45以上＝冷涼帯） | Coefficients.PotatoBeltLatMin |
| Successor.SimCore/Climate.cs:18 | `-30.0` | トウモロコシ自給帯の経度上限（新大陸判定＝lon≤-30） | Coefficients.MaizeBeltLonMax |
| Successor.SimCore/Colonial.cs:59 | `0.25` | 石油二択の供給実在ガード閾値＝必要量の1/4以上の継続生産がある時のみ石油へ振替 | Coefficients.ColonyFuelOilViableFrac |
| Successor.SimCore/Colonial.cs:118 | `(0.5 + col.Militancy)` | 搾取死亡率の基礎ウェイト（不満0でも0.5の基礎死亡係数） | Coefficients.ColonialMortalityBaseWeight |
| Successor.SimCore/Colonial.cs:119 | `Math.Min(0.9, ...)` | 植民地識字率の上限（0.9でキャップ） | Coefficients.ColonialLiteracyCap |
| Successor.SimCore/Colonial.cs:121 | `(0.3 + col.Literacy)` | 識字による不満成長の基礎ウェイト（識字0でも0.3の基礎項） | Coefficients.ColonialMilitancyLiteracyBase |
| Successor.SimCore/Colonial.cs:184 | `Math.Max(0.2, w.MilitaryReadiness)` | 植民地掌握力算定での即応度の下限（0.2） | Coefficients.ColonialHoldReadinessFloor |
| Successor.SimCore/Colonial.cs:207 | `col.Militancy = 0.2` | 独立達成時に不満をリセットする値（0.2） | Coefficients.ColonialPostIndependenceMilitancy |
| Successor.SimCore/Credit.cs:88 | `1.5` | 金融過熱度Heat(与信使用率)のクランプ上限（準備縮小時に100%超で1.5まで許容） | Coefficients.MaxCreditHeat |
| Successor.SimCore/CrownParliament.cs:28 | `0.5` | 破産召集が始まる緊縮強度の閾値（AusterityIntensity>0.5） | Coefficients.SummonParliamentAusterityThreshold（line29の(-0.5)*2.0 も同源） |
| Successor.SimCore/CrownParliament.cs:31 | `-0.5 / 0.7 / 0.3` | ストルイピン後退の発火閾値（協力度<-0.5・正統性>0.7・軍帰属>0.3） | Coefficients.CrownRollbackCoopThreshold / LegitimacyThreshold / ArmyAllegianceThreshold |
| Successor.SimCore/CrownParliament.cs:173 | `1.0 / 0.6 / 0.2 / 0.0` | 教育法段階→国民意識育成の効果係数（義務/公教育/宗教/なし） | Coefficients.EducationLevelWeight[]（line197 lawCeil の0.9/0.7/0.4/0.2 と重複概念） |
| Successor.SimCore/CrownParliament.cs:197 | `0.9 / 0.7 / 0.4 / 0.2` | 教育法段階→国民意識の天井（法の後退で成長停止） | Coefficients.NationBuildEducationCeiling[] |
| Successor.SimCore/CrownParliament.cs:217 | `0.6 / 0.3 / 1.0` | 国教法段階→宗教分裂抵抗の緩和率（内心の自由/信仰の自由/国教のみ） | Coefficients.StateReligionTolerance[] |
| Successor.SimCore/CrownParliament.cs:311 | `1.0 / 0.65 / 0.30 / 0.20 / 0.10 / 0.0` | 政体別の議会統制力の観測目標（共和/立憲/ブル独裁/プロ独裁/軍政/その他） | Coefficients.ParliamentControlByPolity[]（政体→統制力の較正テーブル） |
| Successor.SimCore/Diplomacy.cs:189 | `100` | SeedTension の緊張上限（0..100）。AddTension の上限(200)と非対称な裸リテラル | Coefficients.TensionMaxSeed |
| Successor.SimCore/Diplomacy.cs:358 | `0.9` | 制度疲労による軍質ペナルティの上限クランプ（最大90%減） | Coefficients.DecayMilitaryMaxPenalty |
| Successor.SimCore/Diplomacy.cs:466 | `0.0/0.15/0.2/0.35/0.45/0.60/1.0/0.50/0.3` | 政体ごとのイデオロギー座標（0=伝統権威…1=急進左派）。IdeologyTension の反目/親近を駆動する調整値の集合＋default 0.3 | データファイル or Coefficients.IdeologyCoord テーブル（Polity→座標） |
| Successor.SimCore/Diplomacy.cs:534 | `0.10/0.20/0.35/0.50/0.65/0.72/0.85` | 宗教ファミリーの文明座標（異ファミリー距離＝ReligionTensionの入力）。ファミリー名文字列＋座標の直書きテーブル | データファイル（宗教ファミリー座標テーブル） |
| Successor.SimCore/Diplomacy.cs:649 | `30` | 属国離反時に宗主へ積む遺恨の緊張増分 | Coefficients.DefectionResentmentTension |
| Successor.SimCore/Diplomacy.cs:859 | `0.3` | AI が低緊張相手と不可侵協定を結ぶ確率（rng draw の裸閾値） | Coefficients.NonAggressionFormChance |
| Successor.SimCore/Diplomacy.cs:868 | `0.4` | 非覇権時の防衛同盟締結確率（対覇権は c.AntiHegemonAllianceChance を使うのに通常時だけ裸） | Coefficients.AllianceFormChance |
| Successor.SimCore/Diplomacy.cs:901 | `0.2` | AI植民地化で標的を『考慮すらしない』最小到達可能性（投射×遠征補正）の閾値 | Coefficients.ColonizeConsiderProjectionMin |
| Successor.SimCore/Diplomacy.cs:1282 | `0.05` | 貴族士官の月間戦死上限＝貴族pop の5%/月 | Coefficients.OfficerCasualtyMonthlyCap |
| Successor.SimCore/Diplomacy.cs:1546 | `0.3 / 0.7` | 戦費を払えない場合の戦力充足フロア0.3（現物徴発）と支払い能力の寄与0.7（＝1−フロア） | Coefficients.WarFundingReadinessFloor |
| Successor.SimCore/EconomyStep.cs:620 | `0.95` | 健康保険の医薬品カバー率の上限（最大95%を国家が肩代わり） | Coefficients.InsuranceCoverageMax |
| Successor.SimCore/Factions.cs:54 | `st.Mutated ? 2.0 : 1.0` | 変異済みクラスタの結束成長倍率（旗の下の凝集＝2倍） | Coefficients.MutantCohesionGrowthMult |
| Successor.SimCore/Factions.cs:57 | `st.Cohesion >= 0.3` | 変異ロールを許す結束の最小閾値（「旗を安売りしない」頻度絞り） | Coefficients.FactionMutationCohesionGate |
| Successor.SimCore/Factions.cs:87 | `f.Cohesion < 0.05` | 運動体を「忘れる」（除去する）結束の下限閾値 | Coefficients.FactionForgetCohesion |
| Successor.SimCore/Fiscal.cs:157 | `3` | 国債金利/基準金利の比のクランプ上限（対外債容量の金利感応の上限倍率） | Coefficients.ForeignBondRateSensitivityCap |
| Successor.SimCore/Fiscal.cs:390 | `0.7 / 0.25` | 参政権(Suffrage)→国民運動の裾野スカラー。AdultMale=0.7・Census=0.25 | Coefficients.FranchiseWeightAdultMale/Census。Ideology.cs:214・Politics.cs:19 でも別値の Suffrage→scalar マップが散在（意味別だが要整理） |
| Successor.SimCore/Geo.cs:47 | `7, 30, -106, -86, 95, -95, 3, 42, 46, -82, -66` | BasinOf の海盆判定に使う緯度・経度の境界群（メキシコ湾/太平洋/地中海/インド洋/南米西岸のバウンディングボックス）。海路距離・維持費に効く地理閾値 | Coefficients もしくは Geo 内 const（BasinBox 定義群として命名） |
| Successor.SimCore/HistoricalReference.cs:33 | `0.90 / 0.50 / 0.55 / 0.85` | 忠実度の判定帯：固着(>0.90)・逸脱過多(<0.50)・◎7割帯(0.55-0.85) | Coefficients.Fidelity*Band（帯の境界を命名） |
| Successor.SimCore/Ideology.cs:73 | `2.0, 2.5, 1.5, 1.0` | 意識・闘争→各イデオロギー(反動/自由/共和/社会/共産/無政府)への創発加点の重み群 | Coefficients.Ideology*Weight 群（コメント自認：数値は first-cut・後に係数化） |
| Successor.SimCore/Ideology.cs:103 | `4 + 2 * rich; r[1]=2; r[4]=0.5` | 閉鎖身分(貴族/聖職)の階級基本イデオロギー基底重み(伝統4+2rich・反動2・ナショ0.5) | Coefficients.BaseIdeology_Closed_* 群（BaseWeights全体103-118行の基底表） |
| Successor.SimCore/Ideology.cs:107 | `4; 2; 1; 1` | 兵士の階級基本イデオロギー基底重み(ナショ4・帝国2・伝統1・反動1) | Coefficients.BaseIdeology_Soldier_* 群 |
| Successor.SimCore/Ideology.cs:111 | `2 * rentier + 0.5; 3.0/1.0; 2.0/1.0; 1.5` | 平民階級の基本イデオロギー導出係数群(伝統・自由・共和・社会・共産の各重み) | Coefficients.BaseIdeology_Common_* 群 |
| Successor.SimCore/Ideology.cs:214 | `0.95, 0.80, 0.55, 0.00` | 参政権→参加軸開放度の写像表(Universal/AdultMale/Census/None) | Coefficients.Openness_* 群（Politics.FreedomFromLaws の政治開放度表と姉妹関係） |
| Successor.SimCore/InternationalMarket.cs:253 | `Math.Min(0.9, margin)` | 輸出閾値マージンの上限0.9（世界価格の90%超は割引かない下限ガード）。209行でも同値で再登場 | Coefficients.MaxExportMargin（209/253行で共有） |
| Successor.SimCore/InternationalMarket.cs:323 | `0.85, 1.18` | 世界価格の1期あたり変化率クランプ（最大下落15%・最大上昇18%） | Coefficients.WorldPriceStepDown / WorldPriceStepUp |
| Successor.SimCore/Meritocracy.cs:257 | `0.5, 0.6, 0.7, 0.5, 0.3, 0.35, 0.1` | 政体→近代性スコア表（PolityModernity）。7つの裸の調整値がswitchに直書き | Coefficients の PolityModernity 係数群 or データファイル（政体別テーブル） |
| Successor.SimCore/Military.cs:40 | `Math.Max(0.05, ...)` | 国民意識ゲートによる動員深さの下限（低国民意識でも最低5%は動員可） | Coefficients.MobilizationGateFloor |
| Successor.SimCore/Military.cs:88 | `pool * 0.5` | 1ヶ月の徴募上限＝プール（農民＋労働者）の半分まで | Coefficients.SoldierDrawMaxPoolFrac |
| Successor.SimCore/MultiNationSim.cs:50 | `0.15 + 0.10 *` | 初期 TradeWeight = 基準0.15 + 工業化係数0.10。動的経路(664行)は Coefficients.TradeWeightBase/TradeWeightGdpSlope を使うのに、初期化はここで裸 | Coefficients.TradeWeightInitBase / TradeWeightInitIndSlope |
| Successor.SimCore/MultiNationSim.cs:786 | `0.15 と 1.5` | 労働力価格の正規化値幅（min0.15/max1.5）で労働市場逼迫を0..1化＝賃金プレミアムの閾値 | Coefficients.WageNormMinPrice / WageNormMaxPrice |
| Successor.SimCore/MultiNationSim.cs:877 | `0.15, 0.85` | 移民の労働者比率のクランプ上下限（着地階級の飽和境界） | Coefficients.ImmigrantWorkerFracMin / ImmigrantWorkerFracMax |
| Successor.SimCore/MultiNationSim.cs:883 | `0.6` | 国際移民の持参金＝着地州の1人当たり貯蓄の6割（やや貧しい移民） | Coefficients.ImmigrantDowryFrac |
| Successor.SimCore/NationConfig.cs:61 | `0.10` | 初期税率（ToScenario にハードコードのシナリオ既定・全国家共通） | Coefficients.InitialTaxRate |
| Successor.SimCore/Parliament.cs:164 | `40` | 適格クラスタ不在で解党する四半期数（＝10年） | Coefficients.PartyDissolutionQuarters |
| Successor.SimCore/Parliament.cs:279 | `0.3 / 0.1 / 0.0` | 保守党綱領の政策目標（保護関税0.3・低福祉0.1・累進0） | Coefficients の党綱領テーブル（ConservativePlatform.Tariff 等） |
| Successor.SimCore/Parliament.cs:288 | `0.1 / 0.15` | 自由党綱領の政策目標（自由貿易関税0.1・穏健福祉0.15） | Coefficients の党綱領テーブル（LiberalPlatform） |
| Successor.SimCore/Parliament.cs:311 | `0.5 / 0.5` | 社会党綱領の政策目標（高福祉0.5・累進0.5） | Coefficients の党綱領テーブル（SocialistPlatform） |
| Successor.SimCore/PolicyPreferences.cs:90 | `1.5` | 累進課税で近代エリート(資本)に上乗せする反対強度 | Coefficients.ProgressiveTaxModernExtra |
| Successor.SimCore/PolicyPreferences.cs:92 | `0.34 / (BurdenScale + 1.0)` | 土地税で純受益/純負担を分ける地代シェア閾値(0.34)と地主の追加反対強度(+1.0) | Coefficients.LandTaxPayerThreshold / LandTaxExtraBurden |
| Successor.SimCore/PolicyPreferences.cs:98 | `2.0` | 社会保障(福祉)の富裕層への財源負担反対強度 | Coefficients.WelfareWealthBurden |
| Successor.SimCore/Politics.cs:19 | `0.65, 0.55, 0.50, 0.20` | 参政権→政治的開放度(探究/革新の自由度)の写像表 | Coefficients.FreedomPolitical_* 群（制約解として文書化済だが裸） |
| Successor.SimCore/Politics.cs:24 | `0.25` | 報道自由が政治的開放度に寄与する重み | Coefficients.FreedomPressWeight |
| Successor.SimCore/Politics.cs:60 | `0.15, 0.1` | 宗教教育/無教育での識字→政治意識の変換係数 | Coefficients.ConsciousnessConv_Religious / _None |
| Successor.SimCore/Politics.cs:104 | `0.75 + 0.5` | 政治意識の報道倍率の基底(0.75)と自由化レンジ(0.5)＝StatePress0.75/FreePress1.25 | Coefficients.PressConsciousnessFloor / PressConsciousnessRange |
| Successor.SimCore/Politics.cs:373 | `0.5` | 決定的敗戦の判定閾値（ResolvePeace注入直後の高水準と減衰残滓の区別） | Coefficients.DecayDefeatShockThreshold |
| Successor.SimCore/Politics.cs:973 | `0.5 : 0` | 首都との文化不一致・宗教不一致がそれぞれ分離志向へ加える距離量 | Coefficients.CultureMismatchDistance / ReligionMismatchDistance |
| Successor.SimCore/PopDecision.cs:48 | `0.85` | 都市化シェアの絶対上限（工業化天井の最終クランプ上限）。都市化の物理的天井 | Coefficients.MaxUrbanShareCeiling |
| Successor.SimCore/SelfPlay.cs:16 | `* 2.0 ... / 12.0` | 国力スコアの軍事項の重み(2.0)と逓減キャップ(12.0＝m/(1+m/12)) | Coefficients.ScoreMilitaryWeight / Coefficients.ScoreMilitaryCap |
| Successor.SimCore/SelfPlay.cs:19 | `12000.0` | 国力スコアのGDP規模の除数（経済規模の基準スケール） | Coefficients.ScoreGdpScale |
| Successor.SimCore/SelfPlay.cs:22 | `12.0` | GDP規模項の限界逓減の分母（大経済を上限へ漸近） | Coefficients.ScoreGdpDiminish |
| Successor.SimCore/SelfPlay.cs:24 | `9.0` | 国力スコアの近代産業（工業化度）の重み | Coefficients.ScoreIndustrializationWeight |
| Successor.SimCore/SelfPlay.cs:25 | `12.0` | 国力スコアの生活水準（満足度）の重み | Coefficients.ScoreSatisfactionWeight |
| Successor.SimCore/SelfPlay.cs:26 | `1.5` | 国力スコアの植民地項の重み | Coefficients.ScoreColonyWeight |
| Successor.SimCore/SelfPlay.cs:33 | `50.0 ... 0, 6.0` | 金融項の1人あたり与信スケール(50.0)と上限(6.0) | Coefficients.ScoreFinancePerCapita / Coefficients.ScoreFinanceCap |
| Successor.SimCore/SelfPlay.cs:105 | `4.0` | 工業化余地の到達目標（コメントで均衡≈3.7と明記） | Coefficients.IndustrializationTarget |
| Successor.SimCore/SelfPlay.cs:106 | `0.7` | 不満（満足度不足）の基準閾値 | Coefficients.UnrestSatisfactionTarget |
| Successor.SimCore/SelfPlay.cs:111 | `1.0 / 0.5 / 0.3 / 1.2 / 1.5 / 2.0 / 1.2` | スタンス効用の重み群（uMod/uInd/uWel/uFis）＝技術差・工業化余地・不満・飢饉・財政の係数 | Coefficients.StanceUtility*（各重みを命名） |
| Successor.SimCore/SelfPlay.cs:136 | `2.5 ... 0.5) / 0.5` | 軍国教義の軍拡ベース効用(2.5)と破産時の逓減の閾値/幅(0.5) | Coefficients.MilitaristStanceBias / FiscalReliefKnee |
| Successor.SimCore/SelfPlay.cs:137 | `2.0 ... / 100.0` | 緊張由来の軍拡効用重み(2.0)と緊張の正規化スケール(100.0) | Coefficients.TensionMilitaryWeight / TensionScale |
| Successor.SimCore/SelfPlay.cs:142 | `1.6 / 1.2 / 1.5 / 1.2` | 教義バイアスの効用増幅倍率（Industrialist/Modernizer） | Coefficients.Doctrine*Bias |
| Successor.SimCore/SelfPlay.cs:236 | `400 / 4000` | 圧政目標の既定値(400)と軍備スタンス値(4000＝289行) | Coefficients.CoercionDefault / CoercionMilitarized |
| Successor.SimCore/SelfPlay.cs:315 | `0.08 / 0.05 / 0.7 / 0.45 / 0.22` | 反景気循環金融政策：後退閾値(0.08)・緩和金利(0.05)・過熱閾値(0.7)・引締金利(0.45)・中立金利(0.22) | Coefficients.MonetaryEase* / MonetaryTighten* / MonetaryNeutralRate |
| Successor.SimCore/SelfPlay.cs:337 | `0.6` | 奢侈財への消費税の上限率（AIの天井） | Coefficients.LuxuryConsumptionTaxCap |
| Successor.SimCore/SelfPlay.cs:343 | `0.15 / 0.5 / 0.6 / 1.2 / 0.7` | 反腐敗投資のゲート：汚職閾値(0.15)・財政逼迫上限(0.5)・債務GDP上限(0.6)・投資係数(1.2)・上限(0.7) | Coefficients.AntiCorruption*（閾値・係数・上限を命名） |
| Successor.SimCore/SelfPlay.cs:423 | `0.35 / 0.6` | 優遇/弾圧発火の闘争度閾値（議会系0.35・非議会系0.6） | Coefficients.FavorMilitancyThreshold / SuppressMilitancyThreshold |
| Successor.SimCore/SelfPlay.cs:529 | `0.20 / 0.08 / 12 / 0.18 / 0.55 / 0.12` | 恐慌計測の閾値群：入り(0.20)・脱出(0.08)・助走四半期(12)・GDP平滑(0.18)・過熱印(0.55)・不作印(0.12) | Coefficients.Depression*（局所constをハーネス係数へ集約） |
| Successor.SimCore/Simulation.cs:322 | `7.0` | 全生産性を底上げする工業化スケール（全生産者の Productivity に乗る中核調整値）。命名const化済みだが Coefficients 未収容 | Coefficients.ProductivityScale |
| Successor.SimCore/Simulation.cs:512 | `0.4 / 4.0 / 3.5 / 1.8` | 資源賦存の調整値：非列挙RGO産出0.4（ResourceScarcity）・鉱物4.0/他3.5（ResourceEndowment）・耕地余裕1.8（LandHeadroom）。命名const化済だがCoefficients未収容 | Coefficients.ResourceScarcity / MineralEndowment / OtherEndowment / LandHeadroom |
| Successor.SimCore/Simulation.cs:563 | `250.0 / 0.3` | 徴募：兵力1単位=250人の換算と、農工人口の30%を徴募上限とする係数を直書き | Coefficients.SoldiersPerMilitaryUnit / MaxDraftFraction |
| Successor.SimCore/Simulation.cs:677 | `5.0` | 教育（教師）の生産性を直書き。679行 医療4.0・681行 行政3.0・692行 高度労働力4.0・672行 労働力等も裸 | Coefficients.EducationProductivity / HealthcareProductivity / AdminProductivity |
| Successor.SimCore/Simulation.cs:949 | `0.25, 4.0` | 労働価格プレミアムの対国平均比クランプ下限/上限。950行のEMA重み 0.75/0.25 も裸 | Coefficients.WagePremiumMin / WagePremiumMax / WagePremiumEmaAlpha |
| Successor.SimCore/Simulation.cs:1169 | `0.95 / 0.98` | 労働組合法段階（容認/統制）による工場出力の乗数を直書き | Coefficients.UnionToleratedOutput / UnionControlledOutput |
| Successor.SimCore/Simulation.cs:1188 | `2.0 / 0.3` | 技術→工場生産性：技術2.0までは線形・超過分は係数0.3で逓減する閾値と傾き | Coefficients.TechProdKnee / TechProdSlopeAbove |
| Successor.SimCore/Simulation.cs:1622 | `3.0 / 1.0` | 鉱物発見の地質ポテンシャル重み（既知鉱物州=3倍）を直書き | Coefficients.MineralProneWeight |
| Successor.SimCore/TechEducation.cs:98 | `0.95` | 制度疲労による採用速度低下の上限（採用速度は最大95%まで削がれる） | Coefficients.DecayTechAdoptMaxCut |
| Successor.SimCore/TechEducation.cs:163 | `0.0 / 0.1 / 1.0 / 1.2` | 教育法ごとのエリート創発乗数（世俗水路の強さ）。宗教0.1・義務1.2など重みがswitch直書き | Coefficients.EliteMultReligious/Public/Compulsory（0.0は自明） |
| Successor.SimCore/Territory.cs:64 | `Math.Clamp(c.RegionMobility * gap, 0, 0.2)` | 地域間移民の1ステップ流出率の上限（20%） | Coefficients.RegionMigrationMaxFrac |
| Successor.SimCore/Validation.cs:119 | `0.40/0.30, 0.25/0.15, 0.55/0.45, 0.38/0.30, 0.50/0.20, 11/5` | 検証ハーネスの合格/注意の帯（価格中間帯・満足度・工業化差別化・世界価格発見の各基準線） | Coefficients.Validation*（ハーネス基準線を命名・回帰基準の一元管理） |
| Successor.SimCore/World.cs:59 | `14.0` | 実労働時間の初期値（14時間/日）。倍率排除②の連続労働時間の出発点 | Coefficients.InitialWorkHoursPerDay |
| Successor.SimCore/World.cs:93 | `0.6` | 物品別消費税率の上限（希望率を0〜0.6にクランプ） | Coefficients.ConsumptionTaxRateCap |
| Successor.SimCore/World.cs:312 | `0.7` | 疫病致死率の衛生整備による低減係数（衛生充足×0.7 まで致死率を下げる）。コメント『衛生整備0.7』と対応 | Coefficients.EpidemicSanitationEffect |
| Successor.SimCore/World.cs:524 | `-0.2, 0.2` | 為替バイアス（貿易収支＋政策金利）のクランプ上下限（±0.2） | Coefficients.ExchangeBiasClamp |
| Successor.SimCore/World.cs:652 | `0.5` | 工業化投資で四半期に注ぐ国庫割合（IndustryInvestFrac 満額でも国庫の半分/四半期＝暴走防止の上限）。midpoint ではなく明示的レバー上限 | Coefficients.IndustryInvestTreasuryFracPerQuarter |
| Successor.SimCore/World.cs:664 | `100.0` | 外的脅威の正規化スケール（最大緊張=100 で割って0..1化）。戦争熱入力の閾値スケール | Coefficients.MaxTensionScale（緊張の満点＝定義の単一ソース化） |
| Successor.SimCore/World.cs:715 | `10_000.0` | 侵略の距離ペナルティ正規化スケール（最寄り植民地km/10000） | Coefficients.InvadeDistanceRefKm |

### 6.3 優先度 LOW（軽微・境界的） ― 97件

#### ハードコード名（国名/州名/財名/文明などの文字列比較）（3件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Factions.cs:32 | `["太平", "千年", "救世", "光明", "天命", "新生", "浄化", "黎明", "再臨", "弥勒"]` | 変異名の語幹テーブル（千年王国運動の語彙）。ロジック定数ではないがハードコードされたコンテンツ | データファイル（names.json 等）へ外部化 |
| Successor.SimCore/Factions.cs:33 | `["会", "団", "軍", "同盟"]` | 変異名の接尾テーブル。ハードコードされたコンテンツ語彙 | データファイルへ外部化（MutantStems と同所） |
| Successor.SimCore/NationConfig.cs:73 | `"フランス","日本","イギリス","アメリカ","ロシア","プロイセン","オスマン","オーストリア","ネーデルラント","スペイン","ポルトガル","サルデーニャ＝ピエモンテ"` | 国別シナリオ工場メソッドが world.json の国名文字列を直書きで ByName 検索（12件）。表示名変更で全滅する結合 | world.json 側に安定キー(NationKey)を持たせ ByKey 参照。setup 便宜関数なので優先度は低いが名前結合の一覧 |

#### 固定インデックス（Nations[2]=英 等の魔法索引）（5件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Diplomacy.cs:1472 | `0 (Regions[0] / Id==0)` | 首都＝Regions[0]かつ Id==0 の固定索引前提。行1485/1495/1500/1515でも首都判定/昇格に Id==0 と先頭挿入が散在（文書化済み不変条件だが裸） | const CapitalRegionId=0 / World.Capital プロパティ |
| Successor.SimCore/EconomyStep.cs:741 | `rank < 3` | 役務の階層配分ランク数＝3（ServiceRankが返す0/1/2の総数）。ServiceRankの分類と暗黙結合した裸の個数 | const int ServiceRankCount = 3（ServiceRankと同居） |
| Successor.SimCore/Ideology.cs:83 | `r[9] ... (r[1] + r[4])` | ファシズム生成が反動(1)と国民主義(4)から recruit する裸のイデオロギー索引 | Ideology enum 定数（r[(int)Ideology.Fascism]等・全域でr[数値]索引が散在） |
| Successor.SimCore/Simulation.cs:1672 | `Markets[0], Markets[1], Pops[0], Pops[1]` | プローブ計測で食料市場=Markets[0]・工業=Markets[1]・農民=Pops[0]・労働者=Pops[1] の固定レイアウトを前提 | good-id/ClassId で引く（Markets を id 検索・Pops を ClassId 検索）。テレメトリなので影響は限定的だが順序前提は脆い |
| Successor.SimCore/World.cs:139 | `3, 3, 4` | 固定次元の直書き＝PartySeats/VoteShares の党ブロック数3（保守/自由/社会）と RegimeTrackRecord の政体系統数4（君主/議会制/強権/急進） | const PartyBlocCount=3 / RegimeTrackCount=4（enum 由来の named 定数） |

#### 重複リテラル（同値が複数箇所に散在＝一箇所に集約すべき）（10件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:530 | `"保守党" / "自由党" / "社会党"` | 政党名リテラル。PartyName(530)とPOP支持政党(2607)で二重定義（表示のみだが党名の散在） | static readonly PartyNames[] 単一定義（enum Party と対） |
| Successor.SimCore/Catalog.cs:361 | `24` | DAG緩和の反復回数。ComputeRefPrices(497行)にも同一の 24 が出現＝連鎖最大深(≈9)に対する収束反復数の重複マジック | private const int DagRelaxIterations = 24;（両メソッドで共有） |
| Successor.SimCore/CrownParliament.cs:219 | `100.0` | 外交緊張(MaxTension)を[0,1]へ正規化する緊張の最大スケール | const double MaxTension = 100（緊張スケールの名前付き定数・他所の緊張正規化と共有） |
| Successor.SimCore/Diplomacy.cs:310 | `0.5` | 拿捕賞金の制海権多数派閾値＝sc≤0.5は拿捕不可。行311(sc-0.5)*2、行331(SeaControl>0.5)にも同じ制海過半の閾値が散在 | const SeaControlMajority=0.5（複数箇所と共有） |
| Successor.SimCore/Fiscal.cs:291 | `1e9` | GDP≈0時の債務/GDP比の飽和センチネル値（超過債務扱い） | const DebtGdpSentinel。CapitalMarket.cs:44 は同概念(債務/GDP・GDP不在)に別値 9 を使用＝不整合 |
| Successor.SimCore/Government.cs:47 | `4` | 社会保障法の段階数（なし0/救貧/労災/失業/年金=4段）を割って0..1へ正規化する分母 | SocialSecurityLaw の段階数を定数化（(int)max のenum値）。MultiNationSim.cs:246 に同じ /4.0 が散在 |
| Successor.SimCore/Parliament.cs:275 | `0.5` | 過半数（第一党が単独過半に届くか）の議席閾値 | const double MajoritySeatShare = 0.5（line322/325 の少数与党判定と共有） |
| Successor.SimCore/Simulation.cs:67 | `0xC0FFEE_D15EA5EUL / 0xBADC0FFEE_0DDF00DUL / 0xFAC7_1035_EED5UL` | 独立RNG系列を作るためのseed XORソルト（疫病68行・作物・F3変異112行）。決定論に効く定数だが分散配置 | const ulong EpidemicRngSalt 等の命名定数（1箇所に集約） |
| Successor.SimCore/Territory.cs:98 | `Math.Max(0.1, pushPenalty) / Math.Max(0.1, housingFriction) / Math.Max(0.1, wagePull)` | 機会算定の各乗数（プッシュ罰・住宅摩擦・賃金プル）の共通下限0.1が3箇所に散在 | Coefficients.OpportunityFactorFloor（line89/98で共有） |
| Successor.SimCore/Trade.cs:128 | `Full => 1.0, Limited => 0.5` | 穀物法の保護強度マッピング（限定=0.5/全面=1.0）。InternationalMarket.cs:388に同一定義が重複 | 共通ヘルパ or Coefficients.CornLawLimitedStrength（2ファイルで共有） |

#### 暦定数（3=月/四半期・4・90・360・年号）（9件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.Production.cs:388 | `90.0` | 着工初日労務費算出の四半期→日数換算(四半期=90日) | const DaysPerQuarter=90 |
| Successor.App/ViewModels/GameViewModel.cs:402 | `360.0 / 4.0` | 建設残り日数→年(360)・spec.Quarters→年(四半期4)換算。生産タブの着工ラベル | const DaysPerYear=360 / QuartersPerYear=4 |
| Successor.App/ViewModels/GameViewModel.cs:2554 | `4` | 四半期スタンプ＝Year*4+Quarter（年4四半期）。四半期境界検出 | const QuartersPerYear=4 |
| Successor.SimCore/CrownParliament.cs:160 | `4.0` | 実績信認の在任年数ゲートを四半期へ換算する年→四半期係数 | const int QuartersPerYear = 4（暦定数の共有） |
| Successor.SimCore/Fiscal.cs:125 | `12.0` | 年→月換算（クーポン年率を月次利払いへ）＝月/年 | const int MonthsPerYear = 12（暦定数・全域で反復） |
| Successor.SimCore/Fiscal.cs:136 | `3.0` | 四半期→月換算（関税実収は四半期実行なので月次へ割る）＝月/四半期 | const int MonthsPerQuarter = 3 |
| Successor.SimCore/Fiscal.cs:192 | `2.0` | 月次上限を四半期整合にする係数（1+gating*2 で ÷3＝四半期3か月） | MonthsPerQuarter-1 に由来（暦定数の派生） |
| Successor.SimCore/Fiscal.cs:290 | `11.0` | 月次GDPの年換算係数（1+11=12ヶ月） | MonthsPerYear-1 に由来（暦定数の派生） |
| Successor.SimCore/Politics.cs:654 | `0.25` | 内戦の四半期進行＝1四半期=0.25年（暦の分数） | const QuarterYears = 0.25（CivilWarYearsは年単位で四半期毎に減算） |

#### 調整係数（Coefficients へ移すべき裸の率/閾値/倍率/重み/上限）（67件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.App/ViewModels/GameViewModel.cs:587 | `4 / 8` | 開始前の助走回数＝経済4四半期・技術8反復。WarmupEconomy(4)/WarmupTech(8)の調整値 | Coefficients.WarmupEconomyQuarters / WarmupTechIterations |
| Successor.App/ViewModels/GameViewModel.cs:868 | `0.6 / 0.15 / -0.15 / -0.6` | 軍の帰属(−1議会〜+1王権)を5段階ラベルへ写す境界値 | static readonly ArmyAllegianceBands 閾値配列 |
| Successor.App/ViewModels/GameViewModel.cs:1382 | `0.15 / 0.01` | 法律支持行の賛否判定＝選好±0.15超で賛成/反対・勢力シェア0.01未満は無視 | Coefficients.StanceDisplayThreshold / MinBlocShareShown |
| Successor.App/ViewModels/GameViewModel.cs:1574 | `0.6 / 0.4` | 最後通牒ポップアップの戦況ラベル閾値（勝勢≥0.6優勢・≤0.4劣勢） | Coefficients.WarOutlookFavorable / Unfavorable |
| Successor.App/ViewModels/GameViewModel.cs:1591 | `20` | 緊張ポップアップの再プロンプト解除マージン（目標より20下がったら再提示可） | Coefficients.TensionRepromptMargin |
| Successor.SimCore/Agents.cs:31 | `0.25` | ModernizerAgent(テスト/デモarchetype)が設定する所得税の目標率 | Coefficients.ModernizerIncomeTaxTarget（またはUtilityAgentへ統合済みの効用フレームへ） |
| Successor.SimCore/Agents.cs:32 | `200` | ModernizerAgentの圧政(Coercion)目標水準の絶対値 | Coefficients.ModernizerCoercionTarget |
| Successor.SimCore/Agents.cs:33 | `0.25` | 制限選挙(Census)へ拡大する技術水準の分岐閾値。同行の別の0.25(所得税)とは無関係の別意味 | Coefficients.ModernizerCensusTechThreshold |
| Successor.SimCore/Calibration.cs:60 | `0.02` | 「都市化した」と判定する UrbanShare の増分閾値 | Coefficients.UrbanizedDeltaThreshold |
| Successor.SimCore/Calibration.cs:99 | `[0.01, 0.0175, 0.03, 0.05] / [0.5, 1.2, 2.0]` | 係数探索の格子：正統性侵食率とReformPressureBoostの掃引値 | 探索グリッド定義（データ化・regen変更時の同倍率追従の明示） |
| Successor.SimCore/Catalog.cs:383 | `0.25, 0.20, 0.15, 0.10, 0.06, 0.04, 0.03, 0.02` | 財別の月次腐敗率の階梯（パン0.25〜金属0.005）。id リスト＋腐敗率が switch に直書き＝財プロパティの調整値 | goods.json の財プロパティ Spoilage（財定義に同居） |
| Successor.SimCore/Catalog.cs:396 | `6.0, 3.0, 2.5, 0.4` | 財の輸送重量階梯（巨大構造物6.0/大型機械3.0/嵩高原料2.5/小高価値0.4・既定1.0）。輸送費に効く | Coefficients.TransportWeight*（階梯値の命名）／goods.json 財プロパティ化 |
| Successor.SimCore/Colonial.cs:110 | `> 0.5` | ColonialInfraPerColony を bool フラグとして解釈する閾値（0/1旗の中点） | bool係数化 or フラグ用const（中点0.5・低優先） |
| Successor.SimCore/Colonial.cs:118 | `Math.Max(0.5, ...)` | 植民地人口成長率の月次下限（最大50%減までクランプ） | Coefficients.ColonialPopGrowthFloor |
| Successor.SimCore/CrownParliament.cs:88 | `0.2 / 1.8` | 天命による議会/王権圧力レート変調のクランプ下限・上限 | Coefficients.PolityBreathMin / PolityBreathMax |
| Successor.SimCore/CrownParliament.cs:198 | `0.6` | 国民意識天井を満額化する平均識字の基準値 | Coefficients.NationBuildLiteracyReference |
| Successor.SimCore/Diplomacy.cs:24 | `0.2` | 世界市場での国の交易比重(TradeWeight)の既定値（禁輸で失う交易シェアの基準） | Coefficients.DefaultTradeWeight（または world.json） |
| Successor.SimCore/Diplomacy.cs:45 | `0.05` | 植民地の初期識字率の既定値 | Coefficients.ColonyInitialLiteracy |
| Successor.SimCore/Diplomacy.cs:50 | `0.5 / 0.5` | 植民地の争奪価値＝資源数＋固定加点0.5＋人口×0.5（人口ウェイト） | Coefficients.ColonyValueBase / ColonyValuePopWeight |
| Successor.SimCore/Diplomacy.cs:590 | `0.5 / 1.0` | 民族距離＝同グループ(汎スラヴ等)=0.5・異グループ=1.0（GovCost/緊張の距離ロジック入力） | Coefficients.EthnicSameGroupDistance |
| Successor.SimCore/Diplomacy.cs:904 | `100_000.0` | 植民地化魅力の経済規模項の正規化スケール（係数名 ColonizeValueGdpPer100k が前提とする10万GDP単位） | const ColonizeGdpNormUnit=100000 |
| Successor.SimCore/Diplomacy.cs:1368 | `0.05` | 征服希釈後の中核民族割合(CoreEthnicShare)の下限フロア | Coefficients.CoreEthnicShareFloor |
| Successor.SimCore/EconomyStep.cs:209 | `Math.Max(0.5, ...)` | アヘン依存による労働供給ペナルティの床（成人労働の最低0.5＝最大−50%） | Coefficients.OpiumLaborFloor |
| Successor.SimCore/Factions.cs:54 | `(0.5 + consc)` | 結束成長率の意識ベース下駄（意識0でも成長率が0.5倍残る底上げ） | Coefficients.FactionCohesionConscBase |
| Successor.SimCore/Fiscal.cs:364 | `0.5` | 年金改革レベル Limited のスケール（Full=1.0に対する半額給付） | Coefficients.ReformLevelLimitedScale（ReformLevel→係数の共通マップ） |
| Successor.SimCore/Fiscal.cs:391 | `0.5` | 国民運動の熱意zealの参政権による逓減係数（獲得後は半減） | Coefficients.NationBuildZealFranchiseDamp |
| Successor.SimCore/Government.cs:118 | `0.05` | スライダーの丸め刻み（5%＝20段のUI/AI共通仕様） | const double SliderStep = 0.05（同関数内で2回出現） |
| Successor.SimCore/Ideology.cs:151 | `IdeologyCoord/PolityCoord 4軸座標表` | 10思想×4軸・9政体×4軸の制度座標行列（親和距離の全駆動因） | データファイル(ideology.json/politics.json)への外部化（sweep較正対象の設計行列） |
| Successor.SimCore/InterestGroups.cs:41 | `/ 0.5` | 近代/大衆の発言権を基準0.5で正規化（等倍化）する定数 | Coefficients.FranchiseVoiceNormalizer（基準発言権） |
| Successor.SimCore/Model.cs:121 | `0.3` | pop 子供比率の既定初期値（①年齢コホート） | Coefficients.DefaultChildShare |
| Successor.SimCore/Model.cs:283 | `0.4` | 州の女性構造的労働参加率の既定初期値 | Coefficients.DefaultFemaleParticipation |
| Successor.SimCore/MultiNationSim.cs:412 | `0.5` | 疫病伝染時の重症度ベース（0.5 + 乱数で [0.5,1.5]） | Coefficients.EpidemicTransmitSeverityBase |
| Successor.SimCore/MultiNationSim.cs:504 | `1.0` | 運河による海路短縮の最小閾値(km)＝これ未満しか縮まない国は課金対象外 | Coefficients.CanalTollMinRouteSavingKm |
| Successor.SimCore/MultiNationSim.cs:735 | `0.5` | 人口0時の平均識字フォールバック値 | const 既定識字（軽微・境界的） |
| Successor.SimCore/NationConfig.cs:15 | `400, 800` | 商人・職人の階級サイズ既定初期値（Merchant=400・Artisan=800） | データファイル（world.json）へ一本化 |
| Successor.SimCore/NationConfig.cs:17 | `0.95, 0.15, 0.40, 0.80, 0.85` | 階級別識字率のプログラム既定値（NobLit/PeasLit/WorkLit/BourgLit/ProfLit。world.json 未指定時のフォールバック初期値） | データファイル（world.json）に一本化 or Coefficients.DefaultClassLiteracy* |
| Successor.SimCore/NationConfig.cs:20 | `0.7` | 農民の自給率の既定初期値 | Coefficients.DefaultPeasantSubsistence（またはデータ） |
| Successor.SimCore/NationConfig.cs:23 | `400` | 圧政強度の既定初期値（基準人口1000あたり） | Coefficients.DefaultCoercion（またはデータ） |
| Successor.SimCore/NationConfig.cs:36 | `0.85` | 民族統一度の既定初期値（史実1789） | Coefficients.DefaultEthnicUnity（またはデータ） |
| Successor.SimCore/NationConfig.cs:47 | `0.25` | 初期金利の既定値 | Coefficients.DefaultInitialInterestRate（またはデータ） |
| Successor.SimCore/NationConfig.cs:61 | `0.5` | 初期人頭税率（ToScenario ハードコード・近世財政の国庫支柱）。midpoint ではなく明示的初期値 | Coefficients.InitialPollTaxRate |
| Successor.SimCore/NationConfig.cs:62 | `0.3` | 初期外圧（ToScenario ハードコード・全国家共通のシナリオ既定） | Coefficients.InitialExternalPressure |
| Successor.SimCore/Policy.cs:77 | `8.0 / 10.0 / 12.0 / 14.0` | 労働時間法→実労働時間アンカー（無制限=14時間は較正値） | Coefficients.UnlimitedWorkHours（14のみ較正値・8/10/12は法名由来で境界的） |
| Successor.SimCore/PolicyPreferences.cs:74 | `0.5` | 国家の歳入要求の基礎値（財政逼迫stressに常時加わる基底） | Coefficients.StateRevenueBaseDemand |
| Successor.SimCore/PolicyPreferences.cs:127 | `2.0` | 土地改革への大衆の強い賛成強度 | Coefficients.LandReformMassSupport |
| Successor.SimCore/PolicyPreferences.cs:138 | `-1.5` | 国有化に対する近代エリート(資本)の強い反対強度 | Coefficients.NationalizationCapitalOpposition |
| Successor.SimCore/Politics.cs:32 | `0.6` | 検閲付(Censorship)状態の報道自由度スカラー | Coefficients.PressFreedomCensored |
| Successor.SimCore/Politics.cs:166 | `* 0.5` | 4軸[0,1]の最大距離2で政治的乖離を[0,1]へ正規化する係数(1/2) | const 由来（4軸次元＝MaxAxisDistance=2 から導出すべき定数） |
| Successor.SimCore/Politics.cs:278 | `0.2, 1.8` | 天命による正統性侵食/再生の増幅クリップ(対称安全弁)の上下限 | Coefficients.DynMandateAmplifyClampMin / Max |
| Successor.SimCore/Politics.cs:491 | `0.05` | 形態別実績ストック乗数の符号保存フロア | Coefficients.RegimeTrackRecordFloor |
| Successor.SimCore/Politics.cs:744 | `< 0.5` | 本拠州が少数派＝分離の内乱と判定する中核民族割合の閾値。980/1026行系と同義 | Coefficients.MinorityRegionThreshold |
| Successor.SimCore/PopDecision.cs:32 | `-40, 40` | 移民softmaxの指数 z のクランプ幅（Math.Exp オーバーフロー防止のガード） | const double SoftmaxExpClamp = 40（数値ガード・境界的） |
| Successor.SimCore/SelfPlay.cs:76 | `40.0` | 外圧（黒船）判定の対外緊張閾値 | Coefficients.BlackShipTensionThreshold |
| Successor.SimCore/SelfPlay.cs:237 | `0.25 / 0.7` | 軍事費目標の既定(0.25)と軍備スタンス(0.7＝290行) | Coefficients.MilSpendDefault / MilSpendMilitarized |
| Successor.SimCore/SelfPlay.cs:254 | `0.25/0.4/0.3/0.2/0.6/0.5/0.15/0.35 等` | 各スタンスのAI税制・支出目標の直書き値群（Modernize/Industrialize/Welfare/Fiscal/Militarize の多数のTarget） | Coefficients.Stance*Target 群（AIの政策プロファイルを命名・外部化） |
| Successor.SimCore/SelfPlay.cs:375 | `1.3` | ダンピング発動の輸出/輸入超過比率 | Coefficients.DumpingExportSurplusRatio |
| Successor.SimCore/SelfPlay.cs:592 | `20` | デッドロック疑いのスコア不変四半期数の閾値 | Coefficients.DeadlockFrozenQuarters |
| Successor.SimCore/SelfPlay.cs:644 | `0.5` | 常勝/即死フラグの試合数シェア閾値（過半＝支配とみなす判定・646行にも重複） | Coefficients.DominanceFlagShare |
| Successor.SimCore/SelfPlay.cs:652 | `0.25` | 大恐慌（ピーク比25%超収縮）の判定閾値 | Coefficients.GreatDepressionDrawdown |
| Successor.SimCore/Simulation.cs:546 | `0.40, 0.35, 0.25` | インテリ層を教師/官僚/医者へ分解する構成比を直書き | Coefficients/NationConfig の専門職構成比 |
| Successor.SimCore/Simulation.cs:849 | `50, 35, 45, 45, 10` | 初期外交緊張の各値（英仏植民地競争50・グレートゲーム45等）を SeedTension に直書き | world.json のシナリオ初期緊張テーブル |
| Successor.SimCore/Simulation.cs:993 | `0.80` | 年表に凶作を記録する収穫率の表示閾値（平年8割未満） | Coefficients.HarvestLogThreshold（低優先・表示系） |
| Successor.SimCore/Simulation.cs:1003 | `0.9` | 作物病害の新規発生を許す健全度閾値（回復済みの作物のみ再発） | Coefficients.CropDiseaseHealthyThreshold |
| Successor.SimCore/Sweep.cs:18 | `[300, 2500] / [0.0, 0.7]` | sweepの圧政・外圧の格子値 | 較正グリッド定義（Calibration.cs のグリッドと重複意味・統合検討） |
| Successor.SimCore/Validation.cs:77 | `0.5` | 下流の無い財（dead-end）と判定する最大需要の閾値 | Coefficients.DeadEndDemandThreshold |
| Successor.SimCore/Validation.cs:85 | `1.0 / 0.05` | 世界価格の発見判定：初期価格1.0からの乖離が0.05超で「動いた」とカウント | Coefficients.PriceMovedThreshold（1.0は初期参照価格＝定数化） |
| Successor.SimCore/World.cs:522 | `0.1` | PPP計算の物価下限ガード（1/max(0.1,P)）＝為替の暴発防止 | Coefficients.PppPriceFloor |

#### その他（3件）

| 場所 | 値 | 意味 | 移設先案 |
|---|---|---|---|
| Successor.SimCore/Meritocracy.cs:21 | `stackalloc int[5]` | 昇格先候補の最大数（技師・教師・官僚・医者・商人＝5）。固定容量のマジック数 | const int MaxPromotionTargets = 5; |
| Successor.SimCore/SaveGame.cs:75 | `-1` | 直列化フォーマットの『新型マーカー』センチネル。GraphWriterで書き(75行)、GraphReaderのReadTypeRefで id>=0 判定として暗黙に参照(167行)＝writer/readerに散在する未命名プロトコル定数 | private const int NewTypeMarker = -1;（SaveGame内で共有） |
| Successor.SimCore/Territory.cs:106 | `r.ArableLand >= 1e8` | 耕地賦存量の未設定センチネル（制約なし＝合成較正/最小テスト世界） | const ArableLandUnsetSentinel（名前付き番兵） |

## 7. ファイル別件数（多い順）

| ファイル | 件数 |
|---|--:|
| Successor.SimCore/Simulation.cs | 39 |
| Successor.SimCore/SelfPlay.cs | 37 |
| Successor.SimCore/MultiNationSim.cs | 34 |
| Successor.SimCore/Diplomacy.cs | 31 |
| Successor.App/ViewModels/GameViewModel.cs | 27 |
| Successor.SimCore/Fiscal.cs | 17 |
| Successor.SimCore/World.cs | 17 |
| Successor.SimCore/Ideology.cs | 16 |
| Successor.SimCore/Politics.cs | 15 |
| Successor.SimCore/Colonial.cs | 14 |
| Successor.SimCore/CrownParliament.cs | 12 |
| Successor.SimCore/NationConfig.cs | 12 |
| Successor.SimCore/Catalog.cs | 10 |
| Successor.SimCore/Parliament.cs | 10 |
| Successor.SimCore/Factions.cs | 8 |
| Successor.SimCore/InternationalMarket.cs | 8 |
| Successor.SimCore/PolicyPreferences.cs | 8 |
| Successor.SimCore/EconomyStep.cs | 7 |
| Successor.SimCore/Policy.cs | 7 |
| Successor.App/ViewModels/GameViewModel.Production.cs | 5 |
| Successor.SimCore/Climate.cs | 5 |
| Successor.SimCore/TechEducation.cs | 5 |
| Successor.SimCore/Meritocracy.cs | 4 |
| Successor.SimCore/Validation.cs | 4 |
| Successor.SimCore/Agents.cs | 3 |
| Successor.SimCore/Calibration.cs | 3 |
| Successor.SimCore/CapitalMarket.cs | 3 |
| Successor.SimCore/FactionSnapshot.cs | 3 |
| Successor.SimCore/Formations.cs | 3 |
| Successor.SimCore/Government.cs | 3 |
| Successor.SimCore/Territory.cs | 3 |
| Successor.SimCore/Geo.cs | 2 |
| Successor.SimCore/Military.cs | 2 |
| Successor.SimCore/Model.cs | 2 |
| Successor.SimCore/PopDecision.cs | 2 |
| Successor.App/ViewModels/NationSelectViewModel.cs | 1 |
| Successor.SimCore/Credit.cs | 1 |
| Successor.SimCore/HistoricalReference.cs | 1 |
| Successor.SimCore/InterestGroups.cs | 1 |
| Successor.SimCore/SaveGame.cs | 1 |
| Successor.SimCore/Sweep.cs | 1 |
| Successor.SimCore/Trade.cs | 1 |

