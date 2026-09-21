using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>03地理 手順0（2026-07-28）：陸塊テーブル（landmass.json）の整合番人。
/// 旧・矩形ContinentOfは23島陸塊をユーラシアへ接着し85.6%の国対を「陸続き」にしていた＝島の海越え判定を一次データで固定する。</summary>
public class LandmassTests
{
    [Fact]
    public void 島嶼は本土と別陸塊で海越えになる()
    {
        // 首都座標（world.json）での代表対：英仏＝ドーバー海峡（旧・同一大陸で陸続きだった）
        Assert.False(Landmass.Same(51.5, -0.1, 48.9, 2.3));     // ロンドン↔パリ＝海越え
        Assert.False(Landmass.Same(35.7, 139.7, 37.6, 127.0));  // 江戸↔漢城＝対馬海峡（日本の大陸進出に船が要る）
        Assert.False(Landmass.Same(-18.9, 47.5, 34.0, -5.0));   // マダガスカル↔モロッコ（旧・島国が船なしで大陸国に侵攻できた）
        Assert.False(Landmass.Same(51.5, -0.1, 4.9, 114.9));    // ロンドン↔ボルネオ（旧・11,260kmの陸路侵略が可能だった）
    }

    [Fact]
    public void 大陸内は同一陸塊で陸続きのまま()
    {
        Assert.True(Landmass.Same(59.9, 30.3, 31.2, 121.5));    // ペテルブルク↔上海＝ユーラシア（第4段4-4の趣旨＝露の東方進出は陸路）
        Assert.True(Landmass.Same(48.9, 2.3, 52.5, 13.4));      // パリ↔ベルリン
        Assert.True(Landmass.Same(48.9, 2.3, 28.6, 77.2));      // パリ↔デリー（ユーラシア陸塊＝陸単価が律速する設計・03 §5-1）
        Assert.True(Landmass.Same(40.7, -74.0, 19.4, -99.1));   // NY↔メキシコ＝北米
    }

    [Fact]
    public void 陸塊IDは全行が非空で読み込める()
    {
        // A1裁定＝C#フォールバック無し：埋め込みが壊れていればここで即死する（静かな(0,0)の穴を作らない）
        Assert.NotEqual("", Landmass.Of(51.5, -0.1));
        Assert.Equal(Landmass.Of(51.5, -0.1), Landmass.Of(53.48, -2.24));   // ロンドンとマンチェスター＝同じグレートブリテン島
        Assert.Equal(Landmass.Of(35.68, 139.69), Landmass.Of(34.69, 135.5));   // 江戸と大阪＝本州
    }
}
