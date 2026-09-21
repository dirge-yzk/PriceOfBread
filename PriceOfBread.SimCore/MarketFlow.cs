namespace PriceOfBread.SimCore;

/// <summary>
/// 日次の価格形成（三層時計の「鼓動」＝市場フロー）。月次決算（<see cref="EconomyStep"/>）が残した需給
/// （LastDemand／LastProduced＋LastImport／Stock）を読み、価格を clearing 目標へ**少しずつ**寄せる。
/// 速度は月次係数を <see cref="Coefficients.PriceFlowDailyDiv"/>（≒30日）で割って配分するので、
/// 30日ぶんの積分が従来の月次1回の更新にほぼ一致する＝較正を温存したまま、日々わずかに価格が動く。
/// 金の移動は伴わない（価格を置くだけ）＝保存則に無関係。クリアリング・所得決済は月次のまま。
/// </summary>
public static class MarketFlow
{
    private const double Eps = 1e-9;

    public static void StepDay(Region region, Coefficients coef)
    {
        double div = coef.PriceFlowDailyDiv;
        // ★2026-09-03（キューA-09／出典 トリアージ 2-A 10-4c）：財側の SoftBand の境界対数は**係数だけで決まる定数**なので、
        //   財×日ごとに Math.Log を2回呼ぶ必要がない。ここで1回だけ計算して渡す＝同じ double を使うのでビット恒等。
        double lnCeilGoods = System.Math.Log(coef.PriceCeilFactor);
        double lnFloorGoods = -System.Math.Log(coef.PriceFloorFactor);
        // ★2026-09-08（10-4c の残り＝賃金側）：賃金の天井 effWageMax は coef と region.Industrialization だけで決まり、
        //   この foreach は市場（m.*）しか書かない＝**財ループ不変**（日をまたげば動くが、StepDay 1回の中では定数）。
        //   よって天井/床の対数も財ごとに Math.Log を呼ぶ必要がない＝役務1財あたり2回→州1回へ。渡す double は同一＝ビット恒等。
        double effWageMax = coef.WageClampMax * (1 + coef.WageClampIndScale * region.Industrialization);
        double lnCeilWage = System.Math.Log(effWageMax);
        double lnFloorWage = -System.Math.Log(coef.WageClampMin);
        foreach (var good in region.Goods)
        {
            int gi = good.Id;
            var m = region.Markets[gi];
            double totalDemand = m.LastDemand;
            if (totalDemand < Eps && m.LastProduced < Eps) continue;   // まだ決算が無い／死んだ財は動かさない

            m.TargetStock = System.Math.Max(coef.TargetStockFloor, coef.TargetStockBuffer * totalDemand) * region.StockpileFactor(gi, coef);
            // 在庫ギャップ＝自己有界な正規化差 (Target−Stock)/(Target+Stock)∈(−1,1)（役務の flow と同形）。手クランプ不要で過剰/不足が対称（倍率排除）。
            double gap = (m.TargetStock - m.Stock) / (m.TargetStock + m.Stock + Eps);
            // ★価格振動の根①（2026-09-07・器 PriceLawSupplyIncludesStock）：0＝旧式（今月の流入だけ）・1＝倉庫の在庫も供給に数える（在庫が満ちている州は下がる）。
            // ★価格振動の根①（2026-09-07・器 PriceLawSupplyIncludesStock／2026-09-08 作者裁定「備蓄がある限り、それは生産できているものとみなす」）：
            //   0＝旧式（今月の流入だけ）＝輸入の無い月は倉庫が満杯でも供給0と読まれ、需給差が+1へ飽和して価格が上がり続ける（硫黄の鋸歯の根）。
            //   1＝**需要までは備蓄が生産の代わりをする**＝supply = max(流入, min(流入+在庫, 需要))。上限が需要なので
            //     ・流入が需要を超える月は従来どおり流入がそのまま供給（豊作で価格が落ちる挙動は不変）
            //     ・流入が細っても在庫が需要を覆う月は供給=需要＝需給差0＝**上がりも下がりもしない**（欠乏でないので煽らない）
            //     ・在庫も足りない月だけ不足の分だけ上がる
            //   ⚠在庫の「過剰」を需給差へ足してはいけない＝在庫ギャップ項（上の gap）が既に同じ過剰を下押ししており**二重計上**になる。
            //     実際 max(0, Stock−Target) を足した版は供給が過大に見え、限界的な生産者が price<=mc で止まって機構の番人3件が赤になった（2026-09-08 差し戻し）。
            double flowIn = m.LastProduced + m.LastImport;
            double supply = coef.PriceLawSupplyIncludesStock > 0
                ? System.Math.Max(flowIn, System.Math.Min(flowIn + m.Stock, totalDemand))
                : flowIn;

            // 物価水準（名目のヴェール）：参照価格アンカー・床天井を rp×P でスケール＝賃金/財が一斉に騰落＝一般インフレ/デフレ。P≡1＝従来同値。
            double pl = region.PriceLevel;
            if (good.Tier == GoodTier.Service)
            {
                // 役務は在庫を持てない＝フローのみで価格形成。賃金は下方硬直（賃下げ方向は遅い＝持続的失業を生む）。
                // 需給差は自然有界な正規化差 (D−S)/(D+S)∈[−1,1]（段A。弾力性は役務＝賃金には掛けない＝賃金硬直を温存）。
                double flow = (totalDemand - supply) / (totalDemand + supply + Eps);
                double adj = coef.FlowAdjustSpeed * flow / div;
                double target = m.Price * (1 + adj);   // 無拘束の目標賃金（需給が指す clearing）
                if (gi is Catalog.LaborId or Catalog.AdvLaborId or Catalog.FarmLaborId)
                {
                    // 下方硬直は固定倍率でなく留保賃金から創発：留保賃金は賃上げに速く・賃下げに遅く追従し、賃金はそれを下限に置く。
                    // 持続失業（超過供給）では目標がじわじわ下がり留保賃金も遅れて切り下がる＝名目賃金は下がりにくいが永久には張り付かない。
                    if (!(m.ReservationWage > Eps)) m.ReservationWage = m.Price;   // 否定形＝NaN も初期化側へ（Math.Max は NaN を除去しない）
                    // 組合の賃金交渉（no-monopsony-wage）：組織率(LaborWellbeing代理)で留保賃金の上昇速度を増幅＝賃上げに速く・賃下げに抗う。UnionWageRiseBoost=0.5 だが LaborWellbeing は UnionSatBonus=0（休眠）で実効0＝ReservationWageRiseのまま。下方(Fall)は触らず下方硬直温存。
                    double unionRise = coef.ReservationWageRise * (1.0 + coef.UnionWageRiseBoost * region.LaborWellbeing);
                    double rwSpeed = target >= m.ReservationWage ? unionRise : coef.ReservationWageFall;
                    m.ReservationWage += (target - m.ReservationWage) * rwSpeed;
                    m.Price = System.Math.Max(target, m.ReservationWage);
                }
                else m.Price = target;
                // 賃金/役務の床天井。0.3–3.0×(参照価格×物価水準)。財側の SoftBand が係数化済みなのに賃金だけ直書きだった＝係数化（値は同一で中立）。
                // WageClampSoftBand=1.0（有効）＝財と同じ対数飽和(SoftBand)＝実質賃金成長・国際賃金格差の圧縮を緩める。0で従来のハードクランプ（1789基準3倍を天井に固定）。
                double wrp = good.RefPrice * pl;
                // R2（賃金天井の工業化連動）：天井を固定WageClampMaxでなく工業化度でスケール＝未工業国は従来の3倍据置・工業国のみ実質賃金が伸びる（史実の産業革命の賃金上昇・国際格差の拡大）。WageClampIndScale=1.0（R2点火2026-07-15）。0でeffWageMax=WageClampMax＝旧恒等。床(Min)は据置（下方硬直の温存）。effWageMax とその対数は :24 で州1回に巻き上げ済み
                m.Price = coef.WageClampSoftBand > 0.5
                    ? SoftBandLn(m.Price, wrp, lnFloorWage, lnCeilWage, coef.SoftBandSharpness)   // 床/天井の倍率＝WageClampMin/effWageMax（財の SoftBand と同じ対数空間飽和）
                    : System.Math.Clamp(m.Price, wrp * coef.WageClampMin, wrp * effWageMax);      // 従来のハードクランプ（係数化のみ・値は同一）
            }
            else
            {
                // 加速度的な是正（要望）：平衡(100%)近くは緩やか、端ほど急峻。距離は対数 |ln(価格/参照価格)| で測り
                // ＝床(5%)と天井(2000%)が同じ正規化距離1.0＝対称。倍率＝Center＋(Edge−Center)×ndist^Power。
                double rp = System.Math.Max(good.RefPrice * pl, Eps);
                // 段B：需給に**価格弾力性**を入れる＝実効需要 eD=D×(rp/P)^ε は割高で減り、実効供給 eS=S×(P/rp)^ε は割高で増える。
                // すると需給差 (eD−eS)/(eD+eS) は有限価格 P*=rp×(D/S)^(1/2ε) でゼロを切る＝価格が自律的に均衡へ収束する復元力。
                // P=rp（平衡価格）では eD=D, eS=S＝段Aと同一（平常較正への影響は小）。離れた時だけ押し戻す＝クランプは外側の安全網に降格。
                double dev = m.Price / rp;
                // ★価格法則の輸出反映（2026-08-22・PriceLawSeesExports）：輸入は :27 で供給側に載るのに輸出は在庫にしか載らない非対称を閉じる。
                //   輸出州の産出が「過剰供給」と読まれて価格が崩れ、地元価格で決まる採算が産出を絞り輸出余剰が消える自壊を止める。
                //   TargetStock（:24）には入れない＝在庫目標は国内需要のまま。0＝旧式を逐語＝ビット恒等（分岐で足し算自体を回避）。
                double eD = (coef.PriceLawSeesExports > 0 ? totalDemand + m.LastExport : totalDemand) * System.Math.Pow(dev, -coef.PriceElasticity);
                double eS = supply * System.Math.Pow(dev, coef.PriceElasticity);
                double flow = (eD - eS) / (eD + eS + Eps);
                double lnRef = coef.PriceAmpLnScale;   // amp曲線の端の対数距離（=ln20≈3.0）。安全網クランプ幅とは分離
                double ndist = System.Math.Min(1.0, System.Math.Abs(System.Math.Log(m.Price / rp)) / lnRef);
                double amp = coef.PriceAmpCenter + (coef.PriceAmpEdge - coef.PriceAmpCenter) * System.Math.Pow(ndist, coef.PriceAmpPower);
                m.Price *= 1 + (coef.PriceAdjustSpeed * gap + coef.FlowAdjustSpeed * flow) / div * amp;   // gap は自己有界∈(−1,1)＝旧 symGap クランプ不要
                // 国内寡占マークアップ（no-market-power-monopoly）：財別有効生産者数EffFirmsが少ない財ほど価格支配で上乗せ。μ=1+DomesticMarkupMax/EffFirms はEffFirms→∞で1へ漸近＝自己有界（談合/鉄道プール）。
                // DomesticMarkupMax=0（既定）またはEffFirms未確保でμ=1＝現行price-takerに恒等。月次μを日次へ薄く配分。
                if (coef.DomesticMarkupMax > 0 && region.EffFirms is { } ef && gi < ef.Length && ef[gi] >= 1.0)
                {
                    double mu = 1.0 + coef.DomesticMarkupMax / ef[gi];
                    m.Price *= 1.0 + (mu - 1.0) / div;
                }
                m.Price = SoftBandLn(m.Price, rp, lnFloorGoods, lnCeilGoods, coef.SoftBandSharpness);   // ハードクランプ→対数空間の滑らかな飽和（内部は恒等≒・境界に漸近＝角を除去）
            }
        }
    }

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
