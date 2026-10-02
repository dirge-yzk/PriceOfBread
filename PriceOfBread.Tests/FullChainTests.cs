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
        Assert.Equal(120, sim.World.Regions[0].Goods.Count);   // ★2026-09-30 (129)：119→120＝金鶏樹皮を戻した（作者裁定 2026-09-30）   // ★2026-09-30 段1（財の削減）：130→119＝11財を畳んだ（金鶏樹皮・茶葉・コーヒー豆・コークス・パルプ・機械部品・工具・発電機・包帯・精密工作機械・戦闘機）   // ★2026-09-04：129→130＝冷蔵庫（作者提案の冷蔵＝消費財かつ生肉の遠距離輸送を開く財）     // ＋1934拡張E2/E3の6財（飛行機・戦闘機・自動車・化学兵器・新聞・ラジオ＝2026-07-19）。上下水道/電化インフラはService＝国家世界のみ
        // ★2026-09-06：126→127＝E-13 再開で黄鉄鉱法（化学薬品の第2レシピ）を点火＝フルチェーンにも1件。代替レシピの注入は AddAlternateRecipes（構築経路）へ移した（同日）。
        Assert.Equal(120, sim.World.Regions[0].Producers.Count);   // ★2026-10-02 (169)：119→120＝1780年代の蒸気機関「鋳鉄の機関」（蒸気機関の財の第2生産者・AddAlternateRecipes）   // ★2026-09-30 (129)：118→119＝キニーネが本国の工場へ戻った（樹皮は植民地の財＝フルチェーンに生産者は無い・早期の「樹皮の粉末」は国家世界のビルダー側）   // ★2026-09-30 段1（財の削減）：129→118＝畳んだ11財の既定の生産者   // ★2026-09-27：127→129＝前期の軍需レシピ（マスケット・鋳鉄砲）を回2-1 で旗ごと点火＝小銃・火砲の第2生産者   // ★2026-09-04：125→126＝冷蔵の加工肉レシピ（ChilledMeatEnabled 点火＝加工肉の第2生産者が全州に立つ）   // ★2026-09-04：124→125＝冷蔵庫の生産者（財の追加に伴う。⚠冷蔵の加工肉は ChilledMeatEnabled=0 で休眠＝この数には入らない）   // ★2026-08-14：123→124＝石油ケロシン（灯油の第2レシピ・KeroseneFromOilEnabled=1点火＝原油の40年の買い手ゼロの穴埋め） // 同6財の生産者＋水力発電（A-中1・2026-07-23＝電力の第3レシピ）＋坩堝鋼/錬鉄レール（CrucibleSteelEnabled=1・第7段第1波2026-07-28）。並存レシピ（人絹/鉄筋コンクリート/合成ゴム）は国家世界のビルダー側

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
        Assert.True(OutputOf(r, Catalog.BreadId) > 0, "パンが生産されていない（麦→小麦粉→パンの連鎖が機能していない）");
        Assert.True(OutputOf(r, Catalog.ClothingId) > 0, "衣料が生産されていない（綿花→綿布→衣料の連鎖が機能していない）");
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
    public void 蒸気動力は蒸気機関の発見の後に石炭需要を工業へ連動させ保存則を保つ()   // 蒸気＝電化と同型の据付動力：未発見の国は石炭を焚かない（1789年の時代錯誤を解消）、発見の後は石炭需要が工業基盤に連動
    {
        // 蒸気機関が未発見（★2026-10-01 (150)：工場の蒸気動力の門＝発見の表の「蒸気機関」の行・旧＝技術水準の時計 SteamTechOnset）：機械化工場は石炭を「動力」として焚かない＝水車/畜力
        //   （単独の世界も四半期末に判定する＝製法の必要性の半分点を無限にして開かない盤面にする）
        var lockedSim = new Simulation(Full(1), new NullAgent());
        lockedSim.World.Coef.EnergyPerOutput = 0.2;
        lockedSim.World.Discovered[Discovery.SteamRow] = false;   // 蒸気機関が未発見
        lockedSim.World.Coef.DiscoveryNeedHalfProcess = double.PositiveInfinity;   // 単独の世界の四半期の判定でも開かない（製法の必要性0＝技術力だけでは門に届かない）
        lockedSim.Run(200);
        double lockedCoalDemand = lockedSim.World.Regions[0].Markets[Catalog.CoalId].LastDemand;

        // 蒸気機関を発見済み（技術水準に依らない＝NullAgentで技術が伸びなくても開く）：機械化工場が産出比例で動力(石炭)を焚く＝石炭需要が工業基盤に連動
        var steamSim = new Simulation(Full(1), new NullAgent());
        steamSim.World.Coef.EnergyPerOutput = 0.2;
        steamSim.World.Discovered[Discovery.SteamRow] = true;   // 蒸気機関を発見済み
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
        Assert.Equal(a.World.Regions[0].Markets[Catalog.IronId].Price, b.World.Regions[0].Markets[Catalog.IronId].Price);
        Assert.Equal(a.World.TotalMoney, b.World.TotalMoney);
    }

    // ★2026-09-30 段1（財の削減＝設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §3・作者裁定 2026-09-28／09-29）の番人。
    [Fact]
    public void 畳んだ財はカタログに無く畳み先が引き継ぐ()
    {
        string[] folded = ["茶葉", "コーヒー豆", "コークス", "パルプ", "機械部品", "工具", "発電機", "包帯", "精密工作機械", "飛行機", "戦闘機"];   // ★2026-09-30 (129)：金鶏樹皮は戻した（下の番人）
        foreach (var n in folded) Assert.DoesNotContain(Catalog.Goods, g => g.Name == n);
        for (int i = 0; i < Catalog.Goods.Length; i++) Assert.Equal(i, Catalog.Goods[i].Id);   // 墓石でなく詰めた（作者裁定 Q7）＝id は 0..n−1
        foreach (int g in new[] { Catalog.TeaId, Catalog.CoffeeId })       // 採取の財へ畳んだ2財＝投入なし・植民地の気候ゲート（★2026-09-30 (129)：キニーネは本国の工場へ戻した）
        {
            Assert.Empty(Catalog.Goods[g].Inputs);
            Assert.True(Catalog.IsColonial(g) && Catalog.Goods[g].IsColonial, $"{Catalog.Goods[g].Name} が植民地の採取になっていない");
        }
        Assert.Equal("軍用機", Catalog.Goods[Catalog.MilitaryAircraftId].Name);
        // 残した財（作者裁定 2026-09-29「加工は畳まない」・Q4 蒸気機関・型の違う送電網ほか＝実装ログ (123)）
        foreach (var n in new[] { "サトウキビ", "葉煙草", "生ゴム", "金鶏樹皮", "羊毛", "銅鉱", "鉛鉱", "小麦粉", "飼料", "送電網", "蒸気機関" })
            Assert.Contains(Catalog.Goods, g => g.Name == n);
    }

    /// <summary>振り直しの番人（作者裁定 Q7）：全ての財の定数が goods.json の同じ名前の財を指す＝定数の値の書き違い・振り直しの漏れを捕まえる。</summary>
    [Fact]
    public void 財の定数はカタログの同じ名前の財を指す()
    {
        (int Id, string Name)[] pins =
        [
            (Catalog.IronOreId, "鉄鉱石"),
            (Catalog.CoalId, "石炭"),
            (Catalog.CopperOreId, "銅鉱"),
            (Catalog.LeadOreId, "鉛鉱"),
            (Catalog.GoldOreId, "金"),
            (Catalog.SulfurId, "硫黄"),
            (Catalog.SaltId, "塩"),
            (Catalog.LimestoneId, "石灰石"),
            (Catalog.SilicaSandId, "珪砂"),
            (Catalog.ClayId, "粘土"),
            (Catalog.CrudeOilId, "原油"),
            (Catalog.TimberId, "木"),
            (Catalog.CottonId, "綿花"),
            (Catalog.HempId, "麻"),
            (Catalog.SilkId, "生糸"),
            (Catalog.WheatId, "麦"),
            (Catalog.FruitId, "果実"),
            (Catalog.DyeId, "染料"),
            (Catalog.TobaccoLeafId, "葉煙草"),
            (Catalog.LivestockId, "畜産"),
            (Catalog.FishId, "漁獲"),
            (Catalog.RawRubberId, "生ゴム"),
            (Catalog.PalmOilId, "パーム油"),
            (Catalog.IvoryId, "象牙"),
            (Catalog.SugarCaneId, "サトウキビ"),
            (Catalog.CocoaId, "カカオ"),
            (Catalog.SpiceId, "香辛料"),
            (Catalog.IronId, "鉄"),
            (Catalog.SteelId, "鋼"),
            (Catalog.CopperId, "銅"),
            (Catalog.LeadId, "鉛"),
            (Catalog.CementId, "セメント"),
            (Catalog.GlassId, "ガラス"),
            (Catalog.ChemicalsId, "化学薬品"),
            (Catalog.GunpowderId, "火薬"),
            (Catalog.LumberId, "製材"),
            (Catalog.CottonClothId, "綿布"),
            (Catalog.CanvasId, "帆布"),
            (Catalog.WoolId, "羊毛"),
            (Catalog.WoolenClothId, "毛織物"),
            (Catalog.SilkClothId, "絹織物"),
            (Catalog.LeatherId, "革"),
            (Catalog.FlourId, "小麦粉"),
            (Catalog.MeatId, "肉"),
            (Catalog.SugarId, "砂糖"),
            (Catalog.RubberId, "ゴム"),
            (Catalog.MachineryId, "機械"),
            (Catalog.SteamEngineId, "蒸気機関"),
            (Catalog.RailId, "線路"),
            (Catalog.ClothingId, "衣料"),
            (Catalog.LuxuryClothingId, "高級衣料"),
            (Catalog.FurnitureId, "家具"),
            (Catalog.LuxuryFurnitureId, "高級家具"),
            (Catalog.PaperId, "紙"),
            (Catalog.BreadId, "パン"),
            (Catalog.PreservedMeatId, "加工肉"),
            (Catalog.DriedFishId, "干物"),
            (Catalog.CannedFoodId, "缶詰"),
            (Catalog.BeerId, "ビール"),
            (Catalog.WineId, "ワイン"),
            (Catalog.LampOilId, "灯油"),
            (Catalog.TobaccoId, "煙草"),
            (Catalog.LeatherGoodsId, "革製品"),
            (Catalog.PorcelainId, "陶磁器"),
            (Catalog.SoapId, "石鹸"),
            (Catalog.ChocolateId, "チョコレート"),
            (Catalog.TeaId, "茶"),
            (Catalog.CoffeeId, "コーヒー"),
            (Catalog.WaterproofClothId, "防水布"),
            (Catalog.MedicineId, "医薬品"),
            (Catalog.QuinineId, "キニーネ"),
            (Catalog.ApplianceId, "電化製品"),
            (Catalog.ArtilleryId, "火砲"),
            (Catalog.RifleId, "小銃"),
            (Catalog.AmmunitionId, "弾薬"),
            (Catalog.SailingShipId, "帆船"),
            (Catalog.SteamshipId, "汽船"),
            (Catalog.WarshipId, "軍艦"),
            (Catalog.ElectricityId, "電力"),
            (Catalog.LocomotiveId, "機関車"),
            (Catalog.JewelryId, "宝飾"),
            (Catalog.WatchId, "時計"),
            (Catalog.InstrumentId, "楽器"),
            (Catalog.OpiumId, "アヘン"),
            (Catalog.LaborId, "労働力"),
            (Catalog.EducationId, "教育"),
            (Catalog.HealthcareId, "医療"),
            (Catalog.AdminId, "行政"),
            (Catalog.MilitaryPowerId, "兵力"),
            (Catalog.ChromeId, "クロム"),
            (Catalog.GridId, "送電網"),
            (Catalog.AdvLaborId, "高度労働力"),
            (Catalog.FarmLaborId, "農業労働力"),
            (Catalog.RiceId, "米"),
            (Catalog.SpiritsId, "蒸留酒"),
            (Catalog.MachineGunId, "機関銃"),
            (Catalog.IroncladId, "装甲艦"),
            (Catalog.CheeseId, "チーズ"),
            (Catalog.VegetableId, "野菜"),
            (Catalog.FertilizerId, "肥料"),
            (Catalog.FodderId, "飼料"),
            (Catalog.InfrastructureId, "上下水道"),
            (Catalog.BookId, "書籍"),
            (Catalog.GasId, "ガス"),
            (Catalog.ElecInfraId, "電化インフラ"),
            (Catalog.PotatoId, "ジャガイモ"),
            (Catalog.MaizeId, "トウモロコシ"),
            (Catalog.TinOreId, "錫鉱"),
            (Catalog.SaltpeterId, "硝石"),
            (Catalog.FurId, "毛皮"),
            (Catalog.SilverId, "銀"),
            (Catalog.GuanoId, "グアノ"),
            (Catalog.TankId, "戦車"),
            (Catalog.DreadnoughtId, "弩級戦艦"),
            (Catalog.DomesticServiceId, "家事奉仕"),
            (Catalog.CombustionEngineId, "内燃機関"),
            (Catalog.HousingId, "住居"),
            (Catalog.HorseId, "馬"),
            (Catalog.DynamiteId, "ダイナマイト"),
            (Catalog.BicycleId, "自転車"),
            (Catalog.MilitaryAircraftId, "軍用機"),
            (Catalog.AutomobileId, "自動車"),
            (Catalog.ChemicalWeaponId, "化学兵器"),
            (Catalog.NewspaperId, "新聞"),
            (Catalog.RadioId, "ラジオ"),
            (Catalog.HigherEducationId, "高等教育"),
            (Catalog.RefrigeratorId, "冷蔵庫"),
            (Catalog.CinchonaBarkId, "金鶏樹皮"),   // ★2026-09-30 (129)：戻した財は末尾（既存の番号は変えない）
        ];
        Assert.Equal(Catalog.Goods.Length, pins.Length);
        foreach (var (id, n) in pins) Assert.Equal(n, Catalog.Goods[id].Name);
    }

    /// <summary>採取の財へ畳んだ2財の賦存は、畳む前の原料の賦存と同じ数（設計 §7 組2 の番人）。★2026-09-30 (129)：戻した金鶏樹皮は畳む前と同じ5か所。</summary>
    [Fact]
    public void 畳んだ採取の財の賦存は畳む前と同じ数()
    {
        static int Count(int gid)
        {
            int n = 0;
            foreach (var c in WorldData.Nations())
            {
                foreach (var p in c.Provinces) if (System.Array.IndexOf(p.Resources, gid) >= 0) n++;
                foreach (var col in c.InitialColonies) if (System.Array.IndexOf(col.Resources, gid) >= 0) n++;
            }
            return n;
        }
        Assert.Equal(8, Count(Catalog.TeaId));       // 旧 茶葉＝州5・初期の植民地3
        Assert.Equal(10, Count(Catalog.CoffeeId));   // 旧 コーヒー豆＝州2・初期の植民地8
        Assert.Equal(5, Count(Catalog.CinchonaBarkId));   // 金鶏樹皮＝初期の植民地5（★2026-09-30 (129)：(123) でキニーネの採取へ移した5か所を樹皮へ戻した）
        Assert.Equal(0, Count(Catalog.QuinineId));        // キニーネは採らない（本国の工場で樹皮から作る）
    }

    /// <summary>畳んだ中間財の下流は参照価格を保つ（コスト中立の換算＝作者方針 2026-08-01「既存の較正済み量に絡める」）。
    /// 値＝畳む前（2026-09-30 の goods.json）の参照価格。±0.5%（量の丸め3桁）。</summary>
    [Fact]
    public void 畳んだ中間財の下流は参照価格を保つ()
    {
        (int Id, double Rp)[] pins =
        [
            (Catalog.IronId, 1.4700), (Catalog.SteelId, 2.1760), (Catalog.MachineryId, 4.3948), (Catalog.SteamEngineId, 3.7950),
            (Catalog.PaperId, 1.2000), (Catalog.BreadId, 1.1920), (Catalog.PreservedMeatId, 0.9758), (Catalog.ApplianceId, 3.1444),
            (Catalog.ArtilleryId, 4.0965), (Catalog.ElectricityId, 1.5721), (Catalog.WatchId, 1.8176), (Catalog.GridId, 2.4499),
            (Catalog.MachineGunId, 4.3110), (Catalog.DreadnoughtId, 12.7277), (Catalog.CombustionEngineId, 5.0348), (Catalog.HorseId, 0.9100),
            (Catalog.BicycleId, 2.6629), (Catalog.NewspaperId, 1.2055), (Catalog.RadioId, 2.8504), (Catalog.RefrigeratorId, 2.5627),
        ];
        foreach (var (id, rp) in pins)
            Assert.True(System.Math.Abs(Catalog.RefPrices[id] / rp - 1.0) <= 0.005, $"{Catalog.Goods[id].Name}：参照価格 {Catalog.RefPrices[id]:F4}（畳む前 {rp:F4}）＝コスト中立が崩れた");
    }

    // ★2026-09-30 (129) 金鶏樹皮を戻す（作者裁定 2026-09-30＝財の削減 (123) で畳んだのを戻す・元の裁定 2026-09-29
    //   「加工は畳まない。植民地材→本国に輸送→加工の行程を飛ばすべきではない」）の番人。
    /// <summary>樹皮は植民地で採り（投入なし・気候ゲート）、キニーネは本国の工場で樹皮と化学薬品から作る（単離＝解禁の表の既定の行）。</summary>
    [Fact]
    public void 金鶏樹皮は植民地で採りキニーネは本国の工場で作る()
    {
        var bark = Catalog.Goods[Catalog.CinchonaBarkId];
        Assert.Empty(bark.Inputs);
        Assert.True(Catalog.IsColonial(Catalog.CinchonaBarkId) && bark.IsColonial, "金鶏樹皮が植民地の採取になっていない");
        Assert.Equal(-1, Catalog.DiscoveryRowOf(Catalog.CinchonaBarkId, ""));   // 樹皮を剥ぐのに発見は要らない（17世紀から）＝発見の表に行が無い
        var q = Catalog.Goods[Catalog.QuinineId];
        Assert.False(Catalog.IsColonial(Catalog.QuinineId) || q.IsColonial, "キニーネが植民地の採取に残っている（工程を飛ばす）");
        Assert.Contains(q.Inputs, i => i.GoodId == Catalog.CinchonaBarkId && i.QtyPerOutput > 0);
        Assert.Contains(q.Inputs, i => i.GoodId == Catalog.ChemicalsId && i.QtyPerOutput > 0);   // 単離＝酸とアルコールで抽出する化学の工程
        int iso = Catalog.DiscoveryRowOf(Catalog.QuinineId, "");   // 単離（ペルティエとカヴァントゥ 1820）＝(123) の R8 を工場のレシピへ戻した＝発見の表の既定の行（2026-10-01 段2A）
        Assert.True(iso >= 0 && Catalog.Discoveries[iso].Decade == 1820 && Catalog.Discoveries[iso].Field == DiscoveryField.Process, "キニーネの単離が発見の表の1820年代の製法でない");
    }

    /// <summary>単離の前は「樹皮の粉末」が橋になる（早期の橋＝解禁の門なし・化学薬品を使わない・同じ防護に単離より多くの樹皮）。
    /// 単離の後は参照価格で粉末が単離に負ける（史実＝単離の後20年ほどで硫酸キニーネが薬局から粉末を駆逐した）。</summary>
    [Fact]
    public void 単離の前は樹皮の粉末が橋になり単離の後は参照価格で負ける()
    {
        var sim = new MultiNationSim([NationConfig.Britain()], seed: 1);
        var r = sim.Nations[0].World.Regions[0];
        Producer? iso = null, powder = null;
        foreach (var p in r.Producers)
        {
            if (p.GoodId != Catalog.QuinineId) continue;
            if (p.RecipeName == "樹皮の粉末") powder = p; else if (string.IsNullOrEmpty(p.RecipeName)) iso = p;
        }
        Assert.NotNull(iso);
        Assert.NotNull(powder);
        Assert.Equal(Catalog.DiscoveryRowOf(Catalog.QuinineId, ""), iso!.DiscoveryRow);   // 単離は発見の門の後（2026-10-01 段2A＝発見の表の行）
        Assert.True(iso.DiscoveryRow >= 0, "単離に発見の門が無い");
        Assert.True(powder!.DiscoveryRow < 0, "樹皮の粉末に発見の門がある（単離の前の橋にならない）");
        Assert.DoesNotContain(powder.Inputs, i => i.GoodId == Catalog.ChemicalsId);
        double BarkOf(Producer p) { double b = 0; foreach (var i in p.Inputs) if (i.GoodId == Catalog.CinchonaBarkId) b += i.QtyPerOutput; return b; }
        Assert.True(BarkOf(powder) > BarkOf(iso), $"粉末の樹皮 {BarkOf(powder)} が単離 {BarkOf(iso)} 以下＝効き目の低さが無い");
        double unit = Catalog.FactoryVa;   // 参照価格の式（ComputeRefPrices）と同じ単位費用
        foreach (var i in powder.Inputs) unit += i.QtyPerOutput * Catalog.RefPrices[i.GoodId];
        Assert.True(unit > Catalog.RefPrices[Catalog.QuinineId], $"樹皮の粉末の単位費用 {unit:F3} が単離の参照価格 {Catalog.RefPrices[Catalog.QuinineId]:F3} 以下＝単離の後も粉末が勝つ");
    }
}
