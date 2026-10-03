namespace PriceOfBread.SimCore;

/// <summary>
/// 国力（列強の総合指数）＝人口（規模）＋軍隊（軍事力）＋豊かさ（経済規模・近代産業・生活水準・植民地）。
/// 列強ランキングの基準＝勝利条件。大国（人口・軍・富）が上位に立つ史実的な序列を表す。重みは調整可。
/// </summary>
public static class NationalScore
{
    // マジックナンバー命名（挙動不変・値据え置き）：スコア合成の重み/スケール/上限。
    private const double MilitaryDiminishingCap = 12.0;   // 軍事項の逓減キャップ（Runner の外交の観測 dpdump も Military() を読む＝旧 DiploCore.MilitaryDiminished の写経 12.0／2.0 は 2026-09-28 に削除）
    private const double GdpScaleDivisor = 12000.0;       // 経済規模の基準スケール
    private const double GdpDiminishingDivisor = 12.0;    // 経済規模の限界逓減
    private const double IndustrializationWeight = 9.0;   // 近代産業の重み
    private const double SatisfactionWeight = 12.0;       // 生活水準の重み
    private const double ColonyWeight = 1.5;              // 植民地の重み
    private const double FinancePerCapitaDivisor = 150.0; // 1人あたり与信の基準。★50→150（2026-08-04）：scorebreak実測で**全53カ国が上限6.0に張り付き**（ラオスまで満点）＝軸が死んで差別化ゼロだった。信用が普遍化した世界では基準を上げないと「深さ」を測れない（ロンドンのシティ＝1人あたり与信の厚みが再び差になる）
    private const double FinanceCap = 6.0;                // 金融項の上限

    public static double Of(World w) => Military(w) + Wealth(w) + Finance(w) + Tech(w);

    /// <summary>序列に使う国力（2026-08-02）。★フローでなく**ストック**を返す。まだ種が無ければ現在値を返す
    /// （初期化前でも順位が壊れない）。更新は StepStanding が四半期に1度だけ行う。</summary>
    public static double Standing(World w) => double.IsNaN(w.ScoreStanding) ? Of(w) : w.ScoreStanding;

    /// <summary>国力の在庫を1四半期ぶん現在値へ寄せる（2026-08-02）。上がるのも落ちるのも同じ速さで遅い。
    /// ★rate=1 で在庫＝現在値＝厳密恒等（休眠）。決定論（新規 RNG draw なし）。</summary>
    public static void StepStanding(World w)
    {
        double now = Of(w);
        if (double.IsNaN(w.ScoreStanding)) { w.ScoreStanding = now; return; }   // 開始時の種＝そのときの実力
        double r = PoliticalQuantities.Clamp01(w.Coef.ScoreInertiaRate);
        w.ScoreStanding += (now - w.ScoreStanding) * r;
    }
    // 規模偏重の是正（ユーザ指定）：GDP（＝人口×生産＝規模に比例）と軍（兵数＝人口に比例）の比重を下げ、
    // 1人あたりの発展度（技術・工業化・生活水準・金融の深さ）の比重を上げる＝大人口だけで上位に立てないようにする。
    // 改修2026-06-28：技術項を追加し、軍を逓減キャップ・GDP逓減を強化＝**19世紀の欧州の技術優位を国力評価に反映**（清の人口361M一強の是正・ユーザー指示「技術力で差がでるように」）。

    // 人口点（Population）はスコア合成Ofに入っておらず読者ゼロだったため削除（監査2026-07-12 死13）。人口の国力寄与はGDP項（人口を内包）と軍項（兵数）が担う設計のまま。
    // 軍隊（兵数＝人口に比例＝規模偏重の主因）。比重↓(3→2)＋強い逓減キャップ(cap≈12)＝巨大人口の大軍が国力を独占しない（技術の質は Tech 項＋TechMilitaryQuality が別途表す）。改修2026-06-28：清の大軍(43)が技術乗数で膨らむのを cap で吸収。
    public static double Military(World w) { double m = (w.SelfPower?.Military ?? 0) * 2.0; return m / (1 + m / MilitaryDiminishingCap); }
    /// <summary>豊かさ＝4項の左結合和（<see cref="WealthParts"/> と同順＝計器が内訳を出しても和は厳密一致）。</summary>
    public static double Wealth(World w) { var (gdp, ind, sat, col) = WealthParts(w); return gdp + ind + sat + col; }
    /// <summary>豊かさの内訳＝正典が内訳を公開する（Runner の国力内訳計器が `/12000`・`×9`・`×12` を書き写していた重複を畳む・2026-09-11 #15）。</summary>
    internal static (double Gdp, double Ind, double Sat, double Col) WealthParts(World w)
    {
        double gdpScale = w.Gdp / GdpScaleDivisor;          // 経済規模（人口を内包）。24000→12000＝大国（仏・墺・日）の経済規模を「大国」らしく評価
        // #17 規模の限界逓減：巨大経済(清＝人口361M)が線形で暴走し首位を固定する（1799–1914ずっと清が1位）のを抑える。大経済は高く評価しつつ上限へ漸近させ、トップを「争える」状態に。
        // 改修2026-06-28：逓減を強化(20→12)＝清の規模暴走をさらに抑え、欧州の技術/発展が上回れるように。⚠ NationalScore は測定だけの物差しではない＝毎四半期の外交のランク（侵略の標的・植民地化・介入）と条約港の失効も読む＝重みの調整は AI の標的も動かす（2026-09-26 訂正・棚卸し fis-25）。
        gdpScale /= 1 + gdpScale / GdpDiminishingDivisor;
        return (gdpScale,
            w.MeanIndustrialization * IndustrializationWeight,        // 近代産業（1人あたりの発展度＝非規模）。比重↑ 8→9。改修2026-06-28：首都州のみ→全州人口加重平均（多州国の脱工業化誤認を解消）
            // 生活水準（1人あたりの豊かさ＝非規模）。★2026-09-22：満足度（階層3分割で食料が抜けた＝不満の台帳）から**補給の充足**（食料＋日用品）へ付け替え。
            //   理由＝国力の生活水準は実質賃金の物差し（主食が支出の大半を占める基礎バスケットに対する購買力）＝食料を抜くと物差しが別物になる。
            //   実測（隔離計器 tiersatprobe・seed1-3×604Q）：分割後の満足度のまま読むと国間の標準偏差が 0.082→0.169（×12 で 0.99→2.02 点）へ倍増し、
            //   列強を決める計器の中で生活水準の重みだけが黙って倍になる。補給の充足は分割前の満足度と同じ量＝重み12の較正がそのまま意味を持つ。
            MeanSupplyFill(w) * SatisfactionWeight,
            // ★2026-08-28：植民地項を**個数→質量**へ（ColoniesAreRegions>0 のみ・0では従来の個数＝厳密恒等）。
            //   個数は州追加の細分化で「行を分けただけで増える」＝スペイン常勝22/30の直接の原因だった（単位の非対称）。
            //   質量＝元の Colony.Population 単位（細分化で不変）。旧世界の英植民地8個≒質量8前後＝スケールも概ね保存。
            (w.Coef.ColoniesAreRegions > 0 ? (w.SelfPower?.ColonyMassUnits(w.Coef.ColonialPopScale) ?? 0) : (w.SelfPower?.Colonies ?? 0)) * ColonyWeight);  // 植民地（帝国の富）
    }
    // 技術（改修2026-06-28・ユーザー指示「技術力で差がでるように」）：技術水準＝1人あたりの発展の柱。欧州の技術優位を国力評価へ直接反映＝大人口だけでは上位に立てない。
    public static double Tech(World w) => w.Coef.ScoreTechWeight * Math.Max(0, w.TechLevel);
    // 金融の発達＝1人あたりの与信（信用残高÷人口）。近代的な金融経済の成熟度＝規模でなく「深さ」の指標（ロンドンのシティ的国力）。
    // 人口で割る＝反規模：工業化した英は1人あたり信用が深く高得点、大人口・農業の露は薄く低得点（GDP/GDP比だと全国が上限に張り付き差が出ないため人口あたりに）。
    public static double Finance(World w) =>
        w.Bank is { } b && w.TotalPopulation > 1 ? Math.Clamp(b.Loans / (w.TotalPopulation * FinancePerCapitaDivisor), 0, FinanceCap) : 0;   // 上限12→6＝ロンドンのシティ（金融）一強を緩和（金融国力は残すが青天井にしない）

    // 第5段5-1（2026-07-27）：これが唯一の実装（Politics.StepのMeanSatisfactionPol代入・Credit の avgSat・Validation の満足度もここを呼ぶ）。
    // ★2026-09-25：分母を**大衆のみ**（Politics.IsMassPop＝不満度・闘争度と同じ母集団）へ揃えた。旧＝全POP（閉鎖階級・近代エリート・兵士を含む）
    //   ＝満足度だけが別の母集団を見ていた（分子と分母の非対称）。述語は呼ぶだけ（式を二度書かない）。水準の門は分位の写像で導き直した（各係数の注記）。
    public static double MeanSatisfaction(World w)
    {
        double s = 0, n = 0;
        foreach (var p in w.AllPops()) if (Politics.IsMassPop(p)) { s += p.Satisfaction * p.Size; n += p.Size; }
        return n > 0 ? s / n : 0;
    }

    /// <summary>国の補給の充足（Pop.SupplyFill の人口加重平均＝食料＋日用品）。読み手は国力の生活水準項だけ（2026-09-22）。
    /// 分母は**全POP**（国民1人あたりの物差し）＝2026-09-25 に MeanSatisfaction を大衆へ絞った後も据え置き（大衆へ絞っても国間の標準偏差は 0.0807→0.0806＝重み12の較正に影響しない）。</summary>
    public static double MeanSupplyFill(World w)
    {
        double s = 0, n = 0;
        foreach (var p in w.AllPops()) { s += p.SupplyFill * p.Size; n += p.Size; }
        return n > 0 ? s / n : 0;
    }
}

/// <summary>AIの教義（国の性格＝戦略の偏り）。均質化を避け、国ごとに異なる道を歩ませる。</summary>
public enum Doctrine { Balanced, Militarist, Industrialist, Modernizer }

/// <summary>
/// 効用AI（§8 同一インターフェース）：政策スタンス（候補手）を国の状況に対する効用で点数化し、最良を選んで適用する。
/// 効用＝そのスタンスが助けるスコア成分 × その不足（伸びしろ）。固定戦略でなく状況適応＝対戦の駆動。
/// </summary>
public sealed class UtilityAgent : INationAgent
{
    // マジックナンバー命名（挙動不変・値据え置き）。※既存の c.Xxx 参照値・per-stance の政策設定値・自明値・εは対象外。
    private const double IndustrializationCeiling = 2.5;        // 後発工業化の設計包絡（投資を絞る目標上限）
    private const double MilitaristBaseUtility = 2.5;           // 軍国教義の軍拡ベース効用
    private const double MilitaristEscapeThreshold = 0.5;       // 軍国の脱出弁：この国庫逼迫度からベース効用を減衰し始める（幅も同値）
    private const double IndustrializationRoomTarget = 4.0;     // 工業化余地の到達目標（★鋼市場診断#1 2026-08-16：AiIndustrialCeilingFrontier はここに触れない＝スタンス選択の目標は投資の物理上限と別判断・記録のみ）
    // 不満＝満足がこれを下回る分。★2026-09-22 分位の写像で導き直し（階層3分割で満足度の水準が構造的に下がったため）：
    //   比べる相手＝全国×四半期の NationalScore.MeanSatisfaction（隔離計器 tiersatprobe・seed1-3×604Q）。旧0.7は旧分布の下から53.3%→ 新分布の53.3%分位
    //   ＝1回目0.53→2回目0.61（1回目の値で再測定すると分位が44.8%へ8.5pt ずれた＝帰還。写像は2回で打ち切り）。
    // ★2026-09-25 MeanSatisfaction の分母を大衆のみ（Politics.IsMassPop）へ揃えたので同じ手順で導き直し（隔離計器 satqmap・seed1-3×668Q・全国×四半期）：
    //   旧0.61は旧（全POP）分布の下から51.89% → 新（大衆）分布の51.89%分位＝1回目0.6210 → 2回目0.6167 → 0.62。
    private const double UnrestSatisfactionThreshold = 0.62;
    private const double UModUnrestWeight = 0.3;                // 近代化効用：不満の重み
    private const double UIndRoomWeight = 1.2;                  // 工業化効用：余地の重み
    private const double UModTechWeight = 1.0;                  // 近代化効用：技術差の重み
    private const double UModIndRoomWeight = 0.5;               // 近代化効用：工業化余地の重み
    private const double UIndTechWeight = 0.5;                  // 工業化効用：技術差の重み
    // ★2026-08-04（作者指示「不満度の社会保障への圧力を高め、より満足度を落ちづらくする」）：1.5→2.5。
    //   旧値では不満の理論最大（満足0＝unrest0.7）でも効用1.05＝工業化効用（≈4.0）に構造的に勝てず、
    //   福祉スタンスは飢饉か革命間際でしか選ばれなかった。それでも早期は工業化が勝つ（史実＝福祉国家は工業化の後）。
    private const double UWelUnrestWeight = 2.5;                // 福祉効用：不満の重み
    private const double UWelFamineWeight = 2.0;                // 福祉効用：飢饉の重み
    // 不満→社会保障の**直接の床**（スタンスの勝者に関係なく掛かる＝ビスマルク反射「社会保険は社会主義への防波堤」）。
    // 床＝不満/閾値（0..1）。施行は従来の政治ゲート（法の梯子・1期1段）を通るので即時ではない＝目標だけが上がる。
    private const double UnrestSSPressure = 1.0;
    private const double UFisWeight = 1.2;                      // 緊縮効用：国庫逼迫の重み
    private const double ColonyDefenseWeight = 0.3;             // 植民地防衛の軍拡効用
    // 工場/プランテーションの着工（2026-08-04）。平時の規律＝借入と同じ財政逼迫の上限。
    // ★2026-08-04の再是正：0.5（＝バッファの半分まで減っていても着工）では、工事の日次出費が国庫を吸い上げ、
    //   福祉・行政に回る金が細って満足度が落ち、政党支配×低満足の経路で**軍事独裁が5%→34%**まで増えた（実測R16/R17）。
    //   工場は「余裕のある国が建てるもの」＝国庫が財政バッファを**満たしている**時だけに絞る（0＝逼迫ゼロ）。
    private const double FacilityBuildStressCeiling = 0.0;
    private const double FacilityBuildMinMargin = 0.05;   // 実質相対価格の超過がこれ以下なら建てない（不足していない物を建てない・2026-08-04にボトルネック駆動へ転用）
    private const double FacilityBuildIdleWeight = 2.0;   // 失業の重み＝同じ採算なら人の余っている州を選ぶ（作者のUI要求と同じ判断材料）
    private const double FacilityBuildOutlayShare = 0.10;
    private const double FacilityBuildCostCap = 1.0;   // 建てられる工場の総費用の上限＝財政バッファ比（規模中立＝小国は軽工業から・2026-08-04）   // 工事の月次出費が財政バッファのこの割合を超えたら新規着工を止める（同時工事の暴走を防ぐ・規模中立）
    // ★鋼市場診断#4（2026-08-16・Coefficients.AiSupplyChainEyes>0 のときだけ読む）：
    private const double AiChainStarvedFill = 0.9;   // 投入の配給率（Market.LastRation＝生産タブの「供給:n%」と同じ出所）がこれ未満＝その材が連鎖の律速（上流を候補に足す）
    // ★鋼市場診断#5（2026-08-16・Coefficients.AiProtectiveTariff>0 のときだけ読む）判定線：
    private const double AiTariffEmaRate = 0.25;          // 輸入EMAの追従率（四半期0.25＝約1年の記憶＝「前年比」の最小実装。新しい状態はAI個体の _importEma 1本だけ）
    private const double AiTariffSurgeRatio = 1.5;        // 対EMA比これ超＝輸入急増（1870年代の穀物侵入・英国鋼の輸出攻勢級の流入）
    private const double AiTariffImportFloorShare = 0.001; // 急増と数える最小規模＝輸入額（清算単位＝月次清算では直近1か月分・NominalGdp も月次フロー）>名目GDP×これ（規模中立＝微小財の雑音で関税を動かさない）
    private const double TensionNormalizer = 100.0;            // 緊張の正規化スケール
    private const double TensionUtilityWeight = 2.0;           // スタンス効用：緊張項の重み
    private const double SurvivalRatioCap = 3.0;                // 生存効用：脅威比超過分のクランプ上限
    private const double IndustrialistIndBias = 1.6;          // 資本主義教義：工業化効用の増幅
    private const double IndustrialistModBias = 1.2;          // 資本主義教義：近代化効用の増幅
    private const double ModernizerModBias = 1.5;            // 近代化教義：近代化効用の増幅
    private const double ModernizerWelBias = 1.2;           // 近代化教義：福祉効用の増幅
    private const double CompulsoryEduTechThreshold = 1.0;   // 義務教育の技術閾値
    private const double ProfessionalBureaucracyAdminThreshold = 0.6;   // 02断線走査2026-07-28：専門官僚制を目標に置く行政効率
    private const double AntitrustIndThreshold = 0.5;                   // 02断線走査2026-07-28：談合禁止を目標に置く工業化度
    private const double ForceUnlockBufferMult = 3.0;                   // 02断線走査2026-07-28：再征服をONにする国庫の余裕（月次義務バッファの倍数）
    private const double ForceUnlockMilitancyCeiling = 0.4;             // 02断線走査2026-07-28：この闘争度以上では制圧を強行しない（全pop闘争度↑が革命を招く）
    private const double TrustbustIndThreshold = 0.75;                  // 02断線走査2026-07-28：独占解体（シャーマン法級）を目標に置く工業化度
    private const double CompulsoryEduBufferMult = 3.0;     // 義務教育の財政バッファ倍率
    private const double WomenPropertyTechThreshold = 1.2; // 女性の権利＝財産権の段を目標に置く技術閾値
    private const double WomenSuffrageTechThreshold = 1.5;// 女性の権利＝選挙権の段を目標に置く技術閾値（財産権より後）
    private const double CensusSuffrageTechThreshold = 0.25;// 制限選挙の技術閾値
    private const double IndustrializeInvestFrac = 0.15;   // 産業スタンスの近代化投資率
    private const double ModernizeInvestFrac = 0.08;       // 近代化スタンスの近代化投資率
    /// <summary>開始時に**全国が共有する**近代化投資率（プレイヤーとAIの出発点を揃える）。
    /// ⚠★2026-08-02：<c>World.ModernizationFrac</c> は既定0で、**AIだけが毎ステップ自分で設定していた**。
    ///   プレイヤー国は NullAgent なので0のまま＝国家による工業化が一度も起きない。実測（indprobe 24 0 日本）＝
    ///   Q24の国家累積が 普3.047・仏2.096・両シチリア2.179・英0.943 に対し**日本（プレイヤー）0.000**、
    ///   工業化度は 0.136 対 1.900 の**14倍差**。民間累積はむしろプレイヤーの方が多い（0.180 対 0.066）ので、
    ///   差は丸ごとこの1本のレバーから来ていた＝作者報告「プレイヤーの国家の数値が非常に低くなる」。
    /// ★機構は元から対称（同じ <c>ModernizationFrac</c> をUIもAIも書く）＝**壊れていたのは初期値だけ**。
    ///   2つのうち控えめな近代化スタンスの率を共有の出発点に採る。
    ///   AIは次のステップで自分の判断に上書きするので、この値が効くのは実質プレイヤー国だけ＝較正に無影響。
    /// ⚠★2026-08-10 の是正まで、この段落には「プレイヤーは財政タブのスライダーで上げ下げできる」と書いてあったが**実装と食い違っていた**：
    ///   スライダーは XAML から撤去済みで、さらに <c>Simulation.QuarterStart</c> が**プレイヤー国を含む全国**に毎四半期
    ///   <see cref="AutoModernizationFrac"/> を代入していた＝仮にUIから動かしても次の四半期に必ずこの値へ戻る二重の封鎖だった。
    ///   現在は（a）財政タブにスライダーを復活、（b）QuarterStart は <c>Power.IsPlayer</c> の国を上書きしない、の2点で記述どおりになっている。
    /// ⚠なお裁量は依然として非対称：ここ（プレイヤーの出発点）は控えめな 0.08 だが、産業スタンスのAIは
    ///   <see cref="IndustrializeInvestFrac"/>=0.15 まで採る。プレイヤーはスライダーで 0..1 を自分で選べる＝**上限側で対称性が回復**する
    ///   （AIが賢く踏む率を、プレイヤーは手で踏める）。</summary>
    public const double OpeningModernizationFrac = ModernizeInvestFrac;
    private const double CornUrbanRepeal = 0.35;     // 都市化がこれ超なら撤廃へ（都市が安いパンを要求＝反穀物法同盟）
    private const double CornAgrarianKeep = 0.15;    // 都市化がこれ未満なら維持へ（地主が支配的な農業国）
    private const double EmancipationRentRoom = 0.05;// 地代の取り分が上限をこれ超で上回っていれば解放に余地がある

    /// <summary>★法の目標を**1段だけ**動かす（3法のAI・2026-08-02）。
    /// ⚠★置く前に <see cref="Government.GateInfo"/> を確認する＝**幽霊戦線の防壁**。
    ///   通らない目標を置くだけで <c>CountOpenFronts</c> に毎期1本として数えられ続け、
    ///   <c>PoliticalPushBudget=(1/N)^0.5</c> が**全軸の押しを一律に薄める**。UI経路はボタンが無効化されるので防げるが、
    ///   AI経路には防壁が無かった。新しい判定ではなく、UIが既に使っている関数を呼ぶだけ。</summary>
    private static void StepLawToward(World w, NationPolicy p, PolicyAxis axis, int cur, int want, int max, System.Action<int> set)
    {
        // ★上限は必ず受け取ってクランプする（2026-08-02のバグ修正）：初版は want を無制限に受け、
        //   呼び手が cur+1 を渡していたため目標が四半期ごとに際限なく増え、604期で LandReform=601 という
        //   enum の定義外の値になっていた（法の多様性表に「6017 %」等の数値が並んで発覚）。
        //   下流は (int)w.LandReform / 2.0 のように序数を割合として読むので、定義外の値は静かに全部を汚す。
        want = System.Math.Clamp(want, 0, max);
        if (want == cur) return;
        int next = System.Math.Clamp(cur + (want > cur ? 1 : -1), 0, max);
        if (next == cur) return;
        var (ok, _, _) = Government.GateInfo(w, axis, want > cur, next);
        if (!ok) return;   // 通らない目標は置かない（幽霊戦線を作らない）
        set(next);
    }

    /// <summary>★AI第1波①（2026-08-16・<see cref="Coefficients.AiLawGateUnified"/>>0 のみ）：AIの法目標の統一ゲート＝幽霊戦線の根絶。
    /// <see cref="StepLawToward"/> と同じ作法（GateInfo確認・1段ずつ）だが、基点を**現行法**に置き、**規則で**到達不能な段は目標＝現行法へ畳む
    /// ＝到達不能な法を目標に残さない（⚠2026-09-18まではここが「通らない段は一律で畳む」だった＝下の修正段落）。幽霊戦線の実体＝目標≠現在の軸が CountOpenFronts に毎期数えられ続け、
    /// 政治押し予算 (1/N)^0.5（Government.Step の PoliticalPushBudget）が**全軸の押しを一律に薄める**こと。
    /// StepLawToward（基点＝前回目標）と違い基点＝現行法にする理由：ゲートが後から閉じても翌四半期に戦線が閉じる
    /// ＝「置いた時は通ったが今は通らない」目標も残さない。want は毎四半期再計算されるので、状況が変われば自然に再挑戦する（諦めの恒久化はしない）。
    /// 施行タイミングは不変：Government.StepEnum/StepBool は元々1期1段・同じ判定式（純支持＞−余白）なので、
    /// この整形で法の通り方は変わらず、**開きっぱなしの戦線数だけ**が変わる。
    /// ★2026-09-18 修正（AI＝プレイヤー対称・P5）：通らない段を一律 <c>set(cur)</c> で畳むのをやめ、**通らない理由で分ける**。
    ///   旧＝支持が足りないだけの拒否でも目標＝現行法へ畳む⇒目標＝現状で戦線が閉じ、その軸は二度と押されない**0の吸収状態**。
    ///   プレイヤーは法律タブが目標を書いたら（LawRowVM.Select）それが残るので畳まれない＝**AI だけが諦める**非対称だった
    ///   （Government.GateInfo の注記が「プレイヤー国は NullAgent＝畳まれない＝等価の破れ」と既に自認していた側の鏡像）。
    ///   段の財政法で害が見えたのは、段の軸では畳み先が現行法そのものでなく <c>TaxValue(TaxStage(w.X))</c>＝**下向きの新しい目標**になり、
    ///   下向きは旧支配層が賛成側で Government.Move を通る＝凍結ではなく**後退**になるから（土地税・教育費が0へ落ちる直接経路）。
    ///   新＝到達不能が**規則**による段（前提DAG・段の飛び越し・緊縮の床/天井・総動員・不可逆＝支持層が確立した法の後退）だけ畳む
    ///   ＝プレイヤーもボタンを押せない段＝Move が前提DAGを迂回せず、永久に閉じない幽霊戦線も作らない。
    ///   支持不足は**政治の争点**であって到達不能ではない＝目標は次の1段のまま残す（プレイヤーの置いた目標が消えないのと同じ）。</summary>
    private static void GateLaw(ref PolicyPreferences.Profile? shared, World w, PolicyAxis axis, int cur, int want, int max, System.Action<int> set)
    {
        want = System.Math.Clamp(want, 0, max);
        if (want == cur) { set(cur); return; }   // 目標＝現状（過去に置いた残存目標も畳む＝戦線を閉じる）
        int next = System.Math.Clamp(cur + (want > cur ? 1 : -1), 0, max);
        var (ok, _, _) = Government.GateInfo(w, axis, want > cur, next, ref shared);   // ★(146)：立場の土台は GateAllLawTargets の中で1回（下の注記）
        if (ok) { set(next); return; }           // 通る＝次の1段を目標に
        // 通らない理由で分ける。照会するのは法律タブ（LawRowVM.CanReach）とエンジン（StepEnum/StepBool）が既に共有している副作用なしの2本だけ
        // ＝新しい判定式も新しい状態も新しいタイマーもゼロ。⚠LawPrereq は下げる側でも訊く（段そのものが規則で塞がれているかを訊いている）。
        bool barred = !Government.LawPrereq(w, axis, next, w.Coef).Ok
                   || Government.BlockedByIrreversibility(w, axis, Government.Regressive(axis, want > cur));
        set(barred ? cur : next);   // 規則で到達不能＝現行法へ畳む／支持が足りないだけ＝次の1段を目標に残す（凍結であって後退ではない）
    }

    /// <summary>AI第1波①の適用パス：Apply が旧来どおり計算した法目標（want）を、
    /// 施行の現在値から「いま通る1段」だけに整形する。対象＝Apply が書く全法軸。
    /// ★税・支出・軍事費（作者裁定2026-09-12 R1/R2・2026-09-20 点火）も**対象**（末尾のブロック）＝法になった以上ここを通る。
    /// 対象外（理由つき）：関税＝R3で段にしない／行政費・金利＝利益集団の抵抗を受けない内部事項で政治軸を持たない／
    ///   穀物法・土地改革・農奴解放＝既に StepLawToward 済み（Apply内）／
    /// 通貨制度（MonetaryTarget）＝CountOpenFronts に数えられない軸＝幽霊戦線を作らない（実加入も信用ゲートが握る）／
    /// 社会保障法＝Government.Step がスライダー（SocialSecurityTarget）から毎期導出＝法目標の書き込み口が別／
    /// 移民制限法・国教法＝AiLawWriteCoverage の書き手が常に StepLawToward 経由（二重整形しない）。</summary>
    internal static void GateAllLawTargets(World w, NationPolicy p)   // internal＝番人（潤沢の減税も門を通る）が冪等性を確かめるためだけ（可視性のみ）
    {
        // ★立場の土台（PolicyPreferences.Build＝POP の数回の走査）は、この整形の中で最初に純支持を読むときに1回だけ取って全ての照会で分け合う（2026-10-01 (146) 計算量の予算・恒等）：
        //   土台の入力（POP の保有・現行法・軍事費の目標〔Government.MilitaryTotalTarget〕ほか）をこの整形は書かない＝書くのは法目標（NationPolicy）だけで、
        //   土台が読む軍事費の目標（陸海の維持費の目標）の書き込みは**最後の照会の後**（末尾の軍事費の段）。旧＝照会ごとに Build＝全長の1種で約18万回（Build の15%）。
        PolicyPreferences.Profile? shared = null;
        GateLaw(ref shared, w, PolicyAxis.EducationLaw, (int)w.EducationLaw, (int)p.EducationLaw, (int)EducationLaw.Compulsory, v => p.EducationLaw = (EducationLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Suffrage, (int)w.Suffrage, (int)p.Suffrage, (int)Suffrage.Universal, v => p.Suffrage = (Suffrage)v);
        GateLaw(ref shared, w, PolicyAxis.WomenRights, (int)w.WomenRights, (int)p.WomenRights, (int)WomenRightsLaw.Suffrage, v => p.WomenRights = (WomenRightsLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Censorship, (int)w.Censorship, (int)p.Censorship, (int)CensorshipLaw.FreePress, v => p.Censorship = (CensorshipLaw)v);
        GateLaw(ref shared, w, PolicyAxis.LaborUnion, (int)w.LaborUnion, (int)p.LaborUnion, (int)LaborUnionLaw.Tolerated, v => p.LaborUnion = (LaborUnionLaw)v);
        GateLaw(ref shared, w, PolicyAxis.ShorterHours, (int)w.WorkHours, (int)p.WorkHours, (int)WorkHours.Unlimited, v => p.WorkHours = (WorkHours)v);
        GateLaw(ref shared, w, PolicyAxis.ChildLabor, w.ChildLaborAllowed ? 1 : 0, p.ChildLaborAllowed ? 1 : 0, 1, v => p.ChildLaborAllowed = v != 0);
        GateLaw(ref shared, w, PolicyAxis.ConscriptionLaw, (int)w.ConscriptionLaw, (int)p.ConscriptionLaw, (int)ConscriptionLaw.Universal, v => p.ConscriptionLaw = (ConscriptionLaw)v);
        GateLaw(ref shared, w, PolicyAxis.SecretBallot, (int)w.SecretBallot, (int)p.SecretBallot, (int)SecretBallotLaw.Secret, v => p.SecretBallot = (SecretBallotLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Bureaucracy, (int)w.Bureaucracy, (int)p.Bureaucracy, (int)BureaucracyLaw.Professional, v => p.Bureaucracy = (BureaucracyLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Antitrust, (int)w.Antitrust, (int)p.Antitrust, (int)AntitrustLaw.Trustbust, v => p.Antitrust = (AntitrustLaw)v);
        if (w.Coef.GuildLawGate > 0) GateLaw(ref shared, w, PolicyAxis.Guild, (int)w.Guild, (int)p.Guild, (int)GuildLaw.None, v => p.Guild = (GuildLaw)v);   // ★特権 段4
        GateLaw(ref shared, w, PolicyAxis.WartimeRationing, w.WartimeRationing ? 1 : 0, p.WartimeRationing ? 1 : 0, 1, v => p.WartimeRationing = v != 0);
        GateLaw(ref shared, w, PolicyAxis.MilitaryExport, w.MilitaryExportBan ? 1 : 0, p.MilitaryExportBan ? 1 : 0, 1, v => p.MilitaryExportBan = v != 0);
        if (w.Coef.RebelArmsMarketEnabled > 0) GateLaw(ref shared, w, PolicyAxis.CivilianArms, w.CivilianArmsBan ? 1 : 0, p.CivilianArmsBan ? 1 : 0, 1, v => p.CivilianArmsBan = v != 0);   // ★民間の武器購入の法（2026-09-11）
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeRailways ? 1 : 0, p.NationalizeRailways ? 1 : 0, 1, v => p.NationalizeRailways = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeMines ? 1 : 0, p.NationalizeMines ? 1 : 0, 1, v => p.NationalizeMines = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeMilitary ? 1 : 0, p.NationalizeMilitary ? 1 : 0, 1, v => p.NationalizeMilitary = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeOil ? 1 : 0, p.NationalizeOil ? 1 : 0, 1, v => p.NationalizeOil = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeElectricity ? 1 : 0, p.NationalizeElectricity ? 1 : 0, 1, v => p.NationalizeElectricity = v != 0);
        GateLaw(ref shared, w, PolicyAxis.NationalizationAll, w.NationalizeAll ? 1 : 0, p.NationalizeAll ? 1 : 0, 1, v => p.NationalizeAll = v != 0);
        GateLaw(ref shared, w, PolicyAxis.OpiumPossession, (int)w.OpiumPossession, (int)p.OpiumPossession, (int)ReformLevel.Full, v => p.OpiumPossession = (ReformLevel)v);
        GateLaw(ref shared, w, PolicyAxis.OpiumExport, w.OpiumExportBan ? 1 : 0, p.OpiumExportBan ? 1 : 0, 1, v => p.OpiumExportBan = v != 0);
        GateLaw(ref shared, w, PolicyAxis.CapitalMobility, w.CapitalControlLaw ? 1 : 0, p.CapitalControlTarget ? 1 : 0, 1, v => p.CapitalControlTarget = v != 0);
        // ★税・支出・軍事費の段（作者裁定2026-09-12 R1/R2）：段になった軸も**現行段から・いま通る1段**へ整形する＝法と同じ作法。
        //   ⚠これが無いと、AIが置いた遠い目標（例：緊縮スタンスの所得税0.35＝段1）が通らないまま CountOpenFronts に数え続けられ、
        //     政治押し予算 (1/N)^0.5 が全軸の押しを薄める＝**AIだけが幽霊戦線を開く**（ゲート緑≠機構が働いた）。
        //   ⚠AI＝プレイヤー対称：置く/置かないの物差しは法律タブと同じ GateInfo（IsPlayer は読まない）。
        // ★錨2026-09-18：段序数↔率の写像は Government.TaxStage／TaxValue の2本だけ（軸ごとの錨を引く）＝ここに第二の式（v/3）を置かない。
        void GateTax(PolicyAxis a, double cur, double tgt, Action<double> set)
            => GateLaw(ref shared, w, a, Government.TaxStage(a, cur), Government.TaxStage(a, tgt), Government.TaxLawStages, v => set(Government.TaxValue(a, v)));
        GateTax(PolicyAxis.IncomeTax, w.TaxRate, p.IncomeTaxTarget, v => p.IncomeTaxTarget = v);
        GateTax(PolicyAxis.ProgressiveTax, w.ProgressiveTax, p.ProgressiveTaxTarget, v => p.ProgressiveTaxTarget = v);
        GateTax(PolicyAxis.PollTax, w.PollTax, p.PollTaxTarget, v => p.PollTaxTarget = v);
        // ⚠専売は段にしない（作者裁定2026-09-20）＝AIの整形も通らない（商品ごとの率は下の奢侈品のブロックが置く）
        GateTax(PolicyAxis.LandTax, w.LandTax, p.LandTaxTarget, v => p.LandTaxTarget = v);
        GateTax(PolicyAxis.EducationSpending, w.EducationSpending, p.EducationSpendingTarget, v => p.EducationSpendingTarget = v);
        // 軍事費＝合計だけが段（陸海比は SetNavalUpkeepShare が続けて連続で決める＝D4）。書き込み口は比を保つ既存の1本。
        GateLaw(ref shared, w, PolicyAxis.Coercion, Government.MilitaryStage(w.MilitarySpending), Government.MilitaryStage(p.MilitarySpendingTarget),
                Government.MilitaryLawStages, v => p.MilitarySpendingTarget = Math.Max(1, v) / (double)Government.MilitaryLawStages);   // Max(1,…)＝「なし」を作らない
    }

    // ★撤去2026-08-16（C2-C5）：AutoAntiCorruptionTarget（汚職根絶の自動化・2026-08-02）を物理削除＝汚職V2恒久ONで根絶費の動詞ごと死骸（プレイヤー/AI対称のまま消滅）。

    /// <summary>近代化投資率の既定式＝Apply と同じ**工業化度ギャップ×財政余力**×近代化スタンスの率（スタンスを持たない国の代役）。
    /// 書き手は Simulation.QuarterStart（非プレイヤー国のみ・DecideQuarter の前）。UtilityAgent 国は直後の Apply が上書きするので
    /// 実際に効くのは NullAgent の非プレイヤー国（テスト/warmtest）だけ。プレイヤー国は財政タブのスライダー（2026-08-10）。</summary>
    public static double AutoModernizationFrac(World w, double fiscalStress)
    {
        double easeInd = 1 - fiscalStress;
        double ceil = EffectiveIndCeiling(w);   // ★鋼市場診断#1（2026-08-16）：プレイヤー自動化もAIと同じ包絡（この関数の存在理由＝対称）。既定0＝定数2.5＝ビット恒等
        double indGap = PoliticalQuantities.Clamp01((ceil - w.MeanIndustrialization) / ceil);
        return System.Math.Max(0, indGap * easeInd * ModernizeInvestFrac);
    }

    /// <summary>★鋼市場診断#1（2026-08-16・<see cref="Coefficients.AiIndustrialCeilingFrontier"/>=α）：近代化投資の自主停止の包絡。
    /// 旧＝定数2.5固定＝世界前線（<see cref="World.IndustrialFrontier"/>＝各国MeanIndustrializationの最大・MultiNationSim が四半期同期で配布済み）が
    /// どれだけ先へ行ってもAIは2.5で投資を止める＝secondDeliver を下げる主体の不在の直接原因（診断B-4。プレイヤーは州上限5.0まで踏める非対称）。
    /// α>0＝max(2.5, α×IndustrialFrontier)＝前線が2.5/αを超えて伸びた世界でだけ包絡が開く（開幕は前線≈2.5以下＝挙動不変＝暦年を読まずに時代が創発）。
    /// ⚠経緯（Apply内コメント「無制限だと露が規模常勝」）＝**点火は段階掃引 α=0.6/0.8/1.0**（一気に1.0へ上げない）・列強再現率と露を必ず見る。
    /// ⚠ChooseStance の indRoom(4.0)＝スタンス選択の目標と、ダンピングの後発判定(&lt;2.5)は今回据え置き（別判断＝ここは投資の物理上限のみ・記録）。
    /// 単一国（IndustrialFrontier=0）は α>0 でも 2.5＝較正・単体テスト恒等。NaN は否定形ガードで定数側へ（fail-closed）。</summary>
    private static double EffectiveIndCeiling(World w)
    {
        double a = w.Coef.AiIndustrialCeilingFrontier;
        if (!(a > 0)) return IndustrializationCeiling;   // 0＝厳密恒等（否定形＝NaNも休眠側）。★既定は点火0.6＝この枝は通らない
        double f = a * w.IndustrialFrontier;
        return f > IndustrializationCeiling ? f : IndustrializationCeiling;   // NaN/未注入(0)は定数側へ倒す
    }
    private const double DumpingExportRatio = 1.3;         // ダンピング発動の輸出超過比
    private const double DumpingTariff = -0.10;            // ダンピング（負関税）
    internal const double RecessionDrawdownThreshold = 0.08;// 後退＝緩和のドローダウン閾値（★2026-08-26 に UI の前倒し起債と1本化・その起債は 2026-09-28 に借入の自動化で削除）
    // 恐慌AI対処（2026-07-25 #124・ユーザー裁定＝機構の振幅は触らずAIを賢く。プレイヤーも同じ手を打てる＝対称）：
    private const double PreTighteningInterestRate = 0.32; // 予防的引締め（過熱の兆候で早めに締めバブルを小さく＝恐慌原因の41%が信用過熱先行）
    // ★係数へ昇格（2026-08-22・恐慌較正の二段構え＝作者裁定「まず裸の資本主義の値を予想し、素の経済をそこへ合わせ、AIの対策で史実へ」）。
    //   既定値は旧 const と同値＝**ビット恒等**。目的＝POB_COEF で「裸の測定腕」（安定化チャネル全OFF）を組めるようにする座標であって、
    //   ゲーム側の恒久ONの裁定（2026-07-25）は不変。裸腕＝ PreTightenHeatFrac=1.0（予防なし＝素の過熱閾値のみ）・CrisisAusterityRelief=1.0（緊縮の罠を素通し）
    //   ＋ LolrStrength=0・LolrReserveBoost=0（最後の貸し手なし）。読み手＝Apply の金利ブロック（PreTightenHeatFrac）と財政規律ブロック（CrisisAusterityRelief）の2箇所のみ。
    // #124裁定確定（2026-07-25）：切り分け実測＝a/bとも恐慌を減らすが near−5（帯が恐慌の多さに依存＝1848/1917型政変の駆動因は経済危機そのもの。
    // 逸脱の実体＝後進専制国が危機を凌いで近代化しすぎ・英が固着）。ユーザー裁定＝**賢いAIを固定し、帯は数値側で再較正する**
    //（AI=プレイヤー対称の原則＝バランスをAIの愚かさで取らない）。再較正の第一候補＝C-高7 DecayCeilingUniversal（後進国が「危機の破壊」でなく
    // 「制度の硬直」で後進に留まる経路＝新逸脱国ヒヴァ/エチオピア/マラータ等はdecayストックを持たない50カ国側）→次にDecay3チャネル強化。
    private const double EasingInterestRate = 0.05;        // 緩和金利
    private const double OverheatHeatThreshold = 0.7;      // 過熱＝引締の熱閾値
    private const double TighteningInterestRate = 0.45;    // 引締金利
    private const double NeutralInterestRate = 0.22;       // 平時中立金利
    private const double EducationEaseFloor = 0.3;         // 教育費の逼迫時下限
    private const double EducationEaseSpan = 0.7;          // 教育費の余裕連動幅
    private const double FiscalTaxStressMult = 0.4;        // 逼迫時の広い税の最低率係数
    private const double OpiumBanHysteresis = 0.3;         // アヘン禁令解除のヒステリシス比
    private const double OpiumFullBanRatio = 2.0;          // 全面禁止（国内生産も違法）へ進む中毒度＝輸入禁止の閾値の何倍か（禁制品doc C3・2026-07-30）
    // ★撤去2026-08-16（C2-C5）：反腐敗AI定数4本（CorruptionGraftThreshold/AntiCorruptionDebtGdpCap/AntiCorruptionGraftMult/AntiCorruptionCap）＝読者は根絶費の動詞のみ＝機構ごと物理削除。
    private const double FireSaleFiscalStress = 0.7;       // 国営工場売却の逼迫閾値
    // ★撤去2026-08-16（AI第1波・掃除）：POP動詞①（2026-07-20全廃）の残骸4定数
    //   （ParliamentaryStanceThreshold/NonParliamentaryStanceThreshold/StanceHysteresis/ClassSlotCount）＝全域grepで読み手ゼロを確認し物理削除（理念違反監査§6-5計上済み）。
    // AI第1波②（2026-08-16）総動員のAI分岐の判定線（SelfPlay.DecideQuarter・2026-09-20 に段の点火で常時 live）：
    private const double MobilizeOutmatchRatio = 1.2;   // 陣営戦力（Diplomacy.SideMilitary＝同盟込み）で相手がこの倍を超えたら劣勢＝総動員。
                                                        // 屈服線（AISubmitMilRatio=3.0）・宥和線（AppeaseMilitaryRatio=2.5）よりずっと手前＝「戦えるが苦しい」帯で踏む
    private const double MobilizeLosingScore = 0.5;     // 敗色（自国視点の war.Score 負側）がこれを超えたら存亡＝本土が蹂躙されている
                                                        // （レジームチェンジ1.0・割譲 TerritoryCessionScore=1.2 の手前で踏み込む＝間に合う総動員）
    // 【墓標2026-09-28（作者裁定 F5）】移民制限法のAI判定線3本（閉鎖1.0・選別0.4・開放へ戻す0.15）を削除。締める線は (63) の受入の上限の係数から導き、
    //   開放へ戻す枝は消した（DecideModernLaws の移民制限の段落）。
    // ★E4（総選挙の公示のAI等価物・2026-08-17）の判定線（Coefficients.ElectionEventEnabled>0 のときだけ読む）。
    // ⚠新しい係数は作らない（ゲートは ElectionEventEnabled 1本＝点火でプレイヤーとAIが同時に有効になる）。ここはAIの判断線＝SelfPlay私有定数。
    // ⚠★E3''（確定形2026-08-17）で AiElectionMandateBudget は撤去した＝レバーは「政治資本の何割を賭けるか」ではなく
    //   **裁量余剰の何%を注ぐか**（＝金の予算制約）になったので、押しの大きさをマンデート余白から出す理由が消えた。
    //   余白は今も「そもそも動かせるか」の門（price ≤ margin）としてだけ効く＝二重に使わない。
    private const double AiElectionIncumbentAffinity = 0.5;   // 与党（族）と同じ族の看板への上乗せ＝自派の候補を推す。
                                                              // ブロック立場（概ね−2..+2）に対して控えめ＝支配ブロックの利害が主・党派心は従
    private const double AiElectionMinPush = 0.05;            // 注ぐ額が裁量余剰のこの割合未満なら静観（微風に軍と学校の予算を割かない＝加担しないことも択）

    // ★軍国教義の判定線（2026-08-18・親軍法の撤去）＝**軍の政治的優位**（PoliticalQuantities.MilitaryPoliticalRatio∈[0,1]）がこれ以上なら軍事国家の教義。
    //   ⚠新しい係数は作らない（ここはAIの判断線＝SelfPlay私有定数＝E4の作法と同じ）。
    //   ⚠忠実度ハーネスの閾値（HistoricalReference.MilitarismDegreeOk）とは**別の定数**にする＝物差しを動かすと挙動が動く、を作らない
    //     （参照表はシミュレーションから読まれない＝帯リスクゼロ、という既存の性質を守る）。値の由来は docs/202608。
    //   ⚠**給与は1度も読まない**（金では将校は動かない＝西1931アサーニャ改革は満額俸給付き退役で最大のクーデタを招いた）。
    private const double MilitaristDoctrineDegree = 0.25;

    /// <summary>倍率排除⑧：AIの教義（戦略バイアス）を政体ラベルでなく**支配ブロック（利益集団の力）＋軍国度**から創発させる。
    /// 旧支配層が支配＝軍国（軍が政治的に優位）なら軍事国家／否なら伝統的均衡・近代エリート支配＝資本主義(工業)・大衆支配＝近代化/福祉。
    /// 参政権の拡大で支配ブロックが移れば教義も動的に変わる（政体→固定挙動の写像を廃し、誰が権力を握るかという実体から創発）。
    /// ★2026-08-18（親軍法の撤去）：旧＝法のON/OFF。教義は離散なので判定線は残るが、線を引く量が「法があるか」から
    /// **「軍が文民制度を圧倒しているか」**へ変わった＝軍国は宣言でも金でもなく、人事と軍法の帰属で決まる。</summary>
    public static Doctrine DoctrineFrom(World w)
    {
        var (trad, modern, mass) = InterestGroups.Blocs(w);
        if (trad >= modern && trad >= mass) return PoliticalQuantities.MilitaryPoliticalRatio(w) >= MilitaristDoctrineDegree ? Doctrine.Militarist : Doctrine.Balanced;   // 旧支配層が支配（軍国＝軍事国家／否＝伝統的均衡）
        if (modern >= mass) return Doctrine.Industrialist;                                                    // 近代エリート（ブルジョワ・専門職）が支配＝資本主義
        return Doctrine.Modernizer;                                                                            // 大衆（労働者・農民）が支配＝近代化・福祉
    }

    internal enum Stance { Modernize, Industrialize, Welfare, Fiscal, Militarize }   // ★internal＝計器（Runner の armstrace）が読むためだけ。InternalsVisibleTo("PriceOfBread.Runner")

    /// <summary>★診断（2026-08-26・作者質問「史実の軍縮が創発されているか／理想の軍隊が発散していないか」）：
    /// スタンス選択の**内訳**。実測で「61カ国が全員 Militarize に永久固定・軍縮エピソード0件」が出たので、
    /// uMil のどの項が勝ち続けているのかを国ごとに読めるようにする。⚠**値を作る場所は増やさない**＝
    /// <see cref="UtilityAgent.ChooseStance(World, out StanceBreakdown)"/> の本体1つが計算し、これはその写しを運ぶだけ。</summary>
    internal readonly record struct StanceBreakdown(
        double UMod, double UInd, double UWel, double UFis, double UMil,
        double MilitaristBase, double TensionTerm, double ColonyDefense, double Survival, double Tension,
        string ThreatFrom, double ThreatRatio);   // ★脅威の出所＝生存効用の max を取った相手（argmax の写し）と、そのときの比

    // 【墓標2026-09-26・回5-1 項目19】AIだけの伝統主義ゲート（旧 ModernizationLawsGated・係数 TraditionalistLawGate・裁定2026-07-09 黒船動学）を削除。
    //   プレイヤー（NullAgent）には無い門＝思想⑤の違反。近代化の法（教育法・参政権・女性の権利・秘密投票・専門官僚制・独禁・同業組合）を置けるかは
    //   法律タブと同じ GateInfo（GateAllLawTargets）・旧支配層の抵抗（PolicyPreferences）・政治資本の在庫で決まる＝プレイヤーと同じ物差し。

    /// <summary>財政バッファ＝月次義務（維持費＋利払い＋行政・軍装備の請求権）の <see cref="Coefficients.FiscalStressBufferMonths"/> か月分。
    /// 財政逼迫の物差し（監査2026-07-11 リ4）：旧＝国庫50,000の絶対額固定を53カ国（初期国庫約370倍差）へ一律適用＝
    /// 小経済の国は恒久破産モード（福祉ほぼゼロ・汚職根絶が永久0・逆進課税の高止まり）・大経済の国は無風、という規模非中立の是正。
    /// 公開（2026-08-04・UI）：財政タブが「国庫の余裕＝月次義務の何か月分か」を表示する（AIと同じ物差し）。</summary>
    /// <summary>★軍需の身の丈ゲート倍率（2026-09-07）：FactorySpec が「軍需」の財だけ MilitaryBuildCostCapScale（既定1.0＝恒等）。</summary>
    private static double BuildCapMul(World w, int goodId)
        => w.Coef.MilitaryBuildCostCapScale > 1.0 && Catalog.FactorySpec(goodId).Category == "軍需" ? w.Coef.MilitaryBuildCostCapScale : 1.0;

    public static double FiscalBufferOf(World w)
        => Math.Max(1.0, w.Coef.FiscalStressBufferMonths * (w.LastInterest + w.LastAdminClaim + w.LastMilitaryClaim));   // （連合維持費は 2026-09-30 (135) に撤去＝行政と兵力の購買の1本）
    // ⚠R3安定形（2026-08-08作者裁定「不具合として解決・安定するように」）：兵士給与は**ここ（緊縮反射の分母）には入れない**
    //   ＝R3=1.0の全面算入は世界的な緊縮暴発で棄却済み。給与は下の OpulenceLineOf（潤沢判定＝減税反射・支出反射の線）にだけ算入
    //   ＝清の「軍の請求書を見ない潤沢判定→減税」の嘘だけを閉じ、austerity側は不変。

    /// <summary>潤沢線＝「これを超える国庫は取り過ぎ」の物差し（RelieveOpulence と SpendableRevenue が共有＝減税反射と支出反射の対）。
    /// 兵士給与（LastSoldierPay＝国庫minで切る前の真の請求書）を FiscalBufferSoldierPayWeight で算入＝大軍国は潤沢と誤判定されない。</summary>
    public static double OpulenceLineOf(World w)
        => (FiscalBufferOf(w) + w.Coef.FiscalBufferSoldierPayWeight * w.Coef.FiscalStressBufferMonths * w.LastSoldierPay) * OpulenceBufferQuarters;

    /// <summary>財政逼迫度∈[0,1]＝1−国家の支払い率 r（作者裁定 R4・2026-09-28 支払いの一元化 段2-1）＝全ての国家の支払いの請求のうち払えない割合（0=十分・1=1銭も払えない）。
    /// 旧＝国庫が財政バッファ（月次義務の何か月分か）を下回る度合い＝r と別の物差しで、請求の一部（維持費・利払い・行政・軍装備）しか見なかった。
    /// 公開＝Simulation.QuarterStart（AutoModernizationFrac）とテストが同じ物差しを読む。NaN は逼迫側＝fail-closed。</summary>
    public static double FiscalStressOf(World w)
    {
        double s = 1.0 - w.FiscalFundingRatio;
        return s >= 0 ? (s <= 1 ? s : 1.0) : (s < 0 ? 0 : 1.0);   // 否定形ガード＝NaN は逼迫側（1）
    }

    /// <summary>★脅威の割合∈[0,1)（2026-10-02 (155)）＝安全保障の不安（届く敵意の養える軍の和＝<see cref="World.SecurityThreat"/>）÷(それ＋自国の養える軍の人口の側＝<see cref="World.SecurityThreatOwnMass"/>)
    /// ＝新兵器の必要性の脅威の割合（必要量÷(必要量＋自国の力)・Discovery）と同じ形（有界＝上下を切らない）。小国が大国の隣で感じる不安は1へ近づき、周りに養える軍の少ない国（開国前の日本・清）は0へ近づく。
    /// 定まらない（単一国・分母0・NaN）は0。</summary>
    internal static double SecurityShare(World w)
    {
        double t = w.SecurityThreat, own = w.SecurityThreatOwnMass;
        double x = t / (t + own);
        return x >= 0 && x <= 1 ? x : 0;
    }

    /// <summary>★AI の軍事費の段の目標＝安全保障の不安の唯一の入口（2026-10-02 (155)・作者 2026-10-01＝脅威の入口を軍事費の段の1つにする案に異議なし・推奨で進めた）。
    /// 目標＝平時の段の幅（最下段 段1〜平時の最上段 段3＝戦時経済の段4 の手前）を脅威の割合（<see cref="SecurityShare"/>）で内分＝不安のない国は段1・届く敵意が自国の養える軍と同じなら段2・
    /// 圧倒されるほど段3 へ近づく。段への丸めと1段ずつの登坂と戦時経済の門（総動員）は法の門（Government・GateAllLawTargets）が掛ける＝ここで上下を切らない（思想⑧＝割合が有界）。
    /// 不安が定まらない世界（単一国）は割合0＝最下段（旧の既定 0.25 と同じ段）。旧＝既定 0.25・軍備スタンスで 0.7（脅威は緊張・植民地防衛・生存の効用を通ってスタンスの2値で入った）。</summary>
    internal static double SecurityStageTarget(World w)
    {
        double lo = 1.0 / Government.MilitaryLawStages, hi = (Government.MilitaryLawStages - 1.0) / Government.MilitaryLawStages;   // 段1（0.25）〜段3（0.75）
        return lo + (hi - lo) * SecurityShare(w);
    }

    // スタンス選択（DecideQuarter から抽出＝挙動同一）：状況（技術差・工業化余地・不満・国庫・脅威）への効用で最良の政策姿勢を選ぶ。
    private static Stance ChooseStance(World w) => ChooseStance(w, out _);

    /// <summary>★2026-08-26：内訳つきの本体。従来の <c>ChooseStance(World)</c> は out を捨てる薄いラッパー＝
    /// **式も順序も1文字も変えていない**（計器が uMil の各項を読めるようにするための抽出のみ）。</summary>
    internal static Stance ChooseStance(World w, out StanceBreakdown bd)
    {
        var doctrine = DoctrineFrom(w);   // 倍率排除⑧：支配ブロック＋軍国度から創発（政体ラベルの写像を廃止・政変や参政権拡大で動的に変わる）
        double sat = NationalScore.MeanSatisfaction(w);
        double techGap = Math.Max(0, w.WorldTechLevel - w.TechLevel);                  // 技術の遅れ（後発の伸びしろ）
        double indRoom = Math.Max(0, IndustrializationRoomTarget - w.MeanIndustrialization); // 工業化の余地（到達可能な目標≈均衡3.7。達成後はuIndが下がり教義の道へ）。改修2026-06-28：首都州のみ→全州平均（首都州が崩れてもAIが投資目標を見失わない）
        double unrest = Math.Max(0, UnrestSatisfactionThreshold - sat);                                       // 不満（満足の不足）
        double fiscalNeed = FiscalStressOf(w);   // 国庫の逼迫（規模中立＝月次義務の何か月分か。監査2026-07-11 リ4）

        // スタンスごとの効用＝助ける成分×不足。最大を選ぶ。
        double famine = Math.Max(0, 1.0 - w.FoodFill);   // 飢饉度（主食充足の不足∈[0,1]）。旧AIは FoodFill を一切読まず飢饉を見殺しにしていた（AI監査2026-07-06）＝反応の入力を与える
        double uMod = UModTechWeight * techGap + UModIndRoomWeight * indRoom + UModUnrestWeight * unrest;
        double uInd = UIndRoomWeight * indRoom + UIndTechWeight * techGap;
        double uWel = UWelUnrestWeight * unrest + UWelFamineWeight * famine;        // 飢饉時は福祉スタンスの効用↑＝救済(社会保障)・逆進課税の緩和・労働短縮へ舵を切る（生存反射）
        double uFis = UFisWeight * fiscalNeed;
        // 軍備の基礎効用：専制(軍国)は常に軍拡。加えて対外脅威(緊張)があればどの政体も防衛のため軍拡する
        // ＝緊張緩和（不満が出ない）と対(つい)で、軍が地政学に応答する（脅威下で軍拡・平時は控える）。代償は国庫の維持費。
        double tension = w.Diplomacy is { } dip && w.SelfPower is { } sp ? dip.MaxTension(sp) : 0;
        // 植民地防衛：帝国は海軍を維持して領土を守る（植民地ほど軍拡）。これがないと制海権を失い植民地を奪われる（英の海軍が消える問題）。
        // ★飽和（2026-08-03・作者報告「イギリスの開発度が異常に低い」の根）：旧＝植民地数に無限比例で、
        //   植民地11の英は恒久+3.3＝uInd(≈4.0)と拮抗し tension が少しでもあれば **131年間 Militarize に固定**＝
        //   近代化投資レバーが毎四半期0に潰され、開幕バーストも踏めず Ind 0.33→0.67 に沈んだ（実測 indprobe 120Q）。
        //   飽和は既存の「帝国規模の飽和」定数 ColoniesForFullColonial を再利用（Colonial.cs の反帝世論と同じ写像）＝
        //   新しいスカラーを置かない。守るべき帝国がある事実は残り、規模の無限加算だけが消える。
        // ★型統合後の旧読み手（wave2b G06#2）：NationalScore.Wealth は ColoniesAreRegions>0 で個数→質量へ切替済み（州細分化で「行を分けただけで増える」個数の是正）。
        //   ここは旧単位のまま個数を読んでいた＝分子だけ質量へ揃える（換算は Wealth/:1558-1559 と同じ錨＝初期の 個数/質量）。
        double colonyUnits = w.Coef.ColoniesAreRegions > 0
            ? (w.SelfPower?.ColonyMassUnits(w.Coef.ColonialPopScale) ?? 0) * (w.Diplomacy is { } dipC && dipC.InitialColonyMass > 1e-9 ? dipC.InitialColonyCount / dipC.InitialColonyMass : 1.0)
            : (w.SelfPower?.Colonies ?? 0);
        double colonyDefense = ColonyDefenseWeight * w.Coef.ColoniesForFullColonial
                             * Math.Min(1.0, colonyUnits / Math.Max(1.0, w.Coef.ColoniesForFullColonial));
        // 生存効用（改修2026-06-28）：存亡の脅威なら、緊張に出る前から軍拡を強める（弱小・中堅の自衛＝生存目的の行動）。
        // 監査2026-07-11（リ3）：旧＝全球最強との単純比較＝53カ国中39カ国が開始時点で飽和値4.5を抱え、Welfare/Fiscalスタンスが
        // 数学的に選ばれ得ず、飢饉反射・緊縮反射が大半の国で発火不能だった（破産中の小国が軍拡を選び続ける）。
        // 「脅威は近接から来る」＝相手の軍×投射（距離減衰）＝実際に自国へ届く力との比較へ置換（近代化法ゲート側の全球比較撤去と同じ処方）。
        // ★届く力は実際の戦線と同じ押しの1本（Diplomacy.FrontPush＝陸軍力×投射×上陸×制空・戦線1・戦費満額）で測り、自国も同じ陸軍力で比べる
        //   （2026-09-29 処理の一元化 段3 G）。⚠旧＝陸海合計の Military×投射 ÷ 自国の Military＝画面の戦争見通しと戦線（陸軍力）と物差しが違った
        //   ＝AI とプレイヤーが別の物差しで相手を見ていた（設計思想⑤）。自国の側は本土で守る＝投射を掛けない（旧と同じ）。
        double survival = 0;
        string threatFrom = "-"; double threatRatio = 0;   // ★診断のみ（値の生成には一切関与しない）
        if (w.Diplomacy is { } dipS && w.SelfPower is { } spS)
        {
            double maxReach = 0;
            foreach (var q in dipS.Powers)
                if (q != spS && q.Nation is not null)
                {
                    double nextMax = Math.Max(maxReach, dipS.FrontPush(q, spS, w.Coef));
                    if (nextMax != maxReach) threatFrom = q.Name;                                        // ★診断＝更新した相手を覚えるだけ
                    maxReach = nextMax;
                }
            double ratio = maxReach / Math.Max(1e-9, spS.LandPower);
            threatRatio = ratio;
            if (ratio > w.Coef.SurvivalThreatRatio)
            {
                survival = w.Coef.SurvivalMilitaryWeight * Math.Clamp(ratio - w.Coef.SurvivalThreatRatio, 0, SurvivalRatioCap);
                // ★②2026-08-26：見込みの薄さで割り引く（SurvivalHopelessScale=0 では分岐に入らない＝厳密恒等）。
                if (w.Coef.SurvivalHopelessScale > 0)
                    survival *= 1.0 - w.Coef.SurvivalHopelessScale * (1.0 - PoliticalQuantities.Clamp01(w.Coef.SurvivalThreatRatio / ratio));
            }
        }
        // 軍国の脱出弁（生存）：破産寸前（国庫逼迫>0.5）は軍国教義の底上げ2.5を緩め、緊縮(uFis)へ舵を切れるようにする。
        // 旧実装は軍国が固定2.5で福祉/緊縮を数学的に選べず、軍事費0.7＋逆進課税満額のまま破産スパイラルで見殺しになった（AI監査2026-07-06）。脅威(tension/survival)は残すので本当に攻められている時は軍拡を続ける。
        double militaristBase = (doctrine == Doctrine.Militarist ? MilitaristBaseUtility : 0.0) * (1.0 - PoliticalQuantities.Clamp01((fiscalNeed - MilitaristEscapeThreshold) / MilitaristEscapeThreshold));
        double tensionTerm = TensionUtilityWeight * PoliticalQuantities.Clamp01(tension / TensionNormalizer);   // ★2026-08-26：計器が読むための命名のみ（評価順は ((mb+t)+cd)+s のまま＝ビット恒等）
        double uMil = militaristBase + tensionTerm + colonyDefense + survival;

        // 第5段5-2（2026-07-27）体制AIに目：旧SelfPlayはLegitimacy/AvgMassMilitancyを一切読まず「革命が見えないAI」だった（中核ルールdoc §1-1）。
        // 革命の接近＝大衆闘争×正統性の欠乏（積＝どちらか単独では危機でない）。譲歩（福祉＝ビスマルク社会保険）と引き締め（武力→治安維持力）の
        // 両効用を押し上げ、どちらへ舵を切るかは教義バイアスと不足の大小に委ねる（政体ラベル分岐なし）。
        double revolt = Politics.AvgMassMilitancy(w) * (1.0 - w.Legitimacy);
        uWel += w.Coef.StanceRevoltWelfare * revolt;
        uMil += w.Coef.StanceRevoltCoercion * revolt;
        // 恐慌の目：GDPドローダウン→緊縮（国庫逼迫 fiscalNeed より早く点く先行指標。反景気循環の金融政策とは別レバー＝財政姿勢）。
        uFis += w.Coef.StanceDrawdownFiscal * w.GdpDrawdown;

        // 教義バイアス（多様化）：得意分野の効用を増幅＝政体ごとに異なる戦略を歩ませる
        switch (doctrine)
        {
            case Doctrine.Industrialist: uInd *= IndustrialistIndBias; uMod *= IndustrialistModBias; break;
            case Doctrine.Modernizer: uMod *= ModernizerModBias; uWel *= ModernizerWelBias; break;
        }

        Stance best = Stance.Modernize; double bestU = uMod;
        if (uInd > bestU) { best = Stance.Industrialize; bestU = uInd; }
        if (uWel > bestU) { best = Stance.Welfare; bestU = uWel; }
        if (uFis > bestU) { best = Stance.Fiscal; bestU = uFis; }
        if (uMil > bestU) { best = Stance.Militarize; bestU = uMil; }
        bd = new StanceBreakdown(uMod, uInd, uWel, uFis, uMil, militaristBase, tensionTerm, colonyDefense, survival, tension, threatFrom, threatRatio);   // ★計器へ写すだけ（分岐に影響しない）
        return best;
    }

    /// <summary>★女性の権利の梯子（統合法・2026-08-10）：AI は段を**上げるだけ**。
    /// ⚠下げ目標を書くと不可逆法（<see cref="Government.BlockedByIrreversibility"/>）と噛み合って
    /// 「現在値は降りられないのに目標だけ下がる」永久の開戦線になり、政治予算 PoliticalPushBudget が恒久的に目減りする（幽霊戦線）。
    /// 旧コードは bool を個別に true にするだけで後退が起こり得なかった＝統合で新たに生まれ得る退行を、単調化で構造的に封じる。</summary>
    private static void RaiseWomenRights(NationPolicy p, WomenRightsLaw to)
    { if (to > p.WomenRights) p.WomenRights = to; }

    /// <summary>E5/E6の9法へのAI目標書き手（02断線走査2026-07-28）：doctrine から新法の目標を導く。器・効果・主フラグは全て点火済みだったが
    /// 書き手が人間UIのみで、53カ国バッチでは9法が enum 既定値から永久に動かなかった（プロイセン文官改革もソ連国有化も英配給も原理的に不発）。
    /// 実際の施行は政治ゲート（Government.StepEnum/StepBool＝利益集団の抵抗）が決める。</summary>
    private static void DecideModernLaws(World w, NationPolicy p, Stance s)
    {
        bool atWar = w.Diplomacy is { } dip && w.SelfPower is { } sp && dip.Wars.Any(war => war.A == sp || war.B == sp);
        p.WartimeRationing = atWar;                                                       // 交戦中のみ配給（平時は解除＝資本の反対を無用に買わない）
        // 軍需輸出の法（禁制品doc C5・2026-07-30）：交戦中＝自国の軍を優先して輸出を止める／軍拡スタンス＝平時から武器を溜め込む。
        // それ以外（産業・交易・近代化）は許可＝売って稼ぐ。★どの相手に売らないかは法が緊張で選ぶ（AIもプレイヤーも同じ1本の法＝思想⑤）。
        p.MilitaryExportBan = atWar || s == Stance.Militarize;
        // ★民間の武器購入の法（設計メモ§7 (a)・2026-09-11）：票を持たない大衆から武器を取り上げる＝男子普選前は禁止・普選後は許可
        //   （英1819六法→普選期の解禁／FactionStrength の発言権と同じ物差し＝新しい閾値を作らない）。AI/プレイヤー対称（同じ1本の法）。ゲート0＝目標を書かない＝恒等。
        if (w.Coef.RebelArmsMarketEnabled > 0) p.CivilianArmsBan = w.Suffrage < Suffrage.AdultMale;
        // 【(162)「徴兵法の線を本国の陸の脅威で読む・平時に不安の多くが海の向こうと植民地から来る国は志願制」は (163) の合流で差し戻した 2026-10-02】＝戦時の装備の追随を外した木の10種で
        //   常勝の疑い（英 8/10）＝パッチ measurements/m163_20261002_landthreat.diff。英の徴兵法の残る根＝法の門（徴兵の縮小に旧支配層が反対）＝作者への確認（実装ログ (162)(163)）。
        p.ConscriptionLaw = (SecurityShare(w) >= 0.5 || atWar) ? ConscriptionLaw.Selective
            : (w.Suffrage >= Suffrage.AdultMale ? ConscriptionLaw.Volunteer : p.ConscriptionLaw);   // ★(155) 徴兵法の政治＝安全保障の不安（届く敵意の養える軍が自国の養える軍以上＝脅威の割合が半分以上）か交戦中＝徴兵制／平時の自由民主国で不安が半分未満＝小さな職業軍（英米型）。他は現状維持（旧＝軍備スタンス＝脅威は効用を通って間接に）
        if (w.Suffrage >= Suffrage.AdultMale) p.SecretBallot = SecretBallotLaw.Secret;   // 大衆選挙＝買収・威圧の排除が票の実在に直結
        if (w.AdminEfficiency > ProfessionalBureaucracyAdminThreshold) p.Bureaucracy = BureaucracyLaw.Professional;   // 行政力が育った国＝専門官僚制へ（プロイセン文官改革）
        if (w.MeanIndustrialization > AntitrustIndThreshold)
            p.Antitrust = w.MeanIndustrialization > TrustbustIndThreshold ? AntitrustLaw.Trustbust : AntitrustLaw.CartelBan;   // 工業化＝独占の弊害が政治問題化（シャーマン法1890）
        // ★特権 段4：工業化した国は同業組合を剥がす（独禁と同じ閾値＝工業の政治が立つ度合い・新しい数を作らない）。ゲート0＝目標を書かない＝恒等
        if (w.Coef.GuildLawGate > 0 && w.MeanIndustrialization > AntitrustIndThreshold)
            p.Guild = w.MeanIndustrialization > TrustbustIndThreshold ? GuildLaw.None : GuildLaw.Limited;
        if (w.Polity == Polity.ProletarianDictatorship)
        {   // プロ独裁＝全部門国有化（5部門＋『全て』＝ソ連型計画経済。§11-6d統合後の全点灯）
            p.NationalizeRailways = p.NationalizeMines = p.NationalizeMilitary = p.NationalizeOil
                = p.NationalizeElectricity = p.NationalizeAll = true;
        }
        else if (w.GoverningParty == (int)Parliament.Party.Socialist)
            p.NationalizeRailways = p.NationalizeElectricity = true;   // 社会党執政＝自然独占2部門（アトリー型・戦後国有化。旧・通信は『全て』へ統合＝ソ連型以外は目標に置けない）

        // ★AI第1波③（2026-08-16・AiLawWriteCoverage>0）：書き手ゼロ2法（移民制限法・国教法）のAI目標。
        // 旧＝議会与党の綱領（Parliament.ApplyPlatform・2026-09-26 に撤去＝回5-1）のみ＝非選挙政体（1783年の54カ国中46カ国）では誰もこの2法を動かさなかった。
        // ⚠書き込みは必ず StepLawToward 経由（GateInfo確認・1段ずつ）＝AiLawGateUnified の状態に依存せず幽霊戦線を新設しない独立設計。
        // ⚠0＝ブロックごと不実行（新しい読みもしない）＝ビット恒等。
        if (w.Coef.AiLawWriteCoverage > 0)
        {
            // 移民制限：読むのは**実際に起きている量**＝流入（前四半期の確定値÷人口）と大衆の失業。
            //   PolicyPreferences.ImmigrationRestriction（法選好の大衆スタンス）と同じ材料・同じ合成＝法が争点になる強さそのもの。
            //   ⚠⚠NetSupport（ゲートの出力）を意図として読んではならない（「通る法しか望まない」トートロジー＝Apply の3法ブロックの禁則）＝物質量を直接読む。
            // ★作者裁定 2026-09-28 F5：
            //   締める線＝**今の法の受入の上限まで流入が満ちたときの圧力**（失業0・受入能力1）。上限は (63) の
            //     ImmigrationReceiveCapFrac×人口×法の率×受入能力（MultiNationSim.MigrateBetweenNations の lawCap と同じ係数）
            //     ＝開放 40×0.012＝0.48・選別 ×ImmigrationSelectiveFrac＝0.24。今の法の上限まで流入が届き、職が足りない（失業>0）ときに1段締める。
            //     流入は今の法の下で測った量（法は DecideQuarter の後の Government.Step で動き、移民は四半期末）＝線と同じ法を読む。
            //   旧＝絶対の線（閉鎖1.0・選別0.4・開放へ戻す0.15）。(63) の後は圧力の最大が 開放0.48・選別0.24・閉鎖0.096（×(1＋失業)）で、
            //     閉鎖へは届かず、閉鎖の下では必ず「開放へ戻す」線を下回った＝1783年の鎖国が1〜2四半期で開国した（(75) §5）。
            //   ★開く向きはここに書かない＝流入の圧力が無い間は現状維持（PolicyPreferences の「流入ゼロの国では争点にならない」と同じ読み）。
            //     開くのは資本の要求（危機の要求＝この後の Parliament.TargetCrisisDemands／内戦の講和＝Politics.ConcedeToFaction）か外圧だけ。
            //     外圧＝不平等条約で港を開かされていること（TreatyPortsGranted＞0＝講和が積む既存の数・南京条約1842／安政条約1858／江華島条約1876）。
            //   どの向きも StepLawToward＝法律タブ（LawRowVM）と同じ Government.GateInfo を通る。
            double inflowRate = w.TotalPopulation > 1e-9 ? System.Math.Max(0, w.LastImmigration) / w.TotalPopulation : 0;
            double unemployment = PolicyPreferences.AvgUnemployment(w);   // 唯一定義（旧＝口だけ複製を2026-09-11 #13 で畳んだ）
            double pressure = w.Coef.ImmigrationRestrictionPrefScale * inflowRate * (1.0 + unemployment);
            if (!(pressure >= 0)) pressure = 0;   // NaNガードは否定形（Clamp/MaxはNaNを消さない）＝非有限は「圧力なし」＝現状維持側へ
            int immWant = (int)p.ImmigrationRestriction;   // 現状維持
            if (w.ImmigrationRestriction != ImmigrationLaw.Closed
                && pressure > w.Coef.ImmigrationRestrictionPrefScale * w.Coef.ImmigrationReceiveCapFrac
                              * (w.ImmigrationRestriction == ImmigrationLaw.Open ? 1.0 : w.Coef.ImmigrationSelectiveFrac))
                immWant = System.Math.Max(immWant, (int)w.ImmigrationRestriction + 1);
            if (w.TreatyPortsGranted > 0) immWant = (int)ImmigrationLaw.Open;   // 外圧
            StepLawToward(w, p, PolicyAxis.ImmigrationRestriction, (int)p.ImmigrationRestriction, immWant, (int)ImmigrationLaw.Closed,
                v => p.ImmigrationRestriction = (ImmigrationLaw)v);

            // 国教：支配ブロックの教義（DoctrineFrom＝発言権の実体）から。大衆支配＝信仰の自由（反教権・十分の一税の廃止圧力）・
            //   近代エリート支配＝内心の自由（商業社会の実利＝識字移民・宗派と取引しない）・旧支配層支配（Balanced/Militarist）＝現状維持（国教の防衛）。
            //   上げ目標のみ（Max）＝教義の揺れで宗教反動の台本を書かない（後退は断裂の窓など既存機構に委ねる）。
            //   プロレタリア独裁は書かない＝国家無神論は政体の属性（Government.Step が毎期強制・LawPrereq「プロレタリア独裁のみ」）。
            if (w.Polity != Polity.ProletarianDictatorship)
            {
                int relWant = DoctrineFrom(w) switch
                {
                    Doctrine.Modernizer => (int)StateReligionLaw.ReligiousFreedom,
                    Doctrine.Industrialist => (int)StateReligionLaw.InnerFreedom,
                    _ => (int)p.StateReligion,   // 伝統支配＝現状維持（開いた戦線も新設しない）
                };
                relWant = System.Math.Max(relWant, System.Math.Min((int)p.StateReligion, (int)StateReligionLaw.ReligiousFreedom));   // 単調＝下げ目標を書かない（政体離脱後の残存 StateAtheism は上限クランプが2へ畳む）
                StepLawToward(w, p, PolicyAxis.StateReligion, (int)p.StateReligion, relWant, (int)StateReligionLaw.ReligiousFreedom,
                    v => p.StateReligion = (StateReligionLaw)v);
            }
        }
    }

    /// <summary>★E4（選挙doc §3-2b「AIも同じ択を持つ＝綱領選好から自動で決める」・作者裁定2026-08-17「AI・プレイヤー完全等価」）：
    /// 総選挙の公示（<see cref="World.PendingElection"/>）へのAIの答え。**プレイヤーと同じ動詞**（<see cref="World.ResolveElection"/>）を叩く
    /// ＝加担の代価（余白判定・正統性の支払い・押し込みの記帳）は1つの関数に閉じている（別経路・別式を作らない）。
    ///
    /// <para><b>選好（どの思想に加担するか）</b>＝AIが既に読んでいる量だけから導く（新しい観測を作らない・固有名詞/年号/IsPlayer分岐なし）：</para>
    /// <list type="bullet">
    /// <item>支配ブロックの立場＝<see cref="PolicyPreferences.Stance"/> の3ブロック成分のうち、
    ///   <see cref="DoctrineFrom"/>（支配ブロック＋軍国度から創発する教義）が指す1つ。
    ///   伝統/軍国＝旧支配層・資本主義＝近代エリート・近代化＝大衆。看板議題（法軸＋向き）への賛否そのもの＝「綱領選好」。</item>
    /// <item>与党族との一致＝<see cref="World.GoverningParty"/> と看板思想の族（<see cref="PoliticalQuantities.FamilyOf"/>）が同じなら
    ///   <see cref="AiElectionIncumbentAffinity"/> を上乗せ＝自派の候補を推す。</item>
    /// </list>
    /// <para><b>レバー</b>（★E3''確定形2026-08-17）＝**裁量余剰の何%を注ぐか**＝軍事費・教育費と同じ物差しの上での配分判断。
    /// だからAIは「押したいか」ではなく<b>機会費用</b>で決める＝既にAIが持っている2つの量だけから：</para>
    /// <list type="bullet">
    /// <item><b>存亡の切迫</b>＝与党（族）の得票が不信任線（<see cref="Coefficients.NoConfidenceThreshold"/>）にどれだけ近いか。
    ///   線の2倍の得票があれば0（平時）・線に触れれば1（倒れる寸前）＝**新しい閾値を置かず既存の線を物差しにする**。</item>
    /// <item><b>他の支出の逼迫</b>＝<see cref="FiscalStressOf"/>（国庫が維持費・利払い・行政・軍装備の請求書の何か月分か）。
    ///   1に近い＝軍も行政も払えていない＝選挙に注ぐのは狂気。</item>
    /// </list>
    /// <para>＝<c>レバー ＝ 切迫 ×(1−逼迫)÷(1+紛争記憶 MandateSpent)</c>。「平時は狂気・政権が倒れそうなら合理的」（裁定 §3-②）が
    /// 罰を1つも作らずに出る。⚠プレイヤーが同じ盤面で採るべき手と同じ量＝対称（別の式・別の口を持たない）。</para>
    /// <para><b>余白</b>＝マンデート余白 θ＝MandateWeight×(正統性−0.5) は**「そもそも動かせるか」の門としてだけ**使う
    /// （<c>price=|純支持| ≤ θ</c>＝ResolveElection が拒む線と同一＝無駄玉を撃たない）。押しの大きさは金の予算制約が決める＝二重に使わない。</para>
    /// <para><b>静観</b>＝支配連合の利になる看板が1つも無い／注ぐ額が裁量余剰の <see cref="AiElectionMinPush"/> 未満＝
    /// <c>ResolveElection(-1, 0)</c>（UIの「静観」ボタンと同じ呼び出し）＝公示は消え、素の得票で開票される。</para>
    /// <para>決定論＝RNGを引かない（世論・教義・与党・正統性・国庫だけの関数）。同点は支持の高い議題（配列の先）が勝つ＝安定。
    /// ⚠<see cref="Coefficients.ElectionEventEnabled"/>=0（点火前の値・現在は1.0）では呼び出し側（Parliament.Step）の分岐に入らず、ここも先頭で即 return＝1命令も走らない。</para></summary>
    public static void DecideElection(World w)
    {
        var c = w.Coef;
        if (c.ElectionEventEnabled <= 0 || w.PendingElection is not { } ev) return;   // ゲート1本に統一（点火でプレイヤーとAIが同時に有効）
        // 政治資本の余白＝ResolveElection が「動かせるか」に使うのと同じ量（別の物差しを持ち込まない）
        double margin = c.MandateWeight * (w.Legitimacy - 0.5);
        if (!(margin > 0)) margin = 0;   // NaNガードは否定形（Clamp/MaxはNaNを消さない）＝余白なし側へ倒す
        // ★機会費用でレバー（裁量余剰の何%）を決める＝新しい観測を作らない。
        //   ①存亡の切迫：与党族の得票が不信任線の2倍で0・線に触れて1（既存の線そのものを目盛りに使う＝新しい閾値を置かない）
        double danger = 0;
        if (c.NoConfidenceThreshold > 1e-9 && w.GoverningParty is >= 0 and < 3)
        {
            double share = w.VoteShares[w.GoverningParty];
            double d = 1.0 - (share - c.NoConfidenceThreshold) / c.NoConfidenceThreshold;
            if (d > 1.0) d = 1.0;
            if (!(d > 0)) d = 0;   // 否定形＝NaN（得票が未初期化）は平時側へ倒す
            danger = d;
        }
        //   ②他の支出の逼迫：国庫が月次義務（維持費・利払い・行政・軍装備）の何か月分か＝AIが緊縮/減税で読むのと同じ物差し
        double stress = FiscalStressOf(w);
        if (!(stress >= 0)) stress = 1.0;   // 否定形＝評価不能なら「逼迫している」側へ倒す（fail-closed＝選挙に注がない）
        if (stress > 1.0) stress = 1.0;
        //   ③紛争記憶（反対を押し切った履歴のストック）が厚い政府ほど政治工作に回す余力がない＝既存の政治資本の物差しに絡める
        //   ★レンジの実測（裁定S1・2026-09-21）＝**ここに書いてあった旧の予測「減衰が弱まる」は逆だった**（書き直し）。
        //     旧は青天井だが実測の S は 中央0.0041／最大0.1417＝1/(1+S)≈0.996＝この項は事実上働いていなかった。
        //     θ を退役して在庫だけの門にすると S は 中央0.1196（b 中央0.1804）＝1/(1+S)≈0.893＝**レバーが約10%渋る**＝ここで初めて効く。
        double spent = w.MandateSpent;
        if (!(spent > 0)) spent = 0;
        double lever = danger * (1.0 - stress) / (1.0 + spent);
        if (lever > 1.0) lever = 1.0;
        if (!(lever >= AiElectionMinPush)) { w.ResolveElection(-1, 0); return; }   // 平時＝1銭も注がない（静観も択）。NaNは否定形で静観へ
        int bloc = DoctrineFrom(w) switch { Doctrine.Industrialist => 1, Doctrine.Modernizer => 2, _ => 0 };   // 教義→支配ブロック（DoctrineFromの逆写像＝新しい判定ではない）
        int bestK = -1; double bestU = 0;
        for (int k = 0; k < ev.AgendaIdeologies.Length; k++)
        {
            var (trad, modern, mass, net) = PolicyPreferences.Stance(w, ev.AgendaAxes[k], ev.AgendaIncrease[k]);
            double align = bloc == 0 ? trad : bloc == 1 ? modern : mass;   // 支配ブロックの賛否＝加担したい度合い
            if (PoliticalQuantities.FamilyOf(ev.AgendaIdeologies[k]) == w.GoverningParty) align += AiElectionIncumbentAffinity;
            if (!(align > 0)) continue;   // 支配連合の利にならない看板には加担しない（NaNもここで落ちる）
            // ⚠純支持は**代価**として読む（意図としてではない）＝「通る法しか望まない」トートロジーを持ち込まない。
            //   意図は上の align（ブロックの立場）が担い、ここは ResolveElection が実際に請求する正統性と同一。
            //   ⚠符号の分け方は ResolveElection と**同じ枝の形**（`net < 0`）＝非有限の純支持は両経路とも「無償」に落ちる＝対称
            //     （AI側だけ独自にNaNを弾くと、人が押せる看板をAIだけ押せない非対称になる）。
            double price = net < 0 ? -net : 0;
            if (!(price <= margin)) continue;   // 余白超え＝ResolveElection が拒む（世論に真っ向から逆らえない）。NaNは否定形で除外
            // 同じ額を注ぐなら効きは看板によらない（金の代価は曲がらない）＝選ぶ基準は「望みの強さ」と「食う正統性」だけ。
            // 余白をどれだけ食い潰すかで割り引く＝無償の看板（世論が既にその向き）は満額の魅力、余白すれすれの看板は魅力0。
            double afford = margin > 1e-9 ? 1.0 - price / margin : (price > 0 ? 0 : 1.0);
            if (!(afford > 0)) continue;
            double u = align * afford;
            if (u > bestU) { bestU = u; bestK = k; }
        }
        if (bestK < 0) { w.ResolveElection(-1, 0); return; }   // 静観＝プレイヤーの「静観」と同じ呼び出し（公示は消える）
        w.ResolveElection(ev.AgendaIdeologies[bestK], lever);
    }

    public void DecideQuarter(World w)
    {
        // 金本位は**発展した交易経済の制度**＝AIは工業化が一定以上で志向（信用→安い外資・貿易円滑の実利が金の足枷の損を上回る）。
        // 未発展の農業経済では足枷（切下げ封印→危機で underfunding）の損が勝るので志向しない＝1789から即移行する不自然を解消（金本位era=19c後半が工業化から創発）。実加入は信用ゲートが決める。
        // ★3値化（銀本位doc G4・2026-08-13）：★金は工業化した交易経済だけが持てる／⚠銀は誰でも持てる（銀貨は普遍的だった＝清もインドも独も1783年に銀）。
        // ⚠国名も暦年も書かない——工業化という既存の量だけが段を決める。SilverStandardGate=0（旧既定・点火1.0=2026-08-21銀本位ラウンド）では中段が消え旧2値と厳密一致。
        // ★2026-08-21点火後の形：金閾3.5＝前線だけが自力で金を志向（1853年前後）。残りの金への移行は MonetaryPeerPullScale の交易網の引力が
        //   **政治側**（PolicyPreferences→議会の要求/強制施行）から運ぶ＝この式は「自力の願望」のみを書く（願望と政治の分業）。
        // ★前線比（2026-09-06・銀本位の史実度 doc §6#1＝「全球収束の世界では絶対閾が氾濫する」）：GoldStandardFrontierRatio>0 では
        //   「自国の工業化 ≥ 比×世界の先端」で金を志向する（先端との距離＝交易網への参加資格）。0＝従来の絶対閾（厳密恒等）。
        //   ★先端＝World.IndustrialFrontier（2026-09-26：同じ最大の2つ目の欄を1本へ＝助走の直後の最初の四半期だけは0＝絶対閾へ倒れる・この枝は休眠）。
        // ★金の国は自分から金を降りない（作者裁定 2026-09-28 F6 の2歩目）：史実で金から銀へ法で戻った国は無い。離脱は危機と政治の側が持つ
        //   （兌換停止＝Fiscal の OnGoldStandard／法を下げる要求＝PolicyPreferences の債務者の側→議会・蜂起の強制施行 Politics）。
        //   ⚠これが無いと、初期値で金に置いた英（工業化が閾値に遠い）は最初の四半期に銀へ戻る。
        bool goldWish = w.MonetaryLaw == MonetaryStandardLaw.Gold
            || (w.Coef.GoldStandardFrontierRatio > 0 && w.IndustrialFrontier > 0
                ? w.MeanIndustrialization >= w.Coef.GoldStandardFrontierRatio * w.IndustrialFrontier
                : w.MeanIndustrialization > w.Coef.GoldStandardIndThreshold);
        w.Policy.MonetaryTarget = goldWish ? 2
                                : w.Coef.SilverStandardGate > 0 && w.MeanIndustrialization >= w.Coef.SilverStandardIndThreshold ? 1 : 0;
        // ★資本移動（トリレンマdoc・2026-08-13）：国庫の立場は状況で割れる——**借りたいときは自由・正貨が逃げているときは規制**。
        // 逃避の測り方は既存の量だけ＝正貨準備カバー率が兌換維持の下限を割っている（＝金が出ていく）。★史実＝1931年独の為替管理は
        // 金流出が止まらなくなってから敷かれた。⚠実際に法が通るかは資本の反対（PolicyPreferences）が決める＝プレイヤーと同じ政治ゲート。
        // CapitalMobilityLawGate 点火1.0（2026-08-13）＝live。0 で旧式（法も openness も動かない）。
        if (w.Coef.CapitalMobilityLawGate > 0)
        {
            double cover = w.BroadMoney > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            w.Policy.CapitalControlTarget = cover < w.Coef.GoldCoverMin;
        }
        // ★AI第1波②（2026-08-16）：総動員のAI分岐＝プレイヤーと同じレバー（World.Mobilized＝Military.cs:128 の徴兵深度0.12→0.22＝徴兵法≥選抜の国だけ・M1 2026-09-19）。
        // 入れる条件＝交戦中かつ（陣営戦力比で劣勢 or 敗色が深い＝存亡）。判定材料は既存量のみ（Diplomacy.SideMilitary＝同盟込み陣営戦力・war.Score）。
        // 交戦中で条件を満たさない間は現状維持＝戦中のON/OFFチャタリングを作らない（プレイヤーも一度入れたら講和まで戻さないのが実際の運用）。
        // 復員＝参戦中の戦争が消えた最初の四半期に false。プレイヤーは GameViewModel.Refresh（WarRows が空になった時）が講和で復員＝経路は分離・挙動は対称
        //（SimCore の講和処理 Diplomacy.EndWar には復員が無く、本ラウンドは Diplomacy.cs 不可侵＝AI側の唯一の書き手であるここに置く。設計判断は docs/20260816_AI第1波実装.md）。
        // ⚠NaNの war.Score/戦力は比較が偽に落ちる＝動員しない（fail-closed）。
        // ★軍事費の段（作者裁定2026-09-12 R2）＝最上段「戦時」は World.Mobilized が**解禁する**（M2 2026-09-19＝押し上げない・段は Apply が選ぶ）。その書き手はプレイヤー側3経路と
        //   ここ（AI）だけ＝ここが無いと**段4がプレイヤー専用になる**（AI＝プレイヤー等価の直接違反）。旧の2本のゲート（AiMobilization／FiscalLawStages）は
        //   2026-09-20 の段の点火で係数ごと削除＝この分岐は常時 live（戦時段とAI総動員は1テーマ）。
        //   ⚠判定材料・閾値は1つも増やさない（既存の MobilizeOutmatchRatio / MobilizeLosingScore をそのまま使う）。
        // ★位置（2026-09-19 M2）：Apply の**前**＝総動員した同じ四半期に Apply が戦時段を選べる（LawPrereq の「総動員が前提」が同じ四半期に真）。
        //   後ろ（旧位置）ではAIだけ1四半期遅れる＝プレイヤーは同じポーズ中にトグルと段ボタンを両方押せる（反応速度の非対称）。
        if (w.SelfPower is { } mSelf && w.Diplomacy is { } mDip)
        {
            bool mAtWar = false, mOutmatched = false; double mLosing = 0;
            foreach (var war in mDip.Wars)
            {
                Power? foe = war.A == mSelf ? war.B : war.B == mSelf ? war.A : null;
                if (foe is null) continue;
                mAtWar = true;
                mLosing = Math.Max(mLosing, Math.Max(0, war.A == mSelf ? -war.Score : war.Score));
                if (mDip.SideMilitary(foe, mSelf, w.Coef) > mDip.SideMilitary(mSelf, foe, w.Coef) * MobilizeOutmatchRatio)
                    mOutmatched = true;
            }
            if (!mAtWar) w.Mobilized = false;                                          // 講和＝復員（既存機構＝徴兵深度が平時へ・定員を超えた兵は転職の仕組みが民間の職へ戻す＝Meritocracy の ⑦・(155)）
            else if (mOutmatched || mLosing > MobilizeLosingScore) w.Mobilized = true; // 劣勢/存亡＝総動員（levée en masse 1793・独ソの総力戦動員の型）
        }
        Apply(w, ChooseStance(w));
        // （借入は 2026-09-28 に Fiscal の1行＝国家の支払い率 r＜1 の月にエンジンが自動で借りる形へ＝ここの予約の2行〔r＜1 と恐慌の谷の前倒し AiCrisisBorrow〕を削除。作者裁定「借入は自動化・借金ボタンは不要」）
        // 通貨改革（2026-08-13）：**プレイヤーのボタンと同じ口**（World.RequestCurrencyReform）を押す。
        // AIは実力どおりの新平価を選ぶ（ポワンカレ型）＝旧平価に戻すという自傷は**プレイヤーだけが選べる選択肢**。
        // 機構は対称で、判断だけが違う（巨大建造物と同じ作法）。ゲートは Fiscal が持つので、ここは意思だけ。
        if (w.Coef.CurrencyReformEnabled > 0.5 && !w.OnGoldStandard && w.CurrencyReformCooldown <= 0
            && w.NominalGdp > 1e-9 && w.CumulativeDevaluation / w.NominalGdp > w.Coef.CapitalFlightDistrustThreshold
            && w.NoDevaluationMonths >= w.Coef.CurrencyReformCalmMonths)
            w.RequestCurrencyReform(w.ExchangeRate);
        // ★潤沢の減税（RelieveOpulence）は Apply の中＝門（GateAllLawTargets）の前へ移した（2026-09-28 (85)）。
        //   旧はここ＝門の**後**に税の目標へ連続の倍率を掛け、門が拒む段を置けた（AI だけ・プレイヤーは法律タブで門を通る）。
        // ★鋼市場診断#5（2026-08-16・AiProtectiveTariff>0のみ）：保護関税の反射。
        // ⚠**順序**＝関税目標を書く既存の3か所（Apply のスタンス定数／ダンピングの解除／RelieveOpulence の減税乗算）の**後**に置く。
        //   前に置くと同じ四半期の Apply が上書きし、反射が一度も効かない（衝突ではなく無効化になる）。
        //   逆に「条件時のみ書く」ので、条件が引けば翌四半期からは上の3か所が従来どおり単独で目標を決める＝恒久的な上書きにはしない。
        ReactProtectiveTariff(w);
        SetNavalUpkeepShare(w);// 陸海の維持費の割り振り（2026-08-04）＝陸と海の必要量が指針（(151)・旧＝地理）
        TryTradeBias(w);       // 財ごとの売買方針（2026-08-04）＝プレイヤーと同じ動詞（World.SetTradeBias）
        TryBuildFacility(w);   // 工場/プランテーションの着工（2026-08-04）＝プレイヤーと同じ動詞（2026-08-10から World.SetStateTarget＝国営工場カウンター）
        StartMegaproject(w);   // 巨大建造物の着工（2026-08-02）＝恐慌の失業対策／平時は国庫に余裕がある時だけ
        // ★侵略の標的選択は Diplomacy の TryAiSubjugate へ1本化（作者裁定2026-08-23の「植民地化と侵略の1本化」）。
        //   旧＝ここ（毎四半期・戦争熱で選ぶ）と Diplomacy（確率で・富と到達で選ぶ）の2経路が同じ動詞を叩いていた。
        // 02断線走査2026-07-28：再征服（武力解錠）のAI判断。分離/凍結州の唯一の復帰経路（Territory.cs）なのに書き手ゼロ＝分離が「州の恒久消滅」だった。
        // 国庫に余裕があり大衆闘争が低い時だけON（制圧は毎月の国庫出血＋全pop闘争度↑＝革命前夜に強行しない）＝南北戦争の再統合・カルリスタ戦争型。
        w.ForceUnlock = w.Regions.Any(r => r.State == RegionState.Locked)
            && w.Treasury > ForceUnlockBufferMult * FiscalBufferOf(w)
            && Politics.AvgMassMilitancy(w) < ForceUnlockMilitancyCeiling;
        // ※国家主導の工業化（AIが1棟単位の手動建設＝旧 BuildIndustry で後発を追い上げる）は実験した結果、ロシアが工業化すると
        //   規模(人口→GDP)で常勝になり競争バランスを壊したため不採用。再採用にはスコアの規模偏重の是正が前提。
    }

    /// <summary>国庫が潤沢なら減税する（2026-08-04・P4「国家の退蔵」の是正）。
    /// ── 実測 ──────────────────────────────────────────────────────────────
    /// natprobe/powertrace＝プロイセンの国庫1.3M（GDP約30四半期分）・米38M（128四半期分）・日15M。
    /// スタンスの税目標は固定率で、**国庫がいくら積み上がっても下げる反射が無い**＝黒字国は経済から貨幣を
    /// 吸い上げ続け、需要（＝貯蓄駆動）が痩せてデフレ螺旋に入る（プロイセンのGDP 34k→17k の主因）。
    /// 税は月次義務の OpulenceBufferQuarters 四半期分を超える国庫を「取り過ぎ」とみなし、超過に比例して
    /// スタンスの税目標を連続に縮める（グラッドストン流の減税）。⚠増税側はいじらない＝財政逼迫の既存反射（uFis）が担う。
    /// ⚠プレイヤー国（NullAgent）はこの関数を通らない＝プレイヤーの税は自由。OpulenceTaxRelief=0 で厳密恒等。
    /// ⚠呼び手は Apply の末尾（GateAllLawTargets の前）だけ＝縮めた目標も法律タブと同じ門で段へ整形される（2026-09-28 (85)）。</summary>
    private static void RelieveOpulence(World w)
    {
        if (w.Coef.OpulenceTaxRelief <= 0 || w.SelfPower is null) return;
        double buffer = OpulenceLineOf(w);   // R3安定形（2026-08-08）＝給与算入は潤沢判定のみ（緊縮反射は不変）
        if (!(buffer > 1e-9) || w.Treasury <= buffer) return;
        double mul = Math.Max(OpulenceTaxFloor, buffer / w.Treasury);   // 国庫が予備の2倍なら税目標×0.5（連続・床つき）
        mul = 1.0 - w.Coef.OpulenceTaxRelief * (1.0 - mul);             // 適用率でブレンド（1=満額）
        var p = w.Policy;
        p.IncomeTaxTarget *= mul; p.PollTaxTarget *= mul;
        p.LandTaxTarget *= mul; p.ProgressiveTaxTarget *= mul; p.TariffTarget *= mul;
        // ★専売はこの割引から**外す**（2026-09-20・F3の是正）。専売の広さの規律は Apply の末尾（今の広さから・教義の床・安全水準で1段狭める＝2026-09-30 (127)）が持つ。
        //   旧は `SetStateMonopolyTier(Tier-1)` で段を丸ごと1つ落としていたが、
        //   この関数の操作は「率に 0.25 の床つき連続倍率を掛ける」＝取り過ぎの分だけ薄く下げ**決して無税にしない**もので、
        //   段を落とす翻訳はそれと等価でない：(a) 潤沢の度合いに比例しない (b) 床が無い (c) Apply が毎期教義の段を書いた直後に走る
        //   ＝定常で恒久的に教義−1段。教義段1の国（Modernize／Welfare）は毎期0へ落ちた。
        //   実測＝1914年に128カ国中76カ国（59.4%）が1財も課税しない世界になっていた（旧の率の世界では123カ国中0）。
        //   史実＝英の excise・仏の gabelle・清の塩引・露の酒専売＝1783-1934 に専売を全く取らない国は事実上存在しない。
        //   ⚠新しい床（「段1未満へは落とさない」）は作らない＝それは新しい天井の裏返し（思想⑧）。
        //   ⚠退蔵デフレの是正（2026-08-04 のこの関数の目的）は所得税・人頭税・土地税・累進・関税の5本が従来どおり担う＝instrument は失わない。
    }
    internal const double OpulenceBufferQuarters = 8.0;   // 月次義務の8四半期分＝2年分の戦費予備を超えたら「取り過ぎ」
    private const double OpulenceTaxFloor = 0.25;        // 減税の床（無税国家にはしない＝行政・軍の最低限は回る）

    /// <summary>★鋼市場診断#5（2026-08-16・<see cref="Coefficients.AiProtectiveTariff"/>>0 のみ）：**保護関税の反射**＝防衛側の第二幕。
    /// 診断B-3の逆インセンティブ（#2で参入側を反転させた）に対して、こちらは「安い輸入に押されている国が壁を作る」側＝
    /// 史実「安い英国鋼→独米が製鉄所<b>と関税</b>」の後半分（1879年独の鉄と穀物の関税・1861年米モリル関税）。
    /// これが secondDeliver（次善の売り手の着値＝成約単価）の底上げとして働き、先行者レントを外から削る。
    /// ── 判定は既存の量だけ（新しい観測を作らない）──
    ///  (1) <b>輸入急増</b>＝<see cref="World.ImportsByGood"/>（BilateralMarket が決済ごとに書く国際貿易統計）が自分の記憶（EMA）の
    ///      <see cref="AiTariffSurgeRatio"/>倍を超える。EMAは**AI個体の記憶**（<c>_importEma</c>）＝Worldの力学状態を1ビットも増やさない。
    ///  (2) <b>規模の床</b>＝輸入額（清算単位＝月次清算では直近1か月分・四半期に1回読む）が名目GDP×<see cref="AiTariffImportFloorShare"/>超（規模中立＝微小財の雑音で関税を動かさない）。
    ///  (3) <b>自国財が赤字</b>＝その財の国内生産者の採算（<c>SmoothedMargin</c>＝倒産判定と同じ平滑値）が生産規模の加重平均で負
    ///      ＝「押されている」の実体。★輸入急増だけでは足りない＝国内に産業が無い財（原料・熱帯産品）の輸入増で壁を作らない。
    /// ── 書き方 ──
    /// <c>TariffTarget = max(現行目標, 現在の関税 + TariffReactionMax×これ)</c>。振幅は既存の通商反応の較正済み量（0.25）を流用し、
    /// 実効上限は既存の <c>Government.Move</c> のクランプ [TariffFloor,1] が握る＝**新しい上限も新しい振幅も作らない**。
    /// 基点を「現在の関税」に取るので目標は w.Tariff+0.25α で有界＝毎四半期発火してもラチェットで発散しない。
    /// ⚠<b>条件時のみ書く</b>＝関税スライダーは <c>CountOpenFronts</c> の対象（Government.CountOpenFronts の Front）なので、
    ///   毎期無条件に書くと幽霊戦線を1本増やし全軸の政治押し予算 (1/N)^0.5 を薄める。条件が引けば戦線は自然に閉じる
    ///   ＝Move が w.Tariff を目標へ寄せる（差が5%量子化を割れば戦線から外れる）／産業・軍備スタンスは翌期に自分の定数で目標を書き直す。
    /// ⚠0＝EMAの確保も更新もしない＝厳密恒等（否定形ガード＝NaNも休眠側）。単一国シム（<see cref="World.ImportsByGood"/>=null）は不発＝較正・単体テスト不変。
    /// ★セーブ（設計判断・確認済み）：<b>専用処理は要らない</b>。<see cref="SaveGame"/> は <see cref="MultiNationSim"/> から
    ///   全インスタンスフィールド（private/readonly含む）を辿るので、Simulation._agent → この UtilityAgent → _importEma（double[]）まで
    ///   自動で往復する＝点火後も savetest はビット一致（AI個体の記憶を World の力学状態へ移す必要はない）。
    ///   初回（と財数の変化）は現在値で初期化＝比1.0＝「いきなり急増」と誤認しない。</summary>
    private double[]? _importEma;   // 財別・輸入額（清算単位）のEMA（AI個体の記憶・四半期に1回更新）。係数0では確保すらしない
    private void ReactProtectiveTariff(World w)
    {
        if (!(w.Coef.AiProtectiveTariff > 0)) return;          // 0＝以下を一切実行しない（否定形＝NaNも休眠側）。★既定は点火1.0＝この枝は通らない
        if (w.ImportsByGood is not { } imp) return;            // 多国シムの決済がまだ／単一国＝材料なし
        if (_importEma is not { } ema || ema.Length != imp.Length)
        {
            _importEma = (double[])imp.Clone();                // 初回（と財数の変化）＝現在値で初期化＝比1.0＝急増と誤認しない
            return;
        }
        double floor = AiTariffImportFloorShare * w.NominalGdp;
        bool surge = false;
        for (int g = 0; g < imp.Length; g++)
        {
            double cur = imp[g], prev = ema[g];
            ema[g] = prev + AiTariffEmaRate * (cur - prev);    // 記憶は毎期更新（判定の成否に依らない＝観測と判断を分ける）
            if (surge) continue;                                // 判定は済み。ただしEMAは全財ぶん更新しきる
            if (!(cur > floor)) continue;                       // 規模の床（否定形＝NaNは急増と数えない）
            if (!(prev > 1e-9)) continue;                       // 記憶が無い財は比が無限＝急増と数えない（初回輸入の雑音）
            if (!(cur > prev * AiTariffSurgeRatio)) continue;    // 対EMA比＝「前年比」の最小実装
            if (!DomesticMarginNegative(w, g)) continue;         // 自国財が押されている（産業の無い財では壁を作らない）
            surge = true;
        }
        if (!surge)
        {
            // ★案A（AiTariffRelaxation・作者裁定2026-08-31）：急増の無い平時は目標を漸減＝保護の波→自由化の波の対称化。
            //   旧は「下げはしない」＝恐慌45回/試合の世界で全員が関税100%へ歩く片道ラチェットだった（tariffprobe実測）。
            //   振幅は上げ側と同じ TariffReactionMax（新しい数を作らない）。0＝旧の片道＝厳密恒等。
            if (w.Coef.AiTariffRelaxation > 0 && w.Policy.TariffTarget > 0)
                w.Policy.TariffTarget = System.Math.Max(0, w.Policy.TariffTarget - w.Coef.TariffReactionMax * w.Coef.AiTariffRelaxation);
            return;
        }
        // ★2026-09-04（tariffprobe実測）：目標が110〜125%へ出ていた＝Move の実効クランプ[TariffFloor,1]は関税を切るが**目標は切らない**ので、
        //   平時の漸減（TariffReactionMax×AiTariffRelaxation=0.025/四半期）が1.0を割るまで最大10四半期を空費し、自由化が2.5年遅れていた。
        //   目標も実効と同じ上限1で切る（新しい数ではない＝Move と同じクランプ）。
        double want = System.Math.Min(1.0, w.Tariff + w.Coef.TariffReactionMax * w.Coef.AiProtectiveTariff);
        if (want > w.Policy.TariffTarget) w.Policy.TariffTarget = want;   // 急増時＝上げ（対動詞＝上の平時漸減）
    }

    /// <summary>#5の材料：その財の国内生産者の採算が**生産規模で加重した平均**で赤字か。
    /// 加重＝Scale（現在の操業規模）＝止まっている棟の採算に世論を作らせない。倒産中は除外（既に退出している）。
    /// 生産者が1人もいない財は false＝「自国財でない」＝壁を作らない。NaN採算は num&lt;0 が偽＝fail-closed。</summary>
    private static bool DomesticMarginNegative(World w, int goodId)
    {
        double num = 0, den = 0;
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            foreach (var pr in rg.Producers)
                if (pr.GoodId == goodId && !pr.Bankrupt)
                {
                    double sc = Math.Max(0, pr.Scale);
                    num += pr.SmoothedMargin * sc; den += sc;
                }
        }
        return den > 1e-9 && num < 0;
    }

    /// <summary>陸海の維持費の割り振り（2026-08-04）。合計は各スタンスが決めるので、ここは**比だけ**を置く。
    /// ★2026-10-01 (151)（作者裁定 2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」）：指針は<b>陸と海の必要量</b>
    ///   ＝<see cref="ArmyShape.NavalSpendShareOf"/>（陸＝国境の長さ×地形×隣国の陸軍・同盟は0・属国は弱く＋国内の秩序／海＝艦隊の比と海路の接触＝海を越えて来る陸軍・(183) で守る海の交易を外した）。
    ///   旧＝地理（外洋に開けば+0.10・植民地の質量×0.05・下限0.05・上限0.60＝GeoNavalShare）＝陸続きの植民地格の州まで海軍に数え、清は上限の0.60に張り付いた。
    ///   必要量がまだ一度も算出されていない（−1）ときは今の比のまま。
    ///   海軍シェアは作者仕様(5)で「払った金の比」になったので、必要量は**AIとプレイヤーが金をどう割るかの指針**。
    /// ⚠これを配線し忘れていたため、全AI国が初期値10%の海軍シェアに永久固定され、1914年の海軍上位8国が
    ///   全てアジア・**英の艦隊が圏外**という実測になっていた（2026-08-04の実バグ・作者仕様(5)の実装漏れ）。
    /// ★2026-09-23（作者裁定「海軍の比率のレバーを撤去し、プレイヤーもAIと同じ地理由来の配分に乗せる」）：**プレイヤー国もこの関数を通る**。
    ///   呼び手は Simulation.QuarterStart（IsPlayer の枝＝NullAgent には DecideQuarter が無いため）＝AI とまったく同じ関数・同じ引数。
    ///   AI 国の経路（DecideQuarter の中のこの呼び出し）は1命令も変えていない＝AI挙動はビット恒等。internal＝同一アセンブリからの呼び出しのため。
    /// ⚠勢力の要求「海軍の増強」（PolicyAxis.NavyUpkeep・2026-08-10）とは**衝突しない**：要求は海軍維持費を上げる＝
    ///   合計を押し上げ、ここは合計を保ったまま比だけを置く。AIでは「要求で金が増え、割り方は陸と海の必要量」となる。</summary>
    internal static void SetNavalUpkeepShare(World w)
    {
        if (w.SelfPower is not { } sp || w.Diplomacy is null) return;   // 単一国（テスト・較正）では走らせない
        double share = ArmyShape.NavalSpendShareOf(sp);
        if (!(share >= 0)) return;   // 必要量が未定義（理想の形が一度も算出されていない）＝今の比のまま（否定形＝NaN も）
        var p = w.Policy;
        double total = PoliticalQuantities.Clamp01(p.ArmyUpkeepTarget + p.NavyUpkeepTarget);
        p.NavyUpkeepTarget = total * share;
        p.ArmyUpkeepTarget = total - p.NavyUpkeepTarget;
    }

    /// <summary>財ごとの売買方針（2026-08-04・作者仕様(3)「AIも使用できるように」）。
    /// ★プレイヤーと**同じ動詞**（World.SetTradeBias）を通る＝国庫負担も効果も完全に同条件。
    /// 判断は既存の物質量だけ＝(1)**足りない財**（在庫が目標を大きく割る）は「高く買う」で引き込む
    ///   (2)**だぶついた財**（在庫が目標を大きく超え、かつ国内で売れ残っている）は「安く売る」で放流する。
    /// 財政が逼迫していれば何もしない（どちらも国庫を食うので、借金してまでやる手ではない）。
    /// 1四半期に全財を見直す＝決定論（RNGなし・財id順）。</summary>
    private static void TryTradeBias(World w)
    {
        if (w.SelfPower is null) return;   // 単一国（テスト・較正）では走らせない＝較正に無影響
        if (w.Coef.TradeBiasStrength <= 0) return;
        bool broke = FiscalStressOf(w) > TradeBiasStressCeiling;
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            if (broke) { if (w.TradeBiasOf(g) != 0) w.SetTradeBias(g, 0); continue; }   // 金が無い時は全部畳む
            double stock = 0, target = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || g >= r.Markets.Length) continue;
                stock += r.Markets[g].Stock; target += r.Markets[g].TargetStock;
            }
            if (!(target > 1e-9)) { if (w.TradeBiasOf(g) != 0) w.SetTradeBias(g, 0); continue; }
            double ratio = stock / target;
            int want = ratio < TradeBiasShortageRatio ? 1 : ratio > TradeBiasGlutRatio ? -1 : 0;
            if (want != w.TradeBiasOf(g)) w.SetTradeBias(g, want);
        }
    }
    private const double TradeBiasStressCeiling = 0.4;   // 財政逼迫がこれを超えたら売買方針を畳む（国庫を食う手なので）
    private const double TradeBiasShortageRatio = 0.5;   // 在庫が目標の半分を割ったら「高く買う」
    private const double TradeBiasGlutRatio = 2.0;       // 在庫が目標の2倍を超えたら「安く売る」

    /// <summary>「この州にこの財の工場を建てる価値」＝<b>AIとプレイヤーが共有する唯一の物差し</b>（2026-09-07に括り出し）。
    /// <para><b>Excess</b>＝実質相対価格の超過＝ <c>Price ÷ (RefPrice×物価水準) − 1</c>。高い＝供給が需要に追いつかない
    /// ＝連鎖の律速点（市場自身が示すボトルネック。生産タブの赤字と同じ現象の価格側）。真の律速が上流にあるなら上流の財ほど
    /// 相対価格が高く出る（自分の材料は安いまま）＝この最大化は自然に連鎖を遡って律速点を選ぶ。</para>
    /// <para><b>WorldOpened</b>（鋼市場診断#2・AiWorldPriceEntry&gt;0のみ）＝世界の着値（<see cref="World.WorldGoodsPrices"/>＝
    /// 265州平均）を自国の費用錨と比べた超過が国内チャネルを上回った＝「世界へ売れるレント」で開いた候補。旧は自国の局所価格だけを読むため
    /// 「安い輸入が来るほど建てない」＝史実（安い英国鋼→独米が製鉄所+関税）の真逆になっていた。輸送楔は TransportMargin を流用。</para>
    /// <para><b>Score</b>＝<c>Excess ×(1 + 失業の重み×失業率)</c>＝不足の深さ×人が余っている（同じ不足なら人の余る州を選ぶ）。</para>
    /// ★読み手は2つ＝AIの着工判断（TryBuildFacility）と、プレイヤーが置く全国工場目標の展開
    ///   （World.StepFactoryTargets）。**第二の実装を作らない**＝州の選び方がAIとプレイヤーで食い違わない。
    /// ★基準価格や市場配列の範囲外＝測れない候補は Score/Excess とも NaN（比較は必ず偽＝fail-closed）。</summary>
    public readonly record struct FacilityBuildValue(double Score, double Excess, bool WorldOpened);

    public static FacilityBuildValue FacilityBuildScore(World w, Region rg, Producer pr)
    {
        if (pr.GoodId < 0 || pr.GoodId >= Catalog.RefPrices.Length || pr.GoodId >= rg.Markets.Length) return new(double.NaN, double.NaN, false);
        double rp = Catalog.RefPrices[pr.GoodId] * Math.Max(rg.PriceLevel, 1e-9);
        if (!(rp > 1e-9)) return new(double.NaN, double.NaN, false);
        double eff = rg.Markets[pr.GoodId].Price / rp - 1.0;
        bool worldOpened = false;
        if (w.Coef.AiWorldPriceEntry > 0 && w.WorldGoodsPrices is { } wgp && pr.GoodId < wgp.Length)
        {
            double worldRel = wgp[pr.GoodId] / rp - 1.0 - w.Coef.TransportMargin;
            if (worldRel > eff) { eff = worldRel; worldOpened = true; }   // NaNは比較で偽＝開かない（fail-closed）
        }
        double idle = 0;
        if (pr.LaborPopIndex >= 0 && pr.LaborPopIndex < rg.Pops.Count)
        {
            var lp = rg.Pops[pr.LaborPopIndex];
            if (lp.Size > 1e-9) idle = PoliticalQuantities.Clamp01(lp.UnemploymentRate);
        }
        return new(eff * (1.0 + FacilityBuildIdleWeight * idle), eff, worldOpened);
    }

    /// <summary>工場・プランテーションの着工（2026-08-04・作者指示「AIも有用な場合、工場やプランテーションを建設するように」）。
    /// ★プレイヤーと**同じ動詞**（2026-08-10から <see cref="World.SetStateTarget"/>＝国営工場カウンターを1つ上げる）を通る
    ///   ＝建材の高値ゲートも工期も国庫負担も完全に同条件。旧＝BuildFactory はUIからしか呼ばれず、
    ///   **プレイヤーだけが持つ動詞**だった（AI=プレイヤー対称の破れ）。カウンター化でUIの動詞も1本に畳まれた。
    /// 判断は既存の物質量だけ＝(1)その財が儲かっているか（SmoothedMargin＝採算の平滑値・生産者自身が持つ）
    ///   (2)その州にその財の働き手が余っているか（失業＝建てても人が集まる＝プレイヤーがUIで見ているのと同じ材料）。
    /// 1四半期に1件・決定論（同点は州順→財id順）。財政逼迫時は建てない（借入と同じ規律）。</summary>
    private static void TryBuildFacility(World w)
    {
        if (w.SelfPower is null) return;   // 単一国（テスト・較正）では走らせない＝較正に無影響
        // ★鋼市場診断#3(b)（2026-08-16・AiBuildGuardRelax>0のみ）：財政ガードの緩和＝旧「FiscalStress>0.0で即return」（国庫がバッファ満額でないと建てない）を
        //   **借入と同じ規律**（Fiscal の借入の条件＝国家の支払い率 r＜1 の時だけ止まる・2026-09-28＝旧は維持費＋利払いを国庫で賄えない時）へ。否定形＝NaNは建てない側。
        //   ⚠2026-08-04の前科（0.5緩和→工事の日次出費が国庫を吸い軍事独裁5%→34%＝FacilityBuildStressCeiling の注記）＝当時は月次出費の律速が未実装だった。
        //   いまは下の FacilityBuildOutlayShare が本命の予算制約として残る。緩和は必ずこのフラグ経由・**点火A/Bで軍事独裁の頻度（PolityDistribution）を必ず見る**。
        bool relax = w.Coef.AiBuildGuardRelax > 0;
        if (relax ? !(w.FiscalFundingRatio >= 1) : FiscalStressOf(w) > FacilityBuildStressCeiling) return;
        // ★工事の未払残高で律速する（2026-08-04の是正）。★初版は「1四半期に1件」を無条件に積み、
        //   BuildFactory 側のガードが**初日の労務費だけ**だったため、工期4〜28四半期の工事が際限なく重なり
        //   国庫が日次で流出し続けた。実測（R16）＝軍事独裁が 5.0%→53.4%＝**世界中で国家が財政破綻して軍が政権を取った**。
        //   プレイヤーは自分で押すので暴走しないが、AIは毎期押す＝同じ動詞でも**頻度が違う**＝ここで抑える必要がある。
        //   物差しは既存の財政バッファ（月次義務の N か月分・規模中立）＝新しいスカラーを置かない。
        double buffer = FiscalBufferOf(w);
        double monthlyBuild = 0;
        int pendingWill = 0;   // ★国営工場カウンターの**未達の意志**（2026-08-10）＝目標>現在の棟数。建材高騰で着工が拒まれた月も目標は残る（意志は消えない）＝これも律速に数える
        int[]? pendingRegion = relax ? new int[w.Regions.Count] : null;   // ★#3(a)：州ごとの意志（relax時のみ確保＝既定0では読み書きゼロ）
        for (int ri0 = 0; ri0 < w.Regions.Count; ri0++)
        {
            var rg0 = w.Regions[ri0];
            if (rg0.State != RegionState.Active) continue;
            foreach (var pr0 in rg0.Producers)
            {
                if (pr0.ConstructionLeft > 0)
                {
                    monthlyBuild += pr0.ConstructionPayPerDay * Calendar.DaysPerMonth;
                    monthlyBuild += World.ConstructionMaterialPerMonth(rg0, pr0, w.Coef);   // ★裁定②の安全弁＝素材も律速に数える（2026-09-27 焼き込み＝工事費の6割が律速から漏れていた）
                }
                int pw = Math.Max(0, pr0.StateTarget - World.StateUnits(pr0, w.Coef));
                pendingWill += pw;
                if (pendingRegion is not null) pendingRegion[ri0] += pw;
            }
        }
        // ★鋼市場診断#3(a)（2026-08-16・AiBuildGuardRelax>0のみ）：旧＝全国のどこかに意志が残っていれば**全国 return**＝世界中のAI国が「同時1件」
        //   （四半期1回と合わせて年4棟が上限＝AI資本の参入が構造的に鈍い＝診断B-3の四重ガードの一角）。relax＝州ごとの判定へ
        //   （下の候補ループで意志の残る州だけ静止・他州は並行着工）＝ラチェット防止の性格（意志は溜めない）は州単位で保つ。
        if (!relax && pendingWill > 0) return;   // 前に立てた意志がまだ実現していない＝新しい意志を積まない（旧「1四半期に1件」の性格を保つ＝ラチェットを作らない）
        double outlayCap = FacilityBuildOutlayShare * buffer;
        if (monthlyBuild > outlayCap) return;   // 工事の月次出費が財政バッファを食い過ぎていれば新規着工しない（#3の緩和でも**残す**＝本命の予算制約）
        int bestR = -1, bestG = -1; double bestScore = 0;   // 閾値は relExcess > FacilityBuildMinMargin 側で判定（2026-08-04）
        int[]? bestGr = null; double[]? bestScoreR = null;   // ★#3(a)：relax時は州ごとの最良（並行着工の候補。既定0では確保しない）
        if (relax) { bestGr = new int[w.Regions.Count]; bestScoreR = new double[w.Regions.Count]; for (int i = 0; i < bestGr.Length; i++) bestGr[i] = -1; }
        void Consider(int cri, int gid, double sc)   // NaNスコアは両比較とも偽＝落ちる（否定形と同じfail-closed）
        {
            if (relax) { if (sc > bestScoreR![cri]) { bestScoreR[cri] = sc; bestGr![cri] = gid; } }
            else if (sc > bestScore) { bestScore = sc; bestR = cri; bestG = gid; }
        }
        // ★連鎖の目の読む個体（2026-10-01 (146) 計算量の予算・恒等＝全長の1種で連鎖の目 約1,340万回）：その州でその財を作る**先頭の生産者**
        //   （ProductionDiag.ExplainCost の規約＝FindProducer）。この走査は州の生産者を先頭から回す＝先頭の個体は既に通った
        //   ＝州ごとに「財→先頭の個体」を覚えて渡す（旧＝候補ごとに州の生産者を先頭から探し直した＝連鎖の目の時間の約半分）。
        Producer?[]? firstOf = w.Coef.AiSupplyChainEyes > 0 ? new Producer?[Catalog.Goods.Length] : null;
        for (int ri = 0; ri < w.Regions.Count; ri++)
        {
            var rg = w.Regions[ri];
            if (rg.State != RegionState.Active) continue;
            if (pendingRegion is not null && pendingRegion[ri] > 0) continue;   // ★#3(a)：意志の残る州だけ静止（他州は動く）
            if (firstOf is not null) System.Array.Clear(firstOf);
            foreach (var pr in rg.Producers)
            {
                if (firstOf is not null && pr.GoodId >= 0 && pr.GoodId < firstOf.Length) firstOf[pr.GoodId] ??= pr;   // 先頭の個体（下の continue より前＝建設中・倒産中も数える＝FindProducer と同じ）
                if (pr.ConstructionLeft > 0 || pr.Bankrupt) continue;   // 建設中・倒産中は対象外
                if (pr.GoodId < 0 || pr.GoodId >= Catalog.Goods.Length) continue;
                if (Catalog.Goods[pr.GoodId].Tier == GoodTier.Service) continue;   // 役務は工場として建たない（BuildFactory と同条件）
                var val = FacilityBuildScore(w, rg, pr);
                if (double.IsNaN(val.Score)) continue;   // 基準価格・市場の範囲外＝候補外
                double eff = val.Excess;
                bool worldOpened = val.WorldOpened;
                // ★鋼市場診断#4（2026-08-16・AiSupplyChainEyes>0のみ）：連鎖の律速を読む目。需要シグナル（eff>閾値）のある候補財について
                //   ProductionDiag.ExplainCost（純関数＝力学の状態を1ビットも書かない・従来は生産タブUIだけの読み手）を読み、
                //   配給率（CostItem.Fill＝Market.LastRation＝プレイヤーが生産タブの「供給:n%」で見るのと同じ値＝AI/プレイヤー対称）が
                //   最も低い非労働投入が AiChainStarvedFill を割っていれば、その**上流財を候補に足して優先**する
                //   （score＝下流eff×(1+係数×(1−fill))＞下流自身のscore＝律速点から建てる。StepLawToward が GateInfo を読むのと同じ作法）。
                //   上流には relExcess/SmoothedMargin の関門を課さない＝配給（fill<1）の事実が不足の証拠で、輸入錨や在庫クランプで
                //   価格シグナルが鈍る財こそこの目の出番（診断B-5「連鎖の逆算が実行不能」の是正）。身の丈ゲートは上流自身の工場費用で課す。
                //   労働3種（IsLabor）は建てられないので対象外。qty≈0の投入（電化の労働節約で消えた材）は律速と数えない。
                //   0＝ProductionDiag を一切呼ばない＝恒等。
                if (w.Coef.AiSupplyChainEyes > 0 && eff > FacilityBuildMinMargin)
                {
                    var chain = ProductionDiag.ExplainCost(w, rg, firstOf![pr.GoodId]!);   // ＝ExplainCost(w, rg, pr.GoodId) と同じ個体（(146)）
                    int starved = -1; double minFill = AiChainStarvedFill;
                    foreach (var it in chain.Items)
                        if (!it.IsLabor && it.Qty > 1e-9 && it.Fill < minFill) { minFill = it.Fill; starved = it.GoodId; }
                    if (starved >= 0 && starved < Catalog.Goods.Length && starved < Catalog.RefPrices.Length
                        && Catalog.Goods[starved].Tier != GoodTier.Service
                        && World.FindFactory(rg, starved) is { ConstructionLeft: <= 0, Bankrupt: false }
                        && Catalog.FactorySpec(starved).Cost <= FacilityBuildCostCap * buffer * BuildCapMul(w, starved))
                        Consider(ri, starved, eff * (1.0 + w.Coef.AiSupplyChainEyes * (1.0 - minFill)));
                }
                // ★身の丈ゲート（2026-08-04・R19の赤＝ジェノヴァ6/10最下位の是正）：FactorySpec の建設費は**絶対額**
                //   （軽工業3万〜弩級戦艦32万）なので、都市国家が総費用の大きい工場を着工すると経済ごと沈む。
                //   総費用が財政バッファ（月次義務のNか月分＝規模中立の物差し）を超える工場は建てない
                //   ＝大国は何でも建てられ、小国は軽工業から（史実＝造船所や製鉄所は大国の事業）。
                //   ⚠#4の連鎖の目は上（このゲートの前）で走らせ済み＝高価で建てられない下流でも、安価な上流の律速は明かせる。
                // ★軍需の身の丈ゲート倍率（2026-09-07・英の軍が立たない第2の根）：軍需（9万）は1783年の財政バッファ（月次義務×数か月）を超えて**半世紀建たない**
                //   （armsdebug 実測＝ロンドンの小銃工場は建設0のまま1837年に初めて建つ）。史実の国家は貧しくても兵器廠を持った。
                //   MilitaryBuildCostCapScale=1.0＝現行（厳密恒等）／>1 で軍需カテゴリの上限をその倍率で緩める（対象は AI の着工判断のみ＝プレイヤーは元から自由）。
                if (Catalog.FactorySpec(pr.GoodId).Cost > FacilityBuildCostCap * buffer * BuildCapMul(w, pr.GoodId)) continue;
                if (!(eff > FacilityBuildMinMargin)) continue;   // 否定形＝NaNは建てない
                // 採算>0 は残す（2026-08-04の設計）。★#2の例外＝世界チャネルが開いた候補は国内 margin で拒まない：輸入州の価格は
                //   自国の限界費用に錨（診断δ）＝margin≈0以下に潰されているのが「安い輸入に押されている」状態そのもので、
                //   ここで国内marginを課すと反転が原理的に起きない（世界チャネルの採算は worldRel>閾値 が既に判定している）。
                if (!worldOpened && !(pr.SmoothedMargin > 0)) continue;
                Consider(ri, pr.GoodId, val.Score);   // 不足の深さ×人が余っている
            }
        }
        // ★AIも**同じカウンター**を1つ上げる（作者裁定2026-08-10 E節＝動詞を1本に統合して非対称を作らない）。
        //   SetStateTarget はその場で1歩試し（＝旧 BuildFactory 直呼びと同じ着工）、拒まれた月は目標が残って
        //   月次の StepStateTargets が再挑戦する＝「意志は消えない」。溜まらないのは上の pendingWill ガードが受ける。
        if (!relax)
        {
            if (bestR >= 0)
            {
                var bp = World.FindFactory(w.Regions[bestR], bestG);   // SetStateTarget が掴むのと同じ1本（代替レシピの取り違えでカウンターが噛み合わない事故を作らない）
                if (bp is not null) w.SetStateTarget(bestR, bestG, World.StateUnits(bp, w.Coef) + 1);
            }
            return;
        }
        // ★鋼市場診断#3(a)（2026-08-16）：州ごとの並行着工＝州index順（決定論・同点規律は従来の「州順→財id順」を州内 best が引き継ぐ）。
        //   着工が実際に始まった分の月次出費をその場で加算し、outlayCap（本命の予算制約）を跨いだら残りの州は打ち切り
        //   ＝「全国同時1件」という人工的な直列化だけを外し、金の天井は従来の1本のまま。
        for (int ri = 0; ri < w.Regions.Count; ri++)
        {
            if (bestGr![ri] < 0) continue;
            if (monthlyBuild > outlayCap) break;
            var bp = World.FindFactory(w.Regions[ri], bestGr[ri]);
            if (bp is null) continue;
            w.SetStateTarget(ri, bestGr[ri], World.StateUnits(bp, w.Coef) + 1);
            if (bp.ConstructionLeft > 0)
            {
                monthlyBuild += bp.ConstructionPayPerDay * Calendar.DaysPerMonth;
                monthlyBuild += World.ConstructionMaterialPerMonth(w.Regions[ri], bp, w.Coef);
            }   // その月に始まった工事だけを律速へ（拒まれた月は目標が残り月次で再挑戦＝従来どおり）
        }
    }

    /// <summary>巨大建造物の着工（2026-08-02）。★作者裁定＝**着工はプレイヤーの判断**とし、AIの挙動は動詞の導入前と同じにする。
    /// したがってAIの規則は「着工できるものは即座に全部着工する」＝技術ゲートと国庫が許せば建て始める、という旧挙動の再現。
    /// 拠出の有界化は従来どおり MegaprojectStateCap（1国あたり総額）が効くので、同時着工しても国庫は一気に吸われない。
    /// 着工そのものは Megaprojects.TryStart＝プレイヤーのボタンと同一経路（機構は対称・判断だけが違う）。
    /// 単一国（Diplomacy/SelfPower=null）では何もしない＝較正に無影響。</summary>
    private static void StartMegaproject(World w)
    {
        if (w.Diplomacy is not { } dip || w.SelfPower is null) return;
        foreach (var mp in dip.Megaprojects) Megaprojects.TryStart(w, mp);
    }

    internal static void Apply(World w, Stance s)   // internal＝番人テストがスタンスを固定して呼ぶためだけ（回5-1 項目19・可視性のみ）
    {
        var p = w.Policy;
        p.CoercionTarget = 400;          // 既定＝低治安維持力（警察・内政の鎮圧）。軍備スタンス(Militarize)のみ4000へ
        // ★専売（作者裁定2026-09-20「専売の率を法律から完全に消す。材を選ぶのみにする」）＝教義が選ぶのは率でなく**対象の広さ**（World.StateMonopolyStepOf の段）。
        //   梯子の順＝史実の物品税の財（酒・煙草・茶・コーヒー・石鹸）→奢侈品→日用品→主食→役務（2026-09-30 (127)）。教義の段はこの広さの**床**
        //   ＝末尾の財政の規律が今の広さから広げ・保ち・狭める（新しい数も新しい状態変数も作らない＝今の広さは World.StateMonopolyTier の導出）。
        int stateMonopTier = 0;                // 既定＝非課税（旧 Policy.StateMonopolyTarget の開始0と同じ場所）
        p.MilitarySpendingTarget = SecurityStageTarget(w);   // ★(155) 軍事費の段＝安全保障の不安の唯一の入口（旧＝既定 0.25・軍備スタンスで 0.7＝脅威は緊張・植民地防衛・生存の効用を通ってスタンスの2値で入った）
        p.Censorship = CensorshipLaw.FreePress;    // 既定＝自由化を目指す（革新↑だが政治意識↑）。政治ゲートが可否を決める（旧支配層が抵抗）
        p.LaborUnion = LaborUnionLaw.Tolerated;    // 既定＝労働組合の容認を目指す（不満緩和だが出力↓・資本が抵抗）。軍備スタンスは統制へ戻す
        p.WorkHours = WorkHours.Twelve;            // 既定＝12時間（LaborFatiguePenalty で12hが産出最適＝無制限14hより産出多。福祉スタンスは Ten/Eight へ短縮）。無制限放置の愚行を解消
        switch (s)
        {
            case Stance.Modernize:
                p.EducationLaw = EducationLaw.Public;
                // 豊かな近代化国は義務教育へ＝popが買えない教育を国庫が買い全民就学（多大な財政コスト＝豊かさが前提）。
                if (w.EducationLaw >= EducationLaw.Public && w.TechLevel > CompulsoryEduTechThreshold && w.Treasury > CompulsoryEduBufferMult * FiscalBufferOf(w))   // 旧150,000固定＝旧閾値50,000の3倍→バッファの3倍へ規模中立化（監査2026-07-11 リ4：小型先進国が義務教育を目標に置けなかった）
                    p.EducationLaw = EducationLaw.Compulsory;
                p.EducationSpendingTarget = Government.EducationSpendingTop;   // ★錨2026-09-18＝この軸の教義最大＝段の最上段（値は Government へ移設・ここは参照）
                p.AdminSpendingTarget = 0.6;    // 近代国家＝行政を整備（汚職を抑える＝税の漏れが減る→検地台帳のアンロックにも繋がる）
                p.IncomeTaxTarget = 0.25;   // 治安維持力は上の教義既定に従う（軍国は大軍のまま近代化）
                p.LandTaxTarget = Government.LandTaxTop; p.ProgressiveTaxTarget = 0.3; p.PollTaxTarget = 0;   // 近代税制へ移行（人頭税は廃止）
                stateMonopTier = 1;   // 専売は史実の物品税の財どまり（酒・煙草・茶・コーヒー・石鹸＝旧の率0.2）
                p.SocialSecurityTarget = 0.3;   // 近代国家は公共医療を一定維持＝人口増（乳児死亡の低下）
                p.ChildLaborAllowed = false;    // 近代化＝児童労働を禁止（出力↓だが識字↑＝教育→医療→人口）
                if (w.TechLevel > WomenPropertyTechThreshold) RaiseWomenRights(p, WomenRightsLaw.Property);   // 成熟した近代国家＝女性財産権（労働供給+2割・普通選挙への布石）
                if (w.TechLevel > WomenSuffrageTechThreshold) RaiseWomenRights(p, WomenRightsLaw.Suffrage);   // ★統合法：選挙権の段は財産権より後（tech閾値↑）。教育権の段は梯子が通る
                if (w.TechLevel > CensusSuffrageTechThreshold && p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;
                break;
            case Stance.Industrialize:
                p.EducationLaw = EducationLaw.Public;
                p.EducationSpendingTarget = 0.6;
                p.AdminSpendingTarget = 0.5;
                p.IncomeTaxTarget = 0.15; p.TariffTarget = 0.2;
                p.LandTaxTarget = 0.3; p.PollTaxTarget = 0;   // 産業重視＝所得税を抑え土地・消費に課税
                stateMonopTier = 2;   // 専売を奢侈品まで広げる（旧の率0.3）
                // 金利は下の反景気循環ブロックが一元的に決める（旧・スタンス別の低金利0.1は直後に必ず上書きされる死書き込みだった＝監査2026-07-12 死10で削除。スタンス別金融政策を生かす改修は別件）
                break;
            case Stance.Welfare:
                p.SocialSecurityTarget = 0.6; p.EducationSpendingTarget = 0.5;
                p.AdminSpendingTarget = 0.5;
                RaiseWomenRights(p, WomenRightsLaw.Suffrage);   // 労働者福祉＝女性の権利も梯子の最上段まで推進（北欧型・自分の賃金を持ち、学び、投票する）
                p.IncomeTaxTarget = 0.2;
                p.ProgressiveTaxTarget = 0.6; p.LandTaxTarget = Government.LandTaxTop; p.PollTaxTarget = 0;   // 福祉の財源＝富裕層から（再分配）
                stateMonopTier = 1;   // 逆進税は最小限＝史実の物品税の財だけ（旧の率0.1）
                p.WorkHours = WorkHours.Ten;   // 労働者福祉＝労働時間短縮（産出↓だが死亡率↓・不満↓）
                p.ChildLaborAllowed = false;   // 労働者福祉＝児童労働の禁止
                break;
            case Stance.Fiscal:
                p.IncomeTaxTarget = Government.IncomeTaxTop; p.EducationSpendingTarget = 0.2;   // ★錨2026-09-18＝所得税の教義最大＝段の最上段（35%＝1918年の英標準税率30%超）
                p.AdminSpendingTarget = 0.3;    // 緊縮＝行政費も削る（汚職が増える代償）
                p.PollTaxTarget = 0.3; p.LandTaxTarget = Government.LandTaxTop; p.ProgressiveTaxTarget = 0.4;   // 取れる所から全部取る（緊縮財政）
                stateMonopTier = 3;   // 日用品まで広く課税（旧の率0.4）
                p.SocialSecurityTarget = 0.1;   // 金利は下の反景気循環ブロックが一元的に決める（旧・緊縮の高金利0.4は死書き込み＝監査2026-07-12 死10で削除）
                break;
            case Stance.Militarize:
                p.CoercionTarget = 4000;   // 治安維持力目標↑。※実ゲーム（多国）では CoercionFollowsForce=true が毎期 兵数×即応度 から再導出・上書きするため実効なし＝この書き込みが生きるのは単一Simulationのテストのみ（監査2026-07-12 死11の注記＝「革命抑止」はここでなく軍事費0.7→兵数増経由で効く〔⚠ (155) で軍備スタンスの軍事費 0.7 は削除＝軍事費の段を動かすのは安全保障の不安だけ（SecurityStageTarget）＝この経路はスタンスからは無い・(157) の注記〕）
                // ⚠墓標2026-08-08：ここには「脅威に応じて軍事費を連続化する」(MilitarizeSpendThreatSpan) があったが、
                //   A/B実測で棄却され**再点火禁止**（「形が先・金が後」が代替）。★実装は 2026-08-12 に撤去（11行）。
                //   ⚠2026-10-02 (155)：同じ系統（脅威の連続値で軍事費の段を決める＝上の SecurityStageTarget＝脅威の割合で段1〜3 を内分）を、作者の承認の形
                //   （脅威の入口を軍事費の段の1つにする案・作者 2026-10-01 に異議なし）で再導入した（(157) で注記）。前回の棄却（0.4 の A/B＝政体 exact −4.3・法 −3.8・列強 −3.8・経済 −2.5）と
                //   (155) の10種（r154 比＝exact −5.1・法一致 −2pt・列強の再現率 +0.1・経済規模 −2.6）は exact と経済で同じ向き＝作者への確認（実装ログ (157)）。代替に挙げた SpendFromNeedWeight は 2026-09-26 に削除済み。
                // 【削除2026-10-02 (155)】p.MilitarySpendingTarget = 0.7（軍備スタンスの軍事費）＝軍事費の段は安全保障の不安の唯一の入口（上の SecurityStageTarget・作者 2026-10-01 に異議なし）。
                p.AdminSpendingTarget = 0.5;
                p.IncomeTaxTarget = 0.25;                       // 大軍を養う財源（⚠ (155) から軍備スタンスは軍事費の段を書かない＝残るのは増税だけ・段は安全保障の不安＝(157) の注記）
                p.PollTaxTarget = 0.4;   // 戦費＝人頭税と専売（治安維持力で不満を抑え込む賭け）
                stateMonopTier = 2;   // 物品税の財と奢侈品（2026-09-30 (127)＝旧の段3＝日用品の全てを外した）。史実の軍国の戦費＝酒・煙草・塩の専売と人頭税（露の酒の請負と魂の税・普の煙草とコーヒーの専売・仏の塩税と aides と煙草）
                //   ＝日用品の広い売上税は緊縮（取れる所から全部）と下の財政の規律（払えない請求で広げる）の仕事。
                // 露普歳入是正（2026-08-08）：Militarize が土地税・関税・教育を一切書かない唯一の主要スタンスだった＝
                // 軍国こそ地租と関税で軍を養い（地租改正・ユンカーのGrundsteuer）、義務教育は兵営国家の発明（プロイセン1763・明治学制）。
                // 露＝militarism固定でこのスタンスに130年閉じ込められ、教育が一度も目標化されず識字逆行→技術凍結が第一原因。
                if (w.Coef.MilitarizeAgrarianTaxes > 0)
                {
                    p.LandTaxTarget = Math.Max(p.LandTaxTarget, Government.LandTaxTop);
                    p.TariffTarget = Math.Max(p.TariffTarget, 0.3);
                }
                if (w.Coef.MilitarizeEducationDrive > 0)
                {
                    p.EducationLaw = EducationLaw.Public;
                    p.EducationSpendingTarget = Math.Max(p.EducationSpendingTarget, 0.4);
                }
                p.Censorship = CensorshipLaw.StatePressOnly;    // 軍国＝報道統制（安定優先・革新は犠牲）
                p.LaborUnion = LaborUnionLaw.Illegal;           // 軍国＝労働組合を非合法（統制）
                // 民主的後退（ワイマール→ナチ型＝**合法的な選挙法の巻き戻し**）：軍国教義の議会制民主国（立憲/共和）は
                // 参政権の制限選挙（Census）への後退を**目標に置く**だけ＝実際に通るかは通常の政治ゲート（参政権は憲法級＝硬い純支持＞0）が決める。
                // ＝後退は支持が反対を上回るときだけ通る（参政権は 2026-08-04 から憲法級＝政治資本では押し切れない。旧注記「正統性の高い軍国民主国だけが
                //   大衆の反対を押し切って後退でき、その度に政治資本を燃やす」は余白 θ の世界の記述＝2026-09-21 ㉖で訂正）。
                // 高闘争時は下の譲歩（AdultMale目標）が上書き＝革命脅威下では後退できない。社会党与党は下の「議会の構成を読む」段落が引き上げる＝右派政権のみ実行可。
                if (w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && DoctrineFrom(w) == Doctrine.Militarist)
                    p.Suffrage = Suffrage.Census;
                break;
        }
        // （形が先・金が後 配線C＝軍事費目標を理想軍の請求書へ寄せる器 SpendFromNeedWeight は 2026-08-08 に棄却・2026-09-26 に削除＝棚卸し mil-21）
        // ソ連風税制（2026-08-08作者裁定）：プロ独裁＝取引税型の極端な専売＋大規模な社会保障＋累進最大＝
        // 計画経済の財政的な顔（価格配分は触らない）。法の可否は通常の政治ゲートが決める＝台本でなく目標。
        if (w.Coef.SovietTaxProfile > 0 && w.Polity == Polity.ProletarianDictatorship)
        {
            stateMonopTier = World.StateMonopolyMaxTier;   // ★取引税＝主食まで全ての消費財に課す（旧 Government.StateMonopolyTop=0.8 の翻訳・史実どおり全財）
            p.ProgressiveTaxTarget = Math.Max(p.ProgressiveTaxTarget, Government.ProgressiveTaxTop);   // ★錨2026-09-18＝累進の教義最大＝段の最上段
            p.SocialSecurityTarget = Math.Max(p.SocialSecurityTarget, 1.0);
            p.SocialSecurityLaw = SocialSecurityLaw.Pension;   // 4段の最上位を目標化（実際の到達は政治ゲート）
        }
        // ★戦時段の選択（作者裁定2026-09-19 M2）：総動員中かつ交戦中のAIは軍事費の最上段（戦時経済）を**選ぶ**＝プレイヤーの法律タブの段ボタン
        //   （GameViewModel.Law の `v / 段数`）と同じ書き込み口・同じ写像に v=最上段を入れる＝新しい数は置かない。総動員は段を解禁するだけで押し上げない
        //   （Government.MilitaryTotalTarget）＝AIが自分で選ばなければ戦時経済に届かない。1段ずつの登坂と「総動員が前提」の門は GateAllLawTargets が
        //   法律タブと同じ GateInfo で掛ける＝IsPlayer は読まない。講和の四半期は DecideQuarter の総動員分岐（Apply の前）が先に復員させる＝この行は不発で教義の値へ戻る。
        if (w.AtWarNow && w.Mobilized)
            p.MilitarySpendingTarget = Government.MilitaryLawStages / (double)Government.MilitaryLawStages;

        // ★不満の社会保障圧力（2026-08-04・作者指示）：どのスタンスが勝っていても、不満が高いほど社会保障の目標に
        //   床を敷く＝満足度の下落に福祉が**必ず**応答する（旧＝Welfareスタンスが選ばれない限り目標は0.1〜0.3で凍結）。
        //   床＝不満の深さに線形（満足0.7で0・満足0で1.0）。下げ圧にはしない（Max）＝緊縮スタンスの意図的な0.1は保つ。
        {
            double satNow = NationalScore.MeanSatisfaction(w);
            double unrestNow = Math.Max(0, UnrestSatisfactionThreshold - satNow);
            double ssFloor = Math.Min(1.0, UnrestSSPressure * unrestNow / UnrestSatisfactionThreshold);
            if (ssFloor > p.SocialSecurityTarget) p.SocialSecurityTarget = ssFloor;
        }

        // 革命を避ける参政権の譲歩（Reform Acts／ビスマルク型の上からの改革）：大衆の闘争度が高い＝革命脅威が迫ると、
        // AIは参政権を拡大して不満を**改革＝票**へ流す（転覆より分権が得）。空間モデルでは新規有権者の乖離が暴動でなく投票へ流れて沈静化。
        // ＝硬直して革命される（旧挙動）でなく、史実の英のように改革で生き延びる。実際に通るかは政治ロック（エリートの譲歩・PolicyPreferences）が決める。
        if (Politics.AvgMassMilitancy(w) > w.Coef.SuffrageReformThreshold && p.Suffrage < Suffrage.AdultMale)
            p.Suffrage = Suffrage.AdultMale;

        // ★議会の構成を読む（回5-1 項目17・2026-09-26）：旧 Parliament.ApplyPlatform の法軸の段を AI の意図へ移した＝値も適用範囲も同じ
        //   （議会あり・政治あり・内戦中でない・立憲か共和）。旧はプレイヤー国の目標まで毎四半期上書きし、門の畳み（GateAllLawTargets）の**後**に書いて幽霊戦線を作っていた。
        //   ここはスタンス既定（検閲・労組・労働時間の毎期の再代入）と軍国の民主的後退の後ろ＝旧と同じく与党の段がそれらを上書きし、
        //   危機の要求（TargetCrisisDemands）と門の畳みの前＝危機の要求が最後の言葉・通らない段は法律タブと同じ GateInfo で畳まれる。
        //   読むのは与党の札（第一党）＝プレイヤーも政治タブで同じ札を見る。⚠AI は Parliament.Step より前に判断する＝選挙の四半期だけ新しい与党の段が1期遅れる。
        if (w.ParliamentEnabled && w.PoliticsEnabled && !w.InCivilWar
            && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic)
        {
            if (w.GoverningParty == (int)Parliament.Party.Liberal)          // 近代エリート＝公教育・制限選挙・10時間法
            {
                if (p.EducationLaw < EducationLaw.Public) p.EducationLaw = EducationLaw.Public;
                if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;
                if (p.WorkHours > WorkHours.Ten) p.WorkHours = WorkHours.Ten;
            }
            else if (w.GoverningParty == (int)Parliament.Party.Socialist)   // 大衆＝公教育・男子普選・労組の容認・8時間労働
            {
                if (p.EducationLaw < EducationLaw.Public) p.EducationLaw = EducationLaw.Public;
                if (p.Suffrage < Suffrage.AdultMale) p.Suffrage = Suffrage.AdultMale;
                p.LaborUnion = LaborUnionLaw.Tolerated;
                p.WorkHours = WorkHours.Eight;
            }
        }

        DecideModernLaws(w, p, s);   // 02断線走査2026-07-28：E5/E6の9法にもAIの目標を置く（施行可否は政治ゲート）

        // 反景気循環的金融政策（教義の金利を上書き）：景気後退で利下げ＝信用を支え恐慌を緩衝、過熱で利上げ＝引締。
        // ＝賢い中央銀行ほど恐慌を浅く短くできる（恐慌回避の巧拙が国力差に出るか計測する）。
        double drawdown = w.GdpDrawdown;
        double heat = w.FinancialHeat;
        if (drawdown > RecessionDrawdownThreshold) p.InterestRateTarget = EasingInterestRate;        // 後退＝緩和（恐慌の緩衝）
        else if (heat > OverheatHeatThreshold) p.InterestRateTarget = TighteningInterestRate;        // 過熱＝引締
        else if (heat > OverheatHeatThreshold * w.Coef.PreTightenHeatFrac) p.InterestRateTarget = PreTighteningInterestRate;   // #124：過熱の兆候＝予防的引締め（バブルを小さく＝崩壊も浅く）。係数化2026-08-22＝裸腕の座標（既定0.75不変）
        else p.InterestRateTarget = NeutralInterestRate;                        // 平時＝中立

        // === 財政規律（死のスパイラルの防止）===
        // AIは放っておくと永久に工業化姿勢のまま国庫を0にし、行政費が払えず行政が崩壊→税収が汚職で漏れ続ける→破産が固定化する。
        // 国庫が薄い間は裁量支出（技術導入→福祉→過剰教育の順）を絞り、**行政費だけは維持**してスパイラルを断ち、
        // 広い税（人頭・消費）で黒字へ戻す＝行政が機能→漏れが減る→自立（所得税の解禁ラッチは2026-09-12に廃止）。
        double fiscalStress = FiscalStressOf(w);   // 0=十分, 1=破産（規模中立＝月次義務の何か月分か。監査2026-07-11 リ4）
        if (fiscalStress > 0)
        {
            // #124 恐慌AI対処（2026-07-25）：恐慌の谷での緊縮増税・福祉削減は需要をさらに殺し恐慌を深くする（プロサイクリカル財政の罠＝1930年代の緊縮の再演）。
            // 恐慌中は stress 応答を半減＝借入で凌ぎ、回復後に締める。行政費の維持（スパイラルの要）は不変。プレイヤーも同じ判断ができる＝対称。
            double stressEff = drawdown > RecessionDrawdownThreshold ? fiscalStress * w.Coef.CrisisAusterityRelief : fiscalStress;   // 係数化2026-08-22＝裸腕の座標（既定0.5不変）
            double ease = 1 - stressEff;
            p.SocialSecurityTarget *= ease;                                        // 福祉も絞る（裁量支出を国庫逼迫時に削る。恐慌中は半減幅＝失業の怒りを煽らない）
            p.EducationSpendingTarget = Math.Min(p.EducationSpendingTarget, EducationEaseFloor + EducationEaseSpan * ease);
            p.AdminSpendingTarget = Math.Max(p.AdminSpendingTarget, 0.5);          // 行政費は維持＝スパイラルの要（行政が死ぬと所得税が永久ロック）
            p.PollTaxTarget = Math.Max(p.PollTaxTarget, FiscalTaxStressMult * stressEff);       // 行政不要の確実な財源を確保（恐慌中は引上げ幅も半減）
        }
        // ★専売の広さの規律（2026-09-30 (127)）＝**今の広さから**動かす（教義の広さは床）：払えない請求（r＜1）＝1段広げる（上のコメントの「広い税で黒字へ戻す」）・
        //   国庫が安全水準（FiscalBufferOf＝月次義務の FiscalStressBufferMonths か月分＝この AI の逼迫の物差し）を超えた＝1段狭める・その間＝保つ。Clamp は SetStateMonopolyTier が持つ。
        //   ⚠旧＝「教義の段＋逼迫なら1」を毎四半期に教義の段から計算し直した＝広げた四半期に黒字→次の四半期に教義の段へ戻る→また払えない、の
        //     四半期ごとの往復（seed 1 の英＝段2↔3 を毎四半期・歳入÷年GDP 6%↔17%・日用品の買い手価格が四半期ごとに ±21%）。
        //   理由で動く（逼迫と余裕の2本の線＝既存の r と安全水準）＝タイマーでも新しい状態でもない（今の広さは StateMonopolyMargins からの導出）。
        //   ⚠潤沢の減税（RelieveOpulence・8四半期分の線）は使わない＝あちらは率への連続倍率・こちらは広さの1段＝同じ線に乗せると国庫が数年分積もるまで狭まらない
        //     （probe seed 1＝英が段3→4・蘭が段3 に張り付き 1784 の歳入÷年GDP 18・24%）。
        //   ⚠狭める側は教義の広さより下へ行かない＝旧 RelieveOpulence の注記が退けた「段を落とす翻訳」（床が無い・毎期 教義−1段）にならない。
        //   ⚠開始前の助走（World.WarmupFrozen）は今の広さを積まない＝教義の広さのまま（助走は借りない＝r＜1 は「借りられない」助走の作りもの・
        //     広げた記憶は年月が経って積み上がる量＝国庫・国債・法と同じ門）。
        if (!w.WarmupFrozen)
        {
            int curTier = w.StateMonopolyTier;
            if (fiscalStress > 0) stateMonopTier = Math.Max(stateMonopTier, curTier + 1);
            else if (w.Treasury > FiscalBufferOf(w)) stateMonopTier = Math.Max(stateMonopTier, curTier - 1);
            else stateMonopTier = Math.Max(stateMonopTier, curTier);
        }
        // ★専売の書き込み口（作者裁定2026-09-20）＝**プレイヤーの生産タブのチェックボックスと同じ1本**（World.SetStateMonopolyOn 経由）。
        //   ⚠旧はここが w.StateMonopolyMargins[] へ直書きしていた（天井の自動導出を避けるための意図的な迂回）＝P5の破れ。天井が消えたので迂回の理由も消えた。
        w.SetStateMonopolyTier(stateMonopTier);

        // ★撤去2026-08-16（C2-C5）：汚職根絶のAI目標（AntiCorruptionTarget）を物理削除＝汚職V2恒久ONで根絶費の動詞ごと死骸
        //   （V2点火2026-08-15で既に0固定＝挙動恒等・Fiscal側の請求遮断と対＝プレイヤー/AI対称のまま消滅。監視は法＝報道/資格任用/議会予算権で買う）。

        // P9改（2026-07-19ユーザー裁定）：国民形成費のレバーは廃止＝AI目標も不要。国民運動は「選挙を望む層」が自費で賄う（Fiscal の需要駆動ブロック・プレイヤー/AI対称）。

        // ★★3法のAI（設計＝docs/202608/20260802_3法の開放_部分完了.md【完了】.md・段2・2026-08-02）：穀物法・土地改革・農奴解放。
        // ⚠これまでAIは3軸に触れず「議会が自動で動かすから」とされていたが、**1783年の54カ国中46カ国は非選挙政体で
        //   ApplyPlatform（2026-09-26 に撤去＝回5-1）が丸ごとスキップされる**＝実際には誰も動かしていなかった。開放の主効果はここに出る（露・清・オスマン）。
        // ⚠⚠**PolicyPreferences を「AIの賛否」として読んではならない**＝あれはゲートの入力（物質的立場）で、
        //   それを意図として読むと「通る法しか望まない」トートロジーになり、自党を割って押し切る型が表現できなくなる。
        //   だからAIは**既存の物質量**だけを読む。
        // ⚠段は1つずつ動かす（プレイヤーの法律タブも1段ずつ置く＝手数を対称に保つ。施行は StepEnum が1期1段に律速）。
        {
            // 穀物法＝食べられているかと、都市の重み。飢えているか都市化した国は撤廃へ、農業国で足りていれば維持へ。
            // ★飢饉の閾値は**コードベース既存の較正済み基準**を使う（2026-08-02修正）。初版は 1−FoodFill > 0.05 で、
            //   これは Coefficients.DynMandateFoodAnchor=0.8 のコメントが「1.0基準は慢性輸入国＝英を常時罰する。
            //   罰すべきは飢饉であって輸入依存ではない」と既に否定していた水準＝常態を飢饉と読んで第1分岐が常に真になり、
            //   都市化の2分岐が一度も効かず全54カ国が None へ落ちた（実測 穀物法 None 100%）。
            bool famine = !(w.FoodFill >= w.Coef.DynMandateFoodAnchor);   // 否定形＝NaN は飢饉側（fail-closed）
            int cornWant = famine || w.UrbanShare > CornUrbanRepeal ? 0   // なし＝安いパン
                         : w.UrbanShare < CornAgrarianKeep ? 2 : (int)p.CornLaws;                // 全面＝地主保護
            StepLawToward(w, p, PolicyAxis.CornLaws, (int)p.CornLaws, cornWant, 2, v => p.CornLaws = (ReformLevel)v);

            // 農奴解放・土地改革＝**地代の取り分が上限を超えている余地**が解放の効き。超えていなければ動かす意味がない。
            // ★走査は RGO に限る（2026-08-02修正）。FarmRentCap が実際に律速するのは RGO だけ（EconomyStep.cs の
            //   `s.IsRgo ? Min(...) : s.RentShare`）なのに初版は全生産者を見ていた。全州に必ず居る RentShare=1.0 の
            //   非RGO工場（坩堝鋼・錬鉄レール）が常に上限超過として数えられ、法が1段でも動くと room が恒久的に立つ
            //   自己触媒ラチェットになっていた（実測 土地改革/農奴解放 Full 100%）。
            // ★分母は**全RGO**（超過した数ではない）＝「どれだけ広く封建地代が残っているか」を表す量にする。
            //   超過件数で割ると、200件中1件でも200件全部でも同じ値になり問題の広がりが消える。
            double rentRoom = 0; int rentN = 0;
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                foreach (var pr in rg.Producers)
                {
                    if (!pr.IsRgo) continue;
                    rentN++;
                    if (pr.RentShare > rg.FarmRentCap) rentRoom += pr.RentShare - rg.FarmRentCap;
                }
            }
            double room = rentN > 0 ? rentRoom / rentN : 0;
            if (room > EmancipationRentRoom || famine)
            {
                StepLawToward(w, p, PolicyAxis.SerfEmancipation, (int)p.SerfEmancipation, (int)p.SerfEmancipation + 1, 2, v => p.SerfEmancipation = (ReformLevel)v);
                StepLawToward(w, p, PolicyAxis.LandReform, (int)p.LandReform, (int)p.LandReform + 1, 2, v => p.LandReform = (ReformLevel)v);
            }
        }

        // 阿片の法のAI（禁制品doc C3・2026-07-30。旧＝bool の行政禁令を即時に立てるだけだった）：
        // 中毒度が閾値を超えたら輸入禁止を、その2倍を超えたら全面禁止を**目標に置く**（施行は議会と利益集団の政治ゲート越し＝プレイヤーと同じ経路）。
        // 十分下がれば1段戻す（ヒステリシス・RNGなし決定論）。★輸出は「自国で全面禁止した国は他国へも売らない」＝
        // 保有法と輸出法が別の決定であることは保ちつつ、AIの立場が保有法から導かれる（英が1807→1833と60年かけた非対称もこの形で出る）。
        if (w.Coef.OpiumAddictionGrowth > 0)
        {
            double obAdd = 0, obSz = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { double rp = r.SumPopulation(); obAdd += r.OpiumAddiction * rp; obSz += rp; }
            double obAvg = obSz > 1e-9 ? obAdd / obSz : 0;
            double thr = w.Coef.OpiumBanThreshold;
            p.OpiumPossession = obAvg > thr * OpiumFullBanRatio ? ReformLevel.Full
                              : obAvg > thr ? ReformLevel.Limited
                              : obAvg < thr * OpiumBanHysteresis ? ReformLevel.None
                              : p.OpiumPossession;   // 閾値とヒステリシスの間＝現状維持
            // ③ 阿片の輸出法のAI規則（2026-07-31 裁定）：旧「自国が全面禁止に至った国だけ輸出も禁じる」は**英が出ない**
            // （史実の英は自国で禁じないまま輸出を続け、外交的代価が耐えられなくなって止めた＝1907英清阿片協定・1913インド阿片輸出終了）。
            // 新＝外圧 Σ(阿片を禁じていて、かつ緊張が高い相手の TradeWeight) が閾値を超えたら禁止。**新しいスカラーはゼロ**
            // （緊張・TradeWeight・相手の保有法はすべて既存）。この形は奴隷貿易（英海軍の圧力）・児童労働（国際労働規制）へ再利用できる。
            {
                double opiumPressure = 0;
                if (w.Diplomacy is { } odip && w.SelfPower is { } oself)
                    foreach (var other in odip.Powers)
                    {
                        if (ReferenceEquals(other, oself) || other.Nation is not { } ow2) continue;
                        if (ow2.OpiumPossession == ReformLevel.None) continue;                 // 相手が阿片を禁じていない＝圧力にならない
                        if (odip.Tension(oself, other) < w.Coef.OpiumExportPressureTension) continue;   // 緊張が高い相手だけ
                        opiumPressure += other.TradeWeight;
                    }
                p.OpiumExportBan = opiumPressure > w.Coef.OpiumExportPressureThreshold;
            }
        }

        // === Phase2（AI動詞拡張 2026-07-08＝プレイヤー専用だった動詞の対等化）。多国世界（SelfPower あり＝実ゲーム/selfplay）限定＝単一国テスト・較正は不変。===
        // 軍需補助金（戦時）：交戦中は軍需カテゴリの工場へ補助金＝装備の増産（財政が保つ間）。平時は外す。
        // 売却（資産の現金化）：破産局面では国営工場を四半期1件ずつ売って現金化。
        if (w.SelfPower is { } spv && w.Diplomacy is { } dipv)
        {
            // 近代化投資（国庫→全資本区画の建設予約＝マクロレバー）：産業/近代化スタンスのみ・財政余力でスケール。
            // 暴走防止＝工業化度ギャップで絞る（後発の国家主導工業化＝露/日型。設計包絡~2.5へ近づくほど投資を止める。
            // 無制限だと小国が国庫を注ぎ込み工業化度20超＝旧実験「露が規模常勝」の再来になる＝batch30実測で確認済み）。
            double easeInd = 1 - fiscalStress;
            // ★鋼市場診断#1（2026-08-16）：分母＝EffectiveIndCeiling＝α=0で定数2.5＝ビット恒等／AiIndustrialCeilingFrontier=α>0 で
            //   max(2.5, α×World.IndustrialFrontier)＝世界前線連動。★段階掃引0.6/0.8/1.0の結論＝**α=0.6で点火済み（live）**（露規模常勝の前科）。
            double indCeil = EffectiveIndCeiling(w);
            double indGap = PoliticalQuantities.Clamp01((indCeil - w.MeanIndustrialization) / indCeil);
            // ★採算で絞る（2026-08-03・作者指示「AIがより効果的に近代化投資を行うように」）：
            //   建設は鋼/機械/セメント/製材の**束**を買う。束が参照価格より高騰していると、同じ国庫で買える単位が減り、
            //   さらに需要を足すぶん価格を押し上げる＝**産出は増えず物価だけ上がる**。実測（indprobe）で英の鋼は
            //   在庫0・価格8.6（参照2.18の約4倍）が常態＝注いだ国庫の大半が値を吊り上げるだけに消えていた。
            //   判断は**私的な工場建設が既に使っている同じ関数**（FactoryBuildAfford＝束の現在費用/参照費用の連続減衰）＝
            //   新しい定数も新しい判断も持ち込まない。国家だけが相場を無視して買い漁る非対称を消す。
            w.ModernizationFrac = indGap * easeInd * s switch
            {
                Stance.Industrialize => IndustrializeInvestFrac,
                Stance.Modernize => ModernizeInvestFrac,
                // ★富国強兵（2026-08-02・作者報告「イギリスの開発度が異常に低い」の根）：旧 `_ => 0` は「軍か工場か」の
                //   偽二分法だった。植民地防衛効用（colonyDefense=0.3×植民地数）は植民地11の英で恒久+3.3＝英は**一度も**
                //   工業化系スタンスを選べず、このレバーが毎四半期0に潰され、開幕バーストすら踏めないまま
                //   Ind 0.33→1.22(Q3)→0.67 と減価で沈んだ（実測 indprobe 120Q。仏は3.67まで積む）。
                //   史実は逆＝軍拡国家こそ製鉄と鉄道を建てた（明治の富国強兵・ヴィルヘルム期ドイツ）。
                //   犠牲はレバーの排他でなく**財政**で払わせる＝easeInd(=1−財政逼迫) が既に律速
                //   （軍事費0.7の国は国庫が細り自然に絞られる）。定数は Modernize と共有＝新しいスカラーを置かない。
                //   ★富国強兵（2026-08-03・作者指示「近代化投資を復活」）＝軍拡国家こそ製鉄と鉄道を建てた
                //     （明治の富国強兵・ヴィルヘルム期ドイツ）。旧 `_ => 0` は「軍か工場か」の偽二分法だった。
                //     ⚠R10（無条件・colonyDefense 無限比例のまま）は列強再現率−10＝清などアジア軍拡国まで一斉工業化。
                //       いまは colonyDefense が飽和済み（R12）で帝国が Militarize に固着しない＝前提が変わっている。
                //     犠牲はレバーの排他でなく**財政**が払う（easeInd＝1−財政逼迫が既に律速＝軍事費0.7の国は国庫が細る）。
                //     定数は Modernize と共有＝新しいスカラーを置かない。
                Stance.Militarize => ModernizeInvestFrac,
                _ => 0,   // Welfare/Fiscal＝緊縮と再分配は本当に投資を止める（こちらは真の二分法）
            };
            // ダンピング（負関税＝輸出補助）：産業スタンスで輸出超過かつ財政健全なら世界市場へ安売り（後発の輸出攻勢）。
            // 後発限定（工業化度<2.5）＝成熟した輸出大国が国庫で安売りを続けると輸出ブーム→私的建設の暴走（実測＝ロンドン工業化度35）になるため。
            if (s == Stance.Industrialize && fiscalStress <= 1e-9 && w.LastExportValue > w.LastImportValue * DumpingExportRatio && w.MeanIndustrialization < IndustrializationCeiling)
                p.TariffTarget = DumpingTariff;
            else if (p.TariffTarget < 0)
                p.TariffTarget = 0;   // ダンピング解除（監査2026-07-11 他6）：旧＝リセットなしでスタンス離脱後も輸出額×10%の国庫→商人補助金が恒久残留（緊縮中も垂れ流し＝財政規律と逆行）
            bool atWar = dipv.Wars.Exists(war => war.A == spv || war.B == spv);
            for (int ri = 0; ri < w.Regions.Count; ri++)
            {
                var rg = w.Regions[ri];
                if (rg.State != RegionState.Active) continue;
                foreach (var pr in rg.Producers)
                {
                    // RGO（投入ゼロ）の除外を撤去（プランテーションdoc D1・2026-07-30）＝補助金の動詞をプレイヤーとAIで揃える
                    // （プレイヤーは World.SetSubsidyRate にRGO除外が無く生産タブから既にRGOへ補助できた＝思想⑤の片側欠落）。
                    // ⚠ 実挙動は現時点で厳密恒等：RGO財は FactorySpec が既定("軽工業")へ落ちて「軍需」枝に入らず、
                    //   もう一方の枝は AiDevelopmentalSubsidy=0（休眠）で不発。点火時に初めてRGOへ補助が届く。
                    // ★国営持分の除外（作者裁定2026-08-10）はAI専用の判断を足さずに済む＝AIも同じ World.SetSubsidy を通り、
                    //   国営100%なら動詞が0に落とし、部分国営なら EconomyStep が民営持分ぶんだけ効かせる（機構が対称・判断は不変）。
                    if (Catalog.Goods[pr.GoodId].Tier == GoodTier.Service) continue;
                    if (Catalog.FactorySpec(pr.GoodId).Category == "軍需")
                    {
                        bool want = atWar && fiscalStress < 0.5;
                        if (pr.Subsidized != want) w.SetSubsidy(ri, pr.GoodId, want);
                    }
                    // 産業政策 レバー3（2026-07-24・AI対称化）：産業スタンスの開発国家＝平時に民生上流（Intermediate/Producer）の
                    // 赤字工場（数期EMA採算）へ時限補助・黒字化 or 戦時/財政悪化で解除（明治の官営→払い下げの型）。プレイヤーの補助金動詞と完全対称。
                    // ⚠点火順序＝レバー1（軟予算）・2（財政規律）の後（コスト側なしで44カ国へ補助金ファームを配らない）。AiDevelopmentalSubsidy=0（既定）で不発＝厳密恒等。
                    else if (w.Coef.AiDevelopmentalSubsidy > 0 && s == Stance.Industrialize
                             && Catalog.Goods[pr.GoodId].Tier is GoodTier.Intermediate or GoodTier.Producer)
                    {
                        bool want = !atWar && fiscalStress < 0.5 && pr.SmoothedMargin < 0 && !pr.Bankrupt;
                        if (pr.Subsidized != want) w.SetSubsidy(ri, pr.GoodId, want);
                    }
                }
            }
            if (fiscalStress > FireSaleFiscalStress)
                for (int ri = 0; ri < w.Regions.Count; ri++)
                {
                    var rg = w.Regions[ri];
                    if (rg.State != RegionState.Active) continue;
                    bool sold = false;
                    // ★投げ売りも**同じカウンター**を1つ下げる（作者裁定2026-08-10 E節）。可否は CanSellFactory が握る＝
                    //   「不満のある資本家は買わない」「国有化法の対象は売れない」がAIにもそのまま効く（対称）。
                    //   売れる時だけ下げる＝売れないのに目標だけ下がって毎月再挑戦する空回りを作らない。
                    foreach (var pr in rg.Producers)
                        if (pr.BuiltScale > 1e-9 && w.CanSellFactory(ri, pr.GoodId).Ok)
                        { w.SetStateTarget(ri, pr.GoodId, Math.Max(0, World.StateUnits(pr, w.Coef) - 1)); sold = true; break; }
                    if (sold) break;
                }

            // 〔POP動詞（2026-07-19）は全廃：①階級優遇/弾圧②職業誘導③選別的徴兵ともユーザー指示で撤去（2026-07-20/21）。武力鎮圧は国内危機イベント化（Politics.CheckFactionRevolt でAI即決）。〕
        }

        // 〔②検討メモ〕債務感応の緊縮（債務が高い時に先回りで増税）を試したが、増税が pop の貯蓄/満足を削り急進化（立憲71→65%）＋技術テストに波及したため撤回。
        // 慢性赤字の根（特にイギリスの植民地防衛由来の軍事費）は per-nation の財政再均衡が要る別作業＝後回し。

        // ★AI第1波①（2026-08-16）：ここまでに計算した全法目標を「現行法から・いま通る1段」に統一（幽霊戦線の根絶）。
        // 必ず Apply の**最後**（全ての法書き込みの後）＝どの経路（スタンス既定・教義・譲歩・阿片・国有化）で置いた目標も一律に整形する。
        // ⚠0＝この if 以外は1命令も変わらない（従来の無ゲート代入がそのまま目標）＝ビット恒等。
        // ★要求は法の経路（作者裁定2026-09-11）：危機が立った運動（PendingFactionCrisis＝通知＝プレイヤーと同じ札）の要求と、
        //   F1 州一揆に譲歩した段（World.PendingRevoltDemand＝Politics.ConcedeToRevolt が記録）を法の目標に置く
        //   ＝下からの改革圧力（上の参政権の譲歩 SuffrageReformThreshold）と同じ型＝**要求への応答も法目標の1つ**。置く/置かないは法律タブと同じ GateInfo
        //   （Parliament.TargetCrisisDemands＝gated）＝通らない段は置かない＝拒否。プレイヤー国は Apply を通らず法律タブで同じ p.X を書く＝IsPlayer で分岐しない。
        //   ⚠必ず全法書き込みの後・GateAllLawTargets の前＝スタンス既定（Censorship/LaborUnion/WorkHours の毎期再代入）や DecideModernLaws・財政逼迫の ×ease に
        //     上書きされない位置（危機の要求が最後の言葉・整形は GateAllLawTargets が旧どおり）。
        // ★潤沢の減税（2026-08-04）も**門の前**（2026-09-28 (85)）＝反射が縮めた税の目標も同じ GateTax で「いま通る1段」へ整形される
        //   ＝AI とプレイヤーが同じ門を通る（旧は DecideQuarter で Apply の後＝門の後に倍率を掛け、門が拒む段を置けた＝幽霊戦線）。
        //   危機の要求より前＝要求が最後の言葉のまま。Apply の後に走っていた処理（借入の予約・通貨改革の予約）は国庫も潤沢線も動かさない＝判定は同じ。
        RelieveOpulence(w);
        Parliament.TargetCrisisDemands(w);
        if (w.Coef.AiLawGateUnified > 0) GateAllLawTargets(w, p);
    }
}

/// <summary>1国の最終順位（国力＝列強ランキング）。Laws＝最終的な法体系（目標5：法の多様性の計測用）。
/// Nation＝国名（Power.Name＝world.json）。Name＝首都州名（Regions[0]・割譲遷都で変わりうる）＝史実忠実度の照合キーは Nation。</summary>
public sealed record NationStanding(
    string Name, double Score, double Tech, double Industrialization, double Satisfaction, int Colonies, double Gdp,
    double Population, double Military, Polity Polity, double WarDead, double Tariff,
    Suffrage Suffrage = Suffrage.None, EducationLaw Education = EducationLaw.None, CensorshipLaw Censorship = CensorshipLaw.StatePressOnly,
    LaborUnionLaw Union = LaborUnionLaw.Illegal, WorkHours WorkHours = WorkHours.Unlimited, ReformLevel CornLaws = ReformLevel.None,
    ReformLevel LandReform = ReformLevel.None, ReformLevel Serf = ReformLevel.None, ReformLevel Pension = ReformLevel.None,
    // ★2026-08-18（親軍法の撤去）：旧 bool Militarism を2本へ分ける。
    //   Militarism＝**軍の政治的優位**∈[0,1]（導出量＝軍の圧力×(1−文民制度の弁)）。忠実度は閾値 HistoricalReference.MilitarismDegreeOk で照合する。
    //   Conscription＝徴兵法（**法**なので法多様性の軸に載る＝旧「親軍」枠の後継）。
    double Militarism = 0, ConscriptionLaw Conscription = ConscriptionLaw.Selective,
    string Nation = "", string Suzerain = "", string DisplayName = "",   // Nation/Suzerain＝BaseName（不変鍵＝忠実度照合・集計）。DisplayName＝終局時の表示名（変名後の「ドイツ」等・表示専用）
    double PopGrowth = 0,   // B-中7（史実検証2026-07-22）：1783年比の人口成長倍率。**領土不変な定義**＝本ゲームは国を消さないので絶対人口の±%は噛み合わない。0=初期人口不明（測定対象外）
                            // 案Y（2026-08-21）：宣言と実装の乖離を解消＝分子から併合の持ち込み人口（Region.AnnexBaggage）を除く。旧定義は PopGrowthGross（末尾）に併走
    int HomeProvinces = 0,  // 08の70%計器（2026-07-31）：本国州の保有数（Active な Region＝割譲/分離で減り征服で増える）。領土次元の分子
    double NavalPower = 0, double LandPower = 0, double FleetStock = 0,   // ★2026-08-01（作者裁定「海軍の強さは測定するべき」）：陸海の内訳。現行の忠実度6次元は兵力も海軍も装備も測らない＝改修の効果が検証できない
    double TradeGdp = 0,   // ★貿易開放度次元（2026-08-15・作者承認「テスト速度に影響しないなら追加」）：(輸出+輸入)×年換算/GDP（国際のみ＝TariffBorderOnly点火後の定義）。照合＝HistoricalReference.TradeOpenness1913
    // ★計器のみ（2026-08-18）：Militarism＝軍の政治的優位の**内訳**。合成値だけを見て写像を2度誤った（旧弁＝国家の近代性／新弁＝露が沈む）ので、
    //   milgrade が分子と分母、さらに弁の2項を別々に印字できるようにする。⚠効果には一切接続しない＝挙動非接触。
    double MilPush = 0, double MilValve = 0, double MilCourt = 0, double MilLegitimacy = 0,
    // ★★導出政体（2026-08-21・作者裁定「切り替えでよい」）：権力シェアから読み出した政体（PowerAffinity.PolityFromShares）。
    //   忠実度の政体次元は**これ**で照合する（政体は表示・照合のラベルであって挙動の分岐ではない＝2026-08-19裁定の帰結）。
    //   ⚠Polity（旧ラベル）は**比較可能性のために併走で残す**＝ハーネスが旧基準の exact も同走で数える（LabelExact）。
    Polity DerivedPolity = Polity.Absolutist,
    // 案Y（人口物差しの脱汚染・2026-08-21）：旧定義の人口成長倍率＝総人口比（征服インフレ込み）。PopGrowth が新定義
    //（併合の持ち込み人口 AnnexBaggage を分子から除く＝宣言どおりの領土不変）へ替わったため、比較可能性の橋として併走。
    double PopGrowthGross = 0,
    double ColonyMass = 0);  // ★2026-08-28（作者裁定③）：植民地の質量（Colony.Population単位＝細分化で不変）。忠実度の植民地次元がこちらを読む（0=旧経路＝個数へ退避）

/// <summary>1試合の構造化結果。</summary>
public sealed record MatchResult(
    ulong Seed, int Quarters, string Winner, double WinnerScore, int DecisionQuarter,
    IReadOnlyList<NationStanding> Standings, IReadOnlyList<string> Events,
    IReadOnlyList<string> Anomalies, bool Completed,
    int HarnessPass, int HarnessWarn, int HarnessFail, double MaxLossFinanced,
    IReadOnlyList<string> HarnessIssues,    // 合格しなかった（△/×）検査の内訳（回帰の所在を可視化）
    double MaxDrawdown, int DepressionEpisodes, int OverheatDepressions, int HarvestDepressions,   // GDP最大ドローダウン・恐慌数・うち信用過熱先行数・不作先行数（恐慌の原因分解・要望3）
    int LeadChanges = 0,   // 首位交代の総回数（目標3：独走できず首位が入れ替わり続けるか）
    int GoldSuspensions = 0, int GoldCoverSuspensions = 0,   // GoldCoverMin有効化2026-07-10：金本位の強制離脱（兌換停止）総数・うちカバー率割れ起因
    int SpawnedNations = 0, int Formations = 0,   // 国家アイデンティティ2026-07-12：分離独立で建国した国数・変名（統一/formable）した国数＝状態由来（leanのログ抑制に非依存）
    int CivilWarSplits = 0,   // ★2026-08-13：内戦分裂（CivilWarSplitEnabled）の成立回数。旧は World.CivilWarSplitCount に**読み手がゼロ**で、
                              //   機構が働いたかどうかを**観測すらできなかった**（2026-08-01に「帝国を丸ごと解体する」として差し戻したまま）。
                              //   ★2026-09-28（処理の一元化 段2 束3）：旧経路の国家分裂を削除＝政体をめぐる内戦が分断（白紙講和）で終わった回数（Diplomacy.ResolvePeace）。
    IReadOnlyList<NationStanding>? StandingsAt1914 = null,   // 忠実度の照合断面（Q524＝1914年12月・1934延長2026-07-19）。null＝524以下の走行＝最終Standingsで照合（従来互換）
    bool Deadlocked = false,    // ★ゲート3是正2（2026-08-01）：デッドロックの構造フィールド。⚠旧は Anomalies の文字列一致（"デッドロック" を含むか）で判定しており、
                                //   メッセージ文言を変えた瞬間に**硬性ゲートが静かに恒真になる**（最終調整ハーネス設計 §1-3）。
    double WorstLossRate = 0,   // 慢性赤字の判定量（2026-07-21）：負債化フロー÷名目産出（四半期・EMA）のピーク。MaxLossFinanced は記録用の累積で判定には使わない
    string WorstLossNation = "", double WorstLossGdpQ = 0, int WorstLossQuarter = -1,   // ★診断2026-08-11：ピークを出した国・その時の四半期名目産出・四半期。
                                //   率は比なので**分母が痩せただけでも跳ねる**（実測：産出が消えた残余国で13,537%）。誰の何が起きたかを常に一緒に出す。
    int WarMaxRepeat = 0, int WarPairsGe3 = 0, int WarPairs = 0, string WarWorstPair = "",
    int InterveneChances = 0, int PeaceOffers = 0, int PeaceBlank = 0, int PeaceRefused = 0,   // ★診断2026-08-01：列強の介入（Probeでも埋まる）   // ★診断2026-08-01：同一ペアの再戦（最多回数・3回以上のペア数・交戦ペア総数・最多ペア名）
    int ElectionsHeld = 0, int ElectionQuarters = 0,   // ★E0計器（選挙doc §8-a・2026-08-15）：全国の総選挙回数と選挙政体在籍四半期（平均間隔＝Quarters/Held）
    double TradeGdpMajors = 0,   // ★貿易復活計器（2026-08-15）：6大国の終局時 貿易(輸出+輸入・年換算)/GDP の平均。史実1913英≈0.4（輸出0.25+輸入）＝復活判定の物差し
    // ★史実基準の恐慌計器（2026-08-22・作者指摘「恐慌回数・最大DDは史実基準ですか？」）：大国8（GdpRank1913参照国）の−20%級エピソード数と最大DD（産出床つき）。
    int MajorDepressionEpisodes = 0, double MajorMaxDrawdown = 0, string MajorDdNote = "",
    // ★2026-09-03（キューA-05／出典 トリアージ 2-A 10-2）：**跳躍ダイジェスト**＝7割帯は1914断面の一致率なので
    //   「逸脱3割の空間で起きた跳躍」を原理的に見ない。その盲点だけを埋める報告専用の窓（力学はこの列を読まない）。
    //   拾う3種＝①1年で順位10以上の跳躍 ②10年内に3回以上の政体フリップ ③1年で人口が物理上限を超えて増える。
    IReadOnlyList<string>? Jumps = null,
    // ★E-08 多断面（2026-09-05）：1820/1870年12月の断面（延長走行のみ非null）。主指標は StandingsAt1914 のまま・これらは併記の物差し
    IReadOnlyList<NationStanding>? StandingsAt1820 = null, IReadOnlyList<NationStanding>? StandingsAt1870 = null,
    IReadOnlyList<NationStanding>? StandingsAt1920 = null,    // ★E-09 WW1の帰結（1920年12月・政体のみ採点）
    IReadOnlyList<NationStanding>? StandingsAt1950 = null,   // ★1950断面（2026-09-21・Q668＝1950年1月1日・政体と従属を採点）。全長がQ668未満の走行はnull
    long[]? Cost = null);   // ★計算量の台帳（2026-09-30・CostLedger の添字＝この試合の見た件数・助走を含む）。報告専用＝帯の報告には書かない（Runner が別のファイルへ）

/// <summary>バッチ（複数seed）の集計。</summary>
public sealed record BatchSummary(
    int N, int Breakdowns, double AvgDecisionQuarter, IReadOnlyList<(string Nation, int Wins)> WinnerCounts,
    int Deadlocks, int LateDecisions, int TotalAnomalies, int HarnessFailMatches, IReadOnlyList<string> Flags,
    IReadOnlyList<(Polity Polity, int Count)> PolityDistribution,   // 最終政体の分布（全国×全seed）
    IReadOnlyList<(Polity Polity, int Plays)> PolityPlayCount,      // 各政体が「1国以上で出現したプレイ数」（要望4：希少政体も1/10プレイで出現するか）
    double AvgMaxDrawdown, int GreatDepressionMatches, double AvgDepressionEpisodes,   // 恐慌統計（要望3）：平均最大ドローダウン・大恐慌(25%超収縮)を含む試合数・平均恐慌回数
    double OverheatShare, double HarvestShare,   // 恐慌の原因分解：信用過熱先行（金融政策で回避）／不作先行（食糧安保・福祉で緩衝）の割合
    double AvgLeadChanges = 0,   // 平均首位交代回数（目標3：独走せず首位が入れ替わり続けるか）
    IReadOnlyList<(string Axis, double MaxShare, string Dist)>? LawDiversity = null,   // 法の多様性（目標5）：法軸ごとの最頻値シェア＋分布。MaxShare低いほど多様
    IReadOnlyList<NationFidelity>? Fidelity = null,   // 史実忠実度（新較正基準2026-07-09）：国別の1914参照表一致率（exact/near/法）。◎7割帯=near55-85%
    int GoldSuspensions = 0, int GoldCoverSuspensions = 0, int GoldSuspensionMatches = 0,
    int Independences = 0, int Formations = 0,   // 国家アイデンティティ2026-07-12：分離独立の建国総数・変名（統一/formable）総数
    int CivilWarSplits = 0,   // ★2026-08-13：内戦分裂の成立総数（再点火の判断に要る観測点）
    IReadOnlyList<string>? JumpLines = null,   // ★2026-09-03（A-05）：跳躍ダイジェスト（順位跳躍・政変連発・人口の物理上限超）を種類で畳んだ内訳。報告専用＝硬性ゲートには入れない
    IReadOnlyList<string>? HarnessIssueLines = null,   // 2026-07-31：合格しなかった検査を**検査名で畳んだ**内訳（seedと実測値つき）。要注意フラグとは別欄＝硬性ゲートの0件判定を汚さない
    double AvgWarMaxRepeat = 0, double AvgWarPairsGe3 = 0, double AvgWarPairs = 0, string WarWorstPair = "",
    double AvgInterveneChances = 0, double AvgPeaceOffers = 0, double AvgPeaceBlank = 0, double AvgPeaceRefused = 0,   // ★診断2026-08-01：同一ペアの再戦（平均最多回数・3回以上ペア数・交戦ペア数・全seed中の最悪ペア）
    IReadOnlyList<(string Nation, double Naval, double Land)>? NavalRanking = null,   // ★2026-08-01（作者裁定「海軍の強さは測定するべき」）：1914断面の海軍力の順位（全seed平均・上位のみ）
    double AvgElections = 0, double AvgElectionIntervalQ = 0,   // ★E0計器（選挙doc §8-a・2026-08-15）：1試合あたりの総選挙回数と平均間隔（四半期）。史実英1832-1914＝約15.6Q
    double AvgTradeGdpMajors = 0,   // ★貿易復活計器（2026-08-15）：6大国の終局 貿易/GDP 平均（史実1913英≈0.4）
    // ★史実基準の恐慌計器（2026-08-22・作者指摘「恐慌回数・最大DDは史実基準ですか？そうでないならハーネスが間違っている」）：
    //   大国8（GdpRank1913 参照国＝経済規模次元と同じ集合）の−20%級エピソード数（試合平均）と最大DD（全seed最大）＝Maddison系の史実と直接比較できる唯一の切り口。
    //   史実参照（実質GDP・1783-1934）＝大国8で計**5〜7回**・ほぼ全て1914-34の大戦/大恐慌型（露WWI+内戦≈40%・仏墺≈30%・米29-33≈27%）・
    //   **1783-1913の平時はほぼ0**（長い19世紀のパニックは実質−5〜10%の不況＝−20%崩落ではない）。
    double AvgMajorDepressionEpisodes = 0, double MaxMajorDrawdown = 0, string MaxMajorDdNote = "",
    long[]? CostTotals = null,   // ★計算量の台帳（2026-09-30）＝全試合の見た件数の和（CostLedger の添字）。報告専用＝帯の報告には書かない（Runner が別のファイルへ）
    IReadOnlyList<(ulong Seed, double Exact, double Near)>? SeedFidelity = null);   // ★止める規則（2026-09-30 計算量の予算 第2回）＝種ごとの政体 exact／near（その試合だけで照合した国の平均）＝標準誤差の材料。報告専用＝帯の報告には書かない（Runner が <出力>.cost へ）

/// <summary>各国に効用AIを差し、1試合を最後まで自動進行して構造化結果を返す。seed 固定で再現可能。</summary>
public static class SelfPlayMatch
{
    // マジックナンバー命名（挙動不変・値据え置き）。
    // 慢性赤字の異常閾値（2026-07-21・率へ是正）。旧＝累積の絶対値1000万で判定していたが、LossFinanced は減算のないグロス累積のため
    // 走行年数と経済規模に比例して必ず超え、実測でQ425(1889)通過・以後単調＝「健全性」でなく「通過時刻」を報告していた（30/30は恒真）。
    // 過去に5万→1000万へ200倍したが再点灯した前例もあり、閾値を上げる対処は無効。分母を入れて次元を合わせるのが正着。
    // **閾値の較正（2026-07-21・実測）**：604四半期×seed1-4 のピーク率＝20.35 / 17.36 / 23.64 / 19.32 %（平均≈20%・σ≈2.7pp）。
    // 累積は同じ4本で 49.6M / 39.6M / 12.8M / 56.5M と4.4倍ばらつき、ピーク率とほぼ無相関（seed3は累積最小なのにピーク最大）＝
    // 旧指標の絶対累積は実ストレスと別物を測っていた。全4本ともハーネスは〇10 △0 ×0（全検査合格）で、20%前後は**この経済の構造的な平常値**。
    // よって閾値は実測平常帯（最大23.6%）の上に十分な余裕を取り 35% とする（≈平常平均の1.75倍・σにして約5.5）。
    // ゴールポスト移動ではない根拠＝旧判定は構造上100%の走行で必ず点灯（通過時刻の報告）だったのに対し、本閾値は
    // 実測した平常分布の外側に置いており、かつ経済の健全性は独立の証拠（ハーネス全合格・貯蓄ゼロ床2%・倒産9%稼働・率はプラトー）で裏付けられている。
    // ★分母の床（2026-08-11）＝この率は「負債化フロー ÷ 名目産出」なので、**産出が痩せた国では分母だけで跳ねる**。
    //   旧ガードは `産出 > 1.0`＝**通貨の絶対額**で、世界の産出が百万単位の盤面では事実上ゼロ除算を許していた
    //   （実測：seed6 の マレー王国 が Q572 に四半期産出 £1 で 13,537% を記録＝£135の損失で硬性ゲートが赤になった）。
    //   閾値0.35は本物の国（産出が百万単位）で較正した量なので、そこへ残骸国家を混ぜてはいけない。
    //   床は**その時の世界の平均産出に対する割合**＝盤面の規模が変わっても意味が変わらない。
    //   ⚠除外は黙って行わない＝除外した国と四半期を数え、バッチ出力に出す（silent cap を作らない）。
    private const double LossRateMinOutputShare = 0.001;              // 世界の1国あたり平均産出のこの割合を下回る国は率を計上しない（1/1000＝残骸）
    private const double LossFinancedRateThreshold = 0.35;            // 負債化フロー ÷ 名目産出（四半期・EMA平滑）のピークがこれを超えたら異常
    private const int DeadlockFrozenQuarters = 20;                    // スコア不変がこの四半期数を超えたらデッドロック疑い
    // ★恐慌の閾値の唯一の定義（fis-24・2026-09-26）：画面の通知（GameViewModel）・年代記（Runner の chronicle）・このバッチが同じ3本を読む
    //   （旧＝画面とバッチと年代記に同じ値の写しが3組あった）。⚠物差しはまだ2つ：画面と年代記＝エンジンの World.GdpPcDrawdown（1人あたり・
    //   ピークの忘却あり＝作者裁定E2）／バッチ＝下の自前の EMA（総額・ピークの忘却なし・大国8だけ1人あたり）。バッチを揃えると帯の恐慌の数が変わる
    //   （段0＝帯を動かさない回なので持ち越し）。
    public const double DepressionEnterDrawdown = 0.20;               // 恐慌（景気後退）入り＝ピーク比の収縮がこれ超
    public const double DepressionExitDrawdown = 0.08;                // 回復＝ここまで戻れば脱出（入り0.20／出0.08の2値＝境界の点滅を止める）
    public const double GreatDepressionDrawdown = 0.25;               // 大恐慌（ピーク比収縮）＝画面の語も「恐慌」（以下は「景気後退」）
    private const double LateDecisionQuarterFraction = 0.9;           // 決着が遅い（総四半期に対する割合）
    private const double HeatPeakDecay = 0.97;                        // 直近ピーク過熱の減衰
    private const double HarvestBadDecay = 0.9;                       // 直近不作の深さの減衰

    public static MatchResult Run(ulong seed, int quarters = Coefficients.GameQuarters, double? interestRate = null, bool lean = false)
    {
        NationConfig[] configs = WorldData.Nations();   // 世界データ（world.json）から全国家。国の追加＝world.json編集のみ（Phase 3＝地ならし）
        // 監査2026-07-12（他19）：金利実験は複製へ書く＝WorldData.Nations()の共有キャッシュを恒久汚染しない（旧＝実験後の同一プロセス実行が実験値の初期金利で汚染される地雷）。
        if (interestRate is { } ir)
            configs = [.. configs.Select(cfg => { var c2 = cfg.ShallowClone(); c2.InitialInterestRate = ir; return c2; })];
        var sim = new MultiNationSim(configs, seed, _ => new UtilityAgent());   // 挙動は政体別＝DecideQuarter内で動的に決まる
        // 測定短縮①（2026-07-09）：Telemetryは読者なし＝常時停止（テストは個別Simulationを使うので不干渉）。
        // lean（バッチ用）＝外交ログも止める（並列30試合×3千行の保持を削減）。開戦数はWarsStartedカウンタ＝Validationは不変。
        foreach (var nsim in sim.Nations) nsim.TelemetryEnabled = false;
        if (lean) sim.Diplomacy.Log.Enabled = false;
        // ★開始前の助走（作者裁定2026-08-10）＝**Appの新規ゲームと同じ1本**（MultiNationSim.StartWarmup）。
        // ⚠2026-08-10以前は App だけが助走（4四半期＋技術8反復）を通り、ここは素の盤面から走っていた＝
        //   「遊ぶ世界」と「測る世界」の初期条件が別物だった（帯は、プレイヤーが決して見ない開始価格を測っていた）。
        // 助走は暦を進めない（World.WarmupFrozen）＝1783/Q1 のまま開始＝1914断面(Q524)も全長 GameQuarters も不変。
        // Telemetry を切った**後**に呼ぶ＝助走の四半期でスナップショットを積まない（バッチのメモリを増やさない）。
        sim.StartWarmup();
        // ★異常リストの有界化（2026-08-01・docs/20260801「最終調整ハーネスの設計」§1-4）。
        // ⚠旧＝**非有界の文字列リスト**。異常②（工業化度の逸脱）は条件が続くかぎり毎四半期×毎国で1行積むので
        //   最悪 54カ国×604四半期＝約3.2万行／試合。96シードでは数百万の文字列が同時に生存する＝OOM の主因だった
        //   （実測：2026-07-19のフルスイート並列実行でホストごと落ちた事故と同じ筋）。
        // ★合否が読むのは **件数** だけ（ゲート＝「異常0件」）なので、**文字列は先頭AnomalySampleCapまで**に切り、
        //   総数は別のカウンタで正確に数える。⚠件数の意味は変えない＝ゲートの判定は旧と一致する。
        var anomalies = new BoundedList(AnomalySampleCap);
        var prevScores = new double[configs.Length];
        string lastLeader = "";
        int decisionQ = 0, frozen = 0, leadChanges = 0;
        // ★A-05 跳躍ダイジェスト（報告専用）：年に1回だけ順位と人口を撮り、跳んだ国を記録する。政変は毎四半期のエッジ。
        var jumps = new BoundedList(AnomalySampleCap);
        var jumpRankPrev = new int[configs.Length];
        var jumpPopPrev = new double[configs.Length];
        var jumpPolityPrev = new Polity[configs.Length];
        var jumpFlipQ = new List<int>[configs.Length];
        for (int i0 = 0; i0 < configs.Length; i0++) { jumpRankPrev[i0] = -1; jumpFlipQ[i0] = new List<int>(); jumpPolityPrev[i0] = sim.Nations[i0].World.Polity; }
        List<NationStanding>? standings1914 = null;   // 忠実度の1914断面（Q524）。1934延長2026-07-19：物差しは1914固定＝走行中に捕捉
        List<NationStanding>? standings1820 = null, standings1870 = null, standings1920 = null, standings1950 = null;   // ★E-08（2026-09-05）：1820/1870年12月の断面（併記用・主指標は1914のまま）＋1920/1950
        // B-中7（史実検証2026-07-22）：人口の史実照合のため開始時（1783年）の国別人口を捕捉する。
        // 忠実度表に人口軸が無いため「テーマBの是正が帯に一切露出しない」＝計測器の欠落だった。領土不変な定義にするため
        // 絶対人口でなく**1783年比の成長倍率**を測る（本ゲームは国を消さず併合も起きるので絶対値は定義が噛み合わない）。
        var initPop = new Dictionary<string, double>();
        foreach (var n0 in sim.Nations)
        {
            string key0 = n0.World.SelfPower?.BaseName ?? n0.World.Region.Name;
            if (!initPop.ContainsKey(key0)) initPop[key0] = n0.World.TotalPopulation;
        }
        // 08の70%計器（2026-07-31）：領土次元の分子＝**実効支配している本国州の数**。割譲・分離独立で減り、征服で増える。
        static int HomeProvinceCount(World w)
        {
            int n = 0;
            // ★2026-09-03（第0波W0-01）：**本国州だけ**を数える。ColoniesAreRegions=1 で植民地が州になって以降、
            //   ここは植民地格の州も本国州として数えており、忠実度の領土次元（HomeProvinces1914 との比）が
            //   分子だけ膨らんでいた＝**物差しそのものが壊れていた**（分子と分母の非対称の型）。
            //   ⚠差し戻し口＝Tier の条件を外せば旧挙動。観測専用（力学はこの値を読まない）。
            foreach (var r in w.Regions) if (r.State == RegionState.Active && r.Tier != RegionTier.Colonial) n++;
            return n;
        }
        List<NationStanding> BuildStandings() => sim.Nations
            .Select(n =>
            {
                var w = n.World;
                string bn = w.SelfPower?.BaseName ?? w.Region.Name;
                // 案Y（人口物差しの脱汚染・2026-08-21・親承認）：PopGrowth＝**併合の持ち込み人口（Region.AnnexBaggage）を分子から除いた**1783年比
                // ＝宣言（NationStanding.PopGrowth の注記「領土不変な定義」）どおりの実装。旧値（総人口比＝征服インフレ込み）は PopGrowthGross として併走＝新旧併記の橋
                // （宗主厳格化2026-07-12・導出政体2026-08-21と同型）。分離・割譲で州を**失う**側は分子が減ったまま＝「帯を割る」設計意図を保存。
                // Σの範囲は TotalPopulation と同じ非Seceded＝州が去れば荷物ごと去る・内部移住にも自動整合（案X初期州フィルタの欠陥を回避）。
                double baggage = 0; foreach (var rb in w.Regions) baggage += rb.AnnexBaggage;
                double growthGross = initPop.TryGetValue(bn, out var p0) && p0 > 1e-9 ? w.TotalPopulation / p0 : 0;   // 0=初期人口不明＝測定対象外（建国スポーンなど）
                double growth = growthGross > 0 ? System.Math.Max(0, w.TotalPopulation - baggage) / p0 : 0;
                return new NationStanding(w.Region.Name, NationalScore.Standing(w), w.TechLevel,   // 序列は在庫（2026-08-02）＝忠実度の列強軸も首位交代も同じ物差しで見る
                    w.MeanIndustrialization, NationalScore.MeanSatisfaction(w), w.SelfPower?.Colonies ?? 0, w.Gdp,
                    w.TotalPopulation, w.SelfPower?.Military ?? 0, w.Polity, w.WarDeadCumulative, w.Tariff,
                    w.Suffrage, w.EducationLaw, w.Censorship, w.LaborUnion, w.WorkHours, w.CornLaws, w.LandReform, w.SerfEmancipation, w.Pension,
                    PoliticalQuantities.MilitaryPoliticalRatio(w), w.ConscriptionLaw,   // ★軍の政治的優位（導出量）＋徴兵法（法）＝2026-08-18の親軍法撤去で bool 1本が2本に分かれた
                    w.SelfPower?.BaseName ?? w.Region.Name,
                    (w.SelfPower is { } sp2 ? sim.Diplomacy.Suzerain(sp2)?.BaseName : null) ?? "",   // 主権軸：宗主（BaseName＝変名後もSuzerainsOkと恒久一致）
                    w.SelfPower?.Name ?? "",   // 表示名（変名の物語はこちら）
                    growth,   // B-中7：1783年比の人口成長倍率
                    HomeProvinceCount(w),   // 08の70%計器（2026-07-31）：本国州の保有数＝領土次元の分子
                    w.SelfPower?.NavalPower ?? 0, w.SelfPower?.LandPower ?? 0, w.Regions.Where(rr => rr.State == RegionState.Active).Sum(rr => rr.FleetStock),   // ★2026-08-01：陸海の内訳（海軍力の測定＝作者裁定）
                    // ★★物差し是正（2026-08-21・作者裁定「全て採用」）：旧＝分子（輸出入）は四半期フロー×4＝**年額**、分母 NominalGdp は**月次フロー**
                    //   ＝「年額÷月額」で**12倍過大**だった。⚠債務/GDPは同じ問題を DebtGdpAnnualized=1.0（2026-07-26点火）で是正済み＝
                    //   貿易だけ取り残された内部矛盾の修正（Fiscal.cs 財7「債務/GDPの単位整合」の注記が「NominalGdp は月次フロー」と明記）。
                    //   分母を ×MonthsPerYear で年換算＝英1913 の実測は 0.43%→0.034%＝史実25%との差は×17でなく**×190**
                    //   ＝「較正で届く段階ではなく構造をもう一段変える段階」と物差しが正しく教えるようになる。
                    ((w.ExportsByGood?.Sum() ?? 0) + (w.ImportsByGood?.Sum() ?? 0)) * w.Coef.ClearingsPerYear / System.Math.Max(1.0, w.NominalGdp * Calendar.MonthsPerYear),   // 清算単位→年換算（月次清算に追随）
                    PoliticalQuantities.MilitaryPush(w), PoliticalQuantities.CivilianValve(w),   // ★計器（2026-08-18）：軍の政治的優位の分子と分母。⚠合成値だけを見て2度誤ったので内訳を運ぶ
                    PoliticalQuantities.AppointmentAtCourt(w), w.Legitimacy,                      //   弁の2項（任免の所在・正統性）＝弁が飽和しているのか片方が死んでいるのかを分ける
                    PowerAffinity.PolityFromShares(w),                                            // ★導出政体（2026-08-21）＝忠実度の政体次元の新しい物差し
                    growthGross,                                                                  // 案Y：旧定義（総人口比＝征服インフレ込み）の併走＝新旧比較の橋
                    (w.SelfPower?.ColonyMassUnits(w.Coef.ColonialPopScale) ?? 0)
                        * (sim.Diplomacy.InitialColonyMass > 1e-9 ? sim.Diplomacy.InitialColonyCount / sim.Diplomacy.InitialColonyMass : 1.0));   // ★2026-08-28（裁定③）：質量を**個数単位へ換算**（錨＝初期の 個数/質量。1783年で両計測が厳密一致＝参照値の目盛りを保つ）
            })
            .OrderByDescending(s => s.Score).ToList();
        // 恐慌計測（要望3）：各国の平滑化GDP（EMA・トレンド）のピークからの収縮を追う。高頻度ノイズでなく持続的な収縮＝恐慌。
        // 助走後から。トレンド比20%収縮で恐慌入り・8%まで回復で脱出。
        var gdpPeak = new double[configs.Length];
        var gdpTrend = new double[configs.Length];
        var inDepression = new bool[configs.Length];
        var heatPeak = new double[configs.Length];   // 直近のピーク過熱（減衰保持）＝恐慌が信用バブルに先行されたかの判定
        var harvestBad = new double[configs.Length];  // 直近の不作の深さ（減衰保持）＝恐慌が不作に先行されたかの判定
        // 慢性赤字の計測（2026-07-21・多エージェント調査で指標を是正）：LossFinanced はグロス累積（単調非減少・減算なし）なので
        // 絶対値を固定閾値と比べると、走行年数と経済規模に比例して必ず超える（実測＝Q425で通過し以後単調・財カタログ増で同一断面が9倍）。
        // つまり旧判定は健全性でなく「閾値の通過時刻」を報告していた。よって当期の負債化フローを同期の名目産出で割った**率**で見る
        // （同期間フロー同士の比＝次元が合い、規模にも年数にも非依存）。単発スパイクで鳴らないようEMAで平滑化する。
        var prevLoss = new double[configs.Length];    // 前四半期の LossFinanced（差分＝当期の負債化フロー）
        var lossRate = new double[configs.Length];    // 負債化フロー ÷ 名目産出（四半期）の平滑値
        var satWindow = new List<double[]>(Validation.SatRecentQuarters);   // ★(68) 直近の四半期ごとの各国の満足度（ハーネスの最小国に添える・報告専用＝力学に触れない）
        double worstLossRate = 0;
        string worstLossNation = ""; double worstLossGdpQ = 0; int worstLossQuarter = -1;
        double worldOutPrev = 0, worldOutThis = 0;   // 世界の四半期名目産出（当期は集計中なので床には前期を使う＝決定論）
        int lossRateSkipped = 0; string lossRateSkipWorst = "";   // 分母の床で計上しなかった件数と、その代表国   // ピークを出した国・その時の四半期産出・四半期（診断2026-08-11）
        double worstDrawdown = 0; int depressionEpisodes = 0, overheatDepressions = 0, harvestDepressions = 0;
        // ★史実基準化（2026-08-22・作者指摘「恐慌回数・最大DDは史実基準ですか？そうでないならハーネスが間違っている」）。
        //   旧＝worstDrawdown の床が gdpPeak>£1 だけ＝**領土を失って消えていく国の〜100%収縮を拾う**（92〜97%は国家死の読み値＝経済恐慌の物差しではない）。
        //   新①＝DD は産出床つき（世界の1国あたり平均GDPトレンドの LossRateMinOutputShare 倍＝既存の残骸除外と同じ物差し・新定数なし）。
        //   新②＝**大国8（経済規模次元と同じ GdpRank1913 参照国）**のエピソード数と最大DDを別建てで数える＝史実（Maddison系）と直接比較できる唯一の切り口。
        //   史実参照（実質GDP・ピーク比−20%級・1783-1934）＝大国8で**計5〜7回**（露WWI+内戦≈−40%・仏墺WWI≈−30%・米1929-33≈−27%・独WWI≈−25%・英1919-21≈−20%が縁・日=0）。
        //   **1783-1913の平時に大国の−20%級はほぼ0**＝長い19世紀の恐慌（1825/1873/1893…）は実質GDPでは−5〜−10%の不況であって−20%崩落ではない。
        double majorMaxDrawdown = 0; int majorDepressionEpisodes = 0; string majorDdNote = "";
        // ★大国8の計器は**1人あたり**実質GDP（w.Gdp/TotalPopulation）で測る（2026-08-22・実測＝最大DDの出所が仏1808の7州→5州・米1923の解体＝征服で領土と人口が同時に消える事象）。
        //   総額DDは国家解体を拾う（史実の大国は1783-1913に解体されていない）。1人あたりなら割譲は分子分母が同時に減り、残るのは**経済の収縮**＝史実のDD（米1929-33≈−27%・露WWI≈−40%）と同じ物差し。
        var gdpPcTrend = new double[sim.Nations.Count]; var gdpPcPeak = new double[sim.Nations.Count]; var inMajorDep = new bool[sim.Nations.Count];
        double worldTrendPrev = 0, worldTrendThis = 0;   // DD床用＝世界のGDPトレンド計（前期値を床に使う＝当期は集計中・lossRate と同じ作法）
        var majorSet = new System.Collections.Generic.HashSet<string>();
        foreach (var he in HistoricalReference.Table) if (he.GdpRank1913 is not null) majorSet.Add(he.Nation);
        var isMajor = new bool[sim.Nations.Count];
        for (int i = 0; i < sim.Nations.Count; i++)
            isMajor[i] = sim.Nations[i].World.SelfPower is { } mp && majorSet.Contains(mp.BaseName.Length > 0 ? mp.BaseName : mp.Name);
        const double depEnter = DepressionEnterDrawdown, depExit = DepressionExitDrawdown, warmupQ = 12, gdpSmooth = 0.18, overheatMark = 0.55, harvestMark = 0.12;
        const double lossRateSmooth = 0.18, monthsPerQuarter = 3.0;

        try
        {
            for (int q = 0; q < quarters; q++)
            {
                sim.StepQuarter();
                if (sim.Nations.Count > gdpPeak.Length)   // 建国スポーン（SecessionSpawnsNation）＝末尾拡張のみ・既存添字は不変＝決定論不変
                {
                    int nn = sim.Nations.Count;
                    System.Array.Resize(ref prevScores, nn); System.Array.Resize(ref gdpPeak, nn); System.Array.Resize(ref gdpTrend, nn);
                    System.Array.Resize(ref inDepression, nn); System.Array.Resize(ref heatPeak, nn); System.Array.Resize(ref harvestBad, nn);
                    System.Array.Resize(ref prevLoss, nn); System.Array.Resize(ref lossRate, nn);   // 建国スポーン＝末尾拡張（新国は prevLoss=0 から差分開始）
                    System.Array.Resize(ref isMajor, nn);   // 建国スポーンは大国8ではない（GdpRank1913 参照は既存国のみ）＝false のまま
                    System.Array.Resize(ref gdpPcTrend, nn); System.Array.Resize(ref gdpPcPeak, nn); System.Array.Resize(ref inMajorDep, nn);
                }
                var scores = new double[sim.Nations.Count];
                worldOutPrev = worldOutThis; worldOutThis = 0;   // 分母の床に使う世界計＝**前の四半期**の値（当期は今から集計）
                worldTrendPrev = worldTrendThis; worldTrendThis = 0;   // DD床用も同じ作法（前期の世界GDPトレンド計）
                for (int i = 0; i < sim.Nations.Count; i++)
                {
                    var w = sim.Nations[i].World;
                    double sc = NationalScore.Of(w);
                    scores[i] = NationalScore.Standing(w);   // デッドロック/首位交代の判定も在庫で（序列と同じ物差し）
                    // ★異常検出は**生のフロー**を見る＝在庫は平滑されるので NaN の混入を隠しうる。両方を検査する。
                    if (double.IsNaN(sc) || double.IsInfinity(sc) || double.IsNaN(scores[i]) || double.IsInfinity(scores[i]))
                        anomalies.Add($"Q{q + 1} {w.Region.Name}: スコアが NaN/∞");
                    if (!(w.Region.TotalIndustrialization is >= 0 and <= 10))   // 否定形＝NaN も異常に数える（有限値では旧 `> 10 or < 0` と同値）
                        anomalies.Add($"Q{q + 1} {w.Region.Name}: 工業化度が異常（{w.Region.TotalIndustrialization:F2}）");   // 暴走監視は新旧合計（ヴィンテージ2026-07-10）
                    // 法の序数が定義域の外（2026-08-02の番人）：★enum は範囲を強制しないので、クランプを1つ忘れると
                    // 静かに定義外の値が入り、下流の (int)law/2.0 のような正規化を丸ごと汚す（実際に LandReform=601 が発生した）。
                    // 硬性ゲートの「異常0」に載せる＝以後この種の取りこぼしは必ずバッチで落ちる。現在値と目標の両方を見る。
                    if (Validation.LawOutOfRange(w) is { } bad) anomalies.Add($"Q{q + 1} {w.Region.Name}: 法の値が定義域外（{bad}）");
                    // 慢性赤字：当期の負債化フロー ÷ 同期の名目産出。prevLoss は助走中も更新して差分の取りこぼしを防ぐ。
                    double dLoss = System.Math.Max(0, w.LossFinanced - prevLoss[i]);   // 累積は単調非減少だが分離/併合の再編に備えて負をクランプ
                    prevLoss[i] = w.LossFinanced;
                    // ★床の材料（worldOutThis）は助走中も積む（wave2b G06#5）：旧＝ q>=warmupQ の期しか積まず、
                    //   warmupQ ちょうどの1期だけ worldOutPrev==0 で floorOut が絶対額1.0（通貨の絶対額＝:1475 のコメントが
                    //   「誤り」と明記する床）に落ちていた。outQ は warmup に依存しない値＝計上先だけ外へ出す。
                    double outQ = w.NominalGdp * monthsPerQuarter;   // NominalGdp は月次フロー（Fiscal.cs 財7の注記）
                    worldOutThis += outQ;
                    if (q >= warmupQ)
                    {
                        // 分母の床＝世界の1国あたり平均産出の LossRateMinOutputShare 倍。前期の世界計を使う（当期は集計中）。
                        double floorOut = worldOutPrev > 0 ? worldOutPrev / System.Math.Max(1, sim.Nations.Count) * LossRateMinOutputShare : 1.0;
                        if (outQ <= floorOut) { if (dLoss > 0) { lossRateSkipped++; if (lossRateSkipWorst.Length == 0) lossRateSkipWorst = $"{w.SelfPower?.Name}(Q{q + 1}・産出{outQ:F0})"; } }
                        else
                        {
                            lossRate[i] += lossRateSmooth * (dLoss / outQ - lossRate[i]);
                            if (lossRate[i] > worstLossRate)
                            {
                                worstLossRate = lossRate[i];
                                // ★誰の何が起きたかを一緒に持つ（2026-08-11）＝率は比なので分母が痩せただけでも跳ねる。
                                //   国名・その時の四半期産出・四半期を残せば、次に赤が出た時に「本物の慢性赤字」か
                                //   「産出が消えた残余国の見かけ」かを、再走なしで切り分けられる。
                                worstLossNation = w.SelfPower?.Name ?? w.Region.Name;
                                worstLossGdpQ = outQ; worstLossQuarter = q + 1;
                            }
                        }
                    }
                    // 恐慌計測：助走後、平滑化GDP（トレンド）のピーク比収縮を追う
                    if (q >= warmupQ)
                    {
                        gdpTrend[i] += gdpSmooth * (w.Gdp - gdpTrend[i]);   // EMA＝高頻度の市場ノイズを除去
                        if (gdpTrend[i] > gdpPeak[i]) gdpPeak[i] = gdpTrend[i];
                        worldTrendThis += gdpTrend[i];
                        // ★DDの産出床（史実基準化）：残骸国家（世界平均の1/1000未満）の〜100%収縮を世界DDに数えない。
                        //   床の物差しは lossRate の分母床と同一（LossRateMinOutputShare＝新定数なし）。床未満は自国のエピソード判定にも入れない
                        //  （領土をほぼ失った国の「経済恐慌」は国家死の別事象＝恐慌の物差しから外す）。
                        double ddFloor = worldTrendPrev > 0 ? worldTrendPrev / System.Math.Max(1, sim.Nations.Count) * LossRateMinOutputShare : 1.0;
                        double dd = gdpPeak[i] > ddFloor ? 1.0 - gdpTrend[i] / gdpPeak[i] : 0;
                        if (dd > worstDrawdown) worstDrawdown = dd;
                        if (isMajor[i])
                        {
                            double pc = w.Gdp / System.Math.Max(1.0, w.TotalPopulation);
                            gdpPcTrend[i] += gdpSmooth * (pc - gdpPcTrend[i]);
                            if (gdpPcTrend[i] > gdpPcPeak[i]) gdpPcPeak[i] = gdpPcTrend[i];
                            double ddPc = gdpPcPeak[i] > 0 && gdpPeak[i] > ddFloor ? 1.0 - gdpPcTrend[i] / gdpPcPeak[i] : 0;
                            if (ddPc > majorMaxDrawdown)
                            {
                                majorMaxDrawdown = ddPc;
                                majorDdNote = $"{w.SelfPower?.Name ?? w.Region.Name}・Q{q + 1}{(w.AtWarNow ? "・戦時" : "")}";   // ★出所＋戦時タグ（再走なしで切り分ける＝worstLossNation と同じ作法。戦時なら対策の対象外＝征服/戦災）
                            }
                            if (!inMajorDep[i] && ddPc > depEnter) { inMajorDep[i] = true; majorDepressionEpisodes++; }
                            else if (inMajorDep[i] && ddPc < depExit) inMajorDep[i] = false;
                        }
                        heatPeak[i] = Math.Max(w.FinancialHeat, heatPeak[i] * HeatPeakDecay);    // 直近ピーク過熱（緩やかに減衰。貸し剥がしは一夜＝恐慌と診断される頃にゲージ自体が蒸発しているため記憶は長く持つ）
                        harvestBad[i] = Math.Max(1.0 - w.HarvestFactor, harvestBad[i] * HarvestBadDecay);   // 直近の不作の深さ（緩やかに減衰）
                        if (!inDepression[i] && dd > depEnter)
                        {
                            inDepression[i] = true; depressionEpisodes++;
                            if (heatPeak[i] > overheatMark) overheatDepressions++;     // 信用バブルに先行＝金融政策で避けられた類型
                            if (harvestBad[i] > harvestMark) harvestDepressions++;     // 不作に先行＝実体ショック起因（食糧安保・福祉で緩衝）
                        }
                        else if (inDepression[i] && dd < depExit) inDepression[i] = false;
                    }
                    else { gdpTrend[i] = w.Gdp; gdpPeak[i] = w.Gdp; double pc0 = w.Gdp / System.Math.Max(1.0, w.TotalPopulation); gdpPcTrend[i] = pc0; gdpPcPeak[i] = pc0; }   // 助走中はトレンドとピークを初期化（立ち上げを恐慌と誤認しない）
                }
                if (q >= quarters - Validation.SatRecentQuarters)   // ★(68) 添字は sim.Nations と同じ（Metrics.Compute と同じ順）
                    satWindow.Add([.. sim.Nations.Select(n => NationalScore.MeanSatisfaction(n.World))]);
                if (q + 1 == Coefficients.FidelityQuarters && quarters > Coefficients.FidelityQuarters)
                    standings1914 = BuildStandings();
                if (q + 1 == Coefficients.FidelityQuarters1820 && quarters > Coefficients.FidelityQuarters1820) standings1820 = BuildStandings();   // ★E-08
                if (q + 1 == Coefficients.FidelityQuarters1870 && quarters > Coefficients.FidelityQuarters1870) standings1870 = BuildStandings();
                if (q + 1 == Coefficients.FidelityQuarters1920 && quarters > Coefficients.FidelityQuarters1920) standings1920 = BuildStandings();   // ★E-09   // 1914年12月の断面＝忠実度の照合対象（終局まで走っても物差しは1914）
                if (q + 1 == Coefficients.FidelityQuarters1950) standings1950 = BuildStandings();   // ★1950断面＝終局の四半期（最終Standingsへ退避しない＝全長が届かない走行は null＝断面なし）
                int lead = 0;
                for (int i = 1; i < scores.Length; i++) if (scores[i] > scores[lead]) lead = i;
                string leader = sim.Nations[lead].World.SelfPower?.BaseName ?? sim.Nations[lead].World.Region.Name;   // 不変国名で追跡（監査2026-07-12 他17＝首都州名の分裂と同型で、変名（プロイセン→ドイツ）による偽の首位交代も防ぐ）
                if (leader != lastLeader) { decisionQ = q + 1; if (lastLeader.Length > 0) leadChanges++; lastLeader = leader; }   // 首位交代を数える（初回の首位設定は交代に数えない）
                // ★A-05 跳躍ダイジェスト（報告専用＝力学に触れない）。⚠開始時集合の範囲だけ見る（建国スポーンは対象外＝全走行で同一規約）。
                for (int ji = 0; ji < configs.Length && ji < sim.Nations.Count; ji++)
                {
                    var jw = sim.Nations[ji].World;
                    if (jw.Polity != jumpPolityPrev[ji])
                    {
                        jumpPolityPrev[ji] = jw.Polity;
                        var fl = jumpFlipQ[ji]; fl.Add(q);
                        int within = 0; foreach (var fq in fl) if (q - fq <= Calendar.QuartersPerYear * 10) within++;
                        if (within == 3)   // ちょうど3回目で1度だけ報告（4回目以降で毎期鳴らない）
                            jumps.Add($"Q{q + 1} {jw.SelfPower?.BaseName ?? jw.Region.Name}：10年内に政変3回");
                    }
                }
                if ((q + 1) % Calendar.QuartersPerYear == 0)
                {
                    for (int ji = 0; ji < configs.Length && ji < sim.Nations.Count; ji++)
                    {
                        int rank = 1;
                        for (int jj = 0; jj < scores.Length; jj++) if (scores[jj] > scores[ji]) rank++;
                        var jw = sim.Nations[ji].World;
                        string jn = jw.SelfPower?.BaseName ?? jw.Region.Name;
                        if (jumpRankPrev[ji] > 0 && System.Math.Abs(rank - jumpRankPrev[ji]) >= JumpRankSteps)
                            jumps.Add($"Q{q + 1} {jn}：順位{jumpRankPrev[ji]}位→{rank}位（1年で{System.Math.Abs(rank - jumpRankPrev[ji])}ランク）");
                        jumpRankPrev[ji] = rank;
                        double popNow = jw.TotalPopulation;
                        if (jumpPopPrev[ji] > 1e-9 && popNow > jumpPopPrev[ji] * (1.0 + JumpPopGrowthMax))
                            jumps.Add($"Q{q + 1} {jn}：人口が1年で{(popNow / jumpPopPrev[ji] - 1.0):P0}増（併合を含む・物理上限{JumpPopGrowthMax:P0}超）");
                        jumpPopPrev[ji] = popNow;
                    }
                }
                if (q > 0)
                {
                    bool same = true;
                    for (int i = 0; i < scores.Length; i++) if (Math.Abs(scores[i] - prevScores[i]) > 1e-6) { same = false; break; }
                    frozen = same ? frozen + 1 : 0;
                }
                scores.CopyTo(prevScores, 0);
            }
        }
        // OOMだけは握り潰さない（2026-07-27）：ここで捕まえると「破綻(未完走)」の1件として BatchSummary に静かに混入し、
        // 実装バグと区別がつかなくなる。しかもどのシードが先にメモリ上限へ当たるかはスレッドの到達順次第＝runごとに変わり再現しない。
        catch (OutOfMemoryException) { throw; }
        catch (Exception ex)
        {
            anomalies.Add($"例外/保存則違反: {ex.GetType().Name}: {ex.Message}");
            return new MatchResult(seed, quarters, "(中断)", 0, -1, [], [.. sim.Diplomacy.Log], anomalies, false, 0, 0, 0, 0, [], worstDrawdown, depressionEpisodes, overheatDepressions, harvestDepressions, Cost: sim.CostTotals());
        }

        bool deadlocked = frozen > DeadlockFrozenQuarters;   // ★構造フィールドへ（文字列一致をやめる）
        if (deadlocked) anomalies.Add($"デッドロックの疑い：スコアが{frozen}四半期不変");

        var standings = BuildStandings();   // 終局（1950年1月1日）の最終順位＝勝者・スコア・法多様性・政体分布の対象。忠実度だけは standings1914（Q524断面）

        // Round2 2026-07-08：「決着が遅い」を異常から除外。首位が終盤まで争われるのはバランス目標③（独走不可・首位交代）の達成であり
        // 病理ではない（旧＝異常扱いは「決着しないバグ」検出のためだったが、デッドロックは frozen 検査が別途担う）。指標としては BatchSummary.LateDecisions に残す。

        // 検証ハーネスの合否（自己対戦の最終状態）。保存則・満足度などのマクロ健全性を記録。
        var hm = Metrics.Compute(sim);
        // ★(68)：最小国の文言に直近の平均と振れ幅を添える（判定は終局の値のまま＝Validation.SatRecentQuarters の注記）。
        hm = hm with { Nations = [.. hm.Nations.Select((nm, i) =>
        {
            var v = satWindow.Where(a => i < a.Length).Select(a => a[i]).ToList();   // 窓の中で生まれた国は生まれてからの分だけ
            return v.Count > 0 ? nm with { SatRecentMean = v.Average(), SatRecentSwing = v.Max() - v.Min() } : nm;
        })] };
        var hc = Validation.Evaluate(hm);
        int hp = Validation.Count(hc, Verdict.Pass), hw = Validation.Count(hc, Verdict.Warn), hf = Validation.Count(hc, Verdict.Fail);
        var harnessIssues = hc.Where(ch => ch.Verdict != Verdict.Pass)
            .Select(ch => $"{(ch.Verdict == Verdict.Fail ? "×" : "△")} {ch.Name}：実測{ch.Value}（目標{ch.Target}）").ToList();

        double maxLoss = sim.Nations.Max(n => n.World.LossFinanced);   // 累積そのものは記録として MatchResult に残す（判定には使わない）
        if (worstLossRate > LossFinancedRateThreshold)
            anomalies.Add($"損失financing率が高い：ピーク{worstLossRate:P1}（{worstLossNation}・Q{worstLossQuarter}・その四半期の名目産出{worstLossGdpQ:F0}）");
        // ★分母の床で落とした件数は必ず出す（silent cap を作らない）＝「赤が消えた」のか「測るのをやめた」のかを読み手が区別できる。
        if (lossRateSkipped > 0)
            harnessIssues.Add($"（参考）損失financingの計上外：{lossRateSkipped}件が産出の床未満＝残骸国家（例 {lossRateSkipWorst}）");

        var wrs = sim.Diplomacy.WarRepeatStats();   // 純関数（戦争履歴の集計）＝1度だけ
        return new MatchResult(seed, quarters, standings[0].Nation, standings[0].Score, decisionQ,   // 勝者は国名（監査2026-07-12 他17：旧＝首都州名は遷都で分裂＝常勝検出漏れ。忠実度側と同じNation規約へ統一）
            standings, [.. sim.Diplomacy.Log], anomalies, true, hp, hw, hf, maxLoss, harnessIssues, worstDrawdown, depressionEpisodes, overheatDepressions, harvestDepressions, leadChanges,
            sim.Nations.Sum(n => n.World.GoldSuspensions), sim.Nations.Sum(n => n.World.GoldCoverSuspensions),   // GoldCoverMin有効化2026-07-10：兌換停止の頻度（leanでも数えるスカラーカウンタ）
            sim.Nations.Count - configs.Length, sim.Diplomacy.Powers.Count(p => p.FormationTier > 0),   // 建国数・変名数（状態由来）
            sim.Nations.Sum(n => n.World.CivilWarSplitCount),   // ★内戦分裂の成立回数（2026-08-13に配線＝それまで読み手ゼロ）
            standings1914,   // 1914断面（延長走行のみ非null）＝忠実度の照合対象
            deadlocked,      // ★ゲート3是正2：構造フィールド
            worstLossRate,   // 慢性赤字の判定量（負債化フロー÷名目産出のピーク・2026-07-21）
            worstLossNation, worstLossGdpQ, worstLossQuarter,   // ★そのピークの出所（診断2026-08-11）＝分母が痩せただけかを切り分ける
            wrs.MaxRepeat, wrs.PairsGe3, wrs.Pairs, wrs.Worst,   // ★診断2026-08-01：同一ペアの再戦
            sim.Diplomacy.InterventionChances, sim.Diplomacy.PeaceOffersMade,
            sim.Diplomacy.PeaceOffersBlank, sim.Diplomacy.PeaceOffersRefused,
            sim.Nations.Sum(n => n.World.ElectionsHeld), sim.Nations.Sum(n => n.World.QuartersWithElections),   // ★E0計器（2026-08-15）
            MajorsTradeGdp(sim),   // ★貿易復活計器（2026-08-15）
            MajorDepressionEpisodes: majorDepressionEpisodes, MajorMaxDrawdown: majorMaxDrawdown, MajorDdNote: majorDdNote,
            Jumps: [.. jumps], StandingsAt1820: standings1820, StandingsAt1870: standings1870, StandingsAt1920: standings1920, StandingsAt1950: standings1950,   // ★A-05：跳躍ダイジェスト（報告専用）
            Cost: sim.CostTotals());   // ★計算量の台帳（2026-09-30・報告専用＝読むのは Runner の測りのファイルと番人だけ）
    }

    private static readonly string[] TradeGdpMajorNames = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    // ★貿易復活計器：6大国の (輸出+輸入)×年換算/GDP の平均（終局断面・BaseName鍵＝変名不壊）。ExportsByGood/ImportsByGood は
    //   BilateralMarket が清算ごとに Array.Clear して書く清算単位の値（月次清算では直近1か月分＝ClearingsPerYear で年換算。TariffBorderOnly>0では国際のみ）。
    private static double MajorsTradeGdp(MultiNationSim sim)
    {
        double sum = 0; int n = 0;
        foreach (var nation in sim.Nations)
        {
            var w = nation.World; var p = w.SelfPower; if (p is null) continue;
            if (System.Array.IndexOf(TradeGdpMajorNames, p.BaseName.Length > 0 ? p.BaseName : p.Name) < 0) continue;
            double ex = 0, im = 0;
            if (w.ExportsByGood is { } eg) foreach (var v in eg) ex += v;
            if (w.ImportsByGood is { } ig) foreach (var v in ig) im += v;
            sum += (ex + im) * w.Coef.ClearingsPerYear / System.Math.Max(1.0, w.NominalGdp * Calendar.MonthsPerYear); n++;   // ★物差し是正（2026-08-21）＝分母を年換算（上の TradeGdp と同一の是正・単位＝年額÷年額）。分子は清算単位→年換算（月次清算に追随）
        }
        return n > 0 ? sum / n : 0;
    }

    // ★A-05 跳躍ダイジェストの目盛り（報告専用＝力学に一切入らない）。出典＝トリアージ 10-2 の記述そのまま。
    private const int JumpRankSteps = 10;            // 1年で順位がこれ以上跳んだら報告
    private const double JumpPopGrowthMax = 0.10;    // 1年の人口増がこれを超えたら報告（自然増の物理上限＝併合・征服の混入を炙り出す）
    private const int AnomalySampleCap = 64;   // 標本として残す異常メッセージの本数（総数は別に数える）

    /// <summary>件数は正確に数え、文字列は先頭 cap 本だけ保持するリスト（異常リストのOOM対策・2026-08-01）。
    /// ⚠Count は**打ち切り前の真の件数**を返す＝ゲート「異常0件」の判定は旧実装と厳密に一致する。
    /// 添字アクセスは保持している標本の範囲だけ有効（読み手は `Anomalies[^1]` と全列挙のみ＝下でクランプする）。</summary>
    private sealed class BoundedList(int cap) : IReadOnlyList<string>
    {
        private readonly List<string> _kept = [];
        private int _total;
        public void Add(string line) { _total++; if (_kept.Count < cap) _kept.Add(line); }
        public int Count => _total;                       // ★真の件数（打ち切りに影響されない）
        public string this[int i] => _kept.Count == 0 ? "" : _kept[System.Math.Clamp(i, 0, _kept.Count - 1)];
        public IEnumerator<string> GetEnumerator() => _kept.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _kept.GetEnumerator();
    }

    /// <summary>seeds 1..N をバッチ実行し集計（破綻率・決着・勝者分布・デッドロック・合否・常勝/即死フラグ）。</summary>
    public static BatchSummary RunBatch(int n, int quarters = Coefficients.GameQuarters, double? interestRate = null, int? parallel = null, ulong seedBase = 1)   // ★seedBase（2026-08-14）＝ノイズ床の測定（同一ソースでシード群を振り直す）。既定1＝seeds 1..n＝厳密恒等
    {
        // 各シードは完全独立（自前の MultiNationSim・RNG・共有可変状態なし）＝シード並列で安全に高速化。
        // 結果はシード番号で添字付けして集める＝並列でも順序・集計は決定論的（各シードの結果は他シードに非依存）。
        var arr = new MatchResult[n];
        // WorldData の遅延初期化（LoadDoc/Nations）は lock も Lazy<T> も無い check-then-act。生成値はどの経路でも同一なので無害だが、
        // 並列度を上げるほど窓が広がるため Parallel.For の外で一度触って確定させる（プリウォーム＝ビット恒等）。
        _ = WorldData.Nations();
        // 並列度：2026-07-27 実測（Ryzen 7 5825U＝物理8/論理16・RAM15.3GB）。1シード当たり par=6:91.0秒 → 8:76.6 → 12:66.8 → **16:61.3秒（1.48倍）**。
        // 物理8コアでも SMT が素直に伸びる（batch30 換算 48.4分→約33分）。旧既定 clamp(コア/2,2,6) の上限6は 2026-07-03 の Telemetry 常時ON時代の
        // メモリ見積りに由来し、2026-07-09 の常時OFF化で前提が失効していた。
        // ★GC（2026-09-29 高速化の段 S1・旧注記「GC設定は実測で効果なし〜逆効果（2026-07-27）」を訂正）：今の木ではワークステーション GC の1つのヒープを
        //   10試合が分け合い、GC の停止が全長のバッチの壁の34%だった（毎秒約11回・その度に全ての試合が止まる）。サーバー GC（DATAS 切）で 17分16秒→11分19秒・
        //   報告は全行バイト一致（実装ログ (111)）＝Runner の csproj で入れた（App と Tests は変えない）。
        // n でクランプするのは 30 シードに 32 ワーカーが純粋な遊休になるため。OOM は握り潰さず即 throw する（下の catch 参照）＝静かな帯汚染を防ぐ。
        var opts = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = parallel ?? Math.Clamp(Environment.ProcessorCount, 2, Math.Max(2, n)) };
        System.Threading.Tasks.Parallel.For(0, n, opts, i => arr[i] = Run(seedBase + (ulong)i, quarters, interestRate, lean: true));   // ★seedBase 起点（既定1＝旧 i+1 と同値）   // lean＝外交ログ停止（開戦数はカウンタ・イベント年表はバッチでは未使用）
        var results = new List<MatchResult>(arr);

        int breakdowns = results.Count(r => !r.Completed);
        var completed = results.Where(r => r.Completed).ToList();
        double avgDec = completed.Count > 0 ? completed.Average(r => r.DecisionQuarter) : 0;
        var winCounts = completed.GroupBy(r => r.Winner).Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2).ToList();
        int deadlocks = results.Count(r => r.Deadlocked);   // ★ゲート3是正2：文字列一致をやめる（文言変更で硬性ゲートが恒真になる罠）
        int late = completed.Count(r => r.DecisionQuarter > quarters * LateDecisionQuarterFraction);   // Round2：異常文字列の走査でなく DecisionQuarter から直接算出（情報指標として存続）
        int totalAnom = results.Sum(r => r.Anomalies.Count);
        // ★ゲート3是正1（2026-08-01）：**破綻した試合をハーネス不合格に算入する**。
        // ⚠旧＝例外パスが HarnessPass/Warn/Fail=0・HarnessIssues=[] を返すため、`HarnessFail > 0` に引っかからなかった＝
        //   **破綻が増えるほどハーネスが緑に近づく**（最終調整ハーネス設計 §1-3）。完走しなかった試合は定義上どの検査にも合格していない。
        int harnessFail = results.Count(r => !r.Completed || r.HarnessFail > 0);
        int lossFin = results.Count(r => r.WorstLossRate > LossFinancedRateThreshold);

        var flags = new List<string>();
        if (breakdowns > 0) flags.Add($"破綻：{breakdowns}/{n} が未完走（実装バグの疑い）");
        foreach (var r in results.Where(x => !x.Completed))
            flags.Add($"  └ seed={r.Seed}: {(r.Anomalies.Count > 0 ? r.Anomalies[^1] : "不明")}");
        foreach (var (nation, wins) in winCounts)
            if (wins > n * 0.5) flags.Add($"常勝の疑い：{nation} が {wins}/{n} 勝");
        foreach (var g in completed.GroupBy(r => r.Standings[^1].Nation))   // 国名で集計（他17）
            if (g.Count() > n * 0.5) flags.Add($"即死/低迷の疑い：{g.Key} が {g.Count()}/{n} で最下位");
        // Round2 2026-07-08：「決着しない疑い」フラグを撤去（終盤まで接戦＝目標③の達成。デッドロックは frozen 検査が別途検出）。決着遅延の数は表示指標として存続。
        if (lossFin > 0)
        {
            var wl = results.OrderByDescending(r => r.WorstLossRate).First();   // ★出所も一緒に出す（2026-08-11）＝再走なしで「本物の慢性赤字」か「産出が消えた残余国」かを見分ける
            flags.Add($"損失financing率が高い：{lossFin}/{n} 試合（ピーク率の最大 {wl.WorstLossRate:P1}＝seed{wl.Seed} {wl.WorstLossNation} Q{wl.WorstLossQuarter}・その四半期の名目産出 {wl.WorstLossGdpQ:F0}）");
        }
        // ハーネスの検査名をバッチ出力へ（2026-07-31）：MatchResult は HarnessIssues を持っているのに集計で捨てていたため、
        // 「不合格2/10」は分かっても**どの検査が落ちたか**が分からず、そのたびに単走（selfplay <seed>）を回し直していた。検査名で畳んで seed と実測値を並べる。
        // ⚠**要注意フラグ（硬性ゲートの0件判定）には入れない**＝△(注意)は元から出ていた情報で、フラグに混ぜるとゲートの意味が変わる。専用の欄で出す。
        var harnessLines = results.SelectMany(r => r.HarnessIssues.Select(i => (r.Seed, Issue: i)))
            .GroupBy(x => x.Issue.Split('：')[0])
            .OrderByDescending(x => x.Count())
            .Select(g => $"{g.Key}：{g.Count()}/{n}　" + string.Join("　", g.Select(x => $"seed{x.Seed}={x.Issue[(x.Issue.IndexOf('：') + 1)..]}")))
            .ToList();

        // ★A-05 跳躍ダイジェスト（2026-09-03）：1914断面の一致率が原理的に見ない「途中で起きた跳躍」を種類で畳んで出す。
        //   ⚠報告専用＝要注意フラグにも硬性ゲートにも入れない（△と同じ扱い＝ゲートの意味を変えない）。
        var jumpLines = results.SelectMany(r => (r.Jumps ?? []).Select(j => (r.Seed, Jump: j)))
            .GroupBy(x => x.Jump.Contains("順位") ? "順位の跳躍" : x.Jump.Contains("政変") ? "10年内に政変3回" : "人口の物理上限超")
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}：{g.Count()}件　" + string.Join("　", g.Take(6).Select(x => $"seed{x.Seed} {x.Jump}")))
            .ToList();

        // 恐慌統計（要望3）：経済の波の基線。
        double avgDrawdown = completed.Count > 0 ? completed.Average(r => r.MaxDrawdown) : 0;
        int greatDep = completed.Count(r => r.MaxDrawdown > GreatDepressionDrawdown);   // ピーク比25%超の収縮＝大恐慌
        double avgDepEp = completed.Count > 0 ? completed.Average(r => r.DepressionEpisodes) : 0;
        int totalDep = completed.Sum(r => r.DepressionEpisodes);
        int totalOverheat = completed.Sum(r => r.OverheatDepressions);
        int totalHarvest = completed.Sum(r => r.HarvestDepressions);
        double overheatShare = totalDep > 0 ? (double)totalOverheat / totalDep : 0;   // 恐慌のうち信用過熱起因の割合（＝金融政策で避けられた類型）
        double harvestShare = totalDep > 0 ? (double)totalHarvest / totalDep : 0;      // 不作起因の割合（＝食糧安保・福祉で緩衝する類型）

        // 最終政体の分布（全国×全seed）。要望4：希少政体でも出現するか（10プレイに1回は1国）を可視化。
        var polityDist = completed.SelectMany(r => r.Standings).GroupBy(s => s.Polity)
            .Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList();
        // 各政体が「1国以上で出現したプレイ数」（要望4の直接指標：N/総プレイ）。
        var polityPlays = completed.SelectMany(r => r.Standings.Select(s => s.Polity).Distinct())
            .GroupBy(p => p).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList();

        // 首位交代（目標3）：平均交代回数。多いほど独走がなくレースが動いている。
        double avgLeadChanges = completed.Count > 0 ? completed.Average(r => r.LeadChanges) : 0;
        // 法の多様性（目標5）：全国×全seed の最終法を軸ごとに分布集計。最頻値シェア(MaxShare)が低いほど多様＝国と乱数で法が分岐している。
        var lawDiv = new List<(string Axis, double MaxShare, string Dist)>();
        var lawSamples = completed.SelectMany(r => r.Standings).ToList();
        if (lawSamples.Count > 0)
        {
            void Axis<T>(string name, Func<NationStanding, T> pick) where T : notnull
            {
                var groups = lawSamples.GroupBy(pick).Select(g => (Val: g.Key.ToString() ?? "", Cnt: g.Count()))
                    .OrderByDescending(x => x.Cnt).ToList();
                double maxShare = (double)groups[0].Cnt / lawSamples.Count;
                string dist = string.Join(" ", groups.Select(g => $"{g.Val}{(double)g.Cnt / lawSamples.Count:P0}"));
                lawDiv.Add((name, maxShare, dist));
            }
            Axis("参政権", s => s.Suffrage);
            Axis("教育法", s => s.Education);
            Axis("報道", s => s.Censorship);
            Axis("労組", s => s.Union);
            Axis("労働時間", s => s.WorkHours);
            Axis("穀物法", s => s.CornLaws);
            Axis("土地改革", s => s.LandReform);
            Axis("農奴解放", s => s.Serf);
            Axis("年金", s => s.Pension);
            Axis("徴兵法", s => s.Conscription);   // ★2026-08-18：旧「親軍」＝法でなくなったので多様性の軸から外し、軍国度の唯一の法である徴兵法へ差し替え
        }

        // 史実忠実度（新較正基準2026-07-09）：1914参照表（HistoricalReference.Table）と完走試合の最終政体・法を国別に照合。
        var fidelity = HistoricalReference.Evaluate(completed);
        // ★止める規則（2026-09-30 作者裁定「計算量の予算」の6）：種ごとの exact／near＝同じ照合（Evaluate）を1試合ずつ＝照合できた国の平均（NaN＝未定義の国は除く）。
        var seedFid = completed.Select(r =>
        {
            var f1 = HistoricalReference.Evaluate([r]);
            return (r.Seed, f1.Select(x => x.Exact).Where(v => !double.IsNaN(v)).DefaultIfEmpty(double.NaN).Average(),
                    f1.Select(x => x.Near).Where(v => !double.IsNaN(v)).DefaultIfEmpty(double.NaN).Average());
        }).ToList();

        // GoldCoverMin有効化2026-07-10：金本位の強制離脱（兌換停止）頻度＝「実在するが常態でない危機」の帯を確認する集計。
        int goldSusp = completed.Sum(r => r.GoldSuspensions);
        int goldCoverSusp = completed.Sum(r => r.GoldCoverSuspensions);
        int goldSuspMatches = completed.Count(r => r.GoldSuspensions > 0);

        // 国家アイデンティティ（2026-07-12）：分離独立の建国と変名（統一/formable）の発生総数＝状態由来（leanのログ抑制に非依存）。
        int independences = completed.Sum(r => r.SpawnedNations);
        int formations = completed.Sum(r => r.Formations);
        int civilSplits = completed.Sum(r => r.CivilWarSplits);

        return new BatchSummary(n, breakdowns, avgDec, winCounts, deadlocks, late, totalAnom, harnessFail, flags, polityDist, polityPlays,
            avgDrawdown, greatDep, avgDepEp, overheatShare, harvestShare, avgLeadChanges, lawDiv, fidelity, goldSusp, goldCoverSusp, goldSuspMatches,
            independences, formations, civilSplits, JumpLines: jumpLines, HarnessIssueLines: harnessLines,
            // ★診断2026-08-01：同一ペアの再戦。作者指摘「同じ国が繰り返し開戦するのは不具合」を数で捕まえる
            completed.Count > 0 ? completed.Average(r => r.WarMaxRepeat) : 0,
            completed.Count > 0 ? completed.Average(r => r.WarPairsGe3) : 0,
            completed.Count > 0 ? completed.Average(r => r.WarPairs) : 0,
            completed.OrderByDescending(r => r.WarMaxRepeat).FirstOrDefault() is { } wr ? $"{wr.WarWorstPair}（{wr.WarMaxRepeat}回・seed{wr.Seed}）" : "",
            completed.Count > 0 ? completed.Average(r => r.InterveneChances) : 0,
            completed.Count > 0 ? completed.Average(r => r.PeaceOffers) : 0,
            completed.Count > 0 ? completed.Average(r => r.PeaceBlank) : 0,
            completed.Count > 0 ? completed.Average(r => r.PeaceRefused) : 0,
            // ★海軍力の順位（1914断面・全seed平均）＝「陸軍は兵士ありき・海軍は戦艦ありき」への改修の物差し。BaseName（変名不変鍵）で束ねる。
            completed.SelectMany(r => r.StandingsAt1914 ?? r.Standings)
                .GroupBy(s2 => string.IsNullOrEmpty(s2.Nation) ? s2.Name : s2.Nation)
                .Select(g => (Nation: g.Key, Naval: g.Average(x => x.NavalPower), Land: g.Average(x => x.FleetStock)))
                .OrderByDescending(x => x.Naval).ToList(),   // ⚠全国を保持する（艦隊ストック順の並べ替えを呼び手が行うため。海軍力上位8だけに絞ると英が消える）
            // ★E0計器（選挙doc §8-a）：総選挙の回数と平均間隔。間隔＝Σ在籍四半期÷Σ回数（回数0は0扱い）
            completed.Count > 0 ? completed.Average(r => r.ElectionsHeld) : 0,
            completed.Sum(r => (long)r.ElectionsHeld) > 0 ? completed.Sum(r => (double)r.ElectionQuarters) / completed.Sum(r => (double)r.ElectionsHeld) : 0,
            completed.Count > 0 ? completed.Average(r => r.TradeGdpMajors) : 0,   // ★貿易復活計器
            // ★史実基準の恐慌計器（2026-08-22）：大国8の−20%級エピソード（試合平均）と最大DD（全seed最大＝最悪ケースの物差し）
            completed.Count > 0 ? completed.Average(r => r.MajorDepressionEpisodes) : 0,
            completed.Count > 0 ? completed.Max(r => r.MajorMaxDrawdown) : 0,
            completed.OrderByDescending(r => r.MajorMaxDrawdown).FirstOrDefault() is { } mdd ? $"{mdd.MajorDdNote}・seed{mdd.Seed}" : "",
            CostTotals: SumCost(results),   // ★計算量の台帳（2026-09-30）＝全試合の和（報告専用）
            SeedFidelity: seedFid);         // ★止める規則（2026-09-30 第2回）＝種ごとの exact／near（報告専用）
    }

    /// <summary>計算量の台帳の全試合の和（<see cref="CostLedger"/> の添字・中断した試合もそこまでの件数を足す）。</summary>
    private static long[] SumCost(IEnumerable<MatchResult> results)
    {
        var t = new long[CostLedger.Count];
        foreach (var r in results) CostLedger.AddInto(t, r.Cost);
        return t;
    }
}
