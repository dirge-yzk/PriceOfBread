"""[計測の写しだけ] (184) 農奴解放の法の経路の計器（読むだけ）。環境変数 POB_SERF184_DIR の下に s<seed>.txt。未設定なら1命令も走らない。
  Q 行＝四半期ごとに、農奴解放が全面でない国（と、その四半期に全面へ動いた国）：
        四半期・年・国・現在の段・目標の段・政体・主食充足・AI の飢饉の判定・余地（今の上限に対して＝AI が読む量）・余地（次の段の上限に対して）・
        純支持（上げる向き）・門（GateInfo＝次の段・1 開）・代価・政治資本の残り・正統性・ブロックの力 t/m/s・断裂の窓・交戦中・安全保障の不安の割合・厭戦・財源の充足・国債÷年GDP
  E 行＝出来事：
        A＝AI の目標（SelfPlay の農奴解放の段落）＝条件が立った四半期の 飢饉/余地・目標の前後
        G＝施行（Government.Step の段の法のループ）で現在の段が動いた＝前後と純支持・代価・残り
        F＝勢力の要求をのむ（ConcedeToFaction＝直接の施行）で動いた
        C＝危機の要求（Parliament.TargetCrisisDemands）で目標が動いた（一揆の譲歩の記録か運動の危機か）
        R＝一揆の譲歩の梯子が農奴解放を選んだ
        P＝政体の変更（ExecuteRegimeChange）＝前後・暴力
inj_navy を先に当てた木に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_serf184.py <木の根>"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from edlib import edit
root = sys.argv[1]

def tail(rel, text):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    if (CR + LF) in s: text = text.replace(LF, CR + LF)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + (s + text).encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (184) 農奴解放の法の経路（読むだけ）。</summary>
public static class Serf184Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_SERF184_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Acc { public readonly System.Collections.Generic.List<string> Ev = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Acc> ByDip = new();
    static Acc? For(object? dip) => dip is null ? null : ByDip.GetValue(dip, _ => new Acc());
    static string Nm(World? w) => w?.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "-";
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    static void Add(World w, string s) { if (!On || For(w.Diplomacy) is not { } acc) return; lock (acc) acc.Ev.Add($"E\t{w.Year}\t{Nm(w)}\t" + s); }
    public static void Ai(World w, bool famine, double room, int before, int after) => Add(w, $"A\t{(famine ? 1 : 0)}\t{F(room)}\t{before}\t{after}");
    public static void Crisis(World w, int before, int after, int pendingRevolt, bool factionCrisis) { if (before != after) Add(w, $"C\t{before}\t{after}\t{pendingRevolt}\t{(factionCrisis ? 1 : 0)}"); }
    public static void Forced(World w, int before) { if ((int)w.SerfEmancipation != before) Add(w, $"F\t{before}\t{(int)w.SerfEmancipation}"); }
    public static void Revolt(World w) => Add(w, $"R\t{(int)w.SerfEmancipation}");
    public static void Regime(World w, Polity before, Polity after, bool violent) => Add(w, $"P\t{before}\t{after}\t{(violent ? 1 : 0)}\t{(int)w.SerfEmancipation}");
    /// <summary>施行の前の照会（段の法のループの前・目標が現在より上の時だけ）。</summary>
    public static (double net, double cost, double rem) Pre(World w)
    {
        if (!On || w.SelfPower is null || (int)w.Policy.SerfEmancipation <= (int)w.SerfEmancipation) return (double.NaN, double.NaN, double.NaN);
        var e = PolicyPreferences.Build(w);
        double net = PolicyPreferences.NetSupport(w, PolicyAxis.SerfEmancipation, true, e);
        double cost = net < 0 ? -net * w.Coef.MandateSpendRate : 0;
        return (net, cost, w.PoliticalCapitalMax - w.MandateSpent);
    }
    public static void Enact(World w, int before, (double net, double cost, double rem) pre)
    {
        if (!On || (int)w.SerfEmancipation == before) return;
        Add(w, $"G\t{before}\t{(int)w.SerfEmancipation}\t{F(pre.net)}\t{F(pre.cost)}\t{F(pre.rem)}\t{w.Polity}\t{F(w.Legitimacy)}");
    }
    static double Cap(int lv, double full) => lv >= 2 ? full : lv == 1 ? full + (1.0 - full) * 0.5 : 1.0;
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        readonly System.Collections.Generic.HashSet<World> _done = new();
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"s{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#Q\tq\tyr\tnation\tcur\ttgt\tpolity\tfood\tfamine\troomCur\troomNext\tnet\tgate\tcost\trem\tlegit\tpt\tpm\tps\trupt\twar\tsec\twarEx\tfund\tdebt\n");
            W.Write("#E\tyr\tnation\tkind\t...\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            foreach (var ns in sim.Nations)
            {
                var w = ns.World;
                if (w.SelfPower is not { } sp || !(w.TotalPopulation > 0)) continue;
                int cur = (int)w.SerfEmancipation;
                if (cur >= 2) { if (_done.Contains(w)) continue; _done.Add(w); }
                int tgt = (int)w.Policy.SerfEmancipation;
                var c = w.Coef;
                bool famine = !(w.FoodFill >= c.DynMandateFoodAnchor);
                double lrCap = Cap((int)w.LandReform, c.LandReformRentCap);
                double capCur = System.Math.Min(lrCap, Cap(cur, c.SerfEmancipationRentCap));
                double capNext = System.Math.Min(lrCap, Cap(System.Math.Min(2, cur + 1), c.SerfEmancipationRentCap));
                double rr = 0, rn = 0; int n = 0;
                foreach (var rg in w.Regions)
                {
                    if (rg.State != RegionState.Active) continue;
                    foreach (var pr in rg.Producers)
                    {
                        if (!pr.IsRgo) continue;
                        n++;
                        if (pr.RentShare > rg.FarmRentCap) rr += pr.RentShare - rg.FarmRentCap;
                        if (pr.RentShare > capNext) rn += pr.RentShare - capNext;
                    }
                }
                double roomCur = n > 0 ? rr / n : 0, roomNext = n > 0 ? rn / n : 0;
                double net = double.NaN, cost = double.NaN; int gate = -1;
                double pt = double.NaN, pm = double.NaN, ps = double.NaN;
                if (cur < 2)
                {
                    PolicyPreferences.Profile? sh = PolicyPreferences.Build(w);
                    var e = sh.Value;
                    pt = e.Power[0]; pm = e.Power[1]; ps = e.Power[2];
                    net = PolicyPreferences.NetSupport(w, PolicyAxis.SerfEmancipation, true, e);
                    var gi = Government.GateInfo(w, PolicyAxis.SerfEmancipation, true, cur + 1, ref sh);
                    gate = gi.Ok ? 1 : 0; cost = gi.Cost;
                }
                double ngdp = w.NominalGdp;
                double debt = ngdp > 1e-9 ? w.GovDebt / (ngdp * 12.0) : 0;
                W.Write($"Q\t{q}\t{w.Year}\t{Nm(w)}\t{cur}\t{tgt}\t{w.Polity}\t{F(w.FoodFill)}\t{(famine ? 1 : 0)}\t{F(roomCur)}\t{F(roomNext)}\t{F(net)}\t{gate}\t{F(cost)}\t{F(w.PoliticalCapitalMax - w.MandateSpent)}\t{F(w.Legitimacy)}\t{F(pt)}\t{F(pm)}\t{F(ps)}\t{w.RuptureWindowQuarters}\t{(Government.AtWar(w) ? 1 : 0)}\t{F(UtilityAgent.SecurityShare(w))}\t{F(sp.WarExhaustion)}\t{F(w.FiscalFundingRatio)}\t{F(debt)}\n");
            }
            if (q == quarters - 1)
            {
                if (For(sim.Diplomacy) is { } acc) foreach (var s in acc.Ev) W.Write(s + "\n");
                W.Dispose();
            }
        }
    }
}
'''
tail('PriceOfBread.SimCore/Discovery.cs', TAIL)
edit(root, 'PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var serf184M = Serf184Diag.On ? new Serf184Diag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                serf184M?.Quarter(sim, q, quarters);\n'),
    ("StepLawToward(w, p, PolicyAxis.SerfEmancipation, (int)p.SerfEmancipation, (int)p.SerfEmancipation + 1, 2, v => p.SerfEmancipation = (ReformLevel)v);",
     "{ int __serf0 = (int)p.SerfEmancipation; StepLawToward(w, p, PolicyAxis.SerfEmancipation, (int)p.SerfEmancipation, (int)p.SerfEmancipation + 1, 2, v => p.SerfEmancipation = (ReformLevel)v); if (Serf184Diag.On && (int)p.SerfEmancipation != __serf0) Serf184Diag.Ai(w, famine, room, __serf0, (int)p.SerfEmancipation); }"),
    ("""        RelieveOpulence(w);
        Parliament.TargetCrisisDemands(w);
""",
     """        RelieveOpulence(w);
        int __serfC0 = (int)p.SerfEmancipation; int __serfPR = w.PendingRevoltDemand; bool __serfFC = w.PendingFactionCrisis is not null;
        Parliament.TargetCrisisDemands(w);
        if (Serf184Diag.On) Serf184Diag.Crisis(w, __serfC0, (int)p.SerfEmancipation, __serfPR, __serfFC);
"""),
])
# A 行は条件が立った四半期に目標の前（後は Q 行の tgt で読む）。
edit(root, 'PriceOfBread.SimCore/Government.cs', [
    ("""        foreach (var law in LevelLaws)
        {
            if (law.Open != null && !law.Open(w)) continue;
            int target = law.Target(w, p);
""",
     """        int __serfG0 = (int)w.SerfEmancipation; var __serfPre = Serf184Diag.On ? Serf184Diag.Pre(w) : (double.NaN, double.NaN, double.NaN);
        foreach (var law in LevelLaws)
        {
            if (law.Open != null && !law.Open(w)) continue;
            int target = law.Target(w, p);
"""),
    ("""            if (law.Enact != null) law.Enact(w, p, target);
            else law.Set(w, StepLevel(law.Get(w), target, w, law.Axis));
        }
""",
     """            if (law.Enact != null) law.Enact(w, p, target);
            else law.Set(w, StepLevel(law.Get(w), target, w, law.Axis));
        }
        if (Serf184Diag.On) Serf184Diag.Enact(w, __serfG0, __serfPre);
"""),
])
edit(root, 'PriceOfBread.SimCore/Politics.cs', [
    ("""        var demands = FactionSnapshot.ComputeDemands(w, f.Bloc);
        int steps = 0;
        for (int i = 0; i < demands.Count; i++)
            steps += ApplyDemand(w, c, demands[i].Axis, demands[i].Increase, i == 0 ? c.FactionConcessionSteps : 1);
""",
     """        var demands = FactionSnapshot.ComputeDemands(w, f.Bloc);
        int steps = 0;
        int __serfF0 = (int)w.SerfEmancipation;
        for (int i = 0; i < demands.Count; i++)
            steps += ApplyDemand(w, c, demands[i].Axis, demands[i].Increase, i == 0 ? c.FactionConcessionSteps : 1);
        if (Serf184Diag.On) Serf184Diag.Forced(w, __serfF0);
"""),
    ("""        w.PendingRevoltRegion = worst.Id;
        Parliament.TargetDemand(w, axis, inc);
""",
     """        w.PendingRevoltRegion = worst.Id;
        if (Serf184Diag.On && axis == PolicyAxis.SerfEmancipation) Serf184Diag.Revolt(w);
        Parliament.TargetDemand(w, axis, inc);
"""),
    ("""    internal static void ExecuteRegimeChange(World w, Coefficients c, Polity target, bool violent)
    {
""",
     """    internal static void ExecuteRegimeChange(World w, Coefficients c, Polity target, bool violent)
    {
        if (Serf184Diag.On) Serf184Diag.Regime(w, w.Polity, target, violent);
"""),
])
print('ok serf184', root)
