using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>同業組合法（特権 段4）のON経路。プチブル利益束（第4束「小営業」・旧 PettyInterestGate）の試験は 2026-10-11 (271) に束ごと削除した。</summary>
public class GuildLawTests
{
    /// <summary>★特権 段4（同業組合法・2026-09-07）ON経路：保護の下では職人の自営レシピの財に上乗せが乗り（消費者→職人・貨幣保存）台帳に取り分が載る。
    /// 廃止すると上乗せは消える（取り分0）。GuildLawGate=0 の恒等は DormantIdentity が番人。</summary>
    [Fact]
    public void 同業組合法は職人の上乗せを消費者から職人へ移し台帳に載り廃止で消える()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.PrivilegeLedgerEnabled = 1; w.Coef.GuildLawGate = 1; w.Coef.GuildRentPremium = 0.15;
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
