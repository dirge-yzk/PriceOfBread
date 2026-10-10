namespace PriceOfBread.SimCore;

using System;

/// <summary>第5段5-1（2026-07-27）政治量の唯一実装。旧＝同じ量（実効保持力coerce×4実装・challenge/coerce比2型・
/// 政体座標×3実装・9→3族写像×3箇所・平均満足×2実装）が複数実装で散在していた＝ここへ一元化（全呼び出しは従来と同時点＝挙動恒等）。
/// ⚠時点の統一（四半期頭で1回凍結し配る案）は実測で棄却：幕府転換・参政権の梯子外し・戦争分布・4カ国テスト世界の分離暴発が
/// 「今期値/前期値の同期」に依存しており（総政変224→286・169中7赤）、暗黙の1期ラグ混在はload-bearingと確定（2026-07-27記録）。</summary>
public static class PoliticalQuantities
{
    /// <summary>実効保持力＝Coercion×(人口/基準人口)（人口あたり強度＝大人口ほど統制に多くの力が要る）。旧4実装（Politics/CrownParliament/Crime/UI）の一元化。
    /// 結合順は旧実装どおり C×(P/ref) を厳守（(C×P)/ref はULP差→カオス系で軌道が割れ、chronicle同一性が壊れる＝2026-07-27実測）。</summary>
    public static double EffectiveCoerce(World w, Coefficients c)
        => w.Coercion * (w.TotalPopulation / Math.Max(1.0, c.PoliticalReferencePop));

    /// <summary>challenge/coerce 比の唯一の型（旧＝Clamp有無の2型が混在）。NaN は0側へ（否定形）。</summary>
    public static double ChallengeRatio(double challenge, double coerce)
        => Clamp01(challenge / Math.Max(1.0, coerce));

    /// <summary>イデオロギー11→3族の唯一の写像（0保守/1自由/2社会）。ファシズム(9)・王党派(10)＝保守側。</summary>
    public static int FamilyOf(int ideo) => ideo is 2 or 3 ? 1 : ideo is 5 or 6 or 7 ? 2 : 0;

    /// <summary>天命の母数＝文盲層（重み＝1−識字）の人口シェアと加重平均満足。エンジン（Politics天命）とUI（王権支持度の内訳）の唯一実装。
    /// 第5段5-3で導入（当初1−意識）→5-4の3層分割（文盲=1−ℓ／教育=min(意識,ℓ)／中間=ℓ−教育・和が厳密に1）で文盲層＝素朴王政観へ確定。
    /// sat は母数ゼロのとき -1（呼び手が全pop平均へ退避）。</summary>
    public static (double share, double sat) MandateBase(World w)
    {
        double size = 0, illit = 0, sat = 0;
        foreach (var p in w.AllPops())
        {
            size += p.Size;
            double u = p.Size * (1.0 - p.Literacy);
            illit += u;
            sat += u * p.Satisfaction;
        }
        return (size > 1e-9 ? illit / size : 1.0, illit > 1e-9 ? sat / illit : -1);
    }

    // ======================================================================================================
    // 軍国主義の実体（2026-08-18・作者裁定「親軍法は廃止したい。これは厳密には法ではなく政治機構だ」）
    // ------------------------------------------------------------------------------------------------------
    // 「軍国主義法」という単一の立法は1783-1934に存在しない（唯一の例は1946年の非ナチ化法＝軍国主義を**解体する**法）。
    // ⚠**棄却された第1案の墓標**＝「軍国度＝軍事費×徴兵法の深さ」。史学検証とエンジン実測の両方で棄却された（再点火禁止）。
    //   ・英1913＝徴兵深度0なのに国防費は中央政府支出の43%・軍事費/GDPは1870-1913平均2.6%＝独と同率（積は英を軍国度0と評価する）。
    //   ・ワイマール独1920-34＝ヴェルサイユで徴兵禁止・陸軍10万の志願制＝両因子とも最低。なのに国防軍は「国家の中の国家」で、
    //     カップ一揆で政府防衛を拒否し、シュライヒャーは1932年に首相になった。
    //   ・独50%対仏85%＝法はどちらも皆兵だが実際に徴集したのは独が適齢の約50%・仏は約85%（墺洪は約20%）
    //     ＝軍国の典型とされる独の方が「深度」が低い。
    //   ・日本1922-31＝山梨/宇垣軍縮で平時兵力の約1/3（約10万名）を削り、軍事費の歳出比は1913年33.5%→1930年28.5%が谷。
    //     **なのにこの10年で軍の政治的自律は逆に高まった**（1936年に47.6%へ）。積の式は1925年の日本を「軍国主義が下がった」と読む。
    //   ・決定実験＝独帝国と仏第三共和政の1913年は同じ高支出・同じ皆兵法なのに結果が正反対。ツァーベル事件で帝国議会が
    //     宰相不信任を293対54で可決しても何も起こらない（宰相は皇帝にのみ責任を負う）／フィッシュ事件では内閣が崩壊した。
    //     ⇒ **欠けている次元は「誰が軍の人事・司法・予算を握るか」**。
    //   ・エンジン実測（milgrade 6seed・1914断面）＝61カ国のほぼ全てが**厳密に0.350**（軍事費0.7×選抜0.5）へ張り付き、
    //     プロイセン＝ビルマ＝清＝デンマーク＝教皇領。**識別力がゼロ**だった。
    // ⇒ 軍国主義は単一のスカラーではなく**4つの別々の実体**である。以下はそれぞれ独立に導出する（全て既存量のみ）。
    //   ①軍の練度／②軍の政治的優位／③好戦世論の公共圏／④将校の任免権の所在。
    // ======================================================================================================

    /// <summary>徴兵法の**法定**深さ∈[0,1]（志願0／選抜0.5／皆兵1.0）。「法が最大どれだけ徴発してよいか」の上限であって実績ではない。
    /// ⚠実際に徴集される割合は予算が決める（独50%対仏85%＝同じ皆兵法で1.7倍の開き）。
    /// （旧＝実効徴兵深度 EffectiveConscriptionDepth＝min(法定深度, 養える兵数÷適齢人口)。呼び手0のまま＝作者裁定 2026-09-26（4b）で削除・回1-2）</summary>
    public static double ConscriptionDepth(ConscriptionLaw law) => law switch
    {
        ConscriptionLaw.Universal => 1.0,   // 国民皆兵（普1814の一般兵役義務・仏の徴兵制）
        ConscriptionLaw.Selective => 0.5,   // 選抜徴兵（籤・代人制つき＝旧体制の常態）
        _ => 0.0,                            // 志願兵制（英1916以前・米・1783の多く）
    };

    /// <summary>官僚制の専門化∈[0,1]（猟官0／資格任用0.5／専門1.0）。参謀組織・軍政の専門性の代理
    /// （プロイセン一般幕僚は文官制度と同じ専門化の産物＝シャルンホルスト改革と文官試験は同じ運動）。</summary>
    public static double BureaucracyDepth(BureaucracyLaw law) => law switch
    {
        BureaucracyLaw.Professional => 1.0,
        BureaucracyLaw.MeritExam => 0.5,
        _ => 0.0,
    };

    /// <summary>勤務年数の代理∈[0,1]（志願＝長期1.0／選抜0.6／皆兵＝短期0.4）。徴兵が広いほど1人あたりの在営年数は短い
    /// ＝個人技量は落ち、動員可能兵力は増える（英の長期志願兵は個人技量で最高／普仏の2-3年短期兵は数と参謀で戦う）。</summary>
    public static double ServiceLength(ConscriptionLaw law) => law switch
    {
        ConscriptionLaw.Universal => 0.4,
        ConscriptionLaw.Selective => 0.6,
        _ => 1.0,
    };

    /// <summary>国民の平均識字（人口加重）＝**唯一定義**（2026-09-11 M8＝旧 TechEducation.AvgLiteracy／CrownParliament のインライン和を吸収）。
    /// 集合は <see cref="World.AllPops"/> と同じ（Seceded を除く・State は見ない）・同じ順の加重平均＝ビット恒等。<paramref name="coreOnly"/>＝本国のみ（植民地格の州を除く・2026-08-26）。全 Size=0 で 0。
    /// 読み手＝<see cref="ArmyProfessionalism"/> の下士官層の項・WarFever・TechEducation（Elite/EliteCore）・CrownParliament（国民形成の天井）。</summary>
    public static double AvgLiteracy(World w, bool coreOnly = false)
    {
        double lit = 0, size = 0;
        foreach (var r in w.Regions)
        {
            if (coreOnly && r.Tier == RegionTier.Colonial) continue;
            foreach (var p in r.Pops) { lit += p.Literacy * p.Size; size += p.Size; }
        }
        return size > 1e-9 ? lit / size : 0;
    }

    /// <summary>
    /// ①**軍の練度**∈[0,1]＝識字 × 組織（専門官僚制・鉄道）× 勤務年数。<see cref="Coefficients.MilitarismStrengthBonus"/> の実効係数。
    /// ── 根拠 ────────────────────────────────────────────────────────────
    /// 軍の質の駆動因は参謀組織・鉄道・識字・下士官層であって「軍国主義法」ではない。露は皆兵＋巨額（1913年の陸軍費は独に次ぐ）でも
    /// 低質＝識字と下士官層の不在。英は志願長期勤務で個人技量が最高。普は短期兵だが参謀本部・鉄道・義務教育で質を作った。
    /// ── ⚠入れないもの（既存の墓標と二重計上の回避）─────────────────────────
    /// ・**装備充足・即応度・軍事費÷兵数を入れない**。<see cref="ArmyShape"/> の質は NeedMen の分母に入るので、
    ///   「質が落ちる→必要兵数が増える→1人あたり予算が薄まる→さらに質が落ちる」の正帰還が閉じる（旧 SoldierSustainDriven 凍結と同型の罠）。
    /// ・1人あたりの装備・給与の厚みは <see cref="Diplomacy"/> の戦力合成が既に MilitaryReadiness・装備充足として掛けている＝二重に掛けない。
    /// </summary>
    public static double ArmyProfessionalism(World w)
    {
        double lit = Clamp01(AvgLiteracy(w));
        // 組織＝専門官僚制（参謀本部・軍政の専門化）と鉄道（動員の物理）の相加平均。どちらか片方でも半分は立つ。
        double org = 0.5 * BureaucracyDepth(w.Bureaucracy) + 0.5 * (w.Railways ? 1.0 : 0.0);
        return lit * org * ServiceLength(w.ConscriptionLaw);
    }

    /// <summary>
    /// ②**軍の政治的優位**∈[0,1]＝軍の圧力 ×（1 − 文民制度の弁）。<see cref="Coefficients.MilitarismClout"/> の実効係数であり、
    /// 忠実度ハーネス（<see cref="HistoricalReference.MilitarismDegreeOk"/>）が「その国は軍国か」を判定する物差しでもある。
    /// ── 根拠 ────────────────────────────────────────────────────────────
    /// プロイセン将校団の貴族比率は1860年65%→1913年30%へ落ちたのに政治的性格は不変＝**金でも身分でもなく人事の弁**
    /// （軍事内閣が昇進・免職・懲戒を握る／名誉裁判所令1874／一年志願兵制度の社会的濾過）。
    /// 英は志願制なのに最も貴族的な将校団を持った（士官職売買1871年まで）＝軍の大きさとも徴兵とも無関係。
    /// Finer/Huntington の型＝「軍が政治に出るかは軍の圧力ではなく**文民制度がそれを吸収できるか**で決まる」。
    /// ── ⚠**将校は賃金では動かない**（イベリア＋ラテンアメリカ92件の検証・2026-08-18）─────────────
    /// スペインのアサーニャ軍制改革（1931）は本データセット最大のクーデタ（1932サンフルハダ→1936）を招いたが、
    /// **給与は一切削っていない**——士官団の約40%（約9,000名）が**満額俸給付きで**退役（21,000→12,373名・将官190→90名）、
    /// 16師団→8師団、軍事司法の最高会議を廃止し最高裁の一法廷（軍人判事4＋**文民判事2**）へ移管、
    /// 独裁期の戦功昇進約300件を遡及無効化、サラゴサ士官学校の閉鎖。効いたのは**人数・地位・自律・名誉**であって金ではない。
    /// 同型＝ブラジル1883-87（年金拠出義務化＋新聞発言の禁止＋文民大臣への従属）／スペイン1923（訴追の脅威）／
    /// ボリビア1934（司令部更迭）／ペルー1914（クーデタ2日前に参謀総長を罷免）。
    /// ⇒ **粛清・人事介入の記憶**を圧力に入れる。代理は既存ストック <see cref="World.MandateSpent"/>
    ///   （＝政府が反対を押し切った履歴＝敵対の蓄積器・b/MandateRefillQuarters×実行度で線形に薄れる）＝新しい状態を作らない。
    ///   粛清が次の反乱を生む連鎖（西1841→1843／1868→ラ・グロリオサ／1932→サンフルハダ、伯1892→1893／1904／1922-24→1930）は
    ///   このストックが減衰しながら次期の圧力に残ることで出る。
    /// ── ⚠**兵卒の物質的苦情はここに入れない**（階級による引き金の分岐）─────────────────
    /// 兵卒・下士官はほぼ例外なく物質的苦情で動く（チリ1931コキンボ＝30%減俸／ブラジル1910＝給与・給食・勤務時間／
    /// キューバ1933＝未払給与と昇進／スペイン1836＝給与3か月未払）。しかし**給与未払は単独ではほぼ決起を生まない**
    /// （純粋例はエルサルバドル1931のほぼ1件）＝通常は文民の政治綱領が上に乗る。
    /// ⇒ 兵卒の不満は本関数（将校の政治的自律）に入れず、**既存の経路**に流す：
    ///   兵士popの給与不足 → 満足度↓ → <see cref="World.MilitaryReadiness"/>（士気×装備充足）↓ → **戦力ゲージが痩せる**（＝規律・能力の低下）、
    ///   かつ闘争度として既存の反体制勢力へ流れ込む。**単独では軍政を起こさない**（インヴァーゴードン1931は将校が同じ減俸を受け入れて残ったので
    ///   指揮系統がなく政治的天井が存在しなかった／政治的要求に手を伸ばしたノアの反乱1797はパーカー以下29名が絞首された）。
    ///   ⚠<see cref="World.ArmyAllegiance"/> からは給与の乗算を撤去済み（2026-08-18）＝給与は**帰属の向き**を1ビットも動かさない。
    /// ── プレイヤー最適解の2つの穴（両方塞がっていること）─────────────────────────
    /// ・「軍事費を削れば軍は安全」＝**塞がる**（削減は解隊の脅威＝下の dissolution を立てる。加えて兵卒側の満足度・即応度も崩れる）。
    /// ・「金を積めば将校は黙る」＝**塞がる**（積むほど組織が大きくなり weight が育つ＝黙らせる向きではない）。
    /// ⚠**本関数が読まないのは「給与」**であって軍事費そのものではない。軍事費が効く経路は2つだけで、どちらも水準ではない：
    ///   (i) 兵数＝組織の大きさ／(ii) 現在値と目標の差＝解隊の脅威。**目標が現在値に追随している均衡では水準を変えても値は動かない**。
    /// ── ⚠比のガード ────────────────────────────────────────────────────
    /// 「軍の力 ÷ 文民の力」を素直に割ると**分母が痩せた瞬間に指標が万%へ跳ねる**（memory「比のガードは絶対値で書かない」）。
    /// 積の形（圧力 ×（1−弁））で書く＝単調性は同じで有界。既存の <see cref="PolicyPreferences.ArmyPower"/> も同じ ×(1−議会統制) の形。
    /// </summary>
    public static double MilitaryPoliticalRatio(World w) => MilitaryPush(w) * (1.0 - CivilianValve(w));

    /// <summary>②の**分子**（将校団が政治へ押し出す圧力）∈[0,1]。⚠<see cref="MilitaryPoliticalRatio"/> から切り出しただけで式は不変
    /// （計器 milgrade が分子と分母を別々に印字できないと、写像の誤りと世界の誤りを切り分けられない＝2026-08-18に2度誤った反省）。</summary>
    public static double MilitaryPush(World w)
    {
        var c = w.Coef;
        // --- 分子：将校団が政治へ押し出す圧力（4つの加法＝どれか1つが十分に高ければ軍は動く）。⚠**給与を読まない**（水準でなく組織と解隊が効く） ---
        double soldierShare = w.SoldierSizeK() / Math.Max(1.0, w.TotalPopulation);
        double weight = Clamp01(soldierShare / Math.Max(1e-9, Military.PeaceCapShareOfPop(c)));   // 組織の大きさ＝軍の社会的重み（平時上限の人口比で正規化・(145) 上限割合×標準の構成の働き盛りの男の割合）
        // ★レンジの実測（裁定S1・2026-09-21）＝**ここに書いてあった旧の予測は向きが逆だった**（書き直し）。
        //   旧の指数減衰の S は 中央0.0041／最大0.1417＝purge は実質ゼロの死項だった。θ を退役して在庫だけの門にすると
        //   S は 中央0.1196／最大0.2399（b 中央0.1804・隔離計器 seed1・604四半期）＝purge は 0.002 → **0.060（最大0.120）**＝重み0.5で軍国教義の線
        //   MilitaristDoctrineDegree=0.25（SelfPlay）に対し粛清項だけで最大48%を供給する＝**軍政が出やすくなる**方向。
        //   Clamp01 が効かないのは新旧どちらも同じで、変わるのは値の桁。較正上の相手は在庫の門（連打を止める）＝腕を分けて測る。
        double purge = Clamp01(w.MandateSpent);                                          // 文民が反対を押し切った履歴＝粛清・人事介入・自律の剥奪の記憶（アサーニャ1931型）
        // ★**組織そのものの消滅の脅威**（2026-08-18・欧露15事例）＝軍が「今の自分」より小さくされようとしている度合い。
        //   ノスケが1920-02-29にヴェルサイユの10万人上限を満たすためエアハルト海兵旅団とレーヴェンフェルト旅団の解散を命じたことが
        //   カップ一揆の直接の原因（旅団は「解散に抵抗することを決意した」）＝**粛清や人事介入より重い**。
        //   代理＝維持費の**目標が現在値を下回っている相対幅**（＝これから解隊される割合）。既存の Policy 目標と現在値だけで書ける。
        //   ⚠Nordlinger「群を抜いて最も一般的な介入動機は軍の団体的利害の防衛＝軍事予算・組織の自律・存続」の予算は
        //     **軍の予算であって将校の給与ではない**（ギリシャ1909のクーデタ団も増額された軍事予算と再編を要求し、給与への言及は無い）。
        double cur = w.MilitarySpending;
        // ★軍事費の段（作者裁定2026-09-12 R2）：目標は**施行と同じ1本**（<see cref="Government.MilitaryTotalTarget"/>）で読む。
        //   ⚠生の和のままだと、段化で目標0.70に対し施行は段0.75＝ずれ0.05／0.75 が居座り、解隊の脅威が**常時 +0.067**
        //     ＝軍国教義の閾値（SelfPlay.MilitaristDoctrineDegree=0.25）の27%を丸め誤差が自己供給する正のフィードバックになる。
        double target = Government.MilitaryTotalTarget(w, w.Policy);
        double dissolution = cur > 1e-9 ? Clamp01((cur - target) / cur) : 0;
        // 対外屈辱＝敗戦ショック（減衰つき）＋失地ストック。失地の換算は既存の較正済みスケール（LostLandNationalism）を再利用＝新しい数を置かない。
        double humiliation = Clamp01(w.DefeatShock + c.LostLandNationalism * w.LostRegionsCumulative);
        // 解隊は満額・粛清は半分＝作者裁定「組織そのものの消滅の脅威を、粛清や人事介入より重く置くこと」。
        return Clamp01(weight + dissolution + 0.5 * purge + humiliation);
    }

    /// <summary>②の**分母**（文民の弁＝軍そのものを握れているか）∈[0,1]。⚠切り出しただけで式は不変。</summary>
    public static double CivilianValve(World w)
    {
        // --- 分母：文民の弁＝**軍そのものを握れているか**（国家の一般的な近代性ではない）---
        // ⚠旧実装は 参政権・官僚制の専門化・議会統制力・正統性 の相加平均で、**milgrade 6seed の1914断面で不合格になった**（2026-08-18実測）：
        //   ・プロイセン平均0.204 が **デンマーク0.473・スウェーデン0.473・教皇領0.399・ネーデルラント0.265 より下**に沈んだ。
        //   ・参照2国が分かれる位置（0.13）に閾値を置くと**71カ国中およそ60カ国が軍国**になり、分類が逆向きに空洞化する。
        //   ・欧州検証で確定した硬性条件「英米蘭白丁諾瑞西の軍部シェア≈0」が、丁0.473・瑞典0.473・蘭0.265 で全滅していた。
        // 原因＝旧式の2項が「軍の統制」ではなく「国家の近代性」を測っていたこと。
        //   ①参政権：帝国議会は**男子普通選挙**（当時の欧州で最も広い）だったのに軍の政治的優位は最大だった。しかも本クラスが上に記録している
        //     決定実験そのものが「独帝国と仏第三共和政は**同じ皆兵法・同じ高支出・同じ参政権**なのに結果が正反対」＝参政権は識別子になりえない。
        //   ②官僚制の専門化：プロイセン一般幕僚は文官試験と同じ専門化運動の産物であり、**専門化と軍の優位は両立していた**
        //     （軍が文民官僚機構の**外**にあったから＝憲法64条の軍旗宣誓が「憲法」ではなく皇帝個人へ向いていた）。
        //     加えて BureaucracyDepth は ①ArmyProfessionalism の分子に既に入っている＝**同じ量を両符号で二重に使っていた**。
        // ⇒ 弁は**任免の所在**（軍を誰が握るか＝3大陸の検証で「帰属を決めるのは金でも身分でもなく任免」と確定）と
        //   **正統性**（Finer/Nordlinger の機会項＝正統性の減価が機会を作る／Nordlinger は「政治化された層における正統性の減価」と限定する）の2項だけにする。
        //   ⑤同じ軸の両符号は維持される：任免が文民にある＝弁が開く（Huntington の客観的統制）／その自律が脅かされる＝分子の purge・dissolution。
        return Clamp01(0.5 * ((1.0 - AppointmentAtCourt(w)) + Clamp01(w.Legitimacy)));
    }

    /// <summary>任免が宮廷にある度合い∈[0,1]（君主がいて議会統制が弱いほど1／共和政は0）。
    /// ②の弁と④の向きが**同じ量**を読むための唯一実装（旧＝両者が別々に同じ式を書いていた）。</summary>
    public static double AppointmentAtCourt(World w)
        => w.Crown != CrownForm.None ? 1.0 - Clamp01(w.ParliamentControl) : 0.0;

    /// <summary>
    /// ③**好戦世論の公共圏**∈[0,1]＝報道自由度 × 識字 × 都市化 × 対外屈辱。<see cref="Coefficients.WarFeverMilitarism"/> の実効係数。
    /// ── 根拠 ────────────────────────────────────────────────────────────
    /// 1914年8月の熱狂は**都市の教育ある中産層と学生**に限られ、労働者と農村は静かだった。徴兵の無い英で1900年の南阿主戦熱、
    /// 常備軍10万未満の米で1898年の戦争熱（黄色新聞）＝**軍の大きさでも徴兵の有無でもなく、燃えうる公共圏の大きさ**が決める。
    /// 1930年の統帥権干犯を焚きつけたのも軍ではなく野党（政友会）と新聞だった。
    /// ⚠燃料は屈辱＝屈辱が無ければこの項は0で、平時の対外的な食欲は既存の <see cref="Coefficients.WarFeverThreat"/>（緊張）・
    ///   <see cref="Coefficients.WarFeverNoble"/>（将校貴族）・<see cref="Coefficients.WarFeverSoldier"/>（兵士規模）が担う＝二重計上しない。
    /// </summary>
    public static double WarFeverPublicSphere(World w)
    {
        double press = Politics.PressFreedom(w.Censorship);          // 検閲＝新聞が焚きつけられない
        double lit = Clamp01(AvgLiteracy(w));                        // 読者がいなければ新聞は燃えない
        double urban = Clamp01(w.UrbanShare);                        // 群衆は都市にいる
        double humiliation = Clamp01(w.DefeatShock + w.Coef.LostLandNationalism * w.LostRegionsCumulative
                                   + (w.TreatyPortsGranted > 0 ? 1.0 : 0.0));   // 敗戦・失地・不平等条約（条約港）＝火種
        return press * lit * urban * humiliation;
    }

    /// <summary>
    /// ④**将校の任免権の所在**∈[−1,1]（＋＝君主が任免する／−＝内閣が任免する／0＝どちらでもない＝軍が自分の主）。
    /// <see cref="Coefficients.ArmyMilitarismAffinity"/> の実効係数で、<see cref="World.ArmyAllegiance"/> の向きを決める。
    /// ── 根拠（旧実装は符号が逆だった）──────────────────────────────────
    /// 常備軍こそ王の治安維持力の道具であり、民兵・皆兵は共和派の対案だった（1697-99英の常備軍論争→ジョレス『新しい軍隊』1911→スイス憲法第13条の常備軍禁止）。
    /// しかも**軍が王政を倒す例が反復する**（ブラジル1889・ポルトガル1910・スペイン1931）＝「軍国的なら王権寄り」は成り立たない。
    /// 独将校団が王党派だったのは軍国度ではなく**憲法第64条の軍旗宣誓が「憲法」ではなく「皇帝個人」に向いていた**から
    /// ＝任免権が誰にあるかがそのまま忠誠の向きになる。
    /// </summary>
    public static double OfficerAppointmentCrownward(World w)
    {
        // ⚠宮廷側は <see cref="AppointmentAtCourt"/> と**同一実装を共有**する（②の弁と④の向きが同じ量を読む＝旧は同じ式が2箇所にあった）。
        return AppointmentAtCourt(w) - Clamp01(w.ParliamentControl);      // 議会統制が強いほど内閣側へ（英のシビリアンコントロール／共和政の陸軍大臣）
    }

    // ---- 以下は「軍国主義をどう測るか」の物差し（作者裁定2026-08-18 第5項/第6項）。⚠読み手なし＝計器（milgrade）にも効果にも未配線の申し送り ----

    /// <summary>軍事費の**中央歳出シェア**＝（軍需調達＋兵士給与）÷ 全請求権。作者裁定2026-08-18の第5項。
    /// ⚠水準（GDP比・絶対額）ではなくシェアで測ると、中南米68%・日本1936年47.6%・英1913年43%・日本1930年28.5%が同じ物差しに乗る
    /// （水準では中南米が最小・シェアでは最大＝史実の向き）。分母には全請求権を入れる＝分母が痩せて跳ねるのを防ぐ。</summary>
    public static double MilitaryBudgetShare(World w)
    {
        double mil = Math.Max(0, w.LastMilitaryClaimPerMonth) + Math.Max(0, w.LastSoldierPayPerMonth);
        double other = Math.Max(0, w.LastAdminClaimPerMonth) + Math.Max(0, w.LastInterestPerMonth);   // （連合維持費は 2026-09-30 (135) に撤去＝行政と兵力の購買の1本）
        double tot = mil + other;
        return tot > 1e-9 ? Clamp01(mil / tot) : 0;
    }

    /// <summary>NaN を0側へ倒す [0,1] クランプ（否定形＝<see cref="Math.Clamp"/> は NaN を消さない＝memory「NaNとfail-openガード」）。</summary>
    internal static double Clamp01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;   // −0.0 も +0.0 へ（規定形＝Ideologies の旧 `!(x >= 0)` 形を吸収・2026-09-11）

    /// <summary>現政体の4軸座標（参加軸を実参政権へドリフト済み）。旧3実装（Politics×2＋CrownParliament）の一元化。返り値は呼び手専有（変更可）。</summary>
    public static double[] DriftedPolityCoord(Polity polity, Suffrage suffrage, Coefficients c)
    {
        var coord = (double[])Ideologies.BasePolityCoord(polity).Clone();
        coord[0] += c.PolityCoordDriftWeight * (Ideologies.Openness(suffrage) - coord[0]);
        return coord;
    }

    /// <summary>★政治資本の最大量 b（作者裁定N2・2026-09-21）＝「どれだけ大きなことができるか」＝支持と権力の大きさ。
    /// <code>
    /// 支持 sup ＝ (現政体の親和 − Ideologies.AffMin) / (AffMax − AffMin) ∈[0,1]   ★正規化は PowerShares.Closeness と同じ既存の書き方
    /// b        ＝ MandateWeight × MandateSpendRate × (正統性 ＋ sup)
    /// </code>
    /// ★**目盛りの根拠**＝代価と同じ2係数の積で作る＝単位が一致する（代価＝|純支持|×MandateSpendRate×後退倍率）。
    /// ★**床の保証は廃止された**（裁定S2・2026-09-21・旧記述を書き直し。消さないのは「なぜ無くなったか」が読めなくなるから）。
    ///   旧記述＝「1歩の最大の代価は cost_max＝MandateWeight×(正統性−0.5)×MandateSpendRate×押し予算×憲法級倍率 なので
    ///   b/cost_max ＞ 1＝満タンなら必ず最も高い一歩を1回は払える」。この不等式は**余白の門 θ が代価を頭打ちにしていたから**成り立っていた。
    ///   ⇒ 実測（2026-09-21 ㉓検証）で代価上限/b は平均0.103・最大0.162＝満タンは常に最高値の一歩を6本以上払え（1/0.162≈6.2）、
    ///     在庫の門は selfplay-batch 10 の4腕と eduprobe で**一度も噛まなかった**＝裁定の「支持の薄い独裁は大改革を押し切れない」の否定そのもの。
    ///   ⇒ S1 で θ を退役（<see cref="Government"/> の MandateMargin が非憲法級に +∞ を返す）＝代価の頭打ちが消え、
    ///     門の可否は <c>|純支持|×後退倍率 ＞ MandateWeight×(正統性+sup)</c>（MandateSpendRate は両辺で消える）で決まる。
    ///     実測（隔離計器・seed1・604四半期）では置こうとした一歩の 70.1% が**満タンでも払えない**＝b の小さい国は永久に通せない＝S2 が望んだ姿。
    ///   ⚠P4（進行不能にしない）の担保は床の保証ではなく**門の置き場所**＝門は純支持&lt;0 の枝の中にしかない
    ///     （<see cref="Government.PoliticalCapitalAffords"/> の注記）＝代価0の一歩は S がいくつでも必ず通る。
    ///   MandateWeight に比例するので「在庫ハンマー」（AiFirstWaveTests の MandateWeight=50＝b≈30）でも b が同時に膨らむ。
    /// ★**政体で分岐を書かない**（P2）：支持は <see cref="Ideologies.National"/> の集計で、POP の重みが
    ///   <c>規模×意識×(IdeologyVoiceFloor+0.3×FranchiseVoice)</c>＝発言権による重み付けそのもの。
    ///   <see cref="InterestGroups.FranchiseVoice"/> は閉鎖階級（貴族・聖職）に常に1・参政権の無い大衆に0を返すので、
    ///   専制では発言権が旧支配層に集中し（民衆が不満でも b は大きい）、普通選挙では大衆へ移る（有権者の支持に連動する）＝座標系から出る。
    /// ★現政体の支持そのものは <see cref="CrownParliament"/> の連続動学と政治タブ（PolityAttractorTip）が既に同式で持っている量＝新しい物差しを足さない。</summary>
    public static double PoliticalCapitalMax(World w)
    {
        var c = w.Coef;
        double aff = Ideologies.SupportAt(Ideologies.National(w), DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
        double sup = Clamp01((aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin));
        double b = c.MandateWeight * c.MandateSpendRate * (w.Legitimacy + sup);
        return !(b > 0) ? 0 : b;   // 否定形＝NaN も0側へ（Math.Max は NaN を除去しない）
    }

    /// <summary>★政治的な実行度 ∈(0,1]（作者裁定N2・2026-09-21）＝「どれだけ頻繁に決められるか」。**制度の摩擦だけ**から作る。
    /// <code>
    /// 摩擦   ＝ 議会の実効統制 × 議席の分裂 ＋ 議会の実効統制 ＋ (内戦中 ? 1 : 0)
    /// 実行度 ＝ 1 / (1 + 摩擦)
    /// </code>
    /// ⚠**不満・闘争度では駆動しない**（P9）：それらは既に支持を通して b を縮めており、速度側にも入れると同じ原因が2度効く。
    /// ⚠**王権と議会の協力度（<see cref="World.CoopParliamentCrown"/>）は使わない**＝作者の列挙のうちこの1項だけ差し替えた（裁定を求める）。
    ///   理由：協力度は内側に <c>−CoopConflictMemoryWeight×Min(1,MandateSpent)</c> を持つので、(i) S が回復速度にも効いて P9 を破り、
    ///   (ii) 回復が S の関数になって**線形でなくなり**「実行度が最大なら40四半期」の名乗りが嘘になる。
    ///   拒否権の実体は <see cref="PowerShares.ParliamentEffective"/>（＝議会が実際に差し止められる歳入の割合）が代表する。
    /// ★**政体で分岐を書かない**（P2）：非議会政体は ParliamentControl≈0 で2項とも自然に消え、実行度が1へ寄る。
    /// ★摩擦 ≤ 1×(2/3) + 1 + 1 ＝ 2.667 ⇒ 実行度 ≥ 0.273 ＞ 0＝**0の吸収状態を作らない**（最悪でも 40/0.273 ≈ 147四半期で満タン）。</summary>
    public static double PoliticalCapitalRate(World w)
    {
        double pe = PowerShares.ParliamentEffective(w);
        // 議席の分裂＝1 − 最大会派の議席シェア（maxSeat の形は Politics の一党支配判定と同じ既存式）。
        // ⚠比のガードは絶対値で書かない＝分母（議席の総和）が痩せたら項ごと0（議会が無い/選挙前は分裂も無い）。
        double seats = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];
        double maxSeat = Math.Max(w.PartySeats[0], Math.Max(w.PartySeats[1], w.PartySeats[2]));
        double split = !(seats > 1e-9) ? 0 : Clamp01(1.0 - maxSeat / seats);
        double friction = pe * split + pe + (w.InCivilWar ? 1.0 : 0);
        // ⚠pe も split も構成上 NaN になり得ない（ParliamentEffective は `!(v>=0&&v<=1)?0:v`・Clamp01 も否定形）。
        //   それでも否定形で書く＝摩擦が数でなければ摩擦なし扱いにせず、回復量そのものを 0 に倒すのは呼び手（Politics.Step）の否定形ガード。
        return !(friction > 0) ? 1.0 : 1.0 / (1.0 + friction);
    }
}
