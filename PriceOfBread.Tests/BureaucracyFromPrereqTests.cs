using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(389) Q-a8 AI の官僚制度の判断（旗 <c>AiBureaucracyFromPrereq</c>）の番人。事前登録 measurements/ab_20261017_prereg.txt。
/// 形＝AI が欲しい法（目標が現行より上）が〈官僚制度が前提〉で止まった四半期に、官僚制度の目標をその法が通る最小の段へ上げる（直後の GateLaw が一歩に整形する）。
/// 期待値は列挙値と文言のリテラルで書く（本体の関数から作らない）。突然変異（走査の門を外す／必要な段を置かない／猟官制の早期 return を外す／別の理由で止まる法も数える）で赤を確かめる。</summary>
public class BureaucracyFromPrereqTests
{
    private const string ReasonBureaucracy = "官僚制度（資格任用以上）が前提";

    /// <summary>猟官制の盤面（目標は全て現行と同じ・純支持の門は外す）。</summary>
    private static World Board(double flag, BureaucracyLaw bur = BureaucracyLaw.Spoils)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.PoliticalLockEnabled = false;
        w.Coef.AiBureaucracyFromPrereq = flag;
        w.Bureaucracy = bur; w.Policy.Bureaucracy = bur;
        w.Antitrust = AntitrustLaw.None; w.Policy.Antitrust = AntitrustLaw.None;
        w.NationalizeRailways = false; w.Policy.NationalizeRailways = false;
        w.ProgressiveTax = 0; w.Policy.ProgressiveTaxTarget = 0;
        w.SocialSecurityLaw = SocialSecurityLaw.WorkersComp; w.Policy.SocialSecurityTarget = 1.0 / 3.0;
        w.AusterityIntensity = 0;
        return w;
    }

    private static void Follow(World w) { PolicyPreferences.Profile? shared = null; UtilityAgent.BureaucracyFollowsWants(w, w.Policy, ref shared); }

    private static void Want(World w, PolicyAxis axis)
    {
        var p = w.Policy;
        switch (axis)
        {
            case PolicyAxis.Antitrust: p.Antitrust = AntitrustLaw.CartelBan; break;
            case PolicyAxis.Nationalization: p.NationalizeRailways = true; break;
            case PolicyAxis.ProgressiveTax: p.ProgressiveTaxTarget = 0.8; break;
            case PolicyAxis.SocialSecurity: p.SocialSecurityTarget = 1.0; break;
        }
    }

    [Theory]
    [InlineData(PolicyAxis.Antitrust)]
    [InlineData(PolicyAxis.Nationalization)]
    [InlineData(PolicyAxis.ProgressiveTax)]
    [InlineData(PolicyAxis.SocialSecurity)]
    public void 猟官制の国が前提つきの法を欲しがると官僚制度の目標が資格任用になる(PolicyAxis axis)
    {
        var w = Board(1.0);
        Want(w, axis);
        // プレイヤーの法律タブが同じ理由を出す（AI とプレイヤーは同じ LawPrereq／GateInfo）
        int stage = axis == PolicyAxis.SocialSecurity ? 2 : 1;
        Assert.Equal((false, ReasonBureaucracy), Government.LawPrereq(w, axis, stage, w.Coef));
        Assert.Equal(ReasonBureaucracy, Government.GateInfo(w, axis, true, stage).Reason);
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);
    }

    [Fact]
    public void 旗0では官僚制度の目標に触らない()
    {
        foreach (var axis in new[] { PolicyAxis.Antitrust, PolicyAxis.Nationalization, PolicyAxis.ProgressiveTax, PolicyAxis.SocialSecurity })
        {
            var w = Board(0.0);
            Want(w, axis);
            UtilityAgent.GateAllLawTargets(w, w.Policy);
            Assert.Equal(BureaucracyLaw.Spoils, w.Policy.Bureaucracy);
        }
    }

    [Fact]
    public void 欲しい法が無ければ触らない_前提が満たされていれば触らない_下げない()
    {
        // (a) 何も欲しがらない猟官制の国：目標は猟官制のまま
        var w = Board(1.0);
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.Spoils, w.Policy.Bureaucracy);
        // (b) 資格任用の国が独占禁止を欲しがる（前提は満たされている）：議会などが置いた専門官僚制の目標を下げない・上げもしない
        w = Board(1.0, BureaucracyLaw.MeritExam);
        w.Policy.Bureaucracy = BureaucracyLaw.Professional;
        Want(w, PolicyAxis.Antitrust);
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.Professional, w.Policy.Bureaucracy);
        Assert.Equal(AntitrustLaw.CartelBan, w.Policy.Antitrust);   // 前提が通る法の目標は畳まれない
    }

    [Fact]
    public void 官僚制度のほかの理由で止まる法は数えない()
    {
        // 社会保障の失業保険の段は官僚制度が前提だが、緊縮の上限（債務緊縮が最大）が先に止める＝官僚制度を上げても通らない
        var w = Board(1.0);
        w.AusterityIntensity = 1.0;
        Want(w, PolicyAxis.SocialSecurity);
        Assert.Equal("緊縮の上限", Government.LawPrereq(w, PolicyAxis.SocialSecurity, 2, w.Coef).Reason);
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.Spoils, w.Policy.Bureaucracy);
    }

    [Fact]
    public void 必要な段は仮に置いて通る最小の段_専門官僚制へは押さない()
    {
        var w = Board(1.0);
        Want(w, PolicyAxis.Antitrust); Want(w, PolicyAxis.ProgressiveTax); Want(w, PolicyAxis.Nationalization); Want(w, PolicyAxis.SocialSecurity);
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);   // 4 本とも資格任用で通る＝専門官僚制は要らない
    }

    /// <summary>整形（GateLaw）は目標を現行から一歩へ畳むので、走査が置いた目標そのものは GateAllLawTargets の後では見えない＝走査を直に呼んで見る。</summary>
    [Fact]
    public void 走査が置く目標は必要な最小の段で_高い目標は下げない()
    {
        var w = Board(1.0);
        Want(w, PolicyAxis.Antitrust);
        w.Policy.Bureaucracy = BureaucracyLaw.Spoils;
        Follow(w);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);   // 専門官僚制まで押さない
        w.Policy.Bureaucracy = BureaucracyLaw.Professional;             // 議会などが置いた高い目標
        Follow(w);
        Assert.Equal(BureaucracyLaw.Professional, w.Policy.Bureaucracy);   // 下げない
        w = Board(0.0);
        Want(w, PolicyAxis.Antitrust);
        Follow(w);   // 旗 0 は（計器の読みで走っても）目標を書かない
        Assert.Equal(BureaucracyLaw.Spoils, w.Policy.Bureaucracy);
    }

    /// <summary>旗 2＝官僚制度が足りていれば法律タブの門（純支持・政治資本の残り）も通る法だけ数える。旗 1 は政治の見込みを問わない。</summary>
    [Fact]
    public void 旗2は政治の門が通る見込みのある法だけ数える()
    {
        // 政治の門が閉じる盤面（開幕の政治資本の残りが足りない）：旗 1 は目標を上げ、旗 2 は上げない
        var w1 = Board(1.0); w1.PoliticalLockEnabled = true; w1.Coef.MandateOverStockLegitPower = 0; Want(w1, PolicyAxis.Antitrust);   // ★(405)(419) 壁の一歩を閉じた世界〔旧の旗 0＝壁の一歩は永遠に通らない〕を見る番人＝払える量の指数を 0 に明示（既定は 2）
        PolicyPreferences.Profile? sh = null;
        var gate = Government.GateInfo(w1, PolicyAxis.Antitrust, true, 1, ref sh, BureaucracyLaw.MeritExam);
        Assert.False(gate.Ok);                       // 前提の盤面（変われば読み直す）
        Assert.Equal("政治資本の残りが不足", gate.Reason);   // ★(419) 指数 0 で壁を閉じた世界＝正統性がいくらあっても通らない＝理由は〈正統性が不足〉でなく従来のまま
        Follow(w1);
        Assert.Equal(BureaucracyLaw.MeritExam, w1.Policy.Bureaucracy);
        var w2 = Board(2.0); w2.PoliticalLockEnabled = true; w2.Coef.MandateOverStockLegitPower = 0; Want(w2, PolicyAxis.Antitrust);
        Follow(w2);
        Assert.Equal(BureaucracyLaw.Spoils, w2.Policy.Bureaucracy);
        // 政治の門が開く盤面（純支持の門を外す）：旗 2 も上げる
        var w3 = Board(2.0); Want(w3, PolicyAxis.Antitrust);
        Follow(w3);
        Assert.Equal(BureaucracyLaw.MeritExam, w3.Policy.Bureaucracy);
    }

    /// <summary>追記3 の錠を名指しする番人：走査は目標を上げるだけで下げない＝置いた目標は欲しさが消えても施行まで残る（プレイヤーの置いた目標と同じ・議会の綱領の目標も同じ）。
    /// 欲しさが消えたら畳む形（解放）を足すなら、この番人を意図して書き換える（暗黙に変えない）。</summary>
    [Fact]
    public void 置いた目標は欲しさが消えても施行まで残る_施行されたら専門官僚制へは押さない()
    {
        var w = Board(1.0);
        Want(w, PolicyAxis.Antitrust);
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);
        w.Policy.Antitrust = AntitrustLaw.None;   // 欲しさが消える
        for (var quarter = 0; quarter < 4; quarter++) UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);   // 残る（錠）
        w.Bureaucracy = BureaucracyLaw.MeritExam;   // 施行された
        Want(w, PolicyAxis.Antitrust);
        for (var quarter = 0; quarter < 4; quarter++) UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);   // 資格任用で足りる法は専門官僚制へ押さない
    }

    [Fact]
    public void LawPrereqの官僚制度の引数は既定で今の官僚制度を読む()
    {
        var w = Board(0.0);
        foreach (var bur in new[] { BureaucracyLaw.Spoils, BureaucracyLaw.MeritExam, BureaucracyLaw.Professional })
        {
            w.Bureaucracy = bur;
            Assert.Equal(Government.LawPrereq(w, PolicyAxis.ProgressiveTax, 1, w.Coef), Government.LawPrereq(w, PolicyAxis.ProgressiveTax, 1, w.Coef, bur));
        }
        w.Bureaucracy = BureaucracyLaw.Spoils;
        Assert.True(Government.LawPrereq(w, PolicyAxis.ProgressiveTax, 1, w.Coef, BureaucracyLaw.MeritExam).Ok);   // 仮の官僚制度は今の値を変えない
        Assert.Equal(BureaucracyLaw.Spoils, w.Bureaucracy);
    }
}
