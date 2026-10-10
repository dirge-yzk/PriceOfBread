"""[計測の写しだけ] S3c の局所の誤差：同じ盤面（セーブ）から、刻みと頻度の腕ごとに1四半期ずつ回し、処理の出力の差を基準の腕（HEAD の挙動）と比べる。
  Simulation に建設と信用の旬の長さの上書き（静的・写しだけ）を足し、Runner に s3cloc <seed> <Q1,Q2,...> を足す。
  腕＝ base（価格5日・建設と信用は日次＝HEAD）／P1（価格1日）／P10／P15／C5（建設だけ旬5）／K5（信用だけ旬5）／CK5（両方旬5）／A10（価格10＋両方旬10）／A15／Q（清算を四半期1回）
  出力＝価格・四半期の輸入量・POP の補給の充足・国庫・与信・信頼・工事（棟＋残り）・充足<0.3 の国の数"""
import sys
root = sys.argv[1]

def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); s = raw.decode('utf-8-sig')
    for old, new in pairs:
        assert s.count(old) == 1, (rel, old[:80], s.count(old))
        s = s.replace(old, new)
    s += tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

edit('PriceOfBread.SimCore/Simulation.cs', [
    ('        if (spanStart) World.StepConstruction(hFlow);',
     '        { int hc = S3cConstrSpan > 0 ? S3cConstrSpan : hFlow; if (_dayInQuarter % hc == 0) World.StepConstruction(hc); }'),
    ('        if (spanStart)\n        {\n            (double NominalGdp, double AvgSat, double MeanInd)? creditMonth = null;',
     '        int hk = S3cCreditSpan > 0 ? S3cCreditSpan : hFlow;\n        if (_dayInQuarter % hk == 0)\n        {\n            (double NominalGdp, double AvgSat, double MeanInd)? creditMonth = null;'),
    ('            Credit.Step(World, World.Coef, creditMonth, hFlow);',
     '            Credit.Step(World, World.Coef, creditMonth, hk);'),
    ('    public void StepDayDaily()\n    {\n',
     '    internal static int S3cConstrSpan = 0, S3cCreditSpan = 0;   // [計測の写しだけ] 0＝価格の旬に従う\n    public void StepDayDaily()\n    {\n'),
])

CMD = r'''
if (args.Length > 0 && args[0] == "s3cloc")
{
    ulong lseed = args.Length > 1 && ulong.TryParse(args[1], out var ls1) ? ls1 : 1789UL;
    var qs = (args.Length > 2 ? args[2] : "300").Split(',').Select(int.Parse).ToArray();
    var only = args.Length > 3 && args[3] != "-" ? new HashSet<string>(args[3].Split(',')) : null;
    string refArm = args.Length > 4 ? args[4] : "base";
    var sim0 = new MultiNationSim(WorldData.Nations(), lseed, _ => new UtilityAgent());
    foreach (var n in sim0.Nations) n.TelemetryEnabled = false;
    sim0.Diplomacy.Log.Enabled = false;
    sim0.StartWarmup();
    int done = 0;
    var inv = System.Globalization.CultureInfo.InvariantCulture;
    // 腕＝（名前, 価格の刻み, 建設の旬, 信用の旬, 四半期清算）
    var arms = new List<(string Name, int H, int Cs, int Ks, bool Q)> {
        ("base", 5, 1, 1, false), ("P1", 1, 1, 1, false), ("P10", 10, 1, 1, false), ("P15", 15, 1, 1, false),
        ("C5", 5, 5, 1, false), ("K5", 5, 1, 5, false), ("CK5", 5, 5, 5, false), ("C10", 5, 10, 1, false), ("K10", 5, 1, 10, false),
        ("A10", 10, 10, 10, false), ("A15", 15, 15, 15, false), ("Q", 5, 1, 1, true) };
    foreach (int target in qs)
    {
        while (done < target) { sim0.StepQuarter(); done++; }
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim0, ms);
        int yr0 = sim0.Nations[0].World.Year;
        System.Console.WriteLine($"=== seed {lseed} Q{target}（{yr0}年）国 {sim0.Nations.Count}");
        var res = new Dictionary<string, Dictionary<string, double[]>>();
        var keyOf = new Dictionary<string, string[]>();
        foreach (var arm in arms)
        {
            if (only is not null && arm.Name != refArm && !only.Contains(arm.Name)) continue;
            ms.Position = 0;
            var s = SaveGame.Load(ms);
            foreach (var n in s.Nations) n.TelemetryEnabled = false;
            s.Diplomacy.Log.Enabled = false;
            foreach (var n in s.Nations) { n.World.Coef.PriceFlowStepDays = arm.H; if (arm.Q) n.World.Coef.TradeMonthlyClearing = 0; }
            s.Coef.PriceFlowStepDays = arm.H; if (arm.Q) s.Coef.TradeMonthlyClearing = 0;
            Simulation.S3cConstrSpan = arm.Cs; Simulation.S3cCreditSpan = arm.Ks;
            // 四半期の輸入量（州×財）＝月末の清算の後の LastImport の和（植民地の輸入も含む＝腕の間で同じ）
            var impQ = new Dictionary<(string, string, int), double>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int d = 0; d < Calendar.DaysPerQuarter; d++)
            {
                s.StepDay();
                if ((d + 1) % Calendar.DaysPerMonth == 0)
                    foreach (var n in s.Nations) foreach (var r in n.World.Regions) foreach (var g in r.Goods)
                    { var k = (n.World.SelfPower?.Name ?? "", r.Name, g.Id); impQ[k] = impQ.GetValueOrDefault(k) + r.Markets[g.Id].LastImport; }
            }
            double secs = sw.Elapsed.TotalSeconds;
            Simulation.S3cConstrSpan = 0; Simulation.S3cCreditSpan = 0;
            // 出力を鍵つきで集める（国・州・財／国・州・POP の添字）＝腕の間で国や州の集合が違っても鍵で突き合わせる
            var o = new Dictionary<string, double[]>();
            var keys = new Dictionary<string, List<string>>();
            void Put(string series, string key, double v, double wgt)
            {
                if (!keys.TryGetValue(series, out var kl)) { kl = new List<string>(); keys[series] = kl; }
                kl.Add(key);
                o[series + "\u0001" + key] = new[] { v, wgt };
            }
            int low03 = 0;
            foreach (var n in s.Nations)
            {
                var w = n.World; string nn = w.SelfPower?.Name ?? "";
                double popSum = 0, fillSum = 0;
                foreach (var r in w.Regions)
                {
                    if (r.State != RegionState.Active) continue;
                    foreach (var g in r.Goods)
                    {
                        var m = r.Markets[g.Id];
                        double dval = m.LastDemand * g.RefPrice;
                        if (m.LastDemand > 1e-9 || m.LastProduced > 1e-9) Put("価格", $"{nn}/{r.Name}/{g.Id}", m.Price, dval);
                        double iq = impQ.GetValueOrDefault((nn, r.Name, g.Id));
                        Put("輸入量(四半期)", $"{nn}/{r.Name}/{g.Id}", iq, iq * g.RefPrice);
                    }
                    for (int pi = 0; pi < r.Pops.Count; pi++)
                    {
                        var p = r.Pops[pi];
                        Put("POPの補給の充足", $"{nn}/{r.Name}/{pi}/{p.ClassName}", p.SupplyFill, p.Size);
                        popSum += p.Size; fillSum += p.Size * p.SupplyFill;
                    }
                }
                double nf = popSum > 0 ? fillSum / popSum : 1;
                if (!(nf >= 0.3)) low03++;
                Put("国の補給の充足", nn, nf, popSum);
                Put("国庫", nn, w.Treasury, System.Math.Abs(w.Treasury));
                if (w.Bank is { } b) { Put("与信", nn, b.Loans, b.Loans); Put("信頼", nn, b.Confidence, 1); }
                double built = 0, left = 0;
                foreach (var r in w.Regions) foreach (var p in r.Producers) { built += p.BuiltScale; left += p.ConstructionLeft; }
                Put("工事（棟）", nn, built, built); Put("工事（残り日数）", nn, left, left);
                double impV = 0; foreach (var r in w.Regions) foreach (var g in r.Goods) impV += impQ.GetValueOrDefault((nn, r.Name, g.Id)) * g.RefPrice;
                Put("国の輸入額(参照価格)", nn, impV, impV);
            }
            res[arm.Name] = o;
            keyOf[arm.Name] = keys.SelectMany(kv => kv.Value.Select(k => kv.Key + "\u0001" + k)).ToArray();
            System.Console.WriteLine($"  腕 {arm.Name}（価格 h={arm.H}・建設の旬 {arm.Cs}・信用の旬 {arm.Ks}・清算 {(arm.Q ? "四半期" : "月次")}）1四半期 {secs:F2}秒・充足<0.3 の国 {low03}");
        }
        var bo = res[refArm];
        string[] series = { "価格", "輸入量(四半期)", "POPの補給の充足", "国の補給の充足", "国の輸入額(参照価格)", "国庫", "与信", "信頼", "工事（棟）", "工事（残り日数）" };
        foreach (var arm in arms)
        {
            if (arm.Name == refArm || !res.ContainsKey(arm.Name)) continue;
            var ao = res[arm.Name];
            var line = new System.Text.StringBuilder($"  [{arm.Name} 対 {refArm}]");
            foreach (var se in series)
            {
                // 相対の差＝|a−b|/max(|a|,|b|)。重みつき＝Σ重み×差／Σ重み（重み＝需要額・輸入額・人口・額）。有意＝重みが系列の和の1e-6 以上の鍵だけ。
                var d = new List<double>(); double wsum = 0, wdiff = 0, wtot = 0, absMax = 0; int n5 = 0, n5s = 0, miss = 0; string worst = "", absWorst = ""; double worstV = -1;
                foreach (var kv in bo) { if (!kv.Key.StartsWith(se + "\u0001")) continue; wtot += System.Math.Abs(kv.Value[1]); }
                foreach (var kv in bo)
                {
                    if (!kv.Key.StartsWith(se + "\u0001")) continue;
                    if (!ao.TryGetValue(kv.Key, out var av)) { miss++; continue; }
                    double x = kv.Value[0], y = av[0], den = System.Math.Max(System.Math.Abs(x), System.Math.Abs(y));
                    double rd = den > 1e-12 ? System.Math.Abs(x - y) / den : 0;
                    if (double.IsNaN(rd)) rd = 1;
                    if (System.Math.Abs(x - y) > absMax) { absMax = System.Math.Abs(x - y); absWorst = kv.Key.Substring(se.Length + 1) + $"（{x:G4}→{y:G4}）"; }
                    double wg = System.Math.Max(System.Math.Abs(kv.Value[1]), System.Math.Abs(av[1]));
                    bool sig = wtot > 0 && wg >= 1e-6 * wtot;
                    d.Add(rd); if (rd > 0.05) { n5++; if (sig) n5s++; }
                    if (sig && rd > worstV) { worstV = rd; worst = kv.Key.Substring(se.Length + 1); }
                    wsum += wg; wdiff += wg * rd;
                }
                d.Sort();
                double mx = d.Count > 0 ? d[^1] : 0, p99 = d.Count > 0 ? d[(int)(0.99 * (d.Count - 1))] : 0, p999 = d.Count > 0 ? d[(int)(0.999 * (d.Count - 1))] : 0;
                line.Append($"\n    {se}: 件数 {d.Count}・重みつき平均 {(wsum > 0 ? wdiff / wsum : 0):E2}・99%点 {p99:E2}・99.9%点 {p999:E2}・最大 {mx:E2}・>5% {n5}（有意 {n5s}）・欠け {miss}" + (worstV >= 0 ? $"・有意の最大 {worstV:E2}＝{worst}" : "") + $"・絶対の差の最大 {absMax:G3}＝{absWorst}");
            }
            System.Console.WriteLine(line.ToString());
        }
        }
    }
    return;
}
'''
edit('PriceOfBread.Runner/Program.cs', [
    ('if (args.Length > 0 && args[0] == "savetest")\n',
     CMD.lstrip('\n') + '\nif (args.Length > 0 && args[0] == "savetest")\n'),
])
print('ok s3cloc', root)

# ---- v2（2026-09-30）：小刻みはリポジトリの MarketFlow.SubStep＝写しに「小刻みを切る」スイッチだけ足す（base＝HEAD の挙動を再現）----
edit('PriceOfBread.SimCore/MarketFlow.cs', [
    ('    private static int SubStep(int h, double lnDev, Coefficients coef, double div)\n    {\n',
     '    internal static bool S3cNoSub = false;   // [計測の写しだけ]\n    private static int SubStep(int h, double lnDev, Coefficients coef, double div)\n    {\n        if (S3cNoSub) return h;\n'),
])
edit('PriceOfBread.Runner/Program.cs', [
    ('''        ("A10", 10, 10, 10, false), ("A15", 15, 15, 15, false), ("Q", 5, 1, 1, true) };''',
     '''        ("A10", 10, 10, 10, false), ("A15", 15, 15, 15, false), ("Q", 5, 1, 1, true), ("NEW", 10, 10, 10, false), ("P10s", 10, 1, 1, false) };'''),
    ('''            Simulation.S3cConstrSpan = arm.Cs; Simulation.S3cCreditSpan = arm.Ks;''',
     '''            Simulation.S3cConstrSpan = arm.Cs; Simulation.S3cCreditSpan = arm.Ks;
            MarketFlow.S3cNoSub = !(arm.Name == "NEW" || arm.Name == "P10s");   // 小刻みは NEW（採る形）と P10s だけ＝他の腕は HEAD の閉じた式（1歩）'''),
    ('''            Simulation.S3cConstrSpan = 0; Simulation.S3cCreditSpan = 0;''',
     '''            Simulation.S3cConstrSpan = 0; Simulation.S3cCreditSpan = 0; MarketFlow.S3cNoSub = false;'''),
    # 比べる相手を複数（既定＝base と P1）
    ('''    string refArm = args.Length > 4 ? args[4] : "base";''',
     '''    var refArms = (args.Length > 4 ? args[4] : "base,P1").Split(',');
    string refArm = refArms[0];'''),
    ('''            if (only is not null && arm.Name != refArm && !only.Contains(arm.Name)) continue;''',
     '''            if (only is not null && !refArms.Contains(arm.Name) && !only.Contains(arm.Name)) continue;'''),
    ('''        var bo = res[refArm];''',
     '''        foreach (var refA in refArms) { refArm = refA; if (!res.ContainsKey(refArm)) continue;
        var bo = res[refArm];'''),
    ('''        while (done < target) { sim0.StepQuarter(); done++; }''',
     '''        Simulation.S3cConstrSpan = 1; Simulation.S3cCreditSpan = 1; MarketFlow.S3cNoSub = true;   // 盤面は HEAD の挙動で作る
        while (done < target) { sim0.StepQuarter(); done++; }'''),
    ('''    sim0.StartWarmup();''',
     '''    foreach (var n in sim0.Nations) n.World.Coef.PriceFlowStepDays = 5; sim0.Coef.PriceFlowStepDays = 5;
    Simulation.S3cConstrSpan = 1; Simulation.S3cCreditSpan = 1; MarketFlow.S3cNoSub = true;
    sim0.StartWarmup();'''),
])
