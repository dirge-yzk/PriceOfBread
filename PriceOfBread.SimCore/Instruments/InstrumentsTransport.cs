namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない （蓄えは InternationalMarket の外＝ConditionalWeakTable・セーブに載らない）。〔履歴 InstrumentsTransport.cs#1〕
public static partial class Instruments
{
    /// <summary>経路の分解（hook が BilateralMarket の側で PairKm の式から書き直す）。海経由＝海脚 <see cref="RealSeaKm"/>＋両端の陸脚 <see cref="RealLandKm"/>（売り手側と買い手側の和）・
    /// 陸直行＝陸グラフの最短路＋接ぎ木の km の 1 本。換算 km＝<see cref="EqSea"/>＋<see cref="EqLand"/>（PairKm の戻り値＝<see cref="EqBook"/> に合う）。
    /// <see cref="EqBook"/>＝運賃の計算に使われた換算 km（BilateralMarket の bestKmA）＝分解の突き合わせ先。</summary>
    internal struct TrRoute
    {
        public bool Sea;
        public double EqBook, RealSeaKm, RealLandKm, EqSea, EqLand, TerrSeller, TerrBuyer, RatioSeller, RatioBuyer;
        /// <summary>
        /// 陸直行は実 km を半分ずつ・換算は各端の係数で。〔履歴 InstrumentsTransport.cs#2〕
        /// </summary>
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
        public double FreightLandSeller, FreightLandBuyer, WKmRealLandSeller, WKmRealLandBuyer, WKmEqLandSeller, WKmEqLandBuyer;   // WKmEqLandBuyer〔履歴 InstrumentsTransport.cs#3〕
    }
    sealed class TrPair
    {
        public string SellerNation = "", BuyerNation = "";
        public TrRoute Route;
        public long Deals; public double Value, Weight, Freight, FreightLand;
        public double FreightLandSeller, FreightLandBuyer, ValueEqLandSeller, ValueEqLandBuyer;   // ValueEqLandBuyer〔履歴 InstrumentsTransport.cs#4〕
        public double ValueSea, ValueEqBook, ValueEqSea, ValueEqLand;   // 成約の額で重みを付けた換算 km の和（凍る港で経路が季節ごとに変わる組があるので、運賃の突き合わせは最後の成約の km でなくこの和で行う）
    }
    sealed class TrNat
    {
        public double Paid, PaidLand, PaidDomestic, PaidLandDomestic, Received, WKmReal;
        public double LandSellerSideIn, LandBuyerSideIn, LandInToOwnCarriers;   // LandInToOwnCarriers〔履歴 InstrumentsTransport.cs#5〕
    }
    /// <summary>距離の帯×重さの帯×（陸／海）×（国内／越境）の組の数＝帯の表の長さ（鍵は <see cref="TradeDeal"/> の key）。</summary>
    const int TrBinCount = 2 * 2 * 8 * 5;
    sealed class TrAcc
    {
        public readonly TrBin?[] Bins = new TrBin?[TrBinCount];   // 書き出しは鍵の順（旧の OrderBy と同じ）〔履歴 InstrumentsTransport.cs#6〕
        public int BinsUsed;
        public readonly Dictionary<(Region S, Region B, string SN, string BN), TrPair> Pairs = new();   // Pairs〔履歴 InstrumentsTransport.cs#7〕
        public readonly Dictionary<string, TrNat> Nats = new();
        public int FoldedQuarters;
        // 名前の文字列が同じ物のときだけ使う＝外れれば表を引く（結果は同じ）〔履歴 InstrumentsTransport.cs#8〕
        public string? LastBName, LastSName; public TrNat? LastB, LastS;
        public void Clear() { Array.Clear(Bins); BinsUsed = 0; Pairs.Clear(); Nats.Clear(); LastBName = LastSName = null; LastB = LastS = null; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, TrAcc> TrAccs = new();
    [ThreadStatic] static InternationalMarket? trLastM;   // trLastM〔履歴 InstrumentsTransport.cs#9〕
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
    internal static void TradeDeal(InternationalMarket m, World buyer, World seller, Region sellerR, Region buyerR, double weight, double qty, double value, double freight, in TrRoute rt, bool cross, double shipShareSeller, double shipShareBuyer, int good, double dutyT = 0.0, double dutyC = 0.0)
    {
        NgDeal(m, buyer, seller, good, qty, value, freight, cross, dutyT, dutyC);   // NgDeal〔履歴 InstrumentsTransport.cs#10〕
        var a = TrOf(m);
        double w = qty * weight, eq = rt.EqSea + rt.EqLand;
        double frLand = eq > 0 ? freight * rt.EqLand / eq : 0.0;
        double frLandS = eq > 0 ? freight * rt.EqLandSeller / eq : 0.0, frLandB = eq > 0 ? freight * rt.EqLandBuyer / eq : 0.0;   // frLandS〔履歴 InstrumentsTransport.cs#11〕
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
        var ns = ReferenceEquals(sName, a.LastSName) && a.LastS is not null ? a.LastS : (a.LastS = TrNatOf(a, a.LastSName = sName));   // 戻る額＝その国の船腹の比（清算の運賃の配りと同じ値）〔履歴 InstrumentsTransport.cs#12〕
        ns.LandSellerSideIn += frLandS; ns.LandInToOwnCarriers += frLandS * shipShareSeller;
        nb.LandBuyerSideIn += frLandB; nb.LandInToOwnCarriers += frLandB * shipShareBuyer;
        if (!a.Pairs.TryGetValue((sellerR, buyerR, sName, bName), out var p)) a.Pairs[(sellerR, buyerR, sName, bName)] = p = new TrPair { SellerNation = sName, BuyerNation = bName };
        p.Route = rt; p.Deals++; p.Value += value; p.Weight += w; p.Freight += freight; p.FreightLand += frLand;
        p.FreightLandSeller += frLandS; p.FreightLandBuyer += frLandB; p.ValueEqLandSeller += value * rt.EqLandSeller; p.ValueEqLandBuyer += value * rt.EqLandBuyer;
        if (rt.Sea) p.ValueSea += value;
        p.ValueEqBook += value * rt.EqBook; p.ValueEqSea += value * rt.EqSea; p.ValueEqLand += value * rt.EqLand;
        RailDealSide(m, seller, sellerR, rt.TerrSeller * rt.RatioSeller, rt.EqBook > 0 ? freight * rt.EqLandSeller / rt.EqBook : 0.0);   // RailDealSide〔履歴 InstrumentsTransport.cs#13〕
        RailDealSide(m, buyer, buyerR, rt.TerrBuyer * rt.RatioBuyer, rt.EqBook > 0 ? freight * rt.EqLandBuyer / rt.EqBook : 0.0);
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
        + "freightLandSellerSideInNationGoldPerYear\tfreightLandBuyerSideInNationGoldPerYear\tfreightLandInNationToOwnCarriersGoldPerYear\trailTargetIndex\trailFillShare\trailTargetNoThreatIndex";
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
                F(n.LandSellerSideIn), F(n.LandBuyerSideIn), F(n.LandInToOwnCarriers), RailNatCols(w)));
        }
        if (year % TrSnapEveryYears == 0)
        {
            var top = a.Pairs.OrderByDescending(k => k.Value.Value).ThenBy(k => k.Key.S.Name, StringComparer.Ordinal).ThenBy(k => k.Key.B.Name, StringComparer.Ordinal).ThenBy(k => k.Key.SN, StringComparer.Ordinal).ThenBy(k => k.Key.BN, StringComparer.Ordinal).ToList();
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
            TrAreaRows(sk, sim, year);   // TrAreaRows〔履歴 InstrumentsTransport.cs#14〕
            TrRegionRows(sk, sim, year);
        }
        TrPriceRows(sk, sim, year);
        TrRailRows(sk, sim, year);   // TrRailRows〔履歴 InstrumentsTransport.cs#15〕
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
        Z("TransportMargin", c.TransportMargin); Z("DistanceMarginScale", 1.0);   /* Z〔履歴 InstrumentsTransport.cs#16〕 */ Z("TransportWeightMarginScale", c.TransportWeightMarginScale);
        Z("MerchantMargin", c.MerchantMargin); Z("TerrainWaterMajorCost", c.TerrainWaterMajorCost); Z("TerrainWaterMinorCost", c.TerrainWaterMinorCost);
        Z("DistanceNormKm", Diplomacy.DistanceNormKm);
        for (int t = 0; t < c.TerrainHorseCost.Length; t++) Z("TerrainHorseCost[" + ((Terrain)t) + "]", c.TerrainHorseCost[t]);
        for (int t = 0; t < c.TerrainRailCost.Length; t++) Z("TerrainRailCost[" + ((Terrain)t) + "]", c.TerrainRailCost[t]);
    }

    /// <summary>
    /// 開幕の 1 回と、10 年ごとの年の終わりに書く。〔履歴 InstrumentsTransport.cs#17〕
    /// </summary>
    static void TrAreaRows(Sink sk, MultiNationSim sim, int year)
    {
        var regs = new List<(string Nation, Region R)>();
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) regs.Add((NameOf(n.World), r));
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
        // d2 ≥ dy² なので、dy² が今の最小を超えた側はそこで打ち切る（並びの外へ行くほど |dy| は単調に増える）。〔履歴 InstrumentsTransport.cs#18〕
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
                    if (g >= r.Markets.Length || r.Markets[g] is not { } mk) continue;
                    double sz = 0; foreach (var p in r.Pops) sz += p.Size;
                    if (!(sz > 0) || !(mk.Price > 0)) continue;
                    num += sz * mk.Price * fx; den += sz;
                }
            }
            sk.TrStat!.WriteLine(string.Join('\t', "P", TrI(year), "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", Catalog.Goods[g].Name, F(den > 0 ? num / den : double.NaN), F(Catalog.RefPrices[g])));
        }
    }
    // RailAcc〔履歴 InstrumentsTransport.cs#19〕

    internal sealed class RailAcc
    {
        public double Bought, LaidTerr, DecayLoss, WarLoss, InsRail, PaidRail, PaidRailForeign;
        public long Months, G2Rows, G2Viol, G7Excess, G7Fell, G7Lock, G7Hold12, G1ZeroSides, G1ZeroViol, G1Jump, G1PosSides, G1FormulaViol, G3NoShare, G4Moves, G4Viol;
        public double PrevPost = double.NaN, RunStartE, PreDecay, TPre, NeedPre;   // 年をまたいで持つ（Clear で消さない）
        public double MoveBefore = double.NaN;   // (1125) 割譲・分離・返還の時点の州の線路＝受け手の次の月の輸送の頭で読み直す（G4）。年をまたいで持つ
        public int RunLen;
        public double ProcWant, ProcGot; public long ProcStockZero, ProcSavBind;   // (1386) 敷きの調達の want・買えた量・在庫 0 で 0 の財×月・貯蓄で削った財×月（Clear で 0）
        public double NeedUsedShare = double.NaN;   // (1137) その年の最後の敷きの月にエンジンが使った need÷計器の目標（Clear で nan）
        public long GnOutViol, G1FormulaOutViol;    // (1137) 計器が自分で組んだ〈産出の比〉の need・φ の混ぜの式と、エンジンの need・倍率が合わない月・側の数
        public void Clear() { Bought = LaidTerr = DecayLoss = WarLoss = InsRail = PaidRail = PaidRailForeign = 0; Months = G2Rows = G2Viol = G7Excess = G7Fell = G7Lock = G7Hold12 = G1ZeroSides = G1ZeroViol = G1Jump = G1PosSides = G1FormulaViol = G3NoShare = G4Moves = G4Viol = 0; NeedUsedShare = double.NaN; GnOutViol = G1FormulaOutViol = 0; ProcWant = ProcGot = 0; ProcStockZero = ProcSavBind = 0; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, RailAcc> RailAccs = new();
    internal static RailAcc RailOf(Region r) => RailAccs.GetValue(r, _ => new RailAcc());

    /// <summary>計器の目標（門なし）。脅威の項を含む／含まない。</summary>
    static double RailInsTarget(World w, Coefficients c, bool threat)
    {
        double pop = w.TotalPopulation;
        double tg = pop * c.TransportTargetPerPop;
        if (threat && c.ThreatRailTargetWeight > 0 && w.SecurityThreatOwnMass > 1e-9)
        {
            double ratio = w.SecurityThreat / w.SecurityThreatOwnMass;
            if (ratio > 0 && double.IsFinite(ratio)) tg *= 1.0 + c.ThreatRailTargetWeight * ratio;
        }
        if (c.TransportCongestionWeight > 0 && w.Regions.Count > 0)
        {
            double s = 0; foreach (var r in w.Regions) s += 1.0 - r.LastTradeThrottle;
            tg *= 1.0 + c.TransportCongestionWeight * (s / w.Regions.Count);
        }
        return tg;
    }
    static double RailInsShare(World w, Region r)
    {
        double pot = 0; foreach (var x in w.Regions) pot += Math.Max(0, x.LastTradePotential);
        if (pot > 1e-9) return Math.Max(0, r.LastTradePotential) / pot;
        return w.Regions.Count > 0 ? 1.0 / w.Regions.Count : 1.0;
    }
    /// <summary>(1137) 計器の〈産出の比〉：州ごとに生産者の産出×参照価格（役務の財を外す）を計器が自分で足し、国の和で割る（和≦1e-9 なら均等）。エンジンの関数は呼ばない。</summary>
    static Dictionary<Region, double> RailInsOutShares(World w)
    {
        var v = new Dictionary<Region, double>(w.Regions.Count, ReferenceEqualityComparer.Instance);
        double tot = 0;
        foreach (var r in w.Regions)
        {
            double y = 0;
            foreach (var s in r.Producers) { var g = r.GoodOf(s.GoodId); if (g is null || g.Tier == GoodTier.Service) continue; y += s.Output * g.RefPrice; }
            v[r] = y; tot += y;
        }
        double even = w.Regions.Count > 0 ? 1.0 / w.Regions.Count : 1.0;
        foreach (var r in w.Regions) v[r] = tot > 1e-9 ? v[r] / tot : even;
        return v;
    }
    static double RailInsPhi(double t, double need) => !(need > 0) ? (t > 0 ? 1.0 : 0.0) : Math.Min(1.0, Math.Max(0.0, t / need));
    static bool RailInsWater(Region r, Coefficients c) => (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) || (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0);
    static (double Horse, double Rail) RailInsBases(Region r, Coefficients c)
    {
        int ti = (int)r.Terrain;
        double h = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
        double rl = (ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0) * Math.Min(1.0, Math.Max(0.0, c.RailCostFraction));
        return (h, rl);
    }

    /// <summary>月の輸送の段の頭（Transport.Step・減耗の前）。旗 0 の国は null（敷きの門は旗 1 の道だけ）。</summary>
    internal static RailMonth? RailMonthBegin(World w, Coefficients c)
    {
        if (!(c.RailByRegion > 0)) return null;
        var m = new RailMonth(w, c);
        foreach (var r in w.Regions)
        {
            var a = RailOf(r); a.PreDecay = r.RailTrack;
            if (!double.IsNaN(a.MoveBefore))
            {
                a.G4Moves++;   // G4：移した州の線路は受け手の次の月の頭までビットで変わらない（受け手の世界の州の並びに居る）
                if (BitConverter.DoubleToInt64Bits(r.RailTrack) != BitConverter.DoubleToInt64Bits(a.MoveBefore)) a.G4Viol++;
                a.MoveBefore = double.NaN;
            }
        }
        return m;
    }

    internal sealed class RailMonth
    {
        readonly World W; readonly Coefficients C; readonly double Target;
        readonly Dictionary<Region, double> OutShare;   // (1137) 月の頭（生産の後・敷きの前）に計器が自分で組んだ産出の比
        internal RailMonth(World w, Coefficients c)
        {
            W = w; C = c;
            bool gate = Discovery.LocomotiveRow < w.Discovered.Length && w.Discovered[Discovery.LocomotiveRow];
            Target = gate ? RailInsTarget(w, c, true) : 0.0;
            OutShare = RailInsOutShares(w);
        }
        /// <summary>敷きの前（減耗の後・買う前）：州の線路と、エンジンが使った need を読む（G7 の〈実際の側〉＝エンジンの need・(1137)。旗 0 では旧の計器の need とビットで同じ）。
        /// gnOutViol：エンジンの need が〈計器の目標×計器の産出の比〉と相対 1e-9 で合わない月（計器は旗を見ない）。</summary>
        internal void Pre(Region r, double need)
        {
            var a = RailOf(r);
            a.TPre = r.RailTrack; a.NeedPre = need;
            a.NeedUsedShare = Target > 0 ? need / Target : double.NaN;
            double want = Target * (OutShare.TryGetValue(r, out double so) ? so : double.NaN);
            if (!(Math.Abs(need - want) <= 1e-9 * Math.Abs(want))) a.GnOutViol++;
            a.DecayLoss += a.PreDecay - a.TPre; a.Months++;
        }
        /// <summary>(1386) 敷きの調達 1 回（州×財）：want・買えた量・呼ぶ前の在庫と値段と買い手の貯蓄を、BuyToward が持つ値のまま受け取る（計器は Procure を呼び直さない）。
        /// 在庫 0（または値段 0）で Procure が 0 を返す財×月・貯蓄が min(在庫,want)×値段 に足りず削る財×月を、計器の定義で数える。</summary>
        internal void Procured(Region r, double want, double bought, double stockBefore, double priceBefore, double savingsBefore)
        {
            var a = RailOf(r);
            a.ProcWant += want; a.ProcGot += bought;
            if (stockBefore <= 1e-9 || priceBefore <= 1e-9) a.ProcStockZero++;
            else if (Math.Min(stockBefore, want) * priceBefore > savingsBefore) a.ProcSavBind++;
        }
        /// <summary>敷きの後：G2（ΔT×地形＝買えた単位×CapPerUnit）・G7（need を超える州の月の積み）。</summary>
        internal void Post(Region r, double boughtUnits)
        {
            var a = RailOf(r);
            double tPost = r.RailTrack, dT = tPost - a.TPre;
            double terr = 1.0;
            if (C.RailLayTerrain > 0) { int ti = (int)r.Terrain; terr = ti < C.TerrainRailCost.Length ? C.TerrainRailCost[ti] : 1.0; if (!(terr > 0)) terr = 1.0; }
            double capBought = boughtUnits * C.TransportCapPerUnit;
            a.Bought += capBought; a.LaidTerr += dT * terr;
            double ulpT = 1e-15 * Math.Max(Math.Abs(a.TPre), Math.Abs(tPost));   // ΔT を大きな在庫の差で読む丸め（倍精度の分解能）
            if (capBought > 0) { a.G2Rows++; if (!(Math.Abs(dT * terr - capBought) <= 1e-9 * Math.Max(1.0, capBought) + 4 * ulpT * terr)) a.G2Viol++; }
            double e = a.TPre - a.NeedPre;
            if (e > 0 && !double.IsNaN(a.PrevPost))
            {
                a.G7Excess++;
                if (tPost < a.PrevPost) a.G7Fell++;
                if (dT > C.TransportDecay * a.NeedPre * (1.0 + 1e-9) + 1e-12 + 4 * ulpT) a.G7Lock++;
                if (a.RunLen == 0) a.RunStartE = e;
                a.RunLen++;
                if (a.RunLen >= 12) { if (e > 0.886 * a.RunStartE) a.G7Hold12++; a.RunLen = 0; }
            }
            else a.RunLen = 0;
            a.PrevPost = tPost;
        }
    }

    /// <summary>戦災（Transport.WearRail・旗 1）：州ごとの失い。</summary>
    internal static void RailWar(World w, double wear)
    {
        foreach (var r in w.Regions)
        {
            var a = RailOf(r); a.WarLoss += r.RailTrack * wear;
            if (!double.IsNaN(a.MoveBefore)) a.MoveBefore *= 1.0 - wear;   // 移した後の戦災はエンジンと同じ式で追う（r.RailTrack *= 1.0 - wear）
        }
    }

    /// <summary>割譲・分離・返還の頭（Transport.TakeRailForMove・旗 1）：移す時点の州の線路を控える。読み直しは受け手の次の月の輸送の頭（RailMonthBegin・減耗の前）＝TransferRegion の後の受け手の側（(1125)）。
    /// 旧は頭の直後に End を呼んでいた＝間に何も無く必ず通る門だった。</summary>
    internal static RailMove RailMoveBegin(World from, Region r) { RailOf(r).MoveBefore = r.RailTrack; return new RailMove(); }
    internal sealed class RailMove
    {
        internal void End(Region r) { }   // Transport.cs の呼び口は残す（そこには触れない）。判定は RailMonthBegin
    }

    /// <summary>清算の線路の運賃の払い（BilateralMarket.PayRailFreight・金建て）：受け手がその州の持ち主か、取り分を持つ外国かを計器が自分で確かめる（G3）。</summary>
    internal static void RailFreightPaid(Region r, World recipient, double gold)
    {
        var a = RailOf(r); a.PaidRail += gold;
        if (recipient.Regions.Contains(r)) return;
        a.PaidRailForeign += gold;   // (1147) 受け手が持ち主でない時の額（外国の取り分への払い）
        string key = recipient.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "";
        for (int k = 0; k < r.ForeignRulers.Length && k < r.ForeignShares.Length; k++) if (r.ForeignRulers[k] == key && r.ForeignShares[k] > 0) return;
        a.G3NoShare++;
    }

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, RailNeedCache> RailNeeds = new();
    sealed class RailNeedCache { public int Serial = int.MinValue; public InternationalMarket? M; public double Target; public Dictionary<Region, double>? Out; }

    /// <summary>成約の側ごと（TradeDeal）：G1（T＝0 の州の倍率＝12×馬・3×鉄道へ跳んだ行・T＞0 の州の倍率＝計器の φ の混ぜの式）と、G3 の計器の線路の分（φ と地形から）。</summary>
    static void RailDealSide(InternationalMarket m, World w, Region r, double mult, double landFreight)
    {
        var c = w.Coef;
        bool water = RailInsWater(r, c);   // 水運の州は G1 の母集合の外。G3 の計器の線路の分は水運の州も足す（(1125)・エンジンは水の費用を馬の費用・その RailCostFraction 倍を鉄道の費用として同じ φ の混ぜで払う）
        var a = RailOf(r);
        double h, rl;
        if (water) { h = r.Terrain == Terrain.WaterMajor ? c.TerrainWaterMajorCost : c.TerrainWaterMinorCost; rl = h * Math.Min(1.0, Math.Max(0.0, c.RailCostFraction)); }
        else (h, rl) = RailInsBases(r, c);
        double ratio = c.LandSeaCostRatio;
        double t = r.RailTrack;
        if (!(t > 0))
        {
            if (water) return;
            a.G1ZeroSides++;
            double want = ratio * h;
            if (!(Math.Abs(mult - want) <= 1e-12 * Math.Abs(want))) a.G1ZeroViol++;
            double jump = ratio * rl;
            if (Math.Abs(mult - jump) <= 1e-12 * Math.Abs(jump) && Math.Abs(want - jump) > 1e-12 * Math.Abs(want)) a.G1Jump++;
            return;
        }
        var nc = RailNeeds.GetValue(w, _ => new RailNeedCache());
        if (nc.Serial != m.ClearingSerial || !ReferenceEquals(nc.M, m)) { nc.Serial = m.ClearingSerial; nc.M = m; nc.Target = RailInsTarget(w, c, true); nc.Out = RailInsOutShares(w); }
        double phi = RailInsPhi(t, nc.Target * RailInsShare(w, r));
        double terr = (1.0 - phi) * h + phi * rl;
        double exp = ratio * terr;
        double phiOut = RailInsPhi(t, nc.Target * (nc.Out!.TryGetValue(r, out double so) ? so : double.NaN));   // (1137) 計器の産出の比で組んだ同じ混ぜの式
        double terrOut = (1.0 - phiOut) * h + phiOut * rl, expOut = ratio * terrOut;
        bool potOk = Math.Abs(mult - exp) <= 1e-9 * Math.Abs(exp), outOk = Math.Abs(mult - expOut) <= 1e-9 * Math.Abs(expOut);
        if (!water)
        {
            a.G1PosSides++;
            if (!potOk) a.G1FormulaViol++;
            if (!outOk) a.G1FormulaOutViol++;
        }
        // G3 の計器の線路の分：エンジンの倍率に合った方の式の φ で組む（潜在量の比が先・合わず産出の比が合えばそちら＝旗を読まない・(1137)）。旗 0 は旧と同じ式・同じ値
        if (!potOk && outOk) { phi = phiOut; terr = terrOut; }
        if (terr > 0 && phi > 0) a.InsRail += landFreight * (phi * rl / terr);
    }

    /// <summary>trnation_s の末尾 3 列：railTargetIndex（計器の目標・門なし）・railFillShare（RailCapacity÷目標）・railTargetNoThreatIndex（脅威の項を除く）。</summary>
    static string RailNatCols(World? w)
    {
        if (w is null) return "nan\tnan\tnan";
        double tg = RailInsTarget(w, w.Coef, true), tn = RailInsTarget(w, w.Coef, false);
        return F(tg) + "\t" + F(tg > 0 ? w.RailCapacity / tg : double.NaN) + "\t" + F(tn);
    }

    const string TrRailHeader = "year\tnation\tregion\tterrainCode\twaterFlag\trailTrackEnd\tneedEnd\tphiEnd\tmonthsCount\tboughtCapPerYear\tlaidCapTimesTerrainPerYear\tg2Rows\tg2Viol\tdecayLossPerYear\twarLossPerYear\t"
        + "g7ExcessRows\tg7FellRows\tg7LockRows\tg7Hold12Rows\tg1ZeroSides\tg1ZeroViol\tg1JumpRows\tg1PosSides\tg1FormulaViol\trailFreightInsGoldPerYear\trailFreightPaidGoldPerYear\tg3NoShareRows\tg4Moves\tg4Viol\tneedOutShareEnd\tneedUsedShareEnd\tgnOutViol\tg1FormulaOutViol\trailFreightForeignPaidGoldPerYear\tprocureWantUnitsPerYear\tprocureStockZeroGoodMonths\tprocureSavingsBindGoodMonths\tprocureGotUnitsPerYear\ttPreEnd";

    /// <summary>年の終わり：全ての国の全ての州の行（州の線路・計器の need と φ・門の数）。旗 0 の世界でも書く（線路は 0・G1 の T＝0 の側の数）。</summary>
    static void TrRailRows(Sink sk, MultiNationSim sim, int year)
    {
        if (sk.TrRail is null) { sk.TrRail = Open("trrail", sk.Seed); sk.TrRail.WriteLine(TrRailHeader); }
        foreach (var n in sim.Nations)
        {
            var w = n.World; var c = w.Coef; string nation = NameOf(w);
            if (w.Regions.Count == 0) continue;
            bool gate = Discovery.LocomotiveRow < w.Discovered.Length && w.Discovered[Discovery.LocomotiveRow];
            double tg = RailInsTarget(w, c, true);
            var outSh = RailInsOutShares(w);   // (1137) 年の終わりの計器の産出の比
            foreach (var r in w.Regions)
            {
                var a = RailOf(r);
                double share = RailInsShare(w, r);
                double needG = gate ? tg * share : 0.0;
                sk.TrRail.WriteLine(string.Join('\t', TrI(year), nation, r.Name, TrI((int)r.Terrain), RailInsWater(r, c) ? "1" : "0", F(r.RailTrack), F(needG), F(RailInsPhi(r.RailTrack, tg * share)),
                    L(a.Months), F(a.Bought), F(a.LaidTerr), L(a.G2Rows), L(a.G2Viol), F(a.DecayLoss), F(a.WarLoss),
                    L(a.G7Excess), L(a.G7Fell), L(a.G7Lock), L(a.G7Hold12), L(a.G1ZeroSides), L(a.G1ZeroViol), L(a.G1Jump), L(a.G1PosSides), L(a.G1FormulaViol), F(a.InsRail), F(a.PaidRail), L(a.G3NoShare), L(a.G4Moves), L(a.G4Viol),
                    F(outSh[r]), F(a.NeedUsedShare), L(a.GnOutViol), L(a.G1FormulaOutViol), F(a.PaidRailForeign), F(a.ProcWant), L(a.ProcStockZero), L(a.ProcSavBind), F(a.ProcGot), F(a.TPre)));
                a.Clear();
            }
        }
    }
    /// <summary>(1386) region_s の末尾 7 列の見出し（RegionProdHeaderText の後）。</summary>
    internal const string RegionExtraHeaderText = "transportCapEnd\ttradeThrottleEnd\tregionShareEnd\tworkerSinkShareEnd\tstapleImportPerMonth\tstapleProducedPerMonth\tgdpOutputShareEnd";

    /// <summary>(1386) 州の工場労働者（BuysLabor の生産者の LaborPopIndex の最多の POP・同数なら番号の小さい側）に乗る生産者の LaborWeight×Scale の和のうち、LastInputFill＜0.05 の生産者の分の割合。
    /// 乗る重みが 0 以下なら nan。計器が自分で数える（エンジンの関数は呼ばない）。</summary>
    internal static double WorkerSinkShare(Region r)
    {
        var cnt = new Dictionary<int, int>();
        foreach (var s in r.Producers) if (s.BuysLabor) cnt[s.LaborPopIndex] = cnt.TryGetValue(s.LaborPopIndex, out int k) ? k + 1 : 1;
        if (cnt.Count == 0) return double.NaN;
        int best = -1, bestN = 0;
        foreach (var kv in cnt) if (kv.Value > bestN || (kv.Value == bestN && kv.Key < best)) { best = kv.Key; bestN = kv.Value; }
        double tot = 0, sink = 0;
        foreach (var s in r.Producers)
        {
            if (s.LaborPopIndex != best) continue;
            double wgt = s.LaborWeight * s.Scale;
            tot += wgt; if (s.LastInputFill < 0.05) sink += wgt;
        }
        return tot > 0 ? sink / tot : double.NaN;
    }

    /// <summary>(1386) 主食（財の段が Staple）の州の輸入と産出の和（月あたり＝PrevImport・LastProduced）。</summary>
    internal static (double Import, double Produced) StapleFlows(Region r)
    {
        double imp = 0, prod = 0;
        for (int g = 0; g < Catalog.Goods.Length && g < r.Markets.Length; g++)
        {
            if (Catalog.Goods[g].Tier != GoodTier.Staple) continue;
            imp += r.Markets[g].PrevImport; prod += r.Markets[g].LastProduced;
        }
        return (imp, prod);
    }

    /// <summary>(1386) 州の産出（役務でない財の Output×RefPrice）の和。国の州の和で割る分子と分母に同じ関数を使う。</summary>
    internal static double RegionOutputRef(Region r)
    {
        double y = 0;
        foreach (var s in r.Producers) { var g = r.GoodOf(s.GoodId); if (g is null || g.Tier == GoodTier.Service) continue; y += s.Output * g.RefPrice; }
        return y;
    }

    /// <summary>(1386) region_s の末尾 7 列（州ごとの値を tab で連ねた文字列）。regionShareEnd は国ごとに ShareBasis を 1 回だけ作る。gdpOutputShareEnd は w.GdpPerMonth を呼ばない（台帳と控えを動かさない）。</summary>
    internal static Dictionary<Region, string> RegionExtraRows(World w)
    {
        var res = new Dictionary<Region, string>(w.Regions.Count, ReferenceEqualityComparer.Instance);
        var basis = Transport.ShareBasis(w, w.Coef);
        double tot = 0; var outs = new double[w.Regions.Count];
        for (int i = 0; i < outs.Length; i++) { outs[i] = RegionOutputRef(w.Regions[i]); tot += outs[i]; }
        for (int i = 0; i < outs.Length; i++)
        {
            var r = w.Regions[i]; var (imp, prod) = StapleFlows(r);
            res[r] = string.Join('\t', F(r.LastTransportCap), F(r.LastTradeThrottle), F(Transport.RegionShare(basis, r)), F(WorkerSinkShare(r)), F(imp), F(prod), F(tot > 1e-9 ? outs[i] / tot : double.NaN));
        }
        return res;
    }
    static string L(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
