namespace PriceOfBread.SimCore;

// INS=1（Instruments.On）かつ POB_INSTRUMENTS_DSC=1 のときだけ出す（既定は DscTable を呼ばない＝1 命令も走らない・1 種 300〜415 MB のため）。〔履歴 InstrumentsDsc.cs#1〕
public static partial class Instruments
{
    internal const string DscHeaderText = "kind\tquarter\tyear\tnationUid\tnation\tcol\tcolName\tgoodName\tisRgo\tqPerMonth\tvaluePerMonth\tdgPerMonth\tengHomePerMonth\thomePopK\tgateMax\ttechLevel\tbracket\treplacement3\tvalueRefPerMonth\tsteamFillMeanShare\telecFillMeanShare\tpopK\tnominalGdpPerMonth\tfxRate";

    /// <summary>
    /// 〔履歴 InstrumentsDsc.cs#2〕
    /// </summary>
    internal static readonly bool DscOn = System.Environment.GetEnvironmentVariable("POB_INSTRUMENTS_DSC") == "1";

    sealed class DscSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DscSink> DscSinks = new();

    /// <summary>四半期の末（<see cref="Quarter"/> の国のループの後）：全ての国の R・N 行と国の組の C 行を書く。</summary>
    static void DscTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = DscSinks.GetValue(sim, _ => new DscSink());
        sk.W ??= Open("dsc2", seed);
        if (!sk.Header) { sk.W.WriteLine(DscHeaderText); sk.Header = true; }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int nc = Improvement.ColumnCount, ng = Catalog.Goods.Length;
        var qs = new double[nc]; var vs = new double[nc]; var es = new double[nc]; var gm = new double[nc]; var rgo = new bool[nc];
        var vrs = new double[nc]; var sfs = new double[nc]; var efs = new double[nc];
        var dg = new double[ng];
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World; var st = S(w);
            System.Array.Clear(qs); System.Array.Clear(vs); System.Array.Clear(es); System.Array.Clear(gm); System.Array.Clear(rgo); System.Array.Clear(dg);
            System.Array.Clear(vrs); System.Array.Clear(sfs); System.Array.Clear(efs);
            double homePop = 0, allPop = 0;
            foreach (var r in w.Regions)
            {
                bool home = r.Tier != RegionTier.Colonial;
                foreach (var p in r.Pops) if (!p.IsSoldier && p.Size > 0) { allPop += p.Size; if (home) homePop += p.Size; }
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
                        double rp = pr.GoodId < Catalog.RefPrices.Length ? Catalog.RefPrices[pr.GoodId] : 0;
                        if (rp > 0) vrs[k] += o * rp;
                        sfs[k] += o * pr.SteamFill; efs[k] += o * pr.ElecFill;
                    }
                    if (pr.IsRgo) rgo[k] = true;
                    if (home) es[k] += Discovery.EngineerUse(pr);
                }
            }
            string head = q.ToString(inv) + "\t" + w.Year.ToString(inv) + "\t" + st.Uid.ToString(inv) + "\t" + NameOf(w);
            sk.W.WriteLine("N\t" + head + "\t-1\t\t\t0\t0\t0\t0\t0\t" + F(homePop) + "\t0\t" + F(w.TechLevel) + "\t" + F(Improvement.BracketOf(w.TechLevel)) + "\t" + F(w.ReplacementRate(Calendar.MonthsPerQuarter))
                + "\t0\t0\t0\t" + F(allPop) + "\t" + F(w.NominalGdpPerMonth.Value) + "\t" + F(w.FxRate));
            for (int k = 0; k < nc; k++)
            {
                if (!(qs[k] > 0)) continue;
                int g = k < Catalog.Discoveries.Length ? Catalog.Discoveries[k].Good : k - Catalog.Discoveries.Length;
                double d = g >= 0 && g < ng ? dg[g] : 0;
                sb.Clear();
                sb.Append("R\t").Append(head).Append('\t').Append(k.ToString(inv)).Append('\t').Append(Improvement.ColumnName(k)).Append('\t')
                  .Append(g >= 0 && g < ng ? Catalog.Goods[g].Name : "").Append('\t').Append(rgo[k] ? '1' : '0').Append('\t')
                  .Append(F(qs[k])).Append('\t').Append(F(vs[k])).Append('\t').Append(F(d)).Append('\t').Append(F(es[k])).Append('\t').Append(F(homePop)).Append('\t').Append(F(gm[k]))
                  .Append("\t0\t0\t0\t").Append(F(vrs[k])).Append('\t').Append(F(sfs[k] / qs[k])).Append('\t').Append(F(efs[k] / qs[k])).Append("\t0\t0\t0");
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
                        + "\t" + NameOf(ws) + "\t0\t" + F(v) + "\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0");
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
