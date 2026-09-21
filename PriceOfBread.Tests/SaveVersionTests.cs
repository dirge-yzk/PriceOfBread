using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>セーブのバージョン耐性（POBSIM2・2026-08-31）の番人。
/// プロイセン走§4-4＝係数を1本足しただけで古いセーブが「Boolean cannot be converted to Double」で全滅した
/// （旧POBSIM1はフィールド名を書かず**ソート順の位置**で読むため、追加1本で以降が全部ずれる）。
/// v2はフィールド名テーブルで突き合わせ＝保存側だけの名前は読み捨て・現行側だけの名前は既定値（無引数ctorドナー）。</summary>
public class SaveVersionTests
{
    private static MultiNationSim SmallSim(ulong seed)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed, _ => new UtilityAgent());
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        return sim;
    }

    [Fact]
    public void 往復は等価で二度目の保存はビット一致()
    {
        var sim1 = SmallSim(7);
        using var ms1 = new MemoryStream();
        SaveGame.Save(sim1, ms1);
        ms1.Position = 0;
        var sim2 = SaveGame.Load(ms1);
        using var ms2 = new MemoryStream();
        SaveGame.Save(sim2, ms2);
        Assert.Equal(ms1.ToArray(), ms2.ToArray());   // 名前表込みでも保存はビット決定論
    }

    [Fact]
    public void 保存側にしか無いフィールド名は読み捨てられ現行側の欠落は既定値で埋まる()
    {
        // 「古いビルドのセーブ」を合成する：現行セーブのフィールド名表で FactoryBuildAffordSteep の
        // キー1件を同じ長さの偽名に書き換える＝読み手から見ると「保存側に知らない名前が1つ・現行側に無い名前が1つ」
        // ＝追加と削除が同時に起きた世界。値そのものは自己記述タグなので安全に読み飛ばせるはず。
        var sim1 = SmallSim(7);
        foreach (var n in sim1.Nations) n.World.Coef.FactoryBuildAffordSteep = 99.0;   // 保存値≠既定値にして「どちらが載ったか」を判別可能に
        using var ms = new MemoryStream();
        SaveGame.Save(sim1, ms);
        byte[] buf = ms.ToArray();

        byte[] key = System.Text.Encoding.UTF8.GetBytes("PriceOfBread.SimCore.Coefficients.FactoryBuildAffordSteep");
        byte[] fake = System.Text.Encoding.UTF8.GetBytes("PriceOfBread.SimCore.Coefficients.FactoryBuildAffordSteeq");   // 同じ長さ＝長さ接頭辞を壊さない
        int hits = 0;
        for (int i = 0; i + key.Length <= buf.Length; i++)
        {
            bool eq = true;
            for (int j = 0; j < key.Length; j++) if (buf[i + j] != key[j]) { eq = false; break; }
            if (eq) { System.Array.Copy(fake, 0, buf, i, fake.Length); hits++; }
        }
        Assert.Equal(1, hits);   // 名前表は型の初出に1度だけ＝2件以上なら表の設計が壊れている

        using var tampered = new MemoryStream(buf);
        var sim2 = SaveGame.Load(tampered);   // 旧形式ならここで位置ずれの型例外で死ぬ
        double expected = new Coefficients().FactoryBuildAffordSteep;
        foreach (var n in sim2.Nations)
        {
            Assert.Equal(expected, n.World.Coef.FactoryBuildAffordSteep);            // 欠落＝ドナー既定値（保存値99ではない）
            Assert.Equal(99.0, sim1.Nations[0].World.Coef.FactoryBuildAffordSteep);  // 元の系列は無傷
        }
        var ex = Record.Exception(() => { for (int q = 0; q < 2; q++) sim2.StepQuarter(); });
        Assert.Null(ex);   // 読めるだけでなく進められる
    }
}
