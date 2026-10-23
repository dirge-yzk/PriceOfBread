namespace PriceOfBread.SimCore;

/// <summary>★(495) レシピごとの設備（旗 <see cref="Coefficients.RecipeEquipment"/>・既定 1＝2026-10-23 に点火済み〔0 にすると呼ばれない＝旧の規則〕。事前登録 measurements/eq495_20261023_prereg.txt 追記 5）。
/// <para>**設備を持つ生産者**＝<see cref="Eligible"/>＝BuysLabor の非 RGO・非役務の工場。旗が正の月（暦が進む月だけ・助走は旧の規則）、その <see cref="Producer.Scale"/> は〈据え付けた設備〉で、
/// 動くのは次の 4 つだけ：①購入（<see cref="Distribute"/>＝州の工業化度に足される点を同じ割合で配り、民間の持分の合計を保つよう国営の持分を持たない個体を薄める）
/// ②国家の工場の完成（World.StepConstruction）③国営の休止中の設備の廃棄（<see cref="Dismantle"/>）④退場（発見の門が閉じた個体の設備を同じ州の他の個体へ比例で戻す＝<see cref="Retire"/>）。
/// 時間の経過では動かない（民間も国営も摩耗しない・旗 0 も BuiltScale は縮まない）。利幅を目標にした伸び縮み・ScaleMax・ScaleFloor・倒産の Scale=0・再参入の FactoryReentryScale は働かない（EconomyStep の Scale の更新）。
/// 混ざった工場（国営の持分と民間の持分を持つ個体）の BuiltScale÷Scale は、取引（完成・接収・払い下げ・廃棄）とその個体自身への購入でしか動かない（他の個体の購入と退場の薄めは国営の持分を持つ個体に触れない）。</para>
/// <para>⚠**Scale は州の資本の中の取り分（相対）**＝民間の持分の合計は購入でも退場でも増えも減りもしない（事前登録 追記 3・5）。尺度が増えないので、開幕の工業化度が小さい州で設備の大きさが桁で膨らむ（最初の形の canary で最大 6.3e6）ことも、
/// 国営の 1 棟（FactoryBuildScale）が薄まることも起きない。民間の古い設備は〈購入に薄められる〉で相対的に減る。</para>
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
    internal static void Dismantle(Producer s, double builtBefore, World? ledger = null)
    {
        double drop = builtBefore - s.BuiltScale;
        if (drop > 0) { s.Scale -= drop; if (ledger is not null && Instruments.On) Instruments.EquipmentDismantled(ledger, drop); }
    }

    /// <summary>購入を設備にする。州の工業化度の在庫に <paramref name="points"/>（g）が足される同じ購入を、適格な個体の Scale の合計に同じ割合 φ＝g÷(Ind'＋g) で足す
    /// （<paramref name="indBefore"/>＝Ind'＝足す前の在庫〔新旧合計〕・φ∈(0,1]・Ind'＝0 でも有界）。増える合計＝max(適格な個体の Scale の合計, FactoryBuildScale)×φ。
    /// 個体への配り＝門が開いて倒産でない適格な個体へ max(0, SmoothedMargin) に比例（全てが 0 以下なら等分）。配り先が無ければ増えは 0。
    /// 配ったあと、民間の持分の合計 P_D（<see cref="InSet"/> の max(0, Scale−BuiltScale) の和）を保つよう、国営の持分を持たない個体の Scale を係数 k 倍に薄める（<see cref="Renormalize"/>・事前登録 追記 5 (1)）。
    /// 戻り値＝実際に足した設備の合計（薄める前・計器と番人）。<paramref name="cost"/>＝計算量の台帳（null＝数えない）。<paramref name="ledger"/>＝計器の World（null＝記録しない）。純粋に Scale だけを書く（お金は動かさない）。</summary>
    internal static double Distribute(Region r, Coefficients c, double points, double indBefore, long[]? cost, World? ledger = null)
    {
        if (!(points > 0)) return 0;   // 否定形＝NaN・負は配らない
        var ps = r.Producers;
        var flags = Flags(ps.Count);
        double sumScale = 0, sumW = 0, pBefore = 0; int nCand = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var s = ps[i];
            flags[i] = 0;
            if (!Eligible(s)) continue;
            sumScale += s.Scale;
            if (!(Discovery.Gate(r, s) > 0)) continue;
            double pr = s.Scale - s.BuiltScale; if (pr > 0) pBefore += pr;
            if (s.Bankrupt) { flags[i] = 1; continue; }
            flags[i] = 2; nCand++;
            double m = s.SmoothedMargin;
            if (m > 0) sumW += m;   // 否定形でなく肯定形＝NaN・負は重み 0
        }
        if (cost is not null) cost[CostLedger.RecipeEquipmentScans] += 3L * ps.Count;   // 合計・配り・薄めの 3 周
        if (nCand == 0) return 0;
        double ind = indBefore > 0 ? indBefore : 0;
        double phi = points / (ind + points);
        double basis = sumScale > c.FactoryBuildScale ? sumScale : c.FactoryBuildScale;
        double add = basis * phi;
        if (!(add > 0)) return 0;
        double given = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            if (flags[i] != 2) continue;
            var s = ps[i];
            double w = sumW > 1e-12 ? (s.SmoothedMargin > 0 ? s.SmoothedMargin / sumW : 0.0) : 1.0 / nCand;
            double d = add * w;
            s.Scale += d; given += d;
        }
        // 民間の持分の合計を保つ（尺度を増やさない）。国営の持分を持つ個体（混ざった工場）は薄めない＝BuiltScale÷Scale（国営の持分）を他の個体の購入で動かさない。
        if (!(pBefore > 1e-12))
        {
            // 州に民間の設備が無い（薄める先が無い）：消さずに育てる＝設備を持てなくなる行き止まりを作らない（開幕に工場の無い州だけ・稀）。
            if (ledger is not null) Instruments.EquipmentDistributed(ledger, 0.0, false, given);
            return given;
        }
        double drift = Renormalize(ps, flags, pBefore, out double dScale, out bool shortfall);
        if (ledger is not null) Instruments.EquipmentDistributed(ledger, drift, shortfall, given + dScale);
        return given;
    }

    /// <summary>退場（発見の門が閉じた個体）の設備を、同じ州の薄める集合へ比例で戻す＝Scale は州の資本の中の取り分（相対）で、消えた取り分は他の取り分の割合を上げるだけ（利幅による配り直しではない）。
    /// 呼び手は門が閉じた適格な個体（<paramref name="s"/>）の Scale が正のときに呼ぶ。s.Scale は 0 になる。国営の持分を持たない個体の Scale だけが変わる（<see cref="Renormalize"/>）。事前登録 追記 5 (2)。</summary>
    internal static void Retire(Region r, Producer s, long[]? cost, World? ledger = null)
    {
        var ps = r.Producers;
        var flags = Flags(ps.Count);
        double pBefore = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var q = ps[i];
            flags[i] = 0;
            if (ReferenceEquals(q, s) || !Eligible(q) || !(Discovery.Gate(r, q) > 0)) continue;
            double pr = q.Scale - q.BuiltScale; if (pr > 0) pBefore += pr;
            flags[i] = 1;
        }
        if (cost is not null) cost[CostLedger.RecipeEquipmentScans] += 2L * ps.Count;
        double removed = s.Scale, freed = s.Scale - s.BuiltScale; if (freed < 0) freed = 0;
        s.Scale = 0;
        double drift = Renormalize(ps, flags, pBefore + freed, out double dScale, out bool shortfall);
        if (ledger is not null) Instruments.EquipmentRetired(ledger, drift, shortfall, dScale - removed);
    }

    /// <summary>薄める集合（flags≥1＝適格で発見の門が開いた個体）の民間の持分の合計を <paramref name="target"/> へ戻す。国営の持分を持つ個体（BuiltScale＞1e-9＝混ざった工場）は動かさず、
    /// 国営の持分を持たない個体の Scale に 1 つの係数 k＝(target−混ざった個体の民間の持分の和)÷(国営の持分を持たない個体の Scale の和) を掛ける。k の分子が 0 以下なら触らない（薄めきれない＝shortfall）、
    /// 分母が 0 に近ければ薄める先が無い（全て国営か混ざった個体＝shortfall）。どちらも k＝1（触らない）。戻り値＝達した合計と target の相対差（<paramref name="dScale"/>＝この関数が変えた Scale の合計）。</summary>
    private static double Renormalize(List<Producer> ps, byte[] flags, double target, out double dScale, out bool shortfall)
    {
        double pMixed = 0, pPure = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            if (flags[i] == 0) continue;
            var s = ps[i];
            if (s.BuiltScale > 1e-9) { double pr = s.Scale - s.BuiltScale; if (pr > 0) pMixed += pr; }
            else if (s.Scale > 0) pPure += s.Scale;
        }
        double need = target - pMixed;
        double k = pPure > 1e-12 && need > 0 ? need / pPure : 1.0;   // 薄めきれない（混ざった個体だけで目標を超えた・全てが国営か混ざった個体）ときは触らない＝一度の購入で国営の持分を持たない個体を全滅させない（shortfall に数える）
        dScale = 0;
        if (pPure > 1e-12 && k != 1.0)
            for (int i = 0; i < ps.Count; i++)
            {
                if (flags[i] == 0) continue;
                var s = ps[i];
                if (!(s.BuiltScale > 1e-9) && s.Scale > 0) { double before = s.Scale; s.Scale *= k; dScale += s.Scale - before; }
            }
        double achieved = pMixed + (pPure > 1e-12 ? pPure * k : pPure);
        double scale = target > 1e-9 ? target : 1e-9;
        double drift = Math.Abs(achieved - target) / scale;
        shortfall = drift > 1e-9;
        return drift;
    }

    [ThreadStatic] private static byte[]? flagBuf;
    private static byte[] Flags(int n)
    {
        var b = flagBuf;
        if (b is null || b.Length < n) flagBuf = b = new byte[Math.Max(n, 256)];
        return b;
    }

    /// <summary>薄める集合＝適格で発見の門が開いた個体（倒産中も含む＝設備は残る）。</summary>
    internal static bool InSet(Region r, Producer s) => Eligible(s) && Discovery.Gate(r, s) > 0;
}
