using System.Collections.Generic;
namespace PriceOfBread.SimCore;

/// <summary>★(991) 土地の持ち分 B-1 段 1〜2＝土地税の払えない分の公売（旗 <see cref="Coefficients.LandShareTaxSale"/>・AI の案・確証なし・事前登録 measurements/m991_20261109_prereg_sl.txt）。
/// 作者 2026-11-04「将来的に『土地を手放す』『土地を買う』は実装したい」→〈作者「全て推奨で」で推奨を採った〉。
/// 呼び手＝<see cref="Fiscal.Step"/> の十分の一税の移しの後（走査の中は売り手と不足額 V を集めるだけ＝買い手の払える割合が POP の並びの順に依らない）。
/// 新しい状態は既にある <see cref="Producer.LandShares"/> の欄の値だけ（欄が null の RGO は売る月に [1,0,0,0] を置く＝割り方は null と同じ）。
/// ⚠既知のずれ：値段は POP 水準で畳む（売り手の LandRent は不在地主の送金と今月 rk≤0 の RGO の欠けを含む・輸出の地代を含まない）・純の地代が正でなければ未払いは消える・
/// 州の中は売り手の添字の順に解く（後の売り手の買い手の貯蓄は先の売りで減る）・買い手の居ない RGO の分は払ったのに移らない。</summary>
internal static class LandSale
{
    /// <summary>集めた（添字＝<paramref name="pops"/> の並び, 不足額 V）を解く。法の門＝農奴解放が〈なし〉でない国だけ。</summary>
    internal static void Resolve(World w, Coefficients c, List<Pop> pops, List<(int Pi, double V)> owed, double crownShare)
    {
        if (w.SerfEmancipation == ReformLevel.None || owed.Count == 0) return;
        double tau = w.LandTax, rate = w.GovBondRatePerYear.Value;
        var buyers = new List<int>(); var held = new List<Producer>();
        int oi = 0, baseIdx = 0;
        foreach (var r in w.Regions)
        {
            int end = baseIdx + r.Pops.Count;
            for (; oi < owed.Count && owed[oi].Pi < end; oi++)
            {
                int i = owed[oi].Pi - baseIdx;
                if (i < 0 || !ReferenceEquals(r.Pops[i], pops[owed[oi].Pi])) continue;   // 並びが合わなければ解かない（構造で起きない）
                Instruments.LandsaleSaleBegin(w, r, i);
                double moved = SellOne(w, c, r, i, owed[oi].V, tau, rate, crownShare, buyers, held);
                Instruments.LandsaleSaleEnd(w, r, i, moved);
            }
            baseIdx = end;
        }
    }

    /// <summary>枠 <paramref name="k"/> の生の受け手（0 持ち主・1 聖職・2 担い手・3 投資家）。州の POP の範囲の外（寄せ）は −1。</summary>
    static int SlotPop(Region r, Producer s, int k)
    {
        int j = k switch { 0 => s.OwnerPopIndex, 1 => r.ClergyPopIndex, 2 => s.LaborPopIndex, 3 => r.InvestorPopIndex, _ => -1 };
        return j >= 0 && j < r.Pops.Count ? j : -1;
    }

    /// <summary>売り手 <paramref name="i"/> の 1 回の公売。戻り値＝買い手から国庫へ実際に移った額の和。</summary>
    static double SellOne(World w, Coefficients c, Region r, int i, double v, double tau, double rate, double crownShare, List<int> buyers, List<Producer> held)
    {
        buyers.Clear(); held.Clear();
        foreach (var s in r.Producers)
        {
            if (!s.IsRgo || Colonial.AbsenteeRgo(r, s)) continue;
            var ls = s.LandShares;
            bool holds = false;
            if (ls is null) holds = s.OwnerPopIndex == i;
            else for (int k = 0, nk = System.Math.Min(4, ls.Length); k < nk && !holds; k++) holds = ls[k] > 0 && EconomyStep.LandShareRecipient(r, s, k) == i;
            if (!holds) continue;
            held.Add(s);
            for (int k = 0; k < 4; k++) { int j = SlotPop(r, s, k); if (j >= 0 && j != i && !buyers.Contains(j)) buyers.Add(j); }
        }
        if (held.Count == 0 || buyers.Count == 0) return 0;
        double sumS = 0, sumSpb = 0;
        foreach (int j in buyers) { var pj = r.Pops[j]; double sj = System.Math.Max(0, pj.Savings.Value); sumS += sj; sumSpb += sj * Privileges.TaxAssessment(w, c, pj, crownShare); }
        if (!(sumS > 0)) return 0;
        var seller = r.Pops[i];
        double net = seller.LandRentPerMonth.Value - tau * Fiscal.LandTaxBase(c, seller).Value * (sumSpb / sumS);
        if (!(net > 0) || !(rate > 0)) return 0;
        double price = Calendar.PerMonthToPerYear(net) / rate;   // 持ち分の全体の値段＝純の地代の年額÷国債金利
        double phi = v / price;
        if (phi > 1) phi = 1;                                     // 定義域：持っている分より多く売れない
        if (phi > sumS / price) phi = sumS / price;               // 定義域：買い手の貯蓄より多く払えない
        if (!(phi > 0)) return 0;
        double movedSum = 0, aSum = 0;
        var a = new double[buyers.Count];
        for (int b = 0; b < buyers.Count; b++)
        {
            var pj = r.Pops[buyers[b]];
            double sj = System.Math.Max(0, pj.Savings.Value);
            double amt = System.Math.Min(phi * (sj / sumS) * price, sj);
            if (!(amt > 0)) continue;
            double moved = World.Transfer((w, [pj]), (w, null, null), amt, TreasuryCat.OtherIn);   // 買い手→国庫（公売の代金・走査の税収には足さない）
            movedSum += moved; a[b] = moved / price; aSum += a[b];
        }
        if (!(aSum > 0)) return movedSum;
        foreach (var s in held)
        {
            double eSum = 0;
            for (int b = 0; b < buyers.Count; b++) if (a[b] > 0 && TargetSlot(r, s, buyers[b]) >= 0) eSum += a[b];
            if (!(eSum > 0)) continue;   // この RGO に枠を持つ買い手がいない＝動かさない
            if (s.LandShares is null) s.LandShares = [1.0, 0.0, 0.0, 0.0];   // null＝今の持ち主に 1.0 と同じ割り方の配列
            var ls = s.LandShares;
            for (int k = 0, nk = System.Math.Min(4, ls.Length); k < nk; k++)
            {
                if (!(ls[k] > 0) || EconomyStep.LandShareRecipient(r, s, k) != i) continue;
                double sold = ls[k] * aSum, left = sold;
                ls[k] -= sold;
                int last = -1;
                for (int b = 0; b < buyers.Count; b++) if (a[b] > 0 && TargetSlot(r, s, buyers[b]) >= 0) last = b;
                for (int b = 0; b < buyers.Count; b++)
                {
                    if (!(a[b] > 0)) continue;
                    int kt = TargetSlot(r, s, buyers[b]);
                    if (kt < 0) continue;
                    double part = b == last ? left : sold * (a[b] / eSum);
                    ls[kt] += part; left -= part;
                }
            }
        }
        return movedSum;
    }

    /// <summary>RGO <paramref name="s"/> で POP <paramref name="j"/> が生の受け手の最初の枠（k＜4）。無ければ −1。</summary>
    static int TargetSlot(Region r, Producer s, int j)
    {
        for (int k = 0; k < 4; k++) if (SlotPop(r, s, k) == j) return k;
        return -1;
    }
}
