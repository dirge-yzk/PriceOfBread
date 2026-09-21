using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>P11 東方問題パック「同宗教保護」（原典2026-07-17・実装2026-08-09）の番人。
/// ★2026-08-09に点火（既定0.02/0.5・ign_p11）。0へ戻せば全新コードが >1e-12 ガードの内側で1命令も実行しない。
/// ここでは (1)既定値ピン（点火0.02/0.5） (2)ON経路＝grievance式と緊張増分 (3)介入口実の乗算 (4)実世界で東方問題が立つこと を固定する。</summary>
public class ProtectorTests(ITestOutputHelper output)
{
    // ── 合成ON世界：A=スイス（1州を正教へ＝正教国）・B=トスカーナ（2州を正教300/イスラム700＝イスラム国だが popShare 0.3 が正教）・
    //    C=ジェノヴァ（1州をイスラムへ＝Bと同宗教）。宗教ラベルだけ書き換え＝台本なし（座標・構造は実データのまま）。
    private static MultiNationSim Synthetic()
    {
        var sim = new MultiNationSim([WorldData.ByName("スイス"), WorldData.ByName("トスカーナ"), WorldData.ByName("ジェノヴァ")], seed: 17);
        var swiss = sim.Nations[0].World; var tuscany = sim.Nations[1].World; var genoa = sim.Nations[2].World;
        swiss.Regions[0].Religion = "正教";                                       // A＝正教国
        tuscany.Regions[0].Religion = "正教"; SetPop(tuscany.Regions[0], 300);   // Bの1州＝正教・人口比 300/1000＝0.3
        tuscany.Regions[1].Religion = "イスラム"; SetPop(tuscany.Regions[1], 700);
        genoa.Regions[0].Religion = "イスラム";                                   // C＝全州イスラム（Bと同宗教）
        return sim;
    }
    private static void SetPop(Region r, double size)
    {
        for (int i = 0; i < r.Pops.Count; i++) r.Pops[i].Size = i == 0 ? size : 0;
    }

    [Fact]
    public void 係数の既定は点火済み()
    {
        var c = new Coefficients();
        Assert.Equal(0.02, c.ProtectorTensionRate);   // 点火2026-08-09（ign_p11＝硬性緑・Δ総合+8.2）。0に戻せばビット恒等（既存フルスイートが番人）。休眠側の設計値0.02目安
        Assert.Equal(0.5, c.ProtectorInterventionWeight); // 点火2026-08-09（同上）。0に戻せば乗算自体を踏まない。旧休眠側の設計値0.5目安（点火は測定ラウンドで）
    }

    [Fact]
    public void 保護利害は相手国内の同宗教州人口比で緊張は式どおり増える()
    {
        var sim = Synthetic();
        var c = sim.Coef;
        // 他の全緊張チャネルを黙らせ、緊張の増分＝同宗教保護だけにする（会計の単離）
        c.ThreatTensionRate = 0; c.IdeologyTensionRate = 0; c.IdeologyAffinityRelief = 0;
        c.EthnicTensionRate = 0; c.EthnicAffinityRelief = 0;
        c.LawTensionRate = 0; c.ReligionTensionRate = 0; c.ReligionAffinityRelief = 0;
        c.OpiumBanTensionRate = 0; c.TensionDecay = 0;
        c.ProtectorTensionRate = 0.02;   // 休眠側の設計値で点火
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!; var g3 = sim.Nations[2].World.SelfPower!;
        double tAB0 = sim.Diplomacy.Tension(a, b), tAC0 = sim.Diplomacy.Tension(a, g3), tBC0 = sim.Diplomacy.Tension(b, g3);
        sim.Diplomacy.Step(c, sim.Rng, 1);   // 月1＝四半期境界でない（AI/介入/編成は不走行）。宗教同期→緊張チャネルが同ティックで走る
        // grievance の式：Bの活性州のうち religion==religion(A) の人口比（religion(A)≠religion(B) が前提ガード）
        double gab = Diplomacy.ProtectorGrievance(a, b);
        Assert.Equal(0.3, gab, 12);                                    // 300/(300+700)
        Assert.Equal(0.0, Diplomacy.ProtectorGrievance(b, a));         // Aの州は全部Aの宗教＝0
        Assert.Equal(0.0, Diplomacy.ProtectorGrievance(a, g3));        // Cの州は全部Cの宗教＝0
        Assert.Equal(0.0, Diplomacy.ProtectorGrievance(g3, b));        // 同宗教の対（C=B=イスラム）＝利害は立たない
        // 緊張の増分＝rate×(gAB+gBA)＝rate×0.3（同じ場所・同じAddTensionクランプで積まれたことの検証）
        Assert.Equal(c.ProtectorTensionRate * gab, sim.Diplomacy.Tension(a, b) - tAB0, 12);
        Assert.Equal(0.0, sim.Diplomacy.Tension(a, g3) - tAC0, 12);    // 利害なしの対は増えない
        Assert.Equal(0.0, sim.Diplomacy.Tension(b, g3) - tBC0, 12);    // 同宗教の対は増えない
    }

    [Fact]
    public void 介入動機は同宗教徒を抱える交戦国への口実で増える()
    {
        var sim = Synthetic();
        var c = sim.Coef;
        sim.Diplomacy.Step(c, sim.Rng, 1);   // 宗教ラベルの同期
        var g = sim.Nations[0].World.SelfPower!;    // 介入者＝正教国A
        var tus = sim.Nations[1].World.SelfPower!; var gen = sim.Nations[2].World.SelfPower!;
        g.Military = 4; gen.Military = 5; tus.Military = 1;   // 動機の基礎値を正に固定（軍ゲージは表示同期値＝直接置いてよい）
        var war = new War { A = gen, B = tus, Score = 0.2 };  // 勝者＝ジェノヴァ。同宗教徒を抱えるのは敗者側トスカーナ＝どちら側でも口実になる（max）
        c.ProtectorInterventionWeight = 0.0;   // OFF側を明示（既定は点火済み0.5＝2026-08-09。既定非依存の対照実験）
        double off = sim.Diplomacy.InterventionMotive(g, war, c);   // weight=0＝既存式そのまま
        Assert.True(off > 0, "動機の基礎値が0＝テスト前提の破れ");
        c.ProtectorInterventionWeight = 0.5;
        double on = sim.Diplomacy.InterventionMotive(g, war, c);
        double grievance = Diplomacy.ProtectorGrievance(g, tus);
        Assert.Equal(0.3, grievance, 12);
        Assert.Equal(off * (1.0 + c.ProtectorInterventionWeight * grievance), on, 12);   // ×(1+weight×grievance) の1箇所だけ
    }

    [Fact]
    public void 実世界では露オスマン級の保護利害が上位に立つ()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 1);   // 宗教ラベル同期のみ（1783年開始盤面の構造をそのまま読む）
        var rows = new List<(double g, string a, string b)>();
        foreach (var pa in sim.Diplomacy.Powers)
            foreach (var pb in sim.Diplomacy.Powers)
            {
                if (pa == pb || pa.Nation is null || pb.Nation is null) continue;
                double gg = Diplomacy.ProtectorGrievance(pa, pb);
                if (gg > 1e-9) rows.Add((gg, pa.Name, pb.Name));
            }
        rows.Sort((x, y) => y.g != x.g ? y.g.CompareTo(x.g) : string.CompareOrdinal(x.a + x.b, y.a + y.b));
        foreach (var r in rows.Take(10)) output.WriteLine($"{r.a} → {r.b} : {r.g:F4}");
        double Find(string a, string b) { foreach (var r in rows) if (r.a == a && r.b == b) return r.g; return 0; }
        // 東方問題の両輪が実データから立つ（正確な値は人口動態依存＝ピンしない・存在だけを固定）
        Assert.True(Find("ロシア", "オスマン") > 0.01, "露→オスマン（バルカン正教保護）の利害が立たない");
        Assert.True(Find("オスマン", "ロシア") > 0.001, "オスマン→露（イスラム州＝汎イスラム主義）の利害が立たない");
    }
}
