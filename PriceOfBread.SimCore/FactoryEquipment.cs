namespace PriceOfBread.SimCore;

/// <summary>★(495) レシピごとの設備（旗 <see cref="Coefficients.RecipeEquipment"/>・既定 0＝呼ばれない＝厳密恒等。事前登録 measurements/eq495_20261023_prereg.txt）。
/// <para>**設備を持つ生産者**＝<see cref="Eligible"/>＝BuysLabor の非 RGO・非役務の工場。旗が正の月（暦が進む月だけ・助走は旧の規則）、その <see cref="Producer.Scale"/> は〈据え付けた設備〉で、
/// 動くのは次の 3 つだけ：①購入（<see cref="Distribute"/>＝州の工業化度に足される点を同じ割合で配り、民間の持分の合計を保つよう他の個体から薄める）
/// ②国家の工場の完成（World.StepConstruction）③国営の休止中の設備の廃棄（<see cref="Dismantle"/>）。BuiltScale÷Scale の持分は取引（完成・接収・払い下げ・廃棄）でしか動かない（時間の経過では動かない＝民間も国営も摩耗しない・旗 0 も BuiltScale は縮まない）。利幅を目標にした伸び縮み・ScaleMax・ScaleFloor・倒産の Scale=0・再参入の FactoryReentryScale は働かない（EconomyStep の Scale の更新）。</para>
/// <para>⚠**Scale は州の資本の中の取り分（相対）**＝適格な個体の Scale の合計は購入で増えない（民間の持分の合計を保つ＝事前登録 追記 3）。尺度が増えないので、開幕の工業化度が小さい州で設備の大きさが桁で膨らむ（canary で 最大 6.3e6）ことも、国営の 1 棟（FactoryBuildScale）が薄まることも起きない。
/// 民間の古い設備は〈購入に薄められる〉で相対的に減る（購入が減価を補う定常では (1−減価) の割合で入れ替わる）。</para>
/// <para>⚠Scale は同じ担い手 POP を取り合う工場の間の労働の取り分の重み（EconomyStep の prodW＝LaborWeight×Scale）で、Capacity の総量は変えない。
/// ⚠配りの大きさは州の在庫 <see cref="Region.Industrialization"/> に足される点の割合 φ で決まる（二重の表現＝事前登録 追記 1 (1)・追記 3）。設備の持ち主の区別は BuiltScale（国営持分）÷Scale が持つ。AI の設計＝確証なし。</para></summary>
internal static class FactoryEquipment
{
    /// <summary>設備を持つ生産者か（BuysLabor の工場＝産地〔IsRgo〕・役務・職人・労働の財の並存レシピを除く）。</summary>
    internal static bool Eligible(Producer s)
        => s.BuysLabor && !s.IsRgo && s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier != GoodTier.Service;

    /// <summary>設備の規則が働く月か＝旗が正で、助走の凍結中でない（助走は初期値を作る道具＝旧の規則のまま走り、その終わりの Scale を 1783 年の設備と読む）。</summary>
    internal static bool On(Coefficients c, bool warmFrozen) => c.RecipeEquipment > 0 && !warmFrozen;

    /// <summary>国営の休止中の設備の廃棄（恐慌）＝BuiltScale が <paramref name="builtBefore"/> から減った分だけ Scale からも引く（民間の持分を動かさない）。呼び手は BuiltScale を更新する前の値を渡す。</summary>
    internal static void Dismantle(Producer s, double builtBefore)
    {
        double drop = builtBefore - s.BuiltScale;
        if (drop > 0) s.Scale -= drop;
    }

    /// <summary>購入を設備にする。州の工業化度の在庫に <paramref name="points"/>（g）が足される同じ購入を、適格な個体の Scale の合計に同じ割合 φ＝g÷(Ind'＋g) で足す
    /// （<paramref name="indBefore"/>＝Ind'＝足す前の在庫〔新旧合計〕・φ∈(0,1]・Ind'＝0 でも有界）。増える合計＝max(適格な個体の Scale の合計, FactoryBuildScale)×φ。
    /// 個体への配り＝門が開いて倒産でない適格な個体へ max(0, SmoothedMargin) に比例（全てが 0 以下なら等分）。配り先が無ければ増えは 0。
    /// 戻り値＝実際に足した設備の合計（計器と番人）。<paramref name="cost"/>＝計算量の台帳（null＝数えない）。純粋に Scale だけを書く（お金は動かさない）。</summary>
    internal static double Distribute(Region r, Coefficients c, double points, double indBefore, long[]? cost)
    {
        if (!(points > 0)) return 0;   // 否定形＝NaN・負は配らない
        var ps = r.Producers;
        double sumScale = 0, sumBuilt = 0, sumW = 0; int nCand = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var s = ps[i];
            if (!Eligible(s)) continue;
            sumScale += s.Scale; sumBuilt += s.BuiltScale;
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
        // 民間の持分の合計を保つ（尺度を増やさない）：民間の持分（Scale−BuiltScale）の合計 P に given を足した分を、P/(P＋given) 倍へ全ての適格な個体で薄める。国営の持分 BuiltScale は動かさない。
        // P が 0 に近い州（全て国営）は薄める先が無いので薄めない（尺度が増える側＝稀・計器の scaleSum で見る）。
        double priv = sumScale - sumBuilt;
        if (priv > 1e-12 && given > 0)
        {
            double keep = priv / (priv + given);
            for (int i = 0; i < ps.Count; i++)
            {
                var s = ps[i];
                if (!Eligible(s)) continue;
                double b = s.BuiltScale;
                double p = s.Scale - b; if (p < 0) p = 0;
                s.Scale = b + p * keep;
            }
        }
        return given;
    }

    /// <summary>購入を受け取れる個体＝門が開いて（未発見・退場でない）倒産でない。</summary>
    internal static bool Candidate(Region r, Producer s) => !s.Bankrupt && Discovery.Gate(r, s) > 0;
}
