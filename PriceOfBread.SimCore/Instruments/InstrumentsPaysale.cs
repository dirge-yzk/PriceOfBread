namespace PriceOfBread.SimCore;

// (1228) c124bk 計器 paysale_s：PaySales の代金を生産者の帳（LastRevenue・LastCost）へ載せる記帳（旗 PaySalesToProducerBook）の観測。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable）。
// 同じ盤面で旧の規則（帳に載せない）と新の規則（載せる）の月の損益の符号を両方数える＝旗 0 でも新の規則の影を数える。
public static partial class Instruments
{
    // 列の名前は 1 か所（見出しと行の列の数は同じ配列から作る＝(1241) v 高 1：見出しが 2 列足りないまま R5 が同じ定数と比べて緑だった）
    internal static readonly string[] PaysaleColNames = ["calls", "payToProducers", "payExport", "payMint", "payDomestic", "rentPart", "revBooked", "costBooked", "prodMonths", "negOld", "negNew", "flipNegToPos", "flipPosToNeg", "stateMonths", "stateFlipNegToPos", "diffAll", "diffStateStake"];
    internal static readonly string Paysale2HeaderText = "quarter\tyear\tnation\tgood\t" + string.Join('\t', PaysaleColNames);

    // 列の添字（PaysaleColNames の calls から順）
    const int PsCalls = 0, PsPay = 1, PsExport = 2, PsMint = 3, PsDomestic = 4, PsRent = 5, PsRevB = 6, PsCostB = 7,
              PsMonths = 8, PsNegOld = 9, PsNegNew = 10, PsFlipNP = 11, PsFlipPN = 12, PsStateMonths = 13, PsStateFlip = 14, PsDiffAll = 15, PsDiffStake = 16;
    static int PsLen => PaysaleColNames.Length;   // 列の数は見出しと同じ配列から（添字 0〜PsDiffStake の 17 本）

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
        if (!Tg.Paysale) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (w.WarmupFrozen) return;   // 月の決算の外（輸出の清算・日の段）でも数える＝Live（決算の間だけ立つ）は使わない
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
        if (!Tg.Paysale) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (w.WarmupFrozen) return;
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
                double diff = pNew - pOld;   // (1235) 新の規則が足す損益の差（持ち主の分け方に依らない）と、国の持ち分（国の単位÷全ての単位）を掛けた分
                int su = w.StateUnitsOf(p), pu = w.PrivateUnitsOf(p);
                q[g, PsDiffAll] += diff; q[g, PsDiffStake] += PaysaleStakeDiff(su, pu, diff);
            }
            a.ShRev = a.ShCost = a.BkRev = a.BkCost = 0; a.Any = false;
        }
    }

    /// <summary>国の持ち分の損益の差＝持ち分×差（diffStateStake の 1 生産者ぶん）。</summary>
    internal static double PaysaleStakeDiff(int stateUnits, int privateUnits, double diff) => PaysaleStake(stateUnits, privateUnits) * diff;

    /// <summary>国の持ち分＝国の単位÷（国の単位＋民間の単位）。和が正でない・非有限なら 0（否定形）。</summary>
    internal static double PaysaleStake(int stateUnits, int privateUnits)
    {
        long tot = (long)stateUnits + privateUnits;
        return stateUnits > 0 && tot > 0 ? (double)stateUnits / tot : 0.0;
    }

    /// <summary>四半期の末：挙げた国は毎四半期・全ての国は年末（q%4==3）に、その期間に何か有った財の 1 行。書いた後に国の和を 0 へ戻す（書かない四半期は持ち越す）。</summary>
    static void PaysaleTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.Paysale) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        var ps = PsSinks.GetValue(sim, _ => new PsSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (!PsWorlds.TryGetValue(w, out var s) || s.Q is not { } a) continue;
            if (!(q % 4 == 3 || IsPicked(w, S(w)))) continue;   // 書かない四半期は積んだまま持ち越す（年末に 4 四半期ぶんの行）
            if (Wants("paysale2_s"))
            {
                if (ps.W is null) { ps.W = Open("paysale2", seed); ps.W.WriteLine(Paysale2HeaderText); }
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
