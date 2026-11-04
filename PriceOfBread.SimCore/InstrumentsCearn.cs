using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (822) 植民地の稼ぎの表 cearn_s（読むだけ・恒等・事前登録 measurements/m822_20261104_prereg_ce.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpwd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（計器の欄は ConditionalWeakTable と ThreadStatic だけ）。
// 目的＝(814) の凍結（植民地の州のお金が月 1.5〜2.3% で縮み、工場と採取の産出が 0 で止まる）の出口を、州の POP の貯蓄の台帳から読む。
// 行＝全ての国の全ての州×四半期（region_s と同じ母集合）。額は州の持ち主の国の通貨・四半期の和（…PerQuarter）。助走の月は数えない（Phase の warm で捨てる）。
// 台帳の出所＝貯蓄の書き手の 1 本の計器の口 Sav（Pop の中の書き手がそこを呼ぶ）。POP の州は計器が自分で持つ登録（月初の Begin で全ての国の全ての州の POP）。
//   列は書き手の種類・相手の側（P＝POP・T＝国庫・B／M／F／C／H＝世界の外）・国庫の区分（World.Transfer の頭で写す）・呼び手のファイル名で決める（行番号は列の鍵にしない）。
//   POP→POP は払い手の州で流出・受け手の州で流入。州の中の運搬（社会移動・徴兵・州の中の移動）は同じ州の出と入りが打ち消す。
// 流入 7 列：inMarket（月末の精算の所得の側）・inExport（世界の外〔B・M・F〕から・CapitalMarket.cs 以外）・inState（国庫から）・inCredit（与信 C・H から）・inFromPops（他の POP から）・
//   inMigration（運搬の受け取り）・inOther（CapitalMarket.cs の世界の外から・換算の正・どれでもない受け取り）。
// 流出 9 列（正の額）：outMarket（精算の支払いの側＝所得の側−精算の増分）・outTax（国庫へ・区分 Tax）・outBond（国庫へ・区分 Borrow）・outDues（Fiscal.cs の POP→POP＝十分の一税と仲介者の取り分）・
//   outColonialRent（Colonial.cs の POP→POP＝植民地の取り分）・outCapital（CapitalMarket.cs の POP→世界の外＝外資の配当・資本の送り出し・逃避）・outImport（BilateralMarket.cs の POP→世界の外＝輸入の代金と運賃）・
//   outMigration（運搬の送り出し）・outOther（それ以外）。
// 閉じ：residual＝savings−savingsStart−(Σ流入−Σ流出)。経路の上位＝精算と換算を除く書き込みの鍵（ファイル:行:種類:相手の側）の和の上位（行番号は読み手が測った木で名前へ引く）。
// 値段と床・凍結・人頭税の物差しの列は四半期の末の断面（州の生産者・市場・POP を計器が自分で読む・エンジンの関数を呼ばない・自己費用は読み手が MarginalCost×costPriceLevel×localPriceMultiplier で組む）。
// ⚠限界（計器が自分で申告する）：①登録は月初＝月の途中で作られた POP の書き込みは次の月まで台帳の外（残差と lateRegisteredPops に出る）②並列の国の日次の中で別の国の POP へ払う書き込みは無い前提（flow_s と同じ）
//   ③精算の所得と支払いは州の中の市場の循環を含む（州の外への純の払いは outMarket−inMarket）④国庫の区分は World.Transfer の頭で写した値（運搬の書き込みは区分を読まない）
//   ⑤格・乗数・値段は四半期の末の値（savingsPerCapitaOverNation＝全ての州の 1 人あたりで割る〔事前登録の定義〕・savingsPerCapitaOverMainland＝州の乗数の分母と同じ本土の 1 人あたりで割る〔腕の前の追記〕）⑥国家の市場を通る出入り（stateMarket*）は POP の台帳の外（StateSpent の月の和）⑦鉱脈の品位の器 MineralCostFollowsGrade（既定 0）は自己費用に掛けない。
public static partial class Instruments
{
    internal const string CearnHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tsavingsStart\tsavings\t"
        + "inMarketPerQuarter\tinExportPerQuarter\tinStatePerQuarter\tinCreditPerQuarter\tinFromPopsPerQuarter\tinMigrationPerQuarter\tinOtherPerQuarter\t"
        + "outMarketPerQuarter\toutTaxPerQuarter\toutBondPerQuarter\toutDuesPerQuarter\toutColonialRentPerQuarter\toutCapitalPerQuarter\toutImportPerQuarter\toutMigrationPerQuarter\toutOtherPerQuarter\t"
        + "residualPerQuarter\tlateRegisteredPops\tstateMarketSpendPerQuarter\tstateMarketTakePerQuarter\t"
        + "outTop1Route\toutTop1PerQuarter\toutTop2Route\toutTop2PerQuarter\tinTop1Route\tinTop1PerQuarter\t"
        + "localPriceMultiplier\tlocalPriceAtFloorFlag\tregionPriceLevel\tcostPriceLevel\tsavingsPerCapitaOverNation\tsavingsPerCapitaOverMainland\t"
        + "stapleGood\tstaplePrice\tstapleMarginalCost\ttop1Good\ttop1Price\ttop1MarginalCost\ttop2Good\ttop2Price\ttop2MarginalCost\ttop3Good\ttop3Price\ttop3MarginalCost\t"
        + "belowSelfCostShare\tfactoryOutputRefValuePerMonth\textractionOutputRefValuePerMonth\tfrozenFlag\tpollClaimPerMonth\tearnedGrossPerMonth\ttaxRate\tpollTaxLevel";

    /// <summary>台帳の列（流入 7・流出 9）。<see cref="CearnColumn"/> の返り値。</summary>
    internal enum CearnCol : sbyte
    {
        Skip = -1,
        InMarket, InExport, InState, InCredit, InFromPops, InMigration, InOther,
        OutMarket, OutTax, OutBond, OutDues, OutColonialRent, OutCapital, OutImport, OutMigration, OutOther,
    }
    internal const int CearnInCols = 7, CearnCols = 16;

    internal sealed class CearnAcc
    {
        public readonly double[] Flow = new double[CearnCols];   // 流入は増分の和・流出は減りの和（正）
        public double Start = double.NaN; public bool Started;
        public int Late;
        public double StateSpend, StateTake;
        public readonly Dictionary<(string File, int Line, SavKind Kind, char Cp), double> Out = new(), In = new();
        public void Clear() { System.Array.Clear(Flow); Late = 0; StateSpend = 0; StateTake = 0; Out.Clear(); In.Clear(); }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, CearnAcc> CearnAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, CearnAcc> CearnPopAcc = new();
    sealed class CearnSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CearnSink> CearnSinks = new();
    [System.ThreadStatic] static TreasuryCat _cearnCat;

    /// <summary>試験の口：州の台帳（無ければ null）。</summary>
    internal static CearnAcc? CearnAccOf(Region r) => CearnAccs.TryGetValue(r, out var a) ? a : null;

    /// <summary>World.Transfer の頭（計器の行）：国庫の側の区分（受け手が国庫なら toCat ?? cat・払い手が国庫なら cat＝どちらも Pay に渡る値）を写す。</summary>
    internal static void CearnRoute(TreasuryCat cat, TreasuryCat? toCat) => _cearnCat = toCat ?? cat;

    /// <summary>列の振り分け（純関数）。<paramref name="fileName"/>＝呼び手のファイル名（パスを除く）。精算（Settle）は 2 列に分けるので呼ばない（<see cref="CearnCol.Skip"/>）。</summary>
    internal static CearnCol CearnColumn(SavKind kind, char cp, TreasuryCat cat, string fileName, double delta)
    {
        switch (kind)
        {
            case SavKind.Settle: case SavKind.RegionOut: case SavKind.RegionIn: return CearnCol.Skip;
            case SavKind.Rebase: return delta > 0 ? CearnCol.InOther : CearnCol.OutOther;
            case SavKind.CarryIn: return CearnCol.InMigration;
            case SavKind.CarryOut: return CearnCol.OutMigration;
            case SavKind.Give: case SavKind.GiveIncome:
                return cp switch
                {
                    'T' => CearnCol.InState,
                    'C' or 'H' => CearnCol.InCredit,
                    'P' => CearnCol.InFromPops,
                    'B' or 'M' or 'F' => fileName == "CapitalMarket.cs" ? CearnCol.InOther : CearnCol.InExport,
                    _ => CearnCol.InOther,
                };
            case SavKind.Take:
                return cp switch
                {
                    'T' => cat == TreasuryCat.Tax ? CearnCol.OutTax : cat == TreasuryCat.Borrow ? CearnCol.OutBond : CearnCol.OutOther,
                    'P' => fileName == "Fiscal.cs" ? CearnCol.OutDues : fileName == "Colonial.cs" ? CearnCol.OutColonialRent : CearnCol.OutOther,
                    'B' => fileName == "CapitalMarket.cs" ? CearnCol.OutCapital : fileName == "BilateralMarket.cs" ? CearnCol.OutImport : CearnCol.OutOther,
                    _ => CearnCol.OutOther,
                };
            default: return delta > 0 ? CearnCol.InOther : CearnCol.OutOther;
        }
    }

    /// <summary>Sav の頭：登録された POP の書き込みを、その州の台帳の列へ 1 件足す。</summary>
    static void CearnSav(Pop p, SavKind kind, double delta)
    {
        if (kind is SavKind.RegionOut or SavKind.RegionIn) return;   // 州の移し替えの記帳（お金は動かない）
        if (!CearnPopAcc.TryGetValue(p, out var a)) return;
        if (kind == SavKind.Settle)
        {
            double earned = Calendar.OneMonthOf(p.EarnedPerMonth).Value;   // 精算の所得の側（同じ月の値・精算の書き込みの直後に読む）
            a.Flow[(int)CearnCol.InMarket] += earned;
            a.Flow[(int)CearnCol.OutMarket] += earned - delta;             // 支払いの側＝所得−増分（精算の 1 行に閉じる）
            return;
        }
        bool routed = kind is SavKind.Give or SavKind.GiveIncome or SavKind.Take or SavKind.CarryOut or SavKind.CarryIn;
        char cp = !routed ? '-' : kind is SavKind.Give or SavKind.GiveIncome ? _routeFrom : kind == SavKind.Take ? _routeTo : 'P';
        string file = _routeFile ?? "";
        var col = CearnColumn(kind, cp, _cearnCat, CearnFileName(file), delta);
        if (col == CearnCol.Skip) return;
        bool isIn = (int)col < CearnInCols;
        a.Flow[(int)col] += isIn ? delta : -delta;
        if (!routed) return;
        var key = (file, _routeLine, kind, cp);
        var d = isIn ? a.In : a.Out;
        d.TryGetValue(key, out double v);
        d[key] = v + (isIn ? delta : -delta);
    }

    [System.ThreadStatic] static string? _cearnLastPath, _cearnLastName;
    /// <summary>呼び手のファイルの名前（パスを除く）。直前と同じ文字列の参照（呼び手の位置の定数）なら写しを使う＝呼ぶたびに文字列を作らない。</summary>
    static string CearnFileName(string path)
    {
        if (!ReferenceEquals(path, _cearnLastPath)) { _cearnLastPath = path; _cearnLastName = System.IO.Path.GetFileName(path); }
        return _cearnLastName ?? "";
    }

    static double CearnSavings(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Savings.Value; return s; }

    /// <summary>月初（Begin）：全ての国の州の POP を州の台帳へ登録する。初めての州は始まりの在庫をここで取る。四半期の途中で初めて登録した POP は lateRegisteredPops に数える。</summary>
    static void CearnRegister(World w)
    {
        foreach (var r in w.Regions)
        {
            var a = CearnAccs.GetValue(r, _ => new CearnAcc());
            bool fresh = !a.Started;
            if (fresh) { a.Start = CearnSavings(r); a.Started = true; }
            foreach (var p in r.Pops)
            {
                if (CearnPopAcc.TryGetValue(p, out var a0) && ReferenceEquals(a0, a)) continue;
                CearnPopAcc.AddOrUpdate(p, a);
                if (!fresh) a.Late++;
            }
        }
    }

    /// <summary>World.SettleStateSpent の頭：州の国家の仮勘定（区分つき・正＝国庫→生産者・負＝国庫へ）を読む。</summary>
    internal static void CearnStateSpent(Region r)
    {
        if (!CearnAccs.TryGetValue(r, out var a)) return;
        foreach (double v in r.StateSpent) { if (v > 0) a.StateSpend += v; else if (v < 0) a.StateTake -= v; }
    }

    /// <summary>助走の直後（Phase warm）：助走の月の流れを捨て、始まりの在庫を取り直す。</summary>
    static void CearnWarm(MultiNationSim sim)
    {
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                var a = CearnAccs.GetValue(r, _ => new CearnAcc());
                a.Clear(); a.Start = CearnSavings(r); a.Started = true;
            }
    }

    /// <summary>四半期の末の断面の列（値段と床・主食と上位 3 財・自己費用の下限・凍結・人頭税の物差し）。試験が州から直に呼べる。</summary>
    internal readonly record struct CearnSnap(double PopK, double Savings, double Lpm, int AtFloor, double RegionPriceLevel, double CostPriceLevel, double SavPerCapOverNation, double SavPerCapOverMainland,
        int StapleGood, double StaplePrice, double StapleMc, int[] TopGood, double[] TopPrice, double[] TopMc, double BelowSelfCostShare,
        double FacOut, double ExtOut, int Frozen, double PollClaimPerMonth, double EarnedGrossPerMonth);

    /// <summary>国の 1 人あたりの貯蓄の 2 つの物差し：Mainland＝州の乗数の分母と同じ本土（植民地格を除く）の州の和（本土の人口が 0 なら全ての州）・All＝全ての州の和（事前登録の定義）。分母が正でなければ NaN。</summary>
    internal static (double Mainland, double All) CearnNationPerCap(World w)
    {
        double ns = 0, np = 0, ms = 0, mp = 0;
        foreach (var x in w.Regions)
        {
            double rs = 0, rp = 0;
            foreach (var p in x.Pops) { rs += p.Savings.Value; rp += p.Size; }
            ns += rs; np += rp;
            if (x.Tier != RegionTier.Colonial) { ms += rs; mp += rp; }
        }
        double all = np > 0 ? ns / np : double.NaN;
        return (mp > 1e-9 ? ms / mp : all, all);
    }

    internal static CearnSnap CearnSnapshot(World w, Region r, double natSavPerCap, double mainSavPerCap)
    {
        var c = w.Coef;
        double pop = 0, sav = 0, poll = 0, earned = 0;
        bool pollOn = w.PollTax > 1e-9;
        foreach (var p in r.Pops)
        {
            pop += p.Size; sav += p.Savings.Value;
            double e = p.LastEarnedPerMonth.Value; if (e > 0) earned += e;
            if (pollOn && p.IsCommoner && !p.IsCriminal) poll += c.PollTaxRate * w.PollTax * p.Size;   // 査定の全額（届く割合と払える割合を掛けない）
        }
        double lpm = r.LocalPriceMultiplier, cpl = w.CostPriceLevel;
        int atFloor = lpm <= c.RegionalPriceLevelMin + 1e-9 ? 1 : 0;
        double perCap = pop > 0 && natSavPerCap > 0 ? sav / pop / natSavPerCap : double.NaN;
        double perCapMain = pop > 0 && mainSavPerCap > 0 ? sav / pop / mainSavPerCap : double.NaN;
        // 主食＝層 Staple の財のうち州の LastDemand が最大の財
        int staple = -1; double stapleDem = -1;
        int ng = System.Math.Min(r.Markets.Length, Catalog.Goods.Length);
        for (int g = 0; g < ng; g++)
            if (Catalog.Goods[g].Tier == GoodTier.Staple && r.Markets[g].LastDemand > stapleDem) { stapleDem = r.Markets[g].LastDemand; staple = g; }
        // 上位 3 財＝州の生産者（労働の財と倒産を除く）の Output×参照価格の和の大きい順
        var val = new Dictionary<int, double>();
        int below = 0, belowDen = 0;
        foreach (var s in r.Producers)
        {
            int g = s.GoodId;
            if (Catalog.IsLaborGood(g) || s.Bankrupt || g < 0 || g >= ng) continue;
            double rp = g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
            val.TryGetValue(g, out double v0); val[g] = v0 + s.Output * rp;
            if (s.Scale > 0) { belowDen++; if (r.Markets[g].Price <= s.MarginalCost * cpl * lpm) below++; }
        }
        var top = new List<KeyValuePair<int, double>>(val);
        top.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : x.Key.CompareTo(y.Key));
        var tg = new int[3]; var tp = new double[3]; var tm = new double[3];
        for (int k = 0; k < 3; k++)
        {
            if (k < top.Count && top[k].Value > 0) { tg[k] = top[k].Key; tp[k] = r.Markets[tg[k]].Price; tm[k] = CearnMc(r, tg[k]); }
            else { tg[k] = -1; tp[k] = double.NaN; tm[k] = double.NaN; }
        }
        var prod = RegionProdRow(r);   // region_s と同じ定義の工場と採取の産出（計器の関数）
        int frozen = prod[0] == 0 && prod[1] == 0 && prod[2] + prod[4] > 0 ? 1 : 0;
        return new CearnSnap(pop, sav, lpm, atFloor, r.PriceLevel, cpl, perCap, perCapMain,
            staple, staple >= 0 ? r.Markets[staple].Price : double.NaN, staple >= 0 ? CearnMc(r, staple) : double.NaN, tg, tp, tm,
            belowDen > 0 ? (double)below / belowDen : double.NaN, prod[0], prod[1], frozen, poll, earned);
    }

    /// <summary>州の生産者のうちその財の Capacity が最大の 1 件の MarginalCost（参照価格の単位）。無ければ NaN。</summary>
    static double CearnMc(Region r, int g)
    {
        Producer? best = null;
        foreach (var s in r.Producers) if (s.GoodId == g && !s.Bankrupt && (best is null || s.Capacity > best.Capacity)) best = s;
        return best is null ? double.NaN : best.MarginalCost;
    }

    static string CearnTop(Dictionary<(string File, int Line, SavKind Kind, char Cp), double> d, int rank, out double amount)
    {
        amount = double.NaN;
        if (d.Count <= rank) return "-";
        var l = new List<KeyValuePair<(string File, int Line, SavKind Kind, char Cp), double>>(d);
        l.Sort((x, y) =>
        {
            int c = y.Value.CompareTo(x.Value); if (c != 0) return c;
            c = string.CompareOrdinal(x.Key.File, y.Key.File); if (c != 0) return c;
            c = x.Key.Line.CompareTo(y.Key.Line); if (c != 0) return c;
            c = x.Key.Kind.CompareTo(y.Key.Kind); return c != 0 ? c : x.Key.Cp.CompareTo(y.Key.Cp);
        });
        var k = l[rank].Key; amount = l[rank].Value;
        return System.IO.Path.GetFileName(k.File) + ":" + k.Line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + SavKindNames[(int)k.Kind] + ":" + k.Cp;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の全ての州の行を書き、台帳を戻して始まりの在庫を取り直す。</summary>
    static void CearnTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = CearnSinks.GetValue(sim, _ => new CearnSink());
        sk.W ??= Open("cearn", seed);
        if (!sk.Header) { sk.W.WriteLine(CearnHeaderText); sk.Header = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = NameOf(w);
            var (mainPerCap, natPerCap) = CearnNationPerCap(w);
            foreach (var r in w.Regions)
            {
                var a = CearnAccs.GetValue(r, _ => new CearnAcc());
                var sn = CearnSnapshot(w, r, natPerCap, mainPerCap);
                double inSum = 0, outSum = 0;
                for (int k = 0; k < CearnInCols; k++) inSum += a.Flow[k];
                for (int k = CearnInCols; k < CearnCols; k++) outSum += a.Flow[k];
                double resid = a.Started ? sn.Savings - a.Start - (inSum - outSum) : double.NaN;
                var cols = new List<string>(64) { I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), F(sn.PopK), F(a.Started ? a.Start : double.NaN), F(sn.Savings) };
                foreach (double v in a.Flow) cols.Add(F(v));
                cols.Add(F(resid)); cols.Add(I(a.Late)); cols.Add(F(a.StateSpend)); cols.Add(F(a.StateTake));
                cols.Add(CearnTop(a.Out, 0, out double o1)); cols.Add(F(o1)); cols.Add(CearnTop(a.Out, 1, out double o2)); cols.Add(F(o2)); cols.Add(CearnTop(a.In, 0, out double i1)); cols.Add(F(i1));
                cols.Add(F(sn.Lpm)); cols.Add(I(sn.AtFloor)); cols.Add(F(sn.RegionPriceLevel)); cols.Add(F(sn.CostPriceLevel)); cols.Add(F(sn.SavPerCapOverNation)); cols.Add(F(sn.SavPerCapOverMainland));
                cols.Add(sn.StapleGood >= 0 ? Catalog.Goods[sn.StapleGood].Name : "-"); cols.Add(F(sn.StaplePrice)); cols.Add(F(sn.StapleMc));
                for (int k = 0; k < 3; k++) { cols.Add(sn.TopGood[k] >= 0 ? Catalog.Goods[sn.TopGood[k]].Name : "-"); cols.Add(F(sn.TopPrice[k])); cols.Add(F(sn.TopMc[k])); }
                cols.Add(F(sn.BelowSelfCostShare)); cols.Add(F(sn.FacOut)); cols.Add(F(sn.ExtOut)); cols.Add(I(sn.Frozen));
                cols.Add(F(sn.PollClaimPerMonth)); cols.Add(F(sn.EarnedGrossPerMonth)); cols.Add(F(w.TaxRate.Value)); cols.Add(F(w.PollTax));
                sk.W.WriteLine(string.Join('\t', cols));
                a.Clear(); a.Start = sn.Savings; a.Started = true;
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CearnClose(MultiNationSim sim)
    {
        if (CearnSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
