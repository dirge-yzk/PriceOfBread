using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PriceOfBread.SimCore;

namespace PriceOfBread.Runner;

/// <summary>外部の意思決定者（人間 or LLM）に1国を打たせるための対話ハーネス（2026-08-30・docs/20260830_AIにプレイさせる_2つの実験）。
///
/// ★ 使い方： dotnet PriceOfBread.Runner.dll play &lt;作業dir&gt; [国名=清] [進める四半期数=4] [seed=1789]
///   1) &lt;dir&gt;/state.bin が無ければ新規開始（★App と同じ手順＝プレイヤー国だけ NullAgent・IsPlayer・専売0・StartWarmup）
///   2) &lt;dir&gt;/decision.txt があれば `キー=値` 行を **NationPolicy へリフレクションで**適用し、decision.applied.txt へ退避
///      ⚠ NationPolicy は「**目標値**」であって結果ではない（現在値は World 側＝利益集団の力で律速して漸進）
///      ＝UI と同じ口を叩いているだけで、政治の律速は迂回していない
///   3) 指定四半期ぶん進める
///   4) &lt;dir&gt;/report.txt に状態を書く（＝プレイヤーが見る画面に相当）
///   5) &lt;dir&gt;/state.bin へ保存（SaveGame＝世界まるごと往復・savetest で 0.0E+000 実証済み）
///
/// ⚠ 決定論：この経路は selfplay / fidelity / テストに一切影響しない（別コマンド）。
/// ★ 打った手は decision.log.txt に追記され、後から決定論的に再生できる（ReplayAgent の素材）。</summary>
internal static class PlayHarness
{
    public static int Run(string[] args)
    {
        string dir = args.Length > 1 ? args[1] : "play";
        string playerName = args.Length > 2 ? args[2] : "清";
        int steps = args.Length > 3 && int.TryParse(args[3], out int s) ? s : 4;
        ulong seed = args.Length > 4 && ulong.TryParse(args[4], out ulong sd) ? sd : 1789UL;
        Directory.CreateDirectory(dir);
        string statePath = Path.Combine(dir, "state.bin");

        MultiNationSim sim;
        int playerIndex;
        NationConfig[] cfgs = WorldData.Nations();
        playerIndex = Array.FindIndex(cfgs, c => c.Name == playerName);
        if (playerIndex < 0) { Console.WriteLine($"国が見つからない: {playerName}"); return 2; }

        if (File.Exists(statePath))
        {
            using var fs = File.OpenRead(statePath);
            sim = SaveGame.Load(fs);
        }
        else
        {
            var playerCfg = cfgs[playerIndex];
            sim = new MultiNationSim(cfgs, seed,
                cfg => ReferenceEquals(cfg, playerCfg) ? new NullAgent() : (INationAgent)new UtilityAgent());
            sim.Diplomacy.Powers[playerIndex].IsPlayer = true;
            foreach (var n in sim.Nations) n.TelemetryEnabled = false;
            sim.StartWarmup();
            Console.WriteLine($"新規開始： {playerName}（index {playerIndex}）seed {seed}");
        }

        var world = sim.Nations[playerIndex].World;

        // ---- 2) 決定の適用（キー=値／NationPolicy のフィールド名）----
        string decPath = Path.Combine(dir, "decision.txt");
        var applied = new List<string>();
        var rawKept = new List<string>();   // ReplayAgent（清レポ提案・2026-08-31）：適用した生のコマンド行＝decision.raw.txt へ四半期スタンプ付きで退避（decision.log.txt は「old→new」整形＝再生不能だった）
        if (File.Exists(decPath))
        {
            foreach (string raw in File.ReadAllLines(decPath))
            {
                string line = raw.Trim();
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line[..hash].Trim();
                if (line.Length == 0) continue;
                rawKept.Add(line);
                if (line.StartsWith("!")) { applied.Add(RunVerb(world, line)); continue; }
                applied.Add(ApplyKeyValue(world, line));
            }
            File.AppendAllText(Path.Combine(dir, "decision.log.txt"),
                $"=== {world.Year}Q{world.Quarter} ===\n" + string.Join("\n", applied) + "\n", Encoding.UTF8);
            if (rawKept.Count > 0)
                File.AppendAllText(Path.Combine(dir, "decision.raw.txt"),
                    $"@{world.Year}Q{world.Quarter}\n" + string.Join("\n", rawKept) + "\n", Encoding.UTF8);
            File.Move(decPath, Path.Combine(dir, "decision.applied.txt"), overwrite: true);
        }

        // ---- 3) 進める ----
        int logStart = sim.Diplomacy.Log.Count;
        for (int i = 0; i < steps; i++) sim.StepQuarter();

        // ---- 4) 報告 ----
        var sb = new StringBuilder();
        WriteReport(sb, sim, playerIndex, applied, logStart);
        File.WriteAllText(Path.Combine(dir, "report.txt"), sb.ToString(), Encoding.UTF8);
        Console.Write(sb.ToString());

        // ---- 5) 保存 ----
        using (var fs = File.Create(statePath)) SaveGame.Save(sim, fs);
        return 0;
    }

    /// <summary>ReplayAgent（清レポ提案・2026-08-31）： dotnet PriceOfBread.Runner.dll play-replay &lt;dir&gt; [国名=清] [seed=1789] [追加四半期=0]
    /// &lt;dir&gt;/decision.raw.txt（@YYYYQn スタンプ＋生コマンド行）を読み、新規世界へ同じ四半期に同じ決定を適用して再生する。
    /// 同seed×同介入時点＝決定論でビット同一の軌道（LLMの非決定性はログで固定される）。最後に report.replay.txt を書く。
    /// play と別コマンド・state.bin には触れない＝元のセッションを壊さない。</summary>
    public static int Replay(string[] args)
    {
        string dir = args.Length > 1 ? args[1] : "play";
        string playerName = args.Length > 2 ? args[2] : "清";
        ulong seed = args.Length > 3 && ulong.TryParse(args[3], out ulong sd) ? sd : 1789UL;
        int extraQ = args.Length > 4 && int.TryParse(args[4], out int xq) ? xq : 0;
        string rawPath = Path.Combine(dir, "decision.raw.txt");
        if (!File.Exists(rawPath)) { Console.WriteLine($"decision.raw.txt が無い: {rawPath}（再生には生ログが要る＝2026-08-31以降の play が書く）"); return 2; }

        var script = new Dictionary<(int Y, int Q), List<string>>();
        (int Y, int Q)? cur = null;
        int lastY = 0, lastQ = 0;
        foreach (string raw in File.ReadAllLines(rawPath))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("@"))
            {
                int qi = line.IndexOf('Q');
                if (qi > 1 && int.TryParse(line[1..qi], out int y) && int.TryParse(line[(qi + 1)..], out int q))
                { cur = (y, q); lastY = y; lastQ = q; if (!script.ContainsKey((y, q))) script[(y, q)] = []; }
                continue;
            }
            if (cur is { } k) script[k].Add(line);
        }

        NationConfig[] cfgs = WorldData.Nations();
        int playerIndex = Array.FindIndex(cfgs, c => c.Name == playerName);
        if (playerIndex < 0) { Console.WriteLine($"国が見つからない: {playerName}"); return 2; }
        var playerCfg = cfgs[playerIndex];
        var sim = new MultiNationSim(cfgs, seed,
            cfg => ReferenceEquals(cfg, playerCfg) ? new NullAgent() : (INationAgent)new UtilityAgent());
        sim.Diplomacy.Powers[playerIndex].IsPlayer = true;
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.StartWarmup();
        var world = sim.Nations[playerIndex].World;
        Console.WriteLine($"再生開始： {playerName} seed {seed}　スクリプト {script.Count}時点・最終 {lastY}Q{lastQ}");

        int appliedN = 0, guard = 0;
        while ((world.Year < lastY || (world.Year == lastY && world.Quarter <= lastQ) || extraQ > 0) && guard++ < 2000)
        {
            if (script.TryGetValue((world.Year, world.Quarter), out var lines))
                foreach (var line in lines)
                {
                    string result = line.StartsWith("!") ? RunVerb(world, line) : ApplyKeyValue(world, line);
                    appliedN++;
                    if (result.Contains('⚠')) Console.WriteLine($"  {world.Year}Q{world.Quarter} {result}");
                }
            bool past = world.Year > lastY || (world.Year == lastY && world.Quarter > lastQ);
            if (past && extraQ > 0) extraQ--;
            sim.StepQuarter();
        }
        var sb = new StringBuilder();
        WriteReport(sb, sim, playerIndex, [$"（再生・適用 {appliedN} 手）"], 0);
        File.WriteAllText(Path.Combine(dir, "report.replay.txt"), sb.ToString(), Encoding.UTF8);
        Console.Write(sb.ToString());
        Console.WriteLine($"再生完了：{appliedN}手を適用・report.replay.txt へ出力（state.bin は不変）");
        return 0;
    }

    // 決定行（キー=値）の適用＝play 本体と同じ規則（World直下レバー→NationPolicy の順）。書式不正1行で
    // ハーネス全体が落ちないよう、World直下・NationPolicy の両分岐とも例外を飲んで警告文にする。
    private static string ApplyKeyValue(World world, string line)
    {
        int eq = line.IndexOf('=');
        if (eq <= 0) return $"⚠書式不正: {line}";
        string key = line[..eq].Trim(), val = line[(eq + 1)..].Trim();
        try
        {
            // ⚠World 直下のプレイヤーレバー（NationPolicy に無い。UIはスライダー/ボタンで持つ）
            if (key is "ModernizationFrac" or "Mobilized" or "ForceUnlock")
            {
                var wf = typeof(World).GetProperty(key, BindingFlags.Public | BindingFlags.Instance)!;
                object oldv = wf.GetValue(world)!;
                object nv = wf.PropertyType == typeof(bool) ? bool.Parse(val) : double.Parse(val, CultureInfo.InvariantCulture);
                wf.SetValue(world, nv);
                return $"{key}: {oldv} → {nv}　（★World直下のレバー）";
            }
            var f = typeof(NationPolicy).GetField(key, BindingFlags.Public | BindingFlags.Instance);
            if (f is null) return $"⚠不明なレバー: {key}";
            object v;
            if (f.FieldType == typeof(double)) v = double.Parse(val, CultureInfo.InvariantCulture);
            else if (f.FieldType == typeof(bool)) v = bool.Parse(val);
            else if (f.FieldType == typeof(int)) v = int.Parse(val, CultureInfo.InvariantCulture);
            else if (f.FieldType.IsEnum) v = Enum.Parse(f.FieldType, val, ignoreCase: true);
            else return $"⚠扱えない型: {key}（{f.FieldType.Name}）";
            object old = f.GetValue(world.Policy)!;
            f.SetValue(world.Policy, v);
            return $"{key}: {old} → {v}";
        }
        catch (Exception ex) { return $"⚠適用失敗 {key}={val}（{ex.GetType().Name}）"; }
    }

    private static void WriteReport(StringBuilder sb, MultiNationSim sim, int pi, List<string> applied, int logStart)
    {
        var w = sim.Nations[pi].World;
        var sp = w.SelfPower!;
        var coef = w.Coef;

        double pop = 0, urbanPop = 0, lit = 0, cons = 0, savings = 0;
        var byClass = new Dictionary<int, (double size, double lit, double sat, double mil, double sav)>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                pop += p.Size; if (p.IsUrban) urbanPop += p.Size;
                lit += p.Literacy * p.Size; cons += p.Consciousness * p.Size;
                savings += p.Savings.Value;
                byClass.TryGetValue(p.ClassId, out var acc);
                byClass[p.ClassId] = (acc.size + p.Size, acc.lit + p.Literacy * p.Size,
                                      acc.sat + p.Satisfaction * p.Size, acc.mil + p.Militancy * p.Size, acc.sav + p.Savings.Value);
            }
        }
        double N = Math.Max(1, pop);

        sb.AppendLine($"================ {w.Year}年 Q{w.Quarter}　{sp.Name} ================");
        sb.AppendLine($"政体 {w.Polity}　正統性 {w.Legitimacy:P0}　自由度 {w.Freedom:F2}　交戦 {(w.AtWarNow ? "★あり" : "なし")}");
        sb.AppendLine($"人口 {pop:N0}（都市 {urbanPop / N:P1}）　識字 {lit / N:P1}　生活水準 {NationalScore.MeanSatisfaction(w):F3}　闘争度 {Politics.AvgMassMilitancy(w):F3}　政治意識 {cons / N:F3}");   // ★2026-09-26（sweep-13）：生活水準と闘争度は画面・帯と同じ大衆の母集団（エンジンの1本）＝旧は全POPの別の数
        sb.AppendLine($"国庫 {w.Treasury:N0}　名目GDP {w.NominalGdpPerMonth:N0}　技術 {w.TechLevel:F2}（世界先端 {w.WorldTechLevel:F2}）　エリート {w.Elite:F3}　pop貯蓄計 {savings:N0}");

        sb.AppendLine();
        sb.AppendLine("---- 財政 ----");
        sb.AppendLine($"所得税 {w.TaxRate:P0}　人頭税 {w.PollTax:P0}　土地税 {w.LandTax:P0}　累進 {w.ProgressiveTax:P0}　専売の負担 {w.StateMonopolyBreadth:P0}（実効 {w.StateMonopolyBreadth * w.Coef.StateMonopolyMargin:P0}）");
        sb.AppendLine($"関税 {w.Tariff:P0}　教育費 {w.EducationSpending:P0}　行政費 {w.AdminSpending:P0}　社会保障 {w.SocialSecurity:P0}");
        sb.AppendLine($"★近代化投資 {w.ModernizationFrac:P0}（今四半期の予算 {w.ModernizationBudget():N0}）　総動員 {w.Mobilized}");
        sb.AppendLine($"陸軍維持費 {w.ArmyUpkeep:P0}　海軍維持費 {w.NavyUpkeep:P0}　徴税漏れ {w.LastTaxLeak:N0}　植民地維持 {w.LastColonialUpkeepPerMonth:N0}");

        var led = w.TreasuryLedger;
        string Cat(TreasuryCat c) => $"{c}:{led[(int)c]:N0}";
        sb.AppendLine("収入 " + string.Join(" ", new[] { TreasuryCat.Tax, TreasuryCat.Tariff, TreasuryCat.StateMonopoly, TreasuryCat.Borrow, TreasuryCat.StateFactory, TreasuryCat.TributeIn, TreasuryCat.OtherIn }.Where(c => Math.Abs(led[(int)c]) > 0.5).Select(Cat)));
        sb.AppendLine("支出 " + string.Join(" ", new[] { TreasuryCat.Army, TreasuryCat.Navy, TreasuryCat.Admin, TreasuryCat.Education, TreasuryCat.Welfare, TreasuryCat.Pension, TreasuryCat.Modernization, TreasuryCat.Interest, TreasuryCat.DebtRepay, TreasuryCat.FactoryBuild, TreasuryCat.Subsidy, TreasuryCat.Megaproject, TreasuryCat.ColonialUpkeep, TreasuryCat.WarCost, TreasuryCat.OtherOut }.Where(c => Math.Abs(led[(int)c]) > 0.5).Select(Cat)));

        sb.AppendLine();
        sb.AppendLine("---- 法（現在値 ／ 目標値）----");
        var pol = w.Policy;
        sb.AppendLine($"初等教育 {w.EducationLaw} ／ {pol.EducationLaw}　　参政権 {w.Suffrage} ／ {pol.Suffrage}　　報道 {w.Censorship} ／ {pol.Censorship}");
        sb.AppendLine($"穀物法 ／ {pol.CornLaws}　土地改革 ／ {pol.LandReform}　農奴解放 ／ {pol.SerfEmancipation}");
        sb.AppendLine($"徴兵 ／ {pol.ConscriptionLaw}　官僚 ／ {pol.Bureaucracy}　労組 ／ {pol.LaborUnion}　女性 ／ {pol.WomenRights}");
        sb.AppendLine($"アヘン保有 ／ {pol.OpiumPossession}　アヘン輸出禁止 ／ {pol.OpiumExportBan}　国教 ／ {pol.StateReligion}　独禁 ／ {pol.Antitrust}");

        sb.AppendLine();
        sb.AppendLine("---- 政治：各軸を動かす追い風／向かい風（正＝押せる・負＝抵抗）----");
        foreach (PolicyAxis ax in Enum.GetValues<PolicyAxis>())
        {
            double up = PolicyPreferences.NetSupport(w, ax, true);
            if (Math.Abs(up) < 1e-9) continue;
            var br = PolicyPreferences.SupportBreakdown(w, ax, true)
                        .Where(x => Math.Abs(x.Weight) > 1e-9)
                        .OrderByDescending(x => Math.Abs(x.Weight)).Take(4);
            sb.AppendLine($"  {ax,-18} 強化 {up,7:F3}　［{string.Join(" / ", br.Select(x => $"{x.Who}{x.Weight:+0.00;-0.00}"))}］");
        }

        sb.AppendLine();
        sb.AppendLine("---- 主食（首都州の市場）----");
        var cap = w.Region;
        sb.AppendLine("財         価格   参照   在庫    需要   生産   輸入   輸出");
        foreach (var g in cap.Goods)
        {
            if (g.Tier != GoodTier.Staple) continue;
            var m = cap.Markets[g.Id];
            sb.AppendLine($"{g.Name,-8} {m.Price,7:F3} {g.RefPrice,6:F3} {m.Stock,7:F0} {m.LastDemand,7:F0} {m.LastProduced,6:F0} {m.LastImport,6:F0} {m.LastExport,6:F0}");
        }

        sb.AppendLine();
        sb.AppendLine("---- 建設の束と主要財（首都州）----");
        int[] watch = { coef.SteelGoodId, coef.MachineGoodId, coef.CementGoodId, coef.LumberGoodId,
                        Catalog.CoalId, coef.OpiumGoodId, Catalog.IronId, Catalog.WheatId, Catalog.TimberId };   // 鉄31・機械部品51・麦15・木11
        sb.AppendLine("財         価格   参照  価格/参照  在庫    需要   生産   輸入");
        foreach (int gi in watch.Distinct())
        {
            if (gi < 0 || gi >= cap.Markets.Length) continue;
            var g = cap.GoodOf(gi); if (g is null) continue;
            var m = cap.Markets[gi];
            sb.AppendLine($"{g.Name,-8} {m.Price,7:F3} {g.RefPrice,6:F3} {(g.RefPrice > 1e-9 ? m.Price / g.RefPrice : 0),8:F2} {m.Stock,7:F0} {m.LastDemand,7:F0} {m.LastProduced,6:F0} {m.LastImport,6:F0}");
        }
        double bundleRef = coef.ConsSteel * Catalog.RefPrices[coef.SteelGoodId] + coef.ConsMachine * Catalog.RefPrices[coef.MachineGoodId]
                         + coef.ConsCement * Catalog.RefPrices[coef.CementGoodId] + coef.ConsLumber * Catalog.RefPrices[coef.LumberGoodId];
        double bundleNow = coef.ConsSteel * cap.Markets[coef.SteelGoodId].Price + coef.ConsMachine * cap.Markets[coef.MachineGoodId].Price
                         + coef.ConsCement * cap.Markets[coef.CementGoodId].Price + coef.ConsLumber * cap.Markets[coef.LumberGoodId].Price;
        sb.AppendLine($"建設1単位の束： 参照 {bundleRef * cap.PriceLevel:F2} ／ いまの市価 {bundleNow:F2}　（比 {(bundleRef * cap.PriceLevel > 1e-9 ? bundleNow / (bundleRef * cap.PriceLevel) : 0):F2}）");
        sb.AppendLine($"★建設可能率 {World.ConstructionAfford(w):F3}　（着工に要る床 {coef.FactoryStartAffordFloor:F2}／半減点は束の比 {coef.FactoryBuildPriceCeil:F1} 倍）");
        sb.AppendLine("主要財の産地（州：規模・産出）：");
        foreach (int gi in new[] { coef.SteelGoodId, coef.MachineGoodId, Catalog.IronId })
        {
            var g0 = cap.GoodOf(gi); if (g0 is null) continue;
            var sites = new List<string>();
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var pr in r.Producers)
                    if (pr.GoodId == gi && pr.Output > 1e-9) { sites.Add($"{r.Name}({pr.Scale:F2}/{pr.Output:F0})"); break; }
            }
            sb.AppendLine($"  {g0.Name,-6} {(sites.Count == 0 ? "★どの州にも産地なし" : string.Join(" ", sites.Take(10)))}");
        }

        sb.AppendLine();
        sb.AppendLine("---- 階級（人数／識字／生活水準／闘争度）----");
        foreach (var kv in byClass.OrderByDescending(x => x.Value.size).Take(14))
        {
            var (size, l, st, m2, sv) = kv.Value;
            double sz = Math.Max(1, size);
            sb.AppendLine($"  階級{kv.Key,2}  {size,12:N0}  識字{l / sz,5:P0}  水準{st / sz,6:F3}  闘争{m2 / sz,6:F3}  貯蓄{sv,12:N0}");
        }

        sb.AppendLine();
        sb.AppendLine("---- 州（上位10・工業化順）----");
        foreach (var r in w.Regions.Where(x => x.State == RegionState.Active)
                                    .OrderByDescending(x => x.TotalIndustrialization).Take(10))
            sb.AppendLine($"  {r.Name,-14} 人口{r.SumPopulation(),11:N0} 工業{r.TotalIndustrialization,6:F2}/{coef.IndustryInvestRegionCap:F1} 着工予約{r.StateBuildQueue,9:N0} 港{(r.IsPort ? "○" : "－")} 資本{(r.CapitalEnabled ? "○" : "×")}");

        sb.AppendLine();
        sb.AppendLine("---- 外交（緊張の高い相手 上位8）----");
        var dip = sim.Diplomacy;
        var others = dip.Powers.Where(p => !ReferenceEquals(p, sp)).ToList();
        foreach (var o in others.OrderByDescending(o => dip.Tension(sp, o)).Take(8))
            sb.AppendLine($"  {o.Name,-14} 緊張 {dip.Tension(sp, o),6:F1}　{(dip.IsAtWar(sp, o) ? "★交戦中" : "")}");
        if (sp.PendingWar is { } pw) sb.AppendLine($"  ⚠★最後通牒の待ち： {pw.Name}（要求を受け入れる／開戦）");
        if (sp.PendingCallToArms is { } ca) sb.AppendLine($"  ⚠★参戦要請の待ち： {ca.Caller.Name} が {ca.Enemy.Name} と交戦（受ける／断る）");

        sb.AppendLine();
        sb.AppendLine("---- 世界（国力上位10）----");
        foreach (var n in sim.Nations.OrderByDescending(x => x.World.ScoreStanding).Take(10))
        {
            var xw = n.World;
            sb.AppendLine($"  {xw.SelfPower?.Name,-14} 国力{xw.ScoreStanding,8:F0} 工業{xw.MeanIndustrialization,6:F2} 技術{xw.TechLevel,5:F2} 人口{TotalPop(xw),12:N0}");
        }

        sb.AppendLine();
        sb.AppendLine("---- 直近の出来事 ----");
        var log = sim.Diplomacy.Log;
        for (int i = Math.Max(logStart, log.Count - 40); i < log.Count; i++) sb.AppendLine($"  {log[i]}");

        if (applied.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("---- 適用した決定 ----");
            foreach (string a in applied) sb.AppendLine($"  {a}");
        }
    }

    /// <summary>プレイヤーの動作（World のメソッド＝UIのボタンと同じ口）。書式：
    /// !factory 州名 財名 ／ !sell 州名 財名 ／ !subsidy 州名 財名 率 ／
    /// !state 州名 財名 段 ／ !monopoly 財名 入|切 ／ !bias 財名 -1|0|1</summary>
    private static string RunVerb(World w, string line)
    {
        string[] a = line[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (a.Length == 0) return "⚠空の動作";
        int RegionOf(string name)
        {
            for (int i = 0; i < w.Regions.Count; i++) if (w.Regions[i].Name == name) return i;
            return -1;
        }
        int GoodOf(string name)
        {
            foreach (var g in w.Region.Goods) if (g.Name == name) return g.Id;
            return -1;
        }
        try
        {
            switch (a[0])
            {
                // （!build＝1棟単位の手動建設は 2026-09-26 に削除＝UIにもAIにも無い動詞だった・国家主導の工業化は近代化投資のレバー1本）
                case "factory":
                {
                    int ri = RegionOf(a[1]); int gi = GoodOf(a[2]);
                    if (ri < 0) return $"⚠州が無い: {a[1]}";
                    if (gi < 0) return $"⚠財が無い: {a[2]}";
                    // ★理由の実名化（清レポート§4-3・2026-08-31）：旧「〜の可能性も」の当てずっぽうを CanBuildFactory（BuildFactoryと同一条件の理由つき版）へ置換
                    var (can, reason) = w.CanBuildFactory(ri, gi);
                    bool built = can && w.BuildFactory(ri, gi);
                    return built ? $"工場 {a[1]}/{a[2]}: 着工"
                                 : $"⚠工場 {a[1]}/{a[2]} 不可（{reason}）";
                }
                case "sell":
                {
                    int ri = RegionOf(a[1]); int gi = GoodOf(a[2]);
                    if (ri < 0 || gi < 0) return "⚠州/財が無い";
                    return $"売却 {a[1]}/{a[2]}: {(w.SellFactory(ri, gi) ? "成立" : "⚠不可")}";
                }
                case "subsidy":
                {
                    int ri = RegionOf(a[1]); int gi = GoodOf(a[2]);
                    if (ri < 0 || gi < 0) return "⚠州/財が無い";
                    var (ok, priv, why) = w.CanSubsidize(ri, gi);
                    if (!ok) return $"⚠補助金 {a[1]}/{a[2]} 不可：{(why.Length > 0 ? why : "産地なし")}";
                    double rate = double.Parse(a[3], CultureInfo.InvariantCulture);
                    w.SetSubsidyRate(ri, gi, rate);
                    return $"補助金 {a[1]}/{a[2]} = {rate:P0}（民営持分 {priv:P0}）";
                }
                case "state":
                {
                    int ri = RegionOf(a[1]); int gi = GoodOf(a[2]);
                    if (ri < 0 || gi < 0) return "⚠州/財が無い";
                    int t = int.Parse(a[3], CultureInfo.InvariantCulture);
                    return $"国有化目標 {a[1]}/{a[2]} → {t}: {(w.SetStateTarget(ri, gi, t) ? "受理" : "⚠不可")}";
                }
                case "monopoly":
                {
                    int gi = GoodOf(a[1]); if (gi < 0) return $"⚠財が無い: {a[1]}";
                    // ★2026-09-20：率は引数に取らない（材ごとの ON/OFF だけ）＝生産タブのチェックボックスと同じ1本を叩く。
                    bool on = a[2] is "1" or "on" or "true";
                    bool moved = w.SetStateMonopolyOn(gi, on);
                    return $"専売 {a[1]} = {(on ? "入" : "切")}{(moved ? "" : "（変化なし／政治が通さない）")}";
                }
                case "bias":
                {
                    int gi = GoodOf(a[1]); if (gi < 0) return $"⚠財が無い: {a[1]}";
                    int b = int.Parse(a[2], CultureInfo.InvariantCulture);
                    w.SetTradeBias(gi, b);
                    return $"売買方針 {a[1]} = {b}";
                }
                default: return $"⚠不明な動作: {a[0]}";
            }
        }
        catch (Exception ex) { return $"⚠動作失敗 {line}（{ex.GetType().Name}: {ex.Message}）"; }
    }

    private static double TotalPop(World w)
    {
        double t = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) t += r.SumPopulation();
        return t;
    }
}
