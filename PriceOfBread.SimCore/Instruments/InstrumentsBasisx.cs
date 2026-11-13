using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（状態は全て計器の持ち物＝ConditionalWeakTable）。〔履歴 InstrumentsBasisx.cs#1〕
public static partial class Instruments
{
    internal const string BasisxHeaderText = "quarter\tyear\tnation\tbxRate\tbxRgoCount\tbxFrozenRgoCount\t"
        + "bxBasePbPerMonth\tbxRentPbPerMonth\tbxFrozenPbPerMonth\tbxRentOfFrozenPbPerMonth\tbxRentPbPopSidePerMonth\tbxFrozenShare\t"
        + "bxExpInflPerYear\tbxExpInflRulePerYear\tbxDeltaCNowToPropPerMonth\tbxDeltaCExpToFixedPerMonth\tbxDeltaCExpToPropPerMonth\tbxMovedToFixed\tbxMovedToProp\t"
        + "bxH4BasePbPerMonth\tbxH4DToFixed\tbxH4GToFixed\tbxH4DToProp\tbxH4GToProp\t"
        + "bxFundingRatio\tbxDebtToGdpYear\tbxCrisis1\tbxCrisis2\tbxStance\tbxRBase\t"
        + "bxTargetA\tbxTargetB\tbxTargetC1\tbxTargetC2\tbxTargetS\t"
        + "bxShadowFrozenA\tbxShadowFrozenB\tbxShadowFrozenC1\tbxShadowFrozenC2\tbxShadowFrozenS\t"
        + "bxSwitchA\tbxSwitchB\tbxSwitchC1\tbxSwitchC2\tbxSwitchS\tbxNanFlag";

    /// <summary>規則の数（A・B・C1・C2・S）。</summary>
    internal const int BxRules = 5;

    sealed class BasisxSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, BasisxSink> BasisxSinks = new();

    /// <summary>州ごとの POP 別の凍結の査定の額 A_p（計器の持ち物・その月）。</summary>
    sealed class BxReg { public double[] A = []; public bool Touched; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, BxReg> BxRegs = new();

    /// <summary>国の状態（計器の持ち物）：月の和・四半期の締め月の写し・規則の影の凍結。</summary>
    sealed class BxNat
    {
        // 月の和（BasisxRgoRent が積み BasisxMonth が締めて 0 へ戻す）
        public int Rgo, Frozen;
        public double BasePb, RentPb, FrozenPb, RentFrozenPb;
        public bool DegReady; public double Deg;
        // 四半期の締め月の写し
        public bool Snap;
        public int SRgo, SFrozen;
        public double SBasePb, SRentPb, SFrozenPb, SRentFrozenPb, SRentPopSide, STau, SPi, SFund, SDebt, SH4B, SDf, SGf, SDp, SGp;
        public bool SNan;
        // 支払い率の述語
        public bool Below;           // この四半期のどれかの月に r＜1
        public double LastR = double.NaN;   // 最後に見た月の r
        public int Stance = -1;      // 最後に見た姿勢の番号（-1＝まだ無い）
        // 規則の影の凍結
        public bool Init;
        public readonly bool[] Fixed = new bool[BxRules];
        public readonly double[] F = new double[BxRules];
        public readonly int[] Sw = new int[BxRules];
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, BxNat> BxNats = new();

    static bool BxPicked(World w) => IsPicked(w, S(w));

    /// <summary>規則 A・B の 1 歩（純関数）：影の凍結 <paramref name="fixedState"/>・額 <paramref name="f"/> を、期待インフレ <paramref name="pi"/> と基数 <paramref name="R"/> で更新する。
    /// P で π＜0 なら F（f＝R）。F の戻しは <paramref name="ruleB"/> なら R＞f・でなければ R×(1+π)＞f。NaN は動かさない（否定形）。切り替えたら true。</summary>
    internal static bool BxRuleStep(ref bool fixedState, ref double f, bool ruleB, double pi, double R)
    {
        if (double.IsNaN(pi) || double.IsNaN(R)) return false;
        if (!fixedState)
        {
            if (pi < 0) { fixedState = true; f = R; return true; }
            return false;
        }
        bool back = ruleB ? R > f : R * (1 + pi) > f;
        if (back) { fixedState = false; f = 0; return true; }
        return false;
    }

    /// <summary>身分制免税の度合い d′（Privileges.cs:41〜:45 の式を計器が書く・landfix と同じ）。</summary>
    static double BxDeg(World w)
    {
        var c = w.Coef;
        double disc = c.PrivilegeTaxDiscount.Value;
        return !(disc > 0) ? 0.0
            : c.PrivilegeFromCrownShare.Value > 0 ? double.NaN
            : (w.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate ? disc : 0.0);
    }

    static double BxPb(Pop p, double deg) => p.IsCriminal ? 0.0 : (p.Closed && !p.IsSoldier ? 1.0 - deg : 1.0);

    /// <summary>受け口（<see cref="LandbaseRgoRent"/> の頭・RGO だけ・INS のときだけ）：割る前の地代 <paramref name="rent"/> を受け手ごとに積む。助走の月は積まない。</summary>
    internal static void BasisxRgoRent(World w, Region r, Producer s, double rent)
    {
        if (w.WarmupFrozen || !BxPicked(w)) return;
        var a = BxNats.GetValue(w, _ => new BxNat());
        if (!a.DegReady) { a.Deg = BxDeg(w); a.DegReady = true; }
        bool frozen = LbFrozen.TryGetValue(s, out var fz) && fz!.Has && ReferenceEquals(fz.W, w);
        a.Rgo++; if (frozen) a.Frozen++;
        var ls = s.LandShares;
        long dummy = 0;
        int nk = ls is null ? 1 : ls.Length;
        for (int k = 0; k < nk; k++)
        {
            if (ls is not null && !(ls[k] > 0)) continue;
            int idx = ls is null ? s.OwnerPopIndex : LsRecipient(r, s, k, ref dummy);
            if (idx < 0 || idx >= r.Pops.Count) continue;
            double share = ls is null ? 1.0 : ls[k];
            double rr = System.Math.Max(0, rent * share);
            double pb = BxPb(r.Pops[idx], a.Deg);
            a.RentPb += pb * rr;
            if (frozen)
            {
                double fv = fz!.V * share;
                a.BasePb += pb * fv; a.FrozenPb += pb * fv; a.RentFrozenPb += pb * rr;
                var g = BxRegs.GetValue(r, _ => new BxReg());
                if (g.A.Length < r.Pops.Count) System.Array.Resize(ref g.A, r.Pops.Count);
                g.A[idx] += fv; g.Touched = true;
            }
            else a.BasePb += pb * rr;
        }
    }

    /// <summary>月の閉じ（<see cref="MonthClose"/>・Fiscal.Step の後）：支払い率の述語・姿勢を写し、四半期の締め月は和を写して H4 の D・G・B を数え、月の和を 0 へ戻す。</summary>
    internal static void BasisxMonth(World w)
    {
        if (w.Regions.Count == 0 || !BxPicked(w)) return;
        var a = BxNats.GetValue(w, _ => new BxNat());
        int stn = S(w).ModStance;
        if (stn >= 0) a.Stance = stn;
        if (w.WarmupFrozen) { BxReset(w, a); return; }
        double rate = w.FiscalFundingRatio.Value;
        a.LastR = rate; if (rate < 1) a.Below = true;
        if (w.IsQuarterEndMonth)
        {
            double tau = w.LandTax, pi = w.ExpectedInflationPerYear.Value, deg = a.Deg;
            if (!a.DegReady) deg = BxDeg(w);
            double phi = a.RentPb > 0 ? a.RentFrozenPb / a.RentPb : double.NaN;
            double dF = 0, gF = 0, dP = 0, gP = 0, h4b = 0, popSide = 0;
            foreach (var r in w.Regions)
            {
                BxRegs.TryGetValue(r, out var g);
                for (int i = 0; i < r.Pops.Count; i++)
                {
                    var p = r.Pops[i];
                    double pb = BxPb(p, deg), rp = System.Math.Max(0, p.LandRentPerMonth.Value);
                    double ap = g is not null && g.Touched && i < g.A.Length ? g.A[i] : 0.0;
                    popSide += pb * rp;
                    double cF = -tau * pb * rp * pi;
                    dF += cF; gF += System.Math.Abs(cF);
                    double cP = phi > 0 ? tau * pb * (rp * (1 + pi) - ap / phi) : double.NaN;   // φ＝0 か nan は比例へ戻す物が無い＝nan
                    dP += cP; gP += System.Math.Abs(cP);
                    h4b += pb * (ap + (1 - (phi > 0 ? phi : 0.0)) * rp);
                }
            }
            a.Snap = true; a.SRgo = a.Rgo; a.SFrozen = a.Frozen;
            a.SBasePb = a.BasePb; a.SRentPb = a.RentPb; a.SFrozenPb = a.FrozenPb; a.SRentFrozenPb = a.RentFrozenPb; a.SRentPopSide = popSide;
            a.STau = tau; a.SPi = pi; a.SFund = rate;
            a.SDebt = w.NominalGdpPerMonth.Value > 0 ? w.GovDebt.Value / (w.NominalGdpPerMonth.Value * 12.0) : double.NaN;
            a.SH4B = h4b; a.SDf = dF; a.SGf = gF; a.SDp = dP; a.SGp = gP;
            a.SNan = double.IsNaN(pi) || double.IsNaN(a.BasePb) || !(a.BasePb > 0);
        }
        BxReset(w, a);
    }

    static void BxReset(World w, BxNat a)
    {
        a.Rgo = 0; a.Frozen = 0; a.BasePb = a.RentPb = a.FrozenPb = a.RentFrozenPb = 0; a.DegReady = false;
        foreach (var r in w.Regions)
            if (BxRegs.TryGetValue(r, out var g) && g.Touched) { System.Array.Clear(g.A); g.Touched = false; }
    }

    /// <summary>四半期の末（<see cref="Quarter"/>・QuarterEnd の後）：計器の国の名簿の国の行（basisx_s）を書き、規則の影の凍結を 1 歩進める。</summary>
    static void BasisxTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = BasisxSinks.GetValue(sim, _ => new BasisxSink());
        if (sk.W is null) { sk.W = Open("basisx", seed); sk.W.WriteLine(BasisxHeaderText); }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0 || !BxPicked(w)) continue;
            var a = BxNats.GetValue(w, _ => new BxNat());
            double piRow = w.ExpectedInflationPerYear.Value, rBase = LbR(w);
            if (!a.Init)
            {
                bool fx = LbNatOf(w).Fixed;
                for (int k = 0; k < BxRules; k++) { a.Fixed[k] = fx; a.F[k] = fx ? rBase : 0.0; }
                a.Init = true;
            }
            bool crisis1 = a.LastR < 1, crisis2 = a.Below;
            // 規則を進める（A・B・C1・C2・S）
            // 切り替えの回数は前後の状態の違いで数える。
            void Step(int k, bool ruleB, bool eval) { bool before = a.Fixed[k]; if (eval) BxRuleStep(ref a.Fixed[k], ref a.F[k], ruleB, piRow, rBase); if (a.Fixed[k] != before) a.Sw[k]++; }
            Step(0, false, true); Step(1, true, true); Step(2, false, crisis1); Step(3, false, crisis2);
            if (a.Stance == (int)UtilityAgent.Stance.Modernize && !a.Fixed[4] && !double.IsNaN(rBase)) { a.Fixed[4] = true; a.F[4] = rBase; a.Sw[4]++; }
            double nan = double.NaN;
            bool sn = a.Snap;
            double B = a.SBasePb, tau = a.STau, pi = a.SPi;
            double dNow = sn ? tau * (a.SRentFrozenPb - a.SFrozenPb) : nan;
            double dFix = sn ? -tau * pi * (a.SRentPb - a.SRentFrozenPb) : nan;
            double dProp = sn ? tau * (a.SRentFrozenPb * (1 + pi) - a.SFrozenPb) : nan;
            double mFix = sn && B > 0 ? System.Math.Min(1.0, System.Math.Abs(dFix) / B) : nan;
            double mProp = sn && B > 0 ? System.Math.Min(1.0, System.Math.Abs(dProp) / B) : nan;
            double phi = sn && a.SRentPb > 0 ? a.SRentFrozenPb / a.SRentPb : nan;
            string row = string.Join('\t',
                I(q), I(w.Year), NameOf(w), F(sn ? tau : nan), I(sn ? a.SRgo : -1), I(sn ? a.SFrozen : -1),
                F(sn ? B : nan), F(sn ? a.SRentPb : nan), F(sn ? a.SFrozenPb : nan), F(sn ? a.SRentFrozenPb : nan), F(sn ? a.SRentPopSide : nan), F(phi),
                F(sn ? pi : nan), F(piRow), F(dNow), F(dFix), F(dProp), F(mFix), F(mProp),
                F(sn ? a.SH4B : nan), F(sn ? a.SDf : nan), F(sn ? a.SGf : nan), F(sn ? a.SDp : nan), F(sn ? a.SGp : nan),
                F(sn ? a.SFund : nan), F(sn ? a.SDebt : nan), I(crisis1 ? 1 : 0), I(crisis2 ? 1 : 0), I(a.Stance), F(rBase),
                F(a.Fixed[0] ? 1 : 0), F(a.Fixed[1] ? 1 : 0), F(a.Fixed[2] ? 1 : 0), F(a.Fixed[3] ? 1 : 0), F(a.Fixed[4] ? 1 : 0),
                F(a.Fixed[0] ? a.F[0] : 0), F(a.Fixed[1] ? a.F[1] : 0), F(a.Fixed[2] ? a.F[2] : 0), F(a.Fixed[3] ? a.F[3] : 0), F(a.Fixed[4] ? a.F[4] : 0),
                I(a.Sw[0]), I(a.Sw[1]), I(a.Sw[2]), I(a.Sw[3]), I(a.Sw[4]), I(sn && a.SNan ? 1 : 0));
            sk.W.WriteLine(row);
            a.Snap = false; a.Below = false;
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void BasisxClose(MultiNationSim sim)
    {
        if (BasisxSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
