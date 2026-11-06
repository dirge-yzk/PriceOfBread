using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (895) 土地の持ち分の段 B-0 の恒等の計器 landshare_s（読むだけ・恒等・事前登録 measurements/m895_20261106_prereg_ls.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCornint.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝土地の持ち分（Producer.LandShares・NationConfig.LandOwnership）の器が地代を持ち分どおりに受け手へ渡したかを、期待（計器が自分で掛けた値）と実際（POP の LandRentPerMonth）で比べる＋B-2a の予言の材料（非賦存の地代を今の持ち主の階級で分けた和）。
// 行＝国×四半期（全ての国）。額は国の通貨・四半期の月の和（…PerQuarter）。助走の月（World.WarmupFrozen）は数えない。階級の 5 区分＝Noble（PopClass.Noble）・Clergy（Clergy）・Peasant（Peasant）・Capitalist（Capitalist）・Other（その他）。
// エンジンの関数を呼ばない：受け手の枠（0 貴族＝持ち主・1 教会＝州の聖職者・2 農民＝担い手・3 市民＝州の投資家・範囲の外は持ち主へ寄せる）・賦存の判別（州の Resources に載る財）・不在地主の判別・データの正規化・掛け算を自分で書く
//   （EconomyStep.LandShareRecipient・PayOwnerRent・Colonial.AbsenteeRgo・Simulation.LandOwnershipShares と同じ規則の写し＝片方の誤りをもう片方で拾う）。エンジンの局所から受けるのは受け口の地代の総額 rent だけ（SbRgo の先例）。
// 受け口：EconomyStep の④（国営持分と赤字の枝の後・割る前の総額）＝LandshareRgo・InternationalMarket.PaySales（国営持分を引いた後の正の rent）＝LandshareExp。月の閉じ＝Instruments.MonthClose の頭＝LandshareMonth。
// 期待（expectedByClass）＝非植民地格の州の全ての RGO の rent>0×欄の重み（null＝持ち主に 1）→受け手の POP の ClassId。
//   ＋送金の項（事前登録 [3]・反証役の訂正 1 の (a)）：植民地格の州の輸出作物の大農園の地代は月末に首都の投資家の LandRent へ移る（Colonial.RemitAbsenteeRent）＝計器は植民地格の州の POP ごとに
//   その月の LandRent の積み pre（受け口の rent>0 を同じ受け手で足した値）を数え、月の閉じで残った LandRent post との差 max(0, pre−post) を送った額として首都の投資家の ClassId へ足す（期待と data の両方）。
// data（dataByClass）＝同じ集合で、欄の代わりに world.json の側（WorldData.Nations() の同じ名の国の LandOwnership を計器が自分で正規化）を、非賦存×持ち主が貴族の RGO にだけ掛けた期待（他とデータの無い国は持ち主に 1）。
// 実際（landRentActual）＝月の閉じの非植民地格の州の POP の LandRentPerMonth（階級ごと）。
// ⚠限界（計器が自己申告する）：①実際の側は国内だけ（輸出の地代は LandRent に載らない＝expRent・expSlotExpected は門にしない）②植民地格の州は外し送金だけを足す③rent≦0 は LandRent が立たない＝期待も rent>0 の分だけ
//   ④期待も実際も受け手の POP の ClassId で積む⑤昇格した植民地は Integrated＝本国の側・欄は null＝data は欄を持つ州だけの列か最初の四半期で読む⑥rentNonEndowByOwnerClass は受け口の時点の持ち主の階級
//   ⑦data は国の名で world.json の国を引く＝建国の国・名の変わった国はデータ無し⑧割譲で州が月の途中に動けば旧の国の受け口と新の国の LandRent が分かれる（外交は四半期の境＝見込み 0）⑨slotFallbackCount は計器が自分で引いた受け手の寄せの回数
//   ⑩持ち分に合わせた他の計器＝cornint_s（InstrumentsCornint の所有の値・按分・輸出の層）と cearn_s（InstrumentsCearn の地代の受け手の行）。sbr_s の自営の数（InstrumentsSb.SbRgo の owner==labor）は今のまま＝欄を持つ RGO の農民の枠は数えない。
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

    /// <summary>データの正規化（計器の写し・純関数）：長さ 4・全て有限で 0 以上・和が正なら和で割った写し、そうでなければ null。</summary>
    internal static double[]? LsNormalize(double[]? lo)
    {
        if (lo is null || lo.Length != 4) return null;
        double sum = 0;
        for (int k = 0; k < 4; k++) { double v = lo[k]; if (!(v >= 0) || double.IsInfinity(v)) return null; sum += v; }
        if (!(sum > 0) || double.IsInfinity(sum)) return null;
        var o = new double[4];
        for (int k = 0; k < 4; k++) o[k] = lo[k] / sum;
        return o;
    }

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
        // (904) 末尾の 3 列の積み：送金が期待へ入らなかった月（首都が植民地格・首都の投資家の添字が範囲の外）の額・首都が植民地格だった月数・その規則（Tier≠Colonial の月だけ送金を期待へ足す）での月ごとの相対差の最大
        public double QRemitNotAdded, MaxSlotTierRule; public int CapColonialMonths;
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
    internal static void LandshareRgo(World w, Region r, Producer s, double rent)
    {
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
                else for (int k = 0; k < ls.Length; k++) if (ls[k] > 0) g.Pre[LsRecipient(r, s, k, ref a.Fallback)] += rent * ls[k];
            }
            return;
        }
        bool endow = LsEndow(r, s);
        int ownerCls = LsClass(r.Pops[s.OwnerPopIndex].ClassId);
        if (endow) a.MEndow += rent; else { a.MNonEndow += rent; a.MByOwner[ownerCls] += rent; }
        if (ls is null) g.Exp[ownerCls] += rent;
        else for (int k = 0; k < ls.Length; k++) if (ls[k] > 0) g.Exp[LsClass(r.Pops[LsRecipient(r, s, k, ref a.Fallback)].ClassId)] += rent * ls[k];
        var d = a.Data;
        if (d is not null && !endow && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble)
        {
            long dummy = 0;
            for (int k = 0; k < d.Length; k++) if (d[k] > 0) g.Dat[LsClass(r.Pops[LsRecipient(r, s, k, ref dummy)].ClassId)] += rent * d[k];
        }
        else g.Dat[ownerCls] += rent;
    }

    /// <summary>受け口（InternationalMarket.PaySales・INS のときだけ）：国営持分を引いた後の正の輸出の地代 <paramref name="rent"/>。</summary>
    internal static void LandshareExp(World w, Region r, Producer s, double rent)
    {
        if (!(rent > 0)) return;
        var a = LsAccs.GetValue(w, _ => new LsAcc());
        a.MExp += rent;
        int ab = LsAbsenteeRgo(r, s) ? LsAbsenteeOwner(w, r) : -1;
        if (ab >= 0) { a.MExpSlot[LsClass(w.Region.Pops[ab].ClassId)] += rent; return; }
        if (s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count) return;
        var ls = s.LandShares;
        if (ls is null) a.MExpSlot[LsClass(r.Pops[s.OwnerPopIndex].ClassId)] += rent;
        else for (int k = 0; k < ls.Length; k++) if (ls[k] > 0) a.MExpSlot[LsClass(r.Pops[LsRecipient(r, s, k, ref a.Fallback)].ClassId)] += rent * ls[k];
    }

    /// <summary>月の閉じ（Instruments.MonthClose の頭・その月の全ての州の EconomyStep と不在地主の送金の後）：期待と data と実際を比べ、四半期の和へ足す。</summary>
    internal static void LandshareMonth(World w)
    {
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
            var expTier = (double[])exp.Clone();   // (904) 送金を足す前の期待＝下の Tier≠Colonial の規則の期待の元
            var capR = w.Region;
            bool capColonial = capR.Tier == RegionTier.Colonial;
            if (capColonial) a.CapColonialMonths++;
            if (remit > 0)
            {
                var cap = w.Region;
                bool capIn = cap.InvestorPopIndex >= 0 && cap.InvestorPopIndex < cap.Pops.Count;   // (904) LsAbsenteeOwner と同じ範囲の確かめ（範囲の外は足さず末尾の列へ）
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
        }
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
            // (904) 末尾に足した 3 列（既にある列の値は変えない）：首都が植民地格だった月数・送金が期待へ入らなかった額・Tier≠Colonial の月だけ送金を期待へ足す規則での相対差の最大
            cols.Add(("capColonialMonths", I(a.CapColonialMonths))); cols.Add(("remitNotAddedPerQuarter", F(a.QRemitNotAdded))); cols.Add(("maxRelDiffSlotVsActualTierRule", F(a.MaxSlotTierRule)));
            if (!sk.Header) { sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Name))); sk.Header = true; }
            sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Val)));
            a.QNonEndow = a.QEndow = a.QColonial = a.QExp = a.QRemit = 0;
            System.Array.Clear(a.QByOwner); System.Array.Clear(a.QExpected); System.Array.Clear(a.QData); System.Array.Clear(a.QActual);
            System.Array.Clear(a.QActualF); System.Array.Clear(a.QExpectedF); System.Array.Clear(a.QExpSlot);
            a.QRemitNotAdded = 0; a.MaxSlotTierRule = 0; a.CapColonialMonths = 0;
            a.MaxSlot = a.MaxData = 0; a.Fallback = 0; a.Months = 0; a.DataReady = false;
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void LandshareClose(MultiNationSim sim)
    {
        if (LandshareSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
