using System.Runtime.CompilerServices;

namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (823) 銀の平価の注入の州の表 silverst_s と silverwt_s へ足す反実仮想の列（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝InstrumentsSilverWeight.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝(816) の候補 A′（薄い側の額＝参照価格建て）・A（局所の値段建て）で、旗 0 の盤面の注入を数え直し、入った州・出た州・票の上位の州を見る（挙動は変えない）。
// 取り方（事前登録 [4] の文から独立＝エンジンの関数も局所変数も受け取らない）：
//   ①<see cref="SilverstEnd"/>：注入の直後（助走の注入も含む）に呼ばれ、計器が sim.Nations × World.Regions を**自分で**走査して r.Markets の銀と金鉱を読む。
//      走査に入る州の条件（両方の市場が生きて価格が正）は計器が別に書く。エンジンの票は受け取らない（エンジンの値との突き合わせは silverwt_s の stateCountDiff・w1SumDiff・relW1ReadDiff）。
//   ②<see cref="SilverMintCapture"/>：MintGold が州の市場から引き取るたびに（州・金属・量）を渡す。量は計器の ConditionalWeakTable に溜める（Region に欄を足さない）。注入の直後に 0 へ戻す。
//   ③<see cref="SilverstClose"/>：試合の終わりに溜めた行を書く（SilverWeightClose の頭から呼ぶ）。
// ⚠限界（計器が自己申告する）：A・A′・max 版は計器の定義（反実仮想）でエンジンの規則ではない／dAu（LastDemand）は月の流れ・mintQty は前の注入からの和（単位が違う＝比べない）／
//   L は今の注入でも走査に現れる州だけ（国から消えた州は書かない）／行は試合の終わりに書く＝途中で落ちた試合は行が無い／票の和が 0 のとき rel は nan。
public static partial class Instruments
{
    internal const string SilverstHeaderText = "seq\tfrozen\tyear\tkind\trank\tnation\tregion\tlaw\tpAg\tpAu\tlnRel\tdAg\tdAu\tprodAg\tprodAu\tstockAg\tstockAu\tmintQtyAg\tmintQtyAu\tmintMonthsAg\tmintMonthsAu\tw1\twMinRef\twMinVal\tw1Share\tminRefShare\tminValShare\tinScan";

    /// <summary>州ごとの造幣の引き取り（前の注入から今まで）。</summary>
    sealed class SilverMintAcc { public double Ag, Au; public int MonthsAg, MonthsAu; }
    static readonly ConditionalWeakTable<Region, SilverMintAcc> SilverMintAccs = new();

    sealed class SilverstSink
    {
        public readonly List<string> Rows = [];
        public readonly HashSet<Region> Prev = new(ReferenceEqualityComparer.Instance);
        public bool HavePrev;
    }
    static readonly ConditionalWeakTable<MultiNationSim, SilverstSink> SilverstSinks = new();

    sealed class SilverstRec
    {
        public Region R = null!; public string Nation = ""; public int Law;
        public double PAg, PAu, DAg, DAu, ProdAg, ProdAu, StockAg, StockAu, MintAg, MintAu; public int MintMonthsAg, MintMonthsAu;
        public double W1, WRef, WVal, WMax; public bool InScan;
    }

    /// <summary>1 注入ぶんの自走査の集計（silverwt_s の末尾の列になる）。</summary>
    internal sealed class SilverstAgg
    {
        public int N, EnterCount, LeaveCount, ZeroRef, ZeroVal;
        public double W1Sum, RelW1, RelRef, RelVal, RelMax;
        public double MaxShareRef, MaxShareVal, MaxShareMax, OneRef, OneVal, OneMax;
        public double EnterEqual, EnterW1, EnterRef, EnterVal, EnterMax;
    }

    internal const string SilverstWeightTailHeader = "\trelMinRefIndex\trelMinValIndex\trelMaxRefIndex\tminRefMaxShare\tminValMaxShare\tmaxRefMaxShare\tminRefOneSidedShare\tminValOneSidedShare\tmaxRefOneSidedShare\tenterShareEqual\tenterShareW1\tenterShareMinRef\tenterShareMinVal\tenterShareMaxRef\tenterCount\tleaveCount\tminRefZeroCount\tminValZeroCount\tselfStateCount\tselfW1Sum\tselfRelW1Index\tstateCountDiff\tw1SumDiff\trelW1ReadDiff";

    /// <summary>票の 1 辺（非有限・非正は 0＝否定形で倒す）。</summary>
    static double SilverSide(double v) => v > 0 && v < double.PositiveInfinity ? v : 0.0;

    /// <summary>★(823) 薄い側の額の票＝2 辺（それぞれ額）の小さい方。非有限・非正の辺は 0（＝片側が無い州は票なし）。純関数（試験が手の値で読む）。</summary>
    internal static double SilverMinWeight(double sideAg, double sideAu) => Math.Min(SilverSide(sideAg), SilverSide(sideAu));

    /// <summary>造幣の引き取り（MintGold から州・金属・量）。銀と金鉱以外は溜めない。</summary>
    internal static void SilverMintCapture(Region r, int metal, double qty)
    {
        if (metal != Catalog.SilverId && metal != Catalog.GoldOreId) return;
        var a = SilverMintAccs.GetOrCreateValue(r);
        if (metal == Catalog.SilverId) { a.Ag += qty; a.MonthsAg++; } else { a.Au += qty; a.MonthsAu++; }
    }

    static double SilverstRel(double lnSum, double wSum, double refRatio) => wSum > 0 && refRatio > 1e-9 ? Math.Exp(lnSum / wSum) / refRatio : double.NaN;
    static double SilverstShare(double part, double wSum) => wSum > 0 ? part / wSum : double.NaN;

    /// <summary>注入の直後：計器が自分で全州を走査し、silverst_s の行を溜め、silverwt_s へ足す列の集計を返す。</summary>
    internal static SilverstAgg SilverstEnd(MultiNationSim sim, bool frozen, int seq)
    {
        var sk = SilverstSinks.GetValue(sim, _ => new SilverstSink());
        var ag = new SilverstAgg();
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId];
        double refRatio = refAu > 1e-9 ? refAg / refAu : double.NaN;
        double lnRefRatio = refRatio > 1e-9 ? Math.Log(refRatio) : double.NaN;
        int year = sim.Nations.Count > 0 ? sim.Nations[0].World.Year : 0;
        var scanned = new List<SilverstRec>(); var entered = new List<SilverstRec>(); var left = new List<SilverstRec>();
        // 形ごとの累積（0＝w1・1＝A′・2＝A・3＝max 版）
        var sumW = new double[4]; var sumWLn = new double[4]; var maxW = new double[4]; var oneW = new double[4]; var enterW = new double[4];
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nat = NameOf(w); int law = (int)w.MonetaryLaw;
            foreach (var r in w.Regions)
            {
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                bool live = (mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) && (mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9);
                bool inScan = live && mAg.Price > 1e-9 && mAu.Price > 1e-9;
                bool wasIn = sk.HavePrev && sk.Prev.Contains(r);
                var rec = new SilverstRec
                {
                    R = r, Nation = nat, Law = law, InScan = inScan,
                    PAg = mAg.Price, PAu = mAu.Price, DAg = mAg.LastDemand, DAu = mAu.LastDemand, ProdAg = mAg.LastProduced, ProdAu = mAu.LastProduced,
                    StockAg = mAg.Stock, StockAu = mAu.Stock,
                };
                if (SilverMintAccs.TryGetValue(r, out var acc))
                {
                    rec.MintAg = acc.Ag; rec.MintAu = acc.Au; rec.MintMonthsAg = acc.MonthsAg; rec.MintMonthsAu = acc.MonthsAu;
                    acc.Ag = 0; acc.Au = 0; acc.MonthsAg = 0; acc.MonthsAu = 0;   // 注入の直後に 0 へ戻す
                }
                rec.W1 = SilverSide(rec.DAu);
                rec.WRef = SilverMinWeight(rec.DAg * refAg, rec.DAu * refAu);
                rec.WVal = SilverMinWeight(rec.PAg * rec.DAg, rec.PAu * rec.DAu);
                rec.WMax = Math.Max(SilverSide(rec.DAg * refAg), SilverSide(rec.DAu * refAu));
                if (!inScan) { if (wasIn) left.Add(rec); continue; }
                scanned.Add(rec);
                bool isEnter = sk.HavePrev && !wasIn;
                if (isEnter) entered.Add(rec);
                double ln = Math.Log(rec.PAg / rec.PAu);
                bool oneSided = rec.DAu < 0.01 * rec.ProdAu;
                var ws = new[] { rec.W1, rec.WRef, rec.WVal, rec.WMax };
                for (int f = 0; f < 4; f++)
                {
                    sumW[f] += ws[f]; sumWLn[f] += ws[f] * ln; maxW[f] = Math.Max(maxW[f], ws[f]);
                    if (oneSided) oneW[f] += ws[f];
                    if (isEnter) enterW[f] += ws[f];
                }
            }
        }
        ag.N = scanned.Count; ag.EnterCount = entered.Count; ag.LeaveCount = left.Count;
        foreach (var s in scanned) { if (!(s.WRef > 0)) ag.ZeroRef++; if (!(s.WVal > 0)) ag.ZeroVal++; }
        ag.W1Sum = sumW[0];
        ag.RelW1 = SilverstRel(sumWLn[0], sumW[0], refRatio); ag.RelRef = SilverstRel(sumWLn[1], sumW[1], refRatio);
        ag.RelVal = SilverstRel(sumWLn[2], sumW[2], refRatio); ag.RelMax = SilverstRel(sumWLn[3], sumW[3], refRatio);
        ag.MaxShareRef = SilverstShare(maxW[1], sumW[1]); ag.MaxShareVal = SilverstShare(maxW[2], sumW[2]); ag.MaxShareMax = SilverstShare(maxW[3], sumW[3]);
        ag.OneRef = SilverstShare(oneW[1], sumW[1]); ag.OneVal = SilverstShare(oneW[2], sumW[2]); ag.OneMax = SilverstShare(oneW[3], sumW[3]);
        ag.EnterEqual = sk.HavePrev && ag.N > 0 ? (double)ag.EnterCount / ag.N : double.NaN;
        ag.EnterW1 = sk.HavePrev ? SilverstShare(enterW[0], sumW[0]) : double.NaN; ag.EnterRef = sk.HavePrev ? SilverstShare(enterW[1], sumW[1]) : double.NaN;
        ag.EnterVal = sk.HavePrev ? SilverstShare(enterW[2], sumW[2]) : double.NaN; ag.EnterMax = sk.HavePrev ? SilverstShare(enterW[3], sumW[3]) : double.NaN;

        string R(double v) => SilverWeightR(v);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        void Row(string kind, int rank, SilverstRec x, bool shares)
        {
            double lnRel = x.PAg > 0 && x.PAu > 0 && x.PAg < double.PositiveInfinity && x.PAu < double.PositiveInfinity ? Math.Log(x.PAg / x.PAu) - lnRefRatio : double.NaN;
            sk.Rows.Add(string.Join('\t', I(seq), frozen ? "1" : "0", I(year), kind, I(rank), x.Nation, x.R.Name, I(x.Law),
                R(x.PAg), R(x.PAu), R(lnRel), R(x.DAg), R(x.DAu), R(x.ProdAg), R(x.ProdAu), R(x.StockAg), R(x.StockAu),
                R(x.MintAg), R(x.MintAu), I(x.MintMonthsAg), I(x.MintMonthsAu), R(x.W1), R(x.WRef), R(x.WVal),
                R(shares ? SilverstShare(x.W1, sumW[0]) : double.NaN), R(shares ? SilverstShare(x.WRef, sumW[1]) : double.NaN), R(shares ? SilverstShare(x.WVal, sumW[2]) : double.NaN),
                x.InScan ? "1" : "0"));
        }
        if (sk.HavePrev)
        {
            foreach (var x in entered) Row("E", 0, x, true);
            foreach (var x in left) Row("L", 0, x, false);
        }
        void Top(string kind, Func<SilverstRec, double> weight)
        {
            int rank = 0;
            foreach (var x in scanned.Where(s => weight(s) > 0).OrderByDescending(weight).Take(5)) Row(kind, ++rank, x, true);   // 安定な並べ（同点は走査順）
        }
        Top("T1", s => s.W1); Top("TR", s => s.WRef); Top("TV", s => s.WVal);

        sk.Prev.Clear(); foreach (var s in scanned) sk.Prev.Add(s.R);
        sk.HavePrev = true;
        return ag;
    }

    /// <summary>試合の終わり：溜めた行を書く。</summary>
    static void SilverstClose(MultiNationSim sim, ulong seed)
    {
        if (!SilverstSinks.TryGetValue(sim, out var sk) || sk.Rows.Count == 0) return;
        using var wr = Open("silverst", seed);
        wr.WriteLine(SilverstHeaderText);
        foreach (var row in sk.Rows) wr.WriteLine(row);
        sk.Rows.Clear();
    }
}
