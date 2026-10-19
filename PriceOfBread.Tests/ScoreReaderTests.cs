using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (449) 線 sp の番人（事前登録 measurements/sp_20261020_prereg.txt [6]）＝国力の物差しの点火の前半（形・既定は旗 0＝恒等）。
/// ①国力の読み手が 1 本（旧の 4 項を NationalScore の外から呼ぶのは名指しの許可だけ）／②内訳の 1 本（旗 0＝旧の文字列そのまま・旗 1＝6 項の点の和が国力）／
/// ③開幕のランクの取り直し（旗 1 のときだけ・助走の直後に 6 項とも総量が正）。
/// 期待値は手計算のリテラルか独立の式で書く（実装の関数の出力から作らない＝同語反復にしない）。
/// </summary>
public class ScoreReaderTests
{
    private static MultiNationSim Four()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    // ================= ① 読み手は 1 本 =================

    // ★(451) 合流：出力の場所（AppContext.BaseDirectory）から上へ探す形は隔離ビルド（--artifacts-path がリポジトリの外）で null になって赤だった＝既存の 1 本（ソースの場所から引く）を呼ぶ
    private static string RepoRoot() => GoodIdGuardTests.RepoRoot();

    /// <summary>コメント〔行頭の // と行末の //〕を落とした行（文字列の中に // は無い）。</summary>
    private static string CodeOf(string line)
    {
        int i = line.IndexOf("//", StringComparison.Ordinal);
        return i < 0 ? line : line[..i];
    }

    [Fact]
    public void 旧の国力の4項を_NationalScoreの外から呼ぶのは名指しの許可だけである()
    {
        // 許可（ファイル名→呼んでよい旧の項）。理由は事前登録 [6] S1：
        //   InstrumentsScore.cs＝計器の scoreOld の列（旧の和を新しい物差しと並べる読むだけの列）／GameViewModel.Header.cs＝金融の履歴の系列（国力の読み手でなく与信の深さの標本・セーブの並びのため系列は残る）
        var allowed = new Dictionary<string, string[]>
        {
            ["InstrumentsScore.cs"] = ["OfOld"],
            ["GameViewModel.Header.cs"] = ["Finance"],
        };
        var rx = new Regex(@"NationalScore\.(Military|Wealth|Finance|Tech|WealthParts|OfOld)\b");
        var root = RepoRoot();
        var bad = new List<string>();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.App", "PriceOfBread.Runner" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) || f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
                string name = Path.GetFileName(f);
                int ln = 0;
                foreach (var line in File.ReadLines(f))
                {
                    ln++;
                    foreach (Match m in rx.Matches(CodeOf(line)))
                        if (!(allowed.TryGetValue(name, out var ok) && ok.Contains(m.Groups[1].Value)))
                            bad.Add($"{name}:{ln} {m.Value}");
                }
            }
        Assert.True(bad.Count == 0, "旧の国力の項を許可なく読んでいる：" + string.Join(" / ", bad));
    }

    // ================= ② 内訳の 1 本 =================

    [Fact]
    public void 旗0の内訳は旧の4項と旧の文字列のままである()
    {
        var sim = Four();
        sim.Run(2);
        var ws = sim.Nations.ConvertAll(n => n.World);
        foreach (var w in ws) w.Coef.ScoreFromWorldShare = 0;
        var player = ws[0];
        // 旧の ScoreBreakdownText の式を、この試験に独立に書き写す（旧の実装そのもの）。
        double mil = NationalScore.Military(player), wea = NationalScore.Wealth(player), fin = NationalScore.Finance(player), tec = NationalScore.Tech(player);
        double mine = mil + wea + fin + tec, top = 0; string topName = "";
        foreach (var q in sim.Diplomacy.Powers)
            if (q.Nation is { } qw) { double s = NationalScore.Of(qw); if (s > top) { top = s; topName = qw.SelfPower?.Name ?? ""; } }
        string gap = top > mine + 1e-9 ? $"首位 {topName} との差 {top - mine:F1}" : "首位";
        string expected = $"軍事 {mil:F1}\n生産の豊かさ {wea:F1}\n金融 {fin:F1}\n技術 {tec:F1}\n合計 {mine:F1}　{gap}";
        Assert.Equal(expected, PlayText.ScoreBreakdownText(player, sim.Diplomacy));
        var bd = NationalScore.BreakdownOf(player);
        Assert.Equal(["軍事", "生産の豊かさ", "金融", "技術"], bd.Select(l => l.Label));
        Assert.Equal(mil, bd[0].Points); Assert.Equal(wea, bd[1].Points); Assert.Equal(fin, bd[2].Points); Assert.Equal(tec, bd[3].Points);
        // 国の一覧の値は国ごとの Of と同じ（旗 0＝Scores は Of の並び）
        var byPower = NationalScore.ScoresByPower(sim.Diplomacy);
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is { } pw) Assert.Equal(NationalScore.Of(pw), byPower[p.Id]);
    }

    [Fact]
    public void 旗1の内訳は6項の点で_その和が国力で_首位との差も同じ物差しである()
    {
        var sim = Four();
        sim.Run(2);
        var ws = sim.Nations.ConvertAll(n => n.World);
        foreach (var w in ws) w.Coef.ScoreFromWorldShare = 1;
        // 独立の式：項ごとの総量 tot_t・使う項の数 used から 100×素の量÷総量÷used
        var terms = ws.Select(NationalScore.TermsOf).ToArray();
        var tot = new double[6];
        foreach (var t in terms) for (int k = 0; k < 6; k++) tot[k] += t[k];
        int used = tot.Count(x => x > 0);
        Assert.Equal(6, used);   // 2 四半期走らせた世界では 6 項とも総量が正（退化していない前提）
        var all = NationalScore.Breakdowns(ws);
        var sc = NationalScore.Scores(ws);
        for (int i = 0; i < ws.Count; i++)
        {
            Assert.Equal(["兵の数", "軍事費", "鉄の産出", "動力の消費", "都市の人口", "総人口"], all[i].Select(l => l.Label));
            for (int k = 0; k < 6; k++) Assert.Equal(100.0 * terms[i][k] / tot[k] / used, all[i][k].Points, 9);
            Assert.Equal(sc[i], all[i].Sum(l => l.Points), 9);
            Assert.Equal(sc[i], NationalScore.BreakdownOf(ws[i]).Sum(l => l.Points), 9);   // 国ごとに引く道も同じ
        }
        // ホバーの文字列：6 項の行と、合計＝国力・首位との差＝同じ Scores の最大との差
        var text = PlayText.ScoreBreakdownText(ws[0], sim.Diplomacy);
        foreach (var lab in new[] { "兵の数", "軍事費", "鉄の産出", "動力の消費", "都市の人口", "総人口" }) Assert.Contains(lab + " ", text);
        Assert.DoesNotContain("生産の豊かさ", text);
        Assert.Contains($"合計 {sc[0]:F1}", text);
        double topV = sc.Max();
        if (topV > sc[0] + 1e-9) Assert.Contains($"との差 {topV - sc[0]:F1}", text); else Assert.EndsWith("首位", text);
    }

    // ================= ③ 開幕のランクの取り直し =================

    [Fact]
    public void 旗0は助走でランクを取り直さない()
    {
        // 61 カ国の世界（定員制の分岐）。助走の前後で旧の国力は大きく動く（市場が回る）ので、取り直しが通る突然変異〔門を常に真にする〕なら旗 0 でもランクが動く。
        var off = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        off.Coef.ScoreFromWorldShare = 0;   // 旧の腕〔旗 0〕を明示（既定は 1＝2026-10-20 に点火）
        foreach (var n in off.Nations) n.World.Coef.ScoreFromWorldShare = 0;
        var before = off.Diplomacy.Powers.Select(p => off.Diplomacy.RankOf(p)).ToArray();
        off.StartWarmup();
        Assert.Equal(before, off.Diplomacy.Powers.Select(p => off.Diplomacy.RankOf(p)).ToArray());   // 取り直しの行を通らない＝旧と同じ（恒等の底は 5 種の成績表の全行一致）
        // 取り直しの行が通れば動く盤面であることを確かめる（空振りの番人にしない）：助走の後の旧の国力で定員制のランクを独立に引き直し、構築時と違う国が 1 つ以上ある
        var c = off.Coef;
        var order = off.Diplomacy.Powers.Where(p => p.Nation != null).OrderByDescending(p => NationalScore.Of(p.Nation!)).ThenBy(p => p.Id).ToList();
        int moved = 0;
        for (int i = 0; i < order.Count; i++)
        {
            var want = i < c.GreatPowerCount ? PowerRank.Great
                     : i < c.GreatPowerCount + c.SecondPowerCount ? PowerRank.Secondary
                     : i < c.GreatPowerCount + c.SecondPowerCount + c.MiddlePowerCount ? PowerRank.Middle : PowerRank.Minor;
            if (off.Diplomacy.RankOf(order[i]) != want) moved++;
        }
        Assert.True(moved > 0, "旗 0 でも助走の後に引き直すとランクが動く盤面であること（動かないなら、この番人は門の突然変異を見分けられない）");
    }

    [Fact]
    public void 旗1の条約港の風化は列強の中央値を基準にする()
    {
        // 事前登録 sp_20261020 [1] D4：旗＞0 の側は比の基準＝列強の強さの中央値（上側の中央・添字 n÷2）。旗 0 は最大のまま。
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        sim.Coef.ScoreFromWorldShare = 1;
        foreach (var n in sim.Nations) n.World.Coef.ScoreFromWorldShare = 1;   // 旗の読み元は 2 系統（外交盤の係数と各国の World.Coef）＝POB_COEF は両方に同じ値を置く前提。片方だけ立てると読み手の物差しが食い違う
        sim.StartWarmup();   // 助走の直後（流れの項が出そろった盤面・開幕のランクの取り直しもここ）
        var d = sim.Diplomacy;
        var sc = NationalScore.ScoresByPower(d);   // 旗 1＝世界の総量に対する割合の国力
        var greatS = d.Powers.Where(p => p.Nation != null && d.RankOf(p) == PowerRank.Great).Select(p => sc[p.Id]).OrderBy(x => x).ToList();
        double median = greatS[greatS.Count / 2], max = greatS[^1];
        Assert.True(max > median * 1.2, "列強の最大と中央値が離れている世界でなければ、基準の違いを見分けられない");
        // 列強でない国のうち、比（÷中央値）が 0.45 未満で最大のものを 1 国選ぶ（比÷0.5 が 1 に張り付かない）
        var pick = d.Powers.Where(p => p.Nation != null && d.RankOf(p) != PowerRank.Great && sc[p.Id] / median < 0.45).OrderByDescending(p => sc[p.Id]).First();
        var region = pick.Nation!.Regions[0];
        region.ForeignRulers = ["測定用"]; region.ForeignShares = [0.8];
        double ratio = sc[pick.Id] / median;
        double expected = 0.8 * (1.0 - Math.Min(1.0, ratio / sim.Coef.TreatyPortLapseRatio.Value) * sim.Coef.DominanceSpeed);
        double withMax = 0.8 * (1.0 - Math.Min(1.0, sc[pick.Id] / max / sim.Coef.TreatyPortLapseRatio.Value) * sim.Coef.DominanceSpeed);
        Assert.True(Math.Abs(expected - withMax) > 1e-6, "最大を基準にした値と中央値を基準にした値が違う盤面");
        d.TreatyPortLapse(sim.Coef, 3);
        Assert.Equal(expected, region.ForeignShares[0], 12);
    }

    [Fact]
    public void 旗1は助走の直後にランクを取り直し_その時点で6項とも総量が正である()
    {
        // 61 カ国の全体で読む（定員制の分岐＝real.Count＞12）。旗は MultiNationSim の係数と各国の係数の両方（POB_COEF は全てに同じ値を置く）。
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        sim.Coef.ScoreFromWorldShare = 1;
        foreach (var n in sim.Nations) n.World.Coef.ScoreFromWorldShare = 1;
        var ws = sim.Nations.ConvertAll(n => n.World);
        int UsedTerms()
        {
            var tot = new double[6];
            foreach (var t in ws.Select(NationalScore.TermsOf)) for (int k = 0; k < 6; k++) tot[k] += t[k];
            return tot.Count(x => x > 0);
        }
        Assert.True(UsedTerms() < 6, "構築時に 6 項そろっている＝取り直しの根拠が崩れた（市場が回る前は流れの項が 0 のはず）");
        sim.Diplomacy.ComputeRanks(sim.Coef);   // 構築時のランクを旗 1 で置く（POB_COEF で旗を立てた世界のコンストラクタが置くもの）
        var atBuild = sim.Diplomacy.Powers.Where(p => p.Nation != null).ToDictionary(p => p.Id, p => sim.Diplomacy.RankOf(p));
        sim.StartWarmup();
        Assert.Equal(6, UsedTerms());
        // 独立の再述：国力の降順（同点は Id）で上位 GreatPowerCount＝列強・次の SecondPowerCount＝準列強・次の MiddlePowerCount＝中堅・残り＝弱小
        var c = sim.Coef;
        var sc = NationalScore.ScoresByPower(sim.Diplomacy);
        var order = sim.Diplomacy.Powers.Where(p => p.Nation != null).OrderByDescending(p => sc[p.Id]).ThenBy(p => p.Id).ToList();
        int diffFromBuild = 0;
        for (int i = 0; i < order.Count; i++)
        {
            var want = i < c.GreatPowerCount ? PowerRank.Great
                     : i < c.GreatPowerCount + c.SecondPowerCount ? PowerRank.Secondary
                     : i < c.GreatPowerCount + c.SecondPowerCount + c.MiddlePowerCount ? PowerRank.Middle : PowerRank.Minor;
            Assert.Equal(want, sim.Diplomacy.RankOf(order[i]));
            if (atBuild[order[i].Id] != want) diffFromBuild++;
        }
        Assert.True(diffFromBuild > 0, "助走の前後でランクが 1 つも動かない＝この世界では取り直しの効きを見分けられない");
    }
}
