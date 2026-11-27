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
        int sick = 0; double min = 1.0;
        foreach (var h in w.CropHealth) { if (h < 1.0) sick++; if (h < min) min = h; }
        sk.Rnd ??= Open("rnd", seed);
        if (!sk.RndHeader) { sk.Rnd.WriteLine(RndHeaderText); sk.RndHeader = true; }
        sk.Rnd.WriteLine(string.Join('\t', I(q), I(w.Year), I(S(w).Uid), NameOf(w), I(allies), I(sick), F(min)));
    }
}
