using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 出生配分の分母の番人。（旧＝凍結分母の是正の休眠 FertileAbsoluteBlend の ON 経路の検証〔作者裁定 2026-08-16〕が 3 本あった＝2026-10-25 (551) で旗ごと削除。
/// 凍結分母 init〔popごと t=0〕が残す穴は古い設計の一覧 H25〔9-2 と一緒に直す〕。）
/// </summary>
public class DemographyBirthWeightTests
{
    [Fact]
    public void 標準のピラミッドの出産可能プールは15から49歳の割合に合う()
    {
        var c = new Coefficients();
        // ★2026-09-30 段4e 後半：標準の分母＝標準のピラミッドの出産可能プール（15〜49歳の女×カップル律速＝男女半々なら15〜49歳の割合 0.4925・旧 FertileShareStd 0.35×2 の置き換え）
        Assert.Equal(Pop.StdShare(15, 50), Simulation.FertilePool(new Pop(), c), 12);
    }

    [Fact]
    public void 州への出生の取り分は今の構成で決まる_凍結の分母で州へ配らない()
    {
        // ★2026-10-01 (143)（線2 後半・飢えで州が痩せる型の残り）：国の出生を州へ配る鍵は**州の出産年齢の女性の実数**（今の出産可能のプール×規模の和）。
        //   旧＝POP ごとの t=0 の凍結の分母の鍵で州へも配った＝構成が t=0 から動いた POP の多い州ほど出生が膨らみ、そうでない州が痩せた
        //   （ペルシャのタブリーズは国の平均より年 7‰ 少なく産んで百六十年でピークの1/10 未満へ＝(143) 腕E2）。州の中の階級へは旧の鍵のまま（9-2）。
        //   盤面＝2つの州（どちらも農民と工場労働者）。1か月目だけ不足州の2つの POP を同じ「出産年齢の女性が少ない」構成にして人口動態を1回（凍結の分母が小さく捕まる）、
        //   2か月目は不足州の2つの POP を本国の州の同じ階級と全く同じ構成・同じ規模に戻して1回＝同じ階級の1人あたりの増減は2つの州で一致する。
        var sim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine }, new NullAgent());
        var w = sim.World;
        Pop Of(int region, int cls) { foreach (var p in w.Regions[region].Pops) if (p.ClassId == cls) return p; throw new System.InvalidOperationException(); }
        void Fed() { foreach (var r in w.Regions) foreach (var p in r.Pops) { p.FoodNeed = 0.8 * p.Size; p.FoodMet = p.FoodNeed; } }
        var skew = (double[])Of(1, 0).Ages.Clone();
        for (int k = 3; k < 10; k += 2) { skew[k - 1] += 0.8 * skew[k]; skew[k] *= 0.2; }   // 10〜49 の女性の8割を男へ
        Of(1, 0).Ages = (double[])skew.Clone(); Of(1, 1).Ages = (double[])skew.Clone();
        Fed(); sim.DemographicStep();
        for (int cls = 0; cls < 2; cls++) { Of(1, cls).Ages = (double[])Of(0, cls).Ages.Clone(); Of(1, cls).Size = Of(0, cls).Size; }
        Fed();
        double f0 = Of(0, 0).Size, f1 = Of(1, 0).Size, u0 = Of(0, 1).Size, u1 = Of(1, 1).Size, d0 = w.DiagDeaths;
        sim.DemographicStep();
        Assert.True(w.DiagDeaths > d0, "前提：回復期（平常の死0）の月になった");
        double gf0 = (Of(0, 0).Size - f0) / f0, gf1 = (Of(1, 0).Size - f1) / f1, gu0 = (Of(0, 1).Size - u0) / u0, gu1 = (Of(1, 1).Size - u1) / u1;
        Assert.True(System.Math.Abs(gf0 - gf1) < 1e-12 && System.Math.Abs(gu0 - gu1) < 1e-12,
            $"同じ構成・同じ規模の州の1人あたりの増減が違う（農民 {gf0:E6}・{gf1:E6}／工場労働者 {gu0:E6}・{gu1:E6}）＝州への出生の取り分が過去の構成を覚えている");
    }

    [Fact]
    public void 平常の死は年齢の構成で配る_年寄りの多いPOPほど多く死ぬ()
    {
        // ★2026-10-01 (143)：平常の死＝国の平常の死の率×年齢の指数（その POP の年齢の構成で平常の死の年齢の重みを平均した値）×規模。
        //   盤面＝同じ階級の対（本国の州と不足州の農民・同じ構成）。1か月目は同じ構成で回し、2か月目の前に片方の 50〜59 の男の8割を 70歳以上の男へ移す
        //   （出産可能のプール＝15〜49歳の女×カップル律速は同じ＝出生の取り分は同じ）＝2か月目の1人あたりの増減は年寄りの多い方が小さい（旧＝POP の間は規模に比例＝同じ率）。
        //   ★(144) 合流で盤面だけ直した（主張は同じ）：旧＝20〜29 の男を 50〜59 へ移した＝(142)（4e-2b）で出産可能のプールが 15〜49歳の女×同じ年齢の男とのカップル律速になり、
        //   男が抜けると出生の取り分が減る＝「出生の取り分は同じ」の前提が崩れた（年寄りの多い方が出生でも減る＝突然変異 M4〔死を規模で配る〕でも緑になりうる）。
        var sim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine }, new NullAgent());
        var w = sim.World;
        Pop Of(int region) { foreach (var p in w.Regions[region].Pops) if (p.ClassId == 0) return p; throw new System.InvalidOperationException(); }
        void Fed() { foreach (var r in w.Regions) foreach (var p in r.Pops) { p.FoodNeed = 0.8 * p.Size; p.FoodMet = p.FoodNeed; } }
        var young = Of(0); var old = Of(1);
        Fed(); sim.DemographicStep();   // 凍結の分母（出生の鍵）を同じ構成で捕まえる
        old.Ages = (double[])young.Ages.Clone();
        double move = 0.8 * old.Ages[10];
        old.Ages[10] -= move; old.Ages[14] += move;   // 男の 50〜59 → 70歳以上（出産可能のプール〔15〜49歳〕の外どうし＝出生の取り分は同じ）
        var mw = Pop.MortalityWeights(w.Coef.CrudeBirthRatePerYear.Value);
        double gapYear = move * (mw[14] - mw[10]);   // 年齢の重みの差（1人あたり・年）
        Fed();
        double y0 = young.Size, o0 = old.Size, d0 = w.DiagDeaths;
        sim.DemographicStep();
        Assert.True(w.DiagDeaths > d0, "前提：回復期（平常の死0）の月になった");
        double gy = (young.Size - y0) / y0, go = (old.Size - o0) / o0;
        Assert.True(gy - go > Calendar.PerYearToPerMonth(0.2 * gapYear), $"年寄りの多い POP が多く死なない（1人あたりの増減 若 {gy:E4}・老 {go:E4}・年齢の重みの差の月 {Calendar.PerYearToPerMonth(gapYear):E4}）");
    }
}
