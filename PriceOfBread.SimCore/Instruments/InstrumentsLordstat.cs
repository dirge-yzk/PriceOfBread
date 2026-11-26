using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 目的＝旧支配層の〈地位と権力の軸〉（秘密投票・官僚制度・国教・徴兵・検閲）の選好が札（定数 ±1）で、導ける物（国教＝十分の一税・官僚制度＝仲介者の取り分）を〈次の一段で失う所得の割合〉で読み直す案 D-1 の材料。〔履歴 InstrumentsLordstat.cs#1〕
public static partial class Instruments
{
    sealed class LordstatSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LordstatSink> LordstatSinks = new();

    /// <summary>身分の度合い（純関数）：猟官 1・資格任用 0.5・専門 0。</summary>
    internal static double LdEstate(BureaucracyLaw b) => b == BureaucracyLaw.Spoils ? 1.0 : b == BureaucracyLaw.MeritExam ? 0.5 : 0.0;
    /// <summary>仲介者の度合い（純関数）＝max(身分の度合い, 土地改革の項＝なし 1・限定 0.5・全面 0)。</summary>
    internal static double LdIntermediary(BureaucracyLaw b, ReformLevel lr) => System.Math.Max(LdEstate(b), lr == ReformLevel.None ? 1.0 : lr == ReformLevel.Limited ? 0.5 : 0.0);
    /// <summary>官僚制度の次の一段（猟官→資格任用→専門・専門は動かない）。</summary>
    internal static BureaucracyLaw LdBureaucracyNext(BureaucracyLaw b) => b == BureaucracyLaw.Spoils ? BureaucracyLaw.MeritExam : BureaucracyLaw.Professional;
    /// <summary>官僚制度の前の一段（専門→資格任用→猟官・猟官は動かない）。</summary>
    internal static BureaucracyLaw LdBureaucracyPrev(BureaucracyLaw b) => b == BureaucracyLaw.Professional ? BureaucracyLaw.MeritExam : BureaucracyLaw.Spoils;
    /// <summary>国教の前の一段（信仰の自由→内心の自由→国教・国教と国家無神論は動かない）。</summary>
    internal static StateReligionLaw LdReligionPrev(StateReligionLaw s) => s == StateReligionLaw.ReligiousFreedom ? StateReligionLaw.InnerFreedom : s == StateReligionLaw.InnerFreedom ? StateReligionLaw.StateChurch : s;
    /// <summary>土地改革の次の一段（なし→限定→全面・全面は動かない）。</summary>
    internal static ReformLevel LdLandNext(ReformLevel l) => l == ReformLevel.Full ? ReformLevel.Full : l + 1;
    /// <summary>土地改革の前の一段（全面→限定→なし・なしは動かない）。</summary>
    internal static ReformLevel LdLandPrev(ReformLevel l) => l == ReformLevel.None ? ReformLevel.None : l - 1;
    /// <summary>失う（得る）所得の割合の期待の中身（純関数）＝分子の比 share × (度合いの差 ÷ 今の度合い)。今の度合いが 0 以下なら 0。</summary>
    internal static double LdStakeShare(double share, double degNow, double degOther) => degNow > 1e-9 ? share * System.Math.Abs(degNow - degOther) / degNow : 0.0;
    /// <summary>国教の次の一段（国教→内心の自由→信仰の自由・信仰の自由と国家無神論は動かない）。</summary>
    internal static StateReligionLaw LdReligionNext(StateReligionLaw s) => s == StateReligionLaw.StateChurch ? StateReligionLaw.InnerFreedom : s == StateReligionLaw.InnerFreedom ? StateReligionLaw.ReligiousFreedom : s;
    /// <summary>十分の一税の度合い（純関数）＝国教なら 1・内心の自由・信仰の自由は係数・国家無神論 0（十分の一税の率が 0 以下なら 0）。</summary>
    internal static double LdTitheMul(StateReligionLaw s, Coefficients c)
        => !(c.TitheRate.Value > 0) ? 0.0
         : s == StateReligionLaw.InnerFreedom ? c.InnerFreedomTithe : s == StateReligionLaw.ReligiousFreedom ? c.ReligiousFreedomTithe : s == StateReligionLaw.StateAtheism ? 0.0 : 1.0;
    /// <summary>選挙の実在（純関数）＝議会政体（立憲君主制・共和制）なら 秘密投票 1・公開投票 OpenBallotReality／それ以外は PaperFranchiseReality。</summary>
    internal static double LdBallotReality(bool parliamentary, bool secret, Coefficients c) => parliamentary ? (secret ? 1.0 : c.OpenBallotReality) : c.PaperFranchiseReality;
    /// <summary>
    /// 〔履歴 InstrumentsLordstat.cs#2〕
    /// </summary>
    internal static double LdRentCapOf(ReformLevel lv, double full) => lv == ReformLevel.Full ? full : lv == ReformLevel.Limited ? full + (1.0 - full) * 0.5 : 1.0;
    /// <summary>
    /// 計器が自分で数える。〔履歴 InstrumentsLordstat.cs#3〕
    /// </summary>
    internal static double LdCustomRent(World w)
    {
        if (w.Regions.Count == 0) return 0.0;
        double m = double.PositiveInfinity;
        foreach (var pr in w.Regions[0].Producers) if (pr.IsRgo && pr.RentShare.Value < m) m = pr.RentShare.Value;
        return double.IsPositiveInfinity(m) ? 0.0 : m;
    }
    /// <summary>
    /// 〔履歴 InstrumentsLordstat.cs#4〕
    /// </summary>
    internal static double LdEffRent(double custom, ReformLevel lr, ReformLevel se, Coefficients c)
        => System.Math.Min(custom, System.Math.Min(LdRentCapOf(lr, c.LandReformRentCap.Value), LdRentCapOf(se, c.SerfEmancipationRentCap.Value)));

    static void LordstatTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        var sk = LordstatSinks.GetValue(sim, _ => new LordstatSink());
        sk.W ??= Open("lordstat", seed);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var c = w.Coef;
            double inc0 = 0, incClergy = 0, tithBase = 0, popAll = 0, popPeasant = 0, rent0 = 0; bool clergyHere = false;
            foreach (var r in w.Regions)
                foreach (var p in r.Pops)
                {
                    popAll += p.Size; if (p.ClassId == PopClass.Peasant) popPeasant += p.Size;
                    if (p.ClassId == PopClass.Clergy && p.Size > 1e-9) clergyHere = true;
                    if (CnBloc(p) == 0) { double e = System.Math.Max(0, p.EarnedPerMonth.Value); inc0 += e; if (p.ClassId == PopClass.Clergy) incClergy += e; rent0 += System.Math.Max(0, p.LandRentPerMonth.Value); }
                    if (!p.IsCriminal && p.IsCommoner && !p.IsModernElite && p.LastEarnedPerMonth.Value > 0) tithBase += p.LastEarnedPerMonth.Value;
                }
            if (!clergyHere) tithBase = 0;
            bool parl = PolicyPreferences.HasLegislature(w.Polity);
            var bnow = w.Bureaucracy; var bnext = LdBureaucracyNext(bnow); var lr = w.LandReform;
            double estNow = LdEstate(bnow), estNext = LdEstate(bnext), imNow = LdIntermediary(bnow, lr), imNext = LdIntermediary(bnext, lr);
            double popW = 0, moves = 0, dropSum = 0, aboveW = 0, cutDen = 0, cutNum = 0, adminLoss = 0, rhoNum = 0, rhoDen = 0;
            foreach (var r in w.Regions)
            {
                if (r.Rule.Length != 4 || r.RuleInit.Length != 4) continue;
                double pop = r.SumPopulation(); if (!(pop > 0)) continue;
                popW += pop;
                double drop = r.RuleInit[1] * (imNow - imNext);
                if (r.RuleInit[1] > 0 && imNext < imNow) moves += pop;
                dropSum += pop * drop;
                if (r.Rule[1] > r.RuleInit[1] * imNow) aboveW += pop;
                cutNum += pop * drop; cutDen += pop * (r.Rule[1] + r.Rule[0] * (1.0 - r.AdminFill));
                double fl = r.AdminFill > 0 ? (r.AdminFill < 1 ? r.AdminFill : 1.0) : 0.0;   // fl〔履歴 InstrumentsLordstat.cs#5〕
                rhoNum += pop * r.Rule[1] * fl; rhoDen += pop * (r.Rule[1] + r.Rule[0] * (1.0 - fl));
            }
            adminLoss = cutDen > 0 && inc0 > 1e-9 ? w.LastRuleCut * (cutNum / cutDen) / inc0 : 0.0;
            var snow = w.StateReligion; var snext = LdReligionNext(snow);
            double tdNow = LdTitheMul(snow, c), tdNext = LdTitheMul(snext, c), tr = c.TitheRate.Value;
            double titheLoss = tr * (tdNow - tdNext) * tithBase;
            // 呼び口は四半期の末＝World.PopSums は null（窓の外）＝Build は窓の和でなく求め直し。〔履歴 InstrumentsLordstat.cs#6〕
            var sprev = LdReligionPrev(snow);
            double tdPrev = LdTitheMul(sprev, c), clergyShare = inc0 > 1e-9 ? incClergy / inc0 : 0.0;
            var prof = PolicyPreferences.Build(w);
            // bprev〔履歴 InstrumentsLordstat.cs#7〕
            var bprev = LdBureaucracyPrev(bnow); var lrNext = LdLandNext(lr); var lrPrev = LdLandPrev(lr);
            double cutShare = inc0 > 1e-9 ? w.LastRuleCut / inc0 : 0.0, rho = rhoDen > 0 ? rhoNum / rhoDen : 0.0, cr = cutShare * rho;
            double iBurUp = LdIntermediary(bnext, lr), iLandUp = LdIntermediary(bnow, lrNext), iBurDn = LdIntermediary(bprev, lr), iLandDn = LdIntermediary(bnow, lrPrev);
            // custom〔履歴 InstrumentsLordstat.cs#8〕
            double custom = LdCustomRent(w), rentRatio = inc0 > 1e-9 ? rent0 / inc0 : 0.0, effNow = LdEffRent(custom, lr, w.SerfEmancipation, c);
            double landLoss = effNow > 1e-9 ? (effNow - LdEffRent(custom, lrNext, w.SerfEmancipation, c)) / effNow * rentRatio : 0.0;
            double landGain = effNow > 1e-9 ? (LdEffRent(custom, lrPrev, w.SerfEmancipation, c) - effNow) / effNow * rentRatio : 0.0;
            var cols = new List<(string Name, string Val)>(64)
            {
                ("quarter", I(q)), ("year", I(w.Year)), ("nation", NameOf(w)), ("polityCode", I((int)w.Polity)), ("secretBallotCode", I((int)w.SecretBallot)),
                ("bureaucracyCode", I((int)w.Bureaucracy)), ("stateReligionCode", I((int)w.StateReligion)), ("conscriptionCode", I((int)w.ConscriptionLaw)),
                ("lsIncome0PerMonth", F(inc0)), ("lsClergyIncomePerMonth", F(incClergy)),
                ("dsbRealityNow", F(LdBallotReality(parl, w.SecretBallot == SecretBallotLaw.Secret, c))), ("dsbRealityNext", F(LdBallotReality(parl, true, c))), ("dsbPrefNow0", F(-1.0)),
                ("dbuEstateNow", F(estNow)), ("dbuEstateNext", F(estNext)), ("dbuIntermediaryNow", F(imNow)), ("dbuIntermediaryNext", F(imNext)),
                ("dbuMovesShare", F(popW > 0 ? moves / popW : 0.0)), ("dbuLocalTargetDropMean", F(popW > 0 ? dropSum / popW : 0.0)),
                ("dbuRuleCutPerMonth", F(w.LastRuleCut)), ("dbuRuleCutLossNextShare0", F(adminLoss)), ("dbuAboveTargetShare", F(popW > 0 ? aboveW / popW : 0.0)),
                ("dreTitheDegreeNow", F(tdNow)), ("dreTitheDegreeNext", F(tdNext)), ("dreTitheBasePerMonth", F(tithBase)),
                ("dreTitheLossNextShare0", F(inc0 > 1e-9 ? titheLoss / inc0 : 0.0)), ("dreTitheLossNextClergyShare", F(incClergy > 1e-9 ? titheLoss / incClergy : 0.0)),
                ("dcsPeasantPopShare", F(popAll > 0 ? popPeasant / popAll : 0.0)),
                ("relDegNow", F(tdNow)), ("relDegNext", F(tdNext)), ("relDegPrev", F(tdPrev)), ("relClergyIncome0", F(incClergy)), ("relIncome0", F(inc0)),
                ("relStakeUpExp", F(LdStakeShare(clergyShare, tdNow, tdNext))), ("relStakeDownExp", F(LdStakeShare(clergyShare, tdNow, tdPrev))),
                ("relPrefUpAct", F(PolicyPreferences.Pref(PolicyAxis.StateReligion, 0, prof, w, true))), ("relPrefDownAct", F(PolicyPreferences.Pref(PolicyAxis.StateReligion, 0, prof, w, false))),
                ("relDoctrineCode", I((int)UtilityAgent.DoctrineFrom(w))), ("relGateNetUp", F(PolicyPreferences.NetSupport(w, PolicyAxis.StateReligion, true, prof))),
                ("d2CutOverIncome0", F(cutShare)), ("d2Rent0OverIncome0", F(inc0 > 1e-9 ? rent0 / inc0 : 0.0)), ("d2Rho", F(rho)),
                ("d2SMidBurExp", F(LdStakeShare(cr, imNow, iBurUp))), ("d2SMidLandExp", F(LdStakeShare(cr, imNow, iLandUp))),
                ("d2SGainBurExp", F(LdStakeShare(cr, imNow, iBurDn))), ("d2SGainLandExp", F(LdStakeShare(cr, imNow, iLandDn))),
                ("d2PrefBurUpAct", F(PolicyPreferences.Pref(PolicyAxis.Bureaucracy, 0, prof, w, true))), ("d2PrefLandUpAct", F(PolicyPreferences.Pref(PolicyAxis.LandReform, 0, prof, w, true))),
                ("d2PrefBurDownAct", F(PolicyPreferences.Pref(PolicyAxis.Bureaucracy, 0, prof, w, false))), ("d2PrefLandDownAct", F(PolicyPreferences.Pref(PolicyAxis.LandReform, 0, prof, w, false))),
                ("d2LordLossLandExp", F(landLoss)), ("d2LordGainLandExp", F(landGain)),
            };
            if (!sk.Header) { sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Name))); sk.Header = true; }
            sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Val)));
        }
    }

    static void LordstatClose(MultiNationSim sim)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (LordstatSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
