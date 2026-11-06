using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (894) 土地税の算定の基準 A1 の段 A-0 の恒等の計器 landbase_s・landbasereg_s（読むだけ・恒等・事前登録 measurements/m894_20261106_prereg_lb.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSubst.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口と遅れの列の履歴は ConditionalWeakTable だけ＝セーブに載らない）。INS=1 のときだけ動く（Instruments.Quarter から呼ばれる）。
// 目的＝段 A-2（算定の基準の 2 値・定額の凍結・F_p）の材料：今の土地税の請求（Fiscal.cs:803〜:804 の独立の写し）・実際に取れた土地税・自給へ広げた請求の上界（J-a・J-b）・基数の遅れの比（凍結したときの重さ）・担い手の請求′と手持ちの差（F_p の母集合）を同じ盤面で数える。
// エンジンの関数を呼ばない（AssessPop・ForecastTax・RentShareOf・Privileges.TaxAssessment・Privileges.TitheDegree・Colonial.AbsenteeRgo を呼ばず、式を計器が自分で書く）。エンジンの状態は欄として読むだけ。
// 月の中の順（Simulation.cs）：Government.Step（:1789）→ EconomyStep.Run（:2429・EconomyStep.cs:806 で地代を 0 へ戻し :2747 で積み :2854 で Colonial.RemitAbsenteeRent が不在地主へ移す）→ Fiscal.Step（:2473）。
//   四半期の末（3 か月目の Fiscal.Step の後）の LandRentPerMonth はその月に課税された基数そのもの・LastTaxLandPerMonth（Fiscal.cs:121）はその月に実際に取れた土地税。
// R（国の基数）＝州→POP の Σ max(0, LandRentPerMonth)（犯罪者を含む・州 0 の国は行を書かない）＝cornint_s の rentDomTotalPerMonth と同じ集合（未帰属 rentDomUnattr はその部分集合＝足さない）。
//   baseRCheckRelDiff・baseRCheckBreakFlag＝同じ R を 2 通り（1 本の和・cornint_s と同じく 3 つのブロックに分けて足してから 3 本を足す）に double で数えた相対差と 1e-12 の旗（印字の桁の床で門を置かない）。
// 法の読み手の一覧（訂正 (6)・計器は写していない）：Fiscal.cs:85（Step が w.LandTax を AssessPop へ渡す）・Fiscal.cs:90（特権台帳 Privileges.Record・旗 PrivilegeLedgerEnabled＝0 で今は動かない）。
// ⚠限界（計器が自己申告する）：①四半期の末の月の値（3 か月目）だけ②輸出の地代（InternationalMarket.PaySales）は基数に入らない（cornint_s の rentExpPerMonth を読む）
//   ③請求′は所得税に課税への同意を掛けない上界・累進課税なし・仲介者の取り分なし・当月の土地税なし・月の請求′を四半期の末の貯蓄と比べる④PrivilegeFromCrownShare＞0 の木では免税の度合いが nan
//   ⑤自給へ広げた請求は LastSubsistenceOutput（σ を 1 で切った後の量×収穫係数）を使う＝作った量の余り E_p は入らない
//   ⑥遅れの列は計器が見た四半期だけで数える（建国・割譲で母集合が変わった四半期は比が跳ぶ＝nan にしない＝読み手が割譲の表と突き合わせる・州の履歴は Region を鍵にする＝割譲で持ち主が変わっても州に付いて行く）
//   ⑦州の基数は Colonial.RemitAbsenteeRent の後（訂正 (2)＝植民地格の州の輸出作物の大農園の地代は首都の州の投資家の POP に載る・国の R は変わらない）⑧法の読み手のうち Fiscal.cs:85・:90 は写していない。
//   ⑨(904) 計器の行は QuarterEnd（Simulation.cs:1896）の後に書く＝QuarterEnd の Politics.Step（:1908）・Meritocracy.Step（:1912）・割譲（外交は四半期の境）は課税（3 か月目の Fiscal.Step）の後で計器の前に走る＝四半期の末の LandRentPerMonth・州の POP は課税の時点とは別の盤面の物（社会移動・粛清・割譲で POP と持ち主が動きうる）。
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

    sealed class LandbaseSink { public System.IO.StreamWriter? W, Reg; }
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

    /// <summary>主食の値段（subst_s の stapleNeedWeightedPrice と同じ式＝InstrumentsSubst.cs:89〜:92）：主食の BaseNeed で重みを付けた州の値段。重みが 0 なら nan。</summary>
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
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;   // cornint_s と同じ行の集合
            var c = w.Coef;
            string nation = NameOf(w);
            bool picked = IsPicked(w, S(w));
            // 身分制免税の度合い d′（Privileges.cs:41〜:45 を計器が書く）
            double disc = c.PrivilegeTaxDiscount.Value;
            double deg = !(disc > 0) ? 0.0
                : c.PrivilegeFromCrownShare.Value > 0 ? double.NaN
                : (w.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate ? disc : 0.0);
            double landRate = w.LandTax, pollLevel = w.PollTax, taxRate = w.TaxRate.Value;
            // 十分の一税の率′（Privileges.cs:35〜:37 の度合いを計器が書く）と聖職の有無（Fiscal.Step の clergy と同じ判定）
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
            int carriers = 0, shortPops = 0, noPrice = 0, k1 = 0, k10 = 0, k100 = 0, k1000 = 0, kOver = 0;
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
                    carriers++;
                    double cl = 0;
                    if (pollLevel > 1e-9 && p.IsCommoner) cl += c.PollTaxRate * pollLevel * p.Size;
                    cl += taxRate * System.Math.Max(0, p.LastEarnedPerMonth.Value) * privBase;
                    if (tithe && p.IsCommoner && !p.IsModernElite && p.LastEarnedPerMonth.Value > 0) cl += titheRate * p.LastEarnedPerMonth.Value;
                    double held = System.Math.Max(0, p.Savings.Value);
                    if (!(cl > held)) continue;
                    shortPops++; shortSize += p.Size;
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
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void LandbaseClose(MultiNationSim sim)
    {
        if (LandbaseSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; sk.Reg?.Dispose(); sk.Reg = null; }
    }
}
