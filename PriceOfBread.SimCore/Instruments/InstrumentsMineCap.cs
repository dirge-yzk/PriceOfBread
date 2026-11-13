using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。〔履歴 InstrumentsMineCap.cs#1〕
public static partial class Instruments
{
    internal const string MineCapHeaderText = "year\tnation\tgood\tclass\tproducers\toutputSum\tbuiltScaleSum\tbuiltOver15N\tbuiltOver15Output\tdepInitMissingOutShare\tdepFracOutW\tdepBin0\tdepBin1\tdepBin2\tdepBin3\tdepBin4\t"
        + "gradeOutW\tgradeBin0\tgradeBin1\tgradeBin2\tgradeBin3\ttarget15Share\ttarget30Share\tnationalMinesShare";

    /// <summary>鉱物の RGO 1 個体の読み（番人用）。DepFrac は鉱脈の初期が未捕捉なら NaN。</summary>
    internal readonly record struct McProd(int Good, string Class, double Output, double BuiltScale, double DepFrac, double Grade, double Target, bool NationalMines, double Price, double Mc);

    /// <summary>国 1 つの鉱物の RGO を全て読む。目標＝1＋(p−mc)/mc（p＝その州の清算の値段・mc＝RoRead の mc）・mc が Eps 以下なら margin＝0。</summary>
    internal static List<McProd> McProducers(World w)
    {
        var rows = new List<McProd>();
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Producers)
            {
                int g = p.GoodId;
                if (!p.IsRgo || g < 0 || g >= Catalog.Goods.Length || !Catalog.IsMineral(g) || g >= r.Markets.Length) continue;
                var x = RoRead(w, r, p);
                double price = r.Markets[g].Price;
                double margin = x.Mc > 1e-9 ? (price - x.Mc) / x.Mc : 0.0;
                double dep = r.MineralDepositInitial > 0 ? r.MineralDeposit / r.MineralDepositInitial : double.NaN;
                bool nat = r.NationalizedMask != 0 && Nationalization.SectorOf(g) == NationalizedSector.Mines && (r.NationalizedMask & (1 << (int)NationalizedSector.Mines)) != 0;
                rows.Add(new McProd(g, x.Class, p.Output, p.BuiltScale, dep, r.MineralGradeFactor, 1.0 + margin, nat, price, x.Mc));
            }
        }
        return rows;
    }

    static void MinecapTable(MultiNationSim sim, ulong seed, int q)
    {
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen || !Wants("minecap_s")) return;
        var st = McStates.GetValue(sim, _ => new McState());
        if (!st.Header) { using (var w0 = Open("minecap", seed)) w0.WriteLine(MineCapHeaderText); st.Header = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World; if (w.Regions.Count == 0) continue;
            double floor = w.Coef.DepositGradeFloor;
            var acc = new SortedDictionary<(int, string), McAcc>(Comparer<(int, string)>.Create((a, b) => { int t = a.Item1.CompareTo(b.Item1); return t != 0 ? t : string.CompareOrdinal(a.Item2, b.Item2); }));
            foreach (var m in McProducers(w))
            {
                var key = (m.Good, m.Class);
                if (!acc.TryGetValue(key, out var a)) acc[key] = a = new McAcc();
                a.N++; double o = m.Output > 0 ? m.Output : 0; a.O += o; a.Built += m.BuiltScale;
                if (m.BuiltScale > 1.5) { a.BuiltN++; a.BuiltO += o; }
                if (double.IsNaN(m.DepFrac)) a.DepMissing += o;
                else
                {
                    a.DepO += o; a.DepSum += o * m.DepFrac;
                    a.Dep[m.DepFrac < 0.1 ? 0 : m.DepFrac < 0.25 ? 1 : m.DepFrac < 0.5 ? 2 : m.DepFrac <= 1.0 ? 3 : 4] += o;
                }
                a.GradeSum += o * m.Grade;
                a.Grade[m.Grade <= floor ? 0 : m.Grade < 0.5 ? 1 : m.Grade < 1.0 ? 2 : 3] += o;
                if (m.Target > 1.5) a.T15 += o;
                if (m.Target > 3.0) a.T30 += o;
                if (m.NationalMines) a.Nat += o;
            }
            string nm = NameOf(w);
            foreach (var kv in acc)
            {
                var a = kv.Value; double o = a.O;
                string S(double v) => RpR(o > 0 ? v / o : double.NaN);
                sb.Append(string.Join('\t', I(w.Year), nm, Catalog.Goods[kv.Key.Item1].Name, kv.Key.Item2, I(a.N), RpR(o), RpR(a.Built), I(a.BuiltN), RpR(a.BuiltO), S(a.DepMissing), RpR(a.DepO > 0 ? a.DepSum / a.DepO : double.NaN),
                    S(a.Dep[0]), S(a.Dep[1]), S(a.Dep[2]), S(a.Dep[3]), S(a.Dep[4]), S(a.GradeSum), S(a.Grade[0]), S(a.Grade[1]), S(a.Grade[2]), S(a.Grade[3]), S(a.T15), S(a.T30), S(a.Nat))).Append('\n');
            }
        }
        if (sb.Length > 0) System.IO.File.AppendAllText(System.IO.Path.Combine(Dir, $"minecap_s{seed}.tsv"), sb.ToString(), new System.Text.UTF8Encoding(false));
    }

    sealed class McAcc { public int N, BuiltN; public double O, Built, BuiltO, DepMissing, DepO, DepSum, GradeSum, T15, T30, Nat; public readonly double[] Dep = new double[5], Grade = new double[4]; }
    sealed class McState { public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, McState> McStates = new();
}
