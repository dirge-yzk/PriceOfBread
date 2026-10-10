using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// プチブル利益束（第4束「小営業」＝職人+商人・作者承認2026-08-15・休眠 PettyInterestGate=0）のON経路。
/// (a) gate=1 で独禁法の賛否に「小営業」束が**賛成側**で現れる（SupportBreakdown）。
/// (b) gate=0 では第4束が一切現れず、gate=1 でも**非対象軸**の集計は変わらない（フォールバック＝旧束の値＝gate=0と同じ値・設計書§2）。
/// </summary>
public class PettyInterestTests
{
    /// <summary>英で温め、職人（旧・大衆束）にも声がある状態＋小営業の自営所得を確保（束の力と強さが0にならない土台）。</summary>
    private static World Warm()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        w.Suffrage = Suffrage.AdultMale;   // 職人（旧・大衆束）に発言権がある状態＝小営業の力が立つ
        foreach (var r in w.Regions)       // 自営所得を明示的に注入（月境で Earned が薄い瞬間でも強さの導出元が消えない）
        {
            if (r.ArtisanPopIndex >= 0 && r.ArtisanPopIndex < r.Pops.Count) r.Pops[r.ArtisanPopIndex].EarnedPerMonth = 1_000;
            if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) r.Pops[r.MerchantPopIndex].EarnedPerMonth = 1_000;
        }
        return w;
    }

    [Fact]
    public void gate1で独禁法の賛否に小営業束が賛成側で現れる()
    {
        var w = Warm();
        w.Coef.PettyInterestGate = 0;
        foreach (var (who, _) in PolicyPreferences.SupportBreakdown(w, PolicyAxis.Antitrust, increasing: true))
            Assert.NotEqual("小営業", who);   // 休眠＝第4束はUIホバーにも現れない
        w.Coef.PettyInterestGate = 1.0;
        double petty = double.NaN;
        foreach (var (who, weight) in PolicyPreferences.SupportBreakdown(w, PolicyAxis.Antitrust, increasing: true))
            if (who == "小営業") petty = weight;
        Assert.False(double.IsNaN(petty), "gate=1なのに独禁法のSupportBreakdownに「小営業」束が現れない");
        Assert.True(petty > 0, $"小営業が独禁法の賛成側にいない（{petty:E2}）＝企業集中×自営所得シェアが立っていない");
    }

    [Fact]
    public void gate0は従来と一致しgate1でも非対象軸の集計は変わらない()
    {
        var w = Warm();
        // gate=0：全軸で第4束の行が無い＋純支持を記録
        w.Coef.PettyInterestGate = 0;
        var axes = System.Enum.GetValues<PolicyAxis>();
        var net0 = new System.Collections.Generic.Dictionary<PolicyAxis, double>();
        var opp0 = new System.Collections.Generic.Dictionary<PolicyAxis, double>();
        foreach (var a in axes)
        {
            net0[a] = PolicyPreferences.NetSupport(w, a, increasing: true);
            opp0[a] = PolicyPreferences.OpposingPower(w, a, increasing: true, PolicyPreferences.Build(w));
            foreach (var (who, _) in PolicyPreferences.SupportBreakdown(w, a, increasing: true))
                Assert.NotEqual("小営業", who);
        }
        // gate=1：非対象軸は旧束の値へフォールバック＝集計は浮動小数の結合順を除き不変（対象3本から広げない・設計書§2）
        w.Coef.PettyInterestGate = 1.0;
        foreach (var a in axes)
        {
            double n1 = PolicyPreferences.NetSupport(w, a, increasing: true);
            double o1 = PolicyPreferences.OpposingPower(w, a, increasing: true, PolicyPreferences.Build(w));
            if (a is PolicyAxis.Antitrust or PolicyAxis.Tariff
                  or PolicyAxis.IncomeTax or PolicyAxis.ProgressiveTax
                  or PolicyAxis.Guild) continue;   // 対象3本（表の行＝累進/所得税は1行2軸）＋同業組合法（特権 段4・2026-09-07＝職人自身の特権＝小営業束の立場）＝束の立場が入るので除外
                                                   // ⚠専売は2026-09-20に軸ごと撤去＝対象から外れた（実負担が誰に当たるかは経済の帰着であって政策軸への立場ではない）
            Assert.True(System.Math.Abs(n1 - net0[a]) < 1e-6 * System.Math.Max(1.0, System.Math.Abs(net0[a])),
                $"非対象軸 {a} の純支持が第4束で動いた（gate0 {net0[a]:F9} / gate1 {n1:F9}）＝フォールバックが旧束の値になっていない");
            Assert.True(System.Math.Abs(o1 - opp0[a]) < 1e-6 * System.Math.Max(1.0, System.Math.Abs(opp0[a])),
                $"非対象軸 {a} の反対力が第4束で動いた（gate0 {opp0[a]:F9} / gate1 {o1:F9}）");
        }
    }

    /// <summary>★特権 段4（同業組合法・2026-09-07）ON経路：保護の下では職人の自営レシピの財に上乗せが乗り（消費者→職人・貨幣保存）台帳に取り分が載る。
    /// 廃止すると上乗せは消える（取り分0）。GuildLawGate=0 の恒等は DormantIdentity が番人。</summary>
    [Fact]
    public void 同業組合法は職人の上乗せを消費者から職人へ移し台帳に載り廃止で消える()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.PrivilegeLedgerEnabled = 1; w.Coef.GuildLawGate = 1; w.Coef.GuildRentPremium = 0.15; w.Coef.GuildMobilityDrag = 0.5;
        var money = new MoneyConservation(w);
        sim.Run(8);
        money.Check(w);
        Assert.Equal(GuildLaw.Full, w.Guild);
        Assert.True(w.PrivilegeTakeYear[Privileges.Guild] > 0, $"同業組合の取り分が台帳に載っていない（年換算 {w.PrivilegeTakeYear[Privileges.Guild]:F3}）＝職人の自営レシピが供給していないか配線漏れ");
        w.Guild = GuildLaw.None; w.Policy.Guild = GuildLaw.None;   // 剥離（経路でなく直接＝上乗せの消え方だけを見る）
        sim.Run(4);
        Assert.Equal(0.0, w.PrivilegeTakeMonth[Privileges.Guild]);
        money.Check(w);
    }
}
