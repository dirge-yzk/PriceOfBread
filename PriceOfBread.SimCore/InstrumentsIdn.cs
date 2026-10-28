namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (640) 思想の必要性の表 idn_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsClock.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。
// 目的＝事前登録 measurements/idn640_20261028_prereg.txt [4] の G1（空でない門の前検査）・G2（開幕の必要性）・G3（社会主義と共産主義が別の量で動く）・R3・R4・R7 を読む。
// **旗 0 でも候補の量（新しい強度の部品）を毎回書く**＝腕 A の生が基線の読みになる。本体と同じ純関数 `Discovery.IdeaBoardOf`（第二の式を作らない）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。`Discovery.Step` の `Score` の直後（判定の値を書く前＝解禁する前）の断面。解禁済みの行・前提待ちの行は強度の列だけ書く（needXxx は 0）。
//   known* ＝その思想の行が解禁済みか（1／0）。
//   wageShare ＝ 賃金だけで暮らす人（工場労働者）の人口比（兵士を除く）／wageUnmet ＝ 社会主義の新しい強度（暮らしが必要に届かない人数÷人口）／workerSupplyFill ＝ 工場労働者の充足の人数加重平均（居なければ nan）／
//   ownerTake ＝ 工場の稼ぎのうち持ち主の取り分／sComNew ＝ 共産主義の新しい強度（取り分×wageShare）／workerMil ＝ 今の強度（社会主義と共産主義の同じ量）／colonialShare ＝ 帝国主義の強度／
//   crisis ＝ ファシズムの危機／popMil ＝ 人々の闘争度（Σ人数×闘争度÷人口）／sFasOld ＝ 危機×人々の闘争度（今の強度）／standing ＝ 国力の在庫／scoreNow ＝ 国力の今の値（nan＝単独の世界）／decline ＝ 落ち込み／sFasNew ＝ 危機×人々の闘争度×落ち込み。
//   need* ・tech* ・trans* ＝ 判定が実際に使った 3 項（旗に従う・Score の値そのもの）。
// ⚠限界（計器が自己申告する）：Score の POP の 1 巡と IdeaBoardOf の 1 巡は別（同じ定義を 2 度書く＝G1 で workerMil と needSoc〔旗 0〕の一致を確かめる）／危機は計器の側で FascismCrisis を呼ぶ
//   （挙げた国と年末の行だけ・読むだけ）／国の名前は表示名（建国・改称で別の名前になる）。
public static partial class Instruments
{
    internal static string IdnHeaderText()
        => "quarter\tyear\tnation\tpopK\tknownSoc\tknownCom\tknownImp\tknownFas\twageShare\twageUnmet\tworkerSupplyFill\townerTake\tsComNew\tworkerMil\tcolonialShare\tcrisis\tpopMil\tsFasOld\tstanding\tscoreNow\tdecline\tsFasNew"
         + "\tneedSoc\tneedCom\tneedImp\tneedFas\ttechSoc\ttechCom\ttechImp\ttechFas\ttransSoc\ttransCom\ttransImp\ttransFas";

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<string>> IdnPending = new();
    sealed class IdnSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, IdnSink> IdnSinks = new();

    /// <summary><see cref="Discovery.Step"/> の <c>Score</c> の直後（計器だけ）：この国のこの四半期の行を溜める（挙げた国は毎四半期・他は年末）。</summary>
    internal static void IdnObserve(World w, Discovery.WorldView? view, Coefficients c, Discovery.Terms[] terms)
    {
        var st = S(w);
        int q = Calendar.QuarterIndexOfTick(w.Tick);
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!(IsPicked(w, st) || yearEnd)) return;
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
        if (IdnSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
