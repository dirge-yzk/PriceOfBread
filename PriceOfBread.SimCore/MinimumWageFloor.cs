namespace PriceOfBread.SimCore;

/// <summary>★最低賃金法の床（線 lw (459)・2026-10-20）＝その州の労働者の生活費を労働 1 単位の値段の下限として読む小さい関数群。
/// 作者の向き（2026-10-20）〈生活にかかるコストは最低賃金法でカバーすべき〉と、段〈「なし」「生存」「生活」「充足」〉（独立した法・量の段）を AI が写した形（読みの選択は確証なし・事前登録 measurements/lw459_20261020_prereg.txt §2 B2）。
/// ・籠の中身は法の段が決める：生存＝食料（主食）／生活＝食料＋日用品／充足＝食料＋日用品＋嗜好品。量はその州の階級の必要量（財の BaseNeed × 担い手 POP の人数）・値段はその州の今の市場の値段。
/// ・貯めない（導ける量は貯めない＝設計思想⑩）：州×旬に 1 回、市場の今の値段と人力の能力から求める。籠の値段は参照価格を使わない（参照価格のリワークと衝突しない）。参照価格を読むのは帯の天井で削る一点だけ（<see cref="AbsFloor"/>・天井は帯の順序を保つ構造の守りで、選んだ床を削る上限としては働かない〔床が天井を超える極端な州だけ〕＝既知のずれ）。新しい倍率を作らない。
/// ・床の単位は 1 つ（能力ベース・(469) 線 mw で決めた・確証なし・事前登録 §1 [E]・Q-MW1）：売れた量を分母にする読み（実雇用ベース）は雇用が減ると床が上がりさらに減る正帰還を実測した（lw (459) 13-4 C1：失業 0.906）ので持たない。
///   能力ベースは今の世界（能力が需要の約 20 倍）では値段の約 0.06 倍＝崖と帯の床を外した世界でしか縛らない（縛る範囲が限られる代償）。
/// ・効く労働の財：工場労働（労働力）と農業労働（農業労働力）。技師（高度労働力）には効かない（すでに供給の上限にいて弾力が働いている）。段は籠の中身の違いで、業種の範囲の違いではない。</summary>
public static class MinimumWageFloor
{
    private const double Eps = 1e-9;

    /// <summary>その法が、その労働財に効くか（法が None なら効かない・技師には効かない）。</summary>
    internal static bool Applies(MinimumWageLaw law, int laborGoodId)
        => law != MinimumWageLaw.None && (laborGoodId == Catalog.LaborId || laborGoodId == Catalog.FarmLaborId);

    /// <summary>その法の段の籠に、この財の層が入るか。生存＝主食／生活＝＋日用品／充足＝＋嗜好品。役務・住居・中間財・生産財は入れない（生活にかかる物の籠であって、役務の需要は別の仕組み）。
    /// ⚠段 None は籠を持たない（false）。動物性タンパクの食卓入り（FoodIncludesAnimalProtein＞0）は主食の必要量を譲る別の仕組み＝既定 0 の世界では主食だけが食料。</summary>
    internal static bool InBasket(MinimumWageLaw law, GoodTier tier) => tier switch
    {
        GoodTier.Staple => law >= MinimumWageLaw.Subsistence,
        GoodTier.Daily => law >= MinimumWageLaw.Living,
        GoodTier.Luxury => law >= MinimumWageLaw.Sufficiency,
        _ => false,
    };

    /// <summary>籠（1 人・月・その州の名目）＝その段に入る層の財の必要量×その州の今の値段の和。今の市場で誰にも需要されていない財（まだ誰も作れず需要の門が閉じた財）は入れない。</summary>
    internal static double BasketPerPerson(Region region, MinimumWageLaw law)
    {
        double sum = 0;
        foreach (var g in region.Goods)
        {
            if (!InBasket(law, g.Tier)) continue;
            if (!(g.BaseNeed > 0) || g.Id < 0 || g.Id >= region.Markets.Length) continue;
            var m = region.Markets[g.Id];
            if (!(m.LastDemand > 0)) continue;
            double px = m.Price;
            if (px > 0) sum += g.BaseNeed * px;
        }
        return sum;
    }

    /// <summary>労働 1 単位の値段の床（名目）。立たない（担い手が無い・分母が立たない）とき 0＝床を足さない。
    /// 籠 × 担い手 POP の人数 ÷ 人力の生産者の能力の和＝〈全員が能力どおり働いたときの 1 単位あたり〉。価格にも売れた量にも依らない＝正帰還が無い・状態を作らない。
    /// 今の世界（能力が需要の約 20 倍）では今の値段の約 0.06 倍＝縛らない（事前登録 §1 [E]）。</summary>
    internal static double PerUnit(Region region, int laborGoodId, MinimumWageLaw law)
    {
        if (law == MinimumWageLaw.None || laborGoodId < 0 || laborGoodId >= region.Markets.Length) return 0;
        int lp = -1;
        double cap = 0;
        foreach (var s in region.Producers)
            if (s.GoodId == laborGoodId && EconomyStep.IsHumanLaborSupply(s) && !s.Bankrupt)   // ★(469) 述語を供給の式（EconomyStep.ProfitFactor）と計器 labor_s の1本に揃える（投入を持つ人力の行は数えない）
            {
                if (lp < 0) lp = s.LaborPopIndex;   // 人力の担い手（同じ財の他のレシピは同じ POP を共有する）
                if (s.LaborPopIndex == lp) cap += s.Capacity;
            }
        if (lp < 0 || lp >= region.Pops.Count) return 0;
        if (!(cap > Eps)) return 0;   // 否定形＝NaN も床なし側へ
        double f = BasketPerPerson(region, law) * region.Pops[lp].Size / cap;
        return f > 0 ? f : 0;
    }

    /// <summary>賃金の帯の外（SoftBand の後）で下から切る絶対の床。天井（<paramref name="ceiling"/>＝参照アンカー×天井の倍率）は超えない（帯の順序を保つ構造の守り。選んだ値を削る上限ではない）。</summary>
    internal static double AbsFloor(Region region, int laborGoodId, double ceiling, MinimumWageLaw law)
    {
        double f = PerUnit(region, laborGoodId, law);
        return f > 0 ? System.Math.Min(f, ceiling) : 0;
    }
}
