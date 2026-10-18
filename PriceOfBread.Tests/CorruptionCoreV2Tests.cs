using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>汚職再設計C1（docs/202608/20260814_汚職をサブシステムにする_部分完了.md【一部】.md・作者裁定「それでよい」）の番人。
/// 汚職＝(1−行政)×(1−監視)・監視＝noisy-or(報道, 資格任用, 議会の予算権)。
/// (2)監視のnoisy-or形（1要素だけでも監視が立つ＝8f裁定）を固定する。
/// （(1)係数 CorruptionCoreV2 の既定の確認は 2026-10-11 (253) に係数ごと削除。(3)ON経路＝漏れが官僚のLandRentに記帳される試験は、汚職の財政の漏れの物理削除〔(399)〕で消した。）</summary>
public class CorruptionCoreV2Tests
{
    [Fact]
    public void 監視はnoisy_orで1要素だけでも立つ()
    {
        var sim = new MultiNationSim([WorldData.ByName("スイス")], seed: 11);
        var w = sim.Nations[0].World;
        // 全要素0（検閲どころか国営紙のみ・縁故任用・予算権なし）→監視0
        w.Censorship = CensorshipLaw.StatePressOnly; w.Bureaucracy = BureaucracyLaw.Spoils; w.ParliamentControl = 0;
        Assert.Equal(0.0, w.OversightV2, 12);
        // 報道の自由だけ→0.4や0でなく1.0側の寄与＝積(全滅)でないことの証明（マックレーカーズ＝報道だけでも監視できる）
        w.Censorship = CensorshipLaw.FreePress;
        Assert.Equal(1.0, w.OversightV2, 12);
        // 検閲(0.4)×資格任用(0.6)＝noisy-or: 1−0.6×0.4＝0.76（積なら0.24＝逆）
        w.Censorship = CensorshipLaw.Censorship; w.Bureaucracy = BureaucracyLaw.MeritExam;
        Assert.Equal(1.0 - (1.0 - 0.4) * (1.0 - 0.6), w.OversightV2, 12);
    }
}
