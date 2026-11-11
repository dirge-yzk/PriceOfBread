// (1049) E1 計り台：1 プロセスで 1 構成・全長 1 種。リポジトリのコードは変えない（LbOffForTest は反射で立てる・POB_COEF は呼び手が環境変数で渡す）。
using System.Diagnostics;
using System.Reflection;
using PriceOfBread.SimCore;

string label = args.Length > 0 ? args[0] : "A";
ulong seed = args.Length > 1 ? ulong.Parse(args[1]) : 1UL;
int quarters = args.Length > 2 ? int.Parse(args[2]) : Coefficients.GameQuarters;
bool lbOff = label.StartsWith("B");

var fld = typeof(InternationalMarket).GetField("LbOffForTest", BindingFlags.NonPublic | BindingFlags.Static)
          ?? throw new InvalidOperationException("LbOffForTest が見つからない");
fld.SetValue(null, lbOff);
bool readBack = (bool)fld.GetValue(null)!;

var proc = Process.GetCurrentProcess();
proc.Refresh();
var cpu0 = proc.TotalProcessorTime;
var t0 = DateTime.Now;
var sw = Stopwatch.StartNew();
var r = SelfPlayMatch.Run(seed, quarters, null, lean: true);
sw.Stop();
proc.Refresh();
var cpu1 = proc.TotalProcessorTime;
long[] c = r.Cost ?? new long[CostLedger.Count];
double est = CostLedger.EstimatedSeconds(c);
// 軌道の札：清算の 2 行（ClearingFills・ClearingSkipsLB）以外の台帳の件数の和と勝者・決着の四半期（A と B は同じになるはず）
long otherSum = 0;
for (int i = 0; i < c.Length; i++) if (i != CostLedger.ClearingFills && i != CostLedger.ClearingSkipsLB) otherSum += c[i];
string coef = Environment.GetEnvironmentVariable("POB_COEF") ?? "";
Console.WriteLine($"label={label} seed={seed} q={quarters} lbOff={readBack} coef=[{coef}] start={t0:HH:mm:ss} wall_s={sw.Elapsed.TotalSeconds:F2} cpu_s={(cpu1 - cpu0).TotalSeconds:F2} est_s={est:F3} fills={c[CostLedger.ClearingFills]} lbEval={c[CostLedger.ClearingSkipsLB]} otherSum={otherSum} winner={r.Winner} decisionQ={r.DecisionQuarter}");
// 台帳の行ごとの件数（帰属を読むため）
Console.WriteLine("ledger " + string.Join(",", c));
