using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(410) 画面（法律タブのホバー・階級のホバー）と Runner の play の報告が同じ文言を言う単一ソース <see cref="PlayText"/> の番人。
/// 期待値は文字列のリテラル（実装の式から作らない）。画面側の文言（<c>LawRowVM.StageHint</c> の 2 行目）は PolicyPoliticsTests が同じ文字列を見ている。</summary>
public class PlayTextTests
{
    [Fact]
    public void 門の1行は通る_在庫不足_その他の理由を言い分ける()
    {
        Assert.Equal("制定:可能（政治資本消費:24）", PlayText.GateLine(true, 0.24, ""));
        Assert.Equal("制定:可能（政治資本消費:0）", PlayText.GateLine(true, 0, ""));
        Assert.Equal("制定:不可（政治資本消費:107　不足）", PlayText.GateLine(false, 1.07, Government.ReasonNoCapitalStock));
        Assert.Equal("制定:不可（賛成不足）", PlayText.GateLine(false, 0, Government.ReasonNoMandate));
        // 文言の出所は Government の const（生成側と比較側で散らさない）＝const の値そのものを固定する
        Assert.Equal("政治資本の残りが不足", Government.ReasonNoCapitalStock);
        Assert.Equal("賛成不足", Government.ReasonNoMandate);
    }

    [Fact]
    public void 前提法の理由は_前提_語_段_へ整形し合わない文はそのまま返す()
    {
        Assert.Equal("前提 教育法:公教育以上", PlayText.FormatPrereq("教育法（公教育以上）が前提"));
        Assert.Equal("前提 参政権:制限選挙以上", PlayText.FormatPrereq("参政権（制限選挙以上）が前提"));
        Assert.Equal("戦時のみ緩和可", PlayText.FormatPrereq("戦時のみ緩和可"));                  // 合わない文は安全側でそのまま
        Assert.Equal("前提法（（入れ子）が前提）が前提", PlayText.FormatPrereq("前提法（（入れ子）が前提）が前提"));   // 入れ子の括弧は変換しない
    }

    [Fact]
    public void 次の一歩の1行は前提で止まったときだけ前提の整形へ分かれる()
    {
        Assert.Equal("制定:可能（政治資本消費:31）", PlayText.NextStepLine(true, 0.31, ""));
        Assert.Equal("制定:不可（賛成不足）", PlayText.NextStepLine(false, 0, Government.ReasonNoMandate));
        Assert.Equal("制定:不可（政治資本消費:53　不足）", PlayText.NextStepLine(false, 0.53, Government.ReasonNoCapitalStock));
        Assert.Equal("制定:不可　前提 教育法:公教育以上", PlayText.NextStepLine(false, 0, "教育法（公教育以上）が前提"));
        Assert.Equal("制定:不可　後退不能（支持層が確立済み）", PlayText.NextStepLine(false, 0, "後退不能（支持層が確立済み）"));
    }

    [Fact]
    public void 闘争度の目標の5本の名前は画面のホバーの語と同じ5つで重ならない()
    {
        Assert.Equal(["生活水準の不足", "政治的疎外", "厭戦", "民族的地位", "飢餓"], PlayText.MilitancyPartNames);
        Assert.Equal(5, new System.Collections.Generic.HashSet<string>(PlayText.MilitancyPartNames).Count);
    }
}
