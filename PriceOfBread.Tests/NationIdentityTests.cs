using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 国家アイデンティティ（2026-07-12）の番人：①変名は表示名のみ＝BaseName（忠実度照合・宗主同一性の鍵）は不変
/// ②formable（統一変名）は従属国の領有も版図に数えて成立 ③分離独立の建国＝州の物理移転で新国家がスポーンし保存則が立つ。
/// 既定（EnableFormations=0・SecessionSpawnsNation=0）は旧挙動と厳密一致＝既存162本が番人。
/// </summary>
public class NationIdentityTests
{
    private static NationConfig[] Pick(params string[] names)
        => [.. WorldData.Nations().Where(c => names.Contains(c.Name))];

    [Fact]
    public void 変名は表示名のみでBaseNameは不変_同名は先着のみ()
    {
        var sim = new MultiNationSim(Pick("プロイセン", "オーストリア"), 11UL, _ => new NullAgent());
        var prussia = sim.Diplomacy.Powers.First(p => p.BaseName == "プロイセン");
        var austria = sim.Diplomacy.Powers.First(p => p.BaseName == "オーストリア");
        Assert.True(sim.Diplomacy.RenameNation(prussia, "ドイツ"));
        Assert.Equal("ドイツ", prussia.Name);
        Assert.Equal("プロイセン", prussia.BaseName);
        Assert.Contains(sim.Diplomacy.Log, l => l.Contains("ドイツの成立を宣言した"));
        Assert.False(sim.Diplomacy.RenameNation(austria, "ドイツ"));   // 世界に同名は一つ（先着）
        Assert.False(sim.Diplomacy.RenameNation(austria, "プロイセン"));   // 構築時名too＝他国のBaseNameも予約済み
    }

    [Fact]
    public void 北ドイツ連邦_直接領有と従属国の領有で成立する()
    {
        var sim = new MultiNationSim(Pick("プロイセン", "ライン諸邦", "ザクセン", "ハノーファー"), 13UL, _ => new NullAgent());
        var prussia = sim.Diplomacy.Powers.First(p => p.BaseName == "プロイセン");
        foreach (var n in new[] { "ライン諸邦", "ザクセン", "ハノーファー" })
            sim.Diplomacy.SetDependency(prussia, sim.Diplomacy.Powers.First(p => p.BaseName == n));   // 属国化＝本シムの統一の正史経路
        sim.Coef.EnableFormations = 1.0;
        Formations.Check(sim.Diplomacy, sim.Coef);
        Assert.Equal("北ドイツ連邦", prussia.Name);
        Assert.Equal("プロイセン", prussia.BaseName);
        Assert.Equal(1, prussia.FormationTier);
        // 一方向性：もう一度チェックしても同Tier以下は再発火しない
        Formations.Check(sim.Diplomacy, sim.Coef);
        Assert.Equal("北ドイツ連邦", prussia.Name);
    }

    [Fact]
    public void UI変体パネルの源_プロイセンは4つの変体を進捗つきで名乗れる()
    {
        var sim = new MultiNationSim(Pick("プロイセン"), 3UL, _ => new NullAgent());
        sim.StepQuarter();   // 文化/宗教は毎月同期＝実ゲームで見る時点（構築直後は空）を再現
        var prussia = sim.Diplomacy.Powers.First(p => p.BaseName == "プロイセン");
        // 同一性で名乗れる変体（領有は問わない＝進捗表示の対象）＝BaseName/文化/宗教で適格な全Tier
        var eligible = Formations.Rules
            .Where(r => r.Tier > prussia.FormationTier && Formations.IdentityEligible(prussia, r))
            .Select(r => r.NewName).ToHashSet();
        Assert.Contains("北ドイツ連邦", eligible);   // BaseName プロイセン
        Assert.Contains("ドイツ", eligible);         // BaseName/文化 ドイツ
        Assert.Contains("神聖ローマ", eligible);     // BaseName プロイセン
        Assert.Contains("ローマ", eligible);         // 文明ゲート＝プロテスタント∈地中海圏
        // 必要州の進捗＝直接∪従属国の領有。開幕プロイセンはベルリン等を持つがケルン/ドレスデン/ハノーファーは未領有＝未達
        Assert.True(Formations.OwnsRegion(sim.Diplomacy, prussia, "ベルリン"));
        Assert.False(Formations.OwnsRegion(sim.Diplomacy, prussia, "ケルン"));
        var nordbund = Formations.Rules.First(r => r.NewName == "北ドイツ連邦");
        int have = nordbund.RequiredAll.Count(n => Formations.OwnsRegion(sim.Diplomacy, prussia, n));
        Assert.InRange(have, 1, nordbund.RequiredAll.Length - 1);   // 一部達成（開幕では未完成）
    }

    [Fact]
    public void フランスの列強順位は開幕から上位_リスト生成順の32位ではない()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        sim.StepQuarter();   // ComputeRanks が走った状態
        var dip = sim.Diplomacy;
        var france = dip.Powers.First(p => p.BaseName == "フランス");
        Assert.Equal(PowerRank.Great, dip.RankOf(france));   // 仏は開幕から列強
        // VMと同じ計算＝国力スコアで自国より上の列強の数＋1（旧＝Powersリストの生成順インデックスは誤り）
        double myScore = NationalScore.Of(france.Nation!);
        int myPos = 1;
        foreach (var q in dip.Powers)
        {
            if (q == france || q.Nation is not { } qw || dip.RankOf(q) != PowerRank.Great) continue;
            double qs = NationalScore.Of(qw);
            if (qs > myScore || (qs == myScore && q.Id < france.Id)) myPos++;
        }
        Assert.InRange(myPos, 1, sim.Coef.GreatPowerCount);   // 列強定員内（≤7）＝32位のような列外の値にならない
    }

    [Fact]
    public void 分離独立の建国_州が物理移転し新国家と旧宗主の保存則が立つ()
    {
        var configs = Pick("オーストリア", "イギリス");
        var sim = new MultiNationSim(configs, 17UL, _ => new NullAgent());
        var austria = sim.Nations.First(n => n.World.SelfPower!.BaseName == "オーストリア");
        var w = austria.World;
        var brussels = w.Regions.First(r => r.Name == "ブリュッセル");
        double savings = brussels.SumSavings(); double pop = brussels.SumPopulation();
        // 分離独立の予約（支配度が尽きた州＝Politics.CheckSecession が積む）を置く＝四半期末に建国＋分離独立戦争。
        // ★2026-09-28（処理の一元化 段2 束3）：旧＝旧経路の単一州の分離戦（CivilWarYears・係数 SecessionSpawnsNation）を注入して決着で建国＝その経路は削除（作者裁定 g）。
        w.PendingSecessionWars = [brussels];
        int nations0 = sim.Nations.Count, powers0 = sim.Diplomacy.Powers.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count >= nations0 + 1);
        Assert.Equal(sim.Nations.Count - nations0, sim.Diplomacy.Powers.Count - powers0);
        var belgium = sim.Nations.First(n => n.World.SelfPower!.BaseName == "ベルギー").World;
        Assert.Contains(sim.Diplomacy.Wars, x => x.CivilWarSecession && x.CivilWarParent == w.SelfPower && (x.A == belgium.SelfPower || x.B == belgium.SelfPower));
        Assert.Equal("ベルギー", belgium.SelfPower!.Name);
        Assert.Equal("ベルギー", belgium.SelfPower!.BaseName);
        Assert.Same(brussels, belgium.Region);            // 同一Region実体の物理移転（首都Id0）
        Assert.Equal(0, brussels.Id);
        Assert.DoesNotContain(brussels, w.Regions);       // 親からは消えた
        Assert.True(belgium.TotalPopulation > pop * 0.9); // 人は州ごと移った
        Assert.True(belgium.Treasury > 0);                // 国庫の人口比分与
        Assert.True(savings > 0);
        // 双方が翌四半期も正常に鼓動する（Invariants例外なし）
        sim.StepQuarter();
        Assert.True(belgium.Gdp >= 0 && w.Gdp >= 0);
    }
}
