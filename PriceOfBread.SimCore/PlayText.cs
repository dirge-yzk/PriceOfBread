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
}
