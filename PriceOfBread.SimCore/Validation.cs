namespace PriceOfBread.SimCore;

/// <summary>
/// 検証ハーネス（本格テストの土台）。§11.6 の保存則（破ると即クラッシュ）の"上位"に、
/// 「経済が"良い状態"か」を測る指標と合格基準を置く。実行後の <see cref="MultiNationSim"/> から
/// 決定論的に算出し（<see cref="Metrics"/>）、目標帯と突き合わせて合否を出す（<see cref="Validation"/>）。
/// 帯の値は初期提案＝運用しながら較正する。基準を満たしたらテスト群に固定して回帰防止に使う。
/// </summary>
public sealed record NationMetrics(
    string Name,
    double TechLevel,
    double Industrialization,
    double MeanSatisfaction,    // 人口加重の満足度（全地域・大衆のみ＝NationalScore.MeanSatisfaction・2026-09-25）
    double PriceFloorFrac,      // 床(≤0.051×参照価格)に張り付く財の割合（本拠地市場）
    double PriceCeilFrac,       // 天井(≥19.9×参照価格)に張り付く財の割合
    double PriceMidFrac,        // 中間＝健全な価格発見の割合（高いほど良い＝二極化の逆指標）
    double UrbanShare,
    double MeanLiteracy,
    double Population,
    double Treasury,
    int Colonies,
    bool SpawnedDuringPlay = false,   // ★プレイ中に生まれた国（分離独立・内戦分裂）＝WorstSat の対象外
    // ★2026-09-03（H-02 診断）：満足度が 0.00 になる国が出たとき、「受け手が居ないのか／本当に貧しいのか」を
    //   再走せずに切り分けるための最小の内訳。⚠診断専用＝判定には一切使わない。
    int ActiveRegions = 0, int FoundingRegions = 0, double FoodFill = 0, double PopCount = 0,
    // ★2026-09-27 (68)：直近 Validation.SatRecentQuarters 四半期の満足度の平均と振れ幅（最大−最小）。SelfPlayMatch.Run が走行中に読んだ値から書く＝
    //   履歴を持たない呼び手（Runner の検証レポート・テスト）は NaN＝文言に出さない。⚠診断専用＝判定には一切使わない。
    double SatRecentMean = double.NaN, double SatRecentSwing = double.NaN);

public sealed record SimMetrics(
    int Year,
    IReadOnlyList<NationMetrics> Nations,
    int DeadEndGoods,                 // どの国でも誰も需要しない財（植民地財を除く）
    double IndustrializationSpread,   // 工業化度の最大−最小（技術駆動の差別化の証拠）
    int Wars,                         // 「開戦」ログ件数
    int TotalColonies,                // 全勢力（清等を含む）の植民地総数（保存の確認）
    double TradeVolume,               // 世界市場の直近総取引量
    int WorldPricesMoved,             // 初期1.0から動いた世界価格の財数（価格発見）
    int IndependentColonies = 0,      // 独立した旧植民地の数（監査2026-07-12 死12：保存検査＝保有＋独立が初期総数と一致）。★回1-1：独立の確定で本土になった植民地格の州（SovereignColonialRegions）を含む
    int PromotedColonies = 0,         // 州へ昇格した植民地の累計（2026-08-09＝保存検査の第3の出口）
    int AbsorbedColonies = 0,         // ★J-006（2026-09-05）：同名州の保有国へ渡って消滅した Colony＝第4の出口
    int InitialColonies = -1,         // 構築時の植民地総数（-1＝未捕捉の旧経路＝検査スキップ）
    IReadOnlyList<string>? DeadEndNames = null)   // 死蔵財の**顔ぶれ**（2026-07-31）：件数だけでは「どの財が死んでいるか」が分からず、
                                                 // 落ちた検査の原因追跡のたびに単走を回し直していた（batch604四半期・53カ国の顔ぶれは未測定と記録されていた）。診断のみ＝挙動非依存
{
    public double MeanMidFrac => Nations.Count > 0 ? Nations.Average(n => n.PriceMidFrac) : 0;
    public double WorstMidFrac => Nations.Count > 0 ? Nations.Min(n => n.PriceMidFrac) : 0;
    public double MeanSat => Nations.Count > 0 ? Nations.Average(n => n.MeanSatisfaction) : 0;
    /// <summary>満足度が最も低い国の値。⚠★**プレイ中に生まれた国（分離独立・内戦分裂）は数えない**（2026-08-02）。
    /// ── なぜ除くか ──────────────────────────────────────────────────────────
    /// 最小値は順序統計量の裾なので、**分離独立が起きやすくなる変更ほど機械的に赤くなる**。
    /// 割れて生まれたばかりの1州の残余国家が不幸なのは、分離独立の機構が正しく働いた結果であって世界の破綻ではない。
    /// 検査の意味は「**確立した国家が崩壊しない**」であるべきで、開始時の国が落ちれば従来どおり赤になる。
    /// ⚠開始国が1つも残っていない世界では全体の最小へ退避する。
    /// ── ⚠この検査を読むときの罠（2026-08-02に実際に踏んだ）────────────────────────
    /// <see cref="NationMetrics.Name"/> は**首都州の名前**であって国名ではない。だから「マドリード」は
    /// スペイン**本体**（首都州＝マドリード）だが、「バーミンガム」「セビーリャ」「インドール」「クラウゼンブルク」は
    /// **首都でない州が首都になっている**＝本体が割れた側である。名前だけで「分離独立だ」と判断してはならない——
    /// 判定は必ず <see cref="NationMetrics.SpawnedDuringPlay"/> フラグで行うこと。</summary>
    public double WorstSat
    {
        get
        {
            double lo = double.MaxValue;
            foreach (var n in Nations) if (!n.SpawnedDuringPlay && n.MeanSatisfaction < lo) lo = n.MeanSatisfaction;
            if (lo < double.MaxValue) return lo;
            return Nations.Count > 0 ? Nations.Min(n => n.MeanSatisfaction) : 0;
        }
    }
    // ★2026-08-02：**どの国が最小か**を検査に添える（診断のみ・判定には使わない）。
    // ⚠これが無いと「満足度・最小国が0.28」まで分かっても**誰が壊れたか分からず直せない**。実際に2度踏んだ。
    /// <summary>★H-02（2026-09-03）：最小国の内訳（州数・建国時州数・主食充足・人口）。⚠診断専用＝判定に使わない。
    /// 「満足度0.00」が受け手不在なのか本当の困窮なのかを、再走なしで分けるために検査文へ添える。</summary>
    public string WorstSatDetail
    {
        get
        {
            NationMetrics? worst = null;
            foreach (var n in Nations) if (!n.SpawnedDuringPlay && (worst is null || n.MeanSatisfaction < worst.MeanSatisfaction)) worst = n;
            if (worst is null) foreach (var n in Nations) if (worst is null || n.MeanSatisfaction < worst.MeanSatisfaction) worst = n;
            return worst is null ? "" : $"・州{worst.ActiveRegions}/{worst.FoundingRegions}・主食{worst.FoodFill:F2}・人口{worst.PopCount:F0}"
                + (double.IsNaN(worst.SatRecentMean) ? "" : $"・直近{Validation.SatRecentQuarters}四半期 平均{worst.SatRecentMean:F2} 振れ{worst.SatRecentSwing:F2}");   // ★(68) 谷の読みかを添える
        }
    }

    public string WorstSatName
    {
        get
        {
            // ⚠Name は**首都州の名前**。国名と取り違えないよう、割れて生まれた国には印を添える（2026-08-02に実際に取り違えた）。
            string nm = ""; double lo = double.MaxValue;
            foreach (var n in Nations) if (!n.SpawnedDuringPlay && n.MeanSatisfaction < lo) { lo = n.MeanSatisfaction; nm = n.Name; }
            if (nm.Length > 0) return nm;
            foreach (var n in Nations) if (n.MeanSatisfaction < lo) { lo = n.MeanSatisfaction; nm = n.Name + "・分離独立"; }
            return nm;
        }
    }
}

/// <summary>実行後の sim から検証指標を算出する（副作用なし・決定論的）。</summary>
public static class Metrics
{
    // マジックナンバー命名（挙動不変・値据え置き）：価格分布の床/天井の判定倍率と価格発見の閾値。
    private const double PriceFloorRefFrac = 0.051;    // 床＝参照価格のこの倍率以下
    private const double PriceCeilRefFrac = 19.9;      // 天井＝参照価格のこの倍率以上
    private const double PriceMovedThreshold = 0.05;   // 初期1.0からの乖離＝価格発見とみなす閾値

    public static SimMetrics Compute(MultiNationSim sim)
    {
        var nations = new List<NationMetrics>();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var r = w.Region;   // 本拠地（価格分布の代表。他州は満足度・人口側で合算）
            // ★2026-09-25：満足度は唯一の実装 NationalScore.MeanSatisfaction を呼ぶ（旧＝同じ式の手書きの複製＝全POP）。識字は全POPのまま。
            double popS = 0, litS = 0;
            foreach (var p in w.AllPops()) { popS += p.Size; litS += p.Literacy * p.Size; }
            int floor = 0, ceil = 0, mid = 0, ng = 0;
            foreach (var g in r.Goods)
            {
                double price = r.Markets[g.Id].Price;
                if (price <= g.RefPrice * PriceFloorRefFrac) floor++;
                else if (price >= g.RefPrice * PriceCeilRefFrac) ceil++;
                else mid++;
                ng++;
            }
            nations.Add(new NationMetrics(
                w.Region.Name, w.TechLevel, r.TotalIndustrialization,   // 工業化の差別化ガードも新旧合計で測る（ヴィンテージ2026-07-10）
                NationalScore.MeanSatisfaction(w),
                ng > 0 ? (double)floor / ng : 0,
                ng > 0 ? (double)ceil / ng : 0,
                ng > 0 ? (double)mid / ng : 0,
                w.UrbanShare, popS > 0 ? litS / popS : 0,
                w.TotalPopulation, w.Treasury, w.SelfPower?.Colonies ?? 0,
                // ★最小満足度検査の対象外＝(a)プレイ中に生まれた国（分離建国・従来） (b)**戦争で建国時の州の半分以上を
                //   失った残余国**（2026-08-04・実測＝ベルリンを失いシュテッティンだけ残ったプロイセンが満足0.15で赤）。
                //   どちらも困窮は戦争/分裂の帰結であってエンジンの欠陥ではない。
                //   ⚠首都では判定しない＝割譲の遷都機構が Id を0へスワップして首都ロールを引き継がせるため、
                //     「建国首都の喪失」はエンジンに存在しない概念（首都判定で2度実測に外れて確定）。州の頭数は遷都と無関係。
                //   国そのものは他の全検査（破綻・保存則・デッドロック）の対象のまま。
                // ★2026-09-03（キューH-02）：**人口ゼロの国**も対象外（3つ目の除外）。
                //   ⚠実測＝seed28 の「マレー」は 州1/1・主食1.00・**人口0** で満足度0.00＝困窮ではなく**分母が空**。
                //   満足度は人口加重平均なので、住民が居ない国では**定義されない**（比のガードは分母で書く、の同型）。
                //   「確立した国家が崩壊しないこと」を見るこの検査にとって、住民ゼロの残骸は測る対象ではない。
                //   ⚠国そのものは他の全検査（破綻・保存則・デッドロック）の対象のまま。
                (w.SelfPower?.SpawnedDuringPlay ?? false) || Validation.IsWarMauledRump(w) || !(popS > 0),
                Validation.ActiveRegionCount(w), w.FoundingActiveProvinces, w.Region.FoodFill, popS));   // ★H-02 診断（判定には使わない）
        }
        int deadEnd = 0;
        var deadEndNames = new List<string>();
        // ★時代盲の是正（2026-08-31）：300Q探索（1858年）で弩級戦艦・戦闘機・ラジオ・高等教育が「下流の無い財」として
        //   硬性ゲートを赤にした＝時代未到達の財に下流が無いのは正しい挙動であって死蔵ではない（短尺の物差しのアーティファクト）。
        //   世界最先端の技術水準でまだ解禁が始まっていない財（AdvancedTechScale=0）は検査対象から外す。
        //   604Q完走（1934年・全財解禁済み）では従来と同じ集合を検査する＝受け入れ物差しは不変。
        double maxTech = 0;
        foreach (var n in sim.Nations) maxTech = Math.Max(maxTech, n.World.TechLevel);
        foreach (var g in Catalog.Goods)
        {
            if (g.IsColonial) continue;
            if (!(Catalog.AdvancedTechScale(g.Id, maxTech) > 0)) continue;   // 時代未到達＝下流が無いのが正しい（否定形＝NaNも除外側）
            double maxD = 0;
            foreach (var n in sim.Nations) { var mk = n.World.Region.Markets; if (g.Id < mk.Length) maxD = Math.Max(maxD, mk[g.Id].LastDemand); }   // コードベース規約の < Markets.Length ガード（最小テスト世界で Markets が全財を持たない場合に落ちない）
            if (maxD < 0.5) { deadEnd++; deadEndNames.Add($"{g.Id}:{g.Name}"); }   // 顔ぶれも残す（診断のみ）
        }
        double indMax = 0, indMin = double.MaxValue;
        foreach (var x in nations) { indMax = Math.Max(indMax, x.Industrialization); indMin = Math.Min(indMin, x.Industrialization); }
        double indSpread = nations.Count > 0 ? indMax - indMin : 0;
        int wars = sim.Diplomacy.WarsStarted;   // 測定短縮①（2026-07-09）：ログ文字列走査→スカラーカウンタ（同値・Log.Enabled=falseのleanバッチでも正しい）
        int totalCol = 0; foreach (var p in sim.Diplomacy.Powers) totalCol += p.Colonies;
        double tradeVol = 0; foreach (var v in sim.Market.Volume) tradeVol += v;
        // 世界価格の初期値は WorldPriceInitFromRefPrice>0.5 なら RefPrice 起点（InternationalMarket と同じ規則）＝
        // 「動いた」は初期値からの比で測る（1.0固定と比べると参照価格が1から離れた財が取引ゼロでも動いた扱いになっていた）。
        bool initFromRef = sim.Coef.WorldPriceInitFromRefPrice > 0.5;
        int moved = 0;
        for (int g = 0; g < sim.Market.Price.Count; g++)
        {
            double init = initFromRef && g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 1.0;
            if (Math.Abs(sim.Market.Price[g] / init - 1.0) > PriceMovedThreshold) moved++;
        }
        return new SimMetrics(sim.Year, nations, deadEnd, indSpread, wars, totalCol, tradeVol, moved,
            sim.Diplomacy.IndependentColonies.Count + sim.Diplomacy.SovereignColonialRegions, sim.Diplomacy.PromotedColonies, sim.Diplomacy.AbsorbedColonies, sim.Diplomacy.InitialColonyCount, deadEndNames);
    }
}

public enum Verdict { Pass, Warn, Fail }

public sealed record Check(string Name, Verdict Verdict, string Value, string Target);

/// <summary>
/// 合格基準：指標を目標帯と突き合わせて 合格/注意/不合格 を出す。
/// 帯は「現在の検証済み水準」に較正＝回帰検出の基準線（これを下回ったら劣化）。
/// 価格中間帯の理想は本来もっと高い(≥0.55)が、二極化＝コブウェブ振動×損失吸収の脆さ
/// （生産・在庫・利潤の会計の結合系）という既知の構造課題で、専用の会計再設計が要る（在庫投資を
/// 損失でなく資本家の資産として認識する等）。マクロ層（工業化差別化・戦争・交易・保存・決定論）は健全。
/// </summary>
public static class Validation
{
    /// <summary>★2026-09-27 (68)：「満足度・最小国」に添える直近の四半期の数（平均と振れ幅＝<see cref="NationMetrics.SatRecentMean"/>）。
    /// ⚠判定は終局の1四半期の値のまま。貯蓄が月収の1〜2か月分しかない国は日用品を買える月と買えない月が交互に来る＝終局の1値が
    /// 2か月周期の谷を読むことがある（(68) の診断＝r64 の不合格9種のうち8四半期平均が0.05を割ったのは4種）＝計器に限界を自己申告させる。
    /// 8＝2年＝周期（2か月）の12倍かつ偶奇の四半期を4つずつ含む。報告専用（力学は読まない）。</summary>
    public const int SatRecentQuarters = 8;

    public static List<Check> Evaluate(SimMetrics m)
    {
        var checks = new List<Check>();
        // 大きいほど良い指標の3段判定
        void Higher(string name, double v, double pass, double warn, string fmt = "F2")
            => checks.Add(new Check(name, v >= pass ? Verdict.Pass : v >= warn ? Verdict.Warn : Verdict.Fail,
                v.ToString(fmt), $"≥{pass.ToString(fmt)}合格 / ≥{warn.ToString(fmt)}注意"));
        // 少ないほど良い（件数）。detail＝実測値に添える内訳（診断のみ・判定には使わない）
        void Fewer(string name, int v, int pass, int warn, string detail = "")
            => checks.Add(new Check(name, v <= pass ? Verdict.Pass : v <= warn ? Verdict.Warn : Verdict.Fail,
                v + detail, $"≤{pass}合格 / ≤{warn}注意"));
        void Truth(string name, bool ok, string val)
            => checks.Add(new Check(name, ok ? Verdict.Pass : Verdict.Fail, val, "成立＝合格"));

        // 価格中間帯・満足度の帯＝現在の検証済み水準（回帰検出用）。理想はより高い＝会計再設計の宿題（クラス doc 参照）。
        Higher("価格中間帯・平均", m.MeanMidFrac, 0.40, 0.30);
        Higher("価格中間帯・最小国", m.WorstMidFrac, 0.25, 0.15);
        Fewer("下流の無い財", m.DeadEndGoods, 0, 2, m.DeadEndNames is { Count: > 0 } dn ? $"＝{string.Join("・", dn)}" : "");   // 顔ぶれを実測値に添える（件数だけでは原因が追えない）
        // ★満足度の4本の帯は 2026-09-22 に分位の写像で導き直した（階層3分割で主食が台帳から抜け、水準が構造的に下がったため）。
        //   ⚠終局（604Q）の国だけでは旧の門が旧分布の全標本より下（終局の最小国は旧0.56〜0.59）＝分位が0に潰れて写像が定義されない
        //   ⇒ 比べる相手を「全国×四半期の国の平均満足度」に取る（隔離計器 tiersatprobe・seed1-3×604Q）。
        //   旧0.55/0.45/0.38/0.30 の旧分布での分位＝13.9%/4.0%/0.9%/0.01% → 新分布の同じ分位＝0.26/0.11/0.07/0.04（2回目。1回目は 0.25/0.12/0.07/0.04）。
        //   ⚠0.01%（11万標本の下から約11件）は推定できない裾＝2回目の推定は床の0.00に落ちた（注意帯が空洞になる）→ 1回目の0.04を残す。
        // ★2026-09-25：満足度の分母を大衆のみ（Politics.IsMassPop）へ揃えたので、同じ手順で4本を導き直した（隔離計器 satqmap・seed1-3×668Q・
        //   q＝旧（全POP）分布で「値＜旧値」の割合 → 新（大衆）分布の q 分位・2回目を小数2桁で採用）。
        //   平均 合格 0.26（q 11.40%）→0.24／注意 0.11（2.55%）→0.09・最小国 合格 0.07（0.56%）→0.05／注意 0.04（0.13%）→0.02（2回目 0.0238）。
        Higher("満足度・平均", m.MeanSat, 0.24, 0.09);
        checks.Add(new Check("満足度・最小国",
            m.WorstSat >= 0.05 ? Verdict.Pass : m.WorstSat >= 0.02 ? Verdict.Warn : Verdict.Fail,
            $"{m.WorstSat:F2}（{m.WorstSatName}{m.WorstSatDetail}）", "≥0.05合格 / ≥0.02注意"));   // ★誰がなぜ壊れたかを添える（H-02）
        Higher("工業化の差別化", m.IndustrializationSpread, 0.50, 0.20);
        Truth("戦争が起きる", m.Wars >= 1, $"{m.Wars}件");
        Truth("交易が成立", m.TradeVolume > 0.01, m.TradeVolume.ToString("F2"));
        Higher("世界価格の発見", m.WorldPricesMoved, 11, 5, "F0");
        // 植民地の保存（監査2026-07-12 死12）：植民地は移転（戦争・植民地化）と独立でしか動かない＝保有合計＋独立数が初期総数と常に一致（二重付与・消失の検出。旧TotalColoniesは算出だけで読者ゼロだった）。
        if (m.InitialColonies >= 0)
            Truth("植民地の保存", m.TotalColonies + m.IndependentColonies + m.PromotedColonies + m.AbsorbedColonies == m.InitialColonies, $"{m.TotalColonies}+独立{m.IndependentColonies}+昇格{m.PromotedColonies}+吸収{m.AbsorbedColonies}＝初期{m.InitialColonies}");   // ★J-006：吸収（同名州の保有国へ渡った Colony の消滅）も出口   // 州昇格を第3の出口として算入（2026-08-09）
        return checks;
    }

    public static int Count(IEnumerable<Check> checks, Verdict v)
    {
        int c = 0; foreach (var ch in checks) if (ch.Verdict == v) c++; return c;
    }

    /// <summary>戦争で解体された残余国か（2026-08-04）＝Active州数が建国時の半分未満。
    /// ★首都では判定できない：割譲/建国の遷都機構（Diplomacy 外9・Formations）が Id を0へスワップして
    ///   首都のロールを引き継がせる＝「建国首都の喪失」という同一性はエンジンに存在しない（首都基準は2度実測で外れた）。
    ///   州の頭数は遷都に影響されず、征服で回復すれば除外も自然に解ける。FoundingActiveProvinces=0（旧セーブ/最小世界）は除外しない。</summary>
    /// <summary>★H-02 診断（2026-09-03）：Active な州の数（満足度0の国が「州を持っているのに貧しい」のか
    /// 「そもそも受け手の州が無い」のかを分ける）。⚠診断専用。</summary>
    internal static int ActiveRegionCount(World w)
    {
        int n = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) n++;
        return n;
    }

    internal static bool IsWarMauledRump(World w)
    {
        // ★2026-09-03（H-02）：**人口**でも同じ「半分」を見る。州は保っているのに住民が消えた国（実測＝州1/1・人口14千人）は、
        //   州の頭数では永久に捕まらない。⚠新しい閾値は作らない＝下の州の判定と同じ「半分未満」。
        if (w.FoundingPopulation > 0 && w.TotalPopulation * 2 < w.FoundingPopulation) return true;
        if (w.FoundingActiveProvinces <= 0) return false;
        int act = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) act++;
        return act * 2 < w.FoundingActiveProvinces;
    }

    /// <summary>法の序数が enum の定義域に収まっているか（2026-08-02）。外れた軸名を1つ返す（なければ null）。
    /// 現在値（World）と目標（Policy）の両方＝目標側だけが壊れる形もあるため（実際にそれが起きた）。
    /// ★網羅の基準は Government.Step が StepEnum で駆動している**全 enum 軸**（並び順もそこに合わせる）＝
    ///   一部だけ見る番人は「クランプを1つ忘れれば必ず落ちる」という設置目的を満たさない。
    ///   w.Pension は SocialSecurityLaw からの導出値、p.Pension は施行経路を持たない死んだフィールドなので対象外。</summary>
    public static string? LawOutOfRange(World w)
    {
        var p = w.Policy;
        if (!System.Enum.IsDefined(w.EducationLaw) || !System.Enum.IsDefined(p.EducationLaw)) return "教育法";
        if (!System.Enum.IsDefined(w.Suffrage) || !System.Enum.IsDefined(p.Suffrage)) return "参政権";
        if (!System.Enum.IsDefined(w.WomenRights) || !System.Enum.IsDefined(p.WomenRights)) return "女性の権利";
        if (!System.Enum.IsDefined(w.MonetaryLaw)) return "通貨制度";   // 目標は int の p.MonetaryTarget（現在値のみ検査）
        if (!System.Enum.IsDefined(w.Censorship) || !System.Enum.IsDefined(p.Censorship)) return "報道";
        if (!System.Enum.IsDefined(w.LaborUnion) || !System.Enum.IsDefined(p.LaborUnion)) return "労組";
        if (!System.Enum.IsDefined(w.WorkHours) || !System.Enum.IsDefined(p.WorkHours)) return "労働時間";
        if (!System.Enum.IsDefined(w.CornLaws) || !System.Enum.IsDefined(p.CornLaws)) return "穀物法";
        if (!System.Enum.IsDefined(w.LandReform) || !System.Enum.IsDefined(p.LandReform)) return "土地改革";
        if (!System.Enum.IsDefined(w.SerfEmancipation) || !System.Enum.IsDefined(p.SerfEmancipation)) return "農奴解放";
        if (!System.Enum.IsDefined(w.SocialSecurityLaw) || !System.Enum.IsDefined(p.SocialSecurityLaw)) return "社会保障法";
        if (!System.Enum.IsDefined(w.ConscriptionLaw) || !System.Enum.IsDefined(p.ConscriptionLaw)) return "徴兵法";
        if (!System.Enum.IsDefined(w.SecretBallot) || !System.Enum.IsDefined(p.SecretBallot)) return "秘密投票法";
        if (!System.Enum.IsDefined(w.Bureaucracy) || !System.Enum.IsDefined(p.Bureaucracy)) return "官僚制度";
        if (!System.Enum.IsDefined(w.ImmigrationRestriction) || !System.Enum.IsDefined(p.ImmigrationRestriction)) return "移民制限法";
        if (!System.Enum.IsDefined(w.SpecialistExitLaw) || !System.Enum.IsDefined(p.SpecialistExitLaw)) return "技術者流出禁止法";   // ★段⑤
        if (!System.Enum.IsDefined(w.StateReligion) || !System.Enum.IsDefined(p.StateReligion)) return "国教法";
        if (!System.Enum.IsDefined(w.Antitrust) || !System.Enum.IsDefined(p.Antitrust)) return "独占禁止法";
        if (!System.Enum.IsDefined(w.Guild) || !System.Enum.IsDefined(p.Guild)) return "同業組合法";   // ★特権 段4
        if (!System.Enum.IsDefined(w.OpiumPossession) || !System.Enum.IsDefined(p.OpiumPossession)) return "阿片の保有法";
        if (!System.Enum.IsDefined(w.ColonialSuffrage) || !System.Enum.IsDefined(p.ColonialSuffrage)) return "植民地参政権";
        if (!System.Enum.IsDefined(w.MigrantNativeSuffrage) || !System.Enum.IsDefined(p.MigrantNativeSuffrage)) return "移民原住民参政権";
        return null;
    }

}
