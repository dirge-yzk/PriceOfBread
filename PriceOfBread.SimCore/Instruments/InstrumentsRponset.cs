namespace PriceOfBread.SimCore;

// (1147) 段 C′ の計器 (b)：外国の取り分の初出と四半期の頭の区分（rponset_s）。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない。頭の写しは計器の側（RpSnap の Head・ConditionalWeakTable）。
// 区分は頭の s で判定する（寄せの後の s ではない）：1＝①だけ／3＝③（陸の隣）／4＝講和の付与（頭 s＝0・末 s＞0）。②（交戦の相手）は区分に入れず atWarFlag で持つ。
public static partial class Instruments
{
    internal const string RponsetHeaderText = "quarter\tyear\tnation\tregion\ttier\trowKind\tforeignKey\tclassHead\tsHead\tsEnd\triseS\tfallS\tadjacentFlag\tatWarFlag\townerAtWarFlag\tfirstTableFlag\tdataStateFlag\thasPairHeadFlag\ttailFlag\ttailMass";

    /// <summary>計器の仕事の回数（番人が〈計器が切れている時は増えない〉を数える・純カウンタ）。</summary>
    internal static long RpOnsetCount;

    /// <summary>四半期の頭の写し：(州, 外国の名) の取り分 s と、州の Rule[2]。値は写すだけ・エンジンの欄を握らない。</summary>
    internal sealed class RpHead
    {
        public readonly Dictionary<(Region, string), double> S = new();
        public readonly Dictionary<Region, double> Rule2 = new(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        public bool Used;   // 1 つの頭は 1 つの表にだけ使う（頭が古いまま末の表に使わない）
    }

    internal static RpHead RpHeadCopy(IReadOnlyList<Simulation> nations)
    {
        var h = new RpHead();
        foreach (var sm in nations)
            foreach (var r in sm.World.Regions)
            {
                for (int k = 0; k < r.ForeignShares.Length && k < r.ForeignRulers.Length; k++)
                    if (r.ForeignShares[k] > 0) h.S[(r, r.ForeignRulers[k])] = r.ForeignShares[k];
                if (r.Rule.Length == 4 && r.Rule[2] > 0) h.Rule2[r] = r.Rule[2];
            }
        return h;
    }

    /// <summary>区分（純関数）：隣なら 3・頭 s＞0 なら 1・頭 s＝0 で末 s＞0 なら 4・他は 0。</summary>
    internal static int RpOnsetClass(double sHead, double sEnd, bool adjacent)
        => adjacent ? 3 : sHead > 0 ? 1 : sEnd > 0 ? 4 : 0;

    /// <summary>表の口（RulePowerTable と試験が同じ口を呼ぶ・状態は試合の Diplomacy ごとの計器の側）。</summary>
    internal static void RponsetRows(MultiNationSim sim, ulong seed, int q, bool firstTable)
    {
        var dip = sim.Diplomacy;
        var st = RpStates.GetValue(dip, _ => new RpState());
        RpSnaps.TryGetValue(dip, out var snap);
        RponsetTable(sim, seed, q, dip, st, snap, firstTable);
    }

    /// <summary>四半期の末：頭の写しと今の取り分から、初出と区分の表を書く。頭が無い四半期（計器が切れていた・四半期の途中のセーブの後）は行を書かない。</summary>
    static void RponsetTable(MultiNationSim sim, ulong seed, int q, Diplomacy dip, RpState st, RpSnap? snap, bool firstTable)
    {
        if (!Wants("rponset_s")) return;
        if (firstTable)
            foreach (var sm in sim.Nations)
                foreach (var r in sm.World.Regions)
                {
                    if (r.Rule.Length == 4 && r.Rule[2] > 0 && !RpOnsetHasPair(r)) st.DataRegions.Add(r);   // 名の無い取り分（開幕のデータの州）
                }
        var head = snap?.Head;
        if (head is null || head.Used) return;
        head.Used = true;
        RpOnsetCount++;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var fpOf = new Dictionary<string, Power?>(); var nodesOf = new Dictionary<string, HashSet<int>>();
        var sb = new System.Text.StringBuilder();
        if (!st.HeaderOnset) { using (var w0 = Open("rponset", seed)) w0.WriteLine(RponsetHeaderText); st.HeaderOnset = true; }
        foreach (var sm in sim.Nations)
        {
            var w = sm.World; var ownP = w.SelfPower;
            if (w.Regions.Count == 0) continue;
            string nat = NameOf(w);
            bool ownerAtWar = false;
            if (ownP is not null) foreach (var wr in dip.Wars) if (ReferenceEquals(wr.A, ownP) || ReferenceEquals(wr.B, ownP)) { ownerAtWar = true; break; }
            foreach (var r in w.Regions)
            {
                var rc = Places.Coord(r.Name); int nd = Diplomacy.CessionNode(rc.Lat, rc.Lon);
                bool hasPairHead = false;
                for (int k = 0; k < r.ForeignShares.Length && k < r.ForeignRulers.Length; k++)
                {
                    string key = r.ForeignRulers[k];
                    head.S.TryGetValue((r, key), out double sHead);
                    double sEnd = r.ForeignShares[k] > 0 ? r.ForeignShares[k] : 0;
                    if (!(sHead > 0) && !(sEnd > 0)) continue;
                    if (sHead > 0) hasPairHead = true;
                    if (!fpOf.TryGetValue(key, out var fp)) { fp = dip.Powers.Find(pp => (pp.BaseName.Length > 0 ? pp.BaseName : pp.Name) == key); fpOf[key] = fp; if (fp is not null) nodesOf[key] = Diplomacy.CessionWinnerNodes(fp); }
                    bool adj = fp is not null && nd >= 0 && Diplomacy.CessionAdjacent(nd, nodesOf[key], out _);
                    bool atWar = false;
                    if (fp is not null && ownP is not null) foreach (var wr in dip.Wars) if ((ReferenceEquals(wr.A, ownP) && ReferenceEquals(wr.B, fp)) || (ReferenceEquals(wr.B, ownP) && ReferenceEquals(wr.A, fp))) { atWar = true; break; }
                    int cls = RpOnsetClass(sHead, sEnd, adj);
                    bool tail = cls == 1 && sHead > 0 && sHead < 0.01;
                    sb.Append(string.Join('\t', I(q), I(w.Year), nat, r.Name, r.Tier.ToString(), "pair", key, I(cls), RpR(sHead), RpR(sEnd), RpR(sEnd > sHead ? sEnd - sHead : 0), RpR(sHead > sEnd ? sHead - sEnd : 0),
                        adj ? "1" : "0", atWar ? "1" : "0", ownerAtWar ? "1" : "0", firstTable ? "1" : "0", "0", "0", tail ? "1" : "0", RpR(tail ? sHead : 0))).Append('\n');
                }
                head.Rule2.TryGetValue(r, out double h2);
                double e2 = r.Rule.Length == 4 && r.Rule[2] > 0 ? r.Rule[2] : 0;
                if (h2 > 0 || e2 > 0)
                    sb.Append(string.Join('\t', I(q), I(w.Year), nat, r.Name, r.Tier.ToString(), "region", "", "0", RpR(h2), RpR(e2), RpR(e2 > h2 ? e2 - h2 : 0), RpR(h2 > e2 ? h2 - e2 : 0),
                        "0", "0", ownerAtWar ? "1" : "0", firstTable ? "1" : "0", st.DataRegions.Contains(r) ? "1" : "0", hasPairHead ? "1" : "0", "0", "0")).Append('\n');
            }
        }
        if (sb.Length > 0) System.IO.File.AppendAllText(System.IO.Path.Combine(Dir, $"rponset_s{seed}.tsv"), sb.ToString(), new System.Text.UTF8Encoding(false));
    }

    /// <summary>州に名の付いた取り分（正）が 1 つでもあるか。</summary>
    static bool RpOnsetHasPair(Region r)
    {
        for (int k = 0; k < r.ForeignShares.Length && k < r.ForeignRulers.Length; k++) if (r.ForeignShares[k] > 0) return true;
        return false;
    }
}
