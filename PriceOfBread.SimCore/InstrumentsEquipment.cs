namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (495) レシピごとの設備の表 eq_s・eq_r（読むだけ・恒等） ---------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（蓄えは ConditionalWeakTable＝セーブに載らない）。INS=1 のときだけ出す。
// 目的＝事前登録 measurements/eq495_20261023_prereg.txt（追記 5）の狙い C1（配りが設備になった）・C3a（配りと退場で民間の持分の合計が保たれる）・C3b（台帳の恒等）・D1（向き）・D2（配りが利幅に従う）と後退の確かめ R1〜R3 の読み手 scripts/instruments/eq495/an_eq.py の入力。
// eq_s＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。列（単位は接尾辞）：
//   flagLevel（RecipeEquipment）／pointsBought（四半期の購入が州の工業化度の在庫に足した点の和＝私的・国家・巨大建造物。配りの入力 g）／equipInstalled（そのうち設備に足した Scale の和）／
//   scaleSum（適格な個体の Scale の和＝全ての活きている州）／builtSum（同・BuiltScale の和）／scaleMax・scaleMaxGood（最大の個体と財の番号）／eligibleCount／gateOpenCount（門が開いた適格な個体）／bankruptCount（倒産中の適格な個体）／
//   indMean（MeanIndustrialization）／indMax（州の TotalIndustrialization の最大）／
//   台帳（その四半期に書き手の側で足した量・状態の和とは出所が別）：completedScale（国営の完成が足した Scale）／dismantledScale（恐慌の廃棄が引いた Scale）／distNetScale（配りが変えた Scale の純増＝配った量＋薄めの変化）／retiredNetScale（退場が変えた Scale の純増＝戻した量−消えた量）／
//   distDriftMax（配りと退場の 1 回ごとの〈民間の持分の合計の達成値と目標の相対差〉の四半期の最大＝C3a）／renormShort（薄めきれなかった回数）／activeRegionCount（活きている州の数）／svcLaborShare（工員の取り合いの重み LaborWeight×Scale に占める役務の割合＝記録）。
// eq_r＝挙げた国だけ×レシピ×四半期（発見の行を持つレシピのある財の、門が開いた州があるか Scale が正のレシピ）。列：good・recipe・scaleSum（活きている州の和）・shareOfGood（同じ財の適格なレシピの Scale の和に対する割合）・builtSum・gateOpenRegions・marginSum（門が開いて倒産でない個体の max(0, SmoothedMargin) の和＝配りの重みの元）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／pointsBought と equipInstalled は INS=1 の月の合計（配りが走らなかった月は 0）／レシピの名前が空の個体（既定の 1 本目）は recipe＝"(既定)"。
public static partial class Instruments
{
    internal const string EqSHeaderText = "quarter\tyear\tnation\tflagLevel\tpointsBought\tequipInstalled\tscaleSum\tbuiltSum\tscaleMax\tscaleMaxGood\teligibleCount\tgateOpenCount\tbankruptCount\tindMean\tindMax\tcompletedScale\tdismantledScale\tdistNetScale\tretiredNetScale\tdistDriftMax\trenormShort\tactiveRegionCount\tsvcLaborShare";
    internal const string EqRHeaderText = "quarter\tyear\tnation\tgood\trecipe\tscaleSum\tshareOfGood\tbuiltSum\tgateOpenRegions\tmarginSum";

    sealed class EqAcc { public double Points, Installed, Completed, Dismantled, DistNet, RetiredNet, DriftMax; public int Short; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, EqAcc> EqAccs = new();

    /// <summary>購入が州の工業化度の在庫に足した点 <paramref name="points"/> と、それを設備に足した Scale の合計 <paramref name="installed"/>（EconomyStep が INS=1 のときだけ呼ぶ）。</summary>
    internal static void EquipmentBought(World w, Region r, double points, double installed)
    {
        var a = EqAccs.GetOrCreateValue(w);
        a.Points += points; a.Installed += installed;
    }

    /// <summary>配り（Distribute）1 回の台帳：<paramref name="drift"/>＝民間の持分の合計の達成値と目標の相対差・<paramref name="shortfall"/>＝薄めきれなかった・<paramref name="netScale"/>＝適格な個体の Scale の純増（配った量＋薄めの変化）。</summary>
    internal static void EquipmentDistributed(World w, double drift, bool shortfall, double netScale)
    {
        var a = EqAccs.GetOrCreateValue(w);
        a.DistNet += netScale; if (drift > a.DriftMax) a.DriftMax = drift; if (shortfall) a.Short++;
    }
    /// <summary>退場（Retire）1 回の台帳：<paramref name="netScale"/>＝戻した量−消えた量。</summary>
    internal static void EquipmentRetired(World w, double drift, bool shortfall, double netScale)
    {
        var a = EqAccs.GetOrCreateValue(w);
        a.RetiredNet += netScale; if (drift > a.DriftMax) a.DriftMax = drift; if (shortfall) a.Short++;
    }
    internal static void EquipmentCompleted(World w, double scale) { EqAccs.GetOrCreateValue(w).Completed += scale; }
    internal static void EquipmentDismantled(World w, double scale) { EqAccs.GetOrCreateValue(w).Dismantled += scale; }

    static void EqFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        var acc = EqAccs.GetOrCreateValue(w);
        double points = acc.Points, installed = acc.Installed, completed = acc.Completed, dismantled = acc.Dismantled, distNet = acc.DistNet, retiredNet = acc.RetiredNet, driftMax = acc.DriftMax; int renormShort = acc.Short;
        acc.Points = acc.Installed = acc.Completed = acc.Dismantled = acc.DistNet = acc.RetiredNet = acc.DriftMax = 0; acc.Short = 0;   // 助走の月の分は EquipmentBought が呼ばれない（旗は暦が進む月だけ）＝四半期ごとに戻す
        if (w.Coef.RecipeEquipment <= 0 || !(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        double scaleSum = 0, builtSum = 0, scaleMax = 0, indMax = 0, wElig = 0, wSvc = 0; int scaleMaxGood = -1, elig = 0, open = 0, bankrupt = 0, activeRegions = 0;
        var perRecipe = new Dictionary<(int Good, string Recipe), (double Scale, double Built, int Open, double Margin)>();
        var gatedGoods = new HashSet<int>();
        foreach (var r in w.Regions)
        {
            activeRegions++;
            if (r.TotalIndustrialization > indMax) indMax = r.TotalIndustrialization;
            foreach (var s in r.Producers)
            {
                if (!FactoryEquipment.Eligible(s))
                {
                    if (s.BuysLabor && !s.IsRgo && s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier == GoodTier.Service
                        && !Catalog.IsLaborGood(s.GoodId)) wSvc += s.LaborWeight * s.Scale;
                    continue;
                }
                wElig += s.LaborWeight * s.Scale;
                elig++; scaleSum += s.Scale; builtSum += s.BuiltScale;
                if (s.Scale > scaleMax) { scaleMax = s.Scale; scaleMaxGood = s.GoodId; }
                bool gateOpen = Discovery.Gate(r, s) > 0;
                if (gateOpen) open++;
                if (s.Bankrupt) bankrupt++;
                string rn = s.RecipeName.Length > 0 ? s.RecipeName : "(既定)";
                var key = (s.GoodId, rn);
                perRecipe.TryGetValue(key, out var v);
                double mg = gateOpen && !s.Bankrupt && s.SmoothedMargin > 0 ? s.SmoothedMargin : 0.0;
                perRecipe[key] = (v.Scale + s.Scale, v.Built + s.BuiltScale, v.Open + (gateOpen ? 1 : 0), v.Margin + mg);
                if (s.DiscoveryRow >= 0) gatedGoods.Add(s.GoodId);
            }
        }
        sk.EqS ??= Open("eq", seed);
        if (!sk.EqSHeader) { sk.EqS.WriteLine(EqSHeaderText); sk.EqSHeader = true; }
        sk.EqS.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(w.Coef.RecipeEquipment), F(points), F(installed), F(scaleSum), F(builtSum), F(scaleMax), I(scaleMaxGood), I(elig), I(open), I(bankrupt), F(w.MeanIndustrialization), F(indMax),
            F(completed), F(dismantled), F(distNet), F(retiredNet), F(driftMax), I(renormShort), I(activeRegions), F(wElig + wSvc > 0 ? wSvc / (wElig + wSvc) : 0.0)));
        if (!picked) return;   // eq_r は挙げた国だけ（毎四半期）＝量を抑える
        sk.EqR ??= Open("eqr", seed);
        if (!sk.EqRHeader) { sk.EqR.WriteLine(EqRHeaderText); sk.EqRHeader = true; }
        var goodSum = new Dictionary<int, double>();
        foreach (var (key, v) in perRecipe) { goodSum.TryGetValue(key.Good, out var t); goodSum[key.Good] = t + v.Scale; }
        foreach (var (key, v) in perRecipe.OrderBy(kv => kv.Key.Good).ThenBy(kv => kv.Key.Recipe, StringComparer.Ordinal))
        {
            if (!gatedGoods.Contains(key.Good) || (v.Open == 0 && !(v.Scale > 1e-9))) continue;
            double tot = goodSum[key.Good];
            sk.EqR.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), Catalog.Goods[key.Good].Name, key.Recipe, F(v.Scale), F(tot > 0 ? v.Scale / tot : double.NaN), F(v.Built), I(v.Open), F(v.Margin)));
        }
    }
}
