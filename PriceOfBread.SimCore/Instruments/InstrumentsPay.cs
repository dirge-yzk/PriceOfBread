namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない。〔履歴 InstrumentsPay.cs#1〕
public static partial class Instruments
{
    internal const string PayHeaderText = "quarter\tyear\tnation\tfixHitN\tfixHitAmt\tmintEmptyN\tmintEmptyAmt\tpopEmptyN\tpopEmptyAmt\ttreasEmptyN\ttreasEmptyAmt\tclearUnmovedN\tclearUnmovedAmt\tmintUnmovedN\tmintUnmovedAmt";

    static void PayFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var a = w.DiagPay ?? new double[PayDiag.Len];
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Pay ??= Open("pay", seed);
        if (!sk.PayHeader) { sk.Pay.WriteLine(PayHeaderText); sk.PayHeader = true; }
        var cols = new List<string> { I(q), I(w.Year), NameOf(w) };
        for (int k = 0; k < PayDiag.Len; k += 2) { cols.Add(F(a[k])); cols.Add(F(a[k + 1])); }
        sk.Pay.WriteLine(string.Join('\t', cols));
    }
}

/// <summary>PaySales の診断の数え（INS=1 のときだけ呼ばれる・読むだけ）。置き場は国ごとの World の NonSerialized の配列＝共有の変数を取り合わない（計算量の台帳と同じ作法）。</summary>
internal static class PayDiag
{
    internal const int FixHit = 0, MintEmpty = 2, PopEmpty = 4, TreasEmpty = 6, ClearUnmoved = 8, MintUnmoved = 10, Len = 12;   // (N, Amt) の組

    static void Add(World w, int k, double amount)
    {
        var a = w.DiagPay ??= new double[Len];
        a[k] += 1; a[k + 1] += amount;
    }

    /// <summary>商人 POP の人数が 0 の枝に入った。</summary>
    internal static void CountEmptyMerchant(World w, bool clearing, bool popPayer, bool minted, double amount)
        => Add(w, clearing ? (minted ? MintEmpty : FixHit) : popPayer ? PopEmpty : TreasEmpty, amount);

    /// <summary>一度でも居た国の World の累計の和を 1 行の文字で返す（leakhunt が INS=1 のときだけ呼ぶ・読むだけ。leakhunt は計器の表を出さないので、直しの枝に入ったかを H1 が読む口）。</summary>
    internal static string Summary(IEnumerable<World> worlds)
    {
        var t = new double[Len];
        foreach (var w in worlds) if (w.DiagPay is { } a) for (int k = 0; k < Len; k++) t[k] += a[k];
        string F(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        return $"pay_s 累計（一度でも居た国の和）：fixHit {F(t[FixHit])}回・額{F(t[FixHit + 1])}／mintEmpty {F(t[MintEmpty])}回・額{F(t[MintEmpty + 1])}／popEmpty {F(t[PopEmpty])}回・額{F(t[PopEmpty + 1])}／treasEmpty {F(t[TreasEmpty])}回・額{F(t[TreasEmpty + 1])}／clearUnmoved {F(t[ClearUnmoved])}回・額{F(t[ClearUnmoved + 1])}／mintUnmoved {F(t[MintUnmoved])}回・額{F(t[MintUnmoved + 1])}";
    }

    /// <summary>清算所が払うはずの額に満たなかった（不足 <paramref name="shortfall"/>）。</summary>
    internal static void CountUnmoved(World w, bool minted, double shortfall) => Add(w, minted ? MintUnmoved : ClearUnmoved, shortfall);
}
