"""[計測の写しだけ] (195) 線Q 海軍の残り：海の必要量の物差しの反実仮想と艦隊の移る出来事の計器（読むだけ）。
環境変数 POB_NVQ195_DIR の下に q<seed>.txt。未設定なら1命令も走らない。
  Q 行＝年に1回（q%4==3）の国ごと：年・国・兵士・自国の軍の大きさ（ForcePotential）・軍の総合ゲージ・即応度・海軍シェア・
        陸の必要量・内の守り・海の必要量・艦隊の比の項（今）・海越えの項（今）・
        反実仮想（同じ盤面・静的）＝陸と海越えを相手の軍の大きさ（ForcePotential×(1−海軍シェア)＝即応度なし）で読んだ陸の必要量・海越えの項／
        艦隊の比の項を相手の海の取扱高で重みづけた量（港の取扱高÷世界の平均）・
        海の割合（今＝NavalSpendShareOf）・反実仮想の海の割合（物差しをそろえた）・（＋取扱高の重み）・
        港の取扱高・艦の強さのゲージ・海軍力・隻数（軍艦・装甲艦・弩級戦艦）・要る乗員・
        海越えで最大の相手・その量・その相手の制海権（SeaControl(相手, 自国)）・その投射（Projection＝距離の減衰×制海権）
  F 行＝艦の在庫を持つ州が国を移る出来事（Territory.TransferRegion の入口）＝年・種類（Ceded/Seceded/Returned）・州・首都だったか・送り手・受け手・
        州の艦の強さのゲージ・送り手の艦の強さのゲージ（移る前）・受け手の艦の強さのゲージ（移る前）
inj_navy と inj_need183 の後に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_nvq195.py <木の根>"""
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

/// <summary>[計測の写しだけ] (195) 海の必要量の物差しの反実仮想と艦隊の移る出来事（読むだけ）。</summary>
public static class Nvq195Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_NVQ195_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Acc { public readonly double[] V = new double[14]; public string Top = "-"; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Power, Acc> ByP = new();
    static Acc For(Power p) => ByP.GetValue(p, _ => new Acc());
    public sealed class Ev { public readonly System.Collections.Generic.List<string> L = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Ev> ByDip = new();
    static string Nm(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    static string NmW(World? w) => w?.SelfPower is { } sp ? Nm(sp) : "-new";
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    static double PortT(World w) { double t = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active && r.IsPort) t += r.LastPortThroughput; return t; }
    static double Gauge(World w, Region only)
    {
        if (w.Regions.Count == 0) return 0;
        var cap = w.Regions[0]; var c = w.Coef;
        double denom = EconomyStep.FleetGaugeDenominator(cap, c);
        if (!(denom > 0)) return 0;
        double v = 0;
        foreach (var rr in w.Regions)
        {
            if (only is not null && !ReferenceEquals(rr, only)) continue;
            if (rr.State != RegionState.Active || rr.FleetUnits is not { } fu) continue;
            for (int k = 0; k < fu.Length && k < c.FleetGoodIds.Length; k++) v += fu[k] * EconomyStep.FleetUnitPower(cap, c, k);
        }
        return v / denom;
    }

    /// <summary>ArmyShape.Compute の終わりで呼ぶ（同じ盤面の注意配分の重み hr* と不穏度 u をそのまま読む＝反実仮想は静的）。</summary>
    public static void Need(Diplomacy dip, System.Collections.Generic.List<Power> ps, double[] hrLand, double[] hrCross, double[] hrSea, double[] unrest, Coefficients c)
    {
        int n = ps.Count;
        var lpPot = new double[n]; var T = new double[n]; double tSum = 0;
        for (int j = 0; j < n; j++)
        {
            lpPot[j] = ArmyShape.ForcePotential(ps[j]) * (1 - ps[j].NavalShare);
            T[j] = ps[j].Nation is { } w ? PortT(w) : 0; tSum += T[j];
        }
        double tMean = n > 0 ? tSum / n : 0;
        var landPot = new double[n]; var crossPot = new double[n]; var crossNow = new double[n]; var fleetNow = new double[n]; var fleetV = new double[n];
        var topV = new double[n]; var topJ = new int[n];
        for (int i = 0; i < n; i++) topJ[i] = -1;
        for (int j = 0; j < n; j++)
        {
            double u = unrest[j], sumL = u, sumS = u, sumSV = u;
            for (int k = 0; k < n; k++)
            {
                sumL += hrLand[j * n + k] + hrCross[j * n + k]; sumS += hrSea[j * n + k];
                sumSV += tMean > 0 ? hrSea[j * n + k] * T[k] / tMean : 0;
            }
            for (int i = 0; i < n; i++)
            {
                if (sumL > 1e-12)
                {
                    double a = hrLand[j * n + i] / sumL, ax = hrCross[j * n + i] / sumL;
                    landPot[i] += a * lpPot[j]; crossPot[i] += ax * lpPot[j]; crossNow[i] += ax * ps[j].LandPower;
                    if (ax * ps[j].LandPower > topV[i]) { topV[i] = ax * ps[j].LandPower; topJ[i] = j; }
                }
                if (sumS > 1e-12) fleetNow[i] += hrSea[j * n + i] / sumS * ps[j].NavalPower;
                if (sumSV > 1e-12 && tMean > 0) fleetV[i] += hrSea[j * n + i] * T[i] / tMean / sumSV * ps[j].NavalPower;
            }
        }
        for (int i = 0; i < n; i++)
        {
            var p = ps[i]; var a = For(p);
            double fp = ArmyShape.ForcePotential(p);
            double seaPot = crossPot[i] + fleetNow[i], seaPotV = crossPot[i] + fleetV[i];
            double landSide = System.Math.Max(landPot[i] + p.NeedInternal, fp);
            a.V[0] = fp; a.V[1] = fleetNow[i]; a.V[2] = crossNow[i]; a.V[3] = landPot[i]; a.V[4] = crossPot[i]; a.V[5] = fleetV[i];
            a.V[6] = seaPot / (seaPot + landSide); a.V[7] = seaPotV / (seaPotV + landSide); a.V[8] = T[i];
            a.V[9] = topV[i];
            a.V[12] = p.Nation is { } wx ? wx.LastImportValue + wx.LastExportValue : 0;
            if (topJ[i] >= 0)
            {
                var q = ps[topJ[i]];
                a.Top = Nm(q);
                a.V[10] = dip.SeaControl(q, p); a.V[11] = dip.Projection(q, p, c);
            }
            else { a.Top = "-"; a.V[10] = double.NaN; a.V[11] = double.NaN; }
        }
    }

    /// <summary>Territory.TransferRegion の入口で呼ぶ（艦の在庫を持つ州だけ）。</summary>
    public static void Move(World from, World? to, Region r, string how, bool wasCapital)
    {
        if (!On || r.FleetUnits is not { } fu) return;
        double units = 0; foreach (var u in fu) units += u;
        if (!(units > 0)) return;
        var ev = ByDip.GetValue((object?)from.Diplomacy ?? from, _ => new Ev());
        string line = $"F\t{from.Year}\t{how}\t{r.Name}\t{(wasCapital ? 1 : 0)}\t{NmW(from)}\t{NmW(to)}\t{F(Gauge(from, r))}\t{F(Gauge(from, null!))}\t{F(to is null ? 0 : Gauge(to, null!))}\t{F(units)}";
        lock (ev) ev.L.Add(line);
    }

    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"q{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#Q\tyr\tnation\tsold\tfp\tmil\tready\tns\tneedLand\tneedInt\tneedSea\tfleetT\tcrossT\tlandPot\tcrossPot\tfleetV\tshare\tsharePot\tsharePotV\tportT\tfleet\tnaval\tuWar\tuIron\tuDread\tcrewNeed\ttopCross\ttopCrossV\ttopSC\ttopProj\tforeignX\n");
            W.Write("#F\tyr\thow\tregion\twasCap\tfrom\tto\tregGauge\tfromGauge\ttoGauge\tunits\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
                foreach (var p in sim.Diplomacy.Powers)
                {
                    if (p.Nation is not { } w || !(w.TotalPopulation > 0) || w.Regions.Count == 0) continue;
                    var a = For(p); var c = w.Coef;
                    double uW = 0, uI = 0, uD = 0;
                    foreach (var rr in w.Regions)
                        if (rr.State == RegionState.Active && rr.FleetUnits is { } fu)
                        { if (fu.Length > 0) uW += fu[0]; if (fu.Length > 1) uI += fu[1]; if (fu.Length > 2) uD += fu[2]; }
                    double denom = EconomyStep.FleetGaugeDenominator(w.Regions[0], c);
                    double crewNeed = denom > 0 ? (uW + uI + uD) * EconomyStep.FleetCrewPerHull(w.Regions[0], c, denom) : double.NaN;
                    W.Write($"Q\t{w.Year}\t{Nm(p)}\t{F(w.SoldierSize())}\t{F(a.V[0])}\t{F(p.Military)}\t{F(w.MilitaryReadiness)}\t{F(p.NavalShare)}\t{F(p.NeedLand)}\t{F(p.NeedInternal)}\t{F(p.NeedSea)}\t{F(a.V[1])}\t{F(a.V[2])}\t{F(a.V[3])}\t{F(a.V[4])}\t{F(a.V[5])}\t{F(ArmyShape.NavalSpendShareOf(p))}\t{F(a.V[6])}\t{F(a.V[7])}\t{F(a.V[8])}\t{F(p.FleetGauge)}\t{F(p.NavalPower)}\t{F(uW)}\t{F(uI)}\t{F(uD)}\t{F(crewNeed)}\t{a.Top}\t{F(a.V[9])}\t{F(a.V[10])}\t{F(a.V[11])}\t{F(a.V[12])}\n");
                }
            if (q == quarters - 1)
            {
                if (ByDip.TryGetValue(sim.Diplomacy, out var ev)) foreach (var s in ev.L) W.Write(s + "\n");
                W.Dispose();
            }
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/ArmyShape.cs', [
    ("        }\n    }\n\n    /// <summary>★敵意 h(i,j)∈[0,1)",
     "        }\n        if (Nvq195Diag.On) Nvq195Diag.Need(dip, ps, hrLand, hrCross, hrSea, unrest, c);\n    }\n\n    /// <summary>★敵意 h(i,j)∈[0,1)"),
])
edit('PriceOfBread.SimCore/Territory.cs', [
    ("        bool wasCapital = r.Id == 0;\n",
     "        bool wasCapital = r.Id == 0;\n        if (Nvq195Diag.On) Nvq195Diag.Move(from, to, r, how.ToString(), wasCapital);\n"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var nvq195M = Nvq195Diag.On ? new Nvq195Diag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                nvq195M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
