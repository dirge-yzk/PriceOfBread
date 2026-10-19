using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (456) 終戦の判定の 1 本（EndedWarRecord.VerdictOf）と、それを読む語・終局の判定。画面の終わった戦争の一覧と終戦の報せ・play の報告・講和（ResolvePeace）が
/// 旧は 4 か所に別々の写しを持っていた（政体の内戦で食い違った）。期待値は手計算のリテラル（白紙の線 0.25）。
/// </summary>
public class WarVerdictTests
{
    private const double W = 0.25;   // 既定の白紙の線 Coefficients.WhitePeaceScore（下の最初の確認で既定と同じことを見る）

    [Fact]
    public void 白紙の線は既定の0_25_判定は表のとおり()
    {
        Assert.Equal(0.25, new Coefficients().WhitePeaceScore);
        // 対外戦・分離独立（政体の内戦でない）：|戦勝点|＜0.25 は白紙・それ以外は符号の側が勝ち（A 側プラス視点）
        foreach (var (score, want) in new[]
                 { (0.0, WarVerdict.Blank), (0.24, WarVerdict.Blank), (-0.24, WarVerdict.Blank), (0.25, WarVerdict.AWon), (-0.25, WarVerdict.BWon),
                   (0.9, WarVerdict.AWon), (-0.9, WarVerdict.BWon), (1.5, WarVerdict.AWon), (-1.5, WarVerdict.BWon) })   // ±1.5＝厭戦崩壊の上書き込み
            Assert.Equal(want, EndedWarRecord.VerdictOf(score, regimeWar: false, W));
        // 政体の内戦：戦勝点がちょうど 0 のときだけ白紙。小さくても 0 でなければ押しの大きい側が勝つ（講和の枝と同じ）
        foreach (var (score, want) in new[]
                 { (0.0, WarVerdict.Blank), (0.10, WarVerdict.AWon), (-0.10, WarVerdict.BWon), (0.24, WarVerdict.AWon), (0.25, WarVerdict.AWon), (-0.9, WarVerdict.BWon) })
            Assert.Equal(want, EndedWarRecord.VerdictOf(score, regimeWar: true, W));
    }

    [Fact]
    public void 結果の語は自国の側から読み_画面と報告が同じ1本である()
    {
        var aWon = new EndedWarRecord { AId = 7, BId = 9, Score = 0.8, Verdict = WarVerdict.AWon };
        Assert.Equal(true, aWon.WonBy(7)); Assert.Equal(false, aWon.WonBy(9));
        Assert.Equal("勝利", PlayText.WarResultWord(aWon.WonBy(7))); Assert.Equal("敗北", PlayText.WarResultWord(aWon.WonBy(9)));
        var bWon = new EndedWarRecord { AId = 7, BId = 9, Score = -0.8, Verdict = WarVerdict.BWon };
        Assert.Equal("敗北", PlayText.WarResultWord(bWon.WonBy(7))); Assert.Equal("勝利", PlayText.WarResultWord(bWon.WonBy(9)));
        var blank = new EndedWarRecord { AId = 7, BId = 9, Score = 0.1, Verdict = WarVerdict.Blank };
        Assert.Null(blank.WonBy(7)); Assert.Null(blank.WonBy(9));
        Assert.Equal("白紙講和", PlayText.WarResultWord(blank.WonBy(7)));
    }

    [Fact]
    public void 終局の判定は暦年が終局年に入ったときで_世界の暦と表示暦が同じ関数を通る()
    {
        Assert.Equal(1950, Calendar.EndYear);
        Assert.False(PlayText.IsEnded(1949)); Assert.True(PlayText.IsEnded(1950)); Assert.True(PlayText.IsEnded(1951));
        // 画面は経過日から表示暦の年を出して渡す：GameQuarters 経過〔668 四半期＝668×90 日〕の最初の日に終局（1949 年 12 月が終わった次の日）・1 日前はまだ
        int Year(int days) => Calendar.StartYear + days / Calendar.DaysPerYear;
        Assert.False(PlayText.IsEnded(Year(Coefficients.GameQuarters * Calendar.DaysPerQuarter - 1)));
        Assert.True(PlayText.IsEnded(Year(Coefficients.GameQuarters * Calendar.DaysPerQuarter)));
        // 世界の暦の形も同じ関数：w.Year を渡した結果と一致（1783 年の開始はまだ終局でない）
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan()], seed: 1789);
        var w = sim.Nations[0].World;
        Assert.Equal(PlayText.IsEnded(w.Year), PlayText.IsEnded(w));
        Assert.False(PlayText.IsEnded(w));
    }
}
