using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(894) 線 c76lb：土地税の算定の基準 A1 の段 A-0 の表 <c>landbase_s</c>（国×四半期）・<c>landbasereg_s</c>（州×四半期）（読むだけ）の番人＝計器の不変条件。
/// 事前登録 measurements/m894_20261106_prereg_lb.txt の T1〜T13。期待は計器と別の出所から作る：POP と州と生産者の欄から試験が自分で組んだ値・エンジンの査定 <see cref="Fiscal.AssessPop"/>（T2）・
/// <see cref="World.LastTaxLandPerMonth"/>（T4）・subst_s と cornint_s の列（T8・T10）・計器の有無の 2 つのシム（T7）。計器の列どうしでは比べない。
/// 突然変異（使い捨ての木・measurements/m894_20261106/mut894.txt）：U1〜U15（事前登録 [6] の表）。実測の読み替え（(904)）：事前登録 [6] は U2（R の max(0, …) を外す）を T3・T8 の赤と予言したが、mut894.txt の実測は T3 だけが赤で T8 は緑のまま（T8 が緑のままの理由は未確認）＝U2 を捕まえるのは T3 だけ。</summary>
[Collection("Instruments")]
public class LandbaseInstrumentTests
{
    private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
    private static bool Near(double a, double b, double rel) => Math.Abs(a - b) <= rel * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));

    private const ulong SeedA = 1790UL, SeedB = 1791UL;

    // ---- 盤面 A（手を入れない 6 四半期）：四半期ごとに試験が R・州の基数・V・実際の土地税を数える ----
    private sealed class RunA
    {
        public string Dir = "";
        public readonly Dictionary<(int Q, string N), double> R = new(), Paid = new();
        public readonly Dictionary<(int Q, string N, string Reg), double> RegBase = new(), V = new();
        public readonly List<string> PureMismatch = new();
        public double ExportsMax;
    }
    private static readonly Lazy<RunA> A = new(() =>
    {
        var a = new RunA();
        var (dir, _) = SubstInstrumentTests.Run(SeedA, 6, after: (sim, q) =>
        {
            foreach (var n in sim.Nations)
            {
                var w = n.World; if (w.Regions.Count == 0) continue;
                string nat = Instruments.NameOf(w);
                double R = 0;
                foreach (var r in w.Regions)
                {
                    double b = 0;
                    foreach (var p in r.Pops) b += Math.Max(0, p.LandRentPerMonth.Value);
                    R += b;
                    a.RegBase[(q, nat, r.Name)] = b;
                    a.V[(q, nat, r.Name)] = r.LastSubsistenceOutput * StapleP(r);
                }
                a.R[(q, nat)] = R;
                a.Paid[(q, nat)] = w.LastTaxLandPerMonth.Value;
                double pure = Instruments.LbR(w);
                if (!(Math.Abs(pure - R) <= 1e-12 * Math.Max(R, 1e-300))) a.PureMismatch.Add($"q{q} {nat} LbR {pure:R} 試験 {R:R}");
                if (w.ExportsByGood is { } eg) foreach (var e in eg) a.ExportsMax = Math.Max(a.ExportsMax, e);
            }
        });
        a.Dir = dir;
        return a;
    });

    /// <summary>主食の値段（試験の式）：主食の BaseNeed で重みを付けた州の値段。</summary>
    private static double StapleP(Region r)
    {
        double num = 0, den = 0;
        for (int g = 0; g < Math.Min(r.Markets.Length, Catalog.Goods.Length); g++)
            if (Catalog.Goods[g].Tier == GoodTier.Staple) { num += Catalog.Goods[g].BaseNeed * r.Markets[g].Price; den += Catalog.Goods[g].BaseNeed; }
        return den > 0 ? num / den : double.NaN;
    }

    /// <summary>主食の RGO（IsRgo・主食・倒産でない・産出＞0）の産出で重みを付けた地代の取り分（エンジンの RentShareOf＝min(RentShare, FarmRentCap)）。無ければ nan。</summary>
    private static double StapleShare(World w, Region r)
    {
        double num = 0, den = 0;
        foreach (var s in r.Producers)
        {
            if (!s.IsRgo || s.Bankrupt || s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length || Catalog.Goods[s.GoodId].Tier != GoodTier.Staple || !(s.Output > 0)) continue;
            num += s.Output * EconomyStep.RentShareOf(s, r, w.Coef); den += s.Output;
        }
        return den > 0 ? num / den : double.NaN;
    }

    // ---- 盤面 B（5 四半期・最後の四半期 q4 の Quarter の直前に手を入れる）：期待は手を入れた後の状態から試験が組む ----
    private sealed class Expect
    {
        public double Claim, Exempt, Deg, ExtShare, ExtGross, R, ShortSize, ShortGoods;
        public int Carriers, Short, NoPrice, K1, K10, K100, K1000, KOver;
        public int ClosedRent, ClosedSoldierRent, NegRent, TitheShort, RShareInterior, VPos, AssessCount;
        public double AssessLand;
        public readonly Dictionary<string, (double Base, int Payers, double Claim, int Rgo, int Empty, int Absentee)> Reg = new();
        public string ZeroRegion = "", ZeroNation = "";
        public readonly List<string> PositiveRegions = new();
    }
    private sealed class RunB { public string Dir = ""; public string France = ""; public Expect E = new(); }
    private static readonly Lazy<RunB> B = new(() =>
    {
        var b = new RunB();
        var q0Base = new Dictionary<string, double>();
        var (dir, _) = SubstInstrumentTests.Run(SeedB, 5,
            after: (sim, q) =>
            {
                if (q == 0) foreach (var r in sim.Nations[0].World.Regions) { double s = 0; foreach (var p in r.Pops) s += Math.Max(0, p.LandRentPerMonth.Value); q0Base[r.Name] = s; }
                if (q == 4) Build(sim, b);
            },
            before: (sim, q) => { if (q == 4) Touch(sim, q0Base, b); });
        b.Dir = dir;
        return b;
    });

    /// <summary>q4 の Quarter の直前に盤面に手を入れる（仏＝政体・税率・閉鎖の兵士・負の地代・担い手の不足・持ち主の空の RGO／英＝基数 0 の州）。</summary>
    private static void Touch(MultiNationSim sim, Dictionary<string, double> q0Base, RunB b)
    {
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        b.France = Instruments.NameOf(fr);
        fr.Polity = Polity.Absolutist; fr.LandTax = 0.2; fr.TaxRate = new Share(0.2);
        var used = new HashSet<Pop>();
        // 閉鎖の兵士に地代
        foreach (var r in fr.Regions) { var s = r.Pops.FirstOrDefault(p => p.IsSoldier && !p.IsCriminal && p.Size > 1e-9); if (s is null) continue; s.Closed = true; s.LandRentPerMonth = new MoneyPerMonth(7.0); used.Add(s); break; }
        // 負の地代（閉鎖でない・犯罪者でない・地代＞0 の POP）
        foreach (var r in fr.Regions) { var s = r.Pops.FirstOrDefault(p => !p.Closed && !p.IsCriminal && !used.Contains(p) && p.LandRentPerMonth.Value > 0); if (s is null) continue; s.LandRentPerMonth = new MoneyPerMonth(-5.0); used.Add(s); break; }
        // 担い手の不足（大衆の担い手 4 つの前の月の所得を大きく・Size を 0.5 と 1500 に）
        int k = 0;
        foreach (var r in fr.Regions)
            foreach (var p in r.Pops)
            {
                if (k >= 4 || used.Contains(p) || p.IsCriminal || !p.IsCommoner || p.IsModernElite || !(p.SubsistenceBasis > 0) || !(p.Size > 1e-12)) continue;
                p.LastEarnedPerMonth = new MoneyPerMonth(1e7 * (k + 1));
                if (k == 0) p.Size = 0.5; else if (k == 1) p.Size = 1500;
                used.Add(p); k++;
            }
        // 持ち主の POP が空の RGO（閉鎖でない持ち主・上で使っていない POP）
        bool emptied = false;
        foreach (var r in fr.Regions)
        {
            foreach (var s in r.Producers)
            {
                if (!s.IsRgo || s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count) continue;
                var o = r.Pops[s.OwnerPopIndex];
                if (o.Closed || used.Contains(o) || !(o.Size > 1e-12)) continue;
                o.Size = 0; used.Add(o); emptied = true; break;
            }
            if (emptied) break;
        }
        // 英：q0 で基数が正の州 1 つの全ての POP の地代を 0
        foreach (var r in uk.Regions)
        {
            if (!(q0Base.TryGetValue(r.Name, out var v0) && v0 > 0)) continue;
            foreach (var p in r.Pops) p.LandRentPerMonth = new MoneyPerMonth(0);
            b.E.ZeroRegion = r.Name; b.E.ZeroNation = Instruments.NameOf(uk); break;
        }
        foreach (var r in uk.Regions) if (r.Name != b.E.ZeroRegion && r.Pops.Any(p => p.LandRentPerMonth.Value > 0)) b.E.PositiveRegions.Add(r.Name);
    }

    /// <summary>q4 の Quarter の直後（計器は書かない＝状態は Quarter の時と同じ）：仏の期待を試験の式で組む。</summary>
    private static void Build(MultiNationSim sim, RunB b)
    {
        var w = sim.Nations[1].World; var c = w.Coef; var e = b.E;
        Assert.Equal(0.0, c.PrivilegeFromCrownShare.Value);   // この試験の前提（王権の取り分の駆動は休眠）
        double disc = c.PrivilegeTaxDiscount.Value;
        e.Deg = disc > 0 ? disc : 0.0;   // 絶対王政に置いた
        double rate = w.LandTax;
        double titheRate = c.TitheRate.Value * (w.StateReligion switch { StateReligionLaw.InnerFreedom => c.InnerFreedomTithe, StateReligionLaw.ReligiousFreedom => c.ReligiousFreedomTithe, StateReligionLaw.StateAtheism => 0.0, _ => 1.0 });
        bool clergy = w.Regions.SelectMany(r => r.Pops).Where(p => p.ClassId == PopClass.Clergy && p.Size > 1e-9).Sum(p => p.Size) > 1e-9;
        double crown = Fiscal.CrownShareForPrivilege(w, c);
        var capR = w.Region;
        foreach (var r in w.Regions)
        {
            double sp = StapleP(r), rs = StapleShare(w, r), v = r.LastSubsistenceOutput * sp;
            if (v > 0) e.VPos++;
            if (rs > 0 && rs < 1) e.RShareInterior++;
            if (rate > 1e-9 && double.IsFinite(v)) { e.ExtGross += rate * v; if (double.IsFinite(rs)) e.ExtShare += rate * rs * v; }
            double rb = 0, rc = 0; int payers = 0;
            foreach (var p in r.Pops)
            {
                double rent = p.LandRentPerMonth.Value;
                rb += Math.Max(0, rent); e.R += Math.Max(0, rent);
                if (rent > 0) payers++;
                if (rent < 0) e.NegRent++;
                if (p.IsCriminal) continue;
                double priv = p.Closed && !p.IsSoldier ? 1.0 - e.Deg : 1.0;
                if (rent > 0 && p.Closed && !p.IsSoldier) e.ClosedRent++;
                if (rent > 0 && p.Closed && p.IsSoldier) e.ClosedSoldierRent++;
                if (rate > 1e-9 && rent > 0) { e.Claim += rate * rent * priv; rc += rate * rent * priv; e.Exempt += rate * rent * (1.0 - priv); }
                var asd = Fiscal.AssessPop(w, c, p, crown, null, w.TaxRate.Value, w.ProgressiveTax, w.PollTax, w.LandTax);
                e.AssessLand += asd.Land; if (asd.Land > 0) e.AssessCount++;
                if (!(p.SubsistenceBasis > 0) || !(p.Size > 1e-12)) continue;
                e.Carriers++;
                double poll = w.PollTax > 1e-9 && !p.Closed && !p.IsSoldier ? c.PollTaxRate * w.PollTax * p.Size : 0;
                double inc = w.TaxRate.Value * Math.Max(0, p.LastEarnedPerMonth.Value) * priv;
                double ti = titheRate > 1e-9 && clergy && !p.Closed && !p.IsSoldier && !p.IsModernElite && p.LastEarnedPerMonth.Value > 0 ? titheRate * p.LastEarnedPerMonth.Value : 0;
                double cl = poll + inc + ti, held = Math.Max(0, p.Savings.Value);
                if (!(cl > held)) continue;
                e.Short++; e.ShortSize += p.Size; if (ti > 0) e.TitheShort++;
                if (sp > 0 && double.IsFinite(sp)) e.ShortGoods += (cl - held) / sp; else e.NoPrice++;
                if (p.Size <= 1) e.K1++; else if (p.Size <= 10) e.K10++; else if (p.Size <= 100) e.K100++; else if (p.Size <= 1000) e.K1000++; else e.KOver++;
            }
            int rgo = 0, empty = 0, abs = 0;
            foreach (var s in r.Producers)
            {
                if (!s.IsRgo) continue;
                rgo++;
                if (s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count || r.Pops[s.OwnerPopIndex].Size <= 1e-12) empty++;
                if (r.Tier == RegionTier.Colonial && !ReferenceEquals(r, capR) && capR.InvestorPopIndex >= 0 && capR.InvestorPopIndex < capR.Pops.Count
                    && Colonial.ExportCrop(s.GoodId) && Array.IndexOf(r.Resources, s.GoodId) >= 0) abs++;
            }
            e.Reg[r.Name] = (rb, payers, rc, rgo, empty, abs);
        }
    }

    private static List<Dictionary<string, string>> Lb(string dir, ulong seed) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"landbase_s{seed}.tsv"), null);
    private static List<Dictionary<string, string>> Lbr(string dir, ulong seed) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"landbasereg_s{seed}.tsv"), null);
    private static Dictionary<string, string> FranceRow(RunB b) => Lb(b.Dir, SeedB).Single(r => r["quarter"] == "4" && r["nation"] == b.France);

    /// <summary>T1：見出しの列の名と数（期待の列の名は試験の中にリテラル）。ファイルの先頭の行も同じ。</summary>
    [Fact]
    public void T1_見出しは列の名と数のとおり()
    {
        string[] nat = ["quarter", "year", "nation", "privExemptDegreeShare", "landClaimPerMonth", "landClaimExemptPerMonth", "landTaxPaidPerMonth", "landPaidOverClaim",
            "landClaimExtShareUpperPerMonth", "landClaimExtGrossUpperPerMonth", "baseRPerMonth", "baseRCheckRelDiff", "baseRCheckBreakFlag", "baseLag4Ratio", "baseOverFirstSeenRatio",
            "fpCarrierPops", "fpShortPops", "fpShortSizeK", "fpShortfallGoodsPerMonth", "fpShortNoPricePops",
            "fpShortPopsSizeK1", "fpShortPopsSizeK10", "fpShortPopsSizeK100", "fpShortPopsSizeK1000", "fpShortPopsSizeKOver"];
        string[] reg = ["quarter", "year", "nation", "region", "tier", "regionBasePerMonth", "regionPayersWithRent", "regionStapleRentShare", "regionSubValuePerMonth", "regionClaimPerMonth",
            "regionBaseLag4Ratio", "regionZeroBaseFlag", "regionRgoCount", "regionRgoOwnerEmpty", "regionAbsenteeRgoCount"];
        Assert.Equal(25, nat.Length); Assert.Equal(15, reg.Length);
        Assert.Equal(nat, Instruments.LandbaseHeaderText.Split('\t'));
        Assert.Equal(reg, Instruments.LandbaseRegHeaderText.Split('\t'));
        var a = A.Value;
        Assert.Equal(string.Join('\t', nat), System.IO.File.ReadLines(System.IO.Path.Combine(a.Dir, $"landbase_s{SeedA}.tsv")).First());
        Assert.Equal(string.Join('\t', reg), System.IO.File.ReadLines(System.IO.Path.Combine(a.Dir, $"landbasereg_s{SeedA}.tsv")).First());
    }

    /// <summary>T2：請求と免税（仏を絶対王政・土地税 0.2・閉鎖の兵士に地代）。試験の式とエンジンの査定（AssessPop の Land の和）に合う。</summary>
    [Fact]
    public void T2_土地税の請求と免税は試験の式とエンジンの査定に合う()
    {
        var b = B.Value; var e = b.E; var row = FranceRow(b);
        Assert.True(e.ClosedRent > 0, "前提が崩れた：絶対王政の国に閉鎖かつ兵士でない POP で地代＞0 が無い＝免税を落とす突然変異を拾えない");
        Assert.True(e.ClosedSoldierRent > 0, "前提が崩れた：閉鎖の兵士で地代＞0 が無い＝兵士にも免税を掛ける誤りを拾えない");
        Assert.True(e.Exempt > 0, "免税で抜けた額が 0");
        Assert.Equal(e.Deg, D(row, "privExemptDegreeShare"), 12);
        Assert.True(Near(D(row, "landClaimPerMonth"), e.Claim, 1e-8), $"請求が試験の式と違う {D(row, "landClaimPerMonth")} ≠ {e.Claim}");
        Assert.True(Near(D(row, "landClaimExemptPerMonth"), e.Exempt, 1e-8), $"免税で抜けた額が違う {D(row, "landClaimExemptPerMonth")} ≠ {e.Exempt}");
        Assert.True(e.AssessCount > 0);
        Assert.True(Near(D(row, "landClaimPerMonth"), e.AssessLand, 1e-8), $"請求がエンジンの査定 AssessPop の土地税の和と違う {D(row, "landClaimPerMonth")} ≠ {e.AssessLand}");
    }

    /// <summary>T3：負の地代は請求と R に入らない。</summary>
    [Fact]
    public void T3_負の地代は請求とRに入らない()
    {
        var b = B.Value; var row = FranceRow(b);
        Assert.True(b.E.NegRent > 0, "前提が崩れた：負の地代の POP が無い");
        Assert.True(Near(D(row, "baseRPerMonth"), b.E.R, 1e-8), $"R が Σ max(0, 地代) と違う {D(row, "baseRPerMonth")} ≠ {b.E.R}");
        Assert.True(Near(D(row, "landClaimPerMonth"), b.E.Claim, 1e-8));
    }

    /// <summary>T4：実際に取れた土地税＝World.LastTaxLandPerMonth（全ての行）。</summary>
    [Fact]
    public void T4_実際に取れた土地税はエンジンの値()
    {
        var a = A.Value; var rows = Lb(a.Dir, SeedA);
        Assert.Equal(a.Paid.Count, rows.Count);
        int pos = 0;
        foreach (var r in rows)
        {
            double want = a.Paid[(int.Parse(r["quarter"], Inv), r["nation"])];
            Assert.True(Near(D(r, "landTaxPaidPerMonth"), want, 1e-8), $"q{r["quarter"]} {r["nation"]} {D(r, "landTaxPaidPerMonth")} ≠ {want}");
            if (want > 0) pos++;
            double claim = D(r, "landClaimPerMonth");
            Assert.Equal(claim > 0, !double.IsNaN(D(r, "landPaidOverClaim")));
        }
        Assert.True(pos > 0, "前提が崩れた：取れた土地税が全て 0");
    }

    /// <summary>T5：遅れの比（国と州）。q0〜q3 は nan・q4 は R(q0)÷R(q4)・q5 は R(q1)÷R(q5)。最初の比は q0 で 1・その後は R(q0)÷R(q)。</summary>
    [Fact]
    public void T5_遅れの比と最初の比は試験が数えた基数の比()
    {
        var a = A.Value;
        int checkedLag = 0, checkedReg = 0;
        foreach (var r in Lb(a.Dir, SeedA))
        {
            int q = int.Parse(r["quarter"], Inv); string n = r["nation"];
            double now = a.R[(q, n)], lag = D(r, "baseLag4Ratio"), first = D(r, "baseOverFirstSeenRatio");
            if (!(now > 0)) { Assert.True(double.IsNaN(lag) && double.IsNaN(first)); continue; }
            if (q < 4) Assert.True(double.IsNaN(lag), $"q{q} の遅れの比が nan でない {lag}");
            else { Assert.True(Near(lag, a.R[(q - 4, n)] / now, 1e-8), $"q{q} {n} 遅れの比 {lag} ≠ {a.R[(q - 4, n)] / now}"); checkedLag++; }
            Assert.True(Near(first, a.R[(0, n)] / now, 1e-8), $"q{q} {n} 最初の比 {first}");
        }
        foreach (var r in Lbr(a.Dir, SeedA))
        {
            int q = int.Parse(r["quarter"], Inv);
            double now = a.RegBase[(q, r["nation"], r["region"])], lag = D(r, "regionBaseLag4Ratio");
            if (!(now > 0) || q < 4) { Assert.True(double.IsNaN(lag)); continue; }
            Assert.True(Near(lag, a.RegBase[(q - 4, r["nation"], r["region"])] / now, 1e-8), $"州 q{q} {r["region"]} {lag}");
            checkedReg++;
        }
        Assert.True(checkedLag >= 2 && checkedReg > 0, $"前提が崩れた：遅れの比を確かめた行が少ない 国 {checkedLag}・州 {checkedReg}");
    }

    /// <summary>T6：今の基数 0・4 四半期前が正の州は regionZeroBaseFlag＝1・今も正の州は 0。</summary>
    [Fact]
    public void T6_基数が0になった州は旗が1()
    {
        var b = B.Value;
        Assert.True(b.E.ZeroRegion.Length > 0 && b.E.PositiveRegions.Count > 0, "前提が崩れた：q0 で基数が正の州が無い");
        var rows = Lbr(b.Dir, SeedB).Where(r => r["quarter"] == "4" && r["nation"] == b.E.ZeroNation).ToList();
        Assert.Equal("1", rows.Single(r => r["region"] == b.E.ZeroRegion)["regionZeroBaseFlag"]);
        Assert.Equal(0.0, D(rows.Single(r => r["region"] == b.E.ZeroRegion), "regionBasePerMonth"));
        foreach (var name in b.E.PositiveRegions) Assert.Equal("0", rows.Single(r => r["region"] == name)["regionZeroBaseFlag"]);
    }

    /// <summary>T7：計器あり・なしの同じ種の 2 つのシムで、各四半期の Quarter の直後に全ての POP の地代・貯蓄と国庫・実際の土地税がビットで同じ。</summary>
    [Fact]
    public void T7_計器を出しても地代と貯蓄と国庫は変わらない()
    {
        static List<long> Bits(MultiNationSim sim)
        {
            var l = new List<long>();
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                l.Add(BitConverter.DoubleToInt64Bits(w.Treasury.Value)); l.Add(BitConverter.DoubleToInt64Bits(w.LastTaxLandPerMonth.Value));
                foreach (var r in w.Regions) foreach (var p in r.Pops) { l.Add(BitConverter.DoubleToInt64Bits(p.LandRentPerMonth.Value)); l.Add(BitConverter.DoubleToInt64Bits(p.Savings.Value)); }
            }
            return l;
        }
        const ulong seed = 1792UL; const int quarters = 3;
        var plain = new List<List<long>>();
        var sim0 = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        for (int q = 0; q < quarters; q++) { sim0.StepQuarter(); plain.Add(Bits(sim0)); }
        var withIns = new List<List<long>>();
        SubstInstrumentTests.Run(seed, quarters, after: (sim, q) => withIns.Add(Bits(sim)));
        Assert.Equal(quarters, withIns.Count);
        for (int q = 0; q < quarters; q++) Assert.True(plain[q].SequenceEqual(withIns[q]), $"q{q} 計器の有無で地代・貯蓄・国庫・土地税のビットが違う");
    }

    /// <summary>T8：R の一致＝計器の純関数 LbR が試験の和と double で 1e-12・baseRPerMonth が試験の和と cornint_s の rentDomTotalPerMonth に合う・照合の旗が全ての行で 0。</summary>
    [Fact]
    public void T8_RはcornintのrentDomTotalと同じ集合()
    {
        var a = A.Value;
        Assert.True(a.ExportsMax > 0, "前提が崩れた：輸出額が正の財が無い＝輸出の地代を R に足す突然変異を拾えない");
        Assert.True(a.PureMismatch.Count == 0, string.Join("\n", a.PureMismatch));
        var corn = SubstInstrumentTests.Rows(System.IO.Path.Combine(a.Dir, $"cornint_s{SeedA}.tsv"), null);
        var rows = Lb(a.Dir, SeedA);
        Assert.Equal(corn.Count, rows.Count);
        int pos = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; var cr = corn[i];
            Assert.Equal(cr["quarter"], r["quarter"]); Assert.Equal(cr["nation"], r["nation"]);
            double want = a.R[(int.Parse(r["quarter"], Inv), r["nation"])];
            Assert.True(Near(D(r, "baseRPerMonth"), want, 1e-8), $"R が試験の和と違う {r["nation"]} q{r["quarter"]}");
            Assert.True(Near(D(r, "baseRPerMonth"), D(cr, "rentDomTotalPerMonth"), 1e-8), $"R が cornint_s の rentDomTotalPerMonth と違う {r["nation"]} q{r["quarter"]}");
            Assert.Equal("0", r["baseRCheckBreakFlag"]);
            if (want > 0) pos++;
        }
        Assert.True(pos > 0, "前提が崩れた：R が全て 0");
    }

    /// <summary>T9：照合の旗の閾値（人工のずれ）。1e-6 のずれは 1・1e-15 のずれは 0・NaN は 1。</summary>
    [Fact]
    public void T9_照合の旗は人工のずれを落とす()
    {
        Assert.Equal(1, Instruments.LbCheckFlag(1.0, 1.0 + 1e-6));
        Assert.Equal(1, Instruments.LbCheckFlag(1e6, 1e6 * (1 + 1e-10)));
        Assert.Equal(0, Instruments.LbCheckFlag(1.0, 1.0 + 1e-15));
        Assert.Equal(0, Instruments.LbCheckFlag(0.0, 0.0));
        Assert.Equal(1, Instruments.LbCheckFlag(1.0, double.NaN));
    }

    /// <summary>T10：V_州は subst_s の engineSubOutputPerMonth×stapleNeedWeightedPrice と、試験が州の欄から組んだ値に合う（subst_s に行がある州との交わり）。</summary>
    [Fact]
    public void T10_自給の値はsubst_sと州の欄に合う()
    {
        var a = A.Value;
        var sub = SubstInstrumentTests.Rows(System.IO.Path.Combine(a.Dir, $"subst_s{SeedA}.tsv"), null)
            .ToDictionary(r => (r["quarter"], r["nation"], r["region"]));
        int both = 0, pos = 0;
        foreach (var r in Lbr(a.Dir, SeedA))
        {
            double v = D(r, "regionSubValuePerMonth");
            double want = a.V[(int.Parse(r["quarter"], Inv), r["nation"], r["region"])];
            Assert.Equal(double.IsNaN(want), double.IsNaN(v));
            if (!double.IsNaN(want)) Assert.True(Near(v, want, 1e-8), $"V が州の欄の値と違う {r["region"]} q{r["quarter"]} {v} ≠ {want}");
            if (v > 0) pos++;
            if (!sub.TryGetValue((r["quarter"], r["nation"], r["region"]), out var s)) continue;
            double sv = D(s, "engineSubOutputPerMonth") * D(s, "stapleNeedWeightedPrice");
            if (double.IsNaN(sv)) continue;
            Assert.True(Near(v, sv, 1e-8), $"V が subst_s と違う {r["region"]} q{r["quarter"]} {v} ≠ {sv}");
            both++;
        }
        Assert.True(both > 0 && pos > 0, $"前提が崩れた：subst_s と重なる州 {both}・V＞0 の州 {pos}");
    }

    /// <summary>T11：自給へ広げた請求の上界（取り分と総額）が試験の式に合う（仏・土地税 0.2）。</summary>
    [Fact]
    public void T11_自給へ広げた請求の上界は試験の式()
    {
        var b = B.Value; var e = b.E; var row = FranceRow(b);
        Assert.True(e.RShareInterior > 0 && e.VPos > 0, $"前提が崩れた：地代の取り分が (0,1) の州 {e.RShareInterior}・V＞0 の州 {e.VPos}");
        Assert.True(e.ExtShare > 0 && e.ExtGross > e.ExtShare);
        Assert.True(Near(D(row, "landClaimExtShareUpperPerMonth"), e.ExtShare, 1e-8), $"取り分の上界 {D(row, "landClaimExtShareUpperPerMonth")} ≠ {e.ExtShare}");
        Assert.True(Near(D(row, "landClaimExtGrossUpperPerMonth"), e.ExtGross, 1e-8), $"総額の上界 {D(row, "landClaimExtGrossUpperPerMonth")} ≠ {e.ExtGross}");
    }

    /// <summary>T12：担い手の請求′と手持ちの差（数・人数・主食の単位の不足・値段の無い数・Size の帯）が試験の式に合う。</summary>
    [Fact]
    public void T12_担い手の不足は試験の式()
    {
        var b = B.Value; var e = b.E; var row = FranceRow(b);
        Assert.True(e.Short >= 2 && e.TitheShort > 0 && e.K1 > 0 && e.KOver > 0, $"前提が崩れた：不足 {e.Short}・十分の一税つき {e.TitheShort}・K1 {e.K1}・KOver {e.KOver}");
        Assert.Equal(e.Carriers.ToString(Inv), row["fpCarrierPops"]);
        Assert.Equal(e.Short.ToString(Inv), row["fpShortPops"]);
        Assert.Equal(e.NoPrice.ToString(Inv), row["fpShortNoPricePops"]);
        Assert.True(Near(D(row, "fpShortSizeK"), e.ShortSize, 1e-8));
        Assert.True(Near(D(row, "fpShortfallGoodsPerMonth"), e.ShortGoods, 1e-8), $"不足の量 {D(row, "fpShortfallGoodsPerMonth")} ≠ {e.ShortGoods}");
        Assert.Equal(new[] { e.K1, e.K10, e.K100, e.K1000, e.KOver }.Select(x => x.ToString(Inv)),
            new[] { "fpShortPopsSizeK1", "fpShortPopsSizeK10", "fpShortPopsSizeK100", "fpShortPopsSizeK1000", "fpShortPopsSizeKOver" }.Select(cn => row[cn]));
    }

    /// <summary>T13：州の列（基数・地代のある POP の数・州の請求・RGO の数・持ち主の空の RGO・不在地主の RGO）が試験の式に合い、州の請求の和＝国の請求。</summary>
    [Fact]
    public void T13_州の列は試験の式で州の請求の和は国の請求()
    {
        var b = B.Value; var e = b.E; var row = FranceRow(b);
        var rows = Lbr(b.Dir, SeedB).Where(r => r["quarter"] == "4" && r["nation"] == b.France).ToList();
        Assert.Equal(e.Reg.Count, rows.Count);
        double sum = 0; int empty = 0;
        foreach (var r in rows)
        {
            var x = e.Reg[r["region"]];
            Assert.True(Near(D(r, "regionBasePerMonth"), x.Base, 1e-8), $"{r["region"]} 基数");
            Assert.Equal(x.Payers.ToString(Inv), r["regionPayersWithRent"]);
            Assert.True(Near(D(r, "regionClaimPerMonth"), x.Claim, 1e-8), $"{r["region"]} 州の請求 {D(r, "regionClaimPerMonth")} ≠ {x.Claim}");
            Assert.Equal(x.Rgo.ToString(Inv), r["regionRgoCount"]);
            Assert.Equal(x.Empty.ToString(Inv), r["regionRgoOwnerEmpty"]);
            Assert.Equal(x.Absentee.ToString(Inv), r["regionAbsenteeRgoCount"]);
            sum += D(r, "regionClaimPerMonth"); empty += x.Empty;
        }
        Assert.True(empty > 0, "前提が崩れた：持ち主の空の RGO が無い");
        Assert.True(Near(sum, D(row, "landClaimPerMonth"), 1e-8), $"州の請求の和 {sum} ≠ 国の請求 {D(row, "landClaimPerMonth")}");
        Assert.True(e.ClosedRent > 0, "前提が崩れた：免税の掛かる POP が無い＝州の請求に免税を掛けない誤りを拾えない");
    }

}
