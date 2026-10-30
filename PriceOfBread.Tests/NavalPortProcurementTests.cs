using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(606) 線 navf／★(669) 線 navf3（海軍の需要の道・読む線 nav〔(594)〕の答え）の番人。旗 <see cref="Coefficients.NavalPortProcurement"/>（艦の発注と予算を国の**本国格の港の州**へ配る）。既定 0＝旧と同じ（恒等は成績表の報告で見る）。
/// ここは機構と不変条件だけ：配りの合計が1・港でない州と植民地格の港は0・予算の合計は旧の首都の1本と同じ・決済は港の州の艦の在庫と海軍の区分に入る。
/// 陸の必要量の国境の番人は (614) の F2 のもの＝この線では載せない。</summary>
public class NavalPortProcurementTests
{
    private static MultiNationSim Frozen(params string[] names)
        => new(System.Array.ConvertAll(names, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    /// <summary>配りの割合＝**本国格の**港の取扱高の比（国の港建設の請求が州に割る比から植民地格を外した1本・分子と分母は同じ述語）。盤面＝英仏（外交は止める）を60日→英の州1・2 を港（取扱高 3 と 1）・他は港でない（取扱高が立っていても数えない）。
    /// 期待値は独立に 3/4・1/4（実装の式を写さない）。取扱高が全て0なら首都が全部（旧の1本）。
    /// ★追記 1：国の必要額＝各州が自分の値段で買う額の和（港の州に値段を手で置き、期待値は価格×発注量を直に足して作る）・予算の割合＝発注額の比（全ての州で 予算÷発注額 が同じ）。
    /// 突然変異で赤になる物（記録＝`measurements/navf669_20261030_mutation.txt`）：M1 分母だけ Tier を外す・M2 分子だけ Tier を外す・M3 分母を2倍・M5 首都へ落とさない・M6 上限を首都の値段に戻す・M7 予算の割合を取扱高の比に戻す・M8 州の予算の書き込みが割合を掛けない。</summary>
    [Fact]
    public void 艦の発注は港の州へ取扱高の比で配られ合計は1で港でない州は0()
    {
        var sim = Frozen("イギリス", "フランス");
        var w = sim.Nations[0].World; var sp = w.SelfPower!; var c = w.Coef;
        for (int d = 0; d < 60; d++) sim.StepDay();
        Assert.True(w.Regions.Count >= 5, "前提が崩れた：英の州が5つ未満");
        foreach (var r in w.Regions) { r.NationHasPort = true; r.IsPort = false; r.LastPortThroughput = 0; }
        w.Regions[1].IsPort = true; w.Regions[1].LastPortThroughput = 3;
        w.Regions[2].IsPort = true; w.Regions[2].LastPortThroughput = 1;
        w.Regions[3].IsPort = false; w.Regions[3].LastPortThroughput = 50;   // 港でない州に取扱高が立っていても割らない
        w.Regions[0].Tier = RegionTier.Core;
        for (int i = 1; i <= 3; i++) w.Regions[i].Tier = RegionTier.Core;
        w.LastRevenuePerMonth = new MoneyPerMonth(1e12);   // 艦の目標は払える側で切られない
        sp.NeedSea = 0.4; sp.NavySkillMul = 1.0;
        // 港の州の値段を手で置く（州1は安く・州2は高く・首都は別の値段）＝上限が首都の値段なら必要額が合わない
        foreach (int fg in c.FleetGoodIds) { w.Regions[1].Markets[fg].Price = 10; w.Regions[2].Markets[fg].Price = 30; w.Regions.Find(r => r.Id == 0)!.Markets[fg].Price = 100; }
        var share = new double[w.Regions.Count]; var bshare = new double[w.Regions.Count];
        var q = EconomyStep.NavalPortSplit(w, c, share, bshare, out double need);
        Assert.NotNull(q);
        double qsum = 0; foreach (double x in q!) qsum += x;
        Assert.True(qsum > 0, "前提が崩れた：発注が0");
        // 独立の期待値＝発注 q を各州の値段（州1＝10・州2＝30）で買う額を、取扱高の割合 3/4・1/4 で足す
        double c1 = 0, c2 = 0;
        for (int k = 0; k < q.Length; k++) { c1 += q[k] * 10; c2 += q[k] * 30; }
        Assert.Equal(0.75 * c1 + 0.25 * c2, need, 9);
        Assert.Equal(0.75, share[1], 12); Assert.Equal(0.25, share[2], 12);
        Assert.Equal(0.75 * c1 / need, bshare[1], 12); Assert.Equal(0.25 * c2 / need, bshare[2], 12);   // 予算の割合＝発注額の比
        double bsum = 0; foreach (double x in bshare) bsum += x;
        Assert.Equal(1.0, bsum, 12);
        // 州ごとの予算の書き込み（Simulation が通る1本）＝国の予算 1000 の和が 1000・植民地格と港でない州は 0
        double nat = 1000, bnat = 0;
        for (int i = 0; i < share.Length; i++) { double bi = EconomyStep.NavalPortBudget(nat, share[i], bshare[i]); bnat += bi; if (i != 1 && i != 2) Assert.Equal(0.0, bi); }
        Assert.Equal(nat, bnat, 9);
        double sum = 0; for (int i = 0; i < share.Length; i++) { sum += share[i]; if (i != 1 && i != 2) Assert.Equal(0.0, share[i]); }
        Assert.Equal(1.0, sum, 12);
        // ★(669) 植民地格の港は取扱高が大きくても割らない（分母にも入れない＝本国の港の割合は 3/4・1/4 のまま・合計 1）
        w.Regions[4].IsPort = true; w.Regions[4].Tier = RegionTier.Colonial; w.Regions[4].LastPortThroughput = 1000;
        EconomyStep.NavalPortSplit(w, c, share, bshare, out _);
        Assert.Equal(0.75, share[1], 12); Assert.Equal(0.25, share[2], 12); Assert.Equal(0.0, share[4]); Assert.Equal(0.0, bshare[4]);
        double sumC = 0; foreach (double x in share) sumC += x;
        Assert.Equal(1.0, sumC, 12);
        // 本国の港に取扱高が無く植民地格の港にだけ立つ＝首都が全部（植民地へは寄せない）
        w.Regions[1].LastPortThroughput = 0; w.Regions[2].LastPortThroughput = 0;
        EconomyStep.NavalPortSplit(w, c, share, bshare, out _);
        int cap0 = w.Regions.FindIndex(r => r.Id == 0);
        for (int i = 0; i < share.Length; i++) { Assert.Equal(i == cap0 ? 1.0 : 0.0, share[i]); Assert.Equal(i == cap0 ? 1.0 : 0.0, bshare[i]); }
        w.Regions[1].LastPortThroughput = 3; w.Regions[2].LastPortThroughput = 1;
        // 取扱高が全て0（開幕の清算の前）＝首都が全部
        foreach (var r in w.Regions) r.LastPortThroughput = 0;
        EconomyStep.NavalPortSplit(w, c, share, bshare, out double needFallback);
        int cap = w.Regions.FindIndex(r => r.Id == 0);
        double capPriceSum = 0; for (int k = 0; k < q.Length; k++) capPriceSum += q[k] * 100;
        Assert.Equal(capPriceSum, needFallback, 9);   // 首都へ落ちたときの必要額は首都の値段
        for (int i = 0; i < share.Length; i++) Assert.Equal(i == cap ? 1.0 : 0.0, share[i]);
    }

    /// <summary>予算の置き場：旗1の海軍の予算は本国格の港の州だけに立ち、発注は予算のある州だけに立つ。旗0は首都だけ。
    /// 盤面＝英仏を同じ種で2本・旗だけ違う。60日（艦の目標が無い間は2本とも同じ軌道）→海の必要量を置いて月次の予算が書かれる最初の日に比べる。
    /// 州ごとに：発注があるのは予算が正の州だけ（予算なしで需要だけ立てない）。旗0では首都以外の予算は0。
    /// ★追記 1：旗1の上限は各州の値段で買う額の和なので、旗0の首都の1本（首都の値段）とは一致しない（保存の等式は外した）。ここで見るのは〈発注は予算のある州だけ・予算は本国格の港の州だけ・旗0は首都だけ〉。</summary>
    [Fact]
    public void 海軍の予算は本国格の港の州だけに立ち発注は予算のある州だけに立つ()
    {
        static MultiNationSim Make(double flag)
        {
            var sim = Frozen("イギリス", "フランス");
            foreach (var n in sim.Nations) n.World.Coef.NavalPortProcurement = flag;
            for (int d = 0; d < 60; d++) sim.StepDay();
            return sim;
        }
        var s0 = Make(0.0); var s1 = Make(1.0);
        var w0 = s0.Nations[0].World; var w1 = s1.Nations[0].World;
        foreach (var w in new[] { w0, w1 })
        {
            w.LastRevenuePerMonth = new MoneyPerMonth(1e12);
            w.SelfPower!.NeedSea = 50; w.SelfPower.NeedLand = 3; w.SelfPower.NeedInternal = 1; w.SelfPower.NavySkillMul = 1.0;
        }
        double b0 = 0; bool got = false;
        for (int d = 0; d < 40 && !got; d++)
        {
            s0.StepDay(); s1.StepDay();
            b0 = w0.Region.NavalBudget;
            if (b0 > 0) got = true;
        }
        Assert.True(got, "前提が崩れた：旗0の首都の海軍の予算が立たない");
        foreach (var r in w0.Regions) if (r.Id != 0) Assert.Equal(0.0, r.NavalBudget);
        // 旗1：発注があるのは予算が正の州だけ・植民地格の州と港でない州の予算は0（予算÷発注額が全ての州で等しいことは配りの番人が手で置いた値段で見る＝ここは月末の支払い率と値段の動きの後なので見ない）
        double bsum1 = 0;
        foreach (var r in w1.Regions)
        {
            Assert.Equal(r.NavalBudget > 0, r.NavalOrder is not null);
            bsum1 += r.NavalBudget;
            if (r.NavalBudget > 0 && r.Id != 0) Assert.True(r.IsPort && r.Tier != RegionTier.Colonial, "港でない州か植民地格の州に予算が立った");
        }
        Assert.True(bsum1 > 0, "前提が崩れた：旗1の予算が立たない");
        foreach (var r in w0.Regions) Assert.Null(r.NavalOrder);   // 旗0は配りの書き込みに1度も入らない
    }

    /// <summary>★(679) 線 navf4：画面の艦隊の行（App の <c>GameViewModel.Politics.cs</c>）の充足の式の番人。旗1では予算が国の本国格の港の州へ割られ、首都が港でない国の首都の州の予算は0になる
    /// ＝首都の州の予算÷首都の値段の必要額（旧の式）は0%を出す。新しい式＝国のΣ州の予算÷<see cref="EconomyStep.NavalPortSplit"/> の必要額（各州が自分の値段で買う額の和）。
    /// 期待値は独立に作る＝国の予算を必要額の半分にして Simulation と同じ書き込み（NavalPortBudget）を通し、充足が0.5になること・首都の州の予算は0のまま・旧の式は0になること。</summary>
    [Fact]
    public void 画面の艦隊の充足は国のΣ州の予算を各州の値段の必要額で割り首都が港でない国でも0にならない()
    {
        var sim = Frozen("イギリス", "フランス");
        var w = sim.Nations[1].World; var c = w.Coef;
        c.NavalPortProcurement = 1.0;
        for (int d = 0; d < 60; d++) sim.StepDay();
        var cap = w.Regions.Find(r => r.Id == 0)!;
        foreach (var r in w.Regions) { r.NationHasPort = true; r.IsPort = false; r.LastPortThroughput = 0; r.Tier = RegionTier.Core; r.NavalBudget = 0; }
        var ports = w.Regions.Where(r => r.Id != 0).Take(2).ToList();
        Assert.Equal(2, ports.Count);
        ports[0].IsPort = true; ports[0].LastPortThroughput = 3; ports[1].IsPort = true; ports[1].LastPortThroughput = 1;
        w.LastRevenuePerMonth = new MoneyPerMonth(1e12);
        w.SelfPower!.NeedSea = 50; w.SelfPower.NeedLand = 3; w.SelfPower.NeedInternal = 1; w.SelfPower.NavySkillMul = 1.0;
        foreach (int fg in c.FleetGoodIds) { ports[0].Markets[fg].Price = 10; ports[1].Markets[fg].Price = 30; cap.Markets[fg].Price = 100; }
        var share = new double[w.Regions.Count]; var bshare = new double[w.Regions.Count];
        var q = EconomyStep.NavalPortSplit(w, c, share, bshare, out double need);
        Assert.NotNull(q); Assert.True(need > 0, "前提が崩れた：必要額0");
        double nat = need * 0.5;   // 国の予算は必要額の半分
        for (int i = 0; i < w.Regions.Count; i++) w.Regions[i].NavalBudget = EconomyStep.NavalPortBudget(nat, share[i], bshare[i]);
        Assert.Equal(0.0, cap.NavalBudget);   // 首都の州には予算が立たない
        double sum = 0; foreach (var r in w.Regions) sum += r.NavalBudget;
        Assert.Equal(0.5, sum / need, 9);     // 新しい式の充足
        EconomyStep.FleetOrder(cap, c, w, out double capCost);
        Assert.Equal(0.0, capCost > 1e-9 ? System.Math.Min(1.0, cap.NavalBudget / capCost) : 0.0);   // 旧の式は0%（直す前の画面の値）
    }

    /// <summary>決済：港の州（首都でない・軍需を持たない）が自分の市場で艦を買い、買えた艦をその州の艦の在庫に積み、支払いを海軍の区分へ入れる（国庫→造船の売上）。
    /// 盤面＝英仏を60日→英の州1を港として発注と予算を手で置き（Simulation の月次の書き手は通さない）Run を直に呼ぶ。首都の艦の在庫は動かない。
    /// 突然変異 M4（支払いを陸軍の区分へ）で赤。</summary>
    [Fact]
    public void 港の州は自分の市場で艦を買いその州の艦の在庫に積み海軍の区分で払う()
    {
        var sim = Frozen("イギリス", "フランス");
        var w = sim.Nations[0].World; var c = w.Coef;
        c.NavalPortProcurement = 1.0;
        for (int d = 0; d < 60; d++) sim.StepDay();
        var r = w.Regions[1];
        Assert.True(r.Id != 0 && r.MilitaryScale <= 0, "前提が崩れた：州1が首都か軍需を持つ");
        int kw = System.Array.IndexOf(c.FleetGoodIds, Catalog.WarshipId);
        foreach (var rr in w.Regions) rr.FleetUnits = null;
        var order = new double[c.FleetGoodIds.Length]; order[kw] = 2.0;
        r.NavalOrder = order;
        double cost = EconomyStep.FleetOrderCost(r, c, order);
        Assert.True(cost > 0, "前提が崩れた：必要額0");
        r.NavalBudget = cost;   // 予算は必要額と同じ＝発注の全量を買いにいく
        r.Markets[c.FleetGoodIds[kw]].Stock = Math.Max(r.Markets[c.FleetGoodIds[kw]].Stock, 1000);   // 売り物がある
        System.Array.Clear(r.StateSpent);
        EconomyStep.Run(r, c, 1.0, w.Bank, w);
        double got = r.FleetUnits?[kw] ?? 0;
        Assert.True(got > 0, "買えた艦が港の州の艦の在庫に積まれない");
        Assert.True(got <= order[kw] * (1 + 1e-9), $"発注を超えて買った：{got}＞{order[kw]}");
        Assert.True(r.StateSpent[(int)TreasuryCat.Navy] > 0, "艦の支払いが海軍の区分に入らない");
        Assert.Null(w.Region.FleetUnits);   // 首都の艦の在庫には積まれない
        // 発注が無い（null）州は何も買わない
        var r2 = w.Regions[2]; r2.NavalOrder = null; r2.NavalBudget = 1e9; System.Array.Clear(r2.StateSpent);
        EconomyStep.Run(r2, c, 1.0, w.Bank, w);
        Assert.Equal(0.0, r2.StateSpent[(int)TreasuryCat.Navy]);
    }
}
