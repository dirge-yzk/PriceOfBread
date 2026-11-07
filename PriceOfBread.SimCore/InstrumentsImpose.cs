namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (928) 矯正講和の計器 imp_s（読むだけ・恒等・事前登録 measurements/m928_20261107_prereg_im.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpeace.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（持ち物は計器の ConditionalWeakTable）。
// 目的＝Diplomacy.ResolvePeace の矯正講和（敗者の政体を勝者の政体へ矯正）が起きた時の前後の王冠・政体・正統性を 1 件 1 行で残す（read924 §5・下書き「英の専制化の経路」の型 A の材料）。
// 取り方＝呼び口は 2 つ。ImpBefore（ApplyRegime の前・敗者の政体と王冠と正統性を局所に控える）・ImpAfter（ログ〈政体を矯正〉の直後・前後を並べて 1 行）。エンジンの関数を呼ばない
//   （政体のイデオロギー位置の表は Diplomacy.Ideology と別に計器が書く＝期待の側が実装の写しにならない）。
// 行＝矯正講和 1 件。列（単位は接尾辞：Share＝0〜1・Index＝無次元・Flag＝0／1・Code＝整数の区分）：
//   quarter（＝講和の月÷3）／month／year／winner／loser／winnerPolityCode／loserPolityBeforeCode／loserCrownBeforeCode／loserCrownAfterCode／imposedPolityCode（矯正の後の敗者の政体）／
//   absScoreIndex（未クランプの |戦勝点|）／ideologyGapIndex（計器が自分の表で数えた政体の隔たり）／legitimacyBeforeShare／legitimacyAfterShare／crushingFlag／
//   orderInMonthIndex（同じ月に同じ敗者が矯正された何本目か・1 始まり）。
// ⚠限界（計器が自己申告する）：①月は Diplomacy.Step の入口の月（CpMonth の値）＝四半期の AI・調停の歩の中の講和も同じ月で数える。②crushingFlag は矯正の門の中でしか書かないので恒に 1
//   （門の定義に含まれる列＝列として残すのは read924 §5 の仕様どおり）。③orderInMonthIndex は矯正した件だけを数える（矯正に至らなかった同じ月の講和は数えない）。
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
