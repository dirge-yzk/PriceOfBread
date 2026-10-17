namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (380) 輸送の段0 の計器（読むだけ・設計 docs/202610/20261016/20261016_輸送の手段と距離_設計.md §5-1 段0） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsFm.cs／InstrumentsPop.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （蓄えは InternationalMarket の外＝ConditionalWeakTable・セーブに載らない）。出力は INS=1（POB_INSTRUMENTS）のときだけ。偽の間は本体の hook が 1 命令も走らない。
//
// 列の名前：接尾辞 PerYear＝その年に記帳した成約の合計（金建て・重さ・件数）／Gold＝金建て／Km＝実の km（eq＝運賃の換算 km）／PerMonth＝州の断面の月の流れ（README §1-9）。
// 出すもの（1 試合＝seed ごとに 4 本の TSV）：
//   trbin_s<seed>.tsv     年×（陸直行／海経由）×（国内／越境）×（距離の帯 8）×（財の重さの帯 5）：成約の件数・積出し価格の額・重さ（数量×輸送重量）・
//                         実の km を掛けた重さ（陸脚／海脚に分けて）・運賃の換算 km を掛けた重さ・払われた運賃（陸脚の分／海脚の分に分けて）。成約 1 件ごとに足す＝清算の行の全数。
//   trroute_s<seed>.tsv   経路（売り手の州×買い手の州）ごと＝10 年ごとの年の終わり（年の数字が 10 の倍数）の四半期。額の大きい上位 3000 組＋残りの合計の 1 行。
//                         距離（海脚・陸脚・運賃の換算）・両端の地形と地形の係数・陸÷海の比・その年の成約の額・重さ・運賃。
//   trnation_s<seed>.tsv  年×国：その国の買い手が払った運賃（うち陸脚の分・うち自国の売り手との成約の分）と、その国が世界の船腹の比で受け取った運賃＝§2-7 の不具合の大きさ。
//   trstat_s<seed>.tsv    scope 列で 4 種類：
//                           A＝州の面積（陸路用の陸マスクを 0.2° ごとに数え、一番近い州の座標へ割り当てる・構築で 1 回）と州の中の平均の運び距離 (2/3)√(A/π)。
//                           G＝州の断面（10 年ごとの年の終わり）：地形・人口・都市人口・工場と採取場の産出（参照価格×量）・産出の重さ。
//                           P＝年の終わりの世界の平均の財の値段（金建て）＋労働の賃金（人口加重・金建て）＋参照価格。
//                           Z＝運賃と地形に関わる係数の値（開幕に 1 回）。
// ⚠限界（計器が自分で申告する）：
//   ・運賃の陸脚と海脚の分け方は換算 km の比（運賃は換算 km に比例する＝BilateralMarket の frA）。陸脚の換算 km は PairKm の式を hook の側で書き直した物（地形×陸÷海の比を含む）＝
//     式が変われば食い違う。番人で〈海脚＋陸脚の換算 km が成約の換算 km に合う〉を確かめる。
//   ・重さ＝数量×Catalog.TransportWeight（相対の単位・トンではない）。財の単位がトンに換算できる保証は無い。
//   ・州の面積は 0.2° 格子の標本（誤差は数 %）で、陸の格子点を州の座標へ最近傍で割り当てる＝州の座標が疎な地域（シベリア・サハラ）は大きく出る。列 areaWithin1000KmKm2 は州の座標から 1000 km 以内の格子点だけを数える。
//   ・(385) 直した：面積（A 行）は開幕の 1 回に加えて、州の断面（G 行）と同じ年の終わり（10 年ごと）にその年の州（Active）で取り直す。旧（380）は開幕の 1 回だけで、
//     後から現れた州（建国・昇格・植民）には面積の行が無く、その土地は開幕の時点の隣の州に吸われていた（反証役 tr〔v〕低#3）。開幕の A 行も Active の州だけで分ける（旧は全 Regions）。
//   ・(385) 直した：陸脚を売り手側と買い手側に分けて記帳する（RealLandSellerKm／RealLandBuyerKm／EqLandSeller／EqLandBuyer）。和の列（EqLand・FreightLand）は残す（反証役 tr〔v〕中#1）。
//     海経由は PairKm の式の二つの項そのまま（売り手の州から港まで・港から買い手の州まで）。陸直行は PairKm の式が両端の係数の平均なので、経路の実 km を半分ずつ両端へ割り、
//     換算は各端の係数で掛ける近似（途中の州・第三国を通る分は国で追わない＝越境の陸直行の売り手側は売り手の国の中にあると置く）。
//     ⚠(387) 反証役 tf〔v〕中：海経由でも、内陸州が外国の港（通過港 TransitPortName・BilateralMarket の通過港の分岐）を使う成約では、売り手側の陸脚は
//     港の持ち主の国を通る。これも売り手の国（買い手側は買い手の国）へ、その国の船腹の比で積む近似（自国の港 NearestPortName の成約だけが正確）。
//     直すなら r.LastTransitPortName の持ち主の国で割る＝段1 の払い先の直しで陸直行の第三国と一緒に決める（設計 doc §7-4）。
//   ・(385) 〈陸脚の運賃のうち、その陸脚がある州の国の運び手へ戻る額〉（trnation の freightLandInNationToOwnCarriersGoldPerYear）は成約ごとに
//     〔売り手側の運賃×売り手の国の船腹の比〕＋〔買い手側の運賃×買い手の国の船腹の比〕を足した額（船腹の比はその清算の BilateralMarket.shipShare＝運賃の配りと同じ値）＝時代×種の和の近似ではない。
//   ・緯度が −60 度より南・80 度より北の格子点は数えない（人の住まない地域・計器の定数）。
//   ・成約が無い経路は出ない。上位 3000 組の外は 1 行の合計にまとめる。
public static partial class Instruments
{
    /// <summary>経路の分解（hook が BilateralMarket の側で PairKm の式から書き直す）。海経由＝海脚 <see cref="RealSeaKm"/>＋両端の陸脚 <see cref="RealLandKm"/>（売り手側と買い手側の和）・
    /// 陸直行＝陸グラフの最短路＋接ぎ木の km の 1 本。換算 km＝<see cref="EqSea"/>＋<see cref="EqLand"/>（PairKm の戻り値＝<see cref="EqBook"/> に合う）。
    /// <see cref="EqBook"/>＝運賃の計算に使われた換算 km（BilateralMarket の bestKmA）＝分解の突き合わせ先。</summary>
    internal struct TrRoute
    {
        public bool Sea;
        public double EqBook, RealSeaKm, RealLandKm, EqSea, EqLand, TerrSeller, TerrBuyer, RatioSeller, RatioBuyer;
        /// <summary>(385) 陸脚の売り手側・買い手側（和は <see cref="RealLandKm"/>／<see cref="EqLand"/>）。陸直行は実 km を半分ずつ・換算は各端の係数で。</summary>
        public double RealLandSellerKm, RealLandBuyerKm, EqLandSeller, EqLandBuyer;
    }

    /// <summary>距離の帯の境（実の km の合計）。帯 0＝50 未満 … 帯 7＝12000 以上。計器の定数。</summary>
    internal static readonly double[] TrDistEdges = [50, 150, 400, 1000, 2500, 6000, 12000];
    /// <summary>財の重さの帯の境（輸送重量）。0.4・1.0・2.5・3.0・6.0 の 5 値の中間。計器の定数。</summary>
    internal static readonly double[] TrWeightEdges = [0.7, 1.5, 2.75, 4.5];
    static int TrBand(double[] edges, double x) { int b = 0; while (b < edges.Length && x >= edges[b]) b++; return b; }

    sealed class TrBin
    {
        public long Deals;
        public double Value, Weight, WKmReal, WKmLandReal, WKmSeaReal, WKmEq, WKmEqLand, WKmEqSea, Freight, FreightLand, FreightSea;
        public double FreightLandSeller, FreightLandBuyer, WKmRealLandSeller, WKmRealLandBuyer, WKmEqLandSeller, WKmEqLandBuyer;   // (385) 陸脚の売り手側・買い手側
    }
    sealed class TrPair
    {
        public string SellerNation = "", BuyerNation = "";
        public TrRoute Route;
        public long Deals; public double Value, Weight, Freight, FreightLand;
        public double FreightLandSeller, FreightLandBuyer, ValueEqLandSeller, ValueEqLandBuyer;   // (385) 陸脚の売り手側・買い手側
        public double ValueSea, ValueEqBook, ValueEqSea, ValueEqLand;   // 成約の額で重みを付けた換算 km の和（凍る港で経路が季節ごとに変わる組があるので、運賃の突き合わせは最後の成約の km でなくこの和で行う）
    }
    sealed class TrNat
    {
        public double Paid, PaidLand, PaidDomestic, PaidLandDomestic, Received, WKmReal;
        public double LandSellerSideIn, LandBuyerSideIn, LandInToOwnCarriers;   // (385) その国の中にある陸脚の運賃（売り手側は売り手の国・買い手側は買い手の国へ積む）と、うち同じ国の運び手へ戻る額
    }
    /// <summary>距離の帯×重さの帯×（陸／海）×（国内／越境）の組の数＝帯の表の長さ（鍵は <see cref="TradeDeal"/> の key）。</summary>
    const int TrBinCount = 2 * 2 * 8 * 5;
    sealed class TrAcc
    {
        public readonly TrBin?[] Bins = new TrBin?[TrBinCount];   // ★2026-10-17（開発の高速化）：旧 Dictionary<int, TrBin>＝成約ごとの表引き。書き出しは鍵の順（旧の OrderBy と同じ）
        public int BinsUsed;
        public readonly Dictionary<(Region S, Region B), TrPair> Pairs = new();
        public readonly Dictionary<string, TrNat> Nats = new();
        public int FoldedQuarters;
        // ★2026-10-17：直前の成約の買い手・売り手の国の行（清算は買い手の行ごとに回る＝同じ国が続く）。名前の文字列が同じ物のときだけ使う＝外れれば表を引く（結果は同じ）
        public string? LastBName, LastSName; public TrNat? LastB, LastS;
        public void Clear() { Array.Clear(Bins); BinsUsed = 0; Pairs.Clear(); Nats.Clear(); LastBName = LastSName = null; LastB = LastS = null; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, TrAcc> TrAccs = new();
    [ThreadStatic] static InternationalMarket? trLastM;   // ★2026-10-17：直前の市場とその蓄え（バッチは試合ごとに別のスレッド・1試合の清算は1本のスレッド）
    [ThreadStatic] static TrAcc? trLastA;
    static TrAcc TrOf(InternationalMarket m)
    {
        if (ReferenceEquals(m, trLastM) && trLastA is not null) return trLastA;
        var a = TrAccs.GetValue(m, _ => new TrAcc());
        trLastM = m; trLastA = a;
        return a;
    }
    static TrNat TrNatOf(TrAcc a, string name) { if (!a.Nats.TryGetValue(name, out var n)) a.Nats[name] = n = new TrNat(); return n; }

    /// <summary>成約 1 件（<c>BilateralMarket</c> の記帳の位置・読むだけ）。<paramref name="freight"/>＝その成約の運賃（金建て・<c>freightPool</c> へ足す額）・<paramref name="weight"/>＝財 1 単位の輸送重量。</summary>
    internal static void TradeDeal(InternationalMarket m, World buyer, World seller, Region sellerR, Region buyerR, double weight, double qty, double value, double freight, in TrRoute rt, bool cross, double shipShareSeller, double shipShareBuyer)
    {
        var a = TrOf(m);
        double w = qty * weight, eq = rt.EqSea + rt.EqLand;
        double frLand = eq > 0 ? freight * rt.EqLand / eq : 0.0;
        double frLandS = eq > 0 ? freight * rt.EqLandSeller / eq : 0.0, frLandB = eq > 0 ? freight * rt.EqLandBuyer / eq : 0.0;   // (385) 陸脚の運賃を売り手側・買い手側へ（換算 km の比）
        double realKm = rt.RealSeaKm + rt.RealLandKm;
        int key = (((rt.Sea ? 1 : 0) * 2 + (cross ? 1 : 0)) * (TrDistEdges.Length + 1) + TrBand(TrDistEdges, realKm)) * (TrWeightEdges.Length + 1) + TrBand(TrWeightEdges, weight);
        var b = a.Bins[key];
        if (b is null) { a.Bins[key] = b = new TrBin(); a.BinsUsed++; }
        b.Deals++; b.Value += value; b.Weight += w;
        b.WKmReal += w * realKm; b.WKmLandReal += w * rt.RealLandKm; b.WKmSeaReal += w * rt.RealSeaKm;
        b.WKmEq += w * eq; b.WKmEqLand += w * rt.EqLand; b.WKmEqSea += w * rt.EqSea;
        b.Freight += freight; b.FreightLand += frLand; b.FreightSea += freight - frLand;
        b.FreightLandSeller += frLandS; b.FreightLandBuyer += frLandB; b.WKmRealLandSeller += w * rt.RealLandSellerKm; b.WKmRealLandBuyer += w * rt.RealLandBuyerKm; b.WKmEqLandSeller += w * rt.EqLandSeller; b.WKmEqLandBuyer += w * rt.EqLandBuyer;
        string bName = NameOf(buyer);
        var nb = ReferenceEquals(bName, a.LastBName) && a.LastB is not null ? a.LastB : (a.LastB = TrNatOf(a, a.LastBName = bName));
        nb.Paid += freight; nb.PaidLand += frLand; nb.WKmReal += w * realKm;
        if (!cross) { nb.PaidDomestic += freight; nb.PaidLandDomestic += frLand; }
        string sName = NameOf(seller);
        var ns = ReferenceEquals(sName, a.LastSName) && a.LastS is not null ? a.LastS : (a.LastS = TrNatOf(a, a.LastSName = sName));   // (385) 売り手側の陸脚は売り手の国の中・買い手側の陸脚は買い手の国の中。戻る額＝その国の船腹の比（清算の運賃の配りと同じ値）
        ns.LandSellerSideIn += frLandS; ns.LandInToOwnCarriers += frLandS * shipShareSeller;
        nb.LandBuyerSideIn += frLandB; nb.LandInToOwnCarriers += frLandB * shipShareBuyer;
        if (!a.Pairs.TryGetValue((sellerR, buyerR), out var p)) a.Pairs[(sellerR, buyerR)] = p = new TrPair { SellerNation = NameOf(seller), BuyerNation = NameOf(buyer) };
        p.Route = rt; p.Deals++; p.Value += value; p.Weight += w; p.Freight += freight; p.FreightLand += frLand;
        p.FreightLandSeller += frLandS; p.FreightLandBuyer += frLandB; p.ValueEqLandSeller += value * rt.EqLandSeller; p.ValueEqLandBuyer += value * rt.EqLandBuyer;
        if (rt.Sea) p.ValueSea += value;
        p.ValueEqBook += value * rt.EqBook; p.ValueEqSea += value * rt.EqSea; p.ValueEqLand += value * rt.EqLand;
    }

    /// <summary>清算の終わりに世界の船腹の比で配った運賃（金建て・国ごと）。<c>BilateralMarket</c> の <c>World.Transfer</c> の直前・読むだけ。</summary>
    internal static void TradeFreightReceived(InternationalMarket m, World owner, double gold) => TrNatOf(TrOf(m), NameOf(owner)).Received += gold;

    /// <summary>番人用：この試合の市場に蓄えがあるか（計器を切っている間は作られない＝hook が 1 命令も走らない）。</summary>
    internal static bool TrAccExists(MultiNationSim sim) => TrAccs.TryGetValue(sim.Market, out _);

    /// <summary>助走の直後：助走の月の成約を数えない。</summary>
    internal static void TrClear(MultiNationSim sim) { if (TrAccs.TryGetValue(sim.Market, out var a)) a.Clear(); }

    // ---------------------------------------------------------------- 書き出し（Quarter の末尾から） ----------------------------------------------------------------

    /// <summary>経路の行と州の断面の行を出す年の間隔（年の数字がこの数の倍数の年の終わり）。既定 10。番人だけが 1 にする。</summary>
    internal static int TrSnapEveryYears = 10;

    const string TrBinHeader = "year\trouteCode\tcrossBorderFlag\tdistanceBandCode\tweightBandCode\tdealsCountPerYear\tvalueGoldPerYear\tweightUnitsPerYear\tweightKmRealPerYear\tweightKmLandRealPerYear\tweightKmSeaRealPerYear\t"
        + "weightKmEqPerYear\tweightKmEqLandPerYear\tweightKmEqSeaPerYear\tfreightGoldPerYear\tfreightLandPartGoldPerYear\tfreightSeaPartGoldPerYear\t"
        + "freightLandSellerSidePartGoldPerYear\tfreightLandBuyerSidePartGoldPerYear\tweightKmRealLandSellerSidePerYear\tweightKmRealLandBuyerSidePerYear\tweightKmEqLandSellerSidePerYear\tweightKmEqLandBuyerSidePerYear";
    const string TrRouteHeader = "year\tsellerRegion\tbuyerRegion\tsellerNation\tbuyerNation\tsellerTerrainCode\tbuyerTerrainCode\trouteCode\trealSeaKm\trealLandKm\teqSeaKm\teqLandKm\teqBookKm\tterrainCostSeller\tterrainCostBuyer\t"
        + "landSeaRatioSeller\tlandSeaRatioBuyer\tdealsCountPerYear\tvalueGoldPerYear\tweightUnitsPerYear\tfreightGoldPerYear\tfreightLandPartGoldPerYear\tvalueSeaRouteGoldPerYear\tvalueTimesEqBookKmGoldPerYear\tvalueTimesEqSeaKmGoldPerYear\tvalueTimesEqLandKmGoldPerYear\t"
        + "realLandSellerSideKm\trealLandBuyerSideKm\teqLandSellerSideKm\teqLandBuyerSideKm\tfreightLandSellerSidePartGoldPerYear\tfreightLandBuyerSidePartGoldPerYear\tvalueTimesEqLandSellerSideKmGoldPerYear\tvalueTimesEqLandBuyerSideKmGoldPerYear";
    const string TrNatHeader = "year\tnation\trailGateFlag\tfreightPaidGoldPerYear\tfreightPaidLandPartGoldPerYear\tfreightPaidDomesticGoldPerYear\tfreightPaidLandPartDomesticGoldPerYear\tfreightReceivedGoldPerYear\tweightKmRealPerYear\t"
        + "freightLandSellerSideInNationGoldPerYear\tfreightLandBuyerSideInNationGoldPerYear\tfreightLandInNationToOwnCarriersGoldPerYear";
    const string TrStatHeader = "scope\tyear\tnation\tregion\tterrainCode\tlat\tlon\tareaKm2\tareaWithin1000KmKm2\tmeanHaulKm\tmeanHaulWithin1000KmKm\tportFlag\tpopulationK\turbanPopulationK\t"
        + "factoryOutputValuePerMonth\tgatherOutputValuePerMonth\toutputWeightPerMonth\tname\tvalue\trefValue";

    static string TrI(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    static void TransportFold(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        var a = TrOf(sim.Market);
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        a.FoldedQuarters++;
        if (a.FoldedQuarters == 1) TrStatFirst(sk, seed, sim);
        if (!yearEnd) return;
        FlushTransport(sk, seed, sim);
    }

    /// <summary>年の終わり（と試合の終わりの残り）：蓄えを行にして書き、0 へ戻す。</summary>
    static void FlushTransport(Sink sk, ulong seed, MultiNationSim sim)
    {
        var a = TrOf(sim.Market);
        int year = sim.Nations[0].World.Year;
        sk.TrBin ??= Open("trbin", seed); sk.TrRoute ??= Open("trroute", seed); sk.TrNat ??= Open("trnation", seed); sk.TrStat ??= Open("trstat", seed);
        if (!sk.TrHeader) { sk.TrBin.WriteLine(TrBinHeader); sk.TrRoute.WriteLine(TrRouteHeader); sk.TrNat.WriteLine(TrNatHeader); sk.TrStat.WriteLine(TrStatHeader); sk.TrHeader = true; }
        for (int k = 0; k < a.Bins.Length; k++)
        {
            var b = a.Bins[k]; if (b is null) continue;
            int nw = TrWeightEdges.Length + 1, nd = TrDistEdges.Length + 1;
            int wb = k % nw, db = k / nw % nd, cross = k / (nw * nd) % 2, sea = k / (nw * nd * 2);
            sk.TrBin.WriteLine(string.Join('\t', TrI(year), TrI(sea), TrI(cross), TrI(db), TrI(wb), b.Deals.ToString(System.Globalization.CultureInfo.InvariantCulture), F(b.Value), F(b.Weight), F(b.WKmReal), F(b.WKmLandReal), F(b.WKmSeaReal),
                F(b.WKmEq), F(b.WKmEqLand), F(b.WKmEqSea), F(b.Freight), F(b.FreightLand), F(b.FreightSea),
                F(b.FreightLandSeller), F(b.FreightLandBuyer), F(b.WKmRealLandSeller), F(b.WKmRealLandBuyer), F(b.WKmEqLandSeller), F(b.WKmEqLandBuyer)));
        }
        foreach (var kv in a.Nats.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var n = kv.Value; World? w = null;
            foreach (var sn in sim.Nations) if (NameOf(sn.World) == kv.Key) { w = sn.World; break; }
            sk.TrNat.WriteLine(string.Join('\t', TrI(year), kv.Key, w is null ? "nan" : Discovery.RailTransport(w.Discovered) ? "1" : "0", F(n.Paid), F(n.PaidLand), F(n.PaidDomestic), F(n.PaidLandDomestic), F(n.Received), F(n.WKmReal),
                F(n.LandSellerSideIn), F(n.LandBuyerSideIn), F(n.LandInToOwnCarriers)));
        }
        if (year % TrSnapEveryYears == 0)
        {
            var top = a.Pairs.OrderByDescending(k => k.Value.Value).ThenBy(k => k.Key.S.Name, StringComparer.Ordinal).ThenBy(k => k.Key.B.Name, StringComparer.Ordinal).ToList();
            double restDeals = 0, restValue = 0, restWeight = 0, restFreight = 0, restFreightLand = 0, restVs = 0, restVb = 0, restVsea = 0, restVland = 0, restFls = 0, restFlb = 0, restVls = 0, restVlb = 0;
            for (int i = 0; i < top.Count; i++)
            {
                var (key, p) = (top[i].Key, top[i].Value);
                if (i >= 3000) { restDeals += p.Deals; restValue += p.Value; restWeight += p.Weight; restFreight += p.Freight; restFreightLand += p.FreightLand; restVs += p.ValueSea; restVb += p.ValueEqBook; restVsea += p.ValueEqSea; restVland += p.ValueEqLand; restFls += p.FreightLandSeller; restFlb += p.FreightLandBuyer; restVls += p.ValueEqLandSeller; restVlb += p.ValueEqLandBuyer; continue; }
                var rt = p.Route;
                sk.TrRoute.WriteLine(string.Join('\t', TrI(year), key.S.Name, key.B.Name, p.SellerNation, p.BuyerNation, TrI((int)key.S.Terrain), TrI((int)key.B.Terrain), rt.Sea ? "1" : "0",
                    F(rt.RealSeaKm), F(rt.RealLandKm), F(rt.EqSea), F(rt.EqLand), F(rt.EqBook), F(rt.TerrSeller), F(rt.TerrBuyer), F(rt.RatioSeller), F(rt.RatioBuyer),
                    p.Deals.ToString(System.Globalization.CultureInfo.InvariantCulture), F(p.Value), F(p.Weight), F(p.Freight), F(p.FreightLand), F(p.ValueSea), F(p.ValueEqBook), F(p.ValueEqSea), F(p.ValueEqLand),
                    F(rt.RealLandSellerKm), F(rt.RealLandBuyerKm), F(rt.EqLandSeller), F(rt.EqLandBuyer), F(p.FreightLandSeller), F(p.FreightLandBuyer), F(p.ValueEqLandSeller), F(p.ValueEqLandBuyer)));
            }
            if (top.Count > 3000)
                sk.TrRoute.WriteLine(string.Join('\t', TrI(year), "(上位3000組の外の合計)", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", F(restDeals), F(restValue), F(restWeight), F(restFreight), F(restFreightLand), F(restVs), F(restVb), F(restVsea), F(restVland), "", "", "", "", F(restFls), F(restFlb), F(restVls), F(restVlb)));
            TrAreaRows(sk, sim, year);   // (385) 州の面積をその年の州で取り直す（G 行と同じ年）
            TrRegionRows(sk, sim, year);
        }
        TrPriceRows(sk, sim, year);
        a.Clear();
    }

    /// <summary>試合の終わり：年の途中で終わった残りを書く（四半期の数が 4 の倍数なら何も残らない）。</summary>
    static void TransportClose(Sink sk, MultiNationSim sim)
    {
        if (!TrAccs.TryGetValue(sim.Market, out var a)) return;
        if (a.BinsUsed > 0 || a.Nats.Count > 0) FlushTransport(sk, sk.Seed, sim);
    }

    // ---------------------------------------------------------------- 静的な表（面積・係数） ----------------------------------------------------------------

    static void TrStatFirst(Sink sk, ulong seed, MultiNationSim sim)
    {
        sk.TrStat ??= Open("trstat", seed); sk.TrBin ??= Open("trbin", seed); sk.TrRoute ??= Open("trroute", seed); sk.TrNat ??= Open("trnation", seed);
        if (!sk.TrHeader) { sk.TrBin.WriteLine(TrBinHeader); sk.TrRoute.WriteLine(TrRouteHeader); sk.TrNat.WriteLine(TrNatHeader); sk.TrStat.WriteLine(TrStatHeader); sk.TrHeader = true; }
        int year = sim.Nations[0].World.Year;
        TrAreaRows(sk, sim, year);
        var c = sim.Nations[0].World.Coef;
        void Z(string name, double v) => sk.TrStat!.WriteLine(string.Join('\t', "Z", TrI(year), "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", name, F(v), ""));
        Z("LandSeaCostRatio", c.LandSeaCostRatio); Z("RailCostFraction", c.RailCostFraction); Z("FreightRatePer10Mm", c.FreightRatePer10Mm);
        Z("TransportMargin", c.TransportMargin); Z("DistanceMarginScale", c.DistanceMarginScale); Z("TransportWeightMarginScale", c.TransportWeightMarginScale);
        Z("MerchantMargin", c.MerchantMargin); Z("TerrainWaterMajorCost", c.TerrainWaterMajorCost); Z("TerrainWaterMinorCost", c.TerrainWaterMinorCost);
        Z("DistanceNormKm", Diplomacy.DistanceNormKm);
        for (int t = 0; t < c.TerrainHorseCost.Length; t++) Z("TerrainHorseCost[" + ((Terrain)t) + "]", c.TerrainHorseCost[t]);
        for (int t = 0; t < c.TerrainRailCost.Length; t++) Z("TerrainRailCost[" + ((Terrain)t) + "]", c.TerrainRailCost[t]);
    }

    /// <summary>(385) 州の面積の行（scope A）：その年の Active の州だけで陸マスクの格子を分ける（G 行と同じ州の集合）。開幕の 1 回と、10 年ごとの年の終わりに書く。座標の並びが同じ年は <see cref="AreaCache"/> に当たる。</summary>
    static void TrAreaRows(Sink sk, MultiNationSim sim, int year)
    {
        var regs = new List<(string Nation, Region R)>();
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) if (r.State == RegionState.Active) regs.Add((NameOf(n.World), r));
        var pts = new (double Lat, double Lon)[regs.Count];
        for (int i = 0; i < regs.Count; i++) pts[i] = Climate.CityCoord(regs[i].R.Name);
        var (area, areaNear) = TrAreas(pts);
        for (int i = 0; i < regs.Count; i++)
        {
            var r = regs[i].R;
            sk.TrStat!.WriteLine(string.Join('\t', "A", TrI(year), regs[i].Nation, r.Name, TrI((int)r.Terrain), F(pts[i].Lat), F(pts[i].Lon), F(area[i]), F(areaNear[i]), F(MeanHaul(area[i])), F(MeanHaul(areaNear[i])),
                r.IsPort ? "1" : "0", "", "", "", "", "", "", "", ""));
        }
    }

    static double MeanHaul(double areaKm2) => areaKm2 > 0 ? 2.0 / 3.0 * Math.Sqrt(areaKm2 / Math.PI) : double.NaN;

    static readonly Dictionary<long, (double[] All, double[] Near)> AreaCache = new();
    const double TrAreaStepDeg = 0.2, TrAreaNearKm = 1000.0, TrAreaLatMin = -60.0, TrAreaLatMax = 80.0, KmPerDeg = 111.195;

    /// <summary>陸路用の陸マスクの格子点（0.2° ごと）を一番近い州の座標へ割り当て、面積（km²）を数える。同じ座標の州は等分。座標が (0,0)（未登録）の州は NaN。</summary>
    internal static (double[] All, double[] Near) TrAreas((double Lat, double Lon)[] pts)
    {
        long sig = 1469598103934665603L;
        foreach (var p in pts) { sig = (sig ^ BitConverter.DoubleToInt64Bits(p.Lat)) * 1099511628211L; sig = (sig ^ BitConverter.DoubleToInt64Bits(p.Lon)) * 1099511628211L; }
        lock (AreaCache) if (AreaCache.TryGetValue(sig, out var hit)) return hit;
        var uniq = new List<(double Lat, double Lon)>(); var uid = new int[pts.Length]; var members = new List<int>();
        var seen = new Dictionary<(double, double), int>();
        for (int i = 0; i < pts.Length; i++)
        {
            if (pts[i].Lat == 0 && pts[i].Lon == 0) { uid[i] = -1; continue; }
            if (!seen.TryGetValue(pts[i], out int u)) { u = uniq.Count; seen[pts[i]] = u; uniq.Add(pts[i]); members.Add(0); }
            uid[i] = u; members[u]++;
        }
        var ua = new double[uniq.Count]; var un = new double[uniq.Count];
        int nx = (int)Math.Round(360.0 / TrAreaStepDeg), ny = (int)Math.Round(180.0 / TrAreaStepDeg);
        double cellDeg = TrAreaStepDeg;
        // ★2026-10-17（開発の高速化・結果は旧の総当たりと同じ）：州を緯度の順に並べ、格子点の緯度から外へ広げて探す。d2 は旧と同じ式・同じ距離なら番号の小さい州
        //   （旧＝番号の順に狭義の < で最初の最小）。d2 ≥ dy² なので、dy² が今の最小を超えた側はそこで打ち切る（並びの外へ行くほど |dy| は単調に増える）。
        int nu = uniq.Count;
        var ord = new int[nu]; for (int u = 0; u < nu; u++) ord[u] = u;
        Array.Sort(ord, (p, q) => uniq[p].Lat != uniq[q].Lat ? uniq[p].Lat.CompareTo(uniq[q].Lat) : p.CompareTo(q));
        var oLat = new double[nu]; var oLon = new double[nu];
        for (int k = 0; k < nu; k++) { oLat[k] = uniq[ord[k]].Lat; oLon[k] = uniq[ord[k]].Lon; }
        for (int yi = 0; yi < ny; yi++)
        {
            double lat = 90.0 - (yi + 0.5) * cellDeg;
            if (lat < TrAreaLatMin || lat > TrAreaLatMax) continue;
            double cosLat = Math.Cos(lat * Math.PI / 180.0);
            double cellKm2 = cellDeg * KmPerDeg * cellDeg * KmPerDeg * cosLat;
            int lo0 = 0, hi0 = nu;   // 緯度が lat 以上の最初の位置
            while (lo0 < hi0) { int mid = (lo0 + hi0) >> 1; if (oLat[mid] < lat) lo0 = mid + 1; else hi0 = mid; }
            for (int xi = 0; xi < nx; xi++)
            {
                double lon = -180.0 + (xi + 0.5) * cellDeg;
                if (!Landmask.IsLandForRoads(lat, lon)) continue;
                int best = -1; double bd = double.MaxValue;
                int up = lo0, dn = lo0 - 1;
                while (up < nu || dn >= 0)
                {
                    double dyU = up < nu ? lat - oLat[up] : 0.0, dyD = dn >= 0 ? lat - oLat[dn] : 0.0;
                    bool takeUp = dn < 0 || (up < nu && dyU * dyU <= dyD * dyD);
                    int k = takeUp ? up : dn; double dy = takeUp ? dyU : dyD;
                    if (dy * dy > bd) { if (takeUp) up = nu; else dn = -1; continue; }
                    if (takeUp) up++; else dn--;
                    double dl = Math.Abs(lon - oLon[k]); if (dl > 180.0) dl = 360.0 - dl;
                    double dx = dl * cosLat;
                    double d2 = dy * dy + dx * dx;
                    int u = ord[k];
                    if (d2 < bd || (d2 == bd && u < best)) { bd = d2; best = u; }
                }
                if (best < 0) continue;
                ua[best] += cellKm2;
                if (Math.Sqrt(bd) * KmPerDeg <= TrAreaNearKm) un[best] += cellKm2;
            }
        }
        var all = new double[pts.Length]; var near = new double[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            if (uid[i] < 0) { all[i] = near[i] = double.NaN; continue; }
            all[i] = ua[uid[i]] / members[uid[i]]; near[i] = un[uid[i]] / members[uid[i]];
        }
        lock (AreaCache) AreaCache[sig] = (all, near);
        return (all, near);
    }

    // ---------------------------------------------------------------- 州の断面と世界の値段 ----------------------------------------------------------------

    static void TrRegionRows(Sink sk, MultiNationSim sim, int year)
    {
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = NameOf(w);
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double pop = 0, urban = 0;
                foreach (var p in r.Pops) { pop += p.Size; if (p.IsUrban) urban += p.Size; }
                double fac = 0, gat = 0, wt = 0;
                foreach (var pr in r.Producers)
                {
                    var good = r.GoodOf(pr.GoodId);
                    if (good is null || good.Tier == GoodTier.Service) continue;
                    double o = pr.Output;
                    if (pr.IsRgo) gat += o * good.RefPrice; else fac += o * good.RefPrice;
                    wt += o * Catalog.TransportWeight(pr.GoodId);
                }
                var cc = Climate.CityCoord(r.Name);
                sk.TrStat!.WriteLine(string.Join('\t', "G", TrI(year), nation, r.Name, TrI((int)r.Terrain), F(cc.Lat), F(cc.Lon), "", "", "", "", r.IsPort ? "1" : "0", F(pop), F(urban), F(fac), F(gat), F(wt), "", "", ""));
            }
        }
    }

    /// <summary>運賃の見積もりの材料の財（名前で引く）。馬・飼料・石炭・線路・機関車・帆船・汽船・原油・内燃機関・自動車・鋼・製材。</summary>
    static readonly int[] TrPriceGoods = [Catalog.HorseId, Catalog.FodderId, Catalog.CoalId, Catalog.RailId, Catalog.LocomotiveId, Catalog.SailingShipId, Catalog.SteamshipId,
        Catalog.CrudeOilId, Catalog.CombustionEngineId, Catalog.AutomobileId, Catalog.SteelId, Catalog.LumberId];

    static void TrPriceRows(Sink sk, MultiNationSim sim, int year)
    {
        var px = sim.Market.Price;
        foreach (int g in TrPriceGoods)
            sk.TrStat!.WriteLine(string.Join('\t', "P", TrI(year), "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", Catalog.Goods[g].Name, F(g < px.Count ? px[g] : double.NaN), F(Catalog.RefPrices[g])));
        foreach (int g in new[] { Catalog.LaborId, Catalog.AdvLaborId })
        {
            double num = 0, den = 0;
            foreach (var n in sim.Nations)
            {
                var w = n.World; double fx = w.FxRate;
                foreach (var r in w.Regions)
                {
                    if (r.State != RegionState.Active || g >= r.Markets.Length || r.Markets[g] is not { } mk) continue;
                    double sz = 0; foreach (var p in r.Pops) sz += p.Size;
                    if (!(sz > 0) || !(mk.Price > 0)) continue;
                    num += sz * mk.Price * fx; den += sz;
                }
            }
            sk.TrStat!.WriteLine(string.Join('\t', "P", TrI(year), "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", Catalog.Goods[g].Name, F(den > 0 ? num / den : double.NaN), F(Catalog.RefPrices[g])));
        }
    }
}
