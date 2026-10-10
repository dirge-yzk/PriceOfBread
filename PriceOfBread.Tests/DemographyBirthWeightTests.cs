using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 出生配分の凍結分母の是正（休眠 FertileAbsoluteBlend=0・作者裁定 2026-08-16）のON経路検証。
/// 旧＝重み Size×fertile÷InitialFertileShare（popごと t=0 凍結）は、初期構成が偏ったpop（聖職者F=0.10）が構成正常化すると
/// 恒久出生倍率2.1倍（実測＝聖職者爆発）になる罠。新＝全pop共通の定数分母 FertilePool(標準構成)＝FertileShareStd×(1+CoupleLimitedBirths)。
/// 時間・年号は一切読まない（年代スケール禁止の裁定）・Σbirths=net の保存は既存の正規化がそのまま担う。
/// </summary>
public class DemographyBirthWeightTests
{
    [Fact]
    public void 既定係数が方針どおり休眠()   // 0=旧popごと凍結分母とビット恒等。定数は標準構成 (1−0.3子供)×0.5女性=0.35
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.FertileAbsoluteBlend);   // ★2026-08-30 点火→同日差し戻し（幕府転換テスト赤の単独犯・英人口縮小化）＝再点火は出生率再較正とセット
        // ★2026-09-30 段4e 後半：標準の分母＝標準のピラミッドの出産可能プール（15〜49歳の女×カップル律速＝男女半々なら15〜49歳の割合 0.4925・旧 FertileShareStd 0.35×2 の置き換え）
        Assert.Equal(Pop.StdShare(15, 50), Simulation.FertilePool(new Pop(), c), 12);
    }

    [Fact]
    public void ON経路_共通分母では女性比0_1のpopの出生配分が規模比例より明確に小さい()
    {
        // A/B（同シード）：OFF＝旧凍結分母（聖職者は t=0 捕捉で重み≈Size＝ほぼ規模比例）／ON＝共通定数分母
        //（聖職者 F=0.10 は fertile=0.7×min側律速≈0.14 → 重み≈0.2×Size＝規模比例より明確に小さい）。
        var off = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        on.World.Coef.FertileAbsoluteBlend = 1.0;   // 開始前に点火＝凍結分母を一度も読まない側
        double p0 = on.World.TotalPopulation;
        var ro = off.World.Region; var rn = on.World.Region;
        // 標準構成（農民 F=0.5・子供0.3）は重み=Size のまま＝配分を奪われる側ではない（偏構成popの重みが落ちる分だけ
        // 配分シェアはむしろ増える）。「奪われない」の片側だけを固定する。
        // ⚠計器（2026-09-27 回2-1）：**1四半期**（同じ盤面から規則だけ違う1歩＝機構の層）で見る。旧＝10年後の大きさで比べていたが、
        //   OFF と ON は別の軌道＝10年のうちに片方だけ危機が起きると総人口が割れ（前期の軍需レシピの点火後は q30 で 15,012 対 14,378）、
        //   大きさの比が配分の規則でなく軌道の分岐を測っていた（q20 までの比は点火の前後で同じ 1.022〜1.026）。
        off.Run(1); on.Run(1);
        Assert.True(rn.Pops[1].Size > ro.Pops[1].Size,
            $"標準構成popが共通分母で出生配分を失った（1四半期 OFF {ro.Pops[1].Size:F2} / ON {rn.Pops[1].Size:F2}）＝物差しがずれている");
        off.Run(39); on.Run(39);   // 10年＝配分差が規模に積もる
        Assert.True(rn.Pops[6].Size < ro.Pops[6].Size * 0.99,
            $"女性比0.1の聖職者の出生配分が規模比例（旧凍結分母）より小さくならない（OFF {ro.Pops[6].Size:F1} / ON {rn.Pops[6].Size:F1}）");
        // 人口保存：配分の重みが変わるだけ＝Σbirths−Σdeaths=net（ロジスティック軌道）は既存の正規化で不変
        Assert.True(System.Math.Abs(on.World.TotalPopulation - (p0 + on.World.PopulationDelta)) < 1e-6 * p0,
            "FertileAbsoluteBlend=1 で人口保存（Σbirths=net）が破れた");
    }

    [Fact]
    public void ON経路_fertileほぼ0のpopは共通分母でも重みが膨張しない()   // FertileShareFloor整合：新形はフロア不要＝分子側で自然に安全
    {
        // 旧経路の膨張バグ＝捕捉時 fertile≈0 のpopで分母が極小→重みが桁違いに膨張（FertileShareFloorで防御）。
        // 新形＝分母は全pop共通の定数なので、fertile≈0 は分子側で重み≈0 ＝出生がほぼ配分されない（安全側）。
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.World.Coef.FertileAbsoluteBlend = 1.0;
        var r = sim.World.Region;
        var clergy = r.Pops[6];
        clergy.Ages = Pop.SoldierAges();   // 出産可能プール≈0 の退化構成を直接セット（男だけ＝couple=0・female=0 → fertile=0・年齢の層）
        double size0 = clergy.Size;
        double p0 = sim.World.TotalPopulation;
        sim.Run(8);   // 2年（出生配分が毎月走る）
        double crude = Calendar.PerYearToPerMonth(sim.World.Coef.CrudeBirthRate) * 24.0;   // 同期間の規模比例なら積もる粗出生の目安（≈7%）
        Assert.True(clergy.Size < size0 * (1.0 + 0.5 * crude),
            $"fertile≈0 のpopに出生が集中した（{size0:F1}→{clergy.Size:F1}）＝共通分母の重みが膨張している");
        Assert.True(System.Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0,
            "退化構成popがいても人口保存が破れた");
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
        var mw = Pop.MortalityWeights(w.Coef.CrudeBirthRate);
        double gapYear = move * (mw[14] - mw[10]);   // 年齢の重みの差（1人あたり・年）
        Fed();
        double y0 = young.Size, o0 = old.Size, d0 = w.DiagDeaths;
        sim.DemographicStep();
        Assert.True(w.DiagDeaths > d0, "前提：回復期（平常の死0）の月になった");
        double gy = (young.Size - y0) / y0, go = (old.Size - o0) / o0;
        Assert.True(gy - go > Calendar.PerYearToPerMonth(0.2 * gapYear), $"年寄りの多い POP が多く死なない（1人あたりの増減 若 {gy:E4}・老 {go:E4}・年齢の重みの差の月 {Calendar.PerYearToPerMonth(gapYear):E4}）");
    }
}
