namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (471) 小さな独立の直し 4 件の表 sf_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCa.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。旗に依らず出す（腕 A′ でも基準を読める）。
// 目的＝事前登録 measurements/sf471_20261020_prereg.txt の G0〜G4（兵士の年金の受取・〈安く売る〉の輸出・議席の重み・負の予算）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。
// 列（単位は接尾辞：K＝千人・Value＝金額〔国の通貨〕・Count＝件数・Cum＝助走を含む累計・Weight＝票の重み〔千人×発言権〕）：
//   pensionLevel（年金の法の段 0／0.5／1）／elder60K・soldierElder60K（兵士以外・兵士の 60 歳以上）／pensionToSoldiersCum（兵士へ届いた年金の累計・旗 0 は 0）／
//   cheapGoodCount・dearGoodCount（売買の方針が −1・+1 の財の数）／cheapExportValue（−1 の財の ExportsByGood の和＝直近の清算の越境の輸出額・金建て）／cheapProducedRefValue（−1 の財の州の LastProduced×RefPrice の和）／cheapSubsidyPaidCum（売り手の国庫が払った奨励金の累計・旗 0 は 0）／
//   voteWeightAll・voteWeightScoped（Σ size×FranchiseVoice と、それに SuffrageInclusion を掛けた和＝旗に依らず同じ式）／voteWeightUsed（直近の ComputeVotes が使った重みの合計）／colonialVoteWeight（植民地格の州の FranchiseVoice の重み）／
//   budgetNegCumPopMonthK・budgetNegCumAmount（消費の財のループの直前に残りの予算が負だった POP×月の人口〔千〕と額の累計・床の直前に撮る＝旗に依らない）。
// ⚠限界（計器が自己申告する）：①四半期の末の断面と累計・②cheapExportValue は直近の清算の 1 回・③voteWeightUsed は直近の ComputeVotes（毎期）・④budgetNeg は助走の月も数える累計（差を取って読む）・⑤旗 1 では奨励金の分だけ買い手の支払いが減る（輸入額の統計は積出し価格のまま）。
public static partial class Instruments
{
    internal const string SfHeaderText = "quarter\tyear\tnation\tpensionLevel\telder60K\tsoldierElder60K\tpensionToSoldiersCum\t"
        + "cheapGoodCount\tdearGoodCount\tcheapExportValue\tcheapProducedRefValue\tcheapSubsidyPaidCum\t"
        + "voteWeightAll\tvoteWeightScoped\tvoteWeightUsed\tcolonialVoteWeight\tbudgetNegCumPopMonthK\tbudgetNegCumAmount";

    /// <summary>国の sf_s の行の値（列の順＝<see cref="SfHeaderText"/> の pensionLevel 以後）。番人（テスト）が世界から直に呼べるよう値の組で返す。</summary>
    internal static double[] SfRow(World w)
    {
        double level = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double elder = 0, soldierElder = 0, all = 0, scoped = 0, colonial = 0;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            double os = p.Size * p.AgeShare(60, double.PositiveInfinity);
            if (p.IsSoldier) soldierElder += os; else elder += os;
            double v = p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            all += v; scoped += v * InterestGroups.SuffrageInclusion(w, rg, p);
            if (rg.Tier == RegionTier.Colonial) colonial += v;
        }
        int ng = Catalog.Goods.Length;
        double cheapN = 0, dearN = 0, cheapExp = 0, cheapProd = 0;
        for (int g = 0; g < ng; g++)
        {
            int bias = w.TradeBiasOf(g);
            if (bias > 0) dearN++;
            else if (bias < 0)
            {
                cheapN++;
                if (w.ExportsByGood is { } eg && g < eg.Length) cheapExp += eg[g];
                double rp = g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0.0;
                foreach (var r in w.Regions)
                    if (r.State == RegionState.Active && g < r.Markets.Length) cheapProd += r.Markets[g].LastProduced * rp;
            }
        }
        return [level, elder, soldierElder, w.DiagSfPensionToSoldiersCum, cheapN, dearN, cheapExp, cheapProd, w.DiagSfCheapSubsidyPaidCum,
                all, scoped, w.DiagSfVoteWeightUsed, colonial, w.DiagSfBudgetNegCumPopMonthK, w.DiagSfBudgetNegCumAmount];
    }

    static void SfFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var v = SfRow(w);
        sk.Sf ??= Open("sf", seed);
        if (!sk.SfHeader) { sk.Sf.WriteLine(SfHeaderText); sk.SfHeader = true; }
        var cells = new string[3 + v.Length];
        cells[0] = q.ToString(System.Globalization.CultureInfo.InvariantCulture); cells[1] = w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture); cells[2] = NameOf(w);
        for (int i = 0; i < v.Length; i++) cells[3 + i] = F(v[i]);
        sk.Sf.WriteLine(string.Join('\t', cells));
    }
}
