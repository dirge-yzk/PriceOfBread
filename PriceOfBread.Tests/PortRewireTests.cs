using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>港の配線を領土に追随させる（棚卸A5・W2・doc 20260812_港の配線を領土に追随させる／20260816_港配線の簡素化案・案1）のON経路番人。
/// PortRewireEnabled=1.0点火済み（2026-08-16）。⚠2026-09-21＝**署名の層を削除**＝毎四半期・全国を無条件で張り直す（状態ゼロ）。
/// ここでは (1)既定＝点火済みで、**盤面が1ミリも動かない四半期にも張り直る**（条件付き再配線の復活を赤で捕まえる）
/// (2)割譲で港を失った国の NearestPortName が更新され通過港へ落ちる（S1是正＋是正3の同時性）
/// (3)OFF側＝0では最寄り港を張り直さない（症状S1の固定＝対照はOFF側も明示・空洞化予防）を固定する。</summary>
public class PortRewireTests
{
    [Fact]
    public void 既定は点火済みで盤面が動かない四半期にも張り直す()
    {
        Assert.Equal(1.0, new Coefficients().PortRewireEnabled);   // W3点火済み（2026-08-16＝単独34/70/53・条約港風化との合成36/73/51=優加法）
        // ★2026-09-21（案1）：領土署名で「動いた国だけ」を選ぶ層を消した＝**盤面が動かない四半期でも**全国を張り直す。
        //   手で配線を壊して1四半期回すと元に戻る＝条件付き再配線に戻したらここが赤になる（番人の空洞化予防）。
        var sim = new MultiNationSim(World2(), seed: 17) { DiplomacyFrozen = true };
        var florence = sim.Nations[0].World.Regions.First(r => r.Name == "フィレンツェ");
        sim.Run(1);
        Assert.Equal("リヴォルノ", florence.NearestPortName);
        florence.NearestPortName = "架空港";   // 盤面（州集合・港ビット）は不変＝署名は動かない
        sim.Run(1);
        Assert.Equal("リヴォルノ", florence.NearestPortName);
        Assert.True(florence.NearestPortDistanceKm > 0, "最寄り港距離が張り直されていない");
    }

    // トスカーナ（フィレンツェ=内陸・リヴォルノ=港）＋ジェノヴァ（港）。リヴォルノをジェノヴァへ割譲すると
    // トスカーナは港州0＝S1（敵に渡した港からそのまま輸出）と doc §1-4（落ちる先が無い）の最小再現になる。
    private static NationConfig[] World2() => [WorldData.ByName("トスカーナ"), WorldData.ByName("ジェノヴァ")];

    // Diplomacy.CedeTerritory と同じ物理移転＋保存記帳の最小再現（本体は private・投射圏や講和の前提条件を剥がした裸の州移転）。RNG不使用＝決定論。
    private static void CedeLivornoToGenoa(MultiNationSim sim)
    {
        var tw = sim.Nations[0].World; var gw = sim.Nations[1].World;
        var livorno = tw.Regions.First(r => r.Name == "リヴォルノ");
        double S = livorno.SumSavings(), P = livorno.SumPopulation();
        tw.Regions.Remove(livorno);
        int newId = 1; foreach (var r in gw.Regions) if (r.Id >= newId) newId = r.Id + 1;
        livorno.Id = newId;
        gw.Regions.Add(livorno);
        foreach (var m in livorno.Markets) { m.PrevStock = m.Stock; m.LastProduced = m.LastSold = m.LastImport = m.LastExport = m.LastShortage = 0; m.PopBought = null; }
        livorno.ColonialResources = null;
        tw.CumulativeTradeFlow -= S; tw.PopulationDelta -= P;
        gw.CumulativeTradeFlow += S; gw.PopulationDelta += P;
    }

    [Fact]
    public void 点火時_割譲で港を失った国の最寄り港配線が更新され通過港へ落ちる()
    {
        var sim = new MultiNationSim(World2(), seed: 17) { DiplomacyFrozen = true };
        sim.Coef.PortRewireEnabled = 1.0;
        var florence = sim.Nations[0].World.Regions.First(r => r.Name == "フィレンツェ");
        Assert.Equal("リヴォルノ", florence.NearestPortName);   // 構築時＝自国港
        sim.Run(1);   // 四半期末の張り直し（構築時と同値＝冪等）
        Assert.Equal("リヴォルノ", florence.NearestPortName);
        Assert.Equal("", florence.TransitPortName);              // 港のある国に通過港候補は書かれない
        CedeLivornoToGenoa(sim);
        sim.Run(1);   // 四半期末に全国を張り直す＝州移転が配線へ反映される
        // S1の是正：失った港を指し続けない（港州0＝自国港の配線は空へ）
        Assert.Equal("", florence.NearestPortName);
        Assert.Equal(0.0, florence.NearestPortDistanceKm);
        // 是正3の同時性（doc §1-4）：落ちる先＝通過港が同じ四半期に配線される（hasPort現在判定。最寄り外国港＝旧自国港リヴォルノ）
        Assert.Equal("リヴォルノ", florence.TransitPortName);
        Assert.True(florence.TransitLandKm > 0, "通過港の陸路距離が立っていない");
    }

    [Fact]
    public void 休眠では割譲後も配線が動かない()
    {
        var sim = new MultiNationSim(World2(), seed: 17) { DiplomacyFrozen = true };
        sim.Coef.PortRewireEnabled = 0.0;   // OFF側を明示ピン（点火2026-08-16後は既定1.0＝対照は明示する。対照空洞化の予防と同型）
        var florence = sim.Nations[0].World.Regions.First(r => r.Name == "フィレンツェ");
        sim.Run(1);
        CedeLivornoToGenoa(sim);
        sim.Run(1);
        Assert.Equal("リヴォルノ", florence.NearestPortName);   // ⚠症状S1そのもの（敵に渡した港からそのまま輸出）＝旧挙動をビットで固定
        // ★2026-09-21：0 は「最寄り港を張り直さない」＝手で壊した配線も直らない（門が本当に門であることの対照）。
        florence.NearestPortName = "架空港";
        sim.Run(1);
        Assert.Equal("架空港", florence.NearestPortName);
    }
}
