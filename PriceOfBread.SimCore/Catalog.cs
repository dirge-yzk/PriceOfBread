namespace PriceOfBread.SimCore;

public readonly record struct GoodDef(
    int Id, string Name, GoodTier Tier, double BaseNeed, double Elasticity, double Productivity, Input[] Inputs, bool IsColonial = false, bool NonTradeable = false, bool GrainChain = false,
    double Nutrition = -1,
    double WearRatePerMonth = double.NaN,
    GoodUse Use = GoodUse.Consumed);

public enum GoodUse { Consumed, Held }

public static class Catalog
{
    private static Input I(int g, double q) => new(g, q);

    private static GoodDef[] LoadGoods()
    {
        var asm = typeof(Catalog).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("goods.json", System.StringComparison.Ordinal));
        if (name is null) return BuiltinGoods;
        using var s = asm.GetManifestResourceStream(name)!;
        return System.Text.Json.JsonSerializer.Deserialize<GoodDef[]>(s, WorldData.Options) ?? BuiltinGoods;
    }

    public static string Serialize() => System.Text.Json.JsonSerializer.Serialize(BuiltinGoods, WorldData.Options);

    private static readonly GoodDef[] BuiltinGoods =
    [
        new(IronOreId, "鉄鉱石",   GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(CoalId, "石炭",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(CopperOreId, "銅鉱",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(LeadOreId, "鉛鉱",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(GoldOreId, "金",       GoodTier.Intermediate, 0.0, 0.5, 2.0, [], WearRatePerMonth: 0.005),
        new(SulfurId, "硫黄",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(SaltId, "塩",       GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(LimestoneId, "石灰石",   GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(SilicaSandId, "珪砂",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(ClayId, "粘土",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(CrudeOilId, "原油",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(TimberId, "木",       GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.03),
        new(CottonId, "綿花",     GoodTier.Intermediate, 0.0, 0.5, 2.6, [], WearRatePerMonth: 0.02),
        new(HempId, "麻",       GoodTier.Intermediate, 0.0, 0.5, 2.6, [], WearRatePerMonth: 0.02),
        new(SilkId, "生糸",     GoodTier.Intermediate, 0.0, 0.5, 2.2, [], WearRatePerMonth: 0.03),
        new(WheatId, "麦",       GoodTier.Intermediate, 0.0, 0.4, 2.6, [], GrainChain: true, WearRatePerMonth: 0.06),
        new(FruitId, "果実",     GoodTier.Intermediate, 0.0, 0.6, 2.4, [], WearRatePerMonth: 0.2),
        new(DyeId, "染料",     GoodTier.Intermediate, 0.0, 0.5, 2.5, [], WearRatePerMonth: 0.02),
        new(TobaccoLeafId, "葉煙草",   GoodTier.Intermediate, 0.0, 0.7, 2.3, [], WearRatePerMonth: 0.06),
        new(LivestockId, "畜産",     GoodTier.Intermediate, 0.0, 0.5, 2.4, [], WearRatePerMonth: 0.15),
        new(FishId, "漁獲",     GoodTier.Intermediate, 0.0, 0.6, 2.4, [], WearRatePerMonth: 0.2),

        new(RawRubberId, "生ゴム",   GoodTier.Intermediate, 0.0,  0.5, 2.2, [], true, WearRatePerMonth: 0.02),
        new(PalmOilId, "パーム油", GoodTier.Intermediate, 0.0,  0.5, 2.2, [], true, WearRatePerMonth: 0.03),
        new(IvoryId, "象牙",     GoodTier.Intermediate, 0.0,  0.9, 1.8, [], true, WearRatePerMonth: 0.005),
        new(SugarCaneId, "サトウキビ", GoodTier.Intermediate, 0.0, 0.6, 2.4, [], true, WearRatePerMonth: 0.1),
        new(CocoaId, "カカオ",   GoodTier.Intermediate, 0.0,  0.8, 2.0, [], true, WearRatePerMonth: 0.03),
        new(SpiceId, "香辛料",   GoodTier.Luxury,       0.05, 0.9, 2.0, [], true, WearRatePerMonth: 0.005),

        new(IronId, "鉄",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(IronOreId,1.0), I(CoalId,0.824), I(LimestoneId,0.25)], WearRatePerMonth: 0.005),
        new(SteelId, "鋼",       GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(IronId,1.0), I(CoalId,0.659)], WearRatePerMonth: 0.005),
        new(CopperId, "銅",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(CopperOreId,1.0), I(CoalId,0.659)], WearRatePerMonth: 0.005),
        new(LeadId, "鉛",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(LeadOreId,1.0), I(CoalId,0.659)], WearRatePerMonth: 0.005),
        new(CementId, "セメント", GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LimestoneId,0.8), I(ClayId,0.2), I(CoalId,0.4)], WearRatePerMonth: 0.005),
        new(GlassId, "ガラス",   GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(SilicaSandId,0.7), I(LimestoneId,0.2028), I(SodaAshId,0.15)], WearRatePerMonth: 0.005),
        new(SulfuricAcidId, "硫酸", GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(SulfurId,0.8), I(SaltpeterId,0.1), I(CoalId,0.6)], WearRatePerMonth: 0.03),
        new(GunpowderId, "火薬",     GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(SaltpeterId,0.75), I(SulfurId,0.10), I(TimberId,0.15)], WearRatePerMonth: 0.03),
        new(LumberId, "製材",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(TimberId,1.0)], WearRatePerMonth: 0.02),
        new(CottonClothId, "綿布",     GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(CottonId,1.0)], WearRatePerMonth: 0.02),
        new(CanvasId, "帆布",     GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(HempId,1.0)], WearRatePerMonth: 0.02),
        new(WoolId, "羊毛",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LivestockId,0.5)], WearRatePerMonth: 0.02),
        new(WoolenClothId, "毛織物",   GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(WoolId,1.0)], WearRatePerMonth: 0.02),
        new(SilkClothId, "絹織物",   GoodTier.Intermediate, 0.0, 0.4, 1.7, [I(SilkId,1.0)], WearRatePerMonth: 0.02),
        new(LeatherId, "革",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(LivestockId,0.5), I(TimberId,0.15)], WearRatePerMonth: 0.02),
        new(FlourId, "小麦粉",   GoodTier.Intermediate, 0.0, 0.4, 2.4, [I(WheatId,1.0)], GrainChain: true, WearRatePerMonth: 0.06),
        new(MeatId, "肉",       GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LivestockId,0.6)], NonTradeable: true, WearRatePerMonth: 0.15),
        new(SugarId, "砂糖",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(SugarCaneId,1.0)], WearRatePerMonth: 0.02),
        new(RubberId, "ゴム",     GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(RawRubberId,1.0), I(SulfurId,0.2)], WearRatePerMonth: 0.02),

        new(MachineryId, "機械",     GoodTier.Producer,     0.0, 0.4, 1.4, [I(SteelId,1.514), I(CopperId,0.570)], WearRatePerMonth: 0.005),
        new(SteamEngineId, "蒸気機関", GoodTier.Producer,     0.0, 0.4, 1.4, [I(MachineryId,0.726), I(CoalId,0.4)], WearRatePerMonth: 0.005),
        new(RailId, "線路",     GoodTier.Producer,     0.0, 0.4, 1.8, [I(SteelId,1.0)], WearRatePerMonth: 0.005),

        new(ClothingId, "衣料",     GoodTier.Daily,        0.30, 0.6, 1.8, [I(CottonClothId,0.6), I(DyeId,0.2)], WearRatePerMonth: 0.02),
        new(LuxuryClothingId, "高級衣料", GoodTier.Luxury,       0.10, 0.9, 1.5, [I(SilkClothId,0.4), I(WoolenClothId,0.3), I(FurId,0.3)], WearRatePerMonth: 0.02),
        new(FurnitureId, "家具",     GoodTier.Daily,        0.16, 0.6, 1.7, [I(LumberId,0.6), I(LeatherId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(LuxuryFurnitureId, "高級家具", GoodTier.Luxury,       0.10, 0.9, 1.5, [I(LumberId,0.5), I(SilkClothId,0.3), I(FurId,0.2)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(PaperId, "紙",       GoodTier.Daily,        0.10, 0.6, 1.8, [I(TimberId,1.404), I(SulfuricAcidId,0.05), I(SodaAshId,0.0406)], WearRatePerMonth: 0.02),
        new(BreadId, "パン",     GoodTier.Staple,       0.55, 0.3, 2.0, [I(FlourId,1.0)], WearRatePerMonth: 0.25),
        new(PreservedMeatId, "加工肉",   GoodTier.Daily,        0.12, 0.5, 1.8, [I(MeatId,0.6), I(FlourId,0.2688), I(SaltId,0.05)], Nutrition: 1.0, WearRatePerMonth: 0.06),
        new(DriedFishId, "干物",     GoodTier.Daily,        0.10, 0.5, 1.9, [I(FishId,0.7), I(SaltId,0.2)], Nutrition: 1.0, WearRatePerMonth: 0.03),
        new(CannedFoodId, "缶詰",     GoodTier.Daily,        0.10, 0.6, 1.7, [I(MeatId,0.6), I(IronId,0.25), I(TinOreId,0.05), I(SaltId,0.2)], WearRatePerMonth: 0.005),
        new(BeerId, "ビール",   GoodTier.Daily,        0.12, 0.7, 1.9, [I(WheatId,0.5)], WearRatePerMonth: 0.06),
        new(WineId, "ワイン",   GoodTier.Luxury,       0.12, 0.9, 1.6, [I(FruitId,0.5), I(GlassId,0.2)], WearRatePerMonth: 0.005),
        new(LampOilId, "灯油",     GoodTier.Daily,        0.10, 0.6, 1.9, [I(FishId,0.8)], WearRatePerMonth: 0.005),
        new(TobaccoId, "煙草",     GoodTier.Daily,        0.08, 0.7, 1.9, [I(TobaccoLeafId,1.0)], WearRatePerMonth: 0.03),
        new(LeatherGoodsId, "革製品",   GoodTier.Daily,        0.08, 0.6, 1.7, [I(LeatherId,0.5), I(FurId,0.2), I(DyeId,0.2)], WearRatePerMonth: 0.005),
        new(PorcelainId, "陶磁器",   GoodTier.Daily,        0.07, 0.6, 1.7, [I(ClayId,0.6), I(SilicaSandId,0.3)], WearRatePerMonth: 0.005),
        new(SoapId, "石鹸",     GoodTier.Daily,        0.08, 0.6, 1.8, [I(PalmOilId,0.5), I(SodaAshId,0.2434)], WearRatePerMonth: 0.005),
        new(ChocolateId, "チョコレート", GoodTier.Luxury,   0.05, 0.9, 1.5, [I(CocoaId,0.5), I(SugarId,0.4)], WearRatePerMonth: 0.03),
        new(TeaId, "茶",       GoodTier.Daily,        0.07, 0.8, 2.2, [], true, WearRatePerMonth: 0.03),
        new(CoffeeId, "コーヒー", GoodTier.Daily,        0.07, 0.8, 2.2, [], true, WearRatePerMonth: 0.03),
        new(WaterproofClothId, "防水布",   GoodTier.Daily,        0.05, 0.7, 1.6, [I(RubberId,0.5), I(CottonClothId,0.4)], WearRatePerMonth: 0.02),
        new(MedicineId, "医薬品",   GoodTier.Daily,        0.108, 0.6, 1.6, [I(SulfuricAcidId,0.25), I(SodaAshId,0.2028), I(FruitId,0.3)], WearRatePerMonth: 0.03),
        new(QuinineId, "キニーネ", GoodTier.Daily,        0.04, 0.7, 1.6, [I(CinchonaBarkId,0.5), I(SulfuricAcidId,0.3)], WearRatePerMonth: 0.03),
        new(ApplianceId, "電化製品", GoodTier.Luxury,       0.10, 0.9, 1.3, [I(ElectricityId,0.5), I(MachineryId,0.363), I(CopperId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),

        new(ArtilleryId, "火砲",     GoodTier.Producer,     0.0,  0.4, 1.3, [I(SteelId,1.0), I(MachineryId,0.363)], WearRatePerMonth: 0.005),
        new(RifleId, "小銃",     GoodTier.Producer,     0.0,  0.4, 1.4, [I(SteelId,0.6), I(LumberId,0.4)], WearRatePerMonth: 0.005),
        new(AmmunitionId, "弾薬",     GoodTier.Producer,     0.0,  0.4, 1.5, [I(GunpowderId,0.6), I(LeadId,0.3), I(CopperId,0.2)], WearRatePerMonth: 0.005),
        new(SailingShipId, "帆船",     GoodTier.Producer,     0.0,  0.4, 1.4, [I(LumberId,1.0), I(CanvasId,0.5)], WearRatePerMonth: 0.005),
        new(SteamshipId, "汽船",     GoodTier.Producer,     0.0,  0.4, 1.3, [I(SteamEngineId,0.6), I(LumberId,0.6)], WearRatePerMonth: 0.005),
        new(WarshipId, "軍艦",     GoodTier.Producer,     0.0,  0.4, 1.2, [I(SailingShipId,1.0), I(ArtilleryId,0.5)], WearRatePerMonth: 0.005),
        new(ElectricityId, "電力",     GoodTier.Producer,     0.0,  0.4, 1.4, [I(MachineryId,0.149), I(CoalId,0.9)], NonTradeable: true, WearRatePerMonth: 1.0),
        new(LocomotiveId, "機関車",   GoodTier.Producer,     0.0,  0.4, 1.3, [I(SteamEngineId,0.6), I(SteelId,0.5)], WearRatePerMonth: 0.005),
        new(JewelryId, "宝飾",     GoodTier.Luxury,       0.09, 1.0, 1.4, [I(GoldOreId,0.3), I(SilverId,0.3), I(GlassId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(WatchId, "時計",     GoodTier.Luxury,       0.09, 1.0, 1.4, [I(GoldOreId,0.2), I(SilverId,0.2), I(MachineryId,0.290)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(InstrumentId, "楽器",     GoodTier.Luxury,       0.04, 1.0, 1.4, [I(LumberId,0.5), I(IvoryId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(OpiumId, "アヘン",   GoodTier.Luxury,       0.02, 0.8, 2.0, [], true, WearRatePerMonth: 0.005),

        new(LaborId, "労働力",   GoodTier.Service,      0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),
        new(EducationId, "教育",     GoodTier.Service,      0.06, 0.8, 2.0, [I(PaperId,0.05)], NonTradeable: true, WearRatePerMonth: 1.0),
        new(HealthcareId, "医療",     GoodTier.Service,      0.05, 0.8, 3.0, [I(MedicineId,0.10), I(SoapId,0.05)], NonTradeable: true, WearRatePerMonth: 1.0),
        new(AdminId, "行政",     GoodTier.Service,      0.0,  0.5, 3.0, [I(PaperId,0.10)], NonTradeable: true, WearRatePerMonth: 1.0),
        new(MilitaryPowerId, "兵力",     GoodTier.Service,      0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),

        new(ChromeId, "クロム",   GoodTier.Intermediate, 0.0,  0.5, 2.4, [], WearRatePerMonth: 0.005),

        new(GridId, "送電網",   GoodTier.Producer,     0.0,  0.4, 1.6, [I(CopperId,0.6), I(MachineryId,0.299)], NonTradeable: true, WearRatePerMonth: 1.0),

        new(AdvLaborId, "高度労働力", GoodTier.Service,   0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),
        new(FarmLaborId, "農業労働力", GoodTier.Service,   0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),

        new(RiceId, "米",      GoodTier.Staple,       0.20, 0.4, 2.6, [], WearRatePerMonth: 0.06),
        new(SpiritsId, "蒸留酒",  GoodTier.Daily,        0.05, 0.6, 1.8, [I(WheatId,0.6)], WearRatePerMonth: 0.005),

        new(MachineGunId, "機関銃",  GoodTier.Producer,     0.0,  0.4, 1.2, [I(SteelId,0.5), I(MachineryId,0.645)], WearRatePerMonth: 0.005),
        new(IroncladId, "装甲艦",  GoodTier.Producer,     0.0,  0.4, 1.1, [I(SteamshipId,1.0), I(SteelId,1.0), I(ArtilleryId,0.5)], WearRatePerMonth: 0.005),
        new(CheeseId, "チーズ",   GoodTier.Daily,        0.06, 0.6, 1.8, [I(LivestockId,0.6)], WearRatePerMonth: 0.03),

        new(VegetableId, "野菜",     GoodTier.Staple,       0.25, 0.4, 3.0, [], WearRatePerMonth: 0.2),

        new(FertilizerId, "肥料",     GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(SulfuricAcidId,0.2336), I(LivestockId,0.45)], WearRatePerMonth: 0.005),
        new(FodderId, "飼料",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(WheatId,0.6)], WearRatePerMonth: 0.06),
        new(InfrastructureId, "上下水道", GoodTier.Service,      0.0,  0.4, 2.5, [I(CementId,0.4), I(SteelId,0.2)], NonTradeable: true, WearRatePerMonth: 0.005),
        new(BookId, "書籍",     GoodTier.Daily,        0.02, 0.6, 2.6, [I(PaperId,0.5)], WearRatePerMonth: 0.005),
        new(GasId, "ガス",     GoodTier.Daily,        0.03, 0.6, 2.2, [I(CoalId,0.6)], WearRatePerMonth: 0.005),
        new(ElecInfraId, "電化インフラ", GoodTier.Service,   0.0,  0.4, 1.6, [I(GridId,0.4), I(ApplianceId,0.3), I(SteelId,0.2)], NonTradeable: true, WearRatePerMonth: 0.005),

        new(PotatoId, "ジャガイモ", GoodTier.Staple, 0.06, 0.4, 3.2, [], WearRatePerMonth: 0.2),
        new(MaizeId, "トウモロコシ", GoodTier.Staple, 0.04, 0.4, 2.8, [], WearRatePerMonth: 0.06),
        new(TinOreId, "錫鉱",     GoodTier.Intermediate, 0.0, 0.5, 2.4, [], WearRatePerMonth: 0.005),
        new(SaltpeterId, "硝石",     GoodTier.Intermediate, 0.0, 0.5, 2.8, [], WearRatePerMonth: 0.005),
        new(FurId, "毛皮",     GoodTier.Intermediate, 0.0, 0.5, 2.0, [], WearRatePerMonth: 0.02),
        new(SilverId, "銀",       GoodTier.Intermediate, 0.0, 0.5, 2.0, [], WearRatePerMonth: 0.005),
        new(GuanoId, "グアノ",   GoodTier.Intermediate, 0.0, 0.6, 2.2, [], true, WearRatePerMonth: 0.03),
        new(TankId, "戦車",     GoodTier.Producer, 0.0, 0.4, 1.1, [I(SteelId,1.0), I(CombustionEngineId,0.6), I(ArtilleryId,0.4)], WearRatePerMonth: 0.005),
        new(DreadnoughtId, "弩級戦艦", GoodTier.Producer, 0.0, 0.4, 1.0, [I(IroncladId,1.0), I(SteelId,1.0), I(MachineryId,0.537)], WearRatePerMonth: 0.005),
        new(DomesticServiceId, "家事奉仕", GoodTier.Service, 0.06, 0.7, 1.0, [], NonTradeable: true, WearRatePerMonth: 1.0),
        new(CombustionEngineId, "内燃機関", GoodTier.Producer, 0.0, 0.4, 1.1, [I(SteelId,1.0), I(MachineryId,0.5616)], WearRatePerMonth: 0.005),
        new(HousingId, "住居", GoodTier.Daily, 0.0, 0.4, 1.2, [I(LumberId,0.5), I(LimestoneId,0.2)], NonTradeable: true, WearRatePerMonth: 0.005),
        new(HorseId, "馬",       GoodTier.Intermediate, 0.0, 0.5, 1.6, [I(LivestockId,0.6), I(FodderId,0.4)], WearRatePerMonth: 0.04),
        new(DynamiteId, "ダイナマイト", GoodTier.Intermediate, 0.0, 0.5, 1.4, [I(SulfuricAcidId,0.4), I(SaltpeterId,0.1)], WearRatePerMonth: 0.005),
        new(BicycleId, "自転車",   GoodTier.Daily, 0.04, 0.5, 1.2, [I(SteelId,0.5), I(RubberId,0.3), I(MachineryId,0.218)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(MilitaryAircraftId, "軍用機",   GoodTier.Producer, 0.0, 0.4, 1.16, [I(CombustionEngineId,0.567), I(LumberId,0.284), I(WaterproofClothId,0.189), I(MachineGunId,0.123)], WearRatePerMonth: 0.005),
        new(AutomobileId, "自動車",   GoodTier.Luxury, 0.05, 0.9, 1.2, [I(CombustionEngineId,0.6), I(SteelId,0.4), I(RubberId,0.2)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(ChemicalWeaponId, "化学兵器", GoodTier.Producer, 0.0, 0.4, 1.2, [I(SaltId,0.7), I(ElectricityId,0.2025), I(SaltpeterId,0.2)], WearRatePerMonth: 0.005),
        new(NewspaperId, "新聞",     GoodTier.Daily, 0.06, 0.6, 1.8, [I(PaperId,0.6), I(MachineryId,0.036)], WearRatePerMonth: 0.005),
        new(RadioId, "ラジオ",   GoodTier.Daily, 0.03, 0.6, 1.4, [I(ApplianceId,0.6), I(MachineryId,0.145)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(HigherEducationId, "高等教育", GoodTier.Service, 0.0, 0.8, 3.0, [I(BookId,0.4)], NonTradeable: true, WearRatePerMonth: 1.0),
        new(RefrigeratorId, "冷蔵庫",   GoodTier.Luxury, 0.06, 0.9, 1.3, [I(MachineryId,0.430), I(CoalId,0.6444)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(CinchonaBarkId, "金鶏樹皮", GoodTier.Intermediate, 0.0,  0.6, 2.0, [], true, WearRatePerMonth: 0.03),
        new(SodaAshId, "ソーダ灰", GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(TimberId,2.0)], WearRatePerMonth: 0.03),
        new(BuildingId, "建造物", GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LumberId,1.0), I(LimestoneId,0.15)], NonTradeable: true, WearRatePerMonth: 0.00168),
    ];

    public static readonly GoodDef[] Goods = LoadGoods();

    public static int[] RgoBoosterGoods(int id) => id switch
    {
        CottonId or HempId or WheatId or FruitId or DyeId or TobaccoLeafId or RiceId or VegetableId or PotatoId or MaizeId => [FertilizerId, HorseId],
        FishId => [SailingShipId],
        LivestockId => [FodderId],
        IronOreId or CoalId or CopperOreId or LeadOreId or GoldOreId or SulfurId or LimestoneId or SilicaSandId or ClayId or CrudeOilId or ChromeId or TinOreId or SaltpeterId or SilverId => [IronId, DynamiteId],
        _ => [],
    };

    public static double WeatherExposure(int id) => id switch
    {
        CottonId or HempId or WheatId or FruitId or DyeId or TobaccoLeafId or RiceId or VegetableId or PotatoId or MaizeId => 1.0,
        SugarCaneId or CocoaId or TeaId or CoffeeId or OpiumId => 0.8,
        RawRubberId or PalmOilId => 0.6,
        LivestockId => 0.45,
        FishId or FurId => 0.35,
        _ => 0.0,
    };

    public static bool IsUbiquitousMineral(int id, Coefficients c)
        => c.UbiquitousMineralExempt > 0 && id is LimestoneId or SilicaSandId or ClayId;
    public static bool IsScarceMineral(int id) => id is GoldOreId or SulfurId or ChromeId or TinOreId or SaltpeterId or SilverId;
    public static bool IsScarceMineral(int id, Coefficients c)
        => c.ScarceMineralOnlySulfur >= 4 ? id is SulfurId or SilverId
         : c.ScarceMineralOnlySulfur >= 3 ? id is SulfurId or SaltpeterId
         : c.ScarceMineralOnlySulfur >= 2 ? id is GoldOreId or SulfurId or SaltpeterId or SilverId
         : c.ScarceMineralOnlySulfur > 0 ? id == SulfurId : IsScarceMineral(id);

    public static bool IsMineral(int id) => id is IronOreId or CoalId or CopperOreId or LeadOreId or GoldOreId or SulfurId or LimestoneId or SilicaSandId or ClayId or CrudeOilId or ChromeId or TinOreId or SaltpeterId or SilverId;

    public static string RgoFacilityName(int id) => id switch
    {
        _ when IsMineral(id) => "鉱山",
        GuanoId => "採掘場",
        LivestockId => "牧場",
        FishId => "漁場",
        FurId or IvoryId => "猟場",
        TimberId => "伐採場",
        SilkId => "養蚕場",
        _ => "農場",
    };

    public static bool IsDiseaseProneCrop(int id) => id is WheatId or FruitId or RiceId or PotatoId or MaizeId or CoffeeId;

    public static bool IsColonial(int id) => id is RawRubberId or PalmOilId or IvoryId or SugarCaneId or CocoaId or SpiceId or OpiumId or GuanoId or TeaId or CoffeeId or CinchonaBarkId;

    public const int LaborId = 84;
    public const int EducationId = 85;
    public const int HigherEducationId = 125;
    public const int HealthcareId = 86;
    public const int AdminId = 87;
    public const int InfrastructureId = 101;
    public const int BookId = 102;
    public const int GasId = 103;
    public const int ElecInfraId = 104;
    public const int HousingId = 116;
    public const int NewspaperId = 123;
    public const int RadioId = 124;
    public const int MilitaryPowerId = 88;
    public const int GridId = 90;
    public const int AdvLaborId = 91;
    public const int FarmLaborId = 92;
    public static bool IsLaborGood(int gid) => gid == LaborId || gid == AdvLaborId || gid == FarmLaborId;
    public const int ElectricityId = 78;
    public const int GoldOreId = 4;
    public const int SilverId = 110;
    public const int CoalId = 1;
    public const int DomesticServiceId = 114;
    public const int LivestockId = 19;
    public const int FishId = 20;
    public const int TimberId = 11;
    public const int SilkId = 14;
    public const int IvoryId = 23;
    public const int GuanoId = 111;
    public const int FurId = 109;
    public const int SaltId = 6;
    public const int TeaId = 66;
    public const int SulfuricAcidId = 33;
    public const int CrudeOilId = 10;
    public const int WheatId = 15;
    public const int RailId = 48;
    public const int CannedFoodId = 57;
    public const int QuinineId = 70;
    public const int SailingShipId = 75;
    public const int SteamshipId = 76;
    public const int LocomotiveId = 79;
    public const int RiceId = 93;
    public const int VegetableId = 98;
    public const int FertilizerId = 99;
    public const int PotatoId = 105;
    public const int MaizeId = 106;
    public const int HorseId = 117;
    public const int IronOreId = 0;
    public const int CopperOreId = 2;
    public const int LeadOreId = 3;
    public const int SulfurId = 5;
    public const int LimestoneId = 7;
    public const int SilicaSandId = 8;
    public const int ClayId = 9;
    public const int CottonId = 12;
    public const int HempId = 13;
    public const int FruitId = 16;
    public const int DyeId = 17;
    public const int TobaccoLeafId = 18;
    public const int RawRubberId = 21;
    public const int PalmOilId = 22;
    public const int SugarCaneId = 24;
    public const int CocoaId = 25;
    public const int SpiceId = 26;
    public const int IronId = 27;
    public const int SteelId = 28;
    public const int CopperId = 29;
    public const int LeadId = 30;
    public const int CementId = 31;
    public const int GlassId = 32;
    public const int GunpowderId = 34;
    public const int LumberId = 35;
    public const int CottonClothId = 36;
    public const int CanvasId = 37;
    public const int WoolId = 38;
    public const int WoolenClothId = 39;
    public const int SilkClothId = 40;
    public const int LeatherId = 41;
    public const int FlourId = 42;
    public const int MeatId = 43;
    public const int SugarId = 44;
    public const int RubberId = 45;
    public const int MachineryId = 46;
    public const int SteamEngineId = 47;
    public const int ClothingId = 49;
    public const int LuxuryClothingId = 50;
    public const int FurnitureId = 51;
    public const int LuxuryFurnitureId = 52;
    public const int PaperId = 53;
    public const int BreadId = 54;
    public const int PreservedMeatId = 55;
    public const int DriedFishId = 56;
    public const int BeerId = 58;
    public const int WineId = 59;
    public const int LampOilId = 60;
    public const int TobaccoId = 61;
    public const int LeatherGoodsId = 62;
    public const int PorcelainId = 63;
    public const int SoapId = 64;
    public const int ChocolateId = 65;
    public const int CoffeeId = 67;
    public const int WaterproofClothId = 68;
    public const int MedicineId = 69;
    public const int ApplianceId = 71;
    public const int ArtilleryId = 72;
    public const int RifleId = 73;
    public const int AmmunitionId = 74;
    public const int WarshipId = 77;
    public const int JewelryId = 80;
    public const int WatchId = 81;
    public const int InstrumentId = 82;
    public const int OpiumId = 83;
    public const int ChromeId = 89;
    public const int SpiritsId = 94;
    public const int MachineGunId = 95;
    public const int IroncladId = 96;
    public const int CheeseId = 97;
    public const int FodderId = 100;
    public const int TinOreId = 107;
    public const int SaltpeterId = 108;
    public const int TankId = 112;
    public const int DreadnoughtId = 113;
    public const int CombustionEngineId = 115;
    public const int DynamiteId = 118;
    public const int BicycleId = 119;
    public const int MilitaryAircraftId = 120;
    public const int AutomobileId = 121;
    public const int ChemicalWeaponId = 122;
    public const int RefrigeratorId = 126;
    public const int SodaAshId = 128;
    public const int BuildingId = 129;
    public const int CinchonaBarkId = 127;

    public static double ServiceRefPrice(int id) => id switch
    {
        LaborId or MilitaryPowerId or FarmLaborId => 0.5,
        AdvLaborId => 0.8,
        _ => 0.6,
    };

    public static double AdvLaborPerOutput(int id) => id switch
    {
        ElectricityId => 0.15,
        MachineryId or SteamEngineId or ApplianceId or SteamshipId or WarshipId or LocomotiveId or GridId or MachineGunId or IroncladId or TankId or DreadnoughtId or CombustionEngineId => 0.10,
        IronId or SteelId or CopperId or LeadId or CementId or GlassId or SulfuricAcidId or SodaAshId or GunpowderId => 0.05,
        _ => 0.02,
    };

    public const double LaborPerOutput = 0.5;
    public const double FarmLaborPerOutput = 0.4;
    public const double FactoryVa = 0.06;
    public const double RgoVa = 0.34;

    public static readonly double[] RefPrices = ComputeRefPrices();

    private const int ChainRelaxIterations = 24;

    public static bool IsRefrigerable(int id) => id is FruitId or LivestockId or FishId or MeatId or PreservedMeatId;

    public static double WearRatePerMonthOf(int id) => Goods[id].WearRatePerMonth;

    public static double DurabilityOf(int id) => 1.0 - Goods[id].WearRatePerMonth;

    public static GoodUse UseOf(int id) => Goods[id].Use;

    public static double TransportWeight(int id) => id switch
    {
        RailId or LocomotiveId or SailingShipId or SteamshipId or WarshipId or IroncladId or DreadnoughtId => 6.0,
        MachineryId or SteamEngineId or ArtilleryId or MachineGunId or TankId or CombustionEngineId => 3.0,
        IronOreId or CoalId or CopperOreId or LeadOreId or LimestoneId or ClayId or CrudeOilId or TimberId or IronId or SteelId or CopperId or LeadId or CementId or LumberId or ChromeId or FertilizerId or TinOreId or SaltpeterId or GuanoId => 2.5,
        GoldOreId or SilkId or DyeId or SpiceId or GunpowderId or SilkClothId or MedicineId or QuinineId or SilverId => 0.4,
        _ => 1.0,
    };

    public static (string Category, double Cost, int Quarters) FactorySpec(int id) => id switch
    {
        DreadnoughtId => ("超重軍需", 320_000, 28),
        TankId => ("超重軍需", 200_000, 16),
        IroncladId => ("先端軍需", 240_000, 22),
        MachineGunId => ("高級軍需", 150_000, 12),
        ElectricityId or ApplianceId or LocomotiveId or SteamshipId or WarshipId or GridId or HigherEducationId => ("先端", 180_000, 20),
        MachineryId or SteamEngineId or RailId or CombustionEngineId => ("機械", 110_000, 12),
        ArtilleryId or RifleId or AmmunitionId or SailingShipId             => ("軍需", 90_000,  8),
        IronId or SteelId or CopperId or LeadId or CementId or GlassId or SulfuricAcidId or SodaAshId or GunpowderId or HealthcareId => ("重工業", 70_000, 8),
        EducationId => ("軽工業", 30_000, 4),
        _ => ("軽工業", 30_000, 4),
    };

    public static readonly double AnimalNutritionNeedRatio = ComputeAnimalNutritionNeedRatio();
    private static double ComputeAnimalNutritionNeedRatio()
    {
        double animal = 0, staple = 0;
        foreach (var d in Goods)
        {
            if (d.Tier == GoodTier.Staple) staple += d.BaseNeed;
            else if (d.Nutrition > 0) animal += d.Nutrition * d.BaseNeed;
        }
        return staple > 1e-9 ? animal / staple : 0;
    }

    public const int ReplacesOwn = -1, LandFreight = -2;
    public const int DiscoveryBaseDecade = 1780;
    public static readonly DiscoveryRow[] Discoveries =
    [
        new("蒸気機関", LaborId, "蒸気機関", DiscoveryField.Process, 1780),
        new("甜菜糖", SugarId, "甜菜糖", DiscoveryField.Process, 1800),
        new("魚の缶詰", CannedFoodId, "魚の缶詰", DiscoveryField.Process, 1810),
        new("キニーネの単離", QuinineId, "", DiscoveryField.Process, 1820),
        new("黄鉄鉱法", SulfuricAcidId, "黄鉄鉱法", DiscoveryField.Process, 1830),
        new("木材パルプ製紙", PaperId, "", DiscoveryField.Process, 1840),
        new("過リン酸石灰", FertilizerId, "", DiscoveryField.Process, 1840),
        new("転炉鋼", SteelId, "", DiscoveryField.Process, 1850),
        new("石油ケロシン", LampOilId, "石油ケロシン", DiscoveryField.Process, 1850),
        new("鉄筋コンクリート", HousingId, "鉄筋コンクリート", DiscoveryField.Process, 1860),
        new("クロムなめし", LeatherId, "クロムなめし", DiscoveryField.Process, 1880),
        new("無煙火薬", GunpowderId, "無煙火薬", DiscoveryField.Process, 1880),
        new("水力発電", ElectricityId, "水力発電", DiscoveryField.Process, 1880),
        new("蒸気タービン", ElectricityId, "蒸気タービン", DiscoveryField.Process, 1880),
        new("電動機", LaborId, "電動", DiscoveryField.Process, 1880),
        new("人絹", SilkClothId, "人絹", DiscoveryField.Process, 1890),
        new("石油火力", ElectricityId, "石油火力", DiscoveryField.Process, 1900),
        new("窒素固定", FertilizerId, "ハーバーボッシュ", DiscoveryField.Process, 1910),
        new("合成ゴム", RubberId, "合成ゴム", DiscoveryField.Process, 1910),
        new("ガス灯", GasId, "", DiscoveryField.Product, 1810, Replaces: LampOilId),
        new("缶詰", CannedFoodId, "", DiscoveryField.Product, 1810, Replaces: PreservedMeatId),
        new("汽船", SteamshipId, "", DiscoveryField.Product, 1820, Replaces: SailingShipId),
        new("機関車", LocomotiveId, "", DiscoveryField.Product, 1820, Replaces: LandFreight),
        new("加硫ゴム", RubberId, "", DiscoveryField.Product, 1830, Replaces: LeatherId),
        new("ダイナマイト", DynamiteId, "", DiscoveryField.Product, 1860, Replaces: GunpowderId),
        new("冷蔵", RefrigeratorId, "", DiscoveryField.Product, 1870, Replaces: PreservedMeatId),
        new("石炭火力", ElectricityId, "", DiscoveryField.Product, 1880, Replaces: GasId),
        new("送電網", GridId, "", DiscoveryField.Product, 1880, Replaces: GasId),
        new("電化製品", ApplianceId, "", DiscoveryField.Product, 1880, Replaces: DomesticServiceId),
        new("自転車", BicycleId, "", DiscoveryField.Product, 1880, Replaces: HorseId, After: "加硫ゴム"),
        new("内燃機関", CombustionEngineId, "", DiscoveryField.Product, 1880, Replaces: SteamEngineId, After: "蒸気機関"),
        new("自動車", AutomobileId, "", DiscoveryField.Product, 1890, Replaces: HorseId, After: "内燃機関"),
        new("ラジオ", RadioId, "", DiscoveryField.Product, 1920, Replaces: NewspaperId, After: "電化製品"),
        new("装甲艦", IroncladId, "", DiscoveryField.Weapon, 1850, WarDomain.Sea, After: "汽船"),
        new("機関銃", MachineGunId, "", DiscoveryField.Weapon, 1880, WarDomain.Land),
        new("弩級戦艦", DreadnoughtId, "", DiscoveryField.Weapon, 1900, WarDomain.Sea, After: "装甲艦"),
        new("戦車", TankId, "", DiscoveryField.Weapon, 1910, WarDomain.Land, After: "内燃機関"),
        new("軍用機", MilitaryAircraftId, "", DiscoveryField.Weapon, 1910, WarDomain.Air, After: "内燃機関"),
        new("化学兵器", ChemicalWeaponId, "", DiscoveryField.Weapon, 1910, WarDomain.Land),
        new("特許", NoGood, "", DiscoveryField.Institution, 1780),
        new("株式会社", NoGood, "", DiscoveryField.Institution, 1850),
        new("社会主義", NoGood, "", DiscoveryField.Idea, 1830),
        new("共産主義", NoGood, "", DiscoveryField.Idea, 1840, After: "社会主義"),
        new("帝国主義", NoGood, "", DiscoveryField.Idea, 1860),
        new("ファシズム", NoGood, "", DiscoveryField.Idea, 1920),
        new("合成染料", DyeId, "合成染料", DiscoveryField.Process, 1850),
        new("ルブラン法", SodaAshId, "ルブラン法", DiscoveryField.Process, 1790),
        new("ソルベー法", SodaAshId, "ソルベー法", DiscoveryField.Process, 1860),
        new("電解", SodaAshId, "電解", DiscoveryField.Process, 1890, After: "石炭火力"),
        new("鋳鉄の骨組み", BuildingId, "鋳鉄の骨組み", DiscoveryField.Process, 1790),
    ];
    public const int NoGood = -1;

    private static readonly (int Good, string Recipe, int SuccessorGood, string SuccessorRecipe)[] Retirements =
    [
        (SteamEngineId, "鋳鉄の機関", SteelId, ""),
    ];

    private static readonly int[] WeaponSuccessorGoods = [RifleId, ArtilleryId, SteamEngineId];

    public static readonly int[] DiscoveryAfter = BuildDiscoveryAfter();
    private static int[] BuildDiscoveryAfter()
    {
        var res = new int[Discoveries.Length];
        for (int i = 0; i < Discoveries.Length; i++)
        {
            string a = Discoveries[i].After;
            if (a.Length == 0) { res[i] = -1; continue; }
            int j = System.Array.FindIndex(Discoveries, x => x.Name == a);
            if (j < 0 || j == i) throw new System.InvalidOperationException($"発見の表の行〈{Discoveries[i].Name}〉の前提〈{a}〉が表に無いか自分自身");
            res[i] = j;
        }
        for (int i = 0; i < res.Length; i++)
        {
            int k = i, steps = 0;
            while (res[k] >= 0) { k = res[k]; if (++steps > res.Length) throw new System.InvalidOperationException($"発見の表の前提に輪がある（{Discoveries[i].Name} から）"); }
        }
        return res;
    }
    public static int DiscoveryTier(int row)
    {
        int t = 1;
        while (DiscoveryAfter[row] >= 0) { row = DiscoveryAfter[row]; t++; }
        return t;
    }
    public static int DiscoveryLine(int row)
    {
        while (DiscoveryAfter[row] >= 0) row = DiscoveryAfter[row];
        return row;
    }

    public static int DiscoveryRowOf(int good, string recipe)
    {
        if (good < 0) return -1;
        for (int i = 0; i < Discoveries.Length; i++) if (Discoveries[i].Good == good && Discoveries[i].Recipe == recipe) return i;
        return -1;
    }

    public static int DefaultDiscoveryRowOf(int good)
    {
        var t = _defaultRowOfGood ??= BuildDefaultRows();
        return (uint)good < (uint)t.Length ? t[good] : DiscoveryRowOf(good, "");
    }
    private static int[]? _defaultRowOfGood;
    private static int[] BuildDefaultRows()
    {
        var t = new int[Goods.Length];
        for (int g = 0; g < t.Length; g++) t[g] = DiscoveryRowOf(g, "");
        return t;
    }

    public static int DiscoveryRowNamed(string name) => System.Array.FindIndex(Discoveries, d => d.Name == name);

    public static int RetiredByRowOf(int good, string recipe)
    {
        foreach (var (g, r, sg, sr) in Retirements) if (g == good && r == recipe) return DiscoveryRowOf(sg, sr);
        return -1;
    }

    public static int OpenedByRowOf(int good)
        => System.Array.IndexOf(WeaponSuccessorGoods, good) >= 0 ? DiscoveryRowOf(SteelId, "") : -1;

    public enum SiteKind { Deposit, Plantation }
    public readonly record struct SiteRow(string Name, string Place, int Good, int Decade, SiteKind Kind);
    public static readonly SiteRow[] Sites =
    [
        new("カリフォルニアの金", "アルタ=カリフォルニア", GoldOreId, 1840, SiteKind.Deposit),
        new("ヴィクトリアの金", "オーストラリア", GoldOreId, 1850, SiteKind.Deposit),
        new("ラントの金", "南アフリカ", GoldOreId, 1880, SiteKind.Deposit),
        new("クロンダイクの金", "フォートユーコン", GoldOreId, 1890, SiteKind.Deposit),
        new("コムストックの銀", "西部", SilverId, 1850, SiteKind.Deposit),
        new("ルイジアナの硫黄", "西部", SulfurId, 1890, SiteKind.Deposit),
        new("九州の硫黄", "北九州", SulfurId, 1880, SiteKind.Deposit),
        new("ジャワのキナ", "バタヴィア", CinchonaBarkId, 1850, SiteKind.Plantation),
        new("ニルギリのキナ", "マドラス", CinchonaBarkId, 1860, SiteKind.Plantation),
        new("セイロンのキナ", "セイロン", CinchonaBarkId, 1860, SiteKind.Plantation),
    ];

    public static int SiteRowOf(string place, int good)
    {
        for (int i = 0; i < Sites.Length; i++) if (Sites[i].Good == good && Sites[i].Place == place) return i;
        return -1;
    }

    private static double[] ComputeRefPrices()
    {
        var rp = new double[Goods.Length];
        foreach (var g in Goods)
            rp[g.Id] = g.Tier == GoodTier.Service ? ServiceRefPrice(g.Id)
                     : g.Inputs.Length == 0 ? RgoVa + FarmLaborPerOutput * ServiceRefPrice(FarmLaborId)
                     : FactoryVa;
        for (int iter = 0; iter < ChainRelaxIterations; iter++)
            foreach (var g in Goods)
            {
                if (g.Tier == GoodTier.Service || g.Inputs.Length == 0) continue;
                double c = FactoryVa + LaborPerOutput * ServiceRefPrice(LaborId) + AdvLaborPerOutput(g.Id) * ServiceRefPrice(AdvLaborId);
                foreach (var inp in g.Inputs) if (inp.GoodId >= 0 && inp.GoodId < rp.Length) c += inp.QtyPerOutput * rp[inp.GoodId];
                rp[g.Id] = c;
            }
        return rp;
    }
}
