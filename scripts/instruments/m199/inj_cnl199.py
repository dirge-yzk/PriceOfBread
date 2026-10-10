"""[計測の写しだけ] (199) ＝(193) の計器の写し＋行末に7列（3勢力の政治力 pwT/pwM/pwS・軍の政治力 pwArmy・徴兵法を下げる一歩と上げる一歩の純支持 netDn/netUp〔施行の直前〕・その四半期の政治の押しの予算 push〔施行の後＝その四半期の Government.Step が置いた値〕）。(193) 徴兵法の判断の計器（読むだけ）。環境変数 POB_CNL193_DIR の下に c<seed>.txt。未設定なら1命令も走らない。
fullX を当てた木に最後に当てる（SelfPlay の自己対戦のループの sim.StepQuarter() の直後に、国ごとの行を種のファイルへ流す）。

K 行＝国×四半期（四半期首＝AI の判断と施行）：
  K|q|年|国|交戦(判断の時)|総動員(前)|総動員(後)|戦前の記録(前)|法(前)|AI の生の目標|整形の後の目標|施行の前の目標|法(後)|戦前の記録(後)|
    陸軍の不安の割合(AI が読んだ値)|その値を計算した時に交戦中か|陸軍の不安|自国の養える軍|最大の寄与の国|その寄与|その国と交戦中(計算の時)|その国との敵意|
    本国の州の数|統合格|植民地格|農奴解放|皆兵の門|下げる一歩の門(可否)|代価|上げる一歩の門(可否)|代価|政治資本の残り|人口|戦後の四半期数
  ・陸軍の不安の割合と最大の寄与は四半期末の StepSecurityThreat（外交の前）で計算した値＝次の四半期首の AI が読む値。
  ・門は Government.GateInfo（法律タブと同じ照会・副作用なし）を施行の直前に読む。
  ・戦後の四半期数＝最後に交戦していた四半期からの数（交戦中 0・一度も交戦していない -1）＝計器の中だけの数（力学は読まない）。
使い方：python inj_cnl193.py <木の根>"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'm183'))
from edlib import edit
root = sys.argv[1]

def tail(rel, text):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    if (CR + LF) in s: text = text.replace(LF, CR + LF)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + (s + text).encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (193) 徴兵法の判断（読むだけ）。</summary>
public static class Cnl193Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_CNL193_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Acc
    {
        public bool ThreatAtWar; public string TopName = "-"; public double TopVal; public bool TopAtWar; public double TopHost;
        public double ArmBorder, ArmLand, ArmSea, ArmChina;
        public int Raw = -1; public int LastWarQ = -1; public int Q;
        public int Law0, Pre0, Tgt, Tgt2, AtWar, Mob0, Mob1; public double Share;
        public readonly System.Text.StringBuilder Sb = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, Acc> By = new();
    public static Acc A(World w) => By.GetValue(w, _ => new Acc());
    static string Nm(World? w) => w?.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "-";
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public static void Raw(World w, int raw) { var a = A(w); lock (a) a.Raw = raw; }
    public static void Pre(World w)
    {
        if (w.WarmupFrozen) return;
        var a = A(w);
        lock (a)
        {
            a.Law0 = (int)w.ConscriptionLaw; a.Pre0 = w.ConscriptionPreWar; a.AtWar = w.AtWarNow ? 1 : 0; a.Mob0 = w.Mobilized ? 1 : 0;
            a.Share = UtilityAgent.SecurityShare(w, army: true); a.Raw = -1;
            if (a.AtWar == 1) a.LastWarQ = a.Q;
        }
    }
    public static void Mid(World w) { if (w.WarmupFrozen) return; var a = A(w); lock (a) { a.Tgt = (int)w.Policy.ConscriptionLaw; a.Mob1 = w.Mobilized ? 1 : 0; } }
    public static void Gate(World w)
    {
        if (w.WarmupFrozen) return;
        var a = A(w);
        lock (a)
        {
            a.Tgt2 = (int)w.Policy.ConscriptionLaw;
            int cur = (int)w.ConscriptionLaw;
            string dn = "-\t-", up = "-\t-";
            if (cur > 0) { var g = Government.GateInfo(w, PolicyAxis.ConscriptionLaw, false, cur - 1); dn = (g.Ok ? "1" : "0") + "\t" + F(g.Cost); }
            if (cur < 2) { var g = Government.GateInfo(w, PolicyAxis.ConscriptionLaw, true, cur + 1); up = (g.Ok ? "1" : "0") + "\t" + F(g.Cost); }
            int nc = 0, ni = 0, ncol = 0;
            foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; if (r.Tier == RegionTier.Core) nc++; else if (r.Tier == RegionTier.Integrated) ni++; else ncol++; }
            bool gate = Government.UniversalConscriptionOpen(w);
            a.GateLine = $"{nc}\t{ni}\t{ncol}\t{(int)w.SerfEmancipation}\t{(gate ? 1 : 0)}\t{dn}\t{up}\t{F(w.PoliticalCapitalMax - w.MandateSpent)}\t{F(w.TotalPopulation)}";
            var pe = PolicyPreferences.Build(w);
            double nDn = PolicyPreferences.NetSupport(w, PolicyAxis.ConscriptionLaw, false, pe), nUp = PolicyPreferences.NetSupport(w, PolicyAxis.ConscriptionLaw, true, pe);
            a.X199 = $"{F(pe.Power[0])}\t{F(pe.Power[1])}\t{F(pe.Power[2])}\t{F(PolicyPreferences.ArmyPower(w))}\t{F(nDn)}\t{F(nUp)}";
        }
    }
    public static void Post(World w)
    {
        if (w.WarmupFrozen) return;
        var a = A(w);
        lock (a)
        {
            int since = a.LastWarQ < 0 ? -1 : a.Q - a.LastWarQ;
            a.Sb.Append($"K\t{a.Q}\t{w.Year}\t{Nm(w)}\t{a.AtWar}\t{a.Mob0}\t{a.Mob1}\t{a.Pre0}\t{a.Law0}\t{a.Raw}\t{a.Tgt}\t{a.Tgt2}\t{(int)w.ConscriptionLaw}\t{w.ConscriptionPreWar}\t"
                + $"{F(a.Share)}\t{(a.ThreatAtWar ? 1 : 0)}\t{F(w.SecurityThreatArmy)}\t{F(w.SecurityThreatOwnMass)}\t{a.TopName}\t{F(a.TopVal)}\t{(a.TopAtWar ? 1 : 0)}\t{F(a.TopHost)}\t{a.GateLine}\t{since}\t{F(a.ArmBorder)}\t{F(a.ArmLand)}\t{F(a.ArmSea)}\t{F(a.ArmChina)}\t{a.X199}\t{F(w.PoliticalPushBudget)}\n");
            a.Q++;
        }
    }
    public static void Threat(World w, bool atWar, string top, double topVal, bool topAtWar, double host, double bd, double ld, double sd, double cn)
    {
        var a = A(w);
        lock (a) { a.ThreatAtWar = atWar; a.TopName = top; a.TopVal = topVal; a.TopAtWar = topAtWar; a.TopHost = host; a.ArmBorder = bd; a.ArmLand = ld; a.ArmSea = sd; a.ArmChina = cn; }
    }
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"c{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#K\tq\tyr\tnation\tatWar\tmob0\tmob1\tpre0\tlaw0\traw\ttgt\ttgt2\tlaw1\tpre1\tshare\tthrAtWar\tthrArmy\town\ttop\ttopVal\ttopAtWar\ttopHost\tcore\tinteg\tcol\tserf\tgate\tdnOk\tdnCost\tupOk\tupCost\tcapLeft\tpop\tsince\tarmBorder\tarmLand\tarmSea\tarmChina\tpwT\tpwM\tpwS\tpwArmy\tnetDn\tnetUp\tpush\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            foreach (var ns in sim.Nations)
            {
                var a = A(ns.World);
                lock (a) { if (a.Sb.Length > 0) { W.Write(a.Sb.ToString()); a.Sb.Clear(); } }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
TAIL = TAIL.replace('        public int Law0, Pre0, Tgt, Tgt2, AtWar, Mob0, Mob1; public double Share;\n',
                    '        public int Law0, Pre0, Tgt, Tgt2, AtWar, Mob0, Mob1; public double Share; public string GateLine = ""; public string X199 = "-\\t-\\t-\\t-\\t-\\t-";\n')
tail('PriceOfBread.SimCore/Discovery.cs', TAIL)

# 四半期首（AI の判断の前・後・施行の直前・施行の後）
edit(root, 'PriceOfBread.SimCore/Simulation.cs', [
    ('        _agent.DecideQuarter(World);\n        UpdateRegionalPriceLevels();',
     '        if (Cnl193Diag.On && World.SelfPower is not null) Cnl193Diag.Pre(World);   // [計器の写しだけ]\n'
     '        _agent.DecideQuarter(World);\n'
     '        if (Cnl193Diag.On && World.SelfPower is not null) Cnl193Diag.Mid(World);   // [計器の写しだけ]\n'
     '        UpdateRegionalPriceLevels();'),
    ('        Government.Step(World, World.Coef);   // 政策を反映（法整備＝即時／スライダー＝利益集団の力で律速して漸進）\n    }\n',
     '        if (Cnl193Diag.On && World.SelfPower is not null) Cnl193Diag.Gate(World);   // [計器の写しだけ]\n'
     '        Government.Step(World, World.Coef);   // 政策を反映（法整備＝即時／スライダー＝利益集団の力で律速して漸進）\n'
     '        if (Cnl193Diag.On && World.SelfPower is not null) Cnl193Diag.Post(World);   // [計器の写しだけ]\n    }\n'),
])
# AI の生の目標（DecideModernLaws の徴兵法の目標の行の直後）
_sp = root + '/PriceOfBread.SimCore/SelfPlay.cs'
_s = open(_sp, 'rb').read().decode('utf-8-sig').replace(chr(13), '')
import re
m = (re.search(r'\n(        p\.ConscriptionLaw = [^\n]*csTop[^\n]*\n)', _s)
     or re.search(r'\n(        else if \(atWar && p\.ConscriptionLaw < w\.ConscriptionLaw\) p\.ConscriptionLaw = [^\n]*\n)', _s)
     or re.search(r'\n(        else if \(!atWar && armyShare >= 0\) p\.ConscriptionLaw = [^\n]*\n)', _s))
assert m, 'no conscription target line'
line = m.group(1)
edit(root, 'PriceOfBread.SimCore/SelfPlay.cs', [
    (line, line + '        if (Cnl193Diag.On) Cnl193Diag.Raw(w, (int)p.ConscriptionLaw);   // [計器の写しだけ]\n'),
])
# 安全保障の不安（四半期末・外交の前）＝陸軍の不安の最大の寄与
edit(root, 'PriceOfBread.SimCore/MultiNationSim.cs', [
    ('        Cost[CostLedger.ThreatReachPairs] += pairs;',
     '''        if (Cnl193Diag.On)   // [計器の写しだけ]
            for (int b = 0; b < n; b++)
            {
                if (ps[b] is null) continue;
                double best = 0; int bi = -1; double bd = 0, ld = 0, sd = 0, cn = 0;
                for (int a = 0; a < n; a++)
                {
                    if (a == b || !(colSum[a] > 1e-12) || !(seats[a] > 0)) continue;
                    double v = hrArmy[a * n + b] / colSum[a] * seats[a];
                    if (v > best) { best = v; bi = a; }
                    if (!(hrArmy[a * n + b] > 0)) continue;
                    double kk = ArmyShape.Hostility(Diplomacy, ps[b], ps[a], _coef) / colSum[a] * seats[a];
                    double pj = Diplomacy.Projection(ps[a], ps[b], _coef);
                    bd += kk * borderHome[a * n + b];
                    if (Diplomacy.IsSeaCrossing(ps[a], ps[b], _coef)) sd += kk * pj * Diplomacy.LandingGate(ps[a], ps[b], _coef); else ld += kk * pj;
                    if (ps[a].BaseName == "清") cn += v;
                }
                bool topWar = false; double host = 0; string top = "-";
                if (bi >= 0)
                {
                    top = ps[bi].BaseName.Length > 0 ? ps[bi].BaseName : ps[bi].Name;
                    foreach (var war in Diplomacy.Wars) if ((war.A == ps[bi] && war.B == ps[b]) || (war.A == ps[b] && war.B == ps[bi])) topWar = true;
                    host = ArmyShape.Hostility(Diplomacy, ps[b], ps[bi], _coef);
                }
                Cnl193Diag.Threat(Nations[b].World, Nations[b].World.AtWarNow, top, best, topWar, host, bd, ld, sd, cn);
            }
        Cost[CostLedger.ThreatReachPairs] += pairs;'''),
])
# 種ごとのファイルへ流す（自己対戦のループ）
edit(root, 'PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n', '        sim.StartWarmup();\n        var cnl193M = Cnl193Diag.On ? new Cnl193Diag.Match(seed) : null;   // [計器の写しだけ]\n'),
    ('                sim.StepQuarter();\n',
     '                sim.StepQuarter();\n                cnl193M?.Quarter(sim, q, quarters);   // [計器の写しだけ]\n'),
])
print('ok cnl199', root)
