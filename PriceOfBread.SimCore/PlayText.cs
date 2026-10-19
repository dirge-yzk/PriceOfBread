namespace PriceOfBread.SimCore;

/// <summary>★(410) 画面（App の法律タブ・階級の表）と Runner の play の報告が**同じ文言**を言うための単一ソース（00-HARN の残り・(407) の申し送り）。
/// 文言だけを持つ＝盤面を読まず書かない・計算の順序も係数も持たない（シムの軌道に触れない）。
/// 生成側と比較側で文字列が散らない作法は <see cref="Government.ReasonNoMandate"/> と同じ（散ると表示が嘘をつく）。</summary>
public static class PlayText
{
    /// <summary>闘争度の目標の 5 本の名前。順は <c>Pop.MilLiving</c>・<c>MilVoice</c>・<c>MilWar</c>・<c>MilNation</c>・<c>MilFamine</c>（Politics の目標の和 sum の 5 項と同じ並び）。</summary>
    public static readonly string[] MilitancyPartNames = ["生活水準の不足", "政治的疎外", "厭戦", "民族的地位", "飢餓"];

    /// <summary>法の段のホバーの 2 行目（「制定:可能（政治資本消費:n）」／在庫の門の否決／それ以外の理由）。
    /// <paramref name="reason"/> は <see cref="Government.GateInfo(World, PolicyAxis, bool, int)"/> の理由。前提法・不可逆で止まった理由はここでなく <see cref="FormatPrereq"/>。</summary>
    public static string GateLine(bool ok, double cost, string reason)
        => ok ? $"制定:可能（政治資本消費:{cost * 100:F0}）"
        : reason == Government.ReasonNoCapitalStock ? $"制定:不可（政治資本消費:{cost * 100:F0}　不足）"
        : reason == Government.ReasonNoLegitimacy ? $"制定:不可（政治資本消費:{cost * 100:F0}　{reason}）"   // ★(419) 壁の一歩が正統性で止まった＝必要量の数字に〈正統性が不足〉を添える（在庫の不足と別の文言）
        : $"制定:不可（{reason}）";

    /// <summary>★(419) 前提法・不可逆で止まったか（＝政治の門〔賛成不足・在庫・正統性〕以外の理由が出ている）。法律タブ（<c>LawRowVM</c>）と Runner の報告（<see cref="NextStepLine"/>）が同じ 1 本を呼ぶ。</summary>
    public static bool IsPrereqStop(bool ok, string reason) => !ok && reason.Length > 0 && !Government.IsPoliticalGateReason(reason);

    /// <summary>前提未達の理由文を「◯◯:△△」レイアウトへ整形（作者指示2026-08-15）。
    /// 「◯◯（△△）が前提」→「前提 ◯◯:△△」のパターン変換のみ。合わない文（プロレタリア独裁のみ 等）はそのまま返す＝安全側。</summary>
    public static string FormatPrereq(string reason)
    {
        const string tail = "）が前提";
        int open = reason.IndexOf('（');
        if (open > 0 && reason.EndsWith(tail, System.StringComparison.Ordinal) && open + 1 < reason.Length - tail.Length)
        {
            string state = reason[(open + 1)..^tail.Length];
            if (!state.Contains('（') && !state.Contains('）'))   // 入れ子括弧は変換対象外（誤切り出しを避ける）
                return $"前提 {reason[..open]}:{state}";
        }
        return reason;
    }

    /// <summary>次の一歩の門を 1 行で言う（Runner の報告・法の段のホバーの 2 行目と同じ分岐）。
    /// 前提・不可逆で止まったとき（理由が在庫の門でも賛成不足でもない）は「制定:不可　<see cref="FormatPrereq"/>」、それ以外は <see cref="GateLine"/>。</summary>
    public static string NextStepLine(bool ok, double cost, string reason)
        => IsPrereqStop(ok, reason)
            ? "制定:不可　" + FormatPrereq(reason)
            : GateLine(ok, cost, reason);

    // ===== (435) 画面（App）と Runner の play の報告が同じ関数を呼ぶための単一ソース（テストプレイの道具の直し・出所＝初見の試しのテストプレイの報告 §1・§4）=====
    // 盤面を読むだけ・書かない（文言と並べ方だけ）。App はこれを呼ぶ形に替えた（写しを置かない）。

    /// <summary>政体の和名（英語の enum 名をそのまま出さない）。9 政体を網羅。画面のヘッダー・終局のランキング・Runner の報告が同じ 1 本を呼ぶ。</summary>
    public static string PolityRegimeJa(Polity? p) => p switch
    {
        Polity.Absolutist => "絶対君主制",
        Polity.Despotic => "専制君主制",
        Polity.ConstitutionalMonarchy => "立憲君主制",
        Polity.Shogunate => "幕府",
        Polity.Republic => "共和制",
        Polity.BourgeoisDictatorship => "ブルジョア独裁",
        Polity.ProletarianDictatorship => "プロレタリア独裁",
        Polity.MilitaryDictatorship => "軍事独裁",
        Polity.Anarchy => "無政府",
        _ => "—",
    };

    /// <summary>終局（暦が <see cref="Calendar.EndYear"/> に入った＝1949 年 12 月が終わった）の最終ランキングの 1 行。人口は内部単位（千人）のまま。</summary>
    public readonly record struct EndingStandingRow(int Order, string Name, double Score, Polity Polity, double Population, double Industry, bool IsPlayer);

    /// <summary>終局を過ぎたか（画面は <c>Calendar.StartYear + 経過日 / 年の日数 &gt;= EndYear</c> で最終ランキングを出す。世界の暦は同じ年＝<c>w.Year &gt;= EndYear</c>）。</summary>
    public static bool IsEnded(World w) => IsEnded(w.Year);

    /// <summary>★(456) 暦年が終局に入ったか（<c>year &gt;= Calendar.EndYear</c> の唯一の定義）。画面は表示暦の年〔<c>Calendar.StartYear + 経過日 / Calendar.DaysPerYear</c>〕を渡す（App は SimCore の暦を持たず経過日で数えるため）。</summary>
    public static bool IsEnded(int year) => year >= Calendar.EndYear;

    /// <summary>★(456) 終戦の結果の語（画面の終わった戦争の一覧・終戦の報せ・play の報告が同じ 1 本）。勝ち true＝勝利・負け false＝敗北・白紙 null＝白紙講和。判定は <see cref="EndedWarRecord.VerdictOf"/>（記録の <see cref="EndedWarRecord.WonBy"/>）。</summary>
    public static string WarResultWord(bool? won) => won is null ? "白紙講和" : won.Value ? "勝利" : "敗北";

    /// <summary>最終ランキング（国力 <see cref="NationalScore.Of"/> の降順）。画面の終局の表（<c>ShowEnding</c>）と Runner の報告が同じこの 1 本を読む。</summary>
    public static List<EndingStandingRow> EndingStandings(IEnumerable<World> worlds, World player)
    {
        var wl = worlds.ToList();
        var sc = NationalScore.Scores(wl);   // ★(449) 国力は全国分を 1 回で（国ごとに世界の総量を求め直さない）
        var order = Enumerable.Range(0, wl.Count).OrderByDescending(k => sc[k]).ToList();   // 安定な降順＝旧の OrderByDescending(Of) と同じ並び
        var rows = new List<EndingStandingRow>(wl.Count);
        for (int i = 0; i < order.Count; i++)
        {
            var w = wl[order[i]];
            rows.Add(new EndingStandingRow(i + 1, w.SelfPower?.Name ?? w.Region.Name, sc[order[i]],
                PowerAffinity.PolityFromShares(w),   // 導出政体＝ヘッダーの政体と同じ物差し（2026-08-21 作者裁定）
                w.TotalPopulation, w.MeanIndustrialization, ReferenceEquals(w, player)));
        }
        return rows;
    }

    /// <summary>自国の国力の内訳＋首位との差（画面の国名・地位のホバー <c>PlayerScoreTip</c> と Runner の報告が同じ 1 本）。
    /// ★(449) 内訳は <see cref="NationalScore.Breakdowns"/> の 1 本（6 項の割合の点）。合計も首位も同じ <see cref="NationalScore.Scores"/> の値＝内訳と首位との差が同じ物差しを見る（(435) で見つけた混在を直した）。</summary>
    public static string ScoreBreakdownText(World player)
    {
        var ws = NationalScore.NationWorldsOf(player);   // 実在の国の世界の一覧（盤に居ない世界は自分だけ）＝外交のランクと同じ集合
        int ip = ws.IndexOf(player);
        var sc = NationalScore.Scores(ws);
        var lines = NationalScore.Breakdowns(ws)[ip];
        double mine = sc[ip], top = 0; string topName = "";
        for (int i = 0; i < ws.Count; i++)
            if (sc[i] > top) { top = sc[i]; topName = ws[i].SelfPower?.Name ?? ""; }
        string gap = top > mine + 1e-9 ? $"首位 {topName} との差 {top - mine:F1}" : "首位";
        return string.Join("\n", lines.Select(l => $"{l.Label} {l.Points:F1}")) + $"\n合計 {mine:F1}　{gap}";
    }

    /// <summary>説明 API（#63）の内訳の項（|Δ|&lt;5e-4 を除き、寄与量の降順）。台帳が無い（説明モードオフ・非プレイヤー）なら空。画面の <c>ExplainTerms</c> と Runner の報告が同じ 1 本。</summary>
    public static List<ExplainLedger.Term> ExplainTerms(World w, string quantity)
    {
        var rows = new List<ExplainLedger.Term>();
        if (w.Explain is not { } led) return rows;
        foreach (var t in led.Get(quantity).Where(t => System.Math.Abs(t.Delta) >= 5e-4).OrderByDescending(t => t.Delta)) rows.Add(t);
        return rows;
    }

    /// <summary>内訳 1 行の文言（符号つき・理由）。画面の <c>ExplainTerms</c>／<c>Signed</c> と Runner が同じ書式。</summary>
    public static string SignedLine(double delta, string reason, System.Func<double, string> fmt)
        => $"{(delta >= 0 ? "＋" : "−")}{fmt(System.Math.Abs(delta))}　{reason}";

    /// <summary>階級の層の名前に使う階級名（画面の上流・中流・下流の分け方）。</summary>
    public const string CapitalistClass = "資本家", BureaucratClass = "官僚", ArtisanClass = "職人", MerchantClass = "商人";

    /// <summary>上流（閉鎖階級・資本家）／中流（近代エリート・官僚・職人・商人）／下流 の 3 層（画面の識字・満足度・闘争度の内訳の分け方と同じ 1 本）。</summary>
    public static int ClassLayerOf(Pop p)
        => (p.Closed || p.ClassName == CapitalistClass) ? 0 : (p.IsModernElite || p.ClassName is BureaucratClass or ArtisanClass or MerchantClass) ? 1 : 2;

    public static readonly string[] ClassLayerNames = ["上流", "中流", "下流"];

    /// <summary>3 層の人口加重平均（人口が 0 の層は出さない）。画面の POP タブの識字・満足度・闘争度の内訳（<c>LiteracyClassTerms</c> ほか）と Runner の報告が同じ 1 本。</summary>
    public static List<(string Layer, double Mean)> ClassLayerMeans(World w, System.Func<Pop, double> selector)
    {
        double[] mass = new double[3], sz = new double[3];
        foreach (var p in w.AllPops()) { int layer = ClassLayerOf(p); mass[layer] += p.Size * selector(p); sz[layer] += p.Size; }
        var rows = new List<(string, double)>();
        for (int i = 0; i < 3; i++) if (sz[i] > 1e-9) rows.Add((ClassLayerNames[i], mass[i] / sz[i]));
        return rows;
    }

    /// <summary>物価水準・為替の文言（金融の主フラグが無い世界は〈—〉）。画面の財政タブの金融と Runner の報告が同じ 1 本。</summary>
    public static string PriceLevelText(World w) => !w.MonetaryEnabled ? "—" : w.PriceLevel.ToString("P0");
    public static string ExchangeRateText(World w) => !w.MonetaryEnabled ? "—" : w.ExchangeRate.ToString("P0");
    /// <summary>★(452) 教育費の名乗り（App の財政の行と play の報告が同じ関数を呼ぶ）。教育費は額でなく〈予算の割合〉＝置いた率（<see cref="Policy.EducationSpendingTarget"/>）と実効の率（<see cref="World.EducationSpending"/>）。
    /// 実効が国庫の買う率になるのは法と到達を通した後（Simulation の CompulsoryEduFunding）＝その率は別に出す（play の報告）。表示だけ＝シミュは動かさない。</summary>
    public static string EducationSpendingText(World w) => $"教育費（予算の割合） {w.Policy.EducationSpendingTarget:P0}　実効 {w.EducationSpending:P0}";

    /// <summary>兌換カバー率（基礎貨幣÷広義貨幣）の文言。銀行が無い世界は〈—〉。画面の財政タブの金融と Runner の報告が同じ 1 本。</summary>
    public static string CoverText(World w)
        => w.Bank is null ? "—" : $"{(w.BroadMoney.Value > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0):P0}";

    /// <summary>政治資本の在庫の文言（現在 a ／ 最大 b・超過があれば添える）。画面の政治タブと Runner の報告が同じ 1 本。</summary>
    public static string PoliticalCapitalText(World w)
    {
        double bMax = w.PoliticalCapitalMax, aNow = System.Math.Max(0, bMax - w.MandateSpent);
        // ★(407) 反証役 rf〔bv〕中#4：(405) の壁の一歩の後は S＞b＝在庫は 0 に張り付いて超えた分が見えなかった＝超えた分を出す
        return w.MandateSpent > bMax ? $"{aNow * 100:F0} / {bMax * 100:F0}（超過 {(w.MandateSpent - bMax) * 100:F0}）" : $"{aNow * 100:F0} / {bMax * 100:F0}";
    }

    /// <summary>政治資本のホバー（b と実行度の材料のデータ行）。画面の政治タブと Runner の報告が同じ 1 本。</summary>
    public static string PoliticalCapitalTip(World w)
    {
        var c = w.Coef;
        double bMax = w.PoliticalCapitalMax;
        double aff = Ideologies.SupportAt(Ideologies.National(w), PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
        double sup = System.Math.Max(0, System.Math.Min(1, (aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin)));
        double pe = PowerShares.ParliamentEffective(w);
        double seats = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];
        double maxSeat = System.Math.Max(w.PartySeats[0], System.Math.Max(w.PartySeats[1], w.PartySeats[2]));
        double split = seats > 1e-9 ? System.Math.Max(0, System.Math.Min(1, 1.0 - maxSeat / seats)) : 0;
        // ⚠2026-09-21（検証で発見）：旧は係数 MandateRefillQuarters（＝40）をそのまま出していた＝実行度を無視した嘘の数。満タンまでの実時間は 40/実行度。
        double rate = PoliticalQuantities.PoliticalCapitalRate(w);
        double toFull = !(rate > 0) ? 0 : c.MandateRefillQuarters / rate * (bMax > 0 && w.MandateSpent > bMax ? w.MandateSpent / bMax : 1.0);   // ★(407) S＞b（壁の一歩の後）は空から満タンの S÷b 倍
        return $"正統性 {w.Legitimacy:F2}　現政体の支持 {sup:F2}"
             + $"\n議会の実効統制 {pe:F2}　議席の分裂 {split:F2}　内戦 {(w.InCivilWar ? 1 : 0)}"
             + $"\n実行度 {rate:F2}　満タンまでの四半期 {toFull:F0}";
    }
}
