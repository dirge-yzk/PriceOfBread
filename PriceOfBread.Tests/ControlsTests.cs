using PriceOfBread.App;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段1：共通の表・線グラフ・半円の部品と、名前つきの系列の倉の番人。描画そのものは --uishot が見る＝ここは Avalonia に依らない部分
/// （目録→文字への変換・並べ替えと絞り込み・扇形の Path データ・系列のセーブの往復・貿易の表の保存）だけを見る。</summary>
public class ControlsTests
{
    private sealed record R(string Key, string Name, string? Grp, double Amount, double Share, string Note);

    private static Table<R> Sample(params R[] rows) => new("t", "試験", Page.Market, (_, _) => rows, r => r.Key, r => r.Name,
    [
        Col.Text<R>("name", "名前", r => r.Name),
        Col.NumberNote<R>("amount", "金額", Unit.MoneyPerMonth, r => r.Amount, r => r.Note),
        Col.Number<R>("share", "割合", Unit.Share, r => r.Share),
        Col.Text<R>("note", "備考", r => r.Note),
    ], r => r.Grp ?? "");

    private static TableSnapshot Snap(params R[] rows)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789);
        return TableSnapshot.From(Sample(rows).Read(sim, sim.Nations[0].World));
    }

    [Fact]
    public void 表の列の名前と値は単位から決まる書式で文字になる_最初の文字の列は幅を取り_二つ目以降は上限を持つ()
    {
        var s = Snap(new R("a", "茶", null, 1200, 0.45, "(×1.50)"));
        Assert.Equal(new[] { "名前", "金額", "割合", "備考" }, s.Columns.Select(c => c.Name).ToArray());
        Assert.Equal(new[] { ColumnKind.Text, ColumnKind.Number, ColumnKind.Number, ColumnKind.Text }, s.Columns.Select(c => c.Kind).ToArray());
        Assert.True(double.IsPositiveInfinity(s.Columns[0].MaxWidth));   // 最初の文字の列＝残りを全部取る
        Assert.Equal(96, s.Columns[3].MaxWidth);                           // 二つ目以降＝長い名前で他を圧迫しない
        Assert.All(s.Columns.Where(c => c.Kind == ColumnKind.Number), c => Assert.True(c.MinWidth > 0));   // 固定の幅でなく最小幅
        var cells = s.Rows[0].Cells;
        Assert.Equal("茶", cells[0].Text);
        Assert.Equal("£1.2k/月(×1.50)", cells[1].Text);   // 単位から「£1.2k/月」・添え書きは後ろ
        Assert.Equal(1200, cells[1].Sort);
        Assert.Equal("45%", cells[2].Text);
        Assert.True(double.IsNaN(cells[0].Sort));
    }

    [Fact]
    public void 列の名前は目録の規則に合う_貿易の表の見出しも()
    {
        foreach (var t in new[] { TradeTables.Imports, TradeTables.Exports })
        {
            Assert.All(t.ColumnInfos, c => Assert.True(DisplayNames.IsValid(c.Name), $"{t.Key} の列の名前が規則に合わない: {c.Name}"));
            Assert.Equal(t.ColumnInfos.Count, t.ColumnInfos.Select(c => c.Key).Distinct().Count());
            Assert.True(DisplayNames.IsValid(t.Name));
        }
    }

    [Fact]
    public void 並べ替え_数の列は数で_同値は元の並び_数でない値は昇順でも降順でも末尾()
    {
        var s = Snap(new R("a", "甲", null, 50, 0.1, ""), new R("b", "乙", null, double.NaN, 0.2, ""), new R("c", "丙", null, 300, 0.3, ""), new R("d", "丁", null, 50, 0.4, ""));
        Assert.Equal(new[] { "a", "b", "c", "d" }, s.View(null, -1, false).Select(r => r.Key).ToArray());     // 本体の並びのまま
        Assert.Equal(new[] { "a", "d", "c", "b" }, s.View(null, 1, false).Select(r => r.Key).ToArray());     // 昇順：同値の甲・丁は元の並び・NaN は末尾
        Assert.Equal(new[] { "c", "a", "d", "b" }, s.View(null, 1, true).Select(r => r.Key).ToArray());      // 降順でも NaN は末尾
        Assert.Equal(new[] { "d", "c", "b", "a" }, s.View(null, 0, false).Select(r => r.Key).ToArray());     // 文字の列は文字の順（コード順＝丁・丙・乙・甲）
        Assert.Equal(new[] { "a", "b", "c", "d" }, s.View(null, 0, true).Select(r => r.Key).ToArray());
    }

    [Fact]
    public void 絞り込みは名前の一部_大文字小文字を問わない_群の中だけ並べ替える()
    {
        var s = Snap(new R("a", "Tea", "x", 1, 0, ""), new R("b", "Coffee", "y", 5, 0, ""), new R("c", "Cocoa", "x", 9, 0, ""), new R("d", "Cotton", "y", 2, 0, ""));
        Assert.Equal(new[] { "b", "c", "d" }, s.View("co", -1, false).Select(r => r.Key).ToArray());
        // 群は最初に現れた順のまま（x → y）・群の中だけ金額の降順
        Assert.Equal(new[] { "c", "a", "b", "d" }, s.View(null, 1, true).Select(r => r.Key).ToArray());
        Assert.Empty(s.View("存在しない", -1, false));
    }

    [Fact]
    public void 段と動詞と半円の列は中身を持つ_文字は現在の段の名前()
    {
        int picked = -1; bool invoked = false;
        var stage = new StageCell(["なし", "低率", "中率", "高率"], 0, 2, 3, l => l != 1, l => l == 1 ? null : $"h{l}", l => picked = l);
        var verb = new VerbCell("侵略", true, () => invoked = true);
        var share = new ShareCell([("国家", 0.6), ("在地", 0.4)]);
        var t = new Table<int>("t", "試験", Page.PoliticsLaw, (_, _) => [0], r => r.ToString(), r => "行",
        [
            new StageColumn<int>("s", "段", _ => stage), new VerbColumn<int>("v", "動詞", _ => verb), new ShareColumn<int>("p", "内訳", _ => share),
        ]);
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789);
        var snap = TableSnapshot.From(t.Read(sim, sim.Nations[0].World));
        var cells = snap.Rows[0].Cells;
        Assert.Equal("中率", cells[0].Text);
        Assert.Same(stage, cells[0].Extra);
        Assert.Equal("侵略", cells[1].Text);
        Assert.Equal("国家 60% 在地 40%", cells[2].Text);
        ((StageCell)cells[0].Extra!).Select(3); ((VerbCell)cells[1].Extra!).Invoke();
        Assert.Equal(3, picked); Assert.True(invoked);
        Assert.False(stage.CanSelect(1)); Assert.True(stage.CanSelect(3));
    }

    [Fact]
    public void 動詞の振り分け_確かめの文を持つ動詞は確かめの口を通らずには発動しない()
    {
        int fired = 0; VerbCell? asked = null;
        var plain = new VerbCell("補助金", true, () => fired++);
        var risky = new VerbCell("侵略", true, () => fired++, "目標を侵略する");
        TableView.DispatchVerb(plain, null);                       // 確かめの文が無い物は口が無くても発動
        Assert.Equal(1, fired);
        TableView.DispatchVerb(risky, null);                       // 確かめの文があるのに口が無い＝発動しない（確かめ無しの発動を許さない）
        Assert.Equal(1, fired);
        TableView.DispatchVerb(risky, v => asked = v);             // 口があれば口へ渡すだけ（発動は口の先が決める）
        Assert.Equal(1, fired); Assert.Same(risky, asked);
        TableView.DispatchVerb(plain, v => asked = v);             // 確かめの文が無い物は口を通らない
        Assert.Equal(2, fired); Assert.Same(risky, asked);
    }

    [Fact]
    public void 半円の扇形のPathデータ_旧の関数と同じ_ほぼ0は空_ほぼ全部は半円で閉じる()
    {
        Assert.Equal("", HalfPie.Slice(35, 35, 34, 0.5, 0.5005));
        Assert.Equal("M 1,35 A 34,34 0 0 1 69,35 Z", HalfPie.Slice(35, 35, 34, 0, 1.0));
        // 左端（9 時）から右へ：f=0.5 は頂点（真上）
        Assert.Equal("M 35,35 L 1.00,35.00 A 34,34 0 0 1 35.00,1.00 Z", HalfPie.Slice(35, 35, 34, 0, 0.5));
        Assert.Equal("", HalfPie.Arc(35, 35, 32, 0.0));
        Assert.Equal("M 3,35 A 32,32 0 0 1 35.00,3.00", HalfPie.Arc(35, 35, 32, 0.5));
        Assert.Equal(new Avalonia.Size(72, 38), HalfPie.BoxOf(34));   // 箱 幅＝2r＋4・高さ＝r＋4
    }

    [Fact]
    public void 線グラフの範囲は有限の点だけから求める_非有限は落とす()
    {
        Assert.True(LineChart.Range([3, double.NaN, 9, double.PositiveInfinity, -2], out double min, out double max));
        Assert.Equal(-2, min); Assert.Equal(9, max);
        Assert.False(LineChart.Range([double.NaN, double.NegativeInfinity], out _, out _));
        Assert.False(LineChart.Range([], out _, out _));
    }

    [Fact]
    public void 名前つきの系列の倉_同じ名前は同じ実体_セーブは名前と値の組で往復し_足した系列は旧セーブに無くても空で始まる()
    {
        var a = new SeriesStore();
        a.Of(SeriesKeys.Population).AddRange([1, 2, 3.5]);
        a.Of(SeriesKeys.Gdp).AddRange([10, double.NaN]);
        Assert.Same(a.Of(SeriesKeys.Population), a.Of(SeriesKeys.Population));
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) a.Write(bw);
        ms.Position = 0;
        var b = new SeriesStore();
        b.Of(SeriesKeys.Treasury).Add(99);   // 読み手が先に持っていた系列は、書いた側に無ければそのまま残る
        using (var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true)) b.ReadFrom(br);
        Assert.Equal(new[] { 1.0, 2.0, 3.5 }, b.Of(SeriesKeys.Population).ToArray());
        Assert.Equal(10, b.Of(SeriesKeys.Gdp)[0]);
        Assert.True(double.IsNaN(b.Of(SeriesKeys.Gdp)[1]));
        Assert.Equal(new[] { 99.0 }, b.Of(SeriesKeys.Treasury).ToArray());
        Assert.Empty(b.Of(SeriesKeys.Gini));   // 旧セーブに無い系列は空で始まる
        // 往復後の再書き込みはバイト一致（読んだ順＝書いた順の名前の並びを保つ）
        using var ms2 = new MemoryStream(); using (var bw2 = new BinaryWriter(ms2, System.Text.Encoding.UTF8, leaveOpen: true)) a.Write(bw2);
        var c = new SeriesStore(); ms2.Position = 0; using (var br2 = new BinaryReader(ms2, System.Text.Encoding.UTF8, leaveOpen: true)) c.ReadFrom(br2);
        using var ms3 = new MemoryStream(); using (var bw3 = new BinaryWriter(ms3, System.Text.Encoding.UTF8, leaveOpen: true)) c.Write(bw3);
        Assert.Equal(ms2.ToArray(), ms3.ToArray());
    }

    [Fact]
    public void 系列の名前は全て違い_名前を挟んでも既存の系列はずれない()
    {
        var keys = typeof(SeriesKeys).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!).ToList();
        Assert.Equal(18, keys.Count);   // ヘッダーの年次系列 18 本（位置で並べていた旧の配列と同数）
        Assert.Equal(keys.Count, keys.Distinct().Count());
        // 途中に別の名前を挟んだ倉で書いても、読み手は名前で引く＝人口がずれない
        var w = new SeriesStore(); w.Of(SeriesKeys.Industry).Add(7); w.Of("nation.extra").Add(0.5); w.Of(SeriesKeys.Population).Add(32.4);
        using var ms = new MemoryStream(); using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) w.Write(bw);
        ms.Position = 0;
        var r = new SeriesStore(); using (var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true)) r.ReadFrom(br);
        Assert.Equal(32.4, r.Of(SeriesKeys.Population)[0]);
        Assert.Equal(0.5, r.Of("nation.extra")[0]);
    }

    [Fact]
    public void 貿易の表は額の降順で_行の和が総額と一致し_月割は清算の月数で割った値()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        for (int i = 0; i < 12; i++) sim.StepQuarter();   // 清算を何度か通す（貿易の台帳が立つ）
        var w = sim.Nations[0].World;
        var imp = TradeTables.Imports.Read(sim, w);
        var exp = TradeTables.Exports.Read(sim, w);
        var (te, ti) = TradeTables.Totals(w);
        double mpc = w.Coef.MonthsPerClearing;
        double sumI = imp.Rows.Sum(r => ((NumberCell)r.Cells[1]!).Value) * mpc;
        double sumE = exp.Rows.Sum(r => ((NumberCell)r.Cells[1]!).Value) * mpc;
        Assert.Equal(ti, sumI, 6);   // 総額は行の和（額が 0 の財は両方から外れる）
        Assert.Equal(te, sumE, 6);
        Assert.True(imp.Rows.Count + exp.Rows.Count > 0, "清算を通しても貿易の行が 1 つも無い＝この試験の前提が崩れた");
        foreach (var d in new[] { imp, exp })
        {
            var vals = d.Rows.Select(r => ((NumberCell)r.Cells[1]!).Value).ToList();
            Assert.Equal(vals.OrderByDescending(v => v).ToList(), vals);   // 額の降順
            Assert.Equal(d.Rows.Count, d.Rows.Select(r => r.Key).Distinct().Count());   // 行の鍵が重ならない
            Assert.All(d.Rows, r => { Assert.Equal(4, r.Cells.Count); Assert.True(((NumberCell)r.Cells[2]!).Value is >= 0 and <= 1.0000001); });
        }
    }
}
