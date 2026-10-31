namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (649) 都市と農村の規模の差の区間別の表 urx_s と PopDecision の駆動の表 urxpd_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEpi.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない・CostLedger を触らない。
// 事前登録 measurements/urx_649_20261030_prereg.txt（[2]・追記 1 の [9]・追記 2 の [10] ⓔⓕ）。
// ■urx_s<種>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国だけ）：国×群（Core／Other＝州の Tier の 2 群）×区間×四半期の、都市と農村の POP の規模の差（千人）。
//   印＝ProbeCheck の頭（UrxMark）。区間名＝その印の名前＝直前の印から今の印までの差。UrxEnter／UrxExit＝社会移動（Meritocracy.Step）と疫病（ApplyEpidemicDeaths）の前後で差を切り出す。
//   全ての区間の差の和＝（最後の水準−最初の水準）に構成上一致する（桁落ちの残差だけ）。助走（WarmupFrozen）の間は印を打たず、助走が明けた最初の印で据え直す（その印の差は書かない）。
//   列：quarter／year／nation／group／section／months（印の数）／dUrbanK／dRuralK。四半期の末に section＝_level（Fold の時点の水準）、最初の Fold に section＝_base（据え直した水準）。
// ■urxpd_s<種>.tsv：本国（Tier＝Core）の州の PopDecision.Run の直前の駆動（読むだけ・同じ式を組み直す）。列は UrxPdHeaderText。重みの付く列（w*）は total（pair の規模）で重みを付けた和＝読み手が sumTotal で割る。
// ■(706) urb3：urxpd_s の末尾に 4 列（既存の列の並びと値は変えない）。wSubV＝対の農村の 1 人あたりの自給の値（月・名目）×total の和／wHousU＝対の都市の 1 人あたりの住居の費用×total の和／wGsym＝〈移る値打ち〉の対称の相対差 g×total の和（式は本物の PopDecision.MigrationValues＝旗が 0 でも出す）／
//   flowFlipRegionMonths＝流れの向きが前の月と逆の州×月の数（前の月も今の月も流れが 0 でなく、符号が違う＝飢えの門で 0 になった月は数えず、その次の月の比べ先にもならない）。読み手が wX÷sumTotal で重みを付けた平均にする。
// ⚠限界（計器が自己申告する）：①印の区間は〈その区間の間に起きた全て〉の差（月次政治には PopDecision のほか外交の間に挟まる人の動きも入る＝urxpd_s の sumFlowPos−sumFlowNeg と比べる）
//   ②群は州の Tier の印＝印の間に Tier が変わった州の差は変わった側の群へ入る（国の合計では消える）③urxpd_s は PopDecision の式の写し＝式が変わったら番人（UrxInstrumentTests）が赤になる。
public static partial class Instruments
{
    internal const string UrxHeaderText = "quarter	year	nation	group	section	months	dUrbanK	dRuralK";
    internal const string UrxPdHeaderText = "quarter	year	nation	regionMonths	noPair	capBound	serfGated	famineGated	sumFlowPos	sumFlowNeg	sumTotal	sumPairUrban	sumRegionPop	sumRegionUrban	wTargetPre	wTargetCapped	wCur	wCap	wIncU	wIncR	wZ	wSubV	wHousU	wGsym	flowFlipRegionMonths";
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
        public double PdWSubV, PdWHousU, PdWGsym;                                     // (706) urb3：〈移る値打ち〉の列（total で重みを付けた和）
        public long PdFlipRegionMonths;                                               // (706) urb3：流れの向きが前の月と逆の州×月の数
        public readonly Dictionary<Region, int> PdPrevSign = new(ReferenceEqualityComparer.Instance);   // (706) urb3：州ごとの前の月の流れの向き（−1／0／+1）。四半期をまたいで持つ（累積と違い Fold で消さない）
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
        if (!UrxPdDrive(w.Coef, region, serfFree, out var d)) { ux.PdNoPair++; ux.PdPrevSign[region] = 0; return; }
        ux.PdRegionMonths++;
        if (d.CapBound) ux.PdCapBound++;
        if (d.SerfGated) ux.PdSerfGated++;
        if (d.FamineGated) ux.PdFamineGated++;
        if (d.Flow > 0) ux.PdFlowPos += d.Flow; else if (d.Flow < 0) ux.PdFlowNeg += -d.Flow;
        ux.PdTotal += d.Total; ux.PdPairUrban += d.UrbanSize;
        ux.PdRegionPop += region.SumPopulation(); ux.PdRegionUrban += region.SumUrbanPopulation();
        ux.PdWTargetPre += d.Total * d.TargetPre; ux.PdWTargetCapped += d.Total * d.TargetCapped; ux.PdWCur += d.Total * d.Cur; ux.PdWCap += d.Total * d.Cap;
        ux.PdWIncU += d.Total * d.IncUrban; ux.PdWIncR += d.Total * d.IncRural; ux.PdWZ += d.Total * d.Z;
        ux.PdWSubV += d.Total * d.SubsistenceValue; ux.PdWHousU += d.Total * d.HousingCost; ux.PdWGsym += d.Total * d.Gsym;   // (706) urb3
        int sgn = d.Flow > 0 ? 1 : d.Flow < 0 ? -1 : 0;   // (706) urb3：流れの向きの反転（前の月も今の月も 0 でなく、向きが逆）
        if (ux.PdPrevSign.TryGetValue(region, out int prevSgn) && prevSgn != 0 && sgn != 0 && prevSgn != sgn) ux.PdFlipRegionMonths++;
        ux.PdPrevSign[region] = sgn;
    }

    /// <summary>番人用：今の四半期の urxpd_s の積みの写し（積みは消さない）。(706) 流れの向きの反転と〈移る値打ち〉の列を単体で確かめる口。</summary>
    internal static (long RegionMonths, long FlipRegionMonths, double WSubV, double WHousU, double WGsym, double SumTotal) UrxPdPeek(World w)
    {
        var ux = Ux(w);
        return (ux.PdRegionMonths, ux.PdFlipRegionMonths, ux.PdWSubV, ux.PdWHousU, ux.PdWGsym, ux.PdTotal);
    }

    internal readonly record struct UrxPdDriveRow(double Total, double UrbanSize, double IncUrban, double IncRural, double Z, double TargetPre, double TargetCapped, double Cur, double Cap, bool CapBound, bool SerfGated, bool FamineGated, double Flow, double SubsistenceValue, double HousingCost, double Gsym);

    /// <summary>PopDecision.Run の式の写し（読むだけ）。農村と都市の対が無い・規模が 0 なら false。流れ＝正は農村→都市の人数（千人）。番人（UrxInstrumentTests）が本物の Run の前後の差と突き合わせる。</summary>
    internal static bool UrxPdDrive(Coefficients coef, Region region, double serfFree, out UrxPdDriveRow d)
    {
        d = default;
        if (region.Pops.Count < 2) return false;
        var (rural, urban) = PopDecision.MigrationPair(region);
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
        // (706) urb3：〈移る値打ち〉V の列は常に出す（本物の式 PopDecision.MigrationValues を呼ぶ＝写しではない）。流れは旗 UrbanFlowReal に従い Run と同じ式
        var mv = PopDecision.MigrationValues(region, coef, rural, urban);
        double flow = coef.UrbanFlowReal > 0 ? (mv.Gsym > 0 ? coef.Mobility * mv.Gsym * rural.Size : coef.Mobility * mv.Gsym * urban.Size) : coef.Mobility * (target - cur) * total;
        bool serf = false, famine = false;
        if (flow > 0 && coef.SerfMobilityGate > 0)
        {
            double f = (1.0 - coef.SerfMobilityGate) + coef.SerfMobilityGate * serfFree;
            if (f < 1.0) serf = true;
            flow *= f;
        }
        if (flow > 0 && urban.FoodFill < coef.FamineMigrationFillGate && urban.FoodFill < rural.FoodFill) { flow = 0; famine = true; }
        d = new UrxPdDriveRow(total, urban.Size, incUrban, incRural, z, targetPre, target, cur, cap, capBound, serf, famine, flow, mv.SubsistenceValue, mv.HousingCost, mv.Gsym);
        return true;
    }

    // ---------------------------------------------------------------- (697) 往復の表 urxrt_s（読むだけ・恒等） ----------------------------------------------------------------
    // 人の移し方の 1 本（Pop.Blend）の呼び手のファイルごとに、国の都市 POP への入りと出の総量（千人）を数える。純（入−出）は urx_s の区間・urxpd_s の流れと同じ向きで、往復の総量＝入＋出。
    // 数え方：同じ国の中の移動は農村→都市が入り・都市→農村が出（都市どうし・農村どうしは数えない）。国境を越える移動（越境移民・招聘）は受け手の国の都市 POP への着が入り・送り手の国の都市 POP からの発が出。
    // route＝Blend の呼び手のファイル名（PopDecision＝州の中の農村↔都市・Meritocracy＝社会移動の職の移し・Territory＝州の間の移民と入植・Military＝徴兵と復員・MultiNationSim＝越境）。
    // ⚠限界：①挙げた国の POP だけ（PopOwner の登録は月初＝その月の途中に生まれた POP の移動は数えない）②助走（その国の月が Active でない間）は数えない③呼び手のファイル 1 本の中に別の動詞が混ざる（Territory など）ときは区別しない
    //   ④疫病・出生・死は人の移し方を通らない＝数えない（urx_s の区間の差とは別の物）。
    internal const string UrxRtHeaderText = "quarter\tyear\tnation\troute\turbanInK\turbanOutK\tcalls";
    sealed class UrxRtAcc { public readonly Dictionary<string, double[]> Routes = new(); public readonly List<string> Order = []; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<WState, UrxRtAcc> UrxRtStates = new();

    /// <summary>Pop.Blend の末尾（読むだけ）：<paramref name="n"/> 人が <paramref name="from"/> から <paramref name="to"/> へ移った。呼び手のファイル名ごとに挙げた国の都市への入りと出を積む。</summary>
    internal static void UrxRtFlow(Pop from, Pop to, double n, string routeFile)
    {
        if (!On) return;
        bool srcUrban = from.IsUrban, dstUrban = to.IsUrban;
        if (!srcUrban && !dstUrban) return;
        PopOwner.TryGetValue(from, out var sf); PopOwner.TryGetValue(to, out var stt);
        if (sf is null && stt is null) return;
        bool same = sf is not null && ReferenceEquals(sf, stt);
        if (same && srcUrban == dstUrban) return;
        int cut = Math.Max(routeFile.LastIndexOf('/'), routeFile.LastIndexOf('\\')) + 1;
        int dot = routeFile.LastIndexOf('.');
        string route = dot > cut ? routeFile.Substring(cut, dot - cut) : routeFile.Substring(cut);
        if (dstUrban && stt is not null && stt.Active && (!same || !srcUrban)) UrxRtAdd(stt, route, n, 0);
        if (srcUrban && sf is not null && sf.Active && (!same || !dstUrban)) UrxRtAdd(sf, route, 0, n);
    }

    static void UrxRtAdd(WState st, string route, double inK, double outK)
    {
        var acc = UrxRtStates.GetValue(st, _ => new UrxRtAcc());
        if (!acc.Routes.TryGetValue(route, out var a)) { a = new double[3]; acc.Routes[route] = a; acc.Order.Add(route); }
        a[0] += inK; a[1] += outK; a[2] += 1;
    }

    /// <summary>番人用：今の四半期の積み（route → 入り・出・呼びの数）の写し。積みは消さない。</summary>
    internal static Dictionary<string, (double In, double Out, double Calls)> UrxRtPeek(World w)
    {
        var res = new Dictionary<string, (double In, double Out, double Calls)>();
        if (UrxRtStates.TryGetValue(S(w), out var acc)) foreach (var k in acc.Order) { var a = acc.Routes[k]; res[k] = (a[0], a[1], a[2]); }
        return res;
    }

    static void UrxRtFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        if (!UrxRtStates.TryGetValue(st, out var acc) || acc.Order.Count == 0) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.UrxRt ??= Open("urxrt", seed);
        if (!sk.UrxRtHeader) { sk.UrxRt.WriteLine(UrxRtHeaderText); sk.UrxRtHeader = true; }
        foreach (var name in acc.Order)
        {
            var a = acc.Routes[name];
            if (a[2] > 0) sk.UrxRt.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), name, F(a[0]), F(a[1]), F(a[2])));
            a[0] = a[1] = a[2] = 0;
        }
    }

    static void UrxFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        if (!IsPicked(w, st)) return;
        UrxRtFold(sk, seed, w, st, q);   // (697) 往復の表（Armed でなくても書く）
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
                F(ux.PdWTargetPre), F(ux.PdWTargetCapped), F(ux.PdWCur), F(ux.PdWCap), F(ux.PdWIncU), F(ux.PdWIncR), F(ux.PdWZ),
                F(ux.PdWSubV), F(ux.PdWHousU), F(ux.PdWGsym), ux.PdFlipRegionMonths.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            ux.PdRegionMonths = ux.PdNoPair = ux.PdCapBound = ux.PdSerfGated = ux.PdFamineGated = 0;
            ux.PdFlowPos = ux.PdFlowNeg = ux.PdTotal = ux.PdPairUrban = ux.PdRegionPop = ux.PdRegionUrban = 0;
            ux.PdWTargetPre = ux.PdWTargetCapped = ux.PdWCur = ux.PdWCap = ux.PdWIncU = ux.PdWIncR = ux.PdWZ = 0;
            ux.PdWSubV = ux.PdWHousU = ux.PdWGsym = 0; ux.PdFlipRegionMonths = 0;   // (706) urb3
        }
    }
}
