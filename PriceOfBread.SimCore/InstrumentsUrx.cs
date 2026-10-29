namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (649) 都市と農村の規模の差の区間別の表 urx_s と PopDecision の駆動の表 urxpd_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEpi.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない・CostLedger を触らない。
// 事前登録 measurements/urx_649_20261030_prereg.txt（[2]・追記 1 の [9]・追記 2 の [10] ⓔⓕ）。
// ■urx_s<種>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国だけ）：国×群（Core／Other＝州の Tier の 2 群）×区間×四半期の、都市と農村の POP の規模の差（千人）。
//   印＝ProbeCheck の頭（UrxMark）。区間名＝その印の名前＝直前の印から今の印までの差。UrxEnter／UrxExit＝社会移動（Meritocracy.Step）と疫病（ApplyEpidemicDeaths）の前後で差を切り出す。
//   全ての区間の差の和＝（最後の水準−最初の水準）に構成上一致する（桁落ちの残差だけ）。助走（WarmupFrozen）の間は印を打たず、助走が明けた最初の印で据え直す（その印の差は書かない）。
//   列：quarter／year／nation／group／section／months（印の数）／dUrbanK／dRuralK。四半期の末に section＝_level（Fold の時点の水準）、最初の Fold に section＝_base（据え直した水準）。
// ■urxpd_s<種>.tsv：本国（Tier＝Core）の州の PopDecision.Run の直前の駆動（読むだけ・同じ式を組み直す）。列は UrxPdHeaderText。重みの付く列（w*）は total（pair の規模）で重みを付けた和＝読み手が sumTotal で割る。
// ⚠限界（計器が自己申告する）：①印の区間は〈その区間の間に起きた全て〉の差（月次政治には PopDecision のほか外交の間に挟まる人の動きも入る＝urxpd_s の sumFlowPos−sumFlowNeg と比べる）
//   ②群は州の Tier の印＝印の間に Tier が変わった州の差は変わった側の群へ入る（国の合計では消える）③urxpd_s は PopDecision の式の写し＝式が変わったら番人（UrxInstrumentTests）が赤になる。
public static partial class Instruments
{
    internal const string UrxHeaderText = "quarter	year	nation	group	section	months	dUrbanK	dRuralK";
    internal const string UrxPdHeaderText = "quarter	year	nation	regionMonths	noPair	capBound	serfGated	famineGated	sumFlowPos	sumFlowNeg	sumTotal	sumPairUrban	sumRegionPop	sumRegionUrban	wTargetPre	wTargetCapped	wCur	wCap	wIncU	wIncR	wZ";
    static readonly string[] UrxGroups = ["Core", "Other"];

    sealed class UrxState
    {
        public bool Armed;
        public readonly double[] PrevU = new double[2], PrevR = new double[2], CarryU = new double[2], CarryR = new double[2];
        public readonly double[] BaseU = new double[2], BaseR = new double[2];
        public bool BaseWritten;
        public readonly List<string> Order = [];                              // 区間名の初出の順（行の順を決定論にする）
        public readonly Dictionary<string, UrxAcc> Acc = new();
        // urxpd_s の累積
        public long PdRegionMonths, PdNoPair, PdCapBound, PdSerfGated, PdFamineGated;
        public double PdFlowPos, PdFlowNeg, PdTotal, PdPairUrban, PdRegionPop, PdRegionUrban, PdWTargetPre, PdWTargetCapped, PdWCur, PdWCap, PdWIncU, PdWIncR, PdWZ;
    }
    sealed class UrxAcc { public readonly double[] U = new double[2], R = new double[2]; public int N; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, UrxState> UrxStates = new();
    static UrxState Ux(World w) => UrxStates.GetValue(w, _ => new UrxState());

    /// <summary>今の水準（群ごとの Σ都市 POP と Σ農村 POP の規模）。全ての州（active でなくても POP の規模を数える＝SumUrbanPopulation／SumRuralPopulation と同じ集合）。</summary>
    static void UrxLevel(World w, double[] u, double[] r)
    {
        u[0] = u[1] = r[0] = r[1] = 0;
        foreach (var rg in w.Regions)
        {
            int g = rg.Tier == RegionTier.Core ? 0 : 1;
            foreach (var p in rg.Pops) { if (p.IsUrban) u[g] += p.Size; else r[g] += p.Size; }
        }
    }

    /// <summary>ProbeCheck の頭の印：直前の印（と持ち越し）から今までの差を <paramref name="label"/> の区間へ足す。読むだけ。</summary>
    internal static void UrxMark(World w, string label)
    {
        if (!On || w.WarmupFrozen) return;
        var st = S(w); if (!IsPicked(w, st)) return;
        var ux = Ux(w);
        var cu = new double[2]; var cr = new double[2];
        UrxLevel(w, cu, cr);
        if (!ux.Armed)
        {
            for (int g = 0; g < 2; g++) { ux.PrevU[g] = cu[g]; ux.PrevR[g] = cr[g]; ux.BaseU[g] = cu[g]; ux.BaseR[g] = cr[g]; }
            ux.Armed = true;
            return;
        }
        if (!ux.Acc.TryGetValue(label, out var a)) { a = new UrxAcc(); ux.Acc[label] = a; ux.Order.Add(label); }
        for (int g = 0; g < 2; g++)
        {
            a.U[g] += cu[g] - ux.PrevU[g] + ux.CarryU[g]; a.R[g] += cr[g] - ux.PrevR[g] + ux.CarryR[g];
            ux.PrevU[g] = cu[g]; ux.PrevR[g] = cr[g]; ux.CarryU[g] = 0; ux.CarryR[g] = 0;
        }
        a.N++;
    }

    /// <summary>区間を切り出す前：ここまでの差を持ち越しへ退避して前の印を今に据える（その差は次の普通の印の名前の区間に入る）。助走中は何もしない。</summary>
    internal static void UrxEnter(World w)
    {
        if (!On || w.WarmupFrozen) return;
        var st = S(w); if (!IsPicked(w, st)) return;
        var ux = Ux(w); if (!ux.Armed) return;
        var cu = new double[2]; var cr = new double[2];
        UrxLevel(w, cu, cr);
        for (int g = 0; g < 2; g++) { ux.CarryU[g] += cu[g] - ux.PrevU[g]; ux.CarryR[g] += cr[g] - ux.PrevR[g]; ux.PrevU[g] = cu[g]; ux.PrevR[g] = cr[g]; }
    }

    /// <summary>区間を切り出した後：今との差を <paramref name="label"/> の区間へ足して前の印を今に据える。</summary>
    internal static void UrxExit(World w, string label)
    {
        if (!On || w.WarmupFrozen) return;
        var st = S(w); if (!IsPicked(w, st)) return;
        var ux = Ux(w); if (!ux.Armed) return;
        var cu = new double[2]; var cr = new double[2];
        UrxLevel(w, cu, cr);
        if (!ux.Acc.TryGetValue(label, out var a)) { a = new UrxAcc(); ux.Acc[label] = a; ux.Order.Add(label); }
        for (int g = 0; g < 2; g++) { a.U[g] += cu[g] - ux.PrevU[g]; a.R[g] += cr[g] - ux.PrevR[g]; ux.PrevU[g] = cu[g]; ux.PrevR[g] = cr[g]; }
        a.N++;
    }

    /// <summary>PopDecision.Run の直前：本国の州の駆動を PopDecision.Run と同じ式で組み直して累積する（読むだけ）。<c>UrbanIndSmoothed</c> は書かない（更新後の値を局所で計算）。</summary>
    internal static void UrxPd(World w, Region region, double serfFree)
    {
        if (!On || w.WarmupFrozen || region.Tier != RegionTier.Core) return;
        var st = S(w); if (!IsPicked(w, st)) return;
        var ux = Ux(w);
        if (!UrxPdDrive(w.Coef, region, serfFree, out var d)) { ux.PdNoPair++; return; }
        ux.PdRegionMonths++;
        if (d.CapBound) ux.PdCapBound++;
        if (d.SerfGated) ux.PdSerfGated++;
        if (d.FamineGated) ux.PdFamineGated++;
        if (d.Flow > 0) ux.PdFlowPos += d.Flow; else if (d.Flow < 0) ux.PdFlowNeg += -d.Flow;
        ux.PdTotal += d.Total; ux.PdPairUrban += d.UrbanSize;
        ux.PdRegionPop += region.SumPopulation(); ux.PdRegionUrban += region.SumUrbanPopulation();
        ux.PdWTargetPre += d.Total * d.TargetPre; ux.PdWTargetCapped += d.Total * d.TargetCapped; ux.PdWCur += d.Total * d.Cur; ux.PdWCap += d.Total * d.Cap;
        ux.PdWIncU += d.Total * d.IncUrban; ux.PdWIncR += d.Total * d.IncRural; ux.PdWZ += d.Total * d.Z;
    }

    internal readonly record struct UrxPdDriveRow(double Total, double UrbanSize, double IncUrban, double IncRural, double Z, double TargetPre, double TargetCapped, double Cur, double Cap, bool CapBound, bool SerfGated, bool FamineGated, double Flow);

    /// <summary>PopDecision.Run の式の写し（読むだけ）。農村と都市の対が無い・規模が 0 なら false。流れ＝正は農村→都市の人数（千人）。番人（UrxInstrumentTests）が本物の Run の前後の差と突き合わせる。</summary>
    internal static bool UrxPdDrive(Coefficients coef, Region region, double serfFree, out UrxPdDriveRow d)
    {
        d = default;
        if (region.Pops.Count < 2) return false;
        Pop? rural = null, urban = null;
        foreach (var p in region.Pops)
        {
            if (p.Closed || p.IsCriminal) continue;
            if (p.IsUrban) urban ??= p;
            else rural ??= p;
        }
        if (rural is null || urban is null) return false;
        double incRural = rural.Size > 1e-9 ? (rural.EarnedPerMonth / rural.Size).Value : 0.0;
        double incUrban = urban.Size > 1e-9 ? (urban.EarnedPerMonth / urban.Size).Value : 0.0;
        double total = rural.Size + urban.Size;
        if (total <= 1e-9) return false;
        double z = Math.Clamp((incUrban - incRural) / coef.MigrationTemperature, -40, 40);
        double targetPre = 1.0 / (1.0 + Math.Exp(-z));
        double target = targetPre, cur = urban.Size / total, cap = 1.0;
        bool capBound = false;
        if (region.CapitalEnabled && !(coef.UrbanCapOff > 0))
        {
            double indForCap = region.TotalIndustrialization;
            if (coef.UrbanCapFollowSpeed < 1.0)
            {
                double sm = region.UrbanIndSmoothed;
                if (sm < 0) sm = region.TotalIndustrialization;
                sm += coef.UrbanCapFollowSpeed * (region.TotalIndustrialization - sm);
                indForCap = sm;
            }
            cap = PopDecision.UrbanShareCap(coef, indForCap);
            if (target > cap) { target = cap; capBound = true; }
        }
        double flow = coef.Mobility * (target - cur) * total;
        bool serf = false, famine = false;
        if (flow > 0 && coef.SerfMobilityGate > 0)
        {
            double f = (1.0 - coef.SerfMobilityGate) + coef.SerfMobilityGate * serfFree;
            if (f < 1.0) serf = true;
            flow *= f;
        }
        if (flow > 0 && urban.FoodFill < coef.FamineMigrationFillGate && urban.FoodFill < rural.FoodFill) { flow = 0; famine = true; }
        d = new UrxPdDriveRow(total, urban.Size, incUrban, incRural, z, targetPre, target, cur, cap, capBound, serf, famine, flow);
        return true;
    }

    static void UrxFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        if (!IsPicked(w, st)) return;
        if (!UrxStates.TryGetValue(w, out var ux) || !ux.Armed) return;
        UrxMark(w, "四半期末後");   // Fold の時点までの差を締める＝_level が region_s と同じ時点の水準になる
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nm = NameOf(w), qs = I(q), year = I(w.Year);
        sk.Urx ??= Open("urx", seed);
        if (!sk.UrxHeader) { sk.Urx.WriteLine(UrxHeaderText); sk.UrxHeader = true; }
        if (!ux.BaseWritten)
        {
            for (int g = 0; g < 2; g++) sk.Urx.WriteLine(string.Join('	', qs, year, nm, UrxGroups[g], "_base", "0", F(ux.BaseU[g]), F(ux.BaseR[g])));
            ux.BaseWritten = true;
        }
        foreach (var name in ux.Order)
        {
            var a = ux.Acc[name];
            if (a.N == 0) continue;
            for (int g = 0; g < 2; g++) sk.Urx.WriteLine(string.Join('	', qs, year, nm, UrxGroups[g], name, I(a.N), F(a.U[g]), F(a.R[g])));
            Array.Clear(a.U); Array.Clear(a.R); a.N = 0;
        }
        for (int g = 0; g < 2; g++) sk.Urx.WriteLine(string.Join('	', qs, year, nm, UrxGroups[g], "_level", "0", F(ux.PrevU[g]), F(ux.PrevR[g])));
        if (ux.PdRegionMonths + ux.PdNoPair > 0)
        {
            sk.UrxPdW ??= Open("urxpd", seed);
            if (!sk.UrxPdHeader) { sk.UrxPdW.WriteLine(UrxPdHeaderText); sk.UrxPdHeader = true; }
            sk.UrxPdW.WriteLine(string.Join('	', qs, year, nm, ux.PdRegionMonths.ToString(System.Globalization.CultureInfo.InvariantCulture), ux.PdNoPair.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ux.PdCapBound.ToString(System.Globalization.CultureInfo.InvariantCulture), ux.PdSerfGated.ToString(System.Globalization.CultureInfo.InvariantCulture), ux.PdFamineGated.ToString(System.Globalization.CultureInfo.InvariantCulture),
                F(ux.PdFlowPos), F(ux.PdFlowNeg), F(ux.PdTotal), F(ux.PdPairUrban), F(ux.PdRegionPop), F(ux.PdRegionUrban),
                F(ux.PdWTargetPre), F(ux.PdWTargetCapped), F(ux.PdWCur), F(ux.PdWCap), F(ux.PdWIncU), F(ux.PdWIncR), F(ux.PdWZ)));
            ux.PdRegionMonths = ux.PdNoPair = ux.PdCapBound = ux.PdSerfGated = ux.PdFamineGated = 0;
            ux.PdFlowPos = ux.PdFlowNeg = ux.PdTotal = ux.PdPairUrban = ux.PdRegionPop = ux.PdRegionUrban = 0;
            ux.PdWTargetPre = ux.PdWTargetCapped = ux.PdWCur = ux.PdWCap = ux.PdWIncU = ux.PdWIncR = ux.PdWZ = 0;
        }
    }
}
