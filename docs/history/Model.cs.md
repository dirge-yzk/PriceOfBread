# PriceOfBread.SimCore/Model.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-5 W 直後 `public enum GoodTier { Staple, Daily, Luxury, Intermediate, Producer, Service }`
````
/// <summary>財の階層（§2.3）。予算は優先度（数値の小さい順）に配分する。
/// Service＝POPが算出する役務（労働力・教育・医療・行政）。消費順は**全段の後**＝
/// 「生活必需品も嗜好品も全て満たした後に残る金でのみ買う」＝最上のぜいたく（教育の設計）。</summary>
````

## L8-8 W 直後 `public enum EducationLaw { None, Religious, Public, Compulsory }`
````
/// <summary>教育制度の4段階（§A-1）。法案で進む。性格と天井が段で変わる。</summary>
````

## L11-12 W 直後 `public enum CrownForm { None, Absolutist, Despotic, Shogunate }`
````
/// <summary>王権の登録簿（P2ラベル導出化2026-07-11）：君主制の**形**の永続化。軸（統制力・参政権）だけでは
/// 絶対王政/専制/幕府は署名が同一で区別不能＝形は歴史の産物として登録簿に持つ（貴族共治/官僚軍/封建分権）。廃位（共和・プロ独裁）でNoneへ。</summary>
````

## L15-16 W 直後 `public enum SeizureForm { None, Bourgeois, Proletarian, Military, Anarchy }`
````
/// <summary>奪取の登録簿（P2）：独裁は連続軸から導出できる実体でなく**奪取イベント**＝誰が権力を簒奪したかの記録。
/// ブル独裁・プロ独裁・軍政・無政府はこれが立つ（ボナパルティスト皇帝＝Crown維持×Seizure=Bourgeois等の重ね持ち可）。</summary>
````

## L19-20 W 直後 `public enum Polity`
````
/// <summary>政体（§9-E）。誰が支配するか × 自由/権威 × 君主の有無。利益集団の極端な力で遷移する（<see cref="Politics"/>）。
/// 自由側（立憲君主制・共和制）のみ革新（技術前線の押し上げ）可能、権威側は採用のみ（§2.8）。</summary>
````

## L23 T `Absolutist,`
````
// 絶対君主制：貴族と統治する伝統的君主（権威）
````

## L24 T `Despotic,`
````
// 専制君主制：貴族を抑え官僚軍で個人統治＝上から近代化可（権威）
````

## L25 T `Shogunate,`
````
// 幕府〔日本〕：軍事貴族の封建分権・名目君主（権威）
````

## L26 T `ConstitutionalMonarchy,`
````
// 立憲君主制：君主を残し議会化＝明治型（自由）
````

## L27 T `Republic,`
````
// 共和制：君主を廃した代議制（自由）
````

## L28 T `BourgeoisDictatorship,`
````
// ブルジョア独裁：大衆を抑える権威主義的資本主義（権威）
````

## L29 T `ProletarianDictatorship,`
````
// プロレタリア独裁：労働者の革命独裁（権威）
````

## L30 T `MilitaryDictatorship,`
````
// 軍事独裁：軍人POPが不安定に乗じて権力を掌握（新軍主義・権威）。正統性が侵食されやすく不安定
````

## L31 T `Anarchy,`
````
// 無政府：秩序崩壊（一過性）
````

## L34-36 W 直後 `public enum PowerRank`
````
/// <summary>国家ランク（§改修2026-06-28）。NationalScore の相対閾値で四半期ごとに動的分類（数に限りなし）。
/// 抽象勢力(Nation==null＝被植民地の弱小勢力)は常に Minor。ランク変化でも既存の同盟・戦争・従属は継続（ラベルのみ）。
/// 列強/中堅は「同ランク」相手にのみ緊張上下・防衛同盟を結べ、列強のみ弱小を植民地化できる。</summary>
````

## L39 T `Minor,`
````
// 弱小国：植民地化の標的（列強の餌食）。抽象勢力は常にこれ
````

## L40 T `Middle,`
````
// 中堅国：同ランク同士で緊張上下・同盟
````

## L41 T `Secondary,`
````
// 準列強：列強に次ぐ一団（スペイン・オスマン型）。★2026-08-11に4段階化で新設。
````

## L42-43 W 直後 `Great,      // 列強：列強同士で緊張上下・同盟、弱小を植民地化`
````
//   ★できることは中堅と同じ（植民地化も ColonizeByMiddlePowers の同じ枝を通る）＝**分けたのは同ランクの束**。
//   弱小の集合は不変（列強7＋準列強5＋中堅7＝旧「列強7＋中堅12」と同じ19）＝植民地化の標的は動かない。
````

## L44 T `Great,`
````
// 列強：列強同士で緊張上下・同盟、弱小を植民地化
````

## L47-48 W 直後 `public enum Ideology`
````
/// <summary>イデオロギー（§政体・画一遷移の力ベクトル）。各POPが分布として持ち（階級×条件×時代から創発）、
/// 集計が政体への支持＝引力点を決める。順序は IdeologyShares のインデックス（0..10・9=ファシズム・10=王党派）と一致＝変更不可。</summary>
````

## L51 T `Traditionalism,`
````
// 0 伝統主義：現秩序（君主・教会・身分）の維持
````

## L52 T `Reactionism,`
````
// 1 反動主義：旧秩序への復古（反革命）
````

## L53 T `Liberalism,`
````
// 2 自由主義：立憲・自由市場・制限選挙
````

## L54 T `Republicanism,`
````
// 3 共和主義：君主廃止・代議制・参政権拡大
````

## L55 T `Nationalism,`
````
// 4 ナショナリズム：国威・統一・戦争熱（横断）
````

## L56 T `Socialism,`
````
// 5 社会主義：労働者の権利・再分配（議会内左派）
````

## L57 T `Communism,`
````
// 6 共産主義：革命的労働者独裁（1848以降）
````

## L58 T `Anarchism,`
````
// 7 無政府主義：国家の廃止
````

## L59 T `Imperialism,`
````
// 8 帝国主義：海外膨張・植民地（横断・エリート）
````

## L60 T `Fascism,`
````
// 9 ファシズム（1934拡張E7）：戦間期(1919〜)の危機で結晶する権威主義。反動＋国民動員＋反共。座標が軍事独裁に近接＝軍事独裁へ写像。FascismGate=0で休眠（シェア0＝配列長9と数値等価）
````

## L61 T `Royalism,`
````
// 10 王党派（2026-08-19・作者裁定）：★**政治意識を持つ王権支持者**。伝統(0)/反動(1)は参加0.00＝「政治参加を求めない」層しか表さず、
````

## L62-63 W 直後 `}`
````
//    英保守党・独国家人民党・仏オルレアン派/ボナパルト派・明治の藩閥のように**選挙で戦う王党派**が座標上に存在しなかった（実測：王権シェアが全13断面・全64カ国で14〜18%の頭打ち）。
//    供給＝**王権から実際に利益を得ている層**（閉鎖階級→特権／近代エリート→恩顧）×政治意識。（旧の旗 RoyalistGate は 2026-08-29 に点火・2026-10-18 に焼き込み）
````

## L66-66 W 直後 `public enum RegionState { Active, Locked }`
````
/// <summary>地域の状態（§A-8）。locked＝pop≈0・市場休眠。（旧＝開発〔資金〕か武力で active 化する道があった＝(551) 恒等の線 gc1 で解錠の道を削除。本番の盤面は州を Locked にしない。★(568) gc3：この値を読む行は SimCore・App・Runner から外した〔枠と機械の線 ckf が書き直す発見の走査を除く〕＝型の定義と Region.State の欄だけが残る。消すのは枠と機械の 5 回目の基点〔枝 sm4〕の合流の後〔ScalarDeletionTests の許可表〕。）</summary>
````

## L69-71 W 直後 `public enum SpecialistExitLaw { Free, Restricted, Banned }`
````
/// <summary>★技術者流出禁止法（段⑤・2026-08-17）。軸の増＝禁止を強める。3段＝自由／制限／禁止。
/// ★史実＝英の職工移住禁止法（1719 Artificer's Act）。⚠推したのは**製造業者**であり、1825年に**自由主義**が撤廃した（団結禁止法と同年）。
/// ⚠既定 Free＝禁止なし＝現行と厳密同義。招聘（MultiNationSim.RecruitSpecialists）が送出側の値を読む。</summary>
````

## L74-75 W 直後 `public enum TreasuryCat`
````
/// <summary>国庫の増減の区分（2026-08-13）。財政タブの行と1対1。UIの並びと同じ順に置く。
/// ★増やすときは <see cref="World.Pay"/> の呼び出し側も必ず名乗り直すこと（コンパイラが強制する）。</summary>
````

## L78 T `Tax, Tariff, StateMonopoly, Borrow, Devalue, StateFactory, TributeIn, OtherIn,`
````
// 収入側
````

## L79 T `Army, Navy, Admin, Education, Welfare, Pension, Modernization, AntiCorruption,`
````
// 支出側
````

## L84-87 W 直後 `public enum RegionTier { Core, Integrated, Colonial }`
````
/// <summary>州の**格**（植民地のラベル化・2026-08-01。docs/202607/20260731_植民地のラベル化_部分完了.md §3-1）。
/// RegionState が「州の生死」なのに対し、こちらは「州の身分」＝Colony 型を州属性へ畳んだもの。
/// Core＝中核（1633年から保有＝150年の線）／Integrated＝編入済み（中核民族が過半へ達した植民地）／Colonial＝植民地
/// （参政権・技術伝播・維持費・割譲到達・派閥が Colonial を読む。Core/Integrated は同じ扱い）。</summary>
````

## L90-90 W 直後 `public enum SubsistenceBasisKind : byte { OpeningShare, WarmupShare, Productivity }`
````
/// <summary>★(849) 自給の基（<see cref="Pop.SubsistenceBasis"/>）の意味：OpeningShare＝開幕の食べる割合のデータ（焼いていない）・WarmupShare＝割合のまま助走を通った（助走の後の最初の月に焼く）・Productivity＝焼いた自給の生産性 ρ。</summary>
````

## L93-96 W 直後 `public enum Terrain { Plain, WaterMajor, WaterMinor, Hill, Forest, Mountain, Barren }`
````
/// <summary>★地形タグ（(3)・設計doc 20260826_地形と輸送コスト.DONE.md＝タグ7種×手段3列）。既定 Plain。
/// ⚠数値は州に置かない＝州はタグ（事実）だけを持ち、係数は Coefficients の表に集約（思想②）。
/// 森林＝**温帯林**（馬を強く阻むが鉄道はほぼ妨げない）／不毛＝砂漠・熱帯雨林・凍土（人がほぼ住めない）／
/// 山岳＝逆に**鉄道のほうが厳しい**（トンネル＝国家事業）＝係数が交差する。値の出所は places.json の terrain 列。</summary>
````

## L99-100 W 直後 `public enum Basin { Atlantic, Mediterranean, BlackSea, IndianOcean, Pacific, Baltic, RedSe`
````
/// <summary>海盆（§B 海運）。国の交易が大西洋の交易重心へ出るのに通る海域＝座標から導出（黒海は内陸性のため要override）。
/// チョークポイント（海峡・運河）はこの海盆を結ぶ＝どの国がどの咽喉を使うかを「国名」でなく地理（海盆）から決める。</summary>
````

## L101 T `public enum Basin { Atlantic, Mediterranean, BlackSea, IndianOcean, Pacific, Baltic, RedSe`
````
// ★Caspian末尾追加（D1・2026-08-14）＝バクーが1,104km先の黒海ノードへ吸着していた誤りの是正。閉鎖海＝他海盆と繋がらない（露の石油は鉄道でバトゥミへ＝史実どおり陸送で出る）   // 03手順5（2026-07-29）：バルト・紅海を末尾追加（A2裁定。海盆は海ノードのデータ＝sea_nodes.json の basin が真実源）
````

## L102 T `public static class Basins { public const int Count = 8; }`
````
// Caspian追加（セーブ互換は不問＝作者裁定2026-08-14）   // 03手順6：海盆別配列の固定長（enum末尾追加時はここも更新）
````

## L104-104 W 直後 `public sealed class Good`
````
/// <summary>財（§11.2）。需要パラメータ（必要量・参照価格・弾力性）を各財が持つ。</summary>
````

## L111 T `public double BaseNeed;`
````
// 1人あたり必要量（§9-B）
````

## L112 T `public double RefPrice;`
````
// 需要曲線の参照価格
````

## L113 T `public double Elasticity;`
````
// 価格弾力（必需＝小・日用/奢侈＝大）
````

## L114 T `public bool IsColonial;`
````
// 気候ゲート財（Catalog.IsColonial と同値）＝賦存州（Resources に列挙）のみ産する。非賦存州は BuildProvince が生産者を除去
````

## L115 T `public double WearRatePerMonth;`
````
// 在庫の月の損耗の率（財の表 GoodDef.WearRatePerMonth の写し＝保管期間に応じた減衰。穀物/生鮮＝高・金属/燃料＝低。だぶつきの恒久滞留を防ぐ）。率そのものを使う式の読み口＝耐久度は下の Durability
````

## L116 T `public double Durability => 1.0 - WearRatePerMonth;`
````
// ★(346) 耐久度 d（1か月あたり残る割合）＝読み口だけ（保存しない・Catalog.DurabilityOf と同じ式）
````

## L117 T `public bool NonTradeable;`
````
// 輸送不可（労働力・教育・行政・医療＝役務）。地元市場のみ＝貿易・世界価格の対象外
````

## L118 T `public bool GrainChain;`
````
// A-高3：穀物法の課税客体（麦・小麦粉）。史実の客体は穀粒と挽き割りで、焼成パンの国際貿易は存在しない（旗 CornLawGrainChainGate は 2026-09-26 に焼き込み）
````

## L119 T `public double Nutrition;`
````
// ★塊1（2026-08-31）：非主食の食べ物度（加工肉/干物=1.0）。主食はTierで常に食料＝この値は読まない。FoodIncludesAnimalProtein=0 で不読＝恒等
````

## L122-125 W 直後 `public sealed class Pop`
````
/// <summary>
/// pop ＝ ある地域の人口集団（§2.1）。永続状態（規模・貯蓄・充足度）と、
/// その月だけ使う作業フィールドを持つ。IsUrban は都市化（M2）の集計に使う。
/// </summary>
````

## L132-134 W 直後 `public MoneyStock Savings { get => _savings; init => _savings = value; }`
````
/// <summary>貯蓄（お金の口座）。★2026-09-30 段4g（作者裁定 Q13＝コンパイラの番人）：**書けるのは Pop の中だけ**＝按分の本体（<see cref="Distribute"/>）・
/// 移し方の払い手の側（<see cref="Take"/>）・人と動くお金の運搬（<see cref="Carry"/>）・国の換算（<see cref="Rebase"/>）・月末の精算（<see cref="SettleMonth"/>）。
/// 外の書き手は移し方の1本（<see cref="World.Transfer"/>＝相手を名乗る）を通す＝片側だけの記帳は型で書けない。初期化子（<c>new Pop { Savings = … }</c>＝構築）は <c>init</c>。</summary>
````

## L137-138 W 直後 `internal void ApplyBakedSavings(double value) => _savings = new MoneyStock(value);`
````
/// <summary>★(490) 貯蓄の書き手の 7 本目：開幕の焼き込みの読み込み（<see cref="OpeningBake.Apply"/>）だけが呼ぶ。旗 <c>OpeningBakeEnabled</c> が 0 では呼ばれない。貯蓄が変わるので基礎貨幣の基準線は呼び手が取り直す。
/// 計器の貯蓄の台帳（<c>Instruments.Sav</c>）には載らない＝計器つき×旗 1 の組は flow_s の貯蓄の台帳が焼き込みの書き込みを知らない（旗は倒さない・採るときに SavKind を足す）。</summary>
````

## L140-145 W 直後 `public MoneyStock Debt;`
````
/// <summary>個人債務（設計＝docs/202608/20260802_POPの個人債務_部分完了.md・2026-08-02）。⚠**貨幣ではない**——
/// <see cref="Bank.HouseholdLoans"/>（そちらが貨幣）の内訳＝「誰の分か」を持つ記録である。
/// ⚠★<c>World.OutstandingLoans</c> に足してはならない（両方足すと貨幣が二重に消える）。
/// ★入口は2つ（損失の記憶＝backstop／消費者信用＝主食が買えないときの借入）、出口は1つ（返済規律）。
/// ★既定0＝誰も読み書きしない厳密恒等（NationalConsciousness のPOP属性化と同型）。
/// ⚠**POPを按分する6箇所では無条件に運ぶこと**（主フラグでゲートしない＝0×比=0.0 で自動的に恒等）。</summary>
````

## L147-153 W 直後 `public MoneyStock BondHoldings;`
````
/// <summary>★国債の保有台帳（2026-09-21裁定＝P10の唯一の例外・作者が明示許可）＝このPOPが持つ**自国の国内債**の額面。
/// ⚠**貨幣ではない**——金は Fiscal の起債・利払い・償還の経路どおりに動き、これは「誰がどれだけ持つか」の記録だけ（<see cref="Debt"/> と同型）。
/// ★入口＝起債の購入（払った額だけ増える）。出口＝償還（保有比例で減る）／デフォルト・革命的否認（0へ＝損は持ち手）／
///   発行国の外へ出る（割譲・分離建国・越境＝発行国の対外債へ振替＝<see cref="Fiscal.BookForeignDebt"/>）。
/// ★利払いと償還の受け取りは**この保有に比例**（旧＝貯蓄比で買い・頭数で償還＝往復ごとに富裕な資本家から貧しい資本家へ移っていた）。
/// ★不変条件＝国の全POPの和＝国内元本残高 GovDebt−ForeignDebt（<see cref="BondLedger"/>・相対1e-8）。
/// ⚠**POPを按分する全経路で Savings と同じ規則で運ぶこと**（主フラグでゲートしない＝0×比=0.0 で自動的に恒等）。</summary>
````

## L156 T `public double Literacy;`
````
// 識字（連続・0..1・§A-1／§C）。教育で伸び、革新能力に効く
````

## L157 T `public double Consciousness;`
````
// 政治意識（0..1・§9-E）。識字×世俗の水路×都市化で伸びる
````

## L158 T `public double Militancy;`
````
// 闘争度（0..1・§9-E）。不満・参政権欠如・外圧で伸びる
````

## L159 T `public MoneyPerMonth LastEarnedPerMonth;`
````
// ★前期の所得（2026-08-04）。Earned は EconomyStep 冒頭でゼロ化され期中に積み上がるので、
````

## L160-160 W 直後 `public double[] IdeologyShares = new double[Ideologies.Count];   // イデオロギー分布（伝統/反動/自由/共和/ナ`
````
//   期の初めに読む用途（私的投資の原資）には使えない。ゼロ化の直前にここへ退避する。
````

## L161 T `public double[] IdeologyShares = new double[Ideologies.Count];`
````
// イデオロギー分布（伝統/反動/自由/共和/ナショ/社会/共産/無政府/帝国/ファシズム/王党派・合計1）。階級×条件×時代から創発（Ideology.cs）＝政体遷移の力ベクトル
````

## L162 T `public bool Closed;`
````
// 閉鎖階級（世襲の特権身分＝貴族・聖職）。移民の出入りをしない（史実の身分制）
````

## L163-165 W 直後 `public double SubsistenceBasis;`
````
/// <summary>★(849) 自給の基（旧 Subsistence）。焼く前（<see cref="SubsistenceBasisKind"/> が OpeningShare か WarmupShare）＝開幕の食べる割合のデータ（0..1・world.json の peasantSubsistence）。
/// 焼いた後（Productivity・旗 <see cref="Coefficients.SubsistenceStage2"/> が正の世界で助走の後の最初の月に 1 回）＝自給の生産性 ρ（売れ残りの農業労働の能力 1 単位あたりの主食・技術の倍率で割った値）。
/// 読み手は割合をこの欄から直に読まない＝その月の割合は <see cref="SubsistenceShare"/>（同名のまま意味を替えないための改名＝型の統合が起こす静かな破損を避ける）。</summary>
````

## L167-167 W 直後 `public SubsistenceBasisKind SubsistenceBasisKind;`
````
/// <summary>★(849) <see cref="SubsistenceBasis"/> の意味の印（思想⑩の例外＝開幕の売れ残りの能力は暦が動くと導けない・ρ と同じ理由）。旗 0 は書かない（OpeningShare のまま）。</summary>
````

## L169-169 W 直後 `[System.NonSerialized] public double SubsistenceShare;`
````
/// <summary>★(849) この月に使う食べる割合（0..1）。EconomyStep.Run の頭で全ての POP に書く（旗 0 か焼く前＝<see cref="SubsistenceBasis"/> そのもの）。導ける量＝セーブに載せない。</summary>
````

## L171 T `public bool IsModernElite;`
````
// 近代的利益団体（専門職・ブルジョワ）。その勢力割合がメリトクラシー値を決める
````

## L172 T `public bool IsSoldier;`
````
// 兵士POP（軍隊規模の素）。軍事費で徴募/離職、戦争で戦死、徴兵で大衆から変換。自然出生死は対象外（規模は軍事力学が管理）
````

## L173-175 W 直後 `public bool IsCommoner => !Closed && !IsSoldier;`
````
/// <summary>★平民の民間人の述語（2026-09-30 処理の一元化 段4e H）＝閉鎖階級（貴族・聖職）でも兵士でもない（近代エリートを含む）。
/// 旧＝同じ述語 `!Closed &amp;&amp; !IsSoldier` を人頭税・失業・州の闘争度と群衆・鎮圧の死・緩和・昇格の受け手が10か所で手書きした。
/// 大衆（<see cref="Politics.IsMassPop"/>）はこれから近代エリートを除いたもの。福祉の受給者（Fiscal＝閉鎖階級と近代エリートを除く＝兵士を含む）は別の集合（作者裁定 2026-09-28 §14-2 の3）。</summary>
````

## L177-178 W 直後 `public bool IsCriminal => ClassId == PopClass.Criminal;`
````
/// <summary>★犯罪者の述語（(366) 治安と所有の安全 段0 の器）＝階級の番号だけで判る（新しい旗を持たない＝思想⑩）。CriminalClassEnabled=0 の世界には該当する POP が無い＝どの読み手でも偽。
/// 税・州の間の移民・識字の伸びから外す分岐が読む（IsSoldier が 84 箇所に読まれる規模の点検を避けるため、旗でなく述語）。転職の行き先は州の添字 <see cref="Region.CriminalPopIndex"/> で外す。</summary>
````

## L180-188 W 直後 `public Share MilitiaShare;`
````
/// <summary>★民兵率（段5＝民兵と寝返り・作者裁定3・2026-09-08）＝このPOPの**何割が武器を取ったか**（0..1）。
/// ⚠<see cref="IsSoldier"/> とは**別概念**：兵士POPは常備軍そのもの（軍事費で徴募され、職業として軍にいる＝戦力の素・戦死の母数）。
///   民兵は**職業を持ったまま武器を取った民間人**＝畑と工場から労働が抜け（EconomyStep の労働供給が ×(1−これ)）、
///   戦場では弱兵として数えられ（Power.Military へ MilitiaCombatValue 倍で入る）、戦場の民間人死の配分の重みになり、
///   戦争が終われば札が剥がれて元の暮らしへ戻る（Politics.Step 冒頭＝**タイマーではなく「内戦中でない」という状態**で剥がす）。
/// ⚠人ではなく**札**（率）なのでPOPの分割・併合では運ばない（識字・闘争度と同じ規約）＝人口・貨幣の保存には一切触れない。
/// ⚠階級の禁止規則は**どこにも書かない**（体制側の郷勇＝LoyalistMilitia は有産ブロックだけで組まれ、
///   反乱側のヴァンデ1793・カルリスタ1833は貴族が率いた）＝読むのは「その運動のブロックか」と「どれだけ怒っているか」だけ。
/// ⚠RevoltMilitiaRaise=0（既定）では誰も0以外を書かない＝全経路で ×1.0／+0.0＝厳密恒等。</summary>
````

## L190 T `public double EducationFill = 1.0;`
````
// 教育（財）の充足率＝今月買えた量÷必要量。識字の到達天井（消費が識字を生む）。教育財の無い小シナリオは1.0＝従来挙動
````

## L191-194 W 直後 `public double LastEducationFill = 1.0;`
````
// ★段②（2026-08-17）：前月の EducationFill のスナップショット。EconomyStep の月初（per-pop リセット）で1回だけ写す。
//   ⚠**必要な理由＝順序依存の遮断**：EducationFill は財ループ内（gi==94 の処理時）に 0 へリセットされてから埋め直される。
//   高等教育(136)の需要をそこから作ると、同 Tier 内の処理順が**価格順**なので「94 が先か 136 が先か」が価格で入れ替わり、
//   同じ状態から同じ月の値／前月の値が気まぐれに切り替わる。月初に写せば常に「前月の就学」＝小学校を出てから大学、で一貫する。
````

## L196-206 W 直後 `public double LivingRef = -1.0;`
````
/// <summary>★生活水準の参照点（2026-08-19・作者裁定「生活苦は水準ではなく【低下】から生まれる」）。
/// <code>生活苦 ＝ 参照点 − 今の生活水準</code>＝★上がれば負（体制への支持）・⚠下がれば正（不満）。
/// ⚠これで「ずっと貧しい国はずっと不満」が消える（★徳川日本・清が構造的に安定する＝「徳川パス温存」の個別対処が要らなくなる）。
/// ★史実：トクヴィル「最も繁栄していた地方が最も革命的だった」／Jカーブ（長期の改善の後の急落が革命を生む）。
/// ⚠天命との二重計上にはならない＝**天命は水準・こちらは変化**（数学的に直交）。加えて3層分割で人口が厳密に割られている。
/// ⚠−1＝未捕捉（初回に現在値で初期化＝立ち上がりの跳躍なし。SchoolingConvStock / UnemploymentEma と同じ作法）。
/// （旧の旗 LivingRefWeight は (739) で焼き込み＝常に読み書きされる。）</summary>
// ★作者命名（2026-08-21）＝この機構の名前は**「失望」**（生活苦＝参照点からの落差）。「機構として正しいので存在するべき。
//   リアリティのために必要であれば、純粋な生活水準低下だけにとどまらず、別要素を加算してもよい。
//   ただハーネスが露骨に壊れて調整困難なら休眠でよい」（作者裁定）。
// ★★実測で壊れず＝**2026-08-21に点火（既定1.0＝live）**。硬性ゲート全緑・人口+7.1（日本90%）・経済−10.0は記録して進む。
````

## L208-217 W 直後 `public double FoodRef = -1.0;`
````
/// <summary>★食料の平常＝この pop が慣れている食料の充足 FoodMet/FoodNeed（2026-09-22 回B「飢餓の駆動は急な落ち込みを測る」）。
/// <code>飢餓の駆動 ＝ clamp(利得 ×(平常 − 今の充足 − 起点), 0, 1)</code>（<see cref="Politics.FamineDrive"/> の1本）。
/// ★モラル・エコノミー：食料暴動・飢饉の蜂起は慣れた水準からの**急な**価格高騰・供給途絶が引き金で、慢性の貧困ではない
///   （慢性的に栄養不足の社会は恒常的な内戦にはなかった）。慢性の不足は物理の経路（死亡・疫病・移民・扶養力）が絶対水準で運ぶ。
/// ⚠**新しい状態が要る理由**（思想⑩）：「慣れ」は履歴そのもので現在量からは導けず、既存の平滑量はどれも pop 単位の食料の慣れにならない＝
///   <see cref="LivingRef"/> は満足度の慣れ（階層3分割で食料は満足度の台帳から抜けた）／
///   <see cref="Region.FoodFill"/> は州の平滑（時定数≒5か月＝穀倉の備蓄・飢饉死の読み手）で、州内の階級差（貧しい pop の恒常的な低さ）を
///   慢性の信号として残し、しかも1四半期で落差の約半分を吸う／World.FamineMemory は国の不足の積分で水準でない。
/// 慣れの速度は <see cref="LivingRef"/> と同じ <see cref="Coefficients.LivingRefSpeed"/>（新しい数を作らない）。
/// ⚠−1＝未捕捉（初回の Politics.Step で今の充足に初期化＝開始時の慢性の低さは「平常」として入る＝立ち上がりの跳躍なし）。</summary>
````

## L219 T `public double HigherEduFill;`
````
// 高等教育（財136）の充足率＝自費で買えた量÷欲しかった量。技師・研究者への昇格資格（段②）。既定0＝買っていない（NewsFill と同じ規約）
````

## L221 T `public double SchoolingConvStock = -1.0;`
````
// 政15：識字→政治意識の変換係数の就学履歴ストック。-1=未捕捉（初回Politics.Stepで今期の変換係数で初期化＝立ち上がりの跳躍なし）。**ConsciousnessFromSchoolingStock=1.0点火中**＝毎四半期読み書きされ変換係数は完全にストック（半減期約9年の世代遅れ）＝法改正の即効性ゼロ（02断線走査2026-07-28：陳腐化した「既定0」注記を是正。g=1.0は**恒久確定＝クローズ**＝07_再設計§12 B1裁定2026-07-29「正しい（教育は世代遅れのみ）」。旧「裁定待ち」注記は陳腐化のため是正2026-08-16）
````

## L222 T `public double HealthFill;`
````
// 医療（財）の充足率。人口扶養力に小さく効く（医者がいないと乳児が死ぬ）。既定0＝効果なし
````

## L223 T `public double NewsFill;`
````
// 新聞（財）の充足率（1934拡張E3）。国民意識の実効値＋政治意識のpress項の入力＝「新聞を買えて読めたか」。新聞財の無い小シナリオは0
````

## L224 T `public double RadioFill;`
````
// ラジオ（財）の充足率（E3・1920s〜）。国民意識の識字不要チャネル（検閲非依存＝国営宣伝）。財の解禁前は0
````

## L225 T `public Share UnemploymentRate;`
````
// 労働（労働力/農業/高度）の売れ残り率＝失業。職の喪失は所得の算術を超えて怒りを生む（社会保障が和らげる対象）。労働市場の無い世界は0＝従来挙動
````

## L226 T `public Share UnemploymentEma = new Share(-1.0);`
````
// rank10b（JobSearchUsesEma=0.02点火2026-08-16）：失業率の多年EMA＝慢性失業の記憶。-1=未捕捉（初回更新で現在値から開始＝跳躍なし）。JobSearchUsesEma=0（対照腕）では読み書きされず恒等。失業プール①''の駆動が瞬間UでなくこれをEMA読み＝循環失業と構造失業の分岐
````

## L227 T `public double InitialWork = -1.0;`
````
// 労働供給の初期値（t=0 の work＝成人男女参加＋児童労働）。倍率排除②：旧 work/0.64 の正規化定数を「各popの初期労働供給」へ一般化（標準構成なら0.64＝中立）。-1=未捕捉（初回 EconomyStep で確定）
````

## L228 T `public Share InitialFertileShare = new Share(-1.0);`
````
// ①Phase3：出産可能女性プールの初期値（t=0 の出産可能プール＝Simulation.FertilePool・年齢の層から読む）。出生を女性プール比例化する際の正規化定数（自分の出発点に対する相対変化＝標準構成で Size 比例と恒等）。-1=未捕捉
````

## L229-235 W 直後 `public double[] Ages = StdAges();`
````
/// <summary>★年齢の層（2026-09-30 段4e 後半・設計 docs/202609/20260929/20260929_年齢の層_人口ピラミッドと世代の流れ_設計_未着手.md・作者裁定 2026-09-29＝C 案）＝
/// 層（<see cref="Layout"/>＝既定は作者の11層〔0／1〜5／6〜10／…／61〜70／71以上〕・POB_AGE_BANDS=8 で旧の10歳刻み8層〔0〜9・10〜19・…・60〜69・70以上〕）×男女の**割合**（合計1・8層で16本・11層で22本・添字＝層×2＋性〔0＝男・1＝女〕）。人数は Size×割合＝Size が主
/// （年齢を指定しない死と移動は何もしなくても全層に比例）。読み口は <see cref="AgeShare"/> の1本。
/// 書き手＝加齢と出生と平常の死（<see cref="StepAges"/>＝Simulation.DemographicStep・兵士も年を取る）・年齢を選ぶ死（<see cref="World.KillPops"/> の年齢の重み）・
/// 人の移し方の1本（<see cref="Blend"/>＝移る組の構成）・初期の構成（標準のピラミッド・兵士・職業の性比）。
/// ⚠新しい状態（思想⑩の例外＝設計書 §8）：年齢の構成は0〜70年前の出生・戦死・飢饉・移民の履歴で、今の盤面から導けない。
/// ⚠固定長の構造体にしない（SaveGame の値型の switch に無い型は Struct 経路で無限に潜る）＝double[]。</summary>
````

## L237 T `public Share ChildLaborRate;`
````
// この pop の子供の労働時間割合（0..1・倍率排除④）。家計困窮×合法度×(1−就学)から創発。労働供給（半人前）と識字（働く子は学校へ行けない）の両方を律する。LaborSupply が算出
````

## L238 T `public Share CoreEthnicShare = new Share(1.0);`
````
// 中核（titular）民族の割合（0..1・倍率排除⑧）。国一律スカラー EthnicUnity を POP属性化＝民族統一度は各popの加重平均から創発。移民は core=0 で混ざり希釈、同化で回復、内部移動で運搬、征服で低下
````

## L239 T `public double NationalConsciousness = -1;`
````
// ★国民意識のpop属性（国スカラー World.NationalConsciousness のpop化・2026-08-01）。
````

## L240-241 W 直後 `public double MilVoice;    // 政治的疎外（意識×発言権の無さ×理想と体制の乖離）`
````
// 教育と同じ漸近形で育て、植民地・遠隔・非コアのpopは上がりにくい（倍率でなく構造から）。
// NationConsciousnessPerPop=1.0（既定）でlive・0（対照腕）では誰も読み書きしない＝厳密恒等。-1＝未捕捉（国の値は配らない＝配ると平均が変わる）
````

## L243-243 W 直後 `public double MilVoice;    // 政治的疎外（意識×発言権の無さ×理想と体制の乖離）`
````
// --- 不満（闘争度）の要因内訳（診断・UI表示用。Politics.Step が毎期記録。計算には不使用＝較正中立）。各値は target への寄与ぶん ---
````

## L244 T `public double MilVoice;`
````
// 政治的疎外（意識×発言権の無さ×理想と体制の乖離）
````

## L245 T `public double MilWar;`
````
// 厭戦（戦争の長期化）
````

## L246 T `public double MilNation;`
````
// 民族的地位（★段2で一本鎖＝少数派 (1−中核度) のみ）
````

## L247 T `public double MilFamine;`
````
// 飢餓の駆動項の寄与（診断・UI表示用＝力学は読まない）。★2026-09-22 点火（回Bで平常からの落ち込みへ＝FamineDriveGain=30・FamineDropOnset=0.07・階層3分割と対）
````

## L248-249 W 直後 `public double RemainingBudget;   // その月の残予算（必需→日用の優先配分）`
````
// ★段2（2026-09-09）で MilJobs/MilCrowd/MilGap/MilMid/MilWave を削除＝闘争度の項ごと消えた（11項→5項・設計メモ §7-3）。★(551) MilLiving も削除（(431) の二量分離の後は恒久 0＝生活の項は不満度だけが持つ・4項）。
// MilForeign（外圧→闘争度の直加算の診断）は機構撤去（C裁定2026-07-09＝ReformPressureBoost経路へ一本化）後の恒久0だったため削除（監査2026-07-12 死24）
````

## L251-251 W 直後 `public double RemainingBudget;   // その月の残予算（必需→日用の優先配分）`
````
// --- その月だけの作業用（§9-B/§9-C のループ内）---
````

## L252 T `public double RemainingBudget;`
````
// その月の残予算（必需→日用の優先配分）
````

## L253 T `public double GoodDemand;`
````
// 現在処理中の財の希望量
````

## L254 T `public double GoodNeed;`
````
// 現在処理中の財の必要量
````

## L255-257 W 直後 `public double NeedMet;            // 満たせた必要量の合計（日用品＋買えた嗜好品）`
````
// ⚠この2本は**満足度の台帳**（月次アキュムレータ）＝「全財」ではない（作者裁定2026-09-20・2026-09-22 既定化）：
//   食料（主食＋FoodIncludesAnimalProtein>0 のとき動物性タンパク）は載らず、日用品は必要量が分母に、嗜好品は「買えた分」だけが分子にも分母にも載る。
//   飢えの台帳は下の FoodMet/FoodNeed の側。
````

## L258 T `public double NeedMet;`
````
// 満たせた必要量の合計（日用品＋買えた嗜好品）
````

## L259 T `public double NeedTotal;`
````
// 必要量の合計（日用品の必要量＋買えた嗜好品）
````

## L260 T `public double FoodMet;`
````
// 満たせた**主食**の量（自給＋市場購入）。FoodFill＝扶養力のマルサス項に使う
````

## L261 T `public double FoodNeed;`
````
// 必要な**主食**の量（主食のみ＝飢饉判定の分母）
````

## L262-268 W 直後 `public double SupplyFill`
````
/// <summary>★補給の充足∈[0,1]＝食料の台帳＋満足度の台帳＝(FoodMet+NeedMet)/(FoodNeed+NeedTotal)（導出量＝状態を持たない）。
/// 階層3分割（作者裁定2026-09-20・2026-09-22 既定化）の後も「食えて、買えているか」の全体＝分割前の満足度と同じ量
/// （主食の重み1.0は旧台帳の重みそのもの＝新しい数はゼロ）。政治の読み手（不満・闘争度）は読まない＝物理・経済の行動・評価の読み手だけ：
/// 兵卒の実質（PowerShares.SoldierWelfare→即応度）・
/// 家計の困窮（LaborSupply＝女性の就労と児童労働）・阿片の困窮ゲート（EconomyStep）。
/// ⚠月次の累積器から導く＝EconomyStep.Run の月頭のゼロ化の後（同じ月の消費の途中）に読んではならない（Run の中の読み手は月頭に撮った前月の値＝Scratch.LastSupply）。
/// 台帳が空（決算前）は 1.0＝満足度の初期値と同じ中立。</summary>
````

## L276 T `return PoliticalQuantities.Clamp01(v);`
````
// NaN は 0 側（旧 SoldierWelfare の NaN の倒し先と同じ）・(739) 群2：−0.0 も +0.0
````

## L279-282 W 直後 `public double FoodFill => FoodNeed > 1e-9 ? PoliticalQuantities.Clamp01(FoodMet / FoodNeed`
````
/// <summary>★主食の充足∈[0,1]＝FoodMet÷FoodNeed（導出量・2026-09-30 処理の一元化 段4e H）。必要が無い（FoodNeed≈0）は 1＝飢えていない。
/// ⚠否定形のクランプ（<see cref="PoliticalQuantities.Clamp01"/>）＝NaN は 0 の側。旧＝同じ式を人口動態の飢えの上乗せ・疫病の脆弱さの重み（2か所）が手書きした
/// （上の月次の台帳から導く＝月の途中で読まない）。⚠飢餓の駆動と食料の平常の捕捉（Politics）は生の比のまま＝負の貯蓄の POP は食料の需要が負になり
/// （残予算÷価格が負）比が負になる＝下限0で切ると軌道が変わる（2026-09-30 段4e の申し送り）。</summary>
````

## L284 T `public MoneyPerMonth PaidPerMonth;`
````
// その月に支払った総額
````

## L285 T `public MoneyPerMonth EarnedPerMonth;`
````
// その月に得た総所得
````

## L286 T `public MoneyPerMonth LandRentPerMonth;`
````
// その月に受け取った土地の地代（RGOの所有者取り分＝封建貢租）。土地税の課税ベース
````

## L287 T `public MoneyPerMonth LandTaxBasisAdjPerMonth;`
````
// ★(937) 土地税の基数−地代の差分（その月・流れ）＝凍結した RGO（Producer.FrozenLandRentPerMonth）の〈凍結×持ち分−今の地代の正の部分〉の和。月の頭に 0（LandRentPerMonth と同じ）。旗 LandTaxFixedBasis＝0 では常に 0
````

## L288 T `public MoneyPerMonth IndustrialRentPerMonth;`
````
// その月に受け取った工業レント（非RGO・投資家所有の利潤。設計3段1＝書き込みタグ。★(801) 旗 CapitalDividendFromHostRent が正のとき CapitalMarket.HostRentDividend が読む＝外資の利札の入力。ほかの読み手は計器と Discovery.IdeaBoardOf〔旗 SpreadNeedFascismDecline の盤面・計器 idn_s〕）
````

## L289-292 W 直後 `public MoneyPerMonth SubsistenceValuePerMonth;`
````
/// <summary>★(706) 線 urb3：その月の自給の値＝Σ主食 [自給で満たした量 sub（<c>EconomyStep.Run</c> の自給充足）× その州のその主食の今の市場価格 <c>Market.Price</c>]（EarnedPerMonth と同じ〈その月の流れ〉・月の頭に 0 へ戻す）。
/// 州の中の移動の〈移る値打ち〉（<see cref="PopDecision.MigrationValues"/>）の農村の側だけが読む（旗 <c>UrbanFlowReal</c> が 0 の間は計器だけが読む＝世界は動かない）。
/// ⚠<c>Region.LastSubsistenceOutput</c> では足りない理由＝州ぜんたいの量で値段が掛かっておらず、POP ごとにも分かれない（対の農村の POP だけの値が要る）。
/// ⚠セーブに載る理由＝月の途中の状態（EarnedPerMonth と同じ）＝月の途中で保存して読み戻しても移る判断が同じになる（設計思想⑩の〈本当に必要な場合〉）。値段は買い手の楔を含まない基礎の価格（<c>revenue</c> に積む値と同じ）。</summary>
````

## L295-299 W 直後 `internal static void Carry(Pop from, Pop to, MoneyStock savings, MoneyStock debt, MoneySto`
````
/// <summary>★人と動くお金の運搬の1本（2026-09-30 段4f・作者裁定 Q13＝書き手を Pop の中へ）：貯蓄・個人債務・国債の保有を、人と一緒に <paramref name="from"/> から
/// <paramref name="to"/> へ同じ額で移す（両側を1回の呼び出しで書く＝湧かず消えない）。運ぶ額は呼び手が決める＝ストック×(運ぶ人数÷規模)（E3）・越境は運賃を引いた持参金と
/// 国債の保有 0（保有は発行国の対外債へ振り替える＝<see cref="Fiscal.BookForeignDebt"/>）。旧＝州の間の移民と入植（Territory.MoveBetweenRegions）・社会移動（Meritocracy）・
/// 徴兵と復員（Military.MoveCohort）・州の中の移動（PopDecision）・越境移民と招聘（MultiNationSim）の6か所が同じ2行を写していた（式と順は同じ＝移しただけ）。
/// ★2026-09-30 段4e I：人数と属性の混ぜ方は <see cref="Blend"/> の1本（これを呼ぶ）。</summary>
````

## L302-304 W 直後 `internal static void Carry(Pop from, Pop to, MoneyStock savings, MoneyStock debt, MoneySto`
````
/// <summary>★国境をまたぐ運搬（2026-09-30 段4e＝人と動くお金の換算・(120) の残り）：送り手からは自国の通貨の額（<paramref name="savings"/>・<paramref name="debt"/>）、
/// 受け手へは受け手の通貨へ換算した額（<paramref name="savingsIn"/>・<paramref name="debtIn"/>＝呼び手が <see cref="World.CrossFx"/> で換算して差を換算の台帳へ）。
/// 国内（同じ通貨）は同じ額（上の5引数）。国債の保有は国内だけ運ぶ（越境は発行国の対外債へ振り替えて 0）。</summary>
````

## L310 T `if (Instruments.On) { Instruments.Sav(from, Instruments.SavKind.CarryOut, -savings.Value);`
````
// (262) 計器（読むだけ）：貯蓄の書き手の台帳
````

## L313-319 W 直後 `internal static double Distribute(List<Pop> pops, double amount, List<double>? weights = n`
````
/// <summary>★按分の1本（支払いの一元化 段1-2・2026-09-28＝旧 Fiscal.Distribute の本体・2026-09-30 段4g に World から Pop へ移した＝貯蓄の書き手を Pop の中へ）。
/// 額×(重み÷合計) を先頭から順に足し、端数は最後の受け手へ＝合計が厳密に <paramref name="amount"/>（複合代入の罠を避ける2文）。
/// 重み＝<paramref name="weights"/>（pops と同じ並び・呼び手が今の時点の値で詰める）／null なら頭数（Size）。
/// 受け手不在（空リスト・重みの合計≈0）なら配れなかった額を返す（移し方の1本は受け手不在を先に確かめる＝動かさない）。
/// ⚠否定形のガード＝NaN の額・合計は配らない。**額は符号つき**（負の額＝重みの比で取る）・**微小な額も配る**。額が0か NaN なら何もせず 0 を返す。
/// <paramref name="income"/>＝受け取りを所得（Earned）にも同じ額で書く＝月次の精算の後の受け取りが所得・信用枠・票・課税の基数に載る。
/// 呼び手＝移し方の1本（<see cref="World.Transfer"/>）だけ（2026-09-30 段4g＝外から呼ぶ7行を移し方の1本へ畳んだ）。</summary>
````

## L334 T `if (Instruments.On) Instruments.Sav(pops[i], income ? Instruments.SavKind.GiveIncome : Ins`
````
// (262) 計器（読むだけ）
````

## L340-346 W 直後 `internal static double Take(List<Pop> payers, double amount, List<double>? weights = null,`
````
/// <summary>★移し方の1本の払い手の側（2026-09-30 段4g＝World.Transfer から Pop へ移した＝貯蓄の書き手を Pop の中へ）。戻り値＝動いた額（引いた額の和）。
/// <paramref name="weights"/>＝null なら**貯蓄の保有の比**（d5）：取る額＝min(額, Σmax(0,貯蓄))・各自＝取る額×max(0,貯蓄)÷Σ（旧の式のまま）。
/// 重みがあれば**払い手ごとの自分の請求**：各自の請求＝重み×(額÷Σ重み)・払うのは自分の貯蓄まで（max(0,貯蓄)）＝払い手ごとの1本の率（作者裁定 Q4＝POP が払えない税は請求を立てない・窃盗と没収への拡張は Q4 の払い手の側の形を推奨で広げたもの＝(144) 注記）。
///   呼び手が額に Σ重み（同じ並びの和）を渡せば 額÷Σ重み＝1 ちょうど＝請求そのもの。
/// <paramref name="taken"/>＝非 null なら払い手ごとの動いた額を並びの順に足す。否定形のガード＝NaN の額・請求は取らない。
/// <paramref name="heldKnown"/>＝（重み null のときだけ読む）呼び手が**同じ並びで**もう求めた Σmax(0,貯蓄)（NaN＝ここで数える）。★(351) 切り下げと通貨改革は、額を決めるのに払い手の保有の合計を先に数える
///   ＝その和と同じ和をここでもう一度数えていた。呼び手の求めと移し方の間は何も書かない＝同じ順の同じ和＝1 ビットも違わない（番人＝<see cref="PopWindow.Verify"/> が求め直して確かめる）。</summary>
````

## L360 T `if (!(take > (coef is not null && coef.TakeFloorPositive > 0 ? 0 : 1e-12)) || !(held > 1e-`
````
// ★(984) 旗 TakeFloorPositive＞0：動かす額が正なら動かす（0＜額≦1e-12 の塵の債務も返る）・旗 0／coef null＝今の床 1e-12・保有の床は両方で同じ
````

## L361 T `if (PopWindow.Verify) PopWindow.GuardWrite("取り立て");`
````
// ★(352) 反証役〔pu v〕低：動かす額が決まった後（0 しか動かさない呼びは止めない＝他の書き込み口と同じ形）
````

## L362 T `foreach (var p in payers) { double s = take * (Math.Max(0, p._savings.Value) / held); p._s`
````
// (262) 計器（読むだけ）
````

## L374 T `if (s > 0) { payers[i]._savings -= new MoneyStock(s); moved += s; if (Instruments.On) Inst`
````
// (262) 計器（読むだけ）
````

## L380-380 W 直後 `internal void SettleMonth()`
````
/// <summary>★月末の精算（月内の仮勘定＝1本の月内の形・計画 §10-12-2-3）＝その月の所得と支払いの差を貯蓄へ（EconomyStep の月の終わり・2026-09-30 段4g に Pop の中へ移した）。</summary>
````

## L385 T `if (Instruments.On) Instruments.Sav(this, Instruments.SavKind.Settle, Calendar.OneMonthOf(`
````
// (262) 計器（読むだけ）：同じ式をもう一度（純粋）
````

## L388-399 W 直後 `internal static double Blend(Pop from, Pop to, double n, bool draft = false, double female`
````
/// <summary>★人の移し方の1本（2026-09-30 処理の一元化 段4e I＝設計の <c>PopTransfer.Blend</c>・Pop の中の1本）：<paramref name="from"/> から <paramref name="to"/> へ
/// <paramref name="n"/> 人を移し、移る組の属性を受け手へ人数で混ぜ、人と動くお金（貯蓄・債務・国債の保有＝1人あたりの按分）を <see cref="Carry"/> で運ぶ。
/// 呼び手＝徴兵と復員と階級の移動（Military.MoveCohort）・州の中の移動（PopDecision）・社会移動（Meritocracy）・州の間の移民と入植（Territory.MoveBetweenRegions）・
/// 越境移民と招聘（MultiNationSim.MoveAbroad＝お金は換算して呼び手が運ぶ＝<paramref name="money"/>＝false）。
/// ── 移る組の構成＝送り手の平均。選抜のときだけ違う（残る側はその分だけ質量保存で動く）：
///   <paramref name="draft"/>＝徴兵＝男の [<paramref name="draftLo"/>, <paramref name="draftHi"/>) 歳だけ（子供・老年・女性は残る＝男手の消えた村）／
///   <paramref name="femaleAccess"/>＝20歳以上の女性の選抜（専門職の性別の門＝送り手の女性比×これ）／<paramref name="literacy"/>＝識字の選抜（昇格）。
/// ── 受け手の側：年齢と男女の構成は**層×性の人数**で足す（<see cref="BlendAges"/>＝2026-09-30 段4e 後半の年齢の層＝旧の子供比・老年比・成人の女性比の3欄を置き換えた）。
///   識字・闘争度・政治意識・国民意識・中核民族の割合・思想の分布は人数で混ぜる
///   （作者裁定 2026-09-28 §14-2 の6「国内の移動でも国民意識・政治意識・思想の分布を運ぶ＝1本の Blend で全部」）。国民意識の未捕捉（&lt;0）は重み0（書き手が触らない POP は読み手も見ない）。
///   中核民族の割合は <paramref name="coreAtDest"/>＝受け手の国での移る組の値（越境＝受入国の中核民族ではない＝0）。旧の旗（MigrationCarriesHumanCapital・MigrantCarriesConsciousness）は焼き込み。
/// ⚠[0,1] の量は否定形のクランプ（NaN は0の側）。戻り値＝動いた人数。</summary>
````

## L404 T `if (Instruments.On) Instruments.Route(routeFile, routeLine, 'P', 'P');`
````
// (262) 計器（読むだけ）：呼び出しの位置（コンパイル時の定数）を貯蓄の台帳の経路名にする
````

## L409 T `BlendAges(from, to, n, s0, t0, draft, draftLo, draftHi, femaleAccess);`
````
// 年齢と男女の構成（16本）
````

## L410-410 W 直後 `double cLit = double.IsNaN(literacy) ? from.Literacy : literacy;`
````
// 移る組の構成（送り手の平均・選抜のときだけ違う）
````

## L413-413 W 直後 `double rest = s0 - n;`
````
// 残る側＝選抜で組が平均と違う量だけ質量保存で動かす（平均と同じなら1ビットも触らない）
````

## L416-416 W 直後 `to.Literacy = PoliticalQuantities.Clamp01((to.Literacy * t0 + cLit * n) / t1);`
````
// 受け手の側＝人数で混ぜる
````

## L426-426 W 直後 `double frac = n / s0;`
````
// お金（1人あたりの按分＝ストック×(運ぶ人数÷規模)・E3）は人と一緒に（越境は呼び手が換算して運ぶ）
````

## L430 T `if (Instruments.On) Instruments.UrxRtFlow(from, to, n, routeFile);`
````
// (697) 計器（読むだけ）：都市への入りと都市からの出の総量を呼び手のファイルごとに（往復の表 urxrt_s）
````

## L435-437 W 直後 `private static void BlendAges(Pop from, Pop to, double n, double s0, double t0, bool draft`
````
/// <summary>★年齢の層の運搬（<see cref="Blend"/> の中・段4e 後半）＝移る組の人数16本を受け手へ人数で足す。移る組は送り手の構成（送り手の割合は1ビットも動かない）。
/// 選抜のときだけ違い、残る側が質量保存で動く：徴兵＝男の [<paramref name="draftLo"/>, <paramref name="draftHi"/>) だけ（層の中は按分）／
/// 性別の門＝20歳以上の層の女性を <paramref name="femaleAccess"/> 倍（男はその分だけ多く＝層の人数は送り手の比のまま）。いる人数を超えては移らない（<see cref="TakeWeighted"/>）。</summary>
````

## L440 T `int nb = from.Ages.Length;`
````
// 2×層の数（送り手と受け手は同じ器＝プロセス全体で1つ）
````

## L470-471 W 直後 `internal void Rebase(World from, World to)`
````
/// <summary>★人が動かずに国が替わるときの換算（2026-09-30 段4e＝人と動くお金の換算）＝割譲・再併合で州ごと別の国へ移る POP の貯蓄と債務を、
/// 受け手の国の通貨へ（換算の1本 <see cref="World.CrossFx"/>・差は換算の台帳へ・債務は負の額として換算）。国債の保有は発行国の対外債へ振り替える（呼び手）。</summary>
````

## L474 T `double sav0 = _savings.Value;`
````
// (262) 計器（読むだけ）
````

## L480-484 W 直後 `public static readonly AgeLayout Layout = AgeLayout.FromEnvironment();`
````
// ==== 年齢の層（2026-09-30 段4e 後半・<see cref="Ages"/>）====================================================
// ★2026-10-21 (478)：層の数・下端・幅・標準のピラミッド・大人の境は <see cref="AgeLayout"/> が持つ（8層〔今〕と11層〔作者の刻み〕・プロセス全体で1つ＝<see cref="Layout"/>）。
//   配列を読む関数（<see cref="AgeShareOf"/>・<see cref="StepAges"/>・<see cref="KillAges"/>・<see cref="AgeOneMonth"/>・<see cref="DisasterWeights"/>・<see cref="EnteringAdultsMonthlyOf"/>）は
//   配列の長さから器を引く。新しい配列を作る関数（<see cref="StdAges()"/>・<see cref="SoldierAges()"/>・<see cref="MortalityWeights(double)"/>）は <see cref="Layout"/> を読む。
/// <summary>★年齢の層の器（プロセス全体で1つ）＝環境変数 POB_AGE_BANDS（未設定か11→作者の11層・8→旧の10歳刻み8層）。</summary>
````

## L486-486 W 直後 `internal const double StdPyramidGrowth = 0.005;`
````
/// <summary>標準のピラミッドを作った安定人口の年の増加率（表の前提＝設計書 §3-1 の「年0.5%増」）。死の年齢の重み（<see cref="MortalityWeights(double)"/>）だけが読む。</summary>
````

## L489-489 W 直後 `internal static readonly double[] StdAgesRef = StdAges();`
````
/// <summary>標準のピラミッドの割合（読むだけの写し・<see cref="Layout"/> の器）＝起点合わせの物差し（<see cref="StdShare"/>）。</summary>
````

## L491-491 W 直後 `internal static double StdShare(double lo, double hi, int sex = -1) => AgeShareOf(StdAgesR`
````
/// <summary>標準のピラミッドでの <see cref="AgeShare"/>＝読み手の起点合わせ（1783年の値を旧と揃える）の物差し＝新しい数0。</summary>
````

## L494-494 W 直後 `internal static double[] StdAges() => StdAges(Layout);`
````
/// <summary>標準のピラミッド（男女は各層で半々）＝全ての POP の初期値（<see cref="Layout"/> の器）。</summary>
````

## L503-503 W 直後 `internal static double[] SoldierAges() => SoldierAges(Layout);`
````
/// <summary>兵士 POP の初期の構成＝男の20〜39歳の2層に標準のピラミッドの比（8層で 20〜29:30〜39＝16:13・設計書 §3-1／11層で 21〜30:31〜40）。</summary>
````

## L513-514 W 直後 `internal static double[] MaleAgeWeights(AgeLayout l, double lo, double hi)`
````
/// <summary>★男の [<paramref name="lo"/>, <paramref name="hi"/>) 歳だけに重みを置く年齢の重み（2×層の数本・男＝層との重なりの割合・女＝0）＝将校の戦死の年齢の重み（Diplomacy）。
/// 8層の (20, 50) は 層 2・3・4 の男が 1・ほか 0（旧の 16 本の並びと同じ）。重みは尽きれば残りの全員から取る（<see cref="TakeWeighted"/>）。</summary>
````

## L522-522 W 直後 `internal void SetAdultFemaleShare(double female)`
````
/// <summary>職業 POP の性比（設計書 §3-1）＝20歳以上の層〔<see cref="AgeLayout.AdultFirstBand"/> 以上〕の男女を職業の女性比 <paramref name="female"/> で分ける（それより下は半々のまま）。</summary>
````

## L533-534 W 直後 `public double AgeShare(double lo, double hi, int sex = -1) => AgeShareOf(Ages, lo, hi, sex`
````
/// <summary>★年齢の読み口の1本：<paramref name="lo"/> 歳から <paramref name="hi"/> 歳（含まない）・性 <paramref name="sex"/>（0＝男・1＝女・負＝男女計）の割合。
/// 層の中は一様として按分（重なり÷層の幅）。最上層は分けない（lo が下端以下かつ hi＝∞ のときだけ全部）。人数は Size×これ。</summary>
````

## L549-552 W 直後 `internal static double[] MortalityWeights(double crudeBirthRate) => MortalityWeights(crude`
````
/// <summary>★平常の死の年齢の重み（1人あたりの相対の死亡率・2×層の数本）＝標準のピラミッドが、模型の1783年の出生（粗出生率 <paramref name="crudeBirthRate"/>）と表の増加率の下で
/// 動かない重み（設計書 §3-3・作者裁定 Q3＝層の比から導く・新しい数0・旧 OldMortalityWeight を置き換える）。μ_b＝(入る流れ−出る流れ)÷n_b−増加率（年）。
/// 死因別の死（9-2）までの近似＝率の水準は模型の総数（出生−純増）が決め、これは層への配り方だけ。
/// ★(478)：11層でも同じ規則（層の幅の表から）＝今の重みを写さず標準のピラミッドから導き直す（写すと標準が動かない条件が崩れる）。</summary>
````

## L562 T `if (!(mu > 0)) mu = 0;`
````
// 死亡率は負にならない（否定形＝NaN も0）
````

## L569-572 W 直後 `internal void StepAges(double size0, double births, double deaths, double[] mortW)`
````
/// <summary>★1か月の年齢の層（Simulation.DemographicStep の POP のループ・月次）：月初の規模 <paramref name="size0"/> の人数から、加齢（<see cref="AgeLayout.Outflow"/>）・
/// 出生（0歳〔8層では0〜9歳〕の層の男女へ半々＝作者裁定 Q11）・平常の死（年齢の重み <paramref name="mortW"/>）の流れを**全て月初の人数から同時に**数えて動かし（設計書 §2-2「前の値から同時に」）、
/// 割合へ戻す＝標準のピラミッドは1783年の出生と死で丸めを除いて動かない（順に進めると月の中の順の2次の誤差が10年で 1e-4 残る）。Size は呼び手が書く（これは構成だけ）。
/// <paramref name="deaths"/>＝実際に減った人数（負人口の保護の後）。層が死で尽きた月（大きな死）は0で止める（割合は合計で割る）。</summary>
````

## L593-593 W 直後 `internal void KillAges(double sizeBefore, double d, ReadOnlySpan<double> w)`
````
/// <summary>★年齢を選ぶ死（人数の上）：月初の規模 <paramref name="sizeBefore"/> から <paramref name="d"/> 人を1人あたりの重み <paramref name="w"/> の比で層×性から取り、割合へ戻す（Size は呼び手が書く）。</summary>
````

## L604-605 W 直後 `internal static void DisasterWeights(Span<double> w, double excess)`
````
/// <summary>災害死（疫病・飢えの超過死亡）の年齢の重み＝0〜9歳〔下端が10歳未満の層〕と60歳以上〔下端が60歳以上の層〕に (1＋<paramref name="excess"/>)・ほかは1
/// （設計書 §2-4・旧の子供と老年の超過 DisasterAgeMortalityExcess と同じ数）。層の数は <paramref name="w"/> の長さ（2×層の数）。</summary>
````

## L616-616 W 直後 `internal static void AgeOneMonth(ReadOnlySpan<double> n0, Span<double> n)`
````
/// <summary>1か月の加齢（人数の上・層ごと・性ごと）＝月初の人数 <paramref name="n0"/> から流れを数えて <paramref name="n"/> を動かす（前の値から同時に）。</summary>
````

## L628-629 W 直後 `internal static void TakeWeighted(Span<double> n, Span<double> took, ReadOnlySpan<double> `
````
/// <summary>★重みで人を取り出す1本（人数の上）：<paramref name="d"/> 人を1人あたりの重み <paramref name="w"/>（null＝一様）の比で層×性から取り、取った人数を <paramref name="took"/> へ足す。
/// いる人数を超えては取らない＝あふれた分は残りへ同じ比で配り直す（重みの付いた層が尽きたら残りの全員から一様に）。死（平常・災害・将校）と徴兵の移る組が読む。</summary>
````

## L652-652 W 直後 `internal void SetAgesFromCounts(ReadOnlySpan<double> n)`
````
/// <summary>人数16本から割合へ（合計で割る＝丸めの積み重ねを残さない）。人数の和が無い（空の POP）は前の割合のまま＝次に人が入ったとき移る組の構成で上書きされる。</summary>
````

## L661-661 W 直後 `internal double EnteringAdultsMonthly() => EnteringAdultsMonthlyOf(Ages);`
````
/// <summary>★世代の置き換わり（1か月）＝大人に入る人の割合（8層では10〜19歳の層から20〜29歳へ・11層では16〜20歳の層から21〜30歳へ出ていく1か月の流れ÷規模）＝加齢と同じ1本（<see cref="AgeLayout.Outflow"/>）。</summary>
````

## L665 T `var l = AgeLayout.OfLength(a.Length);`
````
// ★(478) 大人に入る前の最後の層（8層で10〜19歳・11層で16〜20歳）の男女の出る流れ
````

## L669-672 W 直後 `internal double GenerationalMix(double x, double xIn, int months)`
````
/// <summary>★世代の流れで大人の性質を混ぜる1本（2026-09-30 段4e 後半 4e-3・設計書 §5-1・作者裁定 2026-09-29「人が運ぶ記憶は年齢の層の入れ替わりで減衰」）：
/// <paramref name="months"/> か月のあいだに20歳になって大人に入る若者 E が伝わった値 <paramref name="xIn"/> を持ち込み、入る前の大人 A（20歳以上）と混ざった後の値
/// ＝(A·x＋E·x_in)÷(A＋E)。大人の死・移出は平均で出る（x は変わらない）。E＝0 なら x のまま・x は必ず x と x_in の間（加重平均）。
/// ⚠性質は大人に1値（作者裁定 Q5＝大人の境は20歳）。読み手＝識字の下がる側（TechEducation）・就学履歴（Politics.SchoolingConversion）。</summary>
````

## L679-679 W 直後 `internal static double StdReplacementRate(int months)`
````
/// <summary>★標準のピラミッドの世代の置き換わり率（<paramref name="months"/> か月）＝E÷(A＋E)。国の無い抽象の勢力の記憶（外交の遺恨）が読む＝新しい数0。</summary>
````

## L687-687 W 直後 `public readonly record struct Input(int GoodId, double QtyPerOutput);`
````
/// <summary>レシピの投入（§2.4・§11.2 Recipe）：産出1単位あたり投入財をどれだけ要するか。</summary>
````

## L690-693 W 直後 `public sealed class Producer`
````
/// <summary>
/// 生産者（RGO／工場）。§9-A と §9-C が共有。自営は OwnerPopIndex==LaborPopIndex。
/// 工場は Inputs（複数投入＝反モノカルチャー §2.4）を持つ。RGO は空。
/// </summary>
````

## L697 T `public double Productivity;`
````
// 労働1あたり産出（capacity = labor.Size × これ）
````

## L698 T `public double Capacity;`
````
// 能力（§9-C が毎月更新）
````

## L699-700 W 直後 `[System.NonSerialized] internal double HeadsLastMonth;`
````
/// <summary>★(519→524) 能力の式の頭数＝担い手の POP の Size×fraction（供給の段が最後に通った月の値・書くだけの控え）。読み手は交渉の取り分（<see cref="UnionBargaining"/>・旗 UnionBargaining が正のとき）。
/// 作業領域（[NonSerialized]・セーブに載らない）＝供給の段が毎月書き直す。眠った生産者・倒産中は能力の式を通らない月なので 0。線 sm（枠と機械・(519)・採らない）の欄と書き手だけを持ち込んだ。</summary>
````

## L702-703 W 直後 `public double LaborWeight;`
````
// ★配分信念（2026-08-22・恐慌をAIの賢さで抑える）：期待市場価値 付加価値×売れる量 のEMA（時定数＝SupplyAdjustSpeed）。
//   生産者はスポット価格でなくこの信念で職を選ぶ＝コブウェブの教科書的減衰（適応的期待）。点火済み（2026-08-22）＝常時書かれる。
````

## L705-706 W 直後 `public double Output`
````
/// <summary>実産出（§9-A がゆっくり寄せる状態変数）。★書き込みで版を進める（2026-09-30 (136) 計算量の予算・恒等）：書き込みのたびに、
/// この生産者を持つ州の <see cref="Region.GdpVersion"/> を1つ進める＝<see cref="World.GdpPerMonth"/> の使い回しを捨てる理由（宛先は <see cref="_siteRegion"/>）。</summary>
````

## L713 T `public double DesiredOutput;`
````
// 採算が合えば作りたい量（能力×採算）。入力需要の再点火トリクルに使う（死んでも需要を残し価格で蘇生＝床なしでデッドロック防止）
````

## L714 T `public double MarginalCost;`
````
// 1単位の付加価値費用（賃金・生存線分）＝**参照価格の単位**。読むときは EconomyStep.MineralSelfCost（×自己費用の物価水準 World.CostPriceLevel×州の乗数＝価格と同じ単位・2026-09-27 (73)）を通す。退出閾値には投入費を加算（§9-A 仕組み2）
````

## L715 T `public int OwnerPopIndex;`
````
// 地代/利潤の受け手
````

## L716 T `public int LaborPopIndex;`
````
// 賃金の受け手
````

## L717 T `public Share RentShare;`
````
// ρ。自営は実質無関係（owner==labor）
````

## L718 T `public MoneyPerMonth? FrozenLandRentPerMonth;`
````
// ★(937) 定額の土地税の凍結した名目の地代（RGO の割る前の総額・null＝凍結なし＝収穫比例）。書くのは EconomyStep.PayOwnerRent（旗 LandTaxFixedBasis＞0・World.LandTaxBasisFixed の国・助走の月ごとに上書き＝助走の最後の月の地代・不在地主の RGO を除く）と Territory.TransferRegion（移転で null）だけ。状態＝凍結した月の名目の地代は盤面から導けない（設計思想⑩の〈本当に必要な場合〉・事前登録 m937）
````

## L719 T `public double[]? LandShares;`
````
// ★(895) 土地の持ち分（RGO の地代の受け手の割合・和 1）：0 貴族＝OwnerPopIndex・1 教会＝州の ClergyPopIndex・2 農民の自作＝LaborPopIndex・3 市民の地主＝州の InvestorPopIndex。null＝今の OwnerPopIndex に 1.0（割り方は EconomyStep.PayOwnerRent・AddRentRecipients）
````

## L720 T `public List<Input> Inputs = [];`
````
// レシピの投入材（多段チェーン §2.4）。RGO は空
````

## L721 T `public double LastInputFill = 1.0;`
````
// 前期の投入充足率（§9-A 注2：投入が買えないと実産出が落ちる・律速）
````

## L722-723 W 直後 `public double LastMaterialFill = 1.0;   // 素材（非役務投入）だけの最小配給率。素材投入なし＝1.0`
````
// UI診断用の純記帳（2026-07-19・力学は一切読まない）：素材のみの充足率。
// LastInputFill は労働（役務）込みの最小値なので「原料不足」の根拠に使えない（労働不足を原料不足と誤帰属していた）。
````

## L724 T `public double LastMaterialFill = 1.0;`
````
// 素材（非役務投入）だけの最小配給率。素材投入なし＝1.0
````

## L725 T `public double Scale = 1.0;`
````
// 操業規模（企業の参入退出）。採算で伸縮：採算良→拡張・採算悪→縮小（≒閉鎖）。労働配分の重みに掛かり、閉鎖工場は労働を採算の良い財へ解放（§9-A 仕組み4）
````

## L726 T `public double SmoothedMargin;`
````
// 数期移動平均の採算＝(価格−限界費用)/限界費用 のEMA。倒産判定に使う（一過性の赤字では潰れない）
````

## L727 T `public bool Bankrupt;`
````
// 倒産（操業停止・産出0・労働解放）。慢性赤字で倒産し、価格が原価を上回れば新規参入で再開＝有限責任（§9-A 仕組み4 拡張）
````

## L728-729 W 直後 `public double BuiltScale`
````
/// <summary>プレイヤーが建設した工場容量（Scaleの下限＝離散工場・Vic2型）。建設で増え、建てた工場は操業を続ける。
/// ★書き込みで印を立てる（2026-09-30 計算量の予算）：「国営持分を持ちうるか」（&gt;1e-9）が変わる書き込みは、州の工事と国営持分の一覧（<see cref="Region.StateSites"/>）を作り直す印を立てる。</summary>
````

## L740-740 W 直後 `[System.NonSerialized] internal Region? _siteRegion;`
````
/// <summary>作業領域（[NonSerialized]・セーブに載らない）：この生産者を持つ州（一覧を作った時と GDP の和を取った時に付ける・生産者は州をまたがない）＝印と版の宛先。</summary>
````

## L742-745 W 直後 `public Share MothballFrac;`
````
// ★休止（mothball・AI審査2-6・2026-08-31）：BuiltScale のうち操業を止めている割合（0..FactoryMothballCap×民営持分）。
//   建てた工場は倒産免除＋床で全稼働を強制されるため、慢性赤字が資本家の負債へ垂れ流しになる欠落の是正＝
//   設備は残る（BuiltScale不変）が床が (1−MothballFrac) 倍に縮む＝維持労働と産出だけ止まり、採算が戻れば同率で再開。
//   FactoryMothballRate=0（測定OFF腕）では一度も書かれず0のまま＝床はビット恒等（★既定0.05でlive・2026-08-31点火）。
````

## L747-750 W 直後 `public int StateTarget;`
````
// 国営工場カウンター（作者裁定2026-08-10）＝**目標の棟数**（0..StateTargetMax）。1カウント＝1回の建設完成＝Coefficients.FactoryBuildScale。
// 現在の棟数は BuiltScale/FactoryBuildScale（World.StateUnitsOf）＝これは「意志」であって「実現」ではない。
// 目標>現在なら World.StepStateTargets が着工を試み（国庫・建材採算・技術ゲートが速度を決める）、
// 目標<現在なら1棟ずつ払い下げる。プレイヤーもAIも同じこの1本の動詞（World.SetStateTarget）を操作する。
````

## L752-754 W 直後 `public double ConstructionLeft`
````
/// <summary>建設残り**日数**（&gt;0＝建設中）。日次で減り、完成で BuiltScale が1棟増える＝建設は数年かかる。
/// ★書き込みで印を立てる（2026-09-30 計算量の予算）：「工事中か」（&gt;0 と ≦0 の両方の述語＝NaN も）が変わる書き込みは、州の工事中の一覧
/// （<see cref="Region.BuildSites"/>）を作り直す印を立てる＝着工・完成・テストの直書きを1か所で拾う（セーブの読み込みは一覧が空から作り直し・州の移転は一覧が州ごと移る）。</summary>
````

## L765 T `public double ConstructionPayPerDay;`
````
// 建設の1日あたり支払い（国庫→工員＝労務費。素材費でなく労務費）
````

## L766-767 W 直後 `public Share SubsidyRate;`
````
// 補助金（2026-07-19に率へ拡張）：民間持分の赤字のうちこの率を国庫が肩代わり。満額(1.0)なら旧boolのONとビット同値
// （赤字全額肩代わり＋採算floor0＝操業維持）。部分率は損失を率ぶんだけ緩和＝深い赤字なら倒産もありうる。
````

## L769 T `public bool Subsidized => SubsidyRate.Value > 1e-9;`
````
// 互換の読み口（AI/UIの判定用・直列化されない）
````

## L770-771 W 直後 `public double SubsidyDependence;`
````
// 産業政策の代償 レバー1（2026-07-24）：補助依存ストック（0..1）。補助金/国営持分で赤字操業した月に漸増・自立黒字で漸減（コルナイの軟予算制約）。
// 実効生産性に ×(1−SubsidySoftBudgetDecay×これ) で乗る＝恒久補助は実質コスト逓増。SubsidySoftBudgetDecay=0（対照腕）で乗数×1.0＝厳密恒等・既定0.4でlive（ストック自体は常時更新＝プローブ可視）
````

## L773 T `public double LastRevenue, LastCost;`
````
// 工場別の収入・支出（損益＝収入−支出。UI表示用）
````

## L774 T `public bool BuysLabor;`
````
// 労働力（財）を市場から買う工場。労働配分（pop労働の取り合い）に参加せず、賃金でなく労働購入で人を雇う
````

## L775 T `public bool IsRgo;`
````
// 自家農家・採取場（土地の生産者）。農業労働力を買い、残余＝地代の行き先が農制（地主/自作）。土地税の課税ベース
````

## L776 T `public string RecipeName = "";`
````
// 複数レシピの表示名（例：電力の「石炭火力」「石油火力」）。空＝単一レシピ
````

## L777-778 W 直後 `public int SiteRow = -1;`
````
/// <summary>★産地の発見の行（2026-10-01 (148)）＝この生産者が産地の発見の表（<see cref="Catalog.Sites"/>）の行の産地か（−1＝違う＝開始時の賦存か非産地）。
/// 構築のときに場所の名前と財から置く（生産者は自分の州の名前を知らない＝セーブに載せる）。旧＝TechOnset（技術水準の傾斜の始まり・後期の銀と硫黄の産地だけ）。</summary>
````

## L780-781 W 直後 `public bool SiteFound;`
````
/// <summary>★その産地が見つかったか（2値・失われない＝州と一緒に移る）。<see cref="Discovery.Step"/> だけが書く。門＝<see cref="Discovery.Gate(Region, Producer)"/>。
/// 旧＝シルバーラッシュの TechOnset=0 の書き戻しと Region.SilverRushed（再発止め）の2本。</summary>
````

## L783-784 W 直後 `public int DiscoveryRow`
````
/// <summary>★発見の表の行（段2A・2026-10-01）＝この生産者の（財・レシピ）の行の添字（−1＝表に無い＝地理差＝常にある）。
/// 作業領域の控え（[NonSerialized]＝財とレシピ名から導く・セーブに載らない）＝財かレシピ名が変わったら引き直す。</summary>
````

## L798-798 W 直後 `public int RetiredByRow { get { _ = DiscoveryRow; return _retRow; } }`
````
/// <summary>このレシピを退かせる発見の行（−1＝退かない）＝鋳鉄の機関は転炉鋼の発見で退く（Catalog の退場の表）。★(537) 武器の旧のレシピ（マスケット・鋳鉄砲）は退場の表に無い＝退かない。</summary>
````

## L800-801 W 直後 `public int OpenedByRow = -1;`
````
/// <summary>★(537) 新世代の門の印（構築時に <c>Simulation.AddAlternateRecipes</c> だけが置く）＝この発見の行を知るまで門 0（既定の鋼の小銃・鋼の砲＝転炉鋼）。−1＝印なし。旧のレシピ（マスケット・鋳鉄砲・鋳鉄の機関）は退場しない（退場の表は消した）。
/// <see cref="Discovery.Gate(Region, Producer, bool[])"/> だけが読む。新しい発見の行は足さない（<see cref="Catalog.Discoveries"/> の添字は動かない）。</summary>
````

## L805 T `public double ElecFill;`
````
// 電化率（0..1・EMA）。電力を買えた工場は稼働率が上がり（夜間操業）労働投入が減る（機械化）。電力が高い/送電網が無いと0
````

## L806 T `public double SteamFill;`
````
// 蒸気動力化率（0..1・EMA・電化と同型）。蒸気機関の発明後（SteamScale>0）採算が合えば石炭を焚き稼働率↑（SteamBoost）。1789年は水車/畜力＝0＝無ブースト・石炭を焚かない。電化が安くなると電力が蒸気を代替（(1−ElecFill)）
````

## L807 T `public int[] EffGoods = [];`
````
// RGOの効率化材の候補（肥料/帆船/飼料/工具/火薬…）。市場が安い・profit-gateを通る物を毎期選ぶ。空＝なし（工場・役務）。Catalog.RgoBoosterGoods で設定
````

## L808 T `public double EffFill;`
````
// RGO効率化率（0..1・EMA）。効率化材を買えたRGOは産出が増える（電化のRGO版）。買えない/割に合わないと0
````

## L811-811 W 直後 `public sealed class Market`
````
/// <summary>財ごとの市場状態（§9-A）。単一地域。</summary>
````

## L819-820 W 直後 `public double LastProduced`
````
/// <summary>今期の産出。★書き込みで印を立てる（2026-09-30 計算量の予算）：「死んだ財か」（需要も産出も 1e-9 未満＝国内価格が動かさない財）が
/// 変わる書き込みは、州の死んでいない財の一覧（<see cref="Region.LiveGoods"/>）を作り直す印を立てる。</summary>
````

## L833 T `public double LastImport;`
````
// 今期の輸入量（§5.1・財収支に算入）
````

## L834 T `public double LastExport;`
````
// 今期の輸出量
````

## L835 T `public double PrevImport;`
````
// ★S2台帳衛生（旧の旗 TradeLedgerFix・2026-08-30）：前清算の輸入量。ゼロ化(:EconomyStep)直前に退避＝清算外の読み手（人口の主食純輸入）が常に0を見る非対称の是正。旗は 2026-10-18 に焼き込み＝読み手は常に読む
````

## L836 T `public double PrevExport;`
````
// 同・前清算の輸出量
````

## L837-837 W 直後 `public double LastDemand`
````
/// <summary>前期の総需要（消費＋投入）。次期の労働配分の重み（持続・リセットしない）。★書き込みで印を立てる（<see cref="LastProduced"/> と同じ）。</summary>
````

## L848-848 W 直後 `internal const double DeadEps = 1e-9;`
````
/// <summary>死んだ財の境（<see cref="MarketFlow"/> の「需要も産出も Eps 未満なら動かさない」と同じ 1e-9）。</summary>
````

## L850-850 W 直後 `[System.NonSerialized] internal Region? _liveRegion;`
````
/// <summary>作業領域（[NonSerialized]・セーブに載らない）：この市場を死んでいない財の一覧に載せた州＝印の宛先。</summary>
````

## L852-855 W 直後 `[System.NonSerialized] internal bool FlowAsleep;`
````
/// <summary>★月の残りを眠る国内の市場（2026-09-30 計算量の予算 第2回・近似）＝作業領域（(1125) で載る＝セーブに書く）。
/// 書くのは <see cref="MarketFlow.StepDay"/> だけ：月の最初の旬に作り直し、供給も在庫も無く参照より上に張り付いた財が不動点に着いた
/// （1日の動きが相対 1e-8 を切った・ビットで止まった）旬に立てる。立っている財は月の2旬目以降を回さない（入力は月の中で変わらない＝境界相だけが書く）。
/// 眠っている間の値段は不動点から相対 約2.5e-8 の内（S3b の打ち切りの上界）＝読まれても追いつく分はこの上界の内。</summary>
````

## L857-859 W 直後 `[System.NonSerialized] internal double ClearFailLp;`
````
/// <summary>★清算で成約しなかった行の印（2026-09-30 計算量の予算 第2回・近似）＝作業領域（(1125) で載る＝セーブに書く）。
/// 前にこの行（州×財の買い手）を走査して成約しなかったときの自州価格（金建て）。0＝印なし（成約した・門で飛ばした・四半期の最初の清算で消す）。
/// 読むのは月の清算（四半期の2・3回目）の間引きだけ＝不足だけで起きた行は、自州価格がこの値を超えて上がるまで眠る（<see cref="InternationalMarket"/>）。</summary>
````

## L861-862 W 直後 `[System.NonSerialized] internal Region[]? ClearPartners;`
````
/// <summary>★(403) 係数 <c>ClearingPartnerScope</c>（既定 4）：この四半期の最初の清算で成約した買い手行の相手（相手にした売り手・先の K 人）。
/// 作業領域（[NonSerialized]・セーブに載らない・状態ではない）。書くのは最初の清算の走査（<see cref="InternationalMarket"/>）だけ・最初の清算が全て消して作り直す・読むのは四半期の 2・3 月目の清算（thin）だけ。null＝覚えなし＝全ての売り手を見る。</summary>
````

## L864 T `public double ReservationWage;`
````
// 留保賃金＝労働者が慣れた賃金の下限（労働財のみ使用）。賃上げに速く・賃下げに遅く追従＝下方硬直の創発源
````

## L865-868 W 直後 `public Share LastRation = new Share(1.0);`
````
// 配給率の記帳（作者指示2026-08-10「生産コスト欄の各材に供給率を出す」）＝EconomyStep のクリアリングが決めた
// その月のこの財の配給率 ration[gi]（買い手が欲しかった量のうち実際に渡った比）。Producer.LastMaterialFill は
// 「素材の**最小**」の1本しか持たない＝どの材がどれだけ届いたかは読めなかった。純記帳＝力学の読み手はゼロ
// （生産タブの生産コスト欄と ProductionDiag.CostItem.Fill だけが読む）。素材投入を持たない財は 1.0 のまま。
````

## L870-881 W 直後 `public double[]? PopBought;`
````
// popが自腹で買った量の**階級別**内訳（作者フィードバック2026-08-10 項目6）＝添字は <see cref="Pop.ClassId"/>、
// 値はその月に実際に渡った量（非役務＝p.GoodDemand×ration／役務＝階層配分で渡った qty）。
// ★2026-09-20：**純記帳ではなくなった**＝力学の読み手が2つ付いた（旧注記＝「純記帳＝力学の読み手はゼロ。生産タブの
//   『用途』先頭行＝ProductionDiag.ExplainPopConsumption だけが読む」）。
//   ①World.StateMonopolyBreadth＝専売の軸の正規化位置（課税している財が**課税ベース**に占める割合）
//   ②PolicyPreferences.EffectiveStateMonopoly＝小営業の反対の強さ（実効平均率）
//   理由＝専売は pop の消費ループでしか徴収されない（EconomyStep のこの記帳の直後に StateSpent[StateMonopoly] へ積む同じ qty）ので、
//   「誰がいくら負担しているか」を持つ台帳はこれしかない。⚠だからこの配列は**消すと政治が壊れる**（表示専用の器ではない）。
// ⚠これを置く理由＝**popの購入量はどこにも残っていなかった**：Pop.GoodDemand は「現在処理中の財」の作業用フィールドで
//   財ごとに上書きされる（月末に残るのは最後に処理した財の値だけ）。Market.LastSold は工場投入・投資・軍需を含む合算。
//   「誰がこの財を買ったか」を画面で言うには渡した瞬間に記帳するしかない（LastRation と同じ作法）。
// ⚠義務教育の国庫肩代わり分は**入れない**（popの支払いではない）。popの需要が0だった月は null（配列を作らない）。
````

## L883-884 W 直後 `public double[] SpoilLog = new double[12];   // 月次の廃棄量（リング）`
````
// 廃棄材の記録（2026-08-08・作者要望「過剰生産で失われた材を画面に出す」）：腐敗・劣化で消えた在庫量の
// 月次リング（直近12ヶ月）。純テレメトリ＝力学の読み手なし（生産タブの財パネルと spoilprobe が読む）。
````

## L885 T `public double[] SpoilLog = new double[12];`
````
// 月次の廃棄量（リング）
````

## L886 T `public int SpoilIdx;`
````
// リングの書き込み位置
````

## L887-888 W 直後 `public double SpoiledLastYear { get { double s = 0; for (int i = 0; i < 12; i++) s += Spoi`
````
/// <summary>直近12ヶ月の合計＝<see cref="SpoilLog"/> の添字 0..11 の昇順の和（読まれたときに求める＝月次の決算が財ごとに12項を足し直さない・読み手は画面と診断だけ）。
/// 旧＝EconomyStep.Run が月ごと・財ごとに同じ式で書いていた欄（リングを書く所は1つ＝その後の和と読んだ時の和は同じ値）。</summary>
````

## L892-897 W 直後 `public enum ThrottleBinding { None, Shipping, Rail, Port }`
````
/// <summary>貿易タブT2（2026-07-24）：輸送律速の内訳＝cap合成時にどの容量が最小だったか（なし/海運/鉄道/港湾）。純テレメトリ・力学は読まない。
/// ⚠【削除禁止・墓標2026-09-08】この型を持つフィールドはもう無い（読み手ゼロだった <c>Region.LastThrottleBinding</c> を削除した）。それでも**型は残す**：
///   2026-09-08 より前の POBSIM2 セーブには enum 値が入っており、<c>SaveGame</c> の <c>Tag.Enum</c>（SaveGame.cs:296-299）は
///   フィールドが現存するかを見る**前に** <c>ReadTypeRef</c>（SaveGame.cs:267-273）で <c>Type.GetType</c> を引き、無ければ例外を投げる。
///   型を消すと旧セーブの読み込みが必ず落ち、App の SaveMenu の catch に飲まれて「続きから」が黙って無反応になる（＝作者定義の破綻＝セーブ破れ）。
///   規則＝**フィールドは消してよい・型は消してはいけない**。</summary>
````

## L900-900 W 直後 `public sealed class Region`
````
/// <summary>地域（§11.2）。段0/1 は単一地域。Markets は good.Id で添字。Goods は優先度順に持つ。</summary>
````

## L903-904 W 直後 `public byte[]? RecipeTransitionFlags;`
````
// ★通知（作者指示2026-09-04「職人没落・座礁」）：財ごとの通知済みフラグ（bit1=新製法が旧製法を上回った・bit2=職人シェア50%以上を観測・bit4=職人の没落を通知済み）。
//   観測だけ＝力学は読まない。byte[]（1次元）＝SaveGame の Arr で往復する。null＝未確保（初回の四半期末に確保）。
````

## L908 T `public RegionState State = RegionState.Active;`
````
// §A-8
````

## L909 T `public bool IsPort;`
````
// 港州（§5.1拡張）。世界市場と直接交易できる。内陸州は鉄道で港経由（容量ゲート）
````

## L910 T `public double PortCapacity;`
````
// 港湾取扱容量（改修2026-06-28）。0スタート、国家＋資本家の投資で積む。港州の実効海運に**加法的**に上乗せ（0なら従来挙動＝較正中立）。混雑（取扱高>容量）で投資を呼ぶ
````

## L911 T `public double LastPortThroughput;`
````
// 港の取扱高＝この州が買い手か売り手の**海路の成約**の額（四半期累計・港料・保険・拿捕・運河・通過港・海軍の配置が読む）。陸路の直行は積まない（2026-09-27 回4-1）。内陸州にも立つ＝最寄り港（借りた港）まで運んだ海路の荷
````

## L912-914 W 直後 `public string LastLandPartnerName = "";`
````
// ★陸路交易の可視化（2026-08-31・作者報告「陸上交易の経路が非表示」）：この州が**陸路**で運んだ最大の取引の相手州名と額
//   （四半期記録・純テレメトリ＝力学は読まない）。書き手＝BilateralMarket の成約（viaLand<viaSea の対のみ）。
//   リセット＝四半期の第1清算（LastPortThroughput と同じ暦）。地図が PathGraph の実経路で描く。
````

## L916 T `public double LastLandPartnerFlow;`
````
// 陸路の最大の取引の額（上の注記）
````

## L917 T `public double NearestPortDistanceKm;`
````
// 最寄り港への距離km（PortLandKm と同じ値＝陸路が有限なら陸路・無限なら大圏。trade-capacity-even-share-not-network）。内陸州のrailToPort減衰に使用。0=未設定（港州/距離不問）＝減衰なし（現行）
````

## L918 T `public string NearestPortName = "";`
````
// 03 §12-3（2026-07-28）：最寄り港の州名（内陸州が「どの港から海へ出るか」＝手順4の3脚距離と手順5の海盆継承が読む。港州/未設定は空）
````

## L919 T `public double PortLandKm;`
````
// 03手順4A：最寄り港への**陸グラフ**距離km（静的＝構築時にPathGraphで1回計算。港州/未設定=0）。3脚の陸脚
````

## L920-922 W 直後 `public string TransitPortName = "";               // 第1候補の外国港州名（空＝トランジット対象外）`
````
// ── Stage C（03 §11-6・裁定A3・2026-08-09）：内陸国トランジット。港州ゼロ国の州だけに MultiNationSim.WireTransitPorts が
//    構築時に配線する**外国港州**の静的候補（NearestPortName/PortLandKm と同型＝陸グラフ最短・string/double なので直列化に安全）。
//    候補2は候補1と構築時所有国が違う次点（戦時封鎖の迂回先＝同一所有国の第2港は封鎖で一緒に閉まる）。港のある国・座標なし州は空＝旧挙動。
````

## L923 T `public string TransitPortName = "";`
````
// 第1候補の外国港州名（空＝トランジット対象外）
````

## L924 T `public double TransitLandKm;`
````
// 第1候補への陸グラフ距離km
````

## L925 T `public string TransitPort2Name = "";`
````
// 第2候補（構築時所有国が候補1と異なる次点。無ければ空）
````

## L926 T `public double TransitLand2Km;`
````
// 第2候補への陸グラフ距離km
````

## L927 T `public string LastTransitPortName = "";`
````
// 診断：この四半期に実際に使ったトランジット港（未使用・非トランジット州＝空）。力学は読まない
````

## L928 T `public Basin SeaBasin;`
````
// 03手順5：この州が海へ出る海盆（沿岸州＝最寄り海ノードのbasin・内陸州＝最寄り港の海盆を継承＝§12-3。港なし国は国のBasinへフォールバック）
````

## L929 T `public double LastWarIntensity;`
````
// 05-A（2026-07-29）：今月の戦場強度（劣勢度×接触・0..1）。RGO産出の戦災乗数が読む（毎月Diplomacyが再設定＝戦争が終われば自然回復）
````

## L930 T `public double TariffInterest;`
````
// 州の関税利害（−1=自由貿易を望む農業/輸出州・+1=保護を望む工業州）。国政策との乖離＋不満で分離圧力（§9-E拡張）
````

## L931-931 W 直後 `public int OriginPowerId = -1;                    // 1783年（世界構築時）にこの州を領有していた国の Power.Id＝「故`
````
// ── 案Y（人口物差しの脱汚染・2026-08-21）：PopGrowth の「領土不変な定義」（SelfPlay.cs:1402 の宣言）を実装へ揃える記帳。**観測専用＝力学は一切読まない** ──
````

## L932 T `public int OriginPowerId = -1;`
````
// 1783年（世界構築時）にこの州を領有していた国の Power.Id＝「故地」の恒久ラベル。-1=未設定（較正・最小世界＝記帳不発）。分離独立で生まれた国へ移っても不変
````

## L933 T `public double AnnexBaggage;`
````
// この州が現所有国へ入った時点の持ち込み人口（征服割譲・植民地州昇格で記帳／故地への復帰=OriginPowerId一致と分離建国の創設州は0）。
````

## L934-934 W 直後 `public double AnchorPopulation;                   // ★(369) 扶養力の錨に入る人口＝構築（Simulation の構築・P`
````
// SelfPlay の PopGrowth 分子が Σ(非Seceded州のこれ) を差し引く＝征服インフレの除去（併合後の自然増は自国分として残る）。州が去れば荷物ごと去る＝Σは自動整合
````

## L935 T `public double AnchorPopulation;`
````
// ★(369) 扶養力の錨に入る人口＝構築（Simulation の構築・PrebuiltRegions でない州）の時点の州の人口を 1 回だけ写した定数（以後書かない）。本国の州（BuildNation）だけ非 0・植民地の州昇格（PromoteColonyToRegion）は 0＝その住民は PopulationDelta の台帳に入り旧の錨（pop − PopulationDelta）に入っていなかった。州と一緒に国から国へ移る（TransferRegion は Region を渡す）。読み手＝DemographicStep の錨（(383) で旗 CapacityAnchorFromRegions を既定 1 にして旧の錨 pop − PopulationDelta ごと物理削除）
````

## L936 T `public bool NationHasPort;`
````
// ★2026-08-01：この州の**国**が港州を1つでも持つか（Simulation が毎月複製する World 由来の値）。
````

## L937-938 W 直後 `public double CrimeRate;                          // 接16（監査2026-07-11・2026-07-25休眠配線）：州の犯罪`
````
// ⚠軍需調達は首都州でしか走らないため、「軍艦は港でしか造れない」を `region.IsPort` で判定すると
//   内陸に首都のある国（仏・露・普・墺・西・サルデーニャ）が永久に艦隊を持てなくなる。造船するのは港だが、発注するのは国である。
````

## L939 T `public double CrimeRate;`
````
// 接16（監査2026-07-11・2026-07-25休眠配線）：州の犯罪率（Crime.Stepが毎月算出＝旧はローカル変数で捨てられ全国最悪値のみ保持）。CrimePushWeight>0で移民pushへ合流
````

## L940 T `public double PriceLevel = 1.0;`
````
// 物価水準（World の国家物価×地域乗数）。MarketFlow の参照価格アンカー/床天井を rp×これ にスケール＝一般インフレ/デフレ。1.0＝中立
````

## L941 T `public double LocalPriceMultiplier = 1.0;`
````
// 地域別の貨幣的物価乗数（地域物価・名目賃金の格差）。1人あたり貯蓄（手元現金の厚み）が国平均から乖離する分。現金の厚い金融中心州>1・乏しい辺境州<1。四半期ごとに UpdateRegionalPriceLevels が算出。1州なら1.0＝較正中立
````

## L942 T `public double WagePremium = 1.0;`
````
// 州の労働価格プレミアム（1人あたり月次所得の対国平均比・四半期EMA）。UpdateRegionalPriceLevels が算出・Territory.Opportunity が OpportunityWageWeight で参照＝金鉱州の高賃金→移民殺到（ゴールドラッシュの創発・キュー3）。1州なら比1→1.0＝較正中立
````

## L943 T `public Share FarmRentCap = new Share(1.0);`
````
// RGO地代（封建貢租の取り分）の上限（World から複製）。農奴解放=0.5/土地改革=0.3。元の取り分がこれ未満の国（自作農・小作）には効かない＝農奴制国だけ解放が効く。1.0＝無制限（中立）
````

## L944 T `public double CrowdingPremium = 0;`
````
// 政府借入による国債金利プレミアム（World から複製＝GovBondRatePerYear−基礎）。民間投資のハードルレートに上乗せ＝クラウディングアウト（金利チャネル）
````

## L945 T `public double FinancialHeat = 0;`
````
// 与信過熱ゲージ（World から複製・改修2026-06-28）。投資ハードルへ負で配線＝過熱→株高→投資ブーム→恐慌で暴落（株式市場のバブル動学）。較正(Credit無)では0＝中立
````

## L946 T `public double ProductivityBonus = 1.0;`
````
// 地域固有の生産性（辺境の資源賦存など）
````

## L947 T `public double ArableLand = 1e9;`
````
// 耕地賦存量（土地という固定生産要素）。農村人口がこれを超えると土地作物RGOが収穫逓減＝食料/人↓→所得↓→移民が止まる。未設定（合成較正・最小テスト世界）＝1e9＝制約なし
````

## L948 T `public bool CapitalEnabled;`
````
// 工業化度（集約資本）を有効にするか（§2.4。フランス等で true）
````

## L949 T `public double Industrialization;`
````
// 工業化度（地域集約の資本）。投資で積み、生産性と都市化上限を上げる
````

## L950 T `public int InvestorPopIndex = -1;`
````
// 工業化投資の出し手（資本家階級）。-1＝投資なし
````

## L951 T `public int MeritFromIndex = -1;`
````
// メリトクラシー昇格の出身階級（都市労働者）。-1＝昇格なし
````

## L952 T `public int MeritToIndex = -1;`
````
// メリトクラシー昇格の到達階級（専門職・技師）
````

## L953-953 W 直後 `public int ClergyPopIndex = -1;                   // 聖職者（宗教学校の教育を生産。世俗化で収入を失い没落）`
````
// --- サービス財（教育・医療・行政・労働力）の担い手 pop の**リスト添字**（-1＝この地域に不在）---
````

## L954 T `public int ClergyPopIndex = -1;`
````
// 聖職者（宗教学校の教育を生産。世俗化で収入を失い没落）
````

## L955 T `public int TeacherPopIndex = -1;`
````
// 教師（公教育・義務教育の教育を生産）
````

## L956 T `public int BureaucratPopIndex = -1;`
````
// 官僚（行政を生産。不足＝汚職で税収が漏れる）
````

## L957 T `public int DoctorPopIndex = -1;`
````
// 医者（医療を生産。人口扶養力に効く）
````

## L958 T `public int CriminalPopIndex = -1;`
````
// 犯罪者（(366) 治安と所有の安全 段0 の器）。⚠-1＝犯罪者 POP を持たない世界（CriminalClassEnabled=0・較正の小世界）＝除外の分岐（転職の行き先）は添字 ≧0 の時だけ働く＝厳密恒等。点火の世界では州の Pops の末尾（添字は構築の順で決まる）
````

## L959 T `public int ResearcherPopIndex = -1;`
````
// 研究者（高等教育を生産＝大学。段②の昇格先）。⚠-1＝研究者POPを持たない世界＝昇格先に数えない＝厳密恒等
````

## L960 T `public int SoldierPopIndex = -1;`
````
// 兵士（国家が雇う＝給与は EconomyStep が所得へ直接・(145) で兵力の市場を削除）
````

## L961 T `public int MerchantPopIndex = -1;`
````
// 商人（流通マージン＋貿易の仲立ちで稼ぎ、商船を買って海運容量を支える）
````

## L962-962 W 直後 `internal Pop? MerchantPop => MerchantPopIndex >= 0 && MerchantPopIndex < Pops.Count ? Pops`
````
/// <summary>商人の POP（添字が立っていて POP の並びの内にあるときだけ。無ければ null）。清算の商人への手数料・手形の払い手・引き受けの受け手・関税収入の受け手が同じ1本を読む。</summary>
````

## L964 T `public int ArtisanPopIndex = -1;`
````
// 職人（手工業の自営＝工場と並存レシピで競合。敗れると労働者へ転落＝ラッダイト）
````

## L965 T `public int ServantPopIndex = -1;`
````
// 家事使用人（家事奉仕を生産・女性がなりやすい。富裕層の余剰が雇う＝#12）
````

## L966 T `public int EducationProducerIndex = -1;`
````
// 教育の生産者の添字（教育法で担い手と生産性を切替）。★器 EduProducersCoexist>0 では [これ]=教区学校・[これ+1]=公立学校（連続 add＝2件目の添字は導出＝新フィールドを持たない）
````

## L967 T `public double AdminFill = 1.0;`
````
// 行政の充足率（買えた÷必要）。1未満＝汚職（税収が官僚の懐へ漏れる）。行政財の無い世界は1.0＝従来
````

## L968 T `public bool LastPortFrozen;`
````
// 診断：この四半期、海に出る口が凍っていたか（冬季閉鎖・2026-08-11）
````

## L969 T `public double LastTradeThrottle = 1.0;`
````
// 輸送律速率（容量÷潜在輸送量、1未満＝鉄道/船容量が交易を絞っている）。四半期の最初の清算が書く（2026-09-30 (130)＝月の清算は眠る行が数え漏らす）。読み手＝輸送の目標の混雑・港の拡張・貿易タブ
````

## L970-972 W 直後 `public double LastTradePotential;`
````
// ★潜在輸送量（輸送doc T0・2026-08-13）＝**絞る前に運びたかった重量**。LastTradeThrottle の分母そのもの。
// 旧＝比（throttle）しか残しておらず「どれだけ足りないか」が絶対量で測れなかった＝容量の目標を人口以外から導けなかった。
// 四半期の最初の清算（全ての行が起きている）が書く（2026-09-30 (130)）。読み手＝州の輸送の容量の取り分（Transport.ShareBasis）・輸送の目標（T4）。
````

## L974-975 W 直後 `public double LastTransportCap;`
````
// ★純テレメトリ（2026-08-26・作者指摘「輸送力が州割りになっている」の定量化）＝その州に配られた輸送容量の実値。
//   BilateralMarket が cap を組み立てた場所で書くだけ＝**計器が式を二重定義しない**。力学は一切読まない。
````

## L977 T `public bool CivilWarActive;`
````
// 内戦中か（World.InCivilWar の複製・#136）。内戦の経済麻痺（CivilWarEconomyDisruption）の読み口。false＝中立
````

## L978-978 W 直後 `public double CumPrivateIndustrialization;`
````
// 弧3-1（2026-07-24）：成長の出所＝資本家投資分の累積。純カウンタ・力学は読まない。★(608) 国家建設分の累積 CumStateIndustrialization は書き手が (599) で 0 本になり削除した
````

## L980 T `public double AdminBudget;`
````
// 今期の行政調達の予算（行政費スライダー×国庫。Simulation が設定）
````

## L981 T `public double InfraFill = 1.0;`
````
// 上下水道（衛生インフラ）の充足率（買えた÷必要）。衛生＝これ。低い＝過密の疫病（#4施設化＝旧「工業化proxy」を置換）。施設の無い世界は1.0＝従来
````

## L982 T `public double InfraBudget;`
````
// 今期の上下水道調達の予算（行政費スライダー×国庫×都市人口比。Simulation が設定）
````

## L983 T `public double SoapFill;`
````
// 石鹸の消費充足率（市民＋病院が買えた率）。衛生に上下水道とは別に加算（公衆衛生＝清潔。医療品ほどではない）。石鹸の無い世界は0＝従来
````

## L984 T `public double InsuranceCoverage;`
````
// 健康保険(#3)：国家が医薬品購入費を肩代わりする割合（社会保障×福祉充足×InsuranceCoverageMax）。Simulation が設定。popの実効医療費↓
````

## L985 T `public double MonopolyRentFactor = 1.0;`
````
// 独占禁止法（1934拡張E6）：独占レント(塩/茶)の実効倍率。規制なし=1.0／談合禁止=CartelBanRent／独占の解体=TrustbustRent。Simulation が国教法から設定。AntitrustGate=0で常に1.0＝恒等
````

## L986-988 W 直後 `public double WarScareTension;                    // 直近の対外最大緊張（0..200）。戦争の匂いが在庫目標を押し上げる`
````
// 【墓標2026-10-03 (189)】NavalRepairUnits（艦隊摩耗の修理需要＝沈んだ艦の補充を軍艦財の追加需要に）＝艦隊摩耗は艦の在庫（FleetUnits）を沈め、
//   海軍の調達（EconomyStep.FleetOrder＝目標の艦隊と在庫の差を埋める）が補充を自分で発注する＝同じ需要を2度立てない（設計書 §3-2「NavalRepairUnits／NavalRepairPerLoss は機構ごと撤去」）。
// 戦時備蓄＝B3自己成就期待の第1形（07_再設計 §5(a)・A5裁定・2026-07-31）。Simulation が四半期ごとに国の最大緊張を複製する。
````

## L989 T `public double WarScareTension;`
````
// 直近の対外最大緊張（0..200）。戦争の匂いが在庫目標を押し上げる
````

## L990-991 W 直後 `public double StockpileFactor(int goodId, Coefficients c)`
````
/// <summary>戦時備蓄の在庫目標倍率。緊張が閾値を超えるほど、まず主食と軍需を、次いで他の財を控えめに買い占める（A5裁定＝2層）。
/// ★自己成就＝需要スパイク→価格↑→隣国も備蓄（緊張は相互）。在庫が満ちれば需要が消える＝自己有界（既存 TargetStock 機構に乗るだけ）。</summary>
````

## L994 T `if (!(c.StockpilePanicWeight > 0) || !(WarScareTension > c.RivalTensionThreshold)) return `
````
// 否定形＝NaN は在庫目標を動かさない側へ倒す（有限値では旧 <= と厳密同値）
````

## L1000 T `public double ContrabandProductionFactor = 1.0;`
````
// 禁制品の国内生産の実効率（禁制品doc C3・2026-07-30）：全面禁止の州では取り締まれた分だけ産出が消え、残るのは闇栽培（Contraband.LeakFraction）。1.0＝禁止していない
````

## L1001 T `public int NationalizedMask;`
````
// 国有化部門（5部門＋『全て』＝§11-6d統合）のビットマスク（1934拡張E6）。Simulation が World.NationalizedMask() から設定。EconomyStep が財の部門と突き合わせ利潤を国庫へ。NationalizationGate=0で常に0＝恒等
````

## L1002-1005 W 直後 `public double SeizureChill;`
````
// 接収の代償＝資本逃避（作者裁定2026-08-10「押し切っても投資意欲↓」）。0..1。接収が成立した月だけ
// World.StepStateTargets が積み（Coefficients.SeizureInvestmentChill）、毎月 SeizureChillDecay で抜ける。
// 読み手は EconomyStep の私的投資予算 1本だけ（budget×(1−これ)）。NationalizationSeizureRate=0（★2026-08-29点火で既定0.05）では
// **書き手が一度も走らない**＝常に0＝厳密恒等。
````

## L1007 T `public double Mechanization;`
````
// 機械化度（no-labor-saving-tech-unemployment）：工業化度の飽和カーブ∈[0,1)。LaborSaveRate=0（対照腕）か旗 IndustrializationOff（既定 1）で0固定＝EffectiveQty の第2因子は1
````

## L1008 T `public double OldVintageCapital;`
````
// 旧世代の工業化資本（no-vintage-capital-depreciation）。★(613) 経年蓄積と座礁の書き手（技術水準を読む塊）は削除した＝書き手が無く常に0（工業化度の線が欄ごと畳む）
````

## L1009 T `public double DevelopmentPeak;`
````
// 開発度の到達最高値（新旧合計の走行最大・2026-07-27）。「体化された技術・熟練・制度」の記憶＝DevelopmentMemory>0 のとき物理減価を緩める。0＝未到達・DevelopmentMemory=0では未使用
````

## L1010 T `public double StrandedVintage;`
````
// 今月の座礁量。★(613) 書き手（技術差で座礁させる塊）は削除した＝常に0。生産性チャネルのみが差し引く
````

## L1011-1015 W 直後 `public double TotalIndustrialization => Industrialization + OldVintageCapital;`
````
// ヴィンテージ資本有効化2026-07-10（キュー5）：新旧二層の読者を分離。
//  ・TotalIndustrialization＝立っている資本の総量（新旧合計）。都市化天井・衛生proxy・スコア・投資上限・移民引力など「資本ストックの規模」を読む従来読者はこちら
//    ＝旧世代もIndDepreciationで同率減価するため、合計は旧単層Industrializationと同一動学（分割そのものは恒等＝較正中立）。
//  ・ProductiveIndustrialization＝生産性に寄与する分（座礁分を控除）。indMult・機械化度・投資の限界収益のみが読む＝座礁だけが現行との差分（意図した機構）。
//  ★(613) OldVintageCapital=StrandedVintage=0（書き手なし）で両者とも Industrialization に恒等。
````

## L1018 T `public double HousingFill = 1.0;`
````
// 住居役務の充足率（housing-urban-rent-absent）。移民摩擦に使用。住居財なし/HousingNeedPerUrban=0では1.0＝摩擦なし（従来）
````

## L1019 T `public double ElecInfraFill;`
````
// 電化インフラ（上位＝近代都市インフラ）の充足率。満足度＋衛生追加＋送電容量↑。便益は加算なので既定0＝施設の無い世界は便益なし（従来挙動）
````

## L1020 T `public double ElecInfraBudget;`
````
// 今期の電化インフラ調達の予算（上下水道と同型。Simulation が設定）
````

## L1021-1021 W 直後 `internal double Sanitation(Coefficients c) => PoliticalQuantities.Clamp01(InfraFill + c.So`
````
/// <summary>衛生＝上下水道の充足＋石鹸の消費＋電化インフラの上乗せを [0,1] に収めた値（人口の段・疫病の致死率・疫病の脆弱層が同じ1本を読む。EconomyStep の医療の需要の行だけは同じ式を手書きで持つ＝食料の台帳の線の範囲）。</summary>
````

## L1023-1025 W 直後 `public double MilitaryAffordRaw = 1.0;            // 装備の支払能力（軍事予算÷必要額・無クランプ）`
````
// ★軍の恒常性（2026-07-31・作者提案）：装備と給与の「必要額に対して払えた比率」を**1で切らずに**保持する。
// 既存の afford は Math.Min(1,…) で切っているため「余裕がある」が読めず、軍を増やすべきか減らすべきかの信号にならなかった。
// 生の比＝1超で余裕・1未満で不足。Military.Step がこれを見て軍を増減させる（目標を式で決めるのをやめる）。
````

## L1026 T `public double MilitaryAffordRaw = 1.0;`
````
// 装備の支払能力（軍事予算÷必要額・無クランプ）
````

## L1027 T `public Share GarrisonShare;`
````
// 駐屯州調達（課題6）：この州の装備需要・予算の全国シェア（月次・Simulationが設定）
````

## L1028 T `public double SoldierPayBudget;`
````
// 今期の兵力調達の予算（兵士給与。Simulation が設定）
````

## L1029 T `public double CompulsoryEduFunding;`
````
// 義務教育の財源率（教育費スライダー）。popが買えない教育の何割を国庫が買い与えるか。0＝義務教育でない
````

## L1030 T `public double EducationSpending;`
````
// ★(597) 国の教育費の実効の率（World.EducationSpending の月次の複製・Simulation が CompulsoryEduFunding と同じ場所で書く）。奨学金の法が最上段かを州の側で導く唯一の入力（EconomyStep.ScholarshipCover）。複製であって導出量の貯め込みではない
````

## L1031 T `public double GridCapacity;`
````
// 送電容量（地域内）。電力はこの容量までしか工場に届かない＝電化には送電網への投資が要る
````

## L1032 T `public double RailTrack;`
````
// ★(1097) 州の線路の在庫（容量の単位・段 R）。旗 RailByRegion＝1 のときだけ書く（州の投資家が買って積む・減耗と戦災・割譲で州について行く）。0＝旗 0 の世界
````

## L1033 T `public double LastElecWant;`
````
// 前期の電力需要（送電網の増設判断に使う）
````

## L1034 T `public double[]? StateMonopolyMargins;`
````
// 材ごとの専売の上乗せ率（World 参照）。★作者裁定2026-09-20＝0 か標準率の2値だけ（国ごとの率＝旧 Region.StateMonopoly は物理削除）。実効＝率×(1−StateMonopolyLeakMax×(1−AdminFill))（2026-09-30 (127)）
````

## L1035 T `public double FemaleParticipation = 0.4;`
````
// 女性の構造的労働参加率（need=0 の代表値・診断/UI用。倍率排除④：実効参加率は LaborSupply.FemaleParticipation が家計困窮を上乗せ）。賃金保持から導出
````

## L1036 T `public double WomenWageRetention;`
````
// 賃金保持（World から複製・連続0..1。倍率排除④）。女性参加・女性識字の駆動
````

## L1037 T `public double ChildLaborLegal = 1.0;`
````
// 児童労働の合法度（World から複製・連続0..1。倍率排除④）。子供の労働/就学の時間配分を律する（旧 bool ChildLaborAllowed を連続化）
````

## L1038 T `public double ColonialAccess = 1.0;`
````
// 植民地素材の入手率（植民地保有×制海＝封鎖で下がる §6.1）
````

## L1039 T `public int[] Resources = [];`
````
// この州の強み資源 good.Id（NationConfig の Provinces で指定＝産出が増産される財）。UIの地域表示に使う
````

## L1040 T `public string Culture = "", Religion = "";`
````
// 州の主流文化・宗教の自由文字列ラベル（漢/ウイグル/儒仏/イスラム…）。データ先行＝文化/宗教システム未実装でも保持（[[successor-culture-religion-vision]]）
````

## L1041 T `public int[]? ColonialResources;`
````
// この国の植民地が産する固有資源の good.Id（史実・1780）。null＝全植民地財（単一国テスト用の従来挙動）。空の配列＝多国の世界（★(410) ここから入る物は無い・印だけ）
````

## L1042 T `public double OpiumAddiction;`
````
// アヘン中毒度（1人あたり需要の上乗せ）。消費するほど増える＝供給が需要を生む正のフィードバック（実験）
````

## L1043 T `public double[]? ConstructionCarryover;`
````
// 経17（建設Leontief端数の繰越・good.Id添字・1段）：先月の建設束で律速成分に律速され使い残した非律速資材。null なら従来どおり全消費
````

## L1044 T `public double SocialSecurity;`
````
// 社会保障（公共医療）の水準（World から複製。医薬品消費に使う）
````

## L1045 T `public double HarvestFactor = 1.0;`
````
// 今年の豊作/不作（World から複製。主食RGO産出と自給に乗算）
````

## L1046 T `public double LastSubsistenceOutput;`
````
// 人2補完（2026-07-11）：この州の今月の主食**自給**産出Σ（市場外・無償＝Market.LastProducedに載らない）。扶養力の生産アンカーの分子欠落（自給7割の農業国で分子過小→capacity崩落）を埋める。EconomyStep月初にリセット
````

## L1047 T `public double[]? CropHealth;`
````
// 作物別の健全度（World.CropHealth を月次で複製＝参照）。病虫害で<1の作物は栽培州の産出が落ちる（作物固有の不作）。null=従来（健全）
````

## L1048 T `public double WeatherBandFactor = 1.0;`
````
// 緯度帯（気候帯）の天候係数（World から複製）。HarvestFactorと積算＝同じ気候帯の国/州が一緒に不作（欧州一帯の凶作）。1.0=中立
````

## L1049 T `public double FoodFill = 1.0;`
````
// 州ごとの主食充足（消費ベース∈[0,1]・平滑）。書き手は Simulation.SmoothRegionalFoodFill のみ（★2026-09-30 (131) 旧 RegionalFamineStep の局地飢饉の死は削除・平滑だけ残した。旧注記＝RegionalFamineMortality=0 でも常時更新＝死亡計算の早期returnより前）。読み手＝移民の飢饉push（MultiNationSim）・州反乱（Politics）・派閥crisis（Factions）。1州なら World.FoodFill と一致＝較正中立
````

## L1050-1052 W 直後 `public double MineralDepositInitial = -1.0;   // 初期鉱脈（初年率×DepositYearsAtBirth）＝品位曲線の分母`
````
// 【削除 2026-10-01 (148)】MineralDepletion（抽選の鉱脈の発見の富鉱倍率×年15%の戻り・確率の枯渇）と SilverRushed（シルバーラッシュの再発止め）
//   ＝産地の発見（Catalog.Sites・Producer.SiteFound）と採掘による品位（下の鉱脈在庫）の1本へ（検討 §10-3 項目10(c)＝状態が2本減った）。
// ★鉱脈在庫（2026-08-14・(148) で旗 DepletionFromExtraction を焼き込み）。-1＝未捕捉（助走明けの最初の産出月に初期化）
````

## L1053 T `public double MineralDepositInitial = -1.0;`
````
// 初期鉱脈（初年率×DepositYearsAtBirth）＝品位曲線の分母
````

## L1054 T `public double MineralDeposit = -1.0;`
````
// 残量。毎月、鉱物産出ぶんだけ減る（決定論＝採掘量の関数）
````

## L1055 T `public double MineralGradeFactor = 1.0;`
````
// 品位（残量から導出・EconomyStep の鉱物の能力と MineralSelfCost が読む）。既定1.0
````

## L1056 T `public int CulturalGoodId = -1;`
````
// 各国の象徴的主食（英＝茶・露＝蒸留酒・日＝米…）の good.Id。**表示用のみ**（「パンの値段」UI/診断の価格アンカー）。固執財の需要床/闘争度機構は廃止。-1＝なし
````

## L1057 T `public bool[]? Discovered;`
````
// ★発見の表の解禁（段2A・2026-10-01）＝国の World.Discovered と同じ配列への参照（月次に TechLevel と一緒に写す・複製しない）。null＝まだ写していない＝表の行は全て未発見
````

## L1058 T `public double MilitaryScalePeace;`
````
// ★段2a：戦時倍率を除いた平時基準の規模（耐久品の目標在庫はこちら）
````

## L1059 T `public double MilitaryWarMultLast;`
````
// ★段2a：その月の戦時倍率（耐久品の摩耗へ回す）
````

## L1060 T `public double MilitaryScale;`
````
// 軍需消費の規模（治安維持力×戦時倍率。Simulation が設定）
````

## L1061 T `public double MilitaryBudget;`
````
// 陸の装備の調達に充てられる国庫（Simulation が設定）。★(189) 艦は含まない（艦は下の NavalBudget）
````

## L1062-1065 W 直後 `public double NavalBudget;`
````
// ★海軍の予算（2026-10-03 (189)・作者の裁定 2026-08-01「海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」・2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、
//   軍事費を分割するようにしよう」）＝軍事費の器から給与を払った残り（装備の器）×海の金の割合（World.NavalSpendShare＝海軍維持費÷陸海の維持費＝AI とプレイヤーの同じ法の値）を、
//   海軍の調達の必要額（EconomyStep.FleetOrder の必要額＝目標の艦隊と在庫の差）で切った額。Simulation が月初に書き（国家の支払い率 r を掛ける）、EconomyStep の海軍の調達が読む。
//   月ごとに作り直す作業の値（陸の MilitaryBudget と同じ型）＝旧 ShapeNavalShare（陸海の再配分の比・軍需を兵の頭数×海の割合で買った）を置き換えた（状態の数は同じ）。
````

## L1067 T `public double MilitaryBudgetCeiling;`
````
// 軍需予算の**上限**＝歳入シェア側の額（必要額で切る前。Simulation が設定）。恒常性の信号（MilitaryAffordRaw）はこちらから取る＝
````

## L1068-1069 W 直後 `public double MilitaryEquipFill = 1.0;            // 装備充足率＝軍需財を必要量のうち買えた割合（予算×在庫充足）。1未満＝武器`
````
// S3a で実支出を必要額に切っても「まだ払う余裕がある」が消えない。Simulation を経ない経路（テスト）は Budget と同時に設定すること（Budget ≤ Ceiling が不変条件・2026-09-11）
// AtWar（戦時の建設投資抑制フラグ）＝機構は倍率排除バッチ2で撤去済み（軍需の価格競合が代替）・読み手ゼロにつきフィールドごと削除（監査2026-07-12 死37）
````

## L1070 T `public double MilitaryEquipFill = 1.0;`
````
// 装備充足率＝軍需財を必要量のうち買えた割合（予算×在庫充足）。1未満＝武器不足で兵力が下がる（即応度に乗算）
````

## L1071-1082 W 直後 `public Dictionary<int, double>? ArmsStock;`
````
// ★耐久装備のストック（段2a・2026-08-13・doc 20260731/20260801_理想の軍隊の形 §2-9）。
//
// 旧＝上位段（機関銃・装甲艦）とtier3（戦車・弩級戦艦）も**毎期のフロー**として発注される。
// 戦時は MilitaryScale が MilitaryWarMult=9.0 倍になるのに予算は 3.8 倍しか追随しない（⚠当時＝2026-08-13 の記録。WartimeBudgetFollowsNeed は 2026-10-02 (155) で削除＝今の戦時の予算は軍事費の器のまま〔戦時経済の段で器が広がる〕・(157) の注記・(162) で装備の部分だけ戻し (163) の合流で差し戻した）
// （WartimeBudgetFollowsNeed=0.35／1.0は既に硬性赤＝戦費が経済を焼き尽くす）ので予算充足が0.42倍へ落ち、
// 段別コスト比63%の基本段で使い切って**戦争中は上位段もtier3も厳密に0**になる（実測2026-08-13）。
// 塹壕も戦車も戦争中にしか読まれないので、平時に満額でも戦場では消えていた。
//
// 新＝**機関銃は9倍壊れない**。耐久品はストックを持ち、発注は不足分だけ、戦時は**摩耗が増える**（需要でなく decay へ）。
// 弾薬・馬（消耗品）は現行のフローのまま＝1ビットも変えない。基本段もフローのまま残す＝それがカスケードの
// 優先順位を担っている（一律按分に変えたら破綻1/10で差し戻した実測がある）。
// キーは財ID。ArmsStockEnabled=0 では1件も作られない＝厳密恒等。
````

## L1085-1085 W 直後 `public double MilitaryQualityFill = 0.0;          // 上位軍需（機関銃・装甲艦）の充足率＝戦力の質的乗数（Power.Milit`
````
// ★陸分の充足率（2026-08-13）は (189) で上位段・tier3 の決済から艦が外れた時点で合算の Fill と同じ値になった＝別の欄は 2026-10-23 に消した（読み手は Fill を読む）。
````

## L1086 T `public double MilitaryQualityFill = 0.0;`
````
// 上位軍需（機関銃・装甲艦）の充足率＝戦力の質的乗数（Power.Military を 1+Bonus×これ で増幅。基本装備を整えた残余予算で届く＝富裕/軍国国の強み）
````

## L1087 T `public RegionTier Tier;`
````
// ★州の格（植民地のラベル化・2026-08-01）。Colonial は参政権・技術伝播・維持費・割譲到達が読む（Core/Integrated は同じ扱い）
````

## L1088-1090 W 直後 `public RegionTier? TierInParent;`
````
// ★分離の時点の親の下の格（2026-10-03 (183)・州の格の不変条件＝「植民地格だった州は親へ戻れば植民地格」）。Territory.Rehome が書いて読む＝建国（Seceded）で今の格を写し、
//   再併合（Returned）でこの格へ戻して消す・割譲（Ceded）で消す・独立の確定（Sovereign）は残す（格は本土になるが、分断の残骸は親へ平和的に戻りうる）。null＝分離していない。
//   ⚠新しい状態（思想⑩）＝独立の確定が格を本土へ上書きする＝盤面から導けない（分断の固定〔内戦の白紙〕→残骸の自然再併合で、英の植民地格の州が本土の格のまま戻った＝(180) の反証役の指摘）。
````

## L1092 T `public Terrain Terrain;`
````
// ★地形タグ（(3)・2026-08-28）。既定 Plain。出所＝places.json の terrain 列（構築時に1回引く）。係数全1.0の間は読み手がいても恒等
````

## L1093 T `public bool Malarial;`
````
// ★マラリア地（同上）。旧 Colony.Malarial の移設先。植民地維持費（Colonial.AccumUpkeep）がキニーネ需要に読む
````

## L1094-1095 W 直後 `public double Dominance = 1.0;                    // ★E-06 支配度（ストック・0..1・2026-09-05）：Polit`
````
// 【墓標2026-09-26】HeldSince（州の保有開始年）＝回1-1で削除（思想⑩・外部レビューの検討 §10-10［植民地8］）。書き手（構築と植民地の州化）だけで読み手が無かった。
//   （2026-11-08 (956)）heldSince のデータも削除＝構築時の格は Core・支配度は 1.0 の直書き。移転の付け直しは Territory.Rehome（割譲は支配度を動かさない・独立の確定で1）。
````

## L1096 T `public double Dominance = 1.0;`
````
// ★E-06 支配度（ストック・0..1・2026-09-05）：Politics.CheckSecession が毎四半期 (引き留め−離脱圧力)×DominanceSpeed で積分。初期値＝保有年数÷DominanceMatureYears。DominanceIsStock>0 のとき 0 で独立
````

## L1097-1098 W 直後 `public double[] Rule = [1.0, 0.0, 0.0, 0.0];`
````
// ★支配度の一般化（作者採用2026-09-06・段1＝器と観測のみ）：州の4区分（0=国家・1=在地権力・2=外国・3=無統治・合計1・保存対象）。
//   初期値＝州データ（ProvinceSpec.Rule）→ 国既定（NationConfig.Rule）→ 無ければ 国家1.0（現行と同じ挙動）。(399) で点火：Politics.CheckSecession が毎四半期 DominanceSpeed で目標へ寄せる。
````

## L1100 T `public double[] RuleInit = [1.0, 0.0, 0.0, 0.0];`
````
// 構築時の写し（在地の目標＝RuleInit[1]×仲介者の権利の残高 Privileges.IntermediaryDegree）
````

## L1101-1104 W 直後 `public double[] RuleStateYears = [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1];`
````
// ★支配度 段5（§6/§6d・2026-09-07 器）：外国区分の「誰の」持分＝主体つきの並列配列（鍵＝Power.BaseName＝不変・辞書は使わない＝セーブ形式）。
//   合計が Rule[2]（外国）の目標。書き手＝講和の条約港の付与（Diplomacy）。段8で Dependency/割譲/植民地をこの読み出しへ畳む。
// ★支配度 §7（州パネル・2026-09-07）：国家区分の年次リング（11本＝今年を書いた後の最古が**ちょうど10年前**。10本だと9年前になる＝レビュー是正）。
//   Politics.CheckSecession が書く。負値＝未記録。
````

## L1106 T `public int RuleStateHead;`
````
// 次に書く位置（リング）
````

## L1107 T `public int RuleStateLastYear = -1;`
````
// 最後に書いた年（年に1回）
````

## L1108-1108 W 直後 `public double RuleStateTenYearsAgo()`
````
/// <summary>★支配度 §7：国家区分の「10年前」＝年次リングの最古の値（11本埋まる前は −1＝帯を描かない）。</summary>
````

## L1112 T `double v = RuleStateYears[RuleStateHead % RuleStateYears.Length];`
````
// 次に上書きされる位置＝最古
````

## L1117-1120 W 直後 `public double[]? FleetUnits;`
````
// ★艦の在庫（2026-10-03 (189)・作者の裁定 2026-08-01「陸軍は兵士ありきですが、海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」）＝
//   戦闘艦の財ごとの隻数（Coefficients.FleetGoodIds と同じ並び＝軍艦・装甲艦・弩級戦艦）。買えた艦（EconomyStep の海軍の調達）を積み、財の損耗の率（財の表の WearRatePerMonth・読み口は Catalog.DurabilityOf）で毎月減り、交戦の海で沈む（Diplomacy の艦隊摩耗）。
//   null＝まだ艦を持ったことがない（0隻）。Diplomacy が艦の強さ（段の効き＝建造費×段の乗数）と乗員（隻数×1隻の乗員）を毎月ここから導く。
//   ⚠旧 FleetStock（建造費で重みを付けた1本の和）を置き換えた＝段の効き（強さ）と隻数（乗員）は1本の和からは導けない（思想⑩：本当に要る分だけ＝財ごとの隻数）。
````

## L1122-1123 W 直後 `public double[]? NavalOrder;`
````
// ★(606) 線 navf：この州の今月の艦の発注（財ごとの隻数＝<see cref="Coefficients.FleetGoodIds"/> の並び）＝国の発注×この州の港の割合。Simulation が毎月書く複製（旗 NavalPortProcurement は (824) で焼き込み）
//   （GarrisonShare・NationHasPort と同じ型＝積み上げる状態ではない）・読み手は EconomyStep の艦の発注と決済だけ。null＝この州は今月艦を買わない。旗 0 では常に null。
````

## L1125 T `public double MilitaryTier3Fill = 0.0;`
````
// #16 tier3軍需(戦車・弩級戦艦)の充足率＝戦力の最上位乗数(Power.Military を +MilitaryTier3Bonus×これ)。更なる残余予算で届く＝富裕/軍国/晩期のみ
````

## L1126 T `public double MilitaryAirFill = 0.0;`
````
// 1934拡張E2：空軍装備（飛行機・戦闘機・化学兵器）の充足率＝Power.AirPower=Military×これ。財の解禁(1910s)前は0＝空の概念ごと不走行
````

## L1127-1129 W 直後 `public double[] StateSpent = new double[(int)TreasuryCat.Count];`
````
// ★今月の未精算の国家の支払いを**区分ごと**に（添字＝TreasuryCat・正＝支出・負＝収入＝専売益・国営持分の利潤）。Simulation が月次に区分ごとに国庫へ精算する
//   （2026-09-28 支払いの一元化 段2-1 D1〜D3＝旧は1本の double で精算の区分が全て OtherOut に潰れ、専売益だけを別の欄 LastStateMonopolyRevenue に二重に積んで割り戻していた）。
//   精算の後も次の EconomyStep の冒頭（Array.Clear）までは先月の値として残る＝月初の国家の支払いの請求の見込み（Simulation）と Fiscal の予算基数（専売益）が読む。
````

## L1131 T `public double CumEducationCost;`
````
// 義務教育と奨学金（旗 ScholarshipTopStage が正で最上段のとき）の国庫負担の累積（診断・UI予算表示用。リセットせず差分で期間値）
````

## L1132 T `public double CumStateMonopolyRevenue;`
````
// 専売益の累積（診断・UI予算表示用。同上）
````

## L1133-1138 W 直後 `public double HigherEduFunding;`
````
// （株式会社の州の倍率 IndustrializationBoost と、その適用済みの印 JointStockApplied・JointStockMulApplied は 2026-10-01 (152) に削除
//   ＝所有国の発見の2値〔World.JointStock〕から導ける量＝EconomyStep が所有国の値を直に読む・割譲された州は次の月から新しい所有国の制度に従う）
// ★(608) 恒等の線 gc7：欄 StateBuildQueue〔国庫の建設の予約額・書き手は (583) から 0 本〕を物理削除した（名前の復活は DormantIdentityTests.RetiredFields が禁じる）。残る予約は下の MonumentBuildQueue・HigherEduFunding。
// ★段③（2026-08-17・作者裁定「工業化投資は銅やセメントを買うのではなく、研究者や技師を増やす補助金として機能させたい」）：
//   近代化投資のうち高等教育の学費を肩代わりする分の**予約額**（MonumentBuildQueue と同型＝請求権であってカネはまだ国庫にある）。
//   ⚠実支払いは EconomyStep が**買えた分だけ** StateSpent へ計上＝空振りは自動的に国庫へ残る（作者裁定2026-08-16の「返金」はこの型で既に満たされる）。
````

## L1145-1148 W 直後 `[System.NonSerialized] internal Producer[]? BuildSites;`
````
// ★起きている物だけ回す（2026-09-30 作者裁定「計算量の予算」・恒等）＝州の作業領域の一覧。状態ではない＝セーブに載らない（[NonSerialized]＝ロード後は null から作り直す）。
//   起こすのは理由＝書き込みの印（Producer.ConstructionLeft／BuiltScale・Market.LastDemand／LastProduced の setter が「述語が変わる書き込み」で印を立てる）。タイマーは無い。
//   一覧は州に付く＝州が国をまたいでも（Territory.TransferRegion）一覧ごと移る（国の側で作り直す必要が無い）。
/// <summary>工事中の一覧＝!(ConstructionLeft≦0) の生産者を州の生産者の順に（World.StepConstruction が読む）。</summary>
````

## L1150-1151 W 直後 `[System.NonSerialized] internal Producer[]? StateSites;`
````
/// <summary>国営持分の請求の一覧＝工事中か BuiltScale&gt;1e-9 の生産者を州の生産者の順に（月初の国家の支払いの請求＝Simulation が読む）。
/// それ以外の生産者は請求に 0.0×赤字（有限）を足すだけ＝飛ばしても和は1ビットも変わらない（国有化の枝が生きる州は全件へ戻す＝呼び手）。</summary>
````

## L1154-1155 W 直後 `[System.NonSerialized] internal long GdpVersion;`
````
/// <summary>★産出の版（2026-09-30 (136) 計算量の予算・恒等）＝この州の生産者の <see cref="Producer.Output"/> が書かれた回数（宛先を付けた生産者だけ・進むだけで戻らない）。
/// 読むのは <see cref="World.GdpPerMonth"/> の使い回しの判定だけ（版が同じ＝前に和を取った後に産出が1つも書かれていない）。作業領域＝セーブに載らない。</summary>
````

## L1157-1157 W 直後 `internal void EnsureSites()`
````
/// <summary>印が立っていれば（または未作成なら）2つの一覧を作り直す。作るときに生産者へ宛先（この州）を付ける。</summary>
````

## L1181-1181 W 直後 `[System.NonSerialized] internal Good[]? LiveGoods;`
````
/// <summary>死んでいない財の一覧＝Goods の順のうち「需要も産出も 1e-9 未満」でない財（MarketFlow.StepDay が読む＝死んだ財は今も continue で動かさない＝回さないだけ）。</summary>
````

## L1184-1184 W 直後 `[System.NonSerialized] internal int ClearSellerPos;`
````
/// <summary>★(403) 係数 <c>ClearingPartnerScope</c> の作業領域（[NonSerialized]）：この財の清算で、この州が売り手の何番目か（<see cref="ClearSellerStamp"/> が今の財の通し番号と同じときだけ有効）。</summary>
````

## L1187-1190 W 直後 `internal Good[] EnsureLiveGoods()`
````
/// <summary>印が立っていれば（または未作成なら）作り直す。作るときに市場へ宛先（この州）を付ける。</summary>
/// ★(spt2) 作り直しは 1 周（スレッドごとの作業配列へ書く）・中身が前の一覧と同じなら前の配列をそのまま返す（新しい配列を作らない）・市場への宛先は違うときだけ書く。
/// 返す一覧の中身と順は旧と同じ（Goods の順のうち死んでいない財）。呼び手（MarketFlow.StepDay・テスト）は一覧を書き換えず、呼びの間だけ読む＝同じ配列を返してよい
/// （印が立っていない月はもともと同じ配列を返していた）。旧＝作り直すたびに 2 周して新しい配列を作った（州×月に 1 回・StepDay の約 1 割＝measurements/spt2_20261026_identity.txt）。
````

## L1215 T `[System.ThreadStatic] private static Good[]? _liveBuf;`
````
// ★(spt2) 作り直しの作業配列（スレッドごと・セーブに載らない静的な欄）
````

## L1217-1217 W 直後 `private Good?[]? _goodById;`
````
// 財Id→Good の直引き（遅延構築）。Goods は構築後に増減しない＝安全。線形探索 Goods.Find(x=>x.Id==id) の O(財数) を O(1) に（Gdp等のホットパス高速化）。
````

## L1253-1256 W 直後 `public double SumRuralPopulation()`
````
/// <summary>★州の農村人口＝都市でない POP の規模の和（兵士を含む＝都市率 <see cref="SumUrbanPopulation"/> の補数と同じ集合）。2026-09-30 処理の一元化 段4e H。
/// 読み手＝土地の逓減（<see cref="Territory.LandFactor"/>）の入力を持つ全ての所＝機会・残る値打ち・受け入れの上限・輸出代金の地代・人口動態の空き土地。
/// ⚠旧＝同じ和を5か所で手書きし、人口動態の空き土地だけが兵士を除いた（「landFactor と同一入力」の注記と食い違う）。EconomyStep の土地の逓減は
/// 月初の1回の走査で都市・農村・総人口を同時に数える（同じ集合）。</summary>
````

## L1264-1266 W 直後 `public Pop? PopOf(int classId)`
````
/// <summary>★移る先の同じ階級の POP の探し方の1本（2026-09-30 処理の一元化 段4e I）＝その階級の兵士でない**最初の** POP（無ければ null）。
/// 旧＝4通り（Find に !IsSoldier・foreach で最初の一致・foreach を最後まで回して最後の一致・Find で最初の一致）＝同じ階級の POP が複数ある州で
/// 経路ごとに別の POP を選んだ（州を作る経路は階級ごとに1つ＝今は同じ POP）。</summary>
````


## Region.WarScareTension・Region.StockpileFactor（(1185) c118hc で削除）
- 戦時備蓄の欄と式。(1184) で係数が 0 になり常に 1.0 を返していたため削除（恒等）。旧の式は docs/history/Coefficients.p07_wiring_prewarm.cs.md の StockpilePanicWeight の節に写した。
