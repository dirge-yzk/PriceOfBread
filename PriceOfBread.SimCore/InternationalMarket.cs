namespace PriceOfBread.SimCore;

/// <summary>
/// 国際市場（§5.1 多国版）＝**二国間交易**（作者裁定2026-08-11・docs/202608/二国間交易への置換_設計_完了.md【完了】.md）。
/// 各国の**全active州**を、州と州の**実経路**（陸→最寄り港→海盆→港→陸）で結ぶ清算。
/// 買い手は「自分のところまで最も安く着く売り手」を選ぶ＝**世界にひとつの価格は存在しない**（同じ財でも産地と距離で着値が違う）。
/// **輸入の支払い＝輸出の受取**（金建て）で地球規模では金が保存（各国の貿易収支は CumulativeTradeFlow で黒字/赤字を追跡・★段3-1 から各国の通貨へ為替で換算＝名目の差は Diplomacy.CumulativeFxResidual）。
/// 輸送ゲート：各州のアクセスを鉄道/船容量で律速。**港州＝船のみ・内陸州＝鉄道＋船(min)**＝港ハブ効果
/// （内陸は鉄道がボトルネック＝港経由の含意）。国の容量は前期に各州が運びたかった量に比例して配る（TransportShareByPotential・実績の無い開幕は均等割り）。
/// 禁輸・交戦の国は交易が縮む（§6.1）。植民地財も同じ二国間清算に載る（TradeColonialGoods・2026-08-26 点火）。
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
    // ★為替の換算の台帳は 2026-09-30（段3-3）に共有層の Diplomacy.CumulativeFxResidual へ移した（国境をまたぐ全ての払いが同じ1本に書く）。

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

    // 【墓標2026-09-29・支払いの一元化 段2-3（E2）】PaySubsidy（ダンピングの輸出補助金と売買方針の代金＝国庫→商人POP の頭数で等分）と
    //   CollectTariff（関税＝全州の商人POP の貯蓄比で取って残高で切る）は、清算の末尾（BilateralMarket.StepBilateral）で移し方の1本 World.Transfer を直接呼ぶ形へ畳んだ
    //   （受け手・重み・区分・関税の実収の記帳は同じ。受け手へ渡すのは引けた額そのもの＝A4）。

    /// <summary>★④の持分で売上を分ける（支払いの一元化 段3-1・C4・2026-09-29）＝市場の外の売上（輸出代金・輸送財の買い付け）を、
    /// 国内の売上と同じ④の分け方で受け取らせる：その財の生産者の産出の比で分け、地代（RGO は地代の上限と土地の稀少度＝EconomyStep.RentShareOf）
    /// のうち国営持分（World.StateProfitShare）は国庫（区分 StateFactory）・残りの地代は持ち主・賃金は労働。**受け取りは Earned を通す**
    /// （所得・信用枠・票・課税の基数＝月次の精算の後の受け取りなので貯蓄と所得を同じ額で書く＝Fiscal の領主の取り分と同じ作法）。
    /// 生産者のいない在庫（輸入の在庫）は捌き手の商人へ（EconomyStep の「生産者のいない財の売上」と同じ規則・商人不在は州の頭数）。
    /// 旧＝PayExportToProducers（輸出代金の半分〔係数 ExportRevenueToProducers〕を持ち主の貯蓄へ直接・賃金分なし・残りは国の全POP の貯蓄比）。
    /// 払い手＝from（清算所＝(null, null)・輸送財の買い手＝(国, [POP])・国庫＝(国, null)＝区分 <paramref name="cat"/>）。戻り値＝動いた額。
    /// ★段3-2（2026-09-30）：<paramref name="r"/>＝null は**国の全ての活きた州**の生産者の産出の比（役務＝兵士の兵力・官僚の行政・労働の工賃を国として買う＝連合と植民地の維持費・運河の工賃）。
    ///   生産者が1人もいなければ動かさない（受け手不在＝払い手に残る）。国庫が自国の国営持分から買う分は区分だけ動く（払い手の区分から StateFactory へ＝EconomyStep の④と同じ記帳）。
    /// ★段3-5（2026-09-30・C8）：<paramref name="minted"/>＝払い手が造幣局（世界の外＝新しい貨幣）＝世界の台帳は鋳造（CumulativeGoldMinted）へ（移し方の1本の引数をそのまま渡す）。</summary>
    internal static double PaySales((World? W, List<Pop>? Pops) from, Region? r, int goodId, double amount, World w, TreasuryCat cat = TreasuryCat.OtherIn, bool minted = false)
    {
        if (!(amount > 0)) return 0;
        var coef = w.Coef;
        int nr = r is null ? w.Regions.Count : 1;
        static bool Sells(Producer pr, Region rr, int g) => pr.GoodId == g && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < rr.Pops.Count && pr.LaborPopIndex >= 0 && pr.LaborPopIndex < rr.Pops.Count;
        double totalOut = 0;
        for (int ri = 0; ri < nr; ri++)
        {
            var rr = r ?? w.Regions[ri];
            if (r is null && rr.State != RegionState.Active) continue;
            foreach (var pr in rr.Producers)
                if (Sells(pr, rr, goodId)) totalOut += Math.Max(0, pr.Output);
        }
        var to = new List<Pop>(); List<double>? wt = null;
        double stateCut = 0;
        if (totalOut > 1e-9)
        {
            wt = [];
            for (int ri = 0; ri < nr; ri++)
            {
                var rr = r ?? w.Regions[ri];
                if (r is null && rr.State != RegionState.Active) continue;
                double ruralPop = rr.SumRuralPopulation();
                foreach (var pr in rr.Producers)
                {
                    if (!Sells(pr, rr, goodId)) continue;
                    double amt = amount * (Math.Max(0, pr.Output) / totalOut);
                    if (!(amt > 0)) continue;
                    double rent = EconomyStep.RentShareOf(pr, rr, coef, ruralPop) * amt;
                    double wages = amt - rent;
                    double sc = rent * World.StateProfitShare(pr, rr, coef);
                    if (sc > 0) { stateCut += sc; rent -= sc; }
                    if (rent > 0) { to.Add(rr.Pops[pr.OwnerPopIndex]); wt.Add(rent); }
                    if (wages > 0) { to.Add(rr.Pops[pr.LaborPopIndex]); wt.Add(wages); }
                }
            }
        }
        if (to.Count == 0)
        {
            if (r is null) return 0;   // 国として買う役務の担い手がいない＝動かさない（払い手に残る）
            stateCut = 0; wt = null;
            if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) to.Add(r.Pops[r.MerchantPopIndex]);
            else to.AddRange(r.Pops);   // 重み null＝頭数
        }
        double moved = 0;
        // 国営持分の取り分は国庫へ（受け手の区分は StateFactory）。払い手が自国の国庫なら払い手の区分は買ったものの区分（cat）＝区分だけ動く（段4f＝旧の Pay の2行を移し方の1本へ）。
        if (stateCut > 0)
            moved += World.Transfer(from, (w, null, null), stateCut, from.Pops is null && ReferenceEquals(from.W, w) ? cat : TreasuryCat.StateFactory, toCat: TreasuryCat.StateFactory, minted: minted);
        moved += World.Transfer(from, (w, to, wt), amount - stateCut, cat, income: true, minted: minted);
        return moved;
    }

    /// <summary>★市場の外の買い付けの1本（2026-09-30 処理の一元化 段4e K の <c>Procure</c>）＝州の市場から財 <paramref name="g"/> を <paramref name="want"/> だけ買って消費する：
    /// 需要を計上（在庫が無くても＝生産を駆動する）→ 在庫と買い手の貯蓄で律速 → 在庫を減らし財の出入りの台帳へ → 代金はその財の生産者の④の持分（<see cref="PaySales"/>）。
    /// 買い手＝POP（<paramref name="buyer"/>＝汽船の燃料と船と線路の商人・投資家＝貯蓄まで・台帳は販売 LastSold）／null＝国庫（国家の調達＝<see cref="Colonial.BuyUpkeepGood"/>＝
    /// 価格の門と国家の支払い率 r は呼び手が量に掛けた後・台帳は域外 LastExport・区分 <paramref name="cat"/>）。戻り値＝買えた量。
    /// ⚠旧＝国家の調達（BuyUpkeepGood）と汽船の燃料（Transport.BuyShipFuel）と船と線路（Transport.BuyToward）が同じ手順を3つ写した。
    /// 価格が付いていない財（≦1e-9）は買わない（国家の側は旧＝床 1e-9 で買った＝価格は帯の床で常に正＝同じ）。</summary>
    internal static double Procure(World w, Region r, int g, double want, Pop? buyer, TreasuryCat cat = TreasuryCat.OtherIn)
    {
        var m = r.Markets[g];
        m.LastDemand += want;                  // 在庫が無くても需要を計上＝価格上昇と増産を駆動（鶏卵回避）
        if (m.Stock <= 1e-9 || m.Price <= 1e-9) return 0;
        double buy = Math.Min(m.Stock, want);  // 在庫超は買えない（物理の律速）
        double pay = buy * m.Price;
        if (buyer is not null && pay > buyer.Savings) { pay = Math.Max(0, buyer.Savings); buy = pay / m.Price; }   // 買い手の貯蓄まで
        if (buy <= 1e-9) return 0;
        m.Stock -= buy;
        if (buyer is null) m.LastExport += buy;   // 国家の調達＝域外（植民地・艦隊・遠征）へ出る
        else m.LastSold += buy;                   // 私の買い付け＝販売（財収支に計上）
        PaySales(buyer is null ? (w, null) : (w, [buyer]), r, g, pay, w, cat);
        return buy;
    }

    /// <summary>国として財 g を買う支払い（<see cref="PaySales"/> の国の枝）に受け手がいる＝国の活きた州のその財の産出の和＞0（PaySales の totalOut と同じ閾値）。
    /// 産出の和は月次の決算が書いた <see cref="Market.LastProduced"/>（その財の生産者の Output の和＝EconomyStep）を読む＝生産者を走査しない（州の数だけ）。
    /// ★段4d（2026-09-30・分子と分母を同じ規則で）：国として役務を買う請求（連合維持費・植民地の駐留の俸給）は受け手のいる分だけ立てる＝払えない請求で国家の支払い率 r を下げない。</summary>
    internal static bool HasSellers(World w, int g)
    {
        double sum = 0;
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active && g < r.Markets.Length) sum += Math.Max(0, r.Markets[g].LastProduced);
        return sum > 1e-9;
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

    // 【墓標2026-09-29・支払いの一元化 段3-1】Apply（国ごとの純流入を、黒字なら商人の仲介料＋残りを国の全POP の貯蓄比・赤字も全POP の貯蓄比で配る）を削除した。
    //   輸入代金は捌き手の商人・売り手の受取は④の持分・運賃は船の持ち主・仲介料は越境の輸出の受取から州の商人へ（黒字と赤字で対称）＝BilateralMarket.StepBilateral の清算の終わり。
}
