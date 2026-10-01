using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★発見の作り替えの番人（段2A・2026-10-01・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §1A-2）。
/// 発見は2値・点数＝技術力（0〜70）＋必要性（0〜70）＋伝わり（0〜70）が一度でも100に届いたら解禁・失われない。
/// 作者 2026-09-30「中国では人と生糸が余っているので、紡績機は導入されない、という形にしたい。なので、それでよい」
/// 「労働を代替する発明は、安い人件費の国では必要とされない、という認識でよい」。</summary>
public class DiscoveryScoreTests
{
    private static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == name);

    /// <summary>1四半期だけ回した盤面（市場の需要と値段が立つ・開幕の助走は通さない＝速い）。</summary>
    private static MultiNationSim Board(ulong seed = 7)
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent());
        sim.StepQuarter();
        return sim;
    }

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    [Fact]
    public void 点数の性質_どの項も1つでは届かずどの2つでも届く()
    {
        Assert.True(Discovery.TermMax < Discovery.GateScore, "1つの項だけで門に届く");
        Assert.True(2 * Discovery.TermMax >= Discovery.GateScore, "2つの項が揃っても門に届かない");
        // 盤面の上でも3項はどれも [0, 70]
        var sim = Board();
        foreach (var n in sim.Nations)
            foreach (var t in Discovery.Evaluate(n.World, n.World.LastDiscoveryView, n.World.Coef, includeKnown: true))
                foreach (double v in new[] { t.Tech, t.Need, t.Transmit })
                    Assert.True(v >= 0 && v <= Discovery.TermMax, $"{n.World.SelfPower?.Name}：項が範囲の外（{v}）");
    }

    [Fact]
    public void 難易度は年代を1本のYで写す_1780年代は0()
    {
        var c = new Coefficients();
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
        {
            var d = Catalog.Discoveries[r];
            Assert.Equal(c.DiscoveryTechPerDecade * (d.Decade - Catalog.DiscoveryBaseDecade) / 10.0, Discovery.Difficulty(r, c), 12);
            Assert.True(d.Decade % 10 == 0 && d.Decade >= Catalog.DiscoveryBaseDecade, $"{d.Name}：年代が10年刻みでない（{d.Decade}）");
        }
        Assert.Equal(0.0, Discovery.Difficulty(Row("蒸気機関"), c));
    }

    /// <summary>作者の答え（2026-09-30）の番人＝労働を代替する発明（蒸気機関＝労働力の財の行）の必要性は人件費の世界比で決まる：
    /// 同じ盤面・同じ技術で、人件費が世界の平均の半分の国（世界の平均＝自国の2倍）は必要性0＝自力では発見しない（技術力だけでは門に届かない）。
    /// 世界の平均の5分の1の国（人件費が世界の5倍）は必要性が立ち、技術力と合わせて門に届く。
    /// ⚠壊し方＝人件費の世界比を読まない（労働の財の1単位の値段で測る）・技術力だけで解禁する（門を70へ）→ どちらも赤。</summary>
    [Fact]
    public void 安い人件費の国は労働を代替する発明を自力で見つけない()
    {
        var sim = Board();
        int steam = Row("蒸気機関");
        World? w = null;
        foreach (var n in sim.Nations)
            if (!n.World.Discovered[steam] && Discovery.LaborWage(n.World).Wage > 0) { w = n.World; break; }
        Assert.NotNull(w);
        double own = Discovery.LaborWage(w!).Wage;
        var cheap = Discovery.Evaluate(w!, new Discovery.WorldView(null, own * 2.0), w!.Coef)[steam];
        var dear = Discovery.Evaluate(w!, new Discovery.WorldView(null, own / 5.0), w!.Coef)[steam];
        Assert.Equal(0.0, cheap.Need);
        Assert.True(cheap.Score < Discovery.GateScore, $"安い人件費の国が技術力だけで届く（{cheap.Score:F1}）");
        Assert.True(dear.Need > 0, $"高い人件費の国に必要性が立たない（{dear.Need:F1}）");
        Assert.True(dear.Tech == cheap.Tech, "人件費で技術力が変わった");
        Assert.True(dear.Score > cheap.Score);

        // 判定の1本（Step）も同じ＝安い国は解禁しない・高い国は解禁する
        Discovery.Step(w!, new Discovery.WorldView(null, own * 2.0), w!.Coef);
        Assert.False(w!.Discovered[steam]);
        if (dear.Score >= Discovery.GateScore)
        {
            Discovery.Step(w!, new Discovery.WorldView(null, own / 5.0), w!.Coef);
            Assert.True(w!.Discovered[steam]);
        }
    }

    /// <summary>伝わりだけでは届かない・技術力と揃えば届く（設計 §1A-2 の性質）＝必要性の無い行（合成ゴム＝ゴムをまだ作れない国の製法は必要性0）で、
    /// ①技術水準0・接触は最大（伝わり≈70）→ 届かない ②技術水準を難易度の2倍（技術力 70×押し）＋同じ接触 → 押しが半分以上の国は届く（そういう国が1つは在る）。
    /// ⚠壊し方＝門を70へ（伝わりだけで届く）→ ①が赤。</summary>
    [Fact]
    public void 伝わりだけでは届かず技術力と揃えば届く()
    {
        var sim = Board();
        int row = Row("合成ゴム");
        var contact = new double[Catalog.Discoveries.Length];
        contact[row] = 1e6;   // 接触は最大（伝わり→70）
        var view = new Discovery.WorldView(contact, 0);
        int tried = 0, reached = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Discovered[row] || !(Discovery.Evaluate(w, null, w.Coef)[row].Need == 0)) continue;
            tried++;
            w.TechLevel = 0;
            var only = Discovery.Evaluate(w, view, w.Coef)[row];
            Assert.True(only.Transmit > 69.9 && only.Tech == 0 && only.Need == 0, $"前提が崩れた（{only}）");
            Discovery.Step(w, view, w.Coef);
            Assert.False(w.Discovered[row], $"{w.SelfPower?.Name}：伝わりだけで解禁した");
            w.TechLevel = Discovery.Difficulty(row, w.Coef) * 2;
            var both = Discovery.Evaluate(w, view, w.Coef)[row];
            if (!(both.Score >= Discovery.GateScore)) continue;
            reached++;
            Discovery.Step(w, view, w.Coef);
            Assert.True(w.Discovered[row], $"{w.SelfPower?.Name}：技術力と伝わりが揃っても解禁しない（{both}）");
        }
        Assert.True(tried > 10 && reached > 0, $"盤面が空回り（試した国 {tried}・届いた国 {reached}）");
    }

    /// <summary>解禁は失われない（2値の知識＝作者裁定 §1A-2）：点数が0の行を解禁済みにしておけば、判定を何度通しても解禁のまま。</summary>
    [Fact]
    public void 解禁は失われない()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        int row = Row("ラジオ");
        w.TechLevel = 0;
        Assert.True(Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[row].Score < Discovery.GateScore, "前提が崩れた");
        w.Discovered[row] = true;
        for (int i = 0; i < 3; i++) Discovery.Step(w, null, w.Coef);
        Assert.True(w.Discovered[row]);
    }

    /// <summary>財の解禁＝その財のレシピの解禁の最大（設計 §1-2）：地理差のレシピ（表に無い）が1本でもある財は常に解禁・既定のレシピの行しかない財は発見まで0。
    /// 需要は世界の解禁の和で読む（どこかの国が作れる財は輸入で手に入る）＝国の解禁が無くても世界が解禁していれば需要の門は開く・作る門は閉じたまま。</summary>
    [Fact]
    public void 財の解禁はレシピの最大_需要は世界の解禁の和()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        var r = w.Region;
        int ng = Catalog.Goods.Length;
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.PaperId));   // 紙＝ぼろ布製紙（地理差）がある＝木材パルプを発見していなくても解禁
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.SteelId));   // 鋼＝坩堝鋼（地理差）
        int canned = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, ""), fish = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, "魚の缶詰");
        Assert.False(w.Discovered[canned] || w.Discovered[fish], "前提が崩れた（缶詰が発見済み）");
        Assert.Equal(0.0, Discovery.GoodGate(r, Catalog.CannedFoodId));

        var union = new bool[Catalog.Discoveries.Length];
        union[canned] = true;   // 世界のどこかが缶詰を発見した
        var prod = new double[ng]; var dem = new double[ng];
        Discovery.GoodGates(r, prod, ng, r.Discovered);
        Discovery.GoodGates(r, dem, ng, union);
        Assert.Equal(0.0, prod[Catalog.CannedFoodId]);   // 作る門＝自国の解禁
        Assert.Equal(1.0, dem[Catalog.CannedFoodId]);    // 需要の門＝世界の解禁の和
        for (int g = 0; g < ng; g++) Assert.Equal(Discovery.GoodGate(r, g), prod[g]);   // 1回の走査と財ごとの読みは同じ値
    }

    /// <summary>需要の門は世界の解禁の和を読む＝EconomyStep の消費の1本の番人（双子の盤面・片方だけ世界の和に缶詰を立てる→缶詰の需要が立つ）。
    /// ⚠壊し方＝需要の門を自国の解禁で読む（goodGate）→ 双子の需要が同じ＝赤。</summary>
    [Fact]
    public void 需要の門は世界の解禁の和で開く_自国が未発見でも輸入の需要が立つ()
    {
        var a = Board(); var b = Board();
        int canned = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, "");
        int i = a.Nations.FindIndex(n => !n.World.Discovered[canned]);
        Assert.True(i >= 0);
        var wa = a.Nations[i].World; var wb = b.Nations[i].World;
        Assert.NotNull(wa.WorldDiscovered);
        Assert.False(wa.WorldDiscovered![canned] || wb.WorldDiscovered![canned], "前提が崩れた（世界が缶詰を発見済み）");
        wb.WorldDiscovered![canned] = true;   // B だけ世界の和に缶詰（全ての国で同じ配列＝B の世界全体）
        EconomyStep.Run(wa.Region, wa.Coef, 1.0, wa.Bank, wa);
        EconomyStep.Run(wb.Region, wb.Coef, 1.0, wb.Bank, wb);
        Assert.True(wb.Region.Markets[Catalog.CannedFoodId].LastDemand > wa.Region.Markets[Catalog.CannedFoodId].LastDemand,
            $"世界が発見した缶詰に需要が立たない（A {wa.Region.Markets[Catalog.CannedFoodId].LastDemand}／B {wb.Region.Markets[Catalog.CannedFoodId].LastDemand}）");
        Assert.Equal(0.0, Discovery.GoodGate(wb.Region, Catalog.CannedFoodId));   // 作る門は閉じたまま
    }

    /// <summary>製法の割高さは分子と分母が同じ規則（投入の費用どうし）＝全ての値段が参照価格×物価水準の世界では必要性0、
    /// 今の方法だけが使う投入が1%だけ割高なら必要性が立つ（人絹＝絹織物の既定のレシピ〔生糸〕を置き換える製法）。
    /// ⚠壊し方＝参照の側を財の参照価格（工場の自己費用 FactoryVa を含む）で置く＝参照の世界でも割高さが −6.9% に偏り、1%の割高さでは必要性0 → 赤。</summary>
    [Fact]
    public void 製法の割高さは分子と分母が同じ規則_参照価格の世界で0()
    {
        var sim = Board();
        int row = Row("人絹");
        int g0 = Catalog.SilkClothId;
        var refP = Catalog.RefPrices;
        World? w = null;
        foreach (var n in sim.Nations)
        {
            var wn = n.World;
            if (wn.Discovered[row]) continue;
            double dd = 0; foreach (var rg in wn.Regions) if (rg.State == RegionState.Active) dd += rg.Markets[g0].LastDemand;
            if (dd > 1e-6) { w = wn; break; }
        }
        Assert.NotNull(w);
        var p0 = w!.Region.Producers.Find(p => p.GoodId == g0 && p.RecipeName.Length == 0);
        var p1 = w.Region.Producers.Find(p => p.GoodId == g0 && p.RecipeName == "人絹");
        Assert.True(p0 is not null && p1 is not null, "前提が崩れた（絹織物の既定のレシピか人絹が無い）");
        Assert.Single(w.Region.Producers, p => p.GoodId == g0 && Discovery.Gate(w.Region, p) > 0);   // 今の方法は既定のレシピの1本
        var oldOnly = p0!.Inputs.Select(i => i.GoodId).Where(g => g >= 0 && !p1!.Inputs.Any(j => j.GoodId == g)).ToArray();
        Assert.NotEmpty(oldOnly);

        double NeedAt(double bump)
        {
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                for (int g = 0; g < rg.Markets.Length && g < refP.Length; g++) rg.Markets[g].Price = w.NominalOf(refP[g]);
                foreach (int g in oldOnly)
                {
                    rg.Markets[g].LastDemand = 1.0; rg.Markets[g].LastProduced = 1.0;   // 生きた市場＝値段を読む
                    rg.Markets[g].Price = w.NominalOf(refP[g]) * bump;
                }
            }
            return Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef)[row].Need;
        }
        double atRef = NeedAt(1.0), dear = NeedAt(1.01);
        Assert.True(atRef < 1e-6, $"参照価格の世界で必要性が立つ（{atRef}）＝分子と分母が別の規則");
        Assert.True(dear > 0, $"今の方法の投入が1%割高でも必要性が立たない（{dear}）＝参照の側が偏っている");
    }

    /// <summary>伝わりの運び手＝その行の財（取引できるとき）＋レシピの投入の資本財（設計 §1A-7「財そのものの輸入で学ぶ」）。
    /// 労働を代替する発明（蒸気機関）は労働力を運べない＝蒸気機関の財で運ばれる。⚠壊し方＝資本財の投入を運び手にしない→赤。</summary>
    [Fact]
    public void 伝わりの運び手は財と資本財の投入()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        var carriers = Discovery.CarriersByRow(sim.Nations.Select(n => n.World));
        var steam = carriers[Row("蒸気機関")];
        Assert.Contains(Catalog.SteamEngineId, steam);
        Assert.DoesNotContain(Catalog.LaborId, steam);
        Assert.Contains(Catalog.SteelId, carriers[Row("転炉鋼")]);
        Assert.Contains(Catalog.MachineGunId, carriers[Row("機関銃")]);
        var byCarrier = Discovery.RowsByCarrier(carriers);
        Assert.Contains(Row("蒸気機関"), byCarrier[Catalog.SteamEngineId]);
    }

    // ===== ★新兵器の必要性の損耗の項（2026-10-01 (153)・設計 §1A-4 (4a)「その分野の脅威比＋交戦中のその分野の損耗の率」・作者裁定 2026-09-28 ⑤「陸・海・空で分ける」）=====

    /// <summary>新兵器の必要性は交戦中の**その分野の**損耗で上がり、他の分野の損耗では動かない（陸＝機関銃・戦車・化学兵器／海＝装甲艦・弩級戦艦／空＝軍用機）。
    /// 単位＝損耗の強さ（最大を1とした割合）は脅威の割合と同じ物差し＝強度は2つの和（換算の数を置かない）：損耗 x の必要性＝70×(脅威の割合＋x)÷(…＋半分点)。
    /// ⚠壊し方＝損耗を読まない・分野を取り違える（海の行が陸の損耗を読む）・損耗に換算の数を掛ける → 赤。記帳の側（戦死・艦隊摩耗）は WarThemeTests。</summary>
    [Fact]
    public void 新兵器の必要性は交戦中のその分野の損耗で上がり他の分野では動かない()
    {
        var sim = Board();
        int[] landRows = [Row("機関銃"), Row("戦車"), Row("化学兵器")], seaRows = [Row("装甲艦"), Row("弩級戦艦")];
        int airRow = Row("軍用機");
        int[] all = [.. landRows, .. seaRows, airRow];
        World? w = null;
        foreach (var n in sim.Nations)
        {
            var t = Discovery.Evaluate(n.World, null, n.World.Coef, includeKnown: true);
            if (all.All(r => t[r].Tech > 0)) { w = n.World; break; }   // 6行とも代表の生産者が居る国
        }
        Assert.NotNull(w);
        double half = w!.Coef.DiscoveryNeedHalfWeapon;
        Discovery.Terms[] T(double land, double sea, double air)
        {
            var loss = new double[Discovery.LossDomains];
            loss[(int)WarDomain.Land] = land; loss[(int)WarDomain.Sea] = sea; loss[(int)WarDomain.Air] = air;
            return Discovery.Evaluate(w, new Discovery.WorldView(null, 0, loss), w.Coef, includeKnown: true);
        }
        var t0 = T(0, 0, 0); var tl = T(0.5, 0, 0); var ts = T(0, 0.5, 0); var ta = T(0, 0, 0.5);
        var none = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (int r in all) Assert.Equal(none[r].Need, t0[r].Need);   // 損耗0＝損耗の無い判定と同じ
        static double StrengthOf(double need, double h) => h * need / (Discovery.TermMax - need);   // 必要性→強度（70×s÷(s＋h) の逆）
        static double NeedOf(double s, double h) => Discovery.TermMax * s / (s + h);
        void Check(int[] rows, Discovery.Terms[] hit, Discovery.Terms[] miss1, Discovery.Terms[] miss2, string dom)
        {
            foreach (int r in rows)
            {
                string nm = Catalog.Discoveries[r].Name;
                Assert.True(hit[r].Need > t0[r].Need, $"{nm}：{dom}の損耗で必要性が上がらない（{t0[r].Need:F3}→{hit[r].Need:F3}）");
                Assert.Equal(t0[r].Need, miss1[r].Need);
                Assert.Equal(t0[r].Need, miss2[r].Need);
                Assert.Equal(NeedOf(StrengthOf(t0[r].Need, half) + 0.5, half), hit[r].Need, 9);   // 強度＝脅威の割合＋損耗の強さ（同じ単位＝換算の数なし）
                Assert.True(hit[r].Tech == t0[r].Tech && hit[r].Transmit == t0[r].Transmit, $"{nm}：損耗で技術力か伝わりが動いた");
            }
        }
        Check(landRows, tl, ts, ta, "陸");
        Check(seaRows, ts, tl, ta, "海");
        Check([airRow], ta, tl, ts, "空");
    }

    /// <summary>損耗の記帳（World.DiscoveryLossQ＝月ごとに積む）は判定で四半期の月の平均になり（画面と計器が同じ値を読む＝World.LastDiscoveryView.Loss）、記帳は捨てられる。
    /// 多国の世界でも四半期をまたいで積み残さない（判定の Discovery.Step が捨てる＝共有層と助走の捨てる行はこの番人では見ていない＝(154) の反証役の指摘・今は助走中に外交が止まり記帳が積まれない）。⚠壊し方＝捨てない（次の四半期へ積み残す）・平均にしない（月の和のまま）→ 赤。</summary>
    [Fact]
    public void 損耗の記帳は判定で四半期の月の平均になり捨てられる()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        var lq = new double[Discovery.LossDomains];
        lq[(int)WarDomain.Land] = 0.3; lq[(int)WarDomain.Sea] = 0.6; lq[(int)WarDomain.Air] = 0.9;
        w.DiscoveryLossQ = lq;
        Discovery.Step(w, new Discovery.WorldView(null, 0), w.Coef);
        Assert.Null(w.DiscoveryLossQ);
        var l = w.LastDiscoveryView?.Loss;
        Assert.NotNull(l);
        Assert.Equal(0.3 / Calendar.MonthsPerQuarter, l![(int)WarDomain.Land], 12);
        Assert.Equal(0.6 / Calendar.MonthsPerQuarter, l[(int)WarDomain.Sea], 12);
        Assert.Equal(0.9 / Calendar.MonthsPerQuarter, l[(int)WarDomain.Air], 12);
        // 多国の四半期でも積み残さない（判定が捨てる・共有層の捨てる行はこの番人では見ていない＝(154)）
        foreach (var n in sim.Nations) n.World.DiscoveryLossQ = new double[Discovery.LossDomains];
        sim.StepQuarter();
        Assert.All(sim.Nations, n => Assert.Null(n.World.DiscoveryLossQ));
    }
    // ===== ★産地の発見（2026-10-01 (148)・Catalog.Sites＝大鉱床と植林）の番人 =====

    /// <summary>産地の生産者を探す（州の名前＝表の場所・財＝表の財）。</summary>
    private static (World W, Region R, Producer P) Site(MultiNationSim sim, string siteName)
    {
        int row = System.Array.FindIndex(Catalog.Sites, x => x.Name == siteName);
        Assert.True(row >= 0, $"表に {siteName} が無い");
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                foreach (var p in r.Producers)
                    if (p.SiteRow == row) return (n.World, r, p);
        throw new Xunit.Sdk.XunitException($"{siteName} の生産者が盤面に無い");
    }

    /// <summary>産地は表からだけ来る（州も植民地も同じ表・場所の名前で引く）：産地の生産者は表の場所の州にあり財が一致する・開始の盤面に場所がある行は
    /// ちょうど1つの生産者を持つ・銀の行は SilverRushEnabled＝0 の間は置かない（検討 Q13 (d)）・抽象の標的（アルタ=カリフォルニア・南アフリカ）は入植するまで無い。
    /// ⚠壊し方＝銀の旗を読まない → 西部の銀の生産者が産地になる＝赤。</summary>
    [Fact]
    public void 産地は表からだけ来る_銀は旗が0の間は置かない()
    {
        var sim = Board();
        var count = new int[Catalog.Sites.Length];
        var places = new HashSet<string>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                places.Add(r.Name);
                foreach (var p in r.Producers)
                {
                    if (p.SiteRow < 0) continue;
                    var st = Catalog.Sites[p.SiteRow];
                    Assert.Equal(st.Place, r.Name);
                    Assert.Equal(st.Good, p.GoodId);
                    Assert.True(p.IsRgo, $"{st.Name}：産地の生産者が採取でない");
                    count[p.SiteRow]++;
                }
            }
        Assert.Equal(0.0, new Coefficients().SilverRushEnabled);
        for (int i = 0; i < Catalog.Sites.Length; i++)
        {
            var st = Catalog.Sites[i];
            int expect = st.Good == Catalog.SilverId || !places.Contains(st.Place) ? 0 : 1;
            Assert.True(count[i] == expect, $"{st.Name}（{st.Place}）：産地の生産者 {count[i]}（期待 {expect}）");
        }
        foreach (var name in new[] { "ジャワのキナ", "ニルギリのキナ", "セイロンのキナ", "ルイジアナの硫黄", "九州の硫黄", "ヴィクトリアの金", "クロンダイクの金" })
            Assert.True(count[System.Array.FindIndex(Catalog.Sites, x => x.Name == name)] == 1, $"{name} が開始の盤面に無い");
    }

    /// <summary>見つかるまで産まない・見つかったら開く・失われない（2値）＋キナはアンデスだけが開始から（(129) の申し送り）：
    /// ①アジアのキナ（バタヴィア）の生産者は門0・ペルーの樹皮は門1 ②技術水準0では入植者が厚くても届かない（技術力0＋必要性≦70）
    /// ③技術水準を難易度へ上げると届いて開く（門1）④技術水準を0へ戻して判定を重ねても開いたまま。
    /// ⚠壊し方＝門が SiteFound を読まない（常に1）→ ①が赤／判定で SiteFound を戻す → ④が赤。</summary>
    [Fact]
    public void 産地は見つかるまで産まない_見つかったら開いて失われない()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ジャワのキナ");
        Assert.False(p.SiteFound, "前提が崩れた（開始から開いている）");
        Assert.Equal(0.0, Discovery.Gate(r, p));
        // ペルー（アンデス）の樹皮は開始から産む（表に無い＝開始時の賦存）
        bool peru = false;
        foreach (var n in sim.Nations)
            foreach (var rg in n.World.Regions)
                if (rg.Name == "ペルー")
                    foreach (var q in rg.Producers)
                        if (q.GoodId == Catalog.CinchonaBarkId) { peru = true; Assert.True(q.SiteRow < 0); Assert.Equal(1.0, Discovery.Gate(rg, q)); }
        Assert.True(peru, "ペルーの樹皮が無い");
        // 財の解禁（作る門・需要の門）は「場所がまだ知られていない」を「財がまだ発見されていない」と取り違えない
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.CinchonaBarkId));

        double tech0 = w.TechLevel;
        w.TechLevel = 0;
        var t0 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.True(t0.Tech == 0 && t0.Transmit == 0 && t0.Need <= Discovery.TermMax, $"項が範囲の外（{t0}）");
        Discovery.Step(w, null, w.Coef);
        Assert.False(p.SiteFound, "技術力0で開いた");

        w.TechLevel = Discovery.DifficultyOf(Catalog.Sites[p.SiteRow].Decade, w.Coef) * 2;
        var t1 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.Equal(Discovery.TermMax, t1.Tech);
        Assert.True(t1.Score >= Discovery.GateScore, $"技術力70でも住民×樹皮の値で届かない（{t1}）");
        Discovery.Step(w, null, w.Coef);
        Assert.True(p.SiteFound);
        Assert.Equal(1.0, Discovery.Gate(r, p));

        w.TechLevel = 0;
        for (int i = 0; i < 3; i++) Discovery.Step(w, null, w.Coef);
        Assert.True(p.SiteFound, "開いた産地が閉じた");
        Assert.Equal(1.0, Discovery.Gate(r, p));
        w.TechLevel = tech0;
    }

    /// <summary>鉱床の必要性は入植者（その国の中核民族の人数）×財の値（設計 §9-1 の提案③・2026-09-28 作者が採用した提案＝文は AI の整理）・植林は住民：
    /// 同じ州の住民を全て中核民族でなくすと、鉱床（ルイジアナの硫黄）の必要性は0＝技術力70だけでは届かない／植林（ジャワのキナ）の必要性は変わらない。
    /// ⚠壊し方＝鉱床の人の厚みを住民で数える → 鉱床の必要性が0にならない＝赤。</summary>
    [Fact]
    public void 鉱床は入植者が居なければ見つからない_植林は住民で見る()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        w.TechLevel = Discovery.DifficultyOf(Catalog.Sites[p.SiteRow].Decade, w.Coef) * 2;
        Assert.True(Discovery.SiteTerms(w, r, p, w.Coef).Need > 0, "前提が崩れた（入植者が居るのに必要性0）");
        foreach (var q in r.Pops) q.CoreEthnicShare = 0;
        var t = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.Equal(0.0, t.Need);
        Discovery.Step(w, null, w.Coef);
        Assert.False(p.SiteFound, $"入植者0の鉱床が開いた（{t}）");

        var (wk, rk, pk) = Site(sim, "ジャワのキナ");
        double before = Discovery.SiteTerms(wk, rk, pk, wk.Coef).Need;
        Assert.True(before > 0, "前提が崩れた（植林の必要性0）");
        foreach (var q in rk.Pops) q.CoreEthnicShare = 0;
        Assert.Equal(before, Discovery.SiteTerms(wk, rk, pk, wk.Coef).Need);
    }

    /// <summary>開いた鉱床の富鉱は開いた財の生産者だけ（検討 Q13 (c)・旧＝州の全ての鉱物の MineralDepletion）：同じ州の他の生産者の生産性は1ビットも変わらない・
    /// 開いた生産者は ×GoldRushEndowment・操業規模≥1（開口＝即産）・州の鉱脈在庫は BonanzaDepositYears×初期在庫÷DepositYearsAtBirth だけ増える。
    /// ⚠壊し方＝州の全ての鉱物に富鉱を掛ける → 他の生産者が変わる＝赤。</summary>
    [Fact]
    public void 開いた鉱床の富鉱は開いた財の生産者だけ()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        var before = r.Producers.Select(q => q.Productivity).ToArray();
        r.MineralDepositInitial = 1000; r.MineralDeposit = 400;
        var c = w.Coef;
        Discovery.OpenSite(w, r, p, c);
        Assert.True(p.SiteFound);
        for (int i = 0; i < r.Producers.Count; i++)
        {
            var q = r.Producers[i];
            if (ReferenceEquals(q, p)) Assert.Equal(before[i] * c.GoldRushEndowment, q.Productivity, 12);
            else Assert.Equal(before[i], q.Productivity);
        }
        Assert.True(p.Scale >= 1.0);
        Assert.Equal(400 + c.BonanzaDepositYears * 1000 / c.DepositYearsAtBirth, r.MineralDeposit, 9);
    }

    // ===== ★世界のゲート（2026-10-01 (150)）＝工場の蒸気動力・近代港湾＝「蒸気機関」の行・鉄道輸送＝「機関車」の行・冷蔵＝「冷蔵」の行（旧＝技術水準の時計と工業化度の傾斜）の番人 =====

    /// <summary>単独の仏を8四半期育てた盤面（直接 EconomyStep・Transport を1回呼ぶ番人の土台＝呼ぶ間に発見の判定は走らない）。</summary>
    private static Simulation Grown(ulong seed = 21)
    {
        var s = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent());
        s.Run(8);
        return s;
    }

    /// <summary>工場の蒸気動力は「蒸気機関」の発見が開き、技術水準では開かない（旧＝技術水準 SteamTechOnset 0.25 を越えれば発見と関係なく石炭を焚いた）。
    /// 双子の盤面（同じ種）＝①技術水準5・未発見 ②技術水準0・発見済み → 月の経済を1回：①はどの工場も石炭を焚かない（SteamFill＝0）・②は焚く。
    /// ⚠壊し方＝門を技術水準の時計へ戻す（①が焚く）・州へ国の解禁を読ませない（②が焚かない）→ 赤。</summary>
    [Fact]
    public void 世界のゲート_工場の蒸気動力は蒸気機関の発見が開き技術水準では開かない()
    {
        double Fill(double tech, bool known)
        {
            var s = Grown(); var w = s.World; var r = w.Region;
            w.Discovered[Discovery.SteamRow] = known;
            w.TechLevel = tech; r.TechLevel = tech;
            foreach (var p in r.Producers) p.SteamFill = 0;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            double f = 0; foreach (var p in r.Producers) f += p.SteamFill;
            return f;
        }
        Assert.Equal(0.0, Fill(5.0, false));
        Assert.True(Fill(0.0, true) > 0, "蒸気機関を発見した国の工場が石炭を焚かない");
    }

    /// <summary>鉄道の容量は「機関車」の発見が開き、技術水準では開かない（旧＝技術水準の傾斜 RailTechOnset 0.35〜0.85）。
    /// 双子の盤面＝線路と機関車の在庫と投資家の貯蓄を潤沢にして輸送を1回：①技術水準5・未発見＝容量を積まない ②技術水準0・発見済み＝積む。
    /// ⚠壊し方＝門を技術水準の時計へ戻す → 赤。</summary>
    [Fact]
    public void 世界のゲート_鉄道の容量は機関車の発見が開き技術水準では開かない()
    {
        double Built(double tech, bool known)
        {
            var s = Grown(); var w = s.World; var c = w.Coef;
            w.Discovered[Discovery.LocomotiveRow] = known; w.TechLevel = tech;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
                foreach (int g in new[] { c.RailGoodId, c.LocoGoodId }) { r.Markets[g].Stock = 1e6; r.Markets[g].Price = Catalog.RefPrices[g]; }
                r.Pops[r.InvestorPopIndex].SetSavings(1e7);
            }
            double after = w.RailCapacity * (1 - c.TransportDecay);   // 減耗の後
            Transport.Step(w, c);
            return w.RailCapacity - after;
        }
        Assert.Equal(0.0, Built(5.0, false), 12);
        Assert.True(Built(0.0, true) > 0, "機関車を発見した国が鉄道を積まない");
    }

    /// <summary>近代港湾（蒸気の浚渫と埠頭）の拡張は「蒸気機関」の発見が開き、技術水準では開かない（旧＝技術水準 PortTechOnset 0.2）。
    /// 英1国の盤面＝1つの港を混雑させて港の段を1回（非公開＝反射）：①技術水準5・未発見＝港の容量が増えない ②技術水準0・発見済み＝増える。
    /// ⚠壊し方＝門を技術水準の閾値へ戻す → 赤。</summary>
    [Fact]
    public void 世界のゲート_近代港湾は蒸気機関の発見が開き技術水準では開かない()
    {
        double Grow(double tech, bool known)
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            foreach (var n in sim.Nations) n.TelemetryEnabled = false;
            sim.Diplomacy.Log.Enabled = false;
            sim.StepQuarter();
            var w = sim.Nations[0].World;
            w.Discovered[Discovery.SteamRow] = known; w.TechLevel = tech;
            var port = w.Regions.Find(r => r.State == RegionState.Active && r.IsPort && r.InvestorPopIndex >= 0)!;
            foreach (var r in w.Regions) r.LastTradeThrottle = 1.0;
            port.LastTradeThrottle = 0; w.FiscalFundingRatio = 1.0;
            port.Pops[port.InvestorPopIndex].SetSavings(1e7);
            double after = port.PortCapacity * (1 - sim.Coef.PortDecay);
            typeof(MultiNationSim).GetMethod("StepPorts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim, null);
            return port.PortCapacity - after;
        }
        Assert.Equal(0.0, Grow(5.0, false), 9);
        Assert.True(Grow(0.0, true) > 0, "蒸気機関を発見した国が港を広げない");
    }

    /// <summary>冷蔵（生鮮の腐敗の低減）は「冷蔵」の発見が開く（旧＝工業化度の傾斜だけ＝1790年代に全ての国で効いた）。効きの大きさは立っている設備の規模の傾斜のまま。
    /// 双子の盤面＝工業化度5（傾斜1）・肉の在庫を置いて月の経済を1回：腐った量の比（発見済み÷未発見）＝1−RefrigerationEffect。
    /// ⚠壊し方＝発見を読まない → 未発見でも効く＝比が1−効きにならない＝赤。</summary>
    [Fact]
    public void 世界のゲート_冷蔵は冷蔵の発見が開く()
    {
        double Lost(bool known)
        {
            var s = Grown(); var w = s.World; var r = w.Region;
            w.Discovered[Discovery.ReeferRow] = known;
            r.Industrialization = 5.0;
            var m = r.Markets[Catalog.MeatId];
            m.Stock = 1000;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return m.SpoilLog[(m.SpoilIdx + 11) % 12];
        }
        double off = Lost(false), on = Lost(true);
        Assert.True(off > 0, "前提が崩れた（肉が腐らない）");
        Assert.Equal(1.0 - new Coefficients().RefrigerationEffect, on / off, 9);
    }

    // ===== ★制度と思想（2026-10-01 (152)・設計 §1A-6）＝財を持たない行・必要性は行ごとに読む量・伝わりは思想と制度の到達 =====

    /// <summary>制度と思想の行は財を持たない＝技術の押しは1（技術力＝70×技術水準÷難易度）・伝わりは思想と制度の到達（World.SpreadReach）を読み、接触（運び手の財の輸入）は読まない。
    /// ⚠壊し方＝伝わりに接触を読む・到達を読まない → 到達を半分点に置いても伝わりが35にならない＝赤。</summary>
    [Fact]
    public void 制度と思想の行は財を持たず伝わりは到達を読む()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        Assert.Equal(6, Discovery.SpreadRows.Length);
        foreach (int row in Discovery.SpreadRows)
        {
            var d = Catalog.Discoveries[row];
            Assert.Equal(Catalog.NoGood, d.Good);
            Assert.True(Discovery.IsSpread(d.Field), $"{d.Name}：制度でも思想でもない");
            int slot = Discovery.SpreadSlot(row);
            w.SpreadReach[slot] = 0;
            var contact = new double[Catalog.Discoveries.Length]; contact[row] = 1e6;
            var t0 = Discovery.Evaluate(w, new Discovery.WorldView(contact, 0), w.Coef, includeKnown: true)[row];
            Assert.Equal(0.0, t0.Transmit);   // 接触は読まない
            double diff = Discovery.Difficulty(row, w.Coef);
            Assert.Equal(Discovery.TermMax * (diff > 0 ? System.Math.Min(1, w.TechLevel / diff) : 1), t0.Tech, 9);   // 技術の押しは1
            w.SpreadReach[slot] = w.Coef.DiscoveryContactHalf;
            Assert.Equal(Discovery.TermMax / 2, Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[row].Transmit, 9);
        }
        Assert.Equal(-1, Catalog.DiscoveryRowOf(Catalog.NoGood, ""));   // 財で探しても制度と思想の行は出てこない
    }

    /// <summary>制度と思想の必要性は行ごとに読む量（設計 §1A-4・§1A-6）＝特許は中核エリート・株式会社は州の最大工業化度・社会主義と共産主義は工場労働者の人数×闘争度÷人口・
    /// 帝国主義は植民地の州に住む人の割合・ファシズムは危機（敗戦か恐慌×左派の脅威）×人々の闘争度。半分点は分野で1本。盤面の量をテストの側で数えて一致させる。
    /// ⚠壊し方＝特許を最大工業化度で読む・社会主義を全ての人の闘争度で読む・ファシズムの危機を外す → 赤。</summary>
    [Fact]
    public void 制度と思想の必要性は行ごとの量を分野の半分点で読む()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        var c = w.Coef;
        double maxInd = 0, pop = 0, wm = 0, col = 0, mil = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            if (r.TotalIndustrialization > maxInd) maxInd = r.TotalIndustrialization;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier || !(p.Size > 0)) continue;
                pop += p.Size;
                if (p.Militancy > 0) { mil += p.Size * p.Militancy; if (p.ClassId == PopClass.Worker) wm += p.Size * p.Militancy; }
                if (r.Tier == RegionTier.Colonial) col += p.Size;
            }
        }
        // ファシズムの危機を立てる（敗戦1×左派の脅威1＝全ての人の思想を社会主義へ）
        w.DefeatShock = 1.0;
        foreach (var p in w.AllPops()) { System.Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)Ideology.Socialism] = 1.0; }
        Assert.Equal(1.0, Ideologies.FascismCrisis(w), 12);
        Assert.True(wm > 0 && col > 0 && maxInd > 0 && mil > wm, $"前提が崩れた（工場労働者の闘争 {wm}・植民地 {col}・工業化 {maxInd}）");
        double hI = c.DiscoveryNeedHalfInstitution, hD = c.DiscoveryNeedHalfIdea;
        double N(double s, double h) => Discovery.TermMax * s / (s + h);
        var t = Discovery.Evaluate(w, null, c, includeKnown: true);
        Assert.Equal(N(w.EliteCore, hI), t[Discovery.PatentRow].Need, 9);
        Assert.Equal(N(maxInd, hI), t[Discovery.JointStockRow].Need, 9);
        Assert.Equal(N(wm / pop, hD), t[Discovery.SocialismRow].Need, 9);
        Assert.Equal(N(wm / pop, hD), t[Discovery.CommunismRow].Need, 9);
        Assert.Equal(N(col / pop, hD), t[Discovery.ImperialismRow].Need, 9);
        Assert.Equal(N(mil / pop, hD), t[Discovery.FascismRow].Need, 9);
        w.DefeatShock = 0;   // 危機が無ければファシズムの必要性は0（GDP の落ち込みが無い盤面）
        if (!(w.GdpDrawdown > 0)) Assert.Equal(0.0, Discovery.Evaluate(w, null, c, includeKnown: true)[Discovery.FascismRow].Need);
    }

    /// <summary>思想の門は発見の表の思想の行の2値（旧＝世界の発展度の傾斜＝全ての国がほぼ同時に開いた・計器 (152)＝社会主義の門 0.5 が主要国の全てで1788〜93年）。
    /// 双子の盤面＝社会主義の行を立てる／立てない → 門 1／0・思想の更新の後の社会主義の支持＞0／＝0。
    /// ⚠壊し方＝門を発展度の傾斜へ戻す・門を読まずに支持を出す → 赤。</summary>
    [Fact]
    public void 思想の門は発見の2値_見つけるまで支持は0()
    {
        var w = Nation(Board(), "イギリス");   // 思想の更新は毎回はじめから＝同じ盤面で門だけを変えて2度通す
        double Share(bool known, out double gate)
        {
            w.Discovered[Discovery.SocialismRow] = known;
            gate = Ideologies.EraGates(w).Socialism;
            Ideologies.UpdateShares(w);
            double s = 0; foreach (var p in w.AllPops()) s += p.Size * p.IdeologyShares[(int)Ideology.Socialism];
            return s;
        }
        double off = Share(false, out double g0), on = Share(true, out double g1);
        Assert.Equal(0.0, g0); Assert.Equal(1.0, g1);
        Assert.Equal(0.0, off);
        Assert.True(on > 0, "社会主義を見つけた国に社会主義の支持が立たない");
    }

    /// <summary>制度は発見の2値を読むだけ（状態を持たない）＝特許・株式会社は表の行・鉄道は「機関車」の行（旧＝Institutions.Step の閾値の bool と州の倍率）。
    /// 効き＝特許は革新（GrowTech）を×PatentInnovationBonus・株式会社は工業化の投資（EconomyStep）を×JointStockInvestBonus＝双子の盤面で発見の有無だけを変える。
    /// ⚠壊し方＝特許を読まずに革新・株式会社を読まずに投資（所有国の値を読まない）→ 双子が同じ＝赤。</summary>
    [Fact]
    public void 制度は発見の2値を読む_特許は革新を株式会社は投資を速める()
    {
        var w0 = Grown().World;
        foreach (bool k in new[] { false, true })
        {
            w0.Discovered[Discovery.PatentRow] = k; w0.Discovered[Discovery.JointStockRow] = !k; w0.Discovered[Discovery.LocomotiveRow] = k;
            Assert.Equal(k, w0.PatentLaw); Assert.Equal(!k, w0.JointStock); Assert.Equal(k, w0.Railways);
        }
        double Tech(bool patent)
        {
            var w = Nation(Board(), "イギリス");
            w.Freedom = 1.0; w.EducationLaw = EducationLaw.Public;   // 革新は自由＋公教育の国だけ（FrontierCapable）
            w.Discovered[Discovery.PatentRow] = patent;
            double t0 = w.TechLevel;
            TechEducation.GrowTech(w, w.Coef);
            return w.TechLevel - t0;
        }
        double tOff = Tech(false), tOn = Tech(true);
        Assert.True(tOff > 0, "前提が崩れた（技術が伸びない）");
        Assert.True(tOn > tOff * 1.0001, $"特許を見つけた国の技術の伸びが速くならない（{tOff}→{tOn}）");
        double Ind(bool js)
        {
            var s = Grown(); var w = s.World; var r = w.Region;
            w.Discovered[Discovery.JointStockRow] = js;
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
            double i0 = r.Industrialization;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return r.Industrialization - i0;
        }
        double iOff = Ind(false), iOn = Ind(true);
        Assert.True(iOff > 0, $"前提が崩れた（工業化の投資が立たない {iOff}）");
        Assert.True(iOn > iOff * 1.0001, $"株式会社を見つけた国の工業化の投資が速くならない（{iOff}→{iOn}）");
    }

    /// <summary>思想と制度の伝播（MultiNationSim.StepDiffusion）＝制度は解禁済みの国が1で放射し、取引のある国の到達が積まれる・自国の種は自国の到達へ書き戻さない
    /// （2026-09-04 E-12＝書き戻すと国内で正帰還が閉じた）。盤面＝英だけが株式会社を見つけた世界で1四半期。
    /// ⚠壊し方＝制度の放射を0にする（呼び手＝StepDiffusion の種）・自国の種を到達へ書き戻す → 赤。</summary>
    [Fact]
    public void 制度の到達は見つけた国から取引で届き_自国へは書き戻さない()
    {
        var sim = Board();
        int js = Discovery.JointStockRow, slot = Discovery.SpreadSlot(js);
        foreach (var n in sim.Nations) { n.World.Discovered[js] = false; System.Array.Clear(n.World.SpreadReach); }
        var uk = Nation(sim, "イギリス");
        uk.Discovered[js] = true;
        Assert.Equal(1.0, Discovery.SpreadSource(uk, slot, Ideologies.National(uk)));
        sim.StepQuarter();
        Assert.Equal(1, sim.Nations.Count(n => n.World.Discovered[js]));   // 1四半期では誰も新たに見つけない（前提）
        Assert.Equal(0.0, uk.SpreadReach[slot]);   // 自国の種は書き戻さない
        int reached = sim.Nations.Count(n => n.World.SpreadReach[slot] > 0);
        Assert.True(reached > 5, $"英の株式会社が取引のある国へ届かない（届いた国 {reached}）");
    }
}
