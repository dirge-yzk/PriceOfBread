using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1013) 貿易の計器 C3 recipeout_s（国×財×レシピ×賦存の級の産出と単位費用・読むだけ・恒等・事前登録 measurements/m1013_20261110_prereg_ro.txt）
// 部分クラス（InstrumentsNationgood.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 級（X 産地の発見の行／E 開始時の賦存／Z 希少鉱物の非産地〔産出 0〕／N 非賦存の RGO〔0.4〕／S 自給免除の RGO〔1.0〕／F RGO でない生産者／U 座標が引けない RGO）は
//   州の resources・Catalog.Sites・Climate から計器が自分で決める（Productivity からは決めない）。Productivity÷(財の生産性×7.0) は級の規則値との突き合わせ用に列へ出すだけ。
// mc は計器が自分で組む（エンジンの MineralSelfCost・InputCostPerUnit・EffectiveQty・ExplainCostByRecipe は本体から呼ばない）。
// ⚠限界（計器が自己申告する）：①7.0（FullChainProductivityScale）と 0.4（ResourceScarcity）はエンジンの private const の写し＝試験 T3 が固定する。
//   ②産出が 0 の行の単位費用は生産者の単純平均（産出重みが定義できない）。③Region.Resources を持たない小世界は級 N・S の判別が Climate の座標に依る。
public static partial class Instruments
{
    internal const string RecipeoutHeaderText = "year\tnation\tgood\trecipe\tclass\tproducers\toutputSum\tcapacitySum\tscaleSum\tunitCostOutW\tselfCostOutW\tunitCostIfAbundance\tendowFromProductivity\tsiteFound";

    private const double RoFullChainScale = 7.0;   // Simulation.FullChainProductivityScale の写し（試験 T3 が実物と突き合わせる）
    private const double RoScarcity = 0.4;         // Simulation.ResourceScarcity の写し（同上）

    /// <summary>生産者 1 個体の級・規則値・単位費用（番人用）。Class は X/E/Z/N/S/F/U・Rule は倍率の規則値（F は 1.0・U は NaN）。</summary>
    internal readonly record struct RoProd(string Class, double Rule, double Endow, double Mc, double Self, double IfAbundance);

    static int RoSiteRow(string place, int good, Coefficients c, bool shared)
    {
        if (!shared) return -1;
        int row = Catalog.SiteRowOf(place, good);
        return row >= 0 && good == Catalog.SilverId && !(c.SilverRushEnabled > 0) ? -1 : row;
    }

    /// <summary>生産者 1 個体を読む（級・費用）。<paramref name="r"/>＝その生産者を持つ州（価格も同じ州）。</summary>
    internal static RoProd RoRead(World w, Region r, Producer p)
    {
        var c = w.Coef;
        int g = p.GoodId;
        string cls; double rule;
        // 播種の賦存を受けうる生産者＝カタログの既定の生産者（RGO と、レシピ名が空の非役務の財）。職人・代替レシピ・役務は播種の後に足される＝受けない。
        bool eligible = p.IsRgo || (p.RecipeName.Length == 0 && g >= 0 && g < Catalog.Goods.Length && Catalog.Goods[g].Tier != GoodTier.Service);
        if (!eligible) { cls = "F"; rule = 1.0; }
        else
        {
            bool shared = !w.StepsDiplomacy;
            bool inRes = System.Array.IndexOf(r.Resources, g) >= 0;
            int site = p.IsRgo ? RoSiteRow(r.Name, g, c, shared) : -1;
            if (site >= 0)
            {
                cls = "X";
                rule = (inRes ? Simulation.ScarceEndowment(g, c, shared) : Simulation.ResourceEndowment(g)) * c.ResourceEndowmentScale;
                if (p.SiteFound && Catalog.Sites[site].Kind == Catalog.SiteKind.Deposit) rule *= c.GoldRushEndowment;
            }
            else if (inRes) { cls = "E"; rule = Simulation.ScarceEndowment(g, c, shared) * c.ResourceEndowmentScale; }
            else if (shared && c.ScarceMineralGate > 0 && Catalog.IsScarceMineral(g, c)) { cls = "Z"; rule = 0.0; }
            else if (!p.IsRgo) { cls = "F"; rule = 1.0; }
            else
            {
                var (lat, lon) = Climate.CityCoord(r.Name);
                if (lat == 0 && lon == 0 && w.Regions.Count > 0) (lat, lon) = Climate.CityCoord(w.Regions[0].Name);
                if (lat == 0 && lon == 0) { cls = "U"; rule = double.NaN; }
                else if (!Climate.StapleSelfSufficient(g, lat, lon) && !Catalog.IsUbiquitousMineral(g, c)) { cls = "N"; rule = RoScarcity * c.ResourceScarcityScale; }
                else { cls = "S"; rule = 1.0; }
            }
        }
        double endow = eligible && cls != "F" && g >= 0 && g < Catalog.Goods.Length ? p.Productivity / (Catalog.Goods[g].Productivity * RoFullChainScale) : double.NaN;
        // 自己費用（参照価格の単位→州の名目）。鉱物の RGO は品位の枝つき。
        double self = p.MarginalCost * w.CostPriceLevel * r.LocalPriceMultiplier;
        if (c.MineralCostFollowsGrade > 0 && p.IsRgo && Catalog.IsMineral(g))
        {
            double gm = r.MineralGradeFactor;
            if (gm > 1e-9) self /= gm;
        }
        // 投入費（自州の値段）。労働力の節約は BuysLabor の労働力だけ。
        double inputs = 0;
        foreach (var inp in p.Inputs)
        {
            double qty = inp.QtyPerOutput;
            if (inp.GoodId == Catalog.LaborId && p.BuysLabor) qty = inp.QtyPerOutput * (1.0 - c.ElecLaborSave * p.ElecFill) * (1.0 - c.LaborSaveRate * r.Mechanization);
            double price = inp.GoodId >= 0 && inp.GoodId < r.Markets.Length ? r.Markets[inp.GoodId].Price : 0.0;
            inputs += price * qty;
        }
        double mc = self + inputs;
        double selfAb = self;
        if (rule > 1.0)
        {
            double expo = System.Math.Max(0.0, 1.0 - c.EndowmentCostFollowsAbundance);   // 旗 1 の盤面は播種で割ってある＝二重に割らない
            selfAb = self / System.Math.Pow(rule, expo);
        }
        return new RoProd(cls, rule, endow, mc, self, selfAb + inputs);
    }

    sealed class RoAcc { public int N; public double O, Cap, Sc, OMc, OSelf, OAb, SMc, SSelf, SAb, SEndow; public int NEndow, NSiteFound; }
    sealed class RoSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, RoSink> RoSinks = new();

    /// <summary>国 1 つの行（番人用）：財の id・レシピ名・級の順。</summary>
    internal static List<(int Good, string Recipe, string Class, int N, double O, double Cap, double Sc, double Mc, double Self, double Ab, double Endow, double SiteF)> RoRows(World w, int goodLimit)
    {
        var acc = new Dictionary<(int, string, string), RoAcc>();
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Producers)
            {
                int g = p.GoodId;
                if (g < 0 || g >= r.Markets.Length || g >= goodLimit) continue;
                var x = RoRead(w, r, p);
                var key = (g, p.RecipeName.Length == 0 ? "(既定)" : p.RecipeName, x.Class);
                if (!acc.TryGetValue(key, out var a)) { a = new RoAcc(); acc[key] = a; }
                double o = p.Output;
                a.N++; a.O += o; a.Cap += p.Capacity; a.Sc += p.Scale;
                a.OMc += o * x.Mc; a.OSelf += o * x.Self; a.OAb += o * x.IfAbundance;
                a.SMc += x.Mc; a.SSelf += x.Self; a.SAb += x.IfAbundance;
                if (!double.IsNaN(x.Endow)) { a.SEndow += x.Endow; a.NEndow++; }
                if (p.SiteFound) a.NSiteFound++;
            }
        }
        var keys = new List<(int, string, string)>(acc.Keys);
        keys.Sort((a, b) => { int t = a.Item1.CompareTo(b.Item1); if (t != 0) return t; t = string.CompareOrdinal(a.Item2, b.Item2); return t != 0 ? t : string.CompareOrdinal(a.Item3, b.Item3); });
        var rows = new List<(int, string, string, int, double, double, double, double, double, double, double, double)>(keys.Count);
        foreach (var k in keys)
        {
            var a = acc[k]; bool wgt = a.O > 0;
            rows.Add((k.Item1, k.Item2, k.Item3, a.N, a.O, a.Cap, a.Sc,
                wgt ? a.OMc / a.O : a.SMc / a.N, wgt ? a.OSelf / a.O : a.SSelf / a.N, wgt ? a.OAb / a.O : a.SAb / a.N,
                a.NEndow > 0 ? a.SEndow / a.NEndow : double.NaN, k.Item3 == "X" ? (double)a.NSiteFound / a.N : double.NaN));
        }
        return rows;
    }

    static void RecipeoutTable(MultiNationSim sim, ulong seed, int q)
    {
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || sim.Nations.Count == 0 || !NgYear(sim.Nations[0].World.Year)) return;
        var sk = RoSinks.GetValue(sim, _ => new RoSink());
        if (sk.W is null) { sk.W = Open("recipeout", seed); sk.W.WriteLine(RecipeoutHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int yearLabel = sim.Nations[0].World.Year;
        // 国の行は nationgood_s と同じ：清算に居た国→清算の後に増えた国
        var worlds = new List<World>(); var seen = new HashSet<World>(); int goodLimit = Catalog.Goods.Length;
        if (NgAccs.TryGetValue(sim.Market, out var a0) && a0.Any) { foreach (var w0 in a0.Worlds) if (seen.Add(w0)) worlds.Add(w0); goodLimit = System.Math.Min(goodLimit, a0.Gc); }
        foreach (var n in sim.Nations) if (seen.Add(n.World)) worlds.Add(n.World);
        foreach (var w in worlds)
        {
            string nm = NameOf(w);
            foreach (var row in RoRows(w, goodLimit))
                sk.W.WriteLine(string.Join('\t', I(yearLabel), nm, Catalog.Goods[row.Good].Name, row.Recipe, row.Class, I(row.N), FR(row.O), FR(row.Cap), FR(row.Sc), FR(row.Mc), FR(row.Self), FR(row.Ab), FR(row.Endow), FR(row.SiteF)));
        }
    }

    static void RecipeoutClose(MultiNationSim sim) { if (RoSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
