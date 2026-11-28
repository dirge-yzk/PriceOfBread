namespace PriceOfBread.SimCore;

// (1435) c154sd 勢力の足切りと強さの計器 facfloor2_s（国×四半期 1 行）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。
// 同じ盤面（Factions.Step の終わり）で 3 つの足切り（φN・φN_r・φP_n）と 3 つの強さの頭数（Σ N π M・Σ N M²・Σ N M）を数える。
// POP は計器が自分で回す（エンジンの足切りの関数 FactionSnapshot.Floor・組の関数 ComputeClusters・強さの関数 FactionStrength は呼ばない＝
// 規則は事前登録 measurements/m1435_20261128_prereg_sd.txt の文から書いた）。不満度 D は既存の読み手 FactionSnapshot.DiscontentOf を読む（Politics.Discontent の呼び手を増やさない）。
public static partial class Instruments
{
    internal const string Facfloor2HeaderText = "quarter\tyear\tnation\tfloorFlag\tstrengthFlag\tpopK\tminRegionPopP\tpartK\tcellsAboveN\tcellsAboveR\tcellsAboveP"
        + "\ttargetK\ttargetAboveNK\ttargetAboveRK\ttargetAbovePK\tmilSumDK\tmilSumM2K\tfightHeadsK\tfacSeenCount\tfacSeenTargetK"
        + "\tfacEngCount\tfacMinSizeP\tfacBelow1P\tfacBelowFloorR\tfactionsCount\triotFire\tpartyQualFac\tpartyQualSizeMinP";

    /// <summary>計器の足切りの割合（事前登録のリテラル＝FactionSnapshot.MinSizeFraction と同じ 0.01）。</summary>
    internal const double Facfloor2Phi = 0.01;

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<string>> Facfloor2Pending = new();
    sealed class Facfloor2Sink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, Facfloor2Sink> Facfloor2Sinks = new();

    /// <summary>組 1 つの積算（エンジンの参加の規則の頭数・対象の層・強さの頭数 3 つ）。</summary>
    sealed class Facfloor2Cell { public double Part, Target, MilPi, Fight, FightSq, RegionPop; }

    internal readonly record struct Facfloor2Row(double PopK, double MinRegionPopP, double PartK, int CellsAboveN, int CellsAboveR, int CellsAboveP,
        double TargetK, double TargetAboveNK, double TargetAboveRK, double TargetAbovePK, double MilSumDK, double MilSumM2K, double FightHeadsK,
        int FacSeenCount, double FacSeenTargetK, int FacEngCount, double FacMinSizeP, int FacBelow1P, int FacBelowFloorR, int FactionsCount,
        int RiotFire, int PartyQualFac, double PartyQualSizeMinP);

    /// <summary>同じ盤面の 3 つの足切りと 3 つの強さの頭数を数える（試験からも呼ぶ）。</summary>
    internal static Facfloor2Row Facfloor2Count(World w)
    {
        var c = w.Coef;
        var (coord, gap) = FactionSnapshot.MembershipContext(w);   // 不満度 D の文脈（組の回りと同じ値）
        bool dOnly = c.SatReadersToDiscontent > 0;   // エンジンの参加の規則（旗 1＝D・旗 0＝max(D, 闘争度)）
        var cells = new Dictionary<(int Region, int Bloc, int Ideo), Facfloor2Cell>();
        var regionPop = new Dictionary<int, double>();
        double popK = 0, part = 0, target = 0, minRegion = double.PositiveInfinity;
        Region? worst = null; double worstKey = 0, worstHeat = 0;
        foreach (var r in w.Regions)
        {
            double rPop = 0, cMil = 0, cSize = 0;
            foreach (var p in r.Pops)
            {
                rPop += p.Size;
                if (p.IsCommoner) { cMil += p.Militancy * p.Size; cSize += p.Size; }
            }
            regionPop[r.Id] = rPop;
            if (rPop < minRegion) minRegion = rPop;
            if (!(w.PendingRebelRegions is { } pend && pend.Contains(r)))
            {
                double heat = cSize > 1e-9 ? cMil / cSize : 0.0;
                double key = heat * cSize;
                if (key > worstKey) { worstKey = key; worstHeat = heat; worst = r; }
            }
            foreach (var p in r.Pops)
            {
                if (p.Size <= 1e-9) continue;
                double n = p.Size;
                popK += n;
                double d = FactionSnapshot.DiscontentOf(w, p, coord, gap);
                double m = p.Militancy;
                double eng = dOnly ? d : Math.Max(d, m);
                double v = Math.Clamp(InterestGroups.FranchiseVoice(p, w.Suffrage, c), 0.0, 1.0);
                double cons = Math.Clamp(p.Consciousness, 0.0, 1.0);
                double tgt = n * Math.Max(0.0, d) * (1.0 - v) * (1.0 - cons);
                target += tgt;
                if (!(eng > 0)) continue;
                int bloc = PolicyPreferences.BlocOf(p);
                int ideo = FactionSnapshot.ArgMax(p.IdeologyShares);
                var ck = (r.Id, bloc, ideo);
                if (!cells.TryGetValue(ck, out var cell)) { cell = new Facfloor2Cell(); cells[ck] = cell; }
                cell.Part += n * eng; cell.Target += tgt; cell.MilPi += n * eng * m; cell.Fight += n * m; cell.FightSq += n * m * m;
                part += n * eng;
            }
        }
        foreach (var (k, cell) in cells) cell.RegionPop = regionPop[k.Region];
        double totalPop = w.TotalPopulation;
        double floorN = Facfloor2Phi * totalPop, floorP = Facfloor2Phi * part;
        bool byRegion = c.FactionFloorByRegion > 0;
        int aboveN = 0, aboveR = 0, aboveP = 0;
        double tN = 0, tR = 0, tP = 0;
        foreach (var cell in cells.Values)
        {
            if (cell.Part >= floorN) { aboveN++; tN += cell.Target; }
            if (cell.Part >= Facfloor2Phi * cell.RegionPop) { aboveR++; tR += cell.Target; }
            if (cell.Part >= floorP) { aboveP++; tP += cell.Target; }
        }
        var seenKeys = new HashSet<(int, int, int)>();
        double facT = 0, milD = 0, milM2 = 0, fightH = 0, minEng = double.PositiveInfinity;
        int engCount = 0, below1 = 0, belowR = 0, qual = 0;
        double qualMin = double.PositiveInfinity;
        foreach (var f in w.Factions)
        {
            if (!w.Regions.Contains(f.Region)) continue;
            double rp = regionPop[f.Region.Id];
            if (rp >= 0.1 && f.Size < 0.001) below1++;   // N_r≥100 人の州で 1 人未満（千人の単位）
            double voice = f.Bloc == 0 ? 1.0 : InterestGroups.SuffrageGate(w.Suffrage, f.Bloc == 1, c);
            if (f.Cohesion >= c.PartyFormCohesion && voice > 0) { qual++; if (f.Size < qualMin) qualMin = f.Size; }
            var key = (f.Region.Id, f.Bloc, f.Ideo);
            if (!cells.TryGetValue(key, out var cell)) continue;
            if (f.Size == cell.Part)   // この四半期にエンジンが大きさを書いた勢力（同じ和の順＝ビットで一致）
            {
                engCount++;
                if (f.Size < minEng) minEng = f.Size;
                if (f.Size < Facfloor2Phi * rp * (1.0 - 1e-12)) belowR++;
            }
            double floorEng = byRegion ? Facfloor2Phi * cell.RegionPop : floorN;
            if (!(cell.Part >= floorEng) || !seenKeys.Add(key)) continue;
            facT += cell.Target; milD += cell.MilPi; milM2 += cell.FightSq; fightH += cell.Fight;
        }
        return new Facfloor2Row(popK, double.IsPositiveInfinity(minRegion) ? 0.0 : minRegion * 1000.0, part, aboveN, aboveR, aboveP,
            target, tN, tR, tP, milD, milM2, fightH, seenKeys.Count, facT,
            engCount, double.IsPositiveInfinity(minEng) ? -1.0 : minEng * 1000.0, below1, belowR, w.Factions.Count,
            c.RegionalRevoltThreshold > 0 && worst is not null && worstHeat > c.RegionalRevoltThreshold ? 1 : 0,
            qual, double.IsPositiveInfinity(qualMin) ? -1.0 : qualMin * 1000.0);
    }

    /// <summary><see cref="Factions.Step"/> の終わり（計器だけ・呼び手は Instruments.On の内側）：この国のこの四半期の行を溜める。</summary>
    internal static void Facfloor2Observe(World w)
    {
        if (!Tg.Facfloor2) return;
        var x = Facfloor2Count(w);
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int q = Calendar.QuarterIndexOfTick(w.Tick);
        string I(int v) => v.ToString(inv);
        Facfloor2Pending.GetOrCreateValue(w).Add(string.Join('\t', I(q), I(w.Year), NameOf(w), F(w.Coef.FactionFloorByRegion), F(w.Coef.FactionStrengthByMilitancy),
            F(x.PopK), F(x.MinRegionPopP), F(x.PartK), I(x.CellsAboveN), I(x.CellsAboveR), I(x.CellsAboveP),
            F(x.TargetK), F(x.TargetAboveNK), F(x.TargetAboveRK), F(x.TargetAbovePK), F(x.MilSumDK), F(x.MilSumM2K), F(x.FightHeadsK),
            I(x.FacSeenCount), F(x.FacSeenTargetK), I(x.FacEngCount), F(x.FacMinSizeP), I(x.FacBelow1P), I(x.FacBelowFloorR), I(x.FactionsCount),
            I(x.RiotFire), I(x.PartyQualFac), F(x.PartyQualSizeMinP)));
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を書き出す。</summary>
    static void Facfloor2Table(MultiNationSim sim, ulong seed)
    {
        if (!Tg.Facfloor2) return;
        foreach (var n in sim.Nations)
        {
            if (!Facfloor2Pending.TryGetValue(n.World, out var rows) || rows.Count == 0) continue;
            var sk = Facfloor2Sinks.GetValue(sim, _ => new Facfloor2Sink());
            sk.W ??= Open("facfloor2", seed);
            if (!sk.Header) { sk.W.WriteLine(Facfloor2HeaderText); sk.Header = true; }
            foreach (var r in rows) sk.W.WriteLine(r);
            rows.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void Facfloor2Close(MultiNationSim sim)
    {
        if (!Tg.Facfloor2) return;
        if (Facfloor2Sinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
