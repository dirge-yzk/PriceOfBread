"""[計測の写しだけ] (194) 総動員の判断の計器（読むだけ）。環境変数 POB_MOB194_DIR の下に m<seed>.txt。未設定なら1命令も走らない。
fullX（m193/inj_cnl193 まで）を当てた木に最後に当てる。AI の判断（SelfPlay.DecideQuarter）の直前と直後に、交戦中の国の行を書く。

M 行＝交戦中の国×四半期（四半期首＝AI の総動員の判断の直後）：
  M|q|年|国|ランク(0 小・1 中・2 列強)|総動員(前)|総動員(後)|徴兵法|戦争の数|直接の敵の数|うち国|うち列強|
    陣営の戦力比の最大（SideMilitary(敵,自)÷SideMilitary(自,敵)）|劣勢(比>1.2)|敗色|相手が全て海越え|陸で押せる敵がいる（FrontPush(敵,自)>0）|
    自国の軍(Military)|陸軍力|養える軍|兵役の母数|直接の敵の軍の和|陸軍力の和|敵の押し(自国へ)の和|自国の押し(敵へ)の和|敵の養える軍の和|敵の兵役の母数の和|
    共に戦う国（直接の敵と交戦中・自国と交戦していない）の数|軍の和|養える軍の和|うち列強|
    その敵（共に戦う国の敵・直接の敵でない）の数|軍の和|養える軍の和|うち列強|塊の列強の数（自国・直接の敵・共に戦う国・その敵）|
    戦場の強さ（首都の州の LastWarIntensity＝劣勢×接触）|厭戦|兵士|人口|直接の敵の名（; 区切り）|直接の敵の陸の届き（敵→自の Projection×上陸の門の最大）|戦争の規模（木に Diplomacy.WarScale があれば・無ければ -9）
力学は読まない（副作用の無い照会〔SideMilitary・FrontPush・Projection・LandingGate・IsSeaCrossing・EstablishmentOf・RankOf〕と記録だけ）。
使い方：python inj_mob194.py <木の根>"""
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

/// <summary>[計測の写しだけ] (194) 総動員の判断（読むだけ）。</summary>
public static class Mob194Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_MOB194_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Acc { public int Mob0; public int Q; public readonly System.Text.StringBuilder Sb = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, Acc> By = new();
    static Acc A(World w) => By.GetValue(w, _ => new Acc());
    static string Nm(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public static void Pre(World w) { if (w.WarmupFrozen) return; var a = A(w); lock (a) a.Mob0 = w.Mobilized ? 1 : 0; }
    static bool AtWarPair(Diplomacy d, Power x, Power y) { foreach (var war in d.Wars) if ((war.A == x && war.B == y) || (war.A == y && war.B == x)) return true; return false; }
    static double Est(Power p, out double man)
    {
        man = 0;
        if (p.Nation is not { } nw) return 0;
        return Military.EstablishmentOf(nw, nw.Coef, out man);
    }
    public static void Post(World w)
    {
        if (w.WarmupFrozen) return;
        var a = A(w);
        int q;
        lock (a) { q = a.Q; a.Q++; }
        if (w.SelfPower is not { } self || w.Diplomacy is not { } d) return;
        var c = w.Coef;
        var foes = new System.Collections.Generic.List<Power>();
        int nWar = 0;
        double ratioMax = 0, losing = 0; bool out1 = false;
        foreach (var war in d.Wars)
        {
            Power? foe = war.A == self ? war.B : war.B == self ? war.A : null;
            if (foe is null) continue;
            nWar++;
            if (!foes.Contains(foe)) foes.Add(foe);
            losing = System.Math.Max(losing, System.Math.Max(0, war.A == self ? -war.Score : war.Score));
            double sf = d.SideMilitary(foe, self, c), ss = d.SideMilitary(self, foe, c);
            double rr = ss > 1e-12 ? sf / ss : (sf > 0 ? 99 : 0);
            if (rr > ratioMax) ratioMax = rr;
            if (sf > ss * 1.2) out1 = true;
        }
        if (nWar == 0) return;
        int nFnat = 0, nFgreat = 0; bool allSea = true, foeLands = false;
        double fMil = 0, fLand = 0, fPush = 0, myPush = 0, fEst = 0, fMan = 0, reach = 0;
        var names = new System.Text.StringBuilder();
        foreach (var f in foes)
        {
            if (f.Nation != null) nFnat++;
            if (d.RankOf(f) == PowerRank.Great) nFgreat++;
            if (!d.IsSeaCrossing(f, self, c)) allSea = false;
            double fp = d.FrontPush(f, self, c), mp = d.FrontPush(self, f, c);
            if (fp > 0) foeLands = true;
            fMil += f.Military; fLand += f.LandPower; fPush += fp; myPush += mp;
            fEst += Est(f, out double fm); fMan += fm;
            double rc = d.Projection(f, self, c) * d.LandingGate(f, self, c);
            if (rc > reach) reach = rc;
            names.Append(Nm(f)).Append(';');
        }
        var co = new System.Collections.Generic.List<Power>();
        foreach (var p in d.Powers)
        {
            if (p == self || foes.Contains(p) || AtWarPair(d, p, self)) continue;
            foreach (var f in foes) if (AtWarPair(d, p, f)) { co.Add(p); break; }
        }
        var ring2 = new System.Collections.Generic.List<Power>();
        foreach (var p in d.Powers)
        {
            if (p == self || foes.Contains(p) || co.Contains(p)) continue;
            foreach (var x in co) if (AtWarPair(d, p, x)) { ring2.Add(p); break; }
        }
        double cMil = 0, cEst = 0, dMil = 0, dEst = 0; int cG = 0, dG = 0;
        foreach (var p in co) { cMil += p.Military; cEst += Est(p, out _); if (d.RankOf(p) == PowerRank.Great) cG++; }
        foreach (var p in ring2) { dMil += p.Military; dEst += Est(p, out _); if (d.RankOf(p) == PowerRank.Great) dG++; }
        int myG = d.RankOf(self) == PowerRank.Great ? 1 : 0;
        double selfEst = Est(self, out double selfMan);
        var sb = new System.Text.StringBuilder();
        sb.Append("M|").Append(q).Append('|').Append(w.Year).Append('|').Append(Nm(self)).Append('|').Append((int)d.RankOf(self)).Append('|')
          .Append(a.Mob0).Append('|').Append(w.Mobilized ? 1 : 0).Append('|').Append((int)w.ConscriptionLaw).Append('|')
          .Append(nWar).Append('|').Append(foes.Count).Append('|').Append(nFnat).Append('|').Append(nFgreat).Append('|')
          .Append(F(ratioMax)).Append('|').Append(out1 ? 1 : 0).Append('|').Append(F(losing)).Append('|').Append(allSea ? 1 : 0).Append('|').Append(foeLands ? 1 : 0).Append('|')
          .Append(F(self.Military)).Append('|').Append(F(self.LandPower)).Append('|').Append(F(selfEst)).Append('|').Append(F(selfMan)).Append('|')
          .Append(F(fMil)).Append('|').Append(F(fLand)).Append('|').Append(F(fPush)).Append('|').Append(F(myPush)).Append('|').Append(F(fEst)).Append('|').Append(F(fMan)).Append('|')
          .Append(co.Count).Append('|').Append(F(cMil)).Append('|').Append(F(cEst)).Append('|').Append(cG).Append('|')
          .Append(ring2.Count).Append('|').Append(F(dMil)).Append('|').Append(F(dEst)).Append('|').Append(dG).Append('|').Append(myG + nFgreat + cG + dG).Append('|')
          .Append(F(w.Region.LastWarIntensity)).Append('|').Append(F(self.WarExhaustion)).Append('|').Append(F(w.SoldierSizeK())).Append('|').Append(F(w.TotalPopulation)).Append('|')
          .Append(names).Append('|').Append(F(reach)).Append('|').Append(F(SCALEEXPR)).Append('\n');
        lock (a) a.Sb.Append(sb);
    }
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"m{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#M|q|yr|nation|rank|mob0|mob1|law|nWar|nFoe|nFnat|nFgreat|ratioMax|out|losing|allSea|foeLands|selfMil|selfLand|selfEst|selfMan|fMil|fLand|fPush|myPush|fEst|fMan|nCo|cMil|cEst|cGreat|nR2|dMil|dEst|dGreat|clusterGreat|warZone|exh|soldiers|pop|foes|reach|scale\n");
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
_dsrc = open(root + '/PriceOfBread.SimCore/Diplomacy.cs', 'rb').read().decode('utf-8-sig')
_has = 'public double WarScale(' in _dsrc
_out = 'out double homeLosing' in _dsrc
TAIL = TAIL.replace('SCALEEXPR', ('d.WarScale(self, c, out _)' if _out else 'd.WarScale(self, c)') if _has else '(-9.0)')
tail('PriceOfBread.SimCore/Discovery.cs', TAIL)
edit(root, 'PriceOfBread.SimCore/Simulation.cs', [
    ('        _agent.DecideQuarter(World);\n',
     '        if (Mob194Diag.On && World.SelfPower is not null) Mob194Diag.Pre(World);   // [計器の写しだけ]\n'
     '        _agent.DecideQuarter(World);\n'
     '        if (Mob194Diag.On && World.SelfPower is not null) Mob194Diag.Post(World);   // [計器の写しだけ]\n'),
])
edit(root, 'PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var cnl193M = Cnl193Diag.On ? new Cnl193Diag.Match(seed) : null;   // [計器の写しだけ]\n',
     '        var cnl193M = Cnl193Diag.On ? new Cnl193Diag.Match(seed) : null;   // [計器の写しだけ]\n'
     '        var mob194M = Mob194Diag.On ? new Mob194Diag.Match(seed) : null;   // [計器の写しだけ]\n'),
    ('                cnl193M?.Quarter(sim, q, quarters);   // [計器の写しだけ]\n',
     '                cnl193M?.Quarter(sim, q, quarters);   // [計器の写しだけ]\n                mob194M?.Quarter(sim, q, quarters);   // [計器の写しだけ]\n'),
])
print('ok mob194', root)
