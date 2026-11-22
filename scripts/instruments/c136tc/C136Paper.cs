// (1306) c135c3 の使い捨ての素案の確かめ（コミットしない）：TaxChainCheck と同じ盤面・同じ 3 つの助走で、
// 今の Assert の量（dSav・dSat・構成を固定した闘争度の差 dMil）と、素案の量（構成を固定した不満度の差 dDisc）を
// 勢いの重み 0.3／0 × 係数の突然変異（試験の中で w.Coef に置く＝SimCore は触らない）で並べる。出力は C136_OUT/paper.tsv に 1 行ずつ足す。
using System.Text;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

static class C136P
{
    static readonly string Out = Environment.GetEnvironmentVariable("C136_OUT") ?? "D:/PoB_work/c136tc_work/out";
    static readonly object Lock = new();
    static string F(double x) => x.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);

    public static void Go(string tag, double iew, Action<Coefficients> mut)
    {
        (double Sat, double Sav, double[] Mil, double[] Disc, double[] Size, double Paid, double[] Part) Run(double tax, int warmup)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            var w = sim.World;
            w.Coef.InvestExtrapolationWeight = iew; mut(w.Coef);
            w.Coef.AusterityStartDebt = new RatioToGdpYear(99);
            w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0); foreach (var rg in w.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = new MoneyStock(0);
            sim.Run(warmup);
            w.PollTax = tax; w.Policy.PollTaxTarget = tax;
            double satAvg = 0, savAvg = 0, paid = 0; var milC = new double[64]; var discC = new double[64]; var partC = new double[64]; var sizeC = new double[64];
            for (int q = 0; q < 20; q++)
            {
                sim.Run(1);
                var dc = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, w.Coef); var lg = w.Coef.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : new double[0];
                double sat = 0, size = 0, sav = 0;
                foreach (var p in w.AllPops())
                {
                    if (p.Closed || p.IsSoldier) continue;
                    sat += p.Satisfaction * p.Size; size += p.Size;
                    milC[p.ClassId] += p.Militancy * p.Size; discC[p.ClassId] += FactionSnapshot.DiscontentOf(w, p, dc, lg) * p.Size; partC[p.ClassId] += FactionSnapshot.Participation(w, p, dc, lg) * p.Size; sizeC[p.ClassId] += p.Size;
                    if (Politics.IsMassPop(p)) sav += p.Savings.Value;
                }
                satAvg += sat / System.Math.Max(1e-9, size); savAvg += sav; paid += w.LastTaxPollPerMonth.Value;
            }
            return (satAvg / 20, savAvg / 20, milC, discC, sizeC, paid / 20, partC);
        }
        static double Fixed(double[] lv, double[] ls, double[] hv, double[] hs)
        {
            double num = 0, den = 0;
            for (int c = 0; c < ls.Length; c++) { if (!(ls[c] > 0) || !(hs[c] > 0)) continue; num += ls[c] * (hv[c] / hs[c] - lv[c] / ls[c]); den += ls[c]; }
            return den > 0 ? num / den : 0;
        }
        var sb = new StringBuilder();
        double dSat = 0, dSav = 0, dMil = 0, dDisc = 0, dPart = 0;
        for (int step = 0; step < 3; step++)
        {
            int warmup = 20 + 40 * step;
            var l = Run(0.0, warmup); var h = Run(0.6, warmup);
            double s1 = h.Sat - l.Sat, s2 = (h.Sav - l.Sav) / System.Math.Max(1e-9, l.Sav), s3 = Fixed(l.Mil, l.Size, h.Mil, h.Size), s4 = Fixed(l.Disc, l.Size, h.Disc, h.Size);
            dSat += s1; dSav += s2; dMil += s3; dDisc += s4; double s5 = Fixed(l.Part, l.Size, h.Part, h.Size); dPart += s5;
            sb.Append($"{tag}\t{F(iew)}\tw{warmup}\t{F(s2)}\t{F(s1)}\t{F(s3)}\t{F(s4)}\t{F(s5)}\n");
        }
        sb.Append($"{tag}\t{F(iew)}\tsum\t{F(dSav)}\t{F(dSat)}\t{F(dMil)}\t{F(dDisc)}\t{F(dPart)}\n");
        lock (Lock) { Directory.CreateDirectory(Out); File.AppendAllText($"{Out}/paper.tsv", sb.ToString().Replace("\r", "")); }
    }
}

public class C136P_M0a { [Fact] public void Go() => C136P.Go("M0", 0.3, c => { }); }
public class C136P_M0b { [Fact] public void Go() => C136P.Go("M0", 0.0, c => { }); }
public class C136P_P1a { [Fact] public void Go() => C136P.Go("P+1e-8", 0.3, c => c.SupplySlope = 0.50000001); }
public class C136P_P1b { [Fact] public void Go() => C136P.Go("P+1e-8", 0.0, c => c.SupplySlope = 0.50000001); }
public class C136P_P2a { [Fact] public void Go() => C136P.Go("P-1e-8", 0.3, c => c.SupplySlope = 0.49999999); }
public class C136P_P2b { [Fact] public void Go() => C136P.Go("P-1e-8", 0.0, c => c.SupplySlope = 0.49999999); }
public class C136P_M1a { [Fact] public void Go() => C136P.Go("M1_WantWeight0", 0.3, c => c.WantWeight = 0); }
public class C136P_M1b { [Fact] public void Go() => C136P.Go("M1_WantWeight0", 0.0, c => c.WantWeight = 0); }
public class C136P_M2a { [Fact] public void Go() => C136P.Go("M2_FamineGain0", 0.3, c => c.FamineDriveGain = 0); }
public class C136P_M2b { [Fact] public void Go() => C136P.Go("M2_FamineGain0", 0.0, c => c.FamineDriveGain = 0); }
public class C136P_M3a { [Fact] public void Go() => C136P.Go("M3_LivingRefSpeed1", 0.3, c => c.LivingRefSpeed = 1.0); }
public class C136P_M3b { [Fact] public void Go() => C136P.Go("M3_LivingRefSpeed1", 0.0, c => c.LivingRefSpeed = 1.0); }
public class C136P_M4a { [Fact] public void Go() => C136P.Go("M4_WantWeight_neg", 0.3, c => c.WantWeight = -0.8); }
public class C136P_M4b { [Fact] public void Go() => C136P.Go("M4_WantWeight_neg", 0.0, c => c.WantWeight = -0.8); }
