using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1012) 地主の政治 D-0 の恒等の計器 lordstat_s（読むだけ・恒等・事前登録 measurements/m1012_20261110_prereg_cl.txt） -------------------------------------------------------
// 部分クラス＝InstrumentsCornint.cs の型（出力の口は ConditionalWeakTable・力学の値を書かない・乱数を引かない・新しいシミュの状態を作らない・表の関数の中で On を読まない）。
// 目的＝旧支配層の〈地位と権力の軸〉（秘密投票・官僚制度・国教・徴兵・検閲）の選好が札（定数 ±1）で、導ける物（国教＝十分の一税・官僚制度＝仲介者の取り分）を〈次の一段で失う所得の割合〉で読み直す案 D-1 の材料。
//   札のまま残るのは秘密投票・検閲・徴兵（権力の量から導くなら別の設計）。
// エンジンの関数を呼ばない：十分の一税の度合い・仲介者の度合い・身分の度合い・実効発言権・大衆の判定・十分の一税の請求は全て計器が自分で書く（走査の番人 LordstatInstrumentTests の L10 が禁止の名を見張る）。使い回すのは CnBloc だけ。
// 行＝国×四半期（cornint_s と同じ国の集合）。列＝28（鍵 8・分母 2・秘密投票 3・官僚制度 9・国教 5・徴兵 1）。額は国の通貨・どれも直近の月の値（…PerMonth）。
// 〈次の一段〉＝その法を 1 段だけ寛容側へ進めた後（秘密投票は公開→秘密・官僚制度は猟官→資格任用→専門・国教は国教→内心の自由→信仰の自由・国家無神論は例外で動かさない）。
// lsIncome0PerMonth＝閉鎖の層（CnBloc が 0）の max(0, 所得) の和（PolicyPreferences の Income[0] と同じ集合）。十分の一税の母体＝国に聖職者（人口が正）が居るときの大衆（平民で近代でなく犯罪者でない）の max(0, 前の月の所得) の和（徴収の走査は国全体の聖職者の有無で決まる）。
// ⚠限界（計器が自己申告する）：①dbuRuleCutLossNextShare0 は州の取り分を人口で按分した近似（州ごとの取り分はどこにも貯まらない）・在地の目標の min(1−無統治, …) の上限を数えない②十分の一税は請求で、払える額の率を数えない
//   ③dcsPeasantPopShare・dbuEstateNow・dbuEstateNext は突然変異で確かめていない④lsIncome0PerMonth には聖職者の枠の地代が入る国がある（国教の損失の分母が動く）。
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
    /// <summary>国教の次の一段（国教→内心の自由→信仰の自由・信仰の自由と国家無神論は動かない）。</summary>
    internal static StateReligionLaw LdReligionNext(StateReligionLaw s) => s == StateReligionLaw.StateChurch ? StateReligionLaw.InnerFreedom : s == StateReligionLaw.InnerFreedom ? StateReligionLaw.ReligiousFreedom : s;
    /// <summary>十分の一税の度合い（純関数）＝国教なら 1・内心の自由・信仰の自由は係数・国家無神論 0（十分の一税の率が 0 以下なら 0）。</summary>
    internal static double LdTitheMul(StateReligionLaw s, Coefficients c)
        => !(c.TitheRate.Value > 0) ? 0.0
         : s == StateReligionLaw.InnerFreedom ? c.InnerFreedomTithe : s == StateReligionLaw.ReligiousFreedom ? c.ReligiousFreedomTithe : s == StateReligionLaw.StateAtheism ? 0.0 : 1.0;
    /// <summary>選挙の実在（純関数）＝議会政体（立憲君主制・共和制）なら 秘密投票 1・公開投票 OpenBallotReality／それ以外は PaperFranchiseReality。</summary>
    internal static double LdBallotReality(bool parliamentary, bool secret, Coefficients c) => parliamentary ? (secret ? 1.0 : c.OpenBallotReality) : c.PaperFranchiseReality;

    static void LordstatTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = LordstatSinks.GetValue(sim, _ => new LordstatSink());
        sk.W ??= Open("lordstat", seed);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var c = w.Coef;
            double inc0 = 0, incClergy = 0, tithBase = 0, popAll = 0, popPeasant = 0; bool clergyHere = false;
            foreach (var r in w.Regions)
                foreach (var p in r.Pops)
                {
                    popAll += p.Size; if (p.ClassId == PopClass.Peasant) popPeasant += p.Size;
                    if (p.ClassId == PopClass.Clergy && p.Size > 1e-9) clergyHere = true;
                    if (CnBloc(p) == 0) { double e = System.Math.Max(0, p.EarnedPerMonth.Value); inc0 += e; if (p.ClassId == PopClass.Clergy) incClergy += e; }
                    if (!p.IsCriminal && p.IsCommoner && !p.IsModernElite && p.LastEarnedPerMonth.Value > 0) tithBase += p.LastEarnedPerMonth.Value;
                }
            if (!clergyHere) tithBase = 0;
            bool parl = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic;
            var bnow = w.Bureaucracy; var bnext = LdBureaucracyNext(bnow); var lr = w.LandReform;
            double estNow = LdEstate(bnow), estNext = LdEstate(bnext), imNow = LdIntermediary(bnow, lr), imNext = LdIntermediary(bnext, lr);
            double popW = 0, moves = 0, dropSum = 0, aboveW = 0, cutDen = 0, cutNum = 0, adminLoss = 0;
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
            }
            adminLoss = cutDen > 0 && inc0 > 1e-9 ? w.LastRuleCut * (cutNum / cutDen) / inc0 : 0.0;
            var snow = w.StateReligion; var snext = LdReligionNext(snow);
            double tdNow = LdTitheMul(snow, c), tdNext = LdTitheMul(snext, c), tr = c.TitheRate.Value;
            double titheLoss = tr * (tdNow - tdNext) * tithBase;
            var cols = new List<(string Name, string Val)>(32)
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
            };
            if (!sk.Header) { sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Name))); sk.Header = true; }
            sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Val)));
        }
    }

    static void LordstatClose(MultiNationSim sim)
    {
        if (LordstatSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
