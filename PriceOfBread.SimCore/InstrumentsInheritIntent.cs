namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (774) 継いだ着工の意志の表 inheritintent_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHabitcap.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/m774_20261102_prereg_sw.txt。州が別の国へ移る（Territory.TransferRegion＝割譲・分離独立・再併合）とき、移る州の国営工場を建てる意志 StateTarget が新しい国へそのまま移る形の誤りを、
//   同じ盤面で旧（下げる前）と新（Territory.DroppedStateTarget＝旗 InheritedStateIntentDrop に依らず同じ純関数）を並べて読む。
// 表 inheritintent_s（scope 列で 2 種類の行・全ての国。POB_INSTRUMENTS_NATIONS の絞りは読まない）：
//   E＝移しの事象の行（移る州の生産者のうち StateTarget＞0 のもの・四半期の末に出す）：quarter／year／how（Ceded・Seceded・Returned）／fromNation／toNation（四半期の末のその州の持ち主・見つからなければ空）／toBornQuarter（受け手が計器に初めて現れた四半期＝nation_s の生まれと同じ定義）／
//     region／good／recipe／stateTargetOld（下げる前）／stateTargetNew（DroppedStateTarget＝旗に依らず計算）／builtCount（今の棟数）／stateTargetUsed（規則を当てた後に実際に残った値）／constructionLeftDays（工事中なら残りの日数）／
//     buildCostPerMonthOld（旧の意志が次に着工させる月の工費の見込み＝stateTargetOld＞builtCount かつ工事中でないとき UtilityAgent.NewFacilityOutlayPerMonth〔工賃＋素材〕・それ以外は 0）。
//   Q＝四半期の行（年末 q%4==3・州のある国）：quarter／year／nation／bornQuarter／pendingWillCount（StateTarget＞今の棟数の生産者の数）／pendingWillSum（Σ(StateTarget−今の棟数)）／builtSum（Σ今の棟数）。
//     ＝〈移しの外の州〉で意志が残っているかを数える列（事象の行を出さない形の誤りは Q の行で見える）。
// ⚠限界（計器が自己申告する）：分離独立では移しの時点で国が無い＝toNation は四半期の末の持ち主の名前（同じ四半期に州が再び移ると外れる）／toBornQuarter は計器に現れた最初の四半期／
//   buildCostPerMonthOld は工賃＋素材の月の見込みで、工事の途中の借入や切り下げの費用は含まない／stateTargetOld＝0 の生産者は行にしない／単国の試験（MultiNationSim なし）では出さない。
public static partial class Instruments
{
    internal const string InheritIntentHeaderText = "scope\tquarter\tyear\thow\tfromNation\ttoNation\ttoBornQuarter\tregion\tgood\trecipe\tstateTargetOld\tstateTargetNew\tbuiltCount\tstateTargetUsed\tconstructionLeftDays\tbuildCostPerMonthOld\tnation\tbornQuarter\tpendingWillCount\tpendingWillSum\tbuiltSum";

    sealed class InheritEv
    {
        public Territory.Rehoming How; public string From = ""; public Region Reg = null!; public Producer Prod = null!;
        public int Old, New, Built, Used = -1; public double CtlLeft, Cost;
    }
    sealed class InheritAcc { public readonly List<InheritEv> Pending = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, InheritAcc> InheritAccs = new();
    sealed class InheritBorn { public int Q; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, InheritBorn> InheritBorns = new();
    sealed class InheritSink { public bool Started; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, InheritSink> InheritSinks = new();
    static string InheritR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>移しの規則を当てる前（<c>Territory.TransferRegion</c>・読むだけ）：移る州の StateTarget＞0 の生産者ごとに、旧の値と新の値（純関数）・今の棟数・工事中の残り日数・旧の意志の月の工費の見込みを蓄える。</summary>
    internal static void InheritIntentBefore(World from, Region r, Territory.Rehoming how)
    {
        var acc = InheritAccs.GetValue(from, _ => new InheritAcc());
        foreach (var pr in r.Producers)
        {
            if (!(pr.StateTarget > 0)) continue;
            int built = World.StateUnits(pr, from.Coef);
            acc.Pending.Add(new InheritEv
            {
                How = how, From = NameOf(from), Reg = r, Prod = pr, Old = pr.StateTarget, New = Territory.DroppedStateTarget(pr.StateTarget, built), Built = built, CtlLeft = pr.ConstructionLeft,
                Cost = pr.StateTarget > built && !(pr.ConstructionLeft > 0) ? UtilityAgent.NewFacilityOutlayPerMonth(from, r, pr) : 0,
            });
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
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!InheritAccs.TryGetValue(w, out var acc) || acc.Pending.Count == 0) continue;
            foreach (var ev in acc.Pending)
            {
                World? own = null;
                foreach (var m in sim.Nations) if (m.World.Regions.Contains(ev.Reg)) { own = m.World; break; }
                string toName = own is null ? "" : NameOf(own);
                string toBorn = own is not null && InheritBorns.TryGetValue(own, out var tb) ? tb.Q.ToString(inv) : "-1";
                sb.Append(string.Join('\t', "E", q.ToString(inv), w.Year.ToString(inv), ev.How.ToString(), ev.From, toName, toBorn, ev.Reg.Name, Catalog.Goods[ev.Prod.GoodId].Name, ev.Prod.RecipeName,
                    ev.Old.ToString(inv), ev.New.ToString(inv), ev.Built.ToString(inv), ev.Used.ToString(inv), InheritR(ev.CtlLeft), InheritR(ev.Cost), "", "", "", "", "")).Append('\n');
            }
            acc.Pending.Clear();
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
                sb.Append(string.Join('\t', "Q", q.ToString(inv), w.Year.ToString(inv), "", "", "", "", "", "", "", "", "", "", "", "", "", NameOf(w), bq.ToString(inv), cnt.ToString(inv), sum.ToString(inv), builtSum.ToString(inv))).Append('\n');
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"inheritintent_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
