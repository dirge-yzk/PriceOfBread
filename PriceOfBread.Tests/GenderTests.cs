using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 女性（構成属性・POP分割なし）：徴兵は男性のみ＝出身階級の女性比率が上がる。
/// 労働財の供給＝男性＋女性×参加率。★女性の権利は**1本の梯子**（なし→財産権→教育権→選挙権・統合法2026-08-10）で、
/// 財産権の段＝参加率0.4→0.7（労働供給+2割）／教育権の段＝女性識字↑／選挙権の段＝男女普通選挙(Universal)の前提。
/// </summary>
public class GenderTests
{
    [Fact]
    public void 徴兵は男性のみで出身階級の女性比率が上がる()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(61), new NullAgent());   // 大陸軍＝重い徴兵（建設時に実施）
        var r = sim.World.Region;
        var peasant = r.Pops[1];
        var soldier = r.Pops[5];
        Assert.True(soldier.Size > 0, "兵士が徴募されていない");
        Assert.Equal(0.0, soldier.FemaleShare);   // 兵士は男性のみ
        Assert.True(peasant.FemaleShare > 0.5, $"男手が抜けたのに農民の女性比率が上がらない（{peasant.FemaleShare:F3}）");
    }

    [Fact]
    public void 女性財産権法は労働供給を増やす()   // 参加率0.4→0.7＝自分の賃金を持てる女性が働きに出る
    {
        var baseline = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var reformed = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        reformed.World.Policy.WomenRights = WomenRightsLaw.Property;   // 旧支配層の抵抗を近代エリート＋大衆が押し切る（英1870年法）
        baseline.Run(120);
        reformed.Run(120);
        Assert.True(reformed.World.WomenPropertyLaw, "イギリスで女性財産権の段に到達しない");
        // 法→参加率の配線を直接検証（0.4→0.7＝労働供給の能力+2割）。
        // ※産出・賃金の均衡比較は120四半期の軌道分岐（政治・産業構成の違い）に埋もれて脆いため、機構の検証に留める。
        Assert.Equal(0.7, reformed.World.Region.FemaleParticipation, 2);
        Assert.Equal(0.4, baseline.World.Region.FemaleParticipation, 2);
    }

    [Fact]
    public void 女性参政権は前提法なしに成立しない()   // 自分の財産/票を持てない者に票はない（史実の順序）＝Universal（男女普選）は梯子の最上段を要する
    {
        var sim = new Simulation(NationConfig.America().ToScenario(71), new NullAgent());   // 共和制＝参政権が動きやすい国
        sim.World.Policy.Suffrage = Suffrage.Universal;   // 前提法なしで普通選挙を要求しても…
        sim.Run(120);
        // 守るべき不変条件＝Universal が立っているなら女性の権利の梯子が最上段（選挙権）まで登っているはず。
        // 旧「Suffrage<=AdultMale」は「前提法は120四半期で通らない」という軌道依存の仮定に乗っていた（R10識字減衰で共和制の米の軌道が変わり、
        //   米が前提法を正規に通してUniversalへ到達＝機構は正常）。前提法なしのUniversal（本当のバグ）は依然この不変条件が捕捉する。
        Assert.True(sim.World.Suffrage < Suffrage.Universal || sim.World.WomenSuffrage,
            $"前提法なしで女性参政権（Universal）が成立した（Suffrage={sim.World.Suffrage}, 女性の権利={sim.World.WomenRights}）");
    }

    [Fact]
    public void 男女普通選挙は女性の権利が剥がれても黙って降格しない()   // (d) 2026-09-12 FIX3-1＝天井型の前提が下げる側も動かしていたバグ
    {
        // 旧＝Government.Step が「目標が Universal で女性の権利が選挙権未満なら目標を AdultMale へ書き換える」1行を持っていた。
        //   登る側は LawPrereq（case Suffrage）が既に塞いでいるので、その1行の固有の産物は**降りる側だけ**＝
        //   断裂の窓（内戦・クーデタの直後は不可逆が解除される）に女性の権利が剥がれた国で、誰も選んでいない参政権の降格が黙って起きた。
        // 新＝目標はそのまま渡す＝現行＝目標で StepEnum が即 return（前提は登る側の門が持つ）。
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        w.PoliticalLockEnabled = false;                  // 純支持ではなく「目標の書き換え」だけを測る
        w.Suffrage = Suffrage.Universal;                 // 既に男女普通選挙が成立している国
        w.Policy.Suffrage = Suffrage.Universal;          // 誰も降格を選んでいない
        w.WomenRights = WomenRightsLaw.Education;        // 反動が女性の権利を選挙権未満へ剥がした（内戦の譲歩・断裂の窓）
        w.Policy.WomenRights = WomenRightsLaw.Education;
        w.RuptureWindowQuarters = 8;                     // 窓が開いている＝不可逆は後退を止めない
        Assert.False(w.WomenSuffrage, "前提が崩れた：女性の権利が選挙権のまま＝降格の経路を測れない");
        for (int q = 0; q < 8; q++) Government.Step(w, w.Coef);
        Assert.Equal(Suffrage.Universal, w.Suffrage);            // 黙って降格しない
        Assert.Equal(Suffrage.Universal, w.Policy.Suffrage);     // 目標も書き換えられない＝w≠p の幽霊戦線が立たない
        // 登る側の門は残る＝前提なしに Universal へは上がれない（単一ソース＝LawPrereq）
        w.Suffrage = Suffrage.AdultMale;
        Assert.False(Government.LawPrereq(w, PolicyAxis.Suffrage, (int)Suffrage.Universal, w.Coef).Ok);
    }

    [Fact]
    public void 女性の権利は段を飛ばせない()   // ★統合法（2026-08-10）：財産権→教育権→選挙権の順序を「段の並び」だけで保証する（前提法DAG＝純関数なので軌道に依存しない）
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        var c = w.Coef;
        w.WomenRights = WomenRightsLaw.None;
        Assert.True(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Property, c).Ok, "なしから財産権の段へ上がれない");
        Assert.False(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Education, c).Ok, "なしから教育権の段を飛び越せてしまう");
        Assert.False(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Suffrage, c).Ok, "なしから選挙権の段を飛び越せてしまう");
        w.WomenRights = WomenRightsLaw.Property;
        Assert.True(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Education, c).Ok, "財産権から教育権の段へ上がれない");
        Assert.False(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Suffrage, c).Ok, "財産権から選挙権の段を飛び越せてしまう");
        // 選挙権の段は男性普通選挙も前提（男に普通選挙の無い国が女に与えた例は史実にない）
        w.WomenRights = WomenRightsLaw.Education;
        w.Suffrage = Suffrage.Census;
        Assert.False(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Suffrage, c).Ok, "制限選挙のまま女性の選挙権へ上がれてしまう");
        w.Suffrage = Suffrage.AdultMale;
        Assert.True(Government.LawPrereq(w, PolicyAxis.WomenRights, (int)WomenRightsLaw.Suffrage, c).Ok, "男性普通選挙があっても女性の選挙権へ上がれない");
    }

    [Fact]
    public void 女性の権利は梯子を降りない()   // ★統合法：制度は撤回されない（旧＝教育権だけ撤回可という非対称があった）
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        foreach (var stage in new[] { WomenRightsLaw.Property, WomenRightsLaw.Education, WomenRightsLaw.Suffrage })
        {
            w.WomenRights = stage;
            var (ok, _, reason) = Government.GateInfo(w, PolicyAxis.WomenRights, increasing: false, targetLevel: (int)stage - 1);
            Assert.False(ok, $"{stage} の段から後退できてしまう（{reason}）");
        }
    }

    [Fact]
    public void 女性の権利の段は旧3法の効果へ単調に写る()   // ★統合法：段nに達したら旧法nがONだったのと同じ効果になる（挙動の連続性）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var w = sim.World;
        (WomenRightsLaw Stage, bool Prop, bool Edu, bool Suf)[] expect =
        [
            (WomenRightsLaw.None,      false, false, false),
            (WomenRightsLaw.Property,  true,  false, false),
            (WomenRightsLaw.Education, true,  true,  false),
            (WomenRightsLaw.Suffrage,  true,  true,  true),
        ];
        foreach (var (stage, prop, edu, suf) in expect)
        {
            w.WomenRights = stage;
            Assert.Equal(prop, w.WomenPropertyLaw);
            Assert.Equal(edu, w.WomenEducationLaw);
            Assert.Equal(suf, w.WomenSuffrage);
        }
    }
}
