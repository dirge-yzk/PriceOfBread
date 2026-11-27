using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1399) 線 c149wx：旗 <see cref="Coefficients.WarExitFromCapacity"/>（段 2 の器・既定 0）の番人。旗 1 の枝は、戦争の出口を厭戦でなく
/// 〈持ちこたえる力 K′＝強制力÷(強制力＋挑戦)〉と戦勝点で読み、開戦・参戦・調停の門の国難を〈内戦だけ〉にする。
/// 盤面は試験の中で置く。期待値は試験の中の式（K′ の置き方は 強制力×人口÷基準人口 を自分で組む）から作り、エンジンの WarCapacity・KOf・WarEnds は呼ばない。
/// 旗 0 の対照（旧の規則が続く）を各 1 本置く。</summary>
[Collection("Instruments")]
public class WarExitFromCapacityTests
{
    private static MultiNationSim Make(double flag, params string[] names)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(names, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.WarExitFromCapacity = flag;
        c.WarLuck = 0; c.WarPushSpeed = new FractionPerMonth(0); c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.TreatyPortWinnerDistanceKm = 0; c.ConcertReparationRestraint = 0; c.ColonyCaptureBudget = 0; c.TerritoryCessionScore = 1e9;
        foreach (var n in sim.Nations) n.World.SelfPower!.WarExhaustion = 0;
        return sim;
    }

    /// <summary>国 w の K′ が k になるよう強制力と挑戦を置く（式は試験の中で組む：強制力＝Coercion×人口÷基準人口）。k＝0 は強制力 0・挑戦 1、k＝1 は挑戦 0。</summary>
    private static void SetK(World w, Coefficients c, double k)
    {
        w.RadicalPower = 0;
        if (k <= 0) { w.Coercion = 0; w.ReformistPower = 1; return; }
        w.Coercion = 5.0;
        double coerce = w.Coercion * (w.TotalPopulation / System.Math.Max(1.0, c.PoliticalReferencePop));
        Assert.True(coerce > 0, "前提が崩れた：国の強制力が 0");
        w.ReformistPower = k >= 1 ? 0 : coerce * (1 - k) / k;
    }

    private static War Pair(MultiNationSim sim, double score, double kA, double kB)
    {
        var a = sim.Nations[0].World; var b = sim.Nations[1].World;
        SetK(a, sim.Coef, kA); SetK(b, sim.Coef, kB);
        var war = new War { A = a.SelfPower!, B = b.SelfPower!, Score = score };
        sim.Diplomacy.Wars.Add(war);
        return war;
    }

    private static void Close(double want, double got) => Assert.True(System.Math.Abs(want - got) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(want)), $"期待 {want:G17}・実 {got:G17}");

    private static bool Ends(double flag, double score, double kA, double kB, out double recordedScore)
    {
        var sim = Make(flag, "フランス", "イギリス");
        var war = Pair(sim, score, kA, kB);
        int e0 = sim.Diplomacy.EndedWars.Count;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        bool ended = !sim.Diplomacy.Wars.Contains(war);
        recordedScore = ended && sim.Diplomacy.EndedWars.Count > e0 ? sim.Diplomacy.EndedWars[^1].Score : double.NaN;
        return ended;
    }

    [Fact]
    public void 旗1は戦勝点がBの持ちこたえる力に届けば講和し戦勝点はそのまま()   // ①
    {
        Assert.True(Ends(1, 0.4, 1.0, 0.39, out var s), "旗 1：S=0.4 ≥ K′_B=0.39 で終わらない");
        Close(0.4, s);
    }

    [Fact]
    public void 旗0は同じ盤面で終わらない()   // ① の対（旧の規則：厭戦 0 ・戦勝点 0.4 は続く）
    {
        Assert.False(Ends(0, 0.4, 1.0, 0.39, out _), "旗 0：旧の規則で終わってしまった（旗が漏れている）");
    }

    [Fact]
    public void 旗1はAの持ちこたえる力が低ければBの勝ちで講和する()   // ③
    {
        Assert.True(Ends(1, -0.3, 0.2, 0.9, out var s), "旗 1：−S=0.3 ≥ K′_A=0.2 で終わらない");
        Close(-0.3, s);
    }

    [Fact]
    public void 旗1は両側の力が0なら白紙で講和する()   // ④
    {
        Assert.True(Ends(1, 0.0, 0.0, 0.0, out var s), "旗 1：K′=0 の両側で終わらない");
        Close(0.0, s);
    }

    [Fact]
    public void 旗1は両側の力が1なら戦勝点の絶対値が1に届く月に終わる()   // ⑤
    {
        Assert.False(Ends(1, 0.99, 1.0, 1.0, out _), "S=0.99 で終わってしまった");
        Assert.True(Ends(1, 1.0, 1.0, 1.0, out _), "S=1.0 で終わらない（今の決定的の線と同じ月）");
        Assert.True(Ends(1, -1.0, 1.0, 1.0, out _), "S=−1.0 で終わらない");
    }

    [Fact]
    public void 旗1は押しの和が0で戦勝点が動かないなら終わらない()   // ⑨：K′ 両側 0.5・S=0 を 12 か月
    {
        var sim = Make(1, "フランス", "イギリス");
        var war = Pair(sim, 0.0, 0.5, 0.5);
        for (int m = 0; m < 12; m++) sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.Contains(war, sim.Diplomacy.Wars);
        Close(0.0, war.Score);
    }

    [Fact]
    public void 旗1は挑戦も強制力も無い国の持ちこたえる力を1とする()   // ⑩：強制力 0・挑戦 0 の両側は S=0.99 で終わらず S=1.0 で終わる（K′=1）
    {
        foreach (var (s, want) in new[] { (0.99, false), (1.0, true) })
        {
            var sim = Make(1, "フランス", "イギリス");
            var a = sim.Nations[0].World; var b = sim.Nations[1].World;
            foreach (var w in new[] { a, b }) { w.Coercion = 0; w.ReformistPower = 0; w.RadicalPower = 0; }
            var war = new War { A = a.SelfPower!, B = b.SelfPower!, Score = s };
            sim.Diplomacy.Wars.Add(war);
            sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
            Assert.Equal(want, !sim.Diplomacy.Wars.Contains(war));
        }
    }

    private static Coefficients Gate(Coefficients c) { c.DefendProjectionMin = 0; return c; }

    [Theory]
    [InlineData(1, true)]    // ⑥ 旗 1：厭戦では門は閉じない
    [InlineData(0, false)]   // ⑥ 旗 0：厭戦 0.9 で門は閉じる
    public void 開戦の門は旗1で厭戦を読まない(double flag, bool open)
    {
        var sim = Make(flag, "フランス", "イギリス");
        var c = Gate(sim.Coef);
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!;
        sim.Diplomacy.AddTension(a, b, c.WarDeclareTension + 50, TensionReason.Initial);
        a.WarExhaustion = 0.9; a.Nation!.Legitimacy = 0.3;
        Assert.True(sim.Diplomacy.Tension(a, b) >= c.WarDeclareTension, "前提が崩れた：緊張が開戦の線に届いていない");
        Assert.Equal(open, sim.Diplomacy.WarGateOpen(a, b, c));
    }

    [Fact]
    public void 開戦の門は旗1でも旗0でも相手が内戦中なら閉じない()   // ⑦（性質の番人・旗 1 が国難の国を攻められないようにする突然変異で赤）
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var sim = Make(flag, "フランス", "イギリス", "スペイン");
            var c = Gate(sim.Coef);
            var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!; var rebel = sim.Nations[2].World.SelfPower!;
            sim.Diplomacy.Wars.Add(new War { A = b, B = rebel, CivilWarParent = rebel });
            Assert.True(b.Nation!.InCivilWar, "前提が崩れた：b が内戦中でない");
            sim.Diplomacy.AddTension(a, b, c.WarDeclareTension + 50, TensionReason.Initial);
            Assert.True(sim.Diplomacy.WarGateOpen(a, b, c));
        }
    }

    private static void Ally(Diplomacy d, Power a, Power b)
    {
        var set = (System.Collections.Generic.HashSet<long>)typeof(Diplomacy).GetField("_alliance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(d)!;
        set.Add((long)typeof(Diplomacy).GetMethod("Key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [a, b])!);
    }

    /// <summary>同盟国が参戦するか：攻め手 fr が es に宣戦・es の同盟国 ru。ru の正統性・内戦を置く。戻り値＝(ru が fr と交戦したか, es と ru の緊張の増え)。</summary>
    private static (bool joined, double refusal) AllyJoin(double flag, double legitimacy, bool civil)
    {
        var sim = Make(flag, "フランス", "イギリス", "スペイン", "ロシア");
        sim.StepQuarter();
        var c = sim.Coef;
        c.WarExitFromCapacity = flag; c.DefendProjectionMin = 0; c.AllyJoinTension = 1;
        var dip = sim.Diplomacy;
        var att = sim.Nations[0].World.SelfPower!; var trg = sim.Nations[2].World.SelfPower!; var ally = sim.Nations[3].World.SelfPower!; var other = sim.Nations[1].World.SelfPower!;
        Ally(dip, trg, ally);
        dip.SeedTension(ally, att, 100);
        ally.Military = 1e9; att.Military = 1;
        ally.WarExhaustion = 0; ally.Nation!.Legitimacy = legitimacy; ally.Nation.FoodFill = 1.0;
        if (civil) dip.Wars.Add(new War { A = ally, B = other, CivilWarParent = other });
        double t0 = dip.Tension(trg, ally);
        Assert.True(dip.DeclareWar(att, trg, c), "前提が崩れた：宣戦できない");
        return (dip.IsAtWar(ally, att), dip.Tension(trg, ally) - t0);
    }

    [Fact]
    public void 同盟の参戦は旗1で正統性が低くても内戦でなければ参戦する()   // ⑪
    {
        var (joined, _) = AllyJoin(1, 0.3, false);
        Assert.True(joined, "旗 1：正統性 0.3・内戦なしの同盟国が参戦しない");
    }

    [Fact]
    public void 同盟の参戦は旗0で正統性が低ければ断る()   // ⑪ の対（旧の国難）
    {
        var (joined, refusal) = AllyJoin(0, 0.3, false);
        Assert.False(joined);
        Assert.True(refusal > 0, "旗 0：断りの緊張が積まない");
    }

    [Fact]
    public void 同盟の参戦は旗1で内戦中なら断り要請元との緊張が積む()   // ⑫
    {
        var (joined, refusal) = AllyJoin(1, 1.0, true);
        Assert.False(joined, "旗 1：内戦中の同盟国が参戦した");
        Assert.True(refusal > 0, "旗 1：断りの緊張が積まない");
    }

    // (1408) 反証役 v の中 4 件 (a)：枝の試験（⑧ 調停の門・⑬ 統一戦争の門）。厭戦と内戦の読み分けを旗ごとに置く。
    private static string OfferReason(double flag, double exhaustion, bool civil)
    {
        var sim = Make(flag, "フランス", "イギリス", "スペイン", "プロイセン");
        var c = sim.Coef;
        var g = sim.Nations[0].World.SelfPower!; var a = sim.Nations[1].World.SelfPower!; var b = sim.Nations[2].World.SelfPower!; var rebel = sim.Nations[3].World.SelfPower!;
        sim.Diplomacy.Wars.Add(new War { A = a, B = b });
        if (civil) sim.Diplomacy.Wars.Add(new War { A = g, B = rebel, CivilWarParent = rebel });
        Assert.Equal(civil, g.Nation!.InCivilWar);
        g.WarExhaustion = exhaustion;
        var war = sim.Diplomacy.Wars[0];
        sim.Diplomacy.CanOfferPeace(g, war, c, out string reason);
        return reason;
    }

    [Fact]
    public void 調停の門は旗0で厭戦なら厭戦で閉じる()   // ⑧ の対
    {
        Assert.Equal("厭戦", OfferReason(0, 0.9, false));
    }

    [Fact]
    public void 調停の門は旗1で厭戦では閉じず内戦なら国難で閉じる()   // ⑧
    {
        string r1 = OfferReason(1, 0.9, false);
        Assert.NotEqual("厭戦", r1); Assert.NotEqual("国難", r1);
        Assert.Equal("国難", OfferReason(1, 0.0, true));
    }

    private static Formations.UnificationBlock UnifBlock(double flag, double exhaustion, bool civil)
    {
        var sim = Make(flag, "フランス", "イギリス", "プロイセン");
        var c = sim.Coef; c.UnificationWarWeary = 1;
        var p = sim.Nations[0].World.SelfPower!; var t = sim.Nations[1].World.SelfPower!; var rebel = sim.Nations[2].World.SelfPower!;
        if (civil) sim.Diplomacy.Wars.Add(new War { A = p, B = rebel, CivilWarParent = rebel });
        Assert.Equal(civil, p.Nation!.InCivilWar);
        p.WarExhaustion = exhaustion;
        return Formations.UnificationWarBlock(sim.Diplomacy, p, [t], t, c);
    }

    [Fact]
    public void 統一戦争の門は旗0で厭戦ならWearyで旗1では厭戦で閉じず内戦で閉じる()   // ⑬
    {
        Assert.Equal(Formations.UnificationBlock.Weary, UnifBlock(0, 0.9, false));
        Assert.Equal(Formations.UnificationBlock.None, UnifBlock(1, 0.9, false));
        Assert.Equal(Formations.UnificationBlock.Weary, UnifBlock(1, 0.0, true));
    }

    [Fact]
    public void 旗1は同じ敗者の2戦争が前走査の集合で束ねて終わる()   // (b)：押しの無い盤面では前走査と歩の集合は同じ＝束ねの経路が旗 1 でも通ることの番人（集合がずれる盤面は軌道の押しに依るので置けない＝突然変異 μ3 は緑のまま・穴として報告）
    {
        var sim = Make(1, "フランス", "イギリス", "スペイン");
        sim.Coef.CollapsePeaceOneShare = 1;
        var a = sim.Nations[0].World; var b = sim.Nations[1].World; var s = sim.Nations[2].World;
        SetK(a, sim.Coef, 0.5); SetK(b, sim.Coef, 0.5); SetK(s, sim.Coef, 0.5);
        var w1 = new War { A = a.SelfPower!, B = b.SelfPower!, Score = -0.6 };   // A の持ちこたえる力 0.5 を下回る側が負け
        var w2 = new War { A = a.SelfPower!, B = s.SelfPower!, Score = -0.6 };
        sim.Diplomacy.Wars.Add(w1); sim.Diplomacy.Wars.Add(w2);
        int e0 = sim.Diplomacy.EndedWars.Count;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.False(sim.Diplomacy.Wars.Contains(w1) || sim.Diplomacy.Wars.Contains(w2), "2 戦争とも終わるはず");
        Assert.Equal(e0 + 2, sim.Diplomacy.EndedWars.Count);
    }

    // (1414) c151wr 項 2・3：旗 1 の枝のうち試験の無かった所に試験を置く。盤面は試験の中で置き、期待は試験の中の式から作る。

    [Fact]
    public void 旗1は2戦争の敗者の束ねを前走査の取り分の順で終える()   // 項 2：前走査の集合を歩の本体が読む（μ2＝歩の本体が前走査の集合を読まない形に戻すと、束ねず歩く順＝添字の降順で終わり赤）
    {
        var sim = Make(1, "フランス", "イギリス", "スペイン");
        sim.Coef.CollapsePeaceOneShare = 1;
        var a = sim.Nations[0].World; var b = sim.Nations[1].World; var s = sim.Nations[2].World;
        SetK(a, sim.Coef, 0.5); SetK(b, sim.Coef, 0.5); SetK(s, sim.Coef, 0.5);
        var w1 = new War { A = a.SelfPower!, B = b.SelfPower!, Score = -0.6, PushB = 1e12 };   // 添字 0・敗者 A に向く押しが大きい＝取り分が大きい
        var w2 = new War { A = a.SelfPower!, B = s.SelfPower!, Score = -0.6, PushB = 1e6 };   // 添字 1
        sim.Diplomacy.Wars.Add(w1); sim.Diplomacy.Wars.Add(w2);
        int e0 = sim.Diplomacy.EndedWars.Count;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.Equal(e0 + 2, sim.Diplomacy.EndedWars.Count);
        // 歩は添字の降順（w2 が先）。束ねれば取り分の大きい w1 が先に終わる。
        Assert.Equal(b.SelfPower!.Id, sim.Diplomacy.EndedWars[e0].BId);
        Assert.Equal(s.SelfPower!.Id, sim.Diplomacy.EndedWars[e0 + 1].BId);
    }

    /// <summary>項 3 ②：調停の応諾（AcceptsPeace）の厭戦の項。旗 1＝1−持ちこたえる力・旗 0＝厭戦÷線。調停者の軍を 0 にして味方の比 lean を 0 に置く。</summary>
    private static (bool forced, int refused) Mediate(double flag, double exhaustion, double kA, double kB)
    {
        var sim = Make(flag, "フランス", "イギリス", "スペイン");
        var c = sim.Coef; c.InterventionSubmitBar = 0.5; c.WarPeaceExhaustion = 0.9;
        var g = sim.Nations[0].World.SelfPower!; var a = sim.Nations[1].World; var bw = sim.Nations[2].World;
        SetK(a, c, kA); SetK(bw, c, kB);
        g.Military = 0; a.SelfPower!.Military = 1000; bw.SelfPower!.Military = 1000;
        a.SelfPower!.WarExhaustion = exhaustion; bw.SelfPower!.WarExhaustion = exhaustion;
        var war = new War { A = a.SelfPower!, B = bw.SelfPower!, Score = 0 };
        sim.Diplomacy.Wars.Add(war);
        int r0 = sim.Diplomacy.PeaceOffersRefused;
        typeof(Diplomacy).GetMethod("ExecuteMediation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim.Diplomacy, [g, war, c]);
        return (war.ForcedPeace, sim.Diplomacy.PeaceOffersRefused - r0);
    }

    [Fact]
    public void 調停の応諾は旗1で持ちこたえる力を読み厭戦を読まない()   // ②
    {
        var (f1, r1) = Mediate(1, 0.95, 1.0, 0.0);   // 厭戦 0.95 でも A は力 1＝応諾の項 0 で拒む・B は力 0＝項 1 で応じる
        Assert.False(f1, "旗 1：A が拒むのに講和した"); Assert.Equal(1, r1);
        var (f2, r2) = Mediate(1, 0.0, 0.0, 0.0);     // 厭戦 0 でも力 0 の両側は応じる
        Assert.True(f2, "旗 1：力 0 の両側が応じない"); Assert.Equal(0, r2);
        var (f0, r0) = Mediate(0, 0.95, 1.0, 0.0);    // 旗 0：厭戦が線を超えれば力に依らず両側が応じる
        Assert.True(f0, "旗 0：厭戦 0.95 の両側が応じない"); Assert.Equal(0, r0);
    }

    /// <summary>項 3 ⑬′：戦時の徴兵の深さ。旗 1 は厭戦を読まない（深さ＝戦線と敵の比だけ）・旗 0 は厭戦の重みを足す。兵の増えを両方の旗で数える。</summary>
    private static double ConscriptGain(double flag, double exhaustion)
    {
        var sim = Make(flag, "フランス", "イギリス");
        var w = sim.Nations[0].World; var c = sim.Coef;
        w.ConscriptionLaw = ConscriptionLaw.Universal;
        c.ConscriptionFrontStep = 0; c.ConscriptionEnemyRatioWeight = 0; c.ConscriptionExhaustionWeight = 1.0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.IsSoldier) p.Size = 1.0;
        sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower!, Score = 0 });
        w.SelfPower!.WarExhaustion = exhaustion;
        double before = w.SoldierSizeK();
        Military.Step(w, c, atWar: true);
        return w.SoldierSizeK() - before;
    }

    [Fact]
    public void 戦時の徴兵の深さは旗1で厭戦を読まない()   // ⑬′
    {
        double g1 = ConscriptGain(1, 0.95), g1z = ConscriptGain(1, 0.0), g0 = ConscriptGain(0, 0.95), g0z = ConscriptGain(0, 0.0);
        Assert.True(g1z > 0 && g0z > 0, $"前提が崩れた：徴兵が立たない（{g1z}・{g0z}）");
        Close(g1z, g1);                                  // 旗 1：厭戦の高さは兵の増えを変えない
        Assert.True(g0 > g0z * 1.0000001, $"旗 0：厭戦が深さを上げない（{g0}・{g0z}）");   // 旗 0：厭戦で深さが上がる
    }

    /// <summary>項 3 MultiNationSim.cs:414：首都の蜂起で生まれた国が親の厭戦を継ぐか。旗 1＝0・旗 0＝親の値。</summary>
    private static double SpawnedExhaustion(double flag)
    {
        var sim = Make(flag, "イギリス", "フランス");
        var parent = sim.Nations[0]; var pw = parent.World;
        Assert.True(pw.Regions.Count >= 3, "前提が崩れた：親の州が 3 つ未満");
        pw.Coef.WarExitFromCapacity = flag;   // 生まれる国の厭戦の継承は親の World.Coef の旗を読む
        pw.SelfPower!.WarExhaustion = 0.8;
        var regs = new System.Collections.Generic.List<Region> { pw.Regions[0] };
        int n0 = sim.Nations.Count;
        typeof(MultiNationSim).GetMethod("SpawnNation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim, [parent, regs, null]);
        Assert.Equal(n0 + 1, sim.Nations.Count);
        return sim.Nations[^1].World.SelfPower!.WarExhaustion;
    }

    [Fact]
    public void 首都の蜂起で生まれた国は旗1で親の厭戦を継がない()
    {
        double w0 = SpawnedExhaustion(0), w1 = SpawnedExhaustion(1);
        Assert.True(w0 > 0.5, $"前提が崩れた：旗 0 で厭戦を継がない（{w0}）");   // 継ぐ側が立つ盤面であること（首都の州を割る）
        Assert.Equal(0.0, w1);
    }

    /// <summary>項 3 DefendDependent：宗主が属国の防衛に参戦するか。旗 1＝内戦中でなければ参戦（厭戦は読まない）・旗 0＝厭戦が線を超えれば見殺し。</summary>
    private static bool SuzerainJoins(double flag, double exhaustion, bool civil)
    {
        var sim = Make(flag, "フランス", "イギリス", "スペイン", "プロイセン");
        sim.StepQuarter();
        var c = sim.Coef; c.WarExitFromCapacity = flag; c.DefendProjectionMin = 0;
        var dip = sim.Diplomacy;
        var dep = sim.Nations[0].World.SelfPower!; var suz = sim.Nations[1].World.SelfPower!; var enemy = sim.Nations[2].World.SelfPower!; var rebel = sim.Nations[3].World.SelfPower!;
        dip.SetDependency(suz, dep);
        Assert.Equal(suz, dip.Suzerain(dep));
        suz.WarExhaustion = exhaustion; dep.WarExhaustion = 0;
        if (civil) dip.Wars.Add(new War { A = suz, B = rebel, CivilWarParent = rebel });
        Assert.Equal(civil, suz.Nation!.InCivilWar);
        Assert.True(dip.DeclareWar(dep, enemy, c), "前提が崩れた：宣戦できない");
        return dip.IsAtWar(suz, enemy);
    }

    [Fact]
    public void 宗主の防衛参戦は旗1で厭戦では見殺しにせず内戦なら見殺しにする()
    {
        Assert.True(SuzerainJoins(1, 0.95, false), "旗 1：厭戦の宗主が参戦しない");
        Assert.False(SuzerainJoins(1, 0.0, true), "旗 1：内戦中の宗主が参戦した");
        Assert.False(SuzerainJoins(0, 0.95, false), "旗 0：厭戦の宗主が参戦した");
        Assert.True(SuzerainJoins(0, 0.0, false), "旗 0：前提が崩れた（厭戦のない宗主が参戦しない）");
    }

    /// <summary>(1414) 項 1 の検算の試験：旧の崩壊の書き換えの後の戦勝点（±1.5×取り分）で oldDecisiveFlag を数える（μ1＝書き換える前の戦勝点 s で数えると赤）。
    /// 盤面：旗 0・A が厭戦の崩壊線・戦勝点 0.3（|s|＜1）・敗者へ向く押しは勝ち側が圧倒＝書き換え後は −1.5。期待は試験の中の式（|−1.5|≥1）から作る。</summary>
    [Fact]
    public void 計器wexitの旧の決定的は崩壊の書き換えの後の戦勝点で数える()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_wex_" + System.Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("wexit_s");
        Instruments.Configure(dir, "*");
        const ulong Seed = 1789UL;
        var sim = Make(0, "フランス", "イギリス");
        var a = sim.Nations[0].World; var b = sim.Nations[1].World;
        SetK(a, sim.Coef, 0.5); SetK(b, sim.Coef, 0.5);
        var war = new War { A = a.SelfPower!, B = b.SelfPower!, Score = 0.3, PushA = 0, PushB = 1e12 };
        sim.Diplomacy.Wars.Add(war);
        a.SelfPower!.WarExhaustion = 1.0; b.SelfPower!.WarExhaustion = 0;
        try { Instruments.Phase(sim, Seed, "open"); sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61); }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); Instruments.SelectTables(null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"wexit_s{Seed}.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch { }
        var h = lines[0].Split('\t'); var row = lines.Skip(1).Select(l => l.Split('\t')).First(r => r[System.Array.IndexOf(h, "oldCollapseAFlag")] == "1");
        string Col(string name) => row[System.Array.IndexOf(h, name)];
        double s = double.Parse(Col("scoreIndex"), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(System.Math.Abs(s) < 1.0, $"前提が崩れた：書き換える前の戦勝点が決定的の線に届いている（{s}）");
        Assert.Equal("1", Col("oldDecisiveFlag"));   // 書き換えの後は −1.5＝決定的（|−1.5|≥1）
        Assert.Equal("1", Col("oldCollapseAFlag")); Assert.Equal("0", Col("oldCollapseBFlag"));
        Assert.Equal(a.SelfPower!.Id.ToString(), Col("powerIdA")); Assert.Equal(b.SelfPower!.Id.ToString(), Col("powerIdB"));
        Assert.Equal(h.Length, row.Length);
    }

    /// <summary>(1421) 反証役 v の中 4 の直し・μ12（同点の決め手を累計の押しへ戻すと赤）：両側が厭戦 1.0 の同点では、旧の規則は月の押し（その月の A の押し＜B の押し）で敗者を決める。
    /// 盤面：1 か月目で累計を (1e12,0) に置き（計器が前の月の累計を覚える）、2 か月目の前に累計を (1e12,2e11) にする＝その月の押しは A が 0・B が 2e11（A＜B で A が敗者）、累計は A＞B（累計で決めると B が敗者）。
    /// A が敗者＝取り分 2×2e11÷(2e11＋1e12)＝1/3 → |±1.5×1/3|＝0.5＜1（決定的 0）、B が敗者＝取り分が満額で 1.5（決定的 1）。期待は試験の中の式から作る。</summary>
    [Fact]
    public void 計器wexitの同点は月の押しで敗者を決める()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_wex_" + System.Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("wexit_s");
        Instruments.Configure(dir, "*");
        const ulong Seed = 1789UL;
        var sim = Make(0, "フランス", "イギリス");
        var a = sim.Nations[0].World; var b = sim.Nations[1].World;
        SetK(a, sim.Coef, 0.5); SetK(b, sim.Coef, 0.5);
        var war = new War { A = a.SelfPower!, B = b.SelfPower!, Score = 0.3, PushA = 1e12, PushB = 0 };
        sim.Diplomacy.Wars.Add(war);
        a.SelfPower!.WarExhaustion = 0; b.SelfPower!.WarExhaustion = 0;
        try
        {
            Instruments.Phase(sim, Seed, "open"); sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
            war.PushB = 2e11; a.SelfPower!.WarExhaustion = 1.0; b.SelfPower!.WarExhaustion = 1.0;
            sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 62);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); Instruments.SelectTables(null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"wexit_s{Seed}.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch { }
        var h = lines[0].Split('	'); var row = lines.Skip(1).Select(l => l.Split('	')).Last();
        string Col(string name) => row[System.Array.IndexOf(h, name)];
        Assert.Equal("1", Col("oldCollapseAFlag")); Assert.Equal("1", Col("oldCollapseBFlag"));
        Assert.True(war.PushA > war.PushB, "前提が崩れた：累計は A が上のはず");
        double expectedReachIfALoses = System.Math.Min(1.0, 2.0 * war.PushB / (war.PushB + war.PushA + 1e-12));
        double expectedAbs = Diplomacy.CollapseScore * expectedReachIfALoses;
        Assert.True(expectedAbs < 1.0, $"前提が崩れた：A が敗者の時の |戦勝点| が決定的に届く（{expectedAbs}）");
        Assert.Equal("0", Col("oldDecisiveFlag"));   // 月の押しで A が敗者＝|戦勝点| が 1 に届かない（累計で決めると B が敗者で 1.5＝1 になり赤）
    }
}
