namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (514) 賃金の天井の表 wcap_s・wcapreg_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLw.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない（ConditionalWeakTable の蓄え＝セーブに載らない）。INS=1 のときだけ動く。
// 目的＝線 wc（賃金の天井 MarketFlow.EffWageMax を外すか）が要るかを測る。事前登録 measurements/wc0_20261023_prereg.txt。
// 読む位置＝月の決算の終わり（Instruments.MonthClose）＝その月の旬の歩（MarketFlow.StepDay）が書き終えた値段。助走の月は読まない。
// 役務（Tier==Service）の財を 7 つのくくりで読む：labor（工場労働）・advlabor（高度労働力）・farmlabor（農業労働）・education・healthcare・highered・other（行政・上下水道・電化・家事奉仕ほか）。
//   歩は（需要も産出も Eps 未満の死んだ財を除いて）全ての役務の値段を同じ帯 SoftBandLn(want, wrp, −ln WageClampMin, ln EffWageMax, k) に通す＝帯の位置を s で測る。
// 量の定義：
//   wrp＝参照価格×州の物価水準・u＝ln(値段/wrp)・s＝u/ln(EffWageMax)（天井側・u≥0）または u/(−ln WageClampMin)（床側・u<0）＝歩の出力の正規化距離（−1〜1、帯の内側 |s|<0.7 はほぼ恒等）。
//   c70/c90/c95＝s が 0.7/0.9/0.95 以上の月×州の割合（天井側）・f70/f90＝s が −0.7/−0.9 以下（床側）・over＝s≥1（物価水準が後から動いて帯を越えた）。
//   damp＝歩が上へ押す力の通る割合（SoftBand の導関数 (1+t^k)^−(1+1/k)・t＝出力 s の逆〔(s^−k −1)^(−1/k)〕）＝s=0.7 で約 1.0・0.9 で約 0.6・0.95 で約 0.1。縛りが本物かの指標（c70 に入った月×州の平均）。
//   flow＝歩と同じ式の需給差 (D−S)/(D+S+Eps)（S＝max(流入, min(流入+在庫, D))）。wantUp90＝s≥0.9 で flow>0.02（値段がまだ上へ押されている）月×州の割合＝天井が本当に噛んでいる月。
//   ⚠限界：①月末の断面（月の中の旬の最大を見ない）②値段は後ろの歩の出力＝押し戻された後の値であって、天井が無ければ付いた値は復元できない（damp と flow で押し戻しの強さを読む）③人口の重みは州の POP の Size の和
//   ④労働 3 財の床側は LaborBandFloorOffGate が正のとき帯を通らない（参照より下は漸近形を通さない）＝f70/f90 は labor/advlabor/farmlabor では意味を持たない（報告は教育・医療・高等教育の床だけを読む）。
public static partial class Instruments
{
    internal const string WcHeaderText = "quarter\tyear\tnation\tgroup\tliveRegionMonths\tcN\tc70Share\tc90Share\tc95Share\toverShare\tc70PopShare\tc90PopShare\twantUp90Share\tdampMeanC70\tsMeanCeilSide\tmaxS\tmaxRatio\tmaxRegion\tf70Share\tf90Share\tf70PopShare\tmeanFlow\tceilMult";
    internal const string WcRegHeaderText = "quarter\tyear\tnation\tregionId\tregion\tgroup\tpopAvg\tliveMonths\tc70Months\tc90Months\tf70Months\tf90Months\tmaxS\tmaxRatio\tflowMean";
    static readonly string[] WcGroups = ["labor", "advlabor", "farmlabor", "education", "healthcare", "highered", "other"];

    static int WcGroupOf(int gi) => gi == Catalog.LaborId ? 0 : gi == Catalog.AdvLaborId ? 1 : gi == Catalog.FarmLaborId ? 2
        : gi == Catalog.EducationId ? 3 : gi == Catalog.HealthcareId ? 4 : gi == Catalog.HigherEducationId ? 5 : 6;

    /// <summary>国×くくりの四半期の蓄え（月ごとに足し、WcFold が行にして 0 へ戻す）。</summary>
    private sealed class WcGroupAcc
    {
        public long Live, C70, C90, C95, Over, F70, F90, WantUp90, DampN;
        public double PopLive, PopC70, PopC90, PopF70, DampSum, SCeilSum, MaxS = double.NegativeInfinity, MaxRatio, FlowSum, CeilMultSum;
        public string MaxRegion = "";
    }

    /// <summary>州×くくりの四半期の蓄え（天井側 s≥0.7 か床側 s≤−0.7 になった月が 1 回でもあれば行にする）。</summary>
    private sealed class WcRegAcc
    {
        public int RegionId; public string Region = ""; public int Group;
        public long Live, C70, C90, F70, F90;
        public double PopSum, MaxS = double.NegativeInfinity, MaxRatio, FlowSum;
    }

    private sealed class WcState
    {
        public readonly WcGroupAcc[] G = new WcGroupAcc[WcGroups.Length];
        public readonly Dictionary<(int Region, int Group), WcRegAcc> R = new();
        public WcState() { for (int i = 0; i < G.Length; i++) G[i] = new WcGroupAcc(); }
        public void Clear() { for (int i = 0; i < G.Length; i++) G[i] = new WcGroupAcc(); R.Clear(); }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, WcState> WcStates = new();

    private const double WcEps = 1e-9;   // MarketFlow.Eps（private）と同じ値＝歩が死んだ財を continue する判定の写し

    /// <summary>月の決算の終わり：その月の末の役務の値段を、歩の帯の位置 s で読んで蓄える。読むだけ。</summary>
    internal static void WcMonth(World w)
    {
        if (w.WarmupFrozen) return;
        var c = w.Coef; double k = c.SoftBandSharpness;
        var ws = WcStates.GetValue(w, _ => new WcState());
        double lnFloor = -System.Math.Log(c.WageClampMin);
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double pop = 0; foreach (var p in r.Pops) pop += p.Size;
            double ceilMult = MarketFlow.EffWageMax(r, c);
            double lnCeil = System.Math.Log(ceilMult);
            double pl = r.PriceLevel;
            foreach (var good in r.Goods)
            {
                if (good.Tier != GoodTier.Service) continue;
                int gi = good.Id; var m = r.Markets[gi];
                double d = m.LastDemand;
                if (d < WcEps && m.LastProduced < WcEps) continue;   // 歩が動かさない死んだ財
                double wrp = good.RefPrice * pl;
                if (!(wrp > 0) || !(m.Price > 0) || !(lnCeil > WcEps) || !(lnFloor > WcEps)) continue;
                double u = System.Math.Log(System.Math.Max(m.Price, WcEps) / wrp);
                double s = u >= 0 ? u / lnCeil : u / lnFloor;
                double flowIn = m.LastProduced + m.LastImport;
                double supply = System.Math.Max(flowIn, System.Math.Min(flowIn + m.Stock, d));
                double flow = (d - supply) / (d + supply + WcEps);
                int g = WcGroupOf(gi);
                var a = ws.G[g];
                a.Live++; a.PopLive += pop; a.FlowSum += flow; a.CeilMultSum += ceilMult;
                if (s > 0) a.SCeilSum += s;
                if (s > a.MaxS) { a.MaxS = s; a.MaxRatio = m.Price / wrp; a.MaxRegion = r.Name; }
                if (s >= 0.7)
                {
                    a.C70++; a.PopC70 += pop;
                    // 歩が上へ押す力の通る割合（出力 s から入力 t を戻す）。s≥1 は 0
                    double damp = 0;
                    if (s < 1) { double t = System.Math.Pow(System.Math.Pow(s, -k) - 1, -1.0 / k); damp = System.Math.Pow(1 + System.Math.Pow(t, k), -(1 + 1.0 / k)); }
                    a.DampSum += damp; a.DampN++;
                    if (s >= 0.9) { a.C90++; a.PopC90 += pop; if (flow > 0.02) a.WantUp90++; }
                    if (s >= 0.95) a.C95++;
                    if (s >= 1) a.Over++;
                }
                else if (s <= -0.7)
                {
                    a.F70++; a.PopF70 += pop;
                    if (s <= -0.9) a.F90++;
                }
                if (s >= 0.7 || s <= -0.7)
                {
                    if (!ws.R.TryGetValue((r.Id, g), out var ra)) ws.R[(r.Id, g)] = ra = new WcRegAcc { RegionId = r.Id, Region = r.Name, Group = g };
                    ra.Live++;   // 行にするのは縛った月（|s|≥0.7）がある州×くくりだけ＝Live はその月数（行の bindMonths の分母）
                    if (s >= 0.7) ra.C70++; if (s >= 0.9) ra.C90++; if (s <= -0.7) ra.F70++; if (s <= -0.9) ra.F90++;
                    ra.PopSum += pop; ra.FlowSum += flow;
                    if (System.Math.Abs(s) > System.Math.Abs(ra.MaxS) || double.IsNegativeInfinity(ra.MaxS)) { ra.MaxS = s; ra.MaxRatio = m.Price / wrp; }
                }
            }
        }
    }

    /// <summary>四半期の終わり：国×くくりの行と、縛った州の行を書き、蓄えを 0 へ戻す（出さない国の蓄えも戻す）。</summary>
    static void WcFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!WcStates.TryGetValue(w, out var ws)) return;
        if ((picked || yearEnd) && w.TotalPopulation > 0)
        {
            sk.Wc ??= Open("wcap", seed);
            if (!sk.WcHeader) { sk.Wc.WriteLine(WcHeaderText); sk.WcHeader = true; }
            sk.WcReg ??= Open("wcapreg", seed);
            if (!sk.WcRegHeader) { sk.WcReg.WriteLine(WcRegHeaderText); sk.WcRegHeader = true; }
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string qs = q.ToString(inv), ys = w.Year.ToString(inv), nm = NameOf(w);
            for (int g = 0; g < WcGroups.Length; g++)
            {
                var a = ws.G[g];
                if (a.Live == 0) continue;
                double n = a.Live;
                var cols = new[]
                {
                    qs, ys, nm, WcGroups[g], a.Live.ToString(inv), a.C70.ToString(inv), F(a.C70 / n), F(a.C90 / n), F(a.C95 / n), F(a.Over / n),
                    F(a.PopLive > 0 ? a.PopC70 / a.PopLive : 0.0), F(a.PopLive > 0 ? a.PopC90 / a.PopLive : 0.0), F(a.WantUp90 / n),
                    F(a.DampN > 0 ? a.DampSum / a.DampN : double.NaN), F(a.SCeilSum / n), F(a.MaxS), F(a.MaxRatio), a.MaxRegion,
                    F(a.F70 / n), F(a.F90 / n), F(a.PopLive > 0 ? a.PopF70 / a.PopLive : 0.0), F(a.FlowSum / n), F(a.CeilMultSum / n),
                };
                sk.Wc.WriteLine(string.Join('\t', cols));
            }
            foreach (var ra in ws.R.Values.OrderBy(x => x.RegionId).ThenBy(x => x.Group))
            {
                var cols = new[]
                {
                    qs, ys, nm, ra.RegionId.ToString(inv), ra.Region, WcGroups[ra.Group], F(ra.PopSum / ra.Live), ra.Live.ToString(inv),
                    ra.C70.ToString(inv), ra.C90.ToString(inv), ra.F70.ToString(inv), ra.F90.ToString(inv), F(ra.MaxS), F(ra.MaxRatio), F(ra.FlowSum / ra.Live),
                };
                sk.WcReg.WriteLine(string.Join('\t', cols));
            }
        }
        ws.Clear();
    }
}
