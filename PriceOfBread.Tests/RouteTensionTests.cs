using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>13l地政学＝経路依存の緊張（docs/202608/20260814_緊張度をコアにする_部分完了.md 13l・作者裁定2026-08-14）の番人。
/// 法則＝「国は、自分の生命線（閉鎖海盆の唯一の出口）を扼する相手と対立する」。
/// (1)既定0の休眠ピン (2)ON経路＝依存度×係数がそのままAddTensionされること（露→土＝黒海×ボスポラスが国名なしで立つ）を固定する。
/// 観測の実証は 99_実装ログ§4-6（Runner の diploprobe〔2026-10-18 に DiploCore ごと削除〕の v4/v5＝普/瑞→丁1.0・露→土0.22・扼者交代の追随）。</summary>
public class RouteTensionTests
{
    /// <summary>★2026-09-03（キューB-02）：**点火した**（RouteDependenceTensionPerMonth=0.10＝ThreatTensionPerMonth と同桁）。
    /// 旧テストは「既定が0（休眠）」を固定していたので、点火の作法どおり**ONの側を固定する側へ書き換える**。
    /// ⚠OFF側（0で増分ゼロ）は**下の単離テストが末尾で明示的に踏んでいる**（点火したまま0へ戻して増分0を assert）＝
    /// ここに別のOFFテストを足すと恒真な番人が増えるだけなので足さない。</summary>
    [Fact]
    public void 係数は点火済みで扼者の緊張が走る()
    {
        double v = new Coefficients().RouteDependenceTensionPerMonth;
        Assert.True(v > 0, $"RouteDependenceTensionPerMonth が {v}＝休眠に戻っている（差し戻したなら本テストも戻すこと）");
    }

    [Fact]
    public void 黒海の出口を扼する国へ依存度に比例した緊張が立つ()
    {
        // 露＋オスマンの2国世界：ボスポラスの host（イスタンブール州）はオスマン領＝扼者に解決される。
        // ジブラルタル/エーレスンドの host は不在国＝扼者null＝スキップ（単離）。
        var sim = new MultiNationSim([WorldData.ByName("ロシア"), WorldData.ByName("オスマン")], seed: 17);
        var c = sim.Coef;
        // 他の全緊張チャネルを黙らせ、増分＝経路依存だけにする（会計の単離・ProtectorTests と同作法）
        c.ThreatTensionPerMonth = 0; c.IdeologyTensionPerMonth = 0; c.IdeologyAffinityRelief = 0;
        c.EthnicTensionPerMonth = 0; c.EthnicAffinityRelief = 0;
        c.LawTensionPerMonth = 0; c.ReligionTensionPerMonth = 0; c.ReligionAffinityRelief = 0;
        c.OpiumBanTensionPerMonth = 0; c.TensionDecay = 0; c.ProtectorTensionPerMonth = 0;
        c.ActionTensionStep = 0;   // ★2026-09-28 回8-1：AI の挑発も同じ動詞＝ならしが毎月動く（旧＝押した瞬間に入り切った）＝緊張の操作も黙らせる
        sim.StartWarmup();     // 扼者の解決（StepCanals）は助走中も通る
        sim.StepQuarter();     // 海盆プレゼンスの同期を最低1回通す（取扱高ゼロ＝港海盆の均等フォールバック）
        // ★2026-09-30 段1（財の削減）：開幕の四半期の露の港の取扱高（どの港に成約が立つか）は財の構成と助走の軌道の偶然＝
        //   旧は黒海の港（ロストフ）に小さな取扱高が偶然立っていた（財を畳むと成約はバルト海の港へ寄り、黒海の依存度が0になる）。
        //   この番人が測るのは「依存度に比例した緊張」の式＝依存度は手で置く（露の港の取扱高を黒海の港だけに1）。
        foreach (var r in sim.Nations[0].World.Regions) if (r.IsPort) r.LastPortThroughput = r.SeaBasin == Basin.BlackSea ? 1.0 : 0.0;
        var ru = sim.Nations[0].World.SelfPower!;
        var ot = sim.Nations[1].World.SelfPower!;
        var bosporus = sim.Diplomacy.Chokepoints.Find(x => x.Id == "bosporus")!;
        Assert.Equal((int)Basin.BlackSea, bosporus.ExitOfBasin);   // world.json の exitOfBasin が配線されている
        Assert.Same(ot, bosporus.Owner);                           // 扼者＝オスマン（host イスタンブールから解決）
        double before = sim.Diplomacy.Tension(ru, ot);
        c.RouteDependenceTensionPerMonth = 0.1;
        sim.Diplomacy.Step(c, sim.Rng, 1);   // 月1（四半期境界でない＝AI/介入は不走行・緊張チャネルのみ）
        // ⚠依存度は Step 後に読む＝同一ティック内で SyncBasinPresence（プレゼンス再同期）→ RouteTension の順に走るため、
        //   Step 前の値で検算すると同期分だけずれる（初回実走で実測：前0.139→使用値0.076）。
        double dep = ru.BasinPresence[(int)Basin.BlackSea];
        Assert.True(dep > 1e-9, "露の黒海プレゼンスが0＝テスト前提の破れ（港海盆フォールバック不発）");
        double added = sim.Diplomacy.Tension(ru, ot) - before;
        Assert.Equal(c.RouteDependenceTensionPerMonth * dep, added, 12);   // 増分＝rate×依存度・ちょうど1回

        // OFF側の対照（点火したまま0へ戻す＝ゲートの番人）：増分ゼロ
        c.RouteDependenceTensionPerMonth = 0;
        double before2 = sim.Diplomacy.Tension(ru, ot);
        sim.Diplomacy.Step(c, sim.Rng, 2);
        Assert.Equal(0.0, sim.Diplomacy.Tension(ru, ot) - before2, 12);
    }
}
