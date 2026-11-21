namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（`MultiNationSim` が求める導出量を読むだけ）。〔履歴 InstrumentsClock.cs#1〕
public static partial class Instruments
{
    internal static string ClockHeaderText()
        => "kind\tquarter\tyear\trow\tdecade\tworldTotalPerMonth\tholders\townNation\townPerMonth\ttop1Nation\ttop1PerMonth\tbritainPerMonth\tfrancePerMonth\tprussiaPerMonth\tchinaPerMonth";

    /// <summary>8四半期ごとの世界の合計と国別の A（<see cref="MultiNationSim"/> の判定の段の頭の後・全ての国の判定の前）。<paramref name="worldTotal"/>＝世界の高度労働力の雇用の合計。
    /// <paramref name="scanTicks"/>・<paramref name="scanVisits"/>＝世界の合計の巡回の累計（Stopwatch の刻みと訪れた生産者・計算量の単価の根拠）。</summary>
    internal static void ClockSample(MultiNationSim sim, double worldTotal, long scanTicks, long scanVisits)
    {
        if (!Tg.Clock) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        var nations = sim.Nations;
        if (nations.Count == 0) return;
        int nr = Catalog.Discoveries.Length;
        var rep = new Producer?[nr];
        var engN = new double[nations.Count][];
        var totN = new double[nations.Count];
        for (int i = 0; i < nations.Count; i++)
        {
            var w = nations[i].World;
            engN[i] = Discovery.EngineersByGood(w);
            foreach (double e in engN[i]) totN[i] += e;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                    if (p.DiscoveryRow >= 0 && p.DiscoveryRow < nr && rep[p.DiscoveryRow] is null) rep[p.DiscoveryRow] = p;
            }
        }
        var st = S(nations[0].World);
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int year = nations[0].World.Year, quarter = Calendar.QuarterIndexOfTick(nations[0].World.Tick);
        string[] names = ["イギリス", "フランス", "プロイセン", "清"];
        int holdersAll = 0; double topAll = 0; string topAllName = "nan";
        for (int i = 0; i < nations.Count; i++)
        {
            if (totN[i] > 0) holdersAll++;
            if (totN[i] > topAll) { topAll = totN[i]; topAllName = NameOf(nations[i].World); }
        }
        st.ClockRows.Add(string.Join('\t', "W", quarter.ToString(inv), year.ToString(inv), "*", "0", F(worldTotal), holdersAll.ToString(inv), "*", "nan", topAllName, F(topAll), "nan", "nan", "nan", "nan"));
        st.ClockRows.Add(string.Join('\t', "C", quarter.ToString(inv), year.ToString(inv), "*", "0", F(scanTicks / (double)System.Diagnostics.Stopwatch.Frequency), scanVisits.ToString(inv), "*", "nan", "nan", "nan", "nan", "nan", "nan", "nan"));
        for (int row = 0; row < nr; row++)
        {
            var d = Catalog.Discoveries[row];
            if (Discovery.IsSpread(d.Field) || rep[row] is not { } rp) continue;
            int holders = 0; double top1 = 0; string top1Name = "nan";
            var named = new double[names.Length];
            for (int i = 0; i < nations.Count; i++)
            {
                double a = Discovery.ParentEngineers(row, rp, engN[i]);
                if (a > 0) holders++;
                if (a > top1) { top1 = a; top1Name = NameOf(nations[i].World); }
                string nm = NameOf(nations[i].World);
                for (int k = 0; k < names.Length; k++) if (nm.Contains(names[k]) || (k == 2 && nm.Contains("ドイツ"))) named[k] += a;   // 立憲王政フランス・ドイツ（プロイセンの改称）も数える
            }
            st.ClockRows.Add(string.Join('\t', "T", quarter.ToString(inv), year.ToString(inv), d.Name, d.Decade.ToString(inv), F(worldTotal), holders.ToString(inv), "*", "nan",
                top1Name, F(top1), F(named[0]), F(named[1]), F(named[2]), F(named[3])));
        }
    }

    /// <summary>国が行を解禁した四半期の自国の A_n と世界の合計 W（<see cref="Discovery.Step"/>・計器だけ）。</summary>
    internal static void ClockOpened(World w, int row, double a, double worldTotal)
        => S(w).ClockRows.Add(string.Join('\t', "E", Calendar.QuarterIndexOfTick(w.Tick).ToString(System.Globalization.CultureInfo.InvariantCulture), w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Catalog.Discoveries[row].Name, Catalog.Discoveries[row].Decade.ToString(System.Globalization.CultureInfo.InvariantCulture), F(worldTotal), "nan", NameOf(w), F(a), "nan", "nan", "nan", "nan", "nan", "nan"));

    static void ClockTable(Sink sk, ulong seed, MultiNationSim sim)
    {
        if (!Tg.Clock) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        foreach (var n in sim.Nations)
        {
            var st = S(n.World);
            if (st.ClockRows.Count == 0) continue;
            sk.Clock ??= Open("clock", seed);
            if (!sk.ClockHeader) { sk.Clock.WriteLine(ClockHeaderText()); sk.ClockHeader = true; }
            foreach (var row in st.ClockRows) sk.Clock.WriteLine(row);
            st.ClockRows.Clear();
        }
    }
}
