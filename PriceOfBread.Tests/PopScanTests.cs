using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 州×POP の走査のまとめ（2026-10-15 (351)・案 A・結果を変えない）の番人。期待値は各試験の中の素の入れ子の <c>foreach</c>・旧の式の写しから作る（実装の列挙・窓を通さない）。
/// ・名前のある 2 つの集合（国家会計の集合／稼働中の州だけ）が旧の入れ子の走査と同じ列を返す。
/// ・窓（<see cref="PopWindow"/>）の中は使い回し・外は求め直し。使い回す和は求め直しと 1 ビットも違わない。
/// ・課税の走査（<see cref="Fiscal.TaxScan"/>）の並行の列は POP の一覧と同じ並びで対になる（添字合わせが構造で消えた）。
/// ・州の価格水準の 1 周の求め方は旧の 2 周の式とビット一致。払い手の保有の合計を渡す移し方は、渡さない移し方とビット一致。
/// </summary>
public class PopScanTests
{
    private static World Britain() => new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;

    [Fact]
    public void 国家会計の走査は絞らず稼働中の走査は稼働中の州だけを返す()
    {
        var w = Britain();
        Assert.True(w.Regions.Count >= 3, "前提が崩れた：州が足りない");
        w.Regions[1].State = RegionState.Locked;   // 試験の盤面の辺境と同じ状態を 1 州に作る
        var all = new List<(Region, Pop)>(); var active = new List<(Region, Pop)>();
        foreach (var r in w.Regions)
            foreach (var p in r.Pops) { all.Add((r, p)); if (r.State == RegionState.Active) active.Add((r, p)); }   // 期待値＝素の入れ子の走査
        Assert.True(active.Count < all.Count && active.Count > 0, "前提が崩れた：稼働していない州の POP が無い");
        var gotAll = new List<(Region, Pop)>(); foreach (var (r, p) in w.AllPopsWithRegion()) gotAll.Add((r, p));
        var gotActive = new List<(Region, Pop)>(); foreach (var (r, p) in w.ActivePopsWithRegion()) gotActive.Add((r, p));
        Assert.Equal(all.Count, gotAll.Count); Assert.Equal(active.Count, gotActive.Count);
        for (int i = 0; i < all.Count; i++) { Assert.Same(all[i].Item1, gotAll[i].Item1); Assert.Same(all[i].Item2, gotAll[i].Item2); }
        for (int i = 0; i < active.Count; i++) { Assert.Same(active[i].Item1, gotActive[i].Item1); Assert.Same(active[i].Item2, gotActive[i].Item2); }
        Assert.Equal(all.Select(x => x.Item2), w.AllPops());   // 旧の入口 AllPops も同じ列
    }

    [Fact]
    public void 本番の盤面では稼働中の走査と国家会計の走査は同じ列()
    {
        foreach (var cfg in new[] { NationConfig.Britain(), NationConfig.France() })
        {
            var w = new Simulation(cfg.ToScenario(41), new NullAgent()).World;
            Assert.All(w.Regions, r => Assert.Equal(RegionState.Active, r.State));
            var a = new List<Pop>(); foreach (var (_, p) in w.AllPopsWithRegion()) a.Add(p);
            var b = new List<Pop>(); foreach (var (_, p) in w.ActivePopsWithRegion()) b.Add(p);
            Assert.Equal(a, b);
            Assert.True(a.Count > 0);
        }
    }

    [Fact]
    public void 窓の外は毎回求め直し窓の中は使い回す()
    {
        var w = Britain();
        var pop = w.Regions[0].Pops[0];
        double t0 = w.TotalPopulation, s0 = w.SoldierSizeK();
        bool old = PopWindow.Verify; PopWindow.Verify = false;   // この試験は窓の前提（窓の中で POP を書かない）をわざと破って使い回しを見る＝番人（環境変数で既定が真になる）を切る
        w.PopSums = new PopWindow();
        try
        {
            Assert.Equal(t0, w.TotalPopulation);   // 窓の初回＝求めた値
            pop.Size += 5;
            Assert.Equal(t0, w.TotalPopulation);   // 窓の中＝使い回し（窓の中で POP を書かないのが前提）
            Assert.Equal(s0, w.SoldierSizeK());
        }
        finally { w.PopSums = null; PopWindow.Verify = old; }
        double expect = 0; foreach (var r in w.Regions) { double s = 0; foreach (var p in r.Pops) s += p.Size; expect += s; }   // 素の入れ子（州ごとの和を州の並びで）
        Assert.Equal(expect, w.TotalPopulation);   // 窓の外＝求め直し（書き換えた分が入る）
        Assert.Equal(t0 + 5, w.TotalPopulation, 9);
    }

    [Fact]
    public void 窓の中の和は求め直しと一致し立場の土台の写しは使い回しを壊さない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var freshBlocs = InterestGroups.Blocs(w);
            var freshBuild = PolicyPreferences.Build(w);
            w.PopSums = new PopWindow();
            try
            {
                for (int k = 0; k < 3; k++)   // 2 回目以降は使い回し。立場の土台は呼び手が正規化で書き換える＝写しを渡さないと 2 回目が割れる
                {
                    var blocs = InterestGroups.Blocs(w);
                    Assert.Equal(freshBlocs.Traditional, blocs.Traditional); Assert.Equal(freshBlocs.Modern, blocs.Modern); Assert.Equal(freshBlocs.Mass, blocs.Mass);
                    var e = PolicyPreferences.Build(w);
                    Assert.Equal(freshBuild.Wealth, e.Wealth); Assert.Equal(freshBuild.Rent, e.Rent); Assert.Equal(freshBuild.Income, e.Income);
                    Assert.Equal(freshBuild.Poll, e.Poll); Assert.Equal(freshBuild.Old, e.Old); Assert.Equal(freshBuild.Power, e.Power);
                    Assert.Equal(freshBuild.StateRevenue, e.StateRevenue);
                }
            }
            finally { w.PopSums = null; }
        }
    }

    [Fact]
    public void 番人は使い回しが求め直しと1ビットでも違えば止める()
    {
        PopWindow.Check("同じ", 1.5, 1.5);
        PopWindow.Check("同じ(NaN)", double.NaN, double.NaN);
        Assert.Throws<InvalidOperationException>(() => PopWindow.Check("違う", 1.0, System.BitConverter.Int64BitsToDouble(System.BitConverter.DoubleToInt64Bits(1.0) + 1)));
        var w = Britain();
        bool old = PopWindow.Verify; PopWindow.Verify = true;
        w.PopSums = new PopWindow();
        try
        {
            _ = w.TotalPopulation;                                 // 初回
            w.Regions[0].Pops[0].Size += 1;                        // 窓の中で POP が動いた（してはならない書き込み）
            Assert.Throws<InvalidOperationException>(() => w.TotalPopulation);
        }
        finally { w.PopSums = null; PopWindow.Verify = old; }
    }

    /// <summary>四半期首の窓は QuarterStart の中だけ。番人を立てて数四半期回しても使い回しは求め直しと一致し（窓の中で POP を書く段が無い）、窓は閉じて出てくる。</summary>
    [Fact]
    public void 四半期首の窓の中でPOPは動かず窓は閉じて出てくる()
    {
        bool old = PopWindow.Verify; PopWindow.Verify = true;
        try
        {
            // 総選挙の公示への AI の答え（議会の段の中の World.ResolveElection＝国庫→有権者の貯蓄）が窓の中に入ると、この番人が止まる（窓の位置の回帰の番人・2026-10-15 (351) の canary で実際に踏んだ）
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("プロイセン")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            int calls0 = sim.Nations.Sum(n => n.World.ElectionCalls);
            for (int q = 0; q < 12; q++)
            {
                foreach (var n in sim.Nations) n.World.QuartersSinceElection = n.World.Coef.ElectionIntervalQuarters - 2;   // 毎四半期、次の議会の段（QuartersSinceElection++ の直後に ＝間隔−1 を見る）で総選挙の公示が立つ（AI の答えが国庫→有権者の貯蓄を動かす）
                sim.StepQuarter();
                foreach (var n in sim.Nations) Assert.Null(n.World.PopSums);
            }
            Assert.True(sim.Nations.Sum(n => n.World.ElectionCalls) > calls0, "前提が崩れた：公示が立っていない");
        }
        finally { PopWindow.Verify = old; }
    }

    /// <summary>★(352) 反証役〔pu v〕低：走査の途中で州または POP の一覧の数が変わったら、黙って飛ばさず落ちる（旧の入れ子の foreach は List の版の検査で落ちた）。
    /// 期待＝例外（手で 1 件を消して作る）。</summary>
    [Fact]
    public void 走査の途中で一覧の数が変わると落ちる()
    {
        var w = Britain();
        Assert.True(w.Regions[0].Pops.Count >= 2, "前提が崩れた：州の POP が足りない");
        Assert.Throws<InvalidOperationException>(() => { foreach (var (r, _) in w.AllPopsWithRegion()) r.Pops.RemoveAt(r.Pops.Count - 1); });
        var w2 = Britain();
        Assert.True(w2.Regions.Count >= 2, "前提が崩れた：州が足りない");
        Assert.Throws<InvalidOperationException>(() => { foreach (var _ in w2.ActivePopsWithRegion()) w2.Regions.RemoveAt(w2.Regions.Count - 1); });
    }

    /// <summary>★(352) 反証役〔pu v〕低：取り立ての番人は動かす額が決まった後＝窓の中でも 0 しか動かさない呼びは止めない（他の書き込み口と同じ形）。</summary>
    [Fact]
    public void 番人は窓の中でも何も動かさない取り立てを止めない()
    {
        var w = Britain();
        var pops = w.Regions[0].Pops;
        bool old = PopWindow.Verify; PopWindow.Verify = true;
        try
        {
            w.PopSums = new PopWindow();
            Assert.Equal(0.0, Pop.Take(pops, 0.0));
            Assert.Equal(0.0, Pop.Take(pops, 1.0, new List<double>(new double[pops.Count])));
            Assert.Throws<InvalidOperationException>(() => Pop.Take(pops, 1.0));
        }
        finally { w.PopSums = null; PopWindow.Verify = old; }
    }

    /// <summary>番人（POB_POPSCAN_VERIFY）は窓の中の POP への書き込みを、読みの照合を待たずに**書いた段で**止める。窓の外では書ける・窓を閉じれば開いた数は元に戻る。
    /// （議会の段の総選挙の公示への AI の答え〔国庫→有権者の貯蓄〕を窓に入れた最初の木を、canary が軌道の変化で見つけた＝同じ形の書き込みを窓の中で見つける番人。）</summary>
    [Fact]
    public void 番人は窓の中のPOPへの書き込みを書いた段で止める()
    {
        var w = Britain();
        var pops = w.Regions[0].Pops;
        bool old = PopWindow.Verify; PopWindow.Verify = true;
        int open0 = PopWindow.Open;
        try
        {
            Assert.Equal(0.0, Pop.Distribute(pops, 1.0));                 // 窓の外は書ける
            w.PopSums = new PopWindow();
            Assert.Equal(open0 + 1, PopWindow.Open);
            Assert.Throws<InvalidOperationException>(() => Pop.Distribute(pops, 1.0));
            Assert.Throws<InvalidOperationException>(() => Pop.Take(pops, 1.0));
            Assert.Throws<InvalidOperationException>(() => w.KillPops(pops, null, 1.0, violent: true));
            Assert.Throws<InvalidOperationException>(() => pops[0].SettleMonth());
            Assert.Throws<InvalidOperationException>(() => World.Transfer((w, null), (w, pops, null), 1.0, TreasuryCat.OtherOut));
            w.PopSums = new PopWindow();                                  // 開き直しても数は増えない（開いている窓は 1 つ）
            Assert.Equal(open0 + 1, PopWindow.Open);
            w.PopSums = null;
            Assert.Equal(open0, PopWindow.Open);
            Assert.Equal(0.0, Pop.Distribute(pops, 1.0));                 // 閉じれば書ける
        }
        finally { w.PopSums = null; PopWindow.Verify = old; }
    }

    [Fact]
    public void 課税の走査の並行の列はPOPの一覧と同じ並びで対になる()
    {
        var w = Britain();
        var c = w.Coef;
        c.RuleSharesTaxBase = 1.0; c.PrivilegeTaxDiscount = new Share(0.5);
        c.TaxConsentWeight = 0.5;
        w.ColonialSuffrage = ColonialSuffrageLaw.OwnNationals; w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
        Assert.True(w.Regions.Count >= 3);
        for (int i = 0; i < w.Regions.Count; i++) w.Regions[i].Rule = [0.1 + 0.1 * i, 0.3 + 0.05 * i, 0.2, 0.0];   // 州ごとに届き方が違う
        w.Regions[1].Tier = RegionTier.Colonial;                                          // 州ごとに算入率も違う
        foreach (var p in w.Regions[1].Pops) p.CoreEthnicShare = new Share(0.25);
        foreach (var p in w.Regions[0].Pops) if (p.ClassId == PopClass.Noble) p.Size = 0;   // 州 0 には領主が居ない（居る州と居ない州が混ざる）
        Assert.Contains(w.Regions.Skip(1), r => r.Pops.Any(p => p.ClassId == PopClass.Noble && !p.IsSoldier && p.Size > 1e-9));

        var scan = Fiscal.TaxScan.Of(w, c);
        int n = w.Regions.Sum(r => r.Pops.Count);
        Assert.Equal(n, scan.Pops.Count);
        Assert.Equal(n, scan.ScopeInc!.Count); Assert.Equal(n, scan.RuleMul!.Count); Assert.Equal(n, scan.RuleCut!.Count); Assert.Equal(n, scan.RuleLord!.Count);
        int idx = 0; bool sawNoLord = false, sawLord = false;
        foreach (var r in w.Regions)
        {
            Pop? lord = null;   // 期待値＝その州の領主（最初の貴族・兵士でなく規模あり）を素の走査で探す
            foreach (var lp in r.Pops) if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9) { lord = lp; break; }
            double mul = (1.0 - 1.0) + 1.0 * (Math.Clamp(r.Rule[0], 0, 1) + Math.Clamp(r.Rule[1], 0, 1) * (1.0 - 0.5));   // 旧の式（RuleDelivery）を手で
            double cut = 1.0 * Math.Clamp(r.Rule[1], 0, 1) * (1.0 - (1.0 - 0.5));
            foreach (var p in r.Pops)
            {
                Assert.Same(p, scan.Pops[idx]);
                Assert.Equal(mul, scan.RuleMul[idx], 12);
                Assert.Equal(lord is not null ? cut : 0.0, scan.RuleCut[idx], 12);
                Assert.Same(lord, scan.RuleLord[idx]);
                Assert.Equal(InterestGroups.SuffrageInclusion(w, r, p), scan.ScopeInc[idx]);
                if (lord is null) sawNoLord = true; else sawLord = true;
                idx++;
            }
        }
        Assert.True(sawLord && sawNoLord, "前提が崩れた：領主の居る州と居ない州が揃っていない");
        Assert.True(scan.ScopeInc.Distinct().Count() > 1, "前提が崩れた：算入率が州で違わない");
        // 門が閉じていれば列は作らない
        c.RuleSharesTaxBase = 0; c.TaxConsentWeight = 0;
        var off = Fiscal.TaxScan.Of(w, c);
        Assert.Equal(n, off.Pops.Count); Assert.Null(off.ScopeInc); Assert.Null(off.RuleMul); Assert.Null(off.RuleCut); Assert.Null(off.RuleLord);
    }

    /// <summary>州の価格水準の 1 周の求め方＝旧の 2 周の式（この試験に写した）とビット一致。州ごとに貯蓄と所得の厚みを変えて、稼働していない州を挟む。</summary>
    [Fact]
    public void 州の価格水準の1周の求め方は旧の2周とビット一致()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(2);
        var w = sim.World;
        w.Coef.RegionalPriceLevelWeight = 0.3;
        Assert.True(w.Regions.Count >= 4);
        for (int i = 0; i < w.Regions.Count; i++)
            foreach (var p in w.Regions[i].Pops) { p.SetSavings(p.Savings.Value * (1.0 + 0.37 * i) + 0.1 * i); p.EarnedPerMonth = new MoneyPerMonth(p.EarnedPerMonth.Value * (1.0 + 0.21 * i) + 0.3); }
        w.Regions[2].State = RegionState.Locked;   // 稼働していない州を挟む（旧も飛ばす）
        var c = w.Coef;
        var prevPrem = w.Regions.Select(r => r.WagePremium).ToArray();
        // ── 旧の 2 周の式（写し） ──
        double natSav = 0, natEarn = 0, natPop = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active)
            foreach (var p in r.Pops) { natSav += p.Savings.Value; natEarn += p.EarnedPerMonth.Value; natPop += p.Size; }
        double natPerCap = natPop > 1e-9 ? natSav / natPop : 0, natEarnPerCap = natPop > 1e-9 ? natEarn / natPop : 0;
        var expMult = new double[w.Regions.Count]; var expPrem = new double[w.Regions.Count];
        for (int i = 0; i < w.Regions.Count; i++)
        {
            var r = w.Regions[i];
            if (r.State != RegionState.Active) { expMult[i] = 1.0; expPrem[i] = 1.0; continue; }
            double rSav = 0, rEarn = 0, rPop = 0;
            foreach (var p in r.Pops) { rSav += p.Savings.Value; rEarn += p.EarnedPerMonth.Value; rPop += p.Size; }
            double mult = 1.0;
            if (c.RegionalPriceLevelWeight > 0 && natPerCap > 1e-9 && rPop > 1e-9)
            {
                double rPerCap = rSav / rPop;
                mult = !(rPerCap > 0) ? c.RegionalPriceLevelMin : Math.Clamp(Math.Pow(rPerCap / natPerCap, c.RegionalPriceLevelWeight), c.RegionalPriceLevelMin, c.RegionalPriceLevelMax);
            }
            expMult[i] = mult;
            double prem = natEarnPerCap > 1e-9 && rPop > 1e-9 ? Math.Clamp(rEarn / rPop / natEarnPerCap, 0.25, 4.0) : 1.0;
            expPrem[i] = 0.75 * prevPrem[i] + 0.25 * prem;
        }
        sim.UpdateRegionalPriceLevels();
        for (int i = 0; i < w.Regions.Count; i++)
        {
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(expMult[i]), System.BitConverter.DoubleToInt64Bits(w.Regions[i].LocalPriceMultiplier));
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(expPrem[i]), System.BitConverter.DoubleToInt64Bits(w.Regions[i].WagePremium));
        }
        Assert.True(expMult.Distinct().Count() > 1, "前提が崩れた：州の乗数が全部同じ");
    }

    /// <summary>払い手の保有の合計を渡す移し方は、渡さない移し方（Take が自分で数える）とビット一致。保有の合計は試験の素のループで求める（同じ順・同じ max(0,·)）。</summary>
    [Fact]
    public void 払い手の保有の合計を渡す移し方は渡さない移し方とビット一致()
    {
        var a = Britain(); var b = Britain();
        var pa = new List<Pop>(); foreach (var r in a.Regions) pa.AddRange(r.Pops);
        var pb = new List<Pop>(); foreach (var r in b.Regions) pb.AddRange(r.Pops);
        for (int i = 0; i < pa.Count; i++) { double s = (i % 5 == 0) ? -3.0 : 10.0 + 7.3 * i; pa[i].SetSavings(s); pb[i].SetSavings(s); }
        double held = 0; foreach (var p in pa) held += Math.Max(0, p.Savings.Value);
        double movedA = Pop.Take(pa, held * 0.37, heldKnown: held);
        double movedB = Pop.Take(pb, held * 0.37);
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(movedB), System.BitConverter.DoubleToInt64Bits(movedA));
        Assert.True(movedA > 0);
        for (int i = 0; i < pa.Count; i++) Assert.Equal(System.BitConverter.DoubleToInt64Bits(pb[i].Savings.Value), System.BitConverter.DoubleToInt64Bits(pa[i].Savings.Value));
        // 番人を立てると、間違った合計を渡した呼び手は止まる
        bool old = PopWindow.Verify; PopWindow.Verify = true;
        try { Assert.Throws<InvalidOperationException>(() => Pop.Take(pa, 1.0, heldKnown: held * 1.5)); }
        finally { PopWindow.Verify = old; }
    }
}
