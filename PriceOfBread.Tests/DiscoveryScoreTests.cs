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
}
