namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（蓄えは ConditionalWeakTable の側）。〔履歴 InstrumentsAiBuild.cs#1〕
public static partial class Instruments
{
    internal const string AibHeaderText = "quarter\tyear\tnation\tcalls\treturnFundingCount\treturnRoomCount\troomEntryPerMonthMean\tbufferEntryMean\ttreasuryEntryStock\t"
        + "overCostCapCandidateCount\tstartedCount\tstartedOverCapCount\trefusedCount\tskipRoomCount\tstartedOutlayPerMonth\tstartedByCategory\tstartedGoods\toverCapGoods\tskippedGoods\t"
        + "warshipScaleSum\tsailingShipScaleSum\tartilleryScaleSum\trifleScaleSum\tsteelScaleSum\twarshipBuiltSum\tsailingShipBuiltSum\tartilleryBuiltSum\trifleBuiltSum\twarshipBuildingCount\tsailingShipBuildingCount\tartilleryBuildingCount\trifleBuildingCount	bestRegionCount	bestRootDeadCount	rootDroppedCount	bestRootDeadGoods	rootDroppedGoods	rootDroppedBlockers	builtUnits	builtIdleUnits	builtIdleGoods";

    sealed class AibAcc
    {
        public int Calls, ReturnFunding, ReturnRoom, OverCap, Started, StartedOverCap, Refused, SkipRoom, BestN, BestDead, RootDropped;   // RootDropped〔履歴 InstrumentsAiBuild.cs#2〕
        public double RoomSum, BufferSum, TreasuryEntry, StartedOutlay;
        public readonly SortedDictionary<string, int> ByCategory = new(StringComparer.Ordinal), StartedGoods = new(StringComparer.Ordinal), OverCapGoods = new(StringComparer.Ordinal), SkippedGoods = new(StringComparer.Ordinal),
            BestDeadGoods = new(StringComparer.Ordinal), DroppedGoods = new(StringComparer.Ordinal), DroppedBlockers = new(StringComparer.Ordinal);   // BestDeadGoods〔履歴 InstrumentsAiBuild.cs#3〕
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, AibAcc> AibAccs = new();

    /// <summary>TryBuildFacility が（国家の支払い率 r の門を通って）入った：その回の余地（上限−数えた出費）とバッファ・国庫。</summary>
    internal static void AiBuildEnter(World w, double room, double buffer)
    {
        var a = AibAccs.GetOrCreateValue(w);
        a.Calls++; a.RoomSum += room; a.BufferSum += buffer; a.TreasuryEntry = w.Treasury.Value;
    }
    /// <summary>門で戻った回（funding＝支払い率 r＜1・そうでなければ月の出費の余地が無い）。funding の回は Enter の前に戻る＝ここで calls に足す。</summary>
    internal static void AiBuildReturn(World w, bool funding)
    {
        var a = AibAccs.GetOrCreateValue(w);
        if (funding) { a.Calls++; a.ReturnFunding++; } else a.ReturnRoom++;
    }
    internal static void AiBuildOverCap(World w, int goodId)
    {
        var a = AibAccs.GetOrCreateValue(w);
        a.OverCap++; string n = Catalog.Goods[goodId].Name; a.OverCapGoods.TryGetValue(n, out var c); a.OverCapGoods[n] = c + 1;
    }
    internal static void AiBuildSkipRoom(World w, int goodId)
    {
        var a = AibAccs.GetOrCreateValue(w);
        a.SkipRoom++; string n = Catalog.Goods[goodId].Name; a.SkippedGoods.TryGetValue(n, out var c); a.SkippedGoods[n] = c + 1;
    }
    /// <summary>
    /// 〔履歴 InstrumentsAiBuild.cs#4〕
    /// </summary>
    internal static void AiBuildBest(World w, int goodId, bool rootDead)
    {
        var a = AibAccs.GetOrCreateValue(w);
        a.BestN++;
        if (!rootDead) return;
        a.BestDead++; string n = Catalog.Goods[goodId].Name; a.BestDeadGoods.TryGetValue(n, out var c); a.BestDeadGoods[n] = c + 1;
    }
    /// <summary>
    /// 〔履歴 InstrumentsAiBuild.cs#5〕
    /// </summary>
    internal static void AiBuildRootDropped(World w, int goodId, int blockerGood)
    {
        var a = AibAccs.GetOrCreateValue(w);
        a.RootDropped++; string n = Catalog.Goods[goodId].Name; a.DroppedGoods.TryGetValue(n, out var c); a.DroppedGoods[n] = c + 1;
        string b = blockerGood >= 0 && blockerGood < Catalog.Goods.Length ? Catalog.Goods[blockerGood].Name : "-"; a.DroppedBlockers.TryGetValue(b, out var c2); a.DroppedBlockers[b] = c2 + 1;   // 行き止まりの財（根が無い原因）
    }
    /// <summary>州の最良を SetStateTarget で押した結果：<paramref name="started"/>＝着工した（ConstructionLeft ＞ 0）・そうでなければ拒まれて意志が残った。</summary>
    internal static void AiBuildTried(World w, int goodId, double est, bool started, bool overCostCap)
    {
        var a = AibAccs.GetOrCreateValue(w);
        if (!started) { a.Refused++; return; }
        a.Started++; if (overCostCap) a.StartedOverCap++; a.StartedOutlay += est > 0 ? est : 0;
        string n = Catalog.Goods[goodId].Name; a.StartedGoods.TryGetValue(n, out var c); a.StartedGoods[n] = c + 1;
        string cat = Catalog.FactorySpec(goodId).Category; a.ByCategory.TryGetValue(cat, out var k); a.ByCategory[cat] = k + 1;
    }

    /// <summary>助走の分は数えない（Phase の warm が呼ぶ）。助走は 20 四半期の AI の判断が最初の行に積まれ、G0 の分母が偏る。</summary>
    static void AibClear(MultiNationSim sim)
    {
        foreach (var n in sim.Nations)
        {
            var a = AibAccs.GetOrCreateValue(n.World);
            a.Calls = a.ReturnFunding = a.ReturnRoom = a.OverCap = a.Started = a.StartedOverCap = a.Refused = a.SkipRoom = a.BestN = a.BestDead = a.RootDropped = 0;
            a.RoomSum = a.BufferSum = a.TreasuryEntry = a.StartedOutlay = 0;
            a.ByCategory.Clear(); a.StartedGoods.Clear(); a.OverCapGoods.Clear(); a.SkippedGoods.Clear(); a.BestDeadGoods.Clear(); a.DroppedGoods.Clear(); a.DroppedBlockers.Clear();
        }
    }

    static void AibFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var a = AibAccs.GetOrCreateValue(w);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string Join<T>(SortedDictionary<string, T> d) => string.Join(';', d.Select(kv => kv.Key + ":" + kv.Value));
        int enters = a.Calls - a.ReturnFunding;   // 余地を測った回（支払い率 r の門を通った回）
        double wsS = 0, shS = 0, arS = 0, rfS = 0, stS = 0, wsB = 0, shB = 0, arB = 0, rfB = 0; int wsN = 0, shN = 0, arN = 0, rfN = 0;   // 鎖の中間の断面（Scale と BuiltScale は読むだけ）
        int builtU = 0, builtIdleU = 0; var builtIdleGoods = new SortedDictionary<string, int>(StringComparer.Ordinal);   // builtU〔履歴 InstrumentsAiBuild.cs#6〕
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                int g = p.GoodId;
                if (p.ConstructionLeft <= 0 && !p.Bankrupt && g >= 0 && g < Catalog.Goods.Length && w.StateUnitsOf(p) is var su and > 0)
                {
                    builtU += su;
                    if (!(p.Output > 1e-9)) { builtIdleU += su; string gn = Catalog.Goods[g].Name; builtIdleGoods.TryGetValue(gn, out var bc); builtIdleGoods[gn] = bc + su; }
                }
                if (g == Catalog.WarshipId) { wsS += p.Scale; wsB += p.BuiltScale; if (p.ConstructionLeft > 0) wsN++; }
                else if (g == Catalog.SailingShipId) { shS += p.Scale; shB += p.BuiltScale; if (p.ConstructionLeft > 0) shN++; }
                else if (g == Catalog.ArtilleryId) { arS += p.Scale; arB += p.BuiltScale; if (p.ConstructionLeft > 0) arN++; }
                else if (g == Catalog.RifleId) { rfS += p.Scale; rfB += p.BuiltScale; if (p.ConstructionLeft > 0) rfN++; }
                else if (g == Catalog.SteelId) stS += p.Scale;
            }
        sk.Aib ??= Open("aib", seed);
        if (!sk.AibHeader) { sk.Aib.WriteLine(AibHeaderText); sk.AibHeader = true; }
        sk.Aib.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I(a.Calls), I(a.ReturnFunding), I(a.ReturnRoom),
            F(enters > 0 ? a.RoomSum / enters : double.NaN), F(enters > 0 ? a.BufferSum / enters : double.NaN), F(a.TreasuryEntry),
            I(a.OverCap), I(a.Started), I(a.StartedOverCap), I(a.Refused), I(a.SkipRoom), F(a.StartedOutlay), Join(a.ByCategory), Join(a.StartedGoods), Join(a.OverCapGoods), Join(a.SkippedGoods),
            F(wsS), F(shS), F(arS), F(rfS), F(stS), F(wsB), F(shB), F(arB), F(rfB), I(wsN), I(shN), I(arN), I(rfN),
            I(a.BestN), I(a.BestDead), I(a.RootDropped), Join(a.BestDeadGoods), Join(a.DroppedGoods), Join(a.DroppedBlockers),
            I(builtU), I(builtIdleU), Join(builtIdleGoods)));
        a.Calls = a.ReturnFunding = a.ReturnRoom = a.OverCap = a.Started = a.StartedOverCap = a.Refused = a.SkipRoom = a.BestN = a.BestDead = a.RootDropped = 0;
        a.RoomSum = a.BufferSum = a.TreasuryEntry = a.StartedOutlay = 0;
        a.ByCategory.Clear(); a.StartedGoods.Clear(); a.OverCapGoods.Clear(); a.SkippedGoods.Clear(); a.BestDeadGoods.Clear(); a.DroppedGoods.Clear(); a.DroppedBlockers.Clear();
    }
}
