namespace PriceOfBread.SimCore;

// 首都の装備の充足が何で律速されるか（値段の門・配給・投入・需要の側）を、同じ盤面の断面で分ける。〔履歴 InstrumentsArms.cs#1〕
// 限界：四半期の末の断面＝発見の四半期（汽船・自動車の解禁）と軍の規模が月の中で変わる四半期は stock_s の値とずれる（行を厳密値として使わない）。
public static partial class Instruments
{
    sealed class ArmsSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ArmsSink> ArmsSinks = new();

    static readonly (string Name, int Id)[] ArmsNamed = [("rifle", Catalog.RifleId), ("ammo", Catalog.AmmunitionId), ("artillery", Catalog.ArtilleryId), ("horse", Catalog.HorseId)];
    static readonly string[] ArmsGoodCols =
    [
        "WeightRate", "WantQty", "ArmoryHeldQty", "ArmoryTargetQty", "OrderQty", "AffordShare", "PriceGateShare", "RationShare", "GotQty",
        "CapitalPriceOverRefLevelRatio", "CapitalStockQty", "NationOutputQty", "NationNonCapitalStockQty", "FillContribShare",
    ];
    static readonly string[] ArmsNationCols =
    [
        "budgetQty", "budgetCeilingQty", "soldierWelfareShare", "navalShare", "armySkillMul", "armySkill", "qualityMul", "cohesionMul", "militarismMul", "techQualityMul", "militaryIndex", "landPowerIndex",
        "otherWantQty", "otherFillContribQty", "engineEquipFillShare", "soldierK", "readinessShare", "wantQtyTotal", "fillContribQtyTotal",
    ];

    internal static string ArmsHeaderText
    {
        get
        {
            var h = new List<string> { "quarter", "year", "nation" };
            foreach (var (n, _) in ArmsNamed) foreach (var c in ArmsGoodCols) h.Add(n + c);
            h.AddRange(ArmsNationCols);
            return string.Join('\t', h);
        }
    }

    /// <summary>四半期の末（<see cref="Quarter"/> の国ごとの段）：挙げた国の首都の州の行を 1 本書く。</summary>
    static void ArmsFold(MultiNationSim sim, ulong seed, World w, int q, bool picked)
    {
        if (!picked || !Wants("armsfill_s") || w.Regions.Count == 0 || w.WarmupFrozen || w.SelfPower is not { } sp) return;
        var cap = w.Regions[0]; var c = w.Coef;
        if (cap.ArmsStock is null) return;   // 武器庫が一度も開いていない＝軍需の段が走っていない（書かない行を 0 と数えない）
        var sk = ArmsSinks.GetValue(sim, _ => new ArmsSink());
        sk.W ??= Open("armsfill", seed);
        if (!sk.Header) { sk.W.WriteLine(ArmsHeaderText); sk.Header = true; }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        const double eps = 1e-9;
        int ng = cap.Markets.Length;
        double scale = cap.MilitaryScale, scalePeace = cap.MilitaryScalePeace;
        double priceLevel = System.Math.Max(cap.PriceLevel, eps);

        // 陸の装備の段に入る財（艦は 0）の、発見の解禁度を通した重み付きの原単位
        int nb = c.MilitaryGoods.Length;
        var rate = new double[nb]; var land = new bool[nb];
        for (int b = 0; b < nb; b++)
        {
            int gid = c.MilitaryGoods[b];
            land[b] = System.Array.IndexOf(c.FleetGoodIds, gid) < 0 && gid >= 0 && gid < ng;
            rate[b] = land[b] ? c.MilitaryRates[b] * Discovery.GoodGate(cap, gid) : 0.0;
        }
        // 支払能力（見積＝必要量×自州の値段の和・上限÷見積を 1 で切る）
        double estCost = 0;
        for (int b = 0; b < nb; b++) if (land[b]) estCost += scale * rate[b] * cap.Markets[c.MilitaryGoods[b]].Price;
        double afford = estCost > eps ? System.Math.Min(1.0, cap.MilitaryBudgetCeiling / estCost) : 0.0;

        var row = new List<string> { "q" + I(q), F(w.Year), NameOf(w) };
        double wantTot = 0, fillTot = 0, otherWant = 0, otherFill = 0;
        for (int b = 0; b < nb; b++)
        {
            if (!land[b]) continue;
            int gid = c.MilitaryGoods[b];
            var mk = cap.Markets[gid];
            double want = scale * rate[b];
            double wear = b < c.MilitaryWearRatesPerMonth.Length ? c.MilitaryWearRatesPerMonth[b] : 1.0;
            double target = scalePeace * rate[b] / System.Math.Max(1e-6, wear);
            bool durable = wear < 1.0;
            double held = cap.ArmsStock.TryGetValue(gid, out double hv) ? hv : double.NaN;
            // 値段の門（式の写し）
            double rp = gid < Catalog.RefPrices.Length ? Catalog.RefPrices[gid] * priceLevel : 0.0;
            double gate = 1.0;
            if (rp > eps)
            {
                double a = c.FactoryBuildAfford(System.Math.Max(mk.Price, eps), rp);
                double g1 = 1.0;
                if (c.MilitaryProcurePriceGateScale < 1.0) g1 *= System.Math.Max(0, c.MilitaryProcurePriceGateScale);
                gate = a > 0 && a <= 1 ? 1.0 - g1 * (1.0 - a) : 1.0;
            }
            double order = want * afford * gate, ration = mk.LastRation.Value, got = order * ration;
            double contrib = durable ? want * (target > eps ? System.Math.Min(1.0, System.Math.Max(0.0, held / target)) : 1.0) : got;
            wantTot += want; fillTot += contrib;
            int ni = System.Array.FindIndex(ArmsNamed, n => n.Id == gid);
            if (ni < 0) { otherWant += want; otherFill += contrib; continue; }
            double outAll = 0, stockOther = 0;
            foreach (var rg in w.Regions)
            {
                if (gid >= rg.Markets.Length) continue;
                outAll += rg.Markets[gid].LastProduced;
                if (!ReferenceEquals(rg, cap)) stockOther += rg.Markets[gid].Stock;
            }
            row.AddRange([F(rate[b]), F(want), F(held), F(target), F(order), F(afford), F(gate), F(ration), F(got),
                F(rp > eps ? mk.Price / rp : double.NaN), F(mk.Stock), F(outAll), F(stockOther), F(want > 0 ? contrib / want : double.NaN)]);
        }
        // 四つの財のどれかが陸の段に無い盤面（小シナリオ）は行を出さない（列の数がずれた行を書かない）
        if (row.Count != 3 + ArmsNamed.Length * ArmsGoodCols.Length) return;

        // 国ごと：戦力の合成の各項（Diplomacy.SyncPowers の式の写し）
        double cohesion = w.EthnicEnabled ? 1 + c.UnityMilitaryQuality * (w.EthnicUnity - 0.5) : 1.0;
        double militarism = 1.0 + (c.MilitarismStrengthBonus - 1.0) * PoliticalQuantities.ArmyProfessionalism(w);
        double qualityMul = 1 + c.MilitaryQualityBonus * w.Region.MilitaryQualityFill + c.MilitaryTier3Bonus * w.Region.MilitaryTier3Fill;
        row.AddRange([F(cap.MilitaryBudget), F(cap.MilitaryBudgetCeiling), F(PowerShares.SoldierWelfare(w)), F(sp.NavalShare.Value), F(sp.ArmySkillMul), F(w.ArmySkill),
            F(qualityMul), F(cohesion), F(militarism), F(1.0), F(sp.Military), F(sp.LandPower),
            F(otherWant), F(otherFill), F(cap.MilitaryEquipFill), F(w.SoldierSizeK()), F(w.MilitaryReadiness), F(wantTot), F(fillTot)]);
        sk.W.WriteLine(string.Join('\t', row));
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void ArmsClose(MultiNationSim sim)
    {
        if (ArmsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
