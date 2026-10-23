using PriceOfBread.App;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段4：ピン留め（App/Pins.cs・ViewModels/GameViewModel.Pins.cs）の番人。参照は鍵だけ・同じ鍵は 1 つ・セーブの往復・消えた行は薄くして残す。</summary>
public class PinsTests
{
    private static PinRef Row(string table, string row, string name = "名前") => new(PinKind.Row, "Production", table, row, name, "表");

    [Fact]
    public void 参照の文字の往復_壊れた行は捨てる()
    {
        var p = new PinRef(PinKind.Chart, "FiscalMoney", "chart.treasury", "", "国庫", "");
        Assert.Equal(p, PinRef.Decode(p.Encode()));
        var r = Row("production.goods", "62", "パン");
        Assert.Equal(r, PinRef.Decode(r.Encode()));
        Assert.Null(PinRef.Decode(""));
        Assert.Null(PinRef.Decode("r\tx"));
        Assert.Null(PinRef.Decode("z\t\ta\tb\tc\td"));   // 知らない種類
        Assert.Null(PinRef.Decode("r\t\t\tb\tc\td"));    // 表の鍵が空
    }

    [Fact]
    public void 同じ鍵は1つだけ_付け外しの切替_上限を超えて付けない()
    {
        var s = new PinStore();
        Assert.True(s.Toggle(Row("t", "1")));
        Assert.True(s.Add(Row("t", "1", "別名")));   // すでにあれば何もしない
        Assert.Single(s.Pins);
        Assert.False(s.Toggle(Row("t", "1")));       // 外れた
        Assert.Empty(s.Pins);
        for (int i = 0; i < PinStore.MaxPins; i++) Assert.True(s.Add(Row("t", i.ToString())));
        Assert.False(s.Add(Row("t", "extra")));
        Assert.Equal(PinStore.MaxPins, s.Pins.Count);
    }

    [Fact]
    public void セーブの書き出しと読み込みの往復_並びが保たれ_壊れた行は捨てる()
    {
        var s = new PinStore();
        s.Add(Row("production.goods", "62", "パン")); s.Add(new PinRef(PinKind.Header, "", "GDP", "", "GDP", "")); s.Add(Row("pop.regions", "ロンドン", "ロンドン"));
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) s.Write(bw);
        ms.Position = 0;
        var t = new PinStore();
        using (var br = new BinaryReader(ms)) t.ReadFrom(br);
        Assert.Equal(s.Pins, t.Pins);

        // 件数だけ立って中身が壊れている
        using var ms2 = new MemoryStream();
        using (var bw = new BinaryWriter(ms2, System.Text.Encoding.UTF8, leaveOpen: true)) { bw.Write(2); bw.Write("壊れた"); bw.Write(Row("t", "1").Encode()); }
        ms2.Position = 0;
        var u = new PinStore();
        using (var br = new BinaryReader(ms2)) u.ReadFrom(br);
        Assert.Single(u.Pins);
    }

    [Fact]
    public void 旧セーブに無い追記ブロックは空_ReadPinsはEOFで落ちない()
    {
        using var ms = new MemoryStream();
        using var br = new BinaryReader(ms);
        Assert.Empty(GameViewModel.ReadPins(br));
    }
}
