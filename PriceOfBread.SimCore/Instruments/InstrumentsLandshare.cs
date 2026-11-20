using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。〔履歴 InstrumentsLandshare.cs#1〕
public static partial class Instruments
{
    const int LsNC = 5;
    static readonly string[] LsClassNames = ["Noble", "Clergy", "Peasant", "Capitalist", "Other"];

    /// <summary>階級の 5 区分の添字（純関数）：Noble 0・Clergy 1・Peasant 2・Capitalist 3・その他 4。</summary>
    internal static int LsClass(int classId) => classId == PopClass.Noble ? 0 : classId == PopClass.Clergy ? 1 : classId == PopClass.Peasant ? 2 : classId == PopClass.Capitalist ? 3 : 4;

    /// <summary>枠 k の受け手の POP の添字（計器の写し・純関数）。範囲の外は持ち主へ寄せ、寄せた回数を <paramref name="fallback"/> に足す。</summary>
    internal static int LsRecipient(Region r, Producer s, int k, ref long fallback)
    {
        int i = k == 0 ? s.OwnerPopIndex : k == 1 ? r.ClergyPopIndex : k == 2 ? s.LaborPopIndex : k == 3 ? r.InvestorPopIndex : -1;
        if (i >= 0 && i < r.Pops.Count) return i;
        fallback++;
        return s.OwnerPopIndex;
    }

    /// <summary>
    /// 〔履歴 InstrumentsLandshare.cs#2〕
    /// </summary>
    internal static double[]? LsNormalize(double[]? lo)
    {
        if (lo is null || (lo.Length != 4 && lo.Length != 5)) return null;
        double sum = 0;
        for (int k = 0; k < lo.Length; k++) { double v = lo[k]; if (!(v >= 0) || double.IsInfinity(v)) return null; sum += v; }
        if (!(sum > 0) || double.IsInfinity(sum)) return null;
        var o = new double[lo.Length];
        for (int k = 0; k < lo.Length; k++) o[k] = lo[k] / sum;
        return o.Length == 5 && !(1.0 - o[4] > 0) ? null : o;
    }

    /// <summary>
    /// 〔履歴 InstrumentsLandshare.cs#3〕
    /// </summary>
    internal static int LsSlots(World w, double[] ls) => w.Coef.LandShareCrownSlot > 0 && ls.Length == 5 ? 4 : ls.Length;
    internal static double LsW(World w, double[] ls, int k) => w.Coef.LandShareCrownSlot > 0 && ls.Length == 5 ? ls[k] / (1.0 - ls[4]) : ls[k];

    /// <summary>
    /// 〔履歴 InstrumentsLandshare.cs#4〕
    /// </summary>
    static readonly int[] LsMineralIds = [Catalog.IronOreId, Catalog.CoalId, Catalog.CopperOreId, Catalog.LeadOreId, Catalog.GoldOreId, Catalog.SulfurId, Catalog.LimestoneId, Catalog.SilicaSandId, Catalog.ClayId, Catalog.CrudeOilId, Catalog.ChromeId, Catalog.TinOreId, Catalog.SaltpeterId, Catalog.SilverId];   // ★GoodIdGuardTests＝数値の財 id を書かない（定数の名で同じ 14 財）
    internal static bool LsMineral(int id) => System.Array.IndexOf(LsMineralIds, id) >= 0;

    /// <summary>
    /// 〔履歴 InstrumentsLandshare.cs#5〕
    /// </summary>
    static double LsBuiltShare(Producer s) => s.BuiltScale > 1e-9 ? System.Math.Min(1.0, s.BuiltScale / System.Math.Max(s.Scale, 1e-9)) : 0.0;

    /// <summary>月の相対の差（純関数）＝Σ_c|a_c−b_c|÷Σ_c b_c。分母が正でなければ差 0 で 0・差が正なら +∞。</summary>
    internal static double LsRelDiff(double[] a, double[] b)
    {
        double d = 0, tot = 0;
        for (int c = 0; c < a.Length; c++) { d += System.Math.Abs(a[c] - b[c]); tot += b[c]; }
        return tot > 0 ? d / tot : d > 0 ? double.PositiveInfinity : d == 0 ? 0.0 : double.NaN;
    }

    static bool LsEndow(Region r, Producer s) => System.Array.IndexOf(r.Resources, s.GoodId) >= 0;

    /// <summary>不在地主の RGO か（計器の写し）：植民地格・RGO・主食でない（Tier が Staple でも GrainChain でもない）・州の Resources に載る。</summary>
    static bool LsAbsenteeRgo(Region r, Producer s)
        => r.Tier == RegionTier.Colonial && s.IsRgo && s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length
           && !(Catalog.Goods[s.GoodId].Tier == GoodTier.Staple || Catalog.Goods[s.GoodId].GrainChain) && System.Array.IndexOf(r.Resources, s.GoodId) >= 0;

    /// <summary>不在地主（計器の写し）＝国の首都の州の投資家 POP の添字。首都そのものの州・投資家の居ない国は −1。</summary>
    static int LsAbsenteeOwner(World w, Region r)
    {
        var cap = w.Region;
        return ReferenceEquals(cap, r) || cap.InvestorPopIndex < 0 || cap.InvestorPopIndex >= cap.Pops.Count ? -1 : cap.InvestorPopIndex;
    }

    /// <summary>試験だけが登録する data の側の値（国の World ごと・登録が無ければ world.json の同じ名の国）。</summary>
    internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> LsDataOverride = new();

    static double[]? LsDataFor(World w)
    {
        if (LsDataOverride.TryGetValue(w, out var o)) return LsNormalize(o);
        string name = NameOf(w);
        foreach (var c in WorldData.Nations()) if (c.Name == name) return LsNormalize(c.LandOwnership);
        return null;
    }

    sealed class LsReg
    {
        public readonly double[] Exp = new double[LsNC], Dat = new double[LsNC];
        public double[]? Pre;   // 植民地格の州（不在地主の居る州）：POP ごとのその月の LandRent の積み
        public bool Touched;
    }

    sealed class LsAcc
    {
        public readonly Dictionary<Region, LsReg> Regs = new();
        public bool DataReady; public double[]? Data;
        // 月の受け口の積み（月の閉じで四半期へ足して 0 へ戻す）
        public double MNonEndow, MEndow, MColonial, MExp;
        public readonly double[] MByOwner = new double[LsNC], MExpSlot = new double[LsNC];
        // 四半期の和
        public double QNonEndow, QEndow, QColonial, QExp, QRemit;
        public readonly double[] QByOwner = new double[LsNC], QExpected = new double[LsNC], QData = new double[LsNC], QActual = new double[LsNC],
            QActualF = new double[LsNC], QExpectedF = new double[LsNC], QExpSlot = new double[LsNC];
        public double MaxSlot, MaxData; public long Fallback; public int Months;
        // MaxSlotTierRule〔履歴 InstrumentsLandshare.cs#6〕
        public double QRemitNotAdded, MaxSlotTierRule; public int CapColonialMonths;
        // QExpNeNobleMin〔履歴 InstrumentsLandshare.cs#7〕
        public double QMinNonEndow, QMinEndow, QExpMin, QExpNeNoble, QExpNeNobleMin;
        public readonly double[] QMinByOwner = new double[LsNC];
        // MCrownXAct〔履歴 InstrumentsLandshare.cs#8〕
        public double MCrownExp, MCrownAct, MCrownXExp, MCrownXAct;
        public double QCrownExp, QCrownAct, QCrownXExp, QCrownXAct, QCrownAll, QCrownDef, MaxCrown, MaxCrownX;
        public long CrownProp, CrownNatSkip;
    }

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, LsAcc> LsAccs = new();

    static LsReg LsRegOf(LsAcc a, Region r)
    {
        if (!a.Regs.TryGetValue(r, out var g)) { g = new LsReg(); a.Regs[r] = g; }
        g.Touched = true;
        return g;
    }

    static void LsAdd(double[] dst, double[] src) { for (int c = 0; c < dst.Length; c++) dst[c] += src[c]; }

    static void LsMax(ref double max, double v) { if (double.IsNaN(max)) return; if (double.IsNaN(v) || v > max) max = v; }

    /// <summary>受け口（EconomyStep の④・RGO だけ・INS のときだけ）：国営持分と赤字の枝の後・割る前の地代の総額 <paramref name="rent"/>。</summary>
    internal static void LandshareRgo(World w, Region r, Producer s, double rent, double sf0)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        // WarmupFrozen〔履歴 InstrumentsLandshare.cs#9〕
        if (!w.WarmupFrozen && s.IsRgo)
        {
            var a0 = LsAccs.GetValue(w, _ => new LsAcc());
            if (!a0.DataReady) { a0.Data = LsDataFor(w); a0.DataReady = true; }
            double stA = sf0 - r.StateSpent[(int)TreasuryCat.StateFactory];
            if (s.LandShares is { Length: 5 } && stA < 0) { a0.QCrownDef += -stA; a0.CrownProp++; }
            if (r.NationalizedMask != 0) a0.CrownNatSkip++;
            else if (stA >= 0 && rent > 0)
            {
                double fb = LsBuiltShare(s), rp = rent + stA, act = stA - fb * rp;
                a0.QCrownAll += act;
                if (r.Tier != RegionTier.Colonial && !LsEndow(r, s) && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble
                    && a0.Data is { Length: 5 } d5)
                { a0.MCrownExp += rp * (1.0 - fb) * d5[4]; a0.MCrownAct += act; }
            }
        }
        if (!(rent > 0)) return;   // LandRent が立つのは rent>0 の分だけ
        var a = LsAccs.GetValue(w, _ => new LsAcc());
        if (!a.DataReady) { a.Data = LsDataFor(w); a.DataReady = true; }
        int np = r.Pops.Count;
        if (s.OwnerPopIndex < 0 || s.OwnerPopIndex >= np) return;
        var g = LsRegOf(a, r);
        var ls = s.LandShares;
        if (r.Tier == RegionTier.Colonial)
        {
            a.MColonial += rent;
            if (LsAbsenteeOwner(w, r) >= 0)
            {
                g.Pre ??= new double[np];
                if (g.Pre.Length < np) System.Array.Resize(ref g.Pre, np);
                if (ls is null) g.Pre[s.OwnerPopIndex] += rent;
                else for (int k = 0, nk = LsSlots(w, ls); k < nk; k++) if (ls[k] > 0) g.Pre[LsRecipient(r, s, k, ref a.Fallback)] += rent * LsW(w, ls, k);
            }
            return;
        }
        bool endow = LsEndow(r, s);
        int ownerCls = LsClass(r.Pops[s.OwnerPopIndex].ClassId);
        if (endow) a.MEndow += rent; else { a.MNonEndow += rent; a.MByOwner[ownerCls] += rent; }
        if (!w.WarmupFrozen && LsMineral(s.GoodId))   // WarmupFrozen〔履歴 InstrumentsLandshare.cs#10〕
        {
            if (endow) a.QMinEndow += rent; else { a.QMinNonEndow += rent; a.QMinByOwner[ownerCls] += rent; }
        }
        if (ls is null) g.Exp[ownerCls] += rent;
        else for (int k = 0, nk = LsSlots(w, ls); k < nk; k++) if (ls[k] > 0) g.Exp[LsClass(r.Pops[LsRecipient(r, s, k, ref a.Fallback)].ClassId)] += rent * LsW(w, ls, k);
        var d = a.Data;
        if (d is not null && !endow && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble)
        {
            long dummy = 0;
            for (int k = 0, nk = LsSlots(w, d); k < nk; k++) if (d[k] > 0) g.Dat[LsClass(r.Pops[LsRecipient(r, s, k, ref dummy)].ClassId)] += rent * LsW(w, d, k);
        }
        else g.Dat[ownerCls] += rent;
    }

    /// <summary>
    /// 受け口（InternationalMarket.PaySales・INS のときだけ）：国営持分を引いた後の正の輸出の地代 rent。〔履歴 InstrumentsLandshare.cs#11〕
    /// </summary>
    internal static void LandshareExp(World w, Region r, Producer s, double rent, double sc)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!(rent > 0)) return;
        var a = LsAccs.GetValue(w, _ => new LsAcc());
        a.MExp += rent;
        if (!w.WarmupFrozen)   // WarmupFrozen〔履歴 InstrumentsLandshare.cs#12〕
        {
            if (!a.DataReady) { a.Data = LsDataFor(w); a.DataReady = true; }
            bool min = LsMineral(s.GoodId);
            if (min) a.QExpMin += rent;
            bool neNoble = r.Tier != RegionTier.Colonial && !LsEndow(r, s) && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble;
            if (neNoble) { a.QExpNeNoble += rent; if (min) a.QExpNeNobleMin += rent; }
            if (neNoble && r.NationalizedMask == 0 && a.Data is { Length: 5 } d5)
            {
                double fb = LsBuiltShare(s), rp = rent + sc;
                a.MCrownXExp += rp * (1.0 - fb) * d5[4]; a.MCrownXAct += sc - fb * rp;
            }
        }
        int ab = LsAbsenteeRgo(r, s) ? LsAbsenteeOwner(w, r) : -1;
        if (ab >= 0) { a.MExpSlot[LsClass(w.Region.Pops[ab].ClassId)] += rent; return; }
        if (s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count) return;
        var ls = s.LandShares;
        if (ls is null) a.MExpSlot[LsClass(r.Pops[s.OwnerPopIndex].ClassId)] += rent;
        else for (int k = 0, nk = LsSlots(w, ls); k < nk; k++) if (ls[k] > 0) a.MExpSlot[LsClass(r.Pops[LsRecipient(r, s, k, ref a.Fallback)].ClassId)] += rent * LsW(w, ls, k);
    }

    /// <summary>月の閉じ（Instruments.MonthClose の頭・その月の全ての州の EconomyStep と不在地主の送金の後）：期待と data と実際を比べ、四半期の和へ足す。</summary>
    internal static void LandshareMonth(World w)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!LsAccs.TryGetValue(w, out var a)) return;
        if (!w.WarmupFrozen)
        {
            var exp = new double[LsNC]; var dat = new double[LsNC]; var act = new double[LsNC]; var expF = new double[LsNC]; var actF = new double[LsNC];
            double remit = 0;
            bool capField = false;
            foreach (var r in w.Regions)
            {
                a.Regs.TryGetValue(r, out var g);
                if (r.Tier == RegionTier.Colonial)
                {
                    if (g?.Pre is { } pre)
                        for (int k = 0; k < pre.Length && k < r.Pops.Count; k++) remit += System.Math.Max(0, pre[k] - r.Pops[k].LandRentPerMonth.Value);
                    continue;
                }
                bool field = false;
                foreach (var s in r.Producers) if (s.LandShares is not null) { field = true; break; }
                if (ReferenceEquals(r, w.Region)) capField = field;
                var actR = new double[LsNC];
                foreach (var p in r.Pops) actR[LsClass(p.ClassId)] += p.LandRentPerMonth.Value;
                LsAdd(act, actR);
                if (g is not null) { LsAdd(exp, g.Exp); LsAdd(dat, g.Dat); }
                if (field) { LsAdd(actF, actR); if (g is not null) LsAdd(expF, g.Exp); }
            }
            var expTier = (double[])exp.Clone();   // expTier〔履歴 InstrumentsLandshare.cs#13〕
            var capR = w.Region;
            bool capColonial = capR.Tier == RegionTier.Colonial;
            if (capColonial) a.CapColonialMonths++;
            if (remit > 0)
            {
                var cap = w.Region;
                bool capIn = cap.InvestorPopIndex >= 0 && cap.InvestorPopIndex < cap.Pops.Count;   // capIn〔履歴 InstrumentsLandshare.cs#14〕
                if (capIn)
                {
                    int ci = LsClass(cap.Pops[cap.InvestorPopIndex].ClassId);
                    exp[ci] += remit; dat[ci] += remit;
                    if (capField) expF[ci] += remit;
                    if (!capColonial) expTier[LsClass(cap.Pops[cap.InvestorPopIndex].ClassId)] += remit;
                }
                if (capColonial || !capIn) a.QRemitNotAdded += remit;
            }
            LsMax(ref a.MaxSlotTierRule, LsRelDiff(expTier, act));
            LsMax(ref a.MaxSlot, LsRelDiff(exp, act));
            LsMax(ref a.MaxData, LsRelDiff(dat, exp));
            a.QNonEndow += a.MNonEndow; a.QEndow += a.MEndow; a.QColonial += a.MColonial; a.QExp += a.MExp; a.QRemit += remit;
            LsAdd(a.QByOwner, a.MByOwner); LsAdd(a.QExpected, exp); LsAdd(a.QData, dat); LsAdd(a.QActual, act); LsAdd(a.QActualF, actF); LsAdd(a.QExpectedF, expF); LsAdd(a.QExpSlot, a.MExpSlot);
            a.Months++;
            // CrRel〔履歴 InstrumentsLandshare.cs#15〕
            static double CrRel(double act, double exp) => exp > 0 ? System.Math.Abs(act - exp) / exp : act == 0 ? 0.0 : double.IsNaN(act) ? double.NaN : double.PositiveInfinity;
            LsMax(ref a.MaxCrown, CrRel(a.MCrownAct, a.MCrownExp)); LsMax(ref a.MaxCrownX, CrRel(a.MCrownXAct, a.MCrownXExp));
            a.QCrownExp += a.MCrownExp; a.QCrownAct += a.MCrownAct; a.QCrownXExp += a.MCrownXExp; a.QCrownXAct += a.MCrownXAct;
        }
        a.MCrownExp = a.MCrownAct = a.MCrownXExp = a.MCrownXAct = 0;
        a.MNonEndow = a.MEndow = a.MColonial = a.MExp = 0;
        System.Array.Clear(a.MByOwner); System.Array.Clear(a.MExpSlot);
        foreach (var g in a.Regs.Values)
        {
            if (!g.Touched) continue;
            System.Array.Clear(g.Exp); System.Array.Clear(g.Dat); if (g.Pre is not null) System.Array.Clear(g.Pre);
            g.Touched = false;
        }
    }

    sealed class LandshareSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LandshareSink> LandshareSinks = new();

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の行を書き、四半期の和を 0 へ戻す。</summary>
    static void LandshareTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        var sk = LandshareSinks.GetValue(sim, _ => new LandshareSink());
        sk.W ??= Open("landshare", seed);
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var a = LsAccs.GetValue(w, _ => new LsAcc());
            long nNull = 0, nField = 0, nEndow = 0, nFieldCol = 0, nFieldEndow = 0, nFieldReg = 0;
            double sumDev = 0;
            foreach (var r in w.Regions)
            {
                bool field = false;
                foreach (var s in r.Producers)
                {
                    if (!s.IsRgo) continue;
                    bool endow = LsEndow(r, s);
                    if (endow) nEndow++;
                    if (s.LandShares is not { } ls) { nNull++; continue; }
                    nField++; field = true;
                    if (r.Tier == RegionTier.Colonial) nFieldCol++;
                    if (endow) nFieldEndow++;
                    double sum = 0; foreach (var v in ls) sum += v;
                    double dev = double.IsFinite(sum) ? System.Math.Abs(sum - 1.0) : 1.0;
                    if (dev > sumDev) sumDev = dev;
                }
                if (field) nFieldReg++;
            }
            var cols = new List<(string Name, string Val)>(64)
            {
                ("quarter", I(q)), ("year", I(w.Year)), ("nation", NameOf(w)), ("monthsInQuarter", I(a.Months)),
                ("rgoNullCount", I(nNull)), ("rgoFieldCount", I(nField)), ("rgoEndowCount", I(nEndow)), ("rgoFieldColonialCount", I(nFieldCol)), ("rgoFieldEndowCount", I(nFieldEndow)), ("fieldRegionCount", I(nFieldReg)),
                ("rentNonEndowPerQuarter", F(a.QNonEndow)), ("rentEndowPerQuarter", F(a.QEndow)), ("rentColonialPerQuarter", F(a.QColonial)),
            };
            void Add5(string pre, double[] v, string post) { for (int c = 0; c < LsNC; c++) cols.Add((pre + LsClassNames[c] + post, F(v[c]))); }
            Add5("rentNonEndowByOwnerClass", a.QByOwner, "PerQuarter");
            Add5("expectedByClass", a.QExpected, "PerQuarter");
            Add5("dataByClass", a.QData, "PerQuarter");
            Add5("landRentActual", a.QActual, "PerQuarter");
            Add5("landRentActualFieldRegion", a.QActualF, "PerQuarter");
            Add5("expectedFieldRegionByClass", a.QExpectedF, "PerQuarter");
            cols.Add(("colonialRemitPerQuarter", F(a.QRemit)));
            cols.Add(("maxRelDiffSlotVsActual", F(a.MaxSlot))); cols.Add(("maxRelDiffDataVsField", F(a.MaxData)));
            cols.Add(("fieldSumMaxAbsDev", F(sumDev))); cols.Add(("slotFallbackCount", I(a.Fallback)));
            cols.Add(("expRentPerQuarter", F(a.QExp)));
            Add5("expSlotExpected", a.QExpSlot, "PerQuarter");
            // Add〔履歴 InstrumentsLandshare.cs#16〕
            cols.Add(("capColonialMonths", I(a.CapColonialMonths))); cols.Add(("remitNotAddedPerQuarter", F(a.QRemitNotAdded))); cols.Add(("maxRelDiffSlotVsActualTierRule", F(a.MaxSlotTierRule)));
            // Add〔履歴 InstrumentsLandshare.cs#17〕
            cols.Add(("rentNonEndowMineralPerQuarter", F(a.QMinNonEndow)));
            Add5("rentNonEndowMineralByOwnerClass", a.QMinByOwner, "PerQuarter");
            cols.Add(("rentEndowMineralPerQuarter", F(a.QMinEndow))); cols.Add(("expRentMineralPerQuarter", F(a.QExpMin)));
            cols.Add(("expRentNonEndowNoblePerQuarter", F(a.QExpNeNoble))); cols.Add(("expRentNonEndowNobleMineralPerQuarter", F(a.QExpNeNobleMin)));
            cols.Add(("crownExpectedPerQuarter", F(a.QCrownExp))); cols.Add(("crownActualPerQuarter", F(a.QCrownAct))); cols.Add(("crownMaxRelDiff", F(a.MaxCrown)));
            cols.Add(("crownExpExpectedPerQuarter", F(a.QCrownXExp))); cols.Add(("crownExpActualPerQuarter", F(a.QCrownXAct))); cols.Add(("crownExpMaxRelDiff", F(a.MaxCrownX)));
            cols.Add(("crownAllActualPerQuarter", F(a.QCrownAll))); cols.Add(("crownDeficitStatePerQuarter", F(a.QCrownDef)));
            cols.Add(("crownProppedRgoMonths", I(a.CrownProp))); cols.Add(("crownNationalizedSkipCount", I(a.CrownNatSkip)));
            if (!sk.Header) { sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Name))); sk.Header = true; }
            sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Val)));
            a.QNonEndow = a.QEndow = a.QColonial = a.QExp = a.QRemit = 0;
            System.Array.Clear(a.QByOwner); System.Array.Clear(a.QExpected); System.Array.Clear(a.QData); System.Array.Clear(a.QActual);
            System.Array.Clear(a.QActualF); System.Array.Clear(a.QExpectedF); System.Array.Clear(a.QExpSlot);
            a.QRemitNotAdded = 0; a.MaxSlotTierRule = 0; a.CapColonialMonths = 0;
            a.MaxSlot = a.MaxData = 0; a.Fallback = 0; a.Months = 0; a.DataReady = false;
            a.QMinNonEndow = a.QMinEndow = a.QExpMin = a.QExpNeNoble = a.QExpNeNobleMin = 0; System.Array.Clear(a.QMinByOwner);
            a.QCrownExp = a.QCrownAct = a.QCrownXExp = a.QCrownXAct = a.QCrownAll = a.QCrownDef = a.MaxCrown = a.MaxCrownX = 0; a.CrownProp = a.CrownNatSkip = 0;
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void LandshareClose(MultiNationSim sim)
    {
        if (!Tg.Basisx) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (LandshareSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
