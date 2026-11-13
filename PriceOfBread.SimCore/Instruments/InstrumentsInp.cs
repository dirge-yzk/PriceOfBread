namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない。〔履歴 InstrumentsInp.cs#1〕
public static partial class Instruments
{
    internal const double InpEps = 1e-9;   // EconomyStep.Eps と同じ値
    internal const double InpFillOk = 0.95;
    internal const int InpOk = 0, InpLabor = 1, InpSvc = 2, InpWorldUnreleased = 3, InpNationUnreleased = 4, InpWorldNoSeller = 5, InpReachLack = 6, InpNoOneAsks = 7, InpContF = 8, InpContP = 9, InpContI = 10, InpContM = 11, InpZeroBase = 100;
    internal static readonly string[] InpReasonNames = ["ok", "labor", "svcAlloc", "worldUnreleased", "nationUnreleased", "worldNoSeller", "reachLack", "noOneAsks", "contested.f", "contested.p", "contested.i", "contested.m"];
    internal const string InpfHeaderText = "year\tnation\tgood\treason\tmonths\tnExtreme\tn05\tn30\tn70\tdesiredRef\tlossRef\toutputRef\tunsupportedRef\tnProvinces\ttopProvince\ttopShare\tnTied";
    internal const string InpbHeaderText = "year\tnation\tbindingGood\treason\tmonths\tsumAvailable\tsumCoreDemand\tsumPop\tsumFactory\tsumInvest\tsumMilitary\tproducerCountMean\tworldProducers\tnationOtherSurplus\timportLast\tprice\tpriceOverRef";

    internal static string InpReasonName(int code) => code >= InpZeroBase ? "zeroDesired." + InpReasonNames[code - InpZeroBase] : InpReasonNames[code];

    internal sealed class InpRow
    {
        public long Months, NExtreme, N05, N30, N70, NTied;
        public double Desired, Loss, Output, Unsupported;
        public readonly Dictionary<int, int> Prov = new();
    }
    internal sealed class InpBRow
    {
        public long Months;
        public double Avail, Core, Pop, Fac, Inv, Mil, ProducerCount, WorldProducers, Nos, Import, Price, PriceOverRef;
    }

    /// <summary>国ごと（World ごと）の蓄え。ConditionalWeakTable＝セーブに載らない・シミュの状態ではない。</summary>
    internal sealed class InpState
    {
        public double[] Avail = [], Core = [], Pop = [], Fac = [], Inv = [], Mil = [], Nos = [];
        public bool[] Svc = [];
        public double[] GoodGate = [], DemandGate = [];
        public int[] ProducerCount = [];
        public int[] WorldProducers = [];   // 世界の各財の〈産出＞0 の生産者が居る州の数〉（Phase／Quarter の頭に作る）
        public World? W; public Region? Region;
        public readonly Dictionary<(int Good, int Reason), InpRow> Rows = new();
        public readonly Dictionary<(int Good, int Reason), InpBRow> BRows = new();
        public long C2Checked, C2Bad, LowMonths;

        public void Begin(World w, Region region, int ng, double[] goodGate, double[] demandGate, int[] producerCount)
        {
            if (Avail.Length != ng) { Avail = new double[ng]; Core = new double[ng]; Pop = new double[ng]; Fac = new double[ng]; Inv = new double[ng]; Mil = new double[ng]; Nos = new double[ng]; Svc = new bool[ng]; }
            else { Array.Clear(Avail); Array.Clear(Core); Array.Clear(Pop); Array.Clear(Fac); Array.Clear(Inv); Array.Clear(Mil); Array.Clear(Svc); }
            Array.Fill(Nos, double.NaN);
            W = w; Region = region; GoodGate = goodGate; DemandGate = demandGate; ProducerCount = producerCount;
        }

        /// <summary>EconomyStep の財ごとの配給の直後：その財の在庫＋産出と需要の内訳（読むだけ）。</summary>
        public void Good(int gi, double available, double coreDemand, double popDemand, double factoryDemand, double investDemand, double militaryDemand)
        {
            Avail[gi] = available; Core[gi] = coreDemand; Pop[gi] = popDemand; Fac[gi] = factoryDemand; Inv[gi] = investDemand; Mil[gi] = militaryDemand;
        }

        /// <summary>役務の優先配分が最終の配給率を書いた財の印。</summary>
        public void SvcMark(int gi) { Svc[gi] = true; }

        double OtherSurplus(int g)
        {
            if (!double.IsNaN(Nos[g])) return Nos[g];
            double sum = 0;
            if (W is not null)
                foreach (var r in W.Regions)
                {
                    if (ReferenceEquals(r, Region) || g >= r.Markets.Length) continue;   // ReferenceEquals〔履歴 InstrumentsInp.cs#2〕
                    var m = r.Markets[g];
                    double v = m.Stock + m.LastProduced - m.LastDemand;
                    if (v > 0) sum += v;
                }
            return Nos[g] = sum;
        }

        bool WorldHasSeller(int g) => g < WorldProducers.Length && WorldProducers[g] > 0;

        /// <summary>束縛する投入 g* の理由（上から順に最初の 1 つ）。</summary>
        public int Classify(int g)
        {
            if (Catalog.IsLaborGood(g)) return InpLabor;
            if (Svc[g]) return InpSvc;
            if (Avail[g] <= InpEps)
            {
                if (!(DemandGate[g] > 0)) return InpWorldUnreleased;
                if (!(GoodGate[g] > 0)) return InpNationUnreleased;
                if (ProducerCount[g] == 0 && !WorldHasSeller(g)) return InpWorldNoSeller;
                return InpReachLack;
            }
            if (!(Core[g] > 1e-12)) return InpNoOneAsks;
            double f = Fac[g], p = Pop[g], i = Inv[g], m = Mil[g];
            if (f >= p && f >= i && f >= m) return InpContF;
            if (p >= i && p >= m) return InpContP;
            return i >= m ? InpContI : InpContM;
        }

        /// <summary>EconomyStep の生産者の充足の更新の直後（LastInputFill を書いた後）：この工場×月の行を溜める。眠った生産者は呼ばれない。</summary>
        public void Factory(Producer s, double[] ration)
        {
            if (s.Inputs.Count == 0 || s.Bankrupt) return;
            double fill = 1.0;
            foreach (var inp in s.Inputs) fill = Math.Min(fill, ration[inp.GoodId]);
            C2Checked++;
            if (!(fill == s.LastInputFill)) C2Bad++;
            int pg = s.GoodId;
            double refP = pg >= 0 && pg < Catalog.RefPrices.Length ? Catalog.RefPrices[pg] : 0;
            double desired = s.DesiredOutput;
            bool zero = !(desired > 0);
            int reason = InpOk, gStar = -1; bool tied = false;
            if (fill < InpFillOk)
            {
                LowMonths++;
                int firstAny = -1, firstNonLabor = -1; int firstReason = -1;
                foreach (var inp in s.Inputs)
                {
                    if (!(ration[inp.GoodId] == fill)) continue;
                    if (firstAny < 0) firstAny = inp.GoodId;
                    if (firstNonLabor < 0 && !Catalog.IsLaborGood(inp.GoodId)) firstNonLabor = inp.GoodId;
                    int rc = Classify(inp.GoodId);
                    if (firstReason < 0) firstReason = rc; else if (rc != firstReason) tied = true;
                }
                gStar = firstNonLabor >= 0 ? firstNonLabor : firstAny >= 0 ? firstAny : s.Inputs[0].GoodId;
                reason = Classify(gStar);
            }
            int key = reason + (zero ? InpZeroBase : 0);
            var k = (reason == InpOk ? -1 : pg, key);
            if (!Rows.TryGetValue(k, out var row)) Rows[k] = row = new InpRow();
            row.Months++;
            if (fill < 0.05) row.NExtreme++; else if (fill < 0.3) row.N05++; else if (fill < 0.7) row.N30++; else if (fill < InpFillOk) row.N70++;
            if (tied) row.NTied++;
            row.Desired += desired * refP; row.Loss += desired * (1.0 - fill) * refP; row.Output += s.Output * refP; row.Unsupported += s.Output * (1.0 - fill) * refP;
            if (Region is not null) { row.Prov.TryGetValue(Region.Id, out int c); row.Prov[Region.Id] = c + 1; }
            if (reason != InpOk && !zero)
            {
                var bk = (gStar, reason);
                if (!BRows.TryGetValue(bk, out var b)) BRows[bk] = b = new InpBRow();
                b.Months++;
                b.Avail += Avail[gStar]; b.Core += Core[gStar]; b.Pop += Pop[gStar]; b.Fac += Fac[gStar]; b.Inv += Inv[gStar]; b.Mil += Mil[gStar];
                b.ProducerCount += ProducerCount[gStar]; b.WorldProducers += gStar < WorldProducers.Length ? WorldProducers[gStar] : 0;
                b.Nos += OtherSurplus(gStar);
                if (Region is not null && gStar < Region.Markets.Length)
                {
                    var m = Region.Markets[gStar];
                    b.Import += m.LastImport; b.Price += m.Price;
                    double rp = gStar < Catalog.RefPrices.Length ? Catalog.RefPrices[gStar] : 0;
                    b.PriceOverRef += rp > 0 ? m.Price / rp : 0;
                }
            }
        }
    }

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, InpState> Inps = new();

    /// <summary>
    /// 計器が止まっている・助走の月・挙げていない国は null。〔履歴 InstrumentsInp.cs#3〕
    /// </summary>
    internal static InpState? InpBegin(World w, Region region, int ng, double[] goodGate, double[] demandGate, int[] producerCount)
    {
        if (!On || w.WarmupFrozen) return null;
        var st = S(w); if (!IsPicked(w, st)) return null;
        var ip = Inps.GetValue(w, _ => new InpState());
        ip.Begin(w, region, ng, goodGate, demandGate, producerCount);
        return ip;
    }

    /// <summary>試験用：この世界に蓄えが作られたか（INS=0 では作られない）。</summary>
    internal static bool InpHasState(World w) => Inps.TryGetValue(w, out _);

    /// <summary>世界の各財の〈産出＞0 の生産者が居る州の数〉を全ての国の蓄えへ配る（Phase の助走の直後と各 Quarter の頭）。</summary>
    internal static void InpWorldTable(MultiNationSim sim)
    {
        if (!On) return;
        int ng = Catalog.Goods.Length;
        var wp = new int[ng];
        var seen = new int[ng]; int stamp = 0;
        foreach (var n in sim.Nations)
            foreach (var rg in n.World.Regions)
            {
                stamp++;
                foreach (var p in rg.Producers)
                {
                    int g = p.GoodId;
                    if (g < 0 || g >= ng || !(p.Output > 0) || seen[g] == stamp) continue;
                    seen[g] = stamp; wp[g]++;
                }
            }
        foreach (var n in sim.Nations) Inps.GetValue(n.World, _ => new InpState()).WorldProducers = wp;
    }

    static void InpFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!picked || !yearEnd || !Inps.TryGetValue(w, out var ip) || ip.C2Checked == 0) return;
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nm = NameOf(w), year = I(w.Year);
        sk.Inpf ??= Open("inpf", seed); sk.Inpb ??= Open("inpb", seed);
        if (!sk.InpHeader) { sk.Inpf.WriteLine(InpfHeaderText); sk.Inpb.WriteLine(InpbHeaderText); sk.InpHeader = true; }
        long total = 0; foreach (var r in ip.Rows.Values) total += r.Months;
        var dropped = new InpRow();
        foreach (var kv in ip.Rows.OrderBy(x => x.Key.Good).ThenBy(x => x.Key.Reason))
        {
            var r = kv.Value;
            if (r.Months < 0.001 * total && kv.Key.Reason % InpZeroBase != InpOk)   // ok の行（ok・zeroDesired.ok）は畳まない＝C1 の和が合う
            {
                dropped.Months += r.Months; dropped.NExtreme += r.NExtreme; dropped.N05 += r.N05; dropped.N30 += r.N30; dropped.N70 += r.N70; dropped.NTied += r.NTied;
                dropped.Desired += r.Desired; dropped.Loss += r.Loss; dropped.Output += r.Output; dropped.Unsupported += r.Unsupported;
                foreach (var pv in r.Prov) { dropped.Prov.TryGetValue(pv.Key, out int c); dropped.Prov[pv.Key] = c + pv.Value; }
                continue;
            }
            InpWrite(sk.Inpf, year, nm, kv.Key.Good < 0 ? "(all)" : Catalog.Goods[kv.Key.Good].Name, InpReasonName(kv.Key.Reason), r, w);
        }
        if (dropped.Months > 0) InpWrite(sk.Inpf, year, nm, "(many)", "dropped", dropped, w);
        // 検算の行：C2＝fill の再計算が本体の LastInputFill と食い違った件数（months）・見た工場×月の数（desiredRef 欄）
        sk.Inpf.WriteLine(string.Join('\t', year, nm, "(check)", "c2Mismatch", I(ip.C2Bad), "0", "0", "0", "0", F(ip.C2Checked), F(ip.LowMonths), "0", "0", "0", "", "0", "0"));
        foreach (var kv in ip.BRows.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
        {
            var b = kv.Value; double n = b.Months;
            sk.Inpb.WriteLine(string.Join('\t', year, nm, Catalog.Goods[kv.Key.Item1].Name, InpReasonName(kv.Key.Item2), I(b.Months),
                F(b.Avail / n), F(b.Core / n), F(b.Pop / n), F(b.Fac / n), F(b.Inv / n), F(b.Mil / n), F(b.ProducerCount / n), F(b.WorldProducers / n), F(b.Nos / n), F(b.Import / n), F(b.Price / n), F(b.PriceOverRef / n)));
        }
        ip.Rows.Clear(); ip.BRows.Clear(); ip.C2Checked = ip.C2Bad = ip.LowMonths = 0;
    }

    static void InpWrite(System.IO.StreamWriter wr, string year, string nm, string good, string reason, InpRow r, World w)
    {
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int topId = -1, top = 0; foreach (var pv in r.Prov.OrderBy(x => x.Key)) if (pv.Value > top) { top = pv.Value; topId = pv.Key; }
        string topName = topId >= 0 ? (w.Regions.FirstOrDefault(x => x.Id == topId)?.Name ?? I(topId)) : "";
        wr.WriteLine(string.Join('\t', year, nm, good, reason, I(r.Months), I(r.NExtreme), I(r.N05), I(r.N30), I(r.N70), F(r.Desired), F(r.Loss), F(r.Output), F(r.Unsupported), I(r.Prov.Count), topName, F(r.Months > 0 ? (double)top / r.Months : 0), I(r.NTied)));
    }
}
