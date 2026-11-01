namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (755) 需要の価格因子の挟みの表 dfac_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsMigform.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（積みは計器の側の欄）。
// 目的＝事前登録 measurements/m755_20261102_prereg_cl.txt の C1（使った欲しい量＝挟まない欲しい量）・C2（挟みに当たっていた行の中の新÷旧）を同じ盤面で読む。
// 行＝国×層×四半期（需要の入口を通った行が 1 つでもあった層だけ）。層＝1 日用品／2 奢侈品／3 役務その他（役務の優先配分でないもの）／4 役務の優先配分（boughtUsedSum は積まない＝配分は別の枝）。食料（主食・食料の非主食）は積まない。
//   needSum＝Σ 市場に出す必要量／desiredOldSum＝Σ 必要量×Clamp(自己因子, PriceFactorMin, 上限)／desiredNewSum＝Σ 必要量×自己因子／desiredUsedSum＝Σ 必要量×実際に使った因子（旗に従う）／
//   lowBind*＝自己因子＜PriceFactorMin の行の 必要量・旧・使った／highBind*＝自己因子＞上限の行の同じ3つ／boughtUsedSum＝予算と配給の後に実際に買えた量。
//   旗 0 では desiredUsed＝desiredOld（ビット一致）。旗 1 では desiredUsed＝desiredNew。
// ⚠限界（計器が自己申告する）：旧と新は同じ月の盤面から数えた反実仮想（旗 1 の軌道では旧の規則の世界は走らない）／欲しい量は所得の弾力・買い占めの掛け算の前／層 4 は買えた量を持たない／助走の月と挙げていない国は積まない。
public static partial class Instruments
{
    internal const string DfacHeaderText = "quarter	year	nation	tier	needSum	desiredOldSum	desiredNewSum	desiredUsedSum	lowBindNeedSum	lowOldSum	lowUsedSum	highBindNeedSum	highOldSum	highUsedSum	boughtUsedSum";
    const int DfacK = 11, DfacTiers = 4;   // 積みの欄の数／層の数（1〜4）

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> DfacAcc = new();
    sealed class DfacSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DfacSink> DfacSinks = new();

    /// <summary>州の消費の頭（計器が on のとき）：この国の積みの配列。助走の月と挙げていない国は null（POB_INSTRUMENTS_NATIONS の国だけ積む）。</summary>
    internal static double[]? DfacBegin(World w) => w.WarmupFrozen || !IsPicked(w, S(w)) ? null : DfacAcc.GetValue(w, _ => new double[DfacTiers * DfacK]);

    /// <summary>番人（テスト）が読む口：この国の今の積み（層 1〜4 を DfacK ずつ並べた配列・書き出し前）。</summary>
    internal static double[]? DfacPeek(World w) => DfacAcc.TryGetValue(w, out var a) ? a : null;

    /// <summary>層の番号（1〜4）。食料は -1（積まない）。</summary>
    internal static int DfacTier(GoodTier tier, bool food, bool priorityService) => food ? -1 : priorityService ? 4 : tier == GoodTier.Daily ? 1 : tier == GoodTier.Luxury ? 2 : 3;

    /// <summary>需要の入口の POP ごと：必要量に旧（挟む）・新（挟まない）・使った因子を掛けた量を足す。</summary>
    internal static void DfacWant(double[] a, int t, double need, double self, double min, double max, double used)
    {
        int k = (t - 1) * DfacK;
        double old = Math.Clamp(self, min, max);
        a[k] += need; a[k + 1] += need * old; a[k + 2] += need * self; a[k + 3] += need * used;
        if (self < min) { a[k + 4] += need; a[k + 5] += need * old; a[k + 6] += need * used; }
        if (self > max) { a[k + 7] += need; a[k + 8] += need * old; a[k + 9] += need * used; }
    }

    /// <summary>支払いの段：予算と配給の後に実際に買えた量（層 1〜3）。</summary>
    internal static void DfacBought(double[] a, int t, double qty) { if (t <= 3) a[(t - 1) * DfacK + 10] += qty; }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた積みを国×層ごとに書いて 0 へ戻す。</summary>
    static void DfacTable(MultiNationSim sim, ulong seed, int q)
    {
        foreach (var n in sim.Nations)
        {
            if (!DfacAcc.TryGetValue(n.World, out var a)) continue;
            for (int t = 1; t <= DfacTiers; t++)
            {
                int k = (t - 1) * DfacK;
                if (!(a[k] > 0)) continue;
                var sk = DfacSinks.GetValue(sim, _ => new DfacSink());
                sk.W ??= Open("dfac", seed);
                if (!sk.Header) { sk.W.WriteLine(DfacHeaderText); sk.Header = true; }
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var cells = new List<string> { q.ToString(inv), n.World.Year.ToString(inv), NameOf(n.World), t.ToString(inv) };
                for (int j = 0; j < DfacK; j++) cells.Add(F(a[k + j]));
                sk.W.WriteLine(string.Join('	', cells));
            }
            Array.Clear(a);
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void DfacClose(MultiNationSim sim)
    {
        if (DfacSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
