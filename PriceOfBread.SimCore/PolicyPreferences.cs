namespace PriceOfBread.SimCore;

/// <summary>政策の軸（§9-E）。法整備＋スライダー。各軸に「進める方向」を定め、賛否はその向きで測る。</summary>
public enum PolicyAxis
{
    IncomeTax, SocialSecurity, Coercion, Tariff, EducationSpending,  // スライダー
    PollTax, LandTax, ProgressiveTax,                                             // 税制の拡張（行政力でアンロック）
    // ★専売（旧 ConsumptionTax）は2026-09-20に撤去（作者裁定「これは政治問題ではなく生産マネジメントの範囲」）。
    //   材を専売にするのは生産タブの決断＝政治資本を払わず、要求・綱領・譲歩・予算協賛・掌握の軸のどれにも載らない。
    //   実収入としての会計（TreasuryCat.StateMonopoly・Fiscal の予算基数・PowerShares の常設歳入）は残る＝軸だけが消えた。
    //   ⚠撤去で以降の軸の序数が1つ繰り上がる＝改名前のセーブの ManifestoAxis／PendingRevoltDemand／CrisisDemands／AgendaAxes は
    //     別の軸として復元される（Militarism 撤去2026-08-18と同じ前例）。
    EducationLaw, Suffrage, Censorship, LaborUnion, ShorterHours,                 // 法整備
    // ★親軍法（Militarism）は2026-08-18に撤去（作者裁定「厳密には法ではなく政治機構だ。既存の数値から算出できる」）。
    //   軍国主義は**法でなく4つの導出量**（PoliticalQuantities の練度／政治的優位／公共圏／任免権）なので政策軸にも要求軸にも載らない。
    //   政治的に押せる実体は既存の2軸が持つ：軍事費＝PolicyAxis.Coercion（武力スライダー）／徴兵の深さ＝PolicyAxis.ConscriptionLaw。
    WomenRights,                                                                   // ★女性の権利（統合法・2026-08-10）。なし→財産権→教育権→選挙権の**1本の梯子**。旧3軸（WomenProperty/WomenSuffrage/WomenEducation）を畳んだ
                                                                                   // ＝3軸とも選好の式が**完全に同一**（家父長制の旧支配層が抵抗・近代/大衆が賛成）だったので、賛否連合の情報は1つも失われない
    CornLaws, LandReform, SerfEmancipation,                                        // 議会の法案（段5・第4増分）
    ConscriptionLaw,                                                                // 徴兵法（1934拡張E5・末尾追加＝列挙順保存）。旧支配層/軍が徴兵制を支持（大軍＝権威）・大衆が反対（取られる）
    SecretBallot,                                                                   // 秘密投票法（1934拡張E5・末尾追加）。旧支配層が反対（票の買収/脅迫を失う）・近代/大衆が賛成
    Bureaucracy,                                                                    // 官僚制度（1934拡張E5・末尾追加）。専門化＝旧支配層が反対（縁故任用の特権を失う）・近代/大衆が賛成
    StateReligion,                                                                // 国教法の寛容化（1934拡張E6・末尾追加）。教会（旧支配層）が抵抗（十分の一税/地位を失う）・近代/大衆が賛成（信仰の自由）
    Antitrust,                                                                      // 独占禁止法の強化（1934拡張E6・末尾追加）。独占レントを得る旧支配層/資本が抵抗・大衆が賛成（生活水準↑）
    WartimeRationing,                                                               // 戦時配給法（1934拡張E6・末尾追加）。国家/軍が支持（総力戦の統制）・資本が反対（価格統制で利益減）
    Nationalization,                                                                // 国有化6部門共通の政治価数（1934拡張E6・末尾追加）。資本（近代エリート）が強く反対（利潤没収）・大衆が賛成（社会主義）。6法が同じ軸を共有
    NavyUpkeep,                                                                     // ★海軍維持費（2026-08-10 作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）。
                                                                                    // 旧 NavalEmphasis（0..1の抽象的な「注力度」）を畳んだ跡＝**軸の実体は金**（NationPolicy.NavyUpkeepTarget）。
                                                                                    // 陸海の配分は2026-08-04の作者仕様(5)で既に維持費2本＝金で表現済みで、注力度はその上に乗る二重表現だった。
                                                                                    // 勢力の「海軍の増強」＝海軍維持費を上げろという**金の要求**（ティルピッツ海軍法/八八艦隊）。軍/旧支配層が賛成（艦隊＝権威）・大衆は中立。
                                                                                    // ⚠列挙の位置は動かさない（要求の同点は軸順で決まる＝決定論。末尾へ動かすと並びが変わる）
    ChildLabor,                                                                     // 児童労働法（02断線走査2026-07-28・末尾追加）。軸の増＝合法化。旧・唯一の「政治ゲートを通らない無料スイッチ」だった19世紀最大の労働立法（英1833/1847工場法）。価数はShorterHoursと同型＝資本が合法を支持・大衆が反対
    MilitaryExport,                                                                 // 軍需輸出の法（禁制品doc C5・2026-07-30・**末尾追加**＝列挙順を保存）。軸の増＝**禁止**。武器を作って売る資本（近代エリート）が反対し、その強さは軍需財の生産額シェアから創発（ハードコードの賛否表を書かない）
    OpiumPossession,                                                                // 阿片の保有法（禁制品doc C3・2026-07-30・末尾追加）。軸の増＝許可→輸入禁止→禁止。中毒が蝕む大衆が賛成・阿片を売る資本が反対（生産額シェアから創発）
    OpiumExport,                                                                    // 阿片の輸出法（同・末尾追加）。軸の増＝禁止。植民地の阿片で稼ぐ資本が反対（東インド会社）・大衆は無関心
    ImmigrationRestriction,                                                         // ★移民制限法（法の棚卸しdoc L2・器2026-08-01・末尾追加）。軸の増＝制限を強める。機構はあるのに法が無かった唯一の領域
    MonetaryStandard,                                                               // ★通貨制度の法（通貨doc・器2026-08-01・末尾追加）。軸の増＝兌換の維持（金本位）／減＝兌換の停止（管理通貨）。債務者と債権者が正面から対立する唯一の軸
    CapitalMobility,                                                                // ★資本移動の法（トリレンマdoc・器2026-08-13・末尾追加）。軸の増＝**規制**。
                                                                                    // ⚠これが無かったので資本開放度は**関税の従属変数**だった＝「高関税だが資本は自由」（19世紀後半の独/米）が表現できず、
                                                                                    // 兌換の維持・資本の自由・金融政策の自律の3つを同時に持ててしまっていた（トリレンマの破れ）。
    NationalizationAll,                                                             // ★国有化『全て』専用軸（作者裁定2026-08-14 §11-6d・末尾追加）。政治価数は Nationalization と**同一**（Pref はフォールスルー）＝
                                                                                    //   別軸である理由は前提DAGだけ：『全て』はプロレタリア独裁のみ操作可（LawPrereq）を、UIのグレーアウト・施行・譲歩が同じ1本で読めるようにする。
                                                                                    //   ⚠勢力の要求軸には**しない**（ComputeDemands が除外）＝要求は Nationalization 梯子（最終段＝『全て』）が一元管理＝二重要求を出さない。
    ColonialSuffrage,                                                               // ★植民地参政権（参政権の範囲2法・作者裁定2026-08-16・末尾追加＝列挙順保存）。3段：なし/自国民のみ/全面。
                                                                                    //   （旧の旗 SuffrageScopeGate=1.0 は点火2026-09-06・2026-10-18 に焼き込み。）
    MigrantNativeSuffrage,                                                          // ★移民原住民参政権（同・末尾追加）。2段：なし/あり。本国州popの非中核割合を政治的国民に算入する法
    SpecialistExitBan,                                                              // ★技術者流出禁止法（段⑤・2026-08-17・末尾追加＝列挙順保存）。軸の増＝**禁止**。3段：自由/制限/禁止。
                                                                                    //   ⚠賛否は**近代エリートの内側**で割れる（資本＝雇う側が賛成／技師・研究者＝売る側が反対）＝符号がブロック内の富の比で決まる。
                                                                                    //   休眠 SpecialistExitBanPrefScale=0 では Pref=0＝要求リストに載らない（CapitalMobility と同じ思想③の防壁）
    Guild,                                                                          // ★同業組合法（特権システム 段4・2026-09-07・末尾追加＝列挙順を動かさない）。増＝自由化（剥離）
    CivilianArms,                                                                   // ★民間の武器購入の法（設計メモ§7 (a)・2026-09-11・末尾追加＝列挙順を動かさない）。軸の増＝**禁止**。
                                                                                    //   旧支配層＝武力の独占の防衛者として禁止を望み・近代/大衆＝自衛と蜂起の手段として許可を望む（地位/権力軸の型）。
                                                                                    //   休眠 RebelArmsMarketEnabled=0 では Pref=0＝要求リストにも綱領にも載らない（Guild と同じ防壁）
    MinimumWage,                                                                    // ★最低賃金法（線 lw (459)・2026-10-20・末尾追加＝列挙順を動かさない）。軸の増＝**強める**（なし→生存→生活→充足）。
                                                                                    //   大衆（賃金労働）が賛成・資本（雇う側）と旧支配層（農業の雇い主）が反対。門の係数 MinimumWageLawGate は (843) で焼き込み＝この軸の Pref は常に評価される
    Inheritance,                                                                    // ★相続法（線 inh (650)・末尾追加＝列挙順を動かさない）。軸の増＝**取る率を上げる**（なし→低→中→高→全て）。門の係数 InheritanceLawGate は (843) で焼き込み＝この軸の Pref は常に評価される
}

/// <summary>
/// 利益集団の政策選好（§9-E）。**ハードコードの賛否表を廃し、各ブロックの物質的立場から選好を創発させる**（唯物論）。
/// 3ブロック（0=旧支配層〔Closed＝貴族・聖職〕/ 1=近代エリート〔資本・専門職〕/ 2=大衆〔賃金労働〕）。
/// ・経済軸（税・給付・労働・関税）＝**保有から導出**：税は払う側（その課税ベースのシェア）が反対・給付は受ける側が賛成。
/// ・地位/権力軸（参政権・教育法・検閲・女性財産・農奴解放）＝**旧支配層＝現秩序の防衛者**として導出（自由化で特権を失う）。
/// ・**国家を歳入主体に加える**：税には国家の歳入要求が＋に働く＝誰も支持しない税（専売）も財政逼迫時に課税可能になる。
/// 変更に反対するブロックの力が大きいほど施行が遅く（スライダー）/通らない（法整備）。
/// </summary>
public static class PolicyPreferences
{
    private const double BurdenScale = 3.0;   // 課税ベースのシェア→反対強度の換算（シェア0.67で約−2＝強反対）
    internal const double StatusScale = 2.0;   // 自由化に対する旧支配層の抵抗強度（特権の大きさ＝富シェアで変調）
    // マジックナンバー一掃（2026-07-20・値据え置き・命名のみ）
    // ★案D（2026-08-31）：RentProtectionWeight はハードコード定数からCoefficients.RentProtectionWeightCoef へ係数化（当時の既定1.5＝厳密恒等・#17調整対象・今の既定は1.0〔点火2026-08-31 T2腕〕）
    private const double ProgressiveCapitalSurcharge = 1.5;      // 累進課税＝資本（近代エリート）への追加反対強度
    private const double NationalizationCapitalOpposition = 1.5; // 国有化＝資本（近代エリート）の利潤没収への反対強度
    private const double LandTaxRentierThreshold = 0.34;         // 土地税＝この地代シェア未満なら転嫁を歓迎（賛成）
    internal const int BlocCount = 3;                             // 3ブロック（0旧支配層/1近代エリート/2大衆）＝党（保守0/自由1/社会2）と1:1対応

    /// <summary>ブロックの経済プロファイル＋力（1パス集計。selfplay並列のため static キャッシュは使わない）。
    /// ★internal（2026-10-01 (146) 計算量の予算・恒等）：同じ瞬間に何本も立場を読む呼び手（要求の順位 <see cref="FactionSnapshot.ComputeDemands"/>・
    ///   スライダーの一歩 Government.Move）が <see cref="Build"/> を1回だけ取り、下の `in Profile` の読み口へ渡す＝その間に何も書かない呼び手に限る
    ///   （書き込みを挟む呼び手が古い立場を読まないよう、キャッシュは作らない＝World に置いた導出量は記憶になる）。</summary>
    internal readonly struct Profile
    {
        public readonly double[] Power;        // 政治力（富×参政権重み）
        public readonly double[] Wealth;       // 富のシェア（誰が富裕＝資産課税の的）
        public readonly double[] Rent;         // 地代のシェア（誰が地主＝土地税の的）
        public readonly double[] Income;       // 所得のシェア
        public readonly double[] Poll;         // 人頭税の課税対象（非免除）人口シェア（Closedは免除＝0）
        public readonly double[] Old;          // ブロック内の高齢比率（60歳以上・年齢の層。法多様性L2＝年金の受益者。高齢化した国ほど大きい＝水準であって配分シェアではない）
        public readonly double StateRevenue;   // 国家の歳入要求（0..1.5。財政逼迫で増す。税を＋方向に押す主体）
        // ★2026-09-26 A2-2：名目GDP（World.NominalGdpPerMonth と同じ値）。Build で州×生産者の走査を1回だけ走らせて持つ＝Pref の読み手は走査し直さない。
        //   Build と Pref は同じ呼び出しの中で読むだけ（このファイルに産出・州・物価水準を書く行は無い）＝値は getter とビット単位で同じ。
        public readonly double NominalGdp;
        // ★(547) 線 rs2：旧支配層（ブロック 0）の所得のうち地代の割合（正規化の前の和の比＝Rent[0]÷Income[0]・所得 0 なら 0）。農奴解放・土地改革への反対の強さが読む。新しい和は足さない（BuildSums の既存の 2 本の比）。
        public readonly double LordRentToIncome;
        // ★(937) 土地税の基数のシェア（Fiscal.LandTaxBase・旗 LandTaxFixedBasis＝0 では Rent と同じ値）。読むのは土地税の軸だけ（地代の保護と LordRentToIncome は所得の量＝Rent のまま）。
        public readonly double[] LandBase;
        // ★(1048) c99db：旧支配層（ブロック 0）の所得のうち聖職者の所得の割合（正規化の前の和の比＝ClergyInc0÷Income[0]・所得 0 なら 0＝LordRentToIncome と同じ型）。国教の反対の強さ（D-1b）が読む。
        //   (1039) の ClergyIncome0（生の月額）を比へ替えた（反証役 v：生の額を Income のシェアと並べると次元が合わない）。
        public readonly double ClergyToIncome0;
        // ★(1048) c99db：仲介者の取り分の旧支配層の所得に対する割合×仲介者の手に残る部分 ρ（LastRuleCut÷Income[0]×ρ）。官僚制度と土地改革の反対の強さ（D-2）が読む。旗 LordIntermediaryStake＝0 は 0（州を走査しない）。
        public readonly double CutRhoToIncome0;
        public Profile(double[] pw, double[] we, double[] re, double[] inc, double[] po, double[] old, double sr, double nominalGdp, double lordRentToIncome, double[] landBase, double clergyToIncome0 = 0.0, double cutRhoToIncome0 = 0.0)
        { Power = pw; Wealth = we; Rent = re; Income = inc; Poll = po; Old = old; StateRevenue = sr; NominalGdp = nominalGdp; LordRentToIncome = lordRentToIncome; LandBase = landBase; ClergyToIncome0 = clergyToIncome0; CutRhoToIncome0 = cutRhoToIncome0; }
    }

    /// <summary>大衆の平均失業率（人口加重・移民制限法の選好と SelfPlay の移民法の意図が読む＝唯一定義・2026-09-11 #13）。⚠労働市場の無い小シナリオでは0＝無影響。</summary>
    internal static double AvgUnemployment(World w)
    {
        double u = 0, size = 0;
        foreach (var p in w.AllPops()) if (p.IsCommoner) { u += p.UnemploymentRate.Value * p.Size; size += p.Size; }
        return size > 1e-9 ? u / size : 0;
    }

    /// <summary>popの3ブロック分類（0旧支配層/1近代エリート/2大衆）。<see cref="LawGap"/> の添字はこれ。
    /// ★クラスタ側の分類も同じこの1本（2026-09-28 処理の一元化 段1＝旧は FactionSnapshot・Factions・Politics に同じ三項式の写しが4箇所）。
    /// 〔2026-10-11 (271)〕第4束「小営業」（旧 PettyInterestGate＝既定 0 の枝）は削除＝3値だけ。</summary>
    internal static int BlocOf(Pop p) => p.Closed ? 0 : p.IsModernElite ? 1 : 2;

    internal static Profile Build(World w)
    {
        var (t, m, s) = InterestGroups.Blocs(w);
        double[] power = [t, m, s];
        //   ★POP 由来の和（富・地代・所得・人頭税の母体・高齢の人口）は第 3 走査＝窓の中（<see cref="PopWindow"/>）は使い回す（写しを渡す＝下の正規化が書き換える）。
        var sums = w.PopSums is { } win ? win.BuildSums(w) : ComputeBuildSums(w);
        double[] wealth = sums.Wealth, rent = sums.Rent, income = sums.Income, poll = sums.Poll, old = sums.Old, blocPop = sums.BlocPop;
        double tw = sums.Tw, tr = sums.Tr, ti = sums.Ti, tp = sums.Tp, clergyInc0 = sums.ClergyInc0;
        double lordRentToIncome = RentToIncomeOf(rent[0], income[0]);   // ★(547) 正規化の前の和の比（正規化は同じ配列を書き換える）
        double clergyToIncome0 = RentToIncomeOf(clergyInc0, income[0]);   // ★(1048) 同じ比の関数（所得 0 なら 0）＝聖職者の所得÷旧支配層の所得
        double cutRhoToIncome0 = w.Coef.LordIntermediaryStake > 0 ? RentToIncomeOf(w.LastRuleCut, income[0]) * IntermediaryKeepShare(w) : 0.0;   // ★(1048) D-2：旗 0 は州を走査しない
        Normalize(wealth, tw); Normalize(rent, tr); Normalize(income, ti); Normalize(poll, tp);
        double[] landBase = sums.LandBase; Normalize(landBase, sums.Tl);   // ★(937) 土地税の基数のシェア（旗 0 は rent と同じ値・同じ順の和）
        for (int i = 0; i < BlocCount; i++) old[i] = blocPop[i] > 1e-9 ? old[i] / blocPop[i] : 0;   // ブロック内の高齢比率＝水準（高齢化した国だけ大きい）
        // 国家の歳入要求：常に基礎0.5（国家は財源を欲する）＋財政逼迫（funding不足・債務/GDP）で増す。＝誰も支持しない税も国家が押せる。
        double ngdp = w.NominalOf(w.GdpPerMonth.Value);   // ★A2-2：州×生産者の走査はこの1回だけ（旧＝ここで2回・Pref の名目GDPの読み手ごとに1回ずつ）
        double stress = w.FiscalEnabled
            // [単位・B13] 債務/GDP＝**年次名目**（GovDebt÷(月次NominalGdpPerMonth×12)・Min 1）＝Fiscal の国債の金利・CapitalMarket・Government の緊縮と同じ物差し（2026-09-29・棚卸し fis-02 の残り）。
            //   旧＝**月次実質**（年次の約12倍の値）＝国債が年GDP の1割（8.3%）を越えた国は全て1で飽和＝国債の多さが歳入の要求に効かなかった。
            ? PoliticalQuantities.Clamp01((1.0 - w.FiscalFundingRatio.Value) + (ngdp > 1e-9 ? System.Math.Min(1.0, Calendar.ToGdpYear(w.GovDebt, Calendar.PerMonthToPerYear(new MoneyPerMonth(ngdp))).Value) : 0))
            : 0;
        return new Profile(power, wealth, rent, income, poll, old, 0.5 + stress, ngdp, lordRentToIncome, landBase, clergyToIncome0, cutRhoToIncome0);
    }

    /// <summary>★(1048) c99db D-2：仲介者の取り分のうち、在地の区分が国家の区分へ移ると仲介者の手を離れる部分 ρ＝Σ_州 n·R1·f ÷ Σ_州 n·(R1＋R0·(1−f))
    /// （n＝州の人口・R1＝在地の区分・R0＝国家の区分・f＝AdminFill を [0,1] へ・Rule と RuleInit の長さ 4 の州だけ・分母 0 なら 0）。
    /// ⚠限界：エンジンの取り分は査定で重みづく＝人口で按分するのは近似（計器 lordstat_s の ⚠限界① と同じ）。在地の目標の min(1−無統治, …) と支配度の遅れは数えない。純関数・状態なし。</summary>
    internal static double IntermediaryKeepShare(World w)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.Rule.Length != 4 || r.RuleInit.Length != 4) continue;
            double pop = r.SumPopulation(); if (!(pop > 0)) continue;
            double f = r.AdminFill > 0 ? (r.AdminFill < 1 ? r.AdminFill : 1.0) : 0.0;   // 否定形＝NaN は 0
            num += pop * r.Rule[1] * f; den += pop * (r.Rule[1] + r.Rule[0] * (1.0 - f));
        }
        return den > 0 ? num / den : 0.0;
    }

    /// <summary><see cref="Build"/> の POP 由来の和（ブロックごと・正規化の前）。国の入力（法・国庫）は読まない＝窓の中の使い回し（<see cref="PopWindow"/>）に載せられる。</summary>
    internal sealed class BuildSums
    {
        internal double[] Wealth = new double[BlocCount], Rent = new double[BlocCount], Income = new double[BlocCount], Poll = new double[BlocCount];
        internal double[] Old = new double[BlocCount], BlocPop = new double[BlocCount];
        internal double[] LandBase = new double[BlocCount];   // ★(937) 土地税の基数（Fiscal.LandTaxBase）のブロックの和
        internal double Tw, Tr, Ti, Tp, Tl;
        internal double ClergyInc0;   // ★(1039) ブロック 0 の聖職者の max(0, 所得) の和（Income[0] の内訳・窓の写し Copy と番人 CheckSame にも同じ 1 本）
        internal BuildSums Copy() => new()
        {
            Wealth = (double[])Wealth.Clone(), Rent = (double[])Rent.Clone(), Income = (double[])Income.Clone(), Poll = (double[])Poll.Clone(),
            Old = (double[])Old.Clone(), BlocPop = (double[])BlocPop.Clone(), Tw = Tw, Tr = Tr, Ti = Ti, Tp = Tp,
            LandBase = (double[])LandBase.Clone(), Tl = Tl, ClergyInc0 = ClergyInc0,
        };
        /// <summary>番人（<see cref="PopWindow.Verify"/>）：求め直した和と 1 ビットも違わないことを確かめる。</summary>
        internal void CheckSame(BuildSums f)
        {
            PopWindow.Check("立場の土台の合計(富)", Tw, f.Tw); PopWindow.Check("立場の土台の合計(地代)", Tr, f.Tr);
            PopWindow.Check("立場の土台の合計(所得)", Ti, f.Ti); PopWindow.Check("立場の土台の合計(人頭税)", Tp, f.Tp);
            PopWindow.Check("立場の土台の合計(土地税の基数)", Tl, f.Tl);
            PopWindow.Check("立場の土台(聖職者の所得)", ClergyInc0, f.ClergyInc0);
            for (int i = 0; i < BlocCount; i++)
            {
                PopWindow.Check("立場の土台(富)", Wealth[i], f.Wealth[i]); PopWindow.Check("立場の土台(地代)", Rent[i], f.Rent[i]);
                PopWindow.Check("立場の土台(所得)", Income[i], f.Income[i]); PopWindow.Check("立場の土台(人頭税)", Poll[i], f.Poll[i]);
                PopWindow.Check("立場の土台(高齢)", Old[i], f.Old[i]); PopWindow.Check("立場の土台(ブロックの人口)", BlocPop[i], f.BlocPop[i]);
                PopWindow.Check("立場の土台(土地税の基数)", LandBase[i], f.LandBase[i]);
            }
        }
    }

    /// <summary><see cref="Build"/> の第 3 走査の本体＝国家会計の集合（<see cref="World.AllPopsWithRegion"/>）を 1 周して POP 由来の和を数える。窓の外は毎回これ・窓の中は初回と番人の求め直しがこれ。</summary>
    internal static BuildSums ComputeBuildSums(World w)
    {
        var o = new BuildSums();
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            int b = BlocOf(p);
            double sv = System.Math.Max(0, p.Savings.Value);
            o.Wealth[b] += sv; o.Tw += sv;
            double rn = System.Math.Max(0, p.LandRentPerMonth.Value); o.Rent[b] += rn; o.Tr += rn;
            double lb = w.Coef.LandTaxFixedBasis > 0 ? System.Math.Max(0, Fiscal.LandTaxBase(w.Coef, p).Value) : rn; o.LandBase[b] += lb; o.Tl += lb;   // ★(937) 土地税の基数（旗 0 は rn と同じ値）
            double inc = System.Math.Max(0, p.EarnedPerMonth.Value); o.Income[b] += inc; o.Ti += inc;
            if (b == 0 && p.ClassId == PopClass.Clergy) o.ClergyInc0 += inc;   // ★(1039) 旧支配層の聖職者の所得（Income[0] の内訳）
            if (p.IsCommoner) { o.Poll[b] += p.Size; o.Tp += p.Size; }   // 人頭税の対象（特権＝Closed は免除）
            { o.Old[b] += p.Size * p.AgeShare(60, double.PositiveInfinity); o.BlocPop[b] += p.Size; }   // 法多様性L2：ブロックの高齢人口（年金の受益者）。★(471) 兵士も数える（年金の受け手と同じ集合。Old は BlocPop の比にしか使われない＝分子と分母が同じ規則）
        }
        return o;
    }

    /// <summary>地代÷所得（所得が 0 以下なら 0）。ブロック 0 の和は BuildSums の正規化の前の値＝計器 <see cref="Instruments"/> も同じ関数を通す。</summary>
    internal static double RentToIncomeOf(double rent, double income) => income > 1e-9 ? rent / income : 0.0;

    /// <summary>★(547) 線 rs2：産地の取り分の慣習＝先頭の州（州の並びの最初・(559) gc2 で稼働の判定を外した＝本番は全ての州が稼働中・通常は首都・首都を失った後は首都とは限らない）の産地（RGO）の生産者の RentShare の最小。
    /// 産地の取り分は min(生産者の慣習 RentShare, 州の地代の上限 FarmRentCap)（EconomyStep.RentShareOf）で、慣習は国の農制 farmLordShare から作る値（Simulation.CatalogEconomy・
    /// 賦存の大農園だけ PlantationRentShare で上がる）＝最小は賦存でない産地の値。World に慣習の欄は無い（新しい状態を作らない）ので 1 州の生産者を読む。産地が無ければ 0（失う所得 0）。</summary>
    internal static double CustomaryRentShare(World w)
    {
        if (w.Regions.Count == 0) return 0.0;
        double c = double.PositiveInfinity;
        foreach (var pr in w.Regions[0].Producers) if (pr.IsRgo && pr.RentShare.Value < c) c = pr.RentShare.Value;
        return double.IsPositiveInfinity(c) ? 0.0 : c;
    }

    /// <summary>★(547) 線 rs2：<paramref name="axis"/>（農奴解放か土地改革）の次の一段で旧支配層が失う所得の割合＝
    /// (今の実効の取り分−次の実効の取り分)÷今の実効の取り分 × <paramref name="lordRentToIncome"/>。実効の取り分＝min(慣習, 地代の上限)・地代の上限は FarmRentCap と同じ式
    /// （土地改革と農奴解放の上限の小さい方・<see cref="Simulation.RentCap"/>）。次の一段＝その軸だけ 1 段進める（Full の次は Full＝失う 0）。減らす向きの Pref はこの値でなく <see cref="LordIncomeGain"/> を読む（(803) で旗を焼き込んだ）。</summary>
    internal static double LordIncomeLoss(World w, PolicyAxis axis, double lordRentToIncome)
    {
        EffectiveRentShares(w, axis, out double now, out double next);
        return now > 1e-9 ? (now - next) / now * lordRentToIncome : 0.0;
    }

    /// <summary>今と、<paramref name="axis"/> を 1 段進めた後（<paramref name="back"/>＝true なら 1 段戻した後）の実効の取り分 min(慣習, 土地改革と農奴解放の上限の小さい方)
    /// （進める向きは <see cref="LordIncomeLoss"/>・戻す向きは <see cref="LordIncomeGain"/> の中身＝計器 <see cref="Instruments"/> も同じ関数を通す）。</summary>
    internal static void EffectiveRentShares(World w, PolicyAxis axis, out double now, out double other, bool back = false)
    {
        var c = w.Coef;
        var lr = w.LandReform; var se = w.SerfEmancipation;
        var lrOther = axis == PolicyAxis.LandReform ? Step(lr, back) : lr;
        var seOther = axis == PolicyAxis.SerfEmancipation ? Step(se, back) : se;
        double custom = CustomaryRentShare(w);
        now = System.Math.Min(custom, System.Math.Min(Simulation.RentCap(lr, c.LandReformRentCap.Value), Simulation.RentCap(se, c.SerfEmancipationRentCap.Value)));
        other = System.Math.Min(custom, System.Math.Min(Simulation.RentCap(lrOther, c.LandReformRentCap.Value), Simulation.RentCap(seOther, c.SerfEmancipationRentCap.Value)));
    }

    /// <summary>1 段進める（Full の次は Full）か、1 段戻す（なし の前は なし）。</summary>
    private static ReformLevel Step(ReformLevel lv, bool back) => back ? (lv == ReformLevel.None ? ReformLevel.None : lv - 1) : (lv == ReformLevel.Full ? ReformLevel.Full : lv + 1);

    /// <summary>★(650) 相続法の次の段で動く率の差 Δr（進める向き）＝rate(cur+1)−rate(cur)。最上段では今の段の大きさ rate(top)−rate(top−1)（最上段の資産階級の Pref を負に保つ＝戻す要求が立つ）。純関数・状態なし。</summary>
    internal static double InheritanceStepSize(World w)
    {
        int cur = (int)w.Inheritance, top = (int)InheritanceLaw.All;
        return cur < top ? Government.InheritanceRate((InheritanceLaw)(cur + 1)) - Government.InheritanceRate((InheritanceLaw)cur)
                         : Government.InheritanceRate(InheritanceLaw.All) - Government.InheritanceRate(InheritanceLaw.High);
    }

    private static void Normalize(double[] a, double total)
    { if (total > 1e-9) for (int i = 0; i < BlocCount; i++) a[i] /= total; }

    // 「進める方向」への各ブロックの賛否（+賛成 / −反対）を**経済プロファイルから導出**。連続値（概ね −2..+2）。
    // ★internal（2026-10-01 (146)）：要求の順位（FactionSnapshot.ComputeDemands）が自分のブロックの1本だけを読む（旧＝Stance で3ブロックと純支持を全て計算して1本を捨てた）。
    // ★(558) 線 rs3：increasing＝**どちらの向きの選好として読むか**（既定 true＝進める向き）。返り値は常に「進める向き」の符号の値（呼び手が ×sign する）。減らす向き（false）で値が変わるのは
    //   旧支配層の農奴解放・土地改革の 2 行（★(1048)：旗＞0 なら官僚制度・国教の旧支配層も）だけ（<see cref="DirectionAware"/>）。他の全ての軸・ブロックは increasing を読まない。
    internal static double Pref(PolicyAxis a, int b, in Profile e, World w, bool increasing = true)
    {
        bool trad = b == 0, modern = b == 1, mass = b == 2;
        switch (a)
        {
            // === 経済軸（保有から導出）===
            case PolicyAxis.IncomeTax:        // 所得税＝富裕が払う・大衆は再分配で得る
                return (mass ? 1.0 : 0) - BurdenScale * e.Wealth[b];
            case PolicyAxis.ProgressiveTax:   // 累進＝資本（最大の払い手）ほど強く反対
                return (mass ? 1.0 : 0) - (BurdenScale + (modern ? ProgressiveCapitalSurcharge : 0)) * e.Wealth[b];
            case PolicyAxis.LandTax:          // 土地税＝地代の受け手（地主）が反対・他は転嫁を歓迎
                return (e.LandBase[b] < LandTaxRentierThreshold ? 1.0 : 0) - (BurdenScale + 1.0) * e.LandBase[b];   // ★(937) 負担は土地税の基数（旗 0 は Rent と同じ値）
            case PolicyAxis.PollTax:          // 人頭税＝対象（大衆）が反対・免除（特権）は転嫁を歓迎
                return (e.Poll[b] < 1e-6 ? 1.0 : 0) - BurdenScale * e.Poll[b];
            case PolicyAxis.SocialSecurity:   // 福祉＝大衆が受益・富裕が財源負担。法多様性L2＝高齢比率の受益者項（年金軸が同じPrefを使う＝高齢化した国だけ年金が通る・ビスマルク1889型）
                // ★年齢の層（2026-09-30 段4e 後半・設計書 §4-1「PensionOldWeight を標準の値で割り直す」＝起点合わせ）：受益者項＝標準のピラミッドより老いた分（60歳以上の割合−標準の 7.5%）。
                //   旧＝老年比が1783年に0から貯まった（上限0.15）＝「高齢化した国だけ年金が通る」の物差しの起点が0だった。年齢の層は1783年に 7.5% から始まる＝差を取らないと全ての国の全てのブロックで
                //   開幕から +2.5×0.075 の年金の支持が立つ（器の置き換えの人工物）。若い国（標準より若い）は負＝年金を急がない。
                return (mass ? 1.0 : 0) - 2.0 * e.Wealth[b] + w.Coef.PensionOldWeight * (e.Old[b] - Pop.StdShare(60, double.PositiveInfinity));
            case PolicyAxis.EducationSpending:// 教育費＝大衆/近代は受益（上昇・人材）・旧支配層（教会の教育独占）は失う
                return trad ? -1.0 : 1.0;
            case PolicyAxis.Tariff:           // 関税＝保護される生産者（地代＝穀物）が賛成・消費者（大衆）が反対
            {
                // 接8（監査2026-07-11・2026-07-25休眠配線）：競争力ベースの州関税利害（毎期算出済み・従来は分離判定専用）を国政の関税選好へ接続＝
                // 後発工業国の資本家が保護関税を求める回路（米国システム・独1879年型）。近代エリート項に活動州の関税利害平均を加算。
                // ＋国家の歳入要求（19世紀最大の歳入源だった関税）を専売と同型で加算。両係数とも既定0＝不読＝現行と厳密一致。
                double tariffExtra = 0;
                if (modern && w.Coef.TariffInterestPrefWeight > 0)
                {
                    double tiSum = 0; int tiN = 0;
                    // 2026-07-27 是正：首都州(Id==0)は Politics.CheckSecession の分離判定から除外されており TariffInterest が**永久に既定0**。
                    // その構造的な0を平均に混ぜていたため州数の少ない国ほど強く希釈されていた（2州国なら実効半減）。算出されている州だけで平均する。
                    foreach (var rg in w.Regions) if (rg.Id != 0) { tiSum += rg.TariffInterest; tiN++; }
                    if (tiN > 0) tariffExtra += w.Coef.TariffInterestPrefWeight * (tiSum / tiN);
                }
                if (w.Coef.TariffStateRevenueWeight > 0) tariffExtra += w.Coef.TariffStateRevenueWeight * e.StateRevenue;
                // ★輸出利害の反関税（TariffExportOppositionWeight・2026-08-31）：輸出で食う資本（近代エリート）は自由貿易を推す
                //   （マンチェスター学派・穀物法撤廃1846の物質的基盤）。強さ＝輸出/GDPシェア（既存の LastExportValue／名目GDP・クランプ0..1）。
                //   ⚠保護利害（TariffInterestPref＝後発の資本家は保護を求める）と**同じブロック内で正面から割れる**＝独1879 vs 英1846が保有から出る。0＝恒等
                if (modern && w.Coef.TariffExportOppositionWeight > 0 && e.NominalGdp > 1e-9)
                {
                    // ★案C拡張（2026-08-31）：反対の物質基盤＝輸出＋**中間投入の輸入**（資本の投入コスト利害＝原料関税は自分の原価）。
                    //   輸出だけだと「貿易が死んだ後では反対が立たない」履歴の罠（実測）＝投入輸入が現在の生産から立つ分だけ罠が浅くなる。
                    double tradeBase = w.LastExportValue;
                    if (w.ImportsByGood is { } impC)
                        for (int gC = 0; gC < impC.Length && gC < Catalog.Goods.Length; gC++)
                            if (Catalog.Goods[gC].Tier is GoodTier.Producer or GoodTier.Intermediate) tradeBase += impC[gC];
                    tariffExtra -= w.Coef.TariffExportOppositionWeight * PoliticalQuantities.Clamp01(tradeBase / e.NominalGdp);
                }
                // （報復の圧力を支持項として載せる器 TariffRetaliationViaPolitics は2回差し戻し＝2026-09-26 に削除・dorm-51。偏倚は Government が目標へ足す1本だけ）
                return w.Coef.RentProtectionWeightCoef * e.Rent[b] - (mass ? 1.0 : 0) + tariffExtra;
            }
            case PolicyAxis.CornLaws:         // 穀物法＝地主が賛成・産業/大衆は安いパンで撤廃
                return w.Coef.RentProtectionWeightCoef * e.Rent[b] - (trad ? 0 : 1.0);
            case PolicyAxis.LaborUnion:       // 労働組合＝労働者（大衆）が賛成・資本（近代）が反対。法多様性L3＝闘争譲歩（参政権と同型・閾値付き＝高闘争国だけ資本が譲歩＝ストの脅威が容認を引き出す）
                double unionConc = w.Coef.LaborMilitancyConcession * System.Math.Max(0, Politics.AvgMassMilitancy(w) - w.Coef.RiotMilitancyThreshold);
                return mass ? 1.0 : -0.5 + unionConc;
            case PolicyAxis.ShorterHours:     // 労働時間（WorkHours enum）。**Pref は「進める向き＝enum増＝長時間化」への賛否**（A-1#2配線2026-06-24）。WorkHours enum は Eight<Ten<Twelve<Unlimited＝増が長時間。長時間化＝雇用主(資本)が賛成・労働者(大衆)が反対。StepEnum が短縮(enum減)時に符号反転し「短縮＝大衆賛成・資本反対」になる。法多様性L3＝闘争譲歩で資本の長時間支持が緩む（閾値付き）
            case PolicyAxis.ChildLabor:       // 児童労働（02断線走査2026-07-28）＝軸の増＝合法化。ShorterHoursと同型：資本が賛成（安価な労働）・大衆が反対（子を取られる）・闘争譲歩で資本が緩む
                double hoursConc = w.Coef.LaborMilitancyConcession * System.Math.Max(0, Politics.AvgMassMilitancy(w) - w.Coef.RiotMilitancyThreshold);
                return mass ? -1.0 : modern ? 1.0 - hoursConc : 0;
            case PolicyAxis.Coercion:         // 治安維持力＝それで地位を守る側（旧支配層）が賛成・抑圧される大衆が反対
            case PolicyAxis.ConscriptionLaw:   // 徴兵法（E5）＝旧支配層/軍が賛成（大軍＝権威）・大衆が反対（取られる）＝治安維持力と同型
                return trad ? 1.0 : mass ? -1.0 : 0;
            case PolicyAxis.NavyUpkeep:        // 海軍維持費（2026-08-10 作者裁定＝旧「海軍偏重」の振り替え先）＝艦隊に払う金。旧支配層が賛成（艦隊の威信）・大衆は中立（徴兵と違い取られない）
                return trad ? 0.5 : 0;

            // === 地位/権力軸（旧支配層＝現秩序の防衛者。自由化で特権を失う。抵抗は富＝特権の大きさで変調）===
            case PolicyAxis.EducationLaw:     // 世俗の公教育＝教会（旧支配層）が強く抵抗
            case PolicyAxis.WomenRights:      // ★女性の権利の梯子（統合法・2026-08-10）＝家父長制の根幹に触れるので旧支配層が強く抵抗・近代/大衆（女性・社会党・自由派）が賛成。
                                              // ⚠旧3軸（財産権/教育権/参政権）は**この1行と一字一句同じ式**を3回書いていた＝統合しても賛否連合は不変。
                                              // 段による難度差は選好でなく**構造**が持つ：最上段（選挙権）だけが憲法級（政治資本では押せない）＋男性普通選挙が前提。
                return trad ? -StatusScale * (0.5 + e.Wealth[0]) : 1.0;
            case PolicyAxis.SerfEmancipation: // 農奴解放＝農奴所有の地主が強く抵抗
                // ★(547) 線 rs2：強さ＝〈次の一段で失う所得の割合〉（貯蓄の割合 W0 は政治力にも入る二重計上＝(540)）。(803) で旗 LordOppositionByRentLoss を焼き込んだ（旧の式 W0 は消した）
                // ★(558) 線 rs3：減らす向き（increasing=false）の強さは〈1 段戻したときの得〉（LordStake）。(803) で旗 LordOppositionReverseGain を焼き込んだ
                return trad ? -StatusScale * (0.5 + LordStake(w, a, e, increasing)) : 1.0;
            case PolicyAxis.LandReform:       // 土地改革＝地主が強く抵抗・大衆が強く賛成
                // ★(1048) c99db D-2：旗＞0 は地代の損（得）に仲介の取り分の損（得）を足す（同じ一段で I が動く分だけ・二重計上なし＝地代は Rent・取り分は EarnedPerMonth の側）
                if (trad && w.Coef.LordIntermediaryStake > 0) return -StatusScale * (0.5 + LordStake(w, a, e, increasing) + IntermediaryStake(w, a, e, increasing));
                return trad ? -StatusScale * (0.5 + LordStake(w, a, e, increasing)) : mass ? 2.0 : 1.0;
            case PolicyAxis.Censorship:       // 報道の自由化＝旧支配層が抵抗（検閲＝統制）・近代/大衆は自由を好む
            case PolicyAxis.SecretBallot:     // 秘密投票（E5）＝旧支配層が抵抗（票の買収/脅迫の力を失う）・近代/大衆が賛成（真の選挙）
                return trad ? -1.0 : 1.0;   // (1039) 検閲と秘密投票は札のまま（下の官僚制度と国教の受け口の旗に巻き込まない）
            case PolicyAxis.Bureaucracy:      // 官僚制度の専門化（E5）＝旧支配層が抵抗（縁故任用の特権を失う）・近代/大衆が賛成（実力任用）
                // ★(1048) c99db D-2：旗＞0 は〈次の一段で失う仲介の取り分の割合〉（減らす向きは 1 段戻した得）＋底 0.5（LordStake と同じ型）。旗 0 は札 −1（番人 LordstatD1aTests D1a_2）
                if (trad && w.Coef.LordIntermediaryStake > 0) return -StatusScale * (0.5 + IntermediaryStake(w, a, e, increasing));
                return trad ? -1.0 : 1.0;
            case PolicyAxis.StateReligion:    // 国教の寛容化（E6）＝教会（旧支配層）が抵抗（十分の一税/地位）・近代/大衆が賛成（信仰の自由）
                // ★(1048) c99db D-1b：旗＞0 は〈次の一段で失う十分の一税の度合い〉×聖職者の所得の割合（減らす向きは 1 段戻した得）＋底 0.5。旗 0 は札 −1
                if (trad && w.Coef.LordReligionStake > 0) return -StatusScale * (0.5 + ReligionStake(w, e, increasing));
                return trad ? -1.0 : 1.0;
            case PolicyAxis.Antitrust:        // 独占禁止（E6）＝独占レントを得る旧支配層(塩の王室のgabelle)/資本(茶・トラスト)がともに抵抗・高値を払わされる大衆が賛成
                return mass ? 1.0 : -1.0;
            case PolicyAxis.Guild:            // 同業組合の廃止（特権 段4）＝工場資本（自由な労働）と大衆（安い製品・参入の自由）が賛成・旧支配層は中立
                                              // ⚠GuildLawGate=0 では全ブロック0＝綱領・要求・戦線のどれにも現れない（uishot 1786年の総選挙に「同業組合の廃止」が出た＝休眠の漏れ・2026-09-07 是正）
                return w.Coef.GuildLawGate > 0 ? (trad ? 0.0 : 1.0) : 0.0;
            case PolicyAxis.CivilianArms:     // 民間の武器購入の禁止（2026-09-11）＝旧支配層が禁止を望み（武力の独占＝現秩序の防衛・英1819六法）・近代/大衆が許可を望む（自衛と蜂起の手段＝国民衛兵1789）。
                                              // 地位/権力軸の型（検閲・秘密投票と同じ±1）＝保有から導く量が無い軸。⚠RebelArmsMarketEnabled=0 では全ブロック0＝綱領・要求・戦線のどれにも現れない（Guild と同じ休眠の防壁）
                return w.Coef.RebelArmsMarketEnabled > 0 ? (trad ? 1.0 : -1.0) : 0.0;
            case PolicyAxis.MinimumWage:      // 最低賃金法（線 lw (459)・2026-10-20）＝賃金労働者（大衆）が賛成・雇う側の資本（近代）が反対・農業の雇い主（旧支配層）は弱く反対。符号の出所＝労働組合（LaborUnion）と同型（作者の向きを AI が写した形・確証なし）。
                                              // （門の係数 MinimumWageLawGate は (843) で焼き込み＝綱領・要求・戦線に常に現れうる）
                return mass ? 1.0 : modern ? -1.0 : -0.5;
            case PolicyAxis.Inheritance:      // 相続法（線 inh (650)）＝所得税と同じ型：大衆が国庫の使い道で得る（+1.0・段の大きさに依らない）・資産の持ち手は取られる率に比例して反対（BurdenScale×Δr×貯蓄のシェア）
                                              // Δr＝次の段で動く率の差（最上段では今の段の大きさ＝最上段の資産階級の Pref が負＝戻す要求が立つ）。（門の係数 InheritanceLawGate は (843) で焼き込み）
                return (mass ? 1.0 : 0.0) - BurdenScale * InheritanceStepSize(w) * e.Wealth[b];
            case PolicyAxis.WartimeRationing: // 戦時配給（E6）＝国家/軍が総力戦の統制として支持・資本が価格統制で反対（大衆は中立＝公平配給だが統制も伴う）
                return trad ? 0.5 : modern ? -0.5 : 0;
            case PolicyAxis.MilitaryExport:   // 軍需輸出の禁止（C5・2026-07-30）＝武器を作って売る資本が利益から反対。強さは**軍需財の生産額シェア**から創発
                                              // （Tariff の地代シェアと同型＝保有から導出）。武器産業を持たない国では 0＝反対が立たない＝国ごとの特例を書かずに差が出る。
                                              // 軍の賛成は既存の統帥権（ArmyStake＝軍事軸のみ）が担う＝ここには足さない（作者裁定：法の賛否だけで表現する）。
                return modern ? -w.Coef.MilitaryExportPrefScale * MilitaryOutputShare(w, e.NominalGdp) : 0;
            case PolicyAxis.OpiumPossession:  // 阿片の禁止（C3・2026-07-30）＝中毒に蝕まれる大衆が賛成（依存度そのものが賛成の強さ＝害が大きいほど禁令圧が高まる）・
                                              // 売る側の資本が反対（阿片の生産額シェアから創発）。旧支配層は中立。清の禁令が「阿片が広まってから」出るのが条件から創発する。
                return mass ? 1.0 + w.Coef.OpiumBanPrefScale * NationalOpiumAddiction(w)
                     : modern ? -w.Coef.OpiumTradePrefScale * OpiumOutputShare(w, e.NominalGdp) : 0;
            case PolicyAxis.OpiumExport:      // 阿片の輸出禁止（C3）＝植民地の阿片で稼ぐ資本（東インド会社）だけが反対。自国民は中毒にならないので大衆は無関心
                return modern ? -w.Coef.OpiumTradePrefScale * OpiumOutputShare(w, e.NominalGdp) : 0;
            case PolicyAxis.ImmigrationRestriction:
                // ★移民制限法の政治（点火2026-08-01）：**流入で職を脅かされる大衆が推し、労働力を求める資本が反対する**。
                // ⚠国名も暦年も書かない——読むのは**実際に流入している量**（前四半期の確定値 World.LastImmigration÷人口）と**失業**。
                // ★史実＝米の排華移民法1882・紳士協約1907・1924移民法／豪の白豪主義。いずれも労働者の運動が推し、雇主が抵抗した。
                // ⚠流入がゼロの国では両側とも0＝要求リストに載らない（＝法が争点にならない）＝島国や内陸小国で無意味な要求が出ない。
                if (w.TotalPopulation <= 1e-9) return 0;
                double inflowRate = Math.Max(0, w.LastImmigration) / w.TotalPopulation;
                double pressure = w.Coef.ImmigrationRestrictionPrefScale * inflowRate;
                return mass ? pressure * (1.0 + AvgUnemployment(w)) : modern ? -pressure : 0;
            case PolicyAxis.SpecialistExitBan:
                // ★技術者流出禁止法の政治（段⑤・2026-08-17）。増加方向＝**禁止を強める**。
                // ⚠国名も暦年も書かない——読むのは**実際に抜かれている量**（前四半期の確定値 LastSpecialistExit÷人口）だけ。
                //   抜かれていない国では両側とも0＝要求リストに載らない（＝法が争点にならない）。
                // ★史実＝英の職工移住禁止法（1719 Artificer's Act）＝**製造業者が推した**法である。
                //   ⚠そして1825年に**自由主義が撤廃した**（団結禁止法と同年）。同じ国で賛成が反対に転じた。
                // ★★★ その反転を暦年でなく**構造**で出す：賛否は近代エリートの**内側**で割れる——
                //   ⚠資本（技師を雇う側）は賛成（労働力を他国に競り上げられたくない）。
                //   ★技師・研究者（自分を売る側）は反対（国外で高く売る機会を失う）。
                //   ⚠符号は**ブロック内の富の比**で決まる＝資本が優越する初期は通り、専門職が育つほど撤廃へ傾く。
                //   ★大衆は専門職でないので無関心（OpiumExport の「自国民は中毒にならない」と同じ理由で0）。
                if (w.TotalPopulation <= 1e-9 || !modern) return 0;
                double exitRate = Math.Max(0, w.LastSpecialistExit) / w.TotalPopulation;
                double banPressure = w.Coef.SpecialistExitBanPrefScale * exitRate;
                if (!(banPressure > 0)) return 0;
                double capW = 0, proW = 0;
                foreach (var rr in w.Regions)
                    foreach (var q in rr.Pops)
                    {
                        // ⚠物差しは**富**（規模でなく政治力）。資本家は数が少なく富が大きいので初期は優越する＝史実の順序が出る。
                        double wealth = Math.Max(0, q.Savings.Value);
                        if (q.ClassId == PopClass.Capitalist) capW += wealth;
                        else if (q.ClassId == PopClass.Engineer || q.ClassId == PopClass.Researcher) proW += wealth;
                    }
                double bloc = capW + proW;
                return bloc > 1e-9 ? banPressure * (capW - proW) / bloc : 0;
            case PolicyAxis.MonetaryStandard:
                // ★通貨制度の政治（点火2026-08-01）。増加方向＝**兌換の維持（金本位）**。
                // ⚠読むのは**実際に起きている物価の動き**（ExpectedInflationPerYear＝年率の平滑値）と**その勢力の資産保有**だけ。
                //   金本位そのものが良い/悪いという値は1つも書かない——立場は保有から出る。
                // ★史実＝1873-96年の大デフレは債務者と債権者の政治戦争だった。抵当を抱えた農民には、
                //   物価が下がるとは「同じ借金を返すのに去年より多くの小麦を売る」ことを意味した（ブライアン「黄金の十字架」1896）。
                //   逆にインフレは債権を溶かすので、資産を持つ側が金の規律を要求する（英の銀行・独の1871年金本位移行）。
                // ⚠物価が安定していれば**両側とも0**＝通貨は争点にならない（平時の国で無意味な要求が出ない）。
                // ★銀安の政治（銀本位doc G7・2026-08-13）。⚠「銀安だから移行が安くなる」は採らない——史実は逆である。
                //   ★銀が下がるほど、銀本位国の**輸出業者・農民・債務者は銀に留まりたい**（銀安が有利）。
                //   ⚠金へ移りたいのは**債権者・輸入業者**——彼らにとって銀安は輸入価格の上昇であり債権の目減りである。
                //   ⚠新しい選好の型を作らない：既存の「持てる側／持たざる側」の1行に、銀の下落を**インフレと同じ入力**として足すだけ。
                //   銀本位でなければ silverDrop=0＝この項は消える（銀の段はlive=2026-08-21点火。gate=0に戻せば到達しない＝厳密恒等）。
                //   ★(210) 銀の下落は**同じ州の銀と金の比**（World.SilverGoldRatio＝1 が平価）から読む（00一覧 §0-A の 10・(207) の申し送り①）。
                //   旧＝銀の世界価格（WorldSilverPrice＝表示用の統計 Market.Price[銀]＝「局所価格×為替」の平均＝銀本位国の為替の自己参照）÷参照
                //   ＝r207 では 1803 年から約 0.28（下落 0.72）に張り付いた人工物（同じ時期の同じ州の比は 1.0〜1.1＝銀はむしろ高い）。
                double silverDrop = 0;
                if (w.MonetaryLaw == MonetaryStandardLaw.Silver)
                {
                    silverDrop = PoliticalQuantities.Clamp01(1.0 - w.SilverGoldRatio);
                    if (!(silverDrop >= 0 && silverDrop <= 1)) silverDrop = 0;   // NaNは中立へ倒す（否定形＝Clampは NaN を消さない）
                }
                // ★交易網の引力（タスク#75再開・2026-08-21）：対外に開いた経済の「持てる側」（債権者・大商人）は自国も取引の相手に本位を合わせることを要求する
                //   （決済の摩擦と借入条件＝独1871→1870年代の雪崩の網目）。
                // ★(210) 作者裁定 2026-09-28 §10-11-7「比が平価から外れている間だけ取引の過半の相手へ揃える」（承認した推奨）の形＝
                //   引力＝MonetaryPeerPullScale×|1−比|（比が平価から外れている大きさ）×TradeGoldPull（取引の相手の本位の差×開放度・−1..1）。
                //   比が平価どおり（1）なら金と銀の国の間の為替は動かない＝揃える理由が無い＝0。相手が金の過半なら金へ・銀の過半なら銀に留まる向き（負＝金への一歩に反対）。
                //   旧＝世界の金建て比率×開放度（比を読まない・相手を読まない・正だけ）。銀の国だけ（金の国は自分から降りない＝F6 の行は英だけ・一般化は AI の意図・不換の国の戻りはインフレの項が持つ）。
                //   ⚠ 今の世界では同じ州の比が 0.96〜1.13＝銀の下落も引力もほぼ0（(210) の10種）＝この項は比が動く世界でだけ効く（(212) 反証役 vs1#6）。
                double peerPull = 0;
                if (w.Coef.MonetaryPeerPullScale > 0 && w.MonetaryLaw == MonetaryStandardLaw.Silver)
                {
                    peerPull = w.Coef.MonetaryPeerPullScale * Math.Abs(1.0 - w.SilverGoldRatio) * w.TradeGoldPull;
                    if (!(peerPull >= -w.Coef.MonetaryPeerPullScale && peerPull <= w.Coef.MonetaryPeerPullScale)) peerPull = 0;   // NaNは中立へ倒す（否定形＝ClampはNaNを消さない）
                }
                return w.Coef.MonetaryPrefScale
                     * (e.Wealth[b] * (Math.Max(0, w.ExpectedInflationPerYear.Value) + silverDrop + peerPull)   // 持てる側：インフレ／銀安／交易網の引力→金の規律を要求
                      - (1.0 - e.Wealth[b]) * (Math.Max(0, (-w.ExpectedInflationPerYear).Value) + silverDrop));   // 持たざる側：デフレ→兌換停止を／銀安なら銀に留まることを要求
            case PolicyAxis.CapitalMobility:
                // ★資本移動の政治（器2026-08-13）。増加方向＝**規制**。⚠新しい選好の型を作らず、保有から導出する。
                // ・資本／投資家＝**自由を望む**（海外へ投資でき、海外から資金を呼べる）。反対の強さは**実際の対外保有**
                //   （CapitalAbroad÷名目GDP）から創発＝対外投資を持たない国では反対が立たない＝法が争点にならない。
                // ・地主／労働＝**規制を望む**（資本が国内に留まれば国内の雇用と地価を支える）。
                // ★史実＝金融の中心（英シティ）ほど自由を守り、資本を輸入する側（後発国）は流出局面で統制へ傾いた。
                double abroadShare = e.NominalGdp > 1e-9 ? PoliticalQuantities.Clamp01(w.CapitalAbroad.Value / e.NominalGdp) : 0;
                if (!(abroadShare >= 0 && abroadShare <= 1)) abroadShare = 0;   // NaNは中立へ倒す（否定形で書く＝Clampは NaN を消さない）
                return modern ? -(1.0 + w.Coef.CapitalControlPrefScale * abroadShare) : 1.0;
            case PolicyAxis.Nationalization:  // 国有化（E6）＝利潤を没収される資本（近代エリート）が強く反対・大衆が賛成（社会主義的再分配）。旧支配層は中立（土地貴族＝産業資本ではない）
            case PolicyAxis.NationalizationAll:   // 『全て』も同じ賛否連合（§11-6d）＝軸が別なのは前提DAG（プロレタリア独裁のみ）のためだけ
                return modern ? -NationalizationCapitalOpposition : mass ? 1.0 : 0;
            case PolicyAxis.ColonialSuffrage:      // 植民地参政権（作者裁定2026-08-16）＝賛否連合は参政権の梯子と同型（「既存の参政権軸と同型でよい」）：
                                                   //   旧支配層＝帝国の身分秩序の防衛で反対・大衆＝賛成・近代＝制限選挙まで賛成（それ以上は希釈）＋革命脅威で譲歩。
                goto case PolicyAxis.Suffrage;
            case PolicyAxis.MigrantNativeSuffrage: // 移民原住民参政権（同）＝旧支配層が反対（血統と身分の国民の防衛）・近代エリートが賛成（市民的国民）・
                                                   //   大衆は両義0（連帯と職の競合が相殺＝移民制限法の対立軸〔大衆が制限を推し資本が反対〕と整合・作者裁定「大衆両義でも可」）。
                return trad ? -1.0 : modern ? 1.0 : 0;
            case PolicyAxis.Suffrage:         // 参政権の梯子：自分の包摂までは賛成・希釈は反対
                // 旧支配層＝票でなく権力＝拡大は希釈で反対。近代＝制限選挙まで賛成・以上は梯子外し。大衆＝賛成。
                // ただし**革命脅威下ではエリートが譲歩**（転覆=全てを失うより、参政権を分けて改革で生き延びる＝Reform Acts）。
                // 大衆の闘争度 threat が高いほど trad/modern の反対が緩み賛成へ転じる＝梯子外しが解け参政権が拡大→空間モデルで不満が票へ流れ沈静化。
                double threat = Politics.AvgMassMilitancy(w) * w.Coef.SuffrageConcession;
                return trad ? -1.0 + threat
                     : modern ? (w.Suffrage < Suffrage.Census ? 1.0 : -1.0 + threat)
                     : 1.0;
            default: return 0;
        }
    }

    // 議会（議席）の法への力：党≈ブロック（保守0/自由1/社会2）の選好を議席で加重。富でなく票の声。ParliamentEnabled かつ議会のある政体のみ。
    internal static bool HasLegislature(Polity p) => p is Polity.ConstitutionalMonarchy or Polity.Republic;

    internal static double ParliamentNet(World w, PolicyAxis a, double sign, in Profile e)
    {
        if (!w.ParliamentEnabled || !HasLegislature(w.Polity)) return 0;
        double sum = 0;
        for (int party = 0; party < BlocCount; party++) sum += w.PartySeats[party] * Pref(a, party, e, w, sign > 0) * sign;
        return w.Coef.ParliamentLawWeight * sum;
    }

    /// <summary>その向きの変更に反対するブロックの力の合計（スライダーの律速）。同じ瞬間の <see cref="Build"/> を渡す読み口（<see cref="Profile"/> の注記＝間に何も書かない呼び手だけ）。
    /// ★(149) 反証の指摘：旧の公開の口（呼ぶたびに Build）は SimCore・App・Runner に読み手が無くなった＝削除（テストは Build(w) を渡す）。</summary>
    internal static double OpposingPower(World w, PolicyAxis a, bool increasing, in Profile e)
    {
        double sign = increasing ? 1 : -1;
        double sum = 0;
        for (int b = 0; b < BlocCount; b++)
        {
            double pr = Pref(a, b, e, w, increasing) * sign;
            if (pr < 0) sum += e.Power[b] * -pr;   // 進める向きへの反対の強さ×力
        }
        if (w.ParliamentEnabled && HasLegislature(w.Polity))
            for (int party = 0; party < BlocCount; party++)
            {
                double pr = Pref(a, party, e, w, increasing) * sign;
                if (pr < 0) sum += w.Coef.ParliamentLawWeight * w.PartySeats[party] * -pr;
            }
        double armyPr = ArmyStake(a) * sign;   // 第6段6-4：軍＝第4の勢力（軍事軸の縮小に抵抗）
        if (armyPr < 0) sum += ArmyPower(w) * -armyPr;
        return sum;
    }

    /// <summary>軍需財（<see cref="Coefficients.MilitaryExportGoods"/>）の生産額が名目GDPに占めるシェア（禁制品doc C5・2026-07-30）。
    /// 「武器で食っている国ほど武器商人の政治力が強い」を保有から導く。参照価格建て（市場価格でなく）＝価格の暴騰で政治が揺れない。
    /// 名目GDPが未算出（テスト世界の初期など）なら0＝反対が立たない＝厳密に中立。</summary>
    private static double MilitaryOutputShare(World w, double gdp)
    {
        if (gdp <= 1e-9) return 0;
        double v = 0;
        foreach (var r in w.Regions)
        {
            foreach (int g in w.Coef.MilitaryExportGoods)
                if (g >= 0 && g < r.Markets.Length) v += r.Markets[g].LastProduced * Catalog.RefPrices[g];
        }
        return PoliticalQuantities.Clamp01(v / gdp);
    }

    /// <summary>阿片（<see cref="Coefficients.OpiumGoodId"/>）の生産額が名目GDPに占めるシェア（禁制品doc C3・2026-07-30）。
    /// 阿片で稼ぐ資本の政治力＝「売る側の抵抗」を保有から導く（<see cref="MilitaryOutputShare"/> と同型）。</summary>
    private static double OpiumOutputShare(World w, double gdp)
    {
        int g = w.Coef.OpiumGoodId;
        if (gdp <= 1e-9 || g < 0) return 0;
        double v = 0;
        foreach (var r in w.Regions)
        {
            if (g >= r.Markets.Length) continue;
            v += r.Markets[g].LastProduced * Catalog.RefPrices[g];
        }
        return PoliticalQuantities.Clamp01(v / gdp);
    }

    /// <summary>全国の人口加重アヘン中毒度（禁制品doc C3・2026-07-30）。禁令を求める大衆の圧力＝害の大きさ。</summary>
    private static double NationalOpiumAddiction(World w)
    {
        double add = 0, size = 0;
        foreach (var r in w.Regions)
        {
            double rp = r.SumPopulation();
            add += r.OpiumAddiction * rp; size += rp;
        }
        return size > 1e-9 ? add / size : 0;
    }

    // 〔削除 2026-10-11 (271)〕プチブル利益束（第4束「小営業」＝職人+商人・旧 PettyInterestGate＝既定 0 の枝）と、その専用の読み手
    //   （PettyPref・PettyNet・PettyOpposing・PettyStance・FirmConcentration・ArtisanImportShare）を削除。作者 2026-10-11「全て推奨で」（00-A）で採った推奨＝プチブルの利益束の段階化は作る予定が無い。

    /// <summary>第6段6-4（2026-07-28）統帥権：軍の政策選好＝軍事軸のみ（武力＝軍事費/治安維持力・徴兵法の増加に賛成＝縮小に反対）。他軸は無関心。
    /// ★2026-08-18：親軍法の撤去で軸は2本になったが、その2本こそ軍国度（軍事費×徴兵法）の2因子＝軍が押す先は変わっていない。</summary>
    internal static double ArmyStake(PolicyAxis a)
        => a is PolicyAxis.Coercion or PolicyAxis.ConscriptionLaw ? 1.0 : 0.0;

    /// <summary>第6段6-4（2026-07-28）軍の政治力＝係数×兵士人口シェア×(1−議会統制)。文民統制（ParliamentControl）の確立が
    /// 軍を政治から退場させる＝プロイセン統帥権（議会が予算を握っても軍は王に従う）と英型シビリアンコントロールの差が1式で出る。
    /// ArmyPoliticalWeight=0で旧3勢力＝厳密恒等。</summary>
    internal static double ArmyPower(World w)
        => w.Coef.ArmyPoliticalWeight <= 0 ? 0
         : w.Coef.ArmyPoliticalWeight * (w.SoldierSizeK() / System.Math.Max(1.0, w.TotalPopulation))
           * (1.0 - PoliticalQuantities.Clamp01(w.ParliamentControl));

    /// <summary>UI表示用：その向きの変更への各ブロックの賛否（旧支配層/近代エリート/大衆・概ね−2..+2）＋純支持。正の純支持＝次の一歩が通る。</summary>
    public static (double Trad, double Modern, double Mass, double Net) Stance(World w, PolicyAxis a, bool increasing)
    {
        var e = Build(w);
        double sign = increasing ? 1 : -1;
        return (Pref(a, 0, e, w, increasing) * sign, Pref(a, 1, e, w, increasing) * sign, Pref(a, 2, e, w, increasing) * sign, NetSupport(w, a, increasing, e));   // ★同じ瞬間の e を渡す（(146)＝旧は純支持の中で Build をもう1回）
    }

    // SupportRate は第3段3-3（2026-07-27）で削除＝呼び出し元ゼロ・Build(w) の4つ目の重複実行を抱えていた死蔵（改革支持の表示は法律タブの段階ボタン/StageHintへ移行済み）。

    /// <summary>★誰がどの程度その一歩を望んでいるか（2026-08-02・作者指示「誰がどの程度望んでいるかも知れるべき」）。
    /// 正＝押している／負＝抵抗している。<see cref="NetSupport"/> と**同じ項**を勢力ごとに分けて返すだけ＝合計は一致する。
    /// ⚠副作用なし・表示専用（法律タブのホバー）。</summary>
    public static (string Who, double Weight)[] SupportBreakdown(World w, PolicyAxis a, bool increasing)
    {
        var e = Build(w);
        double sign = increasing ? 1 : -1;
        string[] blocNames = ["旧支配層", "近代エリート", "大衆"];
        var list = new List<(string, double)>(BlocCount + 3);
        double streetB = w.Coef.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && HasLegislature(w.Polity)
            ? 1.0 - PoliticalQuantities.Clamp01(w.Coef.ParliamentStreetSupersede) : 1.0;   // ★NetSupport と同じ譲渡率＝合計一致の維持
        for (int b = 0; b < BlocCount; b++) list.Add((blocNames[b], streetB * e.Power[b] * Pref(a, b, e, w, increasing) * sign));
        // ★党別に分解（2026-08-04・作者指摘「POPの誘導が困難で、議会に影響を与えるのが難しい」）。
        //   旧＝「議会 反対 42」の1行だけで、**どの党が反対しているか**が出ず、誰を増やせば通るのかが読めなかった。
        //   党＝族（保守0/自由1/社会2）で、族の議席は支持基盤（政治タブ）の階級×思想から積み上がる
        //   ＝この行を見て「自由党を増やせばよい→どの階級が自由に傾いているか→その階級の識字/意識を上げる」まで一本で辿れる。
        //   ⚠合計は ParliamentNet と**厳密に一致**（同じ項を分けて足すだけ）＝NetSupport の判定には一切触れない表示のみの変更。
        if (w.ParliamentEnabled && HasLegislature(w.Polity))
        {
            string[] partyNames = ["議会・保守党", "議会・自由党", "議会・社会党"];
            for (int party = 0; party < BlocCount; party++)
            {
                double v = w.Coef.ParliamentLawWeight * w.PartySeats[party] * Pref(a, party, e, w, increasing) * sign;
                if (System.Math.Abs(v) > 1e-9) list.Add((partyNames[party], v));
            }
        }
        double army = ArmyPower(w) * ArmyStake(a) * sign;
        if (System.Math.Abs(army) > 1e-9) list.Add(("軍", army));
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax)
            list.Add(("国庫", e.StateRevenue * sign));
        list.Sort((x, y) => y.Item2.CompareTo(x.Item2));   // 賛成が強い順（同値は安定＝List.Sortは不安定だが値が同じなら表示上等価）
        return [.. list];
    }

    /// <summary>その向きの変更への純支持（賛成勢力−反対勢力）。正なら法整備が一歩通る／スライダーが動く。</summary>
    public static double NetSupport(World w, PolicyAxis a, bool increasing) => NetSupport(w, a, increasing, Build(w));

    /// <summary>同じ瞬間の <see cref="Build"/> を渡す読み口（<see cref="Profile"/> の注記）。</summary>
    internal static double NetSupport(World w, PolicyAxis a, bool increasing, in Profile e)
    {
        double sign = increasing ? 1 : -1;
        double sum = 0;
        // ★議会の二重票の是正（ParliamentStreetSupersede・2026-08-31）：党の選好表＝ブロックの選好表なので、
        //   議会がある国では同じ選好が街頭力と議席で二重加算されていた（清レポート実測＝共和国化で教育法が−0.98→−2.67）。
        //   代表された3ブロックの街頭力をこの割合だけ議席チャネルへ譲る＝影響力の置き場所が移るだけ（未代表の小営業・軍・国庫は街頭のまま）。0＝旧＝厳密恒等
        double street = w.Coef.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && HasLegislature(w.Polity)
            ? 1.0 - PoliticalQuantities.Clamp01(w.Coef.ParliamentStreetSupersede) : 1.0;
        for (int b = 0; b < BlocCount; b++) sum += street * e.Power[b] * Pref(a, b, e, w, increasing) * sign;
        sum += ParliamentNet(w, a, sign, e);
        // 政12：国家の歳入要求を**独立主体として**全税軸へ直接加算（ブロック力で乗算しない＝専売の in-Pref 方式の奇妙なスケーリングを避ける）。
        // 所得税・人頭税・土地税も財政逼迫で押せる（戦時所得税＝英1799/米1861）。旗 StateRevenueAllTaxes（0＝専売のみ）は 2026-09-26 に焼き込み。
        // 専売は既に Pref 経由で StateRevenue を持つので二重計上を避け対象外。
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax)
            sum += e.StateRevenue * sign;
        sum += ArmyPower(w) * ArmyStake(a) * sign;   // 第6段6-4：軍＝第4の勢力（軍事軸の増強を推す＝統帥権。ArmyPoliticalWeight=0で0＝恒等）
        return sum;
    }

    /// <summary>★法の隔たり∈[0,1]（不満度の導出量・二量分離 段1・2026-09-09）＝ブロックごとに1つ（返り値の添字＝<see cref="BlocOf"/>）。
    ///
    /// ★定義＝**選好で重みづけた「余地」の平均**＝Σ|選好| × 望む向きの残り可動域 ÷ Σ|選好|。
    ///   ・望む向き＝ComputeDemands と**同じ規則**（選好の符号）／余地＝<see cref="FactionSnapshot.Headroom"/> と**同じ1本**（AxisPosition と対）。
    ///   ・望む法が全て与えられていれば（望む向きに余地が無ければ）0＝隔たり無し。何も与えられていなければ1。
    ///   ⚠[0,1] は**構造で保証**される（重み平均・各項が[0,1]）＝新しい定数も閾値も置かない。
    ///   ⚠何も望まないブロック（Σ|選好|≈0）は0＝「不満の理由が無い」＝ゼロ除算も作らない。
    ///
    /// ⚠旧経路（4軸座標の距離）との違い＝**現に動かせる法**を見る。座標の距離は「体制の型」への距離で、
    ///   その体制の下で望む法が既に全部通っていても縮まなかった（英1867以降の労働者が満額の不満を持ち続ける形）。
    ///
    /// ★『全て国有化』専用軸は除く＝<see cref="FactionSnapshot.ComputeDemands"/> と同じ除外規則（Nationalization と選好が同一＝同文の二重で重みが倍になる）。
    /// ⚠コスト＝Build(w) は**1回だけ**（軸ごとに Stance を呼ぶと軸数×2回になる）。呼び手は pop ループの外で1回取ること。
    /// ⚠キャッシュを作らない（World に置いた瞬間、助走中や月次の読み手に対して古い値を持つ導出量＝それは記憶である）。</summary>
    internal static double[] LawGap(World w)
    {
        var e = Build(w);
        double[] gap = new double[BlocCount], wsum = new double[BlocCount];
        foreach (var a in FactionSnapshot.Axes)
        {
            if (a == PolicyAxis.NationalizationAll) continue;
            for (int b = 0; b < BlocCount; b++)
            {
                double pref = Pref(a, b, e, w);            // 「進める向き」への選好（+賛成/−反対）
                double s = System.Math.Abs(pref);
                if (!(s > 0)) continue;                     // 否定形＝NaN も除外（有限値では s<=0 と同値）。無関心の軸は重みを持たない
                wsum[b] += s;
                gap[b] += s * FactionSnapshot.Headroom(w, a, pref >= 0);   // 望む向きの残り可動域（ComputeDemands と同じ向きの決め方）
            }
        }
        for (int b = 0; b < BlocCount; b++)
        {
            double v = wsum[b] > 1e-9 ? gap[b] / wsum[b] : 0;
            gap[b] = PoliticalQuantities.Clamp01(v);   // 否定形＝NaN も 0 へ（Math.Clamp は NaN を通す＝「∈[0,1]」の名乗りが嘘になる）
        }
        return gap;
    }

    // ---------------------------------------------------------------- (558) 線 rs3：減らす向きの旧支配層の強さ（農奴解放・土地改革） ----------------------------------------------------------------
    // rs2 の損の式は進める向きだけを読む。減らす向きは〈1 段戻したときに得る所得の割合〉で読む（(803) で旗 LordOppositionReverseGain を焼き込んだ）。
    // 新しい関数はこのファイルの末尾に置く（gc2 が 181〜215 行を触る＝重ならない）。産地の走査は CustomaryRentShare の 1 回（新しい和・状態・倍率は 0）。

    /// <summary>旧支配層の農奴解放・土地改革への強さの中身（<c>0.5＋これ</c> に StatusScale を掛ける）。進める向きは損・減らす向き（<paramref name="increasing"/>＝false）は得。</summary>
    private static double LordStake(World w, PolicyAxis a, in Profile e, bool increasing)
        => !increasing ? LordIncomeGain(w, a, e.LordRentToIncome) : LordIncomeLoss(w, a, e.LordRentToIncome);

    /// <summary>この選好（軸×ブロック）の減らす向きの強さが、進める向きの符号反転と違う読みを持つか（<see cref="Pref"/> の <c>increasing=false</c> を別に呼ぶ価値があるか）。
    /// 軸が農奴解放か土地改革・ブロックが旧支配層（0）のとき true。★(1048)：官僚制度は旗 LordIntermediaryStake＞0・国教は旗 LordReligionStake＞0 のときも true。</summary>
    internal static bool DirectionAware(World w, PolicyAxis a, int b)
        => b == 0 && (a == PolicyAxis.SerfEmancipation || a == PolicyAxis.LandReform
                      || (a == PolicyAxis.Bureaucracy && w.Coef.LordIntermediaryStake > 0) || (a == PolicyAxis.StateReligion && w.Coef.LordReligionStake > 0));

    /// <summary>★(558) 線 rs3：<paramref name="axis"/> を 1 段戻したときに旧支配層が得る所得の割合＝
    /// (戻した段の実効の取り分−今の実効の取り分)÷今の実効の取り分 × <paramref name="lordRentToIncome"/>（損 <see cref="LordIncomeLoss"/> と分母の規則が同じ＝その動きの前の状態＝今で割る）。
    /// 戻した段＝その軸だけ 1 段戻す（なし の前は なし＝得 0）。戻した段の上限は今以上＝得は 0 以上。</summary>
    internal static double LordIncomeGain(World w, PolicyAxis axis, double lordRentToIncome)
    {
        EffectiveRentShares(w, axis, out double now, out double prev, back: true);
        return now > 1e-9 ? (prev - now) / now * lordRentToIncome : 0.0;
    }

    // ---------------------------------------------------------------- (1048) c99db：地主の政治 D-1b（国教）と D-2（官僚制度・土地改革の仲介の取り分） ----------------------------------------------------------------
    // 型は LordStake と同じ〈次の一段で失う所得の割合〉（減らす向きは 1 段戻したときの得・分母はその動きの前＝今の度合い）。新しい係数・閾値・状態は 0。

    /// <summary>D-2：<paramref name="a"/>（官僚制度か土地改革）の次の一段（<paramref name="increasing"/>＝false なら 1 段戻す）で動く仲介者の度合い I の割合×<see cref="Profile.CutRhoToIncome0"/>。
    /// I＝<see cref="Privileges.IntermediaryDegreeFor"/>（官僚制度と土地改革の大きい方）＝その軸の一段で I が動かなければ 0（max の性質＝2 本の軸が両方動いて初めて仲介者が損をする）。I＝0 なら 0。</summary>
    private static double IntermediaryStake(World w, PolicyAxis a, in Profile e, bool increasing)
    {
        var b = w.Bureaucracy; var lr = w.LandReform;
        double now = Privileges.IntermediaryDegreeFor(b, lr);
        double other = a == PolicyAxis.Bureaucracy ? Privileges.IntermediaryDegreeFor(BureaucracyStep(b, !increasing), lr) : Privileges.IntermediaryDegreeFor(b, Step(lr, !increasing));
        return now > 1e-9 ? e.CutRhoToIncome0 * System.Math.Abs(now - other) / now : 0.0;
    }

    /// <summary>D-1b：国教の次の一段（<paramref name="increasing"/>＝false なら 1 段戻す）で動く十分の一税の度合いの割合×<see cref="Profile.ClergyToIncome0"/>。
    /// 度合い＝<see cref="Privileges.TitheDegreeFor"/>（十分の一税の徴収と同じ 1 本）。今の度合いが 0（国家無神論・十分の一税の無い世界）なら 0。</summary>
    private static double ReligionStake(World w, in Profile e, bool increasing)
    {
        var s = w.StateReligion; var c = w.Coef;
        double now = Privileges.TitheDegreeFor(s, c), other = Privileges.TitheDegreeFor(ReligionStep(s, !increasing), c);
        return now > 1e-9 ? e.ClergyToIncome0 * System.Math.Abs(now - other) / now : 0.0;
    }

    /// <summary>官僚制度を 1 段進める（猟官→資格任用→専門・専門の次は専門）か、1 段戻す（猟官の前は猟官）。</summary>
    private static BureaucracyLaw BureaucracyStep(BureaucracyLaw b, bool back)
        => back ? (b == BureaucracyLaw.Spoils ? b : b - 1) : (b == BureaucracyLaw.Professional ? b : b + 1);

    /// <summary>国教を 1 段進める（国教→内心の自由→信仰の自由・信仰の自由は法の梯子の上限 2 で止まる）か、1 段戻す（信仰の自由→内心の自由→国教）。国家無神論はどちらにも動かない（政体の属性）。</summary>
    private static StateReligionLaw ReligionStep(StateReligionLaw s, bool back)
        => s == StateReligionLaw.StateAtheism ? s
         : back ? (s == StateReligionLaw.StateChurch ? s : s - 1) : (s == StateReligionLaw.ReligiousFreedom ? s : s + 1);

}
