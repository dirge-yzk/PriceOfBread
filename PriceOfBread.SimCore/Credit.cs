namespace PriceOfBread.SimCore;

/// <summary>
/// 信用・貨幣（§2.5.1／§9-G）。好況なら銀行が与信を広げ広義マネーを増やし（ブーム）、
/// ショックで信用意欲が崩れると与信を回収しマネーが縮む（バスト/恐慌・回復経路あり §2.7）。
/// 与信の増減（信用創造/返済）を全 pop 預金へ注入/回収する。基礎貨幣＝預金＋国庫−与信 は保存。
/// </summary>
public static class Credit
{
    private const double HeatClampMax = 1.5;   // 与信使用率（過熱度）の上限クランプ
    private const double HoardRatioMax = 0.9;  // タンス預金率（退蔵）の上限クランプ
    private const double GoldCoverCreditFloor = 0.25; // カバー率防衛時の与信下限比率（全面凍結はしない）

    /// <param name="month">★月の中の定数（2026-09-26 A2-1）＝名目GDP・大衆の平均満足・平均工業化。日次の呼び手（<see cref="Simulation.StepDayDaily"/>）が
    /// 月の初日に1回だけ求めて渡す（結果が変わらない根拠は <see cref="Simulation.CreditMonth"/>）。null＝ここで求める（テストの直呼び＝旧と同じ）。</param>
    public static void Step(World w, Coefficients c, (double NominalGdp, double AvgSat, double MeanInd)? month = null)
    {
        var bank = w.Bank;
        if (bank is null) return;

        // #5 準備の導出：bank.Gold を凍結値（初期TotalMoney＋交易netFlow）でなく**基礎貨幣 BaseMoney から毎期導出**する。
        // BaseMoney は内部移転で不変・交易と鋳造(#10)でのみ動く保存量なので、ゴールドラッシュ→準備↑→信用上限↑（好況）／貿易赤字→正貨流出→準備↓→信用収縮（Hume）が単一ソースから創発。
        // 旧来の Trade.cs での bank.Gold += netFlow（交易の二重計上）は撤去済み。経済成長で名目額が凍結し maxLoans が実態を反映しない問題も解消。
        // 流通現金（経6・no-currency-in-circulation）：基礎貨幣の全額が銀行準備＝流通現金ゼロという現行の暗黙前提を係数化。
        // >0 で準備＝BaseMoney×(1−CurrencyHoldingRatio)＝家計・企業の手元現金退蔵分を準備から除く（恐慌時の現金退蔵→準備↓→信用収縮の内部流出チャネルの素地）。
        // CurrencyHoldingRatio=0 なら乗数1＝bank.Gold=Max(0,BaseMoney)（旧）。既定0.1。
        // タンス預金の動学（07_再設計 §3・A3裁定・2026-07-31）：退蔵率は固定でなく**銀行の信用の関数**。
        // 信用が崩れるほど手元に引き上げる＝準備が縮む＝与信上限が縮む＝さらに信用が崩れる（自己強化）。
        // ★Bank.Gold は BaseMoney からの導出のまま（ratio_eff が状態関数なら二重ソースは生じない＝07 §3の再結論）。
        // 保存則＝退蔵は BaseMoney の内訳が変わるだけで量は不変＝安全。HoardPanicWeight=0 で静的（旧）。
        double hoardRatio = Math.Clamp(c.CurrencyHoldingRatio * (1.0 + c.HoardPanicWeight * (1.0 - bank.Confidence)), 0, HoardRatioMax);
        bank.Gold = Math.Max(0, w.BaseMoney) * (1.0 - hoardRatio);

        // 割当削減（測定短縮① 2026-07-09）：日次×全国で呼ばれるホットパス＝毎回の List<Pop> 構築を廃止。全popリストは投資家不在の小世界などコールドパスのみ遅延構築。
        // ★2026-09-25：平均満足は唯一の実装 NationalScore.MeanSatisfaction を呼ぶ（旧＝同じ式の手書きの複製で、全POP・Seceded の州まで数えていた）
        //   ＝下の注記どおり「大衆の充足」（Politics.IsMassPop の母集団）を見る。Seceded の差は seed1-3×668Q で分離州の人口0＝実害なし・所要時間も同等。
        double avgSat = month?.AvgSat ?? NationalScore.MeanSatisfaction(w);
        List<Pop>? popsLazy = null;
        List<Pop> Pops()
        {
            if (popsLazy is null)
            {
                popsLazy = [];
                foreach (var r0 in w.Regions) popsLazy.AddRange(r0.Pops);
            }
            return popsLazy;
        }
        // ミンスキー動学：平穏（充足が pivot 以上）が**続く限り**信頼は満枠1.0へゆっくり漸増する＝安定が過信を育てる。
        // 充足が pivot を割ると急峻に崩れ（感応度×不足幅）、しかも崩れる速さは育つ速さの10倍＝パニックは非対称。
        // ※旧式（充足の線形写像が目標）は信頼が0.3で頭打ち＝ブームが構造的に起きず「信用過熱先行の恐慌0%」を観測した。
        //   好況の長さ自体が脆さを蓄える（与信の山）→ 不作・戦争の実体ショックが来た時、山の高さが恐慌の深さになる。
        // 銀行は大衆の充足（★階層3分割の後は日用品の充足＝消費の冷え。食料の欠乏は闘争度の側）だけでなく**経済の収縮そのもの**（トレンドGDPのピーク比）も見る：
        // 高レバレッジで小さな後退が来ると与信回収→投資停止→さらに収縮、の自己強化＝山の高さが恐慌の深さ。
        // 財政破綻（連合を養えない＝事実上の債務不履行）は信用を直接崩す＝経済悪化の段階（外科的な政体フリップでなく、
        // 信用収縮→恐慌→不満→政治の通常経路で帰結を出す）。FiscalEnabled 時のみ＝単一国較正セルは不発。
        double fiscalDefault = w.FiscalEnabled ? c.FiscalDefaultPanic * Math.Max(0, 1.0 - w.FiscalFundingRatio) : 0;
        // 民間負債デフレ（B-3 フィッシャー）：デフレ×レバレッジ(前期Heat)＝実質債務増→債務者困窮→信頼↓→与信回収→さらにデフレの渦。MonetaryEnabled時のみ（デフレ率が定義される）。
        double debtDeflation = w.MonetaryEnabled ? c.DebtDeflationWeight * Math.Max(0, -w.ExpectedInflation) * bank.Heat : 0;
        // 国債二次流通の評価損（no-bond-secondary-market）：BondPrice下落(利回り上昇)で国内債を抱える銀行が評価損→信頼↓→与信収縮（平時のマーク・トゥ・マーケット）。既存診断値BondPriceを配線。BondLossConfWeight=0で恒等（既定0.5）。
        // [単位・B13] 債務/GDP＝**月次名目**（GovDebt÷月次NominalGdp・Clamp 0..1）＝年次名目の約12倍の値で飽和が早い
        double bondLoss = w.FiscalEnabled ? c.BondLossConfWeight * Math.Max(0, 1.0 - w.BondPrice) * PoliticalQuantities.Clamp01(w.GovDebt / Math.Max(1.0, month?.NominalGdp ?? w.NominalGdp)) : 0;
        // シティ型恐慌（A・最終調整2026-06-29）：与信過熱(Heat・前期＝持ち越したレバレッジ)が閾値を超えると、満足度が高くても信頼が脆くなる＝
        // 最も金融化した経済（深い与信＝高Heat＝覇権の金融中心）ほど好況の絶頂でミンスキー暴落を起こす。着火後は既存 DrawdownPanic が暴落へ増幅。OverheatFragility=0で恒等（既定1.1）。
        double overheatFragility = c.OverheatFragility * Math.Max(0, bank.Heat - c.OverheatFragilityThreshold);
        double targetConf = PoliticalQuantities.Clamp01(1.0 - c.PanicSensitivity * Math.Max(0, c.ConfidencePivot - avgSat) - c.DrawdownPanic * w.GdpDrawdown - fiscalDefault - debtDeflation - bondLoss - overheatFragility);
        // 信用は日次（三層時計）。速度を日数配分（÷PriceFlowDailyDiv）＝30日積分が従来の月次1回にほぼ一致＝較正温存。
        // パニック/平時の非対称（速い崩落・遅い醸成）も貸し剥がし倍率も比なので、一律に割っても保たれる。
        double div = c.PriceFlowDailyDiv;
        double spd = (targetConf < bank.Confidence ? c.PanicSpeed : c.ConfidenceSpeed) / div;
        bank.Confidence = PoliticalQuantities.Clamp01(bank.Confidence + spd * (targetConf - bank.Confidence));

        // 金融政策：低金利ほど銀行は貸す（信用ブーム→投資加速）、高金利は引締（安定）。＝中央銀行のレバー
        double appetite = PoliticalQuantities.Clamp01(1.0 - c.InterestRateEffect * w.InterestRate);
        // R12（信用乗数の発展度スケール化・2026-07-13）：全国全時代一律3.0を「発展度（工業化）」でスケール＝1789の露清オスマンに
        // 英規模の近代銀行はない＝浅い銀行（正貨中心）から工業化とともに近代信用創造へ深化（金融深化125年）。devFactor=1で×1.0＝厳密恒等（Weight=0）。
        double devFactor = 1.0;
        if (c.CreditMultiplierDevWeight > 0)
        {
            double dev = PoliticalQuantities.Clamp01((month?.MeanInd ?? w.MeanIndustrialization) / c.CreditDevScale);           // 0=前工業（露清）〜1=工業化（英独）
            double target = c.CreditMultiplierDevFloor + (1 - c.CreditMultiplierDevFloor) * dev;  // floor（前工業でも手形/両替商の最低与信）〜1（満額の近代信用創造）
            devFactor = 1.0 + (target - 1.0) * c.CreditMultiplierDevWeight;                       // Weight=0で1.0（恒等）・1でtargetそのもの
        }
        double maxLoans = bank.Gold * (c.CreditMultiplier - 1) * devFactor;
        // #125 正貨準備の動学（2026-07-25）：金本位国のカバー率防衛＝coverage(BaseMoney/BroadMoney)が床(GoldCoverMin×(1+これ))へ迫ると
        // 与信上限を比例で絞る（1890年代のバンクレート防衛の与信面＝兌換を守るため信用を犠牲にする）。与信↓→BroadMoney↓→coverage回復＝負帰還
        //（現行は準備が薄くても与信が伸び続け、割った瞬間に強制離脱＝カバー率割れ316回の構造）。床0.25＝信用の全面凍結はしない。GoldCoverCreditRestraint=0で不読＝厳密恒等（既定0.5）。
        // ★銀の段の軽い規律（SilverLightDiscipline・2026-08-21・**live=1**）：発券カバー率防衛の与信抑制も金の段の制度（19-10差し戻しの根②＝
        //   点火時に20〜31カ国で binding）。銀の段では絞らない。0に戻せば旧来（銀の段も絞る）と厳密恒等。
        if (c.GoldCoverCreditRestraint > 0 && w.OnGoldStandard && w.BroadMoney > 1e-9 && (c.SilverLightDiscipline <= 0 || w.GoldStandardLaw))
        {
            double gcCoverage = w.BaseMoney / w.BroadMoney;
            double gcFloor = c.GoldCoverMin * (1.0 + c.GoldCoverCreditRestraint);
            if (gcFloor > 1e-9 && gcCoverage < gcFloor) maxLoans *= System.Math.Max(GoldCoverCreditFloor, gcCoverage / gcFloor);
        }
        // ★借り手の返せる額（作者裁定 A16・E6 の1歩目・2026-09-28）：与信の上限＝「準備×信用乗数」と「借り手が返せる額」の小さい方。
        //   旧＝上限は準備の側だけ＝借り手の所得と無関係に準備×乗数まで貸した（与信÷投資家の年間所得＝英1813〜23 で55〜66倍・世界の和で1883年に約200倍＝r81 の計器）。
        //   そこへ金が流出すると、与信が国内の貨幣の全部（貯蓄＋国庫）を超えて基礎貨幣が負になる（(79) の英 1853 −1.8M）。
        //   ⚠回収は基礎貨幣を動かさない（投資家の預金と与信が同じ額だけ減る）＝負へ落ちた国は回収では戻らない（戻すのは交易・鋳造・発行＝E6 の2歩目の償却）。
        //   この上限は「与信で負へ落ちる深さ」を返せる額までに押さえるだけ。⚠損失の負債化（Simulation.BackstopNegativeSavings）はこの上限の外で残高を積む。
        //   返せる額＝首都の投資家（与信の借り手＝下の枝と同じ）の前月の所得×12×SoeSaleYears（国営工場の払い下げ価格と同じ「年間所得×年数」の物差し）。
        //   ⚠NaN は比較が偽＝準備の上限のまま（否定形）。投資家のいない小シナリオ（全POPへ注入する枝）は準備の上限だけ（従来と同じ）。
        //   最後の貸し手は準備を広げるだけ＝返せる額は越えない（下の effMaxLoans でも同じ比較）。過熱度の分母（maxLoans）も返せる額を見る＝所得が落ちると過熱度が上がる（債務÷所得）。
        var home = w.Region;
        double repayable = double.PositiveInfinity;
        if (home.InvestorPopIndex >= 0 && home.InvestorPopIndex < home.Pops.Count)
            repayable = Math.Max(0, home.Pops[home.InvestorPopIndex].LastEarned) * Calendar.MonthsPerYear * c.SoeSaleYears;
        if (repayable < maxLoans) maxLoans = repayable;
        // 銀行取り付け（B-3）：預金者のパニック（充足が平穏線を割る）で正貨が逃避＝実効準備が縮む＝willingな銀行でも貸せない（貸し手意欲Confidenceと別の流動性チャネル）。
        double run = Math.Clamp(c.BankRunSensitivity * Math.Max(0, c.ConfidencePivot - avgSat), 0, c.BankRunMax);
        // 最後の貸し手（B-3 Bagehot）：危機（過熱 or 後退）かつ財政が健全（solvent）なら国庫/中銀が流動性を供給し取り付けを和らげる＝恐慌の底打ち（根治はしない）。AIの安全弁。
        bool crisis = bank.Heat > c.LolrHeatTrigger || w.GdpDrawdown > c.LolrDrawdownTrigger;
        if (crisis && w.FiscalFundingRatio > c.LolrMinFunding) run *= 1.0 - c.LolrStrength;
        // LOLR準備拡張（no-lolr-balance-sheet）：危機×財政健全でmaxLoansを一時拡張＝バジョット原則「平時に蓄えた準備を危機に高利で貸す」。信用創造はBaseMoney中立(広義のみ膨張)ゆえInvariants変更不要。LolrReserveBoost=0で恒等（既定0.5）。
        double lolrBoost = crisis && w.FiscalFundingRatio > c.LolrMinFunding ? c.LolrReserveBoost : 0;
        double effMaxLoans = maxLoans * (1.0 - run) * (1.0 + lolrBoost);
        if (repayable < effMaxLoans) effMaxLoans = repayable;   // ★A16：最後の貸し手の準備拡張でも返せる額は越えない
        // Heat符号逆転の是正（経8）：Heat=Loans/実効上限 は取り付けで分母（準備）が縮むと跳ね上がる。
        // この Heat は World.FinancialHeat 経由で株式ユーフォリア項(EquityHeatEuphoria・既定有効)へ配線され、投資ハードルから減じられる＝
        // 「取り付けの最中に投資が刺激される」符号逆転を生む。安定準備(取り付け前 maxLoans＝run/lolr 反映前)基準の使用率なら、パニックで縮む分母に振り回されない。
        // EuphoriaUsesStableReserve=0 で旧・縮小後 effMaxLoans 基準。既定1.0＝安定準備基準（#17較正で有効化）。
        double heatDenom = effMaxLoans * (1.0 - c.EuphoriaUsesStableReserve) + maxLoans * c.EuphoriaUsesStableReserve;
        bank.Heat = heatDenom > 1e-9 ? Math.Clamp(bank.Loans / heatDenom, 0, HeatClampMax) : 0;   // 与信使用率＝過熱度（既定は取り付けで準備が縮むと過熱度↑・stable基準では準備縮小に不感）
        double targetLoans = effMaxLoans * bank.Confidence * appetite;
        // 説明API（金利doc B2・2026-07-30）：与信目標の加法分解＝「金利を動かしたとき何がどれだけ動いたか」を測れる状態にする。
        // 乗算連鎖 maxLoans×(1−run)×(1+lolr)×Confidence×appetite を before/after 差分で分解（AdminEfficiency と同型）。
        // Explain=null（既定）なら丸ごと短絡＝ゼロコスト・ビット恒等。
        if (w.Explain is { } exCredit)
        {
            exCredit.Reset("CreditTarget");
            exCredit.Add("CreditTarget", "与信の上限（準備×信用乗数と返せる額の小さい方）", maxLoans);
            exCredit.Add("CreditTarget", "銀行取り付けと最後の貸し手", effMaxLoans - maxLoans);
            exCredit.Add("CreditTarget", "銀行の信頼（ミンスキー）", effMaxLoans * bank.Confidence - effMaxLoans);
            exCredit.Add("CreditTarget", "政策金利（貸出意欲）", targetLoans - effMaxLoans * bank.Confidence);
        }
        // 貸し剥がしの非対称（倍率排除）：独立した RecallMultiple 倍率を撤去し、非対称は**信頼の崩落速度**（PanicSpeed≫ConfidenceSpeed）に一本化。
        // Confidence が一夜で崩れると targetLoans=maxLoans×Confidence×appetite も急落するため、対称な CreditAdjustSpeed でも回収が速くなる＝「剥がしは一夜」は信頼の非対称から創発。
        double adjSpd = c.CreditAdjustSpeed / div;
        double delta = adjSpd * (targetLoans - bank.Loans);
        if (bank.Loans + delta < 0) delta = -bank.Loans;

        // 与信は**投資家（資本家）に向ける**＝銀行が産業を金融する：低金利/好況→ブルジョワが借りて投資→工業化ブーム、
        // 引締/恐慌→与信回収→投資縮小。投資家がいない小シナリオは全 pop へ（従来挙動＝テスト安全）。
        // 地方与信（CreditSpreadAcrossProvinces＝全州の投資家へ貯蓄比例配分）は2026-07-09の実測でテスト赤化＝不採用確定、
        // 2026-07-12のユーザー裁定で機構ごと廃止（恒久デッドコードだったelse枝と係数を削除）。与信は首都州の投資家のみ＝ロンドン一強の金融面は覇権抑制の裁定（92%受容）に従う。
        // （需要駆動の与信の器 CreditDemandDriven は作者裁定 A16 で点火せず削除＝不足分は投資家の貯蓄の5%−貯蓄＝常に0で、点火すると回収だけが働いた。
        //   後半の「残高≤返せる額」は上の与信の上限へ入れた）
        // 与信の増減を書くのは下の1行だけ（2026-09-28 E6：投資家の枝と全POPへ注入する枝の同じ行を1本へ＝お金の書き手を増やさない側の整理・ビット恒等）。
        double lent = delta;
        if (home.InvestorPopIndex >= 0 && home.InvestorPopIndex < home.Pops.Count)
        {
            var inv = home.Pops[home.InvestorPopIndex];
            lent = Math.Max(delta, -Math.Max(0, inv.Savings));   // 回収は投資家の貯蓄まで（負債で保存）
            inv.Savings += lent;
        }
        else InjectMoney(Pops(), delta);
        bank.Loans += lent;

        // 民間貸出の金利キャッシュフロー（no-loan-interest-cashflow）：借り手(投資家)が利息を払い、銀行利潤として預金者(全pop)へ分配＝pop→pop純移転でBaseMoney保存。
        // 既定 LoanSpread=0・PolicyRateLoanWeight=0 で loanRate=0＝利息なし＝現行恒等。引締め(InterestRate↑)で借り手の利払い増＝金融政策の所得分配チャネル（資本家が借金で建て利潤から利息を払う）。
        // ── 政策レバー→実金利の換算（経14・単位の集約・唯一の換算地点）──
        //   w.InterestRate  … 0..1 の**無次元**政策レバー（中央銀行のバンクレートの強さ。金利そのものの数値ではない）。
        //   c.LoanSpread    … **年率**の貸出スプレッド（例0.05＝年5%）。BondBaseRate(年率)と同次元で定義する。
        //   loanRate        … 年率の実効貸出金利＝スプレッド＋(無次元レバー×年率換算重み PolicyRateLoanWeight)。
        //   /Calendar.DaysPerYear … 年率を三層時計の1ステップ（日次）へ落とす除算。Step は日次で呼ばれる（Simulation.cs の日次ループ）ので、
        //     1年＝360ステップぶんの積分がちょうど年率 loanRate になる。
        //   ⚠ 旧実装は /div（＝PriceFlowDailyDiv=30＝**1か月の日数**）で割っていた＝年率が**12倍**で着弾する未発見のバグ（金利doc B3・2026-07-30 是正）。
        //     div は「月次係数を日次へ配分する」ための除数（信頼・与信調整の速度に使うのは正しい）であって、年率→日次の換算には使えない。
        //     既定 LoanSpread=0・PolicyRateLoanWeight=0 ゆえ loanRate=0＝この節に入らない＝**是正時点ではビット恒等**。B4（点火）の前提として先に潰す。
        //   ※ 係数の単位注記は Coefficients.cs 側（LoanSpread/PolicyRateLoanWeight/PolicyRateBondWeight/BondBaseRate）に付す提案を notes に列挙。
        double loanRate = w.LoanRateAnnual;   // 換算はWorld側の唯一の地点へ集約（UIと同じ値を見る＝表示と挙動が乖離しない）。式・順序は従来と同一＝ビット恒等
        if (loanRate > 1e-12 && bank.Loans > 1e-9)
        {
            double interestDue = bank.Loans * loanRate / (double)Calendar.DaysPerYear;
            double paid;
            // B4 利息の支払い方（2026-07-31 裁定＝案A+案Bの合成）。
            // 【案A】当期の利潤（フロー）から払う。旧実装は min(利息, Savings) ＝**ストックで頭打ち**で、与信を工場に変えた
            //   借り手は手元現金が薄いためクランプが恒常的に binding し、不足分は**捨てられて**いた（＝儲からなければ借金がタダ）。
            //   利潤は Pop.Earned−Pop.Paid（どちらも実在するフロー量）で取れる。Savings の増分で近似する必要はない。
            //   ★単位：Earned/Paid は EconomyStep.Run で**月次**リセット、Credit.Step は**日次**（Simulation.StepDayDaily）。
            //   月次の利潤で日次の利息を頭打ちにすると1ヶ月で最大30回ぶん払えるので、日割り（/DaysPerMonth）で揃える。
            //   現金（Savings）も同時に上限＝持っていない金は払えない（両方の min）。
            if (home.InvestorPopIndex >= 0 && home.InvestorPopIndex < home.Pops.Count)
            {
                var borrower = home.Pops[home.InvestorPopIndex];
                double profitDaily = Math.Max(0, borrower.Earned - borrower.Paid) / (double)Calendar.DaysPerMonth;
                paid = Math.Min(interestDue, Math.Min(profitDaily, Math.Max(0, borrower.Savings)));
                borrower.Savings -= paid;
            }
            else
            {
                var pops = Pops();
                double tot = 0, profitTot = 0;
                foreach (var p in pops) { tot += Math.Max(0, p.Savings); profitTot += Math.Max(0, p.Earned - p.Paid); }
                paid = Math.Min(interestDue, Math.Min(profitTot / (double)Calendar.DaysPerMonth, tot));
                if (tot > 1e-9) foreach (var p in pops) p.Savings -= paid * (Math.Max(0, p.Savings) / tot);
            }
            // 【案B】払えなかった分は**消えない**＝残高へ繰り入れる（複利化）。
            // ★保存則：bank.Loans は MoneyConservation の恒等式（BaseMoney = baseline + TradeFlow + GoldMinted + FiatIssued）に
            //   入っていないので、繰入は貨幣を作らない＝保存に触れない。倒産の既存規律（「負債は帳消ししない・倒産は産出0のみで貨幣非接触」）と同性質。
            // ★雪だるまは既存機構が止める＝FactoryBankruptcyEnabled（点火済み）と、Loans 増→銀行信頼の低下→与信収縮（ミンスキー）の自己制限。
            // 既定 InterestCapitalization=0 で繰入なし＝案Aのみ（安全側）。1で A+B。
            double unpaidInterest = interestDue - paid;
            // ★是正2026-09-06（6-3 canary＝貨幣保存の違反 −1804/月で発覚）：旧注記「bank.Loans は保存恒等式に入っていない」は誤り。
            //   BaseMoney = TotalMoney + Treasury − OutstandingLoans なので、残高だけ増やすと BaseMoney が同額減る（貨幣を消す）。
            //   正しい会計＝**繰入は与信の創造**：銀行が不足分を借り手に貸し（Loans↑）、借り手がそれを預金者へ払う（TotalMoney↑）＝差し引き BaseMoney 不変。
            //   実装＝繰入額を「払った額」に足して預金者へ配る（借り手の現金は減らない＝借金が増える）。
            double capitalized = c.InterestCapitalization > 0 && unpaidInterest > 1e-12 ? c.InterestCapitalization * unpaidInterest : 0;
            if (capitalized > 0) { bank.Loans += capitalized; paid += capitalized; }
            if (paid > 1e-12)   // 銀行利潤を預金者(全pop)へ**預金額(Max(0,Savings))比例**で分配＝取った額（＋繰入＝新規与信）と厳密一致で保存（経10）
            {
                // 利潤は預金者への配当ゆえ Size(人頭) でなく預金額比例が正しい（人頭だと無貯蓄の貧農も同額/人＝資本家→大衆の意図せぬ恒常移転）。
                // 徴収側（上の tot）と同形の Math.Max(0, Savings) 重み。LoanSpread 既定0でdormant＝現行挙動に影響なし。
                var pops = Pops();
                var held = new List<double>(pops.Count); foreach (var p in pops) held.Add(Math.Max(0, p.Savings));
                World.Distribute(pops, paid, held);   // ⚠預金の合計≈0 なら配らない（旧と同じ＝戻り値を捨てる。受け手不在の保存の穴は段2-2）
            }
        }
    }

    private static void InjectMoney(List<Pop> pops, double delta)
    {
        if (Math.Abs(delta) < 1e-12 || pops.Count == 0) return;
        // 負貯蓄ガード（経20）：按分基準に負の貯蓄が混ざると share が負になり信用注入の符号が逆転する（負債pop から金を抜く）。
        // 利息の徴収/分配側と揃えて Math.Max(0, ·) で防御し、正の貯蓄だけを按分基準にする。
        double total = 0;
        foreach (var p in pops) total += Math.Max(0, p.Savings);
        if (total <= 1e-9) return;

        double done = 0;
        for (int i = 0; i < pops.Count - 1; i++)
        {
            double share = delta * (Math.Max(0, pops[i].Savings) / total);
            pops[i].Savings += share;
            done += share;
        }
        pops[^1].Savings += delta - done;
    }
}
