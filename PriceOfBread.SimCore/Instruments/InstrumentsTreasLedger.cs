namespace PriceOfBread.SimCore;

// (1295) c134tk 計器 treasledger_s：全ての国×四半期の末の、国庫の科目ごとの出入りと民間の貨幣。
// 既存の月の表（Instruments.cs の FiscalRow・選んだ国だけ）と nation_s のバイトを保つため別の表にした（全ての国を四半期で読む）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（前の四半期の値の置き場は計器の ConditionalWeakTable＝セーブに載らない）・エンジンの関数を呼ばない
// （読むのは World.TreasuryLedger の累積と国庫と TotalMoney の値だけ。差分と残差は計器の算術）。Instruments.On かつ表が選ばれた時だけ計算する。
// 行の形（長い形）：quarter・year・nation・item・value。item＝科目の名前（その四半期の差分が 0 の科目は書かない）か、次の「_」始まりの印（国×四半期に必ず書く）：
//   _treasuryEnd＝四半期末の国庫・_treasuryDelta＝前の四半期末からの国庫の変化・_ledgerSum＝科目の差分の和・_residual＝_treasuryDelta−_ledgerSum（台帳を通らない国庫の動き）・
//   _privateMoney＝四半期末の TotalMoney（州の貯蓄の和）・_rebased＝その四半期に助走明けのリベースが有ったら 1（無ければ書かない）・_rebaseJump＝リベースの前後の国庫の差（印と同時）。
// 最初の観測の四半期（途中で生まれた国・助走の無い盤面の最初の国など〔助走明けのリベースを経ていない国〕）は前の値が無い＝科目の差分は 0 からの累積・_treasuryDelta・_residual は nan・_bornTreasury＝_treasuryEnd−_ledgerSum（受け取った初期の国庫）を足す。リベースの四半期は、リベース以後の分だけを差分にする（台帳が捨てられるため）。
public static partial class Instruments
{
    internal const string TreasLedgerHeaderText = "quarter\tyear\tnation\titem\tvalue";

    sealed class TlWorld { public bool Have; public double PrevTreasury; public readonly double[] PrevLedger = new double[NCat]; public bool Rebased; public double Jump; }
    sealed class TlSink { public System.IO.StreamWriter? W; public readonly List<string> Peek = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, TlWorld> TlWorlds = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, TlSink> TlSinks = new();

    /// <summary>差分と残差の算術（月の表 FiscalRow と treasledger_s の共通）：科目ごとの差分を d へ・和は k の昇順・国庫の変化と残差（＝国庫の変化−和）を返す。</summary>
    internal static (double dT, double dSum, double residual) LedgerStep(double[] led, double[] prev, double tre, double prevTre, Span<double> d)
    {
        double dSum = 0;
        for (int k = 0; k < NCat; k++) { double x = led[k] - prev[k]; d[k] = x; dSum += x; }
        double dT = tre - prevTre;
        return (dT, dSum, dT - dSum);
    }

    static string TlF(double v) => double.IsNaN(v) ? "nan" : double.IsInfinity(v) ? (v > 0 ? "inf" : "-inf") : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>試験の読み口：その計器がこれまでに書いた行（タブ区切り・四半期の列から）。</summary>
    internal static IReadOnlyList<string> TreasLedgerPeek(MultiNationSim sim) => TlSinks.GetValue(sim, _ => new TlSink()).Peek;

    /// <summary>受け口（World.RebaseTreasury の中・Instruments.On の時だけ）：台帳が捨てられたので前の値を 0 と新しい国庫へ置き直し、印を立てる。</summary>
    internal static void TreasLedgerRebased(World w, double before, double to)
    {
        if (!Wants("treasledger_s")) return;
        var s = TlWorlds.GetValue(w, _ => new TlWorld());
        System.Array.Clear(s.PrevLedger); s.PrevTreasury = to; s.Have = true; s.Rebased = true; s.Jump = to - before;
    }

    static void TreasLedgerClose(MultiNationSim sim) { if (TlSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }

    /// <summary>四半期の末：全ての国の行を書く。</summary>
    static void TreasLedgerTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("treasledger_s")) return;
        var sk = TlSinks.GetValue(sim, _ => new TlSink());
        sk.Peek.Clear();   // 試験の読み口は最後の四半期の行だけ（ArrearsPeek と同じ形）
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Span<double> dk = stackalloc double[NCat];   // (1316) ループの外へ（LedgerStep が全ての要素を書く＝国ごとに空にする必要は無い）
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var s = TlWorlds.GetValue(w, _ => new TlWorld());
            string head = qs + "\t" + TlF(w.Year) + "\t" + NameOf(w) + "\t";
            var led = w.TreasuryLedger;
            var rows = new List<string>();
            double tre = w.Treasury.Value;
            var (dT0, dSum, res0) = LedgerStep(led, s.PrevLedger, tre, s.PrevTreasury, dk);
            for (int k = 0; k < NCat; k++) if (dk[k] != 0) rows.Add(head + (TreasuryCat)k + "\t" + TlF(dk[k]));
            double dT = s.Have ? dT0 : double.NaN;
            rows.Add(head + "_treasuryEnd\t" + TlF(tre));
            rows.Add(head + "_treasuryDelta\t" + TlF(dT));
            rows.Add(head + "_ledgerSum\t" + TlF(dSum));
            rows.Add(head + "_residual\t" + TlF(s.Have ? res0 : double.NaN));
            if (!s.Have) rows.Add(head + "_bornTreasury\t" + TlF(tre - dSum));   // 助走明けのリベースを経ていない国（途中で生まれた国・助走の無い盤面の最初の国）が受け取った初期の国庫（台帳の外 SetTreasuryRaw で入った分）
            rows.Add(head + "_privateMoney\t" + TlF(w.TotalMoney.Value));
            if (s.Rebased) { rows.Add(head + "_rebased\t1"); rows.Add(head + "_rebaseJump\t" + TlF(s.Jump)); }
            if (sk.W is null) { sk.W = Open("treasledger", seed); sk.W.WriteLine(TreasLedgerHeaderText); }
            foreach (var r in rows) { sk.W.WriteLine(r); sk.Peek.Add(r); }
            s.PrevTreasury = tre; System.Array.Copy(led, s.PrevLedger, NCat); s.Have = true; s.Rebased = false; s.Jump = 0;
        }
    }
}
