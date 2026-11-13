using System.Runtime.CompilerServices;

namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない（蓄えは ConditionalWeakTable＝セーブに載らない）。〔履歴 InstrumentsSb.cs#1〕
public static partial class Instruments
{
    internal const string SbUHeaderText = "quarter\tyear\tnation\tlaw\tsuffrage\tregionMonths\ttolMonths\tctrlMonths\tkappaSum\tmassN\turbanMassSum\tfacScan\teligN\tposN\tguardN\tvaSum\texcessSum\tuSum\townerLoss\twageGain\tbetaVa\tbetaNoStrikeVa\tsSum\tuRateSum\tvwSum\tvoSum\tstrikeSum\texportFactory\texportAll\tpremN\tpremPos\tsVoteSum";
    internal const string SbRHeaderText = "quarter\tyear\tnation\trgoN\trgoVa\trgoRent\trgoWages\tselfN\tselfVa\tselfResid\tcapN\tcapErr\tclosedEarned\truralMassEarned\truralMassSize\tlandRentLast\tlegBreach\tcustomBindN";
    internal const string SbRRegHeaderText = "quarter\tyear\tnation\tregion\tcrowd\trev\tinCost\tva\trent\twages";

    private sealed class SbAcc
    {
        public int RegionMonths, TolMonths, CtrlMonths, EligN, PosN, GuardN, RgoN, SelfN, CapN;
        public double PremN, PremPos, SVoteSum, KappaSum, MassN, UrbanMass, FacScan, StrikeSum, ExportFactory, ExportAll, VaSum, ExcessSum, USum, OwnerLoss, WageGain, BetaVa, BetaNoStrikeVa, SSum, URateSum, VwSum, VoSum;
        public double RgoVa, RgoRent, RgoWages, SelfVa, SelfResid, CapErr, LegBreach;
        public int CustomBindN;
    }
    private sealed class SbRegAcc { public double Crowd = double.NaN, Rev, InCost, Va, Rent, Wages; }
    static readonly ConditionalWeakTable<World, SbAcc> SbAccs = new();
    static readonly ConditionalWeakTable<Region, SbRegAcc> SbRegAccs = new();

    /// <summary>州の月（法が禁止でない国）の前走査の結果を積む。</summary>
    internal static void SbRegionMonth(World w, in UnionBargaining.Ctx ctx)
    {
        var a = SbAccs.GetValue(w, _ => new SbAcc());
        a.RegionMonths++; a.KappaSum += ctx.Kappa;
        if (w.LaborUnion == LaborUnionLaw.Tolerated) a.TolMonths++; else if (w.LaborUnion == LaborUnionLaw.Controlled) a.CtrlMonths++;
        a.MassN += ctx.MassN; a.UrbanMass += ctx.UrbanMass; a.FacScan += ctx.FacScan;
    }

    /// <summary>輸出代金・輸送財の代金（InternationalMarket.PaySales）の分け前を積む（旗 UnionBargaining が正のときだけ呼ばれる・読むだけ）。</summary>
    internal static void SbExport(World w, double amount, bool factory)
    {
        var a = SbAccs.GetValue(w, _ => new SbAcc());
        a.ExportAll += amount; if (factory) a.ExportFactory += amount;
    }

    /// <summary>適格な工場×月の結果を積む。ownerLoss と wageGain は rent・wages の前後の差から（Transfer の返り値とは別の変数）。</summary>
    internal static void SbFactory(World w, in UnionBargaining.Outcome o, double va, double rentBefore, double rentAfter, double wagesBefore, double wagesAfter)
    {
        var a = SbAccs.GetValue(w, _ => new SbAcc());
        a.EligN++; if (o.Transfer > 0) a.PosN++; if (o.Guarded) a.GuardN++;
        a.VaSum += va; a.ExcessSum += o.Excess; a.USum += o.Transfer;
        a.OwnerLoss += rentBefore - rentAfter; a.WageGain += wagesAfter - wagesBefore;
        a.BetaVa += o.Beta * va; a.BetaNoStrikeVa += o.BetaNoStrike * va;
        a.SSum += o.S; a.URateSum += o.U; a.VwSum += o.Vw; a.VoSum += o.Vo; a.StrikeSum += o.Strike;
        a.SVoteSum += o.Vw + o.Vo > 0 ? o.Vw / (o.Vw + o.Vo) : 0.0;                                   // 票の道だけの S（追記 4：参政権の段の前後の読み＝闘争度の内生の動きを含まない）
        if (o.Beta > 0 && o.Excess > 0) { a.PremN++; if (rentBefore - rentAfter > 0 && wagesAfter - wagesBefore > 0) a.PremPos++; }   // 工場ごとの前提（β＞0 かつ 超過＞0）と、その工場で持ち主の減りと労働者の増えが実際に出た数（追記 4 の U3'）
    }

    /// <summary>産地×月（付加価値が非負）の地代の分け方を積む（INS のときだけ呼ばれる）。rent・wages は国営の分け前を引く前の値。</summary>
    internal static void SbRgo(World w, Region r, Producer s, double va, double rent, double wages, double ruralPop, double revenue, double inCost)
    {
        var a = SbAccs.GetValue(w, _ => new SbAcc());
        a.RgoN++; a.RgoVa += va; a.RgoRent += rent; a.RgoWages += wages;
        if (s.OwnerPopIndex == s.LaborPopIndex) { a.SelfN++; a.SelfVa += va; a.SelfResid += Math.Abs(rent + wages - va); }
        // legal〔履歴 InstrumentsSb.cs#2〕
        double legal = r.FarmRentCap.Value;
        double cap = Math.Min(s.RentShare.Value, legal);
        if (cap < 1.0 && va > 0) { a.CapN++; a.CapErr += Math.Abs(rent / va - cap); }
        if (va > 0) { a.LegBreach += Math.Max(0.0, rent / va - legal); if (s.RentShare.Value < legal) a.CustomBindN++; }
        var g = SbRegAccs.GetValue(r, _ => new SbRegAcc());
        g.Crowd = r.ArableLand < 1e8 ? ruralPop / Math.Max(1.0, r.ArableLand) : double.NaN;
        g.Rev += revenue; g.InCost += inCost; g.Va += va; g.Rent += rent; g.Wages += wages;
    }

    private static string R(double v) => double.IsNaN(v) ? "nan" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    static void SbFold(Sink sk, ulong seed, World w, int q, bool picked)
    {
        var c = w.Coef;
        if (!(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var a = SbAccs.GetValue(w, _ => new SbAcc());
        if (c.UnionBargaining > 0)
        {
            sk.SbU ??= Open("sbu", seed);
            if (!sk.SbUHeader) { sk.SbU.WriteLine(SbUHeaderText); sk.SbUHeader = true; }
            sk.SbU.WriteLine(string.Join("\t", I(q), I(w.Year), NameOf(w), I((int)w.LaborUnion), I((int)w.Suffrage), I(a.RegionMonths), I(a.TolMonths), I(a.CtrlMonths), R(a.KappaSum), R(a.MassN), R(a.UrbanMass), R(a.FacScan), I(a.EligN), I(a.PosN), I(a.GuardN),
                R(a.VaSum), R(a.ExcessSum), R(a.USum), R(a.OwnerLoss), R(a.WageGain), R(a.BetaVa), R(a.BetaNoStrikeVa), R(a.SSum), R(a.URateSum), R(a.VwSum), R(a.VoSum), R(a.StrikeSum), R(a.ExportFactory), R(a.ExportAll), R(a.PremN), R(a.PremPos), R(a.SVoteSum)));
        }
        {
            double closedE = 0, massE = 0, massSize = 0, landRent = 0;
            foreach (var (_, p) in w.AllPopsWithRegion())
            {
                landRent += p.LandRentPerMonth.Value;
                if (p.Closed) closedE += p.EarnedPerMonth.Value;
            }
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Pops)
                    if (!p.IsUrban && PolicyPreferences.BlocOf(p) == 2) { massE += p.EarnedPerMonth.Value; massSize += p.Size; }
            }
            sk.SbR ??= Open("sbr", seed);
            if (!sk.SbRHeader) { sk.SbR.WriteLine(SbRHeaderText); sk.SbRHeader = true; }
            sk.SbR.WriteLine(string.Join("\t", I(q), I(w.Year), NameOf(w), I(a.RgoN), R(a.RgoVa), R(a.RgoRent), R(a.RgoWages), I(a.SelfN), R(a.SelfVa), R(a.SelfResid), I(a.CapN), R(a.CapErr),
                R(closedE), R(massE), R(massSize), R(landRent), R(a.LegBreach), I(a.CustomBindN)));
            if (picked)
            {
                sk.SbRReg ??= Open("sbrreg", seed);
                if (!sk.SbRRegHeader) { sk.SbRReg.WriteLine(SbRRegHeaderText); sk.SbRRegHeader = true; }
                foreach (var r in w.Regions)
                {
                    if (!SbRegAccs.TryGetValue(r, out var g)) continue;
                    if (g.Rev != 0 || g.Va != 0) sk.SbRReg.WriteLine(string.Join("\t", I(q), I(w.Year), NameOf(w), r.Name, F(g.Crowd), R(g.Rev), R(g.InCost), R(g.Va), R(g.Rent), R(g.Wages)));
                    g.Rev = g.InCost = g.Va = g.Rent = g.Wages = 0;
                }
            }
            else foreach (var r in w.Regions) if (SbRegAccs.TryGetValue(r, out var g)) { g.Rev = g.InCost = g.Va = g.Rent = g.Wages = 0; }
        }
        a.RegionMonths = a.TolMonths = a.CtrlMonths = a.EligN = a.PosN = a.GuardN = a.RgoN = a.SelfN = a.CapN = 0;
        a.PremN = a.PremPos = a.SVoteSum = a.KappaSum = a.MassN = a.UrbanMass = a.FacScan = a.StrikeSum = a.ExportFactory = a.ExportAll = a.VaSum = a.ExcessSum = a.USum = a.OwnerLoss = a.WageGain = a.BetaVa = a.BetaNoStrikeVa = a.SSum = a.URateSum = a.VwSum = a.VoSum = 0;
        a.RgoVa = a.RgoRent = a.RgoWages = a.SelfVa = a.SelfResid = a.CapErr = a.LegBreach = 0; a.CustomBindN = 0;
    }
}
