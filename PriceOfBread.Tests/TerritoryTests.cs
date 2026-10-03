using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class TerritoryTests
{
    private static Scenario Frontier(ulong s) => new() { Seed = s, Kind = ScenarioKind.FrontierDevelopment };

    [Fact]
    public void 辺境はロックで始まり開発で解錠され移民が流入する()   // §A-8／§A-9
    {
        var sim = new Simulation(Frontier(1), new NullAgent());
        var frontier = sim.World.Regions[1];
        Assert.Equal(RegionState.Locked, frontier.State);
        double startPop = frontier.SumPopulation();

        sim.Run(400);

        Assert.Equal(RegionState.Active, frontier.State);                       // 解錠
        Assert.True(frontier.SumPopulation() > startPop + 100, $"辺境に移民が入らない（{frontier.SumPopulation():F0}）");
        Assert.True(sim.World.Regions[0].SumPopulation() < 1000, "本土から流出していない");
    }

    [Fact]
    public void 地域間移民でも人口と基礎貨幣は保存される()
    {
        var sim = new Simulation(Frontier(2), new NullAgent());
        double p0 = sim.World.TotalPopulation, m0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
        Assert.True(Math.Abs(sim.World.BaseMoney - m0) < 1e-3 * Math.Max(1, m0), "基礎貨幣保存の破れ");
    }

    [Fact]
    public void 同一シードで辺境開発は決定論的()
    {
        var a = new Simulation(Frontier(3), new NullAgent());
        var b = new Simulation(Frontier(3), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Regions[1].SumPopulation(), b.World.Regions[1].SumPopulation());
        Assert.Equal(a.World.Regions[0].SumPopulation(), b.World.Regions[0].SumPopulation());
    }

    /// <summary>★(125) 州の空洞化は不具合（作者 2026-09-30「州から人が抜けきるというのは現実的ではないです。これは不具合とみなしてください。」）の番人＝不変条件：
    /// 賃金の低い州（本土・賃金のプレミアム 0.25）と高い州（辺境・4＝どちらも上下の端）を並べ、機会の差をずっと保ったまま（プレミアムは四半期の更新を回さない＝動かない・
    /// 辺境は耕地が無尽蔵＝入る側は土地が混まない）地域間の移民だけを 167 年（2004 か月）回しても、出る州の人口はピークの1/10 未満へ落ちない
    /// ＝流出は残る人の土地の持ち分（<see cref="Territory.Holding"/>）で止まる（収穫逓減の出る側）。
    /// ⚠突然変異「持ち分を掛けない（Holding≡1）」で赤（本土が空になる）・「入る側の機会にも持ち分を掛ける」で赤（辺境の耕地が全国を吸う）・
    /// 「工業の引力を戻す（機会×(1＋0.5×工業化度)）」で赤（工業の総量が機会の差を広げ、持ち分では止まらない）。</summary>
    [Fact]
    public void 移民だけでは州は空にならない_流出は残る人の土地の持ち分で止まる()
    {
        var sim = new Simulation(Frontier(1), new NullAgent());
        var w = sim.World; var c = w.Coef;
        Region core = w.Regions[0], fr = w.Regions[1];
        fr.State = RegionState.Active;
        fr.ProductivityBonus = core.ProductivityBonus;   // 差は賃金だけ
        core.WagePremium = 0.25; fr.WagePremium = 4.0;
        fr.Industrialization = 10.0;   // 賃金の高い州には工業も積もっている（工業化度の総量は機会に入らない＝二重計上しない）
        core.ArableLand = core.SumPopulation() * 1.8;   // 入植の時の持ち分（本土州と同じ規則＝初期農民×余裕 1.8）
        fr.ArableLand = 1e7;   // 入る側は混まない＝止めるのは出る側だけ（1e8 以上は「未設定」の印）
        double peak = core.SumPopulation(), low = peak, lastYear = peak;
        for (int m = 0; m < 2004; m++)
        {
            if (m == 2004 - 12) lastYear = core.SumPopulation();
            Territory.Step(w, c);
            double p = core.SumPopulation();
            if (p > peak) peak = p;
            if (p < low) low = p;
        }
        Assert.True(Territory.Opportunity(fr, c) - Territory.Opportunity(core, c) - c.TransportFriction > 0, "前提が崩れた：機会の差が閉じた（入る側が混んだ）");
        Assert.True(low < 0.9 * peak, $"前提が崩れた：移民が流れていない（{low:F1}／{peak:F1}）");
        Assert.True(low >= peak / 10, $"本土が移民だけでピークの1/10 未満へ落ちた（{low:F1}／{peak:F1}）");
        Assert.True(Math.Abs(core.SumPopulation() - lastYear) <= 1e-3 * peak, $"流出が止まっていない（最後の1年に {lastYear - core.SumPopulation():F3} 減った）");
    }

    /// <summary>★(125) 受け入れの上限（越境移民と同じ規則）：人口の小さな州が最大機会になっても、1か月の流入は
    /// ImmigrationReceiveCapFrac÷3×その州の人口×<see cref="Territory.AbsorbCap"/> を超えない（旧＝全ての州が同じ月に流し込み、
    /// 人口数万の州が1四半期で10倍以上に膨れ、膨れた分が土地の逓減で何十年もかけて流れ出た）。縮んだ分は出る州に残る（人口は保存）。
    /// ⚠突然変異「縮めない（比≡1）」で赤。</summary>
    [Fact]
    public void 小さな州への流入は受け入れの上限を超えない_超えた分は出る州に残る()
    {
        var sim = new Simulation(Frontier(1), new NullAgent());
        var w = sim.World; var c = w.Coef;
        Region core = w.Regions[0], fr = w.Regions[1];
        fr.State = RegionState.Active;   // 辺境＝人口10（千人）・生産性2.5＝最大機会
        double frPop = fr.SumPopulation(), total = core.SumPopulation() + frPop;
        double cap = c.ImmigrationReceiveCapFrac / Calendar.MonthsPerQuarter * frPop * Territory.AbsorbCap(fr, c);
        Assert.True(cap > 0, "前提が崩れた：受け入れの能力が0");
        Territory.Step(w, c);
        double inflow = fr.SumPopulation() - frPop;
        Assert.True(inflow > 0.5 * cap, $"前提が崩れた：流れていない（{inflow}）");
        Assert.True(inflow <= cap * (1 + 1e-9), $"流入 {inflow} が上限 {cap} を超えた");
        Assert.True(Math.Abs(core.SumPopulation() + fr.SumPopulation() - total) <= 1e-9 * total, "人口が保存されない");
    }

    /// <summary>★(141) 国内の移民は行き先を機会の差の比で全ての州へ配り、距離で減らす（越境移民と同じ規則）の番人。
    /// 仏の1か国の盤面で、出る州 S（リール）の残る値打ちを他の州の機会 O の2割にし、他の州の機会を同じ O に揃える（機会は生産性に比例＝生産性で合わせる）。
    /// 他の州どうしの差＝O−O×持ち分−摩擦＜0＝出ない。受け入れの上限は外す（上限率を巨大に）。1か月の移民＝S から全ての州へ・差は同じ
    /// ＝行き先の階級が同じ州どうしの流れの比は距離の減衰の比だけ（パリ〔約200km〕とマルセイユ〔約830km〕）。人は保存。
    /// ⚠突然変異「上位2州だけへ」で赤（受け入れる州が2つ）・「距離の減衰を掛けない」で赤（比が1）。</summary>
    [Fact]
    public void 国内の移民は行き先を差の比で全ての州へ配り距離で減らす()
    {
        var sim = Board("フランス");
        var w = sim.Nations[0].World; var c = w.Coef;
        c.ImmigrationReceiveCapFrac = 1e9;   // 受け入れの上限を外す（流れの比だけを見る）
        const double O = 2.0;
        Region S = w.Regions.First(r => r.Name == "リール"), a = w.Regions.First(r => r.Name == "パリ"), b = w.Regions.First(r => r.Name == "マルセイユ");
        foreach (var r in w.Regions)
        {
            Assert.Equal(RegionState.Active, r.State);
            double o = Territory.Opportunity(r, c);
            Assert.True(o > 0, $"前提が崩れた：{r.Name} の機会が0");
            r.ProductivityBonus *= (ReferenceEquals(r, S) ? 0.2 * O / Territory.Holding(r, c) : O) / o;   // S の残る値打ち＝0.2×O・他の州の機会＝O
        }
        Assert.True(Math.Abs(Territory.Opportunity(a, c) - O) < 1e-9 && Math.Abs(Territory.Opportunity(S, c) * Territory.Holding(S, c) - 0.2 * O) < 1e-9, "前提が崩れた：機会が揃わない");
        int Classes(Region r) { int m = 0; foreach (var p in r.Pops) if (!p.IsSoldier) m |= 1 << p.ClassId; return m; }
        Assert.Equal(Classes(a), Classes(b));   // 前提：行き先の階級が同じ（動ける人数が同じ）
        var pop0 = w.Regions.ToDictionary(r => r, r => r.SumPopulation());
        double total0 = pop0.Values.Sum();

        Territory.Step(w, c);

        int got = 0;
        foreach (var r in w.Regions)
        {
            if (ReferenceEquals(r, S)) continue;
            double d = r.SumPopulation() - pop0[r];
            Assert.True(d >= -1e-9, $"{r.Name} から人が出た（{d}）＝他の州どうしは差が負のはず");
            if (d > 1e-9) got++;
        }
        Assert.True(got >= 3, $"受け入れた州が {got}（上位の州だけへ流れている）");
        Assert.Equal(w.Regions.Count - 1, got);   // 差が正の全ての州へ
        double da = a.SumPopulation() - pop0[a], db = b.SumPopulation() - pop0[b];
        var ca = Places.Coord(S.Name); var pa = Places.Coord(a.Name); var pb = Places.Coord(b.Name);
        double expect = Territory.MigrationDecay(Geo.DistanceKm(ca.Lat, ca.Lon, pa.Lat, pa.Lon), w.WorldTechLevel, c)
                      / Territory.MigrationDecay(Geo.DistanceKm(ca.Lat, ca.Lon, pb.Lat, pb.Lon), w.WorldTechLevel, c);
        Assert.True(expect > 1.03, $"前提が崩れた：距離の減衰の比 {expect} が1に近い");
        Assert.True(Math.Abs(da / db - expect) <= 1e-9 * expect, $"パリ÷マルセイユの流入 {da / db} ≠ 距離の減衰の比 {expect}");
        Assert.True(Math.Abs(w.Regions.Sum(r => r.SumPopulation()) - total0) <= 1e-9 * total0, "人口が保存されない");
    }

    // ===== ★(158) 遠い併合地への移民の吸い出し（作者 2026-09-30「州から人が抜けきるというのは現実的ではないです。これは不具合とみなしてください」）の番人 =====

    /// <summary>トスカーナにペルシャの州（<paramref name="persian"/>・言葉と民族の違う州＝割譲で中核民族の割合0）を割譲した盤面。
    /// 州の機会を人の移動で動かさないため耕地を未設定（土地の逓減と残る人の持ち分が1）にし、受け入れの上限を外す（閾値だけを見る）。</summary>
    private static (World W, Coefficients C) AnnexBoard(params string[] persian)
    {
        var sim = Board("トスカーナ", "ペルシャ");
        var tus = sim.Nations[0].World; var per = sim.Nations[1].World;
        foreach (var name in persian)
            Territory.TransferRegion(per, tus, per.Regions.First(r => r.Name == name), Territory.Rehoming.Ceded);
        var c = tus.Coef;
        c.ImmigrationReceiveCapFrac = 1e9;
        foreach (var r in tus.Regions) { Assert.Equal(RegionState.Active, r.State); r.ArableLand = 1e8; }
        return (tus, c);
    }

    /// <summary>州の機会を o に揃える（生産性で合わせる＝耕地が未設定なら機会は生産性に比例）。</summary>
    private static void SetOpp(Region r, double o, Coefficients c)
    {
        double o0 = Territory.Opportunity(r, c);
        Assert.True(o0 > 0, $"前提が崩れた：{r.Name} の機会が0");
        r.ProductivityBonus *= o / o0;
        Assert.True(Math.Abs(Territory.Opportunity(r, c) - o) <= 1e-12 * o, $"前提が崩れた：{r.Name} の機会が揃わない");
    }

    /// <summary>機会の差が国内の摩擦と越境の閾値の間なら、本国の州から言葉と民族の違う州（割譲で中核民族の割合0）へは1人も流れない
    /// （(150) の Q12 の5種＝リヴォルノ→タブリーズ 3,063km の型・1試合の長さ＝2,004 か月回してリヴォルノがピークの1/10 未満へ落ちない）。
    /// 盤面＝リヴォルノの機会が人が抜けても戻らない（耕地が未設定）・受け入れの上限を外す・タブリーズの機会がリヴォルノの残る値打ちより 0.4（国内の摩擦と越境の閾値の間）高い・
    /// フィレンツェは同じ機会。⚠ 見るのは閾値（民族の親和）だけ＝差が越境の閾値を超えて続く盤面の空洞化は (125) の土地の逓減と受け入れの上限の持ち分（この番人の外・(160) の訂正④で名前を絞った）。
    /// ⚠壊し方＝国内の全ての組に国内の摩擦（旧）→ リヴォルノが167年で1/10 未満＝赤。</summary>
    [Fact]
    public void 差が国内の摩擦と越境の閾値の間なら本国から異民族の州へは流れない()
    {
        var (w, c) = AnnexBoard("タブリーズ");
        Region R(string n) => w.Regions.First(r => r.Name == n);
        Region liv = R("リヴォルノ"), fir = R("フィレンツェ"), tab = R("タブリーズ");
        const double O = 1.0, Gap = 0.4;
        Assert.True(c.TransportFriction < Gap && Gap < c.CrossBorderOppGapThreshold, "前提が崩れた：差が国内の摩擦と越境の閾値の間にない");
        SetOpp(liv, O, c); SetOpp(fir, O, c); SetOpp(tab, O + Gap, c);
        Assert.Equal(1.0, Politics.AvgRegionCoreShare(liv), 12);   // 本国＝中核民族
        Assert.Equal(0.0, Politics.AvgRegionCoreShare(tab), 12);   // 割譲した異民族の州
        var a = Places.Coord(liv.Name); var b = Places.Coord(tab.Name);
        Assert.True(Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon) > 2500, "前提が崩れた：遠い併合地ではない");
        double fixedShare = liv.Pops.Where(p => !p.IsCommoner).Sum(p => p.Size) / liv.SumPopulation();
        Assert.True(fixedShare < 0.08, $"前提が崩れた：動かない人（閉鎖階級と兵士）の割合 {fixedShare} が大きい＝旧でも1/10 未満へ落ちない");
        double tab0 = tab.SumPopulation(), peak = liv.SumPopulation(), low = peak;
        for (int m = 0; m < 2004; m++)   // 1試合の長さ（668四半期）
        {
            Territory.Step(w, c);
            double p = liv.SumPopulation();
            if (p > peak) peak = p;
            if (p < low) low = p;
        }
        Assert.True(low >= peak / 10, $"リヴォルノが移民だけでピーク {peak:F1} の1/10 未満（{low:F1}）へ落ちた");
        Assert.Equal(tab0, tab.SumPopulation(), 9);   // 差が越境の閾値を超えない限り、言葉と民族の違う州へは1人も行かない
    }

    /// <summary>国内の移民の閾値は民族の親和で国内の摩擦と越境の閾値を結ぶ（<see cref="Territory.MigrationThreshold"/>・<see cref="Territory.EthnicAffinity"/> の1本）。
    /// 差が国内の摩擦と越境の閾値の間の盤面で ①本国→異民族の州＝流れない ②異民族の州→本国＝流れない（行き先の中核の割合だけで読むと流れる）③本国どうし＝流れる
    /// ④征服した同じ民族の州どうし（どちらも中核でない・文化のラベルが同じ）＝流れる ⑤征服した違う民族の州どうし＝流れない／
    /// ⑥差が越境の閾値を超えた分だけ流れる（壁ではなく費用）＝割合＝RegionMobility×(差−越境の閾値)×距離の減衰（9桁）／両端の値は厳密。
    /// ⚠壊し方＝国内の摩擦のまま・行き先の割合だけで読む・文化のラベルの項を落とす・両端を入れ替える → 赤。</summary>
    [Fact]
    public void 国内の移民の閾値は民族の親和で国内の摩擦と越境の閾値を結ぶ()
    {
        var (w, c) = AnnexBoard("タブリーズ", "テヘラン", "イスファハン");
        Region R(string n) => w.Regions.First(r => r.Name == n);
        Region liv = R("リヴォルノ"), fir = R("フィレンツェ"), tab = R("タブリーズ"), teh = R("テヘラン"), isf = R("イスファハン");
        Assert.Equal(teh.Culture, isf.Culture);
        Assert.NotEqual(tab.Culture, teh.Culture);
        Assert.Equal(c.CrossBorderOppGapThreshold, Territory.MigrationThreshold(0.0, c));
        Assert.Equal(c.TransportFriction, Territory.MigrationThreshold(1.0, c));
        const double O = 1.0, Mid = 0.3;   // 国内の摩擦と越境の閾値の間
        Assert.True(c.TransportFriction < Mid && Mid < c.CrossBorderOppGapThreshold, "前提が崩れた");
        double Moved(Region dst, double dstOpp, Region src)   // 全ての州の機会を O・行き先だけ dstOpp にして1か月＝src から出た人数
        {
            foreach (var r in w.Regions) SetOpp(r, ReferenceEquals(r, dst) ? dstOpp : O, c);
            double p0 = src.SumPopulation();
            Territory.Step(w, c);
            return p0 - src.SumPopulation();
        }
        Assert.Equal(0.0, Moved(tab, O + Mid, liv));                       // ①
        Assert.Equal(0.0, Moved(liv, O + Mid, tab));                       // ②
        Assert.True(Moved(fir, O + Mid, liv) > 0, "③ 本国どうしで流れない");
        Assert.True(Moved(teh, O + Mid, isf) > 0, "④ 征服した同じ民族の州どうしで流れない");
        Assert.Equal(0.0, Moved(teh, O + Mid, tab));                       // ⑤
        // ⑥ 差が越境の閾値を超えた分だけ流れる
        const double Over = 0.2;
        double movable = liv.Pops.Where(p => p.IsCommoner && tab.PopOf(p.ClassId) is not null).Sum(p => p.Size);
        var a = Places.Coord(liv.Name); var b = Places.Coord(tab.Name);
        double expect = c.RegionMobility * Over * Territory.MigrationDecay(Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon), w.WorldTechLevel, c) * movable;
        double got = Moved(tab, O + c.CrossBorderOppGapThreshold + Over, liv);
        Assert.True(Math.Abs(got - expect) <= 1e-9 * expect, $"⑥ 流れ {got} ≠ RegionMobility×(差−越境の閾値)×距離の減衰×人数 {expect}");
    }

    /// <summary>★(191) 本国どうし（どちらも格 Core）は言葉と民族が違っても国内の摩擦で移る＝プラハ（チェコ・開幕の中核の割合0）↔ウィーン（ドイツ・1）。
    /// 同じ盤面で、本国と割譲した異民族の州（リヨン＝格 Integrated・中核の割合0）の間は流れない（費用が言葉と民族で上がるのは征服地と植民地が絡む組だけ）。
    /// 差は国内の摩擦と越境の閾値の間（0.3）・耕地は未設定・受け入れの上限は外す（閾値だけを見る）。
    /// ⚠壊し方＝本国の規則を外す（中核の割合だけで読む）→ プラハから出ない＝赤／全ての組に本国の規則を当てる → リヨンへ流れる＝赤。</summary>
    [Fact]
    public void 本国どうしは言葉と民族が違っても国内の摩擦で移る()
    {
        var sim = Board("オーストリア", "フランス");
        var w = sim.Nations[0].World; var fr = sim.Nations[1].World;
        Territory.TransferRegion(fr, w, fr.Regions.First(r => r.Name == "リヨン"), Territory.Rehoming.Ceded);
        var c = w.Coef;
        c.ImmigrationReceiveCapFrac = 1e9;
        foreach (var r in w.Regions) { Assert.Equal(RegionState.Active, r.State); r.ArableLand = 1e8; }
        Region R(string n) => w.Regions.First(r => r.Name == n);
        Region wien = R("ウィーン"), prag = R("プラハ"), lyo = R("リヨン");
        Assert.Equal(RegionTier.Core, wien.Tier); Assert.Equal(RegionTier.Core, prag.Tier); Assert.Equal(RegionTier.Integrated, lyo.Tier);
        Assert.NotEqual(wien.Culture, prag.Culture);
        double cw = Politics.AvgRegionCoreShare(wien), cp = Politics.AvgRegionCoreShare(prag);
        Assert.True(cw > 0.95 && cp < 0.05, $"前提が崩れた：中核の割合 ウィーン {cw}・プラハ {cp}");
        Assert.Equal(0.0, Politics.AvgRegionCoreShare(lyo), 12);
        Assert.Equal(1.0, Territory.EthnicAffinity(prag, cp, wien, cw));
        const double O = 1.0, Mid = 0.3;   // 国内の摩擦と越境の閾値の間
        Assert.True(c.TransportFriction < Mid && Mid < c.CrossBorderOppGapThreshold, "前提が崩れた");
        double Moved(Region dst, double dstOpp, Region src)   // 全ての州の機会を O・行き先だけ dstOpp にして1か月＝src から出た人数
        {
            foreach (var r in w.Regions) SetOpp(r, ReferenceEquals(r, dst) ? dstOpp : O, c);
            double p0 = src.SumPopulation();
            Territory.Step(w, c);
            return p0 - src.SumPopulation();
        }
        Assert.True(Moved(wien, O + Mid, prag) > 0, "本国どうし（プラハ→ウィーン）で流れない");
        Assert.True(Moved(prag, O + Mid, wien) > 0, "本国どうし（ウィーン→プラハ）で流れない");
        Assert.Equal(0.0, Moved(lyo, O + Mid, wien));   // 本国→割譲した異民族の州
        Assert.Equal(0.0, Moved(wien, O + Mid, lyo));   // 割譲した異民族の州→本国
    }

    // ===== 州の移し替えの1本（2026-09-28 処理の一元化 段2 B）の番人 =====

    private static MultiNationSim Board(params string[] names)
        => new(names.Select(WorldData.ByName).ToArray(), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    [Fact]
    public void 再併合の返還州は親の中で一意のIdを受ける()
    {
        // 旧＝返還州の Id を「親の州の数」で振った＝建国で末尾でない州が抜けて Id が歯抜けのとき、返還州に既存の州と同じ Id が付いた。
        var sim = Board("フランス", "イギリス");
        var pw = sim.Nations[0].World;
        Assert.True(pw.Regions.Count >= 3, "前提が崩れた：親の州が3つ未満（末尾でない州が抜けられない）");
        var leaving = pw.Regions[1];
        pw.PendingRebelRegions = [leaving]; pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        int maxId = pw.Regions.Max(r => r.Id);
        Assert.Equal(pw.Regions.Count, maxId);   // 前提＝歯抜け（旧の「州の数」がちょうど既存の最大 Id に当たる）

        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
        sim.ReabsorbNations();

        Assert.Contains(leaving, pw.Regions);
        Assert.Equal(pw.Regions.Count, pw.Regions.Select(r => r.Id).Distinct().Count());
        Assert.Equal(maxId + 1, leaving.Id);
        Assert.Equal(0, pw.Regions[0].Id);
    }

    [Fact]
    public void 国を除いた四半期も交易の接触は正しい国に付く()
    {
        // 四半期のペア貿易額 PairFlowQ は国の番号で引く。旧＝再併合で国を除いても行と列を残した＝後ろの国が1つ前の国の貿易で接触度を受け取った。
        var sim = Board("フランス", "イギリス", "スペイン");
        var fr = sim.Nations[0].World; var es = sim.Nations[2].World;
        fr.PendingRebelRegions = [fr.Regions[1]]; fr.PendingRebelPolity = Polity.Republic;
        es.PendingRebelRegions = [es.Regions[1]]; es.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();
        Assert.Equal(5, sim.Nations.Count);   // 前提：反乱国が2つ（仏の反乱国＝添字3・西の反乱国＝添字4）
        sim.StepQuarter();                    // 反乱国にも名目GDPを立てる（接触度の分母）
        var r1 = sim.Nations[3].World; var r2 = sim.Nations[4].World;
        Assert.True(r2.NominalGdp > 0, "前提が崩れた：西の反乱国の名目GDPが0");
        // 西の反乱国（添字4）と英（添字1）だけが交易した四半期
        int n = sim.Nations.Count;
        var pf = new double[n][];
        for (int i = 0; i < n; i++) pf[i] = new double[n];
        pf[4][1] = pf[1][4] = 1000;
        sim.Market.PairFlowQ = pf;

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, r1.SelfPower!, false, fr.Polity));
        sim.ReabsorbNations();   // 仏の反乱国（添字3）を除く＝西の反乱国は添字3へ詰まる

        Assert.Same(r2, sim.Nations[3].World);
        var q = sim.Market.PairFlowQ!;
        Assert.Equal(sim.Nations.Count, q.Length);
        Assert.Equal(1000.0, q[3][1]); Assert.Equal(1000.0, q[1][3]);
        // 四半期末の伝播の段（StepDiffusion）が交易の接触度を西の反乱国に付ける（除いた国の隣の国ではなく）
        typeof(MultiNationSim).GetMethod("StepDiffusion", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sim, null);
        Assert.True(r2.TradeContactShare > 0, $"交易した西の反乱国に接触度が付いていない（{r2.TradeContactShare}）");
        Assert.Equal(0.0, fr.TradeContactShare);
        Assert.Equal(0.0, es.TradeContactShare);
    }

    [Fact]
    public void 首都を失った国は遷都し距離の基準の座標も新しい首都へ移る()
    {
        // 旧＝割譲の首都の補修と国家形成の遷都（Formations）が別々の手順を持ち、どちらも World.Latitude/Longitude を構築時のまま残した。
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World; var br = sim.Nations[1].World;
        var oldCap = fr.Regions[0];
        double lat0 = fr.Latitude, lon0 = fr.Longitude;

        Territory.TransferRegion(fr, br, oldCap, Territory.Rehoming.Ceded);   // 講和で首都を取られた

        var cap = fr.Regions[0];
        Assert.NotSame(oldCap, cap);
        Assert.Equal(0, cap.Id);
        Assert.Single(fr.Regions, r => r.Id == 0);
        var cc = Places.Coord(cap.Name);
        Assert.False(cc.Lat == 0 && cc.Lon == 0, "前提が崩れた：新しい首都の座標が未登録");
        Assert.Equal((cc.Lat, cc.Lon), (fr.Latitude, fr.Longitude));
        Assert.NotEqual((lat0, lon0), (fr.Latitude, fr.Longitude));

        // 国家形成の遷都も同じ1本＝Id の入れ替え・座標・旧首都は植民地財の産出をやめる（植民地財は首都州だけ）
        var next = fr.Regions[^1]; int nextId = next.Id;
        cap.ColonialResources = [1];
        fr.SetCapital(next);
        Assert.Same(next, fr.Regions[0]);
        Assert.Equal(0, next.Id);
        Assert.Equal(nextId, cap.Id);
        Assert.Empty(cap.ColonialResources!);
        var nc = Places.Coord(next.Name);
        Assert.Equal((nc.Lat, nc.Lon), (fr.Latitude, fr.Longitude));
    }

    /// <summary>★州の不穏は平民の民間人の闘争度（2026-09-30 処理の一元化 段4e H＝州の闘争度の1本 Politics.RegionMilitancy）：移民の押し出し（機会の不穏）・
    /// 植民地の維持費の不満・反帝世論が同じ集合を読む。旧＝3か所が全 POP（駐留の兵・在地の貴族と聖職）の和で手書きした。
    /// 盤面＝仏の首都州の兵士だけ闘争度1＝機会は動かない／平民の民間人を闘争度1＝機会が下がる。⚠突然変異「全 POP で数える」で赤。</summary>
    [Fact]
    public void 州の不穏は平民の民間人の闘争度だけで兵と貴族を含まない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        foreach (var p in r.Pops) p.Militancy = 0;
        double o0 = Territory.Opportunity(r, c);
        foreach (var p in r.Pops) if (!p.IsCommoner) p.Militancy = 1.0;
        Assert.Equal(o0, Territory.Opportunity(r, c), 12);
        foreach (var p in r.Pops) if (p.IsCommoner) p.Militancy = 1.0;
        Assert.True(Territory.Opportunity(r, c) < o0, "平民の民間人の闘争度で機会が下がらない＝押し出しが死んでいる");
        Assert.Equal(1.0, Politics.AvgRegionMilitancy(r), 12);
    }
}
