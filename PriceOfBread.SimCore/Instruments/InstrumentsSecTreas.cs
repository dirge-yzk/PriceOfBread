namespace PriceOfBread.SimCore;

// (1318) c137ak 計器 sectreas_s：分離の建国（MultiNationSim.SpawnNation）の国庫の分与の旧と新の規則を、同じ盤面で並べる（1 回の分離＝1 行）。
// 受け口は SpawnNation の分与の式の直後（州を移す前・World.Transfer の前）の 1 か所だけ。計器は州の POP の大きさと貯蓄を自分で回す
// （エンジンの SumPopulation・SumSavings・TotalPopulation・TotalMoney を呼ばない）。旧の規則＝max(0,国庫)×移る州の人口÷親の人口・
// 新の規則＝max(0,国庫)×移る州の貯蓄の和÷親の貯蓄の和（どちらかの和が 0 以下なら 0）・実際＝エンジンが使った値（呼び手が渡す）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable＝セーブに載らない）。Instruments.On かつ表が選ばれた時だけ動く。
// 行は四半期の末にまとめて書く（quarter＝書いた四半期の末・分離はその四半期の中）。見出しは最初の四半期の末に必ず書く（分離 0 の種は見出しだけ）。
// 列：parent＝親の国・firstRegion＝移る州の最初（新しい国の首都）・regions＝移る州の数・capitalRisingFlag＝親の首都が移る州に入る・byMoneyFlag＝旗 SecessionTreasuryByMoney＞0・
//   movedPopK／parentPopK＝移る州と親の全ての州の POP の大きさの和（千人）・movedMoney／parentMoney＝同じ州の貯蓄の和・movedNegSavings＝移る州の負の貯蓄の和（0 以下）・
//   parentTreasury＝分与の前の親の国庫・oldRuleT／newRuleT＝旧と新の規則の分与・actualT＝エンジンが使った分与・shareOverOneFlag＝新の規則の比が 1 を超える（movedMoney＞parentMoney）。
public static partial class Instruments
{
    internal const string SecTreasHeaderText = "quarter\tyear\tparent\tfirstRegion\tregions\tcapitalRisingFlag\tbyMoneyFlag\tmovedPopK\tparentPopK\tmovedMoney\tmovedNegSavings"
        + "\tparentMoney\tparentTreasury\toldRuleT\tnewRuleT\tactualT\tshareOverOneFlag";

    sealed class StSink { public System.IO.StreamWriter? W; public readonly List<string> Pending = new(); public readonly List<string> Peek = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, StSink> StSinks = new();

    /// <summary>受け口（SpawnNation の分与の式の直後・州を移す前）：旧と新の規則の分与を計器が自分で組み、実際の値と並べて貯める。</summary>
    internal static void SecTreasIfOn(MultiNationSim sim, World pw, IReadOnlyList<Region> regs, double actualT)
    {
        if (!On || !Wants("sectreas_s")) return;
        double movedPop = 0, movedMoney = 0, movedNeg = 0, parentPop = 0, parentMoney = 0;
        foreach (var r in regs)
            foreach (var p in r.Pops) { movedPop += p.Size; double sv = p.Savings.Value; movedMoney += sv; if (sv < 0) movedNeg += sv; }
        foreach (var r in pw.Regions)
            foreach (var p in r.Pops) { parentPop += p.Size; parentMoney += p.Savings.Value; }
        double tre = pw.Treasury.Value, t0 = tre > 0 ? tre : 0;
        double oldT = t0 * (parentPop > 1e-9 ? movedPop / parentPop : 0);
        double newT = t0 * (movedMoney > 0 && parentMoney > 0 ? movedMoney / parentMoney : 0);
        bool capRising = pw.Regions.Count > 0 && regs.Contains(pw.Regions[0]);
        static string B(bool b) => b ? "1" : "0";
        string row = string.Join('\t', TlF(pw.Year), NameOf(pw), regs.Count > 0 ? regs[0].Name : "-", regs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            B(capRising), B(pw.Coef.SecessionTreasuryByMoney > 0), TlF(movedPop), TlF(parentPop), TlF(movedMoney), TlF(movedNeg),
            TlF(parentMoney), TlF(tre), TlF(oldT), TlF(newT), TlF(actualT), B(movedMoney > parentMoney && parentMoney > 0));
        StSinks.GetValue(sim, _ => new StSink()).Pending.Add(row);
    }

    /// <summary>試験の読み口：直前の四半期の末に書いた行（quarter の列から）。</summary>
    internal static IReadOnlyList<string> SecTreasPeek(MultiNationSim sim) => StSinks.GetValue(sim, _ => new StSink()).Peek;

    /// <summary>四半期の末：その四半期に貯めた分離の行を書く（無ければ何もしない・見出しは最初の行の前に 1 回）。</summary>
    static void SecTreasTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("sectreas_s")) return;
        var sk = StSinks.GetValue(sim, _ => new StSink());
        sk.Peek.Clear();
        if (sk.W is null) { sk.W = Open("sectreas", seed); sk.W.WriteLine(SecTreasHeaderText); }   // 分離が 1 度も無い種でも見出しの行だけの表を残す（読み手が〈表が無い〉と〈0 件〉を分ける）
        if (sk.Pending.Count == 0) return;
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\t";
        foreach (var r in sk.Pending) { string line = qs + r; sk.W.WriteLine(line); sk.Peek.Add(line); }
        sk.Pending.Clear();
    }

    static void SecTreasClose(MultiNationSim sim) { if (StSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
