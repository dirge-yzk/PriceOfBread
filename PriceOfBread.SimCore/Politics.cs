namespace PriceOfBread.SimCore;

/// <summary>
/// 政治（§9-E）＋外圧（§A-11）。四半期に意識・闘争度を更新し、旗の力を集計、
/// 反体制の旗が治安維持力を超えたら政体が転換する（ムーア機構：強い旗が型を決める）。
/// 政体→参政権・自由度を算出（段2 のスカラー仮置きを実体化）。PoliticsEnabled で段0〜2 は不動。
/// 国家レベル＝全地域の pop を集計する。
/// </summary>
public static class Politics
{
    // マジックナンバー一掃（2026-07-20・値据え置き・命名のみ）：天命による正統性増幅の対称クリップ境界（安全弁）
    private const double DynMandateMulMin = 0.2, DynMandateMulMax = 1.8;

    /// <summary>探究/革新の自由度（倍率排除②）＝政体ラベルでなく**法律状態（参政権＋報道）から創発**。
    /// 世界の技術水準閾値(0.6)は**政治的開放度と報道の自由の両方**を要する＝検閲する社会は形式的参政権があっても先端で革新できない（採用・模倣に留まる＝閉じた社会の停滞）。
    /// 自由主義革命（立憲君主制/共和）は <see cref="FoundingPress"/> で報道を解放するので閾値を越える。検閲を保つ独裁（ブルジョア/プロレタリア独裁）は越えない。
    /// 係数は制約解：Census+Censorship≥0.6（立憲）・Census+StatePress&lt;0.6（ブルジョア独裁）・AdultMale+FreePress≥0.6（共和）・AdultMale+StatePress&lt;0.6（プロ独裁）・None+FreePress&lt;0.6（無参政権は報道自由でも革新せず）。</summary>
    public static double FreedomFromLaws(Suffrage suffrage, CensorshipLaw press)
    {
        double political = suffrage switch
        {
            Suffrage.Universal => 0.65,
            Suffrage.AdultMale => 0.55,
            Suffrage.Census => 0.50,
            _ => 0.20,                            // None＝閉鎖（参政権なき社会は報道自由でも先端革新に届かない）
        };
        return PoliticalQuantities.Clamp01(political + 0.25 * PressFreedom(press));   // 報道自由（単一ソース）＝先端の知の流通。検閲＝情報が流れない
    }

    /// <summary>報道自由度∈[0,1]（**政治側の**単一ソース・倍率排除/長い裾）。国家報道のみ=0／検閲付=0.6／自由報道=1.0。
    /// Freedom式（先端革新の解禁）と政治意識の press 倍率が**この一実数を共有**＝検閲法→効果の3値表を一本化。
    /// ⚠ここは政治側だけの単一ソースである（2026-08-12に監査で判明・旧コメントは「単一ソース」とだけ書いて誤解を招いていた）。
    /// **技術側は別テーブル**（<see cref="TechEducation"/> の 0.5／0.75／1.0）。理由は床の有無＝政治では国家統制の
    /// 報道自由度は 0 でよいが、技術で 0 を掛けると初期が国家統制の国（1783年の大半）が吸収も革新も完全停止して
    /// 序盤で壊滅する。技術側は「検閲は技術を半減まで落とす」という別の物理を持つので、値を無理に一致させない。</summary>
    public static double PressFreedom(CensorshipLaw press) => press switch
    {
        CensorshipLaw.FreePress => 1.0,
        CensorshipLaw.Censorship => 0.6,
        _ => 0.0,                                // StatePressOnly＝国家報道のみ
    };

    /// <summary>政体が建国時に解放する報道の自由（倍率排除②）。代議制（立憲君主制/共和）は報道を自由化＝革新世界の技術水準が立つ。
    /// 独裁・権威主義は検閲を保つ＝形式的参政権があっても閉じた社会で先端革新に届かない。</summary>
    public static CensorshipLaw FoundingPress(Polity p) => p switch
    {
        Polity.Republic => CensorshipLaw.FreePress,             // 共和＝自由報道（合衆国修正1条型）
        Polity.ConstitutionalMonarchy => CensorshipLaw.Censorship,   // 立憲君主制＝比較的自由な報道（英型）
        _ => CensorshipLaw.StatePressOnly,                      // 独裁・権威主義＝検閲（floor-raise なし＝閉じたまま）
    };

    /// <summary>政体が含意する参政権（利益集団の実効発言権）。立憲君主制＝制限選挙・共和/プロ独裁＝成人男性。
    /// 法律の参政権（World.Suffrage）とこれの大きい方が実効値＝政体だけで一定の包摂が起き、法でさらに広げられる。</summary>
    public static Suffrage PolitySuffrage(Polity p) => p switch
    {
        Polity.Republic => Suffrage.AdultMale,
        Polity.ProletarianDictatorship => Suffrage.AdultMale,
        Polity.ConstitutionalMonarchy => Suffrage.Census,
        Polity.BourgeoisDictatorship => Suffrage.Census,
        _ => Suffrage.None,                      // 絶対君主・専制・幕府・無政府＝参政権なし
    };

    private static double ConsciousnessConversion(EducationLaw law) => law switch
    {
        EducationLaw.Public => 1.0,
        EducationLaw.Compulsory => 1.0,
        EducationLaw.Religious => 0.15,
        _ => 0.1,
    };

    /// <summary>政15：識字→政治意識の変換係数を「今期の教育法」でなく**popの就学履歴ストック**（Literacy蓄積と同型の緩和）から読む。
    /// 現行は法改正の瞬間に成人の変換係数が 0.15→1.0 へ跳ぶ（数十年前に教育を受けた成人まで即時）＝年齢コホート監査（テーマA）と同根の粒度問題。
    /// ConsciousnessFromSchoolingStock=0 では即時値（今期の法）をそのまま返す（**現行既定は1.0点火＝戻り値は完全にストック**・02断線走査2026-07-28注記是正）。>0 で **pop.SchoolingConvStock**（政15の専用フィールド・-1=未捕捉）を
    /// 就学履歴ストックとして、現行の即時値と (1−これ) : これ で混ぜ、ストックは今期の変換係数へ緩和更新（世代遅れ＝法改正が変換に効くまで数年）。
    /// ※嘘コメント是正（作業リスト2第2段2-6・2026-07-27）：旧記述「pop.EducationEma を使う」は誤り＝実装は当初から SchoolingConvStock（EducationEma は人13の高等教育ゲート用＝別物・本経路は触らない）。</summary>
    private static double SchoolingConversion(Pop p, double convertNow, Coefficients c)
    {
        if (c.ConsciousnessFromSchoolingStock <= 0) return convertNow;   // 0＝即時（今期の法）＝旧式（現行既定1.0）
        // 就学履歴ストック（緩和）：今期の変換係数へゆっくり寄る＝過去の教育法の名残が残る（成人の識字→意識変換が跳ばない）。
        // 未捕捉＝-1センチネル（SchoolingConvStock の初期値）→初回は convertNow で埋める＝立ち上がりの跳躍を作らない。
        double stock = p.SchoolingConvStock;
        if (stock < 0) stock = convertNow;   // -1＝未捕捉（初回）→即時値で初期化
        stock = p.GenerationalMix(stock, convertNow, Calendar.MonthsPerQuarter);   // ★世代の流れ（2026-09-30 段4e 後半 4e-3）：20歳になる若者が今の教育法の変換係数を持ち込む（旧＝四半期0.02＝半減期約9年の SchoolingStockSpeed）
        p.SchoolingConvStock = stock;
        double g = c.ConsciousnessFromSchoolingStock;
        return (1.0 - g) * convertNow + g * stock;   // g=0 で convertNow（現行恒等）
    }

    // ==== 不満度（導出量）＝二量分離 段1（設計メモ §6-1・作者裁定2026-09-09）===================================
    //   ★作者の設計は**二つの量**である：
    //     不満度＝**導出量**（記憶を持たない・毎期 盤面から読む）＝生活の失望 ＋ 法の隔たり。政治運動の動力（結社・請願・議会・参加）。
    //     闘争度＝**ストック**（Pop.Militancy のまま）＝生命の危機（飢餓・厭戦・民族）＋インパルス。反乱軍の判定は全てこちらを読む。
    //   ⚠現物は11項の和を1本のストックに混ぜている＝(a)小さな不満が9個積もると生命の危機と同じ値になる
    //     (b)「盤面を戻せば戻る」量と「戻らない」量が同じ時定数で動く。段1は**導出量の側だけ**を先に定義する。
    //   ★段1は**読み手ゼロ**＝世界は1ビットも変わらない。下の3本は闘争度ループ（Step）と**同じ式の単一の定義**であり、
    //     ループはこの関数を呼ぶ（写経しない）＝二実装が生まれない（型の統合が静かに壊す・分子と分母の非対称の再発防止）。

    /// <summary>生活の失望∈[−1,1]（闘争度ループと**同じ1本**・LivingRefWeight∈[0,1] のとき）。LivingRefWeight=0 なら旧式＝1−生活水準（絶対水準）、
    /// >0 なら参照点からの【低下】＝失望（2026-08-19裁定・既定1.0で点火済み）。
    /// ⚠**成長期は負になりうる**（生活水準が参照点を上回る＝体制への支持）。呼び手はクランプの責任を持つ
    /// （闘争度は target のクランプが、不満度は <see cref="Discontent"/> 自身のクランプが吸う）。
    /// ⚠純関数＝**書き込まない**。参照点の未捕捉（LivingRef&lt;0）は現在値として読むだけで、初期化の書き込みは
    /// ループ側（力学の持ち主）に残す＝導出量の評価が盤面を動かさない。</summary>
    internal static double LivingWant(Pop p, Coefficients c)
    {
        double want = 1.0 - p.Satisfaction;
        if (c.LivingRefWeight > 0)
        {
            double refv = p.LivingRef < 0 ? p.Satisfaction : p.LivingRef;   // 未捕捉＝現在値（＝ループの初期化直後と同値・こちらは書かない）
            double drop = refv - p.Satisfaction;                            // ★正＝落ちた（不満）／負＝上がった（体制への支持）
            want = (1.0 - c.LivingRefWeight) * want + c.LivingRefWeight * drop;
        }
        return want;
    }

    /// <summary>飢餓の駆動∈[0,1]（闘争度ループと計器の**同じ1本**）＝<b>平常からの急な落ち込み</b>（2026-09-22 回B・作者承認）。
    /// <code>駆動 ＝ clamp(FamineDriveGain ×(平常 FoodRef − 今の充足 FoodMet/FoodNeed − FamineDropOnset), 0, 1)</code>
    /// ★モラル・エコノミー：食料暴動・飢饉の蜂起は慣れた水準からの急な高騰・途絶が引き金で、慢性の貧困ではない＝
    ///   旧式（絶対水準 0.73 未満で駆動）は慢性の栄養不足を飢饉と読んで飽和させていた（コンゴの人口の39.8%が充足0.51〜0.52に恒常的に居て、
    ///   同じ9カ国が両シードで1783〜84年に内戦へ落ちた）。慢性の不足は物理の経路（死亡・疫病・移民・扶養力＝絶対水準を読む）だけが運ぶ。
    /// ⚠純関数＝**書き込まない**（<see cref="LivingWant"/> と同じ作法）。平常の未捕捉（FoodRef&lt;0）は今の充足として読む＝落ち込み0。
    /// ⚠NaN は否定形で「飢えていない」側へ倒す（Math.Clamp は NaN を通す）。分母なし（FoodNeed≈0）は0。</summary>
    internal static double FamineDrive(Pop p, Coefficients c)
    {
        if (!(c.FamineDriveGain > 0) || !(p.FoodNeed > 1e-9)) return 0;
        double fill = p.FoodMet / p.FoodNeed;                    // ⚠生の比（主食の充足の1本 Pop.FoodFill は下限0で切る＝負の貯蓄の POP の需要が負になると比が負＝段4e の申し送り）
        if (fill > 1) fill = 1;                                  // 平常の側（更新）と同じ上限＝満腹を超えた分は落差に数えない
        double refv = p.FoodRef < 0 ? fill : p.FoodRef;          // 未捕捉＝今の充足（＝ループの初期化直後と同値・こちらは書かない）
        double drive = c.FamineDriveGain * (refv - fill - c.FamineDropOnset);
        return !(drive > 0) ? 0 : drive > 1 ? 1 : drive;
    }

    /// <summary>実効発言権∈[0,1]（闘争度ループと**同じ1本**）＝票の発言権 × 選挙の実在（1934拡張E5「紙の参政権」の是正）。
    /// 議会政体×秘密投票=満額／議会×公開投票=買収・脅迫で減／非議会政体=紙の参政権。</summary>
    internal static double EffectiveVoice(World w, Coefficients c, Pop p)
    {
        double effVoice = InterestGroups.FranchiseVoice(p, w.Suffrage, c);
        {
            double reality = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
                ? (w.SecretBallot == SecretBallotLaw.Secret ? 1.0 : c.OpenBallotReality)
                : c.PaperFranchiseReality;
            effVoice *= reality;
        }
        return effVoice;
    }

    /// <summary>★不満度∈[0,1]（**導出量**・記憶を持たない）＝WantWeight×生活の失望 ＋ PoliticalDistanceWeight×法の隔たり。
    ///
    /// ★法の隔たり＝ wEdu ×（1−実効発言権）× 隔たりの物差し。
    ///   ⚠前2因子は**残す**：wEdu＝min(意識,識字)（3層分割の教育層＝per-pop の解像度と、女性比が政治へ効く唯一の経路がここ）／
    ///   (1−発言権)＝平和的回路の有無（票を持つ層は不満を投票へ流す）。差し替えるのは**物差しの1因子だけ**。
    ///   ・DiscontentLawGap=0（既定・**旧経路**）＝4軸座標の距離 dist(POPの理想点, 現政体座標)/2。
    ///   ・&gt;0＝<see cref="PolicyPreferences.LawGap"/>（政策軸の選好と余地）＝「望む法がどれだけ残っているか」。
    ///
    /// ⚠クランプは**この関数が持つ**（設計 段1-2）。生活の失望は成長期に負になりうるので、
    ///   闘争度側の target クランプに寄りかかっていた分をここで吸わないと、下流のガードが無音で0を返す。
    ///   ⚠**否定形**で書く＝Math.Clamp は NaN を通すので、それでは「∈[0,1]」の名乗りが嘘になる（有限値では Clamp と厳密同値）。
    ///
    /// ⚠<paramref name="regimeCoord"/> と <paramref name="lawGap"/> は**pop ループの外で1回だけ**取る（国とブロックの量＝pop に依らない）。
    ///   ★キャッシュを作らない＝World に置くと助走中や月次の読み手に対して古い値を持つ導出量＝それは記憶である。
    ///
    /// ★読み手（2026-10-19 (431) 採用後）＝参加の述語（旗 ParticipationDiscontent＝1 が既定）・思想の更新の入力（常に）・画面の AvgMassDiscontent・Runner の計器。
    /// 闘争度の目標の先頭は法の項だけ（<see cref="PoliticalTerm"/>）＝生活の項は不満度（本体 <see cref="DiscontentRaw"/>）だけが持つ。</summary>
    /// <param name="regimeCoord">現政体の4軸座標（<see cref="PoliticalQuantities.DriftedPolityCoord"/>）。DiscontentLawGap&gt;0 では読まれない</param>
    /// <param name="lawGap">その pop のブロックの法の隔たり∈[0,1]（<see cref="PolicyPreferences.LawGap"/> の該当要素）。既定(=0)では読まれない</param>
    internal static double Discontent(World w, Coefficients c, Pop p, double[] regimeCoord, double lawGap)
    {
        double distance = c.DiscontentLawGap > 0 ? lawGap : AxisDistance(p, regimeCoord);
        double d = DiscontentRaw(w, c, p, distance);
        return !(d > 0) ? 0 : d > 1 ? 1 : d;   // 否定形＝NaN も 0 へ畳む（有限値では Math.Clamp(d,0,1) と厳密同値）
    }

    /// <summary>★(431) 不満度の2項＝（生活の項 <see cref="Coefficients.WantWeight"/>×<see cref="LivingWant"/>・法の項 <see cref="Coefficients.PoliticalDistanceWeight"/>×<see cref="LawDistance"/>）。
    /// **不満度（<see cref="Discontent"/>）と闘争度の目標（<see cref="Step"/>）が同じこの1本を通る**＝旧は同じ2項を2か所に書いていた（二量分離が途中で止まっていた型・(426)）。純関数＝書かない。
    /// <paramref name="distance"/>＝法の隔たりの物差し（<see cref="AxisDistance"/> か <see cref="PolicyPreferences.LawGap"/>）。闘争度の側は常に <see cref="AxisDistance"/>（<c>DiscontentLawGap</c> を闘争度へ広げない）。</summary>
    private static (double Living, double Voice) DiscontentTerms(World w, Coefficients c, Pop p, double distance)
        => (c.WantWeight * LivingWant(p, c), PoliticalTerm(w, c, p, distance));

    /// <summary>★(431) 法の項（<see cref="Coefficients.PoliticalDistanceWeight"/>×<see cref="LawDistance"/>）の1本。不満度の2項の片方で、**闘争度の目標の先頭もこの項だけ**（<see cref="Step"/>・生活の項は闘争度に入らない）。純関数。</summary>
    private static double PoliticalTerm(World w, Coefficients c, Pop p, double distance)
        => c.PoliticalDistanceWeight * LawDistance(w, c, p, distance);

    /// <summary>★(431) 不満度の本体（**0 で切らない**）＝生活の項＋法の項。<see cref="Discontent"/> はこれを [0,1] に切るだけ。
    /// 闘争度の目標（<see cref="Step"/> の sum）の先頭は法の項だけ＝この値から生活の項を引いた物（<c>Pop.MilVoice</c>・生活の項は不満度だけが持つ＝闘争度の内訳に生活の欄は無い）。
    /// ⚠成長期は負になりうる（生活の失望が負＝体制への支持）。切るのは呼び手の責任（不満度は [0,1]・闘争度は目標の和を切る）。純関数。</summary>
    internal static double DiscontentRaw(World w, Coefficients c, Pop p, double distance)
    {
        var (living, voice) = DiscontentTerms(w, c, p, distance);
        return living + voice;
    }

    /// <summary>★(417) 4軸の距離∈[0,1]＝dist(POPの理想点, 現政体座標)/2（/2＝4軸[0,1]の最大距離2で正規化）。**法の隔たりの物差しの旧経路の唯一の定義**＝不満度（<see cref="Discontent"/>）と
    /// 闘争度の目標（<see cref="Step"/> の政治の隔たり）が同じこの1本を読む（旧は同じ式を2か所に書いていた＝片方だけ直して画面と機構が食い違う型・二量分離 dorm-03）。純関数。</summary>
    private static double AxisDistance(Pop p, double[] regimeCoord)
    {
        double[] ideal = Ideologies.IdealPoint(p.IdeologyShares);
        double pd2 = 0; for (int i = 0; i < 4; i++) { double x = ideal[i] - regimeCoord[i]; pd2 += x * x; }
        return Math.Sqrt(pd2) * 0.5;   // /2＝4軸[0,1]の最大距離2で正規化→[0,1]
    }

    /// <summary>★(417) 法の隔たり∈[0,1]＝意識(wEdu＝min(意識,識字))×(1−実効発言権)×<paramref name="distance"/>（物差し）。不満度と闘争度の目標の**共通の2因子**＋物差し1本（<see cref="Discontent"/> の注）。
    /// 旧の闘争度の目標は ((wEdu×(1−発言権))×√pd2)×0.5・ここは (wEdu×(1−発言権))×(√pd2×0.5)＝×0.5 は2のべきなので浮動小数の値は厳密に同じ。</summary>
    private static double LawDistance(World w, Coefficients c, Pop p, double distance)
        => Math.Min(p.Consciousness, p.Literacy) * (1.0 - EffectiveVoice(w, c, p)) * distance;

    public static void Step(World w, Coefficients c)
    {
        if (!w.PoliticsEnabled) return;

        if (w.RuptureWindowQuarters > 0) w.RuptureWindowQuarters--;   // 第6段6-8b：断裂の窓の減衰（鎮圧・制圧・クーデタが再セット）
        if (w.LostRegionsCumulative > 1e-9)   // 機能7-2：失地ストックの風化＝世代の置き換わり（τ＝0・2026-09-30 段4e 後半 4e-3＝旧は固定の半減期20年 LostLandHalflifeQuarters）
            w.LostRegionsCumulative *= 1.0 - w.ReplacementRate(Calendar.MonthsPerQuarter);

        // ★F1 譲歩は法の経路（作者裁定2026-09-11）：前回の一揆への譲歩で置いた要求の法が要求の向きへ動いた（Government.Step の通常経路でのまれた）
        //   ＝記録を下ろし（AI の Apply の置き直し＝Parliament.TargetCrisisDemands が止まる）、一揆の州（PendingRevoltRegion）が今も自国の Active 州ならその大衆へ既存の緩和率を掛ける
        //   ＝譲歩が鎮める（作者裁定2026-09-09）のは**施行の時点**（F3b の「怨嗟の解消は施行の時点であって目標を置いた時点ではない」と同じ物差し＝置いただけの約束は鎮めない）。
        //   動いていなければ残る＝要求は据え置き・次の一揆の譲歩は同じ段を置き直す（新しいタイマーは無い）。⚠Step の先頭で見る＝施行の翌四半期の分離・F3b・内戦の判定より先に鎮める
        //   （CheckRegionalRevolt の中だと内戦中は読まれず、同じ四半期の分離判定が緩和前の闘争度を読む）。
        //   ⚠否定形＝位置が NaN のときは「動いた」側へ倒す（CrisisDemandsMoved と同じ書き方）。
        if (w.PendingRevoltDemand != 0)
        {
            double now = FactionSnapshot.AxisPosition(w, (PolicyAxis)(Math.Abs(w.PendingRevoltDemand) - 1)), was = w.PendingRevoltDemandPos;
            if (w.PendingRevoltDemand > 0 ? !(now <= was + 1e-9) : !(now >= was - 1e-9))
            {
                w.PendingRevoltDemand = 0;
                foreach (var r in w.Regions) if (r.Id == w.PendingRevoltRegion)
                    foreach (var p in r.Pops) if (p.IsCommoner) p.Militancy *= c.ReformMilitancyRelief;
            }
        }

        double convertNow = ConsciousnessConversion(w.EducationLaw);   // 政15：今期の教育法の即時変換係数（既定はこのまま全pop一律）
        // 便乗の勢い＝前期の運動力(改革派+急進派)÷実効保持力（第5段5-1＝PoliticalQuantities一元化・前期Coercion×現人口＝従来と同時点）。革命が近いほど大衆が雪崩を打つ（仏革命の群衆）。
        double momentum = PoliticalQuantities.Clamp01((w.ReformistPower + w.RadicalPower) / Math.Max(1, PoliticalQuantities.EffectiveCoerce(w, c)) * c.BandwagonSensitivity);

        // ★段5＝民兵の札は「戦争の終わり」で剥がれる（作者裁定3・2026-09-08）＝**タイマーを作らない**。
        //   内戦が終われば（再併合・独立の確定・白紙分断のどれで終わっても InCivilWar が偽になる）武器を置いて畑と工場へ戻る
        //   ＝労働供給がその瞬間に回復する。⚠蜂起の予約中（RevoltingFaction／PendingRebelRegions）は「まだ始まっていないだけ」なので剥がさない
        //   （TriggerFactionRevolt が札を立ててから建国＝開戦までに四半期の境界を跨ぐ＝処理順に依存させない）。
        //   ⚠民兵率0の世界（RevoltMilitiaRaise=0＝既定）ではこの条件の中で1回も書かない＝厳密恒等。
        //   ⚠「内戦中か」の物差しは World.InCivilWar **1本**（新しい述語を作らない）＝CivilWarPredicateUnified=1.0 が点火済みなので
        //     War 経路（CivilWarParent 付き＝親も反乱国も真）が見える（旧経路の内戦 CivilWarYears は 2026-09-28 段2 束3 で削除）。
        //     ⚠それを0へ戻すと反乱国の民兵は建国の翌四半期に剥がれる（＝この札は内戦の述語に依存している）。
        bool inRevolt = w.InCivilWar || w.RevoltingFaction is not null || w.PendingRebelRegions is not null;
        foreach (var p in w.AllPops())
        {
            if (p.MilitiaShare.Value > 0 && !inRevolt) p.MilitiaShare = default;   // 戦争の終わりという**状態**で剥がす
            double urban = p.IsUrban ? c.UrbanConsciousnessBoost : 1.0;
            double convert = SchoolingConversion(p, convertNow, c);   // 政15：既定は convertNow（全pop一律・現行恒等）／>0 で pop の就学履歴ストックへ
            // 便乗：勢いある運動は無学な民をも政治化する（教育に依らず＝専制の安定を崩す）。識字が低いほど便乗しやすい（群衆心理）。
            double bandwagon = c.BandwagonWeight * momentum * (1.0 - p.Literacy);
            // 報道の自由は政治意識を広める（情報の流通）。国家報道のみ＝統制で意識が育ちにくい（安定と引き換え）。
            // 検閲法→倍率の3値表を撤去し PressFreedom（Freedom式と単一ソース）から：StatePress=0.75・FreePress=1.25（厳密一致）。
            // 1934拡張E3：報道の実体＝新聞財。press の自由項を「内容の自由(PressFreedom)×紙が届いたか(NewsFill)」の積に＝
            // 買えない/刷れない社会は自由でも意識が育たない。NewsConsciousnessFloor=1.0 で旧式と厳密一致（新聞財の無い小シナリオ・較正の保護弁）。
            double newsReach = c.NewsConsciousnessFloor + (1.0 - c.NewsConsciousnessFloor) * p.NewsFill;
            double press = 0.75 + 0.5 * PressFreedom(w.Censorship) * newsReach;
            // [PORT:P2] 意識targetへの寄与＝識字×教育変換×都市化＋便乗、×報道（新聞/検閲）。時定数17年の遅い変数＝速い興奮は闘争度(P1)の仕事
            double target = PoliticalQuantities.Clamp01((p.Literacy * convert * urban + bandwagon) * press);
            p.Consciousness += c.ConsciousnessSpeed * (target - p.Consciousness);
        }

        // 倍率排除⑥b-2：軍拡負担の闘争度直接加算（治安維持力>2500 の手書き絶対閾値）を撤去。軍・治安維持力の維持コストは
        // 既存の財政連鎖（大軍→赤字→緊縮→逆進課税/福祉削減→可処分所得↓→満足↓）が WantWeight×(1−Satisfaction) で拾う＝不満の単一ソースへ集約（⑥a/失業と同じ二重計上の解消）。
        //   ★(436) 合流の注：(431) の二量分離で生活の項は**不満度だけ**が持つ（闘争度の目標には入らない）＝軍拡負担・失業・緊縮の苦しさは不満度（参加の述語・思想の入力）へ届き、
        //   闘争度の読み手（動員・罷業・州の一揆・犯罪・民兵ほか）へは届かない。罷業と動員が不満度を読むべきかは作者への問い（(431)・00一覧 §0-2）。
        double warWeariness = w.SelfPower?.WarExhaustion ?? 0;   // 厭戦（対外戦争のみ・較正は0）。闘争度入力へ（旧：Ideology の階級分岐で直接加点）。★(377) 厭戦の項は残す＝(373) の腕で、これを外すと交戦中と平時の闘争度の差が消えた（戦争→内政の経路がこの項だけ・実装ログ (377)）
        // 政治の空間モデル：現政体の4軸座標（実参政権でドリフト済＝第5段5-1でPoliticalQuantitiesへ一元化）をループ前に1回算出。各popの理想点との距離が政治的不満。
        double[] regimeCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        // 参政権の範囲2法（作者裁定2026-08-16・旧の旗 SuffrageScopeGate は 2026-10-18 に焼き込み）：移民原住民参政権の少数派緩和は「州の格」を読む
        // ＝ループを州→popの入れ子へ（AllPops と同じ走査順）。
        foreach (var milReg in w.Regions)
        {
        foreach (var p in milReg.Pops)
        {
            // ★★★生活苦＝**失望**＝【低下】から生まれる（2026-08-19裁定・2026-08-21点火＝既定1.0）。⚠0に戻すと旧式（絶対水準）＝対照用。
            // ⚠参照点は【前期の値】を読む（★更新はこの pop の処理の最後＝同一ティック内で閉じない）。
            // ⚠式は <see cref="LivingWant"/> **1本**（段1・2026-09-09）＝闘争度（ストック）と不満度（導出量）が同じ生活苦を読む＝二実装を作らない。
            //   未捕捉の初期化だけはここに残す（純関数は盤面を書かない）＝初期化直後の LivingWant は同じ現在値を読むのでビット同値。
            if (c.LivingRefWeight > 0 && p.LivingRef < 0) p.LivingRef = p.Satisfaction;   // 未捕捉＝現在値で初期化（立ち上がりの跳躍なし）
            // ★食料の平常（2026-09-22 回B）も同じ作法：未捕捉＝今の充足で初期化＝開始時の慢性の低さは「平常」として入る（1783年の塊の根）。
            //   分母なし（FoodNeed≈0）・NaN の月は捕捉も更新もしない（否定形の `>= 0` は NaN で false）。
            double foodFill = p.FoodNeed > 1e-9 ? Math.Min(1.0, p.FoodMet / p.FoodNeed) : double.NaN;   // ⚠生の比（飢餓の駆動と同じ・Pop.FoodFill の下限0の切りを通さない＝段4e の申し送り）
            if (p.FoodRef < 0 && foodFill >= 0) p.FoodRef = foodFill;
            // 政治の空間モデル（voiceless の一般化）：政治的不満＝**意識 ×（1−実効発言権）× dist(POPの理想点, 現政体座標)**。
            // 社会主義の労働者が絶対王政の下にいる＝4軸で遠い＝高い不満／保守的貴族＝近い＝満足。
            // 3つのゲートは全て実利/実状から創発：①意識（無学は政治的に受動＝徳川パス温存）②（1−発言権）＝**平和的回路の有無**（票を持つ層は不満を投票=改革へ流す）③距離（理想と体制の乖離）。
            // 1934拡張E5 秘密投票（「紙の参政権」問題の是正）：発言権の**鎮静効果**を「選挙の実在」で変調。
            // ⚠式は <see cref="EffectiveVoice"/> **1本**（段1・2026-09-09）＝不満度の側も同じ発言権を読む。
            // 3層モデル＝連続分割（重みの和が厳密に1）：文盲 1−ℓ → 天命（素朴王政観・MandateBase）／教育 min(意識,ℓ) → 綱領政治（本項）。
            // ⚠中間層（ℓ−教育）の**闘争度への直接加算は段2で削除**（設計メモ §7-3）＝「危険な中間」は原因ではなく層（誰が動くか）であり、
            //   ⑤3層の振る舞い分けが担う。層の重み自体は Politics の他の読み手（天命・不満度）と factionprobe が持っている。
            double voice = PoliticalTerm(w, c, p, AxisDistance(p, regimeCoord));   // ★(431) 不満度と同じ法の項の1本。4軸の距離は常に旧経路＝DiscontentLawGap は闘争度の目標へ広げない。生活の項は闘争度の目標に入らない（不満度だけが持つ・二量分離の完成）
            // ★段2＝民族項の**一本鎖**（設計メモ §7-3・作者裁定2026-09-09）。⚠旧は同じ民族統一度を3度読んでいた
            //   ＝①国項 UnitySecessionMilitancy×(1−EthnicUnity)×自分の中核度 ②pop項 MinorityMilitancy×(1−中核度) ③上昇鈍化 ×(1−UnityMilitancyRelief×EthnicUnity)。
            //   EthnicUnity は **CoreEthnicShare の人口加重平均そのもの**なので①③は②の粗い写しにすぎない＝pop解像度の②だけを残す（国平均は②から創発する）。
            double minorityMil = w.EthnicEnabled ? c.MinorityMilitancy * (1.0 - p.CoreEthnicShare.Value) : 0;
            // 参政権の範囲2法（2026-08-16）：移民原住民参政権「あり」＝本国州の非中核分(1−CoreEthnicShare)が政治的国民に算入される
            // ＝少数派の地位不満は ×(1−算入)＝算入1で0（新しい定数を作らない創発形）。植民地格の州は対象外（そちらは植民地参政権＝分離圧の緩和側）。
            // ★E-05段3（2026-09-03）：格を見ず**算入率**で連続に緩める＝本国州は中核度≈1でほぼ0まで消え、植民地は中核度≈0でほとんど残る
            //   （旗 SuffrageScopeUnified は 2026-09-26 に焼き込み＝格を読む旧の枝＝移民原住民参政権「あり」で本国州だけ0 は削除）。
            if (w.EthnicEnabled)
                minorityMil *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, milReg));
            // [PORT:P1] ★段2＝飢餓の駆動項（設計メモ §7-3・作者裁定2026-09-09）。⚠旧 FamineAbsoluteMilitancy=0.04 は2四半期で闘争度を 0.0024 しか動かせず
            //   （派閥形成の門 0.5 の2桁下）＝「一瞬の飢餓では起きないが半年続けば大量の反乱軍」が**係数の値では表現できない**証明だった＝利得ごと作り直す。
            //   ★2026-09-22 回B：駆動は**平常（FoodRef）からの急な落ち込み**を測る（式は <see cref="FamineDrive"/> の1本・起点と利得の導出は係数のコメント）。
            //   ⚠旧式（絶対水準＝充足が FamineDriveOnset 0.73 未満で駆動）は慢性の栄養不足を飢饉と読んで飽和させた（同じ9カ国が両シードで1783〜84年に内戦）。
            //   ★2四半期で門を越える設計は不変：σ=0.30・駆動1.0 なら 0→0.30→0.51 で RegionalRevoltThreshold=0.50 を**2四半期目に**越え、1四半期では越えない。
            //   ★階層3分割と対＝食料は満足度の台帳に載らないので、飢えが政治へ出る経路はこの項の1本だけ（思想⑨）。
            double famineMil = FamineDrive(p, c);
            // [PORT:P1] 闘争度targetへの寄与（加算項）＝法（政治）の隔たり／民族／厭戦／飢餓の**4本**（生活の失望は不満度だけが持つ＝闘争度の目標に入らない・★(431)(551)）。新規機構はこのポートに独立加算で接続する。
            // ★段2（2026-09-09）で 11項→5項（★(431) 二量分離で生活の項を外して4項）：群衆の熱・失業・格差・危険な中間・革命の波を削除し、民族2項を一本鎖へ畳んだ（設計メモ §2-1/§7-3）。
            // 内訳（診断・UI表示用。target への各寄与ぶんを記録＝計算には不使用・較正中立）。値はすべて target が既に計算済み＝代入4本だけ。
            {
                p.MilVoice = voice;
                p.MilWar = c.WarWearinessMilitancy * warWeariness;
                p.MilNation = minorityMil;
                p.MilFamine = famineMil;
            }
            // 外圧→闘争度の直加算（旧PressureWeight）はC裁定2026-07-09で撤去＝外圧はReformPressureBoost（改革派の力を押す）経路に一本化。
            // ★(431) 目標＝法の項＋民族＋厭戦＋飢餓（二量分離の完成・生活の失望は不満度だけが持つ＝闘争度は生命の危機と政治的距離の量）。
            double sum = voice + minorityMil + c.WarWearinessMilitancy * warWeariness + famineMil;
            double target = !(sum > 0) ? 0 : sum > 1 ? 1 : sum;   // 否定形＝NaN も0へ畳む（Math.Clamp は NaN を通す＝闘争度が NaN 汚染される）。有限値では Math.Clamp(sum,0,1) と厳密同値
            // ★闘争度の追従速度 σ（設計メモ §7-3）＝**上昇の側で決まる**：σ < RegionalRevoltThreshold（1四半期では越えない）かつ
            //   1−(1−σ)² > RegionalRevoltThreshold（2四半期で越える）＝門0.50 では σ∈(0.293, 0.50)。
            //   ⚠上昇鈍化（旧 UnityMilitancyRelief×EthnicUnity）は削除＝民族統一度の3度目の読みだった（一本鎖へ）。
            p.Militancy += c.MilitancySpeed * (target - p.Militancy);
            // ★参照点を今の生活水準へ寄せる（⚠want を計算した【後】＝同一ティック内で閉じない）。
            if (c.LivingRefWeight > 0 && c.LivingRefSpeed > 0)
                p.LivingRef += c.LivingRefSpeed * (p.Satisfaction - p.LivingRef);
            // ★食料の平常を今の充足へ寄せる（同じ慣れの速度・駆動を計算した【後】＝同一ティック内で閉じない）。
            //   ⚠利得に関わらず回す＝駆動が休眠の世界でも「慣れ」の分布を計器が測れる（起点と利得はその分布から導いた）。
            if (c.LivingRefSpeed > 0 && foodFill >= 0)
                p.FoodRef += c.LivingRefSpeed * (foodFill - p.FoodRef);
        }
        }   // 参政権の範囲2法：州→popの入れ子ループの閉じ（AllPops の展開形）

        Ideologies.UpdateShares(w);   // 各POPのイデオロギー分布を更新（段1＝創発のみ・遷移は未駆動＝較正不変）

        // ★動員の測定は1本（MeasureMobilization・2026-09-29）＝四半期の測定（ここ）と、内戦の開戦で両陣営の POP が入れ替わった直後の測り直し
        //   （Diplomacy.DeclareCivilWar）が同じ式を通る。H（政権への期待）は下の正統性の床が、意識と人口の和は平均意識と平均満足が読む。
        var (hope, conscSum, sizeSum) = MeasureMobilization(w, c);
        double reformist = w.ReformistPower, radical = w.RadicalPower;
        // 平均満足度＝経済危機の指標（一党支配→独裁化はこれが低い危機国だけ＝満足した安定な立憲を巻き込まない）。
        // 第5段5-1：実装をNationalScore.MeanSatisfactionへ一元化（旧＝本ループの重複集計。pop更新後の同時点で呼ぶ）。
        // ★2026-09-25：母集団は**大衆のみ**（IsMassPop＝闘争度・不満度と同じ）。ガードの sizeSum は全POP＝人口>0で大衆0の国×四半期は実測0件（seed1-3×668Q）。
        w.MeanSatisfactionPol = sizeSum > 1e-9 ? NationalScore.MeanSatisfaction(w) : 1.0;

        // POP動詞①（弾圧の遺恨蓄積ループ・ClassStance常時0で不発）は2026-07-20廃止＝第3段3-1で削除（2026-07-27・厳密恒等）。

        // P8 天命 DynasticMandate v2（第5段5-3・2026-07-27）：抽象EMA→**未教育層の支持**へ再定義（docs/202607/20260727_政治の中核ルールと危機の選択_部分完了.md【一部】.md §8）。
        // Mandate＝未教育層（1−意識加重）の満足の評価EMA＝支持層が実在し検算可能。満足度は未教育popを通じてのみ天命へ・教育popを通じてのみ闘争度へ（§8-3）。
        // 「食えている限りの忠誠」＝dmFood/dmWar が王朝循環の条件を担う（§8-6）。決定的勝敗のインパルスは読まない＝DefeatShock/VictoryLegitBoost専属・二重流入排除。
        // 効果＝正統性の**連続項のみ**非対称増幅（離散インパルス＝敗戦ショック/戦勝/破産/鎮圧/建国には掛けない）。
        double dynErodeMul = 1.0, dynRegenMul = 1.0;
        {   // 天命（P8・2026-07-18 点火＝旗 DynasticMandateEnabled は 2026-09-26 に焼き込み）
            if (w.DynasticMandate < 0) w.DynasticMandate = 0.5;   // 未捕捉のみ初期化（0.5=中立・跳躍なし）
            double dmPrev = w.DynasticMandate;
            // 5-3/5-4：文盲層（重み＝1−識字）の加重満足＝天命の母数（PoliticalQuantities.MandateBase＝UIと共有の唯一実装。母数ゼロ＝全pop平均へ退避）。
            // 3層分割（5-4）＝各popの重みが文盲(1−ℓ)/教育(min(意識,ℓ))/中間(残り)へ厳密に1へ分割：文盲→天命（素朴王政観）・
            // 教育→綱領政治(politicalGap)・中間→危険な中間（下の闘争度項）＝同じ根（満足）が式でなく人口で分割され二重計上が構造的に不可能（規約3を構造で満たす）。
            var (uneduShare, uneduSatAvg) = PoliticalQuantities.MandateBase(w);
            double dmSat = c.DynMandateSatWeight * ((uneduSatAvg >= 0 ? uneduSatAvg : w.MeanSatisfactionPol) - c.DynMandateSatPivot) / (1.0 - c.DynMandateSatPivot);   // 中立点は分位の写像で導出（Coefficients.DynMandateSatPivot）
            double dmFood = c.DynMandateFoodWeight * Math.Min(0, w.FoodFill - c.DynMandateFoodAnchor);
            double dmWar = -c.DynMandateWarWeight * (w.SelfPower?.WarExhaustion ?? 0);
            double dynPerf = dmSat + dmFood + dmWar;   // ★(383) 作者 2026-10-16「汚職→廃止」：汚職の項 dmGraft（天命への -DynMandateGraftWeight×実効汚職）は物理削除した
            double dmTarget = PoliticalQuantities.Clamp01(0.5 + dynPerf);
            w.DynasticMandate += c.DynMandateEmaRate * (dmTarget - w.DynasticMandate);
            if (w.Explain != null)   // 説明API：天命の内訳（合計＝最終値の厳密分解）
            {
                double r = c.DynMandateEmaRate;
                w.Explain.Reset("DynasticMandate");
                w.Explain.Add("DynasticMandate", "前期からの天命(履歴の累積)", dmPrev);
                w.Explain.Add("DynasticMandate", "中立への回帰", r * (0.5 - dmPrev));
                w.Explain.Add("DynasticMandate", "未教育層の満足(天命の母数)", r * dmSat);
                w.Explain.Add("DynasticMandate", "飢えの評価", r * dmFood);
                w.Explain.Add("DynasticMandate", "戦争疲弊の評価", r * dmWar);
                double dmClamp = r * ((dmTarget - 0.5) - dynPerf);
                if (dmClamp != 0) w.Explain.Add("DynasticMandate", "評価の飽和", dmClamp);
            }
            // 第5段5-3（2026-07-27）：手書きのNC減衰（ncFade=1−1.2×NC・DynMandateNcFade削除）→**文盲人口シェア**（5-4で1−識字に確定）へ。
            // 識字が広がるほど天命の母数が自動的に痩せる（改革のジレンマが導出になる＝係数1本消滅）。
            double dynE = (w.DynasticMandate - 0.5) * 2.0 * uneduShare;
            // 第5段5-5（2026-07-28）宗教＝文盲層の政治回路：天命は聖職者（教会）に媒介されて王座へ流れる（§8-10）。
            // 開度＝国教法（国教のみ1.0→内心の自由0.85→信仰の自由0.7→国家無神論0）×容量＝聖職者の実在（文盲人口あたり・飽和）。
            // 公教育の代償が導出になる：識字↑→母数痩せ（5-3/5-4）＋教会収入↓（十分の一税・教育需要）→聖職者減→媒介痩せ＝天命の供給元の破壊。
            // 聖職者が未モデルの世界（合成較正・最小テスト）＝素通し1.0（機構は制度が実在する世界でのみ差を作る）。MandateReligiousCircuit=0で恒等。
            if (c.MandateReligiousCircuit > 0)
            {
                double lawOpen = w.StateReligion switch { StateReligionLaw.InnerFreedom => 0.85, StateReligionLaw.ReligiousFreedom => 0.7, StateReligionLaw.StateAtheism => 0.0, _ => 1.0 };
                bool clergyModeled = false; double clergySize = 0;
                foreach (var p in w.AllPops()) if (p.ClassId == PopClass.Clergy) { clergyModeled = true; clergySize += p.Size; }
                double illit = uneduShare * w.TotalPopulation;
                double capacity = clergyModeled && illit > 1e-9 ? Math.Min(1.0, clergySize / (illit * c.ClergyMediationPerCapita)) : 1.0;
                dynE *= 1.0 - c.MandateReligiousCircuit * (1.0 - lawOpen * capacity);
            }
            dynErodeMul = Math.Clamp(1.0 - c.DynMandateAmplify * dynE, DynMandateMulMin, DynMandateMulMax);   // [PORT:P3] 天命→正統性の連続項レバー（侵食/回復の対称クリップ乗数）。名君＝侵食が鈍る／暗君＝加速
            dynRegenMul = Math.Clamp(1.0 + c.DynMandateAmplify * dynE, DynMandateMulMin, DynMandateMulMax);   // 対称クリップ＝安全弁（開幕即崩壊と無制限加速の両防止）
        }
        double avgConsc = sizeSum > 1e-9 ? conscSum / sizeSum : 0;
        // 倍率排除⑥：政体別 erosion(絶対/専制/幕府=0.07・軍政=0.22)を**制度ギャップ**で一本化（恒久的な罠を作らない＝やがて革命）。
        // 意識ある社会が要求する代表/自由(RepresentationThreshold)と実効(Freedom＝参政権＋報道法から創発)の差が正統性を侵食する。
        // ＝閉じた体制(低Freedom)ほど・意識が高いほど削れ、代表的な体制(Freedom≥閾値)は自動で侵食ゼロ（政体ラベルでなく制度実数から創発）。
        double institutionalGap = avgConsc * Math.Max(0, c.RepresentationThreshold - w.Freedom);
        // ★(383) 作者 2026-10-16「汚職→廃止」：腐敗→正統性の侵食 corruptionErosion（CorruptionLegitimacyErosion×意識×(1−行政効率)）は物理削除した（腐敗は財政漏れ Fiscal の額で効く＝正統性へ重ねて引くのは二重計上）。
        // 政1：全侵食項を erosion に集約（無状態版 leg=1−erosion／ストック版は前期値からこの erosion を差し引き緩慢に再生）。
        double erosion = (c.LegitimacyErosionRate * institutionalGap) * dynErodeMul + c.DefeatShockLegitWeight * w.DefeatShock;   // 敗戦ショックが正統性を一時的に大きく削る（敗戦→革命）。較正は DefeatShock=0＝中立。P8天命は連続項のみ変調（×1.0で恒等・敗戦インパルスは除外）
        // 説明API（#63）：正統性の内訳を実寄与量で記帳。既定は LegitimacyIsStock=1.0＝**ストック**（履歴が累積する経路依存量）。
        // 代数：leg = (1−s)·1 + s·P + s·regen − erosion − mandate（P=前期の正統性・侵食は s に依らず全額）。ゆえに「前期からの正統性（履歴）＋今期の回復−侵食−マンデート＋後続イベント（戦勝/デフォルト/内戦…）」に分解される。
        // null 条件のため説明モードオフでは丸ごと短絡＝本計算はビット恒等。侵食の各項はこの下で個別に負記帳する。
        if (w.Explain != null)
        {
            w.Explain.Reset("Legitimacy");
            w.Explain.Add("Legitimacy", "前期からの正統性(履歴の累積)", w.Legitimacy);
            w.Explain.Add("Legitimacy", "自然回復(安定期の信任回復)", c.LegitimacyRegenRate * Math.Max(0, 1 - w.Legitimacy) * dynRegenMul);
            w.Explain.Add("Legitimacy", "制度ギャップ侵食(意識と参政権・報道の乖離)", -c.LegitimacyErosionRate * institutionalGap * dynErodeMul);
            w.Explain.Add("Legitimacy", "敗戦ショック", -c.DefeatShockLegitWeight * w.DefeatShock);
        }
        // 議会の力学（ParliamentEnabled のみ＝合成較正は従来どおり）：既存の正統性に信号を流し、遷移を創発させる（外科的フリップなし）。
        if (w.ParliamentEnabled)
        {
            double maxSeat = Math.Max(w.PartySeats[0], Math.Max(w.PartySeats[1], w.PartySeats[2]));
            // 立憲君主制：議会の反王党派（自由+社会）の議席が王冠の正統性を削る＝議会優勢→共和（君主の形骸化）。
            if (w.Polity == Polity.ConstitutionalMonarchy)
            {
                double constErode = c.ConstMonarchyErosion * (w.PartySeats[1] + w.PartySeats[2]) * dynErodeMul;
                erosion += constErode;
                w.Explain?.Add("Legitimacy", "議会の反王党派議席(立憲君主の形骸化)", -constErode);
            }
            // 一党支配（共和制・立憲君主制）：閾値超の支配が多元的正統性を削る→権威主義へ漂流（正統性↓→保持力↓→内戦なし即時）。
            // **危機国（平均満足度＜閾値）のみ**＝経済が破綻した社会の支配的政党だけが独裁に転ぶ（ワイマール→ナチ型）。満足した安定な一党優位（英・米の自由党多数等）は無傷で侵食しない。
            if (w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && maxSeat > c.PartyDominanceThreshold.Value && w.MeanSatisfactionPol < c.DominanceInstabilitySatisfaction)
            {
                double domErode = c.DominanceLegitimacyErosion * (maxSeat - c.PartyDominanceThreshold.Value) / (1 - c.PartyDominanceThreshold.Value) * dynErodeMul;
                erosion += domErode;
                w.Explain?.Add("Legitimacy", "一党支配の私物化(多元的正統性の毀損)", -domErode);
            }
        }
        // 政1：正統性＝**ストック**（前期値から侵食を減算し、上限1へ緩慢に再生＝内戦ドレイン等が累積する経路依存量）。
        // 第3段3-4/3-5（2026-07-27）＝旧・フロー/ストック混合（LegitimacyIsStock）を1.0焼き込みで簡約：
        // legFlow（毎期計算して重み0で捨てる死荷重）と (1−s)×MandateSpent（同じく重み0の死項）を削除。s=1.0は2026-07-10点火以来の既定＝厳密恒等。
        double legStart = w.Legitimacy;   // この四半期の測定の前の正統性（上の出来事の衝撃＝矯正講和・不履行・鎮圧の後）＝下の「侵食が進行中」の比べ先（局所の値・状態ではない）
        double leg = w.Legitimacy - erosion + c.LegitimacyRegenRate * Math.Max(0, 1 - w.Legitimacy) * dynRegenMul;   // ストック（前期値から侵食・再生）。P8天命＝回復の変調（×1.0で恒等）
        // 政治資本の請求（Government.MandateGate／Move）：反対を押し切った分だけ正統性を削る。
        // ストック正統性では前期の減算が残留する→毎期ストック全額を引くと増幅率≈1/RegenRate=50倍の二重計上
        // （一歩ΔM=0.125で累積≈6.25＝即崩壊・2026-07-10再設計）→**今期増分 MandateSpentNew だけを一回減算**し、回復は再生項に一本化。
        // 実効治安維持力（下の Coercion=BaseCoercion×Legitimacy×funding）も連動して弱まる＝押し切る統治は治安維持力も燃やす（意図した結合）。
        // MandateSpent（目減り S）は門・政治資本UI表示（a＝max(0,b−S)）・CrownParliamentの紛争記憶・粛清の記憶・AIの選挙レバーが読む現役フィールド＝存続。
        double mandateClaim = w.MandateSpentNew;
        leg -= mandateClaim;
        w.Explain?.Add("Legitimacy", "マンデート行使(政治資本で反対を押し切る)", -mandateClaim);
        w.MandateSpentNew = 0;
        // ★政治資本の在庫化（作者裁定N2・2026-09-21）＝S を「押し切った傷」から「b からの目減り」へ読み替え、回復を**線形**にする。
        //   毎期 b/MandateRefillQuarters×実行度 だけ S を減らして0で止める＝空から満タンまで実行度=1 で厳密に40四半期。
        //   ⚠作者は漸近回復を却下（「b 付近で極端に低速になるのは好ましくない」「使えば使うほど事実上の法改正が進むのも違和感」）。
        //   ★2026-09-21 ㉖で旗 PoliticalCapitalStock を焼き込み＝旧の指数減衰（MandateRecoveryRate＝0.035/期）は係数ごと削除＝回復はこの線形1本。
        //   ⚠四半期の順序＝Government.Step（QuarterStart で b を導出・消費）→ ここ（QuarterEnd で回復）＝消費が先・回復が後。
        //     S を読む記憶（CrownParliament の紛争記憶・PoliticalQuantities の粛清の記憶）は回復後の S を見る＝旧の減衰と同じ位置。
        double refill = w.PoliticalCapitalMax / Math.Max(1e-9, c.MandateRefillQuarters) * PoliticalQuantities.PoliticalCapitalRate(w);
        if (!(refill > 0)) refill = 0;   // 否定形＝NaN/負は「回復しない」へ倒す（Math.Max は NaN を除去しない）
        w.MandateSpent = Math.Max(0, w.MandateSpent - refill);
        double legClamped = PoliticalQuantities.Clamp01(leg);
        if (w.Explain != null && legClamped != leg)
            w.Explain.Add("Legitimacy", legClamped > leg ? "下限クランプ(0未満を切上げ)" : "上限クランプ(1超を切下げ)", legClamped - leg);
        w.Legitimacy = legClamped;
        // ★正統性の底（下から支える受け入れ）＝2つの床の大きい方・書き手は1本（WriterRatchetTests＝心理量の書き手を増やさない）。加算でなく床＝同じ正統性に別の項を足さない（二重計上なし）。
        // ①[PORT:P3] P10①信任帝政の正統性の下限（2026-07-18 点火＝旗 RedefinedMandateEnabled は 2026-09-26 に焼き込み）：立憲君主×王冠保持×高NC×高天命の時のみ Legitimacy≥floor×Mandate の床。
        //   「民の信任を得た皇帝はパンの値段ではもう滅びない」。⚠稀であるべき＝条件を厳しく（緩いと立憲固着＋仏が共和に届かない）。天命との二重計上なし。
        double mandateFloor = w.DynasticMandate >= 0
            && w.Polity == Polity.ConstitutionalMonarchy && w.Crown != CrownForm.None
            && w.NationalConsciousness >= c.RedefinedMandateNcMin && w.DynasticMandate >= c.RedefinedMandateMandateMin
            ? c.RedefinedMandateFloor * w.DynasticMandate : 0;
        // ②政権への失望＝秩序の受け入れ（作者裁定 2026-09-29・ナポレオン3世の人民投票1851＝1848年からの政変続きの後、秩序を約束する強い指導者を92%が承認）。
        //   期待を割られた層（1−H）は今の政権を受け入れる＝正統性の底が 1−H へ上がる＝今の政権が続きやすくなる（慣性の棒 PolityInertia×正統性・保持力・上からの改革の門が同じ正統性を読む）。
        //   直近の政変が1回以下（H=1）なら床0＝何もしない。
        double orderFloor = 1.0 - hope;
        // ③★敗戦は床を通さない（2026-09-29 線2・推奨で進めた）＝床は受け入れた層が下から支える量＝敗戦はその受け入れそのものを取り消す
        //   （1870年5月の人民投票83%〔信任帝政〕→9月のセダンで第二帝政が倒れた・1918 の独墺露）＝床×(1−敗戦ショック)。敗戦の四半期は床0・ショックの減衰（DefeatShockDecay）とともに戻る。
        //   ⚠旧＝床が敗戦の崩れを支えた（半減期50年の失望で 1870 の仏の床≈0.77）。係数0（敗戦ショックは0..1＝ResolvePeace が1.0を置き四半期で減衰する既存の量）。
        double legFloor = Math.Max(mandateFloor, orderFloor) * (1.0 - PoliticalQuantities.Clamp01(w.DefeatShock));   // 旧＝①を当ててから②を当てる2行＝同じ値（max は結合的）。否定形の Clamp01＝NaN は床を残す側
        if (w.Legitimacy < legFloor)
        {
            w.Explain?.Add("Legitimacy", mandateFloor >= orderFloor ? "信任帝政の下限(民の信任を得た皇帝)" : "政変続きへの失望(秩序の受け入れ)", legFloor - w.Legitimacy);
            w.Legitimacy = legFloor;
        }
        // ★侵食が進行中＝この四半期の測定（侵食・回復・政治資本の請求・床）で正統性が下がった（2026-09-29 線2＝(102) の申し送り2 (b)・推奨で進めた）＝上からの改革の門が読む。
        bool eroding = w.Legitimacy < legStart;
        w.DefeatShock *= c.DefeatShockDecay;   // 敗戦ショックは四半期で減衰（leg が今期の全幅を浴びた後・数年で消える）。Politics.Step は四半期1回
        w.VictoryShock *= c.VictoryShockDecay;   // 機能8-②（2026-07-28）：戦勝の熱狂も同様に数年で冷める（r[4]/r[8]の持続はこの減衰だけが決める＝Ideologyは毎期クリア再構成）
        // 機能8-④（2026-07-28）：飢えの記憶＝ストック＋減衰（RegimeTrackRecordと同じ様式）。アンカーは天命と同じ飢饉基準（DynMandateFoodAnchor＝単一ソース）。
        // 「パンの値段」の題名に最も近い機構＝飢饉は過ぎ去っても急進思想（共和/社会）として残る（1789の記憶）。
        w.FamineMemory = w.FamineMemory * c.FamineMemoryDecay + Math.Max(0, c.DynMandateFoodAnchor - w.FoodFill);

        // 治安維持力と軍事のリンク(A)：軍は内外を兼ねる一つの武力＝戦時は軍が前線へ取られ国内の治安維持力が下がる（厭戦＝長期戦ほど大→本国で革命機会＝1917型）。
        // 機能6-1（第7段第2波・2026-07-28）：戦時の結束（rally around the flag）＝開戦直後は保持力が上がるが厭戦で減衰し（WarRallyDecayPoint超でゼロ）、
        // 長期戦はドレインが上回る＝1914年8月の熱狂→1917年の崩壊が単一項で出る。恒常抑制にしない（二月革命型を殺さない）。WarRallyCoercion=0で旧恒等。
        double warExh = w.SelfPower?.WarExhaustion ?? 0;
        double warDrain = 1.0
            + (Government.AtWar(w) ? c.WarRallyCoercion * Math.Max(0, 1.0 - warExh / Math.Max(1e-9, c.WarRallyDecayPoint)) : 0)
            - c.WarCoercionDrain * warExh;   // [PORT:P5] 戦時ドレイン→保持力の乗数（軍は内外を兼ねる一つの武力）
        w.Coercion = w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * warDrain;   // §6.2（絶対値＝犯罪の秩序カバレッジはこのまま人口あたりで評価）
        double coerce = PoliticalQuantities.EffectiveCoerce(w, c);   // 政体力学の実効保持力＝人口あたり強度（第5段5-1＝一元化・今期導出値＝従来と同時点）

        // 財政破綻は外科的な政体フリップでなく**経済悪化（信用収縮＝Credit）**として効かせ、政治的帰結は通常の遷移に委ねる（債務不履行→経済悪化の段階）。
        Factions.Step(w, c);   // F3a：国内勢力の永続化と変異（牙＝下の CheckFactionRevolt・FactionRevoltScale=1.0 稼働中）
        // ★旧経路の内戦（第二の戦争エンジン＝ProcessCivilWar）を削除（2026-09-28 処理の一元化 段2 束3・作者裁定 a）＝内戦は全て戦争の形（首都の蜂起も）。
        //   旧＝CivilWarYears>0 の間は通常遷移・F3b・F1 を止めて抽象的な戦勝点で数年を戦った。
        //   ★首都が反乱国の手にある間（首都の分離独立の内戦中＝World.SecedingRegion）は通常の遷移と蜂起の判定をしない＝政体の問いは戦場で決まる
        //   （旧経路の「内戦中は通常遷移せず戦乱を進める」と同じ規則＝首都の政変は旧経路の内戦だった）。⚠旧＝戦争の形の地方の蜂起・分離独立の内戦は止めていない＝そのまま。
        //   ⚠止めないと、首都を失った政府が戦争の途中で自ら反乱側の政体へ転じ（圧勝の即時の政変）、政体をめぐる内戦が要求戦に化けた（年代記 seed1 の英 1788）。
        bool capitalHeld = w.SecedingRegion is not null;
        if (!capitalHeld) Transition(w, c, reformist, radical, coerce, eroding);
        bool capitalRose = w.PendingRebelRegions is not null;   // 首都の蜂起を予約した四半期＝他の蜂起は判定しない（旧＝旧経路の内戦が始まった四半期と同じ扱い）。予約は四半期末に回収＝この四半期の Transition だけが書く
        CheckSecession(w, c, coerce);   // ★E-06：ストックは内戦中も積分する（旗 DominanceIsStock は 2026-09-26 に焼き込み）。分離独立を判定（保持力は人口項を内包・政5で coerce=軍/警察由来の保持力を渡す）
        if (!capitalRose && !capitalHeld) CheckFactionRevolt(w, c, coerce);   // F3b 組織運動の内乱（結束した国内勢力＝太平天国型・FactionRevoltScale=1.0稼働・0で観察のみ）
        if (!capitalRose && !capitalHeld) CheckRegionalRevolt(w, c, coerce);   // F1 州一揆（王権議会F1・RegionalRevoltThreshold=0で無効・★段2で現行0.50）

        w.Freedom = FreedomFromLaws(w.Suffrage, w.Censorship);   // 倍率排除②：政体ラベルでなく参政権＋報道法から創発

        // 王権議会（CrownParliament）：議会統制力・協力度3チャネル・軍帰属を毎四半期導出＝財政（Fiscal/Government）・外交・議会・政体遷移が読む。RNGなし＝決定論。
        CrownParliament.Step(w, c);
    }

    /// <summary>★動員の測定＝改革派・急進派の力（POP の和×外圧の追い風×政権への期待 H）を World.ReformistPower／RadicalPower へ書く（書き手はこの1本）。
    /// 四半期の測定（<see cref="Step"/>）と、内戦の開戦で両陣営の POP が入れ替わった直後の測り直し（Diplomacy.DeclareCivilWar＝2026-09-29）が同じ式を通る。
    /// ⚠旧＝測定は四半期末の Step だけ＝開戦の四半期は、反乱国は測定0（革命圧力0＝崩壊の線1.0）・政府は出て行った首都の住民を含む前の測定（革命圧力1.0＝線0.6）のまま戦った
    ///   （首都発の内戦の最初の四半期 194件すべて・r103）＝規則は両陣営で同じでも、読む量が自国の POP でなかった。
    /// 戻り値＝期待 H（正統性の床が読む）と意識の和・人口の和（Step の平均意識と平均満足の門が読む）。</summary>
    internal static (double Hope, double ConscSum, double SizeSum) MeasureMobilization(World w, Coefficients c)
    {
        // 労働運動の両義性（B-5）：容認/統制の組合は組織労働として改革派を動員する＝体制への改革圧力（闘争度緩和で革命radicalは下げつつ、ここで改革reformistを上げる）。非合法＝動員なし。
        double unionMob = c.UnionMobilization * (w.LaborUnion switch { LaborUnionLaw.Tolerated => 1.0, LaborUnionLaw.Controlled => 0.5, _ => 0.0 });
        double reformist = 0, radical = 0, conscSum = 0, sizeSum = 0;
        foreach (var p in w.AllPops())
        {
            if (p.IsSoldier) { conscSum += p.Consciousness * p.Size; sizeSum += p.Size; continue; }   // ★(393) 兵は保持力 κ の中にいる＝動員に足すと二重に数える（平均意識・平均満足の門の母数には入れる）
            // POP動詞①（階級弾圧の表出抑制 express・常時1.0）は2026-07-20廃止＝第3段3-1で削除（2026-07-27・厳密恒等）。
            reformist += p.Size * p.Consciousness * (1 - p.Militancy);
            radical += p.Size * p.Consciousness * p.Militancy;
            if (unionMob > 0 && IsMassPop(p))
                reformist += p.Size * p.Consciousness * unionMob;   // 組織労働の政治動員＝改革派を押し上げ
            // 窮乏チャネル（パンの蜂起＝1789）：意識が低くても、窮乏で闘争度が暴動水準を超えた群衆は革命的脅威になる
            // （飢えた群衆そのもの＝バスティーユ）。意識ゲートとは別経路で radical 挑戦力に上乗せ。平穏な大衆には発火しない＝徳川パス温存。
            // 二重計上監査C1（2026-07-11）：高意識のmilitantは上の意識経路（politicalGap）と本経路の両方に乗る二重だった→
            // ×(1−gate×Consciousness)＝「無学の飢えた群衆」の設計意図どおりのゲート（gate=1で完全直交・0で旧＝休眠可）。
            // 失われるradicalはDesperationChallengeWeightの補償再調整とセット（較正のAbsolutist梯子帯が番人）。
            // 〔削除 2026-10-11 (271)：闘争度による手段の上書き DesperationMilitancyOverride と 3層の振る舞い分け DesperationLayerBlend〕
            //   既定の世界で通らない枝（どちらも既定 0）だった。作者 2026-10-11「全て推奨で」（00-A）で採った推奨＝意識を識字で頭打ちにしない（設計思想⑧ 天井の廃止）。
            double whoActs = 1.0 - c.DesperationConsciousnessGate * p.Consciousness;   // 意識ゲート（誰が自ら動くか）
            radical += p.Size * Math.Max(0, p.Militancy - c.RiotMilitancyThreshold) * c.DesperationChallengeWeight * whoActs;
            conscSum += p.Consciousness * p.Size;
            sizeSum += p.Size;
        }
        reformist *= 1 + c.ReformPressureBoost * w.ExternalPressure;   // 明治の追い風（§A-11）
        // ★政権への失望（作者裁定 2026-09-29「連続した革命は許容するが6往復はやりすぎ＝政権への失望のような量が積もって止まる仕掛け」）。
        //   期待 H＝1/max(1, 直近の政変の数)＝政変のたびに「次の政権こそ」が割られる（最初の政変は期待で起きる＝H=1・2回目の後は半分・3回目の後は3分の1）。
        //   ①動員：失望した層は動かない＝改革派・急進派の**両方**に同じ H（分子と分母を同じ規則で＝急進主導か改革主導かの判定は変わらない）。
        //     蓄えるのは H を掛けた後の動員＝読み手（Transition の挑戦・床交差の議会圧力の門・便乗の勢い・革命圧力）が同じ1量を読む。
        //   ②正統性の床 1−H は下（信任帝政の下限の後）。直近の政変の数の書き手は CrownParliament.ApplyRegime（数える）と CrownParliament.Step（忘れる）。
        double hope = 1.0 / Math.Max(1.0, w.RecentRegimeChanges);
        reformist *= hope;
        radical *= hope;
        w.ReformistPower = reformist;
        w.RadicalPower = radical;
        return (hope, conscSum, sizeSum);
    }

    /// <summary>
    /// 政体転換（§9-E・利益集団駆動）。動員（改革派＋急進派）が現体制の保持力（治安維持力×正統性）を超えると倒れ、
    /// **支配的になる勢力と移行の仕方が目標政体を決める**：漸進改革→立憲君主制（明治型）／革命は主力が大衆ならプロレタリア独裁・
    /// ブルジョワなら共和制。権威主義（絶対/専制/幕府）からの転換が主。自由な政体ではブルジョワが大衆の脅威に独裁化、
    /// 貴族が極端なら反動・復古。絶対君主制は貴族が衰え官僚軍が空白を埋めると専制君主制（上からの近代化）へ。
    /// </summary>
    internal static void Transition(World w, Coefficients c, double reformist, double radical, double coerce, bool eroding)   // ★(393) private→internal（アクセス修飾子のみ＝番人が門2 を直接叩く）
    {
        double challenge = reformist + radical;
        // 画一的な政体遷移（イデオロギー駆動）：国民イデオロギー集計→各政体の支持→引力点。
        // 引力点が現政体と異なり、支持差が保持力（PolityInertia×正統性）を超えれば遷移。任意→任意が単一式で出る。
        // 旧来の手書き分岐（軍クーデタ・1917・ブルジョワ反動・独裁の出口…）はこの一式に吸収（軍の力は★(393) から支持への上乗せでなく、下の第 1 の門の後で保持力 κ を兵士の離反の割合で守る側と挑戦の側へ分ける形・厭戦は闘争度とイデオロギー経由）。
        var agg = Ideologies.National(w);
        // 倍率排除⑧段階2：現政体の参加軸を**実際の参政権**へドリフト（控えめ）。広い参政権の立憲＝高参加で安定／狭い立憲＝低参加で共和へ押される
        // ＝政体ラベルでなく実法律が政体の引力を動かす（①②③の総仕上げ）。較正中立：sweep は実参政権＝政体の建国参政権でドリフト≒0。
        double[] curCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        // 法多様性（政体・2026-07-07）：大衆闘争×ブルジョワ勢力＝ブルジョア独裁の引き金（下記 AvgMassMilitancy の設計コメントの配線＝
        // 高闘争の議会制国家でエリートが秩序回復の独裁を敷く）。議会制の立憲/共和のみ＝較正（ParliamentEnabled=false）遮蔽。
        // ★2026-08-12：**危機ゲートの非対称を解消**（旗 BourgeoisCoupNeedsCrisis は 2026-09-26 に焼き込み）。
        //   旧の一党支配→独裁化（domDictator・(384) で物理削除）は「満足した安定な立憲/共和は独裁の引力を受けない」という危機ゲート
        //   （MeanSatisfactionPol < DominanceInstabilitySatisfaction）を持っていたのに、**同じ「議会制国家がエリート独裁に転ぶ」
        //   現象を測るこの項だけが素通り**していた。近代ブロックが厚い国ほど値が大きくなるので、最も厚いアメリカ
        //   （貴族18 に対し 資本家429・専門職143）が最も転びやすい＝1923年に BourgeoisDictatorship で反動剥奪を受け、
        //   参政権と報道を1段ずつ失って識字26%・技術2.64まで落ちる（実測）。秩序回復の独裁は**回復すべき無秩序**を要る。
        double bourgeoisCoup = 0;
        if (c.BourgeoisCoupWeight > 0 && w.ParliamentEnabled && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
            && w.MeanSatisfactionPol < c.DominanceInstabilitySatisfaction)
        {
            var (_, modernPower, _) = InterestGroups.Blocs(w);
            bourgeoisCoup = c.BourgeoisCoupWeight * modernPower * AvgMassMilitancy(w);
        }
        Polity attractor = w.Polity; double best = double.NegativeInfinity, cur = 0;
        foreach (var pol in Ideologies.Polities)
        {
            double s = Ideologies.SupportAt(agg, pol == w.Polity ? curCoord : Ideologies.BasePolityCoord(pol), c.IdeologyAffinitySlope);
            if (pol == Polity.BourgeoisDictatorship) s += bourgeoisCoup;   // 法多様性：大衆闘争が高いほどブルジョワが独裁へ傾く
            // 政体形態の実績ストック（信認⇔失望・docs/2026-07-13_形態別失望ストック.md）：その形態系統の実績で支持を符号付きに増減
            // ＝信認された形態（正）は選ばれ/現体制なら粘り（entrench）、失望された形態（負）は避けられる→両穏健形が負なら極端が浮上（ワイマール/1917）。
            // 現政体 cur にも掛かる（成功体制は保持力↑=固着方向・失敗体制は保持力↓=変化方向）。乗数は Max(0.05,..) で符号保存。0で恒等（現行0.3）。
            if (c.RegimeTrackRecordWeight > 0)   // [PORT:P4] 実績ストック→遷移支持の乗数（Max(0.05,·)＝符号保存）
                s *= Math.Max(0.05, 1.0 + c.RegimeTrackRecordWeight * w.RegimeTrackRecord[CrownParliament.RegimeFamily(pol)]);
            if (pol == w.Polity) cur = s;
            if (s > best) { best = s; attractor = pol; }
        }
        if (attractor == w.Polity || best - cur <= c.PolityInertia * w.Legitimacy) return;   // 現状が引力点 or イデオロギー差が保持力に届かない＝変化なし
        // ★(393) 軍の力の入口：保持力 κ は兵から作った量＝兵が今の政体を支えるか引力点へ回るかで、守る側 κ×a と挑戦の側 κ×(1−a) に分ける
        //   （a＝1−d・d＝兵士 POP だけの思想の集計で思想ごとに〈引力点を現政体より好む分〉の正の部を質量で足し親和の範囲で割った離反の割合）。以後の読み手（予感・門2・拮抗の判定・下賜の幅）は分けた値を読む＝分子と分母を同じ規則で。
        //   事前登録 measurements/mc_20261018_prereg.txt。軍政の支持への足し算（旧 CoupSupport）は消した＝引力点は国民の集計だけで決まる（作者 2026-10-16「全てのバフ・デバフ(特に倍率を変更するもの）は全て削除したいです。」）。
        {
            double holdA = SoldierHoldShare(w, c, curCoord, attractor, out double sCurS, out double sAttS, out double soldierW);
            if (Instruments.On) Instruments.CoupGate(w, attractor, sCurS, sAttS, holdA, soldierW, coerce, challenge);
            challenge += coerce * (1.0 - holdA); coerce *= holdA;
        }
        // 政2：上からの改革（明治型・欽定憲法・露大改革）＝支配ブロック自身が主導する代議制/近代化への移行。大衆動員(challenge)が治安維持力を超えずとも遷移できる例外チャネル。
        // （旗 ReformFromAboveEnabled は 2026-09-26 に焼き込み＝旧挙動＝challenge<=coerce ゲートが無条件で明治1889/プロイセン欽定/露大改革が不能）。
        // 「①引力点が別政体（上の第1ゲートを通過済＝支配層含む国民集計で現政体より別政体の支持が高い）②正統性侵食が進行中(Legitimacy<閾値)③遷移先が反動的奪取でない代議制方向」の三条件が揃えば coerce ゲートを免除。
        // ＝支配層が自ら立憲化/近代化するので暴動を要しない。奪取(独裁/無政府)や急進暴力革命は対象外＝あくまで漸進の上からの改革のみ。
        // C-高1(1)（史実検証2026-07-22）：anticipation＝大衆動員圧(challenge/coerce)＋外圧が高いほど発火閾値が上がる（革命の予感が王を動かす＝
        // 自由民権運動1874-84・ハイドパーク暴動1866→第二次選挙法）。旧＝正統性ストック平衡が完全閉鎖でも0.895・恒常内戦でも0.445で閾値0.4を割らず
        // 窓がデフォルト/敗戦/失脚の直後2-3年のみ＝事実上のno-op だった。Weight=0 で加算0＝厳密恒等。
        // ⚠初版は外圧 w.ExternalPressure（死んだ定数0.3）を常時加算していたため閾値が0.72-0.92へ張り付き「上からの改革」が乱発＝
        // 日本/プロイセン/蘭/西が逸脱過多へ崩れた（batch2026-07-23実測）。動員圧のみへ絞る＝正統性が侵食され**かつ**動員が高まった時だけ窓が開く。
        double anticipation = c.ReformAnticipationWeight > 0
            ? c.ReformAnticipationWeight * PoliticalQuantities.ChallengeRatio(challenge, coerce)
            : 0;
        // ★侵食が「進行中」＝正統性が低い**かつ**この四半期に下がった（2026-09-29 線2＝(102) の申し送り2 (b)・推奨で進めた）。
        //   ⚠旧＝水準だけ（Legitimacy<閾値）＝矯正講和で上限 ImposedRegimeLegitimacy 0.3（門 0.4 の下）に置かれた押し付けの政体は、侵食されていないのに
        //   翌四半期に下賜で戻った（全長で押し付け↔上からの改革の往復 132・米が君主政へ移る主な経路）＝銃剣の上の玉座の低さは侵食ではなく生まれ。
        //   史実の上からの改革は侵食の最中（プロイセン1807〜〔イエナ・ティルジットの後〕・露1861〔クリミアの敗戦と農民騒擾〕・墺1860〔ソルフェリーノと財政危機〕・
        //   墺1867〔ケーニヒグレーツ〕・オスマン1876〔不履行とバルカン危機〕・日本1889〔自由民権運動〕）／押し付けの復古（西1823〜33・ナポリ1821〜）は改革せず約10年続いた。
        //   診断（main af5cc7b・4種×300四半期）＝絶対・専制の下賜 222件のうち正統性が下がった四半期は 64件（残りは衝撃の後の回復の途中＝露の1816年ほか）。
        bool reformFromAbove = !IsSeizure(attractor)
                             && w.Legitimacy < c.ReformFromAboveLegitThreshold + anticipation && eroding;
        if (challenge <= coerce && !reformFromAbove) { if (Instruments.On) Instruments.CoupRoute(w, 1); return; }   // 動員（改革派＋急進派）が実効治安維持力を超えない＝国家が鎮圧＝高治安維持力の権威主義は工業化しても徳川に留まる（上からの改革チャネルのみ免除）
        // P3c 遷移グラフの廃止（旗 PolityGraphRetired は 2026-09-26 に焼き込み）：**平和的な漸進遷移**（非奪取×改革派主導）はグラフのジャンプから外す＝
        // 連続動学（P3b＝議会/王権圧力→統制力→床交差の再ラベル・廃位・王権の形の変異）が同じ帰結を漸進で出す。
        // 暴力革命（急進主導）と奪取（独裁/無政府）は史実どおり不連続イベント＝ExecuteRegimeChange のまま。
        // C-高1(2)：改革派主導の平和的遷移でも「上からの改革」が発火しているなら下賜（ExecuteRegimeChange の GrantedControlOnReform）に到達させる
        //（旧＝この早期returnが下賜の点火口を殺し、欽定憲法が原理的に不能だった）。ReformGrantPcWidth=0 では旧条件のまま＝厳密恒等。
        if (!IsSeizure(attractor) && radical <= reformist && !(reformFromAbove && c.ReformGrantPcWidth > 0)) { if (Instruments.On) Instruments.CoupRoute(w, 2); return; }
        // ★様態の判定はここ（政体変更の唯一の呼び手＝引力点の遷移）だけ（2026-09-28 処理の一元化 段2 束2・A）。
        //   旧＝ExecuteRegimeChange の先頭にあった＝内戦の決着（旧経路の内戦・戦争の形の MultiNationSim.ReabsorbNations）が
        //   実行部だけを呼べず、実行部を丸写しするか（旧経路）登録簿と建国制度の2行だけを書く（戦争の形）しかなかった。
        // 様態：独裁・無政府への奪取 or 急進主導＝暴力（拮抗なら首都の蜂起・圧勝なら即時）／代議制への漸進＝平和即時。
        // 政2：上からの改革は支配層主導の平穏な漸進＝暴力(内戦)経路を通さず平和即時遷移（既定 reformFromAbove=false で従来どおり）
        bool violent = (IsSeizure(attractor) || radical > reformist) && !reformFromAbove;
        // ★首都の分離独立（2026-09-28 処理の一元化 段2 束3・作者裁定 a）：拮抗した暴力の転換＝首都の蜂起は、蜂起側が首都州を握る反乱国を建て、
        //   政府は残りの州へ遷都して戦争の形の内戦を戦う（ウィーン1848・ローマ1849・パリ・コミューン1871・リスボン1910・ペトログラード1917
        //   ＝数日の革命も、軍が一方に付いて最初の月で決着する戦争）。決着は戦後処理の1本（Diplomacy.ResolvePeace→MultiNationSim.ReabsorbNations
        //   ＝反乱の勝ち＝親の政体変更＋首都を返す／政府の勝ち＝再併合＋首都州に鎮圧の後始末）。
        //   ⚠旧＝旧経路の内戦（第二の戦争エンジン ProcessCivilWar＝抽象的な戦勝点・独自の戦死と徴集と正統性の減算・国家分裂）で数年を戦った＝削除した。
        //   残すのは拮抗の判定（CivilWarLandslideRatio）だけ＝圧勝は下の即時の政変。
        //   ・平和的な転換（上からの改革・漸進）はここへ来ない＝violent が偽（2026-08-30 の幕府の教訓・条件①）。
        //   ・首都の外に政府が移れる州が無い国（州が1つ）と単国の世界（建国の回収者がいない）は即時の政変（条件②）。
        //   ・内乱が既に地図の上で進んでいる間（InCivilWar）と蜂起の予約中は新しい蜂起を立てない＝状態で排他（タイマーではない・F3b の状態の門と同じ述語）。
        //     旧経路の「内戦中は通常遷移しない」の置き換え＝戦争の形の内戦の親も反乱国も、この間は拮抗の転換を起こさない。
        if (violent && challenge < coerce * c.CivilWarLandslideRatio)
        {
            if (w.InCivilWar || w.PendingRebelRegions is not null) { if (Instruments.On) Instruments.CoupRoute(w, 7); return; }
            if (RaiseRevolt(w, w.Regions[0], attractor))
            {
                if (Instruments.On) Instruments.CoupRoute(w, 3);
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}の首都{w.Regions[0].Name}で蜂起＝政府は首都を出て戦う（内戦）");
                return;
            }
        }
        // ★親軍法の撤去（2026-08-18）：軍事独裁への遷移で法を立てる行を削除。政体由来の軍事費の床も撤去済み（Government・2026-08-19）
        //   ＝軍政の軍国性は PoliticalQuantities.MilitaryPoliticalRatio 等の導出量が担う。
        // P1-A 下賜（明治1889・欽定憲法型）：上からの改革での遷移は「弱い議会を王権が与える」＝統制力の床を下賜値へ（GrantedControlOnReform=0で休眠・現行0.3）。
        // C-高1(2)（史実検証2026-07-22）：下賜PCの連続化＝床0.3に動員圧比例の幅を加算（強い議会圧力ほど厚い譲歩＝予算協賛権つきの欽定憲法。
        // 幅0.15で最大0.45＝立憲床に到達＝日本/プロイセン/オーストリアの exact=CM 要求と設計意図の矛盾を解く一般則）。Width=0 で加算0＝厳密恒等。
        if (reformFromAbove && c.GrantedControlOnReform > 0)
        {
            double grant = c.GrantedControlOnReform
                + c.ReformGrantPcWidth * PoliticalQuantities.ChallengeRatio(challenge, coerce);
            w.ParliamentControl = PoliticalQuantities.Clamp01(Math.Max(w.ParliamentControl, grant));
            w.ReformGrantCount++;   // C-高1完成検証（2026-07-25）：下賜の発生数（診断＝chronicleが集計。挙動非依存）
        }
        // P2：上からの改革は**PC床（上の下賜）だけ**を書きラベルは導出に委ねる＝下賜0.3<立憲床0.45なら幕府/専制のまま
        // 「近代法を持つが議会に権力を渡さない」プロイセン/明治型が自然点になる（ラベルが後日 床交差で変わった時だけ建国制度が走る）。
        if (reformFromAbove) { if (Instruments.On) Instruments.CoupRoute(w, 4); return; }
        if (Instruments.On) Instruments.CoupRoute(w, violent ? 5 : 6);
        ExecuteRegimeChange(w, c, attractor, violent);
    }

    /// <summary>奪取＝討議でなく実力で押し込む政体（独裁・無政府）。様態判定と政2の適用除外で共有。</summary>
    private static bool IsSeizure(Polity p) => p is Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship
                                                 or Polity.MilitaryDictatorship or Polity.Anarchy;

    // P3a 奪取イベント化（2026-07-12・ビット同一抽出）：政体変更の**実行部**＝覚醒と断裂の窓（暴力の転換）→登録簿（ApplyRegime）→
    // 建国制度→債務→失脚→土地改革。★2026-09-28（処理の一元化 段2 束2・A）：様態の判定（拮抗の暴力転換→内戦・上からの改革の下賜）は
    // 唯一の呼び手 Transition へ移し、ここは**実行部だけ**＝即時の政変（Transition）・内戦の決着（Politics.SettleRevolt＝戦争の形の
    // MultiNationSim.ReabsorbNations と、州が1つの国・単国の世界の蜂起の即時の決着）が同じ1本を通る。
    // （旧経路の内戦の決着 ProcessCivilWar は 2026-09-28 段2 束3 で削除＝首都の蜂起も戦争の形）
    // ⚠旧＝戦争の形の内戦で革命が勝つと登録簿と建国制度の2行だけ＝急進革命の国債の否認・失脚・土地改革・覚醒・断裂の窓が走らなかった
    //   （戦争で勝ったプロレタリア革命が国債を引き継ぎ地代も残る）。規則そのもの（既定は継承・急進革命だけ否認）は変えていない。
    // ⚠矯正講和（Diplomacy.ResolvePeace）と床交差の再ラベル（CrownParliament）は意図どおりこの実行部を通らない（押し付けられた政体・ラベルの付け替え）。
    internal static void ExecuteRegimeChange(World w, Coefficients c, Polity target, bool violent)
    {
        // ★段2（作者裁定2026-09-09 part3）＝**革命の波を削除**した。1848年の同時多発は伝染ではなく
        // 1845-47年の胴枯病と穀物の不作が欧州を**同時に**襲った共通ショックであり、それは盤面（飢餓の駆動項）に既にある。
        // 台本がある限り創発を検証すらできないので、放送も受信も消す。⚠国民の覚醒（RevolutionAwakensNation）は別機構＝残す。
        if (violent)
        {
            RevolutionAwakensNation(w, c);   // 第5段5-6：暴力の転換（即時・内戦を経た勝利とも）＝国民意識の直接回路
            // 第6段6-8b：奪取型の暴力転換（軍政・独裁＝クーデタ）＝断裂の窓（ナポレオン3世1851・ストルイピン1907型の書き換え解禁）。
            if (IsSeizure(target)) w.RuptureWindowQuarters = Math.Max(w.RuptureWindowQuarters, (int)c.RuptureWindowQuartersOnCrush);
        }
        CrownParliament.ApplyRegime(w, c, target);   // 登録簿＋政体キャッシュ（flag=1はPCジャンプも）＝単一の書き込み口（P2）
        RaiseFoundingInstitutions(w, allowRevoke: true, c);   // 新政体の含意参政権・報道へ（自由化＝拡張／反動＝剥奪＝ラチェット解除・反革命の波。政3：c 渡しで自由化はラチェット可）
        if (target is Polity.ProletarianDictatorship or Polity.Anarchy) Fiscal.RepudiateDebt(w, c);   // 既定は債務継承（史実）。急進革命のみ否認（継承＝リセットしない＝攻略封じ＋史実）
        var (dt, dm, dmass) = Dispossessed(target);
        ResolveRevolt(w, c, dt, dm, dmass);   // 敗れたブロックが失脚（既存の失脚スパイクを流用＝反革命の芽）
        // ★革命は土地権力を壊す（2026-08-04・列強再現率の診断）。史実のフランス革命の核心＝**封建地代の廃止**
        //   （1789年8月4日・biens nationaux）。旧＝政変後も貴族の地代＝政治力（LandedPowerCapitalization）が無傷で、
        //   新体制→旧支配層の復権→反動→再革命のピンポンで130年が浪費され、仏の技術が0.28に沈んでいた
        //   （scorebreak実測＝上位がポーランド/ラオス等の平和な小国・仏47位）。
        //   共和革命＝土地改革（限定的）・プロレタリア独裁＝全面（集産化）。**書くのは既存の法**＝FarmRentCap が
        //   地代を削り、地代の資本化＝旧支配層の政治力が一緒に痩せる（機構は全て既存・この1本だけが欠けていた）。
        //   ⚠立憲君主制には適用しない＝地主との妥協の産物（英・七月王政）。（旗 RevolutionLandReform は 2026-09-26 に焼き込み）
        if (dt)
        {
            var floor = target == Polity.ProletarianDictatorship ? ReformLevel.Full
                      : target == Polity.Republic ? ReformLevel.Limited
                      : (ReformLevel?)null;
            if (floor is { } fl && (int)w.LandReform < (int)fl)
            {
                w.LandReform = fl;
                w.Policy.LandReform = (ReformLevel)Math.Max((int)w.Policy.LandReform, (int)fl);   // 新体制の目標にも＝Government が巻き戻さない
            }
        }
    }

    /// <summary>政体が含意する参政権・報道の自由を法へ一回 floor-raise（倍率排除②）。法と標準政策の両方を下限へ上げ、以後 Freedom/eff＝法律値で動く
    /// （毎期の政体→スカラー写像を廃止）。下限は上げるのみ＝反動政体への転落で自動的に剥奪はしない（参政権・報道はラチェット＝制限は別途立法を要する）。
    /// 代議制（立憲/共和）は報道も解放＝革新世界の技術水準が立つ。独裁は検閲を保つ＝形式的参政権があっても閉じた社会で停滞。</summary>
    public static void RaiseFoundingInstitutions(World w, bool allowRevoke = false, Coefficients? c = null)
    {
        var sufTarget = PolitySuffrage(w.Polity);
        var pressTarget = FoundingPress(w.Polity);
        // ★政変は梯子を一括で配らない（2026-08-04・作者指摘「あまりに早期に近代的な法律を取り入れすぎる」）。
        //   ── 実測で判ったこと ────────────────────────────────────────────────
        //   `lawtiming` で見ると 日本1797・オーストリア1813・清1819 で「代議制へ」「制限選挙」「男性普通選挙」
        //   「秘密投票」が**全て同年**だった。政変1回で近代法制が丸ごと手に入っていた＝政治資本のたまりの問題ではなく、
        //   **この関数が政体の含意水準へ一足飛びに書いていた**のが早期近代化の実体だった（門を通らない直接代入）。
        //   ── 直し方 ──────────────────────────────────────────────────────────
        //   政変が動かすのは**1段だけ**。その先は立法（Government の門＝支持と議会）を通して一段ずつ登る。
        //   史実の梯子＝英1832→1867→1884→1918、仏1791制限→1792男子普選（革命は連続した政変として表現される）。
        //   ⚠上げも下げも1段＝反動の剥奪も一括では起きない（対称）。
        //   ⚠初期化（allowRevoke=false）は対象外＝シナリオの開始値はそのまま尊重する。
        //   （旗 StagePrereq は 2026-09-26 に焼き込み）
        if (allowRevoke && c is not null)
        {
            int su = (int)w.Suffrage, pr = (int)w.Censorship;
            sufTarget = (Suffrage)Math.Clamp((int)sufTarget, su - 1, su + 1);
            pressTarget = (CensorshipLaw)Math.Clamp((int)pressTarget, pr - 1, pr + 1);
        }
        if (allowRevoke)
        {
            // 反革命の波（復古王政・ビスマルク反社会主義法・ナポレオン3世のクーデタ）：政体転換は参政権・報道を**政体の含意水準へ上げも下げも**＝ラチェット解除。
            // 下げても大衆の支持が強ければ AI/議会が翌期に自らの選好で再拡大しうる＝19世紀の振り子（剥奪は反動ブロックの支配が続く間だけ保つ）。
            // 政3：縮小方向（剥奪）を反動系政体への遷移時のみに限定するラチェット（旗 SuffrageRatchetOnLiberalize は 2026-09-26 に焼き込み）。c=null の初期化パスでは従来どおり上げも下げも
            // （立憲化で参政権が Census へ縮む旧バグ＝現行1.0点火で解消済み）。>0 かつ遷移先が代議制（反動系でない）なら floor-raise のみ＝自由化はラチェット（19世紀に立憲化が選挙権を狭めた例はない）。
            bool ratchet = c is not null && !IsReactionaryPolity(w.Polity);
            if (ratchet)
            {
                // 自由化遷移＝下限を上げるのみ（剥奪しない）。反動政体への遷移(下の else 相当の剥奪)だけがラチェットを解除する。
                var sufFloorL = (Suffrage)Math.Max((int)w.Suffrage, (int)sufTarget);
                w.Suffrage = sufFloorL;
                w.Policy.Suffrage = (Suffrage)Math.Max((int)w.Policy.Suffrage, (int)sufFloorL);
                var pressFloorL = (CensorshipLaw)Math.Max((int)w.Censorship, (int)pressTarget);
                w.Censorship = pressFloorL;
                w.Policy.Censorship = (CensorshipLaw)Math.Max((int)w.Policy.Censorship, (int)pressFloorL);
            }
            else
            {
                w.Suffrage = sufTarget;
                w.Policy.Suffrage = sufTarget;
                w.Censorship = pressTarget;
                w.Policy.Censorship = pressTarget;
            }
        }
        else
        {
            // 初期化：与えられた初期状態を尊重＝floor-raise のみ（剥奪しない）。
            var sufFloor = (Suffrage)Math.Max((int)w.Suffrage, (int)sufTarget);
            w.Suffrage = sufFloor;
            w.Policy.Suffrage = (Suffrage)Math.Max((int)w.Policy.Suffrage, (int)sufFloor);   // 建国の参政権を標準政策へ＝Government が漸進で巻き戻さない（AIは翌期に自らの選好で上書きしうる）
            var pressFloor = (CensorshipLaw)Math.Max((int)w.Censorship, (int)pressTarget);
            w.Censorship = pressFloor;
            w.Policy.Censorship = (CensorshipLaw)Math.Max((int)w.Policy.Censorship, (int)pressFloor);   // 建国の報道自由も標準政策へ（同上）
        }
    }

    /// <summary>政3：参政権・報道の剥奪（縮小方向）を許す「反動系」政体か。代議制（立憲君主制・共和）＝自由化方向のみ false（自由化はラチェット）。
    /// 絶対君主・専制・幕府・各独裁・無政府＝反動/権威主義への遷移＝剥奪を許す（復古王政・ビスマルク反社会主義法・ナポレオン3世のクーデタ・プロ独裁の集権）。</summary>
    private static bool IsReactionaryPolity(Polity p) => p is not (Polity.ConstitutionalMonarchy or Polity.Republic);

    // 各政体が成立時に失脚させる（特権を奪う）ブロック：旧→新で誰が権力を失うか。
    private static (bool trad, bool modern, bool mass) Dispossessed(Polity target) => target switch
    {
        Polity.Absolutist or Polity.Despotic or Polity.Shogunate => (false, true, true),    // 復古＝近代エリート・大衆を抑える
        Polity.ConstitutionalMonarchy or Polity.Republic         => (true, false, false),   // 自由化＝旧支配層が特権を失う
        Polity.BourgeoisDictatorship                             => (false, false, true),   // 大衆を抑圧
        Polity.ProletarianDictatorship                           => (true, true, false),    // 旧支配層・ブルジョワが失脚
        Polity.MilitaryDictatorship                              => (true, false, true),     // 軍政＝旧支配層も民衆も抑える
        Polity.Anarchy                                           => (true, true, true),      // 秩序崩壊＝全員
        _                                                        => (false, false, false),
    };

    /// <summary>★蜂起を地図の上に立てる1本（2026-09-28 処理の一元化 段2 束3）＝本拠州（首都も）に拠る反乱国の建国の予約。
    /// 四半期末に MultiNationSim が回収して建国（SpawnNation＝守備兵の寝返りも同じ建国路）＋内戦タグの開戦（Diplomacy.DeclareCivilWar）。
    /// 首都が出るなら政府は残る州へ遷都して戦う（Territory.TransferRegion→World.SetCapital）＝首都の分離独立（作者裁定 a）。
    /// 呼び手＝首都の蜂起（<see cref="Transition"/> の拮抗した暴力の転換）と組織運動の蜂起（<see cref="TriggerFactionRevolt"/>）。
    /// 立てられない＝単国の世界（建国の回収者がいない）・その州が出ると政府に移れる州が残らない（州が1つの国）＝false（呼び手が即時の決着へ）。
    /// ⚠旧＝組織運動は「首都発は旧経路・地方発だけ戦争」の2本、首都の政変は旧経路（第二の戦争エンジン ProcessCivilWar）だけだった。</summary>
    private static bool RaiseRevolt(World w, Region seat, Polity polity)
    {
        if (w.StepsDiplomacy || w.SelfPower is null || w.LeavableRegions([seat], capital: true).Count == 0) return false;
        w.PendingRebelRegions = [seat];
        w.PendingRebelPolity = polity;
        w.HadCivilWar = true;
        return true;
    }

    /// <summary>★内戦の決着の当てはめ＝③政体・⑤人と政治の1本（2026-09-28 処理の一元化 段2 束2）。
    /// 戦争の形（MultiNationSim.ReabsorbNations＝講和の予約を四半期末に回収）と、地図の上に立てられない蜂起の即時の決着（<see cref="TriggerFactionRevolt"/>＝州が1つの国・単国の世界）が同じ規則を通る。
    /// （旧経路の内戦 ProcessCivilWar は 2026-09-28 段2 束3 で削除）
    /// <list type="bullet">
    /// <item>反乱の勝ち＝目標政体が今の政体と違えば政体変更の実行部（<see cref="ExecuteRegimeChange"/>・暴力の転換）／同じ政体の要求戦なら運動の要求をのむ（<see cref="ConcedeToFaction"/>）。</item>
    /// <item>政府の勝ち＝鎮圧の後始末＝<paramref name="rebels"/>（反乱していた州の住民・作者裁定 d）の闘争度の緩和と断裂の窓（1849年の反動）。
    ///   正本は旧経路の鎮圧の行（⚠旧の戦争の形の休眠の後始末 RevoltAftermathOnWarPath は正統性も削っていたが、旧経路の鎮圧の行には無い＝削除した）。</item>
    /// <item>どちらでも運動は燃え尽きる（<see cref="SpendRevolt"/>）。</item>
    /// </list>
    /// ⚠旧＝戦争の形で要求戦の反乱が勝っても何も起きなかった（ConcedeToFaction の呼び手が旧経路だけ）・革命の勝ちは登録簿と建国制度の2行だけ。</summary>
    internal static void SettleRevolt(World w, Coefficients c, bool rebelWon, Polity target, FactionState? movement, IEnumerable<Pop> rebels)
    {
        if (rebelWon)   // 反乱が依然優勢＝革命勝利→目標政体へ（失脚で不安定）
        {
            // F3b 経済決着：政体は変わらない＝運動の要求をのませる（F1譲歩の大型版＝要求top-kを複数段・政治資本を大量に燃やす）。
            if (target != w.Polity) ExecuteRegimeChange(w, c, target, violent: true);   // 内戦を経た勝利＝暴力の転換（覚醒・断裂の窓・否認・失脚・土地改革は即時の政変と同じ1本）
            else if (movement is not null) ConcedeToFaction(w, c, movement);
        }
        else   // 中央が持ちこたえた＝反乱鎮圧（闘争度緩和）
        {
            foreach (var p in rebels) p.Militancy *= c.ReformMilitancyRelief;
            // 第6段6-8b：内戦の制圧＝断裂の窓（1849年の反動＝墺1851大晦日勅令・普三級選挙法）。
            w.RuptureWindowQuarters = Math.Max(w.RuptureWindowQuarters, (int)c.RuptureWindowQuartersOnCrush);
        }
        SpendRevolt(w, movement);
    }

    /// <summary>F3b 決着＝運動の燃え尽き（2026-07-12）：鎮圧＝組織壊滅（1849年の反動・指導部の処刑流刑）／勝利＝要求は法へ・運動は解散／
    /// 独立・分断＝国内の運動でなくなる。結束を残すと同じ運動が数年おきに再蜂起し続ける（蘭で実測6回/125年）＝再蜂起には組織の再建（結束の再成長≈20年）が要る。
    /// ★2026-09-28（処理の一元化 段2 束2）：戦争の形の白紙講和（分断）と独立の確定もここを通す＝旧はどちらも参照を残したまま return し、
    /// 一度膠着した国は二度と蜂起できない恒久の錠になっていた（CheckFactionRevolt の注記）。主体の参照はその運動のときだけ外す（同時に別の内戦が進んでいても他の運動を解かない）。</summary>
    internal static void SpendRevolt(World w, FactionState? movement)
    {
        if (movement is null) return;
        movement.Cohesion = 0;
        if (ReferenceEquals(w.RevoltingFaction, movement)) w.RevoltingFaction = null;   // 決着＝内乱の主体を解放（勝敗どちらでも）
    }

    // F3b 組織運動の内乱（牙と変異・2026-07-12）：結束した国内勢力（F3a＝州×階級×主軸のクラスタ）が保持力を上回ると内乱化。
    // Transition＝国家集計の即時革命／F1＝無組織の食料一揆（即時・小）／F3b＝**持続した組織運動の大内乱**（結束ゲート0.5≈3年超）＝
    // 同じ闘争度を読むが関数形と時定数が別・F1との排他（内乱の進行と州の状態＝2026-09-29 に共有クールダウンを撤去）。決着は目標政体で2分岐＝
    // 政治（主軸の最寄り政体≠現政体）→政体戦／経済→要求戦（ConcedeToFaction）。どちらも本拠州（首都も）に拠る反乱国の戦争（TriggerFactionRevolt）。
    // ⚠旧の民族分岐（本拠州が少数派→旧経路の単一州の分離戦）は 2026-09-28 段2 束3 で削除（作者裁定 g）＝民族の分離は支配度の分離独立（CheckSecession）の1本。
    private static void CheckFactionRevolt(World w, Coefficients c, double coerce)
    {
        if (c.FactionRevoltScale <= 0 || w.FactionRng is null) return;
        // ★段6＝クールダウンの畳み込み（F3b蜂起doc §6・2026-09-08）＝**2026-09-29 点火**（作者裁定「蜂起の共有クールダウンは撤去＝根拠のない待ち」＝
        //   旗 RevoltCooldownRetire・タイマー World.RevoltCooldownQuarters・係数 RegionalRevoltCooldown を削除）。旧＝4四半期の固定タイマーで
        //   「同一国で年に何度も燃えない」を作っていた＝**物理でない待ち行列**（実測でヴュルテンベルクが69年間ぴったり毎年7月1日に蜂起＝
        //   タイマーがメトロノームとして見えていた）。F1 が燃えるたびに F3b も4四半期止まった（結束のある運動の86四半期のうち60四半期）。代替は2つとも既に実体がある：
        //     (a) 燃え尽き＝決着で結束0（Politics.SpendRevolt）。再び戦えるまでロジスティック再成長 0.05×(0.5+意識)×(変異?2:1)×(1−結束) で4〜16年＝**枯れて満ちる物理量**。
        //     (b) 物理的排他＝「内乱が今まさに地図の上で進行している」＝新しい旗は立たない（反乱国の州は w.Regions を離れ、
        //         建国前の予約中は PendingRebelRegions が握る）。⚠これは時間ではなく**状態**なので待ち行列にならない。
        //   ⚠往復の歯止めは待ちでなく政権への失望（World.RecentRegimeChanges＝同じ日の作者裁定・Politics.Step の動員と正統性の床）。
        // 既に内乱が地図の上で進行している＝新しい旗は立たない（状態＝終われば必ず解ける／時間で数えない）。
        // ⚠述語に `RevoltingFaction is not null` を入れない：参照は戦争の決着でだけ外れる（再併合・白紙の分断・独立の確定＝2026-09-28 から3つとも
        //   Politics.SpendRevolt を通る。旧＝白紙と独立は参照を残したまま return し、この述語に入れると一度膠着した国は二度と蜂起できない恒久の錠になった）。
        //   状態（地図の上の内乱）だけを見る＝参照の外し忘れが錠にならない。
        // ⚠InCivilWar は CivilWarPredicateUnified（点火済み1.0）に依存する＝0へ戻すと戦争経路の内戦をこの述語が見なくなる（段5の民兵の剥離と同じ依存）。
        if (w.InCivilWar || w.PendingRebelRegions is not null) return;
        FactionState? best = null; double bestStrength = 0, bestDespair = 0;
        foreach (var f in w.Factions)
        {
            if (f.Cohesion < c.FactionCohesionFightThreshold) continue;
            // ★段0＝幽霊の封じ（F3b蜂起doc §7-1・2026-09-08・係数なし）。反乱国家が建つと本拠州は親の Regions を離れるが、
            //   FactionState は親のリストに残って Region 参照を握ったままになる（建国路は w.Factions を掃除しない）。
            //   州の State は Active のままなので上の除外を素通りし、(a) FactionStrength が**他国のPOP**を読み、
            //   (b) 選ばれると親の所有していない州に蜂起を立てる（旧＝民族分岐が SecedingRegion に代入→旧経路の内戦の年数が減らないまま
            //   Transition/分離/F3b/F1 が全停止する＝**破綻**）。既存述語で所有だけを見る。
            if (!w.Regions.Contains(f.Region)) continue;
            double strength = FactionStrength(w, c, f);
            // ★段4＝点火の付け替え（F3b蜂起doc §3-2・2026-09-08）。旧：点火も決着も **同じ FactionStrength vs 保持力+郷勇** ＝
            //   蜂起は勝てるときにしか始まれない（成功率100%＝§2の物差しで不合格）。さらに左辺は f.Size に比例する**州クラスタ規模**、
            //   右辺の郷勇は w.AllPops() の**全国**スケール＝点火が人口に反比例し、人口最大の清で太平天国が最も出にくかった。
            //   新：戦力比は点火から外し（＝「勝てるか」は決着＝戦争側にだけ残す）、連言2門にする。
            //     門1＝組織された意志（示強量）：FactionStrength ÷ f.Size ＝ f.Size が代数的に約分され
            //          AvgMilitancy×結束×(1−発言権)×FactionRevoltScale（全因子[0,1]）が残る＝規模の次元非対称が構造的に消える。
            //     門2＝州の絶望：RegionRevoltHeat（F1の一揆と**同じ既存閾値** RegionalRevoltThreshold で測る）＋怨嗟。
            //   ⚠積にしない（闘争度が intensity と heat の両方に入り二重計上になる＝本プロジェクトで最も再発するバグ型）。
            //   ⚠選択キーも「最も強い運動」→「最も**絶望した**運動」へ移す。旧キーのままだと示強量0のクラスタが選定を専有し
            //     （bestStrength 最大の1件だけが下流へ出る）、絶望の門が実質効かない。
            //   ⚠否定形ゲート（!(x > t)）＝NaN は必ず「点火しない側」＝旧挙動へ倒れる（Math.Clamp/Min/Max は NaN を除去しない）。
            if (!(c.RevoltIgnitionIntensity > 0))
            {
                if (strength > bestStrength) { bestStrength = strength; best = f; }
                continue;
            }
            double intensity = strength / Math.Max(1e-9, f.Size);   // 分子が Size に比例＝Size→0 でも 0 に倒れる（比が痩せて跳ねる型でない）
            double despair = RegionRevoltHeat(f.Region, c)
                           + c.RevoltGrievanceWeight * Math.Min(1.0, c.FactionRejectionMutation * f.Rejections);   // 怨嗟の飽和は変異ロール（Factions.cs）と同じ書き方
            if (!(intensity > c.RevoltIgnitionIntensity)) continue;   // 門1：組織された意志
            if (!(despair > c.RegionalRevoltThreshold)) continue;     // 門2：州の絶望（F1と同じ既存閾値＝新しい閾値を作らない）
            // 最も絶望した運動が立つ。⚠同じ州の運動は熱が**厳密に同値**（同じ純関数・同じ州）＝必ず並ぶので、
            //   並んだときだけ強い方が先頭に立つ（同一州＝按分後の保持力も同じ＝規模の次元非対称は入らない・新しい数もゼロ）。
            if (despair > bestDespair || (despair == bestDespair && strength > bestStrength)) { bestDespair = despair; bestStrength = strength; best = f; }
        }
        if (best is null) { ClearFactionCrisis(w); return; }   // ★要求は法の経路：危機が立っていない＝通知を下ろす（運動は解散しない・記録は次の通知で取り直す）
        double hold = LocalizedHold(w, c, best.Region, coerce);   // R2 蜂起R群2026-08-16：F3b保持力の州按分（FactionRevoltLocalCoerce=0＝coerceのまま＝ビット恒等／★既定は点火0.5＝半按分）
        double militia = LoyalistMilitia(w, c, best);   // 郷勇（全国）＝決着（Politics.cs の旧経路 attack/def）と鎮圧の値段で共有＝死蔵にならない
        if (!(c.RevoltIgnitionIntensity > 0))   // ★段4：0＝旧2門（力の門＋好機/圧倒）＝1文字も触らない厳密恒等の側
        {
            // ★段6の判定（doc §6「FactionOpportunism 一族4本を畳めるか」）＝**畳まない**。理由は二重計上ではなく到達性：
            //   この旧2門は RevoltIgnitionIntensity=0（＝現在の既定＝休眠）のとき**唯一の点火条件**であり、消すと
            //   「危機が立った＝毎四半期その運動が必ず蜂起する」世界になる（力の門も好機のブレーキも無くなる）。
            //   doc §6 が「二重計上だから消せる」と書いたのは**段4を点火した後の世界**の話＝点火と同時に、この if ブロックごと落ちる。
            //   ⇒ 4本（FactionOpportunism / FactionStrikeLegitimacy / FactionStrikeDrawdown / FactionOverwhelmFactor）は
            //     段4を点火する回に**このブロックごと**畳む（今畳むと世界が壊れる＝掃除ではなく挙動の破壊）。
            if (bestStrength <= hold + militia) { ClearFactionCrisis(w); return; }   // ★要求は法の経路：立たなかった＝通知を下ろす
            // 第6段6-5①（2026-07-28）結束の予算化＝一斉投入のタイミング（中核ルールdoc §3-3①/§3-4）：閾値を越えても体制が健在なら
            // 待って蓄積を続け（静かな数十年＝結束は減らずに積み上がる）、弱み（低正統性・交戦・恐慌）か圧倒（保持力の大差超え）で打つ
            // ＝賢い挑戦者は歴史を平坦にせず遅らせて凝縮する（1848/1917の形）。FactionOpportunism=0で旧＝閾値即発火。
            if (c.FactionOpportunism > 0)
            {
                // ★段3＝亡霊の残り(i)（F3b蜂起doc・2026-09-08・係数なし）：「交戦」から**内戦を除く**。
                //   Government.AtWar は CivilWarParent 付きの戦争も数えるので、内戦一元化の主経路では
                //   反乱が1つ立った瞬間に親が**恒久に弱み**になり、好機のブレーキも圧倒判定も両方外れた＝1つ目の蜂起が2つ目を呼ぶ。
                //   体制の弱さは既に保持力（w.Coercion＝正統性×財源率×戦争消耗）に入っているので、ここで数えるのは外征の消耗だけでよい。
                bool externalWar = false;
                if (w.Diplomacy is { } dipEx && w.SelfPower is { } spEx)
                    foreach (var war in dipEx.Wars)
                        if (war.CivilWarParent is null && (war.A == spEx || war.B == spEx)) { externalWar = true; break; }
                bool weakness = w.Legitimacy < c.FactionStrikeLegitimacy
                             || w.GdpDrawdown.Value > c.FactionStrikeDrawdown.Value
                             || externalWar;
                if (!weakness && bestStrength < (hold + militia) * c.FactionOverwhelmFactor) { ClearFactionCrisis(w); return; }   // R2：圧倒判定も同じ実効保持力を読む（フラグ0では hold==coerce＝ビット恒等）。★要求は法の経路：待つ＝通知を下ろす
            }
        }
        // ★D-34 計器（2026-09-03・作者要望）：ここに来た＝**危機が立った**。決着の**共通の分母**として1回ずつ数える
        //   （⚠試行と成立の粒度を揃える＝分子と分母の非対称を作らない）。試行と成立の Hit は CrisisThroughLawPath（評価の四半期だけ）。
        //   ⚠段6（2026-09-08）：無血鎮圧の枝を物理削除したので FactionSuppress の分母も外した（Hit が構造的に0のサイトを
        //   分母に置くと「成立率0%の機構がある」という嘘の内訳になる＝計器が一番バグる）。残る3行の成立率の和が1。
        // ★要求は法の経路（作者裁定2026-09-11・同日点火）：決着は1本＝通知→通常の法改正（AI＝SelfPlay.Apply／プレイヤー＝法律タブ）→要求した法が動かなければ蜂起（政体が同じでも）。
        //   旧3枝（受諾／別軸譲歩／蜂起・AI即決とプレイヤーの択＝ApplyDemand 直書きの蜂起専用改正ルート）は物理削除。
        CrisisThroughLawPath(w, c, best, coerce);
    }

    // 蜂起の点火（CrisisThroughLawPath＝要求した法が通常経路で1段も動かなかった運動）。目標政体で2分岐＝政体戦／体制内要求戦。
    private static void TriggerFactionRevolt(World w, Coefficients c, FactionState best, double coerce)
    {
        w.FactionRevolts++;
        best.Rejections += 1;   // ★段3：対決＝最後の拒否（内戦へ到達＝怨嗟は満額）
        // ★段5＝民兵（作者裁定3・2026-09-08）：蜂起した運動の**自分のブロックのPOP**が、不満度に応じて武器を取る。
        //   ⚠**階級の禁止規則をどこにも書かない**（体制側の郷勇 LoyalistMilitia は有産ブロックだけで組まれ〔郷勇/湘軍/團練・1789年の国民衛兵も納税資格のある能動市民〕、
        //     反乱側のヴァンデ1793・カルリスタ1833・西南戦争1877は貴族が率いた）＝読むのは「その運動のブロックか」と「どれだけ怒っているか」だけ。
        //     ブロックの分類は FactionStrength／LoyalistMilitia と**同じ物差し**（新しい分類を作らない）。
        //   ⚠兵士POPは民兵ではない（常備軍そのもの）＝守備兵の去就は建国路の寝返り（MultiNationSim.SpawnNation の段5＝寝返る割合は守備兵の闘争度）が別に決める。
        //   ⚠人を作らない＝**札**（率）を貼るだけ。人口も貨幣も1ビットも動かない（保存則に触れない）。
        //   ⚠下の決着（地図の上の蜂起・即時の決着）より**前**に置く＝どの決着経路でも民兵は立つ（経路ごとの非対称を作らない）。
        //   ⚠RevoltMilitiaRaise=0（既定）では1命令も走らない＝厳密恒等。
        if (c.RevoltMilitiaRaise > 0)
        {
            foreach (var p in best.Region.Pops)
            {
                if (p.IsSoldier) continue;
                int pb = PolicyPreferences.BlocOf(p);
                if (pb != best.Bloc) continue;
                double share = Math.Min(1.0, c.RevoltMilitiaRaise * p.Militancy);
                if (!(share > p.MilitiaShare.Value)) continue;   // 否定形＝NaN は据え置き（上げる側にしか書かない＝再蜂起で札が減らない）
                p.MilitiaShare = new Share(share);
            }
        }
        // ★蜂起は地図の上に立つ（内戦一元化・2026-08-30／首都の分離独立・2026-09-28 処理の一元化 段2 束3・作者裁定 a）＝本拠州に拠る反乱国を建て、
        //   既存の戦争システムで戦う（RaiseRevolt）。首都発の運動も同じ＝蜂起側が首都州を握り、政府は残る州へ遷都して戦う。
        //   目標＝主軸イデオロギーの最寄り政体。今の政体と違えば政体戦（反乱の勝ち＝政体変更の実行部）／同じなら要求戦（チャーティスト・パンと土地＝勝てば要求をのませる）。
        //   ⚠旧＝首都発の運動は戦争化せず旧経路（第二の戦争エンジン ProcessCivilWar）で数年を戦った（是正2026-08-30＝幕府テスト赤の根因は、
        //     初版が首都運動を**地方へ**旗揚げさせて平和的な転換まで分離戦争に化けさせたこと。蜂起は要求が法で1段も動かなかった運動だけ＝平和的な転換ではない。
        //     首都の運動は首都に拠る＝一元化の対象は地図の上の戦闘）。旧の民族分岐（本拠州が少数派→旧経路の単一州の分離）は削除（作者裁定 g）。
        Polity target = NearestPolity(best.Ideo);
        if (RaiseRevolt(w, best.Region, target))
        {
            w.RevoltingFaction = best;   // 決着（Diplomacy.ResolvePeace の内戦分岐→再併合・分断・独立の確定）で運動は燃え尽きる
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}が蜂起し{best.Region.Name}に拠って立った（内戦）");
            return;
        }
        // 地図の上に立てられない（州が1つの国・単国の世界）＝即時の決着（条件②）。判定は旧経路の内戦の決着の物差しだけを残す
        //   ＝勢力強度 vs 保持力＋郷勇（点火の門とは別＝「勝てるか」は決着にだけ）。当てはめは戦争の形の決着と同じ1本（SettleRevolt）＝
        //   反乱の勝ち＝政体変更の実行部／要求をのむ・政府の勝ち＝本拠州の住民に鎮圧の後始末（作者裁定 d）・運動は燃え尽きる。
        bool won = FactionStrength(w, c, best) > coerce + LoyalistMilitia(w, c, best);
        w.LogAdd(won
            ? $"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}が蜂起し政府が屈した"
            : $"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}の蜂起が鎮圧された");
        SettleRevolt(w, c, won, target, best, best.Region.Pops);
    }

    // 要求リストの表示文字列（PendingFactionCrisisDemands）。
    private static string DemandsJa(List<FactionSnapshot.Demand> d) =>
        d.Count > 0 ? string.Join("・", d.ConvertAll(x => FactionSnapshot.DemandJa(x.Axis, x.Increase))) : "体制への漠然とした不満";

    // 通知を下ろす（PendingFactionCrisis と表示用の札・運動側の要求の記録）。
    private static void ClearFactionCrisis(World w)
    {
        if (w.PendingFactionCrisis is { } f) { f.CrisisDemands = null; f.CrisisDemandPos = null; }
        w.PendingFactionCrisis = null; w.PendingFactionCrisisDemands = "";
    }

    /// <summary>★要求は法の経路（作者裁定2026-09-11・同日点火「蜂起前の要求が簡単に通り過ぎる／受け入れる場合も本当の法改正と同じ経路／
    /// モーダルは通知用・蜂起専用の改正ルートは作らない／蜂起は要求が通らないことによるもの＝政体の else if は不要」）。危機が立った運動の決着は3枝でなく1本：
    /// <list type="number">
    /// <item>通知＝PendingFactionCrisis に札を立てる（AI・プレイヤー共通＝IsPlayer で分岐しない）。要求（法だけ）とその時点の位置を運動側に記録して返る
    ///   ＝通常の法改正の遅延（翌四半期の DecideQuarter／法律タブ→Government.Step）を待つ。⚠これはタイマーではない＝Simulation の四半期順序そのもの。</item>
    /// <item>翌評価（通知済みの同じ運動で再び門を越えた四半期）＝記録した要求の法が**通常の経路で動いたか**を AxisPosition で比べる。
    ///   本丸（先頭）が動いた＝受諾の実体（怨嗟0＝旧受諾と同じ記帳・怨嗟の解消は施行の時点であって目標を置いた時点ではない）／
    ///   本丸以外だけ動いた＝別軸譲歩の実体（本丸拒否＝怨嗟+1＝旧の別軸譲歩と同じ記帳）／1段も動かなかった＝要求が通らなかった＝蜂起。</item>
    /// </list>
    /// ⚠法を書く行はここに1行も無い（ApplyDemand を呼ばない）。目標を置くのは AI＝SelfPlay.Apply（Parliament.TargetCrisisDemands）／プレイヤー＝法律タブ／初心者＝同じ関数＝同じ p.X。
    /// ⚠政体の分類（NearestPolity）は点火の門に使わない＝TriggerFactionRevolt の結果側（分離戦／政体戦／要求戦）にだけ残る。
    /// ⚠待ちは書かない（共有のクールダウンは 2026-09-29 に撤去＝新しい待ち行列を作らない）。運動は解散しない（09-09）。
    /// 計器＝3行の試行は**評価の四半期**にだけ数える（通知の四半期は分母に入れない＝成立率の和が1のまま）。</summary>
    private static void CrisisThroughLawPath(World w, Coefficients c, FactionState best, double coerce)
    {
        if (!ReferenceEquals(w.PendingFactionCrisis, best) || best.CrisisDemands is null)   // 記録なし＝旧セーブから読んだ札も通知からやり直す
        {
            if (w.PendingFactionCrisis is { } prev && !ReferenceEquals(prev, best)) { prev.CrisisDemands = null; prev.CrisisDemandPos = null; }
            w.PendingFactionCrisis = best;
            RecordCrisisDemands(w, best);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}が要求を掲げた（{w.PendingFactionCrisisDemands}）");
            return;   // 通常の法改正の遅延＝翌四半期の Government.Step が目標を読む
        }
        MechanismProbe.Eval(MechanismProbe.Site.FactionConcede);
        MechanismProbe.Eval(MechanismProbe.Site.FactionCompensate);
        MechanismProbe.Eval(MechanismProbe.Site.FactionRevolt);
        int moved = CrisisDemandsMoved(w, best);
        if (moved == 1)
        {
            best.Rejections = 0;
            RecordCrisisDemands(w, best);   // 次の要求（段が進めば次段・端に達すれば次の軸）を取り直す
            MechanismProbe.Hit(MechanismProbe.Site.FactionConcede);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{Factions.DisplayName(best)}の要求を法で受け入れた");
            return;
        }
        if (moved == 2)
        {
            best.Rejections += 1;
            RecordCrisisDemands(w, best);
            MechanismProbe.Hit(MechanismProbe.Site.FactionCompensate);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{Factions.DisplayName(best)}へ別の軸で譲歩した（本丸の要求は通っていない）");
            return;
        }
        ClearFactionCrisis(w);
        MechanismProbe.Hit(MechanismProbe.Site.FactionRevolt);
        TriggerFactionRevolt(w, c, best, coerce);   // 要求が通らなかった＝蜂起（政体が同じでも）
    }

    // 通知時点の要求（法だけ＝ComputeDemands の lawsOnly＝危機側の読み手だけが即時値の軸を除く・綱領側は全軸）と位置を運動側に記録し、表示用の文字列も更新する。
    private static void RecordCrisisDemands(World w, FactionState f)
    {
        var d = FactionSnapshot.ComputeDemands(w, f.Bloc, lawsOnly: true);
        f.CrisisDemands = new int[d.Count]; f.CrisisDemandPos = new double[d.Count];
        for (int i = 0; i < d.Count; i++)
        {
            f.CrisisDemands[i] = d[i].Increase ? (int)d[i].Axis + 1 : -((int)d[i].Axis + 1);
            f.CrisisDemandPos[i] = FactionSnapshot.AxisPosition(w, d[i].Axis);
        }
        w.PendingFactionCrisisDemands = DemandsJa(d);
    }

    // 記録した要求の法が要求の向きへ動いたか：0＝1段も動かず／1＝本丸（先頭）が動いた／2＝本丸以外だけ動いた。要求が無い運動は 0（法で応じようがない）。
    // ⚠否定形＝位置が NaN のときは「動いた」側（＝蜂起しない側）へ倒す（Math.Clamp は NaN を除去しない）。
    private static int CrisisDemandsMoved(World w, FactionState f)
    {
        int moved = 0;
        for (int i = 0; i < f.CrisisDemands!.Length; i++)
        {
            int code = f.CrisisDemands[i]; bool inc = code > 0;
            var axis = (PolicyAxis)(Math.Abs(code) - 1);
            double now = FactionSnapshot.AxisPosition(w, axis), was = f.CrisisDemandPos![i];
            bool advanced = inc ? !(now <= was + 1e-9) : !(now >= was - 1e-9);
            // ★段になった税の軸（作者裁定2026-09-12 C1）＝段が動いていなくても**政府がその向きの段を目標に置いている**なら施行中＝要求は拒まれていない。
            //   税の段は Move の指数接近（1期にギャップの2〜18%）で動き、段の境界を跨ぐには42.5〜57.5%が要る＝評価の四半期には構造的に1段も動かない
            //   ＝この一行が無いと税を要求に載せた瞬間「必ず蜂起」になる。目標を置いた四半期から Fiscal は新しい率で取り立てている＝約束でなく施行。
            //   ⚠これで鎮まるのは**蜂起するかどうか**だけ。闘争度の緩和（Politics.Step 先頭の F1 譲歩）は従来どおり段が動いた時点＝
            //     「怨嗟の解消は施行の時点であって目標を置いた時点ではない」（2026-09-09/09-11）は破っていない。
            if (!advanced) advanced = Government.EnactingStage(w, axis, inc);
            if (!advanced) continue;
            if (i == 0) return 1;
            moved = 2;
        }
        return moved;
    }

    // F3b 反乱の戦力＝**人数 × 闘争度 × 結束 ×（1−発言権）**（challenge/coerceと同じ物差し）。⚠段4（RevoltIgnitionIntensity>0）では
    // これを **f.Size で割った示強量**が点火の門1になり、この量そのものは決着（勝てるか）と鎮圧の値段にだけ残る＝点火と決着はもう同じ式ではない。
    // ★段2（作者裁定2026-09-09 part3）＝**発明された2因子を削除**した：
    //   ・年季 min(1, QuartersActive/FactionSeniorityQuarters)＝**経過時間だけで強くなる成長タイマー**（「根拠のないクールダウンを作らない」の鏡像）。
    //     しかも結束（Cohesion）が既に4〜16年のロジスティックで「長く続いた運動は組織されている」を意味していた＝同じことの二重。
    //   ・組織核 org＝ブロックの意識加重平均。意識は実在するPOP属性だが、**組織の物差しが結束と2本**になり（装備＝資金÷人数を足すと3本）、
    //     貴族や資本家の合流は金と意識を同時に持ち込むので同じ事実を3度数えることになる＝組織は結束1本に畳む。
    // 【回路なし】平和的回路のガス抜き：票を持つブロックの組織運動は議会経路へ向かい武装蜂起しない
    // ＝チャーティズムが参政権拡大で政党化して消えた物理（MilVoiceと同じ階級ゲート・**法を読む＝MilVoiceのFranchiseVoiceと同じ物差し**。
    // 専制のAIが革命回避で発行する紙の参政権も回路と数える＝闘争度側と整合。「選挙の実在」ゲート版は2026-07-12に実測比較し
    // 列強立憲圏の早期独裁化カスケード＝exact36を招いたため不採用＝f3iバッチ参照）。女性係数は掛けない。
    // 旧支配層(bloc0)は宮廷権力＝常に回路あり（ヴァンデ/カルリスタ型の反動蜂起は Transition の国家集計が担う）。
    public static double FactionStrength(World w, Coefficients c, FactionState f)
    {
        double voice = f.Bloc == 0 ? 1.0
                     : f.Bloc == 1 ? (w.Suffrage >= Suffrage.Census ? 1.0 : c.ModernEliteSubCensusVoice)
                     : (w.Suffrage >= Suffrage.AdultMale ? 1.0 : 0.0);   // 大衆と第4束「小営業」(bloc3)＝男子普選まで声なし（制限選挙が店主・親方を締め出した物理＝1848の母体。プチブル利益束2026-08-15）
        // ★裁定1＝発言権の床（F3b蜂起doc §8(A)・作者裁定②・2026-09-08）。(1−発言権) は旧支配層(bloc0)で恒久0、
        //   大衆(bloc2)も男子普選の到達後は恒久0＝**強度が恒久ゼロ**になり、点火式をどう直しても武装できない。
        //   構造的に落ちるのは反動の蜂起（ヴァンデ1793・カルリスタ1833・西南戦争1877・クリステロ1926）と
        //   普選後の大衆蜂起（スパルタクス団1919・ルール1920・アストゥリアス1934）＝史実の主要蜂起の1/6前後。
        //   >0 で床を敷く＝票を持つブロックにも小さな武装能力が残る（議会が塞がれば街頭へ）。
        //   ⚠段4本体（RevoltIgnitionIntensity）とは**別の係数**＝参政権の意味は決着・UI・AIの択も読むので単独で測れるようにする。
        //   ⚠否定形＝床0では有限値のとき必ず偽＝1命令も書き換えない厳密恒等。
        double silenced = 1.0 - voice;
        if (!(silenced >= c.RevoltVoiceFloor)) silenced = c.RevoltVoiceFloor;
        double strength = f.Size * f.AvgMilitancy * f.Cohesion * silenced * c.FactionRevoltScale;
        // ★第5因子＝装備（設計メモ§7 作者裁定2026-09-09 (a)・2026-09-11）：国軍の即応度が装備充足（MilitaryEquipFill）を掛けるのと同じ物差し
        //   （FactionState.EquipFill＝同じ関数 EconomyStep.EquipFill の値＝運動が市場で買えた武器の充足）。丸腰の運動は戦力を持たない
        //   （国軍の「材で詰まった戦争は負ける」と対称）。⚠段2の注記「装備＝資金÷人数」は**採らない**＝資金は市場で財に化けてから数える。
        //   ⚠休眠（RebelArmsMarketEnabled=0）では**掛けない**（×1.0 すら実行しない）＝1命令も変えない厳密恒等。
        if (c.RebelArmsMarketEnabled > 0) strength *= f.EquipFill;
        return strength;
    }

    // 主軸イデオロギーに4軸座標が最も近い政体（新表なし＝既存座標の純関数。one-hotのIdealPoint＝その思想自身の座標）。
    internal static Polity NearestPolity(int ideo)   // internal＝番人テスト（法の経路 (c)＝主軸の最寄り政体が現政体でも要求が通らなければ蜂起）が同じ式で盤面を作る（2026-09-11）
    {
        var oneHot = new double[Ideologies.Count]; oneHot[ideo] = 1.0;
        var coord = Ideologies.IdealPoint(oneHot);
        Polity best = Ideologies.Polities[0]; double bd = double.MaxValue;
        foreach (var pol in Ideologies.Polities)
        {
            var pc = Ideologies.BasePolityCoord(pol);
            double d = 0; for (int i = 0; i < 4; i++) { double x = coord[i] - pc[i]; d += x * x; }
            if (d < bd) { bd = d; best = pol; }
        }
        return best;
    }

    // F3b 湘軍：反乱と思想距離が遠い**有産ブロック**（旧支配層・近代エリート）が体制側で郷勇を組む＝保持側の加算項。
    // 距離が LoyalistDistanceFloor 未満（思想的に近い反乱＝フロンド型）には立たない。異端（変異）の反乱には増し（曽国藩の檄）。
    // internal（2026-08-16）：戦争タブの国内勢力行が強さ比（FactionStrength÷(EffectiveCoerce+郷勇)）を表示するための読み取り専用公開
    //   （純関数・World非変更・InternalsVisibleTo("PriceOfBread.App")）。エンジン側の呼び出しは従来どおり同クラス内のみ。
    internal static double LoyalistMilitia(World w, Coefficients c, FactionState f)
    {
        if (c.LoyalistMilitiaWeight <= 0) return 0;
        var oneHot = new double[Ideologies.Count]; oneHot[f.Ideo] = 1.0;
        var fCoord = Ideologies.IdealPoint(oneHot);
        double militia = 0;
        // 有産ブロックのみ（大衆は郷勇の資金源にならない＝郷勇は郷紳・大資本の金）。
        // AllPops を1回だけ走査し bloc0/bloc1 を同時に集計（各ブロックへの積算順序は従来の2回走査と同一＝ビット恒等）。
        var size = new double[2];
        var shares = new double[2][] { new double[Ideologies.Count], new double[Ideologies.Count] };
        foreach (var p in w.AllPops())
        {
            int pb = PolicyPreferences.BlocOf(p);
            if (pb >= 2 || pb == f.Bloc || p.IsSoldier) continue;
            size[pb] += p.Size;
            for (int i = 0; i < Ideologies.Count; i++) shares[pb][i] += p.IdeologyShares[i] * p.Size;
        }
        for (int b = 0; b < 2; b++)
        {
            if (b == f.Bloc || size[b] <= 1e-9) continue;
            for (int i = 0; i < Ideologies.Count; i++) shares[b][i] /= size[b];
            var ideal = Ideologies.IdealPoint(shares[b]);
            double d = 0; for (int i = 0; i < 4; i++) { double x = ideal[i] - fCoord[i]; d += x * x; }
            double dist = Math.Sqrt(d);
            militia += size[b] * Math.Max(0, dist - c.LoyalistDistanceFloor) * (f.Mutated ? 1.0 + c.LoyalistHeresyBoost : 1.0);
        }
        return c.LoyalistMilitiaWeight * militia;
    }

    /// <summary>R2 蜂起R群2026-08-16（診断A2）：F3b点火の単位整合。FactionRevoltLocalCoerce=0＝coerceをそのまま返す＝**ビット恒等・新しい読み書きゼロ**。
    /// 何が壊れていたか：左辺 FactionStrength＝**州×ブロック×主軸クラスタ**の規模なのに、右辺は**全国**coerce＝
    /// 人口の大きい国ほど構造的に点火不能（太平天国が最も出にくいのは清、という逆理の根）。式自体は正しく、殺しているのは右辺のスケール。
    /// 実効coerce＝coerce×((1−f)+f×州pop/全国pop)＝f=0旧・f=1完全按分の**0..1ブレンド**（点火A/Bで中間値を掃引可。見る帯＝内戦分裂・清）。
    /// 州popは TotalPopulation と同じ物差し（Region.SumPopulation＝全pop）＝比の分子分母の定義を揃える（「比のガードは絶対値で書かない」の同族対策）。
    /// LoyalistMilitia（郷勇）は呼び手で**全国のまま**加算＝郷紳の金は全国から（LoyalistMilitia の設計コメントと整合）。
    /// internal＝ON経路テスト用（InternalsVisibleTo）。</summary>
    internal static double LocalizedHold(World w, Coefficients c, Region region, double coerce)   // ★B9（2026-09-07）：Region で受ける＝州一揆（CheckRegionalRevolt）と共有（新関数を作らない）
    {
        if (!(c.FactionRevoltLocalCoerce > 0)) return coerce;   // ⚠否定形＝フラグNaNも旧側（全国）へ倒す
        double blend = PoliticalQuantities.Clamp01(c.FactionRevoltLocalCoerce);
        double share = region.SumPopulation() / Math.Max(1.0, w.TotalPopulation);
        if (!(share >= 0 && share <= 1)) share = 1.0;   // ⚠否定形＝NaN/比の暴れは全国（旧挙動）へ倒す（Clamp/Min/MaxはNaNを除去しない）
        return coerce * ((1.0 - blend) + blend * share);
    }

    // F3b 経済決着（唯一の呼び手＝内戦の決着 SettleRevolt で要求戦の反乱が勝ったとき＝旧経路・戦争の形の両方）：運動の要求top-k（ブロックのPref×残り可動域＝FactionSnapshotと同じ導出）をのませる。
    // 最上位はFactionConcessionSteps段・以下1段。現行値とPolicy目標の両方を書く（書かないとGovernmentが翌期戻す）。
    // ⚠蜂起前の危機の決着はここを通らない（要求は法の経路＝CrisisThroughLawPath・2026-09-11）＝ApplyDemand の直書きは内戦に勝った運動だけ。
    // ★2026-09-28（処理の一元化 段2 束2）：戦争の形の内戦で要求戦の反乱が勝ったときもここを通る（旧＝再併合だけで要求は何ものまれなかった）。
    private static void ConcedeToFaction(World w, Coefficients c, FactionState f)
    {
        var demands = FactionSnapshot.ComputeDemands(w, f.Bloc);
        int steps = 0;
        for (int i = 0; i < demands.Count; i++)
            steps += ApplyDemand(w, c, demands[i].Axis, demands[i].Increase, i == 0 ? c.FactionConcessionSteps : 1);
        double spend = c.RevoltConcessionSpend * Math.Max(1, steps);
        // ⚠N2の注記（2026-09-21）：ここは**門（Government.PoliticalCapitalAffords）を通さない**＝内戦に負けた講和は払えなくても払う。
        //   結果 S>b になりうる＝残り a=0＝「内戦に負けて政治資本が尽きる」が表現できる（意図した唯一の例外）。
        //   そのとき人気のある法（代価0）は通る＝門を押し切りの枝の中に置いてあるから（進行不能にしない・P4）。
        if (Instruments.On) Instruments.ConcedeSpent(w, spend);   // 計器（読むだけ・(615)）：S＞b の原因が内戦の講和かを分ける
        w.MandateSpent += spend;        // 上限廃止（2026-07-26ユーザー裁定）
        w.MandateSpentNew += spend;
        // 緩和は運動の本拠州の大衆のみ（全国一律にしない＝Transition鎮圧緩和との二重を作らない）。戦争の形では本拠州は再併合で親へ戻った後。
        foreach (var p in f.Region.Pops) if (p.IsCommoner) p.Militancy *= c.ReformMilitancyRelief;
        w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{Factions.DisplayName(f)}の要求をのんだ");
    }

    // 要求1軸の強制施行（enum＝n段・スライダー＝FactionConcessionSlider×n・bool＝反転）。動いた段数を返す（政治資本の請求単位）。
    // internal（2026-08-10）：番人テストが軸ごとの施行経路を直接叩けるように（private のままだと「case を足したか」を
    // 政党構成や勢力の巡り合わせ越しにしか観測できず、テストが脆くなる）。呼び出し元は従来どおり同クラス内のみ。
    internal static int ApplyDemand(World w, Coefficients c, PolicyAxis a, bool inc, int n)
    {
        var p = w.Policy;
        int StepEnumAxis(int cur, int max, Action<int> set)
        {
            int v = Math.Clamp(cur + (inc ? n : -n), 0, max);
            // 前提法DAG（E4・2026-07-19）：勢力の経済決着による直接施行も前提を飛び越えない（男女普選←女性法の漏れを塞ぐ）。
            // 前提未達の段まで v を切り下げる＝譲歩は「前提が整う段まで」しか進めない（相互ロックなし＝非循環DAG）。
            if (inc) while (v > cur && !Government.LawPrereq(w, a, v, c).Ok) v--;
            set(v); return Math.Abs(v - cur);
        }
        // ★税・支出の段（作者裁定2026-09-12 R1）：段になった軸は**段で譲る**（staged:true）＝内戦に勝った運動の要求も段の格子に乗る。
        //   ⚠0.15刻み（FactionConcessionSlider）のままだと段の第1境界0.175を越えられず、Government が翌期に段へ丸め戻す＝
        //     「譲ったのに何も変わらない」＝政治資本だけが燃える no-op 譲歩になる（Parliament.TargetDemand と同じ罠）。
        //   段を持たない連続の軸（社会保障の水準・関税・治安維持力・専売）は旧式（0.15刻み）。
        int Slide(double cur, Action<double> set, bool staged = false)
        {
            if (staged)
            {
                int s0 = Government.TaxStage(a, cur), s = Math.Clamp(s0 + (inc ? n : -n), 0, Government.TaxLawStages);
                // 前提DAG（教育法・緊縮の逆進フロア）＝StepEnumAxis と同じ作法。
                // ★裁定B（2026-09-12 反証）：段の財政軸は**下げる側でも**前提を照会する＝<see cref="Government.GateInfo"/> が段の財政軸だけ
                //   上下とも LawPrereq を通すのと同じ理由（床＝「緊縮の下限」は下げる向きにしか現れない）。旧はここが inc の側だけで、
                //   勝った運動の経済決着だけが床を素通りできた（裁定Bで施行側の Max を廃した以上、翌期に押し戻す者はもう居ない）。
                if (inc) while (s > s0 && !Government.LawPrereq(w, a, s, c).Ok) s--;
                else while (s < s0 && !Government.LawPrereq(w, a, s, c).Ok) s++;
                if (s == s0) return 0;
                set(Government.TaxValue(a, s)); return 1;   // ★錨2026-09-18＝段↔率の写像は Government の2本だけ（軸ごとの錨）
            }
            double v = PoliticalQuantities.Clamp01(cur + (inc ? 1 : -1) * c.FactionConcessionSlider * n);
            set(v); return Math.Abs(v - cur) > 1e-9 ? 1 : 0;
        }
        int Flip(bool cur, Action<bool> set) { if (cur == inc) return 0; set(inc); return 1; }
        return a switch
        {
            PolicyAxis.Suffrage => StepEnumAxis((int)w.Suffrage, 3, v => { w.Suffrage = (Suffrage)v; p.Suffrage = w.Suffrage; }),
            PolicyAxis.EducationLaw => StepEnumAxis((int)w.EducationLaw, 3, v => { w.EducationLaw = (EducationLaw)v; p.EducationLaw = w.EducationLaw; }),
            PolicyAxis.Censorship => StepEnumAxis((int)w.Censorship, 2, v => { w.Censorship = (CensorshipLaw)v; p.Censorship = w.Censorship; }),
            PolicyAxis.LaborUnion => StepEnumAxis((int)w.LaborUnion, 2, v => { w.LaborUnion = (LaborUnionLaw)v; p.LaborUnion = w.LaborUnion; }),
            PolicyAxis.ShorterHours => StepEnumAxis((int)w.WorkHours, 3, v => { w.WorkHours = (WorkHours)v; p.WorkHours = w.WorkHours; }),
            PolicyAxis.CornLaws => StepEnumAxis((int)w.CornLaws, 2, v => { w.CornLaws = (ReformLevel)v; p.CornLaws = w.CornLaws; }),
            PolicyAxis.LandReform => StepEnumAxis((int)w.LandReform, 2, v => { w.LandReform = (ReformLevel)v; p.LandReform = w.LandReform; }),
            PolicyAxis.SerfEmancipation => StepEnumAxis((int)w.SerfEmancipation, 2, v => { w.SerfEmancipation = (ReformLevel)v; p.SerfEmancipation = w.SerfEmancipation; }),
            // ★女性の権利の梯子（統合法・2026-08-10）：勢力の経済決着による直接施行も**段を飛ばさない**（StepEnumAxis が前提DAG＝StagePrereq で切り下げる）。
            PolicyAxis.WomenRights => StepEnumAxis((int)w.WomenRights, 3, v => { w.WomenRights = (WomenRightsLaw)v; p.WomenRights = w.WomenRights; }),
            PolicyAxis.IncomeTax => Slide(w.TaxRate.Value, v => { w.TaxRate = new Share(v); p.IncomeTaxTarget = v; }, staged: true),   // ★R1：段
            PolicyAxis.SocialSecurity => Slide(w.SocialSecurity, v => { w.SocialSecurity = v; p.SocialSecurityTarget = v; }),   // ⚠社会保障は既に法（Government が SocialSecurityLaw を StepEnum）＝段の対象外
            PolicyAxis.Tariff => Slide(w.Tariff, v => { w.Tariff = v; p.TariffTarget = v; }),   // ⚠関税は R3 で段の対象外
            PolicyAxis.PollTax => Slide(w.PollTax, v => { w.PollTax = v; p.PollTaxTarget = v; }, staged: true),                               // ★R1：段
            PolicyAxis.ProgressiveTax => Slide(w.ProgressiveTax, v => { w.ProgressiveTax = v; p.ProgressiveTaxTarget = v; }, staged: true),   // ★R1：段
            // 02断線走査（2026-07-28）：施行できない11軸のうち表示つき3軸＋E5/E6の8軸を接続＝「要求受諾が政治資本だけ燃やすno-op」の解消。
            // 治安維持力の緩和＝ポップアップに表示されながら唯一実装されていなかった大衆の第一要求（CoercionFollowsForce下では次期に軍規模へ再追従＝一時的緩和として正しい）。
            PolicyAxis.Coercion => Slide(PoliticalQuantities.Clamp01(w.BaseCoercion / Math.Max(1e-9, c.CoercionMax)), v => { w.BaseCoercion = v * c.CoercionMax; p.CoercionTarget = v * c.CoercionMax; }),
            PolicyAxis.EducationSpending => Slide(w.EducationSpending, v => { w.EducationSpending = v; p.EducationSpendingTarget = v; }, staged: true),   // ★R1：段
            PolicyAxis.LandTax => Slide(w.LandTax, v => { w.LandTax = v; p.LandTaxTarget = v; }, staged: true),   // ★R1：段
            PolicyAxis.ConscriptionLaw => StepEnumAxis((int)w.ConscriptionLaw, 2, v => { w.ConscriptionLaw = (ConscriptionLaw)v; p.ConscriptionLaw = w.ConscriptionLaw; }),
            PolicyAxis.SecretBallot => StepEnumAxis((int)w.SecretBallot, 1, v => { w.SecretBallot = (SecretBallotLaw)v; p.SecretBallot = w.SecretBallot; }),
            PolicyAxis.Bureaucracy => StepEnumAxis((int)w.Bureaucracy, 2, v => { w.Bureaucracy = (BureaucracyLaw)v; p.Bureaucracy = w.Bureaucracy; }),
            PolicyAxis.ChildLabor => Flip(w.ChildLaborAllowed, v => { w.ChildLaborAllowed = v; p.ChildLaborAllowed = v; }),   // 児童労働法（02断線走査2026-07-28）
            PolicyAxis.StateReligion => StepEnumAxis((int)w.StateReligion, 2, v => { w.StateReligion = (StateReligionLaw)v; p.StateReligion = w.StateReligion; }),   // 上限=信仰の自由（国家無神論は政体の属性＝要求で到達しない）
            PolicyAxis.Antitrust => StepEnumAxis((int)w.Antitrust, 2, v => { w.Antitrust = (AntitrustLaw)v; p.Antitrust = w.Antitrust; }),
            PolicyAxis.Guild => w.Coef.GuildLawGate > 0 ? StepEnumAxis((int)w.Guild, 2, v => { w.Guild = (GuildLaw)v; p.Guild = w.Guild; }) : 0,   // ★特権 段4（ゲート0では要求経路も動かさない）
            PolicyAxis.MinimumWage => w.Coef.MinimumWageLawGate > 0 ? StepEnumAxis((int)w.MinimumWage, 3, v => { w.MinimumWage = (MinimumWageLaw)v; p.MinimumWage = w.MinimumWage; }) : 0,   // ★最低賃金法（線 lw (459)・ゲート0では要求経路も動かさない）
            PolicyAxis.WartimeRationing => Flip(w.WartimeRationing, v => { w.WartimeRationing = v; p.WartimeRationing = v; }),
            PolicyAxis.MilitaryExport => Flip(w.MilitaryExportBan, v => { w.MilitaryExportBan = v; p.MilitaryExportBan = v; }),   // 軍需輸出の法（禁制品doc C5・2026-07-30）
            PolicyAxis.CivilianArms => c.RebelArmsMarketEnabled > 0 ? Flip(w.CivilianArmsBan, v => { w.CivilianArmsBan = v; p.CivilianArmsBan = v; }) : 0,   // ★民間の武器購入の法（2026-09-11・ゲート0では要求経路も動かさない）
            PolicyAxis.OpiumPossession => StepEnumAxis((int)w.OpiumPossession, 2, v => { w.OpiumPossession = (ReformLevel)v; p.OpiumPossession = w.OpiumPossession; }),   // 阿片の保有法（C3・2026-07-30）
            PolicyAxis.OpiumExport => Flip(w.OpiumExportBan, v => { w.OpiumExportBan = v; p.OpiumExportBan = v; }),   // 阿片の輸出法（C3・2026-07-30）
            // 国有化＝未点灯の最初の1部門を灯す（部門順は enum 順＝決定論）。減少向き＝点灯中の最後の1部門を消す。
            PolicyAxis.Nationalization => StepNationalization(w, p, inc),
            // ★是正2026-08-01：この2軸は選好（PolicyPreferences）が点火済みで要求リストに載るのに、
            // ここに case が無く `_ => 0` へ落ちていた＝**要求は成立するが施行されない**＝勢力が永久に同じ要求を出し続ける。
            PolicyAxis.ImmigrationRestriction => StepEnumAxis((int)w.ImmigrationRestriction, 2, v => { w.ImmigrationRestriction = (ImmigrationLaw)v; p.ImmigrationRestriction = w.ImmigrationRestriction; }),
            PolicyAxis.SpecialistExitBan => StepEnumAxis((int)w.SpecialistExitLaw, 2, v => { w.SpecialistExitLaw = (SpecialistExitLaw)v; p.SpecialistExitLaw = w.SpecialistExitLaw; }),   // ★段⑤
            // ★3値化2026-08-13（点火1.0=2026-08-21銀本位ラウンド・旧の旗 SilverStandardGate は 2026-10-18 に焼き込み）：銀を経由する梯子では1段ずつ譲る
            PolicyAxis.MonetaryStandard => StepEnumAxis((int)w.MonetaryLaw, 2, v => { w.MonetaryLaw = (MonetaryStandardLaw)v; p.MonetaryTarget = v; }),
            PolicyAxis.CapitalMobility => Flip(w.CapitalControlLaw, v => { w.CapitalControlLaw = v; p.CapitalControlTarget = v; }),   // ★資本移動の法（トリレンマdoc・2026-08-13）
            // 参政権の範囲2法（2026-08-16）：休眠中は Pref=0＝要求に載らず不到達。点火時に case が無いと「要求は成立するが施行されない」no-op要求（2026-08-01の前例）になるので先に配線。
            PolicyAxis.ColonialSuffrage => StepEnumAxis((int)w.ColonialSuffrage, 2, v => { w.ColonialSuffrage = (ColonialSuffrageLaw)v; p.ColonialSuffrage = w.ColonialSuffrage; }),
            PolicyAxis.MigrantNativeSuffrage => StepEnumAxis((int)w.MigrantNativeSuffrage, 1, v => { w.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)v; p.MigrantNativeSuffrage = w.MigrantNativeSuffrage; }),
            // ★海軍維持費（2026-08-10 作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）。
            //   「海軍の増強」は抽象的な注力度でなく**金の要求**＝海軍維持費（実効値と目標）を上げる。旧・海軍偏重の軸はここへ振り替えた。
            //   ⚠AIもプレイヤーも同じ経路（勢力への譲歩／財政タブの陸軍維持費・海軍維持費）を通る＝対称（議会の綱領は 2026-09-26 に撤去）。
            PolicyAxis.NavyUpkeep => ConcedeNavyUpkeep(w, p, inc, c.FactionConcessionSlider * n),
            _ => 0,   // 行政費等の内部事項は要求として強制しない
        };
    }

    // ★海軍維持費の要求施行（2026-08-10）＝実効値と目標を同時に書く（書かないと Government が翌期戻す＝他のスライダー軸と同作法）。
    // 合計（陸＋海）が1.0を超える分だけ陸軍から出す＝上限に張り付いた時だけ内訳が動き、普段は海軍の純増（＝軍拡）。動いたら1を返す。
    private static int ConcedeNavyUpkeep(World w, NationPolicy p, bool inc, double step)
    {
        double navy = PoliticalQuantities.Clamp01(w.NavyUpkeep.Value + (inc ? step : -step));
        if (Math.Abs(navy - w.NavyUpkeep.Value) <= 1e-9) return 0;
        w.SetMilitaryUpkeep(w.ArmyUpkeep, new Share(navy));
        p.NavyUpkeepTarget = navy;
        p.ArmyUpkeepTarget = Math.Min(p.ArmyUpkeepTarget, 1.0 - navy);
        return 1;
    }

    // 02断線走査（2026-07-28）：国有化要求の施行＝enum順で未点灯の最初の部門を灯す／減少向きは点灯中の最後を消す（決定論）。動いた部門数を返す。
    private static int StepNationalization(World w, NationPolicy p, bool inc)
    {
        var flags = new (bool Cur, System.Action<bool> Set)[]
        {
            (w.NationalizeRailways, v => { w.NationalizeRailways = v; p.NationalizeRailways = v; }),
            (w.NationalizeMines, v => { w.NationalizeMines = v; p.NationalizeMines = v; }),
            (w.NationalizeMilitary, v => { w.NationalizeMilitary = v; p.NationalizeMilitary = v; }),
            (w.NationalizeOil, v => { w.NationalizeOil = v; p.NationalizeOil = v; }),
            (w.NationalizeElectricity, v => { w.NationalizeElectricity = v; p.NationalizeElectricity = v; }),
        };
        if (inc)
        {
            foreach (var f in flags) if (!f.Cur) { f.Set(true); return 1; }
            // 最終段『全て』（§11-6d）＝残り全産業の一括国有化。譲歩でも前提DAG（プロレタリア独裁のみ＋官僚制度）を踏む＝LawPrereq が単一ソース。
            if (!w.NationalizeAll && Government.LawPrereq(w, PolicyAxis.NationalizationAll, 1, w.Coef).Ok)
            { w.NationalizeAll = true; p.NationalizeAll = true; return 1; }
            return 0;
        }
        if (w.NationalizeAll) { w.NationalizeAll = false; p.NationalizeAll = false; return 1; }
        for (int i = flags.Length - 1; i >= 0; i--) if (flags[i].Cur) { flags[i].Set(false); return 1; }
        return 0;
    }

    // F1 州一揆（王権議会F1・2026-07-10）：飢えた州の大衆蜂起（パンの一揆＝1789地方蜂起・シレジア織工・チャーティスト）。
    // 発火＝州の熱（★段2＝州平均闘争度そのもの。飢えは闘争度の中の飢餓の駆動項が運ぶ）>閾値（★2026-09-29 共有クールダウンを撤去＝熱が門の上にある間は毎四半期燃える・蜂起を予約した州は走査から外す）。政府の裁定は決定論2分岐＝
    // **鎮圧**（州の蜂起勢力≤中央保持力＝戦死（人口台帳）・闘争度は部分緩和・正統性ストックに流血の傷＝ペテルルー）／
    // **譲歩**（勝てない＝大衆要求の優先梯子〔穀物法撤廃→労働時間短縮→年金→農奴解放→参政権〕を1段のみ、政治資本を燃やす＝負のマンデート。
    //   法の**目標**として置くだけ＝施行と代金は Government.Step の通常経路・ConcedeToRevolt 参照）。
    // 3択目の宥和（福祉・減税）は既存のAI/プレイヤー動詞が常設＝ここでは扱わない。RegionalRevoltThreshold=0 で無効（★段2で現行0.50＝稼働）。決定論＝RNGなし。
    private static void CheckRegionalRevolt(World w, Coefficients c, double coerce)
    {
        // ★F1 譲歩は法の経路：前回の譲歩で置いた要求の法が動いたかの判定と緩和は **Step の先頭**（施行の翌四半期の政治より前＝分離・F3b・内戦の判定より先に鎮める）。
        // ★段6（2026-09-29 点火）：共有のクールダウン（4四半期の固定タイマー）は撤去＝待ちで数えない。
        if (c.RegionalRevoltThreshold <= 0) return;
        var (worst, worstHeat, crowd) = SelectRevoltRegion(w, c);   // ★項目3（2026-09-11）：選択は計器（factionprobe 段0）と共有の1本（鍵＝熱×群衆）
        if (worst is null || worstHeat <= c.RegionalRevoltThreshold) return;
        double strength = worstHeat * crowd * c.RegionalRevoltStrength;   // 蜂起の勢い＝熱×州大衆の規模（challenge/coerce と同じ人口スケールの物差し）
        // ★B9（実バグ21件・2026-09-07・2026-09-11点火）：蜂起力は州の人口スケール・coerce は全国スケール＝大国では譲歩枝が構造的に到達不能（州の数が政治結果を決めていた）
        //   ＝F3b と同じ LocalizedHold（州按分）を読む。
        coerce = LocalizedHold(w, c, worst, coerce);
        if (strength <= coerce)
        {
            // 鎮圧：州の平民の民間人に規模按分で死＝死の記帳の1本（World.KillPops・段4e J＝人口の台帳と戦死の累計）・貯蓄は遺族に残る＝貨幣保存非接触。
            double dead = Math.Min(crowd * c.RevoltSuppressDeadFrac.Value, crowd);
            if (dead > 1e-9 && crowd > 1e-9)
                dead = w.KillPops(worst.Pops.FindAll(p => p.IsCommoner), null, dead, violent: true);
            double legBeforeRevolt = w.Legitimacy;
            w.Legitimacy = Math.Max(0, w.Legitimacy - c.RevoltSuppressLegitCost);   // 流血の記憶＝ストック正統性への直接の傷（regenで数年かけ回復）
            w.Explain?.Add("Legitimacy", "一揆鎮圧の流血(ストック正統性への傷)", w.Legitimacy - legBeforeRevolt);   // 説明API（#63）
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{worst.Name}の一揆を鎮圧（死者{dead:F0}）");   // 3桁区切りは全廃（作者裁定2026-08-10・外交ログはUIの出来事へそのまま出る）
        }
        else
        {
            ConcedeToRevolt(w, c, worst);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{worst.Name}の一揆に譲歩");
        }
        // 闘争度の緩和はどちらの枝でも掛けない（作者裁定2026-09-09「弾圧は鎮めない・譲歩が鎮める」・2026-09-11点火）：鎮圧は首謀者を除いても州は鎮まらない
        //   （残るのは死者と正統性の傷＝ペテルルー）／譲歩が鎮めるのは施行の時点（Step 先頭の記録下ろし＝置いただけの約束は鎮めない）。
    }

    // 譲歩＝大衆の窮乏に直結する順の優先梯子を1段（パンの一揆→穀物法撤廃が先頭＝1846年）。梯子→軸の対応は下の選択1本。
    // ★F1 譲歩は法の経路（作者裁定2026-09-11・同日点火＝F3b の危機の要求と**同じ目標機構**）：次の段を**法の目標**として置く
    //   ＝Parliament.TargetDemand（TargetCrisisDemands が使う同じ1本＝AI の Apply／初心者経路／法律タブと同じ p.X・同じ Government.GateInfo＝通らない段は置かれない＝拒否）。
    //   現行法は1行も書かない＝施行は Government.Step の通常経路（純支持＞−余白・前提DAG・不可逆・憲法級）＝滞りうるし通らないこともある。
    //   政治資本の請求も置かない＝通常の法改正は施行側（Government.MandateGate）が反対を押し切った分だけ Spend する＝第二の課金を作らない。
    //   記録（World.PendingRevoltDemand）＝AI の Apply はスタンス既定で法目標を毎期再代入する（SelfPlay.Apply の p.WorkHours=Twelve／SocialSecurityTarget／CornLaws の StepLawToward）ので、
    //   F3b と同じく Apply の末尾（Parliament.TargetCrisisDemands）で置き直す＝要求が最後の言葉。法が要求の向きへ動いたら Politics.Step 先頭が下ろす。
    //   闘争度の緩和（譲歩が鎮める・作者裁定2026-09-09）は法が動いた時点＝Politics.Step 先頭が記録した州（PendingRevoltRegion）へ掛ける＝置いただけの約束は鎮めない。
    //   旧梯子（現行値と目標の直書き＋固定代金 RevoltConcessionSpend＝政治資本・反対・法の天井を迂回する蜂起専用の改正ルート）は物理削除。
    private static void ConcedeToRevolt(World w, Coefficients c, Region worst)
    {
        PolicyAxis axis; bool inc;
        if (w.CornLaws > ReformLevel.None) { axis = PolicyAxis.CornLaws; inc = false; }
        else if (w.WorkHours > WorkHours.Eight) { axis = PolicyAxis.ShorterHours; inc = false; }
        else if (w.SocialSecurityLaw < SocialSecurityLaw.Pension) { axis = PolicyAxis.SocialSecurity; inc = true; }
        else if (w.SerfEmancipation < ReformLevel.Full) { axis = PolicyAxis.SerfEmancipation; inc = true; }
        else if (w.Suffrage < Suffrage.AdultMale) { axis = PolicyAxis.Suffrage; inc = true; }
        else return;   // のむものが残っていない＝置くものも施行も無い＝緩和も無い
        w.PendingRevoltDemand = inc ? (int)axis + 1 : -((int)axis + 1);
        w.PendingRevoltDemandPos = FactionSnapshot.AxisPosition(w, axis);
        w.PendingRevoltRegion = worst.Id;
        Parliament.TargetDemand(w, axis, inc);
    }

    // 分離独立（§9-E拡張）：州の関税利害が国政策と乖離＋州の不満が高いと分離圧力。中央の保持力を超える州があれば離脱→内戦（再統合戦）。稀に発生。
    // internal＝番人（RevoltIgnitionTests の「離脱の力は人の数で測る」）が四半期の他の流れを挟まずにこの1段だけを踏む（SpawnSecededNations と同じ作法）。
    internal static void CheckSecession(World w, Coefficients c, double coerce)
    {
        if (w.Regions.Count < 2) return;
        double stance = w.Tariff * 2 - 1;   // 国の関税姿勢（−1自由貿易..+1保護）
        double hold = w.Legitimacy / (1 + w.TotalPopulation * c.SecessionHoldPerPop);   // 中央の保持力（大国ほど辺境を抑えにくい）
        // 政5：中央保持力に軍・警察(Coercion)由来の項を加える。現行の hold は Legitimacy/(1+…)≤1 で、閾値 SecessionThreshold=1.43>1 のため第2条件 worstPressure>hold が常に真＝デッドコード
        // ＋大軍の帝国でも分離率が変わらない（Coercion 不在）。SecessionHoldCoercion=0 では加算項ゼロ＝従来の hold 式のまま。>0（現行1.0）で軍事治安維持力を [0,閾値) 帯へ飽和写像して hold に足し、
        // 第2条件を活性化（大軍の帝国ほど分離を抑える／治安維持力の弱い国は圧力次第で割れる）。SecessionThreshold と桁を揃えるため飽和上限は SecessionThreshold 基準で正規化。
        hold += c.SecessionHoldCoercion * c.SecessionThreshold * (coerce / (coerce + c.PoliticalReferencePop));   // coerce/(coerce+基準人口)∈[0,1)＝軍事保持力の飽和・0で無加算
        var cap = w.Regions[0];   // #13 中核（首都）の文化/宗教＝国民的アイデンティティの基準
        Region? worst = null; double worstPressure = 0;
        double worstMargin = double.NegativeInfinity, worstEffHold = hold;   // ★E-05段1：投射で割った保持力と、その差（0で未使用＝恒等）
        int restive = 0;   // ★E-05段2：閾値を越えている運動の数（＝同じフォルダに何件入っているか）
        var margins = new List<(Region R, double Hold, double Pressure)>();   // ★E-06：州ごとの引き留め（投射済み）と離脱圧力＝ループ後に共有分母を掛けてから積分する
        foreach (var r in w.Regions)
        {
            if (r.Id == 0) continue;   // 首都は離脱しない
            r.TariffInterest = RegionTariffInterest(r);   // 倍率排除：州の関税利害を**実利（競争力）から毎期算出**（静的ラベルを廃止）
            // #13 文化/宗教の異質性：首都(中核)と文化・宗教が違う州ほど分離志向が強い（言語/信仰の少数派＝ナショナリズムの分離圧力）。同一/未設定なら0＝較正中立。
            double cultDist = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                            + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
            // ★E-05段5（2026-09-03・作者「文化がない？」の実測から）：⚠**昇格した植民地は宗主国の首都と同じ文化・同じ宗教を持つ**
            //   （`Simulation.PromoteColonyToRegion` が `Culture = w.Region.Culture, Religion = w.Region.Religion` と代入している）。
            //   ⇒ 文字列比較である上の cultDist は**インドとロンドンで 0**＝文化/宗教の分離圧力も、下の民族主因の加算圧力
            //  （cultDist に掛け算）も**植民地では恒等的に0**。⚠結果として**プロイセン領ポーランドの方がインドより離反圧力が高い**。
            //   ★民族の側は正しく区別されている（現地住民は CoreEthnicShare=0 で作られる）＝壊れているのは文字列だけ。
            //   ⇒ 文化距離を**民族の実体から導く**（1−州の平均中核度）へ線形に寄せる＝新しいデータ（world.json の文化表）を足さない。
            //   ⚠副作用に注意：下の `SecessionCultureAdditive` は既に (1−中核度) を掛けているので、寄せ切ると**2乗**になる
            //   （＝民族主因の圧力が非線形に強まる）。点火は 1.0 でなく中間値から掃く。0で厳密恒等（休眠）。
            if (c.CultureDistanceFromEthnic > 0)
                cultDist += c.CultureDistanceFromEthnic * ((1.0 - AvgRegionCoreShare(r)) - cultDist);
            double mil = AvgRegionMilitancy(r);
            double pressure = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cultDist);   // 関税乖離(0..2)×州不満×文化異質性
            // 政14：分離の駆動因が関税利害の乗算に一元化（関税乖離0なら文化がどれだけ異質でも圧力0）。19世紀の分離は民族・宗教が主因＝**独立の加算圧力**を用意（MinorityMilitancy と接続）。
            // SecessionCultureAdditive=0 では加算ゼロ＝乗算のみの旧式。>0（現行3.0）で「文化/宗教距離 × 州の少数派度(1−平均CoreEthnicShare) × 州不満」を関税と独立に足す
            // ＝関税乖離0の異民族州でも分離圧力が立つ（民族主因の分離）。EthnicEnabled の国のみ（少数派度は民族機構が有効な世界でのみ意味を持つ）。
            if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled)
                pressure += c.SecessionCultureAdditive * c.MinorityMilitancy * cultDist * (1.0 - AvgRegionCoreShare(r)) * mil;
            // ★N7 分離割引（国民意識doc・2026-08-13）：**国民意識が育った州は、条件が揃っても離れない。**
            //   国民国家の形成とは「離れない州を作ること」そのものだから、分離圧力にこの割引が無いのは機構の欠落だった
            //   （国民形成に投じた金が、分離という最も分かりやすい帰結に一切効いていなかった）。
            //   ⚠読むのは州のPOPの国民意識のsize加重平均だけ＝国名も文化名も出てこない。SecessionConsciousnessWeight=0＝掛けず旧式（現行1.0点火）。
            if (c.SecessionConsciousnessWeight > 0)
            {
                double nc = AvgRegionConsciousness(r);
                pressure *= Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * nc);
            }
            // 参政権の範囲2法（作者裁定2026-08-16）：植民地参政権で政治的国民に算入された植民地格の州は
            // 分離圧力が算入率ぶん緩む＝×(1−算入率)。算入率＝なし0／自国民のみ=州の平均中核度（AvgRegionCoreShare＝民族項と同じ言語）／全面1
            // ＝代表を得た州は離れない（N7 分離割引と同型の乗算・新しい定数なし）。
            // ★E-05段3（2026-09-03）：⚠旧はここで `Tier == Colonial` を見ていた＝**同じ問い（代表を得ているか）に法が2本**。
            //   新＝格を見ず、上の少数派の地位不満と**同じ算入率**を使う＝規則は1本（作者裁定「法側で制御したい」）。
            //   ⚠本国州も同じ式を通るが、中核度≈1＝算入率≈1＝圧力≈0への割引＝**従来ここに来ていなかった州の挙動は実質変わらない**。
            //   （旗 SuffrageScopeUnified は 2026-09-26 に焼き込み＝格を読む旧の枝は削除）
            pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, r));
            // ★幽霊の国の根（2026-09-30 (118)・推奨で進めた）：**離脱の力は人の数で測る**（反乱を起こすのは人）。
            //   旧＝圧力は1人あたりの量（関税の乖離×州の平均闘争度×文化）だけで州の人口に依らず、引き留めは国の人口と治安維持力（人の数 coerce）で測る
            //   ＝分子と分母が別の単位。州の間の移民で大衆が数人まで抜けた州（小さな植民地の州）も人口3千の州と同じ圧力で離れ、
            //   人口100未満の「幽霊の国」を生んでいた（fff9f01 の10種＝分離の戦争へ入った州97のうち80が人口100未満・幽霊の国79のうち72がそこで生まれた＝国×四半期の9割）。
            //   新＝州の大衆 N（閉鎖階級と兵士を除く＝平均闘争度と同じ母集団・F1 の群衆と同じ1本 RegionCrowd）の飽和 N/(N+N0) を掛ける。
            //   N0＝人の集まりが1つの政治単位になれる人口＝植民地の州昇格と同じ数（ColonyPromotionMinPop＝北西部条例1787 の自由人口6万人）
            //   ＝州を名乗れる人口の土地は、国を名乗る力も半分まで持つ。数人の州は離れない・人口千以上の州はほぼ旧のまま。
            //   ⚠引き留めの治安維持力の項と同じ基準人口（PoliticalReferencePop＝100万人）は探索の腕で試して採らなかった＝5種で分離の戦争0件・
            //   人口10万〜100万の州の離脱も全て消えた＝史実の小さな独立（テキサス1836 約4万・ウルグアイ1828 約7.5万・コスタリカ1838 約8万・
            //   ドミニカ1844 約13万）が構造的に不能になる（99_実装ログ (118)）。
            //   ⚠闘争度は圧力の中に既にある＝人の数には闘争度を掛けない（二重計上しない）。新しい係数も状態も無い。大衆が NaN の州は圧力0（否定形＝離れない側へ倒す）。
            double crowdSec = RegionCrowd(r);
            pressure *= crowdSec > 0 ? crowdSec / (crowdSec + c.ColonyPromotionMinPop) : 0.0;
            // ★E-05 段1（作者裁定2026-09-03「あらゆる反乱は1つの経路で発生し、1つの方法で対処する。植民地反乱と国内反乱でルールが異なるのはダメ」）。
            //   ⚠**「植民地なら」という条件を1つも書かない**＝植民地が離れやすいのは格が違うからではなく**遠いから**である。
            //   ★保持力は「軍が届く分だけ」効く＝`Diplomacy.Projection` と**同じ形・同じ係数**（DistanceWarDecay / DistanceNormKm）を州へ当てる。
            //     隣接州は投射≈1で従来どおり／シベリアや西部辺境は中／インド・ジャワは小＝**国内の辺境にも同じ規則が効く**のが要点。
            //   ⚠距離は大圏（Geo.DistanceKm）を使う。SeaDistanceKm は内陸州で「大圏×2」へフォールバックし、
            //     **陸続きの州まで遠く見せる**（既知の罠）＝ここでは全州に同じ規則を当てたいので大圏で測る。
            //   ⚠座標未登録(0,0)の州は投射1.0＝中立（幻の遠距離を作らない＝AccumUpkeep と同じ守り）。
            //   ⚠SecessionHoldProjection=0 で下の else へ落ちる＝**厳密恒等**（選び方も従来の「圧力最大」のまま）。
            if (c.SecessionHoldProjection > 0)
            {
                double proj = 1.0;
                var rc = Places.Coord(r.Name);
                if (!(rc.Lat == 0 && rc.Lon == 0))
                {
                    double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
                    if (km > 0 && double.IsFinite(km)) proj = 1.0 / (1.0 + c.DistanceWarDecay * km / Diplomacy.DistanceNormKm);
                }
                double eff = hold * (1.0 + c.SecessionHoldProjection * (proj - 1.0));   // 適用率＝1で投射そのまま・0で従来の hold
                double margin = pressure - eff;
                if (worst is null || margin > worstMargin) { worstMargin = margin; worstPressure = pressure; worstEffHold = eff; worst = r; }
                margins.Add((r, eff, pressure));
            }
            else
            {
                if (pressure > worstPressure) { worstPressure = pressure; worst = r; }
                margins.Add((r, hold, pressure));
            }
            if (pressure > c.SecessionThreshold) restive++;   // ★E-05段2：閾値を越えている州の数＝同時に相手をしている運動の数（分母は下）
        }
        // ★E-05 段2（作者裁定2026-09-03「同じフォルダに入る→反乱鎮圧コストが合計になる」）。
        //   ⚠**実体は統合しない**＝運動はそれぞれ残り、**国家の側の分母**だけが割れる（＝これが「フォルダ」の実装）。
        //   ★形は既存の 旧 `ColonialHoldSharedArmy`（2026-10-18 に削除・同時多発の按分＝分母 `1+k×(不穏数−1)`）をそのまま一般化したもの＝新しい写像を作らない。
        //   ⚠数える対象に**格を見ない**＝植民地格の州も本国の州も、そして組織運動の蜂起（RevoltingFaction）も同じ1件として数える
        //     ＝「植民地反乱と国内反乱でルールが異なるのはダメ」を式の上で満たす。
        //   ⚠SecessionHoldSharedArmy=0 で分母1＝厳密恒等（休眠だった＝2026-09-05 B群で点火・0は従来の腕）。
        if (c.SecessionHoldSharedArmy > 0)
        {
            // ★段3＝亡霊の残り(ii)（F3b蜂起doc・2026-09-08・係数なし）：反乱国を**実数で**数える。
            //   旧＝`RevoltingFaction is not null` の1枠だけ＝N個の反乱国が同時に立っていても分母は1（建国済みの州は
            //   w.Regions を離れているので上の走査でも数えられない）＝分子と分母の非対称。新＝この国を CivilWarParent とする War の本数。
            //   ⚠戦争の無い単国の世界では本数0なので RevoltingFaction の1枠へ落ちる（2026-09-28 から単国の蜂起は即時の決着＝参照は立たない）。
            //   ⚠その1枠は**親がまだ本拠州を持っているとき**だけ数える（段0と同じ述語）＝建国後に置き去りにされた主体の亡霊で分母を永久に膨らませない。
            int civilFronts = 0;
            if (w.Diplomacy is { } dipCw && w.SelfPower is { } spCw)
                foreach (var war in dipCw.Wars) if (war.CivilWarParent == spCw) civilFronts++;
            if (civilFronts == 0 && w.RevoltingFaction is { } rfCw && w.Regions.Contains(rfCw.Region)) civilFronts = 1;
            restive += civilFronts;   // 組織運動の蜂起も同じフォルダの件数（土地でなく運動だが、軍を割る側は同じ）
            double sharedDiv = 1.0 + c.SecessionHoldSharedArmy * System.Math.Max(0, restive - 1);
            hold /= sharedDiv; worstEffHold /= sharedDiv;
        }
        // ★E-06 支配度＝ストックの積分（作者裁定2026-09-05・設計 20260903_支配度はストックにする_再裁定 §4）。
        //   ②支配度 += (引き留め − 離脱圧力) × 速度。引き留め＝この州に届く保持力（E-05段1の投射・段2の共有分母を掛けた後）＝判定と同じ数。
        //   ⚠書き手はここ1箇所。格（Tier）は読まない＝本国の辺境州も同じ式で減る・増える。積分は常に走り、読むのは下の判定だけ。
        //   ⚠圧力が保持力を下回る限り支配度は回復する（上限1）＝「戦争で軍が減っても全植民地が同時に独立しない」のはストックの慣性が担う。
        double sharedDivDom = c.SecessionHoldSharedArmy > 0 ? 1.0 + c.SecessionHoldSharedArmy * System.Math.Max(0, restive - 1) : 1.0;
        foreach (var (r2, h2, p2) in margins)
        {
            double d = r2.Dominance + (h2 / sharedDivDom - p2) * c.DominanceSpeed;
            if (!(d > 0)) d = 0; else if (d > 1) d = 1;
            r2.Dominance = d;
        }
        if (Instruments.On) Instruments.SecObserve(w, c, coerce, stance, hold, sharedDivDom, restive, margins);   // ★(395) 計器（読むだけ）：州ごとの離脱圧力・保持力・支配度の内訳の表
        // ★支配度の一般化（段1〜5・(399) で点火）：州の4区分（国家・在地・外国・無統治）を目標へ寄せる。速度は DominanceSpeed（新しい速度定数を作らない）。合計1を保つ。
        //   目標：無統治＝(1−治安)×(1−到達)（到達＝首都からの距離〔RegionReach〕・辺境ほど・治安が抜けるほど）／在地＝開幕のデータ×仲介者の権利の残高（官僚制度法と土地改革の max〔Privileges.IntermediaryDegree〕・
        //   農奴解放の法は読まない〔作者 2026-10-16：農奴解放は農奴POPを農民POPにする法で州の支配度には影響しない〕）／外国＝講和で積まれた持分の合計（ForeignShareSum）／国家＝残り。
        {
            double order = PoliticalQuantities.Clamp01(w.PublicOrder);
            double intermediary = Privileges.IntermediaryDegree(w);
            foreach (var r4 in w.Regions)
            {
                if (r4.Rule.Length != 4) continue;
                double tUngov = (1.0 - order) * (1.0 - RegionReach(w, c, r4));
                double tLocal = System.Math.Min(1.0 - tUngov, (r4.RuleInit.Length == 4 ? r4.RuleInit[1] : 0) * intermediary);
                double tForeign = ForeignShareSum(r4);
                double tState = System.Math.Max(0, 1.0 - tLocal - tForeign - tUngov);
                double[] t = [tState, tLocal, tForeign, tUngov];
                double sum = 0;
                for (int k = 0; k < 4; k++) { double v = r4.Rule[k] + (t[k] - r4.Rule[k]) * c.DominanceSpeed; if (!(v > 0)) v = 0; r4.Rule[k] = v; sum += v; }
                if (sum > 1e-9) for (int k = 0; k < 4; k++) r4.Rule[k] /= sum; else r4.Rule[0] = 1.0;
                // ★支配度 §7：国家区分の年次リング（10年前の帯＝州パネル）。年に1回。
                if (r4.RuleStateLastYear != w.Year && r4.RuleStateYears.Length > 0)
                {
                    r4.RuleStateYears[r4.RuleStateHead % r4.RuleStateYears.Length] = r4.Rule[0];
                    r4.RuleStateHead = (r4.RuleStateHead + 1) % r4.RuleStateYears.Length;
                    r4.RuleStateLastYear = w.Year;
                }
            }
        }
        w.SecessionHold = c.SecessionHoldProjection > 0 ? worstEffHold : hold;   // ★E-05段1：UIが見る保持力も「その州に届く分」＝表示と判定を同じ数にする
        // ★同時独立（SecessionAsWar・作者裁定2026-09-06）：クールダウン（内戦4年・国につき1つ）を撤去。支配度が尽きた州（旧判定＝圧力最大の州）は
        //   その四半期に内戦一元化の経路へ＝複数州が同時に独立の戦争へ入る（旗 SecessionAsWar は 2026-09-26 に焼き込み）。
        // ★旧経路の単一州の分離（単国の世界・CivilWarYears の4年の内戦→州を Locked＋Seceded で凍結）は削除（2026-09-28 処理の一元化 段2 束3・作者裁定 g）
        //   ＝分離独立は戦争の形の1本（係数 SecessionSpawnsNation も焼き込み）。単国の世界（建国の回収者がいない）は支配度を積分するだけで分離しない。
        if (w.StepsDiplomacy || w.SelfPower is null) return;
        List<Region>? go = null;
        foreach (var (r3, _, _) in margins) if (!(r3.Dominance > 0)) (go ??= []).Add(r3);
        if (go is not null)
        {
            (w.PendingSecessionWars ??= []).AddRange(go);
            w.HadSecession = true;
            foreach (var r3 in go) w.LogAdd($"{w.Year} {r3.Name}が{w.SelfPower?.Name}からの独立を求めて立った（分離独立戦争）");
        }
        // （旧判定＝圧力の瞬間値が最大の州が保持力を超えたら分離＝旗 DominanceIsStock=0 の枝は 2026-09-26 に削除）
    }

    /// <summary>州の関税利害を**実利から創発**（倍率排除：静的な資源ラベル写像を撤去）。
    /// 各生産財の国内価格を世界参照価格(RefPrice)と比べ、安く作れる州＝競争力＝輸出で潤う＝**自由貿易が得(−)**、
    /// 高い州＝高コスト/品薄＝輸入に脅かされる＝**保護が得(+)**。生産額で加重。
    /// ＝競争力ある工業輸出都市（マンチェスター）は自由貿易へ寄り、自由貿易国策との乖離が消えて分離が止む。幼稚産業州は保護へ寄る（リスト型）。</summary>
    private static double RegionTariffInterest(Region r)
    {
        double pull = 0, weight = 0;
        foreach (var good in r.Goods)
        {
            if (good.Id < 0 || good.Id >= r.Markets.Length || good.RefPrice <= 1e-9) continue;
            var m = r.Markets[good.Id];
            if (m.LastProduced <= 1e-9) continue;   // その州が実際に作っている財だけ＝その州の産業利害
            double competitiveness = Math.Clamp((m.Price - good.RefPrice) / good.RefPrice, -1.0, 1.0);   // >0 高コスト→保護・<0 競争力→自由貿易
            double wgt = m.LastProduced * good.RefPrice;   // 生産額で加重（大産業ほど州の関税姿勢を支配）
            pull += wgt * competitiveness; weight += wgt;
        }
        return weight > 1e-9 ? Math.Clamp(pull / weight, -1.0, 1.0) : 0.0;
    }

    /// <summary>州の平均闘争度（人口加重・閉鎖/兵士popを除く）。internal＝計器（factionprobe 段0）が
    /// heat の内訳を**同じ関数**で読むための読み取り専用の公開（式も呼び出し順も不変＝ビット恒等）。</summary>
    internal static double AvgRegionMilitancy(Region r)
    {
        var (mil, size) = RegionMilitancy(r);
        return size > 1e-9 ? mil / size : 0;
    }

    /// <summary>★州の闘争度の和＝平民の民間人（<see cref="Pop.IsCommoner"/>＝閉鎖階級と兵士を除く）の Σ闘争度×規模 と Σ規模（2026-09-30 処理の一元化 段4e H）。
    /// 読み手＝州の平均闘争度（一揆・分離・計器）・植民地の不満（維持費の不満の重み・反帝世論）・移民の押し出し（機会の不穏）。
    /// ⚠旧＝植民地の2か所と機会の押し出しは全ての POP（貴族・聖職・兵士込み）で手書きした＝駐留の兵と在地の貴族の闘争度が植民地の住民の不満と
    ///   州の不穏に混ざった（偶発のずれ）。</summary>
    internal static (double Mil, double Size) RegionMilitancy(Region r)
    {
        double mil = 0, size = 0;
        foreach (var p in r.Pops) if (p.IsCommoner) { mil += p.Militancy * p.Size; size += p.Size; }
        return (mil, size);
    }

    /// <summary>州の大衆＝閉鎖階級と兵士を除く pop の規模の和（F1 の群衆・蜂起の勢いの人口項）。旧は CheckRegionalRevolt のインラインループ（同じ述語・同じ順）。</summary>
    internal static double RegionCrowd(Region r)
    {
        double crowd = 0; foreach (var p in r.Pops) if (p.IsCommoner) crowd += p.Size;
        return crowd;
    }

    /// <summary>★項目3（2026-09-11・同日点火）：F1 州一揆の候補の選択＝CheckRegionalRevolt と計器（factionprobe 段0）が**同じ1本**で選ぶ。
    /// 鍵＝熱×州の大衆人口（蜂起の勢い strength と同じ物差し・RegionalRevoltStrength は定数なので順位は同じ）。門（RegionalRevoltThreshold）は選ばれた州の熱に掛ける
    /// ＝人の多い州が門を越えて初めて燃える（微小州は人の多い州に負けて燃えない）。走査順・狭義の比較・初期値0。
    /// 返り値＝選ばれた州・その熱・その大衆（州が無ければ0）。NaN の熱は鍵の比較で落ちる。</summary>
    internal static (Region? Region, double Heat, double Crowd) SelectRevoltRegion(World w, Coefficients c)
    {
        Region? worst = null; double worstHeat = 0, worstKey = 0, worstCrowd = 0;
        foreach (var r in w.Regions)
        {
            // ★段6（2026-09-29 点火）：タイマーの代わりの**物理的排他**＝同じ州で同じ四半期に組織運動の蜂起（F3b）と無組織の一揆（F1）を二重に起こさない。
            //   旧はクールダウンがこれを兼ねていた（F3bが立つと必ず4四半期ぶん塞がった）。
            if (w.PendingRebelRegions is { } pend && pend.Contains(r)) continue;
            double heat = RegionRevoltHeat(r, c);   // ★段2＝州平均闘争度そのもの（1量）
            double crowd = RegionCrowd(r);
            double key = heat * crowd;
            if (key > worstKey) { worstKey = key; worstHeat = heat; worstCrowd = crowd; worst = r; }
        }
        return (worst, worstHeat, worstCrowd);
    }

    /// <summary>★段2（設計メモ §7-3・作者裁定2026-09-09）：F1州一揆の熱＝**州平均闘争度そのもの**（1量）。
    /// ⚠旧は「州平均闘争度 × 欠乏因子(1−min(FoodFill, 州平均満足))」という2量の積だった＝欠乏が
    /// **闘争度の中（飢餓の駆動項）と熱の外側の両方**に入る二重計上（本企画で最も再発するバグ型）。
    /// 飢えは闘争度のストックが積分して運ぶ＝ここで再び掛けない。値域[0,1]は闘争度の値域そのもの。
    /// ⇒ 熱・暴動閾値(RiotMilitancyThreshold)・F1の門(RegionalRevoltThreshold)・闘争度の飽和が**同じ0.5の1本**になる。
    /// ⚠積が1量になったぶん熱は約2倍（段0実測の欠乏因子 p50 は5カ国で0.42〜0.56）＝<see cref="Coefficients.RegionalRevoltStrength"/> を同じ腕で再導出した。
    /// internal＝計器（factionprobe 段0）が同じ関数で読むための読み取り専用の公開（純関数・World非変更）。</summary>
    internal static double RegionRevoltHeat(Region r, Coefficients c) => AvgRegionMilitancy(r);

    /// <summary>★州の国民意識（N7・2026-08-13）＝POPの国民意識のsize加重平均。未捕捉(-1)のpopは平均から除く
    /// （国スカラーで代用すると州差が消える＝この機構の目的そのものが失われる）。1つも捕捉されていなければ0＝割引なし。</summary>
    /// <summary>州の国民意識＝兵士でなく捕捉済み（≧0）の POP の人口加重平均。<paramref name="none"/>＝1つも捕捉されていない州の値（分離の割引＝0・
    /// 徴兵の割引〔Military〕＝1＝機構の休眠＝割引なし）。★2026-09-30 処理の一元化 段4e H：旧＝徴兵の母数の割引が同じ和を手書きした。</summary>
    internal static double AvgRegionConsciousness(Region r, double none = 0)
    {
        double nc = 0, size = 0;
        foreach (var p in r.Pops) if (!p.IsSoldier && p.NationalConsciousness >= 0) { nc += p.NationalConsciousness * p.Size; size += p.Size; }
        return size > 1e-9 ? PoliticalQuantities.Clamp01(nc / size) : none;
    }

    // 政14：州の平均中核民族割合（人口加重・兵士除く）。(1−これ)＝州の少数派度＝民族主因の分離圧力の源（均質州=1で加算0）。
    /// <summary>★E-05段3（作者裁定2026-09-03「これは選挙権のない原住民などと同様に、法側で制御したい」／「5は不要な差異ではないか。外科的すぎる」）。
    /// **その州のPOPのうち政治的国民に数えられる割合**∈[0,1]。⚠格（Tier）を1度も見ない＝規則は1本。
    ///
    /// ⚠現行は**同じ問いに2つの法が別々に答えていた**：
    ///   ・植民地参政権 `ColonialSuffrageLaw {None, OwnNationals, Full}`（分離圧力の側で読む）
    ///   ・移民原住民参政権 `MigrantNativeSuffrageLaw {None, Granted}`（少数派の地位不満の側で読む）
    /// ★実体は同じ軸＝**非中核の住民を政治的国民に数えるか**。数えないなら算入率＝州の平均中核度、数えるなら1.0。
    ///   ⇒ 本国州は住民のほとんどが中核なので算入率≈1（従来どおり）／植民地は中核度≈0なので算入率≈0。
    ///   ★**植民地が違って見えるのは人口構成の帰結**であって、格の分岐ではない＝作者裁定どおり法側だけで決まる。
    /// ⚠残る1段（植民地参政権の「なし」＝入植者すら本国議会に代表を持たない）は**領域の属性**であって民族の話ではない
    ///   （英領インドはウェストミンスターに議席なし／仏領アルジェリアのコロンはパリに議席あり）。
    ///   ⇒ これを法で表すなら「編入するか植民地のままか」という別の決定が要る＝**J-009 の裁定待ち**。
    /// </summary>
    // ★(481) 旧名 SuffrageInclusion。InterestGroups.SuffrageInclusion(w, r, p)（POP ごと・植民地格の州は植民地参政権の法を読む）とは別の問い＝これは格を見ない州の平均の算入率。同名の 2 本を 1 本へ畳むのは挙動の線（分離圧力と少数派の地位不満の読みが動く・Q-sf2）。
    internal static double RegionAvgSuffrageInclusion(World w, Region r)
        => w.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? 1.0 : AvgRegionCoreShare(r);

    /// <summary>★(399) ② 州への国家の到達∈(0,1]＝首都（国の座標）からの大圏距離で減衰する＝<c>CheckSecession</c> の投射と同じ形・同じ係数（DistanceWarDecay・DistanceNormKm）。
    /// 座標が未登録（0,0）・距離が正でない・非有限の州は 1.0（幻の遠距離を作らない）。無統治の目標 (1−治安)×(1−到達) の読み手。</summary>
    internal static double RegionReach(World w, Coefficients c, Region r)
    {
        var rc = Places.Coord(r.Name);
        if (rc.Lat == 0 && rc.Lon == 0) return 1.0;
        double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
        if (!(km > 0) || !double.IsFinite(km)) return 1.0;
        double v = 1.0 / (1.0 + c.DistanceWarDecay * km / Diplomacy.DistanceNormKm);
        return !(v > 0 && v <= 1.0) ? 1.0 : v;
    }

    /// <summary>★(399) 法の浸透＝活きている州の人口で重みづけた <see cref="RuleReach"/> の平均（労働時間・女性の賃金保持・児童労働の追従の速さ）。人口が正の州が無ければ 1.0。行政効率は読まない（★(408) 州ごとの RuleReach に官僚の役務の充足 AdminFill を掛ける）。</summary>
    internal static double StateReach(World w, Coefficients c)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * (RuleReach(r) * PoliticalQuantities.Clamp01(r.AdminFill)); den += pop;   // ★(408) 国家区分の中でも官僚の役務が届く分にしか法は施行されない（RuleReach 自体には掛けない）
        }
        return den > 0 ? num / den : 1.0;
    }

    /// <summary>★(399) ③ 関税の目減り＝活きている港州の人口で重みづけた外国持分（合計・上限1）の平均。港州が無ければ 0。<c>TreatyPortTariffScale</c> は 1−これ。</summary>
    internal static double ForeignPortShare(World w)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (!r.IsPort) continue;
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * ForeignShareSum(r); den += pop;
        }
        return den > 0 ? num / den : 0.0;
    }

    /// <summary>★(399) ③ 条約港が効いているか＝港州の外国持分が付与の大きさ（ForeignRuleFromTreaty）の半分以上（屈辱の項・移民法の外圧の読み手）。
    /// 持分は国力比で連続に風化するので、付与の半分を割ると失効する。条約港は個数の機構でなく州の外国区分で表す（作者 2026-10-16：条約港は廃止・州の支配度で表現）。</summary>
    internal static bool TreatyPortInForce(World w, Coefficients c)
    {
        double line = 0.5 * c.ForeignRuleFromTreaty;
        if (!(line > 0)) return false;
        foreach (var r in w.Regions)
            if (r.IsPort && ForeignShareSum(r) >= line) return true;
        return false;
    }

    /// <summary>★支配度の一般化 段4（作者採用 §5・2026-09-07・(399) で点火）：公共財（教育の財源・年金の給付）と法の浸透の到達＝国家区分。
    /// 在地区分・外国区分・無統治には届かない（在地の劣化版＝特権の提供は段5以降）。Rule が無い州は1。</summary>
    internal static double RuleReach(Region r)
    {
        if (r.Rule.Length != 4) return 1.0;
        double v = PoliticalQuantities.Clamp01(r.Rule[0]);
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    /// <summary>★支配度 段5：州の外国持分の合計（Rule[2] の目標）。</summary>
    internal static double ForeignShareSum(Region r)
    {
        double v = 0; foreach (var x in r.ForeignShares) if (x > 0) v += x;
        return !(v >= 0) ? 0 : System.Math.Min(1.0, v);
    }
    /// <summary>★支配度 段5：主体（BaseName）の持分を足す（上限1）。無い主体は末尾に追加（並列配列＝セーブ形式）。</summary>
    internal static void AddForeignShare(Region r, string baseName, double amount)
    {
        if (!(amount > 0) || string.IsNullOrEmpty(baseName)) return;
        int k = System.Array.IndexOf(r.ForeignRulers, baseName);
        if (k < 0)
        {
            var names = new string[r.ForeignRulers.Length + 1]; var shares = new double[r.ForeignShares.Length + 1];
            System.Array.Copy(r.ForeignRulers, names, r.ForeignRulers.Length); System.Array.Copy(r.ForeignShares, shares, r.ForeignShares.Length);
            names[^1] = baseName; shares[^1] = 0; r.ForeignRulers = names; r.ForeignShares = shares; k = names.Length - 1;
        }
        r.ForeignShares[k] = System.Math.Min(1.0, r.ForeignShares[k] + amount);
    }

    /// <summary>★支配度の一般化 段3（作者採用 §5・2026-09-07・(399) で点火）：課税・徴兵の到達＝国家区分＋在地区分×届け率。
    /// 届け率＝1−PrivilegeTaxDiscount（在地の仲介者＝徴税請負/村請けの取り分は免税率と同じ既存の物差し＝新しい数を作らない）。無統治・外国区分からは取れない。Rule が無い州は1。
    /// ★(408) 国家区分のうち官僚の役務の充足（Region.AdminFill）が届かない分は在地の仲介者を通る＝R0×(d＋(1−d)×f)＋R1×d（Fiscal.TaxScan の仲介者の取り分が同じ分を領主へ移す）。</summary>
    internal static double RuleDelivery(Region r, Coefficients c)
    {
        if (r.Rule.Length != 4) return 1.0;
        double deliver = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value);
        // ★(408) 国家区分のうち官僚の役務が届く分 f は全額届き、届かない分 1−f は在地の仲介者を通る（届け率 d）。f＝1 で (399) の式（R0＋R1×d）と同じ値。NaN の AdminFill は 0（Clamp01）
        double v = PoliticalQuantities.Clamp01(r.Rule[0]) * (deliver + (1.0 - deliver) * PoliticalQuantities.Clamp01(r.AdminFill)) + PoliticalQuantities.Clamp01(r.Rule[1]) * deliver;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    internal static double AvgRegionCoreShare(Region r)   // ★B1：Colonial.Step の州昇格判定も読む
    {
        double core = 0, size = 0;
        foreach (var p in r.Pops) if (!p.IsSoldier) { core += p.CoreEthnicShare.Value * p.Size; size += p.Size; }
        return size > 1e-9 ? core / size : 1.0;   // pop なし＝1.0（少数派度0＝加算なし＝中立）
    }

    /// <summary>★(393) 兵が今の政体を支える割合 a∈[0,1]＝1−d。d＝兵士 POP だけの思想の集計（<see cref="Ideologies.Soldiers"/>）で思想ごとに〈引力点を現政体より好む分〉の正の部を質量で足し親和の範囲で割った離反の割合
    /// （<see cref="Ideologies.DefectionShare"/>・差だけを読む＝親和の零点に依存しない）。兵が現政体を好むか無関心なら a＝1（旧の保持力のまま）。兵士 POP が無い・重み 0 でも d＝0 で a＝1（縁も連続）。
    /// 現政体の座標は呼び手が渡す（参政権でドリフトした curCoord）・引力点は静的な基準座標。国民の失望の記録（RegimeTrackRecord）と bourgeoisCoup の上乗せは掛けない（兵の支持であって国民側の項ではない）。
    /// sCur／sAtt は集計での支持（計器の列・a の計算には使わない）。事前登録 measurements/mc_20261018_prereg.txt [11](2)。</summary>
    internal static double SoldierHoldShare(World w, Coefficients c, double[] curCoord, Polity attractor, out double sCur, out double sAtt, out double totalWeight)
    {
        var aggS = Ideologies.Soldiers(w, out totalWeight);
        var attCoord = Ideologies.BasePolityCoord(attractor);
        sCur = Ideologies.SupportAt(aggS, curCoord, c.IdeologyAffinitySlope);
        sAtt = Ideologies.SupportAt(aggS, attCoord, c.IdeologyAffinitySlope);
        return 1.0 - Ideologies.DefectionShare(aggS, curCoord, attCoord, c.IdeologyAffinitySlope);
    }

    /// <summary>第5段5-6（2026-07-28）革命→国民意識（ナポレオンの鎖①・中核ルールdoc §11）：暴力革命の成立は天命の媒介回路（聖職者・領主）を
    /// 破壊し国家→市民の直接回路へ置き換える（臣民→国民・1793 levée en masse）。学校天井（NC蓄積のlawCeil×識字）をバイパスする直接加算＝
    /// それが「直接回路」の意味。受け皿＝Military の徴兵深度上限（国民意識ゲート・既配線）→革命が国を軍事的に強くする経路が繋がる。
    /// 加算はCrownParliamentのNC台帳では翌期の「前期からの国民意識」に折り込まれる（Reset前の一回性インパルス）。RevolutionConsciousness=0で恒等。</summary>
    private static void RevolutionAwakensNation(World w, Coefficients c)
    {
        if (c.RevolutionConsciousness <= 0) return;
        // ⚠★是正2026-08-01（敵対レビューで確定）：国民意識のPOP化（NationConsciousnessPerPop>0）では、
        //   World.NationalConsciousness の setter が書く _ncScalar を getter が**一切読まない**ため、
        //   ここでスカラーへ足しても無音で捨てられていた＝**革命が国を軍事的に強くする経路が丸ごと死んでいた**。
        //   POP化のときは同じインパルスをPOPへ直接与える（未初期化POPには国別初期値を先に与えてから足す）。
        if (c.NationConsciousnessPerPop > 0)
        {
            double seed = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
            foreach (var p in w.AllPops())
            {
                double v = p.NationalConsciousness >= 0 ? p.NationalConsciousness : seed;
                p.NationalConsciousness = PoliticalQuantities.Clamp01(v + c.RevolutionConsciousness * (1.0 - v));
            }
            return;
        }
        if (w.NationalConsciousness < 0)
            w.NationalConsciousness = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
        w.NationalConsciousness = PoliticalQuantities.Clamp01(w.NationalConsciousness + c.RevolutionConsciousness * (1.0 - w.NationalConsciousness));
    }

    /// <summary>機能6-2（2026-07-28）：国内の革命圧力∈[0,1]＝動員（改革派＋急進派）÷実効保持力のクランプ比（Transitionと同じ物差し＝新概念なし）。
    /// 講和意欲の実効閾値（Diplomacy）が読む＝内戦の懸念がある国は余力を残して降伏（独1918/露1917）。</summary>
    public static double RevolutionPressure(World w)
        => PoliticalQuantities.ChallengeRatio(w.ReformistPower + w.RadicalPower, PoliticalQuantities.EffectiveCoerce(w, w.Coef));

    /// <summary>人口加重ジニ係数∈[0,1]（O(n log n)）＝**診断量**（ヘッダーの統計・SocialThemeTests）。★段2（2026-09-09）で闘争度の入力から外した
    /// ＝格差は want（1−満足／失望）と二重計上だった（設計メモ §2-1）。格差が広がれば下層の満足が落ちる＝同じ根を2度数えていた。
    /// 加重ロレンツ曲線を昇順に台形則で積分（x=累積人口シェア・y=累積富シェア）し gini=1−Σ(Δx)(y_i+y_{i-1})。全popが同じ pc なら曲線が対角線＝0。
    /// ★2026-09-26（棚卸し pol-37）：**読むときに計算する**（旧＝Politics.Step が全61カ国で毎四半期計算して World.GiniIndex に置いていた＝読み手は画面のヘッダー（プレイヤー国）とテストだけ）＝状態を1つ減らした。</summary>
    public static double GiniIndex(World w)
    {
        var wpcList = new List<(double pc, double size)>();
        foreach (var gp in w.AllPops()) { if (gp.Size <= 1e-9) continue; wpcList.Add((Math.Max(0, gp.Savings.Value) / gp.Size, gp.Size)); }
        double gini = 0;
        if (wpcList.Count > 1)
        {
            wpcList.Sort((a, b) => a.pc.CompareTo(b.pc));
            double totSize = 0, totWealth = 0; foreach (var e in wpcList) { totSize += e.size; totWealth += e.pc * e.size; }
            if (totSize > 1e-9 && totWealth > 1e-9)
            {
                double cumX = 0, cumY = 0, area = 0;   // area＝ロレンツ曲線下の面積（台形則）
                foreach (var e in wpcList)
                {
                    double prevX = cumX, prevY = cumY;
                    cumX += e.size / totSize; cumY += e.pc * e.size / totWealth;
                    area += (cumX - prevX) * (cumY + prevY);   // 台形＝Δx×(y_i+y_{i-1})/2 の /2 は最後にまとめて処理せず gini 式に吸収
                }
                gini = 1.0 - area;   // area は既に (y_i+y_{i-1}) 形＝2×(曲線下面積)。ジニ=1−2×面積 と一致
            }
        }
        return PoliticalQuantities.Clamp01(gini);
    }

    // 大衆（労働者・農民。兵士除く）の母集団＝**この1本だけ**（闘争度と不満度は二量分離の対＝分母の式を二度書かない）。
    // ★2026-09-25：満足度（NationalScore.MeanSatisfaction）も同じ母集団へ揃えた＝同じアセンブリから呼ぶため private→internal（述語は不変）。
    // ★2026-09-26（棚卸し sweep-13）：同じ述語の手書きの写し（植民地の本国圧力・組合の厚生・役務の順位・債務不履行の打撃・特権の俸給・初期の闘争度・大衆の所得）もこの1本を呼ぶ。
    internal static bool IsMassPop(Pop p) => p.IsCommoner && !p.IsModernElite;   // 大衆＝平民の民間人（Pop.IsCommoner）から近代エリートを除く

    // 大衆（労働者・農民。兵士除く）の平均闘争度（人口に依らない水準）。高ければブルジョワが独裁化（ブルジョア独裁の引き金）
    public static double AvgMassMilitancy(World w)   // 戦争熱＝外征の出口にも使う
    {
        double mil = 0, size = 0;
        foreach (var p in w.AllPops())
            if (IsMassPop(p)) { mil += p.Militancy * p.Size; size += p.Size; }
        return !(size > 1e-9) ? 0 : mil / size;
    }

    /// <summary>★大衆の平均不満度∈[0,1]（2026-09-23）＝<see cref="AvgMassMilitancy"/> と**完全に同じ母集団・同じ人口加重**（二量分離の対）。
    /// 中身は <see cref="Discontent"/> を per-pop に呼ぶだけ＝不満度の定義は1箇所（ここに第二の定義を書かない）。
    /// ⚠この関数（大衆の平均）の**エンジンの読み手はゼロ**＝ガラス箱（画面に出すだけ・★(436) 注：不満度そのものは参加の述語と思想の入力が POP ごとに読む）。参加の述語側は ParticipationDiscontent＝1（既定）で、思想の更新は常に、不満度を読む（2026-10-19 (431)）。
    /// ⚠純関数＝World に1ビットも書かない（<see cref="AvgMassMilitancy"/> と同じ作法）。
    /// ⚠政体座標と法の隔たりは**国の量**＝pop ループの外で1回だけ取る（<see cref="Discontent"/> の注意書き・キャッシュは作らない＝World に置いた導出量は記憶である）。
    /// ⚠分母が痩せたときのガードは否定形＝NaN も 0 へ（Math.Clamp は NaN を通す）。</summary>
    public static double AvgMassDiscontent(World w)
    {
        var c = w.Coef;
        double[] coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        double[] gap = c.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
        double dis = 0, size = 0;
        foreach (var p in w.AllPops())
            if (IsMassPop(p)) { dis += Discontent(w, c, p, coord, gap.Length > 0 ? gap[PolicyPreferences.BlocOf(p)] : 0) * p.Size; size += p.Size; }
        return !(size > 1e-9) ? 0 : dis / size;
    }

    // 政変後：勝者・中立の不満は緩和、失脚した集団のPOPは反革命の激しい不満を抱く（国内が不安定化＝仏革命の再現）。
    private static void ResolveRevolt(World w, Coefficients c, bool depTrad, bool depModern, bool depMass)
    {
        // 恐怖政治（タスクB/R5）＝一過性イベント：**急進革命(共和/プロレタリア独裁/アナーキー)の勝利時のみ**、失脚した旧支配層を
        // 物理的に粛清・亡命でSize削減＋資産没収する（仏革命の断頭台＝貴族が最も死んだ／ロシア革命の白系亡命）。緩やかな立憲化(立憲君主制)等の
        // 穏健革命は対象外＝過剰適用を避ける。RevolutionTerror=0 で terror=false＝不発（現行0.25採用）。
        bool terror = c.RevolutionTerror > 0
                   && w.Polity is Polity.Republic or Polity.ProletarianDictatorship or Polity.Anarchy;
        double confiscated = 0; MoneyStock confiscatedBonds = default;
        List<Pop>? seizedPops = null; List<double>? seizedAmt = null;   // 没収の払い手と自分の請求（下で移し方の1本へ・2026-09-30 段4g）
        List<Pop>? purgedPops = null; double purgeTotal = 0;   // 粛清・亡命の死は死の記帳の1本へ（段4e J）
        foreach (var p in w.AllPops())
        {
            bool deposed = (depTrad && p.Closed)
                        || (depModern && p.IsModernElite)
                        || (depMass && IsMassPop(p));
            if (deposed)
            {
                if (terror && !p.IsSoldier && p.Size > 0)
                {
                    (purgedPops ??= []).Add(p); purgeTotal += p.Size * c.RevolutionTerror;   // 粛清・亡命＝失脚した層の RevolutionTerror の割合（下で死の記帳の1本へ）
                    double seized = Math.Max(0, p.Savings.Value) * c.RevolutionTerror;   // 資産没収（貴族の所領・資本の没収）＝自分の請求（下で移し方の1本が取る）
                    if (seized > 0) { (seizedPops ??= []).Add(p); (seizedAmt ??= []).Add(seized); confiscated += seized; }
                    MoneyStock seizedBonds = p.BondHoldings * c.RevolutionTerror;   // ★保有台帳（2026-09-21）：国債も貯蓄と同じ規則で没収（請求権は国内に残る＝下で大衆へ）
                    p.BondHoldings -= seizedBonds; confiscatedBonds += seizedBonds;
                }
            }
        }
        // 粛清・亡命の死＝死の記帳の1本（World.KillPops＝人口の台帳・戦死の累計に計上）。失脚した層の規模の比＝各層の RevolutionTerror の割合。
        if (purgedPops is not null) w.TerrorPurgeCumulative += w.KillPops(purgedPops, null, purgeTotal, violent: true);   // 診断（UI）：粛清量の累計＝書くだけ（挙動非依存）。UIがニュース化に使う
        // 没収資産を大衆pop（非特権・非エリート・非兵士）へSize按分で再分配＝革命の余剰再分配（保存＝pop→pop純移転・貨幣不変）。受け手不在は全popへ按分。
        // ★保有台帳（2026-09-21）：没収した国債も**同じ受け手・同じ按分**で渡す（国の保有の和は不変＝不変条件を保つ）。
        // ★2026-09-30 段4g：貯蓄は移し方の1本（失脚した層＝払い手の重みは自分の没収の額→大衆＝頭数・端数は最後の受け手）。旧＝粛清の走査の中で貯蓄を引き、
        //   全popの頭数も0なら按分の本体の戻り（配れなかった額）を国庫へ入れた（国庫の片側の記帳）＝今は受け手不在なら動かない（払い手に残る＝没収は起きなかった）。
        //   粛清の死（上の死の記帳の1本）は没収の後の規模の和を変えない＝払い手の貯蓄はそのまま＝没収の額は走査の時点の値と同じ。
        if (confiscated > 0 || confiscatedBonds.Value > 0)
        {
            var masses = new List<Pop>(); double ms = 0;
            foreach (var p in w.AllPops()) if (IsMassPop(p)) { masses.Add(p); ms += p.Size; }
            if (!(ms > 1e-9)) { masses = new List<Pop>(w.AllPops()); ms = 0; foreach (var p in masses) ms += p.Size; }   // 受け手不在は全popへ按分
            if (seizedPops is not null) World.Transfer((w, seizedPops), (w, masses, null), confiscated, TreasuryCat.OtherIn, fromWeights: seizedAmt);
            if (ms > 1e-9) foreach (var p in masses) p.BondHoldings += confiscatedBonds * (p.Size / ms);
        }
    }
}
