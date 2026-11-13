namespace PriceOfBread.SimCore.Display;

/// <summary>段の押し釦（<see cref="StageCell"/>）の作り口。段へ動けるか（前提法・政治の門）と、ホバーの文（制定の可否・代価・勢力の賛否）を、
/// AI と同じ <see cref="Government"/> の門から引く＝表示に第二の判定式を置かない。書き込みは呼び手が渡す口（<see cref="NationPolicy"/> の目標の欄・近代化投資の段だけは <see cref="World.ModernizationFrac"/>）だけを通る。
/// 軸の無い行（財政のレバーの 5 段＝行政費・近代化投資・関税・政策金利）は議会を通らない＝門も代価も賛否も無い。
/// 盤面は読むだけ（書き込みは <paramref name="setTarget"/> を押した時だけ）。状態を持たない。</summary>
public static class StageGate
{
    /// <summary>法の表（<see cref="LawDef"/>）の 1 行から。</summary>
    public static StageCell Of(World w, LawDef def)
        => Make(w, def.Axis, def.Name, def.Stages, def.LevelBase, () => def.Current(w), () => def.Target(w), v => def.SetTarget(w.Policy, v));

    /// <summary>財政の項の表（<see cref="FiscalStage"/>）の 1 行から（税・教育費・軍事費の段）。</summary>
    public static StageCell Of(World w, FiscalStage st)
        => Make(w, st.Axis, st.Name, st.Stages, st.LevelBase, () => st.Current(w), () => st.Target(w), v => st.SetTarget(w.Policy, v));

    /// <param name="axis">null＝政治軸を持たない行（議会を通らない）</param>
    /// <param name="levelBase">最下段のエンジン側の段番号（軍事費だけ 1）</param>
    public static StageCell Make(World w, PolicyAxis? axis, string title, IReadOnlyList<string> labels, int levelBase,
        Func<int> current, Func<int> target, Action<int> setTarget)
    {
        int cur = current(), tgt = target();
        return new StageCell(labels, levelBase, cur, tgt,
            level => CanReachAndAfford(w, axis, level, cur),
            level => Hint(w, axis, title, labels, levelBase, level, cur),
            setTarget,
            level => Why(w, axis, level, cur));
    }

    /// <summary>選べない段の押せない理由（短い語）。選べる段は null。前提法は「前提 ◯◯:△△」・政治の門は賛成不足など（<see cref="Government"/> の理由の語）。</summary>
    public static string? Why(World w, PolicyAxis? axis, int level, int cur)
    {
        string? r = WhyRaw(w, axis, level, cur);
        // 理由は「次に押せるはずの段」（現在の隣）の 1 つだけに出す＝その先の段の同じ理由・「前段階が必要」の繰り返しは出さない（V3）
        if (r is not null && Math.Abs(level - cur) > 1)
        {
            if (r == "前段階が必要") return null;
            if (r == WhyRaw(w, axis, cur + Math.Sign(level - cur), cur)) return null;
        }
        return r;
    }

    private static string? WhyRaw(World w, PolicyAxis? axis, int level, int cur)
    {
        if (CanReachAndAfford(w, axis, level, cur) || axis is not { } a) return null;
        if (level > cur && !CanReach(w, axis, level, cur)) return Plain(PlayText.FormatPrereq(Government.LawPrereq(w, a, level, w.Coef).Reason));
        var (_, _, reason) = NextStep(w, axis, level, cur);
        return reason.Length == 0 ? null : Plain(Government.IsPoliticalGateReason(reason) ? reason : PlayText.FormatPrereq(reason));
    }

    /// <summary>エンジンの理由の語を、遊ぶ人に通じる短い語へ（エンジンの文字列は変えない・括弧の中の説明は消す）。</summary>
    private static string Plain(string reason) => (reason switch
    {
        "払えない請求が前提" => "財政難が前提",
        _ when reason.StartsWith("後退不能（", StringComparison.Ordinal) => "後退不能",
        _ => reason,
    }).Replace(':', '：');   // 日本語の文中のコロンは全角（V11）

    /// <summary>その段へ届くか（前提法の連なり）。現行以下（降格）は常に可。</summary>
    private static bool CanReach(World w, PolicyAxis? axis, int level, int cur)
    {
        if (level <= cur) return true;
        if (axis is not { } a) return true;   // 軸の無い行＝前提法の連なりが無い
        return Government.LawPrereq(w, a, level, w.Coef).Ok;
    }

    /// <summary>次の段（現在＋1）が前提の門を越えるか（政治の門は見ない）。段の押し釦の灰色と同じ 1 本（<see cref="CanReach"/>）。最上段は偽。</summary>
    public static bool NextOpen(World w, LawDef def)
    {
        int cur = def.Current(w);
        if (cur >= def.LevelBase + def.Stages.Length - 1) return false;
        return CanReach(w, def.Axis, cur + 1, cur);
    }

    /// <summary>その段へ動く「次の一歩」（cur→cur±1）の情報。副作用なし。</summary>
    private static (bool Ok, double Cost, string Reason) NextStep(World w, PolicyAxis? axis, int level, int cur)
    {
        if (level == cur) return (true, 0, "");
        if (axis is not { } a) return (true, 0, "");
        bool inc = level > cur;
        int next = cur + (inc ? 1 : -1);
        return Government.GateInfo(w, a, inc, next);
    }

    /// <summary>制定できるか。前提法の連なり＋純支持の門＋政治資本の在庫＝AI と同一条件。</summary>
    public static bool CanReachAndAfford(World w, PolicyAxis? axis, int level, int cur)
    {
        if (level == cur) return true;   // 現行段は常に選べる
        if (level > cur && !CanReach(w, axis, level, cur)) return false;
        return NextStep(w, axis, level, cur).Ok;
    }

    /// <summary>段の押し釦のホバーの文。1 行目＝「法の名前:段の名前」・2 行目＝現在／制定の可否と代価・以降＝勢力の賛否。
    /// 出す情報が無い（軸の無い行）時は null（空の文字だと空の吹き出しが出る）。</summary>
    public static string? Hint(World w, PolicyAxis? axis, string title, IReadOnlyList<string> labels, int levelBase, int level, int cur)
    {
        if (axis is not { } a) return null;
        string head = $"{title}:{labels[level - levelBase]}";
        if (level == cur) return head + "\n現在の段階";
        if (level > cur && !CanReach(w, axis, level, cur))
            return head + "\n制定：不可\n" + PlayText.FormatPrereq(Government.LawPrereq(w, a, level, w.Coef).Reason);
        var (ok, cost, reason) = NextStep(w, axis, level, cur);
        // 下げる側の床（緊縮の下限・戦争中は軍を解散できない）も前提で止まる＝政治資本の問題と偽らず、理由そのものを出す
        if (PlayText.IsPrereqStop(ok, reason))
            return head + "\n制定：不可\n" + PlayText.FormatPrereq(reason);
        var body = new System.Text.StringBuilder(head);
        body.Append('\n').Append(PlayText.GateLine(ok, cost, reason));
        foreach (var (who, wgt) in PolicyPreferences.SupportBreakdown(w, a, level > cur))
        {
            if (System.Math.Abs(wgt) < 0.005) continue;   // 誤差同然の項は出さない
            body.Append('\n').Append(who).Append(wgt > 0 ? ":賛成 " : ":反対 ").Append(System.Math.Abs(wgt * 100).ToString("F0"));
        }
        return body.ToString();
    }
}
