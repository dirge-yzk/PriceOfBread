using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 目的＝穀物法の地主の項を〈次の一段で失う所得の割合〉（S↓）と〈戻したときの得〉（S↑）で読み直す案 C1′ の材料。〔履歴 InstrumentsCorncf.cs#1〕
public static partial class Instruments
{
    sealed class CorncfSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CorncfSink> CorncfSinks = new();

    /// <summary>穀物法の段の楔（純関数）：段 0 なし 0・1 限定 0.5・2 全面 1.0。</summary>
    internal static double CfCornWedge(int level) => level >= 2 ? 1.0 : level == 1 ? 0.5 : 0.0;

    /// <summary>大圏距離 km（純関数・計器の写し）。</summary>
    internal static double CfGreatCircleKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * System.Math.PI / 180.0, dLon = (lon2 - lon1) * System.Math.PI / 180.0;
        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2)
                 + System.Math.Cos(lat1 * System.Math.PI / 180.0) * System.Math.Cos(lat2 * System.Math.PI / 180.0) * System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
        a = a < 0 ? 0 : a > 1 ? 1 : a;
        return 6371.0 * 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
    }

    /// <summary>港州の外国持分の人口加重平均（計器の写し）＝自国の関税が効かない割合。</summary>
    internal static double CfForeignPort(World w)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (!r.IsPort) continue;
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            double v = 0; foreach (var x in r.ForeignShares) if (x > 0) v += x;
            num += pop * (!(v >= 0) ? 0 : System.Math.Min(1.0, v)); den += pop;
        }
        return den > 0 ? num / den : 0.0;
    }

    /// <summary>楔 d の三つ組（純関数）：今・段を 1 つ下げた後（なしは今のまま）・段を 1 つ上げた後（全面は今のまま）。d＝max(0, 関税の率)＋穀物法の段の楔×CornLawImportPenalty。</summary>
    internal static (double Now, double Down, double Up) CfDuties(World w)
    {
        var c = w.Coef;
        double rate = System.Math.Clamp(w.Tariff * (1.0 - CfForeignPort(w)), c.TariffFloor, 1.0);
        double t = System.Math.Max(0, rate), pen = c.CornLawImportPenalty;
        int lv = (int)w.CornLaws;
        return (t + CfCornWedge(lv) * pen, t + CfCornWedge(System.Math.Max(0, lv - 1)) * pen, t + CfCornWedge(System.Math.Min(2, lv + 1)) * pen);
    }

    /// <summary>着値の係数 A・B（純関数）：売り手 i の値段 s[i]・観測の門 alive[i]・重み wt[i]（産出）・距離 km[i]。門＝alive かつ s≧0 かつ wt＞0。A＝Σwt·s·(1＋TM×(1＋km÷1万)×運賃)÷Σwt・B＝Σwt·s÷Σwt。売り手が居なければ nan。</summary>
    internal static (double A, double B) CfLandedAB(double[] s, bool[] alive, double[] wt, double[] km, double tm, double freight)
    {
        double a = 0, b = 0, den = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (!alive[i] || !(s[i] >= 0) || !(wt[i] > 0)) continue;
            a += wt[i] * s[i] * (1.0 + tm * (1.0 + km[i] / 10_000.0) * freight); b += wt[i] * s[i]; den += wt[i];
        }
        return den > 1e-12 ? (a / den, b / den) : (double.NaN, double.NaN);
    }

    /// <summary>値段の型（純関数）：1 輸入と競う（L(0)＜p）・2 楔が効かない（L(0)≧p・輸出 0）・3 輸出する（L(0)≧p・輸出＞0）・0 判定不能。</summary>
    internal static int CfType(double p, double l0, bool haveTrade, double export)
    {
        if (!(p > 0) || double.IsNaN(l0)) return 0;
        if (l0 < p) return 1;
        return !haveTrade ? 0 : export > 0 ? 3 : 2;
    }

    /// <summary>財ごとの利害 S（純関数）：Down＝S↓・Up＝S↑・DownTimesM＝m を掛けた S↓′（記録）・UpPriceOnly＝S↑ を値段だけ＋輸入の有無で（記録）。型 1 だけ立つ。law＝穀物法の段 0〜2。</summary>
    internal static (double Down, double Up, double DownTimesM, double UpPriceOnly) CfStake(double p, double a, double b, (double Now, double Down, double Up) d, double m, int law, int type)
    {
        if (type != 1 || !(p > 0) || double.IsNaN(a)) return (0, 0, 0, 0);
        double l = a + d.Now * b, lDn = a + d.Down * b, lUp = a + d.Up * b;
        double down = law >= 1 ? System.Math.Max(0, System.Math.Min(p, l) - lDn) / p : 0.0;
        double up = law < 2 ? m * (lUp - l) / p : 0.0;
        double upPo = law < 2 && m > 0 ? (lUp - l) / p : 0.0;
        return (down, up, m * down, upPo);
    }

    /// <summary>利害の分母（純関数）＝全ての RGO の財の期待の地代の和 trAll（分子と同じ規則）。1e-9 以下なら割らない（Normalize と同じ）。</summary>
    internal static double CfOverAll(double num, double trAll) => trAll > 1e-9 ? num / trAll : num;

    /// <summary>国家の枠の割合（純関数）＝国家の枠へ行った額÷（私の 4 枠＋国家の枠）。どちらも 0 なら nan。</summary>
    internal static double CfCrownShare(double crown, double priv) => crown + priv > 0 ? crown / (crown + priv) : double.NaN;

    /// <summary>国有化の部門の印の添字（計器の写し・純関数）：0 鉄道・1 鉱山・2 軍事・4 石油・5 電力・6 全て（主食・日用・奢侈・中間・生産財）・−1 該当なし。</summary>
    internal static int CfSectorBit(int goodId)
    {
        if (goodId == Catalog.LocomotiveId) return 0;
        if (goodId == Catalog.IronOreId || goodId == Catalog.CoalId || goodId == Catalog.CopperOreId || goodId == Catalog.LeadOreId || goodId == Catalog.GoldOreId || goodId == Catalog.SulfurId
            || goodId == Catalog.LimestoneId || goodId == Catalog.SilicaSandId || goodId == Catalog.ClayId || goodId == Catalog.SilverId) return 1;
        if (goodId == Catalog.ArtilleryId || goodId == Catalog.RifleId || goodId == Catalog.AmmunitionId || goodId == Catalog.WarshipId || goodId == Catalog.TankId || goodId == Catalog.DreadnoughtId
            || goodId == Catalog.MilitaryAircraftId || goodId == Catalog.ChemicalWeaponId) return 2;
        if (goodId == Catalog.NewspaperId || goodId == Catalog.RadioId) return 6;
        if (goodId == Catalog.CrudeOilId) return 4;
        if (goodId == Catalog.ElectricityId) return 5;
        return goodId >= 0 && goodId < Catalog.Goods.Length && Catalog.Goods[goodId].Tier is GoodTier.Staple or GoodTier.Daily or GoodTier.Luxury or GoodTier.Intermediate or GoodTier.Producer ? 6 : -1;
    }

    /// <summary>国営持分 f0（計器の写し・国家の枠を除く）＝建てた容量÷操業規模（上限 1）・国有化の下限（接収の速度が 0 以下のときだけ）。</summary>
    internal static double CfStateFrac(World w, Region r, Producer s)
    {
        double f = s.BuiltScale > 1e-9 ? System.Math.Min(1.0, s.BuiltScale / System.Math.Max(s.Scale, 1e-9)) : 0.0;
        var c = w.Coef;
        if (c.NationalizationSeizureRate <= 0 && r.NationalizedMask != 0)
        {
            int bit = CfSectorBit(s.GoodId);
            if (bit >= 0 && (r.NationalizedMask & (1 << bit)) != 0) f = System.Math.Max(f, c.NationalizationStateShare.Value);
        }
        return f;
    }

    /// <summary>産地の期待の地代を受け手のブロックへ（国の全ての州・RGO）。prodRent[b×nCorn＋穀物の財の位置]＝ブロック b が受ける穀物の地代・allRent[b]＝全ての RGO の財の地代・crown＝国家の枠へ行った額・priv＝私の枠へ行った額。cornPos[財 Id]＝穀物の財の位置（穀物でなければ −1）。</summary>
    internal static void CfRents(World w, int[] cornPos, int nCorn, double[] prodRent, double[] allRent, ref double crown, ref double priv)
    {
        foreach (var r in w.Regions)
        {
            int ng = System.Math.Min(r.Markets.Length, Catalog.Goods.Length);
            foreach (var s in r.Producers)
            {
                if (!s.IsRgo || s.GoodId < 0 || s.GoodId >= ng || s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count) continue;
                double v = System.Math.Max(0, s.Output * r.Markets[s.GoodId].Price) * System.Math.Min(s.RentShare.Value, r.FarmRentCap.Value) * (1.0 - CfStateFrac(w, r, s));
                if (!(v > 0)) continue;
                int cp = cornPos[s.GoodId];
                if (s.LandShares is { } ls)
                {
                    long fb = 0;
                    for (int k = 0, nk = LsSlots(w, ls); k < nk; k++)
                    {
                        if (!(ls[k] > 0)) continue;
                        int b = CnBloc(r.Pops[LsRecipient(r, s, k, ref fb)]); double x = v * ls[k];
                        allRent[b] += x; priv += x;
                        if (cp >= 0) prodRent[b * nCorn + cp] += x;
                    }
                    if (w.Coef.LandShareCrownSlot > 0 && ls.Length == 5 && ls[4] > 0) crown += v * ls[4];
                }
                else
                {
                    int b = CnBloc(r.Pops[s.OwnerPopIndex]);
                    allRent[b] += v; priv += v;
                    if (cp >= 0) prodRent[b * nCorn + cp] += v;
                }
            }
        }
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の行を書く。</summary>
    static void CorncfTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = CorncfSinks.GetValue(sim, _ => new CorncfSink());
        sk.W ??= Open("corncf", seed);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int gc = Catalog.Goods.Length;
        var cornIds = new List<int>(); var cornPos = new int[gc];
        for (int g = 0; g < gc; g++) { bool isCorn = Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].GrainChain; cornPos[g] = isCorn ? cornIds.Count : -1; if (isCorn) cornIds.Add(g); }
        int nc = cornIds.Count;
        // 売り手の側（全ての国の観測：産出・純輸入・需要・物価水準）
        int nn = sim.Nations.Count;
        var sProd = new double[nn][]; var sNet = new double[nn][]; var sDem = new double[nn][]; var sPl = new double[nn];
        for (int i = 0; i < nn; i++)
        {
            var wi = sim.Nations[i].World;
            sProd[i] = new double[gc]; sNet[i] = new double[gc]; sDem[i] = new double[gc];
            foreach (var r in wi.Regions)
            {
                int mn = System.Math.Min(r.Markets.Length, gc);
                for (int g = 0; g < mn; g++) { var m = r.Markets[g]; sProd[i][g] += m.LastProduced; sNet[i][g] += m.LastImport - m.LastExport; sDem[i][g] += m.LastDemand; }
            }
            sPl[i] = wi.MonetaryEnabled ? wi.PriceLevel : 1.0;
        }
        for (int j = 0; j < nn; j++)
        {
            var w = sim.Nations[j].World;
            if (w.Regions.Count == 0) continue;
            var c = w.Coef;
            int law = (int)w.CornLaws;
            var duty = CfDuties(w);
            // 地代
            var prodRent = new double[3 * nc]; var allRent = new double[3]; double crown = 0, priv = 0;
            CfRents(w, cornPos, nc, prodRent, allRent, ref crown, ref priv);
            double trAll = allRent[0] + allRent[1] + allRent[2];
            // 貿易
            bool haveTrade = w.ExportsByGood is { } eg && w.ImportsByGood is { } ig && eg.Length == gc && ig.Length == gc;
            var prodVal = new double[gc];
            foreach (var r in w.Regions) { int ng = System.Math.Min(r.Markets.Length, gc); for (int g = 0; g < ng; g++) prodVal[g] += r.Markets[g].LastProduced * r.Markets[g].Price; }
            // 距離（売り手ごと）
            var km = new double[nn];
            for (int i = 0; i < nn; i++) km[i] = i == j ? 0.0 : CfGreatCircleKm(w.Latitude, w.Longitude, sim.Nations[i].World.Latitude, sim.Nations[i].World.Longitude);
            int sellers = 0; for (int i = 0; i < nn; i++) if (i != j && sim.Nations[i].World.DiscoveryPrice is not null) sellers++;
            var cols = new List<(string Name, string Val)>(100)
            {
                ("quarter", I(q)), ("year", I(w.Year)), ("nation", NameOf(w)), ("cornLawsCode", I(law)),
                ("cfDutyNow", F(duty.Now)), ("cfDutyDown", F(duty.Down)), ("cfDutyUp", F(duty.Up)),
            };
            var sDown = new double[3]; var sUp = new double[3]; var sDownM = new double[3]; var sUpPo = new double[3];
            var typeRent = new double[4]; double rent0 = 0, pinRent0 = 0, pinDen0 = 0;
            var own = w.DiscoveryPrice;
            for (int ci = 0; ci < nc; ci++)
            {
                int g = cornIds[ci], id = g;
                double p = own is not null && g < own.Length && own[g] >= 0 ? own[g] : double.NaN;
                var sp = new double[nn]; var alive = new bool[nn]; var wt = new double[nn];
                for (int i = 0; i < nn; i++)
                {
                    if (i == j) continue;
                    var ds = sim.Nations[i].World.DiscoveryPrice;
                    if (ds is null || g >= ds.Length) continue;
                    sp[i] = ds[g]; wt[i] = sProd[i][g];
                    double dem = sDem[i][g], pr = sProd[i][g], net = sNet[i][g];
                    alive[i] = sPl[i] > 0 && dem > 1e-9 && pr + (net > 0 ? net : 0) > 1e-9;
                }
                var (a, b) = CfLandedAB(sp, alive, wt, km, c.TransportMargin, 1.0);
                double imp = haveTrade ? w.ImportsByGood![g] : double.NaN, exp = haveTrade ? w.ExportsByGood![g] : 0.0;
                double m = haveTrade ? (imp + prodVal[g] > 0 ? imp / (imp + prodVal[g]) : 0.0) : double.NaN;
                double l0 = a + 0.0 * b;
                int type = CfType(p, l0, haveTrade, exp);
                var st = CfStake(p, a, b, duty, m, law, type);
                cols.Add(($"cfPrice_g{id}", F(p))); cols.Add(($"cfLandedA_g{id}", F(a))); cols.Add(($"cfLandedB_g{id}", F(b))); cols.Add(($"cfImportShare_g{id}", F(m)));
                for (int bb = 0; bb < 3; bb++)
                {
                    double pr = prodRent[bb * nc + ci];
                    cols.Add(($"cfRentProd{bb}_g{id}", F(pr)));
                    sDown[bb] += pr * st.Down; sUp[bb] += pr * st.Up; sDownM[bb] += pr * st.DownTimesM; sUpPo[bb] += pr * st.UpPriceOnly;
                }
                cols.Add(($"cfTypeCode_g{id}", I(type)));
                typeRent[type] += prodRent[ci];   // ブロック 0（添字 0×nc＋ci）
                rent0 += prodRent[ci];
                if (!double.IsNaN(a) && p > 0) { pinDen0 += prodRent[ci]; if (p < a + duty.Now * b) pinRent0 += prodRent[ci]; }
            }
            var pref = new double[3]; var prefDn = new double[3]; var prefUp = new double[3];
            double coefRent = c.RentProtectionWeightCoef;
            double rentDomTot = 0; var rentDom = new double[3];
            foreach (var r in w.Regions) foreach (var pp in r.Pops) { double rn = System.Math.Max(0, pp.LandRentPerMonth.Value); rentDom[CnBloc(pp)] += rn; rentDomTot += rn; }
            for (int bb = 0; bb < 3; bb++)
            {
                double dn = CfOverAll(sDown[bb], trAll), up = CfOverAll(sUp[bb], trAll);
                double dnM = CfOverAll(sDownM[bb], trAll), upPo = CfOverAll(sUpPo[bb], trAll);
                double neg = bb == 0 ? 0.0 : 1.0;
                double share = rentDomTot > 1e-9 ? rentDom[bb] / rentDomTot : rentDom[bb];   // 今の式（cornint_s の engineRentShare と同じ規則）
                sDown[bb] = dn; sUp[bb] = up; sDownM[bb] = dnM; sUpPo[bb] = upPo;
                pref[bb] = coefRent * share - neg; prefDn[bb] = coefRent * dn - neg; prefUp[bb] = coefRent * up - neg;
            }
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfStakeDown{bb}", F(sDown[bb])));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfStakeUp{bb}", F(sUp[bb])));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfStakeDownTimesM{bb}", F(sDownM[bb])));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfStakeUpPriceOnly{bb}", F(sUpPo[bb])));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfCornPrefNow{bb}", F(pref[bb])));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfCornPrefDown{bb}", F(prefDn[bb])));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfCornPrefUp{bb}", F(prefUp[bb])));
            cols.Add(("cfRentType1Share0", F(rent0 > 0 ? typeRent[1] / rent0 : double.NaN)));
            cols.Add(("cfRentType2Share0", F(rent0 > 0 ? typeRent[2] / rent0 : double.NaN)));
            cols.Add(("cfRentType3Share0", F(rent0 > 0 ? typeRent[3] / rent0 : double.NaN)));
            cols.Add(("cfPinRatio0", F(pinDen0 > 0 ? pinRent0 / pinDen0 : double.NaN)));
            cols.Add(("cfHavePrice", I(own is not null ? 1 : 0)));
            cols.Add(("cfSellers", I(sellers)));
            for (int bb = 0; bb < 3; bb++) cols.Add(($"cfRentAll{bb}", F(allRent[bb])));
            cols.Add(("cfRentCrownShare", F(CfCrownShare(crown, priv))));
            if (!sk.Header) { sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Name))); sk.Header = true; }
            sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Val)));
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CorncfClose(MultiNationSim sim)
    {
        if (CorncfSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
