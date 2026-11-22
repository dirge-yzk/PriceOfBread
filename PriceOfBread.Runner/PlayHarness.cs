using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Runner;

/// <summary>外部の意思決定者（人間 or LLM）に1国を打たせるための対話ハーネス（2026-08-30・docs/20260830_AIにプレイさせる_2つの実験）。
///
/// ★ 使い方： dotnet PriceOfBread.Runner.dll play &lt;作業dir&gt; [国名=清] [進める四半期数=4] [seed=1789]
///   1) &lt;dir&gt;/state.bin が無ければ新規開始（★App と同じ手順＝プレイヤー国だけ NullAgent・IsPlayer・専売0・StartWarmup）
///   (419) 画面の問いかけ（最後通牒・参戦要請・国内の危機・総選挙の公示）に答える動詞は `!ultimatum`・`!callarms`・`!election`（`!crisis` は内容の照会のみ）。全国工場の目標は `!target`。待っている問いかけと使える動詞は報告の 2 行目。
///   (442) 6 番目の引数に `要約` を付けると標準出力は先頭 60 行＋前回の報告に無かった行だけ（report.txt は全文）。
///   (442) 画面が時間を止める待ちのうち World の状態から読めて答える動詞のある物（最後通牒・参戦要請・総選挙の公示）と国内の危機が立っていると、その四半期で進むのを止めて報告の先頭に待ちと答える動詞を書く（答えるまで進まない。国内の危機は通知＝この呼び出しの最初に立っていた札と別の札が立ったときだけ止める）。
///   (443) 画面が時間を止めるのに play が止めない物＝既知の違い：緊張のポップアップ（TensionRaise／TensionEase・通知済みの印は App の ViewModel の中だけ）・終戦の通知・内戦の陣営選択。
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
        world.Explain = new ExplainLedger();   // ★(435) 説明台帳（App の InitAfterSimReady と同じ＝プレイヤー国だけ常設・本計算はビット恒等〔ExplainTests〕・★(436) 訂正：World.Explain はセーブに載る〔[NonSerialized] でない〕ので保存の前に外し〔下の Save の直前〕、再開のたびにここで付け直す）。正統性・国民意識・行政力・厭戦の内訳を報告に出す。

        // ---- 2) 決定の適用（キー=値／NationPolicy のフィールド名）----
        string decPath = Path.Combine(dir, "decision.txt");
        var applied = new List<string>();
        bool? standingCall = null;   // ★(607) `!callarms 受ける|断る 全て`＝この呼び出しの間に立つ参戦要請へ同じ答えを置く（持ち越さない）
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
                if (PlayFacts.StandingCallArmsOf(line) is { } stand) standingCall = stand;
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
        // ★(442) 画面が時間を止める待ち（最後通牒・参戦要請・総選挙の公示・国内の危機）が立ったら、その四半期で進むのを止める（App は問いが開いている間 Playing=false）。
        //   答えは decision.txt に `!ultimatum`／`!callarms`／`!election` を書いて次の呼び出しで適用される（適用は上の 2) ＝進める前）。
        var period = new PlayPeriod();   // ★(450) 進めた期間の記録（景気の出来事・期間中の戦争＝報告が〈交戦なし〉〈GDP が名目だけ〉で出来事を見落とさないように）
        var (stepped, ledBefore) = StepUntilWait(sim, playerIndex, steps, period, null, standingCall is { } sc ? SkipCallToArms(dir, applied, sc) : null);

        // ---- 4) 報告 ----
        var sb = new StringBuilder();
        AppendStopNote(sb, world, world.SelfPower!, steps, stepped);   // ★(442) 先頭の行＝時間を止めたとき（待ちと答える動詞）
        WriteReport(sb, sim, playerIndex, applied, logStart, ledBefore, period);
        string reportPath = Path.Combine(dir, "report.txt");
        string prevReport = File.Exists(reportPath) ? File.ReadAllText(reportPath) : "";   // ★(442) 要約モードの比べる側（書き換える前に読む）
        File.WriteAllText(reportPath, sb.ToString(), Utf8NoBom);
        // ★(442) 6 番目の引数が `要約`（または `summary`）なら画面（標準出力）には先頭 SummaryHeadLines 行＋前回の報告に無かった行だけを出す。report.txt は常に全文。既定（引数なし）は従来どおり全文。
        Console.Write(args.Length > 5 && args[5] is "要約" or "summary" ? SummaryText(sb.ToString(), prevReport) : sb.ToString());

        // ---- 5) 保存 ----
        world.Explain = null;   // ★(435) 説明台帳は保存に載せない（World.Explain は [NonSerialized] でなく、載せると state.bin が約 3KB 増え旧版と比べられない）＝再開のたびに上で付け直す。これで state.bin は台帳を付けない旧版とバイト一致
        using (var fs = File.Create(statePath)) SaveGame.Save(sim, fs);
        return 0;
    }

    /// <summary>★(607) 進める途中で立った参戦要請に答える（`!callarms 受ける|断る 全て` の指定・<see cref="StepUntilWait"/> が立つたびに呼ぶ）。答えは適用した決定の行と decision.log.txt・decision.raw.txt（再生用の四半期スタンプつき）に残す。</summary>
    internal static Func<World, string> SkipCallToArms(string dir, List<string> applied, bool join)
    {
        return world =>
        {
            string verbLine = join ? "!callarms 受ける" : "!callarms 断る";
            string result = RunVerb(world, verbLine);
            applied.Add(result);
            File.AppendAllText(Path.Combine(dir, "decision.log.txt"), $"=== {world.Year}Q{world.Quarter} ===\n{result}\n", Utf8NoBom);
            File.AppendAllText(Path.Combine(dir, "decision.raw.txt"), $"@{world.Year}Q{world.Quarter}\n{verbLine}\n", Utf8NoBom);
            return result;
        };
    }

    /// <summary>★(442) 要約モードの標準出力の行数（先頭から）。</summary>
    internal const int SummaryHeadLines = 60;
    /// <summary>★(442) 報告の要約＝先頭 <see cref="SummaryHeadLines"/> 行＋<paramref name="previous"/>（前回の報告）に同じ行が無い新しい行（順は報告の順・先頭で出した行は繰り返さない）。前回が空なら全文。
    /// 表示の道具＝report.txt は全文のまま・世界に触れない。</summary>
    internal static string SummaryText(string current, string previous)
    {
        if (previous.Length == 0) return current;
        var cur = current.Replace("\r\n", "\n").Split('\n');
        var old = new HashSet<string>(previous.Replace("\r\n", "\n").Split('\n'), StringComparer.Ordinal);
        var sb = new StringBuilder();
        int head = Math.Min(SummaryHeadLines, cur.Length);
        for (int i = 0; i < head; i++) sb.AppendLine(cur[i]);
        int changed = 0, skipped = cur.Length - head;
        var diff = new StringBuilder();
        for (int i = head; i < cur.Length; i++)
            if (cur[i].Length > 0 && !old.Contains(cur[i])) { diff.AppendLine(cur[i]); changed++; }
        sb.AppendLine($"---- 要約：先頭 {head} 行のあと、前回の報告に無かった行 {changed} 行（残り {skipped} 行のうち・全文は report.txt）----");
        sb.Append(diff);
        return sb.ToString();
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
    internal static string ApplyKeyValue(World world, string line)   // ★(450) 番人が呼ぶ（internal）
    {
        int eq = line.IndexOf('=');
        if (eq <= 0) return $"⚠書式不正: {line}";
        string key = line[..eq].Trim(), val = line[(eq + 1)..].Trim();
        try
        {
            // ⚠World 直下のプレイヤーレバー（NationPolicy に無い。UIはスライダー/ボタンで持つ）
            if (key is "ModernizationFrac" or "Mobilized")
            {
                var wf = typeof(World).GetProperty(key, BindingFlags.Public | BindingFlags.Instance)!;
                object oldv = wf.GetValue(world)!;
                object nv = wf.PropertyType == typeof(bool) ? bool.Parse(val) : double.Parse(val, CultureInfo.InvariantCulture);
                wf.SetValue(world, nv);
                return $"{key}: {oldv} → {nv}　（★World直下のレバー）";
            }
            // ★(450) 読む線 (441) 問い3 §2-21：画面が操作させるのは軍事費の**合計**の 1 本（段ボタン＝MilitarySpendingTarget）だけ。陸海の比は毎四半期の頭に AI と同じ関数（SetNavalUpkeepShare）が置き直す
            //   （作者 2026-09-23）＝陸海を別々に置くと〈置いていない目標が動く〉。陸海の 2 本は拒み、合計は NationPolicy のプロパティ（比を保って按分）で置く（★(451) 拒む形は AI の判断・作者の承認は未確認＝作者の裁定は海軍の比率のレバーの撤去まで）。
            if (key is "ArmyUpkeepTarget" or "NavyUpkeepTarget")
                return $"⚠{key} は置けない：画面が操作させるのは軍事費の合計の 1 本 `MilitarySpendingTarget=0〜1`（段ボタンは 0.25／0.5／0.75／1）。陸海の比は毎四半期の頭に AI と同じ関数が置き直す（作者 2026-09-23）";
            if (key == "MilitarySpendingTarget")
            {
                double oldTotal = world.Policy.MilitarySpendingTarget, askedTotal = double.Parse(val, CultureInfo.InvariantCulture);
                world.Policy.MilitarySpendingTarget = askedTotal;
                double gotTotal = world.Policy.MilitarySpendingTarget;
                return $"{key}: {oldTotal} → {gotTotal}{(Math.Abs(gotTotal - askedTotal) > 1e-9 ? $"（要求 {val} は 0〜1 に丸めた）" : "")}　（軍事費の合計・陸海の比は次の四半期の頭に AI と同じ関数が置き直す）";
            }
            // ★(501) 段0：報告の法の行と同じ画面の名前と段の名前でも置ける（例 `労働組合=容認`・`阿片=全面禁止`）。本体の法の表（LawCatalog）の 1 本＝画面の段ボタンと同じ書き込み先。NationPolicy の欄名（下の反射）も今までどおり受ける。
            if (LawCatalog.All.FirstOrDefault(l => l.Name == key) is { } law)
            {
                int stage = Array.IndexOf(law.Stages, val);
                if (stage < 0) return $"⚠段が読めない: {val}（{key} の段：{string.Join("／", law.Stages)}）";
                string before = law.StageName(law.Target(world));
                law.SetTarget(world.Policy, law.LevelBase + stage);
                return $"{key}: {before} → {law.StageName(law.Target(world))}";
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

    internal static void WriteReport(StringBuilder sb, MultiNationSim sim, int pi, List<string> applied, int logStart, double[]? ledBefore, PlayPeriod? period = null)
    {
        var w = sim.Nations[pi].World;
        var sp = w.SelfPower!;
        var coef = w.Coef;

        double pop = 0, urbanPop = 0, lit = 0, cons = 0, savings = 0;
        var byClass = new Dictionary<int, (double size, double lit, double sat, double mil, double sav)>();
        var classNames = new SortedDictionary<int, string>();   // ★(405) 階級の番号→名前の表（POP の ClassName の最初の非空）＝報告の階級を番号でなく名前で読める
        var milByClass = new Dictionary<int, double[]>();   // ★(410) 闘争度の目標の 4 本（Pop.MilVoice ほか・★(551) 生活の欄は削除）の人口加重和：階級ごと（画面の階級の表と同じ母集団＝階級の全 POP）
        var milMass = new double[4]; double massSize = 0;   // ★(410) 同・大衆（ヘッダーの闘争度と同じ母集団＝Politics.IsMassPop）
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Pops)
            {
                if (p.ClassName.Length > 0 && !classNames.ContainsKey(p.ClassId)) classNames[p.ClassId] = p.ClassName;
                if (!milByClass.TryGetValue(p.ClassId, out var mc)) milByClass[p.ClassId] = mc = new double[4];
                double[] mp = [p.MilVoice, p.MilWar, p.MilNation, p.MilFamine];
                for (int k = 0; k < 4; k++) mc[k] += mp[k] * p.Size;
                if (Politics.IsMassPop(p)) { for (int k = 0; k < 4; k++) milMass[k] += mp[k] * p.Size; massSize += p.Size; }
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
        // ★(410) 00-HARN の残り：闘争度は 4 本の目標の和（クランプ前）へ速さ σ で追う在庫＝ヘッダーの闘争度と和は一致しない。画面の階級のホバーと同じ名前・同じ 0.5% 未満の省略。
        sb.AppendLine($"闘争度の目標の内訳（大衆・人口加重・和は在庫が追う先）： {MilParts(milMass, massSize)}");
        sb.AppendLine($"国庫 {w.Treasury:N0}　名目GDP {w.NominalGdpPerMonth:N0}　エリート {w.Elite:F3}　pop貯蓄計 {savings:N0}");
        AppendEconomy(sb, w, period);   // ★(450) §2-5：名目だけでなく実質・1 人あたりの落ち込みと画面と同じ景気の判定
        // ★(435) 画面のヘッダー・財政タブにあって報告に無かった物（物価・通貨制度・為替・兌換・行政力・政治資本）。文言は画面と同じ関数（PlayText）。
        sb.AppendLine($"物価水準 {PlayText.PriceLevelText(w)}　為替 {PlayText.ExchangeRateText(w)}　通貨制度 現在 {w.MonetaryLaw}（兌換に加入 {(w.OnGoldStandard ? "○" : "×")}{(w.GoldStandardSuspendedInCrisis ? "・危機で兌換停止" : "")}） ／ 目標 {(MonetaryStandardLaw)Math.Clamp(w.Policy.MonetaryTarget, 0, 2)}　兌換カバー率 {PlayText.CoverText(w)}（維持線 {w.Coef.GoldCoverMin.Value:P0}）　資本移動 {(w.CapitalControlLaw ? "規制" : "自由")}");
        sb.AppendLine($"国債残高 {w.GovDebt.Value:N0}　外資信用 {w.CreditRating:P0}　政策金利 目標 {w.Policy.InterestRateTarget:P0}／実効 {w.InterestRate:P0}　貸出金利 {w.LoanRatePerYear.Value:P1}　行政力 {w.AdminEfficiency:P0}　政治資本 {PlayText.PoliticalCapitalText(w)}");

        AppendPeriod(sb, sim, w, sp, period);   // ★(450) 期間中の景気の出来事と戦争
        sb.AppendLine();
        sb.AppendLine("---- 財政 ----");
        sb.AppendLine($"所得税 {w.TaxRate:P0}　人頭税 {w.PollTax:P0}　土地税 {w.LandTax:P0}　累進 {w.ProgressiveTax:P0}　専売の負担 {w.StateMonopolyBreadth:P0}（実効 {w.StateMonopolyBreadth * w.Coef.StateMonopolyMargin:P0}）");
        sb.AppendLine($"関税 {w.Tariff:P0}　{PlayText.EducationSpendingText(w)}　行政費 {w.AdminSpending:P0}　社会保障 {w.SocialSecurity:P0}");   // ★(452) 教育費の名乗りは App と同じ関数
        // ★(410) 00-HARN の残り（プレイの報告 2-3）：教育費の目標は法と支配度を通った後の**実効**でしか国庫の購入にならない（Simulation の CompulsoryEduFunding・払い率を掛けた後の値）。
        //   目標を払っているつもりで実効 0 の国（Religious で教区学校への補助が無い・支配度が届かない）を報告で読めるようにする。
        {
            double effNum = 0, effPop = 0;
            foreach (var r in w.Regions) { double rp = r.SumPopulation(); effNum += rp * r.CompulsoryEduFunding; effPop += rp; }
            double effNation = effPop > 1e-9 ? effNum / effPop : 0;
            sb.AppendLine($"実効の教育費（国庫が教育を買う率・法と支配度と払い率を通した後）： 国（人口加重） {effNation:P1}　首都州 {w.Region.CompulsoryEduFunding:P1}　目標 {w.EducationSpending:P0}　教育法 {w.EducationLaw}"
                + (w.EducationSpending > 0.005 && effNation < 0.0005 ? "　★目標を払っているつもりで実効 0" : ""));
        }
        sb.AppendLine($"★近代化投資 {w.ModernizationFrac:P0}（今四半期の予算 {w.ModernizationBudget():N0}）　総動員 {w.Mobilized}");
        // ★(450) §2-21：画面が操作させるのは軍事費の**合計**の 1 本（段ボタン＝MilitarySpendingTarget 0.25／0.5／0.75／1）だけ。陸海の比は毎四半期 AI と同じ関数が決める（作者 2026-09-23）＝置いても次の四半期に書き換わる。
        sb.AppendLine($"軍事費（合計＝操作できる唯一の 1 本 MilitarySpendingTarget）： 目標 {w.Policy.MilitarySpendingTarget:P0}　実効 {w.MilitarySpending.Value:P0}　うち陸 {w.ArmyUpkeep:P0}・海 {w.NavyUpkeep:P0}（陸海の比は毎四半期 AI と同じ関数が決める＝置けない）　植民地維持 {w.LastColonialUpkeepPerMonth:N0}");
        {   // ★(435) 置いた目標（現在値は上の行・税率が目標に追従しない件は現在と目標を並べて読む）
            var pt = w.Policy;
            sb.AppendLine($"目標（置いた値）： 所得税 {pt.IncomeTaxTarget:P0}　人頭税 {pt.PollTaxTarget:P0}　土地税 {pt.LandTaxTarget:P0}　累進 {pt.ProgressiveTaxTarget:P0}　関税 {pt.TariffTarget:P0}　教育費 {pt.EducationSpendingTarget:P0}　行政費 {pt.AdminSpendingTarget:P0}　社会保障 {pt.SocialSecurityTarget:P0}　軍事費（合計） {pt.MilitarySpendingTarget:P0}　近代化投資 {w.ModernizationFrac:P0}");
        }

        // ★(435) 収支の台帳は**開始からの累計**（区分ごと・収入が正・支出が負）。名乗りを付け、直近の 1 四半期の値を並べる（旧は累計を名乗らず、四半期の値は引き算で出すしかなかった）。
        var led = w.TreasuryLedger;
        string Cat(TreasuryCat c) => ledBefore is null ? $"{CatLabel(c)}:{led[(int)c]:N0}" : $"{CatLabel(c)}:{led[(int)c]:N0}（直近の四半期 {led[(int)c] - ledBefore[(int)c]:+#,0;-#,0;0}）";
        // ★(450) §2-19：戦費（WarCost）は賠償の受取と払いの**正味**＝賠償の受け手では累計が正になる（旧は支出の行に正の値が出て〈累計のはずの数字が符号を変える〉と読めた）。累計が正なら収入の行に置く。
        bool warNetIn = led[(int)TreasuryCat.WarCost] > 0.5;
        var incomeCats = warNetIn ? IncomeCats.Append(TreasuryCat.WarCost).ToArray() : IncomeCats;
        var outlayCats = warNetIn ? OutlayCats.Where(c => c != TreasuryCat.WarCost).ToArray() : OutlayCats;
        sb.AppendLine("収入（開始からの累計" + (ledBefore is null ? "のみ" : "・括弧は直近の 1 四半期") + "） " + string.Join(" ", incomeCats.Where(c => Math.Abs(led[(int)c]) > 0.5 || (ledBefore is not null && Math.Abs(led[(int)c] - ledBefore[(int)c]) > 0.5)).Select(Cat)));
        sb.AppendLine("支出（開始からの累計" + (ledBefore is null ? "のみ" : "・括弧は直近の 1 四半期") + "） " + string.Join(" ", outlayCats.Where(c => Math.Abs(led[(int)c]) > 0.5 || (ledBefore is not null && Math.Abs(led[(int)c] - ledBefore[(int)c]) > 0.5)).Select(Cat)));
        sb.AppendLine("  （正味）の区分は入りと出の差し引き＝StateFactory は国営工場の収益から赤字の肩代わりを引いた値（国営工場の売却代金・国営持分の取り分も同じ区分に入る）・WarCost は賠償の受取から戦費を引いた値。累計が減ることも符号が変わることもある（累計が正の WarCost は収入の行に置く）。");

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
        var bun = EconomyStep.BundleOf(cap, coef);   // ★(568) 今の建設の資材の組（エンジンと同じ関数＝旗 BuildingGoodBundle が正なら建造物＋機械・0 なら旧の鋼の束）
        int[] watch = new[] { bun.G0, bun.G1 }.Concat(bun.Slots == 4 ? new[] { bun.G2, bun.G3 } : [])
                      .Concat([Catalog.CoalId, coef.OpiumGoodId, Catalog.IronId, Catalog.WheatId, Catalog.TimberId]).ToArray();   // 鉄31・機械部品51・麦15・木11
        sb.AppendLine("財         価格   参照  価格/参照  在庫    需要   生産   輸入");
        foreach (int gi in watch.Distinct())
        {
            if (gi < 0 || gi >= cap.Markets.Length) continue;
            var g = cap.GoodOf(gi); if (g is null) continue;
            var m = cap.Markets[gi];
            sb.AppendLine($"{g.Name,-8} {m.Price,7:F3} {g.RefPrice,6:F3} {(g.RefPrice > 1e-9 ? m.Price / g.RefPrice : 0),8:F2} {m.Stock,7:F0} {m.LastDemand,7:F0} {m.LastProduced,6:F0} {m.LastImport,6:F0}");
        }
        double bundleRef = EconomyStep.BundleRef(coef);
        double bundleNow = bun.Cost(cap);
        sb.AppendLine($"建設1単位の束： 参照 {bundleRef * cap.PriceLevel:F2} ／ いまの市価 {bundleNow:F2}　（比 {(bundleRef * cap.PriceLevel > 1e-9 ? bundleNow / (bundleRef * cap.PriceLevel) : 0):F2}）");
        sb.AppendLine($"★建設可能率 {World.ConstructionAfford(w):F3}　（着工に要る床 {coef.FactoryStartAffordFloor:F2}／半減点は束の比 {coef.FactoryBuildPriceCeil:F1} 倍）");
        sb.AppendLine("主要財の産地（州：規模・産出）：");
        foreach (int gi in new[] { bun.G0, bun.G1, Catalog.IronId }.Distinct())
        {
            var g0 = cap.GoodOf(gi); if (g0 is null) continue;
            var sites = new List<string>();
            foreach (var r in w.Regions)
            {
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
                + (milByClass.TryGetValue(kv.Key, out var mcls) ? $"　目標［{MilParts(mcls, size)}］" : ""));   // ★(410) 闘争度の目標の 4 本（画面のホバーと同じ名前）
        }

        // ★(435) 内訳（画面のホバー・POP タブ・政治タブと同じ関数。旧は数字だけで、正統性や闘争度が動く理由が追えなかった＝初見の試しのテストプレイの報告 §1-4）。
        sb.AppendLine();
        sb.AppendLine("---- 内訳（画面のホバーと同じ関数）----");
        AppendExplain(sb, w, "正統性", "Legitimacy", v => v.ToString("F3"), $"{w.Legitimacy:F3}");
        AppendExplain(sb, w, "行政力", "AdminEfficiency", v => v.ToString("P0"), $"{w.AdminEfficiency:P0}（行政費 目標 {w.Policy.AdminSpendingTarget:P0}／実効 {w.AdminSpending:P0}）");
        if (w.AtWarNow) AppendExplain(sb, w, "厭戦", "Exhaustion", v => v.ToString("P0"), $"{sp.WarExhaustion:P0}");
        sb.AppendLine("  闘争度（階級の層ごとの平均）： " + string.Join(" ", PlayText.ClassLayerMeans(w, p => p.Militancy).Select(x => $"{x.Layer} {x.Mean:P0}")));
        sb.AppendLine("  識字（階級の層ごとの平均）： " + string.Join(" ", PlayText.ClassLayerMeans(w, p => p.Literacy).Select(x => $"{x.Layer} {x.Mean:P0}")));
        sb.AppendLine("  満足度（階級の層ごとの平均）： " + string.Join(" ", PlayText.ClassLayerMeans(w, p => p.Satisfaction).Select(x => $"{x.Layer} {x.Mean:P0}")));
        sb.AppendLine("  政治資本： " + PlayText.PoliticalCapitalTip(w).Replace("\n", "　"));
        sb.AppendLine("  国力の内訳（自国）： " + PlayText.ScoreBreakdownText(w).Replace("\n", "　"));

        sb.AppendLine();
        sb.AppendLine("---- 州（上位10・人口順）----");
        foreach (var r in w.Regions
                                    .OrderByDescending(x => x.SumPopulation()).Take(10))
            sb.AppendLine($"  {r.Name,-14} 人口{r.SumPopulation(),11:N0} 港{(r.IsPort ? "○" : "－")} 資本{(r.CapitalEnabled ? "○" : "×")}");

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
            sb.AppendLine($"  {xw.SelfPower?.Name,-14} 国力{xw.ScoreStanding,8:F1} 近代化の信号{xw.ModernizeReturnShare(),5:F2} 人口{TotalPop(xw),12:N0}");
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

        PlayFacts.Append(sb, w, sp);   // ★(607) 待っている問いの中身・工場（財ごと）・失業（数と状態だけ）

        // ★(435) !state・!subsidy ほかで使える財の名と州の名（旧は州名を報告の表の上位10だけから、財名を主食と建材だけから推測するしかなかった）。
        sb.AppendLine();
        sb.AppendLine("---- 動詞で使える名前（そのまま書く）----");
        sb.AppendLine("財： " + string.Join(" ", w.Region.Goods.OrderBy(g => g.Id).Select(g => g.Name)));
        sb.AppendLine("州（自国の稼働中）： " + string.Join(" ", w.Regions.Select(r => r.Name)));
    }

    /// <summary>★(450) §2-19：台帳の区分の名乗り。入りと出の差し引き（正味）の区分は〈正味〉と添える（<see cref="TreasuryCat.StateFactory"/>＝国営工場の収益−赤字の肩代わり・<see cref="TreasuryCat.WarCost"/>＝賠償の受取−戦費）。</summary>
    private static string CatLabel(TreasuryCat c) => c is TreasuryCat.StateFactory or TreasuryCat.WarCost ? $"{c}（正味）" : c.ToString();

    private static readonly TreasuryCat[] IncomeCats = FiscalCatalog.Cats(FiscalSide.Income).ToArray();   // ★(501) 台帳の区分の表は本体の FiscalCatalog（並びは同じ）
    private static readonly TreasuryCat[] OutlayCats = FiscalCatalog.Cats(FiscalSide.Outlay).ToArray();

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
        sb.AppendLine("  順位 国名           国力 政体             人口        鉄の産出");
        foreach (var r in rows.Where(r => r.Order <= 10 || r.IsPlayer))
            sb.AppendLine($"  {r.Order,3}{(r.IsPlayer ? "★" : " ")} {r.Name,-12} {r.Score,6:F0} {PlayText.PolityRegimeJa(r.Polity),-8} {r.Population,12:N0} {r.IronShare,8:P1}");
        sb.AppendLine("  自国の国力の内訳： " + PlayText.ScoreBreakdownText(w).Replace("\n", "　"));
    }

    /// <summary>★(435) 全ての法の現在値と目標値（旧は目標だけの法が多く、労組・女性・官僚・国有化・資本移動ほかの現在値が空欄だった）。
    /// ★(501) 段0：法の表は本体の <see cref="LawCatalog"/> が 1 本で持つ＝名前も段の名前も画面の法律タブと同じ日本語（旧は自前の名前と英語の型の名前で、画面と 5 つ食い違っていた）。
    /// 新しい法を NationPolicy に足したら本体の法の表へ足す（番人 <c>PlayHarnessTests</c> が NationPolicy の法の欄が全て表に載るかを見る）。
    /// <c>Field</c>＝報告の書き込みの口（decision.txt の <c>キー=値</c>）が受ける NationPolicy の欄名（主）／<c>Fields</c>＝その行が読み書きする欄の全て（阿片は 2 欄）。</summary>
    internal static List<(string Name, string Field, string[] Fields, string Current, string Target)> LawStatusRows(World w)
        => LawCatalog.All.Select(l => { var (name, cur, tgt) = LawCatalog.StatusRow(l, w); return (name, l.Key, l.Fields, cur, tgt); }).ToList();

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
                    bool all = a.Length == 3 && a[2] == "全て";   // ★(607) 全て＝この呼び出しの間に立つ要請へ同じ答え（Run が置く）
                    if (a.Length > 3 || (a.Length == 3 && !all)) return "⚠`!callarms 受ける|断る [全て]`";
                    if (w.SelfPower is not { PendingCallToArms: { } call } me || w.Diplomacy is null)
                        return all && a[1] is ("受ける" or "断る") ? $"参戦要請（全て）: {a[1]}・今は待ちなし" : "⚠参戦要請の待ちが無い";
                    if (a.Length < 2 || a[1] is not ("受ける" or "断る")) return "⚠`!callarms 受ける|断る [全て]`";
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
                case "answer":   // ★(607) 1 行で全ての待ちに答える（既存の動詞の行へ展開して順に適用する）
                {
                    var (lines, err) = PlayFacts.ExpandAnswer(a);
                    if (lines is null) return err!;
                    return "回答: " + string.Join(" ／ ", lines.Select(l => RunVerb(w, l)));
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

    /// <summary>★(442)(443) play が止める待ちの種類＝App の <c>PopupPause</c> を呼ぶポップアップのうち、World の状態から読めて play に答える動詞がある物（＋通知の国内の危機）。
    /// 止めない物（既知の違い）：緊張のポップアップ（TensionRaise／TensionEase＝!provoke／!ease は有るが、通知済みの印 <c>_tensionPrompted</c> は App の ViewModel の中だけ）・終戦の通知・内戦の陣営選択。</summary>
    internal enum PlayWait { Ultimatum, CallToArms, Crisis, Election }

    /// <summary>★(419)(442) 待っている問いかけ（画面ならポップアップが出ている問い）とその説明（答える動詞つき）。待ちの有無は本体の状態（<c>PendingWar</c> ほか）をそのまま読む＝報告の 1 行と時間を止める判定が同じ 1 本を使う。</summary>
    internal static List<(PlayWait Kind, string Text)> PendingItems(World w, Power sp)
    {
        var ps = new List<(PlayWait, string)>();
        // ★(435) 待ちは**答えるまで残る**（本体の状態：Diplomacy.PendingWar／PendingCallToArms は ResolvePendingWar／ResolveCallToArms か相手の除去でしか下りない。127 年残った実例）。
        //   ★(442) play は待ちが立つと進むのを止める（<see cref="StepUntilWait"/>）。古さは本体の状態から添える（最後通牒＝今の緊張と開戦の水準・参戦要請＝その戦争が今も続いているか）。
        if (sp.PendingWar is { } foe)
        {
            double tn = w.Diplomacy is { } dp ? dp.Tension(sp, foe) : 0;
            ps.Add((PlayWait.Ultimatum, $"最後通牒（{foe.Name}・今の緊張 {tn:F1}／開戦の水準 {w.Coef.WarDeclareTension:F0}{(tn < w.Coef.WarDeclareTension ? "＝緊張は水準を下回っている古い待ち" : "")}）→ !ultimatum 受諾 ／ !ultimatum 開戦 [総動員|総動員なし]"));
        }
        if (sp.PendingCallToArms is { } ca)
        {
            bool ongoing = w.Diplomacy is { } dq && dq.IsAtWar(ca.Caller, ca.Enemy);
            ps.Add((PlayWait.CallToArms, $"参戦要請（{ca.Caller.Name} が {ca.Enemy.Name} と交戦・その戦争は{(ongoing ? "続いている" : "既に終わっている＝古い待ち")}）→ !callarms 受ける ／ !callarms 断る"));
        }
        if (w.PendingFactionCrisis is { } fc) ps.Add((PlayWait.Crisis, $"国内の危機（{Factions.DisplayName(fc)}・要求 {w.PendingFactionCrisisDemands}）→ 通知のみ＝同じ法の目標をキー=値で置く（!crisis で内容）"));
        if (w.PendingElection is { } ev)
            ps.Add((PlayWait.Election, $"総選挙の公示→ !election <番号|思想名の一部> [注ぐ割合=0.2] ／ !election 棄権　［{string.Join(" ／ ", ev.AgendaLabels.Select((l, i) => $"{i}={l}"))}］（0.2 なら資金 {w.ElectionSpendOf(0.2):N0}・効き {w.ElectionPushEffectOf(0.2):P0}）"));
        return ps;
    }

    /// <summary>★(419) 待っている問いかけと、それに答える動詞・使える動詞を 1 行で。</summary>
    internal static string PromptsLine(World w, Power sp)
    {
        var ps = PendingItems(w, sp);
        return $"待っている問いかけ： {(ps.Count == 0 ? "なし" : string.Join(" ； ", ps.Select(p => p.Text)))}　使える動詞： !factory !sell !subsidy !state !monopoly !bias !target !provoke !ease !invade !unify !mediate !ultimatum !callarms !election !answer !crisis";
    }

    /// <summary>★(442) 進めてはいけない待ち（画面なら問いのポップアップが開いていて時間が止まっている物）。<see cref="PendingItems"/> のうち、
    /// 最後通牒・参戦要請・総選挙の公示は答えるまで止める（画面は答えるまで閉じられない）。国内の危機は**通知**（答える動詞が無く、画面は閉じれば時間が戻る・同じ運動の札が立っている間は 1 度だけ出す＝App の <c>_crisisShown</c>）
    /// ＝<paramref name="crisisSeen"/>（この呼び出しの最初に立っていた札・または既に止めた札）と**別の札**が立ったときだけ止める。</summary>
    internal static List<(PlayWait Kind, string Text)> BlockingWaits(World w, Power sp, FactionState? crisisSeen)
        => PendingItems(w, sp).Where(p => p.Kind != PlayWait.Crisis || !ReferenceEquals(w.PendingFactionCrisis, crisisSeen)).ToList();

    /// <summary>★(442) 最大 <paramref name="steps"/> 四半期を進める。待ちが立っているなら**その四半期で止める**（立っているものは進める前に確かめる＝前の呼び出しで立って答えられていない待ちも止める）。
    /// 返すのは進めた四半期の数と、最後に進めた 1 四半期の直前の収支の台帳の写し（一度も進めなければ null）。</summary>
    internal static (int Stepped, double[]? LedBefore) StepUntilWait(MultiNationSim sim, int playerIndex, int steps, PlayPeriod? period = null, Action<int>? afterStep = null, Func<World, string>? answerCallToArms = null)
    {
        var world = sim.Nations[playerIndex].World;
        var sp = world.SelfPower!;
        FactionState? crisisSeen = world.PendingFactionCrisis;   // 最初に立っている危機の札は前の報告で見せた物＝止めない
        double[]? ledBefore = null;   // ★(435) 直近の 1 四半期の収支＝最後の 1 四半期の直前の累計の写し（台帳は累計＝そのままでは四半期の値が読めなかった）
        int stepped = 0;
        period?.Begin(world);   // ★(450) 進める前の景気の判定と終戦の記録の写し
        for (int i = 0; i < steps; i++)
        {
            if (world.PendingFactionCrisis is null) crisisSeen = null;   // 札が下りたら次の札は新しい
            if (answerCallToArms is not null && sp.PendingCallToArms is not null) answerCallToArms(world);   // ★(607) 参戦要請へ置いた答え（他の待ちは従来どおり止める）
            if (BlockingWaits(world, sp, crisisSeen).Count > 0) break;
            ledBefore = (double[])world.TreasuryLedger.Clone();
            sim.StepQuarter();
            stepped++;
            period?.AfterStep(world);
            afterStep?.Invoke(stepped);   // ★(450) 番人の差し込み口（四半期の途中で新しい危機の札が立つ盤面を再現する・本番の呼び手は渡さない）
        }
        if (answerCallToArms is not null && sp.PendingCallToArms is not null) answerCallToArms(world);   // ★(607) 最後の 1 四半期で立った要請にも同じ答え
        return (stepped, ledBefore);
    }

    /// <summary>★(450) play が 1 回の呼び出しで進めた期間の記録（読むだけ・世界に新しい状態を足さない＝呼び出しの中だけで持つ）。景気の出来事は画面と同じ関数（★(451) 前の判定は呼び出しの頭で〈入りの水準だけ〉から置き直す＝画面の _prevDepression は四半期をまたいで持つので、落ち込みが回復と入りの間で呼び出しをまたぐと語が画面と違いうる）
    /// （<see cref="SelfPlayMatch.InDepression"/>・初期値は画面が国を読み込んだときと同じ〈入りの水準〉だけで判定）で入りと回復の四半期を拾う。戦争は進める前の終戦の記録の写しとの差で拾う
    /// （報告の〈交戦〉は呼び出しの終わりの瞬間の状態だけで、宗主としての自動参戦などで期間中に起きて終わった戦争を見落とした＝読む線 (441) 問い3 §2-19）。</summary>
    internal sealed class PlayPeriod
    {
        public int StartYear, StartQuarter, Stepped;
        /// <summary>景気後退の最中か（直近の判定）。</summary>
        public bool InDepression;
        /// <summary>期間中の最大の落ち込み（1 人あたり・ピーク比）。</summary>
        public double WorstDrawdown;
        public readonly List<string> EconomyEvents = [];
        internal HashSet<EndedWarRecord> EndedBefore = [];
        public List<string> WarsAtStart = [];

        internal void Begin(World w)
        {
            StartYear = w.Year; StartQuarter = w.Quarter;
            double dd = w.GdpPcDrawdown.Value;
            InDepression = SelfPlayMatch.InDepression(false, dd);
            WorstDrawdown = dd;
            if (w.Diplomacy is { } d && w.SelfPower is { } sp) { EndedBefore = new HashSet<EndedWarRecord>(d.EndedWars); WarsAtStart = FoesAtWar(d, sp); }
        }

        internal void AfterStep(World w)
        {
            Stepped++;
            double dd = w.GdpPcDrawdown.Value;
            if (dd > WorstDrawdown) WorstDrawdown = dd;
            bool now = SelfPlayMatch.InDepression(InDepression, dd);
            if (now && !InDepression) EconomyEvents.Add($"{w.Year}年 Q{w.Quarter} {SelfPlayMatch.DepressionWord(dd)}に入った（1 人あたり実質の下落 {dd:P0}）");
            else if (!now && InDepression) EconomyEvents.Add($"{w.Year}年 Q{w.Quarter} 景気が回復した（下落 {dd:P0}）");
            InDepression = now;
        }

        internal static List<string> FoesAtWar(Diplomacy d, Power sp) => d.Powers.Where(o => !ReferenceEquals(o, sp) && d.IsAtWar(sp, o)).Select(o => o.Name).ToList();
    }

    /// <summary>★(450) §2-5：実質（参照価格建て）・1 人あたりの落ち込みと、画面と同じ景気の判定（<see cref="SelfPlayMatch.InDepression"/>）。名目 GDP は物価を含む＝景気の判定に使わない
    /// （旧の報告は名目 GDP だけで、実質の落ち込みと画面が出す景気後退・恐慌の出来事が読めなかった＝本体の記録に恐慌は 0 件・画面だけが作る）。
    /// 期間の記録が無い呼び出し（再生・番人）は前の判定を持たない＝画面が国を読み込んだときと同じ〈入りの水準だけ〉で判定する。</summary>
    private static void AppendEconomy(StringBuilder sb, World w, PlayPeriod? period)
    {
        double dd = w.GdpPcDrawdown.Value;
        bool inDep = period?.InDepression ?? SelfPlayMatch.InDepression(false, dd);
        sb.AppendLine($"実質GDP（参照価格建て・月） {w.GdpPerMonth.Value:N0}（平滑 {w.GdpTrend:N0}・ピーク {w.GdpPeak:N0}＝総額の落ち込み {w.GdpDrawdown.Value:P1}）　1 人あたり実質（平滑） {w.GdpPcTrend:G4}／ピーク {w.GdpPcPeak:G4}＝ピーク比の落ち込み {dd:P1}"
            + $"　景気の判定（画面と同じ関数・1 人あたり・前の判定は呼び出しの頭から）： {(inDep ? SelfPlayMatch.DepressionWord(dd) : "平常")}（入り {SelfPlayMatch.DepressionEnterDrawdown:P0} 超・回復 {SelfPlayMatch.DepressionExitDrawdown:P0} 以下・恐慌 {SelfPlayMatch.GreatDepressionDrawdown:P0} 超）　※名目GDP は物価を含む＝景気の判定には使わない");
    }

    /// <summary>★(450) この呼び出しで進めた期間の景気の出来事と戦争（期間の記録があるときだけ）。戦争は進める前の終戦の記録との差＋開始時と今の交戦相手。
    /// 終戦の記録（<see cref="EndedWarRecord"/>）は戦勝点が A 側視点・結果は記録の判定（<see cref="EndedWarRecord.VerdictOf"/>＝講和が使った 1 本・語は <see cref="PlayText.WarResultWord"/>）。開戦の年は終戦の年−経過月から引いた近似。</summary>
    private static void AppendPeriod(StringBuilder sb, MultiNationSim sim, World w, Power sp, PlayPeriod? period)
    {
        if (period is null) return;
        var dip = sim.Diplomacy;
        sb.AppendLine();
        sb.AppendLine($"---- この呼び出しで進めた期間（{period.StartYear}年 Q{period.StartQuarter} から {period.Stepped} 四半期・今 {w.Year}年 Q{w.Quarter}）----");
        sb.AppendLine("景気の出来事（画面と同じ関数・前の判定は呼び出しの頭から）： " + (period.EconomyEvents.Count == 0 ? $"なし（期間中の最大の落ち込み {period.WorstDrawdown:P0}）" : string.Join(" ／ ", period.EconomyEvents) + $"（期間中の最大の落ち込み {period.WorstDrawdown:P0}）"));
        string NameOf(long id) => dip.Powers.FirstOrDefault(o => o.Id == id)?.Name ?? $"（国 {id}・消滅）";
        var ended = dip.EndedWars.Where(r => !period.EndedBefore.Contains(r) && (r.AId == sp.Id || r.BId == sp.Id)).ToList();
        var now = PlayPeriod.FoesAtWar(dip, sp);
        sb.AppendLine($"戦争： 開始時に交戦中 {(period.WarsAtStart.Count == 0 ? "なし" : string.Join("・", period.WarsAtStart))} ／ 今交戦中 {(now.Count == 0 ? "なし" : string.Join("・", now))} ／ 期間中に終わった戦争 {ended.Count} 件");
        foreach (var r in ended)
        {
            bool isA = r.AId == sp.Id;
            double mine = isA ? r.Score : -r.Score;   // 自国側から見た戦勝点
            string result = PlayText.WarResultWord(r.WonBy(sp.Id));   // ★(456) 画面と同じ語・同じ判定（EndedWarRecord.VerdictOf＝講和が使った判定そのもの）
            sb.AppendLine($"  {NameOf(isA ? r.BId : r.AId)}と {r.EndYear - r.Months / 12}年ごろ〜{r.EndYear}年（{r.Months}か月）　{result}（自国側の戦勝点 {mine:+0.00;-0.00}）　戦死 自国 {(isA ? r.DeadA : r.DeadB):N0}・相手 {(isA ? r.DeadB : r.DeadA):N0}");
        }
    }

    /// <summary>★(442) 時間を止めた（進めていない四半期が残っている・または進めた先で待ちが立った）ことと、待ちと答える動詞を報告の**先頭**に書く。止めていなければ何も書かない。</summary>
    internal static void AppendStopNote(StringBuilder sb, World w, Power sp, int requested, int stepped)
    {
        var blocked = BlockingWaits(w, sp, null).Where(p => p.Kind != PlayWait.Crisis).Select(p => p.Text).ToList();
        if (stepped < requested)   // 途中で止めた（待ちが立っている・国内の危機の新しい札を含む）
        {
            var all = PendingItems(w, sp).Select(p => p.Text).ToList();
            sb.AppendLine($"⏸ 時間を止めた：要求 {requested} 四半期のうち {stepped} 四半期だけ進めた（{w.Year}年 Q{w.Quarter}）。待っている問い： {string.Join(" ； ", all)}");
            sb.AppendLine(blocked.Count > 0
                ? "   答えを decision.txt に書いて次の呼び出しで進める（答えるまで時間は進まない）。"
                : "   国内の危機の新しい札で止めた＝通知（答える動詞は無い）。次の呼び出しはそのまま進む。");   // ★(443) 危機だけで止めたとき
        }
        else if (blocked.Count > 0)   // 最後の 1 四半期で待ちが立った（次の呼び出しは答えを適用してから進む）
            sb.AppendLine($"⏸ 次は答えるまで進めない：{string.Join(" ； ", blocked)}");
    }

    /// <summary>★(410) 闘争度の目標の 4 本（人口加重の和 <paramref name="sum"/>÷<paramref name="size"/>）を画面のホバーと同じ名前・同じ 0.5% 未満の省略で並べる（多い順）。1 本も無ければ「なし」。</summary>
    private static string MilParts(double[] sum, double size)
    {
        double inv = size > 1e-9 ? 1.0 / size : 0;
        var shown = Enumerable.Range(0, 4).Select(k => (Name: PlayText.MilitancyPartNames[k], V: sum[k] * inv)).Where(x => x.V > 0.005).OrderByDescending(x => x.V).ToList();
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
        foreach (var r in w.Regions) t += r.SumPopulation();
        return t;
    }
}
