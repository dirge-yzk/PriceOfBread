namespace PriceOfBread.SimCore;

// (1166) c115sv：兵 0 の崖（旗 SoldierVacancyFromZero・Meritocracy.cs）の恒等の計器 soldiervac_s（国×四半期）。事前登録 measurements/m1166_20261116_prereg_sv.txt。
//   口は Meritocracy.Step の州の空きを組んだ直後（SoldierVacEnter）と州の終わり（SoldierVacExit）。Instruments.On の時だけ呼ばれる。助走の月は数えない。
//   大きさ 0 の州の埋まり（後−前）と、空き（soldierVac0＝エンジンの局所の値を受け取る＝限界）を超えた量を数える。
//   同じ盤面で、大きさ＞0 の兵の今の読み（EarnedPerMonth÷Size）と、新しい読み（徴募の母数の単価×SoldierPayMult×国の兵の受け取りの比 seatPayRate）を計器が自分で組んで比べる
//   （eqSumLogLast・Now の列は指示の元の形＝×(1−U)・記録。eqSumLogK が採る形）
//   （単価は計器が州の POP を自分で回して組む＝Military.RecruitWage を呼ばない）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
public static partial class Instruments
{
    internal const string SoldierVacHeaderText = "year\tq\tnation\tlawCode\tatWarFlag\tregions\tzeroStart\tzeroVac\tzeroVacColonial\tzeroFilled"
        + "\tfillZeroK\tvacZeroK\toverfillZeroK\tfillPosK\toverfillPosK\teqN\teqSumLogLast\teqMaxAbsLogLast\teqSumLogNow\teqMaxAbsLogNow"
        + "\tseatPayRate\teqSumLogK\teqMaxAbsLogK";

    sealed class SoldierVacAcc
    {
        public int Regions, ZeroStart, ZeroVac, ZeroVacColonial, ZeroFilled, EqN;
        public double FillZero, VacZero, OverZero, FillPos, OverPos, EqSumLast, EqMaxLast, EqSumNow, EqMaxNow, K, EqSumK, EqMaxK;
        public double CurStart, CurVac; public bool CurOn;
    }
    sealed class SoldierVacSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, SoldierVacAcc> SoldierVacAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SoldierVacSink> SoldierVacSinks = new();

    /// <summary>州の徴募の母数（兵でない SoldierRecruitClassA・B）の所得の単価＝所得の和÷働き盛り（keep なら頭数）の和。計器が自分で組む。</summary>
    static double SoldierVacUnit(Region r, Coefficients c, bool last, bool keep)
    {
        double pe = 0, pa = 0;
        foreach (var p in r.Pops)
        {
            if (p.IsSoldier || (p.ClassId != c.SoldierRecruitClassA && p.ClassId != c.SoldierRecruitClassB)) continue;
            pe += System.Math.Max(0, (last ? p.LastEarnedPerMonth : p.EarnedPerMonth).Value);
            pa += keep ? p.Size : p.Size * p.AgeShare(16, 60);
        }
        return pa > 1e-9 ? pe / pa : 0;
    }

    /// <summary>社会移動の頭（州の移動の前）：国の大きさ＞0 の兵が実際に受け取った比＝Σ兵の EarnedPerMonth÷Σ（兵の大きさ×計器が組む単価×SoldierPayMult）を計器が自分で組む。</summary>
    internal static void SoldierVacBegin(World w)
    {
        if (w.WarmupFrozen || !Wants("soldiervac_s")) return;
        var a = SoldierVacAccs.GetValue(w, _ => new SoldierVacAcc());
        bool keep = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        double num = 0, den = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                if (!p.IsSoldier) continue;
                if (p.Size > 1e-9)
                {
                    num += System.Math.Max(0, p.EarnedPerMonth.Value);
                    den += p.Size * SoldierVacUnit(r, w.Coef, true, keep) * w.Coef.SoldierPayMult;
                }
                break;
            }
        a.K = den > 1e-9 ? num / den : 0;
    }

    internal static void SoldierVacEnter(World w, Region r, Pop soldier, double vac0)
    {
        if (w.WarmupFrozen || !Wants("soldiervac_s")) return;
        var a = SoldierVacAccs.GetValue(w, _ => new SoldierVacAcc());
        a.CurOn = true; a.CurStart = soldier.Size; a.CurVac = vac0;
        a.Regions++;
        if (soldier.Size <= 1e-9)
        {
            a.ZeroStart++;
            if (vac0 > 1e-9) { a.ZeroVac++; if (r.Tier == RegionTier.Colonial) a.ZeroVacColonial++; }
            return;
        }
        double actual = System.Math.Max(0, soldier.EarnedPerMonth.Value) / soldier.Size;
        if (!(actual > 0)) return;
        bool keep = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        double rate = System.Math.Max(0, 1.0 - soldier.UnemploymentRate.Value);
        double fLast = SoldierVacUnit(r, w.Coef, true, keep) * w.Coef.SoldierPayMult * rate;
        double fNow = SoldierVacUnit(r, w.Coef, false, keep) * w.Coef.SoldierPayMult * rate;
        if (!(fLast > 0) || !(fNow > 0)) return;
        double fK = SoldierVacUnit(r, w.Coef, true, keep) * w.Coef.SoldierPayMult * a.K;
        if (fK > 0) { double lK = System.Math.Log(fK / actual); a.EqSumK += lK; if (System.Math.Abs(lK) > a.EqMaxK) a.EqMaxK = System.Math.Abs(lK); }
        double lL = System.Math.Log(fLast / actual), lN = System.Math.Log(fNow / actual);
        a.EqN++; a.EqSumLast += lL; a.EqSumNow += lN;
        if (System.Math.Abs(lL) > a.EqMaxLast) a.EqMaxLast = System.Math.Abs(lL);
        if (System.Math.Abs(lN) > a.EqMaxNow) a.EqMaxNow = System.Math.Abs(lN);
    }

    internal static void SoldierVacExit(World w, Region r, Pop soldier)
    {
        if (w.WarmupFrozen || !Wants("soldiervac_s")) return;
        var a = SoldierVacAccs.GetValue(w, _ => new SoldierVacAcc());
        if (!a.CurOn) return;
        a.CurOn = false;
        double fill = soldier.Size - a.CurStart;
        double over = System.Math.Max(0, fill - a.CurVac);
        if (a.CurStart <= 1e-9)
        {
            if (soldier.Size > 1e-9) a.ZeroFilled++;
            a.FillZero += fill; a.VacZero += a.CurVac; a.OverZero += over;
        }
        else { a.FillPos += fill; a.OverPos += over; }
    }

    static void SoldierVacTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("soldiervac_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = SoldierVacSinks.GetValue(sim, _ => new SoldierVacSink());
        if (sk.W is null) { sk.W = Open("soldiervac", seed); sk.W.WriteLine(SoldierVacHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int yearLabel = sim.Nations[0].World.Year;
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var a = SoldierVacAccs.GetValue(w, _ => new SoldierVacAcc());
            sb.Clear();
            sb.Append(I(yearLabel)).Append('\t').Append(I(q)).Append('\t').Append(NameOf(w))
              .Append('\t').Append(I((int)w.ConscriptionLaw)).Append('\t').Append(w.AtWarNow ? "1" : "0")
              .Append('\t').Append(I(a.Regions)).Append('\t').Append(I(a.ZeroStart)).Append('\t').Append(I(a.ZeroVac))
              .Append('\t').Append(I(a.ZeroVacColonial)).Append('\t').Append(I(a.ZeroFilled));
            foreach (double v in new[] { a.FillZero, a.VacZero, a.OverZero, a.FillPos, a.OverPos })
                sb.Append('\t').Append(FR(v));
            sb.Append('\t').Append(I(a.EqN));
            foreach (double v in new[] { a.EqSumLast, a.EqMaxLast, a.EqSumNow, a.EqMaxNow, a.K, a.EqSumK, a.EqMaxK })
                sb.Append('\t').Append(FR(v));
            sk.W.WriteLine(sb.ToString());
            SoldierVacAccs.AddOrUpdate(w, new SoldierVacAcc());
        }
    }

    static void SoldierVacClose(MultiNationSim sim) { if (SoldierVacSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
