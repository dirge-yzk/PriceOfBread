using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1024) K2 価格の歩の内訳の計器 pxstep_s（読むだけ・恒等・事前登録 measurements/m1024_20261110_prereg_kf.txt） ----------------------------------------------------------------
// 部分クラス＝InstrumentsScope.cs と同じ作り（蓄えは ConditionalWeakTable・セーブに載らない）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 出すもの（1 試合＝pxstep_s<seed>.tsv）：年×国×格 tierCode×枝 branch の行（件数が 0 の枝は書かない）。
//   枝：labor＝労働の財（日次の写像を回した）・svcClosed＝役務の閉じた式・svcDaily＝役務の日次・goodsClosed＝財の閉じた式・pinnedClosed＝張り付いた財の閉じた式・goodsDaily＝財の日次・
//        dormant＝月の残りを止まっている財（歩に入れなかった）・dead＝需要も産出も無い財（歩に入れなかった）・other＝歩に入れたが上のどれでもない財（0 のはず＝枝の網羅の自己確認）。
//   goodsCount＝その枝の財の件数（財×州×歩）・dailyRuns＝日次の写像を回した日数（labor・svcDaily・goodsDaily だけ 0 でない）。
//   台帳との対応：Σ枝（dormant を除く）の goodsCount＝台帳 PriceGoods（live − dormant）・Σ dailyRuns＝台帳 PriceDailyRuns。
// 書き手は MarketFlow.StepDay の終わりの 1 か所。州は 1 本のスレッドが回す＝蓄えは州ごと（共有のリストを持たない）・集計は整数の和（順に依らない）。
// ⚠限界：①格は呼んだ時点の州の格。②年の終わりに数えるのは今の国が持つ州＝州ごと消えた州の蓄えは落ちる。
public static partial class Instruments
{
    internal static readonly string[] PxBranchNames = ["labor", "svcClosed", "svcDaily", "goodsClosed", "pinnedClosed", "goodsDaily", "dormant", "dead", "other"];
    const int PxB = 9;

    sealed class PxAcc { public readonly long[] Cnt = new long[3 * PxB], Runs = new long[3 * PxB]; }
    sealed class PxSink { public System.IO.StreamWriter? W; public bool Header; public ulong Seed; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, PxAcc> PxAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PxSink> PxSinks = new();

    /// <summary>StepDay の終わりの 1 か所（<c>Instruments.On</c> を読んだ呼び手だけが呼ぶ）。局所の数を渡すだけ：<paramref name="live"/>＝生きた財の一覧の長さ・<paramref name="steps"/>＝歩に入れた財・
    /// 残りは枝ごとの件数と日次の日数。dead＝live−dormant−steps・other＝steps−6 枝の和。</summary>
    internal static void PxStep(Region r, long live, long steps, long dormant, long labor, long svcClosed, long svcDaily, long goodsClosed, long pinned, long goodsDaily,
        long runsLabor, long runsSvc, long runsGoods)
    {
        var a = PxAccs.GetValue(r, _ => new PxAcc());
        int o = (int)r.Tier * PxB;
        a.Cnt[o + 0] += labor; a.Cnt[o + 1] += svcClosed; a.Cnt[o + 2] += svcDaily; a.Cnt[o + 3] += goodsClosed; a.Cnt[o + 4] += pinned; a.Cnt[o + 5] += goodsDaily;
        a.Cnt[o + 6] += dormant; a.Cnt[o + 7] += live - dormant - steps;
        a.Cnt[o + 8] += steps - (labor + svcClosed + svcDaily + goodsClosed + pinned + goodsDaily);
        a.Runs[o + 0] += runsLabor; a.Runs[o + 2] += runsSvc; a.Runs[o + 5] += runsGoods;
    }

    /// <summary>試験の口：州の蓄え（格の和）の枝ごとの件数と日次の日数。</summary>
    internal static (long[] Cnt, long[] Runs) PxGet(Region r)
    {
        var cnt = new long[PxB]; var runs = new long[PxB];
        if (PxAccs.TryGetValue(r, out var a)) for (int t = 0; t < 3; t++) for (int b = 0; b < PxB; b++) { cnt[b] += a.Cnt[t * PxB + b]; runs[b] += a.Runs[t * PxB + b]; }
        return (cnt, runs);
    }

    /// <summary>助走の直後：助走の歩を数えない。</summary>
    static void PxClear(MultiNationSim sim)
    {
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            if (PxAccs.TryGetValue(r, out var a)) { System.Array.Clear(a.Cnt); System.Array.Clear(a.Runs); }
    }

    const string PxHeader = "year\tnation\ttierCode\tbranch\tgoodsCount\tdailyRuns";

    /// <summary>年の終わり（<paramref name="force"/> は試合の終わりの残り）：蓄えを国×格×枝の行にして書き、0 へ戻す。</summary>
    static void PxFold(MultiNationSim sim, ulong seed, int q, bool force)
    {
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd && !force) return;
        var rows = new Dictionary<(string Nation, int Tier, int Branch), (long Cnt, long Runs)>();
        foreach (var n in sim.Nations)
        {
            string name = NameOf(n.World);
            foreach (var r in n.World.Regions)
            {
                if (!PxAccs.TryGetValue(r, out var a)) continue;
                for (int t = 0; t < 3; t++) for (int b = 0; b < PxB; b++)
                {
                    long c = a.Cnt[t * PxB + b], u = a.Runs[t * PxB + b];
                    if (c == 0 && u == 0) continue;
                    var k = (name, t, b); rows.TryGetValue(k, out var cur); rows[k] = (cur.Cnt + c, cur.Runs + u);
                }
                System.Array.Clear(a.Cnt); System.Array.Clear(a.Runs);
            }
        }
        if (rows.Count == 0) return;
        var sk = PxSinks.GetValue(sim, _ => new PxSink());
        sk.Seed = seed;
        sk.W ??= Open("pxstep", seed);
        if (!sk.Header) { sk.W.WriteLine(PxHeader); sk.Header = true; }
        int year = sim.Nations[0].World.Year;
        foreach (var kv in rows.OrderBy(k => k.Key.Nation, StringComparer.Ordinal).ThenBy(k => k.Key.Tier).ThenBy(k => k.Key.Branch))
            sk.W.WriteLine(string.Join('\t', TrI(year), kv.Key.Nation, ((RegionTier)kv.Key.Tier).ToString(), PxBranchNames[kv.Key.Branch],
                kv.Value.Cnt.ToString(System.Globalization.CultureInfo.InvariantCulture), kv.Value.Runs.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>Quarter の呼び口（Instruments.cs の MerchTable の行に名だけ足す）。</summary>
    internal static void PxstepTable(MultiNationSim sim, ulong seed, int q) { PxSinks.GetValue(sim, _ => new PxSink()).Seed = seed; PxFold(sim, seed, q, false); }

    /// <summary>Close の呼び口：年の途中で終わった残りを書き、pxstep_s と cscx_s の書き手を閉じる。</summary>
    static void PxstepClose(MultiNationSim sim)
    {
        ulong seed = PxSinks.TryGetValue(sim, out var s0) ? s0.Seed : 0;
        PxFold(sim, seed, 0, true);
        if (PxSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
        CscxClose(sim);
    }
}
