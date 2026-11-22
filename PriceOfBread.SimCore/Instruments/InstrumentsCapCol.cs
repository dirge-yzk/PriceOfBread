namespace PriceOfBread.SimCore;

// (1304) c135nc 計器 capcol_s：四半期の末に全ての国の首都の格と、首都の付け替え（Territory.TransferRegion の首都の割譲・蜂起＝NextCapital の返り）の旧新。
// 付け替えの受け口は Territory.TransferRegion の首都の行の 2 か所だけ（CapColPickIfOn＝付け替えの前の盤面で計器が旧と新の規則の選びを自分で組む・
// CapColSetIfOn＝SetCapital の直後に実際の首都〔Regions[0]〕を読む）。エンジンの関数を呼ばない（NextCapital・OutputRefValue を呼ばない・州の産出と格を自分で読む）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable＝セーブに載らない）。Instruments.On かつ表が選ばれた時だけ動く。
// 列：homeRegions＝本国格（Core・Integrated）の州の数・g1Flag＝内戦でなく本国格が 1 つ以上で首都が植民地格・picksCount＝この四半期の付け替えの受け口の回数・
//   diffOldCount／diffNewCount＝実際の首都が旧の規則（格を見ない）／新の規則（本国格が有れば本国格だけ）の選びと違った回数・diffOldHome0Count＝そのうち本国格が 0 の盤面の回数・
//   last*＝その四半期の最後の付け替えの実際の首都と旧と新の選び（無ければ -）・capitalChangedFlag＝首都の州が前の四半期の末と違う・otherChangeFlag＝変わったのに受け口を通っていない。
public static partial class Instruments
{
    internal const string CapColHeaderText = "quarter\tyear\tnationIndex\tnationUid\tnation\thomeFirstFlag\tregions\thomeRegions\tcapitalRegion\tcapitalTier"
        + "\tcivilWarFlag\tg1Flag\tpicksCount\tdiffOldCount\tdiffNewCount\tdiffOldHome0Count\tlastActual\tlastOldRulePick\tlastNewRulePick\tlastPickHomeRegions"
        + "\tcapitalChangedFlag\totherChangeFlag";

    sealed class CcWorld
    {
        public int Picks, DiffOld, DiffNew, DiffOldHome0, PickHome = -1;
        public string PendOld = "-", PendNew = "-", LastActual = "-", LastOld = "-", LastNew = "-";
        public Region? Prev; public bool HavePrev;
    }
    sealed class CcSink { public System.IO.StreamWriter? W; public readonly List<string> Peek = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, CcWorld> CcWorlds = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CcSink> CcSinks = new();

    /// <summary>計器の選び（サービス以外の産出×参照価格の最大・同じ値は Id の小さい方）。homeFirst＝本国格が 1 つでもあれば本国格だけから。</summary>
    static Region? CcPick(World w, bool homeFirst)
    {
        bool home = false;
        if (homeFirst) foreach (var q in w.Regions) if (q.Tier != RegionTier.Colonial) { home = true; break; }
        Region? b = null; double bv = double.MinValue;
        foreach (var q in w.Regions)
        {
            if (home && q.Tier == RegionTier.Colonial) continue;
            double g = 0;
            foreach (var s in q.Producers) { var good = q.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; }
            if (g > bv || (g == bv && (b is null || q.Id < b.Id))) { bv = g; b = q; }
        }
        return b;
    }

    static int CcHome(World w) { int n = 0; foreach (var q in w.Regions) if (q.Tier != RegionTier.Colonial) n++; return n; }

    /// <summary>受け口 1（首都の付け替えの前の盤面・首都の州は from.Regions から抜けた後）：旧と新の規則の選びを計器が組んで持つ。</summary>
    internal static void CapColPickIfOn(World from)
    {
        if (!On || !Wants("capcol_s")) return;
        var s = CcWorlds.GetValue(from, _ => new CcWorld());
        s.PendOld = CcPick(from, false)?.Name ?? "-";
        s.PendNew = CcPick(from, true)?.Name ?? "-";
        s.PickHome = CcHome(from);
    }

    /// <summary>受け口 2（SetCapital の直後）：実際の首都（Regions[0]・Id 0 で無ければ -）を読み、旧と新の選びと比べて数える。</summary>
    internal static void CapColSetIfOn(World from)
    {
        if (!On || !Wants("capcol_s")) return;
        var s = CcWorlds.GetValue(from, _ => new CcWorld());
        string actual = from.Regions.Count > 0 && from.Regions[0].Id == 0 ? from.Regions[0].Name : "-";
        s.Picks++;
        if (actual != s.PendOld) { s.DiffOld++; if (s.PickHome == 0) s.DiffOldHome0++; }
        if (actual != s.PendNew) s.DiffNew++;
        s.LastActual = actual; s.LastOld = s.PendOld; s.LastNew = s.PendNew;
    }

    /// <summary>1 国の行（quarter の前まで・試験の読み口）。州が無ければ null。読んだ四半期の数えを 0 へ戻す。</summary>
    internal static string? CapColRow(World w, int nationIndex)
    {
        if (w.Regions.Count == 0) return null;
        var s = CcWorlds.GetValue(w, _ => new CcWorld());
        var cap = w.Regions[0];
        int home = CcHome(w);
        bool civil = w.InCivilWar;
        bool changed = s.HavePrev && !ReferenceEquals(s.Prev, cap);
        static string B(bool b) => b ? "1" : "0";
        static string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string row = string.Join('\t', F(w.Year), I(nationIndex), I(States.TryGetValue(w, out var wst) ? wst.Uid : 0), NameOf(w), B(w.Coef.NextCapitalHomeFirst > 0), I(w.Regions.Count), I(home),
            cap.Name, cap.Tier.ToString(), B(civil), B(!civil && home >= 1 && cap.Tier == RegionTier.Colonial),
            I(s.Picks), I(s.DiffOld), I(s.DiffNew), I(s.DiffOldHome0), s.LastActual, s.LastOld, s.LastNew, I(s.Picks > 0 ? s.PickHome : -1),
            B(changed), B(changed && s.Picks == 0));
        s.Picks = s.DiffOld = s.DiffNew = s.DiffOldHome0 = 0; s.PickHome = -1;
        s.LastActual = s.LastOld = s.LastNew = "-";
        s.Prev = cap; s.HavePrev = true;
        return row;
    }

    /// <summary>試験の読み口：直前の四半期に書いた行（quarter の列から）。</summary>
    internal static IReadOnlyList<string> CapColPeek(MultiNationSim sim) => CcSinks.GetValue(sim, _ => new CcSink()).Peek;

    /// <summary>四半期の末：全ての国の行を書く。</summary>
    static void CapColTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("capcol_s")) return;
        var sk = CcSinks.GetValue(sim, _ => new CcSink());
        sk.Peek.Clear();
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            if (CapColRow(sim.Nations[i].World, i) is not { } row) continue;
            if (sk.W is null) { sk.W = Open("capcol", seed); sk.W.WriteLine(CapColHeaderText); }
            string line = qs + "\t" + row;
            sk.W.WriteLine(line); sk.Peek.Add(line);
        }
    }

    static void CapColClose(MultiNationSim sim) { if (CcSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
