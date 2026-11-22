using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>法の画面の賛否の欄とポップアップの番人（SimCore/Display/LawStakes・LawOpenings）。機構と不変条件だけを見る（数値の期待は式から作らない）。</summary>
public class LawStakesTests
{
    private static World[] Boards()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.Russia()], seed: 1789);
        var cons = sim.Nations[1].World;
        cons.Polity = Polity.ConstitutionalMonarchy; cons.ParliamentEnabled = true;
        cons.PartySeats[0] = 0.4; cons.PartySeats[1] = 0.35; cons.PartySeats[2] = 0.25;   // 議会の項（党の行）を通す盤面
        return [sim.Nations[0].World, cons, sim.Nations[2].World];
    }

    private static IEnumerable<LawDef> Laws() => LawCatalog.All.Where(d => d.Slot != LawSlot.NoRow);

    [Fact]
    public void 行の押す力の和は議会の門の純支持と一致する()
    {
        foreach (var w in Boards())
            foreach (var d in Laws())
                foreach (bool inc in new[] { true, false })
                {
                    double want = PolicyPreferences.NetSupport(w, d.Axis, inc);
                    double got = LawStakes.NetPush(w, d, inc);
                    Assert.True(Math.Abs(want - got) <= 1e-9 * Math.Max(1, Math.Abs(want)), $"{d.Key} inc={inc} 門={want} 行の和={got}");
                }
    }

    [Fact]
    public void 党と軍部の行は議会のある盤面と軍の軸にだけ出る()
    {
        var boards = Boards();
        var all = boards.SelectMany(w => Laws().SelectMany(d => LawStakes.Lines(w, d).Select(l => (w, l)))).ToList();
        Assert.Contains(all, t => t.l.Kind == StakeKind.Party);
        Assert.Contains(all, t => t.l.Kind == StakeKind.Class);
        Assert.DoesNotContain(Laws().SelectMany(d => LawStakes.Lines(boards[0], d)), l => l.Kind == StakeKind.Party && !boards[0].ParliamentEnabled);
    }

    [Fact]
    public void 階級の重みの和は三つのブロックの力の和_影響力の列の和は1()
    {
        foreach (var w in Boards())
        {
            var (t, m, s) = InterestGroups.Blocs(w);
            double sum = LawStakes.Voices(w).Sum(v => v.Weight);
            Assert.True(Math.Abs(sum - (t + m + s)) <= 1e-12 * Math.Max(1, t + m + s));
            var lines = LawStakes.Lines(w, LawCatalog.ByField("Suffrage")!);
            double voice = lines.Where(l => l.Kind == StakeKind.Class).Sum(l => l.Voice);
            Assert.InRange(voice, 1 - 1e-9, 1 + 1e-9);
        }
    }

    [Fact]
    public void 階級の押す力の符号は所属ブロックの好みの符号_同じブロックの階級は同じ符号()
    {
        foreach (var w in Boards())
        {
            var e = PolicyPreferences.Build(w);
            foreach (var d in Laws())
            {
                var lines = LawStakes.Lines(w, d).Where(l => l.Kind == StakeKind.Class);
                foreach (var l in lines)
                {
                    int bloc = int.Parse(l.Key.Split('.')[2]);
                    if (l.Next is { } n && Math.Abs(n) > 0)
                        Assert.Equal(Math.Sign(PolicyPreferences.Pref(d.Axis, bloc, e, w, true)), Math.Sign(n));
                    if (l.Prev is { } p && Math.Abs(p) > 0)
                        Assert.Equal(Math.Sign(-PolicyPreferences.Pref(d.Axis, bloc, e, w, false)), Math.Sign(p));
                }
            }
        }
    }

    [Fact]
    public void 半円は賛成と反対の和が1_賛成は列の正の値の和_全員が無関心なら半円なし()
    {
        foreach (var w in Boards())
            foreach (var d in Laws())
            {
                var lines = LawStakes.Lines(w, d);
                foreach (bool inc in new[] { true, false })
                {
                    var sp = LawStakes.Split(w, d, inc);
                    var col = lines.Select(l => inc ? l.Next : l.Prev).ToList();
                    if (sp is null) { Assert.All(col, v => Assert.Null(v)); continue; }
                    Assert.InRange(sp.For + sp.Against, 1 - 1e-9, 1 + 1e-9);
                    Assert.InRange(col.Sum(v => Math.Max(v ?? 0, 0)), sp.For - 1e-9, sp.For + 1e-9);
                    Assert.False(string.IsNullOrEmpty(sp.Caption));
                }
            }
    }

    [Fact]
    public void 段の端では段の無い向きの列が全て空_半円もなし()
    {
        var w = Boards()[0];
        var d = LawCatalog.ByField("Suffrage")!;
        w.Suffrage = Suffrage.Universal;
        Assert.All(LawStakes.Lines(w, d), l => Assert.Null(l.Next));
        Assert.Null(LawStakes.Split(w, d, true));
        w.Suffrage = Suffrage.None;
        Assert.All(LawStakes.Lines(w, d), l => Assert.Null(l.Prev));
        Assert.Null(LawStakes.Split(w, d, false));
    }

    [Fact]
    public void 賛成寄りと反対寄りの分け方は賛成の割合と一致し_キャプションは法の段の名_前提法の語は次の段の前提と一致する()
    {
        foreach (var w in Boards())
            foreach (var d in Laws())
            {
                foreach (bool inc in new[] { true, false })
                {
                    var sides = LawStakes.Sides(w, d, inc);
                    if (LawStakes.Split(w, d, inc) is not null) Assert.NotNull(sides);   // 全員が無関心の向きだけ半円は無い
                    if (sides is null) continue;
                    Assert.All(sides.Pro, c => Assert.InRange(c.ForShare, 0.5, 1.0));
                    Assert.All(sides.Con, c => Assert.InRange(c.ForShare, 0.0, 0.5));
                    Assert.All(sides.Con, c => Assert.NotEqual(0.5, c.ForShare));
                    Assert.Equal(sides.Pro.Count + sides.Con.Count, sides.Pro.Concat(sides.Con).Select(c => c.ClassId).Distinct().Count());
                    int cur = d.Current(w);
                    Assert.Equal($"{d.StageName(cur)}→{d.StageName(cur + (inc ? 1 : -1))}", sides.Caption);
                }
                bool blocked = d.Current(w) < d.LevelBase + d.Stages.Length - 1 && !Government.LawPrereq(w, d.Axis, d.Current(w) + 1, w.Coef).Ok;
                Assert.Equal(blocked, StageGate.PrereqNote(w, d) is not null);
            }
    }

    // ---- ポップアップの差分 ----

    private static HashSet<(string, int)> NewSeen() => [];

    [Fact]
    public void 官僚制度を資格任用へ書き換えると独占禁止が新たに開く()
    {
        var w = Boards()[0];
        w.Bureaucracy = BureaucracyLaw.Spoils;
        var prev = LawOpenings.Snapshot(w);
        w.Bureaucracy = BureaucracyLaw.MeritExam;
        var now = LawOpenings.Snapshot(w);
        var opened = LawOpenings.Opened(prev, now, NewSeen()).Select(d => d.Key).ToList();
        Assert.Contains(nameof(NationPolicy.Antitrust), opened);
    }

    [Fact]
    public void 自分で段を進めて開いた次の段は新しくない()
    {
        var w = Boards()[0];
        var prev = new[] { new LawGateState("L", 0, false) };
        var now = new[] { new LawGateState("L", 1, true) };
        Assert.Empty(LawOpenings.Opened(prev, now, NewSeen()));
        // 現在の段が同じなら数える（鍵が目録にある法で確かめる）
        string key = LawCatalog.All.First(d => d.Slot != LawSlot.NoRow).Key;
        var open = LawOpenings.Opened([new LawGateState(key, 0, false)], [new LawGateState(key, 0, true)], NewSeen());
        Assert.Single(open);
    }

    [Fact]
    public void 一度開いた段は閉じて開き直しても二度は入らない_最初に開いていた段も入らない()
    {
        string key = LawCatalog.All.First(d => d.Slot != LawSlot.NoRow).Key;
        var seen = NewSeen();
        LawGateState S(bool open) => new(key, 0, open);
        Assert.Single(LawOpenings.Opened([S(false)], [S(true)], seen));
        Assert.Empty(LawOpenings.Opened([S(true)], [S(false)], seen));
        Assert.Empty(LawOpenings.Opened([S(false)], [S(true)], seen));
        // 開幕から開いている段は基準として seen に入る
        var seen2 = NewSeen();
        Assert.Empty(LawOpenings.Opened([], [S(true)], seen2));
        Assert.Empty(LawOpenings.Opened([S(false)], [S(true)], seen2));
    }

    [Fact]
    public void 政治の門だけが変わっても写しは変わらない()
    {
        var w = Boards()[1];
        var seats = (double[])w.PartySeats.Clone();
        w.PartySeats[0] = 1; w.PartySeats[1] = 0; w.PartySeats[2] = 0;
        var a = LawOpenings.Snapshot(w);
        w.PartySeats[0] = 0; w.PartySeats[1] = 0; w.PartySeats[2] = 1;
        var b = LawOpenings.Snapshot(w);
        Assert.Equal(a, b);
        Assert.Empty(LawOpenings.Opened(a, b, NewSeen()));
        for (int i = 0; i < 3; i++) w.PartySeats[i] = seats[i];
    }
}
