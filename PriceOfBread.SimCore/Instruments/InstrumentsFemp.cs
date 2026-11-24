namespace PriceOfBread.SimCore;

// (1351) 女性参加の構造値を旧と新の規則で同じ盤面から数える表 femp_s。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 旧＝FemalePartBase＋(FemalePartProperty−FemalePartBase)×州の賃金保持（エンジンが r.FemaleParticipation へ置く式）・新＝FemalePartBase（賃金保持に依らない構造値）。
// 州の人口（POP の大きさの和）で国へ加重する。エンジンの関数は呼ばず、州の欄と係数を計器が自分で読む。
public static partial class Instruments
{
    internal const string FempHeaderText = "quarter\tyear\tnation\twomenRightsCode\tretention\toldStruct\tnewStruct\tactualStruct\tdistressMax";

    static void FempFold(Sink sk, ulong seed, World w, int q)
    {
        if (!Tg.Femp) return;   // 書く表が選ばれていなければ飛ぶ
        double sz = 0, ret = 0, old = 0, act = 0, dis = 0;
        var c = w.Coef;
        foreach (var r in w.Regions)
        {
            double s = 0; foreach (var p in r.Pops) s += p.Size;
            if (!(s > 0)) continue;
            sz += s;
            ret += s * r.WomenWageRetention;
            old += s * (c.FemalePartBase + (c.FemalePartProperty - c.FemalePartBase) * r.WomenWageRetention);
            act += s * r.FemaleParticipation;
            dis += s * (c.FemaleNeedWeight * r.WomenWageRetention);
        }
        if (!(sz > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Femp ??= Open("femp", seed);
        if (!sk.FempHeader) { sk.Femp.WriteLine(FempHeaderText); sk.FempHeader = true; }
        sk.Femp.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I((int)w.WomenRights), F(ret / sz), F(old / sz), F(c.FemalePartBase), F(act / sz), F(dis / sz)));
    }
}
