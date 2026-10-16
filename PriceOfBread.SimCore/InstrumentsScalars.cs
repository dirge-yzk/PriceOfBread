namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (373) 政治のスカラーの表 scalars_s（読むだけ・事前登録 measurements/ps_20261016_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAnchor.cs／InstrumentsFm.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （(377) で World の Diag 3 欄は消した＝天命の戦争の項は係数と厭戦のストックからここで導く）。国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。INS=1 のときだけ Quarter から呼ばれる。
//   （制度疲労の列 flagD・decay・decayCeiling は制度疲労の物理削除で消した。2026-10-16）。実効汚職 effCorruption ＝ World.EffectiveCorruption（読むだけの導出・財政の漏れの率の本体）。
//   dynMandate・legitimacy・defeatShock ＝ 天命・正統性・敗戦ショック。warExhaustion ＝ 厭戦のストック（Power.WarExhaustion）。
//   milWarMean・militancyMean ＝ Pop.MilWar と Pop.Militancy の人口加重平均（MilWar は Politics が最後に書いた闘争度の目標への寄与＝何もしない機構が緑にならないための実測）。
//   dmWar ＝ 天命の戦争の項（−DynMandateWarWeight×厭戦のストック＝Politics の式と同じ値をここで導く）。汚職の 2 項の列 dmGraft・corrErosion と旗の列 flagC・flagW は (377) の物理削除で消した。
//   atWar・polityCode・educationLaw・suffrage・censorship・bureaucracy ＝ 政体の遷移の年と改革の年の読み取り（法の列挙値の整数）。
// ⚠限界（計器が自分で申告する）：
//   ・dmWar は行を書く時点の厭戦のストックから導く（Politics.Step が使った時点の値とは限らない）。人口 0 の国と死んだ国は行を出さない。
//   ・effCorruption は World.EffectiveCorruption を計器の中で呼ぶ＝AdminEfficiency の走査を国×行ごとに 1 回足す（INS=1 のときだけ・結果は書かない）。
public static partial class Instruments
{
    internal const string ScalarsHeader = "quarter\tyear\tnation\tborn\tpopK\tatWar\tpolityCode\teducationLaw\tsuffrage\tcensorship\tbureaucracy\t"
        + "effCorruption\tdynMandate\tlegitimacy\tdefeatShock\twarExhaustion\tmilWarMean\tmilitancyMean\tdmWar";

    static void ScalarsFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;   // 死んだ国（人口 0）は行を出さない
        double sz = 0, milWar = 0, mil = 0;
        foreach (var p in w.AllPops()) { sz += p.Size; milWar += p.MilWar * p.Size; mil += p.Militancy * p.Size; }
        if (sz > 0) { milWar /= sz; mil /= sz; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var c = w.Coef;
        sk.Sca ??= Open("scalars", seed);
        if (!sk.ScaHeader) { sk.Sca.WriteLine(ScalarsHeader); sk.ScaHeader = true; }
        sk.Sca.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), w.BornOfSecession ? "1" : "0", F(popK), Government.AtWar(w) ? "1" : "0",
            I((int)w.Polity), I((int)w.EducationLaw), I((int)w.Suffrage), I((int)w.Censorship), I((int)w.Bureaucracy),
            F(w.EffectiveCorruption), F(w.DynasticMandate), F(w.Legitimacy), F(w.DefeatShock),
            F(w.SelfPower?.WarExhaustion ?? 0), F(milWar), F(mil), F(-c.DynMandateWarWeight * (w.SelfPower?.WarExhaustion ?? 0))));
    }
}
