namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (373) 政治のスカラーの表 scalars_s（読むだけ・事前登録 measurements/ps_20261016_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAnchor.cs／InstrumentsFm.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （World の Diag 3 欄は Politics が最後に書いた純記帳で、計器だけが読む）。国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。INS=1 のときだけ Quarter から呼ばれる。
//   decay・decayCeiling ＝ 制度疲労のストックと天井（World.InstitutionalDecay／Ceiling）。実効汚職 effCorruption ＝ World.EffectiveCorruption（読むだけの導出・財政の漏れの率の本体）。
//   dynMandate・legitimacy・defeatShock ＝ 天命・正統性・敗戦ショック。warExhaustion ＝ 厭戦のストック（Power.WarExhaustion）。
//   milWarMean・militancyMean ＝ Pop.MilWar と Pop.Militancy の人口加重平均（MilWar は Politics が最後に書いた闘争度の目標への寄与＝何もしない機構が緑にならないための実測）。
//   dmWar・dmGraft・corrErosion ＝ Politics が最後に書いた天命の戦争の項・汚職の項・腐敗→正統性の侵食（旗 1 で 0 になっているかを実測で確かめる）。
//   atWar・polityCode・educationLaw・suffrage・censorship・bureaucracy ＝ 政体の遷移の年と改革の年の読み取り（法の列挙値の整数）。flagD／flagC／flagW ＝ 旗 3 本。
// ⚠限界（計器が自分で申告する）：
//   ・dmWar／dmGraft／corrErosion は Politics.Step が最後に走った四半期の値（Politics は四半期に 1 回）。人口 0 の国と死んだ国は行を出さない。
//   ・effCorruption は World.EffectiveCorruption を計器の中で呼ぶ＝AdminEfficiency の走査を国×行ごとに 1 回足す（INS=1 のときだけ・結果は書かない）。
public static partial class Instruments
{
    internal const string ScalarsHeader = "quarter\tyear\tnation\tborn\tpopK\tatWar\tpolityCode\teducationLaw\tsuffrage\tcensorship\tbureaucracy\tflagD\tflagC\tflagW\t"
        + "decay\tdecayCeiling\teffCorruption\tdynMandate\tlegitimacy\tdefeatShock\twarExhaustion\tmilWarMean\tmilitancyMean\tdmWar\tdmGraft\tcorrErosion";

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
            F(c.DeleteInstitutionalDecay), F(c.DeleteCorruptionScalars), F(c.DeleteWarWearinessScalars),
            F(w.InstitutionalDecay), F(w.InstitutionalDecayCeiling), F(w.EffectiveCorruption), F(w.DynasticMandate), F(w.Legitimacy), F(w.DefeatShock),
            F(w.SelfPower?.WarExhaustion ?? 0), F(milWar), F(mil), F(w.DiagDmWar), F(w.DiagDmGraft), F(w.DiagCorrErosion)));
    }
}
