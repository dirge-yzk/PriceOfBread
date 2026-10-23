namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (504) 食料の台帳を主食の和へ：表 fd_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 旗 StapleLedgerSum・StapleDemandRebalance・HarvestShortfallToMarket のどれかが正のときだけ行を出す（全て 0 の腕では表を開きもしない＝計器つき 10 種の生にファイルが増えない）。
// 目的＝事前登録 measurements/fd504_20261023_prereg.txt の M1〜M6（向き・保存・開幕の効き・凶作の向き・挟みが働いていたか・総需要の倍率）を読む。
// 行＝挙げた国だけ×州×四半期。値は直近の INS=1 の月の 1 回（EconomyStep が主食の財ごとに書く蓄え FdDiag を四半期の末の断面で読む）。
// 列（単位は接尾辞：K＝千人・PerMonth＝月あたりの量〔主食の量の単位〕・Share＝0〜1）：
//   tier（Core／Colonial）／popK／stapleNeedPerMonth（POP の FoodNeed の和）／marketNeedPerMonth（主食の市場に出す必要量の和＝予算の前）／
//   desiredPerMonth（主食の欲しい量の和＝予算の前・パニック買いの前）／desiredOldPerMonth（旧の式〔自給は固定・挟みあり〕の欲しい量の和＝反事実）／
//   demandPerMonth（予算の後の買いたい量の和）／subPerMonth（自給で満たした量の和）／oldMetPerMonth（旧の台帳の FoodMet の和）／carryPerMonth（旗①が足した量の和）／
//   carryPopK（足した POP の人口）／fillOld・fillNew（旧と新の台帳の充足の人口加重平均・FoodNeed が正の POP）／belowOldK・belowNewK（充足が 0.8 未満の人口）／
//   harvestMean（主食の必要量で重み付けした自給の収穫係数）／subsistenceMean（自給率の FoodNeed 加重）／clampBindShare（旧の挟み〔PriceFactorMin／Max〕が縛っていた主食の必要量の重みの割合）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（月ごとの値は読まない）／clampBindShare は相対価格の冪だけで挟みを判定する（StapleElasticityZero＝0 の腕では自己価格弾力の項を含まない近似）／
//   助走の月と死んだ州は直近の値が残る（活きている州だけを出す）／旗が 0 の腕では行が無い。
public static partial class Instruments
{
    internal const string FdHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tstapleNeedPerMonth\tmarketNeedPerMonth\tdesiredPerMonth\tdesiredOldPerMonth\tdemandPerMonth\tsubPerMonth\toldMetPerMonth\tcarryPerMonth\tcarryPopK\t"
        + "fillOld\tfillNew\tbelowOldK\tbelowNewK\tharvestMean\tsubsistenceMean\tclampBindShare";

    /// <summary>主食の台帳の蓄え（シミュの状態ではない・セーブに載らない・ConditionalWeakTable の先例）。書くのは EconomyStep の INS=1 の月だけ・読むのは FdFold だけ。</summary>
    internal sealed class FdDiag
    {
        public double MarketNeed, Desired, DesiredOld, Demand, Sub, OldMet, Carry, CarryPopK, StapleNeed, PopK, FillOldW, FillNewW, FillPopK, BelowOldK, BelowNewK, SubsW;
        public double HarvestMean = double.NaN, ClampBindShare = double.NaN;
        public bool Written;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, FdDiag> FdDiags = new();
    internal static FdDiag? FdDiagOf(Region r) => FdDiags.TryGetValue(r, out var d) ? d : null;

    /// <summary>消費の月の始め：蓄えを 0 に戻す（その月の主食の和を溜め直す）。</summary>
    internal static FdDiag FdBegin(Region r)
    {
        var d = FdDiags.GetOrCreateValue(r);
        d.MarketNeed = d.Desired = d.DesiredOld = d.Demand = d.Sub = d.OldMet = d.Carry = d.CarryPopK = d.StapleNeed = d.PopK = d.FillOldW = d.FillNewW = d.FillPopK = d.BelowOldK = d.BelowNewK = d.SubsW = 0;
        d.HarvestMean = d.ClampBindShare = double.NaN; d.Written = true;
        return d;
    }

    /// <summary>消費の月の終わり（①の締めの後）：POP ごとに旧の台帳の FoodMet（<paramref name="oldMet"/>）と新の台帳の FoodMet から充足を求めて溜める。</summary>
    internal static void FdPop(FdDiag d, Pop p, double oldMet)
    {
        d.PopK += p.Size; d.StapleNeed += p.FoodNeed; d.OldMet += oldMet; d.Carry += p.FoodMet - oldMet; d.SubsW += p.Subsistence * p.FoodNeed;
        if (p.FoodMet - oldMet > 0) d.CarryPopK += p.Size;
        if (p.FoodNeed > 1e-9)
        {
            double fo = PoliticalQuantities.Clamp01(oldMet / p.FoodNeed), fn = PoliticalQuantities.Clamp01(p.FoodMet / p.FoodNeed);
            d.FillPopK += p.Size; d.FillOldW += p.Size * fo; d.FillNewW += p.Size * fn;
            if (fo < 0.8) d.BelowOldK += p.Size;
            if (fn < 0.8) d.BelowNewK += p.Size;
        }
    }

    static void FdFold(Sink sk, ulong seed, World w, int q, bool picked)
    {
        var c = w.Coef;
        if (!(c.StapleLedgerSum > 0 || c.StapleDemandRebalance > 0 || c.HarvestShortfallToMarket > 0) || !picked || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            var d = FdDiagOf(r);
            if (d is null || !d.Written || !(d.PopK > 0)) continue;
            double Div(double a, double b) => b > 0 ? a / b : double.NaN;
            sk.Fd ??= Open("fd", seed);
            if (!sk.FdHeader) { sk.Fd.WriteLine(FdHeaderText); sk.FdHeader = true; }
            sk.Fd.WriteLine(string.Join('\t', I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), F(d.PopK), F(d.StapleNeed), F(d.MarketNeed), F(d.Desired), F(d.DesiredOld), F(d.Demand), F(d.Sub), F(d.OldMet), F(d.Carry), F(d.CarryPopK),
                F(Div(d.FillOldW, d.FillPopK)), F(Div(d.FillNewW, d.FillPopK)), F(d.BelowOldK), F(d.BelowNewK), F(d.HarvestMean), F(Div(d.SubsW, d.StapleNeed)), F(d.ClampBindShare)));
        }
    }
}
