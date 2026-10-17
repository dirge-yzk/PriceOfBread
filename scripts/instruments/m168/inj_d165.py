# ★(377) 2026-10-16 以降の木では使えない＝w.InstitutionalDecay は (373) で物理削除した（再走は削除の前の木で）
# ★(387) 同じく InnovationAdminThreshold・前線の門の行政の連言は (383) で物理削除した＝注入する FrontierCapable の写しは今の木と合わない（黙って食い違う）
"""[計測の写しだけ] (165) 線D 後半の計器（読むだけ）。環境変数 POB_D165_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
単独でも、他の計器（s3a・ghost・gate…）を当てた木にも当てられる（SelfPlay の sim.StartWarmup() と sim.StepQuarter() の直後に足す）。
  N 行＝年に1回（q%4==3）の国ごと：年・国（BaseName）・人口・技術水準・世界の技術水準・Elite・EliteCore・教育法・報道法・自由・行政効率・制度疲労・接触度・
        革新の資格（GrowTech の FrontierCapable を写した判定）・基礎の目標・後追いの目標（GrowTech と同じ式の写し）・
        鉄道の容量・船の容量・内陸の州の潜在量の和・港の州の潜在量の和・内陸の州の容量の和・陸の通した割合・陸の運賃・鉄道の輸送の費用・
        機関車の2値と3項・蒸気機関の2値と3項・動力の平滑化した値段・蒸気の動力の費用・輸送の目標（人口×TransportTargetPerPop）・脅威比
使い方：python inj_d165.py <木の根>"""
import sys
root = sys.argv[1]

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

TAIL = r'''

/// <summary>[計測の写しだけ] (165) 線D 後半の計器（読むだけ）。</summary>
public static class D165Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_D165_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#N\tq\tyr\tnation\tpop\ttech\twtech\telite\teliteCore\tedu\tcens\tfree\tadmin\tdecay\tcontact\tfront\tbaseT\tcatchT\trailCap\tshipCap\tpotIn\tpotPort\tcapIn\tserved\tlandPx\trailCost\tkLoco\tlT\tlN\tlX\tkSteam\tsT\tsN\tsX\tpower\tsteamCost\ttgt\tthreat\tstance\tuMod\tuInd\tuMil\tdoct\teduTgt\teduNet\teduOk\teduWhy\n");
        }
        readonly System.Collections.Generic.HashSet<Producer> siteSeen = new();
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            foreach (var nsS in sim.Nations)
            {
                var wS = nsS.World;
                string bnS = wS.SelfPower is { } spS && spS.BaseName.Length > 0 ? spS.BaseName : (wS.SelfPower?.Name ?? wS.Region.Name);
                foreach (var rg in wS.Regions)
                    foreach (var pS in rg.Producers)
                        if (pS.SiteRow >= 0 && pS.SiteFound && siteSeen.Add(pS))
                            W.Write($"S\t{q + 1}\t{wS.Year}\t{bnS}\t{rg.Name}\t{Catalog.Sites[pS.SiteRow].Name}\n");
            }
            if (q % 4 == 3)
            {
                var ns = sim.Nations;
                int nr = Catalog.Discoveries.Length, rL = Discovery.LocomotiveRow, rS = Discovery.SteamRow;
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World; var c = w.Coef;
                    string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : (w.SelfPower?.Name ?? w.Region.Name);
                    double pop = 0, potIn = 0, potPort = 0, capIn = 0;
                    foreach (var rg in w.Regions)
                    {
                        if (rg.State != RegionState.Active) continue;
                        foreach (var p in rg.Pops) pop += p.Size;
                        if (rg.IsPort) potPort += rg.LastTradePotential;
                        else { potIn += rg.LastTradePotential; capIn += rg.LastTransportCap; }
                    }
                    bool front = (w.Freedom >= c.FrontierFreedomThreshold || (c.InnovationEliteSubstitute > 0 && w.Elite >= c.EliteForFullTech && w.AdminEfficiency >= c.InnovationAdminThreshold))
                                 && w.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory;
                    double baseT = c.AdoptionCeiling * System.Math.Clamp(w.Elite / c.EliteForFullTech, 0, 1);
                    double catchFrac = front ? c.CatchupFractionAdvanced.Value : c.CatchupFraction.Value;
                    double contact = c.CatchupContactWeight > 0 ? 1.0 - c.CatchupContactWeight * (1.0 - System.Math.Clamp(w.TradeContactShare, 0, 1)) : 1.0;
                    double adoptionCeil = System.Math.Max(c.AdoptionCeiling, w.WorldTechLevel * catchFrac * contact);
                    double catchT = baseT + System.Math.Max(0, adoptionCeil - c.AdoptionCeiling) * System.Math.Clamp(w.Elite / c.EliteForCatchup, 0, 1);
                    var ro = new (double Replaced, double Cost)[nr];
                    var t = Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true, ratioOut: ro);
                    double served = Discovery.LandFreightServed(w), landPx = Discovery.LandFreightPrice(w), railCost = (Discovery.LocomotiveRow >= 0 && Discovery.LocomotiveRow < ro.Length ? ro[Discovery.LocomotiveRow].Cost : double.NaN);
                    double power = w.DiscoveryPrice is { } dp && Catalog.LaborId < dp.Length ? dp[Catalog.LaborId] : double.NaN;
                    double threat = w.SecurityThreatOwnMass > 1e-9 ? w.SecurityThreat / w.SecurityThreatOwnMass : 0;
                    var k = w.Discovered;
                    var stance = UtilityAgent.ChooseStance(w, out var bd);
                    string doct = UtilityAgent.DoctrineFrom(w).ToString();
                    double eduNet = PolicyPreferences.NetSupport(w, PolicyAxis.EducationLaw, true);
                    var eduG = Government.GateInfo(w, PolicyAxis.EducationLaw, true, (int)EducationLaw.Public);
                    W.Write($"N\t{q + 1}\t{w.Year}\t{bn}\t{F(pop)}\t{F(w.TechLevel)}\t{F(w.WorldTechLevel)}\t{F(w.Elite)}\t{F(w.EliteCore)}\t{w.EducationLaw}\t{w.Censorship}\t{F(w.Freedom)}\t{F(w.AdminEfficiency)}\t{F(w.InstitutionalDecay)}\t{F(w.TradeContactShare)}\t{(front ? 1 : 0)}\t{F(baseT)}\t{F(catchT)}\t{F(w.RailCapacity)}\t{F(w.ShipCapacity)}\t{F(potIn)}\t{F(potPort)}\t{F(capIn)}\t{F(served)}\t{F(landPx)}\t{F(railCost)}\t{(Discovery.Known(k, rL) ? 1 : 0)}\t{F(t[rL].Tech)}\t{F(t[rL].Need)}\t{F(t[rL].Transmit)}\t{(Discovery.Known(k, rS) ? 1 : 0)}\t{F(t[rS].Tech)}\t{F(t[rS].Need)}\t{F(t[rS].Transmit)}\t{F(power)}\t{F(ro[rS].Cost)}\t{F(w.TotalPopulation * c.TransportTargetPerPop)}\t{F(threat)}\t{stance}\t{F(bd.UMod)}\t{F(bd.UInd)}\t{F(bd.UMil)}\t{doct}\t{w.Policy.EducationLaw}\t{F(eduNet)}\t{(eduG.Ok ? 1 : 0)}\t{eduG.Reason}\n");
                }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n', '        sim.StartWarmup();\n        var d165M = D165Diag.On ? new D165Diag.Match(seed) : null;\n'),
    ('sim.StepQuarter();\n', 'sim.StepQuarter();\n                d165M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
