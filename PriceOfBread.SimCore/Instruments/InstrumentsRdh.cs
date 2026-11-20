namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは Region を鍵の弱い表）。〔履歴 InstrumentsRdh.cs#1〕
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
        if (!Tg.Lwk) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
        if (!Tg.Lwk) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (time == "open") RdhRows(sim, seed, "O", -1);
        else if (time == "warm") RdhRows(sim, seed, "W", -1);
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：Q の行。</summary>
    static void RdhTable(MultiNationSim sim, ulong seed, int q) => RdhRows(sim, seed, "Q", q);

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void RdhClose(MultiNationSim sim)
    {
        if (!Tg.Lwk) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (RdhSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
