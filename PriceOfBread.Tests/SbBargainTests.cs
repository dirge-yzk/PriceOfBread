using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (520) 交渉の取り分（旗 UnionBargaining）と産地の地代（旗 LandRentResidual）の番人（事前登録 measurements/sb520_20261024_prereg.txt [5]）。
/// 期待値は式の出所と別に手で作る（FranchiseVoice の値は参政権を Universal にして 1.0 と置く＝関数を呼び返さない）。
/// 双子シム（同シードの決定論で同一状態）で 1 か月だけ EconomyStep.Run を直接呼び、旗の有無で所得の分け方の差だけを見る。
/// </summary>
public class SbBargainTests
{
    private static Simulation Warm()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        sim.Run(20);
        return sim;
    }

    /// <summary>州の大衆の束の全員を結束した運動体 1 つに入れる（Size＝大衆の人口・結束 coh）＝κ＝coh。</summary>
    private static void Organize(Simulation sim, double coh, double militancy)
    {
        var w = sim.World; var r = w.Region;
        double mass = 0;
        foreach (var p in r.Pops) { if (PolicyPreferences.BlocOf(p) == 2) mass += p.Size; p.Militancy = militancy; p.UnemploymentRate = new Share(0); }
        w.Factions.Clear();
        w.Factions.Add(new FactionState { Region = r, Bloc = 2, Ideo = 0, Size = mass, AvgMilitancy = militancy, Cohesion = coh });
    }

    private static double[] Month(Simulation sim, LaborUnionLaw law, Suffrage suff, double union, double residual)
    {
        var w = sim.World; var r = w.Region;
        w.LaborUnion = law; w.Suffrage = suff;
        w.Coef.UnionBargaining = union; w.Coef.LandRentResidual = residual;
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        var e = new double[r.Pops.Count * 2];
        for (int i = 0; i < r.Pops.Count; i++) { e[2 * i] = r.Pops[i].EarnedPerMonth.Value; e[2 * i + 1] = r.Pops[i].LandRentPerMonth.Value; }
        return e;
    }

    private static (double[] Off, double[] On) Twin(LaborUnionLaw law, Suffrage suff, double coh, double mil, double union, double residual, Action<Simulation>? prep = null)
    {
        var off = Warm(); var on = Warm();
        foreach (var s in new[] { off, on }) { Organize(s, coh, mil); prep?.Invoke(s); }
        var a = Month(off, law, suff, 0, 0);
        var b = Month(on, law, suff, union, residual);
        return (a, b);
    }

    private static double Total(double[] e) { double t = 0; for (int i = 0; i < e.Length; i += 2) t += e[i]; return t; }

    [Fact]
    public void 禁止の国では運動体が強くても所得の分け方は旗0と全く同じ()   // U1
    {
        var (off, on) = Twin(LaborUnionLaw.Illegal, Suffrage.Universal, 1.0, 0.5, 1, 0);
        Assert.Equal(off.Length, on.Length);
        for (int i = 0; i < off.Length; i++) Assert.Equal(BitConverter.DoubleToInt64Bits(off[i]), BitConverter.DoubleToInt64Bits(on[i]));
    }

    [Fact]
    public void 容認の国では持ち主の取り分が減って労働者へ移り所得の合計は変わらない()   // U2・U3
    {
        var (off, on) = Twin(LaborUnionLaw.Tolerated, Suffrage.Universal, 1.0, 0.5, 1, 0);
        var w = Warm().World;
        int lp = -1, op = -1;
        foreach (var p in w.Region.Producers) if (p.BuysLabor && !p.IsRgo) { lp = p.LaborPopIndex; op = p.OwnerPopIndex; break; }
        Assert.True(lp >= 0 && op >= 0 && lp != op, "前提が崩れた：工場の担い手と持ち主が見つからない");
        double gain = on[2 * lp] - off[2 * lp], loss = off[2 * op] - on[2 * op];
        Assert.True(gain > 0, $"門は緑だが働かない：容認の国で労働者の所得が増えない（{gain}）");
        Assert.True(loss > 0, $"持ち主の所得が減らない（{loss}）");
        Assert.Equal(Total(off), Total(on), 6);   // 保存：所得の合計は不変（相対 1e-9 に相当する桁）
        Assert.True(Math.Abs(gain - loss) <= 1e-9 * Math.Max(1.0, Math.Abs(gain)), $"労働者の増え {gain} と持ち主の減り {loss} が一致しない");
    }

    [Fact]
    public void 統制は罷業の力が無く票があるときだけ正で容認より小さい()   // U5
    {
        var (offC0, onC0) = Twin(LaborUnionLaw.Controlled, Suffrage.None, 1.0, 0.5, 1, 0);
        Assert.Equal(Total(offC0), Total(onC0), 6);
        // 票が無い統制の国：V_w＝0（罷業の力は容認だけ）→ β＝0 → 所得は旗 0 と同じ
        for (int i = 0; i < offC0.Length; i++) Assert.Equal(BitConverter.DoubleToInt64Bits(offC0[i]), BitConverter.DoubleToInt64Bits(onC0[i]));
        var (offC, onC) = Twin(LaborUnionLaw.Controlled, Suffrage.Universal, 1.0, 0.5, 1, 0);
        var (offT, onT) = Twin(LaborUnionLaw.Tolerated, Suffrage.Universal, 1.0, 0.5, 1, 0);
        var w = Warm().World;
        int lp = -1;
        foreach (var p in w.Region.Producers) if (p.BuysLabor && !p.IsRgo) { lp = p.LaborPopIndex; break; }
        double gC = onC[2 * lp] - offC[2 * lp], gT = onT[2 * lp] - offT[2 * lp];
        Assert.True(gC > 0, $"票のある統制の国で上乗せが無い（{gC}）");
        Assert.True(gT > gC, $"容認の上乗せ {gT} が統制 {gC} より大きくない（罷業の力の差）");
    }

    [Fact]
    public void 組合の産出の乗数は旗が正のとき1_0で罷業の乗数だけが残る()   // U6
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        w.Coef.StrikeProductionHit = 0;   // 罷業の乗数を切って組合の乗数だけを見る
        w.LaborUnion = LaborUnionLaw.Illegal; w.Coef.UnionBargaining = 0;
        double ill = w.TechProductivity(r);
        w.LaborUnion = LaborUnionLaw.Tolerated;
        double tolOff = w.TechProductivity(r);
        Assert.Equal(ill * w.Coef.LaborUnionToleratedOutputMul, tolOff, 12);   // 旗 0＝今の乗数
        w.Coef.UnionBargaining = 1;
        double tolOn = w.TechProductivity(r);
        Assert.Equal(ill, tolOn, 12);                                           // 旗 1＝乗数なし
        w.LaborUnion = LaborUnionLaw.Controlled;
        Assert.Equal(ill, w.TechProductivity(r), 12);
        // 罷業の乗数は残る（旗 1 でも消えない）
        w.Coef.StrikeProductionHit = 0.5; w.Coef.StrikeMilitancyThreshold = 0.0;
        foreach (var p in r.Pops) p.Militancy = 0.8;
        w.LaborUnion = LaborUnionLaw.Tolerated;
        Assert.True(w.TechProductivity(r) < ill - 1e-9, "旗 1 で罷業の乗数まで消えている");
    }

    [Fact]
    public void ベータの式は手で作った値と一致し平均以下の工場には上乗せが無い()
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        w.LaborUnion = LaborUnionLaw.Tolerated; w.Suffrage = Suffrage.Universal;   // 票の声は 1.0（Universal では女性比の欠けが無い）
        int lp = -1, op = -1;
        foreach (var p in r.Producers) if (p.BuysLabor && !p.IsRgo) { lp = p.LaborPopIndex; op = p.OwnerPopIndex; break; }
        r.Pops[lp].Militancy = 0.3; r.Pops[lp].UnemploymentRate = new Share(0.2);
        // 手計算：V_w＝1.0・V_o＝1.0（持ち主が近代エリートでも大衆でも旧支配層でも Universal では 1.0）・S票＝0.5・m＝0.3（容認）→ S＝1−(1−0.5)(1−0.3)＝0.65・κ＝0.5・U＝0.2
        double beta = 0.5 * (1 - 0.2) * 0.65;
        var f = new Producer { BuysLabor = true, IsRgo = false, LaborPopIndex = lp, OwnerPopIndex = op, HeadsLastMonth = 10 };
        var c = new UnionBargaining.Ctx { LastLp = -1, LastOp = -1, Kappa = 0.5, AvgVaPerHead = 4, Strike = true, Any = true };
        var o = UnionBargaining.Transfer(ref c, w, r, f, 0, 100, 1000);   // a＝10・ā＝4・超過＝6・頭数 10
        Assert.True(o.Eligible);
        Assert.Equal(beta, o.Beta, 12);
        Assert.Equal(beta * 6 * 10, o.Transfer, 9);
        // 平均以下の工場：上乗せ 0
        var o2 = UnionBargaining.Transfer(ref c, w, r, f, 0, 30, 1000);   // a＝3＜ā
        Assert.True(o2.Eligible); Assert.Equal(0.0, o2.Transfer);
        // 守り：取り分を超えない
        var o3 = UnionBargaining.Transfer(ref c, w, r, f, 0, 100, 1.0);
        Assert.Equal(1.0, o3.Transfer, 12); Assert.True(o3.Guarded);
        // 眠った工場・付加価値が負の工場・頭数が無い工場は適格でない
        Assert.False(UnionBargaining.Transfer(ref c, w, r, f, 1, 100, 1000).Eligible);
        Assert.False(UnionBargaining.Transfer(ref c, w, r, f, 0, -5, 1000).Eligible);
        f.HeadsLastMonth = 0;
        Assert.False(UnionBargaining.Transfer(ref c, w, r, f, 0, 100, 1000).Eligible);
    }

    [Fact]
    public void 前走査の平均は頭数加重で眠った生産者と赤字の工場を除く()
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        int np = r.Producers.Count;
        var sleep = new byte[np]; Array.Fill(sleep, (byte)1);
        var produced = new double[Catalog.Goods.Length]; var revenue = new double[Catalog.Goods.Length]; var cnt = new int[Catalog.Goods.Length]; var inCost = new double[np];
        var picks = new List<int>(); var seen = new HashSet<int>();
        for (int i = 0; i < np && picks.Count < 4; i++)
        {
            var p = r.Producers[i];
            if (p.BuysLabor && !p.IsRgo && seen.Add(p.GoodId)) picks.Add(i);
        }
        Assert.True(picks.Count == 4, "前提が崩れた：財の違う工場が 4 つ無い");
        // 工場 0：売上 100・投入 20・頭数 4（付加価値 80）／工場 1：売上 60・投入 10・頭数 1（付加価値 50）／工場 2：赤字（付加価値 −5）／工場 3：眠っている（付加価値 900・頭数 9）
        double[] rev = [100, 60, 10, 1000], ic = [20, 10, 15, 100], hd = [4, 1, 2, 9];
        for (int k = 0; k < 4; k++)
        {
            var p = r.Producers[picks[k]]; int gi = p.GoodId;
            p.Output = 1.0; p.HeadsLastMonth = hd[k]; produced[gi] = 1.0; cnt[gi] = 1; revenue[gi] = rev[k]; inCost[picks[k]] = ic[k];
            sleep[picks[k]] = (byte)(k == 3 ? 1 : 0);
        }
        var c = UnionBargaining.Prepare(w, r, np, sleep, produced, cnt, revenue, inCost);
        Assert.True(c.Any);
        Assert.Equal((80.0 + 50.0) / (4.0 + 1.0), c.AvgVaPerHead, 12);   // 頭数加重＝Σ付加価値÷Σ頭数（単純平均 (20+50)/2 = 35 ではない）
    }

    [Fact]
    public void 大衆に票が付くとベータは上がる向き()   // U4（Census→AdultMale＝持ち主の声が既に 1 の段）
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        w.LaborUnion = LaborUnionLaw.Controlled;   // 罷業の力を除いて票の声だけを見る
        int lp = -1, op = -1;
        foreach (var p in r.Producers) if (p.BuysLabor && !p.IsRgo) { lp = p.LaborPopIndex; op = p.OwnerPopIndex; break; }
        var f = new Producer { BuysLabor = true, IsRgo = false, LaborPopIndex = lp, OwnerPopIndex = op, HeadsLastMonth = 10 };
        w.Suffrage = Suffrage.Census;
        var c1 = new UnionBargaining.Ctx { LastLp = -1, LastOp = -1, Kappa = 0.5, AvgVaPerHead = 4, Strike = false, Any = true };
        double b1 = UnionBargaining.Transfer(ref c1, w, r, f, 0, 100, 1000).Beta;
        w.Suffrage = Suffrage.AdultMale;
        var c2 = new UnionBargaining.Ctx { LastLp = -1, LastOp = -1, Kappa = 0.5, AvgVaPerHead = 4, Strike = false, Any = true };
        double b2 = UnionBargaining.Transfer(ref c2, w, r, f, 0, 100, 1000).Beta;
        Assert.True(b2 > b1, $"大衆に票が付いても β が上がらない（{b1} → {b2}）");
    }

    [Fact]
    public void 参政権が上がってもSは下がらず票の無い容認ではSは闘争度そのもの()   // U4（追記 3 ⑴：和の形だと AdultMale→Universal で下がった）
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        int lp = -1, op = -1;
        foreach (var p in r.Producers) if (p.BuysLabor && !p.IsRgo) { lp = p.LaborPopIndex; op = p.OwnerPopIndex; break; }
        Assert.True(lp >= 0 && op >= 0 && lp != op);
        foreach (double mil in new[] { 0.0, 0.3, 0.8 })
        {
            r.Pops[lp].Militancy = mil; r.Pops[lp].UnemploymentRate = new Share(0);
            var f = new Producer { BuysLabor = true, IsRgo = false, LaborPopIndex = lp, OwnerPopIndex = op, HeadsLastMonth = 10 };
            double prev = -1;
            foreach (var s in new[] { Suffrage.None, Suffrage.Census, Suffrage.AdultMale, Suffrage.Universal })
            {
                w.Suffrage = s;
                var c = new UnionBargaining.Ctx { LastLp = -1, LastOp = -1, Kappa = 0.5, AvgVaPerHead = 4, Strike = true, Any = true };
                double sNow = UnionBargaining.Transfer(ref c, w, r, f, 0, 100, 1000).S;
                Assert.True(sNow >= prev - 1e-12, $"闘争度 {mil}：参政権 {s} で S が下がった（{prev} → {sNow}）");
                if (s == Suffrage.None) Assert.Equal(mil, sNow, 12);   // 票の無い容認：S＝m（担い手に票が無く S票＝0）
                prev = sNow;
            }
        }
    }

    [Fact]
    public void 運動体が無い州は組織された割合が0でベータも0()
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        w.Factions.Clear();
        Assert.Equal(0.0, UnionBargaining.Kappa(w, r));
        Organize(sim, 0.5, 0.3);
        double mass = 0; foreach (var p in r.Pops) if (PolicyPreferences.BlocOf(p) == 2) mass += p.Size;
        Assert.Equal(0.5, UnionBargaining.Kappa(w, r), 12);   // Size＝大衆の全員・結束 0.5
        // 別の束の運動体は数えない
        w.Factions[0].Bloc = 1;
        Assert.Equal(0.0, UnionBargaining.Kappa(w, r));
        Assert.True(mass > 0);
    }

    // ---------------------------------------------------------------- 段1c

    [Fact]
    public void 産地の取り分は旗が正のとき法の上限になり工場は変わらない()   // R3
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        r.FarmRentCap = new Share(0.4);
        Producer? rgo = null, fac = null;
        foreach (var p in r.Producers) { if (p.IsRgo && rgo is null) rgo = p; if (p.BuysLabor && !p.IsRgo && fac is null) fac = p; }
        Assert.True(rgo is not null && fac is not null);
        rgo!.RentShare = new Share(0.3);
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        w.Coef.LandRentResidual = 0; w.Coef.RentScarcityElasticity = 0;
        double facOff = EconomyStep.RentShareOf(fac!, r, w.Coef, rural);
        Assert.Equal(0.3, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);   // 旗 0 は今の式（生産者の固定の取り分 0.3・上限 0.4 の内側）
        w.Coef.LandRentResidual = 1;
        Assert.Equal(0.4, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        Assert.Equal(facOff, EconomyStep.RentShareOf(fac!, r, w.Coef, rural), 12);   // 上限 0.4 の州でも工場の取り分は旗に依らない（産地だけが上限になる）
        r.FarmRentCap = new Share(1.0);
        Assert.Equal(1.0, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        Assert.Equal(facOff, EconomyStep.RentShareOf(fac!, r, w.Coef, rural), 12);
        // AI の土地改革の余地（SelfPlay）は旗に依らず旧のまま＝生産者の RentShare と法の上限の差（追記 3 ⑵）。旗を倒しても生産者の RentShare は書き換わらない
        Assert.Equal(0.3, rgo.RentShare.Value, 12);
    }

    [Fact]
    public void 自作農の所得は変わらず地代の記録だけが増える()   // R1
    {
        var (off, on) = Twin(LaborUnionLaw.Illegal, Suffrage.None, 0.0, 0.0, 0, 1, s =>
        {
            foreach (var p in s.World.Region.Producers) if (p.IsRgo) { p.RentShare = new Share(0.3); p.OwnerPopIndex = p.LaborPopIndex; }
        });
        int n = off.Length / 2;
        double rentOff = 0, rentOn = 0;
        for (int i = 0; i < n; i++)
        {
            Assert.True(Math.Abs(off[2 * i] - on[2 * i]) <= 1e-9 * Math.Max(1.0, Math.Abs(off[2 * i])), $"自作農の州で POP {i} の所得が変わった（{off[2 * i]} → {on[2 * i]}）");
            rentOff += off[2 * i + 1]; rentOn += on[2 * i + 1];
        }
        Assert.True(rentOn > rentOff, $"自作農の自己地代（土地税の課税の元）が増えない（{rentOff} → {rentOn}）");
    }

    [Fact]
    public void 領主と農民の州では地代が増え農民の所得が下がり合計は変わらない()   // R2
    {
        var (off, on) = Twin(LaborUnionLaw.Illegal, Suffrage.None, 0.0, 0.0, 0, 1, s =>
        {
            foreach (var p in s.World.Region.Producers) if (p.IsRgo) { p.RentShare = new Share(0.5); p.OwnerPopIndex = 0; }
            s.World.Region.FarmRentCap = new Share(1.0);
        });
        Assert.Equal(Total(off), Total(on), 6);
        Assert.True(on[1] > off[1], $"領主の LandRent が増えない（{off[1]} → {on[1]}）");
        Assert.True(on[0] > off[0], "領主の所得が増えない");
    }

    // ---------------------------------------------------------------- (529) 段1c の形 P（旗 LandRentCustomBound）

    private static double[] MonthBound(Simulation sim, double bound)
    {
        var w = sim.World; var r = w.Region;
        w.LaborUnion = LaborUnionLaw.Illegal; w.Suffrage = Suffrage.None;
        w.Coef.UnionBargaining = 0; w.Coef.LandRentResidual = 1; w.Coef.LandRentCustomBound = bound;
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        var e = new double[r.Pops.Count * 2];
        for (int i = 0; i < r.Pops.Count; i++) { e[2 * i] = r.Pops[i].EarnedPerMonth.Value; e[2 * i + 1] = r.Pops[i].LandRentPerMonth.Value; }
        return e;
    }

    private static (double[] Off, double[] On, int LaborIdx) TwinBound(double rentShare, double cap, bool landlord)
    {
        var off = Warm(); var on = Warm();
        foreach (var s in new[] { off, on })
        {
            Organize(s, 0.0, 0.0);
            foreach (var p in s.World.Region.Producers) if (p.IsRgo) { p.RentShare = new Share(rentShare); p.OwnerPopIndex = landlord ? 0 : p.LaborPopIndex; }
            s.World.Region.FarmRentCap = new Share(cap);
        }
        int laborIdx = -1; foreach (var p in on.World.Region.Producers) if (p.IsRgo) { laborIdx = p.LaborPopIndex; break; }
        return (MonthBound(off, 0), MonthBound(on, 1), laborIdx);
    }

    [Fact]
    public void 形Pの産地の取り分は慣習の取り分と法の上限の小さい方で旗0は法の上限のまま()   // M1・M2
    {
        var sim = Warm(); var w = sim.World; var r = w.Region;
        Producer? rgo = null;
        foreach (var p in r.Producers) if (p.IsRgo) { rgo = p; break; }
        Assert.True(rgo is not null);
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        w.Coef.LandRentResidual = 1;
        rgo!.RentShare = new Share(0.5);
        r.FarmRentCap = new Share(0.75);                                  // 農奴解放が限定的の国（日・墺・仏）：上限が慣習より高い
        w.Coef.LandRentCustomBound = 0;
        Assert.Equal(0.75, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        w.Coef.LandRentCustomBound = 1;
        Assert.Equal(0.5, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        r.FarmRentCap = new Share(0.3);                                   // 土地改革が全面：上限が慣習より低い＝今の形と同じ
        Assert.Equal(0.3, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        w.Coef.LandRentCustomBound = 0;
        Assert.Equal(0.3, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        rgo.RentShare = new Share(0.85);                                  // ロシア（農奴の現金の床 0.15 を引いた慣習）と未解放（上限 1.0）
        r.FarmRentCap = new Share(1.0);
        w.Coef.LandRentCustomBound = 1;
        Assert.Equal(0.85, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
        w.Coef.LandRentResidual = 0; w.Coef.RentScarcityElasticity = 0;   // 残りの道を倒せば 形 P は読まれない（旗 0 の今の式）
        Assert.Equal(0.85, EconomyStep.RentShareOf(rgo, r, w.Coef, rural), 12);
    }

    [Fact]
    public void 形Pは上限が慣習より高い領主と農民の州で農民の所得が増え領主の所得が減り合計は変わらない()   // M3
    {
        var (off, on, laborIdx) = TwinBound(0.5, 1.0, landlord: true);
        Assert.Equal(Total(off), Total(on), 6);
        Assert.True(on[1] < off[1], $"領主の LandRent が減らない（{off[1]} → {on[1]}）");
        Assert.True(on[0] < off[0], "領主の所得が減らない");
        // 耕す人の POP（産地の LaborPopIndex）の所得の増え＝領主の地代の減り（rent+wages=付加価値の保存・手の値＝(1.0−0.5)×付加価値が両方に出る）。他の階級の所得の増えに紛れない（反証役 pf2 低#6）
        Assert.True(laborIdx >= 0 && laborIdx < off.Length / 2, "産地の耕す人の POP が見つからない");
        double farmerGain = on[2 * laborIdx] - off[2 * laborIdx], landlordLoss = off[1] - on[1];
        Assert.True(farmerGain > 0, $"耕す人の所得が増えない（{off[2 * laborIdx]} → {on[2 * laborIdx]}）");
        Assert.True(Math.Abs(farmerGain - landlordLoss) <= 1e-6 * Math.Max(1.0, landlordLoss), $"耕す人の増え {farmerGain} が領主の地代の減り {landlordLoss} と合わない");
    }

    [Fact]
    public void 形Pは自作農の所得を変えず土地税の課税の元だけが慣習の取り分へ戻る()   // M3
    {
        var (off, on, _) = TwinBound(0.3, 1.0, landlord: false);
        int n = off.Length / 2;
        double rentOff = 0, rentOn = 0;
        for (int i = 0; i < n; i++)
        {
            Assert.True(Math.Abs(off[2 * i] - on[2 * i]) <= 1e-9 * Math.Max(1.0, Math.Abs(off[2 * i])), $"自作農の州で POP {i} の所得が変わった（{off[2 * i]} → {on[2 * i]}）");
            rentOff += off[2 * i + 1]; rentOn += on[2 * i + 1];
        }
        Assert.True(rentOn < rentOff, $"自作農の自己地代（土地税の課税の元）が慣習へ戻らない（{rentOff} → {rentOn}）");
    }
}
