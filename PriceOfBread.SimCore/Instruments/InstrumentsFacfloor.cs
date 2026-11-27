namespace PriceOfBread.SimCore;

// (1422) c152sd 勢力の足切りの計器 facfloor_s（国×四半期 1 行）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。
// 同じ盤面（Factions.Step の終わり）で旧の足切り（0.01×人口）と新の足切り（0.01×参加の総量）を両方数える。POP は計器が自分で回す
// （エンジンの足切りの関数 FloorOf は呼ばない＝規則は事前登録 measurements/m1422_20261127_prereg_sd.txt の文から書いた）。
public static partial class Instruments
{
    internal const string FacfloorHeaderText = "quarter\tyear\tnation\tfloorFlag\tpopK\tpartDK\tpartMaxK\tpartEngK\tcellsAboveN\tcellsAboveP\ttargetK\ttargetAboveNK\ttargetAbovePK"
        + "\tpartAboveNK\tpartAbovePK\tmilSumDK\tfacSeenCount\tfacSeenTargetK\tfactionsCount\triotHeatRegions";

    /// <summary>計器の足切りの割合（事前登録のリテラル＝FactionSnapshot.MinSizeFraction と同じ 0.01）。</summary>
    internal const double FacfloorPhi = 0.01;

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<string>> FacfloorPending = new();
    sealed class FacfloorSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, FacfloorSink> FacfloorSinks = new();

    /// <summary>組 1 つの積算（参加の 2 つの規則・対象の層）。</summary>
    sealed class FacfloorCell { public double PartD, PartMax, Target, MilD; }

    internal readonly record struct FacfloorRow(double PopK, double PartDK, double PartMaxK, double PartEngK, int CellsAboveN, int CellsAboveP,
        double TargetK, double TargetAboveNK, double TargetAbovePK, double PartAboveNK, double PartAbovePK, double MilSumDK,
        int FacSeenCount, double FacSeenTargetK, int FactionsCount, int RiotHeatRegions);

    /// <summary>同じ盤面の両方の足切りを数える（試験からも呼ぶ）。</summary>
    internal static FacfloorRow FacfloorCount(World w)
    {
        var c = w.Coef;
        var (coord, gap) = FactionSnapshot.MembershipContext(w);
        bool dOnly = c.SatReadersToDiscontent > 0;   // エンジンの参加の規則（旗 1＝D・旗 0＝max(D, 闘争度)）
        var cells = new Dictionary<(int Region, int Bloc, int Ideo), FacfloorCell>();
        double popK = 0, partD = 0, partMax = 0, target = 0, milD = 0;
        int riot = 0;
        foreach (var r in w.Regions)
        {
            double rMil = 0, rSize = 0;
            foreach (var p in r.Pops)
            {
                if (p.IsCommoner) { rMil += p.Militancy * p.Size; rSize += p.Size; }
                if (p.Size <= 1e-9) continue;
                double d = FactionSnapshot.DiscontentOf(w, p, coord, gap);
                double m = p.Militancy;
                double mx = Math.Max(d, m);
                double v = Math.Clamp(InterestGroups.FranchiseVoice(p, w.Suffrage, c), 0.0, 1.0);
                double cons = Math.Clamp(p.Consciousness, 0.0, 1.0);
                double n = p.Size;
                popK += n;
                double tgt = n * Math.Max(0.0, d) * (1.0 - v) * (1.0 - cons);
                if (d > 0) { partD += n * d; milD += n * d * m; }
                if (mx > 0) partMax += n * mx;
                target += tgt;
                double eng = dOnly ? d : mx;
                if (!(eng > 0)) continue;
                int bloc = PolicyPreferences.BlocOf(p);
                int ideo = FactionSnapshot.ArgMax(p.IdeologyShares);
                var key = (r.Id, bloc, ideo);
                if (!cells.TryGetValue(key, out var cell)) { cell = new FacfloorCell(); cells[key] = cell; }
                cell.PartD += n * Math.Max(0.0, d); cell.PartMax += n * mx; cell.Target += tgt; cell.MilD += n * Math.Max(0.0, d) * m;
            }
            if (c.RegionalRevoltThreshold > 0 && rSize > 1e-9 && rMil / rSize > c.RegionalRevoltThreshold) riot++;
        }
        double partEng = dOnly ? partD : partMax;
        double totalPop = w.TotalPopulation;
        double floorN = FacfloorPhi * totalPop, floorP = FacfloorPhi * partEng;
        double floorEng = c.FactionFloorByParticipation > 0 ? floorP : floorN;
        int aboveN = 0, aboveP = 0;
        double tN = 0, tP = 0, pN = 0, pP = 0;
        foreach (var cell in cells.Values)
        {
            double size = dOnly ? cell.PartD : cell.PartMax;
            if (size >= floorN) { aboveN++; tN += cell.Target; pN += size; }
            if (size >= floorP) { aboveP++; tP += cell.Target; pP += size; }
        }
        var seenKeys = new HashSet<(int, int, int)>();
        double facT = 0;
        foreach (var f in w.Factions)
        {
            if (!w.Regions.Contains(f.Region)) continue;
            var key = (f.Region.Id, f.Bloc, f.Ideo);
            if (!cells.TryGetValue(key, out var cell)) continue;
            double size = dOnly ? cell.PartD : cell.PartMax;
            if (!(size >= floorEng) || !seenKeys.Add(key)) continue;
            facT += cell.Target;
        }
        return new FacfloorRow(popK, partD, partMax, partEng, aboveN, aboveP, target, tN, tP, pN, pP, milD, seenKeys.Count, facT, w.Factions.Count, riot);
    }

    /// <summary><see cref="Factions.Step"/> の終わり（計器だけ・呼び手は Instruments.On の内側）：この国のこの四半期の行を溜める。</summary>
    internal static void FacfloorObserve(World w)
    {
        if (!Tg.Facfloor) return;
        var x = FacfloorCount(w);
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int q = Calendar.QuarterIndexOfTick(w.Tick);
        string I(int v) => v.ToString(inv);
        FacfloorPending.GetOrCreateValue(w).Add(string.Join('\t', I(q), I(w.Year), NameOf(w), F(w.Coef.FactionFloorByParticipation), F(x.PopK), F(x.PartDK), F(x.PartMaxK), F(x.PartEngK),
            I(x.CellsAboveN), I(x.CellsAboveP), F(x.TargetK), F(x.TargetAboveNK), F(x.TargetAbovePK), F(x.PartAboveNK), F(x.PartAbovePK), F(x.MilSumDK),
            I(x.FacSeenCount), F(x.FacSeenTargetK), I(x.FactionsCount), I(x.RiotHeatRegions)));
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を書き出す。</summary>
    static void FacfloorTable(MultiNationSim sim, ulong seed)
    {
        if (!Tg.Facfloor) return;
        foreach (var n in sim.Nations)
        {
            if (!FacfloorPending.TryGetValue(n.World, out var rows) || rows.Count == 0) continue;
            var sk = FacfloorSinks.GetValue(sim, _ => new FacfloorSink());
            sk.W ??= Open("facfloor", seed);
            if (!sk.Header) { sk.W.WriteLine(FacfloorHeaderText); sk.Header = true; }
            foreach (var r in rows) sk.W.WriteLine(r);
            rows.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void FacfloorClose(MultiNationSim sim)
    {
        if (!Tg.Facfloor) return;
        if (FacfloorSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
