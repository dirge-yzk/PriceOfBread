namespace PriceOfBread.SimCore;

// (1349) c142ds 計器 migcap_s：州ごと・四半期の末に、移民の計画の流入（受け入れの上限を掛ける前）と受け入れの上限を、
// 行き先の値打ちの旧の規則（機会のまま）と新の規則（機会×食の充足＝旗 DestFoodSymmetric 1）の両方で同じ盤面に並べる。国内（1 か月分）と越境（1 四半期分）を分ける。
// 式は事前登録 measurements/m1349_20261125_prereg_ds.txt の〈計器〉の文から計器が自分で組む（エンジンの Territory.Step・MigrateBetweenNations・
// StayValue・Holding・LandFactor・AbsorbCap・EntryFrictionTerm・MigrationDecay を呼ばない）。入力として読むのは機会（Territory.Opportunity＝region_s と同じ）・
// 座標（Places.Coord）・距離（Geo.DistanceKm）・時代の係数（Coefficients.EraFactor）・平均識字（PoliticalQuantities.AvgLiteracy）だけ。
// 四半期の末の盤面での計画＝国内の Step は月ごと＝3 か月目の後の盤面の 1 か月分〈近似〉。力学の値を 1 つも書かない・乱数を引かない・新しい状態を足さない。
public static partial class Instruments
{
    internal const string MigcapHeaderText = "quarter\tyear\tnation\tregion\tpopulationK\tfoodFillSmoothed\topportunityIndex"
        + "\tdomPlanOldKPerMonth\tdomPlanNewKPerMonth\tdomCapKPerMonth\tdomRatioOld\tdomRatioNew\tdomBindOld\tdomBindNew"
        + "\txbPlanOldKPerQuarter\txbPlanNewKPerQuarter\txbCapKPerQuarter\txbRatioOld\txbRatioNew\txbBindOld\txbBindNew\tdestFoodSymmetricFlag";

    internal readonly record struct MigcapRow(World W, Region R, double Food, double Opp,
        double DomPlanOld, double DomPlanNew, double DomCap, double XbPlanOld, double XbPlanNew, double XbCap);

    sealed class McSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, McSink> McSinks = new();

    static double McLand(Region r, double exponent)
    {
        if (r.ArableLand >= 1e8) return 1.0;
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        double x = System.Math.Pow(r.ArableLand / System.Math.Max(1.0, rural), exponent);
        return x < 1.0 ? x : 1.0;
    }

    static double McHold(Region r, double exponent)
    {
        if (r.ArableLand >= 1e8) return 1.0;
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        return System.Math.Pow(System.Math.Max(1.0, r.ArableLand / System.Math.Max(1.0, rural)), exponent);
    }

    static double McUnit(double v) => v > 1.0 ? 1.0 : v > 0.0 ? v : 0.0;   // NaN は 0

    static double McAbsorb(Region r, Coefficients c)
    {
        if (!(c.ImmigrationAbsorbExponent > 0)) return 1.0;
        double slack = McUnit(r.FoodFill) * McUnit(r.HousingFill) * McLand(r, c.LandDiminishExponent);
        return slack > 0 ? System.Math.Pow(slack, c.ImmigrationAbsorbExponent) : 0.0;
    }

    static double McEra(World w, Coefficients c) => c.EraFactor(w.WorldTechLevel, c.EraMigrationScale);   // 距離の尺度の時代の係数（エンジンの移民と同じ入力＝その国の世界の技術先端）

    static int McClasses(Region r) { int m = 0; foreach (var p in r.Pops) if (!p.IsSoldier) m |= 1 << p.ClassId; return m; }

    /// <summary>全ての国の全ての州の行（試験の読み口）。</summary>
    internal static List<MigcapRow> MigcapRows(MultiNationSim sim)
    {
        var rows = new List<MigcapRow>();
        var all = new List<(int N, World W, Region R, double Opp, double OppX, double Lat, double Lon, int Cls)>();
        var sc = sim.Coef;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            foreach (var r in w.Regions)
            {
                var (la, lo) = Places.Coord(r.Name);
                all.Add((i, w, r, Territory.Opportunity(r, w.Coef), Territory.Opportunity(r, sc), la, lo, McClasses(r)));   // 国内は国の係数・越境は共有の係数（エンジンと同じ持ち主）
            }
        }
        int m = all.Count;
        var domOld = new double[m]; var domNew = new double[m]; var domCap = new double[m];
        // 国内：国ごとに、出る州の残る値打ち（機会×持ち分×〔旗 DomesticStayFoodFill 1 なら〕食の充足）と行き先の値打ち（旧＝機会・新＝機会×食の充足）の差から
        // 摩擦と距離の項（摩擦×km÷(距離の尺度×時代の係数)）を引き、正の差の比で 1 か月の流出の割合（移動度×最大の差・0.2 まで）を配る。量は行き先に居る階級の大衆。
        int start = 0;
        while (start < m)
        {
            int end = start; while (end < m && all[end].N == all[start].N) end++;
            var w = all[start].W; var c = w.Coef;
            if (end - start >= 2)
            {
                var gap = new double[end - start];
                double eraD = c.MigrationDistanceScale > 0 ? c.MigrationDistanceScale * McEra(w, c) : 0.0;
                for (int s = start; s < end; s++)
                {
                    var rs = all[s].R;
                    double stay = all[s].Opp * McHold(rs, c.LandDiminishExponent) * (c.DomesticStayFoodFill > 0 ? rs.FoodFill : 1.0);
                    if (!double.IsFinite(stay)) continue;
                    for (int rule = 0; rule < 2; rule++)
                    {
                        double gMax = 0, wSum = 0;
                        System.Array.Clear(gap);
                        for (int d = start; d < end; d++)
                        {
                            if (d == s) continue;
                            double val = rule == 0 ? all[d].Opp : all[d].Opp * all[d].R.FoodFill;
                            if (!double.IsFinite(val)) continue;
                            double g = val - stay - c.TransportFriction;
                            if (!(g > 0)) continue;
                            if (eraD > 0) g -= c.TransportFriction * Geo.DistanceKm(all[s].Lat, all[s].Lon, all[d].Lat, all[d].Lon) / eraD;
                            if (!(g > 0)) continue;
                            gap[d - start] = g; wSum += g; if (g > gMax) gMax = g;
                        }
                        if (!(wSum > 0)) continue;
                        double frac = c.RegionMobility * gMax; if (!(frac > 0)) frac = 0; if (frac > 0.2) frac = 0.2;
                        for (int d = start; d < end; d++)
                        {
                            double g = gap[d - start];
                            if (!(g > 0)) continue;
                            double mov = 0;
                            foreach (var p in rs.Pops) if (p.IsCommoner && (all[d].Cls >> p.ClassId & 1) != 0) mov += p.Size;
                            double f = frac * (g / wSum) * mov;
                            if (rule == 0) domOld[d] += f; else domNew[d] += f;
                        }
                    }
                }
            }
            for (int d = start; d < end; d++)
            {
                var r = all[d].R;
                domCap[d] = Calendar.PerQuarterToPerMonth(c.ImmigrationReceiveCapPerQuarter).Value * r.SumPopulation() * McAbsorb(r, c);
            }
            start = end;
        }
        // 越境：送出の州の残る値打ち（機会×持ち分×食の充足）と他の国の行き先の値打ち（旧＝機会・新＝機会×食の充足）の差から閾値を引き、
        // 距離の減衰 1/(1＋km÷(距離の尺度×送出の国の時代の係数)) を掛け、受入国の重み（民族の引力×識字の差別）で配る。流量＝min(流出率×最大の差, 四半期の上限×(1＋飢饉の緩和×(1−食の充足)))×その階級の人数（農民・労働者）。
        var dstA = new double[sim.Nations.Count]; var lawCap = new double[sim.Nations.Count];
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            dstA[i] = w.EthnicEnabled ? (1.0 + sc.EthnicDraw * (1.0 - w.EthnicUnity)) * McUnit(PoliticalQuantities.AvgLiteracy(w) / sc.DiscriminationLiteracy) : 1.0;
            lawCap[i] = w.ImmigrationRestriction == ImmigrationLaw.Selective ? sc.ImmigrationSelectiveFrac.Value : w.ImmigrationRestriction == ImmigrationLaw.Closed ? sc.ImmigrationClosedFrac.Value : 1.0;
        }
        var xbOld = new double[m]; var xbNew = new double[m]; var xbCap = new double[m];
        var km = new double[m]; var gx = new double[m];
        int[] cls2 = [PopClass.Peasant, PopClass.Worker];
        for (int s = 0; s < m; s++)
        {
            var rs = all[s].R; var ws = all[s].W;
            double push = all[s].OppX * McHold(rs, sc.LandDiminishExponent) * rs.FoodFill;
            if (!double.IsFinite(push)) continue;
            double eraX = sc.MigrationDistanceScale > 0 ? sc.MigrationDistanceScale * McEra(ws, sc) : 0.0;
            for (int x = 0; x < m; x++) km[x] = all[x].N == all[s].N || !double.IsFinite(all[x].OppX) ? double.NaN : Geo.DistanceKm(all[s].Lat, all[s].Lon, all[x].Lat, all[x].Lon);
            for (int rule = 0; rule < 2; rule++)
            {
                double gMax = 0, wSum = 0;
                for (int x = 0; x < m; x++)
                {
                    gx[x] = 0;
                    if (all[x].N == all[s].N || !double.IsFinite(all[x].OppX)) continue;
                    double val = rule == 0 ? all[x].OppX : all[x].OppX * all[x].R.FoodFill;
                    double g = val - push - sc.CrossBorderOppGapThreshold;
                    if (!(g > 0)) continue;
                    if (eraX > 0) g *= 1.0 / (1.0 + km[x] / eraX);
                    if (!(g > 0)) continue;
                    gx[x] = g; if (g > gMax) gMax = g; wSum += g * dstA[all[x].N];
                }
                if (!(wSum > 0)) continue;
                double famine = 1.0 - rs.FoodFill; if (!(famine > 0)) famine = 0;
                double maxFrac = sc.EmigrationMaxPerQuarter.Value * (1.0 + sc.FaminePushMaxFracBoost * famine);
                foreach (int cl in cls2)
                {
                    var sp = rs.PopOf(cl);
                    if (sp is null || !(sp.Size >= 1e-9)) continue;
                    double flow = System.Math.Min(sc.EmigrationRate * gMax, maxFrac) * sp.Size;
                    if (!(flow > 1e-9)) continue;
                    for (int x = 0; x < m; x++)
                    {
                        if (!(gx[x] > 0) || (all[x].Cls >> cl & 1) == 0) continue;
                        double f = flow * (gx[x] * dstA[all[x].N] / wSum);
                        if (!(f > 0)) continue;
                        if (rule == 0) xbOld[x] += f; else xbNew[x] += f;
                    }
                }
            }
        }
        for (int x = 0; x < m; x++)
        {
            var r = all[x].R;
            xbCap[x] = sc.ImmigrationReceiveCapPerQuarter.Value * r.SumPopulation() * lawCap[all[x].N] * McAbsorb(r, sc);
            rows.Add(new MigcapRow(all[x].W, r, r.FoodFill, all[x].Opp, domOld[x], domNew[x], domCap[x], xbOld[x], xbNew[x], xbCap[x]));
        }
        return rows;
    }

    static string McRatio(double plan, double cap) => F(cap > 0 ? plan / cap : plan > 0 ? double.PositiveInfinity : 0.0);
    static string McBind(double plan, double cap) => plan > cap ? "1" : "0";

    /// <summary>四半期の末：全ての国の全ての州に 1 行。</summary>
    static void MigcapTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("migcap_s")) return;
        var sk = McSinks.GetValue(sim, _ => new McSink());
        if (sk.W is null) { sk.W = Open("migcap", seed); sk.W.WriteLine(MigcapHeaderText); }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string flag = (sim.Coef.DestFoodSymmetric > 0 ? 1 : 0).ToString(inv);
        string year = sim.Nations[0].World.Year.ToString(inv), qs = q.ToString(inv);
        foreach (var x in MigcapRows(sim))
            sk.W.WriteLine(string.Join('\t', qs, year, NameOf(x.W), x.R.Name, F(x.R.SumPopulation()), F(x.Food), F(x.Opp),
                F(x.DomPlanOld), F(x.DomPlanNew), F(x.DomCap), McRatio(x.DomPlanOld, x.DomCap), McRatio(x.DomPlanNew, x.DomCap), McBind(x.DomPlanOld, x.DomCap), McBind(x.DomPlanNew, x.DomCap),
                F(x.XbPlanOld), F(x.XbPlanNew), F(x.XbCap), McRatio(x.XbPlanOld, x.XbCap), McRatio(x.XbPlanNew, x.XbCap), McBind(x.XbPlanOld, x.XbCap), McBind(x.XbPlanNew, x.XbCap), flag));
    }

    static void MigcapClose(MultiNationSim sim) { if (McSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
