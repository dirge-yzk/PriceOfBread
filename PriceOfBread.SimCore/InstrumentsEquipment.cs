namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (495) レシピごとの設備の表 eq_s・eq_r（読むだけ・恒等） ---------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（蓄えは ConditionalWeakTable＝セーブに載らない）。INS=1 のときだけ出す。
// 目的＝事前登録 measurements/eq495_20261023_prereg.txt の狙い C1（保存）・C2（買わないと増えない）・V1（ヴィンテージ）・D1（向き）と後退の確かめ R1〜R3 の読み手 scripts/instruments/eq495/an_eq.py の入力。
// eq_s＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。列（単位は接尾辞）：
//   flagLevel（RecipeEquipment）／pointsBought（四半期の購入が州の工業化度の在庫に足した点の和＝私的・国家・巨大建造物。配りの入力 g）／equipInstalled（そのうち設備に足した Scale の和）／
//   scaleSum（適格な個体の Scale の和＝全ての活きている州）／builtSum（同・BuiltScale の和）／scaleMax・scaleMaxGood（最大の個体と財の番号）／eligibleCount／gateOpenCount（門が開いた適格な個体）／bankruptCount（倒産中の適格な個体）／
//   indMean（MeanIndustrialization）／indMax（州の TotalIndustrialization の最大）。
// eq_r＝国×レシピ×四半期（同じ財に適格なレシピが 2 つ以上ある財だけ・門が開いた州があるレシピ）。列：good・recipe・scaleSum（活きている州の和）・shareOfGood（同じ財の適格なレシピの Scale の和に対する割合）・builtSum・gateOpenRegions。
// ⚠限界（計器が自己申告する）：四半期の末の断面／pointsBought と equipInstalled は INS=1 の月の合計（配りが走らなかった月は 0）／レシピの名前が空の個体（既定の 1 本目）は recipe＝"(既定)"。
public static partial class Instruments
{
    internal const string EqSHeaderText = "quarter\tyear\tnation\tflagLevel\tpointsBought\tequipInstalled\tscaleSum\tbuiltSum\tscaleMax\tscaleMaxGood\teligibleCount\tgateOpenCount\tbankruptCount\tindMean\tindMax";
    internal const string EqRHeaderText = "quarter\tyear\tnation\tgood\trecipe\tscaleSum\tshareOfGood\tbuiltSum\tgateOpenRegions";

    sealed class EqAcc { public double Points, Installed; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, EqAcc> EqAccs = new();

    /// <summary>購入が州の工業化度の在庫に足した点 <paramref name="points"/> と、それを設備に足した Scale の合計 <paramref name="installed"/>（EconomyStep が INS=1 のときだけ呼ぶ）。</summary>
    internal static void EquipmentBought(World w, Region r, double points, double installed)
    {
        var a = EqAccs.GetOrCreateValue(w);
        a.Points += points; a.Installed += installed;
    }

    static void EqFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        var acc = EqAccs.GetOrCreateValue(w);
        double points = acc.Points, installed = acc.Installed;
        acc.Points = acc.Installed = 0;   // 助走の月の分は EquipmentBought が呼ばれない（旗は暦が進む月だけ）＝四半期ごとに戻す
        if (w.Coef.RecipeEquipment <= 0 || !(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        double scaleSum = 0, builtSum = 0, scaleMax = 0, indMax = 0; int scaleMaxGood = -1, elig = 0, open = 0, bankrupt = 0;
        var perRecipe = new Dictionary<(int Good, string Recipe), (double Scale, double Built, int Open)>();
        var recipesOfGood = new Dictionary<int, HashSet<string>>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            if (r.TotalIndustrialization > indMax) indMax = r.TotalIndustrialization;
            foreach (var s in r.Producers)
            {
                if (!FactoryEquipment.Eligible(s)) continue;
                elig++; scaleSum += s.Scale; builtSum += s.BuiltScale;
                if (s.Scale > scaleMax) { scaleMax = s.Scale; scaleMaxGood = s.GoodId; }
                bool gateOpen = Discovery.Gate(r, s) > 0;
                if (gateOpen) open++;
                if (s.Bankrupt) bankrupt++;
                string rn = s.RecipeName.Length > 0 ? s.RecipeName : "(既定)";
                var key = (s.GoodId, rn);
                perRecipe.TryGetValue(key, out var v);
                perRecipe[key] = (v.Scale + s.Scale, v.Built + s.BuiltScale, v.Open + (gateOpen ? 1 : 0));
                if (!recipesOfGood.TryGetValue(s.GoodId, out var set)) recipesOfGood[s.GoodId] = set = new HashSet<string>();
                if (gateOpen) set.Add(rn);
            }
        }
        sk.EqS ??= Open("eq", seed);
        if (!sk.EqSHeader) { sk.EqS.WriteLine(EqSHeaderText); sk.EqSHeader = true; }
        sk.EqS.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(w.Coef.RecipeEquipment), F(points), F(installed), F(scaleSum), F(builtSum), F(scaleMax), I(scaleMaxGood), I(elig), I(open), I(bankrupt), F(w.MeanIndustrialization), F(indMax)));
        sk.EqR ??= Open("eqr", seed);
        if (!sk.EqRHeader) { sk.EqR.WriteLine(EqRHeaderText); sk.EqRHeader = true; }
        var goodSum = new Dictionary<int, double>();
        foreach (var (key, v) in perRecipe) { goodSum.TryGetValue(key.Good, out var t); goodSum[key.Good] = t + v.Scale; }
        foreach (var (key, v) in perRecipe.OrderBy(kv => kv.Key.Good).ThenBy(kv => kv.Key.Recipe, StringComparer.Ordinal))
        {
            if (v.Open == 0 || !recipesOfGood.TryGetValue(key.Good, out var rs) || rs.Count < 2) continue;
            double tot = goodSum[key.Good];
            sk.EqR.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), Catalog.Goods[key.Good].Name, key.Recipe, F(v.Scale), F(tot > 0 ? v.Scale / tot : double.NaN), F(v.Built), I(v.Open)));
        }
    }
}
