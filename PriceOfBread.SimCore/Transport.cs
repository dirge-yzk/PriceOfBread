namespace PriceOfBread.SimCore;

public static class Transport
{
    public static void Step(World w, Coefficients c)
    {
        var rm = Instruments.On ? Instruments.RailMonthBegin(w, c) : null;
        if (c.RailByRegion > 0)
        {
            foreach (var r in w.Regions) r.RailTrack = System.Math.Max(0, r.RailTrack * (1 - c.TransportDecay));
        }
        else w.RailCapacity = System.Math.Max(0, w.RailCapacity * (1 - c.TransportDecay));
        w.ShipCapacity = System.Math.Max(0, w.ShipCapacity * (1 - c.TransportDecay));
        double target = Target(w, c);
        double railGate = Discovery.RailTransport(w.Discovered) ? 1.0 : 0.0;
        BuyToward(w, c, [c.RailGoodId, c.LocoGoodId], target * railGate, rail: true, 1.0, rm);
        const double era = 1.0;   // 時代係数は恒等 1（Coefficients.EraFactor は 1.0 を返す＝係数 EraColonyCoal は (1154) で削除）
        double shipBefore = w.ShipCapacity;
        var (shipAdd, steamAdd) = BuyToward(w, c, c.ShipGoodIds, target, rail: false, era);
        if (shipAdd > 0)
        {
            double s = (w.ShipSteamShare.Value * shipBefore + steamAdd) / w.ShipCapacity;
            w.ShipSteamShare = new Share(s >= 0 ? System.Math.Min(1.0, s) : 0.0);
        }
        if (c.ShipFuelRate > 0) BuyShipFuel(w, c, era);
    }

    internal static double Target(World w, Coefficients c)
    {
        double target = w.TotalPopulation * c.TransportTargetPerPop;
        if (c.ThreatRailTargetWeight > 0 && w.SecurityThreatOwnMass > 1e-9)
        {
            double threatRatio = w.SecurityThreat / w.SecurityThreatOwnMass;
            if (threatRatio > 0 && double.IsFinite(threatRatio)) target *= 1.0 + c.ThreatRailTargetWeight * threatRatio;
        }
        if (c.TransportCongestionWeight > 0)
        {
            double congSum = 0; int congN = 0;
            foreach (var r in w.Regions) { congSum += 1.0 - r.LastTradeThrottle; congN++; }
            if (congN > 0) target *= 1.0 + c.TransportCongestionWeight * (congSum / congN);
        }
        return target;
    }

    internal static double RailTrackSum(World w)
    {
        double s = 0;
        foreach (var r in w.Regions) s += r.RailTrack;
        return s;
    }

    internal static double RailPhi(double track, double need)
    {
        if (!(need > 0)) return track > 0 ? 1.0 : 0.0;
        double f = track / need;
        return f >= 1.0 ? 1.0 : (f > 0 ? f : 0.0);
    }

    internal static void WearRail(World w, double wear)
    {
        if (w.Coef.RailByRegion > 0)
        {
            if (Instruments.On) Instruments.RailWar(w, wear);
            foreach (var r in w.Regions) r.RailTrack *= 1.0 - wear;
        }
        else w.RailCapacity *= 1.0 - wear;
    }

    internal static double TakeRailForMove(World from, Region r, double sh)
    {
        if (from.Coef.RailByRegion > 0)
        {
            var mv = Instruments.On ? Instruments.RailMoveBegin(from, r) : null;
            mv?.End(r);
            return 0.0;
        }
        double rail = from.RailCapacity * sh;
        from.RailCapacity -= rail;
        return rail;
    }

    internal static double PreRailCapacity(World w, Coefficients c) => w.TotalPopulation * c.TransportTargetPerPop;

    internal static (double Even, double TotalPot) ShareBasis(World w, Coefficients c)
    {
        int activeCount = w.Regions.Count;
        double evenShare = activeCount > 0 ? 1.0 / activeCount : 1.0;
        double totalPot = 0;
        foreach (var r in w.Regions) totalPot += System.Math.Max(0, r.LastTradePotential);
        return (evenShare, totalPot);
    }

    internal static double RegionShare((double Even, double TotalPot) b, Region r)
        => b.TotalPot > 1e-9 ? System.Math.Max(0, r.LastTradePotential) / b.TotalPot : b.Even;

    /// <summary>(1137) 鉄道の need の取り分の基底（敷きと清算の φ だけが読む）。旗 RailNeedByOutput＝1 は州の産出の比（分母＝GdpPerMonth・分子はその取り直しの控え）、0 は潜在量の比（ShareBasis）。
    /// 取り分の規則は 2 本：清算の陸の容量・船・割譲の移しは潜在量の比（ShareBasis・RegionShare）のまま、鉄道の need だけがこの基底（別の量を 1 回ずつ数える）。</summary>
    internal static (double Even, double TotalPot) RailNeedBasis(World w, Coefficients c)
        => w.Coef.RailNeedByOutput > 0 ? (w.Regions.Count > 0 ? 1.0 / w.Regions.Count : 1.0, w.GdpPerMonth.Value) : ShareBasis(w, c);

    /// <summary>(1137) 鉄道の need の州の取り分。旗 1 は Region.GdpOutputMemo÷産出の和（和≦1e-9 なら均等）＝控えは RailNeedBasis か GdpPerMonth に触れた直後だけ正しい。</summary>
    internal static double RailNeedShare(World w, (double Even, double TotalPot) b, Region r)
        => w.Coef.RailNeedByOutput > 0 ? (b.TotalPot > 1e-9 ? r.GdpOutputMemo / b.TotalPot : b.Even) : RegionShare(b, r);

    internal static void Receive(World to, double rail, double ship, double steamShare)
    {
        if (rail > 0) to.RailCapacity += rail;
        if (!(ship > 0)) return;
        double tot = to.ShipCapacity + ship;
        double s = (to.ShipSteamShare.Value * to.ShipCapacity + steamShare * ship) / tot;
        to.ShipSteamShare = new Share(s >= 0 ? System.Math.Min(1.0, s) : 0.0);
        to.ShipCapacity = tot;
    }

    private static void BuyShipFuel(World w, Coefficients c, double era)
    {
        double totalWant = c.ShipFuelRate * era * (w.ShipCapacity - Colonial.OccupiedShipCapacity(w)) * w.ShipSteamShare.Value;
        if (!(totalWant > 1e-9)) return;
        int regCount = 0;
        foreach (var r in w.Regions) if ((r.MerchantPopIndex >= 0 || r.InvestorPopIndex >= 0)) regCount++;
        if (regCount == 0) return;
        double per = totalWant / regCount;
        foreach (var r in w.Regions)
        {
            int buyerIdx = r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex;
            if (buyerIdx < 0 || Catalog.CoalId >= r.Markets.Length) continue;
            var buyer = r.Pops[buyerIdx];
            var (fuelG, want) = Colonial.ChooseFuelGood(r, c, per);
            InternationalMarket.Procure(w, r, fuelG, want, buyer);
        }
    }

    private static (double Total, double Steam) BuyToward(World w, Coefficients c, int[] goods, double target, bool rail, double era, Instruments.RailMonth? rm = null)
    {
        double addTotal = 0, addSteam = 0;
        bool byReg = rail && c.RailByRegion > 0;
        double perGood = default;
        (double Even, double TotalPot) basis = default;
        if (byReg)
        {
            if (!(target > 0)) return (0, 0);
            basis = RailNeedBasis(w, c);
        }
        else
        {
            double cap = rail ? w.RailCapacity : w.ShipCapacity;
            double gapUnits = (target - cap) * c.TransportBuildRate / c.TransportCapPerUnit;
            if (gapUnits <= 1e-9) return (0, 0);
            int regCount = 0;
            foreach (var r in w.Regions) if (r.InvestorPopIndex >= 0) regCount++;
            if (regCount == 0) return (0, 0);
            perGood = rail ? gapUnits / regCount / goods.Length : gapUnits / regCount;
        }
        foreach (var r in w.Regions)
        {
            if (r.InvestorPopIndex < 0) continue;
            double terrLay = 1.0, boughtUnits = 0;
            if (byReg)
            {
                double need = target * RailNeedShare(w, basis, r);
                double tPre = r.RailTrack;
                double wantCap = c.TransportBuildRate * System.Math.Max(0, need - tPre) + c.TransportDecay * System.Math.Min(tPre, need);
                if (c.RailLayTerrain > 0)
                {
                    int ti = (int)r.Terrain;
                    terrLay = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
                    if (!(terrLay > 0)) terrLay = 1.0;
                }
                perGood = wantCap / c.TransportCapPerUnit * terrLay / goods.Length;
                rm?.Pre(r, need);
                if (!(perGood > 1e-9)) { rm?.Post(r, 0.0); continue; }
            }
            int buyerIdx = rail ? r.InvestorPopIndex : (r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex);
            var investor = r.Pops[buyerIdx];
            double steamGate = rail ? 0.0 : Discovery.GoodGate(r, c.ShipTier2GoodId);
            double steamMult = c.ShipTier2CapMult * steamGate / era;
            int pick = -1;
            if (!rail)
            {
                double best = 0;
                foreach (int gi in goods)
                {
                    if (gi < 0 || gi >= r.Markets.Length) continue;
                    var mg = r.Markets[gi];
                    bool steam = gi == c.ShipTier2GoodId;
                    if (steam && !(steamGate > 0 && (mg.Stock > 1e-9 || mg.LastProduced + mg.LastImport > 0))) continue;
                    double cost = mg.Price / (c.TransportCapPerUnit * (steam ? steamMult : 1.0));
                    if (steam && c.ShipFuelRate > 0)
                    {
                        var (fuelG, fuelQty) = Colonial.ChooseFuelGood(r, c, 1.0);
                        cost += c.ShipFuelRate * era * r.Markets[fuelG].Price * fuelQty / c.TransportDecay;
                    }
                    if (pick < 0 || cost < best) { pick = gi; best = cost; }
                }
            }
            foreach (int gi in goods)
            {
                if (gi < 0 || gi >= r.Markets.Length) continue;
                if (!rail && gi != pick) continue;
                var m = r.Markets[gi];
                double mult = (!rail && gi == c.ShipTier2GoodId) ? steamMult : 1.0;
                double want = perGood / mult;
                double stk0 = 0, pr0 = 0;
                if (rm is not null) { stk0 = m.Stock; pr0 = m.Price; }   // (1386) 計器の受け口（呼ぶ前の値を読むだけ）
                double buy = InternationalMarket.Procure(w, r, gi, want, investor);
                if (byReg) rm?.Procured(r, want, buy, stk0, pr0);
                if (byReg && c.RailProcureNational > 0 && want - buy > 1e-9) buy += ProcureNational(w, c, r, gi, want - buy, investor, target, basis);   // (1407) 州の在庫で足りない分を同じ国の他の州から
                if (buy <= 1e-9) continue;
                double add = buy * c.TransportCapPerUnit * mult;
                if (byReg) { r.RailTrack += add / terrLay; boughtUnits += buy; }
                else if (rail) w.RailCapacity += add; else w.ShipCapacity += add;
                addTotal += add;
                if (!rail && gi == c.ShipTier2GoodId) addSteam += add;
            }
            if (byReg) rm?.Post(r, boughtUnits);
        }
        return (addTotal, addSteam);
    }

    /// <summary>(1407) c150rp 旗 RailProcureNational：州 r の在庫で足りない線路・機関車（deficit 単位）を、同じ国（w）の他の州のうち陸の運賃つきの値段が一番安い 1 州の在庫から、r の投資家が買う。
    /// 売り買いは Procure を売り手の州で呼ぶ（売り手の州の在庫・売り上げ・需要の記帳と、売り手の生産者への払いは州の調達と同じ道）。
    /// 運賃＝荷価×FreightRatePer10Mm×陸の等価距離÷DistanceNormKm。陸の等価距離は清算の陸の道（BilateralMarket.PairKmCore の viaLand）と同じ形＝道の距離×½×(両端の陸の倍率の和)・陸の倍率＝((1−φ)×馬の地形の費用＋φ×鉄道の地形の費用×RailCostFraction)×LandSeaCostRatio（清算の旗 RailByRegion の枝と同じ）。
    /// 運賃の払い先は清算の陸の運びの記帳（railRecv と freightPool）の形を国の中に写す：両端の鉄道の分（φ×鉄道の費用×LandSeaCostRatio÷両端の倍率の和）は端の州の投資家へ（買い手の州の分は払い手自身＝動かさない）・残りは国の商人へ。
    /// 陸の道が無い州（座標が無い・道の距離が無限）からは買わない。戻り値＝買えた単位。</summary>
    internal static double ProcureNational(World w, Coefficients c, Region r, int gi, double deficit, Pop investor, double target, (double Even, double TotalPot) basis)
    {
        if (!(deficit > 1e-9) || !(c.LandRouteEnabled > 0)) return 0;
        var ca = Climate.CityCoord(r.Name);
        if (ca.Lat == 0 && ca.Lon == 0) return 0;
        var (kA, railKA) = LandMul(w, c, r, target, basis);
        Region? src = null; double bestUnit = 0, bestFr = 0, bestK = 0, bestRailK = 0;
        foreach (var b in w.Regions)
        {
            if (ReferenceEquals(b, r) || gi >= b.Markets.Length) continue;
            var mb = b.Markets[gi];
            if (mb.Stock <= 1e-9 || mb.Price <= 1e-9) continue;
            var cb = Climate.CityCoord(b.Name);
            if (cb.Lat == 0 && cb.Lon == 0) continue;
            double km = PathGraph.LandDistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon);
            if (!double.IsFinite(km)) continue;
            var (kB, railKB) = LandMul(w, c, b, target, basis);
            double fr = c.FreightRatePer10Mm * (km * 0.5 * (kA + kB)) / Diplomacy.DistanceNormKm;
            double unit = mb.Price * (1.0 + fr);
            if (src is null || unit < bestUnit) { src = b; bestUnit = unit; bestFr = fr; bestK = kB; bestRailK = railKB; }
        }
        if (src is null) return 0;
        double take = System.Math.Min(deficit, System.Math.Max(0, investor.Savings.Value) / bestUnit);
        if (!(take > 1e-9)) return 0;
        double px = src.Markets[gi].Price;
        double got = InternationalMarket.Procure(w, src, gi, take, investor);
        if (!(got > 1e-9)) return 0;
        double freight = got * px * bestFr;
        if (!(freight > 0)) return got;
        double kSum = kA + bestK;
        double toSrcRail = kSum > 0 ? freight * bestRailK / kSum : 0.0;
        double toOwnRail = kSum > 0 ? freight * railKA / kSum : 0.0;   // 買い手の州の鉄道の分＝払い手（買い手の州の投資家）自身＝動かさない
        if (toSrcRail > 0)
        {
            var srcInv = src.InvestorPopIndex >= 0 && src.InvestorPopIndex < src.Pops.Count && src.Pops[src.InvestorPopIndex].Size > 1e-9 ? new List<Pop> { src.Pops[src.InvestorPopIndex] } : src.Pops;
            World.Transfer((w, [investor]), (w, srcInv, null), toSrcRail, income: true);
        }
        double rest = freight - toSrcRail - toOwnRail;
        if (rest > 0)
        {
            var carriers = new List<Pop>();
            foreach (var r2 in w.Regions) if (r2.MerchantPop is { } mp) carriers.Add(mp);
            if (carriers.Count == 0) foreach (var r2 in w.Regions) carriers.AddRange(r2.Pops);
            World.Transfer((w, [investor]), (w, carriers, null), rest, income: true);
        }
        return got;
    }

    /// <summary>(1407) 州の陸の倍率（清算の旗 RailByRegion の枝の terrCost×landRatio）と、その鉄道の分（railFrac×倍率＝φ×鉄道の地形の費用×RailCostFraction×LandSeaCostRatio）。</summary>
    private static (double K, double RailK) LandMul(World w, Coefficients c, Region x, double target, (double Even, double TotalPot) basis)
    {
        int ti = (int)x.Terrain;
        double rcf01 = PoliticalQuantities.Clamp01(c.RailCostFraction);
        double bh = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
        double br = (ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0) * rcf01;
        if (x.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) { bh = c.TerrainWaterMajorCost; br = bh * rcf01; }
        else if (x.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) { bh = c.TerrainWaterMinorCost; br = bh * rcf01; }
        double phi = RailPhi(x.RailTrack, target * RailNeedShare(w, basis, x));
        double m1 = (1.0 - phi) * bh + phi * br;
        return (m1 * c.LandSeaCostRatio, phi * br * c.LandSeaCostRatio);
    }
}
