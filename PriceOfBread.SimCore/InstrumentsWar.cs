using System.Collections.Generic;
using System.Globalization;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1018) 戦争の計器 warentry_s・warpair_s・warband_s と wars_s の入口の札・q の列（読むだけ・恒等・事前登録 measurements/m1018_20261110_prereg_wn.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（置き場は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝戦争の入口の統合（要求×(1−τ²)・余剰戦力の導き・弱小国の門・屈服の 1 本化）の前に、今の盤面の 〈評価の件数・理由・影の評価・緊張の帯・入口の札〉 を評価の瞬間の値で数える。
// 三つの表：warentry_s（国×四半期・AI の侵略の評価〔Diplomacy.TryAiSubjugate の CanInvade〕の件数と理由・影の評価・余剰の候補）／warpair_s（組×四半期・月の最大が 100 以上か出来事のあった組だけ）／warband_s（四半期×1）。
// ⚠限界（計器が自己申告する）：
//  ①月の列は Diplomacy.Step の頭で読む＝前の月の末の状態。四半期の最初の月の頭は前の四半期の末なので読まず、四半期の末（Quarter）の読みを 3 本目にする（四半期あたり 3 標本）。
//  ②膨張意欲の流れは合算の変化（前の四半期末→今）と、計器が今の盤面から計算した流入（戦争熱・商業）で導く。上限で切れた分も消費の側に出る。
//  ③WarScale の reach は 比×自陣営の陸の力 で復元する（浮動小数の復元）。自陣営の集合は計器が再構成する。
//  ④屈服・講和の置き直し・分離の置き直しは Diplomacy の該当行の hook で数える。
//  ⑤影の評価が呼ぶエンジンの関数（SeaControl・IsSeaCrossing・InvShipStock・InvCannedStock・InvadeWillCost・InvadeShipReq・InvadeCannedReq・Projection・DependentCount）は状態を書かず乱数を引かない（器のコミットで grep 済み）。
public static partial class Instruments
{
    /// <summary>入口の札の名前（添字＝札の番号）。参戦＝ally・suzerain・calltoarms（主の開戦ではない）。</summary>
    internal static readonly string[] WarCodeNames = ["unknown", "main", "subjugate", "declare", "ally", "suzerain", "civil", "ultimatum", "calltoarms", "mediation", "secession"];
    internal static bool WarIsJoin(int code) => code is 4 or 5 or 8;

    /// <summary>CanInvade の理由の文字列（World.cs の文字列そのまま）。添字＝件数の列。末尾の other は未知の文字列。</summary>
    internal static readonly string[] WarReasons = ["", "外交なし", "自国", "弱小国が標的", "既に従属", "従属関係", "奪う領土なし", "交戦中", "属国が上限", "余剰戦力が不足", "兵力が不足", "船が不足", "制海権が不足", "糧食(缶詰)が不足"];
    internal static readonly string[] WarReasonCols = ["ok", "noDip", "self", "weak", "dependent", "suzerainOf", "noLand", "atWar", "maxDep", "will", "mil", "ship", "sea", "canned", "other"];

    /// <summary>影の評価の門の番号（0＝全て通った）。World.cs:2190〜:2205 の順。</summary>
    internal static readonly string[] WarShadowGates = ["pass", "dependent", "suzerainOf", "noLand", "atWar", "maxDep", "will", "mil", "ship", "sea", "canned"];
    internal static readonly string[] WarShadowReasons = ["", "既に従属", "従属関係", "奪う領土なし", "交戦中", "属国が上限", "余剰戦力が不足", "兵力が不足", "船が不足", "制海権が不足", "糧食(缶詰)が不足"];

    sealed class WarEntryAcc
    {
        public long LastStamp = long.MinValue;
        public int Calls, Evals, TgtNonMinor, TgtRankMissing;
        public readonly int[] Reason = new int[WarReasonCols.Length];
        public readonly int[] Shadow = new int[WarShadowGates.Length];
        public int ShadowProjFail, ShadowProjPass, ShadowCalls;
        public bool CallShadowPass;
        // 呼び 1 回の最初の評価で読んだ状態の和
        public int FiniteCalls;
        public double Will, Fever, FMil, FMilSpend, FNoble, FSoldier, FThreat, FDiv;
        public double Land, Force, NeedLand, NeedInternal, NeedSea, PowerMil, LandPower, Own, Reach, Ratio, SA, SC, RA, RC;
        public int RCalls, SCLess;
        public int TauPos, TauPosWeak;   // 評価の組の緊張が正の件数（弱小国の門で落ちた評価だけの件数も）
        public double SumTau2;           // Σ min(1, 緊張÷140)²（要求×(1−τ²) の τ² の和・F4 の材料）
        public double PrevWill;
        public bool HavePrev;
    }

    sealed class WarPairAcc { public double MaxT; public int Ge140, Gt100, Gate, Starts, Submits, Peace, Secession; }

    sealed class WarSt
    {
        public int NextQ;
        public readonly List<(int Q, int Code)> WarMeta = new();
        public readonly List<int> InvadeQ = new();
        public int TagCode = 0, TagA = -1, TagB = -1;
        public readonly Dictionary<int, WarEntryAcc> Entry = new();
        public readonly Dictionary<long, WarPairAcc> Pair = new();
        public int PairsSampled;
        public int[] Band = new int[6];
        public int QSubmits, QPeace, QSecession;
        public readonly int[] QStarts = new int[WarCodeNames.Length];
        public System.IO.StreamWriter? We, Wp, Wb;
        public bool HeaderDone;
        public int Samples;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, WarSt> WarSts = new();
    static WarSt WarOf(Diplomacy d) => WarSts.GetValue(d, _ => new WarSt());
    internal static bool WarNoPair = Environment.GetEnvironmentVariable("POB_WAR_NOPAIR") is { Length: > 0 };

    static long WarKey(Power a, Power b) => a.Id < b.Id ? ((long)a.Id << 32) | (uint)b.Id : ((long)b.Id << 32) | (uint)a.Id;
    static string WI(int v) => v.ToString(CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------- 札・q ----------------------------------------------------------------
    /// <summary>入口の札を立てる（呼び手の行の頭・AddBelligerent／StartWar の直前）。組の鍵つき＝別の組の戦争には効かない。</summary>
    internal static void WarTag(Diplomacy d, Power a, Power b, int code) { var st = WarOf(d); st.TagCode = code; st.TagA = a.Id; st.TagB = b.Id; }
    /// <summary>既に同じ組の札が立っていなければ立てる（DeclareWar の既定の札＝統一）。</summary>
    internal static void WarTagDefault(Diplomacy d, Power a, Power b, int code)
    {
        var st = WarOf(d);
        if (st.TagCode != 0 && ((st.TagA == a.Id && st.TagB == b.Id) || (st.TagA == b.Id && st.TagB == a.Id))) return;
        st.TagCode = code; st.TagA = a.Id; st.TagB = b.Id;
    }
    /// <summary>戦争が立った所（WarStarted）：札と、次の Quarter(q) の q を記録する。</summary>
    internal static void WarNoteStart(Diplomacy d, War war)
    {
        var st = WarOf(d);
        int code = 0;
        if (st.TagCode != 0 && ((st.TagA == war.A.Id && st.TagB == war.B.Id) || (st.TagA == war.B.Id && st.TagB == war.A.Id))) code = st.TagCode;
        st.TagCode = 0;
        if (code == 0 && war.CivilWarParent is not null) code = war.CivilWarSecession ? 10 : 6;
        st.WarMeta.Add((st.NextQ, code));
        st.QStarts[code]++;
        var acc = PairAccOf(st, war.A, war.B);
        acc.Starts++;
    }
    internal static void WarNoteInvade(Diplomacy d) => WarOf(d).InvadeQ.Add(WarOf(d).NextQ);
    static string WarMetaCols(Diplomacy d, int i)
    {
        var st = WarOf(d);
        return i < st.WarMeta.Count ? WI(st.WarMeta[i].Q) + "\t" + WarCodeNames[st.WarMeta[i].Code] : "-1\tnone";
    }
    static string WarInvadeQ(Diplomacy d, int i) { var st = WarOf(d); return WI(i < st.InvadeQ.Count ? st.InvadeQ[i] : -1); }

    /// <summary>試験用：記録された (q, 札の番号) の並び（dip.Wars と同じ順）。</summary>
    internal static IReadOnlyList<(int Q, int Code)> WarMetaOf(Diplomacy d) => WarOf(d).WarMeta;
    internal static IReadOnlyList<int> WarInvadeQsOf(Diplomacy d) => WarOf(d).InvadeQ;

    static WarPairAcc PairAccOf(WarSt st, Power a, Power b)
    {
        long k = WarKey(a, b);
        if (!st.Pair.TryGetValue(k, out var acc)) st.Pair[k] = acc = new WarPairAcc();
        return acc;
    }
    /// <summary>組の出来事（1＝AI の屈服・2＝講和の置き直し・3＝分離の置き直し）。</summary>
    internal static void WarPairEvent(Diplomacy d, Power a, Power b, int kind)
    {
        var st = WarOf(d); var acc = PairAccOf(st, a, b);
        if (kind == 1) { acc.Submits++; st.QSubmits++; }
        else if (kind == 2) { acc.Peace++; st.QPeace++; }
        else { acc.Secession++; st.QSecession++; }
    }

    // ---------------------------------------------------------------- 月の標本 ----------------------------------------------------------------
    /// <summary>外交の月の歩の頭（または四半期の末）：全ての組の緊張を読む。四半期の最初の月（month%3==0）の頭は前の四半期の末なので読まない。</summary>
    internal static void WarPairSample(Diplomacy d, Coefficients c, int month, bool endOfQuarter)
    {
        if (!endOfQuarter && month % Calendar.MonthsPerQuarter == 0) return;
        var st = WarOf(d);
        if (WarNoPair && !endOfQuarter) return;
        double decl = c.WarDeclareTension;
        var ps = d.Powers;
        int n = ps.Count;
        if (endOfQuarter) { st.PairsSampled = 0; System.Array.Clear(st.Band); }
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                var a = ps[i]; var b = ps[j];
                double t = d.Tension(a, b);
                if (endOfQuarter)
                {
                    st.PairsSampled++;
                    st.Band[t <= 0 ? 0 : t <= 20 ? 1 : t <= 70 ? 2 : t <= 100 ? 3 : t < 140 ? 4 : 5]++;
                }
                if (!(t >= 100)) continue;
                var acc = PairAccOf(st, a, b);
                if (t > acc.MaxT) acc.MaxT = t;
                acc.Gt100++;
                if (t >= decl)
                {
                    acc.Ge140++;
                    if (d.WarGateOpen(a, b, c)) acc.Gate++;
                }
            }
    }

    // ---------------------------------------------------------------- 余剰の候補（純関数）----------------------------------------------------------------
    /// <summary>余剰戦力の候補：S_A＝landPotential−(needLand+needInternal)・S_C′＝landPotential×(1−min(1,ratio))−(needLand+needInternal)。ratio＝reach÷自陣営の陸の力（WarScale）。
    /// ratio が NaN／負は 0（届かない）、+∞は 1（全て縛られる）。</summary>
    internal static void WarSurplus(double land, double needLand, double needInternal, double ratio, out double sA, out double sC)
    {
        double need = needLand + needInternal;
        sA = land - need;
        double b = !(ratio > 0) ? 0 : ratio >= 1 ? 1 : ratio;
        sC = land * (1 - b) - need;
    }

    /// <summary>自陣営の陸の力の和（WarScale の分母）を計器が再構成する：p と、p の直接の敵のうち国を持つ相手のどれかと交戦している国（p とは交戦していない）。</summary>
    internal static double WarOwnLand(Diplomacy d, Power p)
    {
        List<Power>? foes = null;
        foreach (var war in d.Wars)
        {
            Power? f = war.A == p ? war.B : war.B == p ? war.A : null;
            if (f is null) continue;
            foes ??= new List<Power>();
            if (!foes.Contains(f)) foes.Add(f);
        }
        if (foes is null) return 0;
        var side = new List<Power> { p };
        foreach (var war in d.Wars)
        {
            Power? f0 = foes.Contains(war.A) ? war.A : foes.Contains(war.B) ? war.B : null;
            if (f0 is null || f0.Nation is null) continue;
            Power m = f0 == war.A ? war.B : war.A;
            if (m == p || side.Contains(m) || d.IsAtWar(m, p)) continue;
            side.Add(m);
        }
        double own = 0;
        foreach (var m in side) { double l = m.LandPower; if (l > 0) own += l; }
        return own;
    }

    // ---------------------------------------------------------------- 影の評価 ----------------------------------------------------------------
    /// <summary>World.CanInvade の :2190 以降の門を同じ順で純関数として評価する（弱小国の門は飛ばす）。最初に落ちた門の番号（0＝全て通った）。</summary>
    internal static int WarShadowGate(World gw, Diplomacy d, Coefficients c, Power self, Power t)
    {
        if (d.Suzerain(t) is not null) return 1;
        if (d.Suzerain(self) == t) return 2;
        if (t.Nation is null && t.ColonyList.Count == 0) return 3;
        if (d.IsAtWar(self, t)) return 4;
        if (d.DependentCount(self) >= c.MaxDependents) return 5;
        if (gw.ExpansionWill < gw.InvadeWillCost(t)) return 6;
        if (self.Military < c.InvadeMilitaryFactor * t.Military) return 7;
        if (d.IsSeaCrossing(self, t))
        {
            if (gw.InvShipStock() < gw.InvadeShipReq(t)) return 8;
            if (d.SeaControl(self, t) < c.InvadeSeaControlMin) return 9;
        }
        if (gw.InvCannedStock() < gw.InvadeCannedReq(t)) return 10;
        return 0;
    }

    static int WarReasonIndex(string reason)
    {
        for (int i = 0; i < WarReasons.Length; i++) if (WarReasons[i] == reason) return i;
        return WarReasons.Length;   // other
    }

    /// <summary>TryAiSubjugate の CanInvade の評価 1 件（Diplomacy.cs:2021 の同じ行から）。reason＝CanInvade の戻りの理由（通れば空）。</summary>
    internal static void WarEval(Diplomacy d, Coefficients c, Power great, Power target, string reason)
    {
        if (great.Nation is not { } gw) return;
        var st = WarOf(d);
        if (!st.Entry.TryGetValue(great.Id, out var acc)) st.Entry[great.Id] = acc = new WarEntryAcc();
        var cp = CpOf(d);
        long stamp = cp.Month;
        if (acc.LastStamp != stamp)
        {
            if (acc.CallShadowPass) acc.ShadowCalls++;
            acc.CallShadowPass = false;
            acc.LastStamp = stamp;
            acc.Calls++;
            WarCallSnapshot(acc, d, c, gw, great);
        }
        acc.Evals++;
        acc.Reason[WarReasonIndex(reason)]++;
        if (!d._rank.ContainsKey(target.Id)) acc.TgtRankMissing++;
        double tens = d.Tension(great, target);
        if (tens > 0) { acc.TauPos++; if (reason == "弱小国が標的") acc.TauPosWeak++; double tau = System.Math.Min(1.0, tens / c.WarDeclareTension); acc.SumTau2 += tau * tau; }
        if (d.RankOf(target) != PowerRank.Minor) acc.TgtNonMinor++;
        if (reason == "弱小国が標的")
        {
            int gate = WarShadowGate(gw, d, c, great, target);
            acc.Shadow[gate]++;
            if (gate == 0)
            {
                if (d.Projection(great, target, c) < Diplomacy.AiColonizeMinProjection) acc.ShadowProjFail++;
                else { acc.ShadowProjPass++; acc.CallShadowPass = true; }
            }
        }
    }

    static void WarCallSnapshot(WarEntryAcc acc, Diplomacy d, Coefficients c, World gw, Power great)
    {
        double trad = InterestGroups.Blocs(gw).Traditional;
        double soldiers = 0, total = 0;
        foreach (var p in gw.AllPops()) { total += p.Size; if (p.IsSoldier) soldiers += p.Size; }
        double soldierShare = total > 1e-9 ? soldiers / total : 0;
        double threat = d.MaxTension(great) / 100.0;
        double mil = PoliticalQuantities.WarFeverPublicSphere(gw);
        double outlet = PoliticalQuantities.Clamp01(trad + c.WarFeverMilitancyGate * mil);
        double diversion = c.WarFeverMilitancy * Politics.AvgMassMilitancy(gw) * outlet;
        double milSpend = PoliticalQuantities.MilitaryBudgetShare(gw);
        double land = ArmyShape.LandPotential(great), force = ArmyShape.ForcePotential(great);
        double ratio = d.WarScale(great, c, out _);
        double own = WarOwnLand(d, great);
        double reach = own > 0 && !double.IsInfinity(ratio) ? ratio * own : double.NaN;
        WarSurplus(land, great.NeedLand, great.NeedInternal, ratio, out double sA, out double sC);
        acc.FiniteCalls++;
        acc.Will += gw.ExpansionWill; acc.Fever += gw.WarFever();
        acc.FMil += mil; acc.FMilSpend += milSpend; acc.FNoble += trad; acc.FSoldier += soldierShare; acc.FThreat += threat; acc.FDiv += diversion;
        acc.Land += land; acc.Force += force; acc.NeedLand += great.NeedLand; acc.NeedInternal += great.NeedInternal; acc.NeedSea += great.NeedSea;
        acc.PowerMil += great.Military; acc.LandPower += great.LandPower; acc.Own += own;
        if (!double.IsNaN(reach)) acc.Reach += reach;
        acc.Ratio += double.IsInfinity(ratio) ? 1 : ratio;
        acc.SA += sA; acc.SC += sC;
        if (land > 1e-12) { acc.RA += sA / land; acc.RC += sC / land; acc.RCalls++; }
        if (sC < sA - 1e-12) acc.SCLess++;
    }

    // ---------------------------------------------------------------- 四半期の末 ----------------------------------------------------------------
    internal const string WarEntryHeader = "quarter\tyear\tnation\trank\tcalls\tevals\ttgtNonMinor\ttgtRankMissing\t"
        + "r_ok\tr_noDip\tr_self\tr_weak\tr_dependent\tr_suzerainOf\tr_noLand\tr_atWar\tr_maxDep\tr_will\tr_mil\tr_ship\tr_sea\tr_canned\tr_other\t"
        + "sh_pass\tsh_dependent\tsh_suzerainOf\tsh_noLand\tsh_atWar\tsh_maxDep\tsh_will\tsh_mil\tsh_ship\tsh_sea\tsh_canned\tsh_projFail\tsh_projPass\tsh_calls\t"
        + "willNow\twillPrev\twillInflowWar\twillInflowCommerce\twillDerivedUse\t"
        + "feverNow\tsumWill\tsumFever\tsumFMil\tsumFMilSpend\tsumFNoble\tsumFSoldier\tsumFThreat\tsumFDiversion\t"
        + "sumLand\tsumForce\tsumNeedLand\tsumNeedInternal\tsumNeedSea\tsumPowerMil\tsumLandPower\tsumOwn\tsumReach\tsumRatio\tsumSA\tsumSC\tsumRA\tsumRC\trCalls\tscLessCalls\ttauPos\ttauPosWeak\tsumTau2";

    internal const string WarPairHeader = "quarter\tnationA\tnationB\ttensionEnd\ttensionMax\tmonthsGe140\tmonthsGt100\twarClock\tgateOpenMonths\tstarts\tsubmits\tpeaceResets\tsecessionResets\tgrudge\thostility";
    internal const string WarBandHeader = "quarter\tpairs\tpairsWritten\tpairsSkipped\tb0\tb0_20\tb20_70\tb70_100\tb100_140\tb140\tsubmits\tpeaceResets\tsecessionResets\tstart_unknown\tstart_main\tstart_subjugate\tstart_declare\tstart_ally\tstart_suzerain\tstart_civil\tstart_ultimatum\tstart_calltoarms\tstart_mediation\tstart_secession";

    /// <summary>四半期の末（Instruments.Quarter）：最後の月の標本を取り、3 本の表に q を付けて書く。出来事はこの Quarter(q) の q に属し、NextQ を q+1 にする。</summary>
    static void WarQuarter(MultiNationSim sim, ulong seed, int q)
    {
        var d = sim.Diplomacy;
        var st = WarOf(d);
        var c = sim.Nations.Count > 0 ? sim.Nations[0].World.Coef : null;
        if (c is null) { st.NextQ = q + 1; return; }
        WarPairSample(d, c, 0, endOfQuarter: true);
        string qs = WI(q);
        if (!st.HeaderDone)
        {
            st.We = Open("warentry", seed); st.We.WriteLine(WarEntryHeader);
            st.Wp = Open("warpair", seed); st.Wp.WriteLine(WarPairHeader);
            st.Wb = Open("warband", seed); st.Wb.WriteLine(WarBandHeader);
            st.HeaderDone = true;
        }
        // warentry_s：全ての国
        foreach (var n in sim.Nations)
        {
            var w = n.World; var sp = w.SelfPower;
            int pid = sp?.Id ?? -1;
            st.Entry.TryGetValue(pid, out var acc);
            acc ??= new WarEntryAcc();
            if (acc.CallShadowPass) { acc.ShadowCalls++; acc.CallShadowPass = false; }
            double will = w.ExpansionWill;
            double adv = w.WarFever();
            double inflowWar = adv > c.WarFeverThreshold ? c.ExpansionWillGrowth * (adv - c.WarFeverThreshold) : 0;
            double inflowCom = c.ExpansionWillBourgeois > 0 ? c.ExpansionWillBourgeois * InterestGroups.Blocs(w).Modern : 0;
            double prev = acc.HavePrev ? acc.PrevWill : double.NaN;
            var sb = new System.Text.StringBuilder();
            void Add(string s) { sb.Append('\t').Append(s); }
            sb.Append(qs); Add(WI(w.Year)); Add(NameOf(w)); Add(sp is null ? "none" : d.RankOf(sp).ToString());
            Add(WI(acc.Calls)); Add(WI(acc.Evals)); Add(WI(acc.TgtNonMinor)); Add(WI(acc.TgtRankMissing));
            foreach (var v in acc.Reason) Add(WI(v));
            foreach (var v in acc.Shadow) Add(WI(v));
            Add(WI(acc.ShadowProjFail)); Add(WI(acc.ShadowProjPass)); Add(WI(acc.ShadowCalls));
            Add(F(will)); Add(F(prev)); Add(F(inflowWar)); Add(F(inflowCom)); Add(F(prev + inflowWar + inflowCom - will));
            Add(F(adv));
            Add(F(acc.Will)); Add(F(acc.Fever)); Add(F(acc.FMil)); Add(F(acc.FMilSpend)); Add(F(acc.FNoble)); Add(F(acc.FSoldier)); Add(F(acc.FThreat)); Add(F(acc.FDiv));
            Add(F(acc.Land)); Add(F(acc.Force)); Add(F(acc.NeedLand)); Add(F(acc.NeedInternal)); Add(F(acc.NeedSea)); Add(F(acc.PowerMil)); Add(F(acc.LandPower)); Add(F(acc.Own)); Add(F(acc.Reach)); Add(F(acc.Ratio));
            Add(F(acc.SA)); Add(F(acc.SC)); Add(F(acc.RA)); Add(F(acc.RC)); Add(WI(acc.RCalls)); Add(WI(acc.SCLess)); Add(WI(acc.TauPos)); Add(WI(acc.TauPosWeak)); Add(F(acc.SumTau2));
            st.We!.WriteLine(sb.ToString());
            // 次の四半期へ
            double pw = will;
            st.Entry[pid] = new WarEntryAcc { PrevWill = pw, HavePrev = true, LastStamp = acc.LastStamp };
        }
        // warpair_s：月の最大が 100 以上か出来事のあった組だけ
        int written = 0;
        var byId = new Dictionary<int, Power>();
        foreach (var p in d.Powers) byId[p.Id] = p;
        if (!WarNoPair)
            foreach (var kv in st.Pair)
            {
                var acc = kv.Value;
                if (!(acc.MaxT >= 100) && acc.Starts + acc.Submits + acc.Peace + acc.Secession == 0) continue;
                int hi = (int)(kv.Key >> 32), lo = (int)(uint)kv.Key;
                if (!byId.TryGetValue(lo, out var a) || !byId.TryGetValue(hi, out var b)) continue;   // 削除された力はもう読めない
                double h = ArmyShape.Hostility(d, a, b, c);
                st.Wp!.WriteLine(string.Join('\t', qs, NameOf(a), NameOf(b), F(d.Tension(a, b)), F(acc.MaxT), WI(acc.Ge140), WI(acc.Gt100), WI(d._warClock.GetValueOrDefault(kv.Key)), WI(acc.Gate),
                    WI(acc.Starts), WI(acc.Submits), WI(acc.Peace), WI(acc.Secession), F(d._grudge.GetValueOrDefault(kv.Key)), F(h)));
                written++;
            }
        var bd = st.Band;
        st.Wb!.WriteLine(string.Join('\t', qs, WI(st.PairsSampled), WI(written), WI(st.PairsSampled - written), WI(bd[0]), WI(bd[1]), WI(bd[2]), WI(bd[3]), WI(bd[4]), WI(bd[5]),
            WI(st.QSubmits), WI(st.QPeace), WI(st.QSecession),
            WI(st.QStarts[0]), WI(st.QStarts[1]), WI(st.QStarts[2]), WI(st.QStarts[3]), WI(st.QStarts[4]), WI(st.QStarts[5]), WI(st.QStarts[6]), WI(st.QStarts[7]), WI(st.QStarts[8]), WI(st.QStarts[9]), WI(st.QStarts[10])));
        st.Pair.Clear(); st.QSubmits = st.QPeace = st.QSecession = 0; System.Array.Clear(st.QStarts);
        st.NextQ = q + 1;
    }

    static void WarClose(MultiNationSim sim)
    {
        if (!WarSts.TryGetValue(sim.Diplomacy, out var st)) return;
        st.We?.Dispose(); st.We = null; st.Wp?.Dispose(); st.Wp = null; st.Wb?.Dispose(); st.Wb = null;
    }
}
