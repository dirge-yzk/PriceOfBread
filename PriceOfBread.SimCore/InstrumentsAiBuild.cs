namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (579) AI の国営工場の着工の門の表 aib_s（読むだけ・恒等） ---------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEquipment.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（蓄えは ConditionalWeakTable の側）。
// 目的＝事前登録 measurements/ukf579_20261027_prereg.txt の狙い（AI の着工の数と拒まれた理由・軍需と先端の工場の着工・門の閉じた向き・鎖の中間）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末＝その年の 4 四半期の合計。蓄えは行を出したときに戻す）。人口が 0 の国は出さない。
// 列（単位は接尾辞）：
//   flagLevel（AiBuildNoCostCap）／calls（TryBuildFacility に入った回）／returnFundingCount（国家の支払い率 r＜1 で戻った回）／returnRoomCount（月の出費の余地が無くて戻った回）／
//   roomEntryPerMonthMean（余地を測った回の〈FacilityBuildOutlayShare×バッファ−今ある工事と未着工の意志の月の出費〉の平均）／bufferEntryMean（FiscalBufferOf の平均）／
//   treasuryEntryStock（最後に入った回の国庫）／
//   overCostCapCandidateCount（他の全ての門を通ったのに旧の身の丈の門〔総費用 ＞ FacilityBuildCostCap×バッファ〕なら落ちた候補〔州×財〕の数。旗が 0 の国では旧の門が先に落とす＝0）／
//   startedCount・startedOverCapCount・refusedCount（州の最良を SetStateTarget で押して、着工した件〔うち総費用が旧の身の丈の門を超える件〕／拒まれて意志が残った件）／skipRoomCount（その州の工事の月の出費の見込みが余地を超えて飛ばした件）／
//   startedOutlayPerMonth（着工した件の見込みの月の出費の和）／startedByCategory（FactorySpec の組ごとの着工の件数）／startedGoods・overCapGoods・skippedGoods（財の名前:件数＝着工した／旧の身の丈の門なら落ちた／余地が足りずに飛ばした州の最良）／
//   鎖の中間（四半期の末の断面・全ての州の生産者の和）：warshipScaleSum・sailingShipScaleSum・artilleryScaleSum・rifleScaleSum・steelScaleSum（その財の Scale の和）／
//   warshipBuiltSum・sailingShipBuiltSum・artilleryBuiltSum・rifleBuiltSum（BuiltScale の和＝完成した棟の累計）／warshipBuildingCount・sailingShipBuildingCount・artilleryBuildingCount・rifleBuildingCount（工事中の棟の数）。
//   ＝〈工場が建った→軍艦の生産の能力が増えた〉の中間（財ごとの Scale の計器は eqr_s にこの財たちが無かった〔発見の行を持たない財は載らない〕ので足した）。
// ⚠限界（計器が自己申告する）：着工の判断の断面（実際の工事の進み方は読まない）／旗 0 の国は overCostCapCandidateCount が 0（旧の門が先に落とす）＝旧との比べは startedCount の差で見る／年末の行は 4 四半期の合計（鎖の中間は年末の断面）。
public static partial class Instruments
{
    internal const string AibHeaderText = "quarter\tyear\tnation\tflagLevel\tcalls\treturnFundingCount\treturnRoomCount\troomEntryPerMonthMean\tbufferEntryMean\tspareEntryPerMonth\ttreasuryEntryStock\t"
        + "overCostCapCandidateCount\tstartedCount\trefusedCount\tskipRoomCount\tstartedOutlayPerMonth\tstartedByCategory\tstartedGoods\toverCapGoods\tskippedGoods\t"
        + "warshipScaleSum\tsailingShipScaleSum\tartilleryScaleSum\trifleScaleSum\tsteelScaleSum\twarshipBuiltSum\tsailingShipBuiltSum\tartilleryBuiltSum\trifleBuiltSum\twarshipBuildingCount\tsailingShipBuildingCount\tartilleryBuildingCount\trifleBuildingCount";

    sealed class AibAcc
    {
        public int Calls, ReturnFunding, ReturnRoom, OverCap, Started, StartedOverCap, Refused, SkipRoom;
        public double RoomSum, BufferSum, TreasuryEntry, StartedOutlay;
        public readonly SortedDictionary<string, int> ByCategory = new(StringComparer.Ordinal), StartedGoods = new(StringComparer.Ordinal), OverCapGoods = new(StringComparer.Ordinal), SkippedGoods = new(StringComparer.Ordinal);
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
            a.Calls = a.ReturnFunding = a.ReturnRoom = a.OverCap = a.Started = a.StartedOverCap = a.Refused = a.SkipRoom = 0;
            a.RoomSum = a.BufferSum = a.TreasuryEntry = a.StartedOutlay = 0;
            a.ByCategory.Clear(); a.StartedGoods.Clear(); a.OverCapGoods.Clear(); a.SkippedGoods.Clear();
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
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                int g = p.GoodId;
                if (g == Catalog.WarshipId) { wsS += p.Scale; wsB += p.BuiltScale; if (p.ConstructionLeft > 0) wsN++; }
                else if (g == Catalog.SailingShipId) { shS += p.Scale; shB += p.BuiltScale; if (p.ConstructionLeft > 0) shN++; }
                else if (g == Catalog.ArtilleryId) { arS += p.Scale; arB += p.BuiltScale; if (p.ConstructionLeft > 0) arN++; }
                else if (g == Catalog.RifleId) { rfS += p.Scale; rfB += p.BuiltScale; if (p.ConstructionLeft > 0) rfN++; }
                else if (g == Catalog.SteelId) stS += p.Scale;
            }
        sk.Aib ??= Open("aib", seed);
        if (!sk.AibHeader) { sk.Aib.WriteLine(AibHeaderText); sk.AibHeader = true; }
        sk.Aib.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(w.Coef.AiBuildNoCostCap), I(a.Calls), I(a.ReturnFunding), I(a.ReturnRoom),
            F(enters > 0 ? a.RoomSum / enters : double.NaN), F(enters > 0 ? a.BufferSum / enters : double.NaN), F(a.TreasuryEntry),
            I(a.OverCap), I(a.Started), I(a.StartedOverCap), I(a.Refused), I(a.SkipRoom), F(a.StartedOutlay), Join(a.ByCategory), Join(a.StartedGoods), Join(a.OverCapGoods), Join(a.SkippedGoods),
            F(wsS), F(shS), F(arS), F(rfS), F(stS), F(wsB), F(shB), F(arB), F(rfB), I(wsN), I(shN), I(arN), I(rfN)));
        a.Calls = a.ReturnFunding = a.ReturnRoom = a.OverCap = a.Started = a.StartedOverCap = a.Refused = a.SkipRoom = 0;
        a.RoomSum = a.BufferSum = a.TreasuryEntry = a.StartedOutlay = 0;
        a.ByCategory.Clear(); a.StartedGoods.Clear(); a.OverCapGoods.Clear(); a.SkippedGoods.Clear();
    }
}
