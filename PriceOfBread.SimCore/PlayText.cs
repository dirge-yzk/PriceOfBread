namespace PriceOfBread.SimCore;

public static class PlayText
{
    public static readonly string[] MilitancyPartNames = ["政治的疎外", "厭戦", "民族的地位", "飢餓"];

    public static string GateLine(bool ok, double cost, string reason)
        => ok ? $"制定:可能（政治資本消費:{cost * 100:F0}）"
        : reason == Government.ReasonNoCapitalStock ? $"制定:不可（政治資本消費:{cost * 100:F0}　不足）"
        : reason == Government.ReasonNoLegitimacy ? $"制定:不可（政治資本消費:{cost * 100:F0}　{reason}）"
        : $"制定:不可（{reason}）";

    public static bool IsPrereqStop(bool ok, string reason) => !ok && reason.Length > 0 && !Government.IsPoliticalGateReason(reason);

    public static string FormatPrereq(string reason)
    {
        const string tail = "）が前提";
        int open = reason.IndexOf('（');
        if (open > 0 && reason.EndsWith(tail, System.StringComparison.Ordinal) && open + 1 < reason.Length - tail.Length)
        {
            string state = reason[(open + 1)..^tail.Length];
            if (!state.Contains('（') && !state.Contains('）'))
                return $"前提 {reason[..open]}:{state}";
        }
        return reason;
    }

    public static string NextStepLine(bool ok, double cost, string reason)
        => IsPrereqStop(ok, reason)
            ? "制定:不可　" + FormatPrereq(reason)
            : GateLine(ok, cost, reason);


    public static string PolityRegimeJa(Polity? p) => p switch
    {
        Polity.Absolutist => "絶対君主制",
        Polity.Despotic => "専制君主制",
        Polity.ConstitutionalMonarchy => "立憲君主制",
        Polity.Shogunate => "幕府",
        Polity.Republic => "共和制",
        Polity.BourgeoisDictatorship => "ブルジョア独裁",
        Polity.ProletarianDictatorship => "プロレタリア独裁",
        Polity.MilitaryDictatorship => "軍事独裁",
        Polity.Anarchy => "無政府",
        _ => "—",
    };

    public readonly record struct EndingStandingRow(int Order, string Name, double Score, Polity Polity, double Population, double IronShare, bool IsPlayer);

    public static bool IsEnded(World w) => IsEnded(w.Year);

    public static bool IsEnded(int year) => year >= Calendar.EndYear;

    public static string WarResultWord(bool? won) => won is null ? "白紙講和" : won.Value ? "勝利" : "敗北";

    public static List<EndingStandingRow> EndingStandings(IEnumerable<World> worlds, World player)
    {
        var wl = worlds.ToList();
        var shares = new double[wl.Count][];
        for (int k = 0; k < wl.Count; k++) shares[k] = new double[NationalScore.ScoreTerms.Count];
        var sc = NationalScore.ShareScores(wl, shares);
        var order = Enumerable.Range(0, wl.Count).OrderByDescending(k => sc[k]).ToList();
        var rows = new List<EndingStandingRow>(wl.Count);
        for (int i = 0; i < order.Count; i++)
        {
            var w = wl[order[i]];
            rows.Add(new EndingStandingRow(i + 1, w.SelfPower?.Name ?? w.Region.Name, sc[order[i]],
                PowerAffinity.PolityFromShares(w),
                w.TotalPopulation, shares[order[i]][NationalScore.ScoreTerms.IronIndex], ReferenceEquals(w, player)));
        }
        return rows;
    }

    public static string ScoreBreakdownText(World player)
    {
        var ws = NationalScore.NationWorldsOf(player);
        int ip = ws.IndexOf(player);
        var sc = NationalScore.Scores(ws);
        var lines = NationalScore.Breakdowns(ws)[ip];
        double mine = sc[ip], top = 0; string topName = "";
        for (int i = 0; i < ws.Count; i++)
            if (sc[i] > top) { top = sc[i]; topName = ws[i].SelfPower?.Name ?? ""; }
        string gap = top > mine + 1e-9 ? $"首位 {topName} との差 {top - mine:F1}" : "首位";
        return string.Join("\n", lines.Select(l => $"{l.Label} {l.Points:F1}")) + $"\n合計 {mine:F1}　{gap}";
    }

    public static List<ExplainLedger.Term> ExplainTerms(World w, string quantity)
    {
        var rows = new List<ExplainLedger.Term>();
        if (w.Explain is not { } led) return rows;
        foreach (var t in led.Get(quantity).Where(t => System.Math.Abs(t.Delta) >= 5e-4).OrderByDescending(t => t.Delta)) rows.Add(t);
        return rows;
    }

    public static string SignedLine(double delta, string reason, System.Func<double, string> fmt)
        => $"{(delta >= 0 ? "＋" : "−")}{fmt(System.Math.Abs(delta))}　{reason}";

    public const string CapitalistClass = "資本家", BureaucratClass = "官僚", ArtisanClass = "職人", MerchantClass = "商人";

    public static int ClassLayerOf(Pop p)
        => (p.Closed || p.ClassName == CapitalistClass) ? 0 : (p.IsModernElite || p.ClassName is BureaucratClass or ArtisanClass or MerchantClass) ? 1 : 2;

    public static readonly string[] ClassLayerNames = ["上流", "中流", "下流"];

    public static List<(string Layer, double Mean)> ClassLayerMeans(World w, System.Func<Pop, double> selector)
    {
        double[] mass = new double[3], sz = new double[3];
        foreach (var p in w.AllPops()) { int layer = ClassLayerOf(p); mass[layer] += p.Size * selector(p); sz[layer] += p.Size; }
        var rows = new List<(string, double)>();
        for (int i = 0; i < 3; i++) if (sz[i] > 1e-9) rows.Add((ClassLayerNames[i], mass[i] / sz[i]));
        return rows;
    }

    public static string PriceLevelText(World w) => !w.MonetaryEnabled ? "—" : w.PriceLevel.ToString("P0");
    public static string ExchangeRateText(World w) => !w.MonetaryEnabled ? "—" : w.ExchangeRate.ToString("P0");
    public static string EducationSpendingText(World w) => $"教育費（予算の割合） {w.Policy.EducationSpendingTarget:P0}　実効 {w.EducationSpending:P0}" + (w.Coef.ScholarshipTopStage > 0 ? $"　奨学金 {EconomyStep.ScholarshipCover(w.Region, w.Coef):P0}" : "");

    public static string CoverText(World w)
        => w.Bank is null ? "—" : $"{w.SpecieCoverRatio():P0}";

    public static string PoliticalCapitalText(World w)
    {
        double bMax = w.PoliticalCapitalMax, aNow = System.Math.Max(0, bMax - w.MandateSpent);
        return w.MandateSpent > bMax ? $"{aNow * 100:F0} / {bMax * 100:F0}（超過 {(w.MandateSpent - bMax) * 100:F0}）" : $"{aNow * 100:F0} / {bMax * 100:F0}";
    }

    public static string PoliticalCapitalTip(World w)
    {
        var c = w.Coef;
        double bMax = w.PoliticalCapitalMax;
        double aff = Ideologies.SupportAt(Ideologies.National(w), PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
        double sup = System.Math.Max(0, System.Math.Min(1, (aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin)));
        double pe = PowerShares.ParliamentEffective(w);
        double seats = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];
        double maxSeat = System.Math.Max(w.PartySeats[0], System.Math.Max(w.PartySeats[1], w.PartySeats[2]));
        double split = seats > 1e-9 ? System.Math.Max(0, System.Math.Min(1, 1.0 - maxSeat / seats)) : 0;
        double rate = PoliticalQuantities.PoliticalCapitalRate(w);
        double toFull = !(rate > 0) ? 0 : c.MandateRefillQuarters / rate * (bMax > 0 && w.MandateSpent > bMax ? w.MandateSpent / bMax : 1.0);
        return $"正統性 {w.Legitimacy:F2}　現政体の支持 {sup:F2}"
             + $"\n議会の実効統制 {pe:F2}　議席の分裂 {split:F2}　内戦 {(w.InCivilWar ? 1 : 0)}"
             + $"\n実行度 {rate:F2}　満タンまでの四半期 {toFull:F0}";
    }
}
