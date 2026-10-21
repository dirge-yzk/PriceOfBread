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
        Assert.Equal(0.0, soldier.AgeShare(0, double.PositiveInfinity, 1));   // 兵士は男性のみ（年齢の層の女の層が0）
        double fem = peasant.AgeShare(20, double.PositiveInfinity, 1) / peasant.AgeShare(20, double.PositiveInfinity);   // 20歳以上のうちの女性
        Assert.True(fem > 0.5, $"男手が抜けたのに農民の女性比率が上がらない（{fem:F3}）");
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
        //   ★(383) 浸透は法の浸透の門で追従する（旧は移動平均）＝120 四半期では 0.7 の手前（0.692）にいることがある。★(399) 門〔LawEnforcementAdminGate〕は削除済み＝浸透は活きている州の人口で重みづけた国家の区分で読む（(402)）。配線は〈基線 0.4 を明らかに超え 0.7 を越えない〉で見る。
        Assert.InRange(reformed.World.Region.FemaleParticipation, 0.5, 0.7 + 1e-9);   // ★(408) 追従は法の浸透×官僚の役務の充足で進む（(408) の行政費の返り・旗は (419) で焼き込み）＝120 四半期では 0.6 前後（旧 0.69）。0.4 から確かに上がる配線の確認に留める
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

/// <summary>★年齢の層（2026-09-30 段4e 後半・設計 20260929_年齢の層）＝POP ごとの10歳刻み8層×男女の割合16本の番人（機構と不変条件）。
/// 加齢は規模を変えず層×性の人数を上へ送る・標準のピラミッドは1783年の出生と死の年齢の重みで動かない・人の移し方は層×性ごとの人数を保存する・
/// 災害死は子供と老年に厚い・兵士は男だけ。</summary>
public class AgeStructureTests
{
    private static double[] Counts(Pop p) { var n = new double[p.Ages.Length]; for (int k = 0; k < n.Length; k++) n[k] = p.Ages[k] * p.Size; return n; }

    // ★(478) 層の数は環境変数 POB_AGE_BANDS で選ぶ器（既定は作者の11層・8は旧）＝この番人は層の器から引く（層の数・幅・境の年齢を直書きしない）。
    private static AgeLayout L => Pop.Layout;
    /// <summary>年齢 <paramref name="age"/> を含む層（下端が age 以下の最大の層）。</summary>
    private static int BandAt(double age) { int b = 0; for (int i = 0; i < L.Bands; i++) if (L.Lower[i] <= age) b = i; return b; }
    /// <summary>年齢 <paramref name="age"/> を含む層の下端（層の中は一様に読まれる＝層の境に揃わない年齢は下の境まで広がる）。</summary>
    private static double LowerOfBandAt(double age) => L.Lower[BandAt(age)];
    /// <summary>年齢 <paramref name="age"/> を含む層の上端（最上層は無限大）。</summary>
    private static double UpperOfBandAt(double age) { int b = BandAt(age); return b == L.Bands - 1 ? double.PositiveInfinity : L.Upper[b]; }
    /// <summary>★(490) 60歳以上の割合がちょうど <paramref name="oldShare"/> の構成（男女半々・和1）。60歳をまたぐ層（11層の 51〜60 歳の層）には質量を置かない＝層の中は一様に読まれるので、またぐ層の一部が 60 歳以上に混ざらない。
    /// 若い層（上端が 60 歳以下）に残りを均等に、老いた層（下端が 60 歳以上）に <paramref name="oldShare"/> を均等に置く。期待の算術は器の定数でなく「老年の割合」そのもの。</summary>
    private static double[] OldShareAges(double oldShare)
    {
        var a = new double[2 * L.Bands]; var young = new List<int>(); var old = new List<int>();
        for (int b = 0; b < L.Bands; b++)
        {
            double up = b == L.Bands - 1 ? double.PositiveInfinity : L.Upper[b];
            if (up <= 60) young.Add(b); else if (L.Lower[b] >= 60) old.Add(b);
        }
        foreach (int b in young) a[2 * b] = a[2 * b + 1] = (1.0 - oldShare) / young.Count / 2.0;
        foreach (int b in old) a[2 * b] = a[2 * b + 1] = oldShare / old.Count / 2.0;
        return a;
    }
    /// <summary>指定の層だけに質量を置いた構成（男女半々・和1）。</summary>
    private static double[] MassIn(params int[] bands) { var a = new double[2 * L.Bands]; foreach (int b in bands) { a[2 * b] = 0.5 / bands.Length; a[2 * b + 1] = 0.5 / bands.Length; } return a; }
    /// <summary>標準のピラミッドの若い層（大人に入る層まで）を半分にして老いた構成（男女半々・和1）。</summary>
    private static double[] AgedAges() { var a = new double[2 * L.Bands]; double s = 0; for (int b = 0; b < L.Bands; b++) { double v = L.Pyramid[b] * (b <= L.EntryBand ? 0.5 : 1.0); a[2 * b] = a[2 * b + 1] = v / 2; s += v; } for (int k = 0; k < a.Length; k++) a[k] /= s; return a; }

    [Fact]
    public void 加齢は規模を変えず毎月幅の12か月分の1を上の層へ送る()
    {
        var p = new Pop { Size = 1000 };
        var before = Counts(p);
        var mortW = Pop.MortalityWeights(new Coefficients().CrudeBirthRatePerYear.Value);
        p.StepAges(p.Size, 0, 0, mortW);
        Assert.Equal(1000.0, p.Size);   // 構成だけが動く
        var after = Counts(p);
        int top = L.Bands - 1;
        for (int s = 0; s < 2; s++)
        {
            double b0 = 0, a0 = 0;
            for (int b = 0; b < L.Bands; b++) { b0 += before[2 * b + s]; a0 += after[2 * b + s]; }
            Assert.Equal(b0, a0, 9);   // 性ごとの人数は保存（出生と死が無い）
            // 最初の層から出ていく1か月の流れ＝n0÷（幅×12か月）（層の中は一様）＝8層は 10歳×12＝120・11層は 1歳×12＝12（手の値で突き合わせる）
            double perMonth0 = L.Bands == 8 ? 120.0 : 12.0;
            Assert.Equal(before[s] - before[s] / perMonth0, after[s], 9);
            // 最上層は死でだけ出る＝その下の層から入るだけ（8層は 60〜69 歳・11層は 61〜70 歳＝どちらも幅10年）
            Assert.Equal(before[2 * top + s] + before[2 * (top - 1) + s] / 120.0, after[2 * top + s], 9);
        }
    }

    [Fact]
    public void 標準のピラミッドは1783年の出生と死の年齢の重みで動かない()
    {
        var c = new Coefficients();
        var p = new Pop { Size = 1000 };
        var mortW = Pop.MortalityWeights(c.CrudeBirthRatePerYear.Value);
        foreach (var w in mortW) Assert.True(w >= 0);
        for (int m = 0; m < 120; m++)
        {
            double births = Calendar.PerYearToPerMonth(c.CrudeBirthRatePerYear.Value) * p.Size, deaths = Calendar.PerYearToPerMonth(c.CrudeBirthRatePerYear.Value - Pop.StdPyramidGrowth) * p.Size;
            p.StepAges(p.Size, births, deaths, mortW);
            p.Size += births - deaths;
        }
        var std = Pop.StdAges();   // 流れは全て月初の人数から同時に＝丸めの誤差だけ
        for (int k = 0; k < std.Length; k++) Assert.Equal(std[k], p.Ages[k], 12);
        // 最上層（70以上／71以上）の死亡率は働き盛り（20〜29／21〜30）より高い（老衰）・最初の層（0〜9／0歳）は働き盛りより高い（乳幼児）
        int prime = L.AdultFirstBand;
        Assert.True(mortW[2 * (L.Bands - 1)] > 3 * mortW[2 * prime] && mortW[0] > mortW[2 * prime]);
    }

    [Fact]
    public void 移し方は層と性ごとの人数を保存し選抜の無い移動は送り手を動かさない()
    {
        var from = new Pop { Size = 800 }; var to = new Pop { Size = 200 };
        to.SetAdultFemaleShare(0.75);
        var s0 = (double[])from.Ages.Clone();
        int n = 2 * L.Bands;
        var sum0 = new double[n]; var fc = Counts(from); var tc = Counts(to);
        for (int k = 0; k < n; k++) sum0[k] = fc[k] + tc[k];
        Pop.Blend(from, to, 100);
        for (int k = 0; k < n; k++) Assert.Equal(s0[k], from.Ages[k]);   // 1ビットも動かない
        fc = Counts(from); tc = Counts(to);
        for (int k = 0; k < n; k++) Assert.Equal(sum0[k], fc[k] + tc[k], 9);
        // 徴兵＝男だけ・移る組は徴兵の年齢の男（20〜39）＝兵士に女と若年（20歳を含む層の下端より下）と 40以上（39歳を含む層の上端より上）は入らず・送り手の女性の人数は動かない
        // ⚠11層は 20 歳が 16〜20 歳の層の中・39 歳が 31〜40 歳の層の中＝層の境に揃わない年齢は層の中を一様に読まれる（事前登録の追記 1 [A2]・読み手の境は別の線）＝不変条件は性と層の外側
        var soldier = new Pop { Size = 50, IsSoldier = true, Ages = Pop.SoldierAges() };
        double femFrom0 = 0; fc = Counts(from); for (int b = 0; b < L.Bands; b++) femFrom0 += fc[2 * b + 1];
        Pop.Blend(from, soldier, 60, draft: true);
        for (int b = 0; b < L.Bands; b++) Assert.Equal(0.0, soldier.Ages[2 * b + 1]);
        Assert.Equal(0.0, soldier.AgeShare(0, LowerOfBandAt(20)) + soldier.AgeShare(UpperOfBandAt(39), double.PositiveInfinity));
        // 皆兵＝池を男49歳まで広げる（Military.DraftAgeHi）
        var reserve = new Pop { Size = 1, IsSoldier = true, Ages = Pop.SoldierAges() };
        Pop.Blend(from, reserve, 60, draft: true, draftHi: 50);
        Assert.True(reserve.AgeShare(40, 50, 0) > 0.1 && reserve.AgeShare(UpperOfBandAt(49), double.PositiveInfinity) == 0);
        double femFrom1 = 0; fc = Counts(from); for (int b = 0; b < L.Bands; b++) femFrom1 += fc[2 * b + 1];
        Assert.Equal(femFrom0, femFrom1, 9);
        double sum = 0; foreach (var x in soldier.Ages) sum += x;
        Assert.Equal(1.0, sum, 12);
    }

    [Fact]
    public void 災害死は子供と老年に厚く総数は規模どおり()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        var p = new Pop { Size = 1000 };
        double child0 = p.AgeShare(0, 10), old0 = p.AgeShare(60, double.PositiveInfinity), mid0 = p.AgeShare(20, 50);
        var aw = new double[2 * L.Bands]; Pop.DisasterWeights(aw, 0.4);
        double dead = w.KillPops([p], null, 100, violent: false, aw);
        Assert.Equal(100.0, dead, 9);
        Assert.Equal(900.0, p.Size, 9);
        Assert.True(p.AgeShare(0, 10) < child0 && p.AgeShare(60, double.PositiveInfinity) < old0 && p.AgeShare(20, 50) > mid0,
            $"災害死が子供と老年に厚くない（0〜9 {child0:F4}→{p.AgeShare(0, 10):F4}・60以上 {old0:F4}→{p.AgeShare(60, double.PositiveInfinity):F4}）");
        // 年齢を選ばない死（戦死の按分・粛清）は全層に比例＝割合は1ビットも動かない
        var q = new Pop { Size = 1000 }; var a0 = (double[])q.Ages.Clone();
        w.KillPops([q], null, 100, violent: true);
        for (int k = 0; k < a0.Length; k++) Assert.Equal(a0[k], q.Ages[k]);
    }

    [Fact]
    public void 読み手の起点合わせは標準のピラミッドで旧の値を保つ()
    {
        var c = new Coefficients();
        var p = new Pop { Size = 1000 };
        // ★(145) 兵役の母数＝働き盛りの男（16〜59歳）の実数（起点合わせをやめた＝上限と徴兵の割合を働き盛りの男で読む）＝標準の POP は規模×標準の構成の働き盛りの男の割合
        Assert.Equal(1000.0 * Military.WorkingMenStdShare, Military.RecruitMen(p), 9);
        // 標準のピラミッドの男16〜59の手の値：8層＝0.5×(0.195×0.4＋0.160＋0.130＋0.105＋0.080)＝0.2765／11層＝0.5×(0.0903＋0.1568＋0.1273＋0.1025＋0.0770×0.9)＝0.2731（16〜20歳の層は全部・51〜60歳の層は 59 歳まで 9/10・事前登録 [3] の表）
        Assert.Equal(L.Bands == 8 ? 0.2765 : 0.2731, Military.WorkingMenStdShare, L.Bands == 8 ? 12 : 4);
        // 男手が抜けた村は母数も細る（空洞の年次）＝男の徴兵の層（20〜39／21〜40）（母数の過半）が0 の POP は母数が約半分
        var q = new Pop { Size = 1000 }; q.Ages[2 * L.SoldierBandA] = 0; q.Ages[2 * L.SoldierBandB] = 0; double s = 0; foreach (var x in q.Ages) s += x; for (int k = 0; k < q.Ages.Length; k++) q.Ages[k] /= s;
        Assert.True(Military.RecruitMen(q) < 0.6 * Military.RecruitMen(p), $"男20〜39 がいない POP の兵役の母数が細らない（{Military.RecruitMen(q):F3}／標準 {Military.RecruitMen(p):F3}）");
        // 教育の必要量＝学齢（5〜14歳）×EduPerChild＝人口あたり0.06（旧＝子供比0.3×0.2）
        Assert.Equal(0.06, c.EduPerChild * p.AgeShare(5, 15), 12);
    }

    [Fact]
    public void 年金の利害は標準のピラミッドより老いた分だけ()   // 設計書 §4-1「PensionOldWeight を標準の値で割り直す」＝起点合わせ
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        foreach (var q in w.AllPops()) q.Ages = Pop.StdAges();   // 標準のピラミッド（構築時の徴兵で農民と労働者の男20〜39 が少し抜けている＝揃える。★(471) 兵士も年金の受益者に数える＝兵士も揃える）
        var on = PolicyPreferences.Stance(w, PolicyAxis.SocialSecurity, true);
        double k = w.Coef.PensionOldWeight;
        w.Coef.PensionOldWeight = 0;
        var off = PolicyPreferences.Stance(w, PolicyAxis.SocialSecurity, true);
        Assert.Equal(off.Mass, on.Mass, 12);   // 1783年（全ての POP が標準のピラミッド）は受益者項0＝旧の開幕（老年比0）と同じ
        Assert.Equal(off.Trad, on.Trad, 12);
        w.Coef.PensionOldWeight = k;
        foreach (var q in w.AllPops()) q.Ages = OldShareAges(0.18);   // 60歳以上 18%（★(490) 層の器から組む＝8層でも11層でも同じ読み〔旧は 8 層の 16 本を直書き〕）
        var aged = PolicyPreferences.Stance(w, PolicyAxis.SocialSecurity, true);
        Assert.Equal(off.Mass + k * (0.18 - Pop.StdShare(60, double.PositiveInfinity)), aged.Mass, 9);   // 老いた分だけ年金を推す
    }

    [Fact]
    public void 将校の戦死は貴族の男20から49歳から()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        var nob = new Pop { Size = 1000, Closed = true };
        // ⚠11層は 20 歳が 16〜20 歳の層の中・49 歳が 41〜50 歳の層の中＝層の境に揃わない年齢は層の中を一様に読まれる（追記 1 [A2]）＝将校の死は 20 歳を含む層の下端から 49 歳を含む層の上端までの男
        double lo = LowerOfBandAt(20), hi = UpperOfBandAt(49);
        double fem0 = nob.AgeShare(0, double.PositiveInfinity, 1) * nob.Size, kids0 = nob.AgeShare(0, lo) * nob.Size, men0 = nob.AgeShare(lo, hi, 0) * nob.Size;
        var ow = (double[])typeof(Diplomacy).GetField("OfficerAgeWeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        double dead = w.KillPops([nob], null, 30, violent: true, ow);
        Assert.Equal(30.0, dead, 9);
        Assert.Equal(fem0, nob.AgeShare(0, double.PositiveInfinity, 1) * nob.Size, 9);   // 女性は死なない
        Assert.Equal(kids0, nob.AgeShare(0, lo) * nob.Size, 9);                         // 子供も死なない
        Assert.Equal(men0 - 30.0, nob.AgeShare(lo, hi, 0) * nob.Size, 9);
    }

    /// <summary>★(142) 4e-2b・思想⑨：疫病の年齢の脆弱さは1本＝主食の充足が同じ POP の間では、年齢の層ごとの1人あたりの死の率が同じ
    /// （POP の間の重み＝規模×(1＋EpidemicVulnConcentration×飢え)×Σ層の割合×年齢の重み・POP の中＝同じ年齢の重み DisasterWeights）。
    /// 旧＝POP の間の重みが子供比＋老年比も読み、POP の中の年齢の重みと同じ脆弱さを2度読んだ＝子供の多い POP の子供は標準の POP の子供より多く死んだ。
    /// ⚠突然変異「POP の間の重みに子供比＋老年比を戻す」で赤。</summary>
    [Fact]
    public void 疫病の死は年齢の脆弱さを1度だけ読む()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        var a = new Pop { Size = 1000 };   // 標準のピラミッド
        var b = new Pop { Size = 1000 };   // 子供の多い POP（0〜9 の層を3倍）
        b.Ages[0] *= 3; b.Ages[1] *= 3; double s = 0; foreach (var x in b.Ages) s += x; for (int k = 0; k < b.Ages.Length; k++) b.Ages[k] /= s;
        a.FoodNeed = b.FoodNeed = 1; a.FoodMet = b.FoodMet = 0.7;   // 同じ主食の充足
        var r = new Region { Pops = [a, b] };
        double[] na = new double[a.Ages.Length], nb = new double[a.Ages.Length];
        for (int k = 0; k < na.Length; k++) { na[k] = a.Ages[k] * a.Size; nb[k] = b.Ages[k] * b.Size; }
        var m = typeof(World).GetMethod("ApplyEpidemicDeaths", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        double dead = (double)m.Invoke(w, [r, w.Coef, 1.0, 1.0])!;
        Assert.True(dead > 1, $"前提が崩れた：死者 {dead}");
        for (int k = 0; k < na.Length; k++)
        {
            double ra = (na[k] - a.Ages[k] * a.Size) / na[k], rb = (nb[k] - b.Ages[k] * b.Size) / nb[k];
            Assert.True(ra > 0, $"前提が崩れた：層 {k} で死がない");
            Assert.True(Math.Abs(ra - rb) <= 1e-9 * ra, $"層 {k} の1人あたりの死の率が POP で違う（標準 {ra:G6}・子供の多い POP {rb:G6}）＝年齢の脆弱さを2度読んでいる");
        }
    }

    [Fact]
    public void 世代の流れは大人と20歳になる若者を人数で混ぜる()   // 4e-3（設計書 §5-4）
    {
        var p = new Pop { Size = 1000 };
        double e = 3 * p.EnteringAdultsMonthly(), a = p.AgeShare(L.AdultAge, double.PositiveInfinity);   // 大人の境（8層は20歳・11層は21歳＝層の境）
        // x は必ず x と x_in の間の加重平均＝(A·x＋E·x_in)÷(A＋E)
        Assert.Equal((a * 0.8 + e * 0.2) / (a + e), p.GenerationalMix(0.8, 0.2, 3), 15);
        Assert.Equal(0.8, p.GenerationalMix(0.8, 0.8, 3), 15);   // 伝わる値が同じなら動かない
        var noYouth = new Pop { Size = 1000, Ages = MassIn(L.AdultFirstBand) };
        Assert.Equal(0.8, noYouth.GenerationalMix(0.8, 0.2, 3));   // 大人になる若者がいない（E＝0）なら動かない
        // 国の置き換わり率＝ΣE÷Σ(A＋E)（規模で割らない）＝標準のピラミッドで四半期 約0.88%（半減期 約20年＝旧の固定の半減期 80四半期とほぼ同じ）
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        double r = w.ReplacementRate(3);
        Assert.Equal(Pop.StdReplacementRate(3), r, 3);
        Assert.InRange(System.Math.Log(2) / r / 4.0, 18.0, 22.0);
        // 老いた国は置き換わりが遅い＝記憶が長い
        foreach (var q in w.AllPops()) { q.Ages = AgedAges(); }   // 若い層（大人に入る層まで）を半分にした構成
        Assert.True(w.ReplacementRate(3) < r, "老いた国の置き換わり率が下がらない");
    }

    [Fact]
    public void 識字の下がる側は世代の流れで大人は字を忘れない()   // 4e-3（設計書 §5-2）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var w = sim.World; var c = w.Coef;
        w.WarmupFrozen = false;
        var p = w.Region.Pops[1];
        p.Literacy = 0.8; p.EducationFill = 0.0;   // 支えの無い識字（学校が無い）
        double motherLit = c.FemaleLitFloor + (1.0 - c.FemaleLitFloor) * w.WomenWageRetention;
        if (c.WomenEducationGate > 0 && w.WomenEducationLaw) motherLit = System.Math.Min(1.0, motherLit * (1.0 + c.WomenEduLitBonus));
        double expect = p.GenerationalMix(0.8, motherLit * 0.8, 1);   // 若者は家庭で母の識字の比×大人の識字を持ち込む
        TechEducation.GrowLiteracy(w, c);
        Assert.Equal(expect, p.Literacy, 12);
        Assert.True(p.Literacy < 0.8 && p.Literacy > 0.8 * motherLit);
        // 若者がいない POP（E＝0）は字を忘れない
        var q = w.Region.Pops[2];
        q.Ages = MassIn(L.AdultFirstBand);
        q.Literacy = 0.7; q.EducationFill = 0.0;
        TechEducation.GrowLiteracy(w, c);
        Assert.Equal(0.7, q.Literacy);
    }

    [Fact]
    public void 試合の中で年齢の層は壊れず兵士は男だけ()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(61), new NullAgent());   // 大陸軍＝重い徴兵
        sim.Run(40);   // 10年（毎月の CheckInvariants が合計1と非負を見る）
        double kids = 0, tot = 0, sold40 = 0;
        foreach (var p in sim.World.AllPops())
        {
            // ⚠11層は徴兵の下端 20 歳が 16〜20 歳の層の中＝兵士の約 6% が層の中で 16〜20 歳に見える（追記 1 [A2]）＝不変条件は性と〈20 歳を含む層の下端より下には居ない〉
            if (p.IsSoldier) { for (int b = 0; b < L.Bands; b++) Assert.Equal(0.0, p.Ages[2 * b + 1]); Assert.Equal(0.0, p.AgeShare(0, LowerOfBandAt(20))); sold40 += p.AgeShare(40, double.PositiveInfinity) * p.Size; }
            kids += p.AgeShare(0, 15) * p.Size; tot += p.Size;
        }
        Assert.InRange(kids / tot, 0.2, 0.5);
        Assert.True(sold40 > 0, "兵士が年を取らない（徴兵は男20〜39 だけ＝40歳以上は加齢でしか入らない）");
    }
}
