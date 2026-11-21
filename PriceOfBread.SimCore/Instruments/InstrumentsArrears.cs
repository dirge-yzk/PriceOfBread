namespace PriceOfBread.SimCore;

// (1284) c132df 計器 arrears_s：不履行の導火線（積む月）と兌換の停止の門（開く月）の観測。事前登録 measurements/m1284_20261121_prereg_df.txt。
//   全ての国・その月に〈払えない月〉の述語が立つか、旧か新の停止の門が開く月だけ 1 行。Fiscal.Step の不履行の判定の前（旗 1 の前の門より前）で読む。
//   同じ盤面で旧の規則（判定の後に r＜GoldSuspendMinFundingRatio の門）と新の規則（判定の前に払えない月の述語の門）を両方数える＝計器が国の状態から自分で組む
//   （エンジンの関数を呼ばない・旗 ArrearsOnePredicate を見ない）。エンジンから受け取るのは Fiscal.Step の引数 spare・unopenedRoom と、月初の利払い÷歳入の門 underLimit だけ
//   （Fiscal.Step の頭で読む先月の利払いと歳入は、この位置では今月の値に書き換わっている＝計器が組み直せない・限界の自己申告）。
//   力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable）。助走（WarmupFrozen）の月は数えない。
// 列：monthIndex・year・nation・law（本位の法）・onGold（月の頭）・conv（兌換）・r・underLimit・unpaidMonth（積む述語）・cover（正貨カバー）・coverTrip・
//     oldTrip・newTrip（旧と新の停止の門が今月開くか）・unpaid（1−r）・unopenedShare（まだ開いていない余地÷払えない額）・offStdArrears（金本位でない月の式の量）・
//     oldArrears・newArrears（旧と新の規則でこの月に積む量）・engineAccBefore（エンジンの累計の月の頭の値）・oldAcc・newAcc（計器の累計の影・届いたら 0 へ戻す）・
//     oldShadowDefaults・newShadowDefaults（影の累計が不履行の線に届いた回数の累計）・foreignShare（対外の割合）。
public static partial class Instruments
{
    internal const string ArrearsHeaderText = "monthIndex\tyear\tnation\tlaw\tonGold\tconv\tr\tunderLimit\tunpaidMonth\tcover\tcoverTrip\toldTrip\tnewTrip"
        + "\tunpaid\tunopenedShare\toffStdArrears\toldArrears\tnewArrears\tengineAccBefore\toldAcc\tnewAcc\toldShadowDefaults\tnewShadowDefaults\tforeignShare";

    internal sealed class ArrearsAcc
    {
        public double OldAcc, NewAcc;
        public int OldDefaults, NewDefaults, Rows;
        public readonly System.Collections.Generic.List<string> Pending = [];
        public bool LastOldTrip, LastNewTrip, LastUnpaidMonth;
        public double LastOldArrears, LastNewArrears;
    }
    sealed class ArrearsSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, ArrearsAcc> ArrearsAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ArrearsSink> ArrearsSinks = new();

    /// <summary>試験用：国の今の溜め（四半期の表へ流す前）。計器が切れている時は null。</summary>
    internal static ArrearsAcc? ArrearsPeek(World w) => ArrearsAccs.TryGetValue(w, out var a) ? a : null;

    /// <summary>Fiscal.Step の不履行の判定の前：旧と新の規則の門と積む量を、国の状態から計器が自分で数える。</summary>
    internal static void ArrearsMonth(World w, Coefficients c, double spare, double unopenedRoom, bool underLimit)
    {
        if (!On) return;
        if (w.WarmupFrozen || !Wants("arrears_s")) return;
        double r = w.FiscalFundingRatio.Value, debt = w.GovDebt.Value;
        bool unpaidMonth = r < 1 && debt > 1e-9 && !underLimit;
        bool onGold = w.OnGoldStandard, law = w.MetallicStandardLaw, money = w.MonetaryEnabled;
        double broad = w.BroadMoney.Value;
        double cover = broad > 1e-9 ? w.BaseMoney.Value / broad : 1.0;
        bool coverTrip = cover < c.GoldCoverMin.Value;
        bool oldTrip = money && onGold && (!law || r < c.GoldSuspendMinFundingRatio || coverTrip);
        bool newTrip = money && onGold && (!law || unpaidMonth || coverTrip);
        if (!unpaidMonth && !oldTrip && !newTrip) return;
        var a = ArrearsAccs.GetValue(w, _ => new ArrearsAcc());
        double unpaid = 1.0 - r;
        double gap = -spare;
        double unopenedShare = gap > 0 && unopenedRoom >= 0 ? unopenedRoom / gap : double.NaN;
        // 金本位でない月の式（(1250) の旗 ArrearsBeyondOpenedRoom＞0）＝払えない額のうち、開いた余地で払えなかった分だけ
        double offStd = unpaid;
        if (c.ArrearsBeyondOpenedRoom > 0 && gap > 0 && unopenedRoom >= 0) { double left = 1.0 - unopenedRoom / gap; offStd = left > 0 ? unpaid * left : 0.0; }
        // 旧：判定は門の前＝月の頭に金本位なら 1−r。新：門が判定の前に開いた月も金本位でない月の式。
        double oldArr = onGold ? unpaid : offStd;
        double newArr = onGold && !newTrip ? unpaid : offStd;
        double engineBefore = w.InsolvencyMonths;
        if (unpaidMonth)
        {
            a.OldAcc += oldArr; a.NewAcc += newArr;
            if (a.OldAcc >= c.DefaultArrearsMonths) { a.OldDefaults++; a.OldAcc = 0; }
            if (a.NewAcc >= c.DefaultArrearsMonths) { a.NewDefaults++; a.NewAcc = 0; }
        }
        else { a.OldAcc = 0; a.NewAcc = 0; }
        a.LastOldTrip = oldTrip; a.LastNewTrip = newTrip; a.LastUnpaidMonth = unpaidMonth; a.LastOldArrears = oldArr; a.LastNewArrears = newArr; a.Rows++;
        double fs = debt > 1e-9 ? w.ForeignDebt.Value / debt : 0.0;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string B(bool v) => v ? "1" : "0";
        var sb = new System.Text.StringBuilder();
        sb.Append(I((int)(w.Tick / Calendar.DaysPerMonth))).Append('\t').Append(I(w.Year)).Append('\t').Append(NameOf(w))
          .Append('\t').Append(I((int)w.MonetaryLaw)).Append('\t').Append(B(onGold)).Append('\t').Append(FR(w.Convertibility)).Append('\t').Append(FR(r))
          .Append('\t').Append(B(underLimit)).Append('\t').Append(B(unpaidMonth)).Append('\t').Append(FR(cover)).Append('\t').Append(B(coverTrip))
          .Append('\t').Append(B(oldTrip)).Append('\t').Append(B(newTrip));
        foreach (double v in new[] { unpaid, unopenedShare, offStd, oldArr, newArr, engineBefore, a.OldAcc, a.NewAcc }) sb.Append('\t').Append(FR(v));
        sb.Append('\t').Append(I(a.OldDefaults)).Append('\t').Append(I(a.NewDefaults)).Append('\t').Append(FR(fs));
        a.Pending.Add(sb.ToString());
    }

    static void ArrearsTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("arrears_s")) return;
        ArrearsSink? sk = null;
        foreach (var n in sim.Nations)
        {
            if (!ArrearsAccs.TryGetValue(n.World, out var a) || a.Pending.Count == 0) continue;
            sk ??= ArrearsSinks.GetValue(sim, _ => new ArrearsSink());
            if (sk.W is null) { sk.W = Open("arrears", seed); sk.W.WriteLine(ArrearsHeaderText); }
            foreach (var row in a.Pending) sk.W.WriteLine(row);
            a.Pending.Clear();
        }
    }

    static void ArrearsClose(MultiNationSim sim) { if (ArrearsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
