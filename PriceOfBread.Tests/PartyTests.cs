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

    // ==== 綱領の撤去（経済軸 2026-09-19・P9二重計上／P8黙った持ち上げ・切り下げ ＋ 法軸と動的与党の要求 2026-09-26・回5-1 項目17）の番人 ====
    // 旧＝Parliament.ApplyPlatform が与党ごとに**目標**を毎四半期直接書いていた
    //   （経済軸＝保守は関税の床0.3・福祉の上限0.1・累進0／自由は関税の上限0.1・福祉の床0.34／社会は福祉1.0・累進の床0.5／少数与党は連立相手の定数へ寄せる。
    //    法軸＝自由は公教育・制限選挙・10時間／社会は公教育・男子普選・労組の容認・8時間。プレイヤー国の目標も上書き）。
    //   加えて与党が動的政党なら、選挙の四半期に由来ブロックの要求を門なしで1段ずつ置いた。
    //   同じ党の同じ軸の選好は ParliamentNet（議席×党選好）として NetSupport に既に入っていた＝同じ原因を「通るか」と「どちらへ動くか」の2回数えていた。
    // 主張＝党は**通りやすさ**（純支持と反対の力＝Government.Move が読む2量）にだけ効き、議会は**目標**を1つも書かない（AI・プレイヤーとも同じ）。
    //   法軸の旧綱領の段は AI の意図（UtilityAgent.Apply の「議会の構成を読む」段落）へ移った＝下の最後の番人。

    /// <summary>英（開幕＝立憲君主制）に議席を直接置いた世界。選挙も不信任も公示も走らない四半期に合わせる（QuartersSinceElection=0→Step 内で1）。</summary>
    private static World SeatedBritain(ulong seed, int gov, double govSeat)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(seed), new NullAgent()).World;
        Assert.Equal(Polity.ConstitutionalMonarchy, w.Polity);   // 前提＝選挙のある政体（旧＝綱領が走った政体）
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
    public void 与党が替わっても議会は法軸も経済軸も目標を書き換えない(int gov, double govSeat)   // (a)
    {
        var w = SeatedBritain(23, gov, govSeat);
        var p = w.Policy;
        // 旧綱領が必ず動かした値＝関税0.2（保守の床0.3と自由の上限0.1の間）・福祉0.2（保守の上限0.1・自由の床0.34・社会1.0の間）・累進0.2（保守0と社会の床0.5の間）
        p.TariffTarget = 0.2; p.SocialSecurityTarget = 0.2; p.ProgressiveTaxTarget = 0.2;
        // 法軸＝旧綱領（自由・社会）が引き上げた4本を、どの党の綱領よりも下に置く
        p.Suffrage = Suffrage.None; p.EducationLaw = EducationLaw.None; p.WorkHours = WorkHours.Unlimited; p.LaborUnion = LaborUnionLaw.Illegal;
        var before = Array.ConvertAll(typeof(NationPolicy).GetFields(), fi => fi.GetValue(p));
        Parliament.Step(w, w.Coef);
        Assert.Equal(gov, w.GoverningParty);   // 選挙が走っていない＝設定した与党のまま（旧はこの与党の綱領の分岐に入った）
        Assert.Equal(Suffrage.None, p.Suffrage);            // 旧＝自由は制限選挙・社会は男子普選へ書き換えた
        Assert.Equal(EducationLaw.None, p.EducationLaw);    // 旧＝自由・社会は公教育へ
        Assert.Equal(WorkHours.Unlimited, p.WorkHours);     // 旧＝自由は10時間・社会は8時間へ
        Assert.Equal(LaborUnionLaw.Illegal, p.LaborUnion);  // 旧＝社会は容認へ
        Assert.Equal(0.2, p.TariffTarget);
        Assert.Equal(0.2, p.SocialSecurityTarget);
        Assert.Equal(0.2, p.ProgressiveTaxTarget);
        Assert.Equal(before, Array.ConvertAll(typeof(NationPolicy).GetFields(), fi => fi.GetValue(p)));   // 目標は1つも動かない
    }

    [Fact]
    public void 動的政党が与党のまま選挙の四半期を迎えても議会は目標を1つも書かない()
    {
        // 旧＝与党が動的政党（クラスタ由来）なら、選挙の四半期に由来ブロックの要求 top-k を門なしで1段ずつ目標へ押した（関税・累進・社会保障を含む軸を上げ下げ両方）。
        var w = new Simulation(NationConfig.Britain().ToScenario(37), new NullAgent()).World;
        w.Coef.DynamicParties = 1.0;
        Parliament.Step(w, w.Coef);   // 常設3党の遅延初期化
        // 全ての有権者を社会主義100%に＝社会族の票は全て社会主義党（国民政党＝思想単位）へ入り、常設の社会党は0＝動的政党が与党の実体になる
        foreach (var pp in w.AllPops()) { Array.Clear(pp.IdeologyShares); pp.IdeologyShares[(int)Ideology.Socialism] = 1.0; }
        w.Parties.Add(new PoliticalParty { Name = "社会主義党", Family = 2, Ideo = (int)Ideology.Socialism });
        w.QuartersSinceElection = w.Coef.ElectionIntervalQuarters - 1;   // Step の中で任期満了＝定期総選挙
        var p = w.Policy;
        var before = Array.ConvertAll(typeof(NationPolicy).GetFields(), fi => fi.GetValue(p));
        Parliament.Step(w, w.Coef);
        Assert.Equal(0, w.QuartersSinceElection);                           // 前提＝この四半期に選挙があった
        Assert.Equal((int)Parliament.Party.Socialist, w.GoverningParty);
        Assert.Equal("社会主義党", w.GoverningPartyName);                   // 前提＝与党の実体は動的政党
        Assert.Equal(before, Array.ConvertAll(typeof(NationPolicy).GetFields(), fi => fi.GetValue(p)));   // 旧＝綱領と要求の押し込みで動いた
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
            double oC = PolicyPreferences.OpposingPower(cons, axis, true, PolicyPreferences.Build(cons)), oS = PolicyPreferences.OpposingPower(soc, axis, true, PolicyPreferences.Build(soc));
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
    public void プレイヤーの置いた目標は政権が替わっても残る()   // (c) P5/P8（回5-1 項目17で法軸4本を追加）
    {
        // プレイヤー国の agent＝NullAgent（App の MainWindowViewModel と同じ）。四半期を丸ごと回す＝旧・毎四半期の上書きが住んでいた経路そのもの。
        var sim = new Simulation(NationConfig.Britain().ToScenario(31), new NullAgent());
        var w = sim.World; var p = w.Policy;
        // 財政タブ（Tariff／ProgressiveTax の setter）と法律タブ（社会保障＝段/3）と同じ書き込み
        double tariff = 0.2, prog = Government.TaxValue(PolicyAxis.ProgressiveTax, 1), ss = 2 / 3.0;
        p.TariffTarget = tariff; p.ProgressiveTaxTarget = prog; p.SocialSecurityTarget = ss;
        // 法律タブで置いた法軸の目標＝現行法のまま（据え置き）。旧綱領はこの4本を上書きした（前提＝旧なら動いた値であること）
        var suf = w.Suffrage; var edu = w.EducationLaw; var hours = w.WorkHours; var union = w.LaborUnion;
        p.Suffrage = suf; p.EducationLaw = edu; p.WorkHours = hours; p.LaborUnion = union;
        Assert.True(hours > WorkHours.Ten && union < LaborUnionLaw.Tolerated,
            $"テスト前提が崩れた：旧の社会党の綱領でも動かない盤面（労働時間{hours}・労組{union}）");
        for (int q = 0; q < 12; q++)
        {
            int gov = q % 3;   // 保守→自由→社会→…（少数与党＝旧は連立相手へも寄せた）
            w.PartySeats[gov] = 0.45; w.PartySeats[(gov + 1) % 3] = 0.35; w.PartySeats[(gov + 2) % 3] = 0.20;
            w.GoverningParty = gov; w.QuartersSinceElection = 0;
            sim.StepQuarter();
            Assert.Equal(Polity.ConstitutionalMonarchy, w.Polity);
            Assert.True(tariff == p.TariffTarget && prog == p.ProgressiveTaxTarget && ss == p.SocialSecurityTarget,
                $"四半期{q}（与党{gov}）：プレイヤーの目標が書き換えられた（関税 {p.TariffTarget:F4}／累進 {p.ProgressiveTaxTarget:F4}／福祉 {p.SocialSecurityTarget:F4}）");
            Assert.True(suf == p.Suffrage && edu == p.EducationLaw && hours == p.WorkHours && union == p.LaborUnion,
                $"四半期{q}（与党{gov}）：プレイヤーの法軸の目標が書き換えられた（参政権 {p.Suffrage}／教育法 {p.EducationLaw}／労働時間 {p.WorkHours}／労組 {p.LaborUnion}）");
        }
    }

    /// <summary>★回5-1 項目17（c）：綱領の法軸の段は AI の意図へ移った＝同じ立憲国の盤面で与党の札だけを替えると、
    /// 社会党のときだけ AI の労働時間の目標が「現行（10時間）より短い側の次の段」＝8時間に置かれる（AI の世界から8時間労働が消えない）。
    /// 政治ロックの隔離弁を切る＝照会の純支持を中立にして、法律タブ（GateInfo）が次の段を通す盤面にする。</summary>
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void 社会党が与党のときだけAIは労働時間の短い段を目標に置く(int gov)
    {
        var w = SeatedBritain(43, gov, 0.6);
        w.PoliticalLockEnabled = false;
        w.WorkHours = WorkHours.Ten;   // 10時間法は施行済み（旧の自由党の綱領の段）＝ここから短い側の次の段は8時間だけ
        Assert.True(Government.GateInfo(w, PolicyAxis.ShorterHours, false, (int)WorkHours.Eight).Ok, "テスト前提が崩れた：法律タブでも8時間の段が押せない");
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        if (gov == (int)Parliament.Party.Socialist) Assert.Equal(WorkHours.Eight, w.Policy.WorkHours);
        else Assert.True(w.Policy.WorkHours >= WorkHours.Ten, $"与党{gov}：社会党でないのに8時間の段が目標に置かれた");
    }
}
