namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1090) 王権と議会の段 P1：中心の影の計器 center_s（読むだけ・恒等・事前登録 measurements/m1090_20261113_prereg_cp.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCrown.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（計器の持ち物は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 式は純関数 PowerCenter が持つ（段 P2 が同じ関数を呼ぶ）。ここは World から数の束を作る読み手と、表の書き手だけ。
// 読み手の規則：Politics.MeasureMobilization と Ideologies.UpdateShares は呼ばない（w.ReformistPower／RadicalPower を書く）＝動員の式は POP ごとに写す。
// 既存の crownpc_s（議会の統制力の項ごとの寄与）は重ねて作らず、比べる列として読む。
// ⚠限界：事前登録 [0] ①〜⑥（K の開幕は今の政体の写像・K±1 の門は未計算・X_V の固定・T_年÷R の代理・DefectionShare は簡易の引力点・関税の露出は 0）。
//   四半期の末の断面（Step の後の共有層の動きを含む）で読む。q（人と兵の換算）は計器が最初の観測の四半期に 1 回だけ作る（支配の力の計器とは独立に同じ定義で組む）。
public static partial class Instruments
{
    /// <summary>計器の仕事の回数（番人が〈計器が切れている時は増えない〉を数える・純カウンタ）。</summary>
    internal static long CenterWorkCount;

    sealed class CenterSink { public System.IO.StreamWriter? W; public bool Header; public bool QInit; public double Q; public System.IO.StreamWriter? P; public bool PHeader; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CenterSink> CenterSinks = new();

    internal const string CenterHeaderText = "quarter\tyear\tnation\tlandPowerIndex\tfrontPowerIndex\tgMinusWIndex\tgHIndex\tqIndex\tpopK\t"
        + "loyalShare\tarmyShare\trevoltShare\tdefectionOldShare\tpushIndex\tvalveIndex\tautonomyIndex\t"
        + "walletInterestShare\twalletTaxYearShare\twalletShare\t"
        + "mRadIndex\tmRefOutIndex\tmRefOutClosedIndex\tmRefOutEliteIndex\tmRefOutMassIndex\t"
        + "xvParticipation\txvAuthority\txvEconomy\txvArmy\t"
        + "forceExecIndex\tforcePartIndex\tforceArmyIndex\tforceRevIndex\tsumForceIndex\tsumCheckIndex\t"
        + "shareExec\tsharePart\tshareArmy\tshareRev\tshareRegime\tnuIndex\tnuCoupIndex\t"
        + "ignitionViolentFlag\tignitionPeacefulFlag\timmediateFlag\tcoupFlag\tinCivilWarFlag\tpendingRebelFlag\t"
        + "kCode\tkSourceCode\tcrownCode\tseizureCode\tpolityCode\tnameCode\tderivedPolityCode\t"
        + "safetyKMinusIndex\tsafetyKIndex\tsafetyKPlusIndex\tkStepGateCode\tradicalToReformIndex\tcrowdToGIndex";

    static Suffrage SuffrageStep(Suffrage s, int d) => (Suffrage)System.Math.Clamp((int)s + d, (int)Suffrage.None, (int)Suffrage.Universal);

    /// <summary>四半期の末（<see cref="Quarter"/> の国ごとの段）：1 国の行を書く。</summary>
    static void CenterFold(MultiNationSim sim, ulong seed, World w, int q)
    {
        if (w.Regions.Count == 0 || w.WarmupFrozen || w.SelfPower is not { } sp) return;
        CenterWorkCount++;
        var sk = CenterSinks.GetValue(sim, _ => new CenterSink());
        var c = w.Coef;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!sk.QInit)
        {
            double totL = 0, totPop = 0;
            foreach (var sm in sim.Nations)
            {
                var wn = sm.World; if (wn.SelfPower is not { } pn || wn.Regions.Count == 0) continue;
                totL += System.Math.Max(0, pn.LandPower);
                foreach (var r in wn.Regions) totPop += System.Math.Max(0, r.SumPopulation());
            }
            if (!(totL > 0 && totPop > 0)) return;
            sk.Q = totL / totPop; sk.QInit = true;   // q＝開幕の世界の陸軍力の和÷人口の和（最初の観測の四半期に 1 回）
        }
        double qv = sk.Q;
        // ---- L・W・G・H ----
        var dip = sim.Diplomacy;
        double lp = System.Math.Max(0, sp.LandPower), wa = 0;
        var opp = new List<Power>();
        foreach (var wr in dip.Wars) { if (ReferenceEquals(wr.A, sp)) opp.Add(wr.B); else if (ReferenceEquals(wr.B, sp)) opp.Add(wr.A); }
        int fronts = System.Math.Max(1, opp.Count);
        foreach (var y in opp)
        {
            int yf = 0; foreach (var wr in dip.Wars) if (ReferenceEquals(wr.A, y) || ReferenceEquals(wr.B, y)) yf++;
            double ps = dip.FrontPush(sp, y, c, fronts), po = dip.FrontPush(y, sp, c, System.Math.Max(1, yf));
            double ratio = ps > 0 ? System.Math.Min(1.0, po / ps) : (po > 0 ? 1.0 : 0.0);
            wa += lp / fronts * ratio;
        }
        double wUse = System.Math.Min(wa, lp);
        double popK = 0; foreach (var r in w.Regions) popK += System.Math.Max(0, r.SumPopulation());
        double g = PowerCenter.GOf(lp, wUse), h = PowerCenter.HOf(lp, wUse, popK * qv);
        // ---- 兵の分かれ ----
        var aggS = Ideologies.Soldiers(w, out double soldierW);
        var curCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        var armyCoord = Ideologies.BasePolityCoord(Polity.MilitaryDictatorship);
        // ---- 動員（MeasureMobilization の式を POP ごとに写す・兵は除く）と票の道 ----
        var kNow = PowerCenter.KFromPolity(w.Polity);
        double unionMob = c.UnionMobilization * Politics.UnionLegality(w.LaborUnion);
        double hope = 1.0 / System.Math.Max(1.0, w.RecentRegimeChanges);
        double boost = 1 + c.ReformPressureBoost * w.ExternalPressure;
        double mRad = 0, mClosed = 0, mElite = 0, mMass = 0, mUp = 0, mDown = 0;
        var xvMass = new double[Ideologies.Count]; double xvTot = 0;
        var sufUp = SuffrageStep(w.Suffrage, +1); var sufDown = SuffrageStep(w.Suffrage, -1);
        foreach (var p in w.AllPops())
        {
            if (p.IsSoldier) continue;
            double core = p.CoreEthnicShare.Value;
            double reform = p.Size * p.Consciousness * (1 - p.Militancy);
            if (unionMob > 0 && Politics.IsMassPop(p)) reform += p.Size * p.Consciousness * unionMob;
            double whoActs = 1.0 - c.DesperationConsciousnessGate * p.Consciousness;
            double rad = p.Size * p.Consciousness * p.Militancy + p.Size * System.Math.Max(0, p.Militancy - c.RiotMilitancyThreshold) * c.DesperationChallengeWeight * whoActs;
            double radW = core * rad * hope, refW = core * reform * boost * hope;
            double path = kNow == BudgetLaw.Decree ? 0.0 : InterestGroups.FranchiseVoice(p, w.Suffrage, c);
            double pathUp = kNow == BudgetLaw.Parliamentary ? path : InterestGroups.FranchiseVoice(p, sufUp, c);   // K＋1（勅令→部分・部分→議会）は票の道が開く側（勅令でなくなる）
            double pathDown = kNow == BudgetLaw.Decree ? 0.0 : (kNow == BudgetLaw.Partial ? 0.0 : InterestGroups.FranchiseVoice(p, sufDown, c));
            double refOut = refW * (1.0 - path);
            mRad += radW;
            if (p.Closed) mClosed += refOut; else if (p.IsModernElite) mElite += refOut; else mMass += refOut;
            mUp += refW * (1.0 - pathUp); mDown += refW * (1.0 - pathDown);
            double wt = radW + refOut;
            if (wt > 0) { var s = p.IdeologyShares; for (int k = 0; k < Ideologies.Count; k++) xvMass[k] += s[k] * wt; xvTot += wt; }
        }
        double mRefOut = mClosed + mElite + mMass;
        var xv = xvTot > 0 ? Ideologies.IdealPoint(xvMass) : curCoord;
        // ---- 親和と兵の分かれ ----
        var affX = new double[Ideologies.Count]; var affA = new double[Ideologies.Count]; var affV = new double[Ideologies.Count];
        var unit = new double[Ideologies.Count];
        for (int k = 0; k < Ideologies.Count; k++)
        {
            System.Array.Clear(unit); unit[k] = 1.0;
            affX[k] = Ideologies.SupportAt(unit, curCoord, c.IdeologyAffinitySlope);
            affA[k] = Ideologies.SupportAt(unit, armyCoord, c.IdeologyAffinitySlope);
            affV[k] = Ideologies.SupportAt(unit, xv, c.IdeologyAffinitySlope);
        }
        var (dl, da, dv) = PowerCenter.SoldierSplit(aggS, affX, affA, affV);
        // 前の版の DefectionShare（簡易の引力点＝国民の集計の支持が最大の政体・限界⑤）
        var agg = Ideologies.National(w);
        Polity att = w.Polity; double best = double.NegativeInfinity;
        foreach (var pol in Ideologies.Polities)
        {
            double sv = Ideologies.SupportAt(agg, pol == w.Polity ? curCoord : Ideologies.BasePolityCoord(pol), c.IdeologyAffinitySlope);
            if (sv > best) { best = sv; att = pol; }
        }
        double defOld = att == w.Polity ? 0.0 : Ideologies.DefectionShare(aggS, curCoord, Ideologies.BasePolityCoord(att), c.IdeologyAffinitySlope);
        // ---- 財布・押し・評価 ----
        double rev = w.LastRevenuePerMonth.Value;
        bool revPos = rev > 0;
        double iShare = revPos ? w.LastInterestPerMonth.Value / rev : 0.0;
        double tShare = revPos ? (w.LastTaxIncomePerMonth.Value + w.LastTaxProgressivePerMonth.Value) / rev : 0.0;
        double push = PoliticalQuantities.MilitaryPush(w);
        var inp = new PowerCenter.Inputs(g, qv, mRad, mRefOut, dl, da, dv, push, iShare, tShare, revPos);
        var res = PowerCenter.Evaluate(inp, kNow);
        var ver = PowerCenter.Decide(inp, res, c.CivilWarLandslideRatio);
        double sfUp = double.NaN, sfDown = double.NaN;
        if (kNow != BudgetLaw.Parliamentary) sfUp = PowerCenter.Evaluate(inp with { MRefOut = mUp }, kNow + 1).Safety;
        if (kNow != BudgetLaw.Decree) sfDown = PowerCenter.Evaluate(inp with { MRefOut = mDown }, kNow - 1).Safety;
        var nm = PowerCenter.NameOf(kNow, w.Crown, w.Seizure);
        double radSide = g * dv + qv * mRad, refSide = qv * mRefOut;
        string B(bool v) => v ? "1" : "0";
        sk.W ??= Open("center", seed);
        if (!sk.Header) { sk.W.WriteLine(CenterHeaderText); sk.Header = true; }
        sk.W.WriteLine(string.Join('\t',
            I(q), I(w.Year), NameOf(w), F(lp), F(wUse), F(g), F(h), F(qv), F(popK),
            F(dl), F(da), F(dv), F(defOld), F(push), F(res.ValveIndex), F(res.AutonomyIndex),
            F(iShare), F(tShare), F(res.Wallet),
            F(mRad), F(mRefOut), F(mClosed), F(mElite), F(mMass),
            F(xv[0]), F(xv[1]), F(xv[2]), F(xv[3]),
            F(res.FExec), F(res.FPart), F(res.FArmy), F(res.FRev), F(res.Sum), F(res.SumParts - res.Sum),
            F(res.SExec), F(res.SPart), F(res.SArmy), F(res.SRev), F(res.SRegime), F(ver.Nu), F(ver.NuCoup),
            B(ver.Violent), B(ver.Peaceful), B(ver.Immediate), B(ver.Coup), B(w.InCivilWar), B(w.PendingRebelRegions is not null),
            I((int)kNow), "0", I((int)w.Crown), I((int)w.Seizure), I((int)w.Polity), I((int)nm), I((int)PowerAffinity.PolityFromShares(w)),
            F(sfDown), F(res.Safety), F(sfUp), "-1", F(refSide > 0 ? radSide / refSide : double.NaN), F(g > 0 ? qv * (mRad + mRefOut) / g : double.NaN)));
        PopPrefFold(sk, seed, w, q);   // (1090) 段 B1：POP の好みの影 poppref_s（四半期 0 と 40 おきだけ）
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CenterClose(MultiNationSim sim)
    {
        if (CenterSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.P?.Dispose(); sk.P = null; }
    }
}
