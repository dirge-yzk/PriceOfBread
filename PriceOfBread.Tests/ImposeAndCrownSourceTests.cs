using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(928) 線 c81im：計器 <c>imp_s</c>（矯正講和 1 件 1 行）と <c>crownpc_s</c> の末尾の列 <c>crownRegisterSourceCode</c> の番人（読むだけの計器の不変条件）。
/// 期待値は計器と別の出所：政体の数値は列挙の手書きの整数（計器の表を読まない）・イデオロギーの隔たりは Diplomacy の表から手で引いた 0.45。
/// 突然変異（measurements/m928_20261107/mutation.txt）：M1 ImpAfter の呼び口を外す／M2 orderInMonthIndex を常に 1 にする／M3 Politics の Transition の口の符号を 0 にする。</summary>
[Collection("Instruments")]
public class ImposeAndCrownSourceTests
{
    private static string[] Header => Instruments.ImpHeaderText.Split('\t');
    private static string Col(string[] row, string name) { int i = System.Array.IndexOf(Header, name); Assert.True(i >= 0, "列が無い " + name); return row[i]; }

    private static T WithInstruments<T>(System.Func<T> f)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_imp_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { return f(); }
        finally { Instruments.Configure(null, null); }
    }

    /// <summary>フランス（絶対王政 0.0）がイギリス（立憲君主 0.45）に壊滅級で勝つ＝隔たり 0.45 ≥ 閾値 0.4 で矯正講和が 1 件。</summary>
    private static (MultiNationSim sim, Power fr, Power uk, System.Collections.Generic.List<string> rows, int ukPolityBefore, int ukCrownBefore, double legBefore) RunImpose()
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.CollapsePeaceOneShare = 1;               // 厭戦の崩壊の講和は満額 1.5（CpeaceColumnsTests と同じ盤面）
        c.ColonyCaptureBudget = 0;
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!;
        foreach (var p in new[] { fr, uk, es }) p.WarExhaustion = 0;
        uk.WarExhaustion = 1.0;
        int polB = (int)uk.Nation!.Polity, crB = (int)uk.Nation!.Crown; double legB = uk.Nation!.Legitimacy;
        sim.Diplomacy.Wars.Add(new War { A = fr, B = uk, PushA = 3e12, PushB = 1 });
        sim.Diplomacy.Wars.Add(new War { A = uk, B = es, PushA = 1, PushB = 1e12 });
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        return (sim, fr, uk, Instruments.ImpRowsPending(sim.Diplomacy), polB, crB, legB);
    }

    [Fact]
    public void 壊滅級の勝利で矯正講和が起きると1行が前後の値つきで書かれ王冠の書き手は2()
    {
        WithInstruments(() =>
        {
            var (sim, fr, uk, rows, polB, crB, legB) = RunImpose();
            Assert.True(rows.Count > 0, string.Join("|", sim.Diplomacy.Log.TakeLast(5)) + " wars=" + sim.Diplomacy.Wars.Count);
            Assert.Equal(Header.Length, Instruments.ImpHeaderText.Split('\t').Length);
            Assert.Equal(1, rows.Count(r => true));                     // 矯正講和は 1 件
            Assert.Equal(1, sim.Diplomacy.Log.Count(l => l.Contains("の政体を矯正")));   // ログの件数と行数が合う
            var row = rows[0].Split('\t'); Assert.Equal(Header.Length, row.Length);
            Assert.Equal("20", Col(row, "quarter"));                    // 61 / 3
            Assert.Equal("61", Col(row, "month"));
            Assert.Equal("フランス", Col(row, "winner")); Assert.Equal("イギリス", Col(row, "loser"));
            Assert.Equal(polB.ToString(), Col(row, "loserPolityBeforeCode"));
            Assert.Equal(crB.ToString(), Col(row, "loserCrownBeforeCode"));
            Assert.Equal(((int)fr.Nation!.Polity).ToString(), Col(row, "winnerPolityCode"));
            Assert.Equal(((int)uk.Nation!.Polity).ToString(), Col(row, "imposedPolityCode"));   // 矯正の後＝勝者の政体
            Assert.Equal(((int)fr.Nation!.Polity).ToString(), Col(row, "imposedPolityCode"));
            Assert.Equal(((int)uk.Nation!.Crown).ToString(), Col(row, "loserCrownAfterCode"));
            Assert.Equal(0.45, double.Parse(Col(row, "ideologyGapIndex"), System.Globalization.CultureInfo.InvariantCulture), 12);   // 立憲 0.45 − 絶対 0.0
            Assert.Equal(1.5, double.Parse(Col(row, "absScoreIndex"), System.Globalization.CultureInfo.InvariantCulture), 12);   // 厭戦の崩壊の講和は満額 1.5（RunImpose の盤面の注）
            Assert.Equal("1", Col(row, "crushingFlag")); Assert.Equal("1", Col(row, "orderInMonthIndex"));
            Assert.Equal(legB, double.Parse(Col(row, "legitimacyBeforeShare"), System.Globalization.CultureInfo.InvariantCulture), 9);
            Assert.Equal(uk.Nation!.Legitimacy, double.Parse(Col(row, "legitimacyAfterShare"), System.Globalization.CultureInfo.InvariantCulture), 9);
            Assert.Equal(2, Instruments.CrownSourceOf(uk.Nation!));      // 王冠の登録簿を最後に書いたのは矯正講和
            // 前後の列を区別する盤面（敗者の王冠と政体を矯正の間に変える）＝Before は前の値・After は後の値で読む
            var pre = Instruments.ImpBefore(uk.Nation!);
            var polPre = uk.Nation!.Polity; var crPre = uk.Nation!.Crown;
            uk.Nation!.Crown = crPre == CrownForm.None ? CrownForm.Despotic : CrownForm.None;
            Instruments.CpMonth(sim.Diplomacy, 61, 1788);   // 講和の月と年を直に与える（この盤面は国の歩を回さず Diplomacy の年が 0 のまま）
            Instruments.ImpAfter(sim.Diplomacy, pre, fr, uk, fr.Nation!, uk.Nation!, 1.5, true);
            var last = Instruments.ImpRowsPending(sim.Diplomacy)[^1].Split('\t');
            Assert.Equal(((int)crPre).ToString(), Col(last, "loserCrownBeforeCode"));
            Assert.Equal(((int)uk.Nation!.Crown).ToString(), Col(last, "loserCrownAfterCode"));
            Assert.NotEqual(Col(last, "loserCrownBeforeCode"), Col(last, "loserCrownAfterCode"));
            Assert.Equal(((int)polPre).ToString(), Col(last, "loserPolityBeforeCode"));
            Assert.Equal("1788", Col(last, "year")); Assert.Equal("20", Col(last, "quarter")); Assert.Equal("61", Col(last, "month"));
            // 勝者の政体と押し付けの政体が違う盤面：敗者の政体を勝者と別にして呼ぶ＝winnerPolityCode は勝者・imposedPolityCode は敗者の今の政体で読む
            var diffPol = fr.Nation!.Polity == Polity.Despotic ? Polity.Republic : Polity.Despotic;
            var preD = Instruments.ImpBefore(uk.Nation!);
            uk.Nation!.Polity = diffPol;
            Instruments.ImpAfter(sim.Diplomacy, preD, fr, uk, fr.Nation!, uk.Nation!, 1.5, true);
            var dRow = Instruments.ImpRowsPending(sim.Diplomacy)[^1].Split('\t');
            Assert.NotEqual(Col(dRow, "winnerPolityCode"), Col(dRow, "imposedPolityCode"));
            Assert.Equal(((int)fr.Nation!.Polity).ToString(), Col(dRow, "winnerPolityCode"));
            Assert.Equal(((int)diffPol).ToString(), Col(dRow, "imposedPolityCode"));
            Assert.Equal(((int)preD.Polity).ToString(), Col(dRow, "loserPolityBeforeCode"));
            uk.Nation!.Polity = preD.Polity;
            // 同じ月に同じ敗者へ 2 本目（計器の呼び口を直に叩く）＝順番は 2・別の敗者は 1 から
            Instruments.ImpAfter(sim.Diplomacy, Instruments.ImpBefore(uk.Nation!), fr, uk, fr.Nation!, uk.Nation!, 1.5, true);
            Instruments.ImpAfter(sim.Diplomacy, Instruments.ImpBefore(fr.Nation!), uk, fr, uk.Nation!, fr.Nation!, 1.5, true);
            var all = Instruments.ImpRowsPending(sim.Diplomacy).Select(r => r.Split('\t')).ToList();
            Assert.Equal(new[] { "1", "2", "3", "4", "1" }, all.Select(r => Col(r, "orderInMonthIndex")).ToArray());
            return 0;
        });
    }

    [Fact]
    public void 開幕と政体変更と蜂起の決着と廃位と幕府の終わりと復元で書き手の符号が付く()
    {
        var offWorld = new Simulation(NationConfig.France().ToScenario(5), new NullAgent()).World;   // 計器が切れている間に作った World
        WithInstruments(() =>
        {
            var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "日本" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            var fr = sim.Nations[0].World; var jp = sim.Nations[1].World; var c = sim.Coef;
            Assert.Equal(0, Instruments.CrownSourceOf(fr));                                   // 開幕（構築）
            Politics.ExecuteRegimeChange(fr, c, Polity.Republic, violent: false);
            Assert.Equal(1, Instruments.CrownSourceOf(fr));                                   // Transition 系
            Politics.SettleRevolt(fr, c, true, Polity.ConstitutionalMonarchy, null, System.Array.Empty<Pop>());
            Assert.Equal(3, Instruments.CrownSourceOf(fr));                                   // 蜂起の決着
            Assert.Equal(CrownForm.Shogunate, jp.Crown);                                      // 前提：日本は幕府
            c.ShogunateEndMilitancy = -1;                                                     // 内圧の門を常に開ける
            CrownParliament.Step(jp, c);
            Assert.Equal(CrownForm.Absolutist, jp.Crown);
            Assert.Equal(5, Instruments.CrownSourceOf(jp));                                   // 幕府の終わり
            fr.Crown = CrownForm.Absolutist; fr.ParliamentControl = 1.0; c.PolityInertia = -1;
            CrownParliament.Step(fr, c);
            Assert.Equal(CrownForm.None, fr.Crown);
            Assert.Equal(4, Instruments.CrownSourceOf(fr));                                   // 廃位
            // 符号 6 の口：助走の後の復元は書き手を不明（6）へ戻す（口を外すと直前の符号 3 が残る）
            Politics.SettleRevolt(fr, c, true, fr.Polity == Polity.Republic ? Polity.Despotic : Polity.Republic, null, System.Array.Empty<Pop>());
            Assert.Equal(3, Instruments.CrownSourceOf(fr));
            GovernanceSnapshot.Capture(fr).RestoreTo(fr);
            Assert.Equal(6, Instruments.CrownSourceOf(fr));
            Assert.Equal(Instruments.CrownSrcUnknown, Instruments.CrownSourceOf(offWorld));   // 計器が切れていた間に作った World は不明
            return 0;
        });
    }

    [Fact]
    public void crownpcの末尾の列が王冠の書き手の符号を書く()
    {
        WithInstruments(() =>
        {
            var w = new Simulation(NationConfig.France().ToScenario(5), new NullAgent()).World;
            Instruments.CrownSource(w, 2);
            var cols = Instruments.CrownHeaderText.Split('\t');
            Assert.Equal("crownRegisterSourceCode", cols[^1]);
            var line = Instruments.CrownLine(w, 0, 1, Instruments.CrownCompute(w, null)).Split('\t');
            Assert.Equal(cols.Length, line.Length);
            Assert.Equal("2", line[^1]);
            return 0;
        });
    }
}
