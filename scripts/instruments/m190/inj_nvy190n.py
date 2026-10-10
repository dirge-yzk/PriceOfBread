"""[計測の写しだけ] (190) 新しい木用（＋艦の隻数3種・海軍の予算・乗員の充足・乗員・陸軍力）。海軍の金と艦の計器（読むだけ）。環境変数 POB_NVY190_DIR の下に v<seed>.txt。未設定なら1命令も走らない。
  V 行＝年に1回（q%4==3）の国ごと：年・国・兵士・軍の器（Military.EnvelopeOf）・兵の給与の請求（LastSoldierPay）・装備の上限（首都 MilitaryBudgetCeiling）・
        装備の予算（首都 MilitaryBudget）・海の金の割合（World.NavalSpendShare）・理想の海の割合（NavalSpendShareOf）・即応度・装備の充足（基本）・
        上位の充足（全/陸）・最上位の充足（全/陸）・訓練の充足・首都の武器庫の軍艦/装甲艦/弩級戦艦・海の熟練の倍率・海軍力・艦隊ゲージ・海の必要量・軍の総合ゲージ・
        人口・名目GDP・使える歳入
  A でも新しい木でも同じ名前で読める量だけを読む（艦隊の在庫そのものは inj_navy の fstock・fleet）。
inj_navy を先に当てた木に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_nvy190.py <木の根>"""
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

/// <summary>[計測の写しだけ] (190) 海軍の金と艦（読むだけ）。</summary>
public static class Nvy190Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_NVY190_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static double U(World w, int k) { double u = 0; foreach (var rr in w.Regions) if (rr.State == RegionState.Active && rr.FleetUnits is { } fu && k < fu.Length) u += fu[k]; return u; }
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"v{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#V\tq\tyr\tnation\tsold\tenv\tpay\tceil\tbudget\tspendSh\tshapeSh\tready\teqFill\tqFill\tqLand\tt3Fill\tt3Land\ttrain\taWar\taIron\taDread\tsklN\tnaval\tfleet\tneedS\tmil\tpop\tnomGdp\trev\tuWar\tuIron\tuDread\tnavBud\tcrewFill\tcrewMen\tland\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
            {
                var ns = sim.Nations;
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World;
                    if (w.SelfPower is not { } sp || !(w.TotalPopulation > 0)) continue;
                    string bn = sp.BaseName.Length > 0 ? sp.BaseName : sp.Name;
                    var cap = w.Region; var c = w.Coef;
                    double aW = cap.ArmsStock?.GetValueOrDefault(Catalog.WarshipId) ?? double.NaN;
                    double aI = cap.ArmsStock?.GetValueOrDefault(Catalog.IroncladId) ?? double.NaN;
                    double aD = cap.ArmsStock?.GetValueOrDefault(Catalog.DreadnoughtId) ?? double.NaN;
                    W.Write($"V\t{q + 1}\t{w.Year}\t{bn}\t{F(w.SoldierSizeK())}\t{F(Military.EnvelopeOf(w, c))}\t{F(w.LastSoldierPayPerMonth)}\t{F(cap.MilitaryBudgetCeiling)}\t{F(cap.MilitaryBudget)}\t{F(w.NavalSpendShare)}\t{F(ArmyShape.NavalSpendShareOf(sp))}\t{F(w.MilitaryReadiness)}\t{F(cap.MilitaryEquipFill)}\t{F(cap.MilitaryQualityFill)}\t{F(cap.MilitaryQualityLandFill)}\t{F(cap.MilitaryTier3Fill)}\t{F(cap.MilitaryTier3LandFill)}\t{F(w.MilitaryTrainingFill)}\t{F(aW)}\t{F(aI)}\t{F(aD)}\t{F(sp.NavySkillMul)}\t{F(sp.NavalPower)}\t{F(sp.FleetGauge)}\t{F(sp.NeedSea)}\t{F(sp.Military)}\t{F(w.TotalPopulation)}\t{F(w.NominalGdpPerMonth)}\t{F(w.SpendableRevenue())}\t{F(U(w, 0))}\t{F(U(w, 1))}\t{F(U(w, 2))}\t{F(cap.NavalBudget)}\t{F(sp.CrewFill)}\t{F(sp.NavalShare * w.SoldierSizeK())}\t{F(sp.LandPower)}\n");
                }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var nvy190M = Nvy190Diag.On ? new Nvy190Diag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                nvy190M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
