namespace PriceOfBread.SimCore;

// (1284) c132df 計器 arrears_s：Fiscal.Step の頭（本位の停止の門と不履行の累計の前）で、同じ盤面の旧の規則（r＜GoldSuspendMinFundingRatio で離れる・判定の後）と
// 新の規則（旗 ArrearsOnePredicate：不履行の導火線と同じ述語で離れる・判定の前）を両方数える。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable＝セーブに載らない）・エンジンの関数を呼ばない
// （上限の内か・正貨カバー・述語・月の量〔(1250) の式〕・累計は計器の算術）。受け取るのは月の局所の spare（切り下げの後の余り）と unopenedRoom だけ。
// 行＝〈導火線の述語 owes が真〉か〈金本位の月に旧か新の停止の述語が真〉の月だけ。cumOld・cumNew は計器が持つ影の累計（owes が偽の月に 0・線に届いた月の後に 0）。
public static partial class Instruments
{
    internal const string ArrearsHeaderText = "quarter\tyear\tnation\tlaw\tmonetary\tonGoldBefore\tconvertibility\tpayRate\tgovDebt\tforeignShare\tinterestToRevenue"
        + "\tunderLimit\twarm\tspare\tunopenedRoom\tcoverage\towes\tpredOld\tpredNew\tarrOldMonth\tarrNewMonth\tcumOld\tcumNew\tinsolvencyBefore\tprevInterestToRevenue";

    sealed class ArWorld { public readonly List<string> Rows = new(); public double CumOld, CumNew; public double PrevInt, PrevRev; }   // PrevInt・PrevRev＝前の月の呼びの末に見た利払いと歳入（エンジンの underLimit は Fiscal.Step の頭で前の月の値を読む）＝(1295)
    sealed class ArSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, ArWorld> ArWorlds = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ArSink> ArSinks = new();

    /// <summary>試験の読み口：その国のまだ書いていない行（無ければ null）。</summary>
    internal static IReadOnlyList<string>? ArrearsPeek(World w) => ArWorlds.TryGetValue(w, out var s) ? s.Rows : null;

    /// <summary>その月の未払い（月の請求の割合）を、本位にいるか（onGold）ごとに計器の算術で：本位の月と旗 ArrearsBeyondOpenedRoom 0 は 1−r・
    /// 本位の外の月は 1−r に (1−まだ開いていない余地÷払えない額) を掛ける（負は 0）。</summary>
    static double ArMonthAmount(Coefficients c, bool onGold, double r, double spare, double unopenedRoom)
    {
        double u = 1.0 - r;
        if (onGold || !(c.ArrearsBeyondOpenedRoom > 0) || !(-spare > 0) || !(unopenedRoom >= 0)) return u;
        double keep = 1.0 - unopenedRoom / -spare;
        return keep > 0 ? u * keep : 0;
    }

    /// <summary>受け口（Fiscal.Step の頭・毎月呼ばれ、計器が閉じていれば入口で返る）。</summary>
    internal static void ArrearsMonth(World w, Coefficients c, double spare, double unopenedRoom)
    {
        if (!On || !Wants("arrears_s")) return;   // On はこの関数の中で 1 回だけ読む（Fiscal.Step の読みを増やさない＝OnDoubleReadScanTests）
        double r = w.FiscalFundingRatio.Value, debt = w.GovDebt.Value;
        double rev = w.LastRevenuePerMonth.Value, intr = w.LastInterestPerMonth.Value;
        var s = ArWorlds.GetValue(w, _ => new ArWorld());
        bool under = s.PrevInt < c.MaxInterestShareOfRevenue.Value * s.PrevRev;   // (1295) 前の月の比（エンジンの Fiscal.cs:13 と同じ読み）。上の intr・rev は今月の値で、列 interestToRevenue に使う
        double prevRatio = s.PrevRev > 0 ? s.PrevInt / s.PrevRev : double.NaN;   // (1322) 列 prevInterestToRevenue＝前の月の呼びの末に見た利払い÷歳入（underLimit の判定に使った比・歳入≦0 は nan）
        s.PrevInt = intr; s.PrevRev = rev;
        bool warm = w.WarmupFrozen;
        bool owes = r < 1 && debt > 1e-9 && !under && !warm;
        bool mon = w.MonetaryEnabled, onGold = w.OnGoldStandard, law = w.MetallicStandardLaw;
        double broad = w.BroadMoney.Value;
        double cover = broad > 1e-9 ? w.BaseMoney.Value / broad : 1.0;
        bool coverTrip = cover < c.GoldCoverMin.Value;
        bool predOld = mon && onGold && (!law || r < c.GoldSuspendMinFundingRatio || coverTrip);
        bool predNew = mon && onGold && (!law || owes || coverTrip);
        double aOld = 0, aNew = 0;
        if (owes)
        {
            aOld = ArMonthAmount(c, onGold, r, spare, unopenedRoom);                 // 旧：離れる判定は累計の後＝この月の本位は今のまま
            aNew = ArMonthAmount(c, onGold && !predNew, r, spare, unopenedRoom);     // 新：先に離れた後の本位で積む
            s.CumOld += aOld; s.CumNew += aNew;
        }
        else { s.CumOld = 0; s.CumNew = 0; }
        if (owes || predOld || predNew)
        {
            static string B(bool b) => b ? "1" : "0";
            s.Rows.Add(string.Join('\t', F(w.Year), NameOf(w), F((int)w.MonetaryLaw), B(mon), B(onGold), F(w.Convertibility), F(r), F(debt),
                F(debt > 0 ? w.ForeignDebt.Value / debt : double.NaN), F(rev > 0 ? intr / rev : double.NaN), B(under), B(warm), F(spare), F(unopenedRoom), F(cover),
                B(owes), B(predOld), B(predNew), F(aOld), F(aNew), F(s.CumOld), F(s.CumNew), F(w.InsolvencyMonths), F(prevRatio)));
        }
        if (s.CumOld >= c.DefaultArrearsMonths) s.CumOld = 0;
        if (s.CumNew >= c.DefaultArrearsMonths) s.CumNew = 0;
    }

    /// <summary>四半期の末：全ての国のその四半期の行を書いて空にする。</summary>
    static void ArrearsTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("arrears_s")) return;
        var sk = ArSinks.GetValue(sim, _ => new ArSink());
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            if (!ArWorlds.TryGetValue(n.World, out var s) || s.Rows.Count == 0) continue;
            if (sk.W is null) { sk.W = Open("arrears", seed); sk.W.WriteLine(ArrearsHeaderText); }
            foreach (var row in s.Rows) { sk.W.Write(qs); sk.W.Write('\t'); sk.W.WriteLine(row); }
            s.Rows.Clear();
        }
    }

    static void ArrearsClose(MultiNationSim sim) { if (ArSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
