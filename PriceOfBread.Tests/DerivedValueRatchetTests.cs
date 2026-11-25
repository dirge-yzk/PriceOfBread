using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1364) 導いた値のラチェット（作者 2026-11-25「許可する入力数・出力数を規則として規定するのはどうだろうか。具体的な数は任せる」）。
/// 7 つの導いた値（闘争度・正統性・厭戦・政治意識・満足度・不満度・天命）の**書き手・入力・出力の数**が、今の数の表を超えないこと。
/// 数え方は <see cref="ValueScan"/> の 1 本（エンジンの本体＝SimCore・Instruments と表示と試験は数えない）。
///
/// ── 目標と今の表 ──
/// 目標（AI の推奨・作者「具体的な数は任せる」）＝書き手 1・入力 3・出力 3。今の数はそれを大きく超える＝**今の数の表だけが猶予**（例外の枠は作らない）。
/// 減らす変更はそのまま通る（上限であって固定値ではない）。減らしたら表を下げる（＝宣言）。増やす変更は赤＝増やすなら因果の最長の道へ寄せる（設計思想⑪）。
/// ⚠入力は機械で数えた下限に偏る＝局所変数は 1 項に畳む（式が局所変数に入る量は、局所変数の数がそのまま入力の数）。文の指紋と手数えの表は置いていない。
/// ⚠不満度は欄でなく関数 FactionSnapshot.DiscontentOf＝書き手の欄が無い（0）・関数の呼び出しを読みとして数える。
/// ⚠追加の量は後の波の番人の線が足す（最初の対象は上の 7 つ）。
/// 表の数は合流の木で数え直す（ut が .Value を、lb が書き手の周りの行を同じ波で動かす）。</summary>
public class DerivedValueRatchetTests
{
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    // 名前・関数か・書き手・入力・出力＝2026-11-25 実測（基 11bfbfcd6・measurements/m1364_20261125_prereg_wr.txt）。
    [Theory]
    [InlineData("Militancy", false, 16, 7, 40)]        // 闘争度（力学 1・運搬 1・事件のスパイク・緩和）
    [InlineData("Legitimacy", false, 11, 8, 47)]       // 正統性
    [InlineData("WarExhaustion", false, 3, 7, 34)]     // 厭戦（段 1・2 で作り直し予定＝rd の材料）
    [InlineData("Consciousness", false, 2, 1, 14)]     // 政治意識（力学 1＋運搬 1）
    [InlineData("Satisfaction", false, 1, 2, 12)]      // 満足度（EconomyStep の 1 行）
    [InlineData("DynasticMandate", false, 2, 1, 4)]    // 天命（World.DynasticMandate・Politics の 2 行）
    [InlineData("DiscontentOf", true, 0, 0, 2)]        // 不満度（関数 FactionSnapshot.DiscontentOf・書き手の欄なし・呼び出し 2）
    public void 導いた値の書き手と入力と出力は表を超えない(string name, bool isFunction, int writers, int inputs, int outputs)
    {
        var texts = ValueScan.EngineFiles(SimCoreDir()).Select(File.ReadAllText).ToList();
        int w = isFunction ? 0 : texts.Sum(t => ValueScan.CountWriters(t, name));
        int i = isFunction ? 0 : ValueScan.CountInputs(texts, name);
        int o = isFunction ? texts.Sum(t => ValueScan.CountCalls(t, name)) : texts.Sum(t => ValueScan.CountReads(t, name));
        string hint = $"（{name}：書き手 {w}/{writers}・入力 {i}/{inputs}・出力 {o}/{outputs}・目標 1/3/3）＝増やすなら導く元を 1 本へ寄せる（設計思想⑪）。減らしたら表を下げる。";
        Assert.True(w <= writers, "書き手が表を超えた" + hint);
        Assert.True(i <= inputs, "入力が表を超えた" + hint);
        Assert.True(o <= outputs, "出力が表を超えた" + hint);
    }

    /// <summary>対照：表の量が実際に走査できている（数えた先が空で緑にしない）。書き手のある量は 1 以上・入力と出力も 1 以上。</summary>
    [Fact]
    public void 走査が空でない()
    {
        var texts = ValueScan.EngineFiles(SimCoreDir()).Select(File.ReadAllText).ToList();
        Assert.True(texts.Count >= 50, $"エンジンの .cs が少ない（{texts.Count}）");
        foreach (var m in new[] { "Militancy", "Legitimacy", "WarExhaustion", "Consciousness", "Satisfaction", "DynasticMandate" })
        {
            Assert.True(texts.Sum(t => ValueScan.CountWriters(t, m)) >= 1, m + " の書き手が 0＝走査が空");
            Assert.True(texts.Sum(t => ValueScan.CountReads(t, m)) >= 1, m + " の出力が 0＝走査が空");
        }
        Assert.True(texts.Sum(t => ValueScan.CountCalls(t, "DiscontentOf")) >= 1, "DiscontentOf の呼び出しが 0＝走査が空");
    }
}
