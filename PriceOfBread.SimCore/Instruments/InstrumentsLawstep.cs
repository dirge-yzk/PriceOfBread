namespace PriceOfBread.SimCore;

// (1372) 計器 lawstep_s：法の段が変わった 1 件ごとに 1 行（Government.LevelLaws の全ての段の法・全ての国・小国も）。仕様 measurements/m1360_20261125/report.txt 〈足すべき計器の仕様〉。
// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（欄は計器の ConditionalWeakTable だけ・セーブに載らない）。
// 書き手の別（writerCode）は計器が盤面から自分で判じる（エンジンの局所変数を受け取らない）：
//   1 AI の目標（施行のループで変わり、下の 2・3 に当たらない）・2 蜂起の要求（PendingRevoltDemand の軸と向きに合う）・3 派閥の危機（PendingFactionCrisis.CrisisDemands の軸と向きに合う）
//   4 派閥の譲歩（ConcedeToFaction の ApplyDemand の前後）・5 政体の戻し（GovernanceSnapshot.RestoreTo の前後）・6 その他（次の施行の頭で前回の記録と違っていた軸＝政体の変化・事件など）
// 2 と 3 は〈同じ軸・同じ向きの要求が立っていた〉ことだけで判じる（AI の目標が偶然同じ向きなら 2・3 に数える＝推）。
// 純支持（netUp・netDown）は施行の道（1・2・3）の行だけ・変える前の盤面で読む（段が目標と違う法だけ計算する）。4・5・6 は nan。ApplyDemand の税の段とスライダー軸は段の法でないので対象外。
public static partial class Instruments
{
    internal const string LawstepHeaderText = "quarter\tyear\tnationUid\tnation\taxis\tfromStage\ttoStage\twriterCode\taiTargetStage\tnetUpIndex\tnetDownIndex\tcostPaidIndex"
        + "\tmassMilitancyBefore\tmassMilitancyAfter\tgoverningPartyCode\tatWarFlag\tpendingRevoltDemandCode";

    sealed class LsRow
    {
        public int Year, From, To, Writer, AiTarget, Party, AtWar, Pending;
        public string Axis = "";
        public double NetUp = double.NaN, NetDown = double.NaN, Cost = double.NaN, MilBefore = double.NaN, MilAfter = double.NaN;
    }
    sealed class LsWorld
    {
        public int[]? Last;
        public readonly System.Collections.Generic.List<LsRow> Rows = new();
        public Government.LevelLaw? PreLaw; public int PreLevel, PreTarget; public double PreNetUp = double.NaN, PreNetDown = double.NaN, PreSpent, HeadMil = double.NaN;
        public int[]? Side; public double SideMil, SideSpent;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, LsWorld> LsWorlds = new();
    static LsWorld LsOf(World w) => LsWorlds.GetValue(w, _ => new LsWorld());

    static int[] LsStages(World w)
    {
        var a = new int[Government.LevelLaws.Length];
        for (int i = 0; i < a.Length; i++) a[i] = Government.LevelLaws[i].Get(w);
        return a;
    }

    /// <summary>大衆（庶民で近代のエリートでない）の闘争度の人口加重の平均（計器が自分で書く）。</summary>
    static double LsMil(World w)
    {
        double mil = 0, size = 0;
        foreach (var p in w.AllPops()) if (Politics.IsMassPop(p)) { mil += p.Militancy * p.Size; size += p.Size; }
        return size > 1e-9 ? mil / size : 0;
    }

    static LsRow LsRowOf(World w, Government.LevelLaw law, int from, int to, int writer) => new()
    {
        Year = w.Year, Axis = law.Axis.ToString(), From = from, To = to, Writer = writer, AiTarget = -1, Party = w.GoverningParty, AtWar = w.AtWarNow ? 1 : 0, Pending = w.PendingRevoltDemand,
    };

    static bool LsLive() => On && Tg.Lawstep;   // 呼び手は On を読まずに呼んでよい

    /// <summary>施行（Government.Step）の頭：前回の記録と違う軸は〈その他〉の行にして、今の段を記録する。</summary>
    internal static void LawstepHead(World w)
    {
        if (!LsLive()) return;
        var st = LsOf(w); var cur = LsStages(w);
        if (st.Last is { } last)
        {
            double? mil = null;
            for (int i = 0; i < cur.Length; i++)
                if (cur[i] != last[i]) { var r = LsRowOf(w, Government.LevelLaws[i], last[i], cur[i], 6); r.MilAfter = mil ??= LsMil(w); st.Rows.Add(r); }
        }
        st.Last = cur; st.PreLaw = null; st.HeadMil = LsMil(w);
    }

    /// <summary>施行のループの 1 本の法の前：前の法を閉じ、この法の今の段・目標・純支持（段が目標と違うときだけ）を置く。</summary>
    internal static void LawstepBegin(World w, Government.LevelLaw law)
    {
        if (!LsLive()) return;
        var st = LsOf(w);
        LsClose(w, st);
        st.PreLaw = law; st.PreLevel = law.Get(w); st.PreTarget = law.Target(w, w.Policy); st.PreSpent = w.MandateSpent;
        st.PreNetUp = st.PreNetDown = double.NaN;
        if (st.PreTarget != st.PreLevel)
        {
            var e = PolicyPreferences.Build(w);
            st.PreNetUp = PolicyPreferences.NetSupport(w, law.Axis, true, e); st.PreNetDown = PolicyPreferences.NetSupport(w, law.Axis, false, e);
        }
    }

    /// <summary>施行の終わり：最後の法を閉じ、今の段を記録する。</summary>
    internal static void LawstepEnd(World w)
    {
        if (!LsLive()) return;
        var st = LsOf(w);
        LsClose(w, st);
        st.Last = LsStages(w);
    }

    static void LsClose(World w, LsWorld st)
    {
        if (st.PreLaw is not { } law) return;
        st.PreLaw = null;
        int now = law.Get(w);
        if (now == st.PreLevel) return;
        bool up = now > st.PreLevel; int pd = w.PendingRevoltDemand;
        int writer = 1;
        if (pd != 0 && (PolicyAxis)(System.Math.Abs(pd) - 1) == law.Axis && (pd > 0) == up) writer = 2;
        else if (w.PendingFactionCrisis is { CrisisDemands: { } cd })
            foreach (int d in cd) if (d != 0 && (PolicyAxis)(System.Math.Abs(d) - 1) == law.Axis && (d > 0) == up) { writer = 3; break; }
        var r = LsRowOf(w, law, st.PreLevel, now, writer);
        r.AiTarget = st.PreTarget; r.NetUp = st.PreNetUp; r.NetDown = st.PreNetDown; r.Cost = w.MandateSpent - st.PreSpent; r.MilBefore = st.HeadMil; r.MilAfter = LsMil(w);
        st.Rows.Add(r);
    }

    /// <summary>派閥の譲歩（ConcedeToFaction）・政体の戻し（RestoreTo）の前：全ての軸の段と大衆の闘争度を置く。</summary>
    internal static void LawstepSideBegin(World w)
    {
        if (!LsLive()) return;
        var st = LsOf(w); st.Side = LsStages(w); st.SideMil = LsMil(w); st.SideSpent = w.MandateSpent;
    }

    /// <summary>同じ後：前後で変わった軸を writer（4 または 5）の行にする。</summary>
    internal static void LawstepSideEnd(World w, int writer)
    {
        if (!LsLive()) return;
        var st = LsOf(w);
        if (st.Side is not { } before) return;
        st.Side = null;
        var cur = LsStages(w); double? milAfter = null;
        for (int i = 0; i < cur.Length; i++)
        {
            if (cur[i] == before[i]) continue;
            var r = LsRowOf(w, Government.LevelLaws[i], before[i], cur[i], writer);
            r.Cost = w.MandateSpent - st.SideSpent; r.MilBefore = st.SideMil; r.MilAfter = milAfter ??= LsMil(w);
            st.Rows.Add(r);
            if (st.Last is { } last) last[i] = cur[i];
        }
    }

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LdSink> LsSinks = new();

    /// <summary>四半期の末：国ごとに溜めた行を書く（国の番号 Uid は NationRow で採番済み）。</summary>
    static void LawstepTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("lawstep_s")) return;
        var sk = LsSinks.GetValue(sim, _ => new LdSink());
        sk.Peek.Clear();
        if (sk.W is null) { sk.W = Open("lawstep", seed); sk.W.WriteLine(LawstepHeaderText); }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!LsWorlds.TryGetValue(w, out var st) || st.Rows.Count == 0) continue;
            string uid = S(w).Uid.ToString(inv), name = NameOf(w);
            foreach (var r in st.Rows)
            {
                string line = string.Join('\t', q.ToString(inv), r.Year.ToString(inv), uid, name, r.Axis, r.From.ToString(inv), r.To.ToString(inv), r.Writer.ToString(inv),
                    r.AiTarget.ToString(inv), F(r.NetUp), F(r.NetDown), F(r.Cost), F(r.MilBefore), F(r.MilAfter), r.Party.ToString(inv), r.AtWar.ToString(inv), r.Pending.ToString(inv));
                sk.W.WriteLine(line); sk.Peek.Add(line);
            }
            st.Rows.Clear();
        }
    }
    static void LawstepClose(MultiNationSim sim) { if (LsSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }

    /// <summary>試験の読み口：直前の四半期に書いた行。</summary>
    internal static System.Collections.Generic.IReadOnlyList<string> LawstepPeek(MultiNationSim sim) => LsSinks.GetValue(sim, _ => new LdSink()).Peek;
}
