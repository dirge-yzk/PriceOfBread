namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (369) 扶養力の錨の表 anchor_s（読むだけ・事前登録 measurements/an_20261016_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（衝突面を消す）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （Pop／World／Region に欄なし）。国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。INS=1 のときだけ Quarter から呼ばれる。
//   anchorOld ＝ 旧の錨（pop − PopulationDelta）／anchorRegions ＝ 今その国が持つ州の AnchorPopulation の和／anchorUsed ＝ 人口の段が実際に使った錨（World.DiagAnchorBaseline＝人口の段が最後に書いた純記帳・人口の段が飛んだ国は前の値のまま・旗に従う式を計器の中で再計算しない＝何もしない機構が緑にならない）。
//   capacity・capBase・arableSum ＝ 人口の段が最後に書いた診断（World.DiagCapacity／DiagCapacityBaseline／DiagLandAnchor＝Σ耕地）＝人口の段が飛んだ国は前の値のまま。
//   born ＝ 分離独立で生まれた国（BornOfSecession）。regionsN ＝ 州の数（全て）／activeRegionsN ＝ 活きた州／anchorRegionsN ＝ AnchorPopulation が正の州（本国起源の州）。
public static partial class Instruments
{
    internal const string AnchorHeader = "quarter\tyear\tnation\tborn\tpopK\tdeltaK\tanchorOld\tanchorRegions\tanchorUsed\tflag\tregionsN\tactiveRegionsN\tanchorRegionsN\tcapacity\tcapBase\tarableSum\tfoodFill";

    static void AnchorFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;   // 死んだ国（人口 0）は行を出さない
        double anchorRegions = 0; int active = 0, withAnchor = 0;
        foreach (var r in w.Regions)
        {
            anchorRegions += r.AnchorPopulation;
            if (r.State == RegionState.Active) active++;
            if (r.AnchorPopulation > 0) withAnchor++;
        }
        double anchorOld = popK - w.PopulationDelta;
        bool flag = w.Coef.CapacityAnchorFromRegions > 0;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Anc ??= Open("anchor", seed);
        if (!sk.AncHeader) { sk.Anc.WriteLine(AnchorHeader); sk.AncHeader = true; }
        sk.Anc.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), w.BornOfSecession ? "1" : "0", F(popK), F(w.PopulationDelta), F(anchorOld), F(anchorRegions), F(w.DiagAnchorBaseline),
            flag ? "1" : "0", I(w.Regions.Count), I(active), I(withAnchor), F(w.DiagCapacity), F(w.DiagCapacityBaseline), F(w.DiagLandAnchor), F(w.FoodFill)));
    }
}
