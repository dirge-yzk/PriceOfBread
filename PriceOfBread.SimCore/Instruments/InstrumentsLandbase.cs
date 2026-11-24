using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口と遅れの列の履歴は ConditionalWeakTable だけ＝セーブに載らない）。〔履歴 InstrumentsLandbase.cs#1〕
public static partial class Instruments
{
    internal const string LandbaseHeaderText = "quarter\tyear\tnation\tprivExemptDegreeShare\t"
        + "landClaimPerMonth\tlandClaimExemptPerMonth\tlandTaxPaidPerMonth\tlandPaidOverClaim\t"
        + "landClaimExtShareUpperPerMonth\tlandClaimExtGrossUpperPerMonth\t"
        + "baseRPerMonth\tbaseRCheckRelDiff\tbaseRCheckBreakFlag\tbaseLag4Ratio\tbaseOverFirstSeenRatio\t"
        + "fpCarrierPops\tfpShortPops\tfpShortSizeK\tfpShortfallGoodsPerMonth\tfpShortNoPricePops\t"
        + "fpShortPopsSizeK1\tfpShortPopsSizeK10\tfpShortPopsSizeK100\tfpShortPopsSizeK1000\tfpShortPopsSizeKOver";

    internal const string LandbaseRegHeaderText = "quarter\tyear\tnation\tregion\ttier\t"
        + "regionBasePerMonth\tregionPayersWithRent\tregionStapleRentShare\tregionSubValuePerMonth\tregionClaimPerMonth\t"
        + "regionBaseLag4Ratio\tregionZeroBaseFlag\tregionRgoCount\tregionRgoOwnerEmpty\tregionAbsenteeRgoCount";

    /// <summary>計器の名乗りの定数（係数ではない）：遅れの列の四半期の数（段 A-2 の案 X の〈4 四半期前の基数〉）。</summary>
    internal const int LandbaseLagQuarters = 4;
    /// <summary>計器の名乗りの定数（係数ではない）：R の 2 通りの数え方の照合の許し（相対・double）。</summary>
    internal const double LandbaseIdentTol = 1e-12;

    sealed class LandbaseSink { public System.IO.StreamWriter? W, Reg, Fix; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LandbaseSink> LandbaseSinks = new();

    /// <summary>遅れの列の履歴（計器の持ち物）：q を添字にした輪（長さ＝遅れ＋1）と最初に見た値。</summary>
    sealed class LbHist
    {
        public readonly int[] Q = [-1, -1, -1, -1, -1, -1, -1, -1];
        public readonly double[] V = new double[8];
        public bool Seen; public double First;
        /// <summary>今の値を入れ、q−lag の値を返す（無ければ nan）。</summary>
        public double Push(int q, double v, int lag)
        {
            int n = Q.Length;
            double before = double.NaN;
            int qb = q - lag;
            if (qb >= 0) { int k = qb % n; if (Q[k] == qb) before = V[k]; }
            int i = q % n; Q[i] = q; V[i] = v;
            if (!Seen) { Seen = true; First = v; }
            return before;
        }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, LbHist> LbNationHist = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, LbHist> LbRegionHist = new();

    /// <summary>国の R（純関数）＝州→POP の順の Σ max(0, LandRentPerMonth)（犯罪者を含む）。</summary>
    internal static double LbR(World w)
    {
        double s = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) s += System.Math.Max(0, p.LandRentPerMonth.Value);
        return s;
    }

    /// <summary>照合の旗（純関数）：相対差が <see cref="LandbaseIdentTol"/> 以下でなければ 1（否定形＝NaN も 1）。</summary>
    internal static int LbCheckFlag(double a, double b) => LbRelDiff(a, b) <= LandbaseIdentTol ? 0 : 1;

    static double LbRelDiff(double a, double b) => System.Math.Abs(a - b) / System.Math.Max(System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)), 1e-300);

    /// <summary>主食の値段（subst_s の stapleNeedWeightedPrice と同じ式＝InstrumentsSubst の stapleP を出す段）：主食の BaseNeed で重みを付けた州の値段。重みが 0 なら nan。</summary>
    static double LbStaplePrice(Region r)
    {
        double pw = 0, pn = 0; int ng = System.Math.Min(r.Markets.Length, Catalog.Goods.Length);
        for (int g = 0; g < ng; g++)
            if (Catalog.Goods[g].Tier == GoodTier.Staple) { pw += Catalog.Goods[g].BaseNeed * r.Markets[g].Price; pn += Catalog.Goods[g].BaseNeed; }
        return pn > 0 ? pw / pn : double.NaN;
    }

    /// <summary>州の主食の RGO（IsRgo・主食・倒産でない・Output＞0）の Output で重みを付けた min(RentShare, FarmRentCap)。無ければ nan。</summary>
    static double LbStapleRentShare(Region r)
    {
        double w = 0, ws = 0;
        foreach (var s in r.Producers)
        {
            if (!s.IsRgo || s.Bankrupt || s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length || Catalog.Goods[s.GoodId].Tier != GoodTier.Staple || !(s.Output > 0)) continue;
            w += s.Output; ws += s.Output * System.Math.Min(s.RentShare.Value, r.FarmRentCap.Value);
        }
        return w > 0 ? ws / w : double.NaN;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の行（landbase_s）と、計器の国の名簿の国の州の行（landbasereg_s）を書く。見出しは開いた時に書く（行 0 の種でもファイルができる）。</summary>
    static void LandbaseTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = LandbaseSinks.GetValue(sim, _ => new LandbaseSink());
        if (sk.W is null) { sk.W = Open("landbase", seed); sk.W.WriteLine(LandbaseHeaderText); }
        if (sk.Reg is null) { sk.Reg = Open("landbasereg", seed); sk.Reg.WriteLine(LandbaseRegHeaderText); }
        if (sk.Fix is null) { sk.Fix = Open("landfix", seed); sk.Fix.WriteLine(LandfixHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;   // cornint_s と同じ行の集合
            var c = w.Coef;
            string nation = NameOf(w);
            bool picked = IsPicked(w, S(w));
            // 身分制免税の度合い d′（Privileges.TaxExemptionDegree を計器が書く）
            double disc = c.PrivilegeTaxDiscount.Value;
            double deg = !(disc > 0) ? 0.0
                : c.PrivilegeFromCrownShare.Value > 0 ? double.NaN
                : (w.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate ? disc : 0.0);
            double landRate = w.LandTax, pollLevel = w.PollTax, taxRate = w.TaxRate.Value;
            // 十分の一税の率′（Privileges.TitheDegree の度合いを計器が書く）と聖職の有無（Fiscal.Step の clergy と同じ判定）
            double titheDeg = !(c.TitheRate.Value > 0) ? 0.0
                : w.StateReligion switch { StateReligionLaw.InnerFreedom => c.InnerFreedomTithe, StateReligionLaw.ReligiousFreedom => c.ReligiousFreedomTithe, StateReligionLaw.StateAtheism => 0.0, _ => 1.0 };
            double titheRate = c.TitheRate.Value * titheDeg;
            bool tithe = false;
            if (titheRate > 1e-9)
            {
                double clergySize = 0;
                foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Clergy && p.Size > 1e-9) clergySize += p.Size;
                tithe = clergySize > 1e-9;
            }
            var cap = w.Region;
            bool capInvestor = cap.InvestorPopIndex >= 0 && cap.InvestorPopIndex < cap.Pops.Count;
            double claim = 0, exempt = 0, extShare = 0, extGross = 0, shortSize = 0, shortGoods = 0;
            var rBloc = new double[3];
            int carriers = 0, shortPops = 0, noPrice = 0, k1 = 0, k10 = 0, k100 = 0, k1000 = 0, kOver = 0, carriersRent = 0, shortRent = 0;
            foreach (var r in w.Regions)
            {
                double stapleP = LbStaplePrice(r);
                double rs = LbStapleRentShare(r);
                double v = r.LastSubsistenceOutput * stapleP;
                double regBase = 0, regClaim = 0; int payers = 0;
                foreach (var p in r.Pops)
                {
                    double rentRaw = p.LandRentPerMonth.Value;
                    double rent = System.Math.Max(0, rentRaw);
                    rBloc[p.Closed ? 0 : p.IsModernElite ? 1 : 2] += rent; regBase += rent;
                    if (rentRaw > 0) payers++;
                    if (p.IsCriminal) continue;
                    double privBase = p.Closed && !p.IsSoldier ? 1.0 - deg : 1.0;
                    if (landRate > 1e-9 && rentRaw > 0)
                    {
                        double k = landRate * rentRaw * privBase;
                        claim += k; regClaim += k; exempt += landRate * rentRaw * (1.0 - privBase);
                    }
                    if (!(p.SubsistenceBasis > 0) || !(p.Size > 1e-12)) continue;
                    carriers++; if (rentRaw > 0) carriersRent++;
                    double cl = 0;
                    if (pollLevel > 1e-9 && p.IsCommoner) cl += c.PollTaxRate * pollLevel * p.Size;
                    cl += taxRate * System.Math.Max(0, p.LastEarnedPerMonth.Value) * privBase;
                    if (tithe && p.IsCommoner && !p.IsModernElite && p.LastEarnedPerMonth.Value > 0) cl += titheRate * p.LastEarnedPerMonth.Value;
                    double held = System.Math.Max(0, p.Savings.Value);
                    if (!(cl > held)) continue;
                    shortPops++; shortSize += p.Size; if (rentRaw > 0) shortRent++;
                    if (stapleP > 0 && double.IsFinite(stapleP)) shortGoods += (cl - held) / stapleP; else noPrice++;
                    if (p.Size <= 1) k1++; else if (p.Size <= 10) k10++; else if (p.Size <= 100) k100++; else if (p.Size <= 1000) k1000++; else kOver++;
                }
                if (landRate > 1e-9 && double.IsFinite(v))
                {
                    extGross += landRate * v;
                    if (double.IsFinite(rs)) extShare += landRate * rs * v;
                }
                var rh = LbRegionHist.GetValue(r, _ => new LbHist());
                double regBefore = rh.Push(q, regBase, LandbaseLagQuarters);
                if (!picked) continue;
                int rgo = 0, empty = 0, absentee = 0;
                bool absenteeRegion = r.Tier == RegionTier.Colonial && !ReferenceEquals(cap, r) && capInvestor;
                foreach (var s in r.Producers)
                {
                    if (!s.IsRgo) continue;
                    rgo++;
                    if (!(s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && r.Pops[s.OwnerPopIndex].Size > 1e-12)) empty++;
                    if (absenteeRegion && s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length
                        && !(Catalog.Goods[s.GoodId].Tier == GoodTier.Staple || Catalog.Goods[s.GoodId].GrainChain)
                        && System.Array.IndexOf(r.Resources, s.GoodId) >= 0) absentee++;
                }
                sk.Reg.WriteLine(string.Join('\t',
                    I(q), I(w.Year), nation, r.Name, r.Tier.ToString(),
                    F(regBase), I(payers), F(rs), F(v), F(regClaim),
                    F(regBase > 0 ? regBefore / regBase : double.NaN), I(regBase <= 0 && regBefore > 0 ? 1 : 0),
                    I(rgo), I(empty), I(absentee)));
            }
            double rSimple = LbR(w);   // 1 本の和（州→POP の順）
            double rB = rBloc[0] + rBloc[1] + rBloc[2];   // cornint_s と同じく 3 つのブロックに分けて足してから 3 本を足す
            var nh = LbNationHist.GetValue(w, _ => new LbHist());
            double before = nh.Push(q, rSimple, LandbaseLagQuarters);
            double paid = w.LastTaxLandPerMonth.Value;
            sk.W.WriteLine(string.Join('\t',
                I(q), I(w.Year), nation, F(deg),
                F(claim), F(exempt), F(paid), F(claim > 0 ? paid / claim : double.NaN),
                F(extShare), F(extGross),
                F(rSimple), F(LbRelDiff(rSimple, rB)), I(LbCheckFlag(rSimple, rB)), F(rSimple > 0 ? before / rSimple : double.NaN), F(rSimple > 0 ? nh.First / rSimple : double.NaN),
                I(carriers), I(shortPops), F(shortSize), F(shortGoods), I(noPrice),
                I(k1), I(k10), I(k100), I(k1000), I(kOver)));
            sk.Fix.WriteLine(string.Join('\t', I(q), I(w.Year), nation, LandbaseA2Cells(w, carriersRent, shortRent)));   // WriteLine〔履歴 InstrumentsLandbase.cs#2〕
        }
    }

    // 請求の写しは Fiscal.cs の AssessPop の土地税の分岐（landRate＞1e-9 かつ 基数＞0）を計器が自分で書く。〔履歴 InstrumentsLandbase.cs#3〕
    internal const string LandfixHeaderText = "quarter\tyear\tnation\tlandClaimAtTaxPerMonth\tlandClaimExemptAtTaxPerMonth\tbaseRAtTaxPerMonth\tprivDegreeAtTax\tlandPaidOverClaimAtTax\tlandClaimPropAtTaxPerMonth\t"
        + "fpCarrierWithRentPops\tfpShortWithRentPops\t"
        + "lbFixedData\tlbFrozenCopyRgo\tlbFrozenEngineRgo\tlbFreezeEventFlag\tlbWarmupCheckedPopMonths\tlbWarmupBreakPopMonths\t"
        + "lbExpBaseRAtTaxPerMonth\tlbEngBaseRAtTaxPerMonth\tlbAdjBreakPopMonths\tlbOverBasePopMonths\tlbFrozenInvariantBreak\tlbFrozenForeignEngine\tlbFrozenCopyNoEngine\tlbUnfrozenRgo";

    /// <summary>計器の名乗りの定数（係数ではない）：差分の照合の許し（相対・double）。</summary>
    internal const double LandbaseAdjTol = 1e-9;

    /// <summary>凍結の写し（計器の持ち物）：凍結した国・写しがあるか（助走の最後に見た月の地代が正）・その地代。国が州を失うと、その国の次の月の締めで消える（⑮）。</summary>
    sealed class LbFz { public World W = null!; public bool Has; public double V; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Producer, LbFz> LbFrozen = new();
    /// <summary>州の期待の差分（計器の持ち物・その月）。</summary>
    sealed class LbAdj { public double[] Exp = []; public bool Touched; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, LbAdj> LbAdjs = new();
    /// <summary>国の写し（計器の持ち物）：名簿・助走の照合の和・事象・月の照合の和・四半期の締め月の写し。</summary>
    sealed class LbNat
    {
        public bool DataReady, Fixed, FreezeSeen;
        public int FreezeEvent;
        public long WarmupChecked, WarmupBreak, FreezeChecked, FreezeBreak;
        public long AdjBreak, OverBase;
        public bool Snap;
        public double Claim, Exempt, R, Deg, Paid, ClaimProp, ExpR, EngR;
        public int CopyRgo, EngineRgo, InvBreak, ForeignEngine, CopyNoEngine, Unfrozen;
        /// <summary>
        /// 〔履歴 InstrumentsLandbase.cs#4〕
        /// </summary>
        public readonly System.Collections.Generic.List<Producer> Held = [];
        public readonly System.Collections.Generic.HashSet<Producer> Here = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, LbNat> LbNats = new();

    static LbNat LbNatOf(World w)
    {
        var a = LbNats.GetValue(w, _ => new LbNat());
        if (!a.DataReady)
        {
            string name = NameOf(w);
            foreach (var c in WorldData.Nations()) if (c.Name == name) { a.Fixed = c.LandTaxBasis == "Fixed"; break; }
            a.DataReady = true;
        }
        return a;
    }

    /// <summary>受け口（EconomyStep.PayOwnerRent の頭・RGO だけ・INS のときだけ）：割る前の地代 <paramref name="rent"/>。名簿の国の凍結の写し（助走の月に上書き）と、POP ごとの期待の差分を積む。</summary>
    internal static void LandbaseRgoRent(World w, Region r, Producer s, double rent)
    {
        BasisxRgoRent(w, r, s, rent);   // BasisxRgoRent〔履歴 InstrumentsLandbase.cs#5〕
        var a = LbNatOf(w);
        if (!a.Fixed) return;
        LbFz? fz;
        if (w.WarmupFrozen && !LsAbsenteeRgo(r, s))
        {
            if (!LbFrozen.TryGetValue(s, out fz)) { fz = new LbFz(); LbFrozen.Add(s, fz); a.Held.Add(s); }
            else if (!ReferenceEquals(fz.W, w)) a.Held.Add(s);   // ReferenceEquals〔履歴 InstrumentsLandbase.cs#6〕
            fz.W = w; fz.Has = rent > 0; fz.V = rent > 0 ? rent : 0.0;
        }
        else if (!LbFrozen.TryGetValue(s, out fz)) return;
        if (!fz.Has || !ReferenceEquals(fz.W, w)) return;
        int np = r.Pops.Count;
        var g = LbAdjs.GetValue(r, _ => new LbAdj());
        if (g.Exp.Length < np) System.Array.Resize(ref g.Exp, np);
        g.Touched = true;
        var ls = s.LandShares;
        long dummy = 0;
        if (ls is null) { if (s.OwnerPopIndex >= 0 && s.OwnerPopIndex < np) g.Exp[s.OwnerPopIndex] += fz.V - System.Math.Max(0, rent); return; }
        for (int k = 0; k < ls.Length; k++)
        {
            if (!(ls[k] > 0)) continue;
            g.Exp[LsRecipient(r, s, k, ref dummy)] += fz.V * ls[k] - System.Math.Max(0, rent * ls[k]);
        }
    }

    /// <summary>
    /// 〔履歴 InstrumentsLandbase.cs#7〕
    /// </summary>
    static void LbDropLeft(World w, LbNat a)
    {
        var here = a.Here;
        foreach (var r in w.Regions) foreach (var s in r.Producers) if (s.IsRgo) here.Add(s);
        int k = 0;
        for (int i = 0; i < a.Held.Count; i++)
        {
            var s = a.Held[i];
            if (!LbFrozen.TryGetValue(s, out var fz) || !ReferenceEquals(fz.W, w)) continue;
            if (here.Contains(s)) { a.Held[k++] = s; continue; }
            LbFrozen.Remove(s);
        }
        a.Held.RemoveRange(k, a.Held.Count - k);
        here.Clear();
    }

    /// <summary>月の閉じ（<see cref="MonthClose"/>・Fiscal.Step の後）：助走の月の照合（G7）・助走の後の最初の月の事象・POP ごとの差分の照合（G8a・G4）・四半期の締め月の写し（A-0′・G8b）。</summary>
    internal static void LandbaseMonth(World w)
    {
        if (w.Regions.Count == 0) return;
        var a = LbNatOf(w);
        if (a.Fixed && a.Held.Count > 0) LbDropLeft(w, a);   // LbDropLeft〔履歴 InstrumentsLandbase.cs#8〕
        if (w.WarmupFrozen)
        {
            if (!a.Fixed) return;
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Pops)
                {
                    double rent = p.LandRentPerMonth.Value, eng = p.LandTaxBasisAdjPerMonth.Value;
                    a.WarmupChecked++;
                    if (!(System.Math.Abs(eng) <= LandbaseIdentTol * System.Math.Max(1.0, System.Math.Abs(rent)))) a.WarmupBreak++;
                }
                if (LbAdjs.TryGetValue(r, out var gw) && gw.Touched) { System.Array.Clear(gw.Exp); gw.Touched = false; }
            }
            return;
        }
        var c = w.Coef;
        bool on = c.LandTaxFixedBasis > 0;
        bool freeze = a.Fixed && !a.FreezeSeen;   // 助走の後の最初の月（助走の無い木では最初の月＝事象 0）
        bool snap = w.IsQuarterEndMonth;
        if (!a.Fixed && !snap) return;   // 名簿の外の国は締め月の写しだけ
        double disc = c.PrivilegeTaxDiscount.Value;
        double deg = !(disc > 0) ? 0.0
            : c.PrivilegeFromCrownShare.Value > 0 ? double.NaN
            : (w.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate ? disc : 0.0);
        double landRate = w.LandTax;
        double claim = 0, exempt = 0, R = 0, claimProp = 0, expR = 0, engR = 0;
        foreach (var r in w.Regions)
        {
            LbAdjs.TryGetValue(r, out var g);
            for (int i = 0; i < r.Pops.Count; i++)
            {
                var p = r.Pops[i];
                double rent = p.LandRentPerMonth.Value, eng = p.LandTaxBasisAdjPerMonth.Value;
                double exp = g is not null && g.Touched && i < g.Exp.Length ? g.Exp[i] : 0.0;
                if (a.Fixed)
                {
                    double scale = System.Math.Max(1.0, System.Math.Abs(rent) + System.Math.Abs(exp));
                    if (!(System.Math.Abs(eng - exp) <= LandbaseAdjTol * scale)) a.AdjBreak++;
                    if (eng - exp > LandbaseAdjTol * scale) a.OverBase++;
                }
                if (!snap) continue;
                double rr = System.Math.Max(0, rent);
                R += rr;
                expR += System.Math.Max(0, rent + exp);
                double bse = on ? rent + eng : rent;
                engR += System.Math.Max(0, bse);
                if (p.IsCriminal) continue;
                double privBase = p.Closed && !p.IsSoldier ? 1.0 - deg : 1.0;
                if (landRate > 1e-9 && bse > 0) { claim += landRate * bse * privBase; exempt += landRate * bse * (1.0 - privBase); }
                if (landRate > 1e-9 && rent > 0) claimProp += landRate * rent * privBase;
            }
            if (g is not null && g.Touched) { System.Array.Clear(g.Exp); g.Touched = false; }
        }
        if (freeze)
        {
            int eng = 0;
            foreach (var r in w.Regions) foreach (var s in r.Producers) if (s.FrozenLandRentPerMonth is not null) eng++;
            a.FreezeSeen = true; a.FreezeEvent = eng > 0 ? 1 : 0; a.FreezeChecked = a.WarmupChecked; a.FreezeBreak = a.WarmupBreak;
        }
        if (!snap) return;
        int copy = 0, engine = 0, inv = 0, foreign = 0, noEng = 0, unf = 0;
        foreach (var r in w.Regions)
            foreach (var s in r.Producers)
            {
                bool has = LbFrozen.TryGetValue(s, out var fz) && fz!.Has && ReferenceEquals(fz.W, w);
                var ev = s.FrozenLandRentPerMonth;
                if (has) copy++;
                if (ev is { } e)
                {
                    engine++;
                    if (!has) foreign++;
                    else if (!(System.Math.Abs(e.Value - fz!.V) <= LandbaseIdentTol * System.Math.Max(1.0, fz.V))) inv++;
                }
                else
                {
                    if (has) noEng++;
                    if (a.Fixed && s.IsRgo && !LsAbsenteeRgo(r, s)) unf++;
                }
            }
        a.Snap = true; a.Claim = claim; a.Exempt = exempt; a.R = R; a.Deg = deg; a.Paid = w.LastTaxLandPerMonth.Value; a.ClaimProp = claimProp; a.ExpR = expR; a.EngR = engR;
        a.CopyRgo = copy; a.EngineRgo = engine; a.InvBreak = inv; a.ForeignEngine = foreign; a.CopyNoEngine = noEng; a.Unfrozen = unf;
    }

    /// <summary>四半期の行の A-0′ と A-2 の列（landfix_s・<see cref="LandfixHeaderText"/> の 4 列目から）。写しが無い国（締め月の決算が走っていない）は nan。四半期ごとの和（G8a・G4）と事象はここで 0 に戻す。</summary>
    static string LandbaseA2Cells(World w, int carriersRent, int shortRent)
    {
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var a = LbNatOf(w);
        double nan = double.NaN;
        string s = string.Join('\t',
            F(a.Snap ? a.Claim : nan), F(a.Snap ? a.Exempt : nan), F(a.Snap ? a.R : nan), F(a.Snap ? a.Deg : nan), F(a.Snap && a.Claim > 0 ? a.Paid / a.Claim : nan), F(a.Snap ? a.ClaimProp : nan),
            I(carriersRent), I(shortRent),
            I(a.Fixed ? 1 : 0), I(a.CopyRgo), I(a.EngineRgo), I(a.FreezeEvent), I(a.FreezeChecked), I(a.FreezeBreak),
            F(a.Snap ? a.ExpR : nan), F(a.Snap ? a.EngR : nan), I(a.AdjBreak), I(a.OverBase), I(a.InvBreak), I(a.ForeignEngine), I(a.CopyNoEngine), I(a.Unfrozen));
        a.AdjBreak = 0; a.OverBase = 0; a.FreezeEvent = 0; a.FreezeChecked = 0; a.FreezeBreak = 0; a.Snap = false;
        return s;
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void LandbaseClose(MultiNationSim sim)
    {
        if (LandbaseSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.Reg?.Dispose(); sk.Reg = null; sk.Fix?.Dispose(); sk.Fix = null; }
    }
}
