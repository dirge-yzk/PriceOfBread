namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。〔履歴 InstrumentsInheritIntent.cs#1〕
public static partial class Instruments
{
    internal const string InheritIntentHeaderText = "scope\tquarter\tyear\thow\tfromNation\ttoNation\ttoBornQuarter\tregion\tgood\trecipe\tstateTargetOld\tstateTargetNew\tbuiltCount\tstateTargetUsed\tconstructionLeftDays\tbuildCostPerMonthOld\tnation\tbornQuarter\tpendingWillCount\tpendingWillSum\tbuiltSum\tinProgressAtTransfer\tstateTargetPrereg";
    internal const string InheritParHeaderText = "transferQuarter\ttoNation\tregion\tproducer\tinProgressAtTransfer\tcompletedQuarter\tparallelStarts\tparallelStartsOwnBaseline\tfromNation\thow\tstateTargetOld\tbuiltCount\twindowQuarters\townExposureRegionQuarters";

    sealed class InheritEv
    {
        public Territory.Rehoming How; public string From = ""; public Region Reg = null!; public Producer Prod = null!;
        public int Old, New, Built, Used = -1; public double CtlLeft, Cost; public bool InProg;
    }
    sealed class InheritAcc { public readonly List<InheritEv> Pending = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, InheritAcc> InheritAccs = new();
    /// <summary>
    /// 〔履歴 InstrumentsInheritIntent.cs#2〕
    /// </summary>
    sealed class InheritReg { public readonly List<World> Touched = new(); public readonly List<InheritPar> Open = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, InheritReg> InheritRegs = new();
    /// <summary>
    /// 窓が閉じる（完成）か試合が終わるまで開いている。〔履歴 InstrumentsInheritIntent.cs#3〕
    /// </summary>
    sealed class InheritPar
    {
        public Territory.Rehoming How; public string From = ""; public Region Reg = null!; public Producer Prod = null!; public int Old, Built; public bool InProg, Done;
        public string? To; public int TransferQ = -1, CompQ = -1, Par, Base, WinQ, Expo;
    }
    sealed class OwnStartBox { public World W = null!; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Producer, OwnStartBox> InheritOwnStarts = new();
    sealed class InheritBorn { public int Q; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, InheritBorn> InheritBorns = new();
    sealed class InheritSink { public bool Started, ParStarted; public ulong Seed; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, InheritSink> InheritSinks = new();
    static string InheritR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>移しの規則を当てる前（<c>Territory.TransferRegion</c>・読むだけ）：移る州の StateTarget＞0 の生産者ごとに、旧の値と新の値（純関数）・今の棟数・工事中の残り日数・旧の意志の月の工費の見込みを蓄える。</summary>
    internal static void InheritIntentBefore(World from, Region r, Territory.Rehoming how)
    {
        ConsDepart(from, r);   // ConsDepart〔履歴 InstrumentsInheritIntent.cs#4〕
        var acc = InheritAccs.GetValue(from, _ => new InheritAcc());
        InheritReg? reg = from.Diplomacy is { } dip ? InheritRegs.GetValue(dip, _ => new InheritReg()) : null;
        foreach (var pr in r.Producers)
        {
            if (!(pr.StateTarget > 0)) continue;
            int built = World.StateUnits(pr, from.Coef);
            acc.Pending.Add(new InheritEv
            {
                How = how, From = NameOf(from), Reg = r, Prod = pr, Old = pr.StateTarget, New = Territory.DroppedStateTarget(pr.StateTarget, built), Built = built, CtlLeft = pr.ConstructionLeft, InProg = pr.ConstructionLeft > 0,
                Cost = pr.StateTarget > built && !(pr.ConstructionLeft > 0) ? UtilityAgent.NewFacilityOutlayPerMonth(from, r, pr) : 0,
            });
            if (reg is null) continue;
            if (!reg.Touched.Contains(from)) reg.Touched.Add(from);
            reg.Open.Add(new InheritPar { How = how, From = NameOf(from), Reg = r, Prod = pr, Old = pr.StateTarget, Built = built, InProg = pr.ConstructionLeft > 0 });
        }
    }

    /// <summary>移しの規則を当てた後（読むだけ）：蓄えた事象の stateTargetUsed に、いま実際に残っている StateTarget を入れる。</summary>
    internal static void InheritIntentAfter(World from, Region r)
    {
        if (!InheritAccs.TryGetValue(from, out var acc)) return;
        foreach (var ev in acc.Pending) if (ev.Reg == r && ev.Used < 0) ev.Used = ev.Prod.StateTarget;
    }

    /// <summary>四半期の末（<see cref="Quarter"/> の最後）：計器に初めて現れた国の生まれの四半期を覚え、蓄えた事象の行（E）と年末の国の行（Q）を追記する。</summary>
    static void InheritIntentTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = InheritSinks.GetValue(sim, _ => new InheritSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        if (!sk.Started) sb.Append(InheritIntentHeaderText).Append('\n');
        foreach (var n in sim.Nations) if (!InheritBorns.TryGetValue(n.World, out _)) InheritBorns.Add(n.World, new InheritBorn { Q = q });
        void EmitAcc(World w, InheritAcc acc)
        {
            foreach (var ev in acc.Pending)
            {
                World? own = null;
                foreach (var m in sim.Nations) if (m.World.Regions.Contains(ev.Reg)) { own = m.World; break; }
                string toName = own is null ? "" : NameOf(own);
                string toBorn = own is not null && InheritBorns.TryGetValue(own, out var tb) ? tb.Q.ToString(inv) : "-1";
                int prereg = Math.Min(ev.Old, ev.InProg ? ev.Built + 1 : ev.Built);   // エンジンの DroppedStateTarget を呼ばずに独立に書く〔履歴 InstrumentsInheritIntent.cs#5〕
                sb.Append(string.Join('\t', "E", q.ToString(inv), w.Year.ToString(inv), ev.How.ToString(), ev.From, toName, toBorn, ev.Reg.Name, Catalog.Goods[ev.Prod.GoodId].Name, ev.Prod.RecipeName,
                    ev.Old.ToString(inv), ev.New.ToString(inv), ev.Built.ToString(inv), ev.Used.ToString(inv), InheritR(ev.CtlLeft), InheritR(ev.Cost), "", "", "", "", "",
                    ev.InProg ? "1" : "0", prereg.ToString(inv))).Append('\n');
            }
            acc.Pending.Clear();
        }
        var preg = sim.Diplomacy is { } sdip ? InheritRegs.GetValue(sdip, _ => new InheritReg()) : null;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!InheritAccs.TryGetValue(w, out var acc) || acc.Pending.Count == 0) continue;
            EmitAcc(w, acc);
        }
        if (preg is not null)   // preg〔履歴 InstrumentsInheritIntent.cs#6〕
        {
            foreach (var w in preg.Touched)
                if (InheritAccs.TryGetValue(w, out var acc) && acc.Pending.Count > 0) EmitAcc(w, acc);
            preg.Touched.Clear();
        }
        if (q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1)
        {
            foreach (var nn in sim.Nations)
            {
                var w = nn.World;
                if (w.Regions.Count == 0) continue;
                int cnt = 0, sum = 0, builtSum = 0;
                foreach (var rg in w.Regions)
                    foreach (var pr in rg.Producers)
                    {
                        int u = World.StateUnits(pr, w.Coef);
                        builtSum += u;
                        if (pr.StateTarget > u) { cnt++; sum += pr.StateTarget - u; }
                    }
                int bq = InheritBorns.TryGetValue(w, out var b) ? b.Q : q;
                sb.Append(string.Join('\t', "Q", q.ToString(inv), w.Year.ToString(inv), "", "", "", "", "", "", "", "", "", "", "", "", "", NameOf(w), bq.ToString(inv), cnt.ToString(inv), sum.ToString(inv), builtSum.ToString(inv), "", "")).Append('\n');
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"inheritintent_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
        sk.Seed = seed;
        if (preg is not null) InheritParFlush(sim, sk, preg, q);
    }

    /// <summary>
    /// 〔履歴 InstrumentsInheritIntent.cs#7〕
    /// </summary>
    static void InheritParFlush(MultiNationSim sim, InheritSink sk, InheritReg reg, int q)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        if (!sk.ParStarted) sb.Append(InheritParHeaderText).Append('\n');
        var done = new List<InheritPar>();
        foreach (var row in reg.Open)
        {
            World? own = null;
            foreach (var m in sim.Nations) if (m.World.Regions.Contains(row.Reg)) { own = m.World; break; }
            if (row.To is null) { row.To = own is null ? "" : NameOf(own); row.TransferQ = q; }
            if (!row.InProg || row.Done)
            {
                if (row.Done) row.CompQ = q;
                done.Add(row);
                continue;
            }
            row.WinQ++;
            if (own is not null)
                foreach (var g in own.Regions)
                    if (g != row.Reg && InheritOwnInProgress(g, null, own)) row.Expo++;
        }
        foreach (var row in done) { reg.Open.Remove(row); InheritParRow(sb, row, inv); }   // 移しの順（開いた順）に書く
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"inheritpar_s{sk.Seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.ParStarted) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.ParStarted = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }

    /// <summary>
    /// 読むだけ。〔履歴 InstrumentsInheritIntent.cs#8〕
    /// </summary>
    static bool InheritOwnInProgress(Region g, Producer? except, World w)
    {
        foreach (var q in g.Producers)
            if (q != except && q.ConstructionLeft > 0 && InheritOwnStarts.TryGetValue(q, out var ob) && ReferenceEquals(ob.W, w)) return true;
        return false;
    }

    static void InheritParRow(System.Text.StringBuilder sb, InheritPar r, System.Globalization.CultureInfo inv)
    {
        string na = r.InProg ? "" : "-1";
        sb.Append(string.Join('\t', r.TransferQ.ToString(inv), r.To ?? "", r.Reg.Name, Catalog.Goods[r.Prod.GoodId].Name, r.InProg ? "1" : "0",
            r.InProg ? r.CompQ.ToString(inv) : na, r.InProg ? r.Par.ToString(inv) : na, r.InProg ? r.Base.ToString(inv) : na, r.From, r.How.ToString(), r.Old.ToString(inv), r.Built.ToString(inv),
            r.InProg ? r.WinQ.ToString(inv) : na, r.InProg ? r.Expo.ToString(inv) : na)).Append('\n');
    }

    /// <summary>
    /// 〔履歴 InstrumentsInheritIntent.cs#9〕
    /// </summary>
    internal static void InheritParStarted(World w, Region r, Producer p)
    {
        InheritOwnStarts.GetValue(p, _ => new OwnStartBox()).W = w;
        if (w.Diplomacy is not { } dip || !InheritRegs.TryGetValue(dip, out var reg) || reg.Open.Count == 0) return;
        bool ownBusy = false, ownKnown = false;
        foreach (var row in reg.Open)
        {
            if (!row.InProg || row.Done) continue;
            if (row.Reg == r) { if (row.Prod != p) row.Par++; continue; }
            if (!w.Regions.Contains(row.Reg)) continue;   // 受け手でない国の着工は数えない
            if (!ownKnown) { ownBusy = InheritOwnInProgress(r, p, w); ownKnown = true; }
            if (ownBusy) row.Base++;
        }
    }

    /// <summary>
    /// 〔履歴 InstrumentsInheritIntent.cs#10〕
    /// </summary>
    internal static void InheritParCompleted(World w, Producer p)
    {
        if (w.Diplomacy is not { } dip || !InheritRegs.TryGetValue(dip, out var reg)) return;
        foreach (var row in reg.Open) if (row.Prod == p && row.InProg) row.Done = true;
    }

    /// <summary>
    /// 〔履歴 InstrumentsInheritIntent.cs#11〕
    /// </summary>
    static void InheritIntentClose(MultiNationSim sim)
    {
        if (!InheritSinks.TryGetValue(sim, out var sk) || !sk.ParStarted) return;
        if (sim.Diplomacy is not { } dip || !InheritRegs.TryGetValue(dip, out var reg) || reg.Open.Count == 0) return;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        foreach (var row in reg.Open) { row.To ??= ""; InheritParRow(sb, row, inv); }
        reg.Open.Clear();
        System.IO.File.AppendAllText(System.IO.Path.Combine(Dir, $"inheritpar_s{sk.Seed}.tsv"), sb.ToString(), new System.Text.UTF8Encoding(false));
    }
}
