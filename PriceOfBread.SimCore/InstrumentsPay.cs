namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (486) 売上の払い（PaySales）の保存則の穴の表 pay_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLw.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない。INS=1 のときだけ動く（数えるのも INS=1 のときだけ）。
// 目的＝事前登録 measurements/pf486_20261021_prereg.txt の H0（直しの恒等＝成績表が違う種を fixHit で説明する）・H2（直した後も消える額が 0）。
// 数える枝＝InternationalMarket.PaySales の `to.Count == 0` かつ `r != null` かつ 商人 POP の添字が立ち人数が 0（階級が絶えた）。払い手の種類 × 鋳造かで分けて**累計**する（読み手は最後の行を読む）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。累計は国ごとの World の作業領域（NonSerialized・セーブしない）。
// 列（N＝回数・Amt＝PaySales の amount の和＝払うはずの額）：
//   fixHit＝清算所が払い手・鋳造でない・商人 0 人（直しが効いた回数と額。直す前の木では世界から消えた額）／
//   mintEmpty＝清算所・鋳造・商人 0 人（従来どおり＝受け手不在で買わない＝穴ではない）／
//   popEmpty＝払い手が POP・商人 0 人（代金を払わずに財を得る財の側の別の穴＝触らない）／treasEmpty＝払い手が国庫・商人 0 人（同上）／
//   clearUnmoved＝清算所・鋳造でない・PaySales の返りが払うはずの額に満たなかった回数と不足の額（直した後も消える額＝0 であるべき）／mintUnmoved＝清算所・鋳造・同じ不足（穴ではない）。
// ⚠限界（計器が自己申告する）：①額は amount（払うはずの額）で、買い手が清算所へ払い込んだ額とは世界の台帳の Outside の記帳で結ばれる（保存則の本物の判定は leakhunt）／②州の POP が全員人数 0 だと全 POP の頭数も 0 で動かせない（clearUnmoved に出る）。
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
