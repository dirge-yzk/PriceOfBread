using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (815) 払えない利札の落としの表 cpwd_s・cpworld_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsDivhost.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（呼び出しの中の緩衝は ThreadStatic の計器の欄）。
// 目的＝旗 CapitalCouponUnpaidWriteDown（払えなかった利札の分だけ外資の負債と持ち主の対外資産を落とす）の門を、エンジンの局所の値に頼らず読む。旗 0 でも旗 1 でも同じ列を書く。
// ⚠期待の側（claimIndex・unpaidIndex・woExpectedIndex）は計器が状態から式で書く（外資の負債×CapitalDividendRatePerQuarter・エンジンの div を受け取らない）。
//   実際の側：paidIndex＝TakeGold の返り値（移した直後の値）・woActualIndex＝計器が読んだ外資の負債の差（台帳）・ownerDropGold＝計器が読んだ対外資産の金建ての和の差（台帳）。
// 列（cpwd_s・請求か落としのある国の行だけ）：quarter／call（CapitalMarket.Step の呼び出しの通し番号）／year／nation／fxRate／fciBeforeFlight（逃避の前）／fciAtCoupon（逃避の後＝利札の時点）／
//   claimIndex／paidIndex／unpaidIndex（claim−paid が 1e-9 を超えるときその値・他は 0）／woExpectedIndex（min(unpaid, fciAtCoupon)）／fciAfterWriteDown／woActualIndex（fciAtCoupon−fciAfterWriteDown）。額は各国の通貨。
// 列（cpworld_s・呼び出しごとに 1 行・金建て）：quarter／call／year／ownerBeforeGold・ownerAfterGold（還流の段の前と後の Σ CapitalAbroad×為替）／ownerDropGold／hostFlightDropGold（Σ(fciBeforeFlight−fciAtCoupon)×為替）／
//   hostWoDropGold（Σ woActual×為替）／unpaidGold／claimGold／woExpectedGold／woEvents（woExpected＞1e-9 の国の数）／skip（host の落としが正なのに ownerDrop≦1e-12）／
//   clipNations（還流の段で対外資産が正から 0 以下へ張り付いた国の数）／fciSumGold・abroadSumGold（呼び出しの終わり）。
// ⚠限界（計器が自己申告する）：①claimIndex は旗 CapitalDividendFromHostRent＝0 のときだけエンジンの請求と同じ式（正なら別の式＝この表の期待の側は使えない）
//   ②為替は計器が呼び出しの時点の World.FxRate を読む（エンジンの e[] と同じ時点の値）③ThreadStatic の緩衝＝1 試合は 1 本のスレッドで回る前提④Diplomacy の参照が合わない四半期は捨てる。
// ★(828) c66dw：表 cpwdft_s（国の行・逃避か利札の落としか台帳の差のある国だけ）＝消えた請求権の累計（エンジンの flightTotal＝持ち主の対外資産を減らす額）を、計器が台帳から書いた累計と同じ点で突き合わせる。
//   期待（台帳）の側は計器が読んだ外資の負債の差×為替を、エンジンと同じ順で前の点のエンジンの累計に足す（ledgerAfterFlight＝ftBefore＋(fci0−fci1)×e・ledgerAfterCoupon＝ftAfterFlight＋(fci1−fci2)×e）。
//   実際の側はエンジンの累計（呼び出しの引数＝足した直後の値）。oneSided*＝両者がビットで違う（片側の書き込み）。旧の規則の額は計器が状態から式で書く：flightOldIndex＝min(fci0, fci0×CapitalFlightRatePerQuarter×(1−CreditRating))（>1e-9 のときだけ・他は 0）・
//   woOldIndex＝min(fci1×CapitalDividendRatePerQuarter−paid, fci1)（旗 CapitalCouponUnpaidWriteDown が正で >1e-9 のときだけ）。absorbed*＝旧の規則の額が正なのに台帳の差が 0（double に吸収）。
//   列：quarter／call／year／nation／fxRate／credit／fciBeforeFlight／fciAtCoupon／fciAfterWriteDown／flightOldIndex／flightLedgerIndex／woOldIndex／woLedgerIndex／ftBefore／ftAfterFlight／ftAfterCoupon／ledgerAfterFlight／ledgerAfterCoupon／oneSidedFlight／oneSidedCoupon／absorbedFlight／absorbedCoupon。
//   ⚠限界：①woOldIndex は旗 CapitalDividendFromHostRent＝0 のときだけエンジンの請求と同じ式 ②credit は World.CreditRating（同じ Step の信用の写し・CapitalMarket.cs の cred[i] と同じ値）③為替は World.FxRate（エンジンの e[] と同じ時点の値）。
public static partial class Instruments
{
    internal const string CpwdftHeaderText = "quarter\tcall\tyear\tnation\tfxRate\tcredit\tfciBeforeFlight\tfciAtCoupon\tfciAfterWriteDown\tflightOldIndex\tflightLedgerIndex\twoOldIndex\twoLedgerIndex\tftBefore\tftAfterFlight\tftAfterCoupon\tledgerAfterFlight\tledgerAfterCoupon\toneSidedFlight\toneSidedCoupon\tabsorbedFlight\tabsorbedCoupon";
    [System.ThreadStatic] static System.Text.StringBuilder? _cpwdftBuf;
    [System.ThreadStatic] static double[]? _cpwdFt0, _cpwdFt1, _cpwdCr;
    internal const string CpwdHeaderText = "quarter\tcall\tyear\tnation\tfxRate\tfciBeforeFlight\tfciAtCoupon\tclaimIndex\tpaidIndex\tunpaidIndex\twoExpectedIndex\tfciAfterWriteDown\twoActualIndex";
    internal const string CpworldHeaderText = "quarter\tcall\tyear\townerBeforeGold\townerAfterGold\townerDropGold\thostFlightDropGold\thostWoDropGold\tunpaidGold\tclaimGold\twoExpectedGold\twoEvents\tskip\tclipNations\tfciSumGold\tabroadSumGold";

    [System.ThreadStatic] static System.Text.StringBuilder? _cpwdBuf, _cpworldBuf;
    [System.ThreadStatic] static object? _cpwdKey;
    [System.ThreadStatic] static int _cpwdCall;
    [System.ThreadStatic] static double[]? _cpwdF0, _cpwdF1, _cpwdA0;
    [System.ThreadStatic] static double _cpwdOwner0, _cpwdOwner1, _cpwdFlight, _cpwdWo, _cpwdUnpaid, _cpwdClaim, _cpwdWoExp;
    [System.ThreadStatic] static int _cpwdEvents, _cpwdClip;
    sealed class CpwdSink { public System.IO.StreamWriter? W, WW, WF; public bool Header, HeaderW, HeaderF; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CpwdSink> CpwdSinks = new();

    static string CpwdR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    static string CpwdI(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>新しい呼び出しの頭（最初の国の逃避の前）か試合が替わったとき、呼び出しの欄を空にする。</summary>
    static void CpwdBegin(IReadOnlyList<Simulation> nations)
    {
        var key = nations[0].World.Diplomacy;
        if (!ReferenceEquals(key, _cpwdKey)) { _cpwdBuf = new(); _cpworldBuf = new(); _cpwdftBuf = new(); _cpwdKey = key; _cpwdCall = 0; }
        _cpwdCall++;
        _cpwdBuf ??= new(); _cpworldBuf ??= new(); _cpwdftBuf ??= new();
        int n = nations.Count;
        if (_cpwdF0 is null || _cpwdF0.Length != n) { _cpwdF0 = new double[n]; _cpwdF1 = new double[n]; _cpwdA0 = new double[n]; }
        if (_cpwdFt0 is null || _cpwdFt0.Length != n) { _cpwdFt0 = new double[n]; _cpwdFt1 = new double[n]; _cpwdCr = new double[n]; }
        _cpwdOwner0 = 0; _cpwdOwner1 = 0; _cpwdFlight = 0; _cpwdWo = 0; _cpwdUnpaid = 0; _cpwdClaim = 0; _cpwdWoExp = 0; _cpwdEvents = 0; _cpwdClip = 0;
    }

    /// <summary>国 <paramref name="i"/> の外資の負債を読む：<paramref name="slot"/> 0＝逃避の前・1＝逃避の後（利札の時点）。<paramref name="flightTotal"/>＝その点のエンジンの消えた請求権の累計（(828)・実際の側）。</summary>
    internal static void CpwdMark(IReadOnlyList<Simulation> nations, int i, int slot, double flightTotal)
    {
        if (!On) return;
        if (slot == 0 && i == 0) CpwdBegin(nations);
        if (_cpwdF0 is null || _cpwdF1 is null || i >= _cpwdF0.Length) return;
        double f = nations[i].World.ForeignCapitalIn.Value;
        if (slot == 0) _cpwdF0[i] = f; else _cpwdF1[i] = f;
        if (_cpwdFt0 is null || _cpwdFt1 is null || _cpwdCr is null || i >= _cpwdFt0.Length) return;
        if (slot == 0) { _cpwdFt0[i] = flightTotal; _cpwdCr[i] = nations[i].World.CreditRating; } else _cpwdFt1[i] = flightTotal;
    }

    /// <summary>国 <paramref name="i"/> の利札の行（落としの後）：<paramref name="paid"/>＝TakeGold の返り値。請求・未払い・落とすはずの額は計器が状態から書く。</summary>
    internal static void CpwdRow(IReadOnlyList<Simulation> nations, int i, double paid, double flightTotal)
    {
        if (!On || _cpwdF0 is null || _cpwdF1 is null || _cpwdBuf is null || i >= _cpwdF0.Length) return;
        var w = nations[i].World;
        double e = w.FxRate, f0 = _cpwdF0[i], f1 = _cpwdF1[i], f2 = w.ForeignCapitalIn.Value;
        CpwdftRow(w, i, e, f0, f1, f2, paid, flightTotal);
        double claim = f1 * w.Coef.CapitalDividendRatePerQuarter.Value;   // エンジンの (ForeignCapitalIn * rate).Value と同じ 1 回の掛け算
        double un = claim - paid; if (!(un > 1e-9)) un = 0;
        double woExp = System.Math.Min(un, f1); if (!(woExp > 1e-9)) woExp = 0;
        double woAct = f1 - f2;
        _cpwdFlight += (f0 - f1) * e; _cpwdWo += woAct * e; _cpwdUnpaid += un * e; _cpwdClaim += System.Math.Max(0, claim) * e; _cpwdWoExp += woExp * e;
        if (woExp > 0) _cpwdEvents++;
        if (!(claim > 1e-9) && woAct == 0) return;   // 請求も落としも無い国は行を書かない
        _cpwdBuf.Append(CpwdI(_cpwdCall)).Append('\t').Append(CpwdI(w.Year)).Append('\t').Append(NameOf(w)).Append('\t').Append(CpwdR(e))
            .Append('\t').Append(CpwdR(f0)).Append('\t').Append(CpwdR(f1)).Append('\t').Append(CpwdR(claim)).Append('\t').Append(CpwdR(paid))
            .Append('\t').Append(CpwdR(un)).Append('\t').Append(CpwdR(woExp)).Append('\t').Append(CpwdR(f2)).Append('\t').Append(CpwdR(woAct)).Append('\n');
    }

    /// <summary>(828) cpwdft_s の 1 行：消えた請求権の累計（エンジン）と台帳の差の累計（計器）を逃避の後と利札の後の 2 点で比べる。</summary>
    static void CpwdftRow(World w, int i, double e, double f0, double f1, double f2, double paid, double ftAfterCoupon)
    {
        if (_cpwdftBuf is null || _cpwdFt0 is null || _cpwdFt1 is null || _cpwdCr is null || i >= _cpwdFt0.Length) return;
        var c = w.Coef;
        double cr = _cpwdCr[i], ft0 = _cpwdFt0[i], ft1 = _cpwdFt1[i];
        double flOld = System.Math.Min(f0, f0 * c.CapitalFlightRatePerQuarter.Value * (1.0 - cr)); if (!(flOld > 1e-9)) flOld = 0;
        double woOld = 0;
        if (c.CapitalCouponUnpaidWriteDown > 0) { woOld = System.Math.Min(f1 * c.CapitalDividendRatePerQuarter.Value - paid, f1); if (!(woOld > 1e-9)) woOld = 0; }
        double flLed = f0 - f1, woLed = f1 - f2;
        double ledF = ft0 + flLed * e, ledC = ft1 + woLed * e;
        bool osF = ledF != ft1, osC = ledC != ftAfterCoupon;
        if (!(flOld > 0) && !(woOld > 0) && flLed == 0 && woLed == 0 && !osF && !osC) return;   // 逃避も落としも差も無い国は行を書かない
        int abF = flOld > 0 && flLed == 0 ? 1 : 0, abC = woOld > 0 && woLed == 0 ? 1 : 0;
        _cpwdftBuf.Append(CpwdI(_cpwdCall)).Append('\t').Append(CpwdI(w.Year)).Append('\t').Append(NameOf(w)).Append('\t').Append(CpwdR(e)).Append('\t').Append(CpwdR(cr))
            .Append('\t').Append(CpwdR(f0)).Append('\t').Append(CpwdR(f1)).Append('\t').Append(CpwdR(f2))
            .Append('\t').Append(CpwdR(flOld)).Append('\t').Append(CpwdR(flLed)).Append('\t').Append(CpwdR(woOld)).Append('\t').Append(CpwdR(woLed))
            .Append('\t').Append(CpwdR(ft0)).Append('\t').Append(CpwdR(ft1)).Append('\t').Append(CpwdR(ftAfterCoupon)).Append('\t').Append(CpwdR(ledF)).Append('\t').Append(CpwdR(ledC))
            .Append('\t').Append(osF ? '1' : '0').Append('\t').Append(osC ? '1' : '0').Append('\t').Append(CpwdI(abF)).Append('\t').Append(CpwdI(abC)).Append('\n');
    }

    /// <summary>還流の段の前（<paramref name="phase"/> 0）と後（1）の対外資産の金建ての和。</summary>
    internal static void CpwdOwner(IReadOnlyList<Simulation> nations, int phase)
    {
        if (!On || _cpwdA0 is null) return;
        double s = 0;
        for (int i = 0; i < nations.Count && i < _cpwdA0.Length; i++)
        {
            var w = nations[i].World; double a = w.CapitalAbroad.Value;
            s += a * w.FxRate;
            if (phase == 0) _cpwdA0[i] = a;
            else if (_cpwdA0[i] > 0 && !(a > 0)) _cpwdClip++;
        }
        if (phase == 0) _cpwdOwner0 = s; else _cpwdOwner1 = s;
    }

    /// <summary>呼び出しの終わり：世界の行を溜める。</summary>
    internal static void CpwdEnd(IReadOnlyList<Simulation> nations)
    {
        if (!On || _cpworldBuf is null) return;
        double fs = 0, ab = 0;
        foreach (var n in nations) { var w = n.World; fs += w.ForeignCapitalIn.Value * w.FxRate; ab += w.CapitalAbroad.Value * w.FxRate; }
        double drop = _cpwdOwner0 - _cpwdOwner1;
        int skip = (_cpwdFlight + _cpwdWo) > 1e-9 && !(drop > 1e-12) ? 1 : 0;
        _cpworldBuf.Append(CpwdI(_cpwdCall)).Append('\t').Append(CpwdI(nations[0].World.Year))
            .Append('\t').Append(CpwdR(_cpwdOwner0)).Append('\t').Append(CpwdR(_cpwdOwner1)).Append('\t').Append(CpwdR(drop))
            .Append('\t').Append(CpwdR(_cpwdFlight)).Append('\t').Append(CpwdR(_cpwdWo)).Append('\t').Append(CpwdR(_cpwdUnpaid)).Append('\t').Append(CpwdR(_cpwdClaim)).Append('\t').Append(CpwdR(_cpwdWoExp))
            .Append('\t').Append(CpwdI(_cpwdEvents)).Append('\t').Append(CpwdI(skip)).Append('\t').Append(CpwdI(_cpwdClip)).Append('\t').Append(CpwdR(fs)).Append('\t').Append(CpwdR(ab)).Append('\n');
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を追記する。</summary>
    static void CpwdTable(MultiNationSim sim, ulong seed, int q)
    {
        if (_cpwdBuf is null && _cpworldBuf is null && _cpwdftBuf is null) return;
        if (!ReferenceEquals(sim.Nations.Count > 0 ? sim.Nations[0].World.Diplomacy : null, _cpwdKey)) { _cpwdBuf?.Clear(); _cpworldBuf?.Clear(); _cpwdftBuf?.Clear(); return; }
        var sk = CpwdSinks.GetValue(sim, _ => new CpwdSink());
        var qs = CpwdI(q);
        if (_cpwdBuf is { Length: > 0 })
        {
            sk.W ??= Open("cpwd", seed);
            if (!sk.Header) { sk.W.WriteLine(CpwdHeaderText); sk.Header = true; }
            foreach (var line in _cpwdBuf.ToString().Split('\n', System.StringSplitOptions.RemoveEmptyEntries)) sk.W.WriteLine(qs + "\t" + line);
            _cpwdBuf.Clear();
        }
        if (_cpworldBuf is { Length: > 0 })
        {
            sk.WW ??= Open("cpworld", seed);
            if (!sk.HeaderW) { sk.WW.WriteLine(CpworldHeaderText); sk.HeaderW = true; }
            foreach (var line in _cpworldBuf.ToString().Split('\n', System.StringSplitOptions.RemoveEmptyEntries)) sk.WW.WriteLine(qs + "\t" + line);
            _cpworldBuf.Clear();
        }
        if (_cpwdftBuf is { Length: > 0 })
        {
            sk.WF ??= Open("cpwdft", seed);
            if (!sk.HeaderF) { sk.WF.WriteLine(CpwdftHeaderText); sk.HeaderF = true; }
            foreach (var line in _cpwdftBuf.ToString().Split('\n', System.StringSplitOptions.RemoveEmptyEntries)) sk.WF.WriteLine(qs + "\t" + line);
            _cpwdftBuf.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CpwdClose(MultiNationSim sim)
    {
        if (CpwdSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.WW?.Dispose(); sk.WW = null; sk.WF?.Dispose(); sk.WF = null; }
    }
}
