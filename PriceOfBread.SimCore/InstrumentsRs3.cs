namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (558) 線 rs3 の計器（読むだけ） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRs2.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しい状態を持たない。rs2 の表（rs2_s<seed>.tsv）には 1 列も足さない（基線の生とバイト一致を保つ）。
// 出力＝ rs3_s<seed>.tsv（国×四半期・挙げた国は毎四半期、他の全ての国は年末）。
//   農奴解放と土地改革の旧支配層（ブロック 0）の強さの〈進める向きに使われた値〉〈減らす向きに使われた値〉を、Pref の返り値の絶対値として直に読む（旗 LordOppositionReverseGain の 0／1 のどちらの腕でも出る）。
//   あわせて 1 段戻したときの得の式の中身（戻した段の実効の取り分・得の割合）と、今の式（貯蓄の割合 W0）の強さ（反事実）を並べる。
// 追加 4 列（事前登録 [11](c)）：pendingCrisisBloc（危機が立っている運動のブロック・無ければ -1）・pendingCrisisTop（その本丸の符号＝軸の序数+1・増加＝正／減少＝負・無ければ 0）・revoltingBloc（進行中の内乱の主体のブロック・無ければ -1）・factionRevoltsCum（World.FactionRevolts の累計）。読むだけ。
// ⚠限界（計器が自分で申告する）：
//   ・四半期の末の断面。法の門が実際に Pref を読む瞬間（四半期の途中）とは違う。
//   ・Pref へ渡す立場の土台は旧支配層の項が読む欄（貯蓄の割合 W0・地代÷所得）だけを ComputeBuildSums から作った最小の物（他の欄は 0）＝この 2 軸の旧支配層の項以外の Pref は読まない。
//   ・慣習の取り分は最初の活動中の州の産地の生産者の最小の RentShare（PolicyPreferences.CustomaryRentShare）。

public static partial class Instruments
{
    internal const string Rs3Header = "quarter\tyear\tnation\tserfLawCode\tlandReformLawCode\tcustomaryRentShare\teffRentNow\teffRentPrevSerf\teffRentPrevLand\t"
        + "lordRentToIncomeShare\tgainSerfShare\tgainLandShare\t"
        + "oppSerfIncUsed\toppSerfDecUsed\toppLandIncUsed\toppLandDecUsed\toppOldIndex\tflagLevel\tflagLevel2\t"
        + "pendingCrisisBloc\tpendingCrisisTop\trevoltingBloc\tfactionRevoltsCum";

    static void Rs3Fold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;
        if (!(w.TotalPopulation > 0)) return;   // 死んだ国は行を出さない
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sums = PolicyPreferences.ComputeBuildSums(w);
        double rentToIncome = PolicyPreferences.RentToIncomeOf(sums.Rent[0], sums.Income[0]);
        double[] wealth = new double[PolicyPreferences.BlocCount];
        if (sums.Tw > 1e-9) for (int i = 0; i < wealth.Length; i++) wealth[i] = sums.Wealth[i] / sums.Tw;
        var e = new PolicyPreferences.Profile(new double[PolicyPreferences.BlocCount], wealth, new double[PolicyPreferences.BlocCount], new double[PolicyPreferences.BlocCount],
            new double[PolicyPreferences.BlocCount], new double[PolicyPreferences.BlocCount], 0.0, 0.0, rentToIncome);
        PolicyPreferences.EffectiveRentSharesBack(w, PolicyAxis.SerfEmancipation, out double now, out double prevS);
        PolicyPreferences.EffectiveRentSharesBack(w, PolicyAxis.LandReform, out _, out double prevL);
        double gainS = PolicyPreferences.LordIncomeGain(w, PolicyAxis.SerfEmancipation, rentToIncome);
        double gainL = PolicyPreferences.LordIncomeGain(w, PolicyAxis.LandReform, rentToIncome);
        double w0 = sums.Tw > 1e-9 ? sums.Wealth[0] / sums.Tw : 0.0;
        double Abs(PolicyAxis ax, bool inc) => System.Math.Abs(PolicyPreferences.Pref(ax, 0, e, w, inc));
        sk.Rs3 ??= Open("rs3", seed);
        if (!sk.Rs3Header) { sk.Rs3.WriteLine(Rs3Header); sk.Rs3Header = true; }
        sk.Rs3.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I((int)w.SerfEmancipation), I((int)w.LandReform),
            F(PolicyPreferences.CustomaryRentShare(w)), F(now), F(prevS), F(prevL),
            F(rentToIncome), F(gainS), F(gainL),
            F(Abs(PolicyAxis.SerfEmancipation, true)), F(Abs(PolicyAxis.SerfEmancipation, false)), F(Abs(PolicyAxis.LandReform, true)), F(Abs(PolicyAxis.LandReform, false)),
            F(PolicyPreferences.StatusScale * (0.5 + w0)), F(w.Coef.LordOppositionByRentLoss), F(w.Coef.LordOppositionReverseGain),
            I(w.PendingFactionCrisis?.Bloc ?? -1), I(w.PendingFactionCrisis?.CrisisDemands is { Length: > 0 } cd ? cd[0] : 0), I(w.RevoltingFaction?.Bloc ?? -1), I(w.FactionRevolts)));
    }
}
