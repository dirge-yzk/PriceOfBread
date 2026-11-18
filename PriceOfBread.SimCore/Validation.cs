namespace PriceOfBread.SimCore;

public sealed record NationMetrics(
    string Name,
    double Industrialization,
    double MeanSatisfaction,
    double PriceFloorFrac,
    double PriceCeilFrac,
    double PriceMidFrac,
    double UrbanShare,
    double MeanLiteracy,
    double Population,
    double Treasury,
    int Colonies,
    bool SpawnedDuringPlay = false,
    int ActiveRegions = 0, int FoundingRegions = 0, double FoodFill = 0, double PopCount = 0,
    double SatRecentMean = double.NaN, double SatRecentSwing = double.NaN);

public sealed record SimMetrics(
    int Year,
    IReadOnlyList<NationMetrics> Nations,
    int DeadEndGoods,
    double IndustrializationSpread,
    int Wars,
    int TotalColonies,
    double TradeVolume,
    int WorldPricesMoved,
    int IndependentColonies = 0,
    int PromotedColonies = 0,
    int AbsorbedColonies = 0,
    int InitialColonies = -1,
    IReadOnlyList<string>? DeadEndNames = null)
{
    public double MeanMidFrac => Nations.Count > 0 ? Nations.Average(n => n.PriceMidFrac) : 0;
    public double WorstMidFrac => Nations.Count > 0 ? Nations.Min(n => n.PriceMidFrac) : 0;
    public double MeanSat => Nations.Count > 0 ? Nations.Average(n => n.MeanSatisfaction) : 0;
    // (1209) 満足度・最小国の判定の値＝直近 Validation.SatRecentQuarters 四半期の平均（SatRecentMean・SelfPlay が作る）。
    //   平均が無い国（窓を作らない経路＝Runner の Metrics.Compute 直・試験）は最後の一点（MeanSatisfaction）。
    public static double JudgedSat(NationMetrics n) => !(n.SatRecentMean >= 0) ? n.MeanSatisfaction : n.SatRecentMean;

    // 判定の国＝建国が途中でない国のうち JudgedSat の最小（無ければ全ての国から）。WorstSat・WorstSatName・WorstSatDetail はこの 1 国を読む。
    NationMetrics? WorstSatNation
    {
        get
        {
            NationMetrics? worst = null;
            foreach (var n in Nations) if (!n.SpawnedDuringPlay && (worst is null || JudgedSat(n) < JudgedSat(worst))) worst = n;
            if (worst is null) foreach (var n in Nations) if (worst is null || JudgedSat(n) < JudgedSat(worst)) worst = n;
            return worst;
        }
    }

    public double WorstSat => WorstSatNation is { } w ? JudgedSat(w) : 0;

    public string WorstSatDetail
    {
        get
        {
            var worst = WorstSatNation;
            return worst is null ? "" : $"・州{worst.ActiveRegions}/{worst.FoundingRegions}・主食{worst.FoodFill:F2}・人口{worst.PopCount:F0}"
                + (double.IsNaN(worst.SatRecentMean) ? "" : $"・直近{Validation.SatRecentQuarters}四半期 平均{worst.SatRecentMean:F2} 振れ{worst.SatRecentSwing:F2} 最後{worst.MeanSatisfaction:F2}");
        }
    }

    public string WorstSatName => WorstSatNation is { } w ? (w.SpawnedDuringPlay ? w.Name + "・分離独立" : w.Name) : "";
}

public static class Metrics
{
    private const double PriceFloorRefFrac = 0.051;
    private const double PriceCeilRefFrac = 19.9;
    private const double PriceMovedThreshold = 0.05;

    public static SimMetrics Compute(MultiNationSim sim)
    {
        var nations = new List<NationMetrics>();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var r = w.Region;
            double popS = 0, litS = 0;
            foreach (var p in w.AllPops()) { popS += p.Size; litS += p.Literacy * p.Size; }
            int floor = 0, ceil = 0, mid = 0, ng = 0;
            foreach (var g in r.Goods)
            {
                double price = r.Markets[g.Id].Price;
                if (price <= g.RefPrice * PriceFloorRefFrac) floor++;
                else if (price >= g.RefPrice * PriceCeilRefFrac) ceil++;
                else mid++;
                ng++;
            }
            nations.Add(new NationMetrics(
                w.Region.Name, r.TotalIndustrialization,
                NationalScore.MeanSatisfaction(w),
                ng > 0 ? (double)floor / ng : 0,
                ng > 0 ? (double)ceil / ng : 0,
                ng > 0 ? (double)mid / ng : 0,
                w.UrbanShare, popS > 0 ? litS / popS : 0,
                w.TotalPopulation, w.Treasury.Value, w.SelfPower?.Colonies ?? 0,
                (w.SelfPower?.SpawnedDuringPlay ?? false) || Validation.IsWarMauledRump(w) || !(popS > 0),
                Validation.ActiveRegionCount(w), w.FoundingActiveProvinces, w.Region.FoodFill, popS));
        }
        int deadEnd = 0;
        var deadEndNames = new List<string>();
        var unavailable = ProductionDiag.UnavailableGoods(sim, sim.Coef);
        foreach (var g in Catalog.Goods)
        {
            if (g.IsColonial) continue;
            if (g.Id == Catalog.MilitaryPowerId) continue;
            if (g.Id == Catalog.BuildingId && !(sim.Coef.BuildingGoodBundle > 0)) continue;
            if (g.Id < unavailable.Length && unavailable[g.Id]) continue;
            double maxD = 0;
            foreach (var n in sim.Nations) { var mk = n.World.Region.Markets; if (g.Id < mk.Length) maxD = Math.Max(maxD, mk[g.Id].LastDemand); }
            if (maxD < 0.5) { deadEnd++; deadEndNames.Add($"{g.Id}:{g.Name}"); }
        }
        double indMax = 0, indMin = double.MaxValue;
        foreach (var x in nations) { indMax = Math.Max(indMax, x.Industrialization); indMin = Math.Min(indMin, x.Industrialization); }
        double indSpread = nations.Count > 0 ? indMax - indMin : 0;
        int wars = sim.Diplomacy.WarsStarted;
        int totalCol = 0; foreach (var p in sim.Diplomacy.Powers) totalCol += p.Colonies;
        double tradeVol = 0; foreach (var v in sim.Market.Volume) tradeVol += v;
        int moved = 0;
        for (int g = 0; g < sim.Market.Price.Count; g++)
        {
            double init = g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 1.0;
            if (Math.Abs(sim.Market.Price[g] / init - 1.0) > PriceMovedThreshold) moved++;
        }
        return new SimMetrics(sim.Year, nations, deadEnd, indSpread, wars, totalCol, tradeVol, moved,
            sim.Diplomacy.SovereignColonialRegions, sim.Diplomacy.PromotedColonies, sim.Diplomacy.AbsorbedColonies, sim.Diplomacy.InitialColonyCount, deadEndNames);
    }
}

public enum Verdict { Pass, Warn, Fail }

public sealed record Check(string Name, Verdict Verdict, string Value, string Target);

public static class Validation
{
    public const int SatRecentQuarters = 8;

    public static List<Check> Evaluate(SimMetrics m)
    {
        var checks = new List<Check>();
        void Higher(string name, double v, double pass, double warn, string fmt = "F2")
            => checks.Add(new Check(name, v >= pass ? Verdict.Pass : v >= warn ? Verdict.Warn : Verdict.Fail,
                v.ToString(fmt), $"≥{pass.ToString(fmt)}合格 / ≥{warn.ToString(fmt)}注意"));
        void Fewer(string name, int v, int pass, int warn, string detail = "")
            => checks.Add(new Check(name, v <= pass ? Verdict.Pass : v <= warn ? Verdict.Warn : Verdict.Fail,
                v + detail, $"≤{pass}合格 / ≤{warn}注意"));
        void Truth(string name, bool ok, string val)
            => checks.Add(new Check(name, ok ? Verdict.Pass : Verdict.Fail, val, "成立＝合格"));

        Higher("価格中間帯・平均", m.MeanMidFrac, 0.40, 0.30);
        Higher("価格中間帯・最小国", m.WorstMidFrac, 0.25, 0.15);
        Fewer("下流の無い財", m.DeadEndGoods, 0, 2, m.DeadEndNames is { Count: > 0 } dn ? $"＝{string.Join("・", dn)}" : "");
        Higher("満足度・平均", m.MeanSat, 0.24, 0.09);
        checks.Add(new Check("満足度・最小国",
            m.WorstSat >= 0.05 ? Verdict.Pass : m.WorstSat >= 0.02 ? Verdict.Warn : Verdict.Fail,
            $"{m.WorstSat:F2}（{m.WorstSatName}{m.WorstSatDetail}）", "≥0.05合格 / ≥0.02注意"));
        Higher("工業化の差別化", m.IndustrializationSpread, 0.50, 0.20);
        Truth("戦争が起きる", m.Wars >= 1, $"{m.Wars}件");
        Truth("交易が成立", m.TradeVolume > 0.01, m.TradeVolume.ToString("F2"));
        Higher("世界価格の発見", m.WorldPricesMoved, 11, 5, "F0");
        if (m.InitialColonies >= 0)
            Truth("植民地の保存", m.TotalColonies + m.IndependentColonies + m.PromotedColonies + m.AbsorbedColonies == m.InitialColonies, $"{m.TotalColonies}+独立{m.IndependentColonies}+昇格{m.PromotedColonies}+吸収{m.AbsorbedColonies}＝初期{m.InitialColonies}");
        return checks;
    }

    public static int Count(IEnumerable<Check> checks, Verdict v)
    {
        int c = 0; foreach (var ch in checks) if (ch.Verdict == v) c++; return c;
    }

    internal static int ActiveRegionCount(World w)
    {
        return w.Regions.Count;
    }

    internal static bool IsWarMauledRump(World w)
    {
        if (w.FoundingPopulation > 0 && w.TotalPopulation * 2 < w.FoundingPopulation) return true;
        if (w.FoundingActiveProvinces <= 0) return false;
        return w.Regions.Count * 2 < w.FoundingActiveProvinces;
    }

    public static string? LawOutOfRange(World w)
    {
        var p = w.Policy;
        if (!System.Enum.IsDefined(w.EducationLaw) || !System.Enum.IsDefined(p.EducationLaw)) return "教育法";
        if (!System.Enum.IsDefined(w.Suffrage) || !System.Enum.IsDefined(p.Suffrage)) return "参政権";
        if (!System.Enum.IsDefined(w.WomenRights) || !System.Enum.IsDefined(p.WomenRights)) return "女性の権利";
        if (!System.Enum.IsDefined(w.MonetaryLaw)) return "通貨制度";
        if (!System.Enum.IsDefined(w.Censorship) || !System.Enum.IsDefined(p.Censorship)) return "報道";
        if (!System.Enum.IsDefined(w.LaborUnion) || !System.Enum.IsDefined(p.LaborUnion)) return "労組";
        if (!System.Enum.IsDefined(w.WorkHours) || !System.Enum.IsDefined(p.WorkHours)) return "労働時間";
        if (!System.Enum.IsDefined(w.CornLaws) || !System.Enum.IsDefined(p.CornLaws)) return "穀物法";
        if (!System.Enum.IsDefined(w.LandReform) || !System.Enum.IsDefined(p.LandReform)) return "土地改革";
        if (!System.Enum.IsDefined(w.SerfEmancipation) || !System.Enum.IsDefined(p.SerfEmancipation)) return "農奴解放";
        if (!System.Enum.IsDefined(w.SocialSecurityLaw) || !System.Enum.IsDefined(p.SocialSecurityLaw)) return "社会保障法";
        if (!System.Enum.IsDefined(w.ConscriptionLaw) || !System.Enum.IsDefined(p.ConscriptionLaw)) return "徴兵法";
        if (!System.Enum.IsDefined(w.SecretBallot) || !System.Enum.IsDefined(p.SecretBallot)) return "秘密投票法";
        if (!System.Enum.IsDefined(w.Bureaucracy) || !System.Enum.IsDefined(p.Bureaucracy)) return "官僚制度";
        if (!System.Enum.IsDefined(w.ImmigrationRestriction) || !System.Enum.IsDefined(p.ImmigrationRestriction)) return "移民制限法";
        if (!System.Enum.IsDefined(w.SpecialistExitLaw) || !System.Enum.IsDefined(p.SpecialistExitLaw)) return "技術者流出禁止法";
        if (!System.Enum.IsDefined(w.StateReligion) || !System.Enum.IsDefined(p.StateReligion)) return "国教法";
        if (!System.Enum.IsDefined(w.Antitrust) || !System.Enum.IsDefined(p.Antitrust)) return "独占禁止法";
        if (!System.Enum.IsDefined(w.Guild) || !System.Enum.IsDefined(p.Guild)) return "同業組合法";
        if (!System.Enum.IsDefined(w.MinimumWage) || !System.Enum.IsDefined(p.MinimumWage)) return "最低賃金法";
        if (!System.Enum.IsDefined(w.Inheritance) || !System.Enum.IsDefined(p.Inheritance)) return "相続法";
        if (!System.Enum.IsDefined(w.OpiumPossession) || !System.Enum.IsDefined(p.OpiumPossession)) return "阿片の保有法";
        if (!System.Enum.IsDefined(w.ColonialSuffrage) || !System.Enum.IsDefined(p.ColonialSuffrage)) return "植民地参政権";
        if (!System.Enum.IsDefined(w.MigrantNativeSuffrage) || !System.Enum.IsDefined(p.MigrantNativeSuffrage)) return "移民原住民参政権";
        return null;
    }

}
