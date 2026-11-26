namespace PriceOfBread.SimCore;

// (1344) c141lr 計器 lrlock_s：守られた法の後退を〈旧の規則なら塞がれたか〉で分けるための盤面（戦時・児童労働の法と合法度）を四半期ごとに 1 行。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンの判定の関数を呼ばない（戦時は計器が w.Diplomacy から自分で導く）。後退の行は読み手 lr_count.py が monlaw_s と突き合わせる。
public static partial class Instruments
{
    internal const string LrlockHeaderText = "quarter\tyear\tnationUid\tnation\tatWarFlag\tchildLaborAllowedFlag\tchildLaborLegalShare";

    static void LrlockFold(Sink sk, ulong seed, World w, int q)
    {
        if (!(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool atWar = false;
        if (w.Diplomacy is { } dip && w.SelfPower is { } sp)
            foreach (var war in dip.Wars) if (war.A == sp || war.B == sp) { atWar = true; break; }
        sk.Lrlock ??= Open("lrlock", seed);
        if (!sk.LrlockHeader) { sk.Lrlock.WriteLine(LrlockHeaderText); sk.LrlockHeader = true; }
        sk.Lrlock.WriteLine(string.Join('\t', I(q), I(w.Year), I(S(w).Uid), NameOf(w), atWar ? "1" : "0",
            w.ChildLaborAllowed ? "1" : "0", w.ChildLaborLegal.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
    }
}
