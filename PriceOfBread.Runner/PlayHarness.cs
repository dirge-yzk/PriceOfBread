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
///   (419) 画面の問いかけ（最後通牒・参戦要請・国内の危機・総選挙の公示）に答える動詞は `!ultimatum`・`!callarms`・`!election`（`!crisis` は内容の照会のみ）。全国工場の目標は `!target`・巨大建造物の着工は `!mega`。待っている問いかけと使える動詞は報告の 2 行目。
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
    /// <summary>★(435) 報告・決定の記録の書き込みは BOM なしの UTF-8（旧は <c>Encoding.UTF8</c> が先頭に BOM を付け、grep の行頭一致が効かなかった＝初見の試しのテストプレイの報告 §4）。</summary>
    internal static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

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
        world.ForceUnlock = true;   // ★(419) 再征服は常時ON（App の InitAfterSimReady と同じ＝新規でも state.bin からの再開でも。★(420) 訂正：ForceUnlock はセーブに載る〔World の自動プロパティ＝SaveGame が全インスタンスフィールドを辿る〕＝(419) より前の state.bin と ForceUnlock=false の決定をこの行が上書きする）
        world.Explain = new ExplainLedger();   // ★(435) 説明台帳（App の InitAfterSimReady と同じ＝プレイヤー国だけ常設・本計算はビット恒等〔ExplainTests〕・★(436) 訂正：World.Explain はセーブに載る〔[NonSerialized] でない〕ので保存の前に外し〔下の Save の直前〕、再開のたびにここで付け直す）。正統性・国民意識・行政力・厭戦の内訳を報告に出す。⚠ForceUnlock の行は var world の直後に置く（番人 再征服の既定 が見る）

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
                $"=== {world.Year}Q{world.Quarter} ===\n" + string.Join("\n", applied) + "\n", Utf8NoBom);
            if (rawKept.Count > 0)
                File.AppendAllText(Path.Combine(dir, "decision.raw.txt"),
                    $"@{world.Year}Q{world.Quarter}\n" + string.Join("\n", rawKept) + "\n", Utf8NoBom);
            File.Move(decPath, Path.Combine(dir, "decision.applied.txt"), overwrite: true);
        }

        // ---- 3) 進める ----
        int logStart = sim.Diplomacy.Log.Count;
        double[]? ledBefore = null;   // ★(435) 直近の 1 四半期の収支＝最後の 1 四半期の直前の累計の写し（台帳は累計＝そのままでは四半期の値が読めなかった）
        for (int i = 0; i < steps; i++)
        {
            if (i == steps - 1) ledBefore = (double[])world.TreasuryLedger.Clone();
            sim.StepQuarter();
        }

        // ---- 4) 報告 ----
        var sb = new StringBuilder();
        WriteReport(sb, sim, playerIndex, applied, logStart, ledBefore);
        File.WriteAllText(Path.Combine(dir, "report.txt"), sb.ToString(), Utf8NoBom);
        Console.Write(sb.ToString());

        // ---- 5) 保存 ----
        world.Explain = null;   // ★(435) 説明台帳は保存に載せない（World.Explain は [NonSerialized] でなく、載せると state.bin が約 3KB 増え旧版と比べられない）＝再開のたびに上で付け直す。これで state.bin は台帳を付けない旧版とバイト一致
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
        world.ForceUnlock = true;   // ★(419) play と同じ（再生が play の軌道から外れないように）
        world.Explain = new ExplainLedger();   // ★(435) play と同じ（内訳の台帳・本計算はビット恒等）
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
        WriteReport(sb, sim, playerIndex, [$"（再生・適用 {appliedN} 手）"], 0, null);   // 再生は四半期ごとの収支を持たない（累計だけ）
        File.WriteAllText(Path.Combine(dir, "report.replay.txt"), sb.ToString(), Utf8NoBom);
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
            // ★(420) ForceUnlock は外した（App は 2026-08-24 にレバーを撤去・play は入口で常に true にする＝App と同じ口だけを残す）
            if (key is "ModernizationFrac" or "Mobilized")
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

    internal static void WriteReport(StringBuilder sb, MultiNationSim sim, int pi, List<string> applied, int logStart, double[]? ledBefore)
    {
        var w = sim.Nations[pi].World;
        var sp = w.SelfPower!;
        var coef = w.Coef;

        double pop = 0, urbanPop = 0, lit = 0, cons = 0, savings = 0;
        var byClass = new Dictionary<int, (double size, double lit, double sat, double mil, double sav)>();
        var classNames = new SortedDictionary<int, string>();   // ★(405) 階級の番号→名前の表（POP の ClassName の最初の非空）＝報告の階級を番号でなく名前で読める
        var milByClass = new Dictionary<int, double[]>();   // ★(410) 闘争度の目標の 5 本（Pop.MilLiving ほか）の人口加重和：階級ごと（画面の階級の表と同じ母集団＝階級の全 POP）
        var milMass = new double[5]; double massSize = 0;   // ★(410) 同・大衆（ヘッダーの闘争度と同じ母集団＝Politics.IsMassPop）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                if (p.ClassName.Length > 0 && !classNames.ContainsKey(p.ClassId)) classNames[p.ClassId] = p.ClassName;
                if (!milByClass.TryGetValue(p.ClassId, out var mc)) milByClass[p.ClassId] = mc = new double[5];
                double[] mp = [p.MilLiving, p.MilVoice, p.MilWar, p.MilNation, p.MilFamine];
                for (int k = 0; k < 5; k++) mc[k] += mp[k] * p.Size;
                if (Politics.IsMassPop(p)) { for (int k = 0; k < 5; k++) milMass[k] += mp[k] * p.Size; massSize += p.Size; }
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
        sb.AppendLine(PromptsLine(w, sp));   // ★(419) 待っている問いかけと使える動詞（1 行）
        // ★(435) 終局（暦が EndYear に入った＝1949 年 12 月が終わった）の最終の順位と自国の内訳＝画面の終局の表と同じ関数。画面と同じく止めはしない（以後も進められる）。
        if (PlayText.IsEnded(w)) AppendEnding(sb, sim, w);
        sb.AppendLine($"政体 {PlayText.PolityRegimeJa(PowerAffinity.PolityFromShares(w))}（画面の政体＝権力の取り分からの導出・内部の政体 {w.Polity}）　正統性 {w.Legitimacy:P0}　自由度 {w.Freedom:F2}　交戦 {(w.AtWarNow ? "★あり" : "なし")}");
        sb.AppendLine($"人口 {pop:N0}（都市 {urbanPop / N:P1}）　識字 {lit / N:P1}　生活水準 {NationalScore.MeanSatisfaction(w):F3}　闘争度 {Politics.AvgMassMilitancy(w):F3}　政治意識 {cons / N:F3}");   // ★2026-09-26（sweep-13）：生活水準と闘争度は画面・帯と同じ大衆の母集団（エンジンの1本）＝旧は全POPの別の数
        // ★(410) 00-HARN の残り：闘争度は 5 本の目標の和（クランプ前）へ速さ σ で追う在庫＝ヘッダーの闘争度と和は一致しない。画面の階級のホバーと同じ名前・同じ 0.5% 未満の省略。
        sb.AppendLine($"闘争度の目標の内訳（大衆・人口加重・和は在庫が追う先）： {MilParts(milMass, massSize)}");
        sb.AppendLine($"国庫 {w.Treasury:N0}　名目GDP {w.NominalGdpPerMonth:N0}　技術 {w.TechLevel:F2}（世界先端 {w.WorldTechLevel:F2}）　エリート {w.Elite:F3}　pop貯蓄計 {savings:N0}");
        // ★(435) 画面のヘッダー・財政タブにあって報告に無かった物（物価・通貨制度・為替・兌換・行政力・政治資本）。文言は画面と同じ関数（PlayText）。
        sb.AppendLine($"物価水準 {PlayText.PriceLevelText(w)}　為替 {PlayText.ExchangeRateText(w)}　通貨制度 現在 {w.MonetaryLaw}（兌換に加入 {(w.OnGoldStandard ? "○" : "×")}{(w.GoldStandardSuspendedInCrisis ? "・危機で兌換停止" : "")}） ／ 目標 {(MonetaryStandardLaw)Math.Clamp(w.Policy.MonetaryTarget, 0, 2)}　兌換カバー率 {PlayText.CoverText(w)}（維持線 {w.Coef.GoldCoverMin.Value:P0}）　資本移動 {(w.CapitalControlLaw ? "規制" : "自由")}");
        sb.AppendLine($"国債残高 {w.GovDebt.Value:N0}　外資信用 {w.CreditRating:P0}　政策金利 目標 {w.Policy.InterestRateTarget:P0}／実効 {w.InterestRate:P0}　貸出金利 {w.LoanRatePerYear.Value:P1}　行政力 {w.AdminEfficiency:P0}　政治資本 {PlayText.PoliticalCapitalText(w)}");

        sb.AppendLine();
        sb.AppendLine("---- 財政 ----");
        sb.AppendLine($"所得税 {w.TaxRate:P0}　人頭税 {w.PollTax:P0}　土地税 {w.LandTax:P0}　累進 {w.ProgressiveTax:P0}　専売の負担 {w.StateMonopolyBreadth:P0}（実効 {w.StateMonopolyBreadth * w.Coef.StateMonopolyMargin:P0}）");
        sb.AppendLine($"関税 {w.Tariff:P0}　教育費 {w.EducationSpending:P0}　行政費 {w.AdminSpending:P0}　社会保障 {w.SocialSecurity:P0}");
        // ★(410) 00-HARN の残り（プレイの報告 2-3）：教育費の目標は法と支配度を通った後の**実効**でしか国庫の購入にならない（Simulation の CompulsoryEduFunding・払い率を掛けた後の値）。
        //   目標を払っているつもりで実効 0 の国（Religious で教区学校への補助が無い・支配度が届かない）を報告で読めるようにする。
        {
            double effNum = 0, effPop = 0;
            foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; double rp = r.SumPopulation(); effNum += rp * r.CompulsoryEduFunding; effPop += rp; }
            double effNation = effPop > 1e-9 ? effNum / effPop : 0;
            sb.AppendLine($"実効の教育費（国庫が教育を買う率・法と支配度と払い率を通した後）： 国（人口加重） {effNation:P1}　首都州 {w.Region.CompulsoryEduFunding:P1}　目標 {w.EducationSpending:P0}　教育法 {w.EducationLaw}"
                + (w.EducationSpending > 0.005 && effNation < 0.0005 ? "　★目標を払っているつもりで実効 0" : ""));
        }
        sb.AppendLine($"★近代化投資 {w.ModernizationFrac:P0}（今四半期の予算 {w.ModernizationBudget():N0}）　総動員 {w.Mobilized}");
        sb.AppendLine($"陸軍維持費 {w.ArmyUpkeep:P0}　海軍維持費 {w.NavyUpkeep:P0}　植民地維持 {w.LastColonialUpkeepPerMonth:N0}");
        {   // ★(435) 置いた目標（現在値は上の行・税率が目標に追従しない件は現在と目標を並べて読む）
            var pt = w.Policy;
            sb.AppendLine($"目標（置いた値）： 所得税 {pt.IncomeTaxTarget:P0}　人頭税 {pt.PollTaxTarget:P0}　土地税 {pt.LandTaxTarget:P0}　累進 {pt.ProgressiveTaxTarget:P0}　関税 {pt.TariffTarget:P0}　教育費 {pt.EducationSpendingTarget:P0}　行政費 {pt.AdminSpendingTarget:P0}　社会保障 {pt.SocialSecurityTarget:P0}　陸軍維持費 {pt.ArmyUpkeepTarget:P0}　海軍維持費 {pt.NavyUpkeepTarget:P0}　近代化投資 {w.ModernizationFrac:P0}");
        }

        // ★(435) 収支の台帳は**開始からの累計**（区分ごと・収入が正・支出が負）。名乗りを付け、直近の 1 四半期の値を並べる（旧は累計を名乗らず、四半期の値は引き算で出すしかなかった）。
        var led = w.TreasuryLedger;
        string Cat(TreasuryCat c) => ledBefore is null ? $"{c}:{led[(int)c]:N0}" : $"{c}:{led[(int)c]:N0}（直近の四半期 {led[(int)c] - ledBefore[(int)c]:+#,0;-#,0;0}）";
        sb.AppendLine("収入（開始からの累計" + (ledBefore is null ? "のみ" : "・括弧は直近の 1 四半期") + "） " + string.Join(" ", IncomeCats.Where(c => Math.Abs(led[(int)c]) > 0.5 || (ledBefore is not null && Math.Abs(led[(int)c] - ledBefore[(int)c]) > 0.5)).Select(Cat)));
        sb.AppendLine("支出（開始からの累計" + (ledBefore is null ? "のみ" : "・括弧は直近の 1 四半期") + "） " + string.Join(" ", OutlayCats.Where(c => Math.Abs(led[(int)c]) > 0.5 || (ledBefore is not null && Math.Abs(led[(int)c] - ledBefore[(int)c]) > 0.5)).Select(Cat)));

        sb.AppendLine();
        sb.AppendLine("---- 法（現在値 ／ 目標値・現在と目標が違う法は ⇒ で目標を添える）----");
        var pol = w.Policy;
        foreach (var chunk in LawStatusRows(w).Chunk(4))
            sb.AppendLine(string.Join("　　", chunk.Select(r => r.Current == r.Target ? $"{r.Name} {r.Current}" : $"{r.Name} {r.Current} ⇒目標 {r.Target}")));

        sb.AppendLine();
        sb.AppendLine("---- 政治：各軸を動かす追い風／向かい風（正＝押せる・負＝抵抗）----");
        foreach (PolicyAxis ax in Enum.GetValues<PolicyAxis>())
        {
            double up = PolicyPreferences.NetSupport(w, ax, true);
            if (Math.Abs(up) < 1e-9) continue;
            var br = PolicyPreferences.SupportBreakdown(w, ax, true)
                        .Where(x => Math.Abs(x.Weight) > 1e-9)
                        .OrderByDescending(x => Math.Abs(x.Weight)).Take(4);
            sb.AppendLine($"  {ax,-18} 強化 {up,7:F3}　［{string.Join(" / ", br.Select(x => $"{x.Who}{x.Weight:+0.00;-0.00}"))}］　次の一歩 {GateText(w, ax)}");   // ★(410) 次の一歩の門＝法律タブのホバーと同じ文言（PlayText）
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
        sb.AppendLine("  階級の名前表： " + string.Join(" ／ ", classNames.Select(kv => $"{kv.Key}={kv.Value}")));
        foreach (var kv in byClass.OrderByDescending(x => x.Value.size).Take(14))
        {
            var (size, l, st, m2, sv) = kv.Value;
            double sz = Math.Max(1, size);
            sb.AppendLine($"  階級{kv.Key,2} {(classNames.TryGetValue(kv.Key, out var cn) ? cn : "？"),-8}  {size,12:N0}  識字{l / sz,5:P0}  水準{st / sz,6:F3}  闘争{m2 / sz,6:F3}  貯蓄{sv,12:N0}"
                + (milByClass.TryGetValue(kv.Key, out var mcls) ? $"　目標［{MilParts(mcls, size)}］" : ""));   // ★(410) 闘争度の目標の 5 本（画面のホバーと同じ名前）
        }

        // ★(435) 内訳（画面のホバー・POP タブ・政治タブと同じ関数。旧は数字だけで、正統性や闘争度が動く理由が追えなかった＝初見の試しのテストプレイの報告 §1-4）。
        sb.AppendLine();
        sb.AppendLine("---- 内訳（画面のホバーと同じ関数）----");
        AppendExplain(sb, w, "正統性", "Legitimacy", v => v.ToString("F3"), $"{w.Legitimacy:F3}");
        AppendExplain(sb, w, "国民意識", "NationalConsciousness", v => v.ToString("P1"), $"{w.NationalConsciousnessDisplay:P0}");
        AppendExplain(sb, w, "行政力", "AdminEfficiency", v => v.ToString("P0"), $"{w.AdminEfficiency:P0}（行政費 目標 {w.Policy.AdminSpendingTarget:P0}／実効 {w.AdminSpending:P0}）");
        if (w.AtWarNow) AppendExplain(sb, w, "厭戦", "Exhaustion", v => v.ToString("P0"), $"{sp.WarExhaustion:P0}");
        sb.AppendLine("  闘争度（階級の層ごとの平均）： " + string.Join(" ", PlayText.ClassLayerMeans(w, p => p.Militancy).Select(x => $"{x.Layer} {x.Mean:P0}")));
        sb.AppendLine("  識字（階級の層ごとの平均）： " + string.Join(" ", PlayText.ClassLayerMeans(w, p => p.Literacy).Select(x => $"{x.Layer} {x.Mean:P0}")));
        sb.AppendLine("  満足度（階級の層ごとの平均）： " + string.Join(" ", PlayText.ClassLayerMeans(w, p => p.Satisfaction).Select(x => $"{x.Layer} {x.Mean:P0}")));
        sb.AppendLine("  政治資本： " + PlayText.PoliticalCapitalTip(w).Replace("\n", "　"));
        sb.AppendLine("  国力の内訳（自国）： " + PlayText.ScoreBreakdownText(w, sim.Diplomacy).Replace("\n", "　"));

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

        // ★(419) 巨大建造物（自国が建設地の州を持つもの・生産タブの一覧と同じ集合）。着工は `!mega 名前`
        {
            var megas = sim.Diplomacy.Megaprojects.Where(m => ReferenceEquals(m.Owner, sp)).OrderBy(m => m.Name, StringComparer.Ordinal).ToList();
            if (megas.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("---- 巨大建造物（自国が建設地）→ !mega 名前 ----");
                foreach (var m in megas)
                {
                    var host = w.Regions.FirstOrDefault(r => r.State == RegionState.Active && r.Name == m.Host);
                    sb.AppendLine($"  {m.Name,-14} {m.Host,-10} {MegaStateText(w, m)}　見積 {(host is null ? 0 : Megaprojects.EstimateCost(host, m, coef)):N0}　着工{(Megaprojects.CanStart(w, m) ? "可" : "不可")}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("---- 世界（国力上位10）----");
        foreach (var n in sim.Nations.OrderByDescending(x => x.World.ScoreStanding).Take(10))
        {
            var xw = n.World;
            sb.AppendLine($"  {xw.SelfPower?.Name,-14} 国力{xw.ScoreStanding,8:F0} 工業{xw.MeanIndustrialization,6:F2} 近代化の信号{xw.ModernizeReturnShare(),5:F2} 技術{xw.TechLevel,5:F2} 人口{TotalPop(xw),12:N0}");
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

        // ★(435) !state・!subsidy ほかで使える財の名と州の名（旧は州名を報告の表の上位10だけから、財名を主食と建材だけから推測するしかなかった）。
        sb.AppendLine();
        sb.AppendLine("---- 動詞で使える名前（そのまま書く）----");
        sb.AppendLine("財： " + string.Join(" ", w.Region.Goods.OrderBy(g => g.Id).Select(g => g.Name)));
        sb.AppendLine("州（自国の稼働中）： " + string.Join(" ", w.Regions.Where(r => r.State == RegionState.Active).Select(r => r.Name)));
    }

    private static readonly TreasuryCat[] IncomeCats = [TreasuryCat.Tax, TreasuryCat.Tariff, TreasuryCat.StateMonopoly, TreasuryCat.Borrow, TreasuryCat.Devalue, TreasuryCat.StateFactory, TreasuryCat.TributeIn, TreasuryCat.OtherIn];
    private static readonly TreasuryCat[] OutlayCats = [TreasuryCat.Army, TreasuryCat.Navy, TreasuryCat.Admin, TreasuryCat.Education, TreasuryCat.Welfare, TreasuryCat.Pension, TreasuryCat.Modernization, TreasuryCat.AntiCorruption, TreasuryCat.Interest, TreasuryCat.DebtRepay, TreasuryCat.FactoryBuild, TreasuryCat.Subsidy, TreasuryCat.Megaproject, TreasuryCat.ColonialUpkeep, TreasuryCat.TributeOut, TreasuryCat.WarCost, TreasuryCat.OtherOut];

    /// <summary>説明台帳の内訳 1 本（画面の <c>ExplainTerms</c> と同じ <see cref="PlayText.ExplainTerms"/>・<see cref="PlayText.SignedLine"/>）。台帳が空（四半期を 1 つも進めていない）なら名乗って止める。</summary>
    private static void AppendExplain(StringBuilder sb, World w, string label, string quantity, Func<double, string> fmt, string head)
    {
        var terms = PlayText.ExplainTerms(w, quantity);
        sb.AppendLine($"  {label} {head}： " + (terms.Count == 0 ? "（台帳が空＝四半期を 1 つ進めると出る）" : string.Join(" ／ ", terms.Take(8).Select(t => PlayText.SignedLine(t.Delta, t.Reason, fmt)))));
    }

    /// <summary>★(435) 終局の最終の順位（画面の終局の表と同じ <see cref="PlayText.EndingStandings"/>）と自国の内訳。上位 10 と自国の行・全国数。</summary>
    private static void AppendEnding(StringBuilder sb, MultiNationSim sim, World w)
    {
        var rows = PlayText.EndingStandings(sim.Nations.Select(n => n.World), w);
        sb.AppendLine($"★終局： {PriceOfBread.SimCore.Calendar.EndYear}年 最終ランキング（全 {rows.Count} 国・自国 {rows.First(r => r.IsPlayer).Order} 位）　※画面と同じく止めはしない（以後も進められる）");
        sb.AppendLine("  順位 国名           国力 政体             人口        工業化度");
        foreach (var r in rows.Where(r => r.Order <= 10 || r.IsPlayer))
            sb.AppendLine($"  {r.Order,3}{(r.IsPlayer ? "★" : " ")} {r.Name,-12} {r.Score,6:F0} {PlayText.PolityRegimeJa(r.Polity),-8} {r.Population,12:N0} {r.Industry,8:F2}");
        sb.AppendLine("  自国の国力の内訳： " + PlayText.ScoreBreakdownText(w, sim.Diplomacy).Replace("\n", "　"));
    }

    /// <summary>★(435) 全ての法の現在値と目標値（旧は目標だけの法が多く、労組・女性・官僚・国有化・資本移動ほかの現在値が空欄だった）。現在値は World・目標値は NationPolicy の同じ名前の欄
    /// ＝画面の法律タブの行（<c>BuildLaws</c>）と同じ組。新しい法を NationPolicy に足したらここへ足す（番人 <c>PlayHarnessTests</c> が NationPolicy の法の欄が全てここに載るかを見る）。</summary>
    internal static List<(string Name, string Field, string Current, string Target)> LawStatusRows(World w)
    {
        var p = w.Policy;
        string Ob(bool on) => on ? "あり" : "なし";
        return
        [
            ("初等教育", nameof(NationPolicy.EducationLaw), w.EducationLaw.ToString(), p.EducationLaw.ToString()),
            ("参政権", nameof(NationPolicy.Suffrage), w.Suffrage.ToString(), p.Suffrage.ToString()),
            ("植民地参政権", nameof(NationPolicy.ColonialSuffrage), w.ColonialSuffrage.ToString(), p.ColonialSuffrage.ToString()),
            ("移民原住民参政権", nameof(NationPolicy.MigrantNativeSuffrage), w.MigrantNativeSuffrage.ToString(), p.MigrantNativeSuffrage.ToString()),
            ("報道", nameof(NationPolicy.Censorship), w.Censorship.ToString(), p.Censorship.ToString()),
            ("労組", nameof(NationPolicy.LaborUnion), w.LaborUnion.ToString(), p.LaborUnion.ToString()),
            ("労働時間", nameof(NationPolicy.WorkHours), w.WorkHours.ToString(), p.WorkHours.ToString()),
            ("児童労働の合法", nameof(NationPolicy.ChildLaborAllowed), Ob(w.ChildLaborAllowed), Ob(p.ChildLaborAllowed)),
            ("女性の権利", nameof(NationPolicy.WomenRights), w.WomenRights.ToString(), p.WomenRights.ToString()),
            ("穀物法", nameof(NationPolicy.CornLaws), w.CornLaws.ToString(), p.CornLaws.ToString()),
            ("土地改革", nameof(NationPolicy.LandReform), w.LandReform.ToString(), p.LandReform.ToString()),
            ("農奴解放", nameof(NationPolicy.SerfEmancipation), w.SerfEmancipation.ToString(), p.SerfEmancipation.ToString()),
            ("徴兵", nameof(NationPolicy.ConscriptionLaw), w.ConscriptionLaw.ToString(), p.ConscriptionLaw.ToString()),
            ("秘密投票", nameof(NationPolicy.SecretBallot), w.SecretBallot.ToString(), p.SecretBallot.ToString()),
            ("官僚", nameof(NationPolicy.Bureaucracy), w.Bureaucracy.ToString(), p.Bureaucracy.ToString()),
            ("移民制限", nameof(NationPolicy.ImmigrationRestriction), w.ImmigrationRestriction.ToString(), p.ImmigrationRestriction.ToString()),
            ("技術者流出禁止", nameof(NationPolicy.SpecialistExitLaw), w.SpecialistExitLaw.ToString(), p.SpecialistExitLaw.ToString()),
            ("社会保障法", nameof(NationPolicy.SocialSecurityLaw), w.SocialSecurityLaw.ToString(), p.SocialSecurityLaw.ToString()),
            ("国教", nameof(NationPolicy.StateReligion), w.StateReligion.ToString(), p.StateReligion.ToString()),
            ("独禁", nameof(NationPolicy.Antitrust), w.Antitrust.ToString(), p.Antitrust.ToString()),
            ("同業組合", nameof(NationPolicy.Guild), w.Guild.ToString(), p.Guild.ToString()),
            ("戦時配給", nameof(NationPolicy.WartimeRationing), Ob(w.WartimeRationing), Ob(p.WartimeRationing)),
            ("軍需輸出の禁止", nameof(NationPolicy.MilitaryExportBan), Ob(w.MilitaryExportBan), Ob(p.MilitaryExportBan)),
            ("民間の武器購入の禁止", nameof(NationPolicy.CivilianArmsBan), Ob(w.CivilianArmsBan), Ob(p.CivilianArmsBan)),
            ("鉄道国有化", nameof(NationPolicy.NationalizeRailways), Ob(w.NationalizeRailways), Ob(p.NationalizeRailways)),
            ("鉱山国有化", nameof(NationPolicy.NationalizeMines), Ob(w.NationalizeMines), Ob(p.NationalizeMines)),
            ("軍需産業国有化", nameof(NationPolicy.NationalizeMilitary), Ob(w.NationalizeMilitary), Ob(p.NationalizeMilitary)),
            ("石油国有化", nameof(NationPolicy.NationalizeOil), Ob(w.NationalizeOil), Ob(p.NationalizeOil)),
            ("電力国有化", nameof(NationPolicy.NationalizeElectricity), Ob(w.NationalizeElectricity), Ob(p.NationalizeElectricity)),
            ("全て国有化", nameof(NationPolicy.NationalizeAll), Ob(w.NationalizeAll), Ob(p.NationalizeAll)),
            ("アヘン保有", nameof(NationPolicy.OpiumPossession), w.OpiumPossession.ToString(), p.OpiumPossession.ToString()),
            ("アヘン輸出禁止", nameof(NationPolicy.OpiumExportBan), Ob(w.OpiumExportBan), Ob(p.OpiumExportBan)),
            ("通貨制度", nameof(NationPolicy.MonetaryTarget), w.MonetaryLaw.ToString(), ((MonetaryStandardLaw)Math.Clamp(p.MonetaryTarget, 0, 2)).ToString()),
            ("資本移動の規制", nameof(NationPolicy.CapitalControlTarget), Ob(w.CapitalControlLaw), Ob(p.CapitalControlTarget)),
        ];
    }

    /// <summary>プレイヤーの動作（World のメソッド＝UIのボタンと同じ口）。書式：
    /// !factory 州名 財名 ／ !sell 州名 財名 ／ !subsidy 州名 財名 率 ／
    /// !state 州名 財名 段 ／ !monopoly 財名 入|切|on|off|1|0|true|false ／ !bias 財名 -1|0|1 ／
    /// ★(405) 外交（UI の緊張・侵略・統一戦争・調停のボタンと同じ口）：!provoke 国名（緊張を上げる）／!ease 国名（緊張を下げる）／!invade 国名 [狙う植民地名] ／!unify 国名 ／!mediate 国名A 国名B</summary>
    internal static string RunVerb(World w, string line)
    {
        string[] a = line[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (a.Length == 0) return "⚠空の動作";
        // ★(435) 引数が足りない書式は添字の例外にせず書式を返す（旧は〈動作失敗…IndexOutOfRange〉）。
        if (VerbSyntaxOf(a[0]) is { } ar && a.Length < ar.MinWords) return $"⚠書式: {ar.Usage}";
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
        Power? PowerOf(string name) => w.Diplomacy?.Powers.Find(p => p.Name == name || p.BaseName == name);   // ★(405) 変名の国も旧名で引ける（UI の FindPower と同じ両受け）
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
                    if (ri < 0) return $"⚠州が無い: {a[1]}";
                    if (gi < 0) return $"⚠財が無い: {a[2]}";
                    // ★(435) 拒否の理由は本体の述語（CanSellFactory＝SellFactory と同じ 1 本）の理由をそのまま出す
                    var (sellOk, sellPrice, sellWhy) = w.CanSellFactory(ri, gi);
                    if (!sellOk) return $"⚠売却 {a[1]}/{a[2]} 不可（{(sellWhy.Length > 0 ? sellWhy : "国営の持分が無い／州が不正")}）";
                    return w.SellFactory(ri, gi) ? $"売却 {a[1]}/{a[2]}: 成立（買取価格 {sellPrice:N0}）" : $"⚠売却 {a[1]}/{a[2]} 不可（実行できなかった）";
                }
                case "subsidy":
                {
                    int ri = RegionOf(a[1]); int gi = GoodOf(a[2]);
                    if (ri < 0) return $"⚠州が無い: {a[1]}";
                    if (gi < 0) return $"⚠財が無い: {a[2]}";
                    var (ok, priv, why) = w.CanSubsidize(ri, gi);
                    if (!ok) return $"⚠補助金 {a[1]}/{a[2]} 不可：{(why.Length > 0 ? why : "産地なし")}";
                    if (!double.TryParse(a[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double rate)) return $"⚠率が読めない: {a[3]}（0〜1）";
                    w.SetSubsidyRate(ri, gi, rate);
                    double got = World.FindFactory(w.Regions[ri], gi)?.SubsidyRate.Value ?? 0;   // ★(435) 置いた率を読み戻す（0〜1 に丸められる）
                    return $"補助金 {a[1]}/{a[2]} = {got:P0}{(Math.Abs(got - rate) > 1e-9 ? $"（要求 {rate:P0} は 0〜1 に丸めた）" : "")}（民営持分 {priv:P0}）";
                }
                case "state":
                {
                    int ri = RegionOf(a[1]); int gi = GoodOf(a[2]);
                    if (ri < 0) return $"⚠州が無い: {a[1]}";
                    if (gi < 0) return $"⚠財が無い: {a[2]}";
                    if (!int.TryParse(a[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int t)) return $"⚠段が読めない: {a[3]}（整数）";
                    var pf = World.FindFactory(w.Regions[ri], gi);
                    if (pf is null) return $"⚠国有化目標 {a[1]}/{a[2]} 不可（この州に産地なし）";
                    int units = World.StateUnits(pf, w.Coef);
                    bool moved = w.SetStateTarget(ri, gi, t);
                    int placed = pf.StateTarget;   // ★(436) 本体が置いた目標を読み戻す（丸めの写しを Runner に持たない＝反証役 hw〔v〕中#1）
                    string round = placed != t ? $"（要求 {t} は 0〜{w.Coef.StateTargetMax} に丸めた）" : "";
                    if (moved) return $"国有化目標 {a[1]}/{a[2]} → {placed}{round}: 受理（今月 1 棟動いた・国営 {units}棟から）";
                    // ★(436) 発見の門が閉じた個体は本体が意志を取り下げる（World.StepStateTarget＝目標を現在の棟数へ戻す・毎月の再試行も無い）＝〈置いた〉と書かない
                    if (t > units && placed == units && units < w.Coef.StateTargetMax)
                        return $"⚠国有化目標 {a[1]}/{a[2]} → {t}: 目標は置けない＝取り下げた（{(w.CanBuildFactory(ri, gi) is { Ok: false } cw ? cw.Reason : "着工の門が閉じている")}）・国営 {units}棟のまま・再試行は無い";
                    // ★(435) 動けなかった月：旧は目標を置いたのに〈⚠不可〉と返し、取り消せないと読めた。目標（意志）は置かれ、毎月の再試行で動く＝理由は着工の門〔CanBuildFactory〕／払い下げの門〔CanSellFactory〕の本体の述語から出す
                    string why = placed > units ? (w.CanBuildFactory(ri, gi) is { Ok: false } cb ? cb.Reason : "着工の条件は満たしている（月次の再試行で動く）")
                               : placed < units ? (w.CanSellFactory(ri, gi) is { Ok: false } cs ? (cs.Reason.Length > 0 ? cs.Reason : "払い下げ不可") : "払い下げの条件は満たしている（月次の再試行で動く）")
                               : "国営の棟数と同じ（動かすものが無い）";
                    return $"⚠国有化目標 {a[1]}/{a[2]} → {placed}{round}: 目標は置いたが今月は動けない（{why}）・国営 {units}棟・毎月の再試行で動く";
                }
                case "monopoly":
                {
                    int gi = GoodOf(a[1]); if (gi < 0) return $"⚠財が無い: {a[1]}";
                    // ★2026-09-20：率は引数に取らない（材ごとの ON/OFF だけ）＝生産タブのチェックボックスと同じ1本を叩く。
                    // ★(405) 入/切の語を両方受け、どちらでもない語は黙って OFF にせず拒む（旧は 1／on／true 以外が全て OFF で、書式の `入` が切になった）。
                    if (a.Length < 3) return "⚠専売は `!monopoly 財名 入|切`";
                    bool on;
                    if (a[2] is "1" or "on" or "true" or "入") on = true;
                    else if (a[2] is "0" or "off" or "false" or "切") on = false;
                    else return $"⚠専売の指定が読めない: {a[2]}（入|切|on|off|1|0|true|false）";
                    bool moved = w.SetStateMonopolyOn(gi, on);
                    return $"専売 {a[1]} = {(on ? "入" : "切")}{(moved ? "" : "（変化なし＝既にその状態）")}";   // ★(435) 旧の〈政治が通さない〉は誤り（専売に政治の門は無い＝2026-09-20）
                }
                case "bias":
                {
                    int gi = GoodOf(a[1]); if (gi < 0) return $"⚠財が無い: {a[1]}";
                    if (!int.TryParse(a[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int b)) return $"⚠方針が読めない: {a[2]}（-1|0|1）";
                    w.SetTradeBias(gi, b);
                    int gotB = w.TradeBiasOf(gi);   // ★(435) 置いた値を読み戻す（-1〜1 に丸められる）
                    return $"売買方針 {a[1]} = {gotB}{(gotB != b ? $"（要求 {b} は -1〜1 に丸めた）" : "")}";
                }
                // ---- ★(405) 外交の動詞（00-HARN）：UI のボタンと同じ口を叩く。invade・unify・mediate は本体の述語が返す理由をそのまま出す（第二の基準を作らない）。
                //   provoke・ease の拒否の理由は本体の述語 Diplomacy.TensionActionRefusal（RaiseTension／EaseTension の門と同じ 1 本）が返す理由をそのまま出す（★(427) 旧は本体の条件の写しの文言＝(407) 反証役 bi〔v〕低#2）----
                case "provoke":
                case "ease":
                {
                    if (a.Length < 2) return $"⚠`!{a[0]} 国名`";
                    if (PowerOf(a[1]) is not { } t) return $"⚠国が無い: {a[1]}";
                    if (w.Diplomacy is null || w.SelfPower is null) return "⚠外交なし";
                    bool ok = a[0] == "provoke" ? w.Diplomacy.RaiseTension(w.SelfPower, t, w.Coef) : w.Diplomacy.EaseTension(w.SelfPower, t, w.Coef);
                    string verb = a[0] == "provoke" ? "挑発" : "緩和";
                    return ok ? $"{verb} {t.Name}: 緊張を{(a[0] == "provoke" ? "上げた" : "下げた")}（{w.Coef.TensionActionRampMonths}か月かけて反映・今 {w.Diplomacy.Tension(w.SelfPower, t):F1}）"
                              : $"⚠{verb} {t.Name} 不可（{w.Diplomacy.TensionActionRefusal(w.SelfPower, t, w.Coef) ?? "実行できなかった"}）";
                }
                case "invade":
                {
                    if (a.Length < 2) return "⚠`!invade 国名 [狙う植民地名]`";
                    if (PowerOf(a[1]) is not { } t) return $"⚠国が無い: {a[1]}";
                    Colony? aim = null;
                    if (a.Length > 2)
                    {
                        aim = t.ColonyList.FirstOrDefault(c2 => c2.Name == a[2]);
                        if (aim is null) return $"⚠{t.Name} に狙う植民地が無い: {a[2]}";
                    }
                    if (!w.CanInvade(t, out string why)) return $"⚠侵略 {t.Name} 不可（{why}）";
                    // ★(419) 屈服か開戦かは戻り値でなく**世界の状態**で読む：Diplomacy.Subjugate は屈服でも拒否〔開戦〕でも true を返す（拒否の枝も StartWar の後に return true）＝Invade の戻り値は〈実行した〉であって〈屈服した〉ではない。
                    //   旧は (407) 反証役 bi〔v〕中の読み〈false＝拒否＝開戦〉を採ったが、false は CanInvade が落ちたときだけ＝開戦の枝は一度も出なかった（(410) の煙の試験で全標的が〈屈服〉に見えた理由）。開戦は交戦の有無で決める。
                    if (!w.Invade(t, aim)) return $"⚠侵略 {t.Name} 不可（実行できなかった）";
                    return w.Diplomacy!.IsAtWar(w.SelfPower!, t)
                        ? $"侵略 {t.Name}: 拒否＝開戦（相手が屈服を拒んだ・余剰戦力と遠征の物資は払った）"
                        : $"侵略 {t.Name}: 屈服させた{(aim is not null ? $"（狙い先 {aim.Name}）" : "")}";
                }
                case "unify":
                {
                    if (a.Length < 2) return "⚠`!unify 国名`";
                    if (PowerOf(a[1]) is not { } t) return $"⚠国が無い: {a[1]}";
                    if (!w.CanUnificationWar(t, out string why)) return $"⚠統一戦争 {t.Name} 不可（{why}）";
                    return w.DeclareUnificationWarOn(t) ? $"統一戦争 {t.Name}: 宣戦" : $"⚠統一戦争 {t.Name} 不可";
                }
                case "mediate":
                {
                    if (a.Length < 3) return "⚠`!mediate 国名A 国名B`（交戦中の2国）";
                    if (PowerOf(a[1]) is not { } pa) return $"⚠国が無い: {a[1]}";
                    if (PowerOf(a[2]) is not { } pb) return $"⚠国が無い: {a[2]}";
                    if (w.Diplomacy is null || w.SelfPower is null) return "⚠外交なし";
                    var war = w.Diplomacy.Wars.FirstOrDefault(x => (x.A == pa && x.B == pb) || (x.A == pb && x.B == pa));
                    if (war is null) return $"⚠{pa.Name} と {pb.Name} の戦争が無い";
                    string? refusal = w.Diplomacy.PlayerOfferPeace(w.SelfPower, war, w.Coef);
                    return refusal is null ? $"調停 {pa.Name}/{pb.Name}: 提案した（結果は出来事の記録）" : $"⚠調停 {pa.Name}/{pb.Name} 不可（{refusal}）";
                }
                // ---- ★(419) 画面の問いかけに答える動詞（App の Popups と同じ口を叩く＝第二の基準を作らない）。待ちが無いときは何もしない ----
                case "ultimatum":   // App: WarAccept／WarDeclareGo（開戦は総動員のチェックを先に World.Mobilized へ書く）
                {
                    if (w.SelfPower is not { PendingWar: { } foe } me || w.Diplomacy is null) return "⚠最後通牒の待ちが無い";
                    if (a.Length < 2 || a[1] is not ("受諾" or "受け入れる" or "開戦")) return "⚠`!ultimatum 受諾|開戦 [総動員|総動員なし]`";
                    bool fight = a[1] == "開戦";
                    if (a.Length > 2)
                    {
                        if (!fight) return "⚠総動員の指定は開戦のときだけ（受諾では総動員に触れない）";
                        if (a[2] == "総動員") w.Mobilized = true; else if (a[2] == "総動員なし") w.Mobilized = false; else return $"⚠総動員の指定が読めない: {a[2]}（総動員|総動員なし）";
                    }
                    w.Diplomacy.ResolvePendingWar(me, fight, w.Coef);
                    return fight ? $"最後通牒 {foe.Name}: 開戦（総動員 {(w.Mobilized ? "入" : "切")}・結果は出来事の記録）" : $"最後通牒 {foe.Name}: 要求を受け入れた（緊張を下げ正統性を払う・結果は出来事の記録）";
                }
                case "callarms":   // App: CallToArmsAccept／CallToArmsRefuse
                {
                    if (w.SelfPower is not { PendingCallToArms: { } call } me || w.Diplomacy is null) return "⚠参戦要請の待ちが無い";
                    if (a.Length < 2 || a[1] is not ("受ける" or "断る")) return "⚠`!callarms 受ける|断る`";
                    bool join = a[1] == "受ける";
                    w.Diplomacy.ResolveCallToArms(me, join, w.Coef);
                    return join ? $"参戦要請 {call.Caller.Name}→{call.Enemy.Name}: 受けた（結果は出来事の記録）" : $"参戦要請 {call.Caller.Name}→{call.Enemy.Name}: 断った（{call.Caller.Name} との緊張が同盟破棄の水準へ・結果は出来事の記録）";
                }
                case "election":   // App: ElectionBack／ElectionAbstain（既定の注ぐ割合は App のスライダーの既定 0.2）
                {
                    if (w.PendingElection is not { } ev) return "⚠総選挙の公示の待ちが無い";
                    if (a.Length < 2) return "⚠`!election <番号|思想名の一部> [注ぐ割合=0.2]` または `!election 棄権`";
                    if (a[1] is "棄権" or "静観") { w.ResolveElection(-1, 0); return "総選挙: 棄権（どの思想にも加担しない）"; }
                    int slot = int.TryParse(a[1], NumberStyles.None, CultureInfo.InvariantCulture, out int sn) ? sn : Array.FindIndex(ev.AgendaLabels, l => l.Contains(a[1], StringComparison.Ordinal));
                    if (slot < 0 || slot >= ev.AgendaIdeologies.Length) return $"⚠公示に無い加担先: {a[1]}（{string.Join(" ／ ", ev.AgendaLabels.Select((l, i) => $"{i}={l}"))}）";
                    double lever = 0.2;
                    if (a.Length > 2 && !double.TryParse(a[2], NumberStyles.Float, CultureInfo.InvariantCulture, out lever)) return $"⚠注ぐ割合が読めない: {a[2]}";
                    double amount = w.ElectionSpendOf(lever), effect = w.ElectionPushEffectOf(lever);
                    int backed0 = w.ElectionsBacked;
                    w.ResolveElection(ev.AgendaIdeologies[slot], lever);
                    return w.ElectionsBacked > backed0
                        ? $"総選挙: {ev.AgendaLabels[slot]} に加担（注ぐ割合 {lever:P0}・資金 {amount:N0}・効き {effect:P0}）"
                        : $"⚠総選挙: {ev.AgendaLabels[slot]} への加担は不成立（余白を超える逆風・注ぐ額 0・効き 0 のいずれか＝金は払っていない）";
                }
                case "crisis":   // App: FactionClose は世界に何も書かない通知。答えは法の目標（キー=値）＝ここは内容の照会だけ
                {
                    if (w.PendingFactionCrisis is not { } fc) return "⚠国内の危機の待ちが無い";
                    return $"国内の危機: {Factions.DisplayName(fc)}（規模 {fc.Size:N0}千人・闘争度 {fc.AvgMilitancy:P0}）　要求: {w.PendingFactionCrisisDemands}　［通知のみ＝答えは同じ法の目標をキー=値で置く］";
                }
                case "target":   // App: StepFactoryTargetAll（▲▼＝分母 Planned を ±1・ここでは ▲|▼|+n|-n|=n）
                {
                    if (a.Length < 3) return "⚠`!target 財名 ▲|▼|+n|-n|=n`（全国の工場の目標＝分母）";
                    int gi = GoodOf(a[1]); if (gi < 0) return $"⚠財が無い: {a[1]}";
                    var c0 = w.FactoryCounter(gi);
                    int target;
                    if (a[2] == "▲") target = c0.Planned + 1;
                    else if (a[2] == "▼") target = c0.Planned - 1;
                    else if (a[2].StartsWith("=") && int.TryParse(a[2][1..], NumberStyles.None, CultureInfo.InvariantCulture, out int absT)) target = absT;
                    else if ((a[2].StartsWith("+") || a[2].StartsWith("-")) && int.TryParse(a[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int dT)) target = c0.Planned + dT;
                    else return $"⚠目標の指定が読めない: {a[2]}（▲|▼|+n|-n|=n）";
                    w.SetFactoryTarget(gi, target);
                    var c1 = w.FactoryCounter(gi);
                    return $"全国工場の目標 {a[1]}: 分母 {c0.Planned}→{c1.Planned}{(target != w.FactoryTargetOf(gi) ? $"（要求 {target} は 0〜{w.FactoryTargetMax} に丸めた）" : "")}（国営 {c1.State}・民営 {c1.Private}・建設予定 {c1.Pending}・上限 {w.FactoryTargetMax}）";
                }
                case "mega":   // App: StartMega → Megaprojects.TryStart
                {
                    if (a.Length < 2) return "⚠`!mega 建造物名`（自国が建設地を持つもの）";
                    var mp = w.Diplomacy?.Megaprojects.FirstOrDefault(m => m.Name == a[1] && ReferenceEquals(m.Owner, w.SelfPower));
                    if (mp is null) return $"⚠自国が建設地の建造物に無い: {a[1]}";
                    if (Megaprojects.StartRefusal(w, mp) is { } megaWhy) return $"⚠着工 {a[1]} 不可（{megaWhy}・{MegaStateText(w, mp)}）";   // ★(435) 理由は本体の述語（CanStart と同じ 1 本）
                    if (!Megaprojects.TryStart(w, mp)) return $"⚠着工 {a[1]} 不可（実行できなかった）";
                    return $"着工 {a[1]}（{MegaStateText(w, mp)}）";
                }
                default: return $"⚠不明な動作: {a[0]}";
            }
        }
        catch (Exception ex) { return $"⚠動作失敗 {line}（{ex.GetType().Name}: {ex.Message}）"; }
    }

    /// <summary>★(435) 動詞ごとの最少の語数（動詞を含む）と書式。足りないときは添字の例外でなく書式を返す。</summary>
    private readonly record struct VerbSyntax(int MinWords, string Usage);
    private static VerbSyntax? VerbSyntaxOf(string verb) => verb switch
    {
        "factory" => new VerbSyntax(MinWords: 3, Usage: "`!factory 州名 財名`"),
        "sell" => new VerbSyntax(MinWords: 3, Usage: "`!sell 州名 財名`"),
        "subsidy" => new VerbSyntax(MinWords: 4, Usage: "`!subsidy 州名 財名 率(0〜1)`"),
        "state" => new VerbSyntax(MinWords: 4, Usage: "`!state 州名 財名 棟数`"),
        "monopoly" => new VerbSyntax(MinWords: 2, Usage: "`!monopoly 財名 入|切`"),
        "bias" => new VerbSyntax(MinWords: 3, Usage: "`!bias 財名 -1|0|1`"),
        _ => null,
    };

    /// <summary>★(419) 待っている問いかけ（画面ならポップアップが出ている問い）と、それに答える動詞・使える動詞を 1 行で。待ちの有無は本体の状態（<c>PendingWar</c> ほか）をそのまま読む。</summary>
    internal static string PromptsLine(World w, Power sp)
    {
        var ps = new List<string>();
        // ★(435) 待ちは**答えるまで残る**（本体の状態：Diplomacy.PendingWar／PendingCallToArms は ResolvePendingWar／ResolveCallToArms か相手の除去でしか下りない。画面は待ちの間に時間を止めるが、play は進め続ける＝127 年残った実例）。
        //   古さは本体の状態から添える（最後通牒＝今の緊張と開戦の水準・参戦要請＝その戦争が今も続いているか）。
        if (sp.PendingWar is { } foe)
        {
            double tn = w.Diplomacy is { } dp ? dp.Tension(sp, foe) : 0;
            ps.Add($"最後通牒（{foe.Name}・今の緊張 {tn:F1}／開戦の水準 {w.Coef.WarDeclareTension:F0}{(tn < w.Coef.WarDeclareTension ? "＝緊張は水準を下回っている古い待ち" : "")}）→ !ultimatum 受諾 ／ !ultimatum 開戦 [総動員|総動員なし]");
        }
        if (sp.PendingCallToArms is { } ca)
        {
            bool ongoing = w.Diplomacy is { } dq && dq.IsAtWar(ca.Caller, ca.Enemy);
            ps.Add($"参戦要請（{ca.Caller.Name} が {ca.Enemy.Name} と交戦・その戦争は{(ongoing ? "続いている" : "既に終わっている＝古い待ち")}）→ !callarms 受ける ／ !callarms 断る");
        }
        if (w.PendingFactionCrisis is { } fc) ps.Add($"国内の危機（{Factions.DisplayName(fc)}・要求 {w.PendingFactionCrisisDemands}）→ 通知のみ＝同じ法の目標をキー=値で置く（!crisis で内容）");
        if (w.PendingElection is { } ev)
            ps.Add($"総選挙の公示→ !election <番号|思想名の一部> [注ぐ割合=0.2] ／ !election 棄権　［{string.Join(" ／ ", ev.AgendaLabels.Select((l, i) => $"{i}={l}"))}］（0.2 なら資金 {w.ElectionSpendOf(0.2):N0}・効き {w.ElectionPushEffectOf(0.2):P0}）");
        return $"待っている問いかけ： {(ps.Count == 0 ? "なし" : string.Join(" ； ", ps))}　使える動詞： !factory !sell !subsidy !state !monopoly !bias !target !mega !provoke !ease !invade !unify !mediate !ultimatum !callarms !election !crisis";
    }

    /// <summary>★(419) 巨大建造物の状態（データだけ＝着工の可否の理由を再導出しない。可否は <see cref="Megaprojects.CanStart"/> の 1 本）。</summary>
    private static string MegaStateText(World w, Megaproject mp)
        => $"{(mp.IsComplete ? "完成" : mp.Started ? $"建設中 {(mp.BuildUnits > 1e-9 ? Math.Clamp(mp.BuildProgress / mp.BuildUnits, 0, 1) : 0):P0}" : "未着工")}・技術 {w.TechLevel:F2}／必要 {mp.TechGate:F2}";

    /// <summary>★(410) 闘争度の目標の 5 本（人口加重の和 <paramref name="sum"/>÷<paramref name="size"/>）を画面のホバーと同じ名前・同じ 0.5% 未満の省略で並べる（多い順）。1 本も無ければ「なし」。</summary>
    private static string MilParts(double[] sum, double size)
    {
        double inv = size > 1e-9 ? 1.0 / size : 0;
        var shown = Enumerable.Range(0, 5).Select(k => (Name: PlayText.MilitancyPartNames[k], V: sum[k] * inv)).Where(x => x.V > 0.005).OrderByDescending(x => x.V).ToList();
        return shown.Count == 0 ? "なし" : string.Join(" ", shown.Select(x => $"{x.Name} {x.V:F3}"));
    }

    /// <summary>★(410) 政治の軸の次の一歩（強化の向き）の門を、法律タブのホバーの 2 行目と同じ文言で（<see cref="PlayText.NextStepLine"/>）。照会は副作用なし。</summary>
    internal static string GateText(World w, PolicyAxis ax)
    {
        try
        {
            if (Government.AtTopStage(w, ax)) return "最上段";   // ★(419) 上げる向きの次の段が無い軸は門を照会せず〈最上段〉（存在しない段への照会が出した嘘の理由を出さない）
            var (ok, cost, reason) = Government.GateInfo(w, ax, true, Government.NextStage(w, ax, true));
            return PlayText.NextStepLine(ok, cost, reason);
        }
        catch (Exception ex) { return $"⚠照会できず（{ex.GetType().Name}）"; }
    }

    private static double TotalPop(World w)
    {
        double t = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) t += r.SumPopulation();
        return t;
    }
}
