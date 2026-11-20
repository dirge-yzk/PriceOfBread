namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。〔履歴 InstrumentsIdn.cs#1〕
public static partial class Instruments
{
    internal static string IdnHeaderText()
        => "quarter\tyear\tnation\tpopK\tknownSoc\tknownCom\tknownImp\tknownFas\twageShare\twageUnmet\tworkerSupplyFill\townerTake\tsComNew\tworkerMil\tcolonialShare\tcrisis\tpopMil\tsFasOld\tstanding\tscoreNow\tdecline\tsFasNew"
         + "\tneedSoc\tneedCom\tneedImp\tneedFas\ttechSoc\ttechCom\ttechImp\ttechFas\ttransSoc\ttransCom\ttransImp\ttransFas";

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<string>> IdnPending = new();
    sealed class IdnSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, IdnSink> IdnSinks = new();

    /// <summary><see cref="Discovery.Step"/> の <c>Score</c> の直後（計器だけ）：この国のこの四半期の行を溜める（全ての国・毎四半期）。</summary>
    internal static void IdnObserve(World w, Discovery.WorldView? view, Coefficients c, Discovery.Terms[] terms)
    {
        if (!Tg.Idn) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        int q = Calendar.QuarterIndexOfTick(w.Tick);   // 全ての国を毎四半期書く（G1 の覆いを〈全ての国・全ての四半期〉にそろえる＝事前登録 追記 3）
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;
        var b = Discovery.IdeaBoardOf(w, view?.ScoreNow);
        double crisis = Ideologies.FascismCrisis(w);
        var k = w.Discovered;
        string K(int row) => Discovery.Known(k, row) ? "1" : "0";
        string T(int row, int part) => row < 0 || row >= terms.Length ? "nan" : F(part == 0 ? terms[row].Tech : part == 1 ? terms[row].Need : terms[row].Transmit);
        int rs = Discovery.SocialismRow, rc = Discovery.CommunismRow, ri = Discovery.ImperialismRow, rf = Discovery.FascismRow;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var cols = new[]
        {
            q.ToString(inv), w.Year.ToString(inv), NameOf(w), F(popK), K(rs), K(rc), K(ri), K(rf),
            F(b.WageShare), F(b.WageUnmet), F(b.WorkerSupplyFill), F(b.OwnerTake), F(Discovery.ComStrength(b)), F(b.WorkerMil), F(b.ColonialShare),
            F(crisis), F(b.PopMil), F(crisis * b.PopMil), F(w.ScoreStanding), view?.ScoreNow is { } sn ? F(sn) : "nan", F(b.Decline), F(Discovery.FasStrength(crisis, b.PopMil, b)),
            T(rs, 1), T(rc, 1), T(ri, 1), T(rf, 1), T(rs, 0), T(rc, 0), T(ri, 0), T(rf, 0), T(rs, 2), T(rc, 2), T(ri, 2), T(rf, 2),
        };
        IdnPending.GetOrCreateValue(w).Add(string.Join('\t', cols));
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を書き出す。</summary>
    static void IdnTable(MultiNationSim sim, ulong seed)
    {
        if (!Tg.Idn) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        foreach (var n in sim.Nations)
        {
            if (!IdnPending.TryGetValue(n.World, out var rows) || rows.Count == 0) continue;
            var sk = IdnSinks.GetValue(sim, _ => new IdnSink());
            sk.W ??= Open("idn", seed);
            if (!sk.Header) { sk.W.WriteLine(IdnHeaderText()); sk.Header = true; }
            foreach (var row in rows) sk.W.WriteLine(row);
            rows.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void IdnClose(MultiNationSim sim)
    {
        if (!Tg.Idn) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (IdnSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
