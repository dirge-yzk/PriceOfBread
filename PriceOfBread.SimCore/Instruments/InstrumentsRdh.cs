namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (721) 線 rdh：名簿の外の空洞化の原因の表 rdh_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPrdf.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは Region を鍵の弱い表）。
// 書くのは環境変数 POB_INSTRUMENTS_RDH が空でないときだけ（INS=1 の中だけ＝Instruments.On）。INS=1 でも RDH が空なら 1 行も書かない（他の表は 1 バイトも変わらない）。
// 行＝全ての国の全ての州（挙げた国・本国の格で絞らない＝バーミンガムがネーデルラント領 int・オムスクがハイデラバード領のときも出る）。
//   kind O＝構築の直後（Phase open）の州の断面（人口・農村・その瞬間の〈移る値打ち〉1 回）／W＝助走（WarmupFrozen の月）の和を助走の直後（Phase warm）に書く／Q＝四半期の和（四半期の末）。
//   駆動の列（months 以下）＝PopDecision.Run の直前に、Run と同じ式（Instruments.UrxPdDrive＝流れと門・PopDecision.MigrationValues＝V と g）で取った月ごとの値の和。
//     gPosMonths／gNegMonths＝g の符号の月（g＝0 は gZeroMonths）・gSum＝Σg（月の平均は ÷months）・vRuralSum／vUrbanSum＝1 人・月の V の和・earnRuralSum／earnUrbanSum＝1 人・月の現金の所得の和（負のまま）・
//     subSum＝1 人・月の自給の値の和・housingSum＝1 人・月の家賃の和・vUrbanClipMonths＝V都市が 0 で切れた月（現金−家賃≦0）・flowToUrbanK／flowToRuralK＝流れ（門の後・千人）の和・
//     serfGateMonths／famineGateMonths＝農奴の門・飢えの門が流れを縛った月・noPairMonths＝農村と都市の対が無い月。
//   人口の列（birthsK・deathsNormalK・deathsFamineK）＝DemographicStep の POP のループの hook（DemoPop・DemoFamine と同じ値）を州へ足した和。兵士の POP は出生と死の外（本体と同じ）。
// ⚠限界（計器が自己申告する）：①O の行の V は構築の直後の欄（前の月の所得・自給の値は 0 のまま）で、助走の最初の月の V とは違う ②流れは Run の直前の盤面の式＝同じ月の後の書き手（社会移動・徴兵）の移しは入らない
//   ③疫病・戦死・鎮圧は epi_s ほか別の表（ここの人口の列に入らない）④州の格・持ち主は行を書いた瞬間の値（四半期の途中の割譲は四半期の末の持ち主へ寄せる）。
public static partial class Instruments
{
    /// <summary>rdh_s を書くか（環境変数 POB_INSTRUMENTS_RDH・番人が書き換える）。呼び手は <see cref="On"/> と両方を見る。</summary>
    internal static bool RdhOn = Environment.GetEnvironmentVariable("POB_INSTRUMENTS_RDH") is { Length: > 0 };

    internal const string RdhHeaderText = "kind\tquarter\tyear\tnation\tregion\ttier\tpopulationK\truralPopulationK"
        + "\tmonths\tgPosMonths\tgNegMonths\tgZeroMonths\tgSum\tvRuralSum\tvUrbanSum\tearnRuralSum\tearnUrbanSum\tsubSum\thousingSum\tvUrbanClipMonths"
        + "\tflowToUrbanK\tflowToRuralK\tserfGateMonths\tfamineGateMonths\tnoPairMonths\tbirthsK\tdeathsNormalK\tdeathsFamineK";

    internal sealed class RdhAcc
    {
        public int Months, GPos, GNeg, GZero, VuClip, Serf, Famine, NoPair;
        public double GSum, VrSum, VuSum, EarnRSum, EarnUSum, SubSum, HousSum, FlowToUrban, FlowToRural, Births, DeathsNormal, DeathsFamine;
        public void Clear()
        {
            Months = GPos = GNeg = GZero = VuClip = Serf = Famine = NoPair = 0;
            GSum = VrSum = VuSum = EarnRSum = EarnUSum = SubSum = HousSum = FlowToUrban = FlowToRural = Births = DeathsNormal = DeathsFamine = 0;
        }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, RdhAcc> RdhAccs = new();
    static RdhAcc RdhOf(Region r) => RdhAccs.GetValue(r, _ => new RdhAcc());
    /// <summary>番人が蓄えを読む口（積みは消さない）。</summary>
    internal static RdhAcc RdhPeek(Region r) => RdhOf(r);

    /// <summary>DemographicStep の州のループの今の州（同じスレッドの中だけ＝国の月は別のスレッドで回りうる）。null＝州の外（番人が hook を直に呼ぶとき）。</summary>
    [ThreadStatic] static Region? RdhCurRegion;

    /// <summary>DemographicStep の州のループの頭と終わり（読むだけ）：出生と死の hook（<see cref="DemoPop"/>・<see cref="DemoFamine"/>）を足す州。</summary>
    internal static void RdhRegion(Region? r) { if (RdhOn) RdhCurRegion = r; }

    static void RdhDemo(double births, double deathsNormal, double deathsFamine)
    {
        if (!RdhOn || RdhCurRegion is not { } r) return;
        var a = RdhOf(r); a.Births += births; a.DeathsNormal += deathsNormal; a.DeathsFamine += deathsFamine;
    }

    /// <summary>PopDecision.Run の直前（読むだけ・助走の月も）：Run と同じ式で、その州のその月の〈移る値打ち〉・g・流れ・門を積む。</summary>
    internal static void RdhPd(World w, Region region, double serfFree)
    {
        if (!RdhOn) return;
        var a = RdhOf(region);
        var c = w.Coef;
        if (!UrxPdDrive(c, region, serfFree, out var d)) { a.NoPair++; return; }
        var (rural, urban) = PopDecision.MigrationPair(region);
        var mv = PopDecision.MigrationValues(region, c, rural!, urban!);
        a.Months++;
        if (mv.Gsym > 0) a.GPos++; else if (mv.Gsym < 0) a.GNeg++; else a.GZero++;
        a.GSum += mv.Gsym; a.VrSum += mv.VRural; a.VuSum += mv.VUrban;
        a.EarnRSum += d.IncRural; a.EarnUSum += d.IncUrban; a.SubSum += mv.SubsistenceValue; a.HousSum += mv.HousingCost;
        if (!(mv.VUrban > 0)) a.VuClip++;
        if (d.Flow > 0) a.FlowToUrban += d.Flow; else if (d.Flow < 0) a.FlowToRural += -d.Flow;
        if (d.SerfGated) a.Serf++;
        if (d.FamineGated) a.Famine++;
    }

    sealed class RdhSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, RdhSink> RdhSinks = new();

    static void RdhRows(MultiNationSim sim, ulong seed, string kind, int q)
    {
        if (!RdhOn) return;
        var sk = RdhSinks.GetValue(sim, _ => new RdhSink());
        sk.W ??= Open("rdh", seed);
        if (!sk.Header) { sk.W.WriteLine(RdhHeaderText); sk.Header = true; }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string I(int v) => v.ToString(inv);
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = NameOf(w);
            foreach (var r in w.Regions)
            {
                double pop = 0, rural = 0;
                foreach (var p in r.Pops) { pop += p.Size; if (!p.IsUrban) rural += p.Size; }
                var a = RdhOf(r);
                if (kind == "O")
                {
                    // 構築の直後の断面＝その瞬間の V と g を 1 回だけ（流れ・門は助走の W の行が持つ）
                    a.Clear();
                    var (ru, ur) = PopDecision.MigrationPair(r);
                    if (r.Pops.Count >= 2 && ru is not null && ur is not null && ru.Size + ur.Size > 1e-9)
                    {
                        var mv = PopDecision.MigrationValues(r, w.Coef, ru, ur);
                        a.Months = 1;
                        if (mv.Gsym > 0) a.GPos = 1; else if (mv.Gsym < 0) a.GNeg = 1; else a.GZero = 1;
                        a.GSum = mv.Gsym; a.VrSum = mv.VRural; a.VuSum = mv.VUrban; a.SubSum = mv.SubsistenceValue; a.HousSum = mv.HousingCost;
                        a.EarnRSum = ru.Size > 1e-9 ? ru.EarnedPerMonth.Value / ru.Size : 0.0; a.EarnUSum = ur.Size > 1e-9 ? ur.EarnedPerMonth.Value / ur.Size : 0.0;
                        if (!(mv.VUrban > 0)) a.VuClip = 1;
                    }
                    else a.NoPair = 1;
                }
                sk.W.WriteLine(string.Join('\t', kind, I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), F(pop), F(rural),
                    I(a.Months), I(a.GPos), I(a.GNeg), I(a.GZero), F(a.GSum), F(a.VrSum), F(a.VuSum), F(a.EarnRSum), F(a.EarnUSum), F(a.SubSum), F(a.HousSum), I(a.VuClip),
                    F(a.FlowToUrban), F(a.FlowToRural), I(a.Serf), I(a.Famine), I(a.NoPair), F(a.Births), F(a.DeathsNormal), F(a.DeathsFamine)));
                a.Clear();
            }
        }
    }

    /// <summary>断面（<see cref="Phase"/>）：open は O の行・warm は助走の和の W の行。</summary>
    static void RdhPhase(MultiNationSim sim, ulong seed, string time)
    {
        if (time == "open") RdhRows(sim, seed, "O", -1);
        else if (time == "warm") RdhRows(sim, seed, "W", -1);
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：Q の行。</summary>
    static void RdhTable(MultiNationSim sim, ulong seed, int q) => RdhRows(sim, seed, "Q", q);

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void RdhClose(MultiNationSim sim)
    {
        if (RdhSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
