namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (888) 割譲の支配度 C1 の段 C-0 の表 cederule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCess.cs・InstrumentsInheritIntent.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない
//   （蓄えは計器の側の ConditionalWeakTable〈試合の Diplomacy ごと〉・World・Region・Sink に欄を作らない＝セーブに触れない）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/m888_20261106_prereg_ce.txt（形 Q′・下書き docs/202611/20261106/20261106_割譲の支配度C1の段C-0_州ごとの治安P1-1の後_下書き.md §4・§10・AI の案・確証なし）。
//   州が別の国へ移る（Territory.TransferRegion＝割譲・分離独立・再併合）事象 1 件ごとに、移る時点（前＝Instruments.RegionMoved・Rehome の前）と同じ四半期の末（後＝Instruments.Quarter）の
//   支配の 4 区分・在地の開幕の値・支配度・外国持分を並べる。旗 0 の世界では前＝後（割譲から四半期の末までに Rule・RuleInit・Dominance の書き手は走らない＝事前登録 [6] ①）。
//   段 C-1（C1＝割譲の 1 回だけ Rule[1] += Rule[0]; Rule[0] = 0・H-a＝受け手の鍵の持分を 0）の見込みの列を計器が自分で当てる（エンジンの関数を呼ばない）。
// 行＝RegionMoved を通る事象 1 件につき 1 行（Ceded・Seceded・Returned・全ての国・POB_INSTRUMENTS_NATIONS の絞りは読まない・助走の間の事象は捨てる）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元・K＝千人・Count＝件数・Flag＝0／1・Sum＝持分の和）：
//   quarter（Quarter の q）／year（RegionMoved が受け取る from の Year）／how／sender（送り手の鍵）／receiver（Ceded・Returned は to の鍵・Seceded は四半期の末の持ち主の鍵）／region／tierBefore・tierAfter／
//   sameCoreFlag（両国の主流文化 Power.Culture の一致・Seceded は nan）／popK（前の州の人口）／receiverPopShare（四半期の末の州の人口÷持ち主の全ての州の人口）／
//   rule*BeforeShare（R0〜R3）・ruleInitLocalBeforeShare（RuleInit[1]）・dominanceBeforeShare・foreignRulerBeforeCount・foreignShareBeforeSum（正の持分の和）・receiverOwnBeforeShare（受け手の鍵の持分）・
//   thirdPartyBeforeSum（受け手の鍵を除いた正の持分の和）・adminFillBeforeShare・deliveryBeforeShare（R0×(d＋(1−d)×AdminFill)＋R1×d・d＝1−PrivilegeTaxDiscount）・reachBeforeShare（R0）／
//   c1StateShare（0）・c1LocalShare（前の R0＋R1）・c1DeliveryShare（四半期の末の AdminFill）・c1ReachShare（0）・haOwnShare（0）／
//   rule*AfterShare・ruleInitLocalAfterShare・dominanceAfterShare・foreignRulerAfterCount・foreignShareAfterSum・receiverOwnAfterShare・thirdPartyAfterSum・adminFillAfterShare・deliveryAfterShare・reachAfterShare／
//   ownerAtQuarterEnd（Region の参照を sim.Nations の全ての World の州一覧から探す・見つからなければ空）／militancyRegionIndex（州の平民の人口加重の闘争度＝RuleCrowdMilitancy）／
//   ungovTargetOldShare・ungovTargetNewShare（rule_s と同じ計器の式・持ち主は四半期の末）／movedAgainFlag（同じ州が同じ四半期の事象に 2 回以上あるか、Ceded・Returned で四半期の末の持ち主が to でない）／
//   foreignAddedFlag（外国持分の主体の数か正の持分の和が増えた＝同じ四半期に AddForeignShare が当たった印）／pickedReceiverFlag（四半期の末の持ち主が計器の国）／
//   identDiffMaxIndex（前と後の |差| の最大＝R0〜R3・RuleInit の全ての要素・Dominance・外国持分の全ての要素・要素の数が違えば inf・double のまま数える）・identBreakFlag（identDiffMaxIndex が CedeIdentTol 以下でなければ 1）。
// ⚠限界（計器が自分で申告する）：①独立の確定（Diplomacy.cs の Territory.Rehome(Sovereign) の直の呼び）は RegionMoved を通らない＝表に無い（同じ四半期に当たった州は identBreakFlag と tierAfter・dominance で見える）。
//   ②単国の世界（World.Diplomacy が null）は行を出さない。③試合が四半期の途中で終わると、その四半期の事象は書かない。④sameCoreFlag は Power.Culture（毎月の同期の値）の一致＝割譲の呼び手が渡す sameCore と同じ読み。
//   ⑤受け手の鍵の式は Diplomacy.cs の AddForeignShare の呼び口と同じ式を計器が自分で書いた（(774) の InheritIntentBefore／After がエンジンと同じ純関数を呼ぶ前例とは違う）。ungovTarget の 2 列だけは rule_s と同じく Politics.RegionReach を読む。
//   ⑥実の盤面では RegionMoved の時の from.Year と Quarter の時の Year は同じ値（Year の繰り上げ Simulation.QuarterEnd は共有層より前）＝year の取り方の違いは手の盤面でしか見えない。
public static partial class Instruments
{
    internal const string CedeHeaderText = "quarter\tyear\thow\tsender\treceiver\tregion\ttierBefore\ttierAfter\tsameCoreFlag\tpopK\treceiverPopShare\t"
        + "ruleStateBeforeShare\truleLocalBeforeShare\truleForeignBeforeShare\truleUngovBeforeShare\truleInitLocalBeforeShare\tdominanceBeforeShare\t"
        + "foreignRulerBeforeCount\tforeignShareBeforeSum\treceiverOwnBeforeShare\tthirdPartyBeforeSum\tadminFillBeforeShare\tdeliveryBeforeShare\treachBeforeShare\t"
        + "c1StateShare\tc1LocalShare\tc1DeliveryShare\tc1ReachShare\thaOwnShare\t"
        + "ruleStateAfterShare\truleLocalAfterShare\truleForeignAfterShare\truleUngovAfterShare\truleInitLocalAfterShare\tdominanceAfterShare\t"
        + "foreignRulerAfterCount\tforeignShareAfterSum\treceiverOwnAfterShare\tthirdPartyAfterSum\tadminFillAfterShare\tdeliveryAfterShare\treachAfterShare\t"
        + "ownerAtQuarterEnd\tmilitancyRegionIndex\tungovTargetOldShare\tungovTargetNewShare\tmovedAgainFlag\tforeignAddedFlag\tpickedReceiverFlag\tidentDiffMaxIndex\tidentBreakFlag";

    /// <summary>旗 0 の恒等の門の許し（前と後の差の最大・double で数える＝印字の桁 G10 の床に置かない）。</summary>
    internal const double CedeIdentTol = 1e-12;

    sealed class CedeEv
    {
        public int Year; public Territory.Rehoming How; public Region Reg = null!; public World? To; public Coefficients Coef = null!;
        public string Sender = "", ToKey = "", TierBefore = "";
        public double SameCore, PopK, AdminFillBefore, DomBefore;
        public double[] RuleBefore = [], RuleInitBefore = [], SharesBefore = [];
        public string[] RulersBefore = [];
    }
    sealed class CedeState { public readonly List<CedeEv> Pending = new(); public readonly List<string> Rows = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, CedeState> CedeStates = new();

    /// <summary>受け手の鍵（Diplomacy.cs の AddForeignShare の呼び口と同じ式を計器が自分で書く）。</summary>
    static string CedeKey(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    static string CedeKeyOf(World w) => w.SelfPower is { } p ? CedeKey(p) : "";

    /// <summary>州が移る時点（<see cref="RegionMoved"/> の最初の文・Rehome の前）：前の値を写して貯める。助走の間と単国の世界は貯めない。</summary>
    static void CedeBefore(World from, World? to, Region r, Territory.Rehoming how)
    {
        if (from.WarmupFrozen || from.Diplomacy is not { } dip) return;
        var st = CedeStates.GetValue(dip, _ => new CedeState());
        st.Pending.Add(new CedeEv
        {
            Year = from.Year, How = how, Reg = r, To = to, Coef = from.Coef,
            Sender = CedeKeyOf(from), ToKey = to is null ? "" : CedeKeyOf(to), TierBefore = r.Tier.ToString(),
            SameCore = to?.SelfPower is { } tp && from.SelfPower is { } fp ? (fp.Culture == tp.Culture ? 1 : 0) : double.NaN,
            PopK = r.SumPopulation(), AdminFillBefore = r.AdminFill, DomBefore = r.Dominance,
            RuleBefore = (double[])r.Rule.Clone(), RuleInitBefore = (double[])r.RuleInit.Clone(),
            SharesBefore = (double[])r.ForeignShares.Clone(), RulersBefore = (string[])r.ForeignRulers.Clone(),
        });
    }

    /// <summary>届き（計器が自分で書く式）＝R0×(d＋(1−d)×AdminFill)＋R1×d・d＝1−PrivilegeTaxDiscount。</summary>
    static double CedeDelivery(double r0, double r1, double adminFill, Coefficients c)
    {
        double d = 1.0 - RuleClamp01(c.PrivilegeTaxDiscount.Value);
        return RuleClamp01(r0) * (d + (1.0 - d) * RuleClamp01(adminFill)) + RuleClamp01(r1) * d;
    }

    /// <summary>外国持分の読み：正の持分の和・受け手の鍵の持分・受け手の鍵を除いた正の持分の和。</summary>
    static (double Sum, double Own, double Third) CedeShares(string[] rulers, double[] shares, string key)
    {
        double sum = 0, own = 0, third = 0;
        for (int k = 0; k < shares.Length; k++)
        {
            double x = shares[k];
            if (!(x > 0)) continue;
            sum += x;
            if (k < rulers.Length && key.Length > 0 && rulers[k] == key) own += x; else third += x;
        }
        return (sum, own, third);
    }

    static double CedeDiffMax(double[] a, double[] b, double dm)
    {
        if (a.Length != b.Length) return double.PositiveInfinity;
        for (int k = 0; k < a.Length; k++) dm = CedeDiff(a[k], b[k], dm);
        return dm;
    }
    static double CedeDiff(double a, double b, double dm)
    {
        if (a.Equals(b)) return dm;
        double x = Math.Abs(a - b);
        return double.IsNaN(x) ? double.PositiveInfinity : x > dm ? x : dm;
    }

    /// <summary>四半期の末（<see cref="Quarter"/> の全ての国の段）：貯めた事象の後の値を読み、行を貯める。</summary>
    static void CedeTable(MultiNationSim sim, int q)
    {
        if (!CedeStates.TryGetValue(sim.Diplomacy, out var st) || st.Pending.Count == 0) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var ev in st.Pending)
        {
            var r = ev.Reg;
            World? own = null;
            foreach (var n in sim.Nations) if (n.World.Regions.Exists(x => ReferenceEquals(x, r))) { own = n.World; break; }
            int seen = 0; foreach (var e2 in st.Pending) if (ReferenceEquals(e2.Reg, r)) seen++;
            bool movedAgain = seen > 1 || (ev.To is not null && !ReferenceEquals(own, ev.To));
            string key = ev.To is not null ? ev.ToKey : own is null ? "" : CedeKeyOf(own);
            var c = (own ?? ev.To)?.Coef ?? ev.Coef;
            double popEnd = r.SumPopulation(), ownerPop = 0;
            if (own is not null) foreach (var rr in own.Regions) ownerPop += rr.SumPopulation();
            double popShare = ownerPop > 0 ? popEnd / ownerPop : double.NaN;
            bool ok4b = ev.RuleBefore.Length == 4, ok4a = r.Rule.Length == 4;
            double R(double[] v, int k) => k < v.Length ? v[k] : double.NaN;
            var (sumB, ownB, thirdB) = CedeShares(ev.RulersBefore, ev.SharesBefore, key);
            var (sumA, ownA, thirdA) = CedeShares(r.ForeignRulers, r.ForeignShares, key);
            double adminAfter = r.AdminFill;
            double delivB = ok4b ? CedeDelivery(ev.RuleBefore[0], ev.RuleBefore[1], ev.AdminFillBefore, c) : double.NaN;
            double delivA = ok4a ? CedeDelivery(r.Rule[0], r.Rule[1], adminAfter, c) : double.NaN;
            double c1Local = ok4b ? ev.RuleBefore[0] + ev.RuleBefore[1] : double.NaN;
            double c1Deliv = ok4b ? CedeDelivery(0.0, c1Local, adminAfter, c) : double.NaN;
            var (rmil, rsize) = RuleCrowdMilitancy(r);
            double mReg = rsize > 1e-9 ? rmil / rsize : double.NaN;
            double ungovOld = double.NaN, ungovNew = double.NaN;
            if (own is not null && ok4a)
            {
                var oc = own.Coef;
                double rNow = RuleRawOrder(own, oc);
                double crowdMil = 0, crowdSize = 0;
                foreach (var rr in own.Regions) { var (cm, cs) = RuleCrowdMilitancy(rr); crowdMil += cm; crowdSize += cs; }
                double mBar = crowdSize > 1e-9 ? crowdMil / crowdSize : double.NaN;
                double orderNew = !(mReg > 0) ? 1.0 : RuleClamp01(rNow * mBar / mReg);
                double reach = Politics.RegionReach(own, oc, r);
                ungovOld = (1.0 - RuleClamp01(own.PublicOrder)) * (1.0 - reach);
                ungovNew = (1.0 - orderNew) * (1.0 - reach);
            }
            bool added = r.ForeignRulers.Length > ev.RulersBefore.Length || sumA > sumB;
            double dm = 0;
            dm = CedeDiffMax(r.Rule, ev.RuleBefore, dm);
            dm = CedeDiffMax(r.RuleInit, ev.RuleInitBefore, dm);
            dm = CedeDiff(r.Dominance, ev.DomBefore, dm);
            dm = CedeDiffMax(r.ForeignShares, ev.SharesBefore, dm);
            bool brk = !(dm <= CedeIdentTol);
            bool picked = own is not null && IsPicked(own, S(own));
            st.Rows.Add(string.Join('\t', I(q), I(ev.Year), ev.How.ToString(), ev.Sender, key, r.Name, ev.TierBefore, r.Tier.ToString(), F(ev.SameCore), F(ev.PopK), F(popShare),
                F(R(ev.RuleBefore, 0)), F(R(ev.RuleBefore, 1)), F(R(ev.RuleBefore, 2)), F(R(ev.RuleBefore, 3)), F(R(ev.RuleInitBefore, 1)), F(ev.DomBefore),
                I(ev.RulersBefore.Length), F(sumB), F(ownB), F(thirdB), F(ev.AdminFillBefore), F(delivB), F(ok4b ? ev.RuleBefore[0] : double.NaN),
                F(ok4b ? 0.0 : double.NaN), F(c1Local), F(c1Deliv), F(ok4b ? 0.0 : double.NaN), F(0.0),
                F(R(r.Rule, 0)), F(R(r.Rule, 1)), F(R(r.Rule, 2)), F(R(r.Rule, 3)), F(R(r.RuleInit, 1)), F(r.Dominance),
                I(r.ForeignRulers.Length), F(sumA), F(ownA), F(thirdA), F(adminAfter), F(delivA), F(ok4a ? r.Rule[0] : double.NaN),
                own is null ? "" : CedeKeyOf(own), F(mReg), F(ungovOld), F(ungovNew),
                movedAgain ? "1" : "0", added ? "1" : "0", picked ? "1" : "0", F(dm), brk ? "1" : "0"));
        }
        st.Pending.Clear();
    }

    /// <summary>番人（テスト）が溜まった行を読む口（Close の前）。</summary>
    internal static List<string> CedeRowsPending(Diplomacy dip) => CedeStates.GetValue(dip, _ => new CedeState()).Rows;

    /// <summary>試合の終わり：cederule_s を書く（行が無くても見出しは書く＝読み手が空の表を読める・見出しと行の書き先はここの 1 本）。</summary>
    static void CedeClose(MultiNationSim sim, ulong seed)
    {
        var st = CedeStates.GetValue(sim.Diplomacy, _ => new CedeState());
        using var wr = Open("cederule", seed);
        wr.WriteLine(CedeHeaderText);
        foreach (var row in st.Rows) wr.WriteLine(row);
        st.Rows.Clear(); st.Pending.Clear();
    }
}
