using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（呼び出しの中の緩衝は ThreadStatic の計器の欄）。〔履歴 InstrumentsCpwd.cs#1〕
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

    /// <summary>
    /// 国 i の外資の負債を読む：slot 0＝逃避の前・1＝逃避の後（利札の時点）。〔履歴 InstrumentsCpwd.cs#2〕
    /// </summary>
    internal static void CpwdMark(IReadOnlyList<Simulation> nations, int i, int slot, double flightTotal)
    {
        if (!Tg.Cpwd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
        if (!Tg.Cpwd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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

    /// <summary>
    /// 〔履歴 InstrumentsCpwd.cs#3〕
    /// </summary>
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
        if (!Tg.Cpwd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
        if (!Tg.Cpwd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
        if (!Tg.Cpwd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
        if (!Tg.Cpwd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (CpwdSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.WW?.Dispose(); sk.WW = null; sk.WF?.Dispose(); sk.WF = null; }
    }
}
