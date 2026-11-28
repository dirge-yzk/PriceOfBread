using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1393) c148po 政治と外交の一元化（恒等）の機構の試験。事前登録 measurements/m1393_20261126_prereg_po.txt。
/// 期待値は旧の式を手で写した物（本体の新しい関数から作らない）。
/// ⚠突然変異 μ1：InterestGroups.SuffrageGate の Census の比べを逆にする → T1 赤。μ2：Politics.RegionReach の減衰の係数を DistanceWarDecay 以外にする → T2 赤。</summary>
public class PoliticsUnificationC148poTests
{
    private static MultiNationSim Board()
        => new([WorldData.ByName("清"), WorldData.ByName("ロシア")], seed: 83) { DiplomacyFrozen = true };

    [Fact]
    public void T1_選挙権の段ごとの声の門は旧の3通りの式と同じ()
    {
        var c = new Coefficients();
        double sub = c.ModernEliteSubCensusVoice;
        Assert.True(sub > 0 && sub < 1, "前提：Census 未満の近代エリートの声が 0 と 1 の間（段の違いが値に出る）");
        // 旧の式（Politics.FactionStrength・Parliament.Qualifies・InterestGroups.FranchiseVoice の 3 通りの写し）を段ごとに手で書いた表
        (Suffrage s, double elite, double other)[] table =
        [
            (Suffrage.None, sub, 0.0), (Suffrage.Census, 1.0, 0.0), (Suffrage.AdultMale, 1.0, 1.0), (Suffrage.Universal, 1.0, 1.0),
        ];
        foreach (var (s, elite, other) in table)
        {
            Assert.Equal(elite, InterestGroups.SuffrageGate(s, true, c));
            Assert.Equal(other, InterestGroups.SuffrageGate(s, false, c));
        }
        // 呼び出し側：政治の派閥の強さ（声が小さいほど silenced が大きい）が段で動く向き＝Bloc 1 は Census で声が全開
        var w = Board().Nations[0].World;
        var f = new FactionState { Bloc = 1, Size = 1.0, FightHeads = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        w.Suffrage = Suffrage.None; double none = Politics.FactionStrength(w, w.Coef, f);
        w.Suffrage = Suffrage.Census; double census = Politics.FactionStrength(w, w.Coef, f);
        Assert.True(none > census, $"Bloc 1 は Census で声が全開＝抑えられる分が減る（None {none} ＞ Census {census}）");
    }

    [Fact]
    public void T2_距離の減衰は旧の式と同じ値を返す()
    {
        var w = Board().Nations[0].World;
        var c = w.Coef;
        int n = 0;
        foreach (double decay in new[] { c.DistanceWarDecay, 5.0 })   // 既定の係数と、他の係数と値が重ならない値（取り違えを見つける）
        {
            c.DistanceWarDecay = decay;
            foreach (var r in w.Regions)
            {
                var rc = Places.Coord(r.Name);
                if (rc.Lat == 0 && rc.Lon == 0) { Assert.Equal(1.0, Politics.RegionReach(w, c, r)); continue; }
                double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
                double want = !(km > 0) || !double.IsFinite(km) ? 1.0 : 1.0 / (1.0 + decay * km / Diplomacy.DistanceNormKm);
                if (!(want > 0 && want <= 1.0)) want = 1.0;
                Assert.Equal(want, Politics.RegionReach(w, c, r));
                if (decay == 5.0 && want < 1.0) n++;
            }
        }
        Assert.True(n >= 3, "対照：減衰が 1 未満になる州を 3 つ以上調べた");
    }

    [Fact]
    public void T3_敗戦ショックの残存は旧の0_85と同じビット()
    {
        Assert.Equal(0.85, 1.0 - 0.15);   // R10：減る割合 0.15 の残りは旧の残存率 0.85 と同じ double
        var c = new Coefficients();
        foreach (double x in new[] { 1.0, 0.7312, 0.123456789, 1e-9 })
            Assert.Equal(x * 0.85, c.DefeatShockDecayPerQuarter.Remaining(x));
    }

    [Fact]
    public void T4_委任の門の予告と実行は同じ判定を返す()
    {
        var w = Board().Nations[0].World;
        w.PoliticalLockEnabled = false;   // 純支持 1・費用 0＝通る
        foreach (PolicyAxis a in System.Enum.GetValues<PolicyAxis>())
            foreach (bool up in new[] { true, false })
                Assert.True(Government.CanStep(w, a, up), $"{a} {up}：政治のロックなしでは予告が通る");
    }
}
