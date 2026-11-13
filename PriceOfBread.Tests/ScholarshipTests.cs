using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(590)(597) 奨学金の法：旗 <c>ScholarshipTopStage</c>（既定 0）。教育費の軸（EducationSpending）の**最上段**の国だけが、高等教育の授業料の一部を国庫が負担する
/// （買い手の値段の楔＝健康保険 #3 の insure と同型・年金が社会保障法の最上段であるのと同じ型）。(590) の世界の旗 ScholarshipLaw（全ての段に比例）は物理削除した。
/// 事前登録 measurements/sch2_597_20261027_prereg.txt [3] の J1〜J6。
/// 期待値は盤面の別の出所から作る：負担分＝割合 s × 売れた量（市場の LastSold）× 値段（市場の Price）。買い手が払う分は 買い手の PaidPerMonth の差から（コードの式を呼ばない）。
/// 割合は別の状態を持たず Region.CompulsoryEduFunding（教育費の軸の実効の率）から、段は Region.EducationSpending（国の実効の率の月次の複製）から EconomyStep.ScholarshipCover が導く＝試験は両方に直に置く。
/// 段の境界の期待値は手計算：実効の率 x → 0.05 の格子へ丸め → ×3（最上段の錨は 1.0）→ 四捨五入（半分は上）。0.80→2.4→段 2・0.85→2.55→段 3。
/// ⚠高等教育は既定で休眠（HigherEduNeedPerEducated=0・ResearcherFrac=0）＝試験は必要量を立て、大学の生産者の代わりに市場の在庫を厚くした盤面で 1 か月だけ回す。</summary>
public class ScholarshipTests
{
    private const double S = 0.4;

    /// <summary>英の開幕を 1 か月だけ回す。買い手は 1 人（他の POP は識字 0 ＝高等教育の必要量 0）・貯蓄は厚く予算は拘束しない・在庫は厚く配給も拘束しない。
    /// funding は Region.CompulsoryEduFunding に直に置く（月次の設定は Simulation が毎月書く＝1 か月だけ回す試験は直に置く）。law は ScholarshipTopStage の旗。edu は Region.EducationSpending（国の実効の率の複製・既定 1.0＝最上段）。
    /// buyerSavings は買い手の貯蓄（既定は厚い＝予算は拘束しない）。poorOthers は買い手以外の貯蓄を 0 にする（義務教育の国庫購入を見る盤面）。</summary>
    internal static (Region Region, World World, Pop Buyer) OneMonth(bool law, double funding, bool poorOthers = false, double buyerSavings = 1e12, double edu = 1.0)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.HigherEduNeedPerEducated = 0.5;   // 高等教育の必要量を立てる（既定 0＝休眠）
        var r = w.Region;
        r.CompulsoryEduFunding = funding;
        r.EducationSpending = edu;
        w.Coef.ScholarshipTopStage = law ? 1 : 0;
        int h = Catalog.HigherEducationId;
        // 大学の代わりの生産者（役務は在庫を持ち越さない＝供給は今月の産出）。産出は厚く入力なし・労働と持ち主は既存の POP の添字（試験の盤面のため）
        int lab = Math.Max(0, r.TeacherPopIndex);
        r.Producers.Add(new Producer { GoodId = h, Productivity = 1e6, Output = 1e9, MarginalCost = 0.4, LaborPopIndex = lab, OwnerPopIndex = lab, RentShare = default, Inputs = [] });
        Pop? buyer = null;
        foreach (var p in r.Pops)
        {
            if (buyer is null && !p.IsSoldier && p.Size > 0 && p.Literacy > 0) { buyer = p; continue; }
            p.Literacy = 0; p.LastEducationFill = 0;   // 必要量 0（買い手を 1 人にする）
        }
        Assert.NotNull(buyer);
        foreach (var p in w.AllPops()) p.SetSavings(ReferenceEquals(p, buyer) ? buyerSavings : (poorOthers ? 0.0 : 1e12));   // 義務教育の国庫購入を見る盤面では他の POP は貯蓄 0（民間が教育を買い切ると国庫の買う分が 0）
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        return (r, w, buyer!);
    }

    [Fact]
    public void 旗の既定は0_高等教育の楔は旗1かつ最上段かつ財源率が正のとき正_他の財と割合0では0()
    {
        Assert.Equal(0, new Coefficients().ScholarshipTopStage);
        var c = new Coefficients();
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region;
        int h = Catalog.HigherEducationId;
        r.CompulsoryEduFunding = S; r.EducationSpending = 1.0;
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure);          // 旗 0＝最上段で財源率が正でも楔なし
        c.ScholarshipTopStage = 1;
        r.CompulsoryEduFunding = 0.0;
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure);          // 財源率 0
        r.CompulsoryEduFunding = S;
        Assert.Equal(S, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure, 12);
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, Catalog.EducationId, true, null, 0).insure);          // 義務教育の財には楔を掛けない
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, Catalog.BreadId, false, null, 0).insure);
        r.CompulsoryEduFunding = 2.0;
        Assert.Equal(EconomyStep.InsuranceCoverageCap, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure, 12);   // 0 除算の守りの上限
        r.CompulsoryEduFunding = double.NaN;
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure);          // NaN は 0 へ落ちる（否定形の fail-closed）
        r.CompulsoryEduFunding = S; r.EducationSpending = double.NaN;
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure);          // 段が NaN＝段 0 の側（fail-closed）
    }

    /// <summary>J2 段の門：段 0〜2 は財源率が正でも楔 0・国庫の負担 0（買い手は全額）／段 3 でだけ正。境界は手計算の表（TaxStage を呼ばない）。
    /// 突然変異＝門を外す（段に依らず割合を返す）・門を >= 2 に緩める（実効 0.65・0.80 が段 2 のまま正になって赤）。</summary>
    [Fact]
    public void J2_段の門_段0から2は補助0_段3でだけ正_境界は手計算()
    {
        var c = new Coefficients { ScholarshipTopStage = 1 };
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region;
        int h = Catalog.HigherEducationId;
        r.CompulsoryEduFunding = S;
        // (実効の率, 段) ― 格子 0.05 へ丸め→×3→四捨五入（半分は上）：0.30→0.9→1／0.35→1.05→1／0.50→1.5→2／0.65→1.95→2／0.80→2.4→2／0.85→2.55→3
        foreach (var (x, stage) in new[] { (0.0, 0), (0.30, 1), (0.35, 1), (0.50, 2), (0.65, 2), (0.80, 2), (0.85, 3), (0.95, 3), (1.0, 3) })
        {
            r.EducationSpending = x;
            double expect = stage == 3 ? S : 0.0;
            Assert.Equal(expect, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure, 12);
            Assert.Equal(expect, EconomyStep.ScholarshipCover(r, c), 12);
        }
        // 決済まで通す：段 2（実効 0.80）は国庫の負担 0・買い手は授業料の全額／段 3（0.85）は国庫が割合×授業料
        var (lo, _, bLo) = OneMonth(true, S, edu: 0.80);
        var (hi, _, bHi) = OneMonth(true, S, edu: 0.85);
        var (off, _, bOff) = OneMonth(false, S, edu: 0.85);
        Assert.Equal(0.0, lo.StateSpent[(int)TreasuryCat.Education]);
        double qLo = lo.Markets[h].LastSold;
        Assert.True(qLo > 0, "前提が崩れた：段 2 の盤面で高等教育が買われていない");
        Assert.Equal(bOff.PaidPerMonth.Value, bLo.PaidPerMonth.Value, 9);                 // 段 2 の旗 1 は旗 0 と同じ支払い（楔なし）
        Assert.Equal(S * hi.Markets[h].LastSold * hi.Markets[h].Price, hi.StateSpent[(int)TreasuryCat.Education], 9);
        Assert.Equal(0.0, off.StateSpent[(int)TreasuryCat.Education]);
        Assert.True(bHi.PaidPerMonth.Value != bOff.PaidPerMonth.Value, "段 3 でも買い手の支払いが変わらない＝楔が決済に届いていない");
    }

    [Fact]
    public void J1_国庫の負担は割合と授業料の積_買い手の自己負担は残りの分()
    {
        var (a, wa, ba) = OneMonth(false, S);
        var (b, wb, bb) = OneMonth(true, S);
        int h = Catalog.HigherEducationId;
        double priceA = a.Markets[h].Price, priceB = b.Markets[h].Price;
        Assert.Equal(priceA, priceB);                                   // 値段は月内で動かない（同じ盤面）
        double qa = a.Markets[h].LastSold, qb = b.Markets[h].LastSold;
        Assert.True(qa > 0, "前提が崩れた：法なしの盤面で高等教育が 1 単位も買われていない（必要量が立っていない）");
        Assert.True(qb >= qa, $"楔で値段が下がったのに買う量が減った（{qa} → {qb}）");
        Assert.Equal(0.0, a.StateSpent[(int)TreasuryCat.Education]);                  // 旗 0＝国庫の負担なし
        // 国庫の負担＝割合×売れた量×値段
        Assert.Equal(0.0, a.StateSpent[(int)TreasuryCat.Education]);
        Assert.Equal(S * qb * priceB, b.StateSpent[(int)TreasuryCat.Education], 9);
        Assert.Equal(S * qb * priceB, b.CumEducationCost - a.CumEducationCost, 9);   // 診断の累積（UI の教育費の行）にも国庫の負担分が載る
        // 買い手の自己負担（PaidPerMonth の差＝他の財の支払いは同じ）＝(1−s)×授業料 − 法なしの授業料
        double dPaid = bb.PaidPerMonth.Value - ba.PaidPerMonth.Value;
        Assert.Equal((1.0 - S) * qb * priceB - qa * priceA, dPaid, 9);
        Assert.Equal(0.0, a.StateSpent[(int)TreasuryCat.Modernization]);   // 補助は近代化の区分ではなく教育の区分（翌月の請求の見込みが読む）
        Assert.Equal(0.0, b.StateSpent[(int)TreasuryCat.Modernization]);
    }

    /// <summary>予算が拘束するとき、楔で自己負担が下がると買える量が厳密に増える（狙いそのもの）。買い手の貯蓄を桁で振り、全ての水準で qb≧qa・どこかで qb＞qa。
    /// 貯蓄が厚い水準（予算が拘束しない）では等しい＝等号でも通る試験にしない（拘束する水準が少なくとも 1 つあることを前提として確かめる）。</summary>
    [Fact]
    public void J1_予算が拘束するとき_楔で買える量が厳密に増える()
    {
        int h = Catalog.HigherEducationId;
        bool strict = false;
        foreach (double sv in new[] { 1e-2, 1e-1, 1.0, 1e1, 1e2, 1e3, 1e4, 1e12 })
        {
            double qa = OneMonth(false, S, buyerSavings: sv).Region.Markets[h].LastSold;
            double qb = OneMonth(true, S, buyerSavings: sv).Region.Markets[h].LastSold;
            Assert.True(qb >= qa - 1e-12, $"貯蓄 {sv}：楔で買う量が減った（{qa} → {qb}）");
            if (qb > qa * (1 + 1e-9) + 1e-12) strict = true;
        }
        Assert.True(strict, "どの貯蓄の水準でも買える量が増えない＝楔が需要に届いていない（予算が拘束する水準が盤面に無い可能性もある）");
    }

    [Fact]
    public void J1_国庫の区分は教育_義務教育の購入と足し算になる()
    {
        // 義務教育（教育財）の国庫購入と同じ区分に載る＝翌月の請求 regrow×StateSpent[Education] が両方を含む
        var (b0, _, _) = OneMonth(false, 0.5, poorOthers: true);
        var (b1, _, _) = OneMonth(true, 0.5, poorOthers: true);
        int h = Catalog.HigherEducationId;
        double sub = 0.5 * b1.Markets[h].LastSold * b1.Markets[h].Price;
        Assert.True(b0.StateSpent[(int)TreasuryCat.Education] > 0, "前提が崩れた：義務教育の購入が無い");
        Assert.Equal(b0.StateSpent[(int)TreasuryCat.Education] + sub, b1.StateSpent[(int)TreasuryCat.Education], 6);
    }

    [Fact]
    public void J2_請求の見込みの行は教育の区分を読む_補助を別の区分へ書くと請求が立たない()
    {
        string src = File.ReadAllText(FindSimCore("Simulation.cs"));
        Assert.Matches(new Regex(@"regrow\s*\*\s*\(\s*Math\.Max\(0,\s*r\.StateSpent\[\(int\)TreasuryCat\.Education\]\)"), src);
        Assert.Matches(new Regex(@"CompulsoryEduFunding \*= payRate;"), src);   // 割合は義務教育の財源率そのもの（別の状態を持たない）＝支払い率 r＜1 の月は負担も比例して縮む
        // 国の教育費の率の複製は 1 行（月次の写し）で、支払い率を掛けない（割合の支払い率は CompulsoryEduFunding に畳まれている＝二重に掛けない）
        Assert.Single(Regex.Matches(src, @"r\.EducationSpending = World\.EducationSpending;"));
        Assert.DoesNotMatch(new Regex(@"r\.EducationSpending\s*\*="), src);
        string eco = File.ReadAllText(FindSimCore("EconomyStep.cs"));
        Assert.Contains("region.StateSpent[(int)TreasuryCat.Education] += subsidy;", eco);
    }

    [Fact]
    public void J3_割合は別の状態を持たず唯一の関数が導く_旗の読み手は定義の場所だけ_AIの経路を持たない()
    {
        // 状態を貯めない（思想⑩）：Region に ScholarshipShare の欄が無い。割合を読む所は EconomyStep.ScholarshipCover の 1 本。
        Assert.Null(typeof(Region).GetField("ScholarshipShare"));
        Assert.Null(typeof(Coefficients).GetField("ScholarshipLaw"));   // (590) の世界の旗は物理削除した（奨学金の仕組みを 2 つ残さない）
        string root = Path.GetDirectoryName(FindSimCore("Simulation.cs"))!;
        var readers = new List<string>();
        foreach (var (full, rel) in GoodIdGuardTests.CsFiles())
        {
            if (!GoodIdGuardTests.IsEngineRel(rel)) continue;
            foreach (var line in File.ReadLines(full))
                if (line.Split("//")[0].Contains("ScholarshipTopStage")) readers.Add(Path.GetFileName(rel).StartsWith("Coefficients.") ? "Coefficients.cs" : Path.GetFileName(rel));   // (1106) 係数の部品は 1 つの持ち主
        }
        // 宣言（Coefficients）・唯一の関数（EconomyStep）・置き換え（World の eduPart）・表示の門（PlayText）の 4 つだけ。AI と別の経路（SelfPlay ほか）が旗を読んだら赤
        Assert.Equal(new[] { "Coefficients.cs", "EconomyStep.cs", "PlayText.cs", "World.cs" }, readers.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        // 旧の旗の名前がコードの行に戻っていない（コメントは墓標として許す）。割合の導出が国（プレイヤーか否か）を読まない
        foreach (var (full, rel) in GoodIdGuardTests.CsFiles())
        {
            if (GoodIdGuardTests.IsTestRel(rel)) continue;
            foreach (var line in File.ReadLines(full)) Assert.DoesNotContain("ScholarshipLaw", line.Split("//")[0]);
        }
        string eco = File.ReadAllText(FindSimCore("EconomyStep.cs"));
        int a = eco.IndexOf("internal static double ScholarshipCover", StringComparison.Ordinal);
        string body = eco[a..eco.IndexOf(";\n", eco.IndexOf("InsuranceCoverageCap) : 0", a, StringComparison.Ordinal), StringComparison.Ordinal)];
        Assert.DoesNotContain("IsPlayer", body);
        foreach (var f in new[] { "SelfPlay.cs", "Agents.cs", "Politics.cs", "Parliament.cs", "Government.cs" })
            Assert.DoesNotContain("ScholarshipCover", File.ReadAllText(Path.Combine(root, f)));
    }

    [Fact]
    public void 旗0は割合0_旗1は最上段のときだけ義務教育の財源率と同じ導出で_法の門を通る()
    {
        double SumShare(Simulation s) { double t = 0; foreach (var r in s.World.Regions) t += EconomyStep.ScholarshipCover(r, s.World.Coef); return t; }
        Simulation Board(int flag, EducationLaw law, double spending)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            sim.World.Coef.ScholarshipTopStage = flag;
            sim.World.EducationLaw = law; sim.World.Policy.EducationLaw = law;
            sim.World.EducationSpending = spending; sim.World.Policy.EducationSpendingTarget = spending;
            sim.Run(2);
            return sim;
        }
        Assert.Equal(0.0, SumShare(Board(0, EducationLaw.Public, 1.0)));            // 旗 0＝最上段でも法なし
        Assert.Equal(0.0, SumShare(Board(1, EducationLaw.None, 1.0)));              // 法の門：教育法なしの国は国庫が授業料を負担しない（義務教育の財源も 0）。★(811) 旗 ParishEduFunded の焼き込みで教区学校の国は財源を持つ＝この行は教育法なしへ付け替えた
        Assert.Equal(0.0, SumShare(Board(1, EducationLaw.Public, 0.0)));            // 段 0＝教育費なし
        Assert.Equal(0.0, SumShare(Board(1, EducationLaw.Public, 0.6)));            // 段 2＝財源率は正でも補助は出ない（(590) の比例の形との違い）
        Assert.Equal(0.0, SumShare(Board(1, EducationLaw.Public, 0.8)));            // 段 2 の上端（0.85 の手前）
        var mid = Board(1, EducationLaw.Public, 0.6);
        Assert.True(mid.World.Regions.Sum(r => r.CompulsoryEduFunding) > 0, "前提が崩れた：段 2 の盤面で義務教育の財源率が 0＝段の門が何も言っていない");
        var on = Board(1, EducationLaw.Public, 1.0);
        Assert.True(SumShare(on) > 0);
        foreach (var r in on.World.Regions)
        {
            Assert.Equal(1.0, r.EducationSpending);   // 国の実効の率の月次の複製が州に届いている
            Assert.Equal(Math.Min(r.CompulsoryEduFunding, EconomyStep.InsuranceCoverageCap), EconomyStep.ScholarshipCover(r, on.World.Coef), 12);   // 義務教育の財源率（法の門・支配度・支払い率を通した後）と同じ値（上限まで）
        }
    }

    /// <summary>J3(c) 支払い率 r＜1 の月、割合は義務教育の財源率と一致する（r を二重に掛けていない）。上限（0.95）より下の盤面（教育法 Public＝財源率は PublicEduFundingShare×実効の率×到達）で、
    /// 国庫を空にして請求を払い切れない月を作る。期待値は盤面の別の出所（係数・州の複製の率・支配度の到達・月の支払い率 FiscalFundingRatio）から手で組む。
    /// 突然変異＝ScholarshipCover に r を掛ける／上限を先に掛ける、のどちらでも赤。r＜1 になっていない盤面は前提の失敗として赤にする（空振りの番人にしない）。</summary>
    [Fact]
    public void J3c_支払い率が1未満の月_割合は財源率と一致し_rを二重に掛けない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.Coef.ScholarshipTopStage = 1;
        w.EducationLaw = EducationLaw.Public; w.Policy.EducationLaw = EducationLaw.Public;
        w.EducationSpending = 1.0; w.Policy.EducationSpendingTarget = 1.0;
        bool below = false;
        var sink = new List<Pop> { w.Region.Pops.First(p => !p.IsSoldier && p.Size > 0) };
        for (int d = 0; d < 4 * Calendar.DaysPerQuarter && !below; d++)
        {
            if (w.Treasury.Value > 0) World.Transfer((w, null), (w, sink, null), w.Treasury.Value);   // 国庫を貨幣保存を保ったまま空にする（SetTreasuryRaw は保存を破る）
            foreach (var rg in w.Regions) rg.HigherEduFunding = Math.Max(rg.HigherEduFunding, 1e7);   // 請求を払える額より大きくする（(1081) 旧は記念建造物の予約 MonumentBuildQueue＝消した。同型の予約〔請求の和に入り EconomyStep が無条件にゼロ化〕の高等教育の予約で代える＝この試験が見る義務教育の財源率とは別の欄）
            sim.StepDay();
            below = w.FiscalFundingRatio.Value < 0.999;
        }
        Assert.True(below, "前提が崩れた：国庫を空にしても支払い率が 1 未満の月が作れない（この番人は何も言っていない）");
        double ratio = w.FiscalFundingRatio.Value;
        int checkedRegions = 0;
        foreach (var r in w.Regions)
        {
            double reach = Politics.RuleReach(r);
            double expect = w.Coef.PublicEduFundingShare.Value * r.EducationSpending * reach * ratio;   // 財源率の手組み：係数×複製の率×到達×月の支払い率
            Assert.True(expect < EconomyStep.InsuranceCoverageCap, "前提が崩れた：上限より下の盤面でない");
            if (!(expect > 0)) continue;
            Assert.Equal(expect, r.CompulsoryEduFunding, 9);                       // 財源率に r は 1 回だけ畳まれている
            Assert.Equal(expect, EconomyStep.ScholarshipCover(r, w.Coef), 9);     // 割合は財源率そのもの（r を掛け直していない）
            checkedRegions++;
        }
        Assert.True(checkedRegions > 0, "前提が崩れた：財源率が正の州が無い");
    }

    [Fact]
    public void J5_旗1は近代化投資の高等教育の予約を立てない_旗0は旧のまま()
    {
        double Funding(int flag)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            for (int q = 0; q < 2; q++) sim.StepQuarter();   // 就学の実績（LastEducationFill）が立つまで回す
            var w = sim.World;
            w.Coef.ScholarshipTopStage = flag;
            w.Coef.ModernizationToHigherEdu = 0.5;
            w.SetTreasuryRaw(1e9);
            w.ModernizationFrac = 0.3;
            foreach (var r in w.Regions) r.HigherEduFunding = 0;
            w.ApplyModernizationInvestment();
            return w.Regions.Sum(r => r.HigherEduFunding);
        }
        Assert.True(Funding(0) > 0, "対照：旗 0 で旧の道が立たない（盤面に進学したい者が居ない＝この試験は何も言っていない）");
        Assert.Equal(0.0, Funding(1));
    }

    [Fact]
    public void J6_教育費の名乗りは旗0で旧の文字列_旗1で奨学金の実効を載せる()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        string old = $"教育費（予算の割合） {w.Policy.EducationSpendingTarget:P0}　実効 {w.EducationSpending:P0}";   // 旧の式を手で書き写した期待値
        Assert.Equal(old, PlayText.EducationSpendingText(w));
        w.Coef.ScholarshipTopStage = 1; w.Region.CompulsoryEduFunding = 0.25; w.Region.EducationSpending = 1.0;
        string on = PlayText.EducationSpendingText(w);
        Assert.StartsWith(old, on);
        Assert.Contains("奨学金", on);
        Assert.Contains((0.25).ToString("P0"), on);
        w.Region.EducationSpending = 0.6;   // 段 2＝奨学金は 0%（旗 1 のときは名乗りを出し、率は段の門を通った値）
        Assert.Contains("奨学金 " + (0.0).ToString("P0"), PlayText.EducationSpendingText(w));
    }

    /// <summary>月次決算（MonthSettle→CheckInvariants＝貨幣保存の検査）を通して回す：補助が実際に流れる盤面（法あり・必要量あり・大学の生産者あり・SetTreasuryRaw なし）で例外が出ず、
    /// 割合が正・買い手の充足が正・国庫の減りが旗 0 の同じ盤面より大きい（補助が国庫から出た）。突然変異 M5（買い手の支払いを減らさない）は保存を破る or 国庫の差が 0 になって赤。</summary>
    [Fact]
    public void J2_月次決算を通して回す_保存の検査が通り_補助が国庫から出る()
    {
        (double treasury, double share, double fill) Run(int flag)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World;
            w.Coef.ScholarshipTopStage = flag; w.Coef.HigherEduNeedPerEducated = 0.5;
            w.EducationLaw = EducationLaw.Public; w.Policy.EducationLaw = EducationLaw.Public;
            w.EducationSpending = 1.0; w.Policy.EducationSpendingTarget = 1.0;
            var r = w.Region; int h = Catalog.HigherEducationId, lab = Math.Max(0, r.TeacherPopIndex);
            r.Producers.Add(new Producer { GoodId = h, Productivity = 1e6, Output = 1e9, MarginalCost = 0.4, LaborPopIndex = lab, OwnerPopIndex = lab, RentShare = default, Inputs = [] });
            sim.Run(1);   // 1 四半期＝3 回の月次決算（CheckInvariants が毎月走る＝例外が出ないこと自体が貨幣保存の確かめ）
            double share = 0, fill = 0;
            foreach (var rg in w.Regions) { share += EconomyStep.ScholarshipCover(rg, w.Coef); foreach (var p in rg.Pops) fill += p.HigherEduFill * p.Size; }
            return (w.Treasury.Value, share, fill);
        }
        var off = Run(0); var on = Run(1);
        Assert.Equal(0.0, off.share);
        Assert.True(on.share > 0, "前提が崩れた：旗 1・Public・最上段で割合が立たない");
        Assert.True(on.fill > 0, "前提が崩れた：高等教育の買い手が居ない（必要量か大学の盤面）");
        Assert.True(on.treasury < off.treasury, $"補助が国庫から出ていない（旗 1 の国庫 {on.treasury} ≧ 旗 0 の国庫 {off.treasury}）");
    }

    // ★(592) 出力の場所（--artifacts-path）の bin/PriceOfBread.SimCore をソースと取り違えないよう、他の番人と同じ GoodIdGuardTests.RepoRoot から引く
    private static string FindSimCore(string file) => Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", file);
}
