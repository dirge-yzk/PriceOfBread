namespace PriceOfBread.SimCore;

// (1290) c133mr 計器 labeltier_s：四半期の末に国ごとに、国の主流の札（Power.Culture・Power.Religion）と、同じ盤面の旧の規則
// （ColonyLabelsFromData 1＝本国格の州だけ・0＝全ての州）と新の規則（旗 MajorityLabelTierOrder：本国格に札の付いた人口が無い札だけ植民地格の州で数える）を
// 計器が自分で数えて並べる。エンジンの関数を呼ばない（Diplomacy.MajorityCultureReligion を呼ばない・州と札と POP の人口を自分で読む）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable＝セーブに載らない）。
// 列：tierC・tierR＝新の規則が札を選んだ格（H 本国格・C 植民地格・A 全ての州〔ColonyLabelsFromData 0〕・- 空）。changedC・changedR＝新≠旧。
public static partial class Instruments
{
    internal const string LabelTierHeaderText = "quarter\tyear\tnation\tcolonyData\ttierOrder\tregions\tcolonialRegions"
        + "\tpowerCulture\tpowerReligion\tpowerCultureEmptyFlag\tpowerReligionEmptyFlag"
        + "\thomeLabelledPopC\thomeLabelledPopR\tcolLabelledPopC\tcolLabelledPopR"
        + "\toldCulture\toldReligion\tnewCulture\tnewReligion\ttierC\ttierR\tcapCulture\tcapReligion\tchangedC\tchangedR";

    sealed class LtSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LtSink> LtSinks = new();

    // 計器の多数決（人口の和が最大の札・同じ値は先に現れた札＝州の並びの順）
    static string LtBest(List<(string Label, double Pop)> t)
    {
        var sum = new List<(string Label, double Pop)>();
        foreach (var (lab, pop) in t)
        {
            int k = sum.FindIndex(x => x.Label == lab);
            if (k < 0) sum.Add((lab, pop)); else sum[k] = (lab, sum[k].Pop + pop);
        }
        string b = ""; double bv = 0;
        foreach (var (lab, v) in sum) if (v > bv) { bv = v; b = lab; }
        return b;
    }

    /// <summary>1 国の行（quarter の前まで・試験の読み口）。その国に州が無いか Power が無ければ null。</summary>
    internal static string? LabelTierRow(World w)
    {
        if (w.SelfPower is not { } pw || w.Regions.Count == 0) return null;
        bool data = w.Coef.ColonyLabelsFromData > 0, order = w.Coef.MajorityLabelTierOrder > 0;
        var hC = new List<(string, double)>(); var hR = new List<(string, double)>();
        var cC = new List<(string, double)>(); var cR = new List<(string, double)>();
        double hpC = 0, hpR = 0, cpC = 0, cpR = 0; int nCol = 0;
        foreach (var r in w.Regions)
        {
            bool col = r.Tier == RegionTier.Colonial;
            if (col) nCol++;
            double pop = 0; foreach (var p in r.Pops) pop += p.Size;
            if (!(pop > 0)) continue;
            if (r.Culture.Length > 0) { if (col) { cC.Add((r.Culture, pop)); cpC += pop; } else { hC.Add((r.Culture, pop)); hpC += pop; } }
            if (r.Religion.Length > 0) { if (col) { cR.Add((r.Religion, pop)); cpR += pop; } else { hR.Add((r.Religion, pop)); hpR += pop; } }
        }
        // 旧：ColonyLabelsFromData 1 は本国格だけ・0 は全ての州（州の並びの順に足す）
        List<(string, double)> All(bool culture)
        {
            var a = new List<(string, double)>();
            foreach (var r in w.Regions)
            {
                double pop = 0; foreach (var p in r.Pops) pop += p.Size;
                if (!(pop > 0)) continue;
                string lab = culture ? r.Culture : r.Religion;
                if (lab.Length > 0) a.Add((lab, pop));
            }
            return a;
        }
        string oldC = data ? LtBest(hC) : LtBest(All(true));
        string oldR = data ? LtBest(hR) : LtBest(All(false));
        string newC = oldC, newR = oldR, tC = data ? (hC.Count > 0 ? "H" : "-") : "A", tR = data ? (hR.Count > 0 ? "H" : "-") : "A";
        if (data && order)
        {
            if (hC.Count == 0) { newC = LtBest(cC); tC = cC.Count > 0 ? "C" : "-"; }
            if (hR.Count == 0) { newR = LtBest(cR); tR = cR.Count > 0 ? "C" : "-"; }
        }
        static string B(bool b) => b ? "1" : "0";
        var cap = w.Region;
        return string.Join('\t', F(w.Year), NameOf(w), B(data), B(order), w.Regions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            nCol.ToString(System.Globalization.CultureInfo.InvariantCulture), pw.Culture, pw.Religion, B(pw.Culture.Length == 0), B(pw.Religion.Length == 0),
            F(hpC), F(hpR), F(cpC), F(cpR), oldC, oldR, newC, newR, tC, tR, cap.Culture, cap.Religion, B(newC != oldC), B(newR != oldR));
    }

    /// <summary>四半期の末：全ての国の行を書く。</summary>
    static void LabelTierTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("labeltier_s")) return;
        var sk = LtSinks.GetValue(sim, _ => new LtSink());
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            if (LabelTierRow(n.World) is not { } row) continue;
            if (sk.W is null) { sk.W = Open("labeltier", seed); sk.W.WriteLine(LabelTierHeaderText); }
            sk.W.Write(qs); sk.W.Write('\t'); sk.W.WriteLine(row);
        }
    }

    static void LabelTierClose(MultiNationSim sim) { if (LtSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
