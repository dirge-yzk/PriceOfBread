namespace PriceOfBread.SimCore;

/// <summary>★(705) 線 dsc1：発見の連続化（改良度 L・1 の上）の列の決め方（設計書 docs/202610/20261030/20261030_発見の連続化_設計.md §3-1・事前登録 measurements/dsc1_705_20261031_prereg.txt [1]）。
/// 列 k＝生産者の発見の行（≧0）・表に無いレシピは「発見の表の行数＋財の番号」（表に無い名前つきのレシピは財ごとに 1 列を既定のレシピと共有する＝限界として名乗る）。
/// 領分＝技術水準の括弧（<see cref="World.TechProductivity"/>）が掛かっていた生産者と同じ＝工場と産地。労働の財（労働力・農業労働力）と役務（高度労働力を含む）は列を持たない。
/// 農家の手仕事（<see cref="Simulation.HouseholdRecipeName"/>）と職人のレシピも列を持たない（古い技法の自営＝工場の改良を分けない）。純関数（状態を書かない）。</summary>
internal static class Improvement
{
    /// <summary>職人の手仕事のレシピ名（<c>Simulation</c> の構築と同じ文字列）。</summary>
    internal const string ArtisanRecipeName = "職人";

    /// <summary>列の数＝発見の表の行数＋財の数。</summary>
    internal static int ColumnCount => Catalog.Discoveries.Length + Catalog.Goods.Length;

    /// <summary>生産者の列（−1＝領分の外）。</summary>
    internal static int ColumnOf(Producer p)
    {
        int g = p.GoodId;
        if (g < 0 || g >= Catalog.Goods.Length) return -1;
        if (g == Catalog.LaborId || g == Catalog.FarmLaborId || Catalog.Goods[g].Tier == GoodTier.Service) return -1;
        if (p.RecipeName == Simulation.HouseholdRecipeName || p.RecipeName == ArtisanRecipeName) return -1;
        int row = p.DiscoveryRow;
        return row >= 0 ? row : Catalog.Discoveries.Length + g;
    }

    /// <summary>列の表示名（計器だけ）＝発見の行の名前か「財の名前/既定」。</summary>
    internal static string ColumnName(int k)
    {
        int nr = Catalog.Discoveries.Length;
        if (k < nr) return Catalog.Discoveries[k].Name;
        int g = k - nr;
        return (uint)g < (uint)Catalog.Goods.Length ? Catalog.Goods[g].Name + "/既定" : "?";
    }

    /// <summary>技術水準の括弧の値（<see cref="World.TechProductivity"/> の括弧と同じ式・計器と番人の手の値の出所ではない＝読み手の写し）。</summary>
    internal static double BracketOf(double techLevel) => 1.0 + System.Math.Min(techLevel, 2.0) + System.Math.Max(0.0, techLevel - 2.0) * 0.3;

    /// <summary>生産者の列の改良度（受容した列で L≧1 のときその値・それ以外は 1）。<paramref name="w"/> が null か配列が無いときも 1。</summary>
    internal static double LevelOf(World? w, Producer p)
    {
        if (w?.ImprovementLevels is not { } imp) return 1.0;
        int k = ColumnOf(p);
        if (k < 0 || k >= imp.Length) return 1.0;
        double l = imp[k];
        return l >= 1.0 ? l : 1.0;   // 否定形の向き＝NaN と 1 未満（未受容）は 1
    }

    /// <summary>投資の techLev の後継の L̄_r＝州の領分の生産者（門が開いた・産出が正）の産出×値段の重みの控え <see cref="Producer.ImprovementCache"/> の平均（重みが 0 なら 1）。</summary>
    internal static double RegionMean(Region r)
    {
        double num = 0, den = 0;
        foreach (var p in r.Producers)
        {
            if (ColumnOf(p) < 0) continue;
            double o = p.Output;
            if (!(o > 0) || !(Discovery.Gate(r, p) > 0)) continue;
            double px = p.GoodId < r.Markets.Length ? r.Markets[p.GoodId].Price : 0;
            if (!(px > 0)) continue;
            double l = p.ImprovementCache; if (!(l >= 1.0)) l = 1.0;
            num += o * px * l; den += o * px;
        }
        return den > 0 ? num / den : 1.0;
    }

    /// <summary>1 四半期の更新の核＝L＋分子÷(3·K_L·L^ρ)−δ·(L−1)（分子＝T＋E＋X・δ＝置き換わり率×(1−E)）。番人 U5 が上界をこの核で確かめる。</summary>
    internal static double Next(double l, double numerator, double k, double rho, double delta)
        => l + (numerator / (3.0 * k * System.Math.Pow(l, rho)) - delta * (l - 1.0));

    /// <summary>開幕の配列（知っている列は 1・知らない発見の表の行は 0）。</summary>
    internal static double[] Opening(World w)
    {
        var imp = new double[ColumnCount];
        int nr = Catalog.Discoveries.Length;
        for (int k = 0; k < imp.Length; k++) imp[k] = k < nr ? (Discovery.Known(w.Discovered, k) ? 1.0 : 0.0) : 1.0;
        return imp;
    }

    /// <summary>★(705) 四半期末の改良度の歩（<c>MultiNationSim.StepDiscoveries</c> の判定と世界の解禁の写しの後・助走の間は呼ばない）。事前登録 measurements/dsc1_705_20261031_prereg.txt [1]：
    /// ΔL＝(T＋E＋X)÷(3·K_L·L^ρ) − δ·(L−1)（L≧1 の列だけ）・T＝D÷(D＋D0)（D＝本国の州の生産者の高度労働力÷本国の人口 千人）・E＝q÷(q＋h·国の需要）・
    /// X＝Σ_p c(n,p)·max(0, 1−L[n,k]÷L[p,k])（c＝輸入に占める相手の割合・前の四半期の組の取引・列ごとに年 1 回〔(列＋四半期) mod 4＝0〕に 4 倍）・δ＝四半期の置き換わり率×(1−E)。
    /// 天井なし（思想⑧）・上界は式で出る（番人 U5）。全ての国の新しい値を古い値から求めてから書く（国の順で X が変わらない）。発見の表の行は判定が立てた所で 0→1（L≧1 ⇔ Discovered）。
    /// 財を持たない行（制度・思想）は {0,1} だけ（増分を積まない）。<paramref name="pairFlow"/>＝[売り手][買い手]（国の添字・長さが国の数と違えば X を積まない＝建国と消滅の四半期）。</summary>
    internal static void Step(IReadOnlyList<World> worlds, double[][]? pairFlow, Coefficients c, int quarter)
    {
        double K = c.ImprovementDivisor;
        if (!(K > 0)) return;
        int n = worlds.Count, nc = ColumnCount, nr = Catalog.Discoveries.Length, ng = Catalog.Goods.Length;
        double rho = c.ImprovementRho, d0 = c.ImprovementEngineerHalf, h = c.ImprovementDemandHalf;
        var olds = new double[n][];
        for (int i = 0; i < n; i++)
        {
            var w = worlds[i];
            w.ImprovementLevels ??= Opening(w);
            var imp = w.ImprovementLevels;
            for (int k = 0; k < nr && k < imp.Length; k++)
            {
                bool known = Discovery.Known(w.Discovered, k);
                if (known && !(imp[k] >= 1.0)) imp[k] = 1.0;
                else if (!known) imp[k] = 0.0;
            }
            olds[i] = (double[])imp.Clone();
        }
        bool xOk = pairFlow is not null && pairFlow.Length == n;
        var qs = new double[nc]; var es = new double[nc]; var dg = new double[ng];
        for (int i = 0; i < n; i++)
        {
            var w = worlds[i]; var old = olds[i]; var imp = w.ImprovementLevels!;
            System.Array.Clear(qs); System.Array.Clear(es); System.Array.Clear(dg);
            double homePop = 0; long visited = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                bool home = r.Tier != RegionTier.Colonial;
                if (home) foreach (var pp in r.Pops) if (!pp.IsSoldier && pp.Size > 0) homePop += pp.Size;
                for (int g = 0; g < ng && g < r.Markets.Length; g++) { double d = r.Markets[g].LastDemand; if (d > 0) dg[g] += d; }
                foreach (var pr in r.Producers)
                {
                    visited++;
                    int k = ColumnOf(pr);
                    if (k < 0 || !(Discovery.Gate(r, pr) > 0)) continue;
                    double o = pr.Output;
                    if (o > 0) qs[k] += o;
                    if (home) es[k] += Discovery.EngineerUse(pr);
                }
            }
            double rep = w.ReplacementRate(Calendar.MonthsPerQuarter);
            double impSum = 0; if (xOk) for (int s = 0; s < n; s++) { double v = pairFlow![s] is { } row && i < row.Length ? row[i] : 0; if (s != i && v > 0) impSum += v; }
            long updated = 0;
            for (int k = 0; k < nc && k < imp.Length; k++)
            {
                double l = old[k];
                if (!(l >= 1.0)) continue;   // 未受容（0）と NaN は積まない
                int g = k < nr ? Catalog.Discoveries[k].Good : k - nr;
                if (g < 0) continue;          // 財を持たない行（制度・思想）は {0,1}
                updated++;
                double D = homePop > 0 ? es[k] / homePop : 0;
                double T = D > 0 ? D / (D + d0) : 0;
                double den = qs[k] + h * (g < ng ? dg[g] : 0);
                double E = den > 0 ? qs[k] / den : 0;
                double X = 0;
                if (xOk && impSum > 0 && (k + quarter) % 4 == 0)
                {
                    for (int s = 0; s < n; s++)
                    {
                        if (s == i || pairFlow![s] is not { } row || i >= row.Length) continue;
                        double v = row[i]; if (!(v > 0)) continue;
                        var ps = olds[s]; if (k >= ps.Length) continue;
                        double lp = ps[k]; if (!(lp >= 1.0)) continue;
                        double gap = 1.0 - l / lp;
                        if (gap > 0) X += (v / impSum) * gap;
                    }
                    X *= 4.0;
                }
                double nl = Next(l, T + E + X, K, rho, rep * (1.0 - E));
                if (double.IsFinite(nl)) imp[k] = nl;   // 非有限は書かない（前の値のまま）
            }
            w.Cost[CostLedger.ImprovementScans] += visited + updated;   // 計算量の台帳（診断・旗が正のときだけ）
        }
    }
}
