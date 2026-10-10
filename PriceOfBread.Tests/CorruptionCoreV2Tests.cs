using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>汚職再設計C1（docs/202608/20260814_汚職をサブシステムにする_部分完了.md【一部】.md・作者裁定「それでよい」）の番人。
/// 汚職＝(1−行政)×(1−監視)・監視＝noisy-or(報道, 資格任用, 議会の予算権)・漏れは官職地代（rent）に記帳。
/// (1)既定0の休眠ピン (2)監視のnoisy-or形（1要素だけでも監視が立つ＝8f裁定） (3)ON経路＝官僚のLandRentが立つ、を固定する。</summary>
public class CorruptionCoreV2Tests
{
    [Fact]
    public void 係数の既定は点火済み()
    {
        Assert.Equal(1.0, new Coefficients().CorruptionCoreV2);   // ★点火2026-08-15（ign_corr）・恒久ON裁定。旧式(1−行政)(1−清廉度)はC2-C5撤去（2026-08-16）で物理削除＝0に戻す道は消滅（AntiCorruptionTests もOFF枝番人ごと撤去）。この係数は点火記録＝本テストが番人
    }

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

    [Fact]
    public void ON経路で漏れが官僚の官職地代に記帳される()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 7);
        var w = sim.Nations[0].World;
        // ⚠係数の宛先に注意：sim.Coef は外交/国際市場の**共有**係数・各国の Fiscal/経済は w.Coef（ToScenario 生まれの別インスタンス）を読む。
        //   sim.Coef に書いても Fiscal には届かない（この取り違いで一度赤になった＝診断: 官僚5pop/漏れ875なのに記帳0）。
        var c = w.Coef;
        sim.StartWarmup();
        c.CorruptionCoreV2 = 1.0;
        // 監視を最低・行政不足を強制＝汚職が確実に立つ盤面（法は実データ・値だけ動かす）
        w.Censorship = CensorshipLaw.StatePressOnly; w.Bureaucracy = BureaucracyLaw.Spoils; w.ParliamentControl = 0;
        sim.StepQuarter();
        Assert.True(w.LastTaxLeak > 1e-9, "汚職漏れが0＝テスト前提の破れ（行政・監視の盤面を確認）");
        double bureaucratRent = 0, bureaucratSize = 0; int bureaucratCount = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) { bureaucratRent += p.LandRentPerMonth; bureaucratSize += p.Size; bureaucratCount++; }
        Assert.True(bureaucratRent > 1e-9, $"官僚のLandRent=0＝8bの記帳が届いていない（診断: 官僚pop数{bureaucratCount}・規模計{bureaucratSize:F1}・leak{w.LastTaxLeak:F3}・実効汚職{w.EffectiveCorruption:F3}）");
    }
}
