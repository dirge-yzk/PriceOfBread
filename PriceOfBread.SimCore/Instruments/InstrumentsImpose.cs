namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（持ち物は計器の ConditionalWeakTable）。〔履歴 InstrumentsImpose.cs#1〕
public static partial class Instruments
{
    internal const string ImpHeaderText = "quarter\tmonth\tyear\twinner\tloser\twinnerPolityCode\tloserPolityBeforeCode\tloserCrownBeforeCode\tloserCrownAfterCode\timposedPolityCode\tabsScoreIndex\tideologyGapIndex\tlegitimacyBeforeShare\tlegitimacyAfterShare\tcrushingFlag\torderInMonthIndex";

    /// <summary>矯正の前の敗者の断面（呼び口の局所に持たせる）。</summary>
    internal sealed class ImpPre { public Polity Polity; public CrownForm Crown; public double Legitimacy; }

    sealed class ImpState
    {
        public readonly System.Collections.Generic.List<string> Rows = new();
        public readonly System.Collections.Generic.Dictionary<(int, string), int> Seq = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, ImpState> ImpStates = new();

    /// <summary>計器自身の政体のイデオロギー位置（Diplomacy.Ideology の写しではなく別の書き方＝政体の列挙の順に依らない）。</summary>
    static double ImpPos(Polity p) => p == Polity.Absolutist || p == Polity.Shogunate ? 0.0
        : p == Polity.Despotic ? 0.15 : p == Polity.MilitaryDictatorship ? 0.2 : p == Polity.BourgeoisDictatorship ? 0.35
        : p == Polity.ConstitutionalMonarchy ? 0.45 : p == Polity.Republic ? 0.60 : p == Polity.ProletarianDictatorship ? 1.0 : p == Polity.Anarchy ? 0.50 : 0.3;

    /// <summary>矯正の前（ApplyRegime の前）：敗者の断面を控える。</summary>
    internal static ImpPre ImpBefore(World dw) => new() { Polity = dw.Polity, Crown = dw.Crown, Legitimacy = dw.Legitimacy };

    /// <summary>On の判定を中で持つ版（ResolvePeace は既に On を何度も読む関数＝呼び側で読み足さない）。切れていれば null。</summary>
    internal static ImpPre? ImpBeforeIfOn(World dw) => On ? ImpBefore(dw) : null;

    /// <summary>矯正の後（ログ〈政体を矯正〉の直後）：1 行を貯め、敗者の王冠の登録簿の最後の書き手を 2（矯正講和）にする。</summary>
    internal static void ImpAfter(Diplomacy d, ImpPre pre, Power winner, Power loser, World ww, World dw, double absScore, bool crushing)
    {
        CrownSource(dw, 2);
        var st = ImpStates.GetValue(d, _ => new ImpState());
        var cp = CpOf(d);
        var key = (cp.Month, NameOf(loser));
        st.Seq.TryGetValue(key, out int n); n++; st.Seq[key] = n;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        st.Rows.Add(string.Join('\t',
            I(cp.Month / 3), I(cp.Month), I(cp.Year), NameOf(winner), NameOf(loser), I((int)ww.Polity), I((int)pre.Polity), I((int)pre.Crown), I((int)dw.Crown), I((int)dw.Polity),
            F(absScore), F(System.Math.Abs(ImpPos(ww.Polity) - ImpPos(pre.Polity))), F(pre.Legitimacy), F(dw.Legitimacy), crushing ? "1" : "0", I(n)));
    }

    static void ImpTable(Sink sk, Diplomacy d)
    {
        if (!ImpStates.TryGetValue(d, out _)) return;
        using var wr = Open("imp", sk.Seed);
        wr.WriteLine(ImpHeaderText);
        foreach (var line in ImpRowsPending(d)) wr.WriteLine(line);
    }

    /// <summary>行の文字列の一覧（表に書く行そのもの・試験が読む）。</summary>
    internal static System.Collections.Generic.List<string> ImpRowsPending(Diplomacy d)
        => ImpStates.TryGetValue(d, out var st) ? new(st.Rows) : new();
}
