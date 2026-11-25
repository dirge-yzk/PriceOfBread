namespace PriceOfBread.SimCore;

// (1336) c140sp 計器 stproc_s：国家の購入（InternationalMarket.Procure の buyer null＝Colonial.BuyUpkeepGood）の量と、旧の規則（LastExport に積む）と新の規則（LastSold に積む）の輸出の値打ちを同じ盤面で数える。
// 受け口は Procure の buyer null の 1 か所（StProcIfOn・want・buy・単価を受け取るだけ）。四半期の末に、その国の全ての州×全ての財の LastExport×Price を計器が自分で数える（エンジンの関数を呼ばない）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable＝セーブに載らない）。Instruments.On かつ表が選ばれた時だけ書く。
// 列：asSoldFlag＝旗 StateProcureAsSold・procCount＝この四半期の国家の購入の回数（buy＞0）・wantValueQ／buyValueQ＝want×単価／buy×単価の四半期の和・
//   settleTickMatchFlag＝決済の月の値打ちを数えた Tick が行の Tick と同じ・settleBuyValueNow＝その Tick の国家の購入の値打ち（旧の規則が今の LastExport に入れている分）・
//   exportValueNow＝Σ LastExport×Price（今の盤面）・exportValueOldRuleNow／exportValueNewRuleNow＝旧／新の規則の輸出の値打ち（旗 0 なら旧＝今・新＝今−決済の月・旗 1 なら新＝今・旧＝今＋決済の月）・
//   stateShareOldRuleNow＝決済の月÷旧の規則の輸出の値打ち。
public static partial class Instruments
{
    internal const string StProcHeaderText = "quarter\tyear\tnationIndex\tnation\tasSoldFlag\tprocCount\twantValueQ\tbuyValueQ"
        + "\tsettleTickMatchFlag\tsettleBuyValueNow\texportValueNow\texportValueOldRuleNow\texportValueNewRuleNow\tstateShareOldRuleNow";

    sealed class SpWorld { public int Count; public double WantQ, BuyQ, Settle; public long Tick = long.MinValue; }
    sealed class SpSink { public System.IO.StreamWriter? W; public readonly List<string> Peek = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, SpWorld> SpWorlds = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SpSink> SpSinks = new();

    /// <summary>受け口：国家の購入 1 回（buy は在庫から出た量・price はその時の単価）。</summary>
    internal static void StProcIfOn(World w, double want, double buy, double price)
    {
        if (!On) return;
        var s = SpWorlds.GetValue(w, _ => new SpWorld());
        if (s.Tick != w.Tick) { s.Tick = w.Tick; s.Settle = 0; }
        s.Count++; s.WantQ += want * price; s.BuyQ += buy * price; s.Settle += buy * price;
    }

    /// <summary>1 国の行（quarter の前まで・試験の読み口）。読んだ四半期の数えを 0 へ戻す（決済の月の値打ちは Tick で戻す）。</summary>
    internal static string StProcRow(World w, int nationIndex)
    {
        var s = SpWorlds.GetValue(w, _ => new SpWorld());
        double now = 0;
        foreach (var r in w.Regions) foreach (var m in r.Markets) now += m.LastExport * m.Price;
        bool match = s.Tick == w.Tick;
        double settle = match ? s.Settle : 0;
        bool asSold = w.Coef.StateProcureAsSold > 0;
        double oldRule = asSold ? now + settle : now, newRule = asSold ? now : now - settle;
        static string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string row = string.Join('\t', F(w.Year), I(nationIndex), NameOf(w), asSold ? "1" : "0", I(s.Count), F(s.WantQ), F(s.BuyQ),
            match ? "1" : "0", F(settle), F(now), F(oldRule), F(newRule), F(oldRule > 0 ? settle / oldRule : double.NaN));
        s.Count = 0; s.WantQ = s.BuyQ = 0;
        return row;
    }

    /// <summary>試験の読み口：直前の四半期に書いた行（quarter の列から）。</summary>
    internal static IReadOnlyList<string> StProcPeek(MultiNationSim sim) => SpSinks.GetValue(sim, _ => new SpSink()).Peek;

    /// <summary>四半期の末：全ての国の行を書く。</summary>
    static void StProcTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("stproc_s")) return;
        var sk = SpSinks.GetValue(sim, _ => new SpSink());
        sk.Peek.Clear();
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (w.Regions.Count == 0) continue;
            if (sk.W is null) { sk.W = Open("stproc", seed); sk.W.WriteLine(StProcHeaderText); }
            string line = qs + "\t" + StProcRow(w, i);
            sk.W.WriteLine(line); sk.Peek.Add(line);
        }
    }

    static void StProcClose(MultiNationSim sim) { if (SpSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
