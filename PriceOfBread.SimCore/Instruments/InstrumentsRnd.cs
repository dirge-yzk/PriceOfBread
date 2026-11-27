namespace PriceOfBread.SimCore;

// (1401) c149rn 計器 rnd_s：乱数を気候と疫病だけにする旗の読み＝同盟の数と作物の病気の状態を四半期ごと国ごとに 1 行。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンの判定の関数を呼ばない（同盟は計器が w.Diplomacy.Powers を自分で回して HasAlliance を数える）。
public static partial class Instruments
{
    internal const string RndHeaderText = "quarter\tyear\tnationUid\tnation\talliesCount\tcropDiseasedCount\tcropHealthMinShare";

    static void RndFold(Sink sk, ulong seed, World w, int q)
    {
        if (!(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int allies = 0;
        if (w.Diplomacy is { } dip && w.SelfPower is { } sp)
            foreach (var o in dip.Powers) if (o != sp && dip.HasAlliance(sp, o)) allies++;
        sk.Rnd ??= Open("rnd", seed);
        if (!sk.RndHeader) { sk.Rnd.WriteLine(RndHeaderText); sk.RndHeader = true; }
        sk.Rnd.WriteLine(string.Join('\t', I(q), I(w.Year), I(S(w).Uid), NameOf(w), I(allies), I(0), F(1.0)));   // (1413) 作物の病気は消した＝列は同じ値（病気の数 0・最小の健全度 1.0）を書いて表の形を保つ
    }
}
