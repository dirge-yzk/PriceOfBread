using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1201) c120fm：統一国家の成立（Formations.Check）の述語を宣戦の候補と同じ形へ寄せる 2 本の旗の機構の番人と、計器 formation_s の番人。
/// 案 a＝<see cref="Coefficients.FormationNatioGate"/>（成立にも Formations.NationalistReady の門）・案 b＝<see cref="Coefficients.FormationOwnDirectOnly"/>（〈持つ〉が従属国の州を数えない）。
/// 盤面は試験の中で置く＝プロイセン・ライン諸邦・ザクセン・ハノーファーの 4 国。北ドイツ連邦の規則の州のうち開幕のプロイセンが持たない 3 州（ケルン・ドレスデン・ハノーファー）を、
/// 直接持たせる（州の割譲）か、持ち主の 3 国を属国にするかで置く。民族主義の取り分はプロイセンの全ての POP の取り分を 0 か 1 に置く。旗は全ての国の World.Coef に置く。
/// 期待値は手で書いた（付け替えの件数・州の数は world.json の開幕の州から数えた）。事前登録 measurements/m1201_20261118_prereg_fm.txt §5・§6。</summary>
[Collection("Instruments")]
public class FormationGateTests
{
    private static readonly string[] Names = ["プロイセン", "ライン諸邦", "ザクセン", "ハノーファー"];
    private static readonly string[] Need = ["ケルン", "ドレスデン", "ハノーファー"];   // 北ドイツ連邦の規則の州のうち開幕のプロイセンが持たない 3 州

    private enum Hold { Direct, Subjects }

    private static (MultiNationSim sim, Power prussia) Board(Hold hold, bool nationalist, double gateA, double gateB, bool player)
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => Names.Contains(n.Name))], 13UL, _ => new NullAgent());
        var d = sim.Diplomacy;
        Power P(string n) => d.Powers.First(p => p.BaseName == n);
        var pr = P("プロイセン");
        if (hold == Hold.Subjects)
            foreach (var n in new[] { "ライン諸邦", "ザクセン", "ハノーファー" }) d.SetDependency(pr, P(n));
        else
            foreach (var city in Need)
            {
                var from = d.Powers.First(p => p.Nation!.Regions.Exists(r => r.Name == city)).Nation!;
                Territory.TransferRegion(from, pr.Nation!, from.Regions.First(r => r.Name == city), Territory.Rehoming.Ceded);
            }
        foreach (var pop in pr.Nation!.AllPops())
        {
            pop.Consciousness = 0.5;   // 集計の重み（大きさ×意識×声）を正に置く
            Array.Clear(pop.IdeologyShares);
            pop.IdeologyShares[nationalist ? 4 : 0] = 1.0;   // 4＝民族主義・0＝伝統主義
        }
        foreach (var n in sim.Nations) { n.World.Coef.FormationNatioGate = gateA; n.World.Coef.FormationOwnDirectOnly = gateB; }
        pr.IsPlayer = player;
        return (sim, pr);
    }

    private static int Renamed(MultiNationSim sim) => sim.Diplomacy.Powers.Count(p => p.Name != p.BaseName);

    private static int RunCheck(Hold hold, bool nationalist, double gateA, double gateB, bool player = false)
    {
        var (sim, pr) = Board(hold, nationalist, gateA, gateB, player);
        Assert.Equal(0, Renamed(sim));   // 盤面の前提＝付け替えはまだ無い
        Formations.Check(sim.Diplomacy, sim.Coef);
        int n = Renamed(sim);
        if (n == 1) Assert.Equal("北ドイツ連邦", pr.Name);
        return n;
    }

    [Fact]
    public void T1_案aは取り分が門の外なら直接持っても成立しない()
    {
        Assert.Equal(1, RunCheck(Hold.Direct, nationalist: false, gateA: 0, gateB: 0));   // 旗 0＝今の規則（門なし）
        Assert.Equal(0, RunCheck(Hold.Direct, nationalist: false, gateA: 1, gateB: 0));   // 旗 1＝宣戦と同じ門で止まる
        Assert.Equal(1, RunCheck(Hold.Direct, nationalist: true, gateA: 1, gateB: 0));    // 対照＝取り分が門の内なら旗 1 でも成立（門が常に閉じる形ではない）
    }

    [Fact]
    public void T2_案bは属国の州を数えず直接持てば成立する()
    {
        Assert.Equal(1, RunCheck(Hold.Subjects, nationalist: false, gateA: 0, gateB: 0));   // 旗 0＝今の規則（属国の州を数える）
        Assert.Equal(0, RunCheck(Hold.Subjects, nationalist: false, gateA: 0, gateB: 1));   // 旗 1＝属国の州を数えない
        Assert.Equal(1, RunCheck(Hold.Direct, nationalist: false, gateA: 0, gateB: 1));     // 対照＝同じ州を直接持てば旗 1 でも成立
    }

    [Theory]
    [InlineData(0, false, 0.0, 0.0)]
    [InlineData(0, false, 1.0, 0.0)]
    [InlineData(0, true, 1.0, 0.0)]
    [InlineData(1, false, 0.0, 0.0)]
    [InlineData(1, false, 0.0, 1.0)]
    [InlineData(1, false, 1.0, 1.0)]
    public void T3_プレイヤーの国とAIの国で同じ盤面の成立が同じ(int hold, bool nationalist, double gateA, double gateB)
    {
        var h = (Hold)hold;
        int ai = RunCheck(h, nationalist, gateA, gateB, player: false);
        int me = RunCheck(h, nationalist, gateA, gateB, player: true);
        Assert.Equal(ai, me);
    }

    private static string[] Header => Instruments.FormationHeaderText.Split('\t');
    private static string Col(string[] row, string name) { int i = Array.IndexOf(Header, name); Assert.True(i >= 0, "列が無い " + name); return row[i]; }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T4_計器は付け替え1件に1行で州の数と使った属国を盤面の数で書く(double gateB)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_formation_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (sim, pr) = Board(Hold.Subjects, nationalist: false, gateA: 0, gateB: gateB, player: false);
            var d = sim.Diplomacy;
            int before = Instruments.FormationRowsPending(d).Count;
            Formations.Check(d, sim.Coef);
            var rows = Instruments.FormationRowsPending(d).Skip(before).Select(r => r.Split('\t')).ToList();
            if (gateB > 0) { Assert.Empty(rows); return; }   // 旗 b=1 は付け替え 0＝行 0
            Assert.Single(rows);
            var row = rows[0];
            Assert.Equal(Header.Length, row.Length);
            Assert.Equal("プロイセン", Col(row, "baseName"));
            Assert.Equal("北ドイツ連邦", Col(row, "newName"));
            Assert.Equal("1", Col(row, "tier"));
            Assert.Equal("Rules", Col(row, "ruleSource"));
            Assert.Equal("5", Col(row, "ownRegionsCount"));    // 開幕のプロイセンの州（world.json）＝ベルリン・シュテッティン・ケーニヒスベルク・ブレスラウ・ダンツィヒ
            Assert.Equal("4", Col(row, "depRegionsCount"));    // ライン諸邦 1・ザクセン 1・ハノーファー 2（クックスハーフェンを含む）
            Assert.Equal(new[] { "ザクセン", "ハノーファー", "ライン諸邦" }, Col(row, "usedDependents").Split('|').OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.Equal(0.0, double.Parse(Col(row, "natShare"), System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("1783", Col(row, "year"));
            _ = pr;
        }
        finally { Instruments.Configure(null, null); }
    }
}
