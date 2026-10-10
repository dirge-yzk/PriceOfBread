"""[計測の写しだけ] (156) 戦争の戦死の計器（読むだけ）。環境変数 POB_CAS_DIR の下に c<seed>.txt。未設定なら1命令も走らない。
  Y 行＝(年, 国) ごとの和：交戦の国×月・兵士の月和・接触した兵士の月和（兵士×接触÷自軍のゲージ）・接触の割合の和・戦死（兵士・上限後）・上限前の戦死・上限に張り付いた月・将校を除く
  W 行＝戦争1本ごと（Wars.Add の直後に登録）：開戦の年・内戦か・両者が国か・両者の名・月数・戦死（両側の和・将校込み・台帳）
  S 行＝10年ごとの四半期末の和：四半期の数・世界の兵士・世界の人口・交戦中の国の兵士
使い方：python inj_cas.py <木の根>（素の木にも、他の計器を当てた木にも当たる）"""
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

/// <summary>[計測の写しだけ] (156) 戦争の戦死の計器（読むだけ）。</summary>
public static class CasDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_CAS_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Acc
    {
        public readonly System.Collections.Generic.Dictionary<(int, string), double[]> M = new();
        public readonly System.Collections.Generic.List<(int y, War w)> Ev = new();
        public readonly System.Collections.Generic.SortedDictionary<int, double[]> S = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Acc> ByDip = new();
    public static Acc For(object dip) => ByDip.GetValue(dip, _ => new Acc());
    static string NameOf(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    public static void Rec(object dip, int year, Power p, double soldierPop, double contact, double casForce, double cas)
    {
        var acc = For(dip);
        var key = (year, NameOf(p));
        if (!acc.M.TryGetValue(key, out var a)) acc.M[key] = a = new double[8];
        double frac = p.Military > 1e-9 ? contact / p.Military : 0;
        a[0] += 1; a[1] += soldierPop; a[2] += soldierPop * frac; a[3] += frac; a[4] += cas; a[5] += casForce; a[6] += casForce > cas ? 1 : 0; a[7] += frac >= 0.5 ? 1 : 0;
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var acc = For(sim.Diplomacy);
        int y = sim.Nations[0].World.Year;
        if (!acc.S.TryGetValue(y / 10 * 10, out var s)) acc.S[y / 10 * 10] = s = new double[4];
        s[0] += 1;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            double sold = w.SoldierSize();
            s[1] += sold; s[2] += w.TotalPopulation;
            if (w.SelfPower is { } sp)
                foreach (var wr in sim.Diplomacy.Wars) if (wr.A == sp || wr.B == sp) { s[3] += sold; break; }
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"c{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#Y\tyear\tnation\tmonths\tsoldMonths\tengSoldMonths\tfracSum\tdead\tdeadPreCap\tcapMonths\tfracGe05Months\n");
        foreach (var kv in acc.M)
        {
            var a = kv.Value;
            W.Write($"Y\t{kv.Key.Item1}\t{kv.Key.Item2}\t{F(a[0])}\t{F(a[1])}\t{F(a[2])}\t{F(a[3])}\t{F(a[4])}\t{F(a[5])}\t{F(a[6])}\t{F(a[7])}\n");
        }
        W.Write("#W\tstartYr\tcivil\tAnat\tBnat\tA\tB\tmonths\tdead\n");
        foreach (var (yr, wr) in acc.Ev)
            W.Write($"W\t{yr}\t{(wr.CivilWarParent is not null ? 1 : 0)}\t{(wr.A.Nation != null ? 1 : 0)}\t{(wr.B.Nation != null ? 1 : 0)}\t{NameOf(wr.A)}\t{NameOf(wr.B)}\t{wr.Months}\t{F(wr.DeadA + wr.DeadB)}\n");
        W.Write("#S\tdec\tquarters\tworldSoldQ\tworldPopQ\twarSoldQ\n");
        foreach (var kv in acc.S)
            W.Write($"S\t{kv.Key}\t{F(kv.Value[0])}\t{F(kv.Value[1])}\t{F(kv.Value[2])}\t{F(kv.Value[3])}\n");
    }
}
'''
edit('PriceOfBread.SimCore/Diplomacy.cs', [
    ("        double cas = Math.Min(casForce, soldierPop * c.WarCasualtyMonthlyCap);",
     "        double cas = Math.Min(casForce, soldierPop * c.WarCasualtyMonthlyCap);\n        if (CasDiag.On) CasDiag.Rec(this, _year, p, soldierPop, contactStrength, casForce, cas);"),
    ("        Wars.Add(war);\n",
     "        Wars.Add(war);\n        if (CasDiag.On) CasDiag.For(this).Ev.Add((_year, war));\n"),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("            for (int q = 0; q < quarters; q++)\n            {\n                sim.StepQuarter();\n",
     "            for (int q = 0; q < quarters; q++)\n            {\n                sim.StepQuarter();\n                if (CasDiag.On) CasDiag.Quarter(sim, seed, q, quarters);\n"),
])
print('ok', root)
