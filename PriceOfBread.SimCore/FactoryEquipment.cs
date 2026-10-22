namespace PriceOfBread.SimCore;

/// <summary>★(495) レシピごとの設備（旗 <see cref="Coefficients.RecipeEquipment"/>・既定 0＝呼ばれない＝厳密恒等。事前登録 measurements/eq495_20261023_prereg.txt）。
/// <para>**設備を持つ生産者**＝<see cref="Eligible"/>＝BuysLabor の非 RGO・非役務の工場。旗が正の月（暦が進む月だけ・助走は旧の規則）、その <see cref="Producer.Scale"/> は〈据え付けた設備〉で、
/// 動くのは次の 4 つだけ：①減価（<see cref="Depreciate"/>＝州の在庫と同じ係数）②購入（<see cref="Distribute"/>＝州の工業化度に足される点を同じ割合で配る）③国家の工場の完成（World.StepConstruction）
/// ④恐慌の廃棄（<see cref="Scrap"/>）。利幅を目標にした伸び縮み・ScaleMax・ScaleFloor・倒産の Scale=0・再参入の FactoryReentryScale は働かない（EconomyStep の Scale の更新）。</para>
/// <para>⚠Scale は同じ担い手 POP を取り合う工場の間の労働の取り分の重み（EconomyStep の prodW＝LaborWeight×Scale）で、Capacity の総量は変えない。
/// ⚠設備の合計は州の在庫 <see cref="Region.Industrialization"/> と同じ割合で増え・同じ係数で減る（二重の表現・尺度は 1 本＝事前登録 追記 1）。設備の持ち主の区別は BuiltScale（国営持分）÷Scale が持ち、
/// 減価と廃棄は両方に同じ係数を掛けて持分を動かさない。AI の設計＝確証なし。</para></summary>
internal static class FactoryEquipment
{
    /// <summary>設備を持つ生産者か（BuysLabor の工場＝産地〔IsRgo〕・役務・職人・労働の財の並存レシピを除く）。</summary>
    internal static bool Eligible(Producer s)
        => s.BuysLabor && !s.IsRgo && s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier != GoodTier.Service;

    /// <summary>設備の規則が働く月か＝旗が正で、助走の凍結中でない（助走は初期値を作る道具＝旧の規則のまま走り、その終わりの Scale を 1783 年の設備と読む）。</summary>
    internal static bool On(Coefficients c, bool warmFrozen) => c.RecipeEquipment > 0 && !warmFrozen;

    /// <summary>設備の減価＝州の在庫の減価（EconomyStep の dep）と同じ係数を、適格な個体の Scale と BuiltScale に同じ文で掛ける（持分 BuiltScale÷Scale が動かない）。</summary>
    internal static void Depreciate(Region r, double dep)
    {
        double keep = 1.0 - dep;
        foreach (var s in r.Producers)
            if (Eligible(s)) { s.Scale *= keep; s.BuiltScale *= keep; }
    }

    /// <summary>恐慌の廃棄＝適格な全個体の Scale と BuiltScale に同じ割合（州の在庫に掛ける scrapKeep と同じ）。</summary>
    internal static void Scrap(Region r, double keep)
    {
        foreach (var s in r.Producers)
            if (Eligible(s)) { s.Scale *= keep; s.BuiltScale *= keep; }
    }

    /// <summary>購入を設備にする。州の工業化度の在庫に <paramref name="points"/>（g）が足される同じ購入を、適格な個体の Scale の合計に同じ割合 φ＝g÷(Ind'＋g) で足す
    /// （<paramref name="indBefore"/>＝Ind'＝足す前の在庫〔新旧合計〕・φ∈(0,1]・Ind'＝0 でも有界）。増える合計＝max(適格な個体の Scale の合計, FactoryBuildScale)×φ。
    /// 個体への配り＝門が開いて倒産でない適格な個体へ max(0, SmoothedMargin) に比例（全てが 0 以下なら等分）。配り先が無ければ増えは 0。
    /// 戻り値＝実際に足した設備の合計（計器と番人）。<paramref name="cost"/>＝計算量の台帳（null＝数えない）。純粋に Scale だけを書く（お金は動かさない）。</summary>
    internal static double Distribute(Region r, Coefficients c, double points, double indBefore, long[]? cost)
    {
        if (!(points > 0)) return 0;   // 否定形＝NaN・負は配らない
        var ps = r.Producers;
        double sumScale = 0, sumW = 0; int nCand = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var s = ps[i];
            if (!Eligible(s)) continue;
            sumScale += s.Scale;
            if (!Candidate(r, s)) continue;
            nCand++;
            double m = s.SmoothedMargin;
            if (m > 0) sumW += m;   // 否定形でなく肯定形＝NaN・負は重み 0
        }
        if (cost is not null) cost[CostLedger.RecipeEquipmentScans] += ps.Count;
        if (nCand == 0) return 0;
        double ind = indBefore > 0 ? indBefore : 0;
        double phi = points / (ind + points);
        double basis = sumScale > c.FactoryBuildScale ? sumScale : c.FactoryBuildScale;
        double add = basis * phi;
        if (!(add > 0)) return 0;
        double given = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var s = ps[i];
            if (!Eligible(s) || !Candidate(r, s)) continue;
            double w = sumW > 1e-12 ? (s.SmoothedMargin > 0 ? s.SmoothedMargin / sumW : 0.0) : 1.0 / nCand;
            double d = add * w;
            s.Scale += d; given += d;
        }
        return given;
    }

    /// <summary>購入を受け取れる個体＝門が開いて（未発見・退場でない）倒産でない。</summary>
    internal static bool Candidate(Region r, Producer s) => !s.Bankrupt && Discovery.Gate(r, s) > 0;
}
