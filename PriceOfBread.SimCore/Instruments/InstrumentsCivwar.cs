namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・World に欄を足さない。〔履歴 InstrumentsCivwar.cs#1〕
public static partial class Instruments
{
    sealed class CwAcc
    {
        public int Civil, Foreign, OldMis, NewMis, CtMis, ZeroM;
        public double ActC, OldC, NewC, ExhC, ActF, NewF, MilC;
        public double AmmoD, AmmoP, PowD, PowP, LeadD, LeadP;
        public double Ct, CtR, ZeroAct;
    }
    sealed class CwSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, CwAcc> CwAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CwSink> CwSinks = new();

    internal const string CwHeaderText = "quarter\tyear\tnationUid\tnation\tcivilWarMonthsCount\tforeignWarMonthsCount\t"
        + "warMultActualCivilSumIndex\twarMultOldRuleCivilSumIndex\twarMultNewRuleCivilSumIndex\twarExhaustionCivilSumShare\t"
        + "warMultActualForeignSumIndex\twarMultNewRuleForeignSumIndex\toldRuleMismatchMonthsCount\tnewRuleMismatchMonthsCount\t"
        + "militaryScaleCivilSumIndex\tammunitionDemandCivilQty\tammunitionProducedCivilQty\tgunpowderDemandCivilQty\tgunpowderProducedCivilQty\tleadDemandCivilQty\tleadProducedCivilQty\t"
        + "contactShareSumIndex\twarMultContactRuleSumIndex\tcontactRuleMismatchMonthsCount\tzeroContactWarMonthsCount\twarMultActualZeroContactSumIndex";

    static bool CwMismatch(double actual, double rule) => !(System.Math.Abs(actual - rule) <= 1e-12 * System.Math.Max(1.0, System.Math.Abs(rule)));

    /// <summary>月（<see cref="WarMonth"/> の中）：内戦か・交戦かを計器が自分で決め、旧と新の規則と実際の値を積む。</summary>
    static void CwMonth(World w)
    {
        if (w.Diplomacy is not { } d || w.SelfPower is not { } sp) return;
        bool civil = false, war = false;
        foreach (var x in d.Wars)
        {
            if (x.A != sp && x.B != sp) continue;
            war = true;
            if (x.CivilWarParent is not null) civil = true;
        }
        if (!war) return;
        var c = w.Coef; var acc = CwAccs.GetValue(w, _ => new CwAcc());
        double exh = sp.WarExhaustion;
        double ruleNew = c.MilitaryWarMult * (1.0 + c.WarMobilizationDepth * exh);
        double ruleOld = civil ? c.MilitaryWarMult * 2.0 : ruleNew;
        double actual = w.Region.MilitaryWarMultLast;
        if (CwMismatch(actual, ruleOld)) acc.OldMis++;
        if (CwMismatch(actual, ruleNew)) acc.NewMis++;
        // nFront〔履歴 InstrumentsCivwar.cs#2〕
        var nFront = new System.Collections.Generic.Dictionary<Power, int>();
        foreach (var x in d.Wars) { nFront[x.A] = nFront.GetValueOrDefault(x.A) + 1; nFront[x.B] = nFront.GetValueOrDefault(x.B) + 1; }
        double touch = 0;
        foreach (var x in d.Wars)
        {
            if (x.A != sp && x.B != sp) continue;
            double pa = d.FrontPush(x.A, x.B, c, nFront[x.A]), pb = d.FrontPush(x.B, x.A, c, nFront[x.B]);
            touch += pa < pb ? pa : pb;
        }
        double share = sp.Military > 1e-9 ? touch / sp.Military : 0;
        if (!(share > 0)) share = 0;
        double ruleContact = 1.0 + c.WarArmsDemandPerContact * share;
        acc.Ct += share; acc.CtR += ruleContact;
        if (CwMismatch(actual, ruleContact)) acc.CtMis++;
        if (share < 1e-9) { acc.ZeroM++; acc.ZeroAct += actual; }
        if (!civil) { acc.Foreign++; acc.ActF += actual; acc.NewF += ruleNew; return; }
        acc.Civil++; acc.ActC += actual; acc.OldC += ruleOld; acc.NewC += ruleNew; acc.ExhC += exh;
        foreach (var r in w.Regions)
        {
            acc.MilC += r.MilitaryScale;
            var m = r.Markets;
            if (Catalog.AmmunitionId < m.Length) { acc.AmmoD += m[Catalog.AmmunitionId].LastDemand; acc.AmmoP += m[Catalog.AmmunitionId].LastProduced; }
            if (Catalog.GunpowderId < m.Length) { acc.PowD += m[Catalog.GunpowderId].LastDemand; acc.PowP += m[Catalog.GunpowderId].LastProduced; }
            if (Catalog.LeadId < m.Length) { acc.LeadD += m[Catalog.LeadId].LastDemand; acc.LeadP += m[Catalog.LeadId].LastProduced; }
        }
    }

    /// <summary>助走の直後（<see cref="Phase"/> の "warm"）：助走の月の積算を捨てる。</summary>
    static void CwWarm(MultiNationSim sim)
    {
        foreach (var n in sim.Nations) CwAccs.Remove(n.World);
    }

    static string CwI(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>四半期の末（<see cref="Quarter"/> の国ごとの段）：交戦の月が 1 以上の国の行を書き、積算を捨てる。</summary>
    static void CwFold(MultiNationSim sim, ulong seed, World w, int q)
    {
        if (!CwAccs.TryGetValue(w, out var a)) return;
        CwAccs.Remove(w);
        if (a.Civil + a.Foreign <= 0) return;
        var sk = CwSinks.GetValue(sim, _ => new CwSink());
        sk.W ??= Open("cwmult", seed);
        if (!sk.Header) { sk.W.WriteLine(CwHeaderText); sk.Header = true; }
        sk.W.WriteLine(string.Join('\t',
            CwI(q), CwI(w.Year), CwI(S(w).Uid), NameOf(w), CwI(a.Civil), CwI(a.Foreign),
            F(a.ActC), F(a.OldC), F(a.NewC), F(a.ExhC), F(a.ActF), F(a.NewF), CwI(a.OldMis), CwI(a.NewMis),
            F(a.MilC), F(a.AmmoD), F(a.AmmoP), F(a.PowD), F(a.PowP), F(a.LeadD), F(a.LeadP),
            F(a.Ct), F(a.CtR), CwI(a.CtMis), CwI(a.ZeroM), F(a.ZeroAct)));
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CwClose(MultiNationSim sim)
    {
        if (CwSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
