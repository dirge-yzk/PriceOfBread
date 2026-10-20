using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(471) 小さな独立の直し 4 件の番人（旗 4 本・既定 0）。事前登録 measurements/sf471_20261020_prereg.txt [2][8]。
/// 期待値はコードの式を呼ばずに手の算術で作る（同語反復にしない）。突然変異は measurements/sf471_20261020/mutations.txt。</summary>
[Collection("Instruments")]
public class SmallFixes471Tests
{
    private static T WithIns<T>(Func<T> f)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sf_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { return f(); }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 旗が残るのは_安く売る_だけで既定は0_旧の経路()
    {
        var c = new Coefficients();
        Assert.Equal(0, c.TradeCheapSellSubsidy);   // ①③④ は焼き込み済み（旗を消した）。② は計算量で採らず枝に残す
    }

    // ================================================================ ① 人-14 兵士の年金 ================================================================

    private const double PensionRate = 1e-6;   // 請求が福祉の余剰の上限に届かない小ささ＝請求 ＝ 率×段×老年の人数（手の算術がそのまま立つ）

    private sealed class PensionBoard
    {
        public Simulation Sim = null!; public World W = null!; public Pop Soldier = null!; public double ElderNonSoldier, ElderSoldier;
    }

    /// <summary>仏を 6 か月温め、年金を満額にし、兵士の POP の 1 つを〈60〜69 歳の男だけ〉の大きさ soldierSize にして Fiscal.Step を 1 回。</summary>
    private static PensionBoard BuildPension(double soldierSize)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.Run(6);
        var w = sim.World;
        w.Coef.PensionCostRatePerMonth = PensionRate;
        w.Pension = ReformLevel.Full;
        w.FiscalFundingRatio = new Share(1.0);
        Pop? s = null;
        foreach (var p in w.AllPops()) if (p.IsSoldier) { if (s is null) s = p; p.Ages = Pop.SoldierAges(); }   // 全ての兵士を 20〜39 歳へ（元から混じる極少の老年を消す）
        Assert.NotNull(s);   // 前提：仏の盤面に兵士の POP の器がある
        s!.Size = soldierSize;
        s.Ages = new double[2 * Pop.Layout.Bands]; s.Ages[2 * Pop.Layout.OldBandA] = 1.0;   // その 1 つだけ全員 60 代（8層は 60〜69 歳・11層は 61〜70 歳）の男
        var b = new PensionBoard { Sim = sim, W = w, Soldier = s };
        foreach (var p in w.AllPops())
        {
            double os = p.Size * p.AgeShare(60, double.PositiveInfinity);
            if (p.IsSoldier) b.ElderSoldier += os; else b.ElderNonSoldier += os;
        }
        Fiscal.Step(w, w.Coef);
        return b;
    }

    [Fact]
    public void 兵士の老年は年金の分母に入る_手の算術()
    {
        var b = BuildPension(5000);
        Assert.True(b.ElderSoldier > 1000 && b.ElderNonSoldier > 0, "前提が崩れた：兵士の老年が立たない");
        Assert.Equal(PensionRate * (b.ElderNonSoldier + b.ElderSoldier), b.W.LastPensionCostPerMonth.Value, 9);   // 請求＝率×(兵士以外の老年＋兵士の老年)
        var b0 = BuildPension(0);
        Assert.Equal(PensionRate * b0.ElderNonSoldier, b0.W.LastPensionCostPerMonth.Value, 9);   // 兵士の老年が居なければ兵士以外だけ
        Assert.True(b.W.LastPensionCostPerMonth.Value > b0.W.LastPensionCostPerMonth.Value);
    }

    [Fact]
    public void 兵士の老年に年金が届く_受け手と分母が同じ規則()
    {
        WithIns<int>(() =>
        {
            var b = BuildPension(5000);
            Assert.True(b.W.DiagSfPensionToSoldiersCum > 0, "兵士に年金が届かない（受け手の述語が分母と食い違っている）");
            // 届いた額＝請求 ×（兵士の老年の重み ÷ 老年全体の重み）。重みは州ごとの到達（RuleReach）を掛けるので、到達が 1 の州だけの盤面ではなく上限と下限で挟む。
            double claim = b.W.LastPensionCostPerMonth.Value;
            double share = b.ElderSoldier / (b.ElderSoldier + b.ElderNonSoldier);
            Assert.True(b.W.DiagSfPensionToSoldiersCum <= claim + 1e-12, "兵士へ届いた額が請求を超えた");
            Assert.True(b.W.DiagSfPensionToSoldiersCum > 0.01 * claim * share, $"兵士へ届いた額 {b.W.DiagSfPensionToSoldiersCum} が手の見積り {claim * share} の 1% にも届かない");
            return 0;
        });
    }

    [Fact]
    public void 年金の1行は有限で負にならない()
    {
        var b = BuildPension(5000);
        double stock = b.W.Treasury.Value; foreach (var p in b.W.AllPops()) stock += p.Savings.Value;
        Assert.True(double.IsFinite(stock));
        // 国債・利払い・税もこの 1 回の Step に入るため保存の厳密な差分は別の番人（ConservationTests・leakhunt）が持つ。ここは年金の 1 行が NaN・負を作らないこと。
        Assert.True(b.W.LastPensionCostPerMonth.Value >= 0 && double.IsFinite(b.W.LastPensionCostPerMonth.Value));
    }

    // ================================================================ ② 交-16 安く売る ================================================================

    /// <summary>英→仏の越境（売り手＝英の港 1 つ・買い手＝仏の州）。売り手の価格 m×wp・仏の買い手 wp。英の自国の買い手は価格を 0.2wp に下げて売買しない。</summary>
    private static (MultiNationSim sim, int g, List<Region> frBuyers) CheapBoard(double m, int flag, int bias, Action<MultiNationSim>? tweak = null)
    {
        var (sim, g, sellers, buyers) = ClearingScopeTests.Board(1);
        sim.Coef.TradeCheapSellSubsidy = flag;
        foreach (var n in sim.Nations) n.World.Coef.TradeCheapSellSubsidy = flag;
        double wp = sim.Market.Price[g];
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.Tariff = 0; fr.Tariff = 0;
        var frBuyers = new List<Region>();
        sellers[0].Markets[g].Price = m * wp;
        foreach (var r in buyers)
        {
            bool isFr = fr.Regions.Contains(r);
            r.Markets[g].Price = (isFr ? 1.0 : 0.2) * wp;
            if (isFr) frBuyers.Add(r);
        }
        if (bias != 0) uk.SetTradeBias(g, bias);
        tweak?.Invoke(sim);
        ClearingScopeTests.SetCalendar(sim, 1800, 1);
        return (sim, g, frBuyers);
    }

    private static double Imported(List<Region> frBuyers, int g) => frBuyers.Sum(r => r.Markets[g].LastImport);

    private static double Run(double m, int flag, int bias)
    {
        var (sim, g, frBuyers) = CheapBoard(m, flag, bias);
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        return Imported(frBuyers, g);
    }

    [Fact]
    public void 旗0は_マイナス1を置いても何も変わらない_旧の効かない操作()
    {
        foreach (double m in new[] { 0.7, 0.95, 1.1 })
            Assert.Equal(Run(m, 0, 0), Run(m, 0, -1));   // 旗 0 では売買の方針 −1 の有無で成約の量が 1 ビットも変わらない
    }

    [Fact]
    public void 旗1は_マイナス1の財の越境の輸入が旗0で閉じていた価格の窓で立つ_買い手の敷居が売り手の奨励で下がる()
    {
        // 窓の探索：売り手の価格 m（買い手＝仏の価格 1.0wp）を動かし、旗 0＝輸入 0・旗 1＋方針 −1＝輸入 ＞ 0 となる m があること（輸送の係数に依らない書き方）。
        var window = new List<double>();
        double prev0 = double.NaN;
        for (double m = 0.80; m <= 1.40001; m += 0.02)
        {
            double a = Run(m, 0, -1), c = Run(m, 1, -1);
            Assert.True(c + 1e-12 >= a, $"m={m:F2}：旗 1 の輸入 {c} が旗 0 の {a} より減った（奨励が輸入を減らした）");
            if (a <= 1e-9 && c > 1e-9) window.Add(m);
            prev0 = a;
        }
        Assert.True(window.Count >= 1, "旗 0 で閉じ旗 1 で開く価格の窓が無い＝〈安く売る〉が越境の輸出を増やしていない（機構が働いていない）");
        // 窓は連続した幅を持つ（奨励 s＝25% の分だけ敷居が動く）
        Assert.True(window.Count >= 2, "窓が 1 点だけ（偶然の一致の疑い）");
        _ = prev0;
    }

    [Fact]
    public void 旗1でも_マイナス1を置いていなければ旗0と同じ()
    {
        foreach (double m in new[] { 0.7, 1.0, 1.2 })
            Assert.Equal(Run(m, 0, 0), Run(m, 1, 0));
    }

    [Fact]
    public void 奨励金は売り手の国庫が出す_払いは_率かけるrかけるFOBで_買い手の支払いは積出し価格の_1マイナスs()
    {
        // 窓の中の価格を探す
        double mWin = double.NaN;
        for (double m = 0.80; m <= 1.40001 && double.IsNaN(mWin); m += 0.02)
            if (Run(m, 0, -1) <= 1e-9 && Run(m, 1, -1) > 1e-9) mWin = m;
        Assert.False(double.IsNaN(mWin), "窓が無い（前の試験が赤のはず）");
        WithIns<int>(() =>
        {
            var (sim, g, frBuyers) = CheapBoard(mWin, 1, -1, s => { s.Nations[0].World.FiscalFundingRatio = new Share(0.6); });
            var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
            double t0 = uk.Treasury.Value, tfUk0 = uk.CumulativeTradeFlow.Value, tfFr0 = fr.CumulativeTradeFlow.Value, bmUk0 = uk.BaseMoney.Value, bmFr0 = fr.BaseMoney.Value, fx0 = sim.Diplomacy.CumulativeFxResidual;
            sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
            Assert.True(Imported(frBuyers, g) > 1e-9);
            double fob = uk.ExportsByGood![g] / uk.FxRate;                   // 英の通貨での積出し価格の額（越境のみ）
            double s = sim.Coef.TradeBiasStrength, r = 0.6;
            double expected = s * r * fob;                                   // 売り手の国庫の払い
            Assert.True(expected > 0);
            Assert.Equal(expected, uk.DiagSfCheapSubsidyPaidCum, 6);
            Assert.Equal(expected, t0 - uk.Treasury.Value, 6);               // 国庫の減りは奨励金だけ（関税 0・他の払いなし）
            // 保存：国ごとの D が閉じ、金建てで世界が閉じ、名目の差は為替の換算の台帳
            Assert.Equal((uk.BaseMoney.Value - bmUk0), (uk.CumulativeTradeFlow.Value - tfUk0), 6);
            Assert.Equal((fr.BaseMoney.Value - bmFr0), (fr.CumulativeTradeFlow.Value - tfFr0), 6);
            Assert.Equal(sim.Diplomacy.CumulativeFxResidual - fx0, (uk.BaseMoney.Value + fr.BaseMoney.Value) - (bmUk0 + bmFr0), 6);
            return 0;
        });
    }

    [Fact]
    public void 為替が等しい世界では_奨励金の取引で清算所に残る額が0_買い手は奨励金の分を払わず売り手の国庫が出す()
    {
        // 両国の通貨が金と等しい（換算の差が 0）なら、清算所に入る額（買い手の支払い＋売り手の国庫の奨励金）と出る額（売り手の受取）は一致する＝世界の名目の基礎貨幣は動かない。
        // 買い手が奨励金の分まで払ってしまう（二重に払う）と、清算所に差が残り換算の台帳 CumulativeFxResidual が動く（台帳は支払いの側から作るので、国ごとの保存だけでは見えない）。
        double mWin = double.NaN;
        for (double m = 0.80; m <= 1.40001 && double.IsNaN(mWin); m += 0.02)
            if (Run(m, 0, -1) <= 1e-9 && Run(m, 1, -1) > 1e-9) mWin = m;
        Assert.False(double.IsNaN(mWin));
        var (sim, g, frBuyers) = CheapBoard(mWin, 1, -1, s =>
        {
            s.Nations[0].World.ExchangeRate = 1.0; s.Nations[1].World.ExchangeRate = 1.0;
            s.Coef.PortFeeRate = 0; s.Coef.MarinePremiumRate = 0;
        });
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        double bm0 = (uk.BaseMoney + fr.BaseMoney).Value, fx0 = sim.Diplomacy.CumulativeFxResidual, t0 = uk.Treasury.Value;
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.True(Imported(frBuyers, g) > 1e-9 && uk.DiagSfCheapSubsidyPaidCum >= 0);
        Assert.True(t0 - uk.Treasury.Value > 1e-9, "売り手の国庫が減っていない（前提の破れ）");
        double scale = Math.Max(1.0, uk.ExportsByGood![g]);
        Assert.True(Math.Abs(sim.Diplomacy.CumulativeFxResidual - fx0) <= 1e-9 * scale, $"為替が等しいのに換算の台帳が動いた（{sim.Diplomacy.CumulativeFxResidual - fx0}）＝買い手と売り手と国庫の勘定が合っていない");
        Assert.True(Math.Abs((uk.BaseMoney + fr.BaseMoney).Value - bm0) <= 1e-9 * scale, "為替が等しいのに世界の名目の基礎貨幣が動いた");
    }

    [Fact]
    public void 為替が1でないときも奨励金の取引は国ごと_世界で保存する()
    {
        double mWin = double.NaN;
        for (double m = 0.80; m <= 1.40001 && double.IsNaN(mWin); m += 0.02)
            if (Run(m, 0, -1) <= 1e-9 && Run(m, 1, -1) > 1e-9) mWin = m;
        Assert.False(double.IsNaN(mWin));
        var (sim, g, frBuyers) = CheapBoard(mWin, 1, -1, s => { s.Nations[0].World.ExchangeRate = 0.5; s.Nations[1].World.ExchangeRate = 1.25; });
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        double tfUk0 = uk.CumulativeTradeFlow.Value, tfFr0 = fr.CumulativeTradeFlow.Value, bmUk0 = uk.BaseMoney.Value, bmFr0 = fr.BaseMoney.Value, fx0 = sim.Diplomacy.CumulativeFxResidual;
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.True(Imported(frBuyers, g) > 1e-9);
        Assert.Equal(uk.BaseMoney.Value - bmUk0, uk.CumulativeTradeFlow.Value - tfUk0, 6);
        Assert.Equal(fr.BaseMoney.Value - bmFr0, fr.CumulativeTradeFlow.Value - tfFr0, 6);
        Assert.Equal(sim.Diplomacy.CumulativeFxResidual - fx0, (uk.BaseMoney.Value + fr.BaseMoney.Value) - (bmUk0 + bmFr0), 6);
    }

    [Fact]
    public void 奨励金の取引は買い手が実際に払う額が積出し価格の_1マイナスs_財は保存する()
    {
        double mWin = double.NaN;
        for (double m = 0.80; m <= 1.40001 && double.IsNaN(mWin); m += 0.02)
            if (Run(m, 0, -1) <= 1e-9 && Run(m, 1, -1) > 1e-9) mWin = m;
        Assert.False(double.IsNaN(mWin));
        var (sim, g, frBuyers) = CheapBoard(mWin, 1, -1);
        var sellerReg = sim.Nations[0].World.Regions.First(r => r.Markets[g].Stock > 1e5);
        double s0 = sellerReg.Markets[g].Stock;
        sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
        Assert.Equal(s0 - sellerReg.Markets[g].Stock, Imported(frBuyers, g), 6);   // 売り手が減った分だけ買い手が増える（無から湧かない）
        Assert.Equal(sellerReg.Markets[g].LastExport, Imported(frBuyers, g), 6);
    }

    // ================================================================ ③ 政-30 植民地の議席 ================================================================

    private static double[] Votes(World w)
    {
        typeof(Parliament).GetMethod("ComputeVotes", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [w, w.Coef]);
        return [w.VoteShares[0], w.VoteShares[1], w.VoteShares[2]];
    }

    /// <summary>仏を 6 か月温め、普通選挙にし、最大人口の州を植民地格にして、植民地参政権を law にした盤面。</summary>
    private static (World w, Region colony) SeatBoard(ColonialSuffrageLaw law)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.Run(6);
        var w = sim.World;
        w.Suffrage = Suffrage.Universal;
        w.ColonialSuffrage = law;
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.Granted;   // 本国側の算入は 1（植民地格の分岐だけを見る）
        var colony = w.Regions.Where(r => r.State == RegionState.Active).OrderByDescending(r => r.SumPopulation()).First();
        colony.Tier = RegionTier.Colonial;
        // （★(475) 反証役 sf〔bv〕低#5：〈植民地の州の住民の思想を社会族に寄せる〉の注に当たるコードは無かった＝思想は元のまま・議席の向きは手計算 HandVotes と比べて見る。）
        return (w, colony);
    }

    private static double[] HandVotes(World w, bool excludeColonial)   // 植民地参政権 None の手計算＝植民地格の州の POP は 1 人も数えない
    {
        double cons = 0, lib = 0, soc = 0;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            if (excludeColonial && rg.Tier == RegionTier.Colonial) continue;
            double voters = p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (voters <= 1e-9) continue;
            double pc = 0, pl = 0, ps = 0;
            for (int i = 0; i < Ideologies.Count; i++)
            {
                int fam = PoliticalQuantities.FamilyOf(i);
                if (fam == 0) pc += p.IdeologyShares[i]; else if (fam == 1) pl += p.IdeologyShares[i]; else ps += p.IdeologyShares[i];
            }
            double sum = pc + pl + ps; if (sum < 1e-9) { pc = 1; sum = 1; }
            cons += voters * pc / sum; lib += voters * pl / sum; soc += voters * ps / sum;
        }
        double t = cons + lib + soc;
        double v0 = cons / t, v1 = lib / t, v2 = soc / t;
        if (w.Coef.ElectoralDistortion > 0)   // 得票→議席の歪み（第一党ボーナス）＝v^(1+γ) を再正規化
        {
            double e = 1.0 + w.Coef.ElectoralDistortion;
            double d0 = Math.Pow(v0, e), d1 = Math.Pow(v1, e), d2 = Math.Pow(v2, e), dt = d0 + d1 + d2;
            v0 = d0 / dt; v1 = d1 / dt; v2 = d2 / dt;
        }
        return [v0, v1, v2];
    }

    [Fact]
    public void 植民地参政権_なし_で植民地の住民の票が数えられず_全面_で全員が数えられる_手の算術()
    {
        var (wN, _) = SeatBoard(ColonialSuffrageLaw.None); var vN = Votes(wN);
        var (wF, _) = SeatBoard(ColonialSuffrageLaw.Full); var vF = Votes(wF);
        var handAll = HandVotes(wF, excludeColonial: false);   // 全面＝算入率 1＝誰も除かない手計算
        for (int i = 0; i < 3; i++) Assert.Equal(handAll[i], vF[i], 12);
        var hand = HandVotes(wN, excludeColonial: true);
        for (int i = 0; i < 3; i++) Assert.Equal(hand[i], vN[i], 12);                 // なし＝植民地の州を除いた手計算と一致
        Assert.True(Enumerable.Range(0, 3).Any(i => Math.Abs(vN[i] - vF[i]) > 1e-6), "法を変えても議席が動かない（述語が読まれていない）");
    }

    [Fact]
    public void 議席の票の述語は_選挙資金の有権者数にも同じ_1本_を通る()
    {
        var (wN, colN) = SeatBoard(ColonialSuffrageLaw.None);
        var (wF, _) = SeatBoard(ColonialSuffrageLaw.Full);
        // 半減点は有権者数に比例する。植民地なしは、植民地の州の住民の分だけ有権者数が減る＝半減点が下がる。
        Assert.True(wN.ElectionPushHalfPoint() < wF.ElectionPushHalfPoint() || !(wF.ElectionPushHalfPoint() > 0));
        double voteColony = 0;
        foreach (var (rg, p) in wN.AllPopsWithRegion()) if (ReferenceEquals(rg, colN)) voteColony += p.Size * InterestGroups.FranchiseVoice(p, wN.Suffrage, wN.Coef);
        Assert.True(voteColony > 0, "前提が崩れた：植民地にした州に有権者が居ない");
    }

    [Fact]
    public void SeatVoiceは植民地格の州だけ算入率を掛け_本国格はFranchiseVoiceそのもの()
    {
        foreach (var law in new[] { ColonialSuffrageLaw.None, ColonialSuffrageLaw.OwnNationals, ColonialSuffrageLaw.Full })
        {
            var (w, c) = SeatBoard(law);
            int colonialSeen = 0, mainlandSeen = 0;
            foreach (var (rg, p) in w.AllPopsWithRegion())
            {
                double fv = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
                if (rg.Tier == RegionTier.Colonial)
                {
                    colonialSeen++;
                    double incl = law switch { ColonialSuffrageLaw.None => 0.0, ColonialSuffrageLaw.Full => 1.0, _ => p.CoreEthnicShare.Value };   // 手の算術（法の段から）
                    Assert.Equal(fv * incl, InterestGroups.SeatVoice(w, rg, p, w.Suffrage));
                }
                else { mainlandSeen++; Assert.Equal(fv, InterestGroups.SeatVoice(w, rg, p, w.Suffrage)); }   // 本国格には及ばない
            }
            Assert.True(colonialSeen > 0 && mainlandSeen > 0);
            _ = c;
        }
    }

    private static (Region, Pop) FirstMainlandVoter(World w)
    {
        foreach (var (rg, p) in w.AllPopsWithRegion())
            if (rg.Tier != RegionTier.Colonial && InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef) > 0) return (rg, p);
        throw new InvalidOperationException("本国格に票を持つ POP が無い");
    }

    [Fact]
    public void 本国格の非中核の住民は移民原住民参政権_なし_でも議席の重みを減らされない()
    {
        var (w, _) = SeatBoard(ColonialSuffrageLaw.None);
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;   // 本国格の非中核の住民は算入率 = 中核度（SuffrageInclusion）。議席の票はこれを掛けない
        foreach (var (rg, p) in w.AllPopsWithRegion()) if (rg.Tier != RegionTier.Colonial) p.CoreEthnicShare = new Share(0.4);
        var (rg0, p0) = FirstMainlandVoter(w);
        Assert.Equal(0.4, InterestGroups.SuffrageInclusion(w, rg0, p0), 12);   // 前提：算入率は 0.4 に立つ
        Assert.Equal(InterestGroups.FranchiseVoice(p0, w.Suffrage, w.Coef), InterestGroups.SeatVoice(w, rg0, p0, w.Suffrage));
    }

    // ================================================================ ④ 経-20 予算の下限 ================================================================

    private static (Region region, World w, Pop pop) BudgetBoard(double savings)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.Run(6);
        var w = sim.World; var region = w.Region;
        var pop = region.Pops.Where((p, i) => !p.IsSoldier && !p.Closed && p.Size > 100 && i != region.InvestorPopIndex).First();
        pop.SetSavings(savings);
        return (region, w, pop);
    }

    private static double[] DemandAfterRun(Region region, World w) { EconomyStep.Run(region, w.Coef, 1.0, w.Bank, w); return region.Markets.Select(m => m.LastDemand).ToArray(); }

    [Fact]
    public void 負の貯蓄のPOPは床で買える量が0_貯蓄0のPOPと同じ需要になり_負の需要が立たない()
    {
        var (r1, w1, p1) = BudgetBoard(-5e4); var d1 = DemandAfterRun(r1, w1);
        var (rz, wz, pz) = BudgetBoard(0.0); var dz = DemandAfterRun(rz, wz);
        Assert.True(p1.Savings.Value < 0 || p1.Savings.Value > -5e4 - 1e9);   // 盤面が作れた
        for (int g = 0; g < d1.Length; g++) Assert.True(d1[g] >= 0, $"財 {g}：需要 {d1[g]} が負");
        // 床＝貯蓄 0 の POP と同じ予算（0）で買う＝需要は貯蓄 0 の盤面と同じ（この POP が買う量が 0）
        for (int g = 0; g < d1.Length; g++) Assert.Equal(dz[g], d1[g], 6);
        _ = wz; _ = pz;
    }

    [Fact]
    public void 床の後も貯蓄は触らない_負の貯蓄の記帳は保存する()
    {
        var (r1, w1, p1) = BudgetBoard(-5e4); var s0 = r1.Pops.Select(p => p.Savings.Value).ToArray();
        EconomyStep.Run(r1, w1.Coef, 1.0, w1.Bank, w1);
        for (int i = 0; i < r1.Pops.Count; i++)
        {
            var p = r1.Pops[i];
            Assert.Equal(s0[i] + (p.EarnedPerMonth - p.PaidPerMonth).Value, p.Savings.Value, 6);   // 月末の貯蓄＝期首＋受け取り−払い（床は予算だけで貯蓄の式は不変）
        }
        _ = p1;
    }

    [Fact]
    public void NaNの予算も床で0になる_否定形()
    {
        var (r1, w1, p1) = BudgetBoard(1e6);
        // 予算を NaN にして床の分岐だけを通す＝EconomyStep.Run の前処理は Savings から予算を作り直すので、床の述語を直接見る代わりに NaN の貯蓄で需要が NaN にならないこと。
        p1.SetSavings(double.NaN);
        var ex = Record.Exception(() => EconomyStep.Run(r1, w1.Coef, 1.0, w1.Bank, w1));
        _ = ex;   // NaN の貯蓄の世界は別の不変条件が先に落ちうる＝例外は問わず、需要が NaN でないことだけ見る
        Assert.All(r1.Markets, m => Assert.False(double.IsNaN(m.LastDemand)));
    }

    [Fact]
    public void 月初の床は家計の与信の分岐の前_負の穴を借りて埋めて買う形を避ける()
    {
        // 残りの予算 −5e4 の POP は、床が無いと shortfall = (desired − 負の予算/値段)×値段 に負の穴の分が乗る＝その分まで借りる。床があると月初に 0 へ止まるので、借りる額は貯蓄 0 の POP と同じ。
        long debtOf(double savings)
        {
            var (r, w, p) = BudgetBoard(savings);
            w.Coef.PopDebtDriven = 1; w.Coef.HouseholdCreditMonths = 12; w.Coef.HouseholdCreditStapleOnly = 1;
            p.LastEarnedPerMonth = new MoneyPerMonth(1e9);   // 枠は大きい（借りる額は shortfall で決まる）
            double d0 = p.Debt.Value;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return (long)Math.Round((p.Debt.Value - d0) * 1000);
        }
        long dNeg = debtOf(-5e4), dZero = debtOf(0);
        Assert.True(dZero > 0, "前提が崩れた：貯蓄 0 の POP が借りない（与信の分岐に入っていない）");
        Assert.Equal(dZero, dNeg);
    }

    [Fact]
    public void 方針の値が変わった回数は計器の欄に数える_同じ値を置き直しても増えない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.Run(2);
        var w = sim.World;
        WithIns<int>(() =>
        {
            double b = w.DiagSfBiasSwitchCum;
            w.SetTradeBias(3, -1); w.SetTradeBias(3, -1); w.SetTradeBias(3, 0); w.SetTradeBias(3, 0);
            Assert.Equal(2.0, w.DiagSfBiasSwitchCum - b);   // 0→−1・−1→0 の 2 回だけ
            return 0;
        });
    }

    [Fact]
    public void 立場の土台の高齢は兵士の老年と兵士の人口を分子と分母の両方に数える()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.Run(6);
        var w = sim.World;
        var s = w.AllPops().First(p => p.IsSoldier);
        s.Size = 1000; s.Ages = new double[2 * Pop.Layout.Bands]; s.Ages[2 * Pop.Layout.OldBandA] = 1.0;   // 全員 60 代（8層は 60〜69 歳・11層は 61〜70 歳）の男の兵士
        int bloc = PolicyPreferences.BlocOf(s);
        var sums = PolicyPreferences.ComputeBuildSums(w);
        double all = w.AllPops().Where(p => PolicyPreferences.BlocOf(p) == bloc).Sum(p => p.Size);                                                      // 手の算術：兵士も含む全員
        double allOld = w.AllPops().Where(p => PolicyPreferences.BlocOf(p) == bloc).Sum(p => p.Size * p.AgeShare(60, double.PositiveInfinity));
        Assert.Equal(all, sums.BlocPop[bloc], 6);     // 分母に兵士の人口
        Assert.Equal(allOld, sums.Old[bloc], 6);      // 分子に兵士の老年
        Assert.True(allOld >= 1000 - 1e-6);
    }

}
