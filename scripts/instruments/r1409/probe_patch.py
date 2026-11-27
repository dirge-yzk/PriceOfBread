# (1409) 使い捨ての木 wt_probe だけに当てる（コミットしない）：行の数を変えない同じ行の置き換え＋新しいファイル 1 本。
import sys
R = sys.argv[1] + '/PriceOfBread.SimCore/'
def sub(f, ln, old, new):
    p = R + f; b = open(p, 'rb').read().split(b'\n')
    s = b[ln - 1].decode('utf-8')
    assert old in s, (f, ln, s[:120]); b[ln - 1] = s.replace(old, new, 1).encode('utf-8')
    open(p, 'wb').write(b'\n'.join(b))
sub('Diplomacy.cs', 1049, 'Hegemon(Coefficients c)', 'Hegemon(Coefficients c, [System.Runtime.CompilerServices.CallerLineNumber] int ln = 0)')
sub('Diplomacy.cs', 1052, 'Cost[CostLedger.DiploHegemonScans] += (long)Powers.Count * Powers.Count;', 'Cost[CostLedger.DiploHegemonScans] += (long)Powers.Count * Powers.Count; Probe1409.Heg(ln, (long)Powers.Count * Powers.Count);')
sub('Diplomacy.cs', 1598, 'AllyJoinThreshold(Power enemy, Coefficients c)', 'AllyJoinThreshold(Power enemy, Coefficients c, [System.Runtime.CompilerServices.CallerLineNumber] int ln = 0)')
sub('Diplomacy.cs', 1599, '=> enemy == Hegemon(c)', '=> Probe1409.Ajt(ln) && enemy == Hegemon(c)')
sub('Diplomacy.cs', 1435, 'SideMilitary(Power p, Power foe, Coefficients c)', 'SideMilitary(Power p, Power foe, Coefficients c, [System.Runtime.CompilerServices.CallerLineNumber] int ln = 0)')
sub('Diplomacy.cs', 1437, 'double m = p.Military;', 'double m = p.Military; Probe1409.Sm(ln);')
sub('Diplomacy.cs', 1770, 'c.RandomOnlyClimateEpidemic > 0 ||', 'Probe1409.On(1, c) ||')
sub('Diplomacy.cs', 1784, 'c.RandomOnlyClimateEpidemic > 0 ||', 'Probe1409.On(2, c) ||')
sub('Diplomacy.cs', 1785, 'TryAiSubjugate(p, c);', '{ Probe1409.Tas(); TryAiSubjugate(p, c); }')
sub('Factions.cs', 46, '!(c.RandomOnlyClimateEpidemic > 0)', '!Probe1409.On(4, c)')
sub('SelfPlay.cs', 385, 'w.Coef.RandomOnlyClimateEpidemic > 0 ||', 'Probe1409.On(8, w.Coef) ||')
sub('Simulation.cs', 1691, '!(hc.RandomOnlyClimateEpidemic > 0)', '!Probe1409.On(16, hc)')
open(R + 'Probe1409.cs', 'wb').write('''namespace PriceOfBread.SimCore;
public static class Probe1409
{
    public static int Mask = -1;   // -1＝旗のまま・0〜31＝bit 1 同盟・2 植民の試行・4 勢力の変異・8 独占の押し・16 作物の病気（bit が立つ＝旗 1 の側）
    public static bool On(int bit, Coefficients c) => Mask < 0 ? c.RandomOnlyClimateEpidemic > 0 : (Mask & bit) != 0;
    public static readonly System.Collections.Generic.Dictionary<string, long> H = new();
    public static long TasCalls;
    static int _sm, _ajt;
    public static void Sm(int ln) => _sm = ln;
    public static bool Ajt(int ln) { _ajt = ln; return true; }
    public static void Tas() => TasCalls++;
    public static void Heg(int ln, long n)
    {
        string k = ln == 1599 ? (_ajt == 1443 ? $"AJT<-SideMilitary<-L{_sm}" : $"AJT<-L{_ajt}") : $"L{ln}";
        H[k] = H.GetValueOrDefault(k) + n; H["calls:" + k] = H.GetValueOrDefault("calls:" + k) + 1;
    }
    public static void Reset() { H.Clear(); TasCalls = 0; }
}
'''.replace('\n', '\r\n').encode('utf-8'))
print('ok')
