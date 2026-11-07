namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (774) 継いだ着工の意志の表 inheritintent_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHabitlg.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/m774_20261102_prereg_sw.txt。州が別の国へ移る（Territory.TransferRegion＝割譲・分離独立・再併合）とき、移る州の国営工場を建てる意志 StateTarget が新しい国へそのまま移る形の誤りを、
//   同じ盤面で旧（下げる前）と新（Territory.DroppedStateTarget＝同じ純関数・旗 InheritedStateIntentDrop は (817) で焼き込み）を並べて読む。
// 表 inheritintent_s（scope 列で 2 種類の行・全ての国。POB_INSTRUMENTS_NATIONS の絞りは読まない）：
//   E＝移しの事象の行（移る州の生産者のうち StateTarget＞0 のもの・四半期の末に出す）：quarter／year／how（Ceded・Seceded・Returned）／fromNation／toNation（四半期の末のその州の持ち主・見つからなければ空）／toBornQuarter（受け手が計器に初めて現れた四半期＝nation_s の生まれと同じ定義）／
//     region／good／recipe／stateTargetOld（下げる前）／stateTargetNew（DroppedStateTarget＝旗に依らず計算）／builtCount（今の棟数）／stateTargetUsed（規則を当てた後に実際に残った値）／constructionLeftDays（工事中なら残りの日数）／
//     buildCostPerMonthOld（旧の意志が次に着工させる月の工費の見込み＝stateTargetOld＞builtCount かつ工事中でないとき UtilityAgent.NewFacilityOutlayPerMonth〔工賃＋素材〕・それ以外は 0）。
//   Q＝四半期の行（年末 q%4==3・州のある国）：quarter／year／nation／bornQuarter／pendingWillCount（StateTarget＞今の棟数の生産者の数）／pendingWillSum（Σ(StateTarget−今の棟数)）／builtSum（Σ今の棟数）。
//     ＝〈移しの外の州〉で意志が残っているかを数える列（事象の行を出さない形の誤りは Q の行で見える）。
// ⚠限界（計器が自己申告する）：分離独立では移しの時点で国が無い＝toNation は四半期の末の持ち主の名前（同じ四半期に州が再び移ると外れる）／toBornQuarter は計器に現れた最初の四半期／
//   buildCostPerMonthOld は工賃＋素材の月の見込みで、工事の途中の借入や切り下げの費用は含まない／stateTargetOld＝0 の生産者は行にしない／単国の試験（MultiNationSim なし）では出さない／
//   toNation の結合は BaseName（同じ BaseName の後継国は取り違える）。
// ★(781) 直し：送り手の World が同じ四半期のうちに sim.Nations から消えても（再併合・全滅した国の割譲）E の行が出る＝移しを蓄えた World を試合（Diplomacy）の登録簿にも覚え、四半期の末に〈sim.Nations の順の行〉（旧と同じ順）の後ろへ消えた送り手の行を追記する。
//   列を末尾に足した：inProgressAtTransfer（移しの時に ConstructionLeft＞0 なら 1）・stateTargetPrereg（事前登録 m774 の文の規則 min(旧の意志, 棟数＋工事中なら 1)＝エンジンの DroppedStateTarget を呼ばずに独立に書く）。Q の行は空。
// 表 inheritpar_s（移しの事象の行 E と同じ生産者ごと・読むだけ）：継いだ工事の窓（移しから継いだ工事の完成まで）の間に、移った州で別の生産者の国営の着工が起きた回数 parallelStarts と、
//   比べる側 parallelStartsOwnBaseline（同じ窓の間の受け手の別の州で〈受け手自身が始めた工事が工事中〉のときに起きた国営の着工の回数）。分母 windowQuarters・ownExposureRegionQuarters は四半期の末の標本。
//   inProgressAtTransfer＝0 の行は窓を持たない（completedQuarter ほか −1）。完成しないまま試合が終わった行は completedQuarter＝−1（窓は打ち切り）。
// ⚠限界（inheritpar_s）：toNation は移した四半期の末の持ち主・窓の間に州が別の国へ移ると窓は追わない／四半期の末の標本（日ごとではない）／自国の工事は計器が見た FacilityStarted だけ（開幕から継いだ工事・助走の間の着工は自国にならない）／
//   同じ州に継いだ工事が複数あると窓の重なる行どうしが同じ着工を数える。
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
    /// <summary>(781) 移しを蓄えた World の登録簿（試合＝Diplomacy ごと）：送り手が sim.Nations から消えても四半期の末にその行を出すため。</summary>
    sealed class InheritReg { public readonly List<World> Touched = new(); public readonly List<InheritPar> Open = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, InheritReg> InheritRegs = new();
    /// <summary>(781) inheritpar_s の 1 行（移しの事象の生産者ごと）。窓が閉じる（完成）か試合が終わるまで開いている。</summary>
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
        ConsDepart(from, r);   // (948) cons_s の出る州の積算を出た国の行へ
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
                int prereg = Math.Min(ev.Old, ev.InProg ? ev.Built + 1 : ev.Built);   // (781) 事前登録 m774 の文の規則（工事中の棟は継ぐ＝棟数＋1 まで）。エンジンの DroppedStateTarget を呼ばずに独立に書く
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
        if (preg is not null)   // (781) sim.Nations から消えた送り手（再併合・全滅した国）の行は、旧の行の後ろへ追記する
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

    /// <summary>(781) 四半期の末：移しの行の持ち主と生まれを確かめ、窓の標本（開いたままの窓の数・受け手の別の州の自国の工事中の州の数）を足し、完成した行・窓を持たない行を書いて閉じる。</summary>
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

    /// <summary>(781) 州 <paramref name="g"/> に、<paramref name="w"/> 自身が始めた工事が工事中の生産者があるか（<paramref name="except"/> は数えない）。読むだけ。</summary>
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

    /// <summary>(781) 国営の棟の着工（<see cref="FacilityStarted"/> の直後・読むだけ）：着工した World を覚え（自国の工事の判定）、開いた窓の数えを進める。</summary>
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

    /// <summary>(781) 国営の棟の完成（<see cref="FacilityCompleted"/> の直後・読むだけ）：継いだ工事の窓を閉じる印（書き出しは次の四半期の末）。</summary>
    internal static void InheritParCompleted(World w, Producer p)
    {
        if (w.Diplomacy is not { } dip || !InheritRegs.TryGetValue(dip, out var reg)) return;
        foreach (var row in reg.Open) if (row.Prod == p && row.InProg) row.Done = true;
    }

    /// <summary>(781) 試合の終わり（<see cref="Close"/>）：開いたままの窓（完成しないまま試合が終わった行）を書く。</summary>
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
