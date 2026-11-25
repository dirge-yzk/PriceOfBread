namespace PriceOfBread.SimCore;

public static class Ideologies
{
    public const int Count = 11;
    private const double WealthExcessCap = 3.0;

    public static void UpdateShares(World w)
    {
        double totSav = 0, totPop = 0;
        foreach (var p in w.AllPops()) { totSav += System.Math.Max(0, p.Savings.Value); totPop += p.Size; }
        double meanWealth = totPop > 1e-9 ? totSav / totPop : 1;

        var era = EraGates(w);
        double eraSoc = era.Socialism, eraComm = era.Communism, eraImp = era.Imperialism, eraFasc = era.Fascism;

        double nationalismSurge = w.Coef.DefeatNationalism * w.DefeatShock + w.Coef.LostLandNationalism * w.LostRegionsCumulative
            + w.Coef.VictoryNationalism * w.VictoryShock + w.Coef.ExternalPressureNationalism * w.ExternalPressure;
        double victoryImp = w.Coef.VictoryImperialism * w.VictoryShock;
        double famRep = w.Coef.FamineRepublicanism * w.FamineMemory, famSoc = w.Coef.FamineSocialism * w.FamineMemory;

        double royalPrivilege = PowerShares.PrivilegeIntact(w);
        double royalPatronage = PowerShares.StatePatronage(w);
        double privLoss = w.Coef.ReactionFromLoss > 0 ? 1.0 - PowerShares.PrivilegeIntact(w) : 0;
        if (!(privLoss >= 0 && privLoss <= 1)) privLoss = 0;
        var (dCoord, dLawGap) = FactionSnapshot.MembershipContext(w, true);
        int dSeen = 0;
        var prds = Instruments.On ? Instruments.PrdsBegin(w, eraSoc, eraComm, dCoord, dLawGap) : null;
        var split = w.Coef.NationalismFromDispersion > 0 && CultureSplit.TryGetValue(w, out var sp) ? sp : null;   // (1230) 表が無い（四半期の末の締めの外）＝f＝0＝今の式
        Region? splitRegion = null; double fCult = 0;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            var r = p.IdeologyShares;
            dSeen++;
            double disc = FactionSnapshot.DiscontentOf(w, p, dCoord, dLawGap);
            double rich = BaseWeights(p, r, meanWealth, w.Coef);
            double cons = p.Consciousness, mil = w.Coef.SatReadersToDiscontent > 0 ? disc : p.Militancy;   // (1371) 旗 1＝思想は D だけ（闘争度を読まない）
            if (disc > mil) mil = disc;
            double conserv = PoliticalQuantities.Clamp01(rich / 2.0);
            double prog = 1.0 - conserv;
            if (w.Coef.ReactionFromLoss > 0)
            {
                double gate = (p.Closed || p.IsSoldier) ? 1.0 : PoliticalQuantities.Clamp01(1.0 - cons);
                r[1] += 2.0 * mil * w.Coef.ReactionFromLoss * privLoss * gate;
            }
            else
                r[1] += 2.0 * mil * conserv;
            if (split is null) r[2] += 2.5 * cons * prog;
            else
            {
                if (!ReferenceEquals(rg, splitRegion)) { splitRegion = rg; fCult = split.TryGetValue(rg.Culture, out double fv) && fv >= 0 && fv <= 1 ? fv : 0; }
                double lib = 2.5 * cons * prog;
                r[2] += lib * (1.0 - fCult);
                r[4] += lib * fCult;
            }
            r[3] += (1.5 * cons + 1.0 * mil) * prog;
            r[5] += (1.0 * cons + 1.5 * mil) * prog;
            r[6] += 2.0 * mil * prog;
            r[7] += 1.0 * mil * prog;
            r[4] += nationalismSurge;
            if (w.ManifestoIdeology >= 0 && w.ManifestoIdeology < r.Length && w.ManifestoCredit > 0)
                r[w.ManifestoIdeology] += w.ManifestoCredit;
            r[8] += victoryImp;
            r[3] += famRep;
            r[5] += famSoc;
            r[5] *= eraSoc; r[6] *= eraComm; r[8] *= eraImp;
            if (w.Coef.FascismGate > 0) r[9] += w.Coef.FascismGate * eraFasc * mil * (r[1] + r[4]);
            {
                double royalSupply = p.Closed ? royalPrivilege : (p.IsModernElite ? royalPatronage : 0);
                if (royalSupply > 0) r[10] += cons * royalSupply;
            }
            var shadow = prds is null ? default : Instruments.PrdsShadow(prds, r, mil);
            Normalize(r);
            if (prds is not null) Instruments.PrdsAdd(prds, p, r, mil, shadow.T, shadow.ShadowAxis, shadow.SumRaw);
        }
        if (prds is not null) Instruments.PrdsEnd(prds);
        w.Cost[CostLedger.ParticipationPops] += dSeen;
    }

    // (1230) 文化の分断の度合い f の表（世界で 1 枚・文化の札→f）。MultiNationSim.StepDay が四半期の末の締めの直前に作り、締めの直後に消す＝セーブの点をまたがない（状態ではない・盤面から導く）。
    //   国ごとに同じ辞書を引く（国の無い盤面の試験でも World から引ける）。
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, Dictionary<string, double>> CultureSplit = new();

    /// <summary>f(X)＝1−M_X÷W_X。W_X＝世界の文化 X の人口（州の札・POP の大きさの和）。M_X＝X の人口が最大の 1 国の X の人口（主の文化を問わない＝(1249) 形 A′・1 国だけが持つ文化は f＝0）。
    /// 札が空の州と人口 0 の州は数えない（表に無い札＝f＝0）。どの国の旗も 0 なら何もしない（false）。</summary>
    internal static bool BuildCultureSplit(IReadOnlyList<World> worlds)
    {
        bool any = false;
        foreach (var w in worlds) if (w.Coef.NationalismFromDispersion > 0) { any = true; break; }
        if (!any) return false;
        var total = new Dictionary<string, double>();
        var largest = new Dictionary<string, double>();
        var own = new Dictionary<string, double>();
        foreach (var w in worlds)
        {
            own.Clear();
            foreach (var r in w.Regions)
            {
                if (r.Culture.Length == 0) continue;
                double pop = 0; foreach (var p in r.Pops) pop += p.Size;
                if (!(pop > 0)) continue;
                own[r.Culture] = own.GetValueOrDefault(r.Culture) + pop;
                total[r.Culture] = total.GetValueOrDefault(r.Culture) + pop;
            }
            foreach (var kv in own) if (kv.Value > largest.GetValueOrDefault(kv.Key)) largest[kv.Key] = kv.Value;   // (1249) 形 A′：主の文化を問わない
        }
        var f = new Dictionary<string, double>(total.Count);
        foreach (var kv in total) f[kv.Key] = 1.0 - largest.GetValueOrDefault(kv.Key) / kv.Value;
        foreach (var w in worlds) CultureSplit.AddOrUpdate(w, f);
        return true;
    }

    internal static void ClearCultureSplit(IReadOnlyList<World> worlds) { foreach (var w in worlds) CultureSplit.Remove(w); }

    /// <summary>試験の盤面用：表を手で置く（null で消す）。</summary>
    internal static void SetCultureSplit(World w, Dictionary<string, double>? table) { if (table is null) CultureSplit.Remove(w); else CultureSplit.AddOrUpdate(w, table); }

    /// <summary>表の f（表が無い・札が表に無い＝NaN）。</summary>
    internal static double CultureSplitOf(World w, string culture) => CultureSplit.TryGetValue(w, out var t) && t.TryGetValue(culture, out double f) ? f : double.NaN;

    public readonly record struct EraGateSet(double Socialism, double Communism, double Imperialism, double Fascism);

    public static EraGateSet EraGates(World w)
    {
        var k = w.Discovered;
        return new EraGateSet(Gate(k, Ideology.Socialism), Gate(k, Ideology.Communism), Gate(k, Ideology.Imperialism), Gate(k, Ideology.Fascism));
        static double Gate(bool[] k, Ideology i) => Discovery.Known(k, Discovery.IdeaRow(i)) ? 1.0 : 0.0;
    }

    public static double FascismCrisis(World w)
    {
        double defeat = PoliticalQuantities.Clamp01(w.DefeatShock);
        double slump = PoliticalQuantities.Clamp01(w.GdpDrawdown.Value / System.Math.Max(1e-9, w.Coef.FascismCrisisSlumpRef));
        var natIdeo = National(w);
        double left = PoliticalQuantities.Clamp01((natIdeo[(int)Ideology.Socialism] + natIdeo[(int)Ideology.Communism]) / System.Math.Max(1e-9, w.Coef.FascismCrisisLeftRef.Value));
        return PoliticalQuantities.Clamp01(System.Math.Max(defeat, slump) * left);
    }

    private static double BaseWeights(Pop p, double[] r, double meanWealth, Coefficients c)
    {
        System.Array.Clear(r, 0, r.Length);
        double rent = System.Math.Max(0, p.LandRentPerMonth.Value), earned = System.Math.Max(0, p.EarnedPerMonth.Value);
        double rentier = (rent + earned) > 1e-9 ? rent / (rent + earned) : 0;
        double wpc = p.Size > 1e-9 ? System.Math.Max(0, p.Savings.Value) / p.Size : 0;
        double wealthRel = meanWealth > 1e-9 ? wpc / meanWealth : 1;
        double rich = PoliticalQuantities.Clamp01((wealthRel - 1) / WealthExcessCap) * WealthExcessCap;
        double poor = PoliticalQuantities.Clamp01(1 - wealthRel);

        if (p.Closed)
        {
            r[0] = 2 * rentier + 0.5 + c.NoblePrivilegeWeight;
            r[1] = c.NoblePrivilegeWeight * 0.5 + rentier;
            r[2] = 1.0 + 3 * rich * (1 - rentier);
            r[3] = 0.5;
            r[4] = 0.5 + poor;
            r[5] = 0.5 * poor * (1 - rentier);
            r[8] = rich * (1 - rentier) + 0.5;
        }
        else if (p.IsSoldier)
        {
            r[4] = 4; r[8] = 2; r[0] = 1; r[1] = 1;
        }
        else
        {
            r[0] = 2 * rentier + 0.5;
            r[2] = (p.IsModernElite ? 3.0 : 1.0) + 3 * rich * (1 - rentier);
            r[3] = p.IsModernElite ? 2.0 : 1.0;
            r[5] = 2 * poor * (1 - rentier) + (p.IsUrban ? 1.0 : 0);
            r[6] = 1.5 * poor * (p.IsUrban ? 1.0 : 0);
            r[7] = poor;
            r[8] = (p.IsModernElite ? 1.0 : 0) + rich * (1 - rentier);
            r[4] = 0.5;
        }
        return rich;
    }

    internal static void Normalize(double[] r)
    {
        double sum = 0;
        for (int i = 0; i < r.Length; i++) { if (!(r[i] >= 0)) r[i] = 0; sum += r[i]; }
        if (sum < 1e-9) { r[0] = 1; return; }
        for (int i = 0; i < r.Length; i++) r[i] /= sum;
    }



    public static readonly Polity[] Polities =
        [Polity.Absolutist, Polity.Despotic, Polity.Shogunate, Polity.ConstitutionalMonarchy,
         Polity.Republic, Polity.BourgeoisDictatorship, Polity.ProletarianDictatorship,
         Polity.MilitaryDictatorship];

    public const double AffMax = 2.0, AffMin = -1.0;

    private static readonly double[][] IdeologyCoord =
    [
        [ 0.00, 0.40, 1.00, 0.25 ],
        [ 0.00, 0.75, 1.00, 0.40 ],
        [ 0.60, 0.10, 0.50, 0.20 ],
        [ 1.00, 0.10, 0.45, 0.20 ],
        [ 0.45, 0.50, 0.50, 1.00 ],
        [ 0.75, 0.30, 0.00, 0.20 ],
        [ 0.30, 0.85, 0.00, 0.20 ],
        [ 0.70, 0.00, 0.20, 0.00 ],
        [ 0.30, 0.60, 0.55, 1.00 ],
        [ 0.35, 0.90, 0.55, 1.00 ],
        [ 0.55, 0.55, 0.90, 0.50 ],
    ];

    private static readonly double[][] PolityCoord =
    [
        [ 0.00, 0.50, 1.00, 0.30 ],
        [ 0.05, 0.80, 0.50, 0.60 ],
        [ 0.00, 0.55, 1.00, 0.15 ],
        [ 0.60, 0.20, 0.55, 0.30 ],
        [ 0.95, 0.10, 0.40, 0.25 ],
        [ 0.30, 0.60, 0.50, 0.45 ],
        [ 0.30, 0.85, 0.00, 0.20 ],
        [ 0.15, 0.75, 0.60, 0.85 ],
        [ 0.70, 0.00, 0.20, 0.00 ],
    ];

    public static double AffinityOf(double[] polity, double[] ideology, double slope)
    {
        double d2 = 0;
        for (int i = 0; i < 4; i++) { double x = polity[i] - ideology[i]; d2 += x * x; }
        return System.Math.Clamp(AffMax - slope * System.Math.Sqrt(d2), AffMin, AffMax);
    }

    public static double[] National(World w) => Aggregate(w, w.AllPops());

    public static double[] Regional(World w, Region r) => Aggregate(w, r.Pops);

    public static double[] Soldiers(World w, out double totalWeight) => Aggregate(w, SoldierPops(w), out totalWeight);

    private static IEnumerable<Pop> SoldierPops(World w)
    {
        foreach (var p in w.AllPops()) if (p.IsSoldier) yield return p;
    }

    private static double[] Aggregate(World w, IEnumerable<Pop> pops) => Aggregate(w, pops, out _);

    private static double[] Aggregate(World w, IEnumerable<Pop> pops, out double totalWeight)
    {
        var agg = new double[Count];
        double total = 0;
        foreach (var p in pops)
        {
            double voice = w.Coef.IdeologyVoiceFloor + (1.0 - w.Coef.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            double weight = p.Size * p.Consciousness * voice;
            if (!(weight > 0)) continue;
            var s = p.IdeologyShares;
            for (int k = 0; k < Count; k++) agg[k] += s[k] * weight;
            total += weight;
        }
        if (total > 1e-9) for (int k = 0; k < Count; k++) agg[k] /= total;
        totalWeight = total;
        return agg;
    }

    public static double[] BasePolityCoord(Polity pol) => PolityCoord[(int)pol];

    public static double Openness(Suffrage s) => s switch
    {
        Suffrage.Universal => 0.95, Suffrage.AdultMale => 0.80, Suffrage.Census => 0.55, _ => 0.00,
    };

    public static double[] IdealPoint(double[] shares)
    {
        var pt = new double[4];
        double tot = 0;
        for (int k = 0; k < Count; k++)
        {
            double s = shares[k];
            if (s <= 0) continue;
            var co = IdeologyCoord[k];
            for (int i = 0; i < 4; i++) pt[i] += s * co[i];
            tot += s;
        }
        if (tot > 1e-9) for (int i = 0; i < 4; i++) pt[i] /= tot;
        return pt;
    }

    public static double SupportAt(double[] agg, double[] polityCoord, double affinitySlope)
    {
        double s = 0;
        for (int k = 0; k < Count; k++) s += agg[k] * AffinityOf(polityCoord, IdeologyCoord[k], affinitySlope);
        return s;
    }

    public static double DefectionShare(double[] agg, double[] curCoord, double[] attCoord, double affinitySlope)
    {
        double sum = 0;
        for (int k = 0; k < Count; k++)
        {
            double gain = AffinityOf(attCoord, IdeologyCoord[k], affinitySlope) - AffinityOf(curCoord, IdeologyCoord[k], affinitySlope);
            if (gain > 0) sum += agg[k] * gain;
        }
        double d = sum / (AffMax - AffMin);
        return PoliticalQuantities.Pos(d);
    }

    public static double Support(double[] agg, Polity pol, double affinitySlope) => SupportAt(agg, PolityCoord[(int)pol], affinitySlope);
}
