namespace PriceOfBread.SimCore.Display;

/// <summary>段の押し釦の可否・ホバーの文・行の右の条件の文（法の表と画面のどちらも同じ 1 本を読む＝設計 §6-2）。
/// 旧は App の LawRowVM が持っていた式を、文字は 1 字も変えずに本体の目録へ移した。盤面は読むだけ（書かない・状態を持たない）。
/// 軸が無い行（財政のレバーの段）は議会を通らないので門も代価も賛否も無い（<paramref name="axis"/> が null）。</summary>
public static class LawView
{
    /// <summary>その段へ届くか（前提法 DAG）。現行以下（降格）は常に可。</summary>
    public static bool CanReach(World w, PolicyAxis? axis, int current, int level)
    {
        if (level <= current) return true;
        if (axis is not { } a) return true;   // 軸の無い行＝前提法 DAG が無い（レバーの段）
        return Government.LawPrereq(w, a, level, w.Coef).Ok;
    }

    /// <summary>その段へ動く「次の一歩」(現在→現在±1)の情報（GateInfo は副作用なし）。</summary>
    public static (bool Ok, double Cost, string Reason) NextStep(World w, PolicyAxis? axis, int current, int level)
    {
        if (level == current) return (true, 0, "");
        if (axis is not { } a) return (true, 0, "");   // 軸の無い行＝政治ゲートが無い
        bool inc = level > current;
        int next = current + (inc ? 1 : -1);
        return Government.GateInfo(w, a, inc, next);
    }

    /// <summary>制定できるか。前提法 DAG＋純支持ゲート＋政治資本の在庫＝AI と同一条件。</summary>
    public static bool CanSelect(World w, PolicyAxis? axis, int current, int level)
    {
        if (level == current) return true;   // 現行段は常に選べる（no-op）
        if (level > current && !CanReach(w, axis, current, level)) return false;   // 前提法 DAG
        return NextStep(w, axis, current, level).Ok;   // 純支持ゲート（議会の反対等）のみ
    }

    /// <summary>段の押し釦のホバー文。1 行目＝「法律名:段階名」（全分岐共通）、2 行目＝「現在の段階」／「制定:可能（政治資本消費:n）」／「制定:不可（…）」、以降＝勢力の賛否。
    /// 空文字＝出す情報が無い（軸の無い行）。</summary>
    public static string StageHint(World w, PolicyAxis? axis, string title, string stageLabel, int current, int level)
    {
        if (axis is not { } ax) return "";   // 軸の無い行（レバーの段）＝代価も賛否も無く、段の値はボタンのラベルそのもの
        string head = $"{title}:{stageLabel}";   // どの段のホバーかをホバー自身が名乗る
        if (level == current) return head + "\n現在の段階";
        if (level > current && !CanReach(w, ax, current, level))
            return head + "\n制定:不可\n" + PlayText.FormatPrereq(Government.LawPrereq(w, ax, level, w.Coef).Reason);
        var (ok, cost, reason) = NextStep(w, ax, current, level);
        // 下げる側の理由も出す：床（緊縮の下限・戦争中は軍を解散できない・軍事費に「なし」は無い）は下げる向きにしか現れない。
        // 前提で止まったときは理由をそのまま返す（政治資本の問題だと嘘をつかない）。文字列は Government の const が単一ソース。
        if (PlayText.IsPrereqStop(ok, reason))
            return head + "\n制定:不可\n" + PlayText.FormatPrereq(reason);
        // 常に同じ書式にまとめる（政治資本 0 でも「政治資本消費:0」と出す）。否決のときも必要量を出す。併せて誰がどの程度望んでいるかを添える。
        var body = new System.Text.StringBuilder(head);
        body.Append('\n').Append(PlayText.GateLine(ok, cost, reason));   // 文言は PlayText が単一ソース（Runner の play の報告と同じ）
        var parts = PolicyPreferences.SupportBreakdown(w, ax, level > current);
        foreach (var (who, wgt) in parts)
        {
            if (System.Math.Abs(wgt) < 0.005) continue;   // 誤差同然の項は出さない
            body.Append('\n').Append(who).Append(wgt > 0 ? ":賛成 " : ":反対 ").Append(System.Math.Abs(wgt * 100).ToString("F0"));
        }
        return body.ToString();
    }

    /// <summary>行の右の条件の文（改正できるか・必要な政治資本・押せない理由）。変更なし・軸の無い行は空。</summary>
    public static (bool Amendable, string Detail) Detail(World w, PolicyAxis? axis, int current, int target)
    {
        if (current == target || axis is not { } ax) return (false, "");
        bool inc = target > current;
        int next = current + (inc ? 1 : -1);
        var (ok, cost, reason) = Government.GateInfo(w, ax, inc, next);
        // 押せることは目標段の枠とボタンの有効・無効が示す。残すのは数字（必要な政治資本）と、押せない理由だけ。
        // 政治の門の否決の定型句は行に出さない（必要な政治資本はホバーの数字・残り a/b は政治の欄が出す）。他の理由（前提法・政体の制約など）はそのまま出す。
        string text = ok ? (cost > 0.001 ? $"政治資本 {cost * 100:F0}" : "")
                         : (Government.IsPoliticalGateReason(reason) ? "" : reason);
        return (ok, text);
    }
}

/// <summary>法の表の 1 行（法 1 本と、読み書きする世界）。群は画面の見出し。</summary>
public sealed record LawLine(LawDef Def, World W, string Group);

/// <summary>制度の表の 1 行（特許制度・株式会社・鉄道）。施行は発見の 2 値を読むだけ（自動で効く＝押す物ではない）。</summary>
public sealed record InstitutionLine(string Name, bool Enacted);

/// <summary>政治と法のページの表（設計 §3-3・§6-2）。法の表は <see cref="LawCatalog"/> を回して並べる＝法を足すとき画面は直さない。</summary>
public static class PoliticsTables
{
    /// <summary>群に載らない法の見出し。</summary>
    public const string OtherGroup = "その他";

    /// <summary>国有化の群の見出し（国有化の門が閉じている間は行ごと出さない）。</summary>
    public const string NationalizationGroup = "国有化";

    private static StageCell StageOf(LawLine l)
    {
        var d = l.Def; var w = l.W;
        int cur = d.Current(w);
        return new StageCell(d.Stages, d.LevelBase, cur, d.Target(w),
            lv => LawView.CanSelect(w, d.Axis, d.Current(w), lv),
            lv => LawView.StageHint(w, d.Axis, d.Name, d.StageName(lv), d.Current(w), lv) is { Length: > 0 } h ? h : null,   // 空なら null＝空の吹き出しを出さない
            lv => d.SetTarget(w.Policy, lv));
    }

    private static IReadOnlyList<LawLine> LawLines(MultiNationSim sim, World w)
    {
        var rows = new List<LawLine>();
        foreach (var d in LawCatalog.In(LawSlot.Law))
            if (d.IsVisible(w)) rows.Add(new LawLine(d, w, d.Group.Length > 0 ? d.Group : OtherGroup));
        if (w.Coef.NationalizationGate > 0)
            foreach (var d in LawCatalog.In(LawSlot.Nationalization))
                if (d.IsVisible(w)) rows.Add(new LawLine(d, w, NationalizationGroup));
        return rows;
    }

    /// <summary>法の表（群ごと＝政治参加・教育と労働・身分と土地・国家と軍・統治機構・国境管理・その他・国有化）。段の押し釦は議会の門を通る（AI と同じ口）。</summary>
    public static Table<LawLine> Laws { get; } = new("politics.laws", "法の表", Page.PoliticsLaw, LawLines,
        l => l.Def.Key, l => l.Def.Name,
        [
            Col.Text<LawLine>("name", "名前", l => l.Def.Name),
            new StageColumn<LawLine>("stage", "段階", StageOf),
            Col.Text<LawLine>("cost", "条件", l => LawView.Detail(l.W, l.Def.Axis, l.Def.Current(l.W), l.Def.Target(l.W)).Detail),
        ],
        l => l.Group);

    /// <summary>制度の表（施行か未施行か）。</summary>
    public static Table<InstitutionLine> Institutions { get; } = new("politics.institutions", "制度", Page.PoliticsLaw,
        (sim, w) => new InstitutionLine[] { new("特許制度", w.PatentLaw), new("株式会社", w.JointStock), new("鉄道", w.Railways) },
        l => l.Name, l => l.Name,
        [
            Col.Text<InstitutionLine>("name", "名前", l => l.Name),
            Col.Text<InstitutionLine>("state", "状態", l => l.Enacted ? "施行" : "未施行"),
        ]);
}
