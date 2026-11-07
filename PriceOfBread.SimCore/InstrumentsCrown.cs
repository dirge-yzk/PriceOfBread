namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (889) 王権と議会の点検の恒等の計器 crownpc_s（読むだけ・恒等・事前登録 measurements/m889_20261106_prereg_cp.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCornint.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（計器の持ち物は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝議会の統制力 ParliamentControl（PC）が四半期ごとにどの項で動いたかを、エンジン（CrownParliament.Step・Politics.cs:512 から）に 1 行も足さずに数え直す。
// 取り方＝前の四半期の末の断面（PC・政体・王冠・奪取・協力度・軍の帰属・政変の記憶・下賜の回数）を計器が貯め、今の四半期の末に Step の順で項を当て直す。
//   召集 → 巻き返し →（奪取なしのとき）① 議会の圧力 → ② 王権の圧力 → ②′ 軍の圧力 →（廃位・幕府の終わりの床・再ラベルは印だけ）。項＝走る値に当てた後−当てる前。
//   式（門・項・切り）は計器が書く（CrownParliament の関数を呼ばない）。思想の支持はエンジンの純関数（Ideologies.National・SupportAt・BasePolityCoord・PoliticalQuantities.DriftedPolityCoord）を読む＝世界を書かない。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元・Flag＝0／1・Code＝整数の区分・PerMonth＝月あたりの流れ）：事前登録の〈列の式〉のとおり。
// ⚠限界（計器が自己申告する）：
//   ①四半期の末の断面＝Step の後の Meritocracy（社会移動）・共有層（講和・移民・内戦の決着・建国・思想の伝播）が動かす入力（正統性・思想の集計・兵士の規模・人口・参政権）は、Step の瞬間の値と違いうる
//     ＝①・②・②′・巻き返しの数え直しがずれうる。ずれた行は residualShare≠0・explainCode 9 と driftMaskCode で名乗る（隠さない）。召集は四半期の頭の Austerity と前の断面だけを読む＝ずれない。
//   ②最初の行（q0・建国の最初の四半期）は前の断面が無い＝項と残差は nan（explainCode 1）。
//   ③下賜（Politics.cs:680）の値はソースから数え直せない（挑戦の比が Transition の局所変数）＝grantFlag の印だけ（explainCode 6）。
//   ④政変（ApplyRegime）は RecentRegimeChanges の増え＞0.5 で印を付ける（1 回で +1・Step は ×(1−置き換わり率) だけ）。Step の前か後かは residualAlt と目標の一致で分ける（explainCode 3・4・5）。
//   ⑤(896) 旗 SummonByInterestShare＞0 の召集は四半期の末の利払い÷歳入と DebtGrip（計器が自分で書く式）を読む。エンジンは Politics.Step（QuarterEnd）で読む＝その後の共有層（割譲・講和・州の移り）で
//     国債か名目 GDP（getter）が動けば押す先がずれる＝旗 1 では召集の列もずれうる（〈召集はずれない〉は旗 0 だけ）。ずれた行は explainCode 9 と driftMaskCode の 16（旗 1 で召集の項≠0）で名乗る。
public static partial class Instruments
{
    /// <summary>前の四半期の末の断面（計器の持ち物・World に欄を足さない）。</summary>
    internal sealed class CrownPrev
    {
        public double Pc, Coop, Army, Rrc;
        public Polity Polity; public CrownForm Crown; public SeizureForm Seizure;
        public int Grants;
    }
    sealed class CrownSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, CrownPrev> CrownPrevs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CrownSink> CrownSinks = new();

    // ---- (928) 王冠の登録簿を最後に書いた口の符号 crownRegisterSourceCode（計器の持ち物・World に欄を足さない・セーブに載らない） ----
    // 符号：0 開幕（Simulation の構築）・1 Transition 系の政体変更（Politics.ExecuteRegimeChange＝Transition と蜂起の決着のうち蜂起以外）・2 矯正講和・3 蜂起の決着（Politics の反乱の勝ち）・
    //   4 廃位（議会が王冠を廃す）・5 幕府の終わり・6 不明（口が印を付けていない国＝助走の後の復元〔GovernanceSnapshot〕・建国の World など）。
    // ⚠限界：①符号は呼び口が付ける印＝口が増えても印が無ければ前の符号のまま。②ExecuteRegimeChange の呼び手のうち Transition と蜂起の決着だけ分けた（他に呼び手は無いと grep で確かめた）。
    internal const int CrownSrcUnknown = 6;
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, System.Runtime.CompilerServices.StrongBox<int>> CrownSrcs = new();
    internal static void CrownSource(World w, int code)
    {
        if (CrownSrcs.TryGetValue(w, out var box)) box.Value = code; else CrownSrcs.Add(w, new System.Runtime.CompilerServices.StrongBox<int>(code));
    }
    /// <summary>On の判定を中で持つ版（同じ関数に呼び口が 2 本ある CrownParliament.Step 用＝呼び側で On を 2 回読まない）。</summary>
    internal static void CrownSourceIfOn(World w, int code) { if (On) CrownSource(w, code); }
    internal static int CrownSourceOf(World w) => CrownSrcs.TryGetValue(w, out var box) ? box.Value : CrownSrcUnknown;

    /// <summary>断面を取る（純粋な読み）。</summary>
    internal static CrownPrev CrownSnap(World w) => new()
    {
        Pc = w.ParliamentControl, Coop = w.CoopParliamentCrown, Army = w.ArmyAllegiance, Rrc = w.RecentRegimeChanges,
        Polity = w.Polity, Crown = w.Crown, Seizure = w.Seizure, Grants = w.ReformGrantCount,
    };

    /// <summary>政体→議会の統制力の目標（CrownParliament.ParliamentControlTarget の写し・初期化と ApplyRegime の跳びの先）。</summary>
    internal static double CrownTarget(Polity p) => p switch
    {
        Polity.Republic => 1.0,
        Polity.ConstitutionalMonarchy => 0.65,
        Polity.BourgeoisDictatorship => 0.30,
        Polity.ProletarianDictatorship => 0.20,
        Polity.MilitaryDictatorship => 0.10,
        _ => 0.0,
    };

    /// <summary>四半期の末に読む入力（Step の順の当て直しが読む物）。</summary>
    internal struct CrownInputs
    {
        public double Austerity, Legitimacy, Rep, CurPrev, CurNow, CrownS, Inertia, Mobilization, Coerce, Lean, SoldierShare;
        public double InterestShare, Dg;   // (896) 旗 1 の召集の入力：利払い÷歳入（歳入が正でなければ 0）・DebtGrip（計器の式）
    }

    /// <summary>当て直しの結果（1 本の始まりの値から）。</summary>
    internal struct CrownRun
    {
        public double Start, Summon, Rollback, Term1, Term2, Term2p, End, ArmyPower;
        public bool SummonClamp, RbCrown, RbCoop, RbLeg, RbArmy, BlockOpen, T1Support, T1Coerce, T1Clamp, T2Gate, T2Zero, T2pGate, T2pZero, RbZero;
    }

    /// <summary>四半期の末の入力を読む（エンジンの純関数で思想の支持・計器が書く式で兵士の傾き・保持力）。</summary>
    internal static CrownInputs CrownGather(World w, Coefficients c, Polity polityPrev)
    {
        var x = new CrownInputs { Austerity = w.AusterityIntensity, Legitimacy = w.Legitimacy };
        double revS = w.LastRevenuePerMonth.Value;
        x.InterestShare = !(revS > 0) ? 0 : w.LastInterestPerMonth.Value / revS;
        double gdpYearS = Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value;
        x.Dg = !(gdpYearS > 0) ? 0 : RuleClamp01(System.Math.Max(0, w.GovDebt.Value) / gdpYearS);
        var agg = Ideologies.National(w);
        double S(Polity p) => Ideologies.SupportAt(agg, Ideologies.BasePolityCoord(p), c.IdeologyAffinitySlope);
        x.Rep = System.Math.Max(S(Polity.ConstitutionalMonarchy), S(Polity.Republic));
        x.CrownS = System.Math.Max(S(Polity.Absolutist), System.Math.Max(S(Polity.Despotic), S(Polity.Shogunate)));
        x.CurPrev = Ideologies.SupportAt(agg, PoliticalQuantities.DriftedPolityCoord(polityPrev, w.Suffrage, c), c.IdeologyAffinitySlope);
        x.CurNow = Ideologies.SupportAt(agg, PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
        x.Inertia = c.PolityInertia * w.Legitimacy;
        x.Mobilization = w.ReformistPower + w.RadicalPower;
        double pop = w.TotalPopulation;
        x.Coerce = w.Coercion * (pop / System.Math.Max(1.0, c.PoliticalReferencePop));
        double sum = 0, size = 0, soldierK = 0;
        foreach (var p in w.AllPops())
        {
            if (!p.IsSoldier) continue;
            var s = p.IdeologyShares;
            double crown = 0, anti = 0;
            for (int i = 0; i < Ideologies.Count; i++)
                if (PoliticalQuantities.FamilyOf(i) == 0) crown += s[i]; else anti += s[i];
            sum += (crown - anti) * p.Size;
            size += p.Size;
        }
        foreach (var (_, p) in w.AllPopsWithRegion()) if (p.IsSoldier) soldierK += p.Size;
        x.Lean = size > 1e-9 ? sum / size : 0.0;
        x.SoldierShare = soldierK / System.Math.Max(1.0, pop);
        return x;
    }

    /// <summary>Step の順の当て直し（純粋な関数）。start＝始まりの PC・crown／seizure／coop／army＝Step の瞬間の値（前の断面）・cur＝その政体の支持。</summary>
    internal static CrownRun CrownApply(in CrownInputs x, Coefficients c, double start, Polity polityAtStep, CrownForm crownAtStep, SeizureForm seizureAtStep, double coop, double army, double cur)
    {
        var r = new CrownRun { Start = start };
        double pc = start;
        if (pc < 0) pc = CrownTarget(polityAtStep);   // :26 未捕捉の初期化
        double before = pc;
        if (c.SummonParliamentRate > 0 && c.SummonByInterestShare > 0)
        {
            double s = RuleClamp01(x.InterestShare / c.MaxInterestShareOfRevenue.Value);   // (896) 旗 1：比例・押す先 DebtGrip・切りなし（SummonClamp は立たない）
            pc = pc + c.SummonParliamentRate * s * System.Math.Max(0, x.Dg - pc);
        }
        else if (c.SummonParliamentRate > 0 && x.Austerity > 0.5)
        {
            double raw = pc + c.SummonParliamentRate * (x.Austerity - 0.5) * 2.0;
            r.SummonClamp = raw > 1;
            pc = RuleClamp01(raw);
        }
        r.Summon = pc - before; before = pc;
        r.RbCrown = crownAtStep != CrownForm.None; r.RbCoop = coop < -0.5; r.RbLeg = x.Legitimacy > CrownParliament.CrownRollbackLegitimacyMin; r.RbArmy = army > CrownParliament.CrownRollbackArmyAllegianceMin;
        if (c.SummonParliamentRate > 0 && r.RbCrown && r.RbCoop && r.RbLeg && r.RbArmy)
        {
            r.RbZero = pc - c.SummonParliamentRate < 0;
            pc = System.Math.Max(0, pc - c.SummonParliamentRate);
        }
        r.Rollback = pc - before; before = pc;
        r.BlockOpen = seizureAtStep == SeizureForm.None;
        r.ArmyPower = double.NaN;
        if (r.BlockOpen)
        {
            r.T1Support = x.Rep - cur > x.Inertia;
            r.T1Coerce = x.Mobilization > x.Coerce;
            if (c.ParliamentPressureRate > 0 && r.T1Support && r.T1Coerce)
            {
                double raw = pc + c.ParliamentPressureRate * (x.Rep - cur);
                r.T1Clamp = raw > 1;
                pc = RuleClamp01(raw);
            }
            r.Term1 = pc - before; before = pc;
            r.T2Gate = crownAtStep != CrownForm.None && x.CrownS > x.Rep;
            if (c.ParliamentPressureRate > 0 && r.T2Gate)
            {
                double raw = pc - c.ParliamentPressureRate * (x.CrownS - x.Rep) * x.Legitimacy;
                r.T2Zero = raw < 0;
                pc = System.Math.Max(0, raw);
            }
            r.Term2 = pc - before; before = pc;
            r.ArmyPower = c.ArmyPoliticalWeight <= 0 ? 0 : c.ArmyPoliticalWeight * x.SoldierShare * (1.0 - RuleClamp01(pc));
            r.T2pGate = x.Lean > 0;
            if (c.ArmyPcPressure > 0 && r.T2pGate)
            {
                double raw = pc - c.ArmyPcPressure * x.Lean * r.ArmyPower;
                r.T2pZero = raw < 0;
                pc = System.Math.Max(0, raw);
            }
            r.Term2p = pc - before;
        }
        r.End = pc;
        return r;
    }

    /// <summary>1 行（前の断面が無ければ prev＝null＝最初の行）。</summary>
    internal sealed class CrownRow
    {
        public CrownPrev? Prev; public CrownInputs X; public CrownRun Run;
        public double PcNow, Residual = double.NaN, ResidualAlt = double.NaN;
        public bool RegimeEvent, Grant, Deposition, ShogunateEnd, FloorHit, Relabel, PoliticsOn;
        public int Explain, DriftMask;
    }

    /// <summary>四半期の末の 1 国の行を数える（純粋な読み・prev は前の四半期の末の断面）。</summary>
    internal static CrownRow CrownCompute(World w, CrownPrev? prev)
    {
        var c = w.Coef;
        var row = new CrownRow { Prev = prev, PcNow = w.ParliamentControl, PoliticsOn = w.PoliticsEnabled };
        row.X = CrownGather(w, c, prev?.Polity ?? w.Polity);
        if (prev is null) { row.Explain = 1; row.Run = new CrownRun { Start = double.NaN, Summon = double.NaN, Rollback = double.NaN, Term1 = double.NaN, Term2 = double.NaN, Term2p = double.NaN, End = double.NaN, ArmyPower = double.NaN }; return row; }
        row.RegimeEvent = w.RecentRegimeChanges - prev.Rrc > 0.5;
        row.Grant = w.ReformGrantCount > prev.Grants;
        if (!row.PoliticsOn) row.Run = new CrownRun { Start = prev.Pc, End = prev.Pc, ArmyPower = double.NaN };   // Politics.cs:199 の早い return＝PC は動かない
        else row.Run = CrownApply(row.X, c, prev.Pc, prev.Polity, prev.Crown, prev.Seizure, prev.Coop, prev.Army, row.X.CurPrev);
        if (!row.RegimeEvent)
        {
            row.Deposition = prev.Crown != CrownForm.None && w.Crown == CrownForm.None;
            row.ShogunateEnd = prev.Crown == CrownForm.Shogunate && w.Crown == CrownForm.Absolutist;
            row.Relabel = prev.Polity != w.Polity;
        }
        double end = row.Run.End;
        if (row.ShogunateEnd && end < c.ConstitutionalControlFloor && row.PcNow == c.ConstitutionalControlFloor) { row.FloorHit = true; end = c.ConstitutionalControlFloor; }   // :159
        row.Residual = row.PcNow - end;
        if (row.RegimeEvent && row.PoliticsOn)
        {
            var alt = CrownApply(row.X, c, CrownTarget(w.Polity), w.Polity, w.Crown, w.Seizure, prev.Coop, prev.Army, row.X.CurNow);
            row.ResidualAlt = row.PcNow - alt.End;
        }
        const double tol = 1e-12;
        if (System.Math.Abs(row.Residual) <= tol) row.Explain = 0;
        else if (row.RegimeEvent) row.Explain = row.PcNow == CrownTarget(w.Polity) ? 3 : System.Math.Abs(row.ResidualAlt) <= tol ? 4 : 5;
        else if (row.FloorHit) row.Explain = 7;
        else if (row.Grant) row.Explain = 6;
        else row.Explain = 9;
        if (row.Explain == 9)
        {
            var r = row.Run;
            row.DriftMask = (r.BlockOpen && r.T1Support && r.T1Coerce ? 1 : 0) | (r.BlockOpen && r.T2Gate ? 2 : 0) | (r.BlockOpen && r.T2pGate ? 4 : 0) | (r.RbCrown && r.RbCoop && r.RbLeg && r.RbArmy ? 8 : 0) | (w.Coef.SummonByInterestShare > 0 && r.Summon != 0 ? 16 : 0);
        }
        return row;
    }

    internal const string CrownHeaderText = "quarter\tyear\tnation\tnationUid\tpolityCode\tcrownCode\tseizureCode\tpolityPrevCode\tcrownPrevCode\tseizurePrevCode\tpoliticsEnabledFlag\tfirstRowFlag\t"
        + "pcPrevShare\tpcNowShare\tpcEmulShare\tresidualShare\tresidualAltShare\texplainCode\tdriftMaskCode\t"
        + "austerityIndex\tsummonTermShare\tsummonClampHitFlag\t"
        + "coopParliamentCrownPrevIndex\tlegitimacyIndex\tarmyAllegiancePrevIndex\trollbackCrownGateFlag\trollbackCoopGateFlag\trollbackLegitimacyGateFlag\trollbackArmyGateFlag\trollbackTermShare\trollbackZeroHitFlag\t"
        + "blockOpenFlag\trepSupportIndex\tcurSupportIndex\tcrownSupportIndex\tinertiaIndex\tmobilizationIndex\teffectiveCoerceIndex\tterm1SupportGateFlag\tterm1CoerceGateFlag\tterm1Share\tterm1ClampHitFlag\t"
        + "term2GateFlag\tterm2Share\tterm2ZeroHitFlag\tsoldierLeanIndex\tarmyPowerIndex\tterm2pGateFlag\tterm2pShare\tterm2pZeroHitFlag\t"
        + "depositionFlag\tshogunateEndFlag\tfloorHitFlag\trelabelFlag\tregimeEventFlag\tgrantFlag\tpcTargetShare\t"
        + "debtGripShare\tinterestPerMonth\trevenuePerMonth\tinterestPerRevenueShare\taboveConstitutionalFloorFlag\trecentRegimeChangesIndex	crownRegisterSourceCode";

    /// <summary>行の値（見出し <see cref="CrownHeaderText"/> と同じ順）。</summary>
    internal static string CrownLine(World w, int q, int uid, CrownRow row)
    {
        var c = w.Coef; var p = row.Prev; var r = row.Run; var x = row.X;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string B(bool v) => v ? "1" : "0";
        string Bp(bool v) => p is null ? "nan" : B(v);
        bool first = p is null;
        double gdpYear = Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value;
        double debtGrip = !(gdpYear > 0) ? 0 : RuleClamp01(System.Math.Max(0, w.GovDebt.Value) / gdpYear);
        double interest = w.LastInterestPerMonth.Value, revenue = w.LastRevenuePerMonth.Value;
        return string.Join('\t',
            I(q), I(w.Year), NameOf(w), I(uid), I((int)w.Polity), I((int)w.Crown), I((int)w.Seizure),
            first ? "nan" : I((int)p!.Polity), first ? "nan" : I((int)p!.Crown), first ? "nan" : I((int)p!.Seizure), B(row.PoliticsOn), B(first),
            F(first ? double.NaN : p!.Pc), F(row.PcNow), F(r.End), F(row.Residual), F(row.ResidualAlt), I(row.Explain), I(row.DriftMask),
            F(x.Austerity), F(r.Summon), Bp(r.SummonClamp),
            F(first ? double.NaN : p!.Coop), F(x.Legitimacy), F(first ? double.NaN : p!.Army), Bp(r.RbCrown), Bp(r.RbCoop), Bp(r.RbLeg), Bp(r.RbArmy), F(r.Rollback), Bp(r.RbZero),
            Bp(r.BlockOpen), F(x.Rep), F(first ? double.NaN : x.CurPrev), F(x.CrownS), F(x.Inertia), F(x.Mobilization), F(x.Coerce), Bp(r.T1Support), Bp(r.T1Coerce), F(r.Term1), Bp(r.T1Clamp),
            Bp(r.T2Gate), F(r.Term2), Bp(r.T2Zero), F(x.Lean), F(r.ArmyPower), Bp(r.T2pGate), F(r.Term2p), Bp(r.T2pZero),
            Bp(row.Deposition), Bp(row.ShogunateEnd), Bp(row.FloorHit), Bp(row.Relabel), Bp(row.RegimeEvent), Bp(row.Grant), F(CrownTarget(w.Polity)),
            F(debtGrip), F(interest), F(revenue), F(revenue > 0 ? interest / revenue : double.NaN), B(row.PcNow >= c.ConstitutionalControlFloor), F(w.RecentRegimeChanges), I(CrownSourceOf(w)));
    }

    /// <summary>四半期の末（<see cref="Quarter"/> の国ごとの段）：1 国の行を書き、次の四半期のために断面を置き直す（州が 0 の国は行を出さないが断面は置く）。</summary>
    static void CrownFold(MultiNationSim sim, ulong seed, World w, int q)
    {
        CrownPrevs.TryGetValue(w, out var prev);
        if (w.Regions.Count > 0)
        {
            var sk = CrownSinks.GetValue(sim, _ => new CrownSink());
            sk.W ??= Open("crownpc", seed);
            if (!sk.Header) { sk.W.WriteLine(CrownHeaderText); sk.Header = true; }
            sk.W.WriteLine(CrownLine(w, q, S(w).Uid, CrownCompute(w, prev)));
        }
        CrownPrevs.AddOrUpdate(w, CrownSnap(w));
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CrownClose(MultiNationSim sim)
    {
        if (CrownSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
