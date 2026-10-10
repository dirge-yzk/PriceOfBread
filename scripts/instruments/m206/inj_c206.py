"""[計測の写しだけ] (206) 線T 後半：清の海軍の桁＝海の必要量の内訳と交易の重みの計器（読むだけ）。
環境変数 POB_C206_DIR の下に c<seed>.txt。未設定なら1命令も走らない（清算の配列も確保しない）。
  T 行＝年に1回（q%4==3）の国ごと：年・国・人口・実質GDP（Gdp）・金建ての名目GDP（NominalGdp×為替）・為替・
        直近の清算の越境の交易額（輸入 CIF＋輸出 FOB＝LastImport＋LastExport）・平滑の交易額（TradeValueSmoothed＝今の海路の接触）・今の重み（÷平均）・
        直近の清算の越境の交易のうち海路の成約（seaA）の額・陸路の額・海路の額の平滑（同じ 0.1 の指数平滑＝計器の中だけ）・その重み（÷平均）・
        港の取扱高（海路の成約・同じ国の沿岸を含む）・その重み・
        海の必要量・艦隊の比の項（今）・反実仮想の艦隊の比の項（同じ盤面・静的）＝重み1（(194) の前）・重み＝海路の越境の交易の平滑・重み＝港の取扱高・
        海越えの項（＝海の必要量−艦隊の比の項）・海軍力・艦の強さのゲージ・海の割合（NavalSpendShareOf）・
        艦隊の比の項（今）の寄与の上位3（相手・量）・越境の交易の相手国の上位3（相手・額・海路の割合）
inj_navy_n と inj_nvq195 の後に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_c206.py <木の根>"""
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

/// <summary>[計測の写しだけ] (206) 海の必要量の内訳と交易の重み（読むだけ）。</summary>
public static class C206Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_C206_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Rec { public double Sea, Land, SeaRef, LandRef; public string Partners = "-"; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, Rec> ByW = new();
    public sealed class Acc { public readonly double[] V = new double[24]; public double SeaEma, SeaRefEma, AllRefEma; public string Top = "-"; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Power, Acc> ByP = new();
    static Acc For(Power p) => ByP.GetValue(p, _ => new Acc());
    static string Nm(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    static string NmW(World w) => w.SelfPower is { } sp ? Nm(sp) : "?";
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    static double PortT(World w) { double t = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active && r.IsPort) t += r.LastPortThroughput; return t; }

    /// <summary>BilateralMarket の清算の終わり（LastImportValue を書く行の後）で国ごとに呼ぶ。</summary>
    public static void Clear(System.Collections.Generic.IReadOnlyList<Simulation> nations, int i, double[] sea, double[] land, double[] pv, double[] ps, int nn, double[] seaR, double[] landR)
    {
        var w = nations[i].World;
        var rec = ByW.GetValue(w, _ => new Rec());
        rec.Sea = sea[i]; rec.Land = land[i]; rec.SeaRef = seaR[i]; rec.LandRef = landR[i];
        var idx = new int[3] { -1, -1, -1 };
        for (int k = 0; k < nn; k++)
        {
            double v = pv[i * nn + k];
            if (!(v > 0)) continue;
            for (int s = 0; s < 3; s++)
                if (idx[s] < 0 || v > pv[i * nn + idx[s]]) { for (int u = 2; u > s; u--) idx[u] = idx[u - 1]; idx[s] = k; break; }
        }
        var sb = new System.Text.StringBuilder();
        for (int s = 0; s < 3; s++)
        {
            if (idx[s] < 0) break;
            double v = pv[i * nn + idx[s]];
            if (sb.Length > 0) sb.Append(',');
            sb.Append(NmW(nations[idx[s]].World)).Append(':').Append(F(v)).Append(':').Append(F(v > 0 ? ps[i * nn + idx[s]] / v : 0));
        }
        rec.Partners = sb.Length > 0 ? sb.ToString() : "-";
    }

    /// <summary>ArmyShape.Compute の終わりで呼ぶ（同じ盤面の重み hrSea と不穏度 u をそのまま読む＝反実仮想は静的）。</summary>
    public static void Need(Diplomacy dip, System.Collections.Generic.List<Power> ps, double[] hrSea, double[] unrest, Coefficients c)
    {
        int n = ps.Count;
        var raw = new double[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                if (i == j) continue;
                double h = ArmyShape.Hostility(dip, ps[i], ps[j], c);
                double sea = 0;
                for (int b = 0; b < Basins.Count; b++) sea += ps[i].BasinPresence[b] * ps[j].BasinPresence[b];
                raw[j * n + i] = h * sea;
            }
        var wC = new double[n]; var wS = new double[n]; var wP = new double[n]; var one = new double[n]; var wR = new double[n]; var wAR = new double[n];
        double sC = 0, sS = 0, sP = 0, sR = 0, sAR = 0;
        for (int i = 0; i < n; i++)
        {
            var w = ps[i].Nation!;
            var a = For(ps[i]);
            double cx = w.TradeValueSmoothed; cx = cx > 0 ? cx : 0;
            double seaLast = ByW.TryGetValue(w, out var rec) ? rec.Sea : 0;
            a.SeaEma += 0.1 * (seaLast - a.SeaEma);
            a.SeaRefEma += 0.1 * ((rec?.SeaRef ?? 0) - a.SeaRefEma); a.AllRefEma += 0.1 * ((rec?.SeaRef ?? 0) + (rec?.LandRef ?? 0) - a.AllRefEma);
            wR[i] = a.SeaRefEma > 0 ? a.SeaRefEma : 0; sR += wR[i]; wAR[i] = a.AllRefEma > 0 ? a.AllRefEma : 0; sAR += wAR[i];
            a.V[16] = rec?.SeaRef ?? 0; a.V[17] = rec?.LandRef ?? 0;
            double pt = PortT(w);
            wC[i] = cx; wS[i] = a.SeaEma > 0 ? a.SeaEma : 0; wP[i] = pt > 0 ? pt : 0; one[i] = 1;
            sC += wC[i]; sS += wS[i]; sP += wP[i];
            a.V[0] = cx; a.V[2] = seaLast; a.V[3] = rec?.Land ?? 0; a.V[4] = a.SeaEma; a.V[6] = pt;
        }
        double mC = sC / n, mS = sS / n, mP = sP / n, mR = sR / n, mAR = sAR / n;
        for (int i = 0; i < n; i++) { var a = For(ps[i]); a.V[1] = mC > 0 ? wC[i] / mC : 1; a.V[5] = mS > 0 ? wS[i] / mS : 1; a.V[7] = mP > 0 ? wP[i] / mP : 1; }
        double[] Fleet(double[] wv, double mean, int[]? top1, double[]? topv, int mode = 0)
        {
            var f = new double[n];
            for (int j = 0; j < n; j++)
            {
                double sum = unrest[j], s0 = 0;
                for (int k = 0; k < n; k++) { sum += raw[j * n + k] * (mean > 0 ? wv[k] / mean : 1.0); s0 += raw[j * n + k]; }
                double scale = 1.0;
                if (mode == 1) { double sw = sum - unrest[j]; if (!(sw > 1e-12)) continue; scale = (s0 / (unrest[j] + s0)) / sw; sum = 1.0; }
                if (mode == 2) { double self = 0; for (int b = 0; b < Basins.Count; b++) self += ps[j].BasinPresence[b] * ps[j].BasinPresence[b]; sum += self * (mean > 0 ? wv[j] / mean : 1.0); }
                if (!(sum > 1e-12)) continue;
                for (int i = 0; i < n; i++)
                {
                    double v = raw[j * n + i] * (mean > 0 ? wv[i] / mean : 1.0) * scale / sum * ps[j].NavalPower;
                    f[i] += v;
                    if (top1 is not null && v > 0)
                        for (int s = 0; s < 3; s++)
                            if (top1[i * 3 + s] < 0 || v > topv![i * 3 + s]) { for (int u = 2; u > s; u--) { top1[i * 3 + u] = top1[i * 3 + u - 1]; topv![i * 3 + u] = topv[i * 3 + u - 1]; } top1[i * 3 + s] = j; topv![i * 3 + s] = v; break; }
                }
            }
            return f;
        }
        var t1 = new int[n * 3]; System.Array.Fill(t1, -1); var tv = new double[n * 3];
        var fNow = Fleet(wC, mC, t1, tv); var f1 = Fleet(one, 0, null, null); var fS = Fleet(wS, mS, null, null); var fP = Fleet(wP, mP, null, null);
        var fN = Fleet(wC, mC, null, null, 1); var fI = Fleet(wC, mC, null, null, 2); var fR = Fleet(wR, mR, null, null); var fAR = Fleet(wAR, mAR, null, null); var fSN = Fleet(wS, mS, null, null, 1);
        for (int i = 0; i < n; i++)
        {
            var a = For(ps[i]);
            a.V[8] = fNow[i]; a.V[9] = f1[i]; a.V[10] = fS[i]; a.V[11] = fP[i]; a.V[12] = ps[i].NeedSea - fNow[i];
            a.V[13] = fN[i]; a.V[14] = fI[i]; a.V[15] = fR[i]; a.V[18] = fAR[i]; a.V[19] = fSN[i]; a.V[20] = mR > 0 ? wR[i] / mR : 1; a.V[21] = mAR > 0 ? wAR[i] / mAR : 1;
            var sb = new System.Text.StringBuilder();
            for (int s = 0; s < 3; s++) { int j = t1[i * 3 + s]; if (j < 0) break; if (sb.Length > 0) sb.Append(','); sb.Append(Nm(ps[j])).Append(':').Append(F(tv[i * 3 + s])); }
            a.Top = sb.Length > 0 ? sb.ToString() : "-";
        }
    }

    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"c{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#T\tyr\tnation\tpop\tgdp\tngdpG\tfx\ttradeLast\tcontact\twNow\tseaLast\tlandLast\tseaEma\twSea\tportT\twPort\tneedSea\tfleetNow\tfleet1\tfleetSea\tfleetPort\tcrossT\tnaval\tgauge\tshare\tfleetN\tfleetI\tfleetR\tfleetAR\tfleetSN\tseaRef\tlandRef\twR\twAR\ttopFleet\tpartners\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
                foreach (var p in sim.Diplomacy.Powers)
                {
                    if (p.Nation is not { } w || !(w.TotalPopulation > 0) || w.Regions.Count == 0) continue;
                    var a = For(p);
                    string partners = ByW.TryGetValue(w, out var rec) ? rec.Partners : "-";
                    double ng = w.NominalGdp * w.ExchangeRate;
                    W.Write($"T\t{w.Year}\t{Nm(p)}\t{F(w.TotalPopulation)}\t{F(w.Gdp)}\t{F(ng)}\t{F(w.ExchangeRate)}\t{F(w.LastImportValue + w.LastExportValue)}\t{F(a.V[0])}\t{F(a.V[1])}\t{F(a.V[2])}\t{F(a.V[3])}\t{F(a.V[4])}\t{F(a.V[5])}\t{F(a.V[6])}\t{F(a.V[7])}\t{F(p.NeedSea)}\t{F(a.V[8])}\t{F(a.V[9])}\t{F(a.V[10])}\t{F(a.V[11])}\t{F(a.V[12])}\t{F(p.NavalPower)}\t{F(p.FleetGauge)}\t{F(ArmyShape.NavalSpendShareOf(p))}\t{F(a.V[13])}\t{F(a.V[14])}\t{F(a.V[15])}\t{F(a.V[18])}\t{F(a.V[19])}\t{F(a.V[16])}\t{F(a.V[17])}\t{F(a.V[20])}\t{F(a.V[21])}\t{a.Top}\t{partners}\n");
                }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/ArmyShape.cs', [
    ("    }\n\n    /// <summary>★敵意 h(i,j)∈[0,1)",
     "        if (C206Diag.On) C206Diag.Need(dip, ps, hrSea, unrest, c);\n    }\n\n    /// <summary>★敵意 h(i,j)∈[0,1)"),
], TAIL)
edit('PriceOfBread.SimCore/BilateralMarket.cs', [
    ("        var importVal = new double[nn]; var exportVal = new double[nn];",
     "        var c206S = C206Diag.On ? new double[nn] : null; var c206L = C206Diag.On ? new double[nn] : null; var c206P = C206Diag.On ? new double[nn * nn] : null; var c206PS = C206Diag.On ? new double[nn * nn] : null; var c206SR = C206Diag.On ? new double[nn] : null; var c206LR = C206Diag.On ? new double[nn] : null;\n        var importVal = new double[nn]; var exportVal = new double[nn];"),
    ("                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }\n",
     "                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }\n"
     "                    if (c206S is not null && crossA) { if (seaA) { c206S[ni] += vA + frG; c206S[siA] += vA; c206PS![ni * nn + siA] += vA + frG; c206PS[siA * nn + ni] += vA; } else { c206L![ni] += vA + frG; c206L[siA] += vA; } c206P![ni * nn + siA] += vA + frG; c206P[siA * nn + ni] += vA; double rv = qA * gd.RefPrice; if (seaA) { c206SR![ni] += rv; c206SR[siA] += rv; } else { c206LR![ni] += rv; c206LR[siA] += rv; } }\n"),
    ("            w0.LastImportValue = importVal[i];\n",
     "            w0.LastImportValue = importVal[i];\n            if (c206S is not null) C206Diag.Clear(nations, i, c206S, c206L!, c206P!, c206PS!, nn, c206SR!, c206LR!);\n"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var c206M = C206Diag.On ? new C206Diag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                c206M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
