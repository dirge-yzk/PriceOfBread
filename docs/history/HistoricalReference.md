# PriceOfBread.SimCore/HistoricalReference.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-10 W 直後 `public sealed record HistoricalEntry(`
````
/// <summary>
/// 史実参照表（1914年）＝新較正基準（ユーザー最高方針 2026-07-09）の一次データ。
/// 基準：各国が**約7割のシードで史実（または近い）政体・主要法**に到達する（100%は「史実に固着＝カオス不足」・5割を大きく割るのは「逸脱過多」）。
/// Exact＝史実そのもの（遷移期の国は複数可＝清の1912共和など）／Near＝許容隣接（近い姿。near一致＝Exact∪Near）。
/// 主要法＝史実が明確な国のみ2-4軸（許容集合）。人間可読の根拠は docs/史実参照表_1914.md（本表と1:1対応）。
/// 照合キーは Nation（NationConfig.Name＝Power.BaseName・構築時国名で変名しても不変）＝首都州の割譲・遷都・統一変名でも追える。
/// 能力の物理（距離・投射・経済基盤）は逸脱の下限として別途維持（モンゴル則）＝本表は「上限側」の物差しではなく忠実度の物差し。
/// </summary>
````

## L12 T `string Nation,`
````
// world.json の国名（NationConfig.Name）
````

## L13 T `string Capital,`
````
// 首都州名（Regions[0]＝standings 表示名。参照表の可読性用）
````

## L14 T `Polity[] Exact,`
````
// 史実の1914年政体
````

## L15 T `Polity[] Near,`
````
// 近い政体（Exact に追加で near 一致）
````

## L16 T `Suffrage[]? SuffrageOk = null,`
````
// 参政権の許容集合（史実が明確な国のみ）
````

## L17 T `EducationLaw[]? EducationOk = null,`
````
// 教育法の許容集合
````

## L18 T `ReformLevel[]? SerfOk = null,`
````
// 農奴解放の許容集合
````

## L19 T `ReformLevel[]? PensionOk = null,`
````
// 年金の許容集合（ビスマルク型の先進国のみ）
````

## L20 T `ReformLevel[]? CornLawsOk = null,`
````
// 穀物法（保護）の許容集合（英の1846撤廃など）
````

## L21 T `ReformLevel[]? LandReformOk = null,`
````
// 土地改革の許容集合（★2026-08-02追加＝それまで忠実度に1ビットも寄与しない死んだ軸だった）
````

## L22 T `bool? MilitarismOk = null,`
````
// 軍国（プロイセン軍国主義など）。★2026-08-18：親軍法（bool）の撤去に伴い、照合対象は法の有無から
````

## L23-24 W 直後 `bool SubjugatedOk = false,          // 主権軸（裁定2・2026-07-10）：史実1914が併合/保護国/属国＝従属（宗主あり）で終局すれば`
````
//   **導出量『軍の政治的優位』（軍の圧力×(1−文民制度の弁)）が閾値 MilitarismDegreeOk 以上か**へ変わった（軸数＝分母は不変）。
//   ⚠この物差しなら英1913（国防費が中央歳出の43%）でも文民制度が厚いので**偽**になる＝金額では測らない
````

## L25 T `bool SubjugatedOk = false,`
````
// 主権軸（裁定2・2026-07-10）：史実1914が併合/保護国/属国＝従属（宗主あり）で終局すれば政体に関わらず一致扱い（英領インド・仏領インドシナ・露領中央アジア・独伊統一の小邦併合など）
````

## L26 T `string[]? SuzerainsOk = null,`
````
// 宗主同一性（監査2026-07-12 リ12・裁定「解決法は任せる」）：史実の宗主（Power.BaseName＝変名しても不変）の許容集合。**exact＝従属かつ宗主が集合内／near＝任意の宗主に従属**の2段判定＝「誰が奪ったか」（勢力圏の地理）を測る。null＝宗主不問（旧挙動＝従属だけでexact）
````

## L27 T `string Note = "",`
````
// 一行根拠（詳細は docs/史実参照表_1914.md）
````

## L28-31 W 直後 `(double Lo, double Hi)? PopGrowthOk = null,`
````
// B-中7（史実検証2026-07-22）：1783→1914の人口成長倍率の許容帯。null＝未定義＝測定対象外。
// **領土不変な定義**を採る（本ゲームは国を消さず併合も起きるので絶対人口の±%は定義が噛み合わない）。
// exact/near とは**合算しない独立の指標**（合算すると過去測定 exact41%/near73% との比較可能性が失われる）。
// 領土獲得・移民を含む国（普×5.3・露×4.5・米×24）は純自然増として過大なので定義せず、領土が概ね安定した国のみ定義する。
````

## L33-37 W 直後 `bool? GreatPowerOk = null,`
````
// 列強軸（2026-07-27）：1914年に「列強（スコア上位 GreatPowerCount）」であるべきか。null＝測定対象外。
// **人口軸と同じく exact/near とは合算しない独立の指標**（合算すると過去測定との比較可能性が失われる）。
// 追加の理由＝実測で 1783年の列強7のうち1914年も列強なのは仏だけ（英は49位・工業化0.00・属国化経由／露は53位＝最下位）だったが、
// 「1914年に誰が列強か」は政体軸でも法軸でも人口軸でも測られておらず、この歪みがどの帯にも露出していなかった。
// 史実1914の列強は英仏独露墺伊米日の8カ国＝定員7に対し1つ多い＝各国とも100%にはならない（◎7割帯の思想と整合）。
````

## L39-41 W 直後 `int? GdpRank1913 = null,          // 経済規模：1913年の世界GDP順位（Maddison推計）。**順位ベース**＝単位系に頑健。一致＝±G`
````
// ===== 08_バランス調整方針（2026-07-31）＝70%計器の6次元化。政体・法・人口・列強に**経済規模**と**領土**を足す。=====
// 各次元とも「厳格一致50%」が目標（厳格50%≒体感7割＝現行計器の実測で exact≈near×0.5 が安定しているため）。
// ★null＝その国は当該次元の測定対象外（人口軸・列強軸と同じ作法＝史実値が明確な国だけ定義する）。
````

## L42 T `int? GdpRank1913 = null,`
````
// 経済規模：1913年の世界GDP順位（Maddison推計）。**順位ベース**＝単位系に頑健。一致＝±GdpRankTolerance位
````

## L43 T `int? HomeProvinces1914 = null,`
````
// 領土（本国）：1914年に保有していた本国州の数（world.json の州帰属で数えた値）。一致＝現保有がこの HomeProvinceMatchFrac 以上
````

## L45-46 W 直後 `Polity[]? Exact1820 = null, Polity[]? Exact1870 = null, int? GdpRank1820 = null, int? GdpR`
````
// ★E-08 多断面（作者提案2026-09-04）：Maddison の基準年 1820／1870 の政体（許容集合）と世界GDP順位。null＝その断面では採点しない。
//   主指標（◎7割帯・exact/near）は1913のまま＝既存基線を壊さない。これらは併記の列。
````

## L48 T `Polity[]? Exact1920 = null,`
````
// ★E-09 WW1の帰結＝1920年12月の政体（帝政4つの消滅・新共和国・ソヴィエト） // 領土（植民地）：1914年の植民地の数。★**場所でなく規模で測る**（作者裁定）＝±ColonyCountTolerance の相対幅
````

## L49-52 W 直後 `Polity[]? Exact1950 = null, Polity[]? Near1950 = null, string[]? SuzerainsOk1950 = null);`
````
// ★1950断面（作者裁定2026-09-21・Q668＝1950年1月1日＝終局）：政体の exact/near と従属の許容。照合の規則は主指標（1914）と同じ1本（PolityMatch）。
//   SuzerainsOk1950＝null なら従属は数えない／非null なら「従属かつ宗主が集合内」を exact・「任意の宗主に従属」を near（主指標の SubjugatedOk＋SuzerainsOk と同型）。
//   ＝空集合 [] は「従属を near だけで数える」（exact には数えない）＝1950年には正式な従属ではなかった国の置き方（オマーン・インド系5カ国・2026-09-23）。
//   1950年に主権を持たない国（併合・植民地・分割）は主指標と同じく「その地の1950年の統治形」を Exact に置く（例：伊の旧諸邦＝伊共和国の共和制）。
````

## L55-55 W 直後 `public sealed record NationFidelity(`
````
/// <summary>1国の史実忠実度（バッチ集計）。Exact/Near＝完走シードに占める一致率。LawMatch＝定義済み法軸×シードの一致率（軸なしは NaN）。</summary>
````

## L58 T `double PopMatch = double.NaN, double PopGrowthMean = 0,`
````
// B-中7：人口成長倍率の一致率と実測平均。NaN＝この国は人口帯が未定義（測定対象外）
````

## L59 T `double GreatMatch = double.NaN, double RankMean = 0,`
````
// 列強軸（2026-07-27）：1914年に列強だった率と平均順位。NaN＝未定義
````

## L60-63 W 直後 `bool GreatExpected = false,          // この国は史実で1914年に列強だったか（＝再現率側か特異度側か）`
````
// ★★列強軸を2つに割る（2026-08-01・ユーザー推薦）。⚠混ぜた集計値は**情報を持たない**——
//   「誰も列強にならない」と常に答える自明な予測器が 47/55＝85.5% を取り、実測80.9%はそれを4.6pt下回っていた。
//   46:8 の不均衡のせいで高く見えていただけである。★仕様「上位8と史実8の一致が5カ国以上」は**再現率**の
//   閾値表現（5/8＝62.5%）、実装「国別 rank<=7」は再現率と特異度を混ぜたもの＝両方出せばどちらへ寄せる裁定も不要になる。
````

## L64 T `bool GreatExpected = false,`
````
// この国は史実で1914年に列強だったか（＝再現率側か特異度側か）
````

## L65 T `double GreatHit = double.NaN,`
````
// その国が「あるべき姿」に到達した率（列強のはずなら列強になった率／非列強のはずなら列強にならなかった率）
````

## L66-66 W 直後 `double EconMatch = double.NaN, double GdpRankMean = 0,`
````
// 08の70%計器（2026-07-31）：経済規模・領土（本国/植民地）の3次元。いずれも NaN＝測定対象外
````

## L70 T `double TradeMatch = double.NaN, double TradeMean = 0,`
````
// ★貿易開放度（2026-08-15・作者承認）：(輸出+輸入)/GDPが史実1913年値の±50%帯に入った率・実測平均
````

## L71 T `double LabelExact = double.NaN,`
````
// ★旧ラベル基準の政体exact（2026-08-21・物差し切替の橋＝導出政体への切替後も過去測定と比べられる併走値）
````

## L73-73 W 直後 `double Exact1820 = double.NaN, double Exact1870 = double.NaN, double Econ1820 = double.NaN`
````
// ★E-08 多断面：1820／1870 断面の政体一致率とGDP順位一致率（NaN＝未定義／断面なし）
````

## L75 T `double Exact1920 = double.NaN,`
````
// ★E-09   // 案Y（2026-08-21）：旧定義（総人口比＝征服インフレ込み）の平均倍率＝人口物差し切替の橋（判定には使わない・診断印字のみ）
````

## L76 T `double Exact1950 = double.NaN, double Near1950 = double.NaN);`
````
// ★1950断面（2026-09-21）：政体の exact/near 一致率（NaN＝断面なし）
````

## L80-80 W 直後 `private const double FixationNearThreshold = 0.90;   // >90%＝史実に固着（カオス不足）`
````
// マジックナンバー命名（挙動不変・値据え置き）：忠実度の判定帯。
````

## L81 T `private const double FixationNearThreshold = 0.90;`
````
// >90%＝史実に固着（カオス不足）
````

## L82 T `private const double DeviationNearThreshold = 0.50;`
````
// <50%＝逸脱過多
````

## L83 T `private const double IdealBandLow = 0.55;`
````
// ◎7割帯の下限
````

## L84 T `private const double IdealBandHigh = 0.85;`
````
// ◎7割帯の上限
````

## L85-85 W 直後 `public const int GdpRankTolerance = 3;               // 経済規模：史実のGDP世界順位±これ以内で一致`
````
// 08_バランス調整方針（2026-07-31）＝6次元の一致定義（実装初期値・調整可）
````

## L86 T `public const int GdpRankTolerance = 3;`
````
// 経済規模：史実のGDP世界順位±これ以内で一致
````

## L87 T `public const double HomeProvinceMatchFrac = 0.7;`
````
// 領土（本国）：史実1914の保有州のうちこの割合以上を保有していれば一致
````

## L88 T `public const double ColonyCountTolerance = 0.5;`
````
// 領土（植民地）：史実の植民地数の±これ（場所でなく規模で測る＝作者裁定）
````

## L89-96 W 直後 `public const double MilitarismDegreeOk = 0.13;`
````
// ★軍国の照合閾値（2026-08-18・親軍法の撤去）：NationStanding.Militarism（＝**軍の政治的優位**∈[0,1]）がこれ以上なら「軍国」と判定する。
//   決め方＝実データ（1914断面）で史実の軍国（プロイセン）が真・非軍国（アメリカ）が偽になる位置を実測して選ぶ（測定値は docs/202608）。
//   ⚠旧案「軍事費×徴兵法の深さ」は棄却（英1913＝深度0なのに国防費は中央歳出の43%／実測で61カ国が0.350に張り付き識別力ゼロ）。
//   ⚠これは**忠実度の物差しそのもの**であり、変えると法一致の値が動く（挙動は1ビットも動かない＝参照表はシミュレーションから読まれない）。
//   ⚠AIの軍国教義の判定線（SelfPlay.MilitaristDoctrineDegree）とは別の定数＝物差しを動かして挙動が動く、を作らない。
//   ★実測で確定（milgrade 6seed・1914断面・2026-08-18）：プロイセン 平均0.204／中央0.153／最小0.118、アメリカ 平均0.118／中央0.099／最大0.287。
//     両者を**中央値で分ける**位置＝(0.099, 0.153] の帯。その中央の 0.13 を採る（普が過半のシードで真・米が過半で偽）。
//   ⚠**分離は弱い**（分布が大きく重なる）＝この軸は現状「よく効く物差し」ではない。次ラウンドの較正対象（docs/202608 §13-3）。
````

## L99-143 W 直後 `public const double DimensionTargetStrict = 0.5;     // 各次元の目標＝厳格一致50%（＝体感7割。exact≈near×0.`
````
// ── 列強でないはずの国（GreatPowerOk: false・D3／2026-08-01に46件を定義。旧＝**0件**）──────
//   ★これが**アジア過近代化を測る唯一の計器**である。旧＝真の列強8カ国に true を置くだけだったので、
//     「列強でない国が列強になっていないか」を1度も測っていなかった（＝清やペルシャが上位7位に入っても合格だった）。
//   1914年の列強＝英仏独墺露伊日米の8（定員7＝もっとも新参で最弱の伊が外れる）。それ以外は全て false。
//   ⚠オスマンは19世紀前半には列強扱いされたが、1914年には「瀕死の病人」＝協調体制の外にあった＝false。
//   ⚠清は1914年には軍閥割拠の只中＝false（1783年時点では世界最大の経済だが、それは GdpRank1913 の次元が測る）。
//   ⚠参照表はシミュレーションから読まれない＝**挙動を1ビットも変えない**（帯リスクゼロ）。
// ★★参照データ拡充（2026-08-01・docs/20260801「導出方法の精査」④）。旧＝経済3件/本土3件/植民地0件＝**測れていなかった**。
// ── 経済規模（GdpRank1913）＝Maddison の1913年 総GDP（1990年国際ドル）順位 ──────────────
//   米517 > 中241 > 独237 > 露232 > 英225 > 印204 > 仏144 > 墺洪100 > 伊96 > 日72
//   ＝米1・清2・普3・露4・英5・(印6)・仏7・墺8・伊9・日10。★順位ベース＝単位系に頑健（±3位）。
// ── 領土（HomeProvinces1914）＝**world.json の州のうち1914年に実際に統治していた数** ────────
//   数え方＝開始州から史実で失ったものを引く（増えた分は不問＝上振れは領土の喪失ではない）。
//   オスマン14→8   ：ベオグラード/ソフィア(1878)・アテネ(1830)・ブカレスト(1878)・サロニカ(1912)・カイロ(英占領1882)を失う
//   オーストリア13→11：ミラノ(1859)・ブリュッセル(オーストリア領ネーデルラント喪失)を失う
//   清22→19        ：ウルガ(外蒙古1911独立)・ラサ(チベット1913事実上独立)・台北(1895下関条約)を失う
//   露13・普4・米3（2026-08-09再導出＝§13でボストン吸収・開始州3）
//   ・西4・蘭2・仏7・日7・英5・サルデーニャ3＝本土は動かない
//   ★(516)：(512) で州を足した 2 国は数え直した＝仏 8（アジャクシオ）・西 5（ラス・パルマス）。植民地の (512) の 4 件（蘭のモルッカ諸島・英のバハマ諸島＝1914 も保持／西のパラワン〔1898 喪失〕・チロエ〔1826 喪失〕）は下の ColonyProvinces1914 に入れていない＝蘭・英は 1 件ずつ甘い向きにずれる（±50% の内・計器の限界として名乗る）。
// ── 領土（ColonyProvinces1914）＝**初期植民地と同じ粒度**での1914年の海外領の数（±50%） ──────
//   英11→21：1783の11をほぼ保持し、エジプト・スーダン・ナイジェリア・ゴールドコースト・東アフリカ・ローデシア
//             ・南アフリカ・ビルマ・マレー・ニュージーランド・セイロンを加える
//   仏6→10 ：1783のカリブ/インド商館を保ちつつアルジェリア・チュニジア・モロッコ・西アフリカ・赤道アフリカ
//             ・マダガスカル・インドシナを得る（ルイジアナは1803売却）
//   西10→2 ：アメリカ大陸の全植民地(1810s-20s)とキューバ/比/プエルトリコ(1898)を失い、モロッコ/リオデオロ/赤道ギニアのみ
//   蘭4→3  ：ケープ(1806)・セイロン(1796)を失い、蘭領東インド・スリナム・キュラソーが残る
//   米0→2  ：フィリピン・プエルトリコ（＋ハワイ/グアム）＝1898の獲得
//   普0→3  ：南西アフリカ・東アフリカ・カメルーン/トーゴ（＋ニューギニア・膠州湾）
//   伊0→2  ：リビア(1911)・エリトリア/ソマリランド
//   日0→1  ：台湾(1895)。★朝鮮(1910)は本土州の併合であって植民地オブジェクトではない
//   露・墺・オスマン・清＝**0**（大陸帝国＝海外植民地を持たない）。★これは強い主張であり、
//     0を置くことで「大陸帝国が海外植民地を取り始めたら不合格になる」＝計器として働かせる意図がある。
// ── ★2026-08-02 追加：法軸の穴を塞ぐ（監査で発見・参照表はシミュレーションから読まれない＝挙動不変）──
//   ① 土地改革（LandReformOk）＝**それまで参照表に1度も出現せず、忠実度に1ビットも寄与しない死んだ軸**だった。
//      全54カ国×10seedが Full に固着していても計器は無反応。史実1914で「全面」に相当するのは
//      日本の地租改正（1873＝封建的土地所有の解体）・デンマークの自作農化・仏の1789封建的負担廃止など少数で、
//      英（地主貴族が農地の大半を保有）・墺洪（マジャール大貴族領）・西（ラティフンディオ）・清・オスマンは「なし」相当。
//      露はストルイピン改革（1906-11）で共同体を割ったが地主大農場は残存＝なし/限定的の両許容。
//   ② 穀物法（CornLawsOk）＝**英1カ国しか定義が無く、その正解が None**＝「世界一律 None が最高得点」という
//      逆インセンティブがあった。史実は逆で、新大陸の安い穀物に対し1879年以降に**農業保護の第二波**が起きる：
//      独1879ビスマルク穀物関税（1885/1887/1902ビューロー関税で強化）・仏1892メリーヌ関税・伊1887・西1891/1906・
//      瑞典1888・葡1889・墺洪も穀物関税。自由貿易を保ったのは英・蘭・丁抹（酪農へ転換し穀物は輸入）と、
//      穀物**輸出国**の露・米。両側を置くことで「一律に倒れたら落ちる」計器になる。
//   ③ 農奴解放（SerfOk）＝1914年に隷属が**まだ合法**だった国を追加（エチオピア1942・ネパール1926・
//      アラビア/イエメン/オマーンは1960-70年代）。これは固着を罰する向きにだけ効く。
````

## L144 T `public const double DimensionTargetStrict = 0.5;`
````
// 各次元の目標＝厳格一致50%（＝体感7割。exact≈near×0.5 の実測から）
````

## L146-146 W 直後 `public static string VerdictOf(double near) =>`
````
/// <summary>判定帯（新基準）：◎7割帯＝near 55-85%／固着＝>90%（史実に固着＝カオス不足）／逸脱過多＝<50%／△境界＝その間（50-55・85-90）。</summary>
````

## L150-150 W 直後 `public static int AxesOf(HistoricalEntry e) =>`
````
/// <summary>エントリの定義済み法軸数（法の一致率の分母単位）。</summary>
````

## L156-156 W 直後 `public static (int Matched, int Axes) LawScore(HistoricalEntry e, NationStanding s)`
````
/// <summary>1国×1シードの法一致数/軸数。</summary>
````

## L167-168 W 直後 `if (e.MilitarismOk is { } m) { axes++; if ((s.Militarism >= MilitarismDegreeOk) == m) ok++`
````
// ★2026-08-18（親軍法の撤去）：s.Militarism は bool から**軍の政治的優位∈[0,1]**（軍の圧力×(1−文民制度の弁)）になった。
//   法の有無で照合できなくなったので、閾値で二値化して照合する＝物差しそのものの変更（docs/20260818に前後の法一致を記録）。
````

## L173-176 W 直後 `public static IReadOnlyList<NationFidelity> Evaluate(IReadOnlyList<MatchResult> completed)`
````
/// <summary>バッチ集計：完走試合の standings を国別に照合し忠実度を返す（表の順＝world.json の国順）。
/// 1934延長（2026-07-19裁定）後は Q524（1914年1月1日）断面 StandingsAt1914 を優先＝主指標の物差しは1914固定。
/// 併記の断面＝1820/1870（政体＋GDP順位）・1920（政体）・1950（政体の exact/near＋従属・Q668＝終局）。いずれも主指標へ合算しない。
/// 照合は NationStanding.Nation（無ければ首都名へフォールバック＝旧データ互換）。決定論＝入力順のみに依存。</summary>
````

## L183 T `int labelExact = 0;`
````
// ★旧ラベル基準の exact（2026-08-21・比較可能性のための併走。物差しの切替で過去測定と数字が切れるのを防ぐ）
````

## L184 T `int popOk = 0, popSeeds = 0; double popSum = 0;`
````
// B-中7：人口成長倍率（独立指標）
````

## L185 T `double popGrossSum = 0;`
````
// 案Y（2026-08-21）：旧定義（総人口比＝征服インフレ込み）の平均＝新旧比較の橋（判定には使わない）
````

## L186 T `int greatOk = 0, greatSeeds = 0; double rankSum = 0;`
````
// 列強軸（2026-07-27・独立指標）
````

## L187 T `bool greatExpected = e.GreatPowerOk == true;`
````
// ★2026-08-01：再現率側（史実の列強）か特異度側（史実の非列強）か
````

## L188 T `int econOk = 0, econSeeds = 0; double econRankSum = 0;`
````
// 08：経済規模（GDP世界順位）
````

## L189 T `int homeOk = 0, homeSeeds = 0; double homeFracSum = 0;`
````
// 08：領土（本国州の保有率）
````

## L190 T `int colOk = 0, colSeeds = 0; double colSum = 0;`
````
// 08：領土（植民地の規模）
````

## L191 T `int tradeOk = 0, tradeSeeds = 0; double tradeSum = 0;`
````
// ★貿易開放度（2026-08-15）：(輸出+輸入)/GDPの±50%帯
````

## L193 T `int ex1820Ok = 0, ex1820N = 0, ex1870Ok = 0, ex1870N = 0, ec1820Ok = 0, ec1820N = 0, ec187`
````
// ★E-08 断面の計数
````

## L194 T `int ex1920Ok = 0, ex1920N = 0, ecDummyOk = 0, ecDummyN = 0;`
````
// ★E-09（経済は採点しない＝ダミーの計数）
````

## L195 T `int ex1950Ok = 0, nr1950Ok = 0, ex1950N = 0;`
````
// ★1950断面（政体の exact/near・従属込み）
````

## L197 T `NationStanding? Find(IReadOnlyList<NationStanding> sec)`
````
// 照合キー＝Nation（無ければ首都名＝旧データ互換）。主指標と全断面で同じ1本
````

## L204 T `var stands = r.StandingsAt1914 ?? r.Standings;`
````
// 1934延長2026-07-19：照合はQ524（1914年1月1日）断面。524以下の走行は最終値＝従来互換
````

## L205 T `int rank = 0;`
````
// 列強軸：スコア降順の順位（1始まり）。同点は Score の大小のみで数える＝入力順に依存しない決定論
````

## L208-208 W 直後 `void Section(IReadOnlyList<NationStanding>? sec, Polity[]? okPol, int? okRank, ref int exO`
````
// ★E-08 多断面（2026-09-05）：1820／1870 の断面があれば、その時点の導出政体とGDP順位を照合（主指標には合算しない）
````

## L221 T `Section(r.StandingsAt1920, e.Exact1920, null, ref ex1920Ok, ref ex1920N, ref ecDummyOk, re`
````
// ★E-09
````

## L222-222 W 直後 `if (r.StandingsAt1950 is { } sec50 && e.Exact1950 is { } ok50 && Find(sec50) is { } s50)`
````
// ★1950断面（2026-09-21）：主指標と同じ照合規則（従属の許容つき）で exact/near を数える（主指標には合算しない）
````

## L237-243 W 直後 `var (isExact, isNear) = PolityMatch(s.DerivedPolity, s.Suzerain, e.Exact, e.Near, e.Subjug`
````
// 主権軸：史実が従属（併合/保護国/属国）の国は、従属で終局していれば政体に関わらず一致（マイソールが英属国＝史実そのもの）。
// 宗主同一性（監査2026-07-12 リ12）：exact＝史実の宗主（SuzerainsOk）に従属／near＝任意の宗主に従属＝「誰が奪ったか」の地理まで測る。
// 属国離反の実働（2026-07-12）で宗主の入れ替わりが日常になったため、従属の有無だけの旧判定では勢力圏形成の忠実度が測れない。
// ★★政体の照合を**導出政体**へ切り替え（2026-08-21・作者裁定「切り替えでよい」）。
//   旧＝法と遷移で決まるラベル s.Polity ／ 新＝権力シェアからの読み出し s.DerivedPolity（PowerAffinity.PolityFromShares）。
//   ★切替の根拠（実測）＝一致率75〜94%・不一致4件中3件は導出が史実に近い（英1815/清/西1900の「軍事独裁」ラベルは
//     いずれも誤りで、導出は正しく議会/王権になる）。⚠旧基準の exact は labelExact として**同走で併記**＝過去測定との橋。
````

## L250-251 W 直後 `if (e.PopGrowthOk is { } pg && s.PopGrowth > 1e-9)`
````
// B-中7：人口成長倍率。帯が未定義／初期人口不明（PopGrowth=0＝建国スポーン等）は測らない。
// 案Y（2026-08-21）：PopGrowth は併合の持ち込み人口を除いた新定義（SelfPlay 側）。旧定義は PopGrowthGross として平均のみ併記。
````

## L257-257 W 直後 `if (e.GdpRank1913 is { } gr)`
````
// 08の70%計器（2026-07-31）＝経済規模：GDPの**世界順位**が史実±3位以内（順位ベース＝単位系に頑健）。
````

## L265-265 W 直後 `if (e.HomeProvinces1914 is { } hp && hp > 0)`
````
// 08＝領土（本国）：史実1914の本国州のうち何割を保有しているか。★征服で増えるぶんは不問（上振れは領土「以上」＝一致）
````

## L272-272 W 直後 `if (e.ColonyProvinces1914 is { } cp)`
````
// 08＝領土（植民地）：**場所でなく規模**（作者裁定）＝史実の植民地数の±50%
````

## L275-277 W 直後 `double colVal = s.ColonyMass > 0 ? s.ColonyMass : s.Colonies;`
````
// ★2026-08-28（作者裁定③）：個数→**質量**（Colony.Population単位）。州追加の細分化で「行を分けただけで
//   個数が増える」単位崩れの是正（スコア側 ColonyMassUnits と同じ物差し）。参照値 cp は旧単位（初期植民地の粒度）
//   だが、初期植民地の平均 Population≈1 なので同じ目盛りとして±50%帯で照合できる。s.ColonyMass=0（旧経路）は個数へ退避。
````

## L283-284 W 直後 `if (tradeRef > 1e-9)`
````
// ★貿易開放度（2026-08-15・作者承認）：1913年の(輸出+輸入)/GDPの±50%相対帯（植民地と同じ作法）。
//   存在意義＝Search再較正の目的関数に貿易を見えるようにする（現況≈1%＝全国未達が既知の出発点）。
````

## L292 T `if (seeds == 0) continue;`
````
// 参照表にあるがバッチに現れない国（将来の国追加時の安全弁）
````

## L296 T `popSeeds > 0 ? (double)popOk / popSeeds : double.NaN,`
````
// B-中7：人口の一致率（独立指標・NaN＝未定義）
````

## L297 T `popSeeds > 0 ? popSum / popSeeds : 0,`
````
//        実測の平均成長倍率（診断用）
````

## L298 T `greatSeeds > 0 ? (double)greatOk / greatSeeds : double.NaN,`
````
// 列強軸：一致率（独立指標・NaN＝未定義）
````

## L299 T `greatSeeds > 0 ? rankSum / greatSeeds : 0,`
````
//        実測の平均順位（診断用）
````

## L300 T `greatExpected,`
````
// ★どちら側の国か
````

## L301 T `greatSeeds > 0 ? (double)greatOk / greatSeeds : double.NaN,`
````
//   その国の到達率（中身は GreatMatch と同じ・分けて集計するための写し）
````

## L302 T `econSeeds > 0 ? (double)econOk / econSeeds : double.NaN,`
````
// 08：経済規模の一致率
````

## L303 T `econSeeds > 0 ? econRankSum / econSeeds : 0,`
````
//        実測の平均GDP順位
````

## L304 T `homeSeeds > 0 ? (double)homeOk / homeSeeds : double.NaN,`
````
// 08：領土（本国）の一致率
````

## L305 T `homeSeeds > 0 ? homeFracSum / homeSeeds : 0,`
````
//        実測の平均保有率
````

## L306 T `colSeeds > 0 ? (double)colOk / colSeeds : double.NaN,`
````
// 08：領土（植民地）の一致率
````

## L307 T `colSeeds > 0 ? colSum / colSeeds : 0,`
````
//        実測の平均植民地数
````

## L308 T `tradeSeeds > 0 ? (double)tradeOk / tradeSeeds : double.NaN,`
````
// ★貿易開放度の一致率（2026-08-15）
````

## L309 T `tradeSeeds > 0 ? tradeSum / tradeSeeds : 0,`
````
//        実測の平均開放度（診断用）
````

## L310 T `(double)labelExact / seeds,`
````
// ★旧ラベル基準の政体exact（併走）
````

## L311 T `popSeeds > 0 ? popGrossSum / popSeeds : 0,`
````
// 案Y：旧定義（征服インフレ込み）の平均倍率（併走・診断用）
````

## L312 T `Exact1820: ex1820N > 0 ? (double)ex1820Ok / ex1820N : double.NaN, Exact1870: ex1870N > 0 ?`
````
// ★E-08
````

## L314 T `Exact1920: ex1920N > 0 ? (double)ex1920Ok / ex1920N : double.NaN,`
````
// ★E-09
````

## L315 T `Exact1950: ex1950N > 0 ? (double)ex1950Ok / ex1950N : double.NaN, Near1950: ex1950N > 0 ? `
````
// ★1950断面
````

## L320-321 W 直後 `public static (bool Exact, bool Near) PolityMatch(Polity polity, string suzerain, Polity[]`
````
/// <summary>1国×1断面の政体照合（主指標と1950断面が共有する1本の規則）。主権軸（裁定2・2026-07-10）＋宗主同一性（監査2026-07-12 リ12）：
/// exact＝史実の政体、または従属かつ宗主が許容集合内（集合が null なら宗主不問）／near＝exact ∪ 近い政体 ∪ 任意の宗主に従属。</summary>
````

## L329-331 W 直後 `public static readonly (string Nation, double Openness)[] TradeOpenness1913 =`
````
// ★貿易開放度の参照（2026-08-15・作者承認「テスト速度に影響しないなら追加」）：1913年の(輸出+輸入)/GDP＝第一次グローバリゼーションの物差し。
// 出典＝Findlay & O'Rourke ほか通説の概数。9カ国＝8件以上なのでゲート対象（50%）。現況の実測≈1%＝全国未達が既知
// ＝この次元の存在意義は**Search再較正の目的関数が貿易を見えるようにする**こと（TradeSpeed等の再導出はこの次元込みで）。
````

## L337 T `public const double TradeOpennessTolerance = 0.5;`
````
// ±50%相対帯（領土・植民地と同じ作法）
````

## L339-345 W 直後 `public static readonly HistoricalEntry[] Table =`
````
// ================== 1914年 史実参照表（全国・world.json 順） ==================
// 略記：Abs=Absolutist Des=Despotic Sho=Shogunate CM=ConstitutionalMonarchy Rep=Republic
//       BD=BourgeoisDictatorship PD=ProletarianDictatorship MD=MilitaryDictatorship
// 主権喪失国（併合・保護国化＝印度諸邦/ポーランド/伊独小邦/中央アジア等）：ゲームは国を消さないため、
// 政体軸は「存続した場合に史実の系が示した統治形」を Exact とし、真の史実（併合/属国化）は外交ログ（属国・州移転・敗戦）の監査で扱う（Note に明記）。
// ★1950断面（2026-09-21裁定）は各行の2行目（Exact1950/Near1950/SuzerainsOk1950）＝根拠は行末の「// 1950：」。1950年1月1日の状態で置く
//   （インド自治領の共和制化1950/1/26・トルコの政権交代1950/5 のように年内に変わる国は両方を exact にする＝清1912の作法）。
````

## L349 T `Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],`
````
// 1950：第四共和政（1946憲法・議会制共和）。近い＝ド・ゴール派の強い執行権志向（ブル独裁）
````

## L353 T `PopGrowthOk: (1.25, 1.7),`
````
// 人口帯拡張2026-07-25：1783約27.5M→1914約39.8M＝×1.45。欧州最遅の自然増（出生率の早期低下）。アルザス喪失1871は−4%＝帯内
````

## L354 T `GreatPowerOk: true,`
````
// 列強軸2026-07-27：1783も1914も列強（普仏戦争の敗北を挟んでも地位は失わない）
````

## L355 T `Exact1920: [Polity.Republic], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Poli`
````
// ★(516) 7→8＝(512) で州アジャクシオ（コルシカ・1914 も仏領）を足した。08の6次元2026-07-31：Maddison1913の総GDP順位7位／本土7州（アルザス喪失を除けば1914も本土保持＝領土安定国）
````

## L357 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic], SuzerainsOk1950: `
````
// 1950：1947憲法＝象徴天皇の議会制立憲君主制。1952年4月まで米の占領（SCAP）＝米への従属もexact。近い＝君主が象徴に退いた議会制（共和）
````

## L361 T `PopGrowthOk: (1.7, 2.4),`
````
// 人口帯拡張2026-07-25：1783約26M→1914約53M＝×2.04。徳川停滞→明治の人口転換。本土領土は安定（朝鮮は属国＝別国のまま非算入）
````

## L362 T `GreatPowerOk: true,`
````
// 列強軸2026-07-27：日露戦争1905の勝利で列強入り＝1783は非列強だが1914は列強（台頭の代表例）
````

## L363 T `Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.Shogunate], Exact1870: [Pol`
````
// 08の6次元2026-07-31：Maddison1913の総GDP順位は10位前後／本土7州（1914も保持＝領土安定国）
````

## L365 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],`
````
// 1950：議会主権の立憲王政（アトリー労働党内閣）。近い＝共和（1914表と同じ）
````

## L370 T `PopGrowthOk: (2.6, 4.3),`
````
// B-中7：1783→1911に約×3.4（領土変化なしの純自然増）。穀物法撤廃後は小麦消費の約8割を輸入して人口を養った＝B-高1（扶養力に輸入食料が入らない）の主対象
````

## L371 T `GreatPowerOk: true,`
````
// 列強軸2026-07-27：全期間を通じ列強（1783は米独立直後の敗戦国だが即座に回復し以後は覇権）。**実測 seed1 で49位＝この軸を追加した直接の動機**
````

## L372 T `Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy], Ex`
````
// 08：1913年は米独に抜かれて5位／本土5州（1914の連合王国＝安定）
````

## L374 T `Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],`
````
// 1950：連邦共和制（トルーマン）。近い＝ブル独裁（1914表と同じ）
````

## L375 T `SuffrageOk: [Suffrage.AdultMale, Suffrage.Universal], EducationOk: [EducationLaw.Public, E`
````
// 非軍国＝1914年も志願兵制（徴兵は1917参戦後）＋小さな常備軍＝軍国度は閾値未満のはず
````

## L378 T `GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Republic], Exact1870:`
````
// 州数再導出2026-08-09：§13再配分でボストン→NY吸収＝開始州3（保持判定の分母を実州数へ・期待値再導出は許可済み）        // 列強軸2026-07-27：1914には世界最大の工業国＝列強（1783は辺境の新興国＝台頭の代表例）
````

## L380 T `Exact1950: [Polity.ProletarianDictatorship],`
````
// 1950：ソ連＝共産党の一党独裁（スターリン）
````

## L384 T `GreatPowerOk: true, Exact1920: [Polity.ProletarianDictatorship], Exact1820: [Polity.Absolu`
````
// 列強軸2026-07-27：全期間を通じ列強（クリミア1856・日露1905の敗北を挟んでも地位は失わない＝人口と版図の慣性）
````

## L386 T `Exact1950: [Polity.Republic, Polity.ProletarianDictatorship], SuzerainsOk1950: ["アメリカ", "イ`
````
// 1950：普は1947解体・独は分断＝西独（1949基本法・議会制共和）と東独（1949・SED一党独裁・首都は東ベルリン）の両方をexact。西は占領規約（1949-55）・東はソ連管理委＝米英仏ソへの従属もexact
````

## L388 T `PensionOk: [ReformLevel.Limited, ReformLevel.Full], MilitarismOk: true,`
````
// 軍国＝一般兵役義務(1814)＋軍事予算＝軍国度が閾値以上のはず
````

## L391 T `GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Absolutist], Exact187`
````
// 列強軸2026-07-27：1783は5位相当の列強、1871統一後は欧州最強の陸軍国＝1914も列強
````

## L393 T `Exact1950: [Polity.Republic, Polity.BourgeoisDictatorship], Near1950: [Polity.MilitaryDict`
````
// 1950：トルコ共和国。1950年1月は共和人民党の一党優位（1946複数政党化・同年5月の選挙で民主党へ交代）＝共和とブル独裁（ケマル主義の一党体制）の両方をexact。近い＝軍の後見
````

## L399 T `Exact1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ", "イギリス", "フランス", "ロシア"],`
````
// 1950：第二共和政（1945）。1955国家条約まで米英仏ソの4国占領＝占領国への従属もexact
````

## L403 T `PopGrowthOk: (1.8, 2.6),`
````
// 人口帯拡張2026-07-25：ハプスブルク圏1783約23M→1914約51M＝×2.2。領土は概ね安定。分離独立で州を失う世界線は帯を割る＝それ自体が忠実度の信号
````

## L404 T `GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Absolutist], Exact187`
````
// 列強軸2026-07-27：二重帝国は最後まで列強（1866普墺の敗北・1878以降の相対的衰退を挟んでも五大国の一角）
````

## L406 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],`
````
// 1950：立憲王政（ユリアナ女王1948即位・1949インドネシアへ主権移譲）
````

## L412 T `Exact1950: [Polity.MilitaryDictatorship], Near1950: [Polity.BourgeoisDictatorship],`
````
// 1950：フランコ体制＝内戦に勝った将軍の独裁（1947王位継承法で名目は王国・フランコが終身の国家元首）。近い＝ファランヘ一党のブル独裁
````

## L416 T `PopGrowthOk: (1.6, 2.2), HomeProvinces1914: 5, ColonyProvinces1914: 2, GreatPowerOk: false`
````
// ★(516) 4→5＝(512) で州ラス・パルマス（カナリア・1914 も西領）を足した。人口帯拡張2026-07-25：1783約10.6M→1914約20.5M＝×1.93。本土領土は安定（植民地喪失は本土人口に非算入）
````

## L418 T `Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy],`
````
// 1950：イタリア共和国（1946国民投票で王政廃止・1948憲法）。近い＝サヴォイア立憲王政の存続（国民投票は54対46）
````

## L422 T `GreatPowerOk: true, Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.Absolut`
````
// 列強軸2026-07-27：1861統一で列強入り＝8カ国中もっとも新参かつ最弱（定員7の1つ余りはここが外れるのが史実的に自然）
````

## L424 T `Exact1950: [Polity.ProletarianDictatorship], Near1950: [Polity.MilitaryDictatorship, Polit`
````
// 1950：中華人民共和国（1949年10月建国・上海は同年5月に人民解放軍が占領）。近い＝国民党の訓政（党国体制＝ブル独裁）・軍閥（軍事独裁）
````

## L427 T `PopGrowthOk: (1.1, 1.9), HomeProvinces1914: 19, ColonyProvinces1914: 0, GreatPowerOk: fals`
````
// B-中7：史実は約×1.45（実測sim×1.07＝過少）。太平天国の人口損失を含んでなお増加した＝低成長だが停滞ではない
````

## L429 T `Exact1950: [Polity.Republic], Near1950: [Polity.ProletarianDictatorship, Polity.BourgeoisD`
````
// 1950：漢城＝大韓民国（1948・李承晩の大統領制・米軍政は1948に終了＝独立）。近い＝北の朝鮮民主主義人民共和国（1948・一党独裁）・李承晩の権威主義（ブル独裁）。朝鮮戦争は同年6月＝断面の後
````

## L432 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],`
````
// 1950：プーナ＝インドのボンベイ州。1950年1月1日はインド自治領（英王を戴く立憲君主制）・1月26日に共和制＝両方exact。英の宗主権は1947に消滅＝英への従属は「近い」だけに数える（空集合・2026-09-23裁定）。⚠ここを ["イギリス"] と置くと逆転する＝1950年に実際に英の影響下だったオマーンが「近い」止まりで、英から独立していたインド5カ国が史実一致になっていた
````

## L435 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],`
````
// 1950：マイソール州＝インドの一州（藩王は1950から州元首ラージプラムク）。インド自治領→1950年1月26日共和制＝両方exact。英の宗主権は1947に消滅＝英への従属は「近い」だけに数える（空集合・2026-09-23裁定）。⚠ここを ["イギリス"] と置くと逆転する＝1950年に実際に英の影響下だったオマーンが「近い」止まりで、英から独立していたインド5カ国が史実一致になっていた
````

## L438 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],`
````
// 1950：1948ポロ作戦でインドへ併合（ニザームは1950から州元首）。インド自治領→共和制＝両方exact。英の宗主権は1947に消滅＝英への従属は「近い」だけに数える（空集合・2026-09-23裁定）。⚠ここを ["イギリス"] と置くと逆転する＝1950年に実際に英の影響下だったオマーンが「近い」止まりで、英から独立していたインド5カ国が史実一致になっていた
````

## L441 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],`
````
// 1950：ラホール＝パキスタン自治領（1947分離独立・英王を戴く立憲君主制・共和制は1956）。近い＝東パンジャーブ（シクの多数）を含むインドの共和制
````

## L444 T `Exact1950: [Polity.Republic], Near1950: [Polity.MilitaryDictatorship],`
````
// 1950：ビルマ連邦（1948独立・議会制共和・英連邦外）。近い＝軍の台頭（1958選挙管理内閣・1962ネウィン）
````

## L447 T `Exact1950: [Polity.Despotic, Polity.MilitaryDictatorship], Near1950: [Polity.Absolutist, P`
````
// 1950：ラナ家世襲宰相制の最末期（1950年11月に国王がインド大使館へ・1951年2月の王政復古）＝1914と同じ両方exact。近い＝王政復古（親政/立憲）
````

## L451 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship],`
````
// 1950：タイ王国＝1932以降の立憲王政だが1947クーデタ→1948ピブーン首相の軍主導政権＝両方exact
````

## L455 T `Exact1950: [Polity.ProletarianDictatorship], Near1950: [Polity.Despotic], SuzerainsOk1950:`
````
// 1950：ハノイ＝仏連合のベトナム国（1949・バオダイ）が保持＝仏への従属をexact。ベトナム民主共和国（1945ハノイで独立宣言・共産党）が北部農村を支配＝プロ独裁もexact。近い＝バオダイの専制
````

## L458 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Absolutist], SuzerainsOk1950: ["フランス"],`
````
// 1950：仏連合の協同国（1949）＝シハヌーク王の王国（1947憲法）。完全独立は1953＝仏への従属をexact
````

## L461 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Absolutist], SuzerainsOk1950: ["フランス"],`
````
// 1950：仏連合の協同国（1949）＝シーサワンウォン王の王国（1947憲法）。完全独立は1953＝仏への従属をexact
````

## L464 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Absolutist, Polity.Despotic]`
````
// 1950：パフラヴィー朝の立憲王政（議会マジュレス・1949憲法改正で国王権限拡大・1951モサッデク首相）。近い＝国王の親政
````

## L467 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic],`
````
// 1950：サウジアラビア王国（1932・イブン・サウードの絶対君主制・独立）
````

## L468 T `SerfOk: [ReformLevel.None],`
````
// アラビア半島の奴隷制はサウジ1962廃止令まで存続＝1914年に解放済みなら逸脱
````

## L471 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic],`
````
// 1950：ムタワッキル王国（1918独立・イマーム・アフマドの神権的君主制）
````

## L472 T `SerfOk: [ReformLevel.None],`
````
// イエメン1962共和革命まで奴隷制＝1914年に解放済みなら逸脱
````

## L475 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: [],`
````
// 1950：マスカット・オマーン＝サイード・ビン・タイムールのスルタン国。英の**正式な保護領ではない**（条約関係と政治代理官のみ＝湾岸の休戦諸国とは地位が違う・2026-09-23裁定）＝英への従属はexactから「近い」へ落とす（空集合＝従属はnearだけ数える）
````

## L476 T `SerfOk: [ReformLevel.None],`
````
// オマーン1970まで奴隷制＝1914年に解放済みなら逸脱
````

## L479 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic, Polity.ConstitutionalMonarchy]`
````
// 1950：アフガニスタン王国（1919独立・ザーヒル・シャー＝1931憲法だが王族宰相の親政）。英の外交管理は1919に消滅＝従属は数えない
````

## L482 T `Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],`
````
// 1950：1920ブハラ人民ソヴィエト共和国→1924ウズベクSSR＝ソ連の構成国（露への従属と一党独裁をexact）
````

## L485 T `Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],`
````
// 1950：1920ホラズム人民ソヴィエト共和国→1924ウズベク/トルクメンSSR＝ソ連の構成国
````

## L488 T `Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],`
````
// 1950：タシュケント＝ウズベクSSRの首都＝ソ連の構成国
````

## L491 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス", "スペ`
````
// 1950：仏西保護領（1912-56）＝スルタン・ムハンマド5世の王制存続（1914と同じ）
````

## L494 T `Exact1950: [Polity.Absolutist, Polity.Despotic], Near1950: [Polity.ConstitutionalMonarchy]`
````
// 1950：ハイレ・セラシエの絶対君主制（1931憲法の議会は諮問的）。伊占領1936-41から回復＝独立
````

## L498 T `Exact1950: [Polity.Absolutist, Polity.Despotic], SuzerainsOk1950: ["フランス"],`
````
// 1950：仏海外領（1946・1947蜂起を鎮圧・独立1960）。1914表と同じく伝統王政の存続をexact
````

## L501 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],`
````
// 1950：英領ゴールドコースト（独立1957）。アシャンティ王は1935に英の間接統治の下で復位＝伝統王政をexact
````

## L503-503 W 直後 `new("ソコト", "ソコト", [Polity.Absolutist], [Polity.Despotic],`
````
// ★アフリカ5カ国（アフリカ植民doc B〜F・2026-08-13）。いずれも伝統王政の存続をexactとし、列強にはならない。
````

## L505 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],`
````
// 1950：英領ナイジェリア（独立1960）。ソコトのスルタンは間接統治の首長として存続
````

## L508 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["ポルトガル", "ベ`
````
// 1950：ンバンザ=コンゴ＝葡領アンゴラ（独立1975）・旧領の北半はベルギー領コンゴ（独立1960）。名目王の系は葡の下で存続
````

## L511 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],`
````
// 1950：仏領アルジェリア（本土の県・独立1962）。1914表と同じく専制の存続をexact
````

## L514 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],`
````
// 1950：英領ナイジェリア。オヨのアラーフィンは間接統治の首長として存続
````

## L517 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス", "フラ`
````
// 1950：英領ナイジェリア北東部と仏領チャド/カメルーンに分割。ボルヌのシェフは間接統治の首長として存続
````

## L520 T `Exact1950: [Polity.Absolutist, Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic]`
````
// 1950：米国のハワイ準州（州昇格1959）。1914表と同じく王制の存続をexact・近い＝準州の代議制（共和）
````

## L523 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],`
````
// 1950：仏領西アフリカ（独立1960）。1914表と同じく伝統王政の存続をexact
````

## L526 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],`
````
// 1950：立憲王政（社会民主党エルランデル内閣）
````

## L531 T `Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],`
````
// 1950：立憲王政（1945解放後に議会制を回復）
````

## L536 T `Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],`
````
// 1950：連邦共和制（1914表と同じ）
````

## L540 T `Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],`
````
// 1950：ポーランド人民共和国（統一労働者党の一党独裁・ビェルト）。1949年11月にソ連のロコソフスキー元帥が国防相＝衛星国＝ソ連への従属もexact
````

## L543 T `Exact1950: [Polity.BourgeoisDictatorship], Near1950: [Polity.MilitaryDictatorship],`
````
// 1950：サラザールの新国家（1933憲法・国民同盟の一党体制）。近い＝1926軍事クーデタ由来の軍の後見（大統領カルモナ元帥）
````

## L547 T `PopGrowthOk: (1.5, 2.0), GreatPowerOk: false),`
````
// 人口帯拡張2026-07-25：1783約3.4M→1914約6.0M＝×1.76。本土領土は安定（ブラジル喪失1822は植民地＝本土人口に非算入）
````

## L549 T `Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: `
````
// 1950：伊共和国の一部（1946）＝伊への従属と共和制をexact。近い＝王政の存続
````

## L552 T `Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],`
````
// 1950：ケルン＝西独ノルトライン=ヴェストファーレン州（1949基本法）＝独への従属と共和制をexact
````

## L555 T `Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: `
````
// 1950：伊共和国の一部。1946国民投票で南部は王政支持が多数＝近い＝王政の存続
````

## L558 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Republic], SuzerainsOk1950: ["サルデーニャ＝ピエモ`
````
// 1950：ローマ＝伊共和国の首都。教皇庁はラテラノ条約（1929）でヴァチカン市国の主権を持つ＝神権的絶対制をexact・伊への従属もexact。近い＝伊共和国の共和制
````

## L561 T `Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: `
````
// 1950：伊共和国の一部（1946）
````

## L564 T `Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: `
````
// 1950：伊共和国の一部（墺の宗主権は1866に消滅＝宗主は伊のみ）
````

## L567 T `Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],`
````
// 1950：西独のバイエルン自由州（1949）
````

## L570 T `Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["プロイセン", "ロシア"],`
````
// 1950：東独（SED一党独裁）のザクセン州＝ソ連占領地区から成立＝独とソ連への従属をexact
````

## L573 T `Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],`
````
// 1950：西独ニーダーザクセン州（1946）
````

## L576 T `Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],`
````
// 1950：西独ヴュルテンベルク=バーデン州（1946・1952バーデン=ヴュルテンベルク）
````

## L579 T `Exact1950: [Polity.Despotic, Polity.Absolutist], Near1950: [Polity.Republic], SuzerainsOk1`
````
// 1950：サイゴン＝仏連合のベトナム国（1949・元皇帝バオダイが国家元首）＝仏への従属をexact。近い＝1955ベトナム共和国
````

## L582 T `Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],`
````
// 1950：英アデン保護領のラヘジ・スルタン国（1914と同じ）
````

## L585 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],`
````
// 1950：インドの首都。1950年1月1日はインド自治領・1月26日に共和制＝両方exact。英の宗主権は1947に消滅＝英への従属は「近い」だけに数える（空集合・2026-09-23裁定）。⚠ここを ["イギリス"] と置くと逆転する＝1950年に実際に英の影響下だったオマーンが「近い」止まりで、英から独立していたインド5カ国が史実一致になっていた
````

## L588 T `Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],`
````
// 1950：ラクナウ＝インドの連合州（ウッタル・プラデーシュ）。インド自治領→共和制＝両方exact。英の宗主権は1947に消滅＝英への従属は「近い」だけに数える（空集合・2026-09-23裁定）。⚠ここを ["イギリス"] と置くと逆転する＝1950年に実際に英の影響下だったオマーンが「近い」止まりで、英から独立していたインド5カ国が史実一致になっていた
````

