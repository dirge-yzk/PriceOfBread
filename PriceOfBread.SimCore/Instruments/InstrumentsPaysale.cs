namespace PriceOfBread.SimCore;

// (1228) c124bk 計器 paysale_s：PaySales の代金を生産者の帳（LastRevenue・LastCost）へ載せる記帳（旗 PaySalesToProducerBook）の観測。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable）。
// 同じ盤面で旧の規則（帳に載せない）と新の規則（載せる）の月の損益の符号を両方数える＝旗 0 でも新の規則の影を数える。
public static partial class Instruments
{
    internal const string PaysaleHeaderText = "quarter\tyear\tnation\tgood\tcalls\tpayToProducers\tpayExport\tpayMint\tpayDomestic\trentPart\trevBooked\tcostBooked\tprodMonths\tnegOld\tnegNew\tflipNegToPos\tflipPosToNeg\tstateMonths\tstateFlipNegToPos";

    // 列の添字（PaysaleHeaderText の calls から順）
    const int PsCalls = 0, PsPay = 1, PsExport = 2, PsMint = 3, PsDomestic = 4, PsRent = 5, PsRevB = 6, PsCostB = 7,
              PsMonths = 8, PsNegOld = 9, PsNegNew = 10, PsFlipNP = 11, PsFlipPN = 12, PsStateMonths = 13, PsStateFlip = 14, PsLen = 15;

    /// <summary>生産者ごとの、前の清算の上書きから次の清算の前までの和（新の規則が載せる分の影と、実際に欄が動いた分）。</summary>
    sealed class PsAcc { public double ShRev, ShCost, BkRev, BkCost; public bool Any; }
    sealed class PsWorld { public double[,]? Q; }
    sealed class PsSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Producer, PsAcc> PsAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, PsWorld> PsWorlds = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PsSink> PsSinks = new();

    static double[,] PsQ(World w)
    {
        var s = PsWorlds.GetValue(w, _ => new PsWorld());
        return s.Q ??= new double[Catalog.Goods.Length, PsLen];
    }

    /// <summary>受け口（InternationalMarket.PaySales の生産者ごとの割りの中・INS のときだけ）：割った代金 <paramref name="amt"/>・その賃金の分 <paramref name="wages"/>・
    /// 記帳の前の欄の値。欄の後の値は計器が自分で読む（実際に動いた量）。</summary>
    internal static void PaysaleAlloc(World w, Producer pr, double amt, double wages, double revBefore, double costBefore, bool minted, bool outside)
    {
        if (Live(w) is null) return;
        int g = pr.GoodId;
        if (g < 0 || g >= Catalog.Goods.Length) return;
        double dRev = pr.LastRevenue - revBefore, dCost = pr.LastCost - costBefore;
        var q = PsQ(w);
        q[g, PsCalls] += 1; q[g, PsPay] += amt; q[g, minted ? PsMint : outside ? PsExport : PsDomestic] += amt;
        q[g, PsRevB] += dRev; q[g, PsCostB] += dCost;
        if (!(wages >= 0)) return;   // 新の規則が載せない割り（NaN を含む）は影にも積まない
        q[g, PsRent] += amt - wages;
        var a = PsAccs.GetValue(pr, _ => new PsAcc());
        a.ShRev += amt; a.ShCost += wages; a.BkRev += dRev; a.BkCost += dCost; a.Any = true;
    }

    /// <summary>受け口（Instruments.PreEconomy＝州の清算の上書きの前・国の請求が読んだのと同じ欄）：旧の規則と新の規則の月の損益の符号を数え、生産者の和を 0 へ戻す。</summary>
    static void PaysaleMonth(World w, Region r)
    {
        if (Live(w) is null) return;
        double[,]? q = null;
        foreach (var p in r.Producers)
        {
            if (!PsAccs.TryGetValue(p, out var a) || !a.Any) continue;
            int g = p.GoodId;
            if (g >= 0 && g < Catalog.Goods.Length)
            {
                q ??= PsQ(w);
                double pOld = (p.LastRevenue - a.BkRev) - (p.LastCost - a.BkCost);
                double pNew = pOld + a.ShRev - a.ShCost;
                bool negOld = pOld < 0, negNew = pNew < 0;
                q[g, PsMonths] += 1;
                if (negOld) q[g, PsNegOld] += 1;
                if (negNew) q[g, PsNegNew] += 1;
                if (negOld && !negNew) q[g, PsFlipNP] += 1;
                if (!negOld && negNew) q[g, PsFlipPN] += 1;
                if (p.BuiltScale > 1e-9) { q[g, PsStateMonths] += 1; if (negOld && !negNew) q[g, PsStateFlip] += 1; }
            }
            a.ShRev = a.ShCost = a.BkRev = a.BkCost = 0; a.Any = false;
        }
    }

    /// <summary>四半期の末：全ての国×その四半期に何か有った財の 1 行。書いたら国の和を 0 へ戻す。</summary>
    static void PaysaleTable(MultiNationSim sim, ulong seed, int q)
    {
        var ps = PsSinks.GetValue(sim, _ => new PsSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (!PsWorlds.TryGetValue(w, out var s) || s.Q is not { } a) continue;
            if (Wants("paysale_s"))
            {
                if (ps.W is null) { ps.W = Open("paysale", seed); ps.W.WriteLine(PaysaleHeaderText); }
                for (int g = 0; g < a.GetLength(0); g++)
                {
                    if (!(a[g, PsCalls] > 0) && !(a[g, PsMonths] > 0)) continue;
                    var cols = new List<string>(4 + PsLen) { I(q), I(w.Year), NameOf(w), Catalog.Goods[g].Name };
                    for (int k = 0; k < PsLen; k++) cols.Add(F(a[g, k]));
                    ps.W.WriteLine(string.Join('\t', cols));
                }
            }
            System.Array.Clear(a);
        }
    }

    static void PaysaleClose(MultiNationSim sim) { if (PsSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
