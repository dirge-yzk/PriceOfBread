namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (493) 硫酸とソーダ灰の表 chem_s・chemgood_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHouse.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ出す。
// 目的＝事前登録 measurements/gd_20261023_prereg.txt の T2〜T5（ルブラン法とソルベー法が開いたか・開いた後にソーダ灰の産出を新しい製法が取ったか・ルブラン法が回る国で硫酸が産まれているか・
//   石鹸などの買い手がソーダ灰を、肥料などの買い手が硫酸を買っているか）を読む。旗は無い（財の割りは構造の変更）。
// chem_s：挙げた国×四半期×{ルブラン法・ソルベー法・電解・黄鉄鉱法}（名前で引く＝並びが動いても読み手は壊れない）。人口が 0 の国は出さない。
//   列：quarter／year／nation／row／known（国が解禁済みなら 1）／recipeProducersCount（活きた州にあるその行のレシピの生産者の数）／recipeOutputPerMonth（その生産者の実産出 Output の和）／
//   goodOutputPerMonth（同じ財の全てのレシピの実産出の和）／recipeOutputShare（前者÷後者・後者が 0 なら nan）／goodPriceMean（活きた州のその財の市場の値段の単純平均・名目）／priceLevel／goodPriceReal（前者÷物価水準）
// chemgood_s：挙げた国×四半期×{硫酸・ソーダ灰}。列：quarter／year／nation／good／outputPerMonth（活きた州の市場の LastProduced の和）／demandPerMonth（LastDemand の和）／soldPerMonth（LastSold の和）／
//   importPerMonth（LastImport の和）／priceMean（名目の州の単純平均）／priceReal（÷物価水準）／sodaProducersActive（ソーダ灰のみ・実産出が正で倒産していない生産者〔州×レシピ〕の数）
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純平均（産出で重み付けしない）／Output は実産出（門が閉じた生産者は 0 に寄る）／国の名前は表示名（建国・改称で別の名前になる）／
//   市場の LastDemand は消費と投入の合計（買い手の内訳＝石鹸かガラスかは区別しない）。
public static partial class Instruments
{
    // 行の名前で引く（番人 SulfuricAcidSodaAshTests が表の末尾 3 行の名前を別に固定する）。
    static readonly string[] ChemRowNames = ["ルブラン法", "ソルベー法", "電解", "黄鉄鉱法"];
    static int[]? _chemRows;
    static int[] ChemRows => _chemRows ??= ChemRowNames.Select(n => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == n)).ToArray();

    internal static string ChemHeaderText()
        => "quarter\tyear\tnation\trow\tknown\trecipeProducersCount\trecipeOutputPerMonth\tgoodOutputPerMonth\trecipeOutputShare\tgoodPriceMean\tpriceLevel\tgoodPriceReal";

    internal static string ChemGoodHeaderText()
        => "quarter\tyear\tnation\tgood\toutputPerMonth\tdemandPerMonth\tsoldPerMonth\timportPerMonth\tpriceMean\tpriceReal\tsodaProducersActive";

    /// <summary>1 行の断面：活きた州にあるその行のレシピの生産者の数・その実産出の和・同じ財の全てのレシピの実産出の和・活きた州のその財の市場の値段の単純平均（名目）。</summary>
    internal static (int Count, double Recipe, double Good, double PriceMean) ChemMeasure(World w, int row)
    {
        var d = Catalog.Discoveries[row];
        int cnt = 0; double rec = 0, good = 0, px = 0; int pxN = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pr in r.Producers)
            {
                if (pr.GoodId != d.Good) continue;
                good += pr.Output;
                if (pr.DiscoveryRow == row) { cnt++; rec += pr.Output; }
            }
            if (d.Good >= 0 && d.Good < r.Markets.Length) { double p = r.Markets[d.Good].Price; if (p > 0) { px += p; pxN++; } }
        }
        return (cnt, rec, good, pxN > 0 ? px / pxN : double.NaN);
    }

    static void ChemFold(Sink sk, ulong seed, World w, int q, bool picked)
    {
        if (!picked || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (int row in ChemRows)
        {
            if (row < 0) continue;
            var (cnt, rec, good, mean) = ChemMeasure(w, row);
            sk.Chem ??= Open("chem", seed);
            if (!sk.ChemHeader) { sk.Chem.WriteLine(ChemHeaderText()); sk.ChemHeader = true; }
            sk.Chem.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), Catalog.Discoveries[row].Name, Discovery.Known(w.Discovered, row) ? "1" : "0", I(cnt), F(rec), F(good),
                F(good > 0 ? rec / good : double.NaN), F(mean), F(w.PriceLevel), F(w.PriceLevel > 0 ? mean / w.PriceLevel : double.NaN)));
        }
        foreach (int g in new[] { Catalog.SulfuricAcidId, Catalog.SodaAshId })
        {
            double outp = 0, dem = 0, sold = 0, imp = 0, px = 0; int pxN = 0, active = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || g >= r.Markets.Length) continue;
                var m = r.Markets[g];
                outp += m.LastProduced; dem += m.LastDemand; sold += m.LastSold; imp += m.LastImport;
                if (m.Price > 0) { px += m.Price; pxN++; }
                if (g == Catalog.SodaAshId) foreach (var pr in r.Producers) if (pr.GoodId == g && pr.Output * pr.Scale > 1e-9 && !pr.Bankrupt) active++;
            }
            double mean = pxN > 0 ? px / pxN : double.NaN;
            sk.ChemGood ??= Open("chemgood", seed);
            if (!sk.ChemGoodHeader) { sk.ChemGood.WriteLine(ChemGoodHeaderText()); sk.ChemGoodHeader = true; }
            sk.ChemGood.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), Catalog.Goods[g].Name, F(outp), F(dem), F(sold), F(imp), F(mean), F(w.PriceLevel > 0 ? mean / w.PriceLevel : double.NaN),
                g == Catalog.SodaAshId ? I(active) : "nan"));
        }
    }
}
