namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない。〔履歴 InstrumentsRegionProd.cs#1〕
public static partial class Instruments
{
    internal const string RegionProdHeaderText = "factoryOutputRefValuePerMonth\textractionOutputRefValuePerMonth\tfactoryActiveCount\tfactoryBankruptCount\textractionActiveCount\textractionBankruptCount";

    /// <summary>州の生産の列（列の順＝<see cref="RegionProdHeaderText"/>）。番人（テスト）が州から直に呼べるよう値の組で返す。</summary>
    internal static double[] RegionProdRow(Region r)
    {
        double facOut = 0, extOut = 0, facAct = 0, facBank = 0, extAct = 0, extBank = 0;
        foreach (var s in r.Producers)
        {
            bool factory = s.BuysLabor;
            if (!factory && Catalog.IsLaborGood(s.GoodId)) continue;
            if (s.Bankrupt) { if (factory) facBank++; else extBank++; continue; }
            if (s.Scale == 0 && s.Output == 0) continue;
            int g = s.GoodId;
            double rp = g >= 0 && g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
            if (factory) { facAct++; facOut += s.Output * rp; } else { extAct++; extOut += s.Output * rp; }
        }
        return [facOut, extOut, facAct, facBank, extAct, extBank];
    }
}
