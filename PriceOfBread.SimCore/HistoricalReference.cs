namespace PriceOfBread.SimCore;

/// <summary>
/// 史実参照表（1914年）＝新較正基準（ユーザー最高方針 2026-07-09）の一次データ。
/// 基準：各国が**約7割のシードで史実（または近い）政体・主要法**に到達する（100%は「史実に固着＝カオス不足」・5割を大きく割るのは「逸脱過多」）。
/// Exact＝史実そのもの（遷移期の国は複数可＝清の1912共和など）／Near＝許容隣接（近い姿。near一致＝Exact∪Near）。
/// 主要法＝史実が明確な国のみ2-4軸（許容集合）。人間可読の根拠は docs/史実参照表_1914.md（本表と1:1対応）。
/// 照合キーは Nation（NationConfig.Name＝Power.BaseName・構築時国名で変名しても不変）＝首都州の割譲・遷都・統一変名でも追える。
/// 能力の物理（距離・投射・経済基盤）は逸脱の下限として別途維持（モンゴル則）＝本表は「上限側」の物差しではなく忠実度の物差し。
/// </summary>
public sealed record HistoricalEntry(
    string Nation,                      // world.json の国名（NationConfig.Name）
    string Capital,                     // 首都州名（Regions[0]＝standings 表示名。参照表の可読性用）
    Polity[] Exact,                     // 史実の1914年政体
    Polity[] Near,                      // 近い政体（Exact に追加で near 一致）
    Suffrage[]? SuffrageOk = null,      // 参政権の許容集合（史実が明確な国のみ）
    EducationLaw[]? EducationOk = null, // 教育法の許容集合
    ReformLevel[]? SerfOk = null,       // 農奴解放の許容集合
    ReformLevel[]? PensionOk = null,    // 年金の許容集合（ビスマルク型の先進国のみ）
    ReformLevel[]? CornLawsOk = null,   // 穀物法（保護）の許容集合（英の1846撤廃など）
    ReformLevel[]? LandReformOk = null, // 土地改革の許容集合（★2026-08-02追加＝それまで忠実度に1ビットも寄与しない死んだ軸だった）
    bool? MilitarismOk = null,          // 軍国（プロイセン軍国主義など）。★2026-08-18：親軍法（bool）の撤去に伴い、照合対象は法の有無から
                                        //   **導出量『軍の政治的優位』（軍の圧力×(1−文民制度の弁)）が閾値 MilitarismDegreeOk 以上か**へ変わった（軸数＝分母は不変）。
                                        //   ⚠この物差しなら英1913（国防費が中央歳出の43%）でも文民制度が厚いので**偽**になる＝金額では測らない
    bool SubjugatedOk = false,          // 主権軸（裁定2・2026-07-10）：史実1914が併合/保護国/属国＝従属（宗主あり）で終局すれば政体に関わらず一致扱い（英領インド・仏領インドシナ・露領中央アジア・独伊統一の小邦併合など）
    string[]? SuzerainsOk = null,       // 宗主同一性（監査2026-07-12 リ12・裁定「解決法は任せる」）：史実の宗主（Power.BaseName＝変名しても不変）の許容集合。**exact＝従属かつ宗主が集合内／near＝任意の宗主に従属**の2段判定＝「誰が奪ったか」（勢力圏の地理）を測る。null＝宗主不問（旧挙動＝従属だけでexact）
    string Note = "",                   // 一行根拠（詳細は docs/史実参照表_1914.md）
    // B-中7（史実検証2026-07-22）：1783→1914の人口成長倍率の許容帯。null＝未定義＝測定対象外。
    // **領土不変な定義**を採る（本ゲームは国を消さず併合も起きるので絶対人口の±%は定義が噛み合わない）。
    // exact/near とは**合算しない独立の指標**（合算すると過去測定 exact41%/near73% との比較可能性が失われる）。
    // 領土獲得・移民を含む国（普×5.3・露×4.5・米×24）は純自然増として過大なので定義せず、領土が概ね安定した国のみ定義する。
    (double Lo, double Hi)? PopGrowthOk = null,
    // 列強軸（2026-07-27）：1914年に「列強（スコア上位 GreatPowerCount）」であるべきか。null＝測定対象外。
    // **人口軸と同じく exact/near とは合算しない独立の指標**（合算すると過去測定との比較可能性が失われる）。
    // 追加の理由＝実測で 1783年の列強7のうち1914年も列強なのは仏だけ（英は49位・工業化0.00・属国化経由／露は53位＝最下位）だったが、
    // 「1914年に誰が列強か」は政体軸でも法軸でも人口軸でも測られておらず、この歪みがどの帯にも露出していなかった。
    // 史実1914の列強は英仏独露墺伊米日の8カ国＝定員7に対し1つ多い＝各国とも100%にはならない（◎7割帯の思想と整合）。
    bool? GreatPowerOk = null,
    // ===== 08_バランス調整方針（2026-07-31）＝70%計器の6次元化。政体・法・人口・列強に**経済規模**と**領土**を足す。=====
    // 各次元とも「厳格一致50%」が目標（厳格50%≒体感7割＝現行計器の実測で exact≈near×0.5 が安定しているため）。
    // ★null＝その国は当該次元の測定対象外（人口軸・列強軸と同じ作法＝史実値が明確な国だけ定義する）。
    int? GdpRank1913 = null,          // 経済規模：1913年の世界GDP順位（Maddison推計）。**順位ベース**＝単位系に頑健。一致＝±GdpRankTolerance位
    int? HomeProvinces1914 = null,    // 領土（本国）：1914年に保有していた本国州の数（world.json の州帰属で数えた値）。一致＝現保有がこの HomeProvinceMatchFrac 以上
    int? ColonyProvinces1914 = null,
    // ★E-08 多断面（作者提案2026-09-04）：Maddison の基準年 1820／1870 の政体（許容集合）と世界GDP順位。null＝その断面では採点しない。
    //   主指標（◎7割帯・exact/near）は1913のまま＝既存基線を壊さない。これらは併記の列。
    Polity[]? Exact1820 = null, Polity[]? Exact1870 = null, int? GdpRank1820 = null, int? GdpRank1870 = null,
    Polity[]? Exact1920 = null,   // ★E-09 WW1の帰結＝1920年12月の政体（帝政4つの消滅・新共和国・ソヴィエト） // 領土（植民地）：1914年の植民地の数。★**場所でなく規模で測る**（作者裁定）＝±ColonyCountTolerance の相対幅
    // ★1950断面（作者裁定2026-09-21・Q668＝1950年1月1日＝終局）：政体の exact/near と従属の許容。照合の規則は主指標（1914）と同じ1本（PolityMatch）。
    //   SuzerainsOk1950＝null なら従属は数えない／非null なら「従属かつ宗主が集合内」を exact・「任意の宗主に従属」を near（主指標の SubjugatedOk＋SuzerainsOk と同型）。
    //   ＝空集合 [] は「従属を near だけで数える」（exact には数えない）＝正式な保護領ではなかった国の置き方（オマーン・2026-09-23）。
    //   1950年に主権を持たない国（併合・植民地・分割）は主指標と同じく「その地の1950年の統治形」を Exact に置く（例：伊の旧諸邦＝伊共和国の共和制）。
    Polity[]? Exact1950 = null, Polity[]? Near1950 = null, string[]? SuzerainsOk1950 = null);

/// <summary>1国の史実忠実度（バッチ集計）。Exact/Near＝完走シードに占める一致率。LawMatch＝定義済み法軸×シードの一致率（軸なしは NaN）。</summary>
public sealed record NationFidelity(
    string Nation, string Capital, int Seeds, double Exact, double Near, double LawMatch, int LawAxes, string Verdict,
    double PopMatch = double.NaN, double PopGrowthMean = 0,    // B-中7：人口成長倍率の一致率と実測平均。NaN＝この国は人口帯が未定義（測定対象外）
    double GreatMatch = double.NaN, double RankMean = 0,       // 列強軸（2026-07-27）：1914年に列強だった率と平均順位。NaN＝未定義
    // ★★列強軸を2つに割る（2026-08-01・ユーザー推薦）。⚠混ぜた集計値は**情報を持たない**——
    //   「誰も列強にならない」と常に答える自明な予測器が 47/55＝85.5% を取り、実測80.9%はそれを4.6pt下回っていた。
    //   46:8 の不均衡のせいで高く見えていただけである。★仕様「上位8と史実8の一致が5カ国以上」は**再現率**の
    //   閾値表現（5/8＝62.5%）、実装「国別 rank<=7」は再現率と特異度を混ぜたもの＝両方出せばどちらへ寄せる裁定も不要になる。
    bool GreatExpected = false,          // この国は史実で1914年に列強だったか（＝再現率側か特異度側か）
    double GreatHit = double.NaN,        // その国が「あるべき姿」に到達した率（列強のはずなら列強になった率／非列強のはずなら列強にならなかった率）
    // 08の70%計器（2026-07-31）：経済規模・領土（本国/植民地）の3次元。いずれも NaN＝測定対象外
    double EconMatch = double.NaN, double GdpRankMean = 0,
    double HomeMatch = double.NaN, double HomeFracMean = 0,
    double ColonyMatch = double.NaN, double ColonyMean = 0,
    double TradeMatch = double.NaN, double TradeMean = 0,   // ★貿易開放度（2026-08-15・作者承認）：(輸出+輸入)/GDPが史実1913年値の±50%帯に入った率・実測平均
    double LabelExact = double.NaN,   // ★旧ラベル基準の政体exact（2026-08-21・物差し切替の橋＝導出政体への切替後も過去測定と比べられる併走値）
    double PopGrowthGrossMean = 0,
    // ★E-08 多断面：1820／1870 断面の政体一致率とGDP順位一致率（NaN＝未定義／断面なし）
    double Exact1820 = double.NaN, double Exact1870 = double.NaN, double Econ1820 = double.NaN, double Econ1870 = double.NaN,
    double Exact1920 = double.NaN,   // ★E-09   // 案Y（2026-08-21）：旧定義（総人口比＝征服インフレ込み）の平均倍率＝人口物差し切替の橋（判定には使わない・診断印字のみ）
    double Exact1950 = double.NaN, double Near1950 = double.NaN);   // ★1950断面（2026-09-21）：政体の exact/near 一致率（NaN＝断面なし）

public static class HistoricalReference
{
    // マジックナンバー命名（挙動不変・値据え置き）：忠実度の判定帯。
    private const double FixationNearThreshold = 0.90;   // >90%＝史実に固着（カオス不足）
    private const double DeviationNearThreshold = 0.50;  // <50%＝逸脱過多
    private const double IdealBandLow = 0.55;            // ◎7割帯の下限
    private const double IdealBandHigh = 0.85;           // ◎7割帯の上限
    // 08_バランス調整方針（2026-07-31）＝6次元の一致定義（実装初期値・調整可）
    public const int GdpRankTolerance = 3;               // 経済規模：史実のGDP世界順位±これ以内で一致
    public const double HomeProvinceMatchFrac = 0.7;     // 領土（本国）：史実1914の保有州のうちこの割合以上を保有していれば一致
    public const double ColonyCountTolerance = 0.5;      // 領土（植民地）：史実の植民地数の±これ（場所でなく規模で測る＝作者裁定）
    // ★軍国の照合閾値（2026-08-18・親軍法の撤去）：NationStanding.Militarism（＝**軍の政治的優位**∈[0,1]）がこれ以上なら「軍国」と判定する。
    //   決め方＝実データ（1914断面）で史実の軍国（プロイセン）が真・非軍国（アメリカ）が偽になる位置を実測して選ぶ（測定値は docs/202608）。
    //   ⚠旧案「軍事費×徴兵法の深さ」は棄却（英1913＝深度0なのに国防費は中央歳出の43%／実測で61カ国が0.350に張り付き識別力ゼロ）。
    //   ⚠これは**忠実度の物差しそのもの**であり、変えると法一致の値が動く（挙動は1ビットも動かない＝参照表はシミュレーションから読まれない）。
    //   ⚠AIの軍国教義の判定線（SelfPlay.MilitaristDoctrineDegree）とは別の定数＝物差しを動かして挙動が動く、を作らない。
    //   ★実測で確定（milgrade 6seed・1914断面・2026-08-18）：プロイセン 平均0.204／中央0.153／最小0.118、アメリカ 平均0.118／中央0.099／最大0.287。
    //     両者を**中央値で分ける**位置＝(0.099, 0.153] の帯。その中央の 0.13 を採る（普が過半のシードで真・米が過半で偽）。
    //   ⚠**分離は弱い**（分布が大きく重なる）＝この軸は現状「よく効く物差し」ではない。次ラウンドの較正対象（docs/202608 §13-3）。
    public const double MilitarismDegreeOk = 0.13;

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
    public const double DimensionTargetStrict = 0.5;     // 各次元の目標＝厳格一致50%（＝体感7割。exact≈near×0.5 の実測から）

    /// <summary>判定帯（新基準）：◎7割帯＝near 55-85%／固着＝>90%（史実に固着＝カオス不足）／逸脱過多＝<50%／△境界＝その間（50-55・85-90）。</summary>
    public static string VerdictOf(double near) =>
        near > FixationNearThreshold ? "固着" : near < DeviationNearThreshold ? "逸脱過多" : near is >= IdealBandLow and <= IdealBandHigh ? "◎7割帯" : "△境界";

    /// <summary>エントリの定義済み法軸数（法の一致率の分母単位）。</summary>
    public static int AxesOf(HistoricalEntry e) =>
        (e.SuffrageOk is null ? 0 : 1) + (e.EducationOk is null ? 0 : 1) + (e.SerfOk is null ? 0 : 1) +
        (e.PensionOk is null ? 0 : 1) + (e.CornLawsOk is null ? 0 : 1) + (e.LandReformOk is null ? 0 : 1) +
        (e.MilitarismOk is null ? 0 : 1);

    /// <summary>1国×1シードの法一致数/軸数。</summary>
    public static (int Matched, int Axes) LawScore(HistoricalEntry e, NationStanding s)
    {
        int axes = 0, ok = 0;
        void Ax<T>(T[]? accepted, T value) { if (accepted is null) return; axes++; if (System.Array.IndexOf(accepted, value) >= 0) ok++; }
        Ax(e.SuffrageOk, s.Suffrage);
        Ax(e.EducationOk, s.Education);
        Ax(e.SerfOk, s.Serf);
        Ax(e.PensionOk, s.Pension);
        Ax(e.CornLawsOk, s.CornLaws);
        Ax(e.LandReformOk, s.LandReform);
        // ★2026-08-18（親軍法の撤去）：s.Militarism は bool から**軍の政治的優位∈[0,1]**（軍の圧力×(1−文民制度の弁)）になった。
        //   法の有無で照合できなくなったので、閾値で二値化して照合する＝物差しそのものの変更（docs/20260818に前後の法一致を記録）。
        if (e.MilitarismOk is { } m) { axes++; if ((s.Militarism >= MilitarismDegreeOk) == m) ok++; }
        return (ok, axes);
    }

    /// <summary>バッチ集計：完走試合の standings を国別に照合し忠実度を返す（表の順＝world.json の国順）。
    /// 1934延長（2026-07-19裁定）後は Q524（1914年1月1日）断面 StandingsAt1914 を優先＝主指標の物差しは1914固定。
    /// 併記の断面＝1820/1870（政体＋GDP順位）・1920（政体）・1950（政体の exact/near＋従属・Q668＝終局）。いずれも主指標へ合算しない。
    /// 照合は NationStanding.Nation（無ければ首都名へフォールバック＝旧データ互換）。決定論＝入力順のみに依存。</summary>
    public static IReadOnlyList<NationFidelity> Evaluate(IReadOnlyList<MatchResult> completed)
    {
        var list = new List<NationFidelity>(Table.Length);
        foreach (var e in Table)
        {
            int seeds = 0, exact = 0, near = 0, lawOk = 0, lawTot = 0;
            int labelExact = 0;   // ★旧ラベル基準の exact（2026-08-21・比較可能性のための併走。物差しの切替で過去測定と数字が切れるのを防ぐ）
            int popOk = 0, popSeeds = 0; double popSum = 0;   // B-中7：人口成長倍率（独立指標）
            double popGrossSum = 0;   // 案Y（2026-08-21）：旧定義（総人口比＝征服インフレ込み）の平均＝新旧比較の橋（判定には使わない）
            int greatOk = 0, greatSeeds = 0; double rankSum = 0;   // 列強軸（2026-07-27・独立指標）
            bool greatExpected = e.GreatPowerOk == true;   // ★2026-08-01：再現率側（史実の列強）か特異度側（史実の非列強）か
            int econOk = 0, econSeeds = 0; double econRankSum = 0;        // 08：経済規模（GDP世界順位）
            int homeOk = 0, homeSeeds = 0; double homeFracSum = 0;        // 08：領土（本国州の保有率）
            int colOk = 0, colSeeds = 0; double colSum = 0;               // 08：領土（植民地の規模）
            int tradeOk = 0, tradeSeeds = 0; double tradeSum = 0;         // ★貿易開放度（2026-08-15）：(輸出+輸入)/GDPの±50%帯
            double tradeRef = 0;
            int ex1820Ok = 0, ex1820N = 0, ex1870Ok = 0, ex1870N = 0, ec1820Ok = 0, ec1820N = 0, ec1870Ok = 0, ec1870N = 0;   // ★E-08 断面の計数
            int ex1920Ok = 0, ex1920N = 0, ecDummyOk = 0, ecDummyN = 0;   // ★E-09（経済は採点しない＝ダミーの計数）
            int ex1950Ok = 0, nr1950Ok = 0, ex1950N = 0;                  // ★1950断面（政体の exact/near・従属込み）
            foreach (var (tn, op) in TradeOpenness1913) if (tn == e.Nation) { tradeRef = op; break; }
            NationStanding? Find(IReadOnlyList<NationStanding> sec)   // 照合キー＝Nation（無ければ首都名＝旧データ互換）。主指標と全断面で同じ1本
            {
                foreach (var st in sec) if (st.Nation == e.Nation || (st.Nation.Length == 0 && st.Name == e.Capital)) return st;
                return null;
            }
            foreach (var r in completed)
            {
                var stands = r.StandingsAt1914 ?? r.Standings;   // 1934延長2026-07-19：照合はQ524（1914年1月1日）断面。524以下の走行は最終値＝従来互換
                int rank = 0;   // 列強軸：スコア降順の順位（1始まり）。同点は Score の大小のみで数える＝入力順に依存しない決定論
                if (Find(stands) is not { } s) continue;
                seeds++;
                // ★E-08 多断面（2026-09-05）：1820／1870 の断面があれば、その時点の導出政体とGDP順位を照合（主指標には合算しない）
                void Section(IReadOnlyList<NationStanding>? sec, Polity[]? okPol, int? okRank, ref int exOk, ref int exN, ref int ecOk, ref int ecN)
                {
                    if (sec is null || Find(sec) is not { } s2) return;
                    if (okPol is not null) { exN++; if (System.Array.IndexOf(okPol, s2.DerivedPolity) >= 0) exOk++; }
                    if (okRank is { } orank)
                    {
                        int g2 = 1; foreach (var st in sec) if (st.Gdp > s2.Gdp) g2++;
                        ecN++; if (System.Math.Abs(g2 - orank) <= GdpRankTolerance) ecOk++;
                    }
                }
                Section(r.StandingsAt1820, e.Exact1820, e.GdpRank1820, ref ex1820Ok, ref ex1820N, ref ec1820Ok, ref ec1820N);
                Section(r.StandingsAt1870, e.Exact1870, e.GdpRank1870, ref ex1870Ok, ref ex1870N, ref ec1870Ok, ref ec1870N);
                Section(r.StandingsAt1920, e.Exact1920, null, ref ex1920Ok, ref ex1920N, ref ecDummyOk, ref ecDummyN);   // ★E-09
                // ★1950断面（2026-09-21）：主指標と同じ照合規則（従属の許容つき）で exact/near を数える（主指標には合算しない）
                if (r.StandingsAt1950 is { } sec50 && e.Exact1950 is { } ok50 && Find(sec50) is { } s50)
                {
                    var (x50, n50) = PolityMatch(s50.DerivedPolity, s50.Suzerain, ok50, e.Near1950, e.SuzerainsOk1950 is not null, e.SuzerainsOk1950);
                    ex1950N++;
                    if (x50) ex1950Ok++;
                    if (n50) nr1950Ok++;
                }
                if (e.GreatPowerOk is { } gp)
                {
                    rank = 1;
                    foreach (var st in stands) if (st.Score > s.Score) rank++;
                    greatSeeds++; rankSum += rank;
                    if ((rank <= Coefficients.GreatPowerCountForFidelity) == gp) greatOk++;
                }
                // 主権軸：史実が従属（併合/保護国/属国）の国は、従属で終局していれば政体に関わらず一致（マイソールが英属国＝史実そのもの）。
                // 宗主同一性（監査2026-07-12 リ12）：exact＝史実の宗主（SuzerainsOk）に従属／near＝任意の宗主に従属＝「誰が奪ったか」の地理まで測る。
                // 属国離反の実働（2026-07-12）で宗主の入れ替わりが日常になったため、従属の有無だけの旧判定では勢力圏形成の忠実度が測れない。
                // ★★政体の照合を**導出政体**へ切り替え（2026-08-21・作者裁定「切り替えでよい」）。
                //   旧＝法と遷移で決まるラベル s.Polity ／ 新＝権力シェアからの読み出し s.DerivedPolity（PowerAffinity.PolityFromShares）。
                //   ★切替の根拠（実測）＝一致率75〜94%・不一致4件中3件は導出が史実に近い（英1815/清/西1900の「軍事独裁」ラベルは
                //     いずれも誤りで、導出は正しく議会/王権になる）。⚠旧基準の exact は labelExact として**同走で併記**＝過去測定との橋。
                var (isExact, isNear) = PolityMatch(s.DerivedPolity, s.Suzerain, e.Exact, e.Near, e.SubjugatedOk, e.SuzerainsOk);
                if (isExact) exact++;
                if (isNear) near++;
                if (PolityMatch(s.Polity, s.Suzerain, e.Exact, e.Near, e.SubjugatedOk, e.SuzerainsOk).Exact) labelExact++;
                var (m, a) = LawScore(e, s);
                lawOk += m; lawTot += a;
                // B-中7：人口成長倍率。帯が未定義／初期人口不明（PopGrowth=0＝建国スポーン等）は測らない。
                // 案Y（2026-08-21）：PopGrowth は併合の持ち込み人口を除いた新定義（SelfPlay 側）。旧定義は PopGrowthGross として平均のみ併記。
                if (e.PopGrowthOk is { } pg && s.PopGrowth > 1e-9)
                {
                    popSeeds++; popSum += s.PopGrowth; popGrossSum += s.PopGrowthGross;
                    if (s.PopGrowth >= pg.Lo && s.PopGrowth <= pg.Hi) popOk++;
                }
                // 08の70%計器（2026-07-31）＝経済規模：GDPの**世界順位**が史実±3位以内（順位ベース＝単位系に頑健）。
                if (e.GdpRank1913 is { } gr)
                {
                    int grank = 1;
                    foreach (var st in stands) if (st.Gdp > s.Gdp) grank++;
                    econSeeds++; econRankSum += grank;
                    if (System.Math.Abs(grank - gr) <= GdpRankTolerance) econOk++;
                }
                // 08＝領土（本国）：史実1914の本国州のうち何割を保有しているか。★征服で増えるぶんは不問（上振れは領土「以上」＝一致）
                if (e.HomeProvinces1914 is { } hp && hp > 0)
                {
                    double frac = (double)s.HomeProvinces / hp;
                    homeSeeds++; homeFracSum += frac;
                    if (frac >= HomeProvinceMatchFrac) homeOk++;
                }
                // 08＝領土（植民地）：**場所でなく規模**（作者裁定）＝史実の植民地数の±50%
                if (e.ColonyProvinces1914 is { } cp)
                {
                    // ★2026-08-28（作者裁定③）：個数→**質量**（Colony.Population単位）。州追加の細分化で「行を分けただけで
                    //   個数が増える」単位崩れの是正（スコア側 ColonyMassUnits と同じ物差し）。参照値 cp は旧単位（初期植民地の粒度）
                    //   だが、初期植民地の平均 Population≈1 なので同じ目盛りとして±50%帯で照合できる。s.ColonyMass=0（旧経路）は個数へ退避。
                    double colVal = s.ColonyMass > 0 ? s.ColonyMass : s.Colonies;
                    colSeeds++; colSum += colVal;
                    double lo = cp * (1.0 - ColonyCountTolerance), hi = cp * (1.0 + ColonyCountTolerance);
                    if (colVal >= lo - 1e-9 && colVal <= hi + 1e-9) colOk++;
                }
                // ★貿易開放度（2026-08-15・作者承認）：1913年の(輸出+輸入)/GDPの±50%相対帯（植民地と同じ作法）。
                //   存在意義＝Search再較正の目的関数に貿易を見えるようにする（現況≈1%＝全国未達が既知の出発点）。
                if (tradeRef > 1e-9)
                {
                    tradeSeeds++; tradeSum += s.TradeGdp;
                    if (s.TradeGdp >= tradeRef * (1.0 - TradeOpennessTolerance) - 1e-9
                        && s.TradeGdp <= tradeRef * (1.0 + TradeOpennessTolerance) + 1e-9) tradeOk++;
                }
            }
            if (seeds == 0) continue;   // 参照表にあるがバッチに現れない国（将来の国追加時の安全弁）
            double nr = (double)near / seeds;
            list.Add(new NationFidelity(e.Nation, e.Capital, seeds, (double)exact / seeds, nr,
                lawTot > 0 ? (double)lawOk / lawTot : double.NaN, AxesOf(e), VerdictOf(nr),
                popSeeds > 0 ? (double)popOk / popSeeds : double.NaN,       // B-中7：人口の一致率（独立指標・NaN＝未定義）
                popSeeds > 0 ? popSum / popSeeds : 0,                       //        実測の平均成長倍率（診断用）
                greatSeeds > 0 ? (double)greatOk / greatSeeds : double.NaN, // 列強軸：一致率（独立指標・NaN＝未定義）
                greatSeeds > 0 ? rankSum / greatSeeds : 0,                  //        実測の平均順位（診断用）
                greatExpected,                                              // ★どちら側の国か
                greatSeeds > 0 ? (double)greatOk / greatSeeds : double.NaN, //   その国の到達率（中身は GreatMatch と同じ・分けて集計するための写し）
                econSeeds > 0 ? (double)econOk / econSeeds : double.NaN,    // 08：経済規模の一致率
                econSeeds > 0 ? econRankSum / econSeeds : 0,                //        実測の平均GDP順位
                homeSeeds > 0 ? (double)homeOk / homeSeeds : double.NaN,    // 08：領土（本国）の一致率
                homeSeeds > 0 ? homeFracSum / homeSeeds : 0,                //        実測の平均保有率
                colSeeds > 0 ? (double)colOk / colSeeds : double.NaN,       // 08：領土（植民地）の一致率
                colSeeds > 0 ? colSum / colSeeds : 0,                       //        実測の平均植民地数
                tradeSeeds > 0 ? (double)tradeOk / tradeSeeds : double.NaN, // ★貿易開放度の一致率（2026-08-15）
                tradeSeeds > 0 ? tradeSum / tradeSeeds : 0,                 //        実測の平均開放度（診断用）
                (double)labelExact / seeds,                                 // ★旧ラベル基準の政体exact（併走）
                popSeeds > 0 ? popGrossSum / popSeeds : 0,                  // 案Y：旧定義（征服インフレ込み）の平均倍率（併走・診断用）
                Exact1820: ex1820N > 0 ? (double)ex1820Ok / ex1820N : double.NaN, Exact1870: ex1870N > 0 ? (double)ex1870Ok / ex1870N : double.NaN,   // ★E-08
                Econ1820: ec1820N > 0 ? (double)ec1820Ok / ec1820N : double.NaN, Econ1870: ec1870N > 0 ? (double)ec1870Ok / ec1870N : double.NaN,
                Exact1920: ex1920N > 0 ? (double)ex1920Ok / ex1920N : double.NaN,   // ★E-09
                Exact1950: ex1950N > 0 ? (double)ex1950Ok / ex1950N : double.NaN, Near1950: ex1950N > 0 ? (double)nr1950Ok / ex1950N : double.NaN));   // ★1950断面
        }
        return list;
    }

    /// <summary>1国×1断面の政体照合（主指標と1950断面が共有する1本の規則）。主権軸（裁定2・2026-07-10）＋宗主同一性（監査2026-07-12 リ12）：
    /// exact＝史実の政体、または従属かつ宗主が許容集合内（集合が null なら宗主不問）／near＝exact ∪ 近い政体 ∪ 任意の宗主に従属。</summary>
    public static (bool Exact, bool Near) PolityMatch(Polity polity, string suzerain, Polity[] exact, Polity[]? near, bool subjugatedOk, string[]? suzerainsOk)
    {
        bool subjugated = subjugatedOk && suzerain.Length > 0;
        bool isExact = (subjugated && (suzerainsOk is null || System.Array.IndexOf(suzerainsOk, suzerain) >= 0)) || System.Array.IndexOf(exact, polity) >= 0;
        return (isExact, isExact || subjugated || (near is not null && System.Array.IndexOf(near, polity) >= 0));
    }

    // ★貿易開放度の参照（2026-08-15・作者承認「テスト速度に影響しないなら追加」）：1913年の(輸出+輸入)/GDP＝第一次グローバリゼーションの物差し。
    // 出典＝Findlay & O'Rourke ほか通説の概数。9カ国＝8件以上なのでゲート対象（50%）。現況の実測≈1%＝全国未達が既知
    // ＝この次元の存在意義は**Search再較正の目的関数が貿易を見えるようにする**こと（TradeSpeed等の再導出はこの次元込みで）。
    public static readonly (string Nation, double Openness)[] TradeOpenness1913 =
    [
        ("イギリス", 0.45), ("フランス", 0.35), ("プロイセン", 0.37), ("ロシア", 0.16),
        ("アメリカ", 0.11), ("清", 0.08), ("日本", 0.31), ("オーストリア", 0.22), ("サルデーニャ＝ピエモンテ", 0.28),
    ];
    public const double TradeOpennessTolerance = 0.5;   // ±50%相対帯（領土・植民地と同じ作法）

    // ================== 1914年 史実参照表（全国・world.json 順） ==================
    // 略記：Abs=Absolutist Des=Despotic Sho=Shogunate CM=ConstitutionalMonarchy Rep=Republic
    //       BD=BourgeoisDictatorship PD=ProletarianDictatorship MD=MilitaryDictatorship
    // 主権喪失国（併合・保護国化＝印度諸邦/ポーランド/伊独小邦/中央アジア等）：ゲームは国を消さないため、
    // 政体軸は「存続した場合に史実の系が示した統治形」を Exact とし、真の史実（併合/属国化）は外交ログ（属国・州移転・敗戦）の監査で扱う（Note に明記）。
    // ★1950断面（2026-09-21裁定）は各行の2行目（Exact1950/Near1950/SuzerainsOk1950）＝根拠は行末の「// 1950：」。1950年1月1日の状態で置く
    //   （インド自治領の共和制化1950/1/26・トルコの政権交代1950/5 のように年内に変わる国は両方を exact にする＝清1912の作法）。
    public static readonly HistoricalEntry[] Table =
    [
        new("フランス", "パリ", [Polity.Republic], [Polity.BourgeoisDictatorship, Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],   // 1950：第四共和政（1946憲法・議会制共和）。近い＝ド・ゴール派の強い執行権志向（ブル独裁）
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Universal], EducationOk: [EducationLaw.Compulsory], SerfOk: [ReformLevel.Full],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "第三共和政。男子普選1848・フェリー義務教育1882・封建的負担は1789廃止。近い＝ブル独裁（ボナパルティズム）/立憲（オルレアン系）",
            PopGrowthOk: (1.25, 1.7),    // 人口帯拡張2026-07-25：1783約27.5M→1914約39.8M＝×1.45。欧州最遅の自然増（出生率の早期低下）。アルザス喪失1871は−4%＝帯内
            GreatPowerOk: true,          // 列強軸2026-07-27：1783も1914も列強（普仏戦争の敗北を挟んでも地位は失わない）
            Exact1920: [Polity.Republic], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Polity.Republic], GdpRank1820: 5, GdpRank1870: 6, GdpRank1913: 7, HomeProvinces1914: 7, ColonyProvinces1914: 10),   // 08の6次元2026-07-31：Maddison1913の総GDP順位7位／本土7州（アルザス喪失を除けば1914も本土保持＝領土安定国）
        new("日本", "江戸", [Polity.ConstitutionalMonarchy], [Polity.Despotic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ"],   // 1950：1947憲法＝象徴天皇の議会制立憲君主制。1952年4月まで米の占領（SCAP）＝米への従属もexact。近い＝君主が象徴に退いた議会制（共和）
            SuffrageOk: [Suffrage.Census], EducationOk: [EducationLaw.Compulsory], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            LandReformOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "明治立憲制（1890帝国議会＝納税制限選挙・男子普選は1925＝枠外）。学制1872→義務教育。身分解放1871。近い＝有司専制（超然内閣の権威側読み）。幕府存続は「稀に残る」枠＝nearに数えない",
            PopGrowthOk: (1.7, 2.4),    // 人口帯拡張2026-07-25：1783約26M→1914約53M＝×2.04。徳川停滞→明治の人口転換。本土領土は安定（朝鮮は属国＝別国のまま非算入）
            GreatPowerOk: true,        // 列強軸2026-07-27：日露戦争1905の勝利で列強入り＝1783は非列強だが1914は列強（台頭の代表例）
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.Shogunate], Exact1870: [Polity.Absolutist], GdpRank1820: 8, GdpRank1870: 8, GdpRank1913: 10, HomeProvinces1914: 7, ColonyProvinces1914: 1),   // 08の6次元2026-07-31：Maddison1913の総GDP順位は10位前後／本土7州（1914も保持＝領土安定国）
        new("イギリス", "ロンドン", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],   // 1950：議会主権の立憲王政（アトリー労働党内閣）。近い＝共和（1914表と同じ）
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            SerfOk: [ReformLevel.Full], CornLawsOk: [ReformLevel.None],
            LandReformOk: [ReformLevel.None],
            Note: "議会主権の立憲王政。1884第三次選挙法＝成人男子約6割（Census寄り・1918男子普選は境界＝両許容）。義務教育1880。穀物法撤廃1846＝保護なし",
            PopGrowthOk: (2.6, 4.3),    // B-中7：1783→1911に約×3.4（領土変化なしの純自然増）。穀物法撤廃後は小麦消費の約8割を輸入して人口を養った＝B-高1（扶養力に輸入食料が入らない）の主対象
            GreatPowerOk: true,        // 列強軸2026-07-27：全期間を通じ列強（1783は米独立直後の敗戦国だが即座に回復し以後は覇権）。**実測 seed1 で49位＝この軸を追加した直接の動機**
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 4, GdpRank1870: 2, GdpRank1913: 5, HomeProvinces1914: 5, ColonyProvinces1914: 21),   // 08：1913年は米独に抜かれて5位／本土5州（1914の連合王国＝安定）
        new("アメリカ", "ニューヨーク", [Polity.Republic], [Polity.BourgeoisDictatorship],
            Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],   // 1950：連邦共和制（トルーマン）。近い＝ブル独裁（1914表と同じ）
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Universal], EducationOk: [EducationLaw.Public, EducationLaw.Compulsory], MilitarismOk: false,   // 非軍国＝1914年も志願兵制（徴兵は1917参戦後）＋小さな常備軍＝軍国度は閾値未満のはず
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "連邦共和制。白人男子普選は1850年代までに定着。公教育（州営コモンスクール）。常備軍小＝非軍国",
            GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Republic], Exact1870: [Polity.Republic], GdpRank1820: 10, GdpRank1870: 3, GdpRank1913: 1, HomeProvinces1914: 3, ColonyProvinces1914: 2),   // 州数再導出2026-08-09：§13再配分でボストン→NY吸収＝開始州3（保持判定の分母を実州数へ・期待値再導出は許可済み）        // 列強軸2026-07-27：1914には世界最大の工業国＝列強（1783は辺境の新興国＝台頭の代表例）
        new("ロシア", "モスクワ", [Polity.Despotic], [Polity.Absolutist, Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.ProletarianDictatorship],   // 1950：ソ連＝共産党の一党独裁（スターリン）
            SuffrageOk: [Suffrage.None, Suffrage.Census], EducationOk: [EducationLaw.Religious, EducationLaw.Public], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "ツァーリ専制（官僚・軍による上からの統治）。1905十月詔書→ドゥーマ＝半立憲（near）。農奴解放1861（買戻金の残滓＝Limited許容）。ドゥーマ選挙は制限＝Census許容",
            GreatPowerOk: true, Exact1920: [Polity.ProletarianDictatorship], Exact1820: [Polity.Absolutist], Exact1870: [Polity.Absolutist], GdpRank1820: 3, GdpRank1870: 4, GdpRank1913: 4, HomeProvinces1914: 13, ColonyProvinces1914: 0),        // 列強軸2026-07-27：全期間を通じ列強（クリミア1856・日露1905の敗北を挟んでも地位は失わない＝人口と版図の慣性）
        new("プロイセン", "ベルリン", [Polity.ConstitutionalMonarchy], [Polity.Despotic, Polity.MilitaryDictatorship],
            Exact1950: [Polity.Republic, Polity.ProletarianDictatorship], SuzerainsOk1950: ["アメリカ", "イギリス", "フランス", "ロシア"],   // 1950：普は1947解体・独は分断＝西独（1949基本法・議会制共和）と東独（1949・SED一党独裁・首都は東ベルリン）の両方をexact。西は占領規約（1949-55）・東はソ連管理委＝米英仏ソへの従属もexact
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory],
            PensionOk: [ReformLevel.Limited, ReformLevel.Full], MilitarismOk: true,   // 軍国＝一般兵役義務(1814)＋軍事予算＝軍国度が閾値以上のはず
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "カイザーライヒ＝権威主義的立憲君主制（帝国議会＝男子普選/プロイセン三級選挙＝制限の二重制＝両許容）。義務教育の祖。ビスマルク年金1889＝世界初。軍国主義＝親軍",
            GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 6, GdpRank1870: 5, GdpRank1913: 3, HomeProvinces1914: 4, ColonyProvinces1914: 3),        // 列強軸2026-07-27：1783は5位相当の列強、1871統一後は欧州最強の陸軍国＝1914も列強
        new("オスマン", "イスタンブール", [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship], [Polity.Absolutist, Polity.Despotic],
            Exact1950: [Polity.Republic, Polity.BourgeoisDictatorship], Near1950: [Polity.MilitaryDictatorship],   // 1950：トルコ共和国。1950年1月は共和人民党の一党優位（1946複数政党化・同年5月の選挙で民主党へ交代）＝共和とブル独裁（ケマル主義の一党体制）の両方をexact。近い＝軍の後見
            SuffrageOk: [Suffrage.None, Suffrage.Census], EducationOk: [EducationLaw.Religious, EducationLaw.Public],
            LandReformOk: [ReformLevel.None],
            Exact1920: [Polity.Absolutist, Polity.MilitaryDictatorship], Exact1820: [Polity.Absolutist], Exact1870: [Polity.Absolutist], GdpRank1820: 12, GdpRank1870: 11, Note: "1908青年トルコ革命＝立憲復活→1913クーデタ＝CUP三頭の軍事支配（名目スルタン）＝両方exact。近い＝ハミト専制の存続。タンジマート＝世俗学校の併存",
            HomeProvinces1914: 8, ColonyProvinces1914: 0, GreatPowerOk: false),
        new("オーストリア", "ウィーン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist, Polity.Despotic],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ", "イギリス", "フランス", "ロシア"],   // 1950：第二共和政（1945）。1955国家条約まで米英仏ソの4国占領＝占領国への従属もexact
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Note: "二重帝国＝立憲君主制。チスライタニア男子普選1907（ハンガリー側は制限＝Census併存）。帝国国民学校法1869＝義務教育",
            PopGrowthOk: (1.8, 2.6),    // 人口帯拡張2026-07-25：ハプスブルク圏1783約23M→1914約51M＝×2.2。領土は概ね安定。分離独立で州を失う世界線は帯を割る＝それ自体が忠実度の信号
            GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 9, GdpRank1870: 9, GdpRank1913: 8, HomeProvinces1914: 11, ColonyProvinces1914: 0),        // 列強軸2026-07-27：二重帝国は最後まで列強（1866普墺の敗北・1878以降の相対的衰退を挟んでも五大国の一角）
        new("ネーデルラント", "アムステルダム", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],   // 1950：立憲王政（ユリアナ女王1948即位・1949インドネシアへ主権移譲）
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public], SerfOk: [ReformLevel.Full],
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Polity.ConstitutionalMonarchy], Note: "オラニエ立憲王政（1848憲法）。選挙権拡大1887/96＝制限（男子普選1917＝境界）。義務教育1900。ゲーム開始は連邦共和国＝共和残存はnear",
            HomeProvinces1914: 2, ColonyProvinces1914: 3, GreatPowerOk: false),
        new("スペイン", "マドリード", [Polity.ConstitutionalMonarchy], [Polity.MilitaryDictatorship, Polity.Absolutist],
            Exact1950: [Polity.MilitaryDictatorship], Near1950: [Polity.BourgeoisDictatorship],   // 1950：フランコ体制＝内戦に勝った将軍の独裁（1947王位継承法で名目は王国・フランコが終身の国家元首）。近い＝ファランヘ一党のブル独裁
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy, Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 11, GdpRank1870: 10, Note: "王政復古体制（カノバス体制＝寡頭立憲）。男子普選1890（カシキスモで形骸だが法は普選）。近い＝プロヌンシアミエントの世紀＝軍事独裁/カルリスタ絶対王政",
            PopGrowthOk: (1.6, 2.2), HomeProvinces1914: 4, ColonyProvinces1914: 2, GreatPowerOk: false),   // 人口帯拡張2026-07-25：1783約10.6M→1914約20.5M＝×1.93。本土領土は安定（植民地喪失は本土人口に非算入）
        new("サルデーニャ＝ピエモンテ", "トリノ", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy],   // 1950：イタリア共和国（1946国民投票で王政廃止・1948憲法）。近い＝サヴォイア立憲王政の存続（国民投票は54対46）
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Public, EducationLaw.Compulsory],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Note: "統一イタリア＝サヴォイア立憲王政（アルベルト憲章）。1912準男子普選。カザーティ/コッピーノ法＝公教育。近い＝マッツィーニ/ガリバルディの共和主義",
            GreatPowerOk: true, Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1870: 7, GdpRank1913: 9, HomeProvinces1914: 3, ColonyProvinces1914: 2),        // 列強軸2026-07-27：1861統一で列強入り＝8カ国中もっとも新参かつ最弱（定員7の1つ余りはここが外れるのが史実的に自然）
        new("清", "上海", [Polity.Republic], [Polity.Despotic, Polity.MilitaryDictatorship],
            Exact1950: [Polity.ProletarianDictatorship], Near1950: [Polity.MilitaryDictatorship, Polity.BourgeoisDictatorship],   // 1950：中華人民共和国（1949年10月建国・上海は同年5月に人民解放軍が占領）。近い＝国民党の訓政（党国体制＝ブル独裁）・軍閥（軍事独裁）
            LandReformOk: [ReformLevel.None],
            Exact1920: [Polity.MilitaryDictatorship, Polity.Republic], Exact1820: [Polity.Despotic], Exact1870: [Polity.Despotic], GdpRank1820: 1, GdpRank1870: 1, Note: "辛亥革命1911→1912中華民国＝共和がexact。近い＝清朝専制の存続（Despotic）・袁世凱の軍権支配（MD＝1914年の実態）。法は転換期＝軸なし",
            PopGrowthOk: (1.1, 1.9), HomeProvinces1914: 19, ColonyProvinces1914: 0, GreatPowerOk: false),   // B-中7：史実は約×1.45（実測sim×1.07＝過少）。太平天国の人口損失を含んでなお増加した＝低成長だが停滞ではない
        new("朝鮮", "漢城", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.Republic], Near1950: [Polity.ProletarianDictatorship, Polity.BourgeoisDictatorship],   // 1950：漢城＝大韓民国（1948・李承晩の大統領制・米軍政は1948に終了＝独立）。近い＝北の朝鮮民主主義人民共和国（1948・一党独裁）・李承晩の権威主義（ブル独裁）。朝鮮戦争は同年6月＝断面の後
            SubjugatedOk: true, SuzerainsOk: ["日本"], Note: "大韓帝国＝専制皇権（光武改革）→1910日本併合＝主権喪失。従属exact＝宗主日本（宗主同一性2026-07-12）・他宗主はnear", GreatPowerOk: false),
        new("マラータ同盟", "プーナ", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: ["イギリス"],   // 1950：プーナ＝インドのボンベイ州。1950年1月1日はインド自治領（英王を戴く立憲君主制）・1月26日に共和制＝両方exact。英の宗主権は1947に消滅したが、脱植民地化の仕組みが今は無い（2026-09-23裁定）＝英の属国のまま1950を迎える世界線を表で罰しない＝英への従属も数える
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "1818英併合＝主権喪失（第三次マラータ戦争）。藩王国的専制の残存をexact。列強化・常勝は逸脱＝要因果監査", GreatPowerOk: false),
        new("マイソール", "シュリーランガパトナ", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: ["イギリス"],   // 1950：マイソール州＝インドの一州（藩王は1950から州元首ラージプラムク）。インド自治領→1950年1月26日共和制＝両方exact。英の宗主権は1947に消滅したが、脱植民地化の仕組みが今は無い（2026-09-23裁定）＝英の属国のまま1950を迎える世界線を表で罰しない＝英への従属も数える
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "1799英征服→藩王国（ウォデヤル朝復位1881＝模範藩王国）。伝統君主制の存続をexact", GreatPowerOk: false),
        new("ハイデラバード", "ハイデラバード", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: ["イギリス"],   // 1950：1948ポロ作戦でインドへ併合（ニザームは1950から州元首）。インド自治領→共和制＝両方exact。英の宗主権は1947に消滅したが、脱植民地化の仕組みが今は無い（2026-09-23裁定）＝英の属国のまま1950を迎える世界線を表で罰しない＝英への従属も数える
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ニザーム藩王国＝英保護下の専制。1914も専制のまま＝exact", GreatPowerOk: false),
        new("シク連邦", "ラホール", [Polity.Despotic, Polity.Absolutist], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],   // 1950：ラホール＝パキスタン自治領（1947分離独立・英王を戴く立憲君主制・共和制は1956）。近い＝東パンジャーブ（シクの多数）を含むインドの共和制
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ランジート・シングの王国→1849英併合。専制王国をexact・ミスル合議制（カールサの伝統）＝共和をnear", GreatPowerOk: false),
        new("ビルマ", "ラングーン", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.Republic], Near1950: [Polity.MilitaryDictatorship],   // 1950：ビルマ連邦（1948独立・議会制共和・英連邦外）。近い＝軍の台頭（1958選挙管理内閣・1962ネウィン）
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "コンバウン朝専制→1885英併合＝主権喪失。伝統専制の存続をexact", GreatPowerOk: false),
        new("ネパール", "カトマンズ", [Polity.Despotic, Polity.MilitaryDictatorship], [Polity.Absolutist],
            Exact1950: [Polity.Despotic, Polity.MilitaryDictatorship], Near1950: [Polity.Absolutist, Polity.ConstitutionalMonarchy],   // 1950：ラナ家世襲宰相制の最末期（1950年11月に国王がインド大使館へ・1951年2月の王政復古）＝1914と同じ両方exact。近い＝王政復古（親政/立憲）
            SerfOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "ラナ家世襲宰相制＝軍門閥の実権支配（名目王）＝Des/MDの両方exact。主権は維持（英同盟）", GreatPowerOk: false),
        new("シャム", "バンコク", [Polity.Despotic], [Polity.Absolutist, Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship],   // 1950：タイ王国＝1932以降の立憲王政だが1947クーデタ→1948ピブーン首相の軍主導政権＝両方exact
            SuffrageOk: [Suffrage.None, Suffrage.Census], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "チュラーロンコーン大王＝上からの近代化専制（主権維持＝緩衝国）。奴隷制・賦役の廃止1905＝解放Full許容。立憲革命1932は先＝near", GreatPowerOk: false),
        new("西山朝", "ハノイ", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.ProletarianDictatorship], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],   // 1950：ハノイ＝仏連合のベトナム国（1949・バオダイ）が保持＝仏への従属をexact。ベトナム民主共和国（1945ハノイで独立宣言・共産党）が北部農村を支配＝プロ独裁もexact。近い＝バオダイの専制
            SubjugatedOk: true, SuzerainsOk: ["阮氏", "フランス"], Note: "史実は1802阮氏に滅亡→仏領インドシナ＝宗主は阮氏/フランスを許容。存続時は農民反乱由来の軍事専制＝Despotic", GreatPowerOk: false),
        new("カンボジア", "プノンペン", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Absolutist], SuzerainsOk1950: ["フランス"],   // 1950：仏連合の協同国（1949）＝シハヌーク王の王国（1947憲法）。完全独立は1953＝仏への従属をexact
            SubjugatedOk: true, SuzerainsOk: ["フランス", "シャム"], Note: "仏保護国1863＝王制存続（ノロドム王）。前史はシャム宗主権（1794以降）＝両許容。伝統王政をexact", GreatPowerOk: false),
        new("ラオス", "ヴィエンチャン", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Absolutist], SuzerainsOk1950: ["フランス"],   // 1950：仏連合の協同国（1949）＝シーサワンウォン王の王国（1947憲法）。完全独立は1953＝仏への従属をexact
            SubjugatedOk: true, SuzerainsOk: ["フランス", "シャム"], Note: "仏保護領1893＝ルアンパバーン王制存続。前史はシャム宗主権＝両許容。伝統王政をexact", GreatPowerOk: false),
        new("ペルシャ", "テヘラン", [Polity.Despotic, Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Absolutist, Polity.Despotic],   // 1950：パフラヴィー朝の立憲王政（議会マジュレス・1949憲法改正で国王権限拡大・1951モサッデク首相）。近い＝国王の親政
            Note: "カージャール専制→1906立憲革命→1911露介入で議会停止＝名目立憲・実態専制＝両方exact", GreatPowerOk: false),
        new("アラビア", "ディルイーヤ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic],   // 1950：サウジアラビア王国（1932・イブン・サウードの絶対君主制・独立）
            SerfOk: [ReformLevel.None],   // アラビア半島の奴隷制はサウジ1962廃止令まで存続＝1914年に解放済みなら逸脱
            SubjugatedOk: true, SuzerainsOk: ["オスマン"], Note: "第一次サウード国1818滅亡（オスマン＝エジプト遠征）→リヤド再興1902＝1914は名目オスマン宗主権下。伝統専制をexact", GreatPowerOk: false),
        new("イエメン", "サヌア", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic],   // 1950：ムタワッキル王国（1918独立・イマーム・アフマドの神権的君主制）
            SerfOk: [ReformLevel.None],   // イエメン1962共和革命まで奴隷制＝1914年に解放済みなら逸脱
            SubjugatedOk: true, SuzerainsOk: ["オスマン"], Note: "ザイド派イマーム国＝神権的君主制（1911ダアーン条約＝オスマン下の自治）", GreatPowerOk: false),
        new("オマーン", "マスカット", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: [],   // 1950：マスカット・オマーン＝サイード・ビン・タイムールのスルタン国。英の**正式な保護領ではない**（条約関係と政治代理官のみ＝湾岸の休戦諸国とは地位が違う・2026-09-23裁定）＝英への従属はexactから「近い」へ落とす（空集合＝従属はnearだけ数える）
            SerfOk: [ReformLevel.None],   // オマーン1970まで奴隷制＝1914年に解放済みなら逸脱
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ブーサイード朝スルタン国（英影響下＝事実上の英保護）", GreatPowerOk: false),
        new("アフガニスタン", "カブール", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic, Polity.ConstitutionalMonarchy],   // 1950：アフガニスタン王国（1919独立・ザーヒル・シャー＝1931憲法だが王族宰相の親政）。英の外交管理は1919に消滅＝従属は数えない
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "アブドゥッラフマーン以降の中央集権専制（鉄のアミール）。外交は英管理（ガンダマク条約）＝英宗主を許容", GreatPowerOk: false),
        new("ブハラ", "ブハラ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],   // 1950：1920ブハラ人民ソヴィエト共和国→1924ウズベクSSR＝ソ連の構成国（露への従属と一党独裁をexact）
            SubjugatedOk: true, SuzerainsOk: ["ロシア"], Note: "露保護国1873＝アミール制存続。伝統専制をexact", GreatPowerOk: false),
        new("ヒヴァ", "ヒヴァ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],   // 1950：1920ホラズム人民ソヴィエト共和国→1924ウズベク/トルクメンSSR＝ソ連の構成国
            SubjugatedOk: true, SuzerainsOk: ["ロシア"], Note: "露保護国1873＝ハン制存続。同上", GreatPowerOk: false),
        new("コーカンド", "タシュケント", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],   // 1950：タシュケント＝ウズベクSSRの首都＝ソ連の構成国
            SubjugatedOk: true, SuzerainsOk: ["ロシア"], Note: "1876露併合＝汗国消滅＝主権喪失。伝統専制の存続をexact", GreatPowerOk: false),
        new("モロッコ", "フェズ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス", "スペイン"],   // 1950：仏西保護領（1912-56）＝スルタン・ムハンマド5世の王制存続（1914と同じ）
            SubjugatedOk: true, SuzerainsOk: ["フランス", "スペイン"], Note: "アラウィー朝スルタン国→1912仏西保護領（王制存続）。伝統王政をexact", GreatPowerOk: false),
        new("エチオピア", "ゴンダール", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.Absolutist, Polity.Despotic], Near1950: [Polity.ConstitutionalMonarchy],   // 1950：ハイレ・セラシエの絶対君主制（1931憲法の議会は諮問的）。伊占領1936-41から回復＝独立
            SerfOk: [ReformLevel.None],
            Note: "メネリク2世＝集権化・近代化の専制（アドワ1896＝独立防衛に成功した非欧州国）", GreatPowerOk: false),
        new("マダガスカル", "アンタナナリヴ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.Absolutist, Polity.Despotic], SuzerainsOk1950: ["フランス"],   // 1950：仏海外領（1946・1947蜂起を鎮圧・独立1960）。1914表と同じく伝統王政の存続をexact
            SubjugatedOk: true, SuzerainsOk: ["フランス"], Note: "メリナ王国＝1896仏併合・王制廃止＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("アシャンティ王国", "クマシ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],   // 1950：英領ゴールドコースト（独立1957）。アシャンティ王は1935に英の間接統治の下で復位＝伝統王政をexact
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "アカン人の連合王国＝金とカカオの交易国家で常備軍を持つ。1824アサマンコで英軍を破り総督を討つが、5次に及ぶ英アシャンティ戦争の末1902英領ゴールドコーストへ併合＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        // ★アフリカ5カ国（アフリカ植民doc B〜F・2026-08-13）。いずれも伝統王政の存続をexactとし、列強にはならない。
        new("ソコト", "ソコト", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],   // 1950：英領ナイジェリア（独立1960）。ソコトのスルタンは間接統治の首長として存続
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "フラニのジハードが建てた19世紀最大のアフリカ国家（人口1000〜2000万）。1903年カノ・ソコト陥落まで英に屈しなかった＝主権喪失は世紀末。伝統王政の存続をexact", GreatPowerOk: false),
        new("コンゴ王国", "ンバンザ=コンゴ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["ポルトガル", "ベルギー"],   // 1950：ンバンザ=コンゴ＝葡領アンゴラ（独立1975）・旧領の北半はベルギー領コンゴ（独立1960）。名目王の系は葡の下で存続
            SubjugatedOk: true, SuzerainsOk: ["ポルトガル", "ベルギー"], Note: "1491年に王が改宗したアフリカ唯一のキリスト教王国。1783年は内戦後の衰退期で、1914年に葡が王制を廃止＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("アルジェ摂政国", "アルジェ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],   // 1950：仏領アルジェリア（本土の県・独立1962）。1914表と同じく専制の存続をexact
            SubjugatedOk: true, SuzerainsOk: ["フランス", "オスマン"], Note: "オスマンの名目宗主下の海賊国家。穀物の未払い債務を口実に1830年仏が侵攻（扇の一撃）＝主権喪失。それまでの専制をexact", GreatPowerOk: false),
        new("オヨ帝国", "オヨ=イレ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],   // 1950：英領ナイジェリア。オヨのアラーフィンは間接統治の首長として存続
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ヨルバの騎兵帝国＝1783年は全盛期だが1836年に内部崩壊する。崩壊は台本でなく不満と圧力から出るなら出る。専制の存続をexact", GreatPowerOk: false),
        new("ボルヌ帝国", "ンガザルガム", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス", "フランス"],   // 1950：英領ナイジェリア北東部と仏領チャド/カメルーンに分割。ボルヌのシェフは間接統治の首長として存続
            SubjugatedOk: true, SuzerainsOk: ["イギリス", "フランス"], Note: "カネム・ボルヌ＝9世紀から続くサハラ縦断交易の塩の帝国。完全内陸＝列強が届くのが最も遅い対照群。専制の存続をexact", GreatPowerOk: false),
        new("ハワイ王国", "ホノルル", [Polity.Absolutist, Polity.ConstitutionalMonarchy], [Polity.Despotic],
            Exact1950: [Polity.Absolutist, Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ"],   // 1950：米国のハワイ準州（州昇格1959）。1914表と同じく王制の存続をexact・近い＝準州の代議制（共和）
            SubjugatedOk: true, SuzerainsOk: ["アメリカ", "イギリス"], Note: "カメハメハの統一(1795)以降の独立王国。1840年憲法で立憲君主制へ移行し、1893年に米系入植者のクーデタで王制打倒＝主権喪失。立憲化と王制の存続をexact", GreatPowerOk: false),
        new("ダホメ王国", "アボメイ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],   // 1950：仏領西アフリカ（独立1960）。1914表と同じく伝統王政の存続をexact
            SubjugatedOk: true, SuzerainsOk: ["フランス"], Note: "フォン人の王国＝常備軍（女性親衛隊）を国制の中心に置いた交易国家。1890/1892仏ダホメ戦争→1894仏領＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("スウェーデン", "ストックホルム", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],   // 1950：立憲王政（社会民主党エルランデル内閣）
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.Limited, ReformLevel.Full],
            Note: "立憲王政（1866二院制）。男子普選1909。義務教育1842＝最初期", GreatPowerOk: false),
        new("デンマーク", "コペンハーゲン", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],   // 1950：立憲王政（1945解放後に議会制を回復）
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public], PensionOk: [ReformLevel.Limited, ReformLevel.Full],
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "六月憲法1849＝立憲王政・広範な男子選挙権。義務教育1814＝世界最初期。老齢扶助法1891＝最初期の年金", GreatPowerOk: false),
        new("スイス", "ベルン", [Polity.Republic], [Polity.BourgeoisDictatorship],
            Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],   // 1950：連邦共和制（1914表と同じ）
            SuffrageOk: [Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            Note: "1848連邦憲法＝連邦共和制・男子普選。義務教育1874", GreatPowerOk: false),
        new("ポーランド", "ワルシャワ", [Polity.ConstitutionalMonarchy, Polity.Republic], [Polity.Absolutist],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],   // 1950：ポーランド人民共和国（統一労働者党の一党独裁・ビェルト）。1949年11月にソ連のロコソフスキー元帥が国防相＝衛星国＝ソ連への従属もexact
            SubjugatedOk: true, SuzerainsOk: ["ロシア", "プロイセン", "オーストリア"], Note: "史実は1795第三次分割で消滅＝宗主は分割三国を許容。存続時は五月三日憲法1791（立憲）〜1918共和が史実の系", GreatPowerOk: false),
        new("ポルトガル", "リスボン", [Polity.Republic], [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship],
            Exact1950: [Polity.BourgeoisDictatorship], Near1950: [Polity.MilitaryDictatorship],   // 1950：サラザールの新国家（1933憲法・国民同盟の一党体制）。近い＝1926軍事クーデタ由来の軍の後見（大統領カルモナ元帥）
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Note: "1910革命＝第一共和政がexact。近い＝立憲王政の存続/軍部介入（1926は先）。第一共和政も識字要件＝制限選挙",
            PopGrowthOk: (1.5, 2.0), GreatPowerOk: false),   // 人口帯拡張2026-07-25：1783約3.4M→1914約6.0M＝×1.76。本土領土は安定（ブラジル喪失1822は植民地＝本土人口に非算入）
        new("ジェノヴァ", "ジェノヴァ", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],   // 1950：伊共和国の一部（1946）＝伊への従属と共和制をexact。近い＝王政の存続
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "1815サルデーニャ併合→統一イタリア＝立憲王政の一部がexact。寡頭共和国の伝統＝near", GreatPowerOk: false),
        new("ライン諸邦", "ケルン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],   // 1950：ケルン＝西独ノルトライン=ヴェストファーレン州（1949基本法）＝独への従属と共和制をexact
            SubjugatedOk: true, SuzerainsOk: ["プロイセン", "フランス"], Note: "仏併合（ナポレオン期）→1815普領ラインラント→独帝国＝宗主は普/仏を許容。小邦絶対主義の存続＝near", GreatPowerOk: false),
        new("両シチリア", "ナポリ", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],   // 1950：伊共和国の一部。1946国民投票で南部は王政支持が多数＝近い＝王政の存続
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "1861統一イタリアへ併合＝立憲王政の一部がexact。ブルボン専制の存続＝near", GreatPowerOk: false),
        new("教皇領", "ローマ", [Polity.Absolutist], [Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Republic], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],   // 1950：ローマ＝伊共和国の首都。教皇庁はラテラノ条約（1929）でヴァチカン市国の主権を持つ＝神権的絶対制をexact・伊への従属もexact。近い＝伊共和国の共和制
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "神権的絶対制（教皇庁は1870併合後も絶対制のまま）がexact。伊併合＝立憲王国の一部", GreatPowerOk: false),
        new("トスカーナ", "フィレンツェ", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],   // 1950：伊共和国の一部（1946）
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "1860併合＝統一イタリアの一部がexact。ハプスブルク＝ロレーナ家の開明専制の存続＝near", GreatPowerOk: false),
        new("ヴェネツィア", "ヴェネツィア", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],   // 1950：伊共和国の一部（墺の宗主権は1866に消滅＝宗主は伊のみ）
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ", "オーストリア"], Note: "1797共和国滅亡→墺領→1866統一イタリア＝宗主は伊/墺を許容。千年共和国の伝統＝near", GreatPowerOk: false),
        new("バイエルン", "ミュンヘン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],   // 1950：西独のバイエルン自由州（1949）
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "独帝国内の立憲王国（王冠・議会を保持した構成国）がexact", GreatPowerOk: false),
        new("ザクセン", "ドレスデン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["プロイセン", "ロシア"],   // 1950：東独（SED一党独裁）のザクセン州＝ソ連占領地区から成立＝独とソ連への従属をexact
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "独帝国内の立憲王国がexact", GreatPowerOk: false),
        new("ハノーファー", "ハノーファー", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],   // 1950：西独ニーダーザクセン州（1946）
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "1866普墺戦争で普併合→独帝国＝立憲帝政の一部がexact", GreatPowerOk: false),
        new("ヴュルテンベルク", "シュトゥットガルト", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],   // 1950：西独ヴュルテンベルク=バーデン州（1946・1952バーデン=ヴュルテンベルク）
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "独帝国内の立憲王国がexact", GreatPowerOk: false),
        new("阮氏", "サイゴン", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.Despotic, Polity.Absolutist], Near1950: [Polity.Republic], SuzerainsOk1950: ["フランス"],   // 1950：サイゴン＝仏連合のベトナム国（1949・元皇帝バオダイが国家元首）＝仏への従属をexact。近い＝1955ベトナム共和国
            SubjugatedOk: true, SuzerainsOk: ["フランス"], Note: "1802全越統一（阮朝）＝明命帝の中央集権専制→1883-85仏保護国化（王制存続）。中央集権専制をexact", GreatPowerOk: false),
        new("ラヘジ", "アデン", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],   // 1950：英アデン保護領のラヘジ・スルタン国（1914と同じ）
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ラヘジ・スルタン国＝英保護領（アデンは1839英直轄）。伝統首長制をexact", GreatPowerOk: false),
        new("デリー", "デリー", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: ["イギリス"],   // 1950：インドの首都。1950年1月1日はインド自治領・1月26日に共和制＝両方exact。英の宗主権は1947に消滅したが、脱植民地化の仕組みが今は無い（2026-09-23裁定）＝英の属国のまま1950を迎える世界線を表で罰しない＝英への従属も数える
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ムガル残滓→1857大反乱後に廃絶＝主権喪失。伝統専制の存続をexact", GreatPowerOk: false),
        new("アワド", "ラクナウ", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: ["イギリス"],   // 1950：ラクナウ＝インドの連合州（ウッタル・プラデーシュ）。インド自治領→共和制＝両方exact。英の宗主権は1947に消滅したが、脱植民地化の仕組みが今は無い（2026-09-23裁定）＝英の属国のまま1950を迎える世界線を表で罰しない＝英への従属も数える
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "1856英併合＝主権喪失。太守（ナワーブ）専制の存続をexact", GreatPowerOk: false),
    ];
}
