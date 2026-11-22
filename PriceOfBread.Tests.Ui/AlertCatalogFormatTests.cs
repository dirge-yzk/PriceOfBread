using System.Collections.Generic;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.Tests;

/// <summary>alerts.csv の書式の穴：AlertCatalog.Make は FormatException を握りつぶして文の型をそのまま出す＝全 id を実際の引数の数で組み立て、穴が残らないことを確かめる。</summary>
public class AlertCatalogFormatTests
{
    // 呼び手が渡す引数（GameViewModel.News.cs の QueueNews の呼び出しと同じ個数）
    private static readonly Dictionary<string, object[]> Args = new()
    {
        ["ev_start"] = [],
        ["ev_depression"] = [0.25],
        ["ev_recession"] = [0.12],
        ["ev_famine"] = [0.7],
        ["ev_credit_heat"] = [0.9],
        ["ev_austerity"] = [],
        ["ev_austerity_end"] = [],
        ["ev_devaluation"] = [],
        ["ev_revolution"] = ["英", "立憲君主政"],
        ["ev_civilwar"] = [],
    };

    [Fact]
    public void 全てのidを実際の引数で組み立てると書式の穴が残らない()
    {
        foreach (var id in AlertCatalog.Ids)
        {
            Assert.True(Args.TryGetValue(id, out var args), $"引数の表に無い id: {id}");
            string text = AlertCatalog.Make(id, args!).Text;
            Assert.False(text.Contains('{') || text.Contains('}'), $"書式の穴が残っている: {id} → {text}");
        }
    }

    [Fact]
    public void 政変の文は国名と政体を含み頭に国名を重ねない()
    {
        string text = AlertCatalog.Make("ev_revolution", "英", "立憲君主政").Text;
        Assert.Equal("英で政変が起き、立憲君主政になった", text);
        Assert.True(AlertCatalog.WholeSentence("ev_revolution"));
        Assert.False(AlertCatalog.WholeSentence("ev_start"));
    }
}
