using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(791) 線 c10cp：旗 <see cref="Coefficients.CollapsePeaceOneShare"/>（崩壊の講和の取り分を 1 回分にする・設計思想⑨）の番人。
/// 盤面＝敗者イギリスが 2 本の戦争（フランスと・スペインと）で同じ月に厭戦の崩壊で負ける。敗者は 1 本目で B・2 本目で A（押しの向きが入れ替わる）。
/// 勝者の押しの累計 PushA／PushB は手で置く（取り分の期待値は置いた値の比から作る＝本体の式を呼ばない）。
/// 期待値は賠償の式のリテラル：ReparationGdpFraction（0.2 の係数）×敗者の GdpPerMonth×戦勝点×支払い率 r。
/// ⚠突然変異「旗を読まない」「取り分を二重に掛ける」「押しで決まる講和も集合に入れる」「取り分を 0 にする」で赤（使い捨ての木・measurements/m791_*_mutation.txt）。</summary>
[Collection("Instruments")]
public class CollapsePeaceTests
{
    private static (MultiNationSim sim, Power fr, Power uk, Power es, War w1, War w2) Board(double flag, double pushFr = 3e12, double pushEs = 1e12, double loseFr = 1)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.CollapsePeaceOneShare = flag;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.TreatyPortWinnerDistanceKm = 0;          // 条約港の倍率（遠い勝者）を外す
        c.ConcertReparationRestraint = 0;          // 会議の削りを外す
        c.ColonyCaptureBudget = 0;                 // 植民地の奪取は別の試験で見る
        c.TerritoryCessionScore = 1e9;             // 本国の割譲は別の試験で見る
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!;
        foreach (var p in new[] { fr, uk, es }) p.WarExhaustion = 0;
        uk.WarExhaustion = 1.0;                    // 敗者は崩壊（閾値以上）
        uk.Nation!.FiscalFundingRatio = new Share(0.8);
        // 1 本目：A＝フランス・B＝イギリス（勝者は A 側＝PushA）・2 本目：A＝イギリス・B＝スペイン（勝者は B 側＝PushB）
        var w1 = new War { A = fr, B = uk, PushA = pushFr, PushB = loseFr };
        var w2 = new War { A = uk, B = es, PushA = 1, PushB = pushEs };
        sim.Diplomacy.Wars.Add(w1); sim.Diplomacy.Wars.Add(w2);
        return (sim, fr, uk, es, w1, w2);
    }

    private static void Close(double want, double got) => Assert.True(Math.Abs(want - got) <= 1e-9 * Math.Max(1.0, Math.Abs(want)), $"期待 {want:G17}・実 {got:G17}");

    private static double Led(Power p) => p.Nation!.TreasuryLedger[(int)TreasuryCat.WarCost];

    private static (double frGain, double esGain, double ukLoss, War w1, War w2, double gdp, double r) Run(double flag, double pushFr = 3e12, double pushEs = 1e12)
    {
        var (sim, fr, uk, es, w1, w2) = Board(flag, pushFr, pushEs);
        double f0 = Led(fr), e0 = Led(es), u0 = Led(uk);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);   // 四半期の AI・調停が走らない月
        Assert.Empty(sim.Diplomacy.Wars);                    // 前提：2 本とも崩壊の講和で終わった
        Assert.True(Math.Abs(w1.Score) >= 1.0 && Math.Abs(w2.Score) >= 1.0, $"前提が崩れた：戦勝点が崩壊の満額でない（{w1.Score:G6}/{w2.Score:G6}）");
        return (Led(fr) - f0, Led(es) - e0, Led(uk) - u0, w1, w2, uk.GdpPerMonth.Value, uk.Nation!.FiscalFundingRatio.Value);
    }

    [Fact]
    public void 旗0は戦争ごとに満額の賠償を払う()
    {
        var o = Run(0);
        Assert.True(o.gdp > 0, "前提が崩れた：敗者の GdpPerMonth が 0");
        double single = 0.2 * o.gdp * o.r;   // 戦勝点 1 の賠償（リテラル 0.2＝ReparationGdpFraction の既定）
        Close(single, o.frGain);
        Close(single, o.esGain);   // 2 本とも満額＝合計は単独の 2 倍
        Close(2 * single, o.frGain + o.esGain);
    }

    [Fact]
    public void 旗1は同じ月の同じ敗者の崩壊の講和の賠償を1回分にして押しの累計の比で配る()
    {
        var o = Run(1);
        double single = 0.2 * o.gdp * o.r;
        double total = o.frGain + o.esGain;
        Close(single, total);                                      // 合計は単独の講和の賠償（戦勝点は 2 本とも満額 1）
        // 取り分＝勝者の押しの累計の比（1 本目の勝者は A 側の押し・2 本目の勝者は B 側の押し）
        double pf = o.w1.PushA, pe = o.w2.PushB;
        Assert.Equal(pf / (pf + pe), o.frGain / total, 9);
        Assert.Equal(pe / (pf + pe), o.esGain / total, 9);
        Assert.True(o.frGain > o.esGain, "前提が崩れた：押しの大きい勝者の取り分が大きくない");
    }

    [Fact]
    public void 旗1の取り分は押しの向きが入れ替わっても勝者の押しで決まる()
    {
        var o = Run(1, pushFr: 1e12, pushEs: 3e12);   // 押しの大小を入れ替える（敗者が A の戦争の勝者の押しは PushB）
        double total = o.frGain + o.esGain;
        double pf = o.w1.PushA, pe = o.w2.PushB;
        Close(0.2 * o.gdp * o.r, total);
        Assert.Equal(pe / (pf + pe), o.esGain / total, 9);
        Assert.True(o.esGain > o.frGain);
    }

    [Fact]
    public void 旗1は崩壊が1本だけなら旧と同じ満額()
    {
        var (sim, fr, uk, es, w1, w2) = Board(1);
        sim.Diplomacy.Wars.Remove(w2);               // 崩壊は 1 本だけ
        double f0 = Led(fr);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.Empty(sim.Diplomacy.Wars);
        double single = 0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value;
        Close(single, Led(fr) - f0);
    }

    [Fact]
    public void 旗1でも押しで決まる講和は集合に入らず旧のまま()
    {
        // 敗者の厭戦を崩壊の線の手前にして、2 本目を押しの戦勝点（決定的）で終える＝集合は作られず、各戦争が自分の戦勝点で満額を払う
        var (sim, fr, uk, es, w1, w2) = Board(1);
        uk.WarExhaustion = 0.5;                        // 崩壊しない
        w1.Score = 1.2; w2.Score = -1.2;               // どちらも押しで決まる決定的勝利（フランス・スペインの勝ち）
        double f0 = Led(fr), e0 = Led(es);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.Empty(sim.Diplomacy.Wars);
        double unit = 0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value;
        Close(unit * 1.0, Led(fr) - f0);     // 戦勝点はクランプ後 1.0（旧のまま満額）
        Close(unit * 1.0, Led(es) - e0);
    }

    [Fact]
    public void 旗1は白紙の崩壊の講和を集合に入れず他の勝者は満額()
    {
        // 1 本目は勝者の押しが敗者の押しの 1/100＝戦勝点が白紙の線（WhitePeaceScore）の下＝賠償は無い。2 本目だけが払う
        var (sim, fr, uk, es, w1, w2) = Board(1, pushFr: 1e12, pushEs: 1e12, loseFr: 1e14);
        sim.Coef.WarPeaceExhaustion = 0.5; fr.WarExhaustion = 0.6;   // 白紙の 1 本目も〈疲れた〉側で講和の枝に入る（崩壊はしない側の厭戦 0.6＞0.5・敗者イギリスは崩壊の 1.0）
        double f0 = Led(fr), e0 = Led(es);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.Empty(sim.Diplomacy.Wars);
        Assert.True(Math.Abs(w1.Score) < sim.Coef.WhitePeaceScore, $"前提が崩れた：1 本目の戦勝点が白紙の線の下でない（{w1.Score:G6}）");
        Close(0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value, Led(es) - e0);   // 2 本目の勝者は満額（白紙が取り分を持っていかない）
        Assert.Equal(0.0, Led(fr) - f0);
    }

    [Fact]
    public void 旗1は両者の厭戦が1_0で並ぶ戦争も集合に入る()
    {
        // 敗者はフランス。1 本目はフランスもイギリスも厭戦 1.0（頭打ちで並ぶ）＝歩の本体の規則（同値なら押しが小さい A 側・同じなら B 側が負ける）で敗者が決まる。2 本目はスペインとの崩壊。
        // 盤面の押し（月の初めの FrontPush）はフランスの方が小さい＝A のフランスが 1 本目も負ける（前提として確かめる）。フランスは 2 本とも負け、合計は単独の賠償。
        var (sim, fr, uk, es, w1, w2) = Board(1);
        sim.Diplomacy.Wars.Clear();
        fr.WarExhaustion = 1.0; uk.WarExhaustion = 1.0; es.WarExhaustion = 0;
        fr.Nation!.FiscalFundingRatio = new Share(0.8);
        var a = new War { A = fr, B = uk, PushA = 1, PushB = 3e12 };   // 勝者はイギリス（B 側の押し）
        var b = new War { A = es, B = fr, PushA = 1e12, PushB = 1 };   // 勝者はスペイン（A 側の押し）
        sim.Diplomacy.Wars.Add(a); sim.Diplomacy.Wars.Add(b);
        double u0 = Led(uk), e0 = Led(es);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.Empty(sim.Diplomacy.Wars);
        Assert.True(a.Score < -1.0 && b.Score > 1.0, $"前提が崩れた：フランスが 2 本とも満額で負けていない（戦勝点 {a.Score:G6}/{b.Score:G6}）");
        Close(0.2 * fr.GdpPerMonth.Value * 0.8, (Led(uk) - u0) + (Led(es) - e0));
    }

    [Fact]
    public void 旗1で押しが正の勝者は取り分が0にならない()
    {
        var o = Run(1, pushFr: 1e12, pushEs: 5.0);    // 小さい押しでも正なら取り分は正
        Assert.True(o.esGain > 0 && o.frGain > 0, $"取り分が 0 の勝者が出た（{o.frGain:G6}/{o.esGain:G6}）");
        Close(0.2 * o.gdp * o.r, o.frGain + o.esGain);
    }

    [Fact]
    public void 旗1は人間の勝者でもAIの勝者でも同じ取り分()
    {
        var a = Run(1);
        var (sim, fr, uk, es, w1, w2) = Board(1);
        fr.IsPlayer = true;
        double f0 = Led(fr), e0 = Led(es);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Close(a.frGain, Led(fr) - f0);
        Close(a.esGain, Led(es) - e0);
    }

    // ---- 直に ResolvePeace を呼ぶ：植民地の奪取の予算と本国の割譲の回数 ----

    private static MultiNationSim Cess(double flag)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.Coef.CollapsePeaceOneShare = flag;
        var fr = sim.Nations[0].World; var uk = sim.Nations[1].World; var es = sim.Nations[2].World;
        foreach (var (p, nav) in new[] { (fr.SelfPower!, 100.0), (uk.SelfPower!, 10.0), (es.SelfPower!, 100.0) })
        { p._fleetDriven = false; p.Military = 1000; p.NavalShare = new Share(nav / 1000.0); p.NavySkillMul = 1.0; }
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is null) p.Military = 0;
        return sim;
    }

    private static int Lost(World uk, System.Collections.Generic.HashSet<string> before) => before.Count - uk.Regions.Count(r => before.Contains(r.Name));

    [Fact]
    public void 本国の割譲は集合で1回_旗なしの2本は2回()
    {
        // 旗なし（cg なし）＝2 本の講和がそれぞれ割譲する
        var simOld = Cess(0);
        var ukOld = simOld.Nations[1].World; var before0 = ukOld.Regions.Select(r => r.Name).ToHashSet();
        simOld.Diplomacy.ResolvePeace(simOld.Coef, new War { A = simOld.Nations[0].World.SelfPower!, B = ukOld.SelfPower!, Score = 1.3 });
        simOld.Diplomacy.ResolvePeace(simOld.Coef, new War { A = ukOld.SelfPower!, B = simOld.Nations[2].World.SelfPower!, Score = -1.3 });
        int lostOld = Lost(ukOld, before0);
        // 集合（枠 1）＝本国の割譲は 1 回
        var simNew = Cess(1);
        var ukNew = simNew.Nations[1].World; var before1 = ukNew.Regions.Select(r => r.Name).ToHashSet();
        var cg = new Diplomacy.CollapseGroup { Sa = 1.0, AbsMax = 1.3, CedeLeft = 1 };
        simNew.Diplomacy.ResolvePeace(simNew.Coef, new War { A = simNew.Nations[0].World.SelfPower!, B = ukNew.SelfPower!, Score = 1.3 }, cg, 0.6);
        simNew.Diplomacy.ResolvePeace(simNew.Coef, new War { A = ukNew.SelfPower!, B = simNew.Nations[2].World.SelfPower!, Score = -1.3 }, cg, 0.4);
        int lostNew = Lost(ukNew, before1);
        Assert.True(lostOld >= 2, $"前提が崩れた：旗なしで 2 回目の割譲が起きていない（失った州 {lostOld}）");
        // 植民地格の州（予算 100 の取り分で奪う）と本国の境界改定が混ざる＝本国の割譲は 1 回・植民地の奪取は予算の取り分の範囲
        Assert.Equal(0, cg.CedeLeft);
        Assert.True(lostNew < lostOld, $"集合の割譲が旧より減っていない（旧 {lostOld}・新 {lostNew}）");
    }

    [Fact]
    public void 植民地の奪取の予算は戦勝点の取り分()
    {
        // 取り分 0.1 の勝者は予算 100×1.0×0.1＝10（植民地 1 つの費用の下限 30 に届かない）＝植民地を奪えない。取り分 1.0 なら奪える（旗なしと同じ）。
        var simA = Cess(1);
        var ukA = simA.Nations[1].World; int colA0 = ukA.Regions.Count(r => r.Tier == RegionTier.Colonial);
        var cg = new Diplomacy.CollapseGroup { Sa = 1.0, AbsMax = 1.3, CedeLeft = 0 };
        simA.Diplomacy.ResolvePeace(simA.Coef, new War { A = simA.Nations[0].World.SelfPower!, B = ukA.SelfPower!, Score = 1.3 }, cg, 0.1);
        Assert.Equal(colA0, ukA.Regions.Count(r => r.Tier == RegionTier.Colonial));
        var simB = Cess(1);
        var ukB = simB.Nations[1].World; int colB0 = ukB.Regions.Count(r => r.Tier == RegionTier.Colonial);
        var cgB = new Diplomacy.CollapseGroup { Sa = 1.0, AbsMax = 1.3, CedeLeft = 0 };
        simB.Diplomacy.ResolvePeace(simB.Coef, new War { A = simB.Nations[0].World.SelfPower!, B = ukB.SelfPower!, Score = 1.3 }, cgB, 1.0);
        Assert.True(ukB.Regions.Count(r => r.Tier == RegionTier.Colonial) < colB0, "前提が崩れた：取り分 1.0 でも植民地を奪えない");
    }
}
