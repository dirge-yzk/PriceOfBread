namespace PriceOfBread.SimCore;

/// <summary>
/// 国際市場（§5.1 多国版）＝**二国間交易**（作者裁定2026-08-11・docs/202608/二国間交易への置換_設計_完了.md【完了】.md）。
/// 各国の**全active州**を、州と州の**実経路**（陸→最寄り港→海盆→港→陸）で結ぶ清算。
/// 買い手は「自分のところまで最も安く着く売り手」を選ぶ＝**世界にひとつの価格は存在しない**（同じ財でも産地と距離で着値が違う）。
/// **輸入の支払い＝輸出の受取**で地球規模では金が保存（各国の貿易収支は CumulativeTradeFlow で黒字/赤字を追跡）。
/// 輸送ゲート：各州のアクセスを鉄道/船容量で律速。**港州＝船のみ・内陸州＝鉄道＋船(min)**＝港ハブ効果
/// （内陸は鉄道がボトルネック＝港経由の含意）。容量は国の州数で分け合う。
/// 禁輸・交戦の国は交易が縮む（§6.1）。植民地素材は交易の外（植民地機構で別途・封鎖が効く）。
/// → 比較優位（工業州は工業品を輸出し食料/原料を輸入、農業州はその逆）が州単位で創発する。
///
/// ⚠**このファイルは「入口」と「分配のヘルパー」だけを持つ**。清算の本体は partial の相方 BilateralMarket.cs にある。
/// ⚠**旧・世界市場**（世界の交易重心／単一の世界価格／残余世界の費用アンカー／世界価格への独占マークアップ）の
///   実装は **2026-08-12 に撤去**（BilateralTrade=1.0 で到達不能になっていた433行）。
///   ★_price は廃止していない——**BilateralMarket が「平均価格」として書き**、Validation の「世界価格の発見」と UI が読む。
/// </summary>
public sealed partial class InternationalMarket
{
    private readonly double[] _price;    // 財ごとの平均価格（BilateralMarket が毎期の成約から書く。旧・単一の世界価格ではない）
    private readonly double[] _volume;   // 直近の財別取引量（テレメトリ）

    public IReadOnlyList<double> Price => _price;
    public IReadOnlyList<double> Volume => _volume;

    private const double DistanceScaleKm = 10_000.0;   // 海路距離をマージンへ落とす正規化スケール（km）
    private const double PriceBandFloorMul = 0.1;       // 平均価格の対RefPrice下限倍率
    private const double PriceBandCeilMul = 10.0;       // 平均価格の対RefPrice上限倍率
    // ⚠2026-08-12：MaxExportMarginCap / WorldPriceStepMin / WorldPriceStepMax は撤去した。
    //   旧・世界市場（世界価格の1ステップ変化率クランプ・輸出マージン上限）だけが読んでおり、
    //   433行の削除で**読み手が0件**になっていた（private const なのでコンパイラは警告を出さない＝静かに残る種類の死骸）。

    public InternationalMarket(int goodCount, double worldPriceInitFromRefPrice = 0.0)
    {
        _price = new double[goodCount];
        _volume = new double[goodCount];
        // 財11（world-price-init-1.0）：平均価格の**初期値**。既定0＝全財1.0。>0.5でRefPrice初期化に切替
        // ＝開始数年の高額財交易の凍結・安い原料の初期過大を解消。★点火2026-07-27（WorldPriceInitFromRefPrice=1.0）＝MultiNationSim から配線済み。
        // ⚠この初期値は今も生きている——BilateralMarket が最初の成約を書くまでの間、Validation と UI が読む値がここで決まる。
        bool initFromRef = worldPriceInitFromRefPrice > 0.5;
        for (int i = 0; i < goodCount; i++)
            _price[i] = initFromRef && i < Catalog.RefPrices.Length ? Catalog.RefPrices[i] : 1.0;   // 参照価格から開始（取引で調整）
    }


    /// <param name="firstClearingOfQuarter">★月次清算（TradeMonthlyClearing・2026-08-22）：四半期の最初の清算なら true＝四半期累計量（LastPortThroughput）をリセット。既定 true＝従来の四半期1回呼び出しと厳密同値。</param>
    public void Step(IReadOnlyList<Simulation> nations, Coefficients c, bool firstClearingOfQuarter = true)
    {
        // 二国間交易（作者裁定2026-08-11・docs/202608/二国間交易への置換_設計_完了.md【完了】.md）＝州と州が実経路
        //（陸→最寄り港→海盆→港→陸）で結ばれる清算。買い手は「自分のところまで最も安く着く売り手」を選ぶ。
        // ⚠旧・世界市場（世界の交易重心／世界価格／残余世界の費用アンカー／世界価格への独占マークアップ）の
        //   実装は **2026-08-12 に撤去**（BilateralTrade=1.0 で到達不能になっていた433行）。
        //   撤去時の基準線＝SOURCE-HASH f66918666a65028b（build緑・テスト283/283緑・savetest 相対差0.0E+000）。
        StepBilateral(nations, c, firstClearingOfQuarter);
    }

    // ダンピング（負の関税）の輸出補助金：国庫→輸出の担い手（商人pop）へ。関税徴収(CollectTariff)の逆＝国内移転で保存（金は湧かない）。
    // 国庫残高を超える分は払えない（部分補助）。商人不在なら支払わず戻す。
    private static void PaySubsidy(World w, double amount, bool tradeBias = false)
    {
        double pay = Math.Min(amount, Math.Max(0, w.Treasury));
        if (pay <= 1e-12) return;
        var merchants = new List<Pop>();
        foreach (var r in w.Regions) if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) merchants.Add(r.Pops[r.MerchantPopIndex]);
        if (merchants.Count == 0) return;
        w.Pay(TreasuryCat.Tariff, -pay);
        double each = pay / merchants.Count;
        foreach (var p in merchants) p.Savings += each;
        // 関税会計の分離（2026-08-08）：売買方針の代金は関税の実収に混ぜない・ダンピング補助金は関税の純額から減算。
        // 旧＝両方が LastTariffRevenue を**負値で上書き**し、CollectTariff の正値を消していた（関税収入が全世界で実質0の根）。
        if (!tradeBias) w.LastTariffRevenue -= pay;
    }

    // 財15：輸出代金の一部を当該財の生産者の所有者popへ直接支払う（生産量シェアで按分）。所得は所有者popのSavingsへ入る＝利潤シグナル。
    // ※LastSold には積まない：LastSold は財収支恒等式(GoodsBalance)の販売量であり、輸出は LastExport で既に計上済み。ここで足すと同一数量の二重計上→在庫恒等式が破れて InvariantViolationException。採算(SmoothedMargin)は価格vs限界費用ベースで別経路（LastSold非依存）。
    // 実際に渡せた額を返す（呼び出し側は netFlow から差し引き、別途 direct として台帳へ計上＝保存）。生産者不在/産出0の財は0を返し全額 netFlow へフォールバック＝現行経路。
    // internal＝単一国経路（Trade.cs）と多国経路が同じ会計を共有（重複定義の解消）。
    // ★2026-09-26（高速化・厳密恒等）：その財の生産者の列（元の順）と totalOut を、呼び手が持つ記憶（ownPr/ownTotal＝清算の局所・州×財ごと）へ
    //   初回だけ作って返し、2回目からは州の全生産者（約150）を2周しない。清算の中で Producers・Output・OwnerPopIndex・Pops の数を変える行は無い
    //   （変わるのは Savings だけ）＝同じ部分列を同じ順で足す＝浮動小数も同一。旧＝成約ごとに全生産者を2周（成約は全長で3.16億回）。
    internal static double PayExportToProducers(Region r, int goodId, double amount, ref Producer[]? ownPr, ref double ownTotal)
    {
        if (amount <= 1e-12) return 0.0;
        if (ownPr is null)
        {
            int n = 0; double totalOut = 0;
            foreach (var pr in r.Producers) if (pr.GoodId == goodId && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < r.Pops.Count) { n++; totalOut += Math.Max(0, pr.Output); }
            var list = new Producer[n]; n = 0;
            foreach (var pr in r.Producers) if (pr.GoodId == goodId && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < r.Pops.Count) list[n++] = pr;
            ownPr = list; ownTotal = totalOut;
        }
        if (ownTotal <= 1e-9) return 0.0;   // 生産者不在＝按分不能＝現行の全pop経路へ全額戻す
        double paid = 0;
        foreach (var pr in ownPr)
        {
            double sh = Math.Max(0, pr.Output) / ownTotal;
            r.Pops[pr.OwnerPopIndex].Savings += amount * sh; paid += amount * sh;
        }
        return paid;
    }

    // 穀物法の保護強度：なし0／限定的0.5／全面1.0（輸入閾値への上乗せ＝CornLawImportPenalty に掛かる）。
    /// <summary>条約港による関税の実効低下（2026-08-04・作者仕様(4)）。港を開かされるほど自国の関税が効かなくなる。
    /// 史実＝南京条約1842は5港開港＋協定関税（清は税率を自分で決められなくなった）・安政条約1858も同型。
    /// 最恵国待遇で特権は全列強へ波及したので**全ての相手に効く**＝二国間テーブルを持たない（モデルの粒度と一致）。
    /// TreatyPortTariffLoss=0 で 1.0＝乗算恒等（休眠）。</summary>
    internal static double TreatyPortTariffScale(World w, Coefficients c)   // internal＝ProductionDiag が関税の楔を同式で読む
        => w.TreatyPortsGranted <= 0 || c.TreatyPortTariffLoss <= 0
            ? 1.0
            : Math.Max(0, 1.0 - c.TreatyPortTariffLoss * w.TreatyPortsGranted);

    internal static double CornFactor(ReformLevel lv) => lv switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };   // internal＝ProductionDiag が穀物法の楔を同式で読む

    // 関税収入を**輸入の捌き手（商人pop）の輸入差益**から国庫へ（④貨幣金融）。
    // 関税の消費者帰着は①価格チャネル（保護で国内価格↑＝EconomyStepの buyerPrice）が既に担う＝消費者は割増を払済み（∝消費・逆進）。
    // ②歳入は別途pop頭割りでなく、その割増が溜まる先＝商人pop（EconomyStep #6 B-A2で輸入差益は生産者でなく商人へ）から国庫へ「付け替え」る＝消費者への二重徴収なし・史実の輸入業者の納税・倍率排除に忠実。
    // 残高超は取り切れない（ラッファー的に部分）。輸入0/較正（交易なし＝amount=0）では何も起きず中立。
    private static void CollectTariff(World w, double amount)
    {
        if (amount <= 1e-12) return;
        var merchants = new List<Pop>();
        foreach (var r in w.Regions) if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) merchants.Add(r.Pops[r.MerchantPopIndex]);
        double total = 0; foreach (var p in merchants) total += Math.Max(0, p.Savings);
        double take = Math.Min(amount, total);
        if (take <= 1e-12 || total <= 1e-9) return;
        foreach (var p in merchants) p.Savings -= take * (Math.Max(0, p.Savings) / total);
        w.Pay(TreasuryCat.Tariff, take);
        w.LastTariffRevenue += take;   // 診断：関税の実収の**累積**（旧＝代入で財ごとに上書き＝最後の1財分しか残らなかった。会計分離2026-08-08）
    }

    // 純貿易流入を pop 貯蓄へ配分＋貿易収支へ計上（保存の追跡）。各国個別は黒字/赤字でよい（世界合計は0）。
    // 黒字（流入）から商人が仲介料を先取り（取扱高＝捌いた量に比例＝交易の担い手）、残りは貯蓄比例。赤字（流出）は貯蓄比例で負担。
    private static void Apply(World w, double netFlow, double turnover, Coefficients c, double directLedger = 0)
    {
        double remaining = netFlow;
        if (netFlow > 0 && turnover > 1e-9)
        {
            double mSize = 0;
            foreach (var r in w.Regions) if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) mSize += r.Pops[r.MerchantPopIndex].Size;
            if (mSize > 1e-9)
            {
                // 仲介料＝取扱高×料率（黒字の固定割合でなく捌いた量に比例）。流入を上限にクランプ（手数料が黒字を超えない）。
                double mShare = Math.Min(netFlow, turnover * c.MerchantTradeMargin);
                foreach (var r in w.Regions)
                    if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count)
                    {
                        var mp = r.Pops[r.MerchantPopIndex];
                        mp.Savings += mShare * (mp.Size / mSize);
                    }
                remaining -= mShare;
            }
        }
        var pops = new List<Pop>();
        foreach (var r in w.Regions) pops.AddRange(r.Pops);
        double total = 0; foreach (var p in pops) total += Math.Max(0, p.Savings);   // 分母は Max(0,·)（負貯蓄popで逆符号配分になるのを防ぐ）
        if (pops.Count > 0 && total > 1e-9)
        {
            double done = 0;
            for (int i = 0; i < pops.Count - 1; i++) { double s = remaining * (Math.Max(0, pops[i].Savings) / total); pops[i].Savings += s; done += s; }
            pops[^1].Savings += remaining - done;
        }
        else w.Pay(TreasuryCat.OtherIn, remaining);   // 分配不能（pop不在・総貯蓄≈0の貯蓄枯渇国）なら残余を国庫へ落とす（財2）＝実マネーが必ず remaining ぶん動く
        // ＝仲介料(pop)＋残余(pop or 国庫)の合計は常に netFlow。財15の生産者直接分(directLedger)は PayExportToProducers で既に pop へ入れ済みなので、ここでは台帳にのみ加算（二重配分しない）。
        w.CumulativeTradeFlow += netFlow + directLedger;   // Δ基礎貨幣 = netFlow(Apply配分) + directLedger(生産者直接) と厳密一致＝MoneyConservation維持。既定0で directLedger=0＝現行不変
    }
}
