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
        c.WarLuck = 0; c.WarPushSpeed = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
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
}
