using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// F4政党統一の番人（2026-07-12）。互換射影＝World.PartySeats/GoverningPartyは常に「族」の集計値・
/// DynamicParties=0（既定）は常設3党のみ＝現行と厳密一致、が守るべき不変条件。
/// </summary>
public class PartyTests
{
    [Fact]
    public void 動的政党0では常設3党のみで与党名は族名()
    {
        // ★主張の書き換え（2026-09-22）：旧名「既定は常設3党のみ」は既定 DynamicParties=1.0（点火済み）の下では不変条件ではなかった
        //   ＝仏 seed11 の最初の12四半期に政党化するクラスタが**たまたま**無かったから通っていた。飢餓の駆動の点火（階層3分割と対）で
        //   1780年代の仏の食料不足が運動を立て、動的政党（自由主義党）が現れた＝旧名の主張は世界の偶然だった。
        //   クラスの宣言どおりの不変条件＝「DynamicParties=0 は常設3党のみ（厳密一致）」を明示で測る。
        var sim = new Simulation(NationConfig.France().ToScenario(11), new NullAgent());
        sim.World.Coef.DynamicParties = 0;
        for (int q = 0; q < 12; q++) sim.StepQuarter();
        var w = sim.World;
        Assert.Equal(3, w.Parties.Count);
        Assert.All(w.Parties, pt => Assert.True(pt.Permanent));
        Assert.Contains(w.GoverningPartyName, new[] { "保守党", "自由党", "社会党" });
    }

    [Fact]
    public void 動的政党でも族の得票集計は不変_射影の恒等()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.World.Coef.DynamicParties = 1.0;
        for (int q = 0; q < 40; q++) sim.StepQuarter();
        var w = sim.World;
        Assert.InRange(w.GoverningParty, 0, 2);   // 互換射影＝族番号の保証（Runnerの3長配列・既存読者の前提）
        var famSum = new double[3];
        foreach (var pt in w.Parties) famSum[pt.Family] += pt.VoteShare;
        for (int f = 0; f < 3; f++)
            Assert.True(Math.Abs(famSum[f] - w.VoteShares[f]) < 1e-9, $"族{f}のΣ政党得票{famSum[f]}が族得票{w.VoteShares[f]}とズレた");
    }

    [Fact]
    public void 統制力と参政権の票を持つクラスタだけが政党化する()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        var w = sim.World;
        w.Coef.DynamicParties = 1.0;
        w.Factions.Add(new FactionState { Region = w.Region, Bloc = 2, Ideo = (int)Ideology.Republicanism, Size = 500, AvgMilitancy = 0.6, Cohesion = 0.5 });
        // 票なし（参政権None）＝在野運動のまま
        w.Suffrage = Suffrage.None; w.ParliamentControl = 0.5;
        Parliament.Step(w, w.Coef);
        Assert.DoesNotContain(w.Parties, pt => !pt.Permanent);
        // 票あり＋統制力あり＝政党化（族＝共和主義→自由）
        w.Suffrage = Suffrage.AdultMale;
        Parliament.Step(w, w.Coef);
        var dyn = w.Parties.Find(pt => !pt.Permanent);
        Assert.NotNull(dyn);
        Assert.Equal(1, dyn!.Family);
        Assert.Contains("共和主義党", dyn.Name);
        // 統制力ゼロの世界では新規政党化は起きない（別世界で確認）
        var sim2 = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        var w2 = sim2.World;
        w2.Coef.DynamicParties = 1.0;
        w2.Factions.Add(new FactionState { Region = w2.Region, Bloc = 2, Ideo = (int)Ideology.Republicanism, Size = 500, AvgMilitancy = 0.6, Cohesion = 0.5 });
        w2.Suffrage = Suffrage.AdultMale; w2.ParliamentControl = 0;
        Parliament.Step(w2, w2.Coef);
        Assert.DoesNotContain(w2.Parties, pt => !pt.Permanent);
    }

    // ==== 経済軸の綱領の撤去（2026-09-19・P9二重計上／P8黙った持ち上げ・切り下げ）の番人 ====
    // 旧＝Parliament.ApplyPlatform が与党ごとに関税・福祉・累進の**目標**を毎四半期直接書いていた
    //   （保守＝関税の床0.3・福祉の上限0.1・累進0／自由＝関税の上限0.1・福祉の床0.34／社会＝福祉1.0・累進の床0.5／少数与党は連立相手の定数へ寄せる）。
    //   同じ党の同じ軸の選好は ParliamentNet（議席×党選好）として NetSupport に既に入っていた＝同じ原因を「通るか」と「どちらへ動くか」の2回数えていた。
    // 主張＝党は**通りやすさ**（純支持と反対の力＝Government.Move が読む2量）にだけ効き、**目標**は書かない（AI・プレイヤーとも同じ）。

    /// <summary>英（開幕＝立憲君主制）に議席を直接置いた世界。選挙も不信任も公示も走らない四半期に合わせる（QuartersSinceElection=0→Step 内で1）。</summary>
    private static World SeatedBritain(ulong seed, int gov, double govSeat)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(seed), new NullAgent()).World;
        Assert.Equal(Polity.ConstitutionalMonarchy, w.Polity);   // 前提＝選挙のある政体（非選挙政体は綱領に届く前に return する）
        double rest = 1.0 - govSeat;
        w.PartySeats[gov] = govSeat;
        w.PartySeats[(gov + 1) % 3] = rest * 0.6;   // 第二党＝旧・連立相手
        w.PartySeats[(gov + 2) % 3] = rest * 0.4;
        w.GoverningParty = gov;
        w.QuartersSinceElection = 0;
        return w;
    }

    [Theory]
    [InlineData(0, 0.6)] [InlineData(1, 0.6)] [InlineData(2, 0.6)]   // 単独過半数
    [InlineData(0, 0.4)] [InlineData(1, 0.4)] [InlineData(2, 0.4)]   // 少数与党（旧＝連立相手の定数へも寄せた）
    public void 与党が替わっても経済軸の目標は綱領に書き換えられない(int gov, double govSeat)   // (a)
    {
        var w = SeatedBritain(23, gov, govSeat);
        var p = w.Policy;
        // 旧綱領が必ず動かした値＝関税0.2（保守の床0.3と自由の上限0.1の間）・福祉0.2（保守の上限0.1・自由の床0.34・社会1.0の間）・累進0.2（保守0と社会の床0.5の間）
        p.TariffTarget = 0.2; p.SocialSecurityTarget = 0.2; p.ProgressiveTaxTarget = 0.2;
        p.Suffrage = Suffrage.None;   // 法軸の綱領（引き上げのみ）が走ったことの目印
        Parliament.Step(w, w.Coef);
        Assert.Equal(gov, w.GoverningParty);   // 選挙が走っていない＝設定した与党の綱領の分岐に入った
        Assert.Equal(gov switch { 1 => Suffrage.Census, 2 => Suffrage.AdultMale, _ => Suffrage.None }, p.Suffrage);   // 綱領そのものは生きている（法軸）
        Assert.Equal(0.2, p.TariffTarget);
        Assert.Equal(0.2, p.SocialSecurityTarget);
        Assert.Equal(0.2, p.ProgressiveTaxTarget);
    }

    [Fact]
    public void 与党の選好は純支持に入り同じ法の通りやすさが議席で変わる()   // (b)
    {
        // 同じ世界（同じ種）で議席だけが違う＝保守が8割か社会が8割か
        var cons = SeatedBritain(29, 0, 0.8);
        var soc = SeatedBritain(29, 2, 0.8);
        foreach (var axis in new[] { PolicyAxis.ProgressiveTax, PolicyAxis.SocialSecurity })
        {
            double nC = PolicyPreferences.NetSupport(cons, axis, true), nS = PolicyPreferences.NetSupport(soc, axis, true);
            Assert.True(nS > nC, $"{axis}：社会党の議席で純支持が上がらない（保守 {nC:F4} ⇔ 社会 {nS:F4}）＝党の選好が通りやすさに入っていない");
            // 差は議会の項（議席×党選好＝ParliamentNet）だけから来る＝勢力別内訳の党の行の合計の差と一致（街頭・軍・小営業は議席に依らない）
            Assert.Equal(nS - nC, PartyRows(soc, axis) - PartyRows(cons, axis), 9);
            double oC = PolicyPreferences.OpposingPower(cons, axis, true), oS = PolicyPreferences.OpposingPower(soc, axis, true);
            Assert.True(oS < oC, $"{axis}：社会党の議席で反対の力が減らない（保守 {oC:F4} ⇔ 社会 {oS:F4}）＝歩幅（Move の抵抗）に党が入っていない");
        }
    }

    private static double PartyRows(World w, PolicyAxis axis)
    {
        double s = 0;
        foreach (var (who, v) in PolicyPreferences.SupportBreakdown(w, axis, true))
            if (who.StartsWith("議会・", StringComparison.Ordinal)) s += v;
        return s;
    }

    [Fact]
    public void プレイヤーの置いた経済軸の目標は政権が替わっても残る()   // (c) P5/P8
    {
        // プレイヤー国の agent＝NullAgent（App の MainWindowViewModel と同じ）。四半期を丸ごと回す＝旧・毎四半期の上書きが住んでいた経路そのもの。
        var sim = new Simulation(NationConfig.Britain().ToScenario(31), new NullAgent());
        var w = sim.World; var p = w.Policy;
        // 財政タブ（Tariff／ProgressiveTax の setter）と法律タブ（社会保障＝段/3）と同じ書き込み
        double tariff = 0.2, prog = Government.TaxValue(PolicyAxis.ProgressiveTax, 1), ss = 2 / 3.0;
        p.TariffTarget = tariff; p.ProgressiveTaxTarget = prog; p.SocialSecurityTarget = ss;
        for (int q = 0; q < 12; q++)
        {
            int gov = q % 3;   // 保守→自由→社会→…（少数与党＝旧は連立相手へも寄せた）
            w.PartySeats[gov] = 0.45; w.PartySeats[(gov + 1) % 3] = 0.35; w.PartySeats[(gov + 2) % 3] = 0.20;
            w.GoverningParty = gov; w.QuartersSinceElection = 0;
            sim.StepQuarter();
            Assert.Equal(Polity.ConstitutionalMonarchy, w.Polity);
            Assert.True(tariff == p.TariffTarget && prog == p.ProgressiveTaxTarget && ss == p.SocialSecurityTarget,
                $"四半期{q}（与党{gov}）：プレイヤーの目標が書き換えられた（関税 {p.TariffTarget:F4}／累進 {p.ProgressiveTaxTarget:F4}／福祉 {p.SocialSecurityTarget:F4}）");
        }
    }
}
