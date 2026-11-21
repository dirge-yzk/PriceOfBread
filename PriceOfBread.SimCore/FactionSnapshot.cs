namespace PriceOfBread.SimCore;

public static class FactionSnapshot
{
    public const double MilitancyThreshold = 0.5;
    public const double MinSizeFraction = 0.01;
    public const int TopK = 3;
    public const double MinDemandScore = 0.05;

    public readonly record struct Demand(PolicyAxis Axis, bool Increase, double Score);

    public sealed class Faction
    {
        public string Name = "";
        public double Size;
        public double AvgMilitancy;
        public int Bloc;
        public List<Demand> Demands = [];
    }

    internal static List<(Region Region, int Bloc, int Ideo, double Size, double MilSum)> ComputeClusters(World w, bool countCost = false)
    {
        var clusters = new Dictionary<(int Region, int Bloc, int Ideo), (Region Region, double Size, double MilSum)>();
        var order = new List<(int Region, int Bloc, int Ideo)>();
        var (coord, lawGap) = MembershipContext(w);
        int seen = 0;
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Pops)
            {
                if (p.Size <= 1e-9) continue;
                seen++;
                double part = Participation(w, p, coord, lawGap);
                if (!(part > 0)) continue;
                var (bloc, ideo) = BlocIdeo(p);
                var key = (r.Id, bloc, ideo);
                if (clusters.TryGetValue(key, out var cur))
                    clusters[key] = (cur.Region, cur.Size + p.Size * part, cur.MilSum + p.Militancy * p.Size * part);
                else { clusters[key] = (r, p.Size * part, p.Militancy * p.Size * part); order.Add(key); }
            }
        }
        if (countCost) w.Cost[CostLedger.ParticipationPops] += seen;
        var result = new List<(Region, int, int, double, double)>(order.Count);
        foreach (var key in SortBySizeDesc(order, clusters))
        {
            var (reg, size, milSum) = clusters[key];
            result.Add((reg, key.Bloc, key.Ideo, size, milSum));
        }
        return result;
    }

    internal static (double[] Coord, double[] LawGap) MembershipContext(World w) => MembershipContext(w, true);

    internal static (double[] Coord, double[] LawGap) MembershipContext(World w, bool byDiscontent)
    {
        var c = w.Coef;
        double[] coord = byDiscontent ? PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c) : [];
        double[] lawGap = byDiscontent && c.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
        return (coord, lawGap);
    }

    internal static double DiscontentOf(World w, Pop p, double[] coord, double[] lawGap)
        => Politics.Discontent(w, w.Coef, p, coord, lawGap.Length > 0 ? lawGap[PolicyPreferences.BlocOf(p)] : 0);

    internal static double Participation(World w, Pop p, double[] coord, double[] lawGap)
    {
        return Math.Max(DiscontentOf(w, p, coord, lawGap), p.Militancy);
    }

    internal static (int Bloc, int Ideo) BlocIdeo(Pop p)
        => (PolicyPreferences.BlocOf(p), ArgMax(p.IdeologyShares));

    public static List<Faction> Compute(World w)
    {
        double totalPop = w.TotalPopulation;
        if (totalPop <= 1e-9) return [];
        var clusters = ComputeClusters(w);
        if (clusters.Count == 0) return [];

        var demandsByBloc = new List<Demand>?[PolicyPreferences.BlocCount];
        var result = new List<Faction>();
        foreach (var (reg, bloc, ideoIdx, size, milSum) in clusters)
        {
            if (size < MinSizeFraction * totalPop) continue;
            demandsByBloc[bloc] ??= ComputeDemands(w, bloc);
            var ideo = (Ideology)ideoIdx;
            result.Add(new Faction
            {
                Name = $"{reg.Name}の{IdeologyJa(ideo)}{BlocJa(bloc)}",
                Size = size,
                AvgMilitancy = milSum / size,
                Bloc = bloc,
                Demands = demandsByBloc[bloc]!,
            });
        }
        return result;
    }

    private static IEnumerable<(int Region, int Bloc, int Ideo)> SortBySizeDesc(
        List<(int Region, int Bloc, int Ideo)> order,
        Dictionary<(int Region, int Bloc, int Ideo), (Region Region, double Size, double MilSum)> clusters)
    {
        var arr = order.ToArray();
        var idx = new int[arr.Length];
        for (int i = 0; i < idx.Length; i++) idx[i] = i;
        System.Array.Sort(idx, (a, b) =>
        {
            int cmp = clusters[arr[b]].Size.CompareTo(clusters[arr[a]].Size);
            return cmp != 0 ? cmp : a.CompareTo(b);
        });
        foreach (int i in idx) yield return arr[i];
    }

    internal static readonly PolicyAxis[] Axes = System.Enum.GetValues<PolicyAxis>();

    internal static bool IsInstantAxis(PolicyAxis a) => a is PolicyAxis.Coercion or PolicyAxis.NavyUpkeep or PolicyAxis.Tariff;

    /// <summary>1 軸の要求の点＝選好の強さ×残りの可動域÷(1＋反対の力)。向きは increase へ。ComputeDemands の 1 軸分（順位づけの前）。</summary>
    internal static double AxisScore(World w, PolicyAxis axis, int bloc, in PolicyPreferences.Profile e, out bool increase)
    {
        double prefInc = PolicyPreferences.Pref(axis, bloc, e, w);
        increase = prefInc >= 0;
        double strength = System.Math.Abs(prefInc);
        if (!increase && PolicyPreferences.DirectionAware(w, axis, bloc)) strength = System.Math.Abs(PolicyPreferences.Pref(axis, bloc, e, w, false));
        double score = strength * Headroom(w, axis, increase);
        score /= 1.0 + PolicyPreferences.OpposingPower(w, axis, increase, e);
        return score;
    }

    public static List<Demand> ComputeDemands(World w, int bloc, bool lawsOnly = false)
    {
        var list = new List<Demand>();
        var e = PolicyPreferences.Build(w);
        foreach (var axis in Axes)
        {
            if (axis == PolicyAxis.NationalizationAll) continue;
            if (lawsOnly && IsInstantAxis(axis)) continue;
            double score = AxisScore(w, axis, bloc, e, out bool increase);
            if (score > MinDemandScore) list.Add(new Demand(axis, increase, score));
        }
        list.Sort((a, b) => { int cmp = b.Score.CompareTo(a.Score); return cmp != 0 ? cmp : a.Axis.CompareTo(b.Axis); });
        if (list.Count > TopK) list.RemoveRange(TopK, list.Count - TopK);
        return list;
    }

    internal static double Headroom(World w, PolicyAxis a, bool increase)
    {
        double v = AxisPosition(w, a);
        return increase ? 1 - v : v;
    }

    internal static double AxisPosition(World w, PolicyAxis a)
    {
        double v = a switch
        {
            PolicyAxis.SocialSecurity => w.SocialSecurity,
            PolicyAxis.Coercion => PoliticalQuantities.Clamp01(w.BaseCoercion / System.Math.Max(1e-9, w.Coef.CoercionMax)),
            PolicyAxis.Tariff => PoliticalQuantities.Clamp01(w.Tariff),
            PolicyAxis.NavyUpkeep => w.NavyUpkeep.Value,
            PolicyAxis.NationalizationAll => 0.5,
            PolicyAxis.Nationalization => NationalizationLitRate(w),
            _ => Government.LevelPosition(w, a),
        };
        return PoliticalQuantities.Clamp01(v);
    }

    private static double NationalizationLitRate(World w)
    {
        int lit = (w.NationalizeRailways ? 1 : 0) + (w.NationalizeMines ? 1 : 0) + (w.NationalizeMilitary ? 1 : 0)
                + (w.NationalizeOil ? 1 : 0) + (w.NationalizeElectricity ? 1 : 0) + (w.NationalizeAll ? 1 : 0);
        return lit / 6.0;
    }

    internal static int ArgMax(double[] shares)
    {
        int best = 0; double bv = shares[0];
        for (int i = 1; i < shares.Length; i++) if (shares[i] > bv) { bv = shares[i]; best = i; }
        return best;
    }

    public static string IdeologyJa(Ideology i) => i switch
    {
        Ideology.Traditionalism => "伝統派",
        Ideology.Reactionism => "反動派",
        Ideology.Liberalism => "自由主義",
        Ideology.Republicanism => "共和主義",
        Ideology.Nationalism => "国民主義",
        Ideology.Socialism => "社会主義",
        Ideology.Communism => "共産主義",
        Ideology.Anarchism => "無政府主義",
        Ideology.Imperialism => "帝国主義",
        Ideology.Fascism => "ファシズム",
        Ideology.Royalism => "王党派",
        _ => "",
    };

    public static string BlocJa(int bloc) => bloc switch
    {
        0 => "の名望家層",
        1 => "の市民派",
        _ => "の大衆運動",
    };

    public static string DemandJa(PolicyAxis a, bool increase) => a switch
    {
        PolicyAxis.IncomeTax => increase ? "所得税の増税" : "所得税の減税",
        PolicyAxis.SocialSecurity => increase ? "社会保障の拡充" : "社会保障の削減",
        PolicyAxis.Coercion => increase ? "治安維持力の強化" : "治安維持力の縮小",
        PolicyAxis.Tariff => increase ? "保護関税" : "自由貿易",
        PolicyAxis.EducationSpending => increase ? "教育費の拡充" : "教育費の削減",
        PolicyAxis.PollTax => increase ? "人頭税の増税" : "人頭税の廃止",
        PolicyAxis.LandTax => increase ? "土地税の増税" : "土地税の減税",
        PolicyAxis.ProgressiveTax => increase ? "累進課税の強化" : "累進課税の緩和",
        PolicyAxis.EducationLaw => increase ? "公教育の拡大" : "教会教育への回帰",
        PolicyAxis.Suffrage => increase ? "参政権の拡大" : "参政権の制限",
        PolicyAxis.Censorship => increase ? "報道の自由化" : "検閲の強化",
        PolicyAxis.LaborUnion => increase ? "労働組合の容認" : "組合の禁圧",
        PolicyAxis.ShorterHours => increase ? "労働時間規制の撤廃" : "労働時間の短縮",
        PolicyAxis.ChildLabor => increase ? "児童労働の解禁" : "児童労働の禁止",
        PolicyAxis.WomenRights => increase ? "女性の権利の拡大" : "家父長制の維持",
        PolicyAxis.CornLaws => increase ? "穀物法の強化" : "穀物法の撤廃",
        PolicyAxis.LandReform => increase ? "土地改革" : "土地改革の巻き戻し",
        PolicyAxis.SerfEmancipation => increase ? "農奴解放" : "農奴制の維持",
        PolicyAxis.ConscriptionLaw => increase ? "徴兵の拡大" : "徴兵の縮小",
        PolicyAxis.SecretBallot => increase ? "秘密投票" : "公開投票への回帰",
        PolicyAxis.Bureaucracy => increase ? "官僚制度の専門化" : "猟官制への回帰",
        PolicyAxis.StateReligion => increase ? "信教の寛容化" : "国教の強化",
        PolicyAxis.Antitrust => increase ? "独占の規制" : "独占の放任",
        PolicyAxis.Guild => increase ? "同業組合の廃止" : "同業組合の保護",
        PolicyAxis.Inheritance => increase ? "相続の課税の強化" : "相続の課税の緩和",
        PolicyAxis.MinimumWage => increase ? "最低賃金の引き上げ" : "最低賃金の引き下げ",
        PolicyAxis.WartimeRationing => increase ? "戦時配給" : "配給の廃止",
        PolicyAxis.MilitaryExport => increase ? "軍需輸出の禁止" : "軍需輸出の解禁",
        PolicyAxis.CivilianArms => increase ? "民間の武器購入の禁止" : "民間の武器購入の許可",
        PolicyAxis.OpiumPossession => increase ? "阿片の禁令" : "阿片の解禁",
        PolicyAxis.OpiumExport => increase ? "阿片輸出の禁止" : "阿片輸出の解禁",
        PolicyAxis.NavyUpkeep => increase ? "海軍の増強" : "海軍の縮小",
        PolicyAxis.ImmigrationRestriction => increase ? "移民の制限" : "移民の受け入れ",
        PolicyAxis.SpecialistExitBan => increase ? "技術者の出国禁止" : "技術者の出国自由",
        PolicyAxis.MonetaryStandard => increase ? "兌換の維持" : "兌換の停止",
        PolicyAxis.CapitalMobility => increase ? "資本移動の規制" : "資本移動の自由",
        PolicyAxis.Nationalization => increase ? "国有化" : "民営化",
        PolicyAxis.NationalizationAll => increase ? "全産業の国有化" : "民営化",
        PolicyAxis.ColonialSuffrage => increase ? "植民地参政権の拡大" : "植民地参政権の制限",
        PolicyAxis.MigrantNativeSuffrage => increase ? "移民原住民参政権の付与" : "移民原住民参政権の剥奪",
        _ => a.ToString(),
    };
}
