namespace PriceOfBread.SimCore.Display;

/// <summary>行の右の条件の文（法の表と画面のどちらも同じ 1 本を読む＝設計 §6-2）。段の押し釦の可否とホバーの文は <see cref="StageGate"/> の 1 本
/// （旧はここにも同じ式の写しがあった＝2026-10-23 に消した）。盤面は読むだけ（書かない・状態を持たない）。
/// 軸が無い行（財政のレバーの段）は議会を通らないので門も代価も賛否も無い（<paramref name="axis"/> が null）。</summary>
public static class LawView
{
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

    private static StageCell StageOf(LawLine l) => StageGate.Of(l.W, l.Def);   // 段の可否とホバーの文は StageGate の 1 本（財政の項と同じ）

    private static IReadOnlyList<LawLine> LawLines(MultiNationSim sim, World w)
    {
        var rows = new List<LawLine>();
        foreach (var d in LawCatalog.In(LawSlot.Law))
            if (d.IsVisible(w)) rows.Add(new LawLine(d, w, d.Group.Length > 0 ? d.Group : OtherGroup));
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
