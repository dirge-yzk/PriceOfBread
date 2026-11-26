using System.Diagnostics;

namespace PriceOfBread.SimCore;

/// <summary>(1391) 速さの計器＝POB_TIME=1（MultiNationSim.TimeOn）の時だけ、段ごとの時間と回数を貯めて、プロセスの終わりに標準エラーへ表で出す。
/// 閉じている時は Begin が 0 を返し End・Lap は何もしない＝世界も .cost も変えない（台帳の行は足さない）。
/// 日次の相は国ごとに並列＝DailyCpu は国ごとの時間の和（CPU の和）・DailyWall は並列の呼びの前後（壁の時間）。他の段は逐次の側で測るので壁の時間。
/// 出力の列：段・回数・秒。</summary>
internal static class StageTimer
{
    internal const int Open = 0, DailyCpu = 1, DailyWall = 2, Close = 3, MonthSettle = 4, QuarterStart = 5, QuarterEnd = 6, MarketStep = 7,
        DiploStep = 8, SharedQuarter = 9, Canals = 10,
        DipSync = 11, DipFormations = 12, DipWarWeary = 13, DipRanks = 14, DipTensions = 15, DipRelations = 16, DipAi = 17, DipWars = 18, DipTail = 19, N = 20;
    private static readonly string[] Names =
    [
        "StepDayOpen", "StepDayDaily(国ごとの和＝CPU)", "StepDayDaily(並列の壁)", "StepDayClose(全体)", "MonthSettle", "QuarterStart", "QuarterEnd", "Market.Step",
        "Diplomacy.Step(全体)", "四半期末の共有層(全体)", "StepCanals",
        "外交:SyncPowers", "外交:Formations", "外交:戦争の傷", "外交:順位", "外交:緊張の各本", "外交:従属・条約港・同盟・封じ込め", "外交:AI・統一戦争・通商反応・宣戦・介入", "外交:戦争・略奪", "外交:漂流・軍の形",
    ];
    private static readonly long[] Ticks = new long[N];
    private static readonly long[] Count = new long[N];
    internal static bool On => MultiNationSim.TimeOn;

    static StageTimer()
    {
        if (MultiNationSim.TimeOn) System.AppDomain.CurrentDomain.ProcessExit += (_, _) => Print();
    }

    internal static long TotalTicks() { long s = 0; foreach (var t in Ticks) s += t; return s; }
    internal static long Begin() => On ? Stopwatch.GetTimestamp() : 0;
    internal static void End(int stage, long t0)
    {
        if (!On) return;
        System.Threading.Interlocked.Add(ref Ticks[stage], Stopwatch.GetTimestamp() - t0);
        System.Threading.Interlocked.Increment(ref Count[stage]);
    }
    /// <summary>前の区切りからの時間を stage へ足し、区切りを今に進める（一続きの処理を段ごとに分ける）。</summary>
    internal static void Lap(ref long lap, int stage)
    {
        if (!On) return;
        long now = Stopwatch.GetTimestamp();
        System.Threading.Interlocked.Add(ref Ticks[stage], now - lap);
        System.Threading.Interlocked.Increment(ref Count[stage]);
        lap = now;
    }

    private static void Print()
    {
        var e = System.Console.Error;
        e.WriteLine("POB_TIME段\t名\t回数\t秒");
        for (int i = 0; i < N; i++) e.WriteLine($"POB_TIME段\t{Names[i]}\t{Count[i]}\t{Ticks[i] / (double)Stopwatch.Frequency:F3}");
    }
}
