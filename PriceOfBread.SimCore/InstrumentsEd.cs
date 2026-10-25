namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (452) 国家の教育の需要の表 ed_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsMed.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。旗 StateEduDemandInFront に依らず出す（腕 A でも買いたい分と取れた分を読める）。
// 目的＝事前登録 measurements/ed452_20261020_prereg.txt の狙い a0〜a2・a5（場面の割合・買えた割合・値段）と付記（教育の産出・売れた量・需要の信号）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。値は州の和（断りがなければ）で、直近の月の値（EconomyStep が INS=1 の月に上書きする計器の側の蓄え EdDiag〔(465) で Region の 7 欄から移した〕）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・PerMonth＝月あたりの量〔数量は財の単位・Money は国の通貨〕・K＝千人）：
//   popK／fundingShare（CompulsoryEduFunding の人口加重＝国庫が未充足の教育を買う率）／fundedRegionShare（その率が正の州の人口の割合）／
//   needPerMonth（教育の必要量〈非兵士の GoodNeed の和〉）／privateDemandPerMonth（民間の買いたい量〈非兵士の GoodDemand の和〉）／availablePerMonth（供給＝在庫＋産出）／
//   stateWantPerMonth（国庫の買いたい量）／stateTakePerMonth（取れた量）／stateWantMoneyPerMonth・stateTakeMoneyPerMonth（それぞれ×その州の教育の値段）／
//   shortRegionShare（率が正の州のうち買いたい量が取れた量を〈1e-9 の相対と 1e-12 の絶対〉の外で超えた州の人口の割合＝供給が尽きた州）／
//   demandSignalPerMonth（教育の市場の LastDemand の和＝値段が読む需要）／producedPerMonth・soldPerMonth（LastProduced・LastSold の和）／
//   priceMean（教育の値段の人口加重）／priceOverRefTimesLevelIndex（値段÷（参照価格×物価水準）の人口加重）／
//   higherWantPerMonth・higherTakePerMonth（高等教育の補助の買いたい量・取れた量）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（月ごとの値は読まない）／診断の欄は直近の INS=1 の月の 1 回の値／助走の月と死んだ州は 0。
public static partial class Instruments
{
    internal const string EdHeaderText = "quarter\tyear\tnation\tpopK\tfundingShare\tfundedRegionShare\tneedPerMonth\tprivateDemandPerMonth\tavailablePerMonth\tstateWantPerMonth\tstateTakePerMonth\t"
        + "stateWantMoneyPerMonth\tstateTakeMoneyPerMonth\tshortRegionShare\tdemandSignalPerMonth\tproducedPerMonth\tsoldPerMonth\tpriceMean\tpriceOverRefTimesLevelIndex\thigherWantPerMonth\thigherTakePerMonth";

    /// <summary>(465) 診断の蓄え（(452) で Region に置いた 7 欄を計器の側へ移した＝シミュの状態ではない・セーブに載らない・ConditionalWeakTable の先例）。
    /// 書くのは EconomyStep の INS=1 の月だけ・読むのは EdFold だけ。値は直近の月の 1 回（非兵士の和）。書かれていない州は全て 0。</summary>
    internal sealed class EdDiag { public double Need, Priv, Avail, Want, Take, HigherWant, HigherTake; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, EdDiag> EdDiags = new();
    static readonly EdDiag EdZero = new();
    internal static EdDiag EdDiagOf(Region r) => EdDiags.TryGetValue(r, out var d) ? d : EdZero;
    /// <summary>教育の月の始め：必要量・民間の買いたい量・供給を置き、国庫の買いたい分と取れた分を 0 に戻す。</summary>
    internal static void EdBegin(Region r, double need, double priv, double avail) { var d = EdDiags.GetOrCreateValue(r); d.Need = need; d.Priv = priv; d.Avail = avail; d.Want = 0; d.Take = 0; }
    internal static void EdSet(Region r, double want, double take) { var d = EdDiags.GetOrCreateValue(r); d.Want = want; d.Take = take; }
    internal static void HigherEdSet(Region r, double want, double take) { var d = EdDiags.GetOrCreateValue(r); d.HigherWant = want; d.HigherTake = take; }

    static void EdFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        double popK = 0, fundW = 0, fundedPop = 0, shortPop = 0, need = 0, priv = 0, avail = 0, want = 0, take = 0, wantMoney = 0, takeMoney = 0, demand = 0, produced = 0, sold = 0, priceW = 0, relW = 0, priceDen = 0, hWant = 0, hTake = 0;
        bool haveEdu = Catalog.EducationId < Catalog.RefPrices.Length;
        double refP = haveEdu ? Catalog.RefPrices[Catalog.EducationId] : double.NaN;
        foreach (var r in w.Regions)
        {
            double rp = r.SumPopulation();
            var d = EdDiagOf(r);
            popK += rp; fundW += rp * r.CompulsoryEduFunding;
            if (r.CompulsoryEduFunding > 1e-9)
            {
                fundedPop += rp;
                if (d.Want > d.Take * (1.0 + 1e-9) + 1e-12) shortPop += rp;   // 否定形でなく読むだけの診断＝NaN は数えない側
            }
            need += d.Need; priv += d.Priv; avail += d.Avail; want += d.Want; take += d.Take; hWant += d.HigherWant; hTake += d.HigherTake;
            if (!haveEdu || Catalog.EducationId >= r.Markets.Length) continue;
            var m = r.Markets[Catalog.EducationId];
            demand += m.LastDemand; produced += m.LastProduced; sold += m.LastSold;
            wantMoney += d.Want * m.Price; takeMoney += d.Take * m.Price;
            if (rp > 0)
            {
                priceW += m.Price * rp; priceDen += rp;
                double den = refP * r.PriceLevel; if (den > 1e-12) relW += m.Price / den * rp;
            }
        }
        double Div(double a, double b) => b > 0 ? a / b : double.NaN;
        sk.Ed ??= Open("ed", seed);
        if (!sk.EdHeader) { sk.Ed.WriteLine(EdHeaderText); sk.EdHeader = true; }
        sk.Ed.WriteLine(string.Join('\t', q.ToString(System.Globalization.CultureInfo.InvariantCulture), w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), F(popK), F(Div(fundW, popK)), F(Div(fundedPop, popK)),
            F(need), F(priv), F(avail), F(want), F(take), F(wantMoney), F(takeMoney), F(Div(shortPop, fundedPop)), F(demand), F(produced), F(sold), F(Div(priceW, priceDen)), F(Div(relW, priceDen)), F(hWant), F(hTake)));
    }
}
