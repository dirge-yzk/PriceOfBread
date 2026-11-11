using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(908) 計器 <c>lawgate2_s</c>（施行の否決の理由の表・読むだけ）の番人。事前登録 measurements/m908_20261107_prereg_sm.txt [3]。
/// 期待は計器と別の出所から作る：盤面の欄を手で置いた規則の読み（社会保障の前提＝官僚制度か緊縮の上限・参政権・不可逆）と、エンジンが戦線に残した符号（FrontsOf＝別の出所）。
/// 突然変異（使い捨ての木・measurements/m908_20261107/mutation.txt）：M1 計器が理由を全て 1 にする→場面の期待で赤／M2 緊縮の上限の判定を外す→場面 8 で赤／
/// M3 符号 5 を控えない→下側の門（符号 5 の戦線の数と行の数）で赤／M4 控えを呼ばない→行が空で下側の門で赤／M5 緊縮と官僚の順序を入れ替える→場面 8 か 11 で赤。</summary>
[Collection("Instruments")]
public class Lawgate2InstrumentTests
{
    private static (List<Instruments.Lawgate2Row> Rows, List<Instruments.GFront> Fronts) Run(Action<World> setup)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lawgate2_" + Guid.NewGuid().ToString("N"));
        var sim = new Simulation(NationConfig.France().ToScenario(11), new NullAgent());
        var w = sim.World;
        w.PoliticalLockEnabled = false;
        w.Coef.PoliticalBudgetGate = 0.5;
        setup(w);
        Instruments.Configure(dir, "*");
        try { Government.Step(w, w.Coef); }
        finally { Instruments.Configure(null, null); }
        return (Instruments.Lawgate2RowsOf(w).ToList(), Instruments.FrontsOf(w).ToList());
    }

    private static Instruments.Lawgate2Row One(List<Instruments.Lawgate2Row> rows, PolicyAxis a)
    {
        var r = rows.Where(x => x.Axis == a).ToList();
        Assert.True(r.Count == 1, $"{a} の行が {r.Count} 本");
        return r[0];
    }

    private static void Gate(List<Instruments.Lawgate2Row> rows, List<Instruments.GFront> fronts)
    {
        Assert.Equal(fronts.Count(f => f.Kind == 'L' && f.Code == 6), rows.Count(r => r.EngineCode == 6));   // 下側の門：符号 6 の戦線の数＝行の数
        Assert.Equal(fronts.Count(f => f.Kind == 'L' && f.Code == 5), rows.Count(r => r.EngineCode == 5));   // 符号 5 も同じ
        Assert.DoesNotContain(rows, r => r.ReasonCode == 0);                                                  // 導出の欠けは 0 行
    }

    [Fact]
    public void 社会保障の否決の理由は緊縮の上限が官僚制度の前提より先()
    {
        // 場面 11：緊縮 0・官僚＝猟官・救貧法→労災が現在・年金を目標（次の一歩＝失業保険＝官僚制度が要る）
        var (rows, fronts) = Run(w => { w.SocialSecurityLaw = SocialSecurityLaw.WorkersComp; w.Policy.SocialSecurityTarget = 1.0; w.Bureaucracy = BureaucracyLaw.Spoils; w.AusterityIntensity = 0; });
        var r = One(rows, PolicyAxis.SocialSecurity);
        Assert.Equal(11, r.ReasonCode); Assert.Equal(6, r.EngineCode); Assert.Equal(1, r.Cur); Assert.Equal(3, r.Target);
        Gate(rows, fronts);
        // 場面 8：同じ盤面で緊縮 0.9（手の数＝floor((1−0.9)×3)＝0 段まで・次の一歩の 2 は超える）＝官僚の前提より緊縮の上限が先
        (rows, fronts) = Run(w => { w.SocialSecurityLaw = SocialSecurityLaw.WorkersComp; w.Policy.SocialSecurityTarget = 1.0; w.Bureaucracy = BureaucracyLaw.Spoils; w.AusterityIntensity = 0.9; });
        r = One(rows, PolicyAxis.SocialSecurity);
        Assert.Equal(8, r.ReasonCode); Assert.Equal(0.9, r.Austerity, 12);
        Gate(rows, fronts);
        // 官僚が資格任用なら緊縮が 0 で前提は通る＝行が無い（官僚の前提だけが理由だったことの裏）
        (rows, fronts) = Run(w => { w.SocialSecurityLaw = SocialSecurityLaw.WorkersComp; w.Policy.SocialSecurityTarget = 1.0; w.Bureaucracy = BureaucracyLaw.MeritExam; w.AusterityIntensity = 0; });
        Assert.DoesNotContain(rows, x => x.Axis == PolicyAxis.SocialSecurity);
        Gate(rows, fronts);
    }

    [Fact]
    public void 参政権と女性の権利と不可逆の理由が盤面の欄から導かれる()
    {
        var (rows, fronts) = Run(w =>
        {
            w.Suffrage = Suffrage.AdultMale; w.WomenRights = WomenRightsLaw.None; w.Policy.Suffrage = Suffrage.Universal;   // 男女普通選挙は女性の権利（選挙権）が前提
            w.SerfEmancipation = ReformLevel.Full; w.Policy.SerfEmancipation = ReformLevel.None;                            // 農奴解放の後退は不可逆
        });
        Assert.Equal(14, One(rows, PolicyAxis.Suffrage).ReasonCode);
        var s = One(rows, PolicyAxis.SerfEmancipation);
        Assert.Equal(5, s.EngineCode); Assert.Equal(20, s.ReasonCode);
        Gate(rows, fronts);
        Assert.True(rows.Count >= 2);
        // 秘密投票：選挙の無い国（参政権＝なし）
        (rows, fronts) = Run(w => { w.Suffrage = Suffrage.None; w.Policy.Suffrage = Suffrage.None; w.Policy.SecretBallot = SecretBallotLaw.Secret; w.SecretBallot = SecretBallotLaw.Open; });
        Assert.Equal(9, One(rows, PolicyAxis.SecretBallot).ReasonCode);
        Gate(rows, fronts);
        // 女性の権利：男性普通選挙が前提（参政権が制限選挙のまま・教育権→選挙権の一歩）
        (rows, fronts) = Run(w => { w.Suffrage = Suffrage.Census; w.Policy.Suffrage = Suffrage.Census; w.WomenRights = WomenRightsLaw.Education; w.Policy.WomenRights = WomenRightsLaw.Suffrage; });
        Assert.Equal(13, One(rows, PolicyAxis.WomenRights).ReasonCode);
        Gate(rows, fronts);
    }
}
