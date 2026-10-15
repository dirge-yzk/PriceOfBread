"""[計測の写しだけ] (347) 主スレッドの区間割り：MultiNationSim.StepDay を区間（Open／Daily／Close／Flush／ProbeDiff の各ラベル）に割って、
区間ごとの壁時間（Stopwatch・主スレッド）を木の終わりに POB_RF_DIR/ph347.txt へ書く（`ラベル 刻み 回数`）。
ラップ方式：ラベルの区間＝前のラップからそのラベルのラップまで。数えていない共有層の処理が台帳の外にどれだけあるかを見るための census（読むだけ）。
使い方：python inj_ph347.py <木の根>（inj_rf347.py の後に当てる＝RF347 型を使う）"""
import sys
root = sys.argv[1]


def edit(rel, pairs):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    crlf = '\r\n' in s
    s = s.replace('\r\n', '\n')
    for old, new in pairs:
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    if crlf: s = s.replace('\n', '\r\n')
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


# RF347 に Lap を足す（inj_rf347 が置いた型の末尾の閉じ括弧の前）
edit('PriceOfBread.SimCore/InternationalMarket.cs', [
    ("    public static long BilTicks, BilBytes, BilCalls;\n",
     "    public static long BilTicks, BilBytes, BilCalls;\n"
     "    static long _last; static readonly System.Collections.Generic.Dictionary<string, (long Ticks, long N)> Laps = new();\n"
     "    public static void Reset() { _last = System.Diagnostics.Stopwatch.GetTimestamp(); }\n"
     "    public static void Lap(string label)\n"
     "    {\n"
     "        long now = System.Diagnostics.Stopwatch.GetTimestamp();\n"
     "        Laps.TryGetValue(label, out var v); Laps[label] = (v.Ticks + (now - _last), v.N + 1); _last = System.Diagnostics.Stopwatch.GetTimestamp();\n"
     "    }\n"
     "    public static string DumpLaps() { var sb = new System.Text.StringBuilder(); foreach (var kv in Laps) sb.Append(kv.Key).Append('\\t').Append(kv.Value.Ticks).Append('\\t').Append(kv.Value.N).Append('\\n'); return sb.ToString(); }\n"),
    ("                $\"gcPauseSec {System.GC.GetTotalPauseDuration().TotalSeconds:F3}",
     "                $\"laps\\n{DumpLaps()}gcPauseSec {System.GC.GetTotalPauseDuration().TotalSeconds:F3}"),
])
edit('PriceOfBread.SimCore/MultiNationSim.cs', [
    ("    private void ProbeDiff(string label)\n    {\n",
     "    private void ProbeDiff(string label)\n    {\n        RF347.Lap(label);\n"),
    ("        foreach (var sim in Nations) sim.StepDayOpen();\n",
     "        RF347.Reset();\n        foreach (var sim in Nations) sim.StepDayOpen();\n        RF347.Lap(\"Open\");\n"),
    ("        if (_coef.LuxuryIncomeElasticity > 0 && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0) DistributeWorldRealIncome();",
     "        RF347.Lap(\"Daily\");\n        if (_coef.LuxuryIncomeElasticity > 0 && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0) DistributeWorldRealIncome();"),
    ("        foreach (var sim in Nations) sim.StepDayClose();\n",
     "        RF347.Lap(\"LuxDistribute\");\n        foreach (var sim in Nations) sim.StepDayClose();\n        RF347.Lap(\"Close\");\n"),
    ("        if (TimeOn) System.Threading.Interlocked.Add(ref TicksNations, System.Diagnostics.Stopwatch.GetTimestamp() - t0);\n",
     "        RF347.Lap(\"Flush\");\n        if (TimeOn) System.Threading.Interlocked.Add(ref TicksNations, System.Diagnostics.Stopwatch.GetTimestamp() - t0);\n"),
    ("            StepPorts(); ProbeDiff(\"港湾\");",
     "            RF347.Lap(\"Market.Step(四半期末)\"); StepPorts(); ProbeDiff(\"港湾\");"),
])
print('INJ_PH_OK')
