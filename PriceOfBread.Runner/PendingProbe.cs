using System.Reflection;
using System.Text;
using PriceOfBread.SimCore;

namespace PriceOfBread.Runner;

/// <summary>(440) 線 dp の機構の確認：プレイヤー国を NullAgent にして**待ちに答えずに**全四半期を進め、四半期の末ごとに外交の待ちの古さと塞がれを数える（読むだけ）。
/// 事前登録 measurements/dp_20261019_prereg.txt の [4] の P1〜P4 と〈説明できない組〉。腕ではない＝成績表には載らない。基点の木（旧）と形の木（新）で同じ盤面を回して比べる。
///
/// 使い方： dotnet PriceOfBread.Runner.dll pendprobe [国名=日本] [seed=11] [四半期数=660] [出力tsv=なし]
/// 出力：毎四半期の行（tsv）と、最後の要約（P1〜P4・組の説明の分布）。言葉の定義は事前登録のとおり。
/// ⚠プレイヤーは答えない＝有効な最後通牒は積まれたまま残る（塞がれ）。古い待ち（根拠の門が閉じたのに残る待ち）と区別して数える。</summary>
internal static class PendingProbe
{
    public static int Run(string[] args)
    {
        string playerName = args.Length > 1 ? args[1] : "日本";
        ulong seed = args.Length > 2 && ulong.TryParse(args[2], out ulong sd) ? sd : 11UL;
        int quarters = args.Length > 3 && int.TryParse(args[3], out int q0) ? q0 : 660;
        string? outPath = args.Length > 4 ? args[4] : null;

        var cfgs = WorldData.Nations();
        int pi = Array.FindIndex(cfgs, c => c.Name == playerName);
        if (pi < 0) { Console.WriteLine($"国が見つからない: {playerName}"); return 2; }
        var playerCfg = cfgs[pi];
        var sim = new MultiNationSim(cfgs, seed, cfg => ReferenceEquals(cfg, playerCfg) ? new NullAgent() : (INationAgent)new UtilityAgent());
        sim.Diplomacy.Powers[pi].IsPlayer = true;
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StartWarmup();

        var d = sim.Diplomacy;
        var c = sim.Nations[0].World.Coef;
        var t = typeof(Diplomacy);
        const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        var worth = t.GetMethod("WorthFighting", F)!; var inDep = t.GetMethod("InDependency", F)!;
        var clockF = (Dictionary<long, int>)t.GetField("_warClock", F)!.GetValue(d)!;
        var keyM = t.GetMethod("Key", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;
        bool Weary(Power a, Power b) => a.WarExhaustion > c.WarWearyThreshold || b.WarExhaustion > c.WarWearyThreshold;
        bool Dep(Power a, Power b) => (bool)inDep.Invoke(d, [a, b])!;
        bool Worth(Power a, Power b) => (bool)worth.Invoke(d, [a, b, c])!;
        int Clock(Power a, Power b) => clockF.GetValueOrDefault((long)keyM.Invoke(d, [a, b])!);
        bool GateOpen(Power a, Power b) => d.Tension(a, b) >= c.WarDeclareTension && !d.IsAtWar(a, b) && !Weary(a, b) && !Dep(a, b) && Worth(a, b);

        var player = d.Powers[pi];
        var sb = new StringBuilder("quarter\tyear\tstaleCalls\tpendingWarGateClosed\tpendingWarStaleTwoQuarters\tgateOpenClockFullSlotEmpty\tgateOpenClockFullSlotEmptyTwoQuarters\tblockedBy\tpairsHot\tbyWeary\tbyDep\tbyWorth\tbyClock\tbyPending\tbyBlocked\tunexplained\n");
        long staleCalls = 0, closedQ = 0, staleTwo = 0, emptyQ = 0, emptyTwo = 0, blockedQ = 0, hotPairs = 0, unexplained = 0, raised = 0, droppedWar = 0, droppedCall = 0;
        long cWeary = 0, cDep = 0, cWorth = 0, cClock = 0, cPending = 0, cBlocked = 0;
        bool prevClosed = false; Power? prevPending = null; bool prevEmptyFull = false; bool prevCall = false;
        for (int q = 0; q < quarters; q++)
        {
            sim.StepQuarter();
            var pw = player.PendingWar; var call = player.PendingCallToArms;
            int stale = call is { } cc && !d.IsAtWar(cc.Caller, cc.Enemy) ? 1 : 0;
            staleCalls += stale;
            bool closed = pw is not null && !GateOpen(player, pw);
            closedQ += closed ? 1 : 0;
            bool twoQ = closed && prevClosed && prevPending == pw;
            staleTwo += twoQ ? 1 : 0;
            // プレイヤーの組で門が開き時計が満ちているのに枡が空
            bool emptyFull = false, blocked = false;
            int hot = 0, nWeary = 0, nDep = 0, nWorth = 0, nClock = 0, nPending = 0, nBlocked = 0, nUnexpl = 0;
            for (int xi = 0; xi < d.Powers.Count; xi++)
            {
                var x = d.Powers[xi];
                for (int yi = xi + 1; yi < d.Powers.Count; yi++)
                {
                    var y = d.Powers[yi];
                    if (d.Tension(x, y) < c.WarDeclareTension || d.IsAtWar(x, y)) continue;
                    hot++;
                    bool pp = x == player || y == player;
                    if (Weary(x, y)) nWeary++;
                    else if (Dep(x, y)) nDep++;
                    else if (!Worth(x, y)) nWorth++;
                    else if (Clock(x, y) < c.WarTriggerMonths) nClock++;
                    else if (pp)
                    {
                        var foe = x == player ? y : x;
                        if (player.PendingWar == foe) nPending++;
                        else if (player.PendingWar is not null) { nBlocked++; blocked = true; }
                        else { nUnexpl++; emptyFull = true; }
                    }
                    else nUnexpl++;
                }
            }
            emptyQ += emptyFull ? 1 : 0; blockedQ += blocked ? 1 : 0;
            bool emptyTwo2 = emptyFull && prevEmptyFull; emptyTwo += emptyTwo2 ? 1 : 0;
            hotPairs += hot; cWeary += nWeary; cDep += nDep; cWorth += nWorth; cClock += nClock; cPending += nPending; cBlocked += nBlocked; unexplained += nUnexpl;
            if (prevPending is not null && pw is null) droppedWar++;
            if (prevCall && call is null) droppedCall++;
            if (pw is not null && prevPending is null) raised++;
            var w0 = sim.Nations[0].World;
            sb.Append($"{q}\t{w0.Year}\t{stale}\t{(closed ? 1 : 0)}\t{(twoQ ? 1 : 0)}\t{(emptyFull ? 1 : 0)}\t{(emptyTwo2 ? 1 : 0)}\t{(blocked ? 1 : 0)}\t{hot}\t{nWeary}\t{nDep}\t{nWorth}\t{nClock}\t{nPending}\t{nBlocked}\t{nUnexpl}\n");
            prevClosed = closed; prevPending = pw; prevEmptyFull = emptyFull; prevCall = call is not null;
        }
        if (outPath is not null) File.WriteAllText(outPath, sb.ToString(), PlayHarness.Utf8NoBom);
        Console.WriteLine($"pendprobe {playerName} seed {seed} 四半期 {quarters}（終了 {sim.Nations[0].World.Year}年）");
        Console.WriteLine($"P1 古い参戦要請の待ち（四半期の末に元の戦争が無い）の延べ四半期数 = {staleCalls}");
        Console.WriteLine($"P2 根拠の門が閉じた最後通牒の待ちの延べ四半期数 = {closedQ}／同じ待ちが 2 四半期の末を続けて門閉 = {staleTwo}");
        Console.WriteLine($"P3 プレイヤーの組で門が開き時計が満ちたのに待ちの枡が空の延べ四半期数 = {emptyQ}／2 四半期続けて = {emptyTwo}／別の有効な待ちで塞がれた延べ四半期数（記録）= {blockedQ}");
        Console.WriteLine($"P4 機構を踏んだ証拠：最後通牒が積まれた回数 = {raised}／積まれた待ちが下りた回数 = {droppedWar}（答えないので下りる＝寿命か相手の消滅）／参戦要請の待ちが下りた回数 = {droppedCall}");
        Console.WriteLine($"組×四半期（緊張が水準以上で交戦なし）= {hotPairs}：厭戦 {cWeary}・従属 {cDep}・到達 {cWorth}・継続未満 {cClock}・待ち中 {cPending}・塞がれ {cBlocked}・説明できない {unexplained}（{(hotPairs > 0 ? 100.0 * unexplained / hotPairs : 0):F2}%）");
        return 0;
    }
}
