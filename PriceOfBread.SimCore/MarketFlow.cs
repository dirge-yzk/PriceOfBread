namespace PriceOfBread.SimCore;

/// <summary>
/// 国内の価格形成（三層時計の「鼓動」＝市場フロー）。月次決算（<see cref="EconomyStep"/>）が残した需給
/// （LastDemand／LastProduced＋LastImport／Stock）を読み、価格を clearing 目標へ**少しずつ**寄せる。
/// 速度は月次係数を <see cref="Coefficients.PriceFlowDailyDiv"/>（≒30日）で割った**1日あたり**の値で、
/// 30日ぶんの積分が従来の月次1回の更新にほぼ一致する＝較正を温存する（旬次になっても1日あたりの式は変えない）。
/// ★旬次（2026-09-26 A3・作者裁定「ゲームの高速化を優先し、多少の帯の悪化は受け入れる」）：呼び手（<see cref="Simulation.StepDayDaily"/>）は
///   旬の最終日（<see cref="Coefficients.PriceFlowStepDays"/>＝h 日ごと）にだけ呼び、h 日ぶんを1歩で進める。
///   根拠＝ここが読む入力（需給・在庫・在庫目標の倍率・物価水準・工業化・組合厚生・EffFirms）の書き手はすべて境界相
///   （月次決算・月末の清算・四半期の首と末）にあり、旬は月をまたがない＝旬の中では入力が定数で、同じ写像を h 回まわしているだけ。
///   ・内側（|t|&lt;<see cref="InnerT"/>）の財と役務＝閉じた式 P′＝SB_h(P0·(1＋y0)^h)。SB_h は SoftBand を h 回重ねた厳密な形。
///     財は刻みが時定数の1/10 を超えるとき h の約数の小刻みで同じ式を重ねる（2026-09-30 S3c・<see cref="SubStep"/>＝役務は需給差が価格に依らない＝1歩で厳密）。
///     ⚠ 1＋h·y の形は使わない（端では y≈−0.163＝h=10 で価格が負になる）。(1＋y)^h なら常に正。
///   ・帯の端・休眠の上乗せ・退化した帯（L≦Eps）・労働（初回は閉じた式を使わない）＝日次の写像を h 回まわし、
///     値がビットで止まったら残りを打ち切る（入力が定数なので以後も同じ値＝厳密に同じ結果）。局所変数だけで回す＝状態を持たない。
///     ⚠例外＝供給も在庫も無く参照より上に張り付いた財（2026-09-29 S3b・近似）は、1日の動きが <see cref="PinnedTol"/>（相対）を切ったら打ち切る。
///     ★その財が不動点に着いた旬からは、月の残りの旬を回さない（2026-09-30 計算量の予算 第2回・近似＝<see cref="Market.FlowAsleep"/>・月の最初の旬に作り直す）。
///   h=1 なら旧来の日次と1ビットも変わらない（眠りは呼び手が月の2旬目以降と言ったときだけ＝既定は使わない）。
/// 金の移動は伴わない（価格を置くだけ）＝保存則に無関係。クリアリング・所得決済は月次のまま。
/// </summary>
public static class MarketFlow
{
    private const double Eps = 1e-9;

    /// <summary>閉じた式を使う内側の境界（t＝ln(P/rp)/L・L はその向きの帯の対数）。財では参照の1/10〜10倍、賃金では0.55〜√(3(1+工業化))倍
    /// （財は既存の pricedist の床・天井の境と同じ）。安定の根拠＝平衡まわりの1日の縮み g≦0.008·amp、|t|&lt;0.5 ⇔ amp&lt;4.02
    /// ⇒ h=10 で hg≦0.32（行き過ぎの限界1・安定の限界2から遠い）。判定は歩の始めと、SB を掛ける前の予測 P0·(1＋y0)^h の両方で行う
    /// （予測は帯で小さくなる前の値＝安全側）。⚠ 否定形で判定する＝NaN は日次の経路へ落ち、NaN の伝わり方は旧来のまま。</summary>
    private const double InnerT = 0.5;

    /// <summary>★張り付いた市場の打ち切り（2026-09-29 高速化の段 S3b・近似）の相対の許し＝1日の動き |P′−P| がこの割合を切ったら残りの日を打ち切る。
    /// 供給も在庫も無い財の日次の写像は帯の不動点（参照の約49倍＝需給の飽和と SoftBand が決める）への強い縮小（1日で距離が約0.71倍＝S3a）
    /// ⇒ 打ち切った値の不動点からの距離 ≦ 動き×ρ/(1−ρ) ≈ 2.5×1e-8（相対）。月の境で需要が変わって不動点が動いた日は動きが大きく出て打ち切らない＝理由で起きる。</summary>
    private const double PinnedTol = 1e-8;

    /// <summary>★閉じた式の小刻み（2026-09-30 高速化の段 S3c・近似）の物差し＝1歩の刻み×1日の縮み g の上限（刻み≦時定数/10＝作者の局所の線の目安）。
    /// g＝平衡まわりの1日の縮み＝FlowAdjustSpeed·ε/div·amp（需給差の項の価格微分・在庫ギャップの項は価格に依らない）＝参照付近（amp 0.7）で時定数 約180日・
    /// 内側の端（|t|＝0.5・amp 4.02）で 約31日 ⇒ h=10 なら参照の約3.5倍（と1/3.5）の内側は1歩・約6.5倍までは 5日×2・その外（内側の端＝10倍まで）は 2日×5（<see cref="SubStep"/>）。</summary>
    private const double StepPerTau = 0.1;

    // ★旬次の計器（2026-09-26 A3）：POB_TIME=1 のときだけ、州ごとの局所の数を出口で足す（既定は足さない）。timeprobe が読む。
    //   財の歩＝この関数が動かした（死んでいない）財の数。閉じた式／日次に落ちた理由別の内訳の和は、財の歩の数に一致する。
    //   日次の回＝日次の写像を実際にまわした回数・省いた日＝ビットで止まって打ち切った残りの日数（日次に落ちた財×h＝回＋省いた日）。
    internal static long CntGoodSteps, CntClosedGoods, CntClosedService,
        CntDailyStartBand, CntDailyStartHigh, CntDailyStartService, CntDailyPredBand, CntDailyLabor, CntDailyMarkup, CntDailyHardBand, CntDailyNaN, CntDailyStep1,
        CntDailyDaysRun, CntDailyDaysSkipped;

    /// <param name="days">この1歩で進める日数 h（旬の長さ）。1＝旧来の日次。</param>
    /// <param name="cost">計算量の台帳（<see cref="CostLedger"/>・呼び手の国の台帳＝並列の日次相でも国ごと）。null＝数えない（テストの直呼び）。診断＝値を読まない。</param>
    /// <param name="monthStart">この歩が月の最初の旬か（既定 true＝眠りを使わない＝テストの直呼びと旧来の形）。false＝月の2旬目以降＝<see cref="Market.FlowAsleep"/> の立った財を回さない。</param>
    /// <remarks>★月の残りを眠る市場（2026-09-30 計算量の予算 第2回・近似・作者裁定「起きている物だけ回す」）：供給も在庫も無く参照より上に張り付いた財
    /// （下の pinned）が不動点に着いた旬（日次の写像が PinnedTol の打ち切りかビットで止まった）に <see cref="Market.FlowAsleep"/> を立て、月の残りの旬は回さない。
    /// 起こす理由＝月の境（月次決算・清算が需給・在庫・輸入を書き直す＝次の月の最初の旬は全ての財を回して印を作り直す）。タイマーではない
    /// （入力の書き手が境界相だけ＝月の中では変わらない・census）。眠っている間の値段は不動点から相対 約2.5e-8 の内（打ち切りの上界）＝月次決算と建設が読む値の誤差もこの内。
    /// ⚠印は作業領域＝セーブに載らない。四半期の境でセーブしたとき、次の歩は月の最初の旬＝印を作り直す（往復は一致）。月の途中でロードすると、その月の残りは起きたまま回る（差は上界の内）。</remarks>
    public static void StepDay(Region region, Coefficients coef, int days = 1, long[]? cost = null, bool monthStart = true)
    {
        int h = days;
        double div = coef.PriceFlowDailyDiv;
        double k = coef.SoftBandSharpness;
        // ★2026-09-03（キューA-09／出典 トリアージ 2-A 10-4c）：財側の SoftBand の境界対数は**係数だけで決まる定数**なので、
        //   財×日ごとに Math.Log を2回呼ぶ必要がない。ここで1回だけ計算して渡す＝同じ double を使うのでビット恒等。
        double lnCeilGoods = System.Math.Log(coef.PriceCeilFactor);
        double lnFloorGoods = -System.Math.Log(coef.PriceFloorFactor);
        // ★2026-09-08（10-4c の残り＝賃金側）：賃金の天井 effWageMax は coef と region.Industrialization だけで決まり、
        //   この foreach は市場（m.*）しか書かない＝**財ループ不変**（月次で動くが、StepDay 1回の中では定数）。
        //   よって天井/床の対数も財ごとに Math.Log を呼ぶ必要がない＝役務1財あたり2回→州1回へ。渡す double は同一＝ビット恒等。
        double effWageMax = coef.WageClampMax * (1 + coef.WageClampIndScale * region.Industrialization);
        double lnCeilWage = System.Math.Log(effWageMax);
        double lnFloorWage = -System.Math.Log(coef.WageClampMin);
        // 閉じた式が使えるのは、h>1 で、SoftBand の両側の帯が正のときだけ（L≦Eps の退化は日次の SoftBandLn が rp を返す枝＝日次へ落とす）。
        bool closedGoods = h > 1 && lnFloorGoods > Eps && lnCeilGoods > Eps;
        bool closedWage = h > 1 && lnFloorWage > Eps && lnCeilWage > Eps;
        // 組合の賃金交渉（no-monopsony-wage）：組織率(LaborWellbeing代理)で留保賃金の上昇速度を増幅＝賃上げに速く・賃下げに抗う。UnionWageRiseBoost=0.5 だが LaborWellbeing は UnionSatBonus=0（休眠）で実効0＝ReservationWageRiseのまま。下方(Fall)は触らず下方硬直温存。
        //   coef と region.LaborWellbeing（月次）だけで決まる＝財ループ不変＝州1回へ巻き上げ（同じ double＝ビット恒等）。
        double unionRise = coef.ReservationWageRise * (1.0 + coef.UnionWageRiseBoost * region.LaborWellbeing);
        long nSteps = 0, nClosedG = 0, nClosedS = 0, nStart = 0, nStartHi = 0, nStartSvc = 0, nPred = 0, nLabor = 0, nMarkup = 0, nHard = 0, nNaN = 0, nStep1 = 0, nRun = 0, nSkip = 0;
        // ★死んだ財を一覧で回さない（2026-09-30 作者裁定「計算量の予算」・恒等）：需要も産出も Eps 未満の財は下で continue する（動かさない）＝回さないだけ。
        //   一覧＝Region.EnsureLiveGoods（Goods の順のうち死んでいない財・LastDemand／LastProduced の書き込みで「死んだ財か」が変わると印が立ち作り直す）。
        //   書き手は全て境界相（月次決算・清算・四半期）とテストの直書き＝日次相の中で変わらない（census）。下の continue は残す（一覧の外の財は旧でも必ず continue した）。
        var live = region.EnsureLiveGoods();
        long nAsleep = 0;
        foreach (var good in live)
        {
            int gi = good.Id;
            var m = region.Markets[gi];
            if (!monthStart && m.FlowAsleep) { nAsleep++; continue; }   // ★月の残りを眠る市場（上の remarks・近似）＝回さない（値段・在庫目標はこの月の不動点のまま）
            m.FlowAsleep = false;
            double totalDemand = m.LastDemand;
            if (totalDemand < Eps && m.LastProduced < Eps) continue;   // まだ決算が無い／死んだ財は動かさない
            nSteps++;

            // 在庫目標・在庫ギャップ・供給は価格に依らない＝旬の中で定数（日次でも毎日同じ値を書いていた）＝1歩に1回だけ求める。
            m.TargetStock = System.Math.Max(coef.TargetStockFloor, coef.TargetStockBuffer * totalDemand) * region.StockpileFactor(gi, coef);
            // 在庫ギャップ＝自己有界な正規化差 (Target−Stock)/(Target+Stock)∈(−1,1)（役務の flow と同形）。手クランプ不要で過剰/不足が対称（倍率排除）。
            double gap = (m.TargetStock - m.Stock) / (m.TargetStock + m.Stock + Eps);
            // ★価格振動の根①（2026-09-07・器 PriceLawSupplyIncludesStock／2026-09-08 作者裁定「備蓄がある限り、それは生産できているものとみなす」）：
            //   旧式（今月の流入だけ）＝輸入の無い月は倉庫が満杯でも供給0と読まれ、需給差が+1へ飽和して価格が上がり続けた（硫黄の鋸歯の根）。
            //   新＝**需要までは備蓄が生産の代わりをする**＝supply = max(流入, min(流入+在庫, 需要))。上限が需要なので
            //     ・流入が需要を超える月は従来どおり流入がそのまま供給（豊作で価格が落ちる挙動は不変）
            //     ・流入が細っても在庫が需要を覆う月は供給=需要＝需給差0＝**上がりも下がりもしない**（欠乏でないので煽らない）
            //     ・在庫も足りない月だけ不足の分だけ上がる
            //   ⚠在庫の「過剰」を需給差へ足してはいけない＝在庫ギャップ項（上の gap）が既に同じ過剰を下押ししており**二重計上**になる。
            //     実際 max(0, Stock−Target) を足した版は供給が過大に見え、限界的な生産者が price<=mc で止まって機構の番人3件が赤になった（2026-09-08 差し戻し）。
            double flowIn = m.LastProduced + m.LastImport;
            double supply = System.Math.Max(flowIn, System.Math.Min(flowIn + m.Stock, totalDemand));   // 旗 PriceLawSupplyIncludesStock（2026-09-08 点火）は 2026-09-26 に焼き込み

            // 物価水準（名目のヴェール）：参照価格アンカー・床天井を rp×P でスケール＝賃金/財が一斉に騰落＝一般インフレ/デフレ。P≡1＝従来同値。
            double pl = region.PriceLevel;
            if (good.Tier == GoodTier.Service)
            {
                // 役務は在庫を持てない＝フローのみで価格形成。賃金は下方硬直（賃下げ方向は遅い＝持続的失業を生む）。
                // 需給差は自然有界な正規化差 (D−S)/(D+S)∈[−1,1]（段A。弾力性は役務＝賃金には掛けない＝賃金硬直を温存）。
                // ⚠ 需給差は価格に依らない＝旬の中で定数＝月の中に押し戻す力が無い（振動もしない）。
                double flow = (totalDemand - supply) / (totalDemand + supply + Eps);
                double adj = coef.FlowAdjustSpeed * flow / div;
                // 賃金/役務の床天井。0.3–3.0×(参照価格×物価水準)。財側の SoftBand が係数化済みなのに賃金だけ直書きだった＝係数化（値は同一で中立）。
                // 財と同じ対数飽和(SoftBand)＝実質賃金成長・国際賃金格差の圧縮を緩める（旗 WageClampSoftBand＝ハードクランプとの切替は 2026-09-26 に焼き込み）。
                // R2（賃金天井の工業化連動）：天井を固定WageClampMaxでなく工業化度でスケール＝未工業国は従来の3倍据置・工業国のみ実質賃金が伸びる（史実の産業革命の賃金上昇・国際格差の拡大）。WageClampIndScale=1.0（R2点火2026-07-15）。0でeffWageMax=WageClampMax＝旧恒等。床(Min)は据置（下方硬直の温存）。effWageMax とその対数は州1回に巻き上げ済み
                double wrp = good.RefPrice * pl;
                if (gi is Catalog.LaborId or Catalog.AdvLaborId or Catalog.FarmLaborId)
                {
                    // 下方硬直は固定倍率でなく留保賃金から創発：留保賃金は賃上げに速く・賃下げに遅く追従し、賃金はそれを下限に置く。
                    // 持続失業（超過供給）では目標がじわじわ下がり留保賃金も遅れて切り下がる＝名目賃金は下がりにくいが永久には張り付かない。
                    // ★旬次（A3）：労働（3財＝生きた財の約3%）は初回は閉じた式を使わない＝日次の写像を h 回（打ち切りつき）＝旬の終わりの賃金と
                    //   留保賃金（＝失業）は日次と一致する。Rise/Fall は1日あたりの定数（÷div しない）なので、閉じた式にするなら相ごとの形が要る（設計 §2）。
                    double P = m.Price, RW = m.ReservationWage;
                    int run = 0;
                    while (run < h)
                    {
                        run++;
                        double target = P * (1 + adj);   // 無拘束の目標賃金（需給が指す clearing）
                        double rw = RW;
                        if (!(rw > Eps)) rw = P;   // 否定形＝NaN も初期化側へ（Math.Max は NaN を除去しない）
                        double rwSpeed = target >= rw ? unionRise : coef.ReservationWageFall;
                        rw += (target - rw) * rwSpeed;
                        double p1 = SoftBandLn(System.Math.Max(target, rw), wrp, lnFloorWage, lnCeilWage, k);   // 賃金の床/天井＝WageClampMin/effWageMax（財の SoftBand と同じ対数空間飽和）
                        bool still = Same(p1, P) && Same(rw, RW);
                        P = p1; RW = rw;
                        if (still) break;
                    }
                    m.ReservationWage = RW;
                    m.Price = P;
                    if (h == 1) nStep1++; else nLabor++;
                    nRun += run; nSkip += h - run;
                }
                else
                {
                    double P0 = m.Price;
                    if (closedWage)
                    {
                        double t0 = BandT(P0, wrp, lnFloorWage, lnCeilWage, out _, out _);
                        if (System.Math.Abs(t0) < InnerT)
                        {
                            double g1 = 1 + adj;
                            double tp = BandT(P0 * System.Math.Pow(g1, h), wrp, lnFloorWage, lnCeilWage, out double lp, out _);
                            if (g1 > 0 && System.Math.Abs(tp) < InnerT)
                            {
                                m.Price = wrp * System.Math.Exp(lp * SoftT(tp, k, h));   // P′＝SB_{w,h}(P0·(1＋a)^h)
                                nClosedS++;
                                continue;
                            }
                            nPred++;
                        }
                        else if (MultiNationSim.TimeOn && double.IsNaN(t0)) nNaN++;
                        else { nStart++; if (MultiNationSim.TimeOn) { nStartSvc++; if (t0 > 0) nStartHi++; } }   // 計器の内訳（役務・上側＝参照より高い側）は POB_TIME のときだけ
                    }
                    else if (h == 1) nStep1++;
                    else nHard++;
                    double P = P0;
                    int run = 0;
                    while (run < h)
                    {
                        run++;
                        double p1 = SoftBandLn(P * (1 + adj), wrp, lnFloorWage, lnCeilWage, k);
                        bool still = Same(p1, P);
                        P = p1;
                        if (still) break;
                    }
                    m.Price = P;
                    nRun += run; nSkip += h - run;
                }
            }
            else
            {
                // 加速度的な是正（要望）：平衡(100%)近くは緩やか、端ほど急峻。距離は対数 |ln(価格/参照価格)| で測り
                // ＝床(5%)と天井(2000%)が同じ正規化距離1.0＝対称。倍率＝Center＋(Edge−Center)×ndist^Power。
                double rp = System.Math.Max(good.RefPrice * pl, Eps);
                // ★価格法則の輸出反映（2026-08-22・PriceLawSeesExports）：輸入は供給側に載るのに輸出は在庫にしか載らない非対称を閉じる。
                //   輸出州の産出が「過剰供給」と読まれて価格が崩れ、地元価格で決まる採算が産出を絞り輸出余剰が消える自壊を止める。
                //   TargetStock には入れない＝在庫目標は国内需要のまま。0＝旧式を逐語＝ビット恒等（分岐で足し算自体を回避）。
                double demandRaw = coef.PriceLawSeesExports > 0 ? totalDemand + m.LastExport : totalDemand;
                // 国内寡占マークアップ（no-market-power-monopoly）：財別有効生産者数EffFirmsが少ない財ほど価格支配で上乗せ。μ=1+DomesticMarkupMax/EffFirms はEffFirms→∞で1へ漸近＝自己有界（談合/鉄道プール）。
                // DomesticMarkupMax=0（既定）またはEffFirms未確保でμ=1＝現行price-takerに恒等。月次μを1日あたりへ薄く配分。
                // ⚠休眠の枝＝点火されたら日次へ落とす（閉じた式に μ を畳まない）。
                bool markup = false;
                double muDay = 1.0;
                if (coef.DomesticMarkupMax > 0 && region.EffFirms is { } ef && gi < ef.Length && ef[gi] >= 1.0)
                {
                    double mu = 1.0 + coef.DomesticMarkupMax / ef[gi];
                    muDay = 1.0 + (mu - 1.0) / div;
                    markup = true;
                }
                double P0 = m.Price;
                if (closedGoods && !markup)
                {
                    double t0 = BandT(P0, rp, lnFloorGoods, lnCeilGoods, out _, out double u0);
                    if (System.Math.Abs(t0) < InnerT)
                    {
                        // y0 は歩の始めの価格で1回だけ（日次の式と同じ式・同じ順）。1＋y0≧0.837（|y|≦0.61·8/30）＝(1＋y0)^h は常に正。
                        // ★2026-09-29（高速化の段 S2・厳密恒等）：y0 の ln(P0/rp) は BandT の ln(max(P0,Eps)/rp) と同じ double（P0≧Eps なら max は P0 そのもの）＝対数を2度取らない。
                        //   P0<Eps と NaN（否定形）は従来どおり ln(P0/rp) を取る。
                        // ★小刻み（2026-09-30 高速化の段 S3c・近似）：1歩の刻み hs は時定数の1/10 まで（<see cref="SubStep"/>）＝h を hs 日ずつ同じ閉じた式で進める
                        //   （y は小刻みの始めの価格で1回）。hs＝h なら旧の1歩と1ビットも同じ。小刻みの途中で内側を外れたら歩の始めから日次の経路へ（旧の予測の帯と同じ扱い）。
                        int hs = SubStep(h, u0, coef, div);
                        double Ps = P0, us = u0; bool inner = true;
                        for (int dn = 0; dn < h; dn += hs)
                        {
                            if (dn > 0 && !(System.Math.Abs(BandT(Ps, rp, lnFloorGoods, lnCeilGoods, out _, out us)) < InnerT)) { inner = false; break; }
                            double g1 = 1 + GoodsY(Ps, rp, demandRaw, supply, gap, coef, div, Ps >= Eps ? us : System.Math.Log(Ps / rp));
                            double tp = BandT(Ps * System.Math.Pow(g1, hs), rp, lnFloorGoods, lnCeilGoods, out double lp, out _);
                            if (!(g1 > 0 && System.Math.Abs(tp) < InnerT)) { inner = false; break; }
                            Ps = rp * System.Math.Exp(lp * SoftT(tp, k, hs));   // P′＝SB_hs(P·(1＋y)^hs)
                        }
                        if (inner)
                        {
                            m.Price = Ps;
                            nClosedG++;
                            continue;
                        }
                        nPred++;
                    }
                    else if (MultiNationSim.TimeOn && double.IsNaN(t0)) nNaN++;
                    else { nStart++; if (MultiNationSim.TimeOn && t0 > 0) nStartHi++; }   // 計器の内訳（上側＝参照より高い側）は POB_TIME のときだけ
                }
                else if (h == 1) nStep1++;
                else if (markup) nMarkup++;
                else nHard++;
                // 日次の写像（旧来の1日ぶんと同じ式・同じ順）を h 回。帯に張り付く財（供給0・在庫0で参照の約49倍）はここ＝張り付く水準を保つ。
                // ★張り付いた市場（S3b・近似）：供給が需要に比べて無い（≦需要×Eps＝流入も在庫も無い）財が参照より上にいる＝不動点への強い縮小
                //   ＝1日の動きが PinnedTol（相対）を切ったら打ち切る。旧＝ビットで止まるまで h 日（丸めの末尾が動き続け、止まるのは0.7%だけ＝S0）。
                //   供給か在庫が立つと需給差が変わり最初の1日の動きが大きく出る＝打ち切らない（眠らせない・理由で起きる）。
                bool pinned = !markup && !(supply > Eps * demandRaw) && P0 > rp;
                double P = P0;
                int run = 0;
                while (run < h)
                {
                    run++;
                    double p1 = P * (1 + GoodsY(P, rp, demandRaw, supply, gap, coef, div, System.Math.Log(P / rp)));   // gap は自己有界∈(−1,1)＝旧 symGap クランプ不要
                    if (markup) p1 *= muDay;
                    p1 = SoftBandLn(p1, rp, lnFloorGoods, lnCeilGoods, k);   // ハードクランプ→対数空間の滑らかな飽和（内部は恒等≒・境界に漸近＝角を除去）
                    bool still = Same(p1, P) || (pinned && System.Math.Abs(p1 - P) <= PinnedTol * P);
                    P = p1;
                    if (still) { if (pinned) m.FlowAsleep = true; break; }   // ★張り付いた財が不動点に着いた＝月の残りの旬は眠る（上の remarks）
                }
                m.Price = P;
                nRun += run; nSkip += h - run;
            }
        }
        if (cost is not null) cost[CostLedger.PriceGoods] += live.Length - nAsleep;   // 計算量の台帳（診断）：この州で回した財（死んでいない財のうち眠っていない財）
        if (cost is not null) cost[CostLedger.PriceDailyRuns] += nRun;   // 計算量の台帳（診断）：日次の写像をまわした回（役務・労働・帯の端の財）
        if (MultiNationSim.TimeOn)
        {
            System.Threading.Interlocked.Add(ref CntGoodSteps, nSteps);
            System.Threading.Interlocked.Add(ref CntClosedGoods, nClosedG);
            System.Threading.Interlocked.Add(ref CntClosedService, nClosedS);
            System.Threading.Interlocked.Add(ref CntDailyStartBand, nStart);
            System.Threading.Interlocked.Add(ref CntDailyStartHigh, nStartHi);
            System.Threading.Interlocked.Add(ref CntDailyStartService, nStartSvc);
            System.Threading.Interlocked.Add(ref CntDailyPredBand, nPred);
            System.Threading.Interlocked.Add(ref CntDailyLabor, nLabor);
            System.Threading.Interlocked.Add(ref CntDailyMarkup, nMarkup);
            System.Threading.Interlocked.Add(ref CntDailyHardBand, nHard);
            System.Threading.Interlocked.Add(ref CntDailyNaN, nNaN);
            System.Threading.Interlocked.Add(ref CntDailyStep1, nStep1);
            System.Threading.Interlocked.Add(ref CntDailyDaysRun, nRun);
            System.Threading.Interlocked.Add(ref CntDailyDaysSkipped, nSkip);
        }
    }

    /// <summary>財の1日ぶんの伸び y（P←P·(1＋y) の y）。段B：需給に**価格弾力性**を入れる＝実効需要 eD=D×(rp/P)^ε は割高で減り、
    /// 実効供給 eS=S×(P/rp)^ε は割高で増える。すると需給差 (eD−eS)/(eD+eS) は有限価格 P*=rp×(D/S)^(1/2ε) でゼロを切る＝価格が自律的に均衡へ収束する復元力。
    /// P=rp（平衡価格）では eD=D, eS=S＝段Aと同一（平常較正への影響は小）。離れた時だけ押し戻す＝クランプは外側の安全網に降格。
    /// ⚠ 旧来の日次の式を同じ順で書き写したもの（日次の経路と閉じた式の y0 はこの1本）＝h=1 でビット恒等。
    /// ★2026-09-29（高速化の段 S2・厳密恒等）：①lnDev＝ln(price/rp) は呼び手が渡す（閉じた式は BandT の対数をそのまま使う）
    ///   ②供給が ±0 なら eS＝supply×dev^ε＝supply（dev∈(0,最大値]・ε∈[0,1] で dev^ε は正の有限＝積は supply と同じビット）＝Pow を呼ばない
    ///   ③ndist＝1（参照の20倍より外＝上の帯に張り付いた財）なら 1^p＝1（IEEE の pow の定義）＝Pow を呼ばない。値は1ビットも変わらない。</summary>
    private static double GoodsY(double price, double rp, double demandRaw, double supply, double gap, Coefficients coef, double div, double lnDev)
    {
        double dev = price / rp;
        double e = coef.PriceElasticity;
        double eD = demandRaw * System.Math.Pow(dev, -e);
        double eS = supply == 0 && dev > 0 && dev <= double.MaxValue && e >= 0 && e <= 1 ? supply : supply * System.Math.Pow(dev, e);
        double flow = (eD - eS) / (eD + eS + Eps);
        double lnRef = coef.PriceAmpLnScale;   // amp曲線の端の対数距離（=ln20≈3.0）。安全網クランプ幅とは分離
        double ndist = System.Math.Min(1.0, System.Math.Abs(lnDev) / lnRef);
        double amp = coef.PriceAmpCenter + (coef.PriceAmpEdge - coef.PriceAmpCenter) * (ndist == 1.0 ? 1.0 : System.Math.Pow(ndist, coef.PriceAmpPower));
        return (coef.PriceAdjustSpeed * gap + coef.FlowAdjustSpeed * flow) / div * amp;
    }

    /// <summary>ビット単位で同じか（打ち切りの判定）。+0/−0 を区別し、同じビットの NaN を同一と見る＝写像の不動点を厳密に言う。</summary>
    private static bool Same(double a, double b) => System.BitConverter.DoubleToInt64Bits(a) == System.BitConverter.DoubleToInt64Bits(b);

    /// <summary>SoftBand の正規化距離 t＝ln(max(P,Eps)/rp)/L（L＝その向きの帯の対数）。<see cref="SoftBandLn"/> と同じ式。u＝その対数（閉じた式の y0 が使い回す）。</summary>
    private static double BandT(double price, double rp, double lnFloorPos, double lnCeilPos, out double L, out double u)
    {
        u = System.Math.Log(System.Math.Max(price, Eps) / rp);
        L = u >= 0 ? lnCeilPos : lnFloorPos;
        return u / L;
    }

    /// <summary>★閉じた式の小刻み（2026-09-30 S3c）：h の約数のうち hs×g≦<see cref="StepPerTau"/> を満たす最大（最小1）。g＝FlowAdjustSpeed·ε/div·amp(lnDev)
    /// （amp は <see cref="GoodsY"/> と同じ式）。NaN は h のまま＝旧の1歩の経路（予測の帯の判定で日次へ落ちる）。</summary>
    private static int SubStep(int h, double lnDev, Coefficients coef, double div)
    {
        double ndist = System.Math.Min(1.0, System.Math.Abs(lnDev) / coef.PriceAmpLnScale);
        double amp = coef.PriceAmpCenter + (coef.PriceAmpEdge - coef.PriceAmpCenter) * System.Math.Pow(ndist, coef.PriceAmpPower);
        double g = coef.FlowAdjustSpeed * coef.PriceElasticity / div * amp;
        int hs = h;
        while (hs > 1 && hs * g > StepPerTau) { hs--; while (hs > 1 && h % hs != 0) hs--; }
        return hs;
    }

    /// <summary>SoftBand を h 回重ねた正規化距離：s＝t／(1＋h·|t|^k)^(1/k)。
    /// |SB(t)|^−k＝|t|^−k＋1 なので h 回重ねると ＋h（厳密）。h=1 なら <see cref="SoftBandLn"/> の soft と同じ式。</summary>
    private static double SoftT(double t, double k, int h)
        => t / System.Math.Pow(1 + h * System.Math.Pow(System.Math.Abs(t), k), 1.0 / k);

    /// <summary>対数空間の滑らかな飽和。内部（|乖離|が小）では恒等≒で価格を素通しし、境界 rp×倍率へ**漸近**する
    /// （ハードな Clamp と違い導関数が連続＝角が無い）。床/天井の倍率が非対称でも可。k が大きいほどハードクランプに近い。
    /// ★境界の**対数**を受け取る形（A-09）＝呼び手が定数なら Math.Log を巻き上げられる。
    /// lnFloorPos＝−ln(床倍率)（正）／lnCeilPos＝ln(天井倍率)（正）。</summary>
    private static double SoftBandLn(double price, double rp, double lnFloorPos, double lnCeilPos, double sharpness)
    {
        double u = System.Math.Log(System.Math.Max(price, Eps) / rp);     // 参照価格からの対数距離
        double L = u >= 0 ? lnCeilPos : lnFloorPos;                       // その向きの境界までの対数距離（正）
        if (L <= Eps) return rp;
        double t = u / L;                                                 // 0=参照・±1=境界
        double k = sharpness;                                             // 飽和の鋭さ（→∞でハードクランプ）。内部は |t|<0.7 でほぼ恒等。係数化（coef.SoftBandSharpness）
        double soft = t / System.Math.Pow(1 + System.Math.Pow(System.Math.Abs(t), k), 1.0 / k);   // Lk ソフトクランプ：|t|→∞ で ±1 へ漸近
        return rp * System.Math.Exp(L * soft);
    }
}
