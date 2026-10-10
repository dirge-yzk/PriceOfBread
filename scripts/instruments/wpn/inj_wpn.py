"""[計測の写しだけ] (153) 新兵器の発見と前期の装備の細りの計器（読むだけ）。環境変数 POB_WPN_DIR の下に w<seed>.txt。未設定なら1命令も走らない。
  M 行＝年に1回（q%4==3）の国ごと：年・国（BaseName）・即応度・装備の充足（首都州）・陸軍力・海軍力・兵士・人口・技術水準・軍事費の段・
        首都州の軍需の予算・予算の上限・支払能力の生の比（年の平均）・規模・平時の規模・交戦中か
  E 行＝年に1回の国ごと×基本段の軍需財：年の和＝必要量・発注・納入・充足の寄与・値段（月の平均）・参照×物価水準（月の平均）・国の産出（四半期末）
  （disc のとき）D 行＝国がその四半期に初めて既知にした発見の行＋その時点の3項（Evaluate・includeKnown）・技術水準・陸と海の脅威の割合・交戦中か
  （disc のとき）W 行＝年に1回の国ごと×新兵器の行：3項（Evaluate・includeKnown）・既知か
  （disc のとき）L 行＝年に1回の国ごと：陸と海の脅威の割合・交戦中の自国の戦死の和（今の戦争の台帳）÷兵士
使い方：python inj_wpn.py <木の根> [eq|disc]（disc＝発見の欄のある木＝(147) 以後）。s3a・ghost・navy の後に当ててよい（SelfPlay の StepQuarter の行の直後に足す）。"""
import sys
root = sys.argv[1]
mode = sys.argv[2] if len(sys.argv) > 2 else 'disc'

def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

DISC_DECL = r'''
        readonly System.Collections.Generic.Dictionary<World, bool[]> Seen = new();
'''
DISC_BODY = r'''
                var known = w.Discovered;
                if (!Seen.TryGetValue(w, out var seen)) { seen = new bool[known.Length]; Seen[w] = seen; }
                var lossArr = w.LastDiscoveryView is { } lv ? lv.GetType().GetProperty("Loss")?.GetValue(lv) as double[] : null;
                string LS(int d) => lossArr is not null && d < lossArr.Length ? F(lossArr[d]) : "0";
                if (lossArr is not null && lossArr.Length > 3 && (lossArr[1] > 0 || lossArr[2] > 0 || lossArr[3] > 0))
                    W.Write($"Q\t{q + 1}\t{yr}\t{bn}\t{LS(1)}\t{LS(2)}\t{LS(3)}\n");
                Discovery.Terms[]? terms = null;
                for (int r = 0; r < known.Length && r < Catalog.Discoveries.Length; r++)
                    if (known[r] && !seen[r])
                    {
                        seen[r] = true;
                        terms ??= q == 0 ? null : Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true);
                        var t = terms is null ? default : terms[r];
                        W.Write($"D\t{q + 1}\t{yr}\t{nn}\t{bn}\t{Catalog.Discoveries[r].Name}\t{Catalog.Discoveries[r].Field}\t{F(t.Tech)}\t{F(t.Need)}\t{F(t.Transmit)}\t{F(w.TechLevel)}\t{F(sl)}\t{F(ss)}\t{(atWar ? 1 : 0)}\t{F(deadFrac)}\t{LS(1)}\t{LS(2)}\t{LS(3)}\n");
                    }
                if (q % 4 == 3)
                {
                    var tt = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true);
                    for (int r = 0; r < tt.Length && r < Catalog.Discoveries.Length; r++)
                    {
                        if (Catalog.Discoveries[r].Field != DiscoveryField.Weapon) continue;
                        W.Write($"W\t{q + 1}\t{yr}\t{bn}\t{Catalog.Discoveries[r].Name}\t{(known[r] ? 1 : 0)}\t{F(tt[r].Tech)}\t{F(tt[r].Need)}\t{F(tt[r].Transmit)}\n");
                    }
                }
'''

TAIL = r'''

/// <summary>[計測の写しだけ] (153) 新兵器の発見と装備の充足の計器（読むだけ）。</summary>
public static class WpnDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_WPN_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Acc
    {
        public readonly double[] Want = new double[16], Dem = new double[16], Got = new double[16], Con = new double[16], Px = new double[16], Ref = new double[16];
        public readonly double[] RF = new double[16], RO = new double[16], RR = new double[16], RN = new double[16], PF = new double[16], PO = new double[16], PR = new double[16], PN = new double[16];
        public int Months; public double Afford, Budget, Ceil, Scale, ScalePeace;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, Acc> ByRegion = new();
    public static void Rec(Region region, int b, int gi, double want, double dem, double got, double con, double px)
    {
        var a = ByRegion.GetValue(region, _ => new Acc());
        if (b >= 16) return;
        if (b == 0) { a.Months++; a.Afford += region.MilitaryAffordRaw; a.Budget += region.MilitaryBudget; a.Ceil += region.MilitaryBudgetCeiling; a.Scale += region.MilitaryScale; a.ScalePeace += region.MilitaryScalePeace; }
        a.Want[b] += want; a.Dem[b] += dem; a.Got[b] += got; a.Con[b] += con; a.Px[b] += px;
        if (want > 1e-12)
        {
            double f = con / want, o = dem / want, r = dem > 1e-12 ? got / dem : 1.0;
            a.RF[b] += f; a.RO[b] += o; a.RR[b] += r; a.RN[b] += 1;
            if (!(region.MilitaryWarMultLast > 1.0)) { a.PF[b] += f; a.PO[b] += o; a.PR[b] += r; a.PN[b] += 1; }
        }
        if (gi >= 0 && gi < Catalog.RefPrices.Length) a.Ref[b] += Catalog.RefPrices[gi] * region.PriceLevel;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, Match> BySim = new();
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        if (!On) return;
        BySim.GetValue(sim, _ => new Match(seed)).Quarter(sim, q, quarters);
    }
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
__DISC_DECL__
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"w{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#M\tq\tyr\tbasename\treadiness\tequipfill\tland\tnaval\tsoldiers\tpop\ttech\tmilspend\tbudget\tceil\taffordRaw\tscale\tscalePeace\tatwar\n");
            W.Write("#E\tq\tyr\tbasename\tgood\twant\tdem\tgot\tcon\tpx\trefPL\tnatProd\tfillAvg\tordAvg\tratAvg\tfillPeace\tordPeace\tratPeace\tnPeace\tnAll\n");
            W.Write("#D\tq\tyr\tname\tbasename\trow\tfield\ttech\tneed\ttrans\ttechLevel\tlandSh\tseaSh\tatwar\tdeadFrac\tlossL\tlossS\tlossA\n");
            W.Write("#Q\tq\tyr\tbasename\tlossL\tlossS\tlossA\n");
            W.Write("#W\tq\tyr\tbasename\trow\tknown\ttech\tneed\ttrans\n");
            W.Write("#L\tq\tyr\tbasename\tlandSh\tseaSh\tatwar\tdeadFrac\tneedL\tlandP\tneedS\tnavalP\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations; int yr = ns[0].World.Year;
            for (int i = 0; i < ns.Count; i++)
            {
                var w = ns[i].World;
                string nn = w.SelfPower?.Name ?? w.Region.Name;
                string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : nn;
                var sp = w.SelfPower;
                double sl = sp is null ? double.NaN : (sp.NeedLand > 1e-12 ? sp.NeedLand / (sp.NeedLand + System.Math.Max(0, sp.LandPower)) : 0);
                double ss = sp is null ? double.NaN : (sp.NeedSea > 1e-12 ? sp.NeedSea / (sp.NeedSea + System.Math.Max(0, sp.NavalPower)) : 0);
                bool atWar = false; double dead = 0;
                if (sp is not null && w.Diplomacy is { } dip)
                    foreach (var war in dip.Wars)
                    {
                        if (war.A == sp) { atWar = true; dead += war.DeadA; }
                        else if (war.B == sp) { atWar = true; dead += war.DeadB; }
                    }
                double sold = 0, pop = 0;
                foreach (var rg in w.Regions) { if (rg.State != RegionState.Active) continue; foreach (var p in rg.Pops) { pop += p.Size; if (p.IsSoldier) sold += p.Size; } }
                double deadFrac = dead > 0 ? dead / (dead + sold) : 0;
__DISC_BODY__
                if (q % 4 == 3)
                {
                    W.Write($"L\t{q + 1}\t{yr}\t{bn}\t{F(sl)}\t{F(ss)}\t{(atWar ? 1 : 0)}\t{F(deadFrac)}\t{F(sp?.NeedLand ?? double.NaN)}\t{F(sp?.LandPower ?? double.NaN)}\t{F(sp?.NeedSea ?? double.NaN)}\t{F(sp?.NavalPower ?? double.NaN)}\n");
                    var cap = w.Region;
                    ByRegion.TryGetValue(cap, out var a);
                    double mo = a is null || a.Months == 0 ? double.NaN : a.Months;
                    double land = sp?.LandPower ?? double.NaN, naval = sp?.NavalPower ?? double.NaN;
                    W.Write($"M\t{q + 1}\t{yr}\t{bn}\t{F(w.MilitaryReadiness)}\t{F(cap.MilitaryEquipFill)}\t{F(land)}\t{F(naval)}\t{F(sold)}\t{F(pop)}\t{F(w.TechLevel)}\t{F(w.MilitarySpending)}\t{F((a?.Budget ?? double.NaN) / mo)}\t{F((a?.Ceil ?? double.NaN) / mo)}\t{F((a?.Afford ?? double.NaN) / mo)}\t{F((a?.Scale ?? double.NaN) / mo)}\t{F((a?.ScalePeace ?? double.NaN) / mo)}\t{(atWar ? 1 : 0)}\n");
                    if (a is not null && a.Months > 0)
                    {
                        var mg = w.Coef.MilitaryGoods;
                        for (int b = 0; b < mg.Length && b < 16; b++)
                        {
                            int g = mg[b]; double np = 0;
                            foreach (var rg in w.Regions) if (rg.State == RegionState.Active && g < rg.Markets.Length) np += rg.Markets[g].LastProduced;
                            W.Write($"E\t{q + 1}\t{yr}\t{bn}\t{Catalog.Goods[g].Name}\t{F(a.Want[b])}\t{F(a.Dem[b])}\t{F(a.Got[b])}\t{F(a.Con[b])}\t{F(a.Px[b] / a.Months)}\t{F(a.Ref[b] / a.Months)}\t{F(np)}\t{F(a.RF[b] / a.RN[b])}\t{F(a.RO[b] / a.RN[b])}\t{F(a.RR[b] / a.RN[b])}\t{F(a.PF[b] / a.PN[b])}\t{F(a.PO[b] / a.PN[b])}\t{F(a.PR[b] / a.PN[b])}\t{F(a.PN[b])}\t{F(a.RN[b])}\n");
                        }
                    }
                    if (a is not null) ByRegion.Remove(cap);
                }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
TAIL = TAIL.replace('__DISC_DECL__', DISC_DECL if mode == 'disc' else '').replace('__DISC_BODY__', DISC_BODY if mode == 'disc' else '')
edit('PriceOfBread.SimCore/EconomyStep.cs', [
    ('                equipGot += ArmsFillContribution(region, coef, coef.MilitaryGoods, b, want, got, milRates[b], region.MilitaryScalePeace, mul, baseStock ? region.ArmsStock : null, opening);\n',
     '                { double __wc = ArmsFillContribution(region, coef, coef.MilitaryGoods, b, want, got, milRates[b], region.MilitaryScalePeace, mul, baseStock ? region.ArmsStock : null, opening); equipGot += __wc; if (WpnDiag.On) WpnDiag.Rec(region, b, gi, want, militaryDemand[gi], got, __wc, clearPrice[gi]); }\n'),
])
edit('PriceOfBread.SimCore/Discovery.cs' if mode == 'disc' else 'PriceOfBread.SimCore/EconomyStep.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('                sim.StepQuarter();\n',
     '                sim.StepQuarter();\n                WpnDiag.Quarter(sim, seed, q, quarters);\n'),
])
print('ok', root, mode)
