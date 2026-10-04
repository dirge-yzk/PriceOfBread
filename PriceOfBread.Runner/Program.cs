using PriceOfBread.SimCore;

// 全パース/整形を不変カルチャに固定：ロケール依存の小数区切り（de-DE等の「,」）で double.Parse が例外/誤読になるのを防ぎ、chronicle/selfplay 出力の桁区切りを一定化＝バイト再現性を保つ。
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

// 引数の数値パースは投げない：タイプミスや不正入力で診断CLIが生スタックトレースで落ちず既定へフォールバック（chronicle/integration 等の TryParse コマンドと挙動統一）。
static int ArgInt(string[] a, int i, int def) => a.Length > i && int.TryParse(a[i], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
static ulong ArgUlong(string[] a, int i, ulong def) => a.Length > i && ulong.TryParse(a[i], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
static double? ArgDouble(string[] a, int i, double? def) => a.Length > i && double.TryParse(a[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;

// 貿易/GDPの物差し（正典＝SelfPlay.cs MajorsTradeGdp と同式）：分子＝(輸出+輸入)を清算/年で年換算、分母＝NominalGdp（月次）を12倍で年換算。
// 周期を必ず年で揃える＝分子×4・分母そのまま、のような単位不一致（3〜12倍の過大表示）を計器間で作らない。
static double TradeGdp(World w)
{
    double ex = w.ExportsByGood is { } eg ? eg.Sum() : 0;
    double im = w.ImportsByGood is { } ig ? ig.Sum() : 0;
    return (ex + im) * w.Coef.ClearingsPerYear / System.Math.Max(1.0, w.NominalGdp * Calendar.MonthsPerYear);
}

// 失業率＝エンジンが pop に書いた値（EconomyStep：労働財の売れ残り率・農業労働は自給分を除く）を、その労働財の生産者の LaborPopIndex から読む
// （旧＝Runner が LastSold/LastProduced から再導出＝閾値も農業の自給控除も別式だった・2026-09-11 #13）。労働市場の無い州は0。
static double UnemploymentOf(Region r, int laborGood)
{
    foreach (var s in r.Producers) if (s.GoodId == laborGood && s.LaborPopIndex >= 0 && s.LaborPopIndex < r.Pops.Count) return r.Pops[s.LaborPopIndex].UnemploymentRate;
    return 0;
}

// 全角混在の列揃え（CJK≈2桁換算の簡易パディング）：史実忠実度テーブルの可読性用。
static string JpPad(string s, int width)
{
    int w = 0; foreach (var ch in s) w += ch > 0x2E7F ? 2 : 1;
    return s + new string(' ', System.Math.Max(0, width - w));
}

// 政体名の和訳（chronicle/explain/integration が共有する1本＝重複定義をしない）。
static string PolJa(Polity p) => p switch
{
    Polity.Absolutist => "絶対君主制", Polity.Despotic => "専制君主制", Polity.ConstitutionalMonarchy => "立憲君主制",
    Polity.Republic => "共和制", Polity.Shogunate => "幕府", Polity.BourgeoisDictatorship => "ブルジョア独裁",
    Polity.ProletarianDictatorship => "プロレタリア独裁", Polity.MilitaryDictatorship => "軍事独裁", Polity.Anarchy => "無政府", _ => p.ToString(),
};

// === 史実忠実度テーブルの表示（新較正基準2026-07-09＝各国が約7割のシードで史実（または近い）1914年政体・主要法に到達） ===
// selfplay-batch の集計と fidelity コマンドの両方から呼ぶ（統合＋単独表示）。
static void PrintFidelity(BatchSummary b)
{
    var fid = b.Fidelity ?? [];
    System.Console.WriteLine($"\n史実忠実度（1914参照表・完走{b.N - b.Breakdowns}試合。判定帯：◎7割帯=near55-85% / △境界=50-55,85-90% / 固着>90%=カオス不足 / 逸脱過多<50%）:");
    System.Console.WriteLine($"  {JpPad("国名", 24)}{JpPad("首都", 16)} 史実%  近い%  法一致%     判定");
    foreach (var f in fid)
    {
        string law = f.LawAxes > 0 ? $"{f.LawMatch,5:P0}({f.LawAxes}軸)" : "  —      ";
        System.Console.WriteLine($"  {JpPad(f.Nation, 24)}{JpPad(f.Capital, 16)} {f.Exact,5:P0}  {f.Near,5:P0}  {law}  {f.Verdict}");
    }
    if (fid.Count > 0)
    {
        double meanExact = fid.Average(f => f.Exact), meanNear = fid.Average(f => f.Near);
        var lawFid = fid.Where(f => f.LawAxes > 0).ToList();
        double meanLaw = lawFid.Count > 0 ? lawFid.Average(f => f.LawMatch) : 0;
        int band = fid.Count(f => f.Verdict == "◎7割帯"), edge = fid.Count(f => f.Verdict == "△境界");
        var stuck = fid.Where(f => f.Verdict == "固着").Select(f => f.Nation).ToList();
        var stray = fid.Where(f => f.Verdict == "逸脱過多").Select(f => f.Nation).ToList();
        System.Console.WriteLine($"  平均: 史実(exact) {meanExact:P0} / 近い(near) {meanNear:P0}（目標≈70%）/ 法一致 {meanLaw:P0}（{lawFid.Count}カ国で定義）");
        System.Console.WriteLine($"  判定: ◎7割帯 {band} / △境界 {edge} / 固着 {stuck.Count} / 逸脱過多 {stray.Count}（全{fid.Count}カ国）");
        // B-中7（史実検証2026-07-22）：人口軸は**独立の1行**（exact/nearへ合算しない＝過去測定との比較可能性を守る）。
        // 1783年比の成長倍率＝領土不変な定義（本ゲームは国を消さないので絶対人口の±%は噛み合わない）。帯が未定義の国はNaNで除外。
        var popFid = fid.Where(f => !double.IsNaN(f.PopMatch)).ToList();
        if (popFid.Count > 0)
            System.Console.WriteLine($"  人口: 成長倍率の一致 {popFid.Average(f => f.PopMatch):P0}（合格 {popFid.Sum(f => f.PopMatch):F1}/{popFid.Count}カ国）　" +
                string.Join("　", popFid.Select(f => $"{f.Nation}×{f.PopGrowthMean:F2}")));
        // 列強軸（2026-07-27）：人口軸と同じく**独立の1行**（exact/nearへ合算しない）。1914年にスコア上位7へ入っていた率。
        // 史実の列強は英仏独露墺伊米日の8カ国＝定員7に対し1つ多いので**理論上限は 7/8＝88%**（100%を目指さない）。
        // 追加の動機＝実測 seed1 で1783年の列強7のうち1914年も列強なのは仏だけ（英49位・工業化0.00／露53位＝最下位）だったが、
        // 「誰が列強か」は政体軸・法軸・人口軸のいずれでも測られておらず、この歪みがどの帯にも露出していなかった。
        var gpFid = fid.Where(f => !double.IsNaN(f.GreatMatch)).ToList();
        if (gpFid.Count > 0)
            System.Console.WriteLine($"  列強: 1914の顔ぶれ一致 {gpFid.Average(f => f.GreatMatch):P0}（{gpFid.Count}カ国で定義・理論上限88%＝定員7に対し史実8）　" +
                string.Join("　", gpFid.Select(f => $"{f.Nation}{f.RankMean:F0}位")));
        // ★2026-09-03（キューA-11／出典 トリアージ 10-8b）：段1の3次元は P0 の丸めだけを出しており、
        //   『何カ国中いくつ合格したか』が出力から復元できなかった（メモリ「バッチ比較はヘッダーから」の規律が働く前提を壊していた）。
        //   合格件数（一致率の総和＝シード平均の和・小数1桁）を併記する＝印字のみ・力学非接触。
        // 08_バランス調整方針（2026-07-31）＝70%計器の6次元化。経済規模・領土（本国/植民地）も**独立の行**で出す（exact/nearへ合算しない）。
        // 各次元の目標＝厳格一致50%（＝体感7割）。★初回実走では「緩≈厳×1.4〜1.5」を確認するため、次元ごとの厳格率をそのまま並べる。
        // ★E-08 多断面（2026-09-05）：1820／1870 の断面を併記（主指標は1913のまま）。定義のある国だけの平均。
        // ★1950断面（2026-09-21）：政体の exact と near（従属込み）を併記＝国別は「exact/near」。near の無い断面の行は従来と同じ印字。
        foreach (var (label, selEx, selNr, selEc) in new (string, Func<PriceOfBread.SimCore.NationFidelity, double>, Func<PriceOfBread.SimCore.NationFidelity, double>, Func<PriceOfBread.SimCore.NationFidelity, double>)[]
                 { ("1820", f => f.Exact1820, f => double.NaN, f => f.Econ1820), ("1870", f => f.Exact1870, f => double.NaN, f => f.Econ1870),
                   ("1920", f => f.Exact1920, f => double.NaN, f => double.NaN), ("1950", f => f.Exact1950, f => f.Near1950, f => double.NaN) })
        {
            var ex = fid.Where(f => !double.IsNaN(selEx(f))).ToList();
            var ec = fid.Where(f => !double.IsNaN(selEc(f))).ToList();
            var nr = fid.Where(f => !double.IsNaN(selNr(f))).ToList();
            if (ex.Count > 0 || ec.Count > 0)
                System.Console.WriteLine($"  断面{label}年: 政体exact {(ex.Count > 0 ? ex.Average(selEx) : double.NaN):P0}（{ex.Count}カ国）{(nr.Count > 0 ? $"　近い {nr.Average(selNr):P0}" : "")}　経済規模 {(ec.Count > 0 ? ec.Average(selEc) : double.NaN):P0}（{ec.Count}カ国）　"
                    + string.Join("　", ex.Select(f => $"{f.Nation}{selEx(f):P0}" + (double.IsNaN(selNr(f)) ? "" : $"/{selNr(f):P0}"))));
        }
        var econFid = fid.Where(f => !double.IsNaN(f.EconMatch)).ToList();
        if (econFid.Count > 0)
            System.Console.WriteLine($"  経済規模: GDP世界順位が史実±{HistoricalReference.GdpRankTolerance}位 {econFid.Average(f => f.EconMatch):P0}（合格 {econFid.Sum(f => f.EconMatch):F1}/{econFid.Count}カ国・目標{HistoricalReference.DimensionTargetStrict:P0}）　" +
                string.Join("　", econFid.Select(f => $"{f.Nation}{f.GdpRankMean:F0}位")));
        var homeFid = fid.Where(f => !double.IsNaN(f.HomeMatch)).ToList();
        if (homeFid.Count > 0)
            System.Console.WriteLine($"  領土（本国）: 史実州の{HistoricalReference.HomeProvinceMatchFrac:P0}以上を保有 {homeFid.Average(f => f.HomeMatch):P0}（合格 {homeFid.Sum(f => f.HomeMatch):F1}/{homeFid.Count}カ国）　" +
                string.Join("　", homeFid.Select(f => $"{f.Nation}×{f.HomeFracMean:F2}")));
        // ★HomeMatch の国別印字（2026-08-14）：従来は平均保有率（HomeFracMean）しか出ず「どの国が落ちたか」が
        //   出力から決定不能だった（平均に閾値を当てる推論は無効＝敵対検証の指摘）。判定はシードごと frac≥0.7 の平均＝これが実体。
        if (homeFid.Count > 0)
            System.Console.WriteLine($"  領土（本国）一致率の内訳: " + string.Join("　", homeFid.Select(f => $"{f.Nation}{f.HomeMatch:P0}")));
        var colFid = fid.Where(f => !double.IsNaN(f.ColonyMatch)).ToList();
        if (colFid.Count > 0)
            System.Console.WriteLine($"  領土（植民地）: 史実規模の±{HistoricalReference.ColonyCountTolerance:P0} {colFid.Average(f => f.ColonyMatch):P0}（{colFid.Count}カ国で定義・★場所でなく規模で測る）　" +
                string.Join("　", colFid.Select(f => $"{f.Nation}{f.ColonyMean:F1}")));
        if (stuck.Count > 0) System.Console.WriteLine($"  固着（>90%＝史実に固着＝カオス不足）: {string.Join("、", stuck)}");
        if (stray.Count > 0) System.Console.WriteLine($"  逸脱過多（<50%＝要因果監査・次の調整対象）: {string.Join("、", stray)}");
    }
}

try { System.Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }   // 診断出力の日本語をパイプ/リダイレクトでも可読に（headless＝副作用なし）

// === 人口の律速を特定： dotnet run -- popprobe ===
// ★「イギリスの人口が131年で×0.99」の原因を、扶養力の内訳から特定する。
//   人口はロジスティック net = PopGrowthRate × fertility × pop × (1 − pop/capacity) なので、
//   pop/capacity が 1 に張り付いていれば伸びない＝**天井（capacity）の問題**である。
if (args.Length > 0 && args[0] == "popprobe")
{
    // ★2026-08-21拡張（人口忠実度の原因究明）：出生率倍率・空き土地ブースト・食料比の内訳（消費/生産/純輸入）・移民の累積収支・戦死を追加。
    //   引数2＝シード（既定1）。参照7カ国（帯定義国）を全部見張る＝英過小・仏過大の因子分解。
    ulong ppSeed = args.Length > 1 && ulong.TryParse(args[1], out var pps) ? pps : 1UL;
    string[] watch = ["イギリス", "フランス", "プロイセン", "アメリカ", "ロシア", "清", "スペイン", "日本", "オーストリア", "ポルトガル"];
    int[] cuts = [4, 100, 200, 268, 300, 400, 524, Coefficients.GameQuarters];   // ★2026-09-26：断面668（全長の終端）を足した（回0-4・計器の裁き10）。★2026-09-30 段4e 後半：断面268（1850年＝年齢の層の史実の照合）
    var msP = new MultiNationSim(WorldData.Nations(), ppSeed, _ => new UtilityAgent());
    int n0P = msP.Nations.Count;   // 初期国数（ベタ書き禁止＝world.json の実件数から取る）
    msP.StartWarmup();   // ★2026-08-21：selfplay-batch と同じ助走を通す＝「測る世界」を帯の世界と一致させる（暦は進まない・人口動態は凍結）
    var immCum = new System.Collections.Generic.Dictionary<string, double>();   // 累積流入（越境。★回1-2・2026-09-27 に国際移民＝世界プールは削除）
    var emiCum = new System.Collections.Generic.Dictionary<string, double>();   // 累積流出（越境）
    // ★2026-09-26：粗出生・粗死亡（‰/年）＝前の断面からの World.DiagBirths/DiagDeaths の増分÷年数÷今の人口（計器の裁き10・純記帳）
    // ★2026-09-30 段4e J（計器の偏り）：粗死亡は全ての死＝DiagDeaths（人口動態・疫病・遠征の病死）＋WarDeadCumulative（戦死・戦災・鎮圧・粛清）＝死の記帳の1本 World.KillPops が死因で2つへ分ける
    var prevBirths = new System.Collections.Generic.Dictionary<string, double>();
    var prevDeaths = new System.Collections.Generic.Dictionary<string, double>();
    var pop0 = new System.Collections.Generic.Dictionary<string, double>();     // 1783年人口（成長倍率の分母）
    // ★物差しの検証（2026-08-21）：帯の実装（SelfPlay の PopGrowth）＝**現保有州**の総人口比なので、征服が倍率を膨らませ（仏）、
    //   領土崩壊が倍率を潰す（清・西）＝「領土不変な定義」というコメントの意図と乖離。初期州集合ベースの倍率を併記して汚染量を実測する。
    //   州は征服でオブジェクトごと移動する（Diplomacy.cs:2588）＝参照集合で追える。征服で得た州は除外・失った州は自然に落ちる（分離の信号は保持）。
    var initRegions = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<Region>>();
    foreach (var s0 in msP.Nations)
        if (s0.World.SelfPower is { } p0 && System.Array.IndexOf(watch, p0.BaseName) >= 0)
        {
            pop0[p0.BaseName] = s0.World.TotalPopulation;
            initRegions[p0.BaseName] = [.. s0.World.Regions];
        }
    Console.WriteLine("断面Q,国名,人口,倍率,倍率初期州,州数,扶養力,人口/扶養力,扶養力基準,医療(門番後),医者役務,食料比,食料比生産,主食純輸入,耕地計,出生率倍率,空き土地ブースト,工業化,都市化,識字,社会保障,土地改革,累積流入,累積流出,累計戦死,粗出生‰,粗死亡‰,15歳未満%,15-64歳%,60以上%,65以上%,層0-9%,層10-19%,層20-29%,層30-39%,層40-49%,層50-59%,層60-69%,層70以上%,20歳以上の女性比,置き換わり率%/四半期");
    int prevCutP = 0;
    // 粗率（‰/年）＝区間の人数÷区間の年数÷今の人口×1000（区間＝前の断面から今の断面まで）
    double CrudeRate(double count, double pop, int span) => pop > 1e-9 && span > 0 ? count / (span / (double)Calendar.QuartersPerYear) / pop * 1000.0 : 0;
    int doneP = 0;
    // ★計器の是正2026-09-08（F3b蜂起doc §5 段1）：旧＝終局の国数−初期の国数＝**純増**しか出せず、建国も消滅も見えなかった
    //   （13国が同時に建って12国が再併合されると「1回」と印字される）。参照集合の出入りで**建国回数と消滅回数を別に数える**。
    var aliveP = new HashSet<Simulation>(msP.Nations);
    int birthsP = 0, deathsP = 0;
    foreach (int cut in cuts)
    {
        for (; doneP < cut; doneP++)
        {
            msP.StepQuarter();
            var nowP = new HashSet<Simulation>(msP.Nations);
            foreach (var nP in msP.Nations) if (aliveP.Add(nP)) birthsP++;
            deathsP += aliveP.RemoveWhere(x => !nowP.Contains(x));
            foreach (var simP in msP.Nations)
            {
                if (simP.World.SelfPower is not { } pwA || System.Array.IndexOf(watch, pwA.BaseName) < 0) continue;
                immCum.TryGetValue(pwA.BaseName, out var ic); immCum[pwA.BaseName] = ic + simP.World.LastImmigration;
                emiCum.TryGetValue(pwA.BaseName, out var ec); emiCum[pwA.BaseName] = ec + simP.World.LastEmigration;
            }
        }
        foreach (var simP in msP.Nations)
        {
            var w = simP.World;
            if (w.SelfPower is not { } pw || System.Array.IndexOf(watch, pw.BaseName) < 0) continue;
            double lit = 0, sz = 0;
            foreach (var pp in w.AllPops()) { lit += pp.Literacy * pp.Size; sz += pp.Size; }
            // ★年齢の層（2026-09-30 段4e 後半・計器）：兵士を含む全ての POP の人数の和＝国のピラミッド（層ごと・性ごと）と世代の置き換わり率（四半期＝20歳になる人÷(大人＋入る人)）
            var ageN = new double[2 * Pop.AgeBands]; double entQ = 0, adultsQ = 0;
            foreach (var pp in w.AllPops())
            {
                for (int k = 0; k < ageN.Length; k++) ageN[k] += pp.Ages[k] * pp.Size;
                entQ += 3.0 * pp.EnteringAdultsMonthly() * pp.Size; adultsQ += pp.AgeShare(20, double.PositiveInfinity) * pp.Size;
            }
            double ageT = 0; foreach (var x in ageN) ageT += x;
            double[] ageSh = new double[2 * Pop.AgeBands];
            for (int k = 0; k < ageN.Length; k++) ageSh[k] = ageT > 1e-9 ? ageN[k] / ageT : 0;
            string ageCols = $"{100 * Pop.AgeShareOf(ageSh, 0, 15, -1):F1},{100 * Pop.AgeShareOf(ageSh, 15, 65, -1):F1},{100 * Pop.AgeShareOf(ageSh, 60, double.PositiveInfinity, -1):F1},{100 * Pop.AgeShareOf(ageSh, 65, double.PositiveInfinity, -1):F1}";
            for (int b = 0; b < Pop.AgeBands; b++) ageCols += $",{100 * (ageSh[2 * b] + ageSh[2 * b + 1]):F1}";
            double adF = Pop.AgeShareOf(ageSh, 20, double.PositiveInfinity, 1), adA = Pop.AgeShareOf(ageSh, 20, double.PositiveInfinity, -1);
            ageCols += $",{(adA > 1e-12 ? adF / adA : 0):F3},{(adultsQ + entQ > 1e-9 ? 100 * entQ / (adultsQ + entQ) : 0):F3}";
            double popHome = 0;   // ★初期州集合ベースの人口（征服で得た州を除外＝「領土不変な定義」の実装。失った州は自然に落ちる）
            if (initRegions.TryGetValue(pw.BaseName, out var ir))
                foreach (var rr in w.Regions) if (rr.State == RegionState.Active && ir.Contains(rr)) popHome += rr.SumPopulation();
            Console.WriteLine($"{cut},{pw.BaseName},{w.TotalPopulation:F0},{(pop0.TryGetValue(pw.BaseName, out var pz) && pz > 1e-9 ? w.TotalPopulation / pz : 0):F2},{(pop0.TryGetValue(pw.BaseName, out var pz2) && pz2 > 1e-9 ? popHome / pz2 : 0):F2},{w.Regions.Count},{w.DiagCapacity:F0}," +
                $"{(w.DiagCapacity > 1e-9 ? w.TotalPopulation / w.DiagCapacity : 0):F3},{w.DiagCapacityBaseline:F0}," +
                $"{w.DiagHealthcare:F3},{w.DiagDoctorCare:F3},{w.DiagFoodRatio:F3},{w.FoodRatioProductionSmoothed:F3},{w.DiagStapleNetImport:F1},{w.DiagLandAnchor:F0}," +
                $"{w.DiagFertility:F3},{w.DiagLandRoom:F3}," +
                $"{w.MeanIndustrialization:F2},{w.UrbanShare:F3},{(sz > 1e-9 ? lit / sz : 0):F3},{w.SocialSecurity:F3},{(int)w.LandReform}," +
                $"{immCum.GetValueOrDefault(pw.BaseName):F1},{emiCum.GetValueOrDefault(pw.BaseName):F1},{w.WarDeadCumulative:F1}," +
                $"{CrudeRate(w.DiagBirths - prevBirths.GetValueOrDefault(pw.BaseName), w.TotalPopulation, cut - prevCutP):F1},{CrudeRate(w.DiagDeaths + w.WarDeadCumulative - prevDeaths.GetValueOrDefault(pw.BaseName), w.TotalPopulation, cut - prevCutP):F1}," + ageCols);
            prevBirths[pw.BaseName] = w.DiagBirths; prevDeaths[pw.BaseName] = w.DiagDeaths + w.WarDeadCumulative;
        }
        Console.WriteLine($"# Q{cut}: 国数 {msP.Nations.Count}（初期{n0P}）");
        prevCutP = cut;
    }
    // ★国の出入り＝**今日の主犯**。史実の1783-1914で新国家は30前後（中南米18・バルカン6・白ノルウェー等）。
    Console.WriteLine($"\n★建国 {birthsP} 回／消滅 {deathsP} 回（初期{n0P}カ国→{msP.Nations.Count}カ国・純増{msP.Nations.Count - n0P}）");
    return;
}

// === 艦の強さと乗員の断面： dotnet run -- navalgauge ===
// ★(189)（2026-10-03・作者の裁定 2026-08-01「海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」）：海軍力＝艦の強さのゲージ×燃料×乗員の充足×海の熟練。
//   断面ごとに国の 換算分母・艦の強さの値・艦の強さのゲージ・隻数・要る乗員・兵士・乗員の充足・海軍シェア（乗員÷兵士）・海の必要量・海軍力 を並べる（読むだけ）。
//   旧（2026-08-01〜(187)）＝艦隊÷乗員（軍の総合ゲージ×海軍シェア）の比と min の律速の側を数えた（乗員の側が全ての主要国で律速だった＝この計器の旧版が示していた）。
if (args.Length > 0 && args[0] == "navalgauge")
{
    int[] cuts = [4, 200, 350, 524];   // ⚠Q0は MilitaryScale がまだ立っていない（月次Stepで設定される）ので最初の断面はQ4
    var msNG = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    msNG.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す
    Console.WriteLine("断面Q,国名,換算分母,艦の強さの値,艦の強さのゲージ,隻数,要る乗員,兵士,乗員の充足,海軍シェア,海の必要量,海軍力");
    int done = 0;
    foreach (int cut in cuts)
    {
        for (; done < cut; done++) msNG.StepQuarter();
        foreach (var simNG in msNG.Nations)
        {
            var w = simNG.World;
            if (w.SelfPower is not { } pw || w.Regions.Count == 0) continue;
            double denom = EconomyStep.FleetGaugeDenominator(w.Regions[0], w.Coef);
            double hulls = 0; foreach (var rr in w.Regions) if (rr.State == RegionState.Active && rr.FleetUnits is { } fu) foreach (var u in fu) hulls += u;
            double crewNeed = denom > 0 ? hulls * EconomyStep.FleetCrewPerHull(w.Regions[0], w.Coef, denom) : 0;
            Console.WriteLine($"{cut},{pw.Name},{denom:F1},{EconomyStep.FleetPowerValue(w):F0},{pw.FleetGauge:F3},{hulls:F0},{crewNeed:F2},{w.SoldierSize():F1},{pw.CrewFill:F3},{pw.NavalShare:F4},{pw.NeedSea:F3},{pw.NavalPower:F3}");
        }
    }
    return;
}

// === 診断：理想の軍隊の形（段1・2026-08-01）＝**接続する前に分布を見る**ための計器（設計書 docs/202607/20260801_理想の軍隊の形_部分完了.md【一部】.md の段1）。
// ★見るもの：① 注意配分の**列和が全国で1未満**（＝縮小写像＝軍拡が発散しない構造の実証）
//   ② 兵/人口比の分布が史実の開き（普1.1〜1.5%／仏0.9／露0.9／墺0.8／英0.5／清0.2／米0.05）に乗るか
//   ③ 英の海の必要が最大か　④ 清の内的秩序項が陸の過半か　⑤ 英の陸が0にならないか
// ⚠**外れたら係数を足さず式を疑う**（設計書の明文）。 使い方： dotnet PriceOfBread.Runner.dll armyshape [四半期] ===
if (args.Length > 0 && args[0] == "armyshape")
{
    // ★2026-08-22 拡張（作者指示「軍隊の理想の形が上手く機能するかだけ注視」）：
    //   ①陸海の内訳を複数断面で ②seaValue を 港取扱高／植民地価値 に分解 ③列和（縮小写像の前提）
    //   ④理想の海比と**実際**の海比（支出・戦力）が時間とともに近づくか＝機構が名ばかりでないかの検証。
    //   引数： armyshape [四半期]（単一断面・従来） / armyshape long（1783/1850/1914 の3断面）
    bool aLong = args.Length > 1 && args[1] == "long";
    int[] aCuts = aLong ? [1, 268, 524] : [args.Length > 1 && int.TryParse(args[1], out var aq0) ? aq0 : 524];
    ulong aSeed = ArgUlong(args, 2, 1UL);
    var msA2 = new MultiNationSim(WorldData.Nations(), aSeed, _ => new UtilityAgent());
    foreach (var s2A in msA2.Nations) { s2A.World.Coef.ArmyShapeEnabled = 1.0; s2A.TelemetryEnabled = false; }
    msA2.Diplomacy.Log.Enabled = false;
    msA2.StartWarmup();   // App/バッチと同じ開始盤面
    int aDone = 0;
    foreach (int cut in aCuts)
    {
        for (; aDone < cut; aDone++) msA2.StepQuarter();
        Console.WriteLine($"━━ Q{cut}（{msA2.Year}年）━━");
        Console.WriteLine("国名,人口,兵数,理想兵数,陸の必要,内的秩序,海の必要,海上価値,港取扱高,植民地価値(旧ColonyListのみ),理想海比,支出海比,戦力海比,列和最大,海軍力,陸軍力,艦隊ストック");
        double colMax = 0; int nOver1 = 0, nRows = 0;
        foreach (var simA2 in msA2.Nations)
        {
            var wA2 = simA2.World;
            if (wA2.SelfPower is not { } pA2) continue;
            var cA2 = wA2.Coef;
            double sol = wA2.SoldierSize(), pop = wA2.TotalPopulation;
            double fleet = EconomyStep.FleetPowerValue(wA2);   // ★(189) 艦の強さの値（隻数×建造費×段の乗数の和）
            // ★守るべき海上価値の**分解**（ArmyShape.cs:77-82 と同一の絞り込み＝IsPort のみ）
            double portVal = 0, colVal = 0;
            foreach (var rr in wA2.Regions) if (rr.State == RegionState.Active && rr.IsPort) portVal += rr.LastPortThroughput;
            foreach (var col in pA2.ColonyList) colVal += col.Value;
            double seaValue = portVal + colVal;
            // （(183) 通商保護の項〔護衛項＝軍×海に出ている経済の割合〕を海の必要量から外した＝護衛項と注意項の列を削除）
            double idealNaval = ArmyShape.NavalSpendShareOf(pA2);   // 陸海の維持費の分け方と軍需の陸海の再配分が読む同じ1本（(151)・−1＝未定義）
            double forceTot = pA2.NavalPower + pA2.LandPower;
            double forceNaval = forceTot > 1e-9 ? pA2.NavalPower / forceTot : double.NaN;
            if (pA2.AttentionColSumMax > colMax) colMax = pA2.AttentionColSumMax;
            // ⚠**1.0ちょうどを「発散」と誤報しない**：不穏度 u=0 の国では colL = Σhr/Σhr = 1 になり、総和の丸めで 1+1e-16 級に浮く。
            //   真の異常は「1を**有意に**超える」ことなので許容幅を置く（比のガードを絶対値で書かない・否定形の作法と同じ規律）。
            if (pA2.AttentionColSumMax > 1.0 + 1e-6) nOver1++;
            nRows++;
            Console.WriteLine($"{pA2.Name},{pop:F0},{sol:F1},{pA2.NeedMen:F1},{pA2.NeedLand:F3},{pA2.NeedInternal:F3},{pA2.NeedSea:F3}," +
                $"{seaValue:F1},{portVal:F1},{colVal:F1}," +
                $"{idealNaval:F3},{wA2.NavalSpendShare:F3},{forceNaval:F3},{pA2.AttentionColSumMax:F4},{pA2.NavalPower:F2},{pA2.LandPower:F2},{fleet:F0}");
        }
        // ★設計書の blocking 条件＝「列和が1未満であること」が縮小写像の実証
        // ★判定は3値：有意に1超＝発散しうる／1ちょうど（±1e-6）＝**非拡大だが縮小ではない**（設計の保証は空）／有意に1未満＝合格
        string verdict = colMax > 1.0 + 1e-6 ? "★★発散しうる（1を有意に超過）"
                       : colMax >= 1.0 - 1e-6 ? "⚠境界＝**非拡大だが縮小写像ではない**（余裕を作る u=不穏度 が実質0）"
                       : "合格（真に1未満＝縮小写像）";
        Console.WriteLine($"# Q{cut} 列和最大 {colMax:R}（設計書の要件＝**真に1未満**）　有意に1超の国 {nOver1}/{nRows}　{verdict}");
    }
    return;
}

// === 診断：制度の閾値がどれで止まっているか（2026-08-26・TechInstitutionTests の赤の切り分け）===
//   4カ国世界（仏日英米＝テストと同じ Four()）を400四半期回し、イギリスの Freedom / Elite / EliteCore / 各制度を出す。
//   dotnet PriceOfBread.Runner.dll instprobe
if (args.Length > 0 && args[0] == "instprobe")
{
    foreach (var (flag, scope) in new[] { (0.0, 0.0), (1.0, 0.0), (1.0, 1.0) })
    {
        var cf = new Coefficients { ColoniesAreRegions = flag, SuffrageScopeGate = scope };
        var msI = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], 1789UL, null, cf);
        msI.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
        msI.Nations[1].World.ParliamentEnabled = false;
        for (int q = 0; q < 400; q++) msI.StepQuarter();
        var wB = msI.Nations[2].World;
        int colReg = 0; foreach (var rr in wB.Regions) if (rr.Tier == RegionTier.Colonial) colReg++;
        Console.WriteLine($"ColoniesAreRegions={flag} SuffrageScopeGate={scope}　英：州{wB.Regions.Count}（うち植民地格{colReg}）　植民地オブジェクト{wB.SelfPower?.ColonyList.Count}");
        Console.WriteLine($"   Freedom {wB.Freedom:F3}（閾値 {cf.FrontierFreedomThreshold:F3}）　教育法 {wB.EducationLaw}");
        Console.WriteLine($"   Elite {wB.Elite:F4}　EliteCore {wB.EliteCore:F4}");
        Console.WriteLine($"   特許 {wB.PatentLaw}　株式会社 {wB.JointStock}　鉄道 {wB.Railways}　技術 {wB.TechLevel:F2}");
        var (tr3, mo3, ma3) = InterestGroups.Blocs(wB);
        Console.WriteLine($"   参政権 {wB.Suffrage}　報道 {wB.Censorship}　政体 {wB.Polity}");
        Console.WriteLine($"   ブロック力 旧支配層{tr3:F3} 近代{mo3:F3} 大衆{ma3:F3}　満足 {NationalScore.MeanSatisfaction(wB):F3}　闘争 {Politics.AvgMassMilitancy(wB):F3}");
        Console.WriteLine($"   人口 {wB.TotalPopulation:F0}");
    }
    return;
}

// === 診断：州の面積と「州が足りない場所」（作者指示2026-08-26 (1)「州を人口だけでなく面積でも分割する」）===
//   ★陸マスク（Landmask＝地図SVGを0.1°格子へ焼いた真値）の各陸セルを**最寄りの州**へ割り当てる。
//     これで ①州ごとの面積 ②どの陸が州から遠いか（＝州が足りない場所）が同時に出る。ポリゴンは無いのでこれが唯一の面積の出どころ。
//   ★新しい州の位置は**最遠点貪欲法**で選ぶ（毎回いちばん遠い陸セルを新しい州にする）＝任意の閾値を先に決めない。
//     k個足したときの最長距離 D(k) が出るので、作者指示「州数が1.5倍になるあたり」を D から逆に読める。
//   dotnet PriceOfBread.Runner.dll provgap [追加する州数] [格子の粗さ度]
if (args.Length > 0 && args[0] == "provgap")
{
    int addN = ArgInt(args, 1, 102);      // 既定＝本国州204の0.5倍＝1.5倍になる数
    double gs = ArgDouble(args, 2, 0.5) ?? 0.5;  // 走査の格子（度）。0.5°≒55km
    // ① 既存の場所を集める（本国州・植民地・辺境）＝Climate.CityCoord が唯一の座標源
    var sites = new List<(string Name, string Owner, string Kind, double Lat, double Lon)>();
    foreach (var cfgG in WorldData.Nations())
    {
        foreach (var pv in cfgG.Provinces) { var cc = Climate.CityCoord(pv.City); sites.Add((pv.City, cfgG.Name, "州", cc.Lat, cc.Lon)); }
        foreach (var co in cfgG.InitialColonies) { var cc = Climate.CityCoord(co.Name); sites.Add((co.Name, cfgG.Name, "植民地", cc.Lat, cc.Lon)); }
    }
    int nHome = sites.Count(x => x.Kind == "州");
    Console.WriteLine($"# provgap　既存 {sites.Count} 箇所（本国州 {nHome}／植民地 {sites.Count(x => x.Kind == "植民地")}）　走査格子 {gs}°");
    // ② 陸セルを列挙（面積は cos緯度 で重み付け）
    var cells = new List<(double Lat, double Lon, double AreaKm2)>();
    const double DegKm = 111.32;
    for (double la = 83.0; la > -56.0; la -= gs)
        for (double lo = -180.0; lo < 180.0; lo += gs)
            if (Landmask.IsLand(la, lo))
                cells.Add((la, lo, gs * DegKm * gs * DegKm * Math.Cos(la * Math.PI / 180.0)));
    double totalLand = cells.Sum(x => x.AreaKm2);
    Console.WriteLine($"# 陸セル {cells.Count} 個　総面積 {totalLand / 1e6:F1} 百万km2（参考：地球の陸地は約149百万km2）");
    // ★島嶼を除く（作者指示の趣旨＝大陸の空白に州を置く）。「周囲300kmの陸地面積」が閾値未満のセルは新設の候補にしない。
    //   ⚠これは州を消す操作ではない＝**新しい州をどこに置くかの候補**を絞るだけ。既存の島の州（ホノルル等）はそのまま。
    double islandMin = ArgDouble(args, 3, 40_000.0) ?? 40_000.0;   // km2（ニュージーランドは通り、ケルゲレンは落ちる）
    var localLand = new double[cells.Count];
    for (int i = 0; i < cells.Count; i++)
    {
        double sum = 0;
        for (int j = 0; j < cells.Count; j++)
        {
            if (Math.Abs(cells[j].Lat - cells[i].Lat) > 3.0) continue;   // 粗い枝刈り（±3°＝約330km）
            if (Geo.DistanceKm(cells[i].Lat, cells[i].Lon, cells[j].Lat, cells[j].Lon) <= 300.0) sum += cells[j].AreaKm2;
        }
        localLand[i] = sum;
    }
    int eligible = 0; for (int i = 0; i < cells.Count; i++) if (localLand[i] >= islandMin) eligible++;
    Console.WriteLine($"# 候補セル {eligible}/{cells.Count}（周囲300kmの陸地が {islandMin / 1000:F0}千km2 以上＝島嶼の一点を除く）");
    // ③ 各陸セルの最寄り距離と割り当て先
    int nc = cells.Count, ns = sites.Count;
    var near = new double[nc]; var owner = new int[nc];
    for (int i = 0; i < nc; i++)
    {
        double best = double.MaxValue; int bi = -1;
        for (int j = 0; j < ns; j++)
        {
            double d = Geo.DistanceKm(cells[i].Lat, cells[i].Lon, sites[j].Lat, sites[j].Lon);
            if (d < best) { best = d; bi = j; }
        }
        near[i] = best; owner[i] = bi;
    }
    // ④ 州ごとの面積（上位・下位）
    var areaBy = new double[ns];
    for (int i = 0; i < nc; i++) if (owner[i] >= 0) areaBy[owner[i]] += cells[i].AreaKm2;
    var ordered = Enumerable.Range(0, ns).OrderByDescending(j => areaBy[j]).ToArray();
    Console.WriteLine();
    Console.WriteLine("# 面積の大きい州 上位25（＝1州が抱えすぎている場所）");
    foreach (int j in ordered.Take(25))
        Console.WriteLine($"  {sites[j].Name}（{sites[j].Owner}・{sites[j].Kind}）　{areaBy[j] / 1e6:F2} 百万km2");
    var sortedNear = near.OrderBy(x => x).ToArray();
    Console.WriteLine();
    Console.WriteLine($"# 陸から最寄りの州までの距離　中央 {sortedNear[nc / 2]:F0}km　四分位 {sortedNear[nc / 4]:F0}／{sortedNear[nc * 3 / 4]:F0}　最大 {sortedNear[^1]:F0}km");
    // ⑤ 最遠点貪欲法で addN 個を足す（毎回いちばん遠い陸セルを新しい州にする）
    Console.WriteLine();
    Console.WriteLine($"# 最遠点貪欲法で {addN} 箇所を追加したときの最長距離の推移");
    var picked = new List<int>();
    var cur = (double[])near.Clone();
    for (int k = 0; k < addN; k++)
    {
        int far = -1; double fd = -1;
        for (int i = 0; i < nc; i++) if (localLand[i] >= islandMin && cur[i] > fd) { fd = cur[i]; far = i; }
        if (far < 0) { Console.WriteLine("  （候補が尽きた）"); break; }
        picked.Add(far);
        for (int i = 0; i < nc; i++)
        {
            double d = Geo.DistanceKm(cells[i].Lat, cells[i].Lon, cells[far].Lat, cells[far].Lon);
            if (d < cur[i]) cur[i] = d;
        }
        if (k == 0 || (k + 1) % 10 == 0 || k == addN - 1)
        {
            double mx = 0; for (int i = 0; i < nc; i++) if (localLand[i] >= islandMin && cur[i] > mx) mx = cur[i];
            Console.WriteLine($"  +{k + 1}州（本国州 {nHome} → {nHome + k + 1}＝{(double)(nHome + k + 1) / nHome:F2}倍）　候補域の最長距離 {mx:F0}km");
        }
    }
    Console.WriteLine();
    Console.WriteLine("# 追加する州の位置（貪欲法の順・最寄りの既存州つき）");
    Console.WriteLine("番号,緯度,経度,最寄りの既存州,その国,距離km");
    for (int k = 0; k < picked.Count; k++)
    {
        var ce = cells[picked[k]];
        int bi = owner[picked[k]];
        Console.WriteLine($"{k + 1},{ce.Lat:F1},{ce.Lon:F1},{(bi >= 0 ? sites[bi].Name : "-")},{(bi >= 0 ? sites[bi].Owner : "-")},{near[picked[k]]:F0}");
    }
    return;
}

// === 診断：輸送力の「州割り」の損失（作者指摘2026-08-26「輸送力が適切に分配されていない。州割りになっている」）===
//   BilateralMarket は国の輸送容量を**活動州の数で頭割り**し（share = 1/activeCount）、州ごとの上限として使う。
//   使われなかった分は他州へ回らない＝交易しない州に配られた容量は死蔵する。
//   ここでは州ごとの (容量, 潜在量) を突き合わせ、**いまの頭割り**と**国で束ねた場合**の輸送量を比べる。
//   dotnet PriceOfBread.Runner.dll transportsplit [四半期] [seed]
if (args.Length > 0 && args[0] == "transportsplit")
{
    int spQ = ArgInt(args, 1, 524);
    ulong spSeed = ArgUlong(args, 2, 1UL);
    var msS = new MultiNationSim(WorldData.Nations(), spSeed, _ => new UtilityAgent());
    foreach (var sS in msS.Nations) sS.TelemetryEnabled = false;
    msS.Diplomacy.Log.Enabled = false;
    msS.StartWarmup();
    for (int q = 0; q < spQ; q++) msS.StepQuarter();
    Console.WriteLine($"# transportsplit Q{spQ}（{msS.Year}年）seed={spSeed}");
    Console.WriteLine("国名,州数,港州数,鉄道容量,海運容量,配分合計,潜在合計,頭割りの輸送量,束ねた輸送量,取りこぼし率,遊休容量,不足州数");
    double wIdle = 0, wCapAll = 0, wFlat = 0, wPool = 0;
    foreach (var simS in msS.Nations)
    {
        var wS = simS.World;
        int nReg = 0, nPort = 0;
        double capSum = 0, potSum = 0, flat = 0, idle = 0; int shortN = 0;
        foreach (var rS in wS.Regions)
        {
            if (rS.State != RegionState.Active) continue;
            nReg++; if (rS.IsPort) nPort++;
            double capS = rS.LastTransportCap, potS = rS.LastTradePotential;
            if (!(capS > 0) || capS == double.MaxValue) continue;   // 否定形＝未計算/無制限は数えない
            capSum += capS; potSum += potS;
            flat += Math.Min(capS, potS);                            // いまの頭割りで実際に運べる量
            if (potS < capS) idle += capS - potS; else shortN++;      // 使い残し／足りない州
        }
        if (!(capSum > 0)) continue;
        double pool = Math.Min(capSum, potSum);                      // 国で束ねたら運べる量
        double lossRate = pool > 1e-9 ? 1.0 - flat / pool : 0;
        wIdle += idle; wCapAll += capSum; wFlat += flat; wPool += pool;
        Console.WriteLine($"{wS.SelfPower?.Name},{nReg},{nPort},{wS.RailCapacity:F0},{wS.ShipCapacity:F0}," +
            $"{capSum:F0},{potSum:F0},{flat:F0},{pool:F0},{lossRate:P1},{idle:F0},{shortN}");
    }
    Console.WriteLine();
    Console.WriteLine($"# 全世界： 配分合計 {wCapAll:F0}　遊休 {wIdle:F0}（{(wCapAll > 0 ? wIdle / wCapAll : 0):P1}）" +
        $"　頭割りの輸送量 {wFlat:F0} → 束ねた場合 {wPool:F0}　**取りこぼし {(wPool > 1e-9 ? 1.0 - wFlat / wPool : 0):P1}**");
    return;
}

// === 診断：基本段の耐久品ストックが軍需産業に需要の枯れを作らないか（2026-08-26）===
//   仮説＝在庫方式は「軍が縮むと目標を在庫が上回り、摩耗で減るまで発注が0になる」＝小国の軍需生産者が買い手を失う。
//   dotnet PriceOfBread.Runner.dll armsdebug <seed> <国名> <fromQ> <toQ>
if (args.Length > 0 && args[0] == "armsdebug")
{
    ulong dSeed = ArgUlong(args, 1, 5UL);
    string dName = args.Length > 2 ? args[2] : "ハノーファー";
    int dFrom = ArgInt(args, 3, 340);
    int dTo = ArgInt(args, 4, 400);
    var msD = new MultiNationSim(WorldData.Nations(), dSeed, _ => new UtilityAgent());
    foreach (var sD in msD.Nations) sD.TelemetryEnabled = false;
    msD.Diplomacy.Log.Enabled = false;
    msD.StartWarmup();
    Console.WriteLine($"# armsdebug seed={dSeed} 国={dName}　BaseArmsStockEnabled={msD.Nations[0].World.Coef.BaseArmsStockEnabled}");
    Console.WriteLine("四半期,年,規模,平時規模,小銃目標,小銃在庫,小銃発注,火砲目標,火砲在庫,装備充足,即応度,軍事力,名目産出,損失累計,損失増分,軍需生産者数,平均採算,倒産数,小銃生産者数,小銃産出計,小銃規模平均,小銃建設規模計,小銃投入充足平均,小銃価格,小銃基準価格,建材採算率,首都の未達意志,首都の建設中,国庫,財政バッファ");   // ★2026-09-07：着工が止まる根（建材の高値ゲート/未達の意志/国庫）   // ★2026-09-07：小銃(82)の生産者の内訳（英の軍が立たない根＝能力か投入か価格か）
    double prevLoss = -1;
    for (int q = 0; q < dTo; q++)
    {
        msD.StepQuarter();
        if (q + 1 < dFrom) continue;
        var simD = msD.Nations.FirstOrDefault(x => (x.World.SelfPower?.Name ?? "") == dName);
        if (simD is null) { Console.WriteLine($"{q + 1},（{dName} は不在）"); continue; }
        var wD = simD.World; var rD = wD.Region; var cD = wD.Coef;
        double Tgt(int b)
        {
            double wr = b < cD.MilitaryWearRates.Length ? cD.MilitaryWearRates[b] : 1.0;
            return !(cD.BaseArmsStockEnabled > 0) || !(wr < 1.0) ? 0 : rD.MilitaryScalePeace * cD.MilitaryRates[b] / Math.Max(1e-6, wr);
        }
        double Held(int b) => rD.ArmsStock is { } dd && dd.TryGetValue(cD.MilitaryGoods[b], out double v) ? v : Tgt(b);
        double t0 = Tgt(0), h0 = Held(0), t2 = Tgt(2), h2 = Held(2);
        int prodN = 0, bankN = 0; double marN = 0, marD = 0;
        foreach (var pr in rD.Producers)
            if (Array.IndexOf(cD.MilitaryGoods, pr.GoodId) >= 0)
            {
                prodN++; if (pr.Bankrupt) bankN++;
                double sc = Math.Max(0, pr.Scale); marN += pr.SmoothedMargin * sc; marD += sc;
            }
        double loss = wD.LossFinanced, dLoss = prevLoss < 0 ? 0 : Math.Max(0, loss - prevLoss);
        prevLoss = loss;
        int rifN = 0; double rifOut = 0, rifScale = 0, rifBuilt = 0, rifFill = 0;   // ★小銃(82)の生産者の内訳
        foreach (var pr in rD.Producers)
            if (pr.GoodId == Catalog.RifleId) { rifN++; rifOut += pr.Output; rifScale += pr.Scale; rifBuilt += pr.BuiltScale; rifFill += pr.LastInputFill; }
        double rifPrice = Catalog.RifleId < rD.Markets.Length ? rD.Markets[Catalog.RifleId].Price : 0, rifRef = Catalog.RifleId < Catalog.RefPrices.Length ? Catalog.RefPrices[Catalog.RifleId] * Math.Max(rD.PriceLevel, 1e-9) : 0;
        int pendingWill = 0, building = 0;
        foreach (var pr in rD.Producers) { pendingWill += Math.Max(0, pr.StateTarget - World.StateUnits(pr, cD)); if (pr.ConstructionLeft > 0) building++; }
        Console.WriteLine($"{q + 1},{msD.Year},{rD.MilitaryScale:F3},{rD.MilitaryScalePeace:F3}," +
            $"{t0:F1},{h0:F1},{Math.Max(0, t0 - h0):F2},{t2:F1},{h2:F1}," +
            $"{rD.MilitaryEquipFill:F3},{wD.MilitaryReadiness:F3},{wD.SelfPower?.Military ?? 0:F3},{wD.NominalGdp * 3:F0}," +
            $"{loss:F0},{dLoss:F1},{prodN},{(marD > 1e-9 ? marN / marD : 0):F3},{bankN}," +
            $"{rifN},{rifOut:F2},{(rifN > 0 ? rifScale / rifN : 0):F3},{rifBuilt:F3},{(rifN > 0 ? rifFill / rifN : 0):F3},{rifPrice:F2},{rifRef:F2}," +
            $"{World.ConstructionAfford(wD):F3},{pendingWill},{building},{wD.Treasury:F0},{UtilityAgent.FiscalBufferOf(wD):F0}");
    }
    return;
}

// === 診断：どこで時間を食っているか（作者報告2026-08-26「シミュレートに時間がかかりすぎ」）===
//   POB_TIME=1 で MultiNationSim が各国の日次・国際市場の所要を積む。ここは1試合回して内訳を出すだけ。
//   dotnet PriceOfBread.Runner.dll timeprobe [四半期]
if (args.Length > 0 && args[0] == "timeprobe")
{
    int tq = ArgInt(args, 1, 120);
    var swT = System.Diagnostics.Stopwatch.StartNew();
    var msTi = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    foreach (var sTi in msTi.Nations) sTi.TelemetryEnabled = false;
    msTi.Diplomacy.Log.Enabled = false;
    long tBuild = swT.ElapsedMilliseconds;
    msTi.StartWarmup();
    long tWarm = swT.ElapsedMilliseconds - tBuild;
    for (int q = 0; q < tq; q++) msTi.StepQuarter();
    long tAll = swT.ElapsedMilliseconds;
    double f = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
    int regs = msTi.Nations.Sum(x => x.World.Regions.Count);
    Console.WriteLine($"# timeprobe {tq}四半期　国{msTi.Nations.Count}　州{regs}　POB_TIME={(MultiNationSim.TimeOn ? "on" : "off")}");
    Console.WriteLine($"  構築 {tBuild}ms　助走 {tWarm}ms　本走 {tAll - tBuild - tWarm}ms　合計 {tAll}ms");
    if (MultiNationSim.TimeOn)
    {
        double n = MultiNationSim.TicksNations * f, m = MultiNationSim.TicksMarket * f;
        Console.WriteLine($"  各国の日次 {n:F0}ms（{n / Math.Max(1, tAll):P0}）　国際市場 {m:F0}ms（{m / Math.Max(1, tAll):P0}）　その他 {tAll - n - m:F0}ms");
        Console.WriteLine($"  1四半期あたり {(double)(tAll - tBuild - tWarm) / tq:F1}ms　→ {Coefficients.GameQuarters}四半期なら {(double)(tAll - tBuild - tWarm) / tq * Coefficients.GameQuarters / 1000:F0}秒/試合");
        Console.WriteLine($"  清算内訳: リスト構築 {PriceOfBread.SimCore.InternationalMarket.TimeListsTicks * f:F0}ms　清算ループ {PriceOfBread.SimCore.InternationalMarket.TimeClearTicks * f:F0}ms"
            + $"　財処理 {PriceOfBread.SimCore.InternationalMarket.CntGoods}　買い手行 {PriceOfBread.SimCore.InternationalMarket.CntBuyers}"
            + $"　反復 {PriceOfBread.SimCore.InternationalMarket.CntRollIters}　走査歩 {PriceOfBread.SimCore.InternationalMarket.CntScanSteps}　空振り {PriceOfBread.SimCore.InternationalMarket.CntSkips}"
            + $"　着値充填 {PriceOfBread.SimCore.InternationalMarket.CntFills}　成約 {PriceOfBread.SimCore.InternationalMarket.CntDeals}");
        // ★無効な売り手を訪ねない（2026-09-26）：機構が働いたか＝行ごと飛ばした買い手行（条件別・買い手行に対する割合）・lp で打ち切った回数・楔の下界で飛ばした売り手。
        long rsN = PriceOfBread.SimCore.InternationalMarket.CntRowSkipNeed, rsR = PriceOfBread.SimCore.InternationalMarket.CntRowSkipRef,
             rsD = PriceOfBread.SimCore.InternationalMarket.CntRowSkipDead, rsB = PriceOfBread.SimCore.InternationalMarket.CntRowSkipBudget;
        long rsAll = rsN + rsR + rsD + rsB, rowsAll = PriceOfBread.SimCore.InternationalMarket.CntBuyers;
        Console.WriteLine($"  無効な売り手を訪ねない: 行ごと飛ばした {rsAll}（買い手行の {(rowsAll > 0 ? 100.0 * rsAll / rowsAll : 0):F1}%＝不足なし {rsN}・参照価格0 {rsR}・需要0 {rsD}・予算0 {rsB}）"
            + $"　lp で打ち切り {PriceOfBread.SimCore.InternationalMarket.CntCutLp}　楔の下界で飛ばした売り手 {PriceOfBread.SimCore.InternationalMarket.CntWedgeSkips}");
        // ★国内価格の旬次（2026-09-26 A3）：機構が本当に働いたか＝閉じた式で進んだ財の割合（1−e）・日次に落ちた理由・打ち切りで省いた日数（1−f）。
        //   助走も含む（計器は構築から数える）。検算＝閉じた式＋日次の理由の和＝財の歩／日次の回＋省いた日＝日次に落ちた財×h（刻み1は×1）。
        long mfSteps = MarketFlow.CntGoodSteps, mfCg = MarketFlow.CntClosedGoods, mfCs = MarketFlow.CntClosedService, mfCp = MarketFlow.CntClosedPinned;
        long mfStart = MarketFlow.CntDailyStartBand, mfPred = MarketFlow.CntDailyPredBand, mfLab = MarketFlow.CntDailyLabor,
             mfMu = MarketFlow.CntDailyMarkup, mfHard = MarketFlow.CntDailyHardBand, mfNaN = MarketFlow.CntDailyNaN, mfH1 = MarketFlow.CntDailyStep1;
        long mfDaily = mfStart + mfPred + mfLab + mfMu + mfHard + mfNaN + mfH1, mfRun = MarketFlow.CntDailyDaysRun, mfSkip = MarketFlow.CntDailyDaysSkipped;
        double PcMf(long a, long b) => b > 0 ? 100.0 * a / b : 0.0;
        Console.WriteLine($"  国内価格（旬 h={msTi.Nations[0].World.Coef.PriceFlowStepDays}日）: 財の歩 {mfSteps}　閉じた式 {mfCg + mfCs + mfCp}（{PcMf(mfCg + mfCs + mfCp, mfSteps):F1}%＝財 {mfCg}・役務 {mfCs}・張り付いた財 {mfCp}）"
            + $"　日次に落ちた {mfDaily}（e={PcMf(mfDaily, mfSteps):F1}%）");
        Console.WriteLine($"    日次の理由: 始めの帯 {mfStart}（上側 {MarketFlow.CntDailyStartHigh}・下側 {mfStart - MarketFlow.CntDailyStartHigh}／うち役務 {MarketFlow.CntDailyStartService}）　予測の帯 {mfPred}　労働 {mfLab}　休眠の上乗せ {mfMu}　固い帯 {mfHard}　NaN {mfNaN}　刻み1 {mfH1}"
            + $"　／日次の回 {mfRun}　打ち切りで省いた日 {mfSkip}（f={PcMf(mfRun, mfRun + mfSkip):F1}%）"
            + $"　検算 {(mfCg + mfCs + mfCp + mfDaily == mfSteps ? "OK" : "不一致")}");
    }
    return;
}

// === 診断：軍縮は創発するか・理想の軍隊は発散しないか（作者質問2026-08-26）===
//   armyshape は**断面**しか見ないので「下がったか」を答えられない。ここでは5年刻みの**時系列**を取り、
//   ①軍事費と兵士比が史実の帯に入るか ②平時に**落ちる**エピソード（＝軍縮）が出るか
//   ③実兵数÷理想兵数が時間とともに発散しないか（上限張り付きを含む）を見る。
//   dotnet PriceOfBread.Runner.dll armstrace [seed]
if (args.Length > 0 && args[0] == "armstrace")
{
    ulong tSeed = ArgUlong(args, 1, 1UL);
    var msT2 = new MultiNationSim(WorldData.Nations(), tSeed, _ => new UtilityAgent());
    foreach (var sT in msT2.Nations) sT.TelemetryEnabled = false;
    msT2.Diplomacy.Log.Enabled = false;
    msT2.StartWarmup();
    const int Step = 20;                       // 5年刻み
    int nN = msT2.Nations.Count;
    var names = new string[nN];
    var spend = new List<double[]>();          // [標本][国] 軍事費
    var solSh = new List<double[]>();          // 兵士比（兵数÷人口）
    var fillR = new List<double[]>();          // 実兵数÷理想兵数
    var warF  = new List<bool[]>();            // 交戦中
    var stanc = new List<int[]>();             // 選ばれたスタンス（0近代化 1産業化 2福祉 3緊縮 4軍備）
    var brk   = new List<UtilityAgent.StanceBreakdown[]>();   // uMil の内訳
    var years = new List<int>();
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        msT2.StepQuarter();
        if ((q + 1) % Step != 0) continue;
        var a = new double[nN]; var b = new double[nN]; var c = new double[nN]; var d = new bool[nN];
        var e = new int[nN]; var f = new UtilityAgent.StanceBreakdown[nN];
        for (int i = 0; i < nN; i++)
        {
            var wT2 = msT2.Nations[i].World;
            // ⚠純関数（World を書き換えない）＝観測が世界を動かさない。本体は SelfPlay と同一の1つ。
            e[i] = (int)UtilityAgent.ChooseStance(wT2, out f[i]);
            names[i] = wT2.SelfPower?.Name ?? "?";
            double popT = wT2.TotalPopulation, solT = wT2.SoldierSize();
            a[i] = wT2.MilitarySpending;
            b[i] = popT > 1e-9 ? solT / popT : 0;
            double needT = wT2.SelfPower?.NeedMen ?? 0;
            c[i] = needT > 1e-9 ? solT / needT : double.NaN;
            d[i] = wT2.SelfPower is { } spT2 && msT2.Diplomacy.AtWarAny(spT2);
        }
        spend.Add(a); solSh.Add(b); fillR.Add(c); warF.Add(d); stanc.Add(e); brk.Add(f); years.Add(msT2.Year);
    }
    int nS = years.Count;
    Console.WriteLine("# ①世界の推移（平均は全カ国）");
    Console.WriteLine("年,平均軍事費,上位5カ国軍事費,軍事費が上限の国,平均兵士比,最大兵士比,平均実÷理想,理想の1.2倍超,交戦国数,前期より軍事費減");
    for (int t = 0; t < nS; t++)
    {
        var sp = spend[t]; var so = solSh[t]; var fi = fillR[t]; var wf = warF[t];
        double avgSp = sp.Average(), avgSo = so.Average(), maxSo = so.Max();
        double top5 = sp.OrderByDescending(x => x).Take(5).Average();
        int capN = sp.Count(x => x > 0.999);
        var fiOk = fi.Where(x => !double.IsNaN(x)).ToArray();
        double avgFi = fiOk.Length > 0 ? fiOk.Average() : double.NaN;
        int overN = fiOk.Count(x => x > 1.2);
        int warN = wf.Count(x => x);
        int downN = t == 0 ? 0 : Enumerable.Range(0, nN).Count(i => sp[i] < spend[t - 1][i] * 0.95);
        Console.WriteLine($"{years[t]},{avgSp:F4},{top5:F4},{capN},{avgSo:P2},{maxSo:P2},{avgFi:F3},{overN},{warN},{downN}");
    }
    // ②軍縮エピソード＝**非交戦**中に、直前10年の山から軍事費が25%以上落ちたこと。
    //   ⚠比のガードを絶対値で書かない：山が痩せた国（<0.02）は分母が痩せて万%に跳ねるので除外する。
    var eps = new List<(int Year, string Name, double From, double To)>();
    for (int t = 2; t < nS; t++)
        for (int i = 0; i < nN; i++)
        {
            if (warF[t][i]) continue;
            double peak = 0; for (int k = Math.Max(0, t - 2); k < t; k++) peak = Math.Max(peak, spend[k][i]);
            if (!(peak > 0.02)) continue;
            if (spend[t][i] < peak * 0.75) eps.Add((years[t], names[i], peak, spend[t][i]));
        }
    Console.WriteLine();
    Console.WriteLine($"# ②軍縮エピソード（非交戦中に10年の山から-25%以上）合計 {eps.Count} 件");
    foreach (var g in eps.GroupBy(e => e.Year / 10 * 10).OrderBy(g => g.Key))
        Console.WriteLine($"  {g.Key}年代: {g.Count()}件  {string.Join(" ", g.Take(8).Select(e => $"{e.Name}({e.From:F3}→{e.To:F3})"))}");
    // ③発散の判定＝実÷理想と兵士比の**長期の傾き**。平時の史実帯は人口の0.5〜1.5%。
    Console.WriteLine();
    Console.WriteLine("# ③発散の判定（前半平均 → 後半平均）");
    int half = nS / 2;
    double sp1 = Enumerable.Range(0, half).Average(t => spend[t].Average());
    double sp2 = Enumerable.Range(half, nS - half).Average(t => spend[t].Average());
    double so1 = Enumerable.Range(0, half).Average(t => solSh[t].Average());
    double so2 = Enumerable.Range(half, nS - half).Average(t => solSh[t].Average());
    double fi1 = Enumerable.Range(0, half).Average(t => fillR[t].Where(x => !double.IsNaN(x)).DefaultIfEmpty(double.NaN).Average());
    double fi2 = Enumerable.Range(half, nS - half).Average(t => fillR[t].Where(x => !double.IsNaN(x)).DefaultIfEmpty(double.NaN).Average());
    Console.WriteLine($"  軍事費 {sp1:F4} → {sp2:F4}　兵士比 {so1:P2} → {so2:P2}（史実の平時帯 0.5〜1.5%）　実÷理想 {fi1:F3} → {fi2:F3}");
    int capEnd = spend[nS - 1].Count(x => x > 0.999);
    Console.WriteLine($"  最終断面で軍事費が上限(1.0)の国 {capEnd}/{nN}　{(capEnd > nN / 4 ? "★振り切れ＝上限が拘束している" : "内点解")}");
    // ④どの項が軍拡を勝たせ続けているか。★「余裕」＝uMil と2位の効用の差＝どれだけ押せば軍備から降りるか。
    Console.WriteLine();
    Console.WriteLine("# ④スタンスと効用の内訳（平均は全カ国）");
    Console.WriteLine("年,軍備,近代化,産業化,福祉,緊縮,平均uMil,平均2位,平均の余裕,教義底上げ,緊張項,植民地防衛,生存,平均緊張,緊張100以上");
    for (int t = 0; t < nS; t++)
    {
        var st = stanc[t]; var bk = brk[t];
        int nMil = st.Count(x => x == 4), nMod = st.Count(x => x == 0), nInd = st.Count(x => x == 1);
        int nWel = st.Count(x => x == 2), nFis = st.Count(x => x == 3);
        double aMil = bk.Average(x => x.UMil);
        double aSecond = bk.Average(x => Math.Max(Math.Max(x.UMod, x.UInd), Math.Max(x.UWel, x.UFis)));
        double aRoom = bk.Average(x => x.UMil - Math.Max(Math.Max(x.UMod, x.UInd), Math.Max(x.UWel, x.UFis)));
        Console.WriteLine($"{years[t]},{nMil},{nMod},{nInd},{nWel},{nFis},{aMil:F3},{aSecond:F3},{aRoom:F3}," +
            $"{bk.Average(x => x.MilitaristBase):F3},{bk.Average(x => x.TensionTerm):F3},{bk.Average(x => x.ColonyDefense):F3}," +
            $"{bk.Average(x => x.Survival):F3},{bk.Average(x => x.Tension):F1},{bk.Count(x => x.Tension >= 100)}");
    }
    // ★どの項を消せば軍備から降りるか＝項ごとの「これを0にしたら2位に負ける国」の数（最終断面）。
    {
        var bkE = brk[nS - 1]; var stE = stanc[nS - 1];
        int milN = stE.Count(x => x == 4);
        int dropBase = 0, dropTen = 0, dropCol = 0, dropSur = 0, dropAll = 0;
        foreach (var x in bkE)
        {
            double second = Math.Max(Math.Max(x.UMod, x.UInd), Math.Max(x.UWel, x.UFis));
            if (!(x.UMil > second)) continue;                         // もともと軍備でない国は数えない
            if (x.UMil - x.MilitaristBase <= second) dropBase++;
            if (x.UMil - x.TensionTerm <= second) dropTen++;
            if (x.UMil - x.ColonyDefense <= second) dropCol++;
            if (x.UMil - x.Survival <= second) dropSur++;
            if (x.UMil - x.MilitaristBase - x.TensionTerm <= second) dropAll++;
        }
        Console.WriteLine();
        Console.WriteLine($"# ⑤最終断面で軍備が勝っている国 {milN}/{nN}。その項を0にしたら降りる国の数：");
        Console.WriteLine($"  教義底上げ {dropBase}　緊張項 {dropTen}　植民地防衛 {dropCol}　生存 {dropSur}　教義と緊張の両方 {dropAll}");
    }
    // ⑥脅威の出所＝生存効用の max を取った相手。1カ国の巨大な軍が全世界を軍拡に縛っていないかを見る。
    Console.WriteLine();
    Console.WriteLine("# ⑥脅威の出所（何カ国から「最大の脅威」と見なされているか）");
    foreach (int tt in new[] { 0, nS / 2, nS - 1 })
    {
        var bkT = brk[tt];
        var top = bkT.GroupBy(x => x.ThreatFrom).OrderByDescending(g => g.Count()).Take(5)
                     .Select(g => $"{g.Key} {g.Count()}カ国(比の中央値 {g.OrderBy(x => x.ThreatRatio).ElementAt(g.Count() / 2).ThreatRatio:F1})");
        var rr = bkT.Select(x => x.ThreatRatio).OrderBy(x => x).ToArray();
        Console.WriteLine($"  {years[tt]}年: {string.Join("　", top)}");
        Console.WriteLine($"          脅威比の分布 四分位 {rr[rr.Length / 4]:F1} / 中央 {rr[rr.Length / 2]:F1} / {rr[rr.Length * 3 / 4]:F1}　最大 {rr[^1]:F1}　閾値1.5未満の国 {rr.Count(x => x < 1.5)}/{nN}");
    }
    return;
}

// === 診断：輸送容量の断面（T0・2026-07-31）＝「清が過大・蘭が過小」は推測であり未測、という指摘に答える計器。
// 実容量（鉄道/海運）と、混雑（LastTradeThrottle）・買い手の財布を国別に並べる（目標列は撤去＝Transport.Step のローカル目標は脅威/混雑の乗数込みで Runner から再導出できない・2026-09-11 #14）。
// 容量/人口 と 容量/GDP を併記＝「人口で持つ国」と「富で持つ国」の分離が数字で出る。 dotnet PriceOfBread.Runner.dll transportdump [四半期] ===
if (args.Length > 0 && args[0] == "transportdump")
{
    int tq = ArgInt(args, 1, 200);
    var msT = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    msT.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < tq; q++) msT.StepQuarter();
    System.Console.WriteLine("国名,人口,GDP,鉄道容量,海運容量,合計容量,容量per人口,容量perGDP,平均律速,植民地海運負担,商人貯蓄,投資家貯蓄,汽船の割合");   // 汽船の割合＝海運容量に占める汽船（2026-09-26 回4-3）
    foreach (var simT in msT.Nations)
    {
        var wT = simT.World;
        double capT = wT.RailCapacity + wT.ShipCapacity;
        double thrSum = 0; int thrN = 0; double merSav = 0, invSav = 0;
        foreach (var rT in wT.Regions)
        {
            if (rT.State != RegionState.Active) continue;
            thrSum += rT.LastTradeThrottle; thrN++;
            if (rT.MerchantPopIndex >= 0) merSav += rT.Pops[rT.MerchantPopIndex].Savings;
            if (rT.InvestorPopIndex >= 0) invSav += rT.Pops[rT.InvestorPopIndex].Savings;
        }
        System.Console.WriteLine($"{wT.SelfPower?.Name},{wT.TotalPopulation:F0},{wT.Gdp:F0},{wT.RailCapacity:F1},{wT.ShipCapacity:F1},{capT:F1}," +
            $"{(wT.TotalPopulation > 1e-9 ? capT / wT.TotalPopulation : 0):F5},{(wT.Gdp > 1e-9 ? capT / wT.Gdp : 0):F5}," +
            $"{(thrN > 0 ? thrSum / thrN : 1.0):F3},{wT.ColonialShipBurden:F3},{merSav:F1},{invSav:F1},{wT.ShipSteamShareEma:F3}");
    }
    return;
}

// === 診断：軍需の支払能力の断面（S3の設計可否＝afford<1 の国数を見る・2026-07-31）===
if (args.Length > 0 && args[0] == "afforddump")
{
    int qq = ArgInt(args, 1, 200);
    var msA = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    msA.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < qq; q++) msA.StepQuarter();
    System.Console.WriteLine("国名,兵数,人口,兵比率,歳入,GDP,MilitaryScale,estCost,MilitaryBudget,affordRaw,1兵あたり装備費");
    foreach (var simA in msA.Nations)
    {
        var wA = simA.World; var rA = wA.Region;
        double est = EconomyStep.MilitaryProcurementNeed(rA, wA.Coef);   // 装備の必要額＝エンジンの1本（全4段・発見込み。旧＝基本段のみの再導出・2026-09-11 #1）
        double raw = rA.MilitaryAffordRaw;                                 // 支払能力の生の比＝エンジンが書いた値（上限÷必要額・無クランプ。旧＝実予算÷基本段＝上限を読んでいなかった）
        double solA = wA.SoldierSize();
        double perA = solA > 1e-9 ? est / solA : 0;
        System.Console.WriteLine($"{wA.SelfPower?.Name},{solA:F1},{wA.TotalPopulation:F0},{(wA.TotalPopulation > 1e-9 ? solA / wA.TotalPopulation : 0):F5},{wA.LastRevenue:F1},{wA.Gdp:F0},{rA.MilitaryScale:F3},{est:F1},{rA.MilitaryBudget:F1},{raw:F4},{perA:F3}");
    }
    return;
}

// === 診断：開幕時点の各国の軍と財政（案Bの較正用・2026-07-31）===
if (args.Length > 0 && args[0] == "armydump")
{
    var ms0 = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    var co0 = new PriceOfBread.SimCore.Coefficients();
    System.Console.WriteLine("国名,初期兵,定員,GDP,軍事費,GDP当たり必要係数");
    foreach (var sim0 in ms0.Nations)
    {
        var w0 = sim0.World; double sol = w0.SoldierSize();
        double head = PriceOfBread.SimCore.Military.EstablishmentOf(w0, co0, out _);   // ★(155) 定員（旧 (145)＝扶養限界の目標×軍拡の倍率）
        double need = w0.Gdp > 1e-9 && w0.MilitarySpending > 1e-9 ? sol / (w0.MilitarySpending * w0.Gdp) : 0;
        System.Console.WriteLine($"{w0.SelfPower?.Name},{sol:F1},{head:F1},{w0.Gdp:F0},{w0.MilitarySpending:F2},{need:F6}");
    }
    return;
}

// === 診断（2026-08-02）：軍ゲージ Power.Military の年次推移： dotnet run -- powerdump [seed] ===
// 作者の問い「日本14・ネーデルラント1・朝鮮7・オスマン1は本当に正しい？」に答えるための計器。
// ⚠ゲージは兵士数そのものではない＝SoldierSize/SoldiersPerMilitary × 充足率 × 質（上位軍需・結束・軍国・技術）。
//   初期兵（armydump）は英1125・普900・墺650・露500・西500・仏450・オスマン375・日300 で史実整合だが、
//   ゲージは充足率と質で桁が動くので、**どの年でどう開くか**を見ないと妥当性を判断できない。
if (args.Length > 0 && args[0] == "powerdump")
{
    ulong sd = args.Length > 1 && ulong.TryParse(args[1], out var s1) ? s1 : 1789UL;
    var msP = new MultiNationSim(WorldData.Nations(), sd, _ => new UtilityAgent());
    msP.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    string[] watch = ["イギリス", "フランス", "オスマン", "日本", "朝鮮", "ネーデルラント", "プロイセン", "オーストリア", "ロシア", "清", "アメリカ"];
    bool detail = args.Contains("detail");
    string[] deep = ["イギリス", "フランス", "日本", "清", "オスマン"];
    var cD = new PriceOfBread.SimCore.Coefficients();
    Console.WriteLine(detail ? "年,国,ゲージ,兵士,充足率,理想軍隊×充足率,装備質,結束,軍国,技術質" : "年," + string.Join(",", watch));
    for (int y = 0; y <= 131; y++)
    {
        if (y % 10 == 0 || y == 131)
        {
            if (!detail)
            {
                var vals = watch.Select(n =>
                {
                    var p = msP.Diplomacy.Powers.Find(x => x.Name == n || x.BaseName == n);
                    return p is null ? "—" : (p.Military * 100).ToString("F0");
                });
                Console.WriteLine($"{1783 + y}," + string.Join(",", vals));
            }
            else
                foreach (var n in deep)
                {
                    var p = msP.Diplomacy.Powers.Find(x => x.Name == n || x.BaseName == n);
                    if (p?.Nation is not { } nw) { Console.WriteLine($"{1783 + y},{n},—"); continue; }
                    double coh = nw.EthnicEnabled ? 1 + cD.UnityMilitaryQuality * (nw.EthnicUnity - 0.5) : 1.0;
                    double milB = 1.0 + (cD.MilitarismStrengthBonus - 1.0) * PoliticalQuantities.ArmyProfessionalism(nw);   // ★2026-08-18＝軍の練度（親軍法の撤去）
                    double tq = cD.TechMilLowFloor + (1 - cD.TechMilLowFloor) * Math.Clamp(nw.TechLevel / cD.TechMilFullLevel, 0, 1);
                    double eq = 1 + cD.MilitaryQualityBonus * nw.Region.MilitaryQualityFill + cD.MilitaryTier3Bonus * nw.Region.MilitaryTier3Fill;
                    double ideal = nw.SoldierSize() / cD.SoldiersPerMilitary + cD.ArmyShapeGauge * p.FleetGauge;   // 理想軍隊＝兵士POP＋艦隊ゲージ（Diplomacy.cs:754 と同式）
                    Console.WriteLine($"{1783 + y},{n},{p.Military * 100:F0},{nw.SoldierSize():F0},{nw.MilitaryReadiness:F2},{ideal * nw.MilitaryReadiness * 100:F0},{eq:F2},{coh:F2},{milB:F2},{tq:F2}");
                }
        }
        for (int q = 0; q < 4; q++) msP.StepQuarter();
    }
    return;
}

// === 診断：構築直後のチョークポイントの実値（巨大建造物§4-1 のデータ移設が旧挙動と一致するかの検証用）===
if (args.Length > 0 && args[0] == "cpdump")
{
    var d0 = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent()).Diplomacy;
    foreach (var cp in d0.Chokepoints)
        System.Console.WriteLine($"id={cp.Id} name={cp.Name} host={cp.Host} open={cp.IsOpen} canal={cp.IsCanal} buildCost={cp.BuildCost:R} techGate={cp.TechGate:R} basins={string.Join(",", cp.ServesBasins)}");
    return;
}

// === 世界データの外部化：現C#設定から world.json を自動生成（一時dump・挙動同値の移行用）： dotnet run -- dumpworld <出力パス> ===
if (args.Length > 0 && args[0] == "dumpworld")
{
    NationConfig[] all = WorldData.Nations();
    string outPath = args.Length > 1 ? args[1] : "world.json";
    System.IO.File.WriteAllText(outPath, WorldData.Serialize(all), System.Text.Encoding.UTF8);
    System.Console.WriteLine($"world.json を書き出しました（{all.Length}カ国）→ {System.IO.Path.GetFullPath(outPath)}");
    return;
}

// === 財カタログの外部化：現C#定義(BuiltinGoods)から goods.json を自動生成（C#とビット等価）： dotnet run -- dumpgoods <出力パス> ===
// === 参照価格の表示： dotnet run -- refprice [id ...]（省略＝全財）＝コスト中立の入力重みを決めるときに読む（2026-09-05・E-15b） ===
if (args.Length > 0 && args[0] == "refprice")
{
    var ids = new List<int>();
    for (int i = 1; i < args.Length; i++) if (int.TryParse(args[i], out var v)) ids.Add(v);
    if (ids.Count == 0) for (int i = 0; i < Catalog.Goods.Length; i++) ids.Add(i);
    foreach (var id in ids) if (id >= 0 && id < Catalog.Goods.Length) Console.WriteLine($"{id,4} {Catalog.Goods[id].Name,-10} RefPrice {Catalog.RefPrices[id]:F4}");
    return;
}

if (args.Length > 0 && args[0] == "dumpgoods")
{
    string outPath = args.Length > 1 ? args[1] : "goods.json";
    System.IO.File.WriteAllText(outPath, Catalog.Serialize(), System.Text.Encoding.UTF8);
    System.Console.WriteLine($"goods.json を書き出しました（{Catalog.Goods.Length}財）→ {System.IO.Path.GetFullPath(outPath)}");
    return;
}

// === 各国の州名・状態を列挙（地図UIの州名突合用・一時診断）： dotnet run -- regiondump ===
if (args.Length > 0 && args[0] == "regiondump")
{
    var rdm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var n in rdm.Nations)
    {
        var w = n.World;
        System.Console.WriteLine($"=== {w.SelfPower?.Name ?? w.Region.Name} ===");
        foreach (var r in w.Regions) System.Console.WriteLine($"  [{r.State}] 「{r.Name}」 Ind={r.Industrialization:F3}");
        if (w.SelfPower is { } sp && sp.ColonyList.Count > 0)
            System.Console.WriteLine($"  植民地: {string.Join(" / ", sp.ColonyList.ConvertAll(c => c.Name))}");
    }
    return;
}

// === AI工業化バーストの診断： dotnet run -- indprobe [四半期数] ＝ 対象国の首都州の工業化・成長経路（民間/国家）・投資家貯蓄・建設材価格を四半期ごとに出力 ===
if (args.Length > 0 && args[0] == "indprobe")
{
    int ipQuarters = args.Length > 1 && int.TryParse(args[1], out var qn) ? qn : 12;
    // ★2026-08-02：**プレイヤー国を再現できるようにする**（作者報告「プレイヤーの国家の数値が非常に低くなる」）。
    // 実ゲームのプレイヤー国は NullAgent＝AIが一切介入しない（MainWindowViewModel のエージェント生成）。全AIで測ると
    // この非対称が見えないので、`indprobe <四半期> 0 <国名>` で1カ国だけ NullAgent にできる（第2引数＝旧 StateBuildUnitsPerMonth は 2026-09-26 に係数ごと削除＝読まない）。
    string playerName = args.Length > 3 ? args[3] : "";
    var ipm = new MultiNationSim(WorldData.Nations(), 1789UL,
        cfg => playerName.Length > 0 && cfg.Name == playerName ? new NullAgent() : new UtilityAgent());
    string[] targets = ["イギリス", "フランス", "両シチリア", "日本", "プロイセン"];
    var ipc = new Coefficients();
    System.Console.WriteLine($"建設束: 鋼{ipc.SteelGoodId} 機械{ipc.MachineGoodId} セメント{ipc.CementGoodId} 製材{ipc.LumberGoodId}　IndInvestmentSpeed={ipc.IndInvestmentSpeed} IndPerConstruction={ipc.IndPerConstruction} IndInvestmentFrac={ipc.IndInvestmentFrac} IndProductivityScale={ipc.IndProductivityScale} CapitalOutputRatio={ipc.CapitalOutputRatio}");
    void Dump(int q)
    {
        foreach (var n in ipm.Nations)
        {
            var w = n.World;
            string name = w.SelfPower?.BaseName ?? "";
            if (System.Array.IndexOf(targets, name) < 0) continue;
            var r = w.Regions[0];
            double inv = r.InvestorPopIndex >= 0 ? r.Pops[r.InvestorPopIndex].Savings : -1;
            double sP = r.Markets[ipc.SteelGoodId].Price, sS = r.Markets[ipc.SteelGoodId].Stock;
            double mP = r.Markets[ipc.MachineGoodId].Price, mS = r.Markets[ipc.MachineGoodId].Stock;
            System.Console.WriteLine($"Q{q,3} {name,-6} {r.Name,-8} Ind={r.Industrialization:F3} 国家累積={r.CumStateIndustrialization:F3} 投資家貯蓄={inv:F0} 鋼(価格{sP:F2}/在庫{sS:F0}) 機械(価格{mP:F2}/在庫{mS:F0})");
        }
        System.Console.WriteLine();
    }
    Dump(0);
    for (int q = 1; q <= ipQuarters; q++) { ipm.StepQuarter(); if (q <= 4 || q % 2 == 0) Dump(q); }
    return;
}

// === 外交コアの観測（20260814_緊張度をコアにする.PARTIAL.md §12 手順1・2026-08-14）： dotnet run -- diploprobe [四半期=524] ===
// ★sベクトル（8カテゴリ過不足）と全ペアのコア項を**観測のみ**（どの挙動にも配線しない）。
//   検証点＝①1783/1914の敵対上位が史実ペア（英仏・英露・独仏…）を拾うか ②露土・英露（経路型）が内積から出ない見込みの確認（13l）
//   ③⑧金融の正規化（世界中央値比＝13b仮置き）が英↑清↓の向きになるか。
if (args.Length > 0 && args[0] == "diploprobe")
{
    int dpQ = ArgInt(args, 1, 524);
    var dpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in dpm.Nations) nn.TelemetryEnabled = false;
    dpm.Diplomacy.Log.Enabled = false;
    dpm.StartWarmup();
    void DpDump(string label)
    {
        var ns = dpm.Nations.Where(n => n.World.SelfPower is not null).ToList();
        var vec = ns.Select(n => DiploCore.SupplyVector(n.World)).ToList();
        // ★観測v2：⑧はDiploCore側で対外資本ポジションから出す（中央値正規化は撤去）。Pは逓減つき相対（§5-2）
        double maxMil = System.Math.Max(1e-9, ns.Max(n => NationalScore.Military(n.World)));
        Console.WriteLine($"=== {label}：sベクトル（P=軍事力/世界最大） ===");
        Console.WriteLine("  国            P    " + string.Join(" ", DiploCore.CatNames.Select(c => c.PadRight(4))));
        foreach (var (n, v) in ns.Zip(vec))
            Console.WriteLine($"  {n.World.SelfPower!.Name,-12} {NationalScore.Military(n.World) / maxMil,4:F2} " + string.Join(" ", v.Select(x => $"{x,5:F2}")) + $"  金融生値{(n.World.CapitalAbroad - n.World.ForeignCapitalIn) / System.Math.Max(1.0, n.World.NominalGdp),7:F3}");
        // 全ペアのコア項（交易到達＝首都間距離の仮関数13i）
        var pairs = new List<(string a, string b, double core, double comp, double reach)>();
        for (int i = 0; i < ns.Count; i++)
            for (int j = i + 1; j < ns.Count; j++)
            {
                var wa = ns[i].World; var wb = ns[j].World;
                double km = Geo.DistanceKm(wa.Latitude, wa.Longitude, wb.Latitude, wb.Longitude);
                double reach = DiploCore.ReachInterim(km);
                double core = DiploCore.CoreTerm(vec[i], vec[j], NationalScore.Military(wa) / maxMil, NationalScore.Military(wb) / maxMil, reach);
                pairs.Add((wa.SelfPower!.Name, wb.SelfPower!.Name, core, DiploCore.Complement(vec[i], vec[j]), reach));
            }
        Console.WriteLine("  --- 敵対上位15（コア項が負） ---");
        foreach (var pr in pairs.OrderBy(x => x.core).Take(15))
            Console.WriteLine($"    {pr.a}×{pr.b}  コア項{pr.core,7:F3}  補完度{pr.comp,7:F3}  到達{pr.reach:F2}");
        Console.WriteLine("  --- 協力上位15（コア項が正） ---");
        foreach (var pr in pairs.OrderByDescending(x => x.core).Take(15))
            Console.WriteLine($"    {pr.a}×{pr.b}  コア項{pr.core,7:F3}  補完度{pr.comp,7:F3}  到達{pr.reach:F2}");
        // 13l の検証点＝経路型の対立が内積から出ない見込みの名指しペア
        string[][] watch = [["ロシア", "オスマン"], ["イギリス", "ロシア"], ["イギリス", "プロイセン"], ["イギリス", "清"], ["フランス", "ロシア"], ["イギリス", "アメリカ"], ["フランス", "プロイセン"]];
        // ★地政学項の観測（13l・作者裁定2026-08-14「無理に経済で完結させず地政学をキャッチアップする」）：
        //   経路依存(a,b)＝aの海盆プレゼンスのうち、bが扼する咽喉が出口になっている割合。
        //   閉鎖海盆→出口の対応は地理の定数（黒海→ボスポラス・バルト→エーレスンド・紅海→バブエルマンデブ・地中海→ジブラルタル）。
        //   ⚠国名は書かない＝「ロシア×オスマン」は 黒海プレゼンス×海峡の扼者 から出る。
        {
            var gate = new (Basin basin, string chokeId)[] {
                (Basin.BlackSea, "bosporus"), (Basin.Baltic, "oresund"), (Basin.RedSea, "bab_el_mandeb"), (Basin.Mediterranean, "gibraltar") };
            var chokeOwner = new Dictionary<string, Power?>();
            foreach (var cp in dpm.Diplomacy.Chokepoints) chokeOwner[cp.Id] = cp.Owner;
            var deps = new List<(string a, string b, double dep)>();
            foreach (var n1 in ns)
            {
                var pa = n1.World.SelfPower!;
                foreach (var (basin, chokeId) in gate)
                {
                    if (!chokeOwner.TryGetValue(chokeId, out var owner) || owner is null || owner == pa) continue;
                    double presence = pa.BasinPresence[(int)basin];
                    if (presence > 0.01) deps.Add((pa.Name, owner.Name, presence));
                }
            }
            Console.WriteLine("  --- 地政学項（経路依存＝閉鎖海盆のプレゼンス×出口の扼者・上位12） ---");
            foreach (var d in deps.OrderByDescending(x => x.dep).Take(12))
                Console.WriteLine($"    {d.a} → {d.b}  依存 {d.dep:F2}");
            // 診断①：咽喉の扼者一覧（ポーランド扼ジブラルタル等の妥当性検証・Q0で表が空になる原因の特定）
            Console.WriteLine("  --- 咽喉の扼者（Id / 扼者 / 開通） ---");
            foreach (var cp in dpm.Diplomacy.Chokepoints)
                Console.WriteLine($"    {cp.Id,-14} {cp.Owner?.Name ?? "（未解決）",-10} {(cp.IsOpen ? "開" : "閉")}");
            // 診断②：主要国の海盆プレゼンス（L1正規化＝交易シェア。露の黒海・ペルシャの地中海0.98の出所を見る）
            Console.WriteLine("  --- 海盆プレゼンス（主要国） ---");
            foreach (var n1 in ns)
            {
                var pa = n1.World.SelfPower!;
                var bn = pa.BaseName.Length > 0 ? pa.BaseName : pa.Name;
                if (bn is not ("ロシア" or "イギリス" or "オスマン" or "ペルシャ" or "ポーランド" or "プロイセン")) continue;
                Console.WriteLine($"    {pa.Name,-10} " + string.Join(" ", Enumerable.Range(0, Basins.Count).Select(b => $"{(Basin)b}={pa.BasinPresence[b]:F2}")));
            }
        }
        // 診断③：財次元平坦の切り分け＝交易の生値（貿易/GDP比が実際に痩せているのか、器の目盛りの問題か）
        Console.WriteLine("  --- 交易生値（英・露） ---");
        foreach (var n1 in ns)
        {
            var w = n1.World; var pa = w.SelfPower!;
            var bn = pa.BaseName.Length > 0 ? pa.BaseName : pa.Name;
            if (bn is not ("イギリス" or "ロシア")) continue;
            var eg = w.ExportsByGood; var igg = w.ImportsByGood;
            if (eg is null || igg is null) { Console.WriteLine($"    {pa.Name}: 集計なし"); continue; }
            double ex = eg.Sum(); double im = igg.Sum();
            Console.WriteLine($"    {pa.Name,-10} 輸出{ex,12:F0} 輸入{im,12:F0}  年換算(輸出+輸入)/GDP {TradeGdp(w):P1}");
            foreach (var g in Enumerable.Range(0, System.Math.Min(eg.Length, igg.Length)).OrderByDescending(g => eg[g] + igg[g]).Take(6))
                Console.WriteLine($"      {Catalog.Goods[g].Name,-10} 輸出{eg[g],10:F0} 輸入{igg[g],10:F0}");
        }
        // 名指しペア＝BaseName照合（変名しても追える。オスマン→ルーム変名で1914行が消えていた）
        Console.WriteLine("  --- 名指しペア（13l検証：露土・英露は経路型＝低い/ゼロの見込み） ---");
        foreach (var wp in watch)
        {
            int ia = ns.FindIndex(n => { var p = n.World.SelfPower!; return (p.BaseName.Length > 0 ? p.BaseName : p.Name).Contains(wp[0]); });
            int ib = ns.FindIndex(n => { var p = n.World.SelfPower!; return (p.BaseName.Length > 0 ? p.BaseName : p.Name).Contains(wp[1]); });
            if (ia < 0 || ib < 0 || ia == ib) continue;
            var wa = ns[ia].World; var wb = ns[ib].World;
            double km2 = Geo.DistanceKm(wa.Latitude, wa.Longitude, wb.Latitude, wb.Longitude);
            double core2 = DiploCore.CoreTerm(vec[ia], vec[ib], NationalScore.Military(wa) / maxMil, NationalScore.Military(wb) / maxMil, DiploCore.ReachInterim(km2));
            Console.WriteLine($"    {wa.SelfPower!.Name}×{wb.SelfPower!.Name}  コア項{core2,7:F3}  補完度{DiploCore.Complement(vec[ia], vec[ib]),7:F3}");
        }
    }
    DpDump("1783（助走明けQ0）");
    for (int q = 1; q <= dpQ; q++) dpm.StepQuarter();
    DpDump($"{1783 + dpQ / 4}（Q{dpQ}）");
    return;
}

// 侵略/植民地化に要る「余剰戦力」の標的依存を測る（作者指摘2026-08-23「どの国も攻め込む余剰戦力が同じ」）。
//   出力＝行為者ごとに、全 Minor 標的の要求量（上限比）と、その内訳（標的の軍事力・投射＝距離×制海権・実効戦力・屈服線）。
//   末尾に要求量の最小/最大/幅を出す＝「標的が変わっても同じ」かどうかを1行で読む。
if (args.Length > 0 && args[0] == "willprobe")
{
    int wpQ = ArgInt(args, 1, 200);
    var wpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in wpm.Nations) nn.TelemetryEnabled = false;
    wpm.Diplomacy.Log.Enabled = false;
    wpm.StartWarmup();
    for (int q = 0; q < wpQ; q++) wpm.StepQuarter();
    var dipw = wpm.Diplomacy;
    Console.WriteLine($"=== 余剰戦力の要求（Q{wpQ}＝{1783 + wpQ / 4}年） ===");
    foreach (var n in wpm.Nations)
    {
        var w = n.World; var me = w.SelfPower; if (me is null) continue;
        var bn = me.BaseName.Length > 0 ? me.BaseName : me.Name;
        if (bn is not ("イギリス" or "フランス" or "プロイセン" or "アメリカ" or "日本" or "ロシア")) continue;
        double cap = System.Math.Max(1e-9, w.Coef.ExpansionWillCap);
        Console.WriteLine($"--- {me.Name}（{dipw.RankOf(me)}・軍事力{me.Military:F2}・陸軍力{me.LandPower:F2}・保有余剰戦力{w.ExpansionWill / cap:P0}） ---");
        Console.WriteLine("    標的            種別   軍事力   投射  実効戦力  屈服線   国力比   要求  可否");
        var reqPct = new List<double>(); int okCount = 0;
        foreach (var t in dipw.Powers)
        {
            if (t == me || dipw.RankOf(t) != PowerRank.Minor) continue;
            bool abst = t.Nation is null;
            double proj = dipw.Projection(me, t, w.Coef);
            double force = dipw.ColonizeForce(me, t, w.Coef);
            double need = dipw.SubmitLine(t, w.Coef);   // エンジンと同じ1本（床つき）＝計器が別の線を描かない
            double adv = force / System.Math.Max(1e-9, need);
            double req = w.ExpansionCostFor(t) / cap;                    // ゲート・実消費・UI表示と同一
            double reqDip = dipw.ColonizeWillCost(me, t, w.Coef) / cap;  // 1本化の検証＝この2つは必ず一致する
            bool can = w.CanInvade(t, out string why);
            if (can) okCount++;
            reqPct.Add(req);
            Console.WriteLine($"    {JpPad(t.Name, 14)} {(abst ? "抽象" : "実在")}  {t.Military,6:F2} {proj,6:F3} {force,9:F3} {need,7:F3} {adv,7:F2} {req,6:P0}  {(can ? "可" : why)}{(System.Math.Abs(req - reqDip) > 1e-9 ? "  ⚠式が不一致" : "")}");
        }
        if (reqPct.Count > 0)
            Console.WriteLine($"    ＝要求余剰戦力 最小{reqPct.Min():P0} 最大{reqPct.Max():P0} 幅{reqPct.Max() - reqPct.Min():P0} 中央値{reqPct.OrderBy(x => x).ElementAt(reqPct.Count / 2):P0}（標的{reqPct.Count}件・今すぐ可能{okCount}件）");
    }
    return;
}

if (args.Length > 0 && args[0] == "threatprobe")
{
    // ★安全保障の不安（(155) 2026-10-02 作り直し）の読み手＝エンジンが四半期末に書いた値を読むだけ（旧＝旧の式〔州の質量×距離の最大の和〕の写し＝エンジンと食い違う計器だった）。
    //   列＝受ける脅威（相手の養える軍のうち自国に向く注意配分の和・兵の頭数）・自国の養える軍の人口の側・脅威の割合（AI の軍事費の段が読む・徴兵法の目標は (178) から陸軍の不安〔本国の国境＋投射×上陸の門〕の割合）・AI の段の目標・軍事費の段・兵・定員。
    //   使い方：threatprobe [四半期=524] [間隔=160]
    int thQ = ArgInt(args, 1, 524);
    int thEvery = ArgInt(args, 2, 160);
    var thm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in thm.Nations) nn.TelemetryEnabled = false;
    thm.Diplomacy.Log.Enabled = false;
    thm.StartWarmup();
    void ThSnap(int q)
    {
        Console.WriteLine($"\n=== Q{q}（{thm.Year}年）国	受ける脅威	自国の養える軍	脅威の割合	段の目標	軍事費の段	兵	定員 ===");
        var rows = new List<(string name, double t, double own, double x, double target, double stage, double sold, double seats)>();
        foreach (var nn in thm.Nations)
        {
            var w = nn.World;
            double seats = Military.EstablishmentOf(w, w.Coef, out _);
            rows.Add((w.SelfPower?.Name ?? "?", w.SecurityThreat, w.SecurityThreatOwnMass, UtilityAgent.SecurityShare(w), UtilityAgent.SecurityStageTarget(w), w.MilitarySpending, w.SoldierSize(), seats));
        }
        foreach (var r in rows.OrderByDescending(x => x.x))
            Console.WriteLine($"  {r.name}	{r.t:F1}	{r.own:F1}	{r.x:F3}	{r.target:F3}	{r.stage:F2}	{r.sold:F1}	{r.seats:F1}");
    }
    for (int q = 1; q <= thQ; q++)
    {
        thm.StepQuarter();
        if (q % thEvery == 0 || q == thQ) ThSnap(q);
    }
    return;
}

if (args.Length > 0 && args[0] == "play-replay") { PriceOfBread.Runner.PlayHarness.Replay(args); return; }   // ReplayAgent（2026-08-31）＝decision.raw.txt からの決定論再生
if (args.Length > 0 && args[0] == "play")
{
    PriceOfBread.Runner.PlayHarness.Run(args);
    return;
}

// ★D-34 機構発火の点検（2026-09-03・トリアージ 10-1a）： dotnet run -- firecheck [四半期=524]
//   ⚠狙いは「**ゲート緑≠機構が働いた**」の自動検出。何もしない機構は何も壊さないのでテストは必ず緑になる。
//   ★Eval=0（呼ばれてすらいない＝配線漏れ）と Hit=0（呼ばれて一度も成立しない＝死んだ機構）は**別の病**なので分けて出す。
// ★D-35 後半 摂動走行（2026-09-03）： dotnet run -- deadprobe [四半期=8] [振る値=1.0]
//   ⚠狙い＝「**点火しても何も動かない係数**」を機械的に洗い出す。今日の実測（反帝世論が20,283回呼ばれて成立0・
//   植民地の独立が試行0・革命の連合が11,054回中0）は全部**人が読んで**見つけた。83本を手で読むのは現実的でない。
//   ★方法＝既定0の係数を1本ずつ非ゼロへ振り、**世界が1ビットでも動くか**だけを見る（短距離・全国）。
//   ⚠出力は「死蔵」と断定しない＝**「動かない候補」**と名乗る。他のフラグに封じられているだけの器を
//   死蔵と誤判定しないため（実例＝RecruitCostPerHead=0 と ModernizationToRecruit=0 の二重の休眠）。
// ★自給自足プローブ（2026-09-04・作者の問い「貿易はシステムの穴ではなく、貿易必要性が低いという認識でよいか」）：
//   dotnet run -- selfsuff [四半期=200]　★各州が**何種類の財を実際に作っているか**を数える。
//   ⚠仮説＝「みんな自分で作れてしまうから交換しない」。ほぼ全種類なら自給自足が確定、少数なら別の原因。
//   ⚠budgetprobe の「需要Σ/生産Σ」は同じプローブの貿易/GDPに4倍のバグがあった（2026-09-03是正）ので、
//   その数字に乗る前に**別の物差しで**確かめる（計器は自分の限界を申告する＝この日の教訓）。
// ★採算分布プローブ（2026-09-04・作者の仮説「過剰生産が罰せられない世界になっている」）：
//   dotnet run -- marginprobe [四半期=200]　★稼働中の生産者を**平滑採算（SmoothedMargin）の帯**で数える。
//   ⚠倒産は −85% 未満・復活は −5% 超＝**その間（慢性赤字だが死なない帯）に何割居るか**が仮説の検証そのもの。
if (args.Length > 0 && args[0] == "marginprobe")
{
    int mpQ2 = ArgInt(args, 1, 200);
    var mpm2 = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in mpm2.Nations) nn.TelemetryEnabled = false;
    mpm2.Diplomacy.Log.Enabled = false;
    mpm2.StartWarmup();
    for (int q = 0; q < mpQ2; q++) mpm2.StepQuarter();
    var c2 = mpm2.Coef;
    double[] edges = [double.NegativeInfinity, c2.FactoryBankruptcyMargin, -0.5, -0.2, c2.FactoryReentryMargin, 0.0, 0.2, double.PositiveInfinity];
    string[] labels = ["倒産圏(<−85%)", "−85〜−50%", "−50〜−20%", "−20〜−5%", "−5〜0%", "0〜+20%", ">+20%"];
    var cnt = new long[labels.Length]; var outSum = new double[labels.Length]; long bankrupt = 0, active = 0;
    foreach (var n2 in mpm2.Nations) foreach (var r in n2.World.Regions)
    {
        if (r.State != RegionState.Active) continue;
        foreach (var pr in r.Producers)
        {
            var g = r.GoodOf(pr.GoodId); if (g is null || g.Tier == GoodTier.Service) continue;
            if (pr.Bankrupt) { bankrupt++; continue; }
            if (!(pr.Output > 1e-9)) continue;
            active++;
            double m = pr.SmoothedMargin;
            for (int b = 0; b < labels.Length; b++) if (m >= edges[b] && m < edges[b + 1]) { cnt[b]++; outSum[b] += pr.Output * g.RefPrice; break; }
        }
    }
    double outTot = 0; foreach (var v in outSum) outTot += v;
    Console.WriteLine($"=== 採算分布（seed 1789・{mpQ2}四半期＝{1783 + mpQ2 / 4}年）稼働 {active:N0}・倒産中 {bankrupt:N0} ===");
    Console.WriteLine($"{"帯",-16}{"生産者数",12}{"割合",9}{"産出額シェア",14}");
    for (int b = 0; b < labels.Length; b++)
        Console.WriteLine($"{labels[b],-16}{cnt[b],12:N0}{(active > 0 ? (double)cnt[b] / active : 0),9:P1}{(outTot > 0 ? outSum[b] / outTot : 0),14:P1}");
    Console.WriteLine("★「−85〜−5%」の3帯が**慢性赤字だが死なない帯**。ここの産出額シェアが大きいほど、過剰生産が罰せられていない。");
    return;
}

if (args.Length > 0 && args[0] == "selfsuff")
{
    int ssQ = ArgInt(args, 1, 200);
    var ssm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in ssm.Nations) nn.TelemetryEnabled = false;
    ssm.Diplomacy.Log.Enabled = false;
    ssm.StartWarmup();
    for (int q = 0; q < ssQ; q++) ssm.StepQuarter();
    int tradeable = 0;
    foreach (var g in Catalog.Goods) if (!g.NonTradeable && g.Tier != GoodTier.Service) tradeable++;
    var counts = new List<int>(); var names = new List<(string, int, double)>();
    foreach (var n2 in ssm.Nations)
        foreach (var r in n2.World.Regions)
        {
            if (r.State != RegionState.Active) continue;
            int made = 0; double selfMet = 0, need = 0;
            foreach (var g in r.Goods)
            {
                if (g.NonTradeable || g.Tier == GoodTier.Service) continue;
                var mk = r.Markets[g.Id];
                if (mk.LastProduced > 1e-9) made++;
                if (mk.LastDemand > 1e-9) { need += mk.LastDemand; selfMet += System.Math.Min(mk.LastDemand, mk.LastProduced); }
            }
            counts.Add(made);
            names.Add((n2.World.SelfPower?.Name + "/" + r.Name, made, need > 1e-9 ? selfMet / need : 1.0));
        }
    counts.Sort();
    Console.WriteLine($"=== 自給自足プローブ（seed 1789・{ssQ}四半期＝{1783 + ssQ / 4}年・州 {counts.Count}）===");
    Console.WriteLine($"⚠貿易可能な財は {tradeable} 種類。各州が実際に作っている種類数の分布：");
    Console.WriteLine($"    最小 {counts[0]}　下位25% {counts[counts.Count / 4]}　中央 {counts[counts.Count / 2]}　上位25% {counts[counts.Count * 3 / 4]}　最大 {counts[^1]}");
    double selfAvg = 0; foreach (var t in names) selfAvg += t.Item3;
    Console.WriteLine($"★自給率（需要のうち自州の生産で賄えた割合）の平均＝{selfAvg / System.Math.Max(1, names.Count):P1}");
    Console.WriteLine("  ⚠自給率が高い＝交換する理由が無い。★低いのに貿易が細いなら、原因は必要性でなく仕組みの側。");
    names.Sort((a, b) => b.Item2.CompareTo(a.Item2));
    Console.WriteLine("  作っている種類が多い州の上位8：");
    for (int i2 = 0; i2 < 8 && i2 < names.Count; i2++) Console.WriteLine($"    {names[i2].Item1}  {names[i2].Item2}種類　自給率{names[i2].Item3:P0}");
    // ★2026-09-04（E-13の採用判定用）：硫黄鎖の世界産出＝地理ゲートで「産地集中→運べない→下流が消える」（2026-08-13の差し戻し理由）が再発していないかを直接見る。
    {
        string[] watch = { "硫黄", "化学薬品", "火薬", "硝石", "肥料", "マッチ", "染料", "硫酸" };
        Console.WriteLine("★硫黄鎖の世界産出（活動州の LastProduced 合計・作っている州数・平均価格/RefPrice）:");
        foreach (var wn in watch)
        {
            int gid = -1;
            for (int g = 0; g < Catalog.Goods.Length; g++) if (Catalog.Goods[g].Name == wn) { gid = g; break; }
            if (gid < 0) continue;
            double prod = 0, pxSum = 0, dem = 0, stock = 0, sold = 0, pxMed = 0; int nProd = 0, nPx = 0, nHigh = 0;
            var pxList = new List<double>();
            foreach (var n2 in ssm.Nations)
                foreach (var r2 in n2.World.Regions)
                {
                    if (r2.State != RegionState.Active || gid >= r2.Markets.Length) continue;
                    double rp = 0;
                    foreach (var gg in r2.Goods) if (gg.Id == gid) { rp = gg.RefPrice; break; }   // RefPrice は州の Good（Catalog の GoodDef には無い）
                    var mk = r2.Markets[gid];
                    if (mk.LastProduced > 1e-9) { prod += mk.LastProduced; nProd++; }
                    dem += mk.LastDemand; stock += mk.Stock; sold += mk.LastSold;
                    if (rp > 1e-9 && mk.Price > 0) { double q = mk.Price / rp; pxSum += q; nPx++; pxList.Add(q); if (q > 5) nHigh++; }
                }
            if (pxList.Count > 0) { pxList.Sort(); pxMed = pxList[pxList.Count / 2]; }
            // ★2026-09-06（E-13 の価格高止まりの診断）：需要・在庫・販売の世界合計と、価格/Ref の中央値・5倍超の州数（平均が少数の外れ値に引かれていないか）
            Console.WriteLine($"    {wn,-6} 産出 {prod,12:F0}　生産州 {nProd,4}　価格/Ref 平均{(nPx > 0 ? pxSum / nPx : 0),6:F2} 中央{pxMed,6:F2} 5倍超{nHigh,3}州　需要 {dem,10:F0}　販売 {sold,10:F0}　在庫 {stock,10:F0}");
        }
        // ★E-15 受け入れ用：化学薬品(37)のレシピ別の産出＝黄鉄鉱法が実際に動いているか（生産者の LastOutput×Scale 相当は持たないので Output×Scale×Productivity の近似でなく、Producer.Output の合計＝直近の産出量）。
        {
            var byRecipe = new Dictionary<string, (double Out, int N, int Active)>();
            foreach (var n2 in ssm.Nations)
                foreach (var r2 in n2.World.Regions)
                {
                    if (r2.State != RegionState.Active) continue;
                    foreach (var pr in r2.Producers)
                    {
                        if (pr.GoodId != Catalog.ChemicalsId) continue;
                        string key = pr.RecipeName.Length > 0 ? pr.RecipeName : "既定（ルブラン法）";
                        var cur = byRecipe.TryGetValue(key, out var v) ? v : (0, 0, 0);
                        byRecipe[key] = (cur.Out + pr.Output * pr.Scale, cur.N + 1, cur.Active + (pr.Output * pr.Scale > 1e-9 && !pr.Bankrupt ? 1 : 0));
                    }
                }
            Console.WriteLine("★化学薬品(37)のレシピ別（Output×Scale の合計・生産者数・稼働数）:");
            foreach (var kv in byRecipe) Console.WriteLine($"    {kv.Key,-12} {kv.Value.Out,12:F0}　生産者 {kv.Value.N,4}　稼働 {kv.Value.Active,4}");
        }
    }
    return;
}

if (args.Length > 0 && args[0] == "deadprobe")
{
    int dpQ = ArgInt(args, 1, 8);
    double dpVal = args.Length > 2 && double.TryParse(args[2], out var dv) ? dv : 1.0;
    static (double Money, double Pop, double Gdp, double Leg) Digest(MultiNationSim m)
    {
        double a = 0, b = 0, g = 0, l = 0;
        foreach (var n in m.Nations) { var w = n.World; a += w.BaseMoney; b += w.TotalPopulation; g += w.Gdp; l += w.Legitimacy; }
        return (a, b, g, l);
    }
    static (double, double, double, double) RunOnce(int q, string? field, double val)
    {
        var m = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
        foreach (var n in m.Nations) n.TelemetryEnabled = false;
        m.Diplomacy.Log.Enabled = false;
        if (field is not null)
        {
            var f = typeof(Coefficients).GetField(field);
            if (f is not null)
            {
                foreach (var n in m.Nations) f.SetValue(n.World.Coef, val);
                f.SetValue(m.Coef, val);   // ⚠共有係数も書く（二国間交易・外交はこちらを読む＝2026-09-03の罠）
            }
        }
        m.StartWarmup();
        for (int i = 0; i < q; i++) m.StepQuarter();
        return Digest(m);
    }
    var probe = new Coefficients();
    var names = new List<string>();
    // ⚠第4引数＝カンマ区切りの名前で絞り込む。★初回（8四半期）の反省＝**射程が足りないと「事件が起きていない」を
    //   「読み手がいない」と誤読する**（分離・危機・ゴールドラッシュ・ファシズムは2年では起きない）。
    //   ⇒ 短距離で「動いた」ものは生存確定、動かなかったものだけを**長距離で測り直す**という2段構えにする。
    var only = args.Length > 3 && args[3].Length > 0
        ? new System.Collections.Generic.HashSet<string>(args[3].Split(',', System.StringSplitOptions.RemoveEmptyEntries))
        : null;
    foreach (var f in typeof(Coefficients).GetFields())
    {
        if (!f.IsPublic || f.IsStatic || f.IsInitOnly) continue;
        if (only is not null && !only.Contains(f.Name)) continue;
        if (f.GetValue(probe) is double d0 && d0 == 0.0) names.Add(f.Name);
    }
    names.Sort(System.StringComparer.Ordinal);
    var baseline = RunOnce(dpQ, null, 0);
    Console.WriteLine($"=== 摂動走行（D-35後半・seed 1789・{dpQ}四半期・振る値 {dpVal}） ===");
    Console.WriteLine($"⚠「動かない候補」＝点火しても世界が1ビットも動かない係数。★死蔵と断定はしない。");
    Console.WriteLine($"⚠この計器の限界は3つ（2026-09-03 に全部踏んだ）：");
    Console.WriteLine($"  ①**構築時に読む係数は判定できない**＝シムを作ってから書き換えるので届かない（例：CenterIsFirstProvince・ResearcherFrac）。外から POB_COEF で1本ずつ起動すれば測れる。");
    Console.WriteLine($"  ②**二重休眠**＝別のフラグが0で封じている係数は単独で振っても動かない（例：AbsorptionIncomeRef と Weight は両方>0が条件）。");
    Console.WriteLine($"  ③**射程不足**＝その事件が起きない距離だと動かない（例：ファシズムは1919年以降・国内危機は100年で19回）。");
    Console.WriteLine($"既定0の係数 {names.Count} 本を1本ずつ検査する。");
    var dead = new List<string>();
    foreach (var nm in names)
    {
        var r = RunOnce(dpQ, nm, dpVal);
        bool same = r.Item1 == baseline.Item1 && r.Item2 == baseline.Item2 && r.Item3 == baseline.Item3 && r.Item4 == baseline.Item4;
        if (same) dead.Add(nm);
    }
    Console.WriteLine();
    Console.WriteLine($"★動いた: {names.Count - dead.Count} 本 ／ ⚠動かない候補: {dead.Count} 本");
    foreach (var nm in dead) Console.WriteLine($"    ⚠{nm}");
    return;
}

// === 戦死の計器： dotnet run -- casprobe [四半期=GameQuarters] [seed=1789] ===
if (args.Length > 0 && args[0] == "casprobe")
{
    // ★戦死の計器（測定回 2026-09-24・再較正①・クラウドの cloud-wip/20260924 を査読して是正のうえ取り込み 2026-09-25）。
    //   観測だけ（Diplomacy.CasualtyProbe＝この世界のインスタンスのフック・既定 null）＝世界を1ビットも変えない。
    //   ①10年ごとの兵士の戦死（士官を除く）②兵士POPあたりの月戦死率 ③上限が効く月の割合 ④上限が効き始める Rate の分位。
    //   ⚠人口の内部単位は千人＝戦死・兵士・人口は千人のまま小数で出す（整数へ丸めると月に数十人の戦死が 0 に消える）。
    //   ⚠戦争の長さはここで測らない＝trenchprobe（EndedWars の終戦月数・時代区分）が持つ。
    int cpQ = ArgInt(args, 1, Coefficients.GameQuarters);
    ulong cpSeed = ArgUlong(args, 2, 1789);
    var cpm = new MultiNationSim(WorldData.Nations(), cpSeed, _ => new UtilityAgent());
    foreach (var nn in cpm.Nations) nn.TelemetryEnabled = false;
    cpm.Diplomacy.Log.Enabled = false;
    cpm.StartWarmup();
    double cpRate = cpm.Coef.WarCasualtyRate, cpCapShare = cpm.Coef.WarCasualtyMonthlyCap;   // Diplomacy.Step が実際に読む共有の係数
    var dec = new SortedDictionary<int, double[]>();   // 年代 → [戦死, 兵士POP月和, 交戦国月数, 上限張り付き月数, 世界人口（年代の最後の四半期）]
    var rStar = new List<double>();                     // 国×月ごとの「上限が効き始める Rate」＝ Rate×上限/上限前
    int cpYear = cpm.Nations[0].World.Year;
    double[] Dec(int y) { int d = y / 10 * 10; if (!dec.TryGetValue(d, out var a)) dec[d] = a = new double[5]; return a; }
    cpm.Diplomacy.CasualtyProbe = (force, soldiers) =>
    {
        double cap = soldiers * cpCapShare;   // WarCasualties の上限（兵士の WarCasualtyMonthlyCap/月）
        var a = Dec(cpYear);
        a[0] += Math.Min(force, cap); a[1] += soldiers; a[2] += 1; if (force >= cap) a[3] += 1;
        rStar.Add(force > 1e-12 ? cpRate * cap / force : double.PositiveInfinity);
    };
    for (int q = 0; q < cpQ; q++)
    {
        cpYear = cpm.Nations[0].World.Year;   // この四半期の暦年（StepQuarter が年を送る前に読む）
        cpm.StepQuarter();
        Dec(cpYear)[4] = cpm.Nations.Sum(nn => nn.World.TotalPopulation);   // 毎四半期上書き＝年代の最後の四半期の値が残る
    }
    Console.WriteLine($"=== 戦死の計器（seed {cpSeed}・{cpQ}四半期・WarCasualtyRate={cpRate:G4}・上限＝兵士の{cpCapShare:P1}/月）人数の単位は千人 ===");
    Console.WriteLine($"{"年代",-6}{"兵士の戦死",14}{"交戦国月",10}{"月戦死率",12}{"上限張付",10}{"世界人口",16}");
    foreach (var (d, a) in dec)
        Console.WriteLine($"{d,-6}{a[0],14:F2}{a[2],10:N0}{(a[1] > 0 ? a[0] / a[1] : 0),12:G4}{(a[2] > 0 ? a[3] / a[2] : 0),10:P1}{a[4],16:N0}");
    double tc = dec.Values.Sum(a => a[0]), ts = dec.Values.Sum(a => a[1]), tm = dec.Values.Sum(a => a[2]), tk = dec.Values.Sum(a => a[3]);
    Console.WriteLine($"{"計",-6}{tc,14:F2}{tm,10:N0}{(ts > 0 ? tc / ts : 0),12:G4}{(tm > 0 ? tk / tm : 0),10:P1}");
    rStar.Sort();
    double Pct(double f) => rStar.Count == 0 ? double.NaN : rStar[Math.Min(rStar.Count - 1, (int)(f * rStar.Count))];
    Console.WriteLine($"上限が効き始める Rate の分位（交戦国月 {rStar.Count:N0}）：p10={Pct(0.1):G4} p25={Pct(0.25):G4} p50={Pct(0.5):G4} p75={Pct(0.75):G4} p90={Pct(0.9):G4}");
    for (int k = 0; k <= 8; k++)   // 現行 Rate の 2^k 倍（×1〜×256）で何割の月が上限に張り付くか（静的な見積もり・軌道は据え置き）
    {
        double r = cpRate * (1 << k);
        Console.WriteLine($"  Rate={r,-6:G4}（現行×{1 << k}）なら上限張り付き＝{(rStar.Count > 0 ? rStar.Count(x => x <= r) / (double)rStar.Count : 0):P1}");
    }
    return;
}

if (args.Length > 0 && args[0] == "firecheck")
{
    int fcQ = ArgInt(args, 1, 524);
    MechanismProbe.Reset();
    MechanismProbe.Enabled = true;
    var fcm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in fcm.Nations) nn.TelemetryEnabled = false;
    fcm.Diplomacy.Log.Enabled = false;
    fcm.StartWarmup();
    // ⚠権力連合（PowerAffinity.Coalition）は **Standings 生成時にしか呼ばれない**＝StepQuarter だけ回すと一度も走らない。
    //   初回走行で「革命は誰とも組めない」と「そもそも連合が計算されていない」を取り違えかけた＝**計器の隔離**の罠。
    //   ★毎期呼んで捨てる（導出政体は報告専用＝世界を1ビットも変えない）。
    for (int q = 1; q <= fcQ; q++)
    {
        fcm.StepQuarter();
        foreach (var nn in fcm.Nations) PowerAffinity.PolityFromShares(nn.World);
    }
    MechanismProbe.Enabled = false;
    Console.WriteLine($"=== 機構発火（D-34・seed 1789・{fcQ}四半期＝{1783 + fcQ / 4}年まで） ===");
    Console.WriteLine("⚠試行0＝配線漏れ（呼ばれてすらいない）／成立0＝死んだ機構（呼ばれて一度も成立しない）");
    Console.WriteLine($"{"機構",-26}{"試行",14}{"成立",14}  所見");
    for (int s = 0; s < (int)MechanismProbe.Site.Count; s++)
    {
        var site = (MechanismProbe.Site)s;
        var (ev, hi) = MechanismProbe.Read(site);
        string note = ev == 0 ? "★配線漏れ＝一度も評価されていない" : hi == 0 ? "★死んだ機構＝一度も成立しない" : $"成立率 {(double)hi / ev:P3}";
        Console.WriteLine($"{site,-26}{ev,14:N0}{hi,14:N0}  {note}");
    }
    return;
}

if (args.Length > 0 && args[0] == "monoprobe")
{
    // S0計器（貿易数量則書き換え設計 §6・2026-08-30）：モノカルチャー/主食輸入/輸出州価格の断面。
    //   monoprobe [四半期=524] [間隔=40]　POB_COEF で腕を切り替えて比較する（S1/S2/S3/S4 のゲート判定用）。
    int mpQ = ArgInt(args, 1, 524);
    int mpEvery = ArgInt(args, 2, 40);
    var mpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in mpm.Nations) nn.TelemetryEnabled = false;
    mpm.Diplomacy.Log.Enabled = false;
    mpm.StartWarmup();
    var mpc = new Coefficients();
    void MpSnap(int q)
    {
        Console.WriteLine($"\n=== Q{q}（{1783 + q / 4}年）===");
        // ① 輸出特化の上位（州,財）＝ LastExport/max(LastProduced,ε)。モノカルチャー成立の直接指標
        var spec = new List<(string nat, string reg, string good, double ratio, double exp)>();
        double wTrade = 0, wGdp = 0;
        int expStates = 0; double lpSum = 0, rpSum = 0;
        var stapleDep = new List<(string nat, double dep)>();
        foreach (var sim in mpm.Nations)
        {
            var w = sim.World;
            if (w.ExportsByGood is { } eg2 && w.ImportsByGood is { } ig2) wTrade += (eg2.Sum() + ig2.Sum()) * mpc.ClearingsPerYear;
            wGdp += System.Math.Max(0, w.NominalGdp) * Calendar.MonthsPerYear;   // NominalGdpは月次＝年換算は×12（旧×4は四半期扱いの誤り）
            double sImp = 0, sDem = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var g in r.Goods)
                {
                    var mk = r.Markets[g.Id];
                    if (mk.LastExport > 1e-9 && mk.LastProduced > 1e-9)
                    {
                        double ratio = mk.LastExport / mk.LastProduced;
                        if (ratio > 0.10) spec.Add((w.SelfPower?.Name ?? "?", r.Name, g.Name, ratio, mk.LastExport));
                        // 輸出州の地元価格/参照価格（価格法則の輸出反映が効いているか＝旧式は 0.17rp 級へ潰れる）
                        double rp = Catalog.RefPrices[g.Id] * r.PriceLevel;
                        if (rp > 1e-9) { expStates++; lpSum += mk.Price / rp; rpSum += 1.0; }
                    }
                    if (g.Tier == GoodTier.Staple) { sImp += mk.LastImport; sDem += mk.LastDemand; }
                }
            }
            if (sDem > 1e-9) stapleDep.Add((w.SelfPower?.Name ?? "?", sImp / sDem));
        }
        Console.WriteLine($"世界貿易/GDP: {(wGdp > 1e-9 ? wTrade / wGdp * 100 : 0):F3} %　輸出行（比>10%）: {spec.Count}件　輸出財の lp/rp 平均: {(rpSum > 0 ? lpSum / rpSum : 0):F3}");
        Console.WriteLine("  輸出特化上位10（州,財,輸出/産出）:");
        foreach (var t in spec.OrderByDescending(x => x.ratio).Take(10))
            Console.WriteLine($"    {t.nat}/{t.reg}/{t.good}  {t.ratio:P0}（量{t.exp:F0}）");
        Console.WriteLine("  主食輸入依存上位8（Σ輸入/Σ需要）:");
        foreach (var t in stapleDep.OrderByDescending(x => x.dep).Take(8))
            Console.WriteLine($"    {t.nat}  {t.dep:P1}");
    }
    for (int q = 1; q <= mpQ; q++)
    {
        mpm.StepQuarter();
        if (q % mpEvery == 0 || q == mpQ) MpSnap(q);
    }
    return;
}

// 貿易消滅の時系列（2026-08-14・外交観測v5の発見＝1914年の英貿易/GDP 0.2%（史実約25%）の切り分け）：
//   開幕在庫の残光が切れて死ぬ（急落）のか、輸送費/自給化で絞まる（漸減）のか、を年代で見る。
if (args.Length > 0 && args[0] == "tradeprobe")
{
    int tpQ = ArgInt(args, 1, 524);
    int tpEvery = ArgInt(args, 2, 20);
    var tpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in tpm.Nations) nn.TelemetryEnabled = false;
    tpm.Diplomacy.Log.Enabled = false;
    tpm.StartWarmup();
    string[] tpWatch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    Console.WriteLine("年,Q," + string.Join(",", tpWatch.Select(n => n + "貿易GDP%")));
    void TpLine(int q)
    {
        var cols = new List<string> { (1783 + q / 4).ToString(), q.ToString() };
        foreach (var wname in tpWatch)
        {
            var nation = tpm.Nations.FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == wname);
            if (nation?.World is not { } w || w.ExportsByGood is not { } eg || w.ImportsByGood is not { } ig) { cols.Add("-"); continue; }
            cols.Add($"{TradeGdp(w) * 100:F2}");
        }
        Console.WriteLine(string.Join(",", cols));
        if (q == 80 || q == 280 || q == tpQ)   // 3点で英の輸出上位＝何が死ぬのか
        {
            var en = tpm.Nations.FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == "イギリス");
            if (en?.World.ExportsByGood is { } eg2)
                Console.WriteLine("  # 英輸出上位: " + string.Join(" / ", Enumerable.Range(0, eg2.Length).OrderByDescending(g => eg2[g]).Take(5).Select(g => $"{Catalog.Goods[g].Name}={eg2[g]:F0}")));
        }
    }
    TpLine(0);
    for (int q = 1; q <= tpQ; q++) { tpm.StepQuarter(); if (q % tpEvery == 0 || q == tpQ) TpLine(q); }
    return;
}

// === 貿易開放度の「単位」と植民地機構の実体規模（2026-08-21・純読み取り／SimCoreは1行も触らない）： dotnet run -- openprobe [Q] ===
//   検証したい主張：「実在する定義の穴は1つだけ＝植民地財が二国間交易にも貿易統計にも入らない（規模は数%）」。
//   ⚠貿易/GDP は**合成指標**。分子（四半期フロー×4）と分母（NominalGdp）の周期が違えば、比そのものが定義の穴になる。
//     よって構成要素を個別に測る：①12ヶ月ぶんの NominalGdp を月次決算の直後に積む（＝年GDP・単位仮定ゼロ）
//     ②4四半期ぶんの(輸出+輸入)を四半期末に積む（＝年貿易額）③比＝定義に依存しない真の開放度
//     ④現行式 (輸出+輸入)×4/NominalGdp との倍率。
//   ⑤植民地機構の実体規模：多国シムでは Trade.Step が !TradeEnabled（Trade.cs:13）で即returnするので、
//     四半期末**以外**の月（各四半期の第1・第2月）の Market.LastImport/LastExport を書くのは植民地機構だけ
//     （EconomyStep の植民地素材輸入／Colonial の LastExport 書き手＝投資・捕囚市場・維持費）。
//     そこを読めば「植民地→本国の財の流入」「本国→植民地の財の流出」を計上漏れなく金額で測れる。8/12ヶ月ぶん→×1.5で年額。
//     金銀（準備の市中放出＝Simulation.SellReserve）だけは同じ口を使うので除外する。
if (args.Length > 0 && args[0] == "openprobe")
{
    int opQ = ArgInt(args, 1, 524);
    var opm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn0 in opm.Nations) nn0.TelemetryEnabled = false;
    opm.Diplomacy.Log.Enabled = false;
    opm.StartWarmup();
    Console.WriteLine($"★条件: {(Coefficients.OverrideSummary().Length > 0 ? Coefficients.OverrideSummary() : "既定値")}");
    string[] opWatch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    int[] opYears = [1784, 1793, 1803, 1813, 1823, 1833, 1853, 1873, 1893, 1913, 1914];
    var acc = new Dictionary<string, double[]>();
    double[] A(string nm) { if (!acc.TryGetValue(nm, out var a)) { a = new double[12]; acc[nm] = a; } return a; }
    static string NmOf(World w) { var p = w.SelfPower; return p is null ? w.Region.Name : (p.BaseName.Length > 0 ? p.BaseName : p.Name); }
    // ⚠価格基準の罠：貿易額は**市場価格（金建て）**・GDPは**参照価格**。植民地財は国内生産者を持たない州が多く
    //   局所価格が RefPrice の何十倍にも張り付きうる（PriceCeilFactor=100）ので、市場価格建ての比だけを見ると過大に見える。
    //   よって同じ量を**参照価格でも**評価して両方出す（実質基準＝実GDPと同じ物差し）。
    Console.WriteLine("年,国,年GDP(12ヶ月),年実GDP(12ヶ月),年貿易額(4四半期),真の開放度%,現行式の開放度%,現行式/真の倍率,植民地→本国流入(市場価格・年額),同/年GDP%,うち植民地財/年GDP%,本国→植民地流出(市場価格・年額),同/年GDP%,植民地→本国流入(参照価格・年額),同/年実GDP%,本国→植民地流出(参照価格)/年実GDP%,主張の指標(ColImportCost÷frac)/年GDP%");
    for (int q = 1; q <= opQ; q++)
    {
        for (int d = 0; d < Calendar.DaysPerQuarter; d++)
        {
            opm.StepDay();
            if ((d + 1) % Calendar.DaysPerMonth != 0) continue;           // 月次決算が走った直後だけ読む
            bool pure = (d + 1) != Calendar.DaysPerQuarter;               // 第3月は同じ日に二国間交易も走る＝混ざる。第1・第2月だけ使う
            foreach (var n2 in opm.Nations)
            {
                var w = n2.World; var a = A(NmOf(w));
                a[0] += System.Math.Max(0, w.NominalGdp);
                a[8] += System.Math.Max(0, w.Gdp);
                if (!pure) continue;
                double er = w.ExchangeRate > 1e-9 ? w.ExchangeRate : 1.0;
                foreach (var r in w.Regions)
                {
                    if (r.State != RegionState.Active) continue;
                    for (int g = 0; g < r.Markets.Length; g++)
                    {
                        if (g == Catalog.GoldOreId || g == Catalog.SilverId) continue;   // 準備放出（SellReserve）は植民地機構ではない
                        var m = r.Markets[g];
                        double p = System.Math.Max(0, m.Price) * er;
                        double rp = g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
                        if (m.LastImport > 0) { double v = m.LastImport * p; a[3] += v; a[9] += m.LastImport * rp; if (Catalog.IsColonial(g)) a[2] += v; }
                        if (m.LastExport > 0) { a[4] += m.LastExport * p; a[10] += m.LastExport * rp; }
                    }
                }
            }
        }
        foreach (var n2 in opm.Nations)
        {
            var w = n2.World; var a = A(NmOf(w));
            double ex = w.ExportsByGood is { } eg3 ? eg3.Sum() : 0, im = w.ImportsByGood is { } ig3 ? ig3.Sum() : 0;
            a[1] += ex + im; a[5] = ex + im; a[6] = w.NominalGdp;
        }
        if (q % 4 != 0) continue;
        int year = 1783 + q / 4;
        if (System.Array.IndexOf(opYears, year) >= 0)
        {
            foreach (var wn in opWatch)
            {
                if (!acc.TryGetValue(wn, out var a)) continue;
                double gdpY = System.Math.Max(1.0, a[0]), rgdpY = System.Math.Max(1.0, a[8]);
                double trueOpen = a[1] / gdpY * 100;
                double codeOpen = a[5] * 4.0 / System.Math.Max(1.0, a[6]) * 100;
                double inFlow = a[3] * 1.5, inCol = a[2] * 1.5, outFlow = a[4] * 1.5;   // 8ヶ月→12ヶ月
                double inFlowR = a[9] * 1.5, outFlowR = a[10] * 1.5;
                double claimed = 0;   // 旧・植民地の採取決済（Region.ColonialImportCost）は 2026-09-28 に削除＝常に0（列の形は保つ）
                Console.WriteLine($"{year},{wn},{a[0]:F0},{a[8]:F0},{a[1]:F0},{trueOpen:F4},{codeOpen:F3},{(trueOpen > 1e-12 ? codeOpen / trueOpen : 0):F2},{inFlow:F0},{inFlow / gdpY * 100:F3},{inCol / gdpY * 100:F3},{outFlow:F0},{outFlow / gdpY * 100:F3},{inFlowR:F0},{inFlowR / rgdpY * 100:F3},{outFlowR / rgdpY * 100:F3},{claimed / gdpY * 100:F3}");
            }
        }
        foreach (var a in acc.Values) { a[0] = a[1] = a[2] = a[3] = a[4] = a[7] = a[8] = a[9] = a[10] = 0; }   // 年ごとに積み直す
    }
    return;
}

// === 対外アクセスの分解（純テレメトリ・力学は1行も読まない）： dotnet run -- accprobe [Q] [every] ===
//   検証したい主張：「1803-04の英の急落は戦争によるアクセス遮断＝一過性で、構造的な貿易減衰とは別物」。
//   ⚠合成量（World.ExternalTradeAccess）だけを見ると誤る。BilateralMarket が実際に量式へ掛けるのは
//     access = ExternalTradeAccess() × Region.LastTradeThrottle（前月の律速率。0は本物の律速＝否定形ガードで通す）である。
//   よって**構成要素を個別に**出す：①外交アクセス（禁輸/交戦の正規化損）②覇権封じ込め残差 ③輸送律速 ④両者の積＝実効。
//   さらに時点値でなく**期間平均**も出す（「1813年は0.951だから関係ない」という時点スナップショットの罠を潰す）。
if (args.Length > 0 && args[0] == "accprobe")
{
    int apQ = ArgInt(args, 1, 524);
    int apEvery = ArgInt(args, 2, 20);
    var apm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in apm.Nations) nn.TelemetryEnabled = false;
    apm.Diplomacy.Log.Enabled = false;
    apm.StartWarmup();
    Console.WriteLine($"★条件: {(Coefficients.OverrideSummary().Length > 0 ? Coefficients.OverrideSummary() : "既定値")}");
    string[] apWatch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    // 期間別の平均を貯める（時点値の罠を潰す）
    var segName = new[] { "1783-1802", "1803-1812", "1813-1822", "1823-1852", "1853-1892", "1893-1934" };
    int SegOf(int y) => y < 1803 ? 0 : y < 1813 ? 1 : y < 1823 ? 2 : y < 1853 ? 3 : y < 1893 ? 4 : 5;
    var segDip = new double[apWatch.Length][]; var segThr = new double[apWatch.Length][]; var segEff = new double[apWatch.Length][]; var segN = new double[apWatch.Length][];
    for (int i = 0; i < apWatch.Length; i++) { segDip[i] = new double[6]; segThr[i] = new double[6]; segEff[i] = new double[6]; segN[i] = new double[6]; }
    var segWDip = new double[6]; var segWThr = new double[6]; var segWEff = new double[6]; var segWN = new double[6];

    // 1国の分解を返す（純読み取り）
    (double dip, double warLost, double hegRes, double thr, double thrW, double defl, double tradePct, int linked, int total) ApDecomp(World w)
    {
        double acc = w.ExternalTradeAccess();
        double warLost = 0;
        if (w.Diplomacy is { } d && (w.SelfPower ?? d.Player) is { } me)
        {
            double sum = 0, lost = 0;
            foreach (var p in d.Powers)
                if (p != me && p.Nation is not null)
                {
                    sum += p.TradeWeight;
                    if (d.HasEmbargo(me, p) || d.IsAtWar(me, p)) lost += p.TradeWeight;
                }
            warLost = sum > 1e-9 ? lost / sum : 0;
        }
        double hegRes = Math.Max(0, (1.0 - warLost) - acc);   // 残差＝覇権封じ込め（＋クランプ）
        double thr = 0, thrW = 0, wsum = 0; int n = 0, linked = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            n++;
            double t = r.LastTradeThrottle;
            thr += t;
            double wt = Math.Max(0, r.LastTradePotential);
            thrW += t * wt; wsum += wt;
            if (r.LastTradeThrottle < 0.999) linked++;
        }
        thr = n > 0 ? thr / n : 1.0;
        thrW = wsum > 1e-9 ? thrW / wsum : thr;
        double tp = TradeGdp(w) * 100;
        return (acc, warLost, hegRes, thr, thrW, w.Gdp > 1 ? w.NominalGdp / w.Gdp : 0, tp, linked, n);
    }

    Console.WriteLine("年,Q,世界貿易GDP%,世界外交acc(GDP加重),世界律速(潜在加重),世界実効acc,交戦数,世界貿易額(年),世界名目GDP,世界実GDP,名目÷実," +
                      string.Join(",", apWatch.SelectMany(n => new[] { n + "外交acc", n + "禁輸交戦損", n + "覇権残差", n + "律速", n + "実効acc", n + "貿易GDP%", n + "律速州/全州", n + "名目÷実" })));
    void ApLine(int q, bool print)
    {
        int year = 1783 + q / 4;
        int seg = SegOf(year);
        double wTrade = 0, wGdp = 0, wDipAcc = 0, wGdpW = 0, wThrNum = 0, wThrDen = 0, wReal = 0;
        foreach (var n in apm.Nations)
        {
            var w = n.World;
            if (w.SelfPower is null) continue;
            double g = Math.Max(1.0, w.NominalGdp);
            if (w.ExportsByGood is { } eg && w.ImportsByGood is { } ig) wTrade += (eg.Sum() + ig.Sum()) * w.Coef.ClearingsPerYear;
            wGdp += g; wReal += Math.Max(0, w.Gdp);
            wDipAcc += w.ExternalTradeAccess() * g; wGdpW += g;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double t = r.LastTradeThrottle;
                double wt = Math.Max(0, r.LastTradePotential);
                wThrNum += t * wt; wThrDen += wt;
            }
        }
        double wDip = wGdpW > 1e-9 ? wDipAcc / wGdpW : 1.0;
        double wThr = wThrDen > 1e-9 ? wThrNum / wThrDen : 1.0;
        segWDip[seg] += wDip; segWThr[seg] += wThr; segWEff[seg] += wDip * wThr; segWN[seg] += 1;
        var cols = new List<string> { year.ToString(), q.ToString(),
            $"{(wGdp > 1 ? wTrade / wGdp * 100 : 0):F3}", $"{wDip:F4}", $"{wThr:F4}", $"{wDip * wThr:F4}",
            apm.Diplomacy.Wars.Count.ToString(),
            $"{wTrade:F0}", $"{wGdp:F0}", $"{wReal:F0}", $"{(wReal > 1 ? wGdp / wReal : 0):F4}" };
        for (int i = 0; i < apWatch.Length; i++)
        {
            var nation = apm.Nations.FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == apWatch[i]);
            if (nation?.World is not { } w) { cols.AddRange(["-", "-", "-", "-", "-", "-", "-", "-"]); continue; }
            var dcm = ApDecomp(w);
            segDip[i][seg] += dcm.dip; segThr[i][seg] += dcm.thrW; segEff[i][seg] += dcm.dip * dcm.thrW; segN[i][seg] += 1;
            cols.AddRange([$"{dcm.dip:F4}", $"{dcm.warLost:F4}", $"{dcm.hegRes:F4}", $"{dcm.thrW:F4}", $"{dcm.dip * dcm.thrW:F4}", $"{dcm.tradePct:F3}", $"{dcm.linked}/{dcm.total}", $"{dcm.defl:F3}"]);
        }
        if (print) Console.WriteLine(string.Join(",", cols));
    }
    ApLine(0, true);
    for (int q = 1; q <= apQ; q++) { apm.StepQuarter(); ApLine(q, q % apEvery == 0 || q == apQ); }
    Console.WriteLine("\n--- 期間平均（全四半期を平均＝時点スナップショットの罠を潰す） ---");
    Console.WriteLine("期間,世界外交acc,世界律速,世界実効," + string.Join(",", apWatch.SelectMany(n => new[] { n + "外交", n + "律速", n + "実効" })));
    for (int s = 0; s < 6; s++)
    {
        var cols = new List<string> { segName[s],
            $"{(segWN[s] > 0 ? segWDip[s] / segWN[s] : 0):F4}", $"{(segWN[s] > 0 ? segWThr[s] / segWN[s] : 0):F4}", $"{(segWN[s] > 0 ? segWEff[s] / segWN[s] : 0):F4}" };
        for (int i = 0; i < apWatch.Length; i++)
        {
            double n0 = segN[i][s];
            cols.AddRange([$"{(n0 > 0 ? segDip[i][s] / n0 : 0):F4}", $"{(n0 > 0 ? segThr[i][s] / n0 : 0):F4}", $"{(n0 > 0 ? segEff[i][s] / n0 : 0):F4}"]);
        }
        Console.WriteLine(string.Join(",", cols));
    }
    return;
}

// === 賦存→産出→価格の翻訳を測る（純テレメトリ・力学は1行も読まない）： dotnet run -- endowprobe <Q> [seed] ===
//   検証したい主張：「賦存は10倍の生産性差を持つのに価格差に翻訳されない／全州が全一次産品を自給する」。
//   合成指標（自給率＝産出>0の州割合）は ResourceScarcity>0 のとき恒真に近いので、**構成要素を個別に出す**：
//   ①生産性（実際に10倍が残っているか）②産出シェア（賦存州が世界産出の何割か）③自給の実体（地元産/地元需要）
//   ④金建て価格の分位・賦存州/非賦存州の価格比。③④が主張の是非を分ける。
if (args.Length > 0 && args[0] == "endowprobe")
{
    int epQ = ArgInt(args, 1, 524);
    ulong epSeed = args.Length > 2 && ulong.TryParse(args[2], out var eps) ? eps : 1789UL;
    var epm = new MultiNationSim(WorldData.Nations(), epSeed, _ => new UtilityAgent());
    foreach (var nn in epm.Nations) nn.TelemetryEnabled = false;
    epm.Diplomacy.Log.Enabled = false;
    epm.StartWarmup();
    var epEndow = new HashSet<(string, int)>();
    foreach (var n0 in WorldData.Nations()) foreach (var p0 in n0.Provinces) foreach (var g0 in p0.Resources) epEndow.Add((p0.City, g0));
    int[] epGoods = [Catalog.CoalId, Catalog.IronOreId, Catalog.WheatId, Catalog.TimberId, Catalog.SilkId, Catalog.LivestockId, Catalog.FishId, Catalog.CottonId, Catalog.CrudeOilId, Catalog.IronId, Catalog.SteelId];
    static double Pct(List<double> v, double p) { if (v.Count == 0) return 0; int i = (int)Math.Clamp(Math.Round(p * (v.Count - 1)), 0, v.Count - 1); return v[i]; }
    void EpDump(int q)
    {
        Console.WriteLine($"--- Q{q}（{1783 + q / 4}年） ---");
        Console.WriteLine("財,州数,賦存州数,産出>0率,地元産/地元需要 中央値,賦存産出シェア,生産性中央値(賦存),生産性中央値(非賦存),生産性比,価格p10,p25,p50,p75,p90,p75/p25,p90/p10,価格中央値(賦存),価格中央値(非賦存),価格比(非/賦),操業Scale中央値(賦存),同(非賦存)");
        foreach (int g in epGoods)
        {
            List<double> prices = [], pE = [], pN = [], prodE = [], prodN = [], selfRatio = [], scE = [], scN = [];
            double outE = 0, outN = 0; int nProv = 0, nEnd = 0, nPos = 0;
            foreach (var n in epm.Nations)
            {
                double er = n.World.ExchangeRate > 1e-9 ? n.World.ExchangeRate : 1.0;
                foreach (var r in n.World.Regions)
                {
                    if (r.State != RegionState.Active) continue;
                    if (g >= r.Markets.Length) continue;
                    nProv++;
                    bool isE = epEndow.Contains((r.Name, g));
                    if (isE) nEnd++;
                    var mkt = r.Markets[g];
                    double gold = mkt.Price * er;
                    if (gold > 1e-9) { prices.Add(gold); (isE ? pE : pN).Add(gold); }
                    if (mkt.LastProduced > 1e-9) nPos++;
                    if (mkt.LastDemand > 1e-9) selfRatio.Add(mkt.LastProduced / mkt.LastDemand);
                    if (isE) outE += mkt.LastProduced; else outN += mkt.LastProduced;
                    foreach (var pr in r.Producers)
                        if (pr.GoodId == g) { (isE ? prodE : prodN).Add(pr.Productivity); (isE ? scE : scN).Add(pr.Scale); }
                }
            }
            prices.Sort(); pE.Sort(); pN.Sort(); prodE.Sort(); prodN.Sort(); selfRatio.Sort(); scE.Sort(); scN.Sort();
            double p10 = Pct(prices, 0.10), p25 = Pct(prices, 0.25), p50 = Pct(prices, 0.50), p75 = Pct(prices, 0.75), p90 = Pct(prices, 0.90);
            double mE = Pct(prodE, 0.5), mN = Pct(prodN, 0.5);
            double vE = Pct(pE, 0.5), vN = Pct(pN, 0.5);
            Console.WriteLine($"{Catalog.Goods[g].Name},{nProv},{nEnd},{(nProv > 0 ? (double)nPos / nProv : 0):P0},{Pct(selfRatio, 0.5):F3},{(outE + outN > 1e-9 ? outE / (outE + outN) : 0):P1},"
                + $"{mE:F3},{mN:F3},{(mN > 1e-9 ? mE / mN : 0):F2},{p10:F3},{p25:F3},{p50:F3},{p75:F3},{p90:F3},{(p25 > 1e-9 ? p75 / p25 : 0):F3},{(p10 > 1e-9 ? p90 / p10 : 0):F3},"
                + $"{vE:F3},{vN:F3},{(vE > 1e-9 ? vN / vE : 0):F3},{Pct(scE, 0.5):F3},{Pct(scN, 0.5):F3}");
        }
        // 貿易/GDP（同じ走行で並べる＝別プロセスの数字と混ぜない）
        foreach (var wn in new[] { "イギリス", "フランス", "アメリカ", "清" })
        {
            var nation = epm.Nations.FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == wn);
            if (nation?.World is { } w && w.ExportsByGood is { } eg && w.ImportsByGood is { } ig)
                Console.WriteLine($"# 貿易/GDP {wn}={TradeGdp(w) * 100:F2}%  輸出={eg.Sum():F0} 輸入={ig.Sum():F0} GDP={w.NominalGdp:F0}");
        }
        Console.WriteLine();
    }
    Console.WriteLine($"=== 賦存の翻訳（seed {epSeed}／ResourceScarcityScale={new Coefficients().ResourceScarcityScale}／ResourceEndowmentScale={new Coefficients().ResourceEndowmentScale}） ===");
    EpDump(0);
    for (int q = 1; q <= epQ; q++) { epm.StepQuarter(); if (q == 120 || q == 200 || q == 400 || q == epQ) EpDump(q); }
    return;
}

// 貿易消滅の根因分解（2026-08-15・貿易復活プログラム第1測定）：4仮説（①費用収束＝価格分散の消滅 ②関税 ③輸送費 ④最安着値の構造）を
//   1表で切り分ける。取引の成立条件（BilateralMarket の成約判定）＝ lp買 > lp売×(1+margin+wedge) なので、
//   国家間の価格分散（max/min）が (1+margin+wedge) を下回れば貿易はゼロ＝どの項が支配的かを財別・年代別に出す。
//   ⚠下の margin/wedge は近似（旧式・リスク0前提）：現行 BilateralMarket は TreatyPortTariffScale・穀物チェーンゲート・
//   buyDear・adValoremRev・Diplomacy.DistanceNormKm も持つ（この計器は複製していない＝式の二重定義を避けて概形だけ見る）。
if (args.Length > 0 && args[0] == "tradewhy")
{
    int twQ = ArgInt(args, 1, 524);
    var twm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in twm.Nations) nn.TelemetryEnabled = false;
    twm.Diplomacy.Log.Enabled = false;
    twm.StartWarmup();
    int[] twGoods = [Catalog.WheatId, Catalog.CoalId, Catalog.IronId, Catalog.CottonId, Catalog.FlourId];   // 麦・石炭・鉄・綿花・小麦粉（貿易の主役級＝主食/燃料/金属/繊維原料）
    void TwDump(int q)
    {
        var c0 = twm.Nations[0].World.Coef;
        double frontier = 0; foreach (var n in twm.Nations) if (n.World.TechLevel > frontier) frontier = n.World.TechLevel;
        double freightRev = c0.TransportRevolutionMax > 0
            ? 1.0 - c0.TransportRevolutionMax * System.Math.Clamp((frontier - c0.TransportRevolutionOnset) / c0.TransportRevolutionSpan, 0.0, 1.0) : 1.0;
        double tarSum = 0; int tarN = 0;
        foreach (var n in twm.Nations) { if (n.World.SelfPower is null) continue; tarSum += System.Math.Max(0, n.World.Tariff); tarN++; }
        Console.WriteLine($"=== {1783 + q / 4}（Q{q}）平均関税 {tarSum / System.Math.Max(1, tarN):P0}　運賃革命係数 {freightRev:F2} ===");
        Console.WriteLine("  財        州数   最安    p25    中央    p75    最高   最高/最安  必要比(3千km+英wedge)  英中央/世界最安");
        foreach (int g in twGoods)
        {
            var prices = new List<double>(); var engPrices = new List<double>();
            World? eng = null;
            foreach (var n in twm.Nations)
            {
                var w = n.World; if (w.SelfPower is null) continue;
                bool isEng = (w.SelfPower.BaseName.Length > 0 ? w.SelfPower.BaseName : w.SelfPower.Name) == "イギリス";
                if (isEng) eng = w;
                double er = w.ExchangeRate > 1e-9 ? w.ExchangeRate : 1.0;
                foreach (var r in w.Regions)
                {
                    if (r.State != RegionState.Active) continue;
                    double lp = r.Markets[g].Price * er;
                    if (!(lp > 1e-9)) continue;
                    prices.Add(lp); if (isEng) engPrices.Add(lp);
                }
            }
            if (prices.Count < 4) { Console.WriteLine($"  {Catalog.Goods[g].Name,-8} 標本不足"); continue; }
            prices.Sort(); engPrices.Sort();
            double min = prices[0], max = prices[^1], med = prices[prices.Count / 2];
            double p25 = prices[prices.Count / 4], p75 = prices[3 * prices.Count / 4];
            // 3,000km の越境に必要な比＝(1+margin+wedge)。margin＝基本+距離比例+重量従量（BilateralMarket の margin 計算の近似・旧式・リスク0前提）
            double kmR = 3000.0 / 10_000.0;
            double rp = Catalog.RefPrices[g];
            double weightAdd = c0.TransportWeightMarginScale > 0 && rp > 1e-9 ? c0.TransportWeightMarginScale * Catalog.TransportWeight(g) / rp * freightRev : 0;
            double margin = c0.TransportMargin * (1.0 + c0.DistanceMarginScale * kmR) + weightAdd * kmR;
            double wedgeEng = eng is not null
                ? System.Math.Clamp(eng.Tariff, c0.TariffFloor, 1) * c0.TariffProtect
                  + (Catalog.Goods[g].Tier == GoodTier.Staple ? (eng.CornLaws == ReformLevel.Full ? 1.0 : eng.CornLaws == ReformLevel.Limited ? 0.5 : 0) * c0.CornLawImportPenalty : 0)
                : 0;
            double needRatio = 1.0 + margin + wedgeEng;
            double engMed = engPrices.Count > 0 ? engPrices[engPrices.Count / 2] : 0;
            Console.WriteLine($"  {Catalog.Goods[g].Name,-8} {prices.Count,4}  {min,6:F2} {p25,6:F2} {med,6:F2} {p75,6:F2} {max,6:F2}   {max / min,6:F2}     {needRatio,6:F2}              {(engMed > 0 ? engMed / min : 0),6:F2}");
        }
    }
    TwDump(0);
    for (int q = 1; q <= twQ; q++) { twm.StepQuarter(); if (q == 80 || q == 200 || q == 400 || q == twQ) TwDump(q); }
    // ★容量スロットルの実測（2026-08-15・第3診断＝価格側3レバー全開でも曲線不変を受けて）：
    //   access = ExternalTradeAccess × 前月律速率(LastTradeThrottle)・州の容量 = (船容量/(1+植民地負担)+基準値)÷州数。
    //   律速率の分布と主要国の容量を出す＝量のクランプの直接測定。
    {
        Console.WriteLine("--- 容量スロットル（最終Q・主要国） ---");
        var c1 = twm.Nations[0].World.Coef;
        foreach (var n in twm.Nations)
        {
            var w = n.World; var pw = w.SelfPower; if (pw is null) continue;
            var bn = pw.BaseName.Length > 0 ? pw.BaseName : pw.Name;
            if (bn is not ("イギリス" or "フランス" or "ロシア" or "プロイセン" or "清" or "アメリカ")) continue;
            int act = 0; var th = new List<double>();
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { act++; th.Add(r.LastTradeThrottle); }
            th.Sort();
            double perState = act > 0 ? (w.ShipCapacity / (1.0 + System.Math.Max(0, w.ColonialShipBurden)) + Transport.PreRailCapacity(w, c1)) / act : 0;   // ★(166) 鉄道以前の陸の容量＝人口×基礎の目標
            Console.WriteLine($"    {pw.Name,-8} 州{act,3}  船容量{w.ShipCapacity,10:F0}  州あたり容量{perState,9:F1}  律速率 min{(th.Count > 0 ? th[0] : 1):F3} 中央{(th.Count > 0 ? th[th.Count / 2] : 1):F3} max{(th.Count > 0 ? th[^1] : 1):F3}  対外アクセス{w.ExternalTradeAccess():F2}");
        }
    }
    // ★量の分解（2026-08-15・壁A/B＝効果なしを受けた第2診断）：価格差が壁を超えても取引されない財（綿花＝分散58倍）の
    //   ボトルネックを特定する。q ∝ 買い手のscaleT=max(LastDemand,LastProduced,TargetStock)＝「消費したことのない州は輸入も始められない」
    //   鶏卵ロックの検証。売り手側＝在庫と流量。
    foreach (int g in new[] { Catalog.CottonId, Catalog.WheatId })
    {
        Console.WriteLine($"--- 量の分解（{Catalog.Goods[g].Name}・最終Q）: 売り手候補=安値+在庫 / 買い手候補=高値 ---");
        var rows = new List<(double lp, string nation, string region, double stock, double dem, double prod, double tgt)>();
        foreach (var n in twm.Nations)
        {
            var w = n.World; if (w.SelfPower is null) continue;
            double er = w.ExchangeRate > 1e-9 ? w.ExchangeRate : 1.0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                var m = r.Markets[g];
                if (!(m.Price * er > 1e-9)) continue;
                rows.Add((m.Price * er, w.SelfPower.Name, r.Name, m.Stock, m.LastDemand, m.LastProduced, m.TargetStock));
            }
        }
        rows.Sort((a, b) => a.lp.CompareTo(b.lp));
        Console.WriteLine("  [売り手側=最安3州]  価格 / 在庫 / 需要 / 生産 / 目標在庫");
        for (int i = 0; i < System.Math.Min(3, rows.Count); i++)
            Console.WriteLine($"    {rows[i].nation} {rows[i].region,-8} {rows[i].lp,7:F2} / {rows[i].stock,8:F0} / {rows[i].dem,7:F1} / {rows[i].prod,7:F1} / {rows[i].tgt,7:F1}");
        Console.WriteLine("  [買い手側=最高3州]  価格 / 在庫 / 需要 / 生産 / 目標在庫   ⚠q∝max(需要,生産,目標在庫)＝全部0なら鶏卵ロック確定");
        for (int i = System.Math.Max(0, rows.Count - 3); i < rows.Count; i++)
            Console.WriteLine($"    {rows[i].nation} {rows[i].region,-8} {rows[i].lp,7:F2} / {rows[i].stock,8:F0} / {rows[i].dem,7:F1} / {rows[i].prod,7:F1} / {rows[i].tgt,7:F1}");
    }
    return;
}

// 貴族rentier分布（6d・docs/202608/20260814_貴族が保守に固着する理由.REVERTED_差し戻し.md【差し戻し】.md）＝NobleInvests点火前検証。
//   検証点①：貴族(Closed)の rentier=rent/(rent+earned) が本当に1に張り付いているか（国別）。
//   検証点②：貴族の Savings が投資プールに対してどの規模か（点火時の資金量インパクト見積り）。
//   ⚠コード読みの所見（実装ログ§4-8）：現行 Ideology.BaseWeights は Closed を固定重み分岐で処理し rentier を読まない
//   ＝述語（CapitalMarket.IsInvestor）だけ開けても思想は動かない。docの診断は旧コード前提の可能性＝この実測とセットで裁定へ。
if (args.Length > 0 && args[0] == "nobleprobe")
{
    int npQ = ArgInt(args, 1, 280);
    var npm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in npm.Nations) nn.TelemetryEnabled = false;
    npm.Diplomacy.Log.Enabled = false;
    npm.StartWarmup();
    string[] npWatch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "オーストリア"];
    void NpDump(int q)
    {
        Console.WriteLine($"=== {1783 + q / 4}（Q{q}） 群=貴族聖職(Closed)/近代エリート/その他 ===");
        Console.WriteLine("  国          群       規模      地代Σ     勤労Σ   rentier  貯蓄Σ    貯蓄シェア");
        foreach (var wname in npWatch)
        {
            var nation = npm.Nations.FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == wname);
            if (nation?.World is not { } w) continue;
            double[] size = new double[3], rent = new double[3], earned = new double[3], sav = new double[3];
            foreach (var r in w.Regions)
                foreach (var p in r.Pops)
                {
                    int g = p.Closed ? 0 : p.IsModernElite ? 1 : 2;
                    size[g] += p.Size; rent[g] += System.Math.Max(0, p.LandRent); earned[g] += System.Math.Max(0, p.Earned); sav[g] += System.Math.Max(0, p.Savings);
                }
            double savAll = System.Math.Max(1e-9, sav[0] + sav[1] + sav[2]);
            string[] gn = ["貴族聖職", "近代エリ", "その他"];
            for (int g = 0; g < 3; g++)
            {
                double rr = (rent[g] + earned[g]) > 1e-9 ? rent[g] / (rent[g] + earned[g]) : 0;
                Console.WriteLine($"  {wname,-6} {gn[g],-6} {size[g],10:F0} {rent[g],9:F0} {earned[g],9:F0}  {rr,5:F2}  {sav[g],9:F0}  {sav[g] / savAll,6:P1}");
            }
        }
    }
    NpDump(0);
    for (int q = 1; q <= npQ; q++) { npm.StepQuarter(); if (q == 140 || q == npQ) NpDump(q); }
    return;
}

// === 銀本位の創発の診断： dotnet run -- silverprobe [四半期] [出力間隔] [seed] ===
// 銀本位doc（2026-08-13）の狙い＝「金へ移った国の銀の準備需要が消え、需要曲線ごと下へ落ちて銀が暴落する」が
// 実際に出ているかを測る。⚠SimCoreは触らない（読むだけ）。POB_COEF で点火/休眠を切り替えて突き合わせる。
if (args.Length > 0 && args[0] == "silverprobe")
{
    int svQ = ArgInt(args, 1, 150);
    int svEvery = ArgInt(args, 2, 20);
    ulong svSeed = ArgUlong(args, 3, 1UL);
    var svc = new Coefficients();
    var svm = new MultiNationSim(WorldData.Nations(), svSeed, _ => new UtilityAgent());
    foreach (var nn in svm.Nations) nn.TelemetryEnabled = false;
    svm.Diplomacy.Log.Enabled = false;
    svm.StartWarmup();   // 自己対戦バッチと同じ開始盤面から測る
    string svOv = Coefficients.OverrideSummary();
    Console.WriteLine($"=== 銀本位プローブ seed={svSeed}／上書き={(svOv.Length > 0 ? svOv : "なし")} ===");
    Console.WriteLine($"    SilverStandardGate={svc.SilverStandardGate} StandardMetalMintGate={svc.StandardMetalMintGate}"
                    + $" 銀閾={svc.SilverStandardIndThreshold} 金閾={svc.GoldStandardIndThreshold} GoldMintFraction={svc.GoldMintFraction} MintReserveTargetWeight={svc.MintReserveTargetWeight}");
    Console.WriteLine($"    RefPrice: 銀({Catalog.SilverId})={Catalog.RefPrices[Catalog.SilverId]:F3}　金鉱({Catalog.GoldOreId})={Catalog.RefPrices[Catalog.GoldOreId]:F3}");
    var svEverAg = new List<string>(); var svEverAu = new List<string>();
    void SvDump(int q)
    {
        double pAg = Catalog.SilverId < svm.Market.Price.Count ? svm.Market.Price[Catalog.SilverId] : -1;
        double pAu = Catalog.GoldOreId < svm.Market.Price.Count ? svm.Market.Price[Catalog.GoldOreId] : -1;
        int lFiat = 0, lAg = 0, lAu = 0, onStd = 0, t0 = 0, t1 = 0, t2 = 0, indHi = 0, susp = 0;
        double agOut = 0, agStock = 0, auOut = 0, agLocal = 0, minted = 0, baseM = 0, broadM = 0, agRes = 0, auRes = 0; int agMkt = 0;
        // ★器 SilverParityFromRatio の診断列（2026-09-07）：局所比R（同一市場内 銀/金鉱 比の Active 州平均＝器が注入する量の元）・
        //   E銀/E金（本位別の平均為替＝自己参照ループの観測点）・床拘束州（銀価格≤RefPrice＝造幣局の床が効いている州数）。
        double svRatioSum = 0; int svRatioCnt = 0, svFloorCnt = 0;
        double svEAg = 0, svEAu = 0; int svEAgCnt = 0, svEAuCnt = 0;
        var agOwners = new List<string>();
        foreach (var n in svm.Nations)
        {
            var w = n.World;
            if (w.MonetaryLaw == MonetaryStandardLaw.Silver) { svEAg += w.ExchangeRate; svEAgCnt++; }
            else if (w.MonetaryLaw == MonetaryStandardLaw.Gold) { svEAu += w.ExchangeRate; svEAuCnt++; }
            minted += w.CumulativeGoldMinted; baseM += w.BaseMoney; broadM += w.BroadMoney; susp += w.GoldSuspensions;
            agRes += w.SilverReserveQty; auRes += w.GoldReserveQty;   // ★床モードの準備（2026-08-14）＝蓄積→demonetizationの投げ売りを観測
            if (w.MonetaryLaw == MonetaryStandardLaw.Fiat) lFiat++;
            else if (w.MonetaryLaw == MonetaryStandardLaw.Silver) lAg++;
            else lAu++;
            string bn = w.SelfPower?.BaseName ?? "?";
            if (w.MonetaryLaw == MonetaryStandardLaw.Silver && !svEverAg.Contains(bn)) svEverAg.Add(bn);
            if (w.MonetaryLaw == MonetaryStandardLaw.Gold && !svEverAu.Contains(bn)) svEverAu.Add(bn);
            if (w.OnGoldStandard) onStd++;
            int t = w.Policy.MonetaryTarget; if (t <= 0) t0++; else if (t == 1) t1++; else t2++;
            if (w.MeanIndustrialization > svc.GoldStandardIndThreshold) indHi++;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    if (p.GoodId == Catalog.SilverId) agOut += p.Output;
                    if (p.GoodId == Catalog.GoldOreId) auOut += p.Output;
                }
                if (r.GoodOf(Catalog.SilverId) is not null) { agStock += r.Markets[Catalog.SilverId].Stock; agLocal += r.Markets[Catalog.SilverId].Price; agMkt++; }
                if (r.GoodOf(Catalog.SilverId) is not null && Catalog.SilverId < r.Markets.Length && r.Markets[Catalog.SilverId].Price <= Catalog.RefPrices[Catalog.SilverId]) svFloorCnt++;
                if (Catalog.SilverId < r.Markets.Length && Catalog.GoldOreId < r.Markets.Length)
                {
                    var svMAg = r.Markets[Catalog.SilverId]; var svMAu = r.Markets[Catalog.GoldOreId];
                    double svAg = svMAg.Price, svAu = svMAu.Price;
                    bool svLive = (svMAg.LastDemand > 1e-9 || svMAg.LastProduced > 1e-9) && (svMAu.LastDemand > 1e-9 || svMAu.LastProduced > 1e-9);
                    if (svLive && svAg > 1e-9 && svAu > 1e-9) { svRatioSum += Math.Log(svAg / svAu); svRatioCnt++; }   // MultiNationSim の器と同じ選別（生きた両市場・対数平均）
                }
                bool hasAgMine = false;
                foreach (var p in r.Producers) if (p.GoodId == Catalog.SilverId && p.Output > 1e-9) hasAgMine = true;
                if (hasAgMine) agOwners.Add($"{w.SelfPower?.BaseName ?? "?"}:{r.Name}:{w.MonetaryLaw}");
            }
        }
        Console.WriteLine($"Q{q,3}({1783 + q / 4}) 銀世界={pAg,6:F3} 金世界={pAu,6:F3} 銀/金={(pAu > 1e-9 ? pAg / pAu : 0),6:F3} 銀局所={(agMkt > 0 ? agLocal / agMkt : 0),6:F3}"
                        + $"　法[不換{lFiat,2}/銀{lAg,2}/金{lAu,2}] 兌換中={onStd,2} 目標[不{t0,2}/銀{t1,2}/金{t2,2}] 工>金閾={indHi,2} 兌換停止累計={susp,3}"
                        + $"　銀産出={agOut,8:F1} 銀在庫={agStock,9:F0} 金産出={auOut,8:F1} 累積鋳造={minted,11:F0} 銀準備={agRes,9:F0} 金準備={auRes,9:F0} 基礎貨幣={baseM,12:F0} 広義貨幣={broadM,12:F0}"
                        + $"　局所比R={(svRatioCnt > 0 ? Math.Exp(svRatioSum / svRatioCnt) : 0),7:F4} E銀={(svEAgCnt > 0 ? (svEAg / svEAgCnt).ToString("F3") : "-"),6} E金={(svEAuCnt > 0 ? (svEAu / svEAuCnt).ToString("F3") : "-"),6} 床拘束州={svFloorCnt,3}");
        if (q == 0 || q % 160 == 0) Console.WriteLine($"      銀鉱州の保有国と通貨法: {string.Join(" / ", agOwners)}");
        // ★工業化の先頭集団（2026-08-21）：金本位の志向は MeanIndustrialization>閾 で決まるので、「誰が先に金へ届くか」を毎ダンプ観測する。
        {
            var svTop = new List<(string Name, double Ind, string Law)>();
            foreach (var n in svm.Nations)
                svTop.Add((n.World.SelfPower?.BaseName ?? "?", n.World.MeanIndustrialization,
                           n.World.MonetaryLaw == MonetaryStandardLaw.Fiat ? "不換" : n.World.MonetaryLaw == MonetaryStandardLaw.Silver ? "銀" : "金"));
            svTop.Sort((a, b) => b.Ind.CompareTo(a.Ind));
            var svSb = new System.Text.StringBuilder("      工業化上位: ");
            for (int i = 0; i < Math.Min(5, svTop.Count); i++) svSb.Append($"{svTop[i].Name}={svTop[i].Ind:F2}({svTop[i].Law})  ");
            Console.WriteLine(svSb.ToString());
        }
        // ★1914年断面（銀本位の史実度・2026-08-21）：国別の本位を史実1914と突き合わせる読み取り専用ダンプ。
        //   Q524＝1914年12月（忠実度ハーネス StandingsAt1914 と同じ物差し）。Q0（1783年の初期形＝史実は銀本位世界）も出す。
        if (q == 0 || q == 524 || q == svQ)
        {
            Console.WriteLine($"      ── 国別断面 Q{q}（{1783 + q / 4}年）: 国名:法(工業化/目標/兌換率/開放度/引力) ──");
            var svLine = new System.Text.StringBuilder();
            int svCol = 0;
            foreach (var n in svm.Nations)
            {
                var w = n.World;
                string law = w.MonetaryLaw == MonetaryStandardLaw.Fiat ? "不換" : w.MonetaryLaw == MonetaryStandardLaw.Silver ? "銀" : "金";
                double svEx = 0, svIm = 0;
                if (w.ExportsByGood is { } svEg) foreach (var v in svEg) if (v > 0) svEx += v;
                if (w.ImportsByGood is { } svIg) foreach (var v in svIg) if (v > 0) svIm += v;
                double svOpen = w.NominalGdp > 1e-9 ? (svEx + svIm) * w.Coef.ClearingsPerYear / (w.NominalGdp * Calendar.MonthsPerYear) : 0;   // MajorsTradeGdp と同じ物差し
                svLine.Append($"{w.SelfPower?.BaseName ?? "?"}:{law}({w.MeanIndustrialization:F2}/{w.Policy.MonetaryTarget}/{w.Convertibility:F2}/開{svOpen:F3}/引{w.TradeGoldPull:F3})  ");
                if (++svCol % 6 == 0) { Console.WriteLine("      " + svLine); svLine.Clear(); }
            }
            if (svLine.Length > 0) Console.WriteLine("      " + svLine);
        }
    }
    SvDump(0);
    for (int q = 1; q <= svQ; q++) { svm.StepQuarter(); if (q % svEvery == 0 || q == svQ || q == 524) SvDump(q); }
    Console.WriteLine($"銀本位を一度でも通った国 {svEverAg.Count}件: {(svEverAg.Count > 0 ? string.Join(" / ", svEverAg) : "なし")}");
    Console.WriteLine($"金本位を一度でも通った国 {svEverAu.Count}件: {(svEverAu.Count > 0 ? string.Join(" / ", svEverAu) : "なし")}");
    return;
}

// === RGO（農園・鉱山・牧場）の能力の診断： dotnet run -- rgoprobe <四半期> [_] [seed] ===
// ★(200) 段2：対照の旧経路（RgoCapitalShare の枝と止め旗 StateShareCounterOnly）は削除＝第2引数は読まない（互換のため位置だけ残す）。
// 作者裁定2026-08-10「国営工場に自動的な拡張なし」＝StateShareCounterOnly は既定1.0で点火済み（Coefficients.cs:2538・2026-08-30）。
// 点火時は資本家の建設予算からRGOの BuiltScale（＝Scaleの下限＝恒久の作付面積、同時に国営持分）を押し上げる唯一の経路
// （EconomyStep の RgoCapitalShare）が止まる。
// ⚠止めた副作用＝**RGOの能力（Scale）と産出が長期に痩せないか**を実測するための計器（既定＝点火済み経路。0を渡せば旧経路と突き合わせられる）。
if (args.Length > 0 && args[0] == "rgoprobe")
{
    int rqQ = ArgInt(args, 1, 120);
    ulong rqSeed = args.Length > 3 && ulong.TryParse(args[3], out var rqs) ? rqs : 1789UL;
    var rqm = new MultiNationSim(WorldData.Nations(), rqSeed, _ => new UtilityAgent());
    foreach (var nn in rqm.Nations) nn.TelemetryEnabled = false;
    rqm.Diplomacy.Log.Enabled = false;
    rqm.StartWarmup();   // 自己対戦バッチ・App と同じ開始盤面から測る
    string[] rqTargets = ["イギリス", "フランス", "アメリカ", "清", "ロシア"];
    void RgoDump(int q)
    {
        double wScale = 0, wBuilt = 0, wOut = 0, wFacOut = 0, wFacBuilt = 0, wFacScale = 0; int wN = 0;
        foreach (var n in rqm.Nations)
        {
            var w = n.World;
            double sc = 0, bs = 0, op = 0, fo = 0, fb = 0, fs = 0; int cnt = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    if (p.IsRgo) { cnt++; sc += p.Scale; bs += Math.Min(p.BuiltScale, p.Scale); op += p.Output; }
                    else { fo += p.Output; fb += Math.Min(p.BuiltScale, p.Scale); fs += p.Scale; }
                }
            }
            wScale += sc; wBuilt += bs; wOut += op; wFacOut += fo; wFacBuilt += fb; wFacScale += fs; wN += cnt;
            string name = w.SelfPower?.BaseName ?? "";
            if (Array.IndexOf(rqTargets, name) >= 0)
                Console.WriteLine($"Q{q,3} {name,-6} RGO {cnt,3}件 Scale計={sc,7:F1} 国営持分={(sc > 1e-9 ? bs / sc : 0),6:P1} 産出={op,9:F0}　工場 産出={fo,9:F0} 国営持分={(fs > 1e-9 ? fb / fs : 0),6:P1}　Ind={w.MeanIndustrialization:F2} 国庫={w.Treasury:F0}");
        }
        Console.WriteLine($"Q{q,3} 世界計 RGO {wN}件 Scale計={wScale:F1} 国営持分={(wScale > 1e-9 ? wBuilt / wScale : 0):P1} RGO産出={wOut:F0} 工場産出={wFacOut:F0} 工場国営持分={(wFacScale > 1e-9 ? wFacBuilt / wFacScale : 0):P1}");
        Console.WriteLine();
    }
    Console.WriteLine($"=== RGOの能力と国営持分（seed {rqSeed}／ScaleMax={new Coefficients().ScaleMax}） ===");
    RgoDump(0);
    for (int q = 1; q <= rqQ; q++) { rqm.StepQuarter(); if (q % 20 == 0 || q == rqQ) RgoDump(q); }
    return;
}

// === 議席配分の断面（開始8四半期後・6カ国）： dotnet run -- parlprobe ===
if (args.Length > 0 && args[0] == "parlprobe")
{
    var pm = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    pm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var nsim in pm.Nations) nsim.TelemetryEnabled = false;
    pm.Diplomacy.Log.Enabled = false;
    for (int q = 0; q < 8; q++) pm.StepQuarter();
    System.Console.WriteLine("国	政体	参政権	議席(保/自/社)	有権者比");
    foreach (var n in pm.Nations.Take(6))
    {
        var w = n.World;
        double tot = 0; foreach (var b2 in Parliament.SupportBase(w)) tot += b2.Voters;
        System.Console.WriteLine($"{w.SelfPower?.Name}	{w.Polity}	{w.Suffrage}	{w.PartySeats[0]:F2}/{w.PartySeats[1]:F2}/{w.PartySeats[2]:F2}	{(w.TotalPopulation > 0 ? tot / w.TotalPopulation : 0):P1}");
    }
    return;
}

// === poweraff：正当性の内訳を【距離1本】で割る（新案・2026-08-19）===
//     dotnet run -- poweraff [seed=1789] [終了年=1934]
// ★PowerAffinity は純関数でどの挙動にも配線されていない＝この命令は世界を1ビットも変えない。
// ★★★見るもの：
//   ① 写像の検算（11思想それぞれの行き先。世界に依存せず座標だけで決まる＝写像そのものの正しさ）
//   ② 年代断面（新案と旧 PowerShares を並べる）
//   ③ ★★★ラチェットの検査＝議会シェアが時代とともに単調増加していないか（作者の最大の懸念）
//   ④ 硬性条件（英・米・蘭・白・丁・諾・瑞西の軍部シェアがほぼ0か）
//   ⑤ 検証点（英1815穀物法／普1867／露1917／日1936）
if (args.Length > 0 && args[0] == "poweraff")
{
    ulong paSeed = ArgUlong(args, 1, 1789UL);
    int paEnd = ArgInt(args, 2, Calendar.EndYear);   // 既定＝終局暦年（全長 GameQuarters の年）
    // ★戦間期を厚くした（2026-08-20）＝当時のファシズムの時代ゲートは (年−1919)/15 で**1.0に達するのは最終年1934年だけ**だった（2026-10-01 (152) から門は発見の表の思想の行）。
    //   ⚠旧は1917→1926→1934の3点しかなく、育つ軌跡が見えなかった。1919/1923/1929/1932を足して分解能を上げる。
    int[] paYears = [1783, 1800, 1815, 1832, 1848, 1867, 1871, 1889, 1900, 1912, 1917, 1919, 1923, 1926, 1929, 1932, 1934];
    string[] paWatch = ["イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "日本", "清", "アメリカ", "オスマン", "スペイン"];
    string[] paQuiet = ["イギリス", "アメリカ", "ネーデルラント", "ベルギー", "デンマーク", "ノルウェー", "スイス"];
    const double paQuietMax = 0.10;

    var paCoef = new Coefficients();
    Console.WriteLine($"=== ① 写像の検算：{Ideologies.Count}思想はどこへ行くか（★世界に依存しない＝座標だけで決まる） ===");
    Console.WriteLine($"  親和の傾き slope = {paCoef.IdeologyAffinitySlope:F2}（Coefficients.IdeologyAffinitySlope・既存）");
    Console.WriteLine("  " + JpPad("思想", 14) + JpPad("参加", 6) + JpPad("強権", 6) + JpPad("国家軍", 8)
        + "｜非闘争→ " + JpPad("王権", 8) + JpPad("議会", 8) + "｜闘争→ " + JpPad("軍部", 8) + JpPad("革命", 8));
    string[] paIdeoNames = ["0 伝統", "1 反動", "2 自由", "3 共和", "4 ナショ", "5 社会", "6 共産", "7 無政府", "8 帝国", "9 ファシズム", "10 王党派"];
    for (int k = 0; k < Ideologies.Count; k++)
    {
        var d = PowerAffinity.IdeologyDestination(k, paCoef.IdeologyAffinitySlope);
        var one = new double[Ideologies.Count]; one[k] = 1.0;
        var ip = Ideologies.IdealPoint(one);
        Console.WriteLine("  " + JpPad(paIdeoNames[k], 14) + JpPad($"{ip[0]:F2}", 6) + JpPad($"{ip[1]:F2}", 6) + JpPad($"{ip[3]:F2}", 8)
            + "｜        " + JpPad($"{d[0]:P0}", 8) + JpPad($"{d[1]:P0}", 8) + "｜      " + JpPad($"{d[2]:P0}", 8) + JpPad($"{d[3]:P0}", 8));
    }

    var pam = new MultiNationSim(WorldData.Nations(), paSeed, _ => new UtilityAgent());
    foreach (var nn in pam.Nations) nn.TelemetryEnabled = false;
    pam.Diplomacy.Log.Enabled = false;
    pam.StartWarmup();
    World? PaFind(string nm) => pam.Nations
        .FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == nm)?.World;
    var paQuietWorst = new Dictionary<string, (double share, int year)>();
    var paParlTrace = new List<(int year, double gb, double fr, double pr, double jp, double allAvg)>();

    void PaDump(int year)
    {
        Console.WriteLine($"\n=== {year}年（seed{paSeed}）正当性の内訳（％） ===");
        Console.WriteLine("  " + JpPad("国名", 12) + JpPad("政体", 20)
            + "【連合前】" + JpPad("王権", 6) + JpPad("議会", 6) + JpPad("軍部", 6) + JpPad("革命", 6)
            + "｜【連合後】" + JpPad("王権", 6) + JpPad("議会", 6) + JpPad("軍部", 6) + JpPad("革命", 6)
            + "｜軍の同盟先" + JpPad("単独支配", 12) + "｜★導出政体" + JpPad("(現行と一致?)", 14));
        foreach (var nm in paWatch)
        {
            var w = PaFind(nm); if (w is null) continue;
            var c = PowerAffinity.Coalition(w);
            string sole = c.SoleRuler is { } sr ? PowerAffinity.HolderNames[(int)sr] + "★" : "連合政治";
            string armyAlly = c.Ally[(int)PowerAffinity.Holder.Army] == PowerAffinity.Holder.Army
                ? "単独" : PowerAffinity.HolderNames[(int)c.Ally[(int)PowerAffinity.Holder.Army]];
            Console.WriteLine("  " + JpPad(w.SelfPower!.Name, 12) + JpPad(w.Polity.ToString(), 20)
                + "          " + JpPad($"{c.Raw[0] * 100:F0}", 6) + JpPad($"{c.Raw[1] * 100:F0}", 6)
                + JpPad($"{c.Raw[2] * 100:F0}", 6) + JpPad($"{c.Raw[3] * 100:F0}", 6)
                + "｜          " + JpPad($"{c.Effective[0] * 100:F0}", 6) + JpPad($"{c.Effective[1] * 100:F0}", 6)
                + JpPad($"{c.Effective[2] * 100:F0}", 6) + JpPad($"{c.Effective[3] * 100:F0}", 6)
                + "｜          " + JpPad(armyAlly, 10) + JpPad(sole, 12)
                // ★★★権力シェアからの導出政体（作者裁定2026-08-19）と現行ラベルの食い違いを並べる。
                //   ⚠**忠実度の政体次元を導出へ切り替えると前後で数字が比較不能になる**ので、切り替えの前にここで影響を見積もる。
                + "｜" + JpPad(PowerAffinity.PolityFromShares(w).ToString(), 22)
                + JpPad(PowerAffinity.PolityFromShares(w) == w.Polity ? "一致" : "⚠不一致", 8));
            // ★★★予算権と統帥権（作者設計 2026-08-20）＝Lc/Lp × 法の重み。⚠独立ストックを持たない。
            var (l2c, l2p) = PowerAffinity.Legitimacy2(w);
            var (bC, bP, bA) = PowerAffinity.BudgetShares(w);
            var (mC, mP, mA) = PowerAffinity.CommandShares(w);
            Console.WriteLine($"      正統性 王権{l2c * 100,3:F0}:議会{l2p * 100,3:F0}"
                + $"  ｜予算法{JpPad(w.BudgetLaw.ToString(), 14)}→ 予算権 王{bC * 100,3:F0} 議{bP * 100,3:F0} 軍{bA * 100,3:F0}"
                + $"  ｜統帥法{JpPad(w.CommandLaw.ToString(), 11)}→ 統帥権 王{mC * 100,3:F0} 議{mP * 100,3:F0} 軍{mA * 100,3:F0}"
                + $"  ｜軍の帰属{w.ArmyAllegiance,5:F2} 文民制度{PowerShares.CivilianInstitutions(w):F2}"
                + (PowerAffinity.ArmyHoldsCommand(w) ? "  ★軍が指揮権を独占" : ""));
        }
        // 検算：全国で合計1・[0,1]
        // ★導出政体と現行ラベルの一致率（全国）＝忠実度の物差しを切り替えたときの影響の見積り。
        int paAgree = 0, paTot = 0;
        foreach (var n in pam.Nations)
            if (n.World.SelfPower is not null) { paTot++; if (PowerAffinity.PolityFromShares(n.World) == n.World.Polity) paAgree++; }
        double worst = 0; string worstWho = ""; bool range = true;
        double parlSum = 0; int parlN = 0;
        foreach (var n in pam.Nations)
        {
            var sh = PowerAffinity.Shares(n.World);
            double sum = 0;
            for (int h = 0; h < PowerAffinity.HolderCount; h++) { sum += sh[h]; if (!(sh[h] >= 0 && sh[h] <= 1)) range = false; }
            double err = System.Math.Abs(sum - 1.0);
            if (err > worst) { worst = err; worstWho = n.World.SelfPower?.Name ?? "?"; }
            parlSum += sh[1]; parlN++;
        }
        Console.WriteLine($"  検算：合計1からの最大誤差 {worst:E1}（{worstWho}・全{pam.Nations.Count}カ国）　全セル[0,1] {(range ? "○" : "× 破れ")}　全世界の議会シェア平均 {(parlN > 0 ? parlSum / parlN : 0):P1}");
        {
            int nCrown = 0, nParl = 0, nArmy = 0, nRev = 0, nCoal = 0;
            foreach (var n in pam.Nations)
            {
                var cc2 = PowerAffinity.Coalition(n.World);
                if (cc2.SoleRuler is not { } sr2) { nCoal++; continue; }
                switch (sr2)
                {
                    case PowerAffinity.Holder.Crown: nCrown++; break;
                    case PowerAffinity.Holder.Parliament: nParl++; break;
                    case PowerAffinity.Holder.Army: nArmy++; break;
                    default: nRev++; break;
                }
            }
            Console.WriteLine($"  単独支配の内訳（全{pam.Nations.Count}カ国）：王権 {nCrown}　議会 {nParl}　★軍事独裁 {nArmy}　革命 {nRev}　｜連合政治 {nCoal}");
            // ★導出政体と現行ラベルの一致率＝**忠実度の政体次元を切り替えたときに何が起きるかの見積り**（作者裁定2026-08-19）。
            Console.WriteLine($"  ★導出政体と現行ラベルの一致：{paAgree}/{paTot}（{(paTot > 0 ? 100.0 * paAgree / paTot : 0):F1} %）");
            // ★★★計器（2026-08-20）：閾値の形を決める前に**実分布を測る**。⚠合成量のレンジを測らずに閾値を置いて3度外した反省。
            //   軍の帰属 |ArmyAllegiance| と、文民の統帥（Lc×統帥重み(王)+Lp×統帥重み(議)）と、文民制度の3つを並べる。
            {
                var la = new List<double>(); var lm = new List<double>(); var lv = new List<double>(); var lc2 = new List<double>();
                foreach (var n in pam.Nations)
                {
                    var w2 = n.World; if (w2.SelfPower is null) continue;
                    la.Add(Math.Abs(w2.ArmyAllegiance));
                    var (q, r) = PowerAffinity.Legitimacy2(w2);
                    var (u, v) = PowerAffinity.CommandWeights(w2.CommandLaw);
                    lm.Add(q * u + r * v);
                    lv.Add(PowerShares.CivilianInstitutions(w2));
                    var sh2 = PowerAffinity.Shares(w2);
                    lc2.Add(sh2[(int)PowerAffinity.Holder.Crown] + sh2[(int)PowerAffinity.Holder.Parliament]);
                }
                static string Q(List<double> xs)
                {
                    if (xs.Count == 0) return "n=0";
                    xs.Sort();
                    double At(double f) => xs[Math.Clamp((int)(f * (xs.Count - 1)), 0, xs.Count - 1)];
                    double mean = 0; foreach (var x in xs) mean += x; mean /= xs.Count;
                    return $"最小{xs[0]:F2} 下位25%{At(0.25):F2} 中央{At(0.5):F2} 上位25%{At(0.75):F2} 最大{xs[^1]:F2} 平均{mean:F2}";
                }
                Console.WriteLine($"  ★計器 |軍の帰属|   {Q(la)}");
                Console.WriteLine($"  ★計器 文民の統帥   {Q(lm)}");
                Console.WriteLine($"  ★計器 正統な担い手 {Q(lc2)}   ⚠これが文民制度を下回ると軍が指揮権を握る");
                // ★★★思想シェアの実分布（2026-08-20）＝**機構を作る前に、材料が実際に立つのかを測る**。
                //   ⚠「機構を入れたのに一度も発火しない」を本セッションで既に2件（軍事独裁・連合政治）踏んでいる。
                //   ファシズム(9)は FascismGate=0 ならシェア常時0、しかも当時の時代ゲートが1919年から15年＝**窓が最後の15年しかない**（2026-10-01 (152) から門は発見の2値）。
                {
                    var lf = new List<double>(); var lk = new List<double>(); var lan = new List<double>(); var lre = new List<double>();
                    foreach (var n in pam.Nations)
                    {
                        var w3 = n.World; if (w3.SelfPower is null) continue;
                        var agg = Ideologies.National(w3);
                        if (agg is null || agg.Length < 11) continue;
                        lf.Add(agg[9]); lk.Add(agg[6]); lan.Add(agg[7]); lre.Add(agg[1]);
                    }
                    Console.WriteLine($"  ★計器 ファシズム   {Q(lf)}");
                    // ★★反動の因子分解（2026-08-21）＝掛け算の相手を差し替えて外した反省。**各因子を単独で測る**：
                    //   反動＝2.0×闘争度×conserv（旧）／2.0×闘争度×(1−PrivilegeIntact)（新・担い手＝閉鎖階級と兵士）。
                    //   どの因子がゼロかが分かれば、直す場所が一意に決まる（推測しない）。
                    {
                        var lpi = new List<double>(); var lmC = new List<double>(); var lmS = new List<double>(); var lcv = new List<double>();
                        foreach (var n in pam.Nations)
                        {
                            var w4 = n.World; if (w4.SelfPower is null) continue;
                            lpi.Add(PowerShares.PrivilegeIntact(w4));
                            double mC = 0, sC = 0, mS = 0, sS = 0, sv = 0, tp = 0;   // ★2026-09-03：一度も読まれない `ts` を削除（CS0219＝死コード。警告を0に保つ＝本物の警告が埋もれない）
                            foreach (var pp in w4.AllPops())
                            {
                                if (pp.Closed) { mC += pp.Militancy * pp.Size; sC += pp.Size; }
                                if (pp.IsSoldier) { mS += pp.Militancy * pp.Size; sS += pp.Size; }
                                sv += System.Math.Max(0, pp.Savings); tp += pp.Size;
                            }
                            lmC.Add(sC > 1e-9 ? mC / sC : 0);
                            lmS.Add(sS > 1e-9 ? mS / sS : 0);
                            // conserv の全国加重平均（Ideology.cs と同じ式：rich/2 を[0,1]クランプ・富は平均比）
                            double meanW = tp > 1e-9 ? sv / tp : 1;
                            double cvW = 0;
                            foreach (var pp in w4.AllPops())
                            {
                                double wpc = pp.Size > 1e-9 ? System.Math.Max(0, pp.Savings) / pp.Size : 0;
                                double rel = meanW > 1e-9 ? wpc / meanW : 1;
                                double rich = System.Math.Clamp((rel - 1) / 3.0, 0, 1) * 3.0;
                                cvW += System.Math.Clamp(rich / 2.0, 0, 1) * pp.Size;
                            }
                            lcv.Add(tp > 1e-9 ? cvW / tp : 0);
                        }
                        Console.WriteLine($"  ★因子 特権保全     {Q(lpi)}   ⚠1.00に張り付くなら (1−これ)=0 で新経路が死ぬ");
                        Console.WriteLine($"  ★因子 閉鎖階級の闘争 {Q(lmC)}");
                        Console.WriteLine($"  ★因子 兵士の闘争   {Q(lmS)}");
                        Console.WriteLine($"  ★因子 conserv平均  {Q(lcv)}   ⚠旧経路の富因子（人口加重）");
                    }
                    Console.WriteLine($"  ★計器 共産主義     {Q(lk)}");
                    Console.WriteLine($"  ★計器 無政府       {Q(lan)}");
                    Console.WriteLine($"  ★計器 反動         {Q(lre)}");
                }
                Console.WriteLine($"  ★計器 文民制度     {Q(lv)}");
            }
        }
        foreach (var qn in paQuiet)
        {
            var w = PaFind(qn); if (w is null) continue;
            double a = PowerAffinity.Coalition(w).Effective[2];   // ★連合後＝「軍が単独で政治を左右しているか」
            if (!paQuietWorst.TryGetValue(qn, out var cur) || a > cur.share) paQuietWorst[qn] = (a, year);
        }
        {
            double G(string nm) { var w = PaFind(nm); return w is null ? double.NaN : PowerAffinity.Shares(w)[1]; }
            paParlTrace.Add((year, G("イギリス"), G("フランス"), G("プロイセン"), G("日本"), parlN > 0 ? parlSum / parlN : 0));
        }
        // ★検証点
        void PaCheck(string nm, string what, System.Func<World, (bool ok, string detail)> f)
        {
            var w = PaFind(nm);
            if (w is null) { Console.WriteLine($"  ★検証点[{what}]：{nm} はこの世界に存在しない（＝判定不能）"); return; }
            var (ok, detail) = f(w);
            Console.WriteLine($"  ★検証点[{what}]　{nm}　{(ok ? "○" : "×")}　{detail}");
        }
        if (year == 1815) PaCheck("イギリス", "英1815 議会が王権を上回るか（穀物法＝議会が実権）", w =>
        { var s = PowerAffinity.Shares(w); return (s[1] > s[0], $"王権 {s[0]:P0} / 議会 {s[1]:P0}"); });
        if (year == 1867) PaCheck("プロイセン", "普1867 王権が議会を上回るか（憲法紛争＝議会が敗北）", w =>
        { var s = PowerAffinity.Shares(w); return (s[0] > s[1], $"王権 {s[0]:P0} / 議会 {s[1]:P0}"); });
        if (year == 1917) PaCheck("ロシア", "露1917 革命が最大になるか", w =>
        { var s = PowerAffinity.Shares(w); return (s[3] >= System.Math.Max(s[0], System.Math.Max(s[1], s[2])), $"王権 {s[0]:P0} 議会 {s[1]:P0} 軍部 {s[2]:P0} 革命 {s[3]:P0}"); });
        if (year == 1934) PaCheck("日本", "日1934 軍部が議会を上回るか", w =>
        { var s = PowerAffinity.Shares(w); return (s[2] > s[1], $"議会 {s[1]:P0} / 軍部 {s[2]:P0}"); });
    }

    int paQ = 0;
    foreach (int y in paYears)
    {
        if (y > paEnd) break;
        int target = (y - 1783) * 4;
        for (; paQ < target; paQ++) pam.StepQuarter();
        PaDump(y);
    }

    Console.WriteLine("\n=== ③ ★★★ラチェットの検査：議会シェアは単調増加していないか ===");
    Console.WriteLine("  " + JpPad("年", 6) + JpPad("英", 8) + JpPad("仏", 8) + JpPad("普", 8) + JpPad("日", 8) + JpPad("全世界平均", 12) + "  判定");
    double prevAvg = -1; int paUp = 0, paDown = 0;
    foreach (var t in paParlTrace)
    {
        string mark = prevAvg < 0 ? "" : (t.allAvg > prevAvg ? "↑" : (t.allAvg < prevAvg ? "↓" : "="));
        if (prevAvg >= 0) { if (t.allAvg > prevAvg) paUp++; else if (t.allAvg < prevAvg) paDown++; }
        Console.WriteLine("  " + JpPad($"{t.year}", 6)
            + JpPad(double.IsNaN(t.gb) ? "-" : $"{t.gb:P0}", 8) + JpPad(double.IsNaN(t.fr) ? "-" : $"{t.fr:P0}", 8)
            + JpPad(double.IsNaN(t.pr) ? "-" : $"{t.pr:P0}", 8) + JpPad(double.IsNaN(t.jp) ? "-" : $"{t.jp:P0}", 8)
            + JpPad($"{t.allAvg:P1}", 12) + "  " + mark);
        prevAvg = t.allAvg;
    }
    Console.WriteLine($"  ★判定：全世界平均の上昇 {paUp} 回 / 下降 {paDown} 回　"
        + (paDown > 0 ? "○ 単調ではない（王権が盛り返す局面がある）" : "⚠ 単調増加＝ラチェットが残っている"));

    Console.WriteLine("\n=== ④ ★★★硬性条件：軍の政治シェアが全期間ほぼ0であるべき国 ===");
    bool paQuietOk = true;
    foreach (var qn in paQuiet)
    {
        if (!paQuietWorst.TryGetValue(qn, out var v)) { Console.WriteLine($"  {JpPad(qn, 16)}この世界に存在しない（＝判定対象外）"); continue; }
        bool ok = v.share < paQuietMax;
        if (!ok) paQuietOk = false;
        Console.WriteLine($"  {JpPad(qn, 16)}最悪 {v.share:P1}（{v.year}年）　{(ok ? "○" : "× 赤")}");
    }
    Console.WriteLine($"  ★硬性条件：{(paQuietOk ? "○ 全通過" : "× 破れあり")}");
    return;
}

// 【削除2026-09-26（棚卸し pol-09）】powershares（権力の取り分の表モデル＝階級×機関の表・取り分・軍政の判定の観測）＝表モデルごと削除。
//   同じ問いの現行の計器は poweraff（PowerAffinity＝正統性の按分・軍の独占）。旧い計器は `git show b7fd1c8:PriceOfBread.Runner/Program.cs` にある。

// === 初期整合の検算： dotnet run -- initcheck [seed] ===
// ★作者裁定2026-08-21（初期整合パス「これは作る」）の**第1段＝検算と警告**。
//   ⚠「手置きの限界」の正体＝結果（識字・国債・特権）だけを置いて、それを支える在庫（教育供給・受益・政治力）を置いていないこと。
//   ★EducationFill は**フロー**（毎月0にリセット→購入で埋め直し）なので「初期値を合わせる」対処は存在しない＝
//     支えの実体は**教育市場の供給力**。⇒ 助走直後の実勢を測り、識字との差＝**支えの無い識字**（＝減衰で失われる分）を国別に警告する。
//   ⚠第2段（支えの逆算配置＝教師POPの規模づけ等）は Pops.Count の罠（POP追加＝全世界の軌道が動く）があるため、
//     この検算で不整合の大きさを見てから設計する（再較正級）。
if (args.Length > 0 && args[0] == "initcheck")
{
    ulong icSeed = ArgUlong(args, 1, 1789UL);
    var icsim = new MultiNationSim(WorldData.Nations(), icSeed, _ => new UtilityAgent());
    foreach (var nn in icsim.Nations) nn.TelemetryEnabled = false;
    icsim.Diplomacy.Log.Enabled = false;
    icsim.StartWarmup();
    // 助走＋2年＝教育市場が実勢に達した直後の断面（減衰の半減期29年に対して十分短い＝まだ識字は初期値に近い）
    for (int q = 0; q < 8; q++) icsim.StepQuarter();
    Console.WriteLine("=== 初期整合の検算（識字 対 教育市場の供給力・助走+2年断面） ===");
    Console.WriteLine("  " + JpPad("国名", 14) + "実勢識字  教育充足  ⚠支えの無い識字（=減衰で失われる分）");
    int icBad = 0, icTot = 0;
    var icRows = new List<(string nm, double lit, double fill, double gap)>();
    foreach (var n in icsim.Nations)
    {
        var w = n.World; if (w.SelfPower is null) continue;
        double lit = 0, fill = 0, size = 0;
        foreach (var pp in w.AllPops()) { lit += pp.Literacy * pp.Size; fill += pp.EducationFill * pp.Size; size += pp.Size; }
        if (!(size > 1e-9)) continue;
        lit /= size; fill /= size;
        double gap = System.Math.Max(0, lit - fill);
        icRows.Add((w.SelfPower.BaseName.Length > 0 ? w.SelfPower.BaseName : w.SelfPower.Name, lit, fill, gap));
    }
    foreach (var r in icRows.OrderByDescending(x => x.gap))
    {
        icTot++;
        bool bad = r.gap > 0.10;
        if (bad) icBad++;
        Console.WriteLine("  " + JpPad(r.nm, 14) + $"{r.lit,7:F3}  {r.fill,7:F3}  {r.gap,7:F3}" + (bad ? "  ⚠不整合" : ""));
    }
    Console.WriteLine($"  ★不整合（gap>0.10）＝{icBad}/{icTot} カ国。⚠この分は「学校の裏付けの無い識字」として減衰で失われる");
    Console.WriteLine("  ★恒久解の候補＝①LiteracyDecayIsGenerational（減衰を世代交代分に限定・休眠実装済み）②教育供給の初期規模づけ（再較正級・未着手）");
    return;
}

// === 識字・政治意識の時系列とその入力： dotnet run -- eduprobe [seed] [終了年] ===
// ⚠**読み取り専用**（世界の状態を1つも書かない）。「日本だけ識字と政治意識が130年間上がらない」の診断（2026-08-19）。
// ★合成された指標から原因を推測しないため、素の構成要素だけを並べる：
//   (1) 識字の低下を「階級内の識字変化」と「階級構成の変化」へ**厳密分解**（初期構成固定／初期識字固定の2本を併記）。
//   (2) 意識 target ＝ (識字×変換係数×都市 ＋ 便乗) × 報道 の**各因子を素のまま**（Politics.Step の式と同じ順）。
//   (3) その上流＝教育法・教育財の生産量/価格/在庫・教師/聖職者POP・近代化法ゲート(SelfPlay)の各条件。
//   (4) 特権保全（PowerShares.PrivilegeIntact）の3つの内訳＝秩禄処分が起きない理由の所在。
if (args.Length > 0 && args[0] == "eduprobe")
{
    ulong epSeed = ArgUlong(args, 1, 1789UL);
    int epEnd = ArgInt(args, 2, 1936);
    int[] epYears = [1788, 1800, 1820, 1840, 1853, 1868, 1876, 1890, 1900, 1912, 1925, 1936];
    string[] epWatch = ["日本", "イギリス", "プロイセン", "ロシア", "清", "朝鮮"];
    const int EpN = 16;   // ClassId の上限（PopClass は 0..13）
    var epsim = new MultiNationSim(WorldData.Nations(), epSeed, _ => new UtilityAgent());
    foreach (var nn in epsim.Nations) nn.TelemetryEnabled = false;
    epsim.Diplomacy.Log.Enabled = false;
    epsim.StartWarmup();
    World? EpFind(string nm) => epsim.Nations
        .FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == nm)?.World;
    static string EpNum(double v, int wd = 6) => double.IsNaN(v) ? "算不能".PadLeft(wd) : v.ToString("F3").PadLeft(wd);
    static double EpDot(double[] a, double[] b) { double s = 0; for (int i = 0; i < a.Length && i < b.Length; i++) s += a[i] * b[i]; return s; }

    // 階級別の集計（規模・識字・教育充足・意識・子供比率）。ClassId でまとめる。
    static (double[] size, double[] lit, double[] fill, double[] cons, double[] child, string[] names, double tot) EpByClass(World w)
    {
        var size = new double[EpN]; var lit = new double[EpN]; var fill = new double[EpN];
        var cons = new double[EpN]; var child = new double[EpN]; var names = new string[EpN];
        for (int i = 0; i < EpN; i++) names[i] = "";
        double tot = 0;
        foreach (var p in w.AllPops())
        {
            int ci = p.ClassId; if (ci < 0 || ci >= EpN || !(p.Size > 0)) continue;
            size[ci] += p.Size; lit[ci] += p.Literacy * p.Size; fill[ci] += p.EducationFill * p.Size;
            cons[ci] += p.Consciousness * p.Size; child[ci] += p.AgeShare(0, 15) * p.Size;
            if (names[ci].Length == 0) names[ci] = p.ClassName;
            tot += p.Size;
        }
        for (int i = 0; i < EpN; i++) if (size[i] > 1e-9) { lit[i] /= size[i]; fill[i] /= size[i]; cons[i] /= size[i]; child[i] /= size[i]; }
        return (size, lit, fill, cons, child, names, tot);
    }
    // 教育財（94）の市場と生産の実測：生産量合計／世俗（教師）分／価格／在庫／需要／担い手POPの頭数
    static (double outTot, double outSecular, double price, double stock, double demand, double teacher, double clergy, double prod) EpEdu(World w)
    {
        double oT = 0, oS = 0, price = 0, stock = 0, demand = 0, teacher = 0, clergy = 0, prod = 0, priceW = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pr in r.Producers)
                if (pr.GoodId == Catalog.EducationId)
                {
                    oT += pr.Output; prod = pr.Productivity;
                    if (pr.LaborPopIndex >= 0 && pr.LaborPopIndex == r.TeacherPopIndex) oS += pr.Output;
                }
            if (Catalog.EducationId < r.Markets.Length)
            {
                var m = r.Markets[Catalog.EducationId];
                double wgt = r.SumPopulation();
                price += m.Price * wgt; priceW += wgt; stock += m.Stock; demand += m.LastDemand;
            }
            if (r.TeacherPopIndex >= 0 && r.TeacherPopIndex < r.Pops.Count) teacher += r.Pops[r.TeacherPopIndex].Size;
            if (r.ClergyPopIndex >= 0 && r.ClergyPopIndex < r.Pops.Count) clergy += r.Pops[r.ClergyPopIndex].Size;
        }
        return (oT, oS, priceW > 1e-9 ? price / priceW : double.NaN, stock, demand, teacher, clergy, prod);
    }
    // 初期（最初の断面）の階級別 識字と構成シェアを覚え、以後の分解に使う
    var epBaseLit = new System.Collections.Generic.Dictionary<string, double[]>();
    var epBaseShare = new System.Collections.Generic.Dictionary<string, double[]>();
    var epTrace = new System.Collections.Generic.List<string>();

    void EpRow(World w, string nm, int year)
    {
        var (size, lit, fill, cons, child, names, tot) = EpByClass(w);
        double avgLit = 0, avgFill = 0, avgCons = 0, avgNews = 0, urban = 0, avgMil = 0;
        foreach (var p in w.AllPops())
        {
            if (!(p.Size > 0)) continue;
            avgLit += p.Literacy * p.Size; avgFill += p.EducationFill * p.Size; avgCons += p.Consciousness * p.Size;
            avgNews += p.NewsFill * p.Size; avgMil += p.Militancy * p.Size; if (p.IsUrban) urban += p.Size;
        }
        if (tot > 1e-9) { avgLit /= tot; avgFill /= tot; avgCons /= tot; avgNews /= tot; avgMil /= tot; urban /= tot; }
        // 意識 target の因子（Politics.Step と同じ式・国平均の代表値）
        double convertNow = w.EducationLaw switch { EducationLaw.Public => 1.0, EducationLaw.Compulsory => 1.0, EducationLaw.Religious => 0.15, _ => 0.1 };
        double convStock = 0, convW = 0;
        foreach (var p in w.AllPops()) { if (!(p.Size > 0)) continue; convStock += (p.SchoolingConvStock >= 0 ? p.SchoolingConvStock : convertNow) * p.Size; convW += p.Size; }
        if (convW > 1e-9) convStock /= convW;
        double newsReach = w.Coef.NewsConsciousnessFloor + (1.0 - w.Coef.NewsConsciousnessFloor) * avgNews;
        double press = 0.75 + 0.5 * Politics.PressFreedom(w.Censorship) * newsReach;
        double urbanF = 1.0 + (w.Coef.UrbanConsciousnessBoost - 1.0) * urban;   // 都市率で加重した平均の都市倍率
        double targetApprox = System.Math.Clamp(avgLit * convStock * urbanF * press, 0, 1);
        var (oT, oS, price, stock, demand, teacher, clergy, prod) = EpEdu(w);
        double tension = w.Diplomacy is { } dip2 && w.SelfPower is { } sp2 ? dip2.MaxTension(sp2) : 0;
        double landTax = w.LandTax, progTax = w.ProgressiveTax;
        double serfV = w.SerfEmancipation switch { ReformLevel.None => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double estateV = w.Bureaucracy switch { BureaucracyLaw.Spoils => 1.0, BureaucracyLaw.MeritExam => 0.5, _ => 0.0 };
        double eduFund = w.EducationLaw == EducationLaw.Compulsory ? w.EducationSpending
            : w.EducationLaw == EducationLaw.Public ? w.Coef.PublicEduFundingShare * w.EducationSpending : 0;

        Console.WriteLine($"  {JpPad(nm, 10)}{JpPad(w.Polity.ToString(), 22)} 教育法{JpPad(w.EducationLaw.ToString(), 11)} 検閲{JpPad(w.Censorship.ToString(), 15)} 参政{JpPad(w.Suffrage.ToString(), 10)}"
            + $" 教義{JpPad(UtilityAgent.DoctrineFrom(w).ToString(), 14)}");
        Console.WriteLine($"      識字{EpNum(avgLit)} 教育充足{EpNum(avgFill)} 意識{EpNum(avgCons)}(target≈{EpNum(targetApprox)}) 闘争{EpNum(avgMil)}"
            + $" 新聞{EpNum(avgNews)} 都市率{EpNum(urban)} Elite{EpNum(w.Elite)} 技術{EpNum(w.TechLevel)} 人口{tot,10:F0} 緊張{tension,6:F1}");
        Console.WriteLine($"      意識の因子＝識字{EpNum(avgLit)} × 変換{EpNum(convStock)}(今期法の即時値{convertNow:F2}) × 都市{EpNum(urbanF)} × 報道{EpNum(press)}"
            + $"（報道自由{Politics.PressFreedom(w.Censorship):F2} × 紙の到達{newsReach:F2}）　意識速度{w.Coef.ConsciousnessSpeed:F3}/四半期");
        Console.WriteLine($"      教育財：生産{oT,9:F1}（うち世俗{oS,9:F1}）生産性{prod,5:F1} 価格{EpNum(price)} 在庫{stock,9:F1} 需要{demand,9:F1}"
            + $" 教師POP{teacher,9:F0} 聖職者POP{clergy,9:F0} 教育費{w.EducationSpending:F2} 義務財源{eduFund:F2}");
        Console.WriteLine($"      特権保全{PowerShares.PrivilegeIntact(w):F2}＝(農奴{serfV:F2}[{w.SerfEmancipation}] ＋ 免税{1 - System.Math.Clamp(landTax + progTax, 0, 1):F2}"
            + $"[地租{landTax:F2}(台帳{(w.LandTaxUnlocked ? "解禁" : "未")}) 累進{progTax:F2}] ＋ 身分{estateV:F2}[{w.Bureaucracy}]) / 3");
        // ★法の門（Government.GateInfo と PolicyPreferences.NetSupport＝施行と同じ単一ソースを**照会するだけ**・書き込みなし）
        // ⚠AIの目標（w.Policy）は GateAllLawTargets が「通らない段」を現行法へ畳んだ**後**の値なので、目標=現状でも
        //   「そもそも欲しくない」のか「欲しいが通らない」のかが区別できない。よって**常に1段上げの可否を照会**して両方出す。
        string EpLaw(PolicyAxis a, int cur, int want, int max, string label)
        {
            if (cur >= max) return $"{label}[最上段{cur}]";
            double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, true) : 1.0;
            var (ok, cost, reason) = Government.GateInfo(w, a, true, cur + 1);
            return $"{label}[現{cur}→{cur + 1} 純支持{net,6:F2} {(ok ? "○可" : "×否")}{(reason.Length > 0 ? "＝" + reason : "")} 政治資本費{cost:F2} AI目標{want}]";
        }
        var (blocT, blocM, blocMass) = InterestGroups.Blocs(w);
        Console.WriteLine($"      法の門：{EpLaw(PolicyAxis.EducationLaw, (int)w.EducationLaw, (int)w.Policy.EducationLaw, (int)EducationLaw.Compulsory, "教育法")}"
            + $" {EpLaw(PolicyAxis.Censorship, (int)w.Censorship, (int)w.Policy.Censorship, (int)CensorshipLaw.FreePress, "検閲")}"
            + $" {EpLaw(PolicyAxis.Suffrage, (int)w.Suffrage, (int)w.Policy.Suffrage, (int)Suffrage.Universal, "参政")}");
        Console.WriteLine($"      　　　　{EpLaw(PolicyAxis.Bureaucracy, (int)w.Bureaucracy, (int)w.Policy.Bureaucracy, (int)BureaucracyLaw.Professional, "官僚制")}"
            + $" {EpLaw(PolicyAxis.SerfEmancipation, (int)w.SerfEmancipation, (int)w.Policy.SerfEmancipation, (int)ReformLevel.Full, "農奴解放")}"
            + $" 地租目標{w.Policy.LandTaxTarget:F2} 教育費目標{w.Policy.EducationSpendingTarget:F2}");
        // ★N2（2026-09-21）：政治資本を在庫 a/b で出す。b は毎四半期 Government.Step の頭で World に置かれる（2026-09-21 ㉖で旗を焼き込み）。
        //   ⚠点火の証拠は「テスト緑」でも「帯が動いた」でもなく **門が実際に否決した回数**＝上の EpLaw の理由行（「政治資本の残りが不足」）を数えること。
        Console.WriteLine($"      ブロック：伝統{blocT:F3} 近代{blocM:F3} 大衆{blocMass:F3}　正統性{w.Legitimacy:F2}"
            + $" 政治資本{Math.Max(0, w.PoliticalCapitalMax - w.MandateSpent):F3}/{w.PoliticalCapitalMax:F3}（目減り{w.MandateSpent:F2} 実行度{PoliticalQuantities.PoliticalCapitalRate(w):F2}）"
            + $" 政治予算{w.PoliticalPushBudget:F2} 行政力{w.AdminEfficiency:F2} 議会統制{w.ParliamentControl:F2} 国庫{w.Treasury,12:F0} 一人当たりGDP{(tot > 1 ? w.Gdp / tot : 0):F4}");
        // ★政変の可否＝動員（改革派＋急進派・どちらも意識に比例）÷ 実効保持力。政体が変わらないと参政権が上がらない構造の直接の計器。
        double epCoerce = PoliticalQuantities.EffectiveCoerce(w, w.Coef);
        double epChal = w.ReformistPower + w.RadicalPower;
        Console.WriteLine($"      政変：動員{epChal,12:F0}（改革派{w.ReformistPower,11:F0} 急進派{w.RadicalPower,11:F0}）÷ 実効保持力{epCoerce,12:F0}"
            + $" ＝ 挑戦比{PoliticalQuantities.ChallengeRatio(epChal, epCoerce):F3}　治安維持力{w.BaseCoercion,9:F0} 軍の政治的優位{PoliticalQuantities.MilitaryPoliticalRatio(w):F3} 政体慣性{w.Coef.PolityInertia * w.Legitimacy:F3}");
        var sb = new System.Text.StringBuilder("      階級別 ");
        for (int i = 0; i < EpN; i++)
        {
            if (!(size[i] > 1e-9)) continue;
            sb.Append($"{names[i]}[{size[i] / System.Math.Max(1e-9, tot) * 100:F1}% 識{lit[i]:F2} 充{fill[i]:F2} 意{cons[i]:F2} 子{child[i]:F2}] ");
        }
        Console.WriteLine(sb.ToString());
        // ★識字の分解（初回断面を基準に固定）
        if (!epBaseLit.ContainsKey(nm))
        {
            epBaseLit[nm] = (double[])lit.Clone();
            var sh0 = new double[EpN]; for (int i = 0; i < EpN; i++) sh0[i] = tot > 1e-9 ? size[i] / tot : 0;
            epBaseShare[nm] = sh0;
        }
        {
            var bl = epBaseLit[nm]; var bs = epBaseShare[nm];
            var cur = new double[EpN];
            for (int i = 0; i < EpN; i++) cur[i] = tot > 1e-9 ? size[i] / tot : 0;
            double bsSum = 0, curSum = 0;
            for (int i = 0; i < EpN; i++) { bsSum += bs[i]; curSum += cur[i]; }
            double fixedWeights = bsSum > 1e-9 ? EpDot(bs, lit) / bsSum : double.NaN;   // 初期構成 × 今の階級内識字＝**階級内の変化だけ**
            double fixedLit = curSum > 1e-9 ? EpDot(cur, bl) / curSum : double.NaN;     // 今の構成 × 初期の階級内識字＝**構成の変化だけ**
            double baseAvg = bsSum > 1e-9 ? EpDot(bs, bl) / bsSum : double.NaN;
            epTrace.Add($"  {year,4} {JpPad(nm, 10)} 実際{EpNum(avgLit)}　階級内だけ動かすと{EpNum(fixedWeights)}　構成だけ動かすと{EpNum(fixedLit)}"
                + $"（初期の実際{EpNum(baseAvg)}）　教育充足{EpNum(avgFill)}　意識{EpNum(avgCons)}");
        }
    }

    int epQ = 0;
    foreach (int y in epYears)
    {
        if (y > epEnd) break;
        int target = (y - 1783) * 4;
        for (; epQ < target; epQ++) epsim.StepQuarter();
        Console.WriteLine($"\n=== {y}年（seed{epSeed}） ===");
        foreach (var nm in epWatch) { var w = EpFind(nm); if (w is not null) EpRow(w, nm, y); else Console.WriteLine($"  {nm}：この世界に存在しない"); }
    }
    Console.WriteLine("\n=== ★識字の分解（実際 ／ 階級内の識字だけ動かした値 ／ 階級構成だけ動かした値。初期断面の階級別識字と構成を基準に固定） ===");
    Console.WriteLine("   実際 ≈ 階級内だけ ならば低下は**識字そのものの減衰**／実際 ≈ 構成だけ ならば低下は**低識字層の相対増加**");
    foreach (var line in epTrace) Console.WriteLine(line);
    var epC = new Coefficients();
    Console.WriteLine($"\n  参考係数：識字の下がる側＝世代の流れ（2026-09-30 段4e 後半・旧 LiteracyGenerationalDecay）"
        + $"　識字の伸び速度（法別）なし0.0001/宗教0.004/公教育0.005/義務0.007　児童労働ペナルティ{epC.ChildLaborLiteracyPenalty}"
        + $"　意識の変換係数（法別）なし0.10/宗教0.15/公教育1.0/義務1.0");
    return;
}

// === 関税漂流の観測： dotnet run -- tariffprobe [seed=1789] [四半期=200] ===
// 「なぜ全員が関税100%へ漂うのか」＝主要国の関税・目標・純支持の**内訳**（SupportBreakdown）を時系列で出す。
if (args.Length > 0 && args[0] == "tariffprobe")
{
    ulong tseed = ArgUlong(args, 1, 1789UL);
    int tq = ArgInt(args, 2, 200);
    var tsim = new PriceOfBread.SimCore.MultiNationSim(PriceOfBread.SimCore.WorldData.Nations(), tseed, _ => new PriceOfBread.SimCore.UtilityAgent());
    tsim.StartWarmup();
    var watch = new[] { "イギリス", "フランス", "プロイセン", "清" };
    for (int q = 1; q <= tq; q++)
    {
        tsim.StepQuarter();
        if (q % 40 != 0) continue;
        System.Console.WriteLine($"--- {tsim.Nations[0].World.Year}年 Q{q} ---");
        foreach (var nm in watch)
        {
            PriceOfBread.SimCore.World? w2 = null;
            foreach (var n2 in tsim.Nations) if (n2.World.SelfPower?.Name == nm) { w2 = n2.World; break; }
            if (w2 is null) continue;
            double net = PriceOfBread.SimCore.PolicyPreferences.NetSupport(w2, PriceOfBread.SimCore.PolicyAxis.Tariff, increasing: true);
            var parts = new System.Collections.Generic.List<string>();
            foreach (var (who, wt2) in PriceOfBread.SimCore.PolicyPreferences.SupportBreakdown(w2, PriceOfBread.SimCore.PolicyAxis.Tariff, increasing: true))
                if (System.Math.Abs(wt2) > 0.01) parts.Add($"{who}{wt2:+0.00;-0.00}");
            System.Console.WriteLine($"  {nm}: 関税{w2.Tariff:P0} 目標{w2.Policy.TariffTarget:P0} 報復偏倚{w2.TariffRetaliationBias:+0.00;-0.00} 条約港{w2.TreatyPortsGranted} 与党{w2.GoverningParty} 輸出/GDP{(w2.NominalGdp > 1e-9 ? w2.LastExportValue / w2.NominalGdp : 0):P1} 純支持{net:+0.00;-0.00} ＝ {string.Join(" ", parts)}");
        }
    }
    return;
}

// === ペア貿易行列の観測： dotnet run -- pairprobe [seed=1789] [四半期=300] [表示間隔Q=60] ===
// diffprobe実測2026-08-31「英発の思想が仏露へ75年届かない」の原因究明＝欧州内ペア（英仏/英独）が英清より細いのか、
// いつからか、何が太いのかを時系列で直接見る（DiffusionWeight=1で記帳を活かし、表示間隔ごとに上位ペアを出力）。
if (args.Length > 0 && args[0] == "pairprobe")
{
    ulong pseed = ArgUlong(args, 1, 1789UL);
    int pq = ArgInt(args, 2, 300);
    int pint = ArgInt(args, 3, 60);
    var psim = new PriceOfBread.SimCore.MultiNationSim(PriceOfBread.SimCore.WorldData.Nations(), pseed,
        _ => new PriceOfBread.SimCore.UtilityAgent(), coef: new PriceOfBread.SimCore.Coefficients { DiffusionWeight = 1e-12 });   // 記帳だけ活かす（伝播はreach種なし＝実質不発）
    psim.StartWarmup();
    string NameOf(int i2) => psim.Nations[i2].World.SelfPower?.Name ?? ("#" + i2);
    for (int q = 1; q <= pq; q++)
    {
        psim.StepQuarter();
        if (q % pint == 0 && psim.Market.LastPairFlow is { } pf)
        {
            var tops = new System.Collections.Generic.List<(double V, string P)>();
            double euro = 0, total = 0;
            var euroSet = new System.Collections.Generic.HashSet<string> { "イギリス", "フランス", "プロイセン", "オーストリア", "ネーデルラント", "スペイン", "サルデーニャ＝ピエモンテ", "スウェーデン", "デンマーク", "スイス", "ポーランド", "ポルトガル", "ジェノヴァ", "ライン諸邦", "両シチリア", "教皇領", "トスカーナ", "ヴェネツィア", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク", "ロシア", "オスマン", "イタリア王国", "ドイツ", "北ドイツ連邦" };
            int n2 = System.Math.Min(psim.Nations.Count, pf.Length);
            for (int a = 0; a < n2; a++)
                for (int b = 0; b < n2; b++)
                {
                    double v = pf[a][b];
                    if (v <= 0) continue;
                    total += v;
                    if (euroSet.Contains(NameOf(a)) && euroSet.Contains(NameOf(b))) euro += v;
                    tops.Add((v, NameOf(a) + "→" + NameOf(b)));
                }
            tops.Sort((x, y) => y.V.CompareTo(x.V));
            System.Console.WriteLine($"--- {psim.Nations[0].World.Year}年 Q{q}（前四半期の清算分）　総額 {total:F0}　欧州内シェア {(total > 1e-9 ? euro / total : 0):P1} ---");
            for (int t2 = 0; t2 < System.Math.Min(15, tops.Count); t2++) System.Console.WriteLine($"  {tops[t2].V,10:F0}  {tops[t2].P}");
            // 英の相手別上位も
            var uk = new System.Collections.Generic.List<(double V, string P)>();
            for (int a = 0; a < n2; a++) for (int b = 0; b < n2; b++)
                if (pf[a][b] > 0 && (NameOf(a) == "イギリス" || NameOf(b) == "イギリス")) uk.Add((pf[a][b], NameOf(a) + "→" + NameOf(b)));
            uk.Sort((x, y) => y.V.CompareTo(x.V));
            System.Console.WriteLine("  英の上位: " + string.Join("　", uk.GetRange(0, System.Math.Min(6, uk.Count)).ConvertAll(z => $"{z.P} {z.V:F0}")));
            // 上位輸出国の財内訳（州市場の LastExport×RefPrice＝直近清算の数量ベース）＝「何を売って世界一なのか」を名指しする
            var expTot = new System.Collections.Generic.Dictionary<int, double>();
            for (int a = 0; a < n2; a++) { double e2 = 0; for (int b = 0; b < n2; b++) e2 += pf[a][b]; expTot[a] = e2; }
            var topExp = new System.Collections.Generic.List<int>(expTot.Keys);
            topExp.Sort((x, y) => expTot[y].CompareTo(expTot[x]));
            for (int t3 = 0; t3 < System.Math.Min(3, topExp.Count); t3++)
            {
                int ei = topExp[t3];
                var gsum = new System.Collections.Generic.Dictionary<int, double>();
                foreach (var r2 in psim.Nations[ei].World.Regions)
                {
                    if (r2.State != PriceOfBread.SimCore.RegionState.Active) continue;
                    for (int g2 = 0; g2 < r2.Markets.Length; g2++)
                        if (r2.Markets[g2].LastExport > 1e-9)
                        { gsum.TryGetValue(g2, out double cur); gsum[g2] = cur + r2.Markets[g2].LastExport * PriceOfBread.SimCore.Catalog.RefPrices[g2]; }
                }
                var gl = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, double>>(gsum);
                gl.Sort((x, y) => y.Value.CompareTo(x.Value));
                var parts = new System.Collections.Generic.List<string>();
                for (int g3 = 0; g3 < System.Math.Min(6, gl.Count); g3++) parts.Add($"{PriceOfBread.SimCore.Catalog.Goods[gl[g3].Key].Name} {gl[g3].Value:F0}");
                var wE = psim.Nations[ei].World;
                double wageS = 0, wageN = 0;
                foreach (var r3 in wE.Regions) { if (r3.State != PriceOfBread.SimCore.RegionState.Active) continue; foreach (var p3 in r3.Pops) if (!p3.Closed && p3.Size > 1) { wageS += System.Math.Max(0, p3.Earned); wageN += p3.Size; } }
                double litS = 0, litN = 0, eduS = 0;   // 2026-09-05：識字・教育充足の列（世界工場の是正＝技術水準の根を見る）
                foreach (var lp in wE.AllPops()) { litS += lp.Literacy * lp.Size; eduS += lp.EducationFill * lp.Size; litN += lp.Size; }
                System.Console.WriteLine($"  輸出内訳 {NameOf(ei)}（計{expTot[ei]:F0}・関税{wE.Tariff:P0}・技術{wE.TechLevel:F2}・所得/人{(wageN > 1 ? wageS / wageN : 0):F3}・識字{(litN > 0 ? litS / litN : 0):P0}・教育充足{(litN > 0 ? eduS / litN : 0):P0}）: {string.Join("　", parts)}");
            }
        }
    }
    return;
}

// === 発見・段7の観測： dotnet run -- diffprobe [seed=1789] [発信国=イギリス] [イデオロギー番号=5] [四半期=120] ===
// 種まき（発信国の到達度=1固定＝段8の発明の代理）→貿易路×メディアの伝播で各国への到達四半期を実測。
// 「英で生まれた思想が仏独→露→やがて印中へ」の時差と経路依存（貿易が切れた国には届かない）を無人で確認する計器。
if (args.Length > 0 && args[0] == "diffprobe")
{
    ulong dseed = ArgUlong(args, 1, 1789UL);
    string dOrigin = args.Length > 2 ? args[2] : "イギリス";
    int dIdeo = ArgInt(args, 3, 5);
    // ★2026-10-01 (152)：到達は発見の表の思想と制度の行ごと（World.SpreadReach）＝思想の番号を思想の行の欄へ（門を持つ思想＝社会5・共産6・帝国8・ファシズム9 だけ）。
    int dSlot = Discovery.SpreadSlot(Discovery.IdeaRow((Ideology)dIdeo));
    if (dSlot < 0) { System.Console.WriteLine($"門を持たない思想（番号 {dIdeo}）＝到達の欄が無い（社会5・共産6・帝国8・ファシズム9）"); return; }
    int dq = ArgInt(args, 4, 120);
    var dsim = new PriceOfBread.SimCore.MultiNationSim(PriceOfBread.SimCore.WorldData.Nations(), dseed,
        _ => new PriceOfBread.SimCore.UtilityAgent(), coef: new PriceOfBread.SimCore.Coefficients { DiffusionWeight = 1.0 });
    dsim.StartWarmup();
    int dOi = -1;
    for (int i = 0; i < dsim.Nations.Count; i++) if (dsim.Nations[i].World.SelfPower?.Name == dOrigin) { dOi = i; break; }
    // ★2026-09-04（段8の受け入れ用）：発信国に "-" を渡すと**発信源を固定しない**＝段8の種（National シェア）だけで誰が起点になるかを見る（創発モード）。
    bool dEmergent = dOrigin == "-";
    if (!dEmergent && dOi < 0) { System.Console.WriteLine($"国が見つからない: {dOrigin}"); return; }
    int dn0 = dsim.Nations.Count;
    var dArr = new int?[dn0];
    for (int q = 0; q < dq; q++)
    {
        if (!dEmergent) dsim.Nations[dOi].World.SpreadReach[dSlot] = 1.0;   // 発信源は固定（再放射の種）。創発モードでは触らない
        dsim.StepQuarter();
        for (int i = 0; i < dn0; i++)
            if (dArr[i] is null && dsim.Nations[i].World.SpreadReach[dSlot] >= 0.1) dArr[i] = q;
    }
    var reached = new System.Collections.Generic.List<(string Name, int Q, double R)>();
    var missing = new System.Collections.Generic.List<string>();
    for (int i = 0; i < dn0; i++)
    {
        var nm = dsim.Nations[i].World.SelfPower?.Name ?? $"#{i}";
        if (dArr[i] is { } aq) reached.Add((nm, aq, dsim.Nations[i].World.SpreadReach[dSlot]));
        else missing.Add($"{nm}({dsim.Nations[i].World.SpreadReach[dSlot]:F3})");
    }
    reached.Sort((a, b) => a.Q.CompareTo(b.Q));
    System.Console.WriteLine($"=== diffprobe seed={dseed} 発信={dOrigin} イデオロギー={dIdeo} {dq}四半期 ===");
    System.Console.WriteLine($"到達（>=0.1）: {reached.Count}/{dn0}カ国");
    foreach (var (nm, q2, r2) in reached) System.Console.WriteLine($"  Q{q2,3}  {nm}　到達度 {r2:F2}");
    System.Console.WriteLine($"未到達: {string.Join("・", missing)}");
    return;
}

// === 最小満足度の犯人追跡： dotnet run -- satprobe <seed> ＝ 全長（GameQuarters）後の全国の満足度・分離フラグ・首都保持を一覧 ===
if (args.Length > 0 && args[0] == "satprobe")
{
    ulong spSeed = args.Length > 1 && ulong.TryParse(args[1], out var sps) ? sps : 8UL;
    // ★バッチと**同じ関数**（SelfPlayMatch.Run・lean）を呼ぶ＝手組みの再現で3度外した教訓。
    var mr = SelfPlayMatch.Run(spSeed, Coefficients.GameQuarters, null, lean: true);
    System.Console.WriteLine($"完走={mr.Completed} ハーネス issues={mr.HarnessIssues.Count}");
    foreach (var iss in mr.HarnessIssues) System.Console.WriteLine($"  {iss}");
    System.Console.WriteLine("首都州名	国名	満足	本国州数");
    foreach (var st in mr.Standings.OrderBy(x => x.Satisfaction).Take(10))
        System.Console.WriteLine($"{st.Name}	{st.Nation}	{st.Satisfaction:F2}	{st.HomeProvinces}");
    return;
}

// === 塹壕の観測： dotnet run -- trenchprobe [seed] ===
// doc 20260812_塹壕と戦車 の T2/T3。**いつ**塹壕が効くのかを測る。設計条件＝「1914以降だけ戦争が伸びる」。
// 時代区分ごとの戦争の平均継続月数と、上位段/tier3の**陸分**充足率の推移を出す。
if (args.Length > 0 && args[0] == "trenchprobe")
{
    ulong sdT3 = ArgUlong(args, 1, 1);
    var simT3 = new MultiNationSim(WorldData.Nations(), sdT3, _ => new UtilityAgent());
    simT3.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    var fillRows = new List<string>();
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        simT3.StepQuarter();
        if ((q + 1) % 80 != 0) continue;
        // ★2026-08-13の訂正：充足率は**首都州にしか書かれない**（MilitaryScale>0 が首都のみ・GarrisonProcurement=0）。
        //   全active州で平均すると上限が 55首都/190州 ≒ 29% の目盛りになり、実態の1/3以下に見える。
        //   下流の読み手（Diplomacy の質乗数・塹壕）は nw.Region＝Regions[0]＝首都を読むので、**首都で測るのが正しい**。
        //   分布も出す＝二値（満額の国と厳密に0の国）なら平均は実態を表さない。
        double ql = 0, t3 = 0; int n = 0, zero = 0, full = 0;
        foreach (var nn in simT3.Nations)
        {
            var cap = nn.World.Region;   // 首都州＝Regions[0]（機構が実際に読む州）
            if (cap is null) continue;
            ql += cap.MilitaryQualityLandFill; t3 += cap.MilitaryTier3LandFill; n++;
            if (cap.MilitaryQualityLandFill < 0.01) zero++;
            if (cap.MilitaryQualityLandFill > 0.9) full++;
        }
        if (n > 0) fillRows.Add($"{simT3.Nations[0].World.Year,6}  陸上位 {ql / n,6:P1}  陸tier3 {t3 / n,6:P1}   （{n}カ国中 ほぼ0が{zero} 満額が{full}）");
    }
    Console.WriteLine($"=== trenchprobe seed {sdT3}：装備充足率の推移（陸分と合算）===");
    foreach (var r in fillRows) Console.WriteLine("  " + r);

    (int lo, int hi, string name)[] eras = [(1783, 1860, "〜1860"), (1861, 1900, "1861-1900"), (1901, 1913, "1901-1913"), (1914, Calendar.EndYear, "1914-")];
    Console.WriteLine();
    Console.WriteLine("=== 戦争の長さ（時代区分・終戦年で分類）===");
    foreach (var (lo, hi, name) in eras)
    {
        int cnt = 0; double mo = 0, dead = 0;
        foreach (var w2 in simT3.Diplomacy.EndedWars)
            if (w2.EndYear >= lo && w2.EndYear <= hi) { cnt++; mo += w2.Months; dead += w2.DeadA + w2.DeadB; }
        Console.WriteLine($"  {name,-10} {cnt,4}件  平均 {(cnt > 0 ? mo / cnt : 0),6:F1}ヶ月  平均戦死 {(cnt > 0 ? dead / cnt : 0),8:F0}");
    }
    return;
}

// === 技術水準の時系列： dotnet run -- techtrace [seed] [四半期] ===
// ⚠実測2026-08-12：1883年にヒヴァ/ラオスの技術がフランスの2倍以上（3.4 vs 1.5）だった。
// どこで折れたのか＝英仏と小国の技術を並べて追う。識字と教育も併記（技術の入力）。
if (args.Length > 0 && args[0] == "techtrace")
{
    ulong sdT = ArgUlong(args, 1, 1);
    int qsT2 = ArgInt(args, 2, Coefficients.GameQuarters);
    string[] watch = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "日本", "ヒヴァ", "ラオス", "ダホメ王国"];
    var simT2 = new MultiNationSim(WorldData.Nations(), sdT, _ => new UtilityAgent());
    simT2.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    World? Find(string nm) => simT2.Nations.Select(n => n.World).FirstOrDefault(w => w.SelfPower?.Name == nm);
    System.Console.WriteLine($"{"年",6}" + string.Join("", watch.Select(nm => $"{nm,10}")));
    bool mid = false;
    for (int q = 0; q < qsT2; q++)
    {
        simT2.StepQuarter();
        if (!mid && simT2.Nations[0].World.Year == 1883) { mid = true; Table("1883年"); }
        if ((q + 1) % 80 != 0) continue;
        System.Console.WriteLine($"{simT2.Nations[0].World.Year,6}" + string.Join("", watch.Select(nm =>
            Find(nm) is { } w ? $"{w.TechLevel,10:F2}" : $"{"—",10}")));
    }
    Table($"{simT2.Nations[0].World.Year}年");

    // 全国を技術順に＝政体・報道法・規模・所得の並びが技術の並びと一致するかを見る（原因の確定と参照値の較正）
    void Table(string when)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"===== {when} =====");
        System.Console.WriteLine($"{"国",-14}{"技術",7}{"エリ",7}{"識字",6}{"教育法",-11}{"報道",-15}{"政体",-22}{"人口k",9}{"1人GDP",9}{"エリ人数",9}{"工業化",7}");
        foreach (var w in simT2.Nations.Select(n => n.World).Where(w => w.SelfPower is not null).OrderByDescending(w => w.TechLevel))
        {
            double lit = 0, sz = 0;
            foreach (var p in w.AllPops()) { lit += p.Literacy * p.Size; sz += p.Size; }
            double pop = w.TotalPopulation;
            System.Console.WriteLine($"{w.SelfPower!.Name,-14}{w.TechLevel,7:F2}{w.Elite,7:F3}{(sz > 0 ? lit / sz : 0),6:P0}{w.EducationLaw,-11}{w.Censorship,-15}{w.Polity,-22}{pop,9:N0}{(pop > 1 ? w.Gdp / pop : 0),9:F3}{w.Elite * pop,9:N0}{w.MeanIndustrialization,7:F2}");
        }
    }
    return;
}

// === 1人あたりGDPの分布： dotnet run -- percapprobe [seed] [四半期] ===
// 目的＝吸収能力の経済項（AbsorptionIncomeRef / AbsorptionIncomeWeight・TechEducation.IncomeAbsorption）の較正値を実測で決める。
// 参照値は「先進国が f≈1・自給自足の小国が f<1」を満たす必要があるので、**時代ごとの分布**（先端・中央・下端）を並べて出す。
// 読み方＝f = clamp(1人GDP / AbsorptionIncomeRef, 0, 1)。固定参照値が時代で死なないか（全国 f=1 になる年が来ないか）を見る。
if (args.Length > 0 && args[0] == "percapprobe")
{
    ulong sdP = ArgUlong(args, 1, 1);
    int qsP = ArgInt(args, 2, 528);
    string[] bigP = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "日本", "オーストリア", "ネーデルラント"];
    string[] smallP = ["ラオス", "ダホメ王国", "ヒヴァ", "モロッコ", "アフガニスタン", "ネパール", "エチオピア", "ブハラ", "オマーン"];
    double[] refsP = [4, 6, 8, 10, 12, 15, 20, 30];
    int[] yearsP = [1800, 1850, 1900, 1914];
    var doneP = new HashSet<int>();
    var simP = new MultiNationSim(WorldData.Nations(), sdP, _ => new UtilityAgent());
    foreach (var nn in simP.Nations) nn.TelemetryEnabled = false;
    simP.Diplomacy.Log.Enabled = false;
    simP.StartWarmup();   // 自己対戦バッチ・App と同じ開始盤面から測る
    void PcDump(string when)
    {
        var rows = simP.Nations.Select(n => n.World).Where(w => w.SelfPower is not null)
            .Select(w => { double pop = w.TotalPopulation; return (W: w, Pc: pop > 1 ? w.Gdp / pop : 0, Pop: pop); })
            .OrderByDescending(x => x.Pc).ToList();
        if (rows.Count == 0) return;
        double Pct(double f) { int i = (int)Math.Round((1 - f) * (rows.Count - 1)); return rows[Math.Clamp(i, 0, rows.Count - 1)].Pc; }
        double med = Pct(0.5), top = rows[0].Pc;
        Console.WriteLine();
        Console.WriteLine($"===== {when}（{rows.Count}カ国）=====");
        Console.WriteLine($"  分布 1人GDP: 最大{top,7:F3} p90={Pct(0.9),7:F3} p75={Pct(0.75),7:F3} 中央={med,7:F3} p25={Pct(0.25),7:F3} p10={Pct(0.1),7:F3} 最小={rows[^1].Pc,7:F3}　最大/中央={(med > 1e-9 ? top / med : 0):F2}倍　最大/最小={(rows[^1].Pc > 1e-9 ? top / rows[^1].Pc : 0):F2}倍");
        Console.WriteLine($"  参照値ごとの f=1 の国数（f=clamp(1人GDP/Ref)）: " + string.Join("　", refsP.Select(rf => $"Ref{rf:F0}→{rows.Count(x => x.Pc >= rf)}/{rows.Count}")));
        void Row(string tag, (World W, double Pc, double Pop) x, int rank)
        {
            var w = x.W;
            double absNow = TechEducation.AbsorptionOf(w, w.Coef);   // 現行（識字×報道のみ・所得項は休眠で1.0）
            Console.WriteLine($"  {tag}{rank,3}位 {JpPad(w.SelfPower!.BaseName, 16)}1人GDP={x.Pc,8:F3} 人口k={x.Pop,10:N0} GDP={w.Gdp,10:N0} エリ={w.Elite,5:F3} 報道={JpPad(w.Censorship.ToString(), 15)}吸収(現)={absNow,5:F3} 技術={w.TechLevel,5:F2} 工業化={w.MeanIndustrialization,5:F2}");
        }
        Console.WriteLine("  --- 主要国 ---");
        foreach (var nm in bigP) { int ix = rows.FindIndex(x => x.W.SelfPower!.BaseName == nm); if (ix >= 0) Row("", rows[ix], ix + 1); }
        Console.WriteLine("  --- 小国 ---");
        foreach (var nm in smallP) { int ix = rows.FindIndex(x => x.W.SelfPower!.BaseName == nm); if (ix >= 0) Row("", rows[ix], ix + 1); }
        Console.WriteLine("  --- 1人GDP 上位5 ---");
        for (int i = 0; i < Math.Min(5, rows.Count); i++) Row("", rows[i], i + 1);
        Console.WriteLine("  --- 1人GDP 下位5 ---");
        for (int i = Math.Max(0, rows.Count - 5); i < rows.Count; i++) Row("", rows[i], i + 1);
    }
    Console.WriteLine($"=== percapprobe seed {sdP}／{qsP}四半期（AbsorptionIncomeRef={new Coefficients().AbsorptionIncomeRef} Weight={new Coefficients().AbsorptionIncomeWeight}＝休眠のまま測る）===");
    PcDump("1783年（助走直後）");
    for (int q = 1; q <= qsP; q++)
    {
        simP.StepQuarter();
        int y = simP.Nations[0].World.Year;
        if (Array.IndexOf(yearsP, y) >= 0 && doneP.Add(y)) PcDump($"{y}年");
    }
    return;
}

// === 列強の顔ぶれの内訳： dotnet run -- rankprobe [seed] [四半期] ===
// ⚠実測2026-08-12：1883年の列強に「ラオス・ヒヴァ・ダホメ王国」が入り、仏露普米が居なかった。
// 国力スコア＝軍＋豊かさ＋金融＋技術。どの項が順位を作っているかを分解して出す（史実の列強も併記）。
if (args.Length > 0 && args[0] == "rankprobe")
{
    ulong sdR = ArgUlong(args, 1, 1);
    int qsR = ArgInt(args, 2, 400);
    var simR2 = new MultiNationSim(WorldData.Nations(), sdR, _ => new UtilityAgent());
    simR2.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < qsR; q++) simR2.StepQuarter();
    var dipR = simR2.Diplomacy;
    var rows = simR2.Nations.Select(n => n.World).Where(w => w.SelfPower is not null)
        .Select(w => (W: w, S: NationalScore.Of(w), M: NationalScore.Military(w), Wl: NationalScore.Wealth(w),
                      F: NationalScore.Finance(w), T: NationalScore.Tech(w)))
        .OrderByDescending(x => x.S).ToList();
    System.Console.WriteLine($"seed={sdR}　{simR2.Nations[0].World.Year}年　（列強の定員={simR2.Coef.GreatPowerCount}）");
    System.Console.WriteLine($"{"順",3}{"国",-16}{"スコア",8}{"軍",8}{"豊かさ",8}{"金融",7}{"技術",7}{"GDP",10}{"工業化",7}{"満足",7}{"植民地",6}");
    void Line(int rank, (World W, double S, double M, double Wl, double F, double T) x)
        => System.Console.WriteLine($"{rank,3}{x.W.SelfPower!.Name,-16}{x.S,8:F1}{x.M,8:F1}{x.Wl,8:F1}{x.F,7:F1}{x.T,7:F1}"
            + $"{x.W.Gdp,10:F0}{x.W.MeanIndustrialization,7:F2}{NationalScore.MeanSatisfaction(x.W),7:F2}{x.W.SelfPower!.Colonies,6}");
    for (int i = 0; i < Math.Min(12, rows.Count); i++) Line(i + 1, rows[i]);
    System.Console.WriteLine("--- 史実の列強（1914年の8カ国）---");
    foreach (var nm in new[] { "イギリス", "フランス", "ロシア", "プロイセン", "オーストリア", "アメリカ", "日本", "イタリア" })
    {
        int ix = rows.FindIndex(x => x.W.SelfPower!.Name == nm);
        if (ix >= 0 && ix >= 12) Line(ix + 1, rows[ix]);
    }
    return;
}

// === アフリカ勢力の運命（A1基準線）： dotnet run -- africaprobe [seed] [四半期] ===
// docs/202608/20260812_アフリカ植民_完了.md【完了】.md の実施順 A1＝「現状のアフリカ勢力9件が604四半期でどうなるか」。
// ★抽象標的は軍が静的（Diplomacy.cs の Military 書き手は Nation!=null の枝のみ）＝取られるだけの器。
//   その空洞さを数字で残す＝いつ・誰に・どう吸収されたか。案Aの前後で同じ物差しを見る。
if (args.Length > 0 && args[0] == "africaprobe")
{
    string[] afrNations = ["モロッコ", "エチオピア", "マダガスカル", "ダホメ王国", "アシャンティ王国"];   // ★案Aで格上げした国はここへ移す
    string[] afrTargets = ["バルバリ", "ソコト", "コンゴ", "南アフリカ"];
    ulong sdA = ArgUlong(args, 1, 1);
    int qsA = ArgInt(args, 2, Coefficients.GameQuarters);
    var simA = new MultiNationSim(WorldData.Nations(), sdA, _ => new UtilityAgent());
    simA.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    var dipA = simA.Diplomacy;
    var fell = new Dictionary<string, string>();   // 名前 → 「Q123 に イギリス へ」
    for (int q = 0; q < qsA; q++)
    {
        simA.StepQuarter();
        foreach (var p in dipA.Powers)
        {
            string nm = p.Name;
            if (Array.IndexOf(afrNations, nm) < 0 && Array.IndexOf(afrTargets, nm) < 0) continue;
            if (fell.ContainsKey(nm)) continue;
            if (p.Nation is not null)
            {
                var suz = dipA.Suzerain(p);
                if (suz is not null) fell[nm] = $"Q{q + 1} {suz.Name}の属国";
                else if (p.Nation.Regions.All(r => r.State != RegionState.Active)) fell[nm] = $"Q{q + 1} 州を全て喪失";
            }
            else if (p.ColonyList.Count == 0) fell[nm] = $"Q{q + 1} 領土なし(接収済み)";
        }
    }
    // ★属国の枠（MaxDependents）が埋まっていないか＝弱小・軍ゼロでも取られない理由の第一候補。
    System.Console.WriteLine($"seed={sdA}　{qsA}四半期（{simA.Nations[0].World.Year}年まで）　属国の上限={simA.Coef.MaxDependents}");
    var holders = dipA.Powers.Where(p => p.Nation is not null)
        .Select(p => (p.Name, N: dipA.Powers.Count(q => dipA.Suzerain(q) == p)))
        .Where(x => x.N > 0).OrderByDescending(x => x.N).ToList();
    System.Console.WriteLine("宗主の属国数: " + string.Join("・", holders.Select(x => $"{x.Name}{x.N}")));
    // ★到達可能性（Projection×遠征技術）＝AiColonizeMinProjection(0.2) を割ると**候補にすら入らない**。
    //   弱小・軍ゼロなのに取られない理由の本命。列強から見たアフリカ各国の値を並べる。
    foreach (var tgt in new[] { "アシャンティ王国", "エチオピア", "ダホメ王国", "マダガスカル" })
    {
        var ty = dipA.Powers.FirstOrDefault(p => p.Name == tgt);
        if (ty is null) continue;
        var line = dipA.Powers.Where(p => p.Nation is not null && dipA.RankOf(p) == PowerRank.Great)
            .Select(p => $"{p.Name} {dipA.Projection(p, ty, simA.Coef):F2}").ToList();
        System.Console.WriteLine($"到達可能性→{tgt}: " + (line.Count > 0 ? string.Join("・", line) : "列強なし"));
    }
    System.Console.WriteLine($"{"勢力",-14}{"区分",-8}{"ランク",-6}{"転落",-22}{"軍",7}{"人口",10}{"植民地",7}");
    foreach (var nm in afrNations.Concat(afrTargets))
    {
        var p = dipA.Powers.FirstOrDefault(x => x.Name == nm);
        if (p is null) { System.Console.WriteLine($"{nm,-14}{"（消滅）",-8}"); continue; }
        string kind = p.Nation is null ? "抽象" : "国";
        // ★ランクも出す（2026-08-12）＝植民地化の標的は Minor だけ。人口が増えて Middle へ上がると
        //   「軍ゼロなのに誰にも狙われない」が起こる＝アフリカが取れなくなる別の歪みになる。
        string rk = dipA.RankOf(p) switch { PowerRank.Great => "列強", PowerRank.Secondary => "準列強", PowerRank.Middle => "中堅", _ => "弱小" };
        double pop = p.Nation is { } w2 ? w2.Regions.Where(r => r.State == RegionState.Active).Sum(r => r.SumPopulation()) : 0;
        System.Console.WriteLine($"{nm,-14}{kind,-8}{rk,-6}{fell.GetValueOrDefault(nm, "存続"),-22}{p.Military,7:F1}{pop,10:F0}{p.ColonyList.Count,7}");
    }
    return;
}

// === 冬季に凍る港の実測： dotnet run -- freezeprobe ===
if (args.Length > 0 && args[0] == "freezeprobe")
{
    var simF = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    System.Console.WriteLine($"WinterPortFreeze={simF.Coef.WinterPortFreeze}");
    System.Console.WriteLine($"{"港",-16}{"国",-12}{"緯度",7}{"経度",8}{"閉じる四半期",12}");
    int frozen = 0;
    foreach (var n in simF.Nations)
        foreach (var r in n.World.Regions)
        {
            if (r.State != RegionState.Active || !r.IsPort) continue;
            var cc = Climate.CityCoord(r.Name);
            int q = Climate.WinterFrozenQuarters(cc.Lat, cc.Lon);
            if (q <= 0) continue;
            frozen++;
            System.Console.WriteLine($"{r.Name,-16}{n.World.SelfPower?.Name,-12}{cc.Lat,7:F1}{cc.Lon,8:F1}{q,12}");
        }
    System.Console.WriteLine($"→ 凍る港 {frozen} 件（残りは通年）");
    return;
}

// === 内陸国の港利用（トランジット）の実測： dotnet run -- transitprobe ===
// 自国に港が無い州が「どの外国の港を借りて、いくら通行料を払っているか」を出す。
// 二国間交易では、これが無い国は対外交易そのものが立たない＝この表が空なら内陸国は鎖国している。
if (args.Length > 0 && args[0] == "transitprobe")
{
    var simT = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    simT.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    int qsT = ArgInt(args, 1, 1);   // 第2引数＝進める四半期数（鉄道が伸びた後を見る用）
    for (int q = 0; q < qsT; q++) simT.StepQuarter();
    System.Console.WriteLine($"{simT.Nations[0].World.Year}年時点");
    var owner = new Dictionary<string, string>();
    foreach (var n in simT.Nations)
        foreach (var r in n.World.Regions)
            if (r.State == RegionState.Active && r.IsPort) owner.TryAdd(r.Name, n.World.SelfPower?.Name ?? "?");
    System.Console.WriteLine($"TransitEnabled={simT.Coef.TransitEnabled}　通行料率={simT.Coef.TransitTollShare:P0}");
    System.Console.WriteLine($"{"州",-14}{"国",-12}{"借りた港",-14}{"港の所有国",-12}{"陸路km",8}{"通行料/期",12}");
    int used = 0, unlinked = 0;
    foreach (var n in simT.Nations)
        foreach (var r in n.World.Regions)
        {
            if (r.State != RegionState.Active || r.IsPort || r.NearestPortName.Length > 0) continue;   // 自国に港がある州は対象外
            if (r.LastTransitPortName.Length == 0) { unlinked++; continue; }
            used++;
            System.Console.WriteLine($"{r.Name,-14}{n.World.SelfPower?.Name,-12}{r.LastTransitPortName,-14}"
                + $"{owner.GetValueOrDefault(r.LastTransitPortName, "?"),-12}{r.TransitLandKm,8:F0}{simT.Coef.TransitTollShare * r.LastPortThroughput,12:F0}");
        }
    System.Console.WriteLine($"→ 港を借りた州 {used} 件／借りられなかった州 {unlinked} 件（後者は対外交易なし）");
    return;
}

// === 海路距離キャッシュのベンチ：港どうしの海路距離を全ペア計算した時のコスト： dotnet run -- routecost ===
// 二国間交易は買い手×売り手のペア距離を毎期引くので、全港ペア N(N-1)/2 通りの計算時間を実測する。
if (args.Length > 0 && args[0] == "routecost")
{
    var simR = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
    var ports = new List<(string Name, double Lat, double Lon)>();
    foreach (var n in simR.Nations)
        foreach (var r in n.World.Regions)
        {
            if (r.State != RegionState.Active || !r.IsPort) continue;
            var cc = Climate.CityCoord(r.Name);
            if (cc.Lat != 0 || cc.Lon != 0) ports.Add((r.Name, cc.Lat, cc.Lon));
        }
    System.Console.WriteLine($"港州 {ports.Count} 件／全ペア {ports.Count * (ports.Count - 1) / 2} 通り");
    var swR = System.Diagnostics.Stopwatch.StartNew();
    double sum = 0; int cnt = 0; double mx = 0; string mxPair = "";
    for (int i = 0; i < ports.Count; i++)
        for (int j = i + 1; j < ports.Count; j++)
        {
            double d = Geo.SeaDistanceKm(ports[i].Lat, ports[i].Lon, ports[j].Lat, ports[j].Lon, true, true);
            sum += d; cnt++;
            if (d > mx) { mx = d; mxPair = $"{ports[i].Name}↔{ports[j].Name}"; }
        }
    long ms1 = swR.ElapsedMilliseconds;
    swR.Restart();
    for (int i = 0; i < ports.Count; i++)
        for (int j = i + 1; j < ports.Count; j++)
            Geo.SeaDistanceKm(ports[i].Lat, ports[i].Lon, ports[j].Lat, ports[j].Lon, true, true);
    long ms2 = swR.ElapsedMilliseconds;
    System.Console.WriteLine($"初回 {ms1}ms／2回目 {ms2}ms（内部キャッシュの効き）　平均 {(cnt > 0 ? sum / cnt : 0):F0}km　最遠 {mx:F0}km（{mxPair}）");
    System.Console.WriteLine($"→ 1ヶ月あたりの追加コストの目安：全ペアを毎月引き直すなら {ms2}ms×12ヶ月×{Coefficients.GameQuarters}四半期/4 = {ms2 * Calendar.MonthsPerYear * (Coefficients.GameQuarters / Calendar.QuartersPerYear) / 1000.0:F0}秒/試合");
    return;
}

// === 国家選択画面の素材：開始時（1783）のランクと難易度の素点： dotnet run -- selectprobe ===
// 選択画面が出す「強さ（列強/中堅/弱小）」と「難易度」は、どちらも固定表でなく開始条件からの導出。
// ここはその導出を数字で見るための計器（世界の構築コストも測る＝画面に載せてよい重さかの判断材料）。
if (args.Length > 0 && args[0] == "selectprobe")
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var cfgs0 = WorldData.Nations();
    long tCfg = sw.ElapsedMilliseconds;
    var coef0 = new Coefficients();   // ★1本だけ構築して使い回す（POB_COEF反映後は全呼び出しで値が同一）
    var ranks = NationBrief.Ranks(cfgs0, coef0);
    long tRank = sw.ElapsedMilliseconds;
    var diff = NationBrief.Difficulties(cfgs0, coef0);
    System.Console.WriteLine($"world.json読込 {tCfg}ms／ランク算出（世界の構築込み）{tRank - tCfg}ms");
    System.Console.WriteLine($"{"国",-14}{"強さ",-6}{"難易度",-8}{"素点",8}{"資源",7}{"分離",7}{"安全",7}{"負荷",7}{"疲労",7}");
    var order = new List<NationConfig>(cfgs0);
    var easeByName = cfgs0.ToDictionary(c => c.Name, c => NationBrief.Ease(c, cfgs0, coef0));   // ★Sort中に再計算しない
    order.Sort((a, b) => easeByName[b.Name].CompareTo(easeByName[a.Name]));
    foreach (var c in order)
    {
        var t = NationBrief.Terms(c, cfgs0, coef0);
        System.Console.WriteLine($"{c.Name,-14}{ranks.GetValueOrDefault(c.Name, ""),-6}{diff.GetValueOrDefault(c.Name, "―"),-8}"
            + $"{t.Ease,8:F3}{t.Resource,7:F2}{t.Hold,7:F2}{t.Safety,7:F2}{t.Load,7:F2}{t.Decay,7:F2}");
    }
    return;
}

// === 輸送容量の診断（輸送doc T0・2026-08-13）： dotnet run -- transprobe <四半期> ===
// ★何を測るか＝「清が過大・蘭が過小」という**推測を実測に置き換える**（doc §6-1）。
//   容量が人口だけで決まる旧式では、交易しない人口大国が容量を積み、交易集約の小国が積めない——はずである。
// ⚠ 出すのは3つだけ：人口・積んだ容量・**潜在輸送量**（絞る前に運びたかった重量）。
//   容量÷潜在＝律速率が1未満なら足りない、潜在÷人口が小さいのに容量が大きい国は誤配分している。
if (args.Length > 0 && args[0] == "transprobe")
{
    int tpQ = ArgInt(args, 1, 120);
    var tpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    tpm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var nn in tpm.Nations) nn.TelemetryEnabled = false;
    void TpDump(int q)
    {
        System.Console.WriteLine($"--- Q{q}（{tpm.Nations[0].World.Year}年）人口・容量・潜在輸送量 ---");
        var rows = new System.Collections.Generic.List<(string name, double pop, double cap, double pot, double thr)>();
        foreach (var n in tpm.Nations)
        {
            var w = n.World;
            if (w.SelfPower is null) continue;
            double pot = 0, thrSum = 0; int thrN = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { pot += r.LastTradePotential; thrSum += r.LastTradeThrottle; thrN++; }
            rows.Add((w.SelfPower.Name, w.TotalPopulation, w.RailCapacity + w.ShipCapacity, pot, thrN > 0 ? thrSum / thrN : 1.0));
        }
        rows.Sort((a, b) => b.cap.CompareTo(a.cap));
        int shown = 0;
        foreach (var row in rows)
        {
            if (shown++ >= 14) break;
            double perPop = row.pop > 1e-9 ? row.cap / row.pop : 0;
            double use = row.cap > 1e-9 ? row.pot / row.cap : 0;
            System.Console.WriteLine($"  {row.name,-10} 人口{row.pop,8:F1} 容量{row.cap,10:F0} 潜在{row.pot,10:F0} 容量/人口{perPop,7:F3} 潜在/容量{use,6:F2} 律速{row.thr,5:F2}");
        }
        System.Console.WriteLine();
    }
    for (int q = 1; q <= tpQ; q++) { tpm.StepQuarter(); if (q % 40 == 0) TpDump(q); }
    TpDump(tpQ);
    return;
}

// === 主食の気候ゲート検証： dotnet run -- staplemap ＝ 州ごとの主食RGO（米/ジャガイモ/トウモロコシ）の有無と生産性 ===
// 「―」＝生産者なし（気候ゲートで除去）。数値＝生産性（賦存列挙×3.5・自給免除×1.0・賦存に乏しい×0.4 の別が読める）
if (args.Length > 0 && args[0] == "staplemap")
{
    var sm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var n in sm.Nations)
    {
        var w = n.World;
        foreach (var r in w.Regions)
        {
            string P(int gid) { var pr = r.Producers.Find(p => p.GoodId == gid); return pr is null ? "―" : pr.Productivity.ToString("F2"); }
            System.Console.WriteLine($"{w.SelfPower?.Name ?? "?"}\t{r.Name}\t米:{P(102)}\t芋:{P(115)}\t玉:{P(116)}");
        }
    }
    return;
}

// === #17較正再走： dotnet run -- search ＝ LegitimacyErosionRate×ReformPressureBoost の格子探索（maxShare昇順） ===
if (args.Length > 0 && args[0] == "search")
{
    var ranked = Calibration.Search(Coefficients.CalibQuarters);   // 較正は524凍結（1934延長2026-07-19＝物差し不変）
    foreach (var r in ranked)
        System.Console.WriteLine($"erosion={r.Erosion:F3} boost={r.ReformBoost:F2} → maxShare={r.Metrics.MaxShare:P0} 破綻={r.Metrics.Breakages} 都市化形={r.Metrics.UrbanizationShapeOk} 自由>専制={r.Metrics.FreeBeatsAutocraticOk}");
    return;
}

// === 貨幣保存リーク狩り： dotnet run -- leakhunt [seed] [四半期] ===
// 恒久プローブ（Simulation.ConservationProbe）を有効化して1試合を回し、保存量 D＝基礎貨幣−累積台帳 が動いたサブステップを stderr へ出す。
// 絞り込みの順序：[PROBE共有]（四半期共有層）→[PROBE国内]（日次/月次）→[PROBE金融]（国際金融の段）→ 現場のコードを成分分解。
if (args.Length > 0 && args[0] == "leakhunt")
{
    ulong lseed = args.Length > 1 && ulong.TryParse(args[1], out var ls) ? ls : 1789UL;
    int lquarters = args.Length > 2 && int.TryParse(args[2], out var lq) ? lq : Coefficients.GameQuarters;
    Simulation.ConservationProbe = true;
    var lm = new MultiNationSim(WorldData.Nations(), lseed, _ => new UtilityAgent());
    // ★世界の台帳（fis-21・2026-09-26）：国ごとの D は片側記帳を拾えない＝四半期ごとに世界の合計を「正当な湧き口と消え口」で説明できるかを見る
    //   （ConservationTests の世界の合計の番人と同じ式）。人口＝Σ現存国の全州−（出生−戦争以外の死亡−戦死）の累積／
    //   貨幣＝Σ現存国の基礎貨幣−（鋳造＋不換発行）の累積（累積は一度でも居た国の全部）。
    //   ★人口には国を持たない勢力（抽象標的）の植民地の住民も数える（回1-2・作者裁定 2026-09-26「住民が消えるのは好ましくない」）＝奪われて州に入るのは世界の中の移転。
    //   ★回1-2（2026-09-27）で国際移民の世界プール（ゲーム外から人と持参金が湧く既知の湧き口）は消えた＝既定の世界で閉じる。
    //   ⚠共有層の段ごとの [PROBE世界] は、月次の段で積んだ片側（起債・利払いの対外分）と四半期末の相手側が別の段に出る＝判定はこの四半期の合計で行う。
    var lseen = new List<Simulation>(lm.Nations); var lseenSet = new HashSet<Simulation>(lm.Nations);
    (double Pop, double Money) LWorld()
    {
        double pop = 0, money = 0;
        foreach (var n in lm.Nations) { foreach (var r in n.World.Regions) pop += r.SumPopulation(); money += n.World.BaseMoney; }
        foreach (var lap in lm.Diplomacy.Powers) if (lap.Nation is null) foreach (var lcol in lap.ColonyList) pop += Math.Max(0, lcol.PopSize);   // 抽象標的の植民地の住民（入植者 SettlerSize は 2026-09-30 段4g に削除＝常に0だった）
        foreach (var n in lseen) { var w = n.World; pop -= w.DiagBirths - w.DiagDeaths - w.WarDeadCumulative; money -= w.CumulativeGoldMinted + w.CumulativeFiatIssued; }
        money -= lm.Diplomacy.CumulativeFxResidual;   // ★段3-1（A7）：為替の換算で世界の名目の基礎貨幣が動く分（清算所の台帳）
        return (pop, money);
    }
    int lPopBad = 0, lMoneyBad = 0; double lPopSum = 0, lMoneySum = 0;
    try
    {
        var lw0 = LWorld();
        for (int q = 0; q < lquarters; q++)
        {
            lm.StepQuarter();
            foreach (var n in lm.Nations) if (lseenSet.Add(n)) lseen.Add(n);
            var lw = LWorld();
            double dP = lw.Pop - lw0.Pop, dM = lw.Money - lw0.Money;
            if (Math.Abs(dP) > 1e-6 * Math.Max(1.0, Math.Abs(lw.Pop))) { lPopBad++; lPopSum += dP; if (lPopBad <= 20) Console.Error.WriteLine($"[世界の台帳] Q{q + 1}（{lm.Year}年）人口 {dP:+0.0;-0.0}"); }
            if (Math.Abs(dM) > 1e-6 * Math.Max(1.0, Math.Abs(lw.Money))) { lMoneyBad++; lMoneySum += dM; if (lMoneyBad <= 20) Console.Error.WriteLine($"[世界の台帳] Q{q + 1}（{lm.Year}年）基礎貨幣 {dM:+0.0;-0.0}"); }
            lw0 = lw;
        }
        // （退蔵結晶化の IW 不変条件は 2026-09-26 に削除＝結晶資産 Pop.IlliquidWealth ごと消えた・dorm-24）
        System.Console.WriteLine($"leakhunt 完走（seed={lseed}・{lquarters}四半期・保存則違反なし）");
        System.Console.WriteLine($"世界の台帳：説明できない増減＝人口 {lPopBad}四半期（累計 {lPopSum:+0.0;-0.0}）・基礎貨幣 {lMoneyBad}四半期（累計 {lMoneySum:+0.0;-0.0}）"
                                 + (Coefficients.OverrideSummary() is { Length: > 0 } lov ? $"（上書き {lov}）" : ""));
    }
    catch (Exception lex)
    {
        System.Console.WriteLine($"leakhunt 停止: {lex.GetType().Name}: {lex.Message}");
    }
    return;
}

// === 説明API（#63）：ある国のある数値が「なぜこの値なのか」を因果分解して表示： dotnet run -- explain [seed] [四半期] [国名] ===
// 対象国に説明台帳を装着して1四半期進め、その四半期の Politics.Step が記帳した正統性の内訳を最終値とともに出力する。
// 台帳は本計算を書き換えない（ExplainLedger 参照）＝装着しても数値はビット恒等。装着していない既定経路は完全ゼロコスト。
if (args.Length > 0 && args[0] == "explain")
{
    ulong eSeed = ArgUlong(args, 1, 1789UL);
    int eQ = System.Math.Max(1, ArgInt(args, 2, 200));
    string eWant = args.Length > 3 ? args[3] : "イギリス";
    var em = new MultiNationSim(WorldData.Nations(), eSeed, _ => new UtilityAgent());
    for (int q = 0; q < eQ - 1; q++) em.StepQuarter();
    var etarget = em.Nations.FirstOrDefault(n => n.World.SelfPower?.Name == eWant) ?? em.Nations[0];
    etarget.World.Explain = new ExplainLedger();   // 最後の1四半期だけ装着＝この期の内訳を記録
    em.StepQuarter();
    var ew = etarget.World;
    int eYear = 1783 + (eQ - 1) / 4;
    System.Console.WriteLine($"=== {ew.SelfPower?.Name}・seed{eSeed}・{eQ}四半期(≈{eYear}年)・{PolJa(ew.Polity)} ===");
    // value=null＝最終値が World に単一のフィールドとして残らない量（与信目標・為替目標など）＝内訳と合計だけを出す（不一致警告は出さない）
    void Show(string quantity, string label, double? value)
    {
        System.Console.WriteLine(value is { } v0 ? $"{label} = {v0:F3}" : $"{label}");
        double sum = 0;
        foreach (var t in ew.Explain!.Get(quantity))
        {
            sum += t.Delta;   // 微小項も合計には算入（下では非表示）
            if (System.Math.Abs(t.Delta) < 5e-4) continue;   // ±0.000 は読みづらいので非表示
            string sign = t.Delta >= 0 ? "＋" : "−";
            System.Console.WriteLine($"    {sign}{System.Math.Abs(t.Delta):F3}  {t.Reason}");
        }
        System.Console.WriteLine($"    ────────  （内訳合計 {sum:F3}）");
        if (value is { } v1 && System.Math.Abs(sum - v1) > 1e-6)
            System.Console.WriteLine($"    ⚠ 内訳合計が最終値と不一致（差 {sum - v1:E2}）＝記帳していない書き込み現場が残っている可能性");
    }
    Show("Legitimacy", "正統性", ew.Legitimacy);
    // 金利doc B2（2026-07-30）：金利の4チャネルを台帳で出す（B4/B5の点火前後で同じ物差しを当てるため）
    Show("CreditTarget", "与信目標", null);
    Show("CapitalReturn", "投資収益率", null);
    Show("ExchangeRateTarget", "為替目標", null);
    Show("ExpectedInflation", "期待インフレ", ew.ExpectedInflation);
    return;
}

// === 第8段8-1（2026-07-28）：政治ポート台帳の生成： dotnet run -- portledger [SimCoreソースdir] ===
// [PORT:Pn] タグをソースから走査して台帳を吐く＋Coefficients の全double係数を反射で値つき列挙（0=休眠/非0=点火）。
// 規約3「同じ根を2ポートから叩かない」の違反検出手段＝台帳を現物から生成する（手書き台帳は現物とズレる＝73件の違反が検出不能だった根）。
if (args.Length > 0 && args[0] == "portledger")
{
    string src = args.Length > 1 ? args[1] : System.IO.Path.Combine("PriceOfBread.SimCore");
    string dateTag = args.Length > 2 ? args[2] : System.DateTime.Now.ToString("yyyy-MM-dd");   // 差分レビュー用に固定文字列を渡せる（省略時は今日の日付）
    Console.WriteLine($"# 政治ポート台帳（生成物・{dateTag}）\n");
    Console.WriteLine("## 1. [PORT:Pn] タグ（接続ポート＝新規機構はここへ独立加算で接続する）\n");
    Console.WriteLine("| ポート | ファイル:行 | 内容 |");
    Console.WriteLine("|---|---|---|");
    var portRe = new System.Text.RegularExpressions.Regex(@"\[PORT:(P\d+)\]");
    var rows = new List<(string port, string loc, string text)>();
    foreach (var f in System.IO.Directory.GetFiles(src, "*.cs"))
    {
        var lines = System.IO.File.ReadAllLines(f);
        for (int li = 0; li < lines.Length; li++)
        {
            var pm = portRe.Match(lines[li]);
            if (!pm.Success) continue;
            string text = lines[li][(pm.Index + pm.Length)..].Trim().TrimStart('：', ':', ' ');
            if (text.Length > 90) text = text[..90] + "…";
            rows.Add((pm.Groups[1].Value, $"{System.IO.Path.GetFileName(f)}:{li + 1}", text.Replace("|", "／")));
        }
    }
    rows.Sort((a, b) => { int c0 = string.CompareOrdinal(a.port, b.port); return c0 != 0 ? c0 : string.CompareOrdinal(a.loc, b.loc); });
    foreach (var r in rows) Console.WriteLine($"| {r.port} | {r.loc} | {r.text} |");
    Console.WriteLine($"\n計 {rows.Count} タグ\n");
    Console.WriteLine("## 2. 係数の点火状態（Coefficients 反射＝既定インスタンスの実値。0=休眠・非0=点火。逆規約は 20260727_変数リファレンス.md 冒頭）\n");
    var cobj = new Coefficients();
    var live = new List<string>(); var dormant = new List<string>();
    foreach (var fi in typeof(Coefficients).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
    {
        if (fi.FieldType != typeof(double) && fi.FieldType != typeof(int)) continue;
        double v = System.Convert.ToDouble(fi.GetValue(cobj));
        (v == 0 ? dormant : live).Add($"{fi.Name} = {v}");
    }
    // ★決定論（原則⑥）：Type.GetFields の返却順は仕様上未規定＝台帳をバイト再現させるため名前で整列する。
    live.Sort(string.CompareOrdinal); dormant.Sort(string.CompareOrdinal);
    Console.WriteLine($"### 点火（非0）{live.Count}件\n");
    foreach (var l in live) Console.WriteLine($"- {l}");
    Console.WriteLine($"\n### 休眠（0）{dormant.Count}件\n");
    foreach (var d in dormant) Console.WriteLine($"- {d}");
    return;
}

// === 地図の線の検査（2026-08-16・作者報告「陸地を横断する海路が描かれている」）： dotnet run -- mapedges [四半期] ===
// ★読み取り専用。UI（GameViewModel.Map.cs の BuildRouteUsage / BuildPathGraphLayer）が**実際に地図へ描く線**を
//   同じ手順で組み直し、1本ずつ Landmask（陸の真値）に当てて「陸の上を通っている長さ」を測る。
// ⚠絵を見て探すのをやめる（seagraph の PassageBypassReport と同じ発想）＝線は数千本あるので目視では特定できない。
// 対象＝**青い線**（海路 SeaEdgeBrush／接ぎ木 SeaStitchBrush）。陸路（PathGraph の辺）は陸の上が正しいので測らない。
if (args.Length > 0 && args[0] == "mapedges")
{
    int mqs = ArgInt(args, 1, 0);
    var msE = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    msE.StartWarmup();
    for (int q = 0; q < mqs; q++) msE.StepQuarter();
    var meE = msE.Diplomacy.Powers[0];
    bool suezE = msE.Diplomacy.ChokeOpenFor(Diplomacy.SuezIdPublic, meE);
    bool panamaE = msE.Diplomacy.ChokeOpenFor(Diplomacy.PanamaIdPublic, meE);
    Console.WriteLine($"# 地図の線の陸横断検査（{msE.Year}年・スエズ{(suezE ? "開" : "閉")}・パナマ{(panamaE ? "開" : "閉")}）");

    // 線分の「陸の上の長さ」（km）。両端は含めず Landmask.IsLand と同じ刻み（格子の半分）で数える。
    static (double LandKm, double TotalKm, double LeadLandKm, double AfterAfloatLandKm) LandRun(double la, double lo, double lb, double lob)
    {
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
        double total = Geo.DistanceKm(la, lo, lb, lob);
        int n = Math.Max(9, (int)Math.Ceiling(total / (Landmask.Step * 111.0 / 2.0)));
        double step = total / (n + 1);
        double land = 0, lead = 0, after = 0;
        bool afloat = false, leading = true;
        for (int s = 1; s <= n; s++)
        {
            double t = s / (double)(n + 1);
            if (!Landmask.IsLand(la + (lb - la) * t, lo + dlon * t)) { afloat = true; leading = false; continue; }
            land += step;
            if (leading) lead += step; else if (afloat) after += step;
        }
        return (land, total, lead, after);
    }

    // ── UI と同じ手順で「描かれる線」を組む（GameViewModel.Map.cs BuildRouteUsage の写し）
    var seaUse = new Dictionary<(int, int), double>();
    var stitchUse = new Dictionary<(string, int), double>();
    var stitchCoord = new Dictionary<string, (double Lat, double Lon)>();
    var stitchSrc = new Dictionary<(string, int), SortedSet<string>>();   // 線の出どころ（交易の口／植民地の宗主側＝首都／植民地側／港州）
    void BumpSea(int a, int b, double w) { var k = a < b ? (a, b) : (b, a); seaUse[k] = seaUse.TryGetValue(k, out double v) ? v + w : w; }
    void BumpStitch(string p, int n, double w, string src)
    {
        var k = (p, n);
        stitchUse[k] = stitchUse.TryGetValue(k, out double v) ? v + w : w;
        if (!stitchSrc.TryGetValue(k, out var set)) stitchSrc[k] = set = new SortedSet<string>(StringComparer.Ordinal);
        set.Add(src);
    }
    var seaIxE = new Dictionary<string, int>(SeaGraph.Nodes.Length);
    for (int i = 0; i < SeaGraph.Nodes.Length; i++) seaIxE[SeaGraph.Nodes[i].Name] = i;

    double halfE = msE.TradeGravityHalfKm;
    var actE = new List<(double Lat, double Lon, double Tw, string Name, string Nation)>();
    var gateOfE = new Dictionary<World, (string Name, double Lat, double Lon)>();
    foreach (var simE in msE.Nations)
    {
        var wE = simE.World;
        string gateE = ""; double glatE = 0, glonE = 0;
        foreach (var rr in wE.Regions)
        {
            if (rr.State != RegionState.Active) continue;
            if (rr.IsPort) { var pc0 = Climate.CityCoord(rr.Name); if (pc0.Lat != 0 || pc0.Lon != 0) { gateE = rr.Name; glatE = pc0.Lat; glonE = pc0.Lon; break; } }
            if (gateE.Length == 0 && rr.LastTransitPortName.Length > 0)
            {
                var pc1 = Climate.CityCoord(rr.LastTransitPortName);
                if (pc1.Lat != 0 || pc1.Lon != 0) { gateE = rr.LastTransitPortName; glatE = pc1.Lat; glonE = pc1.Lon; }
            }
        }
        if (gateE.Length == 0) continue;
        gateOfE[wE] = (gateE, glatE, glonE);
        actE.Add((glatE, glonE, wE.SelfPower?.TradeWeight ?? 1.0, gateE, wE.SelfPower?.Name ?? wE.Region.Name));
    }
    for (int i = 0; i < actE.Count; i++)
        for (int j = i + 1; j < actE.Count; j++)
        {
            double km = SeaGraph.SeaDistanceKm(actE[i].Lat, actE[i].Lon, actE[j].Lat, actE[j].Lon, suezE, panamaE);
            if (!(km > 0)) continue;
            double wij = actE[i].Tw * actE[j].Tw * halfE / (halfE + km);
            if (!(wij > 1e-6)) continue;
            var path = SeaGraph.RoutePath(actE[i].Lat, actE[i].Lon, actE[j].Lat, actE[j].Lon, suezE, panamaE);
            for (int k = 1; k < path.Count; k++)
                if (seaIxE.TryGetValue(path[k - 1], out int a) && seaIxE.TryGetValue(path[k], out int b)) BumpSea(a, b, wij);
            if (path.Count > 0)
            {
                if (seaIxE.TryGetValue(path[0], out int s0)) BumpStitch(actE[i].Name, s0, wij, "交易の口");
                if (seaIxE.TryGetValue(path[^1], out int s1)) BumpStitch(actE[j].Name, s1, wij, "交易の口");
            }
        }
    foreach (var simE in msE.Nations)
    {
        var wE = simE.World;
        if (wE.SelfPower is not { } spE || spE.ColonyList.Count == 0) continue;
        if (!gateOfE.TryGetValue(wE, out var gtE)) continue;   // ★UI と同じ＝海へ出る口が無い国は植民地線も描かない
        double twE = spE.TradeWeight;
        foreach (var col in spE.ColonyList)
        {
            if (Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9) continue;
            double km = SeaGraph.SeaDistanceKm(gtE.Lat, gtE.Lon, col.Latitude, col.Longitude, suezE, panamaE);
            if (!(km > 0)) continue;
            double wij = twE * halfE / (halfE + km);
            if (!(wij > 1e-6)) continue;
            var path = SeaGraph.RoutePath(gtE.Lat, gtE.Lon, col.Latitude, col.Longitude, suezE, panamaE);
            for (int k = 1; k < path.Count; k++)
                if (seaIxE.TryGetValue(path[k - 1], out int a2) && seaIxE.TryGetValue(path[k], out int b2)) BumpSea(a2, b2, wij);
            if (path.Count > 0)
            {
                if (seaIxE.TryGetValue(path[0], out int sn0)) BumpStitch(gtE.Name, sn0, wij, "植民地線の宗主側");
                if (seaIxE.TryGetValue(path[^1], out int sn1)) { BumpStitch(col.Name, sn1, wij, "植民地側"); stitchCoord[col.Name] = (col.Latitude, col.Longitude); }
            }
        }
    }
    var usedSeaNodesE = new HashSet<int>();
    foreach (var k in seaUse.Keys) { usedSeaNodesE.Add(k.Item1); usedSeaNodesE.Add(k.Item2); }
    foreach (var simE in msE.Nations)
        foreach (var rr in simE.World.Regions)
        {
            if (rr.State != RegionState.Active || !rr.IsPort) continue;
            var pc = Climate.CityCoord(rr.Name);
            if (Math.Abs(pc.Lat) < 1e-9 && Math.Abs(pc.Lon) < 1e-9) continue;
            int sn = SeaGraph.NearestSeaNode(pc.Lat, pc.Lon);
            if (!usedSeaNodesE.Contains(sn)) continue;
            BumpStitch(rr.Name, sn, 1.0, "港州");
        }

    // ── ①海の辺（洋上ノードどうし）
    Console.WriteLine($"\n## 海路の辺 {seaUse.Count} 本（描かれるもののみ）");
    var badSea = new List<(string A, string B, double LandKm, double TotalKm, bool Passage)>();
    // 手置きの通路（passages.json 由来）＝**琥珀で描かれる**＝青い線ではない。UI と同じ規則で分ける。
    var passageKeysE = new HashSet<(int, int)>();
    {
        var ixE = new Dictionary<string, int>(SeaGraph.Nodes.Length);
        for (int i = 0; i < SeaGraph.Nodes.Length; i++) ixE[SeaGraph.Nodes[i].Name] = i;
        foreach (var pr in SeaGraph.PassageBypassReport())
            if (ixE.TryGetValue(pr.A, out int pa) && ixE.TryGetValue(pr.B, out int pb))
                passageKeysE.Add((Math.Min(pa, pb), Math.Max(pa, pb)));
    }
    foreach (var kv in seaUse)
    {
        var a = SeaGraph.Nodes[kv.Key.Item1]; var b = SeaGraph.Nodes[kv.Key.Item2];
        var r = LandRun(a.Lat, a.Lon, b.Lat, b.Lon);
        if (r.LandKm < 1.0) continue;
        badSea.Add((a.Name, b.Name, r.LandKm, r.TotalKm, passageKeysE.Contains(kv.Key)));
    }
    foreach (var x in badSea.OrderByDescending(x => x.LandKm))
        Console.WriteLine($"  {(x.Passage ? "琥珀" : "⚠青")} {x.A}↔{x.B}　陸上 {x.LandKm:F0}km / 全長 {x.TotalKm:F0}km");
    Console.WriteLine($"  → 陸を通る海路の辺: {badSea.Count} 本（うち**青**で陸上100km超 {badSea.Count(x => !x.Passage && x.LandKm >= 100)} 本／琥珀の通路 {badSea.Count(x => x.Passage)} 本）");

    // ── ②接ぎ木（州/植民地 → 洋上ノード）。⚠港は水際なので**出だしの陸**は格子の粗さで必ず出る＝
    //    「出港の一歩目」と「大陸横断」を分けて数える（前者は無害・後者が作者の見た線）。
    Console.WriteLine($"\n## 接ぎ木 {stitchUse.Count} 本（州/植民地→海ノード）");
    var badStitch = new List<(string Port, string Node, double LandKm, double TotalKm, double Lead, double After, double Lat, double Lon)>();
    foreach (var kv in stitchUse)
    {
        var pc = Climate.CityCoord(kv.Key.Item1);
        if (Math.Abs(pc.Lat) < 1e-9 && Math.Abs(pc.Lon) < 1e-9)
        {
            if (stitchCoord.TryGetValue(kv.Key.Item1, out var cp)) pc = cp;
            else continue;   // 座標未登録＝描かれない
        }
        var sn = SeaGraph.Nodes[kv.Key.Item2];
        var r = LandRun(pc.Lat, pc.Lon, sn.Lat, sn.Lon);
        if (r.LandKm < 1.0) continue;
        badStitch.Add((kv.Key.Item1 + "[" + string.Join("+", stitchSrc[kv.Key]) + "]", sn.Name, r.LandKm, r.TotalKm, r.LeadLandKm, r.AfterAfloatLandKm, pc.Lat, pc.Lon));
    }
    foreach (var x in badStitch.OrderByDescending(x => x.LandKm))
        Console.WriteLine($"  {(x.LandKm >= 100 ? "⚠" : "・")} {x.Port}({x.Lat:F2},{x.Lon:F2}) → {x.Node}　陸上 {x.LandKm:F0}km(出港側 {x.Lead:F0}／離水後 {x.After:F0}) / 全長 {x.TotalKm:F0}km");
    Console.WriteLine($"  → 陸を通る接ぎ木: {badStitch.Count} 本（うち陸上100km超 {badStitch.Count(x => x.LandKm >= 100)} 本）");
    Console.WriteLine($"\n### 合計：陸上100km超の**青い線** {badSea.Count(x => !x.Passage && x.LandKm >= 100) + badStitch.Count(x => x.LandKm >= 100)} 本");

    // ── ④港州の「海への近さ」（Coastal フラグの妥当性＝原因(b)の検査）
    // 港州の座標から**最も近い海のマス**までの距離（陸マスクの真値）。数百km なら港ではない。
    Console.WriteLine("\n## 港州から海までの距離（陸マスクの真値・50km超のみ）");
    var portRows = new List<(string Name, string Nation, double SeaCellKm, double NodeKm, string Node, string NearNode)>();
    foreach (var simE in msE.Nations)
        foreach (var rr in simE.World.Regions)
        {
            if (rr.State != RegionState.Active || !rr.IsPort) continue;
            var pc = Climate.CityCoord(rr.Name);
            if (Math.Abs(pc.Lat) < 1e-9 && Math.Abs(pc.Lon) < 1e-9) continue;
            double best = double.PositiveInfinity;
            for (int dy = -50; dy <= 50; dy++)
                for (int dx = -50; dx <= 50; dx++)
                {
                    double la = pc.Lat + dy * 0.1, lo = pc.Lon + dx * 0.1;
                    if (Landmask.IsLand(la, lo)) continue;
                    double d = Geo.DistanceKm(pc.Lat, pc.Lon, la, lo);
                    if (d < best) best = d;
                }
            int sn = SeaGraph.NearestSeaNode(pc.Lat, pc.Lon);
            int near = 0; double nk = double.PositiveInfinity;
            for (int i = 0; i < SeaGraph.Nodes.Length; i++)
            {
                double d = Geo.DistanceKm(pc.Lat, pc.Lon, SeaGraph.Nodes[i].Lat, SeaGraph.Nodes[i].Lon);
                if (d < nk) { nk = d; near = i; }
            }
            portRows.Add((rr.Name, simE.World.SelfPower?.Name ?? "", best,
                Geo.DistanceKm(pc.Lat, pc.Lon, SeaGraph.Nodes[sn].Lat, SeaGraph.Nodes[sn].Lon), SeaGraph.Nodes[sn].Name, SeaGraph.Nodes[near].Name));
        }
    foreach (var p in portRows.Where(p => p.SeaCellKm > 50).OrderByDescending(p => p.SeaCellKm))
        Console.WriteLine($"  {p.Name}({p.Nation})　海のマスまで {p.SeaCellKm:F0}km　採用ノード {p.Node} {p.NodeKm:F0}km（最寄りは {p.NearNode}）");
    Console.WriteLine($"  → 港州 {portRows.Count} 件中、海まで50km超 {portRows.Count(p => p.SeaCellKm > 50)} 件");
    Console.WriteLine("\n### 採用ノードが最寄りでない港州（＝海の反対側へ吸着）");
    foreach (var p in portRows.Where(p => p.Node != p.NearNode).OrderByDescending(p => p.NodeKm))
        Console.WriteLine($"  ⚠ {p.Name}({p.Nation})　採用 {p.Node} {p.NodeKm:F0}km　≠　最寄り {p.NearNode}");
    Console.WriteLine($"  → {portRows.Count(p => p.Node != p.NearNode)} 件");

    // ── ⑤エンジン側の帰結（**表示だけの問題ではない**）：接ぎ木はそのまま海路距離に加算され、海盆も最寄りノードから継ぐ。
    Console.WriteLine("\n## エンジンへの波及（海盆と海路距離）");
    foreach (var simE in msE.Nations)
        foreach (var rr in simE.World.Regions)
        {
            if (rr.State != RegionState.Active || !rr.IsPort) continue;
            var pc = Climate.CityCoord(rr.Name);
            if (Math.Abs(pc.Lat) < 1e-9 && Math.Abs(pc.Lon) < 1e-9) continue;
            int sn = SeaGraph.NearestSeaNode(pc.Lat, pc.Lon);
            int near = 0; double nk = double.PositiveInfinity;
            for (int i = 0; i < SeaGraph.Nodes.Length; i++)
            { double d = Geo.DistanceKm(pc.Lat, pc.Lon, SeaGraph.Nodes[i].Lat, SeaGraph.Nodes[i].Lon); if (d < nk) { nk = d; near = i; } }
            if (SeaGraph.Nodes[sn].Basin == SeaGraph.Nodes[near].Basin) continue;
            Console.WriteLine($"  ⚠ {rr.Name}({simE.World.SelfPower?.Name})　SeaBasin={rr.SeaBasin}（最寄り {SeaGraph.Nodes[near].Name} は {SeaGraph.Nodes[near].Basin}）");
        }
    (string A, double aLat, double aLon, string B, double bLat, double bLon, double actualKm)[] casesM =
    [
        ("トリエステ", 45.65, 13.77, "ヴェネツィア", 45.44, 12.34, 150),
        ("トリエステ", 45.65, 13.77, "ナポリ", 40.85, 14.27, 1200),
        ("トリエステ", 45.65, 13.77, "イスタンブール", 41.01, 28.98, 2100),
        ("ハノイ", 21.03, 105.85, "広州", 23.13, 113.26, 1000),
        ("グラスゴー", 55.86, -4.25, "ダブリン", 53.35, -6.26, 400),
        ("マンチェスター", 53.48, -2.24, "ダブリン", 53.35, -6.26, 300),
        ("ケルン", 50.94, 6.96, "アムステルダム", 52.37, 4.90, 400),
    ];
    foreach (var cm in casesM)
    {
        double g = SeaGraph.SeaDistanceKm(cm.aLat, cm.aLon, cm.bLat, cm.bLon, suezE, panamaE);
        Console.WriteLine($"  {cm.A}→{cm.B}　海路 {g:F0}km（実際 約{cm.actualKm}km・×{g / cm.actualKm:F1}）");
    }

    // ── ③なぜその海ノードが選ばれたか（SeaGraph.NearestSeaNode の候補走査を同じ規則で再現）
    // ⚠エンジンの StitchCrossesLand は**出港の一歩目（連続する陸）を数えない**＝「陸→…→陸→水」で終わる線は
    //   「陸を横切っていない」と判定される。近い海ノードが半島で棄却されると、この非対称のせいで
    //   **大陸の反対側のノードが合格してしまう**。その様子を候補順に印字する。
    static bool StitchCross(double la, double lo, double lb, double lob)
    {
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
        int n = Math.Max(9, (int)Math.Ceiling(Geo.DistanceKm(la, lo, lb, lob) / (Landmask.Step * 111.0 / 2.0)));
        bool afloat = false;
        for (int s = 1; s <= n; s++)
        {
            double t = s / (double)(n + 1);
            bool land = Landmask.IsLand(la + (lb - la) * t, lo + dlon * t);
            if (!land) { afloat = true; continue; }
            if (afloat) return true;
        }
        return false;
    }
    Console.WriteLine("\n## 候補走査（陸上100km超の接ぎ木＝なぜ近い海ノードが落ちたか）");
    foreach (var x in badStitch.Where(x => x.LandKm >= 100).OrderByDescending(x => x.LandKm))
    {
        int qla = (int)Math.Round(x.Lat * 10.0), qlo = (int)Math.Round(x.Lon * 10.0);
        double qlat = qla / 10.0, qlon = qlo / 10.0;
        var ord = Enumerable.Range(0, SeaGraph.Nodes.Length)
            .Select(i => (Km: Geo.DistanceKm(qlat, qlon, SeaGraph.Nodes[i].Lat, SeaGraph.Nodes[i].Lon), I: i))
            .OrderBy(t => t.Km).ThenBy(t => t.I).ToList();
        Console.WriteLine($"- {x.Port}({qlat:F1},{qlon:F1}) → 採用 {SeaGraph.Nodes[SeaGraph.NearestSeaNode(x.Lat, x.Lon)].Name}");
        foreach (var (km, i) in ord)
        {
            bool cross = StitchCross(qlat, qlon, SeaGraph.Nodes[i].Lat, SeaGraph.Nodes[i].Lon);
            Console.WriteLine($"    {(cross ? "×棄却" : "○採用")} {SeaGraph.Nodes[i].Name,-16} {km:F0}km");
            if (!cross) break;
        }
    }
    return;
}

// === 地図の線の流通足切りの実測（2026-08-16・作者指示「流通量0.0K以下の陸路・海路も削除」）： dotnet run -- mapflow [四半期] ===
// ★読み取り専用。UI（GameViewModel.Map.cs の BuildRouteUsage）が組む線を**足切りなし／あり**の両方で組み直し、
//   線の種類ごとに「残る本数／消える本数」を数える。判定は貿易一覧と同じ Money(月額) が £0.0k に丸まるか
//   （App 側 FlowShown の写し）＝地図が寂しくなりすぎないか（1783年に線が全滅しないか）を数で確かめるための計器。
if (args.Length > 0 && args[0] == "mapflow")
{
    int fqs = ArgInt(args, 1, 0);
    var msF = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    msF.StartWarmup();
    for (int q = 0; q < fqs; q++) msF.StepQuarter();
    var meF = msF.Diplomacy.Powers[0];
    bool suezF = msF.Diplomacy.ChokeOpenFor(Diplomacy.SuezIdPublic, meF);
    bool panamaF = msF.Diplomacy.ChokeOpenFor(Diplomacy.PanamaIdPublic, meF);

    // 表示関数の写し（App の GameViewModel.Money / FlowShown と同一の丸め）
    static string MoneyF(double v)
    {
        double a = Math.Abs(v); string sign = v < 0 ? "-" : "";
        if (a >= 1e6) return $"{sign}£{a / 1e6:F2}M";
        if (a >= 1e4) return $"{sign}£{a / 1e3:F0}k";
        return $"{sign}£{a / 1e3:F1}k";
    }
    static bool ShownF(double monthly) => MoneyF(Math.Abs(monthly)) != MoneyF(0);
    double mq = Calendar.MonthsPerQuarter;

    var landIxF = new Dictionary<string, int>(PathGraph.Nodes.Length);
    for (int i = 0; i < PathGraph.Nodes.Length; i++) landIxF[PathGraph.Nodes[i].Name] = i;
    var seaIxF = new Dictionary<string, int>(SeaGraph.Nodes.Length);
    for (int i = 0; i < SeaGraph.Nodes.Length; i++) seaIxF[SeaGraph.Nodes[i].Name] = i;
    // ⚠辞書の**値**（積算使用量）も持つ＝「0.0 の要素が実在するのか、微小値なのか」を数で見るため。
    //   UI と同じ重み（陸脚＝州1件につき1.0／海・接ぎ木＝重力カーネル wij／港の接ぎ木＝1.0）。
    var landAll = new Dictionary<(int, int), double>(); var landCut = new Dictionary<(int, int), double>();
    var seaAll = new Dictionary<(int, int), double>(); var seaCut = new Dictionary<(int, int), double>();
    var stitchAll = new Dictionary<(string, int), double>(); var stitchCut = new Dictionary<(string, int), double>();
    static void AddE(Dictionary<(int, int), double> d, int a, int b, double w)
    { var k = a < b ? (a, b) : (b, a); d[k] = d.TryGetValue(k, out double v) ? v + w : w; }
    static void AddS(Dictionary<(string, int), double> d, string p, int n, double w)
    { var k = (p, n); d[k] = d.TryGetValue(k, out double v) ? v + w : w; }

    // ── ①陸脚（内陸州→港）。足切り＝州の決済額 LastPortThroughput（月額）
    int legAll = 0, legCut = 0;
    foreach (var simF in msF.Nations)
        foreach (var rr in simF.World.Regions)
        {
            if (rr.State != RegionState.Active || rr.IsPort) continue;
            string portName = rr.NearestPortName.Length > 0 ? rr.NearestPortName : rr.LastTransitPortName;
            if (portName.Length == 0) continue;
            var cc = Climate.CityCoord(rr.Name);
            var pc = Climate.CityCoord(portName);
            if ((Math.Abs(cc.Lat) < 1e-9 && Math.Abs(cc.Lon) < 1e-9) || (Math.Abs(pc.Lat) < 1e-9 && Math.Abs(pc.Lon) < 1e-9)) continue;
            bool keep = ShownF(rr.LastPortThroughput / mq);
            legAll++; if (keep) legCut++;
            var path = PathGraph.RoutePath(cc.Lat, cc.Lon, pc.Lat, pc.Lon);
            for (int k = 1; k < path.Count; k++)
                if (landIxF.TryGetValue(path[k - 1], out int a) && landIxF.TryGetValue(path[k], out int b))
                { AddE(landAll, a, b, 1.0); if (keep) AddE(landCut, a, b, 1.0); }
        }

    // ── ②国どうしの交易線。足切り＝国の輸出入額 ExportsByGood+ImportsByGood（月額）
    double halfF = msF.TradeGravityHalfKm;
    var actF = new List<(double Lat, double Lon, double Tw, string Name, bool Keep, string Nation)>();
    var gateOfF = new Dictionary<World, (string Name, double Lat, double Lon, bool Keep)>();
    foreach (var simF in msF.Nations)
    {
        var wF = simF.World;
        string gateF = ""; double glatF = 0, glonF = 0;
        foreach (var rr in wF.Regions)
        {
            if (rr.State != RegionState.Active) continue;
            if (rr.IsPort) { var p0 = Climate.CityCoord(rr.Name); if (p0.Lat != 0 || p0.Lon != 0) { gateF = rr.Name; glatF = p0.Lat; glonF = p0.Lon; break; } }
            if (gateF.Length == 0 && rr.LastTransitPortName.Length > 0)
            {
                var p1 = Climate.CityCoord(rr.LastTransitPortName);
                if (p1.Lat != 0 || p1.Lon != 0) { gateF = rr.LastTransitPortName; glatF = p1.Lat; glonF = p1.Lon; }
            }
        }
        if (gateF.Length == 0) continue;
        double tq = 0;
        if (wF.ExportsByGood is { } egF) foreach (double v in egF) tq += v;
        if (wF.ImportsByGood is { } igF) foreach (double v in igF) tq += v;
        bool keep = ShownF(tq / mq);
        gateOfF[wF] = (gateF, glatF, glonF, keep);
        actF.Add((glatF, glonF, wF.SelfPower?.TradeWeight ?? 1.0, gateF, keep, wF.SelfPower?.Name ?? wF.Region.Name));
    }
    int pairAll = 0, pairCut = 0;
    for (int i = 0; i < actF.Count; i++)
        for (int j = i + 1; j < actF.Count; j++)
        {
            double km = SeaGraph.SeaDistanceKm(actF[i].Lat, actF[i].Lon, actF[j].Lat, actF[j].Lon, suezF, panamaF);
            if (!(km > 0)) continue;
            double wij = actF[i].Tw * actF[j].Tw * halfF / (halfF + km);
            if (!(wij > 1e-6)) continue;
            bool keep = actF[i].Keep && actF[j].Keep;
            pairAll++; if (keep) pairCut++;
            var path = SeaGraph.RoutePath(actF[i].Lat, actF[i].Lon, actF[j].Lat, actF[j].Lon, suezF, panamaF);
            for (int k = 1; k < path.Count; k++)
                if (seaIxF.TryGetValue(path[k - 1], out int a) && seaIxF.TryGetValue(path[k], out int b))
                { AddE(seaAll, a, b, wij); if (keep) AddE(seaCut, a, b, wij); }
            if (path.Count > 0)
            {
                if (seaIxF.TryGetValue(path[0], out int s0)) { AddS(stitchAll, actF[i].Name, s0, wij); if (keep) AddS(stitchCut, actF[i].Name, s0, wij); }
                if (seaIxF.TryGetValue(path[^1], out int s1)) { AddS(stitchAll, actF[j].Name, s1, wij); if (keep) AddS(stitchCut, actF[j].Name, s1, wij); }
            }
        }

    // ── ③植民地線。足切り＝首都州に着く ColonialSupply（貿易タブ「植民地直送」と同じ規則）
    int colAll = 0, colCut = 0;
    double colMaxVal = 0; string colMaxWho = "";   // 診断：直送の月額の最大（0本になる時にそれが「本当に微量」なのかを見る）
    foreach (var simF in msF.Nations)
    {
        var wF = simF.World;
        if (wF.SelfPower is not { } spF || spF.ColonyList.Count == 0) continue;
        if (!gateOfF.TryGetValue(wF, out var gtF)) continue;
        var shownRes = new HashSet<int>();
        foreach (var rr in wF.Regions)
        {
            if (rr.State != RegionState.Active || rr.ColonialSupply is not { } cs) continue;
            for (int gi = 0; gi < cs.Length && gi < Catalog.Goods.Length; gi++)
            {
                if (!(cs[gi] > 1e-9)) continue;
                double pg = gi < msF.Market.Price.Count ? Math.Max(msF.Market.Price[gi], 1e-9)
                          : gi < Catalog.RefPrices.Length ? Catalog.RefPrices[gi] : 1.0;
                if (cs[gi] * pg > colMaxVal) { colMaxVal = cs[gi] * pg; colMaxWho = $"{spF.Name}/{Catalog.Goods[gi].Name}"; }
                if (ShownF(cs[gi] * pg)) shownRes.Add(gi);
            }
        }
        double twF = spF.TradeWeight;
        foreach (var col in spF.ColonyList)
        {
            if (Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9) continue;
            double km = SeaGraph.SeaDistanceKm(gtF.Lat, gtF.Lon, col.Latitude, col.Longitude, suezF, panamaF);
            if (!(km > 0)) continue;
            double wij = twF * halfF / (halfF + km);
            if (!(wij > 1e-6)) continue;
            bool delivers = false;
            foreach (int res in col.Resources ?? []) if (shownRes.Contains(res)) { delivers = true; break; }
            colAll++; if (delivers) colCut++;
            var path = SeaGraph.RoutePath(gtF.Lat, gtF.Lon, col.Latitude, col.Longitude, suezF, panamaF);
            for (int k = 1; k < path.Count; k++)
                if (seaIxF.TryGetValue(path[k - 1], out int a2) && seaIxF.TryGetValue(path[k], out int b2))
                { AddE(seaAll, a2, b2, wij); if (delivers) AddE(seaCut, a2, b2, wij); }
            if (path.Count > 0)
            {
                if (seaIxF.TryGetValue(path[0], out int sn0)) { AddS(stitchAll, gtF.Name, sn0, wij); if (delivers) AddS(stitchCut, gtF.Name, sn0, wij); }
                if (seaIxF.TryGetValue(path[^1], out int sn1)) { AddS(stitchAll, col.Name, sn1, wij); if (delivers) AddS(stitchCut, col.Name, sn1, wij); }
            }
        }
    }

    // ── ④港州の接ぎ木。足切り＝港の取扱高 LastPortThroughput（月額）。⚠**足切り後の**航路が通る海ノードだけに接ぐ
    var usedAll = new HashSet<int>(); foreach (var k in seaAll.Keys) { usedAll.Add(k.Item1); usedAll.Add(k.Item2); }
    var usedCut = new HashSet<int>(); foreach (var k in seaCut.Keys) { usedCut.Add(k.Item1); usedCut.Add(k.Item2); }
    int portAll = 0, portCut = 0;
    foreach (var simF in msF.Nations)
        foreach (var rr in simF.World.Regions)
        {
            if (rr.State != RegionState.Active || !rr.IsPort) continue;
            var pc = Climate.CityCoord(rr.Name);
            if (Math.Abs(pc.Lat) < 1e-9 && Math.Abs(pc.Lon) < 1e-9) continue;
            int sn = SeaGraph.NearestSeaNode(pc.Lat, pc.Lon);
            bool keep = ShownF(rr.LastPortThroughput / mq);
            if (usedAll.Contains(sn)) { portAll++; AddS(stitchAll, rr.Name, sn, 1.0); }
            if (keep && usedCut.Contains(sn)) { portCut++; AddS(stitchCut, rr.Name, sn, 1.0); }
        }

    Console.WriteLine($"# 地図の線の流通足切り（{msF.Year}年・スエズ{(suezF ? "開" : "閉")}・パナマ{(panamaF ? "開" : "閉")}・判定＝Money(月額)が£0.0k）");
    Console.WriteLine($"## 出どころ（線を生む対象の数）");
    Console.WriteLine($"  陸脚(内陸州→港)　　 足切り前 {legAll}　→ 後 {legCut}（消える {legAll - legCut}）");
    Console.WriteLine($"  交易線(国ペア)　　　 足切り前 {pairAll}　→ 後 {pairCut}（消える {pairAll - pairCut}）");
    Console.WriteLine($"  　うち交易のある国　 {actF.Count(a => a.Keep)} / {actF.Count}");
    Console.WriteLine($"  植民地線(宗主⇄植民地) 足切り前 {colAll}　→ 後 {colCut}（消える {colAll - colCut}）　直送の月額の最大 {MoneyF(colMaxVal)}({colMaxWho})");
    Console.WriteLine($"  港の接ぎ木　　　　　 足切り前 {portAll}　→ 後 {portCut}（消える {portAll - portCut}）");
    Console.WriteLine($"## 実際に描かれる線（辺の本数＝地図に出る本数）");
    Console.WriteLine($"  陸路　 {landAll.Count} → {landCut.Count}（消える {landAll.Count - landCut.Count}・残率 {(landAll.Count > 0 ? landCut.Count * 100.0 / landAll.Count : 0):F0}%）");
    Console.WriteLine($"  海路　 {seaAll.Count} → {seaCut.Count}（消える {seaAll.Count - seaCut.Count}・残率 {(seaAll.Count > 0 ? seaCut.Count * 100.0 / seaAll.Count : 0):F0}%）");
    Console.WriteLine($"  接ぎ木 {stitchAll.Count} → {stitchCut.Count}（消える {stitchAll.Count - stitchCut.Count}・残率 {(stitchAll.Count > 0 ? stitchCut.Count * 100.0 / stitchAll.Count : 0):F0}%）");
    Console.WriteLine($"  合計　 {landAll.Count + seaAll.Count + stitchAll.Count} → {landCut.Count + seaCut.Count + stitchCut.Count}");

    // ── ⑤辞書の**値**（積算使用量）の分布。「0.0 の要素が実在するのか／微小値なのか」を数で見る。
    // ⚠この値は**流通量ではない**（陸＝その脚を使う後背地の州数・海＝重力カーネル wij の積算＝無次元）。
    //   参考として、この値にそのまま貿易一覧の丸めを当てるとどうなるかも出す（＝全滅するなら物差しが違う証拠）。
    static void Dist(string name, IEnumerable<double> vals)
    {
        var v = vals.OrderBy(x => x).ToList();
        if (v.Count == 0) { Console.WriteLine($"  {name}　要素なし"); return; }
        int zero = v.Count(x => x <= 0.0);
        int qtyCut = v.Count(x => Math.Abs(x) / 1e3 < 0.05);            // Qty(x) が "0.0k" に丸まる（=x<50）
        int moneyCut = v.Count(x => Math.Abs(x / 3.0) / 1e3 < 0.05);    // Money(x/3) が "£0.0k" に丸まる（=x<150）
        Console.WriteLine($"  {name}　n={v.Count}　最小 {v[0]:G4}　中央 {v[v.Count / 2]:G4}　最大 {v[^1]:G4}　"
                        + $"値=0 が {zero} 件　Qtyで0.0k {qtyCut} 件({qtyCut * 100.0 / v.Count:F0}%)　Money(月額)で£0.0k {moneyCut} 件({moneyCut * 100.0 / v.Count:F0}%)");
    }
    Console.WriteLine("## 辞書の値（積算使用量）の分布　※これは流通量ではない＝陸は州数・海は重力カーネルの積算");
    Dist("陸路　", landAll.Values);
    Dist("海路　", seaAll.Values);
    Dist("接ぎ木", stitchAll.Values);

    // ── ⑥通路（passages.json 由来＝琥珀で描かれる海の辺）も海路と同じ足切りを受けるか（例外を作っていないか）
    var passF = new HashSet<(int, int)>();
    foreach (var pr in SeaGraph.PassageBypassReport())
        if (seaIxF.TryGetValue(pr.A, out int pa) && seaIxF.TryGetValue(pr.B, out int pb))
            passF.Add((Math.Min(pa, pb), Math.Max(pa, pb)));
    int passAllN = seaAll.Keys.Count(k => passF.Contains(k)), passCutN = seaCut.Keys.Count(k => passF.Contains(k));
    Console.WriteLine($"## 通路（琥珀＝passages.json 由来の海の辺・全 {passF.Count} 本）");
    Console.WriteLine($"  経路に現れた通路 {passAllN} → 足切り後 {passCutN}（消える {passAllN - passCutN}）※海路と同一の規則＝例外なし");
    foreach (var k in passF.OrderBy(k => SeaGraph.Nodes[k.Item1].Name, StringComparer.Ordinal))
    {
        bool inAll = seaAll.ContainsKey(k), inCut = seaCut.ContainsKey(k);
        Console.WriteLine($"    {(inCut ? "描く" : inAll ? "足切りで消える" : "経路に現れない")}　{SeaGraph.Nodes[k.Item1].Name}↔{SeaGraph.Nodes[k.Item2].Name}");
    }

    Console.WriteLine("## 交易が£0.0k/月に丸まる国（交易線が消える国）");
    foreach (var a in actF.Where(a => !a.Keep).OrderBy(a => a.Nation, StringComparer.Ordinal))
        Console.WriteLine($"  ・{a.Nation}（口＝{a.Name}）");
    return;
}

// === 定義（分子と分母の物差し）の実測（2026-08-21・「貿易/GDPは定義の問題ではない」主張の反証）： dotnet run -- numdef [Q] ===
//   分子＝ExportsByGood+ImportsByGood（**四半期**の国際成約・金建て局所価格）／分母＝NominalGdp（Gdp＝Σ産出×RefPrice の**月次**×PriceLevel）。
//   ①暦の単位（月次GDP vs 四半期貿易）②価格の物差し（金建て局所価格 vs 参照価格）③同国取引 ④植民地財流入
//   を**すべて別々に**測って、同じ物差しに揃えた比を並べる。純テレメトリ＝挙動は1ビットも変えない。
if (args.Length > 0 && args[0] == "numdef")
{
    int ndQ = ArgInt(args, 1, 524);
    var ndm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in ndm.Nations) nn.TelemetryEnabled = false;
    ndm.Diplomacy.Log.Enabled = false;
    ndm.StartWarmup();
    int[] ndSamples = [4, 40, 80, 120, 160, 200, 240, 280, 320, 360, 400, 440, 480, ndQ];
    string[] ndWatch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    string NdName(int i) { var p = ndm.Nations[i].World.SelfPower; return p is null ? $"#{i}" : (p.BaseName.Length > 0 ? p.BaseName : p.Name); }
    Console.WriteLine("年,国,月次GDP実Ref,月次産出Ref,月次産出金建て,金建て割Ref,PriceLevel,州PriceLevel,為替,名目GDP,四半期貿易金建て,四半期越境Ref,四半期同国Ref,月次植民地財,A報告比%,B暦是正%,C金建て整合%,D参照建て%,E参照＋同国%,F植民地込み%,総産出Ref,付加価値Ref,総産出割付加価値,G付加価値分母%,H全部整合%");
    void NdDump(int q)
    {
        for (int wi = 0; wi < ndWatch.Length; wi++)
        {
            int idx = -1;
            for (int i = 0; i < ndm.Nations.Count; i++) if (NdName(i) == ndWatch[wi]) { idx = i; break; }
            if (idx < 0) continue;
            var w = ndm.Nations[idx].World;
            double er = w.ExchangeRate > 1e-9 ? w.ExchangeRate : 1.0;
            double prodRef = 0, prodGold = 0, plW = 0, plWsum = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double rw = 0;
                for (int g = 0; g < r.Markets.Length && g < Catalog.Goods.Length; g++)
                {
                    if (Catalog.Goods[g].Tier == GoodTier.Service) continue;
                    var m = r.Markets[g];
                    double rp = Catalog.RefPrices[g];
                    prodRef += m.LastProduced * rp;
                    prodGold += m.LastProduced * m.Price * er;
                    rw += m.LastProduced * rp;
                }
                plW += r.PriceLevel * rw; plWsum += rw;
            }
            // 付加価値（分母の物差し③）：Gdp＝Σ産出×RefPrice は**総産出**（中間投入の二重計上込み）。
            //   史実のGDPは付加価値なので、深いDAG（116ノード）では分母が数倍膨らんでいる可能性がある。
            //   付加価値＝Σ生産者[産出×RefPrice(財) − Σ投入 QtyPerOutput×産出×RefPrice(投入)]（非役務の投入だけ引く＝賃金は付加価値に残す）。
            double grossRef = 0, interRef = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var s in r.Producers)
                {
                    if (s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length) continue;
                    if (Catalog.Goods[s.GoodId].Tier == GoodTier.Service) continue;
                    grossRef += s.Output * Catalog.RefPrices[s.GoodId];
                    foreach (var inp in s.Inputs)
                    {
                        if (inp.GoodId < 0 || inp.GoodId >= Catalog.Goods.Length) continue;
                        if (Catalog.Goods[inp.GoodId].Tier == GoodTier.Service) continue;
                        interRef += s.Output * inp.QtyPerOutput * Catalog.RefPrices[inp.GoodId];
                    }
                }
            }
            double vaRef = grossRef - interRef;
            double colFull = 0;   // 旧・植民地の採取決済（Region.ColonialImportCost）は 2026-09-28 に削除＝常に0（列の形は保つ）
            double ei = (w.ExportsByGood?.Sum() ?? 0) + (w.ImportsByGood?.Sum() ?? 0);
            double xRef = TradeReachDiag.CrossRefByNation.Length > idx ? TradeReachDiag.CrossRefByNation[idx] : 0;
            double dRef = TradeReachDiag.DomRefByNation.Length > idx ? TradeReachDiag.DomRefByNation[idx] : 0;
            double nom = System.Math.Max(1.0, w.NominalGdp);
            double A = ei * 4.0 / nom * 100;                     // 旧定義（是正前・分子＝四半期×4／分母＝月次のまま＝正典 TradeGdp は既に暦を揃えて修正済み）
            double B = ei * 4.0 / (nom * 12) * 100;              // 暦を揃える（年貿易 ÷ 年名目GDP）
            double C = prodGold > 1e-9 ? ei * 4.0 / (prodGold * 12) * 100 : 0;   // 金建て同士（分母＝月次産出の金建て市場評価）
            double D = prodRef > 1e-9 ? xRef * 4.0 / (prodRef * 12) * 100 : 0;   // 参照価格同士（越境のみ）
            double E = prodRef > 1e-9 ? (xRef + dRef) * 4.0 / (prodRef * 12) * 100 : 0;   // 参照価格同士（同国も入れる）
            double F = prodGold > 1e-9 ? (ei * 4.0 + colFull * 12) / (prodGold * 12) * 100 : 0;   // 金建て＋植民地財流入（月次なので×12）
            double G = vaRef > 1e-9 ? xRef * 4.0 / (vaRef * 12) * 100 : 0;   // 参照建て・分母を付加価値へ（史実GDPと同じ物差し）
            double H = vaRef > 1e-9 && prodRef > 1e-9 ? (ei * 4.0) / (vaRef * (prodGold / prodRef) * 12) * 100 : 0;   // 全部そろえた最良推定（金建て分子÷金建て付加価値）
            Console.WriteLine(string.Join(",", new[]
            {
                (1783 + q / 4).ToString(), ndWatch[wi],
                $"{w.Gdp:F0}", $"{prodRef:F0}", $"{prodGold:F0}", $"{(prodRef > 1e-9 ? prodGold / prodRef : 0):F3}",
                $"{w.PriceLevel:F3}", $"{(plWsum > 1e-9 ? plW / plWsum : 0):F3}", $"{er:F4}", $"{w.NominalGdp:F0}",
                $"{ei:F0}", $"{xRef:F0}", $"{dRef:F0}", $"{colFull:F0}",
                $"{A:F2}", $"{B:F2}", $"{C:F2}", $"{D:F2}", $"{E:F2}", $"{F:F2}",
                $"{grossRef:F0}", $"{vaRef:F0}", $"{(vaRef > 1e-9 ? grossRef / vaRef : 0):F2}", $"{G:F2}", $"{H:F2}",
            }));
        }
    }
    for (int q = 1; q <= ndQ; q++)
    {
        bool on = System.Array.IndexOf(ndSamples, q) >= 0;
        TradeReachDiag.On = on;
        ndm.StepQuarter();
        if (on) { NdDump(q); TradeReachDiag.On = false; }
    }
    return;
}

// === 到達性の実測（2026-08-21・「そもそも相手に到達できているか」）： dotnet run -- reachprobe [Q] ===
//   ①州が海に出られているか（linked）②越境 vs 同国（統計から落ちる分＝定義の問題か実体の問題か）
//   ③実際に取引が立った国ペアの数。⚠旧経路だけの ④買い手の棄却理由 ⑤枝刈りの検証 は 2026-09-26 に旧経路ごと削除（econ-15）。
if (args.Length > 0 && args[0] == "reachprobe")
{
    int rpQ = ArgInt(args, 1, 524);
    var rpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in rpm.Nations) nn.TelemetryEnabled = false;
    rpm.Diplomacy.Log.Enabled = false;
    rpm.StartWarmup();
    int[] samples = [4, 40, 80, 120, 160, 200, 280, 360, 440, rpQ];
    string NameOf(int i) { var p = rpm.Nations[i].World.SelfPower; return p is null ? $"#{i}" : (p.BaseName.Length > 0 ? p.BaseName : p.Name); }
    void RpDump(int q)
    {
        Console.WriteLine($"=== {1783 + q / 4}（Q{q}） ===");
        Console.WriteLine($"  州: 総{TradeReachDiag.RegTotal} 接続{TradeReachDiag.RegLinked} 凍結{TradeReachDiag.RegFrozen} 港無し{TradeReachDiag.RegNoPortLink} 座標無し{TradeReachDiag.RegNoCoord}");
        double vc = TradeReachDiag.ValueCross, vd = TradeReachDiag.ValueDomestic;
        Console.WriteLine($"  成約額: 越境{vc:F0}（{TradeReachDiag.TxCross}件） 同国{vd:F0}（{TradeReachDiag.TxDomestic}件） 越境比{vc / System.Math.Max(1e-9, vc + vd):P1}　額加重平均距離{TradeReachDiag.KmValWeighted / System.Math.Max(1e-9, vc + vd):F0}km");
        Console.WriteLine($"  取引が立った国ペア（有向）: {TradeReachDiag.PairValue.Count}");
        var top = TradeReachDiag.PairValue.OrderByDescending(kv => kv.Value).Take(6)
            .Select(kv => $"{NameOf((int)(kv.Key / 1000))}→{NameOf((int)(kv.Key % 1000))}={kv.Value:F0}");
        Console.WriteLine("    上位: " + string.Join(" / ", top));
        string[] watch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
        foreach (var wn in watch)
        {
            int idx = -1;
            for (int i = 0; i < rpm.Nations.Count; i++) if (NameOf(i) == wn) { idx = i; break; }
            if (idx < 0) continue;
            var w = rpm.Nations[idx].World;
            double gdp = System.Math.Max(1.0, w.NominalGdp * Calendar.MonthsPerYear);   // NominalGdpは月次＝年換算は×MonthsPerYear（TradeGdp と同じ物差し）
            double stat = TradeGdp(w) * 100;
            double cross = TradeReachDiag.CrossByNation[idx] * 4.0 / gdp * 100;
            double dom = TradeReachDiag.DomByNation[idx] * 4.0 / gdp * 100;
            double colRev = 0;   // 旧・植民地の採取決済（Region.ColonialImportCost）は 2026-09-28 に削除＝常に0（列の形は保つ）
            Console.WriteLine($"    {wn,-6} 統計貿易/GDP {stat,6:F2}%　越境{cross,6:F2}%　同国{dom,7:F2}%　植民地財流入/GDP {colRev * 4.0 / gdp * 100,6:F2}%　州{TradeReachDiag.ActiveByNation[idx],3}中接続{TradeReachDiag.LinkedByNation[idx],3}　関税{w.Tariff:P0}");
        }
    }
    for (int q = 1; q <= rpQ; q++)
    {
        bool on = System.Array.IndexOf(samples, q) >= 0;
        TradeReachDiag.On = on;
        rpm.StepQuarter();
        if (on) { RpDump(q); TradeReachDiag.On = false; }
    }
    return;
}

// 【削除2026-09-26（棚卸し econ-15）】gapdec（量式の厳密分解）と vesselprobe（器 scaleT の反証）＝どちらも旧経路（定数速度×自州規模の量式
//   value = access × TradeSpeed × gap × scaleT × unit）を分解する計器で、既定の清算（S4＝水準式）では列の大半が常に0だった。旧経路ごと削除。

// === 購買力の実測（2026-08-21）： dotnet run -- budgetprobe [Q] ===
//   主張「買い手の購買力は枯れていない（需要Σ/生産Σ=0.92）」の検証。
//   ⚠需要Σ/生産Σ は**切られた需要**と**その需要に合わせて縮んだ生産**の比＝恒等式に近い。
//   binding かどうかは desired（希望）vs affordable（払える）でしか測れないので、両方を1表に出して突き合わせる。
if (args.Length > 0 && args[0] == "budgetprobe")
{
    int bpQ = ArgInt(args, 1, 524);
    var bpm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn in bpm.Nations) nn.TelemetryEnabled = false;
    bpm.Diplomacy.Log.Enabled = false;
    bpm.StartWarmup();
    if (PriceOfBread.SimCore.Coefficients.OverrideSummary() is { Length: > 0 } bpOv)
        Console.WriteLine($"★係数の上書き（POB_COEF）: {bpOv}");
    else Console.WriteLine("★係数の上書き（POB_COEF）: なし（既定）");
    string[] bpWatch = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    int[] bpSamples = [4, 40, 80, 120, 200, 280, 360, 440, bpQ];
    void BpDump(int q)
    {
        Console.WriteLine($"=== {1783 + q / 4}（Q{q}） ===");
        Console.WriteLine("  国        希望額     需要額 需要/希望 貿易財比 主食比 日用比 奢侈比 | 予算切れ行%   月初予算   使い残し 残% |  需要Σ/生産Σ 不足Σ/需要Σ 貿易/GDP% | LastDemandに占めるpop消費");
        foreach (var wn in bpWatch)
        {
            var nation = bpm.Nations.FirstOrDefault(n => n.World.SelfPower is { } p && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == wn);
            if (nation?.World is not { } w) continue;
            if (!PriceOfBread.SimCore.BudgetBindDiag.ByWorld.TryGetValue(w, out var a)) { Console.WriteLine($"  {wn,-6} （計数なし）"); continue; }
            double comp = a[PriceOfBread.SimCore.BudgetBindDiag.PopDemVal] + a[PriceOfBread.SimCore.BudgetBindDiag.InputDemVal]
                        + a[PriceOfBread.SimCore.BudgetBindDiag.InvestDemVal] + a[PriceOfBread.SimCore.BudgetBindDiag.MilDemVal];
            double R(int num, int den) => den < 0 ? (comp > 1e-9 ? a[num] / comp : 0) : (a[den] > 1e-9 ? a[num] / a[den] : 0);
            double dem = 0, prod = 0, shortg = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var m in r.Markets) { dem += m.LastDemand; prod += m.LastProduced; shortg += m.LastShortage; }
            }
            double gdp = System.Math.Max(1.0, w.NominalGdp);
            double tg = ((w.ExportsByGood?.Sum() ?? 0) + (w.ImportsByGood?.Sum() ?? 0)) * 4.0 / gdp * 100;
            Console.WriteLine($"  {wn,-6} {a[PriceOfBread.SimCore.BudgetBindDiag.DesiredVal],10:F0} {a[PriceOfBread.SimCore.BudgetBindDiag.DemandVal],10:F0}   {R(PriceOfBread.SimCore.BudgetBindDiag.DemandVal, PriceOfBread.SimCore.BudgetBindDiag.DesiredVal),6:F3}   {R(PriceOfBread.SimCore.BudgetBindDiag.DemandValTrade, PriceOfBread.SimCore.BudgetBindDiag.DesiredValTrade),6:F3} {R(PriceOfBread.SimCore.BudgetBindDiag.DemandValStaple, PriceOfBread.SimCore.BudgetBindDiag.DesiredValStaple),6:F3} {R(PriceOfBread.SimCore.BudgetBindDiag.DemandValDaily, PriceOfBread.SimCore.BudgetBindDiag.DesiredValDaily),6:F3} {R(PriceOfBread.SimCore.BudgetBindDiag.DemandValLux, PriceOfBread.SimCore.BudgetBindDiag.DesiredValLux),6:F3} |"
                + $" {(a[PriceOfBread.SimCore.BudgetBindDiag.Rows] > 0 ? 100.0 * a[PriceOfBread.SimCore.BudgetBindDiag.BoundRows] / a[PriceOfBread.SimCore.BudgetBindDiag.Rows] : 0),10:F1} {a[PriceOfBread.SimCore.BudgetBindDiag.BudgetStart],10:F0} {a[PriceOfBread.SimCore.BudgetBindDiag.BudgetLeft],10:F0} {(a[PriceOfBread.SimCore.BudgetBindDiag.BudgetStart] > 1e-9 ? 100.0 * a[PriceOfBread.SimCore.BudgetBindDiag.BudgetLeft] / a[PriceOfBread.SimCore.BudgetBindDiag.BudgetStart] : 0),4:F0} |"
                + $" {(prod > 1e-9 ? dem / prod : 0),11:F3} {(dem > 1e-9 ? shortg / dem : 0),10:F3} {tg,9:F2}"
                + $" | 需要の内訳 pop{R(PriceOfBread.SimCore.BudgetBindDiag.PopDemVal, -1):P0}");
        }
    }
    void BpWorld(int q)
    {
        // ★全世界の合計（6カ国の窓では周縁の切られ方が見えないため）。ByWorld は全 active 国を持つ。
        var B = new double[PriceOfBread.SimCore.BudgetBindDiag.Count];
        foreach (var kv in PriceOfBread.SimCore.BudgetBindDiag.ByWorld)
            for (int i = 0; i < B.Length; i++) B[i] += kv.Value[i];
        double W(int num, int den) => B[den] > 1e-9 ? B[num] / B[den] : 0;
        double wDem = 0, wProd = 0, wTradeVal = 0, wGdp = 0;
        foreach (var n in bpm.Nations)
        {
            var w = n.World; if (w.SelfPower is null) continue;
            wGdp += System.Math.Max(1.0, w.NominalGdp);
            wTradeVal += (w.ExportsByGood?.Sum() ?? 0) + (w.ImportsByGood?.Sum() ?? 0);
            foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; foreach (var m in r.Markets) { wDem += m.LastDemand; wProd += m.LastProduced; } }
        }
        Console.WriteLine($"  【全世界計 {PriceOfBread.SimCore.BudgetBindDiag.ByWorld.Count}カ国】 需要/希望 {W(PriceOfBread.SimCore.BudgetBindDiag.DemandVal, PriceOfBread.SimCore.BudgetBindDiag.DesiredVal):F3}"
            + $"　貿易財比 {W(PriceOfBread.SimCore.BudgetBindDiag.DemandValTrade, PriceOfBread.SimCore.BudgetBindDiag.DesiredValTrade):F3}"
            + $"　主食比 {W(PriceOfBread.SimCore.BudgetBindDiag.DemandValStaple, PriceOfBread.SimCore.BudgetBindDiag.DesiredValStaple):F3}"
            + $"　日用比 {W(PriceOfBread.SimCore.BudgetBindDiag.DemandValDaily, PriceOfBread.SimCore.BudgetBindDiag.DesiredValDaily):F3}"
            + $"　奢侈比 {W(PriceOfBread.SimCore.BudgetBindDiag.DemandValLux, PriceOfBread.SimCore.BudgetBindDiag.DesiredValLux):F3}"
            + $"　予算切れ行 {(B[PriceOfBread.SimCore.BudgetBindDiag.Rows] > 0 ? 100.0 * B[PriceOfBread.SimCore.BudgetBindDiag.BoundRows] / B[PriceOfBread.SimCore.BudgetBindDiag.Rows] : 0):F1}%"
            + $"　需要Σ/生産Σ {(wProd > 1e-9 ? wDem / wProd : 0):F3}"
            // ★2026-09-03 是正（診断専用＝力学に影響なし）：⚠旧 `wTradeVal * 4.0 / wGdp` は**分子だけを年換算**していた
            //   （しかも ×4＝四半期清算時代の名残で、月次清算の今は ×12 が正しい）。分母 `wGdp` は月次のまま＝**4倍過大**。
            //   ★両方とも清算/月次の同じ期間なので、正しい比は**年換算を掛けずにそのまま割る**（×12/×12 は約分される）。
            //   ⚠この計器で読んだ過去の貿易/GDP は4倍に読めていた（monoprobe＝Σ貿易×年換算/ΣGDP×12 と食い違っていた実体）。
            + $"　世界貿易/GDP {(wGdp > 1e-9 ? wTradeVal / wGdp * 100 : 0):F2}%");
    }
    for (int q = 1; q <= bpQ; q++)
    {
        bool on = System.Array.IndexOf(bpSamples, q) >= 0;
        PriceOfBread.SimCore.BudgetBindDiag.On = on;
        if (on) PriceOfBread.SimCore.BudgetBindDiag.Begin();
        bpm.StepQuarter();
        if (on) { PriceOfBread.SimCore.BudgetBindDiag.On = false; BpDump(q); BpWorld(q); }
    }
    return;
}

// === 03地理 手順3（2026-07-28）：海グラフの診断： dotnet run -- seagraph ===
// §2-1 の実測9経路（旧SeaDistanceKmは欧州域内×0.23〜アフリカ横断×0.38の系統誤差）＋連結性＋通路の効き（スエズ開閉差）。
if (args.Length > 0 && args[0] == "seagraph")
{
    var (comps, largest) = SeaGraph.Connectivity();
    Console.WriteLine($"# 海グラフ診断（k={SeaGraph.K}・陸横断棄却 {SeaGraph.LandRejectKm}km＝内陸ノード基準）");
    Console.WriteLine($"海ノード {SeaGraph.Nodes.Length}　辺 {SeaGraph.Edges.Length}　連結成分 {comps}（最大 {largest}）※全海は1成分が理想（通路で接続）\n");
    Console.WriteLine("## §2-1 実測経路（実際の海路との比＝×0.6〜1.7帯が目標・スエズ/パナマ閉＝1789年状態）");
    (string A, double aLat, double aLon, string B, double bLat, double bLon, double actualKm)[] cases =
    [
        ("ハンブルク", 53.55, 9.99, "マルセイユ", 43.3, 5.37, 5100),
        ("ロンドン", 51.51, -0.13, "ナポリ", 40.85, 14.27, 5300),
        ("ストックホルム", 59.33, 18.07, "ナポリ", 40.85, 14.27, 6800),
        ("ラゴス沖", 6.45, 3.4, "ザンジバル", -6.2, 39.2, 11000),
        ("リスボン", 38.72, -9.14, "イスタンブール", 41.0, 29.0, 4000),
        ("ニューヨーク", 40.71, -74.01, "ブエノスアイレス", -34.6, -58.4, 9800),
        ("ロンドン", 51.51, -0.13, "ケープタウン", -33.9, 18.4, 11000),
        ("ロンドン", 51.51, -0.13, "ボンベイ", 19.0, 72.8, 18000),
        ("リオ", -22.9, -43.2, "リマ", -12.0, -77.0, 7000),
        ("ロンドン", 51.51, -0.13, "東地中海", 33.5, 30.0, 6000),   // スエズ短絡の前半（地中海を東へ抜けられるか）
    ];
    foreach (var cse in cases)
    {
        double g = SeaGraph.SeaDistanceKm(cse.aLat, cse.aLon, cse.bLat, cse.bLon, false, false);
        double old = Geo.SeaDistanceKm(cse.aLat, cse.aLon, cse.bLat, cse.bLon, false, false);
        Console.WriteLine($"- {cse.A}→{cse.B}　旧 {old:F0}km(×{old / cse.actualKm:F2})　グラフ {g:F0}km(×{g / cse.actualKm:F2})　実際 {cse.actualKm}km");
        var path = SeaGraph.RoutePath(cse.aLat, cse.aLon, cse.bLat, cse.bLon, false, false);
        if (path.Count > 0) Console.WriteLine($"    経路: {string.Join("→", path)}");
    }
    Console.WriteLine("\n## 通路の効き（ロンドン→ボンベイ）");
    double closed = SeaGraph.SeaDistanceKm(51.51, -0.13, 19.0, 72.8, false, false);
    double open = SeaGraph.SeaDistanceKm(51.51, -0.13, 19.0, 72.8, true, false);
    Console.WriteLine($"スエズ閉 {closed:F0}km（喜望峰）　スエズ開 {open:F0}km　短絡 {closed - open:F0}km（実史≈8,000km短縮）");
    var sopen = SeaGraph.RoutePath(51.51, -0.13, 19.0, 72.8, true, false);
    Console.WriteLine($"スエズ開の経路: {string.Join("→", sopen)}");
    // 孤立成分の中身（2番目以降＝データ穴）
    {
        int n2 = SeaGraph.Nodes.Length;
        var adj2 = new List<int>[n2];
        for (int i = 0; i < n2; i++) adj2[i] = new List<int>();
        foreach (var e in SeaGraph.Edges) { adj2[e.A].Add(e.B); adj2[e.B].Add(e.A); }
        var comp2 = new int[n2]; System.Array.Fill(comp2, -1);
        int nc2 = 0;
        for (int i = 0; i < n2; i++)
        {
            if (comp2[i] >= 0) continue;
            var st2 = new Stack<int>(); st2.Push(i); comp2[i] = nc2;
            while (st2.Count > 0) { int u = st2.Pop(); foreach (var v in adj2[u]) if (comp2[v] < 0) { comp2[v] = nc2; st2.Push(v); } }
            nc2++;
        }
        var groups = Enumerable.Range(0, n2).GroupBy(i => comp2[i]).OrderByDescending(g => g.Count());
        Console.WriteLine("\n## 成分の内訳（2番目以降＝孤立＝データ穴）");
        foreach (var g in groups.Skip(1))
            Console.WriteLine($"- [{g.Count()}] {string.Join("・", g.Select(i => SeaGraph.Nodes[i].Name))}");
    }
    // ★2026-08-02：**絵を見て探すのをやめる**ための2つの計器（作者指摘「ジブラルタルが閉鎖されても迂回できそう」）。
    // 千数百本の線から目視で誤りを探すのは原理的に無理なので、通路の意味と陸横断の余裕を数値で出す。
    Console.WriteLine("\n## 通路の実効性（その通路を1本だけ抜いたとき、両端はまだ繋がるか）");
    Console.WriteLine("※迂回距離が通行費に近い＝その通路を閉じても何も変わらない＝通路として意味が無い");
    foreach (var p in SeaGraph.PassageBypassReport().OrderByDescending(p => p.DirectKm <= 0 ? 0 : p.AltKm / p.DirectKm))
    {
        string alt = double.IsPositiveInfinity(p.AltKm) ? "∞（唯一の道）" : $"{p.AltKm:F0}km";
        string mult = double.IsPositiveInfinity(p.AltKm) || p.DirectKm <= 0 ? "" : $"（通行費の×{p.AltKm / p.DirectKm:F1}）";
        string gate = p.ChokeId is null ? "ゲート無し" : p.ChokeId is "suez" or "panama" ? $"{p.ChokeId}＝経路計算が見る" : $"{p.ChokeId}＝⚠経路計算は見ない（海盆遮断のみ）";
        Console.WriteLine($"- {p.Name}　通行費 {p.DirectKm:F0}km　迂回 {alt}{mult}　[{gate}]");
        if (p.AltPath.Length > 0) Console.WriteLine($"    迂回経路: {p.AltPath}");
    }
    // ★2026-08-02：接ぎ木（港→海ノード）が陸を越えていないか（作者報告「大阪の線が日本海へ伸びて途切れる」）。
    Console.WriteLine("\n## 港が海へ出る入口（接ぎ木）");
    (string Name, double Lat, double Lon)[] ports =
    [
        ("大阪", 34.69, 135.50), ("江戸", 35.68, 139.69), ("長崎", 32.75, 129.87), ("横浜", 35.44, 139.64),
        ("チャールストン", 32.78, -79.93), ("ニューヨーク", 40.71, -74.01), ("ボストン", 42.36, -71.06),
        ("上海", 31.23, 121.47), ("広州", 23.13, 113.26), ("ボンベイ", 19.00, 72.80),
        ("リスボン", 38.72, -9.14), ("ヴェネツィア", 45.44, 12.32), ("イスタンブール", 41.01, 28.98),
        ("ハンブルク", 53.55, 9.99), ("マルセイユ", 43.30, 5.37), ("アレクサンドリア", 31.20, 29.92),
    ];
    foreach (var p in ports)
    {
        int ni = SeaGraph.NearestSeaNode(p.Lat, p.Lon);
        var nd = SeaGraph.Nodes[ni];
        double km = Geo.DistanceKm(p.Lat, p.Lon, nd.Lat, nd.Lon);
        // 接ぎ木の線が陸を横切るかを、エンジンと同じ規則で数える
        // ⚠**出港の一歩目は数えない**（港は水際の町なので格子の上では陸のマスに乗る）。一度海へ出たあとの陸だけが横断。
        int n2 = Math.Max(9, (int)Math.Ceiling(km / (Landmask.Step * 111.0 / 2.0)));
        double dl = nd.Lon - p.Lon; if (dl > 180) dl -= 360; else if (dl < -180) dl += 360;
        bool afloat = false, crosses = false;
        for (int s = 1; s <= n2 && !crosses; s++)
        {
            double t = s / (double)(n2 + 1);
            if (!Landmask.IsLand(p.Lat + (nd.Lat - p.Lat) * t, p.Lon + dl * t)) afloat = true;
            else if (afloat) crosses = true;
        }
        Console.WriteLine($"  {(crosses ? "⚠陸を横切る" : "✓")} {p.Name,-12} → {nd.Name,-14} {km:F0}km");
    }
    Console.WriteLine("\n## 陸横断の余裕（沿岸までの距離 − 内陸までの距離。小さいほど際どい／負なら陸の上）");
    var lc = SeaGraph.LandClearanceReport();
    Console.WriteLine($"辺 {lc.Length} 本中、**陸判定が構造的に一度も走らない辺**（長さ ≤ 2×救済半径 {2 * SeaGraph.SeaRescueKm:F0}km）＝ {lc.Count(x => x.RescueExempt)} 本");
    Console.WriteLine("### 余裕の小さい10本（陸を横切りかけている＝データ穴の候補）");
    foreach (var x in lc.Where(x => !x.RescueExempt).OrderBy(x => x.WorstMarginKm).Take(10))
        Console.WriteLine($"  - {x.A}↔{x.B}　{x.Km:F0}km　余裕 {x.WorstMarginKm:F0}km");
    Console.WriteLine("### 陸判定を受けない辺のうち長い10本（⚠この長さで陸を横切っていても検出できない）");
    foreach (var x in lc.Where(x => x.RescueExempt).OrderByDescending(x => x.Km).Take(10))
        Console.WriteLine($"  - {x.A}↔{x.B}　{x.Km:F0}km　余裕 {x.WorstMarginKm:F0}km");
    return;
}

// === 03地理 手順1（2026-07-28）：陸グラフの診断： dotnet run -- landgraph ===
// 連結性（§14-2）・代表経路の実測比（§4-1）・辺とバリアの規模を印字。地図表示と並ぶ「grepで見えない地理の型F」の検出器。
// ★landaudit（2026-09-05・作者指摘「陸上取引の一部が内海を無視している／山脈などのバリアは正しく機能しているか」）＝読むだけの計器。
//   ①陸の辺の内部を約10km刻みで陸マスク（Landmask＝海岸線を1格子内側へ収縮した真値）に当て、**連続した水**の最長区間を測る。
//     ⚠収縮の分を戻すため「その格子か4近傍のどれかが陸」なら陸とみなす（＝陸から約11km以内は陸）。実水域だけが残る。
//   ②バリアの壁＝同じ山脈名（（…）と東西南北中央部を除いた語幹）の点どうし（700km以内）を結ぶ線分。陸の辺がその線分を
//     **どちらの点からも BarrierClearanceKm より遠い所で**横切れば「隙間抜け」。端点が壁の点から110km以内（地元免除）で横切る辺は「免除抜け」。
if (args.Length > 0 && args[0] == "landaudit")
{
    if (args.Length > 1)
    {
        foreach (var name in args.Skip(1)) foreach (var line in PathGraph.ExplainCandidates(name, 8)) Console.WriteLine(line);
        return;
    }
    static bool LandOrCoast(double lat, double lon)
    {
        const double st = Landmask.Step;
        return Landmask.IsLand(lat, lon) || Landmask.IsLand(lat + st, lon) || Landmask.IsLand(lat - st, lon) || Landmask.IsLand(lat, lon + st) || Landmask.IsLand(lat, lon - st);
    }
    static (double X, double Y, double Z) U(double lat, double lon)
    {
        double la = lat * Math.PI / 180, lo = lon * Math.PI / 180;
        return (Math.Cos(la) * Math.Cos(lo), Math.Cos(la) * Math.Sin(lo), Math.Sin(la));
    }
    static (double Lat, double Lon) Slerp(double lat1, double lon1, double lat2, double lon2, double t)
    {
        var a = U(lat1, lon1); var b = U(lat2, lon2);
        double dot = Math.Clamp(a.X * b.X + a.Y * b.Y + a.Z * b.Z, -1, 1);
        double w = Math.Acos(dot); if (w < 1e-12) return (lat1, lon1);
        double sa = Math.Sin((1 - t) * w) / Math.Sin(w), sb = Math.Sin(t * w) / Math.Sin(w);
        double x = sa * a.X + sb * b.X, y = sa * a.Y + sb * b.Y, z = sa * a.Z + sb * b.Z;
        return (Math.Atan2(z, Math.Sqrt(x * x + y * y)) * 180 / Math.PI, Math.Atan2(y, x) * 180 / Math.PI);
    }
    var N = PathGraph.Nodes; var E = PathGraph.Edges;
    Console.WriteLine($"# 陸グラフ監査（辺 {E.Length}・バリア {N.Count(x => x.Kind == "barrier")}・峠 {N.Count(x => x.Kind == "pass")}・陸マスク {Landmask.Step}°）");
    // ---- ① 水を渡る陸の辺 ----
    var water = new List<(double Run, double Total, double Km, string A, string B, string Ka, string Kb)>();
    int landEdges = 0;
    foreach (var e in E)
    {
        var a = N[e.A]; var b = N[e.B];
        double km = Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon);
        int steps = Math.Max(2, (int)Math.Ceiling(km / 10.0));
        double run = 0, best = 0, total = 0, step = km / steps;
        for (int i = 1; i < steps; i++)
        {
            var (la, lo) = Slerp(a.Lat, a.Lon, b.Lat, b.Lon, (double)i / steps);
            if (!LandOrCoast(la, lo)) { run += step; total += step; if (run > best) best = run; }
            else run = 0;
        }
        landEdges++;
        if (best >= 30.0)
        {
            // 器>0 では張られている辺は「直線は水でも沿岸迂回が採られた辺」＝迂回の長さを併記（迂回が無ければ器0で張られた水渡り）
            bool cw = PathGraph.EdgeCrossesWater(e.A, e.B, out double pkm);
            string kind = !cw && pkm > km + 1 ? $"沿岸迂回 {pkm:F0}km" : "水を渡る";
            water.Add((best, total, km, a.Name, b.Name, kind, b.Kind));
        }
    }
    water.Sort((x, y) => y.Run.CompareTo(x.Run));
    int nDetour = water.Count(w => w.Ka.StartsWith("沿岸迂回"));
    Console.WriteLine($"\n## ① 直線が水域を通る陸の辺（連続した水 ≥30km）：{water.Count}/{landEdges} 本（うち沿岸迂回で張った辺 {nDetour}・水を渡る辺 {water.Count - nDetour}）");
    foreach (var w in water) Console.WriteLine($"  - {w.A}↔{w.B}　辺 {w.Km:F0}km　水の最長区間 {w.Run:F0}km（合計 {w.Total:F0}km）　{w.Ka}");
    // ---- ② バリアの壁と隙間 ----
    static string Stem(string name)
    {
        int k = name.IndexOf('（'); if (k >= 0) name = name[..k];
        k = name.IndexOf('('); if (k >= 0) name = name[..k];
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name) if ("東西南北中央部".IndexOf(ch) < 0) sb.Append(ch);
        return sb.ToString();
    }
    var bars = Enumerable.Range(0, N.Length).Where(i => N[i].Kind == "barrier").ToArray();
    var groups = bars.GroupBy(i => Stem(N[i].Name)).ToDictionary(g => g.Key, g => g.ToArray());
    Console.WriteLine($"\n## ② バリアの壁（語幹で {groups.Count} 群）と点の間隔");
    var walls = new List<(int I, int J, string Stem, double Km)>();
    foreach (var (stem, idx) in groups.OrderBy(g => g.Key))
    {
        var seg = new List<string>();
        for (int x = 0; x < idx.Length; x++)
            for (int y = x + 1; y < idx.Length; y++)
            {
                double km = Geo.DistanceKm(N[idx[x]].Lat, N[idx[x]].Lon, N[idx[y]].Lat, N[idx[y]].Lon);
                if (km <= 700) { walls.Add((idx[x], idx[y], stem, km)); seg.Add($"{km:F0}"); }
            }
        double gapMax = seg.Count > 0 ? seg.Max(v => double.Parse(v)) : 0;
        Console.WriteLine($"  - {stem}：点 {idx.Length}・壁の線分 {seg.Count}（最長 {gapMax:F0}km）{(idx.Length == 1 ? "　⚠1点＝半径110kmの島" : gapMax > 2 * PathGraph.BarrierClearanceKm ? "　⚠隙間あり（>220km）" : "")}");
    }
    var gap = new List<string>(); var exempt = new List<string>(); int passEdges = 0;
    foreach (var e in E)
    {
        var a = N[e.A]; var b = N[e.B];
        if (a.Kind == "pass" || b.Kind == "pass") { passEdges++; continue; }
        var ua = U(a.Lat, a.Lon); var ub = U(b.Lat, b.Lon);
        foreach (var w in walls)
        {
            var uc = U(N[w.I].Lat, N[w.I].Lon); var ud = U(N[w.J].Lat, N[w.J].Lon);
            if (!PathGraph.ArcsCross(ua, ub, uc, ud, out var p)) continue;   // エンジンの壁判定と同じ1本（2026-09-11 #19c）
            double plat = Math.Atan2(p.Z, Math.Sqrt(p.X * p.X + p.Y * p.Y)) * 180 / Math.PI, plon = Math.Atan2(p.Y, p.X) * 180 / Math.PI;
            double dI = Geo.DistanceKm(plat, plon, N[w.I].Lat, N[w.I].Lon), dJ = Geo.DistanceKm(plat, plon, N[w.J].Lat, N[w.J].Lon);
            double cl = PathGraph.BarrierClearanceKm;
            bool nearI = dI < cl, nearJ = dJ < cl;
            string line = $"{a.Name}↔{b.Name}（{Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon):F0}km）が {w.Stem}［{N[w.I].Name}—{N[w.J].Name} {w.Km:F0}km］を横切る：交点は点から {dI:F0}/{dJ:F0}km";
            if (!nearI && !nearJ) gap.Add(line);
            else
            {
                // 近い点があるのに辺が張られている＝地元免除（端点がその点から110km以内）でしか説明できない
                int k = nearI ? w.I : w.J;
                bool localA = Geo.DistanceKm(N[k].Lat, N[k].Lon, a.Lat, a.Lon) < cl, localB = Geo.DistanceKm(N[k].Lat, N[k].Lon, b.Lat, b.Lon) < cl;
                if (localA || localB) exempt.Add(line + $"　地元免除＝{(localA ? a.Name : b.Name)} が {N[k].Name} から110km以内");
            }
        }
    }
    Console.WriteLine($"\n## ②-a 壁の**隙間**を抜ける辺（交点が両側の点から110kmより遠い）：{gap.Distinct().Count()} 本（峠を端点とする辺 {passEdges} 本は除外）");
    foreach (var g in gap.Distinct()) Console.WriteLine("  - " + g);
    Console.WriteLine($"\n## ②-b 地元免除で壁を抜ける辺（端点が壁の点から110km以内）：{exempt.Distinct().Count()} 本");
    foreach (var g in exempt.Distinct()) Console.WriteLine("  - " + g);
    return;
}
if (args.Length > 0 && args[0] == "landgraph")
{
    var (comps, largest) = PathGraph.Connectivity();
    int nodes = PathGraph.Nodes.Length, barriers = PathGraph.Nodes.Count(x => x.Kind == "barrier"), passes = PathGraph.Nodes.Count(x => x.Kind == "pass");
    Console.WriteLine($"# 陸グラフ診断（k={PathGraph.K}・峠割増×{PathGraph.PassCostMult}）");
    Console.WriteLine($"ノード {nodes}（州/植民地 {nodes - barriers - passes}・バリア {barriers}・峠 {passes}）　辺 {PathGraph.Edges.Length}");
    Console.WriteLine($"連結成分 {comps}（最大成分 {largest}ノード）※島嶼・別大陸は別成分が正しい\n");
    Console.WriteLine("## 代表経路（§4-1 の許容帯＝実際の陸路×0.6〜1.7）");
    (string A, double aLat, double aLon, string B, double bLat, double bLon, double actualKm)[] cases =
    [
        ("ミラノ", 45.46, 9.19, "ミュンヘン", 48.14, 11.58, 590),
        ("マドリード", 40.42, -3.7, "パリ", 48.85, 2.35, 1450),
        ("ケルン", 50.94, 6.96, "ナポリ", 40.85, 14.27, 1600),
        ("パリ", 48.85, 2.35, "マルセイユ", 43.3, 5.37, 863),
        ("モスクワ", 55.76, 37.62, "北京", 39.9, 116.4, 7000),
        ("デリー", 28.61, 77.21, "ラサ", 29.65, 91.1, 1355),
    ];
    foreach (var cse in cases)
    {
        double g = PathGraph.LandDistanceKm(cse.aLat, cse.aLon, cse.bLat, cse.bLon);
        double gc = Geo.DistanceKm(cse.aLat, cse.aLon, cse.bLat, cse.bLon);
        string ratio = double.IsPositiveInfinity(g) ? "到達不能" : $"×{g / cse.actualKm:F2}";
        Console.WriteLine($"- {cse.A}→{cse.B}　大圏 {gc:F0}km　グラフ {(double.IsPositiveInfinity(g) ? "∞" : $"{g:F0}km")}　実際 {cse.actualKm}km　比 {ratio}");
        var path = PathGraph.RoutePath(cse.aLat, cse.aLon, cse.bLat, cse.bLon);
        if (path.Count > 0) Console.WriteLine($"    経路: {string.Join("→", path)}");
    }
    // ★2026-08-02：辺の長さの分布と次数の上位（作者報告「ケープタウンに大量の陸路」「アフリカを横断する無数の陸路」）。
    // k は**個数**であって距離ではないので、ノード密度が地域で11倍違う（ユーラシア154州 vs アフリカ14州）と
    // 同じ k でも辺の長さが桁で変わる。ここはその歪みを数字で見るための計器。
    {
        var lens = PathGraph.Edges.Select(e => e.Km).OrderBy(x => x).ToArray();
        double Pct(double p) => lens[(int)Math.Min(lens.Length - 1, Math.Max(0, Math.Round(p * (lens.Length - 1))))];
        Console.WriteLine($"\n## 辺の長さ（km）　中央値 {Pct(0.5):F0}　75% {Pct(0.75):F0}　90% {Pct(0.9):F0}　99% {Pct(0.99):F0}　最長 {lens[^1]:F0}");
        Console.WriteLine("### 最長10本");
        foreach (var e in PathGraph.Edges.OrderByDescending(e => e.Km).Take(10))
            Console.WriteLine($"  - {PathGraph.Nodes[e.A].Name}↔{PathGraph.Nodes[e.B].Name}　{e.Km:F0}km");
        var deg = new int[PathGraph.Nodes.Length];
        foreach (var e in PathGraph.Edges) { deg[e.A]++; deg[e.B]++; }
        Console.WriteLine($"### 次数の多いノード上位10（k＝{PathGraph.K}・相互に張るので k を超える）");
        foreach (var i in Enumerable.Range(0, deg.Length).OrderByDescending(i => deg[i]).ThenBy(i => i).Take(10))
            Console.WriteLine($"  - {PathGraph.Nodes[i].Name}　次数 {deg[i]}　最長辺 {PathGraph.Edges.Where(e => e.A == i || e.B == i).Max(e => e.Km):F0}km");
        var iso = Enumerable.Range(0, deg.Length).Where(i => deg[i] == 0 && PathGraph.Nodes[i].Kind is "state" or "colony").ToArray();
        Console.WriteLine($"### 孤立した州/植民地（次数0） {iso.Length} ノード： {string.Join("・", iso.Select(i => PathGraph.Nodes[i].Name))}");
        // ★2026-08-02：名前を渡すとその州の隣人を出す（作者報告「ボルドーからの線が途切れている」の追跡用）
        //   dotnet run -- landgraph ボルドー
        if (args.Length > 1)
        {
            int qi = System.Array.FindIndex(PathGraph.Nodes, x => x.Name == args[1]);
            Console.WriteLine($"\n### {args[1]} の隣人");
            if (qi < 0) Console.WriteLine("  （そのノードは陸グラフに無い）");
            else foreach (var e in PathGraph.Edges.Where(e => e.A == qi || e.B == qi).OrderBy(e => e.Km))
            {
                var o = PathGraph.Nodes[e.A == qi ? e.B : e.A];
                Console.WriteLine($"  - {o.Name}（{o.Kind}）　{e.Km:F0}km");
            }
        }
    }
    return;
}

// === ★発見の計器（段2A・2026-10-01）：誰がいつ何を発見したか・点数の3項の生の強度（観測だけ）： dotnet run -- discprobe [seed=1] [四半期=668] [間隔=40] [断面の行=既定の13行｜all｜行名,行名…] ===
// 設計 §7 段2A の番人の計器「誰が最初に見つけたか（発見の国と年を記録する動詞＝観測だけ）」。selfplay-batch と同じ助走（StartWarmup）を通す。
// 出力：①開幕の既知 ②発見の出来事（年・国・行・直前の四半期の3項）③間隔ごとの主要国×主要行の生の強度（技術水準÷難易度・押し・必要性の強度・接触）④行ごとの最初の国と年・主要国の年。
// 生の強度は3項から逆算（必要性 n＝70s÷(s＋半分点) → s＝半分点×n÷(70−n)）＝エンジンの式を写さない。
if (args.Length > 0 && args[0] == "discprobe")
{
    ulong dpSeed = ArgUlong(args, 1, 1UL);
    int dpQ = ArgInt(args, 2, Coefficients.GameQuarters);
    int dpEvery = ArgInt(args, 3, 40);
    var msDp = new MultiNationSim(WorldData.Nations(), dpSeed, _ => new UtilityAgent());
    msDp.StartWarmup();
    var cDp = msDp.Nations[0].World.Coef;
    var rowsDp = Catalog.Discoveries;
    string[] majors = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "スペイン", "オスマン", "日本", "清", "ネーデルラント"];
    string[] keyRows = ["蒸気機関", "甜菜糖", "機関車", "汽船", "転炉鋼", "石炭火力", "電動機", "機関銃", "装甲艦", "自動車", "ラジオ", "合成ゴム", "窒素固定"];
    if (args.Length > 4) keyRows = args[4] == "all" ? rowsDp.Select(x => x.Name).ToArray() : args[4].Split(',');   // 断面に出す行（誰も発見しない行の3項を読む）
    string NameOf(World w) => w.SelfPower?.BaseName ?? w.Region.Name;
    Console.WriteLine($"# discprobe seed={dpSeed} 四半期={dpQ}　Y={cDp.DiscoveryTechPerDecade}・半分点 製法{cDp.DiscoveryNeedHalfProcess}/新兵器{cDp.DiscoveryNeedHalfWeapon}/新製品{cDp.DiscoveryNeedHalfProduct}/制度{cDp.DiscoveryNeedHalfInstitution}/思想{cDp.DiscoveryNeedHalfIdea}/伝わり{cDp.DiscoveryContactHalf}/産地{cDp.DiscoveryNeedHalfSite}・産地の見つける力の人の半分点{cDp.DiscoverySiteFindHalfPop}");
    // ★(148) 産地の発見（Catalog.Sites）＝産地の生産者の2値の立ち上がり（年・国・州・行＜直前の四半期の 技術力/必要性＞）と、断面ごとのまだの産地の2項・人の数・財の値段÷掘る費用（(171)）
    // ★(150)→(159) 値段の跳ねで決まった解禁（作者裁定 Q12「平滑化した価格」・計器だけ）＝値段を読む行（製法・新製品＝(159) から生み出す物の値段）と産地の発見の出来事のうち、
    //   判定の値段（エンジンと同じ平滑化した値段）で点数が100以上だが、持続する値段（瞬間の実質の値段の直近 N 四半期の平均・その四半期を含む）で読むと100に届かない解禁（AI の定義）。
    //   ★(159)：N＝判定の平滑と同じ平均の遅れ（指数平滑の遅れ (1−率)÷率＝窓の平均の遅れ (N−1)÷2 → N＝2(1−率)÷率＋1 の四捨五入・(154) 反証の指摘「物差しの遅れ」をそろえた）。
    //   参考に、瞬間の値段で読んだ点数（判定が瞬間の値段なら＝(147) の形）も同じ出来事で数える。
    var spRing = new Dictionary<World, Queue<double[]>>();
    var spMean = new Dictionary<World, double[]>();
    var spInst = new Dictionary<World, double[]>();
    double spRate = Discovery.PriceSmoothingQ(cDp);
    int spN = (int)System.Math.Round(2.0 * (1.0 - spRate) / spRate + 1.0);
    int spEv = 0, spGate = 0, spHit = 0, spInstGate = 0, spInstHit = 0;
    var spRows = new SortedDictionary<string, (int Ev, int Hit, int InstHit)>();
    void SpikeRing()
    {
        foreach (var dsim in msDp.Nations)
        {
            var w = dsim.World;
            var x = Discovery.InstantRealPrices(w);
            if (!spRing.TryGetValue(w, out var qq)) { qq = new Queue<double[]>(); spRing[w] = qq; }
            qq.Enqueue(x); while (qq.Count > spN) qq.Dequeue();
            var m = new double[x.Length]; var mn = new int[x.Length];
            foreach (var v in qq) for (int g = 0; g < m.Length; g++) if (v[g] >= 0) { m[g] += v[g]; mn[g]++; }   // ★(165) 観測なし（死んだ市場＝NoPrice）は平均に入れない
            for (int g = 0; g < m.Length; g++) m[g] = mn[g] > 0 ? m[g] / mn[g] : Discovery.NoPrice;
            spMean[w] = m; spInst[w] = x;
        }
    }
    void SpikeCount(string name, double gateScore, double refScore, double instScore)
    {
        spEv++;
        bool g = gateScore >= Discovery.GateScore, h = g && !(refScore >= Discovery.GateScore);
        bool gi = instScore >= Discovery.GateScore, hi = gi && !(refScore >= Discovery.GateScore);
        if (g) spGate++;
        if (h) spHit++;
        if (gi) spInstGate++;
        if (hi) spInstHit++;
        spRows[name] = spRows.TryGetValue(name, out var t) ? (t.Ev + 1, t.Hit + (h ? 1 : 0), t.InstHit + (hi ? 1 : 0)) : (1, h ? 1 : 0, hi ? 1 : 0);
    }
    var siteSeen = new HashSet<Producer>();
    var siteLast = new Dictionary<Producer, Discovery.Terms>();
    void SiteEvents()
    {
        foreach (var dsim in msDp.Nations)
        {
            var w = dsim.World;
            foreach (var rg in w.Regions)
                foreach (var p in rg.Producers)
                {
                    if (p.SiteRow < 0) continue;
                    if (p.SiteFound && siteSeen.Add(p))
                    {
                        string tt = siteLast.TryGetValue(p, out var lt) ? $"{lt.Tech:F0}/{lt.Need:F0}" : "—";
                        Console.WriteLine($"{w.Year} {NameOf(w)} {rg.Name} 産地：{Catalog.Sites[p.SiteRow].Name} ＜{tt}＞");
                        if (spMean.TryGetValue(w, out var mref))
                            SpikeCount(Catalog.Sites[p.SiteRow].Name, Discovery.SiteTerms(w, rg, p, w.Coef).Score, Discovery.SiteTerms(w, rg, p, w.Coef, mref).Score, Discovery.SiteTerms(w, rg, p, w.Coef, spInst[w]).Score);
                    }
                    if (!p.SiteFound && rg.State == RegionState.Active) siteLast[p] = Discovery.SiteTerms(w, rg, p, w.Coef);
                }
        }
    }
    void SiteSection()
    {
        var parts = new List<string>();
        foreach (var dsim in msDp.Nations)
        {
            var w = dsim.World;
            foreach (var rg in w.Regions)
                foreach (var p in rg.Producers)
                {
                    if (p.SiteRow < 0 || p.SiteFound) continue;
                    var sp3 = new double[3];   // ★(171) 人の数・その財の値段・その国で今のレシピで掘る費用（エンジンの関数が書く＝式を写さない）
                    var st = Discovery.SiteTerms(w, rg, p, w.Coef, partsOut: sp3);
                    parts.Add($"{Catalog.Sites[p.SiteRow].Name}＠{rg.Name}（{NameOf(w)}・技術 {w.TechLevel:F2}）={st.Tech:F0}/{st.Need:F0}｜人{sp3[0]:G3}・値段{sp3[1]:G3}÷費用{sp3[2]:G3}");
                }
        }
        Console.WriteLine("    産地（まだ）: " + (parts.Count > 0 ? string.Join("  ", parts) : "—"));
    }
    Console.WriteLine("## 1. 開幕の既知（助走の終わり）＝主要国の蒸気機関と甜菜糖の3項（解禁済みでも項を見る）・動力（労働力の財）と砂糖の平滑化した値段÷参照価格（(159) の必要性の入力）");
    {
        var view0 = msDp.Nations[0].World.LastDiscoveryView;
        int rSteam = System.Array.FindIndex(rowsDp, x => x.Name == "蒸気機関"), rBeet = System.Array.FindIndex(rowsDp, x => x.Name == "甜菜糖");
        foreach (var dsim0 in msDp.Nations)
        {
            var w0 = dsim0.World;
            var t0 = Discovery.Evaluate(w0, view0, cDp, includeKnown: true);
            double Sm0(int g) => w0.DiscoveryPrice is { } dp0 && g < dp0.Length ? dp0[g] / Catalog.RefPrices[g] : double.NaN;
            // ★(165) 蒸気機関の行の強度の入力＝動力の平滑化した値段・蒸気の動力の費用（自己費用×自己費用の物価水準÷物価水準＋蒸気機関と石炭の平滑化した値段×量）
            double Sx0(int g) => w0.DiscoveryPrice is { } dpx0 && g < dpx0.Length ? dpx0[g] : double.NaN;
            double sc0 = w0.CostPriceLevel / System.Math.Max(1e-12, w0.NominalOf(1.0));
            var ro0 = new (double Replaced, double Cost)[rowsDp.Length];
            Discovery.Evaluate(w0, view0, cDp, includeKnown: true, ratioOut: ro0);
            double steamCost0 = ro0[rSteam].Cost;
            if (System.Environment.GetEnvironmentVariable("POB_DP_RATIO") == "1" && NameOf(w0) == "イギリス")
            {
                // 計器だけ（(165) の調べ）＝全ての値段が参照価格の世界の比（代替する物の値段×換算÷新しいレシピの費用・自己費用の物価は英の値）＝レシピのデータが「安くなる発明」を写しているか
                var refArr = new double[Catalog.Goods.Length + 1];
                for (int g = 0; g < Catalog.Goods.Length; g++) refArr[g] = Catalog.RefPrices[g];
                refArr[Catalog.Goods.Length] = 1.0;
                var roRef = new (double Replaced, double Cost)[rowsDp.Length];
                Discovery.Evaluate(w0, new Discovery.WorldView(null, null, Discovery.WorldMarket.Flat(refArr)), cDp, includeKnown: true, realPrice: refArr, ratioOut: roRef);
                Console.WriteLine("      （計器）参照価格の世界の比（代替する物の値段×換算÷新しいレシピの費用）: " + string.Join("　", Enumerable.Range(0, rowsDp.Length).Where(r => rowsDp[r].Field is DiscoveryField.Process or DiscoveryField.Product)
                    .Select(r => $"{rowsDp[r].Name} {(roRef[r].Cost > 0 ? roRef[r].Replaced / roRef[r].Cost : double.NaN):F2}")));
            }
            Console.WriteLine($"    （平滑化した値段÷参照）機械 {Sm0(Catalog.MachineryId):F2}・鋼 {Sm0(Catalog.SteelId):F2}・銅 {Sm0(Catalog.CopperId):F2}・鉄 {Sm0(Catalog.IronId):F2}・高度労働力 {Sm0(Catalog.AdvLaborId):F2}・石炭 {Sm0(Catalog.CoalId):F2}・蒸気機関の財 {Sm0(Catalog.SteamEngineId):F2}　世界の蒸気機関の財 {(view0?.Market?.Average is double[] wp0 ? wp0[Catalog.SteamEngineId] / Catalog.RefPrices[Catalog.SteamEngineId] : double.NaN):F2}・機械 {(view0?.Market?.Average is double[] wp1 ? wp1[Catalog.MachineryId] / Catalog.RefPrices[Catalog.MachineryId] : double.NaN):F2}");
            Console.WriteLine($"  {NameOf(w0),-12} 技術 {w0.TechLevel:F2}　動力 {Sm0(Catalog.LaborId):F3}・砂糖 {Sm0(Catalog.SugarId):F3}　動力の値段 {Sx0(Catalog.LaborId):F3}÷蒸気の動力の費用 {steamCost0:F3}（蒸気機関の財 {Sx0(Catalog.SteamEngineId):F3}・石炭 {Sx0(Catalog.CoalId):F3}・自己費用の物価 {sc0:F3}）　蒸気機関 {t0[rSteam].Tech:F0}/{t0[rSteam].Need:F0}/{t0[rSteam].Transmit:F0}{(w0.Discovered[rSteam] ? "＝済" : "")}　甜菜糖 {t0[rBeet].Tech:F0}/{t0[rBeet].Need:F0}/{t0[rBeet].Transmit:F0}");
        }
    }
    foreach (var dsim in msDp.Nations)
    {
        var k = dsim.World.Discovered;
        var names = new List<string>();
        for (int r = 0; r < rowsDp.Length; r++) if (k[r]) names.Add(rowsDp[r].Name);
        if (names.Count > 0) Console.WriteLine($"- {NameOf(dsim.World)}: {string.Join("・", names)}");
    }
    var lastTerms = new Dictionary<World, Discovery.Terms[]>();
    long evTicks = 0, evCount = 0;   // 発見の判定の1回の時間（計算量の台帳の単価 CostLedger.DiscoveryUnitMicros の実測）
    var firstBy = new (string Nation, int Year)?[rowsDp.Length];
    var yearOf = new Dictionary<(string, int), int>();
    foreach (var dsim in msDp.Nations)
        for (int r = 0; r < rowsDp.Length; r++)
            if (dsim.World.Discovered[r]) { firstBy[r] ??= (NameOf(dsim.World), dsim.World.Year); yearOf[(NameOf(dsim.World), r)] = dsim.World.Year; }
    // 製法の行の割高さの参照＝既定のレシピの投入の費用（参照価格）と財の参照価格の比（旧の参照＝財の参照価格は工場の自己費用のぶん割高さを負へ偏らせた）
    {
        var w0 = msDp.Nations[0].World; var rp0 = Catalog.RefPrices;
        var parts0 = new List<string>();
        foreach (var d0 in rowsDp)
        {
            if (d0.Field != DiscoveryField.Process || d0.Good == Catalog.LaborId) continue;
            var dp0 = w0.Regions.SelectMany(rg => rg.Producers).FirstOrDefault(p => p.GoodId == d0.Good && p.RecipeName.Length == 0);
            if (dp0 is null) { parts0.Add($"{d0.Name}=既定なし"); continue; }
            double sr0 = 0; foreach (var inp in dp0.Inputs) if (inp.GoodId >= 0 && inp.GoodId < rp0.Length) sr0 += inp.QtyPerOutput * rp0[inp.GoodId];
            parts0.Add($"{d0.Name}={sr0:F3}/{rp0[d0.Good]:F3}（{sr0 / rp0[d0.Good] - 1:P1}）");
        }
        Console.WriteLine("## 1b. 製法の割高さの参照（既定のレシピの投入の費用／財の参照価格〔旧の参照〕・比−1＝旧の偏り）: " + string.Join("　", parts0));
    }
    Console.WriteLine("## 2. 発見の出来事（年 国 行 ＜直前の四半期の 技術力/必要性/伝わり＞）");
    var prevKnown = new Dictionary<World, bool[]>();
    foreach (var dsim in msDp.Nations) prevKnown[dsim.World] = (bool[])dsim.World.Discovered.Clone();
    for (int q = 0; q < dpQ; q++)
    {
        msDp.StepQuarter();
        SpikeRing();
        SiteEvents();
        foreach (var dsim in msDp.Nations)
        {
            var w = dsim.World;
            if (!prevKnown.TryGetValue(w, out var pk)) { pk = (bool[])w.Discovered.Clone(); prevKnown[w] = pk; }   // 建国した国＝親から継いだ知識は出来事にしない
            lastTerms.TryGetValue(w, out var lt);
            for (int r = 0; r < rowsDp.Length; r++)
                if (w.Discovered[r] && !pk[r])
                {
                    string tt = lt is not null ? $"{lt[r].Tech:F0}/{lt[r].Need:F0}/{lt[r].Transmit:F0}" : "—";
                    Console.WriteLine($"{w.Year} {NameOf(w)} {rowsDp[r].Name} ＜{tt}＞");
                    if (rowsDp[r].Field is DiscoveryField.Process or DiscoveryField.Product && spMean.TryGetValue(w, out var mref))
                    {
                        var tg = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true);
                        var tr = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true, realPrice: mref);
                        var ti = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true, realPrice: spInst[w]);
                        SpikeCount(rowsDp[r].Name, tg[r].Score, tr[r].Score, ti[r].Score);
                    }
                    firstBy[r] ??= (NameOf(w), w.Year);
                    yearOf.TryAdd((NameOf(w), r), w.Year);
                    pk[r] = true;
                }
            long tEv = System.Diagnostics.Stopwatch.GetTimestamp();
            lastTerms[w] = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef);
            evTicks += System.Diagnostics.Stopwatch.GetTimestamp() - tEv; evCount++;
        }
        if ((q + 1) % dpEvery != 0) continue;
        Console.WriteLine($"### {msDp.Nations[0].World.Year}年の断面（国: 行=技術水準÷難易度×押し｜必要性の強度｜接触｜点数）");
        SiteSection();
        foreach (var nm in majors)
        {
            var dsim = msDp.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (dsim is null) continue;
            var w = dsim.World;
            var tm = lastTerms[w];
            var parts = new List<string>();
            var roN = new (double Replaced, double Cost)[rowsDp.Length];   // ★(165) 代替する物の値段÷新しいレシピで作る費用
            Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, ratioOut: roN);
            foreach (var kr in keyRows)
            {
                int r = System.Array.FindIndex(rowsDp, x => x.Name == kr);
                if (r < 0) continue;
                if (w.Discovered[r]) { parts.Add($"{kr}=済"); continue; }
                double diff = Discovery.Difficulty(r, cDp);
                double ratio = diff > 0 ? System.Math.Min(1, w.TechLevel / diff) : 1;
                double push = ratio > 1e-9 ? tm[r].Tech / (Discovery.TermMax * ratio) : 0;
                double half = Discovery.NeedHalf(rowsDp[r].Field, cDp);
                double s = tm[r].Need < Discovery.TermMax - 1e-9 ? half * tm[r].Need / (Discovery.TermMax - tm[r].Need) : double.PositiveInfinity;
                double x = tm[r].Transmit < Discovery.TermMax - 1e-9 ? cDp.DiscoveryContactHalf * tm[r].Transmit / (Discovery.TermMax - tm[r].Transmit) : double.PositiveInfinity;
                // 製法の行＝その財の国の需要（必要性0 が「需要が無い」か「割高さが0以下」かを分ける）
                string demTxt = "";
                if (rowsDp[r].Field == DiscoveryField.Process)
                {
                    double dd = 0; int g0 = rowsDp[r].Good;
                    foreach (var rg in w.Regions) if (rg.State == RegionState.Active && g0 < rg.Markets.Length) dd += rg.Markets[g0].LastDemand;
                    demTxt = $"｜需要{dd:G3}";
                }
                string ratioTxt = rowsDp[r].Field is DiscoveryField.Process or DiscoveryField.Product ? $"｜比{roN[r].Replaced:G3}÷{roN[r].Cost:G3}" : "";
                parts.Add($"{kr}={ratio:F2}×{push:F2}｜{s:G3}｜{x:G3}｜{tm[r].Score:F0}{demTxt}{ratioTxt}");
            }
            Console.WriteLine($"- {nm}（技術 {w.TechLevel:F2}）: {string.Join("  ", parts)}");
            if (System.Environment.GetEnvironmentVariable("POB_DP_RATIO") == "1" && nm == "イギリス")
            {
                // 計器だけ（(165) の調べ）＝物を生む行の比（代替する物の値段×換算÷新しいレシピの費用）と、その行の代表のレシピの投入の平滑化した値段÷参照
                double SmD(int g) => w.DiscoveryPrice is { } dpq && g < dpq.Length ? dpq[g] / Catalog.RefPrices[g] : double.NaN;
                var dbgParts = new List<string>();
                for (int r = 0; r < rowsDp.Length; r++)
                {
                    if (rowsDp[r].Field is not (DiscoveryField.Process or DiscoveryField.Product)) continue;
                    var rp = w.Regions.SelectMany(rg => rg.Producers).FirstOrDefault(pp => pp.DiscoveryRow == r);
                    string ins = rp is null ? "—" : string.Join(",", rp.Inputs.Select(i => $"{Catalog.Goods[i.GoodId].Name}{SmD(i.GoodId):F1}"));
                    dbgParts.Add($"{rowsDp[r].Name}{(w.Discovered[r] ? "＝済" : "")} {roN[r].Replaced:G3}÷{roN[r].Cost:G3}［{ins}］");
                }
                Console.WriteLine("    （計器）比（代替する物の値段×換算÷新しいレシピの費用［投入の平滑化した値段÷参照］）: " + string.Join("  ", dbgParts));
            }
            // 動力の物差し（(159)＝蒸気機関・電動機の必要性の入力）＝労働力（動力）・石炭・蒸気機関・電力の国の値段（需要で重み付け）÷物価水準÷参照価格（瞬間）と平滑化した値段（判定が読む値）・陸の輸送の通した割合
            double Px(int g) { double pd = 0, dd = 0; foreach (var rg in w.Regions) { if (rg.State != RegionState.Active || g >= rg.Markets.Length) continue; pd += rg.Markets[g].Price * rg.Markets[g].LastDemand; dd += rg.Markets[g].LastDemand; } return dd > 1e-12 ? pd / dd / w.NominalOf(Catalog.RefPrices[g]) : double.NaN; }
            double Sm(int g) => w.DiscoveryPrice is { } dpw && g < dpw.Length ? dpw[g] / Catalog.RefPrices[g] : double.NaN;
            double landSm = w.DiscoveryPrice is { } dpl && Catalog.Goods.Length < dpl.Length ? dpl[Catalog.Goods.Length] : double.NaN;
            double We(int cls) { double e = 0, z = 0; foreach (var rg in w.Regions) { if (rg.State != RegionState.Active) continue; var pp = rg.PopOf(cls); if (pp is null || !(pp.Size > 0)) continue; e += pp.Earned; z += pp.Size; } return z > 1e-12 ? e / z : double.NaN; }
            double coalNom = 0; { double pd = 0, dd = 0; foreach (var rg in w.Regions) { if (rg.State != RegionState.Active) continue; pd += rg.Markets[Catalog.CoalId].Price * rg.Markets[Catalog.CoalId].LastDemand; dd += rg.Markets[Catalog.CoalId].LastDemand; } coalNom = dd > 1e-12 ? pd / dd : double.NaN; }
            Console.WriteLine($"    動力の物差し（値段÷参照×物価・瞬間／平滑）: 労働力 {Px(Catalog.LaborId):F2}/{Sm(Catalog.LaborId):F2}・石炭 {Px(Catalog.CoalId):F2}/{Sm(Catalog.CoalId):F2}・蒸気機関 {Px(Catalog.SteamEngineId):F2}/{Sm(Catalog.SteamEngineId):F2}・電力 {Px(Catalog.ElectricityId):F2}/{Sm(Catalog.ElectricityId):F2}　陸の輸送の値段（平滑・参照＝1） {landSm:F2}　1人の月の所得÷物価: 工場労働者 {We(PopClass.Worker) / w.NominalOf(1):F3}・農民 {We(PopClass.Peasant) / w.NominalOf(1):F3}　÷石炭の値段: 工場労働者 {We(PopClass.Worker) / coalNom:F3}・農民 {We(PopClass.Peasant) / coalNom:F3}　1人GDP {w.Gdp / System.Math.Max(1e-9, w.TotalPopulation):F4}");
        }
    }
    Console.WriteLine("## 4. 行ごとの最初の国と年・主要国の年");
    for (int r = 0; r < rowsDp.Length; r++)
    {
        var f = firstBy[r];
        var per = majors.Select(nm => yearOf.TryGetValue((nm, r), out int y) ? $"{nm}{y}" : $"{nm}—");
        Console.WriteLine($"- {rowsDp[r].Name}（{rowsDp[r].Decade}年代）: 最初 {(f is { } ff ? $"{ff.Nation} {ff.Year}" : "—")}　{string.Join(" ", per)}");
    }
    int total = 0; foreach (var dsim in msDp.Nations) foreach (bool b in dsim.World.Discovered) if (b) total++;
    Console.WriteLine($"終局の解禁の総数（国×行）: {total}／{msDp.Nations.Count * rowsDp.Length}");
    Console.WriteLine($"## 5. 値段の跳ねで決まった解禁（製法・新製品の行と産地の発見の出来事・(150) Q12・(159)）: 出来事 {spEv}・判定の値段（平滑）で100以上 {spGate}・そのうち持続する値段（瞬間の実質の値段の直近{spN}四半期の平均＝判定の平滑と同じ遅れ）では100未満 {spHit}"
        + $"／参考＝瞬間の値段で100以上 {spInstGate}・そのうち持続する値段では100未満 {spInstHit}");
    Console.WriteLine("  行ごと（出来事／平滑の跳ね／瞬間の跳ね）: " + string.Join("　", spRows.Select(kv => $"{kv.Key} {kv.Value.Ev}/{kv.Value.Hit}/{kv.Value.InstHit}")));
    Console.WriteLine($"発見の判定（Discovery.Evaluate）の1回＝{evTicks * 1e6 / System.Diagnostics.Stopwatch.Frequency / System.Math.Max(1, evCount):F1} マイクロ秒（{evCount} 回・計算量の台帳の単価 {CostLedger.DiscoveryUnitMicros}）");
    return;
}

// === 02断線走査 §3恒久対策（2026-07-28）：型F検出器＝参照価格の順位・並存レシピの単位投入費・解禁順の警告： dotnet run -- refaudit ===
// ⚠改名2026-09-08：同名の refprice（参照価格の一覧・:834）が先に return するため、2026-09-05 以降この検出器は一度も走っていなかった。
// portledger が政治の二重計上を可視化したのと同じ役割を財の側で果たす（電力・缶詰・グアノ・水力・肥料の5件はこの1回の実行で全部出る）。
if (args.Length > 0 && args[0] == "refaudit")
{
    var goods = Catalog.Goods; var rp = Catalog.RefPrices;
    Console.WriteLine("# refaudit 診断（型F検出器・02断線走査 §3）\n");
    Console.WriteLine("## 1. RGOブースター候補群の参照価格順位（EconomyStep は最安1つだけを選ぶ＝2位以下は解禁済みでも選ばれない）\n");
    var seenSets = new HashSet<string>();
    for (int id = 0; id < goods.Length; id++)
    {
        var b = Catalog.RgoBoosterGoods(id);
        if (b.Length == 0) continue;
        string key = string.Join(",", b);
        if (!seenSets.Add(key)) continue;
        var ranked = b.OrderBy(x => rp[x]).ToArray();
        Console.WriteLine("- 候補群[" + string.Join("・", b.Select(x => goods[x].Name)) + "] 順位: "
            + string.Join(" < ", ranked.Select(x => $"{goods[x].Name}({rp[x]:F2}・{DecadeText(x, "")})")));
    }
    Console.WriteLine("\n## 2. 並存レシピの単位投入費（Σ投入比×参照価格・安い方が市場で駆逐する）と発見の年代（段2A＝発見の表の行・無い＝地理差）\n");
    var rsim = new MultiNationSim([NationConfig.Britain()], seed: 1);
    var rw = rsim.Nations[0].World;
    var byGood = new Dictionary<int, List<Producer>>();
    foreach (var pr in rw.Regions[0].Producers)
    {
        if (!byGood.TryGetValue(pr.GoodId, out var l)) byGood[pr.GoodId] = l = new List<Producer>();
        l.Add(pr);
    }
    foreach (var kv in byGood.OrderBy(k => k.Key))
    {
        if (kv.Value.Count < 2) continue;   // 並存レシピ（同一財に複数生産者）のみ
        Console.WriteLine($"### {goods[kv.Key].Name}({kv.Key})　既定 {DecadeText(kv.Key, "")}");
        foreach (var pr in kv.Value)
        {
            double unitCost = 0; foreach (var inp in pr.Inputs) unitCost += inp.QtyPerOutput * rp[inp.GoodId];
            Console.WriteLine($"  - {(string.IsNullOrEmpty(pr.RecipeName) ? "(既定)" : pr.RecipeName),-10} {DecadeText(pr.GoodId, pr.RecipeName)}　単位投入費 {unitCost:F2}");
        }
    }
    Console.WriteLine("\n## 3. 発見の表（段2A）＝行・分野・年代・難易度（Y＝DiscoveryTechPerDecade）\n");
    var cfa = new Coefficients();
    for (int row = 0; row < Catalog.Discoveries.Length; row++)
    {
        var d = Catalog.Discoveries[row];
        string gname = d.Good >= 0 ? $"{goods[d.Good].Name}・{(d.Recipe == "" ? "既定" : d.Recipe)}" : "財なし";   // 制度と思想の行（財を持たない・(152)）
        Console.WriteLine($"- {d.Name}（{gname}）{d.Field}{(d.Domain != WarDomain.None ? "・" + d.Domain : "")}　{d.Decade}年代　難易度 {Discovery.Difficulty(row, cfa):F2}");
    }
    return;
    static string DecadeText(int g, string recipe) => Catalog.DiscoveryRowOf(g, recipe) is int row and >= 0 ? $"{Catalog.Discoveries[row].Decade}年代" : "地理差";
}

// === セーブ/ロードの往復検証： dotnet run -- savetest [seed] [事前四半期] [事後四半期] ===
// 事前四半期ぶん進めて保存→さらに事後四半期進めた「継続系列」と、ロード→事後四半期の「復元系列」を突き合わせる。
// 復元系列は自己完結の決定論（同じセーブ→同じ未来）。継続系列との厳密ビット一致は Dictionary の列挙順（削除履歴）に依存しうるため、相対差で判定する。
if (args.Length > 0 && args[0] == "savetest")
{
    // 第8段8-5（2026-07-28）：往復地点を序盤60Q→長期300Q（1858年）へ＝**マルコフ違反の自動検出**（保存されない隠れ状態・
    // 静的キャッシュへの依存は長期ほど蓄積して露見する）。
    // ⚠★2026-09-03（キューA-06）：**batch_final というスクリプトはリポジトリに存在しない**（実測＝全域grepで0件）。
    //   実際の受け皿は2段＝(1) 短距離版を xUnit に常設（SaveRoundTripTests・毎回のフルスイートで回る）
    //   (2) 長距離300Qのこのコマンドを受け入れのたび手で走らせる（運転規則の受け入れバッテリー）。
    ulong seed = args.Length > 1 && ulong.TryParse(args[1], out var s1) ? s1 : 1789UL;
    int preQ = args.Length > 2 && int.TryParse(args[2], out var q1) ? q1 : 300;
    int postQ = args.Length > 3 && int.TryParse(args[3], out var q2) ? q2 : 12;
    var sim1 = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent());
    sim1.StartWarmup();   // ★2026-08-10：実ゲームと同じ開始盤面から往復する（App も自己対戦バッチも助走を通る）＝助走の凍結フラグがセーブに正しく載るかも同時に見る
    for (int q = 0; q < preQ; q++) sim1.StepQuarter();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    using var ms = new System.IO.MemoryStream();
    SaveGame.Save(sim1, ms);
    long saveMs = sw.ElapsedMilliseconds;
    ms.Position = 0;
    sw.Restart();
    var sim2 = SaveGame.Load(ms);
    long loadMs = sw.ElapsedMilliseconds;
    System.Console.WriteLine($"保存 {ms.Length / 1024.0 / 1024.0:F1}MB（{saveMs}ms）／読込 {loadMs}ms（{preQ}四半期時点）");
    for (int q = 0; q < postQ; q++) { sim1.StepQuarter(); sim2.StepQuarter(); }
    int diffN = 0;
    double worst = 0;
    for (int i = 0; i < sim1.Nations.Count; i++)
    {
        var a = sim1.Nations[i].World; var b = sim2.Nations[i].World;
        double ra = System.Math.Abs(a.BaseMoney - b.BaseMoney) / System.Math.Max(1.0, System.Math.Abs(a.BaseMoney));
        double rp = System.Math.Abs(a.TotalPopulation - b.TotalPopulation) / System.Math.Max(1.0, a.TotalPopulation);
        double r = System.Math.Max(ra, rp);
        if (r > worst) worst = r;
        if (r > 1e-9)
        {
            diffN++;
            if (diffN <= 5) System.Console.WriteLine($"  乖離 {a.SelfPower?.Name}: 基礎貨幣 {a.BaseMoney:F2}⇔{b.BaseMoney:F2}　人口 {a.TotalPopulation:F0}⇔{b.TotalPopulation:F0}");
        }
    }
    System.Console.WriteLine(diffN == 0
        ? $"savetest OK：{postQ}四半期後も全{sim1.Nations.Count}カ国が継続系列と一致（最大相対差 {worst:E1}）"
        : $"savetest 乖離 {diffN}/{sim1.Nations.Count}カ国（最大相対差 {worst:E1}）＝Dictionary列挙順の揺れなら微小・桁が大きければバグ");
    return;
}

// === 政治アブレーション（政治アーキテクチャ整理2026-07-24 §4-4）： dotnet run -- politics-ablation [seed] ===
// 政治系の主フラグを1本ずつ0にして1試合を回し、総政変数（政体フリップのエッジ数）の感度表を出す（9本×約5.5分≈50分）。
// 「どのノブがどれだけ効いているか」の定量＝調整の起点（docs/政治ポート台帳.md とセットで使う）。
// 実装註：係数はシム構築後に全国の World.Coef インスタンス（重複排除）へ書き、走行後に必ず復元＝共有キャッシュ汚染なし。
if (args.Length > 0 && args[0] == "politics-ablation")
{
    ulong paSeed = args.Length > 1 && ulong.TryParse(args[1], out var pas) ? pas : 1UL;
    string[] paFlags = [
        "RegimeTrackRecordWeight",
        "DecayReformResistWeight", "DecayTechAdoptWeight", "DecayMilitaryWeight",
        "ReformAnticipationWeight", "WarCoercionDrain",   // PushedLawGrievance は 2026-09-20 に係数ごと削除（段の点火）・DynasticMandateEnabled／RedefinedMandateEnabled／ReformFromAboveEnabled は 2026-09-26 に旗ごと焼き込み（x-01）
    ];
    int PaRun(string? flag)
    {
        var sim = new MultiNationSim(WorldData.Nations(), paSeed, _ => new UtilityAgent());
        var saved = new List<(Coefficients c, double v)>();
        System.Reflection.FieldInfo? fi = null;
        if (flag is not null)
        {
            fi = typeof(Coefficients).GetField(flag) ?? throw new System.InvalidOperationException($"係数 {flag} が見つからない");
            foreach (var n0 in sim.Nations)
            {
                var co = n0.World.Coef;
                if (co is null || saved.Exists(x => ReferenceEquals(x.c, co))) continue;
                saved.Add((co, (double)fi.GetValue(co)!));
                fi.SetValue(co, 0.0);
            }
        }
        var prevP = sim.Nations.Select(n0 => n0.World.Polity).ToArray();
        int coups = 0;
        for (int q = 0; q < Coefficients.GameQuarters; q++)
        {
            sim.StepQuarter();
            for (int i = 0; i < prevP.Length; i++)
                if (sim.Nations[i].World.Polity != prevP[i]) { coups++; prevP[i] = sim.Nations[i].World.Polity; }
        }
        if (fi is not null) foreach (var (co, v) in saved) fi.SetValue(co, v);   // 復元（共有キャッシュ対策）
        return coups;   // 建国スポーンの新国家は開始時集合の外＝数えない（全走行で同一規約＝比較は公平）
    }
    int paBase = PaRun(null);
    System.Console.WriteLine($"政治アブレーション（seed{paSeed}・開始{WorldData.Nations().Length}カ国・各主フラグ=0での総政変）");
    System.Console.WriteLine($"  基準（現行値）: 総政変{paBase}回");
    foreach (var f in paFlags)
    {
        int nCoups = PaRun(f);
        System.Console.WriteLine($"  {f}=0: 総政変{nCoups}回（Δ{nCoups - paBase:+0;-0;0}）");
    }
    return;
}

// === 完全な1ゲームの年代記（日付つきイベント＋10年ごと順位）： dotnet run -- chronicle [seed] ===
// WorldData の全ロスター×Coefficients.GameQuarters（1783年起点）を1試合プレイし、順位変動・政変・法律変更・戦争・恐慌を yyyymmdd で記録する。
// 日付はUI(GameViewModel)と同一規約：四半期境界＝各年の 01/04/07/10 月 1日。恐慌の定義もUIと同じ World.GdpPcDrawdown（1人あたり・閾値は SelfPlayMatch の1組＝>20%入り/<8%脱出/>25%大恐慌）。★2026-09-26 是正（fis-24）：旧は総額の World.GdpDrawdown を読みながら「UIと同じ」と書いていた
if (args.Length > 0 && args[0] == "chronicle")
{
    ulong seed = args.Length > 1 && ulong.TryParse(args[1], out var s) ? s : 1789UL;
    NationConfig[] cc = WorldData.Nations();
    var m = new MultiNationSim(cc, seed, _ => new UtilityAgent());
    // --zero A,B,… ＝指定係数を全国で0に（1項目1バッチの項目別帰属用。politics-ablationと同じ重複排除書き）
    int zi = System.Array.IndexOf(args, "--zero");
    if (zi >= 0 && zi + 1 < args.Length)
        foreach (var flag in args[zi + 1].Split(','))
        {
            var fi = typeof(Coefficients).GetField(flag) ?? throw new System.InvalidOperationException($"係数 {flag} が見つからない");
            var done = new List<Coefficients>();
            foreach (var n0 in m.Nations)
            {
                var co = n0.World.Coef;
                if (co is null || done.Exists(x => ReferenceEquals(x, co))) continue;
                done.Add(co); fi.SetValue(co, 0.0);
            }
        }
    m.StartWarmup();   // ★2026-09-07：selfplay-batch/popprobe と同じ助走を通す＝「年代記の世界」を帯の世界と一致させる（旧＝助走なし＝英の領土崩壊が年代記に出ず、popprobe（州31→1）と食い違った＝計器の構築時バグ）（--zero の後＝ゼロ化した係数で助走する）
    int N = m.Nations.Count;
    int Q = Coefficients.GameQuarters;   // 全長＝668四半期＝167年（1783→1950・1950延長2026-09-21）

    string SufJa(Suffrage x) => x switch { Suffrage.None => "参政権なし", Suffrage.Census => "制限選挙", Suffrage.AdultMale => "男子普通選挙", Suffrage.Universal => "普通選挙", _ => x.ToString() };
    string CensJa(CensorshipLaw x) => x switch { CensorshipLaw.StatePressOnly => "国家統制報道", CensorshipLaw.Censorship => "検閲", CensorshipLaw.FreePress => "自由報道", _ => x.ToString() };
    string EduJa(EducationLaw x) => x switch { EducationLaw.Religious => "宗教教育", EducationLaw.Public => "公教育", EducationLaw.Compulsory => "義務教育", _ => x.ToString() };
    string UnionJa(LaborUnionLaw x) => x switch { LaborUnionLaw.Illegal => "組合非合法", LaborUnionLaw.Tolerated => "組合容認", _ => x.ToString() };
    string WhJa(WorkHours x) => x switch { WorkHours.Eight => "8時間労働", WorkHours.Ten => "10時間労働", WorkHours.Twelve => "12時間労働", WorkHours.Unlimited => "労働時間無制限", _ => x.ToString() };
    string RefJa(ReformLevel x) => x switch { ReformLevel.None => "なし", ReformLevel.Limited => "限定", ReformLevel.Full => "全面", _ => x.ToString() };
    string WomenJa(WomenRightsLaw x) => x switch { WomenRightsLaw.None => "なし", WomenRightsLaw.Property => "財産権", WomenRightsLaw.Education => "教育権", WomenRightsLaw.Suffrage => "選挙権", _ => x.ToString() };   // ★統合法（2026-08-10）
    // 四半期c（1..Q）完了時点のUI日付：t=90c日 → 1783+t/360年, 月=01/04/07/10。c=0は開始(1783/01/01)。
    string Date(int c) { int year = 1783 + c / 4; int mm = c % 4; int month = mm == 0 ? 1 : mm == 1 ? 4 : mm == 2 ? 7 : 10; return $"{year:D4}{month:D2}01"; }
    string Name(int i) => m.Nations[i].World.SelfPower?.Name ?? m.Nations[i].World.Region.Name;

    var events = new List<(int c, string date, string nation, string cat, string text)>();
    void Ev(int c, int i, string cat, string text) => events.Add((c, Date(c), i >= 0 ? Name(i) : "—", cat, text));

    // 前回値（エッジ検出）
    var pPol = m.Nations.Select(n => n.World.Polity).ToArray();
    var pDef = new bool[N];
    // ★計器2026-09-08：戦争経路の内戦・分離戦を**戦争オブジェクト単位**で数える（ラッチboolの置き換え）＋国の出入りを建国/消滅で別に数える。
    var seenCivilWars = new HashSet<War>();
    int civilWarCount = 0, secWarCount = 0, birthCount = 0, deathCount = 0, maxRebelNations = 0;
    var foundingRegionCounts = new List<int>();
    var aliveNations = new HashSet<Simulation>(m.Nations);
    var rebelNations = new HashSet<Simulation>();
    var pSuf = m.Nations.Select(n => n.World.Suffrage).ToArray();
    var pCens = m.Nations.Select(n => n.World.Censorship).ToArray();
    var pWH = m.Nations.Select(n => n.World.WorkHours).ToArray();
    var pEdu = m.Nations.Select(n => n.World.EducationLaw).ToArray();
    var pUnion = m.Nations.Select(n => n.World.LaborUnion).ToArray();
    var pCorn = m.Nations.Select(n => n.World.CornLaws).ToArray();
    var pSerf = m.Nations.Select(n => n.World.SerfEmancipation).ToArray();
    var pLand = m.Nations.Select(n => n.World.LandReform).ToArray();
    var pWomen = m.Nations.Select(n => n.World.WomenRights).ToArray();   // ★女性の権利の梯子（統合法・2026-08-10）＝旧・財産権/参政権の2系列を1本へ
    var pConsc = m.Nations.Select(n => n.World.ConscriptionLaw).ToArray();   // ★2026-08-18：親軍法の撤去＝年表の軍事立法は徴兵法のエッジで追う
    var pGold = m.Nations.Select(n => n.World.OnGoldStandard).ToArray();
    // 恐慌エピソード（UIと同じ World.GdpPcDrawdown：>20%入り/<8%脱出・助走12Q以降）
    var inDep = new bool[N]; var depEnterC = new int[N]; var depDepth = new double[N]; var depTroughC = new int[N];
    var depCount = new int[N]; var depMaxDepth = new double[N]; var polChanges = new int[N];
    int greatDepTotal = 0, depTotal = 0;
    var pFamine = new bool[N]; var pHeat = new bool[N]; var pAust = new bool[N];
    var pDeval = m.Nations.Select(n => n.World.CumulativeDevaluation).ToArray();
    var lastDevalC = new int[N]; for (int i = 0; i < N; i++) lastDevalC[i] = -10;   // 通貨切り下げの間引き（スパイラルを年1回に）
    int prevLog = m.Diplomacy.Log.Count;

    // 順位スナップショット：表示時に最終値を読まないよう、各時点のスコア・政体・工業化を退避する
    var snaps = new List<(int year, List<(int idx, double score, Polity pol, double ind)> rows)>();
    void Snap(int c) { var rows = Enumerable.Range(0, N).Select(i => { var w = m.Nations[i].World; return (idx: i, score: NationalScore.Of(w), pol: w.Polity, ind: w.Region.TotalIndustrialization); }).OrderByDescending(r => r.score).ToList(); snaps.Add((1783 + c / 4, rows)); }
    Snap(0);

    void CloseDep(int i, int c, bool ongoing)
    {
        bool great = depDepth[i] > SelfPlayMatch.GreatDepressionDrawdown;
        if (great) greatDepTotal++;
        depTotal++; depCount[i]++; depMaxDepth[i] = Math.Max(depMaxDepth[i], depDepth[i]);
        string span = ongoing ? $"{Date(depTroughC[i])}底・終局まで継続" : $"{Date(depTroughC[i])}底・{Date(c)}回復・{(c - depEnterC[i]) / 4.0:F1}年";
        Ev(depEnterC[i], i, "恐慌", $"{(great ? "大恐慌" : "景気後退")}入り（最大ドローダウン {depDepth[i]:P0}・{span}）");
        inDep[i] = false;
    }

    // 機能5-②-1/④-4（2026-07-28）診断：多正面の分布（同一四半期の交戦戦争数/国）・大戦（同一の敵に3カ国以上が同時交戦）の初回年と回数
    var frontsDist = new int[8];
    int greatWarFirstYear = 0, greatWarQuarters = 0;

    for (int q = 0; q < Q; q++)
    {
        m.StepQuarter();
        int c = q + 1;
        {
            var nowNations = new HashSet<Simulation>(m.Nations);
            foreach (var nn in m.Nations)
                if (aliveNations.Add(nn))
                {
                    birthCount++;
                    foundingRegionCounts.Add(nn.World.Regions.Count);   // ★段7の受け入れ基準＝建国時の州数
                    if (nn.World.SelfPower is { } np)
                        foreach (var war in m.Diplomacy.Wars)
                            if (war.CivilWarParent is not null && war.CivilWarParent != np && (war.A == np || war.B == np)) { rebelNations.Add(nn); break; }
                }
            deathCount += aliveNations.RemoveWhere(x => !nowNations.Contains(x));
            rebelNations.RemoveWhere(x => !nowNations.Contains(x));
            if (rebelNations.Count > maxRebelNations) maxRebelNations = rebelNations.Count;
        }
        {
            var frontsBy = new Dictionary<PriceOfBread.SimCore.Power, int>();
            foreach (var war in m.Diplomacy.Wars)
            {
                frontsBy[war.A] = frontsBy.GetValueOrDefault(war.A) + 1;
                frontsBy[war.B] = frontsBy.GetValueOrDefault(war.B) + 1;
            }
            foreach (var kv in frontsBy) frontsDist[Math.Min(kv.Value, 7)]++;
            bool greatWarNow = frontsBy.Any(kv => kv.Value >= 3);   // 同一の敵に3カ国以上＝その敵のfronts≥3
            if (greatWarNow) { greatWarQuarters++; if (greatWarFirstYear == 0) greatWarFirstYear = 1783 + c / 4; }
        }
        // ⚠計器の是正2026-09-08：分離戦の検知は `w.HadSecession`（**一度立つと二度と落ちないラッチbool**）だったので、
        //   同四半期に13カ国が独立しても1行しか出ず、2度目以降の分離戦は永久に印字されなかった。
        //   戦争オブジェクトの同一性で数える＝**戦争1本につき必ず1行**（同時多発がそのまま並ぶ）。
        foreach (var war in m.Diplomacy.Wars)
        {
            if (war.CivilWarParent is null || !seenCivilWars.Add(war)) continue;
            var rebelP = war.A == war.CivilWarParent ? war.B : war.A;
            int pIdx = m.Nations.FindIndex(nn => nn.World.SelfPower == war.CivilWarParent);
            if (war.CivilWarSecession) { secWarCount++; Ev(c, pIdx, "内戦", $"分離独立戦争が勃発（{rebelP.Name}）"); }
            else { civilWarCount++; Ev(c, pIdx, "内戦", $"内戦が勃発（{rebelP.Name}が地図の上に立った・{war.CivilWarPolity}）"); }
        }
        // 共有外交ログの差分（開戦・講和・独立・運河・疫病・鉱山枯渇・凶作）を日付つきで取り込む
        for (int li = prevLog; li < m.Diplomacy.Log.Count; li++)
        {
            string line = m.Diplomacy.Log[li];
            if (line.Contains("凶作")) continue;   // 飢饉エッジ（per国）で計上するため外交ログ側の凶作は重複として除外
            // ⚠計器の是正2026-09-08：内戦（DeclareCivilWar のログ）は「開戦」も「講和」も含まないので**外交**へ、
            //   分離独立戦争は「独立」を含むので**植民地**へ落ちていた＝年代記の内戦欄が空になる原因のひとつ。内戦の判定を先に置く。
            string cat = line.Contains("内戦") || line.Contains("分離独立戦争") || line.Contains("再統一") || line.Contains("再併合") ? "内戦"
                : line.Contains("開戦") || line.Contains("講和") ? "戦争"
                : line.Contains("独立") ? "植民地" : line.Contains("運河") ? "インフラ"
                : line.Contains("疫病") ? "疫病" : line.Contains("鉱山") ? "資源" : "外交";
            events.Add((c, Date(c), "—", cat, line));
        }
        prevLog = m.Diplomacy.Log.Count;

        for (int i = 0; i < N; i++)
        {
            var w = m.Nations[i].World;
            if (w.Polity != pPol[i]) { Ev(c, i, "政変", $"{PolJa(pPol[i])} → {PolJa(w.Polity)}"); pPol[i] = w.Polity; polChanges[i]++; }
            // （旧経路の内戦〔CivilWarYears＝首都の政変〕の行は 2026-09-28 処理の一元化 段2 束3 で削除＝首都の蜂起も戦争の形＝上の戦争走査が1本で拾う）
            if (w.HadSovereignDefault && !pDef[i]) Ev(c, i, "財政", "国家デフォルト（債務不履行）"); pDef[i] = w.HadSovereignDefault;
            // 法律変更（離散法のみ。関税は連続値のため除外）
            if (w.Suffrage != pSuf[i]) { Ev(c, i, "法律", $"参政権：{SufJa(pSuf[i])} → {SufJa(w.Suffrage)}"); pSuf[i] = w.Suffrage; }
            if (w.Censorship != pCens[i]) { Ev(c, i, "法律", $"報道：{CensJa(pCens[i])} → {CensJa(w.Censorship)}"); pCens[i] = w.Censorship; }
            if (w.WorkHours != pWH[i]) { Ev(c, i, "法律", $"労働時間：{WhJa(pWH[i])} → {WhJa(w.WorkHours)}"); pWH[i] = w.WorkHours; }
            if (w.EducationLaw != pEdu[i]) { Ev(c, i, "法律", $"教育：{EduJa(pEdu[i])} → {EduJa(w.EducationLaw)}"); pEdu[i] = w.EducationLaw; }
            if (w.LaborUnion != pUnion[i]) { Ev(c, i, "法律", $"労働組合：{UnionJa(pUnion[i])} → {UnionJa(w.LaborUnion)}"); pUnion[i] = w.LaborUnion; }
            if (w.CornLaws != pCorn[i]) { Ev(c, i, "法律", $"穀物法：{RefJa(pCorn[i])} → {RefJa(w.CornLaws)}"); pCorn[i] = w.CornLaws; }
            if (w.SerfEmancipation != pSerf[i]) { Ev(c, i, "法律", $"農奴解放：{RefJa(pSerf[i])} → {RefJa(w.SerfEmancipation)}"); pSerf[i] = w.SerfEmancipation; }
            if (w.LandReform != pLand[i]) { Ev(c, i, "法律", $"土地改革：{RefJa(pLand[i])} → {RefJa(w.LandReform)}"); pLand[i] = w.LandReform; }
            if (w.WomenRights != pWomen[i]) { Ev(c, i, "法律", $"女性の権利：{WomenJa(pWomen[i])} → {WomenJa(w.WomenRights)}"); pWomen[i] = w.WomenRights; }
            if (w.ConscriptionLaw != pConsc[i]) { Ev(c, i, "法律", $"徴兵：{pConsc[i]} → {w.ConscriptionLaw}"); pConsc[i] = w.ConscriptionLaw; }
            if (w.OnGoldStandard != pGold[i]) { Ev(c, i, "法律", w.OnGoldStandard ? "金本位制に加入" : "金本位制を離脱（兌換停止）"); pGold[i] = w.OnGoldStandard; }
            // 恐慌（UIと同じ物差しと閾値＝1人あたり・SelfPlayMatch の1組）
            double dd = w.GdpPcDrawdown;
            if (c > 12)
            {
                if (!inDep[i] && dd > SelfPlayMatch.DepressionEnterDrawdown) { inDep[i] = true; depEnterC[i] = c; depDepth[i] = dd; depTroughC[i] = c; }
                else if (inDep[i]) { if (dd > depDepth[i]) { depDepth[i] = dd; depTroughC[i] = c; } if (dd < SelfPlayMatch.DepressionExitDrawdown) CloseDep(i, c, false); }
            }
            bool famine = w.HarvestFactor < 0.80; if (famine && !pFamine[i]) Ev(c, i, "飢饉", $"飢饉（不作 {w.HarvestFactor:P0}）"); pFamine[i] = famine;
            bool heat = w.FinancialHeat > 0.85; if (heat && !pHeat[i]) Ev(c, i, "金融", $"信用過熱（与信 {w.FinancialHeat:P0}）"); pHeat[i] = heat;
            bool aust = w.AusterityIntensity > 0.01;
            if (aust && !pAust[i]) Ev(c, i, "財政", "緊縮財政に突入"); else if (!aust && pAust[i]) Ev(c, i, "財政", "緊縮財政を脱出");
            pAust[i] = aust;
            if (w.CumulativeDevaluation > pDeval[i] + 1) { if (c - lastDevalC[i] >= 4) { Ev(c, i, "財政", "通貨切り下げ（債務不履行）"); lastDevalC[i] = c; } } pDeval[i] = w.CumulativeDevaluation;
        }
        if (c % 40 == 0) Snap(c);
    }
    for (int i = 0; i < N; i++) if (inDep[i]) CloseDep(i, Q, true);   // 終局時に継続中の恐慌を記録
    if (snaps[^1].year != 1783 + Q / 4) Snap(Q);

    // ===== 出力 =====
    Console.WriteLine($"# Price of Bread 完全プレイ年代記　seed {seed}　{N}カ国×{Q / 4}年（1783–{1783 + Q / 4}）\n");

    // 1) 10年ごとの国力ランキング（順位変動つき）
    Console.WriteLine("## 1. 10年ごとの国力ランキング（カッコ内＝前回からの順位変動）");
    var prevRank = new Dictionary<int, int>();
    foreach (var (year, rows) in snaps)
    {
        Console.WriteLine($"\n### {year}年");
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            string mv = prevRank.TryGetValue(row.idx, out var pr) ? (pr == r ? "→" : pr > r ? $"↑{pr - r}" : $"↓{r - pr}") : "—";
            Console.WriteLine($"  {r + 1,2}. {Name(row.idx),-8} スコア{row.score,5:F0}　{PolJa(row.pol),-10}　工業化{row.ind,4:F2}　({mv})");
        }
        prevRank = new Dictionary<int, int>(); for (int r = 0; r < rows.Count; r++) prevRank[rows[r].idx] = r;
    }

    // 2) 全イベント年表
    Console.WriteLine("\n## 2. 年表：重要イベント（yyyymmdd 順）");
    foreach (var e in events.OrderBy(e => e.c).ThenBy(e => e.cat, StringComparer.Ordinal))
        Console.WriteLine(e.nation == "—" ? $"{e.date}  [{e.cat}] {e.text}" : $"{e.date}  [{e.cat}] {e.nation}：{e.text}");

    // 3) 戦争サマリー
    int opens = m.Diplomacy.Log.Count(l => l.Contains("開戦"));
    int decisivePeace = m.Diplomacy.Log.Count(l => l.Contains("講和") && !l.Contains("白紙"));
    int whitePeace = m.Diplomacy.Log.Count(l => l.Contains("白紙講和"));
    Console.WriteLine($"\n## 3. 戦争サマリー　開戦{opens}回／決定的講和{decisivePeace}・白紙講和{whitePeace}・独立{m.Diplomacy.Log.Count(l => l.Contains("独立"))}回");
    int civilWars = m.Nations.Count(n => n.World.HadCivilWar), secs = m.Nations.Count(n => n.World.HadSecession);
    // C-高1完成検証（2026-07-25）：下賜（欽定憲法＝上からの改革でPC床を与えた回数）の実測＝発生国数と総回数
    int grantNations = m.Nations.Count(n => n.World.ReformGrantCount > 0), grantTotal = m.Nations.Sum(n => n.World.ReformGrantCount);
    Console.WriteLine($"内戦を経験：{civilWars}カ国／分離戦：{secs}カ国／国家デフォルト：{m.Nations.Count(n => n.World.HadSovereignDefault)}カ国／下賜（欽定憲法）：{grantNations}カ国・計{grantTotal}回");
    // 02断線走査2026-07-28：F3の当初目的（清1850年代に太平天国様のクラスタが出るか）をバッチ集計に出す＝Diplomacy.Logの目視から解放
    Console.WriteLine($"組織運動の内乱（F3b）：{m.Nations.Sum(n => n.World.FactionRevolts)}回／変異（F3a・千年王国の旗）：{m.Nations.Sum(n => n.World.FactionMutations)}回");
    // ★計器2026-09-08（F3b蜂起doc §5 段1）：件数（国数でなく**事象の数**）・国の出入り・同時に立っている反乱国の最大数・建国時の州数。
    Console.WriteLine($"内戦の勃発（戦争経路）：{civilWarCount}件／分離独立戦争：{secWarCount}件／同時に立っている反乱国の最大数：{maxRebelNations}");
    Console.WriteLine($"国の出入り：建国{birthCount}回・消滅{deathCount}回（初期{N}→終局{m.Nations.Count}）／建国時の州数：" +
        (foundingRegionCounts.Count > 0 ? $"1州 {foundingRegionCounts.Count(x => x == 1)}回・2州以上 {foundingRegionCounts.Count(x => x > 1)}回・最大 {foundingRegionCounts.Max()}州" : "建国なし"));
    Console.WriteLine($"F1 州一揆：鎮圧{m.Diplomacy.Log.Count(l => l.Contains("一揆を鎮圧"))}回・譲歩{m.Diplomacy.Log.Count(l => l.Contains("一揆に譲歩"))}回");
    // 機能5-②-1/④-4（2026-07-28）：多正面の分布（国×四半期の延べ）＋大戦（同一の敵に3カ国以上が同時交戦）の初回年・延べ四半期数
    Console.WriteLine($"多正面分布（戦争数:延べ国四半期）　1:{frontsDist[1]}　2:{frontsDist[2]}　3:{frontsDist[3]}　4+:{frontsDist[4] + frontsDist[5] + frontsDist[6] + frontsDist[7]}");
    Console.WriteLine($"大戦（同一の敵へ3カ国以上）：{(greatWarFirstYear > 0 ? $"初回{greatWarFirstYear}年・延べ{greatWarQuarters}四半期" : "発生なし")}");

    // 4) 恐慌サマリー（国別・回数と深さ）
    Console.WriteLine($"\n## 4. 恐慌サマリー（国別・回数と最大の深さ）　総計{depTotal}回（うち大恐慌{greatDepTotal}回）");
    for (int i = 0; i < N; i++)
        Console.WriteLine($"  {Name(i),-8} 恐慌{depCount[i]}回　最大ドローダウン{depMaxDepth[i],5:P0}　政変{polChanges[i]}回");

    // 5) 最終順位と1783→終局の順位変動
    Console.WriteLine($"\n## 5. 最終順位（{1783 + Q / 4}）と建国（1783）からの順位変動");
    var start = snaps[0].rows; var startRank = new Dictionary<int, int>(); for (int r = 0; r < start.Count; r++) startRank[start[r].idx] = r;
    var final = Enumerable.Range(0, N).OrderByDescending(i => NationalScore.Of(m.Nations[i].World)).ToList();
    for (int r = 0; r < final.Count; r++)
    {
        int i = final[r]; var w = m.Nations[i].World; int sr = startRank[i];
        string mv = sr == r ? "→" : sr > r ? $"↑{sr - r}" : $"↓{r - sr}";
        Console.WriteLine($"  {r + 1,2}. {Name(i),-8} スコア{NationalScore.Of(w),5:F0}　{PolJa(w.Polity),-10}　工業化{w.Region.TotalIndustrialization,4:F2}　人口{w.TotalPopulation / 1000.0,5:F1}M　植民地{w.SelfPower?.Colonies ?? 0,2}　（1783は{sr + 1}位 {mv}）");
    }
    return;
}

// === イギリス内戦の原因診断（各発火時点の経路と内部変数）： dotnet run -- britcw [seed] ===
if (args.Length > 0 && args[0] == "britcw")
{
    ulong bseed = args.Length > 1 && ulong.TryParse(args[1], out var bs) ? bs : 1789UL;
    NationConfig[] bcc = WorldData.Nations();
    var bm = new MultiNationSim(bcc, bseed, _ => new UtilityAgent());
    var bw = bm.Nations[2].World;   // イギリス
    string BDate(int c) { int year = 1783 + c / 4; int mm = c % 4; int month = mm == 0 ? 1 : mm == 1 ? 4 : mm == 2 ? 7 : 10; return $"{year:D4}{month:D2}01"; }
    double BMeanMil(World w) => Politics.AvgMassMilitancy(w);   // ★2026-09-26（sweep-13）：大衆の述語の手書きの写しをやめ、エンジンの1本を呼ぶ（同じ母集団・同じ順）
    bool bwas = bw.InCivilWar; int bsec = 0, brev = 0;
    Console.WriteLine($"=== イギリス内戦の原因診断 seed {bseed} ===");
    Console.WriteLine("日付      経路  challenge  coerce reform radical rad>ref 目標政体       正統性 満足 平均闘争 参政権 関税 自由度 | 首都の蜂起");
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        bm.StepQuarter();
        int c = q + 1;
        bool now = bw.InCivilWar;   // ★2026-09-28：旧経路の内戦（CivilWarYears）を削除＝内戦は全て戦争の形（首都の蜂起も）
        if (now && !bwas)
        {
            double popScale = bw.TotalPopulation / Math.Max(1.0, bw.Coef.PoliticalReferencePop);
            double coerce = bw.Coercion * popScale;
            double reform = bw.ReformistPower, radical = bw.RadicalPower, challenge = reform + radical;
            var cwar = bm.Diplomacy.Wars.Find(x => x.CivilWarParent == bw.SelfPower);
            bool secession = cwar?.CivilWarSecession ?? false;
            if (secession) bsec++; else brev++;
            string capInfo = bw.SecedingRegion is { } held ? $"{held.Name}→遷都 {bw.Regions[0].Name}" : "—";
            Console.WriteLine($"{BDate(c)} {(secession ? "分離戦" : "革命　")} {challenge,9:F0} {coerce,7:F0} {reform,6:F0} {radical,6:F0} {(radical > reform ? "○" : "×"),5} {(cwar?.CivilWarPolity.ToString() ?? "—"),-14} {bw.Legitimacy,5:F2} {bw.MeanSatisfactionPol,4:F2} {BMeanMil(bw),6:F2} {bw.Suffrage,-10} {bw.Tariff,4:F2} {bw.Freedom,4:F2} | {capInfo}");
        }
        bwas = now;
    }
    Console.WriteLine($"\n--- 集計：イギリスの内戦発火 {bsec + brev}回（分離戦 {bsec}・革命 {brev}）---");
    return;
}

// === 較正の経路分布を表示： dotnet run -- calib ===
if (args.Length > 0 && args[0] == "calib")
{
    var cm = Calibration.Evaluate(new Coefficients(), Coefficients.CalibQuarters);   // 較正は524凍結（1934延長2026-07-19＝物差し不変）
    System.Console.WriteLine($"runs={cm.Runs} maxShare={cm.MaxShare:P0} 破綻={cm.Breakages} 都市化形={cm.UrbanizationShapeOk} 自由>専制={cm.FreeBeatsAutocraticOk}");
    foreach (var kv in cm.Distribution) System.Console.WriteLine($"  {kv.Key,-10} {kv.Value}  ({(double)kv.Value / cm.Runs:P0})");
    // 1シナリオを精査（高教育・低強制＝自由化に有利な条件）
    var sc = new Scenario { Seed = 12345, Kind = ScenarioKind.AgrarianToUrban, PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist, EducationLaw = EducationLaw.Religious, EducationSpending = 1.0,
        ExternalPressure = 0.0, BaseCoercion = 150, FiscalEnabled = true, InitialPollTax = 0.05, Coef = new Coefficients() };
    var dsim = new Simulation(sc, new NullAgent());
    dsim.Run(400);
    var dw = dsim.World;
    System.Console.WriteLine($"\n精査(低税0.05/寺子屋/強制150＝FiscalTest): 政体={dw.Polity} 技術={dw.TechLevel:F2} 財源充足={dw.FiscalFundingRatio:P0} ドローダウン={dw.GdpDrawdown:P0} 銀行信頼={(dw.Bank?.Confidence ?? -1):F2} 平均満足={NationalScore.MeanSatisfaction(dw):F2}");
    double maxC = 0; foreach (var p in dw.AllPops()) if (p.Consciousness > maxC) maxC = p.Consciousness;
    System.Console.WriteLine($"最大意識={maxC:F2}");
    var ag = Ideologies.National(dw);
    string[] inm = ["伝統","反動","自由","共和","ナショ","社会","共産","無政府","帝国","ファシズム","王党派"];
    System.Console.WriteLine("国民イデオロギー: " + string.Join(" ", System.Linq.Enumerable.Range(0, Ideologies.Count).Select(k => $"{inm[k]}{ag[k]:F2}")));
    foreach (var pol in Ideologies.Polities) System.Console.WriteLine($"  支持 {pol,-22} {Ideologies.Support(ag, pol, new Coefficients().IdeologyAffinitySlope):F3}");
    return;
}

// === 資本家の降格が発火しているか診断： dotnet run -- captrace ===
if (args.Length > 0 && args[0] == "captrace")
{
    NationConfig[] cc = WorldData.Nations();
    var cm = new MultiNationSim(cc, 1789UL, _ => new UtilityAgent());
    cm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < 500; q++) cm.StepQuarter();
    double upperThr = new Coefficients().UpperFallThreshold;   // 0.8
    Console.WriteLine($"上流転落閾値 = {upperThr} × avgPerCap（これを下回ると四半期 {new Coefficients().UpperFallRate:P0} 降格）");
    foreach (var n in cm.Nations)
    {
        Console.WriteLine($"=== {n.World.SelfPower?.Name} ===");
        foreach (var r in n.World.Regions)
        {
            if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
            double totalSav = 0, total = 0;
            foreach (var p in r.Pops) { total += p.Size; totalSav += System.Math.Max(0, p.Savings); }
            double avg = total > 1e-9 ? totalSav / total : 0;
            var cap = r.Pops[r.InvestorPopIndex];
            double capPc = cap.Size > 1e-9 ? cap.Savings / cap.Size : 0;
            var art = r.ArtisanPopIndex >= 0 ? r.Pops[r.ArtisanPopIndex] : null;
            bool fires = capPc < upperThr * avg;
            Console.WriteLine($"  {r.Name,-14} 資本家 規模{cap.Size,7:F0} 貯蓄/人{capPc,9:F1}  平均/人{avg,7:F1} 閾値{upperThr * avg,7:F1} 降格発火={(fires ? "○" : "×")}  職人規模{(art != null ? art.Size : 0),7:F0}");
        }
    }
    return;
}

// === 完全な結合テスト：world.json の全ロスター×151年(604四半期)を四半期ごとに監視： dotnet run -- integration [seed] ===
if (args.Length > 0 && args[0] == "integration")
{
    ulong seed = args.Length > 1 && ulong.TryParse(args[1], out var s) ? s : 1789UL;
    NationConfig[] ic = WorldData.Nations();
    var im = new MultiNationSim(ic, seed, _ => new UtilityAgent());
    var dip = im.Diplomacy;
    var lastPolity = im.Nations.Select(n => n.World.Polity).ToArray();
    int[] polityChanges = new int[im.Nations.Count];
    var warsSeen = new System.Collections.Generic.HashSet<War>();
    int cessions = 0, independences = 0;
    var colOwner = new System.Collections.Generic.Dictionary<Colony, string>();
    var canalOpenQ = new System.Collections.Generic.Dictionary<string, int>();   // 運河名→開通四半期
    var cpBlockedQ = new System.Collections.Generic.Dictionary<string, int>();    // チョークポイント名→封鎖された四半期数
    int maxCeilGoods = 0; string crashMsg = "";

    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        try { im.StepQuarter(); }
        catch (System.Exception ex) { crashMsg = $"Q{q} で破綻：{ex.GetType().Name} {ex.Message}"; break; }

        for (int i = 0; i < im.Nations.Count && i < lastPolity.Length; i++)   // 分離独立/建国で im.Nations が増える＝固定長 lastPolity/polityChanges の範囲内（既存国）のみ政変を追跡（越境で IndexOutOfRange しない）
            if (im.Nations[i].World.Polity != lastPolity[i]) { polityChanges[i]++; lastPolity[i] = im.Nations[i].World.Polity; }
        foreach (var war in dip.Wars) warsSeen.Add(war);

        // 植民地の所有変化（割譲・独立）
        var cur = new System.Collections.Generic.Dictionary<Colony, string>();
        foreach (var p in dip.Powers) foreach (var c in p.ColonyList) cur[c] = p.BaseName;   // 不変国名＝変名を偽割譲と数えない
        foreach (var kv in cur)
            if (colOwner.TryGetValue(kv.Key, out var prev) && prev != kv.Value) cessions++;
        foreach (var kv in colOwner)
            if (!cur.ContainsKey(kv.Key) && dip.IndependentColonies.Contains(kv.Key)) independences++;
        colOwner = cur;

        // 運河の開通・チョークポイントの封鎖
        foreach (var cp in dip.Chokepoints)
        {
            if (cp.IsCanal && cp.IsOpen && !canalOpenQ.ContainsKey(cp.Name)) canalOpenQ[cp.Name] = q;
            bool contested = cp.Owner is { Nation: not null } && dip.Powers.Any(p => p.Nation != null && p != cp.Owner && dip.IsAtWar(cp.Owner!, p));
            if (contested) cpBlockedQ[cp.Name] = cpBlockedQ.GetValueOrDefault(cp.Name) + 1;
        }

        // 極端価格（天井≥1900%張り付き財の最大数）
        int ceil = 0;
        foreach (var n in im.Nations) foreach (var good in n.World.Region.Goods)
        {
            if (good.Tier == GoodTier.Service) continue;
            var mk = n.World.Region.Markets[good.Id];
            if (mk.LastDemand < 1) continue;
            if (good.RefPrice > 1e-9 && mk.Price / good.RefPrice >= 19.0) ceil++;
        }
        maxCeilGoods = System.Math.Max(maxCeilGoods, ceil);
    }

    Console.WriteLine($"=== 結合テスト seed {seed}：{ic.Length}カ国×{Coefficients.GameQuarters / 4}年({Coefficients.GameQuarters}四半期) ===");
    Console.WriteLine(crashMsg == "" ? $"破綻：なし（{Coefficients.GameQuarters}四半期完走）" : "⚠ " + crashMsg);
    int civilWars = im.Nations.Count(n => n.World.HadCivilWar), secessions = im.Nations.Count(n => n.World.HadSecession);
    Console.WriteLine($"政変：総遷移 {polityChanges.Sum()} 回／内戦を経験 {civilWars}カ国・分離戦 {secessions}カ国");
    int decisive = dip.Powers.Sum(p => p.WarsWon);
    Console.WriteLine($"戦争：総数 {warsSeen.Count}（決定的講和 {decisive}・白紙 {warsSeen.Count - decisive}）");
    Console.WriteLine($"植民地：割譲（戦争で本国交代）{cessions} 回・独立 {independences} 回　（ColonyList経路のみ＝ColoniesAreRegions点火後の州昇格した植民地は数えていない）");
    Console.WriteLine($"運河：" + string.Join("／", dip.Chokepoints.Where(c => c.IsCanal).Select(c => $"{c.Name} {(canalOpenQ.ContainsKey(c.Name) ? $"{1783 + canalOpenQ[c.Name] / 4}年 開通" : "未開通")}")));
    Console.WriteLine($"経路（海峡/運河の封鎖された四半期）：" + string.Join("／", dip.Chokepoints.Select(c => $"{c.Name} {cpBlockedQ.GetValueOrDefault(c.Name)}Q")));
    Console.WriteLine($"極端価格：天井張り付き財の最大数 {maxCeilGoods}（全{ic.Length}カ国合計・植民地原料の構造的天井を含む）");
    Console.WriteLine($"順位（{1783 + Coefficients.GameQuarters / 4}・国力スコア）：");
    var ranked = im.Nations.Select(n => (n.World.SelfPower?.Name, Score: NationalScore.Of(n.World), Pol: n.World.Polity, n.World.Region.TotalIndustrialization)).OrderByDescending(t => t.Score).ToList();
    int rank = 1;
    foreach (var t in ranked) Console.WriteLine($"  {rank++,2}. {t.Item1,-16} スコア{t.Score,5:F0}　{PolJa(t.Pol)}　工業化{t.TotalIndustrialization:F2}");
    return;
}

// === 土地（耕地）の収穫逓減がどこで拘束するか実測： dotnet run -- landprobe [seed] ===
// 各州の農村人口・耕地賦存量・landFactor を125年後に列挙。landFactor<1 の州＝土地が実際に効いている＝混雑。NYの農村/都市内訳も。
if (args.Length > 0 && args[0] == "landprobe")
{
    ulong seed = args.Length > 1 && ulong.TryParse(args[1], out var s) ? s : 1789UL;
    NationConfig[] lc = WorldData.Nations();
    var lm = new MultiNationSim(lc, seed, _ => new UtilityAgent());
    lm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    var landCoef = new Coefficients();
    double expo = landCoef.LandDiminishExponent;
    double LandFac(Region r)
    {
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        return Territory.LandFactor(r, rural, landCoef);   // エンジン本体（Territory.cs）と同一式を直接呼ぶ＝式の二重定義をしない
    }
    double minSeen = 1.0; int bindCount = 0, activeCount = 0;
    for (int q = 0; q < 500; q++)
    {
        lm.StepQuarter();
        foreach (var n in lm.Nations) foreach (var r in n.World.Regions)
            if (r.State == RegionState.Active) { double lf = LandFac(r); minSeen = System.Math.Min(minSeen, lf); }
    }
    Console.WriteLine($"=== landprobe seed {seed}：125年後の各州の土地係数（指数{expo}）===");
    Console.WriteLine($"全期間で観測した最小 landFactor＝{minSeen:F3}（1.000＝一度も土地が拘束しなかった）");
    foreach (var n in lm.Nations)
    {
        var name = n.World.SelfPower?.Name ?? "?";
        foreach (var r in n.World.Regions)
        {
            if (r.State != RegionState.Active) continue;
            activeCount++;
            double rural = 0, urban = 0; foreach (var p in r.Pops) { if (p.IsUrban) urban += p.Size; else rural += p.Size; }
            double lf = LandFac(r);
            bool bind = lf < 0.98;
            if (bind) bindCount++;
            string land = r.ArableLand >= 1e8 ? "∞" : $"{r.ArableLand:F0}";
            if (bind || (name == "アメリカ"))
                Console.WriteLine($"  {name,-14} {r.Name,-10} 農村{rural,8:F0} 都市{urban,8:F0} 耕地{land,8} landFactor {lf:F3}{(bind ? "  ← 拘束" : "")}");
        }
    }
    Console.WriteLine($"拘束中の州（landFactor<0.98）＝{bindCount}/{activeCount}（active州）");
    return;
}

if (args.Length > 0 && args[0] == "refprobe")
{
    // ★同定の計器（2026-09-06・一覧 §0-2「同定が先」）：休眠係数の目盛りを実測分布から決める。refprobe [四半期=480] [間隔=160]
    //   1人あたり実質GDP（AbsorptionIncomeRef の目盛り＝IncomeAbsorption と同じ w.Gdp/人口）・研究基盤=Elite×人口（ResearchBaseRefPop）・艦隊ゲージ×海軍維持費（NavalFuelPerGauge）。中央値と先端（最大）を印字。
    int rq = ArgInt(args, 1, 480), rint = ArgInt(args, 2, 160);
    var rm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    rm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    Console.WriteLine("=== refprobe：同定の目盛り（中央値／先端） ===");
    for (int q = 1; q <= rq; q++)
    {
        rm.StepQuarter();
        if (q % rint != 0 && q != rq) continue;
        var perCap = new List<(string, double)>(); var rbase = new List<(string, double)>(); var fleet = new List<(string, double)>();
        foreach (var n in rm.Nations)
        {
            var w = n.World; if (w.SelfPower is not { } sp || !(w.TotalPopulation > 1e-9)) continue;
            perCap.Add((sp.BaseName, w.Gdp / w.TotalPopulation));   // IncomeAbsorption と同じ実質GDP/人
            rbase.Add((sp.BaseName, w.Elite * w.TotalPopulation));
            fleet.Add((sp.BaseName, sp.FleetGauge * w.NavyUpkeep));
        }
        static string Sum(List<(string n, double v)> xs) { xs.Sort((a, b) => a.v.CompareTo(b.v)); var med = xs[xs.Count / 2]; var top = xs[^1]; return $"中央値 {med.v:N2}({med.n})　先端 {top.v:N2}({top.n})　上位3 {string.Join("/", xs.GetRange(Math.Max(0, xs.Count - 3), Math.Min(3, xs.Count)).ConvertAll(t => t.n))}"; }
        Console.WriteLine($"Q{q,3}({rm.Nations[0].World.Year})  1人あたり実質GDP: {Sum(perCap)}");
        Console.WriteLine($"            研究基盤(Elite×人口): {Sum(rbase)}");
        Console.WriteLine($"            艦隊ゲージ×海軍維持費: {Sum(fleet)}");
    }
    return;
}
if (args.Length > 0 && args[0] == "ruleprobe")
{
    // ★支配度の一般化 段1 の計器（2026-09-06）：主要国の4区分（国家・在地・外国・無統治）の人口加重平均。POB_COEF="RuleSharesEnabled=1" で走らせる。
    int rq = ArgInt(args, 1, 200), rint = ArgInt(args, 2, 40);
    var rm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    rm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    string[] watch = ["清", "イギリス", "フランス", "ロシア", "日本", "オスマン", "プロイセン", "スペイン"];
    Console.WriteLine($"=== ruleprobe（RuleSharesEnabled={new Coefficients().RuleSharesEnabled}）：4区分 [国家/在地/外国/無統治]（人口加重）===");
    for (int q = 1; q <= rq; q++)
    {
        rm.StepQuarter();
        if (q % rint != 0 && q != rq) continue;
        foreach (var n in rm.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } sp || System.Array.IndexOf(watch, sp.BaseName) < 0) continue;
            double[] acc = new double[4]; double pop = 0;
            foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; double sz = r.SumPopulation(); pop += sz; for (int k = 0; k < 4 && k < r.Rule.Length; k++) acc[k] += r.Rule[k] * sz; }
            if (pop > 1e-9) for (int k = 0; k < 4; k++) acc[k] /= pop;
            Console.WriteLine($"Q{q,3}({w.Year}) {sp.BaseName,-8} [{acc[0]:F2}/{acc[1]:F2}/{acc[2]:F2}/{acc[3]:F2}]  行政効率={w.AdminEfficiency:F2} 農奴={w.SerfEmancipation} 州={w.Regions.Count}");
        }
    }
    return;
}
if (args.Length > 0 && args[0] == "knowprobe")
{
    // ★知識と人的資本の計器（2026-09-06）：識字率(Elite)・専門人材率(Expertise)・接触度(TradeContactShare)・技術水準。段2の需要点火（HigherEduNeedPerEducated）の効きもここで見る。
    int kq = ArgInt(args, 1, 480), kint = ArgInt(args, 2, 80);
    var km = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    km.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    string[] watch = ["イギリス", "フランス", "プロイセン", "アメリカ", "ロシア", "清", "日本", "朝鮮"];
    Console.WriteLine("=== knowprobe：識字率(Elite)／専門人材率(Expertise)／接触度／技術 ===");
    for (int q = 1; q <= kq; q++)
    {
        km.StepQuarter();
        if (q % kint != 0 && q != kq) continue;
        foreach (var n in km.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } sp || System.Array.IndexOf(watch, sp.BaseName) < 0) continue;
            var cap = w.Region; var hm = cap.Markets[Catalog.HigherEducationId];
            double eduFillElite = 0, eliteSize = 0, uniOut = 0;
            foreach (var pp in cap.Pops) { if (pp.IsModernElite) { eduFillElite += pp.LastEducationFill * pp.Size; eliteSize += pp.Size; } }
            foreach (var pr in cap.Producers) if (pr.GoodId == Catalog.HigherEducationId) uniOut += pr.Output;
            double resSize = cap.ResearcherPopIndex >= 0 && cap.ResearcherPopIndex < cap.Pops.Count ? cap.Pops[cap.ResearcherPopIndex].Size : -1;
            var bm = cap.Markets[Catalog.BookId];
            Console.WriteLine($"Q{q,3}({w.Year}) {sp.BaseName,-6} 識字率={w.Elite:F3} 専門人材率={w.Expertise:F4}（先端{w.WorldFrontierExpertise:F4}） 接触度={w.TradeContactShare:F2} 技術={w.TechLevel:F2}（先端{w.WorldTechLevel:F2}）"
                + $"  首都の高等教育: 需要={hm.LastDemand:F1} 産出={uniOut:F1} 価格={hm.Price:F2} 近代エリートの教育充足={(eliteSize > 0 ? eduFillElite / eliteSize : 0):F2} 研究者={resSize:F0} 書籍(在庫{bm.Stock:F0}/価格{bm.Price:F2})");
        }
    }
    return;
}
if (args.Length > 0 && args[0] == "privprobe")
{
    // ★特権台帳の計器（2026-09-06・段1の「働いたか」の確認＝ゲート緑≠機構が働いた）。POB_COEF="PrivilegeLedgerEnabled=1" で走らせる。
    //   privprobe [四半期=200] [間隔=40]：主要国の4特権の取り分（直近1年・貨幣）と保有年数を印字。
    int pq = ArgInt(args, 1, 200), pint = ArgInt(args, 2, 40);
    var pm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    pm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    string[] watch = ["清", "イギリス", "フランス", "ロシア", "日本", "オスマン", "プロイセン", "スペイン"];
    Console.WriteLine($"=== privprobe（PrivilegeLedgerEnabled={new Coefficients().PrivilegeLedgerEnabled}）：取り分/年＝{string.Join("・", Privileges.Names)}　保有年数 ===");
    for (int q = 1; q <= pq; q++)
    {
        pm.StepQuarter();
        if (q % pint != 0 && q != pq) continue;
        foreach (var n in pm.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } sp || System.Array.IndexOf(watch, sp.BaseName) < 0) continue;
            var t = w.PrivilegeTakeYear; var y = w.PrivilegeHeldYears;
            Console.WriteLine($"Q{q,3}({w.Year}) {sp.BaseName,-8} 取り分/年 [{t[0]:N0} / {t[1]:N0} / {t[2]:N0} / {t[3]:N0}]  保有年 [{y[0]:F1} / {y[1]:F1} / {y[2]:F1} / {y[3]:F1}]  農奴={w.SerfEmancipation} 官僚={w.Bureaucracy} 国教={w.StateReligion}");
        }
    }
    return;
}
if (args.Length > 0 && args[0] == "factionprobe")
{
    // ★段1の計器（F3b蜂起doc §5・2026-09-08）：**点火の各項**を年次で印字し、段4の閾値（RevoltIgnitionIntensity）を実測から同定する（勘で置かない）。
    //   項＝intensity（＝FactionStrength÷f.Size＝f.Size が代数的に約分され AvgMilitancy×結束×(1−発言権)×FactionRevoltScale が残る「組織された意志」）／
    //      州の絶望（RegionRevoltHeat＝F1と同じ物差し）／怨嗟（min(1, FactionRejectionMutation×Rejections)＝変異ロールと同じ飽和の書き方）／
    //      f.Size／hold（LocalizedHold＝州按分後の保持力）／militia（LoyalistMilitia＝郷勇・全国）。
    //   ⚠世界には1ビットも書かない（読み取りのみ・純関数だけを呼ぶ）。⚠MechanismProbe.Site は増やさない（4サイトの成立率の和が1になる性質を壊さない）。
    //   使い方： dotnet run -- factionprobe [seed=1789] [国名=清/英/仏/露/オスマン] [四半期=GameQuarters]
    ulong fpSeed = ArgUlong(args, 1, 1789UL);
    string[] fpWatch = args.Length > 2 && args[2].Length > 0 && !int.TryParse(args[2], out _) ? [args[2]] : ["清", "イギリス", "フランス", "ロシア", "オスマン"];
    int fpQ = ArgInt(args, 3, Coefficients.GameQuarters);
    int fpEvery = System.Math.Max(1, ArgInt(args, 4, 1));   // ★段0の3行（食料/熱/層）の印字間隔（年）。⚠分布の母集団は間隔に関わらず毎年ぶん貯める
    var fm = new MultiNationSim(WorldData.Nations(), fpSeed, _ => new UtilityAgent());
    fm.StartWarmup();   // ★計器の助走統一（2026-09-07）＝「測る世界」を帯の世界と一致させる
    int fpN0 = fm.Nations.Count;
    var fpAlive = new HashSet<Simulation>(fm.Nations);
    var fpRebelBorn = new HashSet<Simulation>();          // 内戦で建った国＝建国の四半期に CivilWarParent 付きの戦争の当事者だった国
    int fpBirths = 0, fpDeaths = 0, fpMaxRebels = 0;
    var fpFoundRegions = new List<int>();                  // 建国時の州数（段7の受け入れ基準）
    var fpIntensity = new List<double>();                  // 候補（結束≥闘争閾値）の intensity 全件＝閾値同定の母集団
    var fpEligible = new List<double>();                   // 国×年ごとの「門2を越えた候補の意志の最大値」＝θを史実の頻度帯へ合わせるための母集団
    int fpF1Sup = 0, fpF1Con = 0, fpCwOpen = 0, fpSecOpen = 0, fpRebelWin = 0, fpParentWin = 0, fpRump = 0, fpRevolt = 0;
    int fpF3Concede = 0, fpF3Compensate = 0;   // F3b危機の行き先（受諾／別軸譲歩）＝蜂起に至らなかった危機の内訳
    int fpF3Notify = 0;   // ★要求は法の経路（2026-09-11）：通知（「要求を掲げた」）＝危機が立った回数
    int fpPrevLog = fm.Diplomacy.Log.Count;
    var cf0 = new Coefficients();
    // ★段0の計器（2026-09-09・docs/202608/20260819_闘争度を打倒の動機にする_設計メモ §7-3 の点火値を**勘で置かない**ための実分布）。
    //   ⚠世界には1ビットも書かない（読むのは pop/州のフィールドと Politics の純関数だけ）。⚠MechanismProbe.Site は増やさない。
    //   ①pop単位の食料充足 FoodMet/FoodNeed の実分布（⚠「主食」ではない＝EconomyStep が主食に加えて動物性タンパクを Nutrition 加重で同じ2本へ積む）。⚠設計は `DynMandateFoodAnchor=0.8` を pop 単位の閾値へ流用しようとしているが、
    //     この 0.8 の**既存の読み手は3箇所とも国平均**（Politics.cs 天命 dmFood と飢えの記憶・SelfPlay.cs の飢饉判定＝いずれも w.FoodFill）。
    //     国平均に較正された数を pop 単位の分母へ移すのは本企画が最頻バグ型として記録している「分子と分母の非対称」なので、この分布を見るまで点火しない。
    //   ②F1の heat＝州平均闘争度そのもの（★段2で欠乏因子との積をやめた＝欠乏は闘争度の中の飢餓の駆動項が運ぶ）。
    //   ③3層の重み wEdu=min(意識,識字)／wMid=識字−wEdu／文盲=1−識字（Politics.cs の闘争度ループと同一式・和は厳密に1）。
    //   ⚠通算は**国ごとに分ける**（粒度＝計器の5つの失敗型のひとつ）。清の pop 充足 p50 は英より 0.2 低く、混ぜた分位はどちらの国のものでもない。
    var fp0Food = new Dictionary<string, List<(double v, double w)>>();     // pop単位の食料充足（人口加重・国ごとに全観測を通算）
    var fp0Drop = new Dictionary<string, List<(double v, double w)>>();     // ★回B（2026-09-22）：pop単位の落ち込み D＝平常 FoodRef − 充足（同じ母集団・未捕捉は外す）＝飢餓の駆動の起点と利得の母数
    var fp0Edu = new Dictionary<string, List<(double v, double w)>>();      // 3層＝政治意識あり
    var fp0Mid = new Dictionary<string, List<(double v, double w)>>();      // 3層＝識字あり×政治意識なし（危険な中間）
    var fp0Ill = new Dictionary<string, List<(double v, double w)>>();      // 3層＝文盲
    var fp0Heat = new Dictionary<string, List<(double v, double w)>>();     // 州ごとの heat（州1件=重み1＝F1は州を単位に選ぶ）
    var fp0HeatMil = new Dictionary<string, List<(double v, double w)>>();  // heat の内訳＝州平均闘争度
    var fp0HeatMax = new Dictionary<string, List<(double v, double w)>>();  // 国×年の F1 が評価する州の heat（★項目3＝Politics.SelectRevoltRegion が選んだ州＝既定0では最大 heat と同値。候補ゼロの年も0で数える）
    var fp0Ratio = new Dictionary<string, List<(double v, double w)>>();    // 最熱州の 強度÷保持（F1の分岐の実測比＝段2で分岐の到達性を保つ物差し）
    var fp0Crowd = new Dictionary<string, List<(double v, double w)>>();    // 最熱州の群衆規模（＝heat の選択が微小州に流れていないかの番人）
    var fp0PopNoNeed = new Dictionary<string, double>(); var fp0PopAll = new Dictionary<string, double>();   // FoodNeed=0（当月に主食需要が立っていない）pop の人口比
    // ★段3の計器（2026-09-09・設計メモ §7-1 裁定1「参加の述語は不満度」）＝**不満度の実分布**。
    //   参加の述語を闘争度から不満度へ替えるとき、閾値をどう扱うかは「不満度が平時にどの水準にあるか」で決まる＝勘で置かないための母集団。
    //   ⚠読むのは Politics.Discontent **1本**（純関数・世界には1ビットも書かない）＝計器に第二の定義を書かない。
    var fp0Disc = new Dictionary<string, List<(double v, double w)>>();      // 不満度（pop×年・人口加重）
    var fp0Part = new Dictionary<string, List<(double v, double w)>>();      // 参加＝max(不満度, 闘争度)（同上）＝闘争度による上書きを含む形
    var fp0Mil = new Dictionary<string, List<(double v, double w)>>();       // 闘争度そのもの（比較の腕＝現行の参加の述語が読む量）
    var fp0PartShare = new Dictionary<string, List<(double v, double w)>>(); // 国×年の「参加加重の人口比」＝Σ size×参加 ÷ 総人口（＝運動の規模の上限）
    //   ①' **持続**（設計の点火条件は「1四半期では立たず2四半期で立つ」＝断面の分位では測れない量）。
    //     下の断面（①②③）は年1回の走査だが、この持続だけは**四半期ごと**に走る。pop の同一性は参照で見る（Pop は参照型）。
    //     ⚠probe ローカルの辞書だけ＝世界には1ビットも書かない。⚠消えた pop の残りかすは辞書に残る（連続数の誤りにはならない＝次に同じ参照は現れない）。
    var fp0RunSat = new Dictionary<Pop, int>();    // 充足 < 飽和水準（drive=1）が続いている四半期数
    var fp0RunHalf = new Dictionary<Pop, int>();   // 充足 < drive が門(0.50)を超える水準 が続いている四半期数
    var fp0EpiSat = new Dictionary<string, List<(double v, double w)>>();    // 終わったエピソードの長さ（四半期・人口加重）
    var fp0EpiHalf = new Dictionary<string, List<(double v, double w)>>();
    //   ①'' 欠乏の**梯子**（2026-09-11・項目1）＝起点と利得を分位から導くための母集団：固定の水準 0.40〜0.80 で①'と同じ四半期走査を並べる
    //     （①' は係数の2水準しか見ない＝「どの水準なら英の典型が1四半期で終わり、清/露の欠乏が半年続くか」は梯子でしか読めない）。世界には1ビットも書かない。
    double[] fpLadder = { 0.40, 0.45, 0.50, 0.55, 0.60, 0.65, 0.70, 0.75, 0.80 };
    var fpLadRun = new Dictionary<Pop, int>[fpLadder.Length];
    var fpLadEpi = new Dictionary<string, List<(double v, double w)>>[fpLadder.Length];
    for (int li0 = 0; li0 < fpLadder.Length; li0++) { fpLadRun[li0] = new Dictionary<Pop, int>(); fpLadEpi[li0] = new Dictionary<string, List<(double v, double w)>>(); }
    var fpLadBelow = new Dictionary<string, double[]>();   // 国→水準ごとの「水準未満の pop×四半期の人口」（四半期ごとの断面＝①の年1回より密）
    var fpLadTot = new Dictionary<string, double>();       // 国→pop×四半期の人口（分母）
    static List<(double v, double w)> FpBag(Dictionary<string, List<(double v, double w)>> d, string k)
    { if (!d.TryGetValue(k, out var l)) d[k] = l = new List<(double v, double w)>(); return l; }
    // 人口加重の分位（値で昇順・重みの累積が p を越える最初の値）。空なら -1（印字は「—」）
    static double FpQ(List<(double v, double w)> xs, double p)
    {
        if (xs.Count == 0) return -1;
        xs.Sort((a, b) => a.v.CompareTo(b.v));
        double tot = 0; foreach (var e in xs) tot += e.w;
        if (!(tot > 0)) return -1;
        double acc = 0;
        foreach (var e in xs) { acc += e.w; if (acc >= p * tot) return e.v; }
        return xs[^1].v;
    }
    // 閾値 th を**下回る**重みの割合（NaN は否定形で下回り側へ落とさない＝`e.v < th` は NaN で false）
    static double FpBelow(List<(double v, double w)> xs, double th)
    {
        double tot = 0, hit = 0; foreach (var e in xs) { tot += e.w; if (e.v < th) hit += e.w; }
        return tot > 0 ? hit / tot : 0;
    }
    static string FpS(double x) => x < 0 ? "—" : x.ToString("F3");
    // ★段2で実装された（＝もう仮置きではない）：門は RegionalRevoltThreshold＝0.50。計器は係数を読む（2026-09-11）。
    // ★2026-09-22 回B：駆動は**平常（Pop.FoodRef）からの落ち込み** D＝平常−充足 を測る（Politics.FamineDrive）＝起点は落ち込みの量（充足の水準ではない）。
    double Fp0Anchor = cf0.FamineDropOnset;   // 飢餓の駆動項の起点＝落ち込み D がこれを超えると駆動>0（Politics.FamineDrive と同じ1本）
    double Fp0Gain = cf0.FamineDriveGain;
    //   POB_COEF で利得0（休眠）にした導出の腕では飽和・門超えの落ち込みは定義されない＝NaN（印字は「—」・持続の走査は数えない）。
    double fp0Sat = Fp0Gain > 0 ? Fp0Anchor + 1.0 / Fp0Gain : double.NaN;                              // drive が1に飽和する落ち込み＝飢餓項**単独**で2四半期後に門を越えられる落差
    double fp0Half = Fp0Gain > 0 ? Fp0Anchor + cf0.RegionalRevoltThreshold / Fp0Gain : double.NaN;     // drive が門を超える落ち込み＝他項と合わせて越えうる落差
    // 平常は Politics.Step の中で駆動を計算した【後】に今の充足へ寄せられる（慣れの速度 LivingRefSpeed）。計器が四半期末に読む FoodRef は更新後なので、
    //   エンジンが読んだ更新前の平常へ代数で戻す：ref_pre = (ref_post − s×充足)/(1−s)（充足は次の月頭まで不変＝同じ値）。未捕捉（<0）は D=0 と同じ扱いで外す。
    static double FpDrop(Pop p, Coefficients c)
    {
        if (!(p.FoodNeed > 1e-9) || !(p.FoodRef >= 0)) return double.NaN;
        double fill = System.Math.Min(1.0, p.FoodMet / p.FoodNeed);
        double s = c.LivingRefSpeed;
        double refPre = s > 0 && s < 1 ? (p.FoodRef - s * fill) / (1.0 - s) : p.FoodRef;
        return refPre - fill;
    }
    Console.WriteLine($"=== factionprobe seed {fpSeed}／{string.Join("・", fpWatch)}　{fpQ}四半期　点火の各項（年次）===");
    Console.WriteLine($"    候補＝結束≥{cf0.FactionCohesionFightThreshold:F2} かつ州がActive かつ**親が今も所有**（段0の幽霊除外と同じ述語）");
    Console.WriteLine($"    段0の3行（食料/熱/層）＝{fpEvery}年ごとに印字（分布の母集団は毎年ぶん）。⚠pop の FoodMet/FoodNeed は**月次の累積器**（EconomyStep が月頭に0で始める）＝四半期末に読むのは**その四半期の3月目**の値");
    Console.WriteLine("年    国       選択    運動                              規模       闘争 結束  intensity  州の絶望 怨嗟 拒否   強度        hold       militia    比  体制   要求 行き先（危機が立ったときのAI決着）");
    for (int q = 1; q <= fpQ; q++)
    {
        fm.StepQuarter();
        // --- 国の出入り（★純増でなく建国回数と消滅回数を別に数える＝壊れていた計器の型）---
        var fpNow = new HashSet<Simulation>(fm.Nations);
        foreach (var n in fm.Nations)
            if (fpAlive.Add(n))
            {
                fpBirths++;
                fpFoundRegions.Add(n.World.Regions.Count);
                if (n.World.SelfPower is { } np)
                    foreach (var war in fm.Diplomacy.Wars)
                        if (war.CivilWarParent is not null && war.CivilWarParent != np && (war.A == np || war.B == np)) { fpRebelBorn.Add(n); break; }
            }
        fpDeaths += fpAlive.RemoveWhere(x => !fpNow.Contains(x));
        fpRebelBorn.RemoveWhere(x => !fpNow.Contains(x));
        if (fpRebelBorn.Count > fpMaxRebels) fpMaxRebels = fpRebelBorn.Count;
        // --- ログの差分から発火と決着を数える（Site を増やさずに済む唯一の物差し）---
        for (int li = fpPrevLog; li < fm.Diplomacy.Log.Count; li++)
        {
            string line = fm.Diplomacy.Log[li];
            if (line.Contains("一揆を鎮圧")) fpF1Sup++;
            else if (line.Contains("一揆に譲歩")) fpF1Con++;
            else if (line.Contains("分離独立戦争")) fpSecOpen++;
            else if (line.Contains("地図の上に立った")) fpCwOpen++;
            else if (line.Contains("の下で再統一")) fpRebelWin++;
            else if (line.Contains("立ち行かず")) fpRump++;          // 残骸の平和的復帰＝軍事的決着でない（下の再併合から差し引く）
            else if (line.Contains("再併合")) fpParentWin++;
            // ★是正2026-09-11（99ログ「Runner/Program.cs:5432 は『に拠って立った』だけを数える」の指摘）：TriggerFactionRevolt の4枝全部を蜂起に数える
            //   （内戦一元化＝に拠って立った／民族＝分離の内乱を起こした／政体戦＝が蜂起した／要求戦＝要求を掲げて蜂起した）。firecheck の FactionRevolt と同じ母集団になる。
            else if (line.Contains("に拠って立った") || line.Contains("が分離の内乱を起こした") || line.Contains("が要求を掲げて蜂起した") || line.Contains("が蜂起した")) fpRevolt++;
            // ★危機の行き先（2026-09-08）：蜂起0回のとき「点火しなかった」のか「点火したが逸れた」のかを分ける
            //   （F3bの決着3枝は必ず1行ずつ書く＝AI側は受諾／別軸譲歩／蜂起。⚠無血鎮圧の枝は2026-09-08の段6で物理削除）。
            else if (line.Contains("の要求を受け入れた") || line.Contains("の要求を法で受け入れた")) fpF3Concede++;   // ★要求は法の経路：「法で受け入れた」（Politics.CrisisThroughLawPath）も受諾の実体＝同じ行に数える
            else if (line.Contains("が要求を掲げた")) fpF3Notify++;
            else if (line.Contains("へ別の軸で譲歩した")) fpF3Compensate++;
        }
        fpPrevLog = fm.Diplomacy.Log.Count;
        // ---- ★段0①'（四半期ごと）＝欠乏の**持続**。断面の分位（下）は年1回なので、設計の「2四半期」条件はここでしか測れない
        foreach (var n in fm.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } spR || (System.Array.IndexOf(fpWatch, spR.BaseName) < 0 && System.Array.IndexOf(fpWatch, spR.Name) < 0)) continue;
            string rKey = spR.BaseName;
            foreach (var reg0 in w.Regions)
            {
                foreach (var p in reg0.Pops)
                {
                    if (!(p.Size > 1e-9) || !(p.FoodNeed > 1e-9)) continue;
                    double fill = System.Math.Clamp(p.FoodMet / p.FoodNeed, 0, 1);
                    // ①'の持続は**落ち込み**で測る（回B・2026-09-22）＝駆動が飽和／門超えの落差が続いた四半期数（`>` は NaN で false＝未捕捉と休眠の腕は数えない）
                    double drop = FpDrop(p, w.Coef);
                    if (drop > fp0Sat) fp0RunSat[p] = (fp0RunSat.TryGetValue(p, out var rs) ? rs : 0) + 1;
                    else if (fp0RunSat.TryGetValue(p, out var rsEnd)) { FpBag(fp0EpiSat, rKey).Add((rsEnd, p.Size)); fp0RunSat.Remove(p); }
                    if (drop > fp0Half) fp0RunHalf[p] = (fp0RunHalf.TryGetValue(p, out var rh) ? rh : 0) + 1;
                    else if (fp0RunHalf.TryGetValue(p, out var rhEnd)) { FpBag(fp0EpiHalf, rKey).Add((rhEnd, p.Size)); fp0RunHalf.Remove(p); }
                    if (!fpLadBelow.TryGetValue(rKey, out var lb)) fpLadBelow[rKey] = lb = new double[fpLadder.Length];
                    fpLadTot[rKey] = (fpLadTot.TryGetValue(rKey, out var lt) ? lt : 0) + p.Size;
                    for (int li = 0; li < fpLadder.Length; li++)
                    {
                        if (fill < fpLadder[li]) { lb[li] += p.Size; fpLadRun[li][p] = (fpLadRun[li].TryGetValue(p, out var rl) ? rl : 0) + 1; }
                        else if (fpLadRun[li].TryGetValue(p, out var rlEnd)) { FpBag(fpLadEpi[li], rKey).Add((rlEnd, p.Size)); fpLadRun[li].Remove(p); }
                    }
                }
            }
        }
        if (q % 4 != 0) continue;
        foreach (var n in fm.Nations)
        {
            var w = n.World; var c = w.Coef;
            if (w.SelfPower is not { } sp || (System.Array.IndexOf(fpWatch, sp.BaseName) < 0 && System.Array.IndexOf(fpWatch, sp.Name) < 0)) continue;
            double coerce = PoliticalQuantities.EffectiveCoerce(w, c);
            // ---- ★段0（①食料の実分布／③3層の重み）＝Politics の闘争度ループと**同じ母集団**（Seceded州を除く・popの絞りなし）を1回だけ走査する
            {
                var yFood = new List<(double v, double w)>(); var yDrop = new List<(double v, double w)>();
                var yEdu = new List<(double v, double w)>(); var yMid = new List<(double v, double w)>(); var yIll = new List<(double v, double w)>();
                var yDisc = new List<(double v, double w)>(); var yPart = new List<(double v, double w)>(); var yMil = new List<(double v, double w)>();
                double yPop = 0, yNoNeed = 0, yEduS = 0, yMidS = 0, yIllS = 0, yPartS = 0;
                // ★段3：不満度は pop の純関数だが、政体座標と法の隔たりは**国の量**＝pop ループの外で1回だけ取る（Politics.Discontent の注意書きと同じ作法）
                double[] fpCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
                double[] fpGap = c.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
                foreach (var reg0 in w.Regions)
                {
                    foreach (var p in reg0.Pops)
                    {
                        if (!(p.Size > 1e-9)) continue;
                        yPop += p.Size;
                        if (p.FoodNeed > 1e-9) yFood.Add((System.Math.Clamp(p.FoodMet / p.FoodNeed, 0, 1), p.Size));   // 飢餓の駆動項（Politics.cs の FamineDriveGain）と同じ分母ガード・同じクランプ
                        else yNoNeed += p.Size;                                    // 分母なし＝飢餓項は発火しない（分布から外す＝分母を痩せさせないため別に数える）
                        double yD = FpDrop(p, c);                                  // ★回B：平常からの落ち込み（エンジンが読んだ更新前の平常）＝起点と利得の母数
                        if (double.IsFinite(yD)) yDrop.Add((yD, p.Size));
                        double we = System.Math.Min(p.Consciousness, p.Literacy), wm = p.Literacy - we, wi = 1.0 - p.Literacy;
                        yEdu.Add((we, p.Size)); yMid.Add((wm, p.Size)); yIll.Add((wi, p.Size));
                        yEduS += we * p.Size; yMidS += wm * p.Size; yIllS += wi * p.Size;
                        double fpD = Politics.Discontent(w, c, p, fpCoord, fpGap.Length > 0 ? fpGap[PolicyPreferences.BlocOf(p)] : 0);
                        double fpPart = System.Math.Max(fpD, p.Militancy);
                        yDisc.Add((fpD, p.Size)); yMil.Add((p.Militancy, p.Size)); yPart.Add((fpPart, p.Size)); yPartS += fpPart * p.Size;
                    }
                }
                string fpKey = sp.BaseName;
                FpBag(fp0Food, fpKey).AddRange(yFood); FpBag(fp0Drop, fpKey).AddRange(yDrop); FpBag(fp0Edu, fpKey).AddRange(yEdu); FpBag(fp0Mid, fpKey).AddRange(yMid); FpBag(fp0Ill, fpKey).AddRange(yIll);
                FpBag(fp0Disc, fpKey).AddRange(yDisc); FpBag(fp0Mil, fpKey).AddRange(yMil); FpBag(fp0Part, fpKey).AddRange(yPart);
                FpBag(fp0PartShare, fpKey).Add((yPop > 1e-9 ? yPartS / yPop : 0, 1.0));
                fp0PopAll[fpKey] = (fp0PopAll.TryGetValue(fpKey, out var fpPa) ? fpPa : 0) + yPop;
                fp0PopNoNeed[fpKey] = (fp0PopNoNeed.TryGetValue(fpKey, out var fpPn) ? fpPn : 0) + yNoNeed;
                // ---- ★段0（②F1のheatの内訳）＝F1（CheckRegionalRevolt）と同じ述語・同じ関数で州ごとに測る
                double maxHeat = 0; int nAct = 0, over0 = 0, overB = 0;
                var yHeat = new List<(double v, double w)>();
                foreach (var reg0 in w.Regions)
                {
                    if (reg0.State != RegionState.Active) continue;
                    nAct++;
                    double h = Politics.RegionRevoltHeat(reg0, c);
                    double hm = Politics.AvgRegionMilitancy(reg0);   // ★段2＝h と同値（熱は闘争度1量）。2本のままにしてあるのは、熱の定義が再び変わったときに差が見えるため
                    yHeat.Add((h, 1.0)); FpBag(fp0HeatMil, fpKey).Add((hm, 1.0));
                    if (h > c.RegionalRevoltThreshold) over0++;
                    if (h > cf0.RegionalRevoltThreshold) overB++;
                    if (h > maxHeat) maxHeat = h;
                }
                // ★項目3（2026-09-11）：F1 が実際に評価する州＝エンジンと**同じ関数**（Politics.SelectRevoltRegion）で選ぶ＝鍵は 熱×群衆（旧計器は「最熱州」を別ループで写していた）。
                //   ⚠段6の排他（蜂起を予約した州を外す＝2026-09-29 点火）も同じ。
                var (hot, hotHeat, hCrowd) = Politics.SelectRevoltRegion(w, c);
                FpBag(fp0Heat, fpKey).AddRange(yHeat); FpBag(fp0HeatMax, fpKey).Add((hotHeat, 1.0));
                double hMil = 0, hStr = 0, hHold = 0, hRatio = 0;
                if (hot is not null)
                {
                    hMil = Politics.AvgRegionMilitancy(hot);
                    hStr = hotHeat * hCrowd * c.RegionalRevoltStrength;                                            // CheckRegionalRevolt と同一式
                    hHold = Politics.LocalizedHold(w, c, hot, coerce);                                              // 同上（州按分）
                    hRatio = hHold > 1e-9 ? hStr / hHold : 0;
                    FpBag(fp0Ratio, fpKey).Add((hRatio, 1.0)); FpBag(fp0Crowd, fpKey).Add((hCrowd, 1.0));
                }
                if (w.Year % fpEvery == 0)
                {
                    Console.WriteLine($"{w.Year} {sp.BaseName,-8} 食料 国平均{w.FoodFill,5:F3}／pop分位 p05 {FpS(FpQ(yFood, 0.05))} p25 {FpS(FpQ(yFood, 0.25))} p50 {FpS(FpQ(yFood, 0.50))} p75 {FpS(FpQ(yFood, 0.75))}　人口比 <{Fp0Anchor:F2} {FpBelow(yFood, Fp0Anchor),6:P1} <0.5 {FpBelow(yFood, 0.50),6:P1} <0.3 {FpBelow(yFood, 0.30),6:P1}　需要なし {(yPop > 0 ? yNoNeed / yPop : 0),6:P1}");
                    Console.WriteLine($"{w.Year} {sp.BaseName,-8} 熱   州{nAct,3}件 最大{maxHeat,6:F3} 選択{hotHeat,6:F3}（{(hot?.Name ?? "—"),-10} 闘争{hMil,5:F3}）p90 {FpS(FpQ(yHeat, 0.90))} p50 {FpS(FpQ(yHeat, 0.50))}　門{c.RegionalRevoltThreshold:F2}超 {over0,2}件／門{cf0.RegionalRevoltThreshold:F2}超 {overB,2}件　群衆{hCrowd,10:F0} 強度{hStr,10:F0}÷保持{hHold,10:F0}＝{hRatio,6:F2}");
                    Console.WriteLine($"{w.Year} {sp.BaseName,-8} 層   平均[政治意識 {(yPop > 0 ? yEduS / yPop : 0),5:F3}／中間 {(yPop > 0 ? yMidS / yPop : 0),5:F3}／文盲 {(yPop > 0 ? yIllS / yPop : 0),5:F3}]　p50[{FpS(FpQ(yEdu, 0.50))}／{FpS(FpQ(yMid, 0.50))}／{FpS(FpQ(yIll, 0.50))}]　p90[{FpS(FpQ(yEdu, 0.90))}／{FpS(FpQ(yMid, 0.90))}／{FpS(FpQ(yIll, 0.90))}]");
                }
            }
            FactionState? byInt = null; double bestInt = -1;
            FactionState? byStr = null; double bestStr = -1;
            double bestEligible = 0;   // ★閾値の同定（下の表）：**門2を既に越えている**候補の中での意志の最大値。1つも無ければ0
            foreach (var f in w.Factions)
            {
                if (f.Region.State != RegionState.Active || !w.Regions.Contains(f.Region)) continue;
                double str = Politics.FactionStrength(w, c, f);
                double inten = str / System.Math.Max(1e-9, f.Size);
                if (f.Cohesion >= c.FactionCohesionFightThreshold)
                {
                    fpIntensity.Add(inten);
                    double despair = Politics.RegionRevoltHeat(f.Region, c)
                                   + c.RevoltGrievanceWeight * System.Math.Min(1.0, c.FactionRejectionMutation * f.Rejections);
                    if (despair > c.RegionalRevoltThreshold && inten > bestEligible) bestEligible = inten;   // 段4の点火式と同じ連言（門2は F1 と同じ既存閾値）
                }
                if (inten > bestInt) { bestInt = inten; byInt = f; }
                if (str > bestStr) { bestStr = str; byStr = f; }
            }
            fpEligible.Add(bestEligible);   // ⚠国×年で1件（候補ゼロの年も0で数える＝分母を痩せさせない）
            if (byInt is null)
            {
                if (w.Year % 10 == 0)   // ★「立たない」も計器の出力＝なぜ候補がゼロなのかを年10回だけ印字（列は上の表と別）
                    Console.WriteLine($"{w.Year} {sp.BaseName,-8} 候補なし  勢力{w.Factions.Count}件（州Active かつ親所有 {w.Factions.FindAll(f => f.Region.State == RegionState.Active && w.Regions.Contains(f.Region)).Count}件）　参政権{w.Suffrage} 州{w.Regions.Count} 人口{w.TotalPopulation:F0}");
                continue;
            }
            void Row(string tag, FactionState f)
            {
                double str = Politics.FactionStrength(w, c, f);
                double hold = Politics.LocalizedHold(w, c, f.Region, coerce);
                double militia = Politics.LoyalistMilitia(w, c, f);
                double heat = Politics.RegionRevoltHeat(f.Region, c);
                double grief = System.Math.Min(1.0, c.FactionRejectionMutation * f.Rejections);
                // ★項目1（2026-09-11）＝危機が立ったときの**行き先**を Politics.CrisisThroughLawPath と**同じ式**で名指しする＝行き先は1本
                //   （通知→法の目標（AI＝Apply／プレイヤー＝法律タブ）→動かなければ蜂起。要求が無い運動は法で応じようがない＝蜂起）。読むだけ＝世界には書かない。
                int nDem = FactionSnapshot.ComputeDemands(w, f.Bloc, lawsOnly: true).Count;   // 危機側と同じ引数（RecordCrisisDemands＝法だけ）＝行き先の式を写す計器が綱領の要求を数えない
                string fate = nDem > 0 ? "法の経路" : "蜂起";
                Console.WriteLine($"{w.Year} {sp.BaseName,-8} {tag,-6} {Factions.DisplayName(f),-30} {f.Size,10:F0} {f.AvgMilitancy,4:F2} {f.Cohesion,4:F2}  {str / System.Math.Max(1e-9, f.Size),9:F4}  {heat,7:F3} {grief,4:F2} {f.Rejections,4:F0}   {str,10:F0} {hold,10:F0} {militia,10:F0} {(hold + militia > 1e-9 ? str / (hold + militia) : 0),5:F2}  要求{nDem} →{fate}");
            }
            Row("意志", byInt);                                   // ★段4の新しい選択キー（intensity 最大＝規模で約分した「組織された意志」）
            if (!ReferenceEquals(byStr, byInt) && byStr is not null) Row("現行", byStr);   // 現行の選択キー（強度最大＝規模に比例）＝ずれが人口逆比例の正体
        }
    }
    // ★段0の総括（2026-09-09）＝上の年次3行の母集団を**国ごとに**通算して印字する。⚠これが無いと fp0* は貯めるだけの死んだ蓄積になる
    //   （本企画が「計器が一番バグる」の5型のひとつ＝刺し忘れとして記録している型）。世界には1ビットも書かない。
    Console.WriteLine("\n=== 段0の総括（国ごと・助走後の全期間を通算）===");
    var fp0DerivP90 = new List<double>(); var fp0DerivP95 = new List<double>();   // ★回B：国ごとの落ち込み D の p90/p95（起点と利得の導出＝下で中央値を取る）
    foreach (string fpK in fpWatch)
    {
        if (!fp0Food.TryGetValue(fpK, out var kFood)) { Console.WriteLine($"--- {fpK}：観測なし（消滅したか、名前が一致しない）"); continue; }
        var kEdu = FpBag(fp0Edu, fpK); var kMid = FpBag(fp0Mid, fpK); var kIll = FpBag(fp0Ill, fpK);
        var kHeat = FpBag(fp0Heat, fpK); var kHMil = FpBag(fp0HeatMil, fpK);
        var kHMax = FpBag(fp0HeatMax, fpK); var kRatio = FpBag(fp0Ratio, fpK); var kCrowd = FpBag(fp0Crowd, fpK);
        double kPopAll = fp0PopAll.TryGetValue(fpK, out var kpa) ? kpa : 0, kNoNeed = fp0PopNoNeed.TryGetValue(fpK, out var kpn) ? kpn : 0;
        double kFoodW = 0; foreach (var e in kFood) kFoodW += e.w;
        Console.WriteLine($"--- {fpK}　①pop単位の食料充足 FoodMet/FoodNeed（主食＋動物性タンパクの栄養加重／観測 {kFood.Count} 件・のべ人口 {(long)System.Math.Round(kFoodW):N0}）＝物理の経路（死亡・疫病・移民）が読む絶対水準");
        Console.WriteLine($"      分位 p01 {FpS(FpQ(kFood, 0.01))} p05 {FpS(FpQ(kFood, 0.05))} p10 {FpS(FpQ(kFood, 0.10))} p25 {FpS(FpQ(kFood, 0.25))} p50 {FpS(FpQ(kFood, 0.50))} p75 {FpS(FpQ(kFood, 0.75))} p90 {FpS(FpQ(kFood, 0.90))}");
        Console.WriteLine($"      人口比 <0.73（旧起点＝絶対水準） {FpBelow(kFood, 0.73):P1}／<0.5 {FpBelow(kFood, 0.50):P1}／<0.3 {FpBelow(kFood, 0.30):P1}／<0.1 {FpBelow(kFood, 0.10):P1}　FoodNeed=0（当月に主食需要なし）{(kPopAll > 0 ? kNoNeed / kPopAll : 0):P1}");
        // ★回B（2026-09-22）：駆動の母数＝平常からの落ち込み D。駆動 = clamp(g×(D − 起点), 0, 1) を同じ分布に当てる（Politics.FamineDrive と同じ式・読むだけ）
        var kDropL = FpBag(fp0Drop, fpK);
        var kDrive = new List<(double v, double w)>(kDropL.Count);
        double kDriveSum = 0, kDriveW = 0, kDrivePos = 0, kDriveSat = 0;
        foreach (var e in kDropL)
        {
            double d = Fp0Gain * (e.v - Fp0Anchor); d = !(d > 0) ? 0 : d > 1 ? 1 : d;
            kDrive.Add((d, e.w)); kDriveSum += d * e.w; kDriveW += e.w; if (d > 1e-9) kDrivePos += e.w; if (d > cf0.RegionalRevoltThreshold) kDriveSat += e.w;
        }
        double kD90 = FpQ(kDropL, 0.90), kD95 = FpQ(kDropL, 0.95);
        if (kDropL.Count > 0) { fp0DerivP90.Add(kD90); fp0DerivP95.Add(kD95); }
        Console.WriteLine($"--- {fpK}　①d 平常からの落ち込み D＝FoodRef−充足（観測 {kDropL.Count} 件・人口加重）p01 {FpQ(kDropL, 0.01):F3} p10 {FpQ(kDropL, 0.10):F3} p50 {FpQ(kDropL, 0.50):F3} p75 {FpQ(kDropL, 0.75):F3} p90 {kD90:F3} p95 {kD95:F3} p99 {FpQ(kDropL, 0.99):F3}　>起点{Fp0Anchor:F3} の人口比 {(kDropL.Count > 0 ? 1 - FpBelow(kDropL, Fp0Anchor) : 0):P1}");
        Console.WriteLine($"      drive=clamp({Fp0Gain:F1}×(D−{Fp0Anchor:F3}),0,1)：>0 の人口比 {(kDriveW > 0 ? kDrivePos / kDriveW : 0):P1}／>{cf0.RegionalRevoltThreshold:F2} の人口比 {(kDriveW > 0 ? kDriveSat / kDriveW : 0):P1}／平均 {(kDriveW > 0 ? kDriveSum / kDriveW : 0):F4}／p50 {FpS(FpQ(kDrive, 0.50))} p90 {FpS(FpQ(kDrive, 0.90))}　（飽和は D {fp0Sat:F3} 以上）");
        var kEpiS = FpBag(fp0EpiSat, fpK); var kEpiH = FpBag(fp0EpiHalf, fpK);
        double kEpiSn = 0, kEpiS2 = 0, kEpiHn = 0, kEpiH2 = 0;
        foreach (var e in kEpiS) { kEpiSn += e.w; if (e.v >= 2) kEpiS2 += e.w; }
        foreach (var e in kEpiH) { kEpiHn += e.w; if (e.v >= 2) kEpiH2 += e.w; }
        Console.WriteLine($"--- {fpK}　①'欠乏の持続（四半期ごとに走査・終わったエピソードのみ）飽和の落差 D>{fp0Sat:F3}：{kEpiS.Count}件・長さ p50 {FpS(FpQ(kEpiS, 0.50))} p90 {FpS(FpQ(kEpiS, 0.90))} 四半期・2四半期以上 {(kEpiSn > 0 ? kEpiS2 / kEpiSn : 0):P1}");
        Console.WriteLine($"      門超えの落差 D>{fp0Half:F3}：{kEpiH.Count}件・長さ p50 {FpS(FpQ(kEpiH, 0.50))} p90 {FpS(FpQ(kEpiH, 0.90))} 四半期・2四半期以上 {(kEpiHn > 0 ? kEpiH2 / kEpiHn : 0):P1}　（未終了のエピソードは数えない＝**下側に偏る**）");
        Console.WriteLine($"--- {fpK}　①''欠乏の梯子（四半期走査・人口加重・終わったエピソードのみ）　水準：pop×四半期の人口比<水準／件数／長さ p50・p90（四半期）／2四半期以上の比");
        double kLadTot = fpLadTot.TryGetValue(fpK, out var klt) ? klt : 0;
        for (int li = 0; li < fpLadder.Length; li++)
        {
            var kE = FpBag(fpLadEpi[li], fpK); double kEn = 0, kE2 = 0; foreach (var e in kE) { kEn += e.w; if (e.v >= 2) kE2 += e.w; }
            double kBelow = fpLadBelow.TryGetValue(fpK, out var klb) ? klb[li] : 0;
            Console.WriteLine($"      <{fpLadder[li]:F2}：人口比 {(kLadTot > 0 ? kBelow / kLadTot : 0),7:P1}／{kE.Count,6}件／p50 {FpS(FpQ(kE, 0.50)),6} p90 {FpS(FpQ(kE, 0.90)),6}／≥2Q {(kEn > 0 ? kE2 / kEn : 0),6:P1}");
        }
        Console.WriteLine($"--- {fpK}　②F1の熱（州×年 {kHeat.Count} 件・州1件=重み1）heat p50 {FpS(FpQ(kHeat, 0.50))} p90 {FpS(FpQ(kHeat, 0.90))} p99 {FpS(FpQ(kHeat, 0.99))}　門{cf0.RegionalRevoltThreshold:F2}超 {(kHeat.Count > 0 ? 1 - FpBelow(kHeat, cf0.RegionalRevoltThreshold) : 0):P1}／門{cf0.RegionalRevoltThreshold:F2}超 {(kHeat.Count > 0 ? 1 - FpBelow(kHeat, cf0.RegionalRevoltThreshold) : 0):P1}");
        Console.WriteLine($"      内訳 州平均闘争度 p50 {FpS(FpQ(kHMil, 0.50))} p90 {FpS(FpQ(kHMil, 0.90))} p99 {FpS(FpQ(kHMil, 0.99))}　★段2＝heat と同値（積をやめた）");
        Console.WriteLine($"      国×年のF1が評価する州の熱（{kHMax.Count}件・選択＝Politics.SelectRevoltRegion・鍵＝熱×群衆）p50 {FpS(FpQ(kHMax, 0.50))} p90 {FpS(FpQ(kHMax, 0.90))}　門{cf0.RegionalRevoltThreshold:F2}超 {(kHMax.Count > 0 ? 1 - FpBelow(kHMax, cf0.RegionalRevoltThreshold) : 0):P1}／門{cf0.RegionalRevoltThreshold:F2}超 {(kHMax.Count > 0 ? 1 - FpBelow(kHMax, cf0.RegionalRevoltThreshold) : 0):P1}");
        Console.WriteLine($"      最熱州の群衆 p10 {FpS(FpQ(kCrowd, 0.10))} p50 {FpS(FpQ(kCrowd, 0.50))} p90 {FpS(FpQ(kCrowd, 0.90))}　強度÷保持（>1 で譲歩枝）p50 {FpS(FpQ(kRatio, 0.50))} p90 {FpS(FpQ(kRatio, 0.90))} p99 {FpS(FpQ(kRatio, 0.99))}　>1 の割合 {(kRatio.Count > 0 ? 1 - FpBelow(kRatio, 1.0) : 0):P1}");
        Console.WriteLine($"--- {fpK}　③3層の重み（pop×年・人口加重・和は厳密に1）政治意識あり p50 {FpS(FpQ(kEdu, 0.50))} p90 {FpS(FpQ(kEdu, 0.90))}／中間 p50 {FpS(FpQ(kMid, 0.50))} p90 {FpS(FpQ(kMid, 0.90))}／文盲 p10 {FpS(FpQ(kIll, 0.10))} p50 {FpS(FpQ(kIll, 0.50))} p90 {FpS(FpQ(kIll, 0.90))}");
        // ★段3＝参加の述語の母集団（不満度／闘争度／その大きい方）。閾値0.5は「蜂起の門」であって「運動に数えられる下限」ではない＝両者の水準差をここで見る
        var kDisc = FpBag(fp0Disc, fpK); var kMil2 = FpBag(fp0Mil, fpK); var kPart = FpBag(fp0Part, fpK); var kPartSh = FpBag(fp0PartShare, fpK);
        double kTh = cf0.RegionalRevoltThreshold;
        Console.WriteLine($"--- {fpK}　④不満度（導出量・pop×年 {kDisc.Count}件・人口加重）p50 {FpS(FpQ(kDisc, 0.50))} p75 {FpS(FpQ(kDisc, 0.75))} p90 {FpS(FpQ(kDisc, 0.90))} p99 {FpS(FpQ(kDisc, 0.99))}　>{kTh:F2} の人口比 {(kDisc.Count > 0 ? 1 - FpBelow(kDisc, kTh) : 0):P2}");
        Console.WriteLine($"      闘争度（比較の腕＝現行の参加の述語）p50 {FpS(FpQ(kMil2, 0.50))} p90 {FpS(FpQ(kMil2, 0.90))} p99 {FpS(FpQ(kMil2, 0.99))}　>{kTh:F2} の人口比 {(kMil2.Count > 0 ? 1 - FpBelow(kMil2, kTh) : 0):P2}");
        Console.WriteLine($"      参加=max(不満度,闘争度) p50 {FpS(FpQ(kPart, 0.50))} p90 {FpS(FpQ(kPart, 0.90))} p99 {FpS(FpQ(kPart, 0.99))}　>{kTh:F2} の人口比 {(kPart.Count > 0 ? 1 - FpBelow(kPart, kTh) : 0):P2}　参加加重の人口比（国×年）p50 {FpS(FpQ(kPartSh, 0.50))} p90 {FpS(FpQ(kPartSh, 0.90))}");
    }
    {
        // ★回B（2026-09-22）の導出規則（旧起点と同じ形）：起点 = 観測国の落ち込み D の **p90 の中央値**（平時の pop の9割は駆動0）、
        //   飽和（駆動=1）= 起点 + 1/利得 を **p95 の中央値**に置く → 利得 = 1/(p95中央値 − p90中央値)。導出の腕は POB_COEF="FamineDriveGain=0"（旧の導出も駆動が休眠の世界）。
        static double Med(List<double> xs) { if (xs.Count == 0) return double.NaN; var t = new List<double>(xs); t.Sort(); return t.Count % 2 == 1 ? t[t.Count / 2] : (t[t.Count / 2 - 1] + t[t.Count / 2]) / 2; }
        double m90 = Med(fp0DerivP90), m95 = Med(fp0DerivP95);
        Console.WriteLine($"--- ★導出（回B）：落ち込み D の p90＝[{string.Join(" ", fp0DerivP90.Select(x => x.ToString("F4")))}] → 中央値 {m90:F4}／p95＝[{string.Join(" ", fp0DerivP95.Select(x => x.ToString("F4")))}] → 中央値 {m95:F4}"
            + $"　⇒ 起点 {m90:F4}・利得 1/({m95:F4}−{m90:F4}) = {(m95 > m90 ? 1.0 / (m95 - m90) : double.NaN):F2}　（この走行の係数：起点 {Fp0Anchor:F3}・利得 {Fp0Gain:F2}）");
    }
    Console.WriteLine($"--- ⚠起点 {Fp0Anchor:F3} は**落ち込みの量**（FamineDropOnset＝平常 FoodRef からの落差・回B 2026-09-22）＝充足の水準ではない。FamineDeathFillFloor(0.8・飢饉死の集中) とも 天命 dmFood/飢えの記憶/SelfPlay の飢饉判定（国平均 DynMandateFoodAnchor=0.8）とも別の量＝混ぜない");
    Console.WriteLine($"--- ★段2実施済み：heat は闘争度1量・門は {cf0.RegionalRevoltThreshold:F2}・勢い係数 RegionalRevoltStrength={cf0.RegionalRevoltStrength}（消えた欠乏因子の実測中央値≈0.5 を移した）。上の「>1 の割合」が鎮圧枝の到達性の番人");
    Console.WriteLine($"--- 段0 計器の限界（自己申告）：①pop の FoodMet/FoodNeed は月次の累積器（EconomyStep が月頭に0で始める）＝四半期末に読むのは**その四半期の3月目**だけ（月内の欠乏の谷は見えない）。断面の分位（①②③）は**年1回**（第4四半期）の走査＝①'の持続だけが四半期ごと。");
    Console.WriteLine($"    ②p.Size≤1e-9 の pop を分布から外している（力学の闘争度ループは外さない＝重みゼロなので加重分位は同値だが件数は減る）。");
    Console.WriteLine($"    ③heat の分位は州1件=重み1（F1が州を単位に選ぶため）＝人口加重ではない。④助走（StartWarmup）後の {fpQ} 四半期のみ＝助走中の盤面は入らない。");
    Console.WriteLine($"    ⑥①''の梯子は固定9水準（0.40〜0.80）・終わったエピソードだけ（未終了は数えない＝下側に偏る）・pop の同一性は参照＝消えた pop の走行中エピソードは捨てる。人口比は四半期断面（①の年1回より密）＝①の分位とは母集団が違う。");
    Console.WriteLine($"    ⑤「選択」の州＝エンジンと同じ Politics.SelectRevoltRegion（鍵＝熱×群衆）。群衆/強度/保持/比の分布はこの州のもの（旧の「最熱州」の計器の分布とは母集団が違う＝2026-09-11以前の記録と比べない）。");
    Console.WriteLine($"    ⑦①d の落ち込み D は四半期末の FoodRef（更新後）を慣れの速度 LivingRefSpeed で**更新前へ代数で戻した**値（充足は次の月頭まで不変）。未捕捉（FoodRef<0）と FoodNeed=0 の pop は外す。①'の持続も同じ D で数える（利得0の腕では飽和・門超えの落差が無い＝数えない）。");
    fpIntensity.Sort();
    string Pct(double pp) => fpIntensity.Count == 0 ? "—" : fpIntensity[System.Math.Clamp((int)(pp * (fpIntensity.Count - 1)), 0, fpIntensity.Count - 1)].ToString("F4");
    Console.WriteLine($"\n--- intensity の分布（候補{fpIntensity.Count}件・段4の RevoltIgnitionIntensity はここから同定する）");
    Console.WriteLine($"    中央値 {Pct(0.50)}／p75 {Pct(0.75)}／p90 {Pct(0.90)}／p95 {Pct(0.95)}／p99 {Pct(0.99)}／最大 {(fpIntensity.Count > 0 ? fpIntensity[^1].ToString("F4") : "—")}");
    // ★閾値の同定（分位だけでは頻度が決まらない＝門2との連言で決まる）：国×年ごとに「門2を越えた候補の意志の最大値」を取り、
    //   θ を超えた年＝その年に点火**しうる**年。史実帯＝列強1国あたり131年で8〜18件（§2）。
    //   ⚠これは**上限**＝実際の点火は四半期評価・1国1件・共有クールダウン・決着枝（受諾/別軸譲歩）で必ずこれより減る。
    Console.WriteLine($"--- 閾値の同定（国×年 {fpEligible.Count}件が分母・門2＝州の絶望>{cf0.RegionalRevoltThreshold:F2} を既に越えている候補だけを数える）");
    foreach (double th in new[] { 0.05, 0.10, 0.15, 0.20, 0.25, 0.30, 0.35, 0.40, 0.45, 0.50, 0.60, 0.70 })
    {
        int yrs = 0; foreach (double m in fpEligible) if (m > th) yrs++;
        Console.WriteLine($"    θ={th:F2}  点火しうる国×年 {yrs,5}／{fpEligible.Count,5}　＝1国131年あたり {(fpEligible.Count > 0 ? yrs / (double)fpEligible.Count * 131.0 : 0),6:F1}件（史実帯 8〜18）");
    }
    Console.WriteLine($"--- 国の出入り：建国 {fpBirths}回／消滅 {fpDeaths}回（初期{fpN0}→終局{fm.Nations.Count}・純増{fm.Nations.Count - fpN0}＝旧計器はこの純増しか出せなかった）");
    Console.WriteLine($"--- 建国時の州数：{(fpFoundRegions.Count > 0 ? $"1州 {fpFoundRegions.FindAll(x => x == 1).Count}回／2州以上 {fpFoundRegions.FindAll(x => x > 1).Count}回／最大 {fpFoundRegions.Max()}州" : "建国なし")}");
    Console.WriteLine($"--- 同時に立っている反乱国の最大数：{fpMaxRebels}（終局時 {fpRebelBorn.Count}）");
    Console.WriteLine($"--- F3b：蜂起（4枝＝州に拠って立った／分離の内乱／政体戦／要求戦）{fpRevolt}回／内戦の宣言 {fpCwOpen}回・分離独立戦争 {fpSecOpen}回／反乱側の勝利 {fpRebelWin}回・親の勝利（再併合）{fpParentWin}回・残骸の平和的復帰 {fpRump}回");
    Console.WriteLine($"--- F3b 危機の行き先：受諾 {fpF3Concede}回／別の軸で譲歩 {fpF3Compensate}回／蜂起 {fpRevolt}回（＝危機の総数 {fpF3Concede + fpF3Compensate + fpRevolt}回。無血鎮圧の枝は2026-09-08に撤去＝鎮圧の実体は蜂起→内戦→再併合）／通知（要求を掲げた）{fpF3Notify}回");
    Console.WriteLine($"    成功率＝{(fpRebelWin + fpParentWin > 0 ? $"{(double)fpRebelWin / (fpRebelWin + fpParentWin):P0}" : "決着なし")}（史実帯15〜25%）");
    Console.WriteLine($"--- F1 州一揆：鎮圧 {fpF1Sup}回／譲歩 {fpF1Con}回");
    return;
}

// === 物価水準（インフレ/デフレ）の挙動を実測： dotnet run -- pricelevel [seed] ===
// WorldData の全ロスター×125年で各国の物価水準の最小/最大/最終を列挙＋プレイヤー国の年次推移（好況＝インフレ・恐慌＝デフレが出るか）。
if (args.Length > 0 && args[0] == "pricelevel")
{
    ulong seed = args.Length > 1 && ulong.TryParse(args[1], out var s) ? s : 1789UL;
    NationConfig[] pc = WorldData.Nations();
    var pm = new MultiNationSim(pc, seed, _ => new UtilityAgent());
    pm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    int n = pm.Nations.Count;
    double[] min = new double[n], max = new double[n];
    for (int i = 0; i < n; i++) { min[i] = 9; max[i] = 0; }
    var traj = new System.Collections.Generic.List<string>();
    for (int q = 0; q < 500; q++)
    {
        pm.StepQuarter();
        for (int i = 0; i < n; i++) { double p = pm.Nations[i].World.PriceLevel; min[i] = System.Math.Min(min[i], p); max[i] = System.Math.Max(max[i], p); }
        if (q % 20 == 19) traj.Add($"{1783 + q / 4}年 物価{pm.Nations[0].World.PriceLevel:F2} (与信過熱{pm.Nations[0].World.FinancialHeat:P0})");
    }
    Console.WriteLine($"=== pricelevel seed {seed}：{n}カ国125年の物価水準＋金本位 ===");
    for (int i = 0; i < n; i++)
    {
        var w = pm.Nations[i].World;
        string[] pn = { "保守", "自由", "社会" };
        static string Lv(ReformLevel l) => l == ReformLevel.Limited ? "(限定)" : "";
        string laws = (w.CornLaws != ReformLevel.None ? "穀物法" + Lv(w.CornLaws) + " " : "") + (w.LandReform != ReformLevel.None ? "土地改革" + Lv(w.LandReform) + " " : "") + (w.SerfEmancipation != ReformLevel.None ? "農奴解放" + Lv(w.SerfEmancipation) + " " : "") + $"労働{w.Policy.WorkHours}";
        Console.WriteLine($"  {w.SelfPower?.Name,-14} {w.Polity,-22} 与党{pn[w.GoverningParty]} 参政権{w.Suffrage} 法[{laws}]");
    }
    Console.WriteLine($"--- {pm.Nations[0].World.SelfPower?.Name} の物価推移（インフレ/デフレが信用循環と動くか）---");
    foreach (var t in traj) Console.WriteLine("  " + t);
    return;
}

// === 慢性赤字の正体：誰が貯蓄ゼロ床に張り付き借金で埋められているか： dotnet run -- deficit ===
if (args.Length > 0 && args[0] == "deficit")
{
    NationConfig[] dc = WorldData.Nations();
    var dm = new MultiNationSim(dc, 1789UL, _ => new UtilityAgent());
    dm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < 500; q++) dm.StepQuarter();
    // 全国の階級別：一人あたり貯蓄の中央値的な水準と「貯蓄ゼロ床（＝借金で底打ちされている＝慢性赤字）」の割合
    var byClass = new System.Collections.Generic.Dictionary<string, (double sumPcSav, double sumSize, int floored, int total)>();
    foreach (var n in dm.Nations)
        foreach (var r in n.World.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                if (p.Size < 1) continue;
                var k = p.ClassName;
                var v = byClass.GetValueOrDefault(k);
                double pc = p.Savings / p.Size;
                byClass[k] = (v.sumPcSav + pc * p.Size, v.sumSize + p.Size, v.floored + (p.Savings < 1.0 ? 1 : 0), v.total + 1);
            }
        }
    Console.WriteLine("階級           一人あたり貯蓄(加重平均)   貯蓄ゼロ床の州割合");
    foreach (var kv in byClass)
        Console.WriteLine($"  {kv.Key,-12} {(kv.Value.sumSize > 0 ? kv.Value.sumPcSav / kv.Value.sumSize : 0),12:F1}        {(double)kv.Value.floored / Math.Max(1, kv.Value.total),6:P0}（{kv.Value.floored}/{kv.Value.total}州）");
    Console.WriteLine("--- 国別 累積負債化(LossFinanced) と 国庫 ---");
    foreach (var n in dm.Nations)
        Console.WriteLine($"  {n.World.SelfPower?.Name,-8} LossFinanced{n.World.LossFinanced / 1000,9:F0}k  国庫{n.World.Treasury / 1000,8:F0}k  与信過熱{n.World.FinancialHeat:P0}");
    return;
}

// === 価格中間帯の時間推移：2年助走で適正化するか／構造的に頭打ちか： dotnet run -- pricetime ===
if (args.Length > 0 && args[0] == "pricetime")
{
    NationConfig[] tc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var tm = new MultiNationSim(tc, 1789UL, _ => new UtilityAgent());
    tm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    tm.WarmupEconomy(6);
    double MidRate()
    {
        double sh = 0, md = 0;
        foreach (var n in tm.Nations) foreach (var good in n.World.Region.Goods)
        {
            if (good.Tier == GoodTier.Service) continue;
            var mk = n.World.Region.Markets[good.Id];
            if (mk.LastDemand < 1.0) continue;
            double ratio = good.RefPrice > 1e-9 ? mk.Price / good.RefPrice : 1;
            sh++; if (ratio > 0.1 && ratio < 10) md++;
        }
        return sh > 0 ? md / sh : 0;
    }
    int done = 0;
    foreach (var (q, label) in new[] { (8, "助走2年(開始盤面)"), (16, "助走4年"), (24, "6年"), (44, "11年"), (84, "21年"), (204, "51年") })
    {
        for (; done < q; done++) tm.StepQuarter();
        Console.WriteLine($"{label,-18} 全国家の価格中間帯率 {MidRate():P0}");
    }
    return;
}

// === 電化の時期：技術軌跡と電力/電化製品の需要・供給を年次で： dotnet run -- electime ===
if (args.Length > 0 && args[0] == "electime")
{
    NationConfig[] ec = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var em = new MultiNationSim(ec, 1789UL, _ => new UtilityAgent());
    em.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    em.WarmupEconomy(6);
    Console.WriteLine("年    前線 | 各国 技術t/電力需要電/電化製品需要化");
    int done = 0;
    foreach (int q in new[] { 0, 8, 40, 80, 200, 320, 400 })
    {
        for (; done < q; done++) em.StepQuarter();
        double front = em.Nations.Max(n => n.World.TechLevel);
        var bits = em.Nations.Select(n =>
        {
            var w = n.World; var r = w.Region;
            return $"{w.SelfPower?.Name?.Substring(0, 2)}:t{w.TechLevel:F1}/電{r.Markets[Catalog.ElectricityId].LastDemand:F0}/化{r.Markets[Catalog.ApplianceId].LastDemand:F0}";
        });
        Console.WriteLine($"{em.Nations[0].World.Year}  {front:F2} | {string.Join("  ", bits)}");
    }
    return;
}

// === 移民プローブ：R3の距離減衰の実効（欧州→米集中）＋移民pop貯蓄スケール（運賃係数の較正）： dotnet run -- migprobe [seed] ===
if (args.Length > 0 && args[0] == "migprobe")
{
    ulong mseed = ArgUlong(args, 1, 1);
    var mpm = new MultiNationSim(WorldData.Nations(), mseed, _ => new UtilityAgent());
    mpm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    string[] watch = ["アメリカ", "イギリス", "フランス", "アイルランド", "清", "ロシア", "プロイセン"];
    void Row(int year)
    {
        Console.WriteLine($"--- {year}年 ---");
        foreach (var name in watch)
        {
            var s = mpm.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == name);
            if (s is null) continue;
            var w = s.World;
            // 農民(1)・労働者(2)の1人当たり貯蓄（運賃係数の目安）＝最良/最悪州の平均
            double pcSavPeasant = 0, pcSavWorker = 0; int rc = 0;
            foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; var pe = r.Pops.Find(x => x.ClassId == 1); var wo = r.Pops.Find(x => x.ClassId == 2); if (pe is { Size: > 1e-9 }) pcSavPeasant += pe.Savings / pe.Size; if (wo is { Size: > 1e-9 }) pcSavWorker += wo.Savings / wo.Size; rc++; }
            if (rc > 0) { pcSavPeasant /= rc; pcSavWorker /= rc; }
            Console.WriteLine($"  {name,-8} 人口{w.TotalPopulation,9:N0}  流入{w.LastImmigration,7:N0}/流出{w.LastEmigration,7:N0}  農民貯蓄/人{pcSavPeasant,7:F2}  労働者貯蓄/人{pcSavWorker,7:F2}");
        }
    }
    int mdone = 0;
    foreach (int q in new[] { 4, 100, 200, 320, 440 })   // 助走後・1784・1808・1833・1863・1893相当
    {
        for (; mdone < q; mdone++) mpm.StepQuarter();
        Row(1783 + q / 4);
    }
    return;
}

// === 技術水準→暦年の対応曲線（C-高8・2026-07-22）：前線国の TechLevel を5年ごとに出す。
// DefaultRecipeOnset の再配置に使う唯一の物差し（同一switch内に「1.3≈1850s」と「1.0≈1915」が同居する80年ずれの解消）。
// アンカー＝相対アンカー（TechInstitutionTests 公教育の自由国＝対照×1.3・床1.10）： dotnet run -- techcurve ===
if (args.Length > 0 && args[0] == "techcurve")
{
    var tcm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    Console.WriteLine("  年   前線t  前線国        英t    普t    米t    清t");
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        tcm.StepQuarter();
        if ((q + 1) % 20 != 0) continue;   // 5年ごと
        var front = tcm.Nations.OrderByDescending(n => n.World.TechLevel).First();
        double T(string nm) => tcm.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm)?.World.TechLevel ?? 0;
        Console.WriteLine($"{front.World.Year,4}  {front.World.TechLevel,5:F2}  {front.World.SelfPower?.BaseName,-10}  {T("イギリス"),5:F2}  {T("プロイセン"),5:F2}  {T("アメリカ"),5:F2}  {T("清"),5:F2}");
    }
    return;
}

// === 技術の助走盤面：経済の助走＋WarmupTech(k) で開始盤面の技術差を計測： dotnet run -- techboard ===
// 開始シーケンス（WorldData全ロスター）＝App も自己対戦バッチも通る MultiNationSim.StartWarmup と同一。反復kを変えて「団子解消／表示閾値0.3超え」の適正値を調律する。
// ★2026-08-10：経済の助走は暦も技術も止めるので、この表の k=0 行は「技術の助走を1度も回していない盤面」＝t0.00 が正しい（旧は経済助走の4四半期ぶんの GrowTech が混ざっていた）。
if (args.Length > 0 && args[0] == "techboard")
{
    var tbm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    tbm.WarmupEconomy(new Coefficients().StartWarmupQuarters);
    Console.WriteLine("反復  前線 |  上位5国（技術t・前線比%）        …  下位3国");
    int done = 0;
    foreach (int k in new[] { 0, 4, 8, 12, 16, 20, 24, 30, 40 })
    {
        for (; done < k; done++) tbm.WarmupTech(1);
        double front = tbm.Nations.Max(n => n.World.TechLevel);
        var ranked = tbm.Nations.OrderByDescending(n => n.World.TechLevel).ToList();
        string Fmt(Simulation n) { var w = n.World; double rel = front > 1e-9 ? w.TechLevel / front : 0; return $"{w.SelfPower?.BaseName}:t{w.TechLevel:F2}/{rel:P0}"; }
        string top = string.Join("  ", ranked.Take(5).Select(Fmt));
        string bot = string.Join("  ", ranked.TakeLast(3).Select(Fmt));
        Console.WriteLine($"{k,3}  {front:F2} | {top}  …  {bot}");
    }
    Console.WriteLine($"--- 主要プレイ国の前線比（現行の助走 StartWarmup＝経済{new Coefficients().StartWarmupQuarters}Q＋技術{new Coefficients().StartWarmupTechIterations}反復 時点） ---");
    var sm8 = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    sm8.StartWarmup();
    double fr = sm8.Nations.Max(n => n.World.TechLevel);
    foreach (var name in new[] { "イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "オスマン", "日本", "清" })
    {
        var s = sm8.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == name);
        if (s != null) Console.WriteLine($"  {name,-8} t{s.World.TechLevel:F2}  前線比{(fr > 1e-9 ? s.World.TechLevel / fr : 0):P0}");
    }
    return;
}

// === 連鎖トイ経済の価格軌跡（木5/木材2/鉄鉱石6/鉄3）：振動か固着か： dotnet run -- chaintrace ===
if (args.Length > 0 && args[0] == "chaintrace")
{
    var csim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.ProductionChain }, new NullAgent());
    var cm = csim.World.Regions[0].Markets;
    int[] watch = [5, 2, 6, 3, 4, 1];
    string[] nm = ["木", "木材", "鉄鉱石", "鉄", "鉄鋼", "家具"];
    var sum = new double[watch.Length];
    for (int d = 0; d < 400; d++)
    {
        csim.StepDay();   // ★是正（旧 Run(1)＝四半期刻みだったが、ラベル「d」「末尾40日」は日次軌跡を意図している）
        if (d >= 360)
        {
            Console.WriteLine($"d{d + 1,3}: " + string.Join("  ", watch.Select((g, i) => $"{nm[i]}{cm[g].Price,6:F3}")));
            for (int i = 0; i < watch.Length; i++) sum[i] += cm[watch[i]].Price;
        }
    }
    Console.WriteLine("--- 末尾40日の平均 ---");
    Console.WriteLine(string.Join("  ", watch.Select((g, i) => $"{nm[i]}{sum[i] / 40:F3}")));
    return;
}

// === 価格分布（床/中間/天井）：鎖国日本 vs 開国国の比較： dotnet run -- pricedist ===
if (args.Length > 0 && args[0] == "pricedist")
{
    NationConfig[] pc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var pm = new MultiNationSim(pc, 1789UL, _ => new UtilityAgent());
    pm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    pm.WarmupEconomy(6);
    for (int q = 0; q < 8; q++) pm.StepQuarter();
    foreach (var n in pm.Nations)
    {
        var w = n.World; var r = w.Region;
        int floor = 0, mid = 0, ceil = 0, shown = 0;
        foreach (var good in r.Goods)
        {
            if (good.Tier == GoodTier.Service) continue;
            var mkt = r.Markets[good.Id];
            if (mkt.LastDemand < 1.0) continue;
            double ratio = good.RefPrice > 1e-9 ? mkt.Price / good.RefPrice : 1;
            shown++;
            if (ratio <= 0.1) floor++; else if (ratio >= 10) ceil++; else mid++;
        }
        Console.WriteLine($"{w.SelfPower?.Name,-8} 関税{w.Tariff:F2} 表示{shown}財：床(≤10%){floor} 中間{mid} 天井(≥1000%){ceil} ＝中間率{(shown > 0 ? (double)mid / shown : 0):P0}");
    }
    return;
}

// === 天井張り付き財の正体： dotnet run -- ceiling ===（開始盤面で ratio≥1000% の財を需給つきで列挙）
if (args.Length > 0 && args[0] == "ceiling")
{
    NationConfig[] cc2 = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var m2 = new MultiNationSim(cc2, 1789UL, _ => new UtilityAgent());
    m2.WarmupEconomy(6);
    for (int q = 0; q < 8; q++) m2.StepQuarter();
    foreach (var n in m2.Nations)
    {
        var w = n.World; var r = w.Region;
        Console.WriteLine($"=== {w.SelfPower?.Name} {r.Name} ===");
        foreach (var good in r.Goods)
        {
            if (good.Tier == GoodTier.Service) continue;
            var mkt = r.Markets[good.Id];
            double ratio = good.RefPrice > 1e-9 ? mkt.Price / good.RefPrice : 1;
            if (ratio < 10) continue;
            double sup = mkt.LastProduced + mkt.LastImport;
            var pr = r.Producers.Find(x => x.GoodId == good.Id);
            Console.WriteLine($"  {good.Name,-8} 価格{ratio:P0} 需要{mkt.LastDemand,8:F1} 供給{sup,8:F1}(生{mkt.LastProduced,7:F1}+輸{mkt.LastImport,6:F1}) 販売{mkt.LastSold,8:F1} 在庫{mkt.Stock,7:F1} 生産者{(pr != null ? "有" : "無")} 入力数{Catalog.Goods[good.Id].Inputs.Length}");
        }
    }
    return;
}

// === 鋼工場の建設費（旧固定 vs 新・価格依存）の倍率を計測： dotnet run -- buildcost ===
if (args.Length > 0 && args[0] == "buildcost")
{
    var sc = new Scenario { Seed = 2025, StartYear = 1783, Kind = ScenarioKind.France, PoliticsEnabled = true, InitialPolity = Polity.Absolutist, FiscalEnabled = true, TaxRate = 0.10, InitialPollTax = 1.0, InitialTreasury = 200_000, BaseCoercion = 80, EducationLaw = EducationLaw.Religious, ExternalPressure = 0.1 };
    var bcsim = new Simulation(sc, new NullAgent());
    bcsim.World.Policy.CoercionTarget = 80;
    var bccoef = bcsim.World.Coef; var bcr = bcsim.World.Regions[0];
    bcsim.Run(40);
    Console.WriteLine($"着工前 Q40: 国庫{bcsim.World.Treasury / 1000:F0}k 鋼価格比{bcr.Markets[Catalog.SteelId].Price / Catalog.RefPrices[Catalog.SteelId]:P0}");
    Console.WriteLine($"着工: {bcsim.World.BuildFactory(0, Catalog.SteelId)}");
    var bcp = bcr.Producers.Find(x => x.GoodId == Catalog.SteelId)!;
    for (int q = 0; q < 36 && bcp.BuiltScale <= 0; q++)
    {
        bcsim.Run(1);
        Console.WriteLine($"  +Q{q + 1}: 国庫{bcsim.World.Treasury / 1000,6:F0}k 鋼価格比{bcr.Markets[Catalog.SteelId].Price / Catalog.RefPrices[Catalog.SteelId],6:P0} 残建設{bcp.ConstructionLeft,5:F0}日 棟数{bcp.BuiltScale:F2}");
    }
    Console.WriteLine($"完成時の棟数: {bcp.BuiltScale:F2}");
    return;
}

// === 新方式の開始盤面検証（退避→外交凍結→経済24助走→復元）： dotnet run -- freshboard ===
if (args.Length > 0 && args[0] == "freshboard")
{
    NationConfig[] fbc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var fbm = new MultiNationSim(fbc, 1789UL, _ => new UtilityAgent());
    var snaps = fbm.Nations.Select(n => GovernanceSnapshot.Capture(n.World)).ToArray();
    fbm.DiplomacyFrozen = true;
    fbm.WarmupEconomy(24);
    fbm.DiplomacyFrozen = false;
    for (int i = 0; i < fbm.Nations.Count; i++) snaps[i].RestoreTo(fbm.Nations[i].World);
    int warCount = fbm.Diplomacy.Wars.Count;
    Console.WriteLine($"復元後：戦争数{warCount}（外交凍結なら0が期待）");
    foreach (var n in fbm.Nations)
    {
        var w = n.World; var r = w.Region;
        int bid = r.CulturalGoodId >= 0 ? r.CulturalGoodId : Catalog.BreadId;
        double brp = r.Goods.First(x => x.Id == bid).RefPrice;
        double bread = brp > 1e-9 ? r.Markets[bid].Price / brp : 1;
        int ceil = 0; foreach (var good in r.Goods) { if (good.Tier == GoodTier.Service) continue; var mk = r.Markets[good.Id]; if (mk.LastDemand < 1) continue; bool f = Catalog.Goods[good.Id].Inputs.Length > 0 && !Catalog.Goods[good.Id].IsColonial; if (!f && mk.LastProduced + mk.LastImport < 0.05) continue; if ((good.RefPrice > 1e-9 ? mk.Price / good.RefPrice : 1) >= 19) ceil++; }
        Console.WriteLine($"  {w.SelfPower?.Name,-8} 政体{w.Polity,-22} 主食{bread,7:P0} 国庫{w.Treasury / 1000,7:F0}k 人頭税{w.PollTax:P0} 軍事費{w.MilitarySpending:P0} 土地税解禁{(w.LandTaxUnlocked ? "可" : "不")} 工業化{r.TotalIndustrialization:F2} 天井財{ceil,2}");
    }
    return;
}

// === 開始盤面の質：WarmupEconomy(6)＋AIプレイN四半期 で主食価格・国庫・工業化を計測： dotnet run -- startboard ===
if (args.Length > 0 && args[0] == "startboard")
{
    int[] warms = [6, 12, 16, 20, 24];
    foreach (int warm in warms)
    {
        int play = 8;
        NationConfig[] sbc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
        var sbm = new MultiNationSim(sbc, 1789UL, _ => new UtilityAgent());
        sbm.WarmupEconomy(warm);
        for (int q = 0; q < play; q++) sbm.StepQuarter();
        double breadMax = 0, treasMin = double.MaxValue, indMax = 0;
        int ceilTotal = 0;
        foreach (var n in sbm.Nations)
        {
            var w = n.World; var r = w.Region;
            int bid = r.CulturalGoodId >= 0 ? r.CulturalGoodId : Catalog.BreadId;
            double brp = r.Goods.First(x => x.Id == bid).RefPrice;
            breadMax = System.Math.Max(breadMax, brp > 1e-9 ? r.Markets[bid].Price / brp : 1);
            treasMin = System.Math.Min(treasMin, w.Treasury);
            indMax = System.Math.Max(indMax, r.TotalIndustrialization);
            foreach (var good in r.Goods)
            {
                if (good.Tier == GoodTier.Service) continue;
                var mkt = r.Markets[good.Id];
                if (mkt.LastDemand < 1.0) continue;
                bool factory = Catalog.Goods[good.Id].Inputs.Length > 0 && !Catalog.Goods[good.Id].IsColonial;
                if (!factory && mkt.LastProduced + mkt.LastImport < 0.05) continue;
                double ratio = good.RefPrice > 1e-9 ? mkt.Price / good.RefPrice : 1;
                if (ratio >= 19.0) ceilTotal++;
            }
        }
        Console.WriteLine($"経済助走{warm,3}Q＋AI{play}Q（総{warm + play}Q）  主食最高{breadMax,8:P0}  天井財計{ceilTotal,3}  最低国庫{treasMin / 1000,8:F0}k  最大工業化{indMax:F2}");
    }
    return;
}

// === 経済のみ長期助走で天井張り付きが解消する四半期数を計測： dotnet run -- warmsettle ===
if (args.Length > 0 && args[0] == "warmsettle")
{
    NationConfig[] wsc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var wsm = new MultiNationSim(wsc, 1789UL, _ => new UtilityAgent());
    int[] marks = [6, 10, 14, 18, 22, 26, 30, 36, 42];
    int done = 0;
    Console.WriteLine("助走Q  " + string.Join("  ", wsm.Nations.Select(n => $"{n.World.SelfPower?.Name,-6}")) + "  ／各国の天井(≥1900%)張り付き財数（表示対象＝需要≥1・供給可能財）");
    foreach (int q in marks)
    {
        for (; done < q; done++) wsm.WarmupEconomy(1);
        var counts = new List<string>();
        double breadMax = 0;
        foreach (var n in wsm.Nations)
        {
            var w = n.World; var r = w.Region; int ceil = 0;
            foreach (var good in r.Goods)
            {
                if (good.Tier == GoodTier.Service) continue;
                var mkt = r.Markets[good.Id];
                if (mkt.LastDemand < 1.0) continue;
                bool factory = Catalog.Goods[good.Id].Inputs.Length > 0 && !Catalog.Goods[good.Id].IsColonial;
                if (!factory && mkt.LastProduced + mkt.LastImport < 0.05) continue;
                double ratio = good.RefPrice > 1e-9 ? mkt.Price / good.RefPrice : 1;
                if (ratio >= 19.0) ceil++;
            }
            counts.Add($"{ceil,6}");
            int bid = r.CulturalGoodId >= 0 ? r.CulturalGoodId : Catalog.BreadId;
            double brp = r.Goods.First(x => x.Id == bid).RefPrice;
            breadMax = System.Math.Max(breadMax, brp > 1e-9 ? r.Markets[bid].Price / brp : 1);
        }
        var fr = wsm.Nations[0].World; var br = wsm.Nations[2].World;
        Console.WriteLine($"Q{q,3}   " + string.Join("  ", counts) + $"   主食最高{breadMax,7:P0}  仏工業化{fr.Region.TotalIndustrialization,5:F2} 英工業化{br.Region.TotalIndustrialization,5:F2}  仏国庫{fr.Treasury/1000,7:F0}k");
    }
    return;
}

// === 天井財の日次トレース： dotnet run -- ceiltrace <財名> ===
if (args.Length > 0 && args[0] == "ceiltrace")
{
    string target = args.Length > 1 ? args[1] : "鋼";
    NationConfig[] tc3 = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var m3 = new MultiNationSim(tc3, 1789UL, _ => new UtilityAgent());
    m3.WarmupEconomy(6);
    var w3 = m3.Nations[0].World; var r3 = w3.Region;
    int gid = -1; foreach (var gg in r3.Goods) if (gg.Name == target) { gid = gg.Id; break; }
    if (gid < 0) { Console.WriteLine($"財「{target}」が見つかりません"); return; }
    Console.WriteLine($"=== {w3.SelfPower?.Name} {r3.Name} の {target}(id{gid}) 参照価格{r3.Goods.First(x=>x.Id==gid).RefPrice:F2} ===");
    for (int q = 0; q < 8; q++)
    {
        for (int d = 0; d < 90; d++)
        {
            m3.StepDay();
            var mk = r3.Markets[gid];
            double rp = r3.Goods.First(x => x.Id == gid).RefPrice;
            if (d % 15 == 0 || mk.Price >= rp * 19)
                Console.WriteLine($"Q{q} d{d,2} 価格{(rp>1e-9?mk.Price/rp:1):P0} 需要{mk.LastDemand,7:F1} 供給{mk.LastProduced + mk.LastImport,7:F1} 在庫{mk.Stock,7:F1} 目標{mk.TargetStock,6:F1}");
        }
    }
    return;
}

// === 終局ランキングと44四半期延長： dotnet run -- ranking ===
if (args.Length > 0 && args[0] == "ranking")
{
    NationConfig[] rc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    void Show(MultiNationSim m)
    {
        var ranked = m.Nations.Select(n => n.World).OrderByDescending(NationalScore.Of).ToList();
        Console.WriteLine($"=== {m.Nations[0].World.Year}年 ランキング（seed1789）===");
        int rank = 1;
        foreach (var w in ranked)
        {
            Console.WriteLine($"  {rank,2} {w.SelfPower?.Name,-8} 総合{NationalScore.Of(w),6:F1} ｜軍{NationalScore.Military(w),5:F1} 富{NationalScore.Wealth(w),5:F1} 金融{NationalScore.Finance(w),5:F1}（工業{w.Region.TotalIndustrialization,4:F2} 満足{NationalScore.MeanSatisfaction(w),4:F2} 植民地{w.SelfPower?.Colonies ?? 0}）人口{w.TotalPopulation / 1000.0,5:F1}M 政体{PolJa(w.Polity)}");
            rank++;
        }
    }
    var rm = new MultiNationSim(rc, 1789UL, _ => new UtilityAgent());
    rm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < 400; q++) rm.StepQuarter();
    Show(rm);
    for (int q = 0; q < 44; q++) rm.StepQuarter();
    Show(rm);
    return;
}

// === 30seed平均順位（勝敗バランスの全体像）： dotnet run -- rankavg [seeds] ===
if (args.Length > 0 && args[0] == "rankavg")
{
    int seeds = ArgInt(args, 1, 30);
    var rankSum = new Dictionary<string, double>(); var wins = new Dictionary<string, int>();
    var sc = new Dictionary<string, double>(); var mil = new Dictionary<string, double>(); var gdp = new Dictionary<string, double>();
    var ind = new Dictionary<string, double>(); var sat = new Dictionary<string, double>(); var col = new Dictionary<string, double>(); var cnt = new Dictionary<string, int>();
    for (ulong sd = 1; sd <= (ulong)seeds; sd++)
    {
        Console.Error.WriteLine($"  seed {sd}...");   // 進捗（ハングseed特定用）
        var r = PriceOfBread.SimCore.SelfPlayMatch.Run(sd);
        var ranked = r.Standings.OrderByDescending(s => s.Score).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            var s = ranked[i];
            string k = s.Nation.Length > 0 ? s.Nation : s.Name;   // 不変国名鍵（旧＝首都州名は割譲・遷都・変名で同一国が複数キーに分裂）
            rankSum[k] = rankSum.GetValueOrDefault(k) + (i + 1);
            if (i == 0) wins[k] = wins.GetValueOrDefault(k) + 1;
            sc[k] = sc.GetValueOrDefault(k) + s.Score; mil[k] = mil.GetValueOrDefault(k) + s.Military;
            gdp[k] = gdp.GetValueOrDefault(k) + s.Gdp; ind[k] = ind.GetValueOrDefault(k) + s.Industrialization;
            sat[k] = sat.GetValueOrDefault(k) + s.Satisfaction; col[k] = col.GetValueOrDefault(k) + s.Colonies; cnt[k] = cnt.GetValueOrDefault(k) + 1;
        }
    }
    Console.WriteLine($"=== {seeds}seed 平均順位（国力スコア順・{rankSum.Count}国）===");
    Console.WriteLine($"  {"国",-10} 平均順位 勝率  平均スコア ｜ 軍   富(GDP) 工業 満足 植民地");
    foreach (var kv in rankSum.OrderBy(kv => kv.Value / cnt[kv.Key]))
    {
        string nm = kv.Key; int c = cnt[nm];
        Console.WriteLine($"  {nm,-10} {kv.Value / c,6:F2}  {100.0 * wins.GetValueOrDefault(nm) / c,4:F0}% {sc[nm] / c,8:F1} ｜{mil[nm] / c,5:F1} {gdp[nm] / c,7:F0} {ind[nm] / c,4:F2} {sat[nm] / c,4:F2} {col[nm] / c,4:F1}");
    }
    return;
}

// （貨幣保存の漏れ探し drift は 2026-09-26 に削除＝鋳造と不換発行の項を欠き鋳造のある世界で偽の漏れを報告した・leakhunt が同じ役を持つ・fis-19）

// === 助走（政治凍結）の検証： dotnet run -- warmtest ===
if (args.Length > 0 && args[0] == "warmtest")
{
    NationConfig[] wc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var pc = wc[0];   // 仏＝プレイヤー（NullAgent）＝UIと同一
    var wm = new MultiNationSim(wc, 1789UL, c => ReferenceEquals(c, pc) ? new NullAgent() : new UtilityAgent());
    wm.Diplomacy.Powers[0].IsPlayer = true;
    Console.WriteLine($"前: 仏={wm.Nations[0].World.Polity}/{wm.Nations[0].World.EducationLaw}");
    wm.WarmupEconomy(6);
    Console.WriteLine($"後(凍結6Q): 仏={wm.Nations[0].World.Polity}/{wm.Nations[0].World.EducationLaw}");
    return;
}

// === 開始前の助走の診断（作者裁定2026-08-10「助走をプレイヤーの世界と自己対戦バッチで統一し、延長する」）：
//     dotnet run -- warmupprobe [seed] [観測四半期=8] [助走長リスト=0,4,20,40]
//     ①助走が暦を進めていないこと（開始が1783/Q1のままであること）を全国で検算し、
//     ②助走長ごとに「開幕直後の価格の荒れ」を数値化する（帯そのものは measure.sh でメインが測る＝ここは単走の診断）。
//     荒れの物差し＝首都市場の実勢財バスケットについて、四半期ごとの |ln(価格比の変化)| の平均（%／四半期）。
//     併せて開幕時点の参照価格からの乖離（|ln(価格/参照価格)| の平均）と、安全網（床/天井）に張り付いた財数を出す。 ===
if (args.Length > 0 && args[0] == "warmupprobe")
{
    ulong wpSeed = args.Length > 1 && ulong.TryParse(args[1], out var wps) ? wps : 1789UL;
    int wpObs = ArgInt(args, 2, 8);
    int[] wpLens = args.Length > 3
        ? [.. args[3].Split(',').Select(s => int.Parse(s.Trim()))]
        : [0, 4, 20, 40];

    // 助走が価格の荒れを測る対象＝首都市場の「実勢のある財」（需要があり、実際に供給されうる財）。
    // 全助走長で同じ籠を比べるため、籠は各国の助走直後の断面で決めて固定する。
    static List<int> Basket(Region r)
    {
        var ids = new List<int>();
        foreach (var g in r.Goods)
        {
            if (g.Tier == GoodTier.Service) continue;
            var mk = r.Markets[g.Id];
            if (mk.LastDemand < 1.0) continue;
            bool factory = Catalog.Goods[g.Id].Inputs.Length > 0 && !Catalog.Goods[g.Id].IsColonial;
            if (!factory && mk.LastProduced + mk.LastImport < 0.05) continue;
            if (g.RefPrice <= 1e-9 || mk.Price <= 1e-9) continue;
            ids.Add(g.Id);
        }
        return ids;
    }
    static double Ratio(Region r, int gid)
    {
        double rp = 0; foreach (var g in r.Goods) if (g.Id == gid) { rp = g.RefPrice; break; }
        return rp > 1e-9 ? r.Markets[gid].Price / rp : double.NaN;
    }

    Console.WriteLine($"=== 開始前の助走の診断（seed {wpSeed}／観測 {wpObs}四半期／StartWarmupQuarters の既定＝{new Coefficients().StartWarmupQuarters}） ===");
    {   // 参考：暦を止めなかった場合（2026-08-10以前の助走）にエンジンの暦がどこまで進むか＝素の四半期送りで再現
        var refSim = new MultiNationSim(WorldData.Nations(), wpSeed, _ => new UtilityAgent());
        for (int q = 0; q < 4; q++) refSim.StepQuarter();
        var rw = refSim.Nations[0].World;
        Console.WriteLine($"参考｜暦を止めない4四半期（旧助走と同じ暦の進み方）＝ {rw.Year}年Q{rw.Quarter} Tick{rw.Tick}　★表示暦は1783/01/01のまま＝旧はここで1年ずれていた");
    }

    foreach (int wpLen in wpLens)
    {
        var wpCfgs = WorldData.Nations();
        var wpPcfg = wpCfgs[0];   // プレイヤー国＝UI と同一の構成（NullAgent＋IsPlayer）
        var wpSim = new MultiNationSim(wpCfgs, wpSeed, c => ReferenceEquals(c, wpPcfg) ? new NullAgent() : new UtilityAgent());
        wpSim.Diplomacy.Powers[0].IsPlayer = true;
        if (wpLen > 0) wpSim.StartWarmup(null, wpLen);

        // ① 暦の検算（全国）
        int yMin = int.MaxValue, yMax = int.MinValue, qMin = int.MaxValue, qMax = int.MinValue; long tMax = 0;
        foreach (var n in wpSim.Nations)
        {
            yMin = Math.Min(yMin, n.World.Year); yMax = Math.Max(yMax, n.World.Year);
            qMin = Math.Min(qMin, n.World.Quarter); qMax = Math.Max(qMax, n.World.Quarter);
            tMax = Math.Max(tMax, n.World.Tick);
        }
        double popSum = 0, frontier = 0, indMax = 0, treasMin = double.MaxValue, invSav = 0;
        foreach (var n in wpSim.Nations)
        {
            popSum += n.World.TotalPopulation;
            frontier = Math.Max(frontier, n.World.TechLevel);
            indMax = Math.Max(indMax, n.World.MeanIndustrialization);   // 助走で「時間なしに工業化が進む」度合いの見張り（延長の主な副作用）
            treasMin = Math.Min(treasMin, n.World.Treasury);
            foreach (var rr in n.World.Regions)   // 投資家の貯蓄の総額＝「建設を止めたら金が積み上がって解凍直後にバーストしないか」の見張り
                if (rr.State == RegionState.Active && rr.InvestorPopIndex >= 0 && rr.InvestorPopIndex < rr.Pops.Count)
                    invSav += Math.Max(0, rr.Pops[rr.InvestorPopIndex].Savings);
        }

        // ② 助走直後の断面（参照価格からの乖離・安全網の張り付き）＋以後 wpObs 四半期の荒れ
        var wpBaskets = wpSim.Nations.Select(n => Basket(n.World.Region)).ToList();
        var wpPrev = new List<double[]>();
        for (int i = 0; i < wpSim.Nations.Count; i++)
            wpPrev.Add([.. wpBaskets[i].Select(g => Ratio(wpSim.Nations[i].World.Region, g))]);
        var wpMove = wpBaskets.Select(b => new double[b.Count]).ToList();
        double wpDev = 0; int wpDevN = 0, wpStuck = 0;
        for (int i = 0; i < wpSim.Nations.Count; i++)
            foreach (double v in wpPrev[i])
            {
                if (!double.IsFinite(v) || v <= 0) continue;
                wpDev += Math.Abs(Math.Log(v)); wpDevN++;
                if (v >= 19.0 || v <= 0.05) wpStuck++;
            }

        // 建材4財（鋼・機械・セメント・製材）の**助走直後**の価格（参照価格比・全モデル国の中央値）。
        // 資本形成を止めても**建設需要は残している**ことの検算＝需要が抜けていればこの4財だけ比が下振れする（作者の懸念）。
        var wpCoef = new Coefficients();
        string WpMat(int gid)
        {
            var vals = new List<double>();
            foreach (var n in wpSim.Nations)
            {
                var rr = n.World.Region;
                if (gid >= rr.Markets.Length) continue;
                double rp = 0; foreach (var g in rr.Goods) if (g.Id == gid) { rp = g.RefPrice; break; }
                if (rp > 1e-9 && rr.Markets[gid].Price > 1e-9) vals.Add(rr.Markets[gid].Price / rp);
            }
            if (vals.Count == 0) return "   －";
            vals.Sort();
            return $"{vals[vals.Count / 2],5:P0}";
        }
        string wpMats = $"鋼{WpMat(wpCoef.SteelGoodId)} 機械{WpMat(wpCoef.MachineGoodId)} セメント{WpMat(wpCoef.CementGoodId)} 製材{WpMat(wpCoef.LumberGoodId)}";

        for (int q = 0; q < wpObs; q++)
        {
            wpSim.StepQuarter();
            for (int i = 0; i < wpBaskets.Count && i < wpSim.Nations.Count; i++)
                for (int k = 0; k < wpBaskets[i].Count; k++)
                {
                    double now = Ratio(wpSim.Nations[i].World.Region, wpBaskets[i][k]);
                    double was = wpPrev[i][k];
                    if (double.IsFinite(now) && now > 0 && double.IsFinite(was) && was > 0)
                        wpMove[i][k] += Math.Abs(Math.Log(now / was));
                    wpPrev[i][k] = now;
                }
        }
        // 荒れ＝1四半期あたりの平均対数変化。国0（プレイヤー）と全国平均の両方を出す。
        static double WpMean(double[] a, int n) { if (a.Length == 0 || n <= 0) return 0; double s = 0; foreach (double v in a) s += v; return s / a.Length / n; }
        double wpP0 = WpMean(wpMove[0], wpObs);
        double wpAll = 0; int wpAllN = 0;
        for (int i = 0; i < wpMove.Count; i++) { if (wpMove[i].Length == 0) continue; wpAll += WpMean(wpMove[i], wpObs); wpAllN++; }
        wpAll = wpAllN > 0 ? wpAll / wpAllN : 0;
        // 国0で最も荒れた財（上位5）
        var wpWorst = Enumerable.Range(0, wpBaskets[0].Count)
            .OrderByDescending(k => wpMove[0][k])
            .Take(5)
            .Select(k => $"{Catalog.Goods[wpBaskets[0][k]].Name}{wpMove[0][k] / wpObs:P0}");

        // 解凍後の反動の見張り：観測期間（本編）を走らせた後の工業化最大＝助走で貯めた金が一気に建設へ流れていないか。
        double indAfter = 0;
        foreach (var n in wpSim.Nations) indAfter = Math.Max(indAfter, n.World.MeanIndustrialization);

        string wpLabel = wpLen == 0 ? "助走なし   " : $"助走{wpLen,3}Q({wpLen / 4.0,4:F1}年)";
        Console.WriteLine($"{wpLabel}｜暦 {yMin}〜{yMax}年 Q{qMin}〜{qMax} Tick{tMax,4}　人口計{popSum,10:F1} 技術前線{frontier,5:F3} 工業化最大{indMax,5:F2}→{wpObs}Q後{indAfter,5:F2} 投資家貯蓄{invSav / 1000,9:F0}k 最低国庫{treasMin / 1000,7:F0}k　"
                        + $"建材[{wpMats}]　"
                        + $"開幕乖離{(wpDevN > 0 ? wpDev / wpDevN : 0),6:F3} 張付{wpStuck,3}財　"
                        + $"荒れ/四半期 {wpSim.Nations[0].World.SelfPower?.Name}{wpP0,7:P1} 全国平均{wpAll,7:P1}　最荒[{string.Join(" ", wpWorst)}]");
    }
    return;
}

// === 都市失業の追跡（全7か国・UIと同じ構成）： dotnet run -- unemp ===
if (args.Length > 0 && args[0] == "unemp")
{
    NationConfig[] uc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var um = new MultiNationSim(uc, 1789UL, _ => new UtilityAgent());
    um.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    int[] marks = [4, 12, 40, 120];   // 経過四半期（1年, 3年, 10年, 30年）
    int mi = 0;
    Console.WriteLine("年    " + string.Join("  ", uc.Select(c => $"{c.Name,-6}")));
    for (int q = 1; q <= 120; q++)
    {
        um.StepQuarter();
        if (mi < marks.Length && q == marks[mi])
        {
            mi++;
            var cols = um.Nations.Select(n =>
            {
                double u = UnemploymentOf(n.World.Region, Catalog.LaborId);
                double ind = n.World.Region.TotalIndustrialization;
                return $"{u,5:P0}/{ind:F1}";
            });
            Console.WriteLine($"{um.Nations[0].World.Year}  " + string.Join("  ", cols) + "   （失業/工業化）");
        }
    }
    return;
}

// === 給与駆動の昇格の追跡： dotnet run -- wages [quarters] ===
if (args.Length > 0 && args[0] == "wages")
{
    int wq = ArgInt(args, 1, 240);
    var wsim = new Simulation(NationConfig.Britain().ToScenario(59), new NullAgent());
    var wr = wsim.World.Region;
    for (int q = 0; q < wq; q++)
    {
        wsim.StepQuarter();
        if (q % 20 != 19) continue;
        var wk = wr.Pops[2];   // 工場労働者
        string row = $"Q{q + 1,3} 労働者所得/人 {(wk.Size > 1 ? wk.Earned / wk.Size : 0),7:F3}";
        foreach (int idx in (int[])[4, 7, 8, 9])   // 技師・教師・官僚・医者
        {
            var t = wr.Pops[idx];
            row += $"　{t.ClassName}: 規模{t.Size,6:F0} 所得/人{(t.Size > 0.01 ? t.Earned / t.Size : 0),7:F3} 貯蓄/人{(t.Size > 0.01 ? t.Savings / t.Size : 0),8:F1}";
        }
        Console.WriteLine(row);
    }
    var eduW = wr.Markets[Catalog.EducationId];
    var wk2 = wr.Pops[2];
    Console.WriteLine($"教育: 価格{eduW.Price:F2} 生産{eduW.LastProduced:F1} 販売{eduW.LastSold:F1} 需要{eduW.LastDemand:F1}");
    Console.WriteLine($"労働者: 子供比率{wk2.AgeShare(0, 15):F2} 教育Fill{wk2.EducationFill:F2} 識字{wk2.Literacy:P0}　農民: 子供比率{wr.Pops[1].AgeShare(0, 15):F2} 教育Fill{wr.Pops[1].EducationFill:F2} 識字{wr.Pops[1].Literacy:P0}");
    foreach (var pr in wr.Producers)
        if (pr.GoodId == Catalog.EducationId)
            Console.WriteLine($"教育生産者: 労働pop={wr.Pops[pr.LaborPopIndex].ClassName} 産出{pr.Output:F1} 操業{pr.Scale:F2} 直近売上{pr.LastRevenue:F1}");
    return;
}

// === 全役職（POP階級）の機能チェック：各階級の規模・所得・地代・役割： dotnet run -- jobs [quarters] ===
if (args.Length > 0 && args[0] == "jobs")
{
    int jq = ArgInt(args, 1, 80);
    NationConfig[] jc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var jm = new MultiNationSim(jc, 1789UL, _ => new UtilityAgent());
    jm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    jm.WarmupEconomy(6);
    for (int q = 0; q < jq; q++) jm.StepQuarter();
    foreach (var n in jm.Nations)
    {
        var w = n.World; var r = w.Region;
        double tot = r.SumPopulation();
        Console.WriteLine($"=== {w.SelfPower?.Name} {w.Year}年 政体{w.Polity} ===");
        for (int i = 0; i < r.Pops.Count; i++)
        {
            var p = r.Pops[i];
            if (p.Size < 0.5) { Console.WriteLine($"  {p.ClassName,-8} 規模ほぼ0（不在）"); continue; }
            double incPer = p.Earned / p.Size;
            double rentPer = p.LandRent / p.Size;
            double savPer = p.Savings / p.Size;
            var staffs = r.Producers.Where(x => x.LaborPopIndex == i && x.Output > 0.5).Select(x => Catalog.Goods[x.GoodId].Name).Distinct().ToList();
            bool owns = r.Producers.Any(x => x.OwnerPopIndex == i && x.Output > 0.5);
            string role = staffs.Count > 0 ? string.Join("/", staffs) : (owns ? "(所有=地代/利潤)" : "(無役)");
            Console.WriteLine($"  {p.ClassName,-8} 規模{p.Size / tot,5:P1} 所得/人{incPer,8:F3} 地代/人{rentPer,7:F3} 貯蓄/人{savPer,9:F0} 失業{p.UnemploymentRate,5:P0} 満足{p.Satisfaction,4:P0} 役割:{role}");
        }
    }
    return;
}

// === 役務経済の診断： dotnet run -- services [quarters] ===
if (args.Length > 0 && args[0] == "services")
{
    int sq = ArgInt(args, 1, 300);
    NationConfig[] sc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];
    var sim4 = new MultiNationSim(sc, seed: 1789, _ => new UtilityAgent());   // 統治AI＝実プレイに近い診断
    sim4.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    var lastPol = sim4.Nations.Select(n => n.World.Polity).ToArray();
    for (int q = 0; q < sq; q++)
    {
        sim4.StepQuarter();
        for (int i = 0; i < sim4.Nations.Count; i++)
            if (sim4.Nations[i].World.Polity != lastPol[i])
            {
                var w2 = sim4.Nations[i].World;
                Console.WriteLine($"  政変 {w2.Year}年 {w2.Region.Name}: {lastPol[i]} → {w2.Polity}（大衆闘争度 {Politics.AvgMassMilitancy(w2):F2}）");   // ★sweep-13：述語の写しをやめエンジンの1本へ
                lastPol[i] = w2.Polity;
            }
        if (q % 4 == 3)   // パリの年次トレース（恐慌→政治の較正計測）
        {
            var wp = sim4.Nations[0].World;
            var rp = wp.Region;
            double mil2 = Politics.AvgMassMilitancy(wp), satM2 = NationalScore.MeanSatisfaction(wp);   // ★sweep-13：大衆の平均はエンジンの1本（闘争度・満足度とも同じ母集団）
            var lm = rp.Markets[Catalog.LaborId];
            double unemp = UnemploymentOf(rp, Catalog.LaborId);
            var bk = wp.Bank;
            Console.WriteLine($"  巴里 {wp.Year} 闘争{mil2:F2} 満足{satM2:F2} 失業{unemp:P0} 不作{wp.HarvestFactor:F2} パン価格{rp.Markets[Catalog.BreadId].Price:F2} 賃金{lm.Price:F2}" +
                (bk is null ? "" : $" 信用{bk.Loans:F0}/金{bk.Gold:F0} 過熱{wp.FinancialHeat:F2} 信頼{bk.Confidence:F2} 金利{wp.InterestRate:F2}"));
        }
    }
    Console.WriteLine($"=== 役務経済 診断 Q{sq} ===");
    foreach (var n in sim4.Nations)
    {
        var w = n.World;
        var r0 = w.Region;
        double pop = w.TotalPopulation;
        double lit = 0, ssz = 0;
        foreach (var p in w.AllPops()) { lit += p.Literacy * p.Size; ssz += p.Size; }
        var laborM = r0.Markets[Catalog.LaborId];
        var eduM = r0.Markets[Catalog.EducationId];
        var medM = r0.Markets[Catalog.HealthcareId];
        double humanL = 0, steamL = 0, elecL = 0;
        foreach (var p in r0.Producers)
            if (p.GoodId == Catalog.LaborId)
            {
                if (p.Inputs.Count == 0) humanL += p.Output;
                else if (p.RecipeName == "蒸気機関") steamL += p.Output;
                else elecL += p.Output;
            }
        double massMil = Politics.AvgMassMilitancy(w);   // ★sweep-13：述語の写しをやめエンジンの1本へ
        Console.WriteLine($"\n--- {r0.Name}（{w.EducationLaw}/{w.Polity}/自由{w.Freedom:F1}） 技術{w.TechLevel:F2} 識字{lit / ssz:P0} 満足{NationalScore.MeanSatisfaction(w):F2} 国庫{w.Treasury:F0} 行政効率{w.AdminEfficiency:P0} 治安{w.PublicOrder:P0}（犯罪率{w.CrimeRate:P2}） 大衆闘争度{massMil:F2}");
        var advM = r0.Markets[Catalog.AdvLaborId];
        var farmM = r0.Markets[Catalog.FarmLaborId];
        var facts = r0.Producers.Where(p => p.BuysLabor).ToList();
        double elecFill = facts.Count > 0 ? facts.Sum(p => p.ElecFill * p.Output) / Math.Max(1e-9, facts.Sum(p => p.Output)) : 0;
        Console.WriteLine($"  労働: 都市{laborM.Price:F2}（失業 {UnemploymentOf(r0, Catalog.LaborId):P0}） 農村{farmM.Price:F2}（失業 {UnemploymentOf(r0, Catalog.FarmLaborId):P0}） 技師{advM.Price:F2}");
        Console.WriteLine($"  電化: 平均電化率{elecFill:P0} 送電容量{r0.GridCapacity:F0} 電力価格{r0.Markets[Catalog.ElectricityId].Price:F2} 石炭{r0.Markets[Catalog.CoalId].Price:F2}");
        Console.WriteLine($"  教育: 価格{eduM.Price:F2} 生産{eduM.LastProduced:F1} 販売{eduM.LastSold:F1}　医療: 価格{medM.Price:F2} 生産{medM.LastProduced:F1}");
        foreach (var p in r0.Pops)
            Console.WriteLine($"    {p.ClassName,-6} 規模{p.Size,8:F0} 貯蓄/人{(p.Size > 1 ? p.Savings / p.Size : 0),8:F1} 識字{p.Literacy:P0} 教育Fill{p.EducationFill:F2} 満足{p.Satisfaction:F2} 闘争{p.Militancy:F2}");
    }
    return;
}

// === 内戦の診断：50シードで内戦の発生回数・発生プレイ数： dotnet run -- civilwar [seeds] ===
if (args.Length > 0 && args[0] == "civilwar")
{
    int seeds = ArgInt(args, 1, 50);
    int totalCW = 0, playsWithCW = 0;
    for (ulong sd = 1; sd <= (ulong)seeds; sd++)
    {
        NationConfig[] cc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
        var cm = new MultiNationSim(cc, sd, _ => new UtilityAgent());
        cm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
        var wasCW = new HashSet<Simulation>();   // ★2026-09-28：国の添字でなく国で持つ（建国・再併合で国の数が変わる＝旧は配列が7のまま添字あふれ）
        int cwThisPlay = 0;
        for (int q = 0; q < 400; q++)
        {
            cm.StepQuarter();
            foreach (var n in cm.Nations)
            {
                bool now = n.World.InCivilWar;   // ★2026-09-28：旧経路の内戦（CivilWarYears）を削除＝戦争の形の内戦（親も反乱国も真）
                if (now && wasCW.Add(n)) { totalCW++; cwThisPlay++; }   // 0→内戦＝発火
                else if (!now) wasCW.Remove(n);
            }
        }
        if (cwThisPlay > 0) playsWithCW++;
    }
    Console.WriteLine($"内戦の発火：{totalCW}回／{seeds}プレイ（内戦のあったプレイ {playsWithCW}/{seeds}＝{100.0 * playsWithCW / seeds:F0}%）");
    return;
}

// === スコア内訳：国力スコアの各項（軍事・富＝GDP/工業/満足/植民地・金融）： dotnet run -- score [quarters] ===
if (args.Length > 0 && args[0] == "score")
{
    int sq = ArgInt(args, 1, 240);
    NationConfig[] sc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var sm = new MultiNationSim(sc, 1789UL, _ => new UtilityAgent());
    sm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    sm.WarmupEconomy(6);
    for (int q = 0; q < sq; q++) sm.StepQuarter();
    Console.WriteLine($"--- {sm.Nations[0].World.Year}年 国力スコアの内訳 ---");
    Console.WriteLine("国        総力   軍事  富計 | GDP項 工業項 満足項 植民項  金融");
    foreach (var nat in sm.Nations.OrderByDescending(x => NationalScore.Of(x.World)))
    {
        var w = nat.World;
        double mil = NationalScore.Military(w), wealth = NationalScore.Wealth(w), fin = NationalScore.Finance(w);
        var (gdpT, indT, satT, colT) = NationalScore.WealthParts(w);   // 内訳＝正典の1本（旧＝定数の複製・2026-09-11 #15）
        Console.WriteLine($"  {w.SelfPower?.Name,-8} {NationalScore.Of(w),5:F0}  {mil,4:F0}  {wealth,4:F0} | {gdpT,4:F0}  {indT,4:F0}  {satT,4:F0}  {colT,4:F0}   {fin,4:F0}（軍{w.SelfPower?.Military:F1} 工業{w.Region.TotalIndustrialization:F1} 植民{w.SelfPower?.Colonies}）");
    }
    return;
}

// === 外交の診断：強国ほど緊張を集め孤立・交戦するか（勢力均衡）： dotnet run -- diplo ===
if (args.Length > 0 && args[0] == "diplo")
{
    NationConfig[] dc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var dm = new MultiNationSim(dc, 1789UL, _ => new UtilityAgent());
    dm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    dm.WarmupEconomy(6);
    int nn = dm.Nations.Count;
    var warQ = new int[nn]; var maxTen = new double[nn]; var sumTen = new double[nn];
    for (int q = 0; q < 400; q++)
    {
        dm.StepQuarter();
        for (int i = 0; i < nn; i++)
        {
            var p = dm.Nations[i].World.SelfPower!;
            double t = dm.Diplomacy.MaxTension(p);
            maxTen[i] = System.Math.Max(maxTen[i], t); sumTen[i] += t;
            if (dm.Diplomacy.Wars.Any(w => w.A == p || w.B == p)) warQ[i]++;
        }
    }
    int warsStarted = dm.Diplomacy.Log.Count(l => l.Contains("開戦"));
    Console.WriteLine($"ゲーム全体の開戦回数: {warsStarted}（全期間400Q＝100年）");
    Console.WriteLine($"従属/植民地イベント：属国化{dm.Diplomacy.Log.Count(l => l.Contains("を属国化") || l.Contains("属国化（植民地化"))}・植民地化{dm.Diplomacy.Log.Count(l => l.Contains("植民地化"))}・離反{dm.Diplomacy.Log.Count(l => l.Contains("離反"))}");
    foreach (var l in dm.Diplomacy.Log.Where(l => l.Contains("属国化") || l.Contains("植民地化")).Take(6)) Console.WriteLine($"  {l}");
    Console.WriteLine($"凶作ログ {dm.Diplomacy.Log.Count(l => l.Contains("凶作"))}件・鉱山枯渇ログ {dm.Diplomacy.Log.Count(l => l.Contains("鉱山"))}件・疫病ログ {dm.Diplomacy.Log.Count(l => l.Contains("疫病"))}件・独立ログ {dm.Diplomacy.Log.Count(l => l.Contains("独立"))}件。例:");
    foreach (var l in dm.Diplomacy.Log.Where(l => l.Contains("凶作") || l.Contains("鉱山")).Take(4)) Console.WriteLine($"  {l}");
    Console.WriteLine("国        GDP        軍   平均緊張 最大緊張 交戦Q数/400");
    foreach (var nat in dm.Nations.OrderByDescending(x => x.World.Gdp))
    {
        int i = dm.Nations.IndexOf(nat); var w = nat.World;
        Console.WriteLine($"  {w.SelfPower?.Name,-8} {w.Gdp,10:F0} {w.SelfPower?.Military,5:F1}   {sumTen[i] / 400,5:F0}   {maxTen[i],5:F0}    {warQ[i],4}");
    }
    return;
}

// === 犯罪と汚職の診断：秩序・犯罪率・汚職漏れ・官僚貯蓄： dotnet run -- crimecorrupt ===
if (args.Length > 0 && args[0] == "crimecorrupt")
{
    NationConfig[] cc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var cm = new MultiNationSim(cc, 1789UL, _ => new UtilityAgent());
    cm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    cm.WarmupEconomy(6);
    var maxCrime = new double[cc.Length];
    int done = 0;
    foreach (int q in new[] { 8, 40, 120, 240, 400 })
    {
        for (; done < q; done++) { cm.StepQuarter(); for (int i = 0; i < cm.Nations.Count; i++) maxCrime[i] = System.Math.Max(maxCrime[i], cm.Nations[i].World.CrimeRate); }
        Console.WriteLine($"--- {cm.Nations[0].World.Year}年 ---");
        for (int i = 0; i < cm.Nations.Count; i++)
        {
            var w = cm.Nations[i].World;
            double burSav = 0, burSz = 0;
            foreach (var p in w.AllPops()) if (p.ClassId == 8) { burSav += p.Savings; burSz += p.Size; }
            Console.WriteLine($"  {w.SelfPower?.Name,-8} 秩序{w.PublicOrder:P0} 犯罪率{w.CrimeRate:P2}（最大{maxCrime[i]:P2}） 治安維持力{w.Coercion:F0} | 行政効率{w.AdminEfficiency:P0} 汚職漏れ{w.LastTaxLeak,8:F0} 官僚貯蓄/人{(burSz > 0.01 ? burSav / burSz : 0),8:F0}");
        }
    }
    return;
}

// === 財政の診断：国庫の推移と税の状態（所得税ロック/各税率）： dotnet run -- fisc ===
if (args.Length > 0 && args[0] == "fisc")
{
    NationConfig[] fc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var fm = new MultiNationSim(fc, 1789UL, _ => new UtilityAgent());
    fm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    fm.WarmupEconomy(6);
    int done = 0;
    foreach (int q in new[] { 8, 40, 120, 240, 400 })
    {
        for (; done < q; done++) fm.StepQuarter();
        Console.WriteLine($"--- {fm.Nations[0].World.Year}年 ---");
        foreach (var n in fm.Nations)
        {
            var w = n.World;
            var p = w.Policy;
            Console.WriteLine($"  {w.SelfPower?.Name,-8} 国庫{w.Treasury,11:F0} 行政力{w.AdminPowerSmoothed:F2} 行政効率{w.AdminEfficiency:P0} | 税:所得{w.TaxRate:F2}人頭{w.PollTax:F2}土地{(w.LandTaxUnlocked ? $"{w.LandTax:F2}" : "×")}専売の負担{w.StateMonopolyBreadth:F2} | 歳出目標:教育{p.EducationSpendingTarget:F1}行政{p.AdminSpendingTarget:F1}軍事{p.MilitarySpendingTarget:F1}福祉{p.SocialSecurityTarget:F1}強制{p.CoercionTarget:F0}");
        }
    }
    return;
}

// === 軍需資金の内訳診断： dotnet run -- milfund ===
// 「税収が少ない」か「装備コストが高い（軍が大きい/取り分0.25が小さい）」かを実数で切り分ける。
if (args.Length > 0 && args[0] == "milfund")
{
    NationConfig[] mc = [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
        NationConfig.Austria(), NationConfig.Netherlands(), NationConfig.Iberia(), NationConfig.Sardinia(),
    ];
    var mm = new MultiNationSim(mc, 1789UL, _ => new UtilityAgent());
    mm.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < 364; q++) mm.StepQuarter();   // 1874年付近
    Console.WriteLine($"=== 軍需資金の内訳 {mm.Nations[0].World.Year}年 ===");
    Console.WriteLine("  国         歳入   行政    利払い ｜ 国債     債務/GDP 金利 ｜ 余剰FC  軍予算  装備cost afford 装備充足 ｜ 兵数  軍事費");
    foreach (var n in mm.Nations)
    {
        var w = n.World; var r = w.Region; var coef = w.Coef;
        double estCost = EconomyStep.MilitaryProcurementNeed(r, coef);   // 装備の必要額＝エンジンの1本（全4段・発見込み）。旧＝MilitaryUpkeepScale×MilitaryScale の二重掛け（MilitaryScale は Simulation で既に UpkeepScale 込み）・2026-09-11 #1
        double afford = Math.Min(1.0, r.MilitaryAffordRaw);               // エンジンが書いた支払能力（上限÷必要額）を1で切る＝EconomyStep と同じ読み方
        double dGdp = w.NominalGdp > 1e-9 ? w.GovDebt / w.NominalGdp : 0;
        Console.WriteLine($"  {w.SelfPower?.Name,-8} {w.LastRevenue,7:F0} {w.LastAdminClaim,7:F0} {w.LastInterest,7:F0} ｜ {w.GovDebt,9:F0} {dGdp,6:F2} {w.GovBondRate,5:F2} ｜ {w.FiscalCapacity,6:F0} {r.MilitaryBudget,6:F0} {estCost,7:F0} {afford,6:F2} {r.MilitaryEquipFill,6:F2} ｜ {w.SoldierSize(),5:F0} {w.MilitarySpending,5:F2}");
    }
    return;
}

// === フランス貴族/聖職者の再現デバッグ（使い捨て）： dotnet run -- frdbg ===
if (args.Length > 0 && args[0] == "frdbg")
{
    var fsim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    fsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in fsim.Nations) ns.TelemetryEnabled = false;
    fsim.Diplomacy.Log.Enabled = false;
    var fw = fsim.Nations.First(n => n.World.SelfPower?.BaseName == "フランス").World;
    { double nr = 0, rs = 0; int rgos = 0; foreach (var r in fw.Regions) { if (r.State != RegionState.Active) continue; foreach (var pp in r.Pops) if (pp.Closed) nr += pp.LandRent; foreach (var pr in r.Producers) if (pr.IsRgo) { rgos++; rs += pr.RentShare; } }
      Console.WriteLine($"Q0前: 貴族LandRent={nr:N0} RGO数={rgos} RentShare計={rs:F2}"); }
    for (int q = 0; q < 8; q++)
    {
        var (t, m, ms) = InterestGroups.Blocs(fw);
        double tot = Math.Max(1e-9, t + m + ms);
        Console.WriteLine($"Q{q}	教育法={fw.EducationLaw}	目標={fw.Policy.EducationLaw}	官僚={fw.Bureaucracy}	官僚目標={fw.Policy.Bureaucracy}	教義={UtilityAgent.DoctrineFrom(fw)}	旧支配層={t / tot:P0}");
        fsim.StepQuarter();
    }
    return;
}

// === 聖職者テストの再現デバッグ（使い捨て）： dotnet run -- clergydbg ===
if (args.Length > 0 && args[0] == "clergydbg")
{
    var cfg = NationConfig.Japan();
    var csim = new Simulation(cfg.ToScenario(53), new UtilityAgent());
    csim.World.ParliamentEnabled = false;
    csim.World.PoliticalLockEnabled = false;
    for (int q = 0; q < 400; q++)
    {
        csim.Run(1);
        if (q % 40 == 0) Console.WriteLine($"Q{q}	教育法={csim.World.EducationLaw}	目標={csim.World.Policy.EducationLaw}	識字≈{csim.World.Elite:F2}	国庫={csim.World.Treasury:N0}");
    }
    Console.WriteLine($"最終: {csim.World.EducationLaw} 目標 {csim.World.Policy.EducationLaw}");
    return;
}

// === 国力スコアの分解： dotnet run -- scorebreak [シード] ===
// 列強・再現率の診断（2026-08-04）：1914年断面で上位国＋史実列強の国力を項別に分解＝どの項が偽の列強を立てているか。
if (args.Length > 0 && args[0] == "scorebreak")
{
    ulong sbSeed = args.Length > 1 && ulong.TryParse(args[1], out var sbs) ? sbs : 1UL;
    var sbsim = new MultiNationSim(WorldData.Nations(), sbSeed, _ => new UtilityAgent());
    sbsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in sbsim.Nations) ns.TelemetryEnabled = false;
    sbsim.Diplomacy.Log.Enabled = false;
    while (sbsim.Year < 1914) sbsim.StepQuarter();
    var rows = new List<(string Name, double Total, double Mil, double Gdp, double Ind, double Sat, double Col, double Fin, double Tech)>();
    foreach (var ns in sbsim.Nations)
    {
        var w = ns.World; var sp = w.SelfPower; if (sp is null) continue;
        var (gdpScale, ind, sat, col) = NationalScore.WealthParts(w);   // 内訳＝正典の1本（2026-09-11 #15）
        double mil = NationalScore.Military(w), fin = NationalScore.Finance(w), tech = NationalScore.Tech(w);
        rows.Add((sp.BaseName, NationalScore.Of(w), mil, gdpScale, ind, sat, col, fin, tech));
    }
    rows.Sort((a, b) => b.Total.CompareTo(a.Total));
    Console.WriteLine("順位	国	合計	軍事	経済規模	工業化	生活	植民地	金融	技術");
    int rank = 0;
    var refs = new HashSet<string> { "イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "オーストリア", "日本", "オスマン", "清" };
    foreach (var r in rows)
    {
        rank++;
        if (rank <= 12 || refs.Contains(r.Name))
            Console.WriteLine($"{rank}	{r.Name}	{r.Total:F1}	{r.Mil:F1}	{r.Gdp:F1}	{r.Ind:F1}	{r.Sat:F1}	{r.Col:F1}	{r.Fin:F1}	{r.Tech:F1}");
    }
    return;
}

// === 民族同化と国民意識の計器： dotnet run -- ethnoprobe [シード] [四半期] [間隔] ===
// 段4（2026-09-09・設計メモ§7-7の実バグ2件）の物差し。⚠この2つは**到達年**でしか判定できない
// （「アイルランドが1803年に0.5・1849年に0.9」＝速すぎるの正体は年である）ので、
// 少数派州の中核度が 0.5／0.9 を越える**年**を全州ぶん記録して最後に表で出す。
// ⚠計器の限界を自己申告する：①分離した州は走査から外す（州が消えると到達年が入らない＝「未到達」と区別できない）
//   ②助走後の最初の観測で中核度<0.5 の州だけを「少数派州」として数える（開幕から中核の州は同化の対象ではない）。
if (args.Length > 0 && args[0] == "ethnoprobe")
{
    ulong epSeed = args.Length > 1 && ulong.TryParse(args[1], out var eps) ? eps : 1789UL;
    int epQ = args.Length > 2 && int.TryParse(args[2], out var epq) ? epq : Coefficients.GameQuarters;
    int epEvery = args.Length > 3 && int.TryParse(args[3], out var epe) ? epe : 40;
    var epsim = new MultiNationSim(WorldData.Nations(), epSeed, _ => new UtilityAgent());
    epsim.StartWarmup();
    foreach (var ns0 in epsim.Nations) ns0.TelemetryEnabled = false;
    epsim.Diplomacy.Log.Enabled = false;
    var epMinority = new Dictionary<string, (double core0, int y50, int y90, double coreLast, double litLast)>();
    bool epFirst = true;
    Console.WriteLine($"★係数: 同化基礎={new Coefficients().EthnicAssimilation:G6} 教育加点={new Coefficients().AssimilationEducationBonus:G6} 逆流={new Coefficients().AssimilationRevival:G6} 国民意識の導出={new Coefficients().NationConsciousnessDerived:G6}");
    Console.WriteLine("年\t国\t統一度\t国民意識\t最少中核の州(中核度/識字)");
    for (int q = 0; q < epQ; q++)
    {
        foreach (var ns in epsim.Nations)
        {
            var w = ns.World; if (!w.EthnicEnabled) continue;
            foreach (var r in w.Regions)
            {
                double sz = 0, cs = 0, lt = 0;
                foreach (var p in r.Pops) { sz += p.Size; cs += p.Size * p.CoreEthnicShare; lt += p.Size * p.Literacy; }
                if (!(sz > 1e-9)) continue;
                double core = cs / sz;
                string key = $"{w.SelfPower?.BaseName ?? "?"}/{r.Name}";
                if (epFirst) { if (core < 0.5) epMinority[key] = (core, 0, 0, core, lt / sz); continue; }
                if (!epMinority.TryGetValue(key, out var st)) continue;
                if (st.y50 == 0 && core >= 0.5) st.y50 = w.Year;
                if (st.y90 == 0 && core >= 0.9) st.y90 = w.Year;
                st.coreLast = core; st.litLast = lt / sz;
                epMinority[key] = st;
            }
        }
        epFirst = false;
        if (q % epEvery == epEvery - 1)
            foreach (var ns in epsim.Nations)
            {
                var w = ns.World; if (!w.EthnicEnabled) continue;
                Region? worst = null; double worstCore = 2, worstLit = 0;
                foreach (var r in w.Regions)
                {
                    double sz = 0, cs = 0, lt = 0;
                    foreach (var p in r.Pops) { sz += p.Size; cs += p.Size * p.CoreEthnicShare; lt += p.Size * p.Literacy; }
                    if (!(sz > 1e-9)) continue;
                    if (cs / sz < worstCore) { worstCore = cs / sz; worst = r; worstLit = lt / sz; }
                }
                if (worst is null || worstCore > 0.999) continue;   // 全州が中核＝同化の対象がない国は出さない
                Console.WriteLine($"{w.Year}\t{w.SelfPower?.BaseName}\t{w.EthnicUnity:F3}\t{Math.Max(0, w.NationalConsciousness):F3}\t{worst.Name}({worstCore:F3}/識{worstLit:F2})");
            }
        epsim.StepQuarter();
    }
    Console.WriteLine($"\n★少数派州 {epMinority.Count}件の到達年（0＝{epsim.Nations[0].World.Year}年までに未到達）");
    Console.WriteLine("州\t開幕中核度\t0.5到達\t0.9到達\t最終中核度\t最終識字");
    foreach (var kv in epMinority.OrderBy(k => k.Value.y50 == 0 ? 9999 : k.Value.y50).ThenBy(k => k.Key, StringComparer.Ordinal))
        Console.WriteLine($"{kv.Key}\t{kv.Value.core0:F3}\t{kv.Value.y50}\t{kv.Value.y90}\t{kv.Value.coreLast:F3}\t{kv.Value.litLast:F2}");
    int n50 = 0, n90 = 0; foreach (var kv in epMinority) { if (kv.Value.y50 > 0) n50++; if (kv.Value.y90 > 0) n90++; }
    Console.WriteLine($"★合計: 0.5到達 {n50}/{epMinority.Count}件・0.9到達 {n90}/{epMinority.Count}件");
    return;
}

// === 一国の軌跡： dotnet run -- natprobe <国名> [シード] ===
// P4（プロイセン停滞）の診断（2026-08-04）。5年ごとに経済・軍事・政治の主要量を1行で出す。
if (args.Length > 0 && args[0] == "natprobe")
{
    string npName = args.Length > 1 ? args[1] : "プロイセン";
    ulong npSeed = args.Length > 2 && ulong.TryParse(args[2], out var nps) ? nps : 1UL;
    var npsim = new MultiNationSim(WorldData.Nations(), npSeed, _ => new UtilityAgent());
    npsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in npsim.Nations) ns.TelemetryEnabled = false;
    npsim.Diplomacy.Log.Enabled = false;
    Console.WriteLine("年	工業化	GDP	国庫	兵	交戦	満足	闘争	政体	正統	投資家貯蓄	制度疲労");
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        npsim.StepQuarter();
        if (q % 20 != 19) continue;   // 5年ごと
        var ns = npsim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == npName);
        if (ns is null) continue;
        var w = ns.World; var sp = w.SelfPower!;
        int wars = 0; foreach (var war in npsim.Diplomacy.Wars) if (war.A == sp || war.B == sp) wars++;
        double invSav = 0;
        foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; foreach (var pp in r.Pops) if (pp.IsModernElite) invSav += Math.Max(0, pp.Savings); }
        double mpPool = 0;
        double mpDesired = Military.EstablishmentOf(w, w.Coef, out _);   // ★(155) 定員（州の和と一致）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pp in r.Pops)
                if (!pp.IsSoldier && (pp.ClassId == w.Coef.SoldierRecruitClassA || pp.ClassId == w.Coef.SoldierRecruitClassB)) mpPool += Military.RecruitMen(pp);   // 兵役の母数＝徴募元の働き盛りの男（エンジンと同じ1本）
        }
        double lossers = 0, lossVal = 0; int lossN = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pr in r.Producers)
                if (!pr.Bankrupt && pr.SmoothedMargin < 0) { lossN++; lossVal += -pr.SmoothedMargin * pr.Output * Math.Max(0, r.Markets.Length > pr.GoodId && pr.GoodId >= 0 ? r.Markets[pr.GoodId].Price : 0); if (pr.BuiltScale > 1e-9) lossers++; }
        }
        Console.WriteLine($"{w.Year}	{w.MeanIndustrialization:F2}	{w.Gdp:N0}	{w.Treasury:N0}	{w.SoldierSize():F0}(即{w.MilitaryReadiness:F2}/費{w.MilitarySpending:P0})	{wars}	{NationalScore.MeanSatisfaction(w):F2}	{Politics.AvgMassMilitancy(w):F2}	{w.Polity}	{w.Legitimacy:F2}	{invSav:N0}	プール{mpPool:F0}/目標兵{mpDesired:F0}	赤字{lossN}件(Built{lossers:F0})	損失流量{lossVal:N0}	累積負債{w.LossFinanced:N0}");
    }
    return;
}

// === 州別の軍徴募診断： dotnet run -- milprobe <国名> [シード] ===
// 任務#24「軍事項の消耗」（2026-08-08）：徴募は州単位（Military.Step）＝兵士POPの居ない州は徴募ゼロ・
// 目標もその州のプール比例。国全体のプールが太くても**兵士POPの居る州のプールが痩せれば軍は立たない**。
// 10年ごとに全州の 兵士/プール(農民+労働者)/州目標 を並べ、目標が小さいのか満たされないのかを州で確定する。
if (args.Length > 0 && args[0] == "milprobe")
{
    string mprName = args.Length > 1 ? args[1] : "アメリカ";
    ulong mprSeed = args.Length > 2 && ulong.TryParse(args[2], out var mprs) ? mprs : 1UL;
    var mprsim = new MultiNationSim(WorldData.Nations(), mprSeed, _ => new UtilityAgent());
    mprsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in mprsim.Nations) ns.TelemetryEnabled = false;
    mprsim.Diplomacy.Log.Enabled = false;
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        mprsim.StepQuarter();
        if (q % 40 != 39) continue;   // 10年ごと
        var ns = mprsim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == mprName);
        if (ns is null) continue;
        var w = ns.World;
        Console.WriteLine($"── {w.Year} 軍事費{w.MilitarySpending:P0} 総兵{w.SoldierSize():F1} 即応{w.MilitaryReadiness:F2}");
        double mprTarget = Military.EstablishmentOf(w, w.Coef, out double mprNat);   // ★(155) 定員の州按分（エンジンと同じ1本）
        foreach (var r in w.Regions)
        {
            double pa = 0, pb = 0, ss = -1;   // ss=-1＝兵士POPなし
            foreach (var pp in r.Pops)
            {
                if (pp.IsSoldier) ss = Math.Max(0, ss) + pp.Size;
                else if (pp.ClassId == w.Coef.SoldierRecruitClassA) pa += Military.RecruitMen(pp);   // 兵役の母数（エンジンと同じ1本）
                else if (pp.ClassId == w.Coef.SoldierRecruitClassB) pb += Military.RecruitMen(pp);
            }
            double desired = mprNat > 1e-9 ? mprTarget * Military.RegionManpower(r, w.Coef) / mprNat : 0;
            Console.WriteLine($"  [{r.State}] {r.Name}	兵{(ss < 0 ? "なし" : ss.ToString("F1"))}	プール{pa + pb:F0}	州目標{desired:F1}");
        }
    }
    return;
}

// === 政治ブロックの力： dotnet run -- blocprobe ===
// 「制度の階段が立たない」の診断（2026-08-04）。開幕直後の旧支配層/近代エリート/大衆の政治力と教義を並べる。
// 1783年の英で旧支配層が3%なら、法の門番（伝統教義のみ発動）が全員素通りする＝早期近代化の統一根。
if (args.Length > 0 && args[0] == "blocprobe")
{
    var bsim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    bsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in bsim.Nations) ns.TelemetryEnabled = false;
    bsim.Diplomacy.Log.Enabled = false;
    for (int q = 0; q < 2; q++) bsim.StepQuarter();
    Console.WriteLine("国	旧支配層	近代エリート	大衆	貴族富/人	資本家富/人	貴族総富shr");
    foreach (var nm in new[] { "イギリス", "フランス", "プロイセン", "ロシア", "日本", "清", "ビルマ", "オスマン" })
    {
        var ns = bsim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
        if (ns is null) continue;
        var w = ns.World;
        var (t, m, ms) = InterestGroups.Blocs(w);
        double tot = t + m + ms; if (tot < 1e-9) tot = 1;
        double nobSav = 0, nobSize = 0, capSav = 0, capSize = 0, allSav = 0;
        foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; foreach (var p in r.Pops) { allSav += Math.Max(0, p.Savings); if (p.Closed) { nobSav += Math.Max(0, p.Savings); nobSize += p.Size; } else if (p.IsModernElite) { capSav += Math.Max(0, p.Savings); capSize += p.Size; } } }
        Console.WriteLine($"{nm}	{t / tot:P0}	{m / tot:P0}	{ms / tot:P0}	{(nobSize > 1e-9 ? nobSav / nobSize : 0):N0}	{(capSize > 1e-9 ? capSav / capSize : 0):N0}	{(allSav > 1e-9 ? nobSav / allSav : 0):P0}");
    }
    return;
}

// === 列強の軌跡： dotnet run -- powertrace [シード] ===
// 「史実の列強が列強にならない」の診断（2026-08-04）。参照8カ国＋比較のアジア勢について、
// GDP・工業化・人口・国力・植民地・スタンスを年代ごとに並べ、**どこで差がついたか**を見る。
if (args.Length > 0 && args[0] == "powertrace")
{
    ulong ptSeed = args.Length > 1 && ulong.TryParse(args[1], out var pts) ? pts : 1UL;
    var psim = new MultiNationSim(WorldData.Nations(), ptSeed, _ => new UtilityAgent());
    psim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in psim.Nations) ns.TelemetryEnabled = false;
    psim.Diplomacy.Log.Enabled = false;
    string[] watch = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "オーストリア", "日本", "清", "マラータ同盟", "ビルマ"];
    int[] marks = [1800, 1850, 1880, 1914];
    int mi = 0;
    Console.WriteLine("年	国	GDP	工業化	国庫	投資家貯蓄	投資家所得	資本家所得	工業レント	近代化費	歳入	技術");
    for (int q = 0; q < Coefficients.GameQuarters && mi < marks.Length; q++)
    {
        psim.StepQuarter();
        int y = psim.Year;
        if (y < marks[mi]) continue;
        foreach (var nm in watch)
        {
            var ns = psim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (ns is null) continue;
            var w = ns.World; var sp = w.SelfPower!;
            double invSav = 0, invSize = 0, pop = 0, invEarn = 0, capEarn = 0, indRent = 0;   // 資本家単独・工業レント＝設計3段1の物差し
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var pp in r.Pops)
                {
                    pop += pp.Size; if (pp.IsModernElite) { invSav += Math.Max(0, pp.Savings); invSize += pp.Size; invEarn += Math.Max(0, pp.Earned); }
                    if (r.InvestorPopIndex >= 0 && pp == r.Pops[r.InvestorPopIndex]) { capEarn += Math.Max(0, pp.Earned); indRent += Math.Max(0, pp.IndustrialRent); }
                }
            }
            Console.WriteLine($"{y}	{nm}	{w.Gdp:N0}	{w.MeanIndustrialization:F2}	{w.Treasury:N0}	{invSav:N0}	{invEarn:N0}	{capEarn:N0}	{indRent:N0}	{w.ModernizationFrac:P0}	{w.LastRevenue:N0}	{w.TechLevel:F2}");
        }
        Console.WriteLine();
        mi++;
    }
    return;
}

// === 財政の計器： dotnet run -- fisprobe [シード] ===
// 露普歳入是正 S0（2026-08-08・wf_f219b706）：国別に 歳入・抽出率（歳入/GDP）・国庫・潤沢線・減税mul実効・
// 行政力・実効税率（人頭/土地/消費/関税）・関税実収 を1850/1913断面で出す。潤沢ロック（mul=0.25恒久）の検出計器。
if (args.Length > 0 && args[0] == "fisprobe")
{
    ulong fpSeed = args.Length > 1 && ulong.TryParse(args[1], out var fps) ? fps : 1UL;
    var fpsim = new MultiNationSim(WorldData.Nations(), fpSeed, _ => new UtilityAgent());
    fpsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in fpsim.Nations) ns.TelemetryEnabled = false;
    fpsim.Diplomacy.Log.Enabled = false;
    string[] fpWatch = ["イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "アメリカ", "清", "日本", "オスマン"];
    int[] fpMarks = [1850, 1913];
    int fpMi = 0;
    for (int q = 0; q < Coefficients.GameQuarters && fpMi < fpMarks.Length; q++)
    {
        fpsim.StepQuarter();
        if (fpsim.Year < fpMarks[fpMi]) continue;
        Console.WriteLine($"━━ {fpsim.Year} 国	歳入/月	抽出率	国庫	潤沢線	減税mul	行政力	人頭/土地/消費/関税/所得	関税実収 ━━");
        foreach (var nm in fpWatch)
        {
            var ns = fpsim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (ns is null) continue;
            var w = ns.World;
            double line = UtilityAgent.FiscalBufferOf(w) * 8.0;
            double mul = w.Treasury > line && line > 1e-9 ? Math.Max(0.25, line / w.Treasury) : 1.0;
            double extract = w.Gdp > 1e-9 ? w.LastRevenue / w.Gdp : 0;
            Console.WriteLine($"{nm}	{w.LastRevenue:N0}	{extract:P1}	{w.Treasury:N0}	{line:N0}	×{mul:F2}	{w.AdminPowerSmoothed:F2}	{w.PollTax:F2}/{w.LandTax:F2}/{w.StateMonopolyBreadth * w.Coef.StateMonopolyMargin:F2}/{w.Tariff:F2}/{w.TaxRate:F2}	{Math.Max(0, w.LastTariffRevenue):N0}");
        }
        Console.WriteLine();
        fpMi++;
    }
    return;
}

// === 理想の軍隊の形の計器： dotnet run -- armyneed [シード] ===
// 形が先・金が後 段0（2026-08-08・wf_e6c0be63）：国別に NeedMen/現兵数/軍事費/装備afford/S4容量信号/1兵あたり装備費を
// 1800/1850/1913 断面で出し、1913 は史実アンカー（常備軍・対英比）との順位相関を印字＝R2点火のblockingゲート（≥0.7）。
if (args.Length > 0 && args[0] == "armyneed")
{
    ulong anSeed = args.Length > 1 && ulong.TryParse(args[1], out var ans) ? ans : 1UL;
    var ansim = new MultiNationSim(WorldData.Nations(), anSeed, _ => new UtilityAgent());
    ansim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in ansim.Nations) ns.TelemetryEnabled = false;
    ansim.Diplomacy.Log.Enabled = false;
    // 史実1913アンカー（常備軍の対英比・設計段1）：露2.0/普1.4/仏1.3/墺1.1/英1.0/米0.4/清2.0
    var anchor = new (string Name, double Rel)[] { ("ロシア", 2.0), ("プロイセン", 1.4), ("フランス", 1.3), ("オーストリア", 1.1), ("イギリス", 1.0), ("アメリカ", 0.4), ("清", 2.0) };
    string[] anWatch = ["イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "アメリカ", "清", "日本", "オスマン"];
    int[] anMarks = [1800, 1850, 1913];
    int anMi = 0;
    for (int q = 0; q < Coefficients.GameQuarters && anMi < anMarks.Length; q++)
    {
        ansim.StepQuarter();
        if (ansim.Year < anMarks[anMi]) continue;
        Console.WriteLine($"━━ {ansim.Year} 国	NeedMen	現兵	軍事費	装備afford	定員 ━━");
        var needSnapshot = new Dictionary<string, double>();
        foreach (var nm in anWatch)
        {
            var ns = ansim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (ns is null) continue;
            var w = ns.World; var sp = w.SelfPower!;
            var home = w.Region;
            // ★(155) 定員（制度が開く席の数＝払える予算÷1人あたりの費用と徴兵法の割当ての小さい方）＝純関数で検分できる（旧＝扶養限界×軍拡の倍率は削除）
            double target = Military.EstablishmentOf(w, w.Coef, out _);
            needSnapshot[nm] = target;   // 順位相関は定員で取る（旧＝NeedMen）
            Console.WriteLine($"{nm}	{sp.NeedMen:F0}	{w.SoldierSize():F0}	{w.MilitarySpending:P0}	{home.MilitaryAffordRaw:F2}	{target:F0}");
        }
        if (ansim.Year >= 1913)
        {
            // 順位相関（スピアマン）＝扶養限界目標の順位 vs 史実常備軍の順位（アンカー7カ国）
            var pairs = anchor.Where(x => needSnapshot.ContainsKey(x.Name)).ToList();
            var needRank = pairs.OrderByDescending(x => needSnapshot[x.Name]).Select((x, i) => (x.Name, R: i)).ToDictionary(t => t.Name, t => t.R);
            var histRank = pairs.OrderByDescending(x => x.Rel).Select((x, i) => (x.Name, R: i)).ToDictionary(t => t.Name, t => t.R);
            double d2 = pairs.Sum(x => Math.Pow(needRank[x.Name] - histRank[x.Name], 2));
            int nn = pairs.Count;
            double rho = nn >= 3 ? 1.0 - 6.0 * d2 / (nn * (double)(nn * nn - 1)) : 0;
            Console.WriteLine($"順位相関（定員 vs 史実常備軍・{nn}カ国）ρ = {rho:F2}（点火のblockingゲート ≥0.7）");
        }
        Console.WriteLine();
        anMi++;
    }
    return;
}

// === 初期軍需の需給断面： dotnet run -- milstart [シード] ===
// 作者報告（2026-08-21）「初期軍隊と実際の生産が噛み合っていない（火器0%・軍艦1083%）」の実測計器。
// 戦争タブ「理想の軍隊」の充足（GameViewModel.Politics.RefreshArmyNeeds の FlowRow＝afford×陸海mul×価格ゲート×配給）と
// 同じ式を全国×開始直後（Q1/Q8/Q28）で分解して並べる＝0%と1083%を作っている因子を特定する。読み取り専用（エンジンに触れない）。
if (args.Length > 0 && args[0] == "milstart")
{
    ulong msSeed = ArgUlong(args, 1, 1UL);
    var msSim = new MultiNationSim(WorldData.Nations(), msSeed, _ => new UtilityAgent());
    foreach (var ns in msSim.Nations) ns.TelemetryEnabled = false;
    msSim.Diplomacy.Log.Enabled = false;
    msSim.StartWarmup();   // App の新規ゲームと同じ助走＝プレイヤーが見る開始盤面と一致させる
    // ★Q1 だけで判断しない（レビュー2026-08-22）：中盤（1850前後＝Q268）・終局（1914＝Q524）でも fill が破綻していないか、
    //   工業化後に軍需産業が過剰に居座って他産業の労働を食う**逆症状**が出ていないかを同じ計器で見る（第2引数 long）。
    int[] msCuts = args.Length > 2 && args[2] == "long" ? [1, 8, 28, 268, 524] : [1, 8, 28];
    int msDone = 0;
    foreach (int cut in msCuts)
    {
        for (; msDone < cut; msDone++) msSim.StepQuarter();
        Console.WriteLine($"━━ Q{cut}（{msSim.Year}年）━━");
        Console.WriteLine("国名,兵士,MilScale,港,海の金の割合,NeedLand,NeedInt,NeedSea,闘争度,正統性,flatShare,navalMul,landMul,afford,小銃fill,火砲fill,弾薬fill,軍艦fill,汽船fill,小銃ration,軍艦ration,小銃prod,軍艦prod,軍艦need,軍艦価格比");
        int nNaval5 = 0, nLand05 = 0, nShape9 = 0, nShapeNone = 0, nTotal = 0;
        foreach (var nsim in msSim.Nations)
        {
            var w = nsim.World; var c = w.Coef; var r = w.Region;
            if (w.SelfPower is not { } sp || r.Markets.Length == 0) continue;
            int ng = r.Markets.Length;
            double Price(int g) => r.Markets[g].Price;
            double Ration(int g) => r.Markets[g].LastRation;
            double PriceAfford(int g) => EconomyStep.StatePriceAfford(r, c, g);   // 国家調達の価格の門の1本（エンジンと同じ式・2026-09-30 段4e K）
            double[] milRates = EconomyStep.DiscoveredRates(r, c, c.MilitaryGoods, c.MilitaryRates);   // 発見の解禁度込みの原単位＝エンジンと同じ1本（2026-09-11 #17）
            double flatNaval = 0, flatAll = 0;
            for (int b = 0; b < c.MilitaryGoods.Length; b++)
            {
                if (c.MilitaryGoods[b] >= ng) { flatAll = 0; break; }
                double cst = milRates[b] * Price(c.MilitaryGoods[b]);
                flatAll += cst;
                if (Array.IndexOf(c.FleetGoodIds, c.MilitaryGoods[b]) >= 0) flatNaval += cst;
            }
            if (flatAll <= 1e-9) continue;   // 小シナリオ（軍需財なし）は対象外
            double flatShare = flatNaval / flatAll;
            // ★(189) 陸の装備の段は艦を買わない（EconomyStep.LandMul＝艦は0・他は1）＝艦は海軍の調達（海軍の予算 Region.NavalBudget）。旧＝陸海の再配分（ShapeNavalShare）
            double navalMul = 0.0, landMul = 1.0;
            double MilMul(int g) => EconomyStep.LandMul(c, g);
            double afford = Math.Min(1.0, r.MilitaryAffordRaw);
            double Fill(int g) => afford * MilMul(g) * PriceAfford(g) * Ration(g);
            double ProdN(int g) { double s = 0; foreach (var rr in w.Regions) if (rr.State == RegionState.Active && g < rr.Markets.Length) s += rr.Markets[g].LastProduced; return s; }
            double NeedOf(int gid) { for (int b = 0; b < c.MilitaryGoods.Length; b++) if (c.MilitaryGoods[b] == gid) return r.MilitaryScale * milRates[b]; return 0; }
            int gRifle = Catalog.RifleId, gArty = Catalog.ArtilleryId, gAmmo = Catalog.AmmunitionId, gShip = Catalog.WarshipId, gSteam = Catalog.SteamshipId;
            double shipRef = Catalog.RefPrices[gShip] * Math.Max(r.PriceLevel, 1e-9);
            nTotal++;
            if (navalMul > 5) nNaval5++;
            if (landMul < 0.05) nLand05++;
            if (w.NavalSpendShare > 0.9) nShape9++;
            if (r.NavalBudget <= 0) nShapeNone++;
            Console.WriteLine($"{sp.Name},{w.SoldierSize():F0},{r.MilitaryScale:F1},{(r.NationHasPort ? 1 : 0)},{w.NavalSpendShare:F3},{sp.NeedLand:F2},{sp.NeedInternal:F2},{sp.NeedSea:F3},{Politics.AvgMassMilitancy(w):F3},{w.Legitimacy:F3}," +
                $"{flatShare:F3},{navalMul:F2},{landMul:F2},{afford:F2}," +
                $"{Fill(gRifle):P0},{Fill(gArty):P0},{Fill(gAmmo):P0},{Fill(gShip):P0},{Fill(gSteam):P0}," +
                $"{Ration(gRifle):F2},{Ration(gShip):F2},{ProdN(gRifle):F1},{ProdN(gShip):F1},{NeedOf(gShip):F2},{(shipRef > 1e-9 ? Price(gShip) / shipRef : 0):F2}");
        }
        Console.WriteLine($"# Q{cut} 集計: 全{nTotal}カ国　navalMul>5: {nNaval5}　landMul<0.05: {nLand05}　海の金の割合>0.9: {nShape9}　海軍の予算0: {nShapeNone}");
        // ---- 価格の解剖（首都州・主要国）：小銃が参照の何倍か＝入力（鋼/製材/火薬/硝石）のどれが高いかを分解 ----
        // 軍需労働シェア＝軍需7財の prodW（＝需要量×Scale・EconomyStep:300 と同一式）÷ 全生産者の prodW
        //   ＝**逆症状（軍需産業が他産業の労働を食う）の計器**。工業化後にこれが膨らんでいないかを見る。
        Console.WriteLine("解剖:国名,小銃P比,鋼P比,製材P比,火薬P比,硝石P比,帆船P比,火砲P比,小銃Scale,小銃MatFill,小銃Out,小銃需要,小銃在庫,火薬Scale,火薬Out,軍需労働シェア%,工業化");
        foreach (var nm in (string[])["イギリス", "フランス", "ロシア", "清", "日本", "プロイセン", "ネーデルラント"])
        {
            var nsim = msSim.Nations.FirstOrDefault(x => x.World.SelfPower?.BaseName == nm);
            if (nsim is null) continue;
            var w = nsim.World; var r = w.Region;
            double PR(int g) => g < r.Markets.Length && g < Catalog.RefPrices.Length && Catalog.RefPrices[g] > 1e-9
                ? r.Markets[g].Price / (Catalog.RefPrices[g] * Math.Max(r.PriceLevel, 1e-9)) : double.NaN;
            Producer? pRifle = null, pPowder = null;
            double wArms = 0, wAll = 0;
            foreach (var pr in r.Producers)
            {
                if (pr.GoodId == Catalog.RifleId && !pr.IsRgo) pRifle = pr;
                if (pr.GoodId == Catalog.GunpowderId && !pr.IsRgo) pPowder = pr;
                if (pr.GoodId >= r.Markets.Length) continue;
                double wgt = r.Markets[pr.GoodId].LastDemand * pr.Scale;   // EconomyStep:300 の prodW と同一式
                wAll += wgt;
                if (System.Array.IndexOf(Simulation.InitialArmsIndustryGoods, pr.GoodId) >= 0) wArms += wgt;
            }
            Console.WriteLine($"解剖:{nm},{PR(82):F2},{PR(32):F2},{PR(39):F2},{PR(38):F2},{PR(118):F2},{PR(84):F2},{PR(81):F2}," +
                $"{pRifle?.Scale ?? -1:F2},{pRifle?.LastMaterialFill ?? -1:F2},{pRifle?.Output ?? -1:F1},{r.Markets[Catalog.RifleId].LastDemand:F1},{r.Markets[Catalog.RifleId].Stock:F0},{pPowder?.Scale ?? -1:F2},{pPowder?.Output ?? -1:F1}," +
                $"{(wAll > 1e-9 ? wArms / wAll * 100 : 0):F2},{r.Industrialization:F2}");
        }
    }
    return;
}

// === 廃棄材の計器： dotnet run -- spoilprobe [国名] [シード] ===
// 作者要望（2026-08-08）「特定材が失われ続けていないかのチェック」。1800/1850/1913 の3断面で、
// 直近12ヶ月の廃棄量（腐敗・劣化＝Market.SpoiledLastYear・全Active州合算）上位15財を生産比つきで出す。
// 生産比が高い財＝作り続けて捨て続けている（過剰生産の恒常化）。
if (args.Length > 0 && args[0] == "spoilprobe")
{
    string spName = args.Length > 1 ? args[1] : "イギリス";
    ulong spSeed = args.Length > 2 && ulong.TryParse(args[2], out var sps) ? sps : 1UL;
    var spsim = new MultiNationSim(WorldData.Nations(), spSeed, _ => new UtilityAgent());
    spsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in spsim.Nations) ns.TelemetryEnabled = false;
    spsim.Diplomacy.Log.Enabled = false;
    int[] spMarks = [1800, 1850, 1913];
    int spMi = 0;
    for (int q = 0; q < Coefficients.GameQuarters && spMi < spMarks.Length; q++)
    {
        spsim.StepQuarter();
        if (spsim.Year < spMarks[spMi]) continue;
        var ns = spsim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == spName);
        if (ns is null) { spMi++; continue; }
        var w = ns.World;
        int ng = Catalog.Goods.Length;
        double[] spoilYr = new double[ng], prodMo = new double[ng];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            for (int gi = 0; gi < Math.Min(ng, r.Markets.Length); gi++)
            { spoilYr[gi] += r.Markets[gi].SpoiledLastYear; prodMo[gi] += r.Markets[gi].LastProduced; }
        }
        Console.WriteLine($"━━ {w.Year} {spName} 廃棄上位15財（直近12ヶ月・生産比＝廃棄÷月次生産×12）━━");
        foreach (var gi in Enumerable.Range(0, ng).OrderByDescending(i => spoilYr[i]).Take(15))
        {
            if (spoilYr[gi] < 0.5) break;
            string ratio = prodMo[gi] > 1e-9 ? $"{spoilYr[gi] / (prodMo[gi] * 12):P0}" : "生産なし";
            Console.WriteLine($"  {Catalog.Goods[gi].Name}	廃棄{spoilYr[gi]:N0}	生産{prodMo[gi]:N0}/月	生産比{ratio}");
        }
        spMi++;
    }
    return;
}

// === 所得と需要の翻訳の計器： dotnet run -- incomeprobe [シード] ===
// 設計3段0（所得翻訳ゲート・2026-08-08）：r=(LastEarned/Size)÷basketRef（月収が基礎バスケット何個分か）を
// 国別・階級別に出す。r(英労働者)/r(清農民) の勾配が細ければ翻訳器に入力が無い（→段5の賃金勾配を先に）。
// GDP順位表（1800/1850/1913）と 資本家所得/人・貯蓄/月収比も併記＝(4)ビルマ>英 と (6)英14位 の直接の物差し。
if (args.Length > 0 && args[0] == "incomeprobe")
{
    ulong ipSeed = args.Length > 1 && ulong.TryParse(args[1], out var ips) ? ips : 1UL;
    var ipsim = new MultiNationSim(WorldData.Nations(), ipSeed, _ => new UtilityAgent());
    ipsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in ipsim.Nations) ns.TelemetryEnabled = false;
    ipsim.Diplomacy.Log.Enabled = false;
    string[] ipWatch = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "ビルマ", "日本"];
    int[] ipMarks = [1800, 1850, 1913];
    int ipMi = 0;
    for (int q = 0; q < Coefficients.GameQuarters && ipMi < ipMarks.Length; q++)
    {
        ipsim.StepQuarter();
        if (ipsim.Year < ipMarks[ipMi]) continue;
        Console.WriteLine($"━━ {ipsim.Year} GDP順位（上位15）━━");
        var ranked = ipsim.Nations.Where(n => n.World.SelfPower is not null)
            .OrderByDescending(n => n.World.Gdp).ToList();
        for (int i = 0; i < Math.Min(15, ranked.Count); i++)
            Console.WriteLine($"  {i + 1,2}位 {ranked[i].World.SelfPower!.BaseName}	GDP {ranked[i].World.Gdp:N0}");
        Console.WriteLine("国	階級	r(月収/バスケット)	貯蓄/月収	所得/人	規模");
        foreach (var nm in ipWatch)
        {
            var ns = ipsim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (ns is null) continue;
            var w = ns.World;
            var home = w.Region;
            double basketRef = 0;
            foreach (var g in home.Goods) if (g.Tier == GoodTier.Staple || g.Tier == GoodTier.Daily) basketRef += g.BaseNeed * g.RefPrice;
            if (basketRef <= 1e-9) continue;
            // 階級別集計（全Active州合算）：農民(1)・労働者(2)・資本家(InvestorPopIndex)
            double[] earn = new double[3], size = new double[3], sav = new double[3];
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                for (int pi = 0; pi < r.Pops.Count; pi++)
                {
                    var pp = r.Pops[pi];
                    int slot = pp.ClassId == 1 ? 0 : pp.ClassId == 2 ? 1 : (r.InvestorPopIndex == pi ? 2 : -1);
                    if (slot < 0) continue;
                    earn[slot] += Math.Max(0, pp.LastEarned); size[slot] += pp.Size; sav[slot] += Math.Max(0, pp.Savings);
                }
            }
            string[] names = ["農民", "労働者", "資本家"];
            for (int s2 = 0; s2 < 3; s2++)
            {
                if (size[s2] <= 1e-9) continue;
                double perCap = earn[s2] / size[s2];
                double rr = perCap / basketRef;
                double savMonths = earn[s2] > 1e-9 ? sav[s2] / earn[s2] : -1;
                Console.WriteLine($"{nm}	{names[s2]}	{rr:F2}	{savMonths:F0}	{perCap:F1}	{size[s2]:F0}");
            }
        }
        Console.WriteLine();
        ipMi++;
    }
    return;
}

// === 女性就業率の検証（社会移動監査rank12・作者指示2026-08-16「女性の就業率が歴史と同程度か確認」）： dotnet run -- genderprobe [シード] ===
// 読み取り専用の計器：国別の**実効女性労働参加率**（LaborSupply.FemaleParticipation＝構造値(賃金保持)+困窮上乗せ）を
// 成人女性の加重平均で集計し、3断面（1800/1851/1913）で印字。史実アンカー＝**英1851の女性就業率≈30%・首位は家事奉公**
//（英130-150万人）。使用人の女性比と「使用人が女性就業に占める割合」も併記＝ProfessionGenderGate点火A/Bの物差し。
if (args.Length > 0 && args[0] == "genderprobe")
{
    ulong gpSeed = ArgUlong(args, 1, 1UL);
    var gpSim = new MultiNationSim(WorldData.Nations(), gpSeed, _ => new UtilityAgent());
    gpSim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in gpSim.Nations) ns.TelemetryEnabled = false;
    gpSim.Diplomacy.Log.Enabled = false;
    string[] gpWatch = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "日本", "スペイン"];
    int[] gpMarks = [1800, 1851, 1913];
    int gpMi = 0;
    Console.WriteLine("年,国,実効女性参加率,構造参加率(首都),賃金保持,女性の権利,使用人の女性比,使用人/女性就業");
    for (int q = 0; q < Coefficients.GameQuarters && gpMi < gpMarks.Length; q++)
    {
        gpSim.StepQuarter();
        if (gpSim.Year < gpMarks[gpMi]) continue;
        foreach (var nm in gpWatch)
        {
            var ns = gpSim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (ns is null) continue;
            var w = ns.World;
            double femAd = 0, femWork = 0, servFem = 0, servAll = 0, servFemWork = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                for (int pi = 0; pi < r.Pops.Count; pi++)
                {
                    var p = r.Pops[pi];
                    if (p.IsSoldier || p.Size <= 1e-9) continue;   // 兵士は男性のみ＝就業率の分母に入れない（女性の層が0でも明示除外）
                    double fa = p.Size * p.AgeShare(15, 65, 1);                                 // 働く年齢（15〜64歳）の女性（年齢の層・段4e 後半）
                    double part = LaborSupply.FemaleParticipation(r, w.Coef, p.SupplyFill);    // 実効参加率（構造+困窮×賃金保持）
                    femAd += fa; femWork += fa * part;
                    if (pi == r.ServantPopIndex) { double a20 = p.AgeShare(20, double.PositiveInfinity); servFem += (a20 > 1e-300 ? p.AgeShare(20, double.PositiveInfinity, 1) / a20 : 0) * p.Size; servAll += p.Size; servFemWork += fa * part; }
                }
            }
            double eff = femAd > 1e-9 ? femWork / femAd : 0;
            Console.WriteLine($"{gpSim.Year},{nm},{eff:F3},{w.Region.FemaleParticipation:F2},{w.WomenWageRetention:F2},{w.WomenRights}," +
                $"{(servAll > 1e-9 ? servFem / servAll : 0):F2},{(femWork > 1e-9 ? servFemWork / femWork : 0):F3}");
        }
        Console.WriteLine();
        gpMi++;
    }
    return;
}

// === 階級シェアの検証（CohortGateK点火A/Bの計器・2026-08-16）： dotnet run -- classprobe [シード] ===
// 読み取り専用：国別の上流/中流/下流シェア（Meritocracy.ClassOf＝失業プールと同じ物差し・兵士除外）を3断面で印字。
// 史実アンカーの目安＝英中流は19世紀を通じ拡大（rank14の2.5%→4.4%は上層中流のみ・広義中流は15〜25%へ）。
if (args.Length > 0 && args[0] == "classprobe")
{
    ulong cpSeed = ArgUlong(args, 1, 1UL);
    var cpSim = new MultiNationSim(WorldData.Nations(), cpSeed, _ => new UtilityAgent());
    cpSim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in cpSim.Nations) ns.TelemetryEnabled = false;
    cpSim.Diplomacy.Log.Enabled = false;
    string[] cpWatch = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "日本", "スペイン"];
    int[] cpMarks = [1800, 1851, 1913];
    int cpMi = 0;
    Console.WriteLine("年,国,上流,中流,下流,失業プール計,総人口(M)");
    for (int q = 0; q < Coefficients.GameQuarters && cpMi < cpMarks.Length; q++)
    {
        cpSim.StepQuarter();
        if (cpSim.Year < cpMarks[cpMi]) continue;
        foreach (var nm in cpWatch)
        {
            var ns = cpSim.Nations.FirstOrDefault(n => n.World.SelfPower?.BaseName == nm);
            if (ns is null) continue;
            double up = 0, mid = 0, low = 0, unemp = 0, tot = 0;
            foreach (var r in ns.World.Regions)
            {
                if (r.State != RegionState.Active) continue;
                for (int pi = 0; pi < r.Pops.Count; pi++)
                {
                    var p = r.Pops[pi];
                    if (p.Size <= 1e-9) continue;
                    int cls = Meritocracy.ClassOf(r, pi);
                    if (cls < 0 || p.IsSoldier) { tot += p.Size; continue; }   // 兵士は分母（総人口）のみ（★(155) ClassOf は兵士を下流の職に数える＝この計器は旧の物差しのまま兵士を外す）
                    double emp = p.Size * (1.0 - p.UnemploymentRate);
                    if (cls == 2) up += emp; else if (cls == 1) mid += emp; else low += emp;
                    unemp += p.Size * p.UnemploymentRate;
                    tot += p.Size;
                }
            }
            double den = up + mid + low + unemp;
            if (den <= 1e-9) continue;
            Console.WriteLine($"{cpSim.Year},{nm},{up / den:F4},{mid / den:F4},{low / den:F4},{unemp / den:F4},{tot / 1e6:F1}");
        }
        Console.WriteLine();
        cpMi++;
    }
    return;
}

// === プレイヤー代理の回帰計器（AI動詞被覆監査2026-08-16 第0波）： dotnet run -- noviceprobe [シード=1] [国名=日本] [除外手（例 "3" "3,7"）] ===
// 作者指摘「初心者でも日本で列強一位になれる＝簡単すぎる」を**数で追う**装置＝改修前後の物差し。
// 試合1＝初心者スクリプト（App と同じ IsPlayer+NullAgent。UIが呼ぶのと同じ公開動詞だけで監査4-3の①〜⑦を毎四半期実行）、
// 試合2＝同シードで全AI（対照）。両試合の5年ごとの順位軌跡（序列＝NationalScore.Standing・全国家中）と最終順位差を出す。
// 手＝①法を1本ずつ（幽霊戦線を作らない） ②近代化投資0.6 ③平時軍事費0/交戦中1.0×2+総動員（講和で復員） ④国営軽工業を州ごと+1（国庫>建設費×4の節度）
//     ⑤主要輸出財トップ3へ補助金100% ⑥最後通牒は常に受諾（拒否無限の穴） ⑦満足度<0.51で社会保障+0.05（2026-09-25 に0.5から写像）。
// 除外手＝1変数OFF走（寄与の切り分け）。⑥をOFFにすると最後通牒は常に開戦を選ぶ。除外手指定時は対照（試合2）を省略＝除外なし走の試合2と同一なので。
if (args.Length > 0 && args[0] == "noviceprobe")
{
    ulong nvSeed = ArgUlong(args, 1, 1UL);
    string nvNation = args.Length > 2 ? args[2] : "日本";
    var nvOff = new HashSet<int>();
    if (args.Length > 3)
        foreach (var tok in args[3].Split(',', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(tok, out var nvOffV)) nvOff.Add(nvOffV);
    if (Array.FindIndex(WorldData.Nations(), c => c.Name == nvNation) < 0)
    {
        Console.WriteLine($"国名が見つからない: {nvNation}（world.json の Name で指定）");
        return;
    }

    // 1試合を回す。script=true＝初心者スクリプトでプレイヤー国を操作／false＝全AI（対照）。
    // ★出力は**逐次印字+Flush**（2026-08-16）：溜めて最後に吐くと、環境によっては最終フラッシュが失われて
    //   ヘッダーだけのファイルが残る（初版で実際に全損）。行が出た時点で確定保存される形が計器として頑健。
    (List<string> Rows, List<string> Events, int FinalRank, int Nations) NvRun(bool script)
    {
        var cfgs = WorldData.Nations();
        int pi = Array.FindIndex(cfgs, c => c.Name == nvNation);
        var playerCfg = cfgs[pi];
        var sim = script
            ? new MultiNationSim(cfgs, nvSeed, cfg => ReferenceEquals(cfg, playerCfg) ? new NullAgent() : (INationAgent)new UtilityAgent())
            : new MultiNationSim(cfgs, nvSeed, _ => new UtilityAgent());
        foreach (var ns in sim.Nations) ns.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        var pw = sim.Nations[pi].World;
        var power = sim.Diplomacy.Powers[pi];
        if (script)
        {
            power.IsPlayer = true;                 // App と同じ（MainWindowViewModel:82）＝外交AIは自国を代行しない
        }
        sim.StartWarmup();                         // App/自己対戦バッチと同じ助走1本（作者裁定2026-08-10）

        var rows = new List<string>();
        var events = new List<string>();
        int warQ = 0, ultimatums = 0, stateBuilds = 0, crises = 0;
        FactionState? lastCrisis = null;   // ★要求は法の経路：札は危機が立っている間残る＝新しく立った回だけ数える
        void Ev(string s) { events.Add(s); Console.WriteLine("# " + s); Console.Out.Flush(); }   // イベントは起きた瞬間に確定出力

        // ①法の梯子：公教育→自由報道→制限選挙→（以降は純支持が正になりやすい法を1本ずつ）。
        //   目標は Policy.* への代入＝Government.Step の政治ゲートを通る対称機構（UIと同じ）。到達で次へ、
        //   15年（60四半期）通らない法は目標を現状へ戻して断念＝幽霊戦線を残さない（プレイヤーの強みの再現）。
        int ladderIdx = 0, ladderStuck = 0; bool ladderSet = false;
        var ladder = new (string Name, Func<bool> Done, Action Set, Action Close)[]
        {
            ("公教育",       () => pw.EducationLaw >= EducationLaw.Public,        () => pw.Policy.EducationLaw = EducationLaw.Public,        () => pw.Policy.EducationLaw = pw.EducationLaw),
            ("自由報道",     () => pw.Censorship >= CensorshipLaw.FreePress,      () => pw.Policy.Censorship = CensorshipLaw.FreePress,      () => pw.Policy.Censorship = pw.Censorship),
            ("制限選挙",     () => pw.Suffrage >= Suffrage.Census,                () => pw.Policy.Suffrage = Suffrage.Census,                () => pw.Policy.Suffrage = pw.Suffrage),
            ("児童労働禁止", () => !pw.ChildLaborAllowed,                         () => pw.Policy.ChildLaborAllowed = false,                 () => pw.Policy.ChildLaborAllowed = pw.ChildLaborAllowed),
            ("労働時間10h",  () => pw.WorkHours <= WorkHours.Ten,                 () => pw.Policy.WorkHours = WorkHours.Ten,                 () => pw.Policy.WorkHours = pw.WorkHours),
            ("義務教育",     () => pw.EducationLaw >= EducationLaw.Compulsory,    () => pw.Policy.EducationLaw = EducationLaw.Compulsory,    () => pw.Policy.EducationLaw = pw.EducationLaw),
            ("女性財産権",   () => pw.WomenRights >= WomenRightsLaw.Property,     () => pw.Policy.WomenRights = WomenRightsLaw.Property,     () => pw.Policy.WomenRights = pw.WomenRights),
            ("女性教育権",   () => pw.WomenRights >= WomenRightsLaw.Education,    () => pw.Policy.WomenRights = WomenRightsLaw.Education,    () => pw.Policy.WomenRights = pw.WomenRights),
            ("秘密投票",     () => pw.SecretBallot >= SecretBallotLaw.Secret,     () => pw.Policy.SecretBallot = SecretBallotLaw.Secret,     () => pw.Policy.SecretBallot = pw.SecretBallot),
            ("専門官僚制",   () => pw.Bureaucracy >= BureaucracyLaw.Professional, () => pw.Policy.Bureaucracy = BureaucracyLaw.Professional, () => pw.Policy.Bureaucracy = pw.Bureaucracy),
            ("労働時間8h",   () => pw.WorkHours <= WorkHours.Eight,               () => pw.Policy.WorkHours = WorkHours.Eight,               () => pw.Policy.WorkHours = pw.WorkHours),
        };
        void StepLadder()
        {
            while (ladderIdx < ladder.Length && ladder[ladderIdx].Done())
            {
                if (ladderSet) { Ev($"{pw.Year} 法到達: {ladder[ladderIdx].Name}"); ladderSet = false; }
                ladderIdx++; ladderStuck = 0;
            }
            if (ladderIdx >= ladder.Length) return;
            if (!ladderSet) { ladder[ladderIdx].Set(); ladderSet = true; ladderStuck = 0; return; }
            if (++ladderStuck >= 60)   // 15年通らない法は戦線を閉じて次へ（目標=現状＝幽霊戦線を作らない）
            {
                ladder[ladderIdx].Close();
                Ev($"{pw.Year} 法断念: {ladder[ladderIdx].Name}");
                ladderIdx++; ladderSet = false; ladderStuck = 0;
            }
        }

        // ④の候補財＝軽工業（FactorySpec=30,000/4四半期）：綿布・毛織物・絹織物・衣料・家具
        int[] lightGoods = [Catalog.CottonClothId, Catalog.WoolenClothId, Catalog.SilkClothId, Catalog.ClothingId, Catalog.FurnitureId];
        int[]? subsidyGoods = null;   // ⑤：輸出実績が立ってからトップ3を1度だけ選ぶ（初心者が貿易タブを見て決める、の再現）

        for (int q = 0; q < Coefficients.GameQuarters; q++)
        {
            if (script)
            {
                bool atWar = sim.Diplomacy.AtWarAny(power);
                if (atWar) warQ++;
                if (!nvOff.Contains(1)) StepLadder();
                if (!nvOff.Contains(2)) pw.ModernizationFrac = 0.6;   // GameViewModel の ModernizationFrac setter と同じ代入（IsPlayer国はQuarterStartが上書きしない）
                if (!nvOff.Contains(3))
                {
                    if (atWar)
                    {
                        pw.Policy.ArmyUpkeepTarget = 1.0; pw.Policy.NavyUpkeepTarget = 1.0;
                        if (!pw.Mobilized) { pw.Mobilized = true; Ev($"{pw.Year} 交戦中→軍事費全開+総動員"); }
                    }
                    else
                    {
                        pw.Policy.ArmyUpkeepTarget = 0; pw.Policy.NavyUpkeepTarget = 0;
                        // ★段（作者裁定2026-09-12 R2）：軍事費に「なし」は無い＝目標0でも施行は最下段（0.25）。文言は**施行側の値**を名乗る（腕③の意味と表示を一致させる）
                        if (pw.Mobilized) { pw.Mobilized = false; Ev($"{pw.Year} 講和→復員+軍事費{Government.MilitaryTotalTarget(pw, pw.Policy):P0}"); }
                    }
                }
                else if (pw.Mobilized && !atWar) pw.Mobilized = false;   // ③OFFでも復員だけはUI同等（GameViewModel.Refresh の WarRows空判定と同じ）
                if (!nvOff.Contains(4))
                    for (int ri = 0; ri < pw.Regions.Count; ri++)
                    {
                        var r = pw.Regions[ri];
                        if (r.State != RegionState.Active) continue;
                        int bestG = -1; Producer? bestP = null; int bestUnits = int.MaxValue;
                        foreach (int g in lightGoods)
                        {
                            var p = World.FindFactory(r, g);
                            if (p is null || p.ConstructionLeft > 0) continue;
                            int u = pw.StateUnitsOf(p);
                            if (p.StateTarget > u) { bestG = -1; break; }   // この州に未消化の意志あり＝今期は待つ（節度）
                            if (u < bestUnits) { bestUnits = u; bestG = g; bestP = p; }
                        }
                        if (bestG >= 0 && pw.Treasury > Catalog.FactorySpec(bestG).Cost * 4
                            && pw.SetStateTarget(ri, bestG, bestP!.StateTarget + 1)) stateBuilds++;
                    }
                if (!nvOff.Contains(5))
                {
                    if (subsidyGoods is null && pw.ExportsByGood is { } eg)
                    {
                        var top = Enumerable.Range(0, Math.Min(eg.Length, Catalog.Goods.Length))
                            .Where(g => eg[g] > 1e-6 && Catalog.Goods[g].Tier != GoodTier.Service)
                            .OrderByDescending(g => eg[g]).Take(3).ToArray();
                        if (top.Length > 0)
                        {
                            subsidyGoods = top;
                            Ev($"{pw.Year} 補助金ON: " + string.Join("・", top.Select(g => Catalog.Goods[g].Name)));
                        }
                    }
                    if (subsidyGoods is not null)
                        for (int ri = 0; ri < pw.Regions.Count; ri++)
                        {
                            if (pw.Regions[ri].State != RegionState.Active) continue;
                            foreach (int g in subsidyGoods) pw.SetSubsidyRate(ri, g, 1.0);
                        }
                }
                // 手⑦の門：★2026-09-25 MeanSatisfaction の分母を大衆のみへ揃えたので分位の写像で 0.5→0.51（旧分布の下から38.77%・satqmap seed1-3×668Q・2回目0.5102）
                if (!nvOff.Contains(7) && NationalScore.MeanSatisfaction(pw) < 0.51)
                    pw.Policy.SocialSecurityTarget = Math.Min(1.0, pw.Policy.SocialSecurityTarget + 0.05);
            }

            sim.StepQuarter();

            if (script)
            {
                // ポップアップの決着（UIと同じ公開動詞。放置すると時間停止相当の凍結＝それ自体が搾取になるため必ず答える）
                if (power.PendingWar is not null)
                {
                    bool fight = nvOff.Contains(6);
                    Ev($"{pw.Year} 最後通牒({power.PendingWar.Name}): {(fight ? "開戦" : "受諾")}");
                    sim.Diplomacy.ResolvePendingWar(power, fight, pw.Coef);
                    ultimatums++;
                }
                if (power.PendingCallToArms is { } call)
                {
                    Ev($"{pw.Year} 参戦要請({call.Caller.Name})を拒否");
                    sim.Diplomacy.ResolveCallToArms(power, false, pw.Coef);
                }
                if (pw.PendingFactionCrisis is { } crisis)
                {
                    // ★要求は法の経路：初心者の安全策＝法律タブを要求へ向け続ける（AIの Apply と同じ関数＝同じ p.X・同じ GateInfo）
                    if (!ReferenceEquals(crisis, lastCrisis)) { crises++; lastCrisis = crisis; }
                    Parliament.TargetCrisisDemands(pw);
                }
            }

            if ((q + 1) % 20 == 0 || q == Coefficients.GameQuarters - 1)
            {
                int rank = 1; double myS = NationalScore.Standing(pw);
                foreach (var n in sim.Nations) if (n.World != pw && NationalScore.Standing(n.World) > myS) rank++;
                rows.Add($"{pw.Year},{rank},{NationalScore.Of(pw):F1},{NationalScore.Military(pw):F1},{NationalScore.Wealth(pw):F1},{NationalScore.Finance(pw):F1},{NationalScore.Tech(pw):F1},{pw.MeanIndustrialization:F2},{pw.Treasury:F0}");
                Console.WriteLine(rows[^1]); Console.Out.Flush();   // 行は出た瞬間に確定（溜め書きは全損リスク）
            }
        }
        if (script) Ev($"（集計）交戦{warQ}四半期・最後通牒{ultimatums}回・国内危機の通知{crises}回・国営工場+1を{stateBuilds}回");
        int finalRank = int.Parse(rows[^1].Split(',')[1]);
        return (rows, events, finalRank, sim.Nations.Count);
    }

    Console.WriteLine($"=== noviceprobe seed={nvSeed} 国={nvNation}{(nvOff.Count > 0 ? " 除外手=" + string.Join(",", nvOff.OrderBy(x => x)) : "")} ===");
    Console.WriteLine("列＝年,順位,スコア,軍事,富,金融,技術,工業化度,国庫（順位＝NationalScore.Standing の序列・全国家中／スコア内訳＝SelfPlay.cs:19-64／#行＝イベント）");
    Console.WriteLine("--- 試合1: 初心者スクリプト ---");
    Console.WriteLine("年,順位,スコア,軍事,富,金融,技術,工業化度,国庫");
    Console.Out.Flush();
    var nvA = NvRun(script: true);
    if (nvOff.Count > 0)
    {
        Console.WriteLine($"\n最終順位（初心者・除外手{string.Join(",", nvOff.OrderBy(x => x))}）= {nvA.FinalRank}位/{nvA.Nations}カ国（対照は除外手なし走の試合2を参照）");
        return;
    }
    Console.WriteLine("\n--- 試合2: 対照（同シード・全AI） ---");
    Console.WriteLine("年,順位,スコア,軍事,富,金融,技術,工業化度,国庫");
    Console.Out.Flush();
    var nvB = NvRun(script: false);
    Console.WriteLine("\n--- サマリ（順位差＝AI−初心者。正＝初心者が上位） ---");
    Console.WriteLine("年,初心者順位,AI順位,順位差");
    for (int i = 0; i < Math.Min(nvA.Rows.Count, nvB.Rows.Count); i++)
    {
        var a = nvA.Rows[i].Split(','); var b = nvB.Rows[i].Split(',');
        Console.WriteLine($"{a[0]},{a[1]},{b[1]},{int.Parse(b[1]) - int.Parse(a[1])}");
    }
    Console.WriteLine($"最終: 初心者={nvA.FinalRank}位 / 全AI={nvB.FinalRank}位（{nvB.Nations}カ国中・差{nvB.FinalRank - nvA.FinalRank}）");
    return;
}

// === 法の到達年： dotnet run -- lawtiming [シード] ===
// 作者指摘（2026-08-04）「政治資本のたまりが早く、あまりに早期に近代的な法律を取り入れすぎる」の検証。
// 1試合を通して、主要国が各段の法へ**いつ**到達したかを出す。史実の年と並べて読む。
if (args.Length > 0 && args[0] == "lawtiming")
{
    ulong ltSeed = args.Length > 1 && ulong.TryParse(args[1], out var lts) ? lts : 1UL;
    var lsim = new MultiNationSim(WorldData.Nations(), ltSeed, _ => new UtilityAgent());
    lsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    foreach (var ns in lsim.Nations) ns.TelemetryEnabled = false;
    lsim.Diplomacy.Log.Enabled = false;
    string[] watch = ["イギリス", "フランス", "プロイセン", "アメリカ", "ロシア", "日本", "オーストリア", "清"];
    var first = new System.Collections.Generic.Dictionary<string, int>();
    void Mark(string key, int year) { if (!first.ContainsKey(key)) first[key] = year; }
    for (int q = 0; q < Coefficients.GameQuarters; q++)
    {
        lsim.StepQuarter();
        foreach (var ns in lsim.Nations)
        {
            var w = ns.World; string nm = w.SelfPower?.Name ?? "";
            if (System.Array.IndexOf(watch, nm) < 0) continue;
            int y = w.Year;
            if (w.Polity != Polity.Absolutist && w.Polity != Polity.Despotic && w.Polity != Polity.Shogunate) Mark($"{nm}	代議制へ", y);
            if (w.Suffrage >= Suffrage.Census) Mark($"{nm}	制限選挙", y);
            if (w.Suffrage >= Suffrage.AdultMale) Mark($"{nm}	男性普通選挙", y);
            if (w.Suffrage >= Suffrage.Universal) Mark($"{nm}	男女普通選挙", y);
            if (w.EducationLaw >= EducationLaw.Public) Mark($"{nm}	公教育", y);
            if (w.EducationLaw >= EducationLaw.Compulsory) Mark($"{nm}	義務教育", y);
            if (w.SocialSecurityLaw >= SocialSecurityLaw.Unemployment) Mark($"{nm}	失業保険", y);
            if (w.Bureaucracy >= BureaucracyLaw.MeritExam) Mark($"{nm}	資格任用", y);
            if (w.SecretBallot >= SecretBallotLaw.Secret) Mark($"{nm}	秘密投票", y);
        }
    }
    Console.WriteLine($"=== 法の到達年（seed{ltSeed}・1783-1934）※史実＝英:制限1832/男子普選1918/義務教育1880/秘密投票1872、普:義務教育1763、米:男子普選1828 ===");
    string[] order = ["代議制へ", "制限選挙", "男性普通選挙", "男女普通選挙", "公教育", "義務教育", "資格任用", "秘密投票"];
    Console.WriteLine("国	" + string.Join("	", order));
    foreach (var nm in watch)
    {
        var cells = new System.Collections.Generic.List<string>();
        foreach (var k in order) cells.Add(first.TryGetValue($"{nm}	{k}", out int y) ? y.ToString() : "—");
        Console.WriteLine(nm + "	" + string.Join("	", cells));
    }
    return;
}

// === 多国での法の機能検査： dotnet run -- lawaudit2 [四半期] ===
// ⚠なぜ2本目が要るか（2026-08-04）：lawaudit は**単一国**（InternationalMarket なし）で走るので、
//   穀物法・関税など**貿易を通じてしか効かない法**は構造上ゼロと出る（＝機構が死んでいる証拠にならない）。
//   実測でも穀物法が「麦価 0.00% ✗無反応」と出たが、係数（CornLawImportPenalty/CornLawGrainChainGate）は両方1.0で生きていた。
//   ここでは**実世界（WorldData の全ロスター・貿易あり）**で英の法だけを毎四半期ピン留めして A/B を取る＝法が世界に与える実効を測る。
// ⚠指標も是正：参政権は満足でなく**有権者比**（誰が票を持つか）・軍国は兵数でなく軍事費と兵数の両方、等。
if (args.Length > 0 && args[0] == "lawaudit2")
{
    int la2Q = args.Length > 1 && int.TryParse(args[1], out var la2q) ? la2q : 120;
    static PriceOfBread.SimCore.World La2Run(int quarters, Action<PriceOfBread.SimCore.World> pin)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
        sim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
        foreach (var ns in sim.Nations) ns.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        var gb = sim.Nations.First(n => n.World.SelfPower?.Name == "イギリス").World;
        gb.PoliticalLockEnabled = false;   // 法が目標へ自由に動く（機構効果のみを測る＝lawaudit と同条件）
        for (int q = 0; q < quarters; q++) { pin(gb); sim.StepQuarter(); }   // 毎四半期ピン留め＝AIに戻されない
        pin(gb);
        return gb;
    }
    static double La2Staple(PriceOfBread.SimCore.World w)
    {   // 主食の価格（パン）＝題名の指標そのもの
        for (int i = 0; i < Catalog.Goods.Length; i++)
            if (Catalog.Goods[i].Name == "パン" && Catalog.Goods[i].Id < w.Region.Markets.Length) return w.Region.Markets[Catalog.Goods[i].Id].Price;
        return 0;
    }
    static double La2Grain(PriceOfBread.SimCore.World w)
    {
        for (int i = 0; i < Catalog.Goods.Length; i++)
            if (Catalog.Goods[i].Name == "麦" && Catalog.Goods[i].Id < w.Region.Markets.Length) return w.Region.Markets[Catalog.Goods[i].Id].Price;
        return 0;
    }
    static double La2Voters(PriceOfBread.SimCore.World w)
    {
        double t = 0; foreach (var b in Parliament.SupportBase(w)) t += b.Voters;
        return w.TotalPopulation > 1e-9 ? t / w.TotalPopulation : 0;
    }
    var la2 = new System.Collections.Generic.List<(string, string, double, double)>();
    string la2Filter = args.Length > 2 ? args[2] : "";   // 任意：名前で絞る（全行は重いので調査時は1本ずつ）
    void L2(string name, string metric, Action<PriceOfBread.SimCore.World> lo, Action<PriceOfBread.SimCore.World> hi, Func<PriceOfBread.SimCore.World, double> m)
    {
        if (la2Filter.Length > 0 && !name.Contains(la2Filter)) return;
        la2.Add((name, metric, m(La2Run(la2Q, lo)), m(La2Run(la2Q, hi))));
    }

    L2("穀物法", "パン価", w => { w.CornLaws = ReformLevel.None; w.Policy.CornLaws = ReformLevel.None; },
                            w => { w.CornLaws = ReformLevel.Full; w.Policy.CornLaws = ReformLevel.Full; }, La2Staple);
    L2("穀物法", "麦価", w => { w.CornLaws = ReformLevel.None; w.Policy.CornLaws = ReformLevel.None; },
                           w => { w.CornLaws = ReformLevel.Full; w.Policy.CornLaws = ReformLevel.Full; }, La2Grain);
    L2("関税", "歳入", w => w.Policy.TariffTarget = 0.0, w => w.Policy.TariffTarget = 0.5, w => w.LastRevenue);
    L2("関税", "パン価", w => w.Policy.TariffTarget = 0.0, w => w.Policy.TariffTarget = 0.5, La2Staple);
    L2("参政権", "有権者比", w => { w.Suffrage = Suffrage.None; w.Policy.Suffrage = Suffrage.None; },
                              w => { w.Suffrage = Suffrage.AdultMale; w.Policy.Suffrage = Suffrage.AdultMale; }, La2Voters);
    L2("労働時間", "GDP", w => { w.WorkHours = WorkHours.Eight; w.Policy.WorkHours = WorkHours.Eight; },
                            w => { w.WorkHours = WorkHours.Unlimited; w.Policy.WorkHours = WorkHours.Unlimited; }, w => w.Gdp);
    L2("労働時間", "満足", w => { w.WorkHours = WorkHours.Eight; w.Policy.WorkHours = WorkHours.Eight; },
                             w => { w.WorkHours = WorkHours.Unlimited; w.Policy.WorkHours = WorkHours.Unlimited; }, w => NationalScore.MeanSatisfaction(w));
    L2("土地改革", "GDP", w => { w.LandReform = ReformLevel.None; w.Policy.LandReform = ReformLevel.None; },
                            w => { w.LandReform = ReformLevel.Full; w.Policy.LandReform = ReformLevel.Full; }, w => w.Gdp);
    L2("社会保障", "満足", w => { w.SocialSecurity = 0; w.Policy.SocialSecurityTarget = 0; },
                             w => { w.SocialSecurity = 0.8; w.Policy.SocialSecurityTarget = 0.8; }, w => NationalScore.MeanSatisfaction(w));
    L2("児童労働", "GDP", w => w.Policy.ChildLaborAllowed = false, w => w.Policy.ChildLaborAllowed = true, w => w.Gdp);
    L2("検閲", "識字", w => { w.Censorship = CensorshipLaw.FreePress; w.Policy.Censorship = CensorshipLaw.FreePress; },
                         w => { w.Censorship = CensorshipLaw.StatePressOnly; w.Policy.Censorship = CensorshipLaw.StatePressOnly; }, w => PoliticalQuantities.AvgLiteracy(w));   // 識字＝正典の1本（旧 La2Lit は分離州も数えていた・2026-09-11 M8）
    // ★2026-08-18（親軍法の撤去）：旧「軍国主義」の行を徴兵法へ差し替え。軍国度は法でなく導出量（軍事費×徴兵法）なので、
    //   法として測れるのは徴兵法だけ＝志願制↔皆兵の兵数への効きを測る。
    L2("徴兵法", "兵数", w => { w.ConscriptionLaw = ConscriptionLaw.Volunteer; w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer; },
                          w => { w.ConscriptionLaw = ConscriptionLaw.Universal; w.Policy.ConscriptionLaw = ConscriptionLaw.Universal; }, w => w.SoldierSize());

    // ★政体そのものの効き（2026-08-04・作者指摘「政治がゲームに与える影響が小さすぎる」）。
    //   法は効くと実測できたので、次に問うべきは「**誰が統治するか**が国に何をもたらすか」。
    //   国力スコアは 軍事+豊かさ+金融+技術 で政治の項が1つも無い＝政治は経済を経由してしか効かない設計。
    //   その経路が実際どれだけ太いのかを、政体を固定したA/Bで測る。
    L2("政体", "GDP", w => { w.Polity = Polity.Absolutist; }, w => { w.Polity = Polity.Republic; }, w => w.Gdp);
    L2("政体", "歳入", w => { w.Polity = Polity.Absolutist; }, w => { w.Polity = Polity.Republic; }, w => w.LastRevenue);
    L2("政体", "満足", w => { w.Polity = Polity.Absolutist; }, w => { w.Polity = Polity.Republic; }, w => NationalScore.MeanSatisfaction(w));
    L2("政体", "技術", w => { w.Polity = Polity.Absolutist; }, w => { w.Polity = Polity.Republic; }, w => w.TechLevel);
    L2("政体", "国力", w => { w.Polity = Polity.Absolutist; }, w => { w.Polity = Polity.Republic; }, NationalScore.Of);
    // 参政権を広げると国は強くなるのか（＝民主化は割に合うのか）。史実＝英仏の財政国家論の中心問題。
    L2("参政権拡大", "正統性", w => { w.Suffrage = Suffrage.None; w.Policy.Suffrage = Suffrage.None; },
                                w => { w.Suffrage = Suffrage.AdultMale; w.Policy.Suffrage = Suffrage.AdultMale; }, w => w.Legitimacy);
    L2("参政権拡大", "歳入", w => { w.Suffrage = Suffrage.None; w.Policy.Suffrage = Suffrage.None; },
                               w => { w.Suffrage = Suffrage.AdultMale; w.Policy.Suffrage = Suffrage.AdultMale; }, w => w.LastRevenue);
    L2("参政権拡大", "国力", w => { w.Suffrage = Suffrage.None; w.Policy.Suffrage = Suffrage.None; },
                               w => { w.Suffrage = Suffrage.AdultMale; w.Policy.Suffrage = Suffrage.AdultMale; }, NationalScore.Of);
    L2("政体", "正統性", w => { w.Polity = Polity.Absolutist; }, w => { w.Polity = Polity.Republic; }, w => w.Legitimacy);

    L2("正統性", "歳入", w => { w.Legitimacy = 0.2; }, w => { w.Legitimacy = 1.0; }, w => w.LastRevenue);
    L2("正統性", "国力", w => { w.Legitimacy = 0.2; }, w => { w.Legitimacy = 1.0; }, NationalScore.Of);

    Console.WriteLine($"=== 多国での法の機能検査（A/B対照・英・{la2Q}四半期・{WorldData.Nations().Length}カ国/貿易あり）===");
    foreach (var (name, metric, a, b) in la2)
    {
        double rel = Math.Abs(a) > 1e-9 ? Math.Abs(b - a) / Math.Abs(a) : (Math.Abs(b) > 1e-9 ? 1.0 : 0.0);
        string v = rel > 0.05 ? "✓機能" : rel > 0.005 ? "△微小" : "✗無反応";
        Console.WriteLine($"  {name,-10} {metric,-8} low={a,13:F3} high={b,13:F3} 差={rel * 100,7:F2}% {v}");
    }
    return;
}

// === スライダー・法律の機能検査： dotnet run -- lawaudit ===
// 各レバーを low/high の A/B対照で回し、関連指標の差で「機能/微小/無反応」を判定。法律はWorld値を直接設定（政治ゲートを迂回＝機構効果のみを測る）。
if (args.Length > 0 && args[0] == "lawaudit")
{
    static PriceOfBread.SimCore.World RunCfg(Action<PriceOfBread.SimCore.World> cfg)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(7), new NullAgent());
        var w = sim.World;
        w.PoliticalLockEnabled = false;   // スライダー/法が目標へ自由に動く（機能検査＝政治抵抗と分離）
        w.CoercionFollowsForce = true;    // 実ゲームと同じ武力統合（軍事費→治安維持力）
        w.MonetaryEnabled = true;         // 金本位の機構を有効化
        var p = w.Policy;
        // 共通base：行政成熟＋中庸の税で余剰を作る（福祉/軍需/インフラが資金化され効果が出る）。各probeは自分のレバーだけ上書き。
        p.AdminSpendingTarget = 0.6;
        p.IncomeTaxTarget = 0.3; p.PollTaxTarget = 0.4; p.LandTaxTarget = 0.3;
        w.SetStateMonopolyTier(1);   // ★2026-09-20：専売は率でなく対象の段（1＝史実の物品税の財〔2026-09-30 (127) の梯子〕＝旧0.2の翻訳）
        p.SocialSecurityTarget = 0.3; p.EducationSpendingTarget = 0.5; p.MilitarySpendingTarget = 0.3;
        w.EducationLaw = EducationLaw.Public; p.EducationLaw = EducationLaw.Public;
        cfg(w);
        sim.Run(240);
        return w;
    }
    var rows = new System.Collections.Generic.List<(string, string, double, double)>();
    void P(string name, string metric, Action<PriceOfBread.SimCore.World> lo, Action<PriceOfBread.SimCore.World> hi, Func<PriceOfBread.SimCore.World, double> m)
        => rows.Add((name, metric, m(RunCfg(lo)), m(RunCfg(hi))));

    P("所得税", "歳入", w => w.Policy.IncomeTaxTarget = 0, w => w.Policy.IncomeTaxTarget = 0.6, w => w.LastRevenue);
    P("人頭税", "歳入", w => w.Policy.PollTaxTarget = 0, w => w.Policy.PollTaxTarget = 0.8, w => w.LastRevenue);
    P("専売", "歳入", w => w.SetStateMonopolyTier(0), w => w.SetStateMonopolyTier(PriceOfBread.SimCore.World.StateMonopolyMaxTier), w => w.LastRevenue);   // ★2026-09-20：腕は率でなく対象の広さ（無課税↔全消費財）
    P("土地税", "歳入", w => w.Policy.LandTaxTarget = 0, w => w.Policy.LandTaxTarget = 0.8, w => w.LastRevenue);
    P("社会保障", "満足", w => { w.SocialSecurity = 0; w.Policy.SocialSecurityTarget = 0; }, w => { w.SocialSecurity = 0.8; w.Policy.SocialSecurityTarget = 0.8; }, w => NationalScore.MeanSatisfaction(w));
    P("教育費", "識字", w => { w.EducationLaw = EducationLaw.Compulsory; w.Policy.EducationLaw = EducationLaw.Compulsory; w.Policy.EducationSpendingTarget = 0; }, w => { w.EducationLaw = EducationLaw.Compulsory; w.Policy.EducationLaw = EducationLaw.Compulsory; w.Policy.EducationSpendingTarget = 1.0; }, w => PoliticalQuantities.AvgLiteracy(w));
    P("武力(軍事費)", "兵数", w => w.Policy.MilitarySpendingTarget = 0.1, w => w.Policy.MilitarySpendingTarget = 0.9, w => w.SoldierSize());
    P("武力→治安維持力", "治安維持力", w => w.Policy.MilitarySpendingTarget = 0.1, w => w.Policy.MilitarySpendingTarget = 0.9, w => w.BaseCoercion);
    P("関税", "歳入", w => w.Policy.TariffTarget = 0, w => w.Policy.TariffTarget = 0.8, w => w.LastRevenue);
    P("金利", "GDP", w => w.Policy.InterestRateTarget = 0.45, w => w.Policy.InterestRateTarget = 0.05, w => w.Gdp);
    P("行政費", "行政効率", w => w.Policy.AdminSpendingTarget = 0.1, w => w.Policy.AdminSpendingTarget = 0.9, w => w.AdminEfficiency);
    P("教育法", "識字", w => { w.EducationLaw = EducationLaw.Religious; w.Policy.EducationLaw = EducationLaw.Religious; }, w => { w.EducationLaw = EducationLaw.Public; w.Policy.EducationLaw = EducationLaw.Public; }, w => PoliticalQuantities.AvgLiteracy(w));
    // ★女性の権利（統合法・2026-08-10）：経済への効き（労働参加0.4→0.7）は**財産権の段**が担う＝旧プローブと同じ量を測り続ける
    P("女性の権利（財産権）", "GDP", w => { w.WomenRights = WomenRightsLaw.None; w.Policy.WomenRights = WomenRightsLaw.None; }, w => { w.WomenRights = WomenRightsLaw.Property; w.Policy.WomenRights = WomenRightsLaw.Property; }, w => w.Gdp);
    // 軍国は軍事力（戦力評価＝Diplomacy.Step の同期）に出る。単一国検査は player の軍事力を同期しないため測れない＝多国 selfplay で検証（均衡保持を確認済み）。
    // ★2026-08-18（親軍法の撤去）：軍国度は導出量＝法として押せるのは徴兵法だけ。旧「軍国」行はこれに置き換える。
    P("徴兵法", "兵数", w => { w.ConscriptionLaw = ConscriptionLaw.Volunteer; w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer; }, w => { w.ConscriptionLaw = ConscriptionLaw.Universal; w.Policy.ConscriptionLaw = ConscriptionLaw.Universal; }, w => w.SoldierSize());
    P("検閲", "技術", w => { w.Censorship = CensorshipLaw.FreePress; w.Policy.Censorship = CensorshipLaw.FreePress; }, w => { w.Censorship = CensorshipLaw.StatePressOnly; w.Policy.Censorship = CensorshipLaw.StatePressOnly; }, w => w.TechLevel);
    P("労働組合", "満足", w => { w.LaborUnion = LaborUnionLaw.Illegal; w.Policy.LaborUnion = LaborUnionLaw.Illegal; }, w => { w.LaborUnion = LaborUnionLaw.Tolerated; w.Policy.LaborUnion = LaborUnionLaw.Tolerated; }, w => NationalScore.MeanSatisfaction(w));
    P("穀物法", "麦価", w => { w.CornLaws = ReformLevel.None; w.Policy.CornLaws = ReformLevel.None; }, w => { w.CornLaws = ReformLevel.Full; w.Policy.CornLaws = ReformLevel.Full; }, w => w.Region.Markets[Catalog.WheatId].Price);
    P("土地改革", "GDP", w => { w.LandReform = ReformLevel.None; w.Policy.LandReform = ReformLevel.None; }, w => { w.LandReform = ReformLevel.Full; w.Policy.LandReform = ReformLevel.Full; }, w => w.Gdp);
    P("農奴解放", "GDP", w => { w.SerfEmancipation = ReformLevel.None; w.Policy.SerfEmancipation = ReformLevel.None; }, w => { w.SerfEmancipation = ReformLevel.Full; w.Policy.SerfEmancipation = ReformLevel.Full; }, w => w.Gdp);
    P("児童労働", "GDP", w => w.Policy.ChildLaborAllowed = false, w => w.Policy.ChildLaborAllowed = true, w => w.Gdp);
    P("労働時間", "GDP", w => w.Policy.WorkHours = WorkHours.Eight, w => w.Policy.WorkHours = WorkHours.Twelve, w => w.Gdp);
    // ★2026-08-13：Policy（願望）だけを書いていたため「✗無反応」と出ていた。他の行（土地改革・農奴解放）と同じく
    //   **実体（政治を通った現実＝GoldStandardLaw）も書く**。Fiscal が読むのは後者（World.GoldStandardLaw）。
    P("金本位", "兌換", w => { w.Policy.MonetaryTarget = 0; w.MonetaryLaw = MonetaryStandardLaw.Fiat; },
                       w => { w.Policy.MonetaryTarget = 2; w.MonetaryLaw = MonetaryStandardLaw.Gold; }, w => w.Convertibility);
    P("保険", "保険補填", w => { w.SocialSecurity = 0; w.Policy.SocialSecurityTarget = 0; }, w => { w.SocialSecurity = 0.8; w.Policy.SocialSecurityTarget = 0.8; }, w => w.Region.InsuranceCoverage);
    P("参政権", "満足", w => { w.Suffrage = Suffrage.None; w.Policy.Suffrage = Suffrage.None; }, w => { w.Suffrage = Suffrage.AdultMale; w.Policy.Suffrage = Suffrage.AdultMale; }, w => NationalScore.MeanSatisfaction(w));

    Console.WriteLine("=== スライダー・法律の機能検査（A/B対照・英・240四半期）===");
    foreach (var (name, metric, a, b) in rows)
    {
        double rel = Math.Abs(a) > 1e-9 ? Math.Abs(b - a) / Math.Abs(a) : (Math.Abs(b) > 1e-9 ? 1.0 : 0.0);
        string v = rel > 0.05 ? "✓機能" : rel > 0.005 ? "△微小" : "✗無反応";
        Console.WriteLine($"  {name,-12} {metric,-8} low={a,13:F3} high={b,13:F3} 差={rel * 100,7:F2}% {v}");
    }
    return;
}

// === ハングseedの四半期特定： dotnet run -- hangtest [seed] ===
if (args.Length > 0 && args[0] == "hangtest")
{
    ulong sd = ArgUlong(args, 1, 4);
    NationConfig[] cfgs = [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(), NationConfig.Austria(),
        NationConfig.Netherlands(), NationConfig.Iberia(), NationConfig.Sardinia(),
    ];
    var hs = new MultiNationSim(cfgs, sd, _ => new UtilityAgent());
    for (int q = 0; q < 400; q++) hs.StepQuarter();
    int wars = hs.Diplomacy.Log.Count(l => l.Contains("開戦"));
    int civilWars = hs.Nations.Count(n => n.World.HadCivilWar);
    int defaults = hs.Nations.Count(n => n.World.HadSovereignDefault);
    Console.WriteLine($"seed {sd}: 開戦{wars}回 内戦経験{civilWars}国 デフォルト経験{defaults}国");
    foreach (var n in hs.Nations) { var w = n.World; double ir = w.LastRevenue > 0 ? w.LastInterest / w.LastRevenue : 0; if (w.HadSovereignDefault || ir > 0.25 * w.Coef.MaxInterestShareOfRevenue) Console.WriteLine($"  {w.Region.Name,-10} 利払い/歳入{ir,5:F2} 債務/GDP{(w.NominalGdp > 1 ? w.GovDebt / w.NominalGdp : 0),5:F2} default={w.HadSovereignDefault} cooldown{w.BorrowCooldown}"); }
    if (args.Length > 2 && args[2] == "log")
        foreach (var l in hs.Diplomacy.Log.Where(l => l.Contains("開戦") || l.Contains("講和") || l.Contains("独立"))) Console.WriteLine("  " + l);
    return;
}

// === 各国の初期法（史実差別化）の確認： dotnet run -- initlaws ===
if (args.Length > 0 && args[0] == "initlaws")
{
    NationConfig[] cfgs = [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(), NationConfig.Austria(),
        NationConfig.Netherlands(), NationConfig.Iberia(), NationConfig.Sardinia(),
    ];
    Console.WriteLine("=== 各国の初期法（史実1783・構築直後）===");
    Console.WriteLine($"  {"国",-12} {"関税",5} {"穀物法",-8} {"農奴解放",-8} {"参政権",-10} {"報道",-14} {"教育",-10} 軍政比");
    foreach (var c in cfgs)
    {
        var w = new Simulation(c.ToScenario(1), new NullAgent()).World;
        Console.WriteLine($"  {c.Name,-12} {w.Tariff,5:F2} {w.CornLaws,-8} {w.SerfEmancipation,-8} {w.Suffrage,-10} {w.Censorship,-14} {w.EducationLaw,-10} {PoliticalQuantities.MilitaryPoliticalRatio(w):F2}");   // ★2026-08-18：親軍法の撤去＝導出量（軍の政治的優位）
    }
    return;
}

// === 単独日本の行政崩壊の追跡： dotnet run -- soloadmin ===
if (args.Length > 0 && args[0] == "soloadmin")
{
    var ssim = new Simulation(NationConfig.Japan().ToScenario(11), new NullAgent());
    ssim.World.Policy.AdminSpendingTarget = 0.8;
    ssim.World.Policy.LandTaxTarget = 1.0;
    for (int q = 0; q < 40; q++)
    {
        ssim.StepQuarter();
        if (q % 4 != 3) continue;
        var w = ssim.World; var r0 = w.Region;
        var bur = r0.Pops[8]; var mer = r0.Pops[10];
        Console.WriteLine($"{w.Year}: 国庫{w.Treasury,10:F0} 行政費{w.AdminSpending:F2} 行政力{w.AdminPowerSmoothed:F2} 充足{r0.AdminFill:P0} 行政価格{r0.Markets[Catalog.AdminId].Price:F2} 官僚{bur.Size:F0}（貯蓄/人{bur.Savings / Math.Max(1, bur.Size):F0}） 商人{mer.Size:F0}（{mer.Savings / Math.Max(1, mer.Size):F0}）");
    }
    return;
}

// === 死蔵財の特定： dotnet run -- deadend [seed] ===
if (args.Length > 0 && args[0] == "deadend")
{
    ulong seed = ArgUlong(args, 1, 2);
    NationConfig[] dc = [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman()];
    var ds = new MultiNationSim(dc, seed, _ => new UtilityAgent());
    ds.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < 400; q++) ds.StepQuarter();
    Console.WriteLine($"=== 死蔵財 seed={seed} ===");
    Console.WriteLine("各国 技術: " + string.Join("  ", ds.Nations.Select(n => $"{n.World.Region.Name}{n.World.TechLevel:F1}")));
    foreach (var g in Catalog.Goods)
    {
        if (g.IsColonial) continue;
        double maxD = 0, maxS = 0;
        foreach (var n in ds.Nations) { maxD = Math.Max(maxD, n.World.Region.Markets[g.Id].LastDemand); maxS = Math.Max(maxS, n.World.Region.Markets[g.Id].LastProduced); }
        if (maxD < 0.5) Console.WriteLine($"  死蔵: {g.Name}(id{g.Id}) 最大需要{maxD:F2} 最大生産{maxS:F2}");
    }
    return;
}

// === 建設コスト妥当性テスト： dotnet run -- cost-test ===
if (args.Length > 0 && args[0] == "cost-test")
{
    static Scenario Fr() => new() { Seed = 7, StartYear = 1783, Kind = ScenarioKind.France, PoliticsEnabled = true, InitialPolity = Polity.Absolutist, FiscalEnabled = true, TaxRate = 0.20, InitialTreasury = 200_000, BaseCoercion = 1500, EducationLaw = EducationLaw.Public, ExternalPressure = 0.1 };

    // 定常状態の財政（建設しない場合の四半期黒字）を測る
    var b = new Simulation(Fr(), new NullAgent());
    b.Run(40);
    double t0 = b.World.Treasury, g0 = b.World.Gdp;
    b.Run(16);
    double surplusPerQ = (b.World.Treasury - t0) / 16.0;
    Console.WriteLine($"=== 建設コスト妥当性テスト（フランス・税20%）===");
    Console.WriteLine($"定常：国庫 {b.World.Treasury:N0}　四半期黒字 {surplusPerQ:N0}/Q　GDP {b.World.Gdp:N0}\n");

    Console.WriteLine($"{"財",-8}{"カテゴリ",-7}{"総額",8}{"期間",6}{"/四半期",9}{"黒字比",8}{"国庫比",8}");
    int[] reps = { Catalog.ClothingId, Catalog.SteelId, Catalog.RifleId, Catalog.MachineryId, Catalog.ApplianceId };   // 衣料/鋼/小銃/機械/電化製品（各カテゴリ代表）
    foreach (int gid in reps)
    {
        var s = Catalog.FactorySpec(gid);
        double payPerQ = s.Cost / s.Quarters;
        string surplusRatio = surplusPerQ > 1 ? $"{payPerQ / surplusPerQ:F1}倍" : "—";
        Console.WriteLine($"{Catalog.Goods[gid].Name,-8}{s.Category,-7}{s.Cost / 1000,6:F0}k{s.Quarters / 4.0,5:F1}年{payPerQ / 1000,7:F1}k{surplusRatio,8}{s.Cost / 200000.0,7:P0}");
    }

    // 鋼工場を実建設して国庫・工業化への影響を見る
    var s2 = new Simulation(Fr(), new NullAgent());
    s2.Run(40);
    double tb = s2.World.Treasury, indb = s2.World.Region.TotalIndustrialization;
    var steel = s2.World.Regions[0].Producers.Find(p => p.GoodId == Catalog.SteelId)!;
    s2.World.BuildFactory(0, Catalog.SteelId);
    double minT = tb;
    int guard = 0; while (steel.ConstructionLeft > 0 && guard++ < 40) { s2.Run(1); minT = Math.Min(minT, s2.World.Treasury); }
    double tDone = s2.World.Treasury;
    s2.Run(40);
    Console.WriteLine($"\n鋼工場の実建設（70k・2年）:");
    Console.WriteLine($"  着工前国庫 {tb:N0} → 建設中最低 {minT:N0} → 完成時 {tDone:N0} → 10年後 {s2.World.Treasury:N0}");
    Console.WriteLine($"  完成後の鋼工場 稼働{steel.Scale * 100:F0}% 四半期損益{steel.LastRevenue - steel.LastCost:N0}　工業化 {indb:F2}→{s2.World.Region.TotalIndustrialization:F2}");
    return;
}

// === 総合モニタ： dotnet run -- monitor [seed] ===（破綻/極端価格/政権遷移/戦争/植民地移譲/運河/輸送/技術登場）
if (args.Length > 0 && args[0] == "monitor")
{
    ulong mseed = ArgUlong(args, 1, 1UL);
    NationConfig[] mcfgs =
    [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
        NationConfig.Austria(), NationConfig.Netherlands(), NationConfig.Iberia(), NationConfig.Sardinia(),
    ];
    var msim = new MultiNationSim(mcfgs, mseed, _ => new UtilityAgent());
    int MQ = Coefficients.GameQuarters;
    int MYear(int q) => 1783 + q / 4;
    var adv = new Dictionary<int, string> { { Catalog.MachineGunId, "機関銃" }, { Catalog.IroncladId, "装甲艦" }, { Catalog.ApplianceId, "電化製品" }, { Catalog.ElectricityId, "電力" }, { Catalog.GridId, "送電網" } };
    var firstYear = new Dictionary<int, int>();
    var polPrev = new Polity[mcfgs.Length];
    int polTrans = 0; bool mbroke = false; int mbrokeQ = -1; int mlogSeen = 0;
    int mWars = 0, mConq = 0; var mCanals = new List<string>();

    for (int q = 0; q < MQ; q++)
    {
        try { msim.StepQuarter(); }
        catch (Exception) { mbroke = true; mbrokeQ = q; break; }
        for (int n = 0; n < msim.Nations.Count; n++)
        {
            var w = msim.Nations[n].World;
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                    if (adv.ContainsKey(p.GoodId) && p.Output > 1.0 && !firstYear.ContainsKey(p.GoodId)) firstYear[p.GoodId] = MYear(q);
            if (q > 0 && n < polPrev.Length && w.Polity != polPrev[n]) polTrans++;   // 分離独立/建国で Nations が polPrev(固定長)を超え得る＝既存国のみ政変計上（越境で IndexOutOfRange しない）
            if (n < polPrev.Length) polPrev[n] = w.Polity;
        }
        var lg = msim.Diplomacy.Log;
        for (; mlogSeen < lg.Count; mlogSeen++)
        {
            var l = lg[mlogSeen];
            if (l.Contains("開戦")) mWars++;
            if (l.Contains("が開通")) mCanals.Add(l);   // 2026-07-28：ログ文言の「運河運河」重複を是正したため照合語も追随
            if (l.Contains("勝利") && l.Contains("植民地+")) mConq++;
        }
    }

    Console.WriteLine($"=== 総合モニタ seed {mseed}：{(mbroke ? $"★破綻 Q{mbrokeQ}（{MYear(mbrokeQ)}年）" : $"完走 {MQ}Q→{MYear(MQ)}年")} ===");
    Console.WriteLine($"1) 経済破綻: {(mbroke ? "★あり" : "なし")}");
    {
        int fl = 0, ce = 0; var ex = new List<string>();
        for (int g = 0; g < msim.Market.Price.Count; g++)
        {
            if (Catalog.Goods[g].Tier == GoodTier.Service) continue;
            double ratio = msim.Market.Price[g] / Math.Max(1e-9, Catalog.RefPrices[g]);
            if (ratio <= 0.12) { fl++; ex.Add($"{Catalog.Goods[g].Name}↓"); }
            else if (ratio >= 8.0) { ce++; ex.Add($"{Catalog.Goods[g].Name}↑"); }
        }
        Console.WriteLine($"2) 極端な世界価格: 床(≤0.12x){fl} 天井(≥8x){ce}　{string.Join(" ", ex)}");
    }
    Console.WriteLine($"3) 政権遷移: {polTrans}回　最終政体: {string.Join("/", msim.Nations.Select(n => n.World.Polity))}");
    Console.WriteLine($"4) 戦争（開戦）回数: {mWars}（{MYear(MQ) - 1783}年間）");
    Console.WriteLine($"5) 植民地の移譲（決定的勝利）: {mConq}回");
    Console.WriteLine($"6) 運河の開通: {(mCanals.Count == 0 ? "なし" : string.Join(" / ", mCanals))}");
    {
        // 輸送：交易スロットル（1=容量十分/余剰、<1=容量不足）。容量/目標の列は撤去（目標は Transport.Step のローカル＝脅威/混雑の乗数込みで再導出できない・2026-09-11 #14）
        double thr = 0; int rc = 0;
        foreach (var nn2 in msim.Nations)
            foreach (var r in nn2.World.Regions) if (r.State == RegionState.Active) { thr += r.LastTradeThrottle; rc++; }
        Console.WriteLine($"7) 輸送: 平均交易スロットル {(rc > 0 ? thr / rc : 1):F2}（1=余裕/<1=不足）");
    }
    {
        var sb = new List<string>();
        foreach (var kv in adv) sb.Add(firstYear.TryGetValue(kv.Key, out int y) ? $"{kv.Value}={y}" : $"{kv.Value}=未登場");
        Console.WriteLine($"8) 技術（先進財）の最速登場年: {string.Join(" / ", sb)}");
    }
    Console.WriteLine($"参考) 運河の現状: {string.Join(" ", msim.Diplomacy.Chokepoints.Select(cp => $"{cp.Name}={(cp.IsOpen ? "開" : "未")}{(cp.Owner is { } o ? $"[{o.Name}]" : "")}"))}");
    return;
}

// === バッチ自己対戦： dotnet run -- selfplay-batch [N] ===
if (args.Length > 0 && args[0] == "selfplay-batch")
{
    int n = ArgInt(args, 1, 30);
    double? ir = ArgDouble(args, 2, null);   // 任意：初期金利の上書き（信用バブル感度の実験）
    int par = ArgInt(args, 3, 0);            // 任意：並列度の明示（0=既定のOOMガード値clamp(コア数,2,max(2,N))。RAM余裕機で 8〜10 等へ＝結果はビット一致）
    ulong seedBase = ArgUlong(args, 4, 1UL); // ★任意（2026-08-14）：シード起点。11 で seeds 11..11+N-1＝**同一ソースのノイズ床**を測る（既定1＝従来）
    int gqArg = ArgInt(args, 5, 0);          // ★任意（2026-08-30・測定高速化）：四半期数の上書き。探索は 300（1858年まで＝半分の時間）・受け入れは既定 GameQuarters。
                                             //   ⚠300Q の帯は全長の帯と別の物差し（後期次元＝航空/WWI が無測定・断面数も減る）＝同じ長さの腕同士でだけ比較する
    int gq = gqArg > 0 ? gqArg : PriceOfBread.SimCore.Coefficients.GameQuarters;
    var costSys0 = CostSys.Snap();   // ★計算量の測り（2026-09-30）：系の稼働（負荷の記録）の始点
    var b = SelfPlayMatch.RunBatch(n, gq, ir, par > 0 ? par : null, seedBase);
    // ★計算量の測り（2026-09-30 作者裁定「計算量の予算」）＝帯の報告とは**別のファイル**（POB_COST_OUT・measure.sh が <出力>.cost を渡す）。
    //   ⚠報告の本体には1文字も足さない＝恒等の検証（報告の全行バイト一致）を壊さない（CPU 秒は時刻と負荷で揺れる・件数は軌道で動く）。
    if (Environment.GetEnvironmentVariable("POB_COST_OUT") is { Length: > 0 } costOut)
        File.WriteAllText(costOut, CostSys.Report(b, costSys0, seedBase, gq));
    Console.WriteLine($"=== バッチ自己対戦 seeds {seedBase}..{seedBase + (ulong)b.N - 1}（各{gq}四半期{(ir is { } v ? $"・金利{v:F2}" : "")}）===");
    if (PriceOfBread.SimCore.Coefficients.OverrideSummary() is { Length: > 0 } ov)
        Console.WriteLine($"★係数の上書き（POB_COEF）: {ov}");   // 走行条件の名乗り＝ヘッダーを突き合わせてから数字を読む
    Console.WriteLine($"破綻（未完走）: {b.Breakdowns}/{b.N}");
    Console.WriteLine($"平均 首位確定: Q{b.AvgDecisionQuarter:F0}/{gq}    平均首位交代: {b.AvgLeadChanges:F1}回");
    Console.WriteLine($"デッドロック発生: {b.Deadlocks}/{b.N}    終盤決着(最終1割の四半期): {b.LateDecisions}/{b.N}    異常総数: {b.TotalAnomalies}");   // #135（2026-07-25）：終盤までもつれるのはバランス目標（独走不可）の達成＝病理でない（Round2で異常から除外済み）＝ラベルも情報系へ
    Console.WriteLine($"ハーネス不合格を含む試合: {b.HarnessFailMatches}/{b.N}");
    Console.WriteLine($"恐慌統計: 平均最大ドローダウン {b.AvgMaxDrawdown * 100:F1}%　大恐慌(25%超)を含む試合 {b.GreatDepressionMatches}/{b.N}　平均恐慌回数 {b.AvgDepressionEpisodes:F1}");
    // ★史実基準（2026-08-22・作者指摘）：61カ国合算は史実に対応物がない診断値。史実（Maddison系）と比較できるのは大国8の切り口だけ＝
    //   実質GDPの−20%級は大国8で計5〜7回/1783-1934（ほぼ全て大戦・大恐慌型＝1783-1913の平時はほぼ0）・最大DDは露のWWI+内戦≈40%が上限。
    Console.WriteLine($"恐慌統計（大国8・1人あたり実質GDP・史実比較用）: エピソード {b.AvgMajorDepressionEpisodes:F1}/試合（史実参照 5〜7）　最大DD {b.MaxMajorDrawdown * 100:F1}%（史実参照 ≤40%・平時≤15%）{(b.MaxMajorDdNote.Length > 0 ? $"　出所 {b.MaxMajorDdNote}" : "")}");
    Console.WriteLine($"恐慌の原因: 信用過熱先行 {b.OverheatShare * 100:F0}%　不作先行 {b.HarvestShare * 100:F0}%（残りは複合/その他）");
    Console.WriteLine($"金本位の強制離脱（兌換停止）: 総{b.GoldSuspensions}回（うちカバー率割れ {b.GoldCoverSuspensions}回）・発生試合 {b.GoldSuspensionMatches}/{b.N}");   // GoldCoverMin有効化2026-07-10
    Console.WriteLine($"国家の出入り: 分離独立の建国 {b.Independences}回　変名（統一/formable） {b.Formations}回　内戦分裂 {b.CivilWarSplits}回");
    Console.WriteLine($"選挙（E0計器）: 1試合あたり総選挙 {b.AvgElections:F0}回（全61カ国計）　平均間隔 {b.AvgElectionIntervalQ:F1}四半期（史実英1832-1914＝約15.6Q）");
    // ★(186) 計器の分母（申し送り vb#10）＝史実の側も同じ6国の平均（HistoricalReference.TradeOpenness1913＝1913 年・旧＝英1国の約40% と比べていた）。⚠ シミュの値は終局（1950年）の断面の種の平均＝桁の判定は計器 inj_t186 の10年の窓×種の中央で読む（実装ログ (186)）。
    double tradeRef = SelfPlayMatch.MajorsTradeGdpRef1913();
    Console.WriteLine($"貿易（復活計器）: 6大国の終局 貿易/GDP 平均 {b.AvgTradeGdpMajors:P2}（史実＝同じ6国の1913年の平均 {tradeRef:P1}・比 {(tradeRef > 0 ? b.AvgTradeGdpMajors / tradeRef : 0):F3}）");
    Console.WriteLine("\n勝者分布:");
    foreach (var (nation, wins) in b.WinnerCounts) Console.WriteLine($"  {nation,-10} {wins,2} 勝");
    int polTot = b.PolityDistribution.Sum(x => x.Count);
    Console.WriteLine($"\n最終政体の分布（全{polTot}国×seed）:");
    foreach (var (pol, cnt) in b.PolityDistribution) Console.WriteLine($"  {pol,-24} {cnt,4}  ({100.0 * cnt / Math.Max(1, polTot),4:F1}%)");
    Console.WriteLine($"\n政体の出現プレイ数（{b.N}プレイ中・1国以上で採用＝要望4「希少政体も1/10」）:");
    foreach (var (pol, plays) in b.PolityPlayCount) Console.WriteLine($"  {pol,-24} {plays,3}/{b.N} プレイ");
    Console.WriteLine("\n法の多様性（全国×seed の最終法。最頻値シェアが低いほど多様＝目標5）:");
    foreach (var (axis, maxShare, dist) in b.LawDiversity ?? []) Console.WriteLine($"  {axis,-6} 最頻{maxShare:P0}: {dist}");
    PrintFidelity(b);   // 史実忠実度（新較正基準2026-07-09）＝バッチ出力へ統合（単独表示は fidelity コマンド）
    // ★海軍力の順位（2026-08-01・作者裁定「海軍の強さは測定するべき」）：1914断面の上位。報告のみ・合否には使わない。
    // ★史実の物差し＝1914年の主力艦保有は 英 ≫ 独 > 米 > 仏 ≒ 日 ≒ 露 ≒ 墺。**英が1位でなければ「富で海軍を持つ」が出ていない。**
    if (b.NavalRanking is { Count: > 0 } nav)
    {
        Console.WriteLine($"\n海軍力の順位（1914断面・艦の在庫×段の効き×乗員の充足×海の熟練）: {string.Join("　", nav.Take(8).Select((x, i) => $"{i + 1}位 {x.Nation} {x.Naval:F1}"))}");
        // ★艦の強さの値の順位（2026-08-01 段A の計器・(189) 艦の強さの値＝隻数×建造費×段の乗数の和）。**史実では英が圧倒的1位**。
        var fleet = nav.OrderByDescending(x => x.Land).Take(8).ToList();
        Console.WriteLine($"艦の強さの値の順位（同断面）: {string.Join("　", fleet.Select((x, i) => $"{i + 1}位 {x.Nation} {x.Land:F0}"))}");
    }
    // 同一ペアの再戦（2026-08-01）：作者指摘「同じ国が繰り返し開戦するのは不具合」を数で見る窓。合否には使わない（報告のみ）。
    Console.WriteLine($"\n同一ペアの再戦: 平均 最多{b.AvgWarMaxRepeat:F1}回 ／ 3回以上戦ったペア {b.AvgWarPairsGe3:F1}組 ／ 交戦ペア {b.AvgWarPairs:F1}組" +
        (string.IsNullOrEmpty(b.WarWorstPair) ? "" : $"　最悪: {b.WarWorstPair}"));
    Console.WriteLine($"列強の介入: 撃てる局面 平均{b.AvgInterveneChances:F1} ／ 調停の提案 {b.AvgPeaceOffers:F1}回（白紙講和 {b.AvgPeaceBlank:F1}・拒否 {b.AvgPeaceRefused:F1}）");
    // 合格しなかった検査の内訳（2026-07-31）：どの検査がどのseedで落ちたかを検査名で畳んで出す。★要注意フラグ（硬性ゲート）とは別欄。
    if (b.HarnessIssueLines is { Count: > 0 } hil)
    {
        Console.WriteLine($"\nハーネスの内訳（合格しなかった検査）:");
        foreach (var line in hil) Console.WriteLine($"  {line}");
    }
    // ★2026-09-03（キューB-08 前段）：同名衝突で旧 Colony 型へ落ちた件数。**0 が続けば** Colony 型と旧経路を撤去してよい根拠になる。
    // ★2026-09-03（キューA-05）：跳躍ダイジェスト＝1914断面の一致率が見ない「途中の跳躍」。報告専用（合否には使わない）。
    if (b.JumpLines is { Count: > 0 } jl)
    {
        Console.WriteLine("\n跳躍ダイジェスト（順位/政変/人口）:");
        foreach (var line in jl) Console.WriteLine($"  {line}");
    }
    Console.WriteLine($"\n要注意フラグ（{b.Flags.Count}件）:");
    if (b.Flags.Count == 0) Console.WriteLine("  なし");
    else foreach (var f in b.Flags) Console.WriteLine($"  ⚠ {f}");

    // ★★★ゲート0（2026-08-01・最終調整ハーネス設計 §4）＝**合否を持てる器**。
    // ⚠これ以前は「硬性ゲート」は**人間の目視**でしかなく、Runner に Environment.Exit も return 1 も1件も無かった＝
    //   「測定は成功したがゲートは全滅」を自動化から見分けられなかった。
    // ★機械可読の生値（丸めない分子/分母）＋終了コードを出す。人間向けの要約は上にそのまま残す。
    // 許容する不合格率。★0.05 の根拠＝**10シードでは 0/10 だけが合格**（1件で 0.1 > 0.05 ＝赤）＝現行の厳しさを保ちつつ、
    // 96シードでは 4試合まで許す＝「たまたま裾を引いた1〜4試合」で見出しが赤にならない。⚠これは緩和ではなく**標本数への正規化**である。
    const double HarnessFailRateMax = 0.05;
    var gate0 = new List<(string Name, bool Ok, string Value)>
    {
        ("破綻", b.Breakdowns == 0, $"{b.Breakdowns}/{b.N}"),
        ("デッドロック", b.Deadlocks == 0, $"{b.Deadlocks}/{b.N}"),
        ("異常", b.TotalAnomalies == 0, $"{b.TotalAnomalies}件"),
        // ★★「0件」をやめて**率**にする（2026-08-01・96シード化の前提）。
        // ⚠**10シードで0件は96シードで全緑を1ミリも保証しない。** 1試合あたり検査が落ちる確率を p とすると
        //   全緑の確率は (1−p)^N＝p=0.02 でも96シードで14%・p=0.05 なら0.7%。しかも rule of three により
        //   「10シードで0/10」から95%信頼で言えるのは **p ≤ 0.30** までである。
        // ⚠さらに悪いことに、落ちている検査「満足度・最小国」は**全カ国の最小値**（Validation.WorstSat＝Nations.Min）＝
        //   順序統計量の裾なので、**シードを増やすほど構造的に裾を引きやすくなる**。
        // ★このまま96シードへ上げると「何も変えていないのに見出しのゲートが赤になる」＝その後の判断が全部その赤に引きずられる。
        // ★率にすれば標本数に依らず同じ意味になる。10シードでは 0/10 のみが合格＝**現行と同じ厳しさ**（0.1 > 0.05 なので1件で赤）。
        ("ハーネス不合格", b.HarnessFailMatches <= HarnessFailRateMax * b.N,
            $"{b.HarnessFailMatches}/{b.N}（許容≤{HarnessFailRateMax:P0}＝{HarnessFailRateMax * b.N:F1}試合）"),
        ("要注意フラグ", b.Flags.Count == 0, $"{b.Flags.Count}件"),
    };
    Console.WriteLine($"\n=== ゲート0（機械判定・seeds {string.Join(",", Enumerable.Range((int)seedBase, b.N))}）===");
    foreach (var (nm, ok, val) in gate0) Console.WriteLine($"  {(ok ? "緑" : "★赤")} {nm}: {val}");
    // ★忠実度は**丸めない生値**で出す（:P0 では 49.6% と 50.4% がどちらも「50%」になり50%ゲートが原理的に判定不能）。
    // ⚠ 各次元の**参照エントリ数**も並記する＝3件しか定義が無い次元の「57%」は「3カ国中2カ国」でしかない、が読み取れる。
    if (b.Fidelity is { Count: > 0 } fid0)
    {
        Console.WriteLine("  --- 忠実度の生値（分子/分母・丸めなし。★参照エントリ数が小さい次元はゲートに載せない）---");
        void Raw(string dim, IEnumerable<double> vals)
        {
            var l = vals.Where(v => !double.IsNaN(v)).ToList();
            Console.WriteLine(l.Count == 0
                ? $"    {dim}: 参照エントリ0件＝測定不能"
                : $"    {dim}: {l.Sum():F6} / {l.Count} = {(l.Sum() / l.Count):F6}（参照{l.Count}カ国）");
        }
        Raw("政体exact", fid0.Select(f => f.Exact));
        Raw("政体exact(旧ラベル)", fid0.Select(f => f.LabelExact));   // ★2026-08-21：物差し切替の橋＝旧基準を同走で併記（過去測定と比べるのはこちら）
        Raw("政体near", fid0.Select(f => f.Near));
        Raw("法", fid0.Select(f => f.LawMatch));
        Raw("人口", fid0.Select(f => f.PopMatch));
        Raw("列強", fid0.Select(f => f.GreatMatch));
        Raw("経済規模", fid0.Select(f => f.EconMatch));
        Raw("領土・本国", fid0.Select(f => f.HomeMatch));
        Raw("領土・植民地", fid0.Select(f => f.ColonyMatch));
        Raw("貿易開放度", fid0.Select(f => f.TradeMatch));   // ★2026-08-15：1913年(輸出+輸入)/GDPの±50%帯（参照9カ国）
        // ⚠照合できなかった国を明示する（HistoricalReference は照合不能な国を**黙って落とす**＝「参照表の国数のはずが集計はそれより少ない」に気づけない）
        int refCount = HistoricalReference.Table.Length;
        if (fid0.Count != refCount)
            Console.WriteLine($"    ⚠ 照合できた国 {fid0.Count} ／ 参照表 {refCount}（差{refCount - fid0.Count}カ国は変名・建国・未登録のいずれかで**黙って落ちている**）");
    }
    // ★★6次元50%ゲート（2026-08-01・最終調整ハーネス設計 §5-3・裁定①で3値化）。
    // ── なぜ3値なのか（ユーザー裁定2026-08-01・設計書 §0-2）────────────────────────
    //   裁定順序「破綻しない ＞ 正しい挙動 ＞ バランス」を**そのまま終了コードの大小へ写す**。
    //     0 ＝ 硬性ゲート全緑 かつ ゲート対象の全次元が50%以上
    //     1 ＝ 硬性ゲートが赤          ← **差し戻しの理由になる唯一の値**
    //     2 ＝ 硬性ゲート緑・6次元が未達 ← 記録して進む（差し戻しの理由にならない）
    //   ⚠同じ裁定が「帯が悪化しただけでは差し戻さない」と言っている以上、6次元が exit 1 を出してはならない。
    //   ⚠しかし印字のみでは「記録を欠いた採用は認めない」という裁定の後半が機械化されない。3値なら
    //     運用規約が1行で書ける＝「rc==1 で止める、rc==2 は記録して進む」。
    // ⚠国×次元ごとに閾値を掛けない（全カ国×6次元＝多重比較で必ず誰かが落ちる）。次元の集計値で1回だけ判定する。
    bool allGreen = gate0.TrueForAll(g => g.Ok);
    bool dimsOk = true;
    if (b.Fidelity is { Count: > 0 } fidG)
    {
        // 参照エントリが少ない次元はゲートに載せない（報告のみ）。★8の根拠＝8件未満では1カ国の増減が
        // 一致率を12.5%超動かす＝「五分五分(50%)と目標(50%)の距離」を1カ国が跨いでしまい、判定が標本の当たり外れになる。
        // ⚠2026-08-01の参照データ拡充後、これに掛かるのは PopGrowthOk（7件）だけ＝人口次元のみ恒久的に報告のみ（裁定①の条件1）。
        const int MinRefEntriesForGate = 8;
        const double Target = HistoricalReference.DimensionTargetStrict;
        var dims = new (string Name, IEnumerable<double> Vals)[]
        {
            ("政体exact", fidG.Select(f => f.Exact)), ("政体exact(旧ラベル)", fidG.Select(f => f.LabelExact)), ("法", fidG.Select(f => f.LawMatch)),
            ("人口", fidG.Select(f => f.PopMatch)),
            // ★★列強軸は**2つに割る**（2026-08-01）。混ぜた集計値は情報を持たない——「誰も列強にならない」と
            //   常に答える自明な予測器が 47/55＝85.5% を取り、混ぜた実測80.9%はそれを4.6pt**下回っていた**（46:8の不均衡）。
            //   再現率＝史実の列強8カ国が列強になった率（仕様「上位8と史実8の一致5カ国以上」＝62.5%の閾値表現）
            //   特異度＝史実の非列強47カ国が列強にならなかった率（＝**アジア過近代化の直接の計器**）
            //   ⚠ゲートは**低い方**に掛かる（下でそれぞれ独立に判定するので自動的にそうなる）。
            ("列強・再現率", fidG.Where(f => f.GreatExpected).Select(f => f.GreatHit)),
            ("列強・特異度", fidG.Where(f => !f.GreatExpected).Select(f => f.GreatHit)),
            ("経済規模", fidG.Select(f => f.EconMatch)), ("領土・本国", fidG.Select(f => f.HomeMatch)),
            ("領土・植民地", fidG.Select(f => f.ColonyMatch)),
            // ★貿易開放度（2026-08-15・作者承認）：参照9カ国＝ゲート対象。現況≈0%＝既知の未達＝終了コード2の意味は変わらない
            //   （既に他次元が未達）。存在意義＝Search再較正の目的関数が貿易を見る（TradeSpeed等の再導出はこの次元込みで）。
            ("貿易開放度", fidG.Select(f => f.TradeMatch)),
        };
        // ★前回値との差分を必ず併記する（裁定①の条件2）。⚠exit 2 は「未達」であって「悪化」ではないので、
        //   **悪化の検出は差分でしか出せない**。基線は POB_FIDELITY_BASELINE のパス（measure.sh が作業ツリー直下を渡す
        //   ＝隔離スナップショットではなく走行を跨いで残る場所）。⚠毎回上書きする＝常に「直前の走行との差」を意味する。
        string basePath = Environment.GetEnvironmentVariable("POB_FIDELITY_BASELINE") ?? "";
        // ★基線に走行条件を書く（fis-23・2026-09-26）：旧＝数字だけを書いたので、長さ・シード・係数の上書きの違う腕や、同時に回した別の腕の値と
        //   黙って差を取っていた。1行目に「#条件」（シード・四半期数・係数の上書き・measure.sh が渡す SOURCE-HASH）を書き、読むときに突き合わせる。
        //   条件が食い違えば差を出さない（「前回値なし（条件違い）」）。⚠条件の記録が無い旧い基線は従来どおり差を出す（出力は旧と1字も変わらない）。
        string condKey = $"seeds={seedBase}..{seedBase + (ulong)b.N - 1}・四半期={gq}・上書き={(PriceOfBread.SimCore.Coefficients.OverrideSummary() is { Length: > 0 } ovc ? ovc : "なし")}";
        string condLine = $"#条件\t{condKey}\tSOURCE-HASH={Environment.GetEnvironmentVariable("POB_SOURCE_HASH") ?? "?"}";
        string? prevCond = null;
        var prev = new Dictionary<string, double>();
        if (basePath.Length > 0 && File.Exists(basePath))
            foreach (var ln in File.ReadAllLines(basePath))
            {
                int tab = ln.IndexOf('\t');
                if (ln.StartsWith("#条件\t")) { prevCond = ln[(tab + 1)..]; continue; }
                if (tab > 0 && double.TryParse(ln[(tab + 1)..], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double pv)) prev[ln[..tab]] = pv;
            }
        bool condMismatch = prevCond is not null && prevCond.Split('\t')[0] != condKey;
        if (condMismatch) prev.Clear();   // 条件違い＝差を取らない（相手が別の物差し）
        var now = new List<string> { condLine };
        Console.WriteLine($"\n=== 6次元{Target:P0}ゲート（★終了コード2＝未達。硬性ゲートの赤(1)とは別の意味）===");
        if (prevCond is not null) Console.WriteLine($"  前回の基線：{prevCond.Replace('\t', '・')}{(condMismatch ? "　⚠条件が違う＝差を出さない" : "")}");
        int gated = 0, passed = 0;
        foreach (var (nm, vals) in dims)
        {
            var l = vals.Where(v => !double.IsNaN(v)).ToList();
            if (l.Count == 0) { Console.WriteLine($"  —  {nm}: 参照エントリ0件＝測定不能"); continue; }
            double rate = l.Sum() / l.Count;
            now.Add($"{nm}\t{rate.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
            string delta = prev.TryGetValue(nm, out double p0)
                ? $"　前回比 {(rate - p0) * 100:+0.0;-0.0;±0.0} pt"
                : condMismatch ? "　前回値なし（条件違い）" : "　前回値なし";
            if (l.Count < MinRefEntriesForGate)
            { Console.WriteLine($"  報告 {nm}: {rate:P1}（参照{l.Count}カ国＝{MinRefEntriesForGate}件未満につきゲート外）{delta}"); continue; }
            gated++;
            bool ok = rate >= Target;
            if (ok) passed++; else dimsOk = false;
            Console.WriteLine($"  {(ok ? "合格" : "未達")} {nm}: {rate:P1}（参照{l.Count}カ国）{delta}");
        }
        Console.WriteLine($"  6次元の到達: {passed}/{gated} 次元が{Target:P0}以上");
        // ★人口次元の内訳（2026-08-01・ユーザー要望）。⚠集計値だけだと「全体が下振れした」のか
        //   「境界にいる1〜2カ国が出入りしている」のか区別できない。次に D4 を拡充するとき
        //   「どの国を足せば安定するか」もこの内訳からしか分からない。
        var popRows = fidG.Where(f => !double.IsNaN(f.PopMatch)).ToList();
        if (popRows.Count > 0)
        {
            Console.WriteLine($"  人口次元の内訳（参照{popRows.Count}カ国・1カ国＝{100.0 / popRows.Count:F1}pt。案Y2026-08-21＝倍率は併合の持ち込み人口を除く新定義・旧定義は領土込みで併記）:");
            foreach (var f in popRows.OrderByDescending(f => f.PopMatch))
                Console.WriteLine($"    {f.PopMatch,6:P0}  {f.Nation}（実測の人口倍率 平均{f.PopGrowthMean:F2}・領土込み{f.PopGrowthGrossMean:F2}）");
        }
        if (basePath.Length > 0) { try { File.WriteAllLines(basePath, now); } catch (IOException) { /* 基線が書けなくても測定は成立する */ } }
    }
    int rc = !allGreen ? 1 : dimsOk ? 0 : 2;
    Console.WriteLine($"\n  判定: {rc switch {
        0 => "全ゲート緑・6次元も到達（終了コード0）",
        1 => "★硬性ゲートに赤（終了コード1＝差し戻しの理由）",
        _ => "硬性ゲート緑・6次元が未達（終了コード2＝記録して進む）" }}");
    if (rc != 0) Environment.Exit(rc);   // ★measure.sh は $RC をそのまま返すので、自動化がそのまま乗る
    return;
}

// === 史実忠実度の単独測定： dotnet run -- fidelity [N] [金利|-] [並列度] ===
// 新較正基準（2026-07-09 ユーザー最高方針）：各国が約7割のシードで史実（または近い）1914年政体・主要法に到達するか。
// selfplay-batch と同じバッチを回し、史実忠実度テーブル**のみ**を表示（参照表＝SimCore/HistoricalReference.cs・根拠＝docs/史実参照表_1914.md）。
// 逸脱の因果監査（手順は docs/史実参照表_1914.md 末尾）：逸脱シードを selfplay <seed> 500 で再走し外交ログの連鎖（敗戦→革命→政体遷移）を追う。
if (args.Length > 0 && args[0] == "fidelity")
{
    int fn = ArgInt(args, 1, 30);
    double? fir = ArgDouble(args, 2, null);
    int fpar = ArgInt(args, 3, 0);
    int fgq = PriceOfBread.SimCore.Coefficients.GameQuarters;
    var fb = SelfPlayMatch.RunBatch(fn, fgq, fir, fpar > 0 ? fpar : null);
    Console.WriteLine($"=== 史実忠実度 seeds 1..{fb.N}（各{fgq}四半期＝1783→{1783 + fgq / 4}）===");
    if (fb.Breakdowns > 0) Console.WriteLine($"⚠ 破綻（未完走）: {fb.Breakdowns}/{fb.N}（集計は完走分のみ）");
    PrintFidelity(fb);
    return;
}

// === 軍国主義の実体の分布： dotnet run -- milgrade [シード数] [並列度] ===
// ★親軍法の撤去（2026-08-18）の計器。**軍の政治的優位**（軍の圧力×(1−文民制度の弁)）を 1914断面（Q524）で全国ぶん出し、
//   忠実度ハーネスの閾値（HistoricalReference.MilitarismDegreeOk）を「史実の軍国＝プロイセンが真・非軍国＝アメリカが偽」
//   になる位置に置けているかを実データで確かめる（閾値を勘で決めない）。
//   ⚠旧案「軍事費×徴兵法の深さ」はこのプローブ自身が棄却した（61カ国が厳密に0.350へ張り付き識別力ゼロ＝docs/202608）。
if (args.Length > 0 && args[0] == "milgrade")
{
    int mgN = ArgInt(args, 1, 5);
    int mgPar = ArgInt(args, 2, 0);
    int mgQ = PriceOfBread.SimCore.Coefficients.GameQuarters;
    var mgArr = new PriceOfBread.SimCore.MatchResult[mgN];
    _ = WorldData.Nations();
    var mgOpts = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = mgPar > 0 ? mgPar : Math.Clamp(Environment.ProcessorCount, 2, Math.Max(2, mgN)) };
    System.Threading.Tasks.Parallel.For(0, mgN, mgOpts, i => mgArr[i] = SelfPlayMatch.Run((ulong)(i + 1), mgQ, null, lean: true));
    var mgRows = new Dictionary<string, List<double>>();
    var mgParts = new Dictionary<string, double[]>();   // ★内訳の累計（分子・分母・任免の所在・正統性・件数）＝合成値だけでは写像の誤りと世界の誤りが切り分けられない
    int mgDone = 0;
    foreach (var r in mgArr)
    {
        if (!r.Completed) continue;
        mgDone++;
        foreach (var st in r.StandingsAt1914 ?? r.Standings)
        {
            string key = st.Nation.Length > 0 ? st.Nation : st.Name;
            if (!mgRows.TryGetValue(key, out var lst)) mgRows[key] = lst = new List<double>();
            lst.Add(st.Militarism);
            if (!mgParts.TryGetValue(key, out var pa)) mgParts[key] = pa = new double[5];
            pa[0] += st.MilPush; pa[1] += st.MilValve; pa[2] += st.MilCourt; pa[3] += st.MilLegitimacy; pa[4] += 1;
        }
    }
    string Parts(string key)
    {
        if (!mgParts.TryGetValue(key, out var pa) || pa[4] < 1) return "";
        return $"  ｜分子{pa[0] / pa[4],5:F3} 分母{pa[1] / pa[4],5:F3}（任免{pa[2] / pa[4],5:F3} 正統{pa[3] / pa[4],5:F3}）";
    }
    Console.WriteLine($"=== 軍の政治的優位（軍の圧力×(1−文民制度の弁)）1914断面 seeds 1..{mgN}（完走 {mgDone}/{mgN}）===");
    Console.WriteLine($"現行の閾値: ハーネス {PriceOfBread.SimCore.HistoricalReference.MilitarismDegreeOk:F2}");
    static string Line(string name, List<double> v)
    {
        v.Sort();
        double mean = 0; foreach (var x in v) mean += x; mean /= Math.Max(1, v.Count);
        return $"  {name,-16} n={v.Count,3}  平均{mean,5:F3}  最小{v[0],5:F3}  中央{v[v.Count / 2],5:F3}  最大{v[^1],5:F3}";
    }
    Console.WriteLine("── 参照表で軍国が定義されている国（この2つが分かれる位置に閾値を置く）──");
    foreach (var e in PriceOfBread.SimCore.HistoricalReference.Table)
    {
        if (e.MilitarismOk is not { } ok || !mgRows.TryGetValue(e.Nation, out var v) || v.Count == 0) continue;
        Console.WriteLine(Line($"{e.Nation}({(ok ? "軍国" : "非軍国")})", v) + Parts(e.Nation));
    }
    Console.WriteLine("── 全国（平均の降順）──");
    foreach (var kv in mgRows.OrderByDescending(k => k.Value.Sum() / Math.Max(1, k.Value.Count)))
        Console.WriteLine(Line(kv.Key, kv.Value) + Parts(kv.Key));
    return;
}

// === 軍国主義の4実体のアブレーション： dotnet run -- milablate ===
// ★作者裁定2026-08-18の確認項目：**プレイヤー最適解の2つの穴が両方塞がっていること**を、別々の壊れ方として見せる。
//   (a)「軍事費を削れば軍は安全」＝塞がっている（削ると**兵卒側**＝即応度・忠誠が崩れる。軍の政治的優位は動かない）
//   (b)「金を積めば将校は黙る」  ＝塞がっている（軍事費を振り切っても軍の政治的優位は動かない。動くのは人事＝議会統制力）
//   同じ単一国（英・240四半期）で、軍事費だけ／議会統制力だけを振り、4実体がどう反応するかを並べる。
if (args.Length > 0 && args[0] == "milablate")
{
    static World MaRun(Action<World> cfg)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(7), new NullAgent());
        var w = sim.World;
        w.PoliticalLockEnabled = false;
        w.CoercionFollowsForce = true;
        var p = w.Policy;
        p.AdminSpendingTarget = 0.6; p.IncomeTaxTarget = 0.3; p.PollTaxTarget = 0.4;
        p.MilitarySpendingTarget = 0.3;
        w.EducationLaw = EducationLaw.Public; p.EducationLaw = EducationLaw.Public;
        cfg(w);
        sim.Run(240);
        return w;
    }
    Console.WriteLine("=== 軍国主義の4実体のアブレーション（英・240四半期）===");
    Console.WriteLine($"  {"条件",-22} {"軍の練度",8} {"軍政比",8} {"公共圏",8} {"任免権",8} {"即応度",8} {"軍忠誠",8}");
    void MaRow(string name, Action<World> cfg)
    {
        var w = MaRun(cfg);
        Console.WriteLine($"  {name,-22} {PoliticalQuantities.ArmyProfessionalism(w),8:F3} {PoliticalQuantities.MilitaryPoliticalRatio(w),8:F3}"
                        + $" {PoliticalQuantities.WarFeverPublicSphere(w),8:F3} {PoliticalQuantities.OfficerAppointmentCrownward(w),8:F3}"
                        + $" {w.MilitaryReadiness,8:F3} {w.ArmyAllegiance,8:F3}");
    }
    MaRow("基準", _ => { });
    MaRow("軍事費 0.05（振り切り下）", w => w.Policy.MilitarySpendingTarget = 0.05);
    MaRow("軍事費 0.95（振り切り上）", w => w.Policy.MilitarySpendingTarget = 0.95);
    MaRow("議会統制力 0（人事=宮廷）", w => w.ParliamentControl = 0.0);
    MaRow("議会統制力 1（人事=内閣）", w => w.ParliamentControl = 1.0);
    Console.WriteLine("  ★2つの非歴史的最適解が**別々の壊れ方**で塞がっていること（作者裁定2026-08-18の確認項目）：");
    Console.WriteLine("    ・軍事費を削る → 解隊の脅威で軍政比が**上がる**（ノスケ1920のエアハルト旅団解散→カップ一揆／アサーニャ1931→サンフルハダ）。");
    Console.WriteLine("    ・軍事費を積む → 組織が大きくなり軍政比が**上がる**一方、即応度は薄く広くなって**下がる**（中南米の歳出68%国家の型）。");
    Console.WriteLine("      ⇒ 金額そのものは1度も読まない。金で将校は買えず、削っても安全にならない＝両側に罰がある。");
    Console.WriteLine("    ・議会統制力 → 任免権の符号が反転し軍忠誠が王権側↔議会側へ振れる（人事と軍法だけが忠誠の向きを決める）。");
    Console.WriteLine("  ⚠給与は帰属を1ビットも動かさない（2026-08-18の欧露15事例）＝動くのは即応度＝規律・能力のトラック。");
    return;
}

// === 自動テストプレイ（自己対戦）モード： dotnet run -- selfplay [seed] ===
if (args.Length > 0 && args[0] == "selfplay")
{
    ulong sd = ArgUlong(args, 1, 1);
    var r = SelfPlayMatch.Run(sd, ArgInt(args, 2, 400));   // 第2引数＝四半期数（既定400。バッチ既定のCoefficients.GameQuarters（668）で再現したい時は明示）
    Console.WriteLine($"=== 自動テストプレイ seed={r.Seed}（{r.Quarters}四半期＝{r.Quarters / 4}年）===");
    Console.WriteLine($"完走: {(r.Completed ? "○" : "× 中断")}    勝者: {r.Winner}（スコア{r.WinnerScore:F1}）    首位確定: Q{r.DecisionQuarter}/{r.Quarters}");
    Console.WriteLine($"ハーネス合否: 〇{r.HarnessPass} △{r.HarnessWarn} ×{r.HarnessFail}    損失financing 累積(最大): {r.MaxLossFinanced:N0}  ピーク率: {r.WorstLossRate:P2}（名目産出比）");
    Console.WriteLine($"経済の波: 最大ドローダウン {r.MaxDrawdown * 100:F1}%　恐慌エピソード {r.DepressionEpisodes}回");
    foreach (var hi in r.HarnessIssues) Console.WriteLine($"    {hi}");
    Console.WriteLine("\n列強ランキング（国力 ＝ 人口＋軍隊＋豊かさ）:");
    foreach (var st in r.Standings)
        Console.WriteLine($"  {st.Name,-10} 国力{st.Score,6:F0}  人口{st.Population,7:N0} 軍{st.Military,4:F1} 戦死累計{st.WarDead,7:N0} 工業{st.Industrialization,4:F1} 技術{st.Tech,4:F1} 関税{st.Tariff,4:F2} 政体{st.Polity,-22} 満足{st.Satisfaction:F2} 植民地{st.Colonies,2}");
    Console.WriteLine($"\n主要イベント（直近15件 / 全{r.Events.Count}件）:");
    foreach (var e in r.Events.TakeLast(15)) Console.WriteLine($"  {e}");
    Console.WriteLine($"\n異常/要注意（{r.Anomalies.Count}件）:");
    if (r.Anomalies.Count == 0) Console.WriteLine("  なし");
    else foreach (var a in r.Anomalies) Console.WriteLine($"  ⚠ {a}");
    return;
}

// === 初期生産トレース： dotnet run -- trace-prod [quarters] ===（先進財が1783に立たないか確認）
if (args.Length > 0 && args[0] == "trace-prod")
{
    int qs = ArgInt(args, 1, 4);
    NationConfig[] tc = [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
    ];
    var tsim = new MultiNationSim(tc, 1, _ => new UtilityAgent());
    tsim.StartWarmup();   // ★2026-09-07 計器の助走統一：selfplay-batch/popprobe と同じ助走を通す＝「測る世界」を帯の世界と一致させる（旧＝助走なし＝chronicle が popprobe と食い違った構築時型の計器バグ）
    for (int q = 0; q < qs; q++) tsim.StepQuarter();
    int[] watch = [Catalog.ApplianceId, Catalog.ElectricityId, Catalog.SteamEngineId, Catalog.SteelId, Catalog.MachineryId, Catalog.CementId];   // 電化製品・電力・蒸気機関・鋼・機械・セメント（★段1：発電機は機械へ畳んだ）
    foreach (var nat in tsim.Nations.Take(2))
    {
        var tw = nat.World;
        var r = tw.Region;
        int visible = r.Producers.Count(p => p.Inputs.Count > 0 && p.Output > 1);
        int total = r.Producers.Count(p => p.Inputs.Count > 0);
        Console.WriteLine($"=== {tw.SelfPower?.Name ?? r.Name} {tw.Year}年（{qs}四半期後）地域[{r.Name}] 技術{tw.TechLevel:F2} 表示工場{visible}/{total}（Output>1）===");
        foreach (int gid in watch)
        {
            var p = r.Producers.FirstOrDefault(x => x.GoodId == gid);
            if (p != null) Console.WriteLine($"    {Catalog.Goods[gid].Name,-8} Output {p.Output,8:F2}  Scale {p.Scale:F3}  {(p.Output > 1 ? "表示" : "非表示")}");
        }
    }
    return;
}

// === 7カ国 詳細テストプレイ： dotnet run -- test8 [seed] ===
if (args.Length > 0 && args[0] == "test8")
{
    ulong seed = ArgUlong(args, 1, 1);
    NationConfig[] cfgs = [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
    ];
    var sim8 = new MultiNationSim(cfgs, seed, _ => new UtilityAgent());
    int n8 = cfgs.Length;
    var polHist = new List<(int Year, Polity P)>[n8];
    for (int i = 0; i < n8; i++) polHist[i] = [(sim8.Nations[i].World.Year, sim8.Nations[i].World.Polity)];
    const int q8 = 400;
    for (int q = 0; q < q8; q++)
    {
        sim8.StepQuarter();
        for (int i = 0; i < n8; i++)
        {
            var w = sim8.Nations[i].World;
            if (w.Polity != polHist[i][^1].P) polHist[i].Add((w.Year, w.Polity));
        }
    }
    Console.WriteLine($"=== {n8}カ国 詳細テストプレイ seed={seed}（{q8}四半期＝{q8 / 4}年）===");
    var order = Enumerable.Range(0, n8).OrderByDescending(i => NationalScore.Of(sim8.Nations[i].World)).ToList();
    int rk = 1;
    foreach (int i in order)
    {
        var w = sim8.Nations[i].World;
        var pw = w.SelfPower;
        double tot = Math.Max(1, w.TotalPopulation);
        string popStr = string.Join(" ", w.AllPops().GroupBy(p => p.ClassName)
            .Select(g => (g.Key, S: g.Sum(p => p.Size))).OrderByDescending(x => x.S)
            .Select(x => $"{x.Key}{100 * x.S / tot:F0}%"));
        string regions = string.Join("/", w.Regions.Where(r => r.State == RegionState.Active).Select(r => r.Name));
        Console.WriteLine($"\n#{rk} {w.SelfPower?.Name ?? w.Region.Name}  国力{NationalScore.Of(w):F0}  満足{NationalScore.MeanSatisfaction(w):F2}");
        {   // 国力内訳＝現行スコア式（NationalScore.Of＝軍+富+金融+技術）から生成（監査2026-07-12 死13：旧＝古い係数の手書き式で実際の得点内訳と食い違い、バランス作業を誤誘導していた）
            var (gdpScale, indT8, satT8, colT8) = NationalScore.WealthParts(w);   // 内訳＝正典の1本（2026-09-11 #15）
            Console.WriteLine($"  国力内訳: 軍{NationalScore.Military(w):F1} ＋ 富{NationalScore.Wealth(w):F1}（GDP逓減{gdpScale:F1}+工業{indT8:F1}+生活{satT8:F1}+植民{colT8:F1}）＋ 金融{NationalScore.Finance(w):F1} ＋ 技術{NationalScore.Tech(w):F1} ＝ {NationalScore.Of(w):F1}");
        }
        Console.WriteLine($"  人口{w.TotalPopulation:N0}（{popStr}）  植民地{pw?.Colonies ?? 0}（植民人口{w.ColonialPop:F0}）");
        Console.WriteLine($"  工業{w.Region.TotalIndustrialization:F2} 技術{w.TechLevel:F2} 識字{w.AllPops().Sum(p => p.Literacy * p.Size) / Math.Max(1, w.TotalPopulation):P0} 制度[特許{(w.PatentLaw ? "○" : "×")} 株式{(w.JointStock ? "○" : "×")} 鉄道{(w.Railways ? "○" : "×")}]");
        Console.WriteLine($"  輸送[鉄道容量{w.RailCapacity:F0} 船容量{w.ShipCapacity:F0}  港州{w.Regions.Count(r => r.IsPort)}/{w.Regions.Count(r => r.State == RegionState.Active)}]");
        Console.WriteLine($"  法[教育{w.EducationLaw} 参政{w.Suffrage} 報道{w.Censorship} 組合{w.LaborUnion} 軍政比{PoliticalQuantities.MilitaryPoliticalRatio(w):F2} 労働{w.Policy.WorkHours} 児童{(w.Policy.ChildLaborAllowed ? "許" : "禁")} 関税{w.Tariff:F2}]");
        Console.WriteLine($"  地域[{regions}]  戦争 勝{pw?.WarsWon ?? 0}/敗{pw?.WarsLost ?? 0}  戦死累計{w.WarDeadCumulative:N0}  貿易収支累計{w.CumulativeTradeFlow:N0}");
        string specStr = string.Join("  ", w.Regions.Where(r => r.State == RegionState.Active).Select(rg =>
        {
            var topExp = Enumerable.Range(0, rg.Markets.Length).Where(gi => rg.Markets[gi].LastExport > 0.01)
                .OrderByDescending(gi => rg.Markets[gi].LastExport).Take(2).Select(gi => Catalog.Goods[gi].Name).ToList();
            return $"{rg.Name}=[{(topExp.Count > 0 ? string.Join("/", topExp) : "—")}]";
        }));
        Console.WriteLine($"  州別輸出: {specStr}");
        Console.WriteLine($"  政体史: {string.Join(" → ", polHist[i].Select(h => $"{h.Year}:{h.P}"))}{(w.HadSecession ? "　[分離戦]" : w.HadCivilWar ? "　[内戦経験]" : "")}");
        rk++;
    }
    var deadEnds8 = new List<string>();
    foreach (var g in Catalog.Goods)
    {
        if (g.IsColonial) continue;
        double maxD = 0;
        foreach (var n in sim8.Nations) maxD = Math.Max(maxD, n.World.Region.Markets[g.Id].LastDemand);
        if (maxD < 0.5) deadEnds8.Add($"{g.Id}:{g.Name}");
    }
    Console.WriteLine($"\n下流の無い財({deadEnds8.Count}): {(deadEnds8.Count > 0 ? string.Join("、", deadEnds8) : "なし")}");
    int[] milTrace = { Catalog.AmmunitionId, Catalog.GunpowderId, Catalog.LeadId, Catalog.LeadOreId, Catalog.WarshipId, Catalog.SailingShipId, Catalog.CanvasId, Catalog.HempId };   // 弾薬→火薬+鉛→鉛鉱 / 軍艦→帆船→帆布→麻
    Console.Write("軍需連鎖の需給(全国最大 需/産): ");
    foreach (int gi in milTrace)
    {
        double mxD = 0, mxP = 0;
        foreach (var n in sim8.Nations) { var mk = n.World.Region.Markets[gi]; mxD = Math.Max(mxD, mk.LastDemand); mxP = Math.Max(mxP, mk.LastProduced); }
        Console.Write($"{Catalog.Goods[gi].Name}={mxD:F1}/{mxP:F1} ");
    }
    Console.WriteLine();
    var qingP = sim8.Diplomacy.Powers[^1];   // ★抽象清は廃止済み＝末尾は world.json abstractTargets の最後（勝敗の釣り合い確認用に名前で印字）
    Console.WriteLine($"{qingP.Name}（抽象・経済なし）: 戦争 勝{qingP.WarsWon}/敗{qingP.WarsLost}  植民地{qingP.Colonies}");
    int wars8 = sim8.Diplomacy.Log.Count(l => l.Contains("開戦"));
    Console.WriteLine($"総開戦数: {wars8}（各国の勝の合計＝各国＋清の敗の合計＝1対1の決着数）");
    Console.WriteLine("--- 開戦ログ ---");
    foreach (var l in sim8.Diplomacy.Log) if (l.Contains("開戦") || l.Contains("勝利") || l.Contains("白紙")) Console.WriteLine("  " + l);
    return;
}

// === PriceOfBread 検証ハーネス ===
// 散発的な診断を「目標帯つきの構造化レポート」に昇格。保存則（破ると即クラッシュ）の上位で
// 「経済が"良い状態"か」を測る。帯は初期提案＝運用しながら較正し、満たしたらテスト群へ固定する。

NationConfig[] configs = [
    NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
    NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
];
const int quarters = 400;
var sim = new MultiNationSim(configs, 1789);

SimMetrics met;
int[] traceGoods = [Catalog.CottonId, Catalog.CoalId];   // 綿花・石炭＝資源プロファイルの代表（英は綿花不足→輸入・石炭過多→輸出）。輸入依存の確認用
var trace = new List<string>();
try
{
    for (int q = 0; q < quarters; q++)
    {
        sim.StepQuarter();
        if ((q + 1) % 20 == 0)
        {
            var r = sim.Nations[2].World.Region;   // イギリス（二極化最悪）
            var parts = new List<string>();
            foreach (int gid in traceGoods)
            {
                var mk = r.Markets[gid];
                parts.Add($"{Catalog.Goods[gid].Name} P{mk.Price:F2} 産{mk.LastProduced:F0} 需{mk.LastDemand:F0} 輸入{mk.LastImport:F0} 輸出{mk.LastExport:F0}");
            }
            trace.Add($"Q{q + 1,3}  金世界{sim.Market.Price[Catalog.GoldOreId]:F2} 日関税{sim.Nations[1].World.Tariff:F2}  " + string.Join("   ", parts));
        }
    }
    met = Metrics.Compute(sim);
}
catch (Exception ex)
{
    // 完走しない＝最重要の不合格（保存則違反など）。長期テストを止める要因なので即座に報告。
    Console.WriteLine($"× 完走(保存則): 不合格 — {quarters}四半期の途中で停止");
    Console.WriteLine($"   {ex.GetType().Name}: {ex.Message}");
    return;
}

Console.WriteLine($"=== PriceOfBread 検証レポート（{configs.Length}国・{quarters}四半期＝{quarters / 4}年・seed 1789）===");
Console.WriteLine($"〇 完走(保存則): 合格 — {met.Year}年まで保存則違反なく到達\n");

// --- 各国の指標 ---
Console.WriteLine($"{"国",-10}{"技術",6}{"工業化",7}{"満足",6}{"床%",6}{"天井%",6}{"中間%",6}{"都市%",6}{"識字",6}{"植民地",6}");
foreach (var n in met.Nations)
    Console.WriteLine($"{n.Name,-10}{n.TechLevel,6:F2}{n.Industrialization,7:F2}{n.MeanSatisfaction,6:F2}" +
                      $"{n.PriceFloorFrac * 100,6:F0}{n.PriceCeilFrac * 100,6:F0}{n.PriceMidFrac * 100,6:F0}" +
                      $"{n.UrbanShare * 100,6:F0}{n.MeanLiteracy,6:F2}{n.Colonies,6}");

// --- 合格判定 ---
var checks = Validation.Evaluate(met);
Console.WriteLine("\n=== 合格基準（現在の検証済み水準＝回帰検出の基準線）===");
foreach (var c in checks)
{
    string icon = c.Verdict switch { Verdict.Pass => "〇", Verdict.Warn => "△", _ => "×" };
    Console.WriteLine($"  {icon} {c.Name,-14} 実測 {c.Value,-7} 目標 {c.Target}");
}
int pass = Validation.Count(checks, Verdict.Pass), warn = Validation.Count(checks, Verdict.Warn), fail = Validation.Count(checks, Verdict.Fail);
Console.WriteLine($"\n  合計: 〇{pass}  △{warn}  ×{fail}  （全{checks.Count}項）");
Console.WriteLine("  ※帯は現在の検証済み水準＝回帰検出用。価格中間帯の二極化は既知の構造課題（コブウェブ振動×損失吸収の脆さ＝会計の再設計が宿題）。マクロ層は健全。");

// --- 診断の手がかり：床に沈む財の階層別内訳（中間帯が薄い時に何が二極化しているか）---
Console.WriteLine("\n=== 診断：床(0.05×)に沈む財の階層別内訳 ===");
Console.WriteLine($"{"国",-10}{"必需",6}{"日用",6}{"奢侈",6}{"中間財",7}{"資本",6}");
foreach (var n in sim.Nations)
{
    var r = n.World.Region;
    int[] cnt = new int[5];
    foreach (var g in r.Goods) if (g.Tier != GoodTier.Service && r.Markets[g.Id].Price <= g.RefPrice * 0.051) cnt[(int)g.Tier]++;   // 役務財(index5)は列に無い＝他プローブと同じ除外（旧はIndexOutOfRangeの潜在バグ）
    Console.WriteLine($"{r.Name,-10}{cnt[0],6}{cnt[1],6}{cnt[2],6}{cnt[3],7}{cnt[4],6}");
}

// --- 床財の採算分解（イギリス＝二極化最悪国）：赤字(price<mc・倒産が効く) vs 黒字だが床(price≥mc・倒産は効かない) ---
var brR = sim.Nations[2].World.Region; var brC = sim.Nations[2].World.Coef;
int lossFloor = 0, profitFloor = 0;
var lossEx = new List<string>(); var profitEx = new List<string>();
foreach (var g in brR.Goods)
{
    var mk = brR.Markets[g.Id];
    if (mk.Price > g.RefPrice * 0.051) continue;                       // 床財のみ
    var prod = brR.Producers.FirstOrDefault(p => p.GoodId == g.Id);
    if (prod == null) continue;                                        // 生産者なし（植民地財等）は除外
    double mc = EconomyStep.MineralSelfCost(prod, brR, brC, sim.Nations[2].World) + EconomyStep.InputCostPerUnit(prod, brR, brR, brC);   // エンジンの①供給と同じ mc（同じ関数＝表示が静かに嘘にならない・旧＝素の MarginalCost＋素の原単位の写し）
    if (mk.Price < mc) { lossFloor++; if (lossEx.Count < 6) lossEx.Add($"{g.Name}(P{mk.Price:F3}<mc{mc:F3} 産{mk.LastProduced:F0} 需{mk.LastDemand:F0} Sc{prod.Scale:F2})"); }
    else { profitFloor++; if (profitEx.Count < 6) profitEx.Add($"{g.Name}(P{mk.Price:F3}≥mc{mc:F3} 産{mk.LastProduced:F0} 需{mk.LastDemand:F0} Sc{prod.Scale:F2})"); }
}
Console.WriteLine($"\n=== 床財の採算（イギリス）：赤字{lossFloor}（倒産が効く） / 黒字だが床{profitFloor}（倒産は効かない）===");
Console.WriteLine("  赤字例  : " + string.Join("、", lossEx));
Console.WriteLine("  黒字床例: " + string.Join("、", profitEx));

// --- 下流の無い財（あれば列挙）---
if (met.DeadEndGoods > 0)
{
    var list = new List<string>();
    foreach (var g in Catalog.Goods)
    {
        if (g.IsColonial) continue;
        double maxD = 0;
        foreach (var n in sim.Nations) maxD = Math.Max(maxD, n.World.Region.Markets[g.Id].LastDemand);
        if (maxD < 0.5) list.Add($"{g.Id}:{g.Name}");
    }
    Console.WriteLine($"\n下流の無い財: {string.Join("、", list)}");
}

// --- 過剰供給の時系列（行き過ぎ→固着 か 恒常的過剰 か の判定）---
Console.WriteLine("\n=== 過剰供給の時系列（イギリス・20四半期ごと）===");
foreach (var t in trace) Console.WriteLine("  " + t);

/// <summary>★計算量の測り（2026-09-30 作者裁定「計算量の予算」）＝selfplay-batch が帯の報告とは別のファイル（POB_COST_OUT）へ書く1枚。
/// 1試合あたりの CPU 秒（プロセスの CPU÷種の数）・全長の件数の1試合の平均・推定の計算量・覆う割合・予算の上限と目標・「予算の内／超過」の1行。
/// ⚠判定は推定の計算量（決定的）で行い、実測の CPU は負荷（系の稼働・他のプロセスが使ったコア）と一緒に記録だけ（時刻と負荷で約7%揺れる）。</summary>
static class CostSys
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

    public readonly record struct Snap0(long Ticks, double Cpu, long Idle, long Kernel, long User, bool HasSys);

    public static Snap0 Snap()
    {
        long idle = 0, kernel = 0, user = 0;
        bool ok = OperatingSystem.IsWindows() && GetSystemTimes(out idle, out kernel, out user);
        return new Snap0(System.Diagnostics.Stopwatch.GetTimestamp(), System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds, idle, kernel, user, ok);
    }

    public static string Report(BatchSummary b, Snap0 s0, ulong seedBase, int gq)
    {
        var s1 = Snap();
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        double cpuAll = proc.TotalProcessorTime.TotalSeconds;                         // プロセスの CPU（起動・構築・助走・報告の前まで＝全部）
        double wallBatch = (s1.Ticks - s0.Ticks) / (double)System.Diagnostics.Stopwatch.Frequency;
        double cpuBatch = s1.Cpu - s0.Cpu;
        int N = Math.Max(1, b.N);
        double cpuPerMatch = cpuAll / N;
        var o = new System.Text.StringBuilder();
        o.AppendLine("# 計算量の測り（selfplay-batch）＝帯の報告とは別のファイル（件数と CPU 秒は揺れる・報告の全行バイト一致の検証を壊さないためここだけに書く）");
        o.AppendLine($"条件: seeds {seedBase}..{seedBase + (ulong)b.N - 1}（{b.N}種）・各{gq}四半期（助走を含む）・SOURCE-HASH {Environment.GetEnvironmentVariable("POB_SOURCE_HASH") ?? "（measure.sh の外）"}・論理コア {Environment.ProcessorCount}・サーバーGC {System.Runtime.GCSettings.IsServerGC}");
        o.AppendLine($"実測: 1試合あたりの CPU {cpuPerMatch:F1} 秒（プロセスの CPU {cpuAll:F1} 秒÷{b.N}種）・バッチの壁 {wallBatch:F1} 秒・バッチの CPU {cpuBatch:F1} 秒（平均 {cpuBatch / Math.Max(1e-9, wallBatch):F2} コア）");
        if (s0.HasSys && s1.HasSys)
        {
            long tot = (s1.Kernel - s0.Kernel) + (s1.User - s0.User), idle = s1.Idle - s0.Idle;
            double busy = tot > 0 ? 1.0 - (double)idle / tot : 0;
            double other = busy * Environment.ProcessorCount - cpuBatch / Math.Max(1e-9, wallBatch);
            o.AppendLine($"負荷: 系の稼働（平均）{busy:P1}・他のプロセスが使ったコア（平均）{other:F2}（⚠他の測りと重なると1試合の CPU 秒は膨らむ＝記録だけ）");
        }
        var tot2 = b.CostTotals ?? new long[CostLedger.Count];
        var avg = new long[CostLedger.Count];
        for (int i = 0; i < avg.Length; i++) avg[i] = (tot2[i] + N / 2) / N;
        o.AppendLine("計数（1試合の平均・助走を含む）:");
        o.AppendLine("  名前\t件数\t単価（µs）\t推定（秒）");
        for (int i = 0; i < CostLedger.Count; i++)
            o.AppendLine($"  {CostLedger.Names[i]}\t{avg[i]}\t{CostLedger.UnitMicros[i]:F5}\t{avg[i] * CostLedger.UnitMicros[i] * 1e-6:F2}");
        double est = CostLedger.EstimatedSeconds(avg);
        double cover = cpuPerMatch > 0 ? est / cpuPerMatch : 0;
        o.AppendLine($"推定の計算量（1試合）: {est:F1} 秒　覆う割合（推定÷実測の CPU）: {cover:P1}（★下がったら数えていない重い処理が増えた合図）");
        double lim = CostLedger.BudgetLimitEstSeconds, cov0 = CostLedger.BudgetCoverageAtLimit;
        double tgtEst = CostLedger.BudgetTargetCpuSeconds * cov0;
        o.AppendLine(lim > 0
            ? $"予算: 上限（推定）{lim:F1} 秒（許し +{CostLedger.BudgetSlack:P0}＝{lim * (1 + CostLedger.BudgetSlack):F1} 秒）・目標 1試合の CPU {CostLedger.BudgetTargetCpuSeconds:F0} 秒（バッチの条件）＝推定の単位で {tgtEst:F1} 秒（上限を置いたときの覆う割合 {cov0:P1}）"
            : $"予算: 上限（推定）未設定・目標 1試合の CPU {CostLedger.BudgetTargetCpuSeconds:F0} 秒（バッチの条件）");
        string verdict = lim <= 0 ? "上限が未設定"
            : est <= lim * (1 + CostLedger.BudgetSlack)
                ? (est <= tgtEst ? $"予算の内（目標にも届いた＝推定 {est:F1} ≦ 目標 {tgtEst:F1} 秒・余りは機能が使ってよい）" : $"予算の内（推定 {est:F1} ≦ 上限 {lim:F1}×{1 + CostLedger.BudgetSlack:F2} 秒・目標 {tgtEst:F1} 秒まであと {est - tgtEst:F1} 秒）")
                : $"★予算の超過（推定 {est:F1} ＞ 上限 {lim:F1}×{1 + CostLedger.BudgetSlack:F2} 秒）＝どこで払ったかを実装ログとコミットに書く";
        o.AppendLine($"判定: {verdict}");
        // ★止める規則（2026-09-30 作者裁定「計算量の予算」の6・README §1-2）＝種ごとの政体 exact／near と、その平均の標準誤差（標準偏差÷√種の数）。
        //   5種の腕は、判定の量（exact・near）が線から標準誤差の2倍以上離れていれば、10種を回さずに採否を決めてよい。
        //   ⚠種ごとの値は照合できた国の平均＝帯の報告の「忠実度の生値」（国ごとの率の平均）と国の欠けの分だけ違う＝両方を書く。
        if (b.SeedFidelity is { Count: > 0 } sf)
        {
            o.AppendLine("忠実度の種ごとの値（止める規則＝README §1-2）:");
            foreach (var (seed, ex, nr) in sf) o.AppendLine($"  seed{seed}\texact {ex * 100:F1}\tnear {nr * 100:F1}");
            (double Mean, double Se) Ms(IEnumerable<double> v)
            {
                var l = v.Where(x => !double.IsNaN(x)).ToList();
                if (l.Count == 0) return (double.NaN, double.NaN);
                double m = l.Average();
                double sd = l.Count > 1 ? Math.Sqrt(l.Sum(x => (x - m) * (x - m)) / (l.Count - 1)) : double.NaN;
                return (m, sd / Math.Sqrt(l.Count));
            }
            var (me, se) = Ms(sf.Select(x => x.Exact)); var (mn, sn) = Ms(sf.Select(x => x.Near));
            o.AppendLine($"揺れ: {sf.Count}種の平均 exact {me * 100:F1}（標準誤差 {se * 100:F1}pt）・near {mn * 100:F1}（標準誤差 {sn * 100:F1}pt）＝線から標準誤差の2倍（exact {2 * se * 100:F1}・near {2 * sn * 100:F1}pt）以上離れていれば10種を回さずに決めてよい");
        }
        return o.ToString();
    }
}
