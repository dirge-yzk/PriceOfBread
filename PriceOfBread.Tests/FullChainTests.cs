using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class FullChainTests
{
    private static Scenario Full(ulong s) => new() { Seed = s, Kind = ScenarioKind.FullChain };

    private static double OutputOf(Region r, int goodId)
    {
        double o = 0;
        foreach (var p in r.Producers) if (p.GoodId == goodId) o += p.Output;
        return o;
    }

    [Fact]
    public void 全50財がロードされ放置で破綻せず基礎貨幣を保存する()
    {
        var sim = new Simulation(Full(1), new NullAgent());
        Assert.Equal(130, sim.World.Regions[0].Goods.Count);   // ★2026-09-04：129→130＝冷蔵庫（作者提案の冷蔵＝消費財かつ生肉の遠距離輸送を開く財）     // ＋1934拡張E2/E3の6財（飛行機・戦闘機・自動車・化学兵器・新聞・ラジオ＝2026-07-19）。上下水道/電化インフラはService＝国家世界のみ
        // ★2026-09-06：126→127＝E-13 再開で黄鉄鉱法（化学薬品の第2レシピ）を点火＝フルチェーンにも1件。代替レシピの注入は AddAlternateRecipes（構築経路）へ移した（同日）。
        Assert.Equal(129, sim.World.Regions[0].Producers.Count);   // ★2026-09-27：127→129＝前期の軍需レシピ（マスケット・鋳鉄砲）を回2-1 で旗ごと点火＝小銃・火砲の第2生産者   // ★2026-09-04：125→126＝冷蔵の加工肉レシピ（ChilledMeatEnabled 点火＝加工肉の第2生産者が全州に立つ）   // ★2026-09-04：124→125＝冷蔵庫の生産者（財の追加に伴う。⚠冷蔵の加工肉は ChilledMeatEnabled=0 で休眠＝この数には入らない）   // ★2026-08-14：123→124＝石油ケロシン（灯油の第2レシピ・KeroseneFromOilEnabled=1点火＝原油の40年の買い手ゼロの穴埋め） // 同6財の生産者＋水力発電（A-中1・2026-07-23＝電力の第3レシピ）＋坩堝鋼/錬鉄レール（CrucibleSteelEnabled=1・第7段第1波2026-07-28）。並存レシピ（人絹/鉄筋コンクリート/合成ゴム）は国家世界のビルダー側

        double base0 = sim.World.BaseMoney;
        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);   // §11.6（保存・境界）が全ティックで保持
        // 基礎貨幣の変化＝累積貿易流入（植民地輸入の対外流出を含む）
        Assert.True(Math.Abs(sim.World.BaseMoney - base0 - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued) < 1e-3 * base0, "基礎貨幣保存の破れ");
    }

    [Fact]
    public void 基本消費財の連鎖は機能する()   // パン（麦→小麦粉→パン）と衣料（綿花→綿布→衣料）は生産される
    {
        var sim = new Simulation(Full(1), new NullAgent());
        sim.Run(400);
        var r = sim.World.Regions[0];
        Assert.True(OutputOf(r, 62) > 0, "パンが生産されていない（麦→小麦粉→パンの連鎖が機能していない）");
        Assert.True(OutputOf(r, 57) > 0, "衣料が生産されていない（綿花→綿布→衣料の連鎖が機能していない）");
    }

    [Fact]
    public void 鋳造で基礎貨幣が増えても保存則は保たれる()   // #10 金鉱→貨幣供給：鋳造は CumulativeGoldMinted を介した正当な基礎貨幣の増加源
    {
        var sim = new Simulation(Full(1), new NullAgent());
        // ★2026-08-29 造幣3本点火後は**旧・産出比例経路の番人**として係数を明示0にピン（新既定＝床/本位金属/準備目標では
        //   この玩具世界で「平価未満」が成立せず鋳造が発生しない＝minted>0 の前提が空振りした）。
        //   新既定の保存則は本ファイル先頭テスト（全50財…）の恒等式が番人・床モードの発火はR20バッチ（破綻0）で実証済み。
        sim.World.Coef.MintPriceFloorGate = 0;
        sim.World.Coef.StandardMetalMintGate = 0;
        sim.World.Coef.MintReserveTargetWeight = 0;
        sim.World.Coef.GoldMintFraction = 0.5;   // 金産出の半分を鋳造（貨幣化）
        double base0 = sim.World.BaseMoney;
        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);   // §11.6（保存・境界）が鋳造を含めて全ティックで保持（GoodsBalance/MoneyConservation/NonNegative）
        // 基礎貨幣の変化＝累積貿易流入＋累積鋳造（鋳造が新たな増加源）
        Assert.True(Math.Abs(sim.World.BaseMoney - base0 - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted) < 1e-3 * Math.Max(1.0, base0), "鋳造込みの基礎貨幣保存の破れ");
        Assert.True(sim.World.CumulativeGoldMinted > 0, "鋳造が一度も発生していない（金鉱の産出/在庫が無い？）");
    }

    [Fact]
    public void 蒸気動力は技術解禁後に石炭需要を工業へ連動させ保存則を保つ()   // 蒸気＝電化と同型の据付動力：未発明期は石炭を焚かない（1789年の時代錯誤を解消）、解禁後は石炭需要が工業基盤に連動
    {
        // 蒸気未解禁（onset を事実上無限大に）：機械化工場は石炭を「動力」として焚かない＝水車/畜力＝1789年の蒸気の時代錯誤がない
        var lockedSim = new Simulation(Full(1), new NullAgent());
        lockedSim.World.Coef.EnergyPerOutput = 0.2;
        lockedSim.World.Coef.SteamTechOnset = 99.0;   // 蒸気機関が永久に未発明
        lockedSim.Run(200);
        double lockedCoalDemand = lockedSim.World.Regions[0].Markets[Catalog.CoalId].LastDemand;

        // 蒸気解禁（onset を負値にしてゲートを強制1＝NullAgentで技術が伸びなくても発明済みとして検証）：機械化工場が産出比例で動力(石炭)を焚く＝石炭需要が工業基盤に連動
        var steamSim = new Simulation(Full(1), new NullAgent());
        steamSim.World.Coef.EnergyPerOutput = 0.2;
        steamSim.World.Coef.SteamTechOnset = -1.0;   // steamGate=clamp((tech+1)/width)=1（技術0でも解禁）＝蒸気機関が発明済み
        double base0 = steamSim.World.BaseMoney;
        var ex = Record.Exception(() => steamSim.Run(200));
        Assert.Null(ex);   // §11.6（保存・境界）が全ティックで保持
        Assert.True(Math.Abs(steamSim.World.BaseMoney - base0 - steamSim.World.CumulativeTradeFlow - steamSim.World.CumulativeGoldMinted) < 1e-3 * Math.Max(1.0, base0), "蒸気動力込みの基礎貨幣保存の破れ");
        // 蒸気解禁の方が石炭の総需要が高い（動力としての石炭消費が工業に連動＝解禁前は焚かない）
        double steamCoalDemand = steamSim.World.Regions[0].Markets[Catalog.CoalId].LastDemand;
        Assert.True(steamCoalDemand > lockedCoalDemand, $"蒸気解禁で石炭の総需要が増えていない（locked={lockedCoalDemand:F1} steam={steamCoalDemand:F1}）");
    }

    [Fact]
    public void 同一シードで全50財チェーンは決定論的()
    {
        var a = new Simulation(Full(2), new NullAgent());
        var b = new Simulation(Full(2), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Regions[0].Markets[31].Price, b.World.Regions[0].Markets[31].Price);
        Assert.Equal(a.World.TotalMoney, b.World.TotalMoney);
    }
}
