namespace PriceOfBread.SimCore;

// (1359) 内戦の終わり 1 件ごとに 1 行の表 cwend_s。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（行の一覧と建国の記録は計器の中だけ）。
// 計器が自分で war と国から読む（エンジンの局所の RevCut・weary・decisive は呼ばず、同じ式を自分で書く）。crownpc_s の項は複写しない＝鍵 parentUid・quarter で読み手が結ぶ。
public static partial class Instruments
{
    internal const string CwendHeaderText = "quarter\tyear\tparentUid\trebelUid\tmonths\tscoreAtPeace\tendPath\texhParent\texhRebel\trevCutParent\trevCutRebel\t"
        + "inheritedExhaustion\tcapitalRisingFlag\tparentPolity\tcivilWarPolity\tsecessionFlag\tverdictCode\tblankSplitFlag\treunifyReservedFlag\tparentName\trebelName";

    sealed class CweRec
    {
        public int Month, Year, Months, ParentPol, WarPol, Verdict;
        public double Score, ExhP, ExhR, CutP, CutR, Inherited;
        public int CapRising;   // 1／0／−1（建国の記録が無い）
        public bool Secession, BlankSplit, Reserved;
        public string EndPath = "", ParentName = "", RebelName = "";
        public Power Parent = null!, Rebel = null!;
    }
    sealed class CweState { public int Month, Year; public readonly System.Collections.Generic.List<CweRec> Recs = new(); }
    sealed class CweBornRec { public double Exhaustion; public bool CapitalRising; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, CweState> CweStates = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Power, CweBornRec> CweBorns = new();

    /// <summary>月の歩の頭（CpMonth から）：終わりの行に載せる月と年を置く。</summary>
    internal static void CweMonth(Diplomacy d, int month, int year)
    {
        if (!Tg.Cwend) return;
        var st = CweStates.GetValue(d, _ => new CweState()); st.Month = month; st.Year = year;
    }

    /// <summary>建国（SpawnNation）の直後：生まれた国が実際に持った厭戦と首都発の建国かを置く（読むだけ）。</summary>
    internal static void CweBorn(Power born, bool capitalRising)
    {
        if (!On || !Tg.Cwend) return;   // 呼び手は On を読まずに呼ぶ
        CweBorns.Remove(born);
        CweBorns.Add(born, new CweBornRec { Exhaustion = born.WarExhaustion, CapitalRising = capitalRising });
    }

    /// <summary>終戦の 1 本（ConcludeWar）で ResolvePeace の前に呼ぶ：内戦だけ行を積む。</summary>
    internal static void CweEnd(Diplomacy d, Coefficients c, War war)
    {
        if (!On || !Tg.Cwend) return;   // 呼び手は On を読まずに呼ぶ（呼び手の On の読みの数を増やさない・OnDoubleReadScanTests）
        if (war.CivilWarParent is not { } parent) return;
        var rebel = war.A == parent ? war.B : war.A;
        var st = CweStates.GetValue(d, _ => new CweState());
        double CutOf(Power q) => 1.0 - (q.Nation is { } qn ? c.RevolutionSurrenderWeight * Politics.RevolutionPressure(qn) : 0);
        double cutP = CutOf(parent), cutR = CutOf(rebel);
        double exhP = parent.WarExhaustion, exhR = rebel.WarExhaustion;
        bool collapse = exhP >= 1.0 * cutP || exhR >= 1.0 * cutR;
        bool weary = !(war.A.WarExhaustion <= c.WarPeaceExhaustion * CutOf(war.A)) && !(war.B.WarExhaustion <= c.WarPeaceExhaustion * CutOf(war.B));
        string path = war.ForcedPeace ? "forced" : collapse ? "collapse" : System.Math.Abs(war.Score) >= 1.0 ? "decisive" : weary ? "weary" : "other";
        bool regime = !war.CivilWarSecession;
        var verdict = EndedWarRecord.VerdictOf(war.Score, regime, c.WhitePeaceScore);
        bool entryExit = war.CivilWarSecession && war.ForcedPeace;   // ResolvePeace の最初の枝（調停の分離の確定）
        var born = CweBorns.TryGetValue(rebel, out var b) ? b : null;
        st.Recs.Add(new CweRec
        {
            Month = st.Month, Year = st.Year, Months = war.Months, ParentPol = parent.Nation is { } pn ? (int)pn.Polity : -1, WarPol = (int)war.CivilWarPolity,
            Score = war.Score, ExhP = exhP, ExhR = exhR, CutP = cutP, CutR = cutR,
            Inherited = born is null ? double.NaN : born.Exhaustion, CapRising = born is null ? -1 : born.CapitalRising ? 1 : 0,
            Secession = war.CivilWarSecession, Verdict = (int)verdict, BlankSplit = !entryExit && verdict == WarVerdict.Blank,
            Reserved = parent.Nation?.SecedingRegion is not null, EndPath = path,
            ParentName = NameOf(parent), RebelName = NameOf(rebel), Parent = parent, Rebel = rebel,
        });
    }

    static void CweTable(Sink sk, Diplomacy d)
    {
        if (!Tg.Cwend) return;
        if (!CweStates.TryGetValue(d, out var st)) return;
        using var wr = Open("cwend", sk.Seed);
        wr.WriteLine(CwendHeaderText);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int Uid(Power p) => p.Nation is { } w && States.TryGetValue(w, out var ws) ? ws.Uid : 0;
        foreach (var r in st.Recs)
            wr.WriteLine(string.Join('\t', I((int)Calendar.PerQuarterToPerMonth(r.Month)), I(r.Year), I(Uid(r.Parent)), I(Uid(r.Rebel)), I(r.Months), F(r.Score), r.EndPath,
                F(r.ExhP), F(r.ExhR), F(r.CutP), F(r.CutR), F(r.Inherited), I(r.CapRising), I(r.ParentPol), I(r.WarPol),
                r.Secession ? "1" : "0", I(r.Verdict), r.BlankSplit ? "1" : "0", r.Reserved ? "1" : "0", r.ParentName, r.RebelName));
    }
}
