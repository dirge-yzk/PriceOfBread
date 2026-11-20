namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない （Pop／World／Region に欄なし）。〔履歴 InstrumentsAnchor.cs#1〕
public static partial class Instruments
{
    internal const string AnchorHeader = "quarter\tyear\tnation\tborn\tpopK\tdeltaK\tanchorOld\tanchorRegions\tanchorUsed\tregionsN\tactiveRegionsN\tanchorRegionsN\tcapacity\tcapBase\tarableSum\tfoodFill";

    static void AnchorFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!Tg.Anchor) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!(picked || yearEnd)) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;   // 死んだ国（人口 0）は行を出さない
        double anchorRegions = 0; int withAnchor = 0;
        foreach (var r in w.Regions)
        {
            anchorRegions += r.AnchorPopulation;
            if (r.AnchorPopulation > 0) withAnchor++;
        }
        double anchorOld = popK - w.PopulationDelta;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Anc ??= Open("anchor", seed);
        if (!sk.AncHeader) { sk.Anc.WriteLine(AnchorHeader); sk.AncHeader = true; }
        sk.Anc.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), w.BornOfSecession ? "1" : "0", F(popK), F(w.PopulationDelta), F(anchorOld), F(anchorRegions), F(w.DiagAnchorBaseline),
            I(w.Regions.Count), I(w.Regions.Count), I(withAnchor), F(w.DiagCapacity), F(w.DiagCapacityBaseline), F(w.DiagLandAnchor), F(w.FoodFill)));
    }
}
