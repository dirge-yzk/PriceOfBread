namespace PriceOfBread.SimCore.Display;

/// <summary>発見の表の 1 行（政府・思想・技術のどれか）。<see cref="Fraction"/>＝解禁済みは 1・まだの行は点数（技術力＋必要性＋伝わり）÷門。
/// 3 項はまだの行だけ（解禁済みは NaN＝「—」）。<see cref="Era"/>＝年代の文字（無ければ空）。</summary>
public sealed record DiscoveryLine(string Key, string Group, string Name, double Fraction, double Tech, double Need, double Transmit, string Era, double Decade);

/// <summary>産地の発見の表の 1 行（大鉱床・植林）。2 項（技術力・必要性）。</summary>
public sealed record SiteLine(string Key, string Region, string Name, double Fraction, double Tech, double Need, string Era, double Decade);

/// <summary>発見のページの表（設計 §3-3）。値は旧 App の発見パネル（<c>RefreshDiscovery</c>）を本体の目録へ移した物＝式は同じ（エンジンが判定に読む <see cref="Discovery.Evaluate"/> と産地の 2 項）。
/// 発見は 2 値（見つけたら以後失われない）＝解禁済みは 100%。閾値は出さない・割合は出す（作者裁定 2026-09-23）。
/// 並びは解禁度の降順→年代の昇順→名前の全順序（四半期ごとに行が入れ替わらない）。盤面は読むだけ・状態を持たない。</summary>
public static class DiscoveryTables
{
    private static readonly string[] Groups = ["政府", "思想", "技術"];

    private static int ByGate(DiscoveryLine a, DiscoveryLine b)
    {
        int d = b.Fraction.CompareTo(a.Fraction);
        if (d != 0) return d;
        d = a.Decade.CompareTo(b.Decade);
        return d != 0 ? d : string.CompareOrdinal(a.Name, b.Name);
    }

    /// <summary>発見の表の行（政府・思想・技術の順の連続ブロック＝見出しの行は足さず群で区切る）。</summary>
    public static IReadOnlyList<DiscoveryLine> DiscoveryLines(World w)
    {
        var c = w.Coef;
        var gov = new List<DiscoveryLine>(); var ideo = new List<DiscoveryLine>(); var tech = new List<DiscoveryLine>();
        var terms = Discovery.Evaluate(w, w.LastDiscoveryView, c);
        for (int row = 0; row < Catalog.Discoveries.Length; row++)
        {
            var d = Catalog.Discoveries[row];
            bool known = Discovery.Known(w.Discovered, row);
            var tm = terms[row];
            double s = known ? 1.0 : Math.Clamp(tm.Score / Discovery.GateScore, 0, 1);
            if (!(s >= 0)) s = 0;
            string group = d.Field == DiscoveryField.Institution ? Groups[0] : d.Field == DiscoveryField.Idea ? Groups[1] : Groups[2];
            var line = new DiscoveryLine("row." + row, group, d.Name, s, known ? double.NaN : tm.Tech, known ? double.NaN : tm.Need, known ? double.NaN : tm.Transmit, $"{d.Decade}年代", d.Decade);
            (group == Groups[0] ? gov : group == Groups[1] ? ideo : tech).Add(line);
        }
        // 財に紐づかない世界のゲート：電信・鉄道・汽船＝報せの速さ（News の距離ラグが読む世界技術のランプ＝TransportRevolutionOnset/Span）
        if (c.TransportRevolutionSpan > 0)
        {
            double tr = Math.Clamp((w.WorldTechLevel - c.TransportRevolutionOnset) / c.TransportRevolutionSpan, 0, 1);
            if (!(tr >= 0)) tr = 0;
            tech.Add(new DiscoveryLine("transport", Groups[2], "報せの速さ", tr, double.NaN, double.NaN, double.NaN, "", c.TransportRevolutionOnset));
        }
        gov.Sort(ByGate); ideo.Sort(ByGate); tech.Sort(ByGate);
        return gov.Concat(ideo).Concat(tech).ToList();
    }

    /// <summary>発見の表（種類＝政府・思想・技術の群・割合・技術力・必要性・伝わり・年代）。</summary>
    public static Table<DiscoveryLine> Discoveries { get; } = new("discovery.rows", "発見", Page.Discovery, (_, w) => DiscoveryLines(w), l => l.Key, l => l.Name,
    [
        Col.Text<DiscoveryLine>("name", "名前", l => l.Name),
        Col.Number<DiscoveryLine>("fraction", "割合", Unit.Share, l => l.Fraction),
        Col.Number<DiscoveryLine>("tech", "技術力", Unit.Count, l => l.Tech),
        Col.Number<DiscoveryLine>("need", "必要性", Unit.Count, l => l.Need),
        Col.Number<DiscoveryLine>("transmit", "伝わり", Unit.Count, l => l.Transmit),
        Col.Text<DiscoveryLine>("era", "年代", l => l.Era),
    ], l => l.Group);

    /// <summary>産地の発見の行（自国の稼働中の州にある大鉱床と植林）。見つかった産地は 100%・まだの産地は点数（技術力＋必要性）の割合。値はエンジンが判定に読む同じ関数（国の中で 1 度の採点）。</summary>
    public static IReadOnlyList<SiteLine> SiteLines(World w)
    {
        var c = w.Coef;
        var lines = new List<SiteLine>();
        var open = new Dictionary<(string Region, int SiteRow), Discovery.Terms>();
        Discovery.VisitSiteTerms(w, c, (r, p, t, _) => open[(r.Name, p.SiteRow)] = t);
        foreach (var r in w.Regions)
        {
            for (int pi = 0; pi < r.Producers.Count; pi++)
            {
                var p = r.Producers[pi];
                if (p.SiteRow < 0) continue;
                var site = Catalog.Sites[p.SiteRow];
                double sp, tech = double.NaN, need = double.NaN;
                if (p.SiteFound) sp = 1.0;
                else
                {
                    open.TryGetValue((r.Name, p.SiteRow), out var st);
                    sp = Math.Clamp(st.Score / Discovery.GateScore, 0, 1);
                    if (!(sp >= 0)) sp = 0;
                    tech = st.Tech; need = st.Need;
                }
                lines.Add(new SiteLine($"site.{r.Name}.{pi}", r.Name, site.Name, sp, tech, need, $"{site.Decade}年代", site.Decade));
            }
        }
        // 解禁度の降順→年代の昇順→名前（全順序）
        return lines.OrderByDescending(l => l.Fraction).ThenBy(l => l.Decade).ThenBy(l => l.Name, StringComparer.Ordinal).ThenBy(l => l.Region, StringComparer.Ordinal).ToList();
    }

    /// <summary>産地の表（州名・産地・割合・技術力・必要性・年代）。</summary>
    public static Table<SiteLine> Sites { get; } = new("discovery.sites", "産地", Page.Discovery, (_, w) => SiteLines(w), l => l.Key, l => l.Name,
    [
        Col.Text<SiteLine>("region", "州名", l => l.Region),
        Col.Text<SiteLine>("name", "名前", l => l.Name),
        Col.Number<SiteLine>("fraction", "割合", Unit.Share, l => l.Fraction),
        Col.Number<SiteLine>("tech", "技術力", Unit.Count, l => l.Tech),
        Col.Number<SiteLine>("need", "必要性", Unit.Count, l => l.Need),
        Col.Text<SiteLine>("era", "年代", l => l.Era),
    ]);
}
