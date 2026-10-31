namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (705) 段0 の計器 dsc1_s（発見の連続化＝改良度 L の再生の材料・読むだけ・恒等） ----------------------------------------------------------------
// 設計書 docs/202610/20261030/20261030_発見の連続化_設計.md §8-2 の段0（国×レシピの産出の割合 E・技術者の密度 D・取引の割合 c）＋技術水準の全期間の列。
// 事前登録 measurements/dsc1_705_20261031_prereg.txt [1][2]。INS=1（Instruments.On）のときだけ・四半期の末（Instruments.Quarter の国の行の後＝nationUid 採番済み）に全ての国を 1 巡する。
// 世界へ書かない・乱数を引かない・新しいシミュの状態を作らない。列の決め方は Improvement.ColumnOf（段1 の形と同じ 1 本）。
// 行の種類（kind 列）：
//   R＝国×列（その四半期に産出が正の列だけ）：col（列の番号）・colName・goodName・isRgo（列の生産者に産地が 1 つでもあれば 1）・
//      qPerMonth（国の活きた州のその列の生産者の Output の和）・valuePerMonth（Output×その州のその財の値段の和）・
//      dgPerMonth（その財の国の需要＝活きた州の Markets[財].LastDemand の和）・engHomePerMonth（本国の州〔植民地格でない〕の門の開いた生産者の EngineerUse の和）・
//      homePopK（本国の州の兵士でない POP の人数の和・千人）・gateMax（州の門の最大）。
//   N＝国：techLevel（World.TechLevel）・bracket（Improvement.BracketOf＝World.TechProductivity の括弧の写し）・replacement3（World.ReplacementRate(3)＝四半期の置き換わり率）・homePopK。
//   C＝国の組（BilateralMarket の前の四半期の組の取引 LastPairFlow[売り手][買い手] が正の組）：col＝買い手の nationUid・colName＝売り手の nationUid・qPerMonth＝取引の額（四半期の和）。
// ⚠限界（計器が自己申告する）：E の分母の需要は州の市場の LastDemand（月の値）＝国の需要の近似。C の行列は国の添字で引く（建国と消滅で詰まる＝その四半期の Nations の並びで uid に直す）。
//   列を持たない生産者（労働の財・役務・農家の手仕事・職人）は数えない。値段の重みは州の名目の値段（物価水準を割らない）。
public static partial class Instruments
{
    internal const string DscHeaderText = "kind\tquarter\tyear\tnationUid\tnation\tcol\tcolName\tgoodName\tisRgo\tqPerMonth\tvaluePerMonth\tdgPerMonth\tengHomePerMonth\thomePopK\tgateMax\ttechLevel\tbracket\treplacement3";

    sealed class DscSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DscSink> DscSinks = new();

    /// <summary>四半期の末（<see cref="Quarter"/> の国のループの後）：全ての国の R・N 行と国の組の C 行を書く。</summary>
    static void DscTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = DscSinks.GetValue(sim, _ => new DscSink());
        sk.W ??= Open("dsc1", seed);
        if (!sk.Header) { sk.W.WriteLine(DscHeaderText); sk.Header = true; }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int nc = Improvement.ColumnCount, ng = Catalog.Goods.Length;
        var qs = new double[nc]; var vs = new double[nc]; var es = new double[nc]; var gm = new double[nc]; var rgo = new bool[nc];
        var dg = new double[ng];
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World; var st = S(w);
            System.Array.Clear(qs); System.Array.Clear(vs); System.Array.Clear(es); System.Array.Clear(gm); System.Array.Clear(rgo); System.Array.Clear(dg);
            double homePop = 0;
            foreach (var r in w.Regions)
            {
                bool home = r.Tier != RegionTier.Colonial;
                if (home) foreach (var p in r.Pops) if (!p.IsSoldier && p.Size > 0) homePop += p.Size;
                for (int g = 0; g < ng && g < r.Markets.Length; g++) { double d = r.Markets[g].LastDemand; if (d > 0) dg[g] += d; }
                foreach (var pr in r.Producers)
                {
                    int k = Improvement.ColumnOf(pr);
                    if (k < 0) continue;
                    double gate = Discovery.Gate(r, pr);
                    if (gate > gm[k]) gm[k] = gate;
                    if (!(gate > 0)) continue;
                    double o = pr.Output;
                    if (o > 0)
                    {
                        qs[k] += o;
                        double px = pr.GoodId < r.Markets.Length ? r.Markets[pr.GoodId].Price : 0;
                        if (px > 0) vs[k] += o * px;
                    }
                    if (pr.IsRgo) rgo[k] = true;
                    if (home) es[k] += Discovery.EngineerUse(pr);
                }
            }
            string head = q.ToString(inv) + "\t" + w.Year.ToString(inv) + "\t" + st.Uid.ToString(inv) + "\t" + NameOf(w);
            sk.W.WriteLine("N\t" + head + "\t-1\t\t\t0\t0\t0\t0\t0\t" + F(homePop) + "\t0\t" + F(w.TechLevel) + "\t" + F(Improvement.BracketOf(w.TechLevel)) + "\t" + F(w.ReplacementRate(Calendar.MonthsPerQuarter)));
            for (int k = 0; k < nc; k++)
            {
                if (!(qs[k] > 0)) continue;
                int g = k < Catalog.Discoveries.Length ? Catalog.Discoveries[k].Good : k - Catalog.Discoveries.Length;
                double d = g >= 0 && g < ng ? dg[g] : 0;
                sb.Clear();
                sb.Append("R\t").Append(head).Append('\t').Append(k.ToString(inv)).Append('\t').Append(Improvement.ColumnName(k)).Append('\t')
                  .Append(g >= 0 && g < ng ? Catalog.Goods[g].Name : "").Append('\t').Append(rgo[k] ? '1' : '0').Append('\t')
                  .Append(F(qs[k])).Append('\t').Append(F(vs[k])).Append('\t').Append(F(d)).Append('\t').Append(F(es[k])).Append('\t').Append(F(homePop)).Append('\t').Append(F(gm[k]))
                  .Append("\t0\t0\t0");
                sk.W.WriteLine(sb.ToString());
            }
        }
        if (sim.Market.LastPairFlow is { } pf)
        {
            int m = System.Math.Min(pf.Length, sim.Nations.Count);
            for (int s = 0; s < m; s++)
            {
                var row = pf[s]; if (row is null) continue;
                for (int b = 0; b < m && b < row.Length; b++)
                {
                    double v = row[b];
                    if (!(v > 0) || s == b) continue;
                    var wb = sim.Nations[b].World; var ws = sim.Nations[s].World;
                    sk.W.WriteLine("C\t" + q.ToString(inv) + "\t" + wb.Year.ToString(inv) + "\t" + S(wb).Uid.ToString(inv) + "\t" + NameOf(wb) + "\t" + S(wb).Uid.ToString(inv) + "\t" + S(ws).Uid.ToString(inv)
                        + "\t" + NameOf(ws) + "\t0\t" + F(v) + "\t0\t0\t0\t0\t0\t0\t0\t0");
                }
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void DscClose(MultiNationSim sim)
    {
        if (DscSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
