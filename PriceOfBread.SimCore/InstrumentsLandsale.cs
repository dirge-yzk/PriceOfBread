using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (983) 土地の持ち分の段 B-1 の段 0＝恒等の計器 landsale_s（読むだけ・恒等・事前登録 measurements/m983_20261109_prereg_ls.txt） -------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandshare.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（INS のときだけの World ごとの積み＝ConditionalWeakTable）。
// 目的＝土地税が払えない分（POP の貯蓄が請求に足りない月の未払い）が、売り（B-1）の動機として十分な大きさか（no-go）と、土地税の写しがエンジンの取れた額と向きを違えないか（恒等）を、段 1 の器を入れる前に測る。
// 受け口＝Fiscal.Step の徴税の走査の直前（徴税の前の貯蓄・前の月の所得・州の届き Rule／AdminFill を計器が自分で読む）＝LandsaleStep。月の閉じ＝Instruments.MonthClose（LandshareMonth の前＝輸出の地代の月の積みが 0 へ戻る前）＝LandsaleMonth。
// 計器が自分で組む物（エンジンの合成を呼ばない）：届き（RuleDelivery の写し）・仲介者の取り分（TaxScan.Of の写し）・4 種の請求（AssessPop の合成の写し）・払える割合（PayRate の写し）・十分の一税の請求（TitheClaim の写し）・土地税の基数（LandTaxBase の写し）。
//   読むだけの純関数／状態として呼ぶ物：Privileges.TaxExemptionDegree（免税の度合い）・InterestGroups.FranchiseVoice／SuffrageInclusion（同意の声）・Privileges.TitheDegree・Politics.IsMassPop・PowerAffinity.Shares（王権の取り分）。
//   既存の写しの再利用：LsRecipient（枠の受け手の添字・寄せの回数）・LsClass（階級の 5 区分）・LsAbsenteeRgo（不在地主の RGO）・LsAccs（輸出の地代の月の積み MExp）。
// 売りの期待（段 1 が実装したら起きるはずの量＝計器が自分で数える）：売り手＝rp＜1 の請求があり土地税の未払い（kLand×届き×(1−rp)）が正の POP。買い手＝売り手の持ち分 h ごとに、枠 k＜4 の受け手が実在し（寄せの枠を外す）売り手と別の POP である枠。
//   値段の地代＝売り手の LandRent−土地税率×基数×（買い手の貯蓄で重み付けた免税の係数）の正の部。年に手放す割合の期待 saleExpectedSharePerYear＝Σ 未払い×国債金利÷Σ 純の地代（四半期の和の比）。
// ⚠限界（計器が自己申告する）：
//   ①rp は走査の開始時点の貯蓄で数える＝走査の途中で前の POP の仲介者の取り分が受け手（領主）の貯蓄に入る＝計器の rp は下界・未払いは上界（Fiscal.cs の (490) の注と同じ型）。
//   ②値段の地代は国内の地代（LandRent）だけ＝輸出の地代は入れない（課税の基数と同じ集合・分子と分母が同じ集合。輸出の地代の月の和は exportRentPerQuarter に記録だけ）＝輸出作物の産地の持ち分は期待の上で安く見積もられない側（純の地代が小さい側＝割合は上界）。
//   ③買い手の税の係数は買い手の貯蓄の加重平均を h をまたいで 1 つに畳む（h ごとの max(0,地代−税) の和でなく和の max）＝割合は下界側にずれうる。
//   ④欄が null の RGO は [1,0,0,0] として売り手の持ち分に数える（段 1 の決め事は構築の除外を守るか段 1 で決める＝低⑨）。不在地主の RGO は持ち分から外す。
//   ⑤純の地代が 0 以下（!(P>0)）か国債金利が正でない・買い手の貯蓄が 0 の売り手は期待に入れず saleBlocked* に別に積む（未払いが消える端＝低⑪）。
//   ⑥法の門（農奴解放 ≥ 限定的）は期待に掛けず saleLawOpenFlag（四半期の最後の月の値）に分けて記録。
//   ⑦助走の月（World.WarmupFrozen）・Fiscal が無効の月は数えない。
//   ⑧★(995) 公売の払いの写し（LandsaleSaleBegin）は買い手ごとの額の和にエンジンの床を入れる（保有が 1e-9 以下・額が床〔TakeFloorPositive＞0 なら 0・でなければ 1e-12〕以下は 0）。エンジンの関数は呼ばない。
//     写しの重み〔Closed && !IsSoldier ? 1−免税の度合い : 1〕は Privileges.TaxAssessment（同じ式）・写しの基数は Fiscal.LandTaxBase（同じ分岐）と今の盤面で同じ値（紙の確かめ＝事前登録 m995 [1]）。
//     既知の限界：免税の度合いは月の走査の頭の取り（走査の途中で入力が変わる盤面では不一致）・特権の免税（PrivilegeTaxDiscount＞0）や φ*≠0/1 の盤面は同値の外に出うる・買い手の列挙は SellOne と別実装。
public static partial class Instruments
{
    const int LdNC = 5;
    /// <summary>私の枠の数（k＜4）。5 個目（国家の枠）は国庫へ入った後の持ち分で売買の対象外。</summary>
    internal const int LdPrivateSlots = 4;
    internal const double LandsaleGateTol = 1e-9;

    internal const string LandsaleHeaderText = "quarter\tyear\tnation\tmonthsInQuarter\tscanMonths\tpopsScanned\trp1NationMonths\tlowRpNationMonths\t"
        + "unpaidLandPerQuarter\tunpaidLandNoblePerQuarter\tunpaidLandClergyPerQuarter\tunpaidLandPeasantPerQuarter\tunpaidLandCapitalistPerQuarter\tunpaidLandOtherPerQuarter\tunpaidHolderCount\t"
        + "landClaimDeliveredPerQuarter\tpaidCopyPerQuarter\tengineLandTaxPerQuarter\tpaidCopyOverEngine\tpaidCopyRp1RelDiff\t"
        + "saleExpectedSharePerYear\tsaleIncludedUnpaidPerQuarter\tsaleNetRentPerQuarter\tsaleBlockedNoBuyerPerQuarter\tsaleBlockedNoPricePerQuarter\tsaleLawOpenFlag\tbuyerSavingsCoverShare\tbuyerCountSum\tbuyerFallbackSlotCount\t"
        + "sellerLandRentPerQuarter\texportRentPerQuarter\t"
        + "saleActualPaidPerQuarter\tsaleCopyPaidPerQuarter\tsaleEventCount\tsalePaidCopyMaxRelDiff\tsaleSlotSumMaxAbsDev\tsaleHeldDropSum\tsalePhiSum\tsaleActualShareNetSum\tsaleActualNetSum\tsaleBuyerCount\tsaleBuyerNonInvestorCount";   // ★(991) 末尾の 11 列＝実際の公売（LandsaleSaleBegin／End）

    /// <summary>[0,1] へ閉じる（計器の写し・NaN は 0）。</summary>
    internal static double LdClamp01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;

    /// <summary>払える割合（PayRate の計器の写し）：貯蓄の正の部÷請求の和（足りれば 1）。</summary>
    internal static double LdRp(double savings, double claims)
    {
        double held = System.Math.Max(0, savings);
        return held >= claims ? 1.0 : held / claims;
    }

    /// <summary>国庫へ届く割合（RuleDelivery の計器の写し）。Rule が 4 本でなければ 1。範囲の外・NaN は 1。</summary>
    internal static double LdMul(double[] rule, double adminFill, double discount)
    {
        if (rule.Length != 4) return 1.0;
        double d = 1.0 - LdClamp01(discount), f = LdClamp01(adminFill);
        double v = LdClamp01(rule[0]) * (d + (1.0 - d) * f) + LdClamp01(rule[1]) * d;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    /// <summary>仲介者の取り分の率（TaxScan.Of の計器の写し）。Rule が 4 本でなければ 0。</summary>
    internal static double LdCutFrac(double[] rule, double adminFill, double discount)
    {
        if (rule.Length != 4) return 0;
        double d = 1.0 - LdClamp01(discount), f = LdClamp01(adminFill);
        return (LdClamp01(rule[1]) + LdClamp01(rule[0]) * (1.0 - f)) * (1.0 - d);
    }

    /// <summary>売り手が持ち分 h を持つ枠の数＝私の枠の数（長さ 5 の欄でも 4・null は 1 組の [1,0,0,0] 扱いで 4）。</summary>
    internal static int LdSlotCount(double[]? ls) => System.Math.Min(LdPrivateSlots, ls?.Length ?? LdPrivateSlots);

    /// <summary>売り手 <paramref name="seller"/> が RGO <paramref name="s"/> の持ち分を持つか（枠 k＜4 のどれかの受け手がその POP で重みが正）。null の欄は持ち主が全部。</summary>
    internal static bool LdHolds(Region r, Producer s, int seller)
    {
        var ls = s.LandShares;
        long fb = 0;
        if (ls is null) return s.OwnerPopIndex == seller;
        for (int k = 0, nk = LdSlotCount(ls); k < nk; k++)
            if (ls[k] > 0 && LsRecipient(r, s, k, ref fb) == seller) return true;
        return false;
    }

    /// <summary>RGO <paramref name="s"/> の売り手以外の枠の受け手（買い手）を <paramref name="into"/> へ足す（POP の重複は足さない）。枠は k＜4・受け手が実在しない（寄せの）枠は買い手の枠に入れず <paramref name="fallback"/> に数える。
    /// 戻り値＝買い手の枠の数（実在する受け手で売り手と別の POP の枠。同じ POP が 2 枠なら 2）。</summary>
    internal static int LdBuyersOf(Region r, Producer s, int seller, List<int> into, ref long fallback)
    {
        int slots = 0;
        for (int k = 0; k < LdPrivateSlots; k++)
        {
            long f0 = fallback;
            int i = LsRecipient(r, s, k, ref fallback);
            if (fallback > f0) continue;   // 寄せの枠（受け手が実在しない）は買い手に入れない
            if (i == seller) continue;
            slots++;
            if (!into.Contains(i)) into.Add(i);
        }
        return slots;
    }

    static double LdMaxNan(double cur, double v) => double.IsNaN(cur) ? cur : double.IsNaN(v) || v > cur ? v : cur;

    sealed class LdAcc
    {
        public bool Armed;
        // 月の積み
        public double MUnpaid, MPaidCopy, MClaimDelivered, MSellerRent, MInclUnpaid, MInclNetRent, MInclRate, MBlockNoBuyer, MBlockNoPrice, MCoverSavings, MExport;
        public readonly double[] MUnpaidByClass = new double[LdNC];
        public long MHolders, MPops, MLowRp, MLandLow, MBuyerCount, MFallback;
        // 四半期の和
        public double QUnpaid, QPaidCopy, QClaimDelivered, QEngine, QSellerRent, QInclUnpaid, QInclNetRent, QInclRate, QBlockNoBuyer, QBlockNoPrice, QCoverSavings, QExport;
        public readonly double[] QUnpaidByClass = new double[LdNC];
        public long QHolders, QPops, QBuyerCount, QFallback; public int Months, ScanMonths, Rp1Months, LowRpMonths; public bool Law;
        public double MaxOver, MaxRp1Rel;
        // ★(991) 実際の公売（受け口 LandsaleSaleBegin／End）：月の未払いの写し（売り手→V）・免税の度合い・Begin の写し・四半期の和
        public readonly Dictionary<Pop, double> Owed = new(ReferenceEqualityComparer.Instance);
        public double ExemptDeg;
        public bool Pend; public double PPrice, PNet, PCopy; public double[] PHeld = []; public readonly List<double> PSlot = [];
        public double QSaleActual, QSaleCopy, QSaleMaxRel, QSaleSlotDev, QSaleDrop, QSalePhi, QSaleShareNet, QSaleNet;
        public long QSaleEvents, QSaleBuyers, QSaleBuyersNonInv;
    }

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, LdAcc> LdAccs = new();

    /// <summary>受け口（Fiscal.Step の徴税の走査の直前・INS のときだけ＝On の確かめは中）：全ての POP の請求と払える割合を計器が自分で組み、未払いと売りの期待を月の積みへ足す。お金は動かさない。</summary>
    internal static void LandsaleStep(World w)
    {
        if (!On || w.WarmupFrozen || w.Regions.Count == 0) return;
        var a = LdAccs.GetValue(w, _ => new LdAcc());
        a.Armed = true;
        a.MUnpaid = a.MPaidCopy = a.MClaimDelivered = a.MSellerRent = a.MInclUnpaid = a.MInclNetRent = a.MInclRate = a.MBlockNoBuyer = a.MBlockNoPrice = a.MCoverSavings = 0;
        System.Array.Clear(a.MUnpaidByClass);
        a.MHolders = a.MPops = a.MLowRp = a.MLandLow = a.MBuyerCount = a.MFallback = 0;
        a.Owed.Clear();   // ★(991)
        var c = w.Coef;
        double discount = c.PrivilegeTaxDiscount.Value;
        double crownShare = c.PrivilegeFromCrownShare.Value > 0 && c.PrivilegeTaxDiscount.Value > 0 ? PowerAffinity.Shares(w)[0] : 0;
        double exemptDeg = Privileges.TaxExemptionDegree(w, c, crownShare);
        a.ExemptDeg = exemptDeg;   // ★(991) 公売の写しの買い手の免税
        double PB(Pop p) => p.Closed && !p.IsSoldier ? 1.0 - exemptDeg : 1.0;
        double titheRate = c.TitheRate.Value * Privileges.TitheDegree(w, c);
        bool clergy = false;
        if (titheRate > 1e-9) foreach (var r0 in w.Regions) { foreach (var p0 in r0.Pops) if (p0.ClassId == PopClass.Clergy && p0.Size > 1e-9) { clergy = true; break; } if (clergy) break; }
        double rate = w.GovBondRatePerYear.Value;
        bool scopeOn = c.TaxConsentWeight > 0;
        var buyers = new List<int>();
        foreach (var r in w.Regions)
        {
            Pop? lord = null;
            foreach (var lp in r.Pops) if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9) { lord = lp; break; }
            double mul = LdMul(r.Rule, r.AdminFill, discount);
            double cutFrac = lord is not null ? LdCutFrac(r.Rule, r.AdminFill, discount) : 0;
            for (int pi = 0; pi < r.Pops.Count; pi++)
            {
                var p = r.Pops[pi];
                if (p.IsCriminal) continue;
                double pb = PB(p), consent = 1.0;
                if (c.TaxConsentWeight > 0)
                {
                    double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, c);
                    if (scopeOn) voice *= InterestGroups.SuffrageInclusion(w, r, p);
                    consent = 1.0 - c.TaxConsentWeight * (1.0 - voice);
                }
                double earned = System.Math.Max(0, p.LastEarnedPerMonth.Value);
                double kInc = w.TaxRate.Value * earned * pb * consent, kProg = 0, kPoll = 0, kLand = 0;
                if (w.ProgressiveTax > 1e-9 && (p.IsModernElite || p.Closed) && !p.IsSoldier) kProg = w.ProgressiveTax * earned * consent;
                if (w.PollTax > 1e-9 && p.IsCommoner) kPoll = c.PollTaxRate * w.PollTax * p.Size;
                double landBase = c.LandTaxFixedBasis > 0 ? p.LandRentPerMonth.Value + p.LandTaxBasisAdjPerMonth.Value : p.LandRentPerMonth.Value;
                if (w.LandTax > 1e-9 && landBase > 0) kLand = w.LandTax * landBase * pb;
                double full = kInc + kProg + kPoll + kLand;
                double toState = full * mul;
                double cut = lord is not null && !ReferenceEquals(lord, p) && full * cutFrac > 1e-12 ? full * cutFrac : 0;
                double tithe = clergy && Politics.IsMassPop(p) && p.LastEarnedPerMonth.Value > 0 ? titheRate * p.LastEarnedPerMonth.Value : 0;
                double claims = toState + cut + tithe;
                if (!(claims > 0)) continue;
                double rp = LdRp(p.Savings.Value, claims);
                a.MPops++;
                if (rp < 1.0) a.MLowRp++;
                double claimLand = kLand * mul;
                if (claimLand > 0 && rp < 1.0) a.MLandLow++;
                a.MClaimDelivered += claimLand;
                a.MPaidCopy += claimLand * rp;
                double unpaid = claimLand * (1.0 - rp);
                if (!(unpaid > 0)) continue;
                // 売り手：未払いが正の POP
                a.MUnpaid += unpaid; a.MUnpaidByClass[LsClass(p.ClassId)] += unpaid; a.MHolders++;
                a.Owed[p] = unpaid;   // ★(991) 公売の写しの V（徴税の前の貯蓄で数えた未払い）
                a.MSellerRent += p.LandRentPerMonth.Value;
                buyers.Clear();
                foreach (var s in r.Producers)
                {
                    if (!s.IsRgo || LsAbsenteeRgo(r, s) || !LdHolds(r, s, pi)) continue;
                    long fb = 0;
                    a.MBuyerCount += LdBuyersOf(r, s, pi, buyers, ref fb);
                    a.MFallback += fb;
                }
                double sumS = 0, sumSpb = 0;
                foreach (var j in buyers) { double sj = System.Math.Max(0, r.Pops[j].Savings.Value); sumS += sj; sumSpb += sj * PB(r.Pops[j]); }
                if (!(sumS > 0)) { a.MBlockNoBuyer += unpaid; continue; }
                double pbAvg = sumSpb / sumS;
                double net = System.Math.Max(0, p.LandRentPerMonth.Value - w.LandTax * landBase * pbAvg);
                if (!(net > 0) || !(rate > 0)) { a.MBlockNoPrice += unpaid; continue; }
                a.MInclUnpaid += unpaid; a.MInclNetRent += net; a.MInclRate += unpaid * rate; a.MCoverSavings += sumS;
            }
        }
    }

    /// <summary>月の閉じ（Instruments.MonthClose・Fiscal.Step の後・LandshareMonth の前）：エンジンの取れた土地税と比べ、四半期の和へ足す。</summary>
    internal static void LandsaleMonth(World w)
    {
        if (!LdAccs.TryGetValue(w, out var a)) return;
        a.Months++;
        if (!a.Armed) return;
        a.Armed = false;
        double eng = w.LastTaxLandPerMonth.Value, copy = a.MPaidCopy;
        double over = !(copy > 0) ? 0.0 : eng > 0 ? copy / eng : double.PositiveInfinity;
        a.MaxOver = LdMaxNan(a.MaxOver, over);
        if (a.MLandLow == 0 && a.MClaimDelivered > 0)   // 土地税を払う POP の rp が全て 1 の月（エンジンの rp は下界の計器の rp 以上＝1 でなければならない）＝写しは取れた額と等しいはず
        {
            a.Rp1Months++;
            double d = System.Math.Abs(copy - eng), den = System.Math.Max(System.Math.Abs(copy), System.Math.Abs(eng));
            a.MaxRp1Rel = LdMaxNan(a.MaxRp1Rel, d == 0 ? 0.0 : den > 0 ? d / den : double.NaN);
        }
        else a.LowRpMonths++;
        a.ScanMonths++;
        a.QUnpaid += a.MUnpaid; a.QPaidCopy += copy; a.QClaimDelivered += a.MClaimDelivered; a.QEngine += eng; a.QSellerRent += a.MSellerRent;
        a.QInclUnpaid += a.MInclUnpaid; a.QInclNetRent += a.MInclNetRent; a.QInclRate += a.MInclRate; a.QBlockNoBuyer += a.MBlockNoBuyer; a.QBlockNoPrice += a.MBlockNoPrice; a.QCoverSavings += a.MCoverSavings;
        for (int k = 0; k < LdNC; k++) a.QUnpaidByClass[k] += a.MUnpaidByClass[k];
        a.QHolders += a.MHolders; a.QPops += a.MPops; a.QBuyerCount += a.MBuyerCount; a.QFallback += a.MFallback;
        a.Law = w.SerfEmancipation >= ReformLevel.Limited;
        if (LsAccs.TryGetValue(w, out var ls)) a.QExport += ls.MExp;   // 輸出の地代の月の積み（LandshareMonth が 0 へ戻す前）
    }

    sealed class LandsaleSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LandsaleSink> LandsaleSinks = new();

    /// <summary>四半期の末：全ての国の行を書き、四半期の和を 0 へ戻す。</summary>
    static void LandsaleTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = LandsaleSinks.GetValue(sim, _ => new LandsaleSink());
        sk.W ??= Open("landsale", seed);
        if (!sk.Header) { sk.W.WriteLine(LandsaleHeaderText); sk.Header = true; }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var a = LdAccs.GetValue(w, _ => new LdAcc());
            double share = a.QInclNetRent > 0 ? a.QInclRate / a.QInclNetRent : 0.0;
            double cover = a.QInclUnpaid > 0 ? a.QCoverSavings / a.QInclUnpaid : 0.0;
            var v = new List<string>
            {
                I(q), I(w.Year), NameOf(w), I(a.Months), I(a.ScanMonths), I(a.QPops), I(a.Rp1Months), I(a.LowRpMonths),
                F(a.QUnpaid), F(a.QUnpaidByClass[0]), F(a.QUnpaidByClass[1]), F(a.QUnpaidByClass[2]), F(a.QUnpaidByClass[3]), F(a.QUnpaidByClass[4]), I(a.QHolders),
                F(a.QClaimDelivered), F(a.QPaidCopy), F(a.QEngine), F(a.MaxOver), F(a.MaxRp1Rel),
                F(share), F(a.QInclUnpaid), F(a.QInclNetRent), F(a.QBlockNoBuyer), F(a.QBlockNoPrice), a.Law ? "1" : "0", F(cover), I(a.QBuyerCount), I(a.QFallback),
                F(a.QSellerRent), F(a.QExport),
                F(a.QSaleActual), F(a.QSaleCopy), I(a.QSaleEvents), F(a.QSaleMaxRel), F(a.QSaleSlotDev), F(a.QSaleDrop), F(a.QSalePhi), F(a.QSaleShareNet), F(a.QSaleNet), I(a.QSaleBuyers), I(a.QSaleBuyersNonInv),
            };
            sk.W.WriteLine(string.Join('\t', v));
            a.QUnpaid = a.QPaidCopy = a.QClaimDelivered = a.QEngine = a.QSellerRent = a.QInclUnpaid = a.QInclNetRent = a.QInclRate = a.QBlockNoBuyer = a.QBlockNoPrice = a.QCoverSavings = a.QExport = 0;
            System.Array.Clear(a.QUnpaidByClass);
            a.QHolders = a.QPops = a.QBuyerCount = a.QFallback = 0; a.Months = a.ScanMonths = a.Rp1Months = a.LowRpMonths = 0; a.MaxOver = a.MaxRp1Rel = 0;
            a.QSaleActual = a.QSaleCopy = a.QSaleMaxRel = a.QSaleSlotDev = a.QSaleDrop = a.QSalePhi = a.QSaleShareNet = a.QSaleNet = 0; a.QSaleEvents = a.QSaleBuyers = a.QSaleBuyersNonInv = 0;   // ★(991)
        }
    }

    /// <summary>★(991) 州 <paramref name="r"/> の RGO（不在地主を外す）の私の枠の和（null＝1）を <paramref name="slot"/> へ、POP ごとの持ち分（枠の割合の和・寄せは持ち主）を戻り値へ。</summary>
    static double[] LdHeldSnapshot(Region r, List<double> slot)
    {
        var h = new double[r.Pops.Count];
        slot.Clear();
        foreach (var s in r.Producers)
        {
            if (!s.IsRgo || LsAbsenteeRgo(r, s)) continue;
            var ls = s.LandShares;
            if (ls is null) { slot.Add(1.0); if (s.OwnerPopIndex >= 0 && s.OwnerPopIndex < h.Length) h[s.OwnerPopIndex] += 1.0; continue; }
            double sum = 0; long fb = 0;
            for (int k = 0, nk = LdSlotCount(ls); k < nk; k++) { sum += ls[k]; if (ls[k] > 0) { int j = LsRecipient(r, s, k, ref fb); if (j >= 0 && j < h.Length) h[j] += ls[k]; } }
            slot.Add(sum);
        }
        return h;
    }

    /// <summary>★(991) 受け口（LandSale.Resolve の売り手ごとの最初・On の確かめは中）：計器が自分で公売の値段と払いを組み（売り手の V は LandsaleStep の写し・買い手は寄せの枠を外す）、持ち分を写す。</summary>
    internal static void LandsaleSaleBegin(World w, Region r, int seller)
    {
        if (!On || w.WarmupFrozen || !LdAccs.TryGetValue(w, out var a)) return;
        a.Pend = false;
        var p = r.Pops[seller];
        if (!a.Owed.TryGetValue(p, out double v) || !(v > 0)) return;
        var c = w.Coef;
        var buyers = new List<int>(); long fb = 0;
        foreach (var s in r.Producers) if (s.IsRgo && !LsAbsenteeRgo(r, s) && LdHolds(r, s, seller)) LdBuyersOf(r, s, seller, buyers, ref fb);
        double sumS = 0, sumSpb = 0;
        foreach (var j in buyers) { var q = r.Pops[j]; double sj = System.Math.Max(0, q.Savings.Value); sumS += sj; sumSpb += sj * (q.Closed && !q.IsSoldier ? 1.0 - a.ExemptDeg : 1.0); }
        double landBase = c.LandTaxFixedBasis > 0 ? p.LandRentPerMonth.Value + p.LandTaxBasisAdjPerMonth.Value : p.LandRentPerMonth.Value;
        double net = sumS > 0 ? p.LandRentPerMonth.Value - w.LandTax * landBase * (sumSpb / sumS) : 0.0;
        double rate = w.GovBondRatePerYear.Value;
        double price = net > 0 && rate > 0 ? 12.0 * net / rate : 0.0;   // 計器の写し：月の純の地代×12÷年の金利
        double phi = price > 0 ? System.Math.Min(System.Math.Min(v / price, 1.0), sumS / price) : 0.0;
        // ★(995) 払いの予言＝買い手ごとの額の和にエンジンの床を入れる（Pop.Take は保有が 1e-9 以下か額が床以下なら 0 を返す）。ΣS・φ・値段は塵込みのまま（SellOne と同じ）。床は払い手の国の係数（World.Transfer が from.W.Coef を渡す）。
        double floor = c.TakeFloorPositive > 0 ? 0 : 1e-12;
        double copy = 0;
        if (phi > 0)
            foreach (var j in buyers)
            {
                double sj = System.Math.Max(0, r.Pops[j].Savings.Value);
                double amt = System.Math.Min(phi * (sj / sumS) * price, sj);
                if (sj > 1e-9 && amt > floor) copy += amt;
            }
        a.PPrice = price; a.PNet = net; a.PCopy = copy;
        a.PHeld = LdHeldSnapshot(r, a.PSlot);
        a.Pend = true;
    }

    /// <summary>★(991) 受け口（売り手ごとの最後）：エンジンが実際に移した額 <paramref name="moved"/> を写しと比べ、持ち分の動き（枠の和・売り手の手放した割合・買い手）を数える。</summary>
    internal static void LandsaleSaleEnd(World w, Region r, int seller, double moved)
    {
        if (!On || w.WarmupFrozen || !LdAccs.TryGetValue(w, out var a)) return;
        if (!a.Pend) { if (moved > 0) { a.QSaleActual += moved; a.QSaleMaxRel = double.PositiveInfinity; } return; }   // 写しが売りを立てなかったのにエンジンが売った
        a.Pend = false;
        a.QSaleActual += moved; a.QSaleCopy += a.PCopy;
        double d = System.Math.Abs(moved - a.PCopy), den = System.Math.Max(System.Math.Abs(moved), System.Math.Abs(a.PCopy));
        a.QSaleMaxRel = LdMaxNan(a.QSaleMaxRel, d == 0 ? 0.0 : den > 0 ? d / den : double.NaN);
        var slot0 = new List<double>(a.PSlot); var h0 = a.PHeld;
        var h1 = LdHeldSnapshot(r, a.PSlot);
        for (int k = 0; k < slot0.Count && k < a.PSlot.Count; k++) a.QSaleSlotDev = LdMaxNan(a.QSaleSlotDev, System.Math.Abs(a.PSlot[k] - slot0[k]));
        if (!(moved > 0) || !(a.PPrice > 0)) return;
        a.QSaleEvents++;
        double phiA = moved / a.PPrice;
        a.QSalePhi += phiA;
        a.QSaleDrop += h0[seller] > 0 ? 1.0 - h1[seller] / h0[seller] : 0.0;
        a.QSaleShareNet += 12.0 * phiA * a.PNet; a.QSaleNet += a.PNet;
        for (int j = 0; j < h1.Length && j < h0.Length; j++)
            if (j != seller && h1[j] > h0[j]) { a.QSaleBuyers++; if (j != r.InvestorPopIndex) a.QSaleBuyersNonInv++; }
    }

    /// <summary>★(995) 試験の窓（LdAcc は private）：売り手 <paramref name="p"/> の未払い V と免税の度合いを置く（LandsaleStep の代わり）。On のときだけ。</summary>
    internal static void LandsaleTestOwe(World w, Pop p, double v, double exemptDeg)
    {
        if (!On) return;
        var a = LdAccs.GetValue(w, _ => new LdAcc());
        a.Owed[p] = v; a.ExemptDeg = exemptDeg;
    }

    /// <summary>★(995) 試験の窓：四半期の積みの実際の払い・写しの払い・最大の相対差。</summary>
    internal static (double Actual, double Copy, double MaxRel) LandsaleTestRead(World w)
        => LdAccs.TryGetValue(w, out var a) ? (a.QSaleActual, a.QSaleCopy, a.QSaleMaxRel) : (double.NaN, double.NaN, double.NaN);

    static void LandsaleClose(MultiNationSim sim)
    {
        if (LandsaleSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
