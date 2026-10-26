using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(590) 奨学金の法：旗 <c>ScholarshipLaw</c>（既定 0）。教育費の軸（EducationSpending）の実効の率に比例して、高等教育の授業料の一部を国庫が負担する
/// （買い手の値段の楔＝健康保険 #3 の insure と同型）。事前登録 measurements/sch_590_20261027_prereg.txt [3] の J1〜J3・J5・J6。
/// 期待値は盤面の別の出所から作る：負担分＝割合 s × 売れた量（市場の LastSold）× 値段（市場の Price）。買い手が払う分は 買い手の PaidPerMonth の差から（コードの式を呼ばない）。
/// ⚠高等教育は既定で休眠（HigherEduNeedPerEducated=0・ResearcherFrac=0）＝試験は必要量を立て、大学の生産者の代わりに市場の在庫を厚くした盤面で 1 か月だけ回す。</summary>
public class ScholarshipTests
{
    private const double S = 0.4;

    /// <summary>英の開幕を 1 か月だけ回す。買い手は 1 人（他の POP は識字 0 ＝高等教育の必要量 0）・貯蓄は厚く予算は拘束しない・在庫は厚く配給も拘束しない。
    /// share は Region.ScholarshipShare に直に置く（月次の設定は Simulation が毎月書く＝1 か月だけ回す試験は直に置く）。</summary>
    internal static (Region Region, World World, Pop Buyer) OneMonth(double share, double compulsory = 0.0)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.HigherEduNeedPerEducated = 0.5;   // 高等教育の必要量を立てる（既定 0＝休眠）
        var r = w.Region;
        r.CompulsoryEduFunding = compulsory;
        r.ScholarshipShare = share;
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
        foreach (var p in w.AllPops()) p.SetSavings(ReferenceEquals(p, buyer) ? 1e12 : (compulsory > 0 ? 0.0 : 1e12));   // 義務教育の国庫購入を見る盤面では他の POP は貯蓄 0（民間が教育を買い切ると国庫の買う分が 0）
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        return (r, w, buyer!);
    }

    [Fact]
    public void 旗の既定は0_高等教育の楔は割合のとき正_他の財と割合0では0()
    {
        Assert.Equal(0, new Coefficients().ScholarshipLaw);
        var c = new Coefficients();
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region;
        int h = Catalog.HigherEducationId;
        r.ScholarshipShare = 0.0;
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure);
        r.ScholarshipShare = S;
        Assert.Equal(S, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure, 12);
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, Catalog.EducationId, true, null, 0).insure);          // 義務教育の財には楔を掛けない
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, c, Catalog.BreadId, false, null, 0).insure);
        r.ScholarshipShare = 2.0;
        Assert.Equal(EconomyStep.InsuranceCoverageCap, EconomyStep.BuyerWedge(r, c, h, true, null, 0).insure, 12);   // 0 除算の守りの上限
    }

    [Fact]
    public void J1_国庫の負担は割合と授業料の積_買い手の自己負担は残りの分()
    {
        var (a, wa, ba) = OneMonth(0.0);
        var (b, wb, bb) = OneMonth(S);
        int h = Catalog.HigherEducationId;
        double priceA = a.Markets[h].Price, priceB = b.Markets[h].Price;
        Assert.Equal(priceA, priceB);                                   // 値段は月内で動かない（同じ盤面）
        double qa = a.Markets[h].LastSold, qb = b.Markets[h].LastSold;
        Assert.True(qa > 0, "前提が崩れた：法なしの盤面で高等教育が 1 単位も買われていない（必要量が立っていない）");
        Assert.True(qb >= qa, $"楔で値段が下がったのに買う量が減った（{qa} → {qb}）");
        // 国庫の負担＝割合×売れた量×値段
        Assert.Equal(0.0, a.StateSpent[(int)TreasuryCat.Education]);
        Assert.Equal(S * qb * priceB, b.StateSpent[(int)TreasuryCat.Education], 9);
        // 買い手の自己負担（PaidPerMonth の差＝他の財の支払いは同じ）＝(1−s)×授業料 − 法なしの授業料
        double dPaid = bb.PaidPerMonth.Value - ba.PaidPerMonth.Value;
        Assert.Equal((1.0 - S) * qb * priceB - qa * priceA, dPaid, 9);
        Assert.Equal(0.0, a.StateSpent[(int)TreasuryCat.Modernization]);   // 補助は近代化の区分ではなく教育の区分（翌月の請求の見込みが読む）
        Assert.Equal(0.0, b.StateSpent[(int)TreasuryCat.Modernization]);
    }

    [Fact]
    public void J1_国庫の区分は教育_義務教育の購入と足し算になる()
    {
        // 義務教育（教育財）の国庫購入と同じ区分に載る＝翌月の請求 regrow×StateSpent[Education] が両方を含む
        var (b0, _, _) = OneMonth(0.0, compulsory: 0.5);
        var (b1, _, _) = OneMonth(S, compulsory: 0.5);
        int h = Catalog.HigherEducationId;
        double sub = S * b1.Markets[h].LastSold * b1.Markets[h].Price;
        Assert.True(b0.StateSpent[(int)TreasuryCat.Education] > 0, "前提が崩れた：義務教育の購入が無い");
        Assert.Equal(b0.StateSpent[(int)TreasuryCat.Education] + sub, b1.StateSpent[(int)TreasuryCat.Education], 6);
    }

    [Fact]
    public void J2_請求の見込みの行は教育の区分を読む_補助を別の区分へ書くと請求が立たない()
    {
        string src = File.ReadAllText(FindSimCore("Simulation.cs"));
        Assert.Matches(new Regex(@"regrow\s*\*\s*\(\s*Math\.Max\(0,\s*r\.StateSpent\[\(int\)TreasuryCat\.Education\]\)"), src);
        Assert.Matches(new Regex(@"CompulsoryEduFunding \*= payRate;.*ScholarshipShare \*= payRate;"), src);   // 割合も義務教育の財源率と同じ支払い率で縮む（r＜1 の月は負担も比例して縮む）
        string eco = File.ReadAllText(FindSimCore("EconomyStep.cs"));
        Assert.Contains("region.StateSpent[(int)TreasuryCat.Education] += subsidy;", eco);
    }

    [Fact]
    public void J3_書き手はSimulationの月次の導出1行だけ_AIもプレイヤーも同じ率()
    {
        string root = Path.GetDirectoryName(FindSimCore("Simulation.cs"))!;
        int writers = 0;
        foreach (var (full, rel) in GoodIdGuardTests.CsFiles())
        {
            if (rel == "PriceOfBread.Tests/ScholarshipTests.cs") continue;   // 試験の盤面が直に置く
            foreach (var line in File.ReadLines(full))
            {
                string t = line.Split("//")[0];
                if (Regex.IsMatch(t, @"ScholarshipShare\s*(=|\*=|\+=|-=)(?!=)")) writers++;
            }
        }
        var all = GoodIdGuardTests.CsFiles().Select(x => x.Rel).ToList();
        Assert.True(writers == 2, "writers=" + writers + " files=" + all.Count + " sim=" + all.Contains("PriceOfBread.SimCore/Simulation.cs"));   // 導出の 1 行（=）と支払い率の行（*=）。Simulation 以外の書き手が増えたら赤
        foreach (var f in new[] { "SelfPlay.cs", "Agents.cs", "Politics.cs", "Parliament.cs", "Government.cs" })
            Assert.DoesNotContain("ScholarshipShare", File.ReadAllText(Path.Combine(root, f)));
    }

    [Fact]
    public void 旗0は割合を書かない_旗1は義務教育の財源率と同じ導出で_法の門を通る()
    {
        double SumShare(Simulation s) { double t = 0; foreach (var r in s.World.Regions) t += r.ScholarshipShare; return t; }
        Simulation Board(int flag, EducationLaw law, double spending)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            sim.World.Coef.ScholarshipLaw = flag; sim.World.Coef.ParishEduFunded = 0;
            sim.World.EducationLaw = law; sim.World.Policy.EducationLaw = law;
            sim.World.EducationSpending = spending; sim.World.Policy.EducationSpendingTarget = spending;
            sim.Run(2);
            return sim;
        }
        Assert.Equal(0.0, SumShare(Board(0, EducationLaw.Public, 0.6)));            // 旗 0＝法なし
        Assert.Equal(0.0, SumShare(Board(1, EducationLaw.Religious, 0.6)));         // 法の門：教区学校の国は国庫が授業料を負担しない（義務教育の財源も 0）
        Assert.Equal(0.0, SumShare(Board(1, EducationLaw.Public, 0.0)));            // 段 0＝教育費なし
        var on = Board(1, EducationLaw.Public, 0.6);
        Assert.True(SumShare(on) > 0);
        foreach (var r in on.World.Regions) Assert.Equal(r.CompulsoryEduFunding, r.ScholarshipShare, 12);   // 同じ導出に同じ支払い率
        var hi = Board(1, EducationLaw.Public, 1.0);
        Assert.True(SumShare(hi) > SumShare(on), "教育費の段が上がっても割合が増えない（段に比例）");
    }

    [Fact]
    public void J5_旗1は近代化投資の高等教育の予約を立てない_旗0は旧のまま()
    {
        double Funding(int flag)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            for (int q = 0; q < 2; q++) sim.StepQuarter();   // 就学の実績（LastEducationFill）が立つまで回す
            var w = sim.World;
            w.Coef.ScholarshipLaw = flag;
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
        w.Coef.ScholarshipLaw = 1; w.Region.ScholarshipShare = 0.25;
        string on = PlayText.EducationSpendingText(w);
        Assert.StartsWith(old, on);
        Assert.Contains("奨学金", on);
        Assert.Contains((0.25).ToString("P0"), on);
    }

    private static string FindSimCore(string file)
    {
        string? d = AppContext.BaseDirectory;
        while (d is not null && !Directory.Exists(Path.Combine(d, "PriceOfBread.SimCore"))) d = Path.GetDirectoryName(d);
        Assert.NotNull(d);
        return Path.Combine(d!, "PriceOfBread.SimCore", file);
    }
}
