using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(487) 線 mg2：農奴制の初期値（world.json の <c>initialSerfEmancipation</c>）の番人。作者 2026-10-21「農奴制の初期値→推奨」。
/// 期待は**手で書いた表**（事前登録 measurements/mg487_20261021_prereg.txt [3]）＝世界データから作らない。表に無い国は全面（既定）。
/// ★(491) 合流で旗の番人（③④）は枝 mg2 に残した＝ここは①②と⑤（データが既存の読み手へ届く）。
/// ①全 61 国の初期値が表と一致 ②表の名前が世界データに全て在る（綴りの取り違えで表が空振りしない）③農奴の門の倍率が段の通りに効き、清は止まらず・日本は全面の半分・露は 0。
/// ④清と日本の国内の移民の農村の出が開幕から動く（旧データでは農奴解放がなし＝農村の出が全期 0 だった）。</summary>
public class SerfInitialDataTests
{
    private static readonly string[] None = ["ロシア", "プロイセン", "ポーランド"];
    private static readonly string[] Limited =
    [
        "フランス", "日本", "オスマン", "オーストリア", "スペイン", "サルデーニャ＝ピエモンテ", "ポルトガル",
        "デンマーク", "ライン諸邦", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク",
        "両シチリア", "教皇領", "ビルマ", "シャム", "カンボジア", "ラオス", "エチオピア",
    ];

    [Fact]
    public void 世界データの農奴解放の初期値は史実の表と全61国で一致する()
    {
        var all = WorldData.Nations();
        Assert.Equal(61, all.Length);
        var listed = None.Concat(Limited).ToArray();
        Assert.Equal(listed.Length, listed.Distinct().Count());
        foreach (var n in listed) Assert.Contains(all, c => c.Name == n);   // 表の綴りが世界データに在る
        var wrong = new List<string>();
        foreach (var c in all)
        {
            var expect = None.Contains(c.Name) ? ReformLevel.None : Limited.Contains(c.Name) ? ReformLevel.Limited : ReformLevel.Full;
            if (c.InitialSerfEmancipation != expect) wrong.Add($"{c.Name}（期待 {expect}・実際 {c.InitialSerfEmancipation}）");
        }
        Assert.True(wrong.Count == 0, string.Join("、", wrong));
        Assert.Equal(3, all.Count(c => c.InitialSerfEmancipation == ReformLevel.None));
        Assert.Equal(20, all.Count(c => c.InitialSerfEmancipation == ReformLevel.Limited));
        Assert.Equal(38, all.Count(c => c.InitialSerfEmancipation == ReformLevel.Full));
    }

    /// <summary>★(491) 合流：旗 SerfGateCoversMigration は採らなかった（枝 mg2 に残す）＝旗の番人 2 本は枝に残し、ここでは採ったデータが既存の読み手
    /// （<see cref="Privileges.SeigneurialDegree"/>＝領主権の度合い・国民皆兵の門・PopDecision の農村→都市の門・FarmRentCap ほか）へ届くことだけを見る。期待は手で書いた数。</summary>
    [Fact]
    public void 採ったデータは構築後の領主権の度合いに届く_清0_日本半分_ロシア1()
    {
        World W(string name) => new MultiNationSim([WorldData.ByName(name)], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true }.Nations[0].World;
        Assert.Equal(0.0, Privileges.SeigneurialDegree(W("清")));
        Assert.Equal(0.0, Privileges.SeigneurialDegree(W("スイス")));
        Assert.Equal(0.5, Privileges.SeigneurialDegree(W("日本")));
        Assert.Equal(1.0, Privileges.SeigneurialDegree(W("ロシア")));
        Assert.Equal(1.0, Privileges.SeigneurialDegree(W("プロイセン")));
        Assert.True(Government.UniversalConscriptionOpen(W("清")));     // 全面＝門が開く（反証役 mg2〔bv〕中#1 の副作用＝00一覧 §0-2 Q-mg9）
        Assert.False(Government.UniversalConscriptionOpen(W("日本")));  // 限定＝門は閉じたまま
    }
}
