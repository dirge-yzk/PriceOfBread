namespace PriceOfBread.SimCore;

// (1408) 計器 sinkgood_s：投入が届かない工場（LastInputFill＜0.05）と、労働の重みの上位 5 の工場を、四半期の末に 1 工場 1 行で並べる（rh の申し送り 1）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。エンジンの関数は呼ばず、Producers と Markets に残っている値を読むだけ。
// 労働の重み＝EconomyStep.cs の労働配分の式（LaborWeight×Scale・労働力の財の生産者は配分の外・同じ LaborPopIndex の和が分母）を写して数える。
// 投入 1 単位あたりの費用は価格×QtyPerOutput の和（EffectiveQty の機械化・電化の割引は写さない）。
public static partial class Instruments
{
    internal const int SinkgoodMaxInputs = 3;
    internal const int SinkgoodTopWeight = 5;
    internal const double SinkgoodFillBelow = 0.05;

    internal static readonly string SinkgoodHeaderText =
        "quarter\tyear\tnation\tregion\tgood\trecipe\tlaborWeightScaled\tfraction\tlastInputFill\toutput\tprice\tinputCostPerUnit"
        + string.Concat(System.Linq.Enumerable.Range(1, SinkgoodMaxInputs).Select(i => $"\tinputGood{i}\tinputPrice{i}\tinputPrevImport{i}\tinputLastProduced{i}\tinputStock{i}"));

    sealed class SinkgoodSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SinkgoodSink> SinkgoodSinks = new();

    /// <summary>1 つの州の全生産者の（重み・割合）。労働力の財の生産者は重み 0・割合 0（配分の外）。割合＝重み÷同じ LaborPopIndex の重みの和（和が 0 なら 0）。</summary>
    internal static (double[] Weight, double[] Fraction) SinkgoodFractions(Region r, Coefficients? c = null)
    {
        int n = r.Producers.Count;
        var wt = new double[n]; var fr = new double[n];
        var total = new Dictionary<int, double>(); var count = new Dictionary<int, int>();
        bool eqOn = c is not null && c.RecipeEquipment > 0;   // エンジンの FactoryEquipment.On（助走の凍結は四半期の末には無い）。c＝null は装置の項なし
        var inGroup = new bool[n];
        for (int i = 0; i < n; i++)
        {
            var s = r.Producers[i];
            if (SinkgoodDormant(r, s)) continue;                                    // 眠りの生産者は重みの式の外（エンジンの continue）
            if (eqOn && s.Bankrupt && FactoryEquipment.Eligible(s)) continue;       // 倒産した装置の重み 0（分母にも数に入らない）
            if (Catalog.IsLaborGood(s.GoodId)) continue;
            wt[i] = s.LaborWeight * s.Scale;
            inGroup[i] = true;
            total[s.LaborPopIndex] = total.GetValueOrDefault(s.LaborPopIndex) + wt[i];
            count[s.LaborPopIndex] = count.GetValueOrDefault(s.LaborPopIndex) + 1;
        }
        for (int i = 0; i < n; i++)
        {
            if (!inGroup[i]) continue;   // 労働力の財・眠り・倒産した装置は割合 0
            var s = r.Producers[i];
            double t = total.GetValueOrDefault(s.LaborPopIndex), k = count.GetValueOrDefault(s.LaborPopIndex);
            fr[i] = t > 1e-9 ? wt[i] / t : k > 0 ? 1.0 / k : 1.0;   // エンジンの分母 0 の扱い（EconomyStep.cs の fraction）
        }
        return (wt, fr);
    }

    /// <summary>エンジンの眠りの判定（EconomyStep.SleepState の 1）の写し（計器の自前の式）：規模 0・産出 0・倒産なし・建設なし・発明の門が閉じている。</summary>
    internal static bool SinkgoodDormant(Region r, Producer s)
        => !(s.Scale != 0 || s.Output != 0 || s.Bankrupt || s.BuiltScale > 1e-9 || s.ConstructionLeft > 0 || s.SiteRow >= 0
             || s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length || Catalog.Goods[s.GoodId].Tier == GoodTier.Service)
           && !(Discovery.Gate(r, s) > 0);

    /// <summary>1 つの州の出す行（見出し以降の列・四半期と国の列は呼び手が付ける）。filter が偽なら絞らず全生産者。</summary>
    internal static List<string> SinkgoodRegionRows(Region r, bool filter, Coefficients? c = null)
    {
        var rows = new List<string>();
        var (wt, fr) = SinkgoodFractions(r, c);
        var pick = new HashSet<int>();
        var order = new List<int>();
        for (int i = 0; i < r.Producers.Count; i++)
        {
            var s = r.Producers[i];
            if (Catalog.IsLaborGood(s.GoodId) || !(s.Scale > 1e-9) || SinkgoodDormant(r, s)) continue;
            order.Add(i);
            if (s.Inputs.Count > 0 && s.LastInputFill < SinkgoodFillBelow && !s.Bankrupt) pick.Add(i);   // 倒産は操業停止（産出 0）＝投入が届かない工場ではない
        }
        var top = order.OrderByDescending(i => wt[i]).ThenBy(i => i).Take(SinkgoodTopWeight);
        foreach (var i in top) pick.Add(i);
        foreach (int i in order)
        {
            if (filter && !pick.Contains(i)) continue;
            var s = r.Producers[i];
            double cost = 0;
            foreach (var inp in s.Inputs) cost += r.Markets[inp.GoodId].Price * inp.QtyPerOutput;
            var cols = new List<string>
            {
                Catalog.Goods[s.GoodId].Name, s.RecipeName, F(wt[i]), F(fr[i]), F(s.LastInputFill), F(s.Output), F(r.Markets[s.GoodId].Price), F(cost),
            };
            for (int k = 0; k < SinkgoodMaxInputs; k++)
            {
                if (k < s.Inputs.Count)
                {
                    var m = r.Markets[s.Inputs[k].GoodId];
                    cols.Add(Catalog.Goods[s.Inputs[k].GoodId].Name); cols.Add(F(m.Price)); cols.Add(F(m.PrevImport)); cols.Add(F(m.LastProduced)); cols.Add(F(m.Stock));
                }
                else { cols.Add(""); cols.Add("nan"); cols.Add("nan"); cols.Add("nan"); cols.Add("nan"); }
            }
            rows.Add(r.Name + "\t" + string.Join('\t', cols));
        }
        return rows;
    }

    static void SinkgoodTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("sinkgood_s")) return;
        var sk = SinkgoodSinks.GetValue(sim, _ => new SinkgoodSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var nat in sim.Nations)
        {
            var w = nat.World; var st = S(w);
            if (!IsPicked(w, st) || !(w.TotalPopulation > 0)) continue;
            foreach (var r in w.Regions)
            {
                if (r.Tier == RegionTier.Colonial) continue;
                foreach (var row in SinkgoodRegionRows(r, true, w.Coef))
                {
                    sk.W ??= Open("sinkgood", seed);
                    if (!sk.Header) { sk.W.WriteLine(SinkgoodHeaderText); sk.Header = true; }
                    sk.W.WriteLine(I(q) + "\t" + I(w.Year) + "\t" + NameOf(w) + "\t" + row);
                }
            }
        }
    }

    static void SinkgoodClose(MultiNationSim sim) { if (SinkgoodSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
